# Conversion at Contact — gk-contact-rate §7 item 1, and the creation funnel behind item 4

> **Created:** August 3, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row. Files one cross-spec back-prop against
> Goalkeeper Mechanics **#11** (`ERR-011-008`) — id verified free against `spec-error-log.md`
> (last #11 entry was `ERR-011-007`) and against `docs/specs/` before assignment.
> **Owner document:** `docs/tracking/match-engine-design.md` **§5.Z.23**.
> **Purpose:** §5.Z.22 tripled the keeper's contact rate and the goal count did not move. It
> recorded the reason as *"the added contacts are marginal, end-of-envelope touches whose parries
> and spills keep the ball alive in the box"*, and named two next levers: the Stage-0 `pointQuality`
> lottery and parry placement. This pass measured that premise before acting on it. **It is wrong,
> and wrong in a specific and correctable place**: the parries and spills do stop the ball; the
> CATCH does not. This note is that measurement, the fix, the probe that refused the second lever,
> and the sharper localization of the creation residual.

---

## 1. The premise check

Every pass in the §5.Z.17–§5.Z.22 chain has begun from a recorded brief and found the brief partly
wrong. The discipline that catches it is to write the premise down as a sentence and ask what
measurement would refute it.

> **Premise (gk-contact-rate §7 item 1).** The goal-rate surplus now lives in what a keeper's touch
> DOES: marginal end-of-envelope contacts parry and spill the ball live into the box, and goals come
> from the chains that follow.

Refutable by: classifying every keeper contact by band, and following the ball afterwards. No
instrument in the tree reported either. So the first deliverable was the instrument, not the fix —
`src/match-engine/tests/GoalConversionDiagnosticTests.cs` (env-gated `TD_CONVERSION_DIAGNOSTIC=1`,
assertion-free per the ERR-030-014 convention), three full 90-minute matches on the
`ConfigureSquads` path with the §5.Z.20–§5.Z.22 seeds so the numbers are same-population comparable.

## 2. The finding

### 2.1 A claim does not stop the ball

Per-contact band table, corpus aggregate (23 contacts, three full matches). `vIn` / `vOut` are ball
speed the tick before the contact and at the end of the contact tick; `marg` is the real
hand-envelope offset at contact divided by the keeper's reach radius:

| band | n | meanQual | meanMarg | vIn | vOut | goal | atk retained | def recovered |
|---|---|---|---|---|---|---|---|---|
| Caught | 10 | 0.847 | 0.714 | 11.1 | **10.8** | **7** | 0 | 3 |
| Parried | 2 | 0.709 | 0.837 | 10.8 | **0.0** | 0 | 0 | 2 |
| Deflected | 5 | 0.467 | 0.647 | 10.3 | 4.2 | 1 | 0 | 3 |
| Spilled | 4 | 0.192 | 0.740 | 13.9 | 9.0 | 0 | 3 | 0 |
| Missed | 2 | 0.073 | 0.697 | 9.5 | 9.5 | 0 | 0 | 2 |

Read the `vOut` column. A parry takes the ball from 10.8 m/s to a standstill. A deflection more than
halves it. A miss correctly changes nothing. **A catch removes 2.7% of the ball's speed** — one tick
of aerodynamic drag, and nothing else. And the goals follow the catches: **7 of the corpus's 8
contact-adjacent goals came from the band that is supposed to end the threat outright.**

Goal provenance over the same corpus: **14 of 15 goals follow a keeper contact within 10 s** (per
seed 4/4, 2/2, 8/9). The residual really was at the keeper's hands — just not at the hands the brief
named.

### 2.2 Verified against source

`GoalkeeperMechanics.Update`'s catch branch calls `_ballSystem.SetPossessor(agentId)` and nothing
further; the Stage-0 smother/1v1 claim does the same. `SetPossessor` is
`_engine._possessingAgentId = agentId` — a flag.

**Possession is not a kinematic constraint anywhere in this engine.** `RunPhysicsPhase` integrates
the ball unconditionally through `BallPhysicsCore.UpdateBallPhysics`, and `CheckRestartAndApply`
adjudicates a goal on the ball's POSITION without consulting the holder — its own comment
anticipates "a possessed-into-the-goal ball … now 50 m away". So an 11 m/s shot the keeper "caught"
kept travelling and crossed his own line ~0.2 s later.

Three details make this the interesting kind of defect rather than a typo:

- **#11 §3.5.2's body has always been correct.** It reads
  `Ball.SetPossessor(gkId)` / `ball.velocity = gkHandVelocity  // parked at hand position`. Two
  statements. Only the first was implemented.
- **§3.5's Outputs summary is the contributing spec defect.** It read *"one of `Ball.SetPossessor`
  (catch) or `Ball.ApplyKick` (parry / deflect / spill)"* — naming a single ball-side effect for the
  catch. An implementer reading the summary would write exactly what is in the tree.
- **`IGoalkeeperBallSystem` offered no seam for the park.** Its only ball-mutating method is
  `ApplyKick`, which is a kick (and on the Pass/Shot adapters releases possession). There was no way
  to express "arrest the ball" through the interface, so the omission was invisible from the
  contract as well as from the summary.

This also explains §5.Z.22's null result directly. Tripling the contact rate added contacts across
all bands; the catches among them were no-ops on the ball, so the goal count did not move.

### 2.3 The lottery is real, and inverted

The brief's other named sub-lever checks out as a defect, and the instrument quantifies it for the
first time. `pointQuality` should fall as a contact becomes more marginal. Split by marginality
tercile:

| tercile | n | meanMarg | meanQual | catch% |
|---|---|---|---|---|
| low (tight) | 7 | 0.475 | 0.559 | 43% |
| mid | 8 | 0.730 | 0.564 | 38% |
| high (fingertip) | 8 | 0.905 | **0.590** | **50%** |

Flat, and if anything **rising** — a fingertip touch at 90% of full stretch is caught more often than
a comfortable body-line one. The mechanism is arithmetic: `GoalkeeperMechanics` passes
`ballState.Position` as BOTH contact anchors, so `contactPointError` reduces to the noise sample
alone, and `pointQuality = 1 − clamp01(σ·z / σ)` — **σ cancels exactly**. `HandlingPointErrorSigmaM`
is a `[GT]` dial with provably zero effect at any value, and E[pointQuality] = ½ + ∫₀¹(1−z)φ(z)dz
≈ 0.684, matching the 0.68 recorded in §5.Z.20. The real envelope offset IS computed — and stored in
`GkContactState.ContactPointError` as telemetry, explicitly discarded.

Why it is nonetheless **not** landed here: §3. 

## 3. Key decisions

- **KD-CC1 — a claim arrests the ball.** `IGoalkeeperBallSystem` gains `ParkBall()`; both claim
  sites (the §3.5.2 catch and the Stage-0 smother) call it beside `SetPossessor`. The engine adapter
  zeroes `_ball.Velocity` and `_ball.AngularVelocity`. This is §3.5.2's second statement, finally
  implemented.
- **KD-CC2 — `gkHandVelocity` reads as zero at Stage 0.** The ball is at rest in the keeper's frame.
  Carrying the hand's world velocity would be more faithful, and is a §7 refinement; it is not
  landed here because the dive is a synthetic envelope rather than agent locomotion, so "hand
  velocity" has no honest Stage-0 value, and a keeper drifting goalward would carry the ball over
  his own line for a reason the model does not actually represent.
- **KD-CC3 — the park does NOT enter `BallStateType.Controlled`.** `BallCollision.SetBallControlled`
  exists and is documented as the ball-side half of Ball Physics #1's Option-B possession model —
  and it has **zero production call sites**, so no ball this engine has ever produced is Controlled.
  `BallStateMachine` has no exit from that state ("transitions handled externally"), and the only
  production path out is a kick. Introducing the engine's first Controlled ball inside a realism fix
  would be a far larger change, with a latent trap at the GK release rule (which drops possession
  without touching ball state). Zeroing velocity achieves §3.5.2's stated effect and stays inside
  the contract the rest of the engine already runs on.
- **KD-CC4 — no new cross-tick state.** The park writes only `_ball`, which is already serialized,
  so there is **no `SNAPSHOT_SCHEMA_VERSION` change**, no new RNG stream, domain tag or draw site,
  and no draw-order change.
- **KD-CC5 — the spec back-prop patches the SUMMARY, not the body.** §3.5.2's pseudocode was right;
  editing it would be back-propagating a defect that does not exist there. §3.5's Outputs paragraph
  gains the catch's second effect and an ERR-011-008 note recording why the wording mattered, and
  §3.5.2 gains one sentence stating that every contact resolves to exactly one ball-side action, the
  catch's being a pair.
- **KD-CC6 — the `pointQuality` lottery is measured and recorded, NOT fixed.** See §4: the
  geometry-aware form was implemented and run, and the calibration ladder refuses it at every `[GT]`
  value inside #11's own spec ranges. Landing a half-calibrated mechanism alongside KD-CC1 would
  also have made KD-CC1's measured result unattributable.
- **KD-CC7 — parry placement stays out.** It is a real defect (`ComputeParryVelocity` reverses the
  incoming velocity and rotates it by a deterministic ≤ 0.20 rad; nothing steers a parry away from
  the goal mouth), but it produced **zero goals** across the corpus in both the pre- and post-fix
  runs. Fixing what the measurement does not implicate is how a pass loses its attribution.

## 4. The probe that refused the second lever

`pointQuality`'s shape defect is genuine, so rather than assert from algebra that it could not be
landed as a dial change, it was **implemented and measured** — divisor changed from the
self-cancelling `HandlingPointErrorSigmaM` to the keeper's reach radius, with the caller passing the
real anchors (`reachCenter` vs `ballState.Position`) instead of the ball position twice. Same three
seeds, same corpus:

| | pre-probe (KD-CC1 only) | with geometry-aware pointQuality |
|---|---|---|
| Caught | 11 | **0** |
| Parried | 1 | **0** |
| Deflected / Spilled / Missed | 5 / 4 / 2 | 9 / 16 / 6 |
| mean quality | 0.573 | 0.219 |
| quality by marginality tercile | 0.479 / 0.582 / 0.645 (inverted) | **0.261 / 0.255 / 0.150 (correct)** |
| goals / match | 3.7 | 4.3 |

**The shape becomes right and the level collapses.** Quality now falls monotonically with
marginality — the inversion is gone — but no contact reaches the parry band, let alone the catch
band, and goals rise. The cause is not the formula: mean contact marginality is **0.68**, i.e. the
engine's contacts are geometrically poor by construction, because the §5.Z.22 commit gate aims the
envelope at a *predicted* crossing point and the residual prediction error leaves the ball
two-thirds of the way to the envelope edge on a typical contact. A geometry-aware term correctly
reports "these are all scrambling touches".

The ladder then refuses: max `attrFactor` inside #11's ranges is `HandlingBase` 0.70 + `HandlingKAttr`
0.70 = 1.40 at an elite keeper, which with mean pointQuality ≈ 0.32 gives a blended quality ≈ 0.44 —
below `CatchThreshold`'s own floor of 0.65. **No `[GT]` setting inside the spec's ranges makes the
catch band reachable.** Recovering a realistic catch rate needs either better contact geometry (a
#11/#12 prediction-accuracy change) or a re-scaled divisor with widened `[GT]` ranges — both design
decisions requiring owner sign-off, not dial-turning inside a correctness fix (the project's own
balance-pass convention, the #21 G2 precedent). The probe was reverted; §7 carries it forward with
these numbers.

## 5. Acceptance

`match-engine-keeper-claim` (#19 ScenarioRunner, Tier B, 2 seeds × **90 min**, `ConfigureSquads`
path). Full-match windows because a CLAIM is rarer than a contact — 11 across three full matches —
and the sibling contact scenario's 45-min windows would thin this corpus to a per-sample lottery
(the §5.Z.21 / §5.Z.22 AR-4 corpus-sizing lesson, twice learned). Predicates:

1. `claims-occur` ≥ 3 — non-vacuity, the §5.Z.15 lesson (a lever measured on an engine that never
   produces the event is undefined, not low).
2. `claimed-ball-is-arrested` — ball speed ≤ 2.0 m/s at the claim tick.
3. `held-ball-does-not-enter-own-net` — no goal at the claiming keeper's end while he is still the
   recorded holder.

**Verified by execution in a worktree at the pre-fix commit `4b12954`: 2 of 3 predicates fail —
`travellingAfterClaim = 6 of 6`, `concededWhileHolding = 5 of 6`.**

Predicate 3 was mis-specified twice before it was right, and both corrections are worth recording.
It first read "any goal at this end within 5 s of a claim" and failed **on the fixed engine** at 3 of
7 — the keeper claims, the release rule drops the ball loose in his own box, and the scramble
scores. That is a creation-side observation (§7), not a failed claim, and a predicate that fails for
a reason it does not name is worse than no predicate. Narrowed to "while still holding", it then
read **0 pre-fix** — structurally unreachable, because a goal awards through `ApplyRestart`, which
clears possession inside the same `RunTick`, so closing the window on `holder != claimingAgent`
before reading the score closed it on the very tick the goal landed. Reordering the score check
ahead of the window close makes it reachable, and it then reads 5 of 6 pre-fix. **A predicate is not
finished until it has been run at the pre-fix commit.**

Unit locks: `GoalkeeperClaimTests` (3) — a claim parks and does not kick; a non-claim kicks and does
not park; every contact resolves to exactly one ball-side action (park XOR kick, the general form of
the ERR-011-008 shape). These drive a real contact through the orchestrator, so unlike the sibling
publish-free fixtures they boot the #11 event registrar and declare the Physics producer phase
rather than muting the assertion.

## 6. Measured result

Three full matches, `ConfigureSquads` path, same seeds pre/post:

| | baseline | post-fix |
|---|---|---|
| caught-band vIn → vOut | 11.1 → **10.8** m/s | 12.0 → **0.0** m/s |
| goals from caught contacts | **7 of 10** | **0 of 11** |
| caught contacts → defence recovered | 3 of 10 | **11 of 11** |
| goals over the corpus | 15 | **11** |
| goals per match | 5.0 | **3.7** |
| scorelines | 2-2 / 2-0 / 6-3 | **1-0 / 2-2 / 4-2** |
| goals following a contact within 10 s | 14 of 15 (93%) | 4 of 11 (36%) |

**3.7 goals/match is the closest this engine has measured to football's ~2.7** — past the §5.Z.21
best of 4.7 and the §5.Z.22 5.0. Per-seed movement is 4 → 1, 2 → 4, 9 → 6: not uniform, and at n=3
a 1.3 goals/match delta sits only just above the noise bar this chain has learned to respect. What
takes it past a claim about noise is that the mechanism's own signature is unambiguous and does not
depend on the goal count at all: the caught band's exit speed went 10.8 → 0.0 m/s and its goal count
7 → 0, on a fix whose entire content is "stop the ball".

The distribution is also right for the first time: every band now changes the ball in the direction
its name implies, and `Missed` — correctly — is the only band that leaves it untouched.

No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
change. **Full `tools/dotnet-ci/run-gate.sh`: PASSED, 0 failures** (whole tree green, 30 assemblies;
match-engine 368 passed / 8 env-gated diagnostics skipped, goalkeeper-mechanics 78 passed; quarantine
empty, so the full suite is enforced). Match-engine runtime rose ~9 min to 26 m 6 s — the cost of the
new keeper-claim scenario (2 seeds × 90 min) and the AR-1 shot-speed resize (4 seeds × 90 min).

**Blast radius.** A goal-rate move invalidates any A4a round-resolution fit: `round-resolution-corpus.md`
is amended (v0.4) to require a Step-0 re-run before the corpus, since a fit against the July-28 tree
would calibrate the quick-sim to 5.0 goals/match across a whole 380-fixture league. No per-tick work
was added, so the FR-PO-052 certified perf baseline is untouched. Instruments with hardcoded tick
windows or seed-specific event counts are the standing hazard for any behaviour change to this
assembly (three broke in §5.Z.22, one escaping to CI) — the full gate is what checks them here.

## 7. Recorded, NOT fixed

1. **The `pointQuality` lottery — measured, probed, and blocked on a design decision, not on
   effort.** §2.3 proves it is blind and inverted (quality 0.559 / 0.564 / 0.590 across rising
   marginality; catch rate 43% / 38% / **50%**), and that `HandlingPointErrorSigmaM` is a `[GT]`
   constant whose value provably cannot matter (σ cancels). §4 shows the geometry-aware form fixes
   the direction (0.261 / 0.255 / 0.150) and collapses the level to **zero catches and zero parries**,
   because mean contact marginality is 0.68 and no `[GT]` inside #11's ranges lifts the blend back
   over `CatchThreshold`'s 0.65 floor. **The next action is an owner decision, not a calibration
   run:** either improve contact geometry upstream (the §5.Z.22 commit gate's prediction error is
   what puts the ball at the envelope edge) or re-scale the divisor and widen #11 §3.4.5's `[GT]`
   ranges. Landing either without that decision would swap a measured defect for a guessed one.
2. **Parry placement.** `ComputeParryVelocity` returns the incoming velocity reversed and rotated by
   a *deterministic* angle ≤ 0.20 rad — a parry goes back out along the shot line, toward the
   attackers. Real keepers parry wide by training. Zero goals came from parries in either corpus, so
   it is a realism defect without a current goal-rate cost; it becomes load-bearing the moment
   item 1 lands and pushes contacts out of the catch band.
3. **A claimed ball is not held at hand height, and the keeper cannot carry it** (KD-CC2/KD-CC3).
   The parked ball settles under gravity to the keeper's feet, and the GK release rule then drops it
   loose. Adequate at Stage 0; a genuine hand-carry wants the Controlled-state question answered
   first.
4. **Close-chance creation — sharper than "possession churn", and the localization has moved.**
   §5.Z.21 recorded the residual as final-third churn at ~3× football's. Measured here per match:

   | | measured | football |
   |---|---|---|
   | team possession chains | 619 | — |
   | mean chain length | 8.7 s | — |
   | settles per chain | 2.15 | several |
   | final-third entries | 306.7 | ~110 |
   | **penalty-box entries** | **20.0** | ~45 |
   | shots | 13.7 | ~25 |
   | shots per third entry | 0.045 | ~0.2 |
   | shots per box entry | 0.68 | ~0.55 |

   The bottleneck is **not** shot selection and **not** the box: once the ball is in the penalty
   area this engine already shoots at football's rate or above. It is the single stage
   **final third → penalty area**, converting at **6.5%** against football's ~40% — a ~6× deficit at
   one transition. The ball reaches the final third constantly (one entry every 17 s) and almost
   never penetrates centrally. That is a Decision Tree / attacking-AI surface and its own pass; it is
   named here with the numbers so the next pass starts from a measured stage rather than from
   "churn".

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| Measurement-1 (baseline per-contact fate, 3 full matches) | — | **Refuted the brief.** The band table's `vOut` column separates the populations at a glance: parry 10.8 → 0.0, deflect 10.3 → 4.2, catch 11.1 → **10.8**. 7 of 10 catches followed by a goal within 5 s; parries and spills, zero. Localized to `SetPossessor` being a flag with no kinematic effect, verified against `RunPhysicsPhase` and `CheckRestartAndApply` |
| Probe-1 (geometry-aware `pointQuality`, implemented and run, then reverted) | — | The ladder REFUSES: catches 11 → 0, parries 1 → 0, goals 3.7 → 4.3/match. Direction correct, level unreachable inside #11's `[GT]` ranges. Recorded in §4 and §7 item 1 rather than landed half-calibrated |
| Self-1 (acceptance predicate, pre-fix execution) | 2 defects in the SCENARIO | Predicate 3 first over-attributed (failed 3 of 7 on the FIXED engine — post-release scrambles, not failed claims), then under-attributed (read 0 pre-fix — the goal restart clears possession inside the same `RunTick`, so the window closed on the very tick the goal landed). Both found by running it at the pre-fix commit rather than reasoning about it. Final form: 2 of 3 fail pre-fix, 6 of 6 and 5 of 6 |
| AR-1 (full-gate fallout — 1 failure, an instrument, not the mechanism) | 0H+1M | **M-1: `match-engine-shot-speed`'s `mean-shot-distance` predicate failed at 29.77 vs its 24.0 m ceiling**, and the first reading of that was wrong. It is not a thin-window artifact and it is not a regression: this pass removed a population of very-close-range REBOUND shots (a claimed ball is arrested instead of flying on, so the box scramble after a "catch" no longer happens), which shifts the windowed distance mean longer. Measuring rather than assuming settled it three ways — the same scenario **PASSES at the pre-fix commit** (so this pass moved it); the env-gated full-match diagnostic reads **29.5 / 12.9 / 19.5 m** across the three standing seeds, **21.7 m pooled over 41 strikes**, inside §5.Z.21's landed 16.5–27.1 m band (so the engine had not regressed); and holding everything else fixed, the same corpus reads **27.11 m at 18 min, 24.71 m at 45 min, and inside the ceiling over full matches** — i.e. **the shot-distance distribution is not stationary within a match**: early play is long-shot dominated and the close-range strikes accumulate only as box penetration develops, so every windowed estimate is biased toward the opening. Fixed in the ESTIMATOR: corpus widened 2 → 4 seeds (spanning the standing diagnostic population) and windows 18 min → full matches. **Predicates and bounds UNCHANGED** — raising the 24.0 ceiling past the current reading would have discriminated nothing, since pre-§5.Z.21 means of 30–34 m must still fail it. The fourth instance of the AR-4 class, and the first where the window's *bias*, not its variance, was the defect |
| Self-2 (instrument correctness) | 1 defect in the INSTRUMENT | `chains` and `turnovers` were computed from the same condition and were therefore equal by construction in all three seeds — a tautology reported as a finding. Replaced with settles-per-chain, which is the quantity the churn question actually turns on |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-03 | — | Initial: implemented + measured. §1 the premise check; §2 the finding (a claim never stopped the ball — caught band 11.1 → 10.8 m/s, 7 of 10 catches conceding, against parry 10.8 → 0.0) and the quantified `pointQuality` inversion; §3 KD-CC1..CC7; §4 the probe that refused the geometry-aware form; §5 acceptance (2 of 3 predicates fail pre-fix by execution) with both predicate mis-specifications recorded; §6 measured result (goals 5.0 → 3.7/match); §7 the four recorded residuals, including the creation funnel re-localized to the final-third → penalty-area stage (6.5% vs football's ~40%). |
#endregion
