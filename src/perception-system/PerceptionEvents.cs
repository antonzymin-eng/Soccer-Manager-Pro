// File:     src/perception-system/PerceptionEvents.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Perception System #7 §3.8.3, §4.6.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  PerceptionRefreshEvent struct and RefreshTrigger enum.
//           Tier C event; ordinal 0x10; immediate dispatch via CosmeticChannel. §4.6.3.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Trigger reason for a forced mid-heartbeat perception refresh. §4.6.3.
    /// </summary>
    public enum RefreshTrigger
    {
        /// <summary>Ball contact event — pass, shot, clearance, header contact. §4.6.1.</summary>
        BallContact,

        /// <summary>Collision System tackle resolution complete. §4.6.1.</summary>
        TackleCompletion,

        /// <summary>Ball possession changes without direct physical contact. §4.6.1.</summary>
        PossessionChange
    }

    /// <summary>
    /// Published when a forced mid-heartbeat perception refresh occurs.
    /// Tier C: immediate dispatch via CosmeticChannel; excluded from digest. Ordinal 0x10.
    /// Perception System #7 §4.6.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PerceptionRefreshEvent : IEventC
    {
        /// <summary>Cause of the forced refresh. §4.6.3.</summary>
        public RefreshTrigger Trigger;

        /// <summary>First agent to refresh (always populated). §4.6.3.</summary>
        public int PrimaryAgentId;

        /// <summary>Second agent to refresh. PerceptionConstants.AGENT_ID_NONE (-1) if only one agent requires refresh. §4.6.3.</summary>
        public int SecondaryAgentId;

        /// <summary>Entity whose L_rec resets to 0. AGENT_ID_NONE if no specific entity warrants reset. §4.6.3.</summary>
        public int TriggeringEntityId;

        /// <summary>60Hz physics frame at which the triggering event occurred. §4.6.3.</summary>
        public int SimulationFrame;

        /// <summary>Match time (seconds) at which the triggering event occurred. §4.6.3.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                          |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventC, [StructLayout(Sequential)] to            |
// |         |            |        | PerceptionRefreshEvent. Ordinal 0x10. Event System #17 wiring.   |
#endregion
