# Season & Competition Loop Specification #30 — Section 3: Algorithms

**Created:** July 22, 2026
**Last Updated:** August 13, 2026, later same day (v2.1 — **ERR-030-037** (M8, adversarial review over
the #44 C1/C2 landing): §3.4's `AdvanceAndPlayNextRound`/`PlayThroughEngine` pseudocode gained the
#44 loop it never had — the fold construction and per-tick pump inside `PlayThroughEngine`, and the
serve+commit pair sequenced AFTER `f.Played := true` (M6's fix) rather than omitted entirely; §3.5's
`RollToNextSeason` pseudocode gained step (f), the FR-DC-017 boundary sweep, installed after (e)'s
commits. An implementer following §3.4/§3.5 verbatim before this pass would have rebuilt the loop
with no discipline wiring at all, missing the one ordering that IS #44's off-by-one contract)
**Last Updated (prior):** August 13, 2026 (v2.0 — ERR-030-035's consumers half, #44 C1/C2 landing: §3.4's
three "#44 … join the same seam at their own T-phases" / "when they join" mentions corrected — #44
suspensions have joined, citing ERR-044-002/ERR-044-003 and the code sites; only #36 remains open)
**Last Updated (prior):** August 8, 2026, later still (v1.9 — ERR-030-030: §3.3 slot 1 and §3.5 step (d) corrected to reflect #28 T2a — the daily step is LIVE, the season-boundary roster mutation remains reserved)
**Last Updated (prior):** August 8, 2026, later same day (v1.8 — ERR-030-029 at balance-pass AR pass 12 M4: the depleted-squad back-fill rule, normative at the seam that owns it)
**Last Updated (prior):** August 8, 2026 (v1.7 — balance-pass AR pass 12 M1: §3.3's two stale prose clauses — "steps 1–7" and the only-world-tick-live byte-identity premise — corrected to the post-T2 loop)
**Last Updated (prior):** August 8, 2026, still later same day (v1.6 — balance-pass AR pass 7 L2: v1.5's pseudocode lines reordered below the F5 guards, matching §3.3.2's after-every-guard property and the code. Prior header below.)
**Last Updated (prior):** August 8, 2026, even later same day (v1.5 — balance-pass AR pass 6 L4: §3.4's pseudocode gains the pre-round `RunCareerDaySteps` line and the clock guard defining `worldDay`. Prior header below.)
**Last Updated (prior):** August 8, 2026, later same day (v1.4 — balance-pass AR pass 5 M2: §3.4 caught up with the loop it describes — the filter seam is LIVE, `PlayThroughEngine` shows the filtered squads + entry fatigue + the XI derivation, and the appearance-record step appears at its load-bearing position. Prior header below.)
**Last Updated (prior):** August 8, 2026 (v1.3 — balance-pass AR pass 4 header-currency fix: this header sat at v1.1 / July 27 while the table below carried v1.2 (Aug 7, ERR-030-027 — §3.3.2 pins the pre-round convention) and v1.3 (Aug 8, slots 2/4 marked LIVE) — two consecutive landings missed the bump, the exact drift class the v1.1 note below records this file fixing in itself.)
**Last Updated (prior):** July 27, 2026, later same day (v1.1 — back-props ERR-030-016/-017/-019/-020/-021/-022/-023/-024/-025 landed atomically with the ten-spec approval wave. **`ERR-030-025` is a REASSIGNMENT: this spec's #46 projector seam was authored as `ERR-030-015`, which #30's own T3 landing (roadmap A5) claimed first on main for the §3.5 calendar-rebuild fix while this branch was open — the id-collision class the wave itself documented, recurring live. Main's claim has precedence; the seam moved to `-025`.** **New §3.3.1 records the tick-order reconciliation**: `ERR-030-007` had been filed twice, leaving two step 7s, two step 8s and an orphaned `AdvanceDay` line, so the pinned order was not implementable as written. Also fixed here: this file carried **two bare `**Last Updated:**` labels** claiming v0.8 and v0.9 with different content — the same header-drift class the project has recorded before, and one that made the file self-contradictory about its own currency.)
**Last Updated (prior):** July 25, 2026 (v0.9 — ERR-030-010 §3.7 venue correction, found at #30 T0; prior v0.8 back-prop ERR-030-009 #44 availability-filter null seam in §3.4; prior v0.7 ERR-030-007, v0.6 ERR-030-006, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Last Updated (prior):** July 25, 2026 (v0.8 — back-props ERR-030-008 board tick-order seam + ERR-030-009 JobSecurity derived band; prior v0.7 ERR-030-007 academy, v0.6 ERR-030-006 staff, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Last Updated (prior):** July 27, 2026 (v1.0 — **ERR-030-015**: §3.5's boundary roll gains step (c′), the calendar rebuild it omitted, without which a rolled season is permanently unplayable; found at #30 T3. Also consolidates the TWO stale `Version` fields this header carried — the drift class `spec-error-log.md` v1.43 records. Prior v0.9 ERR-030-010 §3.7 venue correction; v0.8 back-props ERR-030-008/009; v0.7 ERR-030-007, v0.6 ERR-030-006, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Version:** 2.1
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
    ring := ids                 # the rotating circle; index 0 is the pinned position
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
    # cursor is now AT the fixture day; its OWN day-slots have NOT run — they run pre-round
    # inside AdvanceAndPlayNextRound (§3.3.2 / ERR-030-027); the caller runs it next (§3.4)

RunWorldTickInFixedOrder():                 # the KD-2 choke point — pinned order
    # 0. facilities    (#53)  — NULL SEAM today (ERR-030-020 — AdvanceFacilityDay: upgrade-completion
    #                           latch. Numbered ZERO, not inserted as a new "1": #53 must precede every
    #                           same-day consumer of a facility-derived input (#29 step 2, #41 step 4,
    #                           #42 step 7), and renumbering to achieve that would invalidate the step
    #                           numbers six APPROVED specs and the frozen ERR log cite BY NUMBER. See
    #                           the conflict note below)
    # 1. progression   (#28)  — LIVE (T2a, August 8, 2026: SeasonLoop.RunCareerDaySteps gathers the
    #                           batch through PlayerCareerStates.GatherTrainingInputs and hands it to
    #                           ProgressionEngine.AdvanceDay here, before slot 2 — see §3.3.2)
    # 2. training      (#29)  — LIVE (T2, August 6, 2026: SeasonLoop.RunCareerDaySteps drives
    #                           PlayerCareerStates.AdvanceTrainingDay here — see §3.3.2)
    # 3. human-systems (#33)  — NULL SEAM today
    # 4. injuries      (#41)  — LIVE (T2, August 6, 2026: AdvanceMedicalDay, armed at the balance
    #                           pass — see §3.3.2. ERR-030-002 — after #28/#29 so the injury-risk
    #                           assembly reads the day's updated fatigue/condition; before the world-day tick)
    # 5. transfers     (#31)  — NULL SEAM today (ERR-030-004 — a deep-tier position reservation: minimal
    #                           transfers are command-driven (SubmitBid), so this seam is empty until the
    #                           deep tier's daily in-flight-negotiation / rival-bid processing; positioned
    #                           after the per-player systems and before the world-day tick)
    # 6. staff         (#34)  — NULL SEAM today (ERR-030-006 — a deep-tier position reservation: #34's
    #                           scaffold projections are pull-based (threaded into #29/#41 when their inputs
    #                           are built), so this seam is empty until the deep tier's daily candidate-pool /
    #                           in-flight-hiring processing; positioned after transfers and before the tick)
    # 7. academy       (#42)  — NULL SEAM today (ERR-030-007 — the youth-intake step: a one-shot
    #                           latched on LastIntakeWorldDay, so on all but one day per intake period
    #                           it is two integer comparisons and a return; positioned after staff and
    #                           before the world-day tick)
    # 8. board         (#45)  — NULL SEAM today (ERR-030-008 — the board-confidence day step:
    #                           one integer drift per modelled club, positioned after academy and before
    #                           the world-day tick. Goes live at #45's own T2, like #42's seam)
    # 9. scouting      (#32)  — NULL SEAM today (ERR-030-022 renumbered this from the duplicate "7" the
    #                           ERR-030-007 collision produced. A deep-tier position reservation: #32's
    #                           minimal tier is the fog-off omniscient identity (no assignment can exist),
    #                           so this seam is empty until the deep tier's daily assignment progress
    #                           (`AdvanceScoutingDay`); its own rationale requires only "after staff",
    #                           which 9 satisfies without moving any other slot)
    # 10. media expiry (#35)  — NULL SEAM today (ERR-030-022 — the conference-window / pending-question
    #                           expiry step. After scouting, before the world-day tick)
    # 11. tenure       (#54)  — NULL SEAM today (ERR-030-021 — EvaluateTenure. Positioned after board
    #                           (step 8) because it READS the day's board confidence; the terminating
    #                           decision itself is #54's, not #30's)
    # 12. world day:    WorldStore.AdvanceDay()   <-- the only LIVE tick
    WorldStore.AdvanceDay()
```

**KD-4 invariant:** `Calendar.dayOf(NextRoundIndex) ≥ WorldStore.CurrentWorldTick` always; a restore
re-checks this and fails loud (F4). The Wave-2+ seams (the slots at 0–11) are **documented positions**, not
interfaces — #33/#31/#34/#32/#35/#42/#45/#53/#54 each slot into a pre-declared slot when they land
(#28, #29 and #41 already HAVE — slots 1/2/4, live since #28 T2a and #29/#41 T2, §3.3.2), so a wrong order
here would force a re-pin across every Wave-2+ spec (§7). The injuries seam (step 4, appended by ERR-030-002
at #41's approval) is positioned after #28/#29 so its occurrence-risk assembly reads the day's updated
training-fatigue / condition, and before the live world-day tick. The transfers seam (step 5, appended by
ERR-030-004 at #31's approval) is a **deep-tier position reservation** — minimal #31 transfers are
command-driven (`SubmitBid`), so the seam is empty until the deep tier's daily negotiation/rival-bid
processing; it is positioned after the per-player systems and before the world-day tick. The staff seam
(step 6, appended by ERR-030-006 at #34's approval) is likewise a **deep-tier position reservation** — #34's
scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is empty
until the deep tier's daily candidate-pool / in-flight-hiring processing; it too sits after transfers and
before the world-day tick. The academy seam (step 7, appended by ERR-030-007 at #42's approval) is the
youth-intake step: unlike steps 5–6 it becomes live at #42's own T-phase rather than only at a deep tier,
but it is a **one-shot latched on `LastIntakeWorldDay`**, so on every day but one per intake period it is
two integer comparisons and a return. The board seam (step 8, appended by ERR-030-008 at #45's approval)
is the board-confidence day step — like the academy seam it goes live at #45's own T-phase rather than only
at a deep tier, and it costs one bounded integer drift per **modelled** club (the minimal tier models the
managed club only). A no-fixture day's advance is **byte-identical to the world** of
a bare `WorldStore.AdvanceDay()` (FR-SN-026 / KD-8) — since #29/#41 T2 the career day-steps also run
(slots 2/4), but their state lives in the career sub-blobs, not the world blob, so the world-digest
identity FR-SN-026 pins is unchanged.

### 3.3.1 Tick-order reconciliation (ERR-030-022, July 27, 2026)

**The order above was internally broken before this reconciliation**, and it was broken in the way a pinned
sequence breaks: silently, by two independent back-props claiming the same slot. `ERR-030-007` was filed
**twice** — once for #42's academy step and once for #32's scouting step — so the block carried **two step
7s and two step 8s**, plus an orphaned `AdvanceDay` comment line at 9 followed by a second at 8. A reader
implementing it verbatim could not have produced a defensible order, and every one of the six specs that
cites a step by number was citing into an ambiguous list.

Reconciled here: **#32 scouting moves to step 9** (its own stated rationale asks only for *"after staff"*,
which 9 satisfies), the duplicate `AdvanceDay` line is deleted, **#35 media expiry is appended as 10**,
**#54 tenure as 11**, and `AdvanceDay` becomes **12**. `FR-SN-034`'s enumeration is extended to match.

**The conflict this reconciliation had to resolve, recorded because the resolution is a judgement:**
`ERR-030-020` (#53 facilities) requires its step to precede every same-day consumer of a facility-derived
input — steps 2, 4 and 7 — and says to renumber the steps below it. `ERR-030-022` (#35) requires that the
slots approved specs cite **by number** not move. **Both cannot be satisfied by inserting a new step 1.**
Resolved by numbering the facility step **0**: it precedes every consumer, and steps 1–8 keep the numbers
#41, #31, #34, #42, #45 and #32 already cite — as does the frozen ERR log, whose historical entries cannot
be re-dated. A step numbered 0 is unusual; **a renumber that silently invalidates six approved specs'
citations is worse**, and the alternative — patching all six — would edit approved text to accommodate a
numbering preference rather than a design need.

**Errata recorded against this log's own history** (ERR-030-022, filed with #35): `ERR-030-007` was used
for two different changes (#42's academy step, #32's scouting step) and `ERR-030-009` for two more (#45's
`JobSecurity` band, #44's §3.4 availability filter). Both duplications are preserved as-filed — the
historical entries are frozen records — and are noted here so a reader resolving an id against this
section finds the ambiguity documented rather than discovering it.

### 3.3.2 Where the round sits in the day order (ERR-030-026 / ERR-030-027, August 7, 2026)

The slot list above has **no slot for "play the round"** — a round is resolved by a separate command
(§3.4), not by the day advance — so where a fixture sits relative to the fixture day's own slots had
to be pinned explicitly. ERR-030-026 found it emergent (it fell out of `AdvanceToNextFixtureDay`'s
loop condition, producing play-the-round-then-process-matchday, which ran every injury one matchday
longer than its assigned tier); the convention adopted there was interim, with the resolution
deferred to the #29/#41 balance pass. **The pinned convention (ERR-030-027): the fixture day's own
slots 0–11 run at the top of `AdvanceAndPlayNextRound`, before selection and resolution; step 12
(the world-day tick) still runs on the NEXT advance.** Consequences, in order of intent:

- **Recovery lands before selection.** A player whose #41 recovery expires on matchday is available
  for that round — tiers mean exactly what they say, with no absorbed one-day bias for the balance
  pass to fit constants through.
- **The occurrence draw sits on matchday morning.** A player drawn injured on the fixture day is a
  pre-kickoff training-ground loss, filtered by selection. Match participation reaches the draw
  through the FR-MD-010 appearance window, which by construction never contains the current day —
  a match played on day *d* first feeds the draw on day *d+1*. One day of latency in a multi-day
  rolling window, in exchange for keeping #41's one-atomic-step-per-player-day contract (FR-MD-022)
  untouched.
- **The re-run is a no-op.** Both live steps are idempotent per day via their own cursors (F6), so
  the next advance re-entering the same world day advances nothing twice. `AdvanceAndPlayNextRound`
  runs the slots only after every one of its guards, so a refused call advances no cursor.

## 3.4 Playing a round (FR-SN-012..013b / KD-9)

A fixture-day begins by running the day's own KD-2 slots pre-round (§3.3.2 / ERR-030-027), then
resolves the **whole round** — every one of its `N/2` fixtures — and applies **all**
their results to the table. Resolving only a subset would leave the unplayed clubs' rows undefined
(the App. C 4-club round 0 = {10v13, 11v12}; playing only 10v13 never gives 11/12 a round-0 result).
The managed club's fixture runs through the full `MatchEngine`; the rest through the round-resolution
model (§3.4.1). The resolve→*filter*→configure seam (ERR-030-009; FR-SN-013) is **LIVE**: #41's
FR-MD-023 availability filter has occupied it since the #29/#41 T2 wiring, applied to **both** clubs
of **every** fixture on **both** resolution paths (the engine boot and the quick-sim rating alike) —
not only the managed squad. #44 suspensions **have joined** the same seam (ERR-044-002, ERR-030-035,
C1/C2, August 13, 2026); #36 international call-ups join it at its own T-phase.

**The filter seam admits more than one consumer** (ERR-030-016, filed at #36's approval): #44
suspensions and #36 international call-ups both reduce the available squad at this point — #44's
removal now lives at `src/discipline/Availability.cs`, gathered alongside #41's at #30's own
`src/season-save/AvailabilityComposition.cs`. **They compose
order-independently *because both are removals*** — set intersection commutes — and that is stated here as
a **property to preserve rather than an accident to rely on**: a future non-removal filter, one that adds
or substitutes a player, would need an **explicit order** and cannot simply join the list. The composition
also carries a shared obligation neither filter owns alone: a squad reduced **below a fieldable eleven by
the composition** is a #44/#36/#30 concern at this seam, not either filter's private business.

**The depleted-squad rule (ERR-030-029, at the balance-pass AR pass 12 — settling the obligation above,
which the code had settled unilaterally at #29/#41 T2 while #36 §2 F7 and §5 T-NT-I-005 were still
waiting on it):** when the composed filters leave a club unable to field the formation, the seam
back-fills by **pressing the least-injured players back in one at a time** — ascending remaining
recovery, ties broken by earliest roster position — probing the engine's own selector
(`SquadRating.CanFieldStartingEleven`) after each, so fieldability is asked of the selection rule that
will actually run rather than answered by a second, parallel rule at the seam. Back-filling to a player
COUNT would be wrong: selection refuses a position-incomplete squad outright (KD-L3), so eighteen fit
outfielders with no goalkeeper would stop the season. In the limit the back-fill is the whole squad —
the unfiltered behaviour — so **the composed filter can never leave a club worse off than having no
filter at all**. If even the whole squad cannot field the formation, the seam **fails loud**
(`InvalidOperationException`, §2.3 F9) — that is a roster-integrity bug, not a football outcome. The
rule is #30's because FR-MD-023 puts selection on this side of the seam; #44/#36 contribute removals
only and inherit the rule unchanged when they join — **#44 has (ERR-044-003, C1/C2): its own §2.3 F5
fail-loud was withdrawn in favour of this rule, recording that a suspended player is reinstatable in
extremis under the never-worse-than-unfiltered invariant just stated.** Only #36 remains a future
joiner.

```
AdvanceAndPlayNextRound(squads: ISquadProvider):
    require not Calendar.IsSeasonComplete       # F5 — season complete; caller runs the boundary roll
    round := Calendar.NextRoundIndex
    roundFixtures := [ f in Fixtures where f.RoundIndex == round and not f.Played ]
    if roundFixtures is empty: throw            # F5
    worldDay := WorldStore.CurrentWorldTick
    require worldDay == Calendar.DayOf(round)   # the clock is AT the fixture day (§3.3's advance
                                                # stops there; playing early or late is a caller bug)
    RunCareerDaySteps(worldDay)                 # the fixture day's OWN slots, pre-round — idempotent,
                                                # so the next advance's re-run is a cursor no-op — and
                                                # AFTER every guard above, so a refused call cannot
                                                # advance a cursor (§3.3.2 / ERR-030-027)
    for f in roundFixtures:                    # ALL N/2 fixtures (FR-SN-012)
        if f.HomeClubId == ManagedClubId or f.AwayClubId == ManagedClubId:
            result, homeXi, awayXi, fold := PlayThroughEngine(f, squads)  # managed fixture — full engine
        else:
            result, homeXi, awayXi := ResolveRound(f)                # §3.4.1 — deterministic (FR-SN-013a)
            fold := null                        # quick-sim synthesizes no cards (#44 §1.1/§7.2) —
                                                # ban SERVING below still runs; only card GENERATION
                                                # is engine-only at minimal
        # The fielded XIs come OUT of the resolution itself (ERR-041-010(b), balance-pass AR pass 2:
        # a second selection walk here was an unenforced agreement with the configuration), and the
        # appearance record is written BEFORE the pinned apply/emit/mark sequence — it is the only
        # fallible call placed BEFORE `f.Played := true` in this block (M6, ERR-030-037 — see below).
        # Both clubs are validated before either is written (pair-atomic, AR pass 3).
        RecordFixtureAppearances(f.HomeClubId, homeXi, f.AwayClubId, awayXi, worldDay)
        Table.ApplyResult(result)              # (1) table  — FR-SN-013 order, every fixture
        EmitMatchOutcome(result)               # (2) event  — producer only (KD-3), one per fixture
        # (2a) media conference QUEUE     (#35) — NULL SEAM (ERR-030-023). Empty until #35 T2.
        # (2b) inbox match-item PROJECTOR (#46) — NULL SEAM (ERR-030-025). Empty until #46 T2.
        #      SAME SITE, TWO SEAMS, deliberately: (2a) is a conference queue and (2b) is an item
        #      projector. Sharing one hook would make #46's most basic item type depend on #35 being
        #      approved; two null seams cost nothing and coalesce into one hook if both land.
        f.Played := true
        # #44 T2 (FR-DC-011): one ban-serving decrement per club per PLAYED fixture, on BOTH
        # resolution paths — deliberately NOT gated on a career being wired (a ban is served by the
        # club playing). Placed AFTER `f.Played := true`, deliberately (M6, ERR-030-037):
        # OnClubFixturePlayed and fold.Commit are BOTH fallible under a bound config (a threshold or
        # ban length below its floor throws), and serving+committing is independent of
        # Table.ApplyResult/EmitMatchOutcome/`f.Played := true` — so running them after the fixture is
        # marked played means a throw here cannot leave the fixture UNPLAYED with its bans already
        # served once. Before this fix, that throw let a caller retrying AdvanceAndPlayNextRound
        # replay the SAME fixture (the unplayed-index filter did not exclude it) and serve every
        # outstanding ban in the league a SECOND time, silently.
        OnClubFixturePlayed(f.HomeClubId)
        OnClubFixturePlayed(f.AwayClubId)
        # ...and ONLY THEN this fixture's OWN cards (FR-DC-010, ERR-030-037/#44 §3.3): serving
        # decrements the bans that were outstanding at KICKOFF; the fold adds the ones earned during
        # the fixture just played. Reversing the two would let a player sent off in fixture N serve
        # one match of his ban DURING the match he was dismissed in.
        fold?.Commit(DisciplineRules)
    Calendar.NextRoundIndex := round + 1

PlayThroughEngine(f, squads):
    engine := new MatchEngine(...)             # SeasonLoop._activeMatch — restart-visible for save
    home := SelectAvailable(squads.ResolveByClubId(f.HomeClubId))   # resolve → FILTER (FR-MD-023 +
    away := SelectAvailable(squads.ResolveByClubId(f.AwayClubId))   # FR-DC-010) → configure; F6 fail-loud
    homeXi := StartingElevenPlayerIds(home)    # the ids derived at the configuration site itself,
    awayXi := StartingElevenPlayerIds(away)    # one statement from the ConfigureSquads consuming
                                               # the same squad instances (AR pass 2)
    engine.ConfigureSquads(home, away,
                           MatchEntryFatigue(home), MatchEntryFatigue(away))   # #29 §3.3 projection
    fold := new CardLedgerFold(engine.PlayerIdsByAgentId(), LEAGUE_COMPETITION_KEY)  # #44 T2, FR-DC-002/005
    while not engine.MatchEnded:
        engine.RunTick()                        # the 10/60 Hz match loop — off the world tick
        fold.ObserveTick(tap)                   # pumped INSIDE the tick loop — the tap is scoped to
                                                # the tick just completed (#37 KD-7); a skipped call
                                                # loses those records rather than deferring them
    # fold returns UNCOMMITTED — PlayNextRound sequences the commit AFTER this fixture's own
    # ban-serving decrement (M6, above)
    return MatchResult{ f.HomeClubId, f.AwayClubId, engine.HomeScore, engine.AwayScore,
                        f.RoundIndex, WorldStore.CurrentWorldTick }, homeXi, awayXi, fold
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
    # (b'') <-- #54 EvaluateTenure inserts HERE (ERR-030-021) — after the board's verdict, which it
    #           reads. #30 supplies the seam and the ordering; the TERMINATION DECISION IS #54's.
    #           FR-BD-012 previously named #30 as deciding it; #30 contains no such rule and never did.
    # (a')  <-- #43 promotion/relegation transform inserts HERE (FR-SN-031), not built now
    # (b')  <-- #40 finance settlement inserts HERE (ERR-030-003) — after (a') so budgets reflect the
    #           post-promotion division; SettleFinances(financeState[club], position, clubCount, board)
    #           per club. NULL SEAM until #40 T2 wires it; #40 references #30 never (one-way #30 → #40).
    nextSeed := DeriveNextSeasonSeed(Seed, SeasonNumber)
    Fixtures := FixtureScheduler.Generate(ClubIds, nextSeed)   # (c) regenerate
    Calendar := ShiftForwardOneSeason(Calendar)        # (c′) rebuild — see the correction note
    AdvanceAges()                                       # (d) #28's RunSeasonBoundary — RESERVED still
                                                          #     (the daily step is LIVE at slot 1 since
                                                          #     T2a, §3.3; this boundary call is not)
    Table := LeagueTable.Empty(ClubIds)                # (e) reset
    SeasonNumber++
    Seed := nextSeed
    # (f) #44's season-boundary sweep (FR-DC-017) — LIVE since T2 (C1/C2, August 13, 2026):
    # DisciplineRules.RollToNextSeason() resets every yellow count to 0; UNSERVED BANS CARRY
    # UNCHANGED — a red card in the final round is still a ban in August, which is the whole reason
    # #44 persists rather than recomputing from ledgers it does not keep (KD-1). Installed LAST,
    # after (e)'s commits, for the same reason the roster sync at (d′) is: a discipline state swept
    # for a season that never began is a half-rolled state, and the sweep is NOT idempotent — running
    # it twice on a refused-then-retried roll would silently forgive a second season's yellows.
    DisciplineRules?.RollToNextSeason()
```

**Step (d) is only partly landed (ERR-030-030).** #28's daily step — derived age, the deterministic
weighted growth spend, and hard retirement FLAGGING at `RETIREMENT_AGE` — has been LIVE since T2a at
KD-2 slot 1 (§3.3), driven every world day, not at the season boundary. What (d) invokes is different:
`RunSeasonBoundary`, the roster-MUTATION half — removing flagged retirees and inserting their 1:1
regen replacements via `RetirementResult`/`RegenResult` (FR-PG-015). That call is deliberately **not**
part of the T2a landing (#28 §3.4/§7) and stays reserved here until it lands; `AdvanceAges()` at (d)
is a placeholder name for a step whose real shape #28 has not yet specified either.

Each step mutates a well-defined slice of `SeasonState`; the whole transform is a pure function of
the prior `SeasonState` + `nextSeed`, so a save taken mid-roll restores to the same continuation
(restartable, FR-SN-029). #43's promotion/relegation is a transform inserted at (a'), between
finalize and regenerate, leaving (a)/(b)/(c)/(c′)/(d)/(e) unchanged (FR-SN-031). #40's finance settlement
(ERR-030-003, at #40's approval) is a NULL SEAM inserted at (b'), after (a') so budgets reflect the
post-promotion division and before (c); it too leaves the surrounding steps unchanged and keeps the
transform a pure function of `SeasonState + nextSeed` (per-club `ClubFinances` prior state carried in).

**Correction note — step (c′) (ERR-030-015, filed at T3 implementation).** Versions of this block before
v0.5 regenerated `Fixtures` but never touched `Calendar`, whose cursor sits at `RoundCount` (season
complete) precisely because the season just ended. Implemented verbatim that produces a season that is
**permanently unplayable**: `IsSeasonComplete` stays true, so `AdvanceToNextFixtureDay` throws F5 and
`AdvanceAndPlayNextRound` throws, on every call for the rest of the career — the transform could not
deliver FR-SN-029's multi-season continuity at all, and no assertion over the rolled state's *fields*
would notice, since schedule, table, seed and season number are all exactly right.
`ShiftForwardOneSeason` shifts the existing round→day mapping forward by one season length plus a
`[GT] SeasonBreakDays` close season and returns the cursor to round 0, so the new season opens exactly
one break after the old one's finale. Shifting the mapping rather than rebuilding a linear calendar is
what keeps the transform pure (a clock-derived first day would make the roll depend on when the client
happened to call it) and preserves a non-uniform schedule — a calendar with a mid-season gap keeps that
gap next season instead of being flattened. The step sits after (c) so a future competition set that
changes the round count regenerates the schedule first; it does not disturb (a')/(b').

**Boundary condition on (c′).** Because the derived calendar is a function of the old one alone, a
client that advanced the world deep into the close season before rolling would install a schedule
opening in the past — a KD-4 / FR-SN-011 cursor-invariant violation. The roll refuses that fail-loud
rather than installing it, and performs no write until every step is computed and validated, so a
refused roll leaves the season untouched rather than carrying a committed board verdict against a
schedule that was then rejected.

## 3.6 Season-state sub-blob codec (FR-SN-019..023)

The season block is a pure `CanonicalSerializer` payload, the `WorldStateSerializer` / `MatchSaveCodec`
posture — version gate first, overflow-safe length prefixes, fail-loud on version/prefix/trailing:

```
EncodeSeason(state) -> bytes:
    WriteU32(SEASON_STATE_FORMAT_VERSION)
    WriteU64(state.Seed)
    WriteI32(state.SeasonNumber)
    WriteI32(state.ManagedClubId)                                    # Appendix B row 3a (ERR-030-011)
    WriteCount(state.ClubIds.Length); for id in ClubIds: WriteI32(id)
    WriteCount(state.Fixtures.Length); for f in Fixtures: WriteFixture(f)
    WriteCalendar(state.Calendar)
    WriteCount(table rows); for r in Table.rows: WriteTableRow(r)     # per-club, ClubId order
    WriteBoard(state.Board)     # ERR-030-009: at #45 T2 `JobSecurity` is written as a DERIVED BAND
                                #  (a u8 enum over #45's per-mille confidence), not an independent
                                #  scalar -- see the note below. That is a SEASON_STATE_FORMAT_VERSION
                                #  bump, landing with the effect at #45 T2, not with this spec text.

DecodeSeason(bytes) -> state:
    version = ReadU32(); if version != SEASON_STATE_FORMAT_VERSION: throw   # F3
    ... symmetric reads, each length via ReadCount (0 <= n <= remaining, overflow-safe) ...
    if bytesRead != bytes.Length: throw   # trailing-byte guard (F3)
    validate Calendar.nextDay >= 0 and internal coherence          # F4 checked at SeasonLoop.Restore
```

**`JobSecurity` after #45 (ERR-030-009).** Board & Ownership #45 owns a persistent per-club
board-confidence scalar. Keeping an independent `JobSecurity` scalar here alongside it would be **two
truths for one quantity** — they would diverge at the first restore with nothing to detect it — so at
**#45 T2** `BoardState.JobSecurity` stops being independent state and becomes a **derived band**
(`JobSecurityBand`, a `u8` enum) projected on read from #45's confidence. #30 keeps sole ownership of
`BoardObjective` and of the season-boundary pass/fail evaluation; only the *job-security* half moves to a
projection. Two consequences, both deliberate: the season block loses its last `float` (every other
management-layer spec — #28/#33/#40/#41/#42/#45 — is integer-only by requirement), and the representation
change is a **`SEASON_STATE_FORMAT_VERSION` bump**, so pre-T2 saves are rejected fail-loud with **no
migration** (cross-version migration is #50's subject). The bump lands with the *effect* at #45 T2.

`SeasonSaveCodec.Encode`/`Decode` gain the season block between the world and match blocks; the outer
frame becomes `version → matchPresent flag → world block → season block → (match block iff present)`,
and `SEASON_SAVE_FORMAT_VERSION` bumps 1 → 2 (§4). The codec never parses the world or match blob
(each keeps its own version gate) — the season block is the only new thing it reads.

## 3.7 Worked example — 4-club schedule

`clubIds = [10, 11, 12, 13]`, identity permutation. Circle method (M = 4, index 0 fixed):

| Round | Fixtures (home v away) |
|---|---|
| 0 | 10 v 13, 11 v 12 |
| 1 | 12 v 10, 11 v 13 |
| 2 | 10 v 11, 12 v 13 |
| 3 (2nd leg) | 13 v 10, 12 v 11 |
| 4 | 10 v 12, 13 v 11 |
| 5 | 11 v 10, 13 v 12 |

> **Corrected at #30 T0 (ERR-030-010).** Rounds 1 and 4 had their venues inverted — this table was
> hand-derived without applying the §3.1 round-parity rule two subsections above it. §3.1's
> pseudocode is authoritative and unchanged; see Appendix C for the measured justification.

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
| 0.7 | 2026-07-24 | — | Back-prop ERR-030-007 (at #42 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the academy null seam as step 7 (after staff, before the world-day tick; `AdvanceDay` → step 8); prose records that this seam goes live at #42's own T-phase but is a latched one-shot, so all but one day per intake period costs two comparisons. |
| 0.8 | 2026-07-25 | — | Back-props ERR-030-008 + ERR-030-009 (at #45 approval): §3.3 tick order gains the **board null seam as step 8** (`AdvanceDay` → step 9; prose + "documented positions" extended to steps 1–8 / #45); §3.6 records that at #45 T2 `JobSecurity` is serialized as a **derived band** over #45's confidence rather than an independent scalar — removing the season block's last float and carrying a `SEASON_STATE_FORMAT_VERSION` bump with no migration path. |
| 0.7 | 2026-07-24 | — | Back-prop ERR-030-007 (at #32 approval): §3.3 `RunWorldTickInFixedOrder` tick order gains the scouting null seam as step 7 (after staff so a scouting day reads the day's staff state, before the world-day tick; `AdvanceDay` → step 8); a deep-tier position reservation, empty at minimal (fog-off ⇒ no assignment; `AdvanceScoutingDay` no-ops). Prose + FR-SN-034 enumeration updated. |
| 0.8 | 2026-07-24 | — | Back-prop ERR-030-009 (at #44 approval): §3.4 notes the #44 availability-filter null seam on the managed squad's resolve→configure path (empty until #44 T2; FR-SN-013). |
| 0.9 | 2026-07-25 | — | **ERR-030-010** (a) §3.1 pseudocode binds `ring := ids` (it was used but never defined); (b) (found at #30 T0 implementation): the §3.7 worked schedule's rounds 1 and 4 venue-corrected to agree with §3.1's round-parity rule (which is authoritative and unchanged). |
| 1.0 | 2026-07-27 | — | **ERR-030-015** (found at #30 T3 implementation / roadmap A5): §3.5's `RollToNextSeason` gains step **(c′) rebuild the calendar**. The prior block regenerated `Fixtures` but left `Calendar`'s cursor at `RoundCount`, so a season rolled from it was permanently unplayable — `AdvanceToNextFixtureDay` and `AdvanceAndPlayNextRound` both throw for the rest of the career, and the transform could not deliver FR-SN-029's multi-season continuity at all. Correction note + boundary-condition note added; (a')/(b') insertion points and every surrounding step unchanged. Also consolidated the two stale `Version` header fields. |
| 1.1 | 2026-07-27 | — | **Nine back-props landed atomically with the ten-spec approval wave.** (Authored as `-015`..`-024`; **`-015` was reassigned to `-025`** because #30's own T3 landing claimed `-015` on main first — see the header.) **ERR-030-022** (#35) — new **§3.3.1 tick-order reconciliation**: `ERR-030-007` was filed twice (#42 academy, #32 scouting), so §3.3 carried **two step 7s and two step 8s** plus an orphaned `AdvanceDay` comment; #32 → step 9, #35 media expiry → 10, `AdvanceDay` → 12, duplicate line deleted. **ERR-030-020** (#53) — the facilities seam at **step 0**, numbered zero rather than inserted as a new 1 because it must precede its same-day consumers *and* the six approved specs citing steps 1–8 by number must not be invalidated; §3.3.1 records the conflict and the judgement. **ERR-030-021** (#54) — the tenure seam at step 11 (after board, which it reads) and the `(b'')` boundary insertion point in §3.5; the terminating decision is #54's, not #30's. **ERR-030-023** (#35) + **ERR-030-025** (#46) — the conference-queue and match-item-projector null seams at §3.4's `EmitMatchOutcome` site, deliberately **two seams at one site** so #46's basic item type does not depend on #35 being approved. **ERR-030-024** (#46) — the drain generalized to sum across every external-delta producer. **ERR-030-016** (#36) — §3.4's resolve→filter→configure seam records that it admits multiple consumers, that the current pair composes order-independently **because both are removals**, and that a non-removal filter would need an explicit order. **ERR-030-017** (#47) + **ERR-030-019** (#50) — the outer-frame amendments are recorded in Appendix B. **Also fixed:** the file's duplicate `**Last Updated:**` headers. **Not touched:** the duplicate v0.7/v0.8 history rows below — frozen records, noted as errata in §3.3.1 rather than rewritten. |
| 1.2 | 2026-08-07 | — | **ERR-030-027** (the #29/#41 balance pass, closing the half of ERR-030-026 deferred to it): new **§3.3.2** pins where the round sits in the day order — the fixture day's own slots run at the top of `AdvanceAndPlayNextRound`, pre-round, so recovery lands before selection (tiers mean what they say) and the occurrence draw sits on matchday morning, fed by the FR-MD-010 appearance window (which never contains today). §3.3 pseudocode comment + §3.4 opening amended. #41's FR-MD-022 one-step contract untouched — this is a #30 wiring pin, chosen over splitting #41's step and bumping the medical format. |
| 1.3 | 2026-08-08 | — | **Balance-pass AR pass 3 (L1)**: §3.3's slot list still marked slots 2 (#29) and 4 (#41) "NULL SEAM today" while §3.3.2 — added in the same v1.2 amendment — reasons entirely from their being live; both now marked LIVE (T2), citing §3.3.2. Doc-only. |
| 1.4 | 2026-08-08 | — | **Balance-pass AR pass 5 (M2)**: §3.4 caught up with the loop it describes — the "null seam, empty until #44 T2" sentence retired (the ERR-030-009 seam has been LIVE via #41 FR-MD-023 since T2, both clubs, both paths); `PlayThroughEngine`'s pseudocode gains the filter + the #29 entry-fatigue projection + the XI derivation at the configuration site; `AdvanceAndPlayNextRound` gains the `RecordFixtureAppearances` step at its load-bearing position (before apply/emit/mark, pair-atomic). §3.4 had not been touched since v0.8 while three landings changed the code it specifies. |
| 1.5 | 2026-08-08 | — | **Balance-pass AR pass 6 (L4)**: §3.4's pseudocode gains the two lines its own prose and §3.3.2 make load-bearing — the pre-round `RunCareerDaySteps(worldDay)` call (ERR-030-027) and the clock-at-fixture-day guard that also defines the `worldDay` the block used undefined. |
| 1.6 | 2026-08-08 | — | **Balance-pass AR pass 7 (L2)**: v1.5's own new lines put `RunCareerDaySteps` ABOVE the F5 guards, contradicting §3.3.2's after-every-guard property two sections up and the code it specifies; reordered, with the season-complete refusal the code performs first added (it also makes `Calendar.DayOf(round)` well-defined). (Rows 1.4-1.6 were prepended descending and reordered ascending at AR pass 8 — the defect this file's own v1.1 note records fixing in itself.) |
| 1.7 | 2026-08-08 | — | **Balance-pass AR pass 12 (M1)**: the pass-3 slot-list correction had stopped above §3.3's prose — "(steps 1–7)" predated ERR-030-022's 0–11 numbering, the "when they land" list still counted #29/#41 as future, and the FR-SN-026 premise clause ("with only the world-day tick live") had been false since T2; all three corrected (byte-identity qualified to the WORLD blob — the career sub-blobs carry the day-steps' state). Note: the v0.8 row below claims the seam clause was extended to "steps 1–8" — the file read "1–7" at this correction, so that claim was inaccurate or the edit was later reverted; recorded here rather than silently rewritten. |
| 1.8 | 2026-08-08 | — | **ERR-030-029 (balance-pass AR pass 12, M4)**: the depleted-squad back-fill rule — press the least-injured back in until the engine's own selector can field the formation; in the limit the unfiltered squad, so the filter never leaves a club worse off; terminal refusal fails loud (F9) — had existed in NO spec while `PlayerCareerStates.SelectAvailable` implemented it and #36 §2 F7 / §5 T-NT-I-005 recorded the obligation as OPEN. §3.4 now owns it; the ERR-030-028 class (a shipped behaviour specified nowhere), on a behavioural rule rather than a byte layout. |
| 1.9 | 2026-08-08 | — | **ERR-030-030** (found at #28 T2a implementation): §3.3's slot 1 comment corrected from "NULL SEAM today" to LIVE — `RunCareerDaySteps` gathers the batch through `PlayerCareerStates.GatherTrainingInputs` and hands it to `ProgressionEngine.AdvanceDay` at slot 1, ahead of #29's slot 2 — and the surrounding prose updated to count #28 among the landed seams. §3.5 step (d)'s `AdvanceAges()` comment corrected: the daily half (age derivation, growth, retirement flagging) is LIVE at slot 1, but the step (d) call itself is `RunSeasonBoundary` — the roster-mutation half (retiree removal + regen) — which #28 T2a deliberately does not land, so (d) stays RESERVED, now stated as such rather than a flat "NULL SEAM". Recorded as the identical stale-seam-text class corrected for #29/#41 at balance-pass AR passes 11/12, recurring on the next subsystem to wire. |
| 2.0 | 2026-08-13 | — | **ERR-030-035, consumers half** (#44 C1/C2 landing): §3.4 stopped one landing short of the byte-layout half ERR-030-035 already fixed in Appendix A/B and §4/§2 (FR-SN-021) — three sentences still read "#44 … join the same seam at their own T-phases" / "when they join" as future tense after #44 had actually joined. Corrected to say #44's suspension view has joined (citing ERR-044-002 for the filter re-scope and ERR-044-003 for the depleted-squad rule #44 now inherits), with only #36 left as a future joiner; the composition-site sentence now names `src/discipline/Availability.cs` and `src/season-save/AvailabilityComposition.cs`. |
| 2.1 | 2026-08-13 | — | **ERR-030-037** (M8, adversarial review over the C1/C2 landing): §3.4's pseudocode gains the #44 loop it never had — `PlayThroughEngine` gains the `CardLedgerFold` construction and per-tick `ObserveTick` pump (`fold.ObserveTick(tap)` inside the tick loop), returns `fold` UNCOMMITTED, and `AdvanceAndPlayNextRound` gains the `OnClubFixturePlayed`×2 + `fold?.Commit(...)` pair, sequenced AFTER `f.Played := true` per M6's fix (`SeasonLoop.cs` v1.22) rather than before it as the code read pre-fix. §3.5's `RollToNextSeason` pseudocode gains step (f), `DisciplineRules?.RollToNextSeason()`, installed after (e)'s commits. Locked in code by `SeasonLoopDisciplineTests.ANewBanEarnedThisFixtureIsNotServedByThisSameFixture`. |
#endregion
