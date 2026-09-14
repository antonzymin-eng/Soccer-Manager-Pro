# Transfers, Contracts & Negotiation #31 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.4 — T0 review close-out; positional need/counter band moved into Stage 2)
**Last Updated (prior):** September 12, 2026 (v0.3 — T0 implementation authored; T1/T2/T3 remain deferred)
**Last Updated (prior):** July 23, 2026 (v0.2 — AR-3 fix pass; prior v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0 — IMPLEMENTED IN PR #407, pending merge.** `TacticalDirector.Transfers` production/test assemblies;
  value types (`Contract`, `Offer`, `NegotiationOutcome`, `TransferWindow`, `ClubTransferState`,
  `TransfersState`); deterministic `ValuePlayerPermille`, always-on #27 positional-stock valuation,
  accepted/counter/rejected `EvaluateOffer`, and inclusive `IsWindowOpen`; `TransfersConstants` loaded through
  Code Standards #20 `GameplayConfig`; and `SubmitBid` with validate-all-first atomic semantics.
  `ITransferRosterPort` is the consumer-owned read/preflight/commit seam for the already-specified #30 T2
  producer, including the read-only positional-stock query needed by Stage 2. No autonomous producer, RNG
  stream, save codec, season-loop invocation, or production roster adapter lands in T0.
- **T1** — `TransfersSaveCodec` (`TRANSFERS_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save;
  coordinate the outer `SEASON_SAVE_FORMAT_VERSION` bump and F3 gates.
- **T2** — wire #30's transfers world-tick slot (ERR-030-004); build the production #30 adapter behind
  `ITransferRosterPort` + `DispatchRosterMoveHook` (ERR-030-005); add genesis contract seeding,
  season-boundary aging/reset, calendar-derived summer window, and production composition. No RNG stream.
- **T3** — deep tier: #33 personality-modulated valuation, #28 CA/PA refinement, wage-bill producer and
  `WageBudget` gate with #40 back-prop, clauses/loans/wage structures, multi-day in-flight negotiation,
  stochastic rival-AI bidding (first #31 draw site; promotes the reserved domain/ordinal spec-text-first), and
  #34 staff influence. These refine the Stage-2 positional-need/counter-band baseline rather than replacing it.

## 7.2 Deferred (recorded, not built)

- **Autonomous AI-club bidding.** Minimal is manager-initiated only; proactive target selection is T3 and the
  first stochastic #31 producer.
- **Wage-bill economy.** T0 posts only transfer fees. `PlayerWage` posts + `WageBudget` affordability remain
  T3 and require the recorded #40 FR-FN-015 back-prop.
- **Contract free-agency / renewal / expiry warnings.** T2 boundary aging can remove expired contracts; the
  sequel flow remains deep.
- **Agents / clauses / loans / wage structures.** These append to the minimal contract model at T3.
- **#33 personality-modulated valuation.** Personality remains a read-only multiplicative refinement when the
  needed #33 trait surface exists. Unlike the superseded v0.2 plan, disabling/defering #33 no longer removes
  all context: Stage 2 already carries positional scarcity.
- **CA/PA-from-#28 valuation refinement.** Deep input refinement on the same valuation identity.
- **#34 staff influence.** `staffMult` remains identity until #34 produces it.
- **Multi-day counter negotiation.** T0 `CounterOffered` is synchronous/no-state; delayed terms/state remain T3.
- **Indexed/cached player search.** Performance extension only.

## 7.3 Seam contracts recorded for downstream authors

- **#40 (Club Finances):** #31 reads `AvailableTransferBudget` and posts only through `ApplyTransaction`; no
  parallel cash ledger. T0 posts transfer fee only. T3 wage economics require the recorded #40 back-prop.
- **#30 (season loop):** owns world-tick timing, season-save composition, and the production roster adapter / 
  move hook at T2. #31 MUST NOT reference #30.
- **#27 (squad/player data):** authoritative player identity and coarse `PlayerPosition`. T0 reads player records
  plus a count of the valuing club's players at that coarse position; all reads are non-mutating. Transfers
  still re-key only through #30's roster owner.
- **#32 / #34:** may consume the generic negotiation seam with their own caller-supplied counterparty values;
  #31 builds no interfaces for them. #34 additionally becomes the staff multiplier producer.
- **#33:** deep personality refinement is read-only and still needs the recorded trait-access back-prop at T3.
- **#38:** drives command APIs such as `SubmitBid`; MUST NOT mutate #31 state directly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3: T3/§7.2 add the deferred wage-bill producer + `WageBudget` gate + #40 FR-FN-015 back-prop (H), the deep club-need signal, and contract free-agency; §7.3 #40 seam corrected (minimal is fee-only, no back-prop at approval) + #33 seam notes only `MoraleOf` is granted, `PersonalityProfile` needs a T3 back-prop (L). |
| 0.3 | 2026-09-12 | — | T0 implementation authored: production/test assemblies, deterministic valuation/offer/window core, atomic `SubmitBid`, GameplayConfig-backed `[GT]` catalogue, and consumer-owned roster port. T1/T2/T3 remain deferred. |
| 0.4 | 2026-09-14 | — | T0 review close-out: positional scarcity moves from deep-only to always-on Stage 2; synchronous deterministic `CounterOffered` band replaces the one-unit accept/reject cliff; deep personality/CA/staff and multi-day state remain refinements. PR #407 remains pending merge. |
#endregion
