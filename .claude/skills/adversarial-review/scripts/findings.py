#!/usr/bin/env python3
"""findings.py — round bookkeeping for the delegated adversarial-review loop.

The orchestrator's job in step 2 (Triage) is deduplicate, assign stable IDs that
persist across rounds, and carry survivors forward. Step 4 (Verify) checks each
prior finding's disposition against the new report. None of that is judgement —
it is set arithmetic over two lists — and all of it degrades exactly where the
loop is most valuable: round 4, several parallel reviewers in, when the IDs have
to line up with what round 1 said.

Held in the orchestrator's context, that decays. Held here, it is a lookup:

  - IDs (H1, M3, L2) are assigned once and reused whenever the same finding
    reappears, keyed by a caller-supplied stable `key`.
  - Prior findings are auto-classified resolved / still present / moved.
  - The severity tally is derived from the list, so the header cannot contradict
    the body — a real risk when merging reports from several reviewers.
  - The 5-round budget is tracked and reports itself at the cap.
  - Termination (only Low, or none) is computed, not asserted.

What stays with the orchestrator: whether two reports describe the same defect
(supply the same `key`), what severity a finding is, whether to escalate to
Fable 5, and every word of the finding text. This script never invents, rates,
re-rates, or drops a finding.

Usage:
  findings.py round   <round.json> [--ledger DIR] [--max-rounds N]
  findings.py status  [--ledger DIR]

round.json:
{
  "target": "src/match-engine",
  "reviewers": "2 x Opus 5",           # optional, printed in the tally line
  "escalations": "1 x Fable 5",        # optional
  "findings": [
    {"key": "gk-latch-restore",        # STABLE across rounds — this drives ID reuse
     "severity": "High",               # High | Medium | Low
     "title": "Trigger latch not serialized",
     "location": "MatchEngine.cs:412",
     "problem": "A restore re-fires a suppressed trigger, so ...",
     "fix": "Serialize _saveCommittedForGk beside the RNG cursor."}
  ],
  "scope": ["MatchEngine.cs (reviewer A)", "GoalkeeperTickState.cs (reviewer A)"],
  "unverified": ["native MXCSR read - no plugin on this host"]
}

Exit codes: 0 = loop COMPLETE (only Low findings, or none)
            1 = loop continues (High or Medium outstanding)
            3 = round budget exhausted with High findings still open
            2 = usage or input error
"""

import argparse
import json
import pathlib
import sys

SEVERITIES = ("High", "Medium", "Low")
PREFIX = {"High": "H", "Medium": "M", "Low": "L"}
GATING = ("High", "Medium")
DEFAULT_MAX_ROUNDS = 5


def die(msg):
    print(f"findings.py: {msg}", file=sys.stderr)
    sys.exit(2)


def load(path):
    p = pathlib.Path(path)
    if not p.exists():
        die(f"no such file: {path}")
    try:
        data = json.loads(p.read_text())
    except json.JSONDecodeError as e:
        die(f"{path} is not valid JSON: {e}")
    if not isinstance(data, dict) or not isinstance(data.get("findings"), list):
        die("input must be an object with a 'findings' array")
    for i, f in enumerate(data["findings"]):
        missing = [k for k in ("key", "severity", "title", "problem", "fix") if not f.get(k)]
        if missing:
            die(f"finding[{i}] missing required field(s): {', '.join(missing)}")
        if f["severity"] not in SEVERITIES:
            die(f"finding[{i}] severity {f['severity']!r} not one of {SEVERITIES}")
    keys = [f["key"] for f in data["findings"]]
    dupes = {k for k in keys if keys.count(k) > 1}
    if dupes:
        # Two reviewers reporting the same defect is expected; merging them is
        # triage, and triage happens before this script runs.
        die(f"duplicate key(s) — deduplicate during triage first: {', '.join(sorted(dupes))}")
    return data


def ledger_dir(arg):
    d = pathlib.Path(arg or ".adversarial-review")
    d.mkdir(parents=True, exist_ok=True)
    return d


def load_rounds(d):
    out = []
    for p in sorted(d.glob("round-*.json"), key=lambda x: int(x.stem.split("-")[1])):
        try:
            out.append((int(p.stem.split("-")[1]), json.loads(p.read_text())))
        except (ValueError, json.JSONDecodeError):
            continue
    return out


def load_idmap(d):
    p = d / "ids.json"
    if p.exists():
        try:
            return json.loads(p.read_text())
        except json.JSONDecodeError:
            return {}
    return {}


def assign_ids(findings, idmap):
    """Reuse an existing ID for a known key; mint the next free one otherwise.

    An ID is bound to the DEFECT, not to its severity — a finding re-rated from
    Medium to High in a later round keeps M3 rather than becoming H2, so the
    thread back to round 1 survives the re-rating. Severity is carried in the
    record, not the label.
    """
    used = {v for v in idmap.values()}
    counters = {p: 0 for p in PREFIX.values()}
    for fid in used:
        pfx, num = fid[0], fid[1:]
        if pfx in counters and num.isdigit():
            counters[pfx] = max(counters[pfx], int(num))
    for f in findings:
        if f["key"] in idmap:
            f["id"] = idmap[f["key"]]
        else:
            pfx = PREFIX[f["severity"]]
            counters[pfx] += 1
            f["id"] = f"{pfx}{counters[pfx]}"
            idmap[f["key"]] = f["id"]
    return idmap


