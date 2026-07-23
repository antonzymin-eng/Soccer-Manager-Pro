# Transfers, Contracts & Negotiation #31 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

All arithmetic is **integer** (currency `long`; valuation/club-need/personality per-mille `int`). No stochastic
draw occurs at the minimal tier (FR-TX-001/016). `PERMILLE_DENOM = 1000`.

## 3.1 Player valuation — `ValuePlayerPermille` (FR-TX-001/002)

The Stage-2 counterparty valuation is a pure deterministic integer function over #27's canonical record. It is
the **identity** the deep tier modulates (KD-1):

```
ValuePlayerPermille(in PlayerAttributes attrs, int age, int clubNeedPermille):
    rating   := MeanAttributeRating(attrs)                 # int [1,20] mean, the LineupSelector precedent
    base     := rating * VALUE_PER_RATING_POINT            # integer currency, monotone in rating
    ageMult  := AgeCurvePermille(age)                      # [0,1000+]; peak band ~1000, decline past ~30, young discount
    needMult := clampPermille(clubNeedPermille)            # the VALUING club's position scarcity (seller on a buy,
                                                           #   buyer on a sell); 1000 = neutral need
    value    := (base * ageMult / PERMILLE_DENOM) * needMult / PERMILLE_DENOM      # integer, deterministic
    return value                                           # NO #33 read, NO #28 CA read at minimal (FR-TX-001)
```

