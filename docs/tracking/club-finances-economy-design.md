# Club Finances & Economy #40 — Design Supplement

> **Created:** July 23, 2026
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #40 · **FR prefix:** FR-FN (grep-verified unclaimed across `docs/specs/**`).
> **Master-plan home:** §5 Stage 3 financials · **Wave:** 2.
> **Determinism:** **minimal tier is fully deterministic — no draw. `_RESERVED_0x29_` / `SubsystemOrdinals`
> 91 stay RESERVED (NOT promoted)** until a deep-tier stochastic revenue draw exists (KD-2, the #29 `0x21`
> precedent).
> **Source plan:** `docs/tracking/spec-plans/spec-40-club-finances-economy.md` v0.2.

---

## 0. Scope

Per-club finances on the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz
match loops): transfer + wage **budgets**, a **wage ledger**, **revenue** (prize money / matchday /
sponsorship), and (deep tier) FFP-style balance constraints. Split from #31 so the economy is a system in
its own right rather than a transfer side-effect. It is the **counterparty-constraint layer** #31
negotiation reads.

**Out of scope (owned elsewhere, referenced as seams):**
- **Negotiation** (bids, contract terms) — #31 owns it and reads #40's budget as a constraint; #40 owns no
  negotiation logic (KD-3).
- **Board / ownership** (takeovers, confidence) — #45 owns it; #40 exposes an identity routing seam for a
  future budget adjustment (KD-4).
- **Staff wages as a mechanic** — #34 supplies staff line items; #40 owns the ledger that aggregates them
  (KD-5).
- **The day-advance / season loop that drives #40** — #30 owns it; #40 exposes the settlement step, #30
  invokes it at the season-boundary roll (never the reverse).

## 1. What exists vs. what #40 adds

