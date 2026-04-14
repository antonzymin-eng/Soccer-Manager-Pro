# Pass Mechanics Specification #5 — Section 7: Future Extensions

**File:** `Pass_Mechanics_Spec_Section_7_v1_0.md`
**Purpose:** Authoritative roadmap for all planned and permanently excluded Pass Mechanics
extensions — what changes at each stage, what architectural hooks exist today, what risks
each extension introduces, and what is permanently excluded. This section is the single
source of truth for Pass Mechanics system evolution. Future extension references in
Sections 1.3 (Stage 1+ Deferrals table) and 2.2 (Architecture notes) are summaries
derived from this section; if a conflict exists, **this section takes precedence**.

**Created:** February 20, 2026, 11:59 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 (v1.0), Section 2 (v1.0), Section 3 (§3.1–§3.9), Section 4
(v1.0), Section 5 (v1.0), Section 6 (v1.0)

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes`. §7.1.1 (Body Part Differentiation) and §7.1.3 (Preferred Foot
  Profile) are directly affected. Does not block this section's drafting.
- `[ERR-008-PENDING]` — `PossessingAgentId` design unresolved. Does not affect this
  section.

---

## Table of Contents

- [Preamble: Role of This Section](#preamble)
- [7.1 Stage 1 Extensions (Year 2)](#71-stage-1-extensions-year-2)
  - [7.1.1 Body Part Differentiation](#711-body-part-differentiation)
  - [7.1.2 Team Instruction Modifiers](#712-team-instruction-modifiers)
  - [7.1.3 Pass Statistics Integration](#713-pass-statistics-integration)
  - [7.1.4 Preferred Foot Profile (WeakFootRating Upgrade)](#714-preferred-foot-profile-weakfootrating-upgrade)
- [7.2 Stage 2 Extensions (Years 3–4)](#72-stage-2-extensions-years-34)
  - [7.2.1 Surface Condition Effects](#721-surface-condition-effects)
  - [7.2.2 Player-Specific Passing Tendencies](#722-player-specific-passing-tendencies)
  - [7.2.3 Advanced Curling Pass Model](#723-advanced-curling-pass-model)
- [7.3 Stage 3+ Extensions (Years 5–11+)](#73-stage-3-extensions-years-511)
  - [7.3.1 Form System Modifier](#731-form-system-modifier)
  - [7.3.2 Psychology and Crowd Pressure](#732-psychology-and-crowd-pressure)
  - [7.3.3 Fixed64 Determinism Migration](#733-fixed64-determinism-migration)
  - [7.3.4 Network-Synchronized Pass Events](#734-network-synchronized-pass-events)
- [7.4 Permanently Excluded Features](#74-permanently-excluded-features)
  - [7.4.1 Random Dice-Roll Pass Error (Rejected)](#741-random-dice-roll-pass-error-rejected)
  - [7.4.2 Ball Magnet / Auto-Completion (Rejected)](#742-ball-magnet--auto-completion-rejected)
  - [7.4.3 Tactical AI Reasoning Inside Pass Mechanics (Rejected)](#743-tactical-ai-reasoning-inside-pass-mechanics-rejected)
- [7.5 Architectural Hooks Summary](#75-architectural-hooks-summary)
- [7.6 Cross-Spec Dependency Map for Extensions](#76-cross-spec-dependency-map-for-extensions)
- [7.7 Backwards Compatibility Guarantees](#77-backwards-compatibility-guarantees)
- [7.8 Known Risks for Future Stages](#78-known-risks-for-future-stages)
- [7.9 Section Summary](#79-section-summary)
- [Cross-Reference Verification](#cross-reference-verification)

---

## Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded Pass
Mechanics extensions.

**Design philosophy:** Stage 0 Pass Mechanics was designed with future extensions
explicitly in mind. Specific architectural decisions — the `PassRequest` struct's reserved
fields, the discrete `PassType` enum that can accommodate additional types without schema
change, the multiplicative modifier chain in the error model, the `BodyPart` field
placeholder in the `PassRequest` struct, and the `WeakFootRating` scalar that will be
superseded by a full `FootPreferenceProfile` — were made to minimise rework when extensions
land. This section documents how each planned extension connects to existing hooks and
identifies the gaps where new architecture is needed.

**Comparison to sibling specifications:**

| Spec | Extension Complexity | Primary Extension Categories |
|------|---------------------|------------------------------|
| Ball Physics (#1) | Low | Surface types, weather, Fixed64 |
| Agent Movement (#2) | High | Animation, dribbling, LOD, psychology |
| Collision System (#3) | Medium | Aerial collision, body parts, constraint solver |
| First Touch (#4) | Medium | Body parts, surface, skill moves, psychology |
| **Pass Mechanics (#5)** | **Medium** | Body parts, team instructions, statistics, Fixed64 |

Pass Mechanics sits at medium extension complexity. The pass type taxonomy is bounded
(7 types at Stage 0), the error model is a simple multiplier chain, and most Stage 1
extensions add fields to existing structs rather than requiring structural redesign. The
largest migration — Fixed64 determinism — is shared with all physics specifications and
follows an established pattern.

**Stage/Year Mapping (per Master Development Plan v1.0):**

| Stage | Years | Focus | Pass Mechanics Extensions |
|-------|-------|-------|---------------------------|
| Stage 0 | Year 1 | Physics Foundation | Core system (this spec) |
| Stage 1 | Year 2 | Tactical Demo | Body parts, team instructions, statistics, preferred foot |
| Stage 2 | Years 3–4 | V1 Release | Surface effects, player tendencies, curling model |
| Stage 3 | Years 5–6 | Management Depth | Form system modifier |
| Stage 4 | Years 7–8 | Human Systems | Psychology, crowd pressure |
| Stage 5 | Years 9–10 | Global Simulation | Fixed64 migration |
| Stage 6 | Year 11+ | Multiplayer | Network-synchronized events |

---

## 7.1 Stage 1 Extensions (Year 2)

Stage 1 is the most architecturally significant upgrade for Pass Mechanics. Body part
differentiation adds a new modifier dimension to every pass. Team instruction modifiers
connect the tactical layer for the first time. Statistics integration gives the management
UI meaningful per-player data. All four extensions build on existing Stage 0 hooks and do
not require structural changes to the core evaluation pipeline.

---

### 7.1.1 Body Part Differentiation

**What it does:** Adds distinct accuracy and power profiles for four contact surfaces —
instep, outside of foot, laces, and heel. Each body part produces a different combination
of accuracy penalty, power modifier, and natural spin bias. The result is that players
do not uniformly execute all pass types with the same physical profile; a low-cross with
the outside of the foot has genuinely different characteristics from one struck with the
instep.

**Why Stage 1:** Requires both the Animation System (not yet built) and a Decision Tree
capable of selecting body part based on tactical context. Stage 0 does not support either
dependency.

**Body part profiles (preliminary — values are `[GAMEPLAY-TUNABLE]`):**

| Body Part | Accuracy Modifier | Power Modifier | Natural Spin Bias | Typical Use |
|-----------|------------------|----------------|-------------------|-------------|
| Instep | ×1.00 (baseline) | ×1.00 | Topspin | All ground and lofted passes |
| Laces | ×0.92 | ×1.15 | Minimal | Driven passes, long balls |
| Outside of foot | ×0.82 | ×0.90 | Sidespin (outward) | Curling passes, disguised crosses |
| Heel | ×0.60 | ×0.70 | Backspin | Creative / dummy passes; low frequency |

Accuracy modifier scales `BASE_ERROR(PassType)` multiplicatively. A ×0.82 modifier
increases error angle by ≈22% relative to instep baseline. Power modifier scales
`V_launch` before clamping.

**Existing architectural hooks:**

The `PassRequest` struct (Section 2.4.1) includes a `[STAGE-1-HOOK]` reserved field:

```csharp
// Section 2.4.1 — PassRequest (Stage 0)
public struct PassRequest
{
    public PassType   PassType;
    public CrossSubType CrossSubType;  // Optional; default = Flat
    public TargetType TargetType;
    public int        TargetAgentId;
    public Vector3    TargetPosition;
    public float      IntendedDistance;
    public bool       IsWeakFoot;
    public float      UrgencyLevel;
    public int        Frame;

