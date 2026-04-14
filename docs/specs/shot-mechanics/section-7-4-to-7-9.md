## 7.4 Permanently Excluded Features

The following features are permanently excluded from Shot Mechanics. They are documented
here to provide explicit rejection rationale and to prevent future proposals from
re-raising resolved questions without new evidence.

---

### 7.4.1 Probabilistic Outcome Rolls (Rejected)

**Proposal:** Use a random number generator to determine shot outcome quality, success
probability, or trajectory deviation.

**Why rejected:**

1. **Determinism is a hard architectural requirement** (Master Vol 1 §1.3, KD-4). Any
   non-seeded RNG produces different outputs on replay, corrupting match replay fidelity
   entirely. This is a blocking constraint, not a preference.

2. **The deterministic hash approach already produces apparent variance** from identical
   inputs. Two shots with identical parameters from two different agents on the same frame
   produce different error vectors because `agentId` is a hash component. This is
   statistically indistinguishable from random to the observer while being
   reproducible on replay.

3. **Probabilistic rolls obscure cause and effect.** A shot that misses because of a
   probability roll cannot be explained to the player. A shot that misses because
   `ErrorAngle = 4.2°` due to `BodyMechanicsScore = 0.31` is explainable, reviewable,
   and tunable.

**Alternative:** The deterministic hash function (`matchSeed + agentId + frameNumber`)
provides all required variance without sacrificing determinism. This is already
implemented in §3.6.9.

---

### 7.4.2 ShotType Enum (Rejected)

**Proposal:** Add a `ShotType` enum to `ShotRequest` or `ShotResult` with values such
as `DRIVEN`, `CHIP`, `FINESSE`, `VOLLEY`, `TOE_POKE`.

**Why rejected:**

1. **Shot labels are Decision Tree vocabulary, not physics primitives** (KD-3, Outline
   v1.2 OI-006). Shot Mechanics translates physical intent parameters to ball vectors;
   it does not classify intent. Named shot types carry zero physics weight.

2. **All named shot types are fully representable by parameter combinations.** A `CHIP`
   is `BelowCentre` contact + high `SpinIntent`. A `DRIVEN` is `Centre` contact + high
   `PowerIntent` + low `SpinIntent`. Adding an enum that encodes what the parameters
   already encode creates false coupling and a maintenance surface.

3. **Consistent with the Ball Physics model.** `Ball.ApplyKick()` operates on velocity
   and spin vectors only. A `ShotType` field would be stripped before the ball physics
   calculation anyway — it has no downstream consumer inside the physics layer.

**If a `ShotType` field is found in any Shot Mechanics file, it is a specification
violation and must be reverted.**

---

### 7.4.3 IGkResponseSystem Inside Shot Mechanics (Rejected)

**Proposal:** Define an `IGkResponseSystem` interface inside Shot Mechanics for the
goalkeeper to respond to shots.

**Why rejected:**

