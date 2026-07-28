# Shot Outcome Distribution — Design Supplement

> **Created:** July 27, 2026
> **Version:** 1.0
> **Status:** DESIGN SUPPLEMENT (class (b) — governs a cross-spec balance/correctness pass;
> the owning specs are #6 Shot Mechanics, #1 Ball Physics, #3 Collision System, #8 Decision Tree,
> and the match-engine composition root)
> **Purpose:** Fix the four defects `goalkeeper-save-pipeline-design.md` §7.1–§7.4 recorded as the
> dominant term in the engine's ~4.7×-football goal rate: shots that cannot miss, a goal with no
> crossbar, defenders who cannot block, and a goal-visibility gate that cannot fire. This is the
> named blocker on roadmap item A4a.

---

## 1. The problem, quantified

§5.Z.17 measured **15.3 goals per match** against football's ~2.7, and localised the residual after
the goalkeeper pass to the **shot-outcome distribution**. In football roughly **30% of shots are
blocked and 30% miss the target**; in this engine both are approximately zero. Four defects, each
verified against source in §5.Z.17 and re-verified for this pass:

1. **§7.1 — a shot cannot miss to the side.** Every SHOOT aims `u ∈ {0.1, 0.9}` (0.732 m inside
   the post, `OptionGenerator.cs` / `TacticalWeights.PlacementCornerOffset`); the error model
   converts its *angular* magnitude to a **fixed goal-width fraction** (`ComputeErrorOffset`:
   `uv = errorDeg × Deg2Rad`), i.e. 2.25° → 0.287 m at the goal plane **regardless of distance** —
   so missing needs > 5.73° against a neutral shooter's ~2.25°; and the largest live multiplier,
   the **pressure penalty, is hardcoded to zero** (`ShotWorldAdapter.ComputePressureScalar => 0f`).
2. **§7.2 — a shot cannot miss vertically, and there is no crossbar.** `ShotExecutor.ExecuteContact`
   rebuilds the vertical from `sin(launchAngle)` and **never reads `finalDirection.z`**, so the
   entire vertical half of the placement-and-error model is inert — *even though the spec's own
   §3.5.7/§3.9 step 9 pins `finalVelocity = finalDirection × kickSpeed`*. And
   `BallCollision.CheckBoundaries` gates **every** boundary test behind `z < Ball.Diameter`
   (0.22 m), so a ball crossing the line airborne is neither a goal nor out of play — the goal is
   7.32 m wide and of unbounded height.
3. **§7.3 — no blocked shots.** `BallCollisionHandler.OnAgentCollision` is called in production and
   its body is an empty TODO; no agent deflects the ball by contact.
4. **§7.4 — the goal-visibility gate is vacuous.** `ComputeGoalOpeningScore` clamps to
   `[GOAL_OPENING_MIN, 1.0]` and the SHOOT gate rejects below `MIN_GOAL_VISIBILITY`; both are 0.05,
   so the gate fires only on the degenerate zero-arc early return.

## 2. Scope and non-goals

**In scope:** the five fixes in §3 (KD-1..KD-7), the measurement instrument (§4), the acceptance
scenario (§5), and the spec back-props (§6). **Out of scope, recorded:**

- **Physical woodwork.** `ApplyGoalPostCollision` keeps zero production callers. With correct
  airborne adjudication a ball inside the frame is a goal and outside it is out; the rebound off
  the bar/post is presentation-grade polish worth ~1–2% of shots, and detecting ball–post proximity
  is a Ball Physics integration of its own. Deliberately not attempted here so the outcome
  distribution's movement is attributable to the four named defects.
- **Pass-side pressure.** `PassWorldAdapter.ComputePressureScalar` stays `0f`. Wiring it changes
  general-play pass completion, which is a different balance question with its own measurement;
  landing it inside this pass would make this pass's result unattributable (the §5.Z.17 lesson).
