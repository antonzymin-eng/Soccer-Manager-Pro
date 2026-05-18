# Defensive AI Specification #14 — Section 9: Approval Checklist

**Created:** May 17, 2026
**Last Updated:** May 18, 2026 (v0.3 — APPROVED: all preconditions resolved; lead-developer R-01..R-05 signed)
**Version:** 0.3
**Status:** APPROVED

---

This checklist is the normative quality gate for transitioning Defensive AI #14
from `IN REVIEW` to `APPROVED`. All items must be resolved before lead-developer
sign-off (R-01..R-05 in §9.5). Section references are verified against actual
source files in the `defensive-ai/` folder; no checklist entries are fabricated
(CLAUDE.md "Never fabricate verification values in Approval Checklists").

---

## 9.1 Self-Contained Spec Content

Each item below is verifiable against the section files in the current
`docs/specs/defensive-ai/` folder.

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | All 14 outline.md adversarial-review findings resolved | [x] | §9.4 finding-to-resolution map |
| 2 | All 37 FRs (FR-DA-001..037) present and numbered | [x] | `section-2.md` §2.1 |
| 3 | All 37 FRs trace to at least one test | [x] | `section-5.md` §5.8 traceability matrix |
| 4 | All constants carry exactly one tag ([GT], [CROSS], [CROSS-PENDING]) | [x] | `section-6.md` §6.1 (26-entry catalogue: 22 [GT] + 4 [CROSS]/[CROSS-PENDING]) |
| 5 | All [GT] constants have Appendix A derivation entries | [x] | `appendices.md` Appendix A (22 derivations) |
| 6 | Stage-binding statement unambiguous: Stage 0 spec; Stage 1 runtime | [x] | `section-1.md` §1.8 |
| 7 | Data structures defined with field-level typing (5 structs + 2 views) | [x] | `section-2.md` §2.2 (§2.2.1–§2.2.7) |
| 8 | Failure modes F1–F5 enumerated with detection, recovery, and test reference | [x] | `section-2.md` §2.4 |
| 9 | All formulas include units, valid input ranges, and at least one worked example | [x] | `section-3.md` §3.4–§3.11 (7 algorithms with worked examples each) |
| 10 | Per-tick pseudocode present (§3.13 — 9-step main loop) | [x] | `section-3.md` §3.13 |
| 11 | Test count target ≥ 85 with category breakdown | [x] | `section-5.md` §5.1 (≥ 85 total; §5.2 59 unit+integration named tests) |
| 12 | FR-to-test traceability matrix (all 37 FRs) | [x] | `section-5.md` §5.8 |
| 13 | Performance budget declared (≤ 0.12 ms on named reference host) | [x] | `section-6.md` §6.3 |
| 14 | Memory footprint declared (< 2 KB working set) | [x] | `section-6.md` §6.5 |
| 15 | Hot-path enumeration table | [x] | `section-6.md` §6.2 |
| 16 | Boundary matrix (13-row table) | [x] | `section-1.md` §1.6 |
| 17 | File layout declared with module responsibilities | [x] | `section-4.md` §4.2 |
| 18 | Internal module contracts table (7 modules) | [x] | `section-4.md` §4.3 |
| 19 | Upstream integration contracts declared (§4.4.1–§4.4.4) | [x] | `section-4.md` §4.4 |
| 20 | Downstream integration contracts declared (§4.5.1–§4.5.4) | [x] | `section-4.md` §4.5 |
| 21 | Determinism and safety boundaries declared (§4.6) | [x] | `section-4.md` §4.6 |
| 22 | Cross-spec validation checks enumerated (10 checks, §4.7) | [x] | `section-4.md` §4.7 |
| 23 | Future extensions catalogued (§7 — 12 items + permanent exclusions) | [x] | `section-7.md` §7.1–§7.12 |
| 24 | All cross-spec references allocated with XC-014-NNN IDs (29 references) | [x] | `section-8.md` §8.1 |
| 25 | ERR-014-001..004 declared with target spec, section, and status | [x] | `section-8.md` §8.3 |
| 26 | CLAUDE.md invariants 1–10 explicitly bound | [x] | `section-8.md` §8.2 |
| 27 | Stale-reference grep record with all-PASS results | [x] | `section-8.md` §8.5 |
| 28 | xG-surrogate metrics defined for Stage 0 validation | [x] | `section-5.md` §5.7 |
| 29 | Exploit-resistance corpus (KD-18): four canonical exploits T-DA-EXP-001..004 | [x] | `section-5.md` §5.6.2; `appendices.md` Appendix E |
| 30 | Determinism regression tests T-DA-DET-001..006 declared | [x] | `section-5.md` §5.4 |
| 31 | Anti-chaos invariant cascade tests T-DA-INV-001..003 declared | [x] | `section-5.md` §5.6.1 |
| 32 | Glossary of terms (16-entry table) | [x] | `appendices.md` Appendix F |
| 33 | Last-man predicate reference card with 3 canonical test cases | [x] | `appendices.md` Appendix B |
| 34 | Offside trap algorithm verification (4 canonical trigger cases) | [x] | `appendices.md` Appendix C |
| 35 | Anti-chaos sensitivity analysis | [x] | `appendices.md` Appendix D |
| 36 | No academic references without DOI verification (zero cited at Stage 0) | [x] | `section-8.md` §8.4 |
| 37 | No interfaces against #15 at Stage 0 (grep clean per §4.7 check 6) | [x] | `section-8.md` §8.5 |
| 38 | No offside adjudication in §3 (grep clean per §4.7 check 5) | [x] | `section-8.md` §8.5 |
| 39 | Fatigue convention 0.0 = rested, 1.0 = fatigued; no inversion found | [x] | `section-8.md` §8.5 |

