# Agent Movement Specification â€” Section 5: Performance Analysis

**Purpose:** Authoritative performance analysis for the Agent Movement System â€” computational complexity, memory budget, profiling targets, and optimization roadmap. All operation counts are derived line-by-line from the implementations in Sections 3.1â€“3.6, following the methodology established in Ball Physics Spec #1 Section 6.

**Created:** February 13, 2026, 11:30 PM PST  
**Updated:** March 4, 2026, 12:00 AM PST  
**Version:** 1.2  
**Status:** Draft  
**Dependencies:** Section 3.1 (State Machine v1.2), Section 3.2 (Locomotion v1.0), Section 3.3 (Directional Movement v1.0), Section 3.4 (Turning v1.0), Section 3.5 (Data Structures v1.4), Section 3.6 (Edge Cases v1.1), Ball Physics Spec #1 Section 6 (pattern reference, budget allocation)

---

## CHANGELOG v1.0 â†’ v1.1

**CRITICAL FIXES:**

1. **IDLE turning cost corrected (Issue #1):** v1.0 showed IDLE turning as 0 ops (assumed pipeline skip). Corrected: IDLE agents still execute `CalculateMaxTurnRate()` (returns 720Â°/s) and one `ApplyTurnRateLimit()` for facing rotation (TARGET_LOCK mode). IDLE equiv ops: 46 â†’ **87**. STUMBLING/GROUNDED also corrected from 0 â†’ **19 float ops** (function calls execute but produce zero output via modifier).

2. **WALKING directional efficiency note added (Issue #2):** Full directional pipeline executes for WALKING but result is nearly always 1.0 (forward zone). Flagged as "correct but wasteful" â€” feeds P1-C optimization justification.

3. **Safety validation branch count corrected (Issue #3):** 35 â†’ **33** branches. Detailed per-sub-function breakdown added (HasInvalidValues: 22, ApplySafetyClamps: 4, EnforcePitchBoundaries: 6, UpdateLastValidState: 1).

4. **OscillationGuard amortized cost added (Issue #4):** ~12 float ops + ~10 branches per state transition. Amortized to ~1.0 op/frame at 5 transitions/second. Explicitly noted as excluded from per-state table but required in profiling.

**MAJOR FIXES:**

5. **Agent struct memory count rebuilt from Section 3.5 v1.2 (Issue #5):** Replaced grouped estimates with field-by-field count matching authoritative Agent class definition. 220 â†’ **224 bytes** per agent. Includes class object header (16 bytes), PlayerAttributes (60 bytes for 15 int fields), and all fields from Section 3.5.1. Cross-reference note added for drift prevention.

6. **STUMBLING 3% estimate flagged as likely overestimate (Issue #6):** Added caveat explaining 3% implies ~162 seconds stumbling per agent per match (60â€“180 stumble events). Noted actual rate likely 1â€“1.5% pending AI behavior tuning.

7. **LOD determinism resolution path specified (Issue #7):** LOD tier assignment is deterministic (pure function of ball/agent positions). Added blend protocol (3â€“5 frame lerp) for tier transitions. Replay/multiplayer synchronization preserved.

8. **Budget utilization finding made prominent (Issue #8):** Added KEY FINDING callout: expected ~2â€“8% utilization of 3.0ms budget. Surplus (~2.75â€“2.95ms) available for reallocation. This should inform Spec #3 and #7 budget negotiations.

**MINOR FIXES:**

9. **GROUNDED integration cost corrected (Issue #9):** Was 0 ops in v1.0 table, corrected to 12 ops (Euler integration still executes; velocity is zero so position unchanged but multiplies still fire).

10. **P2-C Section 3.5 amendment requirement noted (Issue #10):** `StableFrameCount` field requires Section 3.5 amendment if optimization is needed.

11. **Footer next section corrected (Issue #11):** Changed from ambiguous "Section 4 or Appendix A" to "Section 4 (Implementation Details)" per confirmed writing order.

12. **Match-level totals updated:** All state equiv ops updated (IDLE: 46â†’87, STUMBLING: 51â†’70, GROUNDED: 39â†’70). Match total: 1.044B â†’ **1.096B** equiv ops. Safety branch totals: 35â†’33 throughout.

---

## CHANGELOG v1.1 -> v1.2 (March 4, 2026)

**MINOR FIXES:**

1. **Dependency version references updated:** Section 3.1 v1.0 -> v1.2, Section 3.5 v1.2 -> v1.4. Agent struct memory estimate note: PlayerAttributes increased from ~80 to ~84 bytes in v1.3, making per-agent total approximately 228 bytes (was 224). Within tolerance of 256-byte PR-2 budget.

## 5. PERFORMANCE ANALYSIS

### Preamble: Role of This Section

This section is the **single authoritative source** for all agent movement performance targets, budgets, and analysis. Performance figures stated in Sections 2.2 and the outline's Section 4 profiling targets are summaries derived from the analysis herein. If a conflict exists between those sections and this one, **this section takes precedence**.

**Critical distinction from Ball Physics:** Ball physics is a single-entity O(1) system costing ~172 ops per frame at worst. Agent movement is a **22-entity O(n) system** where n=22 (20 outfield + 2 goalkeepers). The per-agent cost is individually O(1) â€” no loops, no spatial queries â€” but the aggregate cost is 22Ã— per-agent. This makes agent movement the single most expensive physics system in Stage 0, consuming the largest slice of the frame budget.

---

### 5.1 Computational Complexity

#### 5.1.1 Per-Agent, Per-Frame Complexity Classification

The agent movement update is **O(1) per agent per frame**. Every code path through the movement pipeline executes a bounded, deterministic number of arithmetic operations regardless of match state, agent count, or simulation time. There are no loops over other agents, no spatial queries, and no variable-length iterations within the movement system itself. (Spatial queries occur in Collision System, Spec #3.)

The only conditional variation in per-frame cost comes from:

- **Movement state**: IDLE is cheapest (early exits in locomotion, directional, turning). SPRINTING is most expensive (full exponential acceleration, full turning pipeline, stumble risk evaluation). DECELERATING is second-most expensive (deceleration force calculation plus reduced turning pipeline).
- **Stumble risk zone**: When an agent's actual angular velocity exceeds the safe threshold (Section 3.4.4), an additional ~17 ops of probability evaluation execute. This is conditional on both speed (>2.2 m/s) and turn intensity, so it fires on a minority of frames.
- **OscillationGuard activation**: On state transitions, the ring buffer check adds ~18 ops. Transitions are infrequent (~1â€“5 per second per agent, not 60).
- **10Hz heartbeat frames**: Every 6th frame, PerformanceContext cache invalidation triggers modifier recomputation (~20 additional ops across 4 attribute evaluations).

None of these variations change the asymptotic complexity â€” they represent constant-factor differences within the O(1) bound.

#### 5.1.2 Operation Counts by Pipeline Step

Each frame, the movement pipeline executes the steps defined in Section 4.4. The following table counts floating-point operations (add, subtract, multiply, divide), transcendental function calls (Exp, Sqrt, Atan2, Acos, Sin, Cos), and branch checks per pipeline step.

Counts are derived by reading each function in Sections 3.1â€“3.6 and counting every arithmetic operation. Transcendental calls are listed separately because their cost varies by hardware (1â€“15 ns per call on modern x86 FPUs; potentially 5â€“10Ã— more on software implementations).

**Pipeline Step 1: Receive MovementCommand**

Read struct fields. Negligible: ~2 assignments, ~1 branch (null/validity check).

**Pipeline Step 2: State Machine Evaluation (Section 3.1)**

`EvaluateState()` dispatches to per-state evaluators via switch. Worst case is the SPRINTING path with ShouldStumble invocation:

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Collision knockdown check | 0 | 0 | 2 |
| Switch dispatch (7-way) | 0 | 0 | 1 |
| EvaluateFromSprinting: reservoir check | 0 | 0 | 1 |
| Speed < SPRINT_EXIT | 0 | 0 | 1 |
| turnAngle > STUMBLE_TURN_ANGLE | 0 | 0 | 1 |
| speed > STUMBLE_SPEED_THRESHOLD | 0 | 0 | 1 |
| ShouldStumble: speed/MAX_SPEED | 1 div | 0 | 0 |
| ShouldStumble: turnAngle/180 | 1 div | 0 | 0 |
| ShouldStumble: Ã— difficulty Ã— prev | 2 mul | 0 | 0 |
| ShouldStumble: Max(risk, MIN) | 0 | 0 | 1 |
| ShouldStumble: (agi+bal)/40 | 1 add + 1 div | 0 | 0 |
| ShouldStumble: risk > resistance | 0 | 0 | 1 |
| commandSpeed < speed - 0.5 | 1 sub | 0 | 1 |
| **Subtotal (SPRINTING worst)** | **~9** | **0** | **~10** |

Average case (JOGGING, no stumble path): ~3 float ops, ~6 branches.
Cheapest case (IDLE): 0 float ops, ~3 branches.

**Conditional: OscillationGuard check (on state transitions only):**

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Write timestamp to ring buffer | 1 (assignment) | 0 | 0 |
| Advance write index (modulo) | 2 (add + mod) | 0 | 0 |
| Count recent transitions (loop 8Ã—) | 8 sub | 0 | 8 compare |
| Threshold compare | 0 | 0 | 1 |
| Lock check (if already locked) | 1 sub | 0 | 1 |
| **Subtotal per transition** | **~12** | **0** | **~10** |

State transitions occur ~1â€“5 times per second per agent, not every frame. At worst case 5 transitions/second on a 60Hz loop: 5/60 = 0.083 probability per frame. **Amortized cost: ~1.0 float op + ~0.8 branches per frame.** This is negligible and excluded from the per-state totals table to avoid false precision, but must be accounted for in profiling.

**Pipeline Step 3: PerformanceContext Attribute Evaluation (Section 3.2.1)**

Four attributes are evaluated per frame: Pace, Acceleration, Agility, Balance. At 60Hz with 10Hz caching, the modifier product (Form Ã— Context Ã— Career) is pre-computed and cached. Each `EvaluateAttribute()` call at 60Hz (cached path):

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Read cached modifier | 0 | 0 | 0 |
| attribute Ã— modifier | 1 mul | 0 | 0 |
| Clamp(floor, ceiling) | 0 | 0 | 2 |
| **Per-call total** | **1** | **0** | **2** |

Four calls: 4 float ops, 8 branches.

Attribute mapping functions (called with effective attribute values):

| Function | Float Ops | Notes |
|---|---|---|
| MapPaceToTopSpeed | 3 (1 sub, 1 mul, 1 add) | Linear interpolation |
| MapAccelerationToK | 3 (1 sub, 1 mul, 1 add) | Linear interpolation |
| MapAgilityToDecel (if DECEL) | 3 (1 sub, 1 mul, 1 add) | Only for DECELERATING state |

| **Subtotal (attribute pipeline)** | **~10â€“13** | **0 transcendentals** | **~8 branches** |

**10Hz heartbeat overhead (every 6th frame):** Cache invalidation forces recomputation of the modifier product. Each `EvaluateAttribute()` call costs 3 multiplies + 1 Clamp instead of 1 multiply + 1 Clamp. Additional cost per 10Hz frame: 4 calls Ã— 2 extra multiplies = 8 additional float ops. Amortized across 6 frames: ~1.3 ops/frame. Negligible.

**Pipeline Step 4: Locomotion Speed Update (Section 3.2)**

Switch on state, then apply the appropriate acceleration/deceleration model.

JOGGING/SPRINTING path (ApplyExponentialAcceleration â€” worst case):

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| k Ã— dt | 1 mul | 0 | 0 |
| Negate (-kÃ—dt) | 1 neg | 0 | 0 |
| Exp(-kÃ—dt) | 0 | 1 Exp | 0 |
| currentSpeed âˆ’ targetSpeed | 1 sub | 0 | 0 |
| Ã— decay | 1 mul | 0 | 0 |
| targetSpeed + result | 1 add | 0 | 0 |
| Abs(newSpeed âˆ’ targetSpeed) | 1 sub + 1 abs | 0 | 0 |
| < MIN_SPEED snap check | 0 | 0 | 1 |
| Clamp(0, MAX_SPEED) | 0 | 0 | 2 |
| Switch dispatch | 0 | 0 | 1 |
| **Subtotal (exponential)** | **~8** | **1 Exp** | **~4** |

WALKING path (ApplyLinearAcceleration): ~4 float ops, 0 transcendentals, ~4 branches.
IDLE path (minimal drag): ~3 float ops, 0 transcendentals, ~1 branch.
DECELERATING path: ~7 float ops (includes attribute-mapped decel rate), 0 transcendentals, ~3 branches.
STUMBLING path (friction drag): ~3 float ops, 0 transcendentals, ~1 branch.

**Pipeline Step 5: Directional Movement (Section 3.3)**

`GetMovementAngle()` + `CalculateDirectionalMultiplier()`:

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Speed < MIN_SPEED guard | 0 | 0 | 1 |
| Vector2 normalize (moveDir) | 4 (2 mul, 1 add, 1 div) | 1 Sqrt | 0 |
| Dot product (2D) | 3 (2 mul, 1 add) | 0 | 0 |
| Clamp dot to [-1, 1] | 0 | 0 | 2 |
| Acos(dot) | 0 | 1 Acos | 0 |
| Ã— Rad2Deg | 1 mul | 0 | 0 |
| Clamp angle [0, 180] | 0 | 0 | 2 |
| lateralMult (1 sub, 1 mul, 1 add) | 3 | 0 | 0 |
| Clamp lateralMult | 0 | 0 | 2 |
| backwardMult (1 sub, 1 mul, 1 add) | 3 | 0 | 0 |
| Clamp backwardMult | 0 | 0 | 2 |
| Zone classification (4 if-checks worst) | 0 | 0 | 4 |
| Lerp in transition zone (worst) | 3 (1 sub, 1 div, 1 lerp) | 0 | 0 |
| **Subtotal (directional)** | **~17** | **1 Sqrt + 1 Acos** | **~13** |

Not active for IDLE, STUMBLING, or GROUNDED (early exit at speed guard): ~0 float ops, 1 branch.

**WALKING efficiency note:** The full directional pipeline (~17 float ops + Sqrt + Acos) executes for WALKING, but in practice most walking movement is forward-facing (0Â°â€“30Â° zone), so the result is nearly always 1.0. The Acos computation and zone classification are "correct but wasteful" at walking speed â€” a P1 optimization candidate (see 5.5.2 P1-C: dot-product zone check to bypass Acos).

**Pipeline Step 6: Turning (Section 3.4)**

Three sub-functions execute: `CalculateMaxTurnRate()`, `ApplyTurnRateLimit()` (called twice â€” once for path direction, once for facing direction), and `CalculateLeanAngle()`. Plus conditional `EvaluateStumbleRisk()`.

`CalculateMaxTurnRate()`:

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| kTurn mapping (1 sub, 1 mul, 1 sub) | 3 | 0 | 0 |
| Clamp kTurn | 0 | 0 | 2 |
| baseTurnRate = BASE / (1 + kÃ—v) | 3 (1 mul, 1 add, 1 div) | 0 | 0 |
| balanceMod mapping (1 sub, 1 mul, 1 add) | 3 | 0 | 0 |
| Clamp balanceMod | 0 | 0 | 2 |
| turnRate Ã— balanceMod | 1 mul | 0 | 0 |
| State switch â†’ stateMod | 0 | 0 | 1 |
| turnRate Ã— stateMod | 1 mul | 0 | 0 |
| Clamp [FLOOR, CAP] | 0 | 0 | 2 |
| **Subtotal** | **~11** | **0** | **~7** |

`ApplyTurnRateLimit()` (per call â€” called twice):

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| SignedAngleDeg: cross (2 mul, 1 sub) | 3 | 0 | 0 |
| SignedAngleDeg: dot (2 mul, 1 add) | 3 | 0 | 0 |
| Atan2 | 0 | 1 Atan2 | 0 |
| Ã— Rad2Deg | 1 mul | 0 | 0 |
| maxAngle = turnRate Ã— dt | 1 mul | 0 | 0 |
| Abs(angleDelta) comparison | 1 abs | 0 | 1 |
| **If within budget**: normalize return | 4 | 1 Sqrt | 0 |
| **If clamped**: Sign + multiply | 2 | 0 | 0 |
| RotateByDeg: Deg2Rad multiply | 1 mul | 0 | 0 |
| RotateByDeg: Cos, Sin | 0 | 1 Cos + 1 Sin | 0 |
| RotateByDeg: 4 mul, 2 add/sub | 6 | 0 | 0 |
| Normalize result | 4 | 1 Sqrt | 0 |
| **Subtotal (clamped path â€” typical)** | **~22** | **1 Atan2 + 1 Cos + 1 Sin + 1 Sqrt** | **~1** |
| **Subtotal (within-budget path)** | **~13** | **1 Atan2 + 1 Sqrt** | **~1** |

Two calls (path direction + facing direction): Double the above. Typical worst case assumes one clamped, one within-budget.

`EvaluateStumbleRisk()` (conditional â€” most frames exit early):

| Path | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Early exit (speed < threshold) | 0 | 0 | 1 |
| Early exit (angular velocity < 1) | 0 | 0 | 1 |
| Early exit (within safe zone) | 5 (safeFraction map + multiply) | 0 | 3 |
| Full evaluation (risk zone) | 13 (safe calc + risk calc + prob calc) | 0 | 5 |
| RNG call | ~3 | 0 | 1 |
| **Typical (early exit at speed)** | **0** | **0** | **~1** |
| **Worst (full risk evaluation)** | **~16** | **0** | **~6** |

`CalculateLeanAngle()`:

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Abs + dead zone check | 1 abs | 0 | 1 |
| Deg2Rad conversion | 1 mul | 0 | 0 |
| Centripetal accel (speed Ã— abs(Ï‰)) | 2 (1 mul, 1 abs) | 0 | 0 |
| Atan2(accel, gravity) | 0 | 1 Atan2 | 0 |
| Ã— Rad2Deg | 1 mul | 0 | 0 |
| Min(lean, MAX) | 0 | 0 | 1 |
| Sign Ã— lean | 2 (1 sign, 1 mul) | 0 | 0 |
| **Subtotal** | **~7** | **1 Atan2** | **~2** |

**Combined Turning (typical frame, SPRINTING state, no stumble trigger):**

| Component | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| CalculateMaxTurnRate | 11 | 0 | 7 |
| ApplyTurnRateLimit Ã— 2 | 35 | 2 Atan2 + 1 Cos + 1 Sin + 2 Sqrt | 2 |
| EvaluateStumbleRisk (early exit) | 0 | 0 | 1 |
| CalculateLeanAngle | 7 | 1 Atan2 | 2 |
| **Subtotal** | **~53** | **3 Atan2 + 1 Cos + 1 Sin + 2 Sqrt** | **~12** |

IDLE: `CalculateMaxTurnRate()` still executes (returns 720Â°/s), and `ApplyTurnRateLimit()` executes once for facing rotation (TARGET_LOCK mode tracking ball while stationary is common). Path direction rotation is skipped (no velocity). Total IDLE turning: ~11 (MaxTurnRate) + ~13 (one ApplyTurnRateLimit, within-budget path) + ~7 (LeanAngle, exits at dead zone) = **~31 float ops + 1 Atan2 + 1 Sqrt â‰ˆ ~41 equiv ops**. GROUNDED and STUMBLING: turn rate modifier is 0.0, so ApplyTurnRateLimit produces zero rotation â€” but the function calls still execute (early exits after computing maxAngle = 0): ~11 + ~4 + ~4 = **~19 float ops, ~1 branch each**.

**Pipeline Step 7â€“8: Velocity and Position Integration (Euler)**

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| v += a Ã— dt (Vector3: 3 mul + 3 add) | 6 | 0 | 0 |
| p += v Ã— dt (Vector3: 3 mul + 3 add) | 6 | 0 | 0 |
| **Subtotal** | **~12** | **0** | **0** |

Constant across all states. This is the cheapest step.

**Pipeline Step 9: Safety Validation (Section 3.6)**

`HasInvalidValues()` + `ApplySafetyClamps()` + `EnforcePitchBoundaries()`:

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| HasInvalidValues: 11 fields Ã— 2 checks (NaN + Inf) | 0 | 0 | 22 |
| ApplySafetyClamps: speed > MAX_SPEED | 0 | 0 | 1 |
| ApplySafetyClamps: speed < MIN_VELOCITY | 0 | 0 | 1 |
| ApplySafetyClamps: TurnRate Clamp (1 compare) | 0 | 0 | 1 |
| ApplySafetyClamps: LeanAngle Clamp (1 compare) | 0 | 0 | 1 |
| EnforcePitchBoundaries: x min, x max | 0 | 0 | 2 |
| EnforcePitchBoundaries: y min, y max | 0 | 0 | 2 |
| EnforcePitchBoundaries: z min (ground), z max | 0 | 0 | 2 |
| UpdateLastValidState: assignment (no branch) | 0 | 0 | 1 |
| **Subtotal (typical â€” all valid)** | **~0** | **0** | **~33** |
| **Subtotal (clamping active)** | **~10** | **1 Sqrt (normalize)** | **~33** |

The 22 NaN/Infinity comparisons dominate this step. On modern CPUs with branch prediction, the "all valid" path (taken ~100% of the time) is near-zero cost because every branch is correctly predicted. Still, the comparisons are issued and counted.

**Pipeline Step 10: Cache Updates**

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Speed = Velocity.magnitude | 3 (mul) + 2 (add) | 1 Sqrt | 0 |
| PhysicalProperties struct populate | 6 (field writes) | 0 | 0 |
| AnimationData update | 4 (field writes) | 0 | 1 |
| **Subtotal** | **~15** | **1 Sqrt** | **~1** |

**Pipeline Step 11: Event Logger**

| Sub-operation | Float Ops | Transcendentals | Branches |
|---|---|---|---|
| Time since last snapshot | 1 sub | 0 | 0 |
| Interval check (usually early-exits) | 0 | 0 | 1 |
| **Subtotal (typical)** | **~1** | **0** | **~1** |

#### 5.1.3 Per-Agent Totals by Movement State

Aggregating pipeline steps for each movement state. Transcendental calls (Exp, Sqrt, Atan2, Acos, Sin, Cos) are counted separately and converted to equivalent float ops using the conservative estimate of 5 float-op-equivalents per transcendental call (accounts for hardware FPU latency on x86; would be higher on software implementations).

| Pipeline Step | IDLE | WALKING | JOGGING | SPRINTING | DECEL | STUMBLING | GROUNDED |
|---|---|---|---|---|---|---|---|
| State eval (float) | 0 | 2 | 3 | 9 | 6 | 5 | 8 |
| State eval (branch) | 3 | 4 | 6 | 10 | 8 | 4 | 3 |
| Attribute pipeline (float) | 10 | 10 | 10 | 10 | 13 | 10 | 10 |
| Attribute pipeline (branch) | 8 | 8 | 8 | 8 | 8 | 8 | 8 |
| Locomotion (float) | 3 | 4 | 8 | 8 | 7 | 3 | 0 |
| Locomotion (transcendental) | 0 | 0 | 1 Exp | 1 Exp | 0 | 0 | 0 |
| Locomotion (branch) | 1 | 4 | 4 | 4 | 3 | 1 | 0 |
| Directional (float) | 0 | 17 | 17 | 17 | 17 | 0 | 0 |
| Directional (transcendental) | 0 | 1 Sqrt + 1 Acos | 1 Sqrt + 1 Acos | 1 Sqrt + 1 Acos | 1 Sqrt + 1 Acos | 0 | 0 |
| Directional (branch) | 1 | 13 | 13 | 13 | 13 | 1 | 1 |
| Turning (float) | 31 | 53 | 53 | 53 | 53 | 19 | 19 |
| Turning (transcendental) | 1 Atan2 + 1 Sqrt | 3 Atan2 + 1 Cos + 1 Sin + 2 Sqrt | same | same | same | 0 | 0 |
| Turning (branch) | 3 | 12 | 12 | 12 | 12 | 2 | 2 |
| Integration (float) | 12 | 12 | 12 | 12 | 12 | 12 | 12 |
| Safety (branch) | 33 | 33 | 33 | 33 | 33 | 33 | 33 |
| Cache updates (float) | 15 | 15 | 15 | 15 | 15 | 15 | 15 |
| Cache updates (transcendental) | 1 Sqrt | 1 Sqrt | 1 Sqrt | 1 Sqrt | 1 Sqrt | 1 Sqrt | 1 Sqrt |
| Logger | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| **Total float ops** | **~72** | **~114** | **~119** | **~125** | **~123** | **~65** | **~65** |
| **Total transcendentals** | **2 Sqrt + 1 Atan2** | **4 Sqrt + 1 Acos + 3 Atan2 + 1 Cos + 1 Sin** | **same + 1 Exp** | **same + 1 Exp** | **4 Sqrt + 1 Acos + 3 Atan2 + 1 Cos + 1 Sin** | **1 Sqrt** | **1 Sqrt** |
| **Transcendental equiv (Ã—5)** | **15** | **50** | **55** | **55** | **50** | **5** | **5** |
| **Total equiv ops** | **~87** | **~164** | **~174** | **~180** | **~173** | **~70** | **~70** |
| **Total branches** | **~49** | **~89** | **~91** | **~95** | **~92** | **~50** | **~48** |

**Notes on IDLE turning cost:** IDLE agents still execute `CalculateMaxTurnRate()` (produces 720Â°/s) and one `ApplyTurnRateLimit()` call for facing rotation (TARGET_LOCK mode tracking ball while stationary is common). Path direction rotation is skipped because velocity is near-zero. This makes IDLE ~87 equiv ops rather than the ~46 that would result from skipping turning entirely. In the worst case (agent in IDLE with no TARGET_LOCK active), the facing rotation call early-exits when desired direction matches current, saving ~8 ops. The table uses the conservative TARGET_LOCK-active case.

**Notes on STUMBLING/GROUNDED turning cost:** The turn rate modifier is 0.0Ã— for both states, but `CalculateMaxTurnRate()` still executes before the modifier zeroes the result (11 ops). `ApplyTurnRateLimit()` receives maxAngle=0 and early-exits after computing the zero product (~4 ops per call Ã— 2 calls). `CalculateLeanAngle()` early-exits at the dead zone check. Total: ~19 float ops, no transcendentals.

**Worst-case per agent per frame: ~180 equivalent operations (SPRINTING).**

This is comparable in magnitude to Ball Physics' AIRBORNE worst case (~172 ops for a single ball) â€” but agent movement must execute this for 22 agents.

#### 5.1.4 Detailed Breakdown: SPRINTING (Worst Case)

SPRINTING activates the full pipeline: exponential acceleration, directional penalties, full turning mechanics, and stumble risk potential. Following the Ball Physics Section 6.1.3 pattern:

**Attribute Pipeline:**
- 4Ã— EvaluateAttribute (cached): 4 Ã— (1 mul + 1 clamp) = 4 mul + 8 comparisons
- MapPaceToTopSpeed: 1 sub + 1 mul + 1 add = 3 ops
- MapAccelerationToK: 1 sub + 1 mul + 1 add = 3 ops
- **Subtotal: ~10 float ops**

**State Machine (SPRINTING â†’ stumble check):**
- Collision check + switch + 4 threshold comparisons + ShouldStumble (3 div + 2 mul + 1 add + 2 comparisons) + decel check (1 sub + 1 comparison)
- **Subtotal: ~9 float ops, ~10 branches**

**Locomotion (Exponential):**
- 1 mul + 1 neg + 1 Exp + 1 sub + 1 mul + 1 add + 1 sub + 1 abs + 3 comparisons
- **Subtotal: ~8 float ops + 1 Exp, ~4 branches**

**Directional Movement:**
- GetMovementAngle: normalize (4 ops + 1 Sqrt) + dot (3 ops) + 2 clamps + 1 Acos + 1 mul = ~8 float + 1 Sqrt + 1 Acos
- CalculateDirectionalMultiplier: 2Ã— zone mapping (6 ops + 4 clamps) + zone selection (4 checks) + lerp (3 ops) = ~9 float
- **Subtotal: ~17 float ops + 1 Sqrt + 1 Acos, ~13 branches**

**Turning:**
- CalculateMaxTurnRate: 11 float, 7 branches
- ApplyTurnRateLimit (path): ~22 float + 1 Atan2 + 1 Cos + 1 Sin + 1 Sqrt, 1 branch
- ApplyTurnRateLimit (facing): ~13 float + 1 Atan2 + 1 Sqrt, 1 branch
- EvaluateStumbleRisk (early exit typical): 0 float, 1 branch
- CalculateLeanAngle: 7 float + 1 Atan2, 2 branches
- **Subtotal: ~53 float ops + 3 Atan2 + 1 Cos + 1 Sin + 2 Sqrt, ~12 branches**

**Integration:** 12 float ops, 0 branches.
**Safety:** ~0 float ops (typical), ~35 branches.
**Cache updates:** ~15 float ops + 1 Sqrt, ~1 branch.
**Logger:** ~1 float op, ~1 branch.

**SPRINTING total: ~125 float ops + 11 transcendentals (~55 equiv) = ~180 equivalent ops, ~97 branches.**

At ~1 ns per equivalent operation on modern hardware (3 GHz, 1 FLOP/cycle): ~180 ns of pure arithmetic per agent. Including branch evaluation overhead (~0.5 ns per correctly-predicted branch): ~180 + 49 â‰ˆ **229 ns per agent per frame** for pure compute.

#### 5.1.5 Per-Frame Total (22 Agents)

All 22 agents execute the movement pipeline. Two are goalkeepers (Spec #10), but for budget purposes we conservatively assume all 22 use this spec's full pipeline.

**Worst case (all 22 agents SPRINTING):**
- 22 Ã— 180 equiv ops = **3,960 equivalent operations**
- 22 Ã— 229 ns = **~5.0 Âµs** of pure compute

This is categorically trivial. Even the absolute worst case (every agent sprinting, every turn clamped, every stumble check evaluated) would complete in well under 0.01ms.

**Why the measured time will be higher than pure compute:** Memory access latency, cache misses on first-frame agent struct reads, Unity's managed runtime overhead (C# JIT vs. native), profiler instrumentation, and GC pauses all add latency. The pure compute estimate of ~5 Âµs should be treated as a theoretical floor. Realistic measured time will be 10â€“50Ã— higher due to these factors, placing actual per-frame agent movement cost in the **0.05â€“0.25ms range** â€” still well within the 3.0ms budget.

**KEY FINDING: Agent movement is not a performance bottleneck.** Expected actual cost of ~0.05â€“0.25ms against a 3.0ms budget represents **~2â€“8% utilization**. The 3.0ms allocation is extremely generous for this system. The surplus budget (~2.75â€“2.95ms) is available for reallocation to more demanding systems (AI decisions, collision detection) once those specs are written and profiled. This finding should inform budget negotiations during Spec #3 (Collision) and Spec #7 (Tactical AI) drafting.

#### 5.1.6 Match-Level Complexity

**90-minute match at 60Hz:**
- Total frames: 90 Ã— 60 Ã— 60 = **324,000 frames**
- Total agent-frames: 324,000 Ã— 22 = **7,128,000 agent-updates**

**Estimated state distribution per agent across a typical match:**

| State | % of Frames | Agent-Frames | Equiv Ops/Agent | Total Equiv Ops |
|---|---|---|---|---|
| JOGGING | 35% | 2,494,800 | ~174 | 434,095,200 |
| WALKING | 25% | 1,782,000 | ~164 | 292,248,000 |
| IDLE | 15% | 1,069,200 | ~87 | 93,020,400 |
| SPRINTING | 10% | 712,800 | ~180 | 128,304,000 |
| DECELERATING | 10% | 712,800 | ~173 | 123,314,400 |
| STUMBLING | 3% | 213,840 | ~70 | 14,968,800 |
| GROUNDED | 2% | 142,560 | ~70 | 9,979,200 |
| **TOTAL** | **100%** | **7,128,000** | | **~1.096 billion** |

**~1.10 billion equivalent float ops per 90-minute match** across all 22 agents. At 3 GHz with 1 FLOP/cycle: ~0.37 seconds of pure compute for the entire match. Agent movement is computationally cheap in absolute terms â€” the real performance concern is per-frame latency (fitting within the 16.67ms budget), not total throughput.

**State distribution methodology note:** The percentages above are educated estimates based on published GPS tracking data distributions for professional football (Bloomfield et al., 2007 reports approximately 40% walking, 35% jogging, 20% running/sprinting, 5% other for outfield players). The estimates are skewed slightly toward JOGGING because in a simulation where AI controls all agents, fewer periods of true IDLE occur compared to real matches (where players wait for set pieces, argue with referees, etc.).

**STUMBLING/GROUNDED caveat:** The 3% STUMBLING estimate implies each agent spends ~162 seconds stumbling across a 90-minute match â€” roughly one stumble every 33 seconds lasting 0.5â€“1.5s each (60â€“180 stumble events per agent per match). **This is likely an overestimate.** Real match stumble frequency depends heavily on AI tactical aggressiveness and the stumble probability tuning in Section 3.4.4. Conservative estimate for cost analysis is appropriate (overestimates total cost, which is safe), but the actual distribution may show STUMBLING at 1â€“1.5% and GROUNDED at 0.5â€“1% once AI behaviors are implemented. Similarly, the 2% GROUNDED implies ~108 seconds on the ground per agent â€” also aggressive unless the Collision System produces frequent knockdowns.

These percentages **MUST** be validated post-implementation by instrumenting state tracking over 100 simulated matches across varied tactical setups (see KL-2 in Section 5.7).

---

### 5.2 Memory Analysis

#### 5.2.1 Static Memory (Per-Agent)

The Agent class (Section 3.5.1, v1.2) contains all movement state for a single agent. Field-by-field byte count derived from the authoritative class definition:

| Field Group | Fields | Size (bytes) | Notes |
|---|---|---|---|
| Object header | (class overhead, 64-bit runtime) | 16 | Sync block + method table pointer |
| Identity | AgentID (int), TeamID (int), IsGoalkeeper (bool + 3 pad) | 12 | readonly, fixed at match start |
| Attributes | PlayerAttributes struct (15 int fields) | 60 | Pace, Accel, Agility, Balance, Strength, Stamina + 9 technical/mental |
| Position | Position (Vector3) | 12 | 3 Ã— float |
| Velocity | Velocity (Vector3) | 12 | 3 Ã— float |
| Facing | FacingDirection (Vector2) | 8 | 2 Ã— float (XY plane) |
| Speed cache | _cachedSpeed (float), _speedDirty (bool + 3 pad) | 8 | Invalidated on velocity change |
| State Machine | CurrentState, PreviousState, CommandedState (3 Ã— enum/int), TimeInState (float), GroundedReason (enum/int), CollisionForce (float) | 24 | 6 Ã— 4 bytes |
| Turning | LeanAngle (float), CurrentTurnRate (float), IsInStumbleRiskZone (bool + 3 pad) | 12 | |
| Fatigue | AerobicStaminaPool, SprintStaminaReservoir, FatigueLevel, StaminaRegenRate (4 Ã— float) | 16 | Read-only from movement system; Spec #13 writes |
| PerformanceContext | FormModifier, ContextModifier, CareerModifier (3 Ã— float) | 12 | Cached at 10Hz |
| Safety/Recovery | LastValidPosition (Vector3), LastValidVelocity (Vector3), StateChangesThisSecond (int), StateLockExpiry (float) | 32 | 6 float + 2 int/float |
| **Total per agent** | | **~224 bytes** | **Platform alignment may adjust Â±8 bytes** |

**Cross-reference note:** This count is derived from the fields listed in Section 3.5.1 v1.2. If Section 3.5 adds fields in a future revision, this table must be updated. The `PhysicalProperties` and `AnimationData` properties are computed (getter returns new struct) and do not add to stored size.

**22 agents: 22 Ã— 224 = ~4,928 bytes â‰ˆ 4.8 KB**

This is negligible. The entire agent array fits comfortably in L2 cache (typically 256 KB â€“ 1 MB) and likely in L1 data cache (32â€“64 KB) on modern CPUs.

**Comparison to Ball Physics:** BallState is 56 bytes (single cache line). Agent is ~224 bytes (~3.5 cache lines at 64-byte line size). Less ideal for cache performance but still small enough for excellent spatial locality when iterating sequentially.

#### 5.2.2 Static Memory (Shared / Singleton)

| Component | Size (bytes) | Notes |
|---|---|---|
| MovementConstants | ~160 | All const/static readonly floats (~40 constants Ã— 4 bytes) |
| MovementThresholds | ~80 | ~20 threshold constants Ã— 4 bytes |
| DirectionalConstants | ~48 | ~12 constants Ã— 4 bytes |
| TurnConstants | ~60 | ~15 constants Ã— 4 bytes |
| StumbleConstants | ~32 | ~8 constants Ã— 4 bytes |
| LeanConstants | ~12 | 3 constants Ã— 4 bytes |
| PitchConfiguration | ~48 | 6 floats Ã— 2 (min/max) Ã— 4 bytes |
| MessageCode enum | ~4 | Integer-backed enum |
| **Total shared** | **~444 bytes** | |

Constants are in the `.data` segment (read-only memory), loaded once at startup. They will be resident in L1 cache after the first frame due to frequent access.

#### 5.2.3 Dynamic Memory

| Component | Size | Allocation Pattern | GC Risk |
|---|---|---|---|
| MovementCommand (per agent) | ~32 bytes | Value type (struct), stack-allocated | None |
| OscillationGuard ring buffer | ~32 bytes (8 Ã— float) | Pre-allocated in Agent constructor | None |
| AgentEventLogger | Design decision: shared vs. per-agent (see below) | Pre-allocated ring buffer | None if pre-allocated |
| Debug.Log strings | Variable | Only in UNITY_EDITOR/DEVELOPMENT_BUILD | None in release |

**Logger architecture decision:** Recommendation is **one shared logger** (mirrors BallEventLogger pattern). Shared logger uses a single ring buffer of ~1,024 events Ã— ~64 bytes/event = ~64 KB. Per-agent loggers would require 22 Ã— 256 events Ã— 64 bytes = ~352 KB â€” 5.5Ã— more memory for marginal benefit. Agent ID is stored per-event for filtering.

**Total dynamic memory: ~64 KB** (shared logger) + ~704 bytes (22 Ã— 32 byte MovementCommand structs) â‰ˆ **~65 KB**.

#### 5.2.4 GC Risk Assessment

**Target: Zero heap allocations in the movement update loop** (same zero-allocation policy as Ball Physics).

Potential allocation sources and mitigations:

| Source | Risk | Mitigation |
|---|---|---|
| String formatting in Debug.Log | HIGH if unguarded | Preprocessor guards: `#if UNITY_EDITOR` |
| Boxing enum values in comparisons | MEDIUM | Use generic `EqualityComparer<T>` or integer cast |
| List resizing in event logger | MEDIUM | Pre-allocated ring buffer with fixed capacity |
| Lambda closures in LINQ | HIGH if present | Zero LINQ in update loop (code review enforced) |
| `new Vector3()` construction | LOW (struct) | Value types; no heap allocation |
| PerformanceContext struct creation | LOW (struct) | Value type, returned by value |
| System.Random.NextDouble() | LOW | Single pre-allocated RNG instance per match |

**GC risk rating: LOW** â€” identical mitigations to Ball Physics, with the same code review enforcement. The only additional risk is the 22Ã— higher call frequency amplifying any accidental allocation. A single string format leak in the hot path would produce 22 allocations per frame (1,320/second) â€” detectable immediately via profiler heap track.

#### 5.2.5 Cache Performance

**Access pattern:** Sequential iteration over a contiguous array of 22 Agent instances. The movement pipeline processes each agent completely before moving to the next (AoS â€” Array of Structures pattern).

**Cache line analysis:**
- Agent struct: ~224 bytes = 3.5 cache lines (64 bytes/line)
- During iteration, each agent triggers 3â€“4 cache line loads
- Total cache lines per frame: 22 Ã— 4 = 88 cache line loads
- At ~4 ns per L1 miss / L2 hit: 88 Ã— 4 = ~352 ns
- At ~10 ns per L2 miss / L3 hit: 88 Ã— 10 = ~880 ns

**Practical expectation:** After the first frame, the constants and at least several agents will be resident in L1/L2. Sequential access pattern means hardware prefetchers will be effective. Cache-related overhead is estimated at 0.5â€“2.0 Âµs per frame â€” small relative to the 3.0ms budget.

**SoA (Structure of Arrays) consideration:** Reorganizing from AoS to SoA (separate arrays for Position[], Velocity[], State[], etc.) would improve cache utilization by loading only the fields needed for each pipeline step. This is a **P2 optimization** (Section 5.6) â€” not needed in Stage 0 where the 3.0ms budget is generous, but valuable if Stage 5's Fixed64 migration pushes costs higher.

---

### 5.3 Profiling Targets

#### 5.3.1 Budget Derivation

Ball Physics Spec #1 Section 6.2.1 allocated **3.0ms** to "Agent physics (22 agents)" in the frame budget table. This is our ceiling.

**Available frame budget at 60Hz:** 16.67ms per frame.

**Agent movement allocation from Ball Physics budget table:**

| System | Budget | % of Frame |
|---|---|---|
| Agent physics (22 agents) | **3.0ms** | **18%** |
| AI decisions (22 agents) | 4.0ms | 24% |
| Collision detection | 1.0ms | 6% |
| Ball physics | 0.5ms | 3% |
| Event system | 0.5ms | 3% |
| Rendering | 5.0ms | 30% |
| Audio + UI | 1.0ms | 6% |
| Headroom | 1.17ms | 10% |

**Agent movement target: <2.0ms average, <3.0ms p99 ceiling.**

The 2.0ms average target provides 33% headroom below the 3.0ms ceiling, accounting for variance from:
- 10Hz heartbeat frames (slightly more expensive every 6th frame)
- State distribution variance (matches with heavy sprinting cost more)
- Occasional stumble risk evaluation overhead
- Platform-dependent transcendental function costs

**Per-agent budget derivation:**
- 22 agents within 2.0ms average â†’ **0.091ms (91 Âµs) per agent average**
- 22 agents within 3.0ms ceiling â†’ **0.136ms (136 Âµs) per agent ceiling**

#### 5.3.2 Per-Function Profiling Targets

| Profiler Label | Target (avg) | Ceiling (p99) | Notes |
|---|---|---|---|
| `AgentMovement.UpdateAll` | <2.0ms | <3.0ms | Full pipeline, all 22 agents |
| `AgentMovement.UpdateSingle` | <0.09ms | <0.14ms | Per-agent complete pipeline |
| `AgentStateMachine.Evaluate` | <0.015ms | <0.03ms | Per-agent state evaluation |
| `AgentLocomotion.Calculate` | <0.020ms | <0.04ms | Per-agent speed update (includes Exp) |
| `AgentDirectional.Calculate` | <0.015ms | <0.03ms | Per-agent zone penalty (includes Acos) |
| `AgentTurning.Calculate` | <0.025ms | <0.04ms | Per-agent turning (most expensive â€” 3 Atan2 + trig) |
| `AgentSafety.Validate` | <0.010ms | <0.02ms | Per-agent NaN check + clamps |
| `AgentMovement.CacheUpdate` | <0.005ms | <0.01ms | Per-agent property cache refresh |

**Observation:** The per-agent targets are conservative relative to pure operation counts. The SPRINTING worst case computes in ~229 ns of pure arithmetic, yet the target allows 91,000 ns (91 Âµs) â€” a 400Ã— margin. This margin is necessary because:
1. Pure op counts exclude memory access latency
2. Unity's managed C# runtime adds JIT and GC overhead
3. Profiler instrumentation itself costs ~1â€“5 Âµs per sample
4. The targets must hold on minimum-spec hardware (2018 laptop), not just modern desktop CPUs

If profiling reveals actual times are 10Ã— below targets (likely), the surplus budget can be reallocated to AI or collision detection.

#### 5.3.3 Measurement Protocol

Mirrors Ball Physics Section 6.2.3 exactly:

1. Run 100 simulated matches (headless mode, no rendering)
2. Record `AgentMovement.UpdateAll` timing every frame via Unity Profiler
3. Record `AgentMovement.UpdateSingle` timing for each of the 22 agents per frame
4. Calculate: mean, p50, p95, p99, max for both aggregate and per-agent timings
5. Flag any frame exceeding 3.0ms for investigation
6. Record state distribution per agent across all matches
7. Record per-function breakdown for p99 frames

**Pass criteria:**

| Metric | Required | Desired |
|---|---|---|
| UpdateAll mean | <2.0ms | <1.0ms |
| UpdateAll p50 | <2.0ms | <1.0ms |
| UpdateAll p95 | <2.5ms | <1.5ms |
| UpdateAll p99 | <3.0ms | <2.0ms |
| UpdateAll max | <5.0ms | <3.0ms |
| UpdateSingle p99 | <0.14ms | <0.05ms |
| GC allocations in update loop | 0 | 0 |

**Failure response:**
- If UpdateAll mean >2.0ms: Profile per-function for all 22 agents, identify bottleneck. Check for accidental heap allocation.
- If UpdateAll p99 >3.0ms: Likely a GC pause or OS interrupt overlapping the measurement window. Verify by checking if the spike is localized to AgentMovement or is frame-wide.
- If UpdateSingle p99 >0.14ms for specific agents: Check if SPRINTING + full stumble evaluation is the cause (expected), or if a pathological edge case is triggering expensive recovery paths (Section 3.6).
- If GC allocations >0: Code review for string formatting, LINQ, boxing. Fix immediately â€” this is a hard requirement.

#### 5.3.4 10Hz Heartbeat Cost Analysis

Every 6th frame, the 10Hz tactical heartbeat triggers PerformanceContext cache invalidation for all 22 agents. This adds overhead to those frames.

**Additional cost per 10Hz frame:**
- Per agent: 4 Ã— (2 extra multiplies for modifier recomputation) = 8 float ops
- 22 agents: 176 additional float ops
- Time estimate: ~0.06 Âµs (negligible)

**10Hz frame total vs. normal frame:**
- Normal frame: ~2,800â€“3,960 equiv ops across 22 agents
- 10Hz frame: ~2,976â€“4,136 equiv ops (+176)
- **Increase: ~4.4%** â€” well within measurement noise

All 22 agents refresh on the same 10Hz tick (not staggered). This creates a predictable per-6th-frame bump rather than spreading the cost randomly. The bump is small enough to be invisible in profiling.

---

### 5.4 Budget Context

#### 5.4.1 Agent Movement Within the Frame Budget

Reproducing Ball Physics Section 6.2.1 budget table with agent movement's position highlighted:

| System | Budget | % of Frame | Status |
|---|---|---|---|
| **Agent physics (this spec)** | **3.0ms** | **18%** | **Analyzed herein** |
| AI decisions (22 agents) | 4.0ms | 24% | Placeholder (Spec #7, #12â€“15) |
| Collision detection | 1.0ms | 6% | Placeholder (Spec #3) |
| Ball physics | 0.5ms | 3% | Validated (Spec #1 Â§6) |
| Event system | 0.5ms | 3% | Placeholder (Spec #17) |
| Rendering | 5.0ms | 30% | Placeholder (Stage 1) |
| Audio + UI | 1.0ms | 6% | Placeholder |
| Headroom | 1.17ms | 10% | Safety margin |
| **TOTAL** | **16.67ms** | **100%** | |

**Agent movement is the second-largest physics consumer** (after AI decisions). This is expected â€” 22 entities each running a full physics pipeline is inherently more expensive than a single ball. However, at ~180 equiv ops per agent, agent movement is cheap per-entity. The 3.0ms budget provides massive headroom.

#### 5.4.2 Budget Pressure from Future Specs

Systems not yet specified will consume from the same frame budget. Known pressure points:

| Future System | Spec | Budget Impact on Agent Movement |
|---|---|---|
| Collision System | #3 | May need some of the 3.0ms if collision response requires agent state writes |
| Goalkeeper Mechanics | #10 | 2 agents use Spec #10 instead of this spec â€” may be cheaper or more expensive |
| First Touch | #11 | Momentary overhead during ball contact â€” amortized across many frames |
| Positioning AI | #12 | Generates MovementCommands consumed by this spec â€” no direct cost here |

**Conservative assessment:** The 3.0ms allocation may need to be shared with Collision System's agent-side response logic. If Collision Spec #3 requires 0.5â€“1.0ms for per-agent collision response, the effective agent movement budget shrinks to 2.0â€“2.5ms. This is still generous relative to the ~0.05â€“0.25ms expected actual cost. No action needed until Spec #3 is drafted.

---

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

## Document Status

**Section 5 Completion:**
- âœ… Per-agent O(1) complexity classification with justification (5.1.1)
- âœ… Operation counts derived line-by-line from Sections 3.1â€“3.6 code (5.1.2)
- âœ… Per-state cost breakdown with SPRINTING detailed walkthrough (5.1.3â€“5.1.4)
- âœ… 22-agent per-frame aggregate analysis (5.1.5)
- âœ… Match-level cost with state distribution estimate (5.1.6)
- âœ… Memory analysis: per-agent struct, shared constants, dynamic, GC risk (5.2)
- âœ… Cache performance analysis with SoA consideration (5.2.5)
- âœ… Profiling targets derived from Ball Physics budget table (5.3)
- âœ… Measurement protocol with pass/fail criteria (5.3.3)
- âœ… 10Hz heartbeat cost analysis (5.3.4)
- âœ… Budget context with full frame allocation table (5.4)
- âœ… Optimization roadmap with P0â€“P3 classification (5.5)
- âœ… Anti-pattern checklist for implementation (5.6)
- âœ… Known limitations with action items (5.7)
- âœ… Cross-references to all related sections (5.8)

**Page Count:** ~13 pages
**Status:** Ready for review
**Next Section:** Section 4 (Implementation Details) â€” establishes code structure and pipeline order that Section 5 references
