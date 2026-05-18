# Attacking AI Specification #15 — Section 9: Approval Checklist

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** IN REVIEW (section files complete; awaiting KD-sequencing preconditions and lead-developer sign-off)
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 9.1 Self-Contained Spec Content

All items below must be confirmed before this spec advances from
`IN REVIEW` to `APPROVED`.

| Item | Status | Evidence |
|---|---|---|
| All 22 outline.md + adversarial-review v1 findings resolved | ✓ COMPLETE | §9.4 mapping table; all 13 outline.md + 9 adversarial-review v1 findings mapped |
| All 36 FRs present and cross-referenced to algorithm sections | ✓ COMPLETE | §2.1 FR table; all 36 FRs (FR-AT-001..036) authored |
| All constants tagged (`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`) | ✓ COMPLETE | §6.1; 38 constants: 33 `[GT]`, 3 `[CROSS]`, 1 `[DERIVED]`, 1 `[CROSS-PENDING]`; 0 `[EST]` (ATTACK_DWELL_TICKS promoted to `[GT]` in §6.1 / Appendix A §A.1) |
| All formulas include units, valid input ranges, and ≥1 worked example | ✓ COMPLETE | §3.4 (RunParameters — full worked example + 4-scenario Appendix B); §3.6 (width-holding formula + worked example); §3.7 (weak-side formula); §3.8 (overload + worked example); §3.9 (TransitionController pseudocode) |
| All cross-spec citations grep-verified at section-file draft time | ✓ COMPLETE | §8.1 XC-015-001..027; all cited sections exist in current spec files |
| Stage-binding statement (§1.8) makes Stage-0 / Stage-1 split unambiguous | ✓ COMPLETE | §1.8; KD-9; KD-17; §7.1 preconditions |
| No PatternType / RunType / OverlapType enum in any spec text or code stub | ✓ COMPLETE | FR-AT-010; KD-8; §4.7 grep check declared; `RunParameters` has exactly 3 fields per FR-AT-011 |
| No action-selection logic (PASS / SHOOT / DRIBBLE) in #15 scope | ✓ COMPLETE | FR-AT-007; KD-3; §4.7 grep check declared |
| All `[EST]` constants promoted to `[GT]` with Appendix A derivations | ✓ COMPLETE | `ATTACK_DWELL_TICKS` promoted; Appendix A §A.1 derivation present |
| Stage-0 dangerous-zone surrogate metric declared and measurable | ✓ COMPLETE | §5.7; KD-10; DANGER_ZONE_MAX_DIST_M and DANGER_ZONE_CORRIDOR_HW_M in §6.1 |
| Tactical-identity acceptance criteria measurable | ✓ COMPLETE | §5.8; KD-10; DIRECT_RUN_COUNT_DELTA and COUNTER_MAX_HOLD_TICKS in §6.1 |
| Test plan meets ≥ 85 test target | ✓ COMPLETE | §5.1: 52 unit + 12 integration + 6 determinism + 3 performance + 6 anti-chaos + 6 profile = 85 |
| Boundary matrix authored and consistent with adjacent specs | ✓ COMPLETE | §1.6; verified against #13 / #14 / #12 / #8 section files |

---

## 9.2 Cross-Spec Sign-Offs Required

| Sign-off | From | Subject | Status |
|---|---|---|---|
| ERR-015-001 ratification | #16 lead-developer | `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocated in #16 §3.4; `[CROSS-PENDING]` promoted to `[CROSS]` | **OPEN** |
| ERR-015-002 ratification | #8 owner | `TacticalContext.AttackIntent[]?` field added to #8 §2.2.6; §3.1.7 updated | **OPEN** |
| #12 RunIntent writer-layer contract | #12 owner | `RunIntent` writer-layer per #12 §4.5 confirmed as the Stage 1+ integration surface | **OPEN** (blocked on #12 APPROVED) |

---

## 9.3 KD-Sequencing Preconditions (gates `IN REVIEW → APPROVED`)

All items below must clear before the lead-developer R-01..R-05 sign-off
is valid.

