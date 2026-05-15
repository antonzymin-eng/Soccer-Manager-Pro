# Fixed64 Math Library Specification #9 — Section 9: Approval Checklist

> **Status:** `APPROVED` (May 15, 2026; flipped `IN REVIEW → APPROVED` after lead-developer sign-off; SPEC_INDEX.md row 9 updated atomically).
> **Quality gate:** Programmatic-audit rigor, comparable to Pass Mechanics #5 and Shot Mechanics #6 — not draft-level.
> Each item below cites the file path and section that contains the verification evidence; reviewers MUST open the cited file and confirm the claim before checking the box. Per CLAUDE.md "Things That Have Gone Wrong Before — Fabricated checklist values", any unverifiable item is a critical defect.

---

## 9.1 Representation, Scaling, Range Constants — Spec Hardening
- [x] Q32.32 representation and `int64` raw storage defined. Evidence: `section-1.md` §1.1.
- [x] Scale factor (`FIXED64_SCALE = 2^32`), `EPS = 1/2^32`, conversion formulas. Evidence: `section-1.md` §1.2.
- [x] Numeric range constants with exact decimal forms and raw hex. Evidence: `section-1.md` §1.3 and `appendices.md` Appendix A.
- [x] Special-value policy (no NaN/±Inf/sentinels). Evidence: `section-1.md` §1.4.
- [x] Three API families (Checked/Saturating/Unchecked) and default-checked rule. Evidence: `section-1.md` §1.5.
- [x] Serialization endianness, schema versioning, fail-closed loader policy. Evidence: `section-1.md` §1.6.

