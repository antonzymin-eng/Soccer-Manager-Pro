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
# Heartbeat-and-stride change policy: Any change to PHYSICS_TICK_HZ,
# TACTICAL_TICK_HZ, or the derived AI_PHASE_STRIDE reshapes the digest rollup
# (different ticks emit AI_NoOp vs AI). Such a change MUST trigger a
# DETERMINISM_DIGEST_VERSION bump (§3.4) and invalidates all pre-existing
# replay corpus and golden vectors. (Pass 4 L-7.)

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
`PhaseDigest = SHA-256(SerializeCanonical(DOMAIN_TAG_PHASE || DigestVersion || Tick || PhaseId || phaseScopeFields))`

The preimage MUST begin with the 1-byte `DOMAIN_TAG_PHASE = 0x10` (see §3.2.4.1 domain-tag table). `Tick` and `PhaseId` MUST be included so that two phases with otherwise-identical scope at different ticks produce distinct digests. `DigestVersion` MUST equal `DETERMINISM_DIGEST_VERSION` (§3.4) and is included to make digests self-identifying across version migrations. Field widths are bound in §3.4 `HASH_INPUT_FIELD_WIDTHS`; concatenation rules per §3.2.4.1.

### 3.2.3 Snapshot chain digest (FM-016-003)
`SnapshotDigest[T] = SHA-256(SerializeCanonical(0x12 ‖ SnapshotHeader[T]) ‖ SerializeCanonical(0x11 ‖ SnapshotPayload[T]))`

`SnapshotHeader[T]` MUST be serialized in the schema declaration order from §2.3: `schemaVersion ‖ tick ‖ prevSnapshotDigest ‖ environmentFingerprint`. `SnapshotPayload[T]` follows the canonical schema order (§3.2.4.1). The two domain tags (`0x12` for header, `0x11` for payload — see §3.2.4.1) bind each component to its hash domain. The `currentSnapshotDigest` field is stored on the snapshot record per the §3.9.2 on-disk layout (after the payload, before the record trailer) and is excluded from the preimage of its own computation. The on-disk record byte layout is normative and is defined in §3.9.2.

**Ring-buffer and chain verification:** The `SnapshotHeader[T]` includes a `prevSnapshotDigest` field that links to `SnapshotDigest[T-1]`. Once `SnapshotDigest[T-1]` is computed and written into `SnapshotHeader[T]`, the raw bytes of snapshot `T-1` are no longer needed to verify the chain — the stored digest value in the header is sufficient. A ring buffer that evicts old snapshot bytes is therefore compatible with chain integrity, because chain link verification at replay load time reads only the stored `prevSnapshotDigest` value from the header, not the original bytes. The ring buffer need only retain snapshot bytes long enough to complete the hash computation for the *next* snapshot's header before eviction.

