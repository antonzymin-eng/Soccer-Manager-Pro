# Club Finances & Economy #40 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** October 11, 2026 (v0.14 — PR #497 review: break days accrue no sponsorship, so attribution is independent of roll timing)
**Last Updated (prior):** October 10, 2026 (v0.13 — T3b2 formula half: §3.7 deterministic daily sponsorship/matchday model, gate producer, composition and worked example; code pending)
**Last Updated (prior):** October 9, 2026 (v0.12 — ERR-030-053: keep final-fixture day accounting in the completed season before settlement)
**Last Updated (prior):** October 8, 2026 (v0.11 — stage the complete daily finance result before publication)
**Last Updated (prior):** October 8, 2026 (v0.10 — zero inputs, sole gate ownership and fixture/save timing pinned)
**Last Updated (prior):** September 11, 2026 (v0.9 — ERR-040-003 review close-out: §3.1 pseudocode realigned to the shipped direct reset after the handoff helper was removed)
**Last Updated (prior):** September 11, 2026 (v0.8 — ERR-040-003 review: explicit completed-season revenue handoff and coherent-prior gate semantics)
**Last Updated (prior):** September 11, 2026 (v0.7 — wording correction: ApplyTransaction remains the single externally-commanded ledger path, not the only T3 mutation)
**Last Updated (prior):** September 11, 2026 (v0.6 — T3a lifecycle: current-season revenue resets at settlement; FFP window still carries)
**Last Updated (prior):** September 11, 2026 (v0.5 — T3a accounting primitive: identity gate, checked daily revenue accrual, no producer/RNG/tick wiring)
**Last Updated (prior):** September 7, 2026 (v0.4 — PR #363 Codex correction: overflow-safe board scaling)
**Version:** 0.12
**Status:** APPROVED

---

All arithmetic is integer. The minimal tier has **no stochastic term** — `SettleFinances` is a pure function
of its parameters (KD-2); there is nothing to save beyond `ClubFinances` itself, and no draw to reproduce
across a save/restore boundary.

## 3.1 The season-boundary step (`SettleFinances`)

```
SettleFinances(in ClubFinances prior, finalTablePosition, clubCount, in BoardModifier board) -> ClubFinances:
    validate prior coherence
    assert 1 <= finalTablePosition <= clubCount                        # F7 — bad input bound
    assert board.BudgetMultiplierMillPermille > 0                      # F4 — non-positive caller error (fail loud)

    prizeMoney = PrizeMoneyForPosition(finalTablePosition, clubCount)   # §3.1.1 — fixed integer interpolation

    result = prior
    result.Balance += prizeMoney                                       # ADDS — carries prior.Balance forward,
                                                                        # never overwrites it (FR-FN-005)

    baseTransferCeiling = BASE_TRANSFER_BUDGET
                        + prizeMoney * TRANSFER_BUDGET_PRIZE_SHARE_PERMILLE / PERMILLE_DENOM
    result.TransferBudget = ScaleAndClampBudget(baseTransferCeiling, board.BudgetMultiplierMillPermille)
                                                                        # SETS — overwrites prior ceiling

    baseWageCeiling     = BASE_WAGE_BUDGET
                        + prizeMoney * WAGE_BUDGET_PRIZE_SHARE_PERMILLE / PERMILLE_DENOM
    result.WageBudget   = ScaleAndClampBudget(baseWageCeiling, board.BudgetMultiplierMillPermille)
                                                                        # SETS — overwrites prior ceiling

    # Future FFP logic that consumes the completed season's revenue inserts HERE, ABOVE the reset, and
    # reads prior.SeasonRevenueAccrued — never result.SeasonRevenueAccrued, which is zero from here on.
    result.SeasonRevenueAccrued = 0

    # WageBillAggregate carries: a committed wage does not vanish at season end.
    # FfpBalanceWindow carries unchanged until the later FFP slice defines its own window update.
    return result

ScaleAndClampBudget(baseCeiling, positiveMultiplier) -> long:
    if baseCeiling <= 0: return 0                                      # F1 lower floor
    if CLUB_FINANCES_BUDGET_CEILING_MAX <= 0:
        return CLUB_FINANCES_BUDGET_CEILING_MAX                        # coherence validation owns bad config

    whole = baseCeiling / PERMILLE_DENOM
    remainder = baseCeiling % PERMILLE_DENOM

    # Saturate before a product that could overflow Int64. This comparison is equivalent to asking whether
    # whole * positiveMultiplier already exceeds the configured ceiling, but requires division only.
    if whole > CLUB_FINANCES_BUDGET_CEILING_MAX / positiveMultiplier:
        return CLUB_FINANCES_BUDGET_CEILING_MAX

    scaledWhole = whole * positiveMultiplier                           # safe after the pre-check
    scaledRemainder = remainder * positiveMultiplier / PERMILLE_DENOM  # remainder < 1000; product is bounded

    if scaledRemainder >= CLUB_FINANCES_BUDGET_CEILING_MAX - scaledWhole:
        return CLUB_FINANCES_BUDGET_CEILING_MAX

    return scaledWhole + scaledRemainder                               # exact integer-floor result below cap
```

The quotient/remainder form is mathematically identical to
`floor(baseCeiling × positiveMultiplier / PERMILLE_DENOM)` for positive `baseCeiling`, but it never forms
the potentially overflowing full product. This matters because Appendix A explicitly permits config values
through the shared signed-Int32 loader; those accepted values can produce a `baseCeiling` large enough that a
non-identity positive board multiplier would overflow `long` before a naïve post-multiply clamp ran. The cap
therefore applies **before** any unsafe product, while ordinary below-cap values retain the exact same integer
floor semantics.

`SeasonRevenueAccrued` is explicitly a **non-negative current-season** accumulator at T3a. Resetting it in
`SettleFinances` prevents a live daily accrual from silently spanning seasons. The completed season's value
remains readable as `prior.SeasonRevenueAccrued` for the whole of the settlement calculation; the FFP slice is
required to derive its next-season penalty from that parameter (plus any other then-specified terms) at a point
**above** the reset, not from `result.SeasonRevenueAccrued`, which is zero from the reset onward. T3a introduces
no intermediate handoff local or helper for this: `prior` is already the source of truth, and a second named
carrier would give the FFP slice a place to accumulate state the reset then contradicts.
T3a does not guess how `FfpBalanceWindow` rolls, so that field is
carried field-identically until its own slice lands. This rule is behaviour-neutral at Stage 2 because the
accumulator is always zero there (KD-8).

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

`ApplyTransaction` is the single **externally-commanded ledger** mutation path between season boundaries
(KD-3/KD-5, FR-FN-013). At T3+, #40-owned autonomous accrual functions such as `AccrueDailyRevenue` are
separate accounting transforms, not a second caller-command ledger. This is the ERR-040-003 correction to
the earlier over-broad wording. `ApplyTransaction` never reads or writes `TransferBudget`/`WageBudget` — those
are set exclusively by `SettleFinances` once per season (§1.6/FR-FN-004); Stage 2 has no concept of "budget
remaining after this season's spend" as a tracked field — if a caller (#31) wants that, it computes it
externally by summing the transactions it has itself submitted, or a future deep-tier extension adds it as a
new field (recorded in §7, not built here).

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

### 3.4.1 T3a daily revenue accounting primitive (`AccrueDailyRevenue`)

T3a defines the **accounting mutation only**. It deliberately does not decide how much sponsorship or
matchday revenue a club earns, does not own the calendar invocation, and does not draw sponsorship variance.
Calendar invocation lands at T3b1 (§3.6); amount production and variance are later slices (§7). Keeping this primitive pure preserves #40's ownership of the accounting
rule while allowing #30 to remain the lifecycle/composition owner when the daily slot is wired.

```
AccrueDailyRevenue(in ClubFinances prior,
                   sponsorshipRevenue,
                   matchdayRevenue,
                   deepRevenueEnabled) -> ClubFinances:
    validate prior coherence                              # F1 runs even when the deep gate is off

    if deepRevenueEnabled == false:
        return prior exactly                              # coherent-prior KD-8 identity; do not interpret deep amounts

    assert sponsorshipRevenue >= 0                       # revenue is not an expenditure channel
    assert matchdayRevenue >= 0

    dailyRevenue = checked(sponsorshipRevenue + matchdayRevenue)
    result = prior
    result.Balance = checked(result.Balance + dailyRevenue)
    result.SeasonRevenueAccrued = checked(result.SeasonRevenueAccrued + dailyRevenue)

    # T3a changes NO other field:
    # TransferBudget / WageBudget / WageBillAggregate / FfpBalanceWindow stay field-identical.
    validate result coherence
    return result
```

The three additions are checked independently: a component-sum overflow, cash-balance overflow, or season-
accumulator overflow fails loud rather than wrapping into a plausible finance value. Because `result` is a
value copy and the function returns only after all checks pass, no partial mutation can escape on failure.

`deepRevenueEnabled = false` is the exact Stage-2 identity **for a coherent prior finance value**, including
for otherwise-invalid deep-only input amounts: the off path returns before interpreting those amounts.
Canonical finance-state coherence is intentionally validated first; disabling a feature is not permission to
admit corrupt budget/liability/revenue state. T-FN-NEU-004 locks both halves of that ordering.

This T3a primitive **does not promote** `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91`: it performs
no draw and stores no draw cursor/action ordinal. Promotion remains atomic with the first genuine stochastic
sponsorship-variance consumer. It also does not update `FfpBalanceWindow`, debit `WageBillAggregate`, choose
`[GT]` revenue magnitudes; T3b1 supplies the separate #30 day-completion call (§3.6). Passing already-derived amounts into this pure
primitive is an internal layering boundary, not permission for #30/#31/#34/#45 to invent competing finance
models; the amount-production rules remain #40-owned when those later T3 slices are specified.

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

`WageBillAggregate` stays `95,000` — `SettleFinances` never touches it. `SeasonRevenueAccrued` is reset to
`0` for the new season; `FfpBalanceWindow` carries unchanged until its later T3 rule lands.

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

## 3.6 T3b1 day-completion composition (ERR-030-052)

#30 invokes `SeasonFinanceRuntime.PrepareDailyRevenue` once per completed world day at **slot 11a**,
after career/management slots 0–11 and before slot 12's `WorldStore.AdvanceDay()`. For every initialized
club, #30 passes `sponsorshipRevenue = 0L`, `matchdayRevenue = 0L`, and the sole #40-owned
`ClubFinancesConstants.DEEP_REVENUE_ENABLED = false` [FIXED] into the T3a primitive. The primitive is
still invoked with the gate off, so canonical finance coherence is checked. Generic low-level loops with
an explicitly empty/unwired finance set skip this pass without manufacturing entries; canonical new games
and Restore-normalized saves have a complete set. Finance commands and settlement still refuse emptiness.

**Atomic publication:** `PrepareDailyRevenue` computes every club into a detached array. #30 replaces
its live array only after the complete pass succeeds, then advances the world clock. If any club fails
coherence or arithmetic validation, no finance result is published and the clock remains on the same day.
The prior career steps are idempotent on that day, so repair-and-retry cannot double-accrue earlier clubs.
This all-club staging rule MUST remain when T3b2 supplies non-zero amounts; per-club value-copy arithmetic
alone does not protect a multi-club pass. The temporary array is not serialized.

**Fixture timing:** slots 0–11 may run pre-round and again on the following advance using their career
cursors. Finance is outside that replayed helper. Resolving a fixture does not complete its day; the next
advance accounts for that day, after its fixtures, exactly once. A save before or after the fixture keeps
the same world day; a save after advancement keeps the next day. No finance cursor, gate bit, save-layout
change, or RNG draw is added. Zero-day and refused advances invoke no accounting. Season-break advances
use the same slot. T3b2 must feed its model through this completion seam, rather than adding a second caller.

**Final-day precondition (ERR-030-053):** playing the last round closes its fixtures, not its world
day. After every fixture is played, `RollToNextSeason` MUST refuse while
`WorldStore.CurrentWorldTick <= Calendar.DayOfRound(Calendar.RoundCount - 1)`, before staging or
committing boundary work. Callers use `AdvanceDays(1)` to complete a pending final fixture day through
slot 11a, against the completed season, before settlement resets `SeasonRevenueAccrued`. The roll never
implicitly advances the world. Its refusal preserves the whole save, and Save/Load/Restore preserves this
precondition through the existing world clock. Already-completed final days require no additional step.
The next opening date and `SeasonBreakDays` remain unchanged; completion consumes the first break advance.
T-FN-DAY-008 in #40 §5.9 covers refusal, continuation and the boundary clock contract. T3b2 MUST also
prove that non-zero final-fixture revenue belongs to the completed season before settlement.

**Worked identity example (integer currency units):** day 5, club 2, coherent prior
`{ Balance: -102, TransferBudget: 202, WageBudget: 302, WageBillAggregate: 402,
SeasonRevenueAccrued: 502, FfpBalanceWindow: -602 }`. Revenue inputs are `0 + 0 = 0`; the disabled gate
returns all six fields identically, and only #30 advances the world clock to day 6. Negative balance and
FFP-window values remain representable; budgets, liability and accrued revenue must stay non-negative.

## 3.7 T3b2 deterministic daily revenue model

**Status:** specified October 10, 2026 as the T3b2 formula half of §7.1. **No code implements this
section yet.** Until the T3b2 implementation slice lands, production still runs §3.6's zero-input identity.
Every magnitude below is `[GT]` and illustrative, like the rest of Appendix A, pending a balance pass.

**Ownership.** #40 owns both formulas, every constant and the single gate producer. #30 owns only timing
(slot 11a, §3.6) and forwards four facts it already holds for the day being completed. It chooses no
amount and adds no flag. The model is integer-only and draw-free; the reserved `0x29`/91 namespace stays
reserved (T3c+).

### 3.7.1 Gate

`DeepRevenueEnabled` replaces the T3b1 `[FIXED] DEEP_REVENUE_ENABLED = false` as the **only** producer of
`deepRevenueEnabled`: a `[GT]` boolean read once through the shared config loader
(`[club-finances] DeepRevenueEnabled`), **default `false`**. With the default, §3.6's behaviour and every
T3b1 lock hold unchanged (KD-8): #30 passes `0L, 0L, false` and does not evaluate §3.7.2. Turning the gate on
is a gameplay change for career saves. It belongs to the balance pass or an owner decision, not to the
T3b2 code landing.

### 3.7.2 Daily amounts (`DailyRevenue`, pure, #40)

Inputs per club for completed world day `d`, supplied by #30:

| Input | Meaning | Range |
|---|---|---|
| `homeFixturesCompleted` | league fixtures scheduled on `d` in which the club is the home side | `0..` (0 or 1 in the Stage-2 double round robin) |
| `leaguePosition` | the club's live league-table position at slot 11a, after `d`'s fixtures | `1..clubCount` |
| `clubCount` | clubs in the league | `≥ 2` |
| `inSeasonDay` | `d` lies in the live season's playing span, `DayOfRound(0) ≤ d ≤ DayOfRound(RoundCount − 1)` | bool |

```
DailyRevenue(homeFixturesCompleted, leaguePosition, clubCount, inSeasonDay) -> (sponsorship, matchday):
    assert homeFixturesCompleted >= 0
    assert clubCount >= 2 and 1 <= leaguePosition <= clubCount
    assert inSeasonDay or homeFixturesCompleted == 0                         # fixtures only occur in-season
    sponsorship = inSeasonDay ? DAILY_SPONSORSHIP_REVENUE : 0                # in-season days only (§3.7.3)

    fillSpan     = MATCHDAY_FILL_TOP_PERMILLE - MATCHDAY_FILL_BOTTOM_PERMILLE  # >= 0 (catalogue invariant)
    fillPermille = MATCHDAY_FILL_TOP_PERMILLE
                   - fillSpan * (leaguePosition - 1) / (clubCount - 1)       # integer division, as §3.1.1
    attendance   = MATCHDAY_STADIUM_CAPACITY * fillPermille / PERMILLE_DENOM   # spectators, floored
    matchday     = checked(homeFixturesCompleted * attendance * MATCHDAY_REVENUE_PER_SPECTATOR)
    return (sponsorship, matchday)
```

Units: currency amounts are integer currency units (the unit of `Balance`); `fillPermille` is per-mille of
capacity (600..950 with the default catalogue); `attendance` is spectators. All arithmetic is `long` and
checked. `fillPermille` is linear in table position, the same shape as §3.1.1 prize money: position 1 fills
`MATCHDAY_FILL_TOP_PERMILLE`, last place fills `MATCHDAY_FILL_BOTTOM_PERMILLE`.

`MATCHDAY_STADIUM_CAPACITY` is `[CROSS-PENDING]` against #53's `STADIUM_BASE_CAPACITY` (20,000). The
intended input is #53's per-club `StadiumCapacity(clubId)` (FR-IN-025, ERR-040-002), but #53 has no
assembly. When #53's T0 lands, that query replaces this constant per club; until then every club uses the
same capacity, and #40 builds no #53 interface (CLAUDE.md: nothing is wired ahead of a T0 landing).

### 3.7.3 Composition at slot 11a

With the gate on, #30 computes the four inputs for each initialized club and passes the result into the
unchanged T3a primitive: `AccrueDailyRevenue(prior, sponsorship, matchday, true)`. §3.6's all-club staging
and detached publication are unchanged: any club's refusal, including a `DailyRevenue` assertion, publishes
nothing and leaves the clock on `d`.

**Why the inputs are already settled at slot 11a.** #30's KD-4 guard (`AdvanceDays`, FR-SN-011) refuses to
move the clock past a pending round's fixture day, so every fixture scheduled on `d` has a result before
`d` completes. The table at slot 11a therefore already includes `d`'s results. `homeFixturesCompleted`
counts scheduled fixtures; an implementation MUST assert that each counted fixture has a result rather
than silently counting an unplayed one.

**Timing consequences (follow from §3.6, restated for amounts):**

- A fixture day's matchday revenue is accrued once, on the advance that completes it, after its fixtures.
- The final round's day is completed by `AdvanceDays(1)` before `RollToNextSeason` (ERR-030-053). Its
  sponsorship and matchday revenue land in the **completed** season's `SeasonRevenueAccrued` before
  settlement reads the handoff and resets it.
- **Season-break days accrue nothing**, before or after the roll. #30 lets a caller advance the whole break
  before rolling (`AdvanceDays` refuses only a step past the next opening day; `SeasonRollTests` advances
  `SeasonBreakDays` and then rolls). Break days after the final round day are outside the completed
  season's span; once the roll installs the next calendar, the remaining break days precede its
  `DayOfRound(0)`. Either way `inSeasonDay` is false, so the completed season's `SeasonRevenueAccrued` at
  settlement is the same whether the roll happens immediately after final-day completion or at the end
  of the break. The same rule gives no sponsorship on the first season's pre-season days before round 0.
- No new save field: `d`, the fixtures, their results and the table are all restored from the existing
  season save, so a save at any point continues identically.

### 3.7.4 Worked example (default catalogue, gate on)

Club 12, `clubCount = 20`, completing day `d`, on which club 12 hosted its round fixture; after `d`'s
results it sits 4th. Prior is §3.5's post-transaction state: `Balance = 2,315,790`,
`SeasonRevenueAccrued = 0`.

```
sponsorship  = 1,000
fillSpan     = 950 − 600 = 350
fillPermille = 950 − 350 × (4 − 1) / (20 − 1) = 950 − 1,050 / 19 = 950 − 55 = 895      [per-mille]
attendance   = 20,000 × 895 / 1,000 = 17,900                                           [spectators]
matchday     = 1 × 17,900 × 2 = 35,800                                                 [currency]
daily        = 1,000 + 35,800 = 36,800
Balance      = 2,315,790 + 36,800 = 2,352,590;  SeasonRevenueAccrued = 0 + 36,800 = 36,800
```

On an in-season day with no home fixture (an away day or a free day) the same club accrues `1,000`; on a
break or pre-season day it accrues `0`. The endpoints with one home fixture are: position 1 → `950‰ → 19,000 → 38,000`; position 20 →
`600‰ → 12,000 → 24,000`. Over a 38-round season that is roughly 0.46–0.72 M of matchday revenue, plus
`1,000` per in-season day, against 0.2–2.0 M of prize money (Appendix A). It is the same order of
magnitude, and the balance pass may retune it.

### 3.7.5 Failure rows

`DailyRevenue` fails loud (`ArgumentOutOfRangeException`) on `homeFixturesCompleted < 0`, `clubCount < 2`,
`leaguePosition` outside `1..clubCount`, or a home fixture on a day that is not `inSeasonDay`, and (`OverflowException`) on checked overflow. These are F8's
sibling rows: they are evaluated only with the gate on, so the default gate cannot surface them.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms: `SettleFinances`, `PrizeMoneyForPosition`, `ApplyTransaction`, `AvailableTransferBudget`, composition at #30's boundary roll, worked example. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): §3.2 `ApplyTransaction` split — wage line items change `WageBillAggregate` only (periodic cash-out deferred), cash line items change `Balance` only; worked example updated. |
| 0.3 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** §3.1 now rejects every non-positive board multiplier before arithmetic; the lower budget clamp is not an authorization for a negative modifier. |
| 0.4 | 2026-09-07 | — | **PR #363 Codex correction.** Replaces post-multiply clamping with an overflow-safe quotient/remainder scale-and-cap that preserves exact integer-floor semantics below the ceiling. |
| 0.5 | 2026-09-11 | OpenAI | **T3a contract.** Adds the pure identity-gated daily sponsorship/matchday accounting primitive; pins checked arithmetic and field isolation while explicitly deferring amount producers, #30 tick wiring, RNG promotion, wage cash-out, and FFP. |
| 0.6 | 2026-09-11 | OpenAI | **T3a lifecycle closure.** Defines `SeasonRevenueAccrued` as current-season state reset by `SettleFinances`; the future FFP term must consume the prior value before reset, while `FfpBalanceWindow` continues to carry until its own rule lands. |
| 0.7 | 2026-09-11 | OpenAI | **T3a wording correction.** Restates `ApplyTransaction` as the single externally-commanded ledger mutation path so §3.2 no longer conflicts with the autonomous T3a accrual path defined in §3.4.1 and FR-FN-003. |
| 0.8 | 2026-09-11 | OpenAI | **ERR-040-003 / review correction.** Names the completed-season revenue handoff before reset, pins future FFP consumption to that handoff, and clarifies that the disabled revenue gate is identity only after canonical prior-state coherence validation. |
| 0.9 | 2026-09-11 | Claude | **ERR-040-003 close-out.** v0.8's `completedSeasonRevenue` local and `CloseCompletedSeasonRevenue(...)` call were removed from `FinanceStep.cs` (v1.8) as a misleading indirection, but §3.1's pseudocode and §3.1's prose still described both — an APPROVED spec instructing the next implementer to rebuild a construct the code had just deleted. §3.1 now shows the shipped `result.SeasonRevenueAccrued = 0` with the FFP insertion point pinned **above** the reset and reading `prior.SeasonRevenueAccrued`, and records why no intermediate carrier is reintroduced. No requirement, arithmetic or worked-example value changes. |
| 0.10 | 2026-10-08 | — | **T3b1 / ERR-030-052.** zero inputs, sole gate ownership and fixture/save timing pinned. |
| 0.11 | 2026-10-08 | — | **PR #491 review.** stage the complete daily finance result before publication. |
| 0.12 | 2026-10-09 | — | **ERR-030-053 / PR #491 Codex review.** keep final-fixture day accounting in the completed season before settlement. |
| 0.13 | 2026-10-10 | — | **T3b2 formula half (§7.1).** New §3.7: `[GT]` config-owned `DeepRevenueEnabled` (default false) replaces the `[FIXED]` T3b1 gate; pure integer `DailyRevenue` (flat daily sponsorship; per home fixture, attendance = capacity × position-linear fill × per-spectator price); `MATCHDAY_STADIUM_CAPACITY` `[CROSS-PENDING]` on #53; slot-11a composition reusing §3.6 staging; KD-4 settledness argument; worked example; failure rows. No code yet; production remains §3.6 identity. |
| 0.14 | 2026-10-11 | — | **PR #497 review (delayed roll).** #30 permits advancing the whole break before `RollToNextSeason`, so v0.13's "break days accrue into the new season" was false and made completed-season revenue depend on roll timing. `DailyRevenue` gains `inSeasonDay` (`DayOfRound(0) ≤ d ≤ DayOfRound(RoundCount − 1)` of the live season): sponsorship accrues only in-season, break and pre-season days accrue nothing, and a home fixture outside that span fails loud. Worked example and failure rows updated. Still spec only. |
#endregion
