# Deterministic Simulation Specification #16 — Section 3: Technical Specification

## 3.1 Core Models and Algorithms
### 3.1.1 Canonical intra-phase ordering
For every authoritative collection in each phase:
1. primary sort key: `EntityId` ascending
2. secondary key: subsystem deterministic ordinal (see below)
3. tertiary key: deterministic insertion index

Unordered container iteration in authoritative paths is forbidden unless copied to a sorted buffer first.

**Subsystem deterministic ordinal:** Each subsystem is assigned a compile-time integer ordinal in `Sim.Constants.Determinism.SubsystemOrdinals` (a dedicated constant catalogue file). Ordinals are contiguous, non-negative, and MUST remain stable across all builds and platforms — reordering or renumbering ordinals is a breaking change requiring a schema version bump. The ordinal is the authoritative secondary sort key; subsystem display name or registration order MUST NOT be used as a substitute.

**Domain convention references:** Field paths in this spec that appear in tolerance rows or digest scope MUST observe CLAUDE.md project conventions:
- **Fatigue:** `0.0 = fully rested`, `1.0 = fully fatigued`. Any comparator applied to fatigue fields must interpret the scale accordingly.
- **Coordinate origin:** pitch corner `(0, 0, 0)` (not pitch center). Authoritative source: Ball Physics §1.2, Appendix C. [XC-016-001]

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

**`AI_NoOp` digest semantics (normative).** `AI_NoOp` produces a digest in the same form as any other phase: `SHA-256(SerializeCanonical(DigestVersion ‖ Tick ‖ PhaseId=2 ‖ phaseScopeFields))` where `phaseScopeFields` is the empty struct (zero bytes). The output is NOT a constant, NOT zero, and NOT omitted — it is tick-sensitive because `Tick` is in the preimage. Implementations MUST emit and rollup this digest at every non-stride tick. The 12-byte worked example in §3.2.4.1 is the canonical `AI_NoOp` preimage at `Tick=120` (with `PhaseId=2` substituted for the `PhaseId=3` shown there, yielding a different SHA-256 output).

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
### 3.2.1 RNG stream derivation key (FM-016-001)
`StreamKey = SipHash-2-4-64((k0,k1), subsystemId ∥ entityId ∥ streamVersion)`  [RNG_STREAM_HASH]

where `(k0, k1)` is the 128-bit SipHash key derived from `matchSeed` via `RNG_KDF` (see §3.2.4). The stream key identifies a *persistent* RNG stream for the lifetime of the (subsystem, entity, streamVersion) tuple within a match. It MUST NOT include per-evaluation counters; per-evaluation indexing is handled by the `RngCursor` and the `actionOrdinal` reservation index defined in §3.2.5.

### 3.2.2 Phase digest construction (FM-016-002)
`PhaseDigest = SHA-256(SerializeCanonical(DigestVersion || Tick || PhaseId || phaseScopeFields))`

`Tick` and `PhaseId` MUST be included in the preimage so that two phases with otherwise-identical scope at different ticks produce distinct digests. `DigestVersion` MUST equal `DETERMINISM_DIGEST_VERSION` (§3.4) and is included to make digests self-identifying across version migrations.

### 3.2.3 Snapshot chain digest (FM-016-003)
`SnapshotDigest[T] = SHA-256(SnapshotHeader[T] || SnapshotPayload[T])`

The `currentSnapshotDigest` field on the snapshot record is stored adjacent to the header but is excluded from the preimage of its own computation (see §3.9.2 for the full preimage layout).

**Ring-buffer and chain verification:** The `SnapshotHeader[T]` includes a `prevSnapshotDigest` field that links to `SnapshotDigest[T-1]`. Once `SnapshotDigest[T-1]` is computed and written into `SnapshotHeader[T]`, the raw bytes of snapshot `T-1` are no longer needed to verify the chain — the stored digest value in the header is sufficient. A ring buffer that evicts old snapshot bytes is therefore compatible with chain integrity, because chain link verification at replay load time reads only the stored `prevSnapshotDigest` value from the header, not the original bytes. The ring buffer need only retain snapshot bytes long enough to complete the hash computation for the *next* snapshot's header before eviction.