**Exists (verified against source / approved specs):**
- `src/player-database/PlayerAttributes.cs` — 31 `int[1,20]` attributes; **no wage / value / contract
  field**. So the minimal-tier wage ledger has **no player-wage producer at Stage 2** (player wages arrive
  with #31 contracts; staff wages with #34) — the ledger is #40-owned and **empty at minimal**, populated by
  #31/#34 line-item commands when they land (KD-5, phantom-free).
- `docs/specs/season-competition-loop/` (#30, APPROVED) — the **season-boundary roll** `RollToNextSeason()`
  (§3.5 / FR-SN-029) is a single restartable transform `finalize table → board → (a' #43 insertion) →
  regenerate fixtures → advance ages [#28 null seam] → reset`; FR-SN-031 already establishes the precedent
  of a **well-defined insertion point** for a later spec's transform. `SeasonSaveCodec` composes opaque,
  independently version-gated sub-blobs. The final league table (which #40's budget projection reads) exists
  after step (a).
- `docs/specs/player-progression-lifecycle/` (#28, APPROVED) — the per-`PlayerId` career-state block
  composed as an opaque `PROGRESSION_SAVE_FORMAT_VERSION` sub-blob (the season-save-sub-blob convention #40
  follows, KD-7); its regen/retirement roster churn (FR-PG-011) is the roster-membership lifecycle #40 must
  track at club granularity.
- `docs/specs/deterministic-sim/section-3.md` — the off-pitch band is open; the roadmap §6 reserves
  **`0x29` / 91** for #40. No `_RESERVED_0x29_` placeholder row exists yet — #40's promotion **adds the
  placeholder row** (reserved, **not** a named tag), because the minimal tier has **no draw** (KD-2).

**#40 adds:** a per-club `ClubFinances` block (balance + transfer/wage budgets + wage-ledger aggregate +
deep-tier revenue/FFP accumulators); a **season-boundary** `SettleFinances` step (budget projection from the
final table) invoked at #30's roll (a new step via back-prop, KD-6); a read-only budget/constraint query
surface (#31/#34/#42 read) + a #40-owned `ApplyTransaction` command (the single ledger-mutation path); the
`FINANCE_SAVE_FORMAT_VERSION` sub-blob; and the **reserved-not-promoted** `0x29`/91 slot.

## 2. Staging (minimal-first → deep, one code path)

- **Stage-2 minimal** — `budget = f(final league position)`, a **pure deterministic** projection computed
  once per season at the boundary roll: prize money + a base budget keyed to finishing position → the
  transfer/wage spending ceilings #31 reads. **No per-day step, no draw, no revenue streams, no FFP.** The
  wage ledger exists structurally but is empty (no #31/#34 producers yet).
- **Stage-3 deep** — per-day revenue accrual (matchday/sponsorship), stochastic sponsorship variance (the
  first `0x29` draw), and an FFP soft-penalty that adjusts the **same** projected figure — each defaulting
  to its Stage-2 identity via a config dial. A club with no deep model configured yields **exactly** the
  minimal league-finish budget (KD-8).

**One code path (KD-8):** the deep revenue/FFP/board terms all default to their identities (dial off /
`BoardModifier.Identity` / no accrual), so the Stage-2 surface is the exact identity the deep tier modulates
— the #21/#28/#29 default-behaviour-neutral discipline.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #40** — the season-boundary roll *invokes* `SettleFinances(finalTable)`; the day-advance loop
  invokes the deep-tier per-day accrual at a reserved slot (deferred). #40 never references #30.
- **#31 → #40** — #31 **reads** the budget/constraint query and, on committing a deal, **calls** #40's
  `ApplyTransaction` command (the one ledger-mutation path). #40 **never** references #31 — one-way; #40
  owns its ledger, #31 is a caller (the #30-invokes-#28 precedent). No two-way write coupling (KD-3).
- **#40 → #27, #16** — reads `PlayerRecord` (roster membership per club); consumes the determinism namespace
  (a reserved, un-promoted slot — no stream registered at minimal).
- **#34 → #40 / #45 → #40** (future) — staff line items (#34) via `ApplyTransaction`; a non-identity
  `BoardModifier` (#45) via KD-4's routing seam. No #34/#45 interface built today.
- **#31/#34/#42/#38 read #40** — the read-only budget/constraint query + a `FinancesViewModel` observer.

Reference DAG: `#30 → {#28, #40}`, `#31 → #40`, `#40 → {#27, #16}`. **Acyclic.**

## 4. Persistent state & save impact (KD-7)

Adds an opaque, independently version-gated **finance sub-blob** (`FINANCE_SAVE_FORMAT_VERSION` [FIXED] = 1)
composed into #30's season save via the `SeasonSaveCodec` pattern — **not** `WORLD_STORE_FORMAT_VERSION`
(this supersedes the plan §4 guess; §7 KD-7 gives the rationale: #40 is management-layer state composed at
the season-save root, like #28/#29, not living-world state inside the `WorldStore` composite that FR-LW-003
walls off). The composing outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 exactly as
#28/#29 do; the codec never parses the sub-blob. Per club: `ClubFinances` — **integer** currency units
(never float — determinism), `Balance`, `TransferBudget`, `WageBudget`, the wage-ledger aggregate, and the
deep-tier revenue/FFP accumulators (zero at Stage 2). Fail-loud on version mismatch / out-of-bounds length
prefix (overflow-safe `ReadCount`) / trailing bytes (F3/F5). **Serialize, don't regenerate** (#30 KD-5);
every field round-trip-covered, including a **mid-season** and a **mid-boundary-roll** save (the FR-SN-029
restartable-transform contract).

## 5. Determinism (KD-2)

World tick only. **The minimal tier is fully deterministic and registers NO RNG stream:**
`budget = f(finalTablePosition, prizeMoney)` is a pure integer projection; there is no stochastic term. So
**`_RESERVED_0x29_` / `SubsystemOrdinals` 91 stay RESERVED — NOT promoted** at authoring (a
`_RESERVED_0x29_` placeholder row, the #29 `0x21` precedent). Promoting a named tag with a zero-draw stream
would be the phantom-surface class FR-LW-031 forbids (the `world.arcs` precedent).

The reservation stands for the **deep-tier stochastic sponsorship/revenue variance**, which is the genuine
first draw site; it promotes `DOMAIN_TAG_CLUB_FINANCES = 0x29` **then** (at #40 T3), keyed
position-independently on `(clubId, seasonNumber, purpose)` — the #28-regen / #30-quick-sim off-pitch
keyed-draw precedent (no free-running cursor, so no cursor to serialize). All currency arithmetic is integer;
no `DETERMINISM_DIGEST_VERSION` bump (a reserved-row placeholder + a future off-pitch stream).

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// #40-owned per-club world-tick financial state (serialized, KD-7). Integer currency units, never float.
public struct ClubFinances
{
    public long Balance;             // signed integer currency units (may be negative — debt, F1 gate below)
    public long TransferBudget;      // spending ceiling #31 reads (>= 0, clamped, F1)
    public long WageBudget;          // wage-ceiling #31 reads (>= 0, clamped)
    public long WageBillAggregate;   // sum of committed wage line items (0 at Stage 2 — no producer yet, KD-5)
    // deep-tier accumulators (0 at Stage 2 → minimal identity, KD-8):
    public long SeasonRevenueAccrued;
    public long FfpBalanceWindow;

    public static ClubFinances CreateInitial(long startingBalance) =>
        new() { Balance = startingBalance, TransferBudget = 0, WageBudget = 0,
                WageBillAggregate = 0, SeasonRevenueAccrued = 0, FfpBalanceWindow = 0 };
}

// The season-boundary step (KD-6, invoked at #30's roll): pure budget projection from the final table.
// Carries `prior.Balance` forward, ADDS position-keyed prize money to Balance, and SETS the season's
// TransferBudget/WageBudget ceilings. Fully deterministic (no RNG parameter at minimal — KD-2).
public static ClubFinances SettleFinances(in ClubFinances prior, int finalTablePosition, int clubCount,
                                          in BoardModifier board);

// The SINGLE ledger-mutation path (KD-3/KD-5): #31 (transfers/player wages) and #34 (staff wages) call this;
// #40 owns the ledger, callers never write fields directly. Fail-loud on a malformed transaction (F2).
public static void ApplyTransaction(ref ClubFinances f, in FinanceTransaction txn);

// KD-3 read-only constraint query — #31 reads its spending ceiling; never a mutation.
public static long AvailableTransferBudget(in ClubFinances f) => f.TransferBudget;

// KD-4 board routing seam — identity until #45 lands (no phantom #45 interface).
public readonly struct BoardModifier { public static BoardModifier Identity => default; }  // ×1.0 budget

// KD-8 observer surface for #38 (value copies).
public readonly struct FinancesViewModel { /* balance / budgets / wage bill */ }
```

## 7. Key design decisions

- **KD-1 (accounting cadence — season-boundary minimal, per-day deep).** The minimal tier settles **once per
  season at the boundary roll** (`SettleFinances`, the #28 `RunSeasonBoundary` precedent) — a single budget
  figure read as a constant during the season. There is **no per-day step** and no per-day accrual state at
  minimal, keeping serialized state tiny. The deep-tier per-day revenue accrual adds a **daily** slot (a
  future #30 tick-order back-prop analogous to #41's, deferred here — the minimal tier does not need it),
  and its accumulators default to zero (KD-8). This split keeps the minimal surface a pure boundary
  transform.

- **KD-2 (minimal is pure, no draw — reserve, don't promote).** The minimal budget is a pure deterministic
  function; #40 registers **no** RNG stream and keeps `0x29`/91 **reserved** (§5). Only the deep-tier
  stochastic revenue variance is a genuine draw site, promoting the tag then. The #29 `0x21`-stays-reserved
  precedent exactly.

- **KD-3 (#31 boundary — read-only constraint + one-way command, no two-way coupling).** #40 exposes (a) a
  **read-only** budget/constraint query #31 reads, and (b) a **#40-owned** `ApplyTransaction` command #31
  invokes to move money on a committed deal. #40 **never references #31**; the ledger mutation lives on #40,
  the decision on #31 — one-way `#31 → #40`. This is the roadmap §5 "define the projection direction, avoid
  two-way coupling" invariant satisfied by making #40 the single ledger owner and #31 a caller.

- **KD-4 (FFP — deep-tier soft penalty, board routing seam).** FFP is a **soft penalty on the next season's
  projected budget** (not a hard gate — a hard gate would deadlock a club that cannot sell), composing
  multiplicatively with a future #45 `BoardModifier`. Both default to identity (×1.0) at Stage 2; **no #45
  interface is built** (FR-LW-031). One code path.

- **KD-5 (wage-ledger ownership — #40 owns, sources write line items).** #40 owns the **canonical** wage
  ledger; #31 (player contracts) and #34 (staff contracts) contribute line items via `ApplyTransaction`
  (they call #40; #40 aggregates into `WageBillAggregate`). **At Stage 2 the ledger is empty** — #27 has no
  wage field and #31/#34 do not exist yet — so the aggregate is 0 and behaviour-neutral; the structure is
  #40-owned and ready for its producers (phantom-free).

- **KD-6 (#30 season-boundary integration — a back-prop, not a #30 rewrite).** #40's `SettleFinances` runs
  at #30's `RollToNextSeason()` (§3.5 / FR-SN-029), which today enumerates `finalize (a) → board (b) →
  [#43 insertion a'] → regenerate (c) → advance-ages [#28 null seam] (d) → reset (e)`. #40's promotion files
  a **#30 back-prop (ERR-030-003)** inserting a **finance-settlement step (b')** — a new lettered step in
  the enumerated transform, the FR-SN-031 well-defined-insertion-point precedent — leaving the surrounding
  steps unchanged. **Ordering rationale (pinned, and forward-compatible with #43):** the finance step runs
  after `finalize` (a) (it needs the final table) and after `board` (b); **critically it is positioned
  AFTER the FR-SN-031 (a') #43 promotion/relegation insertion point**, because the budget depends on the
  club's **post-promotion division** — so when #43 lands its transform stays at (a') and #40's finance step
  at (b') reads the division #43 produced (no collision, correct dependency: `… board (b) → #43 promo/rel
  (a') → #40 finance (b') → regenerate (c) …`). It runs before `regenerate`/`reset` so the new season opens
  with settled budgets. The transform stays a pure function of `SeasonState + nextSeed`, so the FR-SN-029
  restartable/round-trip contract holds through the added step.

- **KD-7 (persistence — season-save sub-blob; supersedes the plan's `WORLD_STORE_FORMAT_VERSION`).**
  `FINANCE_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump. Rationale: #40 is **management-layer** state composed at the season-save
  root, exactly like #28's career-state block — the plan §4's "lives in the world store" conflates
  cross-season persistence with the living-world `WorldStore`, but FR-LW-003 walls the living-world assembly
  off from management concepts, and #28/#29 established the season-save-sub-blob home for precisely this
  class of per-club career state. Fail-loud gates; serialize-don't-regenerate. **Roster-membership
  lifecycle:** the per-club block is keyed by `ClubId` (stable — clubs do not churn like players), so it is
  simpler than #28/#41's per-`PlayerId` churn; a club's finances persist across seasons unconditionally
  (F6).

- **KD-8 (behaviour-neutral identity).** Deep dials off + `BoardModifier.Identity` + zero accumulators + an
  empty wage ledger ⇒ `SettleFinances` yields exactly `budget = f(final position)` and #40 registers no
  stream (existing streams' cursors byte-identical). A club with no deep model configured is the minimal
  league-finish budget, digest-locked. The deep tier extends this identity, never rewrites it.

## 8. Test focus

- **Behaviour-neutral identity proof** — an S3-revenue-unconfigured club yields **exactly** the minimal
  `budget = f(final position)`; adding #40 registers no stream, so existing streams' cursors are
  byte-identical (stream independence).
- **Save→restore round-trip** across a **mid-season** boundary AND a **mid-boundary-roll** boundary (the
  FR-SN-029 restartable contract) — every `ClubFinances` field restores field-identical; two-run determinism
  of a full season's accounting from one world seed.
- **One-way #31 boundary lock** — the budget query is read-only; the only mutation is `ApplyTransaction`;
  #40's assembly references nothing in #31 (asserted structurally).
- **Ledger correctness** — `ApplyTransaction` of a debit/credit updates `Balance` + `WageBillAggregate`
  consistently (integer arithmetic, no float); a malformed transaction fails loud (F2).
- **Integer currency (no float)** — all currency fields are integer; no accounting path introduces a float.
- **Fail-loud** — bad `FINANCE_SAVE_FORMAT_VERSION`, out-of-bounds length prefix, trailing bytes; a budget
  query before the finance block is initialized, and a `TransferBudget`/`WageBudget` driven negative, fail
  loud (F1/F3/F5).

## 9. Risks

- **Projection-direction coupling with #31 (headline).** Resolved by KD-3: #40 is the single ledger owner
  exposing a read-only query + a one-way command; #40 references nothing in #31. No two-way write coupling.
- **Save-home inconsistency.** Resolved by KD-7 reconciling the plan's `WORLD_STORE_FORMAT_VERSION` guess to
  the #28/#29 season-save-sub-blob convention (cross-checked at the section-file stage).
- **Phantom producers.** Mitigated by KD-5/KD-4: the wage ledger is empty and the board modifier is identity
  until #31/#34/#45 land — no interface built ahead of a producer (FR-LW-031).
- **Float in currency (a determinism trap).** Mitigated by KD-7: integer currency units throughout;
  §8 locks it.
- **Deferred extensions (recorded, not built):** per-day revenue accrual (a daily #30 tick-order slot, the
  #41 pattern), stochastic sponsorship variance (the first `0x29` draw), FFP soft-penalty, board (#45)
  modulation, and player/staff wage producers (#31/#34) — all default to their Stage-2 identities.

## 10. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-FN-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; flip `SPEC_INDEX.md` row.
4. #16 §3.4: **ERR-040-001** adds the `_RESERVED_0x29_` placeholder row / `SubsystemOrdinals` 91 (reserved,
   **not** promoted — minimal has no draw; promotes at #40 T3's first stochastic-revenue draw). #30:
   **ERR-030-003** inserts the finance-settlement step (b') after the FR-SN-031 (a') #43 point, before
   regenerate, into the FR-SN-029 boundary roll.
5. T-phase implementation (post-APPROVED): T0 value types + deterministic `SettleFinances` → T1
   `FINANCE_SAVE_FORMAT_VERSION` sub-blob + season-save composition → T2 `SettleFinances` wired at #30's
   boundary roll + `ApplyTransaction` command → T3 deep revenue accrual / FFP / board modulation (promotes
   `0x29`, adds the per-day slot).

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.2. |
| v0.2 | July 23, 2026 | AR-1 (1M+1L): **M1 (extensibility/ordering)** — KD-6 placed #40's settlement in the same finalize→regenerate region FR-SN-031 reserves for #43 promotion/relegation without pinning order; since budget depends on post-promotion division, the finance step (b') is now explicitly positioned AFTER the (a') #43 point (`board → #43 (a') → finance (b') → regenerate`), forward-compatible and collision-free. L1 §6 `SettleFinances` doc — clarified it carries Balance forward + adds position-keyed prize money + sets budget ceilings. |
