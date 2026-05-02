# Deterministic Simulation Specification #16 — Section 5: Testing & Validation

## 5.1 Test Strategy
Validation layers:
1. unit tests for ordering, RNG draw parity, schema encoding.
2. integration tests for save/load and replay equivalence.
3. scenario corpus regression tests with per-phase digest checks.
4. cross-platform certification across supported target matrix.

## 5.2 FR-to-Test Traceability
- FR-DS-001/002 -> canonical tick and intra-phase ordering tests.
- FR-DS-003 -> branch-variant RNG cursor parity tests.
- FR-DS-004/005 -> snapshot roundtrip + checkpoint replay tests.
- FR-DS-006 -> atomic save transaction failure/rollback tests.
- FR-DS-007/008 -> divergence classifier and first-diff localization tests.
- FR-DS-009 -> certification suite gate.

## 5.3 Test Catalogue
- **T-DS-ORDER-001:** same input trace => identical phase digest sequence.
- **T-DS-RNG-002:** conditional branches preserve stream cursor equality.
- **T-DS-SNAP-003:** byte-roundtrip idempotence for canonical schema.
- **T-DS-REPLAY-004:** randomized checkpoint replay equivalence.
- **T-DS-SAVE-005:** corrupt/incompatible save fails deterministically.
- **T-DS-DIFF-006:** injected faults map to correct divergence class.

## 5.4 Determinism and Numerical Verification
- Tier A fields use bitwise equality only.
- Tier B fields use approved comparator/tolerance rows only.
- Tier C mismatches are ignored by authoritative pass/fail gates.
- Certification result requires zero Tier A mismatches and zero out-of-bound Tier B mismatches.

## 5.5 Certification Matrix (minimum)
| Platform | Build | Compiler mode | Required result |
|---|---|---|---|
| Stage 0 Host Platform | Release | Deterministic flags | PASS |
| Stage 5+ Windows x64 | Release | Deterministic flags | PASS |
| Stage 5+ Linux x64 | Release | Deterministic flags | PASS |
| Stage 5+ macOS ARM64 | Release | Deterministic flags | PASS |

Stage 0: hard desync on host platform is a release blocker. Stage 5+: hard desync on any certified platform is a blocker.

## 5.6 Version History
- **v0.4:** Added explicit certification matrix and release-blocking policy.
- **v0.3:** Added mandatory FR traceability and certification gates.

## 5.7 Detailed Test Fixture Requirements
| Fixture class | Purpose | Required artifacts |
|---|---|---|
| deterministic smoke | quick parity confidence | phase digests + event ledger |
| replay checkpoint | validate resume correctness | snapshot chain + input log |
| cross-platform cert | release gate | per-platform digest comparison |
| fault injection | verify taxonomy | known injected mismatches |

## 5.8 Example FR-to-Test Mapping (Expanded)
- `FR-DS-003` maps to:
  - draw-budget exhaustion test,
  - branch parity test,
  - mixed subsystem stream independence test.
- `FR-DS-006` maps to:
  - partial-write failure rollback,
  - atomic commit success path,
  - deterministic error code on storage failure.

## 5.9 Expected Test Outputs
Every determinism test MUST emit:
- first divergent tick/phase (or `NONE`),
- mismatch class,
- offending field paths,
- RNG cursor diff summary,
- replayable artifact pointers.

## 5.10 Test Card Template (Normative)
Each deterministic test case MUST include the following fields:
- `TestId`
- `FRMappings`
- `InitialStateHash`
- `SeedBundleId`
- `InputLogId`
- `ExpectedDigestRollup`
- `ExpectedDivergenceClass`
- `ArtifactPaths`

Digest rollup algorithm: `SHA-256(concat(phaseDigest[0..N]))` with full sequence stored as artifact.

## 5.11 Expanded Test Cards (Examples)
### 5.11.1 T-DS-ORDER-001
- Preconditions: identical build + seed + input log.
- Steps: execute 2 independent runs of same scenario for 7200 ticks.
- Pass: all per-phase digest values equal.
- Fail: first mismatching tick/phase emitted with field path evidence.

### 5.11.2 T-DS-RNG-002
- Preconditions: decision site registry contains stable IDs.
- Steps: execute branch-A and branch-B variants with identical reserve counts.
- Pass: terminal RNG counters equal for all authoritative streams.
- Fail: classify as `HardDesync` if Tier A counters differ.

### 5.11.3 T-DS-SAVE-005
- Preconditions: checkpoint schedule includes randomized save ticks.
- Steps: inject corruption/incompatibility cases into save blobs.
- Pass: deterministic rejection with explicit error IDs; no partial load.
- Fail: ambiguous or non-deterministic failure message.

## 5.12 Certification Evidence Requirements
Certification report MUST include:
1. platform/build matrix,
2. pass/fail per scenario,
3. first divergence output for each failure,
4. artifact retention pointers,
5. owner acknowledgement and disposition.

## 5.13 Version History
- **v0.6:** Added normative test card template and expanded concrete test card examples.