### 3.2.4 Digest algorithm binding (normative)
- `DigestVersion=1` MUST map to `SHA-256` with 32-byte (256-bit) output.
- The SHA-256 output is treated as an opaque 32-octet string; SHA-256 has no inherent endianness and implementations MUST NOT byte-swap the digest octets.
- All multi-byte integer fields *inside* the digest preimage (e.g. `Tick`, `PhaseId`, `DigestVersion`, `SchemaVersion`) MUST be serialized in `SNAPSHOT_PAYLOAD_ENDIANNESS` (§3.4 = `LittleEndian`).
- `matchSeedKey` is derived from `matchSeed` via `HKDF-SHA256` (RFC 5869, `RNG_KDF`):
  `matchSeedKey = HKDF-SHA256(IKM=matchSeed, salt=∅, info="DS-RNG-KEY-v1", length=RNG_KDF_OUTPUT_BYTES)`
  The 16-octet output is split into two little-endian `uint64` values `(k0, k1)`: bytes [0..7] = `k0`, bytes [8..15] = `k1`. These form the SipHash-2-4 key. Implementations MUST NOT use PBKDF2 or any other KDF for this step.
- `StreamKey` derivation uses `SipHash-2-4-64((k0,k1), subsystemId ∥ entityId ∥ streamVersion)` (constant `RNG_STREAM_HASH`; see §3.2.1). `StreamKey` output width is 64-bit unsigned, encoded little-endian when serialized.
- Per-draw values (§3.2.5) are produced by `SipHash-2-4-64((k0,k1), StreamKey ∥ actionOrdinal ∥ drawIndex)` with field widths bound in §3.4. Concatenation (`∥`) is byte-string concatenation in canonical schema order — NOT arithmetic addition. `actionOrdinal` (the per-evaluation reservation index, §3.2.5) provides per-evaluation domain separation so that even under a budget-arithmetic bug a draw at one decision site cannot alias a draw at a different decision site on the same stream.

**Hash-input field widths and concatenation rules (normative).** All hash preimages in §3.2 are byte-string concatenation of fixed-width fields in the order written. Field widths are listed in §3.4 (`HASH_INPUT_FIELD_WIDTHS`). No length prefixes, no separators, no zero padding. Implementations MUST NOT reorder, omit, or resize any field. Any width change is a `DigestVersion` bump.

Worked example (conceptual):
- Tick 120 Physics phase serializes field scope in frozen order.
- Digest algorithm `v1` computes digest `D120P`.
- Replay must reproduce identical `D120P` for Tier A parity.

### 3.2.4.1 SerializeCanonical (normative byte-level schema)
`SerializeCanonical(...)` referenced in §3.2.2 and Appendix A.1 is the byte-string concatenation of fields per the rules below. Implementations MUST follow this schema bit-for-bit; no length prefixes, no separators, no padding, no reordering except where explicitly noted.

**Primitive encodings.** All multi-byte integers are little-endian (`SNAPSHOT_PAYLOAD_ENDIANNESS`).

| Type | Byte width | Encoding |
|---|---|---|
| `bool` | 1 | `0x00` = false, `0x01` = true; no other byte values are legal |
| `u8` / `i8` | 1 | unsigned / two's complement signed |
| `u16` / `i16` | 2 | little-endian; signed = two's complement |
| `u32` / `i32` | 4 | little-endian; signed = two's complement |
| `u64` / `i64` | 8 | little-endian; signed = two's complement |
| `f32` | 4 | IEEE-754 binary32 raw bit pattern, little-endian. NaN bit pattern is normalized to `0x7FC00000` (canonical quiet NaN) before serialization for Tier B fields; Tier A fields reject NaN/Inf (§3.3 / EC-016-004) |
| `f64` | 8 | IEEE-754 binary64 raw bit pattern, little-endian. NaN normalized to `0x7FF8000000000000` for Tier B; rejected for Tier A |
| `string` (UTF-8) | 4 + N | NFC-normalized UTF-8 bytes prefixed by `u32` byte length (NOT codepoint count). Maximum length `STRING_MAX_BYTES = 65536`; longer values fail with `ERR_DS_SCHEMA_INCOMPATIBLE` |
| `bytes` | 4 + N | raw bytes prefixed by `u32` byte length |
| `array<T>` | 4 + N·sizeof(T) | element count as `u32` followed by `N` elements in canonical sort order (§3.1.1); empty array = `0x00000000` only, no terminator |
| `optional<T>` | 1 + (0 or sizeof(T)) | `0x00` = absent (no payload follows); `0x01` = present (payload of `T` follows). No other tag byte values are legal |
| `enum` | 1 (≤256 variants) or 2 (≤65536 variants) | underlying integer value; width fixed at schema definition time and frozen with `DigestVersion` |
| `struct` | sum of fields | flat concatenation of fields in declared schema order; no struct header, no field tag, no per-field separator |

