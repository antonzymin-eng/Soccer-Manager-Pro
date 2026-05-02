# Deterministic Simulation Specification #16 — Section 4: Architecture & Integration

## 4.1 System Boundaries
Authoritative boundary includes:
- gameplay state machines,
- physics authoritative state,
- event ledger,
- RNG cursors/counters,
- snapshot metadata and digest chain.

Non-authoritative boundary includes UI, VFX, audio presentation layers.

## 4.2 Interface Contracts
The signatures below are **non-normative sketches** of the surface this spec requires. Per CLAUDE.md ("Write interfaces only when both sides are specified") and the project's documented phantom-interface hazard (ERR-001/004), the consumer specs (`event-system` #17, `performance-optimization` #18, `testing-strategy` #19) are still `NOT STARTED`. Final interface signatures MUST be co-authored with those specs and MUST NOT be promoted to normative `.cs` interfaces during Stage 0 spec authoring. What IS normative here is the *behavior contract* in §4.2.1, not the C# shape.

Sketches:
- `ITickOrchestrator.RunTick(inputFrame, tickNumber)` — executes fixed phase order.
- `IDeterministicRngService` — stream derivation + reservation semantics (`Reserve`, `DrawReserved`, `Skip`, cursor read/restore).
- `ISnapshotCodec.Serialize/Deserialize` — canonical layout per §3.2 / §3.9.
- `IReplayRunner` — multi-step lifecycle (see §4.2.2).
- `IDesyncAnalyzer.Compare(traceA, traceB)` — first-divergence location and taxonomy.

### 4.2.1 Behavior invariants (normative)
- `RunTick` is **deterministic** in `(prevAuthoritativeState, prevRngState, prevDeterminismContext, inputFrame, tickNumber)` and observes no ambient state (no wall-clock, no engine RNG, no unordered iteration). It is not "pure" in the functional sense — it mutates the state store — but the (input → output) mapping is deterministic.
- `Serialize` MUST be byte-idempotent for equal in-memory authoritative state, AND `Deserialize(Serialize(s)) ≡ s` (round-trip identity for the authoritative scope).
- Replay lifecycle hooks MUST run in the order in §4.2.2; partial/early resume is forbidden.
- `Compare` MUST return field-path differences in canonical sort order (see §3.1.1).

### 4.2.2 Replay lifecycle (normative)
The replay runtime MUST execute these steps in order. Each step MUST fail deterministically with the listed error code on validation failure, and MUST NOT proceed to the next step on failure.

| Step | Action | Failure error |
|---|---|---|
| 1 | Load snapshot bytes | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| 2 | Validate `schemaVersion` and `digestVersion` | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| 3 | Validate `EnvironmentFingerprint` against live runtime (§4.8) | `ERR_DS_REPLAY_ENV_MISMATCH` |
| 4 | Validate `prevSnapshotDigest` chain link to expected predecessor | `ERR_DS_DIGEST_CHAIN_BREAK` |
| 5 | Rehydrate authoritative state (Tier A + Tier B fields only; Tier C excluded) | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| 6 | Restore RNG cursors and `actionOrdinal` per stream (§3.2.5); fail if any required stream is missing | `ERR_DS_RNG_STREAM_MISSING` |
| 7 | Position at next legal phase boundary (§3.4 `LEGAL_SAVE_BOUNDARIES`) | `ERR_DS_SAVE_BOUNDARY` |
| 8 | Reapply authoritative input log from `T+1` | (propagates from `RunTick`) |

Side-effects on non-authoritative subsystems (UI, audio, VFX, telemetry) MUST NOT be triggered during steps 1–7.

## 4.3 Event Interactions
Events MUST be emitted and committed in canonical order at `Events` phase.
Event payloads MUST avoid non-canonical field ordering and MUST include deterministic sequence IDs.

Required event envelope fields:
- `tick`
- `phase`
- `eventSequence`
- `producerEntityId`
- `payloadVersion`

## 4.4 File/Module Layout
Recommended module ownership:
- `sim/tick/*` — phase orchestration and ordering.
- `sim/rng/*` — deterministic RNG APIs and stream registry.
- `sim/snapshot/*` — schema table, codec, digest chain.
- `sim/replay/*` — reconstruction state machine.
- `sim/determinism/*` — digest protocol, tolerance matrix, divergence tooling.

## 4.5 Version History
- **v0.7 (May 2, 2026):** §4.2 reframed as non-normative sketches per CLAUDE.md "interfaces only when both sides are specified" rule (consumer specs #17/#18/#19 still NOT STARTED). §4.2.1 reworded `RunTick` from "pure" to "deterministic in (state, input, tickNumber) with no ambient-state observation". Added §4.2.2 normative replay lifecycle with per-step error codes. §4.8 extended to cover *recording-side* environment pinning with full `EnvironmentFingerprint` schema.
- **v0.4:** Added interface invariants and required authoritative event envelope fields.
- **v0.3:** Integration contracts frozen around deterministic service interfaces.

## 4.6 Integration Sequence Diagrams (Textual)
### 4.6.1 Save transaction sequence
`TickOrchestrator -> SnapshotCodec: Serialize(authoritativeState)`
`SnapshotCodec -> DigestService: Compute(CurrentSnapshotDigest)`
`DigestService -> SnapshotStore: CommitAtomic(header,payload,digest)`

### 4.6.2 Replay resume sequence
`ReplayRunner -> SnapshotStore: Load(T)`
`ReplayRunner -> SnapshotCodec: Deserialize`
`ReplayRunner -> RngService: RestoreCursors`
`ReplayRunner -> TickOrchestrator: ResumeAtBoundary`

## 4.7 Contract Verification Checklist
- Interface invariants are test-covered.
- Error IDs are deterministic and stable.
- Serialization schema changes require version bump and migration note.
- Event envelope version upgrades are backward compatible with replay reader.
## 4.8 Environment pinning (recording and replay)
Both recording and replay runtimes MUST pin and record the following `EnvironmentFingerprint` fields, which are part of the snapshot header (see `SnapshotHeader` in §2.3 and §3.9.2 layout):

| Field | Purpose |
|---|---|
| `workerCount` | Number of authoritative worker threads |
| `schedulerPolicy` | Scheduler identity + version (job-system fingerprint) |
| `reductionTopology` | Canonical reduction tree identifier (see §1.3.1.1) |
| `simdFeatureLevel` | Lowest-common-denominator SIMD level enabled in authoritative paths |
| `floatModelHash` | Hash of compiler/runtime float-mode flags (denormals, rounding mode, FMA on/off) |

### 4.8.1 Recording requirement
At match start, the runtime MUST capture `EnvironmentFingerprint` and embed it into every snapshot header for that match. Mid-match mutation of any pinned field is forbidden and MUST fail with `ERR_DS_REPLAY_ENV_MISMATCH`.

### 4.8.2 Replay requirement
Before resume, the replay runtime MUST compare the live `EnvironmentFingerprint` against the snapshot's recorded fingerprint. Any mismatch MUST fail deterministically with `ERR_DS_REPLAY_ENV_MISMATCH` and MUST NOT attempt fallback execution.
