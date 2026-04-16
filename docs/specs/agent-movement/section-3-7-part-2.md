## 3.7.3 Integration Test Scenarios

### 3.7.3.1 Full Movement Sequence Tests (3 scenarios)

**Test ID: IT-MOV-001**  
**Name:** `Sprint_Turn_Decelerate_Stop_Sequence`  
**Purpose:** Validate complete movement lifecycle  
**Setup:**
1. Agent at IDLE, Pace 14, Agility 12, Acceleration 13, neutral context
2. Command: Sprint to position 30m ahead
3. At 20m: command 45Ã‚Â° direction change
4. At 28m: command stop  
**Expected:**
- Acceleration follows exponential curve (Section 3.2.3)
- State transitions: IDLE Ã¢â€ â€™ WALKING Ã¢â€ â€™ JOGGING Ã¢â€ â€™ SPRINTING
- Turn at 20m: turn rate constrained by speed, radius consistent with 3.4.3
- Stop at 28m: DECELERATING Ã¢â€ â€™ WALKING Ã¢â€ â€™ IDLE (controlled deceleration distance 3Ã¢â‚¬â€œ5m for Agility 12)
- Total trajectory physically plausible: velocity never exceeds top speed, acceleration never exceeds MAX_ACCELERATION Ãƒâ€” dt  
**Duration:** ~6 seconds simulated time  
**Validation:** Record position every frame; verify all constraints

---

**Test ID: IT-MOV-002**  
**Name:** `Jockey_BackwardMovement_WithFacingLock`  
**Purpose:** Validate defensive jockeying scenario Ã¢â‚¬â€ backward movement with independent facing  
**Setup:**
1. Agent JOGGING forward at 4.0 m/s, facing north
2. Command: Move backward (south) while maintaining north-facing  
**Expected:**
- Movement direction reverses, facing direction unchanged
- Effective speed reduced by backward multiplier (0.45Ã¢â‚¬â€œ0.55 Ãƒâ€” forward speed, per FR-4)
- Maximum backward speed Ã¢â€°Ë† 4.0 Ãƒâ€” 0.50 = 2.0 m/s (mid-range Agility)
- Agent decelerates first (Section 3.6.3 direction reversal handling), then accelerates backward
- No state machine oscillation during reversal  
**Duration:** 4 seconds simulated time  
**Section Ref:** 3.3.4, FR-4

---

**Test ID: IT-MOV-003** *(v1.1: sourced distribution)*  
**Name:** `Fatigue_FullMatch_SpeedDegradation`  
**Purpose:** Validate 90-minute fatigue arc produces visible speed reduction  
**Setup:**
1. Agent with Pace 15, Stamina 10 (mid-range)
2. Simulate 90 minutes of scripted mixed-intensity movement:
   - 10% IDLE, 30% WALKING, 40% JOGGING, 20% SPRINTING
   - Source: [BRADLEY-2009] Table 2, mean activity profile for midfield players. The 20% sprint allocation is approximate (actual varies 8Ã¢â‚¬â€œ12% by intensity band; compressed here for conservative fatigue testing).
3. Record top speed achievable every 15 minutes  
**Expected:**
- Minute 0: top speed Ã¢â€°Ë† 9.49 m/s (fresh)
- Minute 45: top speed reduced by 0Ã¢â‚¬â€œ10% (aerobic pool likely still above 0.5 threshold)
- Minute 90: top speed reduced by 15Ã¢â‚¬â€œ30% (QR-1: >15% at exhaustion)
- Sprint reservoir depletes faster in second half
- Speed reduction monotonically non-decreasing (never getting faster as match progresses)  
**Duration:** 90 minutes simulated time (54,000 ticks)  
**Tolerance:** Speed reduction percentages Ã‚Â±5%  
**Note:** This test uses a scripted activity profile, not AI-driven movement. Full AI-driven validation deferred to post-Spec #7 integration (see Section 3.7.4.3).

---

### 3.7.3.2 Multi-Agent Scenarios (3 scenarios)

