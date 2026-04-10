# Ball Physics Specification - Section 7: Future Extensions

**Created:** February 6, 2026, 10:00 AM PST  
**Version:** 1.0  
**Status:** Draft  
**Purpose:** Authoritative roadmap for all planned ball physics extensions â€” what changes at each stage, what architectural hooks exist today, what risks each extension introduces, and what is permanently excluded  
**Dependencies:** Section 1.3 (Implementation Timeline), Section 3.1 (Core Formulas v2.6), Section 4 (Implementation v1.2), Section 6 (Performance Analysis v1.0), Master Vol 1 (Physics Core), Master Development Plan v1.0

---

## 7. FUTURE EXTENSIONS

### Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded ball physics extensions. Future extension references in Sections 1.2, 1.3, 3.1, 4.2, 4.7, and 6.4.5 are summaries derived from the analysis herein. If a conflict exists between those sections and this one, **this section takes precedence**.

**Design philosophy:** Stage 0 ball physics was designed with future extensions in mind. Specific architectural decisions â€” the `windVelocity` parameter, the `SurfaceType` enum, the `SurfaceProperties` static class, the `BallPhysicsConstants` organization, and the Fixed64 migration path â€” were made to minimize rework when extensions are implemented. This section documents how each extension connects to existing hooks and identifies gaps where new architecture is required.

---

### 7.1 Stage 1 Extensions (Year 2)

Stage 1 adds visualization and the wind system. Ball physics changes are minimal â€” the architecture already supports these features through existing parameters and interfaces.

#### 7.1.1 Wind Integration

**What it does:** Applies a non-zero wind velocity vector to all aerodynamic calculations, causing the ball to drift with or against the wind during flight and rolling.

**Existing hook:** `UpdateBallPhysics()` already accepts a `Vector3 windVelocity` parameter (Section 3.1.9). The `relativeVelocity = ball.Velocity - windVelocity` calculation is already implemented in both AIRBORNE and ROLLING states. In Stage 0, this parameter is passed as `Vector3.zero`.

**What changes:**
- `MatchSimulator` obtains wind velocity from the new `WeatherSystem` (external dependency, Section 4.2.2) instead of hardcoding `Vector3.zero`
- No changes to any ball physics code

**Integration contract:**
- `WeatherSystem` must provide a `Vector3` wind velocity in m/s, world-space coordinates
- Wind velocity may change during a match (e.g., gusts), but changes between frames must be small enough to avoid discontinuities â€” recommended maximum Î”v of 0.5 m/s per second
- Ball physics treats wind as a constant within each frame (no sub-frame interpolation)

**New functionality required in WeatherSystem (not this spec):**
- Wind direction and magnitude generation from match seed
- Gust modeling (optional, Stage 2)
- Wind shadow effects from stadium geometry (permanent exclusion â€” see 7.6)

**Performance impact:** Zero. The `relativeVelocity` subtraction already executes every frame regardless of wind value. No new operations are added.

**Risk:** Low. The hook exists, the math is proven, and the parameter is already flowing through the call chain.

#### 7.1.2 Visual Rendering Support

**What it does:** Provides data to the 2D rendering system for ball visualization â€” shadows, height indicators, and motion trails.

**Existing hooks:**
- `BallState.Position.z` provides ball height for shadow offset and scale calculations
- `BallPhysicsConstants.Rendering.SHADOW_OFFSET_FACTOR` (0.3) and `HEIGHT_SCALE_FACTOR` (0.02) are already defined (Section 3.1.2)
- Helper functions `ToRenderPosition()`, `GetShadowOffset()`, and `GetBallScale()` are already specified in Section 3.1.1

**What changes:**
- The rendering system (2D Rendering Spec, Stage 1) reads `BallState` and calls existing helper functions
- No changes to ball physics code

**New functionality required in Rendering System (not this spec):**
- Ball sprite rendering at `ToRenderPosition()` coordinates
- Shadow sprite offset by `GetShadowOffset()` with alpha fade by height
- Ball scale adjustment by `GetBallScale()` for perspective hint
- Trail effect using recent `BallState.Position` history (ring buffer in renderer, not in physics)
- Velocity-based trail length and opacity

