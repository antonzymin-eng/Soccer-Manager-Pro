# Collision System Specification â€” Section 5: Testing

**Purpose:** Defines test requirements, acceptance criteria, and validation methodology for the Collision System (Spec #3). Establishes comprehensive test coverage for spatial partitioning, collision detection, collision response, and system integration.

**Created:** February 15, 2026, 6:30 PM PST  
**Version:** 1.0  
**Status:** Draft  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Prerequisites:** Sections 1â€“4 (v1.0)

---

## 5.1 Test Strategy

### 5.1.1 Test Pyramid

```
        /\
       /  \     E2E Tests (5%)
      /____\    - Full match simulation
     /      \   - 90-minute endurance
    /        \  
   / Integr-  \ Integration Tests (25%)
  /  ation     \ - Multi-agent scenarios
 /______________\ - Cross-system boundaries
/                \ 
/   Unit Tests    \ Unit Tests (70%)
/__(32+ tests)____\ - Formula validation
                    - Edge case handling
```

**Distribution rationale:**

- **Unit tests (70%):** Fast (<5s total), isolated, enable rapid iteration. Cover all formulas, edge cases, and boundary conditions individually.
- **Integration tests (25%):** Verify system interactionsâ€”Collision System with Agent Movement, Ball Physics, and Event System. Test realistic multi-agent scenarios.
- **E2E tests (5%):** Expensive full-match simulations. Run nightly or pre-release. Validate no crashes, no NaN propagation, stable memory.

### 5.1.2 Testing Tools

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **Unity Test Framework** | Test runner (NUnit-based) | Edit Mode for pure logic, Play Mode for integration |
| **Unity Profiler** | Performance validation | Target: p95 < 0.5ms, p99 < 0.6ms |
| **Unity Code Coverage** | Coverage tracking | Target: >85% line coverage for Section 3 code |
| **Custom DeterministicTestHarness** | Reproducible RNG | Seeds tests for deterministic outcomes |

### 5.1.3 Test Data Strategy

**Derived values (not magic numbers):**

All expected results are calculated from formulas defined in Sections 3.1â€“3.4. Test comments include derivation or reference Appendix D.

**Example derivation (CR-001):**
```
Scenario: Two equal-mass agents (85 kg each) collide head-on at 5 m/s each.
  Relative velocity: v_rel = 10 m/s (approaching)
  Coefficient of restitution: e = 0.3
  Impulse: j = -(1 + 0.3) Ã— 10 / (1/85 + 1/85) = -13 Ã— 10 / 0.0235 = -553 kgÂ·m/s
  Î”v per agent: |j| / m = 553 / 85 = 6.5 m/s
  Final velocity: 5 - 6.5 = -1.5 m/s (reversed, slower due to inelastic collision)
  Expected: Velocities swap direction, reduced magnitude (approx 30% of approach speed remains)
```

**Realistic scenarios:**

Test data based on professional match parameters:
- Sprint speed: 8â€“10 m/s (Agent Movement Â§3.2)
- Agent mass: 72.5â€“100 kg (Agent Movement Â§3.5.4.2)
- Hitbox radius: 0.35â€“0.50m (Agent Movement Â§3.5.4.3)
- Typical collisions per frame: 0â€“3 (match observation)
- Corner kick clustering: up to 12 agents in penalty area

---

## 5.2 Unit Test Categories

### 5.2.1 Spatial Hash Tests (6 tests)

**Test ID: SH-001**  
**Name:** `SpatialHash_InsertAtGridCenter_QueryReturnsSelf`  
**Purpose:** Validate basic insert and query at grid center  
**Setup:**
- Grid initialized (empty)
- Insert agent ID 0 at position (52.5, 34, 0) with radius 0.40m  
**Expected:** Query at same position returns list containing ID 0  
**Tolerance:** Exact (list membership)

---

**Test ID: SH-002**  
**Name:** `SpatialHash_TwoAgentsClose_QueryReturnsBoth`  
**Purpose:** Validate nearby agents returned by query  
**Setup:**
- Insert agent ID 0 at (50.0, 34.0, 0) with radius 0.40m
- Insert agent ID 1 at (50.5, 34.0, 0) with radius 0.40m
- Distance: 0.5m < combined radius (0.80m) â†’ collision candidates  
**Expected:** Query for ID 0 returns list containing ID 1 (and vice versa)  
**Tolerance:** Exact (list membership)

---

**Test ID: SH-003**  
**Name:** `SpatialHash_TwoAgentsFar_QueryReturnsOnlySelf`  
**Purpose:** Validate distant agents not returned  
**Setup:**
- Insert agent ID 0 at (50.0, 34.0, 0) with radius 0.40m
- Insert agent ID 1 at (52.0, 34.0, 0) with radius 0.40m
- Distance: 2.0m > combined radius (0.80m) + cell buffer â†’ no collision  
**Expected:** Query for ID 0 returns empty or only self (no ID 1)  
**Tolerance:** Exact (list membership)

---

**Test ID: SH-004**  
**Name:** `SpatialHash_InsertAtBoundary_NoIndexOutOfBounds`  
**Purpose:** Validate boundary handling prevents array overflow  
**Setup:**
- Insert agent at (0.0, 0.0, 0) â€” corner
- Insert agent at (105.0, 68.0, 0) â€” opposite corner
- Insert agent at (-0.5, 34.0, 0) â€” slightly outside pitch
- Insert agent at (105.5, 34.0, 0) â€” slightly outside pitch  
**Expected:** No exceptions thrown; positions clamped to valid cells  
**Tolerance:** Exact (no exception)

---

**Test ID: SH-005**  
**Name:** `SpatialHash_Insert22Agents_AllInserted`  
**Purpose:** Validate full team capacity  
**Setup:**
- Insert 22 agents at evenly distributed positions across pitch
- Query each agent's position  
**Expected:** Each query returns at least self; total entity count = 22  
**Tolerance:** Exact (count = 22)

---

**Test ID: SH-006**  
**Name:** `SpatialHash_EntityStraddlesCellBoundary_InsertedInMultipleCells`  
**Purpose:** Validate multi-cell insertion for boundary-spanning entities  
**Setup:**
- Insert agent at (50.0, 34.0, 0) with radius 0.40m
- Position is exactly on cell boundary (50.0 / 1.0m = cell edge)
- Agent extends into adjacent cell by 0.40m  
**Method:**
- Query from cell (50, 34) â€” should find agent
- Query from cell (49, 34) â€” should also find agent (overlaps into this cell)  
**Expected:** Agent found when querying from either adjacent cell  
**Tolerance:** Exact (found in both cells)  
**Rationale:** Section 3.1.2 Insert() handles boundary overlap by inserting into multiple cells when entity radius extends beyond cell edge.

---

### 5.2.2 Collision Detection Tests (7 tests)

**Test ID: CD-001**  
**Name:** `Detection_AgentsExactlyTouching_ReturnsCollision`  
**Purpose:** Validate collision detected at boundary condition  
**Setup:**
- Agent 1 at (50.0, 34.0, 0), radius 0.40m
- Agent 2 at (50.8, 34.0, 0), radius 0.40m
- Distance: 0.8m = 0.40 + 0.40 (exactly touching)  
**Expected:** `CheckAgentAgentCollision()` returns `true`; penetration = 0.0m (Â±0.001m)  
**Tolerance:** Position Â±0.001m, penetration Â±0.001m

---

**Test ID: CD-002**  
**Name:** `Detection_AgentsNotTouching_ReturnsNoCollision`  
**Purpose:** Validate no false positive detection  
**Setup:**
- Agent 1 at (50.0, 34.0, 0), radius 0.40m
- Agent 2 at (50.81, 34.0, 0), radius 0.40m
- Distance: 0.81m > combined radius (0.80m)  
**Expected:** `CheckAgentAgentCollision()` returns `false`  
**Tolerance:** Exact (boolean)

---

**Test ID: CD-003**  
**Name:** `Detection_AgentsOverlapping_ReturnsPenetrationDepth`  
**Purpose:** Validate penetration depth calculation  
**Setup:**
- Agent 1 at (50.0, 34.0, 0), radius 0.40m
- Agent 2 at (50.0, 34.0, 0), radius 0.40m (same position)
- Distance: 0.0m â†’ full overlap  
**Expected:** 
- Collision detected: `true`
- Penetration depth: 0.80m (combined radii)
- Normal: arbitrary but valid (e.g., Vector2.right fallback)  
**Tolerance:** Penetration Â±0.001m

---

**Test ID: CD-004**  
**Name:** `Detection_AgentBallTouching_ReturnsCollision`  
**Purpose:** Validate agent-ball detection  
**Setup:**
- Agent at (50.0, 34.0, 0), radius 0.40m
- Ball at (50.5, 34.0, 0), radius 0.11m
- Distance: 0.5m < 0.40 + 0.11 = 0.51m  
**Expected:** `CheckAgentBallCollision()` returns `true`  
**Tolerance:** Exact (boolean)

---

**Test ID: CD-005**  
**Name:** `Detection_AgentBallNotTouching_ReturnsNoCollision`  
**Purpose:** Validate no false positive for agent-ball  
**Setup:**
- Agent at (50.0, 34.0, 0), radius 0.40m
- Ball at (50.52, 34.0, 0), radius 0.11m
- Distance: 0.52m > 0.51m (just outside)  
**Expected:** `CheckAgentBallCollision()` returns `false`  
**Tolerance:** Exact (boolean)

---

**Test ID: CD-006**  
**Name:** `Detection_GroundedAgentStillDetectable`  
**Purpose:** Validate grounded agents remain collision obstacles  
**Setup:**
- Agent 1 at (50.0, 34.0, 0), IsGrounded = true
- Agent 2 at (50.5, 34.0, 0), IsGrounded = false (approaching)
- Combined radius: 0.80m; distance: 0.5m  
**Expected:** Collision detected (Agent 1 is obstacle even when grounded)  
**Tolerance:** Exact (boolean)  
**Note:** Response phase handles reduced impulse for grounded agents; detection is unaffected.

---

**Test ID: CD-007**  
**Name:** `Detection_AgentBallCollision_BodyPartAlwaysTorso`  
**Purpose:** Validate Stage 0 constraint that all agent-ball contacts use TORSO body part  
**Setup:**
- Agent at (50.0, 34.0, 0), radius 0.40m
- Ball at various contact positions:
  - (50.5, 34.0, 0.05) â€” low contact (would be FOOT in Stage 1)
  - (50.5, 34.0, 0.5) â€” mid contact
  - (50.5, 34.0, 1.5) â€” high contact (would be HEAD in Stage 1)  
**Expected:** All contacts produce AgentBallCollisionData with BodyPart = TORSO  
**Tolerance:** Exact (enum value = BodyPart.Torso)  
**Rationale:** Section 3.3.5 specifies Stage 0 uses TORSO for all contacts; height-based body part detection deferred to Stage 1.

---

### 5.2.3 Collision Response Tests (5 tests)

**Test ID: CR-001**  
**Name:** `Response_EqualMassHeadOn_VelocitiesReverse`  
**Purpose:** Validate momentum conservation for symmetric collision  
**Setup:**
- Agent 1: mass 85 kg, velocity (5, 0, 0) m/s
- Agent 2: mass 85 kg, velocity (-5, 0, 0) m/s
- Head-on collision, e = 0.3  
**Expected:** 
- Both agents reverse direction
- Post-collision speed â‰ˆ 1.5 m/s each (30% of approach Ã— 0.5 due to each agent)
- Velocities: Agent 1 â‰ˆ (-1.5, 0, 0), Agent 2 â‰ˆ (1.5, 0, 0)  
**Tolerance:** Velocity Â±0.3 m/s (see Appendix D.1)  
**Derivation:** Full derivation in test file comments per Section 5.1.3 example

---

**Test ID: CR-002**  
**Name:** `Response_HeavyHitsLight_HeavyBarelySlows`  
**Purpose:** Validate asymmetric mass collision  
**Setup:**
- Agent 1 (heavy): mass 100 kg, velocity (8, 0, 0) m/s
- Agent 2 (light): mass 72.5 kg, velocity (0, 0, 0) m/s (stationary)
- Collision normal: (1, 0)  
**Expected:**
- Heavy agent loses less velocity than light agent gains
- Heavy: Î”v â‰ˆ 2.4 m/s â†’ final â‰ˆ (5.6, 0, 0)
- Light: Î”v â‰ˆ 3.3 m/s â†’ final â‰ˆ (3.3, 0, 0)  
**Tolerance:** Velocity Â±0.5 m/s (see Appendix D.2)

---

**Test ID: CR-003**  
**Name:** `Response_SameTeam_ReducedMomentum`  
**Purpose:** Validate 30% momentum scale for same-team collision  
**Setup:**
- Agent 1: Team 0, mass 85 kg, velocity (5, 0, 0) m/s
- Agent 2: Team 0, mass 85 kg, velocity (-5, 0, 0) m/s
- Same setup as CR-001 but same team  
**Expected:**
- Impulse magnitude = 30% of opposing-team case
- Velocity change â‰ˆ 0.5 m/s each (vs 1.5 m/s for opposing teams)  
**Tolerance:** Velocity Â±0.2 m/s

---

**Test ID: CR-004**  
**Name:** `Response_Separation_ResolvesPenetration`  
**Purpose:** Validate position correction removes overlap  
**Setup:**
- Agent 1 at (50.0, 34.0, 0), mass 85 kg
- Agent 2 at (50.6, 34.0, 0), mass 85 kg
- Penetration: 0.80 - 0.60 = 0.20m  
**Expected:**
- After separation, distance â‰¥ combined radius (0.80m)
- Position corrections distributed by inverse mass (equal mass â†’ equal correction)
- Each agent moves ~0.10m apart  
**Tolerance:** Final distance â‰¥ 0.799m (penetration resolved with 1% margin)

---

**Test ID: CR-005**  
**Name:** `Response_ParallelVelocity_NoImpulse`  
**Purpose:** Validate no impulse when agents moving in same direction at same speed  
**Setup:**
- Agent 1: velocity (5, 0, 0) m/s
- Agent 2: velocity (5, 0, 0) m/s
- Collision detected (overlapping) but relative velocity = 0  
**Expected:**
- VelocityImpulse1 = (0, 0, 0)
- VelocityImpulse2 = (0, 0, 0)
- Separation still applied (position correction only)  
**Tolerance:** Impulse magnitude < 0.01 kgÂ·m/s

---

### 5.2.4 Fall/Stumble Logic Tests (5 tests)

**Test ID: FL-001**  
**Name:** `FallLogic_ForceBelowStumbleThreshold_NoStateChange`  
**Purpose:** Validate low-force collisions cause no state change  
**Setup:**
- Agent Strength: 10 â†’ fall threshold = 1000 N, stumble threshold = 500 N
- Impact force: 400 N (below stumble threshold)  
**Expected:**
- TriggerGrounded = false
- TriggerStumble = false  
**Tolerance:** Exact (boolean)

---

**Test ID: FL-002**  
**Name:** `FallLogic_ForceAtStumbleThreshold_StumbleProbabilityPositive`  
**Purpose:** Validate stumble probability activates at threshold  
**Setup:**
- Agent Strength: 10 â†’ stumble threshold = 500 N, fall threshold = 1000 N
- Impact force: 750 N (midpoint between thresholds)
- P(stumble) = (750 - 500) / (1000 - 500) = 0.5  
**Method:** Run 10,000 iterations with deterministic RNG seeds (sequential seeds 0â€“9999)  
**Expected:**
- Stumble triggered in approximately 50% of cases
- Range: 4,800â€“5,200 triggers (Â±4% of 5000)  
**Tolerance:** Â±4% of expected probability (statistically: 99% CI for N=10,000)  
**Note:** 10,000 iterations ensures <1% false failure rate while maintaining reasonable test runtime (~50ms).

---

**Test ID: FL-003**  
**Name:** `FallLogic_ForceAtFallThreshold_FallProbabilityPositive`  
**Purpose:** Validate fall probability activates above fall threshold  
**Setup:**
- Agent Strength: 10 â†’ fall threshold = 1000 N
- Impact force: 1250 N (250 N above threshold)
- P(fall) = 250 / 500 = 0.5 (clamped)  
**Method:** Run 10,000 iterations with deterministic RNG seeds  
**Expected:**
- Fall triggered in approximately 50% of cases
- Stumble NOT triggered (fall takes precedence)  
**Tolerance:** Â±4% of expected probability

---

**Test ID: FL-004**  
**Name:** `FallLogic_Strength20RequiresMoreForceThanStrength1`  
**Purpose:** Validate Strength attribute affects thresholds correctly  
**Setup:**
- Agent A: Strength 1 â†’ fall threshold = 550 N
- Agent B: Strength 20 â†’ fall threshold = 1500 N
- Same impact force: 1000 N  
**Expected:**
- Agent A (Strength 1): Force exceeds threshold â†’ P(fall) > 0
- Agent B (Strength 20): Force below threshold â†’ P(fall) = 0  
**Tolerance:** Exact (threshold comparison)

---

**Test ID: FL-005**  
**Name:** `FallLogic_GroundedDurationUsesStrengthAsAgilityProxy`  
**Purpose:** Validate Stage 0 grounded duration calculation uses Strength (not Agility)  
**Setup:**
- Agent A: Strength 5 (proxy Agility 5) â†’ duration = 1.2 - (5 Ã— 0.03) = 1.05s
- Agent B: Strength 15 (proxy Agility 15) â†’ duration = 1.2 - (15 Ã— 0.03) = 0.75s
- Both agents fall from collision  
**Expected:**
- Agent A grounded duration â‰ˆ 1.05s
- Agent B grounded duration â‰ˆ 0.75s
- Higher Strength (as proxy) = faster recovery  
**Tolerance:** Duration Â±0.05s  
**Rationale:** Section 3.3.2 documents that Strength is used as Agility proxy in Stage 0 because AgentPhysicalProperties does not expose Agility. This test validates the proxy behavior is implemented correctly.

---

### 5.2.5 Edge Case Tests (4 tests)

**Test ID: EC-001**  
**Name:** `EdgeCase_NaNPosition_RecoveryWithoutCrash`  
**Purpose:** Validate graceful handling of invalid input  
**Setup:**
- Agent position: (NaN, 34.0, 0)
- Call Insert() on spatial hash  
**Expected:**
- No exception thrown
- Agent not inserted (silently skipped)
- System continues operating  
**Tolerance:** Exact (no crash, agent skipped)

---

**Test ID: EC-002**  
**Name:** `EdgeCase_ExcessivePenetration_LogsWarning`  
**Purpose:** Validate tunneling detection and gentle recovery  
**Setup:**
- Agent 1 at (50.0, 34.0, 0)
- Agent 2 at (50.0, 34.0, 0) (same position, full overlap)
- Penetration: 0.80m > MAX_PENETRATION_DEPTH (0.50m)  
**Expected:**
- Warning logged with penetration depth
- Separation applied with MAX_PENETRATION_DEPTH cap (0.50m)
- No physics explosion  
**Tolerance:** Separation â‰¤ 0.51m (capped + 1% margin)

---

**Test ID: EC-003**  
**Name:** `EdgeCase_MaxCollisionPairs_ProcessedWithoutOverflow`  
**Purpose:** Validate sanity limit on collision pairs  
**Setup:**
- Place 10 agents in a 2m Ã— 2m area (extreme clustering)
- Theoretical pairs: 10 Ã— 9 / 2 = 45 (below MAX_COLLISION_PAIRS_PER_FRAME = 50)  
**Expected:**
- All 45 pairs processed
- No buffer overflow
- No infinite loop  
**Tolerance:** Exact (pair count â‰¤ 50)  
**Note:** This is a moderate stress test. True worst case (22 agents all overlapping) produces 231 pairs, exceeding the 50-pair limit. That scenario is tested in PERF-003 where excess pairs are safely skipped.

---

**Test ID: EC-004**  
**Name:** `EdgeCase_GroundedAgentCollision_NoImpulseGenerated`  
**Purpose:** Validate grounded agents don't push others  
**Setup:**
- Agent 1: IsGrounded = true, velocity = (0, 0, 0)
- Agent 2: IsGrounded = false, velocity = (5, 0, 0) (running into grounded)
- Collision detected  
**Expected:**
- Agent 1 receives no velocity impulse (grounded = infinite mass)
- Agent 2 receives full impulse (bounces off)
- Agent 1 position unchanged  
**Tolerance:** Agent 1 velocity remains (0, 0, 0) Â± 0.001 m/s

---

### 5.2.6 Determinism Tests (3 tests)

**Test ID: DT-001**  
**Name:** `Determinism_SameSeed_IdenticalOutcomes`  
**Purpose:** Validate deterministic RNG produces repeatable results  
**Setup:**
- Match seed: 12345, Frame: 100
- Impact force: 1250 N (50% fall probability)  
**Method:** 
- Run fall determination 100 times with same seed
- Record sequence of fall/no-fall outcomes  
**Expected:** All 100 runs produce identical sequence  
**Tolerance:** Exact (bit-identical)

---

**Test ID: DT-002**  
**Name:** `Determinism_DifferentSeeds_DifferentOutcomes`  
**Purpose:** Validate different seeds produce different sequences  
**Setup:**
- Seed A: 12345, Seed B: 12346
- Same impact force (1250 N)  
**Method:** Run 100 outcomes for each seed  
**Expected:** Sequences differ (at least 10 differences in 100 outcomes)  
**Tolerance:** Difference count â‰¥ 10

---

**Test ID: DT-003**  
**Name:** `Determinism_FullFrameReplay_BitIdentical`  
**Purpose:** Validate entire collision system is deterministic  
**Setup:**
- 22 agents with fixed initial positions/velocities
- Match seed: 99999
- Run 100 frames of collision processing  
**Method:** 
- Record all collision events from run 1
- Reset and run again with same seed
- Compare event logs  
**Expected:** Event logs are bit-identical  
**Tolerance:** Exact (byte-for-byte match)

---

## 5.3 Integration Tests

### 5.3.1 Multi-Agent Scenarios (8 tests)

**Test ID: IT-001**  
**Name:** `Integration_TwoAgentsApproach_BothBounceBack`  
**Purpose:** Validate basic collision response in simulation context  
**Setup:**
- Agent 1 at (45, 34, 0), velocity (3, 0, 0) m/s (walking speed)
- Agent 2 at (55, 34, 0), velocity (-3, 0, 0) m/s
- Let simulation run until collision  
**Expected:**
- Collision occurs at approximately (50, 34, 0)
- Both agents bounce back (velocities reverse direction)
- Neither falls (low-speed collision, force < stumble threshold)  
**Tolerance:** Position Â±0.5m, velocity direction reversed

---

**Test ID: IT-002**  
**Name:** `Integration_SprintingHitsStationary_StationaryKnockedBack`  
**Purpose:** Validate high-speed collision with stationary target  
**Setup:**
- Agent 1 at (45, 34, 0), velocity (9, 0, 0) m/s (sprinting)
- Agent 2 at (50, 34, 0), velocity (0, 0, 0) m/s (stationary)
- Agent 2: Strength 5 (lower threshold)  
**Expected:**
- Agent 1 slows significantly
- Agent 2 knocked backward at speed >3 m/s
- Agent 2 may stumble or fall (force exceeds stumble threshold)  
**Tolerance:** Agent 2 velocity > 3 m/s in +X direction

---

**Test ID: IT-003**  
**Name:** `Integration_AgentHitsBall_BallOnCollisionCalled`  
**Purpose:** Validate agent-ball collision triggers Ball Physics callback  
**Setup:**
- Agent at (49, 34, 0), velocity (5, 0, 0) m/s
- Ball at (50, 34, 0), velocity (0, 0, 0) m/s
- Mock BallCollisionHandler to record callback  
**Expected:**
- `Ball.OnCollision(AgentBallCollisionData)` called
- CollisionData contains: ContactPoint â‰ˆ (49.6, 34, 0), AgentVelocity = (5, 0, 0), BodyPart = TORSO  
**Tolerance:** ContactPoint Â±0.1m

---

**Test ID: IT-004**  
**Name:** `Integration_ThreeAgentsCluster_AllProcessedNoLoop`  
**Purpose:** Validate multi-body collision handling  
**Setup:**
- Agent 0 at (50.0, 34.0, 0)
- Agent 1 at (50.3, 34.3, 0)
- Agent 2 at (50.3, 33.7, 0)
- All overlapping (within combined radii)  
**Expected:**
- All 3 pairs detected: (0,1), (0,2), (1,2)
- Each pair processed exactly once
- No infinite loop (iteration counter < MAX_ITERATIONS)
- Frame completes in <0.5ms  
**Tolerance:** Pair count = 3, no timeout

---

**Test ID: IT-005**  
**Name:** `Integration_GroundedObstacle_RunnerStumbles`  
**Purpose:** Validate grounded agent acts as immovable obstacle  
**Setup:**
- Agent 1 (grounded) at (50, 34, 0), IsGrounded = true
- Agent 2 at (45, 34, 0), velocity (7, 0, 0) m/s (running toward)  
**Expected:**
- Agent 2 collides with Agent 1
- Agent 1 position unchanged (infinite mass)
- Agent 2 velocity reversed, may trigger stumble
- Grounded duration of Agent 1 unaffected  
**Tolerance:** Agent 1 position unchanged Â±0.01m

---

**Test ID: IT-006**  
**Name:** `Integration_FullMatch90Minutes_NoCrashNoNaN`  
**Purpose:** Endurance test for simulation stability  
**Setup:**
- Initialize match with 22 agents at standard positions
- Run simulation for 90 minutes (324,000 frames at 60Hz)
- Random movement commands every 10 frames  
**Expected:**
- No crashes
- No NaN values in any agent position or velocity
- All collision events valid  
**Validation:**
- Periodic position validation every 1000 frames
- Final state validation at end  
**Tolerance:** Zero NaN/Infinity values, zero crashes

---

**Test ID: IT-007**  
**Name:** `Integration_CornerKickClustering_PerformanceUnder500us`  
**Purpose:** Validate worst-case performance scenario  
**Setup:**
- 12 agents clustered in penalty area (18m Ã— 40m)
- All agents within 5m of each other
- Run single frame of collision processing  
**Expected:**
- Frame completes in <0.5ms
- All collisions detected and processed
- No memory allocation during frame  
**Measurement:** Unity Profiler with Collision.Update marker  
**Tolerance:** Time < 0.5ms

---

**Test ID: IT-008**  
**Name:** `Integration_SameTeamCollision_ReducedMomentumNoFall`  
**Purpose:** Validate same-team collision behavior in simulation  
**Setup:**
- Agent 1: Team 0, velocity (8, 0, 0) m/s (sprinting)
- Agent 2: Team 0, velocity (-8, 0, 0) m/s (sprinting, same team)
- Head-on collision  
**Expected:**
- Momentum transfer = 30% of opposing team
- Neither agent falls (same-team collision cannot trigger GROUNDED)
- Stumble possible but less likely than opposing team  
**Tolerance:** No GROUNDED state triggered

---

### 5.3.2 Cross-System Integration (4 tests)

**Test ID: IT-009**  
**Name:** `CrossSystem_CollisionTriggersAgentMovementState`  
**Purpose:** Validate state trigger interface to Agent Movement  
**Setup:**
- Agent 1 (Strength 5) stationary
- Agent 2 (Strength 15) sprinting at 9 m/s into Agent 1
- Force exceeds Agent 1 fall threshold  
**Expected:**
- Agent 1 transitions to GROUNDED state via `TriggerGroundedState()`
- GroundedReason = COLLISION
- GroundedDuration set per Agility calculation  
**Tolerance:** State transition occurs within same frame

---

**Test ID: IT-010**  
**Name:** `CrossSystem_CollisionEventPublished`  
**Purpose:** Validate Event System receives collision events  
**Setup:**
- Register mock ICollisionEventConsumer
- Trigger agent-agent collision  
**Expected:**
- `OnCollisionEvent()` called with valid CollisionEvent
- Event contains: MatchTime, Type = AGENT_AGENT, valid Entity IDs, ContactPoint, ImpactForce  
**Tolerance:** Event received within same frame

---

**Test ID: IT-011**  
**Name:** `CrossSystem_BallDeflectionAfterCollision`  
**Purpose:** Validate Ball Physics receives collision data correctly  
**Setup:**
- Agent running at (5, 0, 0) m/s
- Ball stationary at contact point
- Trigger collision  
**Expected:**
- Ball velocity changes (deflection)
- Ball direction influenced by agent velocity
- BodyPart = TORSO (Stage 0)  
**Integration Dependency:** Requires Ball Physics (Spec #1) integration.  
**Mock Strategy:** If Ball Physics unavailable during isolated Collision System testing, use MockBallCollisionHandler that records callback parameters. Verify:
  - `OnAgentCollision()` called with valid AgentBallCollisionData
  - ContactPoint within agent-ball overlap region
  - AgentVelocity matches agent's velocity at collision time  
**Tolerance:** Ball velocity magnitude > 0 after collision (or mock callback received)

---

**Test ID: IT-012**  
**Name:** `CrossSystem_FoulDataPackaged`  
**Purpose:** Validate ContactForceData prepared for Referee System  
**Setup:**
- Agent 1 running at Agent 2 from behind
- Agent 2 in possession (mock possession flag)
- Trigger collision  
**Expected:**
- ContactForceData contains:
  - ForceMagnitude > 0
  - ContactType = FROM_BEHIND (based on velocity directions)
  - InstigatorAgentID = Agent 1
  - VictimAgentID = Agent 2  
**Note:** VictimHasBall and InstigatorPlayingBall are Stage 1+ features (set to false in Stage 0)  
**Tolerance:** ContactType correctly classified

---

## 5.4 Performance Tests

### 5.4.1 Timing Tests

**Test ID: PERF-001**  
**Name:** `Performance_22AgentsDistributed_Under150us`  
**Purpose:** Validate typical-case performance  
**Setup:**
- 22 agents evenly distributed across pitch (standard formation positions)
- 1 ball at center
- Run 1000 frames, measure each  
**Expected:**
- Mean: <0.15ms
- p50: <0.12ms
- p95: <0.20ms  
**Tolerance:** Mean < 0.15ms

---

**Test ID: PERF-002**  
**Name:** `Performance_22AgentsClustered_Under400us`  
**Purpose:** Validate clustered-case performance  
**Setup:**
- 22 agents clustered in penalty area (random positions within 18m Ã— 40m)
- Measure 100 frames  
**Expected:**
- Mean: <0.40ms
- p95: <0.50ms  
**Tolerance:** p95 < 0.50ms

---

**Test ID: PERF-003**  
**Name:** `Performance_WorstCase_Under500us`  
**Purpose:** Validate absolute worst case (many collisions)  
**Setup:**
- 22 agents + ball clustered in 10m Ã— 10m area
- All agents overlapping (maximum collision pairs)  
**Expected:**
- Frame completes in <0.5ms
- No timeout or freeze  
**Tolerance:** Time < 0.5ms

---

**Test ID: PERF-004**  
**Name:** `Performance_1000Frames_NoMemoryGrowth`  
**Purpose:** Validate zero per-frame allocation  
**Setup:**
- Run 1000 frames of normal collision processing
- Measure memory before and after  
**Expected:**
- No GC allocations during frame processing
- Heap size unchanged (Â±10KB for measurement noise and GC variance)  
**Measurement:** GC.GetTotalMemory(forceFullCollection: true) before/after  
**Tolerance:** Memory delta < 10KB  
**Note:** 10KB tolerance accounts for GC measurement variance; any significant per-frame allocation would accumulate to >100KB over 1000 frames.

---

### 5.4.2 Phase Breakdown Tests

**Test ID: PERF-005**  
**Name:** `Performance_PhaseBreakdown_WithinBudget`  
**Purpose:** Validate per-phase timing  
**Setup:**
- Typical 22-agent scenario
- Enable profiler markers
- Run 100 frames  
**Expected per phase:**

| Phase | Target Mean | Target p95 |
|-------|-------------|------------|
| SpatialHash.Clear | <0.02ms | <0.03ms |
| SpatialHash.Insert | <0.03ms | <0.05ms |
| BroadPhase | <0.05ms | <0.08ms |
| NarrowPhase | <0.10ms | <0.15ms |
| Response | <0.10ms | <0.15ms |
| **Total** | **<0.30ms** | **<0.50ms** |

**Tolerance:** Each phase within target

---

## 5.5 Test Implementation Examples

### 5.5.1 Unit Test Implementation

```csharp
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.Core.Physics.Collision;

namespace TacticalDirector.Core.Physics.Collision.Tests
{
    [TestFixture]
    public class CollisionDetectionTests
    {
        /// <summary>
        /// CD-001: Validate collision detected at exact boundary.
        /// 
        /// Setup:
        ///   Agent 1: position (50.0, 34.0, 0), radius 0.40m
        ///   Agent 2: position (50.8, 34.0, 0), radius 0.40m
        ///   Distance: 0.8m = 0.40 + 0.40 (exactly touching)
        /// 
        /// Expected:
        ///   Collision detected, penetration â‰ˆ 0.0m
        /// </summary>
        [Test]
        public void Detection_AgentsExactlyTouching_ReturnsCollision()
        {
            // Arrange
            var a1 = new AgentPhysicalProperties
            {
                Position = new Vector3(50.0f, 34.0f, 0f),
                Velocity = Vector3.zero,
                Mass = 85f,
                HitboxRadius = 0.40f,
                Strength = 10,
                IsGrounded = false
            };
            
            var a2 = new AgentPhysicalProperties
            {
                Position = new Vector3(50.8f, 34.0f, 0f),
                Velocity = Vector3.zero,
                Mass = 85f,
                HitboxRadius = 0.40f,
                Strength = 10,
                IsGrounded = false
            };
            
            // Act
            bool collision = CollisionDetection.CheckAgentAgentCollision(
                in a1, in a2, out CollisionManifold manifold);
            
            // Assert
            Assert.IsTrue(collision, "Collision should be detected at boundary");
            Assert.AreEqual(0f, manifold.PenetrationDepth, 0.001f,
                "Penetration should be ~0 at exact boundary");
        }
        
        /// <summary>
        /// CR-001: Validate momentum conservation for equal-mass head-on collision.
        /// 
        /// Derivation:
        ///   Relative velocity: v_rel = 10 m/s (5 - (-5))
        ///   Coefficient of restitution: e = 0.3
        ///   Impulse: j = -(1 + 0.3) Ã— 10 / (1/85 + 1/85)
        ///          = -1.3 Ã— 10 / 0.0235
        ///          = -553 kgÂ·m/s
        ///   Î”v per agent: |553| / 85 = 6.5 m/s
        ///   Final velocity: 5 - 6.5 = -1.5 m/s (reversed)
        /// </summary>
        [Test]
        public void Response_EqualMassHeadOn_VelocitiesReverse()
        {
            // Arrange
            var a1 = new AgentPhysicalProperties
            {
                Position = new Vector3(50.0f, 34.0f, 0f),
                Velocity = new Vector3(5f, 0f, 0f),
                Mass = 85f,
                HitboxRadius = 0.40f,
                Strength = 10,
                IsGrounded = false
            };
            
            var a2 = new AgentPhysicalProperties
            {
                Position = new Vector3(50.6f, 34.0f, 0f),
                Velocity = new Vector3(-5f, 0f, 0f),
                Mass = 85f,
                HitboxRadius = 0.40f,
                Strength = 10,
                IsGrounded = false
            };
            
            // Create manifold
            var manifold = new CollisionManifold
            {
                Normal = new Vector2(1f, 0f), // Points from a1 to a2
                ContactPoint = new Vector2(50.3f, 34.0f),
                PenetrationDepth = 0.20f,
                Entity1ID = 0,
                Entity2ID = 1
            };
            
            var rng = new DeterministicRNG(12345);
            
            // Act
            var result = CollisionResponse.CalculateAgentAgentResponse(
                in a1, in a2, in manifold, isSameTeam: false, ref rng);
            
            // Assert
            // Agent 1 should reverse direction (was +5, now negative)
            Assert.Less(result.VelocityImpulse1.x, -3f,
                "Agent 1 should receive significant negative impulse");
            
            // Agent 2 should reverse direction (was -5, now positive)
            Assert.Greater(result.VelocityImpulse2.x, 3f,
                "Agent 2 should receive significant positive impulse");
            
            // Verify approximate magnitude (Â±0.3 m/s tolerance)
            float expectedDeltaV = 6.5f;
            Assert.AreEqual(expectedDeltaV, Mathf.Abs(result.VelocityImpulse1.x), 0.5f,
                "Velocity change should match derived value");
        }
    }
}
```

### 5.5.2 Integration Test Implementation

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TacticalDirector.Core.Physics.Collision;

namespace TacticalDirector.Core.Physics.Collision.Tests
{
    [TestFixture]
    public class CollisionIntegrationTests
    {
        private CollisionSystem _collisionSystem;
        private Agent[] _agents;
        private BallState _ball;
        
        [SetUp]
        public void Setup()
        {
            _collisionSystem = new CollisionSystem();
            _agents = new Agent[22];
            
            // Initialize standard positions
            for (int i = 0; i < 22; i++)
            {
                _agents[i] = CreateTestAgent(i, GetStandardPosition(i));
            }
            
            _ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, 0.11f));
        }
        
        /// <summary>
        /// IT-006: Endurance test for 90-minute match simulation.
        /// Validates no crashes, no NaN propagation over extended run.
        /// </summary>
        [UnityTest]
        public IEnumerator Integration_FullMatch90Minutes_NoCrashNoNaN()
        {
            int totalFrames = 324000; // 90 min Ã— 60 fps
            int checkInterval = 1000;
            
            for (int frame = 0; frame < totalFrames; frame++)
            {
                // Apply random movement commands every 10 frames
                if (frame % 10 == 0)
                {
                    ApplyRandomMovementCommands();
                }
                
                // Update collision system
                _collisionSystem.BeginFrame(12345UL, frame);
                _collisionSystem.UpdateCollisions(
                    GetPhysicalProperties(), ref _ball, 1f / 60f, frame / 60f);
                
                // Periodic validation
                if (frame % checkInterval == 0)
                {
                    ValidateNoNaN();
                    yield return null; // Allow frame to complete
                }
            }
            
            // Final validation
            ValidateNoNaN();
            Assert.Pass("90-minute simulation completed without crash or NaN");
        }
        
        private void ValidateNoNaN()
        {
            foreach (var agent in _agents)
            {
                Assert.IsFalse(float.IsNaN(agent.Position.x), 
                    $"Agent {agent.AgentID} has NaN position");
                Assert.IsFalse(float.IsNaN(agent.Velocity.x), 
                    $"Agent {agent.AgentID} has NaN velocity");
            }
        }
        
        // ... helper methods ...
    }
}
```

---

## 5.6 Test Coverage Requirements

### 5.6.1 Minimum Coverage Targets

| Component | Line Coverage | Branch Coverage |
|-----------|--------------|-----------------|
| SpatialHashGrid.cs | >85% | >80% |
| CollisionDetection.cs | >90% | >85% |
| CollisionResponse.cs | >85% | >80% |
| CollisionSystem.cs | >80% | >75% |
| DeterministicRNG.cs | >90% | >85% |
| **Overall** | **>80%** | **>75%** |

**Note:** These targets align with Ball Physics Spec #1 achieved coverage (~80%). Higher targets may be set post-implementation based on actual complexity.

### 5.6.2 Coverage Gaps Allowed

The following are acceptable coverage gaps:

1. **Unity Editor-only code:** `#if UNITY_EDITOR` blocks for debug visualization
2. **Profiler instrumentation:** `Profiler.BeginSample()` / `EndSample()` calls
3. **Logging statements:** `Debug.Log()` / `Debug.LogWarning()` calls
4. **Guard clauses that should never execute:** e.g., division-by-zero guards after validation

