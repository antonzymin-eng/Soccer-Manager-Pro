// File:     src/decision-tree/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Decision Tree #8 §4.5, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Decision Tree event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.EventSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Registers Decision Tree event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Decision Tree #8 §4.5 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        /// <summary>
        /// Registers DecisionMadeEvent (0x11) with EventRegistry.
        /// </summary>
        public static void Initialize()
        {
            // 41 = SubsystemOrdinals.DecisionTree (Deterministic Simulation #16 §3.1.1)
            // 2  = PhaseId.AI (Deterministic Simulation #16 §3.x)
            // tier 2 = Tier C (Event System #17 §3.1.3)
            // maxPerTick=22: one per active agent per 10 Hz tick (22-agent squad ceiling)
            EventRegistry.RegisterExternalRow<DecisionMadeEvent>(
                ordinal: 0x11, tier: 2, version: 1,
                subsystemOrdinal: 41, maxPerTick: 22, producerPhaseIndex: 2);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
