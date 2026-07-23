# Transfers, Contracts & Negotiation #31 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.4 — AR-6 fix pass; prior v0.3 AR-3/AR-4, v0.2 AR-1, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

All arithmetic is **integer** (currency `long`; valuation/club-need/personality per-mille `int`). No stochastic
draw occurs at the minimal tier (FR-TX-001/016). `PERMILLE_DENOM = 1000`.

## 3.1 Player valuation — `ValuePlayerPermille` (FR-TX-001/002)

The Stage-2 counterparty valuation is a pure deterministic integer function over #27's canonical record —
**attributes + age only** (FR-TX-001). It is the **identity** the deep tier modulates (KD-1):

```
ValuePlayerPermille(in PlayerAttributes attrs, int age):    # minimal: attributes + age ONLY (FR-TX-001)
    rating   := MeanAttributeRating(attrs)                 # int [1,20] mean, the LineupSelector precedent
    base     := rating * VALUE_PER_RATING_POINT            # integer currency, monotone in rating
    ageMult  := AgeCurvePermille(age)                      # [0,1000+]; peak band ~1000, decline past ~30, young discount
    value    := base * ageMult / PERMILLE_DENOM            # integer, deterministic
    return value                                           # NO club-need, NO #33 read, NO #28 CA read (FR-TX-001)
```

`counterpartyView(playerId, clubId)` is the helper that resolves the counterparty's valuation inputs: it looks
up the `PlayerRecord` for `playerId` in #27 and returns `ValuePlayerPermille(record.Attributes, record.Age)`.
At minimal it takes no `clubId`-derived need term; `clubId` is threaded only so the deep tier can attach that
club's need signal without a signature change.

- `MeanAttributeRating` is the integer mean of the consumed #27 `[1,20]` attribute fields (position-weighted
  in a deep refinement; unweighted mean at minimal). `AgeCurvePermille` is a fixed `[GT]` table (Appendix A):
  a neutral peak band, a decline multiplier past ~30, and a discount for the very young (unproven), the master
  plan §4.3 shape — its exact magnitudes are illustrative pending a Stage-2/3 balance pass (#21 G2 precedent).
- **Deep tier** multiplies additional identity-`1000‰` terms and optionally swaps `rating` for #28's CA:
  `value_deep := value * needMult/1000 * personalityMult/1000`, where `needMult` is the **valuing** club's
  position scarcity (seller on a buy, buyer on a sell) and `personalityMult` is from #33 traits (a
  loyal/ambitious seller holds out for more). With `deepTransfersEnabled` off, `needMult ≡ personalityMult ≡
  1000` and `rating` is the #27 mean ⇒ `value_deep == value` **exactly** (FR-TX-002). No replacement path —
  the deep tier only *scales* the identity.

## 3.2 Offer evaluation — `EvaluateOffer` (FR-TX-003)

```
EvaluateOffer(in Offer offer, long counterpartyValuation):     # draw-free at minimal (FR-TX-003)
    # the counterparty (selling club on a buy, buying club on a sell) accepts iff the fee clears its valuation
    if offer.IsBuy:  return offer.Fee >= counterpartyValuation ? Accepted : Rejected
    else:            return offer.Fee <= counterpartyValuation ? Accepted : Rejected   # manager sells; buyer pays <= its value
```