- **Aim-point selection.** `u ∈ {0.1, 0.9}` (0.732 m inside the post) is a professional's aim and
  stays. The misses must come from the error model, not from making the shooter aim badly.
- **A last-toucher tracker.** Deflections still do not update `_lastHolderAgentId` (the AR-7 L-1
  approximation stands): a shot deflected out by a defender awards the corner against the last
  *settled* possession. Correct-by-Law awards need a physical-contact consumer this engine does not
  yet have; recorded, not fixed.

## 3. Design decisions

### KD-1 — Velocity assembly conforms to #6 §3.5.7: `finalVelocity = finalDirection × kickSpeed`

`ExecuteContact` drops the `cos/sin(launchAngle)` re-derivation and multiplies the post-error
`finalDirection` (unit vector) by `_kickSpeed`. This is what the APPROVED spec already says
(§3.5.7 *"finalVelocity = finalDirection × kickSpeed"*, §3.9 step 9); the implementation deviated.
The FM-04a degenerate-XY guard and FM-04 NaN guard stay. **ERR-006-002** (implementation
deviation; no spec change needed for this half).

### KD-2 — The intended aim direction is built per #6 §3.5.6: horizontal-to-target tilted by launch angle

Under KD-1 alone, `_intendedAimDirection` (currently `ComputeAimDirection`'s geometric line to the
`(u, v)` point) would make the placement `v` drive the vertical and leave the **launch-angle model
inert** — swapping one dead half for the other. §3.5.6 pins the composition: the horizontal unit
vector toward the `u` target, tilted upward by `launchAngleDeg` in the vertical plane of the shot.
New `ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(placementTarget, shooterPosition,
launchAngleDeg)` implements §3.5.6 verbatim (Z-up: the tilt lands in `.z`); `ShotExecutor`
INITIATING uses it. The old `ComputeAimDirection` keeps its other callers (none in production
besides this site) and its tests.

Consequence: vertical arrival = `tan(launch)·d − g·d²/(2·v²·cos²(launch))` ± vertical error — a
distribution that genuinely produces over-the-bar and dipping-under outcomes once KD-5 gives the
goal a crossbar.

### KD-3 — The error cone becomes a cone: displacement scales with distance

`MinErrorAngle` / `MaxErrorAngle` / `BaseErrorMax` are **angles**, and §3.6.5 speaks of an "error
cone" — but `ComputeErrorOffset` maps degrees to a **fixed goal-fraction** (`× Deg2Rad` as a uv
scale), which silently shrinks the cone with distance: 2.25° is 0.287 m at the goal plane from
*any* range, where a true cone gives 0.79 m at 20 m. Fix: the error displacement at the goal plane
is `tan(errorMagDeg · Deg2Rad) × shooterDistanceToGoalPlane`, decomposed along the hashed error
direction; `ApplyErrorOffset` takes the offset **in metres** (the `× GoalWidth / × GoalHeight`
uv-to-metres scaling moves into the caller-side conversion and is deleted rather than compounded).
**ERR-006-003** (spec §3.6.9's uv mapping contradicts the angular semantics of its own §3.6
constants; spec patched to the metres-at-goal-plane form).

Clamps: the horizontal clamp is unchanged (±`PlacementErrorHClampFraction` × goal width beyond the
posts — an errant shot lands at most 3.66 m wide). The vertical clamp becomes
`[0, max(baseTarget.z, PlacementErrorVClampFraction × GoalHeight)]`: under KD-2 the base
trajectory's height at the goal plane (`tan(launch)·d`) legitimately exceeds `1.5 × GoalHeight`
for lofted strikes, and clamping the *base* would silently flatten the launch model the moment
KD-1 makes `finalDirection.z` live. The clamp constrains what *error* can add, never what the
launch model intended.

### KD-4 — The shot pressure scalar is wired, reusing the first-touch evaluator

