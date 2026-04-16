## IMPLEMENTATION NOTES

1. **Memory Layout Optimization (Deferred to Stage 1):**
   - Agent struct is currently class-based for ease of development
   - Stage 1 may convert to struct + ECS pattern for cache efficiency
   - Current design supports either approach without API changes
   - See Agent class documentation for ECS refactoring considerations

2. **Fixed64 Support (Deferred to Stage 5):**
   - All floats can be replaced with Fixed64 type via conditional compilation
   - Formulas written to avoid float-specific operations (no Mathf.Lerp, etc.)
   - Ensures deterministic simulation for multiplayer

3. **Animation Data Contract Future-Proofing:**
   - AnimationDataContract includes Stage 1+ fields (HasBallAtFeet, InjuryLevel) even though unused in Stage 0
   - Prevents breaking changes when Stage 1 animation system comes online
   - All fields default to safe values (false, 0.0) if not populated
   - ContractVersion field (always 1 in Stage 0) enables backward compatibility checking

4. **Collision Interface Immutability:**
   - AgentPhysicalProperties is regenerated every frame (not cached)
   - Prevents stale data if agent state changes mid-frame
   - Collision System must not store references to this struct across frames

5. **MovementCommand Extensibility:**
   - Current struct is minimum viable interface for Stage 0
   - Tactical AI systems (Spec #7, #12) may extend with additional fields
   - Factory methods cover common patterns but are not exhaustive
   - Extensions must maintain backward compatibility with existing consumers

6. **PlayerAttributes Cross-Spec Coordination (updated v1.3):**
   - v1.3 adds KickPower, WeakFootRating, Crossing per AM-002-001 (ERR-007 resolution)
   - Full attribute list remains provisional pending coordination with all 20 specs
   - Spec #20 (Code Standards) will create master attribute registry
   - Any attribute name changes after Week 4 require impact analysis
   - Each spec must document which attributes it consumes and their semantics

7. **Telemetry Ring Buffer Tuning:**
   - 1000 events/agent = ~16.7s history at 60Hz, ~528 KB for 22 agents
   - Acceptable for Stage 0 debugging, may optimize in Stage 1
   - Profiling in Section 6 will measure actual performance impact
   - Compression strategies available if memory becomes constraint

8. **GROUNDED Dwell Time Variable Formula (v1.1):**
   - Base 1.0s scaled by (Strength + Balance) / 40.0
   - Modified by GroundedReason: COLLISION (1.0x), SLIDING_TACKLE (0.6x), DIVING_HEADER (0.7x)
   - Collision force scaling applied only for involuntary knockdowns
   - Clamped to [0.6s, 2.5s] range
   - See Section 3.1.5.2 for full formula and examples

9. **STUMBLE Attribute Correction (v1.1, refined v1.2):**
   - Primary attribute is Balance (not Agility)
   - Dwell time range: 300-800ms (not 300-500ms)
   - Stumble probability uses fraction-based safe zone model per Section 3.4
   - Constants renamed and values aligned with Section 3.4 StumbleConstants

10. **Speed Caching Implementation (v1.1):**
    - Speed is now properly cached with dirty flag
    - Invalidated whenever Velocity changes
    - Documentation now matches actual implementation
    - Eliminates redundant sqrt() calls per frame

11. **Turn Rate Model Alignment (v1.2):**
    - Section 3.4 uses hyperbolic decay: Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)
    - All agents share 720Â°/s base rate at zero speed
    - Agility affects k_turn (0.35â€“0.78), which controls speed sensitivity
    - Balance applies as multiplicative modifier (0.85â€“1.0)
    - State-based multipliers from v1.1 were incorrect and have been removed
    - See Section 3.4.2 for full derivation and examples

12. **FacingMode Simplification (v1.2):**
    - Section 3.3 defines exactly two modes: AUTO_ALIGN and TARGET_LOCK
    - "Freeze facing" behavior achieved via TARGET_LOCK with zero-delta target
    - This eliminates redundant enum values while preserving all functionality
    - See Section 3.3.4.2 for complete facing update logic
