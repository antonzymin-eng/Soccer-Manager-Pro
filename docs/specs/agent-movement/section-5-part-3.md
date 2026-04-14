### 5.5 Optimization Roadmap

#### 5.5.1 P0: Required If Budget Exceeded

These optimizations are applied only if profiling reveals budget violations. They require no architectural changes.

**P0-A: Profile per-function, identify bottleneck.**
Use Unity Profiler's per-function breakdown to identify which pipeline step dominates. Expected bottleneck: turning system (most transcendental calls). If confirmed, proceed to P0-B.

**P0-B: Check for accidental heap allocations.**
Run Profiler with "GC Alloc" column enabled. Any non-zero allocation in the movement update loop is a bug. Common sources: string formatting in Debug.Log without preprocessor guards, LINQ usage, enum boxing. Fix immediately.

**P0-C: Eliminate unnecessary Sqrt calls.**
Several places compute `Velocity.magnitude` (which calls Sqrt internally). Where only magnitude comparison is needed, use `sqrMagnitude` instead. Candidates:
- Speed cache update (unavoidable â€” we need the actual speed for formulas)
- GetMovementAngle speed guard (can use `sqrMagnitude > MIN_SPEEDÂ²`)
- HasInvalidValues (does not use Sqrt â€” no change needed)

Estimated savings: 1â€“2 Sqrt calls per agent per frame = ~10â€“20 ns per agent.

**P0-D: Pre-compute repeated subexpressions.**
The term `effectiveAttribute - 1.0f` appears in every attribute mapping function. Pre-compute once per attribute evaluation cycle and pass to all mapping functions. Saves ~4 subtractions per agent per frame (negligible, but good practice).

#### 5.5.2 P1: Recommended During Implementation

**P1-A: Cache PerformanceContext results per 10Hz heartbeat.**
Already designed (Section 3.2.1 + outline Section 4.4). The 10Hz caching of modifier products eliminates ~540 unnecessary Exp calls per second that would occur if fatigue modifiers were recomputed every frame. This is a **design-level optimization already specified** â€” implementation must follow the caching pattern.

**P1-B: Pre-compute speed-squared for state machine comparisons.**
The state machine compares `speed` against thresholds. If speed is stored as both `Speed` (float) and `SpeedSquared` (float), all comparisons can use `SpeedSquared > THRESHOLDÂ²`, avoiding the Sqrt in the speed cache. Saves 1 Sqrt per agent per frame.

**P1-C: Use squared distance for directional zone angle check.**
`GetMovementAngle()` calls Acos, which is expensive. For the zone classification (which only needs to know which zone the angle falls in, not the exact angle), a dot-product-based zone check avoids Acos entirely. The dot product of facing and movement vectors directly maps to zone boundaries: `dot > cos(30Â°)` for forward, `dot > cos(90Â°)` for lateral, etc. Pre-computed cos values for zone boundaries are constants. Saves 1 Acos per agent per frame.

**Estimated P1 savings:** 1 Sqrt + 1 Acos per agent per frame = ~10 equiv ops per agent = 220 per frame across 22 agents. Modest but free.

#### 5.5.3 P2: Deferred Optimizations

**P2-A: SIMD batching of agent updates.**
Process 4 agents simultaneously using SIMD (SSE/AVX) intrinsics. Requires restructuring the agent array to SoA layout. Theoretical 4Ã— throughput on arithmetic-bound sections. Realistic gain: 2â€“3Ã— due to branch divergence between agents in different states.

**P2-B: SoA (Structure of Arrays) layout.**
Reorganize Agent data from AoS to SoA for better cache utilization. Position, Velocity, and State arrays are accessed in tight loops; other fields (attributes, recovery state) are accessed less frequently and can remain in a separate "cold" struct.

**P2-C: Reduce safety checks for stable agents.**
Agents that have been in the same state for >1 second with no anomalies can skip the full HasInvalidValues check (22 branches) and only check Position and Velocity (12 branches). Requires a `StableFrameCount` field to track consecutive clean frames. **Implementation note:** Adding `StableFrameCount` to the Agent class requires a Section 3.5 amendment (new field, ~4 bytes, no architectural impact). This amendment should be deferred until profiling confirms the optimization is needed.