`ShotWorldAdapter.ComputePressureScalar` runs the **same** `TacticalDirector.FirstTouch.
PressureEvaluator.Evaluate` pass the composition root already runs for first touch
(`InternalsVisibleTo` the engine since Phase D D3 — reusing it is the anti-parallel-surface
choice), over the shooter's opposing team, filling the existing `_opponentScratch` buffer.
**Frame care:** #6 executes in the canonical attack-+X frame (§5.Z.14 mirror); the adapter
receives the shooter's *canonical* position and must mirror it back to world space for the away
team before evaluating (the mirror is involutive — `MirrorPitchIfAway` applied once more).
No new RNG draw: pressure changes the error *magnitude* only; the error *direction* stays the
§3.6.9 hash. Pressure is re-sampled at CONTACT exactly as §4.4.1 requires — the call site already
exists and already passes the canonical position; only the adapter body changes.

### KD-5 — Airborne boundary adjudication per Law 9/10: the `z < Diameter` gate is removed

The ball is out of play when it wholly crosses the goal line or touchline **on the ground or in
the air** (Law 9), and a goal is scored when it crosses between the posts and under the crossbar
(Law 10). `CheckBoundaries` drops the `lowEnough` gate on all three exits; the goal-line branches
adjudicate via the existing `IsBetweenPostsUnderCrossbar` (which already tests `z < GOAL_HEIGHT`)
— between the posts under the bar ⇒ goal, otherwise over/wide ⇒ corner or goal kick.
`BallStateMachine.IsOutOfBounds` drops the same gate **in the same commit** (its own doc pins the
two predicates to agree). **ERR-001-004**: the spec's own §3.1.10.3 pseudocode carries the gate
("Stage 0: only detects ground-level exits"); the Laws win, spec patched.

Known behavioural fallout, accepted: lofted passes that cross a touchline in flight now produce
throw-ins at the crossing rather than playing on until they land — correct football, and it will
move every digest. A ball crossing the line airborne and curving back in is out at the crossing
(the Laws' answer).

### KD-6 — Blocked shots: `BallCollision.ApplyAgentDeflection`, called from the empty TODO

Ball Physics owns the response (#1 §3.1.10.1, `BodyPartCoefficients` — already implemented and
test-locked, consumed by nothing); the Collision System owns detection and the call site
(`BallCollisionHandler.OnAgentCollision`). New `BallCollision.ApplyAgentDeflection(ref BallState
ball, Vector3 agentPosition, Vector3 agentVelocity, BodyPart bodyPart)`:

- **Normal:** from the agent's centre (at ball height) to the ball's position — the surface normal
  of a cylindrical body at the contact point. Degenerate (< epsilon) separation: no-op.
- **Response:** reflect the velocity component along the normal (only when approaching:
  `vn < 0`), scale total speed by `speedRetention(bodyPart)`, scale spin by
  `spinRetention(bodyPart)`. No restitution constant is invented — the retention pair IS the
  spec's model. Ball state → `Airborne`-or-`Bouncing` is untouched (the state machine
  re-classifies from velocity as it already does each step).
