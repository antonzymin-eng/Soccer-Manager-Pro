# Deterministic Simulation Specification #16 — Section 3: Technical Specification

## 3.1 Core Models and Algorithms
### 3.1.1 Canonical intra-phase ordering
For every authoritative collection in each phase:
1. primary sort key: `EntityId` ascending
2. secondary key: subsystem deterministic ordinal
3. tertiary key: deterministic insertion index

Unordered container iteration in authoritative paths is forbidden unless copied to a sorted buffer first.

### 3.1.2 Canonical tick pseudocode (normative)
The "tick" in the pseudocode below is the **physics tick** at 60 Hz (16.67 ms). The 10 Hz tactical/AI cadence is expressed by gating the `AI` phase on `tick % 6 == 0`; on non-AI ticks the AI phase is a no-op that emits an empty phase digest so phase ordering and the digest stream remain invariant.

```text
PHYSICS_TICK_HZ = 60      # [FIXED] CLAUDE.md heartbeat
TACTICAL_TICK_HZ = 10     # [FIXED] CLAUDE.md heartbeat
AI_PHASE_STRIDE = PHYSICS_TICK_HZ / TACTICAL_TICK_HZ   # = 6 [DERIVED]

for tick in TickStart..TickEnd:
  BeginTick(tick)
  Phase(Input)
  Phase(Intent)
  if tick % AI_PHASE_STRIDE == 0:
    Phase(AI)             # full evaluation at 10 Hz
  else:
    Phase(AI_NoOp)        # emits empty-scope phase digest
  Phase(Physics)
  Phase(Resolve)
  Phase(Events)
  Phase(Snapshot)
  EndTick(tick)
```
If any phase emits authoritative mutation outside its WriteSet, tick commit MUST fail with `ERR_DS_PHASE_OWNERSHIP`.

The `AI_NoOp` phase MUST be emitted on every non-stride tick so that phase digests at index `2` (`AI`) appear in the digest rollup at every tick (see §5.10 rollup ordering). Skipping the phase entry would change rollup composition and break replay parity.

### 3.1.3 Job-system merge determinism
Parallel jobs MAY execute freely internally, but merge points MUST:
- synchronize at deterministic barriers,
- reduce outputs in canonical key order,
- use fixed reduction topology for accumulations.

### 3.1.4 Branch-safe RNG draw normalization
Per decision site, authoritative code MUST either:
- predeclare draw budget and consume fixed count, or
- use reservation API (`Reserve`, `DrawReserved`, `Skip`) preserving global cursor parity.

## 3.2 Formulas and Worked Examples
### 3.2.1 RNG stream derivation key
`StreamKey = SipHash-2-4-64(matchSeedKey, subsystemId, entityId, streamVersion)`

The stream key identifies a *persistent* RNG stream for the lifetime of the (subsystem, entity, streamVersion) tuple within a match. It MUST NOT include per-evaluation counters; per-evaluation indexing is handled by the `RngCursor` and the `actionOrdinal` reservation index defined in §3.2.5.

### 3.2.2 Phase digest construction
`PhaseDigest = SHA-256(SerializeCanonical(DigestVersion || Tick || PhaseId || phaseScopeFields))`

`Tick` and `PhaseId` MUST be included in the preimage so that two phases with otherwise-identical scope at different ticks produce distinct digests. `DigestVersion` MUST equal `DETERMINISM_DIGEST_VERSION` (§3.4) and is included to make digests self-identifying across version migrations.

### 3.2.3 Snapshot chain digest
`SnapshotDigest[T] = SHA-256(SnapshotHeader[T] || SnapshotPayload[T])`

The `currentSnapshotDigest` field on the snapshot record is stored adjacent to the header but is excluded from the preimage of its own computation (see §3.9.2 for the full preimage layout).

### 3.2.4 Digest algorithm binding (normative)
- `DigestVersion=1` MUST map to `SHA-256` with 32-byte (256-bit) output.
- The SHA-256 output is treated as an opaque 32-octet string; SHA-256 has no inherent endianness and implementations MUST NOT byte-swap the digest octets.
- All multi-byte integer fields *inside* the digest preimage (e.g. `Tick`, `PhaseId`, `DigestVersion`, `SchemaVersion`) MUST be serialized in `SNAPSHOT_PAYLOAD_ENDIANNESS` (§3.4 = `LittleEndian`).
- `StreamKey` derivation uses SipHash-2-4 with a 128-bit key derived from `matchSeed` via `PBKDF2-HMAC-SHA256(matchSeed, "DS-RNG-KEY-v1", 1)` (deterministic key expansion).
- `StreamKey` output width is 64-bit unsigned, encoded little-endian when serialized.
- Per-draw values (§3.2.5) are produced by `SipHash-2-4-64(StreamKey, RngCursor + drawIndex)` with `(RngCursor + drawIndex)` encoded as a little-endian `uint64`.

