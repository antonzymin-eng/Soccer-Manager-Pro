# Transfers, Contracts & Negotiation #31 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.6 — T0 football-judgment close-out: positional need + deterministic counter-offer band)
**Last Updated (prior):** July 23, 2026 (v0.5 — AR-8 doc; prior v0.4 AR-6, v0.3 AR-3/AR-4, v0.2 AR-1, v0.1 initial)
**Version:** 0.6
**Status:** APPROVED

---

All arithmetic is **integer** (currency `long`; valuation/club-need/personality per-mille `int`). No stochastic
draw occurs at the minimal tier (FR-TX-001/016). `PERMILLE_DENOM = 1000`.

## 3.1 Player valuation — `ValuePlayerPermille` / counterparty view (FR-TX-001/002)

Stage 2 keeps the pure #27 **attributes + age** valuation as the reusable identity, then applies one always-on
situational input: the **valuing club's positional stock**. This discharges the football-judgment finding that
the shipped baseline otherwise knows nothing about the counterparty's squad until an optional deep system is
enabled. It stays draw-free and uses only #27 data:

```
ValuePlayerPermille(in PlayerAttributes attrs, int age):
    rating   := MeanAttributeRating(attrs)                 # int [1,20] mean, the LineupSelector precedent
    base     := rating * VALUE_PER_RATING_POINT            # integer currency, monotone in rating
    ageMult  := AgeCurvePermille(age)                      # peak ~1000; older decline; young discount
    return base * ageMult / PERMILLE_DENOM                 # pure identity: attrs + age only

ClubNeedMultiplierPermille(int samePositionCount):
    neutral  := CLUB_SQUAD_SIZE / POSITION_COUNT           # #27-derived neutral coarse-position stock
    delta    := neutral - samePositionCount                 # scarce => positive; overstocked => negative
    return 1000 + delta * CLUB_NEED_PER_PLAYER_PERMILLE     # configured so result remains > 0

counterpartyView(playerId, clubId):
    p        := #27 PlayerRecord(playerId)
    stock    := #27 CountPlayersAtPosition(clubId, p.Position)
    identity := ValuePlayerPermille(p.Attributes, p.Age)
    return identity * ClubNeedMultiplierPermille(stock) / 1000
```

The `clubId` is the **valuing** club: seller on a manager buy, buyer on a manager sell. That gives the same
player a higher counterparty value where his coarse position is scarce and a lower value where it is
oversupplied, without inventing a new position model or using #33/#28.

- `MeanAttributeRating` is the integer mean of the consumed #27 `[1,20]` fields; WeakFoot remains excluded.
  `AgeCurvePermille` is the configured Stage-2 shape in Appendix A.
- **Deep tier** may multiply the Stage-2 counterparty value by personality/staff terms and optionally swap the
  rating identity for #28 CA. Those additions default to identity when their producers are absent, but the
  Stage-2 positional-stock term is **not** disabled with them. The deep path refines the same value; it does
  not replace it.

## 3.2 Offer evaluation — `EvaluateOffer` (FR-TX-003)

The original exact-value cliff (one currency unit below value = certain reject; at value = certain accept) is
not football-like enough to ship even at minimal. T0 therefore has a deterministic **counter-offer band**.
There is still no RNG and no multi-day negotiation state:

```
EvaluateOffer(in Offer offer, long counterpartyValuation):
    validate ALL Offer terms (Fee >= 0, WagePerPeriod >= 0, LengthSeasons > 0, ids non-negative) else throw
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

`counterpartyValuation` is caller-supplied, so the KD-3 reuse contract remains intact. At #31 T0 the caller
uses §3.1's `counterpartyView`. `CounterOffered` here means only **"close enough to keep negotiation open"**;
it causes no mutation and stores no in-flight state. The deep multi-day state machine may later attach terms and
delay to that same enum outcome. Because the band, value and offer are deterministic integers, the same inputs
always produce the same outcome.

## 3.3 The bid pipeline — `SubmitBid` (FR-TX-009/025, atomic)

Invoked by the manager command (never autonomously at minimal). **Validate every gate before any mutation**
(F2 — no half-written deal):

The counterparty club is explicit in the `Offer` (`CounterpartyClubId`): on a **buy** it is the player's
owning (selling) club (which MUST equal the player's current club, cross-checked); on a **sell** it is the
manager-named target buyer (no autonomous AI selects one at minimal). `fromClub`/`toClub` derive from the
direction.

```
SubmitBid(managerClubId, in Offer offer, worldDay, ref ClubFinances finances, ref TransfersState txState):
    (fromClub, toClub) := offer.IsBuy ? (offer.CounterpartyClubId, managerClubId)
                                      : (managerClubId, offer.CounterpartyClubId)
    # ---- VALIDATE-ALL-FIRST (no mutation) ----
    require offer well-formed (ids >= 0, Fee >= 0, WagePerPeriod >= 0, LengthSeasons > 0) else throw  # F6
    require IsWindowOpen(txState.WindowFor(managerClubId), worldDay)                                  else throw  # F4
    require PlayerInClubUniverse(offer.PlayerId) AND ClubOf(offer.PlayerId) == fromClub               else throw  # F6
    cv := counterpartyView(offer.PlayerId, offer.CounterpartyClubId)                                  # §3.1
    outcome := EvaluateOffer(offer, cv)
    if outcome != Accepted: return outcome                                                             # no mutation
    if offer.IsBuy:
        require offer.Fee <= AvailableTransferBudget(finances)
                            - txState.CommittedSpend(managerClubId)                                   else throw  # F1
        require DestinationSquadHasFreeSlot(toClub)                                                   else throw  # F5
    else:
        require DestinationSquadHasFreeSlot(toClub)                                                   else throw  # F5
    # ---- COMMIT (atomic block; all gates passed). MINIMAL = fee-only (FR-TX-005). ----
    if offer.IsBuy:
        ApplyTransaction(ref finances, {Debit, TransferFee, offer.Fee})
        txState.AddCommittedSpend(managerClubId, offer.Fee)
        newId := RequestRosterCommit(fromClub, toClub, offer.PlayerId)
        txState.InsertContract(ContractFrom(offer, newId))
    else:
        txState.RemoveContract(offer.PlayerId)               # before re-key; FR-TX-023
        ApplyTransaction(ref finances, {Credit, TransferFee, offer.Fee})
        RequestRosterCommit(fromClub, toClub, offer.PlayerId)
    return Accepted
