# Staff & Backroom #34 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file AR PASS-1 (2M) → PASS-2 (1M regression) → CONVERGENCE; R-01..R-05 signed; APPROVED; prior v0.1 IN REVIEW)
**Version:** 0.2
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` projection/wage magnitudes are illustrative pending a Stage-3 balance pass (shapes/directions are the reviewed contract) | ✅ | §3.1, Appendix A note (#21 G2 precedent) |
| G3 | Determinism: the scaffold is **draw-free**; `_RESERVED_0x26_` / 88 stays reserved (no #16 change at approval) | ✅ | §1 KD-4, §8.2, #16 §3.4:270 |
| G4 | KD-3: projections return each consumer's **own** identity type (`MedicalModifier`/`CoachingModifier`/`staffMult`/`MentoringPlan`); neutral ⇒ exact `Identity`; #34 invents no multiplier convention and is the sole staff path | ✅ | §3.1, FR-ST-001/002/015 |
| G5 | KD-1: hiring reuses `NegotiationOutcome` + the atomic-commit pattern, but a thin staff `StaffOffer`/`EvaluateStaffOffer` on **wage** (not fee); year-round (no window) | ✅ | §3.4, FR-ST-018 |
| G6 | KD-6 #40 boundary: scaffold posts no `StaffWage` (FR-FN-015 preserved verbatim, no back-prop at approval); the deep gate reads #40's `WageBillAggregate + wage ≤ WageBudget` — **no #34 wage counter** | ✅ | §3.5, FR-ST-016/017, F1 |
| G7 | Atomic hire: all gates validated before any mutation; a failed gate leaves finances **and** `StaffState` untouched; a hire **replaces** the always-filled role-slot occupant | ✅ | §3.4, FR-ST-018, F2 |
| G8 | KD-4: one `STAFF_SAVE_FORMAT_VERSION` season-save sub-blob; **no** `WORLD_STORE_FORMAT_VERSION` bump; codec fail-loud posture mirrored | ✅ | §4.4, Appendix B, FR-ST-010 |
| G9 | KD-2: `StaffAttributes` is a distinct staff-skill vocabulary (not #27's `PlayerAttributes`); per-club **role slots** 1:1 with `StaffRole`; stable monotonic `StaffId` (`NextStaffId`) | ✅ | §2.2, FR-ST-003/006/007, §3.1 |
| G10 | KD-7: a hire changes `EmployerClubId` only; `StaffId` never re-keys; **no #30 roster-commit, no cross-system migration hook** | ✅ | §3.4, FR-ST-008, KD-7 |
| G11 | KD-5: the neutral baseline is a **real** `NeutralHouseStaff` entity projecting `Identity`, not an absence sentinel; `default(StaffRecord)` (all-zero attrs) fails loud (F4) | ✅ | §3.2, FR-ST-004/005, F4 |
| G12 | KD-8 behaviour-neutral: a neutral-baseline-staff season is byte-identical to pre-#34; no stream registered; the composition root swaps a hardcoded `Identity` for a projection that equals `Identity` | ✅ | §5.3, FR-ST-014, T-ST-NEU-001 |
| G13 | Managed-club scope: `StaffState` tracks the managed club only at the scaffold; AI clubs unstaffed (project `Identity`); all-clubs is deep | ✅ | §2.2, FR-ST-011, T-ST-NEU-002 |
| G14 | Genesis-vs-load: seeding runs **only at new-career genesis**; a load decodes from the sub-blob and never re-seeds | ✅ | §3.3, §4.5, FR-ST-012, T-ST-DET-001 |
| G15 | Integer posture: no float in #34; serialized block has no `RngCursor` (draw-free) | ✅ | §1.5, FR-ST-022, T-ST-INT-001/SHAPE-001 |
| G16 | FR-ST-001..024 each traceable to a T-ST-* test **or** a recorded §7 deferral | ✅ | §5.7 |
| G17 | FR prefix FR-ST unclaimed across `docs/specs/**`; XC-034-* allocated; the #41 §7 / #29 §7 / #40 §7 / #33 §7.3 / #31 FR-TX-011 consumer/producer sides named | ✅ | grep-verified; §8.1 |

## 9.2 Post-APPROVED follow-ups (non-blocking)

- **G2 balance pass** — the §3.1/Appendix A projection/wage `[GT]` magnitudes are illustrative; a
  numerical-mirror + balance review pins them at Stage-3 (the #21 G2 / #40 / #41 / #31 precedent).
- **T-phase back-props** — land with the code, not at approval: the #30 outer `SEASON_SAVE_FORMAT_VERSION`
  bump (T1); the #29 `CoachingModifier` field shape + consumption (ERR-029-002, T3); the #40 FR-FN-015 relax +
  `WageBudget` gate for the deep `StaffWage` producer (shared ERR-040, T3); the #16 `DOMAIN_TAG_STAFF = 0x26`
  promotion (ERR-016, T3 first draw).

## 9.3 Approval-time cross-spec back-props

**One:** **ERR-030-006** — #30 §3.3 `RunWorldTickInFixedOrder` gains the staff tick-order null-seam slot (the
ERR-030-002 #41 / ERR-030-004 #31 precedent — an insertion, since FR-SN-034 enumerated #28/#29/#33/#41/#31
only, not #34; the slot is a **deep-tier position reservation**, empty until #34 T2/T3; `AdvanceDay` → step
7). `0x26`/88 stays reserved (draw-free — no #16 change); #41/#29/#40/#33/#31/#27 unchanged (their existing
seams already name #34 the producer/consumer). **No roster-commit back-prop** (KD-7 — staff never re-key).
**Filed atomically at approval** (`spec-error-log.md` v1.37; `season-competition-loop/section-2.md` v0.6 +
`section-3.md` v0.6).

## 9.4 Sign-off

| Role | Decision | Date |
|---|---|---|
| R-01 Lead developer | ✅ APPROVED | Jul 23, 2026 |
| R-02 Determinism owner | ✅ APPROVED (draw-free scaffold; `0x26`/88 stays reserved) | Jul 23, 2026 |
| R-03 Save-format owner | ✅ APPROVED (`STAFF_SAVE_FORMAT_VERSION` sub-blob; no `WORLD_STORE` bump) | Jul 23, 2026 |
| R-04 Finances (#40) owner | ✅ APPROVED (scaffold posts no `StaffWage`, so FR-FN-015 preserved — no back-prop at approval; the deep wage gate reads #40's `WageBillAggregate`, no #34 counter) | Jul 23, 2026 |
| R-05 Season-loop (#30) owner | ✅ APPROVED (staff tick-order slot ERR-030-006; **no** roster-commit — staff never re-key) | Jul 23, 2026 |

## 9.5 Open gates before APPROVED — CLEARED

- Section-file adversarial review: PASS-1 (2M — the 4-slot/`StaffRole`-3 mismatch; the `HireStaff`
  free-slot gate vs the always-filled model) → PASS-2 (1M regression — a stale "full role slot" test) →
  **CONVERGENCE**.
- R-01..R-05 lead-developer sign-off — **granted July 23, 2026**.
- ERR-030-006 (the #30 staff tick-order null seam) — **filed atomically at approval**.
- G1..G17 evidence verification — complete.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial approval checklist (G1..G17, sign-off pending), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file AR PASS-1 (2M) → PASS-2 (1M regression) → CONVERGENCE; G1..G17 ✅; R-01..R-05 signed; ERR-030-006 filed; Status APPROVED. |
#endregion
