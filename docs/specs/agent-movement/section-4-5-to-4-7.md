### 4.5 Memory Layout

#### 4.5.1 Agent Instance Memory

The Agent class instance contains all per-agent state. Memory estimate derived from Section 3.5.1 field list:

```
Agent class instance memory breakdown:
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
SECTION                      FIELDS              BYTES
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Object header (CLR)          sync + vtable          16
Identity & Config
  AgentID                    int                     4
  TeamID                     int                     4
  IsGoalkeeper               bool                    1
  (padding)                                          3
  Attributes                 PlayerAttributes       60
    (6 physical + 9 technical = 15 ints Ã— 4B = 60B)
    (Section 3.5 header notes ~80B as forward estimate
     including alignment padding and future attribute
     fields â€” 60B is the current Stage 0 reality)

Kinematic State
  Position                   Vector3                12
  Velocity                   Vector3                12
  FacingDirection            Vector2                 8
  _cachedSpeed               float                   4
  _speedDirty                bool                    1
  (padding)                                          3

State Machine
  CurrentState               enum (int)              4
  PreviousState              enum (int)              4
  TimeInState                float                   4
  CommandedState             enum (int)              4
  GroundedReason             enum (int)              4
  CollisionForce             float                   4

Turning & Momentum
  LeanAngle                  float                   4
  CurrentTurnRate            float                   4
  IsInStumbleRiskZone        bool                    1
  (padding)                                          3

Fatigue & Stamina
  AerobicStaminaPool         float                   4
  SprintStaminaReservoir     float                   4
  FatigueLevel               float                   4
  StaminaRegenRate           float                   4

PerformanceContext           struct (3 floats)       12

Safety & Recovery
  LastValidPosition          Vector3                12
  LastValidVelocity          Vector3                12
  StateChangesThisSecond     int                     4
  StateLockExpiry            float                   4

AgentEventLogger reference   pointer (64-bit)        8
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
ESTIMATED TOTAL PER AGENT                    ~240 bytes
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
```

**22 agents total:** ~5.3 KB for all agent instances. This is trivial â€” fits entirely in L2 cache on any modern CPU.

**Cache efficiency note:** Unlike Ball Physics' 56-byte `BallState` struct (fits in a single L1 cache line), the Agent class is a heap-allocated reference type at ~240 bytes spanning 4 cache lines. This is acceptable for Stage 0 where clarity and extensibility are prioritized over cache performance. Stage 1 may refactor to a struct-of-arrays (SoA) or ECS pattern if profiling shows cache miss overhead. The public API is designed to support either layout without breaking external contracts (see Section 3.5.1 design note).

#### 4.5.2 Per-Agent Telemetry Buffer

Each agent maintains a ring buffer of 1000 `MovementTelemetryEvent` entries (Section 3.5.7). The actual struct size is derived from the full field list in Section 3.5.7:

```
MovementTelemetryEvent: ~96 bytes per event
  Timestamp          float                4
  AgentID            int                  4
  EventType          enum (int)           4
  Position           Vector3             12
  Velocity           Vector3             12
  State              enum (int)           4
  Command            MovementCommand    ~48
    TargetPosition     Vector3   12
    DesiredState       enum       4
    DecelerationMode   enum       4
    FacingMode         enum       4
    FacingTarget       Vector3   12
    OverrideSafety     bool+pad   4
    DebugLabel         string ref 8
  ContextValue       float                4
  Message            enum (int)           4
  (Total)                               ~96

Per agent:  1000 events Ã— 96 bytes = 96,000 bytes â‰ˆ 93.75 KB
All agents: 22 Ã— 93.75 KB â‰ˆ 2,062 KB â‰ˆ 2.01 MB
```

**âš ï¸ Discrepancy note:** Section 3.5.7's inline comment estimates "~24 bytes/event â‰ˆ 528 KB total." That estimate only counted 4 of the 9 fields. The actual footprint is ~2 MB. This is still acceptable for a development debugging tool (0.025% of 8 GB) but Section 3.5.7 should be corrected in its next revision to reflect the true ~96 byte size.

**Design concern â€” Command field:** Embedding a full `MovementCommand` (including a managed `string` reference for `DebugLabel`) in every telemetry event means each event holds a GC-tracked reference. In Stage 0 this is acceptable because telemetry is a debugging tool, not a shipping feature. Stage 1 should consider replacing the embedded Command with a compact `CommandSnapshot` struct that stores only the enum fields (DesiredState, DecelerationMode, FacingMode) plus a `MessageCode` instead of the full string, reducing per-event size to ~56 bytes and eliminating GC references entirely.

At 60Hz, the buffer holds approximately 16.7 seconds of continuous history per agent. This is sufficient for debugging and replay reconstruction in Stage 0. Stage 1 optimizations (selective logging, delta encoding, Command field compaction) are noted in Section 3.5.7 but not implemented in Stage 0.

**Editor vs. release telemetry behavior:** In editor/development builds, `TryLogSnapshot()` logs every frame (full 60Hz capture). In release builds, logging is restricted to state transitions and safety events only â€” typically 5â€“20 events per agent per minute rather than 3,600. This reduces effective telemetry memory usage in release builds to under 50 KB total across all agents, since buffers rarely fill beyond a few hundred entries.

#### 4.5.3 Static Memory

| Component | Size | Notes |
|-----------|------|-------|
| AgentMovementConstants | ~400 bytes | ~100 const/static readonly floats and ints |
| PitchConfiguration | ~48 bytes | 6 boundary floats Ã— 2 (min/max) Ã— 4 bytes |
| MessageCode enum | Negligible | Integer-backed enum, no runtime allocation |

