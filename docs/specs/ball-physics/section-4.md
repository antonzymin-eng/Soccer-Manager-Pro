# Ball Physics Specification - Section 4: Implementation Details

**Created:** February 4, 2026, 12:35 AM PST  
**Version:** 1.2 (Revised)  
**Status:** Draft  
**Changes from v1.1:**
- Fixed BallState struct size: 56 bytes → 64 bytes (corrected field count; cache-line claim unchanged)
- Added ApplyKick() and SetBallControlled() to §4.1.1 file structure (added in §3.1 v2.5)
- Fixed §4.4.3 agent interface: replaced `ball.IsPossessed` with `ball.State != CONTROLLED` (Option B)
- Corrected spec numbers: Pass Mechanics Spec #4 → #5, Shot Mechanics Spec #5 → #6

---

## 4. IMPLEMENTATION DETAILS

### 4.1 Code Organization

#### 4.1.1 File Structure

```
Core/Physics/Ball/
â”œâ”€â”€ BallState.cs                    // Data structure (64 bytes struct)
â”œâ”€â”€ BallPhysicsConstants.cs         // Organized constants (nested classes)
â”œâ”€â”€ BallPhysicsCore.cs             // Main physics calculations
â”‚   â”œâ”€â”€ UpdateBallPhysics()        // Main update loop
â”‚   â”œâ”€â”€ CalculateMagnusForce()     // Magnus effect
â”‚   â”œâ”€â”€ CalculateDragForce()       // Aerodynamic drag
â”‚   â”œâ”€â”€ UpdateSpinDecay()          // Spin dynamics
â”‚   â””â”€â”€ ValidatePhysicsState()     // Safety checks
â”œâ”€â”€ BallStateMachine.cs            // State transitions
â”‚   â”œâ”€â”€ UpdateBallState()          // State machine logic
â”‚   â””â”€â”€ IsOutOfBounds()            // Boundary detection
â”œâ”€â”€ BallGroundInteraction.cs       // Ground physics
â”‚   â”œâ”€â”€ ApplyBounce()              // Impulse-based bounce
â”‚   â””â”€â”€ CalculateRollingFriction() // Rolling resistance
â”œâ”€â”€ BallCollision.cs               // Collision handlers
â”‚   â”œâ”€â”€ ApplyGoalPostCollision()   // Post/crossbar impacts
â”‚   â”œâ”€â”€ CheckBoundaries()          // Out of play detection
â”‚   â””â”€â”€ CheckPossession()          // Agent control check
â"‚   â"œâ"€â"€ SetBallControlled()        // Transition ball to CONTROLLED state
â"‚   â""â"€â"€ ApplyKick()                // Apply kick impulse (added v2.5, ERR-006)
â”œâ”€â”€ BallEventLogger.cs             // Event logging
â”‚   â”œâ”€â”€ LogBounce()
â”‚   â”œâ”€â”€ LogGoalPostHit()
â”‚   â”œâ”€â”€ LogKick()
â”‚   â””â”€â”€ TryLogSnapshot()
â”œâ”€â”€ SurfaceProperties.cs           // Surface coefficients
â”‚   â”œâ”€â”€ GetCoefficientOfRestitution()
â”‚   â”œâ”€â”€ GetFrictionCoefficient()
â”‚   â”œâ”€â”€ GetRollingResistance()
â”‚   â””â”€â”€ GetSpinRetention()
â””â”€â”€ Tests/
    â”œâ”€â”€ BallPhysicsCoreTests.cs    // Unit tests (20+ tests)
    â”œâ”€â”€ BallStateMachineTests.cs   // State transition tests
    â””â”€â”€ BallIntegrationTests.cs    // End-to-end scenarios
```

#### 4.1.2 Namespace Structure

```csharp
namespace TacticalDirector.Core.Physics.Ball
{
    // All ball physics code
}

namespace TacticalDirector.Core.Physics.Ball.Tests
{
    // Test code
}
```