**Test ID: IT-MA-001**  
**Name:** `TwentyAgents_SimultaneousMovement_NoInterference`  
**Purpose:** Validate agent independence Ã¢â‚¬â€ no shared state corruption  
**Setup:**
1. 20 agents with varied attributes (Pace 5Ã¢â‚¬â€œ20, Agility 3Ã¢â‚¬â€œ18, etc.)
2. All agents moving simultaneously with different commands  
**Expected:**
- Each agent's physics computed independently
- No cross-agent state contamination
- All agents within valid position/velocity bounds
- Performance within PR-1 budget (<1.0ms total)  
**Duration:** 30 seconds simulated time  
**Section Ref:** PR-1, QR-2

---

**Test ID: IT-MA-002**  
**Name:** `AttributeSpectrum_VisibleDifferences`  
**Purpose:** Validate QR-4: attribute sensitivity across full range  
**Setup:**
1. 20 agents with Pace values 1 through 20 (one per value)
2. All agents sprint same 50m straight line  
**Expected:**
- Pace 20 agent finishes first
- Pace 1 agent finishes last
- Arrival times spread across range (not clustered)
- Ordering strictly monotonic with Pace value  
**Tolerance:** No inversions in arrival order  
**Duration:** 10 seconds simulated time

---

**Test ID: IT-MA-003**  
**Name:** `IdenticalAgents_IdenticalResults_Determinism`  
**Purpose:** Validate FR-9 determinism  
**Setup:**
1. Two agents with identical attributes, identical initial state, identical commands
2. Run same 30-second movement sequence twice  
**Expected:**
- Final positions identical (bit-exact if using Fixed64, <0.001m if float)
- Full trajectory point-by-point match
- State transition timestamps identical  
**Tolerance:** Position difference < 0.001m (float mode)  
**Section Ref:** FR-9

---

### 3.7.3.3 Cross-System Integration Tests (4 scenarios)

**Test ID: IT-CS-001**  
**Name:** `StateTransition_DrivesCorrectLocomotionFormula`  
**Purpose:** Validate state machine correctly selects physics model  
**Setup:** Agent transitions through IDLE Ã¢â€ â€™ WALKING Ã¢â€ â€™ JOGGING Ã¢â€ â€™ SPRINTING Ã¢â€ â€™ DECELERATING Ã¢â€ â€™ IDLE  
**Expected:**
- IDLE: zero/minimal acceleration applied (drift reduction only)
- WALKING: linear acceleration at WALK_ACCELERATION (2.0 m/sÃ‚Â²)
- JOGGING: exponential acceleration with attribute-derived k
- SPRINTING: exponential acceleration with turn radius constraint active
- DECELERATING: constant deceleration at attribute-derived rate  
**Verification:** Log which formula function is called per frame; verify matches state  
**Section Ref:** 3.1, 3.2

---

**Test ID: IT-CS-002**  
**Name:** `DirectionalMultiplier_AffectsTurnRate`  
**Purpose:** Validate cross-section interaction: directional penalties affect turning  
**Setup:** Agent sprinting forward, commanded to move laterally (90Ã‚Â°) while turning to face new direction  
**Expected:**
- Effective speed reduced by lateral multiplier
- Turn rate appropriate for the reduced speed (not original speed)
- No inconsistency between Section 3.3 and 3.4 calculations  
**Section Ref:** 3.3.5, 3.4.2

---

**Test ID: IT-CS-003**  
**Name:** `AnimationContract_OutputsEveryFrame`  
**Purpose:** Validate FR-8 animation data contract  
**Setup:** Agent moving through all states for 10 seconds  
**Expected:**
- Every frame produces valid AnimationData: stride frequency > 0 (when moving), foot plant phase in [0.0, 1.0], lean angle in valid range, movement state populated
- No NaN or default values in output
- Data changes smoothly between frames (no single-frame spikes)  
**Section Ref:** FR-8

---

**Test ID: IT-CS-004**  
**Name:** `EventLogging_CriticalEventsRecorded`  
**Purpose:** Validate replay-critical events are logged  
**Setup:** Agent performs: sprint start, stumble, recovery, stop  
**Expected:**
- AgentSprintStartEvent logged with timestamp and agent ID
- AgentStumbleEvent logged with position, velocity, and cause
- All events in chronological order
- Ring buffer does not overflow during normal play  
**Section Ref:** 1.4 (Event System interface)

