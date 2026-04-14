## 7.6 Cross-Spec Dependency Map for Extensions

| Extension | Depends On | Dependency Status |
|-----------|-----------|-------------------|
| §7.1.1 Teammate occlusion | Collision System #3 spatial hash (already approved) | ✓ Available |
| §7.1.2 Continuous ConfidenceScore | Section 5 CE-* test series (Stage 1 amendment) | Planned |
| §7.1.2 PerceivedPosition error | Decision Tree Spec #8 must tolerate non-ground-truth positions | ⚠ Pending |
| §7.1.3 Ball trajectory prediction | Ball Physics #1 `BallState.Velocity` (already approved) | ✓ Available |
| §7.1.3 Ball trajectory prediction | Section 3 struct amendment: `BallPerceivedVelocity` field | Planned |
| §7.1.4 Goalkeeper extension | Goalkeeper Mechanics Spec #11 (not written) | ⚠ Pending |
| §7.1.5 Context-sensitive shoulder check | Decision Tree Spec #8 (not written) | ⚠ Pending |
| §7.1.6 Peripheral degradation curve | §7.1.2 continuous ConfidenceScore (Stage 1 Phase 2) | Sequential |
| §7.1.7 AnimData consumer | Animation System specification (not designed) | ⚠ Pending |
| §7.1.8 Event Bus integration | Event System Spec #17 (not written) | ⚠ Pending |
| §7.2.1 Weather effects | Environmental System specification (not designed) | ⚠ Pending |
| §7.2.2 Communication arc | Communication System specification (not designed) | ⚠ Pending |
| §7.2.2 Crowd noise | Match State specification (not designed) | ⚠ Pending |
| §7.2.3 Referee awareness | Referee System specification (not designed) | ⚠ Pending |
| §7.3.1 Form modifier | Form System (Master Vol 2, not written) | ⚠ Pending |
| §7.3.2 H-Gate psychology | H-Gate System (Master Vol 2, not written) | ⚠ Pending |
| §7.3.3 Injury degradation | Injury System (not designed) | ⚠ Pending |
| §7.3.4 Fixed64 migration | Fixed64 Math Library Spec #9 (not written) | ⚠ Pending |

---

## 7.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All Section 5 tests continue to pass** with default parameters
   (`FormModifier = 1.0f`, `PsychologyModifier = 1.0f`, `InjuryLevel = 0.0f`,
   `WeatherPerceptionModifier = 1.0f`, `TeammateOcclusionEnabled = false`).
   Extensions add new test series (`CE-*`, `FM-*`, `INJ-*`, `HG-*`) but must not
   break the existing `FOV-*`, `OCC-*`, `LR-*`, `BS-*`, `BP-*`, `PS-*`, `FR-*`,
   `SNP-*`, `DET-*`, `IT-*`, `BAL-*`, and `PERF-*` suite.

2. **`PerceptionSnapshot` struct is additive only.** New nullable or defaulted fields
   may be appended. Existing field offsets must not change. Decision Tree consumers
   reading only Stage 0 fields must continue to compile and produce identical results.
   Field additions require a formal Section 3 version increment.

3. **`PerceivedAgent` struct is additive only.** Same constraint as above. `AgentId`,
   `PerceivedPosition`, `PerceivedVelocity`, `ConfidenceScore`, `IsInBlindSide`, and
   `LatencyCounterAtConfirmation` field positions are frozen.

4. **`PerceptionSystem.OnHeartbeat()` signature is frozen.** The entry point called by
   the Simulation Scheduler must not change its signature. Internal delegate or
   parameters may change if the signature change is isolated to the scheduler contract,
   but this requires a formal Section 4 amendment.

5. **Determinism is never compromised.** Any extension introducing non-determinism —
   `System.Random`, time-dependent values, platform-specific floating-point behaviour
   not covered by the Fixed64 migration — is a blocking defect regardless of stage.
   The `matchSeed + observerId + targetId + frameNumber` hash must remain the sole
   source of per-agent-pair variance. This constraint is absolute.

6. **Zero-allocation policy persists in the hot path.** No extension may introduce
   heap allocations inside `PerceptionSystem.ComputeAgentPerception()`. Extensions
   that query external providers (weather, context, form) must return value types.
   This constraint is absolute.

---

## 7.8 Known Risks for Future Stages

