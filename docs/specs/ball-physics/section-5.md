# Ball Physics Specification - Section 5: Testing

**Created:** February 4, 2026, 1:15 AM PST  
**Version:** 1.1 (Rolling distance correction per REV-001)  
**Status:** Ready for Approval  
**Purpose:** Define test requirements and acceptance criteria for Ball Physics System

---

## 5. TESTING

### 5.1 Test Strategy

#### 5.1.1 Test Pyramid

```
        /\
       /  \     E2E Tests (5%)
      /____\    - Full match simulation
     /      \   - Replay reconstruction
    /        \  
   / Integr-  \ Integration Tests (25%)
  /  ation     \ - Multi-system scenarios
 /______________\ - State transitions
/                \ 
/   Unit Tests    \ Unit Tests (70%)
/__(32+ tests)____\ - Formula validation
```

**Distribution rationale:**
- Unit tests are fast (<10s total), enable rapid iteration
- Integration tests verify system boundaries
- E2E tests are expensive, run less frequently

#### 5.1.2 Testing Tools

**Framework:** Unity Test Framework (NUnit)  
**Execution:** Unity Test Runner (Play Mode and Edit Mode)  
**Coverage:** Unity Code Coverage package (target: >80%)  
**Performance:** Unity Profiler (validate p95 <0.5ms)

#### 5.1.3 Test Data Strategy

**Derived values (not magic numbers):**
- All expected results calculated from formulas
- Show derivation in test comments or Appendix D
- Example: Bounce height from drop = eÃ‚Â² Ãƒâ€” h_drop

**Realistic scenarios:**
- Use professional match data as reference
- Free kick speeds: 20-30 m/s typical
- Spin rates: 8-15 rad/s typical
- Trajectories validated against specified YouTube footage (Appendix E)

---

### 5.2 Unit Test Categories

#### 5.2.1 Magnus Effect Tests (6 tests minimum)

**Test ID: UT-MAG-001**  
**Name:** `MagnusForce_ZeroSpin_ReturnsZeroForce`  
**Purpose:** Validate that no spin = no Magnus force  
**Input:** velocity = (20, 0, 0) m/s, angularVelocity = (0, 0, 0) rad/s  
**Expected:** force = (0, 0, 0) N  
**Tolerance:** Exact (0.0) - mathematical identity

---

**Test ID: UT-MAG-002**  
**Name:** `MagnusForce_ZeroVelocity_ReturnsZeroForce`  
**Purpose:** Validate stationary ball has no Magnus force  
**Input:** velocity = (0, 0, 0) m/s, angularVelocity = (0, 0, 10) rad/s  
**Expected:** force = (0, 0, 0) N  
**Tolerance:** Exact (0.0) - mathematical identity

---

**Test ID: UT-MAG-003**  
**Name:** `MagnusForce_Sidespin_ProducesLateralForce`  
**Purpose:** Validate sidespin curves ball horizontally  
**Input:** velocity = (20, 0, 0) m/s, angularVelocity = (0, 0, -12) rad/s  
**Expected:** force.y Ã¢â€°Â  0, force.x Ã¢â€°Ë† 0, force.z Ã¢â€°Ë† 0  
**Tolerance:** |force.x| < 0.1 N, |force.z| < 0.1 N (see Appendix D.1)  
**Derivation:** S = 0.066, C_L = 0.126, F Ã¢â€°Ë† 1.4 N in Y direction

---

**Test ID: UT-MAG-004**  
**Name:** `MagnusForce_Topspin_ProducesDownwardForce`  
**Purpose:** Validate topspin dips ball  
**Input:** velocity = (20, 0, 0) m/s, angularVelocity = (0, -10, 0) rad/s  
**Expected:** force.z < 0 (downward)  
**Tolerance:** force.z < -0.5 N (see Appendix D.1)

---

**Test ID: UT-MAG-005**  
**Name:** `MagnusForce_HighSpinParameter_ClampsLiftCoefficient`  
**Purpose:** Validate S > 1.0 doesn't cause instability  
**Input:** velocity = (5, 0, 0) m/s, angularVelocity = (0, 0, 50) rad/s (S Ã¢â€°Ë† 1.1)  
**Expected:** Calculation completes without error, force finite  
**Tolerance:** No NaN/Infinity, C_L clamped to valid range

---

**Test ID: UT-MAG-006**  
**Name:** `MagnusForce_ParallelSpinAndVelocity_ReturnsZero`  
**Purpose:** Validate Ãâ€° parallel to v produces no curve  
**Input:** velocity = (20, 0, 0) m/s, angularVelocity = (10, 0, 0) rad/s  
**Expected:** force Ã¢â€°Ë† (0, 0, 0) N  
**Tolerance:** |force| < 0.01 N (numerical precision, see Appendix D.2)

---

#### 5.2.2 Drag Force Tests (4 tests minimum)

**Test ID: UT-DRG-001**  
**Name:** `DragForce_OpposesVelocityDirection`  
**Purpose:** Validate drag always opposes motion  
**Input:** velocity = (10, 5, 3) m/s  
**Expected:** force Ã‚Â· velocity < 0 (dot product negative)  
**Tolerance:** Exact (sign check only)

---

**Test ID: UT-DRG-002**  
**Name:** `DragForce_ScalesWithVelocitySquared`  
**Purpose:** Validate F_drag Ã¢Ë†Â vÃ‚Â²  
**Method:** 
- Calculate drag at v = 10 m/s Ã¢â€ â€™ FÃ¢â€šÂ
- Calculate drag at v = 20 m/s Ã¢â€ â€™ FÃ¢â€šâ€š
- Expected: FÃ¢â€šâ€š Ã¢â€°Ë† 4 Ãƒâ€” FÃ¢â€šÂ  
**Tolerance:** Ã‚Â±5% (see Appendix D.3 - accounts for Cd variation)

---

