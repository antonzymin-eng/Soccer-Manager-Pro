// File:     src/shot-mechanics/EventBusRegistrar.cs
// Created:  2026-05-30
// Modified: 2026-06-13
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
        // Idempotency guard: RegisterExternalRow throws ERR_EVT_ORDINAL_COLLISION on a re-register,
        // so a second Initialize() (multiple integration fixtures / repeated boot) would throw.
        // Parallels the Pass Mechanics + Decision Tree (AR-2 M-11) registrar guards. Stage 0 is
        // single-threaded so no locking is required.
        private static bool s_registered = false;

        /// <summary>
        /// Registers ShotExecutedEvent (0x01), ShotCancelledEvent (0x0E),
        /// and ShotAnimationData (0x0F) with EventRegistry. Idempotent — safe to call more than once.
        /// </summary>
        public static void Initialize()
        {
            if (s_registered)
                return;

            // Tier A: ShotExecuted/ShotCancelled are authoritative events (digest-included).
            // maxPerTick: 0 is the unbounded-sentinel — Tier A drop predicate (>=maxPerTick)
            // never fires for 0, so authoritative shots are never dropped under load.
            EventRegistry.RegisterExternalRow<ShotExecutedEvent>(
                ordinal: 0x01, tier: (byte)DeterminismTier.TierA, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            EventRegistry.RegisterExternalRow<ShotCancelledEvent>(
                ordinal: 0x0E, tier: (byte)DeterminismTier.TierA, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 0,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            // Tier C: animation cue. maxPerTick=2 bounds the single-shot CONTACT emission
            // (one ShotAnimationData per CONTACT + one safety headroom slot per FR-EVT-043
            // drop-predicate semantics); a single ShotExecutor cannot publish more in one
            // tick because CONTACT is reached at most once per state-machine pass. Shot
            // Mechanics #6 §4.7.3.
            EventRegistry.RegisterExternalRow<ShotAnimationData>(
                ordinal: 0x0F, tier: (byte)DeterminismTier.TierC, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics, maxPerTick: 2,
                producerPhaseIndex: (byte)PhaseId.Resolve);

            s_registered = true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                               |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: replaced raw int literals with SubsystemOrdinals.ShotMechanics |
// |         |            |        | and (byte)PhaseId.Resolve — prevents silent mismatch on enum reorder.    |
// | 1.2     | 2026-06-01 | —      | AR-2 (Shot Mechanics) M-1: tier ordinals 0/0/2 → (byte)DeterminismTier.* |
// |         |            |        | named consts. M-2: maxPerTick=2 for ShotAnimationData now cited to      |
// |         |            |        | #6 §4.7.3 with rationale comment. L-1: aligned narrative with code.     |
// |         |            |        | Added DeterministicSim asmdef reference to shot-mechanics.asmdef.       |
// | 1.3     | 2026-06-01 | —      | AR-3 L-1: document Tier A maxPerTick=0 unbounded-sentinel semantics.    |
// | 1.4     | 2026-06-13 | —      | AR M-1: added s_registered idempotency guard — a second Initialize()    |
// |         |            |        | (multiple integration fixtures / repeated boot) previously threw       |
// |         |            |        | ERR_EVT_ORDINAL_COLLISION on re-register. Parallels Pass Mechanics +    |
// |         |            |        | Decision Tree AR-2 M-11 registrar guards (DT flagged this for the shot  |
// |         |            |        | registrar). No change to registered rows.                              |
#endregion
