# Deterministic Simulation Specification #16 — Section 2: System Overview

## 2.0 Identifier Taxonomy
This spec uses a single prefix family for internal traceability. Cross-spec references continue to use the CLAUDE.md taxonomy (`XC-`, `FM-`, `EC-`, `ERR-`).

| Prefix | Scope | Example |
|---|---|---|
| `FR-DS-NNN` | Functional requirement (this spec) | `FR-DS-003` |
| `VR-DS-NNN` | Verification requirement (this spec) | `VR-DS-001` |
| `OPS-DS-NNN` | Operational/governance requirement | `OPS-DS-001` |
| `T-DS-<area>-NNN` | Test card | `T-DS-ORDER-001` |
| `GV-<area>-NNN` | Golden vector | `GV-RNG-001` |
| `XC-NNN` | Cross-spec reference (CLAUDE.md taxonomy) | `XC-016-001` |
| `FM-NNN` | Formula reference (CLAUDE.md taxonomy) | `FM-016-001` |
| `EC-NNN` | Edge-case reference (CLAUDE.md taxonomy) | `EC-016-001` |
| `ERR-NNN` | Spec Error Log entry (CLAUDE.md taxonomy) | `ERR-016-001` |

The earlier `FR-DET-` / `VR-DET-` / `OPS-DET-` outline prefixes are deprecated and have been superseded by the `-DS-` family.

## 2.1 Functional Requirements
- **FR-DS-001:** Simulation MUST execute with canonical per-tick phase order.
- **FR-DS-002:** Authoritative intra-phase iteration MUST use deterministic key order.
- **FR-DS-003:** Authoritative random draws MUST use deterministic stream derivation and branch-safe consumption.
- **FR-DS-004:** Snapshot serialization MUST follow canonical binary layout and schema versioning.
- **FR-DS-005:** Replay engine MUST reconstruct deterministic state from snapshot + authoritative inputs.
- **FR-DS-006:** Save/load transaction MUST be atomic across all authoritative domains.
- **FR-DS-007:** Divergence detection MUST classify mismatches as hard desync, soft drift, or cosmetic divergence.
- **FR-DS-008:** Tooling MUST emit first divergent tick/phase/field and RNG cursor diffs.
- **FR-DS-009:** Cross-platform certification suite MUST pass before release.
- **FR-DS-010:** At match start the runtime MUST capture `EnvironmentFingerprint` (worker count, scheduler policy, reduction topology, SIMD level, float-model hash) and embed it in every snapshot header for that match; mid-match mutation of any pinned field is forbidden.
- **FR-DS-011:** A Tier-B field present in digest scope without an approved tolerance row in the tolerance matrix MUST fail validation with `ERR_DS_TIERB_TOLERANCE_MISSING`; no silent fallback epsilon is permitted.
- **FR-DS-012:** The replay engine MUST execute the 8-step lifecycle (§4.2.2) in strict order; each step MUST fail deterministically with its assigned error code and MUST NOT proceed to the next step on failure.
- **FR-DS-013:** Stage-0 `float` fields classified Tier-A MUST satisfy both §1.3.1.1 conditions (pinned execution environment recorded in `EnvironmentFingerprint`, and deterministic reduction topology); fields that cannot satisfy these conditions MUST be classified Tier-B with an approved tolerance row.

## 2.2 Architecture Overview
Core components:
1. **Tick Orchestrator:** enforces phase graph and legal phase boundaries.
2. **DeterministicRngService:** owns stream derivation, cursor management, reservation API.
3. **Authoritative State Store:** Tier A/B data, ownership metadata, mutation guards.
4. **Snapshot Codec:** canonical binary encoding + digest chain generation.
5. **Replay Runtime:** snapshot load, validation, rehydration, input reapplication.
6. **Determinism Auditor:** per-phase digest generation, diffing, classification.

### 2.2.1 Runtime sequence example
| Step | Action | Required invariant |
|---|---|---|
| 1 | Load authoritative input frame | input timestamp corresponds to current tick |
| 2 | Execute canonical phases | phase order cannot branch |
| 3 | Emit event ledger | sequence IDs monotonically increase |
| 4 | Compute phase digests | canonical serialization order frozen |
| 5 | Save snapshot (if scheduled) | digest chain links to previous snapshot |