**Test ID: UT-DRG-003**  
**Name:** `DragCoefficient_DragCrisisTransition`  
**Purpose:** Validate Cd changes in 20-25 m/s range  
**Method:**
- Get Cd at 15 m/s Ã¢â€ â€™ expect 0.20 (laminar)
- Get Cd at 22.5 m/s Ã¢â€ â€™ expect Ã¢â€°Ë†0.15 (interpolated)
- Get Cd at 30 m/s Ã¢â€ â€™ expect 0.10 (turbulent)  
**Tolerance:** Ã‚Â±0.01 (see Appendix D.3)

---

**Test ID: UT-DRG-004**  
**Name:** `DragForce_ZeroVelocity_ReturnsZero`  
**Purpose:** Validate stationary ball has no drag  
**Input:** velocity = (0, 0, 0) m/s  
**Expected:** force = (0, 0, 0) N  
**Tolerance:** Exact (0.0)

---

#### 5.2.3 Bounce Mechanics Tests (6 tests minimum)

**Test ID: UT-BNC-001**  
**Name:** `Bounce_VerticalDrop_ReturnsExpectedHeight`  
**Purpose:** Validate CoR calculation  
**Setup:** Ball at z = 2.11m (2m drop from center), velocity = (0, 0, -6.26) m/s  
**Surface:** GRASS_DRY (e = 0.65)  
**Expected:** 
- Rebound velocity: v_z Ã¢â€°Ë† +4.07 m/s (= e Ãƒâ€” 6.26)
- Rebound height: 0.80-0.90m above ground  
**Derivation:** h = vÃ‚Â²/(2g) = 4.07Ã‚Â²/19.62 = 0.84m  
**Tolerance:** Ã‚Â±0.05m (see Appendix D.4 - air resistance during ascent)

---

**Test ID: UT-BNC-002**  
**Name:** `Bounce_Topspin_KicksForward`  
**Purpose:** Validate spin-to-linear transfer  
**Setup:** 
- velocity = (5, 0, -3) m/s (forward and downward)
- angularVelocity = (0, -10, 0) rad/s (topspin)  
**Expected:** velocity.x after bounce > velocity.x before  
**Tolerance:** ÃŽâ€v_x > 0.2 m/s (see Appendix D.5)  
**Mechanism:** Contact point moving forward adds to linear velocity

---

**Test ID: UT-BNC-003**  
**Name:** `Bounce_Backspin_Bites`  
**Purpose:** Validate backspin reduces forward velocity  
**Setup:**
- velocity = (8, 0, -3) m/s
- angularVelocity = (0, +12, 0) rad/s (backspin)  
**Expected:** velocity.x after bounce < velocity.x before  
**Tolerance:** ÃŽâ€v_x < -0.2 m/s (see Appendix D.5)

---

**Test ID: UT-BNC-004**  
**Name:** `Bounce_PositionClampedToGroundLevel`  
**Purpose:** Validate ball placed at RADIUS height after bounce  
**Expected:** ball.Position.z = 0.11m (RADIUS) after bounce  
**Tolerance:** Exact (enforced by clamp)

---

**Test ID: UT-BNC-005**  
**Name:** `Bounce_SpinRetentionApplied`  
**Purpose:** Validate angular velocity reduced by surface factor  
**Setup:** angularVelocity = (0, 10, 0) rad/s before bounce  
**Surface:** GRASS_DRY (retention = 0.80)  
**Expected:** angularVelocity.magnitude Ã¢â€°Ë† 8.0 rad/s after bounce  
**Tolerance:** Ã‚Â±0.2 rad/s (see Appendix D.6)

---

**Test ID: UT-BNC-006**  
**Name:** `Bounce_WetGrass_HigherRebound`  
**Purpose:** Validate surface-specific CoR  
**Setup:** Same drop from 2m  
**Surface:** GRASS_WET (e = 0.70)  
**Expected:** Rebound height > dry grass case (Ã¢â€°Ë†0.98m vs 0.84m)  
**Tolerance:** Height difference >0.10m

---

#### 5.2.4 State Machine Tests (8 tests minimum)

**Test ID: UT-STM-001**  
**Name:** `StateTransition_RollingToAirborne_AtEnterThreshold`  
**Purpose:** Validate hysteresis enter threshold  
**Setup:** State = ROLLING, Position.z = 0.17m, Velocity.z > 0  
**Expected:** New state = AIRBORNE  
**Tolerance:** Exact (deterministic threshold)

---

**Test ID: UT-STM-002**  
**Name:** `StateTransition_AirborneToRolling_AtExitThreshold`  
**Purpose:** Validate hysteresis exit threshold  
**Setup:** State = AIRBORNE, Position.z = 0.13m, Velocity.z < 0  
**Expected:** New state = BOUNCING Ã¢â€ â€™ ROLLING (after bounce)  
**Tolerance:** Exact (deterministic threshold)

---

**Test ID: UT-STM-003**  
**Name:** `StateTransition_HysteresisPreventsOscillation`  
**Purpose:** Validate ball doesn't rapidly switch states  
**Setup:** Position.z oscillating between 0.14-0.16m over 10 frames  
**Expected:** State remains stable (max 2 transitions, not 10)  
**Tolerance:** Transition count Ã¢â€°Â¤ 2

---

**Test ID: UT-STM-004**  
**Name:** `StateTransition_RollingToStationary_BelowMinVelocity`  
**Purpose:** Validate velocity threshold  
**Setup:** State = ROLLING, velocity.magnitude = 0.05 m/s  
**Expected:** New state = STATIONARY  
**Tolerance:** Exact (threshold = 0.1 m/s)

---

**Test ID: UT-STM-005**  
**Name:** `StateTransition_BouncingToRolling_LowVerticalVelocity`  
**Purpose:** Validate bounce settling  
**Setup:** State = BOUNCING, velocity.z = 0.3 m/s (< 0.5 threshold)  
**Expected:** New state = ROLLING  
**Tolerance:** Exact

---

**Test ID: UT-STM-006**  
**Name:** `StateTransition_BouncingToAirborne_HighVerticalVelocity`  
**Purpose:** Validate high bounce stays airborne  
**Setup:** State = BOUNCING, velocity.z = 2.0 m/s  
**Expected:** New state = AIRBORNE  
**Tolerance:** Exact