---

### Integration Test Summary

| Category | Test IDs | Count |
|---|---|---|
| Full Movement Sequences | IT-MOV-001 to IT-MOV-003 | 3 |
| Multi-Agent | IT-MA-001 to IT-MA-003 | 3 |
| Cross-System | IT-CS-001 to IT-CS-004 | 4 |
| **Total (this section)** | | **10** |
| Edge case integration (Section 3.6.7) | IT-EDGE-001 to IT-EDGE-005 | 5 |
| **Grand Total** | | **15** |

---

## 3.7.4 Real-World Validation Benchmarks

### 3.7.4.1 Data Sources

**Primary:** GPS tracking data from professional matches (publicly available via academic publications).

- **[BRADLEY-2009]** Bradley, P.S. et al. (2009). "High-intensity running in English FA Premier League soccer matches." *Journal of Sports Sciences*, 27(2), 159-168.
  - Mean total distance: 10.7 km per match
  - Sprint threshold: >7.0 m/s
  - High-intensity running: 5.5Ã¢â‚¬â€œ7.0 m/s

- **[DI SALVO-2007]** Di Salvo, V. et al. (2007). "Performance characteristics according to playing position in elite soccer." *International Journal of Sports Medicine*, 28(3), 222-227.
  - Position-specific distances: midfielders ~12 km, centre-backs ~10 km

- **[STOLEN-2005]** Stolen, T. et al. (2005). "Physiology of Soccer." *Sports Medicine*, 35(6), 501-536.
  - Top speed: elite players 9.0Ã¢â‚¬â€œ10.2 m/s (validates FR-2)
  - Acceleration: 0Ã¢â‚¬â€œ30m sprint times used to derive curves

### 3.7.4.2 Immediately Testable Benchmarks

These benchmarks can be validated during Stage 0 implementation without AI integration.

**VB-1: Sprint Speed Range**
- **Metric:** Top speed achievable per agent
- **Expected:** 7.5Ã¢â‚¬â€œ10.2 m/s across Pace 1Ã¢â‚¬â€œ20
- **Source:** [STOLEN-2005]
- **Test:** UT-SPD-001 through UT-SPD-005
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-2: Time to Top Speed**
- **Metric:** Seconds from standstill to 90% of top speed
- **Expected:** 2.5Ã¢â‚¬â€œ3.5 seconds (attribute-dependent)
- **Source:** [STOLEN-2005], Section 3.2.2 table
- **Test:** UT-ACC-003
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-5: Stop Distance**
- **Metric:** Distance from sprint speed to full stop
- **Expected:** 3.0Ã¢â‚¬â€œ5.0m controlled (Agility-dependent), 2.5Ã¢â‚¬â€œ3.5m emergency (Agility-dependent)
- **Source:** Derived from deceleration rates in Section 3.2.5
- **Test:** UT-DEC-001, UT-DEC-002
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-6: Fatigue Speed Reduction**
- **Metric:** Percentage decrease in top speed at minute 90 vs minute 0
- **Expected:** 15Ã¢â‚¬â€œ30% reduction (scripted activity profile)
- **Source:** [BRADLEY-2009], [STOLEN-2005]
- **Test:** IT-MOV-003
- **Status:** TESTABLE AT IMPLEMENTATION (scripted profile)

**VB-7: Backward Movement Speed**
- **Metric:** Ratio of backward speed to forward speed
- **Expected:** 0.45Ã¢â‚¬â€œ0.55Ãƒâ€” forward speed
- **Source:** Biomechanical estimates
- **Test:** UT-DIR-003
- **Status:** TESTABLE AT IMPLEMENTATION

### 3.7.4.3 Deferred Validation Benchmarks

