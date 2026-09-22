// File:     src/positioning-ai/TacticTranslation.cs
// Created:  2026-06-29
// Modified: 2026-09-21
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.4, FR-TI-004 / FR-TI-013 / FR-TI-016 / FR-TI-025 /
//           FR-TI-031; Positioning AI #12 §3.5 / §3.7.1 (ERR-012-012)
// Purpose:  Consumer-side translation seam: maps formation, resolves the approved Duty fore/aft
//           offset, and maps width dials onto #12 compactness. Pure functions; never allocates.

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
        /// <summary>Translates the manager-facing formation enum to #12's local formation family.</summary>
        public static FormationFamily FormationFamily(TacticFormation formation)
        {
            return formation switch
            {
                TacticFormation.F433  => TacticalDirector.PositioningAI.FormationFamily.F433,
                TacticFormation.F4231 => TacticalDirector.PositioningAI.FormationFamily.F4231,
                _                     => TacticalDirector.PositioningAI.FormationFamily.F442
            };
        }

        /// <summary>Resolves a player's duty to its longitudinal formation-anchor offset in metres.</summary>
        public static float DutyForeOffset(Duty duty)
            => TacticalInstructionsConstants.DutyForeOffsetM[
                   ClampIndex((int)duty, TacticalInstructionsConstants.DutyForeOffsetM.Length)];

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
// | 1.1     | 2026-09-21 | —      | Added TacticFormation → FormationFamily translation.              |
// | 1.2     | 2026-09-21 | —      | Added Duty and PositioningFreedom consumer translations.          |
// | 1.3     | 2026-09-21 | —      | PR #434 review / ERR-012-012: retain Duty; remove the unapproved  |
// |         |            |        | #12 PositioningFreedom translation (FR-TI-014 keeps it in #8).    |
#endregion