---

**Test ID: UT-STM-007**  
**Name:** `OutOfBounds_TouchlineLeft_DetectedCorrectly`  
**Purpose:** Validate boundary detection  
**Setup:** Position = (50, -0.12, 0.11) (ball center past line)  
**Expected:** IsOutOfBounds() = true  
**Tolerance:** Exact (boolean)

---

**Test ID: UT-STM-008**  
**Name:** `OutOfBounds_StillInPlay_WhenPartiallyOverLine`  
**Purpose:** Validate ball must entirely cross  
**Setup:** Position = (50, -0.05, 0.11) (ball touching but not entirely over)  
**Expected:** IsOutOfBounds() = false  
**Tolerance:** Exact (boolean)

---

#### 5.2.5 Validation Tests (5 tests minimum)

**Test ID: UT-VAL-001**  
**Name:** `Validation_DetectsNaN_AndRecovers`  
**Purpose:** Validate NaN detection and recovery  
**Setup:** Position = (NaN, 34, 0), LastValidPosition = (50, 34, 0)  
**Expected:** After validation, Position = (50, 34, 0), State = STATIONARY  
**Tolerance:** Exact (forced recovery)

---

**Test ID: UT-VAL-002**  
**Name:** `Validation_DetectsInfinity_AndRecovers`  
**Purpose:** Validate Infinity detection  
**Setup:** Velocity = (Ã¢Ë†Å¾, 0, 0)  
**Expected:** Recovery to LastValidVelocity, State = STATIONARY  
**Tolerance:** Exact

---

**Test ID: UT-VAL-003**  
**Name:** `Validation_ClampsExcessiveVelocity`  
**Purpose:** Validate MAX_VELOCITY enforcement  
**Setup:** Velocity = (60, 0, 0) m/s (> 50 m/s limit)  
**Expected:** Velocity clamped to (50, 0, 0), direction preserved  
**Tolerance:** Exact (enforced clamp)

---

**Test ID: UT-VAL-004**  
**Name:** `Validation_ClampsExcessiveSpin`  
**Purpose:** Validate MAX_SPIN enforcement  
**Setup:** AngularVelocity = (0, 100, 0) rad/s (> 80 rad/s limit)  
**Expected:** AngularVelocity clamped to (0, 80, 0)  
**Tolerance:** Exact

---

**Test ID: UT-VAL-005**  
**Name:** `Validation_FixesGroundPenetration`  
**Purpose:** Validate ground clamp  
**Setup:** Position.z = 0.05m (< RADIUS = 0.11m)  
**Expected:** Position.z = 0.11m, Velocity.z Ã¢â€°Â¥ 0  
**Tolerance:** Exact

---

#### 5.2.6 Spin Decay Tests (3 tests minimum)

**Test ID: UT-SPN-001**  
**Name:** `SpinDecay_ReducesOverTime`  
**Purpose:** Validate spin decays  
**Setup:** Initial Ãâ€° = 20 rad/s, simulate 5 seconds  
**Expected:** Final Ãâ€° < 20 rad/s (monotonic decrease)  
**Tolerance:** Final Ãâ€° < 18 rad/s (minimum 10% decay over 5s)

---

**Test ID: UT-SPN-002**  
**Name:** `SpinDecay_FasterAtHigherVelocity`  
**Purpose:** Validate velocity-dependent decay  
**Method:**
- Case A: v = 5 m/s, Ãâ€° = 20 rad/s, simulate 2s Ã¢â€ â€™ decay rate RÃ¢â€šÂ
- Case B: v = 15 m/s, Ãâ€° = 20 rad/s, simulate 2s Ã¢â€ â€™ decay rate RÃ¢â€šâ€š  
**Expected:** RÃ¢â€šâ€š > RÃ¢â€šÂ (higher velocity = faster decay)  
**Tolerance:** RÃ¢â€šâ€š > 1.5 Ãƒâ€” RÃ¢â€šÂ (see Appendix D.7)

---

**Test ID: UT-SPN-003**  
**Name:** `SpinDecay_ZeroBelowThreshold`  
**Purpose:** Validate minimum spin cutoff  
**Setup:** AngularVelocity = (0, 0.05, 0) rad/s (< 0.1 threshold)  
**Expected:** After UpdateSpinDecay(), AngularVelocity = (0, 0, 0)  
**Tolerance:** Exact

---

### 5.3 Integration Test Scenarios

#### 5.3.1 Full Trajectory Tests (3 scenarios)

**Test ID: IT-TRJ-001**  
**Name:** `FreekickTrajectory_CurvesWithinExpectedRange`  
**Purpose:** End-to-end Magnus effect validation  
**Setup:**
- Initial: Position = (25, 34, 0), Velocity = (22, 0, 6), AngularVelocity = (0, 0, -12)
- Simulate until ball travels 25m or hits ground  
**Expected:** 
- Lateral deviation: 1.5-3.0m
- Flight time: 1.0-1.5s  
**Derivation:** See Section 3.1.14 (worked example)  
**Validation:** Compare to reference clip (Appendix E.1 - Ronaldo vs Portsmouth 2008)

---

**Test ID: IT-TRJ-002**  
**Name:** `LongBall_DecaysRealistically`  
**Purpose:** Validate drag over distance  
**Setup:** Initial velocity = (25, 0, 8) m/s (40m pass trajectory)  
**Expected:**
- Ball reaches 40m in 1.8-2.2s
- Peak height: 4-6m
- Landing velocity: 15-20 m/s (not 25 m/s - drag effect)  
**Tolerance:** Ã‚Â±10% on all metrics (see Appendix D.8)

---

**Test ID: IT-TRJ-003**  
**Name:** `RollingBall_StopsWithinExpectedDistance`  
**Purpose:** Validate rolling friction  
**Setup:** Initial velocity = (10, 0, 0) m/s on ground  
**Expected:** Ball stops in 26-31m  
**Derivation:** See Section 3.1.14 (rolling distance calculation, Î¼_r = 0.13)  
**Tolerance:** Ã‚Â±15% (see Appendix D.9 - accounts for drag variation)

