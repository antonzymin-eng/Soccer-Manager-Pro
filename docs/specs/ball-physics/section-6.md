# Ball Physics Specification - Section 6: Performance Analysis

**Created:** February 5, 2026, 10:30 AM PST  
**Version:** 1.1  
**Status:** Draft  
**Changes from v1.0:**
- AUD-006 residual B: Fixed §6.3.4 prose struct size: `60 bytes` → `64 bytes` (table at §6.3.1 was already correct)
**Purpose:** Authoritative performance analysis for the Ball Physics System â€” computational complexity, memory budget, profiling targets, and optimization roadmap  
**Dependencies:** Section 3.1 (Core Formulas v2.6), Section 4 (Implementation v1.2), Section 5 (Testing v1.0)

> **Note:** `ApplyKick()` (added in §3.1 v2.5) is excluded from per-frame analysis — it is event-driven, not called in the update loop.

---

## 6. PERFORMANCE ANALYSIS

### Preamble: Role of This Section

This section is the **single authoritative source** for all ball physics performance targets, budgets, and analysis. Performance figures stated in Sections 2.2, 4.6, and 5.4.3 are summaries derived from the analysis herein. If a conflict exists between those sections and this one, **this section takes precedence**.

---

### 6.1 Computational Complexity

#### 6.1.1 Per-Frame Complexity Classification

The ball physics update is **O(1)** per frame. There are no loops over agents, no spatial searches, and no variable-length iterations. Every code path through `UpdateBallPhysics()` executes a bounded, deterministic number of arithmetic operations regardless of match state.

**Why this matters:** O(1) means ball physics will never become a bottleneck as the simulation scales. Agent count, AI complexity, and rendering load have zero impact on ball physics cost. This is by design â€” the ball is a single entity with fixed-size state.

#### 6.1.2 Operation Counts by Ball State

Each frame, the main update loop branches based on `BallStateType`. The following table counts floating-point operations (add, multiply, divide, sqrt) per code path, derived directly from the implementations in Section 3.1.

| Ball State | Force Functions Called | Float Ops (Approx.) | Vector Ops (Approx.) | Branch Checks |
|---|---|---|---|---|
| **AIRBORNE** | Gravity + Drag + Magnus + SpinDecay | 45â€“55 | 18â€“22 | 8â€“10 |
| **ROLLING** | Drag + RollingFriction | 18â€“22 | 8â€“10 | 4â€“5 |
| **BOUNCING** | ApplyBounce + Drag | 35â€“45 | 14â€“18 | 6â€“8 |
| **STATIONARY** | None (early return) | 2 | 2 | 3 |
| **CONTROLLED** | None (early return) | 2 | 2 | 3 |
| **OUT_OF_PLAY** | None (early return) | 2 | 2 | 3 |

**Common to all active states (AIRBORNE, ROLLING, BOUNCING):**

| Step | Operations | Notes |
|---|---|---|
| Last-valid-state backup | 2 vector copies | 6 float assignments |
| Wind-relative velocity | 1 vector subtract | 3 float subtracts |
| Semi-implicit Euler integration | 1 divide + 2 vector multiply-adds | 7 float ops |
| ValidatePhysicsState | 18 float comparisons + 6 NaN checks | Worst-case: 3 clamp operations |
| UpdateBallState | 4â€“6 comparisons | State machine with hysteresis |
| TryLogSnapshot | 1 float subtract + 1 comparison | Usually early-exits (1s interval) |

#### 6.1.3 Detailed Breakdown: AIRBORNE (Worst Case)

AIRBORNE is the most expensive path. Here is the operation-level breakdown:

**GetGravityForce():** 1 multiply, 1 Vector3 construction = ~4 ops

**CalculateDragForce():**
- `velocity.magnitude`: 3 multiply + 2 add + 1 sqrt = 6 ops
- Speed guard check: 1 comparison
- `GetDragCoefficient()`: 2 comparisons + 1 subtract + 1 divide + 1 lerp (worst case) = ~5 ops
- Force magnitude: 4 multiplies = 4 ops
- Direction: `normalized` (6 ops) + 1 scalar multiply = 7 ops
- **Subtotal: ~23 ops**

