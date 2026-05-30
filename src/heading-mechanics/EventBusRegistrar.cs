// File:     src/heading-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Heading Mechanics #10 §4.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Heading Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.EventSystem;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Registers Heading Mechanics event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Heading Mechanics #10 §4.3 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        /// <summary>
        /// Registers HeaderExecutedEvent (0x12, Tier B) and HeaderAttemptFailedEvent (0x13, Tier C).
        /// </summary>
        public static void Initialize()
        {
            // 6 = SubsystemOrdinals.HeadingMechanics (Deterministic Simulation #16 §3.1.1)
            // 3 = PhaseId.Physics (Deterministic Simulation #16 §3.x)
            // tier 1 = Tier B; tier 2 = Tier C (Event System #17 §3.1.3)
            EventRegistry.RegisterExternalRow<HeaderExecutedEvent>(
                ordinal: 0x12, tier: 1, version: 1,
                subsystemOrdinal: 6, maxPerTick: 0, producerPhaseIndex: 3);

            EventRegistry.RegisterExternalRow<HeaderAttemptFailedEvent>(
                ordinal: 0x13, tier: 2, version: 1,
                subsystemOrdinal: 6, maxPerTick: 4, producerPhaseIndex: 3);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
