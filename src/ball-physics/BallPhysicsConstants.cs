// File:     src/ball-physics/BallPhysicsConstants.cs
// Created:  2026-05-24
// Modified: 2026-07-27 (shot-outcome pass)
// Modified: 2026-07-28 (shot-speed pass: new GoalFrame block (KD-4 swept-frame geometry))
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  All tunable and physical constants for ball physics simulation.
//           No magic numbers anywhere else in the ball-physics assembly.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

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
            public static readonly float CoefficientLaminar = Config.GetFloat("ball-physics", "Drag.CoefficientLaminar", 0.20f);

            /// <summary>[GT] Drag coefficient in turbulent flow (Re &gt; 400,000). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CoefficientTurbulent = Config.GetFloat("ball-physics", "Drag.CoefficientTurbulent", 0.10f);

            /// <summary>[EST] Speed at which drag crisis begins (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CrisisSpeedLow = 20.0f;

            /// <summary>[EST] Speed at which drag crisis ends (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float CrisisSpeedHigh = 25.0f;
        }

        public static class Magnus
        {
            /// <summary>[GT] Base lift coefficient. Ball Physics #1 §3.1.2.</summary>
            public static readonly float LiftCoefficientBase = Config.GetFloat("ball-physics", "Magnus.LiftCoefficientBase", 0.1f);

            /// <summary>[GT] Lift coefficient scaling factor. Ball Physics #1 §3.1.2.</summary>
            public static readonly float LiftCoefficientScale = Config.GetFloat("ball-physics", "Magnus.LiftCoefficientScale", 0.4f);

            /// <summary>[GT] Minimum spin parameter for valid calculation. Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinSpinParameter = Config.GetFloat("ball-physics", "Magnus.MinSpinParameter", 0.01f);

            /// <summary>[GT] Maximum spin parameter (clamped). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MaxSpinParameter = Config.GetFloat("ball-physics", "Magnus.MaxSpinParameter", 1.0f);

            /// <summary>
            /// [GT] Squared-magnitude threshold below which the ω̂ × v̂ cross product is
            /// treated as degenerate (spin near-parallel to velocity) and Magnus force is
            /// zeroed. Ball Physics #1 §3.1.5.
            /// </summary>
            public static readonly float MinForceDirectionSqMagnitude = Config.GetFloat("ball-physics", "Magnus.MinForceDirectionSqMagnitude", 0.0001f);
        }

        public static class Spin
        {
            /// <summary>[GT] Velocity-dependent spin decay coefficient (s/m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float DecayVelocityFactor = Config.GetFloat("ball-physics", "Spin.DecayVelocityFactor", 0.01f);

            /// <summary>[GT] Spin-rate-dependent decay coefficient (1/rad). Ball Physics #1 §3.1.2.</summary>
            public static readonly float DecaySpinFactor = Config.GetFloat("ball-physics", "Spin.DecaySpinFactor", 0.005f);

            /// <summary>[GT] Aerodynamic torque coefficient. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorqueCoefficient = Config.GetFloat("ball-physics", "Spin.TorqueCoefficient", 0.01f);

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
            public static readonly float MinContactSpeed = Config.GetFloat("ball-physics", "Bounce.MinContactSpeed", 0.01f);
        }

        public static class Rolling
        {
            /// <summary>[GT] Rolling resistance for dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassDry = Config.GetFloat("ball-physics", "Rolling.ResistanceGrassDry", 0.13f);

            /// <summary>[GT] Rolling resistance for wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassWet = Config.GetFloat("ball-physics", "Rolling.ResistanceGrassWet", 0.07f);

            /// <summary>[GT] Rolling resistance for long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceGrassLong = Config.GetFloat("ball-physics", "Rolling.ResistanceGrassLong", 0.22f);

            /// <summary>[GT] Rolling resistance for artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceArtificial = Config.GetFloat("ball-physics", "Rolling.ResistanceArtificial", 0.09f);

            /// <summary>[GT] Rolling resistance for frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ResistanceFrozen = Config.GetFloat("ball-physics", "Rolling.ResistanceFrozen", 0.04f);
        }

        public static class SurfaceCoR
        {
            /// <summary>[GT] Coefficient of restitution — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = Config.GetFloat("ball-physics", "SurfaceCoR.GrassDry", 0.65f);

            /// <summary>[GT] Coefficient of restitution — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = Config.GetFloat("ball-physics", "SurfaceCoR.GrassWet", 0.70f);

            /// <summary>[GT] Coefficient of restitution — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = Config.GetFloat("ball-physics", "SurfaceCoR.GrassLong", 0.55f);

            /// <summary>[GT] Coefficient of restitution — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = Config.GetFloat("ball-physics", "SurfaceCoR.Artificial", 0.72f);

            /// <summary>[GT] Coefficient of restitution — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = Config.GetFloat("ball-physics", "SurfaceCoR.Frozen", 0.80f);
        }

        public static class SurfaceFriction
        {
            /// <summary>[GT] Tangential friction coefficient — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = Config.GetFloat("ball-physics", "SurfaceFriction.GrassDry", 0.60f);

            /// <summary>[GT] Tangential friction coefficient — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = Config.GetFloat("ball-physics", "SurfaceFriction.GrassWet", 0.40f);

            /// <summary>[GT] Tangential friction coefficient — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = Config.GetFloat("ball-physics", "SurfaceFriction.GrassLong", 0.70f);

            /// <summary>[GT] Tangential friction coefficient — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = Config.GetFloat("ball-physics", "SurfaceFriction.Artificial", 0.55f);

            /// <summary>[GT] Tangential friction coefficient — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = Config.GetFloat("ball-physics", "SurfaceFriction.Frozen", 0.20f);
        }

        public static class SurfaceSpinRetention
        {
            /// <summary>[GT] Spin retention multiplier after ground contact — dry grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassDry = Config.GetFloat("ball-physics", "SurfaceSpinRetention.GrassDry", 0.80f);

            /// <summary>[GT] Spin retention multiplier after ground contact — wet grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassWet = Config.GetFloat("ball-physics", "SurfaceSpinRetention.GrassWet", 0.85f);

            /// <summary>[GT] Spin retention multiplier after ground contact — long grass. Ball Physics #1 §3.1.2.</summary>
            public static readonly float GrassLong = Config.GetFloat("ball-physics", "SurfaceSpinRetention.GrassLong", 0.70f);

            /// <summary>[GT] Spin retention multiplier after ground contact — artificial turf. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Artificial = Config.GetFloat("ball-physics", "SurfaceSpinRetention.Artificial", 0.75f);

            /// <summary>[GT] Spin retention multiplier after ground contact — frozen pitch. Ball Physics #1 §3.1.2.</summary>
            public static readonly float Frozen = Config.GetFloat("ball-physics", "SurfaceSpinRetention.Frozen", 0.90f);
        }

        public static class State
        {
            /// <summary>[GT] Minimum velocity before ball considered stationary (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinVelocity = Config.GetFloat("ball-physics", "State.MinVelocity", 0.1f);

            /// <summary>[GT] Minimum spin before considered zero (rad/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float MinSpin = Config.GetFloat("ball-physics", "State.MinSpin", 0.1f);

            /// <summary>
            /// [GT] Height threshold to ENTER airborne state (m). Ball Physics #1 §3.1.2.
            /// Position.z is ball CENTER. At rest: z = RADIUS (0.11 m). 0.17 m means centre is 6 cm above resting position.
            /// </summary>
            public static readonly float AirborneEnterThreshold = Config.GetFloat("ball-physics", "State.AirborneEnterThreshold", 0.17f);

            /// <summary>
            /// [GT] Height threshold to EXIT airborne state (m). Ball Physics #1 §3.1.2.
            /// Hysteresis: exit threshold lower than enter to prevent oscillation.
            /// At 0.13 m, ball centre is 2 cm above resting position.
            /// </summary>
            public static readonly float AirborneExitThreshold = Config.GetFloat("ball-physics", "State.AirborneExitThreshold", 0.13f);

            /// <summary>[GT] Vertical velocity after bounce to stay airborne (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float BounceVelocityThreshold = Config.GetFloat("ball-physics", "State.BounceVelocityThreshold", 0.5f);
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
            public static readonly float PitchBuffer = Config.GetFloat("ball-physics", "Limits.PitchBuffer", 20.0f);
        }

        public static class Possession
        {
            /// <summary>[GT] Max distance for possession (m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ControlRadius = Config.GetFloat("ball-physics", "Possession.ControlRadius", 0.5f);

            /// <summary>[GT] Max relative ball speed for control (m/s). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ControlVelocity = Config.GetFloat("ball-physics", "Possession.ControlVelocity", 2.0f);

            /// <summary>[GT] Min opponent distance for uncontested control (m). Ball Physics #1 §3.1.2.</summary>
            public static readonly float ChallengeRadius = Config.GetFloat("ball-physics", "Possession.ChallengeRadius", 1.0f);

            /// <summary>
            /// [GT] Max ball height for ground control (m). Ball Physics #1 §3.1.2 / §3.1.11.
            /// AUTHORITY: this constant is the single source of truth for the ground-control
            /// height threshold across the simulation. <c>FirstTouchConstants.GroundControlHeight</c>
            /// mirrors this value via [CROSS] (Spec #20 §4.2 single-consumer routing) so that
            /// the First Touch §3.4.3 aerial-ball routing guard cannot silently drift from the
            /// Ball Physics §3.1.11 possession height gate. Tune here only.
            /// </summary>
            public static readonly float ControlHeight = Config.GetFloat("ball-physics", "Possession.ControlHeight", 0.5f);
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
            public static readonly float CoefficientOfRestitution = Config.GetFloat("ball-physics", "GoalPost.CoefficientOfRestitution", 0.75f);

            /// <summary>[GT] Spin retention on metal surface. Ball Physics #1 §3.1.2.</summary>
            public static readonly float SpinRetention = Config.GetFloat("ball-physics", "GoalPost.SpinRetention", 0.40f);
        }

        public static class Rendering
        {
            /// <summary>[GT] Shadow offset per metre of height. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShadowOffsetFactor = Config.GetFloat("ball-physics", "Rendering.ShadowOffsetFactor", 0.3f);

            /// <summary>[GT] Ball scale increase per metre of height. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeightScaleFactor = Config.GetFloat("ball-physics", "Rendering.HeightScaleFactor", 0.02f);
        }

        public static class BodyPartRetention
        {
            /// <summary>[GT] Foot speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float FootSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.FootSpeed", 0.75f);
            /// <summary>[GT] Foot spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float FootSpin = Config.GetFloat("ball-physics", "BodyPartRetention.FootSpin", 0.30f);

            /// <summary>[GT] Shin speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShinSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.ShinSpeed", 0.65f);
            /// <summary>[GT] Shin spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ShinSpin = Config.GetFloat("ball-physics", "BodyPartRetention.ShinSpin", 0.20f);

            /// <summary>[GT] Thigh speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ThighSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.ThighSpeed", 0.60f);
            /// <summary>[GT] Thigh spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ThighSpin = Config.GetFloat("ball-physics", "BodyPartRetention.ThighSpin", 0.40f);

            /// <summary>[GT] Torso speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorsoSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.TorsoSpeed", 0.55f);
            /// <summary>[GT] Torso spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float TorsoSpin = Config.GetFloat("ball-physics", "BodyPartRetention.TorsoSpin", 0.50f);

            /// <summary>[GT] Head speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeadSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.HeadSpeed", 0.70f);
            /// <summary>[GT] Head spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float HeadSpin = Config.GetFloat("ball-physics", "BodyPartRetention.HeadSpin", 0.10f);

            /// <summary>[GT] Arm speed-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ArmSpeed = Config.GetFloat("ball-physics", "BodyPartRetention.ArmSpeed", 0.50f);
            /// <summary>[GT] Arm spin-retention factor on deflection. Ball Physics #1 §3.1.2.</summary>
            public static readonly float ArmSpin = Config.GetFloat("ball-physics", "BodyPartRetention.ArmSpin", 0.30f);
        }

        public static class AgentDeflection
        {
            /// <summary>
            /// [GT] Minimum ball speed (m/s) at which an agent-ball contact deflects the ball
            /// (BallCollision.ApplyAgentDeflection, wired via BallCollisionHandler.OnAgentCollision —
            /// ERR-003-007). NOT a reception guard — receptions are protected geometrically (the
            /// first-touch trigger reach, 1.0 m, is well outside the ~0.4 m combined hitbox, and a
            /// ball cannot jump the gap in one 60 Hz tick below ~35 m/s), and a Controlled ball is
            /// excluded outright. This gate exists so slow rolling balls in a crowd (and the
            /// loose-ball collect approach) never ping-pong off shins; measured shot speeds run
            /// ~12–21 m/s (shot-outcome design §4 baseline), driven passes up to 28 m/s — both
            /// deflect, as football does. Shot-outcome design KD-6 (AR-3 re-anchored 18 → 10:
            /// the assumed 20–35 m/s shot band was refuted by measurement).
            /// </summary>
            public static readonly float MinBallSpeedMps = Config.GetFloat("ball-physics", "AgentDeflection.MinBallSpeedMps", 10.0f);

            /// <summary>
            /// [FIXED] Minimum agent→ball separation (m) below which the deflection normal is
            /// degenerate and the contact is a no-op (mirror of SpatialHashConstants
            /// MIN_DISTANCE_EPSILON semantics at this seam). Shot-outcome design KD-6.
            /// </summary>
            public const float MIN_NORMAL_EPSILON = 1e-4f;
        }

        public static class Logging
        {
            /// <summary>[GT] Interval between position snapshots (seconds). Ball Physics #1 §3.1.2.</summary>
            public static readonly float SnapshotInterval = Config.GetFloat("ball-physics", "Logging.SnapshotInterval", 1.0f);
        }

        /// <summary>
        /// Physical goal-frame geometry for the swept post/crossbar collision
        /// (<c>BallCollision.ApplySweptGoalFrameCollision</c> — ERR-001-005 / shot-speed design
        /// KD-4). All rows are [DERIVED] from the IFAB Pitch constants: goal WIDTH is measured
        /// between the posts' INNER edges and GOAL_HEIGHT is to the crossbar's LOWER edge, so the
        /// cylinder AXES sit half a post diameter outward/upward of those datum lines — the same
        /// conventions <c>IsBetweenPostsUnderCrossbar</c> already relies on.
        /// </summary>
        public static class GoalFrame
        {
            /// <summary>
            /// [DERIVED] Ball-centre-to-frame-axis contact radius (m) for the swept segment test.
            /// Formula: Pitch.POST_DIAMETER / 2 + Ball.RADIUS. Shot-speed design KD-4.
            /// Source constants: Pitch.POST_DIAMETER, Ball.RADIUS.
            /// </summary>
            public static readonly float SweptTestRadius = Pitch.POST_DIAMETER * 0.5f + Ball.RADIUS;

            /// <summary>
            /// [DERIVED] Lateral offset (m) of each post AXIS from the goal-mouth centre line.
            /// Formula: Pitch.GOAL_WIDTH / 2 + Pitch.POST_DIAMETER / 2 (IFAB: width between inner
            /// edges). Source constants: Pitch.GOAL_WIDTH, Pitch.POST_DIAMETER.
            /// </summary>
            public static readonly float PostAxisOffsetY =
                Pitch.GOAL_WIDTH * 0.5f + Pitch.POST_DIAMETER * 0.5f;

            /// <summary>
            /// [DERIVED] Height (m) of the crossbar AXIS. Formula: Pitch.GOAL_HEIGHT +
            /// Pitch.POST_DIAMETER / 2 (IFAB: height to the bar's lower edge).
            /// Source constants: Pitch.GOAL_HEIGHT, Pitch.POST_DIAMETER.
            /// </summary>
            public static readonly float BarAxisZ = Pitch.GOAL_HEIGHT + Pitch.POST_DIAMETER * 0.5f;

            /// <summary>
            /// [DERIVED] Top (m) of the post cylinders' capped extent (the bar's upper edge).
            /// Formula: Pitch.GOAL_HEIGHT + Pitch.POST_DIAMETER.
            /// Source constants: Pitch.GOAL_HEIGHT, Pitch.POST_DIAMETER.
            /// </summary>
            public static readonly float FrameTopZ = Pitch.GOAL_HEIGHT + Pitch.POST_DIAMETER;

            /// <summary>
            /// [GT] Half-width (m) of the X band around each goal-line plane inside which the
            /// six-cylinder swept test runs at all — a cheap prefilter that skips the test for the
            /// overwhelming majority of ticks. Must exceed the per-tick ball displacement at the
            /// fastest kick (VAbsoluteMax / 60 ≈ 0.58 m) plus SweptTestRadius, so a segment that
            /// could touch the frame is never prefiltered out. Shot-speed design KD-4.
            /// </summary>
            public static readonly float SweptPrefilterBandM =
                Config.GetFloat("ball-physics", "GoalFrame.SweptPrefilterBandM", 1.0f);
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
// | 1.9     | 2026-07-27 | —      | Shot-outcome pass (design KD-6): new AgentDeflection block —       |
// |         |            |        | [GT] MinBallSpeedMps (18.0; the blockable-vs-receivable dial) +   |
// |         |            |        | [FIXED] MIN_NORMAL_EPSILON for the deflection normal.             |
// | 1.10    | 2026-07-28 | —      | Shot-speed pass (design KD-4): new GoalFrame block — SweptTestRadius/       |
// |         |            |        | PostAxisOffsetY/BarAxisZ/FrameTopZ [DERIVED] + SweptPrefilterBandM [GT]     |
// |         |            |        | for BallCollision.ApplySweptGoalFrameCollision (ERR-001-005).               |
#endregion