    // [STAGE-1-HOOK] Set by Decision Tree at Stage 1; default = INSTEP in Stage 0
    // Do not branch on this field in Stage 0 logic — value is always INSTEP
    public BodyPart   BodyPart;
}
```

`PassTypeProfiles.cs` already uses a keyed lookup by `PassType`. At Stage 1, the lookup
is extended to a composite key `(PassType, BodyPart)`, or `BodyPart` modifiers are applied
as a multiplicative post-process on the base profile.

**What changes at Stage 1:**

1. `BodyPart` enum added to `PassType.cs` (or its own file):
   ```
   INSTEP       — baseline; Stage 0 default
   LACES        — power emphasis
   OUTSIDE_FOOT — creative; sidespin natural
   HEEL         — low power, low accuracy; emergent
   ```

2. `PassTypeProfiles.cs` extended with per-`BodyPart` modifier tables.

3. Decision Tree (#8) begins populating `PassRequest.BodyPart` based on tactical context
   (contact angle, urgency, intended pass type).

4. New test series `BP-001` through `BP-016` required before Stage 1 sign-off.

**What does NOT change:** The core error formula (`§3.5`), velocity formula (`§3.2`), and
state machine (`§3.8`) are unaffected. `BodyPart` modifiers are applied in the
`INITIATING` state before the existing pipeline runs — no pipeline restructuring needed.

---

### 7.1.2 Team Instruction Modifiers

**What it does:** Connects formation-level passing instructions to the physical parameters
of every pass. A "short passing" team instruction shifts velocity targets toward
`V_MIN` for each pass type and tightens error tolerances; a "direct play" instruction
favours higher velocity and accepts higher launch angles. These are team-wide modifiers,
not per-player — every agent on the team executing a pass applies the same instruction
context.

**Why Stage 1:** Requires the Formation / Instructions System (not yet designed). Stage 0
does not have a tactical instruction layer.

**Instruction types (preliminary):**

| Instruction | Velocity Effect | Error Effect | Favoured Pass Types |
|-------------|----------------|-------------|---------------------|
| Short Passing | V target ← V_MIN + 20% of range | Error base ×0.95 | Ground, ThroughBall |
| Direct Play | V target ← V_MAX − 10% of range | Error base ×1.05 | Driven, Lofted, Aerial ThroughBall |
| Counter | V target unchanged | Error base ×1.10 (urgency bias) | Driven, ThroughBall |
| Possession | V target ← V_MIN + 30% of range | Error base ×0.90 | Ground only |

Error effect reflects that short-passing instructions encourage agents to choose safer
options that they are more accurate at; direct play encourages riskier passes.

**Existing architectural hooks:**

`PassExecutor.cs` (Section 4 file structure) already passes `PassRequest` through a
single evaluation pipeline. At Stage 1, a `TeamInstructionContext` struct is injected
into `PassExecutor` (via constructor or per-evaluation parameter) and applied as a
pre-processing step before the existing `§3.2` velocity formula runs.

```csharp
// Planned Stage 1 struct — not present at Stage 0
public struct TeamInstructionContext
{
    public PassingInstruction Instruction;  // Enum: SHORT, DIRECT, COUNTER, POSSESSION
    public float              VelocityBias; // [0, 1] — 0 = V_MIN, 1 = V_MAX
    public float              ErrorScale;   // Multiplier on BASE_ERROR
}
```

**What does NOT change:** Per-agent attribute calculations are unaffected. The instruction
context modifies the target the agent is trying to execute, not the agent's physical
capability. A poor passer under "Short Passing" instructions is still a poor passer; the
instruction just steers them toward shorter options.

---

### 7.1.3 Pass Statistics Integration

**What it does:** Captures per-player passing statistics for consumption by the Statistics
Engine (Stage 1) and ultimately the management UI. The data captured enables key
performance indicators: pass completion rate, key pass frequency, error angle
distribution, and pass type mix.

**Why Stage 1:** Requires the Statistics Engine. The events that carry statistics data
(`PassAttemptEvent`, `PassCompletedEvent`) are already published in Stage 0 — they carry
minimal fields. Stage 1 extends the event payloads with statistics fields.

**Statistics fields added to existing events:**

```csharp
// PassAttemptEvent — Stage 1 additions marked [STAGE-1-STATS]
public struct PassAttemptEvent
{
    // Stage 0 fields (unchanged)
    public int     PasserId;
    public PassType PassType;
    public Vector3 IntendedTargetPosition;
    public float   ErrorAngleDegrees;
    public int     Frame;

