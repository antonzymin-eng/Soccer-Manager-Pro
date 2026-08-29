---
name: match-realism-pass
description: >-
  Run a match-engine realism pass end to end — gate on whether the whole chain under the target is
  actually wired, measure a football-plausibility gap with an env-gated instrument, localize the
  defect, file the ERR and patch spec + code together, calibrate the [GT] dials over a ladder once
  the gate has passed, re-measure pre/post on identical seeds, and lock the result with a
  ScenarioRunner acceptance scenario proven to fail at the pre-fix commit. Use this skill whenever work touches how
  football-plausible the simulation is: goal rate, shots, shot speed or distance, save/contact rate,
  fouls or cards, possession churn, scorelines, "the engine produces too many / too few X", any §5.Z
  numbered pass, any "measure the lever" or "calibrate" request against the match engine, and any
  follow-up on a recorded-not-fixed residual from a previous pass. Trigger it even when the user only
  names the symptom ("goals are too high", "keepers never save") without asking for a measurement
  pass — the wiring gate and the measure-first discipline are the whole point, and under this repo's
  wire-first posture a target that looks like a mis-set dial is often a stage that was never wired.
---

# Match Realism Pass

This repo has run this pass eight times in nine days (§5.Z.17 shot outcomes → .18 → .19 shot speed and
woodwork → .20 keeper conversion → .21 shot volume → .22 keeper contact → .23 conversion at contact →
.24 close-chance creation — July 27 to August 4). Every one had the same shape, and **seven of the
eight produced a finding that the brief they started from was partly wrong** — §5.Z.24 is the first
whose premise survived its own check, and it survived because the pass re-measured it rather than
assuming it. That is not bad luck, it is the method working. The discipline below exists so the pass
measures before it believes, and so the result is attributable to the thing you changed.

Twice, that partly-wrong brief was wrong in the same specific way: it arrived asking for a *quality*
and the quality turned out to be **undefined**, because a stage of the chain was missing.
**§5.Z.17** was briefed as "the quality of the save, not its existence" and measured zero hand
contacts across six keeper-matches — one of its three causes was `OnShotExecutedEvent` with zero
callers anywhere in the tree. **§5.Z.23** arrived on §5.Z.22's premise that the keeper's contacts
were marginal touches, and found #11's catch branch coded to one of its two spec statements, so a
claimed ball flew on into the net. Neither was a calibration problem, and a ladder run on either
would have fitted a dial to a gap. **That is why this skill now opens with a wiring gate rather than
with a measurement.**

The gate is a filter, not a verdict on calibration. §5.Z.20 is the standing counterexample and it is
the largest single movement this chain has measured: a `[GT]` recalibration inside #11's own spec
ranges took goals per match **14.7 → 8.0**. It fixed two timing defects in the same pass — so the
gate would have had work to do there too — and its owner document is explicit that those fixes alone
were not enough, which is the point: the dial was load-bearing *independently* of the wiring. The
gate exists to stop a *premature* ladder, not the ladder.

Owner document: `docs/tracking/match-engine-design.md` (the §5.Z section chain). Each pass also gets
its own supplement at `docs/tracking/<topic>-design.md`.

## 0. The wiring gate — run this before anything else

**Is every stage between the dial you would turn and the outcome you are measuring built,
constructed, driven each tick, read by someone, and implemented to the whole of its spec text? If any
one of them is not, the calibration ladder in §3 is premature.** What that makes the pass instead
depends on which check failed, and the three routes are different work: **check 1** hands off to a
spec's T0 landing and ends this skill's involvement; **check 5** measures, records, and calibrates
nothing; **checks 2–4 and 6** are a wiring task, which is the branch the rest of §0 describes.

This gate is first because the project's position makes that failure routine rather than rare. **19
of 53 APPROVED specs have no `src/` assembly at all** (re-derived August 18, 2026 against the root
`CLAUDE.md` enumerated list), and several that do exist are wired only
partially — T0 cores with no engine consumer, orchestrators behind opt-in flags, branches implemented
to half their pseudocode. "The spec is APPROVED" says nothing whatsoever about whether code runs.

### Start at the wiring backlog

`docs/tracking/match-engine-wiring-backlog.md` is the standing inventory of built-but-unwired
surfaces, produced by three systematic sweeps (comment sweep, whole-tree production-caller count over
every `public` method, manual triage) across the 18 assemblies the engine composes. It carries **10
Class-A dormant capabilities**, and the two largest were invisible to the project's own tracking
until that audit ran: **W1 — the keeper never comes off his line** (`CommitRushIntent` has no
production caller, though everything downstream of it works, so every 1v1 is a stationary keeper
waiting to dive) and **W2 — no player has ever made a tackle** (three dormant links in one chain, and
no comment anywhere recorded it).

