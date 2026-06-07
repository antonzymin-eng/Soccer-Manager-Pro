// File:     src/pass-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-06-06
// Author:   —
// Spec:     Pass Mechanics #5 §4.6.3, Event System #17 §3.7.4, Code Standards #20
// Purpose:  Registers Pass Mechanics event types with EventRegistry at boot time.
//           Call Initialize() before the first EventBus.Publish call in the match lifecycle.
//           Updates struct-size entries that EventRegistry seeded with size=0 (FR-EVT-003).

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Registers Pass Mechanics event types with EventRegistry.
    /// Must be called during boot phase before first DrainTick (FR-EVT-020).
    /// Pass Mechanics #5 §4.6.3 / Event System #17 §3.7.4.
    /// </summary>
    public static class EventBusRegistrar
    {
        // Idempotency guard: a duplicate Initialize() call (replay setup, scene reload, or
        // editor domain reload) would otherwise throw ERR_EVT_ORDINAL_COLLISION (0x1707)
        // from EventRegistry. Default-false; flipped on first successful registration.
        private static bool s_registered;

        /// <summary>
        /// Registers PassAttemptEvent (0x0C) and PassCancelledEvent (0x0D) with EventRegistry.
        /// Updates struct sizes and sets EventOrdinalCache for zero-allocation hot-path lookups.
        /// Idempotent: subsequent calls are no-ops.
        /// </summary>
        public static void Initialize()
        {
            if (s_registered)
                return;

            EventRegistry.RegisterExternalRow<PassAttemptEvent>(
                ordinal: 0x0C, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.PassMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            EventRegistry.RegisterExternalRow<PassCancelledEvent>(
                ordinal: 0x0D, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.PassMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            s_registered = true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                 |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: replaced raw int literals with SubsystemOrdinals.PassMechanics |
// |         |            |        | and (byte)PhaseId.Resolve — prevents silent mismatch on enum reorder.   |
// | 1.2     | 2026-06-06 | —      | AR-2 M-5: idempotency guard s_registered added. Duplicate Initialize()  |
// |         |            |        |     calls (replay/scene reload/editor domain reload) no longer collide  |
// |         |            |        |     with the already-registered ordinals (ERR_EVT_ORDINAL_COLLISION).   |
// | 1.3     | 2026-06-06 | —      | AR-3 M-1/L-4: dropped the ResetForTesting seam — it was internal but    |
// |         |            |        |     unreachable (no InternalsVisibleTo on this assembly) and tests       |
// |         |            |        |     should mock at the EventRegistry boundary, not poke this guard.     |
#endregion
