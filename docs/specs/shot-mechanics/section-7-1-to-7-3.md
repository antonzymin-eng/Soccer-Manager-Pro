# Shot Mechanics Specification #6 — Section 7: Future Extensions

**File:** `Shot_Mechanics_Spec_Section_7_v1_0.md`
**Purpose:** Authoritative roadmap for all planned and permanently excluded Shot Mechanics
extensions — what changes at each development stage, what architectural hooks exist today,
what risks each extension introduces, and what is permanently excluded. This section is
the single source of truth for Shot Mechanics system evolution. Future extension
references in Section 1.3 (Stage 1+ Deferrals table) are summaries derived from this
section; if a conflict exists, **this section takes precedence**.

**Created:** February 23, 2026, 11:59 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisites confirmed:**
- Shot Mechanics Specification #6 — Section 1 v1.1 (scope, KDs 1–7 locked)
- Shot Mechanics Specification #6 — Section 2 v1.0 (data structures, FRs, pipeline)
- Shot Mechanics Specification #6 — Section 3 Part 1 v1.1 (§3.1–§3.3 specified)
- Shot Mechanics Specification #6 — Section 3 Part 2 v1.2 (§3.4–§3.10 specified)
- Shot Mechanics Specification #6 — Section 4 v1.3 (architecture and integration)
- Shot Mechanics Specification #6 — Section 5 v1.3 (testing)
- Shot Mechanics Specification #6 — Section 6 v1.0 (performance analysis)
- Ball Physics Specification #1 (approved) — Fixed64 migration pattern (§7.4)
- Agent Movement Specification #2 (approved) — `PlayerAttributes.FormModifier`,
  `PlayerAttributes.PsychologyModifier`, Fixed64 migration pattern (§6.4.1)
- Collision System Specification #3 (approved) — `GetAndClearTackleFlag()` confirmed
- Pass Mechanics Specification #5 (approved) — Section 7 structure reused directly
- First Touch Mechanics Specification #4 — Section 7 v1.0 (structural template)

**Open Dependency Flags:**
- `[PENDING]` — Form System (Master Vol 2) not yet written; `FormModifier` field assumed
  from Agent Movement §3.5.6. Does not block this section.
- `[PENDING]` — H-Gate Psychology System (Master Vol 2) not yet written;
  `PsychologyModifier` field assumed from Agent Movement §3.5.6. Does not block
  this section.
- `[PENDING]` — Animation System not yet designed; `ShotAnimationData` stub (§2.4.4)
  provides the Stage 1 interface surface. Does not block this section.
- `[PENDING]` — Set Pieces System not yet designed; §7.1.3 documents the planned
  interface contract. Does not block this section.
- `[PENDING]` — Fixed64 Math Library Spec #9 not yet written; migration pattern
  assumed from Ball Physics §7.4. Does not block this section.

---

## Table of Contents

