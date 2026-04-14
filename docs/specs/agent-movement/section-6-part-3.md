### 6.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All Section 3.7 unit tests continue to pass** with default parameters (no dribbling, dry grass, sea level, no wind, all modifiers = 1.0). Extensions add new test cases but must not break existing ones.

2. **O(1) per-agent complexity is maintained.** No extension may introduce loops over other agents, spatial queries, or variable-length iterations in the per-agent update path. (Exception: LOD tier assignment may query ball position â€” this is O(1) per agent.)

3. **Zero-allocation policy is maintained.** No extension may introduce heap allocations in the physics update loop. New structs (`SurfaceTractionModifier`, `Vector3Fixed`) must be value types.

4. **Per-agent budget ceiling is maintained.** With default parameters, per-agent update must remain under 0.14ms (p99). âš ï¸ This value is pending Section 5 completion. Fixed64 migration may increase this ceiling â€” the new ceiling must be documented and approved before implementation.

5. **State machine transition logic produces identical results for identical inputs.** State thresholds, hysteresis values, and transition conditions must not change based on active extensions. Extensions may add new states but must not alter existing state logic.

6. **AnimationDataContract remains backward compatible.** New fields must be added at end of struct. `ContractVersion` must increment. Animation systems must handle older contract versions gracefully.

7. **PerformanceContext.EvaluateAttribute() returns identical values for default modifiers.** When all modifiers are 1.0, output must equal input. New modifiers must default to 1.0.

8. **Form and Psychology modifiers are invisible when inactive.** Stage 0â€“2 tests must pass without FormSystem or PsychologySystem present. Modifier fields default to 1.0 and require no external dependency to function.

---

### 6.8 Cross-References

| Topic | Section | Summary |
|-------|---------|---------|
| Permanent exclusions list | Section 1.2 | Subset of 6.5 (this section is authoritative) |
| PerformanceContext definition | Section 3.5.6 | Modifier struct with EvaluateAttribute() |
| AnimationDataContract definition | Section 3.5.5 | Data contract for rendering |
| PitchConfiguration definition | Section 3.6.5 | Pitch dimensions and surface properties |
| State machine design | Section 3.1 | AgentMovementState enum |
| Locomotion model | Section 3.2 | Acceleration curves, speed calculations |
| Turning mechanics | Section 3.4 | Turn rate, stumble system |
| File structure | Section 4.1 | PitchConfiguration migration note |
| Performance budgets | Section 5.3 | Per-agent timing targets âš ï¸ (pending Section 5 completion) |
| P2 optimizations | Section 5.6 | SIMD, SoA, LOD âš ï¸ (pending Section 5 completion) |
| Ball Physics Fixed64 migration | Spec #1, Section 7.4.1 | Parallel migration path |
| Ball Physics surface extensions | Spec #1, Section 7.2.1 | Shared SurfaceType system |
| First Touch Mechanics | Spec #11 | Dribbling state trigger |
| Goalkeeper Mechanics | Spec #10 | Goalkeeper-specific movement (excluded from this spec) |
| Fixed64 Math Library | Spec #8 | Type definitions for migration |
| Master Development Plan | Section 1 | Stage 0â†’1â†’2â†’...â†’6 dependency chain |
| Master Vol 1, Section 2.2 | â€” | Kinetic profiles (heel-striker, forefoot, neutral) |
| Master Vol 1, Section 4.1 | â€” | Pitch degradation model |
| Master Vol 1, Section 4.2 | â€” | Weather system (wind, rain, temperature) |
| Master Vol 2, Section 3 | â€” | H-Gate happiness system |
| Master Vol 2, FormSystem | â€” | Form calculation for 6.3.1 |

---

### 6.9 Known Risks and Open Questions

The following items require decisions during future stage planning. They are documented here so they are not forgotten.

