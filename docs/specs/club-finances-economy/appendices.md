# Club Finances & Economy #40 — Appendices

**Created:** July 23, 2026
**Last Updated:** September 4, 2026 (v0.4 — T1 self-identifying save framing back-prop)
**Last Updated (prior):** August 8, 2026 (v0.3 — balance-pass AR pass 8 M2, the ERR-041-012 back-prop)
**Version:** 0.4
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Every constant carries exactly one source tag. Magnitudes marked `[GT]` are illustrative pending a future
Stage-2/3 balance pass (the #21 G2 precedent); the shapes/directions are the reviewed contract.

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `FINANCE_SAVE_MAGIC` | `0x464E4345` (`FNCE`) | [FIXED] | Self-identifying prefix for #40's finance sub-blob. Checked before the version so a sibling block at version 1 cannot be silently interpreted as finance state (§4.4). |
| `FINANCE_SAVE_FORMAT_VERSION` | 1 | [FIXED] | The #40 sub-blob generation (KD-7), independently gated from `WORLD_STORE_FORMAT_VERSION` / `SEASON_STATE_FORMAT_VERSION` / sibling management sub-blob versions. |
| `FINANCE_SAVE_HEADER_BYTES` | 12 | [FIXED] | `u32` magic + `u32` version + `u32` club-record count (§4.4). |
| `FINANCE_SAVE_RECORD_BYTES` | 52 | [FIXED] | Minimum/exact T1 record width: `i32 ClubId` + six `i64` `ClubFinances` fields (§4.4). Used by the overflow-safe count bound. |
| `PERMILLE_DENOM` | 1000 | [FIXED] | Shared per-mille denominator for `BoardModifier` and the prize-money-share weights (§3.1) — keeps every ratio integer, no float. |
| `BOARD_MODIFIER_IDENTITY_PERMILLE` | 1000 | [FIXED] | Per-mille identity for `BoardModifier.BudgetMultiplierMillPermille` (= ×1.0). `BoardModifier.Identity` sets this; `default(BoardModifier)` (all-zero) is NOT valid (FR-FN-018 / F4). |
| `STARTING_CLUB_BALANCE` | 500,000 | [GT] | `ClubFinances.CreateInitial`'s default starting `Balance` at league/game bootstrap. |
| `PRIZE_MONEY_WINNER` | 2,000,000 | [GT] | Prize money for finishing **position 1** — the top endpoint of the linear interpolation (§3.1.1). |
| `PRIZE_MONEY_LAST_PLACE` | 200,000 | [GT] | Prize money for finishing **last place** — the bottom endpoint. MUST be `≤ PRIZE_MONEY_WINNER` (a catalogue invariant — a non-negative `span`, §3.1.1). |
| `BASE_TRANSFER_BUDGET` | 100,000 | [GT] | Flat per-season transfer-budget allocation before the prize-money share is added (§3.1). |
| `TRANSFER_BUDGET_PRIZE_SHARE_PERMILLE` | 400 | [GT] | Per-mille share of `prizeMoney` folded into `TransferBudget` (§3.1). |
| `BASE_WAGE_BUDGET` | 50,000 | [GT] | Flat per-season wage-budget allocation before the prize-money share is added (§3.1). |
| `WAGE_BUDGET_PRIZE_SHARE_PERMILLE` | 150 | [GT] | Per-mille share of `prizeMoney` folded into `WageBudget` (§3.1). |
| `CLUB_FINANCES_BUDGET_CEILING_MAX` | 50,000,000 | [GT] | A generous sanity ceiling on `TransferBudget`/`WageBudget` after the `BoardModifier` multiply — bounds even an extreme deep-tier board/FFP scenario; a Stage-2 top-flight-winner budget never approaches it. |

**`DOMAIN_TAG_CLUB_FINANCES` / `SubsystemOrdinals.ClubFinances`** — `0x29` / `91` respectively, per
`docs/tracking/club-finances-economy-design.md` §5 and the roadmap's off-pitch reservation. **RESERVED, NOT
promoted** at this spec's approval (ERR-040-001 adds only a `_RESERVED_0x29_` placeholder row — the #29
`0x21`-stays-reserved precedent, KD-2) because the minimal tier registers no stream. These are **not**
`[GT]`/`[FIXED]` project constants declared in this catalogue — they are `#16`'s tag-namespace reservation,
to be cross-cited `[CROSS: #16 §3.4]` once genuinely promoted at #40 T3's first stochastic draw.