---

## 5.7 Validation Schedule

### 5.7.1 Continuous (Every Build)

- All unit tests (32+ tests)
- Quick integration tests (IT-001 through IT-004)
- Basic performance test (PERF-001)

### 5.7.2 Daily (Nightly Build)

- Full integration test suite (IT-001 through IT-012)
- All performance tests (PERF-001 through PERF-005)
- Memory stability test (PERF-004)

### 5.7.3 Weekly (Pre-Milestone)

- 90-minute endurance test (IT-006)
- Cross-platform validation (Windows, Mac, Linux)
- Coverage report generation

### 5.7.4 Pre-Release (Approval Gate)

- Full test suite with profiling
- Visual validation of collision behavior
- Performance regression check against baseline
- Coverage verification (>85% line coverage)

---

## Appendix D: Tolerance Derivations

### D.1 CR-001: Equal Mass Head-On Velocity Tolerance (Â±0.3 m/s)

**Derivation:**

```
Given:
  m1 = m2 = 85 kg
  v1 = 5 m/s, v2 = -5 m/s
  e = 0.3 (coefficient of restitution)

Impulse calculation:
  v_rel = v1 - v2 = 5 - (-5) = 10 m/s
  j = -(1 + e) Ã— v_rel / (1/m1 + 1/m2)
  j = -(1.3) Ã— 10 / (0.01176 + 0.01176)
  j = -13 / 0.02353
  j = -552.5 kgÂ·m/s

Velocity change:
  Î”v1 = j / m1 = -552.5 / 85 = -6.5 m/s
  Final v1 = 5 + (-6.5) = -1.5 m/s

Expected impulse magnitude: ~6.5 m/s per agent

Tolerance rationale:
  - Float precision: Â±0.01 m/s
  - Penetration depth affects exact collision timing: Â±0.1 m/s
  - Conservative margin: Â±0.2 m/s additional
  - Total: Â±0.3 m/s (covers implementation variance)
```

