# Club Finances & Economy #40 — Appendices

**Created:** July 23, 2026
**Last Updated:** September 11, 2026 (v0.6 — ERR-030-051: season-boundary worked example corrected to synchronous atomic RollToNextSeason semantics)
**Last Updated (prior):** September 6, 2026 (v0.5 — PR #363 critique: config-loader range disclosure and T1a terminology)
**Last Updated (prior):** September 4, 2026 (v0.4 — T1 self-identifying save framing back-prop)
**Version:** 0.6
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Every constant carries exactly one source tag. Magnitudes marked `[GT]` are illustrative pending a future
Stage-2/3 balance pass (the #21 G2 precedent); the shapes/directions are the reviewed contract.

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `FINANCE_SAVE_MAGIC` | `0x464E4345` (`FNCE`) | [FIXED] | Self-identifying prefix for #40's finance sub-blob. Checked before the version so a sibling block at version 1 cannot be silently interpreted as finance state (§4.4). |
| `FINANCE_SAVE_FORMAT_VERSION` | 1 | [FIXED] | The #40 T1a sub-blob generation (KD-7), independently gated from `WORLD_STORE_FORMAT_VERSION` / `SEASON_STATE_FORMAT_VERSION` / sibling management sub-blob versions. |
| `FINANCE_SAVE_HEADER_BYTES` | 12 | [FIXED] | `u32` magic + `u32` version + `u32` club-record count (§4.4). |
| `FINANCE_SAVE_RECORD_BYTES` | 52 | [FIXED] | Exact T1a record width: `i32 ClubId` + six `i64` `ClubFinances` fields (§4.4). Used by the overflow-safe count bound. |
| `PERMILLE_DENOM` | 1000 | [FIXED] | Shared per-mille denominator for `BoardModifier` and the prize-money-share weights (§3.1) — keeps every ratio integer, no float. |
| `BOARD_MODIFIER_IDENTITY_PERMILLE` | 1000 | [FIXED] | Per-mille identity for `BoardModifier.BudgetMultiplierMillPermille` (= ×1.0). `BoardModifier.Identity` sets this; `default(BoardModifier)` (all-zero) is NOT valid (FR-FN-018 / F4). |
| `STARTING_CLUB_BALANCE` | 500,000 | [GT] | `ClubFinances.CreateInitial`'s default starting `Balance` at league/game bootstrap. |
| `PRIZE_MONEY_WINNER` | 2,000,000 | [GT] | Prize money for finishing **position 1** — the top endpoint of the linear interpolation (§3.1.1). |
| `PRIZE_MONEY_LAST_PLACE` | 200,000 | [GT] | Prize money for finishing **last place** — the bottom endpoint. MUST be `≤ PRIZE_MONEY_WINNER` (a catalogue invariant — a non-negative `span`, §3.1.1). |
| `BASE_TRANSFER_BUDGET` | 100,000 | [GT] | Flat per-season transfer-budget allocation before the prize-money share is added (§3.1). |
| `TRANSFER_BUDGET_PRIZE_SHARE_PERMILLE` | 400 | [GT] | Per-mille share of `prizeMoney` folded into `TransferBudget` (§3.1). |
| `BASE_WAGE_BUDGET` | 50,000 | [GT] | Flat per-season wage-budget allocation before the prize-money share is added (§3.1). |
| `WAGE_BUDGET_PRIZE_SHARE_PERMILLE` | 150 | [GT] | Per-mille share of `prizeMoney` folded into `WageBudget` (§3.1). |
| `CLUB_FINANCES_BUDGET_CEILING_MAX` | 50,000,000 | [GT] | A generous sanity ceiling on `TransferBudget`/`WageBudget` after the `BoardModifier` multiply. |

**Current config-loader range.** The currency-bearing `[GT]` declarations are `long` in #40 so all finance
state/arithmetic remains 64-bit integer, but the shared `GameplayConfig` API currently exposes `GetInt` for
these scalar keys. Tuned values loaded from config are therefore limited to the signed `Int32` range
(−2,147,483,648..2,147,483,647) even though the in-memory fields are `long`. Today's catalogue values are far
inside that range. A future need above it must widen the shared Code Standards-approved config loader rather
than introduce a finance-local parser or narrow #40's accounting types.

**`DOMAIN_TAG_CLUB_FINANCES` / `SubsystemOrdinals.ClubFinances`** — `0x29` / `91` respectively, per
`docs/tracking/club-finances-economy-design.md` §5 and the roadmap's off-pitch reservation. **RESERVED, NOT
promoted** at this spec's approval (ERR-040-001 adds only a `_RESERVED_0x29_` placeholder row — the #29
`0x21`-stays-reserved precedent, KD-2) because the minimal tier registers no stream. These are **not**
`[GT]`/`[FIXED]` project constants declared in this catalogue — they are #16's tag-namespace reservation,
to be cross-cited `[CROSS: #16 §3.4]` once genuinely promoted at #40 T3's first stochastic draw.

## Appendix B — Worked example: save/restore mid-season AND across the atomic season boundary

**Mid-season boundary.** Seed (from §3.5): club 12, season 8, after the transfer + wage-sign transactions:
`ClubFinances { Balance: 2,315,790, TransferBudget: 786,316, WageBudget: 307,368, WageBillAggregate:
107,000, SeasonRevenueAccrued: 0, FfpBalanceWindow: 0 }`. Save now; restore. All six fields restore
field-identical. Continuing to apply the wage-release `Credit` transaction
(`FinanceTransaction{ Credit, PlayerWage, Amount = 20,000 }`) post-restore reaches
`{ Balance: 2,315,790 (unchanged — a wage transaction moves no cash), WageBillAggregate: 87,000, … }` —
identical to an uninterrupted run that never saved (T-FN-DET-001), because `ApplyTransaction` is a pure
function of its inputs with no cursor to diverge.

**Atomic `RollToNextSeason()` boundary.** `RollToNextSeason()` is synchronous; callers cannot save
between its internal steps. At step (b') #30 computes every club's settled finance value from the final
table and holds those values staged. If the later season commit refuses, the live finance array remains
unchanged and a save still contains the pre-roll values. After a successful return the staged values are
installed exactly once; a save taken then restores those six fields identically. Restoring that completed
save does not re-run settlement merely because a restore occurred. This is T-FN-DET-002 after the
ERR-030-051 atomicity correction.

## Appendix C — Worked example: behaviour-neutral identity (KD-8)

With the deep dials off, `BoardModifier.Identity` (`BudgetMultiplierMillPermille = 1000`), and all
deep-tier accumulators at `0`, `SettleFinances` for club 12 at position 4 of 20 (§3.5's worked example)
yields **exactly** `TransferBudget = 786,316` / `WageBudget = 307,368`. `ClubFinances.CreateInitial`
produces `{ Balance: startingBalance, TransferBudget: 0, WageBudget: 0, WageBillAggregate: 0,
SeasonRevenueAccrued: 0, FfpBalanceWindow: 0 }`. Because the minimal tier registers **no** RNG stream at all,
reserving `_RESERVED_0x29_`/91 leaves every existing stream cursor byte-identical trivially (T-FN-NEU-003).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples (mid-season + mid-boundary-roll save/restore; behaviour-neutral identity). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): Appendix B mid-season example updated — wage transactions leave `Balance` unchanged. |
| 0.3 | 2026-08-08 | — | **ERR-041-012 back-prop:** Appendix C comparator corrected after #41's RNG-shape correction. |
| 0.4 | 2026-09-04 | Codex | **T1 implementation back-prop.** Adds self-identifying finance magic and fixed framing widths. |
| 0.5 | 2026-09-06 | — | **PR #363 critique correction.** Uses T1a terminology for the standalone codec and discloses that shared `GameplayConfig.GetInt` currently caps config-sourced currency tuning at signed Int32 range while #40 accounting remains `long`. |
| 0.6 | 2026-09-11 | — | **ERR-030-051 / T2b correction.** Replaces the impossible mid-`RollToNextSeason()` save example with the supported synchronous contract: stage finance settlement at (b'), leave live values untouched on refusal, install once after a successful season commit, and round-trip before/after-call saves field-identically. |
#endregion