```

The finance posts and roster commit are one logical transaction: because every fallible gate clears first,
accepted execution cannot fail halfway under the specified #30 preview/commit contract. `Rejected` and
`CounterOffered` are ordinary no-mutation outcomes.

**Static-ceiling consequence (KD-2).** A sell posts a `Credit` to #40's `Balance` but does **not** touch
`committedSpendThisWindow` (a buy-side accumulator) and does **not** raise `AvailableTransferBudget` (the
`TransferBudget` ceiling is `SettleFinances`-only, FR-FN-003/004). Sell proceeds therefore do not increase
in-window buy headroom at minimal.

## 3.4 The #30 boundary — the roster-commit re-key (KD-7)

`RequestRosterCommit(fromClubId, toClubId, playerId)` is a genuinely new #30-owned mid-season entry point.
#31 declares the consumer contract; #30 builds it at T2 (ERR-030-005). T0 additionally needs one read-only
roster query for §3.1: `CountPlayersAtPosition(clubId, PlayerPosition)`. It reads #27 state only and performs no
allocation or mutation.

```
RequestRosterCommit(fromClubId, toClubId, playerId):
    freeLocal := AllocateFreeLocalIndex(toClubId)
    newId     := toClubId * CLUB_SQUAD_SIZE + freeLocal
    MovePlayerRecord(fromClubId, playerId, toClubId, newId)
    DispatchRosterMoveHook(playerId, newId)
    return newId
```

`DispatchRosterMoveHook` calls each subscriber: #31 `OnPlayerRekeyed`, #28 CA/PA migration, and #33 morale
migration. #31 moves only its own `Contract` and is a no-op for managed↔external minimal transfers because the
buy/sell command already creates/removes that managed-club contract at the correct side of the re-key.

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

Season start, no manager action: `RunWorldTickInFixedOrder` reaches the transfers slot and does no daily work.
No `SubmitBid` ⇒ no transaction, roster commit or contract insertion, so the season remains byte-identical to
pre-#31 (FR-TX-024). With a manager action, suppose §3.1 resolves the counterparty value to `100,000` and the
configured counter band to `5%`: a buy at `100,000` accepts; `99,999` through `95,000` returns
`CounterOffered`; `94,999` rejects. Only the accepted path posts one transfer-fee transaction and one roster
re-key. All three outcomes are draw-free and deterministic.

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
not seeded at minimal. A load reconstructs the contracts from the transfers sub-blob and MUST NOT re-seed.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 algorithms + worked example; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: §3.3 sell/buy from→to derivation made explicit; buyer on sell is named counterparty; §3.4 roster re-key seam clarified as genuinely new #30 capability. |
| 0.3 | 2026-07-23 | — | AR-3: §3.3 minimal fee-only finance posts; sell removes contract before re-key; §3.1 drops minimal club-need and moves it to deep; §3.7 contract aging decrement/removal; §3.8 initial contract seeding. |
| 0.4 | 2026-07-23 | — | AR-6: §3.8 seeding is new-career genesis only; load decodes sub-blob, never reseeds. |
| 0.5 | 2026-07-23 | — | AR-8 doc: §3.3 static-ceiling consequence spelled out; §3.7 reset re-derives from calendar. |
| 0.6 | 2026-09-14 | — | T0 football-judgment close-out: Stage-2 counterparty view gains always-on #27 positional scarcity; `EvaluateOffer` gains a deterministic synchronous counter-offer band; malformed terms are validated at the reusable evaluator; §3.3 records no-mutation counter/reject outcomes; §3.4 adds the read-only positional-stock query. |
#endregion
