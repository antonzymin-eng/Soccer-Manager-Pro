// File:     src/injuries-medical/MedicalModifier.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 KD-5 / FR-MD-016, F4; Code Standards #20
// Purpose:  The medical-staff routing seam #34 becomes the producer of. Per-mille integer multipliers
//           so all medical arithmetic stays integer; default() is NOT a valid runtime value.

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// The medical-staff routing seam (KD-5). Both fields are <b>per-mille integer</b> multipliers —
    /// 1000 = ×1.0 — so no float enters the medical arithmetic (FR-MD-014).
    /// <para>
    /// <b><c>default(MedicalModifier)</c> is NOT a valid runtime value.</b> All-zero means ×0
    /// occurrence risk (nobody is ever injured) and a divide-by-zero recovery-days scale. Always use
    /// <see cref="Identity"/>, which is an explicit factory for exactly that reason. Both fields MUST
    /// be <b>positive</b> — a non-positive value of either kind fails loud at the consuming seam
    /// (F4 / FR-MD-016), because a negative multiplier is the same trap as a zero one but produces no
    /// crash: it silently clamps risk to zero, or one-days a Serious injury.
    /// </para>
    /// <para>
    /// No #34 interface is built (FR-LW-031) — #34 becomes the producer of a non-identity value when
    /// it lands, and MUST NOT add a second occurrence-risk or recovery-speed path (#41 §7.3).
    /// </para>
    /// </summary>
    public readonly struct MedicalModifier
    {
        /// <summary>Per-mille multiplier on the assembled occurrence risk; 1000 = ×1.0, above 1000 raises risk.</summary>
        public readonly int OccurrenceRiskMillMult;

        /// <summary>Per-mille recovery speed; 1000 = ×1.0, above 1000 is faster — it assigns FEWER total recovery days at injury time (§3.3), never a per-tick multiplier.</summary>
        public readonly int RecoverySpeedMillMult;

        /// <summary>Constructs a modifier from its two per-mille multipliers.</summary>
        /// <param name="occurrenceRiskMillMult">Per-mille occurrence-risk multiplier.</param>
        /// <param name="recoverySpeedMillMult">Per-mille recovery-speed multiplier.</param>
        public MedicalModifier(int occurrenceRiskMillMult, int recoverySpeedMillMult)
        {
            OccurrenceRiskMillMult = occurrenceRiskMillMult;
            RecoverySpeedMillMult = recoverySpeedMillMult;
        }

        /// <summary>The identity modifier — ×1.0 on both occurrence risk and recovery speed. The value every call passes until #34 lands (FR-MD-016).</summary>
        public static MedicalModifier Identity =>
            new MedicalModifier(
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE,
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0, KD-5).                             |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (M): the contract widened from "a zero RecoverySpeed     |
// |         |            |        | fails loud" to "both fields MUST be positive", matching the        |
// |         |            |        | MedicalStep gate — a negative multiplier is the same trap with no  |
// |         |            |        | crash to announce it.                                              |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): v1.1 landed with no version row; recorded here.     |
#endregion