If your target touches an entry on that board, the board has already done §0's work and named the
fix. It is a **floor, not a ceiling** — §1.1 says so itself — so a clean board does not end the gate,
but re-deriving what it already contains is wasted effort.

### First, enumerate the chain

The checks below are only as good as the list you run them against, and building that list is
the hard part — nobody had "the catch parks the ball" on a stage list until §5.Z.23's instrument
followed the ball *after* the contact. So write the chain out explicitly, **from the observable
backwards to the dial**, sourced from the owning spec's §3 pipeline rather than from memory: for the
keeper that is `threat armed → SAVE committed → dive launched → airborne → hand contact → band
resolved → ball state written → restart adjudicated`. Name every stage, including the ones that are
a single statement.

If you cannot write that list from source, you do not yet know what to gate. Build §1's funnel
instrument first — a funnel is a chain enumeration you can measure — and resume the gate with its
stages.

### Then the six checks

Checks 1–5 are cheap source reads; check 6 needs a run. **Run all six and report every failure.**
Do not stop at the first — this chain has produced multi-gap passes more than once: §5.Z.15 found
#11 switched off **and** keepers skipped by the physics phase, and §5.Z.17 found three independently sufficient defects
(ERR-011-002/003/004). Stopping at the first means flipping a flag and then measuring a keeper that
still cannot move.

1. **Does an assembly exist at all?** `ls -d src/*/` is ground truth; the assembly map in the root
   `CLAUDE.md` is the annotated index — not `SPEC_INDEX.md`, which records approval, not code, and not
   any spec list restated in this file, which goes stale the moment the next T0 lands (this file once
   listed #41 Injuries and #29 Training as assembly-less; both landed August 5, 2026). Two shapes to
   check for: a spec with **no assembly at all**, and one whose assembly is **T0-only and not
   engine-wired** — #37 Match Analytics is the standing example (no sim assembly may reference it, by
   design). **If the brief names either shape** — "implement discipline", "wire up injuries" — the
   deliverable is that spec's T0 landing (or its engine wiring) off `path-to-playable-roadmap.md`, and
   this skill is the wrong one. A brief that names a *symptom* is check 5, not this one.
2. **Is it constructed at the composition root, and does a phase actually reach it?** Grep
   `src/match-engine/MatchEngine.cs` for the type *and* for its flag. Both halves have failed here in
   the same pass: #11 was constructed, snapshot-safe and `EnableGkHeading`-gated **default false**
   until §5.Z.15 flipped it, and in that same pass keepers turned out to be skipped by
   `RunPhysicsPhase`, so boot placement was the keeper's position for ninety minutes. A subsystem the
   tick pipeline never reaches is not wired, however completely it is built.
   **Then check the flag inside your own instrument.** `DisableGkHeading()` exists for the tests that
   want the old path and is called in five places across `MatchEngineGkHeadingTests.cs` and
   `MatchEngineGkHeadingScenarios.cs`. §1 tells you to copy an exemplar's shape; copy its `Disable*`
   setup by accident and the instrument measures a switched-off subsystem and reports the zero as
   engine behaviour. Assert the flag state in the fixture rather than assuming it.
3. **Does the output have a live consumer?** Check the **read** side, never the write side.
   `OnShotExecutedEvent` had zero callers anywhere in the tree, which made a catch arithmetically
   impossible. A value that is computed and dropped measures as an absence, and an absence read as a
   low number is what sends a pass to the ladder.
4. **Is the branch implemented to the whole of its spec text?** Diff the production path against the
   spec's §3 pseudocode **body**, not its Outputs summary. ERR-011-008: #11's catch is two statements
   — `SetPossessor` **and** park the ball — and only the first was coded, so a claimed shot flew on
   into the net with the keeper recorded as holding it. The §3.5 Outputs summary named one statement;
   `IGoalkeeperBallSystem` exposed no seam for the other, so the omission was invisible from the
   interface as well as from the summary. Half a branch reads exactly like a badly-tuned one.
5. **Is the thing you would tune a Stage-0 placeholder standing in for an unimplemented spec?** This
   is the check for a brief that names a symptom rather than a spec. The foul/card heuristic issues
   ~7 red cards per 9 minutes of played football, and **#44 Discipline has no assembly** — so the
   heuristic is live, the symptom is real, and sweeping its `[GT]`s fits the dials to a stand-in that
   #44 will delete. Measure it, record the number against the owning `open-issues.md` entry and the
   roadmap item that will replace the placeholder, and do not calibrate it.