**Performance impact:** Zero on ball physics. Rendering reads BallState (already a 56-byte struct in cache), no additional physics computation.

**Risk:** Low. All data is already computed and accessible. The rendering system is a pure consumer with no feedback into physics.

#### 7.1.3 Slow-Motion Replay Support

**What it does:** Allows match replay at reduced speed (0.1xâ€“1.0x) with smooth ball trajectory visualization.

**Existing hooks:**
- `BallEventLogger` records position snapshots at `SNAPSHOT_INTERVAL` (1.0s) intervals (Section 3.1.13)
- `BallEventLogger` records discrete events (bounces, goal post hits, kicks) with timestamps
- All physics is deterministic for a given input sequence

**What changes:**
- Replay system reconstructs ball trajectory by re-running physics at the original `dt` but rendering interpolated frames between physics steps
- `BallEventLogger.SNAPSHOT_INTERVAL` may need to decrease from 1.0s to 0.1s for smoother replay reconstruction â€” this is a constant change only, no code change

**Architecture decision: Re-simulation vs. Keyframe Interpolation**

| Approach | Pros | Cons | Recommendation |
|---|---|---|---|
| **Re-simulation** | Bit-perfect accuracy; minimal storage | Requires full input replay; CPU cost during replay | Preferred for Stage 1 |
| **Keyframe interpolation** | Fast playback; low CPU | Requires dense keyframes; interpolation artifacts on curves | Defer to Stage 2 if re-simulation proves too slow |

**New functionality required in Replay System (not this spec):**
- Input recording (kick velocities, surface changes, wind changes per frame)
- Deterministic replay engine that feeds recorded inputs to `UpdateBallPhysics()`
- Frame interpolation for sub-physics-step rendering
- Replay speed control (0.1x, 0.25x, 0.5x, 1.0x)

**Performance impact:** During replay, ball physics runs at the same cost as live simulation. At 0.1x speed, the physics tick rate stays at 60Hz but the renderer draws more visual frames between ticks â€” this is rendering cost, not physics cost.

**Risk:** Medium. The main risk is not in ball physics but in the replay system's ability to perfectly reproduce inputs. If any input is missed or reordered, the replay will diverge. Ball physics itself is deterministic given identical inputs (validated by Section 5 test cases).

**Open question:** Should `BallEventLogger` be extended with a higher-resolution recording mode for replay? Current 1-second snapshots are insufficient for frame-accurate replay reconstruction. This decision affects the logger's ring buffer size (Section 6.3.2). **Recommendation:** Defer logger changes until replay system requirements are finalized in Stage 1 planning.

---

### 7.2 Stage 2 Extensions (Year 3â€“4)

Stage 2 adds environmental complexity. These extensions modify how ball physics receives input but do not change the core force calculations.

#### 7.2.1 Per-Position Surface Queries

**What it does:** Replaces the single global `SurfaceType` with position-dependent surface lookups, modeling worn goalmouths, wet patches, and mixed-surface pitches.

**Existing hooks:**
- `SurfaceProperties` is a static class with switch-statement lookups (Section 3.1.2)
- `SurfaceType` enum already has 5 types: `GRASS_DRY`, `GRASS_WET`, `GRASS_LONG`, `ARTIFICIAL`, `FROZEN`
- All surface-dependent functions (`GetCoefficientOfRestitution`, `GetFrictionCoefficient`, `GetRollingResistance`, `GetSpinRetention`) accept `SurfaceType` as a parameter
- The `UpdateBallPhysics()` function accepts `SurfaceType surface` as a parameter

**What changes:**

1. **New dependency:** `PitchConditionSystem` (Section 4.2.2) provides per-position surface queries
2. **Call site change in MatchSimulator:** Instead of passing a single `_pitchSurface`, the simulator queries `PitchConditionSystem.GetSurfaceAt(ball.Position)` before each physics update
3. **No changes to ball physics code** â€” `UpdateBallPhysics()` already accepts surface as a parameter per frame

**Interface contract for PitchConditionSystem (not this spec):**

```
SurfaceType GetSurfaceAt(Vector3 worldPosition)
```