**Domain-tag fields.** Each top-level digest preimage begins with a 1-byte `DOMAIN_TAG`:
- `0x10` = `PhaseDigest` preimage
- `0x11` = `SnapshotPayload` preimage
- `0x12` = `SnapshotHeader` preimage
- `0x13` = `RngDraw` preimage
- `0x14` = `EnvironmentFingerprint` preimage

This separates hash domains and prevents cross-domain preimage collisions even under identical field bytes. Domain tags are part of the schema and MUST NOT be omitted.

**Field width registry (`HASH_INPUT_FIELD_WIDTHS`, normative).** All identifiers used as hash inputs in §3.2 have fixed widths:

| Field | Type | Width |
|---|---|---|
| `DigestVersion` | u16 | 2 |
| `Tick` | u64 | 8 |
| `PhaseId` | u8 | 1 |
| `SchemaVersion` | u32 | 4 |
| `subsystemId` | u16 | 2 |
| `entityId` | u32 | 4 |
| `streamVersion` | u16 | 2 |
| `actionOrdinal` | u64 | 8 |
| `drawIndex` | u32 | 4 |
| `RngCursor` | u64 | 8 |
| `StreamKey` (output) | u64 | 8 |

Widening any of these is a `DigestVersion` bump and a `streamVersion` bump (for stream-key inputs).

**Worked byte example.** `PhaseDigest` preimage for `Tick=120`, `PhaseId=3` (Physics), `DigestVersion=1`, empty `phaseScopeFields`:

```
DOMAIN_TAG     : 10
DigestVersion  : 01 00
Tick           : 78 00 00 00 00 00 00 00
PhaseId        : 03
phaseScopeFields: (empty — 0 bytes; an empty array would encode as 00 00 00 00, but
                   phaseScopeFields is a struct here, not an array)
```

Total preimage: 12 bytes `10 01 00 78 00 00 00 00 00 00 00 03`. SHA-256 of this exact byte sequence is the digest.

**Verification artifact.** A reference test corpus of (input record → expected SHA-256 digest) tuples MUST exist as `docs/specs/deterministic-sim/golden-vectors/serialize-canonical-corpus.md` before §9.5 acceptance criterion #4 can be checked. The corpus is a normative implementation-conformance test.

### 3.2.5 actionOrdinal and RngCursor semantics
- `actionOrdinal` is a per-stream (i.e. per-subsystem, per-entity) monotonically increasing **reservation index**. It is NOT part of the `StreamKey`.
- `actionOrdinal` increments once per deterministic decision-site evaluation regardless of which branch is taken. It is auxiliary bookkeeping that records how many reservations have been made on the stream.
- Branch-safety is provided by `RngCursor` advancing by the reservation budget (`Reserve(siteId, count)` advances the cursor by exactly `count`) regardless of how many `DrawReserved` calls actually execute. `actionOrdinal` is a corroborating counter, not the source of branch-safety; omitting `actionOrdinal` updates would not break cursor parity but would break snapshot/replay corroboration.
- `RngCursor` is the per-stream draw counter. It advances by exactly the reservation budget of each evaluation, independent of how many `DrawReserved` calls actually consume bytes.
- A draw is computed as `SipHash-2-4-64((k0,k1), StreamKey ∥ actionOrdinal ∥ drawIndex)` where `drawIndex ∈ [0, count)` and `actionOrdinal` is the value at entry to the evaluation (i.e. before the post-evaluation `actionOrdinal += 1`). `RngCursor` is NOT in the SipHash input; it is bookkeeping for snapshot/replay state and corroboration only.
- After the evaluation completes, `RngCursor += count` and `actionOrdinal += 1`.
- Both `actionOrdinal` and `RngCursor` MUST be serialized per stream in snapshot/replay state and restored on load.
- Entity despawn retains a tombstone record `(EntityId, finalActionOrdinal, finalRngCursor)` in a despawn log keyed by `EntityId`; respawn with a new `EntityId` allocates a fresh stream with `actionOrdinal=0`, `RngCursor=0`.
- Reuse of an `EntityId` after despawn within the same match is forbidden. **Cross-spec normative constraint:** entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee `EntityId` uniqueness for the lifetime of a match; once an `EntityId` is despawned it MUST NOT be reassigned. Violation breaks stream isolation and replay parity. This constraint is filed for back-propagation in `docs/tracking/spec-error-log.md` (ERR-016-EntityId-NoReuse) and is tracked in CLAUDE.md "Open Issues" until reciprocal `XC-` references are filed in #2 and #8.

