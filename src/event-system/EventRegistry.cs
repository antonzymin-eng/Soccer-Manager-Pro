// File:     src/event-system/EventRegistry.cs
// Created:  2026-05-30
// Modified: 2026-06-15  (v1.6 — AR-12 M-1 error-code message prefixes)
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

            // AR-8 L-1: Tier A/B structs must embed the canonical 12-byte header (§2.4.1).
            // A struct smaller than EventHeaderBytes would let EventBus.PublishAuthoritative
            // overwrite the header into bytes past the struct's footprint in the slot region
            // (still inside MaxEventSlotBytes), then SerializeLedger would copy only the
            // truncated structSize back — corrupting the canonical record bytes and the
            // FR-DS-009 digest. Tier C is exempt (no header / not in digest).
            if ((tier == (byte)DeterminismTier.TierA || tier == (byte)DeterminismTier.TierB)
                && structSize < EventSystemConstants.EventHeaderBytes)
                throw new ArgumentException(
                    EventSystemConstants.ErrPrefixUnregisteredOrdinal + ": Tier A/B struct " + typeof(T).Name +
                    " sizeof " + structSize + " bytes is smaller than EventHeaderBytes " +
                    EventSystemConstants.EventHeaderBytes + " — Tier A/B structs MUST embed the " +
                    "canonical 12-byte header per §2.4.1.", nameof(T));

            RegisterRowRaw(ordinal, tier, version, subsystemOrdinal,
                maxPerTick, producerPhaseIndex, structSize);
            EventOrdinalCache<T>.Ordinal = ordinal;
        }

        private static void RegisterRowRaw(byte ordinal, byte tier, byte version,
            int subsystemOrdinal, ushort maxPerTick, byte producerPhaseIndex, int structSize)
        {
            // AR-8 M-1: detect collision against a fully-initialised row (structSize > 0).
            // Placeholder rows seeded with structSize=0 (Stage 1 RegisterExternalRow targets)
            // are intentionally overwritable. A non-placeholder row being overwritten means
            // two RegisterExternalRow calls claim the same ordinal — silently redirecting
            // dispatch metadata to the wrong struct/phase/tier. The Subscribe-time guard in
            // EventLedger / CosmeticChannel only fires after a subscriber happens to attach,
            // so registration-time collisions could go undetected for an entire boot.
            if (s_rows[ordinal].IsRegistered && s_rows[ordinal].StructSize > 0)
                throw new InvalidOperationException(
                    EventSystemConstants.ErrPrefixOrdinalCollision + ": ordinal 0x" + ordinal.ToString("X2") +
                    " is already fully registered (subsystem 0x" +
                    s_rows[ordinal].SubsystemOrdinal.ToString("X4") + ", tier " +
                    s_rows[ordinal].Tier + ", structSize " + s_rows[ordinal].StructSize +
                    "). Incoming registration: subsystem 0x" +
                    ((ushort)subsystemOrdinal).ToString("X4") + ", tier " + tier +
                    ", structSize " + structSize + ". Check for duplicate ordinal in " +
                    "RegisterExternalRow calls.");

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
        /// <summary>
        /// Forces the EventRegistry static constructor (Appendix A seeded rows) to run.
        /// EventOrdinalCache&lt;T&gt; is a separate static-generic type, so reading
        /// EventOrdinalCache&lt;T&gt;.Ordinal does NOT trigger this type's initializer —
        /// a Subscribe/Publish of a #17-owned seeded event before anything else touched
        /// EventRegistry saw ordinal 0 and threw ERR_EVT_UNREGISTERED_ORDINAL
        /// (initialization-order fragility surfaced by the first-ever suite execution on
        /// the dotnet CI gate; Unity's runner exhibits the identical order). EventBus
        /// entry points call this before consulting the ordinal cache. The body is empty:
        /// invoking any static member runs the cctor exactly once; afterwards the call is
        /// an inlined no-op (FR-EVT-048 zero-alloc unaffected).
        /// </summary>
        internal static void EnsureInitialized()
        {
        }

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

        /// <summary>
        /// Returns true if the ordinal has a fully initialised registry row: row exists AND struct
        /// size is known (i.e., the owning spec's EventBusRegistrar.Initialize() has been called).
        /// Returns false for placeholder rows seeded via RegisterRowRaw with structSize=0 (FR-EVT-003).
        /// Do NOT use as a pre-condition for Subscribe&lt;T&gt; without also calling Initialize() first.
        /// FR-EVT-080.
        /// </summary>
        public static bool IsRegistered(byte ordinal) =>
            s_rows[ordinal].IsRegistered && s_rows[ordinal].StructSize > 0;

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
// | 1.3     | 2026-05-30 | —      | AR-3 fix: IsRegistered now requires StructSize > 0 so placeholder  |
// |         |            |        | rows (RegisterRowRaw structSize=0) return false until Initialize() |
// |         |            |        | is called — prevents IsRegistered from being a misleading boot-   |
// |         |            |        | readiness predicate that contradicts the Subscribe<T> ordinal guard.|
// | 1.4     | 2026-06-07 | —      | AR-8 M-1: RegisterRowRaw now throws ERR_EVT_ORDINAL_COLLISION       |
// |         |            |        | (0x1707) when targeting an already-fully-initialised row (existing  |
// |         |            |        | StructSize > 0). Placeholder rows (structSize=0) remain overwrit-  |
// |         |            |        | able per FR-EVT-003. Closes the registration-time collision path   |
// |         |            |        | that the Subscribe-time cast guard could not detect until the      |
// |         |            |        | first subscriber attached.                                         |
// |         |            |        | AR-8 L-1: RegisterRow<T> asserts sizeof(T) >= EventHeaderBytes (12) |
// |         |            |        | for Tier A/B types per §2.4.1 canonical header layout. Tier C is   |
// |         |            |        | exempt (immediate-dispatch; excluded from canonical digest).        |
// | 1.5     | 2026-06-12 | —      | Dotnet CI gate fix (first-ever suite execution): new internal no-op |
// |         |            |        | EnsureInitialized() - EventOrdinalCache<T> is a separate            |
// |         |            |        | static-generic type, so reading its Ordinal does not trigger this   |
// |         |            |        | type's seeded-row cctor; a Subscribe/Publish of a #17-owned event   |
// |         |            |        | before anything touched EventRegistry threw                         |
// |         |            |        | ERR_EVT_UNREGISTERED_ORDINAL. EventBus entry points now force the   |
// |         |            |        | cctor.                                                              |
// | 1.6     | 2026-06-15 | —      | AR-12 M-1: throw-site hex literals (0x1706 / 0x1707) replaced with   |
// |         |            |        | EventSystemConstants.ErrPrefix* strings (codes single source of      |
// |         |            |        | truth; rendered text byte-identical). No functional change.         |
#endregion
