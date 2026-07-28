# Shot Speed & Physical Woodwork — Design Supplement

> **Created:** July 28, 2026
> **Version:** 1.1
> **Status:** DESIGN SUPPLEMENT (class (b) — governs a cross-spec balance/correctness pass;
> the owning specs are #8 Decision Tree, #6 Shot Mechanics, #1 Ball Physics, and the
> match-engine composition root)
> **Purpose:** Discharge residual lever (b) of `shot-outcome-distribution-design.md` §4.1 —
> shot speed (measured means 7–10 m/s against football's ~25) — and land the physical
> woodwork that becomes a real outcome class once speeds rise: at ~25 m/s the ball moves
> ~0.42 m per 60 Hz tick, so post/crossbar strikes need a **swept** segment test (a discrete
> per-tick position test tunnels straight through a 0.12 m post), and goal-line adjudication
> needs the **crossing point**, not the up-to-0.42 m-overshot detected position.

---

## 1. The problem, quantified

`ShotOutcomeDiagnosticTests` (3 full 90-minute matches, `ConfigureSquads` path) measures
shot-tick ball speeds of **mean 6.9–10.3 m/s, max 15.3–18.9** against football's ~25 mean.
The §5.Z.18 pass gave the goal a crossbar and shots a vertical error model — but at these
speeds shots stay on the ground, the crossbar rarely bites, and keepers receive easy contacts.
Two structural causes, each verified against source:

1. **#8 §3.5.3 pins PowerIntent at its own clamp floor.**
   `powerIntent = clamp(goalOpening × A_Finishing, 0.1, 1.0)` is a product of two [0,1]
   factors: `A_Finishing` is normalised `(raw−1)/19` (≈ 0.47 for a neutral 10) and
   `goalOpening` is the unblocked arc fraction, typically 0.2–0.6 under any pressure. The
   product is ≤ 0.3 for almost every generated shot and frequently below the 0.1 clamp
   floor — so nearly every shot in the engine is struck at **10–30% power**. The spec's own
   §3.5.3 rationale ("low GoalOpeningScore → agent reduces power to increase placement
   precision") inverts real football: a competitive shot is essentially always struck hard;
   occlusion argues for *placement*, not a pass-weight tap.

2. **#6 §3.2's [GT] anchors put a neutral player's full-power shot at ~16 m/s.**
   `vBase = VFloor + attrFraction × (effectiveCeiling − VFloor) × powerIntent` with
   `VFloor = 10`, `VCeiling = 35`: a neutral player (attrFraction 0.5, kickPower 10 ⇒
   effectiveCeiling 22.5) at **powerIntent = 1.0** reaches vBase = 16.25 m/s, before the
   §3.2.5–§3.2.8 multiplicative reducers (contact zone ≤ 1.0, spin, fatigue up to ×0.8,
   contact quality down to ×0.70) pull the result toward the `VAbsoluteMin` 8.0 clamp.
   Composed with cause 1 (powerIntent ~0.1–0.3) this puts the whole distribution at
   7–10 m/s — the measured band. Adult footballers of any standard strike 20–30 m/s.

**Consequences at today's speeds** (why this is the recorded lever): shots arrive on the
ground (max height at the goal plane rarely clears 1 m, so the §5.Z.18 crossbar and vertical
error model are nearly inert); keepers field slow rollers they still rarely hold; and the
goal rate's remaining mass sits partly behind this (a 9 m/s shot gives the keeper ~0.8 s
of reaction from 12 m — saves should be near-certain, and when they are not it is the
conversion defect (§5.Z.17 §7.5), not pace, deciding outcomes).

**The woodwork consequence of fixing it:** at 25 m/s the per-tick displacement is 0.42 m.
The goal frame's cylinders are 0.12 m in diameter (combined test radius with the ball,
0.17 m) — a discrete per-tick position test **tunnels** through them, and the goal-line
adjudication (which tests y/z at the detected position, up to 0.42 m past the plane) can
misclassify a rising ball that crossed under the bar as over it. `ApplyGoalPostCollision`
(#1 §3.1.2 restitution/spin-retention model) still has zero production callers.

## 2. Scope and non-goals

**In scope:** the #8 PowerIntent reshape (KD-1), the #6 `VFloor` retune (KD-2), the swept
goal-frame collision (KD-4), crossing-point goal-line adjudication (KD-5), measurement
(§4), acceptance (§5), spec back-props (§6). **Out of scope, recorded:**

- **Shot volume** (59–70/match, ~2.5× football) — residual lever (a), a DT-selection /
  possession-churn property; untouched so this pass's effect stays attributable.
- **Keeper catch/parry conversion** — residual lever (c), §5.Z.17 §7.5; untouched.
  Note the interaction: faster shots are HARDER to hold, so goals/match may move either
  way here — the measurement reports it, no predicate pins it.
- **`VAbsoluteMin` stays 8.0.** #6 Appendix A.1.4 deliberately sets the absolute clamp
  BELOW the stacked-penalty floor so a maximally-penalised shot (worst zone × full fatigue
  × worst contact) remains visible rather than being masked by the clamp. At the calibrated
  `VFloor = 24` the worst stack is ~0.42 × 25 ≈ 10.5 m/s — still above the clamp, exactly
  as A.1.4 intends (the retune WIDENS the margin). Raising the clamp would silently
  amputate the penalty model.
- **Rebound presentation** (net ripple, sound, `GoalPostHit` event into the ledger): the
  engine passes a null `BallEventLogger` (design note B2); no event surface changes.
- **Agent-velocity momentum transfer, curved flight through the frame within one tick:**
  Stage-1 refinements on the same seam; the segment is a first-order chord of the true arc
  (max sagitta over one tick at 25 m/s under gravity is g·dt²/8 ≈ 0.3 mm — negligible).

## 3. Design decisions

### KD-1 — #8 PowerIntent: floor-plus-modulation, not product-of-fractions (ERR-008-016)

```
powerIntent = clamp(POWER_INTENT_FLOOR
                    + (1 − POWER_INTENT_FLOOR) × goalOpening × A_Finishing,
                    POWER_INTENT_FLOOR, 1.0)
```

New `[GT] POWER_INTENT_FLOOR` (UtilityWeights, initial 0.65 — calibrated per §4). The floor
encodes "a deliberate shot is always struck hard"; the old formula's direction survives in
the top band (a better opening and a better finisher still strike harder, up to 1.0 for an
elite finisher with an open goal). The lower clamp bound coincides with the floor (the
expression cannot go below it for in-range inputs; the clamp is retained as the VR-02
range guarantee). Spec §3.5.3's mapping and rationale text are patched — the "low opening ⇒
reduce power" clause is the defect, contradicted by the game it models.

### KD-2 — #6 `VFloor` 10 → 24 [GT], calibrated in two measured iterations (ERR-006-004)

`VFloor` is the formula's zero-power/zero-attribute anchor. At 10 m/s it makes the
*attribute×power* term carry the whole distance to football's band, which §1 shows it
cannot (the span is multiplied by two ≤ 1 fractions twice over). Initial value from
arithmetic (20 — a neutral player at the KD-1 floor reaches vBase ≈ 22); the §4 measurement
then calibrated it (the FoulCallProbability precedent: the instrument decides the number,
the design records the shape): **iteration 1 at 20 measured means 12.5–14.8** — the
contact-quality and fatigue reducers bite harder over a full match than the mid-range
estimate — still under the ~15 acceptance threshold, so **iteration 2 moved to 24, measuring
means 14.7–16.1** (maxima 23–28; measured at the keeper-notification tick, after flight
drag). An elite finisher, fresh, clean contact, open goal reaches VCeiling 35 (unchanged,
still ≤ Ball MAX_VELOCITY 50 per XC-4.2-02). Stopping here rather than pushing further is
deliberate: each VFloor step compresses the attribute differential (the span is 35 − VFloor),
and the residual gap to football's ~20–25 sits in the Stage-0 contact-quality/fatigue model —
out of this pass's scope (KD-3). #6 Appendix A.1's `V_FLOOR` row and its A.1.4 worked
figures are patched with the retuned value and a correction note; the formula and its
sensitivity derivation are untouched.

### KD-3 — No other #6 dial moves

`VCeiling`, `VAbsoluteMin`/`VAbsoluteMax`, the sigmoid (`DMid`/`DScale`), the contact-zone
/ spin / fatigue / contact-quality modifiers all stay. One dial per cause (KD-1 the intent,
KD-2 the anchor); every additional dial moved would smear attribution across the
measurement (the §5.Z.17 lesson).

### KD-4 — Swept goal-frame collision: `BallCollision.ApplySweptGoalFrameCollision` (ERR-001-005)

New pure entry on `BallCollision` (Ball Physics owns the goal geometry and the post
response): `bool ApplySweptGoalFrameCollision(ref BallState ball, Vector3 prevPosition)`.

- **Geometry:** six capped cylinders — per goal (x = 0 and x = PITCH LENGTH): two vertical
  posts, axis at y = centreY ± (GOAL_WIDTH/2 + POST_DIAMETER/2) (IFAB: goal width is
  measured between the posts' INNER edges, which is exactly what
  `IsBetweenPostsUnderCrossbar` tests), z ∈ [0, GOAL_HEIGHT + POST_DIAMETER]; one
  horizontal crossbar, axis along y spanning the two post axes, at z = GOAL_HEIGHT +
  POST_DIAMETER/2 (GOAL_HEIGHT is to the bar's LOWER edge — the same convention the
  under-bar test uses).
- **Test:** the ball-centre segment `prevPosition → ball.Position` against each cylinder
  inflated by the ball radius (combined radius 0.06 + 0.11 = 0.17 m); take the EARLIEST
  parametric hit across all six (deterministic ordering by t, ties impossible to matter —
  same t means same contact point class).
- **Response:** place the ball centre at the contact parameter, then apply the EXISTING
  `ApplyGoalPostCollision` restitution + spin-retention model against the normal from the
  closest point on the struck cylinder's axis — its first production caller, as
  §5.Z.17/§5.Z.18 recorded it should eventually gain.
- **Gates:** ball not `Controlled` (a possessed ball is driven at the holder's feet — the
  agent-deflection precedent, gate (a)); degenerate segment (|pos − prev| < epsilon) is a
  no-op; a cheap X-band prefilter (segment within [−0.5, 0.5] m of a goal line plane)
  skips the six-cylinder test for the overwhelming majority of ticks.
- **Determinism:** pure function of two positions and the ball state; no RNG, no logging,
  no new cross-tick state (see KD-6), no schema change.

### KD-5 — Goal-line adjudication at the crossing point (same ERR-001-005)

`CheckBoundaries` gains an overload `CheckBoundaries(BallState ball, Vector3 prevPosition,
int lastTouchTeamID)`: when the ball's x has crossed an out-plane (x < −r or
x > LENGTH + r) this tick, the posts/bar box is evaluated at the segment's interpolated
crossing of that plane — `t = (plane − prev.x)/(pos.x − prev.x)` clamped to [0,1] — instead
of at the detected (overshot) position. At 25 m/s with a rising or dropping ball the z
difference between crossing and detection is up to ~0.2 m, which is precisely the band
around the crossbar; pre-fix a ball that crossed at z = 2.3 (goal) and was detected at
z = 2.5 adjudicated as over the bar. The parameterless-prev overload retains the old
position-tested semantics for callers without a segment (`BallStateMachine.IsOutOfBounds`
keeps its per-position contract — out-NESS is unchanged by this KD, only the
goal-vs-over/wide classification refines; the two predicates still agree on what is out).
Touchline classification has no y/z refinement to make (ThrowIn regardless of height —
Law 9 already handled by §5.Z.18). #1 §3.1.10.3's pseudocode is patched.

