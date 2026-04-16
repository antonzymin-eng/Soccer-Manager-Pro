## 3.3.6 Numerical Examples & Validation

### Example 1: Defensive Jockey (Lateral Movement)

**Player:** Pace 14, Agility 16, Acceleration 12  
**Context:** PerformanceContext.CreateNeutral()  
**Facing:** TARGET_LOCK on opponent with ball  
**Movement:** Shuffling laterally (movement angle â‰ˆ 70Â°)

```
EffectivePace = 14.0
BaseTopSpeed = 7.5 + (14.0 - 1.0) Ã— 0.14211 = 9.347 m/s

EffectiveAgility = 16.0
LateralMult = 0.65 + (16.0 - 1.0) Ã— 0.005263 = 0.65 + 0.0789 = 0.729

DirectionalTopSpeed = 9.347 Ã— 0.729 = 6.814 m/s

EffectiveAccel = 12.0
k_base = 0.658 + (12.0 - 1.0) Ã— 0.01384 = 0.810
k_directional = 0.810 Ã— âˆš0.729 = 0.810 Ã— 0.854 = 0.692
Tâ‚‰â‚€ = 2.3026 / 0.692 = 3.328s (vs 2.94s forward â€” 13% slower, not 44%)

Fatigue modifier: 1.0 (fresh)
FinalTopSpeed = 6.814 Ã— 1.0 = 6.814 m/s (â‰ˆ24.5 km/h lateral)
```

**Validation:** Lateral acceleration is moderately slower (3.33s vs 2.94s) rather than dramatically sluggish. The agent feels responsive entering a jockey â€” the main penalty is the 27% lower top speed, not the acceleration. This matches real-world observation where players initiate lateral movement quickly but can't sustain high lateral speed.

### Example 2: Centre-Back Backpedaling

**Player:** Pace 10, Agility 8, Acceleration 9  
**Context:** PerformanceContext.CreateNeutral()  
**Facing:** TARGET_LOCK on ball  
**Movement:** Directly backward (movement angle = 180Â°)

```
EffectivePace = 10.0
BaseTopSpeed = 7.5 + (10.0 - 1.0) Ã— 0.14211 = 8.779 m/s

EffectiveAgility = 8.0
BackwardMult = 0.45 + (8.0 - 1.0) Ã— 0.005263 = 0.45 + 0.0368 = 0.487

DirectionalTopSpeed = 8.779 Ã— 0.487 = 4.275 m/s

k_base = 0.658 + (9.0 - 1.0) Ã— 0.01384 = 0.769
k_directional = 0.769 Ã— âˆš0.487 = 0.769 Ã— 0.698 = 0.537
Tâ‚‰â‚€ = 2.3026 / 0.537 = 4.289s (vs 2.99s forward â€” 43% slower)

FinalTopSpeed = 4.275 m/s (â‰ˆ15.4 km/h backward)
```

**Validation:** Backward acceleration is notably slower (4.29s vs 2.99s) â€” the sqrt penalty is still meaningful here because the backward multiplier (0.487) is low enough that even the sqrt (0.698) produces a substantial penalty. A centre-back backpedaling at 15.4 km/h with slow ramp-up creates the realistic tactical tension: any forward running at 30+ km/h closes distance rapidly. Defenders must either turn and sprint (losing sight of the ball, needing Section 3.4 turn time) or hold the backpedal and accept the speed disadvantage.

### Example 3: Interpolation Zone â€” 35Â° Angle

**Player:** Agility 12  
**Movement angle:** 35Â° (midpoint of forwardâ†’lateral interpolation)

```
EffectiveAgility = 12.0
LateralMult = 0.65 + (12.0 - 1.0) Ã— 0.005263 = 0.708

Interpolation:
  t = (35 - 30) / (40 - 30) = 0.5
  Multiplier = Lerp(1.0, 0.708, 0.5) = 0.854

TopSpeed = 9.07 Ã— 0.854 = 7.746 m/s (vs 9.07 forward, 6.424 full lateral)
```

**Validation:** Smooth transition â€” no jarring speed change at the 30Â° boundary. The 35Â° angle produces a multiplier roughly halfway between forward and lateral zones, which feels natural for a slight diagonal movement.

### Example 4: Stage 4 Scenario â€” Poor Form Affecting Lateral Movement

**Player:** Pace 15, Agility 17  
**Context:** Form=0.88, Context=0.90, Career=1.0 â†’ Combined = 0.792  
**Movement angle:** 65Â° (lateral jockey)

