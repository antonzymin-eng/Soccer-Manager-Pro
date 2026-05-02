# Deterministic Simulation Specification #16 — Refined Outline (Post-Adversarial Pass)

## 0. Document Metadata
- **Spec ID:** 16
- **Title:** Deterministic Simulation
- **Status:** Draft outline (refined); section files now superseded at v0.7 (see individual section version histories)
- **Version:** 0.4 (May 2, 2026 — prefix unification: `FR-DET-`/`VR-DET-`/`OPS-DET-` deprecated in favor of `-DS-` family per §2.0 of section-2.md)
- **Primary audience:** Gameplay engineering, physics engineering, AI systems, tooling, QA automation, build/release
- **Normative keywords:** MUST, MUST NOT, SHOULD, SHOULD NOT, MAY

---

## 1. Purpose
Define mandatory deterministic contracts for runtime execution, persistence, replay, and cross-platform validation so independent implementations produce equivalent authoritative outcomes.

### 1.1 Determinism guarantees (normative)
1. **Run-to-run:** Same build hash, initial state, seed bundle, and authoritative inputs MUST produce equivalent state/event outputs.
2. **Replay:** Replaying the authoritative input log from tick 0 or any valid checkpoint tick MUST reproduce the same output stream.
3. **Save/load equivalence:** Load at tick `T` then continue MUST match uninterrupted simulation from tick `T` onward.
4. **Cross-platform certification:** Supported target matrix MUST pass certification corpus under defined parity policy.

### 1.2 Determinism tiers (frozen)
- **Tier A (authoritative hard):** bitwise exact equality required.
- **Tier B (bounded-authoritative):** deterministic epsilon policy allowed only for approved fields in the tolerance matrix.
- **Tier C (non-authoritative):** may vary; MUST NOT feed back into authoritative state.

### 1.3 Tier mapping policy
- World state, gameplay state machines, event ledgers, and RNG counters: **Tier A**.
- Whitelisted numeric fields with approved tolerance rows: **Tier B**.
- VFX/UI/audio-only fields: **Tier C**.

---

## 2. Scope
This spec covers:
- Canonical per-tick order and intra-phase ordering.
- RNG ownership, stream derivation, and branch-safe consumption.
- Snapshot/replay binary contract.
- Divergence detection and digest protocol.
- Save/load atomicity and equivalence validation.
- Regression suite governance and cross-platform certification.

Out of scope:
- Gameplay tuning values.
- Rendering-only randomness that cannot affect authoritative state.

---

## 3. Section 1 — Authoritative Tick Order (Refined)

### 3.1 Canonical tick pipeline
`Input -> Intent -> AI -> Physics -> Resolve -> Events -> Snapshot`

### 3.2 Intra-phase deterministic ordering contract (new)
Every phase MUST define:
1. Primary sort key: `EntityId` ascending.
2. Secondary key: subsystem-specific deterministic ordinal.
3. Tertiary key: insertion sequence captured in deterministic queue index.

Iteration over unordered containers is forbidden in authoritative paths unless entries are copied to a deterministically sorted buffer first.

### 3.3 Job-system merge determinism (new)
- Parallel jobs MAY execute in any order internally, but merge MUST occur at deterministic barriers.
- Merge outputs MUST be reduced in canonical key order.
- Floating-point reductions MUST use deterministic reduction topology (fixed tree shape), or be marked Tier B with explicit tolerance approval.

### 3.4 Required artifacts
- Canonical per-tick pseudocode.
- Phase contract table (`ReadSet`, `WriteSet`, `Owner`, `AllowedSideEffects`).
- Deterministic ordering table for each authoritative collection.

### 3.5 Acceptance checks
- Same input trace => identical phase digest sequence.
- Mutation outside owning phase => validation failure.

---

## 4. Section 2 — RNG Policy (Refined)

### 4.1 RNG ownership
- All authoritative random draws MUST route through `DeterministicRngService`.
- Direct use of `System.Random`, engine RNG, wall-clock time, or hardware entropy is forbidden in authoritative code.

### 4.2 Stream derivation contract
Per-stream key:
`(matchSeed, subsystemId, entityId, actionOrdinal, streamVersion)`

### 4.3 Branch-safe draw normalization (new)
To prevent call-count drift:
- Authoritative code MUST predeclare draw budget per decision site, or
- Use reservation API that advances stream by fixed count regardless of branch.

Required API examples:
- `Reserve(siteId, count)`
- `DrawReserved(siteId, index)`
- `Skip(siteId, count)`

### 4.4 RNG snapshot contract
Snapshot MUST include stream cursor/counter per active stream.

### 4.5 Required artifacts
- RNG pseudocode and fixed vector corpus.
- Draw-site registry with stable IDs.
- Lint rule specification banning non-authoritative RNG usage.

### 4.6 Acceptance checks
- Branch-variant scripts preserve global cursor parity.
- Replay after load reproduces identical draw ledger.

---

## 5. Section 3 — Snapshot Schema & Replay Reconstruction (Refined)

### 5.1 Canonical binary layout (new)
Snapshot schema MUST freeze:
- Field order (lexically by schema table order).
- Field width and signedness.
- Endianness (single canonical endianness).
- Padding/alignment policy (explicitly none unless declared field).
- Enum encoding policy.
- Float special-value handling (NaN/Inf policy) or prohibition.

### 5.2 Snapshot chain integrity (new)
Each snapshot record MUST include:
- `SchemaVersion`
- `Tick`
- `PrevSnapshotDigest`
- `CurrentSnapshotDigest`

