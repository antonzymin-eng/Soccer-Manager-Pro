# Player Progression & Lifecycle #28 — Section 3: Core Algorithms

**Created:** July 23, 2026
**Last Updated:** August 10, 2026 (v0.7 — ERR-028-018: §3.1 states the seed-day accrual-window rule — the seed day's own band step MUST be credited to `GrowthCursor`, not merely excluded from replay — closing the gap that let a full band traversal accrue one attribute point short of Appendix A / KD-8's `+1/yr` promise)
**Last Updated (prior):** August 10, 2026 (v0.6 — ERR-028-017: AR pass 5 spec corrections — §3.1.1 states the `ageDays ≤ 0 → age 0` guard the formula omitted; §3.4 states the retirement evaluation runs once per `AdvanceDay` CALL on post-replay age (not once per lived day), with the multi-day-gap `RetirementDay` limitation recorded and cross-referenced to T-PG-DET-002; §3.5's byte layout pins the `str` encoding (u32 length + ASCII, #16 §3.2.4.1) and states the four VALUE gates `Decode` applies (previously undocumented), with the `PA_MIN`/`ABILITY_MAX` config-keyed-acceptance-predicate tension against #30 Appendix B.1's posture recorded as an OPEN decision)
**Last Updated (prior):** August 9, 2026 (v0.5 — ERR-028-014: the never-advanced sentinel retired from #28's legal store states)
**Last Updated (prior):** August 8, 2026 (v0.4 — ERR-028-006/007/008/009: the signed age anchor, the cross-blob cursor rule, the destination-roster-overwrite refusal, and the F8 sentinel guard)
**Version:** 0.7
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
    ageYears  = (worldDay - lifecycle.BirthWorldDay) / DAYS_PER_YEAR    # integer division
    record.Age = ageYears                                              # keep the record's Age current (cache)

    # 2. Per-day point accrual — the ONLY accumulator (FR-PG-002/003).
    ageBand   = ClassifyAgeBand(ageYears)                              # Growth | Stable | Decline
    dailyPts  = DailyPoints(ageBand, record.Position, in trainingInput, curveEnabled)  # signed integer, fixed-point
    lifecycle.GrowthCursor += dailyPts

    # 3. Spend/drain whole attribute-points at the POINT_COST threshold (deterministic order).
    while lifecycle.GrowthCursor >= POINT_COST:
        if not TrySpendOnePoint(ref record, ref lifecycle):     # respects the PA ceiling (F1)
            break                                               # at ceiling — leave the cursor (no thrash)
        lifecycle.GrowthCursor -= POINT_COST
    while lifecycle.GrowthCursor <= -POINT_COST:
        DrainOnePoint(ref record, ref lifecycle)                # symmetric decline
        lifecycle.GrowthCursor += POINT_COST

    # 4. Recompute the derived CA summary (never a second accumulator, FR-PG-003).
    lifecycle.CurrentAbility = ComputeCA(in record.Attributes, record.Position)
```

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
lifecycle overlay): `AgeYears = (worldDay − BirthWorldDay) / DAYS_PER_YEAR` (integer division), **guarded
at zero when `worldDay ≤ BirthWorldDay` (ERR-028-017 — this formula was previously stated unconditionally)**:
`GrowthProjection.AdvanceDayForPlayer` computes `ageDays = worldDay − BirthWorldDay` and returns `age = 0`
whenever `ageDays ≤ 0`, rather than dividing. Since `BirthWorldDay` is SIGNED and ordinarily negative for
a generated player (§3.1.1 below / ERR-028-006), the unconditional formula would otherwise divide a
non-positive numerator by a positive divisor — never undefined in C# integer arithmetic, but the result
(zero or a small negative quotient, truncating toward zero) is not a meaningful age for a player who has
not yet "reached" his own birth day relative to the world clock he is being read against. The guard
applies at every call — including the ordinary case, where `worldDay` sits at or shortly after
`BirthWorldDay` and `ageDays` is small but positive, which the unguarded formula already handles
correctly; the guard only changes the `ageDays ≤ 0` edge. `BirthWorldDay` is pinned once at new-game from
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

**The fail-loud enumeration below was incomplete — it named only the framing gates, not the VALUE
gates `Decode` applies to what the framing successfully reads (ERR-028-017).** `ReadPlayer` range-gates
four value fields the framing-level list omits entirely: each `[1,20]` attribute against
`[ATTRIBUTE_MIN, ATTRIBUTE_MAX]`, `weakFootRating` against `[WEAK_FOOT_MIN, WEAK_FOOT_MAX]` (#27's
bounds), `age` against `≥ 0` (the field is a derived cache — see §3.1.1's SIGNED `birthWorldDay` for the
authoritative anchor, which MAY legitimately be negative; the cache itself may not), and
`potentialAbility` against `[PA_MIN, ABILITY_MAX]` (the F1 growth ceiling — a corrupt value below the
floor would silently freeze a player's growth forever, and one above the ceiling would silently unbound
it). All four throw `InvalidOperationException`, matching the framing gates' type (see the corrected F3
row, §2.3). **`PA_MIN` and `ABILITY_MAX` are `[GT]` (Appendix A)** — this makes the `potentialAbility`
gate a save-acceptance predicate keyed on tunable config, the exact posture #30 Appendix B.1 reasons
AGAINST for its own appearance sub-blob ("gating it would turn a retune into data loss"). **This is
recorded as an OPEN decision, not resolved here**: whether the range gate should instead read from a
`[FIXED]`/`[DERIVED]` bound, or whether the tension with #30's stated posture is acceptable because this
block is the roster itself (KD-4) rather than an overlay, is an owner call for a future pass. Nothing in
this correction changes any tag, retags any constant, or migrates the gate.

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
check per entry, then the trailing-byte check, **then, per player (ERR-028-017 — this ordering previously
stopped at framing and never named these), the VALUE gates**: each `[1,20]` attribute against
`[ATTRIBUTE_MIN, ATTRIBUTE_MAX]`, `weakFootRating` against `[WEAK_FOOT_MIN, WEAK_FOOT_MAX]`, `age` against
`≥ 0`, and `potentialAbility` against `[PA_MIN, ABILITY_MAX]` (the F1 ceiling) — see the note above this
layout block for why the last of those is a save-acceptance predicate keyed on `[GT]` config, recorded as
an open decision, not resolved here. The block is opaque to the season-save root, which frames
it as one more length-prefixed sub-blob (FR-PG-017) — the `SeasonSaveCodec` never parses it, so
`PROGRESSION_SAVE_FORMAT_VERSION` is independent of every other format version. **F3 makes the first
written layout the format permanently** — the ERR-029-004 rule — so this is not a draft pending
adjustment; a future field addition is a new format version, never a reordering of this one.

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
#endregion
