// File:     src/tactical-instructions/PlayerRole.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.3, §3.3, Appendix A.4, Code Standards #20
// Purpose:  Behavioural player role (Poacher, Mezzala, ...). A modifier layered on a
//           position — distinct from positional RoleId (#12), which this layer never
//           references (KD-3 / FR-TI-005). Indexes RoleWeightModifiers (§3.3).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Behavioural player role (FR-TI-005 / KD-3). NOT positional <c>RoleId</c> (#12) — that is a
    /// position; this is a behaviour modifier layered on one. The ordinal indexes the
    /// <see cref="TacticalInstructionsConstants.RoleWeightModifiers"/> row used by #8 (§3.3).
    /// Stage-0 is a curated subset (the Appendix A.4 rows); widen by appending at T-roster time.
    /// <see cref="Default"/> (index 0) is the identity row (all action weights 1.0; FR-TI-031).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum PlayerRole : byte
    {
        /// <summary>No behavioural modifier — RoleWeightModifiers row is all 1.0 (identity). §3.3.</summary>
        Default = 0,

        /// <summary>Poacher: raises SHOOT, lowers HOLD. §3.3 / A.4.</summary>
        Poacher = 1,

        /// <summary>Deep-Lying Playmaker: raises PASS, lowers DRIBBLE. §3.3 / A.4.</summary>
        DeepLyingPlaymaker = 2,

        /// <summary>Ball-Winning Midfielder: raises PRESS / INTERCEPT. §3.3 / A.4.</summary>
        BallWinningMid = 3,

        /// <summary>Inside Forward: raises SHOOT / DRIBBLE, lowers HOLD. §3.3 / A.4.</summary>
        InsideForward = 4,

        /// <summary>Target Man: raises HOLD / SHOOT, lowers DRIBBLE. §3.3 / A.4.</summary>
        TargetMan = 5
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