| ID | Risk | Severity | Stage Affected | Mitigation |
|----|------|----------|----------------|------------|
| KR-1 | Teammate occlusion cost: adding 11 teammate shadow cones per candidate raises worst-case per-agent cost by ~80µs. Without Burst Compiler (P3-A, §6.7), Stage 1 budget may be exceeded | High | Stage 1 | Activate P3-A Burst annotation before Stage 1 teammate occlusion is merged. Measure on reference hardware. |
| KR-2 | PerceivedPosition error → Decision Tree assumption break: if DT Spec #8 is written assuming ground truth positions, Stage 1 PerceivedPosition error will break DT unit tests | High | Stage 1 | Document in Decision Tree Spec #8 (during its Section 2 drafting) that `PerceivedPosition ≠ TruePosition` at Stage 1+. Add DT integration test that validates DT correctness with non-zero error before Stage 1 position error is merged. |
| KR-3 | Ball trajectory prediction → stale velocity: `BallPerceivedVelocity_last` may be extremely stale (ball velocity at occlusion entry) and produce wildly incorrect projections at `T_PREDICT_MAX` | Medium | Stage 1 | Confirm the prediction horizon cap `T_PREDICT_MAX` [GT, ~1.5s] is aggressive enough. Add integration test `BTP-001` for T=1.5s projection accuracy. |
| KR-4 | Full modifier stack: poor form + high injury + severe pressure + low Decisions may push `EffectiveFoV_HalfAngle` below `MIN_FOV_HALF_ANGLE` floor, producing agents with near-zero awareness | Medium | Stage 3+ | Implement `MIN_FOV_HALF_ANGLE` [GT, ~30°] floor before any modifier is activated. Add integration test `FS-001` for worst-case stack before Stage 3 begins. |
| KR-5 | Context-sensitive shoulder check → DT feedback loop: if DT influences check urgency and check results influence DT decisions, heartbeat evaluation order within a single 10Hz tick determines observable outcomes. Unresolved order → non-deterministic emergent behaviour | **Critical** | **Pre-Spec #8** | Resolve heartbeat evaluation order (Perception before DT, or DT before Perception) in the Master Development Plan **before Decision Tree Specification #8 Section 2 is drafted**. This is not merely a Stage 1 concern — it affects DT architectural assumptions from the first section. See §7.1.5 for read-only `IContextProvider` pattern that enforces correct unidirectional flow once order is resolved. |
| KR-6 | Fixed64 Asin accuracy near small r/large d: `Asin(r_occ / d_occ)` at (r=0.5m, d=50m) = ~0.01 rad — small angle where Fixed64 accuracy is most sensitive. Inaccurate Asin here widens shadow cones spuriously, causing false occlusion | Medium | Stage 3+ | Include (r=0.5m, d=50m) as a mandatory accuracy test case in Fixed64 Library Spec #9 acceptance criteria. Acceptance threshold: < 0.001 rad error. |
| KR-7 | Goalkeeper perception extension → struct versioning: if GK Mechanics Spec #11 chooses subtype approach (§7.1.4 option b), `GoalkeeperPerceptionSnapshot` layout must be agreed before #11 Section 3 is drafted. Late changes to the base struct invalidate the subtype | High | Stage 1 | Lock base `PerceptionSnapshot` struct before Goalkeeper Mechanics Spec #11 Section 2 is completed. No base struct amendments after #11 begins. |
| KR-8 | Communication arc → snapshot population ownership: if Communication System can add entities to a receiver's snapshot, the Perception System's invariant "I am the sole populator of PerceivedAgents" is violated | Medium | Stage 2 | Establish explicit ownership: Perception System provides the snapshot builder interface; Communication System calls an add method on it rather than modifying the snapshot directly. Document in Communication System spec draft. |

---

## 7.9 Section Summary

