# Club Finances & Economy #40 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

All arithmetic is integer. The minimal tier has **no stochastic term** — `SettleFinances` is a pure function
of its parameters (KD-2); there is nothing to save beyond `ClubFinances` itself, and no draw to reproduce
across a save/restore boundary.

## 3.1 The season-boundary step (`SettleFinances`)

```
SettleFinances(in ClubFinances prior, finalTablePosition, clubCount, in BoardModifier board) -> ClubFinances:
    assert 1 <= finalTablePosition <= clubCount                        # F7 — bad input bound
    assert board.BudgetMultiplierMillPermille != 0                     # F4 — zero-value trap (fail loud)

    prizeMoney = PrizeMoneyForPosition(finalTablePosition, clubCount)   # §3.1.1 — fixed integer interpolation

    result = prior
    result.Balance += prizeMoney                                       # ADDS — carries prior.Balance forward,
                                                                        # never overwrites it (FR-FN-005)

    baseTransferCeiling = BASE_TRANSFER_BUDGET
                        + prizeMoney * TRANSFER_BUDGET_PRIZE_SHARE_PERMILLE / PERMILLE_DENOM
    result.TransferBudget = Clamp(baseTransferCeiling * board.BudgetMultiplierMillPermille / PERMILLE_DENOM,
                                  0, CLUB_FINANCES_BUDGET_CEILING_MAX)  # SETS — overwrites the prior ceiling (F1 floor)

    baseWageCeiling     = BASE_WAGE_BUDGET
                        + prizeMoney * WAGE_BUDGET_PRIZE_SHARE_PERMILLE / PERMILLE_DENOM
    result.WageBudget   = Clamp(baseWageCeiling * board.BudgetMultiplierMillPermille / PERMILLE_DENOM,
                                0, CLUB_FINANCES_BUDGET_CEILING_MAX)    # SETS — overwrites the prior ceiling (F1 floor)

    # WageBillAggregate / SeasonRevenueAccrued / FfpBalanceWindow are UNTOUCHED by the minimal projection —
    # a committed wage does not vanish at season end (WageBillAggregate carries forward); the deep-tier
    # accumulators reset or carry per their own T3 rules (deferred, KD-1/KD-8).
    return result
```

`SettleFinances` reads no caller state beyond its four parameters — it is a pure function, so calling it
twice with identical inputs yields byte-identical output (no hidden clock, no RNG). A `ClubId` with no prior
`ClubFinances` (never bootstrapped via `CreateInitial`) is a lifecycle bug — the entry must exist before this
is ever called (F6, §2.3).

### 3.1.1 Prize money from final position (`PrizeMoneyForPosition`, pure)

```
PrizeMoneyForPosition(position, clubCount) -> long:
    assert clubCount >= 2                                             # a single-club table has no spread (F7-class)
    span = PRIZE_MONEY_WINNER - PRIZE_MONEY_LAST_PLACE                # >= 0 (Appendix A catalogue invariant)
    return PRIZE_MONEY_WINNER - span * (position - 1) / (clubCount - 1)  # integer division, linear interpolation
```

A pure, deterministic **linear** interpolation between the two `[GT]` endpoints (Appendix A) — position 1
receives `PRIZE_MONEY_WINNER`, position `clubCount` receives `PRIZE_MONEY_LAST_PLACE`, every position between
is an integer-divided linear step. No RNG, no lookup table sized to a variable `clubCount` — the formula
generalizes across divisions of different sizes (relevant once #43 promotion/relegation exists and clubs move
between divisions with different `clubCount`s).

## 3.2 The ledger mutation (`ApplyTransaction`)

