# Transfers, Contracts & Negotiation #31 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** October 18, 2026 (v0.8 — T1 landed: codec + frame-v8 composition; ERR-031-003 / ERR-030-054)
**Last Updated (prior):** September 21, 2026 (v0.7 — PR #407 current-main reconciliation: T0 status finalized; T2 finance-composition staging obligation recorded)
**Last Updated (prior):** September 14, 2026 (v0.6 — PR #407 residual review cleanup: T2 hook regression pinned)
**Last Updated (prior):** September 14, 2026 (v0.5 — PR #407 review correction to T0 public API and valuation contract; prior v0.4 T0 review close-out, v0.3 T0 authored, v0.2 AR-3, v0.1 initial)
**Version:** 0.8
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
  and sell producer preview/commit mismatch residuals while keeping local #31/#40 state untouched. No autonomous
  producer, RNG stream, save codec, season-loop invocation, or production roster adapter lands in T0.
- **T1 — LANDED October 18, 2026.** `TransfersSaveCodec` (magic `"XFER"` + `TRANSFERS_SAVE_FORMAT_VERSION = 1`,
  ERR-031-003) composed into #30's season save as a mandatory block (`SEASON_SAVE_FORMAT_VERSION` 7 → 8,
  ERR-030-054). `SeasonLoop` carries a cloned `TransfersState` through its constructor and `Restore`; the save root
  checks managed-club and contract-roster coherence at Save and Load and refuses an unwired save over a file that
  carries #31 state. Nothing drives the state yet: genesis seeding, the command seam and boundary aging are T2.
- **T2** — wire #30's transfers world-tick slot (ERR-030-004); build the production #30 adapter behind
  `ITransferRosterPort` + `DispatchRosterMoveHook` (ERR-030-005); add genesis contract seeding,
  season-boundary aging/reset, calendar-derived summer window, and production composition. The production
  adapter MUST prove successful preview→commit is infallible and add the §5 T-TX-REKEY-005 integration lock:
  hook dispatch occurs on managed↔external moves while #31's subscriber is observably a no-op during dispatch,
  leaving explicit contract insert/remove to `SubmitBid`. **Finance composition must also adapt T0's value-ref
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
- `CommittedSpendThisWindow` reset/next-window mechanics, owned by T2 season-boundary/window wiring.
- Production proof that #30 hook dispatch and #31 managed↔external no-op semantics coexist exactly as specified;
  T0 can only test the #31 subscriber and fake-port contract separately.

## 7.3 Seam contracts recorded for downstream authors

- **#40:** #31 reads `AvailableTransferBudget` and applies accepted fee transactions only through
  `ApplyTransaction`; T0 stages the value-struct result before the roster commit and publishes it only after
  the commit matches its preview. On current main, production finance state is owned behind #30 `SeasonLoop`
  rather than exposed as a mutable `ClubFinances` ref. T2 must bridge that ownership with an exact-state,
  non-mutating finance preflight/staging seam; it must not rebuild mutable finance from `FinanceView` or bypass
  `SeasonLoop.ApplyTransaction`.
- **#30:** owns world-tick timing, save composition, and the production roster adapter/move hook at T2. A full
  destination is surfaced by preview as `false` and maps to `TransferSubmissionOutcome.SquadFull`; a successful
  preview fixes the exact id the commit MUST return. The T2 adapter must dispatch `DispatchRosterMoveHook` and
  simultaneously demonstrate that #31's managed↔external subscriber does nothing during that callback, because
  `SubmitBid` owns the explicit managed contract insert/remove immediately after the commit returns.
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
#endregion