## 2.3 Data Structures
- `DeterminismContext { buildHash, matchSeed, schemaVersion, digestVersion }`
- `PhaseDigest { tick, phaseId, digest }`
- `RngStreamKey { subsystemId, entityId, streamVersion }` (persistent per-(subsystem, entity, version); see §3.2.1)
- `RngCursor { streamKey, counter, actionOrdinal }` (per-stream draw counter and reservation index; see §3.2.5)
- `SnapshotHeader { schemaVersion, tick, prevSnapshotDigest, environmentFingerprint }` (see §4.8 for fingerprint contents)
- `ToleranceRow { fieldPath, tier, comparator, toleranceValue, rationale, owner, reviewDate }`
- `ComparatorRegistry = { BitwiseEqual, AbsEpsilon, RelEpsilon }` (normative v1)

### 2.3.1 Tolerance row operational schema
| Column | Type | Rule |
|---|---|---|
| `fieldPath` | string | stable dotted path, immutable once published |
| `tier` | enum | `A`, `B`, `C` only |
| `comparator` | enum | from approved comparator registry |
| `toleranceValue` | fixed decimal | required when tier = `B` |
| `rationale` | string | mandatory for tier != `A` |
| `owner` | string | team alias |
| `reviewDate` | date | must be <= 180 days old |

## 2.4 Failure Modes and Recovery
- **Non-canonical ordering detected:** fail fast with deterministic error ID; reject tick commit.
- **RNG policy violation:** fail lint/build gate if direct non-authoritative RNG use detected.
- **Snapshot schema mismatch:** deterministic load failure; no partial rehydration.
- **Digest chain break:** reject replay source; emit integrity error.
- **Hard desync in certification:** block release candidate and trigger bisect workflow.

### 2.4.1 Recovery behavior matrix
| Failure | Runtime behavior | CI behavior |
|---|---|---|
| schema mismatch | abort load, retain pre-load state | fail test job |
| digest chain break | abort replay | fail certification suite |
| Tier A mismatch | halt comparative replay | open blocker issue automatically |
| Tier B drift | continue replay with warning | fail if out-of-bound |

## 2.5 Version History
- **v0.8 (May 2, 2026):** Added FR-DS-010..013: EnvironmentFingerprint recording, Tier-B tolerance enforcement, replay 8-step lifecycle, Stage-0 float Tier-A classification gate (B-8).
- **v0.7 (May 2, 2026):** Added §2.0 Identifier Taxonomy; corrected `RngStreamKey` (removed `actionOrdinal` from key) and extended `RngCursor` (added `actionOrdinal`); extended `SnapshotHeader` with `environmentFingerprint`.
- **v0.5:** Added runtime sequence, tolerance schema, and failure recovery matrix.
- **v0.3:** Added explicit FR set tied to refined outline and determinism governance.

## 2.6 End-to-End Data Flow Example
### 2.6.1 Tick lifecycle with checkpoints
1. Tick begins with deterministic context.
2. Authoritative input frame accepted and validated.
3. Phase pipeline executes with ordering constraints.
4. Event ledger commits ordered entries.
5. Digest stream records phase digests.
6. Optional snapshot emits header + payload + digest.

### 2.6.2 Replay lifecycle with checkpoint resume
1. Load snapshot and validate schema version.
2. Validate digest chain against previous snapshot.
3. Restore authoritative state + RNG cursors.
4. Reapply input log from `T+1`.
5. Compare phase digests and classify divergence if found.

## 2.7 Ownership and Escalation Matrix
| Domain | Primary owner | Backup owner | Escalation trigger |
|---|---|---|---|
| Tick order | Gameplay systems | Runtime systems | phase order mismatch |
| RNG service | Runtime systems | Gameplay systems | cursor drift |
| Snapshot codec | Runtime systems | Tools team | schema mismatch |
| Certification harness | QA automation | Release engineering | hard desync in CI |
