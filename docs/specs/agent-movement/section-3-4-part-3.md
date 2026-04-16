## Future Extension Notes

**Stage 1 â€” Facing rate multiplier:**
- If playtesting confirms that shared Ï‰_max creates unrealistic TARGET_LOCK behavior at sprint speed, introduce `FACING_RATE_MULTIPLIER` (1.2â€“1.5Ã—). Applied as a single multiplication in `UpdateFacingDirection()` before calling `ApplyTurnRateLimit()`. The core `CalculateMaxTurnRate()` formula is unchanged â€” only the facing system's consumption of Ï‰_max is modified. Path curvature constraints remain at base Ï‰_max. This maintains the principle that turning the body (path) is harder than turning the head (facing) while keeping a single authoritative turn rate source.

**Stage 1 â€” Animation-driven lean:**
- Animation system consumes `leanAngleDeg` from the animation data contract to tilt agent models during turns. No change to this section's calculations required â€” the data is already being produced.


**Stage 1 â€” Dribbling turn penalty:**
- Agent with ball at feet receives an additional turn rate penalty (harder to change direction while controlling the ball). Implemented as an external multiplier on the return value of `CalculateMaxTurnRate()`, not a change to the turn rate formula itself. Suggested modifier range: 0.65â€“0.85Ã— depending on Dribbling attribute.

**Stage 2 â€” Surface traction effects:**
- Wet or degraded pitch surfaces reduce available lateral grip, lowering the effective safe fraction (wider risk zone for stumble). Implemented as a surface modifier on `SAFE_FRACTION_MIN/MAX`, not a change to the turn rate model.

**Stage 2 â€” Kinetic profile interaction:**
- Bio-mechanical profile (heel-striker, forefoot, neutral â€” Master Vol 1, Â§2.2) could modify k_turn slightly: forefoot runners turn tighter (lower k_turn modifier), heel-strikers turn wider. This is a modifier on the k_turn calculation, preserving the existing formula.

**Stage 4 â€” Pressure-induced turn hesitation:**
- High-pressure situations (crowd hostility, late match importance) could add noise to the turn rate, simulating momentary indecision. This would be a random modifier on the `CalculateMaxTurnRate()` output, not a structural change.

**Stage 5 â€” Fixed64 migration:**
- All `float` types in this section become `Fixed64`.
- `Mathf.Atan2()` replaced by `Fixed64Math.Atan2()` (lookup table implementation).
- `Mathf.Cos()/Sin()` replaced by `Fixed64Math.Cos()/Sin()`.
- `Mathf.Clamp()` replaced by `Fixed64Math.Clamp()`.
- No algorithmic changes required â€” only type substitution.

---

**End of Section 3.4**

**Page Count:** ~17 pages  
**Next Section:** Section 3.5 â€” Fatigue Integration (dual-energy model, aerobic pool, sprint reservoir)
