// File:     src/Core/Physics/Agent/PlayerAttributes.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.5.1, §4.5.1, Code Standards #20
// Purpose:  Physical and technical attributes driving locomotion calculations.

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Player attributes consumed by locomotion, turning, and fatigue systems.
    /// All attributes are integers in [1, 20]. Agent Movement #2 §3.5.1.
    /// </summary>
    public struct PlayerAttributes
    {
        /// <summary>Top speed potential. Maps to base top speed 7.5–10.2 m/s. §3.2.4.</summary>
        public int Pace;

        /// <summary>Acceleration burst rate. Maps to exponential k 0.658–0.921 s⁻¹. §3.2.3.</summary>
        public int Acceleration;

        /// <summary>Lateral/backward movement quality. Maps to directional multipliers. §3.3.2.</summary>
        public int Agility;

        /// <summary>Stability during turning and collision recovery. §3.4.2, §3.1.5.</summary>
        public int Balance;

        /// <summary>Recovery speed from GROUNDED state. §3.1.5.</summary>
        public int Strength;

        /// <summary>Aerobic endurance. Governs aerobic drain rate (Stage 1+).</summary>
        public int Stamina;

        /// <summary>Creates attributes initialised to mid-range (10) for all fields.</summary>
        public static PlayerAttributes CreateDefault()
        {
            return new PlayerAttributes
            {
                Pace = 10,
                Acceleration = 10,
                Agility = 10,
                Balance = 10,
                Strength = 10,
                Stamina = 10
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
