# Perception System Specification #7 — Section 7: Future Extensions

**File:** `Perception_System_Spec_Section_7_v1_1.md`
**Purpose:** Authoritative roadmap for all planned and permanently excluded Perception System
extensions — what changes at each development stage, what architectural hooks exist today,
what risks each extension introduces, and what is permanently excluded. This section is
the single source of truth for Perception System evolution. Future extension references
in Section 1.3.2 (Stage 1+ Deferrals) are summaries derived from this section; if a
conflict exists, **this section takes precedence**.

**Created:** February 26, 2026, 8:00 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisites confirmed:**
- Perception System Specification #7 — Section 1 v1.1 (scope, KD-1 through KD-7 locked)
- Perception System Specification #7 — Section 2 v1.1 (pipeline, functional requirements, structs)
- Perception System Specification #7 — Section 3 v1.1 (all models, constants, stubs defined)
- Perception System Specification #7 — Section 4 v1.1 (architecture and integration contracts)
- Perception System Specification #7 — Section 5 v1.1 (testing)
- Perception System Specification #7 — Section 6 v1.1 (performance analysis, Fixed64 notes)
- Ball Physics Specification #1 (approved) — Fixed64 wrapper pattern (§7.4)
- Agent Movement Specification #2 (approved) — `PlayerAttributes.Decisions`, `Anticipation`,
  `FormModifier`, `PsychologyModifier`, `InjuryLevel` fields confirmed in §3.5.6
- Collision System Specification #3 (approved) — spatial hash API confirmed
- First Touch Mechanics Specification #4 (approved) — PressureScalar formula, half-turn
  bonus (cross-spec constants consumed read-only)
- Pass Mechanics Specification #5 (approved) — Section 7 structural template
- Shot Mechanics Specification #6 (approved) — Section 7 structural template

**Open Dependency Flags:**
- `[PENDING]` — Decision Tree Specification #8 not yet written; §7.1.5 documents the
  planned `ContextAwarenessRequest` interface surface. Does not block this section.
- `[PENDING]` — Fixed64 Math Library Specification #9 not yet written; migration strategy
  assumed from Ball Physics §7.4 and Perception System §6.10. Does not block this section.
- `[PENDING]` — Animation System not yet designed; `ShoulderCheckAnimData` stub (§3.4.4)
  provides the Stage 1 interface surface. Does not block this section.
- `[PENDING]` — Form System (Master Vol 2) not yet written; `FormModifier` field assumed
  from Agent Movement §3.5.6. Does not block this section.
- `[PENDING]` — Communication System (Stage 2+) not yet designed; §7.2.2 documents the
  planned interface boundary. Does not block this section.
- `[PENDING]` — Goalkeeper Mechanics Specification #11 not yet written; §7.1.4 documents
  the extension surface. Does not block this section.
- `[PENDING]` — Event System Specification #17 not yet written; `PerceptionRefreshEvent`
  stub (§4.6.3) is the Stage 0 placeholder. Does not block this section.

---

## Table of Contents