    // [STAGE-1-STATS] — Not populated in Stage 0; default values
    public float   DistanceMetres;
    public float   PressureScalarAtContact;
    public bool    IsWeakFoot;
    public bool    IsKeyPass;          // Determined by Decision Tree context
    public int     MatchMinute;
}
```

```csharp
// PassCompletedEvent — Stage 1 additions marked [STAGE-1-STATS]
public struct PassCompletedEvent
{
    // Stage 0 fields (unchanged)
    public int     PasserId;
    public int     ReceiverId;
    public PassType PassType;
    public float   DistanceMetres;
    public int     Frame;

    // [STAGE-1-STATS]
    public bool    WasAccurate;        // ErrorAngle < threshold for pass type
    public float   FinalErrorAngle;    // Actual angle delivered vs intended
}
```

**What does NOT change:** Stage 0 event publishing code in `PassEvents.cs` is additive.
New fields default to zero/false at Stage 0. Statistics consumers at Stage 1 check for
non-zero values before aggregating.

---

### 7.1.4 Preferred Foot Profile (WeakFootRating Upgrade)

**What it does:** Replaces the Stage 0 `WeakFootRating` scalar (a single float on
`PlayerAttributes`) with a full `FootPreferenceProfile` struct that captures preferred
foot identity, weak foot skill rating, and body orientation preferences for each foot.
This enables realistic two-footedness spectrums — a player with `WeakFootRating = 5`
(Stage 0) is equivalent to `FootPreferenceProfile { PreferredFoot = RIGHT, WeakRating = 5 }`
in Stage 1.

**Why Stage 1:** Depends on ERR-007 resolution (KickPower, WeakFootRating, Crossing added
to `PlayerAttributes`). The scalar model is deliberately minimal for Stage 0 to unblock
the rest of the spec; Stage 1 is the planned upgrade point.

**Planned `FootPreferenceProfile` struct:**

```csharp
// Planned Stage 1 struct — not present at Stage 0
public struct FootPreferenceProfile
{
    public FootSide PreferredFoot;     // Enum: LEFT, RIGHT
    public int      WeakFootRating;    // [1, 20] — mirrors Stage 0 scalar
    public float    WeakFootNaturalSpin; // Sidespin bias when using weak foot
    public float    WeakFootAngleBias;   // Body orientation correction tendency
}
```

`WeakFootNaturalSpin` captures that players using their weak foot tend to produce
unintended sidespin; `WeakFootAngleBias` captures that weak-foot passes are often slightly
misdirected in the agent's preferred direction. Both are small effects at Stage 1 —
refinement candidates for Stage 2.

**Migration from Stage 0:** `PlayerAttributes.WeakFootRating` (scalar) maps directly to
`FootPreferenceProfile.WeakFootRating`. No formula changes required; the Stage 0
weak foot penalty formula is preserved and extended, not replaced.

---

## 7.2 Stage 2 Extensions (Years 3–4)

Stage 2 focuses on environmental realism and player individuality. Pass Mechanics is a
secondary system for most Stage 2 additions — surface effects are primarily owned by Ball
Physics, and player tendency vectors are primarily owned by the Decision Tree. Pass
Mechanics impact at Stage 2 is modest.

---

### 7.2.1 Surface Condition Effects

**What it does:** Wet, heavy, or artificial turf conditions affect pass execution. The
effects are distributed across three specifications; Pass Mechanics' role is narrow.

**Responsibility distribution:**

| Effect | Owner | Pass Mechanics Role |
|--------|-------|---------------------|
| Ground pass rolls further on wet pitch | Ball Physics friction model | None — Ball Physics handles post-kick |
| Lofted pass landing unpredictable on heavy pitch | First Touch reception quality | None — First Touch handles reception |
| Driven pass slows faster on heavy pitch | Ball Physics drag model | None |
| Agent slips during windup on wet pitch | **Pass Mechanics** | Applies `SURFACE_WINDUP_ERROR_SCALE` to error base |
| Lofted pass launch angle error increases on heavy pitch | **Pass Mechanics** | Applies `SURFACE_ANGLE_ERROR_SCALE` to launch angle derivation |

Pass Mechanics' two surface effects are both error scalars applied in `PassErrorCalculator.cs`
before the existing error formula runs. No new pipeline stages needed.

**Existing architectural hooks:**

`BallState.SurfaceType` (Ball Physics §3.1) is already populated at Stage 0 (value:
`GRASS_DRY`). Pass Mechanics reads this value at Stage 2 via the same Ball Physics
interface it already uses.

```csharp
// Pass Mechanics Stage 2 surface modifier — reads existing Ball Physics state
float surfaceWindupScale = PassConstants.SURFACE_WINDUP_ERROR[BallState.SurfaceType];
float surfaceAngleScale  = PassConstants.SURFACE_ANGLE_ERROR[BallState.SurfaceType];
```

All surface modifier tables are `[GAMEPLAY-TUNABLE]` — derived from observational data
on performance degradation in wet conditions, not first-principles physics.

---

### 7.2.2 Player-Specific Passing Tendencies

**What it does:** Individual players develop characteristic passing styles — a player may
strongly prefer short ground passes, or disproportionately attempt long diagonals. These
tendencies influence pass selection probability in the Decision Tree rather than the
physical execution parameters in Pass Mechanics.

**Pass Mechanics impact:** Limited to one field change. The Stage 0 `WeakFootRating`
scalar (upgraded to `FootPreferenceProfile` at Stage 1 per §7.1.4) is further extended
at Stage 2 with a `PassStyleVector` struct on `PlayerAttributes` that encodes the
agent's tendency distribution across pass types.

```csharp
// Planned Stage 2 struct — illustrative
public struct PassStyleVector
{
    public float GroundBias;        // Preference weight for Ground passes
    public float DrivenBias;
    public float LoftedBias;
    public float ThroughBallBias;
    public float CrossBias;
    public float ChipBias;
    // Weights are relative; normalized by Decision Tree at selection time
}
```

**Pass Mechanics does not consume `PassStyleVector` directly.** The vector modifies
pass type selection probability in the Decision Tree. Once a pass type is selected and
the `PassRequest` arrives at Pass Mechanics, execution is identical regardless of the
player's tendency distribution. The boundary between Decision Tree reasoning and Pass
Mechanics execution is absolute (KD-2, Section 1.3).

---

### 7.2.3 Advanced Curling Pass Model

**What it does:** Extends the Stage 0 spin vector calculation (§3.4) for cross passes to
support more physically accurate curling trajectories. Stage 0 uses discrete cross
sub-types (Flat, Whipped, High) with fixed spin profiles. Stage 2 introduces a continuous
`CrossCurlIntensity` parameter `[0, 1]` that interpolates between spin profiles, enabling
a richer range of curling deliveries from inside-foot whipped crosses to outside-foot
floated balls.

**Why not Stage 0 or Stage 1:** The three discrete sub-types cover all tactically
meaningful cross shapes at Stage 0 and Stage 1. A continuous model requires spin
validation against Ball Physics Magnus force calibration data (Outline risk #5) and
extended testing across the continuous parameter space — work appropriate for Stage 2
when the system is stable.

**Existing architectural hooks:**

`PassRequest.CrossSubType` (Section 2.4.1) is an enum field at Stage 0. At Stage 2, the
enum is replaced by:

```csharp
// Stage 2 replacement for CrossSubType
public float CrossCurlIntensity;  // [0, 1]; 0 = Flat, 0.5 = Whipped, 1 = Max curl
public CrossCurlDirection CrossCurlDirection;  // Enum: INWARD, OUTWARD
```

The `PassTypeProfiles` table for cross types is replaced by an interpolation function
between the three Stage 0 profiles. All Stage 0 cross tests (SV-* and IT-* cross
scenarios) continue to pass using their original discrete values mapped to the continuous
scale.

**Risk:** Spin magnitudes must be validated against Ball Physics Magnus constants before
Stage 2 implementation begins. An over-specified spin vector at high `CrossCurlIntensity`
values could produce implausible trajectories that Ball Physics clips silently.
See §7.8 — KR-4.

---

## 7.3 Stage 3+ Extensions (Years 5–11+)

Long-horizon extensions that depend on systems not yet designed. Architectural commitment
at Stage 0 is minimal: reserve field names, document the intended interface, and avoid
structural choices that would block implementation.

---

### 7.3.1 Form System Modifier

**What it does:** A player's recent form (rolling performance average across the past
5–10 matches) applies a multiplicative modifier to their effective `Passing` and
`Technique` attributes inside Pass Mechanics. A player in excellent form passes as if
their attributes were slightly higher; a player in poor form passes as if lower. The
modifier is bounded — form cannot exceed `[FORM_MODIFIER_MIN, FORM_MODIFIER_MAX]` —
and does not replace the base attribute.

**Attribute impact:**

```
EffectivePassing  = PlayerAttributes.Passing  × FormModifier
EffectiveTechnique = PlayerAttributes.Technique × FormModifier
```

Both effective values are used only inside Pass Mechanics' error formula (§3.5) and
launch angle derivation (§3.3). Agent Movement, Collision System, and all other
consumers continue to use the raw `PlayerAttributes` values.

**Existing architectural hooks:**

`PlayerAttributes.FormModifier` is defined in Agent Movement §3.5.6 as a reserved field
set to `1.0f` at Stage 0. Pass Mechanics does not read it at Stage 0. At Stage 3, the
error and launch angle calculators begin reading it.

No Pass Mechanics structural change is needed. The two formula sites in
`PassErrorCalculator.cs` and `PassVelocityCalculator.cs` add a single multiply before
using the relevant attribute value.

---

### 7.3.2 Psychology and Crowd Pressure

**What it does:** High-pressure match contexts — penalty shootouts, injury-time leads,
hostile crowd environments — impose an additional error multiplier on top of the existing
`PressureScalar` (§3.5). This models the psychological element of passing under mental
stress as distinct from the physical pressure of an opponent pressing. A player with high
`Composure` attribute resists psychological pressure; a low-Composure player degrades
significantly.

**Psychology modifier formula (planned Stage 4):**

```
PsychologyErrorScale = 1.0 + (MatchStressLevel × (1 - Composure/20) × PSYCH_SENSITIVITY)
```

Where `MatchStressLevel` is a float `[0, 1]` computed by the H-Gate System (Master
Volume 2 — not yet designed) from crowd noise, match score context, and individual
player history.

**Existing architectural hooks:**

`PlayerAttributes.PsychologyModifier` is reserved in Agent Movement §3.5.6 (`1.0f` at
Stage 0). At Stage 4, `PassErrorCalculator.cs` reads it and applies it to the existing
multiplier chain. The modification is additive to the existing chain — no restructuring.

The `PressureScalar` field in §3.5 already represents physical pressure from nearby
opponents. `PsychologyErrorScale` is a separate multiplicand to preserve the distinction
between physical and psychological pressure in both code and documentation.

---

### 7.3.3 Fixed64 Determinism Migration

**What it does:** Replaces all `float` calculations in Pass Mechanics with `Fixed64`
fixed-point arithmetic to guarantee bit-identical results across all platforms and
hardware configurations. This is a mandatory prerequisite for network multiplayer at
Stage 6.

**Why Stage 5:** The Fixed64 library must be selected and validated first (see Agent
Movement §6.4.1, Ball Physics §7.4). All physics specifications migrate simultaneously
to avoid cross-spec floating-point divergence during the transition period.

**Migration scope within Pass Mechanics:**

| Component | Migration Complexity | Notes |
|-----------|---------------------|-------|
| `PassVelocityCalculator.cs` | Low | Multiply-add chain; straightforward |
| `PassErrorCalculator.cs` | Low | Multiply-add chain; straightforward |
| `PassTargetResolver.cs` | Medium | 3D vector math; projection requires Fixed64 sqrt |
| Launch angle derivation (§3.3) | High | `atan2`, `sin`, `cos` — requires Fixed64 trig approximation |
| Spin vector calculation (§3.4) | Low | Multiply only; no trig |
| `PassTypeProfiles.cs` constants | Low | Restate all float literals as Fixed64 |

The launch angle derivation is the highest-risk migration point. The Section 6 trig cost
evaluation (§6.4) explicitly chose runtime trig for Stage 0; at Stage 5, the `atan2`
and trigonometric calls must be replaced by the Fixed64 library's approximations. The
accuracy impact on launch angle must be re-validated against the VS-* scenarios
(Section 5.11) before Stage 5 sign-off.

**Coordination required:** Fixed64 migration follows the same approach documented in
Ball Physics §7.4 and Agent Movement §6.5. A shared `Fixed64MathLibrary` specification
(Spec #8) is required before any migration work begins.

**Backwards compatibility:** The `IPassMechanics` interface (Section 4) does not change.
Float-to-Fixed64 migration is an internal implementation change; callers (Decision Tree)
are unaffected.

---

### 7.3.4 Network-Synchronized Pass Events

**What it does:** At Stage 6 (multiplayer), pass events must be synchronized across game
instances with deterministic frame accuracy. `PassAttemptEvent` and `PassCompletedEvent`
gain network metadata fields (`SessionId`, `MatchId`, `ClientTimestamp`) and are
serialized over the network event bus.

**Pass Mechanics impact:** Minimal. The event publishing code in `PassEvents.cs` (§3.9)
already produces lightweight structs with frame numbers — the core structure is already
network-ready. Stage 6 adds serialization metadata fields and registers events with the
network layer.

**Prerequisite:** Fixed64 migration (§7.3.3) must be complete. Synchronizing float-based
pass calculations across machines is not viable; network events must carry deterministic
results.

---

## 7.4 Permanently Excluded Features

The following features have been considered and **rejected**. These are not deferrals.
They will not be implemented at any stage. If a future requirement appears to necessitate
one of these features, a formal design review with full cross-spec impact assessment is
required before any reconsideration.

---

### 7.4.1 Random Dice-Roll Pass Error (Rejected)

**What it would do:** Replace the deterministic error model (§3.5) with a probability
distribution that generates a random error angle each time a pass is executed. A "poor
pass" would be defined by a random draw exceeding a threshold rather than by attribute
and physics inputs.

**Why permanently rejected:**

1. **Violates the core determinism requirement.** Master Volume 1 §1.3 mandates that
   identical inputs always produce identical simulation outputs. A random error model is
   architecturally incompatible with this requirement. Replay systems, determinism tests,
   and multiplayer synchronization all depend on this guarantee.

2. **Erases the distinction between skill levels.** A `Passing = 1` agent and a
   `Passing = 20` agent both have some probability of succeeding and some probability of
   failing under a dice-roll model. This destroys the meaningful attribute differentiation
   that makes player development and scouting systems meaningful — a key design pillar of
   a Football Manager-class simulation.

3. **Produces incoherent outcomes over time.** A random model cannot be improved by
   training, tactical positioning, or tactical instruction. The simulation's feedback loops
   — improve player attributes, improve pass accuracy — cease to function.

**Correct alternative:** The deterministic error model already produces a range of
outcomes that feels "random" to an observer because inputs (pressure, fatigue,
orientation, urgency) vary from moment to moment. The apparent randomness is emergent
from realistic inputs, not seeded. This is the only architecturally sound approach.

---

### 7.4.2 Ball Magnet / Auto-Completion (Rejected)

**What it would do:** Detect that a pass is on a trajectory near a target receiver and
apply a corrective force that curves the ball toward the receiver, guaranteeing or
near-guaranteeing reception.

**Why permanently rejected:**

1. **Directly contradicts the physics-first architecture.** Pass Mechanics calls
   `Ball.ApplyKick()` (Section 1.4) and its responsibility ends. The ball's trajectory
   after that call is entirely owned by Ball Physics. Any mid-flight correction is a
   violation of the architectural boundary between Pass Mechanics and Ball Physics.

2. **Creates an invisible quality floor that cannot be tuned independently.** A ball
   magnet creates a hidden accuracy bonus that is not represented in any attribute,
   formula, or constant. It is not auditable, not testable in isolation, and impossible to
   balance — adding or removing it changes every pass in the game simultaneously.

3. **Eliminates a key gameplay consequence.** Over-hit and under-hit passes that sail
   past a receiver are legitimate game events that create interception opportunities,
   counter-attack scenarios, and goalkeeper collections. Suppressing them impoverishes
   the game's tactical space.

**Correct alternative:** If the goal is to make elite passers feel dominant, achieve this
through the attribute model — reduce `BASE_ERROR` for high `Passing` values, increase
the velocity ceiling, reduce the pressure sensitivity. These are auditable, tunable
parameters that produce the same subjective effect without architectural compromise.

---

### 7.4.3 Tactical AI Reasoning Inside Pass Mechanics (Rejected)

**What it would do:** Allow Pass Mechanics to inspect the game state — opponent positions,
pressure map, tactical instructions — and modify the pass type or target based on its
own assessment of the situation.

**Why permanently rejected:**

1. **The architectural boundary between Decision Tree and Pass Mechanics is absolute**
   (KD-2, Section 1.3). Decision Tree (#8) selects pass type, target, and urgency. Pass
   Mechanics executes. This boundary exists to prevent responsibility overlap, enable
   independent testing of each system, and ensure that the tactical layer is always
   auditable in one place.

2. **Creates untestable emergent behaviour.** If Pass Mechanics can override a pass type
   based on game state, the actual pass type executed is no longer determinable from the
   `PassRequest` alone. Section 5 unit tests become invalid — they cannot verify execution
   of a known input if Pass Mechanics can silently change it.

3. **Doubles the scope of this specification without proportionate benefit.** The Decision
   Tree specification exists precisely to contain this reasoning. Adding tactical logic
   here means maintaining it in two places simultaneously.

**Correct alternative:** All tactical reasoning belongs in Decision Tree (#8). If a new
tactical behaviour is needed — e.g., automatically downgrading a through ball to a safe
ground pass under extreme pressure — it is implemented in Decision Tree, which then sends
a different `PassRequest` to Pass Mechanics.

---

## 7.5 Architectural Hooks Summary

A consolidated reference for all Stage 0 fields and interfaces that serve as extension
points for future stages. Implementers must not remove or repurpose these hooks even if
they appear unused at Stage 0.

| Hook | Location | Stage 0 Value | Consumed At | Extension |
|------|-----------|--------------|-------------|-----------|
| `PassRequest.BodyPart` | Section 2.4.1 | `BodyPart.INSTEP` | Stage 1 | §7.1.1 Body Part Differentiation |
| `PassAttemptEvent.IsKeyPass` | Section 3.9 | `false` | Stage 1 | §7.1.3 Statistics |
| `PassAttemptEvent.PressureScalarAtContact` | Section 3.9 | `0.0f` | Stage 1 | §7.1.3 Statistics |
| `PassCompletedEvent.WasAccurate` | Section 3.9 | `false` | Stage 1 | §7.1.3 Statistics |
| `PlayerAttributes.WeakFootRating` | Agent Movement §3.5.x | Scalar float | Stage 1 | §7.1.4 Foot Profile upgrade |
| `PlayerAttributes.FormModifier` | Agent Movement §3.5.6 | `1.0f` | Stage 3 | §7.3.1 Form System |
| `PlayerAttributes.PsychologyModifier` | Agent Movement §3.5.6 | `1.0f` | Stage 4 | §7.3.2 Psychology |
| `BallState.SurfaceType` | Ball Physics §3.1 | `GRASS_DRY` | Stage 2 | §7.2.1 Surface Conditions |
| Static-only `PassVelocityCalculator` | Section 4 | `float` | Stage 5 | §7.3.3 Fixed64 migration |
| Static-only `PassErrorCalculator` | Section 4 | `float` | Stage 5 | §7.3.3 Fixed64 migration |
| `PassRequest.CrossSubType` | Section 2.4.1 | Enum (Flat/Whipped/High) | Stage 2 | §7.2.3 Curling model |

---

## 7.6 Cross-Spec Dependency Map for Extensions

| Extension | Depends On | Dependency Type | When |
|-----------|-----------|-----------------|------|
| §7.1.1 Body Part Differentiation | Animation System (TBD) | New interface | Stage 1 |
| §7.1.1 Body Part Differentiation | Decision Tree Spec #8 | BodyPart field population | Stage 1 |
| §7.1.2 Team Instruction Modifiers | Formation / Instructions System (TBD) | New struct injection | Stage 1 |
| §7.1.3 Pass Statistics | Statistics Engine (TBD) | Event consumer | Stage 1 |
| §7.1.3 Pass Statistics | Event System Spec #17 | Extended event fields | Stage 1 |
| §7.1.4 Preferred Foot Profile | Agent Movement Spec #2 | ERR-007 resolution | Stage 1 |
| §7.2.1 Surface Conditions | Ball Physics Spec #1 | `SurfaceType` enum | Stage 2 |
| §7.2.1 Surface Conditions | Pitch Condition System (TBD) | Shared modifier source | Stage 2 |
| §7.2.2 Player Tendencies | Decision Tree Spec #8 | PassStyleVector population | Stage 2 |
| §7.2.2 Player Tendencies | Agent Movement Spec #2 | `PassStyleVector` on attributes | Stage 2 |
| §7.2.3 Curling Model | Ball Physics Spec #1 | Magnus force re-validation | Stage 2 |
| §7.3.1 Form Modifier | Form System (Master Vol 2) | `FormModifier` attribute field | Stage 3 |
| §7.3.2 Psychology | H-Gate System (Master Vol 2) | `PsychologyModifier` field | Stage 4 |
| §7.3.3 Fixed64 Migration | Fixed64 Math Library Spec #9 | Shared library | Stage 5 |
| §7.3.3 Fixed64 Migration | Ball Physics Spec #1 §7.4 | Coordinated migration | Stage 5 |
| §7.3.3 Fixed64 Migration | Agent Movement Spec #2 §6.5 | Coordinated migration | Stage 5 |
| §7.3.4 Network Events | Network / Multiplayer System (TBD) | Event serialization | Stage 6 |

---

## 7.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All 100 Section 5 tests continue to pass** with default parameters — `BodyPart.INSTEP`,
   `SurfaceType.GRASS_DRY`, `FormModifier = 1.0f`, `PsychologyModifier = 1.0f`, no team
   instruction context. Extensions add new test series but must not break the existing
   PT-*, PV-*, LA-*, SV-*, PE-*, TR-*, WF-*, PSM-*, EC-*, IT-*, VS-* suite.

2. **`EvaluatePass()` signature is additive only.** The core function signature may gain
   optional parameters (e.g., `TeamInstructionContext?`) but must never remove or reorder
   existing parameters. Callers using the Stage 0 signature must continue to compile and
   produce identical results when optional parameters are absent.

3. **`PassRequest` struct is additive only.** New fields may be appended. Existing
   field offsets must not change. Decision Tree callers that only populate Stage 0 fields
   must continue to work without recompilation — new optional fields default to their
   Stage 0 neutral values (`BodyPart.INSTEP`, etc.).

4. **`PassAttemptEvent` and `PassCompletedEvent` are additive only.** New statistics
   fields may be appended. Existing field offsets must not change. Statistics consumers
   written at Stage 1 that only read Stage 1 fields must continue to compile at Stage 2+.

5. **Determinism is never compromised.** Any extension that introduces
   non-determinism — `System.Random`, time-dependent values, platform-specific behaviour,
   network timing — is a blocking defect regardless of stage. This constraint is absolute
   and non-negotiable per Master Volume 1 §1.3.

6. **Zero-allocation policy persists.** No extension may introduce heap allocations in
   the hot path of `EvaluatePass()`. Extensions that require allocation (e.g., querying
   an animation provider or statistics engine) must use pre-pooled result objects or
   value-type returns.

---

## 7.8 Known Risks for Future Stages

| ID | Risk | Severity | Stage | Mitigation |
|----|------|----------|-------|------------|
| KR-1 | `BodyPart` modifier stack compounds with `WeakFootRating`; worst case (heel + weak foot) may produce extreme error angles | Medium | Stage 1 | Add integration test for full modifier stack with minimum attributes and heel contact; establish floor via `MIN_EFFECTIVE_ERROR` clamping |
| KR-2 | Team Instruction Modifiers may conflict with Decision Tree pass type selection — a "Short Passing" instruction injected into Pass Mechanics after a Driven pass is selected creates an incoherent result | High | Stage 1 | Resolve instruction application point: Decision Tree should filter pass type selection using instruction context BEFORE creating `PassRequest`; Pass Mechanics instruction modifier is a secondary velocity nudge only, not a pass type override |
| KR-3 | `CrossCurlIntensity` continuous parameter (§7.2.3) may allow exploitable spin magnitudes that Ball Physics cannot clip cleanly | Medium | Stage 2 | Validate spin envelope against Ball Physics Magnus constants before Stage 2 implementation begins; define hard cap `MAX_CROSS_CURL_SPIN` in `PassConstants.cs` |
| KR-4 | Surface modifier stack (§7.2.1) applied on top of pressure and fatigue may make wet-pitch passing unplayably inaccurate | Medium | Stage 2 | Introduce `SURFACE_ERROR_FLOOR` constant; playtest with realistic wet-pitch match scenarios using worst-case attribute combinations |
| KR-5 | Form + Psychology + BodyPart + WeakFoot modifier stack may produce error angles that exceed `MAX_ERROR` per pass type | Medium | Stage 4 | The existing error clamp in §3.5 already enforces `MAX_ERROR`; verify clamp still applies after all modifiers; add full-stack integration test series before Stage 4 sign-off |
| KR-6 | Fixed64 `atan2` approximation introduces measurable launch angle error relative to float baseline | High | Stage 5 | Re-run all VS-* validation scenarios against Fixed64 implementation; require ±0.5° maximum deviation from float reference before Stage 5 sign-off; document deviation in Fixed64 Spec #9 |
| KR-7 | `PassRequest.CrossSubType` enum → `CrossCurlIntensity` float migration at Stage 2 is a breaking API change for Decision Tree callers | Medium | Stage 2 | Provide overloaded `PassRequest` constructor accepting old enum and mapping to float; deprecate enum after one stage cycle |
| KR-8 | `PassAttemptEvent` does not carry a version field; multi-stage consumers may silently process stale event formats | Low | Stage 1 | Add `EventVersion` field to both pass events at Stage 1 before any statistics consumers are written |

---

