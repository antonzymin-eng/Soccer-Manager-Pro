# Club Finances & Economy #40 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**
- **FR-FN-001** — `SettleFinances` MUST run only at the season-boundary roll (KD-1/KD-6); #40 MUST NOT run a
  per-day step at Stage 2 and MUST NOT read or advance the world tick or the 10 Hz/60 Hz match loops.
- **FR-FN-002** — Per-club `ClubFinances` (`Balance`, `TransferBudget`, `WageBudget`, `WageBillAggregate`,
  and the deep-tier `SeasonRevenueAccrued`/`FfpBalanceWindow` accumulators) is #40-owned state, keyed by
  `ClubId`, serialized under #40's sub-blob (KD-7). It is the single source of truth for a club's financial
  position.
- **FR-FN-003** — `SettleFinances` MUST be the sole entry point that sets `TransferBudget`/`WageBudget` and
  adds to `Balance` at the season boundary; `ApplyTransaction` MUST be the sole entry point that mutates
  `Balance`/`WageBillAggregate` between boundaries (KD-3/KD-5). `AvailableTransferBudget` (FR-FN-012) and
  `FinancesViewModel` (FR-FN-026) MUST NOT mutate state.
- **FR-FN-004** — `ApplyTransaction` MUST NOT mutate `TransferBudget` or `WageBudget`; those ceilings are set
  exclusively by `SettleFinances` once per season (KD-1/KD-3) — Stage 2 has no "remaining budget net of
  spend" running total (§1.6).

**Budget projection semantics (KD-1/KD-6)**
- **FR-FN-005** — `SettleFinances` MUST carry `prior.Balance` forward and **ADD** the position-keyed prize
  money to it (never overwrite); it MUST **SET** (overwrite) `TransferBudget` and `WageBudget` to the newly
  projected ceilings.
- **FR-FN-006** — The Stage-2 budget projection MUST be `budget = f(finalTablePosition, prizeMoney)` — a
  pure integer function of the final league position (and the fixed prize-money table it derives from) —
  with no per-day step and no per-day accrual state at Stage 2 (KD-1).
- **FR-FN-007** — `finalTablePosition` MUST be in `[1, clubCount]`; a value outside that range reaching
  `SettleFinances` MUST **fail loud** (F7).

**Determinism (KD-2)**
- **FR-FN-008** — The minimal tier MUST NOT register an RNG stream; `SettleFinances` MUST be a pure
  deterministic function of its parameters (no hidden state, no draw).
- **FR-FN-009** — `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` MUST remain **RESERVED, not
  promoted**, until #40's T3 deep-tier stochastic sponsorship-variance draw exists (ERR-040-001 adds the
  placeholder row at approval — the #29 `0x21`-stays-reserved precedent).
- **FR-FN-010** — When #40's T3 deep-tier draw is added, it MUST be **keyed** on `(clubId, seasonNumber,
  purpose)` — position-independent, no free-running cursor — so nothing beyond `ClubFinances` itself is ever
  serialized for it (the #28/#41 keyed-draw precedent).

**Integer currency**
- **FR-FN-011** — All currency-bearing fields (`Balance`, `TransferBudget`, `WageBudget`,
  `WageBillAggregate`, `SeasonRevenueAccrued`, `FfpBalanceWindow`) and every accounting formula (prize-money
  interpolation, budget-ceiling projection, `ApplyTransaction`) MUST be integer; no float MUST appear
  anywhere in the accounting path (the #28/#29/#41 integer-projection posture).

**#31 boundary — read-only query + one-way command (KD-3)**
- **FR-FN-012** — `AvailableTransferBudget` MUST be a pure read-only query (returns `TransferBudget`); it
  MUST NOT mutate the passed `ClubFinances`.
- **FR-FN-013** — `ApplyTransaction` MUST be the **single** ledger-mutation path #31 (and, later, #34) call
  on a committed deal/contract; #40 MUST NOT reference #31 or #34 — the one-way `#31 → #40` / `#34 → #40`
  composition.
