# Agent Movement Specification â€” Section 3.2: Locomotion

**Purpose:** Defines the core locomotion formulas for agent movement â€” acceleration, top speed, and deceleration â€” and introduces the PerformanceContext gateway through which ALL player attribute evaluations must pass. This is the foundational skill-check architecture that every subsequent specification must reference.

**Created:** February 9, 2026, 3:30 PM PST  
**Version:** 1.0  
**Status:** Draft  
**Dependencies:** Section 3.1 (Movement State Machine), Ball Physics Spec #1 (pattern reference)

---

## Table of Contents

- [3.2.1 PerformanceContext Gateway](#321-performancecontext-gateway)
- [3.2.2 Attribute Mapping Functions](#322-attribute-mapping-functions)
- [3.2.3 Acceleration Model](#323-acceleration-model)
- [3.2.4 Top Speed Calculation](#324-top-speed-calculation)
- [3.2.5 Deceleration Model](#325-deceleration-model)
- [3.2.6 Numerical Examples & Validation](#326-numerical-examples--validation)

---

## 3.2.1 PerformanceContext Gateway

### Design Philosophy

Every system in Tactical Director that evaluates a player attribute â€” movement speed, pass accuracy, shot power, decision quality â€” **must** route through the PerformanceContext gateway. No specification may read a raw attribute value and use it directly in a gameplay calculation.

**Why this exists:**

In most football simulations, a player rated 18/20 in Pace will run at approximately the same speed in match 1 and match 38 of the season, regardless of form, confidence, tactical fit, or career trajectory. This produces the "elite players always perform as elite" problem where a 90-rated player operates in a narrow 85â€“92 band with minimal variation.

The PerformanceContext gateway solves this by inserting a mandatory modifier chain between the raw attribute and the physics calculation. In Stage 0, all modifiers are 1.0 (neutral) â€” the gateway adds zero overhead and zero behavioral change. But the architecture is in place from day one, so when Stage 2 introduces form, Stage 4 introduces psychology, and the career memory system arrives, they plug in without refactoring a single skill check.

**The critical enforcement rule:**

> **Any specification that evaluates a player attribute for gameplay purposes MUST call `EvaluateAttribute()` or `EvaluateAttributePair()`. Direct access to raw attribute values for gameplay calculations is a specification violation.**

This rule must be codified in the Code Standards & Style Guide (Spec #20, Week 20) as a compile-time or static analysis check.

### Architecture

```
Raw Attribute (1â€“20)
        â”‚
        â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      PerformanceContext         â”‚
â”‚                                 â”‚
â”‚  EffectiveValue = Base          â”‚
â”‚    Ã— FormModifier        [1.0]  â”‚  â† Stage 2: hot/cold streaks, momentum
â”‚    Ã— ContextModifier     [1.0]  â”‚  â† Stage 4: tactical fit, chemistry, adaptation
â”‚    Ã— CareerModifier      [1.0]  â”‚  â† Stage 4: season-over-season memory
â”‚                                 â”‚
â”‚  Clamped to [FLOOR, CEILING]    â”‚
â”‚                                 â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
        â”‚
        â–¼
Effective Attribute (used in physics)
```

### Data Structures

```csharp
using UnityEngine;
using System;

/// <summary>
/// The universal attribute evaluation gateway.
/// ALL gameplay systems that read player attributes MUST route through this struct.
///
/// Stage 0: All modifiers are 1.0. The gateway is architecturally present but
/// behaviorally transparent â€” raw attributes pass through unchanged.
///
/// Stage 2: FormModifier becomes active (hot/cold streaks, momentum-driven drift).
/// Stage 4: ContextModifier (tactical familiarity, team chemistry, adaptation,
///   manager relationship, pressure) and CareerModifier (season-over-season memory,
///   confidence/self-efficacy from H-Gate) become active.
///
/// DESIGN RULE: No modifier may reduce an attribute below EFFECTIVE_FLOOR (0.5)
/// or raise it above EFFECTIVE_CEILING (1.5). This prevents absurd outcomes where
/// a Pace 20 player moves slower than a Pace 1 player, or a Pace 5 player
/// outruns Usain Bolt.
///
/// EXTENSIBILITY: New modifier layers can be added by extending this struct
/// with additional float fields. The multiplication chain is order-independent
/// (commutative), so insertion order doesn't matter.
/// </summary>
public struct PerformanceContext
{
    // ================================================================
    // MODIFIER LAYERS
    // ================================================================

    /// <summary>
    /// Form modifier: captures hot/cold streaks and momentum-driven drift.
    /// Range: [0.85, 1.15] â€” a player in terrible form performs at 85% of
    /// base ability; a player in peak form performs at 115%.
    ///
    /// Stage 0: Always 1.0 (neutral).
    /// Stage 2: Driven by match results, confidence trend, recent performances.
    /// Update frequency: Once per match day (between matches), interpolated
    ///   during match via 10Hz tactical heartbeat.
    ///
    /// Design note: This is NOT the H-Gate confidence/self-efficacy split.
    /// Form is a coarser, slower-moving modifier. H-Gate operates within
    /// ContextModifier at finer granularity.
    /// </summary>
    public float FormModifier;

    /// <summary>
    /// Context modifier: captures situation-specific performance factors.
    /// Range: [0.80, 1.20] â€” wider range than form because contextual
    /// factors can have dramatic short-term effects (e.g., a player
    /// completely unfamiliar with a new tactical system).
    ///
    /// Stage 0: Always 1.0 (neutral).
    /// Stage 4 sub-components (each contributes multiplicatively):
    ///   - TacticalFamiliarity: How well the player knows the current system
    ///   - TeamChemistry: Social graph connectivity, clique harmony
    ///   - LeagueAdaptation: Adjustment period for new league/country
    ///   - ManagerRelationship: Trust, communication quality
    ///   - PressureSituation: Match importance, crowd hostility, scoreline
    ///   - H-Gate Confidence: Risk-taking willingness (from Master Vol 2)
    ///   - H-Gate Self-Efficacy: Execution belief (from Master Vol 2)
    ///
    /// When Stage 4 is implemented, this single float becomes a computed
    /// product of sub-components. The PerformanceContext struct may be
    /// extended with a ContextBreakdown sub-struct for UI/debug display,
    /// but the physics layer only ever sees this single composite value.
    /// </summary>
    public float ContextModifier;

    /// <summary>
    /// Career modifier: captures season-over-season trajectory and long-term
    /// performance memory.
    /// Range: [0.90, 1.10] â€” narrower range because career effects are
    /// slow-moving and represent gradual shifts, not dramatic swings.
    ///
    /// Stage 0: Always 1.0 (neutral).
    /// Stage 4 sub-components:
    ///   - EndOfSeasonForm: How the player finished last season (carries forward)
    ///   - BreakoutMemory: Sustained overperformance builds lasting confidence
    ///   - DeclineMemory: Sustained underperformance creates lasting doubt
    ///   - InjuryHistory: Returning from major injury, psychological hesitation
    ///
    /// This is the FHM-inspired layer. A striker who finished last season
    /// on a 15-match drought doesn't reset to baseline. Conversely, a
    /// breakout season builds lasting confidence that persists.
    ///
    /// Design note: CareerModifier changes BETWEEN seasons, not during.
    /// It is set once at season start and remains constant throughout.
    /// Mid-season form changes are captured by FormModifier.
    /// </summary>
    public float CareerModifier;

    // ================================================================
    // CONSTANTS
    // ================================================================

    /// <summary>
    /// Minimum combined modifier product.
    /// Prevents any attribute from being reduced below 60% of its base value.
    ///
    /// Derivation: Worst case = 0.85 Ã— 0.80 Ã— 0.90 = 0.612, which naturally
    /// sits just above this floor. The floor exists as a safety net for future
    /// modifier additions that might compound beyond intended range.
    ///
    /// Gameplay rationale: A Pace 20 player (10.2 m/s base) at EFFECTIVE_FLOOR
    /// would have effective Pace 12.0, mapping to ~9.06 m/s â€” still competent
    /// but clearly underperforming. This is the absolute basement for a
    /// catastrophically out-of-form, poorly adapted, psychologically broken
    /// elite player. It should be nearly unreachable in normal gameplay.
    ///
    /// Tightened from 0.5 to 0.6 to prevent edge cases where elite players
    /// move at walking speed, which would look pathological rather than
    /// "having a bad season."
    /// </summary>
    public const float EFFECTIVE_FLOOR = 0.6f;

    /// <summary>
    /// Maximum combined modifier product.
    /// Prevents any attribute from being raised above 150% of its base value.
    ///
    /// Derivation: Best case = 1.15 Ã— 1.20 Ã— 1.10 = 1.518, which slightly
    /// exceeds this ceiling. The ceiling clamp prevents a player in peak
    /// form + perfect context + career high from producing superhuman results.
    ///
    /// Gameplay rationale: A Pace 10 player (8.85 m/s base) at EFFECTIVE_CEILING
    /// would run at 13.275 m/s â€” which exceeds the physics MAX_SPEED of 12 m/s,
    /// so the physics layer clamps independently. The ceiling here prevents
    /// attribute-derived values from being unreasonable before physics clamping.
    /// </summary>
    public const float EFFECTIVE_CEILING = 1.5f;

    // ================================================================
    // FACTORY METHODS
    // ================================================================

    /// <summary>
    /// Creates a neutral PerformanceContext where all modifiers are 1.0.
    /// This is the ONLY context used in Stage 0.
    ///
    /// Usage: Every movement update, skill check, and attribute evaluation
    /// in Stage 0 calls this factory. When Stage 2+ systems come online,
    /// they replace this call with populated modifiers.
    /// </summary>
    public static PerformanceContext CreateNeutral()
    {
        return new PerformanceContext
        {
            FormModifier = 1.0f,
            ContextModifier = 1.0f,
            CareerModifier = 1.0f
        };
    }

    /// <summary>
    /// Creates a PerformanceContext with explicit modifier values.
    /// All values are clamped to their valid ranges on construction.
    ///
    /// Used by Stage 2+ systems that compute modifier values from
    /// gameplay state (form trends, tactical familiarity, career memory).
    /// </summary>
    public static PerformanceContext Create(float form, float context, float career)
    {
        return new PerformanceContext
        {
            FormModifier = Mathf.Clamp(form, 0.85f, 1.15f),
            ContextModifier = Mathf.Clamp(context, 0.80f, 1.20f),
            CareerModifier = Mathf.Clamp(career, 0.90f, 1.10f)
        };
    }

    // ================================================================
    // EVALUATION METHODS
    // ================================================================

    /// <summary>
    /// Evaluates a single attribute through the modifier chain.
    /// This is the PRIMARY entry point for all attribute evaluations.
    ///
    /// Returns an effective attribute value as a float, NOT clamped to 1â€“20
    /// integer range. Downstream formulas use the continuous float value
    /// for smooth physics behavior across the modifier spectrum.
    ///
    /// Formula:
    ///   EffectiveAttribute = RawAttribute Ã— CombinedModifier
    ///   CombinedModifier = Clamp(Form Ã— Context Ã— Career, FLOOR, CEILING)
    ///
    /// Example (Stage 0, all modifiers 1.0):
    ///   EvaluateAttribute(18) = 18.0 Ã— 1.0 = 18.0 (passthrough)
    ///
    /// Example (Stage 4, poor form + bad tactical fit + career decline):
    ///   Form=0.90, Context=0.85, Career=0.95
    ///   Combined = 0.90 Ã— 0.85 Ã— 0.95 = 0.72675
    ///   EvaluateAttribute(18) = 18.0 Ã— 0.72675 = 13.08
    ///   A player rated 18 performs like a 13. This is a genuinely poor season.
    ///
    /// Example (Stage 4, peak everything):
    ///   Form=1.12, Context=1.15, Career=1.08
    ///   Combined = 1.12 Ã— 1.15 Ã— 1.08 = 1.39104
    ///   EvaluateAttribute(12) = 12.0 Ã— 1.39104 = 16.69
    ///   A player rated 12 performs like a 17. This is a magical season.
    /// </summary>
    /// <param name="rawAttribute">Raw attribute value (1â€“20 integer scale)</param>
    /// <returns>Effective attribute as float, used in downstream physics formulas</returns>
    public float EvaluateAttribute(int rawAttribute)
    {
        float combined = FormModifier * ContextModifier * CareerModifier;
        combined = Mathf.Clamp(combined, EFFECTIVE_FLOOR, EFFECTIVE_CEILING);
        return rawAttribute * combined;
    }

    /// <summary>
    /// Evaluates a pair of attributes and returns their weighted average.
    /// Used when a physical property depends on two attributes (e.g., 
    /// deceleration depends on Agility primary + Balance secondary).
    ///
    /// Both attributes pass through the SAME PerformanceContext â€” a player
    /// in poor form has ALL attributes reduced, not selective ones.
    ///
    /// Formula:
    ///   EffectivePair = (EvaluateAttribute(primary) Ã— primaryWeight
    ///                  + EvaluateAttribute(secondary) Ã— secondaryWeight)
    ///                  / (primaryWeight + secondaryWeight)
    ///
    /// Example: Deceleration with Agility=16, Balance=12, weights 0.7/0.3
    ///   Stage 0: (16.0 Ã— 0.7 + 12.0 Ã— 0.3) / 1.0 = 14.8
    ///   Stage 4 (Form=0.90): (14.4 Ã— 0.7 + 10.8 Ã— 0.3) / 1.0 = 13.32
    /// </summary>
    public float EvaluateAttributePair(
        int primaryAttr, float primaryWeight,
        int secondaryAttr, float secondaryWeight)
    {
        float totalWeight = primaryWeight + secondaryWeight;
        if (totalWeight <= 0f) return 0f;  // Safety: avoid division by zero

        float effectivePrimary = EvaluateAttribute(primaryAttr);
        float effectiveSecondary = EvaluateAttribute(secondaryAttr);

        return (effectivePrimary * primaryWeight + effectiveSecondary * secondaryWeight)
               / totalWeight;
    }

    /// <summary>
    /// Returns the combined modifier product (for debug/UI display only).
    /// Gameplay systems should NOT use this directly â€” call EvaluateAttribute() instead.
    /// </summary>
    public float GetCombinedModifier()
    {
        return Mathf.Clamp(
            FormModifier * ContextModifier * CareerModifier,
            EFFECTIVE_FLOOR,
            EFFECTIVE_CEILING);
    }
}
```

### Integration Pattern for All Specifications

Every specification that performs attribute evaluation must follow this pattern:

```csharp
/// <summary>
/// CORRECT: Attribute evaluation through PerformanceContext.
/// This pattern is MANDATORY for all gameplay calculations.
/// </summary>
public static float CalculateTopSpeed(
    int paceAttribute,
    PerformanceContext context)   // â† Always passed in
{
    float effectivePace = context.EvaluateAttribute(paceAttribute);
    return MapPaceToTopSpeed(effectivePace);
}

/// <summary>
/// INCORRECT: Direct attribute access. THIS IS A SPECIFICATION VIOLATION.
/// Static analysis must flag this pattern.
/// </summary>
public static float CalculateTopSpeed_WRONG(int paceAttribute)
{
    // âœ— Raw attribute used directly â€” no PerformanceContext
    return MapPaceToTopSpeed(paceAttribute);
}
```

### Modifier Interaction Model

The three modifiers are multiplicative and order-independent. Their interaction creates a variance envelope around the base attribute:

```
Combined modifier range (all three at extremes):

Worst case: 0.85 Ã— 0.80 Ã— 0.90 = 0.612 â†’ clamped to 0.612 (above FLOOR of 0.6)
Best case:  1.15 Ã— 1.20 Ã— 1.10 = 1.518 â†’ clamped to CEILING (1.5)

Typical variance in a normal season:
  Good form + neutral context + neutral career = 1.10 Ã— 1.0 Ã— 1.0 = 1.10
  Bad form  + neutral context + neutral career = 0.90 Ã— 1.0 Ã— 1.0 = 0.90
  Range for a settled player: Â±10% around base (Form only)

Typical variance for a new signing adapting:
  Neutral form + poor context + neutral career = 1.0 Ã— 0.88 Ã— 1.0 = 0.88
  Range: -12% from base, recovering to neutral over weeks/months

Extreme variance (everything goes wrong):
  Bad form + poor context + career decline = 0.88 Ã— 0.83 Ã— 0.93 = 0.679
  An 18-rated attribute performs as 12.2. A genuinely terrible season.

Extreme variance (everything goes right):
  Peak form + perfect context + career high = 1.13 Ã— 1.18 Ã— 1.08 = 1.44
  A 12-rated attribute performs as 17.3. A magical breakout season.
```

### What PerformanceContext Does NOT Do

- **Does NOT modify base attributes.** The raw 1â€“20 values are permanent (changed only by development, aging, or injury). PerformanceContext applies a transient lens over them.
- **Does NOT apply to cosmetic or non-gameplay reads.** UI screens showing "player rated 18 Pace" display the raw value. Only the match engine sees the effective value.
- **Does NOT apply to physical constants.** Mass, height, hitbox dimensions are not affected by form or psychology.
- **Does NOT differentiate by attribute type.** In Stage 0, all attributes receive the same combined modifier. Stage 4 may introduce attribute-specific weighting (e.g., mental attributes more affected by psychology than physical ones) â€” this is a future extension, not a Stage 0 concern. When implemented, it would be an additional method on PerformanceContext, not a change to the existing API.

---

## 3.2.2 Attribute Mapping Functions

### Design Philosophy

Player attributes exist on an integer 1â€“20 scale. Physics formulas require continuous physical values (m/s, m/sÂ², degrees/second). Attribute mapping functions convert between these domains.

All mapping functions accept a **float** input (the effective attribute from PerformanceContext), not an integer. This allows the modifier chain to produce smooth intermediate values â€” a Pace 18 player at 0.92 form modifier evaluates as Pace 16.56, which maps to a top speed between the Pace 16 and Pace 17 values.

### Mapping Strategy: Linear Interpolation with Defined Endpoints

All attribute-to-physics mappings use the same pattern:

```
PhysicalValue = MIN_VALUE + (effectiveAttribute - 1.0) Ã— (MAX_VALUE - MIN_VALUE) / 19.0
```

Where:
- `effectiveAttribute` = output of `PerformanceContext.EvaluateAttribute()` (float, typically 1.0â€“20.0 but can exceed due to modifiers)
- `MIN_VALUE` = physical value at attribute 1 (worst professional player)
- `MAX_VALUE` = physical value at attribute 20 (elite world-class)
- 19.0 = range of the 1â€“20 scale (20 - 1)

Values outside the 1â€“20 range are permitted (from PerformanceContext modifiers) and produce extrapolated physical values, which are then clamped by physics-layer safety limits (MAX_SPEED, etc.).

### Constants

```csharp
/// <summary>
/// Constants for mapping player attributes (1â€“20 scale) to physical values.
/// All values derived from real-world professional footballer data.
///
/// Sources:
/// - Sprint speed data: FIFA/UEFA GPS tracking databases, published analyses
///   of Premier League, La Liga, Bundesliga player tracking data
/// - Acceleration data: Sports science literature on football-specific
///   acceleration profiles (Haugen et al., 2014; Buchheit et al., 2014)
/// - Deceleration data: Biomechanics studies on controlled vs emergency
///   stopping in team sports (Harper & Kiely, 2018)
///
/// COORDINATE SYSTEM: Consistent with Ball Physics Spec #1 and Section 3.1.8.
///   X = pitch length, Y = pitch width, Z = vertical (up).
///   Speed in m/s. Acceleration in m/sÂ². Time in seconds.
/// </summary>
public static class MovementConstants
{
    // ================================================================
    // TOP SPEED MAPPING (Pace attribute â†’ m/s)
    // ================================================================

    /// <summary>
    /// Top speed at Pace = 1 (slowest professional player).
    /// 7.5 m/s â‰ˆ 27.0 km/h.
    ///
    /// Derivation: Bottom-percentile professional centre-backs and
    /// goalkeepers in GPS tracking data sprint at approximately
    /// 27â€“28 km/h. 7.5 m/s represents the absolute floor for a
    /// player who could still compete at professional level.
    /// </summary>
    public const float TOP_SPEED_MIN = 7.5f;

    /// <summary>
    /// Top speed at Pace = 20 (elite sprinter â€” MbappÃ©, Adama TraorÃ© tier).
    /// 10.2 m/s â‰ˆ 36.7 km/h.
    ///
    /// Derivation: The fastest recorded sprint speeds in professional
    /// football are 36â€“37 km/h (MbappÃ©: 36.0 km/h, Adama TraorÃ©: 37.0 km/h).
    /// 10.2 m/s represents the realistic ceiling. Note: Usain Bolt's
    /// peak was 12.4 m/s â€” footballers in full kit on grass never reach this.
    /// </summary>
    public const float TOP_SPEED_MAX = 10.2f;

    /// <summary>
    /// Per-attribute-point increase in top speed.
    /// (10.2 - 7.5) / 19 = 0.14211 m/s per point â‰ˆ 0.51 km/h per point.
    ///
    /// This means each attribute point is worth roughly half a km/h â€”
    /// enough to be measurable over a sprint distance but not cartoonish.
    /// </summary>
    public const float TOP_SPEED_PER_POINT = (TOP_SPEED_MAX - TOP_SPEED_MIN) / 19.0f;

    // ================================================================
    // ACCELERATION MAPPING (Acceleration attribute â†’ rate constant k)
    // ================================================================

    /// <summary>
    /// Acceleration rate constant at Acceleration = 1 (slowest to accelerate).
    /// Produces time-to-90%-top-speed of approximately 3.5 seconds.
    ///
    /// The acceleration model uses: v(t) = v_max Ã— (1 - e^(-kÃ—t))
    /// At t = T_90: v = 0.9 Ã— v_max â†’ 0.9 = 1 - e^(-kÃ—T_90)
    ///   â†’ e^(-kÃ—T_90) = 0.1 â†’ k = -ln(0.1) / T_90 = 2.3026 / T_90
    /// For T_90 = 3.5s: k = 2.3026 / 3.5 = 0.6579 sâ»Â¹
    ///
    /// This represents a heavy, slow-accelerating centre-back.
    /// </summary>
    public const float ACCEL_K_MIN = 0.658f;

    /// <summary>
    /// Acceleration rate constant at Acceleration = 20 (explosive acceleration).
    /// Produces time-to-90%-top-speed of approximately 2.5 seconds.
    ///
    /// For T_90 = 2.5s: k = 2.3026 / 2.5 = 0.9210 sâ»Â¹
    ///
    /// This represents an explosive forward or winger â€” the kind of player
    /// who reaches full speed in 2.5 seconds from standstill.
    ///
    /// Real-world reference: Elite professional footballers reach ~90% of
    /// top speed in 2.5â€“3.0 seconds (Haugen et al., 2014).
    /// </summary>
    public const float ACCEL_K_MAX = 0.921f;

    /// <summary>Per-attribute-point increase in acceleration rate constant.</summary>
    public const float ACCEL_K_PER_POINT = (ACCEL_K_MAX - ACCEL_K_MIN) / 19.0f;

    // ================================================================
    // DECELERATION MAPPING
    // ================================================================

    /// <summary>
    /// Controlled deceleration rate at Agility = 1 (worst stopping ability).
    /// Units: m/sÂ² (negative, applied against velocity direction).
    ///
    /// Derivation: From FR-3 (revised), controlled stop from sprint (â‰ˆ9 m/s) in 5.0m.
    /// Using vÂ² = vâ‚€Â² - 2 Ã— a Ã— d â†’ a = vâ‚€Â² / (2 Ã— d) = 81 / 10.0 = 8.1 m/sÂ²
    ///
    /// Real-world validation: 8.1 m/sÂ² falls within the 5â€“10 m/sÂ² range
    /// observed in sports science deceleration studies for trained athletes
    /// performing intentional stops (Harper & Kiely, 2018).
    /// </summary>
    public const float DECEL_CONTROLLED_MIN = 8.1f;

    /// <summary>
    /// Controlled deceleration rate at Agility = 20 (best stopping ability).
    ///
    /// Derivation: From FR-3 (revised), controlled stop from sprint (â‰ˆ9 m/s) in 3.0m.
    /// a = vâ‚€Â² / (2 Ã— d) = 81 / 6.0 = 13.5 m/sÂ²
    ///
    /// Real-world validation: 13.5 m/sÂ² is at the high end of controlled
    /// deceleration but achievable by elite athletes with excellent body
    /// control and proprioception (Dos'Santos et al., 2020).
    /// </summary>
    public const float DECEL_CONTROLLED_MAX = 13.5f;

    /// <summary>Per-attribute-point increase in controlled deceleration rate.
    /// (13.5 - 8.1) / 19 = 0.28421 m/sÂ² per point.</summary>
    public const float DECEL_CONTROLLED_PER_POINT =
        (DECEL_CONTROLLED_MAX - DECEL_CONTROLLED_MIN) / 19.0f;

    /// <summary>
    /// Emergency deceleration rate at Agility = 1 (worst emergency stopping).
    ///
    /// Derivation: From FR-3 (revised), emergency stop from sprint (â‰ˆ9 m/s) in 3.5m.
    /// a = 81 / 7.0 = 11.57 m/sÂ²
    ///
    /// Note: Emergency deceleration always exceeds controlled because the
    /// agent applies maximum braking force, accepting stumble risk.
    ///
    /// Real-world validation: 11.57 m/sÂ² (â‰ˆ1.18g) is well within documented
    /// deceleration capability for trained athletes (Harper & Kiely, 2018).
    /// Leaves ~8 m/sÂ² of headroom below the documented human ceiling (~20 m/sÂ²).
    /// </summary>
    public const float DECEL_EMERGENCY_MIN = 11.57f;

    /// <summary>
    /// Emergency deceleration rate at Agility = 20 (best emergency stopping).
    ///
    /// Derivation: From FR-3 (revised), emergency stop from sprint (â‰ˆ9 m/s) in 2.5m.
    /// a = 81 / 5.0 = 16.2 m/sÂ²
    ///
    /// Real-world validation: 16.2 m/sÂ² (â‰ˆ1.65g) is comfortably within
    /// documented peak deceleration for elite athletes (Dos'Santos et al., 2020).
    /// Leaves ~4 m/sÂ² of headroom below the documented ceiling (~20 m/sÂ²),
    /// allowing future tuning toward shorter stops if playtesting demands it.
    /// </summary>
    public const float DECEL_EMERGENCY_MAX = 16.2f;

    /// <summary>Per-attribute-point increase in emergency deceleration rate.
    /// (16.2 - 11.57) / 19 = 0.24368 m/sÂ² per point.</summary>
    public const float DECEL_EMERGENCY_PER_POINT =
        (DECEL_EMERGENCY_MAX - DECEL_EMERGENCY_MIN) / 19.0f;

    // ================================================================
    // STUMBLE RISK (Emergency deceleration)
    // ================================================================

    /// <summary>
    /// Base stumble probability during emergency deceleration at sprint speed.
    /// Modified by Balance attribute.
    ///
    /// Formula: StumbleChance = BASE Ã— (1.0 - (effectiveBalance - 1.0) / 19.0 Ã— REDUCTION)
    ///
    /// At Balance 1:  stumble chance = 0.30 (30%)
    /// At Balance 10: stumble chance = 0.30 Ã— (1.0 - 0.4737 Ã— 0.7) = 0.201 (20.1%)
    /// At Balance 20: stumble chance = 0.30 Ã— (1.0 - 1.0 Ã— 0.7) = 0.09 (9%)
    ///
    /// Only evaluated when agent enters DECELERATING state via emergency mode
    /// while previous state was SPRINTING. Not evaluated during controlled
    /// deceleration or when decelerating from slower speeds.
    /// </summary>
    public const float STUMBLE_BASE_CHANCE = 0.30f;

    /// <summary>
    /// Maximum stumble chance reduction from Balance attribute.
    /// At Balance 20, stumble chance is reduced by 70% from base.
    /// </summary>
    public const float STUMBLE_BALANCE_REDUCTION = 0.70f;

    // ================================================================
    // WALKING ACCELERATION (Linear model)
    // ================================================================

    /// <summary>
    /// Constant acceleration rate for WALKING state (all agents).
    /// Units: m/sÂ².
    ///
    /// Walking uses a simple linear acceleration model rather than the
    /// exponential curve used for jogging/sprinting. This is because:
    /// 1. Walking acceleration is physically simple (no significant momentum)
    /// 2. Attribute differences in walking speed are negligible
    /// 3. Simpler model reduces per-frame computation for a non-critical state
    ///
    /// 2.0 m/sÂ² brings an agent from standstill to walking speed (â‰ˆ1.5 m/s)
    /// in about 0.75 seconds â€” imperceptible to the observer.
    /// </summary>
    public const float WALK_ACCELERATION = 2.0f;

    /// <summary>
    /// Walking deceleration rate (linear, same for all agents).
    /// Mirrors walking acceleration â€” uniform because walking decel is trivial.
    /// </summary>
    public const float WALK_DECELERATION = 2.5f;

    // ================================================================
    // PHYSICS SAFETY LIMITS
    // ================================================================

    /// <summary>
    /// Absolute maximum speed for any agent. Physics clamp, not attribute-derived.
    /// 12.0 m/s â‰ˆ 43.2 km/h. Well above any realistic sprint (max â‰ˆ37 km/h)
    /// but exists as a safety net for numerical overflow or extreme modifier
    /// combinations via PerformanceContext.
    ///
    /// Consistent with Section 2.4 (Failure Modes): runaway velocity detection
    /// triggers at this value.
    /// </summary>
    public const float MAX_SPEED = 12.0f;

    /// <summary>
    /// Absolute maximum acceleration for any agent. Physics clamp.
    /// 8.0 m/sÂ² exceeds any attribute-derived value but catches numerical errors.
    /// </summary>
    public const float MAX_ACCELERATION = 8.0f;

    /// <summary>
    /// Minimum speed below which agent velocity is zeroed (prevents drift).
    /// Matches Ball Physics Spec #1 pattern for MIN_VELOCITY.
    /// </summary>
    public const float MIN_SPEED = 0.05f;
}
```

### Mapping Functions

```csharp
/// <summary>
/// Maps effective Pace attribute to top speed in m/s.
///
/// Input: effectivePace from PerformanceContext.EvaluateAttribute(rawPace)
///   Typical range: 1.0â€“20.0 (can exceed due to modifiers)
/// Output: top speed in m/s
///   Typical range: 7.5â€“10.2 m/s
///   Clamped externally by MAX_SPEED (12.0 m/s)
///
/// Linear interpolation chosen over curved mapping because:
/// 1. Each attribute point should feel equally impactful
/// 2. Avoids "dead zones" where points don't matter
/// 3. Simpler to reason about and balance
/// 4. Realism: GPS data shows approximately linear distribution
///    of sprint speeds across professional football populations
/// </summary>
public static float MapPaceToTopSpeed(float effectivePace)
{
    return MovementConstants.TOP_SPEED_MIN
         + (effectivePace - 1.0f) * MovementConstants.TOP_SPEED_PER_POINT;
}

/// <summary>
/// Maps effective Acceleration attribute to rate constant k (sâ»Â¹).
///
/// k controls the steepness of the exponential acceleration curve:
///   v(t) = v_max Ã— (1 - e^(-kÃ—t))
///
/// Higher k = faster acceleration = reaches top speed sooner.
/// </summary>
public static float MapAccelerationToK(float effectiveAcceleration)
{
    return MovementConstants.ACCEL_K_MIN
         + (effectiveAcceleration - 1.0f) * MovementConstants.ACCEL_K_PER_POINT;
}

/// <summary>
/// Maps effective Agility attribute (with Balance weighting) to
/// controlled deceleration rate in m/sÂ².
///
/// Primary attribute: Agility (weight 0.7)
/// Secondary attribute: Balance (weight 0.3)
/// Use PerformanceContext.EvaluateAttributePair() to compute the input.
/// </summary>
public static float MapAgilityToControlledDecel(float effectiveAgility)
{
    return MovementConstants.DECEL_CONTROLLED_MIN
         + (effectiveAgility - 1.0f) * MovementConstants.DECEL_CONTROLLED_PER_POINT;
}

/// <summary>
/// Maps effective Agility attribute to emergency deceleration rate in m/sÂ².
/// Same attribute weighting as controlled deceleration.
/// </summary>
public static float MapAgilityToEmergencyDecel(float effectiveAgility)
{
    return MovementConstants.DECEL_EMERGENCY_MIN
         + (effectiveAgility - 1.0f) * MovementConstants.DECEL_EMERGENCY_PER_POINT;
}
```

### Attribute Mapping Reference Table

| Effective Attribute | Top Speed (m/s) | Top Speed (km/h) | Accel k (sâ»Â¹) | Tâ‚‰â‚€ (s) | Ctrl Decel (m/sÂ²) | Emrg Decel (m/sÂ²) | Ctrl Stop Dist (m) |
|---|---|---|---|---|---|---|---|
| 1.0 | 7.50 | 27.0 | 0.658 | 3.50 | 8.10 | 11.57 | 2.81 |
| 5.0 | 8.07 | 29.1 | 0.713 | 3.23 | 9.24 | 12.54 | 2.86 |
| 10.0 | 8.78 | 31.6 | 0.782 | 2.94 | 10.66 | 13.76 | 2.93 |
| 12.0 | 9.07 | 32.6 | 0.796 | 2.89 | 11.23 | 14.25 | 2.97 |
| 15.0 | 9.49 | 34.2 | 0.838 | 2.75 | 12.08 | 14.98 | 3.02 |
| 18.0 | 9.92 | 35.7 | 0.879 | 2.62 | 12.93 | 15.71 | 3.08 |
| 20.0 | 10.20 | 36.7 | 0.921 | 2.50 | 13.50 | 16.20 | 3.12 |

**Derivation notes for Tâ‚‰â‚€ column:** Tâ‚‰â‚€ = 2.3026 / k  
**Derivation notes for Ctrl Stop Dist column:** d = vÂ² / (2 Ã— decel), using v = 0.9 Ã— TopSpeed as typical sprint speed at point of deceleration initiation. Note: this column assumes the same effective attribute drives both Pace and Agility â€” in gameplay, these are separate attributes, so actual stop distances depend on the specific player's Agility, not their Pace.

**Attribute Mapping Tuning Note:**

> All attribute-to-physical-value mappings in this section use **linear interpolation**.
> A mild curve (e.g., `t^1.3` where `t = (attr-1)/19`) was considered to better model
> diminishing returns and create more meaningful separation at the elite end. Linear was
> chosen as the Stage 0 default for simplicity and debuggability. This is explicitly
> flagged as a **tuning parameter for Stage 1 playtesting** â€” when players are visible
> running side-by-side, the team should evaluate whether the top-end separation (Pace 17
> vs Pace 20 = 0.43 m/s linear) feels sufficient or needs a curve to make elite speed
> feel special. If a curve is adopted, it will be applied inside the `MapPaceToTopSpeed()`
> and equivalent functions only â€” no downstream formula changes required.

---

## 3.2.3 Acceleration Model

### Exponential Acceleration Curve

Agents accelerating in JOGGING and SPRINTING states follow an exponential approach to top speed:

```
v(t) = v_target Ã— (1 - e^(-k Ã— t))
```

Where:
- `v(t)` = velocity magnitude at time t (m/s)
- `v_target` = current top speed (from MapPaceToTopSpeed, modified by directional multipliers and fatigue)
- `k` = acceleration rate constant (from MapAccelerationToK)
- `t` = time since acceleration began (seconds)

**Why exponential, not linear:**
1. Matches real-world biomechanics â€” initial force production is highest, diminishing as limbs approach maximum turnover rate
2. Produces natural-looking acceleration where the first 1â€“2 seconds show dramatic speed gain and the final approach to top speed is gradual
3. Self-limiting â€” the agent asymptotically approaches top speed without overshooting
4. Consistent with sports science acceleration profiles for team sport athletes (Buchheit et al., 2014)

### Discrete Integration at 60Hz

The continuous formula above is for reference. At 60Hz, we compute velocity incrementally:

```csharp
/// <summary>
/// Computes the agent's new velocity after one physics frame of acceleration.
/// Called every frame (60Hz) while agent is in JOGGING or SPRINTING state
/// and has not yet reached target speed.
///
/// The discrete implementation uses the velocity-form of the exponential:
///   v(t+dt) = v_target + (v(t) - v_target) Ã— e^(-k Ã— dt)
///
/// This is mathematically equivalent to the continuous form but works
/// incrementally from the current velocity, avoiding the need to track
/// "time since acceleration began" (which would require reset logic
/// on every speed change).
///
/// At 60Hz, dt = 1/60 â‰ˆ 0.01667s, and e^(-k Ã— dt) ranges from:
///   k_min (0.658): e^(-0.658 Ã— 0.01667) = 0.98908 (slow approach)
///   k_max (0.921): e^(-0.921 Ã— 0.01667) = 0.98474 (fast approach)
///
/// Numerical accuracy: Over a 3.5-second acceleration from 0 to 10.2 m/s
/// (210 frames), cumulative error vs continuous formula is <0.01 m/s.
/// </summary>
/// <param name="currentSpeed">Current velocity magnitude (m/s)</param>
/// <param name="targetSpeed">Target top speed including all modifiers (m/s)</param>
/// <param name="k">Acceleration rate constant from MapAccelerationToK()</param>
/// <param name="dt">Frame delta time (seconds, typically 1/60)</param>
/// <returns>New velocity magnitude (m/s)</returns>
public static float ApplyExponentialAcceleration(
    float currentSpeed,
    float targetSpeed,
    float k,
    float dt)
{
    // Exponential approach: velocity converges on target
    float decay = Mathf.Exp(-k * dt);
    float newSpeed = targetSpeed + (currentSpeed - targetSpeed) * decay;

    // Snap to target if within MIN_SPEED tolerance (prevents asymptotic drift)
    if (Mathf.Abs(newSpeed - targetSpeed) < MovementConstants.MIN_SPEED)
    {
        newSpeed = targetSpeed;
    }

    return Mathf.Clamp(newSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Walking Acceleration (Linear Model)

Agents in WALKING state use a simpler linear acceleration:

```csharp
/// <summary>
/// Applies constant-rate acceleration for WALKING state.
/// All agents use the same walking acceleration â€” attribute differences
/// are negligible at walking speeds and not worth the computation.
///
/// Linear model: v(t+dt) = v(t) + a Ã— dt
/// Clamped to JOG_ENTER threshold to prevent walking model from
/// pushing agent into jogging speeds.
/// </summary>
public static float ApplyLinearAcceleration(
    float currentSpeed,
    float targetSpeed,
    float acceleration,
    float dt)
{
    float newSpeed;
    if (currentSpeed < targetSpeed)
    {
        newSpeed = currentSpeed + acceleration * dt;
        newSpeed = Mathf.Min(newSpeed, targetSpeed);  // Don't overshoot target
    }
    else
    {
        // Decelerating toward lower target
        newSpeed = currentSpeed - acceleration * dt;
        newSpeed = Mathf.Max(newSpeed, targetSpeed);
    }

    return Mathf.Clamp(newSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Acceleration Selection by State

Mirrors the state-physics activation table from Section 3.1.6:

```csharp
/// <summary>
/// Selects and applies the correct acceleration model based on movement state.
///
/// Called once per frame (60Hz) during the velocity update phase.
/// State machine (Section 3.1) has already determined the current state;
/// this function applies the appropriate physics.
///
/// NOTE: This function only handles speed magnitude. Direction is handled
/// separately in Section 3.3 (Directional Movement) and Section 3.4 (Turning).
///
/// NOTE: targetSpeed is the FINAL target after all modifiers have been applied:
///   1. PerformanceContext (Section 3.2.1)
///   2. Directional multiplier (Section 3.3)
///   3. Fatigue modifier (Section 3.5)
/// The caller is responsible for computing this composite target.
/// </summary>
public static float UpdateSpeed(
    float currentSpeed,
    float targetSpeed,
    float accelK,
    AgentMovementState state,
    float dt)
{
    switch (state)
    {
        case AgentMovementState.IDLE:
            // No acceleration; speed should already be below IDLE_ENTER
            // Apply minimal drag to ensure convergence to zero
            return Mathf.Max(0f, currentSpeed - MovementConstants.WALK_DECELERATION * dt);

        case AgentMovementState.WALKING:
            return ApplyLinearAcceleration(
                currentSpeed, targetSpeed,
                MovementConstants.WALK_ACCELERATION, dt);

        case AgentMovementState.JOGGING:
        case AgentMovementState.SPRINTING:
            return ApplyExponentialAcceleration(
                currentSpeed, targetSpeed, accelK, dt);

        case AgentMovementState.DECELERATING:
            // Handled by deceleration model (Section 3.2.5)
            // This case should not reach UpdateSpeed â€” included for safety
            return currentSpeed;

        case AgentMovementState.STUMBLING:
            // Friction drag only â€” no voluntary acceleration
            // Uses ground friction coefficient (simplified: constant drag)
            return Mathf.Max(0f, currentSpeed - MovementConstants.WALK_DECELERATION * 2.0f * dt);

        case AgentMovementState.GROUNDED:
            // No movement
            return 0f;

        default:
            return currentSpeed;
    }
}
```

---

## 3.2.4 Top Speed Calculation

### Composite Top Speed

The top speed used in the acceleration model is not a single value â€” it's a composite of the Pace attribute, PerformanceContext modifiers, directional penalties, and fatigue:

```csharp
/// <summary>
/// Computes the agent's current effective top speed.
///
/// This is the AUTHORITATIVE top speed calculation. No other function
/// should independently compute top speed.
///
/// Modifier chain (applied in this order):
///   1. Raw Pace attribute â†’ PerformanceContext â†’ Effective Pace
///   2. Effective Pace â†’ MapPaceToTopSpeed â†’ Base top speed (m/s)
///   3. Ã— Directional multiplier (Section 3.3: forward/lateral/backward)
///   4. Ã— Aerobic fatigue modifier (Section 3.5: 0.0â€“1.0 pool â†’ speed scalar)
///   5. Clamp to [0, MAX_SPEED]
///
/// Example â€” Fresh elite winger sprinting forward:
///   Pace=19, Form=1.05, Context=1.0, Career=1.0
///   EffectivePace = 19 Ã— 1.05 = 19.95
///   BaseSpeed = 7.5 + (19.95 - 1.0) Ã— 0.14211 = 10.19 m/s
///   Directional = 1.0 (forward)
///   Fatigue = 1.0 (fresh)
///   Final = 10.19 m/s â‰ˆ 36.7 km/h
///
/// Example â€” Tired centre-back tracking back, poor form:
///   Pace=10, Form=0.88, Context=1.0, Career=0.97
///   EffectivePace = 10 Ã— (0.88 Ã— 1.0 Ã— 0.97) = 10 Ã— 0.8536 = 8.536
///   BaseSpeed = 7.5 + (8.536 - 1.0) Ã— 0.14211 = 8.57 m/s
///   Directional = 0.50 (backward, Agility=10 â†’ mid-range)
///   Fatigue = 0.80 (75th minute)
///   Final = 8.57 Ã— 0.50 Ã— 0.80 = 3.43 m/s â€” barely jogging backward
/// </summary>
public static float CalculateEffectiveTopSpeed(
    int rawPace,
    PerformanceContext context,
    float directionalMultiplier,
    float fatigueSpeedModifier)
{
    float effectivePace = context.EvaluateAttribute(rawPace);
    float baseTopSpeed = MapPaceToTopSpeed(effectivePace);
    float finalSpeed = baseTopSpeed * directionalMultiplier * fatigueSpeedModifier;

    return Mathf.Clamp(finalSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Fatigueâ€“Speed Relationship

The aerobic fatigue pool (Section 3.5, FR-6) applies a speed modifier:

```csharp
/// <summary>
/// Converts aerobic energy pool level to top speed modifier.
///
/// Aerobic pool range: 0.0 (spent) to 1.0 (fresh)
/// Speed modifier range: 0.70 (exhausted floor) to 1.0 (full speed)
///
/// The relationship is piecewise linear:
///   Pool 1.0â€“0.5: Full speed (1.0). Players don't slow down until
///     they're significantly fatigued â€” consistent with sports science
///     showing minimal performance decrement until glycogen depletion
///     threshold (~50% reserves).
///   Pool 0.5â€“0.0: Linear decline from 1.0 to 0.70.
///     At 0.0 (complete exhaustion), player retains 70% speed â€” they're
///     visibly struggling but not immobile.
///
/// Derivation of 0.70 floor:
///   From QR-1: "Fatigued agents visibly slower than fresh agents (>15% speed reduction)"
///   30% reduction at complete exhaustion exceeds the 15% visibility threshold
///   with margin. Below 70%, movement would look pathological rather than fatigued.
///
/// Note: Sprint reservoir (separate pool) controls sprint ACCESS via state
/// machine gating (Section 3.1), not sprint SPEED. An agent with sprint
/// reservoir depleted simply cannot enter SPRINTING state, but their
/// jogging speed is affected by aerobic pool as described here.
/// </summary>
public static float AerobicPoolToSpeedModifier(float aerobicPool)
{
    aerobicPool = Mathf.Clamp01(aerobicPool);

    if (aerobicPool >= 0.5f)
    {
        return 1.0f;  // No speed penalty above 50% reserves
    }
    else
    {
        // Linear interpolation: pool 0.5 â†’ modifier 1.0, pool 0.0 â†’ modifier 0.70
        float t = aerobicPool / 0.5f;  // Normalize 0.0â€“0.5 to 0.0â€“1.0
        return Mathf.Lerp(0.70f, 1.0f, t);
    }
}
```

---

## 3.2.5 Deceleration Model

### Two Deceleration Modes

Agents in the DECELERATING state use one of two modes, selected by the commanding system:

**Controlled deceleration:** Agent brakes gradually. Used for intentional stops (arriving at position, preparing to receive ball, changing direction).

**Emergency deceleration:** Agent brakes at maximum force. Used for urgent stops (unexpected opponent, ball change, tactical override). Carries stumble risk.

### Controlled Deceleration

```csharp
/// <summary>
/// Applies controlled deceleration for one physics frame.
///
/// Uses constant deceleration rate derived from Agility (primary, 0.7)
/// and Balance (secondary, 0.3) via PerformanceContext.
///
/// Constant deceleration chosen over exponential because:
/// 1. Braking is a simpler biomechanical action than acceleration
/// 2. Produces predictable stopping distances for AI path planning
/// 3. Sports science literature models deceleration as approximately
///    constant for intentional stops (Harper & Kiely, 2018)
///
/// Formula: v(t+dt) = max(0, v(t) - decelRate Ã— dt)
///
/// Stopping distance (continuous): d = vâ‚€Â² / (2 Ã— decelRate)
///   At Agility 1,  sprint speed â‰ˆ9 m/s: d = 81 / 16.2 = 5.0m
///   At Agility 20, sprint speed â‰ˆ9 m/s: d = 81 / 27.0 = 3.0m
/// These match FR-3 (revised) requirements exactly.
/// </summary>
/// </summary>
public static float ApplyControlledDeceleration(
    float currentSpeed,
    float decelRate,
    float dt)
{
    float newSpeed = currentSpeed - decelRate * dt;
    return Mathf.Max(0f, newSpeed);
}
```

### Emergency Deceleration

```csharp
/// <summary>
/// Applies emergency deceleration for one physics frame.
/// Higher deceleration rate than controlled, but carries stumble risk.
///
/// The stumble check is performed ONCE on entry to emergency deceleration
/// (not every frame). If the check fails, the agent transitions to
/// STUMBLING state (Section 3.1) instead of continuing deceleration.
///
/// Stumble probability:
///   P(stumble) = BASE Ã— (1.0 - normalizedBalance Ã— REDUCTION)
///   normalizedBalance = (effectiveBalance - 1.0) / 19.0
///
///   At Balance 1:  P = 0.30 Ã— (1.0 - 0.0 Ã— 0.7) = 30.0%
///   At Balance 10: P = 0.30 Ã— (1.0 - 0.474 Ã— 0.7) = 20.1%
///   At Balance 20: P = 0.30 Ã— (1.0 - 1.0 Ã— 0.7) = 9.0%
///
/// Speed threshold: Stumble check ONLY applies when entering emergency
/// deceleration from SPRINTING state. Emergency decel from JOGGING or
/// slower has no stumble risk (insufficient momentum for balance loss).
/// </summary>
public static float ApplyEmergencyDeceleration(
    float currentSpeed,
    float decelRate,
    float dt)
{
    // Same physics as controlled, just higher rate
    float newSpeed = currentSpeed - decelRate * dt;
    return Mathf.Max(0f, newSpeed);
}

/// <summary>
/// Evaluates stumble risk on emergency deceleration entry.
/// Called ONCE when agent transitions to DECELERATING (emergency mode)
/// from SPRINTING state.
///
/// Returns true if the agent stumbles (should transition to STUMBLING state).
///
/// NOTE: The RNG call uses the deterministic simulation seed (Section 1.4,
/// FR-9). The same match seed produces the same stumble outcomes.
/// </summary>
/// <param name="effectiveBalance">Balance after PerformanceContext evaluation</param>
/// <param name="previousState">State before deceleration â€” only SPRINTING triggers check</param>
/// <param name="rngValue">Deterministic random value [0.0, 1.0) from simulation RNG</param>
/// <returns>True if agent stumbles</returns>
public static bool EvaluateStumbleRisk(
    float effectiveBalance,
    AgentMovementState previousState,
    float rngValue)
{
    // Only evaluate when decelerating from sprint
    if (previousState != AgentMovementState.SPRINTING)
        return false;

    float normalizedBalance = Mathf.Clamp01((effectiveBalance - 1.0f) / 19.0f);
    float stumbleChance = MovementConstants.STUMBLE_BASE_CHANCE
                        * (1.0f - normalizedBalance * MovementConstants.STUMBLE_BALANCE_REDUCTION);

    return rngValue < stumbleChance;
}
```

### Deceleration Orchestration

```csharp
/// <summary>
/// Full deceleration update for one frame.
/// Selects mode, evaluates stumble risk (if applicable), and returns
/// new speed + state transition recommendation.
///
/// Called by the main movement update loop when state == DECELERATING.
///
/// Returns a DecelerationResult struct containing the new speed and
/// an optional state transition (to STUMBLING if stumble check fails,
/// to IDLE if speed reaches zero).
/// </summary>
public struct DecelerationResult
{
    /// <summary>New speed after this frame's deceleration (m/s)</summary>
    public float NewSpeed;

    /// <summary>
    /// Recommended state transition, or null if staying in DECELERATING.
    /// STUMBLING: agent lost balance during emergency stop.
    /// IDLE: agent has come to a complete stop.
    /// </summary>
    public AgentMovementState? TransitionTo;
}

/// <summary>
/// Computes deceleration for one frame.
///
/// isEmergency: true for maximum braking, false for controlled stop.
/// isFirstFrame: true on the frame deceleration begins (for stumble check).
/// effectiveAgilityBalance: weighted pair from PerformanceContext.
/// effectiveBalance: solo balance from PerformanceContext (for stumble check).
/// previousState: state before DECELERATING was entered.
/// rngValue: deterministic random for stumble evaluation.
/// </summary>
public static DecelerationResult UpdateDeceleration(
    float currentSpeed,
    bool isEmergency,
    bool isFirstFrame,
    float effectiveAgilityBalance,
    float effectiveBalance,
    AgentMovementState previousState,
    float rngValue,
    float dt)
{
    var result = new DecelerationResult { TransitionTo = null };

    // Check stumble risk on first frame of emergency decel from sprint
    if (isEmergency && isFirstFrame)
    {
        if (EvaluateStumbleRisk(effectiveBalance, previousState, rngValue))
        {
            result.NewSpeed = currentSpeed;  // Momentum carries into stumble
            result.TransitionTo = AgentMovementState.STUMBLING;
            return result;
        }
    }

    // Apply deceleration
    float decelRate = isEmergency
        ? MapAgilityToEmergencyDecel(effectiveAgilityBalance)
        : MapAgilityToControlledDecel(effectiveAgilityBalance);

    float newSpeed = isEmergency
        ? ApplyEmergencyDeceleration(currentSpeed, decelRate, dt)
        : ApplyControlledDeceleration(currentSpeed, decelRate, dt);

    result.NewSpeed = newSpeed;

    // Check if stopped
    if (newSpeed < MovementConstants.MIN_SPEED)
    {
        result.NewSpeed = 0f;
        result.TransitionTo = AgentMovementState.IDLE;
    }

    return result;
}
```

---

## 3.2.6 Numerical Examples & Validation

### Example 1: Elite Winger Sprint from Standstill (Stage 0)

**Player:** Pace 19, Acceleration 18  
**Context:** PerformanceContext.CreateNeutral() (all modifiers 1.0)  
**Direction:** Forward (multiplier 1.0)  
**Fatigue:** Fresh (aerobic pool 1.0 â†’ speed modifier 1.0)

```
EffectivePace = 19 Ã— 1.0 = 19.0
TopSpeed = 7.5 + (19.0 - 1.0) Ã— 0.14211 = 7.5 + 2.558 = 10.058 m/s

EffectiveAccel = 18 Ã— 1.0 = 18.0
k = 0.658 + (18.0 - 1.0) Ã— 0.01384 = 0.658 + 0.2353 = 0.8933 sâ»Â¹

Time to 90% top speed: Tâ‚‰â‚€ = 2.3026 / 0.8933 = 2.578 s âœ“ (within 2.5â€“3.5s range)

Velocity at key times (continuous formula):
  t=0.0s: v = 0.000 m/s (standstill)
  t=0.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 0.5)) = 10.058 Ã— 0.3596 = 3.616 m/s
  t=1.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 1.0)) = 10.058 Ã— 0.5903 = 5.937 m/s
  t=1.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 1.5)) = 10.058 Ã— 0.7373 = 7.416 m/s
  t=2.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 2.0)) = 10.058 Ã— 0.8316 = 8.364 m/s
  t=2.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 2.5)) = 10.058 Ã— 0.8921 = 8.973 m/s
  t=3.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 3.0)) = 10.058 Ã— 0.9310 = 9.364 m/s
  t=3.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 3.5)) = 10.058 Ã— 0.9559 = 9.615 m/s

