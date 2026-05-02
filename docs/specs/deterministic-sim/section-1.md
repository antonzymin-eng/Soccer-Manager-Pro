# Deterministic Simulation Specification #16 — Section 1: Purpose & Scope

## 1.1 What This Specification Covers
This specification defines mandatory deterministic contracts for runtime execution, persistence, replay, and cross-platform validation.

Deterministic guarantees in scope:
1. **Run-to-run equivalence:** Same build hash, initial state, seed bundle, and authoritative input log MUST produce equivalent authoritative outputs.
2. **Replay equivalence:** Replaying authoritative inputs from tick `0` or from a valid checkpoint tick MUST reproduce the same authoritative state/event sequence.
3. **Save/load equivalence:** Loading at tick `T` then continuing simulation MUST match uninterrupted simulation from tick `T` onward.
4. **Cross-platform certification:** Stage 0 requires single-platform/single-build certification only; cross-platform certification activates at Stage 5+ under the defined parity policy.

### 1.1.1 Equivalence policy by artifact
| Artifact | Equality mode | Enforcement |
|---|---|---|
| Authoritative world state | Tier A bitwise | runtime + CI |
| Event ledger | Tier A bitwise | replay + certification |
| RNG cursor/counter | Tier A bitwise | replay + save/load |
| Approved numeric fields | Tier B bounded comparator | CI tolerance gate |
| UI/VFX/audio-only | Tier C unconstrained | non-authoritative only |
### 1.1.2 Stage gating policy
- Stage 0 release gate: same machine, same build, same seed/input parity.
- Stage 5+ release gate: cross-platform parity matrix with approved Tier policy.

## 1.2 What Is Out of Scope
Out of scope for deterministic guarantees:
- Gameplay balance/tuning values (covered by feature specs).
- Rendering-only randomness and presentation-only state.
- UI, audio, and VFX fields that do not feed authoritative simulation.

### 1.2.1 Guardrail
Any field initially designated out-of-scope that later influences authoritative branching MUST be reclassified into Tier A/B before release.

## 1.3 Key Design Decisions
### 1.3.1 Determinism tiers
- **Tier A (authoritative hard):** bitwise exact equality required.
- **Tier B (bounded-authoritative):** deterministic epsilon policy allowed only for fields explicitly approved in tolerance matrix.
- **Tier C (non-authoritative):** allowed to vary; MUST NOT feed authoritative state.

### 1.3.2 Tier mapping policy
- World state, gameplay state machines, event ledgers, and RNG counters are **Tier A**.
- Whitelisted numeric fields with approved tolerance rows are **Tier B**.
- VFX/UI/audio-only fields are **Tier C**.

### 1.3.3 Canonical tick pipeline
`Input -> Intent -> AI -> Physics -> Resolve -> Events -> Snapshot`

Each phase MUST expose `ReadSet`, `WriteSet`, and deterministic ordering rules.

### 1.3.4 Release gate policy
A release candidate MUST be rejected if any of the following occur in certification corpus:
- any Tier A mismatch,
- any Tier B mismatch beyond approved tolerance,
- missing trace artifacts required for first-divergence localization.

## 1.4 Dependencies and Integration Contracts
This spec integrates with and constrains:
- **RNG subsystem:** all authoritative randomness routed via `DeterministicRngService`.
- **Serialization/snapshot subsystem:** canonical schema, digest chain, compatibility policy.
- **Event system:** authoritative ledger ordering and replay contract.
- **Testing infrastructure:** deterministic corpus, replay harness, cross-platform matrix.
- **CI/CD gates:** desync detection, first-divergence localization, regression bisect.

Normative integration requirement: no subsystem MAY introduce non-deterministic inputs (wall-clock time, unordered iteration, platform-dependent entropy) into authoritative paths.

### 1.4.1 Integration responsibilities
| Subsystem | Owner | Determinism responsibility |
|---|---|---|
| Tick orchestrator | Gameplay engineering | phase order + ownership guards |
| RNG service | Systems engineering | stream derivation + cursor parity |
| Snapshot codec | Core runtime | canonical bytes + schema compatibility |
| Replay runtime | QA/runtime tooling | reconstruction + input reapplication |
| Desync analyzer | QA automation | first-diff localization + taxonomy |

## 1.5 Version History
- **v0.5:** Expanded scope section with equivalence tables, release gates, and subsystem responsibility matrix.
- **v0.3:** Draft aligned to refined post-adversarial outline; determinism tiers, replay/save-load equivalence, and cross-platform certification scope frozen.

## 1.6 Operational Scenarios
### 1.6.1 Run-to-run parity scenario
Given:
- identical build hash,
- identical seed bundle,
- identical authoritative input log.

Expected:
- all per-phase digests identical,
- all event sequence IDs identical,
- all Tier A fields identical at each checkpoint.

### 1.6.2 Save/load equivalence scenario
Given a save at tick `T=3600`, loading and continuing to `T=5400` MUST match uninterrupted run from `T=3600` to `T=5400` under identical inputs.

## 1.7 Governance Artifacts Required for Approval
| Artifact | Required owner | Minimum content |
|---|---|---|
| Tier matrix | Simulation lead | all Tier A/B/C field paths |
| Replay policy | Runtime lead | boot sequence + rollback behavior |
| Certification corpus list | QA automation | scenario IDs + coverage rationale |
| Error catalog | Systems lead | deterministic error IDs + meanings |

## 1.8 Anti-Patterns (Forbidden)
- Coupling authoritative logic to frame-rate timing jitter.
- Reading unordered map/set directly in authoritative phases.
- Introducing transient debug-only fields into digest scope without version bump.
- Mutating authoritative state from background jobs outside merge barriers.
