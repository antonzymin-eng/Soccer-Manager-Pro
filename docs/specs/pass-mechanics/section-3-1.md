# Pass Mechanics Specification #5 — Section 3.1: Pass Type Taxonomy and Physical Profiles

**File:** `Pass_Mechanics_Spec_Section_3_1_v1_1.md`
**Purpose:** Defines the complete pass type classification system for Pass Mechanics
Specification #5. Establishes the physical profile for each of the seven pass types
(plus cross sub-types), including velocity ranges, launch angle ranges, dominant spin
signatures, and distance bounds. This subsection is the authoritative data source
consumed by §3.2 (Velocity), §3.3 (Launch Angle), and §3.4 (Spin Vector).

**Created:** February 20, 2026
**Revised:** February 21, 2026
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisite:** Section 2 v1.0 (Functional Requirements and Architecture)

**v1.1 Changes:** Added `vOffset` field to `PhysicalProfile` record and master table.
`vOffset` is the per-type minimum practical kick speed used as the interpolation base in
the §3.2 velocity formula (AM-003-001). Supersedes the v1.0 record definition.

**Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes`. Does not block §3.1 (profiles do not reference these attributes
  directly), but blocks §3.2 and §3.7.
- `[GT]` — Denotes Gameplay-Tunable constants. Values are physically-motivated
  estimates requiring tuning during gameplay balancing. See §3.1.3 for policy.

---

## Table of Contents

- [3.1.1 Overview: Why Classify Pass Types?](#311-overview-why-classify-pass-types)
- [3.1.2 Pass Type Enum Definition](#312-pass-type-enum-definition)
- [3.1.3 Physical Profile Record — Definition and Policy](#313-physical-profile-record--definition-and-policy)
- [3.1.4 Master Physical Profile Table](#314-master-physical-profile-table)
- [3.1.5 Ground Pass Profile](#315-ground-pass-profile)
- [3.1.6 Driven Pass Profile](#316-driven-pass-profile)
- [3.1.7 Lofted Pass Profile](#317-lofted-pass-profile)
- [3.1.8 Through Ball Profile (Ground)](#318-through-ball-profile-ground)
- [3.1.9 Aerial Through Ball Profile](#319-aerial-through-ball-profile)
- [3.1.10 Cross Profile](#3110-cross-profile)
- [3.1.11 Chip Profile](#3111-chip-profile)
- [3.1.12 Profile Lookup — Algorithm and Validation](#3112-profile-lookup--algorithm-and-validation)
- [3.1.13 Cross-Specification Validation Requirements](#3113-cross-specification-validation-requirements)
- [3.1.14 Open Issues](#3114-open-issues)

---

## 3.1.1 Overview: Why Classify Pass Types?

Each pass type carries a distinct **physical signature** — a recognisable combination of
velocity, launch angle, and spin that produces an identifiable ball trajectory. A driven
40m pass looks and behaves differently from a lofted 40m pass. Without explicit
classification, a continuous parameter space would produce physically implausible results
and create exploitable edge cases where extreme parameter values generate impossible ball
behaviour.

Discrete enumeration enforces three guarantees:

**1. Physical plausibility.** Each pass type is bounded to a velocity and angle range
derived from observational football data. Parameter combinations outside these bounds are
physically impossible or tactically incoherent, and the system rejects them at the
clamping stage (§3.2.4, §3.3.4).

**2. Determinism.** Given the same `PassType` enum value, the same `PhysicalProfile`
record is returned. O(1) lookup with no state dependency ensures bitwise reproducibility
across replays (KD-1, §1.3).

**3. Auditability.** Seven discrete profiles are inspectable, testable, and tunable
independently. A continuous parameter space would require exhaustive boundary testing
across a multi-dimensional surface. The discrete approach bounds test coverage to a
finite, manageable set (see §5 for test specifications).

**Architectural note:** Pass Mechanics does not select the pass type. The Decision Tree
(Spec #8) selects the appropriate type based on tactical context and provides it on the
`PassRequest` struct (KD-2, §1.3). This subsection only defines what each pass type
*means* physically — not when it should be used.

---

## 3.1.2 Pass Type Enum Definition

```
PassType (enum)
├── Ground           // Short-to-medium range, surface-rolling
├── Driven           // Firm, penetrating, low-trajectory
├── Lofted           // High arc, long diagonal, aerial phase
├── ThroughBall      // Ground-level into space behind defensive line
├── AerialThrough    // Aerial ball into space for runner
├── Cross            // Wide delivery into penalty area
└── Chip             // Steep-arc lob over nearby defender/goalkeeper

