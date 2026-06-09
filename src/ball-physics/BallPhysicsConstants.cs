// File:     src/ball-physics/BallPhysicsConstants.cs
// Created:  2026-05-24
// Modified: 2026-06-09 (AR-7 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  All tunable and physical constants for ball physics simulation.
//           No magic numbers anywhere else in the ball-physics assembly.

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Physical constants for ball physics simulation.
    /// All values in SI units (metres, kilograms, seconds).
    /// Tags: [GT]=Gameplay-Tuned, [EST]=Estimated, [FIXED]=Physical law, [DERIVED]=Computed.
    /// LAYOUT NOTE: this catalogue predates the src/CLAUDE.md per-tag #region convention
    /// and groups constants by physical domain (nested classes) with per-constant tags
    /// instead. The nested-class names are the public API (e.g. Ball.MASS is consumed
    /// cross-assembly, Possession.ControlHeight is mirrored by FirstTouchConstants), so
    /// a tag-region restructure would be a breaking rename across consumers — the
    /// domain-class layout is retained as a documented deviation (AR-7 L-2).
    /// </summary>
    public static class BallPhysicsConstants
    {
        public static class Ball
        {
            /// <summary>[FIXED] Ball mass in kg (FIFA: 410–450 g, midpoint). Ball Physics #1 §3.1.2.</summary>
            public const float MASS = 0.43f;

            /// <summary>[FIXED] Ball radius in metres (FIFA: 68–70 cm circumference). Ball Physics #1 §3.1.2.</summary>
            public const float RADIUS = 0.11f;

            /// <summary>[DERIVED] Ball diameter in metres. Formula: 2 × RADIUS. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Diameter = 2f * RADIUS;

            /// <summary>[DERIVED] Cross-sectional area in m² (πr²). Formula: π × RADIUS². Ball Physics #1 §3.1.2.</summary>
            public static readonly float CrossSectionArea = UnityEngine.Mathf.PI * RADIUS * RADIUS;

            /// <summary>
            /// [DERIVED] Moment of inertia in kg·m² (hollow-sphere model). Formula: (2/3) × MASS × RADIUS².
            /// Source constants: Ball.MASS, Ball.RADIUS. Ball Physics #1 §3.1.2.
            /// Model caveat: a real football differs ~10–20% from the ideal hollow sphere due to
            /// internal structure; validating the hollow-sphere model against measured data is a
            /// Stage 1 task. (Retagged [EST] → [DERIVED] in AR-7 L-2 — the value is a documented
            /// formula over [FIXED] inputs, not an independent estimate; FR-CS-021.)
            /// </summary>
            public static readonly float MomentOfInertia = (2f / 3f) * MASS * RADIUS * RADIUS;
        }

        public static class Environment
        {
            /// <summary>[FIXED] Air density at sea level in kg/m³. Ball Physics #1 §3.1.2.</summary>
            public const float AIR_DENSITY = 1.225f;

            /// <summary>[FIXED] Gravitational acceleration in m/s². Ball Physics #1 §3.1.2.</summary>
            public const float GRAVITY = 9.81f;

            /// <summary>[FIXED] Dynamic viscosity of air in Pa·s. Ball Physics #1 §3.1.2.</summary>
            public const float AIR_VISCOSITY = 1.81e-5f;
        }

        public static class Drag
        {
            /// <summary>[GT] Drag coefficient for laminar flow (Re &lt; 200,000). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CoefficientLaminar = 0.20f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Drag coefficient in turbulent flow (Re &gt; 400,000). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CoefficientTurbulent = 0.10f; // TODO: replace with config loader (Stage 1)

            /// <summary>[EST] Speed at which drag crisis begins (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CrisisSpeedLow = 20.0f;

            /// <summary>[EST] Speed at which drag crisis ends (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CrisisSpeedHigh = 25.0f;
        }

        public static class Magnus
        {
            /// <summary>[GT] Base lift coefficient. Ball Physics #1 §3.1.2.</summary>
            public static readonly float LiftCoefficientBase = 0.1f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Lift coefficient scaling factor. Ball Physics #1 §3.1.2.</summary>
            public static readonly float LiftCoefficientScale = 0.4f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Minimum spin parameter for valid calculation. Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinSpinParameter = 0.01f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Maximum spin parameter (clamped). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MaxSpinParameter = 1.0f; // TODO: replace with config loader (Stage 1)

            /// <summary>
            /// [GT] Squared-magnitude threshold below which the ω̂ × v̂ cross product is
            /// treated as degenerate (spin near-parallel to velocity) and Magnus force is
            /// zeroed. Ball Physics #1 §3.1.5.
            /// </summary>
            public static readonly float MinForceDirectionSqMagnitude = 0.0001f; // TODO: replace with config loader (Stage 1)
        }

        public static class Spin
        {
            /// <summary>[GT] Velocity-dependent spin decay coefficient (s/m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float DecayVelocityFactor = 0.01f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin-rate-dependent decay coefficient (1/rad). Ball Physics #1 §3.1.2.</summary>
            public static readonly float DecaySpinFactor = 0.005f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Aerodynamic torque coefficient. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorqueCoefficient = 0.01f; // TODO: replace with config loader (Stage 1)

            /// <summary>
            /// [EST] Rate at which spin decays during rolling (rad/s per second). Ball Physics #1 §3.1.2.
            /// Ground-contact friction dominates over aerodynamic torque for rolling balls.
            /// Must NOT be confused with the airborne aerodynamic torque model.
            /// </summary>
            public static readonly float RollingSpinDecayPerSecond = 5.0f;
        }

        public static class Bounce
        {
            /// <summary>
            /// [EST] Ratio of spin angular velocity that converts to linear velocity on contact.
            /// Empirically derived: ~10% of contact point velocity transfers. Ball Physics #1 §3.1.2.
            /// </summary>
            public static readonly float SpinToLinearRatio = 0.1f;

            /// <summary>
            /// [DERIVED] Rotational-coupling divisor for the bounce friction stick impulse.
            /// Formula: 1 + (MASS × RADIUS²) / MomentOfInertia (= 2.5 for the 2/3·m·r² hollow
            /// sphere). Source constants: Ball.MASS, Ball.RADIUS, Ball.MomentOfInertia.
            /// Ball Physics #1 §3.1.8 / ERR-001-002. Friction changes both tangential velocity
            /// and spin, so the impulse that zeroes contact-point slip is m·|v_contact| divided
            /// by this factor — the undivided form over-corrects and reverses the slip.
            /// </summary>
            public static readonly float StickImpulseCouplingDivisor =
                1f + (Ball.MASS * Ball.RADIUS * Ball.RADIUS) / Ball.MomentOfInertia;

            /// <summary>
            /// [GT] Minimum contact-point slip speed (m/s) for the bounce friction impulse to
            /// apply; below this the contact is treated as non-sliding. Ball Physics #1 §3.1.8.
            /// </summary>
            public static readonly float MinContactSpeed = 0.01f; // TODO: replace with config loader (Stage 1)
        }

        public static class Rolling
        {
            /// <summary>[GT] Rolling resistance for dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassDry = 0.13f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Rolling resistance for wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassWet = 0.07f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Rolling resistance for long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassLong = 0.22f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Rolling resistance for artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceArtificial = 0.09f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Rolling resistance for frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceFrozen = 0.04f; // TODO: replace with config loader (Stage 1)
        }

        public static class SurfaceCoR
        {
            /// <summary>[GT] Coefficient of restitution — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = 0.65f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Coefficient of restitution — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = 0.70f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Coefficient of restitution — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = 0.55f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Coefficient of restitution — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = 0.72f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Coefficient of restitution — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = 0.80f; // TODO: replace with config loader (Stage 1)
        }

        public static class SurfaceFriction
        {
            /// <summary>[GT] Tangential friction coefficient — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = 0.60f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Tangential friction coefficient — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = 0.40f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Tangential friction coefficient — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = 0.70f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Tangential friction coefficient — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = 0.55f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Tangential friction coefficient — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = 0.20f; // TODO: replace with config loader (Stage 1)
        }

        public static class SurfaceSpinRetention
        {
            /// <summary>[GT] Spin retention multiplier after ground contact — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = 0.80f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin retention multiplier after ground contact — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = 0.85f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin retention multiplier after ground contact — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = 0.70f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin retention multiplier after ground contact — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = 0.75f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin retention multiplier after ground contact — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = 0.90f; // TODO: replace with config loader (Stage 1)
        }

        public static class State
        {
            /// <summary>[GT] Minimum velocity before ball considered stationary (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinVelocity = 0.1f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Minimum spin before considered zero (rad/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinSpin = 0.1f; // TODO: replace with config loader (Stage 1)

            /// <summary>
            /// [GT] Height threshold to ENTER airborne state (m). Ball Physics #1 §3.1.2.
            /// Position.z is ball CENTER. At rest: z = RADIUS (0.11 m). 0.17 m means centre is 6 cm above resting position.
            /// </summary>
            public static readonly float AirborneEnterThreshold = 0.17f; // TODO: replace with config loader (Stage 1)

            /// <summary>
            /// [GT] Height threshold to EXIT airborne state (m). Ball Physics #1 §3.1.2.
            /// Hysteresis: exit threshold lower than enter to prevent oscillation.
            /// At 0.13 m, ball centre is 2 cm above resting position.
            /// </summary>
            public static readonly float AirborneExitThreshold = 0.13f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Vertical velocity after bounce to stay airborne (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float BounceVelocityThreshold = 0.5f; // TODO: replace with config loader (Stage 1)
        }

        public static class Limits
        {
            /// <summary>[EST] Maximum ball velocity in m/s (fastest shot ~45 m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MaxVelocity = 50.0f;

            /// <summary>[EST] Maximum angular velocity in rad/s. Ball Physics #1 §3.1.2.</summary>
            public static readonly float MaxSpin = 80.0f;

            /// <summary>[EST] Maximum height in metres (sanity check). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MaxHeight = 50.0f;

            /// <summary>[GT] Buffer zone beyond pitch boundaries (m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float PitchBuffer = 20.0f; // TODO: replace with config loader (Stage 1)
        }

        public static class Possession
        {
            /// <summary>[GT] Max distance for possession (m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ControlRadius = 0.5f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Max relative ball speed for control (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ControlVelocity = 2.0f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Min opponent distance for uncontested control (m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ChallengeRadius = 1.0f; // TODO: replace with config loader (Stage 1)

            /// <summary>
            /// [GT] Max ball height for ground control (m). Ball Physics #1 §3.1.2 / §3.1.11.
            /// AUTHORITY: this constant is the single source of truth for the ground-control
            /// height threshold across the simulation. <c>FirstTouchConstants.GroundControlHeight</c>
            /// mirrors this value via [CROSS] (Spec #20 §4.2 single-consumer routing) so that
            /// the First Touch §3.4.3 aerial-ball routing guard cannot silently drift from the
            /// Ball Physics §3.1.11 possession height gate. Tune here only.
            /// </summary>
            public static readonly float ControlHeight = 0.5f; // TODO: replace with config loader (Stage 1)
        }

        public static class Pitch
        {
            /// <summary>[FIXED] Pitch length in metres. Ball Physics #1 §3.1.2.</summary>
            public const float LENGTH = 105.0f;

            /// <summary>[FIXED] Pitch width in metres. Ball Physics #1 §3.1.2.</summary>
            public const float WIDTH = 68.0f;

            /// <summary>[FIXED] Goal width in metres. Ball Physics #1 §3.1.2.</summary>
            public const float GOAL_WIDTH = 7.32f;

            /// <summary>[FIXED] Goal height (crossbar) in metres. Ball Physics #1 §3.1.2.</summary>
            public const float GOAL_HEIGHT = 2.44f;

            /// <summary>[FIXED] Goal post diameter in metres. Ball Physics #1 §3.1.2.</summary>
            public const float POST_DIAMETER = 0.12f;
        }

        public static class GoalPost
        {
            /// <summary>[GT] Coefficient of restitution (aluminium/steel). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CoefficientOfRestitution = 0.75f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Spin retention on metal surface. Ball Physics #1 §3.1.2.</summary>
            public static readonly float SpinRetention = 0.40f; // TODO: replace with config loader (Stage 1)
        }

        public static class Rendering
        {
            /// <summary>[GT] Shadow offset per metre of height. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShadowOffsetFactor = 0.3f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Ball scale increase per metre of height. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeightScaleFactor = 0.02f; // TODO: replace with config loader (Stage 1)
        }

        public static class BodyPartRetention
        {
            /// <summary>[GT] Foot speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float FootSpeed  = 0.75f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Foot spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float FootSpin   = 0.30f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Shin speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShinSpeed  = 0.65f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Shin spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShinSpin   = 0.20f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Thigh speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ThighSpeed = 0.60f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Thigh spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ThighSpin  = 0.40f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Torso speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorsoSpeed = 0.55f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Torso spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorsoSpin  = 0.50f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Head speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeadSpeed  = 0.70f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Head spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeadSpin   = 0.10f; // TODO: replace with config loader (Stage 1)

            /// <summary>[GT] Arm speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ArmSpeed   = 0.50f; // TODO: replace with config loader (Stage 1)
            /// <summary>[GT] Arm spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ArmSpin    = 0.30f; // TODO: replace with config loader (Stage 1)
        }

        public static class Logging
        {
            /// <summary>[GT] Interval between position snapshots (seconds). Ball Physics #1 §3.1.2.</summary>
            public static readonly float SnapshotInterval = 1.0f; // TODO: replace with config loader (Stage 1)
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                                      |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; non-FIXED constants      |
// |         |            |        | renamed PascalCase per Spec #20 §3.2.3; const → static readonly for GT/EST/  |
// |         |            |        | DERIVED per Spec #20 §3.2.3; Diameter/CrossSectionArea/MomentOfInertia now   |
// |         |            |        | [DERIVED] static readonly formulas instead of hardcoded literals; file header |
// |         |            |        | added per FR-CS-056/057.                                                     |
// | 1.2     | 2026-05-24 | —      | Add Rolling.ResistanceArtificial/Frozen; add SurfaceCoR, SurfaceFriction,   |
// |         |            |        | SurfaceSpinRetention nested classes for all 5 surface types per spec §3.1.2  |
// |         |            |        | surface properties table. Eliminates FR-CS-016 literals in SurfaceProperties.|
// | 1.3     | 2026-06-02 | —      | AR-1 H-2: file header path corrected to src/ball-physics/.                  |
// | 1.4     | 2026-06-03 | —      | AR-2 L-5: Possession.ControlHeight XML doc gains cross-spec drift warning   |
// |         |            |        | flagging the parallel FirstTouchConstants.GroundControlHeight declaration   |
// |         |            |        | (Spec #20 §4.2 routing deferred to cross-spec pass).                        |
// | 1.5     | 2026-06-03 | —      | AR-3 fixes. M-1: new BodyPartRetention nested class catalogues the 12       |
// |         |            |        | per-body-part (speedRetention, spinRetention) constants previously inline   |
// |         |            |        | in BodyPartCoefficients.cs (FR-CS-016 — no magic numbers in ball-physics).  |
// |         |            |        | L-5: Possession.ControlHeight XML doc now back-references the root          |
// |         |            |        | CLAUDE.md OPEN ISSUES entry tracking the cross-spec routing decision so     |
// |         |            |        | the deferral has a discoverable anchor.                                     |
// | 1.6     | 2026-06-03 | —      | AR-4 L-4: BodyPartRetention XML docs use "factor" instead of "multiplier"   |
// |         |            |        | for terminology consistency with the consuming BodyPartCoefficients class   |
// |         |            |        | (which already uses retention / coefficient / factor vocabulary).           |
// | 1.7     | 2026-06-08 | —      | Cross-spec routing close-out (Spec #20 §4.2): Possession.ControlHeight is   |
// |         |            |        | now the declared AUTHORITY for the ground-control height threshold; XML    |
// |         |            |        | drift warning replaced with an authority/consumer pointer naming           |
// |         |            |        | FirstTouchConstants.GroundControlHeight as the single-consumer [CROSS]     |
// |         |            |        | mirror. Closes the long-standing CLAUDE.md OPEN ISSUE                      |
// |         |            |        | "Possession.ControlHeight ↔ GroundControlHeight cross-spec routing"        |
// |         |            |        | (since 2026-06-03). No value change; tune-here-only governance.            |
// | 1.8     | 2026-06-09 | —      | AR-7 fixes. M-1/L-1: new Bounce.StickImpulseCouplingDivisor [DERIVED]       |
// |         |            |        | (1 + m·r²/I; ERR-001-002), Bounce.MinContactSpeed [GT] (was 0.01f literal   |
// |         |            |        | in BallGroundInteraction), Magnus.MinForceDirectionSqMagnitude [GT] (was    |
// |         |            |        | 0.0001f literal in BallPhysicsCore) — FR-CS-016. L-2: Ball.MomentOfInertia  |
// |         |            |        | retagged [EST] → [DERIVED] (documented formula over [FIXED] inputs per      |
// |         |            |        | FR-CS-021; hollow-sphere model caveat retained); class doc gains LAYOUT     |
// |         |            |        | NOTE recording the domain-class-vs-tag-region deviation as intentional.     |
#endregion