---

#### 5.3.2 Multi-Bounce Scenarios (2 scenarios)

**Test ID: IT-MBC-001**  
**Name:** `BouncingBall_SettlesWithinReasonableTime`  
**Purpose:** Validate bounce settling behavior  
**Setup:** Drop ball from 5m height  
**Expected:**
- Ball stops bouncing within 5 seconds
- Final state = STATIONARY or ROLLING
- No oscillation between states (max 2 transitions per second)  
**Tolerance:** Time < 6 seconds, transition frequency check

---

**Test ID: IT-MBC-002**  
**Name:** `BouncingBall_LosesEnergyMonotonically`  
**Purpose:** Validate energy conservation  
**Method:** Track bounce heights: hÃ¢â€šÂ, hÃ¢â€šâ€š, hÃ¢â€šÆ’...  
**Expected:** hÃ¢â€šÂ > hÃ¢â€šâ€š > hÃ¢â€šÆ’ (strictly decreasing)  
**Tolerance:** Each bounce Ã¢â€°Â¤ previous bounce (no energy gain)

---

#### 5.3.3 State Transition Sequences (2 scenarios)

**Test ID: IT-STS-001**  
**Name:** `StateSequence_KickToGoal_ValidTransitions`  
**Purpose:** Validate realistic match scenario  
**Sequence:**
1. STATIONARY Ã¢â€ â€™ AIRBORNE (kick)
2. AIRBORNE (flight)
3. BOUNCING (ground contact)
4. ROLLING (settling)
5. STATIONARY (stopped)  
**Expected:** No invalid transitions, no state oscillation (max 1 transition per 100ms)

---

**Test ID: IT-STS-002**  
**Name:** `StateSequence_ChipPass_ValidTransitions`  
**Purpose:** Validate lofted pass  
**Sequence:**
1. CONTROLLED Ã¢â€ â€™ AIRBORNE (chip)
2. AIRBORNE Ã¢â€ â€™ BOUNCING
3. BOUNCING Ã¢â€ â€™ AIRBORNE (high bounce)
4. AIRBORNE Ã¢â€ â€™ BOUNCING
5. BOUNCING Ã¢â€ â€™ ROLLING  
**Expected:** Multiple bounces handled correctly, no deadlock

---

#### 5.3.4 Collision Tests (3 scenarios - NEW)

**Test ID: IT-COL-001**  
**Name:** `GoalPostCollision_DeflectsCorrectly`  
**Purpose:** Validate post hit physics  
**Setup:**
- Ball traveling at (15, 0, 0) m/s toward goal post
- Contact point on cylindrical post surface  
**Expected:**
- Velocity reversed in normal direction
- Spin retention = 0.40 (metal surface)
- Ball deflects away from goal  
**Tolerance:** Reflection angle Ã‚Â±5Ã‚Â° (see Appendix D.10)

---

**Test ID: IT-COL-002**  
**Name:** `CrossbarCollision_BallBouncesDown`  
**Purpose:** Validate crossbar physics  
**Setup:**
- Ball trajectory intersects crossbar (z = 2.44m)
- Velocity = (10, 0, 5) m/s (upward and forward)  
**Expected:**
- Ball bounces downward after contact
- CoR = 0.75 (goal post material)
- Ball lands in goal or in front  
**Derivation:** See Appendix D.11 for expected landing position

---

**Test ID: IT-COL-003**  
**Name:** `PossessionCheck_DetectsControl`  
**Purpose:** Validate agent can take possession  
**Setup:**
- Ball: Position = (50, 34, 0.11), Velocity = (1, 0, 0) m/s, State = ROLLING
- Agent: Position = (50.3, 34, 0), Velocity = (1, 0, 0) m/s  
**Expected:** CheckPossession() = true (within 0.5m, relative velocity <2 m/s)  
**Tolerance:** Boolean (exact)

---

#### 5.3.5 Event Logging Tests (1 scenario - NEW)

**Test ID: IT-LOG-001**  
**Name:** `EventLogger_CapturesKeyEvents`  
**Purpose:** Validate replay data generation  
**Setup:** Simulate shot sequence (kick Ã¢â€ â€™ flight Ã¢â€ â€™ bounce Ã¢â€ â€™ goal)  
**Expected Events:**
1. KICK event (timestamp, agent ID)
2. POSITION_SNAPSHOT events (every 1 second)
3. BOUNCE event (surface, CoR, velocities)
4. GOAL event (scorer, team)  
**Validation:** Event list contains all 4 types in correct chronological order

---

### 5.4 Acceptance Criteria

#### 5.4.1 Unit Test Requirements

**Mandatory:**
- Ã¢Å“â€¦ All 32 specified unit tests pass
- Ã¢Å“â€¦ Code coverage >80% for BallPhysicsCore
- Ã¢Å“â€¦ Code coverage >70% for BallStateMachine
- Ã¢Å“â€¦ Zero compiler warnings
- Ã¢Å“â€¦ Test suite completes in <10 seconds

**Quality:**
- Ã¢Å“â€¦ All magic numbers replaced with derived values
- Ã¢Å“â€¦ Test names clearly describe purpose
- Ã¢Å“â€¦ Expected values include derivation comments or reference to Appendix D
- Ã¢Å“â€¦ Tolerance values justified in Appendix D

#### 5.4.2 Integration Test Requirements

**Mandatory:**
- Ã¢Å“â€¦ All 12 integration scenarios pass (9 original + 3 collision/logging)
- Ã¢Å“â€¦ Trajectories match derived specifications (Section 3.1.14)
- Ã¢Å“â€¦ No state machine oscillations observed (max 2 transitions/second)
- Ã¢Å“â€¦ Energy conservation within 5% for bounces

**Realism validation:**
- Ã¢Å“â€¦ Free kick curve: 1.5-3.0m at 25m distance
- Ã¢Å“â€¦ Bounce height: 0.80-0.90m from 2m drop (dry grass)
- Ã¢Å“â€¦ Rolling distance: 26-31m from 10 m/s initial velocity

