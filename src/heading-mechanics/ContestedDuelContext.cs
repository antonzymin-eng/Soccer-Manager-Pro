// File:     src/heading-mechanics/ContestedDuelContext.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §3.7, KD-8, FR-HE-010, Code Standards #20
// Purpose:  Metadata for a single contested-duel frame; participant arrays are managed by
//           HeadingDuelResolution to avoid per-frame heap allocation (FR-CS-066).

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Metadata for a contested duel involving ≥2 eligible agents at overlapping candidate frames.
    /// Participant agent IDs and disturbance factors are stored in HeadingDuelResolution's
    /// pre-allocated backing arrays (zero heap allocation on hot path — FR-CS-066).
    /// Winner is populated by §3.7 after duel resolution.
    /// Heading Mechanics #10 §2.2 / §3.7.
    /// </summary>
    public struct ContestedDuelContext
    {
        /// <summary>Unique duel identifier for this frame. Carried into HeaderExecutedEvent.ContestedDuelId.</summary>
        public int DuelId;

        /// <summary>Number of participants in this duel. ≥2 by definition.</summary>
        public int ParticipantCount;

        /// <summary>Agent ID of the duel winner after §3.7 resolution. -1 before resolution completes.</summary>
        public int WinnerAgentId;

        /// <summary>Index into HeadingDuelResolution's pre-allocated participant buffer for this duel's entries.</summary>
        public int BufferStartIndex;

        /// <summary>Sentinel value for WinnerAgentId before resolution.</summary>
        public const int UnresolvedWinnerId = -1;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