`counterpartyValuation` is `counterpartyView(offer.PlayerId, offer.CounterpartyClubId)` — the counterparty's
attributes+age valuation at minimal (the deep tier attaches its need + personality). The minimal resolution is
synchronous (no `CounterOffered` — that is the deep-tier multi-day path). Because both inputs are deterministic
integers, the same offer against the same world state always yields the same outcome.

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
    cv := counterpartyView(offer.PlayerId, offer.CounterpartyClubId)                 # the counterparty's attributes+age valuation (§3.1)
    if EvaluateOffer(offer, cv) != Accepted:  return Rejected                        # no mutation, not a failure
    if offer.IsBuy:
        require offer.Fee <= AvailableTransferBudget(finances)
                            - txState.CommittedSpend(managerClubId)                  else throw   # F1
        require DestinationSquadHasFreeSlot(toClub)                                  else throw   # F5
    else:
        require DestinationSquadHasFreeSlot(toClub)                                  else throw   # F5 (buyer squad)
    # ---- COMMIT (atomic block; all gates passed). MINIMAL = fee-only (FR-TX-005); wage posts are deep. ----
    if offer.IsBuy:
        ApplyTransaction(ref finances, {Debit,  TransferFee, offer.Fee})             # Balance -= (fee only)
        txState.AddCommittedSpend(managerClubId, offer.Fee)
        newId := RequestRosterCommit(fromClub, toClub, offer.PlayerId)               # #30 seam, re-keys (KD-7);
                                                                                     #   OnPlayerRekeyed is a no-op for #31 (no old managed contract) — FR-TX-023
        txState.InsertContract(ContractFrom(offer, newId))                           # managed-club contract (wage recorded, NOT posted — FR-TX-005)
    else:  # SELL — managed club receives the fee; the departing player leaves the managed squad
        txState.RemoveContract(offer.PlayerId)                                       # remove BEFORE the re-key so OnPlayerRekeyed
                                                                                     #   has no managed contract to move (FR-TX-023) — no double-handle
        ApplyTransaction(ref finances, {Credit, TransferFee, offer.Fee})             # Balance += (fee only)
        RequestRosterCommit(fromClub, toClub, offer.PlayerId)                        # re-keys into the buyer's squad (the AI buyer's contract is untracked at minimal)
    return Accepted
    # DEEP adds the PlayerWage posts (buy {Debit,PlayerWage,inWage} / sell {Credit,PlayerWage,outWage}, capturing
    #   outWage before RemoveContract) + a WageBudget affordability gate, behind deepTransfersEnabled (§7).
```

The finance posts and the roster commit are one logical transaction: because every gate cleared first
(including the destination free-slot check, F5), no individual step can fail mid-commit (`ApplyTransaction`
magnitudes are pre-validated; a `Credit` cannot fail on affordability), so the club is never debited for a
player it does not receive, and the sell's `RemoveContract` (before the infallible re-key) never strands a
half-removed contract.

## 3.4 The #30 boundary — the roster-commit re-key (KD-7)

`RequestRosterCommit(fromClubId, toClubId, playerId)` is a **genuinely new #30-owned mid-season entry point**
(§4, §8). #30 owns **no** per-player roster mutation today — its mid-season tick is all null seams + the
world-day advance, and even the season-boundary roll's roster step (`AdvanceAges`) is itself an inert null
seam; #27 owns `Squad`/`PlayerRecord`. So this is new capability #30 grows, **orchestrating** a #27 `Squad`
move (not an extension of boundary churn, which does not exist). #31 declares the seam contract; #30 builds it
at T2 (ERR-030-005). It:

```
RequestRosterCommit(fromClubId, toClubId, playerId):        # #30-owned orchestration of a #27 Squad move; #31 calls, does not implement
    freeLocal := AllocateFreeLocalIndex(toClubId)           # F5 fail-loud if the destination Squad is full
    newId     := toClubId * CLUB_SQUAD_SIZE + freeLocal      # the #27 club-scoped id formula (re-key)
    MovePlayerRecord(fromClubId, playerId, toClubId, newId)  # the #27-owned Squad move #30 drives
    DispatchRosterMoveHook(playerId, newId)                 # each per-PlayerId system migrates its own state
    return newId
```

`DispatchRosterMoveHook` calls each subscriber: **#31.`OnPlayerRekeyed(oldId, newId)`**, #28's CA/PA
migration, #33's morale migration. #28/#33 always **move** their state old→new (it follows the player across
clubs). #31's `OnPlayerRekeyed` moves its `Contract` old→new **only** for an intra-managed-club re-key (both
ids in the managed club) — **not reached at minimal**, where every transfer is managed↔AI and `SubmitBid` has
already Inserted (buy) or Removed (sell) the managed contract, so the hook is a **no-op** for #31 (FR-TX-023 —
this is what prevents the hook and the sell's `RemoveContract` from double-handling the same contract). #31
migrates **only** its own `Contract`; it never touches #28/#33 state.

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
When the manager *does* `SubmitBid` a fair-value buy inside the window, exactly **one** `ApplyTransaction` post
(the transfer fee — wages are deep, FR-TX-005) + one re-keyed `Contract` land, deterministically and
atomically. This is the KD-8 identity the deep tier modulates.

## 3.7 Contract aging at the season boundary (FR-TX-028)

`RollToNextSeason` ages the managed club's contracts (durable career state survives the roll):

```
AgeContractsAtBoundary(ref TransfersState txState):        # invoked from #30's RollToNextSeason
    expired := []
    for each playerId in txState.ManagedContractIds:       # iterate KEYS — no foreach-over-struct copy
        newLen := txState.ContractOf(playerId).LengthSeasons - 1
        if newLen <= 0:  expired.Add(playerId)             # would reach 0 ⇒ EXPIRED (never stored as 0)
        else:            txState.SetContractLength(playerId, newLen)   # write the decrement back to the store
    for each playerId in expired:  txState.RemoveContract(playerId)    # remove AFTER iterating (no modify-during-foreach)
    txState.ResetWindow(); txState.ResetCommittedSpend()   # season-scoped state resets (FR-TX-007/028)
