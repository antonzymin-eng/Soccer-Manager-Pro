// File:     src/pass-mechanics/PhysicalProfile.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.3, Code Standards #20
// Purpose:  PhysicalProfile struct: immutable physical bounds record per pass type.
//           Loaded at initialisation from PassTypeProfiles; never modified at runtime.
//           SpinType moved to its own file (L5 fix).

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Immutable record of physical bounds for one pass type (or cross sub-type).
    /// All values are [GT] Gameplay-Tunable per §3.1.3. Validated at load time.
    /// Pass Mechanics #5 §3.1.3.
    /// </summary>
    internal struct PhysicalProfile
    {
        /// <summary>[GT] Absolute minimum launch speed (m/s) — clamp floor for Ball.ApplyKick().</summary>
        public float VMin;

        /// <summary>[GT] Practical minimum kick speed (m/s) — interpolation base in §3.2 formula.</summary>
        public float VOffset;

        /// <summary>[GT] Maximum launch speed (m/s).</summary>
        public float VMax;

        /// <summary>[GT] Minimum launch angle (degrees above horizontal).</summary>
        public float AngleMin;

        /// <summary>[GT] Maximum launch angle (degrees above horizontal).</summary>
        public float AngleMax;

        /// <summary>[GT] Minimum viable pass distance (metres).</summary>
        public float DistMin;

        /// <summary>[GT] Maximum viable pass distance (metres). Formula base for velocity and angle.</summary>
        public float DistMax;

        /// <summary>[GT] Base spin magnitude (rad/s) at Technique = 1. §3.1.4 master table.</summary>
        public float SpinMagnitudeBase;

        /// <summary>[GT] Maximum spin magnitude (rad/s) at Technique = 20. §3.1.4 master table.</summary>
        public float SpinMagnitudeMax;

        /// <summary>Dominant spin direction for this pass type. Metadata mirroring §3.1.4 table.</summary>
        public SpinType DominantSpin;

        /// <summary>True if pass type has a significant aerial phase.</summary>
        public bool IsAerial;

        /// <summary>True if ball is targeted at a space position rather than an agent.</summary>
        public bool IsSpaceTargeted;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                    |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                  |
// | 1.1     | 2026-05-26 | —      | L5: SpinType moved to SpinType.cs (one-type-per-file).   |
#endregion