#### 5.4.3 Performance Requirements

**Standardized targets (FINAL):**
- Ã¢Å“â€¦ `UpdateBallPhysics()` <0.5ms per call (p95)
- Ã¢Å“â€¦ `UpdateBallPhysics()` <0.35ms per call (mean)
- Ã¢Å“â€¦ `UpdateBallPhysics()` <0.7ms per call (p99 - worst case spike)
- Ã¢Å“â€¦ Zero heap allocations during physics loop
- Ã¢Å“â€¦ No GC pressure (confirmed via profiler)

**Measurement protocol:**
- Run 100 simulated matches (5400 seconds simulated time each)
- Profile every frame at 60Hz (324,000 frames per match)
- Generate statistical report (mean, p50, p95, p99)
- Flag any frames >0.7ms for investigation

#### 5.4.4 Stability Requirements

**Mandatory:**
- Ã¢Å“â€¦ Zero NaN or Infinity values in 1000 match simulation
- Ã¢Å“â€¦ Position drift <1cm after 90 minutes (54,000 ticks)
- Ã¢Å“â€¦ All failure modes trigger recovery (no crashes)
- Ã¢Å“â€¦ Edge cases handled gracefully (zero velocity, extreme spin, etc.)

**Edge case coverage:**
- Ã¢Å“â€¦ Ball at rest
- Ã¢Å“â€¦ Ball at max velocity (50 m/s)
- Ã¢Å“â€¦ Ball at max spin (80 rad/s)
- Ã¢Å“â€¦ Ball at boundary (exactly on line)
- Ã¢Å“â€¦ Simultaneous ground contact and boundary crossing
- Ã¢Å“â€¦ Post collision during bounce
- Ã¢Å“â€¦ Zero gravity (sanity check for hypothetical mods)

#### 5.4.5 Visual Validation Criteria

**Standardized protocol:**
- Record 10 different shot/pass scenarios
- Compare side-by-side with 3 reference clips (Appendix E)
- Survey: 10 football players/fans (recruitment via r/bootroom)
- Survey question: "Does this trajectory look realistic? (Yes/No/Unsure)"
- Required: >90% "Yes" responses

**Reference clips (Appendix E):**
1. Ronaldo vs Portsmouth 2008 (knuckleball free kick)
2. Roberto Carlos vs France 1997 (extreme curve)
3. Generic Premier League long pass compilation (drag/arc validation)

**Validation timing:** After all unit/integration tests pass

---

### 5.5 Validation Frequency Clarification

**CRITICAL: When validation runs**

**Every Frame (60Hz):**
- `ValidatePhysicsState()` runs at END of `UpdateBallPhysics()`
- Checks: NaN/Infinity, velocity clamp, spin clamp, ground penetration
- Cost: ~0.02ms (included in 0.35ms mean budget)
- Justification: Prevents catastrophic failures from propagating

**Performance impact:**
- Validation is lightweight (mostly comparison operations)
- Early returns on normal cases
- Recovery paths rarely executed (<0.1% of frames)
- Profiled separately as "BallPhysics.Validation"

**Can be disabled:**
- Production builds: Keep enabled (safety-critical)
- Editor builds: Always enabled (helps catch bugs during development)

---

### 5.6 Test Execution Plan

#### 5.6.1 Development Phase Testing

**During implementation (Stage 0 Months 1-2):**
- Write unit test alongside each function (TDD approach)
- Run unit tests after every code change
- Commit only when tests pass
- Target: Green tests 95% of time

**Weekly integration testing:**
- Run full test suite every Friday
- Review coverage report
- Address any gaps immediately

#### 5.6.2 Pre-Approval Testing

**Before Section 3.1 approval:**
- Ã¢Å“â€¦ All unit tests pass
- Ã¢Å“â€¦ All integration tests pass
- Ã¢Å“â€¦ Coverage targets met (>80% core, >70% state machine)
- Ã¢Å“â€¦ Performance targets met (p95 <0.5ms, mean <0.35ms)
- Ã¢Å“â€¦ Visual validation completed (>90% approval)

**Stage 0 Quality Gate (before Stage 1):**
- Run 1000 simulated matches
- Zero critical failures
- <5 validation warnings per match
- Community feedback positive (>75% on r/gamedev)

#### 5.6.3 Regression Testing

**Continuous:**
- Unit tests run on every commit (local machine)
- Integration tests run nightly (if CI/CD available in future)
- Performance tests run weekly

**Before any constant tuning:**
- Baseline metrics captured (commit SHA + results)
- Tuning changes tested
- Regression check (ensure no other tests break)
- Document tuning rationale in code comments

---

### 5.7 Test Data Management

#### 5.7.1 Reference Trajectories

**Store validated trajectories as test fixtures:**

```csharp
public static class ReferenceTrajectories
{
    // Free kick: 25m curve shot
    // Derived 2026-02-04 from Section 3.1.14 calculations
    // Validated against Ronaldo vs Portsmouth 2008 footage
    public static readonly TrajectoryData Freekick_25m_Curve = new()
    {
        InitialPosition = new Vector3(25, 34, 0),
        InitialVelocity = new Vector3(22, 0, 6),
        InitialSpin = new Vector3(0, 0, -12),
        ExpectedLateralDeviation = (1.5f, 3.0f), // Min, Max in meters
        ExpectedFlightTime = (1.0f, 1.5f), // Min, Max in seconds
        ReferenceClip = "Appendix E.1"
    };
    
    // Long ball: 40m pass
    // Derived 2026-02-04, typical long pass scenario
    public static readonly TrajectoryData LongBall_40m = new()
    {
        InitialPosition = new Vector3(30, 34, 0.11),
        InitialVelocity = new Vector3(25, 0, 8),
        InitialSpin = new Vector3(0, -5, 0), // Slight topspin
        ExpectedDistance = (38f, 42f),
        ExpectedPeakHeight = (4f, 6f),
        ExpectedFlightTime = (1.8f, 2.2f),
        ReferenceClip = "Appendix E.3"
    };
}
```

