// File:     src/defensive-ai/TacticTranslation.cs
// Created:  2026-06-29
// Modified: 2026-07-07
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-022 / FR-TI-025 / FR-TI-031; Defensive AI #14 §3.7 / KD-9
// Purpose:  Consumer-side (T2) translation seam: resolves the #21 OffsideTrap toggle onto the #14
//           offside-trap request. Pure functions, translate-once (FR-TI-025); never allocates.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// #21 → #14 translation (§3.4). Lives in the consuming assembly per KD-2 — the #21 data layer
    /// never references #14. The §3.4 table maps <c>TeamTactic.OffsideTrap</c> onto the #14 trap as a
    /// <b>bool passthrough</b>; <c>false</c> is the <see cref="TeamTactic.Balanced"/> identity
    /// (FR-TI-031). Per KD-9 the toggle is a <b>request, never a guarantee</b> — #14's §3.7.2
    /// autonomous cascade remains the adjudicator. The man-mark override translation (FR-TI-023,
    /// per-<c>PlayerTactic</c>) lands with the per-agent routing field; it is not part of this
    /// team-level seam.
    /// </summary>
    public static class TacticTranslation
    {
        /// <summary>
        /// §3.4: <c>TeamTactic.OffsideTrap</c> → the #14 offside-trap request flag carried on
        /// <see cref="DefensiveSnapshot.OffsideTrapRequested"/> (identity passthrough; <c>false</c>
        /// is the behaviour-neutral default, FR-TI-031).
        /// </summary>
        public static bool OffsideTrapRequested(bool offsideTrap) => offsideTrap;

        /// <summary>
        /// §3.4 (cheap-item addition, FR-TI-033): <see cref="MarkingOrientation"/> → multiplicative
        /// scalar on the #14 MAN_MARK candidate radius (<c>DefensiveAIConstants.ManMarkCandidateRadiusM</c>).
        /// Direct ordinal lookup into <see cref="TacticalInstructionsConstants.MarkingOrientationScalar"/>
        /// (Balanced ⇒ ×1.0, identity, FR-TI-031); a ball-oriented team narrows the radius (favours
        /// ZONAL/ball-side coverage), a man-oriented team widens it (commits tighter to individuals).
        /// F5: an out-of-range ordinal clamps to the boldest peer.
        /// </summary>
        public static float MarkRadiusScalar(MarkingOrientation orientation)
            => TacticalInstructionsConstants.MarkingOrientationScalar[
                   ClampIndex((int)orientation, TacticalInstructionsConstants.MarkingOrientationScalar.Length)];

        private static int ClampIndex(int index, int count)
        {
            if (index < 0) return 0;
            return index >= count ? count - 1 : index;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-06-29 | —      | Initial T2 consumer seam: OffsideTrap → #14 trap request          |
// |         |            |        |   (bool passthrough, false identity; KD-9 request-not-guarantee). |
// | 1.1     | 2026-07-07 | —      | Cheap-item addition: + MarkRadiusScalar(MarkingOrientation) →     |
// |         |            |        |   #14 MAN_MARK candidate radius scalar (direct ordinal lookup,    |
// |         |            |        |   §3.1 F5 clamp; Balanced ⇒ ×1.0 identity).                       |
#endregion
