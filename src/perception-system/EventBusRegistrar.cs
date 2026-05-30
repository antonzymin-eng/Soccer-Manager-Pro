// File:     src/perception-system/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Perception System #7 §4.6.5, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Perception System event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.EventSystem;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Registers Perception System event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Perception System #7 §4.6.5 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        /// <summary>
        /// Registers PerceptionRefreshEvent (0x10) with EventRegistry.
        /// </summary>
        public static void Initialize()
        {
            // 40 = SubsystemOrdinals.PerceptionSystem (Deterministic Simulation #16 §3.1.1)
            // 2  = PhaseId.AI (Deterministic Simulation #16 §3.x)
            // tier 2 = Tier C (Event System #17 §3.1.3)
            EventRegistry.RegisterExternalRow<PerceptionRefreshEvent>(
                ordinal: 0x10, tier: 2, version: 1,
                subsystemOrdinal: 40, maxPerTick: 5, producerPhaseIndex: 2);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
