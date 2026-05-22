namespace TacticalDirector.Core.Physics.Ball
{
    /// <summary>
    /// Physical constants for ball physics simulation.
    /// All values in SI units (meters, kilograms, seconds).
    /// Tags: [GT]=Gameplay-Tuned, [EST]=Estimated, [FIXED]=Physical law, [DERIVED]=Computed.
    /// </summary>
    public static class BallPhysicsConstants
    {
        public static class Ball
        {
            /// <summary>Ball mass in kg (FIFA: 410-450g, midpoint) [FIXED]</summary>
            public const float MASS = 0.43f;

            /// <summary>Ball radius in meters (FIFA: 68-70cm circumference) [FIXED]</summary>
            public const float RADIUS = 0.11f;

            /// <summary>Ball diameter in meters [DERIVED]</summary>
            public const float DIAMETER = 0.22f;

            /// <summary>Cross-sectional area in m² (πr²) [DERIVED]</summary>
            public const float CROSS_SECTION_AREA = 0.0380f;

            /// <summary>
            /// Moment of inertia in kg·m² (hollow sphere: 2/3 mr²).
            /// Real football differs ~10-20% due to internal structure. [EST]
            /// </summary>
            public const float MOMENT_OF_INERTIA = 0.00347f;
        }

        public static class Environment
        {
            /// <summary>Air density at sea level in kg/m³ [FIXED]</summary>
            public const float AIR_DENSITY = 1.225f;

            /// <summary>Gravitational acceleration in m/s² [FIXED]</summary>
            public const float GRAVITY = 9.81f;

            /// <summary>Dynamic viscosity of air in Pa·s [FIXED]</summary>
            public const float AIR_VISCOSITY = 1.81e-5f;
        }

        public static class Drag
        {
            /// <summary>Drag coefficient for laminar flow (Re < 200,000) [GT]</summary>
            public const float COEFFICIENT_LAMINAR = 0.20f;

            /// <summary>Drag coefficient in turbulent flow (Re > 400,000) [GT]</summary>
            public const float COEFFICIENT_TURBULENT = 0.10f;

            /// <summary>Speed at which drag crisis begins (m/s) [EST]</summary>
            public const float CRISIS_SPEED_LOW = 20.0f;

            /// <summary>Speed at which drag crisis ends (m/s) [EST]</summary>
            public const float CRISIS_SPEED_HIGH = 25.0f;
        }

        public static class Magnus
        {
            /// <summary>Base lift coefficient [GT]</summary>
            public const float LIFT_COEFFICIENT_BASE = 0.1f;

            /// <summary>Lift coefficient scaling factor [GT]</summary>
            public const float LIFT_COEFFICIENT_SCALE = 0.4f;

            /// <summary>Minimum spin parameter for valid calculation [GT]</summary>
            public const float MIN_SPIN_PARAMETER = 0.01f;

            /// <summary>Maximum spin parameter (clamped) [GT]</summary>
            public const float MAX_SPIN_PARAMETER = 1.0f;
        }

        public static class Spin
        {
            /// <summary>Velocity-dependent spin decay coefficient (s/m) [GT]</summary>
            public const float DECAY_VELOCITY_FACTOR = 0.01f;

            /// <summary>Spin-rate-dependent decay coefficient (1/rad) [GT]</summary>
            public const float DECAY_SPIN_FACTOR = 0.005f;

            /// <summary>Aerodynamic torque coefficient [GT]</summary>
            public const float TORQUE_COEFFICIENT = 0.01f;

            /// <summary>
            /// Rate at which spin decays during rolling (rad/s per second). [EST]
            /// Ground-contact friction dominates over aerodynamic torque for rolling balls.
            /// Must NOT be confused with the airborne aerodynamic torque model.
            /// </summary>
            public const float ROLLING_SPIN_DECAY_PER_SECOND = 5.0f;
        }

        public static class Bounce
        {
            /// <summary>
            /// Ratio of spin angular velocity that converts to linear velocity on contact.
            /// Empirically derived: ~10% of contact point velocity transfers. [EST]
            /// </summary>
            public const float SPIN_TO_LINEAR_RATIO = 0.1f;
        }

        public static class Rolling
        {
            /// <summary>Rolling resistance for dry grass [GT]</summary>
            public const float RESISTANCE_GRASS_DRY = 0.13f;

            /// <summary>Rolling resistance for wet grass [GT]</summary>
            public const float RESISTANCE_GRASS_WET = 0.07f;

            /// <summary>Rolling resistance for long grass [GT]</summary>
            public const float RESISTANCE_GRASS_LONG = 0.22f;
        }

        public static class State
        {
            /// <summary>Minimum velocity before ball considered stationary (m/s) [GT]</summary>
            public const float MIN_VELOCITY = 0.1f;

            /// <summary>Minimum spin before considered zero (rad/s) [GT]</summary>
            public const float MIN_SPIN = 0.1f;

            /// <summary>
            /// Height threshold to ENTER airborne state (m). [GT]
            /// Position.z is ball CENTER. At rest: z = RADIUS (0.11m).
            /// 0.17m means center is 6cm above resting position.
            /// </summary>
            public const float AIRBORNE_ENTER_THRESHOLD = 0.17f;

            /// <summary>
            /// Height threshold to EXIT airborne state (m). [GT]
            /// Hysteresis: exit threshold lower than enter to prevent oscillation.
            /// At 0.13m, ball center is 2cm above resting position.
            /// </summary>
            public const float AIRBORNE_EXIT_THRESHOLD = 0.13f;

            /// <summary>Vertical velocity after bounce to stay airborne (m/s) [GT]</summary>
            public const float BOUNCE_VELOCITY_THRESHOLD = 0.5f;
        }

        public static class Limits
        {
            /// <summary>Maximum ball velocity in m/s (fastest shot ~45 m/s) [EST]</summary>
            public const float MAX_VELOCITY = 50.0f;

            /// <summary>Maximum angular velocity in rad/s [EST]</summary>
            public const float MAX_SPIN = 80.0f;

            /// <summary>Maximum height in meters (sanity check) [EST]</summary>
            public const float MAX_HEIGHT = 50.0f;

            /// <summary>Buffer zone beyond pitch boundaries (m) [GT]</summary>
            public const float PITCH_BUFFER = 20.0f;
        }

        public static class Possession
        {
            /// <summary>Max distance for possession (m) [GT]</summary>
            public const float CONTROL_RADIUS = 0.5f;

            /// <summary>Max relative ball speed for control (m/s) [GT]</summary>
            public const float CONTROL_VELOCITY = 2.0f;

            /// <summary>Min opponent distance for uncontested control (m) [GT]</summary>
            public const float CHALLENGE_RADIUS = 1.0f;

            /// <summary>Max ball height for ground control (m) [GT]</summary>
            public const float CONTROL_HEIGHT = 0.5f;
        }

        public static class Pitch
        {
            /// <summary>Pitch length in meters [FIXED]</summary>
            public const float LENGTH = 105.0f;

            /// <summary>Pitch width in meters [FIXED]</summary>
            public const float WIDTH = 68.0f;

            /// <summary>Goal width in meters [FIXED]</summary>
            public const float GOAL_WIDTH = 7.32f;

            /// <summary>Goal height (crossbar) in meters [FIXED]</summary>
            public const float GOAL_HEIGHT = 2.44f;

            /// <summary>Goal post diameter in meters [FIXED]</summary>
            public const float POST_DIAMETER = 0.12f;
        }

        public static class GoalPost
        {
            /// <summary>Coefficient of restitution (aluminum/steel) [GT]</summary>
            public const float COEFFICIENT_OF_RESTITUTION = 0.75f;

            /// <summary>Spin retention on metal surface [GT]</summary>
            public const float SPIN_RETENTION = 0.40f;
        }

        public static class Rendering
        {
            /// <summary>Shadow offset per meter of height [GT]</summary>
            public const float SHADOW_OFFSET_FACTOR = 0.3f;

            /// <summary>Ball scale increase per meter of height [GT]</summary>
            public const float HEIGHT_SCALE_FACTOR = 0.02f;
        }

        public static class Logging
        {
            /// <summary>Interval between position snapshots (seconds) [GT]</summary>
            public const float SNAPSHOT_INTERVAL = 1.0f;
        }
    }
}
