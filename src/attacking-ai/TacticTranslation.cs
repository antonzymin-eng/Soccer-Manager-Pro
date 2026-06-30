// File:     src/attacking-ai/TacticTranslation.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.3 / §3.4, FR-TI-021 / FR-TI-025 / FR-TI-031; Attacking AI #15 §3.8
// Purpose:  Consumer-side (T2) translation seam: resolves a #21 FocusPlay onto the #15 overload
//           flank preference. Pure functions, translate-once (FR-TI-025); never allocates.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// #21 → #15 translation (§3.3 / §3.4). Lives in the consuming assembly per KD-2 — the #21 data
    /// layer never references #15. <see cref="FocusPlay"/> is a NEW branch (KD-11) with no existing
    /// #15 hook; it contributes a flank bias in <see cref="OverloadDetector"/> (FR-TI-021).
    /// <see cref="FocusPlay.Mixed"/> (the zero-value identity) and <see cref="FocusPlay.ThroughMiddle"/>
    /// express no lateral preference (<c>null</c>), so a default tactic is behaviour-neutral
    /// (FR-TI-031). Consumed by <see cref="OverloadDetector.Evaluate"/> as a flank-preference bias;
    /// the bias magnitude is pending the §5.6 / G2 balance pass.
    /// </summary>
    public static class TacticTranslation
    {
        /// <summary>
        /// §3.3: <see cref="FocusPlay"/> → preferred attacking <see cref="Flank"/>, or <c>null</c> for
        /// no lateral preference. <c>Mixed</c> (identity) and <c>ThroughMiddle</c> → <c>null</c>;
        /// <c>LeftFlank</c> → <see cref="Flank.Left"/>; <c>RightFlank</c> → <see cref="Flank.Right"/>.
        /// </summary>
        public static Flank? PreferredFlank(FocusPlay focus)
        {
            switch (focus)
            {
                case FocusPlay.LeftFlank:  return Flank.Left;
                case FocusPlay.RightFlank: return Flank.Right;
                default:                   return null; // Mixed (identity) / ThroughMiddle: no bias.
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-06-29 | —      | Initial T2 consumer seam: FocusPlay → #15 preferred flank         |
// |         |            |        |   (Mixed/ThroughMiddle → null identity; FR-TI-021).              |
#endregion