---

## 9.2 Cross-Spec Sign-Offs Required

Items in this table require confirmation from counterpart spec authors or
lead-developer ratification before `IN REVIEW → APPROVED` advancement.

| Item | Target spec | Required action | Status |
|---|---|---|---|
| ERR-014-001: `TacticalContext.MarkDirective?` field | Decision Tree #8 §2.2.6 | Lead-developer ratification + #8 section-file patch | RESOLVED — `MarkDirective?` field added to #8 §2.2.6 v1.1.3 (May 18, 2026); `Stage0Default` initializes to `null`; ownership table row added |
| ERR-014-004: `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` | Deterministic Simulation #16 §3.4 | #16 §3.4 v1.0.5 patch; `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promotion in #14 §6.1 and §4.6 | RESOLVED — allocated in #16 §3.4 v1.0.5 (May 18, 2026); #14 §6.1 and §4.6 promoted atomically |
| `BaselineDefensiveShape` + `GetLine` accessor surface confirmed at v0.3 | Positioning AI #12 §4.5.1–§4.5.2 | Confirmed per ERR-013-007/008 resolution (May 17, 2026) | CONFIRMED — #12 v0.3 patch |
| `HOLD_SHAPE` handoff protocol acknowledged | Pressing AI #13 §7.4 | APPROVED May 17, 2026; §7.4 reserves #14 handoff slot | CONFIRMED — #13 APPROVED |
| #11 FR-GK-016 defensive wall ownership confirmed | Goalkeeper Mechanics #11 §1.4 | Present in #11 v0.2 IN REVIEW; confirms wall is #14's scope | CONFIRMED — present in #11 v0.2 |
| ERR-014-002 / ERR-014-003 channel registration | Event System #17 §3.10 | Deferred to Stage 1 first `src/DefensiveAI/` commit per KD-15; not a sign-off gate for `APPROVED` (pattern per #13 §7.3 precedent) | DEFERRED (not blocking APPROVED) |

---

## 9.3 KD-Sequencing Preconditions (Gates `IN REVIEW → APPROVED`)

