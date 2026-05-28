// File:     src/shot-mechanics/ShotAgentAttributes.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.3.1, Code Standards #20
// Purpose:  Shot Mechanics' snapshot of agent attributes consumed from Agent Movement.
//           All fields are read-only from Agent Movement; Shot Mechanics never writes them.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Shot Mechanics' snapshot of agent attributes consumed from Agent Movement.
    /// Read once at INITIATING state and held constant for the execution pipeline.
    /// Shot Mechanics #6 §4.3.1.
    /// </summary>
    public struct ShotAgentAttributes
    {
        /// <summary>Shooting accuracy within ~20m [1–20]. Sigmoid-blended with LongShots (§3.2.3). Agent Movement §3.5.6.</summary>
        public int Finishing;

        /// <summary>Shooting accuracy beyond ~20m [1–20]. Sigmoid-blended with Finishing (§3.2.3). Agent Movement §3.5.6.</summary>
        public int LongShots;

        /// <summary>Pressure resistance; governs pressure scalar in error model [1–20]. §3.6.5. Agent Movement §3.5.6.</summary>
        public int Composure;

        /// <summary>Kick velocity ceiling attribute [1–20]. §3.2.4. Agent Movement §3.5.6.</summary>
        public int KickPower;

        /// <summary>Technical spin quality; modulates sidespin in §3.4.3 [1–20]. Agent Movement §3.5.6.</summary>
        public int Technique;

        /// <summary>Weak-foot quality [1–5]. 5 = virtually no penalty; 1 = maximum penalty. §3.8. Agent Movement §3.5.6.</summary>
        public int WeakFootRating;

        /// <summary>Fatigue scalar [0, 1]. 0 = fully rested, 1 = fully fatigued. §3.2.7. Agent Movement §3.5.6.</summary>
        public float Fatigue;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