CrossSubType (optional enum — only valid when PassType = Cross)
├── Flat             // Default if CrossSubType not specified
├── Whipped          // Strong sidespin, mid-trajectory curve
└── High             // Hanging cross, headed finish target
```

**Constraints:**
- `CrossSubType` is only evaluated when `PassType = Cross`. Any other `PassType` with a
  non-null `CrossSubType` field shall log a warning and discard the field.
- An absent or null `CrossSubType` when `PassType = Cross` defaults to `Flat` per KD-6
  (§1.3). This is not a validation error.
- All seven `PassType` values must resolve to a non-null `PhysicalProfile`. A missing
  profile entry is a programming error (FM-01, §2.6).

---

## 3.1.3 Physical Profile Record — Definition and Policy

A `PhysicalProfile` is an immutable data record loaded from `PassTypeProfiles` at system
initialisation. It contains all per-type physical bounds consumed by §3.2, §3.3, and §3.4.

```
PhysicalProfile (record)
{
    PassType        passType            // Enum identifier
    CrossSubType?   crossSubType        // Non-null only for cross profiles
    float           vMin                // Absolute minimum launch speed (m/s) — clamp floor  [GT]
    float           vOffset             // Practical minimum kick speed (m/s) — formula base  [GT]
    float           vMax                // Maximum launch speed (m/s)                         [GT]
    float           angleMin            // Minimum launch angle (degrees)                     [GT]
    float           angleMax            // Maximum launch angle (degrees)                     [GT]
    float           distMin             // Minimum viable distance (m)                        [GT]
    float           distMax             // Maximum viable distance (m)                        [GT]
    SpinType        dominantSpin        // Topspin | Backspin | Sidespin | Mixed
    float           spinMagnitudeBase   // Base spin magnitude (rad/s)                        [GT]
    float           spinMagnitudeMax    // Maximum spin magnitude (rad/s)                     [GT]
    bool            isAerial            // True if significant aerial phase
    bool            isSpaceTargeted     // True if target is a position, not agent
}
```

**`vMin` vs `vOffset` distinction:**
- `vMin` — absolute defensive clamp floor. Ball.ApplyKick() must never receive a velocity
  below this. Exists to prevent zero or near-zero velocity causing undefined physics.
- `vOffset` — minimum practical kick speed for a valid pass of this type. The §3.2
  velocity formula uses `vOffset` as its interpolation base (not `vMin`). A player with
  KickPower=1 at any distance still generates at least `vOffset` launch speed, because
  executing a valid pass requires a minimum mechanical energy input. `vOffset > vMin`
  always. See §3.2 and Amendment AM-003-001 for the full derivation.

**`[GT]` policy:** All values marked `[GT]` are Gameplay-Tunable. They are initialised
to physically-motivated estimates in this specification and are subject to adjustment
during gameplay balancing. Gameplay-Tunable values must:

1. Be stored in a configuration file (`PassTypeProfiles.json`) separate from compiled
   code to allow tuning without recompilation.
2. Be validated at load time against hard physical bounds (documented per field below).
3. Be audited as a group when any single value changes, because profile interactions
   (e.g., velocity-to-distance ratio, spin-to-velocity ratio) can produce systemic
   implausibilities if modified in isolation.

**Hard physical bounds (load-time validation guards):**

| Field             | Absolute Minimum | Absolute Maximum | Rationale                                 |
|-------------------|-----------------|-----------------|-------------------------------------------|
| vMin              | 3.0 m/s         | 35.0 m/s        | Playable range; 35 m/s ≈ world-class shot |
| vOffset           | vMin + 1.0      | vMax − 2.0      | Must exceed vMin; must leave room for power scaling to reach vMax |
| vMax              | vMin + 2.0      | 35.0 m/s        | Must exceed vMin by at least 2 m/s        |
| angleMin          | 0.0°            | 70.0°           | 70° approaches vertical — physically implausible for a pass |
| angleMax          | angleMin + 1.0° | 80.0°           | Must exceed angleMin                      |
| distMin           | 1.0 m           | 100.0 m         | Minimum meaningful pass                   |
| distMax           | distMin + 5.0   | 100.0 m         | Minimum viable distance range             |
| spinMagnitudeBase | 0.0 rad/s       | 150.0 rad/s     | 150 rad/s ≈ maximum physically achievable |
| spinMagnitudeMax  | spinMagnitudeBase | 200.0 rad/s   | Elite technique ceiling                   |

Any profile failing these guards at load time produces a fatal initialisation error.

---

## 3.1.4 Master Physical Profile Table

All values are `[GT]` unless otherwise noted. Cross-check against Ball Physics §3.1
maximum velocity constants before finalising — specifically `BallPhysics.MAX_BALL_SPEED`.

| PassType          | vMin  | vOffset | vMax  | angleMin | angleMax | distMin | distMax | Dominant Spin      | isAerial |
|-------------------|-------|---------|-------|----------|----------|---------|---------|--------------------|----------|
| Ground            | 5.0   | 8.0     | 18.0  | 2°       | 5°       | 3 m     | 30 m    | Topspin            | false    |
| Driven            | 10.0  | 12.0    | 28.0  | 5°       | 12°      | 15 m    | 50 m    | Strong Topspin     | false    |
| Lofted            | 8.0   | 9.0     | 22.0  | 20°      | 45°      | 20 m    | 60 m    | Backspin           | true     |
| ThroughBall       | 6.0   | 8.5     | 20.0  | 2°       | 5°       | 10 m    | 40 m    | Topspin            | false    |
| AerialThrough     | 8.0   | 9.0     | 22.0  | 25°      | 40°      | 20 m    | 50 m    | Backspin           | true     |
| Cross (Flat)      | 8.0   | 10.0    | 26.0  | 8°       | 15°      | 20 m    | 45 m    | Sidespin           | false    |
| Cross (Whipped)   | 8.0   | 10.0    | 24.0  | 15°      | 25°      | 25 m    | 50 m    | Strong Sidespin    | false    |
| Cross (High)      | 8.0   | 10.0    | 20.0  | 25°      | 40°      | 25 m    | 50 m    | Backspin + Sidespin| true     |
| Chip              | 5.0   | 6.0     | 14.0  | 45°      | 65°      | 3 m     | 20 m    | Backspin           | true     |

> **Note:** `vMin` values revised in v1.1 to align with Appendix A.1.5 corrected constants.
> `vOffset` field is new in v1.1 per Amendment AM-003-001. All vOffset values are [GT]
> placeholder estimates requiring Ball Physics drag simulation validation before finalisation.

**⚠ Validation notes:**

1. `Driven.vMax = 28.0 m/s` is the highest velocity in any pass profile. This must be
   verified against `BallPhysics.MAX_BALL_SPEED`. If Ball Physics caps ball speed below
   28 m/s, this profile ceiling is unreachable and must be reduced. **This is a required
   cross-specification check — do not approve §3.1 without confirming it.**

2. `Ground.distMax = 30 m` and `Driven.distMin = 15 m` create a 15m overlap zone where
   both types are physically plausible. This is intentional and correct — the Decision
   Tree selects the type based on tactical intent, not distance alone. Document this
   explicitly in Decision Tree Spec #8.

3. `Chip.distMax = 20 m` is deliberately conservative. A chip over 20m is physically
   possible but rarely tactically appropriate. If playtesting reveals a need for
   longer-range chips, expand to 30 m. Flag for post-playtesting audit.

4. `Cross (Flat).vMin = 16.0 m/s` exceeds `Driven.vMin = 14.0 m/s`. This is correct —
   flat crosses require more pace than equivalent driven passes because the delivery angle
   from wide positions means lower horizontal velocity at arrival.

---

## 3.1.5 Ground Pass Profile

**Use case:** Short-to-medium range passes along the ground surface. The most frequent
pass type in any match simulation. Prioritises reliability over penetration.

### Physical Characteristics

The ground pass uses a near-zero launch angle (2°–5°) — sufficient to clear minor surface
irregularities without generating a meaningful aerial phase. The ball contacts the surface
quickly and enters a rolling phase governed by Ball Physics friction (Spec #1, §4.x).

Moderate topspin causes the ball to accelerate slightly on first ground contact relative
to a spin-neutral ball, then roll. Effective delivery speed perceived by the receiver is
therefore higher than launch speed, which must be accounted for when §3.6 (Target
Resolution) calculates arrival time for player-targeted passes.

Deceleration after landing is entirely owned by Ball Physics (friction model). Pass
Mechanics does not model post-contact deceleration.

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                          |
|---------------------|-------------------|---------------------|------------------------------------------------|
| Velocity (§3.2)     | KickPower [GT]    | —                   | Distance-scaled; linear power-to-velocity      |
| Accuracy / Error (§3.5) | Passing       | —                   | Standard error model applies                   |
| Topspin magnitude (§3.4) | Technique   | —                   | Higher technique = cleaner contact = consistent spin |

### Distance Sensitivity

Ground passes are most accurate over short distances (3–15 m) where angular error
produces small positional deviation at the target. Over 20 m, the same angular error
produces proportionally larger deviation — this is an emergent property of the error
model, not a separate distance penalty. Document this in playtesting notes.

### Failure Modes Specific to This Profile

- If `IntendedDistance > 30 m` and `PassType = Ground`, this is a constraint violation.
  §3.2 will clamp velocity to `vMax = 18.0 m/s` and log a type-distance mismatch warning.
  It is not a system error — the Decision Tree should not request this combination, but
  if it does, graceful degradation is correct.

---

## 3.1.6 Driven Pass Profile

**Use case:** Firm, penetrating passes cutting through or over defensive lines. Higher
velocity than ground pass with slight elevation designed to clear players' legs.

### Physical Characteristics

Low but meaningful elevation (5°–12°) produces one to two ground bounces at medium
range, staying low. This creates a characteristic trajectory that is difficult to
intercept but also harder to receive cleanly — Driven passes have the highest First Touch
difficulty modifier of all ground-trajectory types (see First Touch Spec #4, §3.x for
the incoming ball speed difficulty table).

Strong topspin keeps the trajectory flat and increases bounce speed. At maximum velocity
(28 m/s) and minimum range (15 m), a driven pass delivers approximately 21–24 m/s
effective speed to the receiving agent — the most demanding reception condition in the
system for ground passes.

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                              |
|---------------------|-------------------|---------------------|----------------------------------------------------|
| Velocity (§3.2)     | KickPower         | —                   | Less distance-responsive than ground pass; power-dominant |
| Accuracy / Error (§3.5) | Passing       | Technique           | Technique governs contact cleanliness under pressure |
| Topspin magnitude (§3.4) | Technique   | —                   | Clean contact required for maximum topspin         |

### Distance Sensitivity

Driven passes are most effective from 20–40 m. Below 15 m, the velocity requirement is
physically disproportionate for the distance (the ball clears the target zone before the
receiver can react). The Decision Tree should avoid requesting Driven at distances below
distMin — this is a Decision Tree constraint, not enforced here beyond the distance
warning log.

---

## 3.1.7 Lofted Pass Profile

**Use case:** Long diagonal switches, playing over a press, aerial delivery to a target
in space. Distance over precision.

### Physical Characteristics

High trajectory (20°–45°) with significant aerial phase. Backspin holds the ball in the
air longer by partially counteracting Magnus downforce, and reduces run on landing,
making the ball hold up for the receiving agent.

Critically: small angular error at launch translates to large positional error at
distance. A 1° deviation at 45 m range produces approximately 0.79 m positional error
at target — larger than for any equivalent ground pass at the same range. The error model
(§3.5) must apply a **distance amplification factor** for lofted passes to reflect this
real sensitivity. This is not a penalty — it is a geometric consequence of longer flight
paths.

**Distance amplification factor for lofted passes:**

```
PositionalError = tan(angularError) × IntendedDistance

