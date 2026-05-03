# Deterministic Simulation Specification #16 — Critique Log

> **Created:** May 3, 2026
> **Purpose:** Consolidated history of all adversarial reviews against Spec #16, with severity, resolution status, and outstanding follow-ups.
> **Supersedes:** `adversarial-review.md` (first/second-pass log, 2026-05-01/02) and `third-pass-fix-log.md` (2026-05-03). Both files were merged into this one and removed.
> **Maintenance rule:** Each new adversarial pass appends a section here. Resolved findings stay in the log as historical record. Open findings are surfaced in the **Outstanding Items** section near the end.

---

## Summary Status

| Pass | Date | Findings | Resolved | Outstanding |
|---|---|---|---|---|
| 1 — Outline review | 2026-05-01 | 5 H + 4 M + 1 L = 10 | 10 (subsumed by passes 2–3) | 0 |
| 2 — Full §1–§9 + appendices | 2026-05-02 | 21 (mixed severity) | 21 | 0 |
| 3 — Third-pass adversarial | 2026-05-03 | 4 H + 6 M + 6 L + 1 cross-cutting = 17 | 16 resolved + 1 mitigated | M-F (back-propagation pending lead-dev) |
| 4 — Fourth-pass adversarial | 2026-05-03 | 3 C + 5 M + 8 L + 3 cross-cutting = 19 | 0 (new — pending action) | All |
| 5 — Fifth-pass adversarial | 2026-05-03 | 2 C + 2 H + 5 M + 6 L + 1 cross-cutting = 16 | 0 (new — pending action) | All |

