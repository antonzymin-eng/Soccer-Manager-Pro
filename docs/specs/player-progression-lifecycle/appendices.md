# Player Progression & Lifecycle #28 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue (`PlayerProgressionConstants`)

Region order Fixed → Derived → GT (Code Standards #20). Magnitudes are illustrative pending the
balance pass (§1.3); the shapes/tags are the contract.

| Constant | Tag | Value (illustrative) | Meaning |
|---|---|---|---|
| `DAYS_PER_YEAR` | `[FIXED]` | 365 | World-days per age-year (the age-derivation divisor, §3.1.1). |
| `ATTRIBUTE_MIN` / `ATTRIBUTE_MAX` | `[CROSS]` | 1 / 20 | Mirror of `PlayerDatabaseConstants.ATTRIBUTE_MIN/MAX` (the `[1,20]` bounds a spend respects). |
| `PROGRESSION_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | The lifecycle sub-blob version (independent of every other format version; §3.5). |
| `DOMAIN_TAG_PLAYER_PROGRESSION` | `[CROSS]` | `0x20` | Mirror of the #16 §3.4 tag this spec promotes (regen draw site). |
| `SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` | `[CROSS]` | 82 | Mirror of `SubsystemOrdinals.PlayerProgression`. |
| `PROGRESSION_REGEN_FIELDS` | `[DERIVED]` | (= regen draw budget) | Fixed per-regen reservation size (the #27 `FIELDS_PER_PLAYER` discipline, §3.3). |
| `ABILITY_MAX` | `[GT]` | 10000 | The wide-integer CA/PA scale ceiling. |
| `POINT_COST` | `[GT]` | (= `DAYS_PER_YEAR`) | Cursor points per whole attribute-point; with the §4.3 band step this makes exactly one `[1,20]` step per year (KD-8). |
| `GROWTH_AGE` | `[GT]` | 24 | Age below which a player is in the Growth band (§4.3 <24 → +1/yr). |
| `DECLINE_AGE` | `[GT]` | 30 | Age above which a player is in the Decline band (§4.3 >30 → −1/yr). |
| `RETIREMENT_AGE` | `[GT]` | 36 | Hard retirement age (§4.3; deterministic, no draw). |
| `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` | `[GT]` | +1 / −1 | Per-day cursor accrual in the Growth / Decline band (Stable = 0); `POINT_COST = DAYS_PER_YEAR` ⇒ one step/year. |

No `[EST]` constants. Array/table-valued growth weights (the deep-tier per-attribute curve) are a
Stage-3 `[GT]` carve-out (the `TacticalInstructionsConstants` array-table precedent — compile-time
tables with their own invariant tests, not config-overridable at Stage 2).

## Appendix B — Worked example: byte-exact growth across a save (KD-1 / T-PG-DET-001)

A Growth-band player (age derived < `GROWTH_AGE` from `BirthWorldDay`), `GROWTH_DAILY_POINTS = +1`,
`POINT_COST = DAYS_PER_YEAR = 365`, `GrowthCursor = 0`, `Passing = 12`. Attribute change is the cursor
alone — there is no discrete year-rollover step (age is a pure function of the world day, §3.1.1):

| World-day | Derived age band | Accrue | Cursor | Spend? | Passing | Save→restore here == continuous? |
|---|---|---|---|---|---|---|
| 0 | Growth | +1 | 1 | no | 12 | yes (cursor 1) |
| 200 | Growth | +1 | 201 | no | 12 | yes (cursor 201) |
| 364 | Growth | +1 | 365 | **spend 1** → cursor 0 | **13** | yes (cursor 0 post-spend) |
| 365 | Growth | +1 | 1 | no | 13 | yes (cursor 1) |

A save on day 200 (cursor 201) restores and reaches day 364 → cursor 365 → spends exactly once → 13,
identical to the uninterrupted run. There is no anchor to re-cross and no discrete rollover, so a save
on day 364, 365, or 366 all restore to the same continuation — nothing is double-counted (the age band
is recomputed from `BirthWorldDay` each day, never banked). Integer-only ⇒ `CanonicalSerializer`
round-trips it bitwise.

## Appendix C — Worked example: retirement + regen at the season boundary (KD-5/KD-6)

Player `PlayerId = 175` (club 7, localIndex 0) reaches age 36 on world-day 4020 mid-season:
- **Day 4020 (`AdvanceDay`):** `RetirementFlag = true`, `RetirementDay = 4020`. The player stays in
  the roster, stays selectable — the season's remaining fixtures are undisturbed (FR-PG-014).
- **Season boundary (`RunSeasonBoundary`):** `retirees = [175]` → `RetirementResult([175])`.
  `GenerateRegen(rng, streamIndex, clubId=7, ref nextPlayerId, ...)` draws a new record with a
  **fresh** `PlayerId = nextPlayerId` (say 631, beyond the initial `7 * 25 + k` range), a drawn PA,
  and `[1,20]` attributes below it. The lifecycle entry for 175 is removed; the entry for 631 is
  inserted → block entry count unchanged (FR-PG-019). `#30`/`#27` apply the `Squad` swap.
- **Idempotency (F6):** a save taken mid-boundary carries the boundary marker + `nextPlayerId = 632`
  + the mutated block; restore→re-run sees `boundaryAlreadyApplied` and no-ops — no double regen, no
  duplicate id.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices: constant catalogue, byte-exact growth-across-a-save worked example, retirement+regen boundary worked example. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