- **Gates, all in the handler (detection-side, #3's half):**
  (a) ball not `Controlled` — a dribbling touch is possession, not a deflection;
  (b) ball moving **toward** the agent (`dot(ball.Velocity, ball.Position − agent.Position) < 0`)
      — stateless self-block prevention: on the kick-release frame the ball moves away from the
      kicker, so a shooter can never block their own shot, with no cooldown state and no schema
      change;
  (c) ball speed ≥ `[GT] AgentDeflectionMinBallSpeedMps` (default **18.0**) — passes
      (≈ 8–16 m/s) keep routing through #4's first-touch control model exactly as today, so
      reception is untouched; shots (≈ 20–35 m/s) deflect. This constant is the single dial that
      separates "blockable ball" from "receivable ball" at Stage 0; a per-intent model is Stage 1+.
- **Agent-velocity contribution: none at Stage 0.** Agent speeds (≤ ~9 m/s) are second-order
  against shot speeds and a momentum-transfer model needs agent mass at the contact point;
  recorded as the Stage-1 refinement on the same seam.
- **Body part:** the caller's Stage-0 hardcoded `Torso` (0.55 speed retention) stands.
- **Determinism:** pure function of existing per-tick state; **no RNG, no new serialized state, no
  schema change**. The goalkeeper's hand-contact model (#11) is unchanged and runs in the Physics
  phase before Resolve-phase collisions; a keeper's *body* now also blocks, which is correct.

**ERR-003-007**: #3 §3.4.3's routing stub is now a live call; the deflection entry point the TODO
deferred is chosen (`ApplyAgentDeflection`) and recorded in both specs.

### KD-7 — The goal-visibility gate can fire: `MIN_GOAL_VISIBILITY` rises off the floor

`GOAL_OPENING_MIN` (the §3.2.3.2 step-5 floor, spec-pinned: "a tiny gap always exists") stays
0.05. `MIN_GOAL_VISIBILITY` (the §3.1.4.1 SHOOT gate, `[GT]`) rises to **0.12** so a shooter whose
goal arc is ≥ ~88% occluded holds the ball, passes, or dribbles instead of donating a blocked
shot. The exact value is checked against the §4 measurement (the gate should trim the most
hopeless attempts, not suppress shooting); it is a dial on *shot selection*, while KD-6 handles
the shots still taken.

### KD-8 — No snapshot, RNG, or draw-order change

No new cross-tick state anywhere in this pass (KD-6's toward-agent gate is the stateless
alternative to a cooldown, chosen for exactly this reason). No new RNG stream, domain tag, or
draw site; no change to the number or order of existing draws (pressure alters an argument to a
hash-based, not drawn, error direction). `SNAPSHOT_SCHEMA_VERSION` unchanged. Digests move for
any match containing a shot or an airborne boundary crossing — a behaviour change, as intended.

## 4. Measurement

New env-gated, assertion-free instrument `ShotOutcomeDiagnosticTests`
(`TD_SHOT_DIAGNOSTIC=1`), the `GkSaveDiagnosticTests` pattern: full 90-minute matches on the
`ConfigureSquads` path, 3 seeds. Reports per match:

- **shots** (rising edges of each keeper's `ShotDetectedTickMs` — the #11 notification fires once
  per completed shot at the defended end);
- **goals** (score deltas) and **goals per shot**;
- **on-target crossings** (ball crossing either goal-line plane inside the mouth — posts + bar);
- **off-target exits** (goal-kick/corner restarts within a window after a shot);
- **blocked shots** (AGENT_BALL collision events, via `TestOnly_SetCollisionObserver`, with ball
  speed ≥ the KD-6 gate at contact);
- ball-speed distribution at shot contact (sanity: the KD-6 gate must sit below it).

Run **before** the fixes (the §5 baseline) and after; the football reference is ~2.7 goals,
~30% blocked, ~30% off target, ~12 shots-on-target per match. This pass pins **directions and
mechanisms**; `[GT]` re-tuning beyond the two values named here belongs to A4a's calibration once
the distribution is football-shaped.

### 4.1 Measured (3 full 90-minute matches, `ConfigureSquads` path, identical seeds both runs)

| Metric | Pre-fix | Post-fix | Football |
|---|---|---|---|
| Goals per match | 18 / 12 / 16 (**mean 15.3**) | 8 / 14 / 15 (**mean 12.3**) | ~2.7 |
| Goals per shot | 0.24–0.29 | **0.14–0.25** | ~0.10 |
| Shots per match | 41–75 | 59–70 | ~25 |
| Fast-ball body contacts per match | **0** | **560–612** (77–196 inside the 10 s post-shot window) | — |
| On-target goal-mouth crossings | 10–19 | 8–14 | ~12 |
| Shot-tick ball speed (mean / max, m/s) | 7.2–9.4 / 17.2–20.7 | 6.9–10.3 / 15.3–18.9 | ~25 mean |

**Reading it.** Every structurally-absent outcome class now occurs: shots deflect off bodies
(zero → hundreds), airborne crossings are adjudicated (goal under the bar, out above it — probed,
see §5), the error cone widens with range, and conversion fell (0.24–0.29 → 0.14–0.25 goals/shot).
The goal rate moved 15.3 → 12.3 — real but far from ~2.7, **and the measurement localises the
remaining mass outside this pass's brief**: (a) **shot volume** — 59–70 shots/match is ~2.5×
football, a Decision-Tree selection / possession-churn property the KD-7 gate barely dents;
(b) **shot speed** — measured means of 7–10 m/s against football's ~25 (`VFloor`/`VCeiling`/
`PowerIntent` shaping in #6/#8), which both keeps shots on the ground (so the now-real crossbar
rarely bites — over-the-bar misses need ball speed the engine seldom produces) and gives keepers
easy contacts they still rarely hold; (c) the keeper's catch/parry conversion (§5.Z.17 §7.5).
Each is recorded as the follow-up lever set, deliberately not folded into this pass so its own
effect stays attributable.