#### 4.1.3 Class Responsibilities

**BallPhysicsCore (Static class)**
- Pure physics calculations
- No state management
- All methods static, side-effect free (except validation warnings)
- Profiling hooks in editor builds

**BallStateMachine (Static class)**
- State transition logic only
- No physics calculations
- Returns new state, doesn't modify ball directly

**BallGroundInteraction (Static class)**
- Ground contact physics
- Bounce mechanics
- Rolling friction

**BallCollision (Static class)**
- Collision detection
- Boundary checks
- Possession evaluation

**BallEventLogger (Instance class)**
- Event history tracking
- Replay data generation
- Ring buffer management

**SurfaceProperties (Static class)**
- Surface coefficient lookup
- Single global surface type for Stage 0

### 4.2 Dependencies

#### 4.2.1 Internal Dependencies

```
BallPhysicsCore
    â”œâ”€> BallState (data structure)
    â”œâ”€> BallPhysicsConstants (constants)
    â”œâ”€> SurfaceProperties (coefficients)
    â””â”€> BallEventLogger (optional, for logging)

BallStateMachine
    â”œâ”€> BallState
    â””â”€> BallPhysicsConstants

BallGroundInteraction
    â”œâ”€> BallState
    â”œâ”€> BallPhysicsConstants
    â”œâ”€> SurfaceProperties
    â””â”€> BallEventLogger (optional)

BallCollision
    â”œâ”€> BallState
    â”œâ”€> BallPhysicsConstants
    â””â”€> BallEventLogger (optional)
```

**Dependency Rules:**
- No circular dependencies
- Logger always optional (null-safe calls)
- Constants never import other classes
- State struct has zero dependencies

#### 4.2.2 External Dependencies

**Unity Engine (Required):**
```csharp
using UnityEngine;           // Vector3, Mathf, Profiling
```

**Stage 0 (Same Stage):**
```csharp
using TacticalDirector.Core.EventSystem;  // For publishing events (Spec #17)
```

**Future Dependencies:**
- `WeatherSystem` - Wind velocity (Stage 1)
- `PitchConditionSystem` - Surface queries (Stage 2)

#### 4.2.3 Optional Dependencies