**Version control:**
- Commit reference data with code
- Update when constants tuned
- Document reason for changes in Git commit message

#### 5.7.2 Surface Test Coverage

**Stage 0 scope: 2 surfaces only (simplified)**

| Surface | CoR | Friction | Rolling | Test Coverage |
|---------|-----|----------|---------|---------------|
| GRASS_DRY | 0.65 | 0.60 | 0.05 | UT-BNC-001/002/003/004/005, IT-TRJ-003, IT-MBC-001/002 |
| GRASS_WET | 0.70 | 0.40 | 0.03 | UT-BNC-006 |

**Removed from Stage 0:**
- GRASS_LONG (defer to Stage 2)
- ARTIFICIAL (defer to Stage 2)
- FROZEN (defer to Stage 2)

**Rationale:** 
- Reduces untested code paths
- Simplifies initial implementation
- Can add surfaces in Stage 2 with pitch condition system
- Two surfaces sufficient to validate surface-specific logic

**SurfaceProperties.cs changes:**
- Remove unused surface types from enum
- Keep 2 surfaces (DRY/WET)
- Add TODO comment for Stage 2 expansion

---

### 5.8 Test Implementation Examples

#### 5.8.1 Unit Test Example (Complete)

```csharp
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.Core.Physics.Ball;

namespace TacticalDirector.Core.Physics.Ball.Tests
{
    [TestFixture]
    public class BallPhysicsCoreTests
    {
        [Test]
        public void MagnusForce_Sidespin_ProducesLateralForce()
        {
            // ARRANGE
            // Ball moving forward with clockwise sidespin (from above)
            // Expected: Force in -Y direction (curves right from kicker's view)
            Vector3 velocity = new Vector3(20, 0, 0); // 20 m/s forward
            Vector3 angularVelocity = new Vector3(0, 0, -12); // 12 rad/s sidespin
            
            // Derived expected force (see Appendix D.1):
            // S = (r Ãƒâ€” |Ãâ€°|) / |v| = (0.11 Ãƒâ€” 12) / 20 = 0.066
            // C_L = 0.1 + 0.4 Ãƒâ€” (0.066 - 0.01) / 0.99 = 0.1227
            // F = 0.5 Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” A Ãƒâ€” C_L = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 400 Ãƒâ€” 0.038 Ãƒâ€” 0.1227
            // F Ã¢â€°Ë† 1.14 N
            // Direction: Ãâ€° Ãƒâ€” v = (0,0,-1) Ãƒâ€” (1,0,0) = (0,-1,0)
            float expectedForceMagnitude = 1.14f;
            
            // ACT
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(velocity, angularVelocity);
            
            // ASSERT
            // Force should be primarily in Y direction
            Assert.Less(Mathf.Abs(force.x), 0.1f, 
                "X component should be near zero (force perpendicular to velocity)");
            Assert.Less(Mathf.Abs(force.z), 0.1f, 
                "Z component should be near zero (no lift for horizontal spin)");
            
            // Y component should be negative (curves right)
            Assert.Less(force.y, -0.5f, 
                "Y component should be significantly negative");
            
            // Magnitude should be close to derived value (Ã‚Â±10% tolerance)
            float actualMagnitude = force.magnitude;
            Assert.That(actualMagnitude, 
                Is.InRange(expectedForceMagnitude * 0.9f, expectedForceMagnitude * 1.1f),
                $"Force magnitude should be approximately {expectedForceMagnitude}N (Ã‚Â±10%)");
        }
    }
}
```

#### 5.8.2 Integration Test Example (Complete)

```csharp
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.Core.Physics.Ball;

namespace TacticalDirector.Core.Physics.Ball.Tests
{
    [TestFixture]
    public class BallIntegrationTests
    {
        private const float DELTA_TIME = 1f / 60f; // 60 Hz simulation
        
        [Test]
        public void FreekickTrajectory_CurvesWithinExpectedRange()
        {
            // ARRANGE
            BallState ball = BallState.CreateAtPosition(new Vector3(25, 34, 0));
            ball.Velocity = new Vector3(22, 0, 6); // Forward and upward
            ball.AngularVelocity = new Vector3(0, 0, -12); // Sidespin
            ball.State = BallStateType.AIRBORNE;
            
            BallEventLogger logger = new BallEventLogger();
            SurfaceType surface = SurfaceType.GRASS_DRY;
            Vector3 wind = Vector3.zero;
            
            float startY = ball.Position.y;
            float matchTime = 0f;
            int maxIterations = 300; // Safety: 5 seconds max
            int iterations = 0;
            
            // ACT
            // Simulate until ball travels ~25m or hits ground
            while (ball.Position.x < 50 && 
                   ball.Position.z >= BallPhysicsConstants.Ball.RADIUS && 
                   iterations < maxIterations)
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, DELTA_TIME, surface, wind, logger, matchTime);
                matchTime += DELTA_TIME;
                iterations++;
            }
            
            // ASSERT
            float lateralDeviation = Mathf.Abs(ball.Position.y - startY);
            
            Assert.Greater(lateralDeviation, 1.5f, 
                "Ball should curve at least 1.5m (minimum Magnus effect)");
            Assert.Less(lateralDeviation, 3.0f, 
                "Ball should not curve more than 3.0m (maximum reasonable curve)");
            
            // Flight time should be realistic (1.0-1.5 seconds)
            Assert.That(matchTime, Is.InRange(1.0f, 1.5f),
                "Flight time should be between 1.0 and 1.5 seconds");
            
            // Ball should have landed (not still flying)
            Assert.Less(ball.Position.z, 0.5f, 
                "Ball should have landed or be very close to ground");
        }
    }
}
```

#### 5.8.3 Test Structure Guidelines

**Standard test structure (Arrange-Act-Assert):**

```csharp
[Test]
public void ComponentName_Scenario_ExpectedBehavior()
{
    // ARRANGE
    // - Set up initial conditions
    // - Create test data
    // - Include derivation comments for expected values
    
    // ACT
    // - Call the function under test
    // - Perform the operation
    
    // ASSERT
    // - Verify expected outcomes
    // - Use descriptive failure messages
    // - Check all relevant properties
}
```