## 5. Acceptance

New #19 ScenarioRunner scenario `match-engine-shot-outcomes` (Tier B, cross-spec, owning specs
{1, 3, 6, 8, 16, 19}; 4 seeds × 9 min, ~59 s): asserts **reachability of the outcome classes the
engine could not previously produce**. Two adjudication predicates are **probed by scripted
stimulus** (`TestOnly_SetBall`, the gk-heading-scenario precedent) rather than by natural
occurrence — an airborne crossing above the bar is out (not a goal, not nothing) and one under
the bar is a goal — because airborne line-crossings above 1 m are rare in 36 minutes of natural
play (passes aim at teammates infield; measured shot speeds ground the ball before the goal), so
a natural-occurrence floor would be flaky for the wrong reason; the natural count is reported
un-asserted. The natural-play predicates: shots taken, **fast balls deflect off bodies** (the
direction-change classifier, ≥ 60° with residual speed), goals still scored, a loose goal-rate
sanity ceiling, and two-run digest determinism run **sequentially** (interleaved engines diverge
at tick 1 — the §5.Z.7 process-static-EventBus property; the first draft interleaved and failed
its own determinism predicate, which is worth recording: the scenario reproduced a documented
engine property before it ever tested the fix). No exact rate bands (the §5.Z.17 rule).

**Pre-fix failure verified by execution** (worktree at the pre-fix commit, scenario copied in):
**3 of 8 predicates fail** — `over-the-crossbar-is-out-not-a-goal` (measured `score=0-0 cue=None`:
the crossing adjudicated as *nothing*, the exact §7.2 defect), `airborne-crossing-under-the-bar-is-a-goal`
(`score=0-0`), and `fast-balls-deflect-off-bodies` (0). The goal-rate ceiling does NOT
discriminate pre-fix on the neutral 9-minute windows (pre-fix scoring concentrates on the
`ConfigureSquads` path and in later match phases), which is recorded here so nobody later reads
that predicate as the pass's proof — the three zero-based reachability predicates are.

## 6. Spec back-props (filed with the landing, spec-error-log.md)

| ERR | Spec | Content |
|---|---|---|
| ERR-006-002 | #6 §3.5.6/§3.5.7 | Implementation deviation: `ExecuteContact` rebuilt the vertical from `sin(launch)` and discarded `finalDirection.z`; conformed to the spec's own `finalVelocity = finalDirection × kickSpeed`. Spec text already correct — no patch; code brought to spec. |
| ERR-006-003 | #6 §3.6.9 | The uv error mapping (`deg × Deg2Rad` as a goal-fraction) contradicts §3.6's angular constants — the cone was not a cone. Patched to metres-at-goal-plane: `tan(errDeg·Deg2Rad) × distance`. |
| ERR-001-004 | #1 §3.1.10.3 | The spec's own pseudocode gates every boundary exit behind `z < DIAMETER`, contradicting Law 9/10; gate removed for goal lines and touchlines, goal adjudicated via the posts/crossbar box. `IsOutOfBounds` moves in the same commit. |
| ERR-003-007 | #3 §3.4.3 / #1 §3.1.10.1 | The deferred agent-ball deflection entry point is chosen and wired: `BallCollision.ApplyAgentDeflection`, gated per KD-6. `BodyPartCoefficients` gains its first consumer. |

