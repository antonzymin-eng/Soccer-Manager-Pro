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
- FR-DS-010 -> T-DS-ENV-007: EnvironmentFingerprint recorded at match start; mid-match mutation rejected.
- FR-DS-011 -> T-DS-FAULT-008: Tier-B field without tolerance row fails with `ERR_DS_TIERB_TOLERANCE_MISSING`.
- FR-DS-012 -> T-DS-FAULT-009: out-of-order or skipped replay lifecycle step fails deterministically.
- FR-DS-013 -> T-DS-ENV-010: float Tier-A field without environment pinning fails classification gate.

## 5.3 Test Catalogue
- **T-DS-ORDER-001:** same input trace => identical phase digest sequence.
- **T-DS-RNG-002:** conditional branches preserve stream cursor equality.
- **T-DS-SNAP-003:** byte-roundtrip idempotence for canonical schema.
- **T-DS-REPLAY-004:** randomized checkpoint replay equivalence.
- **T-DS-SAVE-005:** corrupt/incompatible save fails deterministically.
- **T-DS-DIFF-006:** injected faults map to correct divergence class.
- **T-DS-ENV-007:** EnvironmentFingerprint recorded at match start; mid-match mutation triggers `ERR_DS_REPLAY_ENV_MISMATCH` on subsequent replay.
- **T-DS-FAULT-008:** Tier-B digest-scope field with no tolerance row triggers `ERR_DS_TIERB_TOLERANCE_MISSING`; no silent epsilon substitution occurs.
- **T-DS-FAULT-009:** Corrupted `prevSnapshotDigest` on replay load triggers `ERR_DS_DIGEST_CHAIN_BREAK` before rehydration; authoritative state is not mutated.

## 5.4 Determinism and Numerical Verification
- Tier A fields use bitwise equality only.
- Tier B fields use approved comparator/tolerance rows only.
- Tier C mismatches are ignored by authoritative pass/fail gates.
- Certification result requires zero Tier A mismatches and zero out-of-bound Tier B mismatches.

## 5.5 Certification Matrix (minimum)
| Stage | Platform | OS / runtime | Build | Compiler mode | Required result |
|---|---|---|---|---|---|
| 0 | Windows x64 (developer host) | Windows 10/11, Unity 2022 LTS, Mono/IL2CPP per project default | Release | Deterministic flags (denormals-are-zero off, fp-contract off, fma off unless platform-pinned) | PASS |
| 5+ | Windows x64 | Windows 10/11, Unity 2022 LTS, IL2CPP | Release | Deterministic flags | PASS |
| 5+ | Linux x64 | Ubuntu 22.04 LTS, Unity 2022 LTS, IL2CPP | Release | Deterministic flags | PASS |
| 5+ | macOS ARM64 | macOS 13+, Unity 2022 LTS, IL2CPP | Release | Deterministic flags | PASS |

**FR-DS-009-GATE:**
- **Stage 0:** any hard desync on the Stage 0 host platform (Windows x64, Unity 2022 LTS) MUST block the release candidate. Cross-platform parity is NOT a Stage 0 gate (per CLAUDE.md).
- **Stage 5+:** any hard desync on any certified platform in the matrix MUST block the release candidate.

The exact Stage 0 host platform tuple (OS version, Unity LTS revision, IL2CPP version, compiler flag set) MUST be pinned in `docs/tracking/certification-platform.md` before the first certification run; Section 5.5 lists the platform family but the version pin is a separate operational artifact.

## 5.6 Version History
- **v0.7 (May 2, 2026):** Stage 0 host platform pinned (Windows x64, Unity 2022 LTS); FR-DS-009-GATE split by stage with explicit "Stage 0 cross-platform NOT a gate" note. Digest rollup ordering bound to canonical (tick, phaseOrdinal) sort.
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

Digest rollup algorithm: `SHA-256(concat(phaseDigest[i] for i in canonical (tick, phaseOrdinal) order))` where `tick` is ascending and `phaseOrdinal` follows the canonical pipeline order from §3.1.2 (`Input=0, Intent=1, AI=2, Physics=3, Resolve=4, Events=5, Snapshot=6`). `AI_NoOp` shares ordinal `2` — it produces an empty-scope phase digest at index `2` on non-stride ticks, preserving the rollup shape. Full sequence stored as artifact.

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

### 5.11.4 T-DS-ENV-007
- FRMappings: FR-DS-010, FR-DS-012.
- Preconditions: recording-side and replay-side runtimes with different `workerCount` or `simdFeatureLevel`.
- Steps: (a) Record a match snapshot on runtime A (fingerprint A). (b) Attempt replay on runtime B with a different `EnvironmentFingerprint`. (c) Verify §4.2.2 step 3 fails before rehydration.
- Pass: `ERR_DS_REPLAY_ENV_MISMATCH` returned at step 3; authoritative state unchanged from pre-load state.
- Fail: replay proceeds past step 3 or no error is returned.

### 5.11.5 T-DS-FAULT-008
- FRMappings: FR-DS-011.
- Preconditions: tolerance matrix contains no entry for `agents[0].analyticsScore` (a Tier-B field); field appears in digest scope.
- Steps: attempt digest computation when the undeclared Tier-B field is in scope.
- Pass: `ERR_DS_TIERB_TOLERANCE_MISSING` returned; no digest is produced; no fallback epsilon is applied.
- Fail: digest computation proceeds with any assumed tolerance value.

### 5.11.6 T-DS-FAULT-009
- FRMappings: FR-DS-012.
- Preconditions: valid snapshot at tick T exists; `prevSnapshotDigest` header field is flipped by one bit.
- Steps: initiate replay resume from the corrupted snapshot.
- Pass: `ERR_DS_DIGEST_CHAIN_BREAK` returned at §4.2.2 step 4; authoritative state is not rehydrated (step 5 is not reached).
- Fail: rehydration proceeds or a non-deterministic error is emitted.

## 5.12 Certification Evidence Requirements
Certification report MUST include:
1. platform/build matrix,
2. pass/fail per scenario,
3. first divergence output for each failure,
4. artifact retention pointers,
5. owner acknowledgement and disposition.

## 5.13 Version History
- **v0.8 (May 2, 2026):** Added FR-DS-010..013 traceability rows to §5.2 (B-8). Added T-DS-ENV-007, T-DS-FAULT-008, T-DS-FAULT-009 to §5.3 catalogue and §5.11 expanded test cards (B-9). Added AI_NoOp ordinal-2 note to §5.10 (A-1).
- **v0.6:** Added normative test card template and expanded concrete test card examples.