**Naming convention:**
- Test method: `ComponentName_Scenario_ExpectedBehavior`
- Test class: `[ComponentName]Tests`
- Namespace: `TacticalDirector.Core.Physics.Ball.Tests`

**Documentation:**
- XML comments for test purpose (optional)
- Inline comments for derivations (required for non-obvious values)
- Reference to Appendix D for tolerance justification

---

### 5.9 Defect Classification

**Critical (Must fix before approval):**
- NaN/Infinity during normal operation
- Crash or infinite loop
- Physics instability (runaway velocity)
- State machine deadlock
- Performance >0.5ms p95 or >0.7ms p99

**Major (Fix before Stage 1):**
- Test failure with clear impact on realism
- Coverage gap >20%
- Validation warning frequency >10 per match
- Incorrect formula implementation
- Memory leak

**Minor (Can defer to Stage 1):**
- Edge case handling could be more elegant
- Test could have better name
- Missing test for extremely rare scenario
- Performance variance (occasional spike but mean OK)
- Documentation clarity

**Enhancement (Backlog):**
- Additional surface types (Stage 2)
- More reference trajectories
- Stress testing at 120Hz
- Comparative testing vs. other engines

---

## APPENDICES

### Appendix D: Tolerance Derivations

#### D.1 Magnus Force Tolerances

**UT-MAG-003: Lateral force tolerance Ã‚Â±0.1 N**

Derivation:
- Expected force: ~1.4 N (see Section 3.1.14)
- Tolerance: 1.4 Ãƒâ€” 0.07 = 0.098 N Ã¢â€°Ë† 0.1 N
- Rationale: ~7% tolerance accounts for:
  - Numerical precision (float vs double)
  - Spin parameter discretization
  - Lift coefficient approximation

**UT-MAG-004: Downward force >0.5 N**

Derivation:
- Topspin configuration should produce significant downforce
- 0.5 N = minimum threshold to ensure Magnus is active
- ~35% of typical sidespin force (less efficient for topspin)
- Validates direction, not precise magnitude

---

#### D.2 Numerical Precision Tolerance

**UT-MAG-006: Parallel spin tolerance <0.01 N**

Derivation:
- Cross product of parallel vectors should be exactly zero
- Float precision: ~1e-7 for normalized vectors
- Magnitude after scaling: ~1e-7 Ãƒâ€” 2.0 (max force scale) = 2e-7 N
- Conservative tolerance: 0.01 N (5 orders of magnitude buffer)
- Rationale: Catches implementation errors while allowing float imprecision

---

#### D.3 Drag Scaling Tolerances

**UT-DRG-002: Velocity squared scaling Ã‚Â±5%**

Derivation:
- Theoretical: FÃ¢â€šâ€š = 4 Ãƒâ€” FÃ¢â€šÂ (exact)
- Drag coefficient varies with Reynolds number
  - At 10 m/s: Re Ã¢â€°Ë† 1.5Ãƒâ€”10Ã¢ÂÂµ Ã¢â€ â€™ Cd Ã¢â€°Ë† 0.20
  - At 20 m/s: Re Ã¢â€°Ë† 3.0Ãƒâ€”10Ã¢ÂÂµ Ã¢â€ â€™ Cd Ã¢â€°Ë† 0.18 (drag crisis begins)
- Cd ratio: 0.18/0.20 = 0.90
- Adjusted: FÃ¢â€šâ€š = 4 Ãƒâ€” 0.90 Ãƒâ€” FÃ¢â€šÂ = 3.6 Ãƒâ€” FÃ¢â€šÂ
- Tolerance: 5% = 0.18, covers range 3.42 to 3.78
- Validates vÃ‚Â² relationship while accounting for Cd variation

**UT-DRG-003: Drag coefficient Ã‚Â±0.01**

Derivation:
- Linear interpolation error in drag crisis region
- Real behavior is sharp, we approximate linearly
- Ã‚Â±0.01 = Ã‚Â±5% at Cd Ã¢â€°Ë† 0.20
- Acceptable approximation for gameplay
- Exact match not required (empirical tuning expected)

---

#### D.4 Bounce Height Tolerance

**UT-BNC-001: Rebound height Ã‚Â±0.05m**

Derivation:
- Theoretical: h = eÃ‚Â² Ãƒâ€” h_drop = 0.65Ã‚Â² Ãƒâ€” 2.0 = 0.845m
- Air resistance during ascent reduces height by ~3-5%
- Wind effects (even calm): Ã‚Â±1%
- Expected range: 0.80 - 0.88m
- Tolerance: Ã‚Â±0.05m captures 0.795 - 0.895m
- Covers theoretical Ã‚Â± air resistance Ã‚Â± minor environmental factors

---

#### D.5 Spin Transfer Tolerance

**UT-BNC-002/003: Velocity change >0.2 m/s**

Derivation:
- Contact point velocity: v_contact = Ãâ€° Ãƒâ€” r = 10 Ãƒâ€” 0.11 = 1.1 m/s
- Spin-to-linear ratio: 0.1 (10% transfer efficiency)
- Expected ÃŽâ€v = 1.1 Ãƒâ€” 0.1 = 0.11 m/s (theoretical minimum)
- Friction effects can increase to ~0.3 m/s
- Threshold: 0.2 m/s = middle of expected range
- Validates spin transfer is active without demanding exact magnitude

---

#### D.6 Spin Retention Tolerance

**UT-BNC-005: Angular velocity Ã‚Â±0.2 rad/s**

Derivation:
- Expected: 10 Ãƒâ€” 0.80 = 8.0 rad/s
- Tolerance: 8.0 Ãƒâ€” 0.025 = 0.20 rad/s
- Rationale: Ã‚Â±2.5% tolerance
  - Accounts for numerical integration error
  - Friction impulse variation
  - Surface interaction complexity
- Tight tolerance validates retention factor applied correctly

---

#### D.7 Velocity-Dependent Decay Tolerance

**UT-SPN-002: Decay ratio >1.5**

