#!/usr/bin/env python3
"""
chat-review.py — Claude Code session analyzer for Soccer Manager Pro.

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
CACHE_WRITE_MULT = 1.25   # 5-minute TTL; 1h TTL is 2.0
CACHE_READ_MULT = 0.10

# ---------------------------------------------------------------------------
# Thresholds. A finding is emitted only when its threshold trips.
# Tuned so a healthy session is silent.
# ---------------------------------------------------------------------------
THRESHOLDS = {
    "repeat_prompt_count": 3,        # same prompt shape N+ times => skill candidate
    "context_tokens": 60_000,        # per-session cached prefix this large => trim it
    "rules_file_tokens": 25_000,     # a single rules file this large => restructure
    "tool_repeat_count": 8,          # same tool+arg shape N+ times => script it
    "resolved_ratio": 0.25,          # >25% of open-issue entries say RESOLVED => archive
}


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
            if body and not body.startswith("<") and len(body) > 8:
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

    cost = (
        usage["input"] * PRICE_IN
        + usage["cache_write"] * PRICE_IN * CACHE_WRITE_MULT
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
            issues = text[j:]
            bullets = len(re.findall(r"^- \*\*", issues, re.M))
            resolved = len(re.findall(r"RESOLVED", issues))
            entry["open_issue_bullets"] = bullets
            entry["resolved_mentions"] = resolved
            entry["last_updated_entries"] = len(re.findall(r"\*\*Last Updated", text[:i]))
        out["rules_files"].append(entry)
    return out


# ---------------------------------------------------------------------------
# Findings — each gated on a threshold
# ---------------------------------------------------------------------------

def build_findings(sessions: list[dict], repo: dict | None) -> list[dict]:
    findings = []

    # 1. Repeated prompt shapes across sessions => skill candidates.
    shapes: dict[str, list[str]] = defaultdict(list)
    for s in sessions:
        for p in s["prompts"]:
            k = shape_key(p)
            if k:
                shapes[k].append(p[:200])
    for key, examples in sorted(shapes.items(), key=lambda kv: -len(kv[1])):
        if len(examples) < THRESHOLDS["repeat_prompt_count"]:
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

    # 2. Oversized cached prefix.
    for s in sessions:
        if s["context_tokens"] < THRESHOLDS["context_tokens"]:
            continue
        write_cost = s["context_tokens"] * PRICE_IN * CACHE_WRITE_MULT / 1e6
        findings.append({
            "id": f"context/{s['session_id'][:8]}",
            "category": "token-cost",
            "severity": "high" if s["context_tokens"] > 150_000 else "medium",
            "title": f"{s['context_tokens']:,} tokens loaded before turn 1",
            "detail": f"Session {s['session_id'][:8]} paid ${write_cost:.2f} to cache "
                      "its prefix, then ~10% of that per later turn. Trim the "
                      "always-loaded rules files.",
            "evidence": [f"cache_creation_input_tokens={s['context_tokens']:,}"],
            "metric": s["context_tokens"],
        })

    # 3. Repeated identical tool invocations => script it.
    agg: Counter = Counter()
    for s in sessions:
        for k, c in s["tools"].items():
            agg[k] += c
    for key, count in agg.most_common(12):
        if count < THRESHOLDS["tool_repeat_count"]:
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
        # 5. Resolved entries retained in the always-loaded file.
        bullets = entry.get("open_issue_bullets") or 0
        resolved = entry.get("resolved_mentions") or 0
        if bullets and resolved / max(bullets, 1) > THRESHOLDS["resolved_ratio"]:
            findings.append({
                "id": f"resolved/{entry['path']}",
                "category": "token-cost",
                "severity": "medium",
                "title": f"{resolved} RESOLVED mentions across {bullets} open-issue entries",
                "detail": "Resolved entries are history, not open issues. Archive them "
                          "to a sibling file; git already preserves them verbatim.",
                "evidence": [f"{entry['path']}: {bullets} bullets, {resolved} resolved"],
                "metric": resolved,
            })

    order = {"high": 0, "medium": 1, "low": 2}
    findings.sort(key=lambda f: (order.get(f["severity"], 3), -f.get("metric", 0)))
    return findings


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--projects", default=str(Path.home() / ".claude" / "projects"),
                    help="Claude Code projects directory")
    ap.add_argument("--repo", default=None, help="repo root, to measure rules files")
    ap.add_argument("--since", type=int, default=None, help="only sessions from last N days")
    ap.add_argument("--out", default=None, help="write JSON here instead of stdout")
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
                    "cache_write_mult": CACHE_WRITE_MULT,
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