- `MeanAttributeRating` is the integer mean of the consumed #27 `[1,20]` attribute fields (position-weighted
  in a deep refinement; unweighted mean at minimal). `AgeCurvePermille` is a fixed `[GT]` table (Appendix A):
  a neutral peak band, a decline multiplier past ~30, and a discount for the very young (unproven), the master
  plan §4.3 shape — its exact magnitudes are illustrative pending a Stage-2/3 balance pass (#21 G2 precedent).
- **Deep tier** multiplies one more term, `personalityMult` (from #33 traits — a loyal/ambitious seller holds
  out for more) and optionally swaps `rating` for #28's CA: `value_deep := value * personalityMult / 1000`.
  With `deepTransfersEnabled` off, `personalityMult ≡ 1000` and `rating` is the #27 mean ⇒ `value_deep ==
  value` **exactly** (FR-TX-002). No replacement path — the deep tier only *scales* the identity.

## 3.2 Offer evaluation — `EvaluateOffer` (FR-TX-003)

```
EvaluateOffer(in Offer offer, long counterpartyValuation):     # draw-free at minimal (FR-TX-003)
    # the counterparty (selling club on a buy, buying club on a sell) accepts iff the fee clears its valuation
    if offer.IsBuy:  return offer.Fee >= counterpartyValuation ? Accepted : Rejected
    else:            return offer.Fee <= counterpartyValuation ? Accepted : Rejected   # manager sells; buyer pays <= its value
```

`counterpartyValuation` is `ValuePlayerPermille(...)` for the counterparty's view (its need signal). The
minimal resolution is synchronous (no `CounterOffered` — that is the deep-tier multi-day path). Because both
inputs are deterministic integers, the same offer against the same world state always yields the same outcome.

## 3.3 The bid pipeline — `SubmitBid` (FR-TX-009/025, atomic)

Invoked by the manager command (never autonomously at minimal). **Validate every gate before any mutation**
(F2 — no half-written deal):

The counterparty club is explicit in the `Offer` (`CounterpartyClubId`): on a **buy** it is the player's
owning (selling) club (which MUST equal the player's current club, cross-checked); on a **sell** it is the
manager-named target buyer (no autonomous AI selects one at minimal). `fromClub`/`toClub` derive from the
direction.

```
SubmitBid(managerClubId, in Offer offer, worldDay, ref ClubFinances finances, ref TransfersState txState):
    (fromClub, toClub) := offer.IsBuy ? (offer.CounterpartyClubId, managerClubId)    # buy: from seller to us
                                      : (managerClubId, offer.CounterpartyClubId)     # sell: from us to buyer
    # ---- VALIDATE-ALL-FIRST (no mutation) ----
    require IsWindowOpen(txState.WindowFor(managerClubId), worldDay)                 else throw   # F4
    require PlayerInClubUniverse(offer.PlayerId) AND ClubOf(offer.PlayerId) == fromClub  else throw   # F6
    require offer well-formed (Fee >= 0, WagePerPeriod >= 0, LengthSeasons > 0)      else throw   # F6
    cv := ValuePlayerPermille(counterpartyView(offer.PlayerId, offer.CounterpartyClubId))
    if EvaluateOffer(offer, cv) != Accepted:  return Rejected                        # no mutation, not a failure
    if offer.IsBuy:
        require offer.Fee <= AvailableTransferBudget(finances)
                            - txState.CommittedSpend(managerClubId)                  else throw   # F1
        require DestinationSquadHasFreeSlot(toClub)                                  else throw   # F5
    else:
        require DestinationSquadHasFreeSlot(toClub)                                  else throw   # F5 (buyer squad)
    # ---- COMMIT (atomic block; all gates passed) ----
    if offer.IsBuy:
        ApplyTransaction(ref finances, {Debit,  TransferFee, offer.Fee})             # Balance -=
        ApplyTransaction(ref finances, {Debit,  PlayerWage,  offer.WagePerPeriod})   # WageBillAggregate += (FR-FN-016)
        txState.AddCommittedSpend(managerClubId, offer.Fee)
        newId := RequestRosterCommit(fromClub, toClub, offer.PlayerId)               # #30 seam, re-keys (KD-7)
        txState.InsertContract(ContractFrom(offer, newId))                           # managed-club contract
    else:  # SELL — managed club receives the fee and sheds the departing wage
        outgoingWage := txState.ContractOf(offer.PlayerId).WagePerPeriod             # the departing contract's wage
        ApplyTransaction(ref finances, {Credit, TransferFee, offer.Fee})             # Balance +=
        ApplyTransaction(ref finances, {Credit, PlayerWage,  outgoingWage})          # WageBillAggregate -= (FR-FN-016)
        RequestRosterCommit(fromClub, toClub, offer.PlayerId)                        # re-keys into the buyer's squad
        txState.RemoveContract(offer.PlayerId)                                       # #31 tracks the managed club only;
                                                                                     #   the AI buyer's contract is untracked at minimal
    return Accepted
```

The finance posts and the roster commit are one logical transaction: because every gate cleared first
(including the destination free-slot check), no individual step can fail mid-commit (`ApplyTransaction`
magnitudes are pre-validated), so the club is never debited for a player it does not receive.

## 3.4 The #30 boundary — the roster-commit re-key (KD-7)

`RequestRosterCommit(fromClubId, toClubId, playerId)` is a **new #30-owned mid-season entry point** (§4, §8;
today #30 churns rosters only at `RollToNextSeason`). It:

```
RequestRosterCommit(fromClubId, toClubId, playerId):        # #30-owned; #31 calls, does not implement
    freeLocal := AllocateFreeLocalIndex(toClubId)           # F5 fail-loud if the destination Squad is full
    newId     := toClubId * CLUB_SQUAD_SIZE + freeLocal      # the #27 club-scoped id formula (re-key)
    MovePlayerRecord(fromClubId, playerId, toClubId, newId)  # #27 Squad move (owned by #30's roster owner)
    DispatchRosterMoveHook(playerId, newId)                 # each per-PlayerId system migrates its own state
    return newId
```

`DispatchRosterMoveHook` calls each subscriber: **#31.`OnPlayerRekeyed(oldId, newId)`** (moves the `Contract`,
FR-TX-023), #28's CA/PA migration, #33's morale migration. #31 migrates **only** its own `Contract`; it never
touches #28/#33 state. This generalizes nothing — it is a genuinely new mid-season capability #30 grows (the
season-boundary regen/retire hook is a *different* entry point); #31 declares the seam contract, #30 builds it
at T2 (ERR-030-004).

## 3.5 The transfer window — `IsWindowOpen` (FR-TX-019/020)

```
DeriveSummerWindow(in SeasonCalendar cal):                 # #31-owned; reads #30's calendar read-only
    return TransferWindow{ OpenWorldDay  = SEASON_START_WORLD_DAY,
                           CloseWorldDay = SEASON_START_WORLD_DAY + SUMMER_WINDOW_LENGTH_DAYS }

IsWindowOpen(in TransferWindow w, uint worldDay):
    return w.OpenWorldDay <= worldDay AND worldDay <= w.CloseWorldDay
```

The window cursor (`ActiveWindow`) is season state (serialized, reset at the season boundary). #30 owns the
calendar; #31 derives the window from it and never mutates it.

## 3.6 Worked example (behaviour-neutral minimal)

Season start, no manager action: `RunWorldTickInFixedOrder` reaches the new transfers slot, which — at minimal
— has no daily work (window open/close is a predicate evaluated at command time), so it is a null seam. No
`SubmitBid` is issued ⇒ no `ApplyTransaction`, no roster commit, no `Contract` inserted ⇒ the season is
byte-identical to pre-#31 (FR-TX-024, T-TX-NEU-001). A save→restore here is field-identical (T-TX-DET-001).
When the manager *does* `SubmitBid` a fair-value buy inside the window, exactly two `ApplyTransaction` posts +
one re-keyed `Contract` land, deterministically and atomically. This is the KD-8 identity the deep tier
modulates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 (valuation, offer evaluation, atomic bid pipeline, the #30 roster re-key, the window model, worked example). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: `SubmitBid` pipeline resolves `fromClub`/`toClub` from the explicit `Offer.CounterpartyClubId` + cross-checks `ClubOf(playerId) == fromClub` (M1); sell branch defines `outgoingWage` + buyer free-slot gate + managed-club-only contract scope (M2/L1). |
#endregion