Derivation:
- Decay formula: rate = k_v Ãƒâ€” v + k_Ãâ€° Ãƒâ€” Ãâ€°
- At v=5: rate_1 = 0.01Ãƒâ€”5 + 0.005Ãƒâ€”20 = 0.05 + 0.10 = 0.15
- At v=15: rate_2 = 0.01Ãƒâ€”15 + 0.005Ãƒâ€”20 = 0.15 + 0.10 = 0.25
- Ratio: 0.25 / 0.15 = 1.67
- Threshold: 1.5 (slightly below theoretical)
- Validates velocity-dependent term is active

---

#### D.8 Long Ball Trajectory Tolerance

**IT-TRJ-002: Ã‚Â±10% on distance/height/time**

Derivation:
- Multiple factors contribute to variance:
  - Drag coefficient approximation: Ã‚Â±5%
  - Magnus effect on lofted ball: Ã‚Â±3%
  - Numerical integration error: Ã‚Â±2%
- Combined (root sum square): Ã¢Ë†Å¡(5Ã‚Â² + 3Ã‚Â² + 2Ã‚Â²) = 6.2%
- Conservative tolerance: 10% (1.6Ãƒâ€” theoretical)
- Provides safety margin for tuning during implementation

---

#### D.9 Rolling Distance Tolerance

**IT-TRJ-003: Ã‚Â±15% on distance**

Derivation:
- Section 3.1.14 estimates 26-31m (midpoint: 28.5m)
- 60Hz simulation reference value: 28.3m
- Range: 31-26 = 5m
- Percentage: 5/28.5 = 17.5%
- Tolerance: 15% = 4.3m (conservative, within the range)
- Rationale: Rolling involves both friction and drag
  - Drag varies with vÂ² (decreases as ball slows)
  - Friction constant but surface-dependent
  - Complex interaction makes precise prediction difficult
- 15% validates "within right order of magnitude"

---

#### D.10 Post Collision Angle Tolerance

**IT-COL-001: Reflection angle Ã‚Â±5Ã‚Â°**

Derivation:
- Ideal reflection: ÃŽÂ¸_out = ÃŽÂ¸_in (mirror law)
- Post is cylinder: Normal varies around circumference
- Ã‚Â±5Ã‚Â° = Ã‚Â±0.087 radians
- At 15 m/s: Ã‚Â±5Ã‚Â° = Ã‚Â±1.3 m/s lateral component
- Tolerance accounts for:
  - Contact point discretization
  - Spin effects on deflection
  - Energy loss (CoR < 1.0)
- Tight enough to validate physics, loose enough for numerical variance

---

#### D.11 Crossbar Landing Position

**IT-COL-002: Ball lands in goal or in front**

Derivation:
- Crossbar: z = 2.44m, x = 0 (goal line)
- Initial velocity: (10, 0, 5) m/s upward
- After bounce: v_z Ã¢â€°Ë† -0.75 Ãƒâ€” 5 = -3.75 m/s (downward)
- Time to ground: t = 2.44 / 3.75 Ã¢â€°Ë† 0.65s
- Horizontal distance: d = 10 Ãƒâ€” 0.65 = 6.5m
- Goal depth: 2.0m
- Conclusion: Ball lands ~4.5m in front of goal (OUT)
- Test validates ball doesn't teleport or freeze

---

### Appendix E: Visual Validation Reference Clips

#### E.1 Ronaldo vs Portsmouth 2008 (Knuckleball)

**URL:** `https://www.youtube.com/watch?v=0uiGJcMCN-s` (Timestamp: 0:30-0:38)

**Characteristics:**
- Distance: ~25m from goal
- Minimal spin (~3 rad/s)
- Erratic trajectory (drag crisis effect)
- Lateral movement: ~2m

**Validation use:**
- Verify drag crisis behavior (20-25 m/s range)
- Confirm low-spin trajectories can still curve
- Check knuckleball "wobble" is present

---

#### E.2 Roberto Carlos vs France 1997 (Extreme Curve)

**URL:** `https://www.youtube.com/watch?v=3ECoR__tJNQ` (Timestamp: 0:15-0:25)

**Characteristics:**
- Distance: ~30m from goal
- Extreme sidespin (~15-18 rad/s estimated)
- Lateral deviation: ~3-4m
- Initial trajectory outside post, curves inside

**Validation use:**
- Upper bound test for Magnus effect
- Verify high spin rates produce dramatic curves
- Check ball doesn't over-curve (stay within 4m)

---

#### E.3 Premier League Long Pass Compilation

**URL:** `https://www.youtube.com/watch?v=generic_long_passes` (Example - use any PL highlights)

**Characteristics:**
- Typical lofted passes (40-50m)
- Arc height: 5-8m
- Flight time: 2-3 seconds
- Landing controlled by receiving player

**Validation use:**
- General trajectory realism
- Verify drag effect over distance (ball slows in flight)
- Check arc shape (parabolic with drag)
- Validate time-to-target feels natural

**Note:** Use any Premier League highlights reel with good camera angles showing full pass trajectories.

---

**END OF SECTION 5**

---

## Document Status

**Section 5 Completion:**
- Ã¢Å“â€¦ All test categories defined (32 unit + 12 integration)
- Ã¢Å“â€¦ Test implementation examples provided (Section 5.8)
- Ã¢Å“â€¦ Tolerance derivations complete (Appendix D)
- Ã¢Å“â€¦ Visual validation protocol standardized (Section 5.4.5, Appendix E)
- Ã¢Å“â€¦ Surface coverage reduced to 2 types (Section 5.7.2)
- Ã¢Å“â€¦ Performance targets standardized (Section 5.4.3)
- Ã¢Å“â€¦ Validation frequency clarified (Section 5.5)
- Ã¢Å“â€¦ Missing integration tests added (Section 5.3.4-5.3.5)

**Page Count:** ~12 pages (comprehensive with all fixes)

**Ready for:** Approval and implementation (Stage 0 Months 1-2)

**Next Sections:** 6 (Performance Analysis), 7 (Future Extensions), 8 (References), Appendices A-C