**CalculateMagnusForce():**
- `velocity.magnitude`: 6 ops (as above)
- `angularVelocity.magnitude`: 6 ops
- Two guard checks: 2 comparisons
- Spin parameter S: 1 multiply + 1 divide + 1 clamp = ~4 ops
- Lift coefficient C_L: 1 subtract + 1 divide + 1 multiply + 1 add = 4 ops
- Cross product: 6 multiply + 3 subtract = 9 ops
- Guard on cross product: 3 multiply + 2 add + 1 comparison = 6 ops
- Normalize: 6 ops
- Force magnitude: 4 multiplies = 4 ops
- Final multiply: 3 ops
- **Subtotal: ~50 ops**

**UpdateSpinDecay():**
- Two magnitudes: 12 ops
- Guard check: 1 comparison
- r^5 power: 4 multiplies (rÃ—rÃ—rÃ—rÃ—r) = 4 ops
- Torque magnitude: 3 multiplies = 3 ops
- Torque vector: normalize (6) + scalar multiply (3) = 9 ops
- Velocity decay: 1 multiply
- Spin decay: 1 multiply
- Combined: 1 add
- Apply decay: 3 multiply + 3 add = 6 ops
- Angular acceleration: 1 divide + 3 multiply = 4 ops
- Add to velocity: 3 multiply + 3 add = 6 ops
- Final magnitude check: 6 ops + 1 comparison
- **Subtotal: ~50 ops**

**Total AIRBORNE worst-case per frame:** ~4 + 23 + 50 + 50 + 7 (integration) + 30 (validation) + 6 (state machine) + 2 (logger) = **~172 floating-point operations**

This is trivial for any modern CPU. A single core at 3 GHz with 1 FLOP/cycle can perform 3 billion ops/second. 172 ops takes approximately **57 nanoseconds** of pure arithmetic, excluding memory access and branch prediction.

#### 6.1.4 Detailed Breakdown: BOUNCING (Second Worst Case)

BOUNCING triggers `ApplyBounce()` before the normal force switch:

**ApplyBounce():**
- 3 surface property lookups (switch statements): ~3 ops each = 9 ops
- Dot product (vn): 3 multiply + 2 add = 5 ops
- Tangent decomposition: 3 multiply + 3 subtract = 6 ops
- Contact point: 3 multiply + cross product (9) = 12 ops
- Contact velocity: 3 add = 3 ops
- Normal impulse: 2 multiply + 1 abs = 3 ops
- Max friction: 1 multiply
- Contact speed + guard: 6 ops + 1 comparison
- Friction impulse (when sliding): ~15 ops
- Angular impulse: cross product (9) + divide (3) = 12 ops
- Final assembly: 6 ops + 3 multiply = 9 ops
- Position correction: 3 assignments
- **Subtotal: ~82 ops**

Plus Drag (~23 ops) + common steps (~45 ops) = **~150 ops** per bounce frame.

Bounce frames are rare â€” typically 2â€“5 per ground contact event, a few dozen per match.

#### 6.1.5 Match-Level Complexity

**90-minute match at 60Hz:**
- Total frames: 90 Ã— 60 Ã— 60 = **324,000 frames**
- No frame depends on previous frames' computational cost (each is independent O(1))

**Estimated state distribution over a typical match (conservative):**

| State | % of Frames | Frame Count | Ops/Frame | Total Ops |
|---|---|---|---|---|
| ROLLING | 40% | 129,600 | ~70 | 9,072,000 |
| AIRBORNE | 20% | 64,800 | ~172 | 11,145,600 |
| CONTROLLED | 25% | 81,000 | ~10 | 810,000 |
| STATIONARY | 10% | 32,400 | ~10 | 324,000 |
| BOUNCING | 3% | 9,720 | ~150 | 1,458,000 |
| OUT_OF_PLAY | 2% | 6,480 | ~10 | 64,800 |
| **TOTAL** | **100%** | **324,000** | | **~22.9 million** |

