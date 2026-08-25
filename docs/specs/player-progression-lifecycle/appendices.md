# Player Progression & Lifecycle #28 — Appendices

**Created:** July 23, 2026
**Last Updated:** August 24, 2026 (v0.10 — round-2 finding spec-32-flat-band-step-sweep-stopped-two-
paragraphs-short: Appendix A's `POINT_COST` row corrected — it still described the KD-8 identity
against "the §4.3 band step" after ERR-028-020 made that step the retired predicate, restated against
§3.1.3's accrual curve; the identity itself (one `[1,20]` step per year of a full band) is unchanged,
since the ramp's whole-life integral equals the step's exactly at every half-width. Prior entry below.)
**Last Updated (prior):** August 23, 2026 (v0.9 — football-judgment proxy review, batch-1 adversarial finding config-unbound-premise-false-28: Appendix A's `AGE_BAND_RAMP_HALF_WIDTH_YEARS` row corrected — its "the catalogue lock runs config-unbound" justification for the computing-site guard was false, since `PlayerProgressionConstants.cs` has zero `Config.GetX` calls; a catalogue-level lock now exists alongside the computing-site one. Prior entry below.)
**Last Updated (prior):** August 22, 2026 (v0.8 — **ERR-028-022 + ERR-028-023**, the reviewed High findings against the v0.7 landing. **ERR-028-022:** Appendix A's `RETIREMENT_GAME_READING_SPAN_YEARS` row published a P5 exactness claim — "the offsets over a uniform `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` population sum to exactly 0… the league's retirement RATE is unchanged" — that was FALSE of the floored-mean implementation it described, by −204,621 days over the uniform `[1,20]³` product (−25.58 d/player); annotated in place with the corrected sum-carrying form and with the residual honest fact that #27's generator draws `[6,14]` (centre 10 vs the offset's neutral 10.5), leaving ≈ −38 days per generated player. **ERR-028-023:** Appendix B's "The fix" paragraph still asserted the retired three-way seed credit (`GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` / `0`) in the PRESENT TENSE, immediately above the ERR-028-020 currency note added beneath the same table; corrected to `DailyBandPoints(Age₀ · DAYS_PER_YEAR)` and marked as the ERR-028-018-era text it now is. **ERR-028-022, Appendix C:** its worked example claimed the player's offset "is 0" and his retirement day therefore "exactly `RETIREMENT_AGE · DAYS_PER_YEAR`" — unattainable under either form, since the neutral point is the range MIDPOINT 10.5 and no integer attribute sits there; corrected in place with the reachable nearest values (sums 31/32 → ∓12 days; an all-10 outfielder −38, i.e. the same residual Appendix A now records, arriving as one player), the flow and the illustrative crossing day left standing. Prior entry below.)
**Last Updated (prior):** August 22, 2026 (v0.7 — **ERR-028-020 / ERR-028-021**: Appendix A gains `AGE_BAND_RAMP_HALF_WIDTH_YEARS`, `RETIREMENT_GOALKEEPER_BONUS_YEARS` and `RETIREMENT_GAME_READING_SPAN_YEARS`, all `[GT]`, each with its zero-identity and its fail-loud invariant; `RETIREMENT_AGE` re-described as the BASELINE rather than the whole rule, and `GROWTH_DAILY_POINTS`/`DECLINE_DAILY_POINTS` as the rate's magnitude rather than a three-way lookup. Appendix B's traversal note and Appendix C's worked example brought onto the ramped curve and the per-player retirement day. Prior entry below.)
**Last Updated (prior):** August 11, 2026 (v0.6 — ERR-028-019: docs close-out for AR passes 5-8 — Appendix A gains `MAX_DERIVABLE_AGE_YEARS`, tagged `[FIXED]` as a REPRESENTABILITY bound, not a football-plausibility one; its own value history — first set to a football-plausibility 1000 in the same commit, corrected same-session to 100,000,000 after it broke the `i64` field-width lock ERR-028-006 bought — recorded verbatim rather than summarized, per this pass's no-fabrication constraint)
**Last Updated (prior):** August 10, 2026 (v0.5 — ERR-028-018: correcting v0.4's own scope note, which was falsified by execution — Appendix B's worked example now describes what the public `SeedFrom`+`AdvanceDay` entry point actually produces, since `ProgressionEngine.SeedLifecycle` now credits the seed day's own band step)
**Last Updated (prior):** August 10, 2026 (v0.4 — ERR-028-017: Appendix A's `DOMAIN_TAG_PLAYER_PROGRESSION`/`SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` rows marked "not yet in the catalogue" — the v0.3 "copied verbatim from code" claim was false for these two, which exist only in doc-comment prose, not as declared constants; Appendix B's worked example scoped to the raw per-player projection, since the public `SeedFrom`+`AdvanceDay` entry point spends its first point one day later (world-day 365, not 364) since ERR-028-014 anchored the cursor at the seed day)
**Last Updated (prior):** August 9, 2026 (v0.3 — Appendix A brought current with `PlayerProgressionConstants.cs`: 7 constants added that landed at #28 T0/T1 with no catalogue row — `PROGRESSION_SAVE_MAGIC`, `PROGRESSION_NOT_ADVANCED_SENTINEL` (both `[FIXED]`) and `PA_MIN`, `REGEN_PA_HEADROOM`, `REGEN_AGE_MIN`, `REGEN_AGE_MAX`, `NEW_GAME_PA_HEADROOM` (all `[GT]`); values copied verbatim from code, none changed)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.10
**Status:** APPROVED

---

## Appendix A — Constant catalogue (`PlayerProgressionConstants`)

Region order Fixed → Derived → GT (Code Standards #20). Magnitudes are illustrative pending the
balance pass (§1.3); the shapes/tags are the contract.

| Constant | Tag | Value (illustrative) | Meaning |
|---|---|---|---|
| `DAYS_PER_YEAR` | `[FIXED]` | 365 | World-days per age-year (the age-derivation divisor, §3.1.1). |
| `MAX_DERIVABLE_AGE_YEARS` | `[FIXED]` | 100,000,000 | **Added ERR-028-019 (AR pass 5).** The widest age, in years, the model will derive from a `PlayerLifecycle`'s `BirthWorldDay` anchor — a REPRESENTABILITY bound, not a football one; it exists so the `long` day difference in §3.1.1's age derivation can never narrow into `int` as garbage. Both accrual loops (§3.1) saturate the derived `int age` at this ceiling rather than overflowing. **Tag justification, verified against the constant's own doc comment in `PlayerProgressionConstants.cs` (the source this pass's no-fabrication rule requires): the value was FIRST SET TO 1000 in the same commit that added this constant — a football-plausibility number, not a representability one — which broke `SaveRestore_ANegativeBirthWorldDayBeyondInt32Range_SurvivesTheCodec`, the lock proving the `i64` `BirthWorldDay` field width ERR-028-006 bought.** That lock needs an anchor that does NOT fit in 32 bits (`birthWorldDay < int.MinValue`, i.e. an age beyond ~5.88M years), so any bound below that floor makes the field width unprovable — a "reasonable age" gate and a 64-bit-width proof cannot both hold, and the width proof was judged to win (a silently truncating codec is the worse failure). The shipped value, 100,000,000, sits far above that floor and far below the overflow ceiling (the widest derivable age is `MAX_DERIVABLE_AGE_YEARS + uint.MaxValue/365 ≈ 1.117e8`, comfortably inside `int.MaxValue`), and still refuses the anchor that produced the original defect (`-(long)int.MaxValue * 365 - 365 ≈ -7.84e11`, which derives ≈ 2.15e9 and overflows). If a football-plausibility bound on age is ever wanted, the constant's own doc comment records that it belongs at the roster generator as a separate `[GT]`, not here. |
| `ATTRIBUTE_MIN` / `ATTRIBUTE_MAX` | `[CROSS]` | 1 / 20 | Mirror of `PlayerDatabaseConstants.ATTRIBUTE_MIN/MAX` (the `[1,20]` bounds a spend respects). |
| `PROGRESSION_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | The lifecycle sub-blob version (independent of every other format version; §3.5). |
| `PROGRESSION_SAVE_MAGIC` | `[FIXED]` | `0x50524F47` (`"PROG"`) | The #28 sub-blob's self-identifying leading tag, written BEFORE the version (ERR-028-004) — deliberately NOT the `DOMAIN_TAG_PLAYER_PROGRESSION` RNG tag §3.5 once named in its place: every sub-blob format in the save stack sits at version 1, so without a magic each codec would decode a sibling's bytes cleanly and silently (ERR-029-005/ERR-041-009). |
| `PROGRESSION_NOT_ADVANCED_SENTINEL` | `[FIXED]` | `uint.MaxValue` | `PlayerLifecycle.LastAdvancedWorldDay`'s never-advanced sentinel value — `uint.MaxValue` rather than 0 because day 0 is a legitimate world day (the day-0 trap; the #29 `TRAINING_NOT_ADVANCED_SENTINEL` precedent). **Not a legal stored cursor value as of ERR-028-014** — `SeedFrom` anchors the cursor at the seed day and `FromBlocks` refuses a lifecycle carrying it (§5.9 T-PG-BLOCK-007); the constant survives as the refused-`worldDay`-argument value `AdvanceDay` checks against (F8, §5.7 T-PG-SAVE-004). |
| `DOMAIN_TAG_PLAYER_PROGRESSION` | `[CROSS]` | `0x20` | **Not yet in the catalogue (ERR-028-017, correcting this row's own "copied verbatim from code" claim in the v0.3 header) — lands with the regen stream.** `PlayerProgressionConstants.cs` names this tag only in doc-comment prose (e.g. its file-header cross-reference), not as a declared `const`; #16 §3.4's `_RESERVED_0x20_` row is not yet promoted to a live #28 constant, since the landing has no draw site (§3.2/§3.3). This row records the intended future constant, not a present one. |
| `SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` | `[CROSS]` | 82 | **Not yet in the catalogue, same correction, same reason** — `SubsystemOrdinals.PlayerProgression = 82` is reserved in #16 §3.4 text but has no live #28 mirror until the regen stream registers one. |
| `PROGRESSION_REGEN_FIELDS` | `[DERIVED]` | (= regen draw budget) | Fixed per-regen reservation size (the #27 `FIELDS_PER_PLAYER` discipline, §3.3). |
| `ABILITY_MAX` | `[GT]` | 10000 | The wide-integer CA/PA scale ceiling. |
| `POINT_COST` | `[GT]` | (= `DAYS_PER_YEAR`) | Cursor points per whole attribute-point; over a full band traversal (§3.1.3's accrual curve — exact at every ramp half-width including the shipped one, per the curve's own P5 pivot) this makes exactly one `[1,20]` step per year (KD-8). *(Corrected — round-2 finding spec-32-flat-band-step-sweep-stopped-two-paragraphs-short: this row said "the §4.3 band step", present tense, after ERR-028-020 made §4.3's flat step the retired predicate; the KD-8 identity itself — one step per year of the band — is unchanged, since the ramp's whole-life integral equals the step's exactly.)* |
| `GROWTH_AGE` | `[GT]` | 24 | Age below which a player is in the Growth band (§4.3 <24 → +1/yr). |
| `DECLINE_AGE` | `[GT]` | 30 | Age above which a player is in the Decline band (§4.3 >30 → −1/yr). |
| `RETIREMENT_AGE` | `[GT]` | 36 | **Baseline** retirement age (§3.4; deterministic, no draw). **No longer the whole rule (ERR-028-021)** — the per-player retirement day is this plus `RETIREMENT_GOALKEEPER_BONUS_YEARS` plus the game-reading offset, compared in DAYS. At a zero bonus and a zero span it IS the whole rule, identically to the retired `AgeYears >= RETIREMENT_AGE` comparison. |
| `AGE_BAND_RAMP_HALF_WIDTH_YEARS` | `[GT]` | 2 | **Added ERR-028-020.** Half-width, in years, of the centred linear ramp carrying the daily accrual rate across each band edge (§3.1.3). **Zero is the exact §4.3 identity**, not an approximation of it — the ramps collapse to the retired step functions and every day's accrual is byte-identical, which is what keeps KD-8 / FR-PG-007 expressible (the #41 FR-MD-027 / #30 KD-7a dial posture: the off value routes to the retired path verbatim). **P5 is exact for ANY half-width**: a centred ramp has the same integral as the step it replaces, so the curve redistributes accrual across an edge without creating or destroying any, and ERR-028-018's no-residue traversal invariant survives by construction. MUST be **non-negative** and MUST leave the two ramps disjoint — `2 x half-width <= (DECLINE_AGE + 1) - GROWTH_AGE`, i.e. <= 3 at today's 24/30, since a day inside both ramps would accrue growth and decline at once. **Both enforced fail-loud at the computing site** (the `MedicalStep.DrawOccurrence` posture) **and at the catalogue** (`PlayerProgressionConstantsTests`). *(Corrected — config-unbound-premise-false-28, football-judgment proxy review batch-1: this row justified the computing-site placement with "this is a config key and the catalogue lock runs config-unbound, so it sees only the fallback" — copied from ERR-041-003's rationale, where it is true, without checking it against this catalogue. `PlayerProgressionConstants.cs` has ZERO `Config.GetX` calls today, so a catalogue lock is not defeated here; the computing-site guard is a forward-looking placement for the Stage-1 loader, and a catalogue-level lock now exists alongside it.)* |
| `RETIREMENT_GOALKEEPER_BONUS_YEARS` | `[GT]` | 3 | **Added ERR-028-021.** Years added to a goalkeeper's retirement age (§3.4). Position is the one career-length input this spec already holds on the record, and goalkeepers demonstrably outlast outfielders. Zero restores the single league-wide age exactly. MUST be non-negative (fail-loud at the computing site). |
| `RETIREMENT_GAME_READING_SPAN_YEARS` | `[GT]` | 4 | **Added ERR-028-021.** Full attribute-range span, in years, of the game-reading retirement offset (§3.4): the floor of the Anticipation / Positioning / Composure mean retires `span / 2` years early, the ceiling `span / 2` late, every point between moving the day linearly (the full-range form `ERR-008-019` was owner-revised to — no plateau anywhere). **Doctrine P3, recorded:** robustness is the obvious input and is deliberately NOT used — #29's `ComputeInjuryRisk` and #41's `RobustnessMitigation` already price Strength/Stamina/Balance twice over (`ERR-041-003`), so a third read would be that defect a third time; career length is owned by the reading trio, which nothing else consumes. **P5 exact**: the offset is anti-symmetric about the attribute midpoint and integer division truncates toward zero symmetrically, so the offsets over a uniform `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` population sum to exactly 0 — the league's retirement RATE is unchanged and only who-retires-when moves. Zero restores the single hard age exactly. MUST be non-negative, and the computed day MUST be positive (both fail-loud at the computing site). **Corrected at ERR-028-022 (August 22, 2026): the P5 sentence above was published against an implementation for which it was FALSE, and is annotated rather than restated.** §3.4 floored the reading trio to a mean before the anti-symmetric map, and `floor(sum/3)` is not symmetric about the midpoint, so the cancellation held only along the `Ant == Pos == Comp` diagonal: the true sum over the uniform `[1,20]³` product was **−204,621 days (−25.58 d/player)**, i.e. the whole league retiring ~2 months early — a rate change, which is exactly what the sentence denied. §3.4 now carries the SUM undivided (`(2·sum − 3·(MIN + MAX)) · span / (6 · (MAX − MIN))`), which sums to exactly 0 over that product and reproduces the retired diagonal values bit-for-bit. **Residual, stated not papered over:** the uniform product is an idealisation — #27's `RosterGenerator` draws `[6,14]`, centred on 10 against the offset's neutral 10.5 — so the generated league still averages **≈ −38 days per player**. Re-pivoting on the generator's mean is deliberately NOT done: it is the coupling `ERR-041-020` refused for `AGE_RISK_PIVOT_YEARS`, and it would silently re-pivot #28 the day #47's authored database replaces those bounds. |
| `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` | `[GT]` | +1 / −1 | Per-day cursor accrual at the FULL rate of the Growth / Decline phase; `POINT_COST = DAYS_PER_YEAR` ⇒ one step/year at full rate. *(Since ERR-028-020 the accrual is `AbilityModel.DailyBandPoints(ageDays)` — the difference of an exact integer cumulative — so a day inside a ramp takes 0 or the full step and the DENSITY follows the continuous rate; these two values are the rate's magnitude, no longer a three-way lookup.)* |
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
alone — there is no discrete year-rollover step (age is a pure function of the world day, §3.1.1).

**This table describes BOTH `GrowthProjection.AdvanceDayForPlayer` called directly AND the public
`ProgressionEngine.SeedFrom` + `AdvanceDay` entry point a real career uses — as of ERR-028-018
(August 10, 2026) they agree exactly, world-day for world-day.**

**Correction to the ERR-028-017 scope note previously here (falsified by execution the same day,
ERR-028-018):** that note said the public entry point spends its first point one day later than this
table (world-day 365, not 364) purely because `SeedFrom` anchors `LastAdvancedWorldDay` at the seed
day, and called it "only the day-column labels differ by one" while asserting "the per-player
projection itself is unchanged and this table's arithmetic is correct." Both claims were false. The
band exit is decided by the DERIVED AGE, not by the cursor, so leaving the seed day's own band step
uncredited (`GrowthCursor = 0` at seed) shifted the accrual window one day right of a fixed band edge —
harmless for a single intra-band year (which is all this table exercises), but for a full N-year band
traversal it accrued N·365 − 1 days instead of N·365: one whole `[1,20]` attribute point short every
time, since `POINT_COST == DAYS_PER_YEAR` (KD-8). Measured through the public API before the fix:
seedAge 16 (8 years of Growth) gained 7 points with a 364-day residue cursor; seedAge 23 (1 year)
gained ZERO. The residue survived the Stable band (which accrues nothing, so it can never be spent)
and ate the first year of Decline. Appendix A and KD-8 both promise +1/yr, so this contradicted
normative spec text — a defect, not a scope difference.

**The fix:** `SeedLifecycle` now credits the seed day's own band step at construction
(`GrowthCursor = DailyBandPoints(Age₀ · DAYS_PER_YEAR)`, §3.1.3's continuous curve) instead of starting
every band at cursor `0`.
*(**Corrected at ERR-028-023, August 22, 2026 — this sentence is ERR-028-018-era text.** As published
it described the credit as `GROWTH_DAILY_POINTS` for a Growth-band seed, `DECLINE_DAILY_POINTS` for
Decline and `0` for Stable — the three-way band step ERR-028-020 retired — in the **present tense**,
directly above the currency note added beneath this table for that very landing. The two forms disagree
at bootstrap ages **24, 25, 29, 30**, i.e. 4 of the 19 ages #27's `RosterGenerator` draws, so the
described behaviour reopened this appendix's own defect for ~21% of a bootstrapped roster. The claim
about THIS table's player is unaffected: he is far below the growth ramp, where curve and step agree
day for day.)* A player seeded at world-day 0 now reaches
world-day 364 with `GrowthCursor = 365` and spends there — exactly the row below, exactly the direct-call
model, no residual one-day offset. Mutation-verified: reverting the seed-day credit fails 6 of 109
`PlayerProgression.Tests`, including the new `AdvanceDay_AWholeGrowthBandTraversal_
GainsExactlyOnePointPerYear_AndLeavesNoResidue` lock, which this table's single-year example did not
by itself have the reach to catch:

| World-day | Derived age band | Accrue | Cursor | Spend? | Passing | Save→restore here == continuous? |
|---|---|---|---|---|---|---|
| 0 | Growth | +1 | 1 | no | 12 | yes (cursor 1) |
| 200 | Growth | +1 | 201 | no | 12 | yes (cursor 201) |
| 364 | Growth | +1 | 365 | **spend 1** → cursor 0 | **13** | yes (cursor 0 post-spend) |
| 365 | Growth | +1 | 1 | no | 13 | yes (cursor 1) |

**Currency note (ERR-028-020, August 22, 2026).** This table is still exact, and it is worth saying why
rather than leaving a reader to check: the example's player is age < `GROWTH_AGE` by more than
`AGE_BAND_RAMP_HALF_WIDTH_YEARS`, so every day it covers sits at the FULL growth rate, where the ramped
curve and the retired band step agree day for day. What the ramp changes is the *edges* — a player
crossing `GROWTH_AGE` now tapers to zero across `±half-width` instead of stopping on his birthday — and
this table exercises no edge. The invariant it was written to protect (a completed traversal gains
exactly one point per year and leaves **no residue**) also survives, because a centred ramp has the same
integral as the step: §5's traversal lock now runs to the END of the ramp and asserts the identical
totals. One case IS new and is deliberately locked separately: a player *seeded inside* a ramp finishes
growth holding a fractional cursor, which is carried against his first days of decline rather than lost
— arithmetically distinct from the whole-point shortfall ERR-028-018 fixed, and easy to mistake for its
recurrence.

A save on day 200 (cursor 201) restores and reaches day 364 → cursor 365 → spends exactly once → 13,
identical to the uninterrupted run. There is no anchor to re-cross and no discrete rollover, so a save
on day 364, 365, or 366 all restore to the same continuation — nothing is double-counted (the age band
is recomputed from `BirthWorldDay` each day, never banked). Integer-only ⇒ `CanonicalSerializer`
round-trips it bitwise.

## Appendix C — Worked example: retirement + regen at the season boundary (KD-5/KD-6)

Player `PlayerId = 175` (club 7, localIndex 0) — an outfielder of average Anticipation / Positioning /
Composure, so his `RetirementAgeDays` is exactly `RETIREMENT_AGE · DAYS_PER_YEAR` — reaches it on
world-day 4020 mid-season:

*(**ERR-028-021**: the threshold is per-player and compared in days. This example keeps the plain
`RETIREMENT_AGE` arithmetic because its player is the P5 pivot case — an average-reading outfielder,
whose offset is 0. A goalkeeper of the same age and attributes would not yet be flagged: his day sits
`RETIREMENT_GOALKEEPER_BONUS_YEARS · DAYS_PER_YEAR` later. The boundary flow below — flag now, roster
mutation at the season boundary — is unchanged by that ERR.)*

*(**Corrected at ERR-028-022, August 22, 2026 — "whose offset is 0" is not attainable, and was not
attainable under the arithmetic it was written against either.** The offset's neutral point is the
attribute-range **midpoint**, `(ATTRIBUTE_MIN + ATTRIBUTE_MAX) / 2 = 10.5`, which no integer attribute
can sit on — so **no** player's offset is exactly 0, and this example's "exactly
`RETIREMENT_AGE · DAYS_PER_YEAR`" never held. Under §3.4's corrected sum-carrying form the nearest
outfielders are those whose reading trio sums to 31 (**−12 days**) or 32 (**+12 days**); an all-10
outfielder, the closest thing to "average" the [1,20] scale offers, sums to 30 and retires **−38
days** early — which is exactly the ≈ −38 d/player residual Appendix A records for #27's generated
population, arriving here as one player. Under the retired floored-mean form the same all-10 player
was also −38, so the example's day was already off by that much when it was written; what ERR-028-022
changes is the SHAPE of the map, not this player's number. **The example's flow — flag on the day the
threshold is crossed, roster mutation at the season boundary — is unaffected, and so is world-day
4020 as an illustrative crossing day**; what is corrected is the claim that the crossing day equals
the baseline exactly for this or any player. Read `RetirementAgeDays` as the authority and this day as
one worked instance of it.)*
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
| 0.4 | 2026-08-10 | — | ERR-028-017 (AR pass 5 spec-vs-code sweep, no code change): the v0.3 row above claims Appendix A's values are "copied verbatim from code", but `DOMAIN_TAG_PLAYER_PROGRESSION` and `SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` were never declared constants at all — grep of `PlayerProgressionConstants.cs` finds them only in doc-comment prose (the class summary, the `PROGRESSION_SAVE_MAGIC` comment); both rows corrected to "not yet in the catalogue — lands with the regen stream" rather than listed as present. Appendix B's worked example gains a scope note: it describes `GrowthProjection.AdvanceDayForPlayer` called directly, and the public `SeedFrom`+`AdvanceDay` entry point a real career uses spends its first point one day later (world-day 365, not 364) because `SeedFrom` anchors the cursor at the seed day (ERR-028-014) rather than treating day 0 as the first day to accrue. The table's own arithmetic is unchanged and correct for what it exercises. |
| 0.5 | 2026-08-10 | — | ERR-028-018: the v0.4 scope note above is CORRECTED, not merely superseded — it was falsified by execution. `SeedLifecycle` crediting the seed day as "already lived" while leaving `GrowthCursor` at 0 was not a label-only discrepancy; a band exit is decided by the DERIVED AGE, so the uncredited seed day cost one whole attribute point per full band traversal (N·365 − 1 days accrued, not N·365) and left a 364-day residue that ate the first year of Decline — measured via the public API, not merely reasoned. `ProgressionEngine.SeedLifecycle` now credits the seed day's own band step (commit `789ea74`), and Appendix B's table now describes the public `SeedFrom` + `AdvanceDay` entry point exactly, not just the direct-call model. Doc-only here; the code fix landed at `789ea74` without this close-out, which this row and `spec-error-log.md` ERR-028-018 supply retroactively (FR-CS-057). |
| 0.6 | 2026-08-11 | — | ERR-028-019 — docs close-out for AR passes 5-8 (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`), landed with no `docs/specs/` edit. Appendix A gains `MAX_DERIVABLE_AGE_YEARS`, `[FIXED]` (a representability bound — see the row's own note for why football-plausibility was the wrong justification and how that was caught the same session it was introduced). Appendix B's worked example is unaffected by these four commits — it exercises a single intra-band year, never the PA-ceiling or fully-drained refusal branches AR passes 6/8 changed (§3.1's now `GrowthCursor = 0` clamp on both sides) — so no correction is recorded here; §3.1/§3.5 carry the algorithm-level detail. Code unchanged by this pass; verified against `src/player-progression/PlayerProgressionConstants.cs` at commit `6987dbf`. |
| 0.7 | 2026-08-22 | — | **ERR-028-020 / ERR-028-021** (football-judgment proxy review, batch 1 — spec + code, same commit). Appendix A gains three `[GT]` rows: `AGE_BAND_RAMP_HALF_WIDTH_YEARS` (the centred accrual ramp of the new §3.1.3, with its exact-zero identity, its exact-for-any-half-width P5 property, and both fail-loud invariants — non-negative, and ramps disjoint), `RETIREMENT_GOALKEEPER_BONUS_YEARS` and `RETIREMENT_GAME_READING_SPAN_YEARS` (§3.4's per-player retirement day, including the doctrine-P3 record of why robustness is deliberately NOT the input and the exact anti-symmetry that keeps the league retirement rate unchanged). `RETIREMENT_AGE`'s row corrected from "hard retirement age" to the BASELINE of a per-player computation, and `GROWTH_DAILY_POINTS`/`DECLINE_DAILY_POINTS` from a three-way band lookup to the rate's magnitude. Appendix B gains a currency note establishing that its table is unchanged (it exercises no band edge) and naming the one genuinely new case — a mid-ramp seed's carried fraction, which resembles ERR-028-018's shortfall and is not it. Appendix C's worked example states that its player is the P5 pivot case and what a goalkeeper's day would be. |
| 0.8 | 2026-08-22 | — | **ERR-028-022 + ERR-028-023** — the reviewed High findings against the v0.7 landing, spec + code in the same commit. **ERR-028-022 (Appendix A):** the `RETIREMENT_GAME_READING_SPAN_YEARS` row's P5 claim was published against an implementation for which it was false — §3.4 floored the Anticipation/Positioning/Composure sum to a mean before the anti-symmetric map, and `floor(sum/3)` is not symmetric about the attribute midpoint, so the offsets cancelled only along the `Ant == Pos == Comp` diagonal and summed to **−204,621 days (−25.58 d/player)** over the uniform `[1,20]³` product: the whole league retiring about two months early, which is the retirement-RATE change the row denied. Annotated in place (never restated) with §3.4's corrected sum-carrying form, and with the residual recorded rather than papered over: #27's `RosterGenerator` draws `AttributeBaseMean ± AttributeSpread` = `[6,14]`, centred on 10 against the offset's neutral 10.5, so a generated league still averages ≈ −38 days per player. Re-pivoting on the generator's mean is deliberately not done — the coupling `ERR-041-020` refused for `AGE_RISK_PIVOT_YEARS`, and it would re-pivot #28 silently when #47's authored database replaces those bounds. **ERR-028-023 (Appendix B):** the "The fix" paragraph described the ERR-028-018 seed-day credit as `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` / `0` by seed-time band, in the present tense, directly above the ERR-028-020 currency note added under the same table — the retired three-way step asserted as current behaviour in the appendix that documents the defect it causes. The two forms disagree at bootstrap ages **24, 25, 29, 30** (4 of the 19 ages `RosterGenerator` draws), so the described behaviour reopens the one-day accrual discrepancy for ~21% of a bootstrapped roster. Corrected to `DailyBandPoints(Age₀ · DAYS_PER_YEAR)` and marked as ERR-028-018-era text; the table's own arithmetic is unaffected, its player sitting far below the growth ramp. **ERR-028-022 (Appendix C):** the retirement worked example asserted its player is "the P5 pivot case — an average-reading outfielder, whose offset is 0", so his retirement day is "exactly `RETIREMENT_AGE · DAYS_PER_YEAR`". No player's offset is 0 under EITHER form: the neutral point is the attribute-range midpoint 10.5, which no integer attribute can occupy. Corrected in place with the reachable values — reading trios summing to 31 or 32 give ∓12 days, and an all-10 outfielder (the closest the scale offers to "average") gives −38, the same figure Appendix A now records as the generated population's residual, arriving as a single player. The example's flow and its illustrative world-day 4020 crossing are left standing; what is withdrawn is the claim that any player's crossing day equals the baseline exactly. |
| 0.9 | 2026-08-23 | — | Football-judgment proxy review, batch-1 adversarial finding config-unbound-premise-false-28. Appendix A's `AGE_BAND_RAMP_HALF_WIDTH_YEARS` row justified its computing-site guard with "this is a config key and the catalogue lock runs config-unbound, so it sees only the fallback" — the ERR-041-003 rationale, copied here without checking `PlayerProgressionConstants.cs`, which has zero `Config.GetX` calls today. Corrected: the computing-site guard is a forward-looking placement for the Stage-1 config loader, and a catalogue-level lock (`PlayerProgressionConstantsTests`) now exists alongside it. No value changed. |
| 0.10 | 2026-08-24 | — | Round-2 finding spec-32-flat-band-step-sweep-stopped-two-paragraphs-short. Appendix A's `POINT_COST` row still read "with the §4.3 band step this makes exactly one `[1,20]` step per year" — the same stale framing the v0.9-adjacent `section-3.md` v0.11 sweep corrected elsewhere but did not reach here. Restated against §3.1.3's accrual curve; the KD-8 identity itself is unchanged (the ramp's whole-life integral equals the retired step's exactly, at every half-width — the P5 pivot), so no value or number moved. |
#endregion