### 3.2.4 Digest algorithm binding (normative)
- `DigestVersion=1` MUST map to `SHA-256` with 32-byte (256-bit) output.
- The SHA-256 output is treated as an opaque 32-octet string; SHA-256 has no inherent endianness and implementations MUST NOT byte-swap the digest octets.
- All multi-byte integer fields *inside* the digest preimage (e.g. `Tick`, `PhaseId`, `DigestVersion`, `SchemaVersion`) MUST be serialized in `SNAPSHOT_PAYLOAD_ENDIANNESS` (§3.4 = `LittleEndian`).
- `matchSeedKey` is derived from `matchSeed` via `HKDF-SHA256` (RFC 5869, `RNG_KDF`):
  `matchSeedKey = HKDF-SHA256(IKM=matchSeed, salt=NULL, info="DS-RNG-KEY-v1", length=RNG_KDF_OUTPUT_BYTES)`
  The 16-octet output is split into two little-endian `uint64` values `(k0, k1)`: bytes [0..7] = `k0`, bytes [8..15] = `k1`. These form the SipHash-2-4 key. Implementations MUST NOT use PBKDF2 or any other KDF for this step.
  - **`salt` parameter (normative).** `salt = NULL` is interpreted per RFC 5869 §2.2: the HKDF-Extract step uses an internally-allocated string of `HashLen` (32) zero bytes. Implementations MUST pass the salt as either a null/absent value or as 32 zero bytes — both are equivalent under RFC 5869 §2.2 — but MUST NOT pass an empty-length non-null buffer if the underlying library distinguishes the two cases. A KAT row exercising this binding MUST be present in `hkdf-sha256-kat.md` (§9.5 #4(a)).
  - **`info` parameter byte encoding (normative).** The bytes fed to HKDF-Expand are the **raw 13 ASCII bytes** of the literal `DS-RNG-KEY-v1` (`0x44 0x53 0x2D 0x52 0x4E 0x47 0x2D 0x4B 0x45 0x59 0x2D 0x76 0x31`). HKDF `info` is an opaque RFC 5869 byte string and is **NOT** wrapped in the §3.2.4.1 `string` framing (no `u32` length prefix, no NFC normalization — the literal is ASCII). The same raw-bytes rule applies to any other HKDF/HMAC parameter that escapes the canonical-serializer. A KAT row binding this exact `info` to its expected `(k0, k1)` output MUST be in `hkdf-sha256-kat.md`.
- `StreamKey` derivation uses `SipHash-2-4-64((k0,k1), subsystemId ∥ entityId ∥ streamVersion)` (constant `RNG_STREAM_HASH`; see §3.2.1). `StreamKey` output width is 64-bit unsigned, encoded little-endian when serialized.
- Per-draw values (§3.2.5) are produced by `SipHash-2-4-64((k0,k1), DOMAIN_TAG_RNGDRAW ∥ StreamKey ∥ actionOrdinal ∥ drawIndex)` where `DOMAIN_TAG_RNGDRAW = 0x13` (§3.2.4.1) is included in the SipHash message to bind RNG-draw inputs to their hash domain. Field widths are bound in §3.4 `HASH_INPUT_FIELD_WIDTHS`. Concatenation (`∥`) is byte-string concatenation in canonical schema order — NOT arithmetic addition. `actionOrdinal` (the per-evaluation reservation index, §3.2.5) provides per-evaluation domain separation so that even under a budget-arithmetic bug a draw at one decision site cannot alias a draw at a different decision site on the same stream.

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
| `bool` | 1 | `0x00` = false, `0x01` = true; no other byte values are legal. Decode of any other byte value MUST fail with `ERR_DS_SCHEMA_INCOMPATIBLE` |
| `u8` / `i8` | 1 | unsigned / two's complement signed |
| `u16` / `i16` | 2 | little-endian; signed = two's complement |
| `u32` / `i32` | 4 | little-endian; signed = two's complement |
| `u64` / `i64` | 8 | little-endian; signed = two's complement |
| `f32` | 4 | IEEE-754 binary32 raw bit pattern, little-endian. NaN bit pattern is normalized to `0x7FC00000` (canonical quiet NaN) before serialization for Tier B fields; Tier A fields reject NaN/Inf (§3.3 / EC-016-004). Negative-zero bit pattern `0x80000000` is normalized to positive-zero `0x00000000` BEFORE serialization for **both** Tier A and Tier B (avoids spurious `BitwiseEqual` failures from arithmetically-equal zero accumulators with differing sign bits — see Pass 5 M-2) |
| `f64` | 8 | IEEE-754 binary64 raw bit pattern, little-endian. NaN normalized to `0x7FF8000000000000` for Tier B; rejected for Tier A. Negative-zero `0x8000000000000000` normalized to `0x0000000000000000` for both tiers before serialization |
| `string` (UTF-8) | 4 + N | NFC-normalized UTF-8 bytes prefixed by `u32` byte length (NOT codepoint count). NFC normalization MUST follow Unicode 15.1 normalization tables (`UNICODE_NFC_VERSION`, §3.4); the Unicode version is recorded in `EnvironmentFingerprint` (§4.8) so that a Unicode-table upgrade invalidates replay parity deterministically. Stage-0 authoritative strings are restricted to ASCII (`U+0000`–`U+007F`), making NFC normalization a no-op; the Unicode-version pin exists to bind future non-ASCII usage. Maximum length `STRING_MAX_BYTES = 65536`; longer values fail with `ERR_DS_SCHEMA_INCOMPATIBLE` |
| `bytes` | 4 + N | raw bytes prefixed by `u32` byte length |
| `array<T>` | 4 + sum of element widths | element count as `u32` followed by `N` elements in canonical sort order (§3.1.1); empty array = `0x00000000` only, no terminator. For fixed-width `T`, total width = `4 + N·sizeof(T)`. For variable-width `T` (`string`, `bytes`, `optional<T>`, nested `array<T>`, struct-with-string), the total width is the sum of each element's encoded width per its row in this table; there is no per-element framing — the canonical encoding of each element is self-delimiting |
| `optional<T>` | 1 + (0 or width(T)) | `0x00` = absent (no payload follows); `0x01` = present (payload of `T` follows). No other tag byte values are legal; decode of any other tag byte MUST fail with `ERR_DS_SCHEMA_INCOMPATIBLE` |
| `enum` | 1 (≤256 variants) or 2 (≤65536 variants) | underlying integer value; width fixed at schema definition time and frozen with `SchemaVersion` (§2.3 — adding a 257th variant is a `SchemaVersion` bump because the on-wire width changes; `DigestVersion` only bumps if the digest *algorithm* changes). Decode of an out-of-range integer value MUST fail with `ERR_DS_SCHEMA_INCOMPATIBLE` (covered by EC-016-002) |
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
| `DOMAIN_TAG` | u8 | 1 |
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
- A draw is computed as `SipHash-2-4-64((k0,k1), DOMAIN_TAG_RNGDRAW ∥ StreamKey ∥ actionOrdinal ∥ drawIndex)` where `DOMAIN_TAG_RNGDRAW = 0x13` (§3.2.4.1), `drawIndex ∈ [0, count)`, and `actionOrdinal` is the value at entry to the evaluation (i.e. before the post-evaluation `actionOrdinal += 1`). `RngCursor` is NOT in the SipHash input; it is bookkeeping for snapshot/replay state and corroboration only.
- After the evaluation completes, `RngCursor += count` and `actionOrdinal += 1`.
- Both `actionOrdinal` and `RngCursor` MUST be serialized per stream in snapshot/replay state and restored on load.
- Entity despawn retains a tombstone record `(EntityId, finalActionOrdinal, finalRngCursor)` in a despawn log keyed by `EntityId`; respawn with a new `EntityId` allocates a fresh stream with `actionOrdinal=0`, `RngCursor=0`.
- Reuse of an `EntityId` after despawn within the same match is forbidden. **Cross-spec normative constraint:** entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee `EntityId` uniqueness for the lifetime of a match; once an `EntityId` is despawned it MUST NOT be reassigned. Violation breaks stream isolation and replay parity. This constraint was back-propagated to **Agent Movement #2 §2.5 (`XC-002-001`)** and **Decision Tree #8 §1.7.3 (`XC-008-001`)** on May 6, 2026 (both v1.1.1 non-behavioral patches). Tracking: filed as **`ERR-016-002`** in `docs/tracking/spec-error-log.md` (numeric-suffix form per CLAUDE.md `ERR-NNN` taxonomy; the verbal-suffix `ERR-016-EntityId-NoReuse` is deprecated and MUST NOT be used in new references); resolved at the spec-text level by the May 6 patches.

### 3.2.5.3 Despawn tombstone log (normative)
The tombstone log enforcing the no-reuse constraint is **part of authoritative state** and Tier A. Its layout and lifecycle:

| Property | Value |
|---|---|
| Data structure | `DespawnLog : array<DespawnEntry>` where `DespawnEntry { entityId : u32, finalActionOrdinal : u64, finalRngCursor : u64, despawnTick : u64 }` |
| Tier | **Tier A** — bitwise equality required across replay |
| Snapshot inclusion | Included in `SnapshotPayload` after the per-stream RNG cursor table; serialized in canonical order (`entityId` ascending) per §3.2.4.1 `array<T>` rules |
| Phase ownership | Written by `Resolve` (which finalizes despawns); read-only in `Snapshot`; immutable in all other phases per §3.6.1 |
| Lifecycle | Append-only within a match. Cleared at match-finalization boundary per §3.2.5.2; not carried across match boundaries |
| Replay semantics | Restored in §4.2.2 step 5 (rehydrate authoritative state) along with all Tier A fields; absence after a save that contained tombstones is `ERR_DS_SCHEMA_INCOMPATIBLE` |

A save scheduled after one or more despawns MUST include the tombstone entries in the snapshot payload; replay from that save MUST restore the tombstones before any T+1 spawn/despawn evaluation can run, otherwise the no-reuse constraint cannot be enforced under continued execution.

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
| `PHYSICS_DT` | `0x3C888889` (f32 bit pattern of `1.0f / 60.0f`) | [DERIVED] | Physics tick interval. Computation rule: `(float)(1.0 / 60.0)` evaluated under round-to-nearest-even; the literal bit pattern `0x3C888889` is the normative reference value (derivation in §3.4.3). Implementations MUST NOT use `0.0166666675f` literals or pre-baked constants — they MUST compute from `1.0f / (float)PHYSICS_TICK_HZ` to match the bit pattern exactly |
| `STRING_MAX_BYTES` | `65536` | [FIXED] | Maximum UTF-8 byte length for any `string` field in canonical serialization (§3.2.4.1) |
| `HASH_INPUT_FIELD_WIDTHS` | (table in §3.2.4.1) | [FIXED] | Normative widths of all hash-input fields. Width changes require `DigestVersion` bump |
| `NAN_CANONICAL_F32` | `0x7FC00000` | [FIXED] | Canonical quiet-NaN bit pattern for Tier B `f32` fields (§3.2.4.1) |
| `NAN_CANONICAL_F64` | `0x7FF8000000000000` | [FIXED] | Canonical quiet-NaN bit pattern for Tier B `f64` fields (§3.2.4.1) |
| `ZERO_CANONICAL_F32` | `0x00000000` | [FIXED] | Canonical zero bit pattern for Tier A and Tier B `f32` fields; `-0.0` (`0x80000000`) MUST be normalized to this value before serialization (§3.2.4.1) |
| `ZERO_CANONICAL_F64` | `0x0000000000000000` | [FIXED] | Canonical zero bit pattern for `f64` fields; `-0.0` MUST be normalized to this value before serialization (§3.2.4.1) |
| `ERR_DS_ENV_MUTATION` | `0x160D` | [FIXED] | Recording-side `EnvironmentFingerprint` mutated mid-match (§4.8.1). Distinct from `ERR_DS_REPLAY_ENV_MISMATCH` (0x1604) which is replay-side fingerprint divergence |
| `UNICODE_NFC_VERSION` | `"15.1"` | [FIXED] | Unicode normalization-table version pinned for `string` NFC encoding (§3.2.4.1). Recorded in `EnvironmentFingerprint` (§4.8) so a Unicode-table upgrade triggers a deterministic `ERR_DS_REPLAY_ENV_MISMATCH` rather than a silent digest drift |
| `DOMAIN_TAG_PHASE` | `0x10` | [FIXED] | Hash-domain tag for `PhaseDigest` preimage (§3.2.2, §3.2.4.1) |
| `DOMAIN_TAG_SNAPSHOT_PAYLOAD` | `0x11` | [FIXED] | Hash-domain tag for `SnapshotPayload` preimage |
| `DOMAIN_TAG_SNAPSHOT_HEADER` | `0x12` | [FIXED] | Hash-domain tag for `SnapshotHeader` preimage |
| `DOMAIN_TAG_RNGDRAW` | `0x13` | [FIXED] | Hash-domain tag for SipHash-2-4 per-draw input (§3.2.5) |
| `DOMAIN_TAG_ENV_FP` | `0x14` | [FIXED] | Hash-domain tag for `EnvironmentFingerprint` / `floatModelHash` preimage (§4.8.3) |
| `DOMAIN_TAG_EVENT_LEDGER` | `0x15` | [FIXED] | Hash-domain tag for the `Events`-phase `phaseScopeFields` preimage consumed by Event System #17 §3.4.2 / FM-017-001 (`SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord)`). Allocated as the next-available value after `DOMAIN_TAG_ENV_FP = 0x14` per ERR-017-001 (`docs/tracking/spec-error-log.md`); promotes #17 §3.10 row from `[CROSS-PENDING]` to `[CROSS]` atomically with #16 Tier 2 `APPROVED`. Owned by #16's tag namespace; #17 consumes read-only |
| `DOMAIN_TAG_HEADING` | `0x16` | [FIXED] | Hash-domain tag for Heading Mechanics #10 RNG draw sites (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) per #10 §3.4 / §3.7 / §4.4. Allocated as the next-available value after `DOMAIN_TAG_EVENT_LEDGER = 0x15` per ERR-010-001 (`docs/tracking/spec-error-log.md`); promotes #10 §3.1 row from `[CROSS-PENDING]` to `[CROSS]` atomically with this allocation. Owned by #16's tag namespace; #10 consumes read-only |
| `DOMAIN_TAG_PRESSING_AI` | `0x19` | [FIXED] | Hash-domain tag for Pressing AI #13 RNG draw sites. Allocated as `0x19` within the ERR-012-001 Phase B/C block (`0x17…0x1C`); `0x17` allocated to Positioning AI #12 (ERR-012-001, May 18, 2026) and `0x1D` allocated to Goalkeeper Mechanics #11 (ERR-011-001, May 18, 2026; shifted from originally-reserved `0x18` because #12 reached `APPROVED` first per first-to-`APPROVED` precedent). Resolves ERR-013-005 (`docs/tracking/spec-error-log.md`); promotes #13 §6.1 `DOMAIN_TAG_PRESSING_AI` row from `[CROSS-PENDING]` to `[CROSS]` atomically with this allocation. Owned by #16's tag namespace; #13 consumes read-only |
| `DOMAIN_TAG_POSITIONING_AI` | `0x17` | [FIXED] | Hash-domain tag for Positioning AI #12 RNG draw sites (determinism-scoped per-tick phase and slot computation). Allocated as `0x17` within the ERR-012-001 Phase B/C block (`0x17…0x1C`); #12 reached `APPROVED` first on May 18, 2026, claiming `0x17` per first-to-`APPROVED` precedent. Resolves ERR-012-001 (`docs/tracking/spec-error-log.md`); promotes #12 §6.1 `DOMAIN_TAG_POSITIONING_AI` row from `[CROSS-PENDING]` to `[CROSS]` atomically with #12 `APPROVED` transition. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #12 consumes read-only |
| `_RESERVED_0x18_` | `0x18` | [FIXED] | Skipped. Originally informally noted in §3.5 v1.0.3 changelog as a reservation for Goalkeeper Mechanics #11 (ERR-011-001). #11 was subsequently reallocated to `0x1D` when Positioning AI #12 reached `APPROVED` first and claimed `0x17` per first-to-`APPROVED` precedent (ERR-011-001 / KD-7 policy). Value `0x18` is permanently orphaned — MUST NOT be assigned to any subsystem without explicit ERR tracking. Resolves ERR-016-003 (`docs/tracking/spec-error-log.md`). |
| `DOMAIN_TAG_GOALKEEPER` | `0x1D` | [FIXED] | Hash-domain tag for Goalkeeper Mechanics #11 RNG draw sites (4 draw sites: `DRAW_SITE_DIVE_TIMING_JITTER`, `DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_POINT_ERROR`, `DRAW_SITE_CROSS_CLAIM_TIEBREAK` per #11 §3.4 / §4.4). Allocated as `0x1D` per ERR-011-001 (`docs/tracking/spec-error-log.md`); value shifted from originally-proposed `0x17` to `0x1D` because Positioning AI #12 reached `APPROVED` first on May 18, 2026, claiming `0x17` (first-to-`APPROVED` precedent per ERR-011-001 KD-7 policy). Next available value after `DOMAIN_TAG_ATTACKING_AI = 0x1B`. Resolves ERR-011-001; promotes #11 §3.4.9 `DOMAIN_TAG_GOALKEEPER` row from `[CROSS-PENDING]` to `[CROSS]` atomically with #11 `APPROVED` transition. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #11 consumes read-only |
| `DOMAIN_TAG_DEFENSIVE_AI` | `0x1A` | [FIXED] | Hash-domain tag for Defensive AI #14 RNG draw sites (stochastic tie-breaking in mark-assignment). Allocated as `0x1A` within the ERR-012-001 Phase B/C block (`0x17…0x1C`). Resolves ERR-014-004 (`docs/tracking/spec-error-log.md`); promotes #14 §6.1 `DOMAIN_TAG_DEFENSIVE_AI` row from `[CROSS-PENDING]` to `[CROSS]` atomically with #14 `APPROVED` transition (May 18, 2026). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #14 consumes read-only |
| `DOMAIN_TAG_ATTACKING_AI` | `0x1B` | [FIXED] | Hash-domain tag for Attacking AI #15 RNG draw sites (stochastic tie-breaks in `DeterministicRngService`). Allocated as `0x1B` within the ERR-012-001 Phase B/C block (`0x17…0x1C`). Resolves ERR-015-001 (`docs/tracking/spec-error-log.md`); promotes #15 §6.1.9 `DOMAIN_TAG_ATTACKING_AI` row from `[CROSS-PENDING]` to `[CROSS]` atomically with #15 `APPROVED` transition (May 18, 2026). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #15 consumes read-only |
| `_RESERVED_0x1C_` | `0x1C` | [FIXED] | Skipped. Block-end margin value of the ERR-012-001 Phase B/C block (`0x17…0x1C`). Block closed at `0x1B` (Attacking AI #15). Value `0x1C` was never assigned to any subsystem and is permanently orphaned — MUST NOT be assigned without explicit ERR tracking. Resolves ERR-016-003 (`docs/tracking/spec-error-log.md`). |
| `DOMAIN_TAG_LIVING_WORLD` | `0x1E` | [FIXED] | Hash-domain tag for Living World #22 off-pitch RNG draw sites (the aperiodic `world.text` interaction-text sub-stream; the `world.arcs` sub-stream stays a documented KD-10 seam until its trigger producers exist). Next available value after `DOMAIN_TAG_GOALKEEPER = 0x1D` (the `0x17…0x1C` Phase B/C block is closed). Opens the off-pitch subsystem-ordinal band (`SubsystemOrdinals.LivingWorld = 80`, band 80–99, disjoint from the match Physics/Mechanics/AI bands per §3.1.1). Resolves ERR-022-001 (`docs/tracking/spec-error-log.md`); the `0x1E` / `80` allocation landed in code (`DeterministicSimConstants` / `SubsystemOrdinals`) with #22's slice-3 wiring, and this spec-text row + ERR were filed retroactively to record it. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #22 consumes read-only |
| `DOMAIN_TAG_PLAYER_DATABASE` | `0x1F` | [FIXED] | Hash-domain tag for Squad/Player Data Layer #27 roster-generation RNG draws (`RosterGenerator`, siteId `player-database.roster-generation`, `entityId = clubId`; a boot / off-match-tick draw site, not a per-tick one). Next available value after `DOMAIN_TAG_LIVING_WORLD = 0x1E`; off-pitch subsystem-ordinal band (`SubsystemOrdinals.PlayerDatabase = 81`, alongside `LivingWorld = 80`). Resolves ERR-027-001 (`docs/tracking/spec-error-log.md`); the `0x1F` / `81` allocation landed in code with #27 T0, and this row confirms the #27 Appendix A `DOMAIN_TAG_PLAYER_DATABASE` / `SubsystemOrdinals.PlayerDatabase` `[CROSS]` cross-cite. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #27 consumes read-only |
| `DOMAIN_TAG_PLAYER_PROGRESSION` | `0x20` | [FIXED] | Hash-domain tag for Player Progression & Lifecycle #28 off-pitch RNG draws (regen/newgen generation, siteId `player-progression.regen`, `entityId = clubId` — a **per-club** stream, the #27 `RosterGenerator` pattern; a world-tick / off-match-tick draw site, not a per-tick one). Aging/decline/growth of existing players is a pure deterministic integer projection and registers **no** stream (`0x20` covers regen generation only, #28 §4.3/§5). Value `0x20` per roadmap §6 (this promotes the former `_RESERVED_0x20_` placeholder). Off-pitch subsystem-ordinal band (`SubsystemOrdinals.PlayerProgression = 82`, alongside `LivingWorld = 80` / `PlayerDatabase = 81`). Resolves ERR-028-001 (`docs/tracking/spec-error-log.md`). Like `DOMAIN_TAG_SEASON_LOOP = 0x22` (and unlike `0x1E`/`0x1F`, code-first), this row **reserves the namespace at #28's section-file approval** — the code const + per-club RNG-stream registration land at #28 T2 (the first regen; registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids, the `world.arcs` precedent). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #28 consumes read-only |
| `_RESERVED_0x21_` | `0x21` | [FIXED] | **Reserved — held for Training System #29 per roadmap §6 (`SubsystemOrdinals` 83); MUST NOT be reused.** #29 was authored + APPROVED July 23, 2026 and **confirmed FULLY DETERMINISTIC** — conditioning / training-fatigue / growth-input are pure integer projections, and per-player variation is a deterministic function of the player's own attributes — so **it registers NO RNG stream and this row was deliberately NOT promoted** (ERR-029-001; unlike `0x20`/#28, whose regen is a genuine draw site). Growth flows through #28's deterministic curve; injury variation is #41's. A named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids (`world.arcs` precedent). The reservation stands for a *future* stochastic training extension, which would promote it at that first draw site. No code const. |
| `DOMAIN_TAG_SEASON_LOOP` | `0x22` | [FIXED] | Hash-domain tag for Season & Competition Loop #30 off-pitch RNG draws (the season RNG sub-stream, siteId `season-loop.season-events`, `entityId = seasonNumber`; a world-tick / off-match-tick draw site, not a per-tick one — the FR-SN-013a/§3.4.1 non-managed-fixture "quick-sim" round-resolution model is its first consumer, keyed on `(seed, seasonNumber, roundIndex, homeClubId, awayClubId)`). Value `0x22` per roadmap §6 (the block skips `0x20`/`0x21`, reserved above for #28/#29). Off-pitch subsystem-ordinal band (`SubsystemOrdinals.SeasonLoop = 84`). Resolves ERR-030-001 (`docs/tracking/spec-error-log.md`). Unlike `0x1E`/`0x1F` (code-first, row filed retroactively), this row **reserves the namespace at #30's section-file approval** — the code const + RNG-stream registration land at #30 T2 (the first draw site; registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids, the `world.arcs` precedent). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #30 consumes read-only |

| `_RESERVED_0x23_` | `0x23` | [FIXED] | **Reserved — held for Transfers, Contracts & Negotiation #31 per roadmap §6 (`SubsystemOrdinals` 85); MUST NOT be reused.** Placeholder pending #31's promotion (the A-04 every-gap-has-a-placeholder rule + the `_RESERVED_0x20_`/`0x21_` precedent — #40/#41 reached the catalogue before #31–#36). Promotes to a named tag at #31's first draw site, or stays reserved if #31's minimal tier proves draw-free (the #29/#40 pattern). No code const. |
| `_RESERVED_0x24_` | `0x24` | [FIXED] | **Reserved — held for Scouting & Player Knowledge #32 per roadmap §6 (`SubsystemOrdinals` 86); MUST NOT be reused.** Placeholder pending #32's promotion (A-04 gap rule). Scouting accuracy is a documented draw site (roadmap §3 #32), so this is expected to promote at #32's first draw. No code const. |
| `_RESERVED_0x25_` | `0x25` | [FIXED] | **Reserved — held for Personalities, Morale & Squad Dynamics #33 per roadmap §6 (`SubsystemOrdinals` 87); MUST NOT be reused.** Placeholder pending #33's promotion (A-04 gap rule). No code const. |
| `_RESERVED_0x26_` | `0x26` | [FIXED] | **Reserved — held for Staff & Backroom #34 per roadmap §6 (`SubsystemOrdinals` 88); MUST NOT be reused.** Placeholder pending #34's promotion (A-04 gap rule). No code const. |
| `_RESERVED_0x27_` | `0x27` | [FIXED] | **Reserved — held for Media & Press Interactions #35 per roadmap §6 (`SubsystemOrdinals` 89); MUST NOT be reused.** Placeholder pending #35's promotion (A-04 gap rule). No code const. |
| `_RESERVED_0x28_` | `0x28` | [FIXED] | **Reserved — held for National Teams & International Management #36 per roadmap §6 (`SubsystemOrdinals` 90); MUST NOT be reused.** Placeholder pending #36's promotion (A-04 gap rule). No code const. |
| `_RESERVED_0x29_` | `0x29` | [FIXED] | **Reserved — held for Club Finances & Economy #40 per roadmap §6 (`SubsystemOrdinals.ClubFinances = 91`); MUST NOT be reused.** #40 was authored + APPROVED July 23, 2026 and its **minimal tier is fully deterministic** — `budget = f(finalTablePosition, prizeMoney)` is a pure integer projection with no draw — so **it registers NO RNG stream and this row was deliberately NOT promoted** (ERR-040-001; the #29 `_RESERVED_0x21_` precedent). A named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids (`world.arcs`). The reservation stands for #40's **deep-tier stochastic sponsorship/revenue variance** (the genuine first draw site), which promotes it at #40 T3 — keyed position-independently on `(clubId, seasonNumber, purpose)`. No code const. |
| `DOMAIN_TAG_INJURIES_MEDICAL` | `0x2A` | [FIXED] | Hash-domain tag for Injuries & Medical #41 off-pitch RNG draws (the `injuries.occurrence` world-tick sub-stream, siteId `injuries.occurrence`, `entityId = playerId`; a world-tick / off-match-tick draw site, not a per-tick one). **All #41 draws are position-independent / keyed** on `(playerId, worldDay, purpose)` via a fixed-radix action-ordinal bijection (#41 §3.1.1) — no free-running cursor, so nothing is serialized; the match tick never draws for #41 (#41 KD-1). Value `0x2A` per roadmap §6 (the intervening `0x23`–`0x28` are reserved placeholders for #31–#36 and `0x29` for #40 — `0x29`/#40 stays a `_RESERVED_` placeholder while #40's minimal tier is draw-free; #37–#39 are read-only/presentation/infra and take no tag). Off-pitch subsystem-ordinal band (`SubsystemOrdinals.InjuriesMedical = 92`). Resolves ERR-041-001 (`docs/tracking/spec-error-log.md`). Like `DOMAIN_TAG_SEASON_LOOP = 0x22` (spec-text-first, unlike the code-first `0x1E`/`0x1F`), this row **reserves the namespace at #41's section-file approval** — the code const + RNG-stream registration land at #41 T2 (the first draw site; registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids, the `world.arcs` precedent). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Owned by #16's tag namespace; #41 consumes read-only |

### 3.4.1 Reserve budget enforcement (normative)
`Reserve(siteId, count)` MUST validate `count` against the budget registered for `siteId` in the draw-site registry (§3.6.2). On mismatch, `Reserve` MUST fail the tick commit with `ERR_DS_RNG_BUDGET_MISMATCH` and MUST NOT advance `RngCursor` or `actionOrdinal`. Silent acceptance of a divergent count is forbidden — it would silently break replay parity across builds.

### 3.4.2 Tier B comparator default policy
`TIER_B_DEFAULT_COMPARATOR` declares the comparator *class* (`AbsEpsilon`) but does NOT supply a default tolerance magnitude. A Tier B field that appears in a digest scope without a matching tolerance row in the tolerance matrix MUST fail validation with `ERR_DS_TIERB_TOLERANCE_MISSING`. Implementations MUST NOT silently substitute a fallback epsilon.

### 3.4.3 PHYSICS_DT bit-pattern derivation (normative)
`PHYSICS_DT = (float)(1.0 / 60.0)` evaluated under round-to-nearest-even. The literal bit pattern `0x3C888889` is verified as follows:

1. `1/60` in binary is the repeating fraction `1.0001000100010001000100010001…₂ × 2⁻⁶` (period 4).
2. IEEE-754 binary32 stores 23 mantissa bits after the implicit leading 1. Pre-round 23-bit mantissa: `00010001000100010001000`.
3. The 24th (round) bit is `1`; the trailing bits (sticky) are non-zero. Round-to-nearest-even with round=`1` and sticky non-zero rounds **up**.
4. Post-round mantissa: `00010001000100010001001`.
5. Biased exponent: `-6 + 127 = 121 = 01111001₂`. Sign bit: `0`.
6. Concatenated: `0 ‖ 01111001 ‖ 00010001000100010001001` = `0011 1100 1000 1000 1000 1000 1000 1001` = `0x3C888889`.

Reference verification: `python3 -c "import struct; print(hex(struct.unpack('<I', struct.pack('<f', 1.0/60.0))[0]))"` MUST print `0x3c888889`. Any literal other than `0x3C888889` is a fabricated constant and MUST be rejected at code review and at the §3.4 KAT gate (Pass 4 X-1).

**Numeric-literal review gate (normative).** Every numeric literal in §3.4 MUST be cross-checked against either (a) a programmatically-generated KAT or (b) an appendix derivation before the constant is added. Visual review of hex literals is insufficient (regression class identified by Pass 4 C-1).

## 3.5 Version History
- **v1.0.13 (July 23, 2026):** Patch revision (adversarial-review fix). Added the six `_RESERVED_0x23_`…`_RESERVED_0x28_` placeholder rows (`SubsystemOrdinals` 85–90, held for #31/#32/#33/#34/#35/#36 per roadmap §6) to §3.4 — the A-04 "every allocation gap must have an explicit placeholder" rule + §3.1.1 ordinal-contiguity requirement were violated when the #40 (`0x29`/91) and #41 (`0x2A`/92) approvals (v1.0.11/v1.0.12) allocated past the `0x22`/84 block without reserving the intervening `0x23`–`0x28` / 85–90 values (the `_RESERVED_0x20_`/`0x21_` precedent set at #30's landing). Each promotes at its spec's first draw site (or stays reserved if draw-free, the #29/#40 pattern). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump.
- **v1.0.12 (July 23, 2026):** Patch revision. Added the `_RESERVED_0x29_` placeholder row (`SubsystemOrdinals.ClubFinances = 91`) to §3.4 at Club Finances & Economy #40's section-file approval — **reserved, NOT promoted** (ERR-040-001; the #29 `_RESERVED_0x21_` precedent): #40's minimal tier is a pure integer budget projection with no draw, so it registers no stream, and a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids. The reservation promotes to `DOMAIN_TAG_CLUB_FINANCES = 0x29` at #40 T3's first stochastic sponsorship/revenue draw (keyed on `(clubId, seasonNumber, purpose)`). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. All other constants unchanged.
- **v1.0.11 (July 23, 2026):** Patch revision. Added the `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals.InjuriesMedical = 92` row to §3.4 at Injuries & Medical #41's section-file approval (the `injuries.occurrence` world-tick sub-stream; all draws position-independent/keyed on `(playerId, worldDay, purpose)` via a fixed-radix bijection — no serialized cursor, no match-tick draw). Value `0x2A` per roadmap §6; resolves ERR-041-001. Spec-text-first like `0x22`/`0x20` — the code const + stream registration land at #41 T2 with the first draw site (FR-LW-031 — no phantom stream). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.10 (July 23, 2026):** Patch revision. Updated the `_RESERVED_0x21_` row rationale at Training System #29's section-file approval: #29 was authored + APPROVED and **confirmed fully deterministic** (conditioning / training-fatigue / growth-input are pure integer projections; per-player variation is a deterministic own-attribute function), so it registers **no** RNG stream and the row was **deliberately NOT promoted** — `_RESERVED_0x21_` / `SubsystemOrdinals` 83 stay reserved (ERR-029-001). Unlike `0x20`/#28 (regen = a genuine draw site), #29 has no #29-owned stochastic outcome; a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 avoids. No code const, no new row, no `DETERMINISM_DIGEST_VERSION` bump (rationale text only). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.9 (July 23, 2026):** Patch revision. **Promoted** the `_RESERVED_0x20_` placeholder row (added in v1.0.8) to `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` at Player Progression & Lifecycle #28's section-file approval — the per-club regen/newgen RNG stream (siteId `player-progression.regen`, `entityId = clubId`, the #27 `RosterGenerator` per-club pattern; `SubsystemOrdinals.PlayerProgression = 82`). Aging/decline/growth of existing players is a pure deterministic integer projection and registers no stream, so `0x20` covers regen generation only. Resolves ERR-028-001. Spec-text-first like `DOMAIN_TAG_SEASON_LOOP = 0x22` (not code-first like `0x1E`/`0x1F`): the code const + per-club stream registration land at #28 T2 with the first regen (FR-LW-031 — no phantom stream). `_RESERVED_0x21_` (Training #29) stays a placeholder until #29 promotes. Pure namespace promotion; no `DETERMINISM_DIGEST_VERSION` bump (the catalogue changed a placeholder to a named row; no preimage layout, field width, or hash-input rule changed). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.8 (July 22, 2026):** Patch revision. Added three §3.4 catalogue rows for the management-layer roadmap's off-pitch determinism block: `DOMAIN_TAG_SEASON_LOOP = 0x22` (Season & Competition Loop #30 season RNG sub-stream, siteId `season-loop.season-events`, `SubsystemOrdinals.SeasonLoop = 84`; first consumer = the FR-SN-013a quick-sim round-resolution model; resolves ERR-030-001, filed at #30's section-file approval — unlike `0x1E`/`0x1F` this row PRECEDES the code const, which lands at #30 T2 with the first draw site) plus two reserved-pending-promotion placeholder rows `_RESERVED_0x20_` (Player Progression #28) / `_RESERVED_0x21_` (Training #29) marking the gap the roadmap §6 contiguous-block reservation leaves because #30 (Wave 1) reached the catalogue before #28/#29 (Wave 2) — distinct from the permanently-orphaned `0x18`/`0x1C` (these WILL be assigned when #28/#29 promote). All three are pure namespace allocations in #16's tag-namespace; no `DETERMINISM_DIGEST_VERSION` bump (the catalogue grew; no preimage layout, field width, or hash-input rule changed). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.7 (July 22, 2026):** Patch revision. Added the two off-pitch domain-tag rows to the §3.4 catalogue: `DOMAIN_TAG_LIVING_WORLD = 0x1E` (Living World #22 `world.text`/`world.arcs` sub-streams; `SubsystemOrdinals.LivingWorld = 80` — opens the off-pitch 80–99 band; resolves ERR-022-001, filed retroactively since the `0x1E`/`80` allocation had landed in code with #22's slice-3 wiring) and `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` (Squad/Player Data Layer #27 `RosterGenerator` roster-generation stream; `SubsystemOrdinals.PlayerDatabase = 81`; resolves ERR-027-001, confirming #27's Appendix A `[CROSS]` cross-cite at its promotion). Both are pure namespace allocations in #16's tag-namespace, boot / off-match-tick draw sites; no `DETERMINISM_DIGEST_VERSION` bump (the catalogue grew; no preimage layout, field width, or hash-input rule changed). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.6 (May 18, 2026):** Patch revision. Added `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows to §3.4 domain-tag table per A-04 probe requirement (every allocation gap must have an explicit placeholder). `0x18` was informally noted in v1.0.3 changelog as a GK reservation before GK shifted to `0x1D`; `0x1C` was the block-end margin of the ERR-012-001 Phase B/C block. Both values are permanently orphaned per ERR-016-003. No `DETERMINISM_DIGEST_VERSION` bump (pure documentation — no preimage layout change).
- **v1.0.5 (May 18, 2026):** Patch revision. Added `DOMAIN_TAG_POSITIONING_AI = 0x17`, `DOMAIN_TAG_GOALKEEPER = 0x1D`, and `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` to §3.4 constants catalogue. Resolves ERR-012-001 (#12 Positioning AI `APPROVED` first, claims `0x17`; #11 Goalkeeper shifts to `0x1D`), ERR-011-001 (#11 Goalkeeper allocated `0x1D`), and ERR-014-004 (#14 Defensive AI allocated `0x1A`). Updated `DOMAIN_TAG_PRESSING_AI` row to reflect that `0x17` and `0x1D` are now allocated (no longer reserved). Pure namespace allocations; no `DETERMINISM_DIGEST_VERSION` bump.
- **v1.0.4 (May 18, 2026):** Patch revision. Added `DOMAIN_TAG_ATTACKING_AI = 0x1B` to §3.4 constants catalogue, allocated as `0x1B` within the ERR-012-001 `0x17…0x1C` Phase B/C block (`0x1A` reserved for #14 Defensive AI per ERR-014-004, `IN REVIEW`). Resolves ERR-015-001 (cross-spec back-prop from Attacking AI #15 §6.1.9): #15 declared the tag as `[CROSS-PENDING]` pending #16 allocation; #15's row promotes `[CROSS-PENDING]` → `[CROSS]` atomically with #15 `APPROVED` transition. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump.
- **v1.0.3 (May 17, 2026):** Patch revision. Added `DOMAIN_TAG_PRESSING_AI = 0x19` to §3.4 constants catalogue, allocated as `0x19` within the ERR-012-001 `0x17…0x1C` Phase B/C block (`0x17` reserved for #12 Positioning AI per ERR-012-001; `0x18` reserved for #11 Goalkeeper per ERR-011-001). Resolves ERR-013-005 (cross-spec back-prop from Pressing AI #13 §6.1): #13 declared the tag as `[CROSS-PENDING]` pending #16 allocation; #13's row promotes `[CROSS-PENDING]` → `[CROSS]` atomically with this patch. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump.
- **v1.0.2 (May 16, 2026):** Patch revision. Added `DOMAIN_TAG_HEADING = 0x16` to §3.4 constants catalogue, allocated as the next value after `DOMAIN_TAG_EVENT_LEDGER = 0x15`. Resolves ERR-010-001 (cross-spec back-prop from Heading Mechanics #10 §3.1 / §3.4 / §3.7 / §4.4): #10 declared the tag as `[CROSS-PENDING]` pending #16 allocation; #10's row promotes `[CROSS-PENDING]` → `[CROSS]` atomically with this allocation. Follows the v1.0.1 precedent. No behavioral change — pure namespace allocation in #16's tag-namespace. No `DETERMINISM_DIGEST_VERSION` bump (the catalogue grew; no existing preimage layout, field width, or hash-input rule changed). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0.1 (May 14, 2026):** Patch revision. Added `DOMAIN_TAG_EVENT_LEDGER = 0x15` to §3.4 constants catalogue, allocated as the next value after `DOMAIN_TAG_ENV_FP = 0x14`. Resolves ERR-017-001 (cross-spec back-prop from Event System #17 §3.4.2 / §3.10): #17 declared the tag as `[CROSS-PENDING]` pending #16 allocation; #17's row promotes `[CROSS-PENDING]` → `[CROSS]` atomically with #16 Tier 2 `APPROVED`. No behavioral change — pure namespace allocation in #16's tag-namespace. No `DETERMINISM_DIGEST_VERSION` bump (the catalogue grew; no existing preimage layout, field width, or hash-input rule changed). All other constants, formulas, and §3.4.x sub-rules unchanged.
- **v1.0 (May 4, 2026):** Fourth- and fifth-pass adversarial-critique resolution (Pass 4 + Pass 5 in `critique-log.md`). Highlights: (a) Pass 4 C-1: `PHYSICS_DT` corrected from fabricated `0x3C8888B7` to derived `0x3C888889`; §3.4.3 worked derivation added; numeric-literal review gate stated. (b) Pass 5 C-1: `SnapshotDigest` preimage field order pinned to §2.3 declaration order in §3.2.3 and §3.9.2 reworked to a normative on-disk record layout. (c) Pass 5 C-2: `DOMAIN_TAG_PHASE` (0x10) added to `PhaseDigest` formula in §3.2.2; `DOMAIN_TAG_RNGDRAW` (0x13) added to per-draw SipHash input in §3.2.4 / §3.2.5; domain-tag constants added to §3.4. (d) Pass 4 C-3a: EC-016-009..014 added to §3.10 covering `ERR_DS_REPLAY_BOUNDARY`, `ERR_DS_TIERB_NONFINITE`, `ERR_DS_RNG_BUDGET_MISMATCH`, `ERR_DS_STORAGE_ATOMICITY`, the new `ERR_DS_ENV_MUTATION` (Pass 4 M-3), and signed-zero Tier-A normalization (Pass 5 M-2). (e) Pass 4 M-2: `array<T>` width formula corrected for variable-width T. (f) Pass 4 M-3: `ERR_DS_ENV_MUTATION` (0x160D) added for recording-side fingerprint mutation. (g) Pass 4 L-1: NFC pinned to Unicode 15.1 via `UNICODE_NFC_VERSION`; Stage-0 strings restricted to ASCII; Unicode version added to `EnvironmentFingerprint` (§4.8). (h) Pass 5 H-2: HKDF `info` byte encoding pinned to raw 13 ASCII bytes (no `string` framing); `salt=NULL` semantics pinned per RFC 5869 §2.2. (i) Pass 5 M-1: `enum` width-immutability axis rebound from `DigestVersion` to `SchemaVersion`. (j) Pass 5 M-2: `-0.0` normalized to `+0.0` for both Tier A and Tier B `f32`/`f64` before serialization; `ZERO_CANONICAL_F32`/`F64` constants added. (k) Pass 5 M-3: §3.2.5.3 despawn tombstone log declared part of authoritative state, Tier A, included in `SnapshotPayload`, written by `Resolve`. (l) Pass 5 L-1: EC-016-001 trigger generalized to "any boundary other than `EndOfSnapshot`". (m) Pass 5 L-2: `ERR-016-EntityId-NoReuse` formally deprecated in favor of numeric `ERR-016-002` per CLAUDE.md taxonomy. (n) Pass 5 L-4: `bool`/`optional<T>`/`enum` decode-side error binding to `ERR_DS_SCHEMA_INCOMPATIBLE` made explicit. (o) Pass 5 M-5: §3.6.2.1 added — full draw-site registry operational schema mirroring §2.3.1 style, with stream-version-bump triggers. (p) Pass 4 L-7: `AI_PHASE_STRIDE` heartbeat-and-stride change policy added as a comment in the §3.1.2 canonical tick pseudocode block — any change to `PHYSICS_TICK_HZ`, `TACTICAL_TICK_HZ`, or `AI_PHASE_STRIDE` triggers a `DETERMINISM_DIGEST_VERSION` bump and invalidates pre-existing replay corpus and golden vectors. Mirrored in §7.2 trigger criterion (d). (q) Pass 4 L-8 raised to project-level CLAUDE.md review (out-of-spec) — see Pass 6 fix-log entry in `critique-log.md`.
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
| Resolve | conflict resolution state; **`DespawnLog` append-only writes for entities finalized this tick (§3.2.5.3)** | intent queue |
| Events | event ledger | historical snapshots; `DespawnLog` (read-only after Resolve) |
| Snapshot | serialized bytes + digest; **`DespawnLog` is read-only — included in payload but not mutated here** | live gameplay intent queue; `DespawnLog` writes |

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

### 3.6.2.1 Draw-site registry operational schema (normative)
The registry is the binding artifact for `ERR_DS_RNG_BUDGET_MISMATCH` (§3.4.1) and the §3.2.5.1 stable-declaration-order rule. It MUST be expressed as a constant catalogue at `Sim.Constants.Determinism.DrawSiteRegistry` (per CLAUDE.md "constants live in their designated `.cs` constant catalogues") and MUST conform to the following schema:

| Column | Type | Rule |
|---|---|---|
| `siteId` | string | stable site identifier (e.g. `"AI.DecidePass"`); immutable once published; case-sensitive; ASCII only at Stage 0 |
| `owningSubsystem` | enum | matches a value in `Sim.Constants.Determinism.SubsystemOrdinals` (§3.1.1); used only for traceability — does NOT enter `StreamKey` derivation, which uses the subsystem ordinal |
| `reservedBudget` | u32 | exact `count` value that `Reserve(siteId, count)` MUST be called with; mismatch fails with `ERR_DS_RNG_BUDGET_MISMATCH` |
| `declarationOrdinal` | u32 | compile-time-deterministic position within the registry; sets the §3.2.5.1 intra-stream call order; reordering = `streamVersion` bump |
| `migrationNote` | string | rationale string when the row is changed (added, budget changed, declarationOrdinal changed, retired); empty for new rows |
| `owner` | string | team alias; mandatory |
| `reviewDate` | date (`YYYY-MM-DD`) | last review; MUST be ≤ 180 days old at certification time |
| `streamVersionBumpRequired` | bool | `true` if this row's most-recent change required a `streamVersion` bump (any change to `reservedBudget` or `declarationOrdinal`); cleared after the bump is recorded in §7.2 / `DigestVersion` history |

Example row (illustrative):

| siteId | owningSubsystem | reservedBudget | declarationOrdinal | migrationNote | owner | reviewDate | streamVersionBumpRequired |
|---|---|---|---|---|---|---|---|
| `AI.DecidePass` | `AI` | `3` | `12` | "v1 initial" | `ai-systems` | `2026-05-04` | `false` |

**Immutability rule:** `siteId`, `declarationOrdinal`, and `reservedBudget` are immutable per `streamVersion`. Any change requires a `streamVersion` bump and an update to `DETERMINISM_DIGEST_VERSION` history (§7.2). Removing a row is also a `streamVersion` bump; retired rows MUST remain in the registry as tombstones for the lifetime of the prior stream version.

**Stream-version-bump triggers (normative):** any of (a) adding a new row, (b) changing any of `siteId`/`declarationOrdinal`/`reservedBudget`, (c) retiring a row, (d) reordering existing rows. Adding a new row is a bump because it changes the `actionOrdinal` numbering for any subsequent decision sites in the registry.

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

### 3.9.2 Snapshot record on-disk layout (normative)
A snapshot record on durable storage is the byte-exact concatenation, in order, of the four sections below. Implementations MUST emit and consume this layout exactly; field reordering is a `SchemaVersion` bump.

```
[ SnapshotHeader bytes              ] (variable, schema order from §2.3)
[ SnapshotPayload bytes             ] (variable, canonical schema order)
[ currentSnapshotDigest             ] (32 bytes, raw SHA-256 octets, opaque)
[ recordTrailer                     ] (8 bytes: u64 LE total record size)
```

`SnapshotHeader` field-byte order is the §2.3 schema declaration order: `schemaVersion (u32) ‖ tick (u64) ‖ prevSnapshotDigest (32 bytes) ‖ environmentFingerprint (variable, §4.8)`. All multi-byte integer fields use `SNAPSHOT_PAYLOAD_ENDIANNESS` (§3.4 = `LittleEndian`). The trailing `currentSnapshotDigest` is the SHA-256 output of §3.2.3 stored as raw 32-octet opaque bytes (no endian swap). The `recordTrailer` enables atomic-write integrity checks (§4.6.1.1) without re-parsing the payload.

**Worked example.** Given:
- `PrevSnapshotDigest = 0xAA11…`
- `SnapshotPayloadHash = 0x9F20…`

Digest preimage is composed per §3.2.3: `SerializeCanonical(0x12 ‖ schemaVersion ‖ tick ‖ prevSnapshotDigest ‖ environmentFingerprint) ‖ SerializeCanonical(0x11 ‖ payloadBytes)`. The computed `currentSnapshotDigest` is stored in the trailing 32-byte slot of the on-disk record.

If replayed load at identical tick produces a different digest (e.g., `0x9F21`), classification is `HardDesync` unless field set is explicitly Tier B scoped.

## 3.10 Edge-Case Decision Table
| ID | Case | Trigger | Required behavior | Error/Classification |
|---|---|---|---|---|
| EC-016-001 | Mid-tick / off-boundary save request | save commit attempted at any boundary other than `EndOfSnapshot` (i.e. during any of `Input`, `Intent`, `AI`, `AI_NoOp`, `Physics`, `Resolve`, or `Events`) | deny or defer to legal boundary | `ERR_DS_SAVE_BOUNDARY` |
| EC-016-002 | Unknown enum value on load | schema decode finds out-of-range enum | fail load deterministically | `ERR_DS_SCHEMA_INCOMPATIBLE` |
| EC-016-003 | Missing RNG stream key | stream absent in snapshot | fail replay bootstrap | `ERR_DS_RNG_STREAM_MISSING` |
| EC-016-004 | NaN in Tier A field | decode or runtime emission | reject snapshot/tick commit | `ERR_DS_TIERA_NONFINITE` |
| EC-016-005 | Tier B field without tolerance row | digest scope contains B-tier path with no matching tolerance row | reject digest computation | `ERR_DS_TIERB_TOLERANCE_MISSING` |
| EC-016-006 | Snapshot chain break on resume | `prevSnapshotDigest` does not match expected predecessor | abort replay before rehydration | `ERR_DS_DIGEST_CHAIN_BREAK` |
| EC-016-007 | EnvironmentFingerprint mismatch (replay) | live runtime fingerprint ≠ snapshot fingerprint | abort replay before rehydration | `ERR_DS_REPLAY_ENV_MISMATCH` |
| EC-016-008 | Authoritative write outside WriteSet | phase mutates field not in declared WriteSet | fail tick commit | `ERR_DS_PHASE_OWNERSHIP` |
| EC-016-009 | Replay cursor off `EndOfSnapshot[T]` | step 7 of §4.2.2 finds the replay cursor not positioned at the save boundary before T+1 reapplication | abort replay before reapplying inputs | `ERR_DS_REPLAY_BOUNDARY` |
| EC-016-010 | Tier B `f32`/`f64` non-finite outside canonical NaN | runtime emits Inf or a non-canonical NaN bit pattern in a Tier B field | reject digest computation | `ERR_DS_TIERB_NONFINITE` |
| EC-016-011 | Reservation budget mismatch | `Reserve(siteId, count)` invoked with `count` ≠ registered budget for `siteId` (§3.6.2) | fail tick commit; do not advance `RngCursor` or `actionOrdinal` | `ERR_DS_RNG_BUDGET_MISMATCH` |
| EC-016-012 | Save-commit atomicity failure | atomic-write contract (§4.6.1.1) violated (cross-volume rename, missing fsync, incomplete rename) | abort save; clean up temp file; prior snapshot retained | `ERR_DS_STORAGE_ATOMICITY` |
| EC-016-013 | EnvironmentFingerprint mid-match mutation (recording side) | recording runtime mutates a pinned fingerprint field after match start | fail tick commit; do not write snapshot | `ERR_DS_ENV_MUTATION` |
| EC-016-014 | Tier A `-0.0` vs `+0.0` desync | a Tier A `f32`/`f64` accumulator produces `-0.0` on one path and `+0.0` on another | normalize to `+0.0` per §3.2.4.1 BEFORE serialization; a residual `BitwiseEqual` failure post-normalization is a hard desync | `ERR_DS_TIERA_NONFINITE` does NOT apply (signed zero is finite); classification = `HardDesync` |

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