### KD-6 — Engine wiring: capture-before-integrate, collide-after-integrate

`RunPhysicsPhase` captures `_prevTickBallPosition = _ball.Position` immediately before
`BallPhysicsCore.UpdateBallPhysics`, and calls `ApplySweptGoalFrameCollision` immediately
after it (the rebound is ball physics, so it lives in the Physics phase; the Resolve-phase
`CheckRestartAndApply` then sees the post-rebound, in-play ball and no restart fires).
`CheckRestartAndApply` passes the same captured position to the KD-5 overload.
`_prevTickBallPosition` is WITHIN-TICK state — written at the top of every Physics phase
and consumed later the same tick, reset by construction — so it joins the
`RestartAppliedThisTick` class: no `SNAPSHOT_SCHEMA_VERSION` change, no exclusion-proof
extension beyond a doc note. A diagnostic-only woodwork counter
(`TestOnly_WoodworkStrikes`) joins the observation surface (not serialized, not
digest-load-bearing — the `AiPhaseRunCount` class).

### KD-7 — No snapshot, RNG, or draw-order change

No new serialized state, no new RNG stream/domain tag/draw site, no draw-order change.
Digests move for any match containing a shot (different kick speeds) — a behaviour change,
as intended. `SNAPSHOT_SCHEMA_VERSION` unchanged.

