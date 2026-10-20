# Transfers, Contracts & Negotiation #31 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.10 — T2: the production port and its new members)
**Last Updated (prior):** October 20, 2026 (v0.9 — ERR-031-006: §4.5 #30/#40 bullets completed for the stable-id re-home)
**Last Updated (prior):** October 19, 2026 (v0.8 — ERR-031-005: §4.5 stable-id port contract)
**Last Updated (prior):** October 18, 2026 (v0.7 — #31 T1 landed: §4.2 codec row live, §4.4 rewritten for the shipped composition; ERR-031-003)
**Last Updated (prior):** September 14, 2026 (v0.6 — PR #407 review correction: submission-result seam + prospective-stock semantics)
**Last Updated (prior):** September 14, 2026 (v0.5 — T0 football-judgment close-out; prior v0.4 T0 landing, v0.3 AR-6, v0.2 AR-3, v0.1 initial)
**Version:** 0.10
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
| `TransfersState.cs` | managed contract store + club state (T0, live; `OnPlayerRekeyed` deleted at ERR-031-005 — nothing re-keys) |
| `ITransferRosterPort.cs` | consumer-owned player/position reads plus exact preflight/commit seam for the specified #30 T2 producer |
| `TransferCommands.cs` | atomic `SubmitBid → TransferSubmissionOutcome` command surface (T0, live) |
| `TransfersSaveCodec.cs` | magic-led `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob encode/decode (T1, live; ERR-031-003) |
| `TransfersConstants.cs` | Appendix A catalogue using `GameplayConfig` for `[GT]` values (T0, live) |

## 4.3 The reusable negotiation seam (KD-3)

`EvaluateOffer(in Offer, long counterpartyValuation)` is authored generically over a caller-supplied currency
valuation and emits only `NegotiationOutcome.Accepted`, `.CounterOffered`, or `.Rejected`. #32/#34 can reuse
that seam without importing manager-club budget or roster-capacity semantics.

`SubmitBid`, by contrast, returns **`TransferSubmissionOutcome`**, which maps those three negotiation states and
adds `InsufficientBudget` / `SquadFull` for ordinary player-reachable command results. Keeping the enums
separate prevents UI/T2 concerns from contaminating the reusable negotiation API.

## 4.4 Save composition (KD-4)

**T1 (live).** `TransfersSaveCodec.Encode(TransfersState stateOrNull) → byte[]` and `Decode(byte[]) → TransfersState`
(null for the zero-club, unwired block). #30's season root composes the opaque, independently version-gated block
into its frame between the #40 finance block and the optional match block (`SEASON_SAVE_FORMAT_VERSION` 7 → 8,
ERR-030-054) and never parses it; #30 owns the cross-blob rule that the state belongs to the season's managed
club and that every contract names a rostered player. T1 adds `TacticalDirector.DeterministicSim` to #31's
references for `CanonicalSerializer` / `SaveBlobFramingHelpers` only — still no RNG stream (KD-5).

## 4.5 Interface contracts recorded for composition & #30

- **Roster consumer port (live; stable ids since ERR-031-005):** `TryGetPlayer` (record + owning club),
  `CarriesClub` (the league's club universe, ERR-031-010), `CountPlayersAtPosition` and `CanRelease` (the releasing
  club keeps a matchday squad, ERR-031-007) are read-only; ownership is never derived from the id. The production
  implementation is #30's `SeasonTransferRoster` over the #28 store and the #29/#41/#30 career rows (T2). For valuation,
  `TransferCommands` converts the raw current stock into **stock excluding the negotiated player**: subtract one
  on a manager buy because the seller currently owns him; subtract zero on a manager sell because the buyer does
  not. `TryPreviewRosterCommit` returns `false` for ordinary destination-capacity exhaustion (`CLUB_SQUAD_SIZE`
  players). `RequestRosterCommit` re-homes the player under his unchanged id and is contractually infallible after
  that preview; #31 confirms through `TryGetPlayer` that he is on the destination roster **before** applying staged
  finance/spend/contract state.
- **The composition root** MUST invoke #31 at the recorded T2 season slot, route UI commands through `SubmitBid`,
  adapt the #30 roster owner to `ITransferRosterPort`, and never let the UI mutate #31 state directly.
- **#30** owns the future tick slot, the production id-preserving roster re-home (`RequestRosterCommit`) and
  season-save composition. *(ERR-031-006: originally "production roster move/hook" — the re-key hook was deleted
  at ERR-031-005; nothing re-keys, so there is no hook to own.)*
- **#40** is consumed only through `AvailableTransferBudget` and `ApplyTransaction`; `TransferCommands` stages
  the canonical transaction on a value-copy, then assigns that validated copy only after the port's `TryGetPlayer`
  confirms the roster commit landed the player on the destination roster *(ERR-031-006: originally "after the
  roster commit matches its preview")*. #31 still writes no #40 field directly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 save/version coordination and #30 mid-season seam clarification. |
| 0.3 | 2026-07-23 | — | AR-6 genesis-vs-load composition rule. |
| 0.4 | 2026-09-12 | — | T0 implementation back-prop: live phase-real edges and consumer-owned roster port. |
| 0.5 | 2026-09-14 | — | T0 football-judgment close-out: positional-stock read and live synchronous counter-offer semantics. |
| 0.6 | 2026-09-14 | — | PR #407 review correction: separate submission-result enum, currency API names, prospective stock excluding negotiated player, and verify roster preview/commit before local mutation. |
| 0.7 | 2026-10-18 | — | #31 T1 landing: `TransfersSaveCodec.cs` live (magic-led, ERR-031-003); §4.4 records the shipped frame-v8 composition (ERR-030-054), the null-for-unwired decode, #30-owned coherence, and the new `DeterministicSim` reference (serialization only). |
| 0.8 | 2026-10-19 | — | ERR-031-005: §4.5 port contract — owning club reported by `TryGetPlayer`, capacity-only preview, id-preserving commit confirmed through the port. |
| 0.9 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): §4.5 #30 bullet owns the id-preserving re-home, not a "roster move/hook"; #40 bullet applies the staged copy after `TryGetPlayer` confirms the landing, not after the commit "matches its preview". |
| 0.10 | 2026-10-20 | — | #31 T2: §4.5 names `CarriesClub` / `CanRelease` (ERR-031-010 / ERR-031-007) and the production `SeasonTransferRoster`. |
#endregion