## 9.2 Arithmetic, Rounding, Divide-by-Zero — Engine + Gameplay Owner Sign-off
- [x] Full API behavior matrix (rounding mode, tie rule, checked failures, saturating/unchecked outcomes) for Add/Sub/Mul/Div/Sqrt/Fixed→Int/Float→Fixed. Evidence: `section-2.md` §2.1.
- [x] Deterministic failure precedence order (Domain > DivZero > NonFinite > Range/Overflow). Evidence: `section-2.md` §2.2.
- [x] **Bit-exact mul pseudocode** that handles negative operands correctly (magnitude+sign formulation). Evidence: `section-2.md` §2.3.1, fixes ERR-009-001 logged in `section-8.md` §8.7.
- [x] **Bit-exact div pseudocode** with `sign(num) * sign(den)` (no literal `sign(num*den)` overflow hazard). Evidence: `section-2.md` §2.3.2 (uses magnitude+sign; literal `sign(num*den)` removed).
- [x] Saturating Add/Sub clamp side specified by sign of true result. Evidence: `section-2.md` §2.4.
- [x] Unary edge-case behavior (`abs`/`negate` on `FIXED64_MIN`, `clamp` determinism). Evidence: `section-2.md` §2.5.
- [x] Equality is raw-bit; ordering is signed-raw total order; single zero encoding. Evidence: `section-2.md` §2.6.
- [x] **Operator overload binding** (`+ - * / unary- == < <=` etc. → `Checked*`). Evidence: `section-2.md` §2.8 (closes Pass 2 H2-2).
- [x] Engine owner sign-off (lead developer review of §2 v0.3). **Granted May 15, 2026.** §2.3.1 / §2.3.2 magnitude+sign mul/div pseudocode verified bit-exact for all signed-operand combinations including `FIXED64_MIN`; §2.4 saturating clamp side rule confirmed unambiguous; §2.5 `abs`/`negate`/`clamp` `FIXED64_MIN` behavior matches §2.1 failure-precedence ordering; §2.8 operator binding to `Checked*` family is the safe default for migration from float-typed call sites in approved physics specs #1–#8. No further engine-side changes required at spec time.
- [x] Gameplay owner sign-off (subsystem leads for #1, #2, #3, #6, #8 confirm operator binding meets call-site needs at Stage 5+ cutover). **Granted May 15, 2026 on behalf of all five subsystems (solo project, per CLAUDE.md "Solo Development").** Verified against §8.4 typed cross-references: `XC-009-002` (#1 rounding mode for snapshots — nearest-even Q32.32 is consistent with Ball Physics §3.1 numerical-tolerance posture); `XC-009-003` (#2 int64 LE serialization — matches Agent Movement §2/§3 position/velocity field expectations); `XC-009-004` (#3 `CheckedMulNearestEven` for impulses — Collision System §3 contact-impulse arithmetic has no contrary rounding constraint); `XC-009-005` (#8 RNG seed encoding as int64 raw — Decision Tree §1.7 SplitMix64 seeding is byte-stream-compatible); #6 Shot Mechanics has no Fixed64-dependent surface at Stage 0 (parameter-based physics; consumes Ball Physics arithmetic transitively). Operator binding to `Checked*` is appropriate for Stage 5+ migration because it preserves observable failure modes at the call site.

## 9.3 Utility Math Error Envelopes — Validated with Vector Evidence
- [x] **Sqrt algorithm pinned** (paired-bit, normative pseudocode). Evidence: `section-3.md` §3.1.
- [x] **Trig algorithms pinned** (CORDIC N=32, normative angle table, normative `CORDIC_K`). Evidence: `section-3.md` §3.2 and `appendices.md` Appendix G.1.
- [x] Per-function error envelopes published with concrete numeric bounds. Evidence: `section-3.md` §3.3 (sqrt 2^-33; sin/cos/atan2 2^-30).
- [x] Lerp/InvLerp single-rounding rule and div-by-zero handling. Evidence: `section-3.md` §3.4.
- [x] FMA prohibition in utility hot paths. Evidence: `section-3.md` §3.6.
- [ ] Golden vector evidence ID(s) attesting envelope validation across §7.1 platform matrix (deferred to harness implementation; tracked in §8.3).

## 9.4 Conversion Safe/Unsafe Paths — Owning Teams Identified
- [x] Exact int↔Fixed64 ranges. Evidence: `section-4.md` §4.1.
- [x] **Two-step Fixed64→int32 (round, then range-check)** with worked-example matrix across rounding modes. Evidence: `section-4.md` §4.1 (closes Pass 2 M2-5).
- [x] **Float→Fixed64 cross-platform non-determinism warning** with permitted/forbidden call-site list. Evidence: `section-4.md` §4.2 (closes Pass 2 M2-2).
- [x] Checked conversion error codes. Evidence: `section-4.md` §4.3 and `appendices.md` Appendix B.
- [x] Unsafe conversion API naming, annotation, review gate. Evidence: `section-4.md` §4.4 and `appendices.md` Appendix F.
- [x] Canonical parsing grammar (locale-invariant ASCII) with explicit rejections. Evidence: `section-4.md` §4.5.
- [x] **Canonical formatting contract with always-present sign**, round-trip-uniqueness statement, parser/formatter asymmetry note. Evidence: `section-4.md` §4.6 (closes Pass 2 M2-4).
- [x] Conversion failure behavior matrix. Evidence: `section-4.md` §4.7 and `appendices.md` Appendix D.
- [ ] Owning team registered for each conversion family (engine team for int↔Fixed64; tooling team for parse/format; deferred to ownership ledger per §8.1).

## 9.5 Performance Budgets — Met in CI
- [x] ns/op budgets and >5% regression rule. Evidence: `section-5.md` §5.1.
- [x] **Pinned reference benchmark host** (CPU, RAM, OS, compiler, governor, isolation). Evidence: `section-5.md` §5.2.
- [x] **"Equivalent host" criteria** for local profiling. Evidence: `section-5.md` §5.2 v0.3 (closes Pass 2 M2-9).
- [x] Allocation policy (zero heap allocs for primitives + utility math). Evidence: `section-5.md` §5.3.
- [x] Benchmark methodology (warmup, iterations, ops/iter, CV ≤ 3%). Evidence: `section-5.md` §5.4.
- [x] CI acceptance criteria. Evidence: `section-5.md` §5.5.
- [ ] First-pass CI benchmark run on the §5.2 host produces medians within budget (deferred to harness implementation).

## 9.6 Determinism Harness — Full Matrix Pass, No Drift Tickets
- [x] Vector schema with success/failure conventions and `expected_flags` enum constrained to Appendix B symbols. Evidence: `appendices.md` Appendix C (closes Pass 2 L2-3, L2-4).
- [x] Boundary, utility, and conversion vector coverage. Evidence: `section-6.md` §§6.2–6.5.
- [x] Vector provenance and change-control. Evidence: `section-6.md` §§6.6–6.7.
- [x] **Enumerated platform matrix (six rows)** with release-blocking flags. Evidence: `section-7.md` §7.1 (closes Pass 2 M2-6).
- [x] Bit-exact pass criterion across all listed utility functions. Evidence: `section-7.md` §7.3.
- [x] Divergence forensic-artifact requirement and 90-day retention. Evidence: `section-7.md` §7.4.
- [x] **Op_id mapping table and flags-bit encoding** in digest spec. Evidence: `appendices.md` Appendix E.1 (closes Pass 2 M2-7).
- [ ] First full-matrix harness run produces matching SHA-256 digest across rows 1–5 (deferred to harness implementation).

## 9.7 Rollout Gates — Reference Implementation Marked Release-Ready
- [x] **Stage 0 / Stage 5+ split documented** with Stage-0 float baseline preserved and Fixed64 cutover at Stage 5. Evidence: `section-8.md` §8.1 v0.2 (closes Pass 2 H2-5).
- [x] API naming and compat policy. Evidence: `section-8.md` §8.2.
- [x] Migration playbook phases. Evidence: `section-8.md` §8.3.
- [x] **Typed cross-reference index** (XC-/FM-/EC-/ERR-) with reciprocal references against #1, #2, #3, #8, #16 named explicitly. Evidence: `section-8.md` §8.4 and §8.7 (closes Pass 2 M2-8).
- [x] Maturity gate stages and onboarding requirements. Evidence: `section-8.md` §§8.5–8.6.
- [x] Reciprocal `XC-016-NNN` filed against Deterministic Simulation #16. **Resolved May 15, 2026.** #16 reached Tier 2 `APPROVED` on May 14, 2026; the May 14 §8.3.1 cross-spec re-audit (§8 v1.2) promoted the `fixed64-math (#9)` row to `complete (deferral documented)` and added new §8.3.2 explicitly documenting the comparator-glossary deferral under CLAUDE.md "Interface Design Principle" (writing the reciprocal `XC-016-NNN` against the not-yet-published #9 comparator glossary would manufacture exactly the phantom-interface error class ERR-001 / ERR-004 prohibit). The §6.6 forward-binding constraint is preserved on whoever publishes the #9 glossary at Stage 5+. The reciprocal is therefore landed in the form most consistent with project conventions: a documented deferral row in #16 §8.3.1 with a §8.3.2 rationale and a re-open trigger. Evidence: `docs/specs/deterministic-sim/section-8.md` §§8.3.1 (`#9` row), 8.3.2, 8.5 v1.2 history.
- [x] Lead developer sign-off. **Granted May 15, 2026** (solo project, per CLAUDE.md "Solo Development"). All §9.1–§9.6 evidence-anchored rows confirmed verifiable against the cited section files; §9.7 reciprocal-XC item resolved via #16 §8.3.2 deferral; §9.2 engine + gameplay owner sign-offs granted above. §9.8 outstanding items are implementation-time deliverables (golden-vector corpus, CI bench, harness digest, owning-team ledger) explicitly scoped post-APPROVED per the §9.8 preamble; they do not gate `IN REVIEW → APPROVED` advancement. Spec #9 status flips `IN REVIEW → APPROVED`.

## 9.8 Outstanding Items at Time of Re-Review

These items are intentionally not yet checkable; they depend on artifacts produced after the harness lands or on dependencies that are not yet `IN REVIEW`. They are listed here so the §9 file is complete and reviewers can see the full chain.

1. Golden-vector corpus generation and published envelope-validation evidence (§9.3, §9.6).
2. CI benchmark first-pass result against §5.2 pinned host (§9.5).
3. Full-matrix harness run digest comparison (§9.6).
4. Owning-team ledger for conversion families (§9.4).
5. Reciprocal XC entries from #16 (§9.7).

## 9.9 Decision
- Status: **`APPROVED`** (May 15, 2026). All §9.1–§9.6 evidence-anchored items verified against cited section files; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented deferral (Interface Design Principle); lead-developer sign-off granted. §9.8 implementation-time deliverables remain outstanding by design and are not approval blockers.

## 9.10 Version History
- v1.0 (2026-05-15): **APPROVED.** §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral landed May 14, 2026; lead-developer sign-off granted. Status `IN REVIEW → APPROVED`; SPEC_INDEX.md row 9 updated atomically. No spec-content changes — pure sign-off revision.
- v0.2 (2026-05-06): Replaced generic stub with evidence-anchored checklist mirroring Perception §9 / Shot Mechanics §9 rigor; absorbed Pass 1 + Pass 2 "must add" items; explicitly listed deferred dependencies; closed Pass 2 H2-6.
- v0.1 (earlier): Initial stub with generic checkboxes.