**P2-D: LOD (Level of Detail) movement.**
Agents far from the ball receive simplified updates. Three tiers:
- LOD 0 (within 20m of ball): Full pipeline per this spec
- LOD 1 (20â€“40m): Skip turning calculations, reduce safety checks
- LOD 2 (>40m): Lerp to target position, update at 30Hz instead of 60Hz

Estimated savings: ~40% reduction in total movement cost for typical match states where 8â€“12 agents are far from the ball.

**Determinism guarantee:** LOD tier assignment is deterministic â€” it is a pure function of ball position and agent position, both of which are authoritative simulation state. LOD 1/2 agents still write to the same authoritative Agent struct fields; the simplified math produces slightly different results than the full pipeline (e.g., turning without lean angle computation), so agents may exhibit micro-differences when transitioning between LOD tiers. To avoid visible pops, LOD transitions should blend over 3â€“5 frames (lerp between simplified and full pipeline results). Since LOD assignment is deterministic, replay and multiplayer synchronization are preserved â€” all clients compute the same LOD tier for each agent on each frame.

#### 5.5.4 P3: Future Stage Optimizations

**P3-A: Fixed64 migration (Stage 5+).**
Replacing float with Fixed64 increases per-operation cost by approximately 2â€“5Ã—. Transcendental functions (Exp, Atan2, Cos, Sin) become especially expensive â€” software lookup tables replace hardware FPU instructions.

| Metric | Float (Current) | Fixed64 (Estimated) |
|---|---|---|
| SPRINTING equiv ops/agent | ~180 | ~500â€“700 |
| Per-agent time | ~0.01ms | ~0.03â€“0.05ms |
| 22 agents total | ~0.22ms | ~0.66â€“1.1ms |
| Budget utilization | ~7% | ~22â€“37% |

Still within the 3.0ms ceiling, but with reduced headroom. P2 optimizations (SIMD, SoA, LOD) may become necessary to maintain comfortable margins.

**P3-B: Headless mode for accelerated simulation.**
Skip logging, validation, and animation data output for background match simulation. Estimated savings: ~30% per agent (removes safety checks, cache updates, and logger).

---

### 5.6 Performance Anti-Patterns to Avoid

During implementation, the following patterns MUST be avoided in the agent movement update loop. Mirrors Ball Physics Section 6.7.

| Anti-Pattern | Why It's Dangerous | How to Detect |
|---|---|---|
| LINQ queries | Allocates enumerator objects on heap; 22 agents = 22 allocations/frame | Code review; zero-allocation policy |
| String concatenation in hot path | GC pressure every frame; 22Ã— amplification | Profiler heap allocation track |
| Dictionary lookups in update loop | Hash computation + potential resize per agent | Code review; use switch or array index |
| `Debug.Log()` without conditional | String formatting for 22 agents every frame | Preprocessor guards required |
| Virtual method calls on Agent | Vtable indirection prevents inlining | Agent class is not polymorphic; keep it sealed |
| `foreach` over agent list | May allocate enumerator (Unity-version-dependent) | Use `for (int i = 0; i < count; i++)` |
| `new` keyword in update loop | Any heap allocation in 22Ã— hot path | Profiler; struct types only |
| Redundant magnitude calculations | Velocity.magnitude called multiple times per pipeline | Cache Speed once, use it everywhere |
| Per-frame PerformanceContext recalc | 3 multiplies Ã— 4 attributes Ã— 22 agents = 264 extra ops | Use 10Hz caching pattern |

**Enforcement:** Code review checklist (see Development Best Practices) must verify zero-allocation compliance before merging any agent movement code. The 22Ã— amplification factor makes any per-agent allocation 22Ã— worse than the equivalent Ball Physics violation.

---

### 5.7 Known Limitations & Future Validation Required

**KL-1: Operation counts are approximations, not measurements.**
Per-function op counts are derived by reading the code in Sections 3.1â€“3.6, not by profiling compiled output. Actual cost depends on Unity's Vector3 implementation (hardware intrinsic vs. software Sqrt), C# JIT compiler inlining decisions, and managed runtime overhead. Treat all op counts as order-of-magnitude estimates. **Action:** Replace estimates with measured values after Stage 0 implementation. Update tables in 5.1.2â€“5.1.4.

