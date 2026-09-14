# Transfers, Contracts & Negotiation #31 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.5 — PR #407 review correction to T0 public API and valuation contract)
**Last Updated (prior):** September 14, 2026 (v0.4 — T0 review close-out; prior v0.3 T0 authored, v0.2 AR-3, v0.1 initial)
**Version:** 0.5
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0 — IMPLEMENTED IN PR #407, pending merge.** `TacticalDirector.Transfers` production/test assemblies;
  value types (`Contract`, `Offer`, `NegotiationOutcome`, `TransferSubmissionOutcome`, `TransferWindow`,
  `ClubTransferState`, `TransfersState`); deterministic currency-returning `ValuePlayer`/`CounterpartyValue`;
  always-on #27 positional stock measured prospectively with the negotiated player excluded; synchronous
  accepted/counter/rejected negotiation; typed `InsufficientBudget`/`SquadFull` command results; inclusive
  `IsWindowOpen`; and `SubmitBid` with validate/preflight-first semantics. `ITransferRosterPort` remains the
  consumer-owned read/preflight/commit seam for the already-specified #30 T2 producer. No autonomous producer,
  RNG stream, save codec, season-loop invocation, or production roster adapter lands in T0.
- **T1** — `TransfersSaveCodec` (`TRANSFERS_SAVE_FORMAT_VERSION = 1`) + composition into #30's season save;
  coordinate the outer `SEASON_SAVE_FORMAT_VERSION` bump and F3 gates.
- **T2** — wire #30's transfers world-tick slot (ERR-030-004); build the production #30 adapter behind
  `ITransferRosterPort` + `DispatchRosterMoveHook` (ERR-030-005); add genesis contract seeding,
  season-boundary aging/reset, calendar-derived summer window, and production composition. No RNG stream.
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

## 7.3 Seam contracts recorded for downstream authors

- **#40:** #31 reads `AvailableTransferBudget` and applies accepted fee transactions only through
  `ApplyTransaction`; T0 stages the value-struct result before the roster commit and publishes it only after
  the commit matches its preview.
- **#30:** owns world-tick timing, save composition, and the production roster adapter/move hook at T2. A full
  destination is surfaced by preview as `false` and maps to `TransferSubmissionOutcome.SquadFull`; a successful
  preview fixes the exact id the commit MUST return.
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
#endregion