- Must return in <0.01ms (tile-based lookup, not per-pixel)
- Recommended: grid of 1mÃ—1m tiles, each assigned a `SurfaceType`
- Tile state may change during the match (degradation from footfall)

**Potential new SurfaceType values (Stage 2):**

| New Type | CoR (est.) | Friction (est.) | Rolling (est.) | Spin Retention (est.) |
|---|---|---|---|---|
| `GRASS_WORN` | 0.60 | 0.55 | 0.06 | 0.75 |
| `MUD` | 0.50 | 0.75 | 0.15 | 0.60 |
| `GRASS_WATERLOGGED` | 0.45 | 0.30 | 0.02 | 0.90 |

These values require empirical tuning during Stage 2 development. Adding new enum values to `SurfaceType` and new switch cases to `SurfaceProperties` is the only ball physics code change required.

**Performance impact:** The surface lookup adds one function call per frame (<0.01ms). The `SurfaceProperties` switch statement grows from 5 to ~8 cases â€” negligible. Total impact: <0.01ms per frame.

**Risk:** Medium. The risk is not in ball physics but in the `PitchConditionSystem` design. Key questions for that spec:
- How does surface degradation propagate (gradual or threshold)?
- How does rain change surface type mid-match (transition rules)?
- How do mixed-surface boundaries behave when the ball rolls across a tile edge?

**The tile-boundary question** is the most significant for ball physics. If the ball crosses from `GRASS_DRY` to `MUD` mid-frame, which surface applies? **Recommendation:** Use the surface at `ball.Position` at the start of each frame. Do not interpolate between surfaces â€” the 1m tile size is much larger than per-frame ball displacement (~0.5m at 30 m/s), so mid-frame transitions are rare and discontinuities are imperceptible.

#### 7.2.2 Weather Effects on Ball Physics

**What it does:** Rain, temperature, and humidity modify ball physics parameters dynamically during a match.

**Existing hooks:**
- Wind is already integrated (7.1.1)
- Surface type can change per frame (the parameter is passed each call)
- All physics constants that weather could affect are isolated in `BallPhysicsConstants`

**Weather effect mapping:**

| Weather Condition | Ball Physics Parameter Affected | Mechanism | Magnitude (est.) |
|---|---|---|---|
| Light rain | Surface friction | `SurfaceType` changes from `GRASS_DRY` to `GRASS_WET` | Friction âˆ’33% |
| Heavy rain | Surface friction + bounce | `SurfaceType` changes to `GRASS_WATERLOGGED` | Friction âˆ’50%, CoR âˆ’30% |
| Cold temperature (<5Â°C) | Air density | Ï increases from 1.225 to ~1.29 kg/mÂ³ | Drag +5% |
| High altitude (>1500m) | Air density | Ï decreases (e.g., 1.06 at Mexico City) | Drag âˆ’13% |
| High humidity | Air density | Ï decreases slightly | Drag âˆ’1% (negligible) |
| Snow/frost | Surface properties | `SurfaceType` changes to `FROZEN` | Already implemented |

**Architecture decision: How to modify air density**

Air density (`Ï`) is currently a compile-time constant in `BallPhysicsConstants.Air.DENSITY` (1.225 kg/mÂ³). To support altitude and temperature effects, this must become a runtime parameter.

**Option A: Add air density parameter to UpdateBallPhysics()**
- Pros: Explicit, no hidden state
- Cons: Adds a parameter to an already 6-parameter function

**Option B: Create an EnvironmentState struct passed alongside SurfaceType**
- Pros: Clean, extensible for future environmental parameters
- Cons: New struct, new allocation concern