**KL-2: State distribution percentages (5.1.6) are speculative.**
The 35% JOGGING / 25% WALKING / 15% IDLE / 10% SPRINTING / 10% DECEL / 3% STUMBLING / 2% GROUNDED distribution is an educated guess based on published GPS tracking data. Real simulated distribution could differ significantly â€” an aggressive pressing tactic may produce 20%+ SPRINTING, while a possession-based style may be 40%+ WALKING. **Action:** Instrument state tracking in the match simulator. Record actual state distribution over 100 simulated matches across varied tactical setups. Update 5.1.6 with measured data.

**KL-3: Frame budget table uses placeholder values for unbuilt systems.**
AI decisions (4.0ms), collision detection (1.0ms), and rendering (5.0ms) are estimates. These systems have no specifications yet (Specs #3, #7, #12â€“15). The agent physics 3.0ms allocation may need adjustment. **Action:** Revisit full frame budget after Specs #3 and #7 are written.

**KL-4: Agent struct size may vary with platform alignment.**
Listed as ~224 bytes but struct padding/alignment rules differ across platforms (Mono vs. IL2CPP, 32-bit vs. 64-bit). Actual size may be 224â€“256 bytes due to alignment and the class object header. **Action:** Verify with `System.Runtime.InteropServices.Marshal.SizeOf<Agent>()` after implementation (note: this measures unmanaged size; managed class overhead adds 16 bytes on 64-bit). Update 5.2.1 if needed.

**KL-5: 10Hz fatigue update cost not separately profiled.**
The 10Hz heartbeat's additional ~176 float ops per frame is estimated but not measured. The interaction between PerformanceContext cache invalidation and the modifier recomputation path should be profiled to confirm the overhead is negligible as predicted. **Action:** Include `AgentMovement.HeartbeatUpdate` as a separate profiler label. Measure the delta between 10Hz frames and normal frames.

**KL-6: Cache performance for 22 sequential agent updates not measured.**
The L1/L2 cache analysis in 5.2.5 is theoretical. Actual cache miss rates depend on what other systems access between agent movement frames, Unity's memory allocator layout, and GC compaction patterns. **Action:** Run hardware performance counters (cache miss rate, instructions per cycle) on the agent update loop. If L1 miss rate exceeds 5%, evaluate SoA migration (P2-B).

**KL-7: Transcendental function cost is hardware-dependent.**
The 5 equiv-ops estimate for transcendental functions (Exp, Sqrt, Atan2, Acos, Sin, Cos) is based on modern x86 FPUs. On ARM (mobile, Apple Silicon), WebAssembly (future web deployment), or software implementations, costs may be 2â€“10Ã— higher. The most sensitive function is `ApplyTurnRateLimit()` which calls Atan2, Cos, Sin, and Sqrt on every active agent frame. **Action:** Profile transcendental function cost on target platforms during implementation. If ARM/Wasm costs exceed 50 ns per call, consider P1-C (dot-product zone check to eliminate Acos) and lookup-table approximations for Sin/Cos.

---

### 5.8 Cross-References

| Topic | Authoritative Section | Summary |
|---|---|---|
| Frame budget allocation | Ball Physics Spec #1 Â§6.2.1 | 3.0ms allocated to agent physics |
| Per-agent performance requirement | Section 2.2 (PR-1) | Derived from 3.0ms / 22 agents |
| State machine code (op count source) | Section 3.1 | EvaluateState() and per-state evaluators |
| Locomotion code (op count source) | Section 3.2 | Acceleration models and attribute mapping |
| Directional code (op count source) | Section 3.3 | Zone multiplier and angle computation |
| Turning code (op count source) | Section 3.4 | Turn rate, stumble risk, lean angle |
| Data structures (memory analysis source) | Section 3.5 | Agent class field definitions |
| Safety system (validation cost source) | Section 3.6 | HasInvalidValues, ApplySafetyClamps |
| Implementation details (pipeline order) | Section 4 (outline) | Per-frame execution sequence |
| Fixed64 migration path | Section 6 (future) | Performance impact on Fixed64 |
| Measurement protocol pattern | Ball Physics Spec #1 Â§6.2.3 | 100-match profiling methodology |
| Anti-pattern checklist | Development Best Practices | Zero-allocation enforcement |

---

**END OF SECTION 5**

---

