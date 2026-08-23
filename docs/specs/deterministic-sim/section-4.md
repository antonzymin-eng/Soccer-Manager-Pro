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
| 4a | Validate `prevSnapshotDigest` chain link to expected predecessor | `ERR_DS_DIGEST_CHAIN_BREAK` |
| 4b | Re-derive this record's OWN §3.2.3 digest from the loaded header and payload and compare it against the stored `currentSnapshotDigest` (added v1.2, `ERR-016-011`) | `ERR_DS_SNAPSHOT_DIGEST_MISMATCH` |
| 5 | Rehydrate authoritative state (Tier A + Tier B fields only; Tier C excluded) | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| 6 | Restore RNG cursors and `actionOrdinal` per stream (§3.2.5); fail if any required stream is missing | `ERR_DS_RNG_STREAM_MISSING` |
| 7 | Verify the replay cursor is at `EndOfSnapshot[T]` (the save point). The snapshot was committed at this boundary; the cursor must be exactly here before T+1 reapplication is permitted. An off-boundary cursor indicates a corrupt or partial load. | `ERR_DS_REPLAY_BOUNDARY` |
| 8 | Reapply authoritative input log from `T+1` | (propagates from `RunTick`) |

Side-effects on non-authoritative subsystems (UI, audio, VFX, telemetry) MUST NOT be triggered during steps 1–7.

**Step 4 is one step in two parts, not a ninth step** (`ERR-016-011`). The lifecycle remains the **8-step** lifecycle FR-DS-012 binds and §4.6.2 diagrams; 4a and 4b are the two halves of one integrity question and are numbered as a split so that no existing citation of a step number is invalidated. They are not redundant:

- **4a asks whether this is the record that should follow the last one** — it compares the record's declared `prevSnapshotDigest` against the expected predecessor. It says nothing about the record's own contents.
- **4b asks whether these bytes are the record they claim to be** — it recomputes `SHA-256( 0x12 ‖ schemaVersion ‖ tick ‖ prevSnapshotDigest ‖ envFpDigest ‖ 0x11 ‖ payloadBytes )` from what was just loaded and compares it against the stored `currentSnapshotDigest`. An altered payload, or an altered stored digest, passes 4a and fails 4b.

**4b MUST run before step 5.** Authoritative state that has not been verified MUST NOT be rehydrated; a lifecycle that verifies after rehydrating has already applied the bytes it was about to reject.

