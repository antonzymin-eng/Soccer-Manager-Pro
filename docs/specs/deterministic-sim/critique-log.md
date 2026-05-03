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

**Spec status:** `IN PROGRESS`. Promotion to `IN REVIEW` blocked on:
1. Fourth-pass critical-tier fixes (C-1, C-2, C-3a, C-3b — see Pass 4 below).
2. §9.5 implementation-readiness review (gating item still unchecked).
3. §8.3 deferred dependencies (#9 Fixed64, #17 Event System, #18 Performance Optimization, #19 Testing Strategy) reaching `IN REVIEW`.
4. Three golden-vector files named in §9.5 #4.

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

---

## Appendix — File History

| Original log | Date range | Findings count | Status |
|---|---|---|---|
| `adversarial-review.md` | 2026-05-01 – 2026-05-02 | 10 (Pass 1) + 21 (Pass 2) | Merged into this file; deleted |
| `third-pass-fix-log.md` | 2026-05-03 | 17 (Pass 3) | Merged into this file; deleted |

## Version History

- **v1.0 (May 3, 2026):** Initial consolidated critique log. Merges Pass 1 (outline review, 2026-05-01), Pass 2 (full §1–§9 + appendices, 2026-05-02), and Pass 3 (third-pass adversarial, 2026-05-03) from the now-removed `adversarial-review.md` and `third-pass-fix-log.md`. Adds Pass 4 (fourth-pass adversarial, 2026-05-03) with self-review adjustments applied (3 downgrades, 1 withdrawal, 1 missed finding added).