**KR-1: Dribbling modifier value validation.**
The placeholder values in 6.1.2 (Ã—0.85 pace, Ã—0.80 agility, Ã—0.90 acceleration) are unsourced estimates. If used without validation, they may create unrealistic gameplay. **Recommendation:** Stage 1 planning must include either (a) literature review for GPS tracking studies comparing dribbling vs. open-run speeds, or (b) gameplay testing protocol with A/B comparison. Do not ship dribbling modifiers without validation.

**KR-2: SurfaceType enum synchronization.**
Agent Movement and Ball Physics both depend on `SurfaceType` enum (6.2.1). If either spec adds enum values independently, deserialization will break. **Recommendation:** Before Stage 2, establish `SurfaceType` as a shared definition in `TacticalDirector.Core.Physics.Common` namespace. Both specs import from shared location. Same mitigation as Ball Physics KR-6.

**KR-3: Fixed64 budget overage.**
Section 6.4.1 estimates Fixed64 migration will exceed the 3.0ms frame budget (5.5â€“7.7ms for 22 agents). This is the highest-risk extension. **Recommendation:** P2 optimizations (SIMD, SoA) and LOD system (6.4.2) must be implemented and validated before Fixed64 migration begins. Fixed64 migration should not start until combined optimizations demonstrate <3.0ms is achievable.

**KR-4: LOD tier boundary discontinuities.**
When an agent crosses the 20m or 40m threshold from ball, physics fidelity changes abruptly. This could cause visible "pops" in agent behavior. **Recommendation:** Hysteresis zones (e.g., LOD 0â†’1 at 22m, LOD 1â†’0 at 18m) are included in 6.4.2 design. Monitor during Stage 3 playtesting for effectiveness.

**KR-5: LOD 2 determinism with 30Hz update.**
LOD 2 agents update at 30Hz while LOD 0/1 agents update at 60Hz. Frame counting must be deterministic â€” if agent A is LOD 2 on frame N, it must be LOD 2 on frame N in replay. **Recommendation:** LOD tier assignment must be recorded in replay data OR derived deterministically from ball position (which is recorded). Verify determinism before shipping LOD 2.

**KR-6: PitchConfiguration ownership migration.**
Section 3.6.5 defines `PitchConfiguration` in Agent Movement namespace. Section 4.1.1 notes it should migrate to shared namespace for Collision System. If migration happens mid-Stage-1, it breaks in-progress work. **Recommendation:** Execute migration as first task of Collision System implementation (Spec #3), before any collision code references pitch dimensions.

**KR-7: Goalkeeper LOD exception.**
Should goalkeepers always be LOD 0 regardless of ball distance? A goalkeeper 50m from ball during opponent's attack is still tactically critical (positioning, communication). **Recommendation:** Add `AlwaysLOD0` flag to goalkeeper agents. This is now documented in 6.4.2.

**KR-8: Animation system feedback loop.**
Section 6.1.1 states animation is a "pure consumer" with no feedback into physics. But what if animation timing affects gameplay perception (e.g., player expects tackle to land based on animation)? **Recommendation:** Stage 1 animation implementation must document any cases where animation timing diverges from physics timing. Animation system must never alter physics state â€” only consume it.

**KR-9: Form/Psychology modifier interaction.**
In Stage 4, both FormModifier and PsychologyModifier will be active simultaneously. Their multiplicative combination could produce extreme values:
- Best case: 1.10 Ã— 1.05 = 1.155 (+15.5% effective attributes)
- Worst case: 0.85 Ã— 0.85 = 0.7225 (-27.75% effective attributes)

The worst case may feel punishing. **Recommendation:** Consider clamping total modifier product to [0.75, 1.15] range, or using additive rather than multiplicative combination. Decide during Stage 4 planning.

**KR-10: Section 5 dependency.**
Multiple values in this section reference Section 5 (Performance Analysis) which is not yet written. Specific pending items:
- Per-agent budget ceiling (0.14ms p99) â€” placeholder
- Total frame budget (3.0ms) â€” placeholder
- Operation counts for Fixed64 estimates â€” derived from Section 5

**Recommendation:** After Section 5 completion, review Section 6 for consistency and update any conflicting values.

---

**END OF SECTION 6**

---