```
ApplyTransaction(ref ClubFinances f, in FinanceTransaction txn):
    if txn.Amount < 0: throw                                    # F2 — sign lives in Kind, magnitude is >= 0
    if txn.Kind is not a defined FinanceTransactionKind: throw    # F2
    if txn.LineItem is not a defined FinanceLineItem: throw       # F2

    if txn.LineItem == PlayerWage or txn.LineItem == StaffWage:
        # WAGE COMMITMENT — a wage transaction changes the ongoing wage LIABILITY, never cash (Balance).
        # WageBillAggregate is the club's CURRENT total wage bill, not a running sum of payments; the
        # actual cash cost of wages is the periodic wage PAYMENT, a deferred deep-tier accrual step (§7)
        # that debits Balance from the aggregate — NOT modelled here. Signing/raising a contract is a
        # Debit (raise the liability); terminating/reducing one is a Credit (lower it). Balance is
        # untouched, so repeated wage transactions cannot double-count as both cash and commitment.
        if txn.Kind == Debit:
            f.WageBillAggregate += txn.Amount                    # a new/increased wage commitment
        else:
            if txn.Amount > f.WageBillAggregate: throw            # F1 — would drive the aggregate negative
            f.WageBillAggregate -= txn.Amount                     # a termination/reduction
    else:
        # CASH movement (TransferFee / General) — changes Balance ONLY, never the wage aggregate.
        signedAmount = (txn.Kind == Debit) ? -txn.Amount : txn.Amount
        f.Balance += signedAmount                                # may go negative — debt is representable;
                                                                  # Balance carries NO F1 floor (only the
                                                                  # budget ceilings and the wage aggregate do)
    # This function NEVER touches TransferBudget/WageBudget (FR-FN-004) — those are SettleFinances-only.
```

