// File:     src/shot-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Shot Mechanics #6 §4.7.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Shot Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Registers Shot Mechanics event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Shot Mechanics #6 §4.7.3 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        /// <summary>
        /// Registers ShotExecutedEvent (0x01), ShotCancelledEvent (0x0E),
        /// and ShotAnimationData (0x0F) with EventRegistry.
        /// </summary>
        public static void Initialize()
        {
            EventRegistry.RegisterExternalRow<ShotExecutedEvent>(
                ordinal: 0x01, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            EventRegistry.RegisterExternalRow<ShotCancelledEvent>(
                ordinal: 0x0E, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            EventRegistry.RegisterExternalRow<ShotAnimationData>(
                ordinal: 0x0F, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 2,
                producerPhaseIndex: (byte)PhaseId.Resolve);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                               |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: replaced raw int literals with SubsystemOrdinals.ShotMechanics |
// |         |            |        | and (byte)PhaseId.Resolve — prevents silent mismatch on enum reorder.    |
#endregion