~23 million float ops per 90-minute match. At 3 GHz: ~7.6 milliseconds of pure compute for the entire match. Ball physics is categorically insignificant as a CPU consumer.

---

### 6.2 Profiling Targets

#### 6.2.1 Budget Derivation

**Available frame budget at 60Hz:** 16.67ms per frame

**Ball physics allocation:** <3% of frame budget = **<0.5ms hard ceiling**

**Target:** 0.3ms average (provides 40% headroom below ceiling)

**Budget justification (why 3%):**

| System | Estimated Budget | % of Frame |
|---|---|---|
| Agent physics (22 agents) | 3.0ms | 18% |
| AI decisions (22 agents) | 4.0ms | 24% |
| Collision detection | 1.0ms | 6% |
| Ball physics | 0.5ms | 3% |
| Event system | 0.5ms | 3% |
| Rendering | 5.0ms | 30% |
| Audio + UI | 1.0ms | 6% |
| Headroom | 1.17ms | 10% |
| **TOTAL** | **16.67ms** | **100%** |

**Critical note:** These budgets are estimates for Stage 0 planning purposes. The agent physics and AI budgets are the real constraints â€” ball physics at 3% is generous. Actual allocation will be validated when all systems are integrated (Stage 0 Month 9-10, per Development Plan).

#### 6.2.2 Per-Function Profiling Targets

Based on the operation counts in Section 6.1, expected timings on target hardware:

| Profiler Label | Target (avg) | Ceiling (p99) | Notes |
|---|---|---|---|
| `BallPhysics.Update` | <0.05ms | <0.15ms | Full update including all substeps |
| `BallPhysics.Magnus` | <0.01ms | <0.03ms | Most expensive single calculation |
| `BallPhysics.Drag` | <0.005ms | <0.01ms | Simple arithmetic |
| `BallPhysics.Bounce` | <0.02ms | <0.05ms | Only on bounce frames |
| `BallPhysics.SpinDecay` | <0.01ms | <0.03ms | Includes Pow() call |
| ValidatePhysicsState | <0.005ms | <0.02ms | NaN checks + clamps |

**Observation:** The 0.3ms "target" and 0.5ms "ceiling" from Section 2.2 and 4.6.2 are extremely conservative relative to the operation counts. Pure arithmetic for the worst-case AIRBORNE path is ~57 nanoseconds. Even with cache misses, branch mispredictions, and profiling overhead, the actual measured time should be well under 0.1ms.

**Why keep the conservative target:** At this stage, we cannot validate against real hardware. Memory access patterns, Unity engine overhead, garbage collection pauses, and profiler instrumentation all add latency that pure operation counting doesn't capture. The 0.3ms target provides a comfortable margin. If profiling shows the system is 10x faster than target (likely), the surplus budget can be reallocated to AI or rendering.

#### 6.2.3 Measurement Protocol

**When:** After implementation, before integration with other systems

**How:**

1. Run 100 simulated matches (headless, no rendering)
2. Record `BallPhysics.Update` timing every frame via Unity Profiler
3. Calculate: mean, p50, p95, p99, max
4. Flag any frame exceeding 0.5ms for investigation
5. Record per-function breakdown for the p99 frame

**Pass criteria:**

| Metric | Required | Desired |
|---|---|---|
| Mean | <0.3ms | <0.1ms |
| p50 | <0.3ms | <0.1ms |
| p95 | <0.4ms | <0.15ms |
| p99 | <0.5ms | <0.2ms |
| Max | <1.0ms | <0.5ms |

**Failure response:**
- If mean >0.3ms: Profile per-function, identify bottleneck, apply optimization from Section 6.3
- If p99 >0.5ms: Check for GC allocation (should be zero), check for branch misprediction in state machine
- If max >1.0ms: Likely external factor (GC pause, OS interrupt) â€” verify by checking if BallPhysics.Update itself was slow or if the frame was slow globally

