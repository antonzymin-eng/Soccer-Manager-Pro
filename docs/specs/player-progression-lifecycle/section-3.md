# Player Progression & Lifecycle #28 — Section 3: Core Algorithms

**Created:** July 23, 2026
**Last Updated:** August 24, 2026 (v0.13 — round-2 Medium/Low adversarial findings. M2
(spec-32-flat-band-step-sweep-stopped-two-paragraphs-short): §3.2's "Recorded limitation, not fixed
here" paragraph — two paragraphs below the v0.11 correction of the same stale phrasing — restated
against §3.1.3's accrual curve instead of "the §4.3 flat band step"; the ~421-of-ABILITY_MAX figure is
UNCHANGED (the P5 pivot means the whole-life integral is exact at every half-width, so the number needed
no re-derivation). L1 (four-guards-enumerated-as-five-and-mis-named): §3.4's `RetirementAgeDays`
pseudocode note corrected — it said "these two dial guards", describing the single combined
non-negativity `if` as two guards, which made the paragraph claim "four" and then list five items; now
"this ONE combined dial guard". M5 (retirement-day-derived-from-attributes-the-same-step-mutates): new
§3.4 paragraph states the retirement-day feedback-loop invariant explicitly — the day is re-evaluated
against the same record §3.1.2 just mutated, bounded today only by the one-directional band order and
`RetirementFlag`'s stickiness — and points at the new **T-PG-RET-009** lock (`section-5.md` v0.9). No
numeric value, no draw, no format version. Prior entry below.)
**Last Updated (prior):** August 24, 2026 (v0.12 — round-2 adversarial finding `construction-day-credit-implemented-twice` (High), spec + code in the same commit: the construction-day credit is stated as having ONE implementation, `AbilityModel.ConstructionDayCredit`, which both `SeedFrom` (§3.1) and `GenerateRegen` (§3.3) MUST call rather than compute locally. §3.3's `BandStepFor(age)` reference is retired with the method — it was character-for-character the expression `SeedLifecycle` also carried, which is why ERR-028-018 credited one site and missed the other and ERR-028-020 had to revisit both. Behaviour unchanged (verified by probe over ages 0..200 and the `int` edges before the collapse); no new constant, no draw, no format version. Prior entry below.)
**Last Updated (prior):** August 23, 2026 (v0.11 — football-judgment proxy review, batch-1 adversarial findings: §3.4's `RetirementAgeDays` pseudocode gains the missing `RETIREMENT_GAME_READING_SPAN_YEARS`/`RETIREMENT_GOALKEEPER_BONUS_YEARS` non-negativity guard, Appendix A/the code already had (guards-unexercised); §3.1.3's disjointness-guard rationale corrected — `PlayerProgressionConstants.cs` has zero `Config.GetX` calls, so "the catalogue lock runs config-unbound" was false here (config-unbound-premise-false-28); §3.2's `DailyPoints` description corrected from "the flat §4.3 band step" to `DailyBandPoints`'s ramp, present tense having gone stale at ERR-028-020 (spec-32-stale-flat-band-step); §3.5's `birthWorldDay` value-gate rationale corrected — `ClassifyAgeBand` no longer reads `int.MinValue` as Growth (classifyageband-growth-claim-stale). Prior entry below.)
**Last Updated (prior):** August 22, 2026 (v0.10 — **ERR-028-022 + ERR-028-023, the reviewed High findings against the v0.9 landing, spec + code in the same commit.** ERR-028-022: §3.4's game-reading offset floored the Anticipation/Positioning/Composure SUM to a mean before the anti-symmetric map, and `floor(sum/3)` is not symmetric about the attribute midpoint — so v0.9's published "the offsets sum to exactly 0 over a uniform attribute population, the league's retirement RATE is unchanged" P5 claim was FALSE off the `Ant == Pos == Comp` diagonal, by −204,621 days over the uniform `[1,20]³` product (−25.58 d/player: the whole league retiring ~2 months early). The pseudocode now carries the sum undivided — exactly anti-symmetric, and bit-for-bit identical to v0.9 on the diagonal — and the superseded claim is annotated in place, together with the residual honest fact that #27's generator draws `[6,14]` (centre 10) against the model's neutral 10.5, leaving ≈ −38 days per generated player. ERR-028-023: §3.1's normative seed-credit MUST still mandated the three-way band step ERR-028-020 retired (`GROWTH_DAILY_POINTS` in Growth, `DECLINE_DAILY_POINTS` in Decline, `0` in Stable), which disagrees with the shipped `DailyBandPoints(Age₀ · DAYS_PER_YEAR)` at bootstrap ages 24, 25, 29 and 30 — 4 of the 19 ages `RosterGenerator` draws — so an implementer following the spec reopened ERR-028-018's one-day discrepancy for ~21% of the roster; amended to mirror §3.3's already-corrected regen wording. Neither fix adds a draw, a stream, a domain tag or a format version. Prior entry below.)
**Last Updated (prior):** August 22, 2026 (v0.9 — **ERR-028-020 + ERR-028-021, the football-judgment proxy review's batch-1 #28 findings, spec + code in the same commit.** ERR-028-020: §3.1's daily accrual is no longer `DailyPoints(ClassifyAgeBand(ageYears), …)` — a hard step at an exact integer age on a continuous football judgment (pattern (b), and (d) against §1.3's promised age-keyed curves) — but the age-CONTINUOUS `DailyBandPoints(ageDays)` of the new **§3.1.3**, a centred linear ramp of half-width `AGE_BAND_RAMP_HALF_WIDTH_YEARS` at each edge, expressed as the difference of an exact integer cumulative so the cursor scale and the save format are untouched; the P5 pivot is exact (both integrals equal the step model's for every half-width, including the 0 that reproduces KD-8's identity byte-for-byte), and `ClassifyAgeBand` is demoted to a READ of the curve rather than a second authority over it. ERR-028-021: §3.4's `AgeYears >= RETIREMENT_AGE` — one integer age for the whole league, no position or attribute input — becomes a per-player `RetirementAgeDays(record)` in days, with a goalkeeper allowance and a full-range game-reading offset over Anticipation/Positioning/Composure, chosen over robustness under doctrine P3 because #29 and #41 already price that trio twice (`ERR-041-003`); anti-symmetric, so the league retirement rate is unchanged and only who-retires-when moves. Neither fix adds a draw. Prior entry below.)
**Last Updated (prior):** August 11, 2026 (v0.8 — ERR-028-019: docs close-out for AR passes 5-8, four consecutive production landings (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`) with no `docs/specs/` edit — §3.1's spend/drain pseudocode rewritten for the AR pass 6/8 changes (fail-loud on a future-dated `BirthWorldDay`, saturating age narrowing at `MAX_DERIVABLE_AGE_YEARS`, both refusal branches clamp to 0 rather than banking or leaving the cursor, `DrainOnePoint` returns `bool`); §3.1.1's age-formula guard corrected from "guarded at zero" to "fails loud below zero, ordinary at zero"; §3.3 states the AR pass 7 regen construction-day credit; §3.5's fail-loud enumeration rewritten from four value gates to eight, with the Encode/FromBlocks-vs-Decode exception-type split stated per gate, and gains the FR-PG-011 id-cursor and M3 club-size rules (previously undocumented); a new OPEN decision recorded on the `CurrentAbility`/`ComputeCA` save-acceptance predicate, adjacent to the existing `PA_MIN` one)
**Last Updated (prior):** August 10, 2026 (v0.7 — ERR-028-018: §3.1 states the seed-day accrual-window rule — the seed day's own band step MUST be credited to `GrowthCursor`, not merely excluded from replay — closing the gap that let a full band traversal accrue one attribute point short of Appendix A / KD-8's `+1/yr` promise)
**Last Updated (prior):** August 10, 2026 (v0.6 — ERR-028-017: AR pass 5 spec corrections — §3.1.1 states the `ageDays ≤ 0 → age 0` guard the formula omitted; §3.4 states the retirement evaluation runs once per `AdvanceDay` CALL on post-replay age (not once per lived day), with the multi-day-gap `RetirementDay` limitation recorded and cross-referenced to T-PG-DET-002; §3.5's byte layout pins the `str` encoding (u32 length + ASCII, #16 §3.2.4.1) and states the four VALUE gates `Decode` applies (previously undocumented), with the `PA_MIN`/`ABILITY_MAX` config-keyed-acceptance-predicate tension against #30 Appendix B.1's posture recorded as an OPEN decision)
**Last Updated (prior):** August 9, 2026 (v0.5 — ERR-028-014: the never-advanced sentinel retired from #28's legal store states)
**Last Updated (prior):** August 8, 2026 (v0.4 — ERR-028-006/007/008/009: the signed age anchor, the cross-blob cursor rule, the destination-roster-overwrite refusal, and the F8 sentinel guard)
**Version:** 0.13
**Status:** APPROVED

---

All algorithms run on the world tick and are testable without Unity (integer-only except the derived
CA summary, which is integer too). The `[GT]` magnitudes are illustrative pending the balance pass;
the contract is the shapes and the byte-exactness.

## 3.1 The daily growth projection (KD-1) — integer fixed-point, byte-exact

The daily step is a pure function of the player's state + inputs — **no RNG draw** (FR-PG-002). It is
the single writer of attribute change (FR-PG-008).

**FR-PG-021's public entry point.** `AdvanceDayForPlayer` below is the per-player projection; #30
drives the batch that wraps it, once per carried club per carried player, in ascending `ClubId` /
`PlayerId` order:

```
AdvanceDay(worldDay, in trainingInputs):          # FR-PG-021, the public entry point #30 drives
    if worldDay == PROGRESSION_NOT_ADVANCED_SENTINEL: FAIL LOUD     # F8 (ERR-028-009)
    for each carried club, in ascending ClubId:
        for each carried player, in ascending PlayerId:
            if worldDay > lifecycle.LastAdvancedWorldDay:
                for d in (lifecycle.LastAdvancedWorldDay + 1) .. worldDay:
                    AdvanceDayForPlayer(..., d, ...)          # gap-complete: the cursor is an accumulator
                lifecycle.LastAdvancedWorldDay = worldDay
            else:
                skip                                          # idempotent per day (ERR-030-027)
```

**The seed day IS the cursor (ERR-028-014) — there is no never-advanced branch.** An earlier revision
of this pseudocode carried a first case — `if lifecycle.LastAdvancedWorldDay == PROGRESSION_NOT_ADVANCED_SENTINEL:
AdvanceDayForPlayer(..., worldDay, ...)` — with a comment claiming the store "anchors; cannot know an
earlier start." That comment was the defect, not merely imprecise phrasing: the store **can** know,
because `SeedFrom` (§3.1.1) is handed `newGameWorldDay` and anchors `LastAdvancedWorldDay` to it at
generation — a generated player's records describe the roster **as of** that day, so that day is
already accounted for, not a day still to be lived. Seeding the sentinel there instead made "where does
this career's lived history start" unrepresentable: composing a store seeded at day 0 against a world
clock at day 3650 (a save loaded far into a career, or a fresh regen bootstrap paired with a running
season) collapsed a decade-long span into a single day's accrual on the first `AdvanceDay` call, while
every player's **derived** age (§3.1.1) still read ten years on — silently. `FromBlocks` (the restore
path) now **refuses** to construct a lifecycle carrying the sentinel cursor at all (F8's sibling rule,
below), which makes the sentinel a refused world **day**, never a legal store **state**. With every
carried player guaranteed a real cursor, only two cases remain — a gap to replay, or the day is already
done — which is the pseudocode above.

**"Already accounted for" means the seed day's own band step is CREDITED, not skipped
(ERR-028-018).** Anchoring `LastAdvancedWorldDay` at the seed day, on its own, only stops that day
from being replayed — it says nothing about whether the day contributed to `GrowthCursor`. A band exit
is decided by the player's **derived age** (§3.1.1), not by the cursor, so a seed that anchors the
cursor but leaves `GrowthCursor = 0` still shifts the accrual window one day right of every fixed band
edge: a full traversal of an *N*-year band then accrues `N · DAYS_PER_YEAR − 1` days rather than
`N · DAYS_PER_YEAR`, and because `POINT_COST == DAYS_PER_YEAR` (KD-8) that is one whole `[1,20]`
attribute point short, every single traversal — with the shortfall banked as a permanent residue that
survives the (accrual-free) Stable band and eats the first year of Decline. `SeedFrom` (§3.1.1) MUST
therefore seed `GrowthCursor` at the seed day's own step taken from §3.1.3's continuous curve,
`DailyBandPoints(Age₀ · DAYS_PER_YEAR)` — asked of the credit's single owner,
`AbilityModel.ConstructionDayCredit(Age₀)`, never recomputed here (§3.3 states why that is normative
and not a style preference) — the single call to `AdvanceDayForPlayer` line 2 would have
made on that day, without also running its spend/drain step or its `LastAdvancedWorldDay` write (both
already handled by the anchor). This is not derivable from "the seed day is already accounted for" by
itself; that sentence is a claim about the CURSOR's correctness, and crediting the band step is what
makes it true rather than aspirational.

*(**Amended at ERR-028-023, August 22, 2026.** This MUST previously named the seed-time age BAND and
its three constants — "`GROWTH_DAILY_POINTS` in Growth, `DECLINE_DAILY_POINTS` in Decline, `0` in
Stable" — which is the three-way step ERR-028-020 retired, still mandated normatively one section above
the curve that replaced it. The two forms disagree at bootstrap ages **24, 25, 29, 30** at the shipped
`AGE_BAND_RAMP_HALF_WIDTH_YEARS`, i.e. 4 of the 19 ages #27's `RosterGenerator` draws, so an implementer
following the retired wording reopened ERR-028-018's one-day accrual discrepancy for roughly a fifth of
the roster — silently, and only inside the ramps, where nothing outside them would show it. The code
has computed `AbilityModel.DailyBandPoints(rec.Age · DAYS_PER_YEAR)` since ERR-028-020 landed; the
ERR-028-020 commit amended the sibling regen paragraph in §3.3 for exactly this reason and did not
reach this one. Same wording, same authority, same single curve.)*

**The F8 guard runs before anything else in the batch (ERR-028-009).** #29's `TrainingStep` and #41's
`MedicalStep` both refuse `worldDay == sentinel` under an explicit F8 row landed one day before #28's
own T1/T2a landing shipped without one — the same folder-boundary lesson recurring immediately. Two
concrete consequences without the guard: `AdvanceDay(sentinel)` would **store** the sentinel as a real
cursor value, so the step stops being idempotent (a second identical call accrues again, breaking the
ERR-030-027 contract this section itself relies on two paragraphs down); and the gap-replay loop `for d
in (cursor+1)..worldDay` never terminates when `worldDay` is `uint.MaxValue`.

Idempotency is required because #30's `AdvanceAndPlayNextRound` runs a fixture day's KD-2 slots
**twice** — once pre-round and once from the advance loop (ERR-030-027) — and each subsystem is
responsible for absorbing its own re-entry; without the cursor, the second call would bank a second
day of growth for every fixture day, a silent ~11% rate error rather than a crash (ERR-028-005).
Gap-completeness is the other half of the same cursor: it is what makes §5.2's T-PG-DET-002 true,
because age is derived (gap-independent — §3.1.1) but `GrowthCursor` is an accumulator that must see
every intervening day's `dailyPts` or lose them across the gap.

```
AdvanceDayForPlayer(ref record, ref lifecycle, worldDay, in trainingInput, curveEnabled):
    # 1. Age is DERIVED — no discrete rollover step (§3.1.1); attribute change is the cursor alone.
    ageDays = worldDay - lifecycle.BirthWorldDay
    if ageDays < 0: FAIL LOUD                          # M2(a), ERR-028-019 — a future-dated anchor is
                                                         #   corrupt state, never age 0 (§3.1.1)
    ageYears  = saturate(ageDays / DAYS_PER_YEAR, at MAX_DERIVABLE_AGE_YEARS)   # AR pass 5, ERR-028-019
    record.Age = ageYears                                              # keep the record's Age current (cache)

    # 2. Per-day point accrual — the ONLY accumulator (FR-PG-002/003).
    #    ERR-028-020: the rate is a continuous function of ageDAYS (§3.1.3), NOT a three-way band on
    #    ageYEARS. ClassifyAgeBand survives only as a DESCRIPTION of the curve (§3.1.3) — it is no
    #    longer consulted here, and re-deriving a band from GROWTH_AGE/DECLINE_AGE at this line would
    #    reinstate the cliff behind the fix.
    dailyPts  = DailyBandPoints(ageDays)                               # signed integer, fixed-point (§3.1.3)
    lifecycle.GrowthCursor += dailyPts

    # 3. Spend/drain whole attribute-points at the POINT_COST threshold (deterministic order).
    #    Both loops CLAMP the cursor to 0 on a refused step and BREAK (AR pass 6, ERR-028-019) — a
    #    refusal here is PERMANENT within the current band (PA never rises in Growth; no attribute
    #    rises in Decline), so retaining the fraction never lets a retry succeed and only accrues
    #    residue that would silently eat the next band's first days (the exact defect ERR-028-018
    #    fixed at the seed boundary, reopened here if either loop kept a nonzero remainder).
    while lifecycle.GrowthCursor >= POINT_COST:
        if not TrySpendOnePoint(ref record, ref lifecycle):     # respects the PA ceiling (F1)
            lifecycle.GrowthCursor = 0
            break
        lifecycle.GrowthCursor -= POINT_COST
    while lifecycle.GrowthCursor <= -POINT_COST:
        if not DrainOnePoint(ref record, ref lifecycle):        # AR pass 6 — now returns bool; a fully
                                                                  #   drained player (every attribute at
                                                                  #   ATTRIBUTE_MIN) is the loop's failure
                                                                  #   exit, mirroring the spend side
            lifecycle.GrowthCursor = 0
            break
        lifecycle.GrowthCursor += POINT_COST

    # 4. Recompute the derived CA summary (never a second accumulator, FR-PG-003).
    lifecycle.CurrentAbility = ComputeCA(in record.Attributes, record.Position)
```

**The spend/drain refusal exits were revised twice after this pseudocode was first written
(ERR-028-019, superseding the account below without deleting it).** The original pseudocode (v0.1-0.7)
showed the spend-side refusal as `break # at ceiling — leave the cursor (no thrash)` and the drain side
as an unconditional `DrainOnePoint(...)` call with no failure exit at all — both accurate against the
code at the time, and both since revised by execution-driven findings:
- **AR pass 5 (`GrowthCursor = 0` → `POINT_COST - 1`, superseded the same session):** the "leave the
  cursor" comment was replaced with a clamp to `POINT_COST - 1`, reasoned as preserving "the pending
  fraction… the next Growth day's accrual can still cross the threshold and try again."
- **AR pass 6 (`POINT_COST - 1` → `0`, current):** that reasoning was falsified by execution — within
  one band traversal, PA never rises in Growth and no attribute rises in Decline, so a refused spend is
  refused on *every remaining day of that band*; the retained fraction bought nothing and cost a whole
  point of Decline 364 days late (measured: a PA-bound player's first decline point landed on day 4743
  against an unbound player's day 4379). Clamping to `0` restores the no-residue invariant the
  ERR-028-018 traversal lock established. The same pass added `DrainOnePoint`'s failure exit — as a
  `void` no-op at the floor it had none, so an out-of-band cursor ground the loop upward one
  `POINT_COST` per iteration with no diagnostic (a cursor of `long.MinValue/2` is ~1.26e13 iterations,
  roughly 70 days of CPU) from a save file that round-tripped byte-exact; `DrainOnePoint` now returns
  `bool` and the loop breaks on `false`, mirroring the spend side exactly.

**Byte-exactness (FR-PG-006):** every field mutated is integer, and a save carries `GrowthCursor` +
`BirthWorldDay` + the `[1,20]` attributes. Restore recomputes the identical continuation because
`DailyBandPoints` and the spend/drain `while` bounds are pure integer functions of serialized state, and
age is a pure integer function of `(worldDay, BirthWorldDay)` with **no** discrete rollover event to
double-count — a save on any day restores to the identical continuation. **KD-8 identity (revised at
ERR-028-020):** with `curveEnabled` off **and `AGE_BAND_RAMP_HALF_WIDTH_YEARS = 0`**, `DailyBandPoints`
returns `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` (`±1` with `POINT_COST = DAYS_PER_YEAR`) so the
cursor crosses `POINT_COST` exactly once per year in the Growth/Decline bands and zero times in Stable
— the literal §4.3 `±1/year` step, byte-for-byte (§5 lock). The dial's off position is the identity;
the shipped half-width is non-zero, and §3.1.3 states what it changes.

### 3.1.1 Age derivation — one representation

Age is **derived** from the single serialized anchor `BirthWorldDay` (the authoritative field on the
lifecycle overlay): `AgeYears = (worldDay − BirthWorldDay) / DAYS_PER_YEAR` (integer division).

**SUPERSEDED (ERR-028-019): the "guarded at zero when `worldDay ≤ BirthWorldDay`" claim below, stated
at ERR-028-017, is no longer what the code does — annotated in place per this project's convention
rather than silently restated.** ERR-028-017 (v0.6) stated: *"guarded at zero when `worldDay ≤
BirthWorldDay`… `GrowthProjection.AdvanceDayForPlayer` computes `ageDays = worldDay − BirthWorldDay`
and returns `age = 0` whenever `ageDays ≤ 0`, rather than dividing."* That was accurate against the
code at the time. AR pass 6 (M2(a), ERR-028-019) found the guard's `ageDays ≤ 0` case conflated two
states that must be handled differently, and split it:
- **`ageDays == 0`** (a player born exactly on the world day being advanced to) is ORDINARY — `age = 0`,
  unchanged from before.
- **`ageDays < 0`** (`BirthWorldDay` is AHEAD of `worldDay` — the anchor claims the player is born
  after the very day he is being advanced to) is now a **FAIL LOUD** (`InvalidOperationException`),
  never `age = 0`. This state cannot arise from a real career (`SeedFrom` anchors at the seed day;
  `AdvanceDay` only ever moves `worldDay` forward), so the old else-branch's silent `age = 0` was
  reading corrupt state as ordinary data — and PERMANENTLY: `ProgressionSaveCodec`'s `DescribeOutOfRangeValues`
  (§3.5) has no world day to bound the anchor's upper end against — its own ceiling is `uint.MaxValue`,
  a property of the FORMAT, not of any one clock — so a save carrying a future-dated anchor loaded
  cleanly and every subsequent `AdvanceDay` silently re-derived age 0 forever (measured before the fix: a
  35-year-old read `age=0 band=Growth retirementFlag=false` after ten simulated years, and saved cleanly
  — worse than a permanently-unsavable career, because it is undetectable). The composition boundary
  (`PlayerCareerStates.RequireBirthWorldDayWithinClock`, called from both `SeasonLoop`'s per-player
  walk and `SeasonSaveManager`'s block-level walk — #30-owned, see #30 Appendix B.1) now refuses a
  `BirthWorldDay` ahead of the world clock BEFORE a day step can reach this guard; the guard here is the
  structural half, independent of any caller remembering to run that check.

Both the ordinary `ageDays == 0` case and the guarded `ageDays < 0` case apply at every call —
including the ordinary case, where `worldDay` sits at or shortly after `BirthWorldDay` and `ageDays` is
small but positive, which the plain division formula already handles correctly.

**The narrowing is SATURATING, at `MAX_DERIVABLE_AGE_YEARS` (AR pass 5, ERR-028-019).** Once `ageDays`
is known non-negative, `ageYears = ageDays / DAYS_PER_YEAR` is narrowed from `long` to `int` for
`record.Age`; above `MAX_DERIVABLE_AGE_YEARS` the narrowing clamps rather than truncates, so the
narrowing itself cannot produce a corrupt `int` even from an anchor whose `long` quotient would
overflow. `MAX_DERIVABLE_AGE_YEARS` (Appendix A) is a **REPRESENTABILITY** bound, not a
football-plausibility one, and the constant's own history records a correction worth repeating here: it
was first set to `1000` — a "reasonable age" sanity number — in the same commit that added it, and that
value broke the lock proving the `i64` `BirthWorldDay` field width `ERR-028-006` bought (which by
construction needs an anchor whose derived age does NOT fit in 32 bits). A representability bound and a
football-plausibility bound are different constants with different failure modes; conflating them once
already cost a same-session revert. The shipped value is `100,000,000` — see Appendix A for the full
derivation.

`BirthWorldDay` is pinned once at new-game from
#27's generation-time `PlayerRecord.Age`
(`BirthWorldDay = newGameDay − Age0 · DAYS_PER_YEAR`, where `Age0 = PlayerRecord.Age` at new-game).
There is **no** `AgeAnchorDay` field and **no** rollover `while`-loop — age is a pure function of the
world day, so nothing anchors or double-counts. #28 keeps the career-state `PlayerRecord.Age` field
**current** as a derived cache (the CA-cache pattern — recomputed each day, never a second source of
truth), so a consumer reading `record.Age` gets current age, not the frozen new-game seed. This is the
same one-representation discipline as the CA/PA model (§3.2): one authoritative anchor (`BirthWorldDay`),

**`BirthWorldDay` MUST be stored as a SIGNED quantity (ERR-028-006).** A new world starts at
`newGameDay = 0`, so the anchor formula above is negative for every generated player with `Age0 > 0` —
which is nearly the entire bootstrap roster. Clamping the anchor to 0 is **forbidden**: an
unrepresentable anchor and a correctly-signed one are indistinguishable at read time (both are valid
`uint` values), so the clamp does not fail loud — it silently reports every clamped player's age as
`worldDay / DAYS_PER_YEAR`, which reads as `0` for the whole league on the very first `AdvanceDay` and
never diverges from that error on its own. Measured against the clamped implementation: bootstrap ages
`26, 22, 30, 26, 28, 30 → 0, 0, 0, 0, 0, 0` after one simulated day, and a 100-player sample banded
`growth=100 stable=0 decline=0` — the Decline band unreachable and `RETIREMENT_AGE` (§3.4) never
firing, because no derived age can ever exceed one year. A player born before the epoch is the
**ordinary** representation for a non-zero generated age, not an edge case to be special-cased away.
one derived cache (`record.Age`).

### 3.1.2 The weighted spend order (`TrySpendOnePoint`)

`TrySpendOnePoint` picks the next attribute to raise by a **deterministic** rule: the position bias
table (`PlayerDatabaseConstants.PositionAttributeBias`, read-only) weights which attributes a player
develops, and ties break by ascending `AttrIdx`. An attribute at `ATTRIBUTE_MAX`, or whose raise would
push the derived CA past `PotentialAbility`, is skipped (F1). This preserves per-attribute individuality
(two players with the same CA but different positions develop different attributes) without a draw.

### 3.1.3 The age-continuous accrual rate (ERR-028-020)

**The defect this replaces.** `DailyPoints` took a three-way `ClassifyAgeBand(ageYears)` and returned
one of three constants, so "is this player still developing?" — a continuous football judgment — was
answered by a hard step at an exact integer age: a player developed at the full rate on the last day of
his 23rd year and at exactly zero on the first day of his 24th, and the same discontinuity sat at
`DECLINE_AGE`. §1.3 promises "per-attribute CA/PA growth-decline curves keyed to age" and no
age-continuous curve existed anywhere in this spec's text. Recorded as pattern (b) **and** (d) by
`docs/tracking/football-judgment-proxy-review.md` §3; fixed under that document's §6 doctrine **P1**
(continuous, never a cliff) and **P5** (pivot on today's baseline).

**The shape.** Each edge becomes a linear ramp of half-width `AGE_BAND_RAMP_HALF_WIDTH_YEARS`
**centred on the old step**, in DAYS:

```
g = GROWTH_AGE  · DAYS_PER_YEAR                    # the retired growth edge, in days
e = (DECLINE_AGE + 1) · DAYS_PER_YEAR              # the retired decline edge (the predicate was ageYears > DECLINE_AGE)
h = AGE_BAND_RAMP_HALF_WIDTH_YEARS · DAYS_PER_YEAR

growthRate(d)   = 1                     for d ≤ g − h
                = (g + h − d) / (2h)    for g − h < d < g + h
                = 0                     for d ≥ g + h
declineRate(d)  = 0                     for d ≤ e − h
                = (d − (e − h)) / (2h)  for e − h < d < e + h
                = 1                     for d ≥ e + h
```

**Why an INTEGRAL and not a rate.** `GrowthCursor` is integer fixed-point at a scale where one day of
full growth is one unit (`POINT_COST = DAYS_PER_YEAR`, KD-8), so a per-day rate has nothing between 0
and 1 to return. The accrual is therefore defined as the difference of an exact integer **cumulative**:

```
AccruedBandPoints(n)  =  GROWTH_DAILY_POINTS · G(n)  +  DECLINE_DAILY_POINTS · D(n)      # n = days LIVED
    G(n) = n                                    for n ≤ g − h
         = (g − h) + u − u² / (4h),  u = n − (g − h)     for g − h < n < g + h
         = g                                    for n ≥ g + h
    D(n) = 0                                    for n ≤ e − h
         = v² / (4h),                v = n − (e − h)     for e − h < n < e + h
         = n − e                                for n ≥ e + h

DailyBandPoints(ageDays) = AccruedBandPoints(ageDays + 1) − AccruedBandPoints(ageDays)
```

All integer, all floor division. The per-day step stays in `{0, ±1}` — so **the persisted cursor's
scale is unchanged and `PROGRESSION_SAVE_FORMAT_VERSION` does not move** — while its DENSITY follows
the continuous rate exactly, with no rounding drift over any span. Both branches are written in the
shifted variables `u`/`v`, bounded by `2h`, so the squared term cannot overflow for an anchor near
`MAX_DERIVABLE_AGE_YEARS`.

**The representability ceiling is applied to the AGE, not to the cumulative**, and the distinction is
load-bearing rather than stylistic. §3.1.1's age narrowing saturates at `MAX_DERIVABLE_AGE_YEARS`, and
under the retired band step that pinned age classified as `Decline`, so a player beyond the ceiling
kept draining at the full rate. Saturating `AccruedBandPoints` would clamp BOTH terms of
`DailyBandPoints`' difference to the same value, and such a player would silently stop declining
altogether — a behaviour change nothing inside the football range could surface. `DailyBandPoints`
therefore clamps `ageDays` to one day inside the ceiling, making his daily step the step AT the
ceiling, which is the full decline rate exactly as before (§5 lock).

**The P5 pivot is exact, not fitted.** Because each ramp is centred on its old edge, `G(∞) = g` and
`D` past `e + h` is `n − e` — the **same totals the step model produced, for every half-width including
0**. The ramp redistributes accrual across an edge without creating or destroying any, so no
growth-rate recalibration is owed: a traversal that starts outside a ramp and finishes past it still
gains exactly one attribute-point per year of the band and still leaves **no residue**, which is
ERR-028-018's invariant preserved by construction (§5 lock). A player *seeded inside* a ramp does end
his growth holding a fractional cursor — his own remaining integral is not a whole multiple of
`POINT_COST` — and that fraction is **carried, not lost**: it correctly offsets his first days of
decline. That is arithmetically distinct from ERR-028-018's pathology, which was a whole point *short*
of the integral on every traversal, and §5 locks the distinction so a future reader does not "fix" it
by re-rounding the accrual and putting the cliff back.

**Two catalogue invariants, enforced fail-loud at the computing site** (the `MedicalStep.DrawOccurrence`
guard posture): `AGE_BAND_RAMP_HALF_WIDTH_YEARS ≥ 0`, and `2 · half-width ≤ (DECLINE_AGE + 1) −
GROWTH_AGE` so the two ramps stay disjoint — a day inside both would accrue growth and decline at once,
which the arithmetic represents and no football reading does. *(Corrected —
config-unbound-premise-false-28, football-judgment proxy review batch-1: this paragraph justified the
computing-site placement with "the `[GT]` is a config key and the catalogue lock runs config-unbound,
so it sees only the fallback" — the same rationale ERR-041-003 states truly of an already-migrated
catalogue, copied here without checking it applied. `PlayerProgressionConstants.cs` has ZERO
`Config.GetX` calls today (confirmed by `src/CLAUDE.md`'s own 2026-08-10 tree-wide measurement, which
lists `player-progression` among the ten catalogues with none), so the computing-site guard is a
forward-looking placement for the Stage-1 config loader, not a workaround for a catalogue lock a
config-unbound gate defeats today — a catalogue-level lock IS available now and exists in
`PlayerProgressionConstantsTests`.)*

**`ClassifyAgeBand` survives as a DESCRIPTION, not as a second authority.** It returns the SIGN of the
year's own net accrual (positive ⇒ Growth, negative ⇒ Decline, zero ⇒ Stable), read from
`AccruedBandPoints`. Re-deriving a band from `GROWTH_AGE`/`DECLINE_AGE` would be a second surface
answering a question the curve already answers — the parallel-surface class this project has filed
three times (`LineupSelector.CanSelect` being the nearest). A whole year rather than a single day
because inside a ramp the per-day accrual is quantised and adjacent days differ.

**Not fixed here, recorded:** the review's finding also names "no per-player variance" in the growth
rate. That is the Stage-3 `curveEnabled` tier's — it needs the `(PA − CA)` modulation §3.2 already
reserves, and adding a per-player term here would either duplicate that or introduce #28's first draw
site (§3.3's stream is deliberately unregistered until the season boundary lands). The cliff is what
this ERR removes.

## 3.2 The CA/PA model

`PotentialAbility` (PA) is generated once at regen/new-game (a wide-integer ceiling from the
`progression.regen` stream, §3.3) and never rises. `CurrentAbility` (CA) is `ComputeCA(attributes,
position)` — a position-weighted mean of the `[1,20]` attributes scaled to `[0, ABILITY_MAX]`,
**recomputed** whenever attributes change (FR-PG-003). CA→PA gap drives growth magnitude in the deep
tier (`DailyPoints` scales with `(PA − CA)` when `curveEnabled`); in the Stage-2 shipped tier the gap
is unused and `DailyPoints` is `DailyBandPoints` — §3.1.3's age-continuous ramp, not a flat step (see
§3.1.3; `curveEnabled` off reproduces the retired flat step only at `AGE_BAND_RAMP_HALF_WIDTH_YEARS =
0`, KD-8). *(Corrected — spec-32-stale-flat-band-step, football-judgment proxy review batch-1: this
paragraph still described `DailyPoints` as "the flat §4.3 band step" and the minimal tier as flat,
present tense, after ERR-028-020 made both stale — `DailyBandPoints` is the accrual authority now, the
surviving private wrapper only delegates, and the Stage-2 tier is ramped, not flat. The `(PA − CA)`
deep-tier half is unaffected and kept verbatim.)* CA is a cache in the serialized block, but the
attributes are authoritative — a restore recomputes CA from the attributes, so a corrupt CA cache can
never diverge (it is overwritten on the first `AdvanceDay`, and §5 locks recompute-equals-stored).

**New-game PA is authored data, owned by #47.** #28 does not draw a new-game player's `PotentialAbility`
— per the owner's decision recorded at ERR-028-003, PA for the ~500 bootstrapped players of a new game
is **authored data owned by New-Game Setup & Database Editor #47**, which is APPROVED but has no `src/`
assembly yet. Until #47 exists to supply it, #28 exposes a seeding seam (`ProgressionEngine.SeedFrom`)
that fills the gap with a deterministic `[GT]` placeholder: `PA = clamp(CA + NEW_GAME_PA_HEADROOM,
PA_MIN, ABILITY_MAX)`. This is **deliberately not a draw**: a draw here would be #28's first draw site
at all, and would force the `player-progression.regen` stream (FR-PG-020) to register for a value #47
is going to overwrite the moment its assembly lands — a stream registered against a number nobody
reads once #47 ships is exactly the phantom-surface class FR-LW-031 forbids.

**Recorded limitation, not fixed here:** over §3.1.3's accrual curve, a whole youth career (roughly
eight growth years, one attribute-point's worth of accrual per year — the P5 pivot means this total is
exact at every ramp half-width, including the shipped one, not merely at the retired flat step) raises
CA by only ~421 of `ABILITY_MAX` = 10,000 (8 years × ~52.6 per point). The PA ceiling therefore binds
only when the authored CA→PA gap is under about 420 — no realistic authored wonderkid gap is that
small. **PA-as-ceiling is decorative regardless of PA's source** (authored or drawn); the cause is the
growth RATE, not where PA comes from. Closing it is the Stage-3 `curveEnabled` tier's job, and KD-W1
forbids retuning the growth rate in a landing that has not wired the deep tier. *(Corrected —
round-2 finding spec-32-flat-band-step-sweep-stopped-two-paragraphs-short: this paragraph still said
"at the §4.3 flat band step" and "the flat-band rate", present tense, two paragraphs below the v0.11
correction that swept the same stale phrasing elsewhere in this section — ERR-028-020 made both stale.
The ~421 figure itself is UNCHANGED by the correction: the ramp is centred on the old step edge, so its
whole-life integral equals the step's for every half-width (§3.1.3's P5 paragraph), which is exactly
why this number needed no re-derivation.)*

## 3.3 Regen generation (KD-3)

```
GenerateRegen(rng, streamIndex, clubId, ref nextPlayerId, referenceRosterWorld):
    reserveErr = rng.Reserve(streamIndex, PROGRESSION_REGEN_FIELDS)     # fixed budget (#27 pattern)
    ...draw name/nation/position/attributes/PA from the reservation, mirroring RosterGenerator...
    rng.CloseReservation(streamIndex)
    playerId = nextPlayerId; nextPlayerId += 1                          # FRESH id, monotonic (FR-PG-011)
    return new PlayerRecord { PlayerId = playerId, ... } + PlayerLifecycle { PotentialAbility = drawnPA, ... }
```

The reservation is a **fixed budget** (`PROGRESSION_REGEN_FIELDS`) so a regen consumes a constant
number of draws regardless of outcome — the #27 `FIELDS_PER_PLAYER` discipline (the fixed-budget
reservation is what keeps the stream position deterministic across fail-loud paths). `entityId =
clubId`; successive regens for the same club draw at successive `ActionOrdinal`s (the stream advances),
so a club's newgen sequence is reproducible. Club/nation come from `referenceRosterWorld` (read-only,
#27). A regen's PA is drawn (a younger player with a high PA is a "wonderkid"); its `[1,20]` attributes
are generated below PA so the player has room to grow.

**A regen's `GrowthCursor` MUST credit its own construction day's band step, exactly like a seeded
player's (AR pass 7, ERR-028-018/ERR-028-019).** `SeedLifecycle` (§3.1) and `GenerateRegen` are the two
sites that construct a `PlayerLifecycle` from scratch, and both anchor `LastAdvancedWorldDay` at their
own construction day — which, per §3.1's "already accounted for" rule, means that day's own band step
must be CREDITED to `GrowthCursor`, not left at `0`. ERR-028-018 fixed this at `SeedLifecycle` only;
`GenerateRegen` was not visited, and a regen anchored at `worldDay` with `GrowthCursor = 0` accrued
`N · DAYS_PER_YEAR − 1` days over its remaining *N*-year Growth band, identically to the pre-ERR-028-018
defect — measured: a regen gained +5 points over its remaining Growth band where an identically
generated seeded player gained +6, the same 364-day residue surviving into Decline. `GenerateRegen` now
sets `GrowthCursor = ConstructionDayCredit(age)` at construction — the construction day's own step,
**taken from §3.1.3's continuous curve** (`DailyBandPoints(age · DAYS_PER_YEAR)`), which is the same
authority the daily step and `SeedLifecycle` read.

**The credit has ONE implementation, and both construction sites MUST call it.** `ConstructionDayCredit`
belongs to the same owner as the curve it reads (`AbilityModel`, §3.1.3); neither `SeedFrom` nor
`GenerateRegen` may compute it locally. This is normative because the alternative has already cost this
spec two landings: the rule was written out twice — once inlined in `SeedLifecycle`, once as a
`RegenGenerator.BandStepFor` whose own documentation described itself as the shared owner of a rule it
did not own — so ERR-028-018 credited one site and left the other at `0` (caught a day later), and
ERR-028-020 then had to visit both again to move each off the retired three-way step. A rule two sites
owe is a rule exactly one place may state.

*(**Amended August 24, 2026** — round-2 adversarial finding
`construction-day-credit-implemented-twice`. This paragraph named `BandStepFor(age)`, the
`RegenGenerator`-local method that has since been DELETED; the call is
`AbilityModel.ConstructionDayCredit(age)`. The value is unchanged — `BandStepFor`'s body was
character-for-character the expression `SeedLifecycle` also carried, verified by probe over ages 0..200
and the `int` domain's edges before the collapse — so what moved is ownership, not arithmetic.)*

*(**Amended at ERR-028-020.** This paragraph previously described `BandStepFor` as classifying the
drawn age into a band and returning one of three constants, and defended that as "classified rather
than hard-coded, so it does not silently become wrong if either age constant moves". The reasoning was
right and the mechanism no longer is: since §3.1.3 the step is a continuous function of age, so a
three-way classification here would have been a **second answer to a question with one authority** —
the parallel-surface class — and would have gone wrong on exactly the move it was defending against.
The two forms agree today, because a regen's drawn age is always in
`[REGEN_AGE_MIN, REGEN_AGE_MAX]`, entirely below the growth ramp; they diverge the first time either
that band or `AGE_BAND_RAMP_HALF_WIDTH_YEARS` moves, and the symptom would be one attribute point per
band traversal — ERR-028-018's shape, at the sibling construction site.)*

## 3.4 Retirement + the season boundary (KD-5 / KD-6)

**Daily (in `AdvanceDay`):** `if ageDays >= RetirementAgeDays(record) and not RetirementFlag:
RetirementFlag = true; RetirementDay = worldDay`. Deterministic — no draw (FR-PG-013). The player stays
in the roster and stays selectable (FR-PG-014).

**The retirement day is PER-PLAYER and continuous (ERR-028-021), superseding the single
`AgeYears >= RETIREMENT_AGE` comparison this section carried until August 22, 2026.** That comparison
was a hard integer-age threshold with no attribute or position input at all: a goalkeeper retired on
the identical clock as a forward, despite goalkeepers demonstrably playing markedly longer careers, and
one day of the calendar was the whole difference between a career continuing and ending — for the whole
league at once. Recorded as pattern (b)/(c) by `docs/tracking/football-judgment-proxy-review.md` §3;
fixed under **P1** (continuous, never a cliff), **P3** (the attribute-ownership ledger) and **P5**
(pivot on today's baseline).

```
RetirementAgeDays(record) -> days:
    if RETIREMENT_GAME_READING_SPAN_YEARS < 0 or RETIREMENT_GOALKEEPER_BONUS_YEARS < 0:
        FAIL LOUD                                                          # catalogue/config integrity
    days = RETIREMENT_AGE · DAYS_PER_YEAR
    if record.Position == Goalkeeper:
        days += RETIREMENT_GOALKEEPER_BONUS_YEARS · DAYS_PER_YEAR
    sum   = Anticipation + Positioning + Composure                         # NOT floored to a mean
    days += ((2·sum − 3·(ATTRIBUTE_MIN + ATTRIBUTE_MAX)) · RETIREMENT_GAME_READING_SPAN_YEARS
             · DAYS_PER_YEAR) / (6 · (ATTRIBUTE_MAX − ATTRIBUTE_MIN))      # full-range, anti-symmetric
    if days <= 0: FAIL LOUD                                                # catalogue/config integrity
    return days
```

*(**Added — guards-unexercised, football-judgment proxy review batch-1.** This pseudocode previously
showed only the `days <= 0` guard, omitting the leading non-negativity check on the two career-length
dials that Appendix A mandates for both (`RETIREMENT_GOALKEEPER_BONUS_YEARS` and
`RETIREMENT_GAME_READING_SPAN_YEARS` "MUST be non-negative") and the code has always carried
(`AbilityModel.RetirementAgeDays`). Test ids for all four §3.1.3/§3.4 catalogue/config integrity
guards — the ramp half-width's non-negativity and disjointness guards, this ONE combined dial
non-negativity guard, and the `days <= 0` guard — are allocated at §5.3.1/§5.6 below.)*

*(**Corrected — round-2 finding four-guards-enumerated-as-five-and-mis-named.** The paragraph above
previously read "these TWO dial guards" where the code above is ONE `if`
(`readingSpanYears < 0 or goalkeeperBonusYears < 0`) — describing it as two guards made the paragraph
claim "four" and then enumerate five items. Two test CASES are needed to prove the `or` is checked on
both operands, which is not the same thing as two separate guards; the fix corrects the enumeration to
match the code without changing the "four" count, which was already right.)*

**The reading trio's SUM is carried undivided into the numerator (ERR-028-022), superseding the
`mean = (Anticipation + Positioning + Composure) / 3` step this pseudocode carried between
ERR-028-021's landing and August 22, 2026.** Flooring the sum to a mean before the anti-symmetric map
destroys the anti-symmetry the map exists to have: `floor(sum / 3)` is not symmetric about the
attribute midpoint — truncation always bites downward — so the two halves of the population no longer
cancel anywhere off the `Anticipation == Positioning == Composure` diagonal. Measured through the built
assembly: **−204,621 days over the uniform `[1,20]³` product, i.e. −25.58 days per player**. The claim
below said the league's retirement *rate* was unchanged; in fact the whole league retired about two
months early. Carrying the sum is exactly anti-symmetric (the product sums to **0**) and reproduces
every value on the old diagonal bit-for-bit, since for `sum == 3·mean` the numerator and denominator
are both exactly 3× the retired form's and integer division truncates toward zero.

**The comparison is in DAYS, against a day-resolution threshold**, so one attribute point moves a
player's retirement by tens of days rather than by a whole year — the full-range ramp form
`ERR-008-019` was owner-revised to, with no plateau anywhere across `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]`.

**P3: why the reading trio and NOT robustness.** The obvious input — a durable player lasts longer —
would be the **third** read of `Strength`/`Stamina`/`Balance`: #29's `ComputeInjuryRisk` and #41's
`RobustnessMitigation` already price that trio twice over, which `ERR-041-003` records as a
contract-level double count. Career length is therefore owned here by `Anticipation`/`Positioning`/
`Composure`, which no other subsystem consumes: the player who ages well is the one whose game rests on
reading play rather than on the pace he is losing. This is a ledger entry, not a preference — a future
spec wanting a career-length term must find its stage here first.

**P5: exact at both scales.** At a zero goalkeeper bonus and a zero reading span,
`ageDays >= RETIREMENT_AGE · DAYS_PER_YEAR` is *identically* the retired
`AgeYears >= RETIREMENT_AGE` (age being that quotient, floored) — the dial's off position reproduces
the old rule rather than approximating it. At the shipped values the offset is anti-symmetric about the
attribute midpoint and integer division truncates toward zero symmetrically, so the offsets over a
uniform `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` population **sum to exactly 0**: the league's retirement RATE
is unchanged and only *which* players retire *when* moves. (§5 locks all three properties.)

*(**Corrected at ERR-028-022, August 22, 2026 — the paragraph above was published against an
implementation for which it was false, and is annotated rather than restated.** As written it is true
of the formula THIS SECTION NOW STATES; it was not true of the floored-mean form the section carried
from ERR-028-021's landing until this date, whose offsets summed to **−204,621 days** over the uniform
`[1,20]³` product. Two things about the correction are worth carrying forward. First, **"sum to exactly
0 over a uniform population" is a claim about the whole attribute PRODUCT, and it must be checked
there** — the §5 lock swept only the `Ant == Pos == Comp` diagonal, which is precisely the line on
which the defect vanished (there the division by 3 is exact), so the lock passed against the broken
model and a mutation to a differently-wrong rounding also passed. Second, **the population that is
uniform on `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` is a modelling idealisation, not this game's league.** #27's
`RosterGenerator` draws each attribute independently on `AttributeBaseMean ± AttributeSpread` = `[6,14]`,
which centres on **10** while the offset's neutral point is the midpoint **10.5**; over that population
the corrected offset averages **≈ −38 days per player**, so the generated league does retire fractionally
early — about five weeks, not two months, and by a half-point of attribute centring rather than by a
broken map. That residual is stated rather than papered over: closing it would mean either pivoting the
offset on the generator's mean (which re-pivots #28 the day #47's authored database replaces those
bounds — the same coupling `ERR-041-020` refused for `AGE_RISK_PIVOT_YEARS`) or an odd-width attribute
range, and neither is this ERR's to decide.)*

**This is a re-evaluated function, not a stored property, and the function's own inputs are what the
daily step mutates (round-2 adversarial finding
retirement-day-derived-from-attributes-the-same-step-mutates).** `RetirementAgeDays(record)` is called
once per `AdvanceDay` (see the placement paragraph below), against the SAME record §3.1's spend/drain
step (§3.1.2) has just mutated earlier in that same call — `TrySpendOnePoint` raises
Anticipation/Positioning/Composure during Growth, `DrainOnePoint` lowers them during Decline, and this
section's own offset reads exactly that trio. So a player's retirement day is not fixed at birth; it
moves under him as those three attributes change, and re-evaluating tomorrow can return a different day
than today's read did. **Bounded today, but by an accident of order, not by a stated rule:** within one
band every spend/drain call moves the trio in the SAME direction (up in Growth, down in Decline, never
mixed), and `RetirementFlag` is sticky once set (no un-flagging), so the day observed across a run of
same-direction days is monotone and no oscillation is reachable. That bound stops holding the day
something can move Anticipation/Positioning/Composure independently of the band-driven spend/drain
order — the Stage-3 `curveEnabled` tier or #47's authored data touching them directly — and nothing in
this section currently guards against it; §5.6's T-PG-RET-009 monotonicity lock covers only what holds
today. See
`AbilityModel.RetirementAgeDays`'s own doc for the code-side statement of this invariant.

**Still deterministic, still no draw.** A draw here would be #28's first draw site and would force the
`player-progression.regen` stream to register for a value the season boundary has not yet needed
(§3.3, FR-PG-020) — the phantom-surface class FR-LW-031 forbids. Per-player *variation* is delivered by
per-player *attributes*, which is what the finding asked for.

**Evaluated once per `AdvanceDay` call, on the post-replay derived age — not once per lived day
(ERR-028-017, correcting a placement §3.1's `AdvanceDayForPlayer` pseudocode never showed).** §3.1's
`AdvanceDay` gap-replay loop calls `GrowthProjection.AdvanceDayForPlayer` once per day in the gap, but
the retirement check above lives in `ProgressionEngine.AdvancePlayerTo`, which wraps the WHOLE replay
and evaluates retirement exactly once, after the loop, against the age derived at `worldDay` (the
call's target day) — never against any intermediate day the replay passed through. §3.1 itself has no
retirement step at all; this section (§3.4) is the only normative placement, and until now it did not
say which of "per lived day" or "per call" the evaluation runs at.
**Known limitation, stated rather than fixed:** `RetirementDay` is stamped `worldDay` — the call's
target day — not the earlier day within the gap on which `AgeYears` first reached `RETIREMENT_AGE`. For
any gap of more than one day where the threshold is crossed mid-gap, these two days differ, and the
earlier crossing day is not recoverable from the stored state. This is a property of a system whose only
per-day cursor is the growth accumulator (§3.1) and whose age is DERIVED rather than stepped — pinning
the true crossing day would need either a per-day retirement check inside the replay loop (changing
`AdvanceDayForPlayer`'s contract, which §5's T-PG-DET-002 keystone below directly exercises) or a second
stored anchor, and neither is justified by what `RetirementDay` is actually used for today (FR-PG-014's
selectability gate reads only the boolean flag). **See §5's T-PG-DET-002**, which mandates far-future gap
tests: a reader running that test against a player who crosses `RETIREMENT_AGE` mid-gap will observe
this limitation directly, so the cross-reference is here rather than left implicit.

**At the season boundary (`RunSeasonBoundary`, invoked by #30 KD-6):**
```
RunSeasonBoundary(...):
    if boundaryAlreadyApplied(seasonNumber): return          # idempotent (F6 / FR-PG-024)
    retirees = [ pid for pid in roster if lifecycle[pid].RetirementFlag ]
    emit RetirementResult(retirees)
    for pid in retirees:                                      # deterministic order: ascending PlayerId
        clubId = ClubOf(pid)
        regen  = GenerateRegen(rng, streamIndex, clubId, ref nextPlayerId, referenceRosterWorld)
        remove lifecycle[pid]; remove record[pid]            # 1:1 replacement (FR-PG-019)
        add regen; emit into RegenResult
    markBoundaryApplied(seasonNumber)
```
It **does not re-bank growth** (banked daily, KD-1) — its job is applying the deferred roster
mutations. `#30`/`#27` apply the `Squad` removal+insert from `RetirementResult`/`RegenResult`
(FR-PG-012/015). Restartable: a save taken mid-boundary carries the boundary marker + `nextPlayerId`
+ the partially-mutated block, so a restore→re-run resumes deterministically without double-applying.

## 3.5 The save codec (KD-4)

**Layout correction (ERR-028-004):** an earlier revision of this section specified the block as
`PROGRESSION_SAVE_FORMAT_VERSION -> DOMAIN_TAG_PLAYER_PROGRESSION -> NextPlayerId -> ...` —
version-first, with the RNG hash-domain tag standing in as the block's identifier. That is the exact
defect ERR-029-005 / ERR-041-009 filed as a MUST against in the sibling #29/#41 blocks: every sub-blob
format in this save stack sits at version **1**, so a version gate alone cannot tell one format from
another — it only separates generations of the SAME format — and a transposed `byte[]` at the frame
would decode a sibling's bytes against this layout cleanly and silently. A domain tag is doubly wrong
for the job besides: it is a hash-domain separator with an unrelated purpose, and ERR-029-005 already
established that the magic is deliberately *not* an RNG tag. The shipped layout instead leads with a
magic, checked before the version, and is otherwise unchanged from the original field order:

```
u32 PROGRESSION_SAVE_MAGIC ("PROG")   # BEFORE the version
u32 PROGRESSION_SAVE_FORMAT_VERSION
i32 nextPlayerId
u32 clubCount
per club, ascending ClubId:
    i32 clubId
    u32 playerCount
    per player, ascending PlayerId:
        i32 playerId, str firstName, str lastName, i32 age, u8 position,
        i32 attribute[0..30], i32 weakFootRating,
        i32 potentialAbility, i32 currentAbility, i64 growthCursor,
        i64 birthWorldDay, u8 retirementFlag, u32 retirementDay, u32 lastAdvancedWorldDay
```

**`str` is `u32` length-prefix + ASCII body (ERR-028-017), pinned explicitly because F3 makes the first
written layout the format permanently.** This section left the string encoding unstated — no width, no
character set — for a field F3 forbids ever changing. The shipped encoding, per Deterministic
Simulation #16 §3.2.4.1 (`CanonicalSerializer.WriteString`/`ReadString`), is a `u32` byte-length prefix
followed by that many ASCII bytes; `ProgressionSaveCodec.Encode` refuses a non-ASCII name at the write
site rather than mangling it silently (the never-write-what-Decode-refuses rule), so a name outside
ASCII is a save-time failure, not a round-trip corruption.

**The fail-loud enumeration below was incomplete when ERR-028-017 wrote it — and is incomplete AGAIN
now, this time by count rather than by omission-of-a-whole-class (ERR-028-019 SUPERSEDES the four-gate
enumeration ERR-028-017 recorded, per this project's convention of annotating a superseded claim in
place rather than silently restating it).** ERR-028-017 (v0.6, August 10, 2026) stated that `ReadPlayer`
range-gated exactly FOUR value fields the framing-level list omitted entirely: attributes, weak-foot,
age, and `potentialAbility`. That was accurate against the code at the time. AR passes 5 through 8
(August 10-11, 2026) added FOUR MORE, all through the same shared owner
(`ProgressionSaveCodec.DescribeOutOfRangeValues`), for **EIGHT** value gates total:

1. Each `[1,20]` attribute against `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]`.
2. `weakFootRating` against `[WEAK_FOOT_MIN, WEAK_FOOT_MAX]` (#27's bounds).
3. `age` against `≥ 0` (the field is a derived cache — see §3.1.1's SIGNED `birthWorldDay` for the
   authoritative anchor, which MAY legitimately be negative; the cache itself may not).
4. `potentialAbility` against `[PA_MIN, ABILITY_MAX]` (the F1 growth ceiling — a corrupt value below the
   floor would silently freeze a player's growth forever, and one above the ceiling would silently
   unbound it).
5. **`growthCursor` against `(-POINT_COST, POINT_COST)` (AR pass 6, ERR-028-019) — no `[GT]` judgement
   needed, since the band is DERIVABLE.** Both spend/drain loops (§3.1) leave `|GrowthCursor| ≤
   POINT_COST - 1` after any completed step, and construction (`SeedLifecycle`/`GenerateRegen`, §3.1/§3.3)
   writes one DAY's band step — `GROWTH_DAILY_POINTS` (`+1`) or `DECLINE_DAILY_POINTS` (`-1`), never a
   whole POINT — so `|GrowthCursor| < POINT_COST` is exactly the serialized invariant. The distinction
   matters and is not pedantry: `POINT_COST == DAYS_PER_YEAR == 365` (KD-8), so an implementer reading
   "one whole point" as the construction credit would seed `|GrowthCursor| = POINT_COST`, which this very
   gate refuses — the strict inequality is load-bearing, and §3.1's accrual step is what construction
   mirrors, not §3.1's spend threshold. Out of range, this field does not merely corrupt data — it WEDGES the day step: before this
   gate, an out-of-band cursor ground the drain loop upward one `POINT_COST` per iteration with no
   failure exit, from a save file that round-tripped byte-exact.
6. **`birthWorldDay` against `[-(MAX_DERIVABLE_AGE_YEARS · DAYS_PER_YEAR), uint.MaxValue]` (AR pass 5,
   ERR-028-019) — the LOWER half only; see §3.1.1 for the UPPER half, which this codec cannot check
   (it has no world clock).** `BirthWorldDay` is the AUTHORITATIVE age anchor (§3.1.1) and was, until
   this gate, the one lifecycle field with no range gate at all — an anchor far below the floor narrowed
   the derived age to `int.MinValue` — at the time this gate was added, `ClassifyAgeBand` read that as
   `Growth` under the retired age-only band step (so the player grew forever and `RETIREMENT_AGE` could
   never fire — ERR-028-006's failure mode through a different door) — and which this very gate then
   refused as a negative age, making a career that loaded, advanced and projected fine PERMANENTLY
   unsavable. *(Corrected — classifyageband-growth-claim-stale, football-judgment proxy review batch-1:
   `ClassifyAgeBand` no longer reads `int.MinValue` as `Growth` — since ERR-028-020 it reads the
   continuous accrual curve, and both `AccruedBandPoints` cumulatives are 0 at a hugely negative age, so
   it now returns `Stable`. The int-narrowing this gate guards against is unchanged; only that one
   downstream symptom is history rather than present behaviour.)*
7. **`currentAbility` MUST equal `ComputeCA(attributes, position)` exactly (AR pass 8 L-4, ERR-028-019).**
   Guarded on the position ordinal's own validity first — `ComputeCA` indexes
   `PlayerDatabaseConstants.PositionAttributeBias` by the raw ordinal with no bounds check, so an
   undefined position would throw `IndexOutOfRangeException` ahead of the boundary's own, more specific
   `Enum.IsDefined` refusal, which must be the one that fires. `CurrentAbility` is a DERIVED cache
   (FR-PG-003) that self-heals on the *next* day step — but the spend/drain loop above reads it BEFORE
   that recompute, and (since AR pass 6) a refused spend at the ceiling now DISCARDS the fraction rather
   than retaining it, so a stale restored value costs a whole `[1,20]` point PERMANENTLY at the next
   threshold crossing, not merely a one-step delay. **This is recorded as an OPEN decision below**,
   because the equality is checked against `PositionAttributeBias`, which is `[GT]` and carries a
   standing `TODO: replace with config loader (Stage 1)`.
8. **`retirementDay` is gated against `retirementFlag` (AR pass 8 L-4, ERR-028-019): unset MUST carry
   `0`; set MUST carry a day at or before `lastAdvancedWorldDay`.** `RetirementFlag` is sticky — set
   once, at the world day it fires, and never cleared (§3.4) — so `RetirementDay`'s own legal range is a
   function of the flag, not an independent field; it cannot legitimately fire on a day this player was
   never advanced to.

**All eight throw `InvalidOperationException` from `Decode`, matching the framing gates' type (see the
corrected F3 row, §2.3) — but this is now stated as a general rule, not merely true of `Decode`'s value
gates specifically (ERR-028-019, AR pass 8 M-1).** Every shared boundary rule in this section — the
eight value gates above, plus the FR-PG-011 id-cursor rule and the M3 club-size rule below — is now
split into a `Describe*` half (returns the violation text, throws nothing) and a `Require*` wrapper
around it. `Encode` and `ProgressionEngine.FromBlocks` (both bad-ARGUMENT boundaries) call the
`Require*` wrapper and get `ArgumentException`; `Decode` (a corrupt-FILE boundary) calls the `Describe*`
half directly and throws `InvalidOperationException` of its own. Before AR pass 8, the id-cursor and
club-size rules' `Decode` call sites threw the SAME `ArgumentException` `Encode` throws, naming an
argument (`clubs`/`nextPlayerId`) `Decode`'s own signature does not have — contradicting `Decode`'s own
`<exception>` doc and the ERR-029-004/ERR-041-008 convention already binding on this section's own value
gates. Three decode-side test locks had been asserting the observed (wrong) `ArgumentException` rather
than the contract; they are retyped, not merely re-passed.

**`PA_MIN` and `ABILITY_MAX` are `[GT]` (Appendix A)** — this makes the `potentialAbility`
gate a save-acceptance predicate keyed on tunable config, the exact posture #30 Appendix B.1 reasons
AGAINST for its own appearance sub-blob ("gating it would turn a retune into data loss"). **This is
recorded as an OPEN decision, not resolved here**: whether the range gate should instead read from a
`[FIXED]`/`[DERIVED]` bound, or whether the tension with #30's stated posture is acceptable because this
block is the roster itself (KD-4) rather than an overlay, is an owner call for a future pass. Nothing in
this correction changes any tag, retags any constant, or migrates the gate.

**A second, sharper instance of the same hazard class, recorded here and NOT resolved (ERR-028-019,
carrying forward AR pass 9's finding).** Gate 7 above — `currentAbility == ComputeCA(attributes,
position)` — refuses to load any save where the stored value differs from what recomputing produces.
`ComputeCA` weights attributes through `PlayerDatabaseConstants.PositionAttributeBias`, tagged `[GT]`
with a standing `TODO: replace with config loader (Stage 1)` (`src/CLAUDE.md`, "Migration status").
**Consequence: tuning one cell of that bias table makes every previously-written save refuse to load,
permanently, with no migration path** — F3 (#30 Appendix B.1) forbids cross-version migration, and a
`[GT]` retune is not a format-version bump, so there is no mechanism to reconcile an old file's stored
`CurrentAbility` against a new bias table's recomputation. **Not triggerable today**: `PositionAttributeBias`
is presently a compile-time constant, so a stored value always equals its recomputation at write time —
the defect bites at the FIRST tune of that table, which is a planned Stage-1 activity, not a hypothetical
one. This is the same hazard class as the `PA_MIN`/`ABILITY_MAX` OPEN decision immediately above, one
gate sharper: `PA_MIN`/`ABILITY_MAX` bound a value against config-set floor/ceiling constants, while this
gate makes save VALIDITY itself a function of a `[GT]` formula's current coefficients. **This is deliberately
NOT resolved here** — no tag is changed, no code is changed. The alternative worth recording for a future
owner pass: recompute `CurrentAbility` from the attributes at `Decode` time instead of refusing a mismatch
(the field is a derived cache — nothing forbids re-deriving it on load), which would trade the "corrupt
save" diagnostic this gate currently provides for silent self-healing on every bias-table retune; which of
the two is preferable is an owner call, not something this pass decides.

**`birthWorldDay` widened `u32 → i64` (ERR-028-006).** The anchor MUST be signed (§3.1.1) and a 32-bit
signed field is not comfortably wide against `Age0 · DAYS_PER_YEAR` for long-lived save histories, so
the field is `i64`, matching `GrowthCursor`'s width. The widening is **free**: `PROGRESSION_SAVE_FORMAT_VERSION`
is still 1 and this format has never shipped in a released build, so there is no prior-version file to
migrate and F3's "first written layout is the format permanently" rule has nothing to grandfather.

The block carries the same MUSTs as its `TrainingBlock`/`MedicalBlock`/`AppearanceBlock` siblings:
the magic is checked **before** the version, so a foreign block is refused as the wrong format rather
than mis-diagnosed as the wrong generation of this one; the club id is **written**, not implied by list
order — identity carried by position alone is an implicit agreement with a sibling blob this codec is
forbidden to read (the ERR-041-008 rule); keys (`ClubId`, and `PlayerId` within a club) are **strictly
ascending** on decode, so a corrupt blob cannot smuggle in a duplicate; trailing bytes **throw**
(F5); and `Encode` refuses to write anything `Decode` would refuse to read back (an undefined
`PlayerPosition` ordinal or a non-ASCII name) — the never-write-what-Decode-refuses rule.
`Restore(byte[])` applies the fail-loud gate posture (FR-PG-018) in that order: magic, then version,
then an overflow-safe `ReadCount` for each count prefix (`0 ≤ count ≤ remaining`), then the ascending-key
check per entry, then the trailing-byte check, then per player the **EIGHT** VALUE gates enumerated
above (attributes, weak-foot, age, `potentialAbility`, `growthCursor`, `birthWorldDay`,
`currentAbility`, `retirementDay` — ERR-028-017 named the first four of these, ERR-028-019 the other
four; see that discussion above this layout block for the `[GT]`-keyed-acceptance-predicate open
decisions on `potentialAbility` and `currentAbility`), then, once every player has decoded, the
**global** checks that need the whole block assembled: the cross-club duplicate-id rule
(`RequireGloballyUniquePlayerIds`/`ERR-041-019`), the never-advanced-sentinel rule (`ERR-028-014`), the
M3 club-size rule (`[1, CLUB_SQUAD_SIZE]` per club, ERR-028-019), and the FR-PG-011 id-cursor rule
(`nextPlayerId` ahead of every carried `PlayerId`, ERR-028-019) — see the id-cursor and club-size
paragraphs below for what each refuses and why. **`Decode` throws `InvalidOperationException` for every
one of these; `Encode` and `ProgressionEngine.FromBlocks` throw `ArgumentException`** — the M-1 split
stated above, restated here because this is the ordering paragraph a future reader of the codec follows
top to bottom. The block is opaque to the season-save root, which frames
it as one more length-prefixed sub-blob (FR-PG-017) — the `SeasonSaveCodec` never parses it, so
`PROGRESSION_SAVE_FORMAT_VERSION` is independent of every other format version. **F3 makes the first
written layout the format permanently** — the ERR-029-004 rule — so this is not a draft pending
adjustment; a future field addition is a new format version, never a reordering of this one.

**A `null` player name round-trips to `""` — a known, deliberate non-idempotency, not a defect (AR pass
8 L-5, ERR-028-019).** `CanonicalSerializer.WriteString` writes length `0` for a `null` string and the
guarded string read returns `string.Empty` for a zero-length body, so the BYTES a `null` name and an
empty name produce are identical — the same input always produces the same output, so this is not a
round-trip defect — and no sim code reads names, so it has no downstream effect. Recorded here so a
future reader does not re-file it: this is a property of `CanonicalSerializer`'s own string contract
(#16 §3.2.4.1), not of this codec, and is deliberately left alone rather than "fixed" by refusing a
`null` name at `Encode`, which would turn a harmless identity collapse into a new fail-loud surface for
no behavioural gain.

**The FR-PG-011 id-cursor rule is enforced at FOUR boundaries sharing one owner (AR pass 5 + 8,
ERR-028-019) — previously enforced at exactly ONE (`ProgressionEngine.FromBlocks`), and undocumented in
this spec at all until now.** `NextPlayerId` must exceed every carried `PlayerId`, or the next regen
allocation collides with a live player and one player ends up with two careers sharing state silently.
Before AR pass 5, this rule lived only in `FromBlocks` — and since `Restore` is `Decode` + `FromBlocks`,
`Encode` could write a blob whose own `Restore` refuses forever (probe-verified before the fix: encoding
a club carrying players {10, 11} with `nextPlayerId: 0` produced a blob that decoded cleanly and whose
`Restore` then threw permanently; the codec's own round-trip fixture had satisfied the rule only by
coincidence). AR pass 5 added the check to `Encode` and `Decode`, sharing one owner
(`DescribeIdCursorNotAheadOfCarriedIds`) with `FromBlocks`. AR pass 8 (L-3) closed the fourth boundary:
`ProgressionEngine.SeedFrom` computed `maxPlayerId + 1` and assigned it directly with NO check at all,
so at `maxPlayerId == int.MaxValue` the addition silently overflowed (this project runs unchecked
arithmetic by default) to a negative cursor — a store that seeds, advances and plays but can NEVER be
saved, since the wrapped negative value reads as far behind every carried id it is actually ahead of.
`SeedFrom` now computes the candidate cursor and calls the same shared gate before assigning it.
Exception split: `Encode`/`FromBlocks`/`SeedFrom` throw `ArgumentException`; `Decode` throws
`InvalidOperationException` (M-1).

**The M3 club-size rule is enforced at THREE boundaries sharing one owner (AR pass 6, ERR-028-019) —
previously enforced NOWHERE, at any boundary, in this spec or in code.** A club's player count must sit
in `[1, CLUB_SQUAD_SIZE]` — `PlayerDatabase.Squad`'s own constructor bound, and #28's block IS the
roster (KD-4), so `ProgressionEngine.SquadFor` builds a `Squad` straight off it. Before this gate, a
0- or 30-player club advanced, saved and loaded cleanly and only threw from `SquadFor`, mid-round, inside
`ISquadProvider.ResolveByClubId`, after earlier fixtures in that round had already been applied to the
table — a club outside `Squad`'s own bound is state every boundary that can write or read it must
refuse, not just the one that happens to call `SquadFor` next. Shared owner
(`DescribeClubSizeOutOfRange`), called from `Encode`, `Decode` and `FromBlocks`. Same exception split as
the id-cursor rule: `Encode`/`FromBlocks` throw `ArgumentException`; `Decode` throws
`InvalidOperationException` (M-1).

**The cross-blob cursor rule (ERR-028-007).** `LastAdvancedWorldDay` is the **fourth** persisted
per-player cursor in #30's save frame — after #29's training cursor, #41's medical cursor, and #30's
own world-day clock — and it MUST be checked against the world clock at all three boundaries the
#29/#41 balance-pass AR loop established for its siblings: `SeasonSaveManager.Save`,
`SeasonSaveManager.Load`, and `SeasonLoop` composition. All three MUST delegate to a **single shared
predicate** rather than three independently hand-copied comparisons — the AR loop's own recorded lesson
(pass 9, `#41`/`#29`) is that two hand-copied walks of the same rule drift the moment one is edited and
the other is not. A lagging or leading cursor is worse here than for its siblings: `AdvanceDay` (§3.1)
**replays** every day between the cursor and the target, so a file whose cursor is paired against the
wrong world clock does not merely skip or repeat one day of growth — it banks N days of accrual from a
single day's `TrainingInput`, silently compounding every day of drift into growth points. **There is no
sentinel exemption (ERR-028-014).** An earlier revision of this rule exempted the sentinel value from
the within-one-day check, on the premise — copied from #29/#41's own cursor rule, where it is sound —
that "a never-advanced player has no clock to be paired against." The premise is false for #28
specifically: #29's and #41's fresh states (zero fatigue, no injuries) carry no clock-anchored quantity,
so "never advanced" means the same thing at every world day, but #28's fresh state **does** carry one —
age is derived from `BirthWorldDay` (§3.1.1) — so a never-advanced #28 state means a different age at
every clock value it might be paired against. The sentinel is not a legal store state at all: §3.1's
`SeedFrom` anchors `LastAdvancedWorldDay` at the seed day (never the sentinel), and `FromBlocks` (the
decode path) **refuses** a lifecycle carrying the sentinel cursor. With no legal state left for the
exemption to protect, the ordinary bidirectional lag predicate above applies unconditionally at all
three boundaries.

**The sibling anchor-vs-clock rule (AR pass 6 M2(b), ERR-028-019) — a DIFFERENT invariant from the
cursor rule immediately above, checked at the same two of the three boundaries.** `BirthWorldDay`
(§3.1.1) is an ANCHOR, not a cursor — it is checked ahead-only, never for lag, because a player's age
being derived from an arbitrarily old anchor is ordinary (a player born long before the epoch is the
normal case for a generated player, ERR-028-006), while an anchor AHEAD of the world clock is corrupt
state (§3.1.1's M2(a) fail-loud guard). This codec's own `DescribeOutOfRangeValues` (this section, gate
6 above) cannot enforce the ahead-of-clock half — it has no world day to bound against, only the
format's own `uint.MaxValue` ceiling — so the check lives at the **composition and file boundaries
`ProgressionSaveCodec` does not own**: `PlayerCareerStates.RequireBirthWorldDayWithinClock`
(`src/season-save/`, #30-owned), called from both `SeasonLoop`'s per-player composition walk and
`SeasonSaveManager`'s block-level walk, refuses `BirthWorldDay > worldTick` before a day step can reach
§3.1.1's guard. This is documented in full at #30 Appendix B.1 (the cross-blob cursor-vs-clock
paragraph's sibling) and #30 §2.3's new **F10** row — cited here rather than restated, since the
mechanism lives in an assembly #28 MUST NOT reference (§4.1).

**Obligation for the deferred season boundary (regen insertion).** `RunSeasonBoundary` (§3.4) is not
implemented by this landing, but when it is, a regen inserted mid-career **MUST** have its
`LastAdvancedWorldDay` anchored at its insertion day — the world day `RunSeasonBoundary` runs on — for
exactly the reason ERR-028-014 fixes at new-game: a regen is a freshly generated player whose records
describe the roster as of the day he is inserted, not before, so seeding anything else (including the
sentinel) reopens the identical unrepresentable-start defect one call site later. This is written down
here so the next author implementing `RunSeasonBoundary` does not rediscover the sentinel trap from
scratch.

**The roster must never be silently erased (ERR-028-008).** #28's block, per KD-4, is the **canonical
serialized roster** — not a cache rebuildable from the world seed once any player's `[1,20]` attributes
have evolved away from their generated values (see ERR-030-030). The save root MUST therefore refuse to
write a **zero-club** progression block over a destination file that already carries a **populated**
one: an empty store is a legitimate state only for a file that has never carried a roster, or that
itself already carries an empty one, never as a silent replacement for one that does. An unreadable or
foreign destination is not this guard's concern and is overwritten as before — the refusal is narrowly
about not erasing a roster the codec can actually see.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms: KD-1 integer growth projection + age derivation + weighted spend, the CA/PA model, regen generation, retirement + season boundary, the save codec. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-08-08 | — | ERR-028-003: §3.2 states new-game `PotentialAbility` is authored data owned by #47, with #28's `NEW_GAME_PA_HEADROOM` seed as a placeholder, plus the recorded ~421-of-`ABILITY_MAX` growth-rate limitation. ERR-028-004: §3.5's layout corrected from version-first/domain-tag-as-identifier to the shipped magic-led `PROG` layout. ERR-028-005: §3.1 gains the public batch `AdvanceDay` pseudocode showing the `LastAdvancedWorldDay` idempotency/gap-completeness cursor. Spec + code, same commit (T1/T2a). |
| 0.4 | 2026-08-08 | — | ERR-028-006: §3.1.1 states `BirthWorldDay` MUST be signed (a new world starts at day 0, so any generated player with `Age0 > 0` anchors negative) and forbids clamping it; §3.5's layout widened `u32 → i64`, free at format version 1. ERR-028-007: §3.5 gains the cross-blob cursor rule — `LastAdvancedWorldDay` is the fourth persisted per-player cursor and MUST be checked at all three save/load/composition boundaries through one shared predicate, lag being worse here because `AdvanceDay` replays gaps. ERR-028-008: §3.5 states the save root MUST refuse to overwrite a populated progression block with an empty one. ERR-028-009: §3.1's `AdvanceDay` pseudocode gains the F8 sentinel-refusal guard as its first line. Spec + code, same commit (AR over the T1/T2a landing). |
| 0.5 | 2026-08-09 | — | ERR-028-014: §3.1's `AdvanceDay` pseudocode loses the never-advanced branch and its "anchors; cannot know an earlier start" comment, which was false — `SeedFrom` is handed the seed day, so the store always knows it; the seed-day-is-the-cursor rule is stated in its place. §3.5's cursor rule drops the sentinel exemption from the cross-blob cursor check — the exemption's premise (copied from #29/#41, sound there) is false for #28, whose fresh state carries a clock-anchored quantity (derived age); the sentinel is no longer a legal store state at either boundary, `FromBlocks` refuses it. §3.5 gains the deferred-season-boundary obligation: a mid-career regen insertion must anchor its cursor at its insertion day, for the same reason. Spec + code, same commit. |
| 0.6 | 2026-08-10 | — | ERR-028-017 (AR pass 5 spec-vs-code sweep, found against the T1/T2a landing, no code change). **§3.1.1**: the age formula is stated unconditionally; `GrowthProjection.AdvanceDayForPlayer` guards `age = 0` when `ageDays ≤ 0` rather than dividing — now stated. **§3.4**: the daily retirement check's placement was undocumented — it runs ONCE per `AdvanceDay` call (in `ProgressionEngine.AdvancePlayerTo`, which wraps the whole gap-replay loop), against the age derived at the call's target day, never once per lived day inside the replay; `RetirementDay` is therefore stamped with the call's target day, not the earlier day within a multi-day gap on which the threshold was actually crossed — recorded as a known limitation and cross-referenced to §5's T-PG-DET-002 far-future-gap tests, which a reader would otherwise rebuild. **§3.5**: the byte layout left `str` unencoded (now pinned: `u32` length + ASCII, #16 §3.2.4.1) and its fail-loud enumeration named only framing gates, omitting the four VALUE gates `Decode` applies (attribute range, weak-foot range, non-negative age, `PotentialAbility` within `[PA_MIN, ABILITY_MAX]`) — now stated, with the `PA_MIN`/`ABILITY_MAX` `[GT]` tags' tension against #30 Appendix B.1's no-`[GT]`-gating-on-decode posture recorded as an OPEN decision, not resolved. |
| 0.7 | 2026-08-10 | — | ERR-028-018 (High): §3.1 gains the accrual-window paragraph "already accounted for" was silent on — anchoring `LastAdvancedWorldDay` at the seed day stops that day being REPLAYED, but says nothing about whether `GrowthCursor` was CREDITED for it, and the code shipped crediting nothing. Since a band exit is decided by the derived age, not the cursor, that left every full band traversal one whole attribute point short (`N · DAYS_PER_YEAR − 1` days accrued, not `N · DAYS_PER_YEAR`) with a permanent residue eating the first year of the next accruing band — contradicting Appendix A / KD-8's `+1/yr` promise. `ProgressionEngine.SeedLifecycle` now credits the seed day's own band step at construction; spec text now states this as a MUST rather than leaving it implied by "already accounted for". Spec + code, same commit in spirit — code landed at `789ea74`, this row supplies the close-out FR-CS-057 requires. |
| 0.8 | 2026-08-11 | — | ERR-028-019 — docs close-out for AR passes 5-8 (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`), four consecutive production landings with no `docs/specs/` edit at all. **§3.1**: the spend/drain pseudocode rewritten — fail-loud on `ageDays < 0` (M2(a), AR pass 6) rather than the retired `ageDays ≤ 0 → age 0` guard; saturating narrowing at `MAX_DERIVABLE_AGE_YEARS` (AR pass 5); both spend and drain refusal branches now clamp `GrowthCursor = 0` and `break` (superseding the AR-pass-5-only `POINT_COST - 1` clamp this section never carried, and the original no-exit `DrainOnePoint` call, AR pass 6 High); the two-clamp-values history stated explicitly so a reader is not left to reconstruct it from `spec-error-log.md`. **§3.1.1**: the age-formula guard corrected — `ageDays == 0` is ordinary (age 0), `ageDays < 0` now FAILS LOUD rather than silently deriving age 0 (M2(a)); the ERR-028-017 "guarded at zero" claim SUPERSEDED in place; `MAX_DERIVABLE_AGE_YEARS`'s own history (first set to a football-plausibility 1000, corrected same-session to the representability bound 100,000,000) stated so the tag distinction is not re-litigated. **§3.3**: regen construction now credits `GrowthCursor` at its own construction day's band step (AR pass 7), the second of two `PlayerLifecycle` construction sites ERR-028-018 needed and did not reach at `SeedLifecycle` alone. **§3.5**: the four-value-gate enumeration ERR-028-017 recorded is SUPERSEDED (not restated) by eight; the Encode/FromBlocks-vs-Decode `ArgumentException`/`InvalidOperationException` exception-type split (AR pass 8 M-1) stated as a general rule covering every shared boundary rule in this section, correcting F8's stale claim (§2.3) that both codec sides threw the same type for the sentinel gate; the FR-PG-011 id-cursor rule (four boundaries, including the AR-pass-8 `SeedFrom` overflow fix) and the M3 club-size rule (three boundaries) stated in full — neither had ANY normative text in this spec before this pass; the null-name non-idempotency documented as deliberate (AR pass 8 L-5); a new paragraph cross-references the #30-owned `BirthWorldDay`-vs-clock composition/file check (M2(b)) to #30 Appendix B.1 rather than restating a mechanism §4.1 forbids #28 from referencing; a new OPEN decision recorded on the `CurrentAbility`/`ComputeCA`/`PositionAttributeBias` save-acceptance predicate (AR pass 9's finding), adjacent to the existing `PA_MIN`/`ABILITY_MAX` one, not resolved — no tag changed, no code changed. Code unchanged by this pass; verified against `src/player-progression/*.cs` at commit `6987dbf`. |
| 0.9 | 2026-08-22 | — | **ERR-028-020 + ERR-028-021** — the football-judgment proxy review's batch-1 #28 findings (`docs/tracking/football-judgment-proxy-review.md` §3 / §6.3.1 batch 1), spec + code in the same commit. **ERR-028-020 (§3.1, new §3.1.3):** the daily accrual was `DailyPoints(ClassifyAgeBand(ageYears), …)` — a hard three-way step at an exact integer age, on a judgment ("is this player still developing?") that is continuous everywhere; pattern (b), and pattern (d) against §1.3's promise of "per-attribute CA/PA growth-decline curves keyed to age", of which no age-continuous curve existed anywhere in this spec. Replaced under doctrine **P1** by `DailyBandPoints(ageDays)`: a centred linear ramp of half-width `AGE_BAND_RAMP_HALF_WIDTH_YEARS` at each edge, evaluated as the difference of an exact integer cumulative so the per-day step stays in `{0, ±1}`, the cursor scale is unchanged and `PROGRESSION_SAVE_FORMAT_VERSION` does not move. **P5 is exact rather than fitted**: a centred ramp has the same integral as the step it replaces for EVERY half-width, so no growth-rate recalibration is owed and ERR-028-018's no-residue traversal invariant survives by construction; half-width 0 reproduces KD-8 / FR-PG-007 byte-for-byte (§5 lock, executed through a parameterised overload because the `[GT]` is read once at static init). `ClassifyAgeBand` demoted to a READ of the curve — the sign of the year's net accrual — rather than a second surface deciding the same question. Two catalogue invariants (non-negative half-width; disjoint ramps) enforced fail-loud at the computing site. Recorded, not fixed: the finding's "no per-player variance" half is the Stage-3 `curveEnabled` tier's, needing §3.2's `(PA − CA)` modulation. **ERR-028-021 (§3.4):** retirement was `AgeYears >= RETIREMENT_AGE` — one integer age for the entire league, with no position or attribute input, so a goalkeeper retired on a forward's clock and one calendar day separated a career continuing from ending; pattern (b)/(c). Replaced by a per-player `RetirementAgeDays(record)` compared in DAYS: the baseline, plus `RETIREMENT_GOALKEEPER_BONUS_YEARS`, plus a full-range anti-symmetric offset over the Anticipation/Positioning/Composure mean. **P3 ledger entry recorded explicitly**: robustness was the obvious input and is deliberately NOT used, because #29's `ComputeInjuryRisk` and #41's `RobustnessMitigation` already price Strength/Stamina/Balance twice over (`ERR-041-003`) — career length is owned by the reading trio, which nothing else consumes. **P5 exact at both scales**: zero bonus + zero span is identically the retired comparison, and the shipped offset sums to exactly 0 over a uniform attribute population, so the league's retirement rate is unchanged and only who-retires-when moves. Neither fix adds an RNG draw, a stream, a domain tag or a format version. |
| 0.10 | 2026-08-22 | — | **ERR-028-022 + ERR-028-023** — the reviewed High findings against the v0.9 landing, spec + code in the same commit. **ERR-028-022 (§3.4):** the game-reading offset computed `mean = (Anticipation + Positioning + Composure) / 3` and mapped that mean anti-symmetrically. `floor(sum / 3)` is NOT symmetric about the attribute midpoint — truncation always bites downward — so the map was anti-symmetric only along the `Ant == Pos == Comp` diagonal, and v0.9's P5 claim ("the offsets over a uniform `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` population sum to exactly 0: the league's retirement RATE is unchanged and only which players retire when moves") was false everywhere else. Measured through the built assembly: **−204,621 days over the uniform `[1,20]³` product = −25.58 days per player** — the whole league retiring about two months early, which is a rate change. The §5 lock that purported to prove the property swept only the diagonal, i.e. exactly the line on which the division by 3 is exact and the defect vanishes, so it passed against the broken model and against a mutation to a differently-wrong rounding. §3.4's pseudocode now carries the SUM undivided into the numerator (`(2·sum − 3·(MIN + MAX)) · span / (6 · (MAX − MIN))`): exactly anti-symmetric (the product sums to 0), and bit-for-bit identical to the retired form on the diagonal, since for `sum == 3·mean` numerator and denominator are both exactly 3× the old ones and integer division truncates toward zero. The superseded claim is ANNOTATED in place rather than restated, and the **residual honest fact is recorded with it**: #27's `RosterGenerator` draws each attribute on `AttributeBaseMean ± AttributeSpread` = `[6,14]`, centred on 10 against the offset's neutral midpoint of 10.5, so the corrected offset still averages **≈ −38 days per generated player** — a half-point of centring, not a broken map, and not this ERR's to re-pivot (pinning the offset to the generator's mean is the coupling `ERR-041-020` refused for `AGE_RISK_PIVOT_YEARS`). **ERR-028-023 (§3.1):** the normative seed-credit MUST still ordered `SeedFrom` to credit "the seed day's own `DailyPoints` step for the player's seed-time age band (`GROWTH_DAILY_POINTS` in Growth, `DECLINE_DAILY_POINTS` in Decline, `0` in Stable)" — the three-way band step ERR-028-020 retired, mandated normatively one section above the curve that replaced it. The code has computed `AbilityModel.DailyBandPoints(rec.Age · DAYS_PER_YEAR)` since that landing, and the two forms disagree at bootstrap ages **24, 25, 29, 30** at the shipped `AGE_BAND_RAMP_HALF_WIDTH_YEARS` — 4 of the 19 ages `RosterGenerator` draws — so an implementer following the spec reopened ERR-028-018's one-day accrual discrepancy for ~21% of the roster, silently and only inside the ramps. Amended to `DailyBandPoints(Age₀ · DAYS_PER_YEAR)`, mirroring the §3.3 regen paragraph the ERR-028-020 commit amended for exactly this reason and stopped at. Neither fix adds an RNG draw, a stream, a domain tag or a format version. |
| 0.11 | 2026-08-23 | — | Football-judgment proxy review, batch-1 adversarial findings, four doc-only corrections, spec + code together where a test id is allocated. **guards-unexercised:** §3.4's `RetirementAgeDays` pseudocode showed only `if days <= 0: FAIL LOUD`, omitting the leading non-negativity check on `RETIREMENT_GAME_READING_SPAN_YEARS`/`RETIREMENT_GOALKEEPER_BONUS_YEARS` that Appendix A already mandates and `AbilityModel.RetirementAgeDays` has always enforced — added. **config-unbound-premise-false-28:** §3.1.3's disjointness-guard paragraph justified its computing-site placement with "the `[GT]` is a config key and the catalogue lock runs config-unbound" — copied from ERR-041-003's rationale, where it is true, without checking it against this catalogue: `PlayerProgressionConstants.cs` has zero `Config.GetX` calls, so the rationale was false here. Corrected to state the placement is forward-looking for the Stage-1 loader, and that a catalogue-level lock exists today in `PlayerProgressionConstantsTests`. **spec-32-stale-flat-band-step:** §3.2 still described `DailyPoints` as "the flat §4.3 band step" in the minimal tier, present tense, after ERR-028-020 made `DailyBandPoints`'s ramp the accrual authority; corrected. **classifyageband-growth-claim-stale:** §3.5's `birthWorldDay` lower-bound value-gate rationale said `ClassifyAgeBand` reads `int.MinValue` as `Growth` — true when written, false since ERR-028-020 (`ClassifyAgeBand` now returns `Stable` there); annotated as history, the int-narrowing concern itself unchanged. |
| 0.12 | 2026-08-24 | — | Round-2 adversarial finding `construction-day-credit-implemented-twice` (High), spec + code in the same commit. The construction-day credit — the rule that a site anchoring `LastAdvancedWorldDay` at its own construction day owes that day's band step to `GrowthCursor` — was IMPLEMENTED TWICE: inlined in `ProgressionEngine.SeedLifecycle` and again as `RegenGenerator.BandStepFor`, whose own documentation described itself as the shared owner of a rule it did not own. That duplication is not hypothetical debt here: it is why ERR-028-018 credited the seed site and left the regen site at `0` (found a day later, AR pass 7) and why ERR-028-020 then had to visit both sites again to move each off the retired three-way step. §3.3 now states the single-owner requirement normatively (`AbilityModel.ConstructionDayCredit`, alongside the §3.1.3 curve it reads) and §3.1's seed-credit MUST points at the same owner; §3.3's `BandStepFor(age)` reference retires with the method, which is deleted. **No behaviour change** — the two implementations were character-for-character identical, verified by probe over ages 0..200 and the `int` domain's edges (including `int.MinValue`/`MaxValue`) before the collapse, and the suite is unchanged at 147 pre-existing passes. What the landing adds is the divergence detection neither form had: a cross-SITE lock seeding a `ProgressionEngine` from a regen's own returned record and requiring both credits to agree, plus a seed-site case at a ramp age (every prior seed-credit case drove 18/27/34, all outside both ramps, where the retired step and the continuous curve agree day for day). No new constant, no `[GT]`, no draw, no stream, no domain tag, no format version. |
| 0.13 | 2026-08-24 | — | Round-2 Medium/Low adversarial findings, doc-only. **M2 (spec-32-flat-band-step-sweep-stopped-two-paragraphs-short):** §3.2's "Recorded limitation, not fixed here" paragraph — two paragraphs below the v0.11 correction of the same stale phrasing — restated against §3.1.3's accrual curve instead of "the §4.3 flat band step"; the ~421-of-`ABILITY_MAX` figure is unchanged (the P5 pivot makes it exact at every half-width). **L1 (four-guards-enumerated-as-five-and-mis-named):** §3.4's `RetirementAgeDays` pseudocode note corrected — it said "these TWO dial guards" for the single combined non-negativity `if`, which made the paragraph claim "four" and then list five items; now "this ONE combined dial guard". **M5 (retirement-day-derived-from-attributes-the-same-step-mutates):** new §3.4 paragraph states the retirement-day feedback-loop invariant — the day is re-evaluated against the same record §3.1.2 just mutated, bounded today only by the one-directional band order and `RetirementFlag`'s stickiness — and cites the new **T-PG-RET-009** lock (`section-5.md` v0.9). No numeric value, no draw, no format version. |
#endregion