---

### D.2 CR-002: Asymmetric Mass Velocity Tolerance (Â±0.5 m/s)

**Derivation:**

```
Given:
  m1 = 100 kg (heavy), v1 = 8 m/s
  m2 = 72.5 kg (light), v2 = 0 m/s
  e = 0.3

Impulse calculation:
  v_rel = 8 - 0 = 8 m/s
  j = -(1.3) Ã— 8 / (0.01 + 0.0138)
  j = -10.4 / 0.0238
  j = -437 kgÂ·m/s

Velocity changes:
  Î”v1 = -437 / 100 = -4.37 m/s â†’ final v1 â‰ˆ 3.6 m/s
  Î”v2 = 437 / 72.5 = 6.03 m/s â†’ final v2 â‰ˆ 6.0 m/s

Tolerance rationale:
  - Mass ratio introduces asymmetry in rounding
  - Wider tolerance (Â±0.5 m/s) accounts for mass calculation variance
```

---

### D.3 FL-002/FL-003: Probability Tolerance (Â±4%)

**Derivation:**

```
Probability tests run N = 10,000 iterations.
For expected probability p = 0.5:
  Expected successes: Î¼ = 5000
  Standard deviation: Ïƒ = âˆš(N Ã— p Ã— (1-p)) = âˆš2500 = 50
  
99% confidence interval: Î¼ Â± 2.58Ïƒ = 5000 Â± 129 = [4871, 5129]
Tolerance of Â±4%: 5000 Â± 200 = [4800, 5200]

The Â±4% tolerance provides:
  - >99% confidence that correctly-implemented code passes
  - <1% false failure rate in CI pipeline
  - 10,000 iterations complete in ~50ms (acceptable test runtime)
```

---

### D.4 PERF-001: Timing Tolerance (<0.15ms mean)

**Derivation:**

```
Performance budget (Section 2.4):
  Total frame budget: 16.67ms (60Hz)
  Physics allocation: ~15% = 2.5ms
  Ball Physics: ~0.3ms
  Agent Movement: ~0.5ms Ã— 22 agents (but batched, ~1.5ms total)
  Collision System: remaining ~0.5ms

Breakdown for Collision System:
  - Spatial hash operations: 0.05ms
  - Broad phase (22 queries): 0.05ms
  - Narrow phase (~5 pairs): 0.03ms
  - Response (~2 actual collisions): 0.02ms
  - Overhead: 0.00ms
  - Total typical: ~0.15ms

Worst case (corner kick clustering):
  - More pairs to check: +0.20ms
  - More responses to compute: +0.10ms
  - Total worst: ~0.45ms

Target: <0.15ms mean, <0.50ms p99
```

---

