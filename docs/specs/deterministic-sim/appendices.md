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
Fixed draw budget per reservation ensures branch-dependent control flow cannot alter per-stream cursor parity. The reservation budget — not the number of draws actually consumed — is what advances the cursor.

Worked example (stream `(AI, entity=42, v1)`, initial `RngCursor=200`, `actionOrdinal=11`):
- Site `AI.DecidePass` reserves 3 draws (`Reserve("AI.DecidePass", 3)`).
- Fast branch: `DrawReserved(0)` then `Skip(2)`.
- Slow branch: `DrawReserved(0..2)`.
- Both branches end with `RngCursor=203`, `actionOrdinal=12`.

See §3.2.5 for the formal cursor model and §3.7 for the full pseudocode form.

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
| Vector ID | Purpose | FR mapping | Test card(s) | Artifact path | Expected result |
|---|---|---|---|---|---|
| GV-RNG-001 | branch-safe parity | FR-DS-003 | T-DS-RNG-002 | (stub — to be authored alongside `siphash-2-4-kat.md`) | identical end cursors across branch variants |
| GV-SNAP-001 | snapshot roundtrip | FR-DS-004, FR-DS-006 | T-DS-SNAP-003, T-DS-SAVE-005 | (stub) | byte-identical payload across `Serialize ∘ Deserialize ∘ Serialize` |
| GV-DIGEST-001 | phase digest parity | FR-DS-007, FR-DS-008 | T-DS-ORDER-001 | (stub) | identical digest stream from identical input log |
| GV-HKDF-001 | RFC 5869 HKDF-SHA256 KAT (RFC §A.1–A.3) | FR-DS-003 | T-DS-RNG-002 | `docs/specs/deterministic-sim/golden-vectors/hkdf-sha256-kat.md` (§9.5 #4(a)) | every RFC vector reproduced bit-exact + `info="DS-RNG-KEY-v1"` row + `salt=NULL` row |
| GV-SIPHASH-001 | SipHash-2-4 reference vectors (Aumasson & Bernstein 2012 App. A) | FR-DS-003 | T-DS-RNG-002 | `docs/specs/deterministic-sim/golden-vectors/siphash-2-4-kat.md` (§9.5 #4(b)) | all 64 reference vectors reproduced bit-exact |
| GV-CANON-001 | `SerializeCanonical` reference corpus per §3.2.4.1 | FR-DS-004, FR-DS-007 | T-DS-ORDER-001, T-DS-SNAP-003 | `docs/specs/deterministic-sim/golden-vectors/serialize-canonical-corpus.md` (§9.5 #4(c)) | every (input record → expected SHA-256) tuple reproduced bit-exact, including the §3.2.4.1 12-byte `PhaseDigest` worked example, the new §3.2.3 `SnapshotDigest` worked example with declared field order, and `-0.0`/`+0.0` Tier-A normalization fixtures |

The starter rows GV-RNG-001 / GV-SNAP-001 / GV-DIGEST-001 (above the line) are scenario-level vectors. The lower three rows (GV-HKDF-001 / GV-SIPHASH-001 / GV-CANON-001) are the §9.5 acceptance criterion #4 implementation-conformance corpora and are gating for promotion to IN REVIEW. (Pass 4 L-6, Pass 5 L-5.)

The Test Card Template (§5.10) MUST add a `GoldenVectors : array<GV-ID>` field so test cards declare which vectors they consume; the starter mapping above is the initial population of that field.
