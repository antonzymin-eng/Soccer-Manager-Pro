# Transfers, Contracts & Negotiation #31 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.6 — PR #407 review correction: submission-result seam + prospective-stock semantics)
**Last Updated (prior):** September 14, 2026 (v0.5 — T0 football-judgment close-out; prior v0.4 T0 landing, v0.3 AR-6, v0.2 AR-3, v0.1 initial)
**Version:** 0.6
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

At **T0**, **`TacticalDirector.Transfers`** (`src/transfers/`) references **`#27 PlayerDatabase`**
(player records, canonical attributes, coarse position, and read-only positional stock), **`#40 ClubFinances`**
(the constraint + single commit path), and **`TacticalDirector.ProjectConstants`** solely for Code Standards
#20-mandated `GameplayConfig.Get*` loading of #31's `[GT]` catalogue.

T0 deliberately carries no dead #16/#30/#22 edge: minimal #31 is draw-free, has no codec, and consumes the
future #30 roster owner through a #31-owned interface adapted by composition at T2.

```
T0 current:  #31 Transfers ──► #27 PlayerDatabase
                         ├──► #40 ClubFinances
                         └──► ProjectConstants

T2 future: compositionRoot ──► #31 Transfers
                              └─ adapts #31 ITransferRosterPort to #30 roster owner
T3 future: #32/#34/#38 ─────► #31 Transfers
```

Acyclic; no sim assembly references #31's consumers.

## 4.2 File layout (lands incrementally by T-phase)

| File | Contents |
|---|---|
| `Contract.cs` | `Contract` value type (T0, live; FR-TX-015 append discipline) |
| `Offer.cs` / `NegotiationOutcome.cs` | counterparty-generic `Offer`/`NegotiationOutcome` plus manager-command `TransferSubmissionOutcome` (T0, live) |
| `TransferWindow.cs` | `TransferWindow` + inclusive `IsWindowOpen` (T0, live); derivation remains T2 composition work |
| `ClubTransferState.cs` | season-scoped window + `CommittedSpendThisWindow` state (T0, live) |
| `PlayerValuation.cs` | currency-returning `ValuePlayer` / `CounterpartyValue`, age curve, and always-on prospective positional-stock multiplier (T0, live) |
| `NegotiationEngine.cs` | deterministic accepted/counter/rejected evaluation only; budget/capacity are not negotiation outcomes |
| `TransfersState.cs` | managed contract store + club state + `OnPlayerRekeyed` semantics (T0, live) |
| `ITransferRosterPort.cs` | consumer-owned player/position reads plus exact preflight/commit seam for the specified #30 T2 producer |
| `TransferCommands.cs` | atomic `SubmitBid → TransferSubmissionOutcome` command surface (T0, live) |
| `TransfersSaveCodec.cs` | `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob encode/decode (T1, deferred) |
| `TransfersConstants.cs` | Appendix A catalogue using `GameplayConfig` for `[GT]` values (T0, live) |

## 4.3 The reusable negotiation seam (KD-3)

`EvaluateOffer(in Offer, long counterpartyValuation)` is authored generically over a caller-supplied currency
valuation and emits only `NegotiationOutcome.Accepted`, `.CounterOffered`, or `.Rejected`. #32/#34 can reuse
that seam without importing manager-club budget or roster-capacity semantics.

`SubmitBid`, by contrast, returns **`TransferSubmissionOutcome`**, which maps those three negotiation states and
adds `InsufficientBudget` / `SquadFull` for ordinary player-reachable command results. Keeping the enums
separate prevents UI/T2 concerns from contaminating the reusable negotiation API.

## 4.4 Save composition (KD-4)

`TransfersSaveCodec.Encode(in TransfersState) → byte[]` remains T1. The composition root will append the opaque,
independently version-gated transfers sub-blob to #30's `SeasonSaveCodec` frame; #30 never parses its contents.
T0 intentionally ships no codec and no save-format change.

## 4.5 Interface contracts recorded for composition & #30

- **T0 roster consumer port (live):** `TryGetPlayer` and `CountPlayersAtPosition` are read-only. For valuation,
  `TransferCommands` converts the raw current stock into **stock excluding the negotiated player**: subtract one
  on a manager buy because the seller currently owns him; subtract zero on a manager sell because the buyer does
  not. `TryPreviewRosterCommit` returns `false` for ordinary destination-capacity exhaustion and otherwise fixes
  the exact destination `PlayerId`. `RequestRosterCommit` is contractually infallible after that preview and MUST
  return the same id. #31 verifies that equality **before** applying staged finance/spend/contract state.
- **The composition root** MUST invoke #31 at the recorded T2 season slot, route UI commands through `SubmitBid`,
  adapt the #30 roster owner to `ITransferRosterPort`, and never let the UI mutate #31 state directly.
- **#30** owns the future tick slot, production roster move/hook and season-save composition.
- **#40** is consumed only through `AvailableTransferBudget` and `ApplyTransaction`; `TransferCommands` stages
  the canonical transaction on a value-copy, then assigns that validated copy after the roster commit matches
  its preview. #31 still writes no #40 field directly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 save/version coordination and #30 mid-season seam clarification. |
| 0.3 | 2026-07-23 | — | AR-6 genesis-vs-load composition rule. |
| 0.4 | 2026-09-12 | — | T0 implementation back-prop: live phase-real edges and consumer-owned roster port. |
| 0.5 | 2026-09-14 | — | T0 football-judgment close-out: positional-stock read and live synchronous counter-offer semantics. |
| 0.6 | 2026-09-14 | — | PR #407 review correction: separate submission-result enum, currency API names, prospective stock excluding negotiated player, and verify roster preview/commit before local mutation. |
#endregion
