#!/usr/bin/env python3
"""
chat-review.py — Claude Code session analyzer for System XI.

Created: July 31, 2026
Purpose: Read Claude Code transcript JSONL files and emit an actionable-findings
         JSON document consumed by the chat-review artifact dashboard.

CONTRACT — quiet by default. A finding is emitted ONLY when its threshold trips.
A run that trips nothing emits `"findings": []`, and the dashboard renders its
"nothing actionable" state. This is deliberate: a monitor that always has
something to say trains its reader to ignore it.

Reads:  ~/.claude/projects/<slug>/*.jsonl  (Claude Code writes one file per session)
Writes: findings JSON on stdout (or --out PATH)

Usage:
    python3 tools/chat-review.py                       # analyze all local sessions
    python3 tools/chat-review.py --out review.json     # write to a file
    python3 tools/chat-review.py --repo .              # include repo-side measures
    python3 tools/chat-review.py --since 7             # last N days only

No network, no API key, no model call. Pure local measurement.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone
from pathlib import Path

SCHEMA_VERSION = 1

# Opus 5 pricing, $ per 1M tokens. Source: claude-api skill, cached 2026-06-24.
PRICE_IN = 5.00
PRICE_OUT = 25.00
# Cache writes price by TTL, and the gap is large enough to matter: a session
# whose writes are all 1h TTL costs 60% more to cache than the 5m rate implies.
# Claude Code writes the split into usage.cache_creation, so measure it rather
# than assuming — this file assumed 1.25 flat for its first three days and
# understated its own sessions by ~27%.
CACHE_WRITE_MULT_5M = 1.25
CACHE_WRITE_MULT_1H = 2.00
CACHE_READ_MULT = 0.10

# ---------------------------------------------------------------------------
# Thresholds. A finding is emitted only when its threshold trips.
# Tuned so a healthy session is silent.
# ---------------------------------------------------------------------------
THRESHOLDS = {
    "repeat_prompt_count": 3,        # same prompt shape N+ times => skill candidate
    "min_sessions": 2,               # ...and in N+ DISTINCT sessions. A run inside one
                                     # session is just the shape of that task, not a habit.
    "rules_prefix_tokens": 20_000,   # ALL rules files together this large => split them.
                                     # Gates on the controllable share, not the whole
                                     # cached prefix — most of that prefix is harness,
                                     # and no repo edit can shrink it.
    "rules_file_tokens": 25_000,     # a single rules file this large => restructure
    "tool_repeat_count": 8,          # same tool+arg shape N+ times => script it
    "resolved_ratio": 0.25,          # >25% of open-issue entries say RESOLVED => archive
}

# Harness-generated user turns. These arrive with role=user but nobody typed them:
# compaction resumes, interrupt markers, slash-command echoes, hook output. Counting
# them produced "repeated ask, 5x: Continue from where you left off" — a skill
# suggestion for a message the user cannot stop sending.
HARNESS_PROMPT = re.compile(
    r"^\s*(?:"
    r"continue from where you left off"
    r"|\[request interrupted"
    r"|caveat: the messages below were generated"
    r"|this session is being continued from a previous conversation"
    r"|api error"
    r"|\[no response requested\]"
    r")",
    re.I,
)


# ---------------------------------------------------------------------------
# Transcript parsing
# ---------------------------------------------------------------------------

def iter_transcripts(root: Path, since_days: int | None):
    """Yield (path, [parsed lines]) for each session transcript."""
    if not root.exists():
        return
    cutoff = None
    if since_days is not None:
        cutoff = datetime.now(timezone.utc) - timedelta(days=since_days)
    for path in sorted(root.rglob("*.jsonl")):
        try:
            mtime = datetime.fromtimestamp(path.stat().st_mtime, timezone.utc)
        except OSError:
            continue
        if cutoff and mtime < cutoff:
            continue
        rows = []
        with path.open(encoding="utf-8", errors="replace") as fh:
            for line in fh:
                line = line.strip()
                if not line:
                    continue
                try:
                    rows.append(json.loads(line))
                except json.JSONDecodeError:
                    continue  # a partially-flushed final line is normal
        if rows:
            yield path, rows


def text_of(content) -> str:
    """Claude Code stores message content as a str or a list of typed blocks."""
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        parts = []
        for block in content:
            if isinstance(block, dict) and block.get("type") == "text":
                parts.append(block.get("text", ""))
        return "\n".join(parts)
    return ""


def tool_calls_of(content) -> list[tuple[str, str]]:
    """Return (tool_name, coarse_arg_signature) for each tool_use block."""
    out = []
    if not isinstance(content, list):
        return out
    for block in content:
        if not isinstance(block, dict) or block.get("type") != "tool_use":
            continue
        name = block.get("name", "?")
        inp = block.get("input") or {}
        # Coarse signature: the command/pattern/path shape, not the full value.
        sig = ""
        for key in ("command", "pattern", "file_path", "skill", "query"):
            if key in inp and isinstance(inp[key], str):
                sig = normalize(inp[key])[:80]
                break
        out.append((name, sig))
    return out


STOPWORDS = {
    "the", "a", "an", "and", "or", "to", "of", "in", "for", "on", "is", "it",
    "this", "that", "with", "please", "can", "you", "i", "we", "my", "me",
}


def normalize(s: str) -> str:
    """Collapse a prompt to a comparable shape: lowercase, no numbers/ids/paths."""
    s = s.lower()
    s = re.sub(r"\berr-\d{3}-\d{3}\b", "<err>", s)
    s = re.sub(r"\bar-\d+\b", "<ar>", s)
    s = re.sub(r"[a-z0-9_\-/.]+\.(cs|md|py|json|jsonl|html|sh)\b", "<file>", s)
    s = re.sub(r"\b\d[\d,._]*\b", "<n>", s)
    s = re.sub(r"[^a-z<>\s]", " ", s)
    words = [w for w in s.split() if w not in STOPWORDS and len(w) > 2]
    return " ".join(words)


def shape_key(prompt: str) -> str:
    """A stable key for 'this is the same kind of ask'. Top content words, sorted."""
    words = normalize(prompt).split()
    if len(words) < 3:
        return ""
    return " ".join(sorted(set(words))[:12])


# ---------------------------------------------------------------------------
# Session measurement
# ---------------------------------------------------------------------------

def analyze_session(path: Path, rows: list[dict]) -> dict:
    prompts: list[str] = []
    tools: Counter = Counter()
    usage = {
        "input": 0, "output": 0,
        "cache_write": 0, "cache_read": 0,
        "cache_write_1h": 0, "cache_write_5m": 0,
    }
    first_write = 0
    models: Counter = Counter()
    started = ended = None

    for row in rows:
        ts = row.get("timestamp")
        if ts:
            started = started or ts
            ended = ts
        rtype = row.get("type")

        if rtype == "user":
            msg = row.get("message") or {}
            # A tool_result also arrives as role=user; only count real prompts.
            if row.get("toolUseResult") is not None:
                continue
            body = text_of(msg.get("content"))
            if (body and not body.startswith("<") and len(body) > 8
                    and not HARNESS_PROMPT.match(body)):
                prompts.append(body)

        elif rtype == "assistant":
            msg = row.get("message") or {}
            models[msg.get("model") or "?"] += 1
            u = msg.get("usage") or {}
            usage["input"] += u.get("input_tokens", 0) or 0
            usage["output"] += u.get("output_tokens", 0) or 0
            cw = u.get("cache_creation_input_tokens", 0) or 0
            usage["cache_write"] += cw
            usage["cache_read"] += u.get("cache_read_input_tokens", 0) or 0
            if first_write == 0 and cw:
                first_write = cw
            cc = u.get("cache_creation") or {}
            usage["cache_write_1h"] += cc.get("ephemeral_1h_input_tokens", 0) or 0
            usage["cache_write_5m"] += cc.get("ephemeral_5m_input_tokens", 0) or 0
            for name, sig in tool_calls_of(msg.get("content")):
                tools[(name, sig)] += 1

    # Split cache writes by TTL. Anything the transcript did not classify is
    # priced at the 5m rate — that is the documented default when no ttl is set,
    # not a guess chosen to flatter the number.
    w_1h = usage["cache_write_1h"]
    w_5m = usage["cache_write"] - w_1h
    cost = (
        usage["input"] * PRICE_IN
        + w_1h * PRICE_IN * CACHE_WRITE_MULT_1H
        + w_5m * PRICE_IN * CACHE_WRITE_MULT_5M
        + usage["cache_read"] * PRICE_IN * CACHE_READ_MULT
        + usage["output"] * PRICE_OUT
    ) / 1_000_000

    return {
        "session_id": path.stem,
        "file": str(path),
        "started": started,
        "ended": ended,
        "turns": sum(models.values()),
        "prompts": prompts,
        "context_tokens": first_write,   # the cached prefix carried into turn 1
        "usage": usage,
        "cost_usd": round(cost, 4),
        "models": dict(models),
        "tools": {f"{n}|{s}": c for (n, s), c in tools.items()},
    }


# ---------------------------------------------------------------------------
# Repo-side measurement
# ---------------------------------------------------------------------------

def measure_repo(repo: Path) -> dict:
    """Measure the always-loaded rules files. chars/4 is an estimate — see note."""
    out = {"rules_files": [], "note": "token counts are chars/4 estimates; "
           "for exact figures run: ant messages count-tokens --model claude-opus-5 "
           "--message '{role: user, content: \"@./CLAUDE.md\"}'"}
    for rel in ("CLAUDE.md", "src/CLAUDE.md"):
        p = repo / rel
        if not p.exists():
            continue
        text = p.read_text(encoding="utf-8", errors="replace")
        entry = {"path": rel, "chars": len(text), "tokens_est": round(len(text) / 4)}
        # Section split for the root rules file.
        i = text.find("## PROJECT IDENTITY")
        j = text.find("## OPEN ISSUES")
        if i > 0 and j > i:
            entry["sections"] = {
                "header_chain": round(i / 4),
                "rules_body": round((j - i) / 4),
                "open_issues": round((len(text) - j) / 4),
            }
            entry["last_updated_entries"] = len(re.findall(r"\*\*Last Updated", text[:i]))
        out["rules_files"].append(entry)

    # Archive hygiene. This used to read CLAUDE.md's inlined OPEN ISSUES section;
    # after the split that section is an index of titles, so the entries — and the
    # staleness worth catching — live in docs/tracking/open-issues.md. Reading the
    # index instead would report zero forever, which is worse than not checking.
    p = repo / "docs/tracking/open-issues.md"
    if p.exists():
        issues = p.read_text(encoding="utf-8", errors="replace")
        entries = re.split(r"(?m)^(?=- \*\*)", issues)[1:]
        # A title announcing closure is the easy case and, in practice, the rare one:
        # entries go stale precisely because nobody edited the title. The signal that
        # actually finds them is a BODY that announces closure under a title that does
        # not. Markers are deliberately narrow phrases, not bare "resolved" / "✅" —
        # a live entry's body routinely reports sub-parts as resolved.
        BODY_CLOSED = re.compile(
            r"superseded above; entry retained for history"
            r"|opened and closed"
            r"|\bUNBLOCKED\b", re.I)
        TITLE_CLOSED = re.compile(r"\b(RESOLVED|CLOSED|COMPLETE|DONE)\b", re.I)
        contradicted, closed_titles, seen = [], 0, defaultdict(list)
        for e in entries:
            m = re.match(r"- \*\*(.+?)\*\*", e, re.S)
            if not m:
                continue
            title = " ".join(m.group(1).split())
            body = e[m.end():]
            seen[re.sub(r"[^a-z0-9 ]", "", title.lower())[:120]].append(title)
            if TITLE_CLOSED.search(title):
                closed_titles += 1
            elif BODY_CLOSED.search(body):
                contradicted.append(title[:90])
        out["open_issues"] = {
            "path": "docs/tracking/open-issues.md",
            "active_entries": len(entries),
            "titles_announcing_closure": closed_titles,
            "body_closed_title_not": contradicted,
            "duplicate_titles": [v[0][:90] for v in seen.values() if len(v) > 1],
        }
    return out


# ---------------------------------------------------------------------------
# Findings — each gated on a threshold
# ---------------------------------------------------------------------------

def build_findings(sessions: list[dict], repo: dict | None) -> list[dict]:
    findings = []

    # 1. Repeated prompt shapes across sessions => skill candidates.
    shapes: dict[str, list[str]] = defaultdict(list)
    shape_sessions: dict[str, set] = defaultdict(set)
    for s in sessions:
        for p in s["prompts"]:
            k = shape_key(p)
            if k:
                shapes[k].append(p[:200])
                shape_sessions[k].add(s["session_id"])
    for key, examples in sorted(shapes.items(), key=lambda kv: -len(kv[1])):
        if len(examples) < THRESHOLDS["repeat_prompt_count"]:
            continue
        # A habit spans sessions. Three similar asks inside one session is one task
        # being steered, which is not something a skill would have saved.
        if len(shape_sessions[key]) < THRESHOLDS["min_sessions"]:
            continue
        findings.append({
            "id": f"repeat/{abs(hash(key)) % 10**6}",
            "category": "skill-candidate",
            "severity": "medium",
            "title": f"Repeated ask, {len(examples)}x: \"{examples[0][:70]}…\"",
            "detail": "This prompt shape recurs across sessions. Re-specifying it "
                      "each time costs tokens and drifts. Encode it as a skill.",
            "evidence": examples[:4],
            "metric": len(examples),
        })

    # 2. Rules files that dominate the part of the prefix this repo controls.
    #
    #    This used to gate on the whole cached prefix, which was wrong twice
    #    over. The prefix is mostly harness — system prompt, tool schemas, MCP
    #    definitions, the skills listing — none of which any repo edit can
    #    shrink; on the session that exposed this, the rules files were 15.9k of
    #    203.8k, under 8%. So the old threshold fired forever (the floor sits
    #    above it) while the advice it printed, "trim the always-loaded rules
    #    files", pointed at the one part that was already small. A monitor that
    #    always fires and misattributes the cause is worse than silence.
    #
    #    Gate on the controllable share instead, and print the total only as
    #    context so the attribution stays visible. Finding 4 catches ONE
    #    oversized rules file; this catches many small ones summing large.
    rules = (repo or {}).get("rules_files") or []
    rules_total = sum(e["tokens_est"] for e in rules)
    if rules and rules_total >= THRESHOLDS["rules_prefix_tokens"]:
        biggest = max(rules, key=lambda e: e["tokens_est"])
        prefixes = [s["context_tokens"] for s in sessions if s["context_tokens"]]
        share = ""
        if prefixes:
            typical = max(prefixes)
            share = (f" For scale, the largest measured session prefix was "
                     f"{typical:,} tokens, so these files are ~"
                     f"{round(100 * rules_total / typical)}% of it — the rest is "
                     "harness (system prompt, tool schemas, skills listing) and "
                     "no repo edit shrinks it.")
        findings.append({
            "id": "rules-prefix/total",
            "category": "token-cost",
            "severity": "medium",
            "title": f"{len(rules)} rules files total ~{rules_total:,} tokens",
            "detail": (f"Every session in this subtree loads all of them. The "
                       f"largest is {biggest['path']} at ~{biggest['tokens_est']:,}."
                       f"{share} Split or trim by content that is reference "
                       "rather than rule."),
            "evidence": [f"{e['path']}: ~{e['tokens_est']:,}" for e in rules],
            "metric": rules_total,
        })

    # 3. Repeated identical tool invocations => script it.
    agg: Counter = Counter()
    tool_sessions: dict[str, set] = defaultdict(set)
    for s in sessions:
        for k, c in s["tools"].items():
            agg[k] += c
            tool_sessions[k].add(s["session_id"])
    for key, count in agg.most_common(12):
        if count < THRESHOLDS["tool_repeat_count"]:
            continue
        # Same rule as prompt shapes: nine Edits inside one session is the shape of
        # that session's work. A shape worth scripting comes back on another day.
        if len(tool_sessions[key]) < THRESHOLDS["min_sessions"]:
            continue
        name, _, sig = key.partition("|")
        if not sig:
            continue
        findings.append({
            "id": f"tool/{abs(hash(key)) % 10**6}",
            "category": "script-candidate",
            "severity": "low",
            "title": f"{name} run {count}x with the same shape",
            "detail": "A repeated invocation with a stable argument shape is a "
                      "script, not a judgement call. Wrap it and reference it "
                      "from a skill so it is not re-derived.",
            "evidence": [f"{name}: {sig}"],
            "metric": count,
        })

    # 4. Rules files that are too large to be rules.
    for entry in (repo or {}).get("rules_files", []):
        if entry["tokens_est"] < THRESHOLDS["rules_file_tokens"]:
            continue
        sec = entry.get("sections")
        detail = (f"{entry['path']} is ~{entry['tokens_est']:,} tokens and loads "
                  "into every session that touches its subtree.")
        if sec:
            live = sec["rules_body"]
            pct = round(100 * live / entry["tokens_est"])
            detail += (f" Only ~{live:,} tokens ({pct}%) are durable rules; "
                       f"~{sec['header_chain']:,} are the changelog header chain and "
                       f"~{sec['open_issues']:,} are open-issue entries. Both belong "
                       "in docs/tracking/, referenced by a short index.")
        findings.append({
            "id": f"rules/{entry['path']}",
            "category": "token-cost",
            "severity": "high",
            "title": f"{entry['path']} is ~{entry['tokens_est']:,} tokens",
            "detail": detail,
            "evidence": [json.dumps(sec)] if sec else [],
            "metric": entry["tokens_est"],
        })
    # 5. Closed entries still sitting in the live open-issues file. Runs against
    #    docs/tracking/open-issues.md regardless of rules-file size — it is a
    #    tracking-hygiene check, and it was previously reachable only when the rules
    #    file was oversized, so trimming the rules file silently disabled it.
    oi = (repo or {}).get("open_issues")
    if oi:
        bullets, closed = oi["active_entries"], oi["titles_announcing_closure"]
        if bullets and closed / bullets > THRESHOLDS["resolved_ratio"]:
            findings.append({
                "id": "resolved/open-issues",
                "category": "workflow",
                "severity": "medium",
                "title": f"{closed} of {bullets} active entries have titles announcing closure",
                "detail": "An entry whose own title says RESOLVED is history, not an open "
                          "issue. Move it to open-issues-resolved.md in the commit that "
                          "closes it; git preserves it verbatim either way.",
                "evidence": [f"{oi['path']}: {bullets} active, {closed} announcing closure"],
                "metric": closed,
            })
        stale = oi.get("body_closed_title_not") or []
        if stale:
            findings.append({
                "id": "stale/open-issues",
                "category": "workflow",
                "severity": "medium",
                "title": f"{len(stale)} active entries whose body says closed but title does not",
                "detail": "The title is what the CLAUDE.md index shows, so a title that "
                          "outlived its body misreports project state to every session. "
                          "Re-read each against its owning source, then archive or retitle.",
                "evidence": stale[:4],
                "metric": len(stale),
            })
        dupes = oi.get("duplicate_titles") or []
        if dupes:
            findings.append({
                "id": "dupe/open-issues",
                "category": "workflow",
                "severity": "low",
                "title": f"{len(dupes)} duplicated open-issue titles",
                "detail": "Two entries with the same title are usually one item filed "
                          "twice — but diff them before merging: the copies may differ, "
                          "and the later one may record a correction the earlier lacks.",
                "evidence": dupes[:4],
                "metric": len(dupes),
            })

    order = {"high": 0, "medium": 1, "low": 2}
    findings.sort(key=lambda f: (order.get(f["severity"], 3), -f.get("metric", 0)))
    return findings


def emit_hook(findings: list[dict]) -> int:
    """SessionStart hook output. Silence is the common case and the point.

    Prints nothing at all when nothing tripped — no empty JSON, no "all clear".
    A monitor that greets you every session trains you to ignore it, and the
    standing instruction on this repo is to speak only when there is something
    to act on.

    When something did trip, the payload is deliberately small: one line per
    finding, capped, with no evidence arrays and no session dump. The full
    record is a `python3 tools/chat-review.py --repo .` away.
    """
    if not findings:
        return 0

    MAX = 5
    shown, extra = findings[:MAX], max(0, len(findings) - MAX)
    lines = [f"- [{f['severity']}] {f['category']}: {f['title']}" for f in shown]
    if extra:
        lines.append(f"- (+{extra} more, lower severity)")

    n = len(findings)
    headline = f"chat-review: {n} actionable finding{'s' if n != 1 else ''}"
    top = findings[0]["title"]

    print(json.dumps({
        "systemMessage": f"{headline}. Top: {top}",
        "suppressOutput": True,
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": (
                f"{headline} from tools/chat-review.py (thresholds tripped; this is "
                "not a scheduled digest):\n" + "\n".join(lines) +
                "\n\nDo not act on these unless the user asks, and do not re-raise a "
                "finding they have already declined. Run `python3 "
                "tools/chat-review.py --repo .` for the full record, or use the "
                "chat-review skill."
            ),
        },
    }))
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--projects", default=str(Path.home() / ".claude" / "projects"),
                    help="Claude Code projects directory")
    ap.add_argument("--repo", default=None, help="repo root, to measure rules files")
    ap.add_argument("--since", type=int, default=None, help="only sessions from last N days")
    ap.add_argument("--out", default=None, help="write JSON here instead of stdout")
    ap.add_argument("--hook", action="store_true",
                    help="SessionStart hook mode: print NOTHING when there is nothing "
                         "actionable; otherwise print a compact Claude Code hook JSON "
                         "payload. Never fails the session — errors exit 0 silently.")
    args = ap.parse_args()

    sessions = [analyze_session(p, rows)
                for p, rows in iter_transcripts(Path(args.projects), args.since)]
    repo = measure_repo(Path(args.repo)) if args.repo else None
    findings = build_findings(sessions, repo)

    total_cost = round(sum(s["cost_usd"] for s in sessions), 2)
    doc = {
        "schema_version": SCHEMA_VERSION,
        "generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "pricing": {"model": "claude-opus-5", "input_per_mtok": PRICE_IN,
                    "output_per_mtok": PRICE_OUT,
                    "cache_write_mult_5m": CACHE_WRITE_MULT_5M,
                    "cache_write_mult_1h": CACHE_WRITE_MULT_1H,
                    "cache_read_mult": CACHE_READ_MULT},
        "summary": {
            "sessions": len(sessions),
            "turns": sum(s["turns"] for s in sessions),
            "cost_usd": total_cost,
            "tokens_out": sum(s["usage"]["output"] for s in sessions),
            "tokens_cache_write": sum(s["usage"]["cache_write"] for s in sessions),
            "tokens_cache_read": sum(s["usage"]["cache_read"] for s in sessions),
            "actionable": len(findings),
        },
        "repo": repo,
        "sessions": [{k: v for k, v in s.items() if k != "prompts"} for s in sessions],
        "findings": findings,
    }

    if args.hook:
        return emit_hook(findings)

    payload = json.dumps(doc, indent=2)
    if args.out:
        Path(args.out).write_text(payload, encoding="utf-8")
        print(f"wrote {args.out}: {len(findings)} actionable finding(s) "
              f"across {len(sessions)} session(s)", file=sys.stderr)
    else:
        print(payload)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
