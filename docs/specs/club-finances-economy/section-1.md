# Club Finances & Economy #40 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Introduction

Club Finances & Economy gives each club a per-club `ClubFinances` record — balance, transfer/wage spending
**budgets**, a wage **ledger**, and (deep tier) revenue/FFP accumulators — settled once per season at the
**season-boundary roll**, not the world tick and not the 10 Hz tactical / 60 Hz physics match loops. The
minimal tier is a **pure deterministic projection**: `budget = f(finalTablePosition, prizeMoney)`, computed
once per season from the final league table. Split out of #31 Transfer Market so the economy is a system in
its own right rather than a transfer side-effect (the design supplement's own framing) — #40 is the
**counterparty-constraint layer** #31's negotiation logic reads, never the negotiation logic itself.

## 1.2 Scope

**In scope:** the per-club `ClubFinances` record (balance, transfer/wage budget ceilings, wage-ledger
aggregate, deep-tier revenue/FFP accumulators); the season-boundary `SettleFinances` step (budget projection
from the final table); the single ledger-mutation entry point `ApplyTransaction`; a read-only
`AvailableTransferBudget` constraint query (#31/#34/#42 read); a read-only `FinancesViewModel` observer for
#38; the persistent finance sub-blob under #30's season save; and the **reserved, not promoted**
`_RESERVED_0x29_` / `SubsystemOrdinals` 91 namespace slot.

**Out of scope (owned elsewhere, referenced as seams):**
- **Negotiation** (bids, contract terms) — #31 owns it and reads #40's budget as a constraint; #40 owns no
  negotiation logic (KD-3).
- **Board / ownership** (takeovers, confidence) — #45 owns it; #40 exposes an identity routing seam
  (`BoardModifier`) for a future budget adjustment (KD-4).
- **Staff wages as a mechanic** — #34 supplies staff line items; #40 owns the ledger that aggregates them
  (KD-5).
- **The season loop that drives #40** — #30 owns `RollToNextSeason()`; #40 exposes the settlement step, #30
  invokes it at the season-boundary roll (never the reverse).
- **The final league table itself, and promotion/relegation** — #30 owns table finalization; #43 (future)
  owns promotion/relegation; #40 reads the *result* (`finalTablePosition`, post-promotion) as an input.

## 1.3 Dependencies

| Spec | Relationship | Direction |
|---|---|---|
| #27 Squad/Player Data | reads the `Squad.ClubId` enumeration to know the stable set of clubs requiring a `ClubFinances` entry (F6) | #40 → #27 |
| #16 Deterministic Sim | consumes the determinism namespace (a **reserved, un-promoted** slot at minimal — no stream registered) | #40 → #16 |
| #30 Season & Competition Loop | invokes `SettleFinances` at the season-boundary roll's new step (b'); creates the per-club `ClubFinances` entry (`CreateInitial`) at club/league bootstrap | #30 → #40 |
| #31 Transfer Market (future) | reads `AvailableTransferBudget`; calls `ApplyTransaction` on a committed deal | #31 → #40 |
| #34 Staff (future) | calls `ApplyTransaction` for staff wage line items | #34 → #40 |
| #45 Board & Ownership (future) | supplies a non-identity `BoardModifier` when it lands | #45 → #40 |
| #42 (future, if a squad-planning/financial-projection consumer exists) | reads the read-only budget/constraint query | #42 reads #40 |
| #38 UI/Client (future) | reads the `FinancesViewModel` observer (value copies) | #38 reads #40 |
| #43 Promotion/Relegation (future) | its transform (inserted at #30's step (a')) produces the post-promotion division/`finalTablePosition` that #40's step (b') reads — no #40-side interface, a read of #30's table state | #43 (indirectly, via #30) → #40 |

Reference DAG: `#30 → {#28, #40}`, `#31 → #40`, `#34 → #40` (future), `#45 → #40` (future), `#40 → {#27,
#16}`. **Acyclic.**

## 1.4 Key decisions

- **KD-1 (accounting cadence — season-boundary minimal, per-day deep).** The minimal tier settles **once per
  season at the boundary roll** (`SettleFinances`, the #28 `RunSeasonBoundary` precedent) — a single budget
  figure read as a constant during the season. There is **no per-day step** and no per-day accrual state at
  minimal, keeping serialized state tiny. The deep-tier per-day revenue accrual adds a **daily** slot (a
  future #30 tick-order back-prop analogous to #41's, deferred here — the minimal tier does not need it),
  and its accumulators default to zero (KD-8). This split keeps the minimal surface a pure boundary
  transform.

- **KD-2 (minimal is pure, no draw — reserve, don't promote).** The minimal budget is a pure deterministic
  function; #40 registers **no** RNG stream and keeps `_RESERVED_0x29_` / `SubsystemOrdinals` 91 **reserved**
  (§1.5). Only the deep-tier stochastic revenue variance is a genuine draw site, promoting the tag then. The
  #29 `0x21`-stays-reserved precedent exactly.

- **KD-3 (#31 boundary — read-only constraint + one-way command, no two-way coupling).** #40 exposes (a) a
  **read-only** budget/constraint query #31 reads, and (b) a **#40-owned** `ApplyTransaction` command #31
  invokes to move money on a committed deal. #40 **never references #31**; the ledger mutation lives on #40,
  the decision on #31 — one-way `#31 → #40`. `ApplyTransaction` MUST NOT mutate `TransferBudget`/
  `WageBudget` — those ceilings are set only by `SettleFinances`; Stage 2 has no "remaining budget net of
  spend" running total (a design decision resolved at section-file authoring — see §1.6). This is the
  roadmap §5 "define the projection direction, avoid two-way coupling" invariant satisfied by making #40 the
  single ledger owner and #31 a caller.

- **KD-4 (FFP — deep-tier soft penalty, board routing seam).** FFP is a **soft penalty on the next season's
  projected budget** (not a hard gate — a hard gate would deadlock a club that cannot sell), composing
  multiplicatively with a future #45 `BoardModifier`. Both default to identity (×1.0) at Stage 2; **no #45
  interface is built** (FR-LW-031). `BoardModifier.Identity` is an **explicit factory** (per-mille
  `BudgetMultiplierMillPermille = 1000`), and `default(BoardModifier)` (all-zero, ×0) is **not** a valid
  runtime value — reaching `SettleFinances` it fails loud (F4). This mirrors #41's `MedicalModifier`
  Identity-vs-`default()` zero-value-trap lesson, folded in here proactively rather than left as a future AR
  finding (§1.6). One code path.

- **KD-5 (wage-ledger ownership — #40 owns, sources write line items).** #40 owns the **canonical** wage
  ledger; #31 (player contracts) and #34 (staff contracts) contribute line items via `ApplyTransaction`
  (they call #40; #40 aggregates into `WageBillAggregate`). **At Stage 2 the ledger is empty** — #27 has no
  wage field and #31/#34 do not exist yet — so the aggregate is 0 and behaviour-neutral; the structure is
  #40-owned and ready for its producers (phantom-free).

- **KD-6 (#30 season-boundary integration — a back-prop, not a #30 rewrite).** #40's `SettleFinances` runs
  at #30's `RollToNextSeason()` (§3.5 / FR-SN-029), which today enumerates `finalize (a) → board (b) →
  [#43 insertion (a')] → regenerate (c) → advance-ages [#28 null seam] (d) → reset (e)`. #40's promotion
  files a **#30 back-prop (ERR-030-003)** inserting a **finance-settlement step (b')** — a new lettered step
  in the enumerated transform, the FR-SN-031 well-defined-insertion-point precedent — leaving the
  surrounding steps unchanged. **Ordering rationale (pinned, and forward-compatible with #43):** the finance
  step runs after `finalize` (a) (it needs the final table) and after `board` (b); **critically it is
  positioned AFTER the FR-SN-031 (a') #43 promotion/relegation insertion point**, because the budget depends
  on the club's **post-promotion division** — so when #43 lands its transform stays at (a') and #40's
  finance step at (b') reads the division #43 produced (no collision, correct dependency:
  `… board (b) → #43 promo/rel (a') → #40 finance (b') → regenerate (c) …`). It runs before
  `regenerate`/`reset` so the new season opens with settled budgets. The transform stays a pure function of
  `SeasonState + nextSeed`, so the FR-SN-029 restartable/round-trip contract holds through the added step.

- **KD-7 (persistence — season-save sub-blob; supersedes an earlier `WORLD_STORE_FORMAT_VERSION` guess).**
  `FINANCE_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump. Rationale: #40 is **management-layer** state composed at the season-save
  root, exactly like #28's career-state block — a `WorldStore`-level home would conflate cross-season
  persistence with the living-world `WorldStore`, but FR-LW-003 walls the living-world assembly off from
  management concepts, and #28/#29/#41 established the season-save-sub-blob home for precisely this class of
  per-club/per-player career state. Fail-loud gates; serialize-don't-regenerate. **Roster-membership
  lifecycle:** the per-club block is keyed by `ClubId` (stable — clubs do not churn like players), so it is
  simpler than #28/#41's per-`PlayerId` churn; a club's finances persist across seasons unconditionally
  (F6) — a `ClubFinances` entry, once created, is never removed by a season roll.

- **KD-8 (behaviour-neutral identity).** Deep dials off + `BoardModifier.Identity` + zero accumulators + an
  empty wage ledger ⇒ `SettleFinances` yields exactly `budget = f(final position)` and #40 registers no
  stream (existing streams' cursors byte-identical — trivially so at Stage 2, since no stream is registered
  at all). A club with no deep model configured is the minimal league-finish budget, digest-locked. The deep
  tier extends this identity, never rewrites it.

## 1.5 Determinism posture

World-independent, season-boundary only. **The minimal tier is fully deterministic and registers NO RNG
stream:** `budget = f(finalTablePosition, prizeMoney)` is a pure integer projection; there is no stochastic
term. So **`_RESERVED_0x29_` / `SubsystemOrdinals` 91 stay RESERVED — NOT promoted** at authoring (a
`_RESERVED_0x29_` placeholder row, the #29 `0x21` precedent, filed as ERR-040-001 against
`deterministic-sim/section-3.md` at approval). Promoting a named tag with a zero-draw stream would be the
phantom-surface class FR-LW-031 forbids (the `world.arcs` precedent).

The reservation stands for the **deep-tier stochastic sponsorship/revenue variance**, which is the genuine
first draw site; it promotes `DOMAIN_TAG_CLUB_FINANCES = 0x29` **then** (at #40 T3), keyed
position-independently on `(clubId, seasonNumber, purpose)` — the #28-regen / #30-quick-sim / #41-occurrence
off-pitch keyed-draw precedent (no free-running cursor, so no cursor to serialize). All currency arithmetic
is integer; no `DETERMINISM_DIGEST_VERSION` bump (a reserved-row placeholder + a future keyed off-pitch
stream).

## 1.6 Ambiguities resolved at section-file authoring

Two points the design supplement left implicit are pinned here, both consistent with the supplement's own
data-structure sketch and no-contradiction with any KD:

1. **`ApplyTransaction` never mutates `TransferBudget`/`WageBudget` (KD-3).** The supplement's
   `AvailableTransferBudget(f) => f.TransferBudget` is a trivial passthrough with no decrement logic, and
   `ApplyTransaction`'s signature takes only a `FinanceTransaction` (no budget delta). Stage 2 therefore
   models `TransferBudget`/`WageBudget` as **season ceilings**, not a running "remaining budget net of this
   season's spend" — that aggregation, if wanted, is #31's own bookkeeping (or a deep-tier #40 extension),
   not built here. Pinned as FR-FN-004.
2. **`BoardModifier` gains an explicit per-mille field rather than staying an empty marker struct.** The
   supplement's sketch (`public readonly struct BoardModifier { public static BoardModifier Identity =>
   default; }`) would make `default(BoardModifier)` — an all-zero value — indistinguishable from
   `Identity`, which is exactly the zero-value trap #41's `MedicalModifier` AR-1 finding corrected
   (`default()` there meant ×0 risk / divide-by-zero recovery). Rather than ship the same trap and rediscover
   it in a future adversarial-review pass, `BoardModifier` is given an explicit
   `BudgetMultiplierMillPermille` field with `Identity => new(1000)`, and `default(BoardModifier)` (0) is
   pinned as an F4 fail-loud case (FR-FN-018). The routing seam remains identity-only and phantom-free — no
   #45 interface is built — this only changes how "identity" is represented internally.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial. Status IN REVIEW. |
#endregion