1. **The interface principle of this project** (documented throughout all specifications)
   states: write the interface when both sides are specified. Goalkeeper Mechanics
   (Spec #11) is not yet written. An interface defined today would be speculative and
   likely incorrect.

2. **The event bus is the correct decoupling mechanism.** Goalkeeper Mechanics will
   subscribe to `ShotExecutedEvent` when it is built. Shot Mechanics publishes one event
   per shot — already the case at Stage 0. No additional interface is needed.

3. **`ShotExecutedEvent` already carries all fields GK Mechanics requires** (KD-7):
   `KickVelocity`, `BodyMechanicsScore`, `EstimatedTarget`, `StumbleTriggered`,
   `ContactFrame`. No amendment to the event struct should be needed.

**When Goalkeeper Mechanics Spec #11 is drafted:** It defines its own subscription
pattern. If `ShotExecutedEvent` is missing a field GK Mechanics needs, the correct
resolution is an amendment to `ShotExecutedEvent` via a formal specification change —
not an `IGkResponseSystem` coupling.

---

### 7.4.4 Goal Detection Inside Shot Mechanics (Rejected)

**Proposal:** Add goal boundary checking and match-state update logic to
`ShotExecutor.Execute()`.

**Why rejected:**

1. **Goal detection requires geometry checking against goal boundaries, offside line
   position, and referee system state.** None of these are owned by Shot Mechanics.
   Adding them would expand scope beyond the translation-of-intent-to-ball-vector
   responsibility stated in §1.2.

2. **Match Referee and Event System are the correct owners.** `ShotExecutedEvent` carries
   `KickVelocity` and `EstimatedTarget` — sufficient for downstream goal detection.
   Goal detection subscribes to the ball physics simulation, not to this system.

3. **Cross-system coupling risk.** If goal detection is inside Shot Mechanics, then
   every change to goal geometry, offside rules, or VAR logic requires amendments to
   Shot Mechanics — a specification that has nothing to do with these concepts.

---

## 7.5 Architectural Hooks Summary

A consolidated reference showing which Stage 0 fields and code sites serve as extension
points for future stages. Implementers must not remove or repurpose these fields even if
they appear unused at Stage 0.

| Hook | Location | Current Stage 0 Value | Consumed / Activated At |
|------|-----------|-----------------------|-------------------------|
| `ShotAnimationData` stub struct | §2.4.4 | Populated; unconsumed | Stage 1 (§7.1.1) |
| `ShotAnimationData.WindupFrames` | §2.4.4 | Calculated estimate | Stage 1 — Animation System authoritative value |
| `ContactZone` enum (sized for extension) | §2.4.1 | 3 values (Centre, BelowCentre, OffCentre) | Stage 1 (§7.1.2) |
| `ShotExecutedEvent` xG-reserved fields | §2.4.3 | Populated; unconsumed | Stage 1 (§7.1.4) |
| `SetPieceContext` nullable field (planned) | §2.4.1 (Stage 1 addition) | N/A at Stage 0 | Stage 1 (§7.1.3) |
| `BallState.SurfaceType` read (no-op) | §3.1 | `GRASS_DRY` | Stage 2 (§7.2.1) |
| `surfaceModifier = 1.0f` no-op | `VelocityCalculator.cs` | `1.0f` | Stage 2 (§7.2.1) |
| `STUMBLING`/`DIVING` hard reject → penalty | §3.1 VR-004 | Rejects | Stage 2 (§7.2.2) |
| `IsPenaltyKick` flag (planned) | §2.4.1 (Stage 2 addition) | N/A at Stage 0 | Stage 2 (§7.2.3) |
| `PsychologyPressureScale` default 1.0f | §3.6.5 | `1.0f` | Stage 2 simplified (§7.2.3); Stage 4 H-Gate (§7.3.2) |
| `PlayerAttributes.FormModifier` | Agent Movement §3.5.6 | `1.0f` | Stage 3 (§7.3.1) |
| `injuryModifier = 1.0f` no-op | `VelocityCalculator.cs` | `1.0f` | Stage 4 (§7.3.3) |
| `PlayerAttributes.InjuryLevel` read (no-op) | §3.2, §3.7 | `0.0f` | Stage 4 (§7.3.3) |
| `PlayerAttributes.PsychologyModifier` | Agent Movement §3.5.6 | `1.0f` | Stage 4 (§7.3.2) |
| Static-only calculator classes | `VelocityCalculator.cs` et al. | `float` | Stage 5 Fixed64 (§7.3.4) |
| `float` → `Fixed64` conversion at event publish | `ShotExecutor.Execute()` | N/A | Stage 5 (§7.3.4) |

---

## 7.6 Cross-Spec Dependency Map for Extensions

| Extension | Depends On | Dependency Type | Status | Stage |
|-----------|-----------|-----------------|--------|-------|
| §7.1.1 Animation Integration | Animation System (TBD) | Event Bus consumer | ⚠ Pending | Stage 1 |
| §7.1.2 Extended ContactZone | Decision Tree Spec #8 | Intent parameter expansion | ⚠ Pending | Stage 1 |
| §7.1.2 Extended ContactZone | Biomechanics literature (Lees & Nolan 1998) | `ContactZoneModifier` values | ✓ Available | Stage 1 |
| §7.1.3 Set Piece Stub | Set Pieces System (TBD) | `ShotRequest` caller | ⚠ Pending | Stage 1 |
| §7.1.3 Set Piece Stub | Decision Tree Spec #8 | Caller boundary confirmed | ⚠ Pending | Stage 1 |
| §7.1.4 xG Hookup | Statistics Engine (TBD) | Event Bus consumer | ⚠ Pending | Stage 1 |
| §7.2.1 Surface Effects | PitchConditionSystem (shared) | Modifier lookup | ⚠ Pending | Stage 2 |
| §7.2.1 Surface Effects | Ball Physics Spec #1 §7.2 | `SurfaceType` enum | ✓ Confirmed | Stage 2 |
| §7.2.2 Off-Balance Shots | Agent Movement Spec #2 | `STUMBLING`/`DIVING` states | ✓ Confirmed | Stage 2 |
| §7.2.3 Penalty Ritual | Set Pieces System (TBD) | `IsPenaltyKick` flag | ⚠ Pending | Stage 2 |
| §7.2.3 Penalty Ritual | H-Gate System (Master Vol 2) | `MatchStressLevel` (simplified) | ⚠ Pending | Stage 2 |
| §7.2.4 Sound Hooks | Audio System (TBD) | Event Bus consumer | ⚠ Pending | Stage 2 |
| §7.3.1 Form Modifier | Form System (Master Vol 2) | `FormModifier` field population | ⚠ Pending | Stage 3 |
| §7.3.1 Form Modifier | Agent Movement Spec #2 §3.5.6 | `PlayerAttributes.FormModifier` field | ✓ Confirmed | Stage 3 |
| §7.3.2 H-Gate Psychology | H-Gate System (Master Vol 2) | `PsychologyModifier` population | ⚠ Pending | Stage 4 |
| §7.3.2 H-Gate Psychology | Agent Movement Spec #2 §3.5.6 | `PlayerAttributes.PsychologyModifier` | ✓ Confirmed | Stage 4 |
| §7.3.3 Injury Degradation | Injury System (TBD) | `InjuryLevel` population | ⚠ Pending | Stage 4 |
| §7.3.4 Fixed64 Migration | Fixed64 Math Library Spec #9 | Type substitution | ⚠ Pending | Stage 5 |
| §7.3.4 Fixed64 Migration | Ball Physics §7.4 | Migration pattern reference | ✓ Confirmed | Stage 5 |
| §7.3.4 Fixed64 Migration | Agent Movement §6.4.1 | Shared library approach | ✓ Confirmed | Stage 5 |

---

## 7.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All 104 Section 5 tests continue to pass** with default parameters
   (`ContactZone = Centre`, `SurfaceType = GRASS_DRY`, `FormModifier = 1.0f`,
   `PsychologyModifier = 1.0f`, `InjuryLevel = 0.0f`, `IsWeakFoot = false`,
   `IsPenaltyKick = false`). Extensions add new test series (`FM-*`, `INJ-*`, `OB-*`,
   `SP-*`) but must not break the existing `PV-*`, `SV-*`, `LA-*`, `SN-*`, `SP-*`,
   `SE-*`, `BM-*`, `WF-*`, `SSM-*`, `EC-*`, `IT-*`, `VS-*` suite.

2. **`ShotExecutor.Execute()` signature is additive only.** The method signature may
   gain optional parameters but must never remove or reorder existing parameters.
   Callers using the Stage 0 signature (Decision Tree Spec #8) must continue to compile
   and produce identical results.

3. **`ShotRequest` struct is additive only.** New nullable fields may be appended.
   Existing field offsets must not change. The `ContactZone` enum integer values 0, 1, 2
   are frozen — new enum members must be assigned values ≥ 3 to preserve serialised
   replay compatibility.

4. **`ShotExecutedEvent` struct is additive only.** New fields may be appended.
   Existing field offsets must not change. Consumers that only read Stage 0 fields
   (Goalkeeper Mechanics Spec #11 when written; Statistics Engine at Stage 1) must
   continue to work without recompilation.

5. **Determinism is never compromised.** Any extension that introduces
   non-determinism — `System.Random`, time-dependent values, platform-specific floating
   point behaviour not covered by Fixed64 migration — is a blocking defect regardless
   of stage. The deterministic hash function (`matchSeed + agentId + frameNumber`) must
   be preserved as the sole source of per-shot variance. This constraint is absolute.

6. **Zero-allocation policy persists in the hot path.** No extension may introduce heap
   allocations inside `ShotExecutor.Execute()`. Extensions that require interface queries
   (e.g., `IAnimationShotDurationProvider.GetWindupFrames()`) must return value types
   and must not allocate. This constraint is absolute.

---

## 7.8 Known Risks for Future Stages

| ID | Risk | Severity | Stage Affected | Mitigation |
|----|------|----------|----------------|------------|
| KR-1 | Animation frame timing: `WindupFrames` sync between state machine and Animation System may drift if frame pipeline ordering is not resolved | High | Stage 1 | Resolve frame pipeline ordering document before Stage 1 implementation begins (§7.1.1 architecture note) |
| KR-2 | Extended `ContactZone` integer values: new enum members added with wrong integer assignments break replay deserialisation for matches recorded at Stage 0 | High | Stage 1 | Enforce frozen integer assignments (0, 1, 2) in §7.1.2; add serialisation regression test at Stage 1 |
| KR-3 | Set piece physics path: if Set Pieces System creates a `SetPieceExecutor` rather than calling `ShotExecutor`, shot physics diverge between open play and set pieces | High | Stage 1 | Enforce §7.1.3 architecture note — single `ShotExecutor` for all foot-contact shots |
| KR-4 | Full modifier stack on wet pitch + weak foot + poor body shape + high fatigue produces extreme `V_BASE` reduction, possibly below `V_MIN` clamp — resulting in unrealistic rolling shots | Medium | Stage 2 | Integration test the full stack with minimum attribute values. Confirm `V_MIN` clamp (§3.2) is adequate. Consider a `COMBINED_MODIFIER_FLOOR` constant if needed |
| KR-5 | Penalty pressure + H-Gate stack: simplified `1.35f` penalty baseline (Stage 2) and H-Gate `PsychologyPressureScale` (Stage 4) may double-count pressure if both are active simultaneously | Medium | Stage 4 | When H-Gate is active, verify the `IsPenaltyKick` pathway is superseded. Add integration test `PK-H-001` |
| KR-6 | Off-balance shot always sets `StumbleTriggered = true` for `DIVING` state — `ShotExecutedEvent` consumers (GK Mechanics) must correctly interpret this as a trajectory irregularity flag, not a failed shot | Medium | Stage 2 | Confirm with GK Mechanics Spec #11 during drafting that `StumbleTriggered = true` semantics are as documented in §2.4.3 |
| KR-7 | Fixed64 `Exp()` accuracy for sigmoid blend: the distance sigmoid σ(x) is sensitive near its inflection point (`D_MID = 20m`). Fixed64 `Exp()` accuracy here determines whether attribute blending changes materially post-migration | Medium | Stage 5 | Include sigmoid accuracy test in Fixed64 library evaluation criteria (§7.3.4). Accept < 1% output difference as pass threshold |
| KR-8 | Form + injury + psychology full-stack: worst-case combination of all three Stage 3+/4 modifiers may produce shots that appear broken (near-zero velocity, maximum error cone) rather than merely poor. May require a `MINIMUM_SHOT_QUALITY_FLOOR` constant | Low | Stage 4 | Add full-stack integration test `FS-001` at Stage 4 start. Floor constant is a `[GT]` value if needed |

---

## 7.9 Section Summary

| Subsection | Key Content |
|------------|-------------|
| **7.1 Stage 1** | Animation stub activation, extended ContactZone, set piece interface boundary, xG event hookup |
| **7.2 Stage 2** | Surface condition modifiers, off-balance/diving shot states, penalty kick ritual, sound hooks |
| **7.3 Stage 3+** | Form modifier, H-Gate psychology, injury degradation, Fixed64 migration |
| **7.4 Permanent Exclusions** | Probabilistic outcome rolls (violates determinism), ShotType enum (already eliminated), IGkResponseSystem (premature coupling), goal detection (scope violation) |
| **7.5 Architectural Hooks** | 15 Stage 0 hooks enumerated with current values and activation stages |
| **7.6 Cross-Spec Dependencies** | 18 extensions mapped to their upstream spec dependencies |
| **7.7 Backwards Compatibility** | 6 invariants that must be preserved across all future stages |
| **7.8 Known Risks** | 8 risks identified with severity ratings and mitigations |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| Ball Physics #1 §7.4 | Fixed64 migration pattern | ✓ | §7.3.4 migration strategy follows same approach |
| Ball Physics #1 §7.2 | `SurfaceType` enum | ✓ | §7.2.1 references `SurfaceType` for surface modifier lookup |
| Agent Movement #2 §3.5.6 | `FormModifier`, `PsychologyModifier`, `InjuryLevel` fields | ✓ | §7.3.1, §7.3.2, §7.3.3 reference these fields |
| Agent Movement #2 §6.4.1 | Fixed64 migration — shared library | ✓ | §7.3.4 confirms shared library approach |
| Agent Movement #2 locomotion states | `STUMBLING`, `DIVING` states | ✓ | §7.2.2 extends §3.1 VR-004 to cover these states |
| Collision System #3 | `GetAndClearTackleFlag()` | ✓ | Not extended; no changes from shot mechanics side |
| Pass Mechanics #5 §7 | Section 7 structural template | ✓ | All 9 subsections follow Pass Mechanics §7 precedent |
| First Touch #4 §7 | Section 7 structural template | ✓ | Preamble and hooks table follow First Touch §7 precedent |
| Shot Mechanics §1.3.2 | Stage 1+ deferrals table | ✓ | All 6 deferrals in §1.3.2 are addressed in §7.1–§7.3 |
| Shot Mechanics §1.3.3 | Permanent exclusions | ✓ | All 4 permanent exclusions in §1.3.3 formally rejected in §7.4 |
| Shot Mechanics §2.4.3 KD-7 | `ShotExecutedEvent` xG fields | ✓ | §7.1.4 confirms these fields are the Stage 1 hookup |
| Shot Mechanics §2.4.4 | `ShotAnimationData` stub | ✓ | §7.1.1 documents the Stage 1 activation |
| Shot Mechanics §3.2 | `FormModifier` no-op site | ✓ | §7.3.1 documents the Stage 3 activation |
| Shot Mechanics §3.6.5 | `PsychologyPressureScale` default 1.0f | ✓ | §7.2.3 (Stage 2), §7.3.2 (Stage 4) document activation |
| Shot Mechanics §6.9 | Fixed64 migration analysis per formula | ✓ | §7.3.4 references §6.9 as authoritative for performance estimates |
| Master Vol 1 §1.3 | Determinism requirement | ✓ | §7.4.1 rejection rationale and §7.7 invariant #5 |
| Master Vol 2 §FormSystem | Form modifier spec | ⚠ Pending | Not yet written; field assumed from Agent Movement §3.5.6 |
| Master Vol 2 §H-Gate | Psychology modifier spec | ⚠ Pending | Not yet written; field assumed from Agent Movement §3.5.6 |
| Shot Mechanics Outline §7 | Outline items addressed | ✓ | All outline §7 items covered; section expanded per First Touch §7 and Pass Mechanics §7 precedent |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 23, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. 4 Stage 1, 4 Stage 2, 4 Stage 3+ extensions. 4 permanent exclusions. 15 architectural hooks. 18 cross-spec dependencies. 6 backwards-compatibility invariants. 8 known risks. |
| 1.1 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11, Fixed64 Math Library #8→#9. (2) Prerequisite Section 3 Part 2 v1.1→v1.2. |

---

*End of Section 7 — Shot Mechanics Specification #6*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*

*Next: Section 8 — References*
