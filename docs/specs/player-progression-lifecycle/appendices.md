# Player Progression & Lifecycle #28 — Appendices

**Created:** July 23, 2026
**Last Updated:** August 9, 2026 (v0.3 — Appendix A brought current with `PlayerProgressionConstants.cs`: 7 constants added that landed at #28 T0/T1 with no catalogue row — `PROGRESSION_SAVE_MAGIC`, `PROGRESSION_NOT_ADVANCED_SENTINEL` (both `[FIXED]`) and `PA_MIN`, `REGEN_PA_HEADROOM`, `REGEN_AGE_MIN`, `REGEN_AGE_MAX`, `NEW_GAME_PA_HEADROOM` (all `[GT]`); values copied verbatim from code, none changed)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.3
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
| `PROGRESSION_SAVE_MAGIC` | `[FIXED]` | `0x50524F47` (`"PROG"`) | The #28 sub-blob's self-identifying leading tag, written BEFORE the version (ERR-028-004) — deliberately NOT the `DOMAIN_TAG_PLAYER_PROGRESSION` RNG tag §3.5 once named in its place: every sub-blob format in the save stack sits at version 1, so without a magic each codec would decode a sibling's bytes cleanly and silently (ERR-029-005/ERR-041-009). |
| `PROGRESSION_NOT_ADVANCED_SENTINEL` | `[FIXED]` | `uint.MaxValue` | `PlayerLifecycle.LastAdvancedWorldDay`'s never-advanced sentinel value — `uint.MaxValue` rather than 0 because day 0 is a legitimate world day (the day-0 trap; the #29 `TRAINING_NOT_ADVANCED_SENTINEL` precedent). **Not a legal stored cursor value as of ERR-028-014** — `SeedFrom` anchors the cursor at the seed day and `FromBlocks` refuses a lifecycle carrying it (§5.9 T-PG-BLOCK-007); the constant survives as the refused-`worldDay`-argument value `AdvanceDay` checks against (F8, §5.7 T-PG-SAVE-004). |
| `DOMAIN_TAG_PLAYER_PROGRESSION` | `[CROSS]` | `0x20` | Mirror of the #16 §3.4 tag this spec promotes (regen draw site). |
| `SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` | `[CROSS]` | 82 | Mirror of `SubsystemOrdinals.PlayerProgression`. |
| `PROGRESSION_REGEN_FIELDS` | `[DERIVED]` | (= regen draw budget) | Fixed per-regen reservation size (the #27 `FIELDS_PER_PLAYER` discipline, §3.3). |
| `ABILITY_MAX` | `[GT]` | 10000 | The wide-integer CA/PA scale ceiling. |
| `POINT_COST` | `[GT]` | (= `DAYS_PER_YEAR`) | Cursor points per whole attribute-point; with the §4.3 band step this makes exactly one `[1,20]` step per year (KD-8). |
| `GROWTH_AGE` | `[GT]` | 24 | Age below which a player is in the Growth band (§4.3 <24 → +1/yr). |
| `DECLINE_AGE` | `[GT]` | 30 | Age above which a player is in the Decline band (§4.3 >30 → −1/yr). |
| `RETIREMENT_AGE` | `[GT]` | 36 | Hard retirement age (§4.3; deterministic, no draw). |
| `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` | `[GT]` | +1 / −1 | Per-day cursor accrual in the Growth / Decline band (Stable = 0); `POINT_COST = DAYS_PER_YEAR` ⇒ one step/year. |
| `PA_MIN` | `[GT]` | 4000 | Regen `PotentialAbility` floor — a regen is drawn in `[max(PA_MIN, CA + REGEN_PA_HEADROOM), ABILITY_MAX]` (§3.3). |
| `REGEN_PA_HEADROOM` | `[GT]` | 1000 | Minimum ability-point gap between a regen's generated `CurrentAbility` and its drawn `PotentialAbility` — the "room to grow" a young regen must have (§3.3). |
| `REGEN_AGE_MIN` / `REGEN_AGE_MAX` | `[GT]` | 16 / 20 | Regen minimum / maximum generated age (the young band, §3.3). |
| `NEW_GAME_PA_HEADROOM` | `[GT]` | 1500 | The `PotentialAbility` headroom a new-game (bootstrapped) player is seeded with above his generated CA: `PA = clamp(CA + NEW_GAME_PA_HEADROOM, PA_MIN, ABILITY_MAX)`. A deliberate placeholder for authored data (ERR-028-003) — §3.2 sources new-game PA from #47's authored player database, which has no `src/` assembly yet, so #28 seeds one deterministically (never drawn) until #47 lands. |

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
| 0.3 | 2026-08-09 | — | Appendix A gains 7 rows for constants `PlayerProgressionConstants.cs` has carried since #28 T0/T1 with no catalogue entry: `PROGRESSION_SAVE_MAGIC` and `PROGRESSION_NOT_ADVANCED_SENTINEL` (`[FIXED]`), `PA_MIN`, `REGEN_PA_HEADROOM`, `REGEN_AGE_MIN`, `REGEN_AGE_MAX` and `NEW_GAME_PA_HEADROOM` (`[GT]`). Values, tags and doc text copied verbatim from code (authoritative here); no value changed. `PROGRESSION_NOT_ADVANCED_SENTINEL`'s row also records ERR-028-014 — the sentinel is no longer a legal *stored* cursor value, though the constant remains live as the refused `AdvanceDay` argument (F8). Doc-only, no code change. |
#endregion