Worked example (conceptual):
- Tick 120 Physics phase serializes field scope in frozen order.
- Digest algorithm `v1` computes digest `D120P`.
- Replay must reproduce identical `D120P` for Tier A parity.

## 3.2.5 actionOrdinal and RngCursor semantics
- `actionOrdinal` is a per-stream (i.e. per-subsystem, per-entity) monotonically increasing **reservation index**. It is NOT part of the `StreamKey`.
- `actionOrdinal` increments once per deterministic decision-site evaluation regardless of which branch is taken (this is what makes reservation branch-safe).
- `RngCursor` is the per-stream draw counter. It advances by exactly the reservation budget of each evaluation (`Reserve(siteId, count)` advances the cursor by `count`), independent of how many `DrawReserved` calls actually consume bytes.
- A draw is computed as `SipHash-2-4-64(StreamKey, RngCursor + drawIndex)` where `drawIndex ∈ [0, count)` and `RngCursor` is the cursor value at the start of the reservation.
- After the evaluation completes, `RngCursor += count` and `actionOrdinal += 1`.
- Both `actionOrdinal` and `RngCursor` MUST be serialized per stream in snapshot/replay state and restored on load.
- Entity despawn retains a tombstone record `(EntityId, finalActionOrdinal, finalRngCursor)` in a despawn log keyed by `EntityId`; respawn with a new `EntityId` allocates a fresh stream with `actionOrdinal=0`, `RngCursor=0`.
- Reuse of an `EntityId` after despawn within the same match is forbidden.

## 3.3 Edge Cases
- **Mid-tick save request:** MUST be denied unless normalized to legal boundary with explicit phase marker.
- **Entity spawn/despawn during phase:** mutation queue applies at phase-defined barrier with deterministic insertion index.
- **NaN/Inf serialization:** forbidden for Tier A fields unless explicit canonical encoding is defined.
- **Unknown schema version on load:** deterministic incompatibility failure.
- **Missing stream on replay:** hard load failure (`ERR_DS_RNG_STREAM_MISSING`), no synthetic stream creation.

## 3.4 Constants Catalogue
Target catalogue: `Sim.Constants.Determinism`. Every constant carries one of the CLAUDE.md source tags (`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`).

| Constant | Value | Tag | Purpose |
|---|---|---|---|
| `PHYSICS_TICK_HZ` | `60` | [FIXED] | Physics/render heartbeat (CLAUDE.md, Ball Physics §1) |
| `TACTICAL_TICK_HZ` | `10` | [FIXED] | Tactical/AI heartbeat (CLAUDE.md) |
| `AI_PHASE_STRIDE` | `6` | [DERIVED] | `PHYSICS_TICK_HZ / TACTICAL_TICK_HZ` |
| `DETERMINISM_DIGEST_VERSION` | `1` | [FIXED] | Binds digest algorithm version (see §3.2.4) |
| `SNAPSHOT_PAYLOAD_ENDIANNESS` | `LittleEndian` | [FIXED] | Payload byte order (digest octets are opaque, see §3.2.4) |
| `TIER_A_COMPARATOR` | `BitwiseEqual` | [FIXED] | Mandatory comparator for Tier A fields |
| `TIER_B_DEFAULT_COMPARATOR` | `AbsEpsilon` | [FIXED] | Default comparator class for Tier B; no default magnitude (see §3.4.2) |
| `LEGAL_SAVE_BOUNDARIES` | `{ EndOfSnapshot }` | [FIXED] | Only legal phase boundary for save commit |
| `RNG_KEY_HASH` | `SipHash-2-4-64` | [FIXED] | Stream key derivation (see §3.2.1) |
| `RNG_DRAW_HASH` | `SipHash-2-4-64` | [FIXED] | Per-cursor draw derivation (see §3.2.5) |
| `PHASE_DIGEST_HASH` | `SHA-256` | [FIXED] | Phase digest algorithm under `DigestVersion=1` |
| `ERR_DS_PHASE_OWNERSHIP` | `0x1601` | [FIXED] | Mutation outside owning phase WriteSet |
| `ERR_DS_SCHEMA_INCOMPATIBLE` | `0x1602` | [FIXED] | Snapshot schema mismatch on load |
| `ERR_DS_RNG_STREAM_MISSING` | `0x1603` | [FIXED] | Required stream absent from snapshot |
| `ERR_DS_REPLAY_ENV_MISMATCH` | `0x1604` | [FIXED] | Worker count / topology mismatch on resume |
| `ERR_DS_SAVE_BOUNDARY` | `0x1605` | [FIXED] | Save requested outside `LEGAL_SAVE_BOUNDARIES` |
| `ERR_DS_TIERA_NONFINITE` | `0x1606` | [FIXED] | NaN/Inf observed in Tier A field |
| `ERR_DS_TIERB_TOLERANCE_MISSING` | `0x1607` | [FIXED] | Tier B field present in digest scope without approved tolerance row |
| `ERR_DS_DIGEST_CHAIN_BREAK` | `0x1608` | [FIXED] | `PrevSnapshotDigest` mismatch on replay load |

