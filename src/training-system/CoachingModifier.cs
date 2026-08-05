// File:     src/training-system/CoachingModifier.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 KD-3 / FR-TR-016; Code Standards #20
// Purpose:  The staff routing seam #34 becomes the producer of. A value type, not an interface against
//           the absent #34 producer (FR-LW-031 — no phantom interface).

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// The coaching-quality routing seam (KD-3). <see cref="Identity"/> is ×1.0 on every #29 term, so a
    /// game with no coaching staff runs the step exactly as if the seam did not exist (FR-TR-016).
    /// <para>
    /// The struct is deliberately <b>empty at T0</b> and <see cref="Identity"/> is <c>default</c>: with
    /// no field there is no zero-value trap to guard, and no magnitude can be invented before #34
    /// defines what coach quality means. #34's quality fields APPEND here, and
    /// <see cref="TrainingStep"/>'s <c>ApplyCoach</c> seam — today the exact identity — becomes the one
    /// place that consumes them (#29 T3). This is the #28 <c>TrainingInput.Neutral</c> pattern.
    /// </para>
    /// <para>
    /// Note the contrast with #41's <c>MedicalModifier</c>, which carries per-mille fields and therefore
    /// needs an EXPLICIT identity factory (its <c>default</c> would be ×0 risk and a divide-by-zero
    /// recovery scale, #41 F4). #29's is safe as <c>default</c> only for as long as it stays empty —
    /// the T3 landing that adds a per-mille field MUST convert this to an explicit factory at the same
    /// time.
    /// </para>
    /// </summary>
    public readonly struct CoachingModifier
    {
        /// <summary>The identity modifier — ×1.0 on every #29 term, byte-identical to no coaching (FR-TR-016).</summary>
        public static CoachingModifier Identity => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, KD-3). |
#endregion