- [Preamble: Role of This Section](#preamble-role-of-this-section)
- [7.1 Stage 1 Extensions (Year 2)](#71-stage-1-extensions-year-2)
  - [7.1.1 Teammate Shadow Cone Occlusion](#711-teammate-shadow-cone-occlusion)
  - [7.1.2 Continuous ConfidenceScore and PerceivedPosition Error](#712-continuous-confidencescore-and-perceivedposition-error)
  - [7.1.3 Ball Trajectory Prediction (Anticipation-Driven)](#713-ball-trajectory-prediction-anticipation-driven)
  - [7.1.4 Goalkeeper Perception Extension](#714-goalkeeper-perception-extension)
  - [7.1.5 Context-Sensitive Shoulder Check Urgency](#715-context-sensitive-shoulder-check-urgency)
  - [7.1.6 Peripheral Vision Degradation Curve](#716-peripheral-vision-degradation-curve)
  - [7.1.7 ShoulderCheckAnimData Consumer Activation](#717-shouldercheckanimdata-consumer-activation)
  - [7.1.8 Real Event Bus Integration](#718-real-event-bus-integration)
- [7.2 Stage 2 Extensions (Years 3–4)](#72-stage-2-extensions-years-34)
  - [7.2.1 Weather and Fog Effects on Perception Range](#721-weather-and-fog-effects-on-perception-range)
  - [7.2.2 Crowd Noise and Communication Arc](#722-crowd-noise-and-communication-arc)
  - [7.2.3 Referee Awareness](#723-referee-awareness)
- [7.3 Stage 3+ Extensions (Years 5–11+)](#73-stage-3-extensions-years-511)
  - [7.3.1 Form Modifier Integration](#731-form-modifier-integration)
  - [7.3.2 H-Gate Psychology and Crowd Pressure](#732-h-gate-psychology-and-crowd-pressure)
  - [7.3.3 Injury State Degradation on Perception Quality](#733-injury-state-degradation-on-perception-quality)
  - [7.3.4 Fixed64 Determinism Migration](#734-fixed64-determinism-migration)
- [7.4 Permanently Excluded Features](#74-permanently-excluded-features)
  - [7.4.1 PerceptionState or AwarenessLevel Enums (Rejected)](#741-perceptionstate-or-awarenesslevel-enums-rejected)
  - [7.4.2 Rendering of Perception Cones in Gameplay (Rejected)](#742-rendering-of-perception-cones-in-gameplay-rejected)
  - [7.4.3 Per-Entity Fidelity Variation — Stage 0 Only (Activates Stage 1)](#743-per-entity-fidelity-variation--stage-0-only-activates-stage-1)
  - [7.4.4 Decision Tree Management of Shoulder Check Scheduling (Rejected)](#744-decision-tree-management-of-shoulder-check-scheduling-rejected)
- [7.5 Architectural Hooks Summary](#75-architectural-hooks-summary)
- [7.6 Cross-Spec Dependency Map for Extensions](#76-cross-spec-dependency-map-for-extensions)
- [7.7 Backwards Compatibility Guarantees](#77-backwards-compatibility-guarantees)
- [7.8 Known Risks for Future Stages](#78-known-risks-for-future-stages)
- [7.9 Section Summary](#79-section-summary)
- [Cross-Reference Verification](#cross-reference-verification)
- [Version History](#version-history)

---

## Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded Perception
System extensions.

**Design philosophy:** Stage 0 Perception was designed with future extensions in mind.
Specific architectural decisions — the `ShoulderCheckAnimData` stub populated but unconsumed
at Stage 0, the `ConfidenceScore` field binary-valued (1.0 or absent) with explicit Stage 1
annotation, the `PerceivedPosition` carrying ground truth with a Stage 1 upgrade note, the
`FormModifier` and `PsychologyModifier` fields available in `PlayerAttributes` but not yet
consumed by the Perception pipeline, the `MAX_PERCEPTION_RANGE = 120f` [GT] constant
preserving the spatial hash architecture while remaining tunable — were made to minimise
structural rework when extensions arrive. This section documents how each extension connects
to existing hooks and identifies gaps where new architecture is required.

**Core constraint that governs all extensions:** Determinism is never negotiable. Every
extension listed in §7.1–§7.3 must preserve bitwise-identical outputs given identical
inputs. The deterministic hash seed — `matchSeed + observerId + targetId + frameNumber` —
used in L_rec noise generation must be preserved. Any extension introducing `System.Random`,
time-dependent values, or platform-specific floating-point behaviour not covered by the
Fixed64 migration is a blocking defect regardless of stage.

**Comparison to sibling specifications:**

| Spec | Extension Complexity | Primary Extension Categories |
|------|---------------------|------------------------------|
| Ball Physics (#1) | Low | Surface types, weather, Fixed64 |
| Agent Movement (#2) | High | Animation, dribbling, LOD, psychology |
| Collision System (#3) | Medium | Aerial collision, body parts, constraint solver |
| First Touch (#4) | Medium | Body parts, surface, skill moves, psychology |
| Pass Mechanics (#5) | Medium | Body parts, formation instructions, set pieces |
| Shot Mechanics (#6) | Medium-High | Animation, set pieces, psychology, off-balance shots |
| **Perception System (#7)** | **High** | Confidence gradients, prediction, teammate occlusion, weather, communication, psychology, Fixed64 |

Perception System carries the highest extension complexity of all Stage 0 specifications
because it is the cognitive substrate for the entire agent model. Every system that makes
agents feel more realistic — psychology, form, fatigue, injury, weather, communication —
has a perception-layer component. Extensions must be carefully sequenced to prevent
specification entanglement before the relevant upstream systems are written.

---

## 7.1 Stage 1 Extensions (Year 2)

Stage 1 extensions expand the realism of the core perception model. All Stage 1 work
extends the existing pipeline structure; no pipeline steps are added or removed.

---

### 7.1.1 Teammate Shadow Cone Occlusion

**Outline reference:** OQ-1 deferral (resolved in Outline v1.1).

**What it adds:** Teammates generate shadow cones from the observer's perspective using the
same angular interval algorithm as opponents (§3.2). An entity occluded by a teammate's
body is excluded from the snapshot, exactly as with opponent occlusion.

**Why deferred:** Stage 0 deferred teammate occlusion because the O(n²) per-agent cost
was not justified by the realism gain during the physics foundation stage (OQ-1 rationale).
At Stage 0, the 10 opponents per observer case is already covered. Extending to all 21
other agents (10 opponents + 11 teammates) doubles the worst-case shadow cone evaluation
budget from ~10 cones to ~21 cones per candidate.

**Architectural hook:** The `ApplyOcclusionFilter()` step (Step 4, §3.0) already accepts
a `nearbyOpponents` collection. Extending it to a `nearbyEntities` collection — encompassing
both teams — requires one collection parameter change and no algorithmic restructuring. The
shadow cone formula (§3.2.2) is entity-agnostic; it does not test team membership.

**Stage 1 implementation note:** When teammate occlusion is activated, update the Section 5
test `OCC-006` to include teammate occluder cases. Add `OCC-007 through OCC-010` covering
teammate chain occlusion (teammate occluding a teammate). Existing `OCC-001 through OCC-005`
tests must continue to pass without modification.

**Performance impact:** Worst case per-agent: 21 shadow cones instead of 10. Approximately
+80µs per agent at Stage 0 timing baseline. Full-22-agent impact: +1.76ms. Budget headroom
at Stage 0 is approximately 1ms surplus (§6.3.1); this extension requires the P3-A
Burst Compiler optimisation (§6.7) to be active before Stage 1 deployment.

---

### 7.1.2 Continuous ConfidenceScore and PerceivedPosition Error

**What it adds:** Two connected changes to `PerceivedAgent`:

1. **`ConfidenceScore`** transitions from binary (1.0 or absent) to a continuous [0.0, 1.0]
   float. Score degrades with distance from observer and with occlusion proximity (how
   close to being occluded). Elite `Decisions` attribute values sustain higher confidence
   at distance; low `Decisions` values degrade more steeply.

2. **`PerceivedPosition`** gains an additive error term. True position is perturbed by a
   vector whose magnitude is scaled by `(1.0 - ConfidenceScore) × MAX_POSITION_ERROR`.
   Error direction uses the same deterministic hash seed (`matchSeed + observerId +
   targetId + frameNumber`) as L_rec noise — no new randomness source is introduced.

**Architectural hook:** Both fields are already defined in the Stage 0 `PerceivedAgent`
struct (§3.7.2) with explicit Stage 1 upgrade annotations. `ConfidenceScore` currently
defaults to `1.0f` for all confirmed entities. `PerceivedPosition` currently copies ground
truth directly. The upgrade is a formula substitution at Step 7 (`BuildPerceptionSnapshot`)
with no struct layout changes.

**Downstream impact on Decision Tree Spec #8:** The Decision Tree must be designed with
the Stage 1 upgrade in mind. When `ConfidenceScore < 1.0`, positions may be imprecise.
Decision Tree authors must not assume `PerceivedPosition == TruePosition`. This constraint
must be documented in Decision Tree Specification #8 before that spec's Section 3 is
drafted, even though the change activates at Stage 1.

**Stage 1 implementation note:** Add test series `CE-001 through CE-010` (Confidence Error
series) to the Section 5 test suite covering: score degradation monotonicity, position
error scaling, deterministic reproducibility, elite vs. average Decisions comparison.
Existing `FOV-*`, `OCC-*`, `LR-*` tests must be unaffected; `ConfidenceScore` is
applied after those filters.

---

### 7.1.3 Ball Trajectory Prediction (Anticipation-Driven)

**What it adds:** When the ball is not currently visible (`BallVisible = false`),
high-`Anticipation` agents receive a predicted `BallPerceivedPosition` derived from
the ball's last known velocity and a linear projection forward in time, rather than
simply retaining the last confirmed static position.

At Stage 0, the stale ball model (§3.5.2) freezes `BallPerceivedPosition` at the last
confirmed snapshot. This produces correct but unrealistic behaviour for a tracking
midfielder who loses the ball behind an occluder: they should continue to mentally
project its path, not assume it stopped.

**Formula (indicative):**
```
PredictedPosition = BallPerceivedPosition_last
                  + BallPerceivedVelocity_last × (CurrentTime - LastVisibleTime)
                  × AnticipationScalar(Anticipation)

AnticipationScalar(A) = 0.3 + (A - 1) / 19 × 0.7  // [GT] — scaled from 0.3 (A=1) to 1.0 (A=20)
```

Prediction accuracy degrades with elapsed time since last visibility. A maximum prediction
horizon `T_PREDICT_MAX` [GT, ~1.5s] caps projection. Beyond this horizon, `BallPerceivedPosition`
reverts to the frozen last-known value.

**Architectural hook:** `PerceptionSnapshot` already carries `BallStalenessFrames` (§3.7.1)
which provides the elapsed-time denominator. `BallPerceivedVelocity` must be added to
the struct at Stage 1 — this is a **struct extension** (additive field only). Existing
consumers reading only Stage 0 fields must not be affected.

**Stage 1 implementation note:** Requires amending `PerceptionSnapshot` struct (§3.7.1)
to add `BallPerceivedVelocity`. This amendment must go through a formal version increment
of Section 3 before Stage 1 implementation begins. The Decision Tree must be notified
that `BallPerceivedPosition` may now be a projection, not a last-known static position.

---

### 7.1.4 Goalkeeper Perception Extension

**What it adds:** Goalkeeper Mechanics Specification #11 (not yet written) will define
goalkeeper-specific perception requirements. Anticipated extensions include:

- Expanded FoV angle for goalkeepers (wider awareness of cross-box movement)
- Specialised ball-flight tracking within the penalty area (continuous high-fidelity
  ball perception even at high speed)
- Shot trajectory prediction from `BallState.Velocity` and Magnus spin data (for dive
  preparation latency modelling)
- Reduced L_rec for opponents in the box (goalkeeper always knows who is in the box)

**Architectural hook:** At Stage 0, the goalkeeper is processed by the standard pipeline
with no special-casing. When Goalkeeper Mechanics (#11) is written, it may either:

(a) **Override constants** — supply a `GoalkeeperPerceptionParams` struct that replaces
    `BASE_FOV_HALF_ANGLE`, `L_MAX`, `L_MIN` for goalkeeperId in the pipeline; no
    structural change to the pipeline itself.

(b) **Subtype the snapshot** — define a `GoalkeeperPerceptionSnapshot : PerceptionSnapshot`
    that extends the struct with goalkeeper-specific fields (e.g. `ShotTrajectoryEstimate`).
    If this path is chosen, it requires a formal amendment to Section 3 (§3.7.1) before
    #11's Section 3 is drafted.

The choice of approach (a) vs (b) is deferred until Goalkeeper Mechanics Specification
#11 is initiated. This specification makes no commitment until both sides are specified.

---

### 7.1.5 Context-Sensitive Shoulder Check Urgency

**Outline reference:** OQ-3, Stage 1 flag.

**What it adds:** At Stage 0, possession is the only context modifier on shoulder check
interval (possession halves the base interval, §3.4.3). Stage 1 extends this to a richer
context map: pressing posture triggers more frequent checks; tracking a runner into the
blind-side arc (detected at the FoV boundary) triggers an immediate reactive check;
set piece dead-ball states suppress checks entirely.

**Why deferred:** Implementing context-sensitive urgency requires the Decision Tree
(Specification #8) to be specified first. The check-interval logic must read tactical
state (pressing, tracking, set piece) that the Perception System does not own at Stage 0.
Building this before #8 is written creates circular dependency risk (OQ-3 resolution).

**Architectural hook:** The `_nextCheckTick` and `_checkWindowExpiry` dictionaries
(§2.3.3) already manage per-agent check state. Urgency context at Stage 1 adds a
`CheckUrgencyModifier` scalar that multiplies the base interval. This scalar is read
by the Perception System from a `IContextProvider` interface — the Perception System
**queries** context; it does not receive commands from the Decision Tree. The flow is
strictly unidirectional: context → perception → snapshot → DT. This preserves the
separation of concerns established in KD-1 and avoids the circular dependency identified
in KR-5. The `IContextProvider` interface definition follows the project principle:
write the interface only when both sides (Perception System and Decision Tree) are
fully specified.

**Blocking pre-requisite (KR-5):** Heartbeat evaluation order — whether Perception
runs before or after the Decision Tree within a single 10Hz tick — must be resolved
before Decision Tree Specification #8 Section 2 is drafted. This ordering determines
whether context changes made by the DT in tick N affect Perception in the same tick N
or in tick N+1. An ambiguous or incorrect answer creates a feedback loop that can
produce non-deterministic observable outcomes. This is a **blocking pre-requisite for
DT Spec #8**, not merely a Stage 1 concern. It should be resolved in the Master
Development Plan before Spec #8 begins.

**Stage 1 implementation note:** Section 4 (Architecture & Integration) must be amended
to document the `IContextProvider` boundary interface, the heartbeat evaluation order
decision, and the read-only query contract before Stage 1 implementation begins. No
interface stub is defined here.

---

### 7.1.6 Peripheral Vision Degradation Curve

**What it adds:** At Stage 0, the FoV model is binary — entities are either inside the
effective half-angle cone or outside it (§3.1.2). Stage 1 introduces a non-linear
clarity gradient: entities near the FoV boundary receive a `ClarityScore` that degrades
with angle from the centreline, modulated by the `Decisions` attribute.

At maximum centreline bearing (0°), `ClarityScore = 1.0`. At the FoV boundary (effective
half-angle), `ClarityScore` reaches a minimum threshold above which recognition is still
possible but latency is extended. Beyond the boundary, `ClarityScore = 0.0` (invisible).

**Architectural hook:** The FoV angle test in `ApplyFieldOfView()` (Step 3, §3.0) currently
discards entities beyond the half-angle with no gradient. Adding a `ClarityScore` output
from this test requires propagating the score into the latency calculation (Step 5) and
ultimately into `PerceivedAgent.ConfidenceScore` (§7.1.2). This extension depends on
§7.1.2 (continuous ConfidenceScore) being implemented first; it is therefore Stage 1
Phase 2, after §7.1.2.

---

### 7.1.7 ShoulderCheckAnimData Consumer Activation

**What it adds:** The `ShoulderCheckAnimData` stub struct (§3.4.4) is defined and populated
at Stage 0 but has no consumer. When the Animation System is designed (Stage 1+), it will
subscribe to this data to trigger the shoulder-check animation on the agent model.

**Architectural hook:** The stub struct fields are already finalised:
- `AgentId` — identifies which agent performed the check
- `FireFrame` — heartbeat tick on which check was triggered
- `CheckDirection` — left or right of rear centreline (degrees)
- `AnyEntityConfirmed` — whether at least one blind-side entity confirmed during the window

No changes to the struct are anticipated. The Animation System's subscription pattern
follows the same model as `ShotAnimationData` in Shot Mechanics (§7.1.1 of that spec).

**Stage 1 implementation note:** When the Animation System specification is drafted,
it must formally consume `ShoulderCheckAnimData`. This specification requires no amendment;
the struct is already complete.

---

### 7.1.8 Real Event Bus Integration

**What it adds:** The `EventBusStub` (§4.6.5) is replaced with the real Event System
(Specification #17) when that specification is written. The `PerceptionRefreshEvent`
published on forced mid-heartbeat refresh will reach actual subscribers.

**Architectural hook:** The `EventBusStub.Publish<T>()` method signature matches the
anticipated real Event Bus API. The replacement is a one-line substitution at the call
site in `PerceptionSystem`. No changes to `PerceptionRefreshEvent` struct are required.

**Stage 1 implementation note:** The stub must be replaced — not removed — when Event
System (#17) is available. If the real Event Bus requires a different publish signature,
this specification requires a corresponding amendment to Section 4 before the replacement
is merged.

---

## 7.2 Stage 2 Extensions (Years 3–4)

Stage 2 extensions add environmental and social context to the perception model.

---

### 7.2.1 Weather and Fog Effects on Perception Range

**Outline reference:** OQ-5 resolution — `MAX_PERCEPTION_RANGE = 120f` [GT] is a named
constant specifically so Stage 2 can reduce it dynamically.

**What it adds:** A `WeatherPerceptionModifier` scalar (supplied by a future Environmental
System) multiplies `MAX_PERCEPTION_RANGE` at runtime. Dense fog reduces the effective
range; light rain adds moderate degradation; clear night play (under floodlights) has
no effect. The pressure-induced FoV narrowing (§3.6) continues to stack with weather
effects additively.

**Indicative model:**
```
EffectiveRange = MAX_PERCEPTION_RANGE × WeatherPerceptionModifier × PressureScalar_range
// WeatherPerceptionModifier: [0.4 (dense fog), 1.0 (clear)] [GT, ranges from Environmental System]
```

**Architectural hook:** `MAX_PERCEPTION_RANGE` is already a named constant (§3.10).
The Stage 2 change replaces its use in Step 2 (`QueryNearbyEntities`) with a
runtime-evaluated `EffectiveRange`. No pipeline restructuring is required.

**Stage 2 implementation note:** The `WeatherPerceptionModifier` constant origin and
supply mechanism must be documented in the Environmental System specification (not yet
designed). Until that spec is written, this remains an architectural intention with no
active interface.

---

### 7.2.2 Crowd Noise and Communication Arc

**What it adds:** Agents can "call" to nearby teammates, extending effective awareness
to entities in the caller's FoV that are not yet in the receiver's FoV. A receiver
agent may have an entity added to their snapshot from a teammate's verbal confirmation
with an elevated L_rec cost (communication adds latency relative to direct visual
confirmation).

This models the "man on" communication mechanic described in Master Volume I §3.1.

**Architectural hook:** Communication awareness represents a new category of snapshot
population — entities added not from the observer's own FoV but from a teammate's
broadcast. These entities require a new field in `PerceivedAgent`: `PerceivedViaComms = true`.
This is a **struct extension** requiring a formal Section 3 amendment.

Crowd noise at high stadium attendance disrupts communication: the effective radius
of the verbal call shrinks with crowd amplitude. This requires a `StadiumNoiseLevel`
scalar from the Match State system (not yet specified).

**Stage 2 implementation note:** This extension cannot proceed until both the
Communication System specification and the Match State specification are drafted.
The Perception System's role is to consume communication events and extend snapshots —
it does not generate the calls. Boundary ownership must be formally established when
those specs are written.

---

### 7.2.3 Referee Awareness

**What it adds:** Agents gain awareness of referee position. This affects tactical
decisions (fouling risk near referee, blocking deliberate fouls from obvious angles).
At Stage 2, referee position would be added to `PerceptionSnapshot` as a standalone
`RefereePerceivedPosition` field rather than as a full `PerceivedAgent` entry.

**Architectural hook:** The referee is not an agent in the Stage 0 model. Adding referee
awareness requires either:
(a) A `RefereePerceivedPosition : Vector2?` nullable field in `PerceptionSnapshot`
(b) Treating the referee as an entity in the candidate set (requires referee to appear
    in the spatial hash)

Approach (a) is preferred: the referee moves differently from agents and does not
participate in the shadow cone model. When Referee System specification is drafted,
this preference must be confirmed or overridden.

---

## 7.3 Stage 3+ Extensions (Years 5–11+)

Stage 3+ extensions integrate deep psychological and physiological systems into the
perception model.

---

### 7.3.1 Form Modifier Integration

**What it adds:** `PlayerAttributes.FormModifier` (already available from Agent Movement
§3.5.6, currently not consumed by the Perception pipeline) is applied to the base FoV
half-angle and the L_rec calculation. Poor form reduces the effective FoV and increases
recognition latency; peak form widens FoV and sharpens recognition.

**Indicative extension:**
```
EffectiveFoV_HalfAngle = BASE_FOV_HALF_ANGLE
                       × (1.0 + (Decisions - 10) / 10 × FOV_DECISIONS_SCALAR)
                       × FovPressureScalar       // §3.1.3
                       × FormModifier            // Stage 3 — [GT] default = 1.0f
```

**Architectural hook:** The pipeline already reads `PlayerAttributes` at Step 1
(`CacheAttributes`). Adding `FormModifier` to the cached attribute set is a one-line
change. The formula extension sites in §3.1.3 and §3.3.2 are already additive
multiplication chains — inserting a further scalar requires no restructuring.

**Stage 3 implementation note:** When Form System (Master Vol 2) is written, validate
that the multiplicative stack (pressure × form × decisions) does not produce a
`EffectiveFoV_HalfAngle` below a defined `MIN_FOV_HALF_ANGLE` floor [GT, ~30°].
Add integration test `FM-001` covering this floor case.

---

### 7.3.2 H-Gate Psychology and Crowd Pressure

**What it adds:** `PlayerAttributes.PsychologyModifier` (available from Agent Movement
§3.5.6) modulates the pressure scalar sensitivity in the FoV narrowing formula (§3.6).
An agent with high psychological resilience (H-Gate score) maintains FoV breadth under
pressure better than a psychologically fragile player.

At Stage 3+, the `PressureScalar_fov` formula (§3.1.3) becomes:

```
PressureScalar_fov = 1.0 - (1.0 - PressureScalar_raw)
                         × (1.0 - PsychologyResistance)
// PsychologyResistance = f(PsychologyModifier) ∈ [0.0, 0.5] [GT]
// High PsychologyModifier → resists FoV degradation under pressure
```

**Architectural hook:** The `PressureScalar` computation at Step 3 (§3.6) already
produces a scalar that is then applied to FoV. The H-Gate modifier inserts a blending
term into this formula without changing its call site. `PsychologyModifier` is read at
Step 1 cache alongside existing attributes.

**Stage 3 implementation note:** When H-Gate Psychology System (Master Vol 2) is
written, verify that the perception psychology modifier does not double-count pressure
with any psychology effects applied at the Decision Tree layer. Add integration test
`HG-P-001` for this interaction.

---

### 7.3.3 Injury State Degradation on Perception Quality

**What it adds:** `PlayerAttributes.InjuryLevel` (available from Agent Movement §3.5.6)
degrades perception quality. A significantly injured agent experiences reduced FoV,
elevated L_rec, and suppressed shoulder check frequency. A minor knock has negligible
effect; a serious injury that leaves an agent on the pitch produces measurable
awareness degradation.

**Indicative extension:**
```
InjuryFovModifier = 1.0 - InjuryLevel × INJURY_FOV_DEGRADATION_SCALE  // [GT]
InjuryLrecAdder   = InjuryLevel × INJURY_LREC_SCALE                    // [GT], adds ticks
InjuryCheckIntervalModifier = 1.0 + InjuryLevel × INJURY_CHECK_SCALE   // [GT], lengthens interval
```

**Architectural hook:** The formula insertion sites (FoV §3.1.3, L_rec §3.3.2, check
interval §3.4.3) are all multiplicative chains where a further term can be inserted
without structural change. All three modifiers default to `1.0f` / `0` at Stage 0 when
`InjuryLevel = 0`.

**Stage 3 implementation note:** Validate the full modifier stack — poor form + severe
injury + high pressure + low Decisions — does not push `EffectiveFoV_HalfAngle` below
`MIN_FOV_HALF_ANGLE` floor. Add integration test `INJ-P-001` for worst-case stack.

---

### 7.3.4 Fixed64 Determinism Migration

**Spec #6 reference:** §6.10 is the authoritative Fixed64 analysis for the Perception System.

**What it requires:** Floating-point `Atan2`, `Asin`, `Sqrt`, and `Cos` calls in Steps 3B
(FoV angular test), 4A (shadow cone angular interval), and 4B (bearing comparison) must
be replaced with Fixed64 equivalents from the Fixed64 Math Library (Specification #9, not
yet written).

**Estimated post-migration cost:** ~1ms for 22 agents, within the 2ms budget with ~50%
headroom. The 10Hz cadence provides intrinsic headroom vs. 60Hz physics systems because
the budget allocation is 10× looser (§6.10.2).

**Architectural hook:** All trigonometric operations are centralised in a `PerceptionMath`
static wrapper class (§6.10.3). The migration replaces `PerceptionMath` internals without
changing any call site in the pipeline. This is the identical pattern to Ball Physics §7.4.

**Migration strategy:**
1. Confirm Spec #9 (Fixed64 Library) is written and approved.
2. Replace `PerceptionMath` internals with Fixed64 equivalents.
3. Run Section 5 determinism tests (DET-001 through DET-004) on migrated build.
4. Confirm < 1% output change for ConfidenceScore and FoV boundary cases.
5. Run full test suite against Stage 0 baseline to confirm backwards compatibility.

**High-risk formula:** The shadow cone `Asin(r_occ / d_occ)` calculation (§3.2.2) is
most sensitive to Fixed64 accuracy near small occluder radii at long range. Fixed64
library evaluation must include an Asin accuracy test at (r=0.5m, d=50m) as part of
the acceptance criteria.

---

## 7.4 Permanently Excluded Features

This section covers features that were formally evaluated during design. §7.4.1, §7.4.2,
and §7.4.4 are **permanently rejected** — they will never be added to this system.
§7.4.3 is included here because §1.3.3 lists it as a Stage 0 exclusion; that entry
applies to Stage 0 only. Stage 1 activates it (§7.1.2). Its Stage 0 deferral rationale
is documented here for completeness. The design pressure to revisit all four will recur
— the rationale must be available when it does.

---

### 7.4.1 PerceptionState or AwarenessLevel Enums (Rejected)

**What it would do:** Classify agents into labelled states such as `TRACKING`, `AWARE`,
`BLIND`, or integer awareness levels 1–5.

**Why permanently rejected:**

1. **Destroys continuous model fidelity.** The Stage 0 model produces numerically grounded
   outputs derived from FoV geometry, attribute values, and occlusion geometry. Collapsing
   these into five bins erases exactly the richness that makes elite players feel different
   from average players. Two players with `Decisions = 10` and `Decisions = 19` have
   measurably different L_rec values — an enum cannot represent this.

2. **Creates invisible state transitions.** An enum forces a threshold at which behaviour
   jumps discontinuously. A player who crosses the `AWARE`/`TRACKING` boundary instantly
   changes behaviour — not because physics changed, but because a threshold was crossed.
   This produces exploitable, unrealistic tactical artefacts.

3. **Inconsistent with project-wide design pattern.** Ball Physics, Agent Movement, Pass
   Mechanics, Shot Mechanics, and First Touch Mechanics are all parametric and continuous.
   Introducing a classified state in Perception creates an incongruous island in the model.

**Correct alternative:** Any Decision Tree logic that requires a threshold must implement
the threshold itself, reading the continuous `ConfidenceScore` and `VisibleOpponents.Count`
from the snapshot. The Perception System delivers the values; classification is a Decision
Tree responsibility.

---

### 7.4.2 Rendering of Perception Cones in Gameplay (Rejected)

**What it would do:** Render each agent's FoV cone as a visual overlay during live gameplay.

**Why permanently rejected:** Perception cones are implementation details of the agent
cognitive model — they are not intended to be legible to the player during live play.
Exposing them degrades immersion and potentially provides exploitable information about
opponent awareness states. Visual debug tools (the `OcclusionDebugRecord` in §3.7.3) exist
for development purposes only and are excluded from production builds.

**Correct alternative:** Coach/analytics views in Stage 3+ may provide zone-of-influence
overlays derived from snapshot data, but these are analytical abstractions — not real-time
perception cone renders.

---

### 7.4.3 Per-Entity Fidelity Variation — Stage 0 Only (Activates Stage 1)

**What it would do:** Allow different entities in the same snapshot to be perceived with
different spatial fidelity — for example, a nearby entity at full fidelity, a far entity
at degraded fidelity.

**Why rejected at Stage 0:** `PerceivedPosition` was deliberately set to ground truth at
Stage 0 to keep the Decision Tree interface simple during its initial design. Introducing
positional uncertainty before Decision Tree Specification #8 is written risks the DT
being designed around ground truth and then breaking when error is added. The Stage 1
path (§7.1.2) ensures this is added at the correct sequencing point.

**Clarification:** This is not a permanent exclusion — it is explicitly planned for Stage 1.
It appears here because `§1.3.3` lists "per-entity perceptual fidelity variation" as a
permanent exclusion. That entry refers specifically to Stage 0. Stage 1 activates it.
The language in §1.3.3 must not be read as "never."

---

### 7.4.4 Decision Tree Management of Shoulder Check Scheduling (Rejected)

**Outline reference:** OQ-3 resolution.

**What it would do:** Allow the Decision Tree to request shoulder checks on demand —
i.e., the DT evaluates tactical context, determines that a check is warranted, and
sends a request to the Perception System to execute it.

**Why permanently rejected:**

1. **Creates circular dependency.** The Decision Tree consumes `PerceptionSnapshot` to
   make decisions. If it then sends requests back to the Perception System, a feedback
   loop is created: perception drives decisions, decisions drive perception timing, which
   changes the next perception snapshot. At Stage 0, this loop cannot be controlled
   without careful specification of evaluation order. The risk is compounded because
   Decision Tree Specification #8 is not yet written.

2. **Violates system boundary.** Perception owns awareness. Decision making owns decisions.
   The shoulder check is a sensory mechanism, not a tactical decision — it is the act of
   turning to look, not the decision to turn and look. The DT can respond to what is seen
   (tactical decision); it does not manage the sensor (sensory mechanism ownership).

3. **Stage 1 path preserves the spirit without the coupling.** Context-sensitive urgency
   (§7.1.5) adds a `ContextProvider` read by the Perception System — the Perception System
   queries context, it does not receive commands. This is strictly unidirectional and
   avoids the loop.

---

## 7.5 Architectural Hooks Summary

A consolidated reference for all Stage 0 fields, stubs, and constants that serve as
extension points for future stages. All hooks are present in Stage 0 code; none are
activated pending the upstream specifications.

| Hook | Location | Current Value / Behaviour | Activation Stage |
|------|----------|--------------------------|-----------------|
| `ShoulderCheckAnimData` stub struct | §3.4.4 | Populated each heartbeat; no consumer | Stage 1 (§7.1.7) |
| `ConfidenceScore` in `PerceivedAgent` | §3.7.2 | Binary: 1.0f for confirmed, absent if unconfirmed | Stage 1 (§7.1.2) |
| `PerceivedPosition` ground truth flag | §3.7.2 | Equals `TruePosition` at Stage 0 | Stage 1 (§7.1.2) |
| `PerceivedVelocity` ground truth flag | §3.7.2 | Equals `TrueVelocity` at Stage 0 | Stage 1 (§7.1.2) |
| `BallStalenessFrames` field | §3.7.1 | Populated; used as elapsed-time denominator | Stage 1 (§7.1.3) |
| `MAX_PERCEPTION_RANGE = 120f` [GT] | §3.10 | Fixed constant; spatial hash preserved | Stage 2 (§7.2.1) |
| `EventBusStub.Publish<T>()` | §4.6.5 | No-op in RELEASE; console log in DEV | Stage 1 (§7.1.8) |
| Teammate occlusion disable (OQ-1) | §3.2 / Step 4 | Only opponents cast shadow cones | Stage 1 (§7.1.1) |
| `FormModifier` read in `PlayerAttributes` | §3.10 | Available; not consumed by pipeline | Stage 3 (§7.3.1) |
| `PsychologyModifier` in `PlayerAttributes` | §3.10 | Available; not consumed by pipeline | Stage 3+ (§7.3.2) |
| `InjuryLevel` in `PlayerAttributes` | §3.10 | Available; not consumed by pipeline | Stage 3+ (§7.3.3) |
| `PerceptionMath` wrapper class | §6.10.3 | Float arithmetic; wraps all trig calls | Stage 3+ (§7.3.4) |
| `GoalkeeperPerceivedExtension` surface | §7.1.4 | Standard agent pipeline; no GK special-casing | Stage 1 (§7.1.4) |
| Possession doubling of check interval | §3.4.3 | Sole context modifier at Stage 0 | Stage 1 extends (§7.1.5) |

---

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
