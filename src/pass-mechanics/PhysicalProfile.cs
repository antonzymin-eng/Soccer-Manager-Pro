// File:     src/pass-mechanics/PhysicalProfile.cs
// Created:  2026-05-26
// Modified: 2026-06-11
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

        /// <summary>[GT] Minimum viable pass distance (metres).
        /// NOTE (AR-9 L-3): declared-but-unconsumed at Stage 0 — no calculator clamps or
        /// validates against it (Decision Tree #8 owns request plausibility; FM-07 rejects
        /// only non-positive/non-finite distance). Retained as §3.1.4 master-table data.</summary>
        public float DistMin;

        /// <summary>[GT] Maximum viable pass distance (metres). Formula base for velocity and angle.</summary>
        public float DistMax;

        /// <summary>[GT] Base spin magnitude (rad/s) at Technique = 1. §3.1.4 master table.</summary>
        public float SpinMagnitudeBase;

        /// <summary>[GT] Maximum spin magnitude (rad/s) at Technique = 20. §3.1.4 master table.</summary>
        public float SpinMagnitudeMax;

        /// <summary>Dominant spin direction for this pass type. Metadata mirroring §3.1.4 table.
        /// NOTE (AR-9 L-3): unconsumed at Stage 0 — actual spin axes are selected by
        /// PassVelocityCalculator.ComputeSpinVector's per-type switch.</summary>
        public SpinType DominantSpin;

        /// <summary>True if pass type has a significant aerial phase.
        /// NOTE (AR-9 L-3): unconsumed at Stage 0 — the §3.3.4 apex-formula gate is
        /// PassVelocityCalculator.IsAerialFormula, a PARALLEL classification surface
        /// (the helper, not this field, is authoritative). The AR-9 sweep verified the
        /// two agree across all 9 (PassType, CrossSubType) profiles; keep them
        /// consistent if either changes.</summary>
        public bool IsAerial;

        /// <summary>True if ball is targeted at a space position rather than an agent.</summary>
        public bool IsSpaceTargeted;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                    |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                  |
// | 1.1     | 2026-05-26 | —      | L5: SpinType moved to SpinType.cs (one-type-per-file).   |
// | 1.2     | 2026-06-11 | —      | AR-9 L-3 (doc-only): DistMin / DominantSpin / IsAerial   |
// |         |            |        |     noted as declared-but-unconsumed at Stage 0 (CS       |
// |         |            |        |     AR-10 MaxIterations precedent); IsAerial note names  |
// |         |            |        |     the IsAerialFormula parallel-surface drift hazard    |
// |         |            |        |     and records the 9-profile agreement verification     |
// |         |            |        |     (AR-10 L-2 wording fix, same commit).                |
#endregion