`ApplyTransaction` is the single mutation path between season boundaries (KD-3/KD-5, FR-FN-013). It never
reads or writes `TransferBudget`/`WageBudget` — those are set exclusively by `SettleFinances` once per season
(§1.6/FR-FN-004); Stage 2 has no concept of "budget remaining after this season's spend" as a tracked field —
if a caller (#31) wants that, it computes it externally by summing the transactions it has itself submitted,
or a future deep-tier extension adds it as a new field (recorded in §7, not built here).

## 3.3 The read-only constraint query (`AvailableTransferBudget`)

```
AvailableTransferBudget(in ClubFinances f) -> long:
    return f.TransferBudget     # pure read — #31's spending ceiling for the current season
```

A trivial passthrough (KD-3) — #40 does not compute a combined "budget-and-balance-aware" headroom; whether
#31 additionally considers `Balance` (e.g. refusing a deal that would drive `Balance` deeply negative) is a
#31-owned decision reading both this query and, if it needs it, a future read-only `Balance` accessor — not
prescribed here.

## 3.4 Composition at #30's season-boundary roll (informative)

Per the KD-6 back-prop (ERR-030-003), `RollToNextSeason()` becomes:

```
RollToNextSeason():
    finalTable := Table.OrderedView()                     # (a) finalize
    Board.Evaluate(finalTable)                             # (b) board pass/fail + job-security
    # (a')  <-- #43 promotion/relegation transform inserts HERE (FR-SN-031), not built now
    for each clubId in ClubIds:                             # (b') NEW — #40's finance-settlement step
        position   = finalTable.PositionOf(clubId)          # POST-promotion division/position if #43 has run
        clubCount  = finalTable.ClubCountForClubsDivision(clubId)
        financeState[clubId] = SettleFinances(financeState[clubId], position, clubCount, board[clubId])
    nextSeed := DeriveNextSeasonSeed(Seed, SeasonNumber)
    Fixtures := FixtureScheduler.Generate(ClubIds, nextSeed)   # (c) regenerate
    AdvanceAges()                                           # (d) #28 — NULL SEAM today
    Table := LeagueTable.Empty(ClubIds)                     # (e) reset
    SeasonNumber++
    Seed := nextSeed
```

Step (b') runs **once per club per season**, strictly after (a')'s promotion/relegation transform (when #43
exists) and strictly before (c) regenerate — so a club's finance projection reflects the division it will
actually play in next season (KD-6's ordering rationale). While #43 is unbuilt, (a') is a no-op and (b') reads
the pre-#43 `finalTable` directly — the same "prerequisite gate degenerates to a pass-through" pattern #26's
T2/T4 decision gates use ahead of their own upstream engine-substrate deliverables.

## 3.5 Worked example

Club 12, season 7, finishes **position 4 of 20** clubs. Prior `ClubFinances` (from season 6's end):
`Balance = 1,250,000`, `TransferBudget = 400,000` (stale — about to be overwritten), `WageBudget = 180,000`
(stale), `WageBillAggregate = 95,000` (a still-active wage commitment carried over — untouched by
`SettleFinances`), `SeasonRevenueAccrued = 0`, `FfpBalanceWindow = 0`. `BoardModifier.Identity` (per-mille
1000).

**Step 1 — prize money:** `span = 2,000,000 − 200,000 = 1,800,000`.
`prizeMoney = 2,000,000 − 1,800,000 × (4−1) / (20−1) = 2,000,000 − 5,400,000/19 = 2,000,000 − 284,210 =
1,715,790` (integer floor: `5,400,000 / 19 = 284,210` remainder 10, discarded).

**Step 2 — Balance:** `result.Balance = 1,250,000 + 1,715,790 = 2,965,790` (ADDS, per FR-FN-005).

**Step 3 — TransferBudget:** `baseCeiling = 100,000 + 1,715,790 × 400 / 1000 = 100,000 + 686,316 = 786,316`
(exact — `1,715,790 × 400 = 686,316,000`, `/1000 = 686,316`). `× board 1000/1000 = 786,316` unchanged.
`result.TransferBudget = 786,316` (SETS, overwriting the stale `400,000`).

**Step 4 — WageBudget:** `baseCeiling = 50,000 + 1,715,790 × 150 / 1000 = 50,000 + 257,368 = 307,368`
(`1,715,790 × 150 = 257,368,500`, integer-divided by 1000 floors to `257,368`). `× 1000/1000 = 307,368`
unchanged. `result.WageBudget = 307,368` (SETS, overwriting the stale `180,000`).

`WageBillAggregate` stays `95,000` — `SettleFinances` never touches it.

**Post-`SettleFinances` state:** `{ Balance: 2,965,790, TransferBudget: 786,316, WageBudget: 307,368,
WageBillAggregate: 95,000, SeasonRevenueAccrued: 0, FfpBalanceWindow: 0 }`.

**Mid-season ledger activity (season 8, via `ApplyTransaction`):**

1. A transfer fee is paid: `FinanceTransaction{ Debit, TransferFee, Amount = 650,000 }`. `signedAmount =
   −650,000`; `Balance = 2,965,790 − 650,000 = 2,315,790`. `WageBillAggregate` unaffected (not a wage
   `LineItem`). `AvailableTransferBudget` still returns `786,316` — unchanged by the spend (FR-FN-004; the
   ceiling is a season constant, not a running remaining-budget total).
2. The signing's wage commitment is recorded: `FinanceTransaction{ Debit, PlayerWage, Amount = 12,000 }`.
   A wage transaction changes the LIABILITY only, never cash: `WageBillAggregate = 95,000 + 12,000 =
   107,000`; `Balance` is **unchanged** at `2,315,790` (the periodic wage cash-out is a deferred deep-tier
   accrual, §7 — not this transaction).
3. Later, the club releases a different squad player, terminating a wage commitment:
   `FinanceTransaction{ Credit, PlayerWage, Amount = 20,000 }`. `20,000 ≤ 107,000` so no F1 fail;
   `WageBillAggregate = 107,000 − 20,000 = 87,000`; `Balance` again **unchanged** at `2,315,790`.

A hypothetical cash (`TransferFee`/`General`) transaction large enough to drive `Balance` negative would
**not** fail loud — debt is representable (`Balance` carries no F1 floor); only `TransferBudget`/
`WageBudget`/`WageBillAggregate` do.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms: `SettleFinances`, `PrizeMoneyForPosition`, `ApplyTransaction`, `AvailableTransferBudget`, composition at #30's boundary roll, worked example. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): §3.2 `ApplyTransaction` split — wage line items change `WageBillAggregate` only (periodic cash-out deferred), cash line items change `Balance` only; worked example updated. |
#endregion
