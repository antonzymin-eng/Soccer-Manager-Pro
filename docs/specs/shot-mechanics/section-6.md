# Shot Mechanics Specification #6 — Section 6: Performance Analysis

**File:** `Shot_Mechanics_Spec_Section_6_v1_0.md`
**Purpose:** Authoritative performance analysis for the Shot Mechanics system —
computational complexity classification, operation counts derived from the §3 sub-system
formulas, memory footprint, profiling targets, trig cost evaluation, optimization roadmap,
and known limitations. This section is the single authoritative source for all Shot
Mechanics performance targets and budgets. Budget figures stated in §2.6 are summaries
derived from this section; if a conflict exists, this section takes precedence.

**Created:** February 23, 2026, 11:30 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 v1.1, Section 2 v1.0, Section 3 Part 1 (§3.1–§3.3) v1.1,
Section 3 Part 2 (§3.4–§3.10) v1.2, Section 4 v1.3, Section 5 v1.3

**Open Dependency Flags:** None. All hard dependencies confirmed stable in Section 1.

---

## Table of Contents

- [Preamble: Role of This Section](#preamble-role-of-this-section)
- [6.1 Computational Complexity](#61-computational-complexity)
  - [6.1.1 Overall Classification](#611-overall-classification)
  - [6.1.2 Per-Stage Complexity Breakdown](#612-per-stage-complexity-breakdown)
  - [6.1.3 Operation Count by Pipeline Stage](#613-operation-count-by-pipeline-stage)
  - [6.1.4 Per-Evaluation Summary](#614-per-evaluation-summary)
  - [6.1.5 Comparison to Pass Mechanics](#615-comparison-to-pass-mechanics)
  - [6.1.6 Simultaneous Shot Analysis (Worst-Case Bounding)](#616-simultaneous-shot-analysis-worst-case-bounding)
- [6.2 Performance Budget](#62-performance-budget)
  - [6.2.1 Frame Budget Derivation](#621-frame-budget-derivation)
  - [6.2.2 Profiling Markers](#622-profiling-markers)
  - [6.2.3 Per-Function Timing Targets](#623-per-function-timing-targets)
  - [6.2.4 Measurement Protocol](#624-measurement-protocol)
- [6.3 Trig Cost Evaluation](#63-trig-cost-evaluation)
- [6.4 Memory Footprint](#64-memory-footprint)
  - [6.4.1 Per-Evaluation Stack Usage (ShotContext)](#641-per-evaluation-stack-usage-shotcontext)
  - [6.4.2 Static (Persistent) Memory](#642-static-persistent-memory)
  - [6.4.3 Cache-Line Analysis](#643-cache-line-analysis)
- [6.5 Cache Behaviour](#65-cache-behaviour)
- [6.6 Optimization Roadmap](#66-optimization-roadmap)
- [6.7 Anti-Pattern Checklist](#67-anti-pattern-checklist)
- [6.8 Known Limitations](#68-known-limitations)
- [6.9 Fixed64 Migration Notes](#69-fixed64-migration-notes)
- [6.10 Cross-References](#610-cross-references)
- [6.11 Section Summary](#611-section-summary)
- [6.12 Version History](#612-version-history)

---

## Preamble: Role of This Section

This section provides the **single authoritative source** for all Shot Mechanics
performance targets, budgets, and analysis.

**Critical distinction from per-frame systems:** Ball Physics, Agent Movement, and
Collision System all analyse per-frame costs because those systems run every frame.
Shot Mechanics is a **discrete event processor** — it executes only when the Decision
Tree submits a `ShotRequest`, and only the CONTACT state performs the expensive
calculation pipeline. A typical 90-minute match produces approximately 20–40 CONTACT
executions total across both teams, based on real-world shot data (~12–18 shots on
target per match, StatsBomb Open Data) plus off-target and blocked attempts.

This fundamentally changes the performance analysis framework:

1. **Total match budget** matters more than per-frame budget. 40 shot executions at
   0.05ms each = 2ms across a 90-minute match. Trivial.
2. **Per-evaluation latency** is measured in isolation. The question is not "does this
   fit in 16.67ms?" but "does a single CONTACT execution complete before the frame
   budget is exhausted?"
3. **Simultaneous shots are architecturally impossible** in Stage 0. The state machine
   enforces a single active shot per agent, and two agents cannot both be in CONTACT
   on the same frame without violating the possession model. Nevertheless, the
   theoretical maximum is bounded for completeness in §6.1.6.

**Why this section follows Pass Mechanics §6 structure:**
Shot Mechanics is architecturally analogous to Pass Mechanics — both are discrete event
processors with similar pipeline depths. The Pass Mechanics analysis (§6) established
the methodology and terminology. This section reuses that methodology directly and
notes wherever Shot Mechanics differs in cost.

---

## 6.1 Computational Complexity

### 6.1.1 Overall Classification

**Shot Mechanics CONTACT evaluation: O(1)**

Every operation in the 13-step pipeline (§2.2.3) is constant-time. There is no loop
over agents, no spatial query, no data structure traversal. All inputs are scalar
attributes read from a pre-captured snapshot (frozen at INITIATING). There is no
equivalent of the Pass Mechanics O(n) pressure query, because shot error modelling
uses the `Pressure` scalar already present in `AgentState` (a single field read),
rather than querying opponents in a spatial radius.

This makes Shot Mechanics the **cheapest discrete-event system** in the Stage 0
specification set on a per-evaluation basis.

**WINDUP frames: O(1) per frame.**
Each WINDUP frame polls a single tackle interrupt flag — one read, one branch. No
computation.

**FOLLOW_THROUGH and STUMBLING frames: O(1) per frame.**
State machine advances a frame counter. No physics computation.

---

### 6.1.2 Per-Stage Complexity Breakdown

| Pipeline Stage | Sub-System | Complexity | Notes |
|---|---|---|---|
| [1] Validation | §3.1 ShotValidator | O(1) | 8 sequential comparisons |
| [2] Body Mechanics | §3.7 BodyMechanicsEvaluator | O(1) | 4 scalar inputs; 1 sigmoid; 2 multiplies |
| [3] Velocity | §3.2 VelocityCalculator | O(1) | Sigmoid + 5 multiplications + clamp |
| [4] Launch Angle | §3.3 LaunchAngleCalculator | O(1) | Table lookup + 3 additions |
| [5] Spin Vector | §3.4 SpinVectorCalculator | O(1) | Table lookup + 3 scalar multiplications |
| [6] Weak Foot Penalty | §3.8 WeakFootPenaltyApplier | O(1) | 1 branch + 2 multiplications |
| [7] Placement Resolution | §3.5 PlacementResolver | O(1) | 2 lerps + 1 world-space conversion |
| [8] Error Calculation | §3.6 ErrorCalculator | O(1) | 4 multiplications + hash + rotation |
| [9] Velocity Vector3 | ShotExecutor | O(1) | 3 multiplications + Sin/Cos for tilt |
| [10] NaN/bounds guard | ShotExecutor | O(1) | 3 comparisons |
| [11] Ball.ApplyKick() | Ball Physics #1 | O(1) | 2 Vector3 writes + state update |
| [12] Event publication | §3.10 ShotEventEmitter | O(1) | Struct copy to ring buffer |
| [13] State transition | §3.9 ShotStateMachine | O(1) | Enum write + field clear |

**No stage introduces super-linear cost.** Shot Mechanics CONTACT is fully bounded.

---

### 6.1.3 Operation Count by Pipeline Stage

**Methodology:** Equivalent-op (equiv-op) definition inherited from Agent Movement
Spec #2 §5.1. One equiv-op ≈ one CPU multiplication on a modern desktop x86-64. Trig
functions (Sin, Cos, Atan2) are counted at 5 equiv-ops each (Unity Burst-compiled
estimate). Square roots: 3 equiv-ops. Division: 1.5 equiv-ops. Struct field read: 0.5
equiv-ops. Struct field write: 0.5 equiv-ops. Branch (predictable): 0.5 equiv-ops.

**Costs are for CONTACT execution only — the computationally significant frame.**
WINDUP frame cost is < 2 equiv-ops (1 read + 1 branch) and is negligible at 7–17
frames per shot.

---

#### Stage [1] — ShotValidator (§3.1)

8 sequential validation rules. Each is a comparison + branch:

| Operation | Count | Equiv-ops | Subtotal |
|---|---|---|---|
| Field reads (AgentId, PowerIntent×2, SpinIntent×2, ContactZone, PlacementTarget u+v, DistanceToGoal, FrameNumber, BallState.PossessingAgentId) | 11 reads | 0.5 each | 5.5 |
| Comparisons + predictable branches | 9 | 0.5 each | 4.5 |
| Early-return on failure (worst case: all pass) | 1 | 0.5 | 0.5 |
| **Stage [1] total** | | | **10.5** |

---

#### Stage [2] — BodyMechanicsEvaluator (§3.7)

Body mechanics score from run-up angle, body lean, plant foot position, and knee
position. The score is a product of four sub-scalars clamped to [0.0, 1.0]:

| Operation | Equiv-ops |
|---|---|
| Read AgentPhysicalProperties fields (4 fields) | 2.0 |
| Run-up angle penalty: Abs(deltaAngle / MAX_ANGLE) clamped | 2.0 |
| Body lean penalty scalar: similar normalisation | 2.0 |
| Plant foot offset penalty: scalar normalisation | 2.0 |
| Knee position bonus (BelowCentre case): 1 conditional multiply | 1.5 |
| BodyMechanicsScore multiply-combine (3 multiplications) | 3.0 |
| Stumble trigger evaluation (2 comparisons) | 1.0 |
| Emit stumble signal if triggered (conditional struct write) | 0.5 |
| **Stage [2] total** | **14.0** |

---

#### Stage [3] — VelocityCalculator (§3.2)

Most arithmetically complex single stage — the sigmoid blend is the dominant cost:

| Operation | Equiv-ops |
|---|---|
| Read Finishing, LongShots, KickPower, Fatigue, DistanceToGoal, PowerIntent, SpinIntent, ContactZone | 4.0 |
| Sigmoid weight: `(d − D_MID) / D_SCALE` | 1.5 |
| Sigmoid exp: `exp(−x)` (no native exp equiv-op — counted as 2× a multiply) | 2.0 |
| Sigmoid division: `1 / (1 + exp_result)` | 1.5 |
| EffectiveAttribute convex blend (2 multiplies + 1 add) | 3.0 |
| V_BASE formula: normalise attr to [0,1], scale, apply PowerIntent | 4.0 |
| ContactZoneModifier table lookup + apply (1 read + 1 multiply) | 1.5 |
| Spin–velocity trade-off: `(1.0 − SPIN_VELOCITY_TRADE_OFF × SpinIntent)` | 2.0 |
| Fatigue multiplier: `(1.0 − FATIGUE_KICK_PENALTY × Fatigue)` | 2.0 |
| BodyMechanicsScore → ContactQualityModifier (1 read + 1 multiply) | 1.5 |
| WeakFootModifier read + apply (1 read + 1 multiply) — from §3.8 | 1.5 |
| V_ABSOLUTE clamp (2 comparisons + conditionals) | 1.0 |
| **Stage [3] total** | **25.5** |

**Note on sigmoid cost:** `Mathf.Exp()` is the single most expensive operation in
the pipeline. Its measured cost on x86-64 (Burst-compiled) is approximately 2ns,
equivalent to ~2 multiplications at 1GHz. At 60Hz with 20–40 shot events per
match, total sigmoid cost across a full match is < 0.04ms. Not a budget concern.

---

#### Stage [4] — LaunchAngleCalculator (§3.3)

Angle derivation via ContactZone lookup and additive modifiers:

| Operation | Equiv-ops |
|---|---|
| BaseAngle[ContactZone] table lookup (1 read) | 0.5 |
| PowerLiftModifier: `POWER_LIFT_MOD × (1.0 − PowerIntent)` | 2.0 |
| SpinLiftModifier: `SPIN_LIFT_MOD × SpinIntent` | 2.0 |
| BodyLeanPenalty: read + scale | 1.5 |
| BodyShapePenalty: read + scale | 1.5 |
| Sum all additive terms (4 additions) | 2.0 |
| Clamp to [MIN_LAUNCH_ANGLE, MAX_LAUNCH_ANGLE] | 1.0 |
| **Stage [4] total** | **10.5** |

---

#### Stage [5] — SpinVectorCalculator (§3.4)

Spin vector from ContactZone and Technique modifier:

| Operation | Equiv-ops |
|---|---|
| SpinBase[ContactZone] lookup (3 components; 3 reads) | 1.5 |
| SpinIntent scalar read | 0.5 |
| Technique read + normalise to [0, 1] | 1.5 |
| TechniqueModifier scale computation | 1.5 |
| Apply: `spinBase × SpinIntent × TechniqueModifier` (3 multiplications × 3 components) | 9.0 |
| Magnitude cap check + conditional rescale | 2.0 |
| **Stage [5] total** | **16.0** |

---

#### Stage [6] — WeakFootPenaltyApplier (§3.8)

Conditional application — most executions take the false branch:

| Operation | Equiv-ops |
|---|---|
| IsWeakFoot read + branch | 1.0 |
| **If dominant foot (common path):** Return velocityMultiplier = 1.0, weakFootMultiplier = 1.0 | 1.0 |
| **If weak foot (uncommon path):** Read WeakFootRating; compute velocityMultiplier; compute weakFootMultiplier | 5.0 |
| **Stage [6] total (dominant foot)** | **2.0** |
| **Stage [6] total (weak foot)** | **6.0** |

Dominant foot is the statistically common case (~70–80% of shots at elite level;
~85% expected in Stage 0 AI behaviour). Op count below uses **dominant foot (2.0)**
as typical and notes weak-foot variant separately.

---

#### Stage [7] — PlacementResolver (§3.5)

Goal-relative (u, v) to world-space aim direction:

| Operation | Equiv-ops |
|---|---|
| Goal geometry reads (post positions, crossbar height) via GoalGeometryProvider | 2.0 |
| Lerp u across goal width: `GoalLeft + u × GoalWidth` | 2.0 |
| Lerp v across goal height: `GroundY + v × GoalHeight` | 2.0 |
| Target point (3D world-space): 2 float results + 1 fixed Z | 0.5 |
| Aim direction: `(targetPoint − agentPosition)` | 1.5 |
| Normalise direction vector: sqrt (3 equiv-ops) + 3 divides (1.5 each) | 7.5 |
| **Stage [7] total** | **15.5** |

---

#### Stage [8] — ErrorCalculator (§3.6)

Error magnitude, deterministic hash, and error application in goal-relative space:

| Operation | Equiv-ops |
|---|---|
| BaseErrorAngle computation (Finishing normalise + scale) | 3.0 |
| PowerPenaltyScalar: read + multiply | 2.0 |
| PressureScalar: read + multiply | 2.0 |
| FatigueScalar: read + multiply | 2.0 |
| BodyShapeScalar: read (BodyMechanicsScore already computed) + multiply | 1.5 |
| WeakFootMultiplier: read (from Stage [6]) + multiply | 1.5 |
| Full error magnitude: 5 multiplications | 5.0 |
| Deterministic hash: `hash(matchSeed + agentId × PRIME + frameNumber)` — integer ops | 3.0 |
| Hash to error angle in goal-relative space: normalise to [0, 2π], multiply by magnitude | 3.0 |
| Apply error as (Δu, Δv) offset in goal-relative space | 2.0 |
| Clamp to goal boundary or policy (§3.6.11) | 2.0 |
| Reconvert to world-space aim direction: repeat normalise | 7.5 |
| **Stage [8] total** | **34.5** |

**Stage [8] is the most expensive error-propagation stage.** The two vector
normalisations (one for pre-error direction, one for post-error) account for 15 of the
34.5 equiv-ops. If profiling reveals this as a hotspot, both normalisations can be
merged with the Stage [7] normalise into a single operation — a P2 optimisation (§6.6).

---

#### Stage [9] — Velocity Vector3 Construction

Final velocity vector combining scalar speed, aim direction, and launch angle:

| Operation | Equiv-ops |
|---|---|
| Read kickSpeed, finalDirection, launchAngleDeg | 2.0 |
| Convert launchAngleDeg to radians | 1.0 |
| Sin(launchAngle) for vertical component | 5.0 |
| Cos(launchAngle) for horizontal scale | 5.0 |
| Scale horizontal direction: `finalDirection × cos × kickSpeed` (3 multiplies) | 3.0 |
| Vertical component: `kickSpeed × sin` (1 multiply) | 1.0 |
| Assemble Vector3 velocity | 1.0 |
| **Stage [9] total** | **18.0** |

**Note on trig:** Two trig function calls (Sin, Cos) are required here. These are
the only trig calls in the pipeline that operate on the actual output velocity — they
are not redundant with any other stage. See §6.3 for trig cost evaluation and the
rationale for retaining runtime trig (same conclusion as Pass Mechanics §6.4).

---

#### Stage [10] — NaN / Bounds Guard (§2.7 FM-05)

Safety check on all three velocity components:

| Operation | Equiv-ops |
|---|---|
| float.IsNaN check × 3 | 1.5 |
| float.IsInfinity check × 3 | 1.5 |
| Bounds check: magnitude vs V_ABSOLUTE_MAX | 2.0 |
| Conditional clamp (recovery path rarely executed) | 1.0 |
| **Stage [10] total (normal path)** | **4.0** |

---

#### Stage [11] — Ball.ApplyKick() (Ball Physics §3.1.11.2)

External call. Cost owned by Ball Physics Spec #1. Shot Mechanics cost is the call
overhead only:

| Operation | Equiv-ops |
|---|---|
| Parameter marshalling (Vector3 velocity, Vector3 spin, int, float) | 2.0 |
| Ball Physics internal execution | *not counted here* |
| **Stage [11] total (Shot Mechanics cost only)** | **2.0** |

---

#### Stage [12] — ShotEventEmitter (§3.10)

Struct construction and ring buffer write:

| Operation | Equiv-ops |
|---|---|
| Populate ShotExecutedEvent struct (12 fields) | 6.0 |
| Populate ShotAnimationData stub (6 fields) | 3.0 |
| Ring buffer write (1 pointer increment + struct copy) | 3.0 |
| **Stage [12] total** | **12.0** |

---

#### Stage [13] — State Machine Transition (§3.9)

Minimal overhead:

| Operation | Equiv-ops |
|---|---|
| State enum write | 0.5 |
| Frame counter reset | 0.5 |
| Result struct finalise | 2.0 |
| **Stage [13] total** | **3.0** |

---

### 6.1.4 Per-Evaluation Summary

| Stage | Sub-System | Equiv-ops (dominant foot) | Equiv-ops (weak foot) |
|---|---|---|---|
| [1] | Validation | 10.5 | 10.5 |
| [2] | Body Mechanics | 14.0 | 14.0 |
| [3] | Velocity | 25.5 | 25.5 |
| [4] | Launch Angle | 10.5 | 10.5 |
| [5] | Spin Vector | 16.0 | 16.0 |
| [6] | Weak Foot | 2.0 | 6.0 |
| [7] | Placement Resolution | 15.5 | 15.5 |
| [8] | Error Calculation | 34.5 | 34.5 |
| [9] | Velocity Vector3 | 18.0 | 18.0 |
| [10] | NaN guard | 4.0 | 4.0 |
| [11] | Ball.ApplyKick() call | 2.0 | 2.0 |
| [12] | Event emission | 12.0 | 12.0 |
| [13] | State transition | 3.0 | 3.0 |
| **Total** | | **167.5** | **171.5** |

**Typical evaluation: ~167–172 equiv-ops.**

This count is comparable to the Pass Mechanics direct-pass path (183.5 equiv-ops per
Pass Mechanics §6.1.3). Shot Mechanics is slightly cheaper in aggregate because it has
no O(n) pressure query (Pass Mechanics Phase 7 added ~20 ops for n=2 opponents).
Shot Mechanics folds pressure into a single pre-computed scalar read.

---

### 6.1.5 Comparison to Pass Mechanics

| Metric | Shot Mechanics | Pass Mechanics | Notes |
|---|---|---|---|
| Total equiv-ops (typical) | ~168 | ~184 | Shot cheaper due to no spatial query |
| Most expensive stage | Error Calc [8] (34.5) | Launch Angle Phase 5 (varies) | Different pipeline shapes |
| Trig calls | 2 (Sin, Cos in [9]) | 2 (Sin, Cos in Phase 5) | Equivalent |
| Spatial query | None | O(n) in Phase 7 | Key difference |
| Events per match | 20–40 | 800–1,200 | Shots 20× rarer |
| Total match cost | ~6,720–13,760 equiv-ops | ~147,200–220,800 equiv-ops | Shots are ~20× cheaper overall |

**Shot Mechanics is approximately 16–20× less expensive than Pass Mechanics over a
full match,** almost entirely because shots are far less frequent. Per-execution cost
is similar.

---

### 6.1.6 Simultaneous Shot Analysis (Worst-Case Bounding)

**Simultaneous shots are architecturally impossible in Stage 0.** Only one agent can
have ball possession at any time (Ball Physics §3.1.11.1 `PossessingAgentId`). A
`ShotRequest` requires possession. Therefore, two CONTACT evaluations on the same
frame cannot occur.

**For completeness and forward planning:** If ball possession were relaxed (e.g.,
contested possession in a future stage) and two CONTACT evaluations occurred
simultaneously:

| Scenario | Max CONTACT executions | Total equiv-ops | Estimated time |
|---|---|---|---|
| Stage 0 (normal) | 1 | ~168 | < 0.01ms |
| Hypothetical 2-agent simultaneous | 2 | ~336 | < 0.02ms |
| Theoretical maximum (all 22 agents — physically impossible) | 22 | ~3,696 | < 0.20ms |

Even the physically impossible 22-agent maximum is well within any frame budget.
No mitigation is required.

---

## 6.2 Performance Budget

### 6.2.1 Frame Budget Derivation

Frame budget at 60Hz: **16.67ms per frame.**

Budget allocation from Ball Physics Spec #1 §6.2.1:

| System | Budget | Status |
|---|---|---|
| Ball Physics (per frame, all balls) | 2.0ms | Allocated |
| Agent Movement (per frame, 22 agents) | 3.0ms | Allocated |
| Collision System (per frame) | 4.0ms | Allocated |
| Pass Mechanics (per frame amortised) | < 0.003ms | Allocated |
| Shot Mechanics (per frame amortised) | **< 0.001ms** | Allocated here |
| Remaining (GK, AI, rendering) | ~7.6ms | Available |

**Shot Mechanics amortised per-frame cost:**

```
Match total: 40 shots × 0.01ms/shot = 0.40ms across 90 minutes
90 minutes × 60 Hz = 324,000 frames
Per-frame amortised: 0.40ms / 324,000 = 0.0000012ms
```

This is unmeasurably small. **Shot Mechanics has zero meaningful impact on the
per-frame budget.** The relevant performance constraint is the **per-evaluation
latency target**, not the per-frame budget.

**Per-evaluation performance target:**

| Metric | Target | Classification |
|---|---|---|
| p50 CONTACT evaluation | < 0.010ms | Comfort |
| p95 CONTACT evaluation | < 0.050ms | **Primary gate** |
| p99 CONTACT evaluation | < 0.100ms | Extreme tail — no spikes |
| Zero GC allocations per evaluation | 0 bytes | Mandatory |

**Target rationale:** The p95 < 0.050ms target is borrowed directly from Pass Mechanics
§6.3.2, which established this as the discrete-event threshold comfortable within the
remaining frame budget even in a theoretically impossible simultaneous execution scenario.
Shot Mechanics at ~168 equiv-ops is slightly cheaper than Pass Mechanics (~184 equiv-ops),
so the same target applies with margin.

---

### 6.2.2 Profiling Markers

Four Unity Profiler markers are defined for Shot Mechanics. Names must match exactly
between this specification and the implementation. Any deviation requires a spec
amendment before the implementation proceeds.

| Marker Name | Scope | Primary Metric | Purpose |
|---|---|---|---|
| `ShotMech.Contact` | Entire CONTACT evaluation (stages 1–13) | p95 < 0.050ms — primary gate | Master budget check |
| `ShotMech.VelocityCalc` | Stage [3] VelocityCalculator only | Isolates sigmoid cost | Validates sigmoid not regressing |
| `ShotMech.ErrorCalc` | Stage [8] ErrorCalculator only | Isolates error application cost | Validates hash + double-normalise not dominating |
| `ShotMech.LaunchAngleTrig` | Stage [9] trig in velocity construction only | Validates trig decision (§6.3) | If > 10% of ShotMech.Contact at p95, LUT option must be reconsidered |

---

### 6.2.3 Per-Function Timing Targets

| Function | Equiv-ops | Estimated Time | Target (p95) | Pass/Fail Criterion |
|---|---|---|---|---|
| `ShotValidator.Validate()` | 10.5 | < 0.001ms | < 0.005ms | Not primary concern |
| `BodyMechanicsEvaluator.Evaluate()` | 14.0 | < 0.001ms | < 0.005ms | Not primary concern |
| `VelocityCalculator.Calculate()` | 25.5 | ~0.002ms | < 0.010ms | Sigmoid regression check |
| `LaunchAngleCalculator.Calculate()` | 10.5 | < 0.001ms | < 0.005ms | Not primary concern |
| `SpinVectorCalculator.Calculate()` | 16.0 | ~0.001ms | < 0.005ms | Not primary concern |
| `WeakFootPenaltyApplier.Apply()` | 2–6 | < 0.001ms | < 0.002ms | Not primary concern |
| `PlacementResolver.Resolve()` | 15.5 | ~0.001ms | < 0.005ms | Not primary concern |
| `ErrorCalculator.Calculate()` | 34.5 | ~0.003ms | < 0.012ms | Double-normalise regression check |
| `ShotExecutor.BuildVelocityVector()` | 18.0 | ~0.002ms | < 0.008ms | Trig regression check |
| **`ShotExecutor.ExecuteContact()` (full)** | ~168 | ~0.010ms | **< 0.050ms** | **Primary gate** |

---

### 6.2.4 Measurement Protocol

Measurement methodology is inherited from Pass Mechanics §6.3.4 and Ball Physics
§6.2.3:

1. **Profiling run:** 100 simulated matches at 60Hz using a fixed match seed.
2. **Shot sampling:** Record timing for every CONTACT execution across all 100 matches
   (~2,000–4,000 evaluations).
3. **Percentile reporting:** p50, p95, p99 of `ShotMech.Contact` duration.
4. **Platform:** Windows desktop (Intel Core i7-class or equivalent). ARM and WebGL
   results noted separately (see §6.8 KL-2).
5. **Build configuration:** Unity Burst-compiled release build. Do not profile in
   editor or development build; results are unrepresentative.
6. **Pass/fail:** p95 < 0.050ms. Failure requires root-cause analysis before
   milestone sign-off. A single p99 spike above 0.100ms triggers investigation
   (not automatic failure — could be OS scheduler jitter).

---

## 6.3 Trig Cost Evaluation

**Verdict: Runtime trig retained. No lookup table (LUT) at Stage 0.**

This section mirrors Pass Mechanics §6.4, which reached the same conclusion. The
argument is restated here for Shot Mechanics completeness.

**Trig calls in Shot Mechanics:** Two — `Sin(launchAngle)` and `Cos(launchAngle)` in
Stage [9]. Both use the same angle, so in principle they could be batched. All other
angular operations (§3.7 body mechanics penalties, §3.6 error rotation) operate on
pre-computed scalars and do not require additional trig.

**Cost estimate per evaluation:**
- 2 trig calls × 5 equiv-ops = 10 equiv-ops
- At ~0.001ms per equiv-op: ~0.01ms total trig cost per shot
- 40 shots per match: ~0.04ms total trig across entire match

**LUT option considered:**
A lookup table for Sin/Cos at 1° resolution (360 entries × 4 bytes = 1,440 bytes)
would reduce trig cost to ~2 equiv-ops per call (1 index computation + 1 array read).
Savings: 8 equiv-ops per shot × 40 shots = 320 equiv-ops per match ≈ 0.032ms/match.

**Decision: LUT rejected for Stage 0.** Reasons:

1. **Savings are unmeasurable.** 0.032ms over a 90-minute match (324,000 frames) is
   0.0001ms per frame — below the precision of any profiling tool.
2. **Accuracy risk.** 1° resolution introduces ≤ 0.02rad error. At 20m distance,
   this is ≤ 0.4m lateral deviation — detectable in a determinism regression test,
   potentially causing inconsistency with Appendix B hand-calculated verification cases.
3. **Determinism risk.** LUT indexing introduces floating-point truncation at the
   degree boundary; runtime Sin/Cos is deterministic across platforms using the same
   compiler and instruction set.
4. **No observed need.** Profiler marker `ShotMech.LaunchAngleTrig` will confirm the
   measured cost at runtime. If it exceeds 10% of `ShotMech.Contact` at p95, the LUT
   option must be revisited.

**Action:** Retain runtime trig. Validate with `ShotMech.LaunchAngleTrig` marker.

---

## 6.4 Memory Footprint

### 6.4.1 Per-Evaluation Stack Usage (ShotContext)

All Shot Mechanics structs are value types — no heap allocation, no GC pressure.
Stack usage per CONTACT evaluation:

| Struct | Fields | Estimated Size | Notes |
|---|---|---|---|
| `ShotRequest` | 8 fields (int×3, float×4, ContactZone enum×1, bool×1, Vector2×1) | ~40 bytes | Caller-allocated; passed by value to ShotExecutor |
| `ShotResult` | 10 fields (Vector3×3, Vector2×2, float×3, int×1, ShotOutcome enum×1) | ~72 bytes | Stack-allocated in ShotExecutor |
| `ShotContext` (internal) | ~12 intermediate floats + 2 Vector3 intermediates | ~72 bytes | Internal computation context; not exposed |
| `ShotExecutedEvent` | 12 fields (Vector3×3, Vector2×1, float×5, int×2, ContactZone×1, bool×1) | ~80 bytes | Struct copy to ring buffer at Stage [12] |
| `ShotAnimationData` | 6 fields (int×1, ContactZone×1, float×3, bool×1, int×1) | ~24 bytes | Stub; populated but unconsumed |
| **Total per evaluation** | | **~288 bytes** | Well within any stack limit |

**Comparison to Pass Mechanics:** Pass Mechanics §6.5.1 reported ~152 bytes. Shot
Mechanics is larger (~288 bytes) due to the additional ShotAnimationData stub struct
and the larger ShotResult (which exposes more diagnostics fields). Both are trivial.

---

### 6.4.2 Static (Persistent) Memory

| Data | Size | Lifetime |
|---|---|---|
| `ShotConstants` block (all [GT] scalars) | ~300 bytes | Application lifetime |
| `ContactZoneModifier` table (3 entries × 4 bytes) | 12 bytes | Application lifetime |
| `BaseAngle` table (3 entries × 4 bytes) | 12 bytes | Application lifetime |
| `SpinBase` table (3 entries × 12 bytes for Vector3) | 36 bytes | Application lifetime |
| `GoalGeometryProvider` cache | ~48 bytes | Application lifetime |
| **Total static** | **~408 bytes** | Negligible |

---

### 6.4.3 Cache-Line Analysis

Cache lines on modern x86-64: 64 bytes.

| Struct | Size | Cache lines spanned | Risk |
|---|---|---|---|
| `ShotRequest` | ~40 bytes | 1 | None — fits in 1 line |
| `ShotResult` | ~72 bytes | 2 | Minor: spans 2 lines. Not a hotspot at discrete-event frequency. |
| `ShotExecutedEvent` | ~80 bytes | 2 | Minor: spans 2 lines. Single struct copy to ring buffer — not a loop. |
| `ShotContext` (internal) | ~72 bytes | 2 | Minor. Internal only; compiler may optimise register usage. |

**Cache-line splitting assessment: Acceptable.** Unlike per-frame systems (Agent
Movement, Ball Physics), Shot Mechanics is not called in a tight loop over agents.
Struct sizes that span two cache lines are a concern only in SIMD batch operations.
No padding is required at Stage 0. If profiling reveals a cache miss rate that
meaningfully contributes to p95 variance, padding `ShotResult` to 128 bytes is a P2
optimisation.

---

## 6.5 Cache Behaviour

**Hot data on CONTACT evaluation frame:**

| Data | Prior Cache State | Reason |
|---|---|---|
| `AgentState` of shooter | **Warm** | Agent Movement updated this agent this frame (Step 3 precedes Shot Mechanics Step 6) |
| `PlayerAttributes` of shooter | **Warm** | Agent Movement reads attributes each frame; same struct |
| `AgentPhysicalProperties` of shooter | **Warm** | Frozen at INITIATING — typically 7–17 frames prior, still likely in L2 |
| `BallState.PossessingAgentId` | **Warm** | Ball Physics updates BallState each frame (Step 4) |
| `ShotConstants` block | **Warm after first shot** | Static block fits entirely in L2/L3 cache; persists for entire match |
| `ContactZoneModifier` table | **Warm after first shot** | 12 bytes; always in L1 after first access |
| `GoalGeometryProvider` cache | **Warm** | Static; never evicted in practice |
| Ring buffer write target | **Likely warm** | Ring buffer is typically < 1KB; fits in L1 |

**Cold data risk:** None in the normal execution path. Unlike Pass Mechanics, Shot
Mechanics performs no spatial query that would touch cold opponent agent structs. All
data accessed is either shooter-local (warm from Agent Movement), static tables (warm
after first match minute), or the ring buffer (small, warm).

**Net cache assessment: Excellent.** Shot Mechanics is better-behaved in cache terms
than Pass Mechanics, which risked cold misses on opponent data in the pressure query.

---

## 6.6 Optimization Roadmap

**P0 — Mandatory (before any implementation proceeds):**

| Optimisation | Rationale |
|---|---|
| Zero GC allocations in CONTACT execution | Guaranteed by all-struct design. Any `new` call in the hot path is a specification violation. Verify with Unity Profiler GC alloc column on first test run. |
| All [GT] constants in `ShotConstants.cs` only | No magic numbers in calculator classes. Enforcement: code review gate. |
| Stateless calculator classes | No mutable instance or static fields on any of the 7 calculator classes. Verified by §4 statelessness rule. |

**P1 — Conditional (if p95 > 0.030ms during profiling):**

| Optimisation | Potential Saving | Risk |
|---|---|---|
| Merge Stage [7] and Stage [8] vector normalisations into a single operation | ~7.5 equiv-ops | Low — requires careful refactor; no logic change |
| Combine Sin/Cos in Stage [9] into a single `SinCos()` call if Unity Burst exposes it | ~2–3 equiv-ops | Low — compiler may already optimise this |
| Inline `WeakFootPenaltyApplier` into `VelocityCalculator` for dominant-foot case | ~0.5 equiv-ops | Low — micro-optimisation; reduces call overhead |

**P2 — Post-Stage 0 (if ARM or WebGL profiling reveals issue):**

| Optimisation | Rationale |
|---|---|
| LUT for Sin/Cos if `ShotMech.LaunchAngleTrig` > 10% of `ShotMech.Contact` on ARM | Accepted accuracy trade-off; requires determinism regression test to pass with LUT values |
| Pad `ShotResult` to 128 bytes if cache splitting confirmed as p95 contributor | Profile-driven; not expected to be necessary |

**P3 — Stage 1+ only:**

| Optimisation | Rationale |
|---|---|
| SIMD batch processing if Stage 1 introduces simultaneous multi-ball scenarios | Not applicable at Stage 0 (single-ball, single-possession model) |
| Fixed64 migration (see §6.9) | Determinism hardening; deferred to Spec #9 |

---

## 6.7 Anti-Pattern Checklist

The following anti-patterns are explicitly prohibited. Any occurrence constitutes a
specification violation and must be corrected before code review approval:

| Anti-Pattern | Consequence | Detection |
|---|---|---|
| `new` inside any calculator class during CONTACT | GC allocation; breaks zero-alloc requirement | Unity Profiler GC alloc column |
| `List<T>`, `Dictionary<T, V>`, or any managed collection in hot path | GC pressure + unpredictable allocation | Code review |
| Mutable static field on any calculator class | Breaks determinism; breaks thread safety | Code review + IT-012 determinism test |
| Magic number in calculator class (value not in ShotConstants.cs) | Untuneable; obscures design intent | Code review |
| Calling `Ball.ApplyKick()` more than once per CONTACT state | Duplicate kick; simulation divergence | IT-012 + ShotEventEmitter test |
| Publishing `ShotExecutedEvent` for CANCELLED shots | Incorrect GK response; corrupted statistics | IT-005 test |
| `float.IsNaN()` check skipped in Stage [10] | NaN propagation into Ball Physics; match abort | EC-008 test |
| Trig call added to WINDUP frame loop | Per-frame overhead where none is specified | Code review + profiler |

---

## 6.8 Known Limitations

### KL-1: Op Count Based on Burst-Compiled Estimates

All equiv-op counts assume Unity Burst compilation with x86-64 SSE/AVX intrinsics
available. Uncompiled C# (Unity editor, non-Burst builds) may be 5–10× slower.

**Action:** Profile only in Burst-compiled release builds. Do not use editor profiling
results for performance gate decisions.

---

### KL-2: ARM / WebGL Not Profiled

Analysis targets desktop x86-64. On ARM (Apple Silicon, mobile) or WebGL (no SIMD,
single-threaded), trig cost may be 2–4× higher, moving the p95 target closer to the
0.050ms budget limit.

**Action:** Profile on each target platform before Stage 1. Document platform-specific
adjustments. If ARM trig cost causes p95 > 0.030ms, apply P2 LUT optimisation.

---

### KL-3: Event Ring Buffer Not Specified in This Specification

Stage [12] writes to a ring buffer. Buffer size and overflow behaviour are owned by
Event System Spec #17 (Stage 1). At Stage 0, a simple array of 64 `ShotExecutedEvent`
structs serves as the stub. With ≤ 40 shots per match and immediate event flush at
Step 8 (§2.3 frame pipeline), overflow is impossible.

**Action:** When Event System Spec #17 is written, confirm ring buffer capacity ≥ 64
elements or adjust Stage 0 stub. No blocking issue.

---

### KL-4: Sigmoid Exp() on Embedded / Console Targets

`Mathf.Exp()` cost varies significantly across platforms. On embedded targets
(Nintendo Switch, last-gen consoles), software floating-point Exp() can be 5–10×
slower than desktop hardware.

**Action:** If console targeting is confirmed for Stage 1+, evaluate replacing the
sigmoid with a piecewise linear approximation. Accuracy trade-off is acceptable given
[GT] classification of D_MID and D_SCALE — the blend shape is tunable regardless.

---

### KL-5: Op Count Excludes Ball Physics Internal Cost

Stage [11] counts only the Shot Mechanics call overhead (2.0 equiv-ops). The Ball
Physics internal cost of `Ball.ApplyKick()` is non-trivial (velocity assignment +
spin state update + possession handoff). This cost is owned and budgeted in Ball
Physics Spec #1 §6 and is not Double-counted here.

**Action:** None — correct by design. If cross-system profiling reveals the CONTACT
frame is expensive, `ShotMech.Contact` and Ball Physics markers should be compared
in the same profiling session.

---

## 6.9 Fixed64 Migration Notes

All Stage 0 specifications use `float` (IEEE 754 single-precision) arithmetic.
Fixed-point migration is deferred to Fixed64 Math Library Spec #9. The migration
path is documented here for the Shot Mechanics implementation.

**Affected operations in Shot Mechanics:**

| Operation | Floating-Point Form | Fixed64 Form | Migration Risk |
|---|---|---|---|
| Sigmoid in §3.2 | `Mathf.Exp()` | Fixed64 piecewise Exp() or LUT | Medium — Exp() approximation must match Appendix B verification values |
| Sin/Cos in Stage [9] | `Mathf.Sin/Cos()` | Fixed64.Sin/Cos() | Low — sin/cos are well-defined; determinism test covers this |
| Vector3 normalise in §3.5, §3.6 | `Mathf.Sqrt()` | Fixed64.Sqrt() | Low — standard implementation |
| Hash function in §3.6 | Integer ops | Integer ops (unchanged) | None — already integer |
| Attribute normalisation (1–20 scale) | Float divide | Fixed64 divide | Low |

**Migration policy:** Float → Fixed64 replacement must pass IT-012 (determinism
regression test) before being accepted. Any Fixed64 implementation that causes a
bitwise output change vs. the float implementation requires Appendix B numerical
re-verification.

**Recommendation:** Migrate highest-risk operations first (sigmoid Exp(), then trig),
as these have the most approximation risk. Hash function requires no migration.

---

## 6.10 Cross-References

| Topic | Authoritative Section | Summary |
|---|---|---|
| Frame budget allocation | Ball Physics #1 §6.2.1 | 16.67ms/frame; Shot Mechanics allocated < 0.001ms amortised |
| Equiv-op definition | Agent Movement #2 §5.1 | 1 equiv-op ≈ 1 CPU multiply; trig = 5 equiv-ops |
| Discrete-event performance methodology | Pass Mechanics #5 §6 | Methodology reused; comparison to shots in §6.1.5 |
| Zero-allocation policy | Development Best Practices §4 | P0 mandatory anti-pattern enforcement |
| ShotRequest struct sizes | §2.4.1 (Section 2) | Source of memory footprint figures in §6.4.1 |
| ShotResult struct sizes | §2.4.2 (Section 2) | Source of memory footprint figures in §6.4.1 |
| Profiling marker implementation | §4.5 (Section 4) | Marker names must match exactly |
| Determinism regression test | §5.12 IT-012 (Section 5) | Pass criterion for Fixed64 migration |
| Performance acceptance criteria | §5.15.4 (Section 5) | PERF-001 through PERF-005 |
| Trig decision (LUT vs. runtime) | §6.3 (this section) | Runtime retained; `ShotMech.LaunchAngleTrig` validates |
| Fixed64 migration | Fixed64 Math Library Spec #9 | All Stage 0 float ops; migration deferred |
| Ring buffer size | Event System Spec #17 (Stage 1) | Stage 0 stub: 64-element array; confirmed adequate |

---

## 6.11 Section Summary

| Subsection | Key Finding |
|---|---|
| **6.1 Complexity** | O(1) per evaluation — no loops, no spatial queries |
| **6.1.4 Op Counts** | ~168 equiv-ops typical (dominant foot); ~172 (weak foot) |
| **6.1.5 vs Pass Mechanics** | ~8% cheaper per evaluation; ~16–20× cheaper per match due to shot rarity |
| **6.1.6 Simultaneous Shots** | Architecturally impossible at Stage 0; bounded at ~168 equiv-ops if relaxed |
| **6.2 Budget** | p95 < 0.050ms primary gate; amortised per-frame impact < 0.0000012ms |
| **6.3 Trig Decision** | **Runtime trig retained.** 0.032ms/match savings from LUT are unmeasurable. Risk: accuracy + determinism. |
| **6.4 Memory** | ~288 bytes stack per evaluation; ~408 bytes static. Zero GC pressure. |
| **6.5 Cache** | All hot data warm from Agent Movement and Ball Physics. No cold-read risk (unlike Pass Mechanics). |
| **6.6 Optimisations** | P0 mandatory (zero allocation, stateless calculators). P1–P2 conditional on profiling. P3 post-Stage 0. |
| **6.7 Anti-Patterns** | 8 prohibited patterns defined. Enforcement via code review + IT-012. |
| **6.8 Limitations** | 5 known. KL-2 (ARM trig) highest priority; KL-4 (console Exp()) if console target confirmed. |
| **6.9 Fixed64** | Migration deferred to Spec #9. Sigmoid Exp() and trig are medium-risk; hash requires no migration. |

---

## 6.12 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 23, 2026, 11:30 PM PST | Claude (AI) / Anton | Initial draft. Runtime trig confirmed. Op counts derived from §3.1–§3.10. Simultaneous shot analysis bounded. Fixed64 migration path documented. |
| 1.1 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Prerequisites clarified: Section 3 Part 1 v1.1, Part 2 v1.2. (2) Decision Tree #7→#8. |

---

*End of Section 6 — Shot Mechanics Specification #6*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*

*Next: Section 7 — Future Extensions*
