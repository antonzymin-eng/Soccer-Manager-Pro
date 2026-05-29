// File:     src/attacking-ai/AttackRole.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.3, FR-AT-012, Code Standards #20
// Purpose:  Four-value attacking role enum (exactly FR-AT-012). No additional values permitted.

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Attacking role assigned to each off-ball agent per tick. Exactly four values (FR-AT-012).
    /// RUNNER is assigned to MIDFIELD/ATTACK-line agents making timed depth runs.
    /// SUPPORT_BALL is assigned to agents within the support radius (§3.5).
    /// HOLD_WIDTH anchors the near touchline to stretch the defensive shape (§3.6).
    /// WEAK_SIDE occupies the far side of the pitch for diagonal switches (§3.7).
    /// </summary>
    public enum AttackRole : byte
    {
        /// <summary>Default role; agent holds width near the near touchline. §3.6.</summary>
        HoldWidth   = 0,

        /// <summary>Agent is within SUPPORT_RADIUS_M of the ball carrier. §3.5.</summary>
        SupportBall = 1,

        /// <summary>Agent makes a timed depth run; carries RunParameters. §3.3–§3.4.</summary>
        Runner      = 2,

        /// <summary>Agent holds the far side of the pitch opposite the ball. §3.7.</summary>
        WeakSide    = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