```
EffectivePace = 15.0 Ã— 0.792 = 11.88
BaseTopSpeed = 7.5 + (11.88 - 1.0) Ã— 0.14211 = 9.046 m/s

EffectiveAgility = 17.0 Ã— 0.792 = 13.46
LateralMult = 0.65 + (13.46 - 1.0) Ã— 0.005263 = 0.716

DirectionalTopSpeed = 9.046 Ã— 0.716 = 6.477 m/s

Compare to neutral context:
  EffectivePace = 15.0, BaseTopSpeed = 9.495 m/s
  EffectiveAgility = 17.0, LateralMult = 0.734
  DirectionalTopSpeed = 9.495 Ã— 0.734 = 6.969 m/s

Difference: 6.477 vs 6.969 = -7.1% lateral speed
```

**Analysis:** PerformanceContext reduces not just forward speed but also lateral agility. The -7.1% feels like a player who's "half a step slower" in the jockey â€” enough for an attacker to exploit but not so dramatic that the defender is useless. The dual effect (lower top speed Ã— lower directional multiplier) compounds naturally through the existing architecture.

### Validation Summary

| Test Case | Requirement | Result | Status |
|---|---|---|---|
| Forward multiplier (0Â°â€“30Â°) | 1.0Ã— (FR-4) | 1.0 | âœ“ PASS |
| Lateral range (Agility 1) | 0.65Ã— (FR-4) | 0.650 | âœ“ PASS |
| Lateral range (Agility 20) | 0.75Ã— (FR-4) | 0.750 | âœ“ PASS |
| Backward range (Agility 1) | 0.45Ã— (FR-4) | 0.450 | âœ“ PASS |
| Backward range (Agility 20) | 0.55Ã— (FR-4) | 0.550 | âœ“ PASS |
| Smooth interpolation at 30Â° | No discontinuity | Lerp band 30Â°â€“40Â° | âœ“ PASS |
| Smooth interpolation at 90Â° | No discontinuity | Lerp band 80Â°â€“90Â° | âœ“ PASS |
| Multiplier applies to top speed | FR-4 | Via CalculateEffectiveTopSpeed() | âœ“ PASS |
| Multiplier applies to accel rate | FR-4 | Via ApplyDirectionalToAccelK() (âˆšmult) | âœ“ PASS |
| Accel penalty â‰¤ speed penalty | Biomechanical realism | âˆš0.65=0.81 vs 0.65 (lateral) | âœ“ PASS |
| Facing independent from movement | FR-4 | TARGET_LOCK mode | âœ“ PASS |
| Facing rotation is uniform | Constant angular velocity | Signed-angle Atan2 method | âœ“ PASS |
| Zone hysteresis prevents flicker | No animation stutter at boundaries | Â±3Â° dead zone | âœ“ PASS |
| PerformanceContext affects lateral | Extensibility | Agility reduced by modifiers | âœ“ PASS |

---

## Cross-References

| Section | References To | Nature |
|---|---|---|
| 3.3.2 (Zones) | FR-4 (Sections 1-2) | Implements zone angles and multiplier ranges |
| 3.3.3 (Multiplier) | Section 3.2.4 (Top Speed) | Multiplier consumed by CalculateEffectiveTopSpeed() |
| 3.3.3 | Section 3.2.1 (PerformanceContext) | Agility evaluated through gateway |
| 3.3.3 (Zone Hysteresis) | Animation system (Stage 1) | Stable discrete zone for animation selection |
| 3.3.4 (Facing) | Section 3.4 (Turning) | Turn rate governs facing rotation speed |
| 3.3.5 (Integration) | Section 3.2.3 (Acceleration) | Modified k (âˆšmult) passed to acceleration model |
| 3.3.5 | Section 3.1.6 (State-Physics Table) | State determines whether multiplier is active |

---

## Future Extension Notes

**Stage 1 â€” Dribbling directional penalty:**
- Agent with ball at feet receives an additional directional penalty (reduced lateral/backward multiplier). Implemented as a secondary multiplier applied after the base directional calculation. The function signature of `CalculateDirectionalMultiplier()` does not change â€” a dribble modifier is applied externally by the caller.

**Stage 2 â€” Surface effects:**
- Wet or degraded pitch surfaces could differentially affect lateral movement (reduced traction in sidestep). Implemented as a surface modifier on the lateral/backward multiplier ranges, not a change to the zone geometry.

**Stage 4 â€” Pressure-dependent facing behavior:**
- High-pressure situations (crowd hostility, match importance) could reduce the effectiveness of TARGET_LOCK by adding noise to the facing direction (simulating loss of concentration). This would be a modifier on the `UpdateFacingDirection()` output, not a structural change.

---

**End of Section 3.3**

**Page Count:** ~10 pages  
**Next Section:** Section 3.4 â€” Turning & Momentum (turn radius, lean angle, speed-dependent constraints)