| # | Precondition | Status |
|---|---|---|
| (a) | ERR-015-001 domain-tag `0x1B` ratified via #16 §3.4 patch; `DOMAIN_TAG_ATTACKING_AI` promoted `[CROSS-PENDING]` → `[CROSS]` | **OPEN** |
| (b) | ERR-015-002 mechanism (Option B: `TacticalContext.AttackIntent[]?`) ratified by lead developer; amendment text filed for #8 | **OPEN** |
| (c) | All `[CROSS-PENDING]` tags in this spec promoted to `[CROSS]` (currently only `DOMAIN_TAG_ATTACKING_AI`) | **OPEN** (depends on (a)) |
| (d) | `ATTACK_DWELL_TICKS` confirmed `[GT]` with Appendix A derivation present | ✓ COMPLETE (promoted in §6.1 + Appendix A §A.1) |
| (e) | #12 `RunIntent` writer-layer accessor name confirmed grep-verified against `positioning-ai/section-4.md` at section-file draft | ✓ COMPLETE (§4.5.2 confirmed per XC-015-011; `positioning-ai/section-4.md` §4.5.2 text verified) |
| (f) | ERR-015-005 back-prop amendment to #8 §1.3.2 ratified (adds "Attacking AI #15" to multi-agent-coordination deferral row) | **OPEN** |
| (g) | Lead-developer R-01..R-05 review pass | **OPEN** |

---

## 9.4 Finding-to-Resolution Map

Complete mapping of all findings from `outline.md` (May 6, 2026 review)
and `adversarial-review-outline-detailed-v1.md` (May 17, 2026 review)
to their resolutions in `outline-detailed.md` v1.1 and the section files.

### From outline.md (May 6, 2026 adversarial review — 13 findings)

| # | Finding | Sev | Resolution |
|---|---|---|---|
| 1 | Missing metadata header | H | `outline-detailed.md` "METADATA HEADER" section; §1.1 Purpose; §1.8 Stage-Binding |
| 2 | Section plan misaligned with CLAUDE.md 9-section template | H | Full §1–§9 + appendices template followed; Stage-Binding Clarification section in outline-detailed.md |
| 3 | §3 collides with Decision Tree #8 (action selection) | H | KD-3: #15 does not own action selection; "CRITICAL BOUNDARY DECISION" section in outline-detailed.md; §1.1 purpose statement; FR-AT-007; §4.7 grep check |
| 4 | Pattern-template enum risk (overlap, underlap, cutback) | H | KD-8: no PatternType / RunType enum anywhere; `RunParameters` = 3-field parameterization; FR-AT-010 / FR-AT-011; vocabulary in Appendix F glossary only |
| 5 | xG acceptance criteria infeasible at Stage 0 | H | KD-10: dangerous-zone shot surrogate + average shot distance; §5.7; DANGER_ZONE_MAX_DIST_M / DANGER_ZONE_CORRIDOR_HW_M in §6.1 |
| 6 | Boundary with Defensive AI #14 §4 unstated | M | KD-6: mutual exclusion by possession phase; FR-DA-013 cited; emergencyFlag Stage 1+ boundary hint; §1.6 boundary matrix; §7.3 |
| 7 | Boundary with Positioning AI #12 unstated | M | KD-4: #12 owns baseline slot; #15 writes RunIntent writer-layer per #12 §4.5; §1.6 boundary matrix; §4.5.2 |
| 8 | Determinism plan absent | M | KD-11: EntityId-ascending iteration; `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]`; §4.6 digest scope; ERR-015-001 filed |
| 9 | Coordinate convention unmentioned | M | KD-16: full coordinate binding (corner-origin, final-third formula, weak-side Y-threshold); §1.7 |
| 10 | Tick-rate split (10 Hz / 60 Hz) unstated | M | KD-2 + §1.7: 10 Hz tactical, 60 Hz physics; FR-AT-001 |
| 11 | Constant-tag policy not invoked | M | KD-14 + §6.1: full 38-constant catalogue with tags; Appendix A derivations |
| 12 | No event production declared | L | KD-15: `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` channels deferred Stage 1; ERR-015-003 / ERR-015-004 |
| 13 | "Tactical identity" unmeasurable | L | KD-10 + §5.8: DIRECT_RUN_COUNT_DELTA (measurable from AttackIntent histograms); COUNTER_MAX_HOLD_TICKS (measurable from TransitionHoldState log) |

### From adversarial-review-outline-detailed-v1.md (May 17, 2026 — 9 findings)