#### 6.2.4 Target Hardware Baseline

**Minimum spec (Stage 0 prototype):**
- CPU: Intel i5-8250U or equivalent (2018 laptop, 4 cores, 1.6â€“3.4 GHz)
- RAM: 8 GB
- GPU: Integrated (Intel UHD 620) â€” irrelevant for physics

**Why laptop-tier:** If the simulation runs well on a 2018 laptop, it will run everywhere. Web deployment targets (Stage 2+) will include mobile browsers, which have comparable or better single-thread performance to 2018 laptop CPUs.

**Ball physics is not hardware-bound.** The ~172 ops per AIRBORNE frame would run acceptably on hardware from 2010. The real performance concerns for this project are AI and rendering, not physics.

---

### 6.3 Memory Analysis

#### 6.3.1 Static Memory (Loaded Once)

| Component | Size | Notes |
|---|---|---|
| `BallState` struct | 64 bytes | 4Ã—Vector3(12) + 2Ã—Vector3(12) + enum(4) = 64 bytes (per Section 4.5.1 v1.2) |
| `BallPhysicsConstants` | ~400 bytes | All const/static readonly, in .data segment |
| `SurfaceProperties` switch tables | ~100 bytes | 5 surface types Ã— 4 properties Ã— ~5 bytes |
| **Total static** | **~560 bytes** | Negligible |

#### 6.3.2 Dynamic Memory (Per-Match)

**BallEventLogger:**

The logger uses a `List<BallEvent>` which allocates on the managed heap.

**BallEvent struct size:**

| Field | Type | Size |
|---|---|---|
| Timestamp | float | 4 bytes |
| Type | enum (int) | 4 bytes |
| Position | Vector3 | 12 bytes |
| Velocity | Vector3 | 12 bytes |
| AgentID | int | 4 bytes |
| Detail | string (ref) | 8 bytes (pointer on 64-bit) |
| **Total** | | **44 bytes** + string heap allocation |

**Concern: The `Detail` field is a string.**

Each `LogBounce()` call creates a new string via interpolation:
```csharp
Detail = $"Surface:{surface},CoR:{cor:F2},Vn:{vnBefore:F1}â†’{vnAfter:F1}"
```

This allocates on the managed heap and creates GC pressure. Over a 90-minute match:

**Estimated event counts per match:**

| Event Type | Frequency | Count | String Alloc? |
|---|---|---|---|
| POSITION_SNAPSHOT | Every 1.0s | ~5,400 | No (empty string) |
| KICK | Per possession change | ~400â€“600 | Yes (~20 bytes each) |
| BOUNCE | Per ground contact | ~200â€“400 | Yes (~40 bytes each) |
| GOAL_POST_HIT | Rare | ~2â€“10 | Yes (~30 bytes each) |
| GOAL | Rare | ~2â€“6 | Yes (~10 bytes each) |
| HEADER | Per headed ball | ~50â€“100 | Yes (~15 bytes each) |
| POSSESSION_CHANGE | Per change | ~400â€“600 | No (not yet implemented) |

**Total events per match:** ~6,500â€“7,100 (dominated by snapshots)

**Memory per match:**
- BallEvent structs: ~7,000 Ã— 44 bytes = **~308 KB**
- String allocations: ~1,200 strings Ã— ~25 bytes average = **~30 KB**
- List overhead (capacity doubling): Up to 2Ã— struct memory = **~616 KB worst case**
- **Total logger memory: ~650 KB per match**

**Assessment:** 650 KB is acceptable for a desktop/web target. However, the string allocation pattern is a GC concern (see Section 6.3.3).

#### 6.3.3 Garbage Collection Risk Assessment

**Zero-allocation physics update:** The `UpdateBallPhysics()` main loop creates zero heap allocations. All calculations use stack-allocated structs and value types. This is correct and must be preserved.

**Logger allocations (managed heap):**
- `List<BallEvent>` resizes via doubling (allocates new backing array, old one becomes garbage)
- String interpolation in `Detail` field creates garbage every event
- `$"Surface:{surface},CoR:{cor:F2}..."` allocates ~40 bytes per bounce

