// File:     src/heading-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-06-14
// Author:   —
// Spec:     Heading Mechanics #10 §4.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Heading Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.

using TacticalDirector.DeterministicSim;
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
        // Idempotency guard (AR-3 M-2): a second Initialize() would re-register ordinals
        // 0x12/0x13 and throw ERR_EVT_ORDINAL_COLLISION; integration/smoke tests boot the
        // registry per fixture. Matches the Pass Mechanics v1.2 / Decision Tree precedent.
        private static bool s_registered;

        /// <summary>
        /// Registers HeaderExecutedEvent (0x12, Tier B) and HeaderAttemptFailedEvent (0x13, Tier C).
        /// Idempotent: subsequent calls are no-ops.
        /// </summary>
        public static void Initialize()
        {
            if (s_registered)
            {
                return;
            }
            s_registered = true;

            EventRegistry.RegisterExternalRow<HeaderExecutedEvent>(
                ordinal: 0x12, tier: 1, version: 1,
                subsystemOrdinal: SubsystemOrdinals.HeadingMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Physics);

            EventRegistry.RegisterExternalRow<HeaderAttemptFailedEvent>(
                ordinal: 0x13, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.HeadingMechanics, maxPerTick: 4,
                producerPhaseIndex: (byte)PhaseId.Physics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                  |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: replaced raw int literals with SubsystemOrdinals.HeadingMechanics |
// |         |            |        | and (byte)PhaseId.Physics — prevents silent mismatch on enum reorder.    |
// | 1.2     | 2026-06-14 | —      | AR-3 M-2: s_registered idempotency guard — second Initialize() no-ops      |
// |         |            |        | instead of throwing ERR_EVT_ORDINAL_COLLISION (DT AR-2 M-11 follow-up).   |
#endregion
