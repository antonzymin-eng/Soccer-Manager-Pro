# Player Progression & Lifecycle #28 — Section 3: Core Algorithms

**Created:** July 23, 2026
**Last Updated:** August 11, 2026 (v0.8 — ERR-028-019: docs close-out for AR passes 5-8, four consecutive production landings (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`) with no `docs/specs/` edit — §3.1's spend/drain pseudocode rewritten for the AR pass 6/8 changes (fail-loud on a future-dated `BirthWorldDay`, saturating age narrowing at `MAX_DERIVABLE_AGE_YEARS`, both refusal branches clamp to 0 rather than banking or leaving the cursor, `DrainOnePoint` returns `bool`); §3.1.1's age-formula guard corrected from "guarded at zero" to "fails loud below zero, ordinary at zero"; §3.3 states the AR pass 7 regen construction-day credit; §3.5's fail-loud enumeration rewritten from four value gates to eight, with the Encode/FromBlocks-vs-Decode exception-type split stated per gate, and gains the FR-PG-011 id-cursor and M3 club-size rules (previously undocumented); a new OPEN decision recorded on the `CurrentAbility`/`ComputeCA` save-acceptance predicate, adjacent to the existing `PA_MIN` one)
**Last Updated (prior):** August 10, 2026 (v0.7 — ERR-028-018: §3.1 states the seed-day accrual-window rule — the seed day's own band step MUST be credited to `GrowthCursor`, not merely excluded from replay — closing the gap that let a full band traversal accrue one attribute point short of Appendix A / KD-8's `+1/yr` promise)
**Last Updated (prior):** August 10, 2026 (v0.6 — ERR-028-017: AR pass 5 spec corrections — §3.1.1 states the `ageDays ≤ 0 → age 0` guard the formula omitted; §3.4 states the retirement evaluation runs once per `AdvanceDay` CALL on post-replay age (not once per lived day), with the multi-day-gap `RetirementDay` limitation recorded and cross-referenced to T-PG-DET-002; §3.5's byte layout pins the `str` encoding (u32 length + ASCII, #16 §3.2.4.1) and states the four VALUE gates `Decode` applies (previously undocumented), with the `PA_MIN`/`ABILITY_MAX` config-keyed-acceptance-predicate tension against #30 Appendix B.1's posture recorded as an OPEN decision)
**Last Updated (prior):** August 9, 2026 (v0.5 — ERR-028-014: the never-advanced sentinel retired from #28's legal store states)
**Last Updated (prior):** August 8, 2026 (v0.4 — ERR-028-006/007/008/009: the signed age anchor, the cross-blob cursor rule, the destination-roster-overwrite refusal, and the F8 sentinel guard)
**Version:** 0.8
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
therefore seed `GrowthCursor` at the seed day's own `DailyPoints` step for the player's seed-time age
band (`GROWTH_DAILY_POINTS` in Growth, `DECLINE_DAILY_POINTS` in Decline, `0` in Stable) — the single
call to `AdvanceDayForPlayer` line 2 would have made on that day, without also running its spend/drain
step or its `LastAdvancedWorldDay` write (both already handled by the anchor). This is not derivable
from "the seed day is already accounted for" by itself; that sentence is a claim about the CURSOR's
correctness, and crediting the band step is what makes it true rather than aspirational.

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
    ageBand   = ClassifyAgeBand(ageYears)                              # Growth | Stable | Decline
    dailyPts  = DailyPoints(ageBand, record.Position, in trainingInput, curveEnabled)  # signed integer, fixed-point
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
`DailyPoints` and the spend/drain `while` bounds are pure integer functions of serialized state, and
age is a pure integer function of `(worldDay, BirthWorldDay)` with **no** discrete rollover event to
double-count — a save on any day restores to the identical continuation. **KD-8 identity:** with
`curveEnabled` off, `DailyPoints` returns `GROWTH_DAILY_POINTS` / `DECLINE_DAILY_POINTS` (`±1` with
`POINT_COST = DAYS_PER_YEAR`) so the cursor crosses `POINT_COST` exactly once per year in the
Growth/Decline bands and zero times in Stable — the literal §4.3 `±1/year` step, byte-for-byte (§5 lock).

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

## 3.2 The CA/PA model

`PotentialAbility` (PA) is generated once at regen/new-game (a wide-integer ceiling from the
`progression.regen` stream, §3.3) and never rises. `CurrentAbility` (CA) is `ComputeCA(attributes,
position)` — a position-weighted mean of the `[1,20]` attributes scaled to `[0, ABILITY_MAX]`,
**recomputed** whenever attributes change (FR-PG-003). CA→PA gap drives growth magnitude in the deep
tier (`DailyPoints` scales with `(PA − CA)` when `curveEnabled`); in the minimal tier the gap is
unused and `DailyPoints` is the flat §4.3 band step. CA is a cache in the serialized block, but the
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

**Recorded limitation, not fixed here:** at the §4.3 flat band step, a whole youth career (roughly
eight growth years, one attribute raised per year) raises CA by only ~421 of `ABILITY_MAX` = 10,000
(8 years × ~52.6 per point). The PA ceiling therefore binds only when the authored CA→PA gap is under
about 420 — no realistic authored wonderkid gap is that small. **PA-as-ceiling is decorative regardless
of PA's source** (authored or drawn); the cause is the growth RATE, not where PA comes from. Closing it
is the Stage-3 `curveEnabled` tier's job, and KD-W1 forbids retuning the flat-band rate in a landing
that has not wired the deep tier.

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
sets `GrowthCursor = BandStepFor(age)` at construction — the construction day's own `DailyPoints` step
for the drawn age's band (`GROWTH_DAILY_POINTS` in Growth, `DECLINE_DAILY_POINTS` in Decline, `0` in
Stable; a regen's drawn age is always in `[REGEN_AGE_MIN, REGEN_AGE_MAX]`, which is always below
`GROWTH_AGE` today, so this is Growth-band only in practice — but classified rather than hard-coded, so
it does not silently become wrong if either age constant moves).

## 3.4 Retirement + the season boundary (KD-5 / KD-6)

**Daily (in `AdvanceDay`):** `if AgeYears >= RETIREMENT_AGE and not RetirementFlag: RetirementFlag =
true; RetirementDay = worldDay`. Deterministic-hard — no draw (FR-PG-013). The player stays in the
roster and stays selectable (FR-PG-014).

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
   the derived age to `int.MinValue`, which `ClassifyAgeBand` reads as `Growth` (so the player grows
   forever and `RETIREMENT_AGE` can never fire — ERR-028-006's failure mode through a different door),
   and which this very gate then refused as a negative age, making a career that loaded, advanced and
   projected fine PERMANENTLY unsavable.
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
#endregion
