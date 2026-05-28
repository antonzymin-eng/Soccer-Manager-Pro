// File:     src/goalkeeper-mechanics/CrossClaimDuelContext.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.5, §3.6, Code Standards #20
// Purpose:  Per-duel resolution context for cross-claim and aerial contests.
//           Mirrors the ContestedDuelContext pattern from Heading Mechanics #10.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Per-duel context for one cross-claim or aerial duel frame.
    /// Pre-allocated and reset each frame; no heap allocation on hot path.
    /// Mirrors Heading Mechanics #10 ContestedDuelContext for consistency.
    /// Goalkeeper Mechanics #11 §2.2.5 / §3.6.
    /// </summary>
    public struct CrossClaimDuelContext
    {
        /// <summary>Sentinel value for WinnerAgentId before the duel has been resolved.</summary>
        public const int UnresolvedWinnerId = -1;

        /// <summary>Unique duel identifier for this frame. Derived from contact frame index. §3.6.</summary>
        public int DuelId;

        /// <summary>Number of agents registered in this duel. Valid range [1, MaxCrossClaimParticipants]. §3.6.</summary>
        public int ParticipantCount;

        /// <summary>Agent ID of the duel winner. -1 (UnresolvedWinnerId) until GoalkeeperCrossClaimDuel.ResolveAll runs. §3.6.</summary>
        public int WinnerAgentId;

        /// <summary>Body part used by the winning agent for contact. Determined by §3.6.1 geometry. §3.6.</summary>
        public BodyPartEnum ContactBodyPart;

        /// <summary>
        /// Start index into the shared participant ID / base-score buffer.
        /// Participant data occupies slots [BufferStartIndex, BufferStartIndex + ParticipantCount). §3.6.
        /// </summary>
        public int BufferStartIndex;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
