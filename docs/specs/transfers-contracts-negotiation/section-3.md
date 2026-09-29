# Transfers, Contracts & Negotiation #31 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** September 21, 2026 (v0.9 — ERR-031-002 affordability-before-staging correction)
**Last Updated (prior):** September 14, 2026 (v0.8 — PR #407 residual review cleanup: budget invariant + T2 hook lock)
**Last Updated (prior):** September 14, 2026 (v0.7 — PR #407 review correction: precise valuation, symmetric need, typed submission outcomes; prior v0.6 T0 football close-out, v0.5 AR-8, v0.4 AR-6, v0.3 AR-3/AR-4, v0.2 AR-1, v0.1 initial)
**Version:** 0.9
**Status:** APPROVED

---

All arithmetic is **integer** (currency `long`; valuation/club-need/personality per-mille `int`). No stochastic
draw occurs at the minimal tier (FR-TX-001/016). `PERMILLE_DENOM = 1000`.

## 3.1 Player valuation — `ValuePlayer` / counterparty view (FR-TX-001/002)

Stage 2 keeps the pure #27 **attributes + age** valuation as the reusable identity, then applies one always-on
situational input: the **valuing club's positional stock**. The base function returns **currency**, not a
per-mille quantity. To avoid a 20-bucket price model, the 31-field attribute mean is retained as the exact
rational `sum / ATTRIBUTE_COUNT` until the final currency division:

```
ValuePlayer(in PlayerAttributes attrs, int age):
    sum      := CanonicalAttributeSum(attrs)                # exact sum of 31 [1,20] fields; weak foot excluded
    ageMult  := AgeCurvePermille(age)                       # peak 1000; older decline; young discount
    numer    := sum * VALUE_PER_RATING_POINT * ageMult
    denom    := ATTRIBUTE_COUNT * PERMILLE_DENOM
    return numer / denom                                    # integer currency; one attribute point can move value

ClubNeedMultiplierPermille(int samePositionCountExcludingPlayer):
    neutral  := CLUB_SQUAD_SIZE / POSITION_COUNT
    delta    := neutral - samePositionCountExcludingPlayer
    return 1000 + delta * CLUB_NEED_PER_PLAYER_PERMILLE     # configured/derived bound keeps result > 0

counterpartyView(playerId, clubId, isManagerBuy):
    p        := #27 PlayerRecord(playerId)
    rawStock := #27 CountPlayersAtPosition(clubId, p.Position)
    stock    := isManagerBuy ? rawStock - 1 : rawStock       # exclude negotiated player on BOTH directions
    identity := ValuePlayer(p.Attributes, p.Age)
    return identity * ClubNeedMultiplierPermille(stock) / 1000
```

The `clubId` is the **valuing** club: seller on a manager buy, buyer on a manager sell. A seller currently owns
the negotiated player, so its raw stock includes him and subtracts one; a buyer does not yet own him, so its raw
stock already excludes him. Both directions therefore evaluate the same prospective-stock concept instead of
being offset by one player. Personality/staff/CA remain deep refinements of this Stage-2 value.

## 3.2 Offer evaluation — `EvaluateOffer` (FR-TX-003)

The original exact-value cliff is softened by a deterministic **counter-offer band**. The reusable evaluator
returns only negotiation semantics; budget and squad-capacity results belong to `SubmitBid` instead:

```
EvaluateOffer(in Offer offer, long counterpartyValuation):
    validate ALL Offer terms else throw
    band := max(1, counterpartyValuation * NEGOTIATION_COUNTER_BAND_PERMILLE / 1000)  # if value > 0

    if offer.IsBuy:
        if offer.Fee >= counterpartyValuation:        return Accepted
        if offer.Fee >= counterpartyValuation-band:   return CounterOffered
        return Rejected
    else:
        if offer.Fee <= counterpartyValuation:        return Accepted
        if offer.Fee <= counterpartyValuation+band:   return CounterOffered
        return Rejected
```

`counterpartyValuation` is caller-supplied, so the KD-3 reuse contract remains intact. `CounterOffered` means
only “close enough to keep negotiation open”; it stores no in-flight state at T0.

## 3.3 The bid pipeline — `SubmitBid` (FR-TX-009/025, atomic)

`SubmitBid` returns `TransferSubmissionOutcome`. Negotiation results map one-for-one to `Rejected`, `Accepted`
and `CounterOffered`; ordinary player-reachable resource conditions add `InsufficientBudget` and `SquadFull`.
Malformed terms, out-of-window commands and violated producer/state invariants still fail loud.

```
SubmitBid(managerClubId, in Offer offer, worldDay, ref ClubFinances finances, ref TransfersState txState):
    (fromClub, toClub) := offer.IsBuy ? (offer.CounterpartyClubId, managerClubId)
                                      : (managerClubId, offer.CounterpartyClubId)

    # ---- VALIDATE/PREFLIGHT (no local mutation) ----
    require offer well-formed                                                       else throw  # F6
    require IsWindowOpen(txState.WindowFor(managerClubId), worldDay)                else throw  # F4
    require PlayerInClubUniverse(offer.PlayerId) AND ClubOf(offer.PlayerId)==fromClub else throw

    cv := counterpartyView(offer.PlayerId, offer.CounterpartyClubId, offer.IsBuy)
    negotiation := EvaluateOffer(offer, cv)
    if negotiation == Rejected:       return TransferSubmissionOutcome.Rejected
    if negotiation == CounterOffered: return TransferSubmissionOutcome.CounterOffered

    if offer.IsBuy:
        budget := AvailableTransferBudget(finances)
        committed := txState.CommittedSpend(managerClubId)
        require 0 <= committed <= budget                    else throw  # corrupted/invariant-breaking state
        if offer.Fee > budget - committed:
            return InsufficientBudget                       # F1; no mutation
        # ERR-031-002: decide the ordinary affordability outcome before checked finance staging.
    else:
        require valid managed Contract(offer.PlayerId)       else throw

    txn := offer.IsBuy ? {Debit,TransferFee,offer.Fee} : {Credit,TransferFee,offer.Fee}
    stagedFinances := finances
    ApplyTransaction(ref stagedFinances, txn)               # canonical #40 mutation on staged copy

    if !TryPreviewRosterCommit(fromClub, toClub, offer.PlayerId, out previewId):
        return SquadFull                                     # F5; no mutation

    require previewId belongs to toClub                      else throw
    require no duplicate managed contract at previewId       else throw

    # ---- COMMIT ----
    committedId := RequestRosterCommit(fromClub, toClub, offer.PlayerId)
    require committedId == previewId                         else throw  # producer contract breach

    finances := stagedFinances
    if offer.IsBuy:
        txState.AddCommittedSpend(managerClubId, offer.Fee)
        txState.InsertContract(ContractFrom(offer, previewId))
    else:
        txState.RemoveContract(offer.PlayerId)

    return Accepted
```

The buy affordability result is resolved **before** staged #40 mutation validation. This ordering is normative:
a coherent signed `Balance` may be near `long.MinValue`, and checked debit staging must not throw before the
player-reachable `InsufficientBudget` result (`ERR-031-002`). All local #31/#40 writes occur only **after**
the preflighted roster commit returns its promised preview id.
That removes the previous defensive check-after-mutation ordering. Because `OnPlayerRekeyed` is a specified
no-op for every minimal managed↔external move, a sell may retain its old managed contract during the roster
commit and remove it immediately afterward without double-handling.

A preview/commit id mismatch is an integration defect after the external port has already mutated. T0 therefore
can guarantee only that local #31/#40 state is untouched; it cannot roll the producer back. The regression suite
locks both directions: on a buy no contract/spend/finance is applied, and on a sell the old managed contract and
finance remain. T2's production adapter MUST make the successful-preview→commit contract genuinely infallible.

**Static-ceiling consequence (KD-2).** A sell posts a `Credit` to #40's `Balance` but does **not** touch
`committedSpendThisWindow` and does **not** raise `AvailableTransferBudget`; sell proceeds therefore do not
increase in-window buy headroom at minimal.

## 3.4 The #30 boundary — roster preflight/commit re-key (KD-7)

`ITransferRosterPort` is #31-owned and T2 composition adapts it to #30's roster owner. T0 uses three read/
preflight facts before the commit: `TryGetPlayer`, `CountPlayersAtPosition`, and `TryPreviewRosterCommit`.
A successful preview fixes the exact destination id. The subsequent `RequestRosterCommit` is contractually
infallible and MUST return that id; a mismatch is an integration defect, not a normal transfer outcome.

```
TryPreviewRosterCommit(fromClubId, toClubId, playerId, out newId):
    validate source ownership
    find free destination local index
    if none: return false
    newId := toClubId * CLUB_SQUAD_SIZE + freeLocal
    return true

RequestRosterCommit(fromClubId, toClubId, playerId):
    # precondition: successful preview with no intervening roster mutation
    MovePlayerRecord(fromClubId, playerId, toClubId, previewedId)
    DispatchRosterMoveHook(playerId, previewedId)
    return previewedId
```

`DispatchRosterMoveHook` calls each subscriber. #31 moves only its own contract state and does nothing for the
minimal managed↔external hook because `SubmitBid` owns the explicit insert/remove. At T2 this becomes a required
production integration regression: a real managed→external commit MUST dispatch the hook, and observing #31's
subscriber during that dispatch MUST prove it is a no-op (the old managed contract remains present until
`SubmitBid` removes it after the commit returns). The mirror external→managed hook is likewise a #31 no-op until
`SubmitBid` inserts the destination contract.

## 3.5 The transfer window — `IsWindowOpen` (FR-TX-019/020)

```
DeriveSummerWindow(in SeasonCalendar cal):
    return TransferWindow{ OpenWorldDay  = SEASON_START_WORLD_DAY,
                           CloseWorldDay = SEASON_START_WORLD_DAY + SUMMER_WINDOW_LENGTH_DAYS }

IsWindowOpen(in TransferWindow w, uint worldDay):
    return w.OpenWorldDay <= worldDay AND worldDay <= w.CloseWorldDay
```

The window cursor (`ActiveWindow`) is season state (serialized, reset at the season boundary). #30 owns the
calendar; #31 derives the window from it and never mutates it.

## 3.6 Worked example (behaviour-neutral minimal)

Season start, no manager action: no `SubmitBid` means no transaction, roster commit or contract insertion, so
the season remains byte-identical to pre-#31 (FR-TX-024). With a manager action, suppose §3.1 resolves the
counterparty value to `100,000` and the configured counter band to `5%`: a buy at `100,000` accepts; `99,999`
through `95,000` returns `CounterOffered`; `94,999` rejects. If the accepted `100,000` offer exceeds remaining
transfer headroom, `SubmitBid` instead returns `InsufficientBudget`; if the destination is full, it returns
`SquadFull`. All non-accepted command results leave local finance/transfer state untouched.

## 3.7 Contract aging at the season boundary (FR-TX-028)

`RollToNextSeason` ages the managed club's contracts (durable career state survives the roll):

```
AgeContractsAtBoundary(ref TransfersState txState):
    expired := []
    for each playerId in txState.ManagedContractIds:
        newLen := txState.ContractOf(playerId).LengthSeasons - 1
        if newLen <= 0:  expired.Add(playerId)
        else:            txState.SetContractLength(playerId, newLen)
    for each playerId in expired: txState.RemoveContract(playerId)
    txState.ResetWindow(); txState.ResetCommittedSpend()
```

An expired contract is removed; the player becomes un-contracted. Free-agency, auto-renewal and warnings remain
deep-tier. `ResetWindow()` re-derives the new window from #30's calendar rather than zeroing the cursor.

## 3.8 Initial contract population (career start)

At **new-career genesis only** (never on load) the managed club's #27 squad is seeded with one contract per
rostered player:

```
SeedInitialContracts(managerClubId, in Squad squad, ref TransfersState txState):
    for each playerId in squad.PlayerIds:
        txState.InsertContract(Contract{ PlayerId = playerId,
                                         WagePerPeriod = DefaultWageFor(squad, playerId),
                                         LengthSeasons = DEFAULT_CONTRACT_SEASONS })
```

`DefaultWageFor` / `DEFAULT_CONTRACT_SEASONS` are `[GT]`; every seeded contract is F6/F7-valid. AI clubs are
not seeded at minimal. A load reconstructs contracts from the transfers sub-blob and MUST NOT re-seed.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 algorithms + worked example; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: explicit buy/sell direction and #30 roster re-key seam. |
| 0.3 | 2026-07-23 | — | AR-3: minimal fee-only finance, contract handling, aging and seeding. |
| 0.4 | 2026-07-23 | — | AR-6: seeding is new-career genesis only. |
| 0.5 | 2026-07-23 | — | AR-8: static-ceiling consequence and boundary reset clarified. |
| 0.6 | 2026-09-14 | — | T0 football-judgment close-out: always-on #27 positional scarcity, deterministic counter band, evaluator validation and no-mutation negotiation results. |
| 0.7 | 2026-09-14 | — | PR #407 review correction: currency APIs renamed; attribute mean no longer truncates to 20 buckets; positional stock excludes the negotiated player symmetrically; `SubmitBid` gains typed budget/full outcomes and applies local state only after matching roster commit. |
| 0.8 | 2026-09-14 | — | Residual review cleanup: budget subtraction now explicitly requires `0 <= committed <= budget`; preview/commit mismatch limits are stated for both directions; T2 must lock real hook dispatch plus observable managed↔external #31 no-op behavior. |
| 0.9 | 2026-09-21 | — | ERR-031-002: move the ordinary buy-affordability decision ahead of checked staged `ApplyTransaction`, preventing coherent extreme debt from throwing before `InsufficientBudget`; runtime regression lands with the spec. |
#endregion