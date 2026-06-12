# SipHash-2-4-64 Known-Answer Test Vectors

**Purpose:** Verifies the `RNG_STREAM_HASH` constant in Deterministic Simulation Spec #16 §3.4 against the canonical SipHash-2-4 reference test vectors. Pinned by §9.5 acceptance criterion #4 (b) of #16.
**Created:** May 6, 2026
**Source:** Aumasson, J.-P. and D. J. Bernstein, *SipHash: a fast short-input PRF*, INDOCRYPT 2012, Appendix A (the canonical 64-output-byte test vector table). The same table is embedded in the SipHash reference implementation at https://github.com/veorq/SipHash/blob/master/vectors.h.
**Authority:** Aumasson & Bernstein 2012 Appendix A.

---

## How this file is consumed

The `DeterministicRngService` SipHash-2-4-64 implementation MUST execute every test case below and produce the listed 64-bit output bit-for-bit. A single mismatch is a hard failure of `FR-DS-009-GATE` (Stage 0 certification gate, §16 §5.5). Test card binding: `Sim.Tests.Determinism.Rng.SipHash24KatTests` (TBD — see #16 §5.2 traceability block).

---

## Common Inputs (apply to all 64 test vectors)

The reference test set fixes a single 128-bit key and runs against messages of length 0 through 63 bytes (each message is the byte sequence `00, 01, 02, ..., len-1`).

| Field | Value |
|-------|-------|
| `key` (16 bytes, little-endian as stored) | `000102030405060708090a0b0c0d0e0f` |
| `key bytes (k0,k1)` | `k0 = 0x0706050403020100`, `k1 = 0x0f0e0d0c0b0a0908` (little-endian load of the above) |
| Message for index `i` (0 ≤ i ≤ 63) | byte sequence `0x00, 0x01, ..., 0x(i-1)`; index 0 is the empty message |
| Algorithm | SipHash-2-4 (2 compression rounds per message block, 4 finalization rounds) |
| Output | 64-bit (8-byte) little-endian |

---

## Vector Table (64 entries)

Output column shows the 64-bit SipHash-2-4 result as 16 lowercase hex characters. The byte order shown is the **little-endian byte sequence as emitted by the reference implementation** (`out[0]..out[7]`), concatenated. To obtain the integer interpretation (Python: `int.from_bytes(bytes.fromhex(value), 'little')`), reverse byte order.

| `i` (msg len) | SipHash-2-4-64 output (little-endian bytes, hex) |
|----:|--------------------------------------------------|
|  0 | `310e0edd47db6f72` |
|  1 | `fd67dc93c539f874` |
|  2 | `5a4fa9d909806c0d` |
|  3 | `2d7efbd796666785` |
|  4 | `b7877127e09427cf` |
|  5 | `8da699cd64557618` |
|  6 | `cee3fe586e46c9cb` |
|  7 | `37d1018bf50002ab` |
|  8 | `6224939a79f5f593` |
|  9 | `b0e4a90bdf82009e` |
| 10 | `f3b9dd94c5bb5d7a` |
| 11 | `a7ad6b22462fb3f4` |
| 12 | `fbe50e86bc8f1e75` |
| 13 | `903d84c02756ea14` |
| 14 | `eef27a8e90ca23f7` |
| 15 | `e545be4961ca29a1` |
| 16 | `db9bc2577fcc2a3f` |
| 17 | `9447be2cf5e99a69` |
| 18 | `9cd38d96f0b3c14b` |
| 19 | `bd6179a71dc96dbb` |
| 20 | `98eea21af25cd6be` |
| 21 | `c7673b2eb0cbf2d0` |
| 22 | `883ea3e395675393` |
| 23 | `c8ce5ccd8c030ca8` |
| 24 | `94af49f6c650adb8` |
| 25 | `eab8858ade92e1bc` |
| 26 | `f315bb5bb835d817` |
| 27 | `adcf6b0763612e2f` |
| 28 | `a5c91da7acaa4dde` |
| 29 | `716595876650a2a6` |
| 30 | `28ef495c53a387ad` |
| 31 | `42c341d8fa92d832` |
| 32 | `ce7cf2722f512771` |
| 33 | `e37859f94623f3a7` |
| 34 | `381205bb1ab0e012` |
| 35 | `ae97a10fd434e015` |
| 36 | `b4a31508beff4d31` |
| 37 | `81396229f0907902` |
| 38 | `4d0cf49ee5d4dcca` |
| 39 | `5c73336a76d8bf9a` |
| 40 | `d0a704536ba93e0e` |
| 41 | `925958fcd6420cad` |
| 42 | `a915c29bc8067318` |
| 43 | `952b79f3bc0aa6d4` |
| 44 | `f21df2e41d4535f9` |
| 45 | `87577519048f53a9` |
| 46 | `10a56cf5dfcd9adb` |
| 47 | `eb75095ccd986cd0` |
| 48 | `51a9cb9ecba312e6` |
| 49 | `96afadfc2ce666c7` |
| 50 | `72fe52975a4364ee` |
| 51 | `5a1645b276d592a1` |
| 52 | `b274cb8ebf87870a` |
| 53 | `6f9bb4203de7b381` |
| 54 | `eaecb2a30b22a87f` |
| 55 | `9924a43cc1315724` |
| 56 | `bd838d3aafbf8db7` |
| 57 | `0b1a2a3265d51aea` |
| 58 | `135079a3231ce660` |
| 59 | `932b2846e4d70666` |
| 60 | `e1915f5cb1eca46c` |
| 61 | `f325965ca16d629f` |
| 62 | `575ff28e60381be5` |
| 63 | `724506eb4c328a95` |

---

## Project-Specific Test Case — `RNG_STREAM_HASH` invocation pattern

This case verifies the project's `RNG_STREAM_HASH` binding (per #16 §3.2.4) hashes a fully-populated `(StreamKey || actionOrdinal || drawIndex)` byte concatenation and produces the expected 64-bit cursor draw. Domain tag `DOMAIN_TAG_RNGDRAW = 0x13` is prepended per §3.2.4 / §3.2.5 (Pass 5 C-2).

| Field | Value |
|-------|-------|
| `(k0, k1)` | derived from `matchSeed = 0xAA55AA55AA55AA55` via `RNG_KDF` (`hkdf-sha256-kat.md` Test Case 4, pinned): `k0 = 0xBBD0FA9E7732C34A`, `k1 = 0xA0409EEF6597CA91` |
| Input bytes | `0x13 || StreamKey(u64 LE) || actionOrdinal(u64 LE) || drawIndex(u32 LE)` per §3.4 `HASH_INPUT_FIELD_WIDTHS` — 1+8+8+4 = 21 bytes |
| Test fixture | `StreamKey = 0x0102030405060708`, `actionOrdinal = 1`, `drawIndex = 0` (identical to `serialize-canonical-corpus.md` entry D-03) |
| Preimage (21 bytes) | `130807060504030201010000000000000000000000` |
| Expected output | `0x9E847C2A31BDDB4E` (little-endian bytes `4edbbd312a7c849e`) |

**Project case status:** PINNED (June 12, 2026). Pinned by the C# KAT suite (`SipHash24KatTests.SipHash24_ProjectDrawPreimage_Pinned`) and cross-derived by an independent Python SipHash-2-4 mirror. **v1.0 stub correction:** the stub stated `StreamKey(16 bytes)` with fixture `0x00..0x0f`, contradicting the very §3.4 `HASH_INPUT_FIELD_WIDTHS` table it cited (`StreamKey = u64`, 8 bytes; also §3.2.4 "`StreamKey` output width is 64-bit unsigned") — corrected to u64 before pinning, and the fixture re-anchored to the corpus D-03 preimage so the two files lock the same 21 bytes. Any change to field widths, domain tag, or concatenation order MUST trigger `DETERMINISM_DIGEST_VERSION` bump and re-pin.

---

## Verification Procedure

1. Test runner: `Sim.Tests.Determinism.Rng.SipHash24KatTests`.
2. For each `i` in 0..63, build the message `(0x00, 0x01, ..., 0x(i-1))`, invoke `SipHash-2-4-64(k0=0x0706050403020100, k1=0x0f0e0d0c0b0a0908, message)`, assert the 8-byte little-endian output equals the table row for `i`.
3. For the project-specific case, build the 21-byte input per §3.2.4 / §3.4 byte schema; assert against the pinned value `0x9E847C2A31BDDB4E`.
4. On any mismatch: emit `ERR_DS_KAT_FAILURE`, abort certification run, fail CI.

---

## References

- **Aumasson, J.-P., and D. J. Bernstein** — *SipHash: a fast short-input PRF.* INDOCRYPT 2012. https://www.aumasson.jp/siphash/siphash.pdf — Appendix A is the source of the 64-vector table above.
- **SipHash reference implementation** — https://github.com/veorq/SipHash (`vectors.h` reproduces the same 64 entries).
- **Deterministic Simulation Spec #16** — §3.2.4 (per-draw hash input), §3.4 (constants table; `RNG_STREAM_HASH`, `DOMAIN_TAG_RNGDRAW`, `HASH_INPUT_FIELD_WIDTHS`), §9.5 acceptance criterion #4 (b).

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | May 6, 2026 | Initial KAT file. 64-entry reference table reproduced from Aumasson & Bernstein 2012 Appendix A. Project-specific case stubbed pending first successful reference-implementation run. |
| 1.1 | May 14, 2026 | **Byte-exact hand-verification pass against Aumasson & Bernstein 2012 Appendix A** and the `veorq/SipHash` reference `vectors.h` (per #16 §9.5 #4(b) spec-level sub-condition, §9 v1.3). All 64 output rows match byte-for-byte; metadata (16-byte key `000102…0f`; `k0 = 0x0706050403020100`, `k1 = 0x0f0e0d0c0b0a0908` via little-endian load; SipHash-2-4 round counts c=2 / d=4; 64-bit little-endian output convention; increasing-length input `0x00, 0x01, …, 0x(i-1)`) all correct. No findings; no content changes required. Project-specific case remains stubbed (no change). §9.5 #4(b) spec-level sub-condition: **SATISFIED** as of this commit. |
| 1.2 | June 12, 2026 | **Project-specific case PINNED**: v1.0 stub's `StreamKey(16 bytes)` corrected to u64 per the §3.4 `HASH_INPUT_FIELD_WIDTHS` table the stub itself cited; fixture re-anchored to the `serialize-canonical-corpus.md` D-03 preimage (`StreamKey = 0x0102030405060708`, `actionOrdinal = 1`, `drawIndex = 0`); `(k0, k1)` taken from the simultaneously pinned `hkdf-sha256-kat.md` Test Case 4. Output `0x9E847C2A31BDDB4E` pinned by the C# KAT suite (`src/deterministic-sim/tests/SipHash24KatTests.cs`, which also executes all 64 reference vectors — the prior in-repo suite covered only vectors 0–7) and an independent Python mirror. |