def cmd_round(args):
    data = load(args.file)
    d = ledger_dir(args.ledger)
    history = load_rounds(d)
    round_n = (history[-1][0] + 1) if history else 1
    idmap = load_idmap(d)

    findings = data["findings"]
    assign_ids(findings, idmap)

    prior = {}
    if history:
        for f in history[-1][1].get("findings", []):
            prior[f["key"]] = f

    current_keys = {f["key"] for f in findings}
    by_key = {f["key"]: f for f in findings}

    dispositions = []
    for key, pf in prior.items():
        pid = pf.get("id", idmap.get(key, "?"))
        if key not in current_keys:
            dispositions.append(f"{pid} → resolved")
        else:
            cur = by_key[key]
            old_loc, new_loc = pf.get("location"), cur.get("location")
            if old_loc and new_loc and old_loc != new_loc:
                dispositions.append(f"{pid} → moved to `{new_loc}`")
            elif pf["severity"] != cur["severity"]:
                dispositions.append(f"{pid} → still present, re-rated {pf['severity']} → {cur['severity']}")
            else:
                dispositions.append(f"{pid} → still present")

    counts = {s: sum(1 for f in findings if f["severity"] == s) for s in SEVERITIES}
    open_gating = counts["High"] + counts["Medium"]
    complete = open_gating == 0

    L = []
    L.append(f"## Adversarial review — {data.get('target', 'target unspecified')} — round {round_n}")
    L.append("")
    tally = " · ".join(f"**{counts[s]} {s}**" for s in SEVERITIES if counts[s]) or "**no findings**"
    meta = []
    if data.get("reviewers"):
        meta.append(f"reviewers: {data['reviewers']}")
    if data.get("escalations"):
        meta.append(f"escalations: {data['escalations']}")
    L.append(tally + (f"   ({'; '.join(meta)})" if meta else ""))
    L.append("")

    for sev in SEVERITIES:
        group = [f for f in findings if f["severity"] == sev]
        if not group:
            continue
        L.append(f"### {sev}")
        for f in group:
            loc = f" — `{f['location']}`" if f.get("location") else ""
            carried = " *(carried)*" if f["key"] in prior else ""
            L.append(f"- **{f['id']} · {f['title']}**{loc}{carried}. {f['problem']}")
            L.append(f"  *Fix:* {f['fix']}")
        L.append("")

    if round_n > 1:
        L.append("### Prior findings")
        L.extend(f"- {x}" for x in dispositions) if dispositions else L.append("- none carried into this round")
        L.append("")

    L.append("### Scope reviewed")
    scope = data.get("scope") or []
    L.append(", ".join(f"`{s}`" for s in scope) if scope
             else "_no scope recorded — the full re-review rule requires accounting for the whole scope._")
    for u in data.get("unverified") or []:
        L.append(f"- Could not verify: {u}")
    L.append("")

    budget_exhausted = round_n >= args.max_rounds and counts["High"] > 0
    if complete:
        L.append(f"**Loop complete** — round {round_n} returned no High or Medium findings. "
                 f"{counts['Low']} Low finding(s) recorded; Low does not gate.")
    elif budget_exhausted:
        L.append(f"**Round budget exhausted** — {args.max_rounds} rounds spent and "
                 f"{counts['High']} High finding(s) still open. Per the skill's round-budget rule, stop "
                 f"and report state rather than starting round {round_n + 1}: an artifact that will not "
                 f"converge in {args.max_rounds} rounds has a problem this loop is not the right tool for.")
    else:
        L.append(f"**Not clean** — {open_gating} gating finding(s) open "
                 f"({counts['High']} High, {counts['Medium']} Medium). Round {round_n} of {args.max_rounds}.")

    print("\n".join(L))

    (d / f"round-{round_n}.json").write_text(json.dumps(data, indent=2))
    (d / "ids.json").write_text(json.dumps(idmap, indent=2))
    print(f"\n<!-- ledger: {d}/round-{round_n}.json -->", file=sys.stderr)

    if budget_exhausted:
        return 3
    return 0 if complete else 1


def cmd_status(args):
    d = ledger_dir(args.ledger)
    history = load_rounds(d)
    if not history:
        print("No rounds recorded yet.")
        return 0
    idmap = load_idmap(d)
    for n, data in history:
        c = {s: sum(1 for f in data.get("findings", []) if f["severity"] == s) for s in SEVERITIES}
        print(f"round {n}: {c['High']}H {c['Medium']}M {c['Low']}L — {data.get('target', '?')}")
    print(f"\n{len(idmap)} distinct finding(s) tracked across {len(history)} round(s).")
    return 0


def main():
    ap = argparse.ArgumentParser(add_help=True)
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("round")
    r.add_argument("file")
    r.add_argument("--ledger", default=None)
    r.add_argument("--max-rounds", type=int, default=DEFAULT_MAX_ROUNDS)
    r.set_defaults(fn=cmd_round)
    s = sub.add_parser("status")
    s.add_argument("--ledger", default=None)
    s.set_defaults(fn=cmd_status)
    args = ap.parse_args()
    sys.exit(args.fn(args))


if __name__ == "__main__":
    main()