**The verifier and the recorder MUST compute one preimage, from one implementation.** Two hand-written derivations of the same preimage that agree only by inspection are the `ERR-010-002` defect class, and here the failure is worse in both directions: a verifier whose preimage drifts from the recorder's rejects every honest record, while a verifier that omits a field silently accepts tampering in it.

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
- **v1.2 (August 22, 2026):** `ERR-016-011`. §4.2.2 step 4 is split into **4a** (the chain link to the predecessor, unchanged) and new **4b** (re-derive the record's own §3.2.3 digest from the loaded header and payload and compare it against the stored `currentSnapshotDigest`, failing with the new `ERR_DS_SNAPSHOT_DIGEST_MISMATCH`; §3.4 / §3.10 EC-016-016). Nothing in the lifecycle had ever asked the second question, so an altered payload — the authoritative state step 5 is about to rehydrate — loaded clean. **The lifecycle stays 8 steps** (FR-DS-012, §4.6.2's diagram): 4a/4b is a split, deliberately not a renumbering, since spec renumbering cascades are this project's most recurring defect class. Added normative requirements that 4b run BEFORE step 5, and that the verifier and recorder compute one preimage from one implementation.
- **v1.1 (July 19, 2026):** ERR-016-006 (Option A, owner sign-off). §4.8.3 reconciled to the pinned Stage-0 **Mono** backend: `compilerToolchain` gains `"Mono"` (field 1); the field-4 `il2cppVersion` note flips so Stage-0 certification ACCEPTS the `"MONO"` sentinel (the reject-MONO / IL2CPP-required rules move to Stage 5+), resolving the contradiction with `certification-platform.md`; a new "Stage-0 Mono backend mapping" paragraph pins fields 1–4 under Mono and records that the live-host hasher now exists (`FloatFlagTuple.ComputeHash()` / `EnvironmentFingerprint.CreateStage0MonoCertified`). The §4.8.2 runtime MXCSR validation stays a Stage-1+/host task. Proposal: `docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md`.
- **v1.0 (May 4, 2026):** Pass 4 / Pass 5 critique resolution. (a) Pass 4 M-3: §4.8.1 mid-match mutation now fails with `ERR_DS_ENV_MUTATION` (0x160D), distinct from replay-side `ERR_DS_REPLAY_ENV_MISMATCH`; EC-016-013 paired in §3.10. (b) Pass 4 M-4: §4.8.3 `il2cppVersion` row clarified — Mono fallback uses sentinel `"MONO"` so cross-backend replay deterministically fails; certification rejects `"MONO"` snapshots. (c) Pass 4 M-5: on-disk snapshot record layout moved to §3.9.2 (normative); §4.8 fingerprint table extended with `unicodeNormalizationVersion` row (Pass 4 L-1 binding).
- **v0.9 (May 3, 2026):** Third-pass critique fixes. (a) M-I: §4.6.1.1 atomic-write contract bound (same-volume write-then-rename, fsync barrier, atomic rename, directory fsync, partial-save forbidden); paired with `ERR_DS_STORAGE_ATOMICITY`. (b) H-D: §4.8 `floatModelHash` row pointed to new §4.8.3 normative composition (SHA-256 over canonical 11-field tuple of compiler/runtime float-mode flags); Stage-0 required values listed; flag strings cross-referenced to §5.5.1.
- **v0.8 (May 2, 2026):** §4.2.2 step 7 reworded to clarify cursor-at-EndOfSnapshot[T] assertion and replaced `ERR_DS_SAVE_BOUNDARY` with `ERR_DS_REPLAY_BOUNDARY` (A-4). §4.6.2 sequence diagram replaced with 8-step diagram matching normative §4.2.2 lifecycle (A-6).
- **v0.7 (May 2, 2026):** §4.2 reframed as non-normative sketches per CLAUDE.md "interfaces only when both sides are specified" rule (consumer specs #17/#18/#19 still NOT STARTED). §4.2.1 reworded `RunTick` from "pure" to "deterministic in (state, input, tickNumber) with no ambient-state observation". Added §4.2.2 normative replay lifecycle with per-step error codes. §4.8 extended to cover *recording-side* environment pinning with full `EnvironmentFingerprint` schema.
- **v0.4:** Added interface invariants and required authoritative event envelope fields.
- **v0.3:** Integration contracts frozen around deterministic service interfaces.

## 4.6 Integration Sequence Diagrams (Textual)
### 4.6.1 Save transaction sequence
`TickOrchestrator -> SnapshotCodec: Serialize(authoritativeState)`
`SnapshotCodec -> DigestService: Compute(CurrentSnapshotDigest)`
`DigestService -> SnapshotStore: CommitAtomic(header,payload,digest)`

### 4.6.1.1 Atomic-write contract (normative)
`SnapshotStore.CommitAtomic` MUST satisfy ALL of the following on the Stage-0 host platform (Windows x64):
1. **Same-volume write-then-rename.** Payload is written to a sibling temp file on the *same filesystem volume* as the destination. Cross-volume saves are rejected with `ERR_DS_STORAGE_ATOMICITY` because `rename` is not atomic across volumes.
2. **fsync barrier before rename.** The temp file MUST be fsync'd (or `FlushFileBuffers` on Windows) to durable storage before the rename call.
3. **Atomic rename.** The temp file is renamed to the final path via POSIX `rename(2)` (Linux/macOS) or `MoveFileEx(..., MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)` (Windows). Cloud blob stores, network filesystems without atomic rename semantics, and FAT32 destinations are NOT supported substrates.
4. **Directory fsync.** On Linux/macOS, the containing directory MUST be fsync'd after rename so the directory entry is durable.
5. **Failure mode.** If any step fails, `CommitAtomic` MUST return `ERR_DS_STORAGE_ATOMICITY` and the temp file MUST be cleaned up; the prior snapshot file MUST remain untouched. Partial saves are forbidden.

Cross-platform certification (Stage 5+) extends this contract per platform; cloud-storage targets MUST be addressed under a separate atomicity strategy and are explicitly out-of-scope for Stage 0.

### 4.6.2 Replay resume sequence
Steps correspond to the normative §4.2.2 lifecycle. Each arrow fails with the listed error code; no step proceeds on failure.

```
ReplayRunner -> SnapshotStore: Load(T)                            [step 1 — ERR_DS_SCHEMA_INCOMPATIBLE on load failure]
ReplayRunner -> SnapshotCodec: ValidateSchemaAndDigestVersion     [step 2 — ERR_DS_SCHEMA_INCOMPATIBLE]
ReplayRunner -> EnvService: ValidateEnvironmentFingerprint        [step 3 — ERR_DS_REPLAY_ENV_MISMATCH]
ReplayRunner -> DigestChain: ValidatePrevSnapshotDigest           [step 4 — ERR_DS_DIGEST_CHAIN_BREAK]
ReplayRunner -> SnapshotCodec: RehydrateAuthoritativeState        [step 5 — ERR_DS_SCHEMA_INCOMPATIBLE]
ReplayRunner -> RngService: RestoreAllCursors                     [step 6 — ERR_DS_RNG_STREAM_MISSING]
ReplayRunner -> ReplayRunner: AssertCursorAtEndOfSnapshot[T]      [step 7 — ERR_DS_REPLAY_BOUNDARY]
ReplayRunner -> TickOrchestrator: ReapplyInputsFromTickT+1        [step 8 — propagates from RunTick]
```

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
| `floatModelHash` | SHA-256 over the canonical float-flag tuple (§4.8.3) |
| `unicodeNormalizationVersion` | Unicode NFC table version pinned for `string` encoding (§3.2.4.1, `UNICODE_NFC_VERSION = "15.1"`). A Unicode-table upgrade between recording and replay deterministically fails replay with `ERR_DS_REPLAY_ENV_MISMATCH` instead of silently drifting digests |

### 4.8.1 Recording requirement
At match start, the runtime MUST capture `EnvironmentFingerprint` and embed it into every snapshot header for that match. Mid-match mutation of any pinned field is forbidden and MUST fail with **`ERR_DS_ENV_MUTATION`** (recording-side, 0x160D — distinct from `ERR_DS_REPLAY_ENV_MISMATCH` which is replay-side fingerprint divergence; see §3.4 and EC-016-013). Implementations MUST NOT reuse the replay-side error code for recording-side mutation.

### 4.8.2 Replay requirement
Before resume, the replay runtime MUST compare the live `EnvironmentFingerprint` against the snapshot's recorded fingerprint. Any mismatch MUST fail deterministically with `ERR_DS_REPLAY_ENV_MISMATCH` and MUST NOT attempt fallback execution.

### 4.8.3 floatModelHash composition (normative)
`floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))` over a canonical, ordered tuple of compiler and runtime float-mode flags. The 0x14 domain tag is the `EnvironmentFingerprint` domain (§3.2.4.1).

The tuple fields, in this exact serialization order, are:

| # | Field | Type | Source |
|---|---|---|---|
| 1 | `compilerToolchain` | `string` | One of `"MSVC"`, `"Clang"`, `"AppleClang"`, `"GCC"` (IL2CPP, Stage 5+), or `"Mono"` (the Stage-0 backend — Option A / ERR-016-006) (UTF-8) |
| 2 | `compilerVersion` | `string` | Major.Minor.Patch as reported by the toolchain (e.g. `"19.38.33135"`) |
| 3 | `targetTriple` | `string` | LLVM-style target triple (e.g. `"x86_64-pc-windows-msvc"`) |
| 4 | `il2cppVersion` | `string` | IL2CPP version string (Stage 5+), or the sentinel `"MONO"` for the Stage-0 Mono backend (Option A / ERR-016-006). **Stage-0 certification runs on the Mono backend and ACCEPTS `"MONO"` as the certified value** (the Stage-0 host pin in `certification-platform.md` is Mono; the reverse — requiring IL2CPP at Stage 0 — was the contradiction ERR-016-006 resolved). The sentinel still binds the backend into the fingerprint, so a cross-backend replay (Mono recording vs IL2CPP replay) fails with `ERR_DS_REPLAY_ENV_MISMATCH` rather than drifting digests. **From Stage 5+**, when the backend pin becomes IL2CPP, a non-empty IL2CPP version is mandatory at certification and a certification run MUST reject any `"MONO"` fingerprint as `ERR_DS_REPLAY_ENV_MISMATCH`. |
| 5 | `denormalsAreZero` | `bool` | Runtime CSR/MXCSR denormals-are-zero bit |
| 6 | `flushToZero` | `bool` | Runtime CSR/MXCSR flush-to-zero bit |
| 7 | `roundingMode` | `u8` | `0=NearestEven, 1=ToZero, 2=Upward, 3=Downward` |
| 8 | `fpContractMode` | `u8` | `0=Off, 1=On, 2=Fast` (corresponds to `-ffp-contract` / `/fp:precise|fast`) |
| 9 | `fmaEnabled` | `bool` | Whether fused multiply-add is permitted in authoritative paths |
| 10 | `fastMath` | `bool` | `-ffast-math` / `/fp:fast` |
| 11 | `simdLevel` | `string` | Lowest-common-denominator level (e.g. `"SSE4.2"`, `"AVX2"`) — must match `simdFeatureLevel` |

**Required Stage-0 values.** For Stage-0 certification on Windows x64 (§5.5):
- `denormalsAreZero = false`, `flushToZero = false`
- `roundingMode = 0` (NearestEven)
- `fpContractMode = 0` (Off)
- `fmaEnabled = false` unless platform-pinned and recorded
- `fastMath = false`

Concrete compiler flag strings to achieve this are listed in §5.5.1. The runtime MUST query MXCSR (or platform equivalent) at match start and reject the run with `ERR_DS_REPLAY_ENV_MISMATCH` if observed flags do not match the recorded tuple.

**Stage-0 Mono backend mapping (Option A; ERR-016-006).** Tuple fields 1–4 were originally written for a natively-compiled IL2CPP build, which contradicted the Stage-0 **Mono** host pin in `docs/tracking/certification-platform.md` (§4.8.3/§5.5 predate that pin). Under the pinned Mono backend the fields map as:
- `compilerToolchain` = `"Mono"`.
- `compilerVersion` = the Mono runtime / BCL version string, supplied by the certification host at capture time and recorded in the pinned tuple in `certification-platform.md` (it is not synthesised — see ERR-016-006).
- `targetTriple` = the .NET RID for the pinned host (Windows x64 ⇒ `"win-x64"`), in lieu of an LLVM triple.
- `il2cppVersion` = `"MONO"` (per field 4 above).

Fields 5–11 use the Required Stage-0 values above plus the pinned SIMD level (`"SSE4.2"`). The **live-host hasher** is implemented: `FloatFlagTuple.ComputeHash()` computes `SHA-256(SerializeCanonical(0x14 ‖ tuple))`, and `EnvironmentFingerprint.CreateStage0MonoCertified(monoRuntimeVersion)` assembles the Stage-0 Mono tuple and stamps the result (a genuine, non-placeholder fingerprint). The §4.8.2 runtime MXCSR *validation* (querying the live float-mode flags and rejecting on mismatch) remains a Stage-1+/host engineering task (native interop) tracked in ERR-016-006; the recorded tuple uses the pinned Stage-0 flag values, which is what §4.8.2 validates against.
