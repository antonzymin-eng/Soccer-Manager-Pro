// File:     src/goalkeeper-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Goalkeeper Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.DeterministicSim;
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
            EventRegistry.RegisterExternalRow<SaveAttemptedEvent>(
                ordinal: 0x14, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Physics);

            EventRegistry.RegisterExternalRow<BallClaimedEvent>(
                ordinal: 0x15, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Physics);

            EventRegistry.RegisterExternalRow<DistributionExecutedEvent>(
                ordinal: 0x16, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            // maxPerTick=8: Launched + InFlight (≤5 frames at 60Hz within one 100ms tick) + Reached/Aborted
            EventRegistry.RegisterExternalRow<GoalkeeperRushEvent>(
                ordinal: 0x17, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics, maxPerTick: 8,
                producerPhaseIndex: (byte)PhaseId.Physics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                   |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: replaced raw int literals with SubsystemOrdinals/PhaseId typed  |
// |         |            |        | constants; GoalkeeperRushEvent maxPerTick 2→8 (InFlight fires each        |
// |         |            |        | physics frame at 60Hz: Launched + ≤5 InFlight + terminal = ≤7).          |
#endregion
