// File:     src/shot-mechanics/ShotCancelledEvent.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.3, §4.7.1, Code Standards #20
// Purpose:  Struct event published when a shot is cancelled by tackle interrupt during WINDUP.
//           NOT published for ShotOutcome.Invalid (programming error, not a game event). §4.7.1.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Published when a tackle interrupt fires during WINDUP, cancelling the shot.
    /// Not published for ShotOutcome.Invalid. §2.4.3, §4.7.1.
    /// </summary>
    public struct ShotCancelledEvent
    {
        /// <summary>Agent whose shot was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the shooting agent. For event filtering.</summary>
        public int TeamId;

        /// <summary>Simulation frame on which the tackle interrupt fired.</summary>
        public int CancelFrame;

        /// <summary>Reason for cancellation. TackleInterrupt is the only valid reason at Stage 0.</summary>
        public ShotCancelReason Reason;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
