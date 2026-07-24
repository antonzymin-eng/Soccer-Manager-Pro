# Season & Competition Loop Specification #30 — Section 3: Algorithms

**Created:** July 22, 2026
**Last Updated:** July 24, 2026 (v0.7 — back-prop ERR-030-007 scouting tick-order seam; prior v0.6 ERR-030-006, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Version:** 0.7
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 3.1 Deterministic round-robin fixture generation (FR-SN-001..004)

The **circle method** (polygon method) produces a single round-robin for `N` clubs in `N−1` rounds;
running it twice with home/away swapped yields the double round-robin (FR-SN-002).

```
Generate(clubIds[N], seed) -> Fixture[]:
    if N < 2: throw            # F1 / FR-SN-004
    ids := clubIds
    if N is odd:
        ids := clubIds + [BYE]  # phantom club; fixtures against BYE are dropped
        M := N + 1
    else:
        M := N
    # Fixed circle rotation — index 0 pinned, the rest rotate. No RNG:
    # the seed selects only the *labelling* order below (§3.1.1), not the pairing structure,
    # so the single-league case needs no draw (FR-SN-027).
    firstLeg := []
    for round in 0 .. M-2:
        for i in 0 .. M/2 - 1:
            a := ring[i]; b := ring[M-1-i]
            if a != BYE and b != BYE:
                # home/away by round parity for a balanced first leg
                (home, away) := (round even) ? (a, b) : (b, a)
                firstLeg.append(Fixture{ round, home, away })
        rotate ring (index 0 fixed; others shift one position)
    # Second leg: same pairings, reversed venue, rounds offset by (M-1).
    secondLeg := [ Fixture{ f.round + (M-1), f.away, f.home } for f in firstLeg ]
    return firstLeg ++ secondLeg
```

**Determinism (FR-SN-001):** the rotation is a fixed integer schedule; there is no RNG in the
pairing. The output is a pure function of `(clubIds, seed)`.

### 3.1.1 Where the seed enters

For the single-league Stage-0 surface, the pairing structure is seed-independent (the circle
method is deterministic on its own). The `seed` selects only a **deterministic label permutation**
of `clubIds` before generation (so two seasons over the same club set differ in *which* club sits at
each ring position, hence the fixture order), drawn once from the season RNG sub-stream
(`DOMAIN_TAG_SEASON_LOOP`). If the permutation is the identity (a documented Stage-0 option), the
generator makes **zero** draws — FR-SN-027's "needs no draw for the single-league case" holds
exactly, and the RNG sub-stream is reserved for genuinely stochastic season events (#43 cup draws,
a rule that leaves a tie to a draw) rather than the schedule itself.

### 3.1.2 Serialization, not regeneration (FR-SN-028 / KD-5)

The concrete `Fixture[]` is **serialized into the season blob**. `Generate` runs once, at season
creation; a loaded season trusts the serialized list and never re-runs the generator. Its
determinism is a two-run creation-time test (§5), not a load-time recomputation.

## 3.2 League table update & tie-breaks (FR-SN-005..008)

```
ApplyResult(table, home, away, homeGoals, awayGoals):
    if home == away: throw                      # F2
    if homeGoals < 0 or awayGoals < 0: throw    # F2
    hr := table.row(home) or throw              # F2 (unknown club)
    ar := table.row(away) or throw
    hr.Played++; ar.Played++
    hr.GoalsFor += homeGoals; hr.GoalsAgainst += awayGoals
    ar.GoalsFor += awayGoals; ar.GoalsAgainst += homeGoals
    hr.GoalDifference = hr.GoalsFor - hr.GoalsAgainst   # recomputed, never accumulated
    ar.GoalDifference = ar.GoalsFor - ar.GoalsAgainst
    if homeGoals > awayGoals:  hr.Won++;  ar.Lost++; hr.Points += WIN_POINTS
    elif homeGoals < awayGoals: ar.Won++; hr.Lost++; ar.Points += WIN_POINTS
    else: hr.Drawn++; ar.Drawn++; hr.Points += DRAW_POINTS; ar.Points += DRAW_POINTS
```

`WIN_POINTS = 3`, `DRAW_POINTS = 1` (`[GT]`, App. A). `GoalDifference` is **recomputed** from GF−GA
each apply (never accumulated) so it cannot drift.

**Tie-break (FR-SN-007), a total order:**

```
Compare(a, b):   # returns a ordered-before b
    by Points   descending
    then GoalDifference descending
    then GoalsFor descending
    then ClubId ascending          # final deterministic tiebreak — clubIds are unique, so never equal
```

`ClubId` ascending as the last key makes the order **total** (no two rows ever compare equal, since
`ClubId` is unique per club — F2 guarantees each club appears once). `OrderedView()` returns a
read-only sorted copy (FR-SN-033 observer-neutrality — sorting a copy never mutates the stored rows).

## 3.3 Calendar cursor & the day-advance tick order (FR-SN-009..012 / KD-2)

```
AdvanceToNextFixtureDay():
    targetDay := Calendar.dayOf(Calendar.NextRoundIndex)
    while WorldStore.CurrentWorldTick < targetDay:       # CurrentWorldTick is uint (WorldStore)
        RunWorldTickInFixedOrder()          # KD-2 — one calendar day
    # cursor is now AT the fixture day; the caller runs AdvanceAndPlayNextRound (§3.4)

RunWorldTickInFixedOrder():                 # the KD-2 choke point — pinned order
    # 1. progression   (#28)  — NULL SEAM today (FR-SN-034)
    # 2. training      (#29)  — NULL SEAM today
    # 3. human-systems (#33)  — NULL SEAM today
    # 4. injuries      (#41)  — NULL SEAM today (ERR-030-002 — after #28/#29 so the injury-risk
    #                           assembly reads the day's updated fatigue/condition; before the world-day tick)
    # 5. transfers     (#31)  — NULL SEAM today (ERR-030-004 — a deep-tier position reservation: minimal
    #                           transfers are command-driven (SubmitBid), so this seam is empty until the
    #                           deep tier's daily in-flight-negotiation / rival-bid processing; positioned
    #                           after the per-player systems and before the world-day tick)
    # 6. staff         (#34)  — NULL SEAM today (ERR-030-006 — a deep-tier position reservation: #34's
    #                           scaffold projections are pull-based (threaded into #29/#41 when their inputs
    #                           are built), so this seam is empty until the deep tier's daily candidate-pool /
    #                           in-flight-hiring processing; positioned after transfers and before the tick)
    # 7. scouting      (#32)  — NULL SEAM today (ERR-030-007 — a deep-tier position reservation: #32's
    #                           minimal tier is the fog-off omniscient identity (no assignment can exist),
    #                           so this seam is empty until the deep tier's daily assignment progress
    #                           (`AdvanceScoutingDay`); positioned after staff so a scouting day reads the
    #                           day's staff state (the ChiefScout doing the scouting), before the tick)
    # 8. world day:     WorldStore.AdvanceDay()   <-- the only LIVE tick
    WorldStore.AdvanceDay()
```

**KD-4 invariant:** `Calendar.dayOf(NextRoundIndex) ≥ WorldStore.CurrentWorldTick` always; a restore
re-checks this and fails loud (F4). The Wave-2+ seams (steps 1–7) are **documented positions**, not
interfaces — #28/#29/#33/#41/#31/#34/#32 each slot into a pre-declared slot when they land, so a wrong order
here would force a re-pin across every Wave-2+ spec (§7). The injuries seam (step 4, appended by ERR-030-002
at #41's approval) is positioned after #28/#29 so its occurrence-risk assembly reads the day's updated
training-fatigue / condition, and before the live world-day tick. The transfers seam (step 5, appended by
ERR-030-004 at #31's approval) is a **deep-tier position reservation** — minimal #31 transfers are
command-driven (`SubmitBid`), so the seam is empty until the deep tier's daily negotiation/rival-bid
processing; it is positioned after the per-player systems and before the world-day tick. The staff seam
(step 6, appended by ERR-030-006 at #34's approval) is likewise a **deep-tier position reservation** — #34's
scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is empty
until the deep tier's daily candidate-pool / in-flight-hiring processing; it too sits after transfers and
before the world-day tick. The scouting seam (step 7, appended by ERR-030-007 at #32's approval) is likewise
a **deep-tier position reservation** — #32's minimal tier is the fog-off omniscient identity (no assignment
can exist, and `AdvanceScoutingDay` no-ops with fog off), so the seam is empty until the deep tier's daily
assignment progress; it sits after staff so a scouting day reads the day's staff state (the ChiefScout doing
the scouting), and before the world-day tick. With only the world-day tick live, a no-fixture day's advance
is **byte-identical** to a bare `WorldStore.AdvanceDay()` (FR-SN-026 / KD-8).

## 3.4 Playing a round (FR-SN-012..013b / KD-9)

A fixture-day resolves the **whole round** — every one of its `N/2` fixtures — and applies **all**
their results to the table. Resolving only a subset would leave the unplayed clubs' rows undefined
(the App. C 4-club round 0 = {10v13, 11v12}; playing only 10v13 never gives 11/12 a round-0 result).
The managed club's fixture runs through the full `MatchEngine`; the rest through the round-resolution
model (§3.4.1).

```
AdvanceAndPlayNextRound(squads: ISquadProvider):
    round := Calendar.NextRoundIndex
    roundFixtures := [ f in Fixtures where f.RoundIndex == round and not f.Played ]
    if roundFixtures is empty: throw          # F5 — season complete; caller runs the boundary roll
    for f in roundFixtures:                    # ALL N/2 fixtures (FR-SN-012)
        if f.HomeClubId == ManagedClubId or f.AwayClubId == ManagedClubId:
            result := PlayThroughEngine(f, squads)       # managed fixture — full MatchEngine
        else:
            result := ResolveRound(f)                    # §3.4.1 — deterministic (FR-SN-013a)
        Table.ApplyResult(result)              # (1) table  — FR-SN-013 order, every fixture
        EmitMatchOutcome(result)               # (2) event  — producer only (KD-3), one per fixture
        f.Played := true
    Calendar.NextRoundIndex := round + 1

PlayThroughEngine(f, squads):
    engine := new MatchEngine(...)             # SeasonLoop._activeMatch — restart-visible for save
    engine.ConfigureSquads(squads.ResolveByClubId(f.HomeClubId),    # F6 fail-loud
                           squads.ResolveByClubId(f.AwayClubId))
    while not engine.MatchEnded: engine.RunTick()   # the 10/60 Hz match loop — off the world tick
    return MatchResult{ f.HomeClubId, f.AwayClubId, engine.HomeScore, engine.AwayScore,
                        f.RoundIndex, WorldStore.CurrentWorldTick }
```

The match runs on the 10 Hz/60 Hz loops (`MatchEngine.RunTick` to `MatchEnded` — the real engine
API), but `AdvanceAndPlayNextRound` is invoked *from* the world-tick loop, so the two clocks stay
disjoint (FR-SN-025). `EmitMatchOutcome` records the event in season state and is producer-only —
#22 ingest activates with #33 (KD-3 / FR-SN-017).

### 3.4.1 Round-resolution model for non-managed fixtures (FR-SN-013a)

The **minimal-first identity** MAY run *every* fixture through the full `MatchEngine` (`ResolveRound`
== `PlayThroughEngine` with a neutral/AI tactic): correct and deterministic, but `N·(N−1)` full
matches per season. The **quick-sim deepening** resolves a non-managed fixture through a deterministic
result model — a scoreline drawn from the `DOMAIN_TAG_SEASON_LOOP` sub-stream (FR-SN-027), keyed on
`(seed, seasonNumber, roundIndex, homeClubId, awayClubId)` so it is replay-stable and independent of
draw order — giving the reserved RNG sub-stream its concrete consumer. Both produce a `MatchResult`
applied to the table identically (FR-SN-012); the choice is a `SeasonState`/config dial, not a
rewrite, and a later spec may upgrade quick-sim to a fuller AI-vs-AI simulation. **Determinism note:**
because the managed fixture consumes the match RNG (its own streams) and non-managed fixtures consume
the season sub-stream by *key* (not by cursor position), the two are order-independent — the same
final table results regardless of the order fixtures within a round are resolved (a §5 lock).

## 3.5 Season-boundary roll (FR-SN-029 / KD-6)

```
RollToNextSeason():
    finalTable := Table.OrderedView()                 # (a) finalize
    Board.Evaluate(finalTable)                         # (b) board pass/fail + job-security
    # (a')  <-- #43 promotion/relegation transform inserts HERE (FR-SN-031), not built now
    # (b')  <-- #40 finance settlement inserts HERE (ERR-030-003) — after (a') so budgets reflect the
    #           post-promotion division; SettleFinances(financeState[club], position, clubCount, board)
    #           per club. NULL SEAM until #40 T2 wires it; #40 references #30 never (one-way #30 → #40).
    nextSeed := DeriveNextSeasonSeed(Seed, SeasonNumber)
    Fixtures := FixtureScheduler.Generate(ClubIds, nextSeed)   # (c) regenerate
    AdvanceAges()                                       # (d) #28 — NULL SEAM today
    Table := LeagueTable.Empty(ClubIds)                # (e) reset
    SeasonNumber++
    Seed := nextSeed
```

Each step mutates a well-defined slice of `SeasonState`; the whole transform is a pure function of
the prior `SeasonState` + `nextSeed`, so a save taken mid-roll restores to the same continuation
(restartable, FR-SN-029). #43's promotion/relegation is a transform inserted at (a'), between
finalize and regenerate, leaving (a)/(b)/(c)/(d)/(e) unchanged (FR-SN-031). #40's finance settlement
(ERR-030-003, at #40's approval) is a NULL SEAM inserted at (b'), after (a') so budgets reflect the
post-promotion division and before (c); it too leaves the surrounding steps unchanged and keeps the
transform a pure function of `SeasonState + nextSeed` (per-club `ClubFinances` prior state carried in).

## 3.6 Season-state sub-blob codec (FR-SN-019..023)

The season block is a pure `CanonicalSerializer` payload, the `WorldStateSerializer` / `MatchSaveCodec`
posture — version gate first, overflow-safe length prefixes, fail-loud on version/prefix/trailing:

```
EncodeSeason(state) -> bytes:
    WriteU32(SEASON_STATE_FORMAT_VERSION)
    WriteU64(state.Seed)
    WriteI32(state.SeasonNumber)
    WriteCount(state.ClubIds.Length); for id in ClubIds: WriteI32(id)
    WriteCount(state.Fixtures.Length); for f in Fixtures: WriteFixture(f)
    WriteCalendar(state.Calendar)
    WriteCount(table rows); for r in Table.rows: WriteTableRow(r)     # per-club, ClubId order
    WriteBoard(state.Board)

DecodeSeason(bytes) -> state:
    version = ReadU32(); if version != SEASON_STATE_FORMAT_VERSION: throw   # F3
    ... symmetric reads, each length via ReadCount (0 <= n <= remaining, overflow-safe) ...
    if bytesRead != bytes.Length: throw   # trailing-byte guard (F3)
    validate Calendar.nextDay >= 0 and internal coherence          # F4 checked at SeasonLoop.Restore
```

`SeasonSaveCodec.Encode`/`Decode` gain the season block between the world and match blocks; the outer
frame becomes `version → matchPresent flag → world block → season block → (match block iff present)`,
and `SEASON_SAVE_FORMAT_VERSION` bumps 1 → 2 (§4). The codec never parses the world or match blob
(each keeps its own version gate) — the season block is the only new thing it reads.

## 3.7 Worked example — 4-club schedule

`clubIds = [10, 11, 12, 13]`, identity permutation. Circle method (M = 4, index 0 fixed):

| Round | Fixtures (home v away) |
|---|---|
| 0 | 10 v 13, 11 v 12 |
| 1 | 10 v 12, 13 v 11 |
| 2 | 10 v 11, 12 v 13 |
| 3 (2nd leg) | 13 v 10, 12 v 11 |
| 4 | 12 v 10, 11 v 13 |
| 5 | 11 v 10, 13 v 12 |

12 fixtures = `N·(N−1) = 4·3` (FR-SN-002); each club appears once per round (FR-SN-003). If clubs 10
and 11 both finish P=3 W=2 D=0 L=1 with GF/GA giving equal GD and equal GF, club 10 orders above 11
by ascending `ClubId` (FR-SN-007 final key) — a total order.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial algorithms: circle-method fixtures, table + tie-break, day-advance order, boundary roll, season codec, worked 4-club schedule. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-07-23 | — | Back-prop ERR-030-002 (at #41 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the injuries null seam as step 4 (after #28/#29, before the world-day tick); prose updated (steps 1–4). |
| 0.4 | 2026-07-23 | — | Back-prop ERR-030-003 (at #40 approval): §3.5 `RollToNextSeason` gains the #40 finance-settlement null seam at (b') (after (a') #43 point, before (c) regenerate); prose updated. |
| 0.5 | 2026-07-23 | — | Back-prop ERR-030-004 (at #31 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the transfers null seam as step 5 (after injuries, before the world-day tick; `AdvanceDay` → step 6); a deep-tier position reservation, empty at minimal. Prose + FR-SN-034 enumeration updated. |
| 0.6 | 2026-07-23 | — | Back-prop ERR-030-006 (at #34 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the staff null seam as step 6 (after transfers, before the world-day tick; `AdvanceDay` → step 7); a deep-tier position reservation, empty at minimal. Prose + FR-SN-034 enumeration updated. |
| 0.7 | 2026-07-24 | — | Back-prop ERR-030-007 (at #32 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the scouting null seam as step 7 (after staff so a scouting day reads the day's staff state, before the world-day tick; `AdvanceDay` → step 8); a deep-tier position reservation, empty at minimal (fog-off ⇒ no assignment; `AdvanceScoutingDay` no-ops). Prose + FR-SN-034 enumeration updated. |
#endregion