### 3.4.2 Tier B comparator default policy
`TIER_B_DEFAULT_COMPARATOR` declares the comparator *class* (`AbsEpsilon`) but does NOT supply a default tolerance magnitude. A Tier B field that appears in a digest scope without a matching tolerance row in the tolerance matrix MUST fail validation with `ERR_DS_TIERB_TOLERANCE_MISSING`. Implementations MUST NOT silently substitute a fallback epsilon.

## 3.5 Version History
- **v0.7 (May 2, 2026):** Major adversarial-fix pass. (a) Reformulated RNG model: `StreamKey` no longer carries `actionOrdinal`; `RngCursor` advances by reservation budget; per-draw values defined as `SipHash-2-4-64(StreamKey, cursor + i)`. (b) Added `Tick`/`PhaseId`/`DigestVersion` to `PhaseDigest` preimage. (c) Removed misleading "big-endian" digest language; bound payload-integer endianness to `SNAPSHOT_PAYLOAD_ENDIANNESS`. (d) Merged §3.4 and §3.4.1 constants into one tagged catalogue with hex IDs for all errors; added `ERR_DS_TIERB_TOLERANCE_MISSING` (0x1607) and `ERR_DS_DIGEST_CHAIN_BREAK` (0x1608). (e) Added `PHYSICS_TICK_HZ`/`TACTICAL_TICK_HZ`/`AI_PHASE_STRIDE` constants and `AI_NoOp` phase pattern reconciling 60 Hz physics tick with 10 Hz tactical cadence. (f) `LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }` only. (g) §3.6.1 universal prohibitions: seed root, environment fingerprint, snapshot history are immutable for all phases.
- **v0.4:** Added canonical tick pseudocode, explicit snapshot digest formula, and deterministic error IDs.
- **v0.3:** Technical contracts normalized to deterministic ordering, RNG reservation, and snapshot/digest rules.

## 3.6 Operational Policy Tables
### 3.6.1 Phase ownership contract
| Phase | Primary writes | Prohibited writes |
|---|---|---|
| Input | input buffer | world physics state |
| Intent | intent queue | snapshot bytes |
| AI | decision buffers | physics integration buffers |
| Physics | transforms, velocities | UI caches |
| Resolve | conflict resolution state | intent queue (read-only at this point) |
| Events | event ledger | historical snapshots |
| Snapshot | serialized bytes + digest | live gameplay intent queue |

**Universal prohibitions** (apply to ALL phases, not just where listed):
- `RNG seed root` (`matchSeed`, derived `matchSeedKey`) is **immutable for the lifetime of a match**. No phase may write it after match start.
- `EnvironmentFingerprint` is immutable for the lifetime of a match (see §4.8).
- Historical snapshots and the digest chain are append-only; no phase may rewrite a committed entry.

### 3.6.2 RNG draw-site registry requirements
Each draw site MUST define:
- stable ID,
- owning subsystem,
- reserved draw budget,
- migration note for version changes.

## 3.7 Worked Example: Branch-Safe RNG
Decision site `Shot.SelectTargetZone` reserves 4 draws per evaluation (`Reserve("Shot.SelectTargetZone", 4)`).

Initial state for stream `(AI, entity=18, v1)`: `RngCursor = 100`, `actionOrdinal = 7`.

