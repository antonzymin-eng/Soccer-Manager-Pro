// File:     src/positioning-ai/TacticTranslation.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-016 / FR-TI-025 / FR-TI-031; Positioning AI #12 §3.5
// Purpose:  Consumer-side (T2) translation seam: resolves a #21 TacticWidth / TacticDefWidth onto
//           the multiplicative lateral-compactness scalar that #12 ContextModifier already applies
//           to (slot − centroid).Y. Pure functions, translate-once (FR-TI-025); read by
//           ContextModifier, never allocates.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// #21 → #12 translation (§3.4). Lives in the consuming assembly per KD-2 — the #21 data
    /// layer never references #12. Both width enums place <c>Standard</c> on the identity row
    /// (scalar 1.00, FR-TI-031); their ordinals index
    /// <see cref="TacticalInstructionsConstants.WidthScalar"/> /
    /// <see cref="TacticalInstructionsConstants.DefWidthScalar"/> DIRECTLY (no rank remap, unlike
    /// the #8 Pressing/Passing maps), with the §3.1 F5 widening clamp.
    /// </summary>
    public static class TacticTranslation
    {
        /// <summary>
        /// §3.4: in-possession <see cref="TacticWidth"/> → lateral-compactness scalar on the #12
        /// (slot − centroid).Y rescale (Standard ⇒ 1.00, identity, FR-TI-031; a wider shape grows
        /// the lateral spread, a narrower shape shrinks it). F5: a widened ordinal clamps to the
        /// boldest peer, VeryWide.
        /// </summary>
        public static float WidthCompactnessScalar(TacticWidth width)
            => TacticalInstructionsConstants.WidthScalar[
                   ClampIndex((int)width, TacticalInstructionsConstants.WidthScalar.Length)];

        /// <summary>
        /// §3.4: out-of-possession <see cref="TacticDefWidth"/> → lateral-compactness scalar on the
        /// #12 (slot − centroid).Y rescale for the OOP phases (Standard ⇒ 1.00, identity,
        /// FR-TI-031). F5: a widened ordinal clamps to the boldest peer, Wide.
        /// </summary>
        public static float DefWidthCompactnessScalar(TacticDefWidth defWidth)
            => TacticalInstructionsConstants.DefWidthScalar[
                   ClampIndex((int)defWidth, TacticalInstructionsConstants.DefWidthScalar.Length)];

        private static int ClampIndex(int index, int count)
        {
            if (index < 0) return 0;
            return index >= count ? count - 1 : index;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-06-29 | —      | Initial T2 consumer seam: TacticWidth / TacticDefWidth → #12      |
// |         |            |        |   lateral-compactness scalar (direct ordinal lookup, §3.1 F5).    |
#endregion