| Subsection | Key Content |
|------------|-------------|
| **7.1 Stage 1** | Teammate occlusion, continuous confidence/position error, ball trajectory prediction, GK extension, context shoulder check, peripheral degradation, AnimData consumer, Event Bus hookup |
| **7.2 Stage 2** | Weather/fog range reduction, crowd noise + communication arc, referee awareness |
| **7.3 Stage 3+** | Form modifier, H-Gate psychology, injury degradation, Fixed64 migration |
| **7.4 Permanent Exclusions / Stage 0 Deferrals** | §7.4.1 PerceptionState enums (parametric model violated); §7.4.2 gameplay cone rendering (immersion/exploit risk); §7.4.4 DT-managed shoulder check scheduling (circular dependency, boundary violation). §7.4.3 is a Stage 0 deferral only — activates at Stage 1 via §7.1.2. |
| **7.5 Architectural Hooks** | 13 Stage 0 hooks enumerated with current values and activation stages |
| **7.6 Cross-Spec Dependencies** | 18 extensions mapped to upstream dependency and status |
| **7.7 Backwards Compatibility** | 6 invariants that must be preserved across all future stages |
| **7.8 Known Risks** | 8 risks identified with severity ratings and mitigations |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| OQ-1 | Teammate occlusion deferred — §7.1.1 | ✓ | Outline v1.1 OQ-1 fully addressed |
| OQ-2 | Ball L_rec exemption — permanent; no §7 entry | ✓ | Design decision not reversed at any stage |
| OQ-3 | DT shoulder check request rejected — §7.4.4; Stage 1 path §7.1.5 | ✓ | Both aspects of OQ-3 addressed |
| OQ-5 | MAX_PERCEPTION_RANGE = 120f as named constant — §7.2.1 | ✓ | Stage 2 weather extension documented |
| §1.3.2 Stage 1+ deferrals | Teammate occlusion, weather, crowd noise, referee, peripheral degradation, context shoulder check | ✓ | All 6 deferrals addressed in §7.1–§7.2 |
| §1.3.3 Permanent exclusions | PerceptionState enum, gameplay cone rendering, per-entity fidelity at Stage 0 | ✓ | §7.4.1 and §7.4.2 are permanent rejections. §7.4.4 is also permanent. §7.4.3 is a Stage 0 deferral — §1.3.3 language confirmed as Stage-0-only; §7.4 preamble clarifies. |
| §3.4.4 ShoulderCheckAnimData stub | Stage 1 consumer activation — §7.1.7 | ✓ | Struct confirmed complete; no changes required |
| §3.7.2 PerceivedAgent ConfidenceScore note | Stage 1 continuous upgrade — §7.1.2 | ✓ | Consistent |
| §3.7.2 PerceivedAgent PerceivedPosition note | Stage 1 error model — §7.1.2 | ✓ | Consistent |
| §3.10 MAX_PERCEPTION_RANGE [GT] | §7.2.1 weather modifier | ✓ | Named constant pattern confirmed |
| §4.6.5 EventBusStub | §7.1.8 replacement at Stage 1 | ✓ | Stub replace-not-remove noted |
| §6.7 P3-A Burst optimisation | KR-1 mitigation prerequisite | ✓ | Linked correctly |
| §6.10 Fixed64 migration notes | §7.3.4 migration strategy | ✓ | §6.10 is authoritative; §7.3.4 references it |
| Ball Physics #1 §7.4 | Fixed64 wrapper pattern | ✓ | §7.3.4 migration follows same approach |
| Agent Movement #2 §3.5.6 | FormModifier, PsychologyModifier, InjuryLevel fields | ✓ | §7.3.1, §7.3.2, §7.3.3 reference these fields |
| Perception System Outline §7 | All outline items — Stages 1, 2, 3, permanent exclusions | ✓ | All outline items covered; section expanded per Shot/Pass Mechanics §7 precedent |
| Pass Mechanics #5 §7 | Section 7 structural template | ✓ | Preamble, hooks table, backwards compatibility, risk log all follow precedent |
| Shot Mechanics #6 §7 | Section 7 structural template | ✓ | Comparison table, complexity rating, extended risk format follow Shot Mechanics §7 |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 26, 2026, 8:00 PM PST | Claude (AI) / Anton | Initial draft. 8 Stage 1, 3 Stage 2, 4 Stage 3+ extensions. 4 §7.4 entries (mislabelled as "4 permanent exclusions" — corrected in v1.1). 13 architectural hooks. 18 cross-spec dependencies. 6 backwards-compatibility invariants. 8 known risks. |
| 1.1 | February 26, 2026, 9:00 PM PST | Claude (AI) / Anton | Six fixes: (1) `AnticipatioScalar` typo corrected to `AnticipationScalar` in §7.1.3 formula. (2) §7.1.5 ContextProvider language tightened — clarified as read-only query interface (Perception queries context; DT does not send commands); KR-5 blocking pre-requisite for DT Spec #8 Section 2 made explicit. (3) KR-5 severity upgraded to Critical and stage moved to Pre-Spec #8 — risk window is earlier than previously stated. (4) Open Dependency Flag for Communication System corrected from §7.2.3 to §7.2.2. (5) §7.4.3 title corrected from "Rejected" to "Stage 0 Only (Activates Stage 1)" — §7.4 preamble updated to distinguish 3 permanent rejections from 1 Stage 0 deferral. (6) §7.9 summary updated to reflect corrected §7.4 categorisation. |

---

*End of Section 7 — Perception System Specification #7 (v1.1)*

*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*

*Next: Section 8 — References*