**Spec status:** `IN PROGRESS`. Promotion to `IN REVIEW` blocked on:
1. Fourth-pass critical-tier fixes (C-1, C-2, C-3a, C-3b — see Pass 4 below).
2. **Fifth-pass critical-tier fixes (C-1 SnapshotDigest field-order contradiction, C-2 PhaseDigest domain-tag desync — see Pass 5 below).**
3. §9.5 implementation-readiness review (gating item still unchecked).
4. §8.3 deferred dependencies (#9 Fixed64, #17 Event System, #18 Performance Optimization, #19 Testing Strategy) reaching `IN REVIEW`.
5. Three golden-vector files named in §9.5 #4.

---

## Pass 1 — Outline Adversarial Review (2026-05-01)

Reviewer mode: adversarial / implementation-risk focused. Validation pass against `outline.md` expanded draft.

**Top risks identified:**
1. Missing canonical tick-order tie-break semantics.
2. RNG contract incomplete for branch-dependent draw count normalization.
3. Snapshot/replay requirements omit byte-level canonicalization details.
4. Divergence detection lacks frozen hash schema and field-order contract.
5. Cross-platform certification criteria too generic to enforce objectively.

**Validation matrix (all subsumed by Pass 2/3 fixes):**

| ID | Severity | Finding |
|---|---|---|
| H-1 | High | Tick order phase-level only; intra-phase ordering unspecified |
| H-2 | High | RNG stream ownership and consumption model can drift under branching |
| H-3 | High | Snapshot schema lacks canonical binary layout guarantees |
| H-4 | High | Replay reconstruction algorithm boundaries ambiguous |
| H-5 | High | Divergence tooling lacks normative digest spec |
| M-1 | Medium | Tier A/B/C approval thresholds undefined |
| M-2 | Medium | Save/load equivalence criteria test-idea level only |
| M-3 | Medium | Instrumentation lacks performance budgets and retention policy |
| M-4 | Medium | Regression governance missing baseline update policy |
| L-1 | Low | Requirement taxonomy IDs not instantiated |

**Verdict at the time:** NOT READY FOR IMPLEMENTATION OR APPROVAL.

All Pass 1 risks were addressed by section-file authoring through v0.7 and the second-pass remediation; see Pass 2 resolutions below.

---

## Pass 2 — Full Spec Adversarial Review (2026-05-02)

Validation against full §1–§9 + appendices. Twenty-one findings; all resolved by section edits dated 2026-05-02 (v0.7 across each section file).

| ID | Finding | Resolution |
|---|---|---|
| A-1 | `SPEC_INDEX.md` said `NOT STARTED` while content existed at v0.5–v0.6 | `SPEC_INDEX.md` updated to `IN PROGRESS`; §9.4 status reconciled |
| A-2 | Phantom interfaces declared against `NOT STARTED` consumer specs (#17/#18/#19) | §4.2 reframed as non-normative sketches; behavior contract in §4.2.1 retained as normative anchor |
| B-3 | `actionOrdinal` simultaneously a `StreamKey` component and a per-evaluation counter (mutually exclusive) | `StreamKey` no longer carries `actionOrdinal`; `RngCursor` advances by reservation budget; `actionOrdinal` is per-evaluation reservation index |
| B-4 | Snapshot vs save boundary contradiction (`EndOfEvents` listed as legal save boundary before Snapshot phase ran) | New §1.3.0 terminology; `LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }` only |
| B-5 | "Big-endian byte string as produced by SHA-256" — misleading | §3.2.4 reworded: SHA-256 output is opaque 32-octet string; payload integers are little-endian |
| B-6 | Tier A bitwise float equality vs parallel float merges non-deterministic without pinning | New §1.3.1.1 Stage 0 conditional Tier A; §4.8 extended to recording-side environment pinning |
| C-7 | Constants split across §3.4 and §3.4.1; two error codes lacked hex IDs | Merged into one tagged catalogue; `ERR_DS_TIERB_TOLERANCE_MISSING` (0x1607) and `ERR_DS_DIGEST_CHAIN_BREAK` (0x1608) added |
| C-8 | Outline used `FR-DET-` / `VR-DET-` / `OPS-DET-`; section files used `-DS-` | New §2.0 Identifier Taxonomy; outline.md migrated to `-DS-` family |
| C-9 | Tick rate ambiguity: 10 Hz tactical vs 60 Hz physics | §3.1.2 binds physics tick at 60 Hz; AI gated to 10 Hz via `AI_PHASE_STRIDE = 6` and `AI_NoOp` |
| D-11 | Tier B default tolerance silent fallback | New §3.4.2 explicitly forbids fallback; missing tolerance row triggers `ERR_DS_TIERB_TOLERANCE_MISSING` |
| D-12 | `PhaseDigest` preimage missing `Tick`/`PhaseId` | §3.2.2 preimage extended; §5.10 rollup bound to canonical (tick, phaseOrdinal) |
| D-13 | `actionOrdinal` per-evaluation vs per-draw confusion | §3.2.5 fully rewritten |
| D-14 | `ResumeFrom` interface dropped digest validation | §4.2.2 normative replay lifecycle (8 steps) added |
| D-15 | Cross-spec audit table relied on specs that are all `NOT STARTED` | §8.3 sequencing constraint stated; rows reclassified `deferred dependency` |
| D-16 | Stage 0 host platform unnamed | §5.5 pinned to Windows x64, Unity 2022 LTS, IL2CPP; FR-DS-009-GATE split per stage |
| D-18 | Subsystem ordinal assignment rule absent | §3.1.1 added compile-time integer ordinal in `Sim.Constants.Determinism.SubsystemOrdinals` |
| D-19 | §3.6.1 Resolve row had stale parenthetical | Fixed |
| D-21 | EntityId no-reuse cross-spec normative constraint missing | Added in §3.2.5 (back-prop pending) |
| E-17 | Phase-ownership table listed seed-root prohibition only for `Resolve` | §3.6.1 universal-prohibitions block added |
| E-19 | `RunTick` described as "pure" while mutating state | §4.2.1 reworded "deterministic in (state, input, tickNumber)" |
| E-20 | §7.2 digest upgrade had no trigger criteria | §7.2 expanded with explicit triggers and coexistence policy |
| E-21 | §9.5 self-graded checklist with gating item unchecked | §9.5 reordered; presence-only vs gating made explicit |

Verdict after Pass 2: spec became internally consistent and CLAUDE.md-compliant.

---

## Pass 3 — Third-Pass Adversarial Review (2026-05-03)

Source: third-pass adversarial review (read-only, delivered 2026-05-03). Files modified: `section-1.md` (v0.9), `section-3.md` (v0.9), `section-4.md` (v0.9), `section-5.md` (v0.9), `section-9-approval-checklist.md` (v0.9), plus `docs/tracking/spec-error-log.md` (ERR-016-002 filed).

| ID | Severity | Finding | Status | Primary fix |
|---|---|---|---|---|
| H-A | High | `SerializeCanonical(...)` invoked but never normatively defined | ✅ Resolved | §3.2.4.1 (new) |
| H-B | High | Hash inputs lack field-width binding; `RngCursor + drawIndex` is arithmetic addition | ✅ Resolved | §3.2.1, §3.2.4, §3.2.4.1, §3.2.5, §3.4 `HASH_INPUT_FIELD_WIDTHS` |
| H-C | High | Stage-0 `float` Tier-A under §1.3.1.1 unattainable on Unity multi-core | ✅ Resolved | §1.3.1.1 (rewritten) |
| H-D | High | `floatModelHash` hand-waved | ✅ Resolved | §4.8.3 (new) + §5.5.1 (new) |
| M-E | Medium | Save/load equivalence sample size unspecified | ✅ Resolved | §5.5.2 (new) |
| M-F | Medium | EntityId no-reuse rule normative on APPROVED specs #2/#8 with no back-propagation filed | ⚠️ Mitigated | ERR-016-002 in `spec-error-log.md`; awaits lead-dev minor revision of #2 and #8 |
| M-G | Medium | NaN/Inf policy silent for Tier B | ✅ Resolved | `ERR_DS_TIERB_NONFINITE` (0x160A); canonical NaN bit patterns |
| M-H | Medium | Cross-match EntityId lifecycle undefined | ✅ Resolved | §3.2.5.2 (new) |
| M-I | Medium | Storage atomicity hand-waved | ✅ Resolved | §4.6.1.1 (new) + `ERR_DS_STORAGE_ATOMICITY` (0x160C) |
| M-J | Medium | Physics dt irrational and unbound | ✅ Resolved (BUT see Pass 4 C-1 — value introduced is wrong) | §3.4 `PHYSICS_DT` |
| L-K | Low | Two `Version History` blocks; §3.4 → §3.4.2 numbering gap | ✅ Resolved | §3.5 consolidated; §3.4.1 added |
| L-L | Low | §9.5 acceptance criterion #4 unfalsifiable | ✅ Resolved | §9.5 #4 names three concrete artifacts |
| L-M | Low | §5.5 "Deterministic flags" prose only | ✅ Resolved | §5.5.1 (new) — concrete MSVC/Clang strings |
| L-N | Low | `Reserve(siteId, count)` budget enforcement unspecified | ✅ Resolved | §3.4.1 (new) + `ERR_DS_RNG_BUDGET_MISMATCH` (0x160B) |
| L-O | Low | `actionOrdinal` and `RngCursor` widths unbound | ✅ Resolved | `HASH_INPUT_FIELD_WIDTHS` (both u64) |
| L-P | Low | `AI_NoOp` empty-scope digest semantics need normative sentence | ✅ Resolved | §3.1.2 explicit normative paragraph |
| Cross-cut | Structural | §16 approval predicated on unwritten consumer specs | ✅ Acknowledged | §8.3 sequencing constraint stands |

**Constants added in Pass 3:** `PHYSICS_DT` (incorrect value — see Pass 4 C-1), `STRING_MAX_BYTES`, `HASH_INPUT_FIELD_WIDTHS`, `NAN_CANONICAL_F32`, `NAN_CANONICAL_F64`, `ERR_DS_TIERB_NONFINITE`, `ERR_DS_RNG_BUDGET_MISMATCH`, `ERR_DS_STORAGE_ATOMICITY`.

**Sections added in Pass 3:** §3.2.4.1, §3.2.5.2, §3.4.1, §4.6.1.1, §4.8.3, §5.5.1, §5.5.2.

**Pass 3 file version sync:**

| File | Before | After |
|---|---|---|
| `section-1.md` | v0.7 | v0.9 |
| `section-3.md` | v0.8 | v0.9 |
| `section-4.md` | v0.8 | v0.9 |
| `section-5.md` | v0.7 | v0.9 |
| `section-9-approval-checklist.md` | v0.8 | v0.9 |

---

## Pass 4 — Fourth-Pass Adversarial Review (2026-05-03)

Reviewer mode: adversarial, with self-critique pass applied before publication. Severities re-calibrated after self-review (see *Self-Review Adjustments* subsection below). All findings new — none yet acted on in spec text.

### Critical

**C-1. §3.4 `PHYSICS_DT` bit pattern is wrong.**
Spec declares `PHYSICS_DT = 0x3C8888B7` as the normative reference value of `(float)(1.0/60.0)` under round-to-nearest-even, and instructs implementations to "match the bit pattern exactly." Re-derived from first principles:
- `1/60` in binary = `0.0000010001000100010001…` × 2⁰ = `1.0001000100010001000100010001…` × 2⁻⁶.
- 23-bit mantissa pre-round: `00010001000100010001000`. Round bit (24) is `1`, sticky bits nonzero ⇒ round up.
- Mantissa post-round: `00010001000100010001001`. Exponent `-6 + 127 = 121 = 01111001`. Sign 0.
- Concatenated: `0 01111001 00010001000100010001001` = `0x3C888889`.

The spec's `0x3C8888B7` decodes to mantissa `00010001000100010110111` ≈ `0.01666694…`, which is unrelated to `1/60`. Not a single-character typo of `0x3C888889` (`B7` ≠ `89`). This is a fabricated normative constant. Since Pass 3's M-J fix introduced this value, the v0.9 changelog claim that PHYSICS_DT was added "with computation rule" produced an unverified literal. **Required fix:** correct to `0x3C888889`; add a worked derivation in an appendix; re-run any existing KAT that touches this constant.

**C-2. FR-DS-013 has no defined test card.**
§5.2 maps `FR-DS-013 → T-DS-ENV-010`. `T-DS-ENV-010` is not present in §5.3 catalogue and not expanded in §5.11. FR↔test traceability is broken. **Required fix:** define T-DS-ENV-010 (float Tier-A field without environment pinning fails classification gate) in §5.3 and §5.11, OR retarget §5.2's FR-DS-013 mapping to an existing card.

**C-3a. §3.10 edge-case decision table is missing four declared error codes.**
§3.10 covers `EC-016-001..008`. §3.4 declares `ERR_DS_REPLAY_BOUNDARY` (0x1609), `ERR_DS_TIERB_NONFINITE` (0x160A), `ERR_DS_RNG_BUDGET_MISMATCH` (0x160B), and `ERR_DS_STORAGE_ATOMICITY` (0x160C). None appear in §3.10. **Required fix:** add four `EC-016-009..012` rows mapping each error to its trigger and required behavior.

**C-3b. §5.3 / §5.11 has no fault-injection cards for the same four error codes.**
Independent of C-3a, §9.5 acceptance criterion #2 ("All §3.4 error codes have at least one fault-injection test case in §5.3 or §5.11") cannot be checked while these four codes lack test cards. **Required fix:** add T-DS-FAULT-010..013 (one per orphan error code) to §5.3 and §5.11.

### Medium

**M-1. §5.5.2 `matchSeed` typing for save-tick seed.**
§5.5.2: `SipHash-2-4-64(matchSeed, "T-DS-REPLAY-004") mod 2^31` puts variable-length `matchSeed` (HKDF IKM) in the SipHash *key* slot. SipHash keys are exactly 128 bits, normatively `(k0, k1)` derived from `matchSeed` via HKDF (see §3.2.4). **Required fix:** substitute `matchSeedKey` (or `(k0, k1)`) for `matchSeed`, OR define a separate KDF derivation for the test-fixture seed.

**M-2. §3.2.4.1 `array<T>` size formula breaks for variable-width T.**
"`array<T>` | `4 + N·sizeof(T)`" assumes fixed-width `T`. For `T ∈ {string, bytes, optional<T>, array<T>, struct-with-string}`, `sizeof(T)` is undefined. **Required fix:** restrict `array<T>` to fixed-width primitives, OR add per-element-size note acknowledging variable-width payloads.

**M-3. EnvironmentFingerprint mid-match-mutation has no recording-time error code.**
§4.8.1 forbids mid-match mutation and reuses `ERR_DS_REPLAY_ENV_MISMATCH`. That code is semantically "replay-side fingerprint diverges from snapshot fingerprint." Recording-side mutation is a different failure mode. **Required fix:** add `ERR_DS_ENV_MUTATION` (or rename existing code), and add an `EC-016-*` row.

**M-4. §4.8.3 `il2cppVersion` undefined for non-IL2CPP backends.**
The float-flag tuple includes `il2cppVersion` as a mandatory string. Stage 0 is IL2CPP-only per §5.5, but editor/dev builds (used for tests, replay-on-developer-machines) may use Mono. **Required fix:** restrict the fingerprint to IL2CPP at Stage 0 explicitly, OR define a Mono fallback value (e.g., empty string with sentinel).

**M-5. §2.3 `SnapshotHeader` and snapshot record layout.**
§2.3 lists `SnapshotHeader { schemaVersion, tick, prevSnapshotDigest, environmentFingerprint }`. §3.2.3 says `currentSnapshotDigest` is "stored adjacent to the header." The on-disk *snapshot record* layout is never specified — header-payload-digest order, endianness of trailing digest, alignment — all undefined. §4.6.1.1 atomic-write contract operates on this file but the format is undefined. **Required fix:** add an explicit on-disk record layout in §3.9.2 or §4.6.

### Low

**L-1. NFC normalization Unicode version isn't pinned.**
§3.2.4.1 specifies strings are "NFC-normalized UTF-8". Unicode evolves; new code points and (rarely) normalization-table changes are added between Unicode versions. Two runtimes on different ICU/.NET Unicode tables can produce different NFC outputs for the same input string. This propagates into digest preimage and breaks Tier A bitwise equality across upgrades. **Required fix:** bind Unicode version explicitly (e.g., "NFC per Unicode 15.1 normalization tables"), add Unicode version to `EnvironmentFingerprint`, OR restrict authoritative strings to ASCII-only at Stage 0.

> *Note: this finding was added during self-review after the initial draft missed it. Originally would have been "High"; downgraded to Low only because Stage 0 authoritative strings are likely sparse and ASCII-dominant. Should be re-evaluated when string usage in authoritative state is enumerated.*

**L-2. §2.6.2 example diverges from §4.2.2 normative lifecycle.**
§2.6.2 lists 5 replay steps; §4.2.2 (normative) lists 8. §2.6 is titled "End-to-End Data Flow **Example**" so it is illustrative, not normative — but a reader cross-referencing the two sections gets divergent contracts. **Required fix:** update §2.6.2 to mirror §4.2.2's 8 steps, or add an explicit "see §4.2.2 for the normative form" pointer.

**L-3. FR-DS-009 wording missing stage qualifier.**
FR-DS-009: "Cross-platform certification suite MUST pass before release." §5.5 FR-DS-009-GATE explicitly stage-qualifies the requirement (Stage 0: not a gate; Stage 5+: blocking). The unqualified MUST in §2.1 reads as universal. **Required fix:** add "(Stage 5+)" parenthetical to FR-DS-009 OR cite FR-DS-009-GATE inline.

**L-4. §3.2.4 HKDF `salt=∅` notation is ambiguous (functionally moot for SHA-256).**
"salt=∅" could be read as empty-string OR not-provided. RFC 5869 §2.2 specifies that absent salt is treated as `HashLen` zero bytes; HMAC-SHA-256 with empty key and HMAC-SHA-256 with a 32-zero-byte key produce identical outputs (both pad to the same 64-byte all-zero block). So functionally there is no observable difference for SHA-256 under any RFC-conformant library. The remaining concern is interface-level: some libraries reject explicit empty salt with a programming error before producing output. **Required fix:** pin one notation ("salt = NULL per RFC 5869 §2.2") and bind a KAT vector that exercises it.

**L-5. §6.10 phase-budget table mixes per-tick and per-stride-tick units.**
AI row is per-stride-tick % (22%); other rows are per-tick %. Row values sum to 100% on stride ticks and 78% on non-stride ticks. The table is internally consistent if read as per-phase upper bounds, but is not labeled that way. **Required fix:** label the table as "per-phase CPU upper bound on the indicated ticks" and explicitly note non-stride slack is idle.

**L-6. Appendix G manifest doesn't reference §9.5 acceptance vectors.**
Appendix G lists `GV-RNG-001`, `GV-SNAP-001`, `GV-DIGEST-001`. §9.5 #4 names `hkdf-sha256-kat.md`, `siphash-2-4-kat.md`, `serialize-canonical-corpus.md`. Two disjoint manifests. **Required fix:** cross-reference or merge.

**L-7. AI_PHASE_STRIDE change isn't called out as digest-version-affecting.**
Changing `AI_PHASE_STRIDE` reshapes the digest rollup (which ticks emit `AI_NoOp` vs full `AI`). It is `[DERIVED]` from `PHYSICS_TICK_HZ / TACTICAL_TICK_HZ`, both `[FIXED]`. **Required fix:** note explicitly that any change to either heartbeat invalidates pre-existing replay corpus and triggers a `DigestVersion` bump.

**L-8. Tag-semantics drift: `[FIXED]` used for spec design choices.**
CLAUDE.md defines `[FIXED]` as "Fixed / physical law; derived from physics; never tune." §3.4 marks design choices (`RNG_KDF=HKDF-SHA256`, `RNG_STREAM_HASH=SipHash-2-4-64`, `LEGAL_SAVE_BOUNDARIES`, all `ERR_DS_*` codes) as `[FIXED]`. There is no project tag for "spec-pinned design constant." Project-level decision: widen `[FIXED]` semantics, add a `[SPEC]` tag, or accept the drift. **Action:** raise at next CLAUDE.md review.

### Cross-cutting

**X-1. Fabricated-constant regression risk reinforced.**
PHYSICS_DT (C-1) was added in v0.9 specifically as Pass-3 critique-fix M-J. It went in *with the wrong bit pattern*. This is the same class of bug the spec exists to prevent — a fabricated or unverified normative constant. **Recommendation:** v1.0 approval gate must require every numeric literal in §3.4 cross-checked against a programmatically-generated KAT or appendix derivation. Visual review of hex literals is insufficient.

**X-2. Approval gates remain blocked by §9.5 #4 and §8.3.**
Already tracked but worth restating: §9.5 #4 cannot be checked (three named artifacts don't yet exist), and §8.3 is independently blocked on four `NOT STARTED` dependency specs. Spec is structurally blocked from approval through at least two independent gates. Realistic next milestone: "review-ready," not "approval-ready."

**X-3. EntityId no-reuse cross-spec back-propagation still floating.**
§3.2.5 declares the constraint normatively; reciprocal `XC-002-NNN` / `XC-008-NNN` not filed in #2 / #8. A normative constraint that binds APPROVED upstream specs without their reciprocal acknowledgement is not yet binding. Self-flagged and tracked as ERR-016-002 + CLAUDE.md Open Issue.

### Self-Review Adjustments (applied to draft before publication)

The first draft of Pass 4 contained findings that did not survive a deliberate self-critique. Adjustments:

| Original draft finding | Issue | Adjustment in published list |
|---|---|---|
| H-1 §2.6.2 vs §4.2.2 contradiction | §2.6 is example-titled; not normative | Downgraded to L-2 |
| H-2 FR-DS-009 contradicts Stage 0 | FR-DS-009-GATE explicitly stage-qualifies it; spec internally consistent | Downgraded to L-3 (clarity nit) |
| H-3 HKDF salt ambiguity | HMAC-SHA-256 padding makes empty-salt and zero-salt produce identical output | Downgraded to L-4 (interface-level only) |
| H-4 Reserve invocation conditionality | Determinism flows from authoritative state; soft documentation gap, not a real bug | Withdrawn |
| M-1 phase-budget table "over/under-allocation" | Arithmetic claim was wrong; rows sum to 100% on stride ticks. Real issue is mixed units | Reframed as L-5 (units, not arithmetic) |
| L-2 RFC 2119 "MAY" misuse | "no … MAY" is a standard English prohibition construction; not a 2119 violation | Withdrawn |
| (missed in draft) Unicode normalization version | Not in original draft; surfaced during self-review | Added as L-1 |
| C-3 reasoning conflated §3.10 rows with §5.3/§5.11 cards | Two distinct gaps muddled together | Split into C-3a (table) and C-3b (cards) |

Net: severity inflation in three "High" items reduced; two pseudo-findings withdrawn; one genuine miss (NFC normalization) added.

---

## Pass 5 — Fifth-Pass Adversarial Review (2026-05-03)

Reviewer mode: adversarial; targeted re-read of all section files plus outline.md and appendices.md after Pass 4 published. Self-critique pass applied before publication. Findings here are explicitly de-duplicated against Passes 1–4 (any item already raised under another ID was dropped).

### Critical

**C-1. §3.2.3 vs §3.9.2 disagree on `SnapshotDigest` preimage field order.**
§3.2.3 normatively defines `SnapshotDigest[T] = SHA-256(SnapshotHeader[T] || SnapshotPayload[T])`. The schema for `SnapshotHeader` in §2.3 is `{ schemaVersion, tick, prevSnapshotDigest, environmentFingerprint }` (declared field order). §3.2.4.1 mandates "flat concatenation of fields in declared schema order; no reordering." But §3.9.2 expands the same preimage as `SchemaVersion || Tick || EnvironmentFingerprint || PrevSnapshotDigest || PayloadBytes` — `EnvironmentFingerprint` and `PrevSnapshotDigest` are **swapped relative to §2.3 declaration order**. Two normative orderings produce two different SHA-256 outputs. Tier-A bitwise parity is unattainable until one is removed and the other made canonical. **Required fix:** pick an authoritative order (recommend keeping §2.3's declared order so `prevSnapshotDigest` stays adjacent to `tick`), update the other section to match, and add the resolved order to the §3.2.4.1 worked-byte family with an exact SHA-256 expected value bound into `serialize-canonical-corpus.md`.

**C-2. §3.2.2 `PhaseDigest` formula omits the §3.2.4.1 `DOMAIN_TAG` (0x10).**
§3.2.2 (normative): `PhaseDigest = SHA-256(SerializeCanonical(DigestVersion || Tick || PhaseId || phaseScopeFields))` — no domain tag. §3.2.4.1 (Pass 3 H-A fix): "Each top-level digest preimage begins with a 1-byte `DOMAIN_TAG`" and the §3.2.4.1 worked example begins with `10`. §3.1.2 `AI_NoOp` description likewise references §3.2.4.1's tagged example. An implementer reading §3.2.2 in isolation produces a 12-byte preimage starting with `01 00 78 …` (no domain tag) — different SHA-256, broken parity. **Required fix:** update §3.2.2 (and §3.2.5 per-draw formula by analogous reasoning) to reference the `DOMAIN_TAG` requirement explicitly, OR rewrite §3.2.2 to read `SHA-256(SerializeCanonical(DOMAIN_TAG_PHASE || DigestVersion || Tick || PhaseId || phaseScopeFields))`. §3.2.4 hash-input field-widths table should also list `DOMAIN_TAG : u8 : 1`.

### High

**H-1. Outline `FR-DS-` numbering is permuted relative to section-2 `FR-DS-` numbering.**
`outline.md` §12 lists `FR-DS-001..008`. `section-2.md` §2.1 lists `FR-DS-001..013`. The first three IDs match (`001` tick pipeline, `002` intra-phase ordering, `003` RNG ownership), but from `FR-DS-004` onward the semantics diverge:

| ID | outline.md §12 | section-2.md §2.1 |
|---|---|---|
| FR-DS-004 | branch-safe RNG normalization | snapshot canonical binary layout |
| FR-DS-005 | snapshot canonical binary schema | replay engine reconstructs deterministic state |
| FR-DS-006 | replay reconstruction state machine | save/load atomic across authoritative domains |
| FR-DS-007 | deterministic digest protocol | divergence detection classification |
| FR-DS-008 | save/load equivalence protocol | tooling emits first divergent tick/phase/field |

`outline.md` carries `[refined]` status and v0.4 (May 2, 2026) — it is "live" enough that downstream readers will trust its IDs. CLAUDE.md flags renumbering cascades as the project's single most-recurring bug class; this is one, internal to spec #16. **Required fix:** either (a) re-sync `outline.md` §12 to mirror section-2 §2.1's full 13-FR list with matching semantics, or (b) mark `outline.md` `SUPERSEDED — see section-2.md §2.1 for canonical FRs` and freeze it.

**H-2. HKDF `info` byte encoding is undefined.**
§3.2.4 invokes `HKDF-SHA256(IKM=matchSeed, salt=∅, info="DS-RNG-KEY-v1", length=16)`. §3.2.4.1 defines `string` encoding as `u32 byte length || NFC-normalized UTF-8 bytes`. But HKDF `info` is an opaque RFC 5869 byte string passed directly to the KDF — it is not a `SerializeCanonical`-encoded field. The spec doesn't say whether the bytes fed to HKDF are (a) the raw 13 ASCII bytes `D S - R N G - K E Y - v 1`, or (b) the §3.2.4.1 length-prefixed UTF-8 form `0D 00 00 00 44 53 2D 52 4E 47 2D 4B 45 59 2D 76 31` (17 bytes). These produce different `(k0, k1)` outputs from HKDF, which then cascade into every SipHash output (StreamKey derivation, every per-draw value, the entire RNG ledger). **Required fix:** state explicitly that HKDF `info` is the raw UTF-8 bytes of the literal with NO length prefix and NO NFC normalization (since the literal is ASCII), bind a KAT row covering this exact `info` to `hkdf-sha256-kat.md` (§9.5 #4(a)). Recommend the same explicit treatment for any other HKDF/HMAC-style parameter that escapes the canonical-serializer.

### Medium

**M-1. §3.2.4.1 `enum` width "frozen with `DigestVersion`" conflates two version axes.**
"`enum` | 1 (≤256 variants) or 2 (≤65536 variants) | underlying integer value; width fixed at schema definition time and frozen with `DigestVersion`." Adding a 257th variant to a previously 1-byte enum is a *schema* change (`SchemaVersion` bump per §2.3), not necessarily a *digest algorithm* change (`DigestVersion` bump). The two are tracked as separate constants (`DETERMINISM_DIGEST_VERSION` vs `SchemaVersion`); coupling enum width to `DigestVersion` says any schema-driven enum widening also forces `DigestVersion=2` and a new replay reader. **Required fix:** rebind to `SchemaVersion`. If both bumps are intended (the digest algorithm depends on the schema), state that explicitly and rename one constant.

**M-2. Tier-A `BitwiseEqual` makes `-0.0` and `+0.0` non-equal — no canonicalization rule.**
§3.2.4.1 normalizes Tier-B NaN bit patterns to canonical quiet-NaN before serialization. Tier-A `f32`/`f64` fields are passed through as raw bit patterns. IEEE-754 `+0.0` (`0x00000000`) and `-0.0` (`0x80000000`) are arithmetically equal but bitwise distinct. Two paths producing the same arithmetic zero with different sign bits will fail Tier-A `BitwiseEqual` and be classified `HardDesync`. This is a known IEEE-754 hazard for replay-comparison codes. **Required fix:** add a Tier-A normalization step that maps `-0.0 → +0.0` before serialization for `f32`/`f64`, OR explicitly reclassify zero-producing accumulators to Tier-B with a tolerance of 0 plus zero-sign-allowed. Note in §3.3 edge-case list and add an `EC-016-*` row.

**M-3. Despawn-tombstone log persistence and tier classification missing.**
§3.2.5 requires retention of `(EntityId, finalActionOrdinal, finalRngCursor)` tombstones for every despawned entity. §3.2.5.2 says tombstones are scoped to the match and cleared at match finalization. But: (a) is the despawn log part of authoritative state? (b) does it appear in the snapshot payload? If a save occurs after despawn, replay must restore tombstones to enforce the no-reuse constraint on continued execution. (c) what is its tier? (d) what is its canonical serialization order? None specified. Without this, replay parity past any despawn-then-save sequence is undefined. **Required fix:** add `DespawnLog` to §2.3 data structures, classify Tier A, define canonical order (e.g. `EntityId` ascending), add to §3.6.1 phase-ownership table (Resolve writes? Snapshot reads only?).

**M-4. "Replay cursor" in §4.2.2 step 7 is undefined.**
§4.2.2 step 7: "Verify the replay cursor is at `EndOfSnapshot[T]`…". `ERR_DS_REPLAY_BOUNDARY` (0x1609) trips on this. §4.6.2 sequence diagram says `AssertCursorAtEndOfSnapshot[T]`. But "replay cursor" is not in §2.3 data structures, not in §3.2.5 (where `RngCursor` is), and not defined anywhere as a concept. Two implementers will model it differently. **Required fix:** either define a `ReplayCursor { tick, phaseOrdinal }` data structure in §2.3 with the legal values it can hold (one per phase boundary in the canonical pipeline), or reword step 7 in terms of an existing concept ("the most recently completed `(tick, phaseOrdinal)` is `(T, Snapshot)`").

**M-5. Draw-site registry has no schema, no file location, no example.**
§3.6.2 lists what each registry entry must contain (stable ID, owning subsystem, reserved budget, migration note), but unlike the tolerance matrix (§2.3.1 has a full per-column schema, immutability rule, and review-date discipline), the draw-site registry has no operational schema, no file path, no version contract, no example row, no review-date or owner field. The registry is the binding artifact for `ERR_DS_RNG_BUDGET_MISMATCH` (§3.4.1) and the §3.2.5.1 stable-declaration-order rule — it is more critical to per-stream determinism than the tolerance matrix is to Tier-B parity. **Required fix:** add §3.6.2.1 with the per-column schema (mirror §2.3.1 style), file location (suggest `Sim.Constants.Determinism.DrawSiteRegistry` per the constant catalogue convention), example row, immutability rule, and stream-version-bump trigger conditions.

### Low

**L-1. §3.10 EC-016-001 trigger description is incomplete.**
EC-016-001 trigger: "request during `AI`/`Physics`". `LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }` means save requested during ANY of the seven non-Snapshot phases (Input, Intent, AI, AI_NoOp, Physics, Resolve, Events) is illegal. Listing only two phases is misleading. **Required fix:** rephrase trigger as "save commit attempted at any boundary other than `EndOfSnapshot`."

**L-2. Cross-reference ID format inconsistency for ERR-016 entries.**
§3.2.5 references `ERR-016-EntityId-NoReuse` (verbal suffix). CLAUDE.md Open Issues and Pass 3 references use `ERR-016-002` (numeric suffix). Two formats for the same registry. CLAUDE.md cross-reference taxonomy is `ERR-NNN`-style. **Required fix:** rename `ERR-016-EntityId-NoReuse` to `ERR-016-002` (or whatever numeric ID the spec-error-log assigns) and grep `docs/specs/deterministic-sim/` for any other verbal-suffix variants.

**L-3. `outline.md` §13 "Open Questions" item 1 is stale.**
"Stage-0 float paths: which fields remain Tier B and for how long?" was answered by §1.3.1.1 (Pass 3 H-C fix): every parallel-touched float at Stage 0 is Tier B; Tier A is restricted to serial-path fields. Outline doesn't reflect this. Compounds with H-1 above. **Required fix:** strike the question or mark it resolved in `outline.md`.

**L-4. §3.2.4.1 `bool` and `optional<T>` decode-side error code unspecified.**
"`0x00` = false, `0x01` = true; no other byte values are legal." OK on encode side. On decode, what happens when a deserializer sees `0x02..0xFF`? Probably `ERR_DS_SCHEMA_INCOMPATIBLE`, but not stated. Same for `optional<T>` tag byte and `enum` out-of-range. **Required fix:** one sentence in §3.2.4.1 binding all illegal-byte decode paths to `ERR_DS_SCHEMA_INCOMPATIBLE` (already covers `EC-016-002` for enums).

**L-5. Appendix G manifest entries are not bound to FRs or test cards.**
Pass 4 L-6 noted Appendix G is disjoint from §9.5 #4. Adding: even within Appendix G, `GV-RNG-001`/`GV-SNAP-001`/`GV-DIGEST-001` have no FR back-references (which `FR-DS-NNN` does each vector validate?) and no test-card back-references (which `T-DS-*` exercises each vector?). The §5.10 Test Card Template lists `ArtifactPaths` but doesn't pull in golden vectors as a typed field. **Required fix:** add columns `FR mapping` and `Test card(s)` to Appendix G; add a `GoldenVectors` field to the §5.10 template.

**L-6. §6.10 "Snapshot + Digest 18%" mixes per-tick digest cost with scheduled save cost.**
Same shape as Pass 4 L-5 (which addressed the AI row). PhaseDigest is computed every tick (§3.2.2). Durable saves are scheduled, not every-tick (§1.3.0 — "saves are subscribers" to the always-running Snapshot phase). Lumping the two into one budget masks where the cost actually lands: digest is steady, save spikes on save ticks. The 18% number is averaged but not labeled as such. **Required fix:** either split into two rows (`Snapshot/Digest steady-state` and `Save commit (scheduled)`), or annotate the 18% as "averaged over a save-cadence period of N ticks" with N pinned.

### Cross-cutting

**X-1. Outline-vs-section drift is now load-bearing.**
H-1 (FR ID permutation) and L-3 (stale Open Question) are both symptoms. `outline.md` is dated v0.4 (2026-05-02) and labeled `Status: Draft outline (refined)`, but section files have moved to v0.9 with substantially different FR sets, terminology (`AI_NoOp`, `EnvironmentFingerprint`, `DOMAIN_TAG`, etc.), and resolved-vs-open scope. Two documents, both presented as live, contradicting each other. **Recommendation:** at next maintenance pass either freeze `outline.md` with a banner pointing to section files as canonical, or re-sync §12 / §13 to match section-2 §2.1 and the spec's resolved decisions.

### Self-Review Adjustments (applied to draft before publication)

| Original draft finding | Issue | Adjustment in published list |
|---|---|---|
| H-x §3.2.4 SipHash same-key-for-stream-and-draw is a security issue | SipHash is a PRF; reusing key with disjoint inputs is sound | Withdrawn (not a determinism bug) |
| H-y `STRING_MAX_BYTES = 65536` with u32 length-prefix is inconsistent | Bound is a policy ceiling, not a contradiction; u32 width is for the on-wire encoding, the limit is enforced separately | Withdrawn |
| M-z §5.5.2 `mod 2^31` introduces modulo bias on save-tick selection | Bias is functionally negligible at typical scenario lengths (hundreds–thousands of ticks vs 2³¹); not falsifiable as a parity defect | Withdrawn |
| L-w §3.4 `PHYSICS_DT` is `[DERIVED]` but spec also pins a literal — tag/value tension | Already implicit in Pass 4 C-1 (wrong literal) and X-1 (fabricated-constant regression risk); folding here would duplicate | Withdrawn (covered by Pass 4) |
| L-v §3.2.5 cross-spec `XC-016-001` for fatigue/coordinate refs uses CLAUDE.md format inconsistently | On re-read it does conform to CLAUDE.md `XC-NNN-` taxonomy | Withdrawn |

Net Pass 5: 2 C + 2 H + 5 M + 6 L + 1 cross-cutting published. Five draft findings withdrawn during self-critique.

---

## Outstanding Items (cross-pass roll-up)

| Item | Source | Severity | Owner | Tracker |
|---|---|---|---|---|
| Correct PHYSICS_DT bit pattern to `0x3C888889` | Pass 4 C-1 | Critical | Spec author | This log |
| Define T-DS-ENV-010 OR retarget FR-DS-013 mapping | Pass 4 C-2 | Critical | Spec author | This log |
| Add EC-016-009..012 rows for orphan error codes | Pass 4 C-3a | Critical | Spec author | This log |
| Add T-DS-FAULT-010..013 fault-injection cards | Pass 4 C-3b | Critical | Spec author | This log |
| Fix SipHash key argument in §5.5.2 save-tick seed | Pass 4 M-1 | Medium | Spec author | This log |
| Fix `array<T>` size formula for variable-width T | Pass 4 M-2 | Medium | Spec author | This log |
| Add recording-time env-mutation error code | Pass 4 M-3 | Medium | Spec author | This log |
| Define snapshot record on-disk layout | Pass 4 M-5 | Medium | Spec author | This log |
| Pin Unicode version OR restrict authoritative strings to ASCII | Pass 4 L-1 | Low (re-evaluate) | Spec author + Sim lead | This log |
| Mirror §4.2.2 lifecycle into §2.6.2 | Pass 4 L-2 | Low | Spec author | This log |
| Eight further L-* hygiene items | Pass 4 L-3..L-8 | Low | Spec author | This log |
| Back-propagate EntityId no-reuse to specs #2 and #8 | Pass 3 M-F | Medium | Lead developer (touches APPROVED specs) | ERR-016-002 + CLAUDE.md Open Issues |
| Author three golden-vector files referenced by §9.5 #4 | Pass 3 L-L | Medium | Systems Engineering owner of `DeterministicRngService` | §9.5 #4 |
| Revisit Tier-A scope per-field once parallel-reduction surface is implementation-known | Pass 3 H-C | Medium (deferred) | Simulation lead | §1.3.1.1 |
| FR-DS rows for new error codes / sample protocol (optional) | Pass 3 follow-up | Low | Spec author | §2.1 |
| §8.3 deferred dependencies (#9, #17, #18, #19) reach `IN REVIEW` | Pass 2 D-15, Pass 3 cross-cut | Structural blocker on approval | Cross-spec planning | §8.3, §9.4 |
| Resolve §3.2.3 vs §3.9.2 `SnapshotDigest` field-order contradiction | Pass 5 C-1 | Critical | Spec author | This log |
| Add `DOMAIN_TAG` to §3.2.2 `PhaseDigest` formula (and analogous §3.2.5 per-draw) | Pass 5 C-2 | Critical | Spec author | This log |
| Re-sync `outline.md` §12 FR list with `section-2.md` §2.1, OR mark outline `SUPERSEDED` | Pass 5 H-1 | High | Spec author | This log |
| Define HKDF `info` byte encoding explicitly; bind KAT row | Pass 5 H-2 | High | Spec author | This log |
| Rebind `enum` width "frozen with" axis from `DigestVersion` to `SchemaVersion` | Pass 5 M-1 | Medium | Spec author | This log |
| Add Tier-A `-0.0`/`+0.0` canonicalization rule | Pass 5 M-2 | Medium | Spec author | This log |
| Specify despawn-tombstone log persistence, tier, and canonical order | Pass 5 M-3 | Medium | Spec author | This log |
| Define `ReplayCursor` data structure (or reword §4.2.2 step 7) | Pass 5 M-4 | Medium | Spec author | This log |
| Add §3.6.2.1 draw-site registry schema (mirror §2.3.1) | Pass 5 M-5 | Medium | Spec author | This log |
| Six L-* hygiene items (EC trigger; ID format; stale outline; bool/optional decode error; Appendix G FR/test back-refs; §6.10 budget split) | Pass 5 L-1..L-6 | Low | Spec author | This log |

---

## Appendix — File History

| Original log | Date range | Findings count | Status |
|---|---|---|---|
| `adversarial-review.md` | 2026-05-01 – 2026-05-02 | 10 (Pass 1) + 21 (Pass 2) | Merged into this file; deleted |
| `third-pass-fix-log.md` | 2026-05-03 | 17 (Pass 3) | Merged into this file; deleted |

## Version History

- **v1.1 (May 3, 2026):** Appended Pass 5 (fifth-pass adversarial, 2026-05-03). Sixteen new findings (2 C + 2 H + 5 M + 6 L + 1 cross-cutting), all de-duplicated against Passes 1–4. Five draft findings withdrawn during self-review. Promotion-blocker list and Outstanding Items roll-up updated with Pass 5 entries. No section files modified.
- **v1.0 (May 3, 2026):** Initial consolidated critique log. Merges Pass 1 (outline review, 2026-05-01), Pass 2 (full §1–§9 + appendices, 2026-05-02), and Pass 3 (third-pass adversarial, 2026-05-03) from the now-removed `adversarial-review.md` and `third-pass-fix-log.md`. Adds Pass 4 (fourth-pass adversarial, 2026-05-03) with self-review adjustments applied (3 downgrades, 1 withdrawal, 1 missed finding added).
