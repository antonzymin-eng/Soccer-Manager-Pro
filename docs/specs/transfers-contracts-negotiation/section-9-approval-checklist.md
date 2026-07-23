# Transfers, Contracts & Negotiation #31 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.5 — AR-8 (3L doc, non-gating); prior v0.4 AR-6 + AR-7 convergence, v0.3 AR-3/AR-4, v0.2 AR-1/AR-2 + sign-off, v0.1 initial)
**Version:** 0.5
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` valuation/window magnitudes are illustrative pending a Stage-2/3 balance pass (shapes/directions are the reviewed contract) | ✅ | §3.1, Appendix A note (#21 G2 precedent) |
| G3 | Determinism: minimal is **draw-free**; `_RESERVED_0x23_` / 85 stays reserved (no #16 change at approval) | ✅ | §1 KD-5, §8.2, #16 §3.4:267 |
| G4 | KD-1: the minimal valuation is a pure integer function of #27 attributes + age — **no** club-need, **no** #33 read, **no** #28 CA read; club-need/personality/CA are all deep-tier multiplicative bias, identity = `×1000‰` | ✅ | §3.1, FR-TX-001/002/003 |
| G5 | KD-2 #40 boundary: read `AvailableTransferBudget`, commit via `ApplyTransaction`, #31 owns `committedSpendThisWindow` (FR-FN-004), no parallel ledger, no direct field writes | ✅ | §3.3, FR-TX-004..008 |
| G6 | Atomic commit: all gates validated before any mutation; a failed gate leaves finances **and** roster untouched (no half-written deal); the sell removes its `Contract` **before** the infallible re-key (no orphan) | ✅ | §3.3, FR-TX-009/023, F2 |
| G7 | KD-3: the offer/response seam is counterparty-generic; #31 builds no #32/#34 interface; #34 influence is a `×1000‰` identity seam | ✅ | §4.3, FR-TX-010/011 |
| G8 | KD-4: one `TRANSFERS_SAVE_FORMAT_VERSION` season-save sub-blob (durable contracts + season-scoped state); **no** `WORLD_STORE_FORMAT_VERSION` bump; codec fail-loud posture mirrored | ✅ | §4.4, Appendix B, FR-TX-012..014 |
| G9 | KD-6: the transfer-window model is #31-owned, derived read-only from #30's `SeasonCalendar`; #30 has none | ✅ | §3.5, FR-TX-019/020 |
| G10 | KD-7: a transfer re-keys the club-scoped `PlayerId` through a NEW #30 mid-season entry point + roster-move hook; #31 migrates only its own `Contract`; #28/#33 migrate their own | ✅ | §3.4, FR-TX-021..023 |
| G11 | KD-8 behaviour-neutral: zero manager action ⇒ zero transfers ⇒ byte-identical season; a bid is an explicit command; no stream registered | ✅ | §3.6, FR-TX-024/025, T-TX-NEU-001 |
| G12 | Integer posture: no float in #31 (only integer `long` exchanged with #40); serialized block has no `RngCursor` (draw-free) | ✅ | §1.5, FR-TX-018, T-TX-INT-001/SHAPE-001 |
| G13 | Zero-value-trap hygiene: `default(Contract)` (`LengthSeasons = 0`) fails loud at insertion validation (F7); boundary aging **removes** a contract that would decrement to 0 (never stores 0), so F7 never collides with a legitimately-aged contract | ✅ | §2.3, §3.7, F7, T-TX-FAIL-003, T-TX-DET-002 |
| G14 | Roster lifecycle in lockstep with #28/#30 (genesis-only seeding of the managed squad — a load decodes from the sub-blob, never re-seeds; regen-insert / retire-remove; transfer re-key; boundary decrement-and-remove aging) — every managed player has a contract the sell/aging flows operate on | ✅ | FR-TX-028, §3.7, §3.8, §4.5, T-TX-DET-001/002, T-TX-INIT-001 |
| G15 | FR-TX-001..028 each traceable to a T-TX-* test **or** a recorded §7 deferral | ✅ | §5.7 |
| G16 | FR prefix FR-TX unclaimed across `docs/specs/**`; XC-031-* allocated; the #40 §7.3 / #33 §7.3 consumer sides named | ✅ | grep-verified; §8.1 |
| G17 | Wage deferral: minimal is **fee-only** (no `PlayerWage` post), so #40 FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2) is preserved with **no #40 back-prop at approval**; the wage producer + `WageBudget` gate are deep (ERR-040, T3) | ✅ | FR-TX-005, §7.3, §8.3, T-TX-BID-006 |

## 9.2 Post-APPROVED follow-ups (non-blocking)

- **G2 balance pass** — the §3.1/Appendix A valuation/window `[GT]` magnitudes are illustrative; a
  numerical-mirror + balance review pins them at Stage-2/3 (the #21 G2 / #40 / #41 / #33 precedent).
- **T-phase back-props** — land with the code, not at approval: the #30 outer `SEASON_SAVE_FORMAT_VERSION`
  bump + the mid-season `RequestRosterCommit` seam (ERR-030-005, T1/T2); the #16 `DOMAIN_TAG_TRANSFERS = 0x23`
  promotion (ERR-016, T3 first draw); the #40 FR-FN-015 relax + `WageBudget` gate for the deep wage producer
  (ERR-040, T3); the #33 `PersonalityProfile` trait read surface for the deep `personalityMult` (ERR-033, T3).

## 9.3 Approval-time cross-spec back-props

**One:** **ERR-030-004** — #30 §3.3 `RunWorldTickInFixedOrder` gains the transfers tick-order null-seam slot
(the ERR-030-002 #41 precedent — an insertion, since FR-SN-034 enumerates #28/#29/#33/#41 only, not #31; the
slot is a **deep-tier position reservation**, empty until #31 T2/T3). `0x23`/85 stays reserved (draw-free —
no #16 change); #40/#33/#27 unchanged (their existing seams already name #31 the consumer). Cleaner than #40
(ERR-040-001 + ERR-030-003) and #41 (ERR-041-001 + ERR-030-002) — #31 is draw-free, so no #16 promotion.

## 9.4 Sign-off

| Role | Decision | Date |
|---|---|---|
| R-01 Lead developer | ✅ APPROVED | Jul 23, 2026 |
| R-02 Determinism owner | ✅ APPROVED (draw-free minimal; `0x23`/85 stays reserved) | Jul 23, 2026 |
| R-03 Save-format owner | ✅ APPROVED (`TRANSFERS_SAVE_FORMAT_VERSION` sub-blob; no `WORLD_STORE` bump) | Jul 23, 2026 |
| R-04 Finances (#40) owner | ✅ APPROVED (`AvailableTransferBudget` read + `ApplyTransaction` commit; no parallel ledger; **AR-3: minimal is fee-only, so FR-FN-015 `WageBillAggregate ≡ 0` is preserved — no back-prop at approval**) | Jul 23, 2026 |
| R-05 Season-loop (#30) owner | ✅ APPROVED (transfers tick-order slot ERR-030-004 + mid-season roster-commit seam) | Jul 23, 2026 |

## 9.5 Open gates before APPROVED — CLEARED

- Section-file AR-1 (3M+1L) → AR-2 (1L) → AR-3 (1H+3M+5L) → AR-4 (1M + 1 regression + 3L) → **AR-5 (declared
  clean — premature)** → AR-6 (2M+1L, reopened the AR-4 regressions: a stale re-key test still asserting the
  AR-3-removed hook-move, + the AR-4 seeding fix left the load lifecycle undefined; all resolved) → AR-7 →
  **converged** → AR-8 (0H+0M+3L doc, non-gating — applied).
- R-01..R-05 sign-off — **granted July 23, 2026** (re-affirmed after the AR-3 fixes).
- ERR-030-004 (the #30 transfers tick-order step-5 null seam) — **filed atomically at approval**
  (`spec-error-log.md` v1.36; `season-competition-loop/section-2.md` + `section-3.md` v0.5).
- AR-3 (H) resolution: wage posting deferred to the deep tier (minimal fee-only), so #40 FR-FN-015 is
  preserved and **no #40 back-prop is needed at approval** (ERR-040 recorded as a T3 deferral, §8.3/§9.2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial approval checklist. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (3M+1L) → AR-2 (1L) converged; G-items ✅; R-01..R-05 signed; Status APPROVED; ERR-030-004 filed. |
| 0.3 | 2026-07-23 | — | AR-3 (1H+3M+5L) fix pass (H: wage deferral; M: sell double-handle, club-need, aging-vs-F7; L×5) all resolved; AR-4 fix pass (1M: career-start contract seeding §3.8; regression: `counterpartyView` double-application; L: §6 post-count, §7 T1/T2 cites, §1.2 accessor) all resolved → AR-5 convergence; new G17 (wage deferral), G14 seeding; ERR-040/ERR-033 recorded as T3 deferrals; sign-off re-affirmed. |
| 0.4 | 2026-07-23 | — | AR-6 (2M+1L) fix pass: T-TX-REKEY-001 corrected to the insert/remove-via-`SubmitBid` model (was still asserting the AR-3-removed hook-move); §3.8/§4.5/FR-TX-028 scope seeding to new-career genesis only (a load decodes from the sub-blob, never re-seeds) + T-TX-DET-001 lock; T-TX-REKEY-003 wording (L) → AR-7 convergence. |
| 0.5 | 2026-07-23 | — | AR-8 (0H+0M+3L, non-gating doc): §9.5 chain corrected to record AR-5 as a premature false-clean (was skipped AR-4→AR-6); §3.3 note that sell income does not raise in-window buy headroom (static ceiling, `committedSpendThisWindow` is buy-side only); §3.7 `ResetWindow()` re-derives from the calendar. Loop remains converged (Lows do not gate). |
#endregion
