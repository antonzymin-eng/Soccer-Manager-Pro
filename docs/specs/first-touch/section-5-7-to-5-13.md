## 5.7 Unit Tests â€” Edge Cases and Robustness (EC)

These tests verify that the system degrades gracefully under invalid or extreme inputs,
consistent with the validation rules in Â§4.8.

---

**Test ID:** EC-001
**Name:** `EdgeCase_NaNBallVelocity_DoesNotPropagateNaN`
**Purpose:** If ball velocity contains NaN (upstream data corruption), the system must
recover rather than propagating NaN through the simulation.

**Inputs:** ballSpeed = float.NaN (all other inputs valid)
**Expected:** Function returns a valid q âˆˆ [0.0, 1.0]. No NaN in output.
**Recovery method:** NaN ballSpeed should be clamped/replaced with VELOCITY_REFERENCE
fallback before formula application. See Â§2.6 FM-02.
**Failure interpretation:** NaN output causes the entire simulation to destabilize within
a few frames.

---

**Test ID:** EC-002
**Name:** `EdgeCase_ZeroBallVelocity_UsesMinimumVelocityFloor`
**Purpose:** A stationary ball (v = 0) must not cause division by zero in velocity
difficulty calculation (VelDifficulty = 0 / 15 = 0 â†’ denominator = 0).

**Inputs:** ballSpeed = 0.0 m/s
**Expected:** VelDifficulty clamped to 0.1 (VELOCITY_MIN_CLAMP from Â§3.1.4).
q = finite value âˆˆ [0.0, 1.0].
**Failure interpretation:** Division by zero exception or Infinity output.

---

**Test ID:** EC-003
**Name:** `EdgeCase_AttributesBelowMinimum_ClampedTo1`
**Purpose:** Attributes must be validated before formula entry. Technique = âˆ’5 should
behave identically to Technique = 1.

**Inputs:** Technique=âˆ’5, FirstTouch=âˆ’3
**Expected:** q equivalent to Technique=1, FirstTouch=1
**Note:** Clamping is documented in Â§4.8.1 validation asserts. In debug builds this
should also log an assertion failure.

---

**Test ID:** EC-004
**Name:** `EdgeCase_AttributesAboveMaximum_ClampedTo20`
**Purpose:** Attributes above 20 must not produce super-human results.

**Inputs:** Technique=25, FirstTouch=30
**Expected:** q equivalent to Technique=20, FirstTouch=20
**Failure interpretation:** If q > 1.0 (before final clamp), super-maximum attributes are
producing phantom quality â€” indicates the clamp is happening after normalization, not before.

---

**Test ID:** EC-005
**Name:** `EdgeCase_AgentAtPitchBoundary_BallPositionClamped`
**Purpose:** A touch at the pitch edge must produce a newBallPosition that remains within
pitch bounds after displacement.

**Setup:** Agent at position (0.5, 34.0, 0) (near sideline). Touch direction toward
X < 0 (off pitch). q=0.50 â†’ r â‰ˆ 0.90m.
**Expected:** newBallPosition.x â‰¥ 0.0 (clamped to pitch boundary)
**Failure interpretation:** Negative X position means boundary clamping in Â§3.3.4 is
not being applied.

---

**Test ID:** EC-006
**Name:** `EdgeCase_BallHeightAboveGroundControlHeight_ShouldNotEvaluateFirstTouch`
**Purpose:** The height guard at Â§2.2 must prevent First Touch evaluation for aerial balls.
This tests that the guard is active.

**Inputs:** BallHeight = 0.6m (above GROUND_CONTROL_HEIGHT = 0.5m)
**Expected:** EvaluateFirstTouch() throws an error, returns an error code, or is not called
(depending on implementation: the guard may be in the calling Collision System code).
The Â§4.8.1 validation assert must fire in debug builds.
**Note:** The *correct* handling is routing to Heading Mechanics. This test confirms the
guard triggers, not that heading mechanics runs (which is out of scope).

---

**Test ID:** EC-007
**Name:** `EdgeCase_SimultaneousContact_OnlyPrimaryContactEvaluated`
**Purpose:** If two agents contact the ball in the same frame (Collision System's primary
contact resolution), only one FirstTouchContext should enter EvaluateFirstTouch().
Tests the boundary condition at the Collision System interface.

**Setup:** Configure mock Collision System to return two simultaneous AgentBallCollisionData
structs in the same frame.
**Expected:** EvaluateFirstTouch() is called exactly once (primary contact only).
**Note:** This test exercises the contract defined in Collision System Spec Â§3.x
(primary contact determination). First Touch does not make this decision itself.

---