6. **Does the gate on that stage actually fire?** Checks 1–5 are all method-level — *nothing calls X*
   — and the wiring backlog §1.1 names their shared blind spot outright: **gate-level dormancy**,
   where the call site exists and executes but its condition is almost never true. Such a surface
   looks perfectly wired to every check above it. The measured instance is C1 — **#12 commits
   `InPoss` on 9.5% of final-third samples**, starving every phase-gated mechanism in #13/#14/#15 —
   and it was found by runtime instrumentation during §5.Z.24, by no static analysis. So this check
   costs a run: count how often each gate and trigger condition on your chain fires over a match, not
   just whether its call site is reachable. A stage firing at 9.5% is not wired in any sense that
   matters to a dial downstream of it. The general instrument for this is **W12** on the backlog and
   is not built yet; until it is, instrument your own chain's gates as part of §1 and resume the gate
   with the counts — the same fallback the chain-enumeration step above uses.

### When checks 2–4 or 6 fail — the wiring branch

The pass does not stop, it changes shape. **Everything in this skill still applies except §3.**
Instrument (§1) so the missing stage is proven absent rather than assumed absent, localize (§2), land
the wiring, re-measure pre/post on identical seeds (§4), lock it with an acceptance scenario (§5),
sweep the fixed windows your change moved (§6), and close out (§7). Say explicitly in the commit and
the supplement: **this pass wired X; no `[GT]` was moved.** That sentence is what makes the resulting
delta attributable — a wiring landing and a calibration landing are not distinguishable after the
fact from the numbers alone.

The reason to land the wiring first is not that it moves the number more — sometimes it does,
sometimes a dial moves it more. It is that **a missing stage bounds the outcome at a level no dial
can reach**, so a ladder run underneath one is measuring the gap and not the setting. §5.Z.18 took
goals per match **15.3 → 12.3** by making four outcome classes reachable at all — the goal had no
crossbar, a collision TODO was still a TODO, and `ShotWorldAdapter`'s pressure query returned a
hardcoded `0f`. §5.Z.23's catch-park took the corpus from 15 goals to 11 (5.0 → **3.7** per match,
the closest this engine has measured to football's ~2.7) with **no `[GT]` touched at all**. The §5.Z
Phase H possession bootstrap turned every match from a 90-minute 0–0 deadlock with the ball never in
motion into a match that plays. None of those was reachable by tuning.

## 0.1 The premise check

§0 is the mechanical half; this is the semantic half, and it runs second because a premise about a
stage that does not exist is not worth interrogating. A realism brief almost always arrives carrying
a premise. §5.Z.15 named "the quality of the goalkeeper's save" as the next lever —
and the keepers turned out to make **zero** hand contacts all match, so "save quality" was not a low
number, it was undefined. §5.Z.22 assumed a contact stops a shot; tripling contacts left goals
unchanged because the added contacts were marginal touches whose parries kept the ball alive.

So before designing a fix, write down the brief's premise as a sentence and ask what measurement
would refute it. If no instrument in the tree can answer that, the first deliverable is the
instrument, not the fix.

## 1. Instrument before hypothesis

Build (or extend) an env-gated diagnostic test. Exemplars to copy the shape from:
`src/match-engine/tests/GkContactRateDiagnosticTests.cs`,
`ShotOutcomeDiagnosticTests.cs`, `FoulRateDiagnosticTests.cs`, `GkSaveDiagnosticTests.cs`.

The conventions that matter:

- Gate on an env var and `Assert.Ignore` when unset, so the instrument ships without costing the
  gate time: `if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_GK_DIAGNOSTIC"))) Assert.Ignore(...)`.
  Put the exact run command in the file header comment.
- **Never assert current behaviour.** Pinning a defect turns it into a contract, and a later correct
  fix then reads as a regression. Instruments report; scenarios assert.
- **Report a funnel, not an endpoint.** `armed → committed → dive → airborne → contact → caught`
  localized the keeper chain to a single stage at exactly zero. A single end-of-chain number tells
  you the chain is broken and nothing about where.
- **Measure per episode, not per frame,** when attribution matters. A frame aggregate cannot separate
  "wrong position" from "wrong timing"; the per-episode measurement at the ball's goal-plane crossing
  is what showed 9 of 15 misses were dive-early rather than dive-late.

