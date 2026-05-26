// File:     src/pass-mechanics/PassCancelledEvent.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, Code Standards #20
// Purpose:  PassCancelledEvent struct: published when a tackle interrupt cancels
//           the pass during WINDUP state. Invalid request rejection does not
//           produce this event (KD-8, §3.9.3).

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Published when a tackle interrupt cancels the pass during WINDUP state.
    /// Invalid request rejection does NOT produce this event (KD-8, §3.9.3).
    /// Pass Mechanics #5 §3.9.3.
    /// </summary>
    public struct PassCancelledEvent
    {
        /// <summary>ID of the agent whose pass was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the executing agent.</summary>
        public int TeamId;

        /// <summary>Reason for cancellation (TackleInterrupt in Stage 0).</summary>
        public CancelReason CancelReason;

        /// <summary>Pass type that was in progress.</summary>
        public PassType PassType;

        /// <summary>Simulation frame at cancellation.</summary>
        public int Frame;

        /// <summary>Match time in seconds at cancellation.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                           |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).  |
#endregion