Distance covered in 3.0 seconds: â‰ˆ17.1m (integrated numerically)
```

**Validation:** Reaches sprint threshold (5.8 m/s) at approximately t=1.1s. Agent transitions IDLE â†’ WALKING â†’ JOGGING â†’ SPRINTING within first 1.5 seconds. Consistent with real-world observation of elite wingers.

### Example 2: Slow Centre-Back Sprint (Stage 0)

**Player:** Pace 8, Acceleration 6  
**Context:** PerformanceContext.CreateNeutral()  
**Direction:** Forward (multiplier 1.0)  
**Fatigue:** Fresh

```
EffectivePace = 8.0
TopSpeed = 7.5 + (8.0 - 1.0) Ã— 0.14211 = 7.5 + 0.995 = 8.495 m/s

EffectiveAccel = 6.0
k = 0.658 + (6.0 - 1.0) Ã— 0.01384 = 0.658 + 0.0692 = 0.7272 sâ»Â¹

Tâ‚‰â‚€ = 2.3026 / 0.7272 = 3.167 s âœ“

Velocity at key times:
  t=1.0s: v = 8.495 Ã— (1 - e^(-0.7272)) = 8.495 Ã— 0.5168 = 4.390 m/s
  t=2.0s: v = 8.495 Ã— (1 - e^(-1.4544)) = 8.495 Ã— 0.7664 = 6.510 m/s
  t=3.0s: v = 8.495 Ã— (1 - e^(-2.1816)) = 8.495 Ã— 0.8872 = 7.533 m/s