Run it and write the baseline numbers down before touching production code. Football references to
compare against: **~2.7 goals/match, ~25 shots, ~17 m mean shot distance, ~20–25 m/s shot speed,
~30% of shots blocked, ~30% off target, ~22 fouls, ~3.5 yellows, ~0.25 reds.**

## 2. Localize against source, not against intuition

Read the actual production path and name the defect precisely enough to file it. Every pass so far
found the cause in one of three places, and it is worth checking them in this order:

1. **A formula that omits the dominant term** — `U_SHOOT` had no distance term (ERR-008-017);
   `AssembleRiskScore` has no age term. Ask what the strongest real-world predictor is and grep for it.
   ERR-008-018 is the same class with a twist worth knowing: #8 §3.1.5.2 *promised* the dribble's
   directional term and delegated it to "the scoring stage (§3.2.2)" — but §3.2.2 is the PASS formula
   and DRIBBLE's own §3.2.4.1 never had one, so the term was delegated to a section that does not own
   it. A cross-reference is not an implementation; follow it to the named section and check.
2. **A value consumed at the wrong time** — the reaction window was recomputed every frame, so what
   the contact consumed was dated by the ball's whole flight (ERR-011-005); the detection stamp was
   never cleared, dating dives against shots 85–349 *seconds* old (ERR-011-006).
3. **A gate that is structurally unreachable or vacuous** — `MIN_GOAL_VISIBILITY` equalled
   `GOAL_OPENING_MIN` so the SHOOT gate could never fire; `OnShotExecutedEvent` had zero callers
   anywhere, making a catch arithmetically impossible.

Cause 3 — and cause 2 whenever the "wrong time" is *never* — is §0's wiring gate failing late. If you
land there, go back and finish the gate rather than pressing on: the fix is a wiring fix, and §3 does
not apply to it.

When the spec text is itself the defect, patch the spec and the code **in the same commit** and file
the ERR. Use the `err-file-and-backprop` skill for that step.

## 3. Calibrate on a ladder — once §0 has passed — and report what the ladder refuses

**Read `match-engine-wiring-backlog.md` KD-W1 before this step — as of August 4, 2026 it freezes it.**
The rule is project-wide and stronger than this skill's own gate: *do not land a `[GT]` change
governing a subsystem that is not fully wired; constants wait for the calibration pass that follows
the backlog.* Defect fixes, instruments and measurement are explicitly unaffected and continue
freely. So on the match engine today the ladder is closed until that pass, and the honest move when a
brief asks for one is to say so and land the wiring item instead. Check whether KD-W1 is still in
force before reading further — it lifts when the backlog is worked off.

The hazard KD-W1 names is diagnostic, not arithmetic, and it is worth carrying even after the freeze
lifts: measured conversion of ~18% against football's ~11% reads as "the shot model is too generous"
when part of it is "no keeper has ever narrowed an angle and no defender has ever tackled." A pass
aimed at the shot model would have chased the wrong lever and left a `[GT]` that later has to be
un-tuned.

**When the freeze does lift, this step is conditional on the gate and on nothing else.** A ladder run
over a chain with a missing stage produces a number fitted to the gap, so that the correct later fix
reads as a regression against it. If §0 failed, land the wiring, re-measure, and re-enter here.

And then do not flinch from the ladder: `[GT]` calibration has been load-bearing in most of this
chain's passes and produced its largest single movement. §5.Z.20 took goals per match
**14.7 → 8.0** with a recalibration inside #11's own §3.4.3/§3.4.5 spec ranges, and its owner
document is explicit that the two timing defects fixed alongside it were not sufficient — the old
values "could not reach the catch band … even with a perfect window." §5.Z.19 moved `VFloor` 10 → 24
over two measured iterations; §5.Z.18 moved `MIN_GOAL_VISIBILITY` 0.05 → 0.12. A gate that talks you
out of those is doing more damage than the premature ladder it exists to stop.

`[GT]` values get chosen by running 3 full matches per rung on the same seeds, not by picking a
plausible number. Two findings worth carrying in:

- **The offline sweep gives the shape, never the value.** The foul sweep pointed at 0.025; a live run
  measured 37.5 fouls/90 min there, because 20× fewer fouls means 20× fewer restarts, so play runs on
  and the contact count *rises*. Always confirm on a live run.
