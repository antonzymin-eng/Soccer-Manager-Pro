// File:     src/heading-mechanics/HeaderAttemptFailedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §3.9, KD-12, FR-HE-006, FR-HE-028, Code Standards #20
// Purpose:  Tier-C event published when a header attempt does not produce ball contact.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Published on every failed header attempt — NO Ball.ApplyKick call is made (FR-HE-006 / KD-12).
    /// Tier C event: telemetry-only payload; excluded from the determinism digest (Event System #17 KD-3).
    /// Heading Mechanics #10 §2.2 / §3.9.
    /// </summary>
    public struct HeaderAttemptFailedEvent
    {
        /// <summary>Agent that attempted the header.</summary>
        public int AgentId;

        /// <summary>Match time in seconds from kickoff at the failure frame. §2.2.</summary>
        public float MatchTime;

        /// <summary>Closest-approach distance (m) between ball path and agent head centre over the attempt window. §3.9.</summary>
        public float MissDistanceM;

        /// <summary>
        /// Signed timing offset in ms (positive = late) at the failure frame.
        /// Zero for PositionedPoorly / DisturbedInDuel failures where timing was not the cause.
        /// </summary>
        public float TimingOffsetMs;

        /// <summary>
        /// Structured cause per FR-HE-028.
        /// One of: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel.
        /// </summary>
        public FailureCause FailureCause;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
