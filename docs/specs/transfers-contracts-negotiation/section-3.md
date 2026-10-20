# Transfers, Contracts & Negotiation #31 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.12 — T2: §3.3 ERR-031-007/009/010, §3.5 ERR-031-008, §3.7 the staged boundary)
**Last Updated (prior):** October 20, 2026 (v0.11 — ERR-031-006: §3.4 test-id reference corrected to T-TX-REKEY-005)
**Last Updated (prior):** October 19, 2026 (v0.10 — ERR-031-005: §3.3/§3.4 re-home under a stable id)
**Last Updated (prior):** September 21, 2026 (v0.9 — ERR-031-002 affordability-before-staging correction)
**Last Updated (prior):** September 14, 2026 (v0.8 — PR #407 residual review cleanup: budget invariant + T2 hook lock)
**Last Updated (prior):** September 14, 2026 (v0.7 — PR #407 review correction: precise valuation, symmetric need, typed submission outcomes; prior v0.6 T0 football close-out, v0.5 AR-8, v0.4 AR-6, v0.3 AR-3/AR-4, v0.2 AR-1, v0.1 initial)
**Version:** 0.12
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
    require CarriesClub(offer.CounterpartyClubId)                                   else throw  # F6, ERR-031-010
    require PlayerInClubUniverse(offer.PlayerId) AND ClubOf(offer.PlayerId)==fromClub else throw
        # ClubOf is the roster port's answer, never offer.PlayerId / CLUB_SQUAD_SIZE (ERR-031-005)

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
    else if managed Contract(offer.PlayerId) exists:
        require it is valid                                  else throw
        # ERR-031-009: a sale no longer REQUIRES a contract — §3.7 leaves an expired player un-contracted

    if !CanRelease(fromClub, offer.PlayerId):
        return SquadDepleted                                 # F9 / FR-TX-029, ERR-031-007; no mutation

    txn := offer.IsBuy ? {Debit,TransferFee,offer.Fee} : {Credit,TransferFee,offer.Fee}
    stagedFinances := finances
    ApplyTransaction(ref stagedFinances, txn)               # canonical #40 mutation on staged copy

    if !TryPreviewRosterCommit(fromClub, toClub, offer.PlayerId):
        return SquadFull                                     # F5; no mutation

    require no managed contract already at offer.PlayerId   else throw  # on a buy

    # ---- COMMIT ---- (ERR-031-005: a stable-id re-home)
    RequestRosterCommit(fromClub, toClub, offer.PlayerId)
    require ClubOf(offer.PlayerId) == toClub (via the port) else throw  # producer contract breach

    finances := stagedFinances
    if offer.IsBuy:
        txState.AddCommittedSpend(managerClubId, offer.Fee)
        txState.InsertContract(ContractFrom(offer, offer.PlayerId))
    else if managed Contract(offer.PlayerId) exists:
        txState.RemoveContract(offer.PlayerId)

    return Accepted
```

The buy affordability result is resolved **before** staged #40 mutation validation. This ordering is normative:
a coherent signed `Balance` may be near `long.MinValue`, and checked debit staging must not throw before the
player-reachable `InsufficientBudget` result (`ERR-031-002`). All local #31/#40 writes occur only **after**
the preflighted roster commit is confirmed to have landed the player on the destination roster.
That removes the previous defensive check-after-mutation ordering. Because the commit keeps the player's id and
nothing re-keys (ERR-031-005), a sell may retain its old managed contract during the roster commit and remove it
immediately afterward without double-handling.

A commit that does not land (originally: a preview/commit id mismatch) is an integration defect after the external port has already mutated. T0 therefore
can guarantee only that local #31/#40 state is untouched; it cannot roll the producer back. The regression suite
locks both directions: on a buy no contract/spend/finance is applied, and on a sell the old managed contract and
finance remain. T2's production adapter MUST make the successful-preview→commit contract genuinely infallible.

**Static-ceiling consequence (KD-2).** A sell posts a `Credit` to #40's `Balance` but does **not** touch
`committedSpendThisWindow` and does **not** raise `AvailableTransferBudget`; sell proceeds therefore do not
increase in-window buy headroom at minimal.

## 3.4 The #30 boundary — roster preflight/commit, a stable-id re-home (KD-7, ERR-031-005)

`ITransferRosterPort` is #31-owned and T2 composition adapts it to #30's roster owner. #31 uses three read/
preflight facts before the commit: `TryGetPlayer` (the record **and the club whose roster carries him**),
`CountPlayersAtPosition`, and `TryPreviewRosterCommit`. A successful preview guarantees the commit lands;
`RequestRosterCommit` is contractually infallible after it, and #31 confirms the landing through `TryGetPlayer`
before applying any local state.

```
TryPreviewRosterCommit(fromClubId, toClubId, playerId):        # ERR-031-010
    if either club is not in the league, or fromClubId == toClubId,
       or playerId is not on fromClubId's roster:  THROW        # an integration defect, never "full"
    return Count(toClubId's roster) < CLUB_SQUAD_SIZE           # false = destination full (F5) — ONLY that

CanRelease(clubId, playerId):                                    # ERR-031-007
    return CanFieldStartingEleven(clubId's roster without playerId)   # the engine's own selector

RequestRosterCommit(fromClubId, toClubId, playerId):
    # precondition: successful preview with no intervening roster mutation
    MovePlayerRecord(playerId, fromClubId -> toClubId)        # SAME PlayerId; the record and its career rows move
```

**Why a re-home and not a re-key (ERR-031-005, owner decision October 19, 2026).** This section originally
re-keyed the player into the destination's free `localIndex` (`newId := toClubId * CLUB_SQUAD_SIZE + freeLocal`).
That made the id encode the club, and it contradicted #28 FR-PG-011 — regens take FRESH, monotonic ids from
`ProgressionEngine.NextPlayerId`, outside every club's packed range — and #27 FR-SQ-010 as amended by ERR-027-004
(global uniqueness for any future allocator). It also reused a departed player's slot id, so any PlayerId-keyed
state left behind (a ban, an injury, an appearance record) would have been inherited by a different player. With a
stable id there is nothing to migrate: every subsystem's PlayerId-keyed state follows the player by construction,
and #30 moves only its own per-club `(ClubId, PlayerId)` rows with the record. The old `OnPlayerRekeyed` /
`DispatchRosterMoveHook` re-key hook is deleted; T2 instead locks that a real re-home carries the player's #28/#29/#41/#30
career state and #44 tally with him and leaves nothing behind (§5.3 T-TX-REKEY-005; *ERR-031-006: this reference originally read the non-existent id "T-TX-REHOME-005"*).

## 3.5 The transfer window — `IsWindowOpen` (FR-TX-019/020)

```
SummerWindowFrom(uint seasonStartWorldDay):                      # ERR-031-008
    return TransferWindow{ OpenWorldDay  = seasonStartWorldDay,
                           CloseWorldDay = seasonStartWorldDay + SUMMER_WINDOW_LENGTH_DAYS - 1 }

IsWindowOpen(in TransferWindow w, uint worldDay):
    return w.OpenWorldDay <= worldDay AND worldDay <= w.CloseWorldDay
```

The window cursor (`ActiveWindow`) is season state (serialized, reset at the season boundary). #30 owns the
calendar; #31 derives the window from it and never mutates it.

**Corrected at T2 (ERR-031-008).** This section originally derived the window from a `SEASON_START_WORLD_DAY`
`[CROSS] #30` constant that #30 never defined — and a single constant cannot place season N > 1, whose start lies
later on the absolute world-day axis — and its close day `start + LENGTH` made a `LENGTH`-day window `LENGTH + 1`
days long under the inclusive `IsWindowOpen`. The start is now the world day #30 BEGINS the season: the new-game
day at genesis (`League.CreateLoop`), the roll day at each boundary (`RollToNextSeason` step (d″)); it is persisted
in the block as `ActiveWindow`, so a load never re-derives it. **Units:** world days (`uint`). **Range:** start +
`SUMMER_WINDOW_LENGTH_DAYS` − 1 ≤ `uint.MaxValue`, else fail loud. **Example:** a season begun on day 280 with the
default 45-day window is open on days 280..324 — 45 days.

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
PrepareSeasonBoundary(retiredIds, arrivingManagedIds, nextWindow) -> TransfersState:   # PURE (T2)
    next := new TransfersState(managedClub, nextWindow)          # committedSpend = 0
    for each contract in txState (ascending PlayerId):
        if contract.PlayerId in retiredIds:  continue            # G14: the retiree's contract leaves with him
        if contract.LengthSeasons - 1 <= 0:  continue            # F7: expired, never stored as 0
        next.Insert(contract with LengthSeasons - 1)
    for each id in arrivingManagedIds:                            # #28 regens joining the MANAGED club
        next.Insert(Contract{ id, DEFAULT_WAGE, DEFAULT_CONTRACT_SEASONS })   # genesis terms (§3.8)
    return next
```
#30 stages this at its roll step (d″) — after #28's boundary plan, whose `RetiredPlayerIds` and managed-club
`RegenPlayerIds` it passes — and installs the returned state by reference only after `BeginNextSeason`, so a refused
roll leaves #31 untouched (#30 §3.5, ERR-030-056).

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
| 0.10 | 2026-10-19 | — | ERR-031-005 (owner decision Oct 19, 2026): §3.3 pseudocode confirms the commit through the port instead of comparing a previewed id; ownership from the port; §3.4 rewritten as a stable-id re-home with a capacity-only preview. ERR-031-004: F5 reachable because bootstrap squads sit below the cap. |
| 0.11 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): §3.4 cites the T2 carry-everything lock by its real id, T-TX-REKEY-005 (was the non-existent "T-TX-REHOME-005"). The v0.8 row's "hook dispatch" obligation is superseded by that lock (the hook is deleted). |
| 0.12 | 2026-10-20 | — | #31 T2: §3.3 pseudocode — unknown counterparty fails loud (ERR-031-010), a sale needs no contract (ERR-031-009), `SquadDepleted` before staging (ERR-031-007); §3.5 window from the season-begin day, inclusive length corrected (ERR-031-008); §3.7 rewritten as the pure, staged `PrepareSeasonBoundary` with the G14 retiree/regen lockstep. |
#endregion