- [Preamble: Role of This Section](#preamble-role-of-this-section)
- [7.1 Stage 1 Extensions (Year 2)](#71-stage-1-extensions-year-2)
  - [7.1.1 Animation System Integration](#711-animation-system-integration)
  - [7.1.2 Extended ContactZone Resolution](#712-extended-contactzone-resolution)
  - [7.1.3 Set Piece Shot Interface Stub](#713-set-piece-shot-interface-stub)
  - [7.1.4 xG Statistics Engine Hookup](#714-xg-statistics-engine-hookup)
- [7.2 Stage 2 Extensions (Years 3–4)](#72-stage-2-extensions-years-34)
  - [7.2.1 Surface Condition Effects on Shot Execution](#721-surface-condition-effects-on-shot-execution)
  - [7.2.2 Off-Balance and Diving Shot States](#722-off-balance-and-diving-shot-states)
  - [7.2.3 Penalty Kick Ritual and Pressure System](#723-penalty-kick-ritual-and-pressure-system)
  - [7.2.4 Shot Sound Design Hooks](#724-shot-sound-design-hooks)
- [7.3 Stage 3+ Extensions (Years 5–11+)](#73-stage-3-extensions-years-511)
  - [7.3.1 Form System Modifier](#731-form-system-modifier)
  - [7.3.2 H-Gate Psychology and Crowd Pressure](#732-h-gate-psychology-and-crowd-pressure)
  - [7.3.3 Injury State Degradation](#733-injury-state-degradation)
  - [7.3.4 Fixed64 Determinism Migration](#734-fixed64-determinism-migration)
- [7.4 Permanently Excluded Features](#74-permanently-excluded-features)
  - [7.4.1 Probabilistic Outcome Rolls (Rejected)](#741-probabilistic-outcome-rolls-rejected)
  - [7.4.2 ShotType Enum (Rejected)](#742-shottype-enum-rejected)
  - [7.4.3 IGkResponseSystem Inside Shot Mechanics (Rejected)](#743-igkresponsesystem-inside-shot-mechanics-rejected)
  - [7.4.4 Goal Detection Inside Shot Mechanics (Rejected)](#744-goal-detection-inside-shot-mechanics-rejected)
- [7.5 Architectural Hooks Summary](#75-architectural-hooks-summary)
- [7.6 Cross-Spec Dependency Map for Extensions](#76-cross-spec-dependency-map-for-extensions)
- [7.7 Backwards Compatibility Guarantees](#77-backwards-compatibility-guarantees)
- [7.8 Known Risks for Future Stages](#78-known-risks-for-future-stages)
- [7.9 Section Summary](#79-section-summary)
- [Cross-Reference Verification](#cross-reference-verification)
- [Version History](#version-history)

---

## Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded Shot
Mechanics extensions.

**Design philosophy:** Stage 0 Shot Mechanics was designed with future extensions in
mind. Specific architectural decisions — the `ShotAnimationData` stub struct populated
but unconsumed at Stage 0, the `FormModifier` and `PsychologyModifier` fields defaulting
to `1.0f`, the `ContactZone` enum sized for future members, the `PsychologyPressureScale`
field defaulting to `1.0f` in the error formula, the `InjuryLevel` field read but not
applied — were made to minimise structural rework when extensions arrive. This section
documents how each extension connects to existing hooks and identifies gaps where new
architecture is required.

**Core constraint that governs all extensions:** Determinism is never negotiable. Every
extension listed in §7.1–§7.3 must preserve bitwise-identical outputs given identical
inputs. Any design that violates this constraint is a blocking defect regardless of
stage. The deterministic hash approach — `matchSeed + agentId + frameNumber` — must be
preserved across all extensions.

**Comparison to sibling specs:**

| Spec | Extension Complexity | Primary Extension Categories |
|------|---------------------|------------------------------|
| Ball Physics (#1) | Low | Surface types, weather, Fixed64 |
| Agent Movement (#2) | High | Animation, dribbling, LOD, psychology |
| Collision System (#3) | Medium | Aerial collision, body parts, constraint solver |
| First Touch (#4) | Medium | Body parts, surface, skill moves, psychology |
| Pass Mechanics (#5) | Medium | Body parts, formation instructions, set pieces |
| **Shot Mechanics (#6)** | **Medium-High** | Animation, set pieces, psychology, off-balance shots |

Shot Mechanics carries higher extension complexity than Pass Mechanics because of the
penalty kick subsystem (Stage 2), the off-balance/diving shot states (Stage 2), and the
deeper psychology integration warranted by high-pressure finishing scenarios. The core
parameter-based model is stable; extensions add inputs to the existing modifier chain
without restructuring it.

**Stage/Year Mapping (per Master Development Plan v1.0):**

| Stage | Years | Focus | Shot Mechanics Extensions |
|-------|-------|-------|---------------------------|
| Stage 0 | Year 1 | Physics Foundation | Core system (this spec) |
| Stage 1 | Year 2 | Tactical Demo | Animation integration, extended ContactZone, set piece stub, xG hookup |
| Stage 2 | Years 3–4 | V1 Release | Surface effects, off-balance shots, penalty kick ritual, sound hooks |
| Stage 3 | Years 5–6 | Management Depth | Form System modifier |
| Stage 4 | Years 7–8 | Human Systems | H-Gate psychology, injury degradation |
| Stage 5 | Years 9–10 | Global Simulation | Fixed64 migration |
| Stage 6 | Year 11+ | Multiplayer | Determinism validation under network conditions |

---

## 7.1 Stage 1 Extensions (Year 2)

Stage 1 adds animation integration, extended contact zone resolution, a set piece
interface stub, and statistics engine hookup. Shot Mechanics changes at Stage 1 are
moderate — the `ShotAnimationData` stub and the `ContactZone` enum are already
architecturally prepared; Stage 1 populates and consumes them.

---

### 7.1.1 Animation System Integration

**What it does:** Activates the `ShotAnimationData` stub struct that Shot Mechanics
already populates at Stage 0 but that no consumer reads. The Animation System subscribes
to the Event System and selects and plays the appropriate shot animation clip based on
`ShotAnimationData.ContactZone`, `PowerIntent`, `BodyMechanicsScore`, `IsWeakFoot`, and
`WindupFrames`. Poor body mechanics (`BodyMechanicsScore < 0.4`) triggers unbalanced
animation variants; weak foot triggers the foot-side variant.

**Why Stage 1:** Requires the Animation System, which is not built at Stage 0. The stub
struct is fully specified and populated today (§2.4.4); the dependency is the consumer,
not the producer.

**Existing architectural hooks:**

The `ShotAnimationData` struct (Section 2.4.4) is fully defined and populated during
every `ShotExecutor.Execute()` call at Stage 0:

```csharp
// Section 2.4.4 — ShotAnimationData (populated at Stage 0, unconsumed)
public struct ShotAnimationData
{
    public int         AgentId;
    public ContactZone ContactZone;       // Animation clip selector
    public float       PowerIntent;       // Blend: slow windup → explosive
    public float       BodyMechanicsScore;// Poor mechanics → unbalanced variant
    public bool        IsWeakFoot;        // Foot-side animation variant
    public int         WindupFrames;      // Animation system sync duration
    // [STAGE-1-HOOK] Published to Event Bus at Stage 0 but no consumer exists.
    // Animation System subscribes at Stage 1 via Event System Spec #17.
}
```

**What changes at Stage 1:**

1. Animation System subscribes to `ShotAnimationData` via the Event Bus (Event System
   Spec #17). No changes to Shot Mechanics are required — the data is already published.

2. If the Animation System needs feedback (e.g., actual animation duration for
   `WindupFrames` synchronisation), a lightweight
   `IAnimationShotDurationProvider.GetWindupFrames(AgentId, ContactZone, PowerIntent)`
   interface should be added to `ShotAnimationData` population. This interface is owned
   by the Animation System and injected at Stage 1.

3. The `WindupFrames` field in `ShotAnimationData` is currently a calculated estimate
   (§3.9 windup state duration). At Stage 1, this should be replaced with the Animation
   System's authoritative clip duration to keep simulation and animation synchronised.

**No changes to `ShotExecutor.cs`, `ShotStateMachine.cs`, or any core physics
calculation.** Shot Mechanics is the publisher; Stage 1 is about adding a consumer.

**Architecture note:** If the Animation System introduces a frame-timing dependency
(animation completion gates the RELEASE transition), this must be resolved in the frame
pipeline ordering document before Stage 1 implementation. The shot state machine
(§3.9) must not wait on animation frames — it operates on simulation time only.

---

### 7.1.2 Extended ContactZone Resolution

**What it does:** Replaces the three discrete `ContactZone` values
(`Centre | BelowCentre | OffCentre`) with a richer biomechanical contact-point model
that discriminates instep, outside-foot, toe, and inside-foot contact geometries, each
carrying distinct velocity, launch angle, and spin consequences.

**Why Stage 1:** The three-zone model is sufficient for Stage 0's physics fidelity
requirements. Full contact geometry requires validated biomechanical data and
considerably more formula complexity. It is also dependent on a richer Decision Tree
(Spec #8) that can populate the additional contact zone values.

**Existing architectural hooks:**

The `ContactZone` enum (§2.4.1) is deliberately not exhaustive at Stage 0:

```csharp
// Section 2.4.1 — ContactZone enum
// [STAGE-1-EXTENSION] Enum is sized for additional members.
// Do NOT add members before Stage 1 design review.
public enum ContactZone
{
    Centre      = 0,    // Default — full instep driven contact
    BelowCentre = 1,    // Chip / loft — contact beneath ball equator
    OffCentre   = 2,    // Curl / finesse — asymmetric contact
    // [STAGE-1-RESERVED] Instep, OutsideFoot, Toe, InsideFoot
}
```

The `ContactZoneModifier` lookup in §3.2 (velocity) and §3.3 (launch angle) uses a
`switch` on `ContactZone`. Adding new cases requires formula development for each new
zone but does not change the pipeline structure.

**What changes at Stage 1:**

1. Add `Instep`, `OutsideFoot`, `Toe`, `InsideFoot` members to `ContactZone`.

2. Derive `ContactZoneModifier` values for each new member from biomechanical literature
   (Lees & Nolan 1998; Inoue et al. 2014). All new values carry `[GT]` flag for
   gameplay tuning.

3. Update Decision Tree Spec #8 to select from the extended enum. The three Stage 0
   values (`Centre`, `BelowCentre`, `OffCentre`) must remain valid — the Decision Tree
   must not be updated to remove them without a formal amendment to this spec.

4. Update Section 5 test suite to cover new contact zones. The existing 104 tests must
   continue to pass unmodified — new tests are additive.

**Caution:** `ContactZone` integer values (0, 1, 2) are used as hash inputs in §3.6.9
(deterministic error direction). Adding new enum members must preserve existing integer
assignments. New members must be assigned values ≥ 3. Serialised match replays that
contain `ContactZone` values 0–2 must deserialise correctly under the extended enum.

---

### 7.1.3 Set Piece Shot Interface Stub

**What it does:** Provides a defined interface boundary that the Set Pieces System
(Stage 1+) will implement when penalty kicks, direct free kicks, and corners are added.
At Stage 0, `ShotExecutor.Execute()` only accepts `ShotRequest` structs from agents
in `RUNNING`, `IDLE`, or `JOGGING` locomotion states. Set pieces require pre-kick ritual
modelling, spot positioning, referee delay variance, and wall/barrier geometry — none of
which belong in Shot Mechanics.

**Why Stage 1:** The Set Pieces System does not exist yet. A stub interface defined now
prevents the Set Pieces System from bypassing `ShotExecutor` or duplicating physics
logic.

**Existing architectural hooks:**

Section 1.3.1 already documents the scope boundary:

> *Set piece shots — penalties, direct and indirect free kicks, corners — require
> distinct pre-kick ritual and pressure modelling. Owned by Set Pieces System (Stage 1+).*

The `ShotRequest` struct is the boundary. Set pieces populate `ShotRequest` and call
`ShotExecutor.Execute()` — exactly as the Decision Tree does. Shot Mechanics does not
know or care whether the shot originates from open play or a set piece.

**What changes at Stage 1:**

1. Confirm that `ShotRequest` is sufficient for set piece execution. Expected: yes, with
   one addition — a `SetPieceContext` nullable field to carry referee delay and wall
   geometry data used only by the Set Pieces System's own pre-execution validation, not
   by Shot Mechanics internals.

2. If `SetPieceContext` is added to `ShotRequest`, it must be nullable and Shot
   Mechanics must never read or branch on it. The field is pass-through only.

3. Penalty kick psychology pressure (crowd noise, one-on-one with goalkeeper) integrates
   via `PsychologyPressureScale` in the error formula (§3.6.5). The H-Gate System
   populates this field — no structural change to the error formula is needed.

**Architecture note — no `SetPieceRequest` subtype:** Do not create a
`SetPieceRequest : ShotRequest` subtype or a separate `SetPieceExecutor`. The
parameter-based model handles set piece physics identically to open-play shots once the
pre-kick ritual is resolved. A separate execution path would duplicate formula logic and
introduce divergence risk.

---

### 7.1.4 xG Statistics Engine Hookup

**What it does:** Activates the xG-relevant fields in `ShotExecutedEvent` that are
populated at Stage 0 but that no consumer reads. The Statistics Engine (Stage 1+)
subscribes to `ShotExecutedEvent` and uses `BodyMechanicsScore`, `PowerIntent`,
`EstimatedTarget`, `KickVelocity`, `DistanceToGoal`, and `IsWeakFoot` to compute
expected-goals (xG) values per shot.

**Why Stage 1:** Requires the Statistics Engine, which is not built at Stage 0. The
event struct carries all fields needed for xG calculation; the dependency is the consumer.

**Existing architectural hooks:**

The `ShotExecutedEvent` struct (§2.4.3, KD-7) already reserves all xG-relevant fields:

```csharp
// Section 2.4.3 — ShotExecutedEvent (populated at Stage 0, xG fields unconsumed)
public struct ShotExecutedEvent
{
    public int         AgentId;
    public Vector3     KickVelocity;          // [xG-RESERVED] Initial ball velocity
    public float       BodyMechanicsScore;    // [xG-RESERVED] Shot quality input
    public float       PowerIntent;           // [xG-RESERVED] Normalised power
    public Vector2     EstimatedTarget;       // [xG-RESERVED] Goal-relative target
    public float       DistanceToGoal;        // [xG-RESERVED] Metres at shot time
    public bool        IsWeakFoot;            // [xG-RESERVED] Foot identifier
    public ContactZone ContactZone;           // [xG-RESERVED] Contact geometry
    public bool        StumbleTriggered;      // GK: irregular trajectory possible
    public int         ContactFrame;          // Replay synchronisation
}
```

**What changes at Stage 1:**

1. Statistics Engine subscribes to `ShotExecutedEvent` via the Event Bus.

2. Statistics Engine computes xG per shot. The xG model is owned by the Statistics
   Engine — Shot Mechanics has no xG logic and no knowledge of xG model structure.

3. No changes to Shot Mechanics. The `ShotExecutedEvent` fields are already correct and
   complete. The `[xG-RESERVED]` comments may be updated to `[xG-ACTIVE]` once the
   Statistics Engine is built, but this is a cosmetic change only.

**Caution:** The xG model consumes `DistanceToGoal` and `EstimatedTarget` as at shot
submission time. These are the values at `ShotRequest` submission, not at ball contact.
The Statistics Engine must document this assumption. If the xG model requires values at
ball contact, `ShotExecutedEvent` will need a `DistanceToGoalAtContact` field — this is
a Stage 1 design decision, not a Stage 0 decision.

---

## 7.2 Stage 2 Extensions (Years 3–4)

Stage 2 adds surface condition effects, off-balance shot states, penalty kick ritual
modelling, and sound design hooks. These extensions require either systems not yet built
(surface conditions, audio) or formula complexity beyond Stage 0 scope (off-balance
shots, penalty ritual pressure).

---

### 7.2.1 Surface Condition Effects on Shot Execution

**What it does:** Applies pitch surface condition (wet, dry, heavy, artificial) as a
modifier to shot velocity and `BodyMechanicsScore`. A wet pitch reduces available
traction during the shot delivery stride, lowering maximum `KickPower` efficiency. A
heavy pitch increases ball-surface friction during the short roll before contact in
ground-level shots, affecting effective `PowerIntent` transfer.

**Why Stage 2:** Requires the `PitchConditionSystem` (shared infrastructure) and a
confirmed `SurfaceType` enum from Ball Physics §7.2. Both systems exist at Stage 2.

**Existing architectural hooks:**

Surface type is already passed through `BallState.SurfaceType` (Ball Physics §3.1).
Shot Mechanics reads this field at Stage 0 but does not branch on it (set to
`GRASS_DRY` default):

```csharp
// ShotValidator.cs — Stage 0 surface read (§3.1)
// [STAGE-2-HOOK] SurfaceType is read and passed to ShotContext but not used.
// Stage 2 adds SurfaceModifier lookup table here.
float surfaceModifier = 1.0f; // [STAGE-2-HOOK] Replace with lookup
```

**What changes at Stage 2:**

1. Add `SurfaceModifier` lookup table to `ShotConstants.cs`:
   ```
   GRASS_DRY       → 1.00f  (baseline)
   GRASS_WET       → 0.94f [GT]  (−6% power efficiency on wet run-up)
   GRASS_HEAVY     → 0.88f [GT]  (−12% — waterlogged pitch; stride power loss)
   ARTIFICIAL_TURF → 0.97f [GT]  (−3% — consistent but harder underfoot)
   ```
   All values `[GT]` — gameplay-tunable. Justified by qualitative biomechanics
   (reduced traction = reduced power transfer); exact values require playtesting.

2. Apply `SurfaceModifier` to `V_BASE` in `VelocityCalculator` (§3.2):
   ```
   V_BASE_adjusted = V_BASE × SurfaceModifier
   ```
   This is a single multiplication at one existing formula site. No structural change.

3. Apply `SurfaceModifier` as a minor penalty to `BodyMechanicsScore` (§3.7):
   ```
   BodyMechanicsScore_adjusted = BodyMechanicsScore × sqrt(SurfaceModifier)
   ```
   Square root dampens the effect — surface does not dominate body mechanics.

**Interaction risk:** Surface modifier stacks with `WeakFootPenalty` (§3.8) and
`BodyMechanicsScore` scalar on error (§3.6.7). The full modifier stack on a weak-foot
shot on a heavy pitch by a low-attribute player must be integration-tested. See §7.8
KR-4.

---

### 7.2.2 Off-Balance and Diving Shot States

**What it does:** Extends shot initiation to cover `STUMBLING` and `DIVING` locomotion
states from Agent Movement, enabling acrobatic shot attempts (off-balance efforts,
diving headers-of-foot, sliding shots). These produce dramatically lower
`BodyMechanicsScore`, significantly wider error cones, and probabilistic stumble
outcomes even on full-power shots.

**Why Stage 2:** Stage 0 rejects `ShotRequest` from any locomotion state other than
`RUNNING`, `IDLE`, or `JOGGING` (§3.1 VR-004). Extending to off-balance states requires
validated `BodyMechanicsScore` formulas for unstable postures, which require playtesting
data. The `STUMBLING` signal pathway from `ShotStateMachine` to Agent Movement (§3.9,
§3.10) is already implemented — Stage 2 inverts the flow, reading `STUMBLING` as an
input rather than only an output.

**Existing architectural hooks:**

Section §3.9 (ShotStateMachine) already reads `AgentLocomotionState` for validation:

```csharp
// ShotValidator.cs — Stage 0 locomotion check (§3.1 VR-004)
// [STAGE-2-HOOK] STUMBLING and DIVING are rejected here at Stage 0.
// Stage 2 adds penalty formulas for these states rather than hard rejection.
if (request.LocomotionState == STUMBLING || request.LocomotionState == DIVING)
{
    // Stage 0: reject with log.
    // Stage 2: pass through with OffBalancePenalty applied to BodyMechanicsScore.
}
```

The `BodyMechanicsScore` modifier chain (§3.7) is parameterised — adding an
`OffBalancePenalty` multiplier requires one new formula term and one new constant.

**What changes at Stage 2:**

1. Add `STUMBLING` and `DIVING` as valid (but penalised) locomotion states in §3.1.

2. Add `OffBalancePenalty` scalar to `BodyMechanicsEvaluator` (§3.7):
   ```
   STUMBLING → OffBalancePenalty = 0.55f [GT]  (severe quality reduction)
   DIVING    → OffBalancePenalty = 0.40f [GT]  (extreme quality reduction)
   ```

3. The `STUMBLE_THRESHOLD` in §3.9 must be re-evaluated. A `DIVING` shot already
   implies an off-balance state — `StumbleTriggered` in `ShotExecutedEvent` should
   always be `true` for `DIVING` shots regardless of `BodyMechanicsScore`.

4. Add integration test series `OB-*` covering off-balance shot outcomes:
   - `OB-001`: Stumbling shot produces `BodyMechanicsScore < STUMBLE_THRESHOLD`
   - `OB-002`: Diving shot always sets `StumbleTriggered = true`
   - `OB-003`: Off-balance shot error cone is ≥ 2× the equivalent standing shot

---

### 7.2.3 Penalty Kick Ritual and Pressure System

**What it does:** Provides Shot Mechanics' contribution to the penalty kick experience:
heightened `PsychologyPressureScale` values in the error formula (§3.6.5), a
`RUN-UP_STUTTER_PROBABILITY` parameter in the windup state (§3.9), and integration with
the H-Gate System for shoot-out specific psychological modelling. The pre-kick ritual
animation, referee whistle, and goalkeeper movement are owned by the Set Pieces System
(§7.1.3) and Goalkeeper Mechanics (Spec #11) respectively.

**Why Stage 2:** Requires the H-Gate Psychology System (Stage 4 in isolation; Stage 2
if a simplified version ships with V1). Penalty ritual modelling also depends on the Set
Pieces System (§7.1.3) being operational.

**Existing architectural hooks:**

The `PsychologyPressureScale` field in the error formula (§3.6.5) defaults to `1.0f` at
Stage 0:

```csharp
// ErrorCalculator.cs — §3.6.5 Pressure Scalar
// [STAGE-2-HOOK] PsychologyPressureScale defaults to 1.0f.
// Stage 2/4: H-Gate System populates this for penalty kicks.
float psychologyScale = context.PsychologyPressureScale; // Stage 0: always 1.0f
```

**What changes at Stage 2 (simplified H-Gate):**

1. Add `IsPenaltyKick` flag to `ShotRequest` (from Set Pieces System).
   Shot Mechanics reads this flag to set an elevated `PsychologyPressureScale`
   baseline (e.g., `1.35f [GT]`) for all penalty attempts.

2. Add `RUN-UP_STUTTER_CHECK` to the WINDUP→EXECUTE transition in §3.9:
   A `BodyMechanicsScore` threshold check (lower for penalty kicks) that triggers
   `StumbleTriggered = true` even if contact is made, representing a stuttered run-up.
   Uses the existing deterministic hash seed — no new randomness introduced.

3. When the full H-Gate System is available (Stage 4), the simplified `1.35f` baseline
   is replaced by the H-Gate System's `MatchStressLevel` output. This is a substitution
   at the same formula site, not a structural change.

---

### 7.2.4 Shot Sound Design Hooks

**What it does:** Publishes shot contact audio events to the Event Bus for the Audio
System to consume. Different contact zones, power levels, and body mechanics scores
produce different sound characteristics — a well-struck driven shot sounds different
from a mishit toe-poke.

**Why Stage 2:** Requires the Audio System, which is not built at Stage 0 or Stage 1
(animation priorities first). The Event Bus is the correct decoupling mechanism.

**Existing architectural hooks:**

`ShotExecutedEvent` (§2.4.3) already carries all fields required for audio
classification: `ContactZone`, `PowerIntent`, `BodyMechanicsScore`, `KickVelocity`,
`IsWeakFoot`. No new fields are required.

**What changes at Stage 2:**

1. Audio System subscribes to `ShotExecutedEvent` via the Event Bus. Shot Mechanics
   publishes one event per shot — already the case at Stage 0.

2. Audio System implements audio clip selection from event fields. This is entirely
   inside the Audio System; Shot Mechanics requires no changes.

3. If high-precision audio timing (< 2 frames) requires a dedicated
   `ShotContactAudioEvent` published before the full `ShotExecutedEvent`, a lightweight
   event struct may be added. This is a Stage 2 design decision — a new event type does
   not structurally change Shot Mechanics, it adds one `EventBus.Publish()` call in
   `ShotExecutor.Execute()`.

---

## 7.3 Stage 3+ Extensions (Years 5–11+)

These extensions add management-layer depth (Form System), human psychological realism
(H-Gate), physical degradation (injury), and long-term platform determinism (Fixed64).
None require structural changes to Shot Mechanics — each is an additional multiplier in
an existing modifier chain or a type substitution.

---

### 7.3.1 Form System Modifier

**What it does:** Applies the player's current form rating as a multiplier to effective
attributes in the velocity formula (§3.2). A player in poor form fires shots below their
technical ceiling; a player in excellent form slightly exceeds their average. Form is a
management-layer concept, not a frame-by-frame physical variable.

**Why Stage 3:** Requires the Form System (Master Vol 2 — not yet written). The field
exists on `PlayerAttributes` at Stage 0 but is set to `1.0f`.

**Existing architectural hooks:**

The `EffectiveAttribute` formula (§3.2) already incorporates `FormModifier`:

```csharp
// VelocityCalculator.cs — §3.2 EffectiveAttribute
// Stage 0: FormModifier is always 1.0f (Agent Movement §3.5.6 placeholder).
// Stage 3: Form System populates this field from match-to-match form tracking.
float effectiveKickPower = attributes.KickPower
                         × attributes.FormModifier        // [STAGE-3-HOOK]
                         × context.FatigueModifier;
```

**What changes at Stage 3:**

1. Form System (Master Vol 2) begins populating `PlayerAttributes.FormModifier` with
   values in `[0.75f, 1.15f]` range (estimated; Form System owns the range definition).

2. No changes to Shot Mechanics code. The multiplication is already present.

3. Add integration test `FM-001`: Form modifier `0.75f` reduces shot velocity by
   approximately 12% at maximum power for a standard attribute profile. This test can
   be written at Stage 0 using a mock attribute value; it will begin passing
   meaningfully when the Form System is live.

**Caution:** Form modifier stacks with all other modifiers. The full stack
(FormModifier × FatigueModifier × WeakFootPenalty × SurfaceModifier) must be verified
to not produce `V_BASE` values below `V_MIN` (§3.2 clamp). The existing clamp is the
safety net; the integration test series should include a worst-case stack scenario.

---

### 7.3.2 H-Gate Psychology and Crowd Pressure

**What it does:** Replaces the `PsychologyPressureScale` default of `1.0f` with a live
value from the H-Gate System, driven by `MatchStressLevel` (crowd noise, scoreline,
player personality attribute). Under high psychological pressure, the error formula
(§3.6.5) widens, producing missed shots that are physically accurate representations of
stress-induced technique breakdown.

**Why Stage 4:** Requires the H-Gate System (Master Vol 2 — not yet written). This is a
deep management-layer system requiring extensive design; it is correctly deferred.

**Existing architectural hooks:**

The `PsychologyPressureScale` field in the error formula (§3.6.5) already exists:

```csharp
// ErrorCalculator.cs — §3.6.5
// [STAGE-4-HOOK] PsychologyPressureScale defaults to 1.0f.
// H-Gate System populates via PlayerAttributes.PsychologyModifier.
float pressureScalar = BASE_PRESSURE_SCALAR
                     * (1.0f + context.PressureLevel * PRESSURE_SENSITIVITY)
                     * context.PsychologyPressureScale;  // [STAGE-4-HOOK]
```

**What changes at Stage 4:**

1. H-Gate System (Master Vol 2) populates `PlayerAttributes.PsychologyModifier` before
   each match frame. This field is the source for `PsychologyPressureScale`.

2. No structural changes to Shot Mechanics. The multiplication site already exists.

3. Confirm interaction with `FatigueScalar` (§3.6.6) and `BodyShapeScalar` (§3.6.7).
   A player under maximum psychological pressure with high fatigue and poor body shape
   must be tested. The error cone width must not produce outputs outside the goal frame
   by more than `MAX_ERROR_ANGLE_RADIANS` (§3.6.11 clamp). The clamp is the safety net.

**Note — simplified penalty pressure (Stage 2):** The simplified `IsPenaltyKick`
pressure elevation in §7.2.3 is a stopgap. When the H-Gate System is active, the
penalty-specific `1.35f` baseline is superseded by the H-Gate System's
`MatchStressLevel` output. The `IsPenaltyKick` flag may be retained as an input to the
H-Gate System (it contributes to `MatchStressLevel` computation) but its direct role
in the error formula is eliminated.

---

### 7.3.3 Injury State Degradation

**What it does:** Applies `PlayerAttributes.InjuryLevel` (currently read but not
applied at Stage 0) as a degradation to both `V_BASE` in velocity calculation (§3.2)
and `BodyMechanicsScore` in body mechanics evaluation (§3.7). A significantly injured
player cannot generate full shot velocity or maintain shot technique.

**Why Stage 4:** Requires the Injury System (not yet designed). The field exists on
`PlayerAttributes` at Stage 0; the formula sites are prepared.

**Existing architectural hooks:**

Section §3.2 already reads `InjuryLevel` and documents the Stage 0 no-op:

```csharp
// VelocityCalculator.cs — §3.2
// [STAGE-4-HOOK] InjuryLevel is read but not applied. Stage 0 assumption: no injury.
// Stage 4: InjuryModifier = (1.0f - InjuryLevel × INJURY_VELOCITY_SCALE)
float injuryModifier = 1.0f; // [STAGE-4-HOOK]
```

**What changes at Stage 4:**

1. Add `INJURY_VELOCITY_SCALE` constant to `ShotConstants.cs` `[GT]`.
   Estimated: `0.20f` — full injury (`InjuryLevel = 1.0f`) reduces velocity by 20%.

2. Add `INJURY_MECHANICS_SCALE` constant for `BodyMechanicsScore` degradation `[GT]`.
   Estimated: `0.30f` — full injury reduces body mechanics score by 30%.

3. Apply both modifiers at their existing formula sites. These are single
   multiplications at two existing no-op sites.

4. Add integration test `INJ-001` through `INJ-003`:
   - `INJ-001`: `InjuryLevel = 0.5f` reduces `V_BASE` by approximately 10%
   - `INJ-002`: `InjuryLevel = 1.0f` reduces `BodyMechanicsScore` to near-zero cap
   - `INJ-003`: Full modifier stack with injury does not produce `NaN` or sub-`V_MIN`
     velocity

---

### 7.3.4 Fixed64 Determinism Migration

**What it does:** Replaces `float` arithmetic throughout Shot Mechanics with Fixed64
(Q32.32 or equivalent) fixed-point arithmetic to guarantee bitwise-identical outputs
across CPU architectures, compilers, and optimisation settings. This is required for
multiplayer replay fidelity under Stage 5 network conditions.

**Why Stage 5:** Fixed64 library (Fixed64 Math Library Spec #9) is not yet written.
Float arithmetic is sufficient for Stage 0–4 single-machine determinism, achieved
through the deterministic hash seed (`matchSeed + agentId + frameNumber`). Fixed64 is
required only when cross-machine determinism is needed (Stage 5+).

**Existing architectural hooks:**

Section 6.9 (Performance Analysis — Fixed64 Migration Notes) documents the migration
risk for each formula. Summary:

| Formula Site | Migration Risk | Notes |
|---|---|---|
| `V_BASE` derivation (§3.2) — multiplications | Low | Direct substitution |
| Sigmoid `σ(x)` for distance blend (§3.2) | Medium | `Exp()` → Fixed64 Exp; accuracy risk |
| Launch angle trig — `atan2`, `sin`, `cos` (§3.3) | Medium | Fixed64 trig library required |
| Spin vector derivation (§3.4) | Low | Multiplications only |
| Error angle trig — `atan2` (§3.6) | Medium | Same trig library as §3.3 |
| Deterministic hash (§3.6.9) | None | Integer arithmetic; no migration |
| `BodyMechanicsScore` scalar chain (§3.7) | Low | Multiplications only |
| Weak foot `WeakFootPenalty` (§3.8) | Low | Multiplications only |

**Migration approach (consistent with Ball Physics §7.4 and Agent Movement §6.4.1):**

1. Evaluate candidate Fixed64 libraries at Stage 5 start. Acceptance criteria:
   - `< 0.01 m/s` velocity error per shot vs. float reference over 10,000 shots
   - `< 0.1°` launch angle error per shot
   - `Exp()` accuracy sufficient for sigmoid blend (< 1% output difference)
   - Performance ≤ 2× float cost on reference CPU

2. All Shot Mechanics math is isolated in static, pure-function calculator classes
   (`VelocityCalculator.cs`, `LaunchAngleCalculator.cs`, etc. — §4.2). Type
   substitution (`float` → `Fixed64`) is contained within these files; callers use
   the same interface.

3. `ShotAnimationData` and `ShotExecutedEvent` carry `float` fields for external
   consumers (Animation System, Statistics Engine). These consumers may not support
   Fixed64. A conversion layer at event publication is required: fixed-point
   calculations → float fields before `EventBus.Publish()`.

4. The deterministic hash (§3.6.9) uses integer arithmetic — no migration required.
   This is the single Shot Mechanics formula with zero cross-machine risk.

**Note:** Section 6.9 contains the detailed per-formula migration analysis. This section
documents only the architectural approach and timeline. If a conflict exists between §6.9
and this section, §6.9 takes precedence for performance estimates; this section takes
precedence for migration strategy.

---

