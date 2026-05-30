// File:     src/goalkeeper-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Goalkeeper Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Registers Goalkeeper Mechanics event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Goalkeeper Mechanics #11 §4.3 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        /// <summary>
        /// Registers SaveAttemptedEvent (0x14), BallClaimedEvent (0x15),
        /// DistributionExecutedEvent (0x16), and GoalkeeperRushEvent (0x17).
        /// </summary>
        public static void Initialize()
        {
            // 7 = SubsystemOrdinals.GoalkeeperMechanics (Deterministic Simulation #16 §3.1.1)
            // 3 = PhaseId.Physics; 4 = PhaseId.Resolve (Deterministic Simulation #16 §3.x)
            // tier 0 = Tier A; tier 2 = Tier C (Event System #17 §3.1.3)
            EventRegistry.RegisterExternalRow<SaveAttemptedEvent>(
                ordinal: 0x14, tier: 0, version: 1,
                subsystemOrdinal: 7, maxPerTick: 0, producerPhaseIndex: 3);

            EventRegistry.RegisterExternalRow<BallClaimedEvent>(
                ordinal: 0x15, tier: 0, version: 1,
                subsystemOrdinal: 7, maxPerTick: 0, producerPhaseIndex: 3);

            EventRegistry.RegisterExternalRow<DistributionExecutedEvent>(
                ordinal: 0x16, tier: 0, version: 1,
                subsystemOrdinal: 7, maxPerTick: 0, producerPhaseIndex: 4);

            // maxPerTick=2: rush can fire Launched + Reached/Aborted in adjacent frames
            EventRegistry.RegisterExternalRow<GoalkeeperRushEvent>(
                ordinal: 0x17, tier: 2, version: 1,
                subsystemOrdinal: 7, maxPerTick: 2, producerPhaseIndex: 3);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