- **FR-FN-014** — A malformed `FinanceTransaction` (a negative `Amount`, or an out-of-contract `Kind`/
  `LineItem` enum value) reaching `ApplyTransaction` MUST **fail loud** (F2) rather than being silently
  clamped or ignored.

**Wage ledger ownership (KD-5)**
- **FR-FN-015** — `WageBillAggregate` MUST be `0` at Stage 2 (no #31/#34 producer exists yet, KD-5); #40 owns
  the canonical aggregate, and #31/#34 MUST NOT maintain a parallel wage total of their own.
- **FR-FN-016** — A wage-classified `ApplyTransaction` (`LineItem ∈ {PlayerWage, StaffWage}`) MUST update
  the wage **liability** `WageBillAggregate` **only** — a Debit (contract sign/raise) increases it, a Credit
  (termination/reduction) decreases it — and MUST NOT move `Balance` (a wage transaction changes the ongoing
  wage bill, not cash; the periodic wage cash-out that debits `Balance` is a deferred deep-tier accrual, §7,
  so `WageBillAggregate` stays the *current total* wage bill rather than an unbounded sum of payments). A
  cash-classified transaction (`LineItem ∈ {TransferFee, General}`) MUST move `Balance` **only** and MUST NOT
  touch `WageBillAggregate`. A Credit wage reversal larger than the current aggregate MUST **fail loud** (F1)
  rather than driving it negative.

**FFP & board modulation (KD-4)**
- **FR-FN-017** — The (deep-tier, deferred) FFP penalty MUST be a **soft** adjustment to the *next* season's
  projected budget, never a hard gate that could deadlock a club unable to sell (KD-4).
- **FR-FN-018** — `BoardModifier` MUST compose multiplicatively with the FFP term (both defaulting to
  identity at Stage 2); `BoardModifier.Identity` MUST be an **explicit factory**
  (`BudgetMultiplierMillPermille = 1000`), and `default(BoardModifier)` (all-zero, ×0) MUST NOT be treated
  as a valid runtime value — reaching `SettleFinances` it MUST **fail loud** (F4, the #41 `MedicalModifier`
  zero-value-trap lesson applied here per §1.6).
- **FR-FN-019** — No #45 interface MUST be built ahead of #45 landing (FR-LW-031); #45 becomes the producer
  of a non-identity `BoardModifier` when it exists.

**Persistence (KD-7)**
- **FR-FN-020** — `FINANCE_SAVE_FORMAT_VERSION` [FIXED] = 1; #40's state lands as an opaque, independently
  version-gated sub-blob under #30's season save (`SeasonSaveCodec` pattern), **not** a
  `WORLD_STORE_FORMAT_VERSION` bump.
- **FR-FN-021** — Every `ClubFinances` field, per `ClubId`, MUST be serialized and round-trip
  field-identical; **serialize, don't regenerate** (#30 KD-5). No RNG cursor exists to serialize at Stage 2
  (FR-FN-009/010).
- **FR-FN-022** — Restore MUST **fail loud** on version mismatch / out-of-bounds length prefix (overflow-
  safe `ReadCount`) / trailing bytes (F3/F5).

**Season-boundary integration (KD-6)**
- **FR-FN-023** — #40's `SettleFinances` step MUST be invoked at #30's **new** reserved slot (b'),
  positioned **after** the (a') #43 promotion/relegation insertion point and **before** (c) regenerate, in
  `RollToNextSeason()` (the ERR-030-003 back-prop) — never reordered ahead of (a')/(b) nor after (c).
- **FR-FN-024** — The finance-settlement transform MUST remain a pure function of `SeasonState + nextSeed`
  (with the per-club `ClubFinances` prior state), so the FR-SN-029 restartable/round-trip contract holds
  through the added step — a save taken mid-`RollToNextSeason()` (before or after step (b')) restores to the
  same continuation.

**Club lifecycle & availability/observers (KD-7/KD-8)**
- **FR-FN-025** — A club's `ClubFinances` entry, once created (`CreateInitial`), MUST persist across every
  season boundary unconditionally — clubs do not churn like players (KD-7); `SettleFinances`/
  `ApplyTransaction` invoked for a `ClubId` with no entry MUST **fail loud** (F6, a bootstrap/lifecycle bug,
  never auto-created).
- **FR-FN-026** — A read-only `FinancesViewModel` (value copies: balance / transfer budget / wage budget /
  wage bill) MUST be exposed for #38.

**Reference direction & neutrality (KD-8)**
- **FR-FN-027** — The reference direction MUST stay one-way: `#30 → #40 → {#27, #16}` and `#31 → #40` (and,
  later, `#34 → #40` / `#45 → #40`); #40's assembly MUST NOT reference `MatchEngine`, `LivingWorld`,
  `SeasonSave`, #30, #31, #34, or #45. #27's assembly stays schema-untouched.
- **FR-FN-028** — Behaviour-neutral identity: with the deep dials off, `BoardModifier.Identity`, and zero
  deep-tier accumulators, `SettleFinances` MUST yield **exactly** `budget = f(finalTablePosition,
  prizeMoney)`; registering #40's reserved namespace slot MUST leave every existing stream's cursor
  byte-identical (the #22/#26/#28/#29/#41 stream-independence precedent).

## 2.2 Data structures

```csharp
public enum FinanceTransactionKind : byte { Debit = 0, Credit = 1 }
public enum FinanceLineItem : byte { General = 0, TransferFee = 1, PlayerWage = 2, StaffWage = 3 }

// #40-owned per-club season-boundary financial state (serialized, KD-7). Integer currency units
// throughout — never float (FR-FN-011).
public struct ClubFinances
{
    public long Balance;             // signed — may be negative (debt); no F1 gate on Balance itself
    public long TransferBudget;      // spending ceiling #31 reads (>= 0, clamped, F1); SET only by SettleFinances
    public long WageBudget;          // wage-ceiling #31/#34 read (>= 0, clamped, F1); SET only by SettleFinances
    public long WageBillAggregate;   // sum of committed wage line items (0 at Stage 2 — no producer yet, KD-5); >= 0 (F1)
    // deep-tier accumulators (0 at Stage 2 -> minimal identity, KD-8/FR-FN-028):
    public long SeasonRevenueAccrued;
    public long FfpBalanceWindow;

    public static ClubFinances CreateInitial(long startingBalance) =>
        new() { Balance = startingBalance, TransferBudget = 0, WageBudget = 0,
                WageBillAggregate = 0, SeasonRevenueAccrued = 0, FfpBalanceWindow = 0 };
}

// KD-4 board routing seam — identity until #45 lands. Per-mille integer multiplier (1000 = x1.0) so the
// budget projection stays integer-only (FR-FN-011). Identity is an EXPLICIT factory — default() (all-zero,
// x0) is NOT a valid runtime value; it MUST fail loud at SettleFinances (FR-FN-018 / F4), mirroring #41's
// MedicalModifier Identity-vs-default() lesson (§1.6).
public readonly struct BoardModifier
{
    public readonly int BudgetMultiplierMillPermille;   // 1000 = x1.0; > 1000 raises the projected ceilings
    public static BoardModifier Identity => new(1000);
    public BoardModifier(int mult) { BudgetMultiplierMillPermille = mult; }
}

// The season-boundary step (KD-1/KD-6, invoked at #30's new slot (b')): pure budget projection from the
// final table. Carries prior.Balance forward, ADDS position-keyed prize money to Balance, and SETS the
// season's TransferBudget/WageBudget ceilings. Fully deterministic — no RNG parameter at minimal (KD-2).
public static ClubFinances SettleFinances(in ClubFinances prior, int finalTablePosition, int clubCount,
                                          in BoardModifier board);

// KD-3/KD-5 — the SINGLE ledger-mutation path between season boundaries: #31 (transfers/player wages) and
// #34 (staff wages) call this; #40 owns the ledger, callers never write ClubFinances fields directly.
// Fails loud on a malformed transaction (F2) or a wage-reversal larger than the current aggregate (F1).
public static void ApplyTransaction(ref ClubFinances f, in FinanceTransaction txn);

// The transaction value #31/#34 construct. Amount is an unsigned MAGNITUDE — sign is carried by Kind, never
// by Amount's own sign (a negative Amount is malformed, F2).
public readonly struct FinanceTransaction
{
    public readonly FinanceTransactionKind Kind;
    public readonly FinanceLineItem LineItem;
    public readonly long Amount;    // >= 0 (F2 gate)
    public FinanceTransaction(FinanceTransactionKind kind, FinanceLineItem lineItem, long amount)
    { Kind = kind; LineItem = lineItem; Amount = amount; }
}

// KD-3 read-only constraint query — #31 reads its spending ceiling; never a mutation.
public static long AvailableTransferBudget(in ClubFinances f) => f.TransferBudget;

// KD-8 observer surface for #38 (value copies).
public readonly struct FinancesViewModel { /* Balance / TransferBudget / WageBudget / WageBillAggregate */ }
```

The **finance block** persisted under `FINANCE_SAVE_FORMAT_VERSION` is, per club: `ClubFinances` keyed by
`ClubId`. **No RNG cursor is serialized** — the minimal tier registers no stream at all (FR-FN-008/009), so
there is nothing beyond `ClubFinances` to persist. The set tracks the stable `ClubId` universe (FR-FN-025) —
unlike #28/#41's per-`PlayerId` roster churn, entries are never removed by a season roll.

`SettleFinances` is the sole season-boundary mutating entry point (FR-FN-003); `ApplyTransaction` is the sole
between-boundary mutating entry point; `AvailableTransferBudget` and `FinancesViewModel` construction are
pure reads over a `ClubFinances` value. See §3.

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | `ClubFinances` coherence violated — `TransferBudget < 0`, `WageBudget < 0`, or `WageBillAggregate < 0` reaching a consuming seam (incl. a Credit wage-reversal larger than the current aggregate) | **Fail loud** — an invalid combination is a bug, never silently clamped or repaired (the #27/#28/#41 F1-class precedent). |
| **F2** | `ApplyTransaction` invoked with a malformed `FinanceTransaction` (negative `Amount`, or an out-of-contract `Kind`/`LineItem` value) | **Fail loud** — magnitude/enum validity is a caller-contract bug, never defaulted. |
| **F3** | `FINANCE_SAVE_FORMAT_VERSION` mismatch on restore | **Fail loud** (`ArgumentException`), the `MatchSaveCodec` posture. |
| **F4** | A `BoardModifier` with `BudgetMultiplierMillPermille == 0` (e.g. `default(BoardModifier)`) reaching `SettleFinances` | **Fail loud** — a zero multiplier is a caller-contract bug (×0 budget), not a legitimate "no adjustment" identity (the #41 `MedicalModifier` zero-value-trap precedent, §1.6). |
| **F5** | Corrupt length prefix (out-of-bounds) or trailing bytes in the finance block | **Fail loud** (overflow-safe bound; the `WorldStateSerializer.ReadCount` posture). |
| **F6** | `SettleFinances` or `ApplyTransaction` invoked for a `ClubId` with no `ClubFinances` entry | **Fail loud** — clubs do not churn (KD-7), so a missing entry is a bootstrap/lifecycle bug, never auto-created. |
| **F7** | `finalTablePosition` outside `[1, clubCount]` passed to `SettleFinances` | **Fail loud** (`ArgumentException`) — an out-of-range position is a caller bug, never clamped. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-FN-001..028), data structures, F1..F7. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): FR-FN-016 — a wage `ApplyTransaction` moves the `WageBillAggregate` liability ONLY (not `Balance`); cash items (`TransferFee`/`General`) move `Balance` only. |
#endregion
