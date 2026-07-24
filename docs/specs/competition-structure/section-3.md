# Competition Structure #43 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 3.1 The registry & formats (FR-CP-001..006)

The `CompetitionSet` is a canonically-ordered collection of instances. Instance 0 is the #30 league
**binding** (no data); every other instance owns its entrant set + per-format state and drives
#30's pure machinery:

- **RoundRobin** — `Fixtures := FixtureScheduler.Generate(entrants, instanceSeed)`;
  `Table := LeagueTable.Empty(entrants)`; results apply via the same `ApplyResult` discipline.
  `instanceSeed := DeriveInstanceSeed(worldSeed, competitionId, seasonNumber)` (a pure derivation —
  distinct instances never share a fixture sequence).
- **Knockout** — rounds are drawn (§3.2) and persisted (§3.3); fixtures for a round are the drawn
  pairings.
- **GroupThenKnockout** — a keyed group-assignment draw (`GroupAssign` purpose) partitions the
  canonical entrant list into groups; each group is a RoundRobin sub-instance; group winners feed a
  knockout `BracketState`.

## 3.2 Keyed draws (deep — FR-CP-007/008/009, the #41 §3.1.1 / #32 §3.3 mechanism)

```
DeriveDrawOrdinal(seasonNumber, roundIndex, slotIndex, purpose) -> u64:
    assert 0 <= roundIndex < CP_ROUND_RADIX                # bound guards (F5)
    assert 0 <= slotIndex  < CP_SLOT_RADIX
    assert 0 <= purpose    < CP_PURPOSE_RADIX
    return (((u64)seasonNumber * CP_ROUND_RADIX + roundIndex) * CP_SLOT_RADIX + slotIndex)
           * CP_PURPOSE_RADIX + purpose

DrawRound(competitionId, seasonNumber, roundIndex, canonicalEntrants) -> int[]:   # FR-CP-009
    drawn := copy(canonicalEntrants)                       # ascending ClubId base (FR-CP-005)
    n := drawn.length
    for i in 0 .. n-2:                                     # keyed Fisher–Yates
        r := KeyedDraw(competition.draws, competitionId,
                       DeriveDrawOrdinal(seasonNumber, roundIndex, i, Pairing)) mod (n - i)
        swap(drawn[i], drawn[i + r])
    return drawn                                           # pairings: [0]v[1], [2]v[3], ...
```

Position-independent: same `(competitionId, seasonNumber, roundIndex, slotIndex, purpose)` ⇒ same
draw across call orders, days, and save/restore; distinct `competitionId`s (`entityId`) make
concurrent competitions mutually independent (FR-CP-024). Radices are fixed constants; purposes
APPEND-only. **No cursor exists** — nothing to serialize, nothing to race.

**Worked example.** Cup `competitionId = 5`, season 0, round 0, canonical entrants
`[3, 7, 12, 20]` (`n = 4`): `i = 0` keyed draw mod 4 → say `2` ⇒ swap idx 0↔2 → `[12, 7, 3, 20]`;
`i = 1` draw mod 3 → say `0` ⇒ no swap; `i = 2` draw mod 2 → say `1` ⇒ swap idx 2↔3 →
`[12, 7, 20, 3]`. Pairings: **12 v 7, 20 v 3**. Re-running in any order reproduces it exactly;
shuffling the input first (then canonicalizing) reproduces it too (the FR-CP-005 lock).

## 3.3 Bracket lifecycle (deep — FR-CP-010/011)

```
ResolveKnockoutRound(comp):
    require prior round fully resolved                     # F2
    if comp.Bracket.CurrentRoundEntrants is not drawn:
        entrants := canonical(currentRoundParticipants)    # round 0: the entrant set; else prior winners
        comp.Bracket.Rounds += { Entrants = DrawRound(...), Winners = [] }    # persisted (FR-CP-010)
    # fixtures = the drawn pairings; each resolves via the #30 resolution paths (managed club ->
    # MatchEngine, others -> the FR-SN-013a model); winners recorded per pairing.
```

Coherence gates (F4, enforced at decode **and** at each mutation): `Winners[k]` ∈
`{Entrants[2k], Entrants[2k+1]}`; `|Rounds[r+1].Entrants| == |Rounds[r].Entrants| / 2`; round-0
entrant multiset == the competition's entrant set. A restored bracket is authoritative — **no draw
re-rolls on load** (FR-CP-025); the keyed mechanism makes a test-side re-derivation cross-check
possible (T-CP-DET-004).

## 3.4 Promotion/relegation (deep — FR-CP-015..018, at FR-SN-031's (a'))

```
ApplyPromotionRelegation(divisions /* ordered chain, final tables */, membership):
    for d in 0 .. divisions.length - 2:
        down := bottom RELEGATION_COUNT of divisions[d].Table.OrderedView()    # FR-SN-007 total order
        up   := top    PROMOTION_COUNT  of divisions[d+1].Table.OrderedView()
        require RELEGATION_COUNT == PROMOTION_COUNT                            # squad-size-preserving swap
        membership.Swap(down, up)                          # membership ONLY — ClubIds stable (FR-CP-016)
    # applied to every division instance's entrant set — incl. instance 0's SeasonState.ClubIds via
    # #30's command API (the T-phase ERR-030-008 hook) — BEFORE roll step (c) regenerates (FR-CP-017).
```

Pure and draw-free; a no-op with one division (FR-CP-018). **Worked example:** division 1 (12
clubs), division 2 (12 clubs), counts = 3: final div-1 positions 10/11/12 (say clubs 8, 2, 19)
swap with final div-2 positions 1/2/3 (say clubs 30, 27, 41). Next season div 1 = {…, 30, 27, 41},
div 2 = {…, 8, 2, 19}; every club keeps its `ClubId`, squad, finances, and knowledge overlay
untouched. Deterministic: same standings ⇒ same swap, always.

## 3.5 The merged fixture-day view (deep — FR-CP-019)

```
MergedNextFixtureDay(instances):                           # pure over the per-instance mappings
    candidates := union of each instance's next unresolved round-day
    # deterministic slotting (config-derived, KD-5): cup rounds are ASSIGNED days, at scheduling
    # time, only from days on which none of their entrants has a league fixture; assignment is a
    # pure function of (league calendar, cup round count, [GT] spacing) — no search at query time.
    return min(candidates)                                 # with each day's fixture list
```

Invariants: one fixture per club per day (the FR-SN-003 rule lifted to the collection); #30's
`SeasonCalendar` is never modified (the league instance's mapping is read as-is). The root queries
this view only when `instances.length > 1` — the minimal path never reaches it.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §3 (registry/formats + instance seeds, keyed Fisher–Yates draw + worked example, bracket lifecycle + coherence gates, promotion/relegation + worked example, merged fixture-day view), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