| # | Finding | Sev | Resolution |
|---|---|---|---|
| H-1 | `relativeAngle_rad` declared in RunParameters but fully derived; FR-AT-011 inconsistent | H | `relativeAngle_rad` removed from RunParameters struct; FR-AT-011 updated to exactly 3 fields; angle computed at use-site only (§3.4 formula); "PARAMETERIZED MOVEMENT" section in outline-detailed.md |
| H-2 | `laneAssignment.lateralBias` non-existent field; `lineMembership` / `laneAssignment` conflation | H | §3.4 rewritten to use `formationSlot.lateralPct − 0.5` (confirmed #12 §2.2 field); §3.3 step 2a corrected to `formationSlot.lineMembership` (ATTACK or MIDFIELD); XC-015-010 / XC-015-013 verify field names |
| H-3 | #8 §1.3.2 citation inaccurate — #15 not explicitly named | H | §1.3.1, §1.8, XC-015-005 corrected to "covered by implication"; ERR-015-005 filed for back-prop to #8 §1.3.2 |
| M-4 | `TOUCHLINE_HOLD_Y_M` name implies absolute Y but means distance | M | Renamed to `TOUCHLINE_HOLD_DIST_M [GT]`; §3.6 formula rewritten with explicit per-side derivation (see §6.1.4) |
| M-5 | `rotate()` call undefined — coordinate frame ambiguous | M | §3.4 defines `teamAttackAngle` as match-half constant (`0.0` or `π`); explicit `depthVec = Vector2(cos(...), sin(...)) × depthOffset_m`; `lateralVec = Vector2(−sin(...), cos(...)) × lateralOffset_m`; output in pitch-frame (X=goal-to-goal, Y=touchline-to-touchline) |
| M-6 | Transition SET (§3.9) / DECREMENT (§3.1) presented in wrong order | M | §3.1 is now a pure gate that dispatches to §3.9; §3.9 (`TransitionController`) owns SET-then-DECREMENT ordering; §3.13 pseudocode reflects this |
| L-7 | `DANGER_ZONE_CORRIDOR_HW_M` "2.23× factor" unexplained | L | Value updated to 10.16m; derived from FIFA penalty-area half-width (20.16m) / 2 = 10.08m, rounded to 10.16m for 6-yard-box + GK reach; Appendix A §A.3 derivation |
| L-8 | `overloadFlank` LEFT/RIGHT acceptability unresolved | L | KD-8 extended with scope clarification: `overloadFlank` is a spatial discriminator (analogous to #14's `MarkAssignment.mode`), not a movement-pattern enum; acceptable per KD-8 |
| L-9 | §3.3 uses `laneAssignment` where `lineMembership` is correct | L | §3.3 step 2a corrected to `formationSlot.lineMembership` (same fix as H-2 above) |

---

## 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

These sign-offs are gated on all §9.3 preconditions (a)–(f) being
`COMPLETE`. Do not sign off while any precondition is `OPEN`.

| Sign-off | Question | Status |
|---|---|---|
| R-01 | Content completeness — all sections (§1–§9, Appendices A–G) present and complete per CLAUDE.md 9-section template? | ☐ |
| R-02 | Technical accuracy — all formulas, pseudocode, constants, and worked examples correct and consistent? | ☐ |
| R-03 | Cross-spec consistency — all XC-015-NNN citations point to sections that exist and say what is claimed? | ☐ |
| R-04 | Stage-binding correctness — Stage-0 / Stage-1 split is unambiguous; no Stage-1 interfaces authored at Stage 0; no Stage-0 code stubs? | ☐ |
| R-05 | Approval granted — `SPEC_INDEX.md` row 15 to flip `IN REVIEW → APPROVED`; ERR-015-NNN back-prop amendments dispatched: | ☐ |

**Date of approval:** _______________

---

## 9.6 Open Issues at IN REVIEW

Items that gate `IN REVIEW → APPROVED` (per §9.3):

- **OI-001** — ERR-015-001: `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocation
  in #16 §3.4. If #11 or #12 reaches `APPROVED` first and claims `0x1B`
  (per first-to-APPROVED precedent), this spec's domain tag shifts to the
  next available slot. Verify `spec-error-log.md` ERR-012-001 block
  immediately before filing ERR-015-001 back-prop to #16.

- **OI-002** — ERR-015-002: `TacticalContext.AttackIntent[]?` field
  ratification in #8 §2.2.6. Option B selected (mirrors PressDirective?
  / MarkDirective? pattern). Back-prop text authored at Stage 1.

- **OI-003** — ERR-015-005: one-token back-prop to #8 §1.3.2 to add
  "Attacking AI #15" explicitly to the multi-agent-coordination deferral
  row. Follows ERR-012-002 / ERR-013-004 one-token-patch precedent.

- **OI-004** — Lead-developer R-01..R-05 sign-off (§9.5).

Non-blocking (do not gate APPROVED):

- **OI-005** — ERR-015-003 / ERR-015-004: `ATTACK_RUN_STARTED` /
  `OVERLOAD_DECLARED` channel registration in #17 §3.10. Stage 1
  deliverable; does not block Stage-0 spec approval.

- **OI-006** — #18 Appendix F.0 channel-registry rows for #15 channels.
  Stage 1 deliverable per #18 §7.2 schedule.

---

## 9.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. Status set to IN REVIEW. §9.1–§9.7 authored. 22-finding resolution map complete. 7 preconditions (a)–(g) tabulated; (d) and (e) marked COMPLETE. 6 open issues declared. |