At 45m range with 2° error: tan(2°) × 45 = 0.035 × 45 = 1.57 m
At 15m range with 2° error: tan(2°) × 15 = 0.035 × 15 = 0.52 m
```

This amplification is inherent in the geometry and requires no separate factor — it
emerges naturally from the angular error model in §3.5 when positional error is computed
from angular deviation × flight distance. Include an explicit note in §3.5 confirming
this is handled correctly.

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                              |
|---------------------|-------------------|---------------------|----------------------------------------------------|
| Velocity (§3.2)     | KickPower         | —                   | Long-range requires high power; significant distance scaling |
| Accuracy / Error (§3.5) | Passing       | —                   | Error sensitivity is highest for this type due to range |
| Launch angle (§3.3) | —                 | IntendedDistance    | Angle is distance-derived, not attribute-driven    |
| Backspin magnitude (§3.4) | Technique | —                   | Backspin hold-up effect scales with technique      |

### Launch Angle Derivation Note

For lofted passes, the launch angle is not a fixed value but a function of
`IntendedDistance`. A 20 m loft requires a steeper angle than a 60 m loft for the same
hang time. §3.3 handles this derivation. Cross-reference §3.3.2 (Lofted Angle Formula).

---

## 3.1.8 Through Ball Profile (Ground)

**Use case:** Penetrating pass into the space behind a defensive line for a runner.
Physically identical to a ground pass — the distinction is entirely in target resolution.

### Physical Characteristics

Ground trajectory. Same spin (topspin) and angle range (2°–5°) as a Ground pass.
The `PhysicalProfile` for ThroughBall is therefore nearly identical to Ground, with two
differences:

1. `isSpaceTargeted = true` — Target is a projected position, not an agent's current
   position. §3.6 handles lead distance calculation separately for this type.
2. Velocity calibration must account for the receiver's running speed arriving at the
   projected contact point simultaneously with the ball (§3.6 Through Ball targeting).

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                                      |
|---------------------|-------------------|---------------------|------------------------------------------------------------|
| Velocity (§3.2)     | KickPower         | —                   | Calibrated for receiver arrival time, not raw distance     |
| Lead distance (§3.6)| Technique (Vision proxy) | —            | Higher Technique = more accurate lead; Stage 0 proxy, see KD-7 |
| Accuracy / Error (§3.5) | Passing       | —                   | Standard model; Vision proxy affects lead, not accuracy    |

### Critical Design Note

The physical profile for ThroughBall and Ground are nearly identical. This is correct and
intentional. The conceptual difference — playing the ball to space vs. to a player —
belongs to target resolution (§3.6), not to the physical profile. Resist any temptation
to artificially differentiate the profiles for "feel"; the receiver's run and the lead
distance model create natural differentiation at the gameplay level.

---

## 3.1.9 Aerial Through Ball Profile

**Use case:** Aerial delivery into space for a runner — typically a ball played over the
defensive line, often for a striker making a run in behind.

### Physical Characteristics

Mid-to-high trajectory (25°–40°) with significant aerial phase. Backspin applied to hold
the ball up and allow the receiver to run onto it rather than chasing an over-hit ball.

This is the most complex pass type in the system because it combines:

1. Space-targeted resolution (`isSpaceTargeted = true`) — lead distance applies.
2. Aerial trajectory — arrival time calculation must account for flight arc, not linear
   distance (§3.3 handles this; §3.6 consumes the result).
3. High first-touch difficulty — aerial reception under forward momentum is among the
   most demanding scenarios in First Touch Spec #4.

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                                         |
|---------------------|-------------------|---------------------|---------------------------------------------------------------|
| Velocity (§3.2)     | KickPower         | —                   | Must achieve sufficient range with aerial arc overhead         |
| Launch angle (§3.3) | —                 | IntendedDistance    | Distance-derived; same derivation as Lofted                   |
| Lead distance (§3.6)| Technique (Vision proxy) | —            | Lead calculation uses aerial arrival time, not ground time    |
| Backspin (§3.4)     | Technique         | —                   | Hold-up effect critical for receiver timing                   |

---

## 3.1.10 Cross Profile

**Use case:** Wide delivery into the penalty area from wide positions. Three sub-types
with meaningfully distinct physical signatures. All are space-targeted passes
(`isSpaceTargeted = true`), targeting a zone in the penalty area rather than a specific
agent.

### Sub-Type: Flat Cross

**Physical profile:** Low trajectory (8°–15°), maximum pace (16–26 m/s), dominant
sidespin for curl. Designed to arrive fast and low — most dangerous delivery type
against a retreating defence. Inswing or outswing curl is produced by sidespin
interacting with Ball Physics Magnus model (Spec #1, §3.x).

The high minimum velocity (16 m/s) reflects the requirement that a flat cross must
arrive with sufficient pace to be a threat before defenders close. A slow flat cross is
not a flat cross — the Decision Tree should use a Lofted or High Cross instead. If
`IntendedVelocity` produces a value below `vMin = 16 m/s`, §3.2 clamps to 16 m/s
(not an error, but logged as an atypical request).

### Sub-Type: Whipped Cross

**Physical profile:** Mid-trajectory (15°–25°), strong sidespin, velocity range 14–24 m/s.
Ball swings sharply through the air — creates a delivery that is simultaneously aerial
(clearing the first defender) and curling (making it difficult for the goalkeeper to
judge). Spin magnitude is higher than flat cross — the sidespin component requires
cross-validation against Ball Physics Magnus constants to verify the curl is visually
plausible at typical cross distances (25–50 m). Flag for §3.4 numerical check.

### Sub-Type: High Cross

**Physical profile:** High trajectory (25°–40°), mixed spin (Backspin + Sidespin),
velocity range 12–20 m/s. Designed to hang in the penalty area for a headed finish.
Backspin holds the ball up; sidespin creates a late curve that makes the delivery harder
for the goalkeeper to claim.

The `dominantSpin = Mixed` designation means §3.4 must apply two spin components
simultaneously: a backspin vector to generate lift-hold, and a sidespin vector for curve.
The combined spin vector is passed as a single `Vector3` to `Ball.ApplyKick()` — the
Ball Physics engine resolves the combined Magnus effect. §3.4 must document the component
composition rule.

### Cross Attribute Mapping (All Sub-Types)

| Output Parameter     | Primary Attribute                | Secondary Attribute | Notes                                          |
|---------------------|----------------------------------|---------------------|------------------------------------------------|
| Velocity (§3.2)     | KickPower                        | —                   | Sub-type velocity ranges differ                |
| Accuracy / Error (§3.5) | Crossing [ERR-007] (proxy: Passing) | —           | Crossing attribute governs cross accuracy      |
| Sidespin (§3.4)     | Technique                        | —                   | Technique governs curl precision               |
| Backspin (§3.4, High Cross only) | Technique           | —                   | Mixed spin; see §3.4 composition rule          |

`[ERR-007-PENDING]` — `Crossing` attribute absent from `PlayerAttributes`. Use `Passing`
as proxy for Stage 0. Document explicitly in §3.5 error model. Stage 1 upgrade point.

---

## 3.1.11 Chip Profile

**Use case:** Lobbing the ball over a nearby defender or goalkeeper at close-to-medium
range. Precision over power.

### Physical Characteristics

Steep launch angle (45°–65°) with strong backspin. The steep angle is required to
generate sufficient height over a close obstacle. Backspin causes the ball to drop
steeply on descent and hold up on landing rather than running through — appropriate for
delivering into a narrow space behind the defender.

The chip is the **most Technique-dependent** pass type. Power contribution to velocity
is lower than any other type — a chip is primarily a placement skill, not a power skill.
The velocity range (5–14 m/s) is narrow and low. A chip requiring more than 14 m/s to
reach the target at any angle within the profile range is outside this pass type's
operational envelope — the Decision Tree should have selected a different type.

`Chip.distMax = 20 m` is conservative and intentional. Beyond 20 m, the angle required
to generate sufficient height drops below the plausible chip signature. The Decision Tree
must not request `PassType = Chip` for distances above 20 m. If it does, §3.2 clamps
velocity and §3.3 adjusts angle to the nearest feasible value — but the result will not
resemble a chip. Log a Decision Tree contract violation warning.

### Attribute Mapping

| Output Parameter     | Primary Attribute | Secondary Attribute | Notes                                                     |
|---------------------|-------------------|---------------------|-----------------------------------------------------------|
| Velocity (§3.2)     | Technique         | KickPower           | Technique is primary — reversed from all other types       |
| Accuracy / Error (§3.5) | Passing       | Technique           | Both contribute; technique affects chip contact quality   |
| Backspin (§3.4)     | Technique         | —                   | Backspin magnitude strongly technique-dependent            |
| Launch angle (§3.3) | IntendedDistance  | —                   | Distance-derived within 45°–65° range                    |

**Design note — Technique as primary velocity driver:**

This is the only pass type where Technique rather than KickPower is the primary velocity
attribute. The rationale is that a chip is fundamentally a contact skill — the ball must
be struck with precise backspin from beneath, requiring technical finesse rather than
raw power. A powerful player with poor technique will overhit chips or fail to generate
sufficient backspin. This attribute role reversal must be clearly documented in §3.2
(Velocity Model) and is explicitly a design decision, not an error.

---

## 3.1.12 Profile Lookup — Algorithm and Validation

### Lookup Algorithm

```
function GetPhysicalProfile(passType: PassType, crossSubType: CrossSubType?) -> PhysicalProfile

    key = (passType, crossSubType ?? CrossSubType.Flat)

    if key not in PassTypeProfiles:
        LogFatal("FM-01: No PhysicalProfile found for key: " + key)
        return PhysicalProfile.DEFAULT_SAFE  // Prevents null reference; emergency fallback only

    profile = PassTypeProfiles[key]

    // Validate profile is internally consistent (load-time check should have caught this,
    // but defensive runtime check on first access is warranted)
    AssertProfileValid(profile)  // Throws if vMin > vMax, angleMin > angleMax, etc.

    return profile