These benchmarks require AI-driven movement (Spec #7: Decision Tree, Spec #12: Positioning AI) and CANNOT be validated during Agent Movement Spec #2 implementation. They are NOT part of the Stage 0 approval criteria for this spec.

**VB-3: Total Distance Per Match** *(DEFERRED)*
- **Metric:** Summed agent distance over 90 minutes
- **Expected:** 9.5Ã¢â‚¬â€œ12.5 km per agent (position-dependent)
- **Source:** [BRADLEY-2009]
- **Prerequisite:** AI-driven movement patterns (Spec #7+)
- **Preliminary proxy:** IT-MOV-003 with scripted movement provides partial coverage
- **Target validation date:** After Spec #7 implementation

**VB-4: Sprint Distance Per Match** *(DEFERRED)*
- **Metric:** Cumulative distance covered at >7.0 m/s
- **Expected:** 500Ã¢â‚¬â€œ1200m per agent per match
- **Source:** [BRADLEY-2009]
- **Prerequisite:** AI-driven sprint decisions (Spec #7+)
- **Target validation date:** After Spec #7 implementation

### 3.7.4.4 Visual Validation Protocol

**After all unit and integration tests pass:**

1. Record 5 different movement scenarios in simulation:
   a. 50m sprint from standstill
   b. Sprint with 90Ã‚Â° direction change
   c. Defensive jockeying (backward/lateral)
   d. Stop-start sequence (receive ball, turn, sprint)
   e. End-of-match fatigued agent movement

2. Compare side-by-side with broadcast match footage

3. Survey: minimum 10 football players/fans (recruitment via r/bootroom)
   - Question: "Does this agent movement look realistic? (Yes/No/Unsure)"
   - Required: >80% "Yes" responses per scenario

**Validation timing:** After all unit/integration tests pass, before Stage 0 quality gate.

### 3.7.4.5 Reference Data Storage

```csharp
/// <summary>
/// Reference data for Agent Movement validation.
/// All values derived from published sports science data.
/// 
/// Sources:
///   BRADLEY-2009: GPS tracking, Premier League
///   STOLEN-2005: Football physiology review
///   DI SALVO-2007: Position-specific performance
/// 
/// Created: 2026-02-11 from Section 3.7.4 benchmarks
/// </summary>
public static class AgentMovementBenchmarks
{
    // VB-1: Sprint speed range (Section 3.2.4)
    public static readonly float MIN_TOP_SPEED = 7.5f;   // Pace 1 (m/s)
    public static readonly float MAX_TOP_SPEED = 10.2f;   // Pace 20 (m/s)
    
    // VB-2: Time to 90% top speed (Section 3.2.2)
    public static readonly float MIN_TIME_TO_TOP = 2.5f;  // best attributes (s)
    public static readonly float MAX_TIME_TO_TOP = 3.5f;  // worst attributes (s)
    
    // VB-5: Stop distances (Section 3.2.5)
    public static readonly float CONTROLLED_STOP_MIN = 3.0f;  // meters (Agility 20)
    public static readonly float CONTROLLED_STOP_MAX = 5.0f;  // meters (Agility 1)
    public static readonly float EMERGENCY_STOP_MIN = 2.5f;   // meters (Agility 20)
    public static readonly float EMERGENCY_STOP_MAX = 3.5f;   // meters (Agility 1)
}
```

---

## 3.7.5 Regression Testing Requirements

### 3.7.5.1 Trigger Conditions

**Full regression sweep required when:**

1. **Any constant change** in MovementConstants or MovementThresholds (Section 3.5, 3.1)
2. **Any formula change** in acceleration, deceleration, turn rate, or directional multiplier functions
3. **Any state machine modification** Ã¢â‚¬â€ new states, changed transitions, threshold adjustments
4. **Any refactor** Ã¢â‚¬â€ code reorganization, performance optimization, dependency changes
5. **Integration of new spec** Ã¢â‚¬â€ when Collision System (Spec #3), First Touch (Spec #11), or other consuming specs are connected

### 3.7.5.2 Regression Protocol

**Before any constant tuning:**

1. **Baseline capture:** Record commit SHA; run full test suite; save results as `baseline_{SHA}.json` including all 68 unit tests, 15 integration tests, and VB-1/2/5/6/7 benchmark values.

2. **Apply change:** Modify constant(s); document rationale in code comment and changelog.

3. **Regression check:** Run full test suite; compare against baseline. Flag any test that changed status. Flag any benchmark value that changed by more than Ã‚Â±5%.

4. **Review & commit:** If only intended tests changed, approve. If unintended tests changed, investigate before committing. Update golden trajectories if benchmarks legitimately shifted.

### 3.7.5.3 Golden Trajectory Tests

**Purpose:** Detect unintended physics changes by comparing full agent trajectories against recorded baselines.

**Dual-tolerance system** *(v1.1)*:
- **Per-frame position delta:** Ã‚Â±0.05m (5cm). Accounts for float accumulation across individual frames. Derived from: at MAX_SPEED (12 m/s) Ãƒâ€” dt (0.0167s) = 0.2m per frame max travel; 0.05m tolerance = 25% of max single-frame displacement, sufficient to catch formula errors while tolerating float variance.
- **End-of-trajectory final position:** Ã‚Â±0.15m (15cm). Accounts for cumulative drift over test duration. Derived from: PR-3 specifies <5cm drift per agent per 90 minutes; golden trajectory tests are 3Ã¢â‚¬â€œ6s long (0.06Ã¢â‚¬â€œ0.11% of a full match); 0.15m provides >100Ãƒâ€” headroom over the prorated drift budget, which is intentionally generous because golden trajectories compare against themselves (not an absolute target) and the primary purpose is detecting *regressions*, not absolute accuracy.

**Golden trajectories to maintain:**

| ID | Scenario | Key Attributes | Duration |
|---|---|---|---|
| GT-001 | 50m straight sprint | Pace 15, Acc 12 | ~6s |
| GT-002 | Sprint + 45Ã‚Â° turn | Pace 12, Agility 14 | ~5s |
| GT-003 | Backward jockey 10m | Pace 10, Agility 16 | ~4s |
| GT-004 | Fatigued sprint (aerobic 0.4) | Pace 15, Stamina 10 | ~6s |
| GT-005 | Full stop from sprint | Pace 14, Agility 11 | ~3s |

**Regeneration rule:** Golden trajectories MUST be regenerated whenever an intentional physics change is made. The regeneration MUST be documented in the changelog with reason and commit SHA of new baseline.

---

## 3.7.6 Performance Testing Criteria

### 3.7.6.1 Budget Targets

| Metric | Target | Source |
|---|---|---|
| All 20 agents per frame (p95) | <1.0ms | PR-1 |
| Per-agent update (mean) | <0.05ms (50Ã‚Âµs) | PR-1 |
| All 20 agents (mean) | <0.7ms | PR-1 |
| All 20 agents (p99) | <1.5ms | Investigation trigger |
| Heap allocations | Zero during UpdateAgentMovement() | PR-2 |
| GC pressure | None attributable to movement | PR-2 |
| AgentMovementState size | Ã¢â€°Â¤256 bytes | PR-2 |
| Total movement memory | <5KB for 20 agents | PR-2 |

### 3.7.6.2 Performance Test Protocol

1. Run 100 simulated matches (5400 seconds simulated time each)
2. 20 outfield agents per match, varied attribute distributions
3. Profile every frame at 60Hz (324,000 frames per match)
4. Agents executing mixed movement patterns (not idle Ã¢â‚¬â€ worst-case workload)

**Investigation triggers:**
- Any frame >1.5ms: log full call stack, identify hotspot
- >0.1% of frames exceeding 1.0ms: performance optimization required
- Any heap allocation: treat as bug, fix immediately

### 3.7.6.3 Profiling Markers

Required custom Unity Profiler markers:

```
AgentMovement.UpdateAll          (top-level, all 20 agents)
  AgentMovement.UpdateSingle     (per-agent, development builds only)
    AgentMovement.StateMachine   (~10% budget)
    AgentMovement.Locomotion     (~35% budget)
    AgentMovement.Directional    (~15% budget)
    AgentMovement.Turning        (~25% budget)
    AgentMovement.Validation     (~15% budget)
```

Budget percentages are advisory estimates, not hard limits. Actual distribution will be confirmed by profiling during implementation.

---

## 3.7.7 Test Execution Plan

### 3.7.7.1 Development Phase (Stage 0 Months 3-4)

- Write unit test alongside each function (TDD approach)
- Run unit tests after every code change; commit only when tests pass
- Target: green tests 95% of time
- Weekly: run full test suite every Friday; review coverage; address gaps

### 3.7.7.2 Pre-Approval

**Before Agent Movement Spec #2 implementation approval:**
- All 68 unit tests pass (49 functional + 19 edge case)
- All 15 integration tests pass (10 functional + 5 edge case)
- Code coverage >80% for core locomotion, >70% for state machine
- Performance targets met (p95 <1.0ms for 20 agents)
- VB-1, VB-2, VB-5, VB-6, VB-7 within specified ranges
- Visual validation completed (>80% approval)

### 3.7.7.3 Stage 0 Quality Gate

**Before advancing to Stage 1:**
- 1000 simulated matches: zero critical failures, <5 validation warnings per match per agent
- All immediately-testable benchmarks within ranges
- Golden trajectories match baselines
- Community feedback positive (>75% on r/gamedev)

### 3.7.7.4 Continuous Regression

- Unit tests: every commit (local)
- Integration tests: nightly (if CI/CD available)
- Performance tests: weekly
- Golden trajectory comparison: every physics-affecting commit
- Benchmark validation: monthly

---

## 3.7.8 Acceptance Criteria Summary

### Mandatory (must pass for Stage 0 approval)

**Unit Tests:**
- [ ] All 49 functional unit tests pass (Section 3.7.2)
- [ ] All 19 edge case unit tests pass (Section 3.6.7)
- [ ] Code coverage >80% for AgentMovementCore
- [ ] Code coverage >70% for AgentStateMachine
- [ ] Zero compiler warnings
- [ ] Test suite completes in <15 seconds

**Integration Tests:**
- [ ] All 10 functional integration tests pass (Section 3.7.3)
- [ ] All 5 edge case integration tests pass (Section 3.6.7)
- [ ] No state machine oscillations (max 6 transitions/second)
- [ ] 20-agent simultaneous operation stable for 90 minutes

**Performance:**
- [ ] All 20 agents update in <1.0ms per frame (p95)
- [ ] Per-agent update <0.05ms (mean)
- [ ] Zero heap allocations during movement update
- [ ] AgentMovementState Ã¢â€°Â¤256 bytes per agent

**Immediately Testable Benchmarks:**
- [ ] VB-1: Sprint speed range 7.5Ã¢â‚¬â€œ10.2 m/s
- [ ] VB-2: Time to 90% top speed 2.5Ã¢â‚¬â€œ3.5s
- [ ] VB-5: Stop distances within specified ranges
- [ ] VB-6: Fatigue reduction >15% at minute 90 (scripted profile)
- [ ] VB-7: Backward multiplier 0.45Ã¢â‚¬â€œ0.55Ãƒâ€”
- [ ] Visual validation >80% "Yes" responses

**Stability:**
- [ ] Zero NaN or Infinity values in 1000-match simulation
- [ ] Position drift <5cm per agent after 90 minutes
- [ ] All failure modes trigger recovery (no crashes)

**Determinism:**
- [ ] Identical agents + identical commands = identical results
- [ ] Replay reconstruction produces matching trajectories

### Quality (should pass, non-blocking)

- [ ] All magic numbers replaced with named constants
- [ ] Test names clearly describe purpose
- [ ] Expected values include derivation comments
- [ ] Tolerance values justified in Section 3.7.9
- [ ] Golden trajectories established for 5 baseline scenarios
- [ ] Regression protocol documented and first baseline captured

### Deferred (NOT required for this spec's approval)

- [ ] VB-3: Total match distance (requires AI integration, Spec #7+)
- [ ] VB-4: Sprint distance per match (requires AI integration, Spec #7+)

---

## 3.7.9 Tolerance Derivations

**Purpose:** Formal derivation and rationale for every tolerance value used in this section, mirroring Ball Physics Appendix D pattern. Every non-exact tolerance must have an entry here.

### 3.7.9.1 Float Arithmetic Precision

**Applies to:** UT-PC-004, UT-SPD-001 through UT-SPD-003

**Derivation:** IEEE 754 single-precision float has ~7 significant digits. For values in the 1Ã¢â‚¬â€œ20 range, machine epsilon Ã¢â€°Ë† 1.2 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ· Ãƒâ€” value Ã¢â€°Ë† 2.4 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ¶. A tolerance of Ã‚Â±0.01 provides >4000Ãƒâ€” headroom over float precision error. This generous margin accounts for multiple chained operations (attribute evaluation Ã¢â€ â€™ mapping Ã¢â€ â€™ modifier chain) while still catching any formula error that would produce a wrong integer part.

### 3.7.9.2 Acceleration Curve Tolerances

**Applies to:** UT-ACC-001 (Ã‚Â±0.15 m/s), UT-ACC-002 (>0.4 m/s difference), UT-ACC-006 (>5.0 m/s)

**Derivation for UT-ACC-001:** The discrete 60Hz integration introduces error vs. the continuous formula. Section 3.2.3 documents this error as <0.01 m/s over a full 3.5s acceleration. At t=0.5s (30 frames), error is proportionally smaller. However, frame timing jitter (Ã‚Â±0.5 frames) at k=0.8518 produces velocity uncertainty of Ã‚Â±0.8518 Ãƒâ€” 9.49 Ãƒâ€” e^(-0.4259) Ãƒâ€” dt Ã¢â€°Ë† Ã‚Â±0.088 m/s per frame boundary. Adding Ã‚Â±0.01 integration error + Ã‚Â±0.088 jitter + Ã‚Â±0.05 safety margin = Ã‚Â±0.148, rounded to Ã‚Â±0.15 m/s.

**Derivation for UT-ACC-002:** Expected difference Ã¢â€°Ë† 0.60 m/s. Conservative bound of >0.4 m/s allows for Ã‚Â±0.15 m/s tolerance on each agent's speed measurement (2 Ãƒâ€” 0.15 = 0.30 subtracted from 0.60 leaves 0.30 headroom, rounded down to 0.4 threshold for additional safety).

### 3.7.9.3 Time-to-Speed Tolerance

**Applies to:** UT-ACC-003 (Ã‚Â±0.3s)

**Derivation:** TÃ¢â€šâ€°Ã¢â€šâ‚¬ = 2.3026 / k. At k = 0.7826 (Acc=10), TÃ¢â€šâ€°Ã¢â€šâ‚¬ = 2.94s. Frame granularity at 60Hz = 16.67ms. The 90% threshold is crossed mid-frame; actual crossing time depends on whether the check runs before or after the speed update. This introduces Ã‚Â±1 frame uncertainty (Ã‚Â±0.017s). The Ã‚Â±0.3s tolerance provides >17Ãƒâ€” headroom over measurement uncertainty, which is intentionally generous to allow for minor constant tuning without breaking this test. If the implementation produces TÃ¢â€šâ€°Ã¢â€šâ‚¬ outside 2.6Ã¢â‚¬â€œ3.2s for Acceleration 10, it indicates a formula error or constant mismatch.

### 3.7.9.4 Fatigue Modifier Tolerance

**Applies to:** UT-ACC-004 (>10% reduction), UT-SPD-004 (10%Ã¢â‚¬â€œ15% reduction)

**Derivation:** At aerobic pool 0.3, the modifier formula produces exactly 0.88 (Section 3.2.4). This gives 12% speed reduction. The >10% lower bound provides 2% headroom below the exact answer. The 15% upper bound accounts for the possibility that fatigue also affects acceleration rate (Section 3.4.2), which may cause the agent to not fully reach their reduced top speed within the measurement window.

### 3.7.9.5 Deceleration Distance Tolerances

**Applies to:** UT-DEC-001 (Ã‚Â±0.4m), UT-DEC-002 (Ã‚Â±0.4m), UT-DEC-005 (>0.5m difference)

**Derivation:** The continuous formula d = vÃ¢â€šâ‚¬Ã‚Â²/(2a) gives the theoretical stopping distance. Discrete integration at 60Hz adds up to 1 frame of additional travel: MAX_SPEED Ãƒâ€” dt = 12.0 Ãƒâ€” 0.0167 = 0.20m worst case. The Ã‚Â±0.4m tolerance provides 2Ãƒâ€” headroom over the discrete error, accounting for the speed-dependent nature of the last-frame overshoot (faster agents overshoot more) and any tolerance stacking from the deceleration rate mapping.

### 3.7.9.6 Directional Multiplier Tolerances

**Applies to:** UT-DIR-002 (Ã‚Â±0.02), UT-DIR-004 (>0.05 difference), UT-DIR-005 (<0.03 jump), UT-DIR-006 (Ã‚Â±0.3 m/s), UT-DIR-007 (>0.15s)

**Derivation for UT-DIR-002:** FR-4 specifies 0.65Ã¢â‚¬â€œ0.75 range for lateral movement. The Ã‚Â±0.02 tolerance allows the implementation to sit anywhere in a [0.63, 0.77] band, which is intentionally wider than the FR-4 range to avoid test brittleness during tuning. If the multiplier falls outside [0.63, 0.77], it's either wrong or the FR-4 range needs updating.

**Derivation for UT-DIR-005:** Zone boundary smoothing should produce no discontinuity. A 0.03 maximum jump between adjacent 1Ã‚Â° angles is imperceptible in gameplay (0.03 Ãƒâ€” 10 m/s = 0.3 m/s difference for 1Ã‚Â° of angle change). Larger jumps indicate a missing interpolation or a step function at the boundary.


**Agility 1 exemption (v1.2):** At Agility 1, the lateral multiplier is 0.65 (lowest in
FR-4 range), producing a forward-lateral jump of (1.0 - 0.65) / 10 deg = 0.035/degree.
This exceeds the 0.03 threshold by 17%. However, 0.035/degree at 9 m/s = 0.315 m/s per
degree, still imperceptible. Agility 1 is explicitly exempt; test implementations should
skip this test case when input Agility = 1, or use a relaxed threshold of 0.04.

### 3.7.9.7 Turning & Stumble Tolerances

**Applies to:** UT-TRN-003 (>0.12s), UT-TRN-004 (Ã‚Â±15% ratio), UT-TRN-006 (>0.08 probability), UT-TRN-007 (>2Ã‚Â° lean)

**Derivation for UT-TRN-003:** Turn rate at Agility 5 vs Agility 18 at 7.0 m/s. The exact difference depends on the turn rate formula (Section 3.4.2) which maps Agility to degrees/second. With the full attribute range (5 to 18 = 13 points), and turn rate per point estimated at ~5Ã¢â‚¬â€œ10Ã‚Â°/s at sprint-adjacent speeds, the expected time difference for a 90Ã‚Â° turn is 0.15Ã¢â‚¬â€œ0.30s. The >0.12s threshold is conservatively below the lower bound.

**Derivation for UT-TRN-004:** r_min Ã¢Ë†Â vÃ‚Â². The ratio r(6)/r(3) should theoretically = 4.0. The Ã‚Â±15% tolerance allows [3.4, 4.6], accounting for the agility_factor denominator which may not perfectly cancel between speed levels if there are any speed-dependent agility scaling effects.

---

## End of Section 3.7

**Page Count:** ~26 pages  
**Next Section:** Section 4 Ã¢â‚¬â€ Implementation Details (Code Organization, Dependencies, Configuration)

---

## CROSS-REFERENCES

**This section derives test requirements from:**
- Section 2.1 (Functional Requirements FR-1 through FR-9)
- Section 2.2 (Performance Requirements PR-1 through PR-3)
- Section 2.3 (Quality Requirements QR-1 through QR-4)
- Section 3.1 (State Machine Ã¢â‚¬â€ transition logic, hysteresis thresholds, dwell times)
- Section 3.2 (Locomotion Ã¢â‚¬â€ acceleration curves, top speed mapping, deceleration rates, fatigue modifiers)
- Section 3.3 (Directional Movement Ã¢â‚¬â€ zone multipliers, facing independence)
- Section 3.4 (Turning Ã¢â‚¬â€ turn rate, min radius, stumble risk, lean angle)
- Section 3.5 (Data Structures Ã¢â‚¬â€ MovementConstants, AgentMovementState)
- Section 3.6 (Edge Cases Ã¢â‚¬â€ error recovery, boundary conditions)
- Ball Physics Spec #1, Section 5 (testing pattern reference)
- Ball Physics Spec #1, Appendix D (tolerance derivation pattern reference)

**This section is consumed by:**
- Section 4 (Implementation Details Ã¢â‚¬â€ test file organization)
- Development Best Practices (regression testing workflow)
- Spec #19 (Testing Strategy & Framework Ã¢â‚¬â€ cross-spec test architecture)