**Option C: Make air density a field on a configuration object set per-match**
- Pros: Set once at match start, no per-frame overhead
- Cons: Less flexible if air density changes mid-match (altitude doesn't change, but temperature can)

**Recommendation:** Option B. Define a lightweight `EnvironmentState` struct:

```
struct EnvironmentState
{
    float AirDensity;       // kg/mÂ³ (default 1.225)
    SurfaceType Surface;    // replaces standalone surface parameter
    Vector3 WindVelocity;   // replaces standalone wind parameter
}
```

This consolidates the existing `surface` and `windVelocity` parameters into a single struct, reducing the parameter count of `UpdateBallPhysics()` from 6 to 4: `(ref BallState ball, float dt, EnvironmentState env, BallEventLogger logger, float matchTime)`. This is a **breaking API change** â€” all call sites must update. Schedule this refactor at the start of Stage 2.

**Performance impact:** Negligible. Air density is used in drag and Magnus calculations that already execute every frame. Replacing a constant with a struct field access is a single memory load â€” effectively free.

**Risk:** Low for ball physics. The `EnvironmentState` refactor is a clean API change with clear migration path. The risk is in the `WeatherSystem` providing correct values â€” incorrect air density will produce subtly wrong trajectories that are hard to detect without automated validation.

**Validation requirement:** After Stage 2 implementation, re-run all Section 5 integration tests with varied `EnvironmentState` values. Add new test cases: "long ball at Mexico City altitude travels 8â€“12% farther than at sea level."

#### 7.2.3 Altitude and Temperature Effects

**What it does:** Adjusts air density based on the match venue's altitude and current temperature, affecting drag and Magnus force magnitude.

**Physics basis:**

Air density varies with altitude and temperature per the ideal gas law:

```
Ï = P / (R_specific Ã— T)

Where:
  P = atmospheric pressure (Pa), decreases with altitude
  R_specific = 287.05 J/(kgÂ·K) for dry air
  T = temperature (Kelvin)
```

**Simplified model (sufficient for gameplay):**

```
Ï(altitude, temp_C) = 1.225 Ã— (1 - 0.0000226 Ã— altitude)^5.256 Ã— (288.15 / (273.15 + temp_C))
```

**Reference values:**

| Venue | Altitude (m) | Typical Temp (Â°C) | Air Density (kg/mÂ³) | Drag Relative to Sea Level |
|---|---|---|---|---|
| London (sea level) | 11 | 12 | 1.225 | 100% (baseline) |
| Madrid | 650 | 20 | 1.145 | 93% |
| Mexico City | 2,240 | 16 | 0.977 | 80% |
| La Paz, Bolivia | 3,640 | 10 | 0.844 | 69% |
| Doha (hot) | 10 | 38 | 1.137 | 93% |

**Gameplay impact:** At high-altitude venues (Mexico City, La Paz), the ball experiences significantly less drag. Long balls travel farther, free kicks curve less (reduced Magnus force), and knuckleball effects are more pronounced. This creates a meaningful home advantage for high-altitude clubs â€” a real phenomenon in football.

**Implementation:** Calculate `Ï` once at match initialization from the venue's altitude and pre-match temperature. Store in `EnvironmentState.AirDensity`. Temperature changes during the match are negligible (Î”Ï < 0.5% over 90 minutes) and should be ignored.

**Risk:** Low. This is a single constant substitution with well-understood physics. The simplified formula above is accurate to within 1% for altitudes under 4,000m.

---

### 7.3 Stage 3+ Extensions (Year 5+)

These extensions are lower priority and depend on systems that do not yet have specifications.

#### 7.3.1 Per-Position Surface Degradation Integration

**What it does:** Links the `PitchConditionSystem` to the footfall density tracking described in Master Vol 1 Section 4.1, so that heavily-used areas (goalmouths, center circle) degrade during the match.

**Existing hook:** Per-position surface queries (7.2.1) provide the data path. Ball physics receives degraded surface types without any code change.

**What's new:** The `PitchConditionSystem` must implement degradation logic:
- Track footfall density per tile
- Transition tiles from `GRASS_DRY` â†’ `GRASS_WORN` â†’ `MUD` based on thresholds
- Apply groundskeeper quality as a recovery modifier between matches

**Ball physics changes:** None. The existing `SurfaceType` parameter already flows through. New surface types may be added to the enum (see 7.2.1).

**Performance impact:** Zero on ball physics.

**Risk:** Low for ball physics. The risk is entirely in the degradation model's calibration â€” too aggressive and all pitches become mud by halftime, too conservative and degradation is invisible.

#### 7.3.2 Multi-Ball Simulation

**What it does:** Enables simultaneous simulation of multiple balls for training mini-games, what-if analysis, or set-piece rehearsal.

**Existing hook:** `UpdateBallPhysics()` operates on a single `ref BallState`. Multi-ball is a loop over an array of `BallState` structs â€” no architectural change needed.

**Implementation:**

```
BallState[] balls = new BallState[n];
for (int i = 0; i < balls.Length; i++)
{
    BallPhysicsCore.UpdateBallPhysics(ref balls[i], dt, env, logger, matchTime);
}
```

**Performance (from Section 6.4.5):**
- At n=10: ~1,720 ops per frame, <0.01ms â€” trivially fast
- Ball-to-ball collision is NOT planned (training balls don't interact)
- Each ball has independent state, no cross-dependencies

**Risk:** Low. The only concern is `BallEventLogger` â€” the current design assumes a single ball. Multi-ball would either need per-ball loggers or a shared logger with ball ID tagging. **Recommendation:** Add an optional `ballId` field to `BallEvent` when multi-ball is implemented.

#### 7.3.3 Accelerated Simulation

**What it does:** Runs match simulation at speeds faster than real-time for season simulation, background processing, and statistical analysis.

**Existing hook:** Ball physics is O(1) per frame with no rendering dependencies. Accelerated simulation simply calls `UpdateBallPhysics()` multiple times per rendered frame (or per wall-clock frame in headless mode).

**Performance (from Section 6.4.5):**

| Speed | Physics Updates/Frame | Ball Physics Cost/Frame | Feasible? |
|---|---|---|---|
| 10x | 10 | ~0.5ms | Yes (within budget) |
| 100x | 100 | ~5ms | Yes (headless only, rendering disabled) |
| 1000x | 1000 | ~50ms | Yes (background thread, no frame budget) |

**Optimization for headless mode:** Skip `BallEventLogger.TryLogSnapshot()` and `ValidatePhysicsState()` in headless mode. This reduces per-frame ops by ~30% (Section 6.4.5). Implement via a `bool headlessMode` parameter or compile flag.

**Risk:** Low for ball physics. The risk is in the match simulator's ability to run all other systems (AI, collision, agent movement) at the same accelerated rate. Ball physics will never be the bottleneck.

---

### Stage 4 (Year 6): No Ball Physics Extensions

Stage 4 (Social Systems & Psychology) introduces player personality, morale, media interaction, and dressing room dynamics. None of these systems interact with ball physics. The ball does not care about a player's mood. No changes to any ball physics code, parameters, or architecture are planned or anticipated for Stage 4.

---

### 7.4 Stage 5+ Extensions (Year 7+)

#### 7.4.1 Fixed64 Migration for Multiplayer Determinism

**What it does:** Replaces all `float` arithmetic in ball physics with fixed-point (`Fixed64`) arithmetic to guarantee bit-identical results across platforms â€” required for multiplayer synchronization.

**Existing hook:** Section 4.7 defines a 4-step migration path:
1. Create `BallPhysicsMath.cs` abstraction layer
2. Replace `Vector3` with `Vector3Math` (custom type)
3. Replace `Mathf` with `BallPhysicsMath`
4. Implement `Fixed64Math` backend (Spec #8)

**Performance impact (from Section 6.4.5):**

| Metric | Float (Current) | Fixed64 (Estimated) |
|---|---|---|
| AIRBORNE ops/frame | ~172 | ~500â€“700 |
| Time per frame | ~0.05ms | ~0.15â€“0.25ms |
| Budget utilization | ~10% | ~30â€“50% |

Still within the 0.5ms ceiling. Fixed64 migration is feasible without architectural changes.

**What changes in ball physics:**
- Every `float` becomes `Fixed64`
- Every `Vector3` becomes `Vector3Fixed` (custom struct)
- `Mathf.Sqrt`, `Mathf.Abs`, `Mathf.Clamp` become `Fixed64Math` equivalents
- `BallPhysicsConstants` values become `Fixed64` constants
- All test tolerances in Section 5 must be re-evaluated for fixed-point precision

**What does NOT change:**
- Force calculation formulas (identical math, different numeric type)
- State machine logic (integer comparisons unaffected)
- Event logging (timestamps and positions stored as Fixed64)
- Code organization and class structure

**Validation requirements:**
- Run 1,000 match pairs (float vs. Fixed64), verify <1cm trajectory divergence (Section 4.7)
- Re-run all Section 5 unit tests with Fixed64 backend
- Test cross-platform determinism: Windows, Mac, Linux must produce bit-identical results for the same seed
- Benchmark Fixed64 performance on minimum-spec hardware

**Risk:** Medium. Fixed64 migration is a large mechanical refactor touching every calculation in the system. The math doesn't change but bugs can be introduced through:
- Overflow in intermediate calculations (Fixed64 has smaller range than float)
- Loss of precision in chains of multiply/divide operations
- `sqrt()` implementation differences between Fixed64 libraries

**Mitigation:** Incremental migration with the float backend kept as fallback (Section 4.7). Test after each function conversion, not just at the end.

---

### 7.5 Permanently Excluded Features

The following features will **never** be implemented in the ball physics system, regardless of stage. Each exclusion has been evaluated and rejected on cost/benefit grounds.

| Feature | Reason for Exclusion | Alternatives |
|---|---|---|
| **Ball deformation physics** | Negligible gameplay impact. Real ball deformation during contact lasts <5ms and affects trajectory by <0.1%. Visual-only deformation can be handled by the rendering system without physics simulation. | Visual deformation in renderer (squash/stretch on impact) â€” no physics backing needed |
| **Ball wear/degradation** | A match ball is replaced when damaged. Modeling scuffing, pressure loss, or panel separation adds complexity with zero tactical impact. | None needed â€” not a meaningful game mechanic |
| **Individual grass blade simulation** | O(nÂ²) or worse for realistic grass-ball interaction. Even simplified models require per-blade collision detection across millions of blades. Performance prohibitive and visually invisible in 2D top-down rendering. | Aggregate surface properties (already implemented via `SurfaceType`) capture the net effect |
| **Ball seam aerodynamics** | Panel geometry effects on drag are below measurement noise for gameplay purposes. Wind tunnel studies show <3% drag variation from seam orientation. Implementing this requires tracking ball rotation around all 3 axes at render precision, with no observable gameplay difference. | Standard drag coefficient with turbulent transition (already implemented in drag crisis model) captures the macro effect |
| **Wind shadow from stadium** | Stadium geometry creating wind shadows and eddies would require CFD-level simulation or a complex precomputed flow field. Massive complexity for a second-order effect on an already second-order phenomenon (wind). | Uniform wind field with optional gusts (Stage 2 WeatherSystem) is sufficient |
| **Ball-to-ball collision** | Only relevant in multi-ball training scenarios (7.3.2). Training balls operating in the same space is an edge case of an edge case. The physics is trivial (elastic sphere collision) but the detection adds O(nÂ²) checks. | If needed, implement in a separate training physics module, not in the core ball physics system |
| **Spin axis precession** | Real footballs undergo slow gyroscopic precession of the spin axis. The effect is <0.5Â° per second and completely invisible in gameplay. Modeling it adds complexity to the spin dynamics with zero observable benefit. | Current spin decay model (Section 3.1.7) captures the important behavior (spin magnitude decay) |

---

### 7.6 Extensibility Design Analysis

This section documents how the current Stage 0 architecture was designed to accommodate future extensions, identifying both the prepared hooks and the gaps that will require new work.

#### 7.6.1 Prepared Extension Points

| Extension Point | Location | Mechanism | Extensions Served |
|---|---|---|---|
| Wind velocity parameter | `UpdateBallPhysics()` parameter | Pass non-zero `Vector3` | 7.1.1 (Wind) |
| Surface type parameter | `UpdateBallPhysics()` parameter | Change per-frame | 7.2.1 (Per-position surface) |
| `SurfaceType` enum | `BallPhysicsConstants.cs` | Add new enum values | 7.2.1 (New surfaces), 7.3.1 (Degradation) |
| `SurfaceProperties` switch | `SurfaceProperties.cs` | Add new switch cases | 7.2.1, 7.3.1 |
| Rendering constants | `BallPhysicsConstants.Rendering` | Read by renderer | 7.1.2 (Visual rendering) |
| Rendering helpers | `ToRenderPosition()`, `GetShadowOffset()`, `GetBallScale()` | Called by renderer | 7.1.2 |
| Event logger | `BallEventLogger` with ring buffer | Extend event types | 7.1.3 (Replay), 7.3.2 (Multi-ball) |
| Profiling hooks | Conditional `Profiler.BeginSample()` | Unchanged | All extensions (performance monitoring) |
| Static class design | All physics in static methods | No instance state to migrate | 7.4.1 (Fixed64 â€” type substitution only) |

#### 7.6.2 Gaps Requiring New Architecture

| Gap | Required For | Estimated Work | When to Address |
|---|---|---|---|
| `EnvironmentState` struct | 7.2.2 (Weather), 7.2.3 (Altitude) | Small â€” consolidate existing parameters into struct, update call sites | Start of Stage 2 |
| Air density as runtime value | 7.2.3 (Altitude/temperature) | Small â€” replace `const` with struct field, update `CalculateDragForce()` and `CalculateMagnusForce()` | Stage 2 with `EnvironmentState` |
| `BallPhysicsMath` abstraction | 7.4.1 (Fixed64) | Medium â€” wrapper functions for all math operations | Start of Stage 5 |
| `Vector3Fixed` type | 7.4.1 (Fixed64) | Medium â€” custom struct replacing Unity `Vector3` | Stage 5 with Fixed64 library |
| Multi-ball logger support | 7.3.2 (Multi-ball) | Small â€” add `ballId` field to `BallEvent` struct | Stage 3 when multi-ball is needed |
| Headless mode flag | 7.3.3 (Accelerated sim) | Small â€” boolean flag or compile switch to skip logging/validation | Stage 2 when fast-forward is needed |
| Higher-res replay recording | 7.1.3 (Slow-motion replay) | Small â€” configurable `SNAPSHOT_INTERVAL` or event-driven recording | Stage 1 when replay system is designed |

#### 7.6.3 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All Section 5 unit tests continue to pass** with default `EnvironmentState` (sea level, no wind, `GRASS_DRY`). Extensions add new test cases but must not break existing ones.

2. **O(1) per-frame complexity is maintained.** No extension may introduce loops over agents, spatial queries, or variable-length iterations in the ball physics update path.

3. **Zero-allocation policy is maintained.** No extension may introduce heap allocations in the physics update loop. New structs (`EnvironmentState`, `Vector3Fixed`) must be value types.

4. **The 0.5ms frame budget ceiling is maintained.** Even with Fixed64 (the most expensive extension), per-frame cost must remain below 0.5ms on minimum-spec hardware.

5. **State machine behavior is preserved.** State transition logic (hysteresis thresholds, transition conditions) must produce identical behavior for identical inputs, regardless of which extensions are active.

---

### 7.7 Cross-References

| Topic | Section | Summary |
|---|---|---|
| Implementation timeline by stage | Section 1.3 | Stage 0/1/2 feature allocation |
| Permanent exclusions list | Section 1.2 | Subset of 7.5 (this section is authoritative) |
| Wind parameter in code | Section 3.1.9 (`UpdateBallPhysics`) | `windVelocity` parameter already present |
| Rendering helpers | Section 3.1.1 | `ToRenderPosition()`, shadow/scale functions |
| Surface properties future opportunity | Section 3.1.2 (code comment) | "FUTURE OPPORTUNITY (Stage 3+)" note |
| Future external dependencies | Section 4.2.2 | `WeatherSystem`, `PitchConditionSystem` |
| Fixed64 migration path | Section 4.7 | 4-step migration plan |
| P3 optimizations (Fixed64, multi-ball, accel sim) | Section 6.4.5 | Performance estimates for each |
| Pitch degradation model | Master Vol 1, Section 4.1 | Footfall density tracking, tile degradation |
| Weather system design | Master Vol 1, Section 4.2 | Static states, dynamic transitions |
| Altitude effects | Master Vol 1, Section 4.2 | Air density variation |
| Stage dependency chain | Master Development Plan, Section 1 | Stage 0â†’1â†’2â†’...â†’6 |
| Stage 1 deliverables (rendering) | Master Development Plan, Section 3 | Ball sprite, trail, shadow, camera |
| Stage 2 deliverables (season structure) | Master Development Plan, Section 4 | Weather, pitch conditions |

---

### 7.8 Known Risks and Open Questions

The following items require decisions during future stage planning. They are documented here so they are not forgotten.

**KR-1: EnvironmentState API break timing.**
The `EnvironmentState` refactor (7.2.2) changes the `UpdateBallPhysics()` signature. This breaks all call sites. If implemented mid-Stage-2, it disrupts in-progress work. **Recommendation:** Execute this refactor as the first task of Stage 2, before any other Stage 2 work begins. All tests must pass before proceeding.

**KR-2: Surface tile boundary discontinuities.**
When the ball rolls across a tile boundary (7.2.1), surface properties change instantaneously. At high ball speeds, this could produce a visible "jerk" in trajectory. **Recommendation:** Monitor during Stage 2 playtesting. If visible, consider a 2-tile blending zone where properties are linearly interpolated. This adds ~10 float ops per frame in ROLLING state only â€” negligible.

**KR-3: Fixed64 sqrt accuracy.**
The Magnus and drag calculations both use `sqrt()` (via `vector.magnitude`). Fixed64 `sqrt()` implementations vary in accuracy from 1 ULP to 100+ ULP depending on the library. Low-accuracy `sqrt()` could produce meaningfully different trajectories. **Recommendation:** Benchmark candidate Fixed64 libraries for `sqrt()` accuracy before selection. Require <4 ULP error for the chosen library. Add a dedicated `sqrt()` accuracy test to the Fixed64 library validation suite.

**KR-4: Replay divergence from environmental state changes.**
If the replay system (7.1.3) does not perfectly record `EnvironmentState` changes (wind gusts, surface degradation events), replays will diverge from the original match. Ball physics is deterministic for identical inputs, but the replay system must ensure inputs are identical. **Recommendation:** The replay system must record the full `EnvironmentState` at every frame where it changes, not just at fixed intervals. This is a replay system requirement, not a ball physics requirement.

**KR-5: Multi-ball event logger contention.**
If multiple balls share a single `BallEventLogger` (7.3.2), concurrent writes could corrupt the ring buffer. The current logger design assumes single-ball, single-threaded access. **Recommendation:** Either use per-ball loggers (simple, wasteful of memory) or add a `ballId` field and ensure single-threaded access via the main update loop (no parallel ball updates). Decide during Stage 3 planning.

**KR-6: SurfaceType enum serialization stability.**
The `SurfaceType` enum (Section 3.1.2) currently has 5 members with implicit integer backing values (0â€“4). Adding new members in Stage 2 (e.g., `GRASS_WORN`, `MUD`, `GRASS_WATERLOGGED`) will shift the implicit integer assignments of any members declared after the insertion point. If any system serializes `SurfaceType` as an integer â€” replay files, save games, network packets (Stage 6), or debug logs â€” old data will silently map to wrong surface types. **Recommendation:** Before Stage 2, assign explicit integer backing values to all existing enum members (`GRASS_DRY = 0, GRASS_WET = 1, ...`). All new members must use values > 4. This is a low-effort preventive fix that should be applied during the EnvironmentState refactor (KR-1).

---

**END OF SECTION 7**

---

## Document Status

**Section 7 Completion:**
- âœ… Stage 1 extensions documented with existing hooks identified (7.1)
- âœ… Stage 2 extensions documented with architecture decisions (7.2)
- âœ… Stage 3+ extensions documented with performance estimates (7.3)
- âœ… Stage 5+ Fixed64 migration risks analyzed (7.4)
- âœ… Permanent exclusions consolidated with rationale (7.5)
- âœ… Extensibility design analysis â€” hooks, gaps, and guarantees (7.6)
- âœ… Cross-references to all related sections (7.7)
- âœ… Known risks and open questions documented (7.8)

**Page Count:** ~10 pages

**Ready for:** Review and critique

**Next Sections:** 8 (References), Appendices A-C
