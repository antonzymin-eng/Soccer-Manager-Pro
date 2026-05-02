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
- `ITickOrchestrator.RunTick(inputFrame)` MUST execute fixed phase order.
- `IDeterministicRngService` MUST provide stream derivation + reservation semantics.
- `ISnapshotCodec.Serialize/Deserialize` MUST honor canonical layout.
- `IReplayRunner.ResumeFrom(snapshot, inputLog)` MUST restore only authoritative state and continue at legal boundary.
- `IDesyncAnalyzer.Compare(traceA, traceB)` MUST report first-divergence location and taxonomy.

### 4.2.1 Contract invariants
- `RunTick` is pure relative to `(previousAuthoritativeStateIncludingRngAndContext, authoritativeInputFrame, tickNumber)`.
- `Serialize` MUST be byte-idempotent for equal in-memory authoritative state.
- `ResumeFrom` MUST restore RNG cursors before applying `T+1` input.
- `Compare` MUST return deterministic ordering for field-path differences.

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
## 4.8 Replay pre-resume environment pinning
Before resume, runtime MUST pin worker count, scheduler policy, and reduction topology fingerprint from snapshot metadata. Mismatch MUST fail deterministically with `ERR_DS_REPLAY_ENV_MISMATCH`.
