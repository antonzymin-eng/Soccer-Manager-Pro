// File:     src/goalkeeper-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-06-14
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
        // Idempotency guard: RegisterExternalRow throws ERR_EVT_ORDINAL_COLLISION on a row that is
        // already initialised, so a second Initialize() (e.g. a test re-boot, or two systems both
        // booting the registry) would throw. Guard makes the boot a no-op after the first call,
        // matching the Pass Mechanics v1.2 / Decision Tree AR-2 M-11 sibling-registrar precedent.
        private static bool s_registered;

        /// <summary>
        /// Registers SaveAttemptedEvent (0x14), BallClaimedEvent (0x15),
        /// DistributionExecutedEvent (0x16), and GoalkeeperRushEvent (0x17).
        /// Idempotent: subsequent calls after the first are no-ops.
        /// </summary>
        public static void Initialize()
        {
            if (s_registered)
            {
                return;
            }
            s_registered = true;

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
// | 1.2     | 2026-06-14 | —      | AR-5: s_registered idempotency guard so a double Initialize() is a no-op  |
// |         |            |        | instead of throwing ERR_EVT_ORDINAL_COLLISION (DT AR-2 M-11 sibling-      |
// |         |            |        | registrar follow-up; PM v1.2 precedent).                                  |
#endregion
