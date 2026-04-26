# First Touch Mechanics Specification #4 â€” Section 7: Future Extensions

**Purpose:** Authoritative roadmap for all planned First Touch Mechanics extensions â€” what
changes at each stage, what architectural hooks exist today, what risks each extension
introduces, and what is permanently excluded. This section is the single source of truth
for First Touch system evolution. Future extension references in Sections 1.3 (Stage 1+
Deferrals table) and 2.2 (Architecture notes) are summaries derived from this section;
if a conflict exists, **this section takes precedence**.

**Created:** February 18, 2026, 8:00 PM PST
**Version:** 1.1
**Status:** Approved
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)
**Prerequisites:** Section 1 (v1.1), Section 2 (v1.0), Section 3 (v1.2), Section 4 (v1.1),
Section 5 (v1.2), Section 6 (v1.0)

**Changelog:**
- v1.1 (March 05, 2026): C-03 audit fix — replaced all instances of "Decision Tree Spec #7"
  with "Decision Tree Spec #8" (5 occurrences: §7.1.4, §7.2.4, §7.6 dependency map x2,
  §7.2.4 body text). Perception System is Spec #7; Decision Tree is Spec #8 per PROGRESS.md.

---

## Table of Contents

- [Preamble: Role of This Section](#preamble)
- [7.1 Stage 1 Extensions (Year 2)](#71-stage-1-extensions-year-2)
  - [7.1.1 Animation-Driven Touch Types](#711-animation-driven-touch-types)
  - [7.1.2 Body Part Differentiation](#712-body-part-differentiation)
  - [7.1.3 Touch Sound Design Hooks](#713-touch-sound-design-hooks)
  - [7.1.4 Dribbling Locomotion Integration](#714-dribbling-locomotion-integration)
- [7.2 Stage 2 Extensions (Years 3â€“4)](#72-stage-2-extensions-years-3-4)
  - [7.2.1 Surface Condition Effects](#721-surface-condition-effects)
  - [7.2.2 Skill Moves Interface](#722-skill-moves-interface)
  - [7.2.3 Closing Speed Pressure](#723-closing-speed-pressure)
  - [7.2.4 Feint Touch (Dummy / Let Ball Run)](#724-feint-touch-dummy--let-ball-run)
- [7.3 Stage 3+ Extensions (Years 5â€“11+)](#73-stage-3-extensions-years-5-11)
  - [7.3.1 Form System Modifier](#731-form-system-modifier)
  - [7.3.2 Psychology and Crowd Pressure](#732-psychology-and-crowd-pressure)
  - [7.3.3 Injury State Effects](#733-injury-state-effects)
  - [7.3.4 Fixed64 Determinism Migration](#734-fixed64-determinism-migration)
- [7.4 Permanently Excluded Features](#74-permanently-excluded-features)
  - [7.4.1 Random "Bad Luck" Touches (Rejected)](#741-random-bad-luck-touches-rejected)
  - [7.4.2 Ball Magnet / Automatic Control (Rejected)](#742-ball-magnet--automatic-control-rejected)
- [7.5 Architectural Hooks Summary](#75-architectural-hooks-summary)
- [7.6 Cross-Spec Dependency Map for Extensions](#76-cross-spec-dependency-map-for-extensions)
- [7.7 Backwards Compatibility Guarantees](#77-backwards-compatibility-guarantees)
- [7.8 Known Risks for Future Stages](#78-known-risks-for-future-stages)
- [7.9 Section Summary](#79-section-summary)

---

## Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded First Touch
extensions.

**Design philosophy:** Stage 0 First Touch was designed with future extensions in mind.
Specific architectural decisions â€” the `TouchResult` struct's reserved fields, the
`BodyPart` placeholder always set to FOOT at Stage 0, the `TouchType` field in
`FirstTouchEvent`, the `ControlQuality` pipeline's modifier chain, and the
`DribblingStateSignal` interface â€” were made to minimise rework when extensions are
implemented. This section documents how each extension connects to existing hooks and
identifies gaps where new architecture is required.

**Comparison to sibling specs:**

| Spec | Extension Complexity | Primary Extension Categories |
|------|---------------------|------------------------------|
| Ball Physics (#1) | Low | Surface types, weather, Fixed64 |
| Agent Movement (#2) | High | Animation, dribbling, LOD, psychology |
| Collision System (#3) | Medium | Aerial collision, body parts, constraint solver |
| **First Touch (#4)** | **Medium** | Body parts, surface, skill moves, psychology |

First Touch sits closer to Collision System in extension complexity. Unlike Agent Movement,
which must coordinate 22 independent entities with per-agent modifier pipelines, First Touch
operates on a single agent-ball event at a time. Extension complexity comes primarily from
cross-spec integration requirements, not from per-agent scaling.

**Stage/Year Mapping (per Master Development Plan v1.0):**

| Stage | Years | Focus | First Touch Extensions |
|-------|-------|-------|------------------------|
| Stage 0 | Year 1 | Physics Foundation | Core system (this spec) |
| Stage 1 | Year 2 | Tactical Demo | Animation, body parts, sound hooks, dribbling |
| Stage 2 | Years 3â€“4 | V1 Release | Surface effects, skill moves, closing speed pressure |
| Stage 3 | Years 5â€“6 | Management Depth | Form system modifier |
| Stage 4 | Years 7â€“8 | Human Systems | Psychology, injury state |
| Stage 5 | Years 9â€“10 | Global Simulation | Fixed64 migration |
| Stage 6 | Year 11+ | Multiplayer | Determinism validation |

---

## 7.1 Stage 1 Extensions (Year 2)

Stage 1 adds animation, body part selection, sound, and tighter dribbling integration.
First Touch changes are moderate â€” the architecture already supports body part modifiers
through `BodyPart` field and `ControlQuality` pipeline; Stage 1 populates them.

---

### 7.1.1 Animation-Driven Touch Types

**What it does:** Different touch animations â€” cushion, wedge, flick, trap â€” produce
different ball outcome characteristics. A cushion touch dampens ball velocity further than
a wedge; a flick redirects off the intended plane. The touch type should inform animation
selection, and the animation result should feed back into ball displacement.

**Why Stage 1:** Requires the Animation System (not yet built). First Touch cannot select
touch type until the animation layer exists to play it. The mathematics are trivial; the
dependency is the blocker.

**Existing architectural hooks:**

The `TouchResult` struct (Section 4.2) already includes a reserved `TouchType` field:

```csharp
// Section 4.2.1 â€” TouchResult (Stage 0)
public struct TouchResult
{
    public float ControlQuality;        // [0, 1]
    public float TouchRadius;           // metres
    public Vector3 BallDisplacement;    // metres
    public PossessionOutcome Outcome;
    public float DribblingSignal;       // [0, 1]

    // [STAGE-1-HOOK] Populated by Animation System at Stage 1
    // Currently always UNSET; do not branch on this value in Stage 0 logic
    public TouchType TouchTypeHint;
}
```

The `FirstTouchEvent` (Section 4.6) already carries `TouchType` for statistics consumers.

**What changes at Stage 1:**

1. Add `TouchType` enum to `TouchResult`:
   ```
   CUSHION    â€” Soft reception; reduces ball speed by additional 15â€“25%
   WEDGE      â€” Firm stop; redirects ball to preferred foot side
   FLICK      â€” Redirects ball laterally; high quality required (q â‰¥ 0.7)
   TRAP       â€” Ball pinned momentarily; sets up shot or pass
   UNSET      â€” Stage 0 default; treated as CUSHION in Stage 1
   ```

2. Animation System provides `AnimationTouchResult` via new interface:
   ```
   IAnimationFirstTouchProvider.GetTouchType(AgentID, BallApproachAngle, BallHeight)
     â†’ TouchType
   ```

3. `EvaluateFirstTouch()` queries this interface before computing `BallDisplacement`.
   Touch type modifies the `BLEND_MIN_MAGNITUDE` and `BLEND_DIRECTION_WEIGHT` constants:
   - CUSHION: `BLEND_DIRECTION_WEIGHT` Ã— 0.7 (ball stays closer to body)
   - WEDGE: `BLEND_DIRECTION_WEIGHT` Ã— 1.2 (ball deflects more firmly)
   - FLICK: Rotation applied to `desiredDirection` before blend
   - TRAP: `BallDisplacement` forced to `Vector3.zero`; ball placed at feet

4. Unit test suite extended with `TA-*` (touch animation) series; existing CQ-*, TR-*,
   BD-* tests must continue to pass with all `TouchType.UNSET` inputs.

**Risk:** Animation system timing may not align with First Touch evaluation within the same
frame. If Animation System resolves touch type in a later phase, First Touch may need to
defer `BallDisplacement` calculation to the following frame. This must be resolved during
Stage 1 architecture design.

**Dependency:** Animation System (Spec TBD, Stage 1 scope).

---

### 7.1.2 Body Part Differentiation

**What it does:** Ball contact with foot, chest, and thigh applies different control
quality modifiers. A chest touch is inherently harder to control than a foot touch; a
thigh touch harder still. Each body part has a distinct modifier applied multiplicatively
to the base control quality before clamping.

**Why Stage 1:** Requires Collision System Stage 1 body part detection (Collision System
Spec Â§3.3.4 documents this deferral). At Stage 0, `AgentBallCollisionData.BodyPart` is
always `FOOT`. Stage 1 Collision System will populate this field with the actual contact
body part, at which point First Touch can apply the modifier.

**Existing architectural hooks:**

`AgentBallCollisionData.BodyPart` (from Collision System Â§4.2.6) already exists and is
passed into `EvaluateFirstTouch()`. The field is populated; Stage 0 First Touch ignores
it. No structural changes to the input contract are needed.

**What changes at Stage 1:**

Add body part modifier to `ControlQuality` computation, inserted between the attribute
weighted average and the pressure/velocity terms:

```
// Section 3.1 formula, Stage 1 extension:
baseQ = (TECHNIQUE_WEIGHT Ã— Technique + FIRST_TOUCH_WEIGHT Ã— FirstTouch) / 20.0
bodyPartModifier = GetBodyPartModifier(collision.BodyPart)
adjustedQ = baseQ Ã— bodyPartModifier
q = Clamp01(adjustedQ - velocityPenalty - movementPenalty - pressurePenalty)
  Ã— orientationBonus
```

Proposed modifiers (empirical; subject to gameplay validation):

| Body Part | Modifier | Rationale |
|-----------|----------|-----------|
| FOOT      | Ã— 1.00   | Baseline; most controlled surface |
| THIGH     | Ã— 0.85   | Less contact surface, more skill required |
| CHEST     | Ã— 0.80   | Ball bounces; harder to control direction |
| HEAD      | Routed to Heading Mechanics (#10); not evaluated here |

**Stage 0 compatibility:** With `BodyPart` always `FOOT`, modifier is always Ã— 1.00.
All existing tests pass unchanged.

**Risk:** Modifier values are empirically chosen and require gameplay validation. The
values above are initial proposals, not academically sourced. Flag in Section 7.8
(KR-3).

**Dependency:** Collision System Spec #3 Stage 1 body part detection (Â§3.3.4).

---

### 7.1.3 Touch Sound Design Hooks

**What it does:** First Touch emits touch quality data that the Audio System uses to
select appropriate sound samples â€” a clean control sounds different from a heavy touch
or a deflection.

**Why Stage 1:** Audio System does not exist at Stage 0. No sound infrastructure is in
scope for physics foundation.

**Existing architectural hooks:**

`FirstTouchEvent` (Section 4.6) already carries `ControlQuality`, `Outcome`, and
`TouchTypeHint`. The Audio System consumes `FirstTouchEvent` from the Event Bus without
any changes to First Touch logic.

**What changes at Stage 1:**

No changes to First Touch Mechanics logic. The Audio System subscribes to `FirstTouchEvent`
and maps `ControlQuality` bands to audio clips:

```
q â‰¥ 0.85  â†’ "clean_touch" sample
q â‰¥ 0.55  â†’ "firm_touch" sample
q â‰¥ 0.30  â†’ "heavy_touch" sample
q <  0.30 â†’ "deflection" or "stumble" sample
INTERCEPTION â†’ "interception" sample (opponent foot sound)
```

The Audio System owns all sample selection and playback logic. First Touch Mechanics is
not modified.

**Dependency:** Audio System (Stage 1 scope). Event Bus (Stage 1 scope).

---

### 7.1.4 Dribbling Locomotion Integration

**What it does:** When First Touch grants CONTROLLED possession, Agent Movement applies
dribbling locomotion penalties (speed cap, turn rate reduction). Stage 0 issues the
`DribblingSignal` via the interface; Stage 1 ensures Agent Movement acts on it
appropriately with full animation support.

**Current state:** `DribblingStateSignal` interface is fully specified in Section 3.7
and tested in IT-002 and PO-008. The signal is emitted. Agent Movement Spec Â§6.1.2
documents its Stage 1 activation.

**What changes at Stage 1:**

This is primarily an Agent Movement responsibility. First Touch Mechanics:

1. Ensures `DribblingSignal` is set to 1.0 for CONTROLLED outcomes and 0.0 for all
   others â€” already the case in Stage 0 (confirmed by PO-007, PO-008).

2. Adds `DribblingIntent` enum to `TouchResult` for Stage 1 AI consumers:
   ```
   DRIBBLE_OPEN   â€” Agent intends to run with ball; max speed penalty applied
   DRIBBLE_TIGHT  â€” Agent intends to shield/control; minimal speed penalty
   ```
   This is populated by the Decision Tree (Spec #8, Stage 1); First Touch passes it
   through to Agent Movement as a hint, not a command.

**Dependency:** Agent Movement Spec #2 Â§6.1.2 (dribbling modifier activation), Decision
Tree Spec #8 (dribbling intent classification).

---

## 7.2 Stage 2 Extensions (Years 3â€“4)

Stage 2 is the V1 release milestone. First Touch extensions here add environmental
realism (surface conditions) and expand the player action vocabulary (skill moves, feints,
closing speed pressure). These are more complex than Stage 1 extensions and require new
sub-system integrations.

---

### 7.2.1 Surface Condition Effects

**What it does:** Pitch surface state â€” dry grass, wet grass, mud â€” affects how reliably
a player can control the ball. A wet pitch makes the ball skid further; mud creates
unpredictable resistance. This is applied as a multiplicative modifier to `ControlQuality`
and an additive modifier to `TouchRadius`.

**Why Stage 2:** Requires the `PitchConditionSystem`, a shared sub-system also consumed
by Ball Physics (Spec #1 Â§7.2) and Agent Movement (Spec #2 Â§6.2.1). Building it at Stage
2 allows all three consumers to share a single implementation.

**Existing architectural hooks:**

`Ball Physics SurfaceType` enum is already defined:
```
GRASS_DRY, GRASS_WET, MUD, ARTIFICIAL_DRY, ARTIFICIAL_WET
```

Ball Physics passes `SurfaceType` in `BallState` (Â§3.1). `EvaluateFirstTouch()` receives
`BallState` as input. The surface type is already available; First Touch currently ignores
it.

**What changes at Stage 2:**

Add surface modifier lookup after the body part modifier (Stage 1) in the `ControlQuality`
pipeline:

```
surfaceModifier = GetSurfaceModifier(ballState.SurfaceAtBallPosition)
adjustedQ = baseQ Ã— bodyPartModifier Ã— surfaceModifier
```

Proposed modifiers (empirical; subject to gameplay tuning):

| Surface | Quality Modifier | Touch Radius Addend | Rationale |
|---------|-----------------|---------------------|-----------|
| GRASS_DRY | Ã— 1.00 | + 0.00m | Baseline |
| GRASS_WET | Ã— 0.90 | + 0.10m | Ball skids, harder to control |
| MUD | Ã— 0.75 | + 0.20m | Heavy ball; unpredictable |
| ARTIFICIAL_DRY | Ã— 0.95 | + 0.05m | Faster bounce, less cushion |
| ARTIFICIAL_WET | Ã— 0.85 | + 0.15m | Fast + slippery |

**Note on determinism:** `PitchConditionSystem` must return the same value given the same
match tick and field position. It cannot use `System.Random`. This constraint aligns with
Master Vol 1 Â§1.3 and is the responsibility of `PitchConditionSystem` to enforce.

**Risk:** Surface modifier stack (body part Ã— surface Ã— velocity penalty) could produce
very low quality values that force nearly all touches in poor conditions to INTERCEPTION.
Gameplay balancing may require adjusting the pressure weight or introducing a floor to
prevent simulations becoming unplayable on wet pitches. Requires playtesting data.

**Dependency:** PitchConditionSystem (Stage 2 shared sub-system), Ball Physics Spec #1
Â§7.2 (surface type definition).

---

### 7.2.2 Skill Moves Interface

**What it does:** After gaining CONTROLLED possession, a player can execute a skill move â€”
drag-back, Cruyff turn, step-over, roulette. Skill moves are distinct from first touch:
they occur after possession is established, not during the reception event.

**Scope boundary (critical):** Skill Moves are **not** part of First Touch Mechanics. The
First Touch system determines whether possession is gained (CONTROLLED) and where the ball
ends up. A Skill Move is a subsequent intentional action by the agent. The Skill Move
System (a separate spec, Stage 2 scope) owns all skill move logic.

**First Touch's role:** Issue the CONTROLLED outcome and provide `TouchResult.BallPosition`
as the starting position for the skill move sequence. No other interface changes are
needed. The `DribblingSignal = 1.0` flag is already the hook for the Skill Move System to
activate.

**What changes at Stage 2 in First Touch:** None. This entry exists to clarify scope
boundary and confirm that `DribblingSignal` is the correct integration point.

**Dependency:** Skill Move System (Spec TBD, Stage 2 scope).

---

### 7.2.3 Closing Speed Pressure

**What it does:** The current pressure model (Section 3.5) uses opponent proximity with
inverse-square distance falloff. It does not account for opponent approach velocity â€”
a defender sprinting at the receiver is more disruptive than a stationary defender at the
same distance. Stage 2 adds a closing speed modifier to the pressure calculation.

**Existing architectural hooks:**

`AgentBallCollisionData` passes `AgentVelocity`. The opponent velocity used in the
pressure calculation (Section 3.5) currently uses position only. The velocity data is
already available in principle; querying opponent velocities from the spatial hash requires
an API extension.

**What changes at Stage 2:**

1. Extend `SpatialHashQuery` to return opponent velocity alongside position:
   ```csharp
   // Stage 2 pressure query result
   public struct PressureAgentData
   {
       public Vector3 Position;
       public Vector3 Velocity;   // [STAGE-2-ADDITION]
   }
   ```

2. Add closing speed term to pressure formula. Define `closingSpeed` as the component of
   opponent velocity directed toward the receiving agent:
   ```
   closingSpeed = max(0, dot(opponentVelocity, normalize(agentPos - opponentPos)))
   closingSpeedContrib = closingSpeed / MAX_SPRINT_SPEED   // normalise to [0, 1]
   effectiveDistance = distance Ã— (1 - CLOSING_SPEED_WEIGHT Ã— closingSpeedContrib)
   pressureFromOpponent = PRESSURE_BASE / effectiveDistanceÂ²
   ```
   Where `CLOSING_SPEED_WEIGHT` âˆˆ [0, 1] controls the magnitude of the effect.
   Proposed initial value: `CLOSING_SPEED_WEIGHT = 0.3` (empirical; flag for tuning).

3. Effective distance must be floor-clamped to prevent division by zero:
   ```
   effectiveDistance = max(MIN_PRESSURE_DISTANCE, effectiveDistance)
   ```

**Performance impact:** The closing speed calculation adds ~8 ops per opponent (dot product,
normalise, clamp). For n=2 opponents this is 16 additional ops â€” negligible relative to the
143-op baseline. Within existing performance budget (Â§6.3.2).

**Dependency:** Collision System Stage 2 spatial hash API extension (opponent velocity
query).

---

### 7.2.4 Feint Touch (Dummy / Let Ball Run)

**What it does:** An agent deliberately lets the ball run past them â€” not controlling it â€”
to deceive an opponent. This is distinct from failing to control the ball (which is also
LOOSE_BALL) because it is intentional. The agent appears to be receiving but steps over
the ball, allowing a teammate to collect it.

**Scope boundary:** Feint touch detection requires AI intent. First Touch cannot
distinguish a deliberate dummy from a failed control attempt using physics data alone.
The Decision Tree (Spec #8, Stage 1/2 scope) must instruct First Touch that a feint
was intended.

**What changes at Stage 2:**

1. Add `IsFeintIntended` flag to `MovementCommand` or a new `FirstTouchIntent` struct
   provided by the Decision Tree before the collision event is processed.

2. If `IsFeintIntended = true` and `Outcome` would be CONTROLLED, override to
   `LOOSE_BALL` and set `BallDisplacement` to `Vector3.zero` (ball continues on its
   original trajectory â€” agent steps over it).

3. Emit `FirstTouchEvent` with `OutcomeOverride = FEINT` for statistics/replay systems
   to distinguish intentional dummies from failed touches.

**Risk:** Feint detection creates a timing dependency â€” the Decision Tree must resolve
intent before `EvaluateFirstTouch()` executes in the same frame. If Decision Tree runs
after First Touch in the frame pipeline, the feint cannot be applied until the following
frame, which introduces a one-frame touch-then-release artifact. Frame ordering must be
resolved during Stage 2 architecture design.

**Dependency:** Decision Tree Spec #8, frame pipeline ordering specification.

---

## 7.3 Stage 3+ Extensions (Years 5â€“11+)

Later stage extensions integrate First Touch with the management simulation layer â€” form,
psychology, injury â€” and address long-term technical requirements such as Fixed64
determinism for multiplayer.

---

### 7.3.1 Form System Modifier

**What it does:** A player's current form (hot/cold streak) affects their touch quality.
A player in good form performs above their rated attribute; a player in poor form performs
below it. This is consistent with Football Manager's form model and is a key differentiator
for the management simulation depth.

**Why Stage 3:** Form System (Master Vol 2 Â§FormSystem) is a management-layer sub-system
not in scope until Stage 3 (Management Depth milestone).

**Existing architectural hooks:**

`PlayerAttributes` (Agent Movement Â§3.5.6) includes `FormModifier` (initialised to 1.0
at Stage 0). First Touch reads this field but treats it as a no-op at Stage 0 because it
is always 1.0.

**What changes at Stage 3:**

No structural changes. `FormSystem` sets `FormModifier` âˆˆ [0.7, 1.3] based on recent
performance. First Touch already multiplies `baseQ` by `FormModifier` in the
`ControlQuality` pipeline â€” this was built into the formula for this exact purpose.

```
// Section 3.1 â€” already implemented, Stage 0 value is always 1.0
baseQ = (TECHNIQUE_WEIGHT Ã— Technique + FIRST_TOUCH_WEIGHT Ã— FirstTouch) / 20.0
        Ã— attributes.FormModifier   // [STAGE-3-ACTIVATION] FormSystem writes this
```

**Required action at Stage 3:** Verify that `FormModifier` clamping is enforced by
`FormSystem` and not by First Touch. First Touch should not defensively clamp this value â€”
it trusts the contract.

**Dependency:** FormSystem (Master Vol 2, Stage 3 scope).

---

### 7.3.2 Psychology and Crowd Pressure

**What it does:** High-pressure psychological states â€” home crowd roaring, cup final
nerves, derby atmosphere â€” apply an additional modifier to touch quality beyond the
tactical pressure model in Section 3.5. This is the "big match temperament" mechanic
critical for realistic player simulation.

**Why Stage 4:** Psychology System (Master Vol 2 Â§H-Gate System) is a human simulation
layer not in scope until Stage 4.

**Existing architectural hooks:**

`PlayerAttributes.PsychologyModifier` (Agent Movement Â§3.5.6) is initialised to 1.0 at
Stage 0. First Touch does not yet multiply by this field â€” it must be added to the
pipeline at Stage 4.

**What changes at Stage 4:**

Insert `PsychologyModifier` into the `ControlQuality` pipeline. Applied after the
`FormModifier`, before the velocity/pressure terms:

```
baseQ = (TECHNIQUE_WEIGHT Ã— Technique + FIRST_TOUCH_WEIGHT Ã— FirstTouch) / 20.0
        Ã— attributes.FormModifier
        Ã— attributes.PsychologyModifier   // [STAGE-4-ADDITION]
adjustedQ = baseQ Ã— bodyPartModifier Ã— surfaceModifier
q = Clamp01(adjustedQ - velocityPenalty - movementPenalty - pressurePenalty)
  Ã— orientationBonus
```

**Interaction with tactical pressure (Section 3.5):** Psychological pressure and tactical
proximity pressure are intentionally separate. Tactical pressure (Section 3.5) represents
physical distraction from nearby opponents. Psychological pressure represents internal
cognitive load. Stacking both is correct and desirable â€” a nervous player under close
marking is doubly impaired.

**Risk:** Stacking three multiplicative modifiers (body part, form, psychology) before the
subtractive terms could produce very low effective `baseQ` in worst cases. Ensure that
the `Clamp01()` floor and the possession threshold (q < INTERCEPTION_THRESHOLD) do not
produce unrealistic results. Requires playtesting calibration at Stage 4.

**Dependency:** H-Gate Psychology System (Master Vol 2, Stage 4 scope).

---

### 7.3.3 Injury State Effects

**What it does:** An injured player has reduced touch quality proportional to injury
severity and location. A muscle strain affects control differently than a shoulder
complaint.

**Why Stage 4 / Optional:** Injury system integration is Stage 4 scope (Human Systems
milestone). The architecture decision â€” whether injury applies via `PlayerAttributes`
modifier (like FormModifier) or via a separate `InjuryState` struct â€” is not yet resolved.

**Existing architectural hooks:** None explicit. If injury is modelled as a generic
performance modifier (like FormModifier), no structural changes to First Touch are needed.
If injury is body-part-specific (knee injury reduces FOOT modifier, shoulder injury reduces
CHEST modifier), a more complex integration is required.

**Recommended approach:** Implement injury as a generic `InjuryModifier` in
`PlayerAttributes`, clamped to [0.5, 1.0]. The floor of 0.5 represents a player too
injured to play at all being handled upstream (squad selection) rather than in touch
mechanics. First Touch multiplies by `InjuryModifier` in the same pipeline position as
`FormModifier`.

**Dependency:** Injury System (Spec TBD, Stage 4 scope). Architecture decision deferred
to Stage 4.

---

### 7.3.4 Fixed64 Determinism Migration

**What it does:** Replaces `float` arithmetic in all First Touch calculations with Fixed64
arithmetic, guaranteeing bit-identical results across platforms. Required for Stage 6
multiplayer determinism.

**Why Stage 5:** Fixed64 library selection (shared with Ball Physics Â§7.4, Agent Movement
Â§6.4.1, Collision System Â§7.3.3) is a Stage 5 milestone. All four specs migrate together
to share the same library and validation methodology.

**Existing architectural hooks:**

All First Touch math is isolated in `FirstTouchMath` static class (Section 3). No
instance state. All inputs and outputs are passed as parameters. This design was
deliberately chosen to make the Fixed64 migration a clean substitution.

**Migration strategy:** Identical to Ball Physics Â§7.4 pattern:

1. Define `Fixed64Math` wrapper abstracting arithmetic operations.
2. Replace `float` with `Fixed64` in `FirstTouchMath` methods.
3. Re-run all 72 Section 5 unit tests against Fixed64 implementation.
4. Validate that outputs match float reference to within Â± 0.001m position error.
5. Run IT-005 determinism test (Section 5.9) on Fixed64 implementation.

**Performance note:** Fixed64 arithmetic is approximately 2â€“5Ã— slower than float on
modern hardware (Â§6.2.1 methodology). First Touch's low frequency (200â€“400
evaluations/match) means total Fixed64 overhead is negligible â€” estimated additional
match cost < 2ms. No performance concern.

**Dependency:** Fixed64 library (Stage 5 selection, shared across all physics specs).

---

## 7.4 Permanently Excluded Features

The following features will **never** be implemented in First Touch Mechanics. Each has
an immutable rationale. These decisions are not subject to future review unless the project
scope fundamentally changes.

| Feature | Exclusion Reason | Owner (if any) |
|---------|-----------------|----------------|
| Random "bad luck" touches | Violates determinism; randomness has no role here | N/A â€” permanently excluded |
| Ball magnet / automatic control | Physical limits always apply; cannot guarantee CONTROLLED | N/A â€” permanently excluded |
| Form as "mood" per match | Form is a rolling metric, not a single-match mood toggle | FormSystem owns form logic |
| Stamina-driven touch degradation | Stamina is a Sprint/Endurance concern, not First Touch | Agent Movement Spec #2 |
| Possession by proximity without contact | No physical justification; violates physics model | N/A â€” permanently excluded |
| Heading mechanics via this system | Ball above 0.5m is Heading Mechanics' domain | Heading Mechanics Spec #10 |
| Goalkeeper catches via this system | Catching/parrying is a distinct action model | Goalkeeper Mechanics Spec #11 |

---

### 7.4.1 Random "Bad Luck" Touches (Rejected)

**Proposed concept:** Introduce a random chance that even high-quality players
occasionally fumble the ball, independent of physics inputs, to model "unlucky" moments
and create drama.

**Why rejected:**

1. **Violates determinism.** Master Vol 1 Â§1.3 requires deterministic simulation.
   `System.Random` is prohibited. Any non-deterministic luck mechanic is architecturally
   incompatible with the project's core design constraint.

2. **Undermines player attribution.** If bad touches can occur randomly, match outcome
   statistics lose meaning. The management layer depends on meaningful performance data.
   A random fumble is noise that degrades analytical quality.

3. **Physics already models uncertainty.** Ball velocity, approach angle, movement
   penalty, and pressure all contribute to genuine variation in control quality. Poor
   control quality is not random â€” it is the correct response to difficult circumstances.
   The model already produces realistic-looking imperfect touches without randomness.

4. **Industry analysis:** Football Manager uses stochastic elements in its outcome
   resolution. Tactical Director's differentiator is deterministic, observable physics
   that players can understand and reason about. Introducing randomness would undermine
   this core value proposition.

**Alternative:** If "drama" variation is wanted, weight the distribution of pass
velocities or opponent pressure more aggressively. Variation from deterministic inputs
is always preferable to unexplained randomness.

---

### 7.4.2 Ball Magnet / Automatic Control (Rejected)

**Proposed concept:** When a player is near the ball, automatically grant CONTROLLED
possession without evaluating the physics model â€” "magnetic" ball control as a gameplay
simplification.

**Why rejected:**

1. **Physics accuracy is the project's core design principle.** Auto-granting possession
   bypasses the touch radius, velocity, pressure, and orientation calculations that define
   this system's value. It is architecturally contradictory.

2. **Even elite players have physical limits.** Even with Technique = 20 and
   FirstTouch = 20, a ball arriving at 50 m/s cannot be controlled (q approaches zero at
   high velocity by design). Removing this limit produces unrealistic outcomes.

3. **Not required for usability.** AI agents have the Decision Tree to position
   optimally before receiving; human players (if a management-layer UI is ever added)
   benefit from seeing realistic touch outcomes, not guaranteed ones.

**Alternative:** If a specific scenario requires guaranteed control (tutorial mode,
scripted cutscene), a `ForceControlled` override flag in `FirstTouchIntent` is the
correct mechanism â€” an explicit, traceable exception rather than a systemic bypass.

---

## 7.5 Architectural Hooks Summary

A consolidated reference showing which Stage 0 fields and interfaces serve as extension
points for future stages. Implementers should not remove or repurpose these fields even
if they appear unused at Stage 0.

| Hook | Location | Current Stage 0 Value | Consumed At |
|------|-----------|-----------------------|-------------|
| `TouchResult.TouchTypeHint` | Section 4.2.1 | `TouchType.UNSET` | Stage 1 (Â§7.1.1) |
| `AgentBallCollisionData.BodyPart` | Collision System Â§4.2.6 | `BodyPart.FOOT` | Stage 1 (Â§7.1.2) |
| `FirstTouchEvent.TouchType` | Section 4.6 | `TouchType.UNSET` | Stage 1 Audio |
| `TouchResult.DribblingIntent` | Section 4.2.1 (reserved) | `DribblingIntent.UNSET` | Stage 1 (Â§7.1.4) |
| `PlayerAttributes.FormModifier` | Agent Movement Â§3.5.6 | `1.0f` | Stage 3 (Â§7.3.1) |
| `PlayerAttributes.PsychologyModifier` | Agent Movement Â§3.5.6 | `1.0f` | Stage 4 (Â§7.3.2) |
| `BallState.SurfaceType` | Ball Physics Â§3.1 | `GRASS_DRY` | Stage 2 (Â§7.2.1) |
| `DribblingStateSignal` interface | Section 3.7 | Fully active | Stage 1 validation |
| Static-only `FirstTouchMath` class | Section 3 | Float | Stage 5 Fixed64 (Â§7.3.4) |

---

## 7.6 Cross-Spec Dependency Map for Extensions

| Extension | Depends On | Dependency Type | When |
|-----------|-----------|-----------------|------|
| Â§7.1.1 Animation Touch Types | Animation System (TBD) | New interface | Stage 1 |
| Â§7.1.2 Body Part Modifiers | Collision System Â§3.3.4 | Field population | Stage 1 |
| Â§7.1.3 Sound Hooks | Audio System / Event Bus (TBD) | Event consumer | Stage 1 |
| Â§7.1.4 Dribbling Intent | Decision Tree Spec #8 | Intent flag | Stage 1 |
| Â§7.2.1 Surface Conditions | PitchConditionSystem (shared) | Modifier lookup | Stage 2 |
| Â§7.2.1 Surface Conditions | Ball Physics Â§7.2 | SurfaceType enum | Stage 2 |
| Â§7.2.2 Skill Moves | Skill Move System (TBD) | DribblingSignal hook | Stage 2 |
| Â§7.2.3 Closing Speed | Collision System spatial hash extension | Velocity query | Stage 2 |
| Â§7.2.4 Feint Touch | Decision Tree Spec #8 | Intent flag | Stage 2 |
| Â§7.3.1 Form Modifier | FormSystem (Master Vol 2) | PlayerAttributes field | Stage 3 |
| Â§7.3.2 Psychology | H-Gate System (Master Vol 2) | PlayerAttributes field | Stage 4 |
| Â§7.3.3 Injury State | Injury System (TBD) | PlayerAttributes field | Stage 4 |
| Â§7.3.4 Fixed64 | Fixed64 library (shared) | Type substitution | Stage 5 |

---

## 7.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All 72 Section 5 tests continue to pass** with default parameters (FOOT body part,
   GRASS_DRY surface, FormModifier = 1.0, PsychologyModifier = 1.0, no feint intent,
   no closing speed). Extensions add new test series (TA-*, BP-*, SC-*, etc.) but must
   not break the existing CQ-*, TR-*, PR-*, OR-*, PO-*, EC-*, BD-*, IT-*, VS-* suite.

2. **`EvaluateFirstTouch()` signature is additive only.** The core function signature
   may gain optional parameters but must never remove or reorder existing parameters.
   Callers using the Stage 0 signature must continue to compile and produce identical
   results.

3. **`FirstTouchEvent` struct is additive only.** New fields may be appended. Existing
   field offsets must not change. Consumers that only read Stage 0 fields must continue
   to work without recompilation.

4. **Determinism is never compromised.** Any extension that introduces non-determinism
   (e.g., `System.Random`, time-dependent values, platform-specific behaviour) is a
   blocking defect regardless of the implementing stage. This constraint is absolute.

5. **Zero-allocation policy persists.** No extension may introduce heap allocations in
   the hot path of `EvaluateFirstTouch()`. Extensions that require allocation (e.g.,
   querying an animation provider via interface) must pre-pool results or use value-type
   returns.

---

## 7.8 Known Risks for Future Stages

| ID | Risk | Severity | Stage Affected | Mitigation |
|----|------|----------|----------------|------------|
| KR-1 | Animation frame timing misalignment with First Touch evaluation | High | Stage 1 | Resolve frame pipeline ordering before Stage 1 implementation begins |
| KR-2 | Feint touch creates one-frame artifact if Decision Tree runs after First Touch | High | Stage 2 | Resolve frame pipeline ordering at Stage 2 design |
| KR-3 | Body part modifiers (Ã—0.85, Ã—0.80) are empirical; may require rebalancing | Medium | Stage 1 | Mark values as `[GAMEPLAY-TUNABLE]`; log changes to changelog |
| KR-4 | Surface modifier stack may make wet-pitch play unplayably difficult | Medium | Stage 2 | Introduce configurable `SURFACE_MODIFIER_FLOOR` constant; test with realistic rain match scenarios |
| KR-5 | Psychology + Form + Body Part modifier stack may produce extreme low-q in worst cases | Medium | Stage 4 | Add integration test series for full modifier stack with minimum attribute values |
| KR-6 | Fixed64 library not yet selected; migration estimate may change | Low | Stage 5 | Evaluate candidate libraries at Stage 5 start before committing to implementation |
| KR-7 | `DribblingIntent` flag requires Decision Tree (Stage 1) before full dribbling integration | Low | Stage 1 | Confirm Decision Tree spec scope at Stage 1 planning |

---

## 7.9 Section Summary

| Subsection | Key Content |
|------------|-------------|
| **7.1 Stage 1** | Animation touch types, body part modifiers, sound hooks, dribbling intent |
| **7.2 Stage 2** | Surface conditions, skill move interface, closing speed pressure, feint touch |
| **7.3 Stage 3+** | Form modifier, psychology, injury state, Fixed64 migration |
| **7.4 Permanent Exclusions** | Random luck touches (violates determinism), ball magnet (violates physics) |
| **7.5 Architectural Hooks** | 9 Stage 0 hooks enumerated with current values and activation stages |
| **7.6 Cross-Spec Dependencies** | 14 extensions mapped to their upstream spec dependencies |
| **7.7 Backwards Compatibility** | 5 invariants that must be preserved across all future stages |
| **7.8 Known Risks** | 7 risks identified with severity ratings and mitigations |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| Ball Physics #1 Â§7.4 | Fixed64 migration pattern | âœ“ | Â§7.3.4 migration strategy follows same approach |
| Agent Movement #2 Â§6.1.2 | Dribbling modifier activation | âœ“ | Â§7.1.4 confirms DribblingSignal is the hook |
| Agent Movement #2 Â§6.4.1 | Fixed64 migration (shared library) | âœ“ | Â§7.3.4 confirms shared library approach |
| Agent Movement #2 Â§3.5.6 | FormModifier, PsychologyModifier fields | âœ“ | Â§7.3.1, Â§7.3.2 reference these fields |
| Collision System #3 Â§3.3.4 | Stage 1 body part detection deferral | âœ“ | Â§7.1.2 confirms dependency |
| Collision System #3 Â§7.3.3 | Fixed64 migration entry | âœ“ | Aligned â€” all specs migrate at Stage 5 |
| Ball Physics #1 Â§7.2 | Surface type enum definition | âœ“ | Â§7.2.1 references SurfaceType |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ“ | Â§7.4.1 rejection rationale and Â§7.7 invariant #4 |
| Master Vol 2 Â§FormSystem | Form modifier specification | âš  Pending | FormSystem spec not yet written; interface assumed |
| Master Vol 2 Â§H-Gate | Psychology modifier | âš  Pending | H-Gate spec not yet written; interface assumed |
| First Touch #4 Â§3.5 | Pressure formula (base for Â§7.2.3 extension) | âœ“ | Closing speed extends Â§3.5 formula |
| First Touch #4 Â§3.7 | DribblingStateSignal interface | âœ“ | Â§7.1.4 confirms this is the Stage 1 hook |
| First Touch Outline Â§7 | Section 7 structure approved | âœ“ | All outline items addressed |

---

**End of Section 7**

**Page Count:** ~18 pages
**Version:** 1.0
**Next Section:** Section 8 â€” References