**Fixed64 Math Library (Spec #8):**
- Stage 0: Use standard floats (acceptable for prototype)
- Stage 5+: Replace with Fixed64 for multiplayer determinism
- Migration path defined in Section 4.7

### 4.3 Configuration

#### 4.3.1 Tunable Parameters

**Located in:** `BallPhysicsConstants.cs`

**Categories:**
1. **Ball Properties** - Fixed (FIFA regulations)
   - `MASS`, `RADIUS`, `CROSS_SECTION_AREA`, `MOMENT_OF_INERTIA`

2. **Aerodynamics** - Tunable (match real footage)
   - `Drag.COEFFICIENT_LAMINAR`, `Drag.COEFFICIENT_TURBULENT`
   - `Drag.CRISIS_SPEED_LOW`, `Drag.CRISIS_SPEED_HIGH`
   - `Magnus.LIFT_COEFFICIENT_BASE`, `Magnus.LIFT_COEFFICIENT_SCALE`

3. **Spin Decay** - Tunable (match observed decay rates)
   - `Spin.DECAY_VELOCITY_FACTOR`, `Spin.DECAY_SPIN_FACTOR`
   - `Spin.TORQUE_COEFFICIENT`

4. **Bounce** - Tunable per surface (match drop tests)
   - `Bounce.COR_GRASS_DEFAULT`, `Bounce.FRICTION_GRASS_DEFAULT`
   - `Bounce.SPIN_RETENTION_GRASS`, `Bounce.SPIN_TO_LINEAR_RATIO`

5. **State Machine** - Tunable (prevent oscillation)
   - `State.AIRBORNE_ENTER_THRESHOLD`, `State.AIRBORNE_EXIT_THRESHOLD`
   - `State.MIN_VELOCITY`, `State.MIN_SPIN`
   - `State.BOUNCE_VELOCITY_THRESHOLD`

6. **Safety Limits** - Fixed (sanity checks)
   - `Limits.MAX_VELOCITY`, `Limits.MAX_SPIN`, `Limits.MAX_HEIGHT`

#### 4.3.2 Configuration Management

**Stage 0 Approach:**
- All constants in code (no external config files)
- Modify `BallPhysicsConstants.cs` directly
- Recompile required for changes
- Version controlled via Git

**Tuning Workflow:**

1. **Record Baseline**
   - Run integration test suite
   - Record test results (curve distance, bounce height, etc.)
   - Commit baseline metrics to Git

2. **Adjust Parameter**
   - Modify single constant in `BallPhysicsConstants.cs`
   - Document reason in code comment

3. **Retest**
   - Run full test suite
   - Compare to baseline metrics
   - Verify no regressions

4. **Visual Validation**
   - Run match simulation
   - Compare trajectory to real footage
   - Get community feedback if available

5. **Commit or Revert**
   - If improvement: Commit with explanation
   - If worse: Revert and try different value

**Tuning Log Example:**
```csharp
// 2026-02-05: Increased LIFT_COEFFICIENT_SCALE from 0.35 to 0.40
//             Reason: Free kicks not curving enough in playtesting
//             Result: Curve improved from 1.8m to 2.2m (target: 2.0-2.5m)
//             Approved by: Lead Developer
public const float LIFT_COEFFICIENT_SCALE = 0.40f;
```

### 4.4 Integration Points

#### 4.4.1 Match Simulator Interface

**Match simulator calls ball physics at 60Hz:**

```csharp
public class MatchSimulator
{
    private BallState _ball;
    private BallEventLogger _ballLogger;
    private SurfaceType _pitchSurface;
    private Vector3 _windVelocity;
    private float _matchTime;
    
    public void Update(float deltaTime)
    {
        // Update match time (seconds)
        _matchTime += deltaTime;
        
        // Update ball physics
        BallPhysicsCore.UpdateBallPhysics(
            ref _ball,
            deltaTime,
            _pitchSurface,
            _windVelocity,
            _ballLogger,
            _matchTime
        );
        
        // Handle state changes
        if (_ball.State == BallStateType.OUT_OF_PLAY)
        {
            HandleOutOfPlay();
        }
    }
    
    private void HandleOutOfPlay()
    {
        var (isOut, restart) = BallCollision.CheckBoundaries(_ball, _lastTouchTeam);
        
        if (isOut)
        {
            // Publish event to EventBus (Spec #17)
            // Event consumers handle restart logic
        }
    }
}
```

#### 4.4.2 Event System Interface

**Ball physics publishes events to `EventBus` (Spec #17):**

**Event Types:**
- `GoalScoredEvent` - Ball enters goal
- `BallOutOfPlayEvent` - Ball crosses boundary
- `BounceEvent` - Ball bounces on ground
- `GoalPostHitEvent` - Ball contacts post/crossbar

**Publishing Contract:**
- Events include: timestamp, position, velocity, relevant IDs
- All events are fire-and-forget (no return values)
- Logger is independent of event system

**Event consumers:**
- Match referee (for restarts)
- Statistics tracker (for shot/pass recording)
- UI system (for event notifications)
- Replay system (for visualization)

#### 4.4.3 Agent System Interface

**Agents query ball state for possession checks:**

```csharp
public class Agent
{
    public void Update(BallState ball)
    {
        // Check if can take possession
        bool canControl = BallCollision.CheckPossession(
            ball,
            this.Position,
            this.Velocity
        );
        
        // Possession is tracked externally by agent system (Option B — see §3.1.11)
        // ball.State == CONTROLLED indicates another agent already has possession
        if (canControl && ball.State != BallStateType.CONTROLLED)
        {
            AttemptPossession();
        }
    }
}
```

**Force application:**
- Ball physics does NOT handle kicks/headers directly
- Pass Mechanics (Spec #5) calculates kick velocity
- Shot Mechanics (Spec #6) calculates shot velocity
- Ball physics receives new velocity via `ApplyKick()` (§3.1.11.2), continues from there

### 4.5 Memory Layout

#### 4.5.1 BallState Struct (Cache-Friendly)

```csharp
// Total: 64 bytes (fits in single cache line)
public struct BallState
{
    public Vector3 Position;        // 12 bytes (3 Ã— float)
    public Vector3 Velocity;        // 12 bytes
    public Vector3 AngularVelocity; // 12 bytes
    public BallStateType State;     // 4 bytes (enum backed by int)
    public Vector3 LastValidPosition; // 12 bytes
    public Vector3 LastValidVelocity; // 12 bytes
    // Total: 64 bytes (4Ã—Vector3 + 2Ã—Vector3 + 1Ã—enum = 64 bytes, fits single 64-byte cache line)
}
```

**Design rationale:**
- Struct (value type) avoids heap allocation
- All hot data (accessed every frame) in single struct
- Fits in L1 cache line (64 bytes typical)
- No pointers or references (no GC pressure)

#### 4.5.2 Constants Class (Static, Readonly)

```csharp
// Located in .data segment (read-only memory)
// Loaded once at startup
// No runtime allocation
public static class BallPhysicsConstants
{
    // All constants marked 'const' (compile-time values)
    // Or 'static readonly' for complex types
}
```

### 4.6 Performance Profiling

#### 4.6.1 Profiling Hooks

**Conditional compilation for editor builds:**

```csharp
public Vector3 CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Magnus");
    #endif
    
    // ... calculations ...
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
    
    return force;
}
```

**Profiling labels:**
- `BallPhysics.Update` - Main update loop
- `BallPhysics.Magnus` - Magnus force calculation
- `BallPhysics.Drag` - Drag force calculation
- `BallPhysics.Bounce` - Bounce application
- `BallPhysics.SpinDecay` - Spin dynamics

#### 4.6.2 Performance Target

**Per-frame budget (60Hz):**
- `UpdateBallPhysics()`: <0.5ms (target: 0.3ms average)
- Total frame contribution: <3% of 16.7ms frame budget

**Measurement protocol:**
1. Run 100 simulated matches
2. Record profiler data every frame
3. Calculate mean, p50, p95, p99 times
4. Flag any frame >0.5ms for investigation

### 4.7 Migration Path to Fixed64

**Stage 0: Use standard floats**
- Faster development iteration
- Adequate for single-player prototype
- Cross-platform float behavior acceptable for now

**Stage 5+ Migration:**

1. **Wrapper Layer**
   - Create `BallPhysicsMath.cs` abstraction
   - Define interface: `Add()`, `Multiply()`, `Sqrt()`, etc.
   - Implementation: Float backend initially

2. **Gradual Replacement**
   - Replace `Vector3` with `Vector3Math` (custom type)
   - Replace `Mathf` with `BallPhysicsMath`
   - Test determinism after each replacement

3. **Fixed64 Backend**
   - Implement `Fixed64Math` backend (Spec #8)
   - Switch backend via compile flag
   - Retest all integration tests
   - Verify replay determinism

4. **Validation**
   - Compare float vs. Fixed64 trajectories (should be nearly identical)
   - Run 1000 match pairs, ensure <1cm divergence
   - Benchmark performance difference (<10% acceptable)

**Risk mitigation:**
- Keep float backend as fallback
- Incremental migration reduces risk
- Extensive testing at each step

---

**End of Section 4**

**Page Count:** ~6 pages (revised from 8)  
**Status:** Ready for review  
**Next Section:** Section 5 (Testing)
