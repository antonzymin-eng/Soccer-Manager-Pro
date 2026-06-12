# HKDF-SHA256 Known-Answer Test Vectors

**Purpose:** Verifies the `RNG_KDF` constant in Deterministic Simulation Spec #16 §3.4 against the canonical RFC 5869 known-answer vectors. Pinned by §9.5 acceptance criterion #4 (a) of #16.
**Created:** May 6, 2026
**Source:** RFC 5869 Appendix A.1 — A.3 (HKDF-SHA256 test cases). All three test cases from the SHA-256 family are reproduced here verbatim.
**Authority:** RFC 5869 (Krawczyk & Eronen, May 2010), Section 2 (algorithm) and Appendix A (test vectors).

---

## How this file is consumed

The `DeterministicRngService` implementation MUST execute every test case below and produce the listed `OKM` (Output Keying Material) bit-for-bit. A single mismatch is a hard failure of `FR-DS-009-GATE` (Stage 0 certification gate, §16 §5.5). The check is part of the unit-test suite under `Sim.Tests.Determinism.Rng.HkdfSha256KatTests` (test card binding TBD — see #16 §5.2 traceability block).

Hex strings below use lowercase, no separators, no `0x` prefix. Empty strings are explicitly noted.

The HKDF construction (RFC 5869 §2):
```
PRK = HMAC-SHA256(salt, IKM)            -- §2.2 Extract step
T(0) = empty string
T(n) = HMAC-SHA256(PRK, T(n-1) || info || byte(n)),  n = 1..ceil(L/HashLen)
OKM  = first L bytes of T(1) || T(2) || ...           -- §2.3 Expand step
```
For SHA-256: `HashLen = 32`. `salt` defaults to `HashLen` zero bytes when not supplied. `info` defaults to empty. Per #16 §3.2.4 (Pass 5 H-2), the project pins `info` to raw ASCII bytes (no string-framing) and `salt = NULL` is implemented as `HashLen` zero bytes (RFC 5869 §2.2 default).

---

## Test Case 1 — RFC 5869 Appendix A.1 (Basic test case with SHA-256)

| Field | Value |
|-------|-------|
| `Hash` | SHA-256 |
| `IKM` (22 bytes) | `0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b` |
| `salt` (13 bytes) | `000102030405060708090a0b0c` |
| `info` (10 bytes) | `f0f1f2f3f4f5f6f7f8f9` |
| `L` | 42 |
| `PRK` (32 bytes) | `077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5` |
| `OKM` (42 bytes) | `3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865` |

---

## Test Case 2 — RFC 5869 Appendix A.2 (Test with SHA-256 and longer inputs/outputs)

| Field | Value |
|-------|-------|
| `Hash` | SHA-256 |
| `IKM` (80 bytes) | `000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f` |
| `salt` (80 bytes) | `606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeaf` |
| `info` (80 bytes) | `b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeeff0f1f2f3f4f5f6f7f8f9fafbfcfdfeff` |
| `L` | 82 |
| `PRK` (32 bytes) | `06a6b88c5853361a06104c9ceb35b45cef760014904671014a193f40c15fc244` |
| `OKM` (82 bytes) | `b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c59045a99cac7827271cb41c65e590e09da3275600c2f09b8367793a9aca3db71cc30c58179ec3e87c14c01d5c1f3434f1d87` |

---

## Test Case 3 — RFC 5869 Appendix A.3 (Test with SHA-256 and zero-length salt/info)

| Field | Value |
|-------|-------|
| `Hash` | SHA-256 |
| `IKM` (22 bytes) | `0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b` |
| `salt` | (empty / not provided — implementation MUST use `HashLen` (32) zero bytes per RFC 5869 §2.2) |
| `info` | (empty) |
| `L` | 42 |
| `PRK` (32 bytes) | `19ef24a32c717b167f33a91d6f648bdf96596776afdb6377ac434c1c293ccb04` |
| `OKM` (42 bytes) | `8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8` |

---

## Project-Specific Test Case 4 — `RNG_KDF` invocation pattern

This case verifies that the project's `RNG_KDF` binding produces the same `PRK` as a direct HKDF-Extract call, and binds the §3.2.4 normative `info` literal to its expected `(k0, k1)` as §3.2.4 mandates ("A KAT row binding this exact `info` to its expected `(k0, k1)` output MUST be in `hkdf-sha256-kat.md`").

| Field | Value |
|-------|-------|
| `IKM` (8 bytes) | `matchSeed` (u64) as little-endian bytes. Fixture: `matchSeed = 0xAA55AA55AA55AA55` → `55aa55aa55aa55aa` |
| `salt` | `NULL` per §3.2.4 → 32 zero bytes (RFC 5869 §2.2 default) |
| `info` (13 bytes) | raw ASCII bytes `DS-RNG-KEY-v1` = `44532d524e472d4b45592d7631` (§3.2.4 normative; no §3.2.4.1 string framing) |
| `L` | 16 (= `RNG_KDF_OUTPUT_BYTES`; bytes [0..7] = k0, bytes [8..15] = k1, both little-endian u64) |
| `PRK` (32 bytes) | `d178986b5af973f6269098e66000584a9132b0e078c11ef78c1c257d146b550f` |
| `OKM` (16 bytes) | `4ac332779efad0bb91ca9765ef9e40a0` |
| `k0` | `0xBBD0FA9E7732C34A` (little-endian load of OKM[0..7]) |
| `k1` | `0xA0409EEF6597CA91` (little-endian load of OKM[8..15]) |

**Project Test Case 4 status:** PINNED (June 12, 2026). Pinned by the C# KAT suite (`HkdfSha256KatTests.Hkdf_ProjectCase4_RngKdfInvocationPattern_Pinned`) and cross-derived by an independent Python RFC 5869 mirror (same reproducer convention as `serialize-canonical-corpus.md` Appendix A). **v1.0 stub corrections (F-HKDF-02):** the stub's `info` literal `SMP-RNG-MATCH` contradicted the §3.2.4 normative text as approved (`DS-RNG-KEY-v1`, with its 13 ASCII bytes listed explicitly in §3.2.4); the stub's `L = 32` contradicted `RNG_KDF_OUTPUT_BYTES = 16` (§3.4); the stub's 32-byte IKM contradicted the u64 `matchSeed` consumed verbatim by `DeterministicRngService(ulong matchSeed)` (KD-7). All three corrected before pinning — no pinned value ever changed. From this commit on, any change to `info` byte encoding, `salt` semantics, IKM width, or `L` MUST trigger a `DETERMINISM_DIGEST_VERSION` bump (per §3.4 numeric-literal review gate) and re-pin. The derived `(k0, k1)` feed the project-specific case in `siphash-2-4-kat.md`.

---

## Verification Procedure

1. Test runner: `Sim.Tests.Determinism.Rng.HkdfSha256KatTests`.
2. For each of cases 1–3, supply `IKM`, `salt`, `info`, `L` exactly as shown; assert `PRK` (intermediate) and `OKM` (final) match byte-for-byte.
3. For case 4, supply the project-specific `info` bytes (`DS-RNG-KEY-v1`); assert `PRK`, `OKM`, and the derived `(k0, k1)` against the pinned values above.
4. On any mismatch: emit `ERR_DS_KAT_FAILURE` (TBD: allocate from §3.4 error-code range), abort certification run, fail CI.

---

## References

- **RFC 5869** — Krawczyk, H. and P. Eronen, *HMAC-based Extract-and-Expand Key Derivation Function (HKDF)*, May 2010. https://www.rfc-editor.org/rfc/rfc5869
- **Deterministic Simulation Spec #16** — §3.2.4 (`RNG_KDF` definition), §3.4 (constants table), §9.5 acceptance criterion #4 (a).

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | May 6, 2026 | Initial KAT file. RFC 5869 Appendix A.1–A.3 reproduced verbatim. Project Test Case 4 stubbed pending first successful reference-implementation run. |
| 1.1 | May 14, 2026 | **Byte-exact hand-verification pass against RFC 5869** (per #16 §9.5 #4(a) spec-level sub-condition, §9 v1.3). Finding **F-HKDF-01** filed and fixed: Test Case 1 OKM had a stray `0` nibble inserted between bytes 34–35 (`…bf 34 00 72 00 8d 5b 88 71 85 86 5` — 85 hex chars; canonical: `…bf 34 00 72 08 d5 b8 87 18 58 65` — 84 hex chars). Corrected to RFC 5869 §A.1 reference value `3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865`. Test Cases 2 and 3 OKM, all three PRK values, and all metadata (IKM/salt/info/L) verified correct against RFC 5869 §A.2 / §A.3 with no findings. Project Test Case 4 remains stubbed (no change). §9.5 #4(a) spec-level sub-condition: **SATISFIED** as of this commit. |
| 1.2 | June 12, 2026 | **Project Test Case 4 PINNED** (F-HKDF-02): v1.0 stub carried three input errors corrected before pinning — `info` `SMP-RNG-MATCH` → §3.2.4 normative `DS-RNG-KEY-v1`; `L` 32 → 16 (= `RNG_KDF_OUTPUT_BYTES`, §3.4); IKM 32-byte pattern → 8-byte LE u64 `matchSeed` (KD-7 / `DeterministicRngService(ulong)` ctor). Pinned PRK/OKM/(k0,k1) derived by the C# KAT suite (`src/deterministic-sim/tests/HkdfSha256KatTests.cs`) and an independent Python RFC 5869 mirror. Implementation note: the C# `HkdfSha256` was simultaneously upgraded from T(1)-only Expand (L ≤ 32 — structurally incapable of reproducing Test Cases 1–2 at L=42/82) to full RFC 5869 §2.3 multi-block Expand; all three RFC cases now assert PRK + full OKM byte-exact. |