```

An expired contract is **removed** — the player becomes un-contracted; at minimal it simply leaves #31's
tracking (roster membership stays #27/#28-owned — an un-contracted player is not auto-removed from the squad).
Free-agency, auto-renewal, and expiry-warning flows are deep-tier (§7). Because a contract is removed the moment
it *would* decrement to `0`, a stored `LengthSeasons` is always `> 0`, so the F7 zero-value-trap gate
(`LengthSeasons = 0` is invalid) never collides with a legitimately-aged contract.

## 3.8 Initial contract population (career start)

At **new-career genesis** (the T0 construction of a fresh career — *not* a load, see below) the managed club's
#27 squad is seeded with **one `Contract` per rostered player**, so every
managed player has a contract the sell path (§3.3) and boundary aging (§3.7) can operate on — a career never
starts with an un-contracted squad, and FR-TX-028's "durable career state" has an initial set to be durable
across:

```
SeedInitialContracts(managerClubId, in Squad squad, ref TransfersState txState):   # #31-owned; reads #27 read-only
    for each playerId in squad.PlayerIds:              # the #27 club-scoped ids
        txState.InsertContract(Contract{ PlayerId = playerId,
                                         WagePerPeriod = DefaultWageFor(squad, playerId),  # [GT], Appendix A
                                         LengthSeasons = DEFAULT_CONTRACT_SEASONS })         # [GT] > 0 (F7-valid)
```

`DefaultWageFor` / `DEFAULT_CONTRACT_SEASONS` are `[GT]` (balance-pass-pinned); each seeded contract satisfies
F6/F7 (`WagePerPeriod ≥ 0`, `LengthSeasons > 0`). **AI clubs are not seeded** (they hold no #31 contract state
at minimal, §2.2). Seeding mutates only `TransfersState` and is **not read by the sim** at minimal (no wage
posting, no autonomous producer), so it does not perturb the byte-identical season advance (FR-TX-024) — it
only populates the transfers sub-blob that exists because #31 exists.

**Seeding runs once, at new-career genesis ONLY.** A load-from-save reconstructs `TransfersState` from the
transfers sub-blob (§4.4, F3-gated) and MUST NOT re-seed — re-seeding a loaded career would collide with the
already-present ids (`InsertContract` throws F7-style) or overwrite the restored, aged/traded contracts, silently
destroying career progress. The composition root invokes `SeedInitialContracts` at career creation and the
sub-blob decode on load — **never both** (§4.5). This is why the T-TX-DET-001 / FR-TX-027 round-trip is
field-identical: the restored contracts come from the sub-blob, not from a re-run of the seeder.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 (valuation, offer evaluation, atomic bid pipeline, the #30 roster re-key, the window model, worked example). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: `SubmitBid` pipeline resolves `fromClub`/`toClub` from the explicit `Offer.CounterpartyClubId` + cross-checks `ClubOf(playerId) == fromClub` (M1); sell branch defines `outgoingWage` + buyer free-slot gate + managed-club-only contract scope (M2/L1). |
| 0.3 | 2026-07-23 | — | AR-3: commit is fee-only at minimal, wage posts deep (H); sell `RemoveContract` moved BEFORE `RequestRosterCommit` + §3.4 hook made direction-aware/no-op (sell double-handle — M); §3.1 drops club-need to the deep bias + defines `counterpartyView` (M); new §3.7 decrement-and-remove contract aging (F7 — M); §3.4 corrects the "#30 churns rosters" claim + fixes the T2 build cite to ERR-030-005 (L); §3.6 fee-post count corrected to one. AR-4: fixed the `counterpartyView` double-application in §3.3 (regression from §3.1's redefinition), made §3.7 aging struct-safe (iterate keys, remove after the loop), and added §3.8 career-start contract seeding (M — the sell/aging flows previously had no initial contract set). |
| 0.4 | 2026-07-23 | — | AR-6 (M): §3.8 scopes seeding to **new-career genesis only** — a load reconstructs from the sub-blob and must not re-seed (a re-seed would overwrite/collide with the restored career; the AR-4 §3.8 addition had left this undefined). |
#endregion
