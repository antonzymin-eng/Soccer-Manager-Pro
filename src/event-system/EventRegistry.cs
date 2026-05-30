// File:     src/event-system/EventRegistry.cs
// Created:  2026-05-30
// Modified: 2026-05-30  (v1.1 — Stage 1 placeholder rows added)
// Author:   —
// Spec:     Event System #17 §2.4.2, Appendix A, Code Standards #20
// Purpose:  Compile-time Appendix A registry. Maps event type ordinals to tier, version,
//           subsystem ordinal, maxPerTick, struct size, and producer phase index.
//           EventOrdinalCache<T> provides O(1) type→ordinal lookup with no hot-path allocation.

using System;
using System.Runtime.CompilerServices;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Appendix A registry — canonical mapping from <c>eventTypeOrdinal</c> to tier, version,
    /// subsystem ordinal, maxPerTick, struct size, and producer phase.
    /// All fields are read-only after type initialization (static constructor).
    /// Event System #17 §2.4.2 / Appendix A.
    /// </summary>
    public static class EventRegistry
    {
        // ── Registry row schema ──────────────────────────────────────────────────────

        internal struct RegistryRow
        {
            internal byte  Ordinal;
            internal byte  Tier;            // 0=A, 1=B, 2=C  (DeterminismTier ordinal)
            internal byte  Version;
            internal ushort SubsystemOrdinal;
            internal ushort MaxPerTick;     // Tier C only; 0 for Tier A/B
            internal byte  ProducerPhaseIndex;
            internal int   StructSize;      // sizeof(T) stored at registration
            internal bool  IsRegistered;
        }

        private static readonly RegistryRow[] s_rows = new RegistryRow[256];

        static EventRegistry()
        {
            // Appendix A — 11 seeded rows (Spec #17 v1.0)
            // ordinals 0x01–0x08: Tier A; 0x09–0x0B: Tier C
            // ShotExecutedEvent (0x01) is owned by #6; its struct is defined in shot-mechanics/.
            // BallContactEvent (0x02) and BallCrossedLineEvent (0x03) are owned by #1/#3.
            // These three are registered here for registry completeness; their struct sizes
            // are set to 0 (updated when those assemblies call RegisterRow at Stage 1+).
            RegisterRowRaw(0x01, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            RegisterRowRaw(0x02, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.CollisionSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
            RegisterRowRaw(0x03, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.BallPhysics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);

            // #17-owned events:
            RegisterRow<PossessionChangedEvent>(0x04, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve);
            RegisterRow<FoulCommittedEvent>(0x05, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve);
            RegisterRow<CardIssuedEvent>(0x06, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve);
            RegisterRow<GoalAwardedEvent>(0x07, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve);
            RegisterRow<SubstitutionEvent>(0x08, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve);

            // Tier C events:
            RegisterRow<TickHeartbeatEvent>(0x09, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 1, producerPhaseIndex: (byte)PhaseId.Snapshot);
            RegisterRow<VfxImpactCue>(0x0A, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 64, producerPhaseIndex: (byte)PhaseId.Resolve);
            RegisterRow<UiNotificationCue>(0x0B, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.EventSystem,
                maxPerTick: 32, producerPhaseIndex: (byte)PhaseId.Resolve);

            // ── Stage 1 placeholder rows — structSize=0; updated by owning spec's
            //    EventBusRegistrar.Initialize() call before first publish. FR-EVT-003.
            // Pass Mechanics #5 Tier A events:
            RegisterRowRaw(0x0C, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.PassMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            RegisterRowRaw(0x0D, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.PassMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            // Shot Mechanics #6 Tier A/C events:
            RegisterRowRaw(0x0E, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            RegisterRowRaw(0x0F, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.ShotMechanics,
                maxPerTick: 2, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            // Perception System #7 Tier C event:
            RegisterRowRaw(0x10, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.PerceptionSystem,
                maxPerTick: 5, producerPhaseIndex: (byte)PhaseId.AI, structSize: 0);
            // Decision Tree #8 Tier C event:
            RegisterRowRaw(0x11, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.DecisionTree,
                maxPerTick: 22, producerPhaseIndex: (byte)PhaseId.AI, structSize: 0);
            // Heading Mechanics #10 Tier B / Tier C events:
            RegisterRowRaw(0x12, tier: 1, version: 1,
                subsystemOrdinal: SubsystemOrdinals.HeadingMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
            RegisterRowRaw(0x13, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.HeadingMechanics,
                maxPerTick: 4, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
            // Goalkeeper Mechanics #11 Tier A / Tier C events:
            RegisterRowRaw(0x14, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
            RegisterRowRaw(0x15, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
            RegisterRowRaw(0x16, tier: 0, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics,
                maxPerTick: 0, producerPhaseIndex: (byte)PhaseId.Resolve, structSize: 0);
            RegisterRowRaw(0x17, tier: 2, version: 1,
                subsystemOrdinal: SubsystemOrdinals.GoalkeeperMechanics,
                maxPerTick: 8, producerPhaseIndex: (byte)PhaseId.Physics, structSize: 0);
        }

        private static void RegisterRow<T>(byte ordinal, byte tier, byte version,
            int subsystemOrdinal, ushort maxPerTick, byte producerPhaseIndex)
            where T : struct
        {
            int structSize = Unsafe.SizeOf<T>();
            RegisterRowRaw(ordinal, tier, version, subsystemOrdinal,
                maxPerTick, producerPhaseIndex, structSize);
            EventOrdinalCache<T>.Ordinal = ordinal;
        }

        private static void RegisterRowRaw(byte ordinal, byte tier, byte version,
            int subsystemOrdinal, ushort maxPerTick, byte producerPhaseIndex, int structSize)
        {
            s_rows[ordinal] = new RegistryRow
            {
                Ordinal           = ordinal,
                Tier              = tier,
                Version           = version,
                SubsystemOrdinal  = (ushort)subsystemOrdinal,
                MaxPerTick        = maxPerTick,
                ProducerPhaseIndex = producerPhaseIndex,
                StructSize        = structSize,
                IsRegistered      = true,
            };
        }

        // ── Lookup helpers ───────────────────────────────────────────────────────────

        /// <summary>O(1) ordinal lookup for type T. Returns 0x00 (invalid sentinel) if T is not
        /// registered; callers should treat 0x00 as a registry error.</summary>
        internal static byte GetOrdinal<T>() where T : struct => EventOrdinalCache<T>.Ordinal;

        /// <summary>Returns the tier byte (0=A, 1=B, 2=C) for the given ordinal. §2.4.2.</summary>
        internal static byte GetTier(byte ordinal) => s_rows[ordinal].Tier;

        /// <summary>Returns the current payload version for the given ordinal. §3.7 / KD-9.</summary>
        internal static byte GetVersion(byte ordinal) => s_rows[ordinal].Version;

        /// <summary>Returns the subsystem ordinal for the given ordinal. §3.1.1 / FM-017-002.</summary>
        internal static ushort GetSubsystemOrdinal(byte ordinal) => s_rows[ordinal].SubsystemOrdinal;

        /// <summary>Returns maxPerTick for Tier C ordinals; 0 for Tier A/B. §3.5.3 / §3.6.2.</summary>
        internal static ushort GetMaxPerTick(byte ordinal) => s_rows[ordinal].MaxPerTick;

        /// <summary>Returns the producer phase index for the given ordinal. FM-017-002 component 1.</summary>
        internal static byte GetProducerPhaseIndex(byte ordinal) => s_rows[ordinal].ProducerPhaseIndex;

        /// <summary>Returns sizeof(T) for the event struct registered at ordinal. §3.5.1.</summary>
        internal static int GetStructSize(byte ordinal) => s_rows[ordinal].StructSize;

        /// <summary>Returns true if the ordinal has a registered row in Appendix A. FR-EVT-080.</summary>
        public static bool IsRegistered(byte ordinal) => s_rows[ordinal].IsRegistered;

        // ── External registration (Stage 1+ downstream specs) ────────────────────────

        /// <summary>
        /// Registers an event type defined in a downstream spec assembly (e.g., #10 HeaderExecutedEvent).
        /// Called from the owning spec assembly's static initializer at their IN REVIEW commit.
        /// Overwrites any placeholder row for the same ordinal. FR-EVT-003 / §3.7.4.
        /// </summary>
        public static void RegisterExternalRow<T>(byte ordinal, byte tier, byte version,
            int subsystemOrdinal, ushort maxPerTick, byte producerPhaseIndex)
            where T : struct
        {
            RegisterRow<T>(ordinal, tier, version, subsystemOrdinal, maxPerTick, producerPhaseIndex);
        }
    }

    // ── O(1) type→ordinal cache (generic static field pattern) ───────────────────────

    /// <summary>
    /// Per-type generic cache for event type ordinal. Populated by EventRegistry static
    /// constructor. O(1) access after initialization with no hot-path allocation.
    /// Ordinal is 0x00 (invalid sentinel) until the type is registered.
    /// </summary>
    internal static class EventOrdinalCache<T> where T : struct
    {
        internal static byte Ordinal;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-30 | —      | Stage 1: added placeholder rows 0x0C–0x17 for downstream spec      |
// |         |            |        | events. structSize=0; updated by EventBusRegistrar.Initialize().  |
// | 1.2     | 2026-05-30 | —      | AR-2 fix: GoalkeeperRushEvent placeholder (0x17) maxPerTick 2→8.  |
// |         |            |        | InFlight fires each physics frame at 60Hz; a rush completing       |
// |         |            |        | within one 100ms tick = Launched + InFlight×≤5 + terminal = ≤7.  |
#endregion