**Total static memory:** Under 500 bytes. Loaded once at startup, never reallocated.

#### 4.5.4 Total Memory Footprint

| Component | Per-Agent | All 22 Agents |
|-----------|-----------|---------------|
| Agent instances | ~240 B | ~5.3 KB |
| Telemetry buffers | ~93.75 KB | ~2,062 KB (~2.01 MB) |
| Static constants | â€” | ~0.5 KB |
| **Total** | â€” | **~2.07 MB** |

For context, Ball Physics' total memory footprint is under 1 KB (one BallState struct + constants). Agent Movement is significantly larger due to 22 entities and per-agent telemetry buffers. However, 2.07 MB is still negligible on any target platform â€” less than 0.03% of a typical 8 GB system. The telemetry buffers (~2 MB) dominate the footprint and are a debugging tool that can be reduced or disabled in release builds.

### 4.6 Performance Profiling

#### 4.6.1 Profiling Hooks

Conditional compilation profiling hooks are inserted around each pipeline step, following the Ball Physics Spec #1 Section 4.6.1 pattern:

```csharp
public static Vector3 CalculateAcceleration(
    AgentMovementState state, float speed, float topSpeed,
    float k_accel, Vector3 targetDir, float fatigueMod)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("AgentMovement.Acceleration");
    #endif

    // ... acceleration calculation ...

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif

    return acceleration;
}
```

**Profiling labels (one per pipeline step):**
- `AgentMovement.StateEval` â€” State machine evaluation
- `AgentMovement.Acceleration` â€” Locomotion acceleration calculation
- `AgentMovement.DirectionalPenalty` â€” Zone multiplier application
- `AgentMovement.TurnRate` â€” Turn rate + lean + stumble calculation
- `AgentMovement.FacingUpdate` â€” Facing direction rotation
- `AgentMovement.Integration` â€” Velocity and position integration
- `AgentMovement.Safety` â€” Safety validation and clamping
- `AgentMovement.CacheUpdate` â€” Speed cache and property refresh
- `AgentMovement.Telemetry` â€” Event logging

**Aggregate label:**
- `AgentMovement.AllAgents` â€” Wraps the entire 22-agent loop. This is the primary measurement point for the Section 5 performance budget.

#### 4.6.2 Performance Target

**Per-frame budget (60Hz):**
- All 22 agents combined: **< 3.0ms** (hard ceiling from Ball Physics Spec #1 Section 6.2.1 budget table)
- Target average: **< 2.0ms** for all 22 agents
- Target per-agent average: **< 0.091ms** (2.0ms Ã· 22)
- Per-agent worst case: **< 0.14ms** (3.0ms Ã· 22)

These targets represent Agent Movement's allocation from the shared 16.7ms frame budget. Ball Physics consumes < 0.5ms. Collision System (Spec #3) is budgeted at < 2.0ms. The remaining ~11.2ms is available for AI, rendering, audio, and other systems.

**Measurement protocol:**

1. Run 10 simulated matches (minimum 900 simulated minutes of play)
2. Record Unity Profiler data every frame using `AgentMovement.AllAgents` label
3. Calculate: mean, p50, p95, p99, max
4. Flag any frame where `AgentMovement.AllAgents` exceeds 3.0ms for investigation
5. Report per-agent breakdown using individual pipeline step labels

Section 5 (Performance Analysis) derives the expected operation counts and validates that these targets are achievable.

### 4.7 Migration Path to Fixed64

Identical strategy to Ball Physics Spec #1 Section 4.7, adapted for Agent Movement's larger API surface.

**Stage 0: Use standard floats**
- All calculations use `float` (32-bit IEEE 754)
- `Vector3` and `Vector2` from Unity (float-backed)
- Acceptable for single-player prototype
- Cross-platform float behavior variations are tolerable for non-competitive play

**Stage 5+ Migration:**

1. **Wrapper Layer** â€” Create `AgentMovementMath.cs` abstraction defining `Add()`, `Multiply()`, `Sqrt()`, `Exp()`, `Pow()`, `Atan2()` interfaces. Initial implementation delegates to standard `Mathf`.

2. **Gradual Replacement** â€” Replace `Vector3`/`Vector2` with custom `AgentVector3`/`AgentVector2` types that use the math wrapper. Replace direct `Mathf` calls with `AgentMovementMath` calls. Test determinism after each file conversion. Agent Movement has more math operations than Ball Physics (4 subsystems vs. 1), so this migration is estimated at 2â€“3Ã— the effort.

3. **Fixed64 Backend** â€” Implement `Fixed64Math` backend per Spec #9. Switch backend via compile flag. Retest all 68 unit tests, 10 integration tests, and 8 validation benchmarks. Verify replay determinism across platforms.

4. **Validation** â€” Compare float vs. Fixed64 agent trajectories over 100 simulated matches. Accept if positional divergence is < 1cm per agent per match. Benchmark performance difference â€” accept if < 15% overhead (slightly higher tolerance than Ball Physics' 10% due to larger computation volume).

**Risk mitigation:**
- Float backend retained as compile-time fallback
- Migration is incremental (one file at a time)
- Each converted file is tested independently before proceeding
- 68 existing unit tests serve as regression suite throughout migration

---

**End of Section 4**

**Page Count:** ~10 pages (expanded from ~8 by integration point section and corrected telemetry analysis)  
**Status:** Draft v1.1, revised â€” all 10 critique issues resolved  
**Next Section:** Section 5 (Performance Analysis)