## 4. Measurement

`ShotOutcomeDiagnosticTests` (`TD_SHOT_DIAGNOSTIC=1`, 3 seeds × 90 min, same seeds
pre/post) runs before and after; the shot-tick speed row is the primary read, plus goals /
goals-per-shot / on-target / off-target / deflections so the distribution's movement is
visible in one table. Calibration loop: if means land under ~15 or over ~30, move `VFloor`
(primary) and `POWER_INTENT_FLOOR` (secondary) and re-run — values are pinned by the
instrument, not the arithmetic. A woodwork-strike count per match is reported via the
KD-6 counter (football reference: ~1–2% of shots).

### 4.1 Measured (3 full 90-minute matches, `ConfigureSquads` path, same seeds throughout)

The pre-fix column reproduces the §5.Z.18 recorded distribution exactly (the baseline re-run
matched it seed for seed). Calibration ran two iterations, both recorded:

| Metric | Pre-fix | Iter 1 (VFloor 20) | **Final (VFloor 24)** | Football |
|---|---|---|---|---|
| Shot-tick speed mean (m/s) | 6.9 / 7.6 / 10.3 | 12.5 / 14.0 / 14.8 | **14.7 / 16.1 / 15.9** | ~20–25 |
| Shot-tick speed max (m/s) | 15.3 / 17.9 / 18.9 | 21.8 / 21.4 / 25.4 | **23.4 / 23.3 / 27.6** | ~30+ |
| Shots per match | 59 / 70 / 59 | 57 / 37 / 51 | **31 / 34 / 45** | ~25 |
| Goals per match | 8 / 14 / 15 (12.3) | 16 / 9 / 15 (13.3) | **13 / 13 / 18 (14.7)** | ~2.7 |
| Goals per shot | 0.14–0.25 | 0.24–0.29 | **0.38–0.42** | ~0.10 |
| On-target crossings | 8 / 14 / 11 | 12 / 8 / 12 | **11 / 9 / 14** | ~12 |
| Off-target exits | 4 / 3 / 3 | 10 / 10 / 10 | **5 / 6 / 8** | — |
| Woodwork strikes | 0 (structural) | — (not yet instrumented) | **1 / 0 / 5** | ~0.5–1 |
| Fast contacts in shot window | 77 / 196 / 87 | 183 / 158 / 141 | **119 / 102 / 37** | — |