end function
```

**Note on `CrossSubType` handling:**

When `PassType != Cross` and `CrossSubType` is non-null, the key construction must
discard the `CrossSubType` and log a warning. It must not fail — a Decision Tree
providing a spurious `CrossSubType` for a non-cross pass type is a caller error, but
graceful handling is preferable to a crash.

### Lookup Performance

Profile lookup is O(1) via dictionary/map keyed on `(PassType, CrossSubType)`. The
dictionary contains at most 9 entries (7 pass types + 2 cross sub-type overrides).
No allocation occurs at lookup time — profiles are immutable records loaded at
initialisation.

This lookup executes in the `INITIATING` state of the Pass Execution State Machine
(§3.8). It must complete in under 0.1 ms on target hardware per NFR-01 (§2.5).
With 9 entries, lookup is effectively instantaneous — no caching is required.

### Profile Validation — `AssertProfileValid`

The following assertions fire at initialisation for all profiles, and on first runtime
access as a defensive measure:

```
assert profile.vMin >= 3.0 && profile.vMin <= 35.0
assert profile.vMax > profile.vMin
assert profile.vMax <= 35.0
assert profile.angleMin >= 0.0 && profile.angleMin < 80.0
assert profile.angleMax > profile.angleMin
assert profile.distMin >= 1.0
assert profile.distMax > profile.distMin + 5.0
assert profile.spinMagnitudeBase >= 0.0
assert profile.spinMagnitudeMax >= profile.spinMagnitudeBase
```

---

## 3.1.13 Cross-Specification Validation Requirements

The following cross-specification checks must be completed before §3.1 is approved.
These are **blocking** items — downstream sections (§3.2, §3.3, §3.4) cannot be
finalised without their corresponding checks.

| Check ID | Description | Dependency | Blocks |
|----------|-------------|------------|--------|
| XC-3.1-01 | Confirm `BallPhysics.MAX_BALL_SPEED ≥ 28.0 m/s`. If not, reduce `Driven.vMax` to match. | Ball Physics §3.1 | §3.2 |
| XC-3.1-02 | Confirm First Touch Spec #4 incoming-ball-speed difficulty table covers the full velocity range 5–28 m/s. | First Touch §3.x | §3.6 |
| XC-3.1-03 | Confirm Ball Physics Magnus force model handles `spinMagnitudeMax` values without producing physically implausible curl on Whipped Cross and High Cross profiles. Requires numerical check. | Ball Physics §3.x (Magnus) | §3.4 |
| XC-3.1-04 | Confirm `Crossing` attribute status in Agent Movement Spec #2 following ERR-007 resolution. Update Cross profile attribute mapping accordingly. | Agent Movement §3.x, ERR-007 | §3.5, §3.7 |
| XC-3.1-05 | Confirm Decision Tree Spec #8 intent mapping produces valid `(PassType, CrossSubType)` combinations only — specifically that it never passes `CrossSubType` for non-Cross types. | Decision Tree §x.x | §3.1.12 |

---

## 3.1.14 Open Issues

| OI ID | Issue | Impact | Resolution Path |
|-------|-------|--------|-----------------|
| OI-3.1-01 | `Driven.vMax = 28.0 m/s` unverified against Ball Physics cap | Could require profile change | Complete XC-3.1-01 before §3.2 |
| OI-3.1-02 | `Crossing` attribute still pending ERR-007 resolution | Cross profile uses Passing as proxy | ERR-007 → AM-002-001 approval |
| OI-3.1-03 | Whipped Cross sidespin magnitude requires Magnus numerical calibration | Could produce implausible curl at 50 m range | Complete XC-3.1-03 before §3.4 |
| OI-3.1-04 | Chip `distMax = 20 m` may be too conservative | Could require expansion post-playtesting | Flag for playtesting audit; do not change pre-launch |
| OI-3.1-05 | Chip attribute role reversal (Technique primary, KickPower secondary) needs Decision Tree awareness | DT must know not to select Chip for low-Technique agents at close range | Document constraint in §1.5 dependency table when Decision Tree spec is written |

---

## Section 3.1 Summary

This section defines the seven pass types and their physical profiles as the authoritative
data source for Pass Mechanics §3.2–§3.4. Key commitments:

- Discrete enum classification enforces physical plausibility and determinism.
- All profile values are `[GT]` — stored in configuration, validated at load time.
- Cross sub-typing (Flat / Whipped / High) uses enum discriminators per KD-6.
- Chip uniquely uses Technique as primary velocity attribute — design decision, not error.
- Five cross-specification validation checks are required before §3.1 approval (XC-3.1-01 through XC-3.1-05).
- Two dependency flags remain open: ERR-007 (Crossing attribute) and Ball Physics max speed confirmation.

**Next subsection:** §3.2 — Pass Velocity Model

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026 | Claude (AI) / Anton | Initial draft. Seven pass type profiles. PhysicalProfile record definition. Master profile table. Five cross-spec validation checks. |
| 1.1 | February 21, 2026 | Claude (AI) / Anton | Added `vOffset` field to PhysicalProfile record per Amendment AM-003-001. Added `vOffset` column to master profile table and hard physical bounds table. Added load-time validation guard for `vOffset`. Revised `vMin` values to align with Appendix A.1.5 corrected constants (previously some vMin values were higher than their corresponding vOffset, which would have been invalid at load-time). |
| 1.2 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 2 instances); Chip velocity prose corrected 8–16→5–14 m/s to match master table (M-02). |

---

*End of Section 3.1 — Pass Mechanics Specification #5*