## 7. Risks and their tests

- **Pass reception regression** (KD-6): locked by asserting a sub-gate-speed ball inside an
  agent's hitbox is NOT deflected, and by the existing first-touch suites running unchanged.
- **Self-block on kick release** (KD-6 gate b): locked by a unit test with the ball inside the
  kicker's hitbox moving away.
- **Play-develops fallout** (KD-5): airborne exits increase restart counts; the existing
  `match-engine-play-develops`, `match-engine-goalkeeper-saves` and
  `match-engine-discipline-plausible` scenarios re-run in the full gate and must stay green (their
  predicates are reachability-shaped, not rate-pinned — goals scored across the spread still
  holds at any plausible post-fix rate).
- **Lofted-shot flattening** (KD-3 clamp): locked by a unit test that a high-launch base target
  above `1.5 × GoalHeight` survives `ApplyErrorOffset` unflattened.

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| AR-1 (design) | 1M + 2L | **M:** KD-6's first draft gated deflection on planar speed only; a dropping ball from a header/loft could exceed the gate vertically and slip it — gate uses full 3-D speed. **L:** KD-4's first draft forgot the §5.Z.14 canonical-frame mirror on the shooter position (would have evaluated the away shooter's pressure at the mirrored point — the exact per-side-pair class §5.Z.12 names); L: §5 originally proposed rate bands, retracted per the §5.Z.17 rule. |
| AR-2 (design) | 0H + 0M | CONVERGENCE. Re-walked KD-1..KD-8 against source anchors; verified `BodyPartCoefficients` exists and is test-locked, `_opponentScratch` is per-call scratch (no aliasing with a concurrent first-touch pass — both run in the same single-threaded Resolve phase), and the KD-6 toward-agent gate composes with the KD-5 boundary change (a deflected ball crossing the line airborne is now adjudicated at the crossing). |
| AR-3 (measurement-driven, at implementation) | 1M + 1M | **M (KD-6):** the design assumed shot speeds of 20–35 m/s and set the deflection gate at 18; the pre-fix instrument measured **12–21 m/s** (mean 7–9 at the post-tick read) — the gate would have made almost every shot unblockable. Re-anchored 18 → 10, with the reception argument shifted from the speed gate to geometry (the first-touch trigger reach, 1.0 m, is well outside the ~0.4 m combined hitbox; a ball cannot jump the gap in one 60 Hz tick below ~35 m/s), since pass speeds reach 28 m/s and no clean pass/shot speed separation exists. **M (§5):** the scenario's first draft ran its two determinism engines interleaved and failed its own predicate — the documented §5.Z.7 process-static-EventBus property; re-run sequentially. Its airborne-exit predicate was also re-based from natural occurrence to scripted stimulus (see §5). |

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-27 | — | Initial converged design: KD-1..KD-8 for the §7.1–§7.4 shot-outcome defects, measurement + acceptance plan, four ERR back-props. |
| 1.1 | 2026-07-27 | — | Implementation + measurement folded in: §4.1 measured table (goals 15.3 → 12.3, conversion 0.24–0.29 → 0.14–0.25, deflections 0 → 560–612/match), AR-3 (deflection gate 18 → 10 off measurement; scenario determinism run sequentially), §5 pre-fix execution evidence (3 of 8 predicates fail, two at `cue=None`/0-0 and one at exactly zero). Residual levers recorded: shot volume, shot speed, keeper conversion. |