- **Branch A** (early-out): consumes `DrawReserved(0)` and `DrawReserved(1)`, then `Skip(2)`.
- **Branch B** (full evaluation): consumes `DrawReserved(0..3)`.

After evaluation, **both branches** end with `RngCursor = 104`, `actionOrdinal = 8`. The cursor advancement is fixed by the reservation budget, not by the number of `DrawReserved` calls executed. This preserves replay parity regardless of branch taken.

## 3.8 Desync Triage Example
If first mismatch occurs at `tick=2210`, `phase=Physics`, `field=agents[18].velocity.x`:
1. classify tier/comparator,
2. inspect RNG cursor diffs for AI/Physics streams,
3. replay with forensic traces,
4. bisect to first bad commit.

## 3.9 Numeric Worked Examples
### 3.9.1 Canonical ordering serialization example
Input entity records in container iteration order:
`[(id=44,ord=2,ins=9), (id=12,ord=7,ins=1), (id=12,ord=3,ins=4)]`

Canonical sorted order by `(EntityId, ordinal, insertion)`:
1. `(12,3,4)`
2. `(12,7,1)`
3. `(44,2,9)`

Serialized canonical key stream:
`12|3|4;12|7|1;44|2|9`

Any alternate ordering MUST be treated as deterministic contract violation.

### 3.9.2 Snapshot digest chain example
Given:
- `PrevSnapshotDigest = 0xAA11…`
- `SnapshotPayloadHash = 0x9F20…`

Digest input bytes are composed as:
`SchemaVersion || Tick || EnvironmentFingerprint || PrevSnapshotDigest || PayloadBytes`

All multi-byte integer fields use `SNAPSHOT_PAYLOAD_ENDIANNESS` (§3.4 = `LittleEndian`). The computed `currentSnapshotDigest` is stored in the snapshot record adjacent to the header but excluded from the hash preimage.

If replayed load at identical tick produces a different digest (e.g., `0x9F21`), classification is `HardDesync` unless field set is explicitly Tier B scoped.

## 3.10 Edge-Case Decision Table
| Case | Trigger | Required behavior | Error/Classification |
|---|---|---|---|
| Mid-tick save request | request during `AI`/`Physics` | deny or defer to legal boundary | `ERR_DS_SAVE_BOUNDARY` |
| Unknown enum value on load | schema decode finds out-of-range enum | fail load deterministically | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| Missing RNG stream key | stream absent in snapshot | fail replay bootstrap | `ERR_DS_RNG_STREAM_MISSING` |
| NaN in Tier A field | decode or runtime emission | reject snapshot/tick commit | `ERR_DS_TIERA_NONFINITE` |
| Tier B field without tolerance row | digest scope contains B-tier path with no matching tolerance row | reject digest computation | `ERR_DS_TIERB_TOLERANCE_MISSING` |
| Snapshot chain break on resume | `prevSnapshotDigest` does not match expected predecessor | abort replay before rehydration | `ERR_DS_DIGEST_CHAIN_BREAK` |
| EnvironmentFingerprint mismatch | live runtime fingerprint ≠ snapshot fingerprint | abort replay before rehydration | `ERR_DS_REPLAY_ENV_MISMATCH` |
| Authoritative write outside WriteSet | phase mutates field not in declared WriteSet | fail tick commit | `ERR_DS_PHASE_OWNERSHIP` |

## 3.11 Algorithm Pseudocode: Deterministic Merge Barrier
```text
function MergePhaseOutputs(jobOutputs):
  buffer = []
  for output in jobOutputs:
    buffer.add(output)

  sort buffer by (EntityId, SubsystemOrdinal, InsertionIndex)

  for item in buffer:
    apply(item)

  if writesOutsideDeclaredWriteSet():
    fail(ERR_DS_PHASE_OWNERSHIP)
```

## 3.12 Version History
- **v0.7 (May 2, 2026):** Edge-case decision table extended with `ERR_DS_TIERB_TOLERANCE_MISSING`, `ERR_DS_DIGEST_CHAIN_BREAK`, `ERR_DS_REPLAY_ENV_MISMATCH`, `ERR_DS_PHASE_OWNERSHIP` rows. Snapshot digest example updated to include `EnvironmentFingerprint` in the preimage layout.
- **v0.6:** Added numeric worked examples, edge-case decision table, and deterministic merge pseudocode.
