# Deterministic Simulation Specification #16 — Appendices

## Appendix A — Derivations
### A.1 Digest scope derivation
Digest scope is derived from authoritative field registry filtered by phase ownership and tier policy.

Derivation steps:
1. Collect fields in active phase `WriteSet` and immutable `ReadSet` snapshots.
2. Exclude Tier C fields.
3. Serialize remaining fields by canonical schema order.
4. Hash bytes using approved digest version.

### A.2 RNG branch-normalization rationale
Fixed draw budget/reservation ensures branch-dependent control flow cannot alter global stream cursor parity.

Worked example:
- Site `AI.DecidePass` reserves 3 draws.
- Fast branch consumes 1 draw + 2 skips.
- Slow branch consumes all 3 draws.
- Both branches end with identical cursor advancement.

## Appendix B — Numerical Verification
### B.1 Comparator policy
- Tier A: bitwise equality.
- Tier B: approved comparator rows only.
- Tier C: excluded from authoritative pass/fail.

### B.2 Failure classification thresholds
Mismatch class is determined by tier and comparator outcome, not by absolute magnitude alone.

### B.3 Comparator examples
| Comparator | Pass condition | Example |
|---|---|---|
| `BitwiseEqual` | bytes identical | two serialized vectors exactly equal |
| `AbsEpsilon` | `abs(a-b) <= eps` | position delta <= `0.0001` |
| `RelEpsilon` | `abs(a-b) <= eps*max(1,abs(a),abs(b))` | velocity ratio within bound |

## Appendix C — Sensitivity Analysis
### C.1 Instrumentation overhead sensitivity
Trace verbosity and digest scope size are primary cost drivers.

### C.2 Replay validation sensitivity
Checkpoint density increases validation confidence but raises CI runtime; certification profile must balance both.

### C.3 Scenario sizing guidance
| Scenario class | Recommended duration | Checkpoint interval |
|---|---|---|
| smoke | 2–5 simulated minutes | every 300 ticks |
| standard | full match | every 120 ticks |
| stress | full match + overtime equivalents | every 60 ticks |

## Appendix D — Replay Failure Cookbook
### D.1 Common failure signatures
| Signature | Likely cause | First action |
|---|---|---|
| digest mismatch at first resumed tick | RNG cursor not restored | inspect cursor snapshot table |
| snapshot load failure | schema incompatibility | verify migration matrix |
| Tier B drift only | comparator threshold too tight | review tolerance row rationale |

### D.2 Investigator checklist
- confirm build hash parity,
- confirm identical input log,
- confirm schema/digest version,
- compare first divergent phase traces,
- attach minimized repro bundle.

## Appendix E — Trace Schema Example (Illustrative)
```json
{
  "tick": 2210,
  "phase": "Physics",
  "phaseDigest": "abc123...",
  "rngCursors": [{"stream":"AI.18", "counter": 492}],
  "eventCount": 3
}
```

## Appendix F — Incident Postmortem Template
### F.1 Required sections
- Incident summary
- First divergent tick/phase
- Root cause
- Reproduction steps
- Mitigation
- Preventive actions

### F.2 Example incident summary (abbreviated)
- Scenario: Cross-platform certification `CERT-DS-014`
- Divergence: Tick 4512, Phase Physics, Tier A velocity mismatch
- Root cause: non-canonical reduction order in parallel merge
- Fix: enforce canonical sorted merge at barrier
- Verification: `T-DS-ORDER-001` and certification corpus re-run passed

## Appendix G — Golden Vector Manifest (Starter)
| Vector ID | Purpose | Expected result |
|---|---|---|
| GV-RNG-001 | branch-safe parity | identical end cursors |
| GV-SNAP-001 | snapshot roundtrip | byte-identical payload |
| GV-DIGEST-001 | phase digest parity | identical digest stream |