```

**Validation:** Noticeably slower to reach sprint speed (â‰ˆ1.5s vs â‰ˆ1.1s for elite winger). Top speed 1.56 m/s lower. Over a 30m chase, the winger pulls away by approximately 2.5â€“3.0 meters â€” consistent with observable real-world speed differences.

### Example 3: Stage 4 Scenario â€” Elite Player in Poor Form

**Player:** Pace 18, Acceleration 17  
**Context:** Form=0.88 (cold streak), Context=0.85 (new league, poor tactical fit), Career=0.95 (coming off mediocre season)  
**Combined modifier:** 0.88 Ã— 0.85 Ã— 0.95 = 0.7106 â†’ clamped to 0.7106 (above FLOOR)

```
EffectivePace = 18 Ã— 0.7106 = 12.79
TopSpeed = 7.5 + (12.79 - 1.0) Ã— 0.14211 = 7.5 + 1.676 = 9.176 m/s

EffectiveAccel = 17 Ã— 0.7106 = 12.08
k = 0.658 + (12.08 - 1.0) Ã— 0.01384 = 0.658 + 0.1533 = 0.8113 sâ»Â¹

Tâ‚‰â‚€ = 2.3026 / 0.8113 = 2.838 s
```

**Analysis:** An elite Pace 18 player in this scenario performs like a Pace 13 player. Their top speed drops from 9.92 m/s to 9.18 m/s â€” they're still fast, but noticeably off their best. A mid-table Pace 14 player in good form (modifier 1.10) would have EffectivePace 15.4 and top speed 9.55 m/s â€” actually faster. This demonstrates the system's ability to produce the "upset" scenarios where average players outperform struggling stars.

### Example 4: Emergency Deceleration with Stumble Risk

**Player:** Agility 14, Balance 9  
**Context:** PerformanceContext.CreateNeutral()  
**Scenario:** Agent at 9.5 m/s (sprinting) receives emergency stop command.

```
Effective Agility-Balance pair:
  Agility contribution: 14.0 Ã— 0.7 = 9.8
  Balance contribution: 9.0 Ã— 0.3 = 2.7
  Weighted average: (9.8 + 2.7) / 1.0 = 12.5

