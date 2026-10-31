# Transfers, Contracts & Negotiation #31 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.14 — AR round 4, ERR-031-014: §7.2 records option (b) as the interim behaviour and the counterparty-ledger choice as an open owner decision)
**Last Updated (prior):** October 20, 2026 (v0.13 — AR round 3, ERR-031-013: §7.2 records the counterparty-ledger owner decision)
**Last Updated (prior):** October 20, 2026 (v0.12 — T-TX-REKEY-005 no longer deferred)
**Last Updated (prior):** October 20, 2026 (v0.11 — ERR-031-011: §7.2 no longer defers the spend reset / next window; T2 bullet names ERR-031-011)
**Last Updated (prior):** October 20, 2026 (v0.10 — T2 landed)
**Last Updated (prior):** October 20, 2026 (v0.9 — ERR-031-006: T2 plan, §7.2 and §7.3 restated for the stable-id re-home; hook obligation removed)
**Last Updated (prior):** October 18, 2026 (v0.8 — T1 landed: codec + frame-v8 composition; ERR-031-003 / ERR-030-054)
**Last Updated (prior):** September 21, 2026 (v0.7 — PR #407 current-main reconciliation: T0 status finalized; T2 finance-composition staging obligation recorded)
**Last Updated (prior):** September 14, 2026 (v0.6 — PR #407 residual review cleanup: T2 hook regression pinned)
**Last Updated (prior):** September 14, 2026 (v0.5 — PR #407 review correction to T0 public API and valuation contract; prior v0.4 T0 review close-out, v0.3 T0 authored, v0.2 AR-3, v0.1 initial)
**Version:** 0.14
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0 — IMPLEMENTED BY PR #407.** `TacticalDirector.Transfers` production/test assemblies;
  value types (`Contract`, `Offer`, `NegotiationOutcome`, `TransferSubmissionOutcome`, `TransferWindow`,
  `ClubTransferState`, `TransfersState`); deterministic currency-returning `ValuePlayer`/`CounterpartyValue`;
  always-on #27 positional stock measured prospectively with the negotiated player excluded; synchronous
  accepted/counter/rejected negotiation; typed `InsufficientBudget`/`SquadFull` command results; inclusive
  `IsWindowOpen`; and `SubmitBid` with validate/preflight-first semantics. `ITransferRosterPort` remains the
  consumer-owned read/preflight/commit seam for the already-specified #30 T2 producer. T0 tests cover both buy
  and sell producer commit-not-landed residuals (originally preview/commit id-mismatch residuals; restated at
  ERR-031-005 / ERR-031-006) while keeping local #31/#40 state untouched. No autonomous
  producer, RNG stream, save codec, season-loop invocation, or production roster adapter lands in T0.
- **T1 — LANDED October 18, 2026.** `TransfersSaveCodec` (magic `"XFER"` + `TRANSFERS_SAVE_FORMAT_VERSION = 1`,
  ERR-031-003) composed into #30's season save as a mandatory block (`SEASON_SAVE_FORMAT_VERSION` 7 → 8,
  ERR-030-054). `SeasonLoop` carries a cloned `TransfersState` through its constructor and `Restore`; the save root
  checks managed-club and contract-roster coherence at Save and Load and refuses an unwired save over a file that
  carries #31 state. Nothing drives the state yet: genesis seeding, the command seam and boundary aging are T2.
- **T2 — LANDED October 20, 2026.** `SeasonTransferRoster` (#30's production port: capacity-only preview that
  throws on impossible moves, id-preserving re-home of the #28 record + lifecycle and the #29/#41/#30 rows),
  `SeasonLoop.SubmitTransferBid` (exact-value #40 staging, publish only on `Accepted`), genesis seeding in
  `League.CreateLoop`, the staged roll step (d″) (§3.7), and a refusal of raw `TransferFee` postings once #31 is
  wired. The world-tick slot 5 stays a documented null seam (ERR-030-004): the minimal tier has no daily work — the
  window is a predicate evaluated at command time (§6.1). T2 surfaced ERR-031-007 (depletion), ERR-031-008 (window),
  ERR-031-009 (un-contracted sale), ERR-031-010 (preview semantics / unknown club) and #30's ERR-030-056;
  AR round 2 re-anchored the window close to the opening fixture day (ERR-031-011).
  *(Original plan text follows.)* T2 — wire #30's transfers world-tick slot (ERR-030-004); build the production #30 adapter behind
  `ITransferRosterPort` over #30's mid-season roster-commit entry point (ERR-030-005) — a capacity-only
  `TryPreviewRosterCommit` and an id-preserving `RequestRosterCommit` re-home (ERR-031-005); add genesis contract
  seeding, season-boundary aging/reset, calendar-derived summer window, and production composition. The
  production adapter MUST prove successful preview→commit is infallible (the commit lands the player on the
  destination roster, which #31 confirms through `TryGetPlayer`) and add the §5 T-TX-REKEY-005 carry-everything
  integration lock: a real re-home carries the player's #28 record + lifecycle, #29/#41/#30 rows and #44 tally
  under the same id and leaves nothing behind at the source club, while explicit managed contract insert/remove
  stays with `SubmitBid` after the confirmed commit. *(ERR-031-006: this bullet originally required building the
  adapter "+ `DispatchRosterMoveHook`" and a hook-dispatch / subscriber-no-op lock; the re-key hook was deleted at
  ERR-031-005 — nothing re-keys, so there is nothing to dispatch.)* **Finance composition must also adapt T0's value-ref
  command boundary to #30's already-live `SeasonLoop.AvailableTransferBudget(int)` / `ApplyTransaction(int, in
  FinanceTransaction)` ownership without exposing or reconstructing raw mutable `ClubFinances` from
  `FinanceView`. T2 therefore owes a non-mutating preflight/staging seam over the exact SeasonLoop-held #40 state
  before the roster commit, followed by publication through the canonical #40 transaction path only after the
  roster commit is guaranteed infallible; budget/full/counter/reject paths must remain no-mutation.** No RNG stream.
- **T3** — deep tier: #33 personality-modulated valuation, #28 CA/PA refinement, wage-bill producer and
  `WageBudget` gate with #40 back-prop, clauses/loans/wage structures, multi-day negotiation, stochastic rival
  bidding (first #31 draw site), and #34 staff influence.

## 7.2 Deferred (recorded, not built)

- Autonomous AI-club bidding and stochastic target selection.
- Wage-bill economics / `WageBudget` gating.
- Contract free-agency, renewal and expiry-warning sequel flows.
- Agents, clauses, loans and wage structures.
- #33 personality and #28 CA/PA valuation refinements.
- #34 staff influence.
- Multi-day counter negotiation; T0 `CounterOffered` remains synchronous/no-state.
- Indexed/cached player search.
- **Mirror posting to the counterparty AI club's #40 entry — OPEN OWNER DECISION (AR round 3, ERR-031-013; open-issues
  `transfer-counterparty-ledger`).** Option (b) is the recorded INTERIM behaviour the minimal tier ships (the
  counterparty is left unposted); the (a)-vs-(b) choice has NOT been made by the project owner and nothing here
  claims it has. Minimal posts the fee to the MANAGED club only (KD-2), so AI clubs' `FinanceView`
  and `SettleFinances` never see transfer cash and money enters or leaves the league on every transfer. Option (a):
  post the mirror leg (a buy credits the seller, a sale debits the buyer) inside the same staged, publish-on-`Accepted`
  seam as the managed leg, which needs a #40 cross-reference for the AI club's affordability and an invariant
  that the two entries change by equal and opposite amounts. Option (b): keep the minimal tier as is (the interim behaviour that ships).
- *(No longer deferred — T-TX-REKEY-005, the production proof that a real #30 re-home carries the player's whole
  career — #28 record + lifecycle, #29/#41/#30 rows, #44 tally — under the same id and leaves nothing behind, landed
  with AR round 2 in `SeasonTransfersT2Tests`; see §5.3. ERR-031-006 had restated it from "proof that #30 hook
  dispatch and #31 managed↔external no-op semantics coexist" — the hook and #31's subscriber were deleted at
  ERR-031-005.)*

## 7.3 Seam contracts recorded for downstream authors

- **#40:** #31 reads `AvailableTransferBudget` and applies accepted fee transactions only through
  `ApplyTransaction`; T0 stages the value-struct result before the roster commit and publishes it only after
  `TryGetPlayer` confirms the commit landed the player on the destination roster *(ERR-031-006: originally "after
  the commit matches its preview")*. On current main, production finance state is owned behind #30 `SeasonLoop`
  rather than exposed as a mutable `ClubFinances` ref. T2 must bridge that ownership with an exact-state,
  non-mutating finance preflight/staging seam; it must not rebuild mutable finance from `FinanceView` or bypass
  `SeasonLoop.ApplyTransaction`.
- **#30:** owns world-tick timing, save composition, and the production roster adapter at T2. A full
  destination (`CLUB_SQUAD_SIZE` players) is surfaced by preview as `false` and maps to
  `TransferSubmissionOutcome.SquadFull`; a successful preview guarantees the subsequent `RequestRosterCommit` — which
  returns nothing — re-homes the player onto the destination roster under his unchanged id, and #31 confirms that
  through `TryGetPlayer` before any local write. No hook is dispatched: the re-home carries every PlayerId-keyed
  row with the player (T-TX-REKEY-005 locks that nothing is left behind), and `SubmitBid` owns the explicit managed
  contract insert/remove immediately after the confirmed commit. *(ERR-031-006: originally "a successful preview
  fixes the exact id the commit MUST return. The T2 adapter must dispatch `DispatchRosterMoveHook` and
  simultaneously demonstrate that #31's managed↔external subscriber does nothing during that callback" — the
  re-key design deleted at ERR-031-005.)*
- **#27:** authoritative player identity and coarse `PlayerPosition`. Positional need uses the valuing club's
  current same-position count converted to **stock excluding the negotiated player** before valuation.
- **#32 / #34:** may consume `Offer` + `NegotiationOutcome` + `EvaluateOffer`; they do not consume the manager
  command's `TransferSubmissionOutcome` unless they intentionally call that command layer.
- **#33:** deep personality refinement remains read-only.
- **#38:** drives `SubmitBid`; ordinary budget/squad-capacity conditions are typed outcomes, not exceptions.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 deep wage/need/free-agency clarifications. |
| 0.3 | 2026-09-12 | — | T0 implementation authored. |
| 0.4 | 2026-09-14 | — | T0 football-judgment close-out: positional scarcity + synchronous deterministic counter band. |
| 0.5 | 2026-09-14 | — | PR #407 review correction: currency API names, exact-rational attribute mean, exclude-player positional stock, separate submission outcomes, and preview/commit-before-local-mutation contract. |
| 0.6 | 2026-09-14 | — | Residual review cleanup: T0 covers both preview/commit breach directions; T2 explicitly owes successful-preview infallibility plus real hook-dispatch and observable managed↔external #31 no-op integration coverage. |
| 0.7 | 2026-09-21 | — | PR #407 current-main reconciliation: finalizes T0 landing status after preserving the original branch lineage and records the newly-visible T2 finance-composition obligation created by #40 T2a/T2b: stage/preflight against exact SeasonLoop-held finance state without exposing/reconstructing raw `ClubFinances`, then publish only through the canonical transaction path after an infallible roster commit. T1/T2/T3 phase boundaries are unchanged. |
| 0.8 | 2026-10-18 | — | T1 landed: magic-led `TransfersSaveCodec` (ERR-031-003) composed into the v8 season frame (ERR-030-054); loop carries a cloned state; coherence + fifth destination guard. T2/T3 boundaries unchanged. |
| 0.9 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): §7.1 T2 builds the adapter over #30's roster-commit entry point with a capacity-only preview and an id-preserving re-home, and owes the T-TX-REKEY-005 carry-everything lock instead of `DispatchRosterMoveHook` + a hook-dispatch/no-op lock; §7.2 deferred proof and §7.3 #30/#40 seam contracts restated (commit returns nothing, confirmed via `TryGetPlayer`). The v0.6 row's hook-dispatch obligation is superseded. |
| 0.10 | 2026-10-20 | — | #31 T2 landed: production port, bid seam, genesis, staged boundary; slot 5 remains a null seam at minimal; ERR-031-007..010. |
| 0.11 | 2026-10-20 | — | ERR-031-011 (AR round 2): §7.2 drops "`CommittedSpendThisWindow` reset/next-window mechanics" from the deferred list — T2 landed both (§3.7 step (d″), §3.5); the T2 bullet records ERR-031-011. The T-TX-REKEY-005 deferral line is left for the pass that completes that lock. |
| 0.12 | 2026-10-20 | — | AR round 2: §7.2 drops the T-TX-REKEY-005 production proof from the deferred list — it is live (§5.3). |
| 0.13 | 2026-10-20 | — | ERR-031-013 (AR round 3, M16): §7.2 adds the deferred owner decision on mirror-posting transfer cash to the counterparty AI club's #40 entry (option (a) mirror post in the same staged seam vs (b) keep the minimal tier), cross-referencing open-issues `transfer-counterparty-ledger` and KD-2. |
| 0.14 | 2026-10-20 | — | ERR-031-014 (AR round 4, R4-L3): §7.2 states that option (b) ships as the recorded interim behaviour and that the (a)-vs-(b) choice has NOT been made by the project owner (the 0.13 wording read as a recorded owner decision). |
#endregion
