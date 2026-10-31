# Transfers, Contracts & Negotiation #31 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** October 21, 2026 (v0.12 — AR round 5, ERR-031-015: G5 states the unposted counterparty as interim option (b) and cites the renamed test; G15 admits a tested structural lock)
**Last Updated (prior):** October 20, 2026 (v0.11 — AR round 4, ERR-031-014: G5 evidence wording (option (b) is interim; owner decision open); G11 cites the renamed, narrowed RNG scan; G15 cites the §5.7 FR table)
**Last Updated (prior):** October 20, 2026 (v0.10 — AR round 3, ERR-031-013: G5/G11/G12/G15/G17 evidence re-verified against the real test names; counterparty-ledger scope note)
**Last Updated (prior):** October 20, 2026 (v0.9 — ERR-031-011: G9/G14/G15 restated and re-verified against source)
**Last Updated (prior):** October 20, 2026 (v0.8 — ERR-031-006: G6 evidence corrected — the sell removes its contract after the confirmed commit)
**Last Updated (prior):** October 19, 2026 (v0.7 — ERR-031-005: G10 restated)
**Last Updated (prior):** September 21, 2026 (v0.6 — ERR-031-001/002 T0 approval-evidence reconciliation)
**Last Updated (prior):** July 23, 2026 (v0.5 — AR-8 (3L doc, non-gating); prior v0.4 AR-6 + AR-7 convergence, v0.3 AR-3/AR-4, v0.2 AR-1/AR-2 + sign-off, v0.1 initial)
**Version:** 0.12
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` valuation/window magnitudes are illustrative pending a Stage-2/3 balance pass (shapes/directions are the reviewed contract) | ✅ | §3.1, Appendix A note (#21 G2 precedent) |
| G3 | Determinism: minimal is **draw-free**; `_RESERVED_0x23_` / 85 stays reserved (no #16 change at approval) | ✅ | §1 KD-5, §8.2, #16 §3.4:267 |
| G4 | KD-1: the **base** valuation is a pure integer function of #27 attributes + age; the minimal counterparty view then applies always-on #27 positional scarcity. **No** #33 read and **no** #28 CA/PA read at minimal; those and #34 staff remain deep refinements (`ERR-031-001`) | ✅ | §1 KD-1, §3.1, FR-TX-001/002/003 |
| G5 | KD-2 #40 boundary: read `AvailableTransferBudget`, commit via `ApplyTransaction`, #31 owns `committedSpendThisWindow` (FR-FN-004), no parallel ledger, no direct field writes. **Scope note (ERR-031-013):** the fee is posted to the MANAGED club's #40 entry only — the counterparty AI club's entry is left untouched in the interim (option (b); not a decision), so AI clubs' `FinanceView` / `SettleFinances` never see transfer cash and money enters or leaves the league on every transfer (§7.2: option (b) is the recorded interim behaviour; the (a)-vs-(b) choice is an open owner decision, open-issues `transfer-counterparty-ledger`); this gate does not claim league-wide ledger completeness | ✅ | §3.3, FR-TX-004..008; T-TX-BID-008 (`TransfersT0Tests.SubmitBid_PostsOnlyThroughTheLedger_AndTheTransfersAssemblyHoldsNoFinanceState`); `SeasonTransfersT2Tests.AnAcceptedTransfer_LeavesTheCounterpartyClubsLedgerUntouched_InTheInterimMinimalTier` |
| G6 | Atomic commit: all gates validated before any mutation; buy affordability is decided before checked staged finance validation (`ERR-031-002`); a failed gate leaves finances **and** roster untouched (no half-written deal); the sell removes its `Contract` only **after** the confirmed roster commit (`RequestRosterCommit` re-homes under the unchanged id, then `TryGetPlayer` must report the destination before any local write), so a commit that does not land leaves the managed contract and finance intact *(ERR-031-006: originally "removes its `Contract` **before** the infallible re-key (no orphan)" — the reverse of §3.3/FR-TX-023 and the shipped code)* | ✅ | §3.3, FR-TX-008/009/023, F1/F2; `TransferCommands.SubmitBid` (`RequestRosterCommit` → `RequireCommitLanded` → `RemoveContract`); T-TX-BID-002/006 (`SubmitBid_Sell_CommitsRosterBeforeRemovingContract_AndCreditsFee`, `SubmitBid_SellRosterCommitThatDoesNotLand_RetainsLocalContractAndFinance`) |
| G7 | KD-3: the offer/response seam is counterparty-generic; #31 builds no #32/#34 interface; #34 influence is a `×1000‰` identity seam | ✅ | §4.3, FR-TX-010/011 |
| G8 | KD-4: one `TRANSFERS_SAVE_FORMAT_VERSION` season-save sub-blob (durable contracts + season-scoped state); **no** `WORLD_STORE_FORMAT_VERSION` bump; codec fail-loud posture mirrored | ✅ | §4.4, Appendix B, FR-TX-012..014 |
| G9 | KD-6: the transfer-window model is #31-owned, derived read-only from #30's `SeasonCalendar` — open on the day #30 begins the season, close at the calendar's opening fixture day + `SUMMER_WINDOW_IN_SEASON_DAYS` − 1, independent of roll timing (ERR-031-011); #30 has none | ✅ | §3.5, FR-TX-019/020; `TransferWindow.SummerWindowFor` (the single derivation), called by `League.CreateLoop` (new-game day, `season.Calendar.DayOfRound(0)`) and `SeasonLoop.RollToNextSeason` step (d″) (roll day, `nextCalendar.DayOfRound(0)`); T-TX-WIN-001/002 (`TransfersT2Tests.SummerWindowFor_*`, `SeasonTransfersT2Tests.APromptRollAndALateRoll_CloseTheWindowOnTheSameDay`, `…TheCloseSitsTheSameDistanceAfterTheOpeningFixture_AtGenesisAndAtABoundary`) |
| G10 | KD-7: a transfer re-homes the player under his stable `PlayerId` through a NEW #30 mid-season entry point; no PlayerId-keyed state migrates (ERR-031-005 — originally a packed-id re-key) | ✅ | §3.4, FR-TX-021..023 |
| G11 | KD-8 behaviour-neutral: zero manager action ⇒ zero transfers ⇒ a season, its roll and the next season's play byte-identical to an unwired career everywhere outside the #31 block; a bid is an explicit command; no stream registered | ✅ | §3.6, FR-TX-024/025, T-TX-NEU-001 (`SeasonTransfersT2Tests.NoTransferAction_IsBehaviourNeutral` — played season → roll → one season-2 round, the whole career snapshot minus the #31 block; non-vacuity `OneAcceptedSale_MovesTheProgressionSnapshot_…`; `TransfersT0Tests.TheTransfersAssembly_RegistersNoRngStream_NoMethodCallsOrConstructsAnRngType` — FR-TX-016 / G3 only: no RNG type is reached; it does not claim general determinism) |
| G12 | Integer posture: no float in #31 (only integer `long` exchanged with #40); serialized block has no `RngCursor` (draw-free) | ✅ | §1.5, FR-TX-018, T-TX-INT-001 (`TransfersT0Tests.T0StateAndCommands_ContainNoFloatOrDoubleFields`); the block layout is pinned byte-for-byte by `TransfersT1Tests.Encode_PinsTheAppendixBLayout_ByteForByte` (T-TX-FAIL-001/DET-001), which has no cursor field. *(AR round 3: originally cited a non-existent "T-TX-SHAPE-001".)* |
| G13 | Zero-value-trap hygiene: `default(Contract)` (`LengthSeasons = 0`) fails loud at insertion validation (F7); boundary aging **removes** a contract that would decrement to 0 (never stores 0), so F7 never collides with a legitimately-aged contract | ✅ | §2.3, §3.7, F7, T-TX-FAIL-003, T-TX-DET-002 |
| G14 | Roster lifecycle in lockstep with #28/#30 (genesis-only seeding of the managed squad — a load decodes from the sub-blob, never re-seeds; regen-insert / retire-remove; transfer re-home (stable id, ERR-031-005); boundary decrement-and-remove aging): every managed player has a contract **at genesis**; expiry leaves the player un-contracted and still sellable (ERR-031-009); managed regens are contracted at the boundary, and a retiree's contract leaves with him *(ERR-031-011: originally "every managed player has a contract the sell/aging flows operate on" — false after the first expiry)* | ✅ | FR-TX-028, §3.7, §3.8, §4.5, T-TX-DET-001/002, T-TX-INIT-001; `League.CreateLoop` → `TransfersState.CreateAtGenesis`, `TransfersState.PrepareSeasonBoundary`, `TransferCommands.SubmitBid` (a sale needs no contract); `SeasonTransfersT2Tests.CreateLoop_WithAProgressionStore_SeedsOneGenesisContractPerManagedPlayer`, `TheRoll_AgesContracts_…` (regen contracted, retiree removed), `AfterEveryGenesisContractExpires_TheManagerCanStillSell`; `TransfersT2Tests.ThreeBoundaries_ExpireEveryGenesisContract_WithoutAThrow` |
| G15 | FR-TX-001..029 each traceable to a T-TX-* test, a recorded §7 deferral **or a tested structural lock** (T-TX-REKEY-003's allow-list, T-TX-BID-008's scans — AR round 5, ERR-031-015: §5.7's "structural" rows had no home in this criterion) *(ERR-031-011: originally FR-TX-001..028 — FR-TX-029 / F9 landed at T2 without joining this list)*. **Re-verified AR round 3 (ERR-031-013) against source:** every T-TX-* id in §5 now names a live test — T-TX-BID-008, T-TX-DET-003 and T-TX-REKEY-003 had none while this row read ✅, and landed with that pass. **AR round 4 (ERR-031-014):** the FR → test direction was a bare assertion — §5.7 now carries the explicit 29-row FR-TX-001..029 table (each row a T-TX id with live test names, a §7 deferral, or a named structural enforcement) | ✅ | §5.7 (the FR-TX-001..029 table); FR-TX-029 → T-TX-DEP-001 (`TransfersT0Tests.SubmitBid_AReleaseThatDepletesTheSellingSquad_ReturnsSquadDepleted_WithNoMutation`, `SeasonTransfersT2Tests.AFullDestination_IsSquadFull_AndADepletedSeller_IsSquadDepleted`); FR-TX-027 → T-TX-DET-001/003 (`SeasonTransfersT2Tests.TwoRunsOverIdenticalTransferActivity_ProduceByteIdenticalState`); T-TX-BID-008 → `TransfersT0Tests.SubmitBid_PostsOnlyThroughTheLedger_AndTheTransfersAssemblyHoldsNoFinanceState`; T-TX-REKEY-003 → `TransfersT0Tests.TheTransfersAssembly_ReferencesNoOtherSubsystemsPerPlayerState` (an allow-list of the four asmdef references since AR round 5); the §5.7 "structural" rows FR-TX-011/013/023 cite it |
| G16 | FR prefix FR-TX unclaimed across `docs/specs/**`; XC-031-* allocated; the #40 §7.3 / #33 §7.3 consumer sides named | ✅ | grep-verified; §8.1 |
| G17 | Wage deferral: minimal is **fee-only** (no `PlayerWage` post), so #40 FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2) is preserved with **no #40 back-prop at approval**; the wage producer + `WageBudget` gate are deep (ERR-040, T3) | ✅ | FR-TX-005, §7.3, §8.3, T-TX-BID-001 (`TransfersT0Tests.SubmitBid_AcceptedBuy_PostsFeeOnly_CommitsSpend_AndInsertsContractUnderTheStableId` — `WageBillAggregate` and `TransferBudget` unchanged after an accepted buy; `SubmitBid_Sell_CommitsRosterBeforeRemovingContract_AndCreditsFee` for the sell). *(AR round 3: originally cited T-TX-BID-006, which is the sell-side commit-not-landed lock.)* |

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
| 0.6 | 2026-09-21 | — | ERR-031-001/002: G4 now matches shipped always-on positional scarcity, and G6 locks affordability before checked finance staging. |
| 0.7 | 2026-10-19 | — | ERR-031-005: G10 restated for the stable-id re-home (post-approval correction, owner decision Oct 19, 2026). |
| 0.8 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): G6's ✅ rested on false text ("removes its `Contract` **before** the infallible re-key"); restated to the shipped order — removal only after the commit is confirmed via `TryGetPlayer` — and re-verified against `src/transfers/TransferCommands.cs` and the two sell-path tests in `src/transfers/tests/TransfersT0Tests.cs` (evidence column now names them). |
| 0.9 | 2026-10-20 | — | ERR-031-011 (AR round 2): G9/G14/G15 were ✅ on text the T2 back-prop left false or incomplete. G9 restated to the re-anchored window derivation; G14 restated (contracts at genesis, expiry leaves the player un-contracted and sellable, managed regens contracted at the boundary); G15 counts FR-TX-001..029. All three re-verified against `src/transfers/TransferWindow.cs`, `src/season-save/League.cs`, `src/season-save/SeasonLoop.cs`, `src/transfers/TransfersState.cs`, `src/transfers/TransferCommands.cs` and the tests the evidence column names. |
| 0.10 | 2026-10-20 | — | ERR-031-013 (AR round 3): G11 cited a T-TX-NEU-001 lock narrower than the gate (restated to the new roll-through lock + non-vacuity + draw-free test); G12 cited a non-existent T-TX-SHAPE-001 (now T-TX-INT-001 and the byte-pinned layout test); G15 was ✅ with three §5 ids untested (T-TX-BID-008, T-TX-DET-003, T-TX-REKEY-003 — landed and re-verified); G17 cited T-TX-BID-006 for the wage-bill-unchanged evidence, which lives in T-TX-BID-001; G5 gains the T-TX-BID-008 evidence and a scope note that the counterparty AI club's #40 entry is deliberately untouched (M16), so the gate claims no league-wide ledger completeness. |
| 0.11 | 2026-10-20 | — | ERR-031-014 (AR round 4): G5 now says option (b) is the recorded interim behaviour and the (a)-vs-(b) owner decision is open (it read as a recorded owner decision); G11 cites `…RegistersNoRngStream…` and scopes it to FR-TX-016/G3; G15 cites the new explicit FR-TX-001..029 table in §5.7 for its FR → test direction. |
| 0.12 | 2026-10-21 | — | ERR-031-015 (AR round 5): G5 no longer says the counterparty is "deliberately" untouched (interim option (b), owner decision open) and cites the renamed test `…_InTheInterimMinimalTier`; G15's criterion admits "a tested structural lock" (§5.7's structural rows) and names T-TX-REKEY-003 as an allow-list. |
#endregion
