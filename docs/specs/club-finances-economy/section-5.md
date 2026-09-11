# Club Finances & Economy #40 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** September 11, 2026 (v0.8 — T3a lifecycle acceptance: settlement resets current-season revenue and carries FFP window)
**Last Updated (prior):** September 11, 2026 (v0.7 — T3a daily-revenue identity, mutation-isolation and overflow acceptance locks)
**Last Updated (prior):** September 11, 2026 (v0.6 — ERR-030-051/T2b: boundary atomicity contract corrected and live lifecycle evidence named)
**Last Updated (prior):** September 11, 2026 (v0.5 — T-FN-LIFE-001 coverage split recorded across T2a/T2b)
**Last Updated (prior):** September 7, 2026 (v0.4 — PR #363 Codex correction: overflow-safe board-scaling coverage)
**Last Updated (prior):** September 7, 2026 (v0.3 — PR #363 follow-up: non-positive board failure coverage)
**Version:** 0.8
**Status:** APPROVED

---

Tests land at T-phase; this is the acceptance contract.

## 5.1 Determinism & save/restore

- **T-FN-DET-001** — Save→restore across a **mid-season** boundary: each club's `ClubFinances` (`Balance`,
  `TransferBudget`, `WageBudget`, `WageBillAggregate`, `SeasonRevenueAccrued`, `FfpBalanceWindow`) restores
  **field-identical**; resuming `ApplyTransaction` calls afterward reaches the same state as an uninterrupted
  run.
- **T-FN-DET-002** — `RollToNextSeason()` is one synchronous, atomic command: there is **no supported
  mid-call save seam** between (b') and (c). A save immediately before the call restores the pre-roll
  finance state; a save immediately after a successful return restores the already-settled state
  field-identically and does not settle a second time merely because it was restored. If the roll is
  refused after the (b') result has been computed, live `ClubFinances` remains byte-for-byte unchanged.
  This is the #40 extension of #30's all-or-nothing boundary contract (FR-FN-024 / ERR-030-051).
- **T-FN-DET-003** — Two-run determinism: a full season's `SettleFinances` + `ApplyTransaction` sequence from
  one world seed produces byte-identical `ClubFinances` for every club on both runs.
- **T-FN-DET-004** — No RNG stream registered at minimal: the serialized finance block contains no
  `RngCursor`/`actionOrdinal` field (grep/schema-shape assertion), and `_RESERVED_0x29_`/91 is not consumed
  by any draw (KD-2/FR-FN-008/009). T3a remains covered by this same lock because its daily accounting
  primitive is still draw-free; promotion waits for the first stochastic sponsorship-variance consumer.
- **T-FN-DET-005** — `SettleFinances` purity: called twice with identical inputs (`prior`,
  `finalTablePosition`, `clubCount`, `board`) yields byte-identical output `ClubFinances` (no hidden state,
  no clock read).

## 5.2 Club bootstrap & lifecycle (FR-FN-025)

- **T-FN-LIFE-001** — Every `ClubId` in #27's `Squad` enumeration has a `ClubFinances` entry after
  `CreateInitial` bootstrap; the per-club entry count is unchanged across any number of season rolls
  (clubs do not churn, KD-7) — no leak, no removal. T2a proves canonical bootstrap/universe coherence;
  T2b's `SeasonLoopFinanceTests.RollToNextSeason_SettlesEveryClubFromFinalTable_AndPreservesClubSetAcrossRolls`
  proves the across-roll half through the real #30 boundary.
- **T-FN-FAIL-CLUB-001** — `SettleFinances` or `ApplyTransaction` invoked for a `ClubId` with no
  `ClubFinances` entry fails loud (F6).

## 5.3 Behaviour-neutral identity (KD-8)

- **T-FN-NEU-001** — With the deep dials off, `BoardModifier.Identity`, and zero accumulators,
  `SettleFinances` yields **exactly** `budget = f(finalTablePosition, prizeMoney)` — no FFP/board adjustment
  changes the projected ceilings (KD-8/FR-FN-028).
- **T-FN-NEU-002** — `ClubFinances.CreateInitial(startingBalance)` yields `Balance = startingBalance`,
  `TransferBudget = 0`, `WageBudget = 0`, `WageBillAggregate = 0`, `SeasonRevenueAccrued = 0`,
  `FfpBalanceWindow = 0` — the pre-first-season identity.
- **T-FN-NEU-003** — Registering #40's reserved namespace slot (not a stream — none is registered at
  minimal) leaves every existing stream's cursor byte-identical across a full season run with and without
  #40 active (stream independence) — trivially true at Stage 2 since no stream exists yet, the same test
  class as #41's `T-MD-NEU-003`.
- **T-FN-NEU-004** — T3a `AccrueDailyRevenue(..., deepRevenueEnabled:false)` returns the complete
  `ClubFinances` value field-identically and does not interpret deep-only revenue inputs; even negative
  placeholder inputs on the disabled path cannot change state or throw (KD-8 / §3.4.1).

## 5.4 Season-boundary ordering (KD-6)

- **T-FN-ORD-001** — `SettleFinances` is invoked at #30's new step (b'), strictly after the (a') #43
  promotion/relegation insertion point and strictly before (c) regenerate — a structural/ordering assertion
  against #30's `RollToNextSeason()` sequence (post-ERR-030-003).
- **T-FN-ORD-002** — `SettleFinances` reads the POST-promotion/relegation division/`finalTablePosition` (i.e.
  #43's transform result, when #43 exists) rather than the pre-promotion table — an integration-level
  ordering lock (forward-looking; degenerates to a no-op check while #43 is unbuilt, per the #26 T2/T4-style
  prerequisite-gate pattern).
- **T-FN-ORD-003** — `SettleFinances` is **not** invoked on the world tick or any daily loop; it fires
  exactly once per season, at the boundary roll only (KD-1/FR-FN-001).

## 5.5 #31 boundary — read-only lock (KD-3)

- **T-FN-BOUND-001** — `AvailableTransferBudget` never mutates the passed `ClubFinances` (a pure read) —
  FR-FN-012.
- **T-FN-BOUND-002** — #40's assembly references nothing in #31 (or #34/#45), and `ApplyTransaction` is
  called only from #31/#34's (future) call sites (asmdef-shape assertion) — FR-FN-013/027.
- **T-FN-BOUND-003** — `ApplyTransaction` never mutates `TransferBudget` or `WageBudget` — a cash line item
  (`TransferFee`/`General`) moves `Balance` only, a wage line item (`PlayerWage`/`StaffWage`) moves
  `WageBillAggregate` only; a season's worth of `ApplyTransaction` calls leaves both ceilings exactly as
  `SettleFinances` last set them — FR-FN-004/016.

## 5.6 Ledger correctness, T3a revenue & integer currency

- **T-FN-LEDGER-001** — `ApplyTransaction(Debit, TransferFee)` decreases `Balance` by `Amount` and leaves
  `WageBillAggregate` unchanged.
- **T-FN-LEDGER-002** — `ApplyTransaction(Debit, PlayerWage/StaffWage)` increases `WageBillAggregate` by
  `Amount` and leaves `Balance` **unchanged** (a wage transaction moves the liability, not cash); a
  subsequent `Credit` of the same `LineItem`/`Amount` decreases `WageBillAggregate` back to its prior value,
  `Balance` still unchanged.
- **T-FN-LEDGER-003** — `Balance` may go negative (debt) without failing; a debt-driving transaction does not
  throw (FR-FN-011's integer posture does not imply a non-negativity constraint on `Balance`).
- **T-FN-LEDGER-004** — A Credit wage-reversal larger than the current `WageBillAggregate` fails loud (F1)
  rather than driving the aggregate negative (FR-FN-016).
- **T-FN-REV-001** — Enabled T3a `AccrueDailyRevenue` adds `sponsorshipRevenue + matchdayRevenue` exactly
  once to both `Balance` and `SeasonRevenueAccrued`, while `TransferBudget`, `WageBudget`,
  `WageBillAggregate`, and `FfpBalanceWindow` remain field-identical (FR-FN-003 / §3.4.1).
- **T-FN-REV-002** — With T3a enabled, a negative sponsorship or matchday component fails loud (F8) rather
  than turning the revenue path into an implicit expenditure channel.
- **T-FN-REV-003** — `SettleFinances` given a non-zero `prior.SeasonRevenueAccrued` MUST return
  `SeasonRevenueAccrued = 0`, while carrying `WageBillAggregate` and `FfpBalanceWindow` field-identically.
  This closes the current-season accumulator at the boundary without inventing the future FFP-window rule;
  the Stage-2 zero-accumulator case remains exactly behaviour-neutral (FR-FN-005 / §3.1).
- **T-FN-INT-001** — Every `ClubFinances`/`FinanceTransaction`/`BoardModifier` field is an integer type; no
  accounting formula (`PrizeMoneyForPosition`, the budget-ceiling projection, `ApplyTransaction`,
  `AccrueDailyRevenue`) introduces a float — a static/reflection-level assertion mirroring #41's integer
  posture (FR-FN-011).
- **T-FN-INT-002** — Board scaling MUST not overflow before the upper budget cap can apply. A base ceiling
  derived from accepted signed-Int32 tuning extremes and a positive non-identity board multiplier that would
  overflow a direct `baseCeiling * multiplier` MUST return `CLUB_FINANCES_BUDGET_CEILING_MAX` without an
  `OverflowException`; a representative below-cap case MUST retain the exact integer-floor result from
  `baseCeiling * multiplier / PERMILLE_DENOM` (FR-FN-011, §3.1).
- **T-FN-INT-003** — T3a independently locks every checked-arithmetic failure site: overflow while summing
  the two revenue components, overflow adding a safe daily total to `Balance`, and overflow adding a safe
  daily total to `SeasonRevenueAccrued` each MUST throw `OverflowException` without exposing partial state
  (F9 / FR-FN-011 / §3.4.1).

## 5.7 FFP/board seam & fail-loud

- **T-FN-MOD-001** — `BoardModifier.Identity` yields the exact Stage-2 budget-ceiling projection (×1.0) —
  KD-4/KD-8.
- **T-FN-FAIL-BOARD-001** — Any `BoardModifier` with `BudgetMultiplierMillPermille <= 0`, including
  `default(BoardModifier)` and an explicit negative multiplier, reaching `SettleFinances` fails loud —
  FR-FN-018/F4. The clamp is not used to legitimize an invalid board multiplier.
- **T-FN-FAIL-001** — Bad `FINANCE_SAVE_FORMAT_VERSION` → fail loud (F3).
- **T-FN-FAIL-002** — Out-of-bounds length prefix / trailing bytes → fail loud (F5).
- **T-FN-FAIL-003** — `TransferBudget` or `WageBudget` negative reaching a consuming seam (e.g. a corrupted
  restore) → fail loud (F1).
- **T-FN-FAIL-004** — `ApplyTransaction` with a negative `Amount`, or an out-of-contract `Kind`/`LineItem`
  byte on restore, → fail loud (F2).
- **T-FN-FAIL-005** — `finalTablePosition` outside `[1, clubCount]` passed to `SettleFinances` → fail loud
  (F7).
- **T-FN-FAIL-006** — `SettleFinances`/`ApplyTransaction` invoked for a `ClubId` with no `ClubFinances` entry
  → fail loud (F6) — duplicate coverage with T-FN-FAIL-CLUB-001 by design (both the general fail-loud sweep
  and the club-lifecycle suite lock it).

## 5.8 FR traceability

| FR | Covering test(s) |
|---|---|
| FR-FN-001 | T-FN-ORD-003 |
| FR-FN-002 | T-FN-DET-001, T-FN-LIFE-001 |
| FR-FN-003 | T-FN-BOUND-003, T-FN-REV-001 |
| FR-FN-004 | T-FN-BOUND-003 |
| FR-FN-005 | T-FN-REV-003, worked-example lock (§3.5) |
| FR-FN-006 | T-FN-NEU-001 |
| FR-FN-007 | T-FN-FAIL-005 |
| FR-FN-008 | T-FN-DET-004, T-FN-DET-005 |
| FR-FN-009 | T-FN-DET-004 |
| FR-FN-010 | (deferred to stochastic T3 slice — recorded in §7) |
| FR-FN-011 | T-FN-INT-001, T-FN-INT-002, T-FN-INT-003 |
| FR-FN-012 | T-FN-BOUND-001 |
| FR-FN-013 | T-FN-BOUND-002 |
| FR-FN-014 | T-FN-FAIL-004 |
| FR-FN-015 | T-FN-NEU-002 |
| FR-FN-016 | T-FN-LEDGER-002, T-FN-LEDGER-004 |
| FR-FN-017 | (deferred to later T3 FFP slice — recorded in §7) |
| FR-FN-018 | T-FN-MOD-001, T-FN-FAIL-BOARD-001 |
| FR-FN-019 | (structural — no #45 interface exists to test against; asserted by assembly-reference absence, T-FN-BOUND-002-class) |
| FR-FN-020 | T-FN-FAIL-001 |
| FR-FN-021 | T-FN-DET-001 |
| FR-FN-022 | T-FN-FAIL-001, T-FN-FAIL-002 |
| FR-FN-023 | T-FN-ORD-001 |
| FR-FN-024 | T-FN-DET-002 |
| FR-FN-025 | T-FN-LIFE-001, T-FN-FAIL-CLUB-001 |
| FR-FN-026 | T-FN-NEU-002 (`FinancesViewModel` shape locked alongside the identity state) |
| FR-FN-027 | T-FN-BOUND-002 |
| FR-FN-028 | T-FN-NEU-001, T-FN-NEU-002, T-FN-NEU-003, T-FN-NEU-004 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (T-FN-*) + full FR-FN-001..028 traceability table. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): T-FN-LEDGER-002 / T-FN-BOUND-003 restated — wage transaction moves the aggregate only, `Balance` unchanged. |
| 0.3 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** T-FN-FAIL-BOARD-001 widened from default/zero only to every non-positive board multiplier. |
| 0.4 | 2026-09-07 | — | **PR #363 Codex correction.** Adds T-FN-INT-002 to lock overflow-safe pre-cap board scaling and exact below-cap integer-floor semantics. |
| 0.5 | 2026-09-11 | — | **PR #392 review follow-up.** T-FN-LIFE-001 remains one acceptance id, but its executable evidence is phase-split: T2a covers canonical bootstrap/universe coherence; T2b must cover unchanged entry cardinality across season rolls after live #30 wiring exists. |
| 0.6 | 2026-09-11 | — | **ERR-030-051 / T2b executable-contract correction.** T-FN-DET-002 removes the unsupported mid-call save premise and instead locks synchronous all-or-nothing boundary semantics; T-FN-LIFE-001 names the live across-roll regression evidence. |
| 0.7 | 2026-09-11 | OpenAI | **T3a acceptance back-prop.** Adds identity-off, revenue-field isolation, negative-component and all three overflow-site locks; updates FR-FN-003/011/028 traceability while keeping stochastic FR-FN-010 and FFP FR-FN-017 deferred. |
| 0.8 | 2026-09-11 | OpenAI | **T3a lifecycle acceptance.** Adds T-FN-REV-003 for boundary reset of current-season revenue while carrying the future FFP window; FR-FN-005 traceability now includes that executable lock. |
#endregion