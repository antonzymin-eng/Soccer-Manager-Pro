---
name: match-realism-pass
description: >-
  Run a match-engine realism pass end to end — measure a football-plausibility gap with an env-gated
  instrument, localize the defect, file the ERR and patch spec + code together, calibrate the [GT]
  dials over a ladder, re-measure pre/post on identical seeds, and lock the result with a
  ScenarioRunner acceptance scenario proven to fail at the pre-fix commit. Use this skill whenever
  work touches how football-plausible the simulation is: goal rate, shots, shot speed or distance,
  save/contact rate, fouls or cards, possession churn, scorelines, "the engine produces too many /
  too few X", any §5.Z numbered pass, any "measure the lever" or "calibrate" request against the
  match engine, and any follow-up on a recorded-not-fixed residual from a previous pass. Trigger it
  even when the user only names the symptom ("goals are too high", "keepers never save") without
  asking for a measurement pass — the measure-first discipline is the whole point.
---

# Match Realism Pass

This repo has run this pass six times in two weeks (§5.Z.17 shot outcomes → .18 → .19 shot speed and
woodwork → .20 keeper conversion → .21 shot volume → .22 keeper contact). Every one had the same
shape, and every one produced a finding that the brief it started from was partly wrong. That is not
bad luck — it is the method working. The discipline below exists so the pass measures before it
believes, and so the result is attributable to the thing you changed.

Owner document: `docs/tracking/match-engine-design.md` (the §5.Z section chain). Each pass also gets
its own supplement at `docs/tracking/<topic>-design.md`.

## The premise check, first

A realism brief almost always arrives carrying a premise. §5.Z.15 named "the quality of the
goalkeeper's save" as the next lever — and the keepers turned out to make **zero** hand contacts all
match, so "save quality" was not a low number, it was undefined. §5.Z.22 assumed a contact stops a
shot; tripling contacts left goals unchanged because the added contacts were marginal touches whose
parries kept the ball alive.

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
2. **A value consumed at the wrong time** — the reaction window was recomputed every frame, so what
   the contact consumed was dated by the ball's whole flight (ERR-011-005); the detection stamp was
   never cleared, dating dives against shots 85–349 *seconds* old (ERR-011-006).
3. **A gate that is structurally unreachable or vacuous** — `MIN_GOAL_VISIBILITY` equalled
   `GOAL_OPENING_MIN` so the SHOOT gate could never fire; `OnShotExecutedEvent` had zero callers
   anywhere, making a catch arithmetically impossible.

When the spec text is itself the defect, patch the spec and the code **in the same commit** and file
the ERR. Use the `err-file-and-backprop` skill for that step.

## 3. Calibrate on a ladder, and report what the ladder refuses

`[GT]` values get chosen by running 3 full matches per rung on the same seeds, not by picking a
plausible number. Two findings worth carrying in:

- **The offline sweep gives the shape, never the value.** The foul sweep pointed at 0.025; a live run
  measured 37.5 fouls/90 min there, because 20× fewer fouls means 20× fewer restarts, so play runs on
  and the contact count *rises*. Always confirm on a live run.
- **Report when the ladder refuses the target.** Shot volume could not reach count ≈ 25 *and*
  mean ≤ 22 m by any falloff value, because once long shots correctly lose to passes, volume is
  bounded by close-chance creation. Recording that refusal is more valuable than hitting the number,
  and it is what identified possession churn as the structural residual.

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
