# Discipline & Suspensions #44 — Section 9: Approval Checklist

**Created:** July 24, 2026
**Last Updated:** August 15, 2026 (v0.4 — M25, the spec half of #44's adversarial-review round 4
(`open-issues.md`): §5 (Test Plan) was found stale — it still mandated a withdrawn fail-loud
(T-DC-BAN-004/F5) and had no row for F6, FR-DC-009's `null`-return case, or F2's negative-`PlayerId`
refusal, which is exactly what **G14**'s ✅ ("FR-DC-001..022 each traceable to a T-DC-* test") claims
does not happen. Re-checked here after `section-5.md` v0.4 corrected that table: G14's evidence
citation is bare "§5.6", not individual test IDs, and §5.6 now accounts for F5/F6/FR-DC-009/FR-DC-011
explicitly — **G14 is left ✅ unchanged**, since the claim was true of the corrected table; it was the
table G14 pointed at that was stale, not the checklist row itself. No other gate re-verified in this
pass.)
**Last Updated (prior):** August 13, 2026 (v0.3 — L6, adversarial review over the C1/C2 landing: §9.3's
ERR-030-009 back-prop entry annotated LIVE since T2, so it reads as the historical approval-time
record it is)
**Last Updated (prior):** July 24, 2026 (v0.2 — section-file AR PASS-1 (1M) → PASS-2 (2L) → CONVERGENCE; R-01..R-05 signed; APPROVED; prior v0.1 IN REVIEW)
**Version:** 0.4
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` threshold/ban magnitudes are illustrative pending a balance pass (shapes are the reviewed contract) | ✅ | Appendix A note (#21 G2 precedent) |
| G3 | Determinism: **no RNG stream / tag / ordinal** (the #37/#49 read-only class — a positive property; no #16 row); any quick-sim synthesis is #30-owned | ✅ | §8.2, FR-DC-019 |
| G4 | KD-2: the read is the #37-class per-tick tap + occupancy fold; ledger bytes, post-match slot state (the v1.33 reset), and new subscription patterns all ruled out by verified source | ✅ | §1 KD-2, §3.1, XC-044-001/002 |
| G5 | KD-5: de-dup = the verified single-event kind-2 emission contract (`ApplyCardAndCheckSentOff`); no yellow-then-red pair exists | ✅ | FR-DC-006, §3.1, T-DC-FOLD-001 |
| G6 | KD-4: availability is a VIEW — pure predicate + reduced value-copy squad; #27 never written (byte-identity-locked) | ✅ | FR-DC-001/008/009, T-DC-VIEW-001 |
| G7 | KD-3: fold at resolution, filter at next selection (ERR-030-009), serving per club fixture on both paths — the off-by-one lock | ✅ | FR-DC-010/011, §3.3, T-DC-BAN-002/003 |
| G8 | KD-1: the tally persists (`DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob — recompute impossible, no ledgers retained); no `WORLD_STORE` bump; fail-loud codec | ✅ | FR-DC-014/015, §4.4, Appendix B |
| G9 | KD-6: `(PlayerId, CompetitionId)` partition key from day one; hygiene = **migrate** on re-key (bans follow the player — the recorded #32 contrast), drop on retirement | ✅ | FR-DC-012/013, T-DC-HYG-001 |
| G10 | KD-7 live-at-minimal (the #41 class): observer-neutrality digest-locked; no-trigger identity (except the sub-blob); determinism | ✅ | FR-DC-003/018/021, §5.1 |
| G11 | KD-8 boundary: yellows reset, unserved bans carry, `(0,0)` entries dropped | ✅ | FR-DC-017, T-DC-SAV-002 |
| G12 | Minimal coverage asymmetry (card-free quick-sim) stated honestly; evened by the deferred #30-owned synthesis | ✅ | §1.1, §7.2 |
| G13 | Integer posture; no float; no RNG-state field in the blob | ✅ | FR-DC-016/020, T-DC-INT-001 |
| G14 | FR-DC-001..022 each traceable to a T-DC-* test **or** a recorded §7 deferral | ✅ | §5.6 |
| G15 | FR prefix FR-DC unclaimed across `docs/specs/**`; XC-044-* allocated; the #37/#30/#31/#28/#43-facing sides named | ✅ | grep-verified; §8.1 |

## 9.2 Post-APPROVED follow-ups (non-blocking)

- **G2 balance pass** — `YELLOW_ACCUMULATION_THRESHOLD` / the three ban lengths are illustrative;
  pinned at the balance pass against real-competition rules (the #21 G2 precedent).
- **T-phase back-props** — the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the hygiene-hook
  wiring as #31's FR-TX-022 build lands (T2); the #43 partition + #30 quick-sim synthesis
  coordinations (T3).

## 9.3 Approval-time cross-spec back-props

**One:** **ERR-030-009** — #30 FR-SN-013's managed-fixture flow gains the pre-declared
availability-filter null seam (resolve → *filter* → configure; a null seam until #44 T2 — the
ERR-030-002/004/006/007 pattern, flow-side). **No #16 change** (read-only — no tag/stream). **No
#37/#43/#27/#17 change.** **Filed atomically at approval** (`spec-error-log.md` v1.40;
`season-competition-loop/section-2.md` v0.8 + `section-3.md` v0.8). **LIVE since T2 (C1/C2, August
13, 2026)** — this entry records the approval-time back-prop verbatim; the seam is no longer null
(§7.3, §8.3).

## 9.4 Sign-off

| Role | Decision | Date |
|---|---|---|
| R-01 Lead developer | ✅ APPROVED | Jul 24, 2026 |
| R-02 Determinism owner | ✅ APPROVED (no RNG stream/tag/ordinal — the read-only positive property; pure fold in canonical publish order; the immediate `(0,0)`-drop canonical-representation rule) | Jul 24, 2026 |
| R-03 Save-format owner | ✅ APPROVED (`DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob, persist forced by verification; no `WORLD_STORE` bump; canonical ascending keys) | Jul 24, 2026 |
| R-04 Season-loop (#30) owner | ✅ APPROVED (ERR-030-009 flow-side null seam resolve→*filter*→configure; serving path-independent; the boundary yellows-reset/bans-carry rule) | Jul 24, 2026 |
| R-05 Data-layer (#27) owner | ✅ APPROVED (availability is a view — reduced value-copy squads; #27 never written, byte-identity-locked; migrate-on-re-key hygiene consistent with the club-scoped id formula) | Jul 24, 2026 |

## 9.5 Open gates before APPROVED — CLEARED

- Section-file adversarial review: PASS-1 (1M — the `(0,0)`-drop canonical-minimality rule was
  outside the FRs with immediate-vs-boundary ambiguity, a serialized-representation determinism
  hazard) → PASS-2 (2L — the club-derivation implementability note; the 9.3 citation
  completeness) → **CONVERGENCE**.
- R-01..R-05 sign-off — **granted July 24, 2026**.
- ERR-030-009 (the #30 availability-filter null seam) — **filed atomically at approval**.
- G1..G15 evidence verification — complete.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial approval checklist (G1..G15, sign-off pending), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (1M) → PASS-2 (2L) → CONVERGENCE; G1..G15 ✅; R-01..R-05 signed; ERR-030-009 filed (`spec-error-log.md` v1.40, `season-competition-loop` section-2/3 v0.8); Status APPROVED. |
| 0.3 | 2026-08-13 | — | **L6** (adversarial review over the C1/C2 landing): §9.3's ERR-030-009 entry annotated LIVE since T2 — the "null seam until #44 T2" clause is the frozen approval-time back-prop text, not a claim about today's state. |
| 0.4 | 2026-08-15 | — | **M25** (#44 adversarial-review round 4, `open-issues.md`): re-checked G14 against `section-5.md` v0.4's corrected test-plan table (that section had mandated a withdrawn fail-loud and lacked rows for F6/FR-DC-009/F2). G14 cites bare "§5.6", which now names all four gaps explicitly — left **✅** unchanged, since the checklist row itself was never wrong, only the table it pointed at. |
#endregion
