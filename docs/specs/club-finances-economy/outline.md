# Club Finances & Economy #40 — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring from the converged design supplement)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/club-finances-economy-design.md` v0.2
**FR prefix:** FR-FN · **Wave:** 2 · **Master-plan home:** §5 Stage 3 (financials)

---

## Purpose

Per-club finances on the **season boundary** (not the world tick, not the match loops): transfer + wage
**budgets**, a **wage ledger**, **revenue** (prize money / matchday / sponsorship), and (deep tier) FFP-style
balance constraints. Split from #31 Transfer Market so the economy is a system in its own right rather than
a transfer side-effect — the **counterparty-constraint layer** #31's negotiation reads. The minimal tier is
**fully deterministic — no draw**: `budget = f(finalTablePosition, prizeMoney)`, a pure integer projection
computed once per season at #30's boundary roll.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions KD-1..KD-8 |
| 2 | Functional requirements FR-FN-001..028, data structures, failure modes F1..F7 |
| 3 | Algorithms — `SettleFinances` (season-boundary budget projection), `ApplyTransaction` (ledger mutation), `AvailableTransferBudget` (read-only query), worked example |
| 4 | Architecture, assembly, file layout, reference direction |
| 5 | Test plan (T-FN-*) + FR traceability |
| 6 | Performance / season-boundary cadence |
| 7 | Future extensions, T-phase plan T0–T3, the #31/#34/#45 deferred seams |
| 8 | References |
| 9 | Approval checklist |
| Appendices | Constant catalogue + worked examples |

## Key decisions (summary; full text in §1)

- **KD-1** Accounting cadence — season-boundary minimal, per-day deep: the minimal tier settles **once per
  season** at the boundary roll (`SettleFinances`); there is no per-day step and no per-day accrual state at
  Stage 2. The deep-tier per-day revenue accrual is a future daily slot, deferred.
- **KD-2** Minimal is pure, no draw — reserve, don't promote: `budget = f(finalTablePosition, prizeMoney)` is
  a pure integer projection; #40 registers **no** RNG stream and `_RESERVED_0x29_` / `SubsystemOrdinals` 91
  stay **RESERVED** until the deep-tier stochastic sponsorship-variance draw exists.
- **KD-3** #31 boundary — read-only constraint + one-way command, no two-way coupling: #40 exposes a
  read-only budget/constraint query and a #40-owned `ApplyTransaction` command; #40 never references #31.
- **KD-4** FFP — deep-tier soft penalty, board routing seam: FFP is a soft penalty on the *next* season's
  projected budget (never a hard gate), composing multiplicatively with a future #45 `BoardModifier`. Both
  default to identity at Stage 2; no #45 interface is built.
- **KD-5** Wage-ledger ownership — #40 owns, sources write line items: #40 owns the canonical wage ledger;
  #31/#34 contribute line items via `ApplyTransaction`. At Stage 2 the ledger is empty (no producer yet).
- **KD-6** #30 season-boundary integration — a back-prop, not a #30 rewrite: `SettleFinances` runs at #30's
  `RollToNextSeason()`, inserted as a new step (b') **after** the (a') #43 promotion/relegation insertion
  point and **before** regenerate (c) — the budget depends on the club's post-promotion division.
- **KD-7** Persistence — season-save sub-blob; supersedes an earlier `WORLD_STORE_FORMAT_VERSION` guess:
  `FINANCE_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, keyed by
  `ClubId` (clubs do not churn like players — simpler than #28/#41's per-`PlayerId` churn).
- **KD-8** Behaviour-neutral identity: deep dials off + `BoardModifier.Identity` + zero accumulators + an
  empty wage ledger ⇒ `SettleFinances` yields exactly the minimal league-finish budget, and registering
  #40's reserved namespace slot leaves every existing stream's cursor byte-identical.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from the converged design supplement. Status IN REVIEW. |
#endregion