**GC risk level: LOW but non-zero.**

At ~7,000 events per match, the List will resize approximately 13 times (2^13 = 8,192 > 7,000). Each resize creates a garbage array. The largest garbage array is ~154 KB (half the final capacity). Unity's incremental GC should handle this without frame spikes, but it should be monitored.

**Mitigation (implement if GC spikes observed during profiling):**
1. Pre-allocate List capacity: `new List<BallEvent>(8192)` â€” eliminates all resize allocations
2. Replace `Detail` string with a fixed-size enum + numeric fields â€” eliminates all string allocations
3. Use ring buffer instead of List (Section 4 mentions this) â€” caps memory growth

**Recommendation:** Pre-allocate List capacity at match start. This is trivial (one line of code) and eliminates the resize problem entirely. The string allocation issue is low priority â€” address only if profiling shows GC spikes correlated with event logging.

#### 6.3.4 Cache Performance

**BallState struct (64 bytes):** Fits in a single L1 cache line (64 bytes typical). All fields accessed during `UpdateBallPhysics()` are in one contiguous block. This is optimal â€” no cache misses during the physics update for ball state data.

**Constants access:** All constants are `const` (inlined at compile time) or `static readonly` (loaded once into L1 on first access, stays resident). No cache concern.

**Surface property lookups:** Switch statements compile to jump tables. The surface type doesn't change within a match, so branch prediction will be ~100% accurate after the first frame.

**Assessment:** Cache performance is effectively perfect for ball physics. The entire working set (state + constants) fits in L1 with room to spare.

---

### 6.4 Optimization Opportunities

#### 6.4.1 Classification

Optimizations are categorized by priority:

- **P0 (Required):** Must implement if profiling shows budget exceeded
- **P1 (Recommended):** Low effort, measurable benefit, implement during initial coding
- **P2 (Deferred):** Only implement if needed, likely unnecessary
- **P3 (Future Stage):** Architecture changes for Stage 1+

#### 6.4.2 P0: Required If Budget Exceeded

**None expected.** The operation count analysis (Section 6.1) shows ball physics is ~100x under budget. A P0 optimization would only be needed if the analysis is fundamentally wrong (e.g., Unity overhead is orders of magnitude higher than expected).

If P0 is triggered, the investigation order is:
1. Check for accidental heap allocation in the update loop (boxing, LINQ, string ops)
2. Check for Unity API overhead (Vector3.magnitude may be slower than manual sqrt)
3. Check profiling instrumentation cost (remove profiler hooks, re-measure)

#### 6.4.3 P1: Recommended During Initial Implementation

**P1-A: Pre-allocate BallEventLogger capacity**

```csharp
// At match initialization
private readonly List<BallEvent> _events = new(8192);
```

- **Effort:** 1 line change
- **Benefit:** Eliminates ~13 GC allocations per match
- **Risk:** None
- **Verdict:** Always implement this. No reason not to.

**P1-B: Replace `Mathf.Pow(radius, 5)` with manual multiplication in SpinDecay**

```csharp
// Before (calls Math.Pow, which handles arbitrary exponents)
float r5 = Mathf.Pow(BallPhysicsConstants.Ball.RADIUS, 5);

// After (5 multiplies, avoids function call overhead)
float r = BallPhysicsConstants.Ball.RADIUS;
float r5 = r * r * r * r * r;
```

- **Effort:** 1 line change
- **Benefit:** Avoids `Math.Pow()` function call overhead (~10-50ns)
- **Risk:** None (mathematically identical)
- **Verdict:** Implement. `Pow()` for integer exponents is wasteful.

**P1-C: Pre-compute `RADIUS^5` as a constant**

Since RADIUS never changes, `r^5` can be a compile-time constant:

```csharp
public static class Ball
{
    public const float RADIUS = 0.11f;
    // Pre-computed: 0.11^5 = 0.0000161051
    public const float RADIUS_POW_5 = 0.0000161051f;
}
```