## Appendix B — Worked example: save/restore across a mid-season AND a mid-boundary-roll boundary

**Mid-season boundary.** Seed (from §3.5): club 12, season 8, after the transfer + wage-sign transactions:
`ClubFinances { Balance: 2,315,790, TransferBudget: 786,316, WageBudget: 307,368, WageBillAggregate:
107,000, SeasonRevenueAccrued: 0, FfpBalanceWindow: 0 }`. Save now; restore. All six fields restore
field-identical. Continuing to apply the wage-release `Credit` transaction
(`FinanceTransaction{ Credit, PlayerWage, Amount = 20,000 }`) post-restore reaches
`{ Balance: 2,315,790 (unchanged — a wage transaction moves no cash), WageBillAggregate: 87,000, … }` —
identical to an uninterrupted run that never saved (T-FN-DET-001), because `ApplyTransaction` is a pure
function of its inputs with no cursor to diverge.

**Mid-`RollToNextSeason()` boundary.** A save is taken between step (b') `SettleFinances` (which has already
produced season 8's `ClubFinances` for every club) and step (c) regenerate. Restoring resumes
`RollToNextSeason()` at (c) with every club's season-8 `ClubFinances` already committed and field-identical
— `SettleFinances` is **not** re-run for any club on resume, because (b') runs exactly once per
`RollToNextSeason()` call and the resumed roll continues from (c), never re-entering (b'). This is the
FR-SN-029 restartable-transform contract (T-FN-DET-002/FR-FN-024), now extended through #40's inserted step
exactly as FR-SN-031 anticipates for a later-inserted transform.

## Appendix C — Worked example: behaviour-neutral identity (KD-8)

With the deep dials off, `BoardModifier.Identity` (`BudgetMultiplierMillPermille = 1000`), and all
deep-tier accumulators at `0`, `SettleFinances` for club 12 at position 4 of 20 (§3.5's worked example)
yields **exactly** `TransferBudget = 786,316` / `WageBudget = 307,368` — the same figures a
`board.BudgetMultiplierMillPermille = 1000` multiply produces, since `× 1000 / 1000` is the identity on
integer division with no remainder loss (both `baseCeiling` values here are already integers, so the
identity multiply changes nothing, T-FN-NEU-001). `ClubFinances.CreateInitial(startingBalance)` yields
`{ Balance: startingBalance, TransferBudget: 0, WageBudget: 0, WageBillAggregate: 0, SeasonRevenueAccrued:
0, FfpBalanceWindow: 0 }` — the pre-first-season identity (T-FN-NEU-002). Because the minimal tier registers
**no** RNG stream at all (KD-2), reserving `_RESERVED_0x29_`/91 leaves every existing stream's cursor
byte-identical **trivially** — the same property #41's occurrence draw has for the same reason (since
ERR-041-012, #41 also registers NO stream; this line originally claimed the opposite as its comparator)
— there is nothing yet to be independent *of* (T-FN-NEU-003).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples (mid-season + mid-boundary-roll save/restore; behaviour-neutral identity). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): Appendix B mid-season example updated — wage transactions leave `Balance` unchanged. |
| 0.3 | 2026-08-08 | — | **ERR-041-012 back-prop (balance-pass AR pass 8, M2)**: the Appendix C comparator asserted #41 registers an `injuries.occurrence` stream — a factual claim about a sibling spec that ERR-041-012 established as never-true; restated. |
| 0.4 | 2026-09-04 | Codex | **T1 implementation back-prop.** Adds the self-identifying finance magic and fixed framing widths after review found that a version-only header could silently cross-decode a sibling version-1 block. |
#endregion