| # | Precondition | Status | Notes |
|---|---|---|---|
| (a) | ERR-014-001 mechanism choice (#8 `TacticalContext.MarkDirective?` field) ratified by lead developer | [x] DONE — `MarkDirective?` field added to #8 §2.2.6 v1.1.3 (May 18, 2026) |
| (b) | ERR-014-004 domain-tag allocation `0x1A` ratified via #16 §3.4 patch | [x] DONE — allocated in #16 §3.4 v1.0.5 (May 18, 2026); `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted |
| (c) | All `[CROSS-PENDING]` tags documented with ERR-014-NNN IDs | [x] DONE | §6.1 `DOMAIN_TAG_DEFENSIVE_AI` row; §4.6; XC-014-022 in §8.1; §8.3 ERR-014-004 entry |
| (d) | All `[EST]` constants promoted to `[GT]` with Appendix A derivations | [x] DONE | §6.1 catalogue: all 22 `[GT]` constants have derivation entries in Appendix A |
| (e) | #3 tackle-physics subsection (`section-3-3.md`) grep-verified against actual collision-system section files | [x] DONE — confirmed: `docs/specs/collision-system/section-3-3.md` exists and describes agent-agent collision response; Q2 resolution confirmed at lead-developer review |
| (f) | #12 `BaselineDefensiveShape` and `GetLine` accessor confirmed at section-file draft | [x] DONE | Confirmed per ERR-013-007/008 resolution; §4.4.2 accessor declarations present |
| (g) | Lead-developer R-01..R-05 review pass | [x] DONE — signed May 18, 2026 |

**Advancement rule:** preconditions (a), (b), (e), and (g) are the remaining
open gates. Preconditions (c), (d), and (f) are already satisfied by the v0.1
section files. ERR-014-002 / ERR-014-003 channel rows are deferred (Stage 1
first commit) and do not gate `APPROVED` per KD-15 and #13 §7.3 precedent.

---

## 9.4 Outline.md Finding-to-Resolution Map

All 14 adversarial-review findings from the `outline-detailed.md` v1.0
review pass are resolved by the v0.1 section files:

| # | Finding summary | Severity | Resolution | Section file evidence |
|---|---|---|---|---|
| 1 | Missing metadata header (creation date, version, status, source) | H | Metadata block added to all section files | All section files §1 metadata block |
| 2 | Section plan misaligned with CLAUDE.md §1–§9 template structure | H | Sections §1–§9 + Appendices authored per CLAUDE.md template; Stage-Binding Clarification explicit in §1.8 | `section-1.md` §1.8; all section file headers |
| 3 | Tackle ownership ambiguity (who owns tackle contact vs. intent) | H | KD-6 resolved: #14 produces `TackleIntentRequest` (intent); #8 mediates; #3 owns contact physics. No type enum in physics layer. | `section-1.md` §1.5 KD-6; `section-3.md` §3.6; `section-4.md` §4.5.2 |
| 4 | Offside-line ownership undefined (step-up vs. adjudication) | H | KD-9 resolved: step-up decision = #14; adjudication = future referee spec. FR-DA-020 normative. §4.7 check 5 grep-validates no adjudication text. | `section-1.md` §1.2.2; `section-3.md` §3.7.1; `section-8.md` §8.5 |
| 5 | Boundary with #13 and #12 unstated | H | KD-3 (#12 boundary) + KD-4 (#13 disjoint partition) declared; §1.6 boundary matrix (13 rows) | `section-1.md` §1.5, §1.6 |
| 6 | Last-man computation undefined (algorithm, tie-break, GK exclusion) | H | KD-12 resolved: formal `IsLastManCandidate` + `IsLastManThreat` with `distToOwnGoal` normalisation; GK excluded; EntityId ascending tie-break. Appendix B reference card. | `section-3.md` §3.8; `appendices.md` Appendix B |
| 7 | Determinism plan absent | M | KD-10 + EntityId-ascending iteration (FR-DA-003) + digest scope declaration (§4.6) + T-DA-DET-001..006 tests | `section-4.md` §4.6; `section-5.md` §5.4 |
| 8 | Coordinate convention unmentioned | M | KD-1 cite-not-redefine + §1.7 full coordinate binding (X, Y, Z, origin) citing #1 §1.2 (XC-014-001) | `section-1.md` §1.7; `section-8.md` §8.2 item 1 |
| 9 | Tick-rate split (10 Hz / 60 Hz) unstated | M | KD-2 + §1.7 explicit binding + §4.1 architecture statement + FR-DA-001 + §6.4 per-frame budget = N/A | `section-1.md` §1.7; `section-3.md` §3.12; `section-4.md` §4.1 |
| 10 | Fatigue convention not pre-committed | M | KD-1 + FR-DA-008 + §1.7 explicit binding + §4.7 check 3 | `section-1.md` §1.7; `section-2.md` FR-DA-008 |
| 11 | No event production declared | M | KD-15: `MARK_ASSIGNED` / `LINE_STEPPED` channels declared at Stage 1 via ERR-014-002/003; §7.3 | `section-7.md` §7.3; `section-8.md` §8.3 |
| 12 | Constant-tag policy not invoked | M | KD-13 + §6.1 full 26-entry catalogue with tags + Appendix A derivations | `section-6.md` §6.1; `appendices.md` Appendix A |
| 13 | Chance-prevention lacks acceptance criteria | L | §5.7: Stage-0 xG surrogate (shots-in-box + avg shot distance); measurement hooks declared against #3 events | `section-5.md` §5.7 |
| 14 | No GK interaction model | L | KD-7: defensive wall ownership (#11 FR-GK-016 confirmation); `COVER_GK_ZONE` protocol (§3.9); GK exclusion from pool (FR-DA-009) | `section-1.md` §1.5 KD-7; `section-3.md` §3.9; `section-2.md` FR-DA-009 |

---

## 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

All five sign-offs required for `IN REVIEW → APPROVED`. The sign-off covers
the complete section-file set at v0.1 (all files in `docs/specs/defensive-ai/`
dated May 17, 2026).

```
R-01  Specification completeness and accuracy:                Lead Developer  Date: May 18, 2026

R-02  Cross-spec boundary correctness
      (KD-3 #12 boundary / KD-4 #13 partition / KD-5 #8 coupling /
       KD-6 #3 tackle surface / KD-7 #11 GK boundary):       Lead Developer  Date: May 18, 2026

R-03  Algorithm correctness
      (§3.1 phase gate / §3.3–§3.5 assignment / §3.6 tackle intent /
       §3.7 offside trap / §3.8–§3.9 last-man + GK cover /
       §3.10 anti-chaos / §3.11 hysteresis / §3.13 pseudocode): Lead Developer  Date: May 18, 2026

R-04  Test plan adequacy
      (§5.1 counts / §5.2–§5.3 named tests / §5.4 determinism /
       §5.5 performance / §5.6 exploit-resistance / §5.7 xG surrogate /
       §5.8 FR traceability):                                Lead Developer  Date: May 18, 2026

R-05  Performance budget validity
      (§6.3 ≤ 0.12 ms budget against named reference host;
       §6.5 < 2 KB memory footprint; §6.2 hot-path enumeration): Lead Developer  Date: May 18, 2026
```

---

## 9.6 Post-Approval Follow-Ups (Non-Blocking)

Items below do not gate `APPROVED` but must be completed at Stage 1:

1. **ERR-014-002 / ERR-014-003 channel rows:** Register `MARK_ASSIGNED` and
   `LINE_STEPPED` in #17 §3.10 at the first `src/DefensiveAI/` commit. This
   is the same deferred-channel pattern as Pressing AI #13 §7.3 (APPROVED
   May 17, 2026 with channels deferred to Stage 1).

2. **Collision System #3 §3.3 grep confirmation:** Verify that `section-3-3.md`
   exists in `docs/specs/collision-system/` and that it describes the agent-agent
   collision response surface that #14 references via `TackleIntentRequest`
   dispatch (Q2 resolution confirmation). If the section structure differs
   from `§3.3`, update XC-014-004 with the correct section reference.

3. **#15 Attacking AI interface declaration:** Once #15 Attacking AI reaches
   `IN REVIEW`, confirm whether `MarkDirective.emergencyFlag` exposure requires
   an explicit accessor in the orchestrator (per §7.4). If yes, file an
   ERR-014-005 amendment to #14 §4.5.3.

4. **Comprehensive audit:** A full programmatic audit at Pass Mechanics #5
   draft-level rigor parity is recommended before Stage 1 implementation
   begins. This is the same recommendation applied to Decision Tree #8 (OPEN
   ISSUES entry dated April 27, 2026).