**Test ID:** EC-008
**Name:** `EdgeCase_InterceptionChain_DoesNotRecurseInSameFrame`
**Purpose:** When outcome = INTERCEPTION, the intercepting agent must not evaluate their
own first touch in the same frame (Critical Issue #4 resolution, Â§3.4.5).

**Setup:** Frame N: receive INTERCEPTION outcome. Confirm no second EvaluateFirstTouch()
call occurs for the intercepting agent in frame N.
**Expected:** EvaluateFirstTouch() for intercepting agent called zero times in frame N,
exactly once in frame N+1.
**Failure interpretation:** Same-frame recursion would cause infinite regression or incorrect
double-evaluation of the ball state.

---

## 5.8 Unit Tests â€” Ball Displacement (BD)

These tests exercise `CalculateBallDisplacement()` and the related velocity calculation from
Â§3.3. The ball displacement model has three interdependent components: the angular direction
blend (Â§3.3.2), the displacement distance (Â§3.3.3), and the new ball velocity (Â§3.3.5).
Each is tested independently before the compound behaviour is verified.

All vector inputs are in the XY plane (Z = 0) consistent with the coordinate system
defined in Ball Physics Â§3.1.1.

---

**Test ID:** BD-001
**Name:** `BallDisplacement_PerfectQuality_ActualDirectionMatchesIntended`
**Purpose:** At q = 1.0, the angular error model (Â§3.3.2) must produce zero deviation â€”
the ball goes exactly where the agent intended. This validates that the blend formula
`blended = IntendedDir Ã— 1.0 + IncomingDir Ã— 0.0` produces ActualDir = IntendedDir.
**[HAND-CALC]**

```
q = 1.0
ErrorWeight = 1.0 - q = 0.0
blended = IntendedDir Ã— 1.0 + IncomingDir Ã— 0.0 = IntendedDir
ActualDir = Normalise(IntendedDir) = IntendedDir  [already unit vector]
Angle between ActualDir and IntendedDir = 0Â°
```

**Inputs:**
- IntendedDir = (1.0, 0.0) [agent wants ball to go right]
- IncomingDir = (âˆ’0.707, 0.707) [ball coming from lower-left]
- q = 1.0

**Expected:** ActualDir = (1.0, 0.0) Â± 2Â°
**Tolerance:** Angular deviation < 2Â° (floating-point precision margin only)
**Failure interpretation:** Non-zero deviation at q=1.0 means the blend formula applies
a residual error term when it should be zero. Check ErrorWeight computation.

---

**Test ID:** BD-002
**Name:** `BallDisplacement_ZeroQuality_ActualDirectionMatchesIncoming`
**Purpose:** At q = 0.0, the agent has no control. The ball must follow the incoming
direction (deflected along its original path), not the intended direction. This validates
the opposite extreme of the blend formula.
**[HAND-CALC]**

```
q = 0.0
ErrorWeight = 1.0 - 0.0 = 1.0
blended = IntendedDir Ã— 0.0 + IncomingDir Ã— 1.0 = IncomingDir
ActualDir = Normalise(IncomingDir) = IncomingDir
```

**Inputs:**
- IntendedDir = (1.0, 0.0) [agent wants ball right]
- IncomingDir = (0.0, 1.0) [ball came from below, deflects upward]
- q = 0.0

**Expected:** ActualDir â‰ˆ (0.0, 1.0) (matches IncomingDir)
**Tolerance:** Angular deviation from IncomingDir < 2Â°
**Failure interpretation:** ActualDir closer to IntendedDir than IncomingDir means the
blend weights are inverted (ErrorWeight applied to IntendedDir instead of IncomingDir).
This would produce the physically wrong behaviour: failed touches go where intended.

---

**Test ID:** BD-003
**Name:** `BallDisplacement_MidQuality_ActualDirectionIsBlendedBetweenBoth`
**Purpose:** At q = 0.5, the actual direction must be a normalised blend of IntendedDir
and IncomingDir with equal weights. The result should lie between the two directions.
**[HAND-CALC]**

```
q = 0.5, ErrorWeight = 0.5
IntendedDir = (1.0, 0.0)
IncomingDir = (0.0, 1.0)

blended = (1.0, 0.0) Ã— 0.5 + (0.0, 1.0) Ã— 0.5 = (0.5, 0.5)
magnitude = sqrt(0.5Â² + 0.5Â²) = sqrt(0.5) = 0.707
ActualDir = (0.5/0.707, 0.5/0.707) = (0.707, 0.707)
Angle from IntendedDir (1,0): arccos(0.707) = 45Â°
Angle from IncomingDir (0,1): arccos(0.707) = 45Â°
ActualDir is exactly between both â†’ 45Â° from each âœ“
```

**Inputs:**
- IntendedDir = (1.0, 0.0)
- IncomingDir = (0.0, 1.0)
- q = 0.5

**Expected:** ActualDir = (0.707, 0.707) Â± 2Â°
**Tolerance:** Â±0.01 on each component
**Failure interpretation:** ActualDir not equidistant between IntendedDir and IncomingDir
means the weighting is asymmetric. This produces skill-weighted bias at mid-quality.

---

**Test ID:** BD-004
**Name:** `BallDisplacement_NearZeroBlendMagnitude_FallsBackToIncomingDir`
**Purpose:** When IntendedDir and IncomingDir are nearly opposite (anti-parallel), the
linear blend produces a near-zero vector. Normalising a zero vector is undefined. The
Â§3.3.2 safety guard must detect this and fall back to IncomingDir.
**[HAND-CALC]**

```
q = 0.5, ErrorWeight = 0.5
IntendedDir = (1.0, 0.0)
IncomingDir = (âˆ’1.0, 0.0)  [anti-parallel: agent trying to play ball back the way it came]

blended = (1.0, 0.0) Ã— 0.5 + (âˆ’1.0, 0.0) Ã— 0.5 = (0.0, 0.0)
magnitude = 0.0 â†’ below BLEND_MIN_MAGNITUDE (0.001)
FALLBACK: ActualDir = IncomingDir = (âˆ’1.0, 0.0)
```

**Inputs:**
- IntendedDir = (1.0, 0.0)
- IncomingDir = (âˆ’1.0, 0.0) [exactly anti-parallel]
- q = 0.5

**Expected:** ActualDir = (âˆ’1.0, 0.0) [fallback to IncomingDir]
**Tolerance:** Angular deviation from (âˆ’1.0, 0.0) < 5Â°
**Failure interpretation:** Any of the following indicates the fallback guard is absent or
broken: (1) NaN in ActualDir â€” divide-by-zero; (2) ActualDir = (0, 0) â€” zero vector not
caught; (3) ActualDir = IntendedDir â€” guard falls back to wrong direction.

**Note:** This is the highest-priority test in the BD category. The blend fallback guard
(Â§3.3.2) was added specifically because of this edge case. Its absence causes
a simulation-stopping NaN cascade whenever an agent at qâ‰ˆ0.5 attempts a back-heel touch.

---

**Test ID:** BD-005
**Name:** `BallDisplacement_NewBallVelocitySpeed_CappedAtTouchMaxBallSpeed`
**Purpose:** Verifies the velocity cap in Â§3.3.5. A touch cannot accelerate the ball
beyond TOUCH_MAX_BALL_SPEED (12.0 m/s), regardless of agent speed or ball momentum.
This prevents first touches from launching the ball at kick-like speeds.
**[HAND-CALC]**

```
Scenario: fast agent, high incoming ball, perfect control
AgentContrib = ActualDir Ã— min(agentSpeed, DRIBBLE_MAX_SPEED)
             = ActualDir Ã— min(7.0, 5.5) = ActualDir Ã— 5.5   [capped to dribble max]

BallRetained = ball.Velocity Ã— (1.0 - q) Ã— MOMENTUM_RETENTION
             = ball.Velocity Ã— 0.0 Ã— 0.5 = 0     [q=1.0 â†’ no retention]

newBallVelocity = (ActualDir Ã— 5.5 Ã— 1.0) + 0
               = ActualDir Ã— 5.5   [magnitude = 5.5 < 12.0 â†’ no cap needed]

For cap test, use q=0.3 (heavy touch retains momentum):
AgentContrib magnitude = ActualDir Ã— 5.5 Ã— 0.3 = 1.65 m/s
BallRetained = 20.0 m/s Ã— 0.7 Ã— 0.5 = 7.0 m/s [same direction as incoming]
If directions align: newSpeed = 1.65 + 7.0 = 8.65 m/s  [under cap, need worse case]

Use q=0.3, incoming ball 30 m/s, agent 7 m/s, aligned directions:
AgentContrib = 5.5 Ã— 0.3 = 1.65 m/s
BallRetained = 30.0 Ã— 0.7 Ã— 0.5 = 10.5 m/s
newSpeed = 1.65 + 10.5 = 12.15 m/s â†’ exceeds TOUCH_MAX_BALL_SPEED (12.0)
â†’ newBallVelocity clamped: magnitude = 12.0 m/s âœ“
```

**Inputs:**
- q = 0.30 (heavy touch, retains momentum)
- agentSpeed = 7.0 m/s (sprinting)
- ballSpeed = 30.0 m/s (hard incoming)
- ActualDir = IncomingDir (aligned for maximum combined speed)

**Expected:** newBallVelocity.magnitude = 12.0 m/s (clamped to TOUCH_MAX_BALL_SPEED)
**Tolerance:** Â±0.01 m/s
**Failure interpretation:** magnitude > 12.0 means the speed cap in Â§3.3.5 is not applied.
This allows first-touch deflections to produce kick-speed balls, violating physical realism.

---

**Test ID:** BD-006
**Name:** `BallDisplacement_NewBallVelocityZ_AlwaysZeroOnGroundTouch`
**Purpose:** Â§3.3.5 explicitly sets newBallVelocity.Z = 0.0 for ground touches (Stage 0
simplification). Verifies that vertical velocity from the incoming ball is discarded.

**Inputs:**
- Ball incoming with velocity (10.0, 5.0, 2.0) [Z = 2.0 m/s upward component]
- q = 0.8 (good touch)
- Ground touch (BallHeight â‰¤ GROUND_CONTROL_HEIGHT)

**Expected:** newBallVelocity.Z = 0.0 (exact)
**Tolerance:** Exact 0.0
**Failure interpretation:** Non-zero Z means the vertical component is being retained,
which would cause the ball to bounce upward after a ground touch â€” physically wrong
for a Stage 0 "flatten the ball" reception model.

**Note:** This test documents the Stage 0 simplification noted in Â§3.3.5. Stage 1 will
introduce loft modelling for chest and thigh contacts. Any test that passes here will
require revision when loft is implemented.

---

**Test ID:** BD-007
**Name:** `BallDisplacement_PitchBoundaryEnforcement_XAxisClamped`
**Purpose:** newBallPosition must not exceed the pitch X boundary (0 to PITCH_LENGTH 105.0m).
Tests the boundary clamping from Â§3.3.4 on the X axis specifically.

**Setup:**
- Agent at position (1.0, 34.0, 0) â€” near goal line
- q = 0.40 â†’ r â‰ˆ 1.10m (Poor band)
- IntendedDir = (âˆ’1.0, 0.0) [touching ball toward own goal line / off pitch]
- DisplacementDist = r = 1.10m

**[HAND-CALC]**

```
newBallPosition.x = agent.x + ActualDir.x Ã— r = 1.0 + (âˆ’1.0 Ã— 1.10) = âˆ’0.10m
â†’ clamped to 0.0m (PITCH_LENGTH lower bound)
newBallPosition.y = 34.0 + 0 = 34.0m  [unchanged]
newBallPosition.z = Ball.RADIUS = 0.11m
```

**Expected:** newBallPosition.x = 0.0m (clamped); Y and Z unchanged
**Tolerance:** Â±0.005m on X
**Failure interpretation:** Negative X position is off the pitch, which would place the
ball in an invalid game state and cause Collision System spatial hash to behave incorrectly.

---

**Test ID:** BD-008  
**Name:** `BallDisplacement_PitchBoundaryEnforcement_YAxisClamped`
**Purpose:** Same boundary enforcement on the Y axis (0 to PITCH_WIDTH 68.0m). Tests
the sideline boundary specifically â€” a common scenario for wing play.

**Setup:**
- Agent at position (52.5, 67.5, 0) â€” near sideline
- q = 0.45 â†’ r â‰ˆ 0.90m
- IntendedDir = (0.0, 1.0) [touching ball toward sideline / off pitch]

**[HAND-CALC]**

```
newBallPosition.y = 67.5 + (1.0 Ã— 0.90) = 68.40m â†’ clamped to 68.0m (PITCH_WIDTH upper bound)
newBallPosition.x = 52.5 + 0 = 52.5m  [unchanged]
newBallPosition.z = 0.11m
```

**Expected:** newBallPosition.y = 68.0m (clamped)
**Tolerance:** Â±0.005m on Y

---

## 5.9 Integration Tests (IT)

Integration tests verify that First Touch interacts correctly with its three primary
consumers: Ball Physics (Â§4.5.1), Agent Movement (Â§4.5.3), and the Collision System (Â§4.5.2).
These tests require a minimal game context with at least two agents and a ball.

---

**Test ID:** IT-001
**Name:** `Integration_CollisionToFirstTouchToBallPhysics_CompletePipeline`
**Purpose:** The full Step-5-of-9 pipeline (Â§2.2). Collision System detects contact â†’
First Touch evaluates â†’ Ball Physics receives updated BallState. No steps are mocked.

**Setup:** One agent moving at 2.0 m/s toward a ball. Ball moving at 12.0 m/s toward agent.
All systems active (Collision, First Touch, Ball Physics).
**Expected:**
1. Collision System fires `AgentBallCollisionData` with correct contact point.
2. FirstTouchContext is built and populated from live game state.
3. EvaluateFirstTouch() runs and produces FirstTouchResult with no NaN fields.
4. Ball Physics receives `newBallState` and applies it as authoritative ball position/velocity.
5. Ball is no longer at its pre-touch position one frame after contact.
**Failure interpretation:** Ball remains at original position = Ball Physics did not receive
or apply the new state.

---

**Test ID:** IT-002
**Name:** `Integration_ControlledOutcome_AgentMovementReceivesDribblingModifier`
**Purpose:** When outcome = CONTROLLED, Agent Movement must receive the dribbling modifier
and apply speed/turn-rate penalties to the possessing agent.

**Setup:** Stage CONTROLLED outcome (elite agent, slow ball, no pressure).
Agent Movement system is live (not mocked).
**Expected:**
1. Agent enters dribbling locomotion state (Â§Agent Movement Â§6.1.2).
2. Agent's effective max speed is reduced per dribbling modifier.
3. Modifier persists across subsequent frames until possession is lost.
**Failure interpretation:** No speed reduction means the dribbling signal was not sent or
Agent Movement is not processing it.

---

**Test ID:** IT-003
**Name:** `Integration_PossessionChange_UpdatesTeamPossessionState`
**Purpose:** When outcome changes possession (CONTROLLED, INTERCEPTION), the match state's
team possession flag must update correctly.

**Setup A:** Team A agent receives ball cleanly â†’ CONTROLLED.
**Setup B:** Team A agent's heavy touch is intercepted by Team B agent (next frame).
**Expected A:** Match state shows Team A in possession.
**Expected B:** Match state shows Team B in possession after interception resolves.
**Failure interpretation:** Possession flag unchanged = First Touch is not writing to
match state, or match state is not reading from First Touch output.

---

**Test ID:** IT-004
**Name:** `Integration_EventEmission_FirstTouchEventPublishedOnEachContact`
**Purpose:** Every call to EvaluateFirstTouch() that produces a result must emit a
FirstTouchEvent (Â§3.8.3) to the Event System.

**Setup:** Three separate contacts in sequence (CONTROLLED, LOOSE_BALL, DEFLECTION).
Event System subscriber records received events.
**Expected:** Exactly three FirstTouchEvents received, one per contact, with correct
ControlQuality, TouchRadius, Outcome, and AgentID fields.
**Failure interpretation:** Missing events break the statistics and replay systems.

---

**Test ID:** IT-005
**Name:** `Integration_ReplayDeterminism_IdenticalMatchStateProducesIdenticalResults`
**Purpose:** The determinism requirement (Master Vol 1 Â§1.3). Same match state recorded
at frame N must produce identical First Touch output when replayed.

**Method:**
1. Record a 10-second match segment. Log all FirstTouchContext inputs and FirstTouchResult
   outputs at every contact.
2. Replay the same segment from identical initial conditions.
3. Compare logged inputs and outputs frame-by-frame.
**Expected:** Bitwise-identical outputs for all contacts in both runs.
**Failure interpretation:** Any divergence is a critical determinism failure. Root cause
must be identified before implementation proceeds. Common causes: floating-point
non-determinism, uninitialized memory, frame-order dependency.

---

**Test ID:** IT-006
**Name:** `Integration_GoalkeeperFootControl_ProcessedNormally`
**Purpose:** IsGoalkeeper = true in AgentBallCollisionData (Â§4.3.1) must not alter
First Touch evaluation for a foot contact. The flag is informational in Stage 0.

**Setup:** Goalkeeper agent (IsGoalkeeper=true) receives a back pass at 14 m/s.
Goalkeeper Technique=12, FirstTouch=10 (typical goalkeeper attributes).
**Expected:** EvaluateFirstTouch() produces the same result as an outfield player with
identical attributes. IsGoalkeeper flag does not modify q, r, or outcome.
**Failure interpretation:** Different result means the IsGoalkeeper flag is incorrectly
used to modify the formula, violating the Â§1.3 scope boundary.

---

**Test ID:** IT-007
**Name:** `Integration_PressureQueryFromSpatialHash_ReturnsCorrectOpponents`
**Purpose:** The pressure spatial query (Â§3.5.1) uses the Collision System's spatial hash
(Collision System Â§3.1.4). Verify the correct opponents are returned in a live scenario.

**Setup:** Receiving agent at (50, 34, 0). Place exactly two Team B agents within
pressure radius: one at (51.5, 34, 0) (1.5m away) and one at (50, 36.5, 0) (2.5m away).
Place one Team B agent outside radius at (53.2, 34, 0) (3.2m away).
**Expected:** Spatial query returns exactly two opponents. pressureScalar matches the
two-opponent hand calculation.
**Failure interpretation:** Three opponents returned means the radius filter is wrong.
One or zero returned means the spatial hash is not registering one of the in-range agents.

---

**Test ID:** IT-008
**Name:** `Integration_ConsecutiveTouches_OneTwoPass_BothEvaluateCorrectly`
**Purpose:** A one-two pass scenario: Agent A receives from Agent B (Touch 1), immediately
passes back to Agent B who touches it again (Touch 2). Tests that consecutive evaluations
in the same match context are independent and produce correct results.

**Setup:**
- Frame N: Agent A receives pass at 18 m/s. Should produce CONTROLLED or LOOSE_BALL.
- Frame N+2: Agent B receives the return pass at 14 m/s. Should produce CONTROLLED.
**Expected:** Both evaluations complete independently. Agent B's evaluation in frame N+2
uses Agent B's attributes, not Agent A's (no context bleed between evaluations).
**Failure interpretation:** Any shared state between evaluations is a critical data isolation
failure.

---

## 5.10 Real-World Validation Scenarios (VS)

These scenarios ground the First Touch system in recognisable football events. They serve
as sanity checks that the mathematical model produces intuitive results. All expected
values are hand-calculated and documented in Appendix B.

Each scenario maps to a player archetype and situation observable in professional football.
The scenarios do not test specific real players â€” they test attribute profiles that represent
real-world archetypes.

---

### VS-001: Elite Creative Midfielder, Standard Pass in Space
**Football context:** An elite technical midfielder (Ã–zil / De Bruyne archetype) receiving
a pass in a half-turn position with no immediate pressure.

**Setup:**
- Technique: 18, FirstTouch: 19
- Ball speed: 14 m/s (crisp pass)
- Agent speed: 3.0 m/s (jogging to meet ball)
- Half-turn oriented: Yes (orientationBonus = 0.15)
- No opponents within pressure radius (pressureScalar = 0.0)

**[HAND-CALC]** (Appendix B, VS-001):
```
Step 1: WeightedAttr = (18 Ã— 0.70) + (19 Ã— 0.30) = 12.60 + 5.70 = 18.30
Step 2: NormAttr = 18.30 / 20.0 = 0.915
Step 3: AttrWithBonus = 0.915 Ã— 1.15 = 1.052
Step 4: VelDifficulty = 14.0 / 15.0 = 0.933
Step 5: MoveDifficulty = 1.0 + (3.0 / 7.0) Ã— 0.5 = 1.0 + 0.214 = 1.214
Step 6: RawQuality = 1.052 / (0.933 Ã— 1.214) = 1.052 / 1.132 = 0.929
Step 7: q = 0.929 Ã— 1.0 = 0.929
Step 8: q = Clamp(0.929, 0, 1) = 0.929
Touch radius (Perfect band): t = (0.929 - 0.85) / 0.15 = 0.527 â†’ r_base = Lerp(0.30, 0.10, 0.527) = 0.195m
Velocity modifier: (14.0 / 15.0) x 0.25 = 0.933 x 0.25 = 0.233m
r = r_base + modifier = 0.195 + 0.233 = 0.428m
Outcome: r = 0.428m <= 0.60m â†’ CONTROLLED
```

**Expected outputs:**
- q â‰ˆ 0.929 (Â±0.03) â†’ Perfect band
- r â‰ˆ 0.428m (±0.02m)
- outcome = CONTROLLED
- dribbling state activated: true

**Real-world validation:** An elite playmaker's first touch in this scenario should produce
a ball landing within approximately 0.2m, ready for immediate play. This matches observable
professional ball control.

---

### VS-002: Target Striker, Long Ball Reception Under Pressure
**Football context:** A physical centre-forward (Haaland / Giroud archetype) receiving a
60-metre diagonal ball under defensive pressure.

**Setup:**
- Technique: 12, FirstTouch: 11
- Ball speed: 22 m/s (long diagonal ball, decelerating to this speed at arrival)
- Agent speed: 0.5 m/s (bracing, nearly stationary)
- Half-turn oriented: No
- Two defenders at 1.5m and 2.0m (pressureScalar calculated below)

**[HAND-CALC] Pressure first:**
```
Defender 1 at 1.5m: rawContrib = (0.3 / 1.5)Â² = 0.04
Defender 2 at 2.0m: rawContrib = (0.3 / 2.0)Â² = 0.0225
rawPressure = 0.0625
pressureScalar = Clamp(0.0625 / 1.5, 0, 1) = 0.042
```

**[HAND-CALC] Control quality:**
```
Step 1: WeightedAttr = (12 Ã— 0.70) + (11 Ã— 0.30) = 8.40 + 3.30 = 11.70
Step 2: NormAttr = 11.70 / 20.0 = 0.585
Step 3: AttrWithBonus = 0.585 Ã— 1.0 = 0.585
Step 4: VelDifficulty = 22.0 / 15.0 = 1.467
Step 5: MoveDifficulty = 1.0 + (0.5 / 7.0) Ã— 0.5 = 1.0 + 0.036 = 1.036
Step 6: RawQuality = 0.585 / (1.467 Ã— 1.036) = 0.585 / 1.520 = 0.385
Step 7: q = 0.385 Ã— (1.0 - 0.042 Ã— 0.40) = 0.385 Ã— 0.983 = 0.379
Step 8: q = 0.379
Touch radius (Poor band): t = (0.379 - 0.35) / 0.25 = 0.116 â†’ r = Lerp(1.20, 0.60, 0.116) = 1.130m
Velocity modifier at 22 m/s: VelExcess = 7, VelMod = 1 + (7/15) Ã— 0.25 = 1.117
r_adjusted = 1.130 Ã— 1.117 = 1.262m â†’ below 2.0m cap
Outcome: r = 1.262m â‰¥ 1.20m â†’ check opponents.
Two defenders, nearest at 1.5m < INTERCEPTION_RADIUS 2.50m â†’ INTERCEPTION
```

**Expected outputs:**
- q â‰ˆ 0.379 (Â±0.03) â†’ bottom of Poor band
- r â‰ˆ 1.262m (Â±0.05m) â†’ crosses INTERCEPTION_THRESHOLD
- outcome = INTERCEPTION (defenders exploit the heavy touch)

**Real-world validation:** A target striker's first touch on a fast long ball under tight
marking is frequently contested. The INTERCEPTION outcome here represents a defender
winning the second ball â€” a common scenario in physical football.

**Note on pressure values:** The two defenders at 1.5m and 2.0m in this scenario produce
lower pressure (0.042) than the outline's qualitative description implied ("medium pressure").
This is correct per the Â§3.5 formula. "Medium pressure" in colloquial terms corresponds to
opponents at tighter distances than 1.5m. Outline approximations were illustrative, not
precise. This hand-calculation takes precedence.

---

### VS-003: Defensive Midfielder, Receiving Under a High Press
**Football context:** A defensive midfielder (Busquets / Rice archetype) trying to receive
in a high-press scenario with a striker immediately closing.

**Setup:**
- Technique: 14, FirstTouch: 13
- Ball speed: 17 m/s (firm pass from goalkeeper)
- Agent speed: 1.5 m/s (standing and scanning)
- Half-turn oriented: Yes (pre-scanning, orientationBonus = 0.15)
- One pressing striker at 0.8m (very close)

**[HAND-CALC] Pressure:**
```
distance = 0.8m (> MIN_PRESSURE_DISTANCE 0.3m â†’ no clamp needed)
rawContrib = (0.3 / 0.8)Â² = 0.141
pressureScalar = Clamp(0.141 / 1.5, 0, 1) = 0.094
```

**[HAND-CALC] Control quality:**
```
Step 1: WeightedAttr = (14 Ã— 0.70) + (13 Ã— 0.30) = 9.80 + 3.90 = 13.70
Step 2: NormAttr = 13.70 / 20.0 = 0.685
Step 3: AttrWithBonus = 0.685 Ã— 1.15 = 0.788
Step 4: VelDifficulty = 17.0 / 15.0 = 1.133
Step 5: MoveDifficulty = 1.0 + (1.5 / 7.0) Ã— 0.5 = 1.0 + 0.107 = 1.107
Step 6: RawQuality = 0.788 / (1.133 Ã— 1.107) = 0.788 / 1.254 = 0.628
Step 7: q = 0.628 Ã— (1.0 - 0.094 Ã— 0.40) = 0.628 Ã— 0.962 = 0.604
Step 8: q = 0.604
Touch radius (Good band): t = (0.604 - 0.60) / 0.25 = 0.016 â†’ r = Lerp(0.60, 0.30, 0.016) = 0.595m
Velocity modifier: VelExcess = 2.0, VelMod = 1 + (2/15) Ã— 0.25 = 1.033
r_adjusted = 0.595 Ã— 1.033 = 0.615m â†’ above LOOSE_BALL_THRESHOLD (0.60m)
Outcome: r = 0.615m â‰¥ 0.60m. r < 1.20m (below INTERCEPTION_THRESHOLD).
â†’ Check opponent: striker at 0.8m < 2.50m? Yes.
BUT: r = 0.615m < INTERCEPTION_THRESHOLD (1.20m) â†’ INTERCEPTION priority not met.
â†’ LOOSE_BALL
```

**Expected outputs:**
- q â‰ˆ 0.604 (Â±0.03) â†’ just inside Good band
- r â‰ˆ 0.615m (Â±0.03m) â†’ just above LOOSE_BALL_THRESHOLD
- outcome = LOOSE_BALL (ball requires a second touch; striker can contest)

**Real-world validation:** A technically competent player in a half-turn receiving a firm
pass under a close press should retain the ball roughly 60% of the time but frequently
require a second touch. The LOOSE_BALL outcome represents a contested situation â€” the
midfielder has not cleanly controlled it but has not lost it outright. This is realistic.

**Design insight:** The half-turn bonus (15%) is what keeps this scenario from being an
INTERCEPTION. Without the half-turn, q would drop to approximately 0.526, pushing into
the Poor band with r â‰ˆ 0.80m and potentially triggering INTERCEPTION. The mechanical
incentive to adopt a half-turn stance is therefore demonstrated by this scenario.

---

## 5.11 Acceptance Criteria Summary

For implementation to be approved for Stage 0 sign-off, ALL of the following gates must pass.

### 5.11.1 Unit Test Gate

| Category | Required Count | Minimum Pass Rate |
|---|---|---|
| Control Quality (CQ) | 12 tests | 100% |
| Touch Radius (TR) | 10 tests | 100% |
| Pressure Evaluation (PR) | 8 tests | 100% |
| Body Orientation (OR) | 7 tests | 100% |
| Possession State Machine (PO) | 8 tests | 100% |
| Edge Cases (EC) | 8 tests | 100% |
| Ball Displacement (BD) | 8 tests | 100% |
| **Total unit tests** | **61 tests** | **100%** |

**No partial pass accepted.** Any failing unit test is a blocking issue. The minimum test
count from the outline was 25; this specification provides 61 â€” more than 2.4Ã— the minimum,
consistent with this project's pattern of exceeding minimums on critical systems.

### 5.11.2 Integration Test Gate

| Category | Required Count | Minimum Pass Rate |
|---|---|---|
| Integration tests (IT) | 8 tests | 100% |

### 5.11.3 Validation Scenario Gate

All three validation scenarios (VS-001, VS-002, VS-003) must produce outputs within the
specified tolerances.

### 5.11.4 Performance Gate

| Metric | Target | Hard Limit |
|---|---|---|
| p95 evaluation latency | < 0.05ms | < 0.10ms |
| p99 evaluation latency | < 0.10ms | < 0.20ms |
| Heap allocations per touch | 0 | 0 |
| Total match impact (300 touches) | < 15ms | < 30ms |

### 5.11.5 Coverage Gate

| Scope | Target |
|---|---|
| Section 3.1 (Control Quality) | â‰¥ 90% line coverage |
| Section 3.2 (Touch Radius) | â‰¥ 90% line coverage |
| Section 3.3 (Ball Displacement) | â‰¥ 85% line coverage |
| Section 3.4 (Possession) | â‰¥ 90% line coverage |
| Section 3.5 (Pressure) | â‰¥ 90% line coverage |
| Section 3.6 (Orientation) | â‰¥ 90% line coverage |
| Overall First Touch System | â‰¥ 85% line coverage |

### 5.11.6 Determinism Gate

The IT-005 determinism test must pass with zero divergences over a 10-second match
segment. This is a binary pass/fail gate with no tolerance.

---

## 5.12 Test Execution Plan

### 5.12.1 Test Ordering

Tests must run in the following order to ensure dependency satisfaction and efficient
failure diagnosis. Unit tests run first; failures here do not block integration tests
from being attempted (diagnosis value), but unit test failures are blocking for sign-off.

```
Order 1: EC tests (edge cases / robustness)    â€” validates input sanitisation first
Order 2: CQ tests (control quality formula)   â€” validates primary computation
Order 3: TR tests (touch radius)               â€” depends on correct q output
Order 4: BD tests (ball displacement)         â€” depends on correct q and r outputs
Order 5: PR tests (pressure evaluation)        â€” isolated sub-system
Order 6: OR tests (orientation detection)      â€” isolated sub-system
Order 7: PO tests (possession state machine)   â€” depends on q and r
Order 8: IT tests (integration)                â€” requires all units passing
Order 9: VS tests (validation scenarios)       â€” requires integration passing
```

### 5.12.2 Execution Cadence

| Cadence | Tests | Trigger |
|---|---|---|
| On every code change | CQ, TR, PR, OR, PO, EC (all unit tests) | Git pre-commit hook |
| On pull request | All unit + all IT tests | CI pipeline |
| On milestone | All tests including VS | Manual milestone gate |
| Nightly | IT-005 determinism + performance profiling | Scheduled CI job |

**Target total execution time:**
- Unit tests only: < 10 seconds
- Unit + integration: < 60 seconds
- Full suite including VS: < 5 minutes

### 5.12.3 Failure Response Protocol

**Level 1 â€” Formula error (CQ, TR, PR, OR):** Halt further development. Fix before
any integration work. These failures contaminate all downstream tests.

**Level 2 â€” State machine error (PO):** Fix before integration testing. Priority order
errors (PO-006) are particularly critical as they affect gameplay-observable behaviour.

**Level 3 â€” Integration failure (IT):** May proceed with other non-dependent IT tests
for diagnosis. Must resolve before milestone gate.

**Level 4 â€” Validation scenario deviation (VS):** If outputs are within 2Ã— the tolerance,
investigate formula calibration. If outputs are within 10Ã— tolerance, investigate whether
the player archetype attributes need revision. If outputs exceed 10Ã— tolerance, treat as
Level 1 (formula error).

**Level 5 â€” Determinism failure (IT-005):** This is a CRITICAL failure. Halt all
development. Root-cause analysis required before any code proceeds. Possible causes:
floating-point operations with non-deterministic ordering, unintentional state sharing
between evaluations, platform-specific math library differences.

### 5.12.4 Regression Test Strategy

All 61 unit tests serve as the regression suite. When any formula constant is tuned
(e.g., PRESSURE_WEIGHT, MOVEMENT_PENALTY, BLEND_MIN_MAGNITUDE, TOUCH_MAX_BALL_SPEED),
the following tests will likely need expected value recalculation: CQ-002, CQ-003,
CQ-006, CQ-010, CQ-012, BD-003, BD-005, VS-001, VS-002, VS-003.

These tests should be reviewed and updated alongside any constant change. The hand
calculations in Appendix B must be re-run to verify new expected values before updating
the tests.

---

## 5.13 Section Summary

| Subsection | Test Count | Key Content |
|---|---|---|
| **5.2 Control Quality (CQ)** | 12 | Formula validation, bonus isolation, clamps, determinism |
| **5.3 Touch Radius (TR)** | 10 | Band interpolation, continuity at boundaries, monotonicity, velocity modifier |
| **5.4 Pressure (PR)** | 8 | Inverse-square formula, stacking, saturation, boundary conditions |
| **5.5 Orientation (OR)** | 7 | Window bounds, symmetry, Atan2 sign convention |
| **5.6 Possession (PO)** | 8 | All four outcomes, priority ordering, dribbling signal |
| **5.7 Edge Cases (EC)** | 8 | NaN, zero velocity, attribute bounds, pitch boundary, simultaneity |
| **5.8 Ball Displacement (BD)** | 8 | Blend formula, zero-quality/perfect-quality extremes, near-zero fallback, velocity cap, Z zeroing, pitch boundary X and Y |
| **5.9 Integration (IT)** | 8 | Full pipeline, determinism, cross-system data flow |
| **5.10 Validation (VS)** | 3 | Real-world archetype scenarios with complete hand calculations |
| **Total** | **72 tests** | Exceeds outline minimum (25 unit + 8 integration) by 2.6Ã— |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|---|---|---|---|
| Ball Physics #1 Â§3.1.1 | Coordinate system (XY pitch, Z up) | âœ“ | IT-001, BD-001â€“BD-008 use XY plane inputs |
| Ball Physics #1 Â§3.1.2 | Ball.RADIUS = 0.11m | âœ“ | BD-007, BD-008 verify Z = 0.11m in newBallPosition |
| Agent Movement #2 Â§3.5.6 | Technique, FirstTouch [1â€“20] | âœ“ | All CQ tests use [1â€“20] attribute range |
| Agent Movement #2 Â§6.1.2 | DribblingModifier | âœ“ | IT-002, PO-008 verify dribbling signal |
| Collision System #3 Â§4.2.6 | AgentBallCollisionData | âš  Pending | IT-001, IT-007 depend on this interface |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ“ | CQ-011, IT-005 explicitly test determinism |
| Master Vol 1 Â§6 | Touch radius thresholds | âœ“ | All TR tests use Master Vol 1 band values |
| First Touch #4 Â§3.1 | Control quality formula | âœ“ | All CQ derivations reference Â§3.1 steps |
| First Touch #4 Â§3.2 | Touch radius bands | âœ“ | All TR derivations reference Â§3.2.2 |
| First Touch #4 Â§3.3 | Ball displacement model | âœ“ | BD-001â€“BD-008 cover Â§3.3.2â€“Â§3.3.5 |
| First Touch #4 Â§3.4 | Possession state machine | âœ“ | All PO tests reference Â§3.4.2 priority order |
| First Touch #4 Â§3.5 | Pressure formula | âœ“ | All PR derivations reference Â§3.5.2â€“Â§3.5.3 |
| First Touch #4 Â§3.6 | Orientation window | âœ“ | All OR tests reference Â§3.6.2 constants |
| First Touch Outline Â§5.1 | Min 25 unit tests | âœ“ | 61 unit tests provided (2.44Ã— minimum) |
| First Touch Outline Â§5.2 | Min 8 integration tests | âœ“ | 8 integration tests provided |

---

**End of Section 5**

**Page Count:** ~28 pages
**Version:** 1.0
**Next Section:** Section 6 â€” Performance Analysis
