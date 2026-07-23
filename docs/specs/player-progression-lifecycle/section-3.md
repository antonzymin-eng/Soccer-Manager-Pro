# Player Progression & Lifecycle #28 — Section 3: Core Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

All algorithms run on the world tick and are testable without Unity (integer-only except the derived
CA summary, which is integer too). The `[GT]` magnitudes are illustrative pending the balance pass;
the contract is the shapes and the byte-exactness.

## 3.1 The daily growth projection (KD-1) — integer fixed-point, byte-exact

The daily step is a pure function of the player's state + inputs — **no RNG draw** (FR-PG-002). It is
the single writer of attribute change (FR-PG-008).

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
lifecycle overlay): `AgeYears = (worldDay − BirthWorldDay) / DAYS_PER_YEAR` (integer division).
`BirthWorldDay` is pinned once at new-game from #27's generation-time `PlayerRecord.Age`
(`BirthWorldDay = newGameDay − Age0 · DAYS_PER_YEAR`, where `Age0 = PlayerRecord.Age` at new-game).
There is **no** `AgeAnchorDay` field and **no** rollover `while`-loop — age is a pure function of the
world day, so nothing anchors or double-counts. #28 keeps the career-state `PlayerRecord.Age` field
**current** as a derived cache (the CA-cache pattern — recomputed each day, never a second source of
truth), so a consumer reading `record.Age` gets current age, not the frozen new-game seed. This is the
same one-representation discipline as the CA/PA model (§3.2): one authoritative anchor (`BirthWorldDay`),
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

`ProgressionEngine.Snapshot() → byte[]` writes, via `CanonicalSerializer`: `PROGRESSION_SAVE_FORMAT_VERSION`
→ `DOMAIN_TAG_PLAYER_PROGRESSION` → `NextPlayerId` → the boundary marker → an entry count → per entry
`{ PlayerRecord (PlayerId, names, current age, position, 31 attrs + WeakFoot) , PlayerLifecycle
(incl. BirthWorldDay + GrowthCursor) }`.
`Restore(byte[])` reads it back with the fail-loud gate posture (FR-PG-018): version gate first, an
overflow-safe `ReadCount` for the entry count (`0 ≤ count ≤ remaining`), and a trailing-byte check.
The block is opaque to the season-save root, which frames it as one more length-prefixed sub-blob
(FR-PG-017) — the `SeasonSaveCodec` never parses it, so `PROGRESSION_SAVE_FORMAT_VERSION` is
independent of every other format version.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms: KD-1 integer growth projection + age derivation + weighted spend, the CA/PA model, regen generation, retirement + season boundary, the save codec. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