**Reading it.** The speed distribution moved where the arithmetic said it would: means roughly
doubled into the mid-teens (measured at the keeper-notification tick, i.e. after some flight
drag — contact speeds run higher), maxima reach 23–28, and the frame is live. Note the
shot-tick speed measurement itself was depressed pre-fix by an artefact this pass removes:
pre-fix many "shots" were rolling balls whose speed had decayed well below kick speed by the
notification tick. Three second-order effects, all recorded honestly:

1. **Shots per match FELL, 59–70 → 31–45** — toward football's ~25, not away from it. A
   football-pace shot ends its possession episode decisively (out, saved, scored, or cleared)
   instead of dribbling into the keeper and recycling; the possession economy changes with the
   pace. Residual lever (a), shot volume, is therefore roughly HALF discharged as a side effect.
2. **Goals per shot ROSE, 0.14–0.25 → 0.38–0.42** (goals 12.3 → 14.7/match). A football-pace
   shot beats this keeper far more often than a roller: the catch/parry conversion —
   §5.Z.17 §7.5, residual lever (c) — is now measured against real shot speeds for the first
   time, and it, not shot mechanics, is now unambiguously the dominant term in the goal rate.
   This pass deliberately does not touch it (§2).
3. **Off-target exits roughly doubled** (misses at pace fly out instead of dying in play), and
   the woodwork is a real outcome class for the first time: 1 / 0 / 5 strikes per match against
   football's ~0.5–1 — the right order of magnitude, with per-seed spread expected at these
   counts.