- **Report when the ladder refuses the target — and read the refusal as a wiring diagnosis until
  proven otherwise.** A ladder refuses when the *level* is set upstream of the dial, which is what a
  missing or wrong stage looks like from inside a sweep. Shot volume could not reach count ≈ 25 *and*
  mean ≤ 22 m by any falloff value, because once long shots correctly lose to passes, volume is
  bounded by close-chance creation. §5.Z.23's geometry-aware `pointQuality` form fixed the direction
  and collapsed catches and parries to **zero**, and no `[GT]` inside #11's ranges lifts the blend
  back over `CatchThreshold`'s 0.65 floor, because mean contact marginality is 0.68 — so the next
  action there is a design decision about the contact geometry upstream, explicitly **not** another
  calibration run. That decision is now **parked rather than resolved**, and the reason is the point
  of this whole section: W1's keeper rush trigger changes the contact geometry the decision turns on,
  so answering it before the wiring lands would answer it against the wrong engine. Recording the
  refusal, and naming the upstream stage that bounds it, is more valuable than hitting the number.

Also check whether the threshold you are tuning is a **cliff rather than a dial**: the foul force
threshold gave 480 fouls at 1200 N, 90 at 2000 N and 0 at 3000 N, with intermediate values living on
the last thirty samples of a 130 000-tick run. A setting like that reads as calibrated while being
pure noise — the fix belongs elsewhere (there, a call *probability*).

## 4. Re-measure pre/post on identical seeds

Same seeds, same corpus length, both sides. Report the before → after pairs, not just the after.

**n=3 is noise.** An earlier build of the §5.Z.17 pass reported 14.0 goals/match on the same seeds
and that delta did not survive review. If the headline number moves by less than roughly a goal a
match over three matches, say the measurement does not support the claim rather than claiming it.

## 5. Lock it with an acceptance scenario

Register a scenario on the #19 ScenarioRunner. Copy the shape from
`src/match-engine/tests/MatchEngineKeeperContactScenarios.cs`: a `ScenarioManifest` with
`owningSpecIds`, a seed, `TestTier.TierB`, the path built from
`TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX`, and a `ClosedLoopScenario`.

Two rules earn their keep here:

- **Prove non-vacuity by execution.** Check out the pre-fix commit in a worktree, run the scenario
  there, and record how many predicates actually fail. Do not infer it. The keeper-contact pass
  reported "3 of 4 predicates fail on the pre-fix engine, verified by executing the scenario in a
  worktree" — that sentence is only worth writing if you ran it.
- **Size the corpus for the event you are asserting.** Threat episodes and shots are rare composed
  events; 9-minute windows thinned to 3 strikes and turned a predicate into a per-sample lottery.
  The shot scenarios moved to 18 min/seed for exactly this reason. If a reachability predicate reads
  zero on a legitimately-fixed engine, the window is too short.

Assert *structure* (reachability, ordering, bands), not a specific goal rate you have not earned. The
goalkeeper-saves scenario deliberately pins no save percentage and no goal rate.

## 6. Re-measure every fixed window your change moved

This is the step that has failed most often, and it is not a defect in the mechanism you landed — it
is instruments dating from before your change. The keeper-contact pass alone broke three, one of
which escaped to CI:

- the shot instruments sampled the strike at *end* of tick and named the goal by velocity sign, which
  broke once same-tick post-strike touches became common (a 13 m strike read as 92.3 m);
- the P1 observer-neutrality non-vacuity window, because the pass moved that seed's first restart
  from ~3 900 to 7 270 ticks;
- the #37 MatchAnalytics liveness window, because away-possession onset moved past 30 s.

So: grep the test tree for hardcoded tick windows, seed-specific event counts, and "first X happens
by tick N" assumptions in any area your change perturbs, and re-measure them. Fix at the root where
you can — the strike-time `TestOnly_LastShotStrikePosition/Velocity` seam replaced end-of-tick
sampling rather than widening a tolerance.

## 7. Close out

Declare explicitly, in the commit and the design supplement, whether the pass changed any of:
**`SNAPSHOT_SCHEMA_VERSION`, an RNG stream, a domain tag, a draw site, or the draw order.** Most
realism passes are pure functions of current-tick state and change none of them; saying so is what
makes the digest movement reviewable.

Then run the `dotnet-gate` skill, and the `landing-close-out` skill for the document sync. Record the
residual you did **not** fix as a named next lever with its measurement — that recorded residual is
what every subsequent pass in this chain started from.

Classify that residual as you record it: **a missing stage, or a mis-set dial?** The next pass runs
§0 against your sentence, so the classification is the handoff. §5.Z.23 is the model — it left two
levers, and named one of them as blocked on a design decision about upstream contact geometry rather
than on a calibration run. A residual recorded as "tune X" when X's level is bounded upstream sends
the next pass straight to the ladder this gate exists to stop.
