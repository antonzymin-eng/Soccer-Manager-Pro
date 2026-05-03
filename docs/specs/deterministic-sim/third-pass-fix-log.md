# Deterministic Simulation Specification #16 — Third-Pass Fix Log

> **Created:** May 3, 2026
> **Source critique:** Third-pass adversarial review (read-only, delivered May 3, 2026)
> **Source files modified by this pass:** `section-1.md` (v0.9), `section-3.md` (v0.9), `section-4.md` (v0.9), `section-5.md` (v0.9), `section-9-approval-checklist.md` (v0.9), plus `docs/tracking/spec-error-log.md` (ERR-016-002 filed)
> **Companion log:** Earlier first/second-pass findings remain in `adversarial-review.md` (21-row resolution table dated 2026-05-02). This file covers ONLY the third-pass findings (4 H, 6 M, 6 L, 1 cross-cutting).

---

## Index

| Finding | Severity | Description | Status | Primary fix location |
|---|---|---|---|---|
| H-A | High | `SerializeCanonical(...)` invoked but never normatively defined | ✅ Resolved | §3.2.4.1 (new) |
| H-B | High | Hash inputs lack field-width binding and per-evaluation domain separator; `RngCursor + drawIndex` is arithmetic addition | ✅ Resolved | §3.2.1, §3.2.4, §3.2.4.1, §3.2.5, §3.4 (`HASH_INPUT_FIELD_WIDTHS`) |
| H-C | High | Stage-0 `float` Tier-A under §1.3.1.1 unattainable on Unity multi-core (workpartition non-portability) | ✅ Resolved | §1.3.1.1 (rewritten) |
| H-D | High | `floatModelHash` hand-waved (no flag list, no compiler-flag vocabulary, no serialization) | ✅ Resolved | §4.8.3 (new) + §5.5.1 (new) |
| M-E | Medium | Save/load equivalence sample size unspecified | ✅ Resolved | §5.5.2 (new) |
| M-F | Medium | EntityId no-reuse rule normative on already-APPROVED specs #2/#8 with no back-propagation filed | ✅ Mitigated (filed) | `spec-error-log.md` ERR-016-002, CLAUDE.md Open Issues update pending lead-developer step |
| M-G | Medium | NaN/Inf policy silent for Tier B | ✅ Resolved | `ERR_DS_TIERB_NONFINITE` (0x160A); canonical NaN bit patterns (§3.2.4.1, §3.4) |
| M-H | Medium | Cross-match EntityId lifecycle undefined | ✅ Resolved | §3.2.5.2 (new) |
| M-I | Medium | Storage atomicity hand-waved | ✅ Resolved | §4.6.1.1 (new) + `ERR_DS_STORAGE_ATOMICITY` (0x160C) |
| M-J | Medium | Physics dt irrational and unbound; no normative `PHYSICS_DT` constant | ✅ Resolved | §3.4 `PHYSICS_DT = 0x3C8888B7` with computation rule |
| L-K | Low | Two `Version History` blocks (§3.5 and §3.12); §3.4 → §3.4.2 with no §3.4.1 | ✅ Resolved | §3.5 consolidated; §3.12 stub; §3.4.1 added (Reserve enforcement) |
| L-L | Low | §9.5 acceptance criterion #4 unfalsifiable ("verified correct" without artifact) | ✅ Resolved | §9.5 #4 names three concrete KAT/corpus artifacts |
| L-M | Low | §5.5 "Deterministic flags" lists semantic intent without concrete MSVC/Clang strings | ✅ Resolved | §5.5.1 (new) |
| L-N | Low | `Reserve(siteId, count)` budget enforcement unspecified | ✅ Resolved | §3.4.1 (new) + `ERR_DS_RNG_BUDGET_MISMATCH` (0x160B) |
| L-O | Low | `actionOrdinal` and `RngCursor` widths unbound | ✅ Resolved | §3.4 `HASH_INPUT_FIELD_WIDTHS` (both u64) |
| L-P | Low | `AI_NoOp` empty-scope digest semantics need normative sentence | ✅ Resolved | §3.1.2 explicit normative paragraph |
| Cross-cutting | Structural | §16 approval predicated on unwritten consumer specs (#9/#17/#18/#19) | ✅ Acknowledged | Already in §8.3 + §9.4; no further action — sequencing constraint stands |

---

## H — High severity

### H-A. `SerializeCanonical(...)` invoked but never defined
**Critique summary:** Two implementers following only §3.2.2 / Appendix A.1 prose can produce different bytes for the same in-memory state, making Tier A bitwise equality unreachable.

**Fix:** Added §3.2.4.1 *SerializeCanonical (normative byte-level schema)* containing:
- a primitive encoding table (bool, u8/i8, u16/i16, u32/i32, u64/i64, f32, f64, string, bytes, array, optional, enum, struct) with exact byte widths and rules;
- a `DOMAIN_TAG` byte for each top-level digest preimage (`0x10`–`0x14`) preventing cross-domain preimage collisions;
- a `HASH_INPUT_FIELD_WIDTHS` registry (`DigestVersion=u16`, `Tick=u64`, `PhaseId=u8`, `subsystemId=u16`, `entityId=u32`, `streamVersion=u16`, `actionOrdinal=u64`, `drawIndex=u32`, `RngCursor=u64`, `StreamKey=u64`);
- a 12-byte worked example for the `PhaseDigest` preimage at `Tick=120, PhaseId=3`;
- a verification-artifact pointer to `golden-vectors/serialize-canonical-corpus.md` (gates §9.5 #4).

UTF-8 NFC string encoding, `STRING_MAX_BYTES = 65536`, no length prefixes/separators/padding outside the table, schema reordering = `DigestVersion` bump.

### H-B. Hash inputs lack widths + per-evaluation domain separator
**Critique summary:** `subsystemId ∥ entityId ∥ streamVersion` ambiguous without fixed widths; `(RngCursor + drawIndex)` arithmetic addition aliases draws across evaluations under any cursor-budget bug.

**Fix:**
- §3.2.4 amended: per-draw value formula changed from `SipHash-2-4-64((k0,k1), StreamKey ∥ (RngCursor + drawIndex))` (arithmetic) to `SipHash-2-4-64((k0,k1), StreamKey ∥ actionOrdinal ∥ drawIndex)` (concatenation).
- `actionOrdinal` (per-evaluation reservation index) is now in the SipHash input as the per-evaluation salt, giving each draw unique provenance even if cursor arithmetic is buggy.
- `RngCursor` removed from SipHash input; remains as bookkeeping/snapshot field.
- §3.4 `HASH_INPUT_FIELD_WIDTHS` table binds every field width.
- Concatenation is byte-string concatenation in canonical schema order; no addition, no length prefixes, no separators.
- Width changes require `DigestVersion` bump (and `streamVersion` bump for stream-key inputs).

### H-C. Stage-0 `float` Tier-A unattainable on Unity multi-core
**Critique summary:** Pinning worker count and reduction topology does not pin physical-core/HT placement, SIMD throttling, scheduler placement, or work-partition stability. Parallel partial sums remain non-associative under canonical-key merge.

**Fix:** §1.3.1.1 rewritten:
- Stage-0 Tier A is now **restricted to the serial execution path**. A `float` field MAY be Tier A only if every authoritative write occurs on the main simulation thread without participation in any parallel reduction/scan/work-partitioned accumulation.
- Fields touched by parallel reductions MUST be Tier B with an approved tolerance row at Stage 0.
- Environment pinning is necessary but not sufficient for Tier A; it remains required for both tiers because environment changes invalidate snapshot/replay parity.
- Fixed-shape canonical-key reduction makes a field repeatable on the same machine under matching `EnvironmentFingerprint` but does NOT promote it to Tier A.
- Stage 5+ Fixed64 migration removes the serial-path restriction.

### H-D. `floatModelHash` hand-waved
**Critique summary:** §4.8 named the field but did not specify covered flags, serialization, compiler vocabulary. IL2CPP backends (MSVC/Clang/AppleClang) interpret float flags differently.

**Fix:**
- Added §4.8.3 *floatModelHash composition (normative)* defining the hash as `SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))` over an 11-field tuple: `compilerToolchain, compilerVersion, targetTriple, il2cppVersion, denormalsAreZero, flushToZero, roundingMode, fpContractMode, fmaEnabled, fastMath, simdLevel`.
- Stage-0 required values listed (denormals off, flush-to-zero off, NearestEven, fp-contract off, FMA off, fast-math off).
- Cross-references §5.5.1 for concrete flag strings per backend.
- Runtime MUST query MXCSR (or platform equivalent) at match start and reject the run on observed-vs-recorded mismatch with `ERR_DS_REPLAY_ENV_MISMATCH`.

---

## M — Medium severity

### M-E. Save/load equivalence sample size unspecified
**Fix:** Added §5.5.2 *Save/load equivalence sample protocol (normative)* binding minimum scenario count (≥12), save ticks per scenario (≥50, drawn from `[60, scenarioLength-60]`), save-tick seed derivation (`SipHash-2-4-64(matchSeed, "T-DS-REPLAY-004") mod 2^31`), replay length post-resume (≥600 ticks), pass criterion (deterministic — 100% Tier A bit-equality, zero out-of-bound Tier B), and fail action (block release; do not retry with fresh seed).

### M-F. EntityId no-reuse not back-propagated to specs #2/#8
**Fix:**
- ERR-016-002 filed in `docs/tracking/spec-error-log.md` (medium severity, open).
- §3.2.5 paragraph updated to explicitly state the constraint is filed for back-propagation in `spec-error-log.md` and tracked in CLAUDE.md Open Issues until reciprocal `XC-` references are filed in #2 and #8.
- Required fix: minor-revision both Agent Movement (#2) and Decision Tree (#8) to add reciprocal `XC-002-NNN` and `XC-008-NNN` references citing Deterministic Simulation §3.2.5. **Pending lead-developer authorization** because it touches APPROVED specs.

### M-G. NaN/Inf policy silent for Tier B
**Fix:**
- New `ERR_DS_TIERB_NONFINITE` (`0x160A`).
- New constants `NAN_CANONICAL_F32 = 0x7FC00000` and `NAN_CANONICAL_F64 = 0x7FF8000000000000`.
- §3.2.4.1 Tier B `f32`/`f64` rule: NaN normalized to canonical quiet-NaN bit pattern before serialization. NaN/Inf observed *outside* this canonical encoding triggers `ERR_DS_TIERB_NONFINITE` — classified as a hard encoding bug, NOT as Tier B drift.

### M-H. Cross-match EntityId lifecycle undefined
**Fix:** Added §3.2.5.2 *Cross-match EntityId lifecycle*:
- Each match instance allocates a fresh `EntityId` namespace.
- Despawn tombstone log scoped to owning match; cleared at match finalization.
- Career-mode persistence handled by stable `PersonId` outside the per-match namespace; `PersonId → EntityId` mapping established at match setup, frozen for match lifetime.
- New RNG stream allocated per match (`actionOrdinal=0, RngCursor=0`); RNG state does not cross matches.
- Replay of matchN+1 is independent of matchN tombstone state.

### M-I. Storage atomicity hand-waved
**Fix:** Added §4.6.1.1 *Atomic-write contract (normative)* with five mandatory steps for `SnapshotStore.CommitAtomic` on the Stage-0 host:
1. Same-volume write-then-rename (cross-volume rejected).
2. fsync (or `FlushFileBuffers`) before rename.
3. Atomic rename (`rename(2)` POSIX / `MoveFileEx` with `MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH` Windows).
4. Directory fsync after rename (Linux/macOS).
5. On failure: return `ERR_DS_STORAGE_ATOMICITY`, clean up temp file, leave prior snapshot untouched.

Cloud blob stores, FAT32, and network filesystems without atomic-rename semantics are explicitly out-of-scope for Stage 0.

### M-J. Physics dt irrational and unbound
**Fix:** Added `PHYSICS_DT` to §3.4 with normative computation rule `(float)(1.0 / 60.0)` under round-to-nearest-even and reference bit pattern `0x3C8888B7`. Implementations MUST compute from `1.0f / (float)PHYSICS_TICK_HZ` to match the bit pattern exactly; pre-baked literals (`0.0166666675f`) are forbidden.

---

## L — Low severity / hygiene

### L-K. Duplicate Version History blocks; missing §3.4.1
**Fix:**
- Section-3 now has a single Version History at §3.5; the §3.12 block was consolidated and replaced with a stub note pointing to §3.5.
- §3.4 now has a §3.4.1 (Reserve budget enforcement) and §3.4.2 (Tier B comparator default policy), eliminating the previous §3.4 → §3.4.2 numbering gap.

### L-L. §9.5 acceptance criterion #4 unfalsifiable
**Fix:** §9.5 #4 now names three concrete artifacts that MUST exist before the box can be checked:
- `golden-vectors/hkdf-sha256-kat.md` — RFC 5869 §A.1–A.3 known-answer vectors;
- `golden-vectors/siphash-2-4-kat.md` — Aumasson & Bernstein 2012 Appendix A reference vectors;
- `golden-vectors/serialize-canonical-corpus.md` — `SerializeCanonical` reference corpus described in §3.2.4.1.

### L-M. §5.5 Deterministic flags concrete strings
**Fix:** Added §5.5.1 listing exact MSVC flags (`/fp:precise`, `/fp:except-`, `/Qfma-`, `/arch:SSE2`), MXCSR runtime setup (`_MM_SET_FLUSH_ZERO_MODE`, `_MM_SET_DENORMALS_ZERO_MODE`, `_MM_SET_ROUNDING_MODE`), and Clang/AppleClang equivalents (`-ffp-contract=off`, `-fno-fast-math`, `-fno-finite-math-only`, `-mno-fma`, `-msse2`, `fesetround(FE_TONEAREST)`). IL2CPP propagation requirements specified.

### L-N. `Reserve(siteId, count)` budget enforcement
**Fix:** §3.4.1 added: `Reserve` MUST validate `count` against the registered budget for `siteId` in §3.6.2's draw-site registry. On mismatch: hard-fail tick commit with `ERR_DS_RNG_BUDGET_MISMATCH` (0x160B); no `RngCursor`/`actionOrdinal` advance. Silent acceptance forbidden.

### L-O. `actionOrdinal` / `RngCursor` widths unbound
**Fix:** Both bound to `u64` in `HASH_INPUT_FIELD_WIDTHS` (§3.4 / §3.2.4.1). Width change = `DigestVersion` bump.

### L-P. `AI_NoOp` empty-scope digest semantics
**Fix:** §3.1.2 amended with normative paragraph: AI_NoOp emits `SHA-256(SerializeCanonical(DigestVersion ‖ Tick ‖ PhaseId=2 ‖ ∅))`. The output is NOT constant, NOT zero, NOT omitted — it is tick-sensitive because `Tick` is in the preimage. The 12-byte worked example in §3.2.4.1 (with `PhaseId=2` substituted for `PhaseId=3`) is the canonical AI_NoOp preimage.

---

## Cross-cutting structural risk

**Critique summary:** §4.2 disclaimer about `NOT STARTED` consumer specs (#9/#17/#18/#19) pushes a large fraction of the integration contract into unwritten specs. §16's "approved" status risks being predicated on unwritten specs.

**Status:** Already addressed by §8.3 sequencing constraint and §9.4 status block. No new edits — the constraint stands. SPEC_INDEX.md may optionally elevate this to its front-page status block; that is an SPEC_INDEX edit decision, not a Spec #16 edit.

---

## Constants added in this pass

| Constant | Value | Tag | Source |
|---|---|---|---|
| `PHYSICS_DT` | `0x3C8888B7` (f32 bit pattern of `1.0f/60.0f`) | [DERIVED] | §3.4 / M-J |
| `STRING_MAX_BYTES` | `65536` | [FIXED] | §3.4 / H-A |
| `HASH_INPUT_FIELD_WIDTHS` | (registry table) | [FIXED] | §3.4 / H-B |
| `NAN_CANONICAL_F32` | `0x7FC00000` | [FIXED] | §3.4 / M-G |
| `NAN_CANONICAL_F64` | `0x7FF8000000000000` | [FIXED] | §3.4 / M-G |
| `ERR_DS_TIERB_NONFINITE` | `0x160A` | [FIXED] | §3.4 / M-G |
| `ERR_DS_RNG_BUDGET_MISMATCH` | `0x160B` | [FIXED] | §3.4 / L-N |
| `ERR_DS_STORAGE_ATOMICITY` | `0x160C` | [FIXED] | §3.4 / M-I |

## Sections added in this pass

- **§3.2.4.1** — SerializeCanonical (normative byte-level schema)
- **§3.2.5.2** — Cross-match EntityId lifecycle
- **§3.4.1** — Reserve budget enforcement (normative)
- **§4.6.1.1** — Atomic-write contract (normative)
- **§4.8.3** — floatModelHash composition (normative)
- **§5.5.1** — Deterministic compiler/runtime flag strings (normative)
- **§5.5.2** — Save/load equivalence sample protocol (normative)

## Open follow-up items

| Item | Owner | Tracker |
|---|---|---|
| Back-propagate EntityId no-reuse to specs #2 and #8 | Lead developer (touches APPROVED specs) | ERR-016-002 in `spec-error-log.md`; mirror in CLAUDE.md Open Issues |
| Author the three golden-vector files referenced by §9.5 #4 | Systems Engineering owner of `DeterministicRngService` | §9.5 acceptance criterion #4 |
| Revisit Tier-A scope per-field once parallel-reduction surface is implementation-known | Simulation lead | §1.3.1.1 |
| Section-2.md FR-DS list: optionally add FR rows for the new error codes / sample protocol | Spec author | FR coverage check during next review pass |

## Version sync

| File | Version before | Version after | Notes |
|---|---|---|---|
| `section-1.md` | v0.7 | v0.9 | H-C |
| `section-2.md` | v0.8 | v0.8 (unchanged) | No FR additions in this pass; new error codes covered by existing FR-DS-011 / FR-DS-012 spirit |
| `section-3.md` | v0.8 | v0.9 | H-A, H-B, M-G, M-H, M-J, L-K, L-N, L-O, L-P |
| `section-4.md` | v0.8 | v0.9 | H-D, M-I |
| `section-5.md` | v0.7 | v0.9 | M-E, L-M |
| `section-9-approval-checklist.md` | v0.8 | v0.9 | L-L |
| `spec-error-log.md` | v1.6 | v1.7 | ERR-016-002 filed |

*End of third-pass fix log — May 3, 2026.*