### 3.2.5.1 Intra-stream draw-site ordering
A single stream `(subsystem, entity)` may be drawn from by multiple decision sites within one phase (e.g. `AI.DecidePass` and `AI.DecideShoot` both call `Reserve()` against entity 42's AI stream). The `RngCursor` outcome depends on the order in which those sites call `Reserve()`.

**Normative ordering rule:** Decision sites within the same (subsystem, entity) stream MUST call `Reserve()` in **stable declaration order** — the lexicographic order of their stable site IDs as registered in the draw-site registry (§3.6.2). This order MUST be compile-time deterministic and identical across all builds and platforms. The draw-site registry is the single source of truth for this ordering; any reordering of site IDs in the registry is a breaking change requiring a `streamVersion` bump.

### 3.2.5.2 Cross-match EntityId lifecycle
The "lifetime of a match" no-reuse constraint applies within a single match instance. Across match boundaries:
- Each match instance allocates a fresh `EntityId` namespace; matchN's `EntityId=42` and matchN+1's `EntityId=42` are unrelated.
- The despawn tombstone log is scoped to the owning match and is cleared when the match instance is finalized.
- Career-mode persistence (a player surviving across matches) is encoded outside the per-match `EntityId` namespace via a stable career-level `PersonId`; the per-match `EntityId` is reallocated each match. Mapping `PersonId → EntityId` is established at match setup and frozen for the match's lifetime.
- A new RNG stream is allocated per match: `actionOrdinal=0`, `RngCursor=0`. RNG state does NOT carry across match boundaries.

This binds replay across matches: replay of matchN+1 is independent of any matchN tombstone state.

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
| `RNG_KDF` | `HKDF-SHA256` | [FIXED] | KDF (RFC 5869) for deriving 128-bit SipHash key `(k0,k1)` from `matchSeed`; MUST NOT be substituted with PBKDF2 or any other KDF (see §3.2.4) |
| `RNG_KDF_OUTPUT_BYTES` | `16` | [FIXED] | HKDF output length; bytes [0..7]=k0, bytes [8..15]=k1 (both little-endian uint64) |
| `RNG_STREAM_HASH` | `SipHash-2-4-64` | [FIXED] | SipHash algorithm used for both stream key derivation (§3.2.1) and per-cursor draw values (§3.2.5) |
| `PHASE_DIGEST_HASH` | `SHA-256` | [FIXED] | Phase digest algorithm under `DigestVersion=1` |
| `ERR_DS_PHASE_OWNERSHIP` | `0x1601` | [FIXED] | Mutation outside owning phase WriteSet |
| `ERR_DS_SCHEMA_INCOMPATIBLE` | `0x1602` | [FIXED] | Snapshot schema mismatch on load |
| `ERR_DS_RNG_STREAM_MISSING` | `0x1603` | [FIXED] | Required stream absent from snapshot |
| `ERR_DS_REPLAY_ENV_MISMATCH` | `0x1604` | [FIXED] | Worker count / topology mismatch on resume |
| `ERR_DS_SAVE_BOUNDARY` | `0x1605` | [FIXED] | Save requested outside `LEGAL_SAVE_BOUNDARIES` |
| `ERR_DS_TIERA_NONFINITE` | `0x1606` | [FIXED] | NaN/Inf observed in Tier A field |
| `ERR_DS_TIERB_TOLERANCE_MISSING` | `0x1607` | [FIXED] | Tier B field present in digest scope without approved tolerance row |
| `ERR_DS_DIGEST_CHAIN_BREAK` | `0x1608` | [FIXED] | `PrevSnapshotDigest` mismatch on replay load |
| `ERR_DS_REPLAY_BOUNDARY` | `0x1609` | [FIXED] | Replay cursor not positioned at `EndOfSnapshot[T]` before T+1 reapplication (§4.2.2 step 7) |
| `ERR_DS_TIERB_NONFINITE` | `0x160A` | [FIXED] | NaN/Inf observed in a Tier B field outside the canonical-NaN encoding (§3.2.4.1). NOT classified as Tier B drift; treated as a hard encoding bug |
| `ERR_DS_RNG_BUDGET_MISMATCH` | `0x160B` | [FIXED] | `Reserve(siteId, count)` called with `count` not equal to the registered budget for `siteId` in the draw-site registry (§3.6.2) |
| `ERR_DS_STORAGE_ATOMICITY` | `0x160C` | [FIXED] | Save commit failed atomic write contract (§4.6.1.1); snapshot was not made durable as a single observable transition |
| `PHYSICS_DT` | `0x3C8888B7` (f32 bit pattern of `1.0f / 60.0f`) | [DERIVED] | Physics tick interval. Computation rule: `(float)(1.0 / 60.0)` evaluated under round-to-nearest-even; the literal bit pattern `0x3C8888B7` is the normative reference value. Implementations MUST NOT use `0.0166666675f` literals or pre-baked constants — they MUST compute from `1.0f / (float)PHYSICS_TICK_HZ` to match the bit pattern exactly |
| `STRING_MAX_BYTES` | `65536` | [FIXED] | Maximum UTF-8 byte length for any `string` field in canonical serialization (§3.2.4.1) |
| `HASH_INPUT_FIELD_WIDTHS` | (table in §3.2.4.1) | [FIXED] | Normative widths of all hash-input fields. Width changes require `DigestVersion` bump |
| `NAN_CANONICAL_F32` | `0x7FC00000` | [FIXED] | Canonical quiet-NaN bit pattern for Tier B `f32` fields (§3.2.4.1) |
| `NAN_CANONICAL_F64` | `0x7FF8000000000000` | [FIXED] | Canonical quiet-NaN bit pattern for Tier B `f64` fields (§3.2.4.1) |

### 3.4.1 Reserve budget enforcement (normative)
`Reserve(siteId, count)` MUST validate `count` against the budget registered for `siteId` in the draw-site registry (§3.6.2). On mismatch, `Reserve` MUST fail the tick commit with `ERR_DS_RNG_BUDGET_MISMATCH` and MUST NOT advance `RngCursor` or `actionOrdinal`. Silent acceptance of a divergent count is forbidden — it would silently break replay parity across builds.

### 3.4.2 Tier B comparator default policy
`TIER_B_DEFAULT_COMPARATOR` declares the comparator *class* (`AbsEpsilon`) but does NOT supply a default tolerance magnitude. A Tier B field that appears in a digest scope without a matching tolerance row in the tolerance matrix MUST fail validation with `ERR_DS_TIERB_TOLERANCE_MISSING`. Implementations MUST NOT silently substitute a fallback epsilon.

## 3.5 Version History
- **v0.9 (May 3, 2026):** Third-pass adversarial critique resolution. (a) H-A: added §3.2.4.1 `SerializeCanonical` normative byte-level schema with primitive encoding table, domain tags, field-width registry, and worked byte example. (b) H-B: bound hash-input field widths in §3.4 (`HASH_INPUT_FIELD_WIDTHS`); changed per-draw SipHash input from `(RngCursor + drawIndex)` arithmetic addition to `(StreamKey ‖ actionOrdinal:u64 ‖ drawIndex:u32)` byte concatenation, providing per-evaluation domain separation independent of cursor budget correctness. (c) M-G: added `ERR_DS_TIERB_NONFINITE` (0x160A); Tier B `f32`/`f64` NaN normalized to canonical quiet-NaN bit patterns (`NAN_CANONICAL_F32`, `NAN_CANONICAL_F64`). (d) L-N: `Reserve(siteId, count)` budget mismatch is hard fail (`ERR_DS_RNG_BUDGET_MISMATCH`, 0x160B); §3.4.1 normative. (e) M-J: `PHYSICS_DT` constant with normative computation rule and reference bit pattern (`0x3C8888B7`). (f) L-O: `actionOrdinal` and `RngCursor` widths bound to u64. (g) M-H: §3.2.5.2 cross-match EntityId lifecycle rules (per-match namespace, tombstone scope, career-level `PersonId` mapping, RNG reset). (h) L-P: AI_NoOp digest semantics clarified (tick-sensitive, not constant). (i) M-I: `ERR_DS_STORAGE_ATOMICITY` (0x160C) added for save-commit atomicity violation (paired with §4.6.1.1 atomic-write contract).
- **v0.8 (May 2, 2026):** Second-pass critique fixes. (a) Replaced PBKDF2-HMAC-SHA256 with HKDF-SHA256 (RFC 5869) for `matchSeedKey` derivation; added `RNG_KDF`, `RNG_KDF_OUTPUT_BYTES` constants; renamed `RNG_KEY_HASH`/`RNG_DRAW_HASH` to `RNG_STREAM_HASH` — eliminates KDF ambiguity (A-2, A-3). (b) Added `ERR_DS_REPLAY_BOUNDARY` (0x1609) error code. (c) Added `AI_NoOp` row to §3.6.1 phase ownership table (A-1). (d) Fixed §3.6.1 Resolve row parenthetical (D-19). (e) Fixed §3.2.5 cause/effect: `RngCursor` advance is the source of branch-safety; `actionOrdinal` is auxiliary (A-5). (f) Added §3.2.5.1 intra-stream draw-site ordering rule (B-10). (g) Added EntityId no-reuse cross-spec normative constraint (D-21). (h) Clarified ring-buffer/chain-digest relationship in §3.2.3 (B-11). (i) Added subsystem ordinal assignment rule and domain convention refs (fatigue, corner origin) to §3.1.1 (D-18, B-12). (j) Added FM-016-001/002/003 formula IDs; EC-016-001..008 edge-case IDs (C-15). (k) Edge-case decision table extended with `ERR_DS_TIERB_TOLERANCE_MISSING`, `ERR_DS_DIGEST_CHAIN_BREAK`, `ERR_DS_REPLAY_ENV_MISMATCH`, `ERR_DS_PHASE_OWNERSHIP` rows; snapshot digest example updated to include `EnvironmentFingerprint` (formerly §3.12).
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
| AI_NoOp | (none — empty WriteSet; emits empty phase digest only) | all phase-specific writes; identical prohibitions as the AI phase |
| Physics | transforms, velocities | UI caches |
| Resolve | conflict resolution state | intent queue |
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
| ID | Case | Trigger | Required behavior | Error/Classification |
|---|---|---|---|---|
| EC-016-001 | Mid-tick save request | request during `AI`/`Physics` | deny or defer to legal boundary | `ERR_DS_SAVE_BOUNDARY` |
| EC-016-002 | Unknown enum value on load | schema decode finds out-of-range enum | fail load deterministically | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| EC-016-003 | Missing RNG stream key | stream absent in snapshot | fail replay bootstrap | `ERR_DS_RNG_STREAM_MISSING` |
| EC-016-004 | NaN in Tier A field | decode or runtime emission | reject snapshot/tick commit | `ERR_DS_TIERA_NONFINITE` |
| EC-016-005 | Tier B field without tolerance row | digest scope contains B-tier path with no matching tolerance row | reject digest computation | `ERR_DS_TIERB_TOLERANCE_MISSING` |
| EC-016-006 | Snapshot chain break on resume | `prevSnapshotDigest` does not match expected predecessor | abort replay before rehydration | `ERR_DS_DIGEST_CHAIN_BREAK` |
| EC-016-007 | EnvironmentFingerprint mismatch | live runtime fingerprint ≠ snapshot fingerprint | abort replay before rehydration | `ERR_DS_REPLAY_ENV_MISMATCH` |
| EC-016-008 | Authoritative write outside WriteSet | phase mutates field not in declared WriteSet | fail tick commit | `ERR_DS_PHASE_OWNERSHIP` |

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

_(Version History consolidated into §3.5; the duplicate block formerly at §3.12 was merged on May 3, 2026 per third-pass critique L-K.)_