- **Effort:** 2 lines (add constant + replace usage)
- **Benefit:** Eliminates 4 multiplies per AIRBORNE frame
- **Risk:** Must update if RADIUS changes (add comment noting dependency)
- **Verdict:** Implement. Trivial optimization, zero runtime cost.

**P1-D: Use `sqrMagnitude` instead of `magnitude` for guard checks**

```csharp
// Before (calculates sqrt, then compares)
if (speed < MIN_VELOCITY) ...

// After (compares squared values, avoids sqrt)
if (velocity.sqrMagnitude < MIN_VELOCITY * MIN_VELOCITY) ...
```

- **Effort:** ~10 line changes across all guard checks
- **Benefit:** Avoids 3â€“5 sqrt operations per frame (sqrt is ~5-20ns each)
- **Risk:** Slightly less readable; requires pre-computing squared thresholds
- **Verdict:** Implement for guard checks only (where the magnitude value isn't needed afterward). Do NOT apply where the actual magnitude is used in subsequent calculations.

#### 6.4.4 P2: Deferred (Only If Needed)

**P2-A: Spin parameter lookup table**

Pre-compute a 256-entry lookup table mapping spin parameter S to lift coefficient C_L. Replace the per-frame calculation with a table lookup + interpolation.

- **Effort:** ~20 lines (initialization + lookup function)
- **Benefit:** Replaces ~8 ops with 1 array access + 1 lerp (~5 ops)
- **Risk:** Adds complexity, marginal benefit (~3 ops saved)
- **Verdict:** Not worth it. The current linear calculation is 4 ops. A lookup table saves ~2 ops but adds memory access latency and code complexity.

**P2-B: SIMD vectorization**

Use Unity's `Mathematics` package (`float3`, `math.dot()`, etc.) which maps to SIMD instructions on supported hardware.

- **Effort:** Moderate refactor (replace all Vector3 with float3)
- **Benefit:** 2â€“4x throughput for vector operations
- **Risk:** Breaks compatibility with standard Unity Vector3 API; may complicate Fixed64 migration
- **Verdict:** Unnecessary. Ball physics is already ~100x under budget. SIMD would matter if we were simulating 100+ balls simultaneously, which we are not.

**P2-C: Replace `Detail` string with structured data**

```csharp
// Replace string Detail with:
public struct BallEventData
{
    public SurfaceType Surface;
    public float Param1;  // e.g., CoR
    public float Param2;  // e.g., Vn_before
    public float Param3;  // e.g., Vn_after
}
```

- **Effort:** Moderate (change struct, update all logging calls, update replay reader)
- **Benefit:** Eliminates all string allocations in logger (~1,200 per match)
- **Risk:** Less human-readable event data; replay system must decode
- **Verdict:** Implement only if GC profiling shows string allocations causing frame spikes. Unlikely to be needed.

#### 6.4.5 P3: Future Stage Optimizations

**P3-A: Fixed64 migration (Stage 5+)**

Replacing float with Fixed64 will increase per-operation cost by approximately 2â€“5x (fixed-point multiply is ~3 ops, division is ~10 ops vs. hardware FPU instructions). Expected impact:

| Metric | Float (Current) | Fixed64 (Estimated) |
|---|---|---|
| AIRBORNE ops/frame | ~172 | ~500â€“700 |
| Time per frame | ~0.05ms | ~0.15â€“0.25ms |
| Budget utilization | ~10% | ~30â€“50% |

Still well within the 0.5ms ceiling. Fixed64 migration is feasible without architectural changes.

**P3-B: Multi-ball simulation (Stage 3+ training/replay)**

If future features require simulating multiple balls (e.g., training mini-games, what-if analysis):

- Current architecture: One `BallState` struct, one `UpdateBallPhysics()` call
- Multi-ball: Array of `BallState`, loop over array
- Complexity: O(n) where n = ball count
- At n=10: ~1,720 ops = still trivially fast
- No architectural change needed; just call the update function in a loop

**P3-C: Accelerated simulation (Stage 2+ fast-forward)**

For simulating matches at >1x speed (e.g., simulating a season):

- At 10x speed: 10 physics updates per rendered frame = ~0.5ms (still within budget)
- At 100x speed: 100 updates per frame = ~5ms (exceeds ball physics budget but acceptable as a batch operation since rendering is disabled)
- At 1000x speed (headless): CPU-bound; ~50ms per frame of physics alone. Acceptable for background processing.

**Optimization for fast-forward:** Skip logging and validation in headless mode. Reduces ops by ~30%.

---

### 6.5 Thermal and Sustained Performance

#### 6.5.1 Sustained Load Analysis

Ball physics runs continuously for 90+ minutes per match. Unlike burst workloads, sustained load can cause thermal throttling on laptops and mobile devices.

**Ball physics contribution to thermal load:** Negligible.

At ~0.05ms per frame, ball physics consumes <0.3% of a single CPU core's capacity. Even if every other system in the simulation is also running, ball physics will not meaningfully contribute to thermal throttling. The systems to watch for thermal impact are rendering (GPU), AI (CPU), and any background processes (match simulation threads).

#### 6.5.2 Battery Impact (Mobile/Laptop)

Ball physics power draw: Immeasurable in isolation. The entire match simulation's power draw will be dominated by rendering and AI. Ball physics optimization for power savings has zero ROI.

---

### 6.6 Scalability Considerations

#### 6.6.1 What Doesn't Scale (And Doesn't Need To)

Ball physics is inherently a single-entity system. There is one ball. The computational cost does not increase with:

- Number of agents (22 or 2,200 â€” ball physics is the same)
- Pitch size (no spatial queries)
- Match duration (no accumulated state beyond the logger)
- AI complexity (ball physics is downstream of decisions, not upstream)

#### 6.6.2 What Could Scale (Future)

| Scenario | Scale Factor | Impact | Mitigation |
|---|---|---|---|
| Multi-ball training | n balls | O(n), linear | Loop over array, no architecture change |
| Headless batch sim | m matches | O(m), linear | Parallelize across CPU cores |
| Replay reconstruction | 1 ball, variable speed | O(speed factor) | Skip logging in headless mode |
| Fixed64 migration | 1 ball, higher cost | ~3x per-op | Still within budget |

None of these scenarios require architectural changes to the ball physics system.

---

### 6.7 Performance Anti-Patterns to Avoid

During implementation, the following patterns MUST be avoided in the ball physics update loop:

| Anti-Pattern | Why It's Dangerous | How to Detect |
|---|---|---|
| LINQ queries | Allocates enumerator objects on heap | Code review; zero-allocation policy |
| String concatenation in hot path | GC pressure every frame | Profiler heap allocation track |
| `new Vector3()` in struct methods | May box on some Unity versions | IL inspection or profiler |
| Dictionary lookups in update loop | Hash computation + potential resize | Code review; use switch or array |
| `Debug.Log()` without conditional | String formatting every frame even in release | Preprocessor guards required |
| Virtual method calls | Vtable indirection prevents inlining | All physics classes are static |
| `foreach` over List | May allocate enumerator (Unity-version-dependent) | Use `for (int i = 0; ...)` |

**Enforcement:** Code review checklist (see Development Best Practices) must verify zero-allocation compliance before merging any ball physics code.

---

### 6.8 Cross-References

| Topic | Authoritative Section | Summary Statement |
|---|---|---|
| Performance requirements | Section 2.2 (PR-1, PR-2, PR-3) | Derived from the budget analysis in 6.2.1 |
| Profiling hooks | Section 4.6.1 | Implementation of the labels defined in 6.2.2 |
| Performance test protocol | Section 4.6.2 | Executes the measurement protocol from 6.2.3 |
| Profiling acceptance criteria | Section 5.4.3 | Tests verify the targets from 6.2.2 |
| Memory layout | Section 4.5 | BallState struct size used in 6.3.1 |
| Event logger design | Section 3.1.13 | BallEvent struct analyzed in 6.3.2 |
| Fixed64 migration path | Section 4.7 | Performance impact estimated in 6.4.5 |

---

### 6.9 Known Limitations & Future Validation Required

The following items are acknowledged weaknesses in this analysis. Each must be revisited post-implementation.

**KL-1: Operation counts are approximations, not measurements.**
The per-function op counts (e.g., "~172 ops for AIRBORNE") are derived by reading the code in Section 3.1, not by profiling compiled output. Actual cost depends on Unity's Vector3 implementation (hardware intrinsic vs. software sqrt), compiler inlining decisions, and JIT behavior. Treat all op counts as order-of-magnitude estimates until validated by profiling per Section 6.2.3.
**Action:** Replace estimates with measured values after Stage 0 implementation. Update tables in 6.1.2 and 6.1.3.

**KL-2: State distribution percentages (6.1.5) are speculative.**
The 40% ROLLING / 20% AIRBORNE / 25% CONTROLLED split is an educated guess based on general football match flow. Real distribution could differ significantly â€” e.g., a possession-heavy match may be 40%+ CONTROLLED, reducing total compute. A long-ball style match may be 30%+ AIRBORNE, increasing it.
**Action:** Instrument state tracking in the match simulator. Record actual state distribution over 100 simulated matches. Update 6.1.5 with measured data.

**KL-3: Frame budget table (6.2.1) uses placeholder values for unbuilt systems.**
Agent physics (3.0ms), AI decisions (4.0ms), collision detection (1.0ms), and rendering (5.0ms) are estimates. These systems have no specifications yet (Specs #2â€“#7, #12â€“#15). The ball physics 3% allocation may need to shrink or could grow depending on actual system costs.
**Action:** Revisit full frame budget after Specs #2â€“#4 are written. Finalize budget allocation during Stage 0 Month 9-10 integration testing per Development Plan.

**KL-4: BallEvent struct size may vary by platform.**
Listed as 44 bytes but struct padding/alignment rules differ across platforms (Mono vs. IL2CPP, 32-bit vs. 64-bit). Actual size may be 48 bytes due to alignment. The string reference field (Detail) is 4 bytes on 32-bit, 8 bytes on 64-bit.
**Action:** Verify with `System.Runtime.InteropServices.Marshal.SizeOf<BallEvent>()` after implementation. Update 6.3.2 totals if needed.

**KL-5: `HasInvalidValues()` branch prediction cost not analyzed.**
The NaN/Infinity check evaluates 18 boolean conditions (6 fields Ã— 3 components, each checked for IsNaN and IsInfinity). On modern CPUs with branch prediction, this should be near-zero cost since the "valid" path is taken ~100% of the time. However, this is unverified.
**Action:** Include `HasInvalidValues` in per-function profiling. If >0.01ms, consider replacing with a single bitwise NaN check pattern.

---

**END OF SECTION 6**

---

## Document Status

**Section 6 Completion:**
- âœ… Computational complexity fully derived from Section 3.1 code (6.1)
- âœ… Per-state operation counts with AIRBORNE and BOUNCING detailed breakdowns (6.1.2â€“6.1.4)
- âœ… Match-level cost analysis with state distribution estimate (6.1.5)
- âœ… Frame budget derivation with full system allocation table (6.2.1)
- âœ… Per-function profiling targets with labels matching Section 4.6.1 (6.2.2)
- âœ… Measurement protocol with pass/fail criteria (6.2.3)
- âœ… Memory analysis covering static, dynamic, and GC risk (6.3)
- âœ… Optimization roadmap with P0â€“P3 classification (6.4)
- âœ… Thermal/sustained load and scalability analysis (6.5â€“6.6)
- âœ… Anti-pattern checklist for implementation (6.7)
- âœ… Cross-references to all related sections (6.8)
- âœ… Known limitations documented with action items (6.9)

**Page Count:** ~10 pages

**Ready for:** Review and approval

**Next Sections:** 7 (Future Extensions), 8 (References), Appendices A-C
