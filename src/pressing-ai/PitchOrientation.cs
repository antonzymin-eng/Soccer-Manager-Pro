// File:     src/pressing-ai/PitchOrientation.cs
// Created:  2026-06-15
// Modified: 2026-06-15
// Author:   —
// Spec:     Pressing AI #13 §3.8–§3.9, Code Standards #20
// Purpose:  Pure static helper: maps an absolute pitch X coordinate into an
//           attack-relative coordinate (0 m = pressing team's own goal line) so
//           zone / pitch-third checks work symmetrically for teams attacking +X or −X.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Coordinate helper for attack-direction-relative pitch geometry (AR-2 H-1).
    /// §3.8 press-zone bounds and the §3.9 own-defensive-third floor are defined
    /// "from the pressing team's own goal line", but the snapshot stores absolute
    /// pitch X. These helpers fold the pressing team's attacking direction in so the
    /// same constants apply to a team attacking +X or −X. Pressing AI #13 §3.8–§3.9.
    /// </summary>
    public static class PitchOrientation
    {
        /// <summary>
        /// Converts an absolute pitch X (0–105 m) into an attack-relative X where
        /// 0 m is the pressing team's own goal line and 105 m is the opponent's.
        /// </summary>
        /// <param name="absoluteX">Absolute pitch X coordinate (metres).</param>
        /// <param name="attackingDirectionX">
        /// X component of the pressing team's normalised attacking direction.
        /// ≥ 0 → attacking +X (own goal at X=0); &lt; 0 → attacking −X (own goal at X=105).
        /// </param>
        /// <returns>Attack-relative X in metres.</returns>
        public static float AttackRelativeX(float absoluteX, float attackingDirectionX)
        {
            return attackingDirectionX >= 0f
                ? absoluteX
                : PressingAIConstants.PITCH_LENGTH_M - absoluteX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-15 | —      | Initial implementation (AR-2 H-1 shared attack-relative coordinate helper). |
#endregion