### 5.3 Replay reconstruction state machine (new)
Define lifecycle:
1. Load snapshot bytes.
2. Validate schema/digest chain.
3. Rehydrate authoritative state only.
4. Restore RNG cursors.
5. Resume at next legal phase boundary.
6. Reapply authoritative inputs from `T+1`.

### 5.4 Mid-tick save rule
Mid-tick save is forbidden unless normalized to a legal boundary with explicit phase marker.

### 5.5 Required artifacts
- Schema table and binary examples.
- Replay boot sequence pseudocode.
- Compatibility matrix and migration strategy.

### 5.6 Acceptance checks
- Byte-roundtrip idempotence.
- Checkpoint replay equivalence across randomized tick set.

---

## 6. Section 4 — Tolerances & Divergence Detection (Refined)

### 6.1 Mandatory tolerance matrix format
Each field path MUST specify: `Tier`, `Comparator`, `ToleranceValue`, `Rationale`, `Owner`, `ReviewDate`.

### 6.2 Deterministic digest protocol (new)
Freeze:
- Digest algorithm and version.
- Canonical field serialization order.
- Per-phase digest scopes.
- Collision response workflow.

### 6.3 Divergence taxonomy
- **Hard desync:** Tier A mismatch.
- **Soft drift:** Tier B out-of-bound mismatch.
- **Cosmetic divergence:** Tier C mismatch only.

### 6.4 First-divergence localization
Tooling MUST emit:
- First divergent tick.
- First divergent phase.
- Field path(s) and comparator deltas.
- RNG cursor diff if present.

### 6.5 Acceptance checks
- Fault injection triggers expected divergence class.
- Bisect tooling isolates first bad change in CI.

---

## 7. Section 5 — Save/Load Equivalence (Refined)

### 7.1 Atomic save transaction
Save MUST capture all authoritative domains in one atomic transaction or fail with deterministic error code.

### 7.2 Equivalence protocol (new)
Minimum validation protocol MUST define:
- Scenario corpus size.
- Randomized checkpoint count per scenario.
- Required pass percentage.
- Required digest equality scope.

### 7.3 Failure behavior
Corrupt/incompatible saves MUST fail deterministically with explicit error IDs and no partial rehydration.

---

## 8. Section 6 — Instrumentation & Desync Debugging (Refined)

### 8.1 Trace channels
- Tick header
- Phase digest stream
- Event ledger
- RNG draw ledger
- Snapshot digest chain

### 8.2 Performance and storage budgets (new)
Spec MUST set:
- Max CPU overhead per verbosity tier.
- Max memory overhead.
- Max artifact size per match.
- CI retention duration and compression policy.

### 8.3 Triage workflow
`record -> replay -> compare -> locate first divergence -> classify -> route owner`

### 8.4 Required artifacts
- Trace schema.
- CLI command reference.
- Incident bundle format.

---

## 9. Section 7 — Determinism Regression Suite (Refined)

### 9.1 Test architecture
- Unit: RNG, hashing, serialization codecs.
- Integration: subsystem phase determinism.
- Scenario: full-match deterministic traces.
- Soak: long-horizon drift detection.

### 9.2 Golden trace governance (new)
Golden updates MUST include:
- Change reason tag.
- Before/after divergence report.
- Reviewer approvals from owning subsystem + determinism owner.
- Rollback plan if production drift is detected.

### 9.3 CI gating
Any hard desync in protected branches is blocking.

---

## 10. Section 8 — Cross-Platform Certification (Refined)

### 10.1 Certification matrix
Freeze supported: OS, CPU arch, compiler/runtime versions, build flags.

### 10.2 Objective pass criteria (new)
- Tier A fields: exact match required.
- Tier B fields: must pass tolerance rows with zero unauthorized fields.
- Tier C fields: ignored for authoritative parity.

### 10.3 Re-certification triggers
Runtime upgrade, compiler update, numeric-library change, architecture addition, or deterministic core refactor.

### 10.4 Required artifacts
- Signed certification report.
- Exception register with owner + expiry.

---

## 11. Section 9 — Approval Checklist (Refined)
Approval gates MUST be objective and evidence-backed:
- Architecture contracts frozen and linked.
- RNG/vector corpus approved.
- Snapshot schema + digest protocol frozen.
- Regression suite and certification matrix passing.
- Tooling and runbooks operational.
- Open exceptions explicitly approved with expiry.

Outcome:
- Approved / Approved with bounded exceptions / Rejected.

---

## 12. Requirement ID Seed Set (New)
- `FR-DS-001`: canonical tick pipeline
- `FR-DS-002`: intra-phase ordering keys
- `FR-DS-003`: authoritative RNG ownership
- `FR-DS-004`: branch-safe RNG normalization
- `FR-DS-005`: snapshot canonical binary schema
- `FR-DS-006`: replay reconstruction state machine
- `FR-DS-007`: deterministic digest protocol
- `FR-DS-008`: save/load equivalence protocol
- `VR-DS-001`: phase digest determinism tests
- `VR-DS-002`: checkpoint replay equivalence suite
- `VR-DS-003`: cross-platform certification corpus
- `OPS-DS-001`: golden trace governance workflow

---

## 13. Open Questions (Narrowed)
1. Stage-0 float paths: which fields remain Tier B and for how long?
2. Fixed64 transition trigger and coexistence plan.
3. Snapshot cadence defaults vs storage budgets.
4. CI budget for full certification matrix frequency.