## 5. Acceptance

New #19 ScenarioRunner scenario `match-engine-shot-speed` (Tier B, cross-spec, owning
specs {1, 6, 8, 16, 19}; 2 natural seeds × 9 min + scripted probes): asserts
**reachability of the classes this pass exists to produce**, no exact rate bands
(the §5.Z.17 rule):

1. `shot-speeds-reach-football-band` — natural play **on the `ConfigureSquads` path** (the
   same distribution the §4 instrument measures and the [GT] values were calibrated
   against — see AR-4): shot-tick speeds observed with mean ≥ 14 m/s and max ≥ 20 m/s
   across the window (a loose floor the pre-fix engine's 7–10/15–19 cannot reach, not a
   pinned band).
2. `post-strike-rebounds-into-play` — scripted stimulus (`TestOnly_SetBall`, the
   established probe precedent): a 25 m/s ball aimed dead-centre at a post from 0.5 m out;
   after the tick the ball is IN PLAY (no restart cue), moving away from the goal line,
   and the score is unchanged. Pre-fix this ball crosses the plane inside the mouth
   (wide of the 7.32 m box? no — post centre is OUTSIDE the box) → adjudicates goal kick
   or corner; post-fix it rebounds.
3. `crossbar-strike-rebounds-into-play` — same shape against the bar (z at bar height,
   inside the mouth laterally). Pre-fix: crossing adjudicates under/over at the overshot
   position — either way the ball is OUT or a GOAL; post-fix: rebounds in play.
4. `rising-ball-crossing-adjudicated-at-the-crossing` — scripted: a ball whose segment
   crosses the out-plane at z ≈ 2.30 (under the 2.44 bar ⇒ goal) but whose detected
   position is z ≈ 2.55 (over). Pre-fix: no goal; post-fix: goal. The sharpest
   pre-fix-fails predicate — it isolates KD-5 alone.
5. Two-run digest determinism, engines run sequentially (the §5.Z.7 interleaving
   property).

Unit locks: OptionGenerator powerIntent floor/monotonicity/open-goal-elite = 1.0;
swept-cylinder tunneling detection (a segment that fully crosses a post inside one tick —
the discrete-test discriminator), post + crossbar reflection with restitution and spin
retention applied, degenerate-segment and Controlled no-ops, in-mouth trajectory NOT
falsely clipped; KD-5 crossing-point interpolation at the pure-function level (the
scenario-4 geometry without the engine).

