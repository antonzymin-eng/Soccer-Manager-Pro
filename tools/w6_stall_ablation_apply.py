#!/usr/bin/env python3
"""Measurement-only W6 possession-stall ablations.

Applies a named rollback slice to the exact post-W6 checkout. This is not a
production fix; it is a causal localization tool.
"""
from pathlib import Path
import sys

if len(sys.argv) != 3:
    raise SystemExit("usage: w6_stall_ablation_apply.py <repo> <variant>")
repo = Path(sys.argv[1])
variant = sys.argv[2]
valid = {"current", "gk-no-kick-release", "gk-legacy", "outfield-legacy", "all-legacy"}
if variant not in valid:
    raise SystemExit(f"unsupported variant {variant}; expected one of {sorted(valid)}")

p = repo / "src/match-engine/MatchEngine.cs"
s = p.read_text(encoding="utf-8")


def once(old: str, new: str, label: str) -> None:
    global s
    n = s.count(old)
    if n != 1:
        raise SystemExit(f"{label}: expected exactly one anchor, found {n}")
    s = s.replace(old, new, 1)


if variant in {"gk-no-kick-release", "gk-legacy", "all-legacy"}:
    once(
        """                BallCollision.ApplyKick(ref _engine._ball, velocity, spin, agentId, matchTime, logger: null);\n                _engine.ReleasePossessionOnKick(agentId);\n\n                // ERR-012-011""",
        """                BallCollision.ApplyKick(ref _engine._ball, velocity, spin, agentId, matchTime, logger: null);\n\n                // MEASUREMENT ABLATION: omit W6 GK/heading kick-side possession release.\n                // ERR-012-011""",
        "gk kick release",
    )

if variant in {"gk-legacy", "all-legacy"}:
    once(
        "            public void SetPossessor(int agentId) => _engine.TakeControlledPossession(agentId);",
        "            public void SetPossessor(int agentId) => _engine._possessingAgentId = agentId; // measurement legacy",
        "gk SetPossessor",
    )

if variant in {"outfield-legacy", "all-legacy"}:
    once(
        "                        TakeControlledPossession(newHolder);",
        "                        _possessingAgentId = newHolder; // measurement legacy",
        "first-touch controlled",
    )
    once(
        "                            TakeControlledPossession(interceptor);",
        "                            _possessingAgentId = interceptor; // measurement legacy",
        "first-touch interceptor",
    )
    once(
        "                TakeControlledPossession(claimer);",
        "                _possessingAgentId = claimer; // measurement legacy",
        "loose pickup",
    )

p.write_text(s, encoding="utf-8", newline="\n")

# Reuse the paired per-seed driver, then constrain the corpus to the known failing seed
# so all ablations can run in parallel at half the two-seed cost.
q = repo / "src/match-engine/tests/MatchEngineInPossGateScenarios.cs"
t = q.read_text(encoding="utf-8")
second = "            0x1A2B3C4D5E6F7081UL,\n"
if t.count(second) != 1:
    raise SystemExit(f"seed anchor count {t.count(second)}")
t = t.replace(second, "", 1)
# The one-seed healthy baseline has ~14k final-third samples, so lower only the
# non-vacuity instrument floor for this causal probe; the possession bound remains untouched.
t = t.replace("totalSamples >= 20000", "totalSamples >= 10000", 1)
q.write_text(t, encoding="utf-8", newline="\n")