---

## 9.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial checklist. §9.1: 39 self-contained items — all [x] checked against actual v0.1 section files (no fabricated values). §9.2: 6 cross-spec sign-offs; 2 OPEN, 3 CONFIRMED, 1 DEFERRED. §9.3: 7 preconditions; 3 [x] done, 4 [ ] open (a, b, e, g). §9.4: all 14 outline.md findings mapped to resolutions. §9.5: R-01..R-05 sign-off lines. §9.6: 4 non-blocking post-approval follow-ups. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. M1: §9.1 item 4 evidence corrected "27-entry" → "26-entry catalogue: 22 [GT] + 4 [CROSS]/[CROSS-PENDING]"; §9.4 item 12 corrected "27-entry" → "26-entry". L2: §9.1 item 32 evidence corrected "13-entry" → "16-entry" (Appendix F has 16 entries per actual file count). |
| 0.3 | May 18, 2026 | AI agent (claude/review-phase-0-requirements-yMzh6) | APPROVED. §9.2: ERR-014-001 RESOLVED (`MarkDirective?` in #8 §2.2.6 v1.1.3); ERR-014-004 RESOLVED (#16 §3.4 v1.0.5 allocates `0x1A`). §9.3: preconditions (a)(b)(e)(g) all DONE. R-01..R-05 signed May 18, 2026. |