**Pre-fix failure verification — by execution, 2026-07-28**, twice: the neutral-path first
draft ran against the unmodified working tree (mean 7.39 / max 12.50 vs floors 14/20; both
frame probes exits; rising crossing `cue=GoalKick`), and after AR-4 moved the natural
windows to the `ConfigureSquads` path, the amended scenario ran in a **worktree at the
pre-fix commit**: **5 of 7 predicates fail** — `shot-speed-mean-reaches-football-band`
(measured mean **8.90** vs floor 14), `shot-speed-max-reaches-football-band` (measured max
**17.59** vs floor 20), `post-strike-rebounds-into-play` and
`crossbar-strike-rebounds-into-play` (both probes adjudicated as exits — `restarted=true` —
instead of rebounding), and `rising-crossing-adjudicated-at-the-crossing` (`cue=GoalKick`,
score 0-0 — the overshot detected position read as over-the-bar, exactly the KD-5 defect).
`shots-are-taken` and `two-run-digest-determinism` pass pre-fix as expected (they guard the
fix, they do not date it). Unit locks on the new `BallCollision` entry points are
compile-gated pre-fix (the symbols do not exist) — structural, not executed.

## 6. Spec back-props (filed with the landing, spec-error-log.md)

| ERR | Spec | Content |
|---|---|---|
| ERR-008-016 | #8 §3.5.3 / §3.1.4 | PowerIntent's product-of-fractions form pins nearly every shot at the 0.1 clamp floor (measured means 7–10 m/s); the "low opening ⇒ reduce power" rationale inverts football. Patched to floor-plus-modulation with `[GT] POWER_INTENT_FLOOR`. |
| ERR-006-004 | #6 Appendix A.1 (calibration) | `V_FLOOR` 10 → 20: the 10 m/s anchor made the doubly-attenuated attribute×power term carry the whole distance to football's band. Value row + A.1.4 worked figures patched; formula untouched. |
| ERR-001-005 | #1 §3.1.10.3 / §3.1.2 | Boundary adjudication tested the detected position (up to 0.42 m past the plane at 25 m/s) and the goal frame was non-physical (`ApplyGoalPostCollision`: zero callers; discrete tests tunnel through a 0.12 m post at > ~10 m/s). Swept segment test + crossing-point adjudication added; pseudocode patched. |

## 7. Risks and their tests

- **Reception regression at higher ball speeds** (KD-1/KD-2): pass speeds already reach
  28 m/s and reception is protected by geometry, not speed (AR-3 of the shot-outcome
  pass); the first-touch suites and `match-engine-play-develops` re-run unchanged in the
  full gate.