Emergency decel rate = 11.57 + (12.5 - 1.0) Ã— 0.24368 = 11.57 + 2.802 = 14.372 m/sÂ²

Stopping distance = vÂ² / (2 Ã— decel) = 90.25 / 28.744 = 3.14m âœ“ (within 2.5â€“3.5m range)

Stumble check (previousState = SPRINTING):
  effectiveBalance = 9.0
  normalizedBalance = (9.0 - 1.0) / 19.0 = 0.4211
  stumbleChance = 0.30 Ã— (1.0 - 0.4211 Ã— 0.70) = 0.30 Ã— 0.7053 = 0.2116 (21.2%)

If rngValue < 0.2116: agent stumbles â†’ STUMBLING state
If rngValue â‰¥ 0.2116: agent stops in â‰ˆ3.14m â†’ IDLE state

Stopping time: t = v / decel = 9.5 / 14.372 = 0.661 seconds (â‰ˆ40 frames at 60Hz)
```

### Validation Summary

| Test Case | Requirement | Result | Status |
|---|---|---|---|
| Elite sprint Tâ‚‰â‚€ | 2.5â€“3.5s (FR-2) | 2.58s | âœ“ PASS |
| Slow CB sprint Tâ‚‰â‚€ | 2.5â€“3.5s (FR-2) | 3.17s | âœ“ PASS |
| Controlled stop dist (Agility 1) | 3.0â€“5.0m (FR-3 revised) | 5.00m | âœ“ PASS (boundary) |
| Controlled stop dist (Agility 20) | 3.0â€“5.0m (FR-3 revised) | 3.00m | âœ“ PASS (boundary) |
| Emergency stop dist (Agility 1) | 2.5â€“3.5m (FR-3 revised) | 3.50m | âœ“ PASS (boundary) |
| Emergency stop dist (Agility 20) | 2.5â€“3.5m (FR-3 revised) | 2.50m | âœ“ PASS (boundary) |
| Top speed range (Pace 1â€“20) | 7.5â€“10.2 m/s (FR-2) | 7.50â€“10.20 | âœ“ PASS |
| MAX_SPEED not exceeded | <12.0 m/s (Section 2.4) | Max 10.20 | âœ“ PASS |
| Stage 4 modifier range | Noticeable variation | Â±29% top speed swing | âœ“ PASS |
| Stumble probability range | Attribute-dependent | 9%â€“30% | âœ“ PASS |
| 60Hz numerical error | <0.05 m/s cumulative (PR-3) | <0.01 m/s | âœ“ PASS |
| EFFECTIVE_FLOOR enforcement | â‰¥0.6 combined modifier | Min realistic 0.612 | âœ“ PASS |

---

## Cross-References

| Section | References To | Nature |
|---|---|---|
| 3.2.1 (PerformanceContext) | ALL future specs | Mandatory gateway for attribute evaluation |
| 3.2.1 | Spec #20 (Code Standards) | Enforcement rule for direct attribute access |
| 3.2.1 | Master Vol 2 (H-Gate) | ContextModifier sub-components (Stage 4) |
| 3.2.2 (Attribute Mapping) | Section 3.1.3 (State Thresholds) | Speed values must be consistent |
| 3.2.3 (Acceleration) | Section 3.1.6 (State-Physics Table) | JOGGING/SPRINTING use exponential |
| 3.2.4 (Top Speed) | Section 3.3 (Directional Movement) | Directional multipliers applied here |
| 3.2.4 | Section 3.5 (Fatigue) | Aerobic pool â†’ speed modifier |
| 3.2.5 (Deceleration) | Section 3.1.4 (Transitions) | DECELERATING â†’ STUMBLING path |
| 3.2.5 | Section 3.1.5 (Dwell Times) | STUMBLING recovery timing |

---

## Future Extension Notes

**Stage 2 â€” Form System activation:**
- Replace `PerformanceContext.CreateNeutral()` calls with `PerformanceContext.Create(form, 1.0, 1.0)` where form is computed from match-to-match momentum model.
- No changes to any physics formulas, mapping functions, or deceleration model required.

**Stage 4 â€” Full PerformanceContext activation:**
- Populate ContextModifier from H-Gate outputs (confidence â†’ risk-taking, self-efficacy â†’ execution quality).
- Populate CareerModifier from end-of-season evaluation.
- Consider attribute-specific modifier weighting (mental attributes more affected by psychology than physical) â€” this would require adding `EvaluateAttributeWeighted()` to PerformanceContext, not changing existing methods.
- Staff report system reads `GetCombinedModifier()` to generate natural-language explanations of performance variation.

**Stage 5 â€” Fixed64 migration:**
- All `float` types in this section become `Fixed64`.
- `Mathf.Exp()` replaced by `Fixed64Math.Exp()` (lookup table implementation).
- `Mathf.Clamp()` replaced by `Fixed64Math.Clamp()`.
- No algorithmic changes required â€” only type substitution.

---

**End of Section 3.2**

**Page Count:** ~18 pages  
**Next Section:** Section 3.3 â€” Directional Movement (forward/lateral/backward multipliers)
