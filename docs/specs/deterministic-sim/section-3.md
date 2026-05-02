# Deterministic Simulation Specification #16 — Section 3: Technical Specification

## 3.1 Core Models and Algorithms
### 3.1.1 Canonical intra-phase ordering
For every authoritative collection in each phase:
1. primary sort key: `EntityId` ascending
2. secondary key: subsystem deterministic ordinal
3. tertiary key: deterministic insertion index

Unordered container iteration in authoritative paths is forbidden unless copied to a sorted buffer first.

### 3.1.2 Canonical tick pseudocode (normative)
```text
for tick in TickStart..TickEnd:
  BeginTick(tick)
  Phase(Input)
  Phase(Intent)
  Phase(AI)
  Phase(Physics)
  Phase(Resolve)
  Phase(Events)
  Phase(Snapshot)
  EndTick(tick)
```
If any phase emits authoritative mutation outside its WriteSet, tick commit MUST fail with `ERR_DS_PHASE_OWNERSHIP`.

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
`StreamKey = SipHash-2-4-64(matchSeedKey, subsystemId, entityId, actionOrdinal, streamVersion)`

### 3.2.2 Phase digest construction
`PhaseDigest = SHA-256(SerializeCanonical(phaseScopeFields))`

### 3.2.3 Snapshot chain digest
`SnapshotDigest[T] = SHA-256(SnapshotHeaderWithoutCurrentDigest[T] || SnapshotPayload[T])`


### 3.2.4 Digest algorithm binding (normative)
- `DigestVersion=1` MUST map to `SHA-256` with 32-byte output.
- Digest byte order is network byte order (big-endian byte string as produced by SHA-256).
- `StreamKey` derivation uses SipHash-2-4 with a 128-bit key derived from `matchSeed`.
- `StreamKey` output width is 64-bit unsigned.

Worked example (conceptual):
- Tick 120 Physics phase serializes field scope in frozen order.
- Digest algorithm `v1` computes digest `D120P`.
- Replay must reproduce identical `D120P` for Tier A parity.

## 3.2.5 actionOrdinal semantics
- `actionOrdinal` is a per-entity, per-subsystem monotonically increasing counter across the whole match.
- Counter increments once per deterministic decision site evaluation.
- Counter state MUST be serialized in snapshot/replay state and restored on load.
- Entity despawn retains ordinal tombstone; respawn with new EntityId starts at 0.

## 3.3 Edge Cases
- **Mid-tick save request:** MUST be denied unless normalized to legal boundary with explicit phase marker.
- **Entity spawn/despawn during phase:** mutation queue applies at phase-defined barrier with deterministic insertion index.
- **NaN/Inf serialization:** forbidden for Tier A fields unless explicit canonical encoding is defined.
- **Unknown schema version on load:** deterministic incompatibility failure.
- **Missing stream on replay:** hard load failure (`ERR_DS_RNG_STREAM_MISSING`), no synthetic stream creation.

## 3.4 Constants Catalogue
- `DETERMINISM_DIGEST_VERSION = 1`
- `SNAPSHOT_ENDIANNESS = LittleEndian`
- `TIER_A_COMPARATOR = BitwiseEqual`
- `TIER_B_DEFAULT_COMPARATOR = AbsEpsilon`
- `LEGAL_SAVE_BOUNDARIES = { EndOfEvents, EndOfSnapshot }`
- `ERR_DS_PHASE_OWNERSHIP = 0x1601`
- `ERR_DS_SCHEMA_INCOMPATIBLE = 0x1602`
- `ERR_DS_RNG_STREAM_MISSING = 0x1603`

### 3.4.1 Constant tags and catalogue
Target catalogue: `Sim.Constants.Determinism`
- `DETERMINISM_DIGEST_VERSION` [FIXED]
- `SNAPSHOT_ENDIANNESS` [FIXED]
- `TIER_A_COMPARATOR` [FIXED]
- `TIER_B_DEFAULT_COMPARATOR` [FIXED]
- `LEGAL_SAVE_BOUNDARIES` [FIXED]
- `ERR_DS_PHASE_OWNERSHIP` [FIXED]
- `ERR_DS_SCHEMA_INCOMPATIBLE` [FIXED]
- `ERR_DS_RNG_STREAM_MISSING` [FIXED]
- `ERR_DS_SAVE_BOUNDARY` [FIXED]
- `ERR_DS_TIERA_NONFINITE` [FIXED]

## 3.5 Version History
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
| Resolve | conflict resolution state | RNG seed root |
| Events | event ledger | historical snapshots |
| Snapshot | serialized bytes + digest | live gameplay intent queue |

### 3.6.2 RNG draw-site registry requirements
Each draw site MUST define:
- stable ID,
- owning subsystem,
- reserved draw budget,
- migration note for version changes.

## 3.7 Worked Example: Branch-Safe RNG
Decision site `Shot.SelectTargetZone` reserves 4 draws per evaluation.
- Branch A: consumes draws `[0,1]` then skips `[2,3]`.
- Branch B: consumes draws `[0,1,2,3]`.
Both branches end at identical cursor position, preventing replay drift.

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
- `PrevSnapshotDigest = 0xAA11`
- `SnapshotPayloadHash = 0x9F20`

Digest input bytes are composed as:
`SchemaVersion || Tick || PrevSnapshotDigest || PayloadBytes`
(`currentSnapshotDigest` is excluded from preimage and stored adjacent after hash computation).

If replayed load at identical tick produces a different digest (e.g., `0x9F21`), classification is `HardDesync` unless field set is explicitly Tier B scoped.

## 3.10 Edge-Case Decision Table
| Case | Trigger | Required behavior | Error/Classification |
|---|---|---|---|
| Mid-tick save request | request during `AI`/`Physics` | deny or defer to legal boundary | `ERR_DS_SAVE_BOUNDARY` |
| Unknown enum value on load | schema decode finds out-of-range enum | fail load deterministically | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| Missing RNG stream key | stream absent in snapshot | fail replay bootstrap | `ERR_DS_RNG_STREAM_MISSING` |
| NaN in Tier A field | decode or runtime emission | reject snapshot/tick commit | `ERR_DS_TIERA_NONFINITE` |

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
- **v0.6:** Added numeric worked examples, edge-case decision table, and deterministic merge pseudocode.