- **False frame clips** (KD-4): a trajectory through the middle of the mouth must never
  hit a cylinder — unit-locked (in-mouth segment ⇒ no hit) and structurally bounded (the
  cylinders' y/z envelopes exclude the mouth interior by construction).
- **Restart-rate fallout** (KD-2): faster shots produce more goal kicks (misses fly out
  rather than dying in play); `match-engine-discipline-plausible` and
  `match-engine-goalkeeper-saves` re-run in the gate (reachability-shaped predicates).
- **Existing scenario `match-engine-shot-outcomes`**: its three load-bearing predicates
  (adjudication probes + deflection reachability) are speed-agnostic or benefit from
  higher speeds; the loose goal-rate ceiling only tightens. Re-run in the gate.
- **Keeper pipeline at pace** (interaction with lever (c)): `match-engine-goalkeeper-saves`
  asserts stage reachability, not save rates — faster shots do not unreach any stage
  (the dive commit is armed by ball proximity/on-target state, not speed ceilings).

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| AR-1 (design) | 1M + 3L | **M (KD-4):** the first draft placed the crossbar axis at z = GOAL_HEIGHT (2.44) — but GOAL_HEIGHT is the bar's LOWER edge (the same convention `IsBetweenPostsUnderCrossbar` relies on), so the axis sits at GOAL_HEIGHT + POST_DIAMETER/2 and a ball crossing at exactly 2.44 grazes the bar's underside instead of passing 6 cm inside it. **L:** post axis likewise moved OUTWARD by POST_DIAMETER/2 from the inner edge (IFAB measures goal width between inner edges — an axis on the 3.66 m line would have protruded the post 6 cm INTO the mouth and clipped legitimate near-post goals); the KD-5 overload's t must clamp [0,1] against a prev already beyond the plane (a rebound landing outside can re-cross next tick); scenario probe 2 corrected — a dead-centre post strike detected one tick later sits OUTSIDE the mouth, so its pre-fix adjudication is goal kick/corner, not a goal (the doc's first draft claimed a false goal). |
| AR-2 (design) | 0H + 0M | CONVERGENCE. Re-walked KD-1..KD-7 against source anchors: verified `A_Finishing` is the normalised form in `DecisionContext` (§1's arithmetic holds), `POST_DIAMETER` = 0.12 exists in the #1 catalogue, `ApplyGoalPostCollision`'s signature accepts an axis point (postCenter) so the swept caller can reuse it without change, the engine's Physics phase integrates the ball exactly once per tick (the segment is well-defined), and `_prevTickBallPosition`'s within-tick classification matches the `RestartAppliedThisTick` precedent (reset each Physics phase before use). Confirmed no existing test pins absolute shot speeds (the §5 suite is relational). |
| AR-3 (probe geometry, at implementation) | 2M | **M (§5 probe 3):** the first bar-probe design struck the bar's UNDERSIDE (centre z below the axis) — but the reflected normal points down-and-back, so the rebound dips INTO the goal and legitimately scores (a real football outcome: in off the underside of the bar). As a no-goal discriminator that probe is wrong twice over; both frame probes moved to dead-centre FRONT-face strikes, whose reflection is a clean −X rebound. **M (§5 probe 4):** the first rising-crossing geometry (crossing z ≈ 2.39–2.43) necessarily passes within the bar's 0.17 m combined radius — a ball whose CENTRE crosses just under the 2.44 datum has its top edge inside the bar, so "goal at the crossing" and "clears the frame" conflict. Solved analytically: crossing z ≈ 2.30 with slope 0.8 keeps the segment ≥ 0.22 m from the axis while the detected position still reads 2.58 (over) — the box-test-vs-cylinder tension is recorded as the box test's documented Stage-0 approximation (centre-point adjudication), not patched. |
| AR-4 (caught by the full gate) | 1M | **M (§5 predicate 1):** the scenario's first landed draft played its natural windows on the NEUTRAL path (`new MatchEngine(seed)`, all-10 squads) while the [GT] values were calibrated against the `ConfigureSquads` distribution — and the floors did not transfer: the neutral windows re-rolled to mean 9.60 under the final constants and the gate failed the scenario. The neutral path samples a different shot population (uniform attributes, different possession economy), so asserting the calibrated band against it pins numbers nobody measured. Fixed by sampling the same path the instrument measures (the diagnostic's `BuildSquad` recipe); floors unchanged. Pre-fix failure re-verified by execution on the amended scenario in a worktree at the pre-fix commit. |

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-28 | — | Initial converged design: KD-1..KD-7 for the shot-speed residual + swept woodwork, measurement + acceptance plan, three ERR back-props. AR-1 (1M+3L) + AR-2 (convergence). |
| 1.1 | 2026-07-28 | — | Implementation + measurement folded in: AR-3 (2M — the underside-bar probe legitimately scores off the rebound, both frame probes moved to front-face strikes; the rising-crossing geometry solved analytically against the bar's combined radius), §5 pre-fix execution record (5 of 7 predicates fail on the unmodified tree, by execution), §4.1 measured table over two calibration iterations (VFloor 20 → 24; means 6.9–10.3 → 14.7–16.1 m/s, shots/match 59–70 → 31–45, goals/shot 0.14–0.25 → 0.38–0.42, woodwork 0 → 1/0/5), KD-2 rewritten to the calibrated value. Residual levers recorded: keeper conversion (now dominant, measured against real pace for the first time), the remaining half of shot volume. |
