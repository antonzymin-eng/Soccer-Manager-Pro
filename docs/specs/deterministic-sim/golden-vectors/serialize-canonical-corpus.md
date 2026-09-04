# SerializeCanonical Reference Corpus

**Purpose:** Verifies the `SerializeCanonical(...)` byte-level encoding contract pinned in Deterministic Simulation Spec #16 §3.2.4.1 and the SHA-256 digest construction over canonical preimages pinned in §3.2.2 / §3.2.3 / §3.2.5. Pinned by §9.5 acceptance criterion #4 (c) of #16.
**Created:** May 14, 2026
**Authority:** #16 `section-3.md` §3.2.4.1 (primitive encoding table, domain-tag table, `HASH_INPUT_FIELD_WIDTHS` registry, worked byte example) and §3.2.2 / §3.2.3 / §3.2.5 (digest preimage formulas).

---

## How this file is consumed

The Stage 0 serializer implementation MUST execute every corpus entry below and produce the listed `bytes` output bit-for-bit; the SHA-256 column is the digest of those bytes (or, where the entry is a top-level digest preimage, the canonical phase / snapshot / RNG-draw / fingerprint digest). A single mismatch is a hard failure of `FR-DS-009-GATE` (Stage 0 certification gate, §16 §5.5). The check is part of the unit-test suite under `Sim.Tests.Determinism.Serialization.SerializeCanonicalCorpusTests` (test card binding TBD — see #16 §5.2 traceability block).

Hex strings below use **lowercase, no separators, no `0x` prefix**. All multi-byte integers are **little-endian** (`SNAPSHOT_PAYLOAD_ENDIANNESS`, §3.4). Length prefixes for `string`, `bytes`, `array<T>`, and `optional<T>` are bound in §3.2.4.1; field widths for hash-input identifiers (`DOMAIN_TAG`, `DigestVersion`, `Tick`, `PhaseId`, `SchemaVersion`, `entityId`, `actionOrdinal`, `drawIndex`, `RngCursor`, `StreamKey`) are bound in `HASH_INPUT_FIELD_WIDTHS` (§3.2.4.1, §3.4).

The corpus is organized by §3.2.4.1 type kind. Every type kind enumerated in the §3.2.4.1 primitive table is covered by ≥1 entry; the §9.5 #4(c) checklist phrasing of "dictionaries with sort-key rule" maps to `array<T>` under the canonical sort rule (§3.1.1 — bytewise-lexicographic ascending) since §3.2.4.1 has no separate `map<K,V>` kind, and "discriminated unions" maps to `optional<T>` (the only discriminated form §3.2.4.1 supports; out-of-range tag bytes are decode-side `ERR_DS_SCHEMA_INCOMPATIBLE` per §3.2.4.1).

Every byte/SHA-256 pair below was produced by the reproducer Python script in Appendix A (a single deterministic script — re-running it MUST regenerate the same hex). No value below was hand-typed without programmatic verification (per CLAUDE.md "Never fabricate verification values").

---

## 1. Primitives — `bool`, integers

§3.2.4.1 rules: `bool` is 1 byte (`0x00`/`0x01`; any other byte = `ERR_DS_SCHEMA_INCOMPATIBLE`). Signed integers are two's-complement, little-endian. Unsigned integers are little-endian.

| ID | Input (structured literal) | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| P-01 | `bool false` | `00` | `6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d` |
| P-02 | `bool true` | `01` | `4bf5122f344554c53bde2ebb8cd2b7e3d1600ad631c385a5d7cce23c7785459a` |
| P-03 | `u8 = 0xAB` | `ab` | `087d80f7f182dd44f184aa86ca34488853ebcc04f0c60d5294919a466b463831` |
| P-04 | `i8 = -1` (two's complement = `0xFF`) | `ff` | `a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89` |
| P-05 | `u16 = 0x1234` | `3412` | `e74d0e44a658ffcdc0ee7266ebd171413b8fcf182c97a27254d9f48abaea6266` |
| P-06 | `i16 = -1` | `ffff` | `ca2fd00fa001190744c15c317643ab092e7048ce086a243e2be9437c898de1bb` |
| P-07 | `u32 = 0x12345678` | `78563412` | `1a2de690568587e6cd9adbd7d9f65ef269becd2f89fb89c224975b0c5944b973` |
| P-08 | `i32 = -1` | `ffffffff` | `ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e` |
| P-09 | `u64 = 0x0123456789ABCDEF` | `efcdab8967452301` | `a85ba2b36261d0dca4b6cbbc840fa8a441ec95200abba5c5623e7ddadeff99e5` |
| P-10 | `i64 = -1` | `ffffffffffffffff` | `12a3ae445661ce5dee78d0650d33362dec29c4f82af05e7e57fb595bbbacf0ca` |

---

## 2. Floats — IEEE-754 with NaN / signed-zero normalization

§3.2.4.1 rules: `f32` / `f64` are IEEE-754 raw bit patterns, little-endian. `-0.0` is normalized to `+0.0` (`ZERO_CANONICAL_F32` = `0x00000000`, `ZERO_CANONICAL_F64` = `0x0000000000000000`) for **both** Tier A and Tier B before serialization (Pass 5 M-2). Tier B NaN is normalized to the canonical quiet-NaN bit pattern (`NAN_CANONICAL_F32` = `0x7FC00000`, `NAN_CANONICAL_F64` = `0x7FF8000000000000`). Tier A rejects NaN/Inf with `ERR_DS_TIERA_NONFINITE`; Tier B rejects non-canonical NaN/Inf bit patterns with `ERR_DS_TIERB_NONFINITE`.

`PHYSICS_DT` (`(float)(1.0/60.0)` = `0x3C888889`, derivation in §3.4.3) is included as F-05 to bind the corpus to the §3.4 numeric-literal review gate (Pass 4 C-1).

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| F-01 | `f32 = +1.0` (bit pattern `0x3F800000`) | `0000803f` | `e00e5eb9444182f352323374ef4e08ebcb784725fdd4fd612d7730540b3e0c8c` |
| F-02 | `f32 = -1.0` (bit pattern `0xBF800000`) | `000080bf` | `c68830a25204a09f8e77aada6bc5807f607cccaaa0ebb2a7122d317584478a8b` |
| F-03 | `f32 = -0.0` (bit pattern `0x80000000`) → normalized to `ZERO_CANONICAL_F32 = 0x00000000` BEFORE serialization | `00000000` | `df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119` |
| F-04 | `f32 = NaN` (Tier B) → normalized to `NAN_CANONICAL_F32 = 0x7FC00000` | `0000c07f` | `ef1eaf26cea96eb18f8fa3137abdf23f52852a855c22ae6f169d21a379dcd739` |
| F-05 | `f32 = PHYSICS_DT = (float)(1.0/60.0)` (bit pattern `0x3C888889`, §3.4.3) | `8988883c` | `8b28d83cb5ddc0a51f24f23094cbfa145fee5d22bd205753291d42ccfef0ccc9` |
| F-06 | `f64 = +1.0` (bit pattern `0x3FF0000000000000`) | `000000000000f03f` | `6c3c396ed6b5c36dcae172271f462051b1266b851e92df3deea8ac65478fd712` |
| F-07 | `f64 = -1.0` (bit pattern `0xBFF0000000000000`) | `000000000000f0bf` | `e77817b649821c634355a917817c1224a360514b1244fe09e832bac4e8ea4440` |
| F-08 | `f64 = -0.0` → normalized to `ZERO_CANONICAL_F64 = 0x0000000000000000` | `0000000000000000` | `af5570f5a1810b7af78caf4bc70a660f0df51e42baf91d4de5b2328de0e83dfc` |
| F-09 | `f64 = NaN` (Tier B) → normalized to `NAN_CANONICAL_F64 = 0x7FF8000000000000` | `000000000000f87f` | `74999fd28ab18ccca2bee199f260d19764603a3c78353d773d16d215eebe8e19` |

---

## 3. Strings (UTF-8, NFC, length-prefixed)

§3.2.4.1 rule: `4 + N` bytes — `u32` byte length (NOT codepoint count) followed by NFC-normalized UTF-8 bytes. Stage-0 authoritative strings are restricted to ASCII (`U+0000`–`U+007F`), making NFC a no-op (Pass 4 L-1). Maximum length `STRING_MAX_BYTES = 65536`; exceeding fails with `ERR_DS_SCHEMA_INCOMPATIBLE`.

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| S-01 | `string ""` (empty; length prefix only) | `00000000` | `df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119` |
| S-02 | `string "abc"` (ASCII; 3 bytes payload) | `03000000616263` | `3da9865b43fa2ec490f78da9db16acd5638704dbce5cc7b3df2e3c7a23addf19` |
| S-03 | `string "System XI"` (ASCII; 17 bytes payload) | `11000000546163746963616c204469726563746f72` | `5cf517dc212e6764ef025056c198fc3b30d4ea3b8388851324ecead15818be3c` |

---

## 4. Bytes (raw, length-prefixed)

§3.2.4.1 rule: `4 + N` bytes — `u32` byte length followed by raw bytes (no NFC, no normalization).

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| B-01 | `bytes ⌀` (empty) | `00000000` | `df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119` |
| B-02 | `bytes 0xCAFEBABE` (4 bytes payload) | `04000000cafebabe` | `ed2b11b284f1d7bc0eb85ec7b84a96b7dd54ea457488170d5334c119abbad0cb` |

---

## 5. Optional&lt;T&gt; (discriminated; absent = `0x00`, present = `0x01 || payload`)

§3.2.4.1 rule: `1 + (0 or width(T))` bytes. Tag byte values other than `0x00` / `0x01` are decode-side `ERR_DS_SCHEMA_INCOMPATIBLE` (Pass 5 L-4). This is the only discriminated-union form §3.2.4.1 admits.

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| O-01 | `optional<u32> = absent` | `00` | `6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d` |
| O-02 | `optional<u32> = present(42)` | `012a000000` | `54a9fc9cc1ebb46c6257d79a5f512135ef54ca839155386e6a365c999fa98432` |
| O-03 | `optional<string> = absent` | `00` | `6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d` |
| O-04 | `optional<string> = present("x")` (variable-width payload) | `010100000078` | `0958fbeb1db5b861931b66968e84b79a28f0f55b4671bd7d9b4465ddcef1a3b7` |

---

## 6. Enum (1-byte width ≤256 variants, 2-byte width ≤65536 variants)

§3.2.4.1 rule: enum width is fixed at schema definition time and frozen with `SchemaVersion` (Pass 5 M-1). Adding a 257th variant is a `SchemaVersion` bump because the on-wire width changes. Out-of-range integer values are decode-side `ERR_DS_SCHEMA_INCOMPATIBLE`.

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| E-01 | `enum<≤256 variants>{variant=5}` | `05` | `e77b9a9ae9e30b0dbdb6f510a264ef9de781501d7b6b92ae89eb059c5ab743db` |
| E-02 | `enum<≤65536 variants>{variant=0x0123}` | `2301` | `fff4c04ca7c0dea1d9ce552d547b47c4352b05178009e884c025fe6dbb6aa88d` |

---

## 7. Array&lt;T&gt; (canonical-sorted; covers "dictionaries with sort-key rule")

§3.2.4.1 rule: `4 + sum of element widths` — `u32` element count followed by `N` elements in canonical sort order (§3.1.1: bytewise-lexicographic ascending over each element's encoded bytes). Empty array = `00000000` only, no terminator. For variable-width `T` (`string`, `bytes`, `optional<T>`, nested `array<T>`, struct-with-string), there is no per-element framing — each element's encoding is self-delimiting (Pass 4 M-2).

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| A-01 | `array<u32> []` (empty) | `00000000` | `df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119` |
| A-02 | `array<u32> [1, 2, 3]` (sorted ascending) | `03000000010000000200000003000000` | `374d429ee174762e85d14ea494b50c91a44a1557227814f14650940d7e283790` |
| A-03 | `array<u8> [0x0a, 0x0b, 0x0c]` | `030000000a0b0c` | `41b6e54688c327cc0990b50b9d0c735db1812e8f0719b441c7af6444a7098698` |
| A-04 | `array<string> ["a", "bb"]` (variable-width; bytewise-lex sort: `"a" < "bb"` because `0x61 < 0x62`) | `020000000100000061020000006262` | `57e411b2ddb65ad728b9e9b7d166ad7972668e2cfbfec972aa914c264560ac96` |
| A-05 | `array<array<u8>> [[0x01], [0x02, 0x03]]` (nested variable-width; sort by encoded bytes: `01000000 01` < `02000000 02 03`) | `020000000100000001020000000203` | `7aa0997bd00d00df12486693c42807e61f7002c85d24ac4efca2cb094c7e9493` |

---

## 8. Struct (flat concatenation in declared schema order)

§3.2.4.1 rule: sum of fields — flat concatenation in declared schema order; no struct header, no field tag, no per-field separator. Variable-width fields rely on the field's own self-delimiting encoding.

| ID | Input | bytes (hex) | SHA-256 of bytes |
|---|---|---|---|
| ST-01 | `struct { u32 a; u8 b } = (1, 0xFF)` | `01000000ff` | `646b2567984a9cdb58007a0b01e3bb373fd30743712760fc5b6bb9c9c71034ac` |
| ST-02 | `struct { string s; u32 n } = ("ok", 7)` (variable-width head field; concatenation only) | `020000006f6b07000000` | `e78a4f600003740aa8aff6a8e9801b41e087161c91555d5673535414526306d9` |
| ST-03 | `DespawnEntry { entityId:u32=42, finalActionOrdinal:u64=7, finalRngCursor:u64=99, despawnTick:u64=120 }` (per §3.2.5.3 Tier A despawn-tombstone schema) | `2a000000070000000000000063000000000000007800000000000000` | `2211ce0646d2c821788ff7f57ab18585e848477611d18e85107ad0475a97904f` |

---

## 9. Domain-tagged digest preimages (top-level — `PhaseDigest`, `RngDraw`, `EnvironmentFingerprint`, `SnapshotHeader`, `SnapshotPayload`, `SnapshotDigest`)

§3.2.4.1 domain tags: `0x10` `PhaseDigest`, `0x11` `SnapshotPayload`, `0x12` `SnapshotHeader`, `0x13` `RngDraw`, `0x14` `EnvironmentFingerprint`. Each top-level digest preimage MUST begin with its 1-byte domain tag (Pass 5 C-2). The SHA-256 column for these entries is the **canonical digest** (the hash an implementation would commit to a snapshot record / phase log / RNG sample).

`HASH_INPUT_FIELD_WIDTHS` (§3.2.4.1 / §3.4): `DOMAIN_TAG=u8`, `DigestVersion=u16`, `Tick=u64`, `PhaseId=u8`, `SchemaVersion=u32`, `entityId=u32`, `streamVersion=u16`, `actionOrdinal=u64`, `drawIndex=u32`, `RngCursor=u64`, `StreamKey=u64`.

### 9.1 PhaseDigest (§3.2.2)

`PhaseDigest = SHA-256(SerializeCanonical(DOMAIN_TAG_PHASE || DigestVersion || Tick || PhaseId || phaseScopeFields))`

D-01 reproduces the §3.2.4.1 "Worked byte example" bit-for-bit. Re-deriving D-01's SHA-256 from the §3.2.4.1 prose (12-byte preimage `10 01 00 78 00 00 00 00 00 00 00 03`) is the canonical cross-check between this corpus and the spec text — any divergence is a corpus-vs-spec defect, not an implementation defect.

D-02 covers the `AI_NoOp` digest semantics from §3.2.2 (tick-sensitive, NOT a constant; same Tick=120 as D-01 but `PhaseId=2` → distinct digest).

| ID | Input | preimage bytes (hex) | SHA-256 (= digest) |
|---|---|---|---|
| D-01 | `PhaseDigest(Tick=120, PhaseId=3 (Physics), DigestVersion=1, phaseScopeFields = empty struct)` — §3.2.4.1 worked example | `100100780000000000000003` | `9add9c4e2d104800c98cbfda36858969494a2736bf5ad12bb3accb4731c130a1` |
| D-02 | `PhaseDigest(Tick=120, PhaseId=2 (AI_NoOp), DigestVersion=1, phaseScopeFields = empty struct)` — §3.2.2 normative AI_NoOp clarification | `100100780000000000000002` | `9b8707a501d0bde9e106243e4bea7e85177b7579a2acb9374acdc9fd27b1f26a` |

### 9.2 RngDraw (§3.2.5)

`RngDraw = SipHash-2-4-64((k0,k1), DOMAIN_TAG_RNGDRAW || StreamKey || actionOrdinal || drawIndex)`

D-03 below covers the **canonical preimage byte string** that is fed into SipHash-2-4. The SipHash output itself is parameterized by `(k0, k1)` derived via `RNG_KDF` and is covered in `siphash-2-4-kat.md` Project-Specific Test Case. The SHA-256 column for D-03 hashes the preimage bytes as a verifier of the byte concatenation, not the SipHash output.

| ID | Input | preimage bytes (hex) | SHA-256 of preimage (verifier) |
|---|---|---|---|
| D-03 | `RngDraw(StreamKey=0x0102030405060708, actionOrdinal=1, drawIndex=0)` — §3.2.5 byte concatenation under HASH_INPUT_FIELD_WIDTHS | `130807060504030201010000000000000000000000` | `d3a3aff04348adbe7a3893cd3e7a2df713f001691849c1f9ed1c6ee144b0f8c6` |

### 9.3 SnapshotHeader / SnapshotPayload / SnapshotDigest (§3.2.3)

`SnapshotDigest[T] = SHA-256( SerializeCanonical(0x12 || SnapshotHeader[T]) || SerializeCanonical(0x11 || SnapshotPayload[T]) )`

`SnapshotHeader[T]` MUST be serialized in §2.3 declaration order: `schemaVersion(u32) || tick(u64) || prevSnapshotDigest(32 bytes) || environmentFingerprint(32 bytes — opaque SHA-256 output)`. `SnapshotPayload[T]` follows the canonical schema order (§3.2.4.1).

D-05 (`EnvironmentFingerprint` preimage with empty struct) is *illustrative* — Stage 0's actual `EnvironmentFingerprint` carries §4.8 fields (CPU model, OS, Unity LTS, Mono/IL2CPP, denormals/fp-contract/fma flag set, worker count, SIMD level, `UNICODE_NFC_VERSION`). The corpus uses an empty fingerprint payload here to provide a 32-byte digest fixture that downstream test cases (D-04, D-07) can chain off. The full Stage 0 `EnvironmentFingerprint` corpus row is a §4.8 deliverable, not a §3.2.4.1 deliverable, and lives outside this file.

D-06 (`SnapshotPayload` preimage) uses a single-entry `array<DespawnEntry>` (ST-03 wrapped in an array of length 1) as the smallest concrete payload that exercises both `array<T>` framing and a §3.2.5.3 Tier-A struct.

D-07 is the **chained `SnapshotDigest`** — its SHA-256 is the digest written to the on-disk record's trailing 32-byte slot (§3.9.2).

| ID | Input | preimage bytes (hex) | SHA-256 (= digest) |
|---|---|---|---|
| D-04 | `SerializeCanonical(0x12 || SnapshotHeader{schemaVersion=1, tick=120, prevSnapshotDigest=0×32, environmentFingerprint=SHA256(D-05)})` | `12010000007800000000000000000000000000000000000000000000000000000000000000000000000000000083891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d879` | `cbec838550524abb8e07b94b9ce345748dccfc02067c8d38feb474cd113b85fb` |
| D-05 | `SerializeCanonical(0x14 || EnvironmentFingerprint{}=empty struct)` — illustrative Stage-0 placeholder; full Stage-0 `EnvironmentFingerprint` row lives under §4.8 | `14` | `83891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d879` |
| D-06 | `SerializeCanonical(0x11 || array<DespawnEntry>=[ST-03])` — illustrative single-entry payload | `11010000002a000000070000000000000063000000000000007800000000000000` | `244959992ce01bdcdeeae842e51002eb092a5497da6ec13a4865bd7a5bd042d2` |
| D-07 | `SnapshotDigest = SHA-256(D-04 preimage || D-06 preimage)` per §3.2.3 — chained snapshot digest | `12010000007800000000000000000000000000000000000000000000000000000000000000000000000000000083891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d87911010000002a000000070000000000000063000000000000007800000000000000` | `40215bc9a6c6eb968a1a0097d2e9aa35da15ec5cfc2dba45004aadc692bbbb3c` |

---

## 10. Cross-checks against §3.2.4.1 spec text

These cross-checks bind the corpus to the spec prose. A reviewer can confirm them without running any code:

1. **D-01 preimage matches the §3.2.4.1 worked-byte-example layout.** §3.2.4.1 lists the 12-byte preimage as `10 01 00 78 00 00 00 00 00 00 00 03`. Corpus D-01 `bytes (hex)` column = `100100780000000000000003`. **Match.**
2. **F-05 bit pattern matches §3.4.3.** §3.4.3 derives `PHYSICS_DT = (float)(1.0/60.0) = 0x3C888889`. Corpus F-05 `bytes (hex)` column = `8988883c` (little-endian of `0x3C888889`). **Match.**
3. **F-04 bit pattern matches `NAN_CANONICAL_F32`.** §3.4: `NAN_CANONICAL_F32 = 0x7FC00000`. Corpus F-04 = `0000c07f`. **Match.**
4. **F-09 bit pattern matches `NAN_CANONICAL_F64`.** §3.4: `NAN_CANONICAL_F64 = 0x7FF8000000000000`. Corpus F-09 = `000000000000f87f`. **Match.**
5. **F-03 / F-08 bit patterns match `ZERO_CANONICAL_F32` / `ZERO_CANONICAL_F64`.** §3.4: both are all-zero. Corpus F-03 = `00000000`, F-08 = `0000000000000000`. **Match.**
6. **D-03 RngDraw layout matches §3.2.5 / §3.4 `HASH_INPUT_FIELD_WIDTHS`.** §3.2.5 specifies `DOMAIN_TAG_RNGDRAW || StreamKey || actionOrdinal || drawIndex`; widths = `1 + 8 + 8 + 4 = 21` bytes. Corpus D-03 preimage length = 21 bytes (`13` + 8-byte StreamKey + 8-byte actionOrdinal + 4-byte drawIndex). **Match.**
7. **D-04 SnapshotHeader field order matches §2.3 / §3.2.3.** §3.2.3: `schemaVersion || tick || prevSnapshotDigest || environmentFingerprint`. Corpus D-04 preimage decodes as `12` (DOMAIN_TAG) + `01000000` (schemaVersion=1) + `7800000000000000` (tick=120) + 32 zero bytes (prevSnapshotDigest) + 32 bytes envFp. **Match.**

---

## 11. Verification Procedure

1. Test runner: `Sim.Tests.Determinism.Serialization.SerializeCanonicalCorpusTests`.
2. For each corpus entry, construct the input from its structured-literal description; invoke `Serialization.SerializeCanonical(input)`; assert the output byte string equals the `bytes (hex)` column byte-for-byte.
3. For each corpus entry, compute `SHA-256(bytes)`; assert it equals the `SHA-256` column byte-for-byte. For D-01..D-07, the SHA-256 column is the canonical digest written to the corresponding §3.2.2 / §3.2.3 / §3.2.5 record slot.
4. On any mismatch: emit `ERR_DS_KAT_FAILURE` (TBD — allocate from §3.4 error-code range), abort certification run, fail CI.
5. The corpus reproducer (Appendix A) MUST be runnable as a sanity check (`python3 reproducer.py` regenerates every byte/SHA-256 pair). Any drift between the markdown table and the reproducer output is a corpus defect; the reproducer is authoritative.

---

## 12. Coverage matrix (§9.5 #4(c) checklist)

The §9.5 #4(c) checklist requires coverage of "primitives, fixed-width integers, floats with NaN normalization, arrays, dictionaries with sort-key rule, optionals, discriminated unions". Mapped to §3.2.4.1 type kinds and corpus entries:

| §9.5 #4(c) checklist item | §3.2.4.1 type kind(s) | Corpus entries |
|---|---|---|
| primitives | `bool` | P-01, P-02 |
| fixed-width integers | `u8`, `i8`, `u16`, `i16`, `u32`, `i32`, `u64`, `i64` | P-03..P-10 |
| floats with NaN normalization | `f32`, `f64` (with `ZERO_CANONICAL_*`, `NAN_CANONICAL_*`) | F-01..F-09 |
| arrays | `array<T>` (fixed- and variable-width `T`, nested) | A-01..A-05 |
| dictionaries with sort-key rule | `array<T>` under §3.1.1 canonical sort (no separate `map<K,V>` kind in §3.2.4.1) | A-02, A-04, A-05 |
| optionals | `optional<T>` (fixed- and variable-width `T`) | O-01..O-04 |
| discriminated unions | `optional<T>` (the only discriminated form §3.2.4.1 admits) | O-01..O-04 |
| (additional §3.2.4.1 kinds) | `string`, `bytes`, `enum`, `struct` | S-01..S-03, B-01, B-02, E-01, E-02, ST-01..ST-03 |
| (additional §3.2.4.1 kinds) | domain-tagged digest preimages (§3.2.2 / §3.2.3 / §3.2.5) | D-01..D-07 |

Every type kind in the §3.2.4.1 primitive table has ≥1 entry, satisfying the checklist's "≥1 worked input/output pair for every type kind".

---

## Appendix A — Reproducer (authoritative source of every byte/SHA-256 pair)

The following Python 3 script regenerates every entry above. Re-running it MUST produce the same byte and SHA-256 outputs; if a row diverges, the markdown table is wrong (the script is authoritative). Save as `tools/golden-vectors/serialize_canonical_reproducer.py` once `tools/` exists; until then it lives only in this Appendix.

```python
import hashlib, struct

def sha(b): return hashlib.sha256(b).hexdigest()

vectors = []
def add(label, b):
    vectors.append((label, b, sha(b)))

# Primitives
add("P-01 bool false",  bytes([0x00]))
add("P-02 bool true",   bytes([0x01]))
add("P-03 u8=0xAB",     bytes([0xAB]))
add("P-04 i8=-1",       bytes([0xFF]))
add("P-05 u16=0x1234",  struct.pack("<H", 0x1234))
add("P-06 i16=-1",      struct.pack("<h", -1))
add("P-07 u32=0x12345678", struct.pack("<I", 0x12345678))
add("P-08 i32=-1",      struct.pack("<i", -1))
add("P-09 u64=0x0123456789ABCDEF", struct.pack("<Q", 0x0123456789ABCDEF))
add("P-10 i64=-1",      struct.pack("<q", -1))

# Floats
add("F-01 f32=+1.0",    struct.pack("<f", 1.0))
add("F-02 f32=-1.0",    struct.pack("<f", -1.0))
add("F-03 f32=-0.0 normalized to +0.0", bytes(4))
add("F-04 f32=NaN normalized to NAN_CANONICAL_F32", struct.pack("<I", 0x7FC00000))
add("F-05 f32=PHYSICS_DT (1/60)", struct.pack("<I", 0x3C888889))
add("F-06 f64=+1.0",    struct.pack("<d", 1.0))
add("F-07 f64=-1.0",    struct.pack("<d", -1.0))
add("F-08 f64=-0.0 normalized to +0.0", bytes(8))
add("F-09 f64=NaN normalized to NAN_CANONICAL_F64", struct.pack("<Q", 0x7FF8000000000000))

# Strings
add("S-01 string ''",            struct.pack("<I", 0))
add("S-02 string 'abc'",         struct.pack("<I", 3) + b"abc")
add("S-03 string 'System XI'", struct.pack("<I", 17) + b"System XI")

# Bytes
add("B-01 bytes empty",          struct.pack("<I", 0))
add("B-02 bytes 0xCAFEBABE",     struct.pack("<I", 4) + bytes.fromhex("cafebabe"))

# Optional
add("O-01 optional<u32> absent", bytes([0x00]))
add("O-02 optional<u32> present 42", bytes([0x01]) + struct.pack("<I", 42))
add("O-03 optional<string> absent", bytes([0x00]))
add("O-04 optional<string> present 'x'", bytes([0x01]) + struct.pack("<I", 1) + b"x")

# Enum
add("E-01 enum 1B variant=5",    bytes([0x05]))
add("E-02 enum 2B variant=0x0123", struct.pack("<H", 0x0123))

# Array
add("A-01 array<u32> empty",     struct.pack("<I", 0))
add("A-02 array<u32> [1,2,3]",   struct.pack("<I", 3) + struct.pack("<III", 1, 2, 3))
add("A-03 array<u8> [0x0a,0x0b,0x0c]", struct.pack("<I", 3) + bytes([0x0A, 0x0B, 0x0C]))
add("A-04 array<string> ['a','bb']",
    struct.pack("<I", 2) + struct.pack("<I", 1) + b"a" + struct.pack("<I", 2) + b"bb")
add("A-05 array<array<u8>> [[0x01],[0x02,0x03]]",
    struct.pack("<I", 2) + struct.pack("<I", 1) + bytes([0x01])
                        + struct.pack("<I", 2) + bytes([0x02, 0x03]))

# Struct
add("ST-01 struct{u32 a; u8 b}=(1,0xFF)", struct.pack("<I", 1) + bytes([0xFF]))
add("ST-02 struct{string s; u32 n}=('ok',7)",
    struct.pack("<I", 2) + b"ok" + struct.pack("<I", 7))
add("ST-03 DespawnEntry{42,7,99,120}",
    struct.pack("<I", 42) + struct.pack("<Q", 7) + struct.pack("<Q", 99) + struct.pack("<Q", 120))

# Domain-tagged preimages
DOMAIN_TAG_PHASE = 0x10
DOMAIN_TAG_SNAPSHOT_PAYLOAD = 0x11
DOMAIN_TAG_SNAPSHOT_HEADER = 0x12
DOMAIN_TAG_RNGDRAW = 0x13
DOMAIN_TAG_ENV_FP = 0x14

add("D-01 PhaseDigest preimage Tick=120 PhaseId=3 (worked example)",
    bytes([DOMAIN_TAG_PHASE]) + struct.pack("<H", 1) + struct.pack("<Q", 120) + bytes([0x03]))
add("D-02 PhaseDigest preimage AI_NoOp Tick=120 PhaseId=2",
    bytes([DOMAIN_TAG_PHASE]) + struct.pack("<H", 1) + struct.pack("<Q", 120) + bytes([0x02]))
add("D-03 RngDraw preimage StreamKey=0x0102030405060708 actionOrdinal=1 drawIndex=0",
    bytes([DOMAIN_TAG_RNGDRAW])
    + struct.pack("<Q", 0x0102030405060708)
    + struct.pack("<Q", 1)
    + struct.pack("<I", 0))

env_fp_preimage_empty = bytes([DOMAIN_TAG_ENV_FP])
env_fp_digest = hashlib.sha256(env_fp_preimage_empty).digest()
add("D-05 EnvironmentFingerprint preimage (empty struct)", env_fp_preimage_empty)

snapshot_hdr = (bytes([DOMAIN_TAG_SNAPSHOT_HEADER])
                + struct.pack("<I", 1) + struct.pack("<Q", 120)
                + bytes(32) + env_fp_digest)
add("D-04 SnapshotHeader preimage (schemaVersion=1, tick=120, prev=0, envFp=SHA256(D-05))",
    snapshot_hdr)

payload = (struct.pack("<I", 1)
           + struct.pack("<I", 42) + struct.pack("<Q", 7)
           + struct.pack("<Q", 99) + struct.pack("<Q", 120))
snapshot_payload = bytes([DOMAIN_TAG_SNAPSHOT_PAYLOAD]) + payload
add("D-06 SnapshotPayload preimage (array<DespawnEntry>=[ST-03])", snapshot_payload)

add("D-07 SnapshotDigest preimage (D-04 || D-06)", snapshot_hdr + snapshot_payload)

for label, b, h in vectors:
    print(f"--- {label}")
    print(f"    bytes ({len(b)}): {b.hex()}")
    print(f"    sha256       : {h}")
```

---

## References

- **Deterministic Simulation Spec #16** — `section-3.md` §3.2.2 (PhaseDigest formula), §3.2.3 (SnapshotDigest chain), §3.2.4.1 (`SerializeCanonical` byte-level schema, primitive table, domain-tag table, `HASH_INPUT_FIELD_WIDTHS`, worked byte example), §3.2.5 / §3.2.5.3 (RngDraw, despawn tombstone), §3.4 (constants catalogue), §3.4.3 (`PHYSICS_DT` derivation), §9.5 acceptance criterion #4 (c).
- **`hkdf-sha256-kat.md`** (this directory) — RFC 5869 KAT vectors backing `RNG_KDF`.
- **`siphash-2-4-kat.md`** (this directory) — Aumasson & Bernstein 2012 Appendix A KAT vectors backing `RNG_STREAM_HASH`.

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | May 14, 2026 | Initial corpus authored against #16 §3.2.4.1 (frozen at v0.9 + Pass 4/5 fixes). 41 entries spanning primitives (P-01..P-10), floats with NaN/zero normalization (F-01..F-09), strings (S-01..S-03), bytes (B-01..B-02), optionals (O-01..O-04), enums (E-01..E-02), arrays including nested + variable-width (A-01..A-05), structs including §3.2.5.3 Tier-A `DespawnEntry` (ST-01..ST-03), and domain-tagged digest preimages for `PhaseDigest` / `RngDraw` / `EnvironmentFingerprint` / `SnapshotHeader` / `SnapshotPayload` / `SnapshotDigest` (D-01..D-07). Every byte and SHA-256 pair generated by the Appendix A reproducer; no fabricated values per CLAUDE.md. D-01 byte sequence cross-verified against the §3.2.4.1 worked-byte-example (`10 01 00 78 00 00 00 00 00 00 00 03`) and matches. §9.5 #4(c) spec-level sub-condition: **SATISFIED** as of this commit (subject to lead-developer + spec-author review per §9.5 #4(c) Tier 2 gate). |
