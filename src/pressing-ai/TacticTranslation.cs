// File:     src/pressing-ai/TacticTranslation.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-017 / FR-TI-025; Pressing AI #13 §3.3
// Purpose:  Consumer-side (T2) translation seam: resolves a #21 LineOfEngagement onto the
//           multiplicative scalar applied to the #13 press-trigger radius
//           (PressTriggerDistanceM). Pure functions, translate-once (FR-TI-025); read by
//           the press selector, never allocates.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// #21 → #13 translation (§3.4). Lives in the consuming assembly per KD-2 — the #21 data
    /// layer never references #13. Unlike the #8 Pressing/Passing maps (whose #21 and #8 enums
    /// order oppositely, forcing a rank remap), <see cref="LineOfEngagement"/> ordinals
    /// (VeryLow=0…VeryHigh=4) index
    /// <see cref="TacticalInstructionsConstants.LineOfEngagementScalar"/> DIRECTLY, so this is a
    /// straight ordinal lookup with the §3.1 F5 widening clamp.
    /// </summary>
    public static class TacticTranslation
    {
        /// <summary>
        /// §3.4: <see cref="LineOfEngagement"/> → multiplicative scalar on the #13 press-trigger
        /// radius (Standard ⇒ 1.0, identity, FR-TI-031; a higher line widens the engagement radius,
        /// a lower line narrows it). F5: a widened value (ordinal beyond the table) clamps to the
        /// boldest peer, VeryHigh.
        /// </summary>
        public static float PressTriggerRadiusScalar(LineOfEngagement line)
            => TacticalInstructionsConstants.LineOfEngagementScalar[
                   ClampIndex((int)line, TacticalInstructionsConstants.LineOfEngagementScalar.Length)];

        private static int ClampIndex(int index, int count)
        {
            if (index < 0) return 0;
            return index >= count ? count - 1 : index;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-06-28 | —      | Initial T2 consumer seam: LineOfEngagement → #13 press-trigger    |
// |         |            |        |   radius scalar (direct ordinal lookup, §3.1 F5 clamp).          |
#endregion
