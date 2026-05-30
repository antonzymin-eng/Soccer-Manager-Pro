// File:     src/event-system/EventBus.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.2.1, §3.2.2, §4.4, Code Standards #20
// Purpose:  Public static event bus. Publish/Subscribe entry points plus DrainTick,
//           SerializeLedger, OnTickBoundary, BeginPhase, and BeginTick lifecycle hooks.
//           Static mutable singleton is spec-mandated (§3.2.1 KD-4/KD-8); an explicit
//           exception to CLAUDE.md "Banned Architectural Patterns" §4.
//           Delegates ring-buffer work to EventLedger (internal) and Tier C work to
//           CosmeticChannel (public). All hot-path methods allocate 0 bytes (FR-EVT-048/049/050).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine.Profiling;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Public event bus. Single static entry point for publish, subscribe, and tick-lifecycle
    /// integration. Delegates internal storage to EventLedger and Tier C to CosmeticChannel.
    /// Event System #17 §3.2.1 / §4.4.
    /// </summary>
    public static class EventBus
    {
        private static readonly ProfilerMarker s_drainTickMarker =
            new ProfilerMarker("EventSystem.DrainTick");
        private static readonly ProfilerMarker s_serializeLedgerMarker =
            new ProfilerMarker("EventSystem.SerializeLedger");

        // ── Tick lifecycle ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by TickOrchestrator at the start of each tick before any phase runs.
        /// Stores the current tick value for embedding in event headers.
        /// </summary>
        public static void BeginTick(uint tick)
        {
            EventLedger.CurrentTick = tick;
        }

        /// <summary>
        /// Called by the tick scheduler at each phase entry.
        /// Resets the per-phase <c>intraPhaseDrawIndex</c> counter to zero (§3.2.4 / §4.4.4).
        /// </summary>
        public static void BeginPhase(PhaseId phase)
        {
            EventLedger.BeginPhase(phase);
        }

        /// <summary>
        /// Called at Events-phase entry. Sorts the accumulated tick queue once (FM-017-002)
        /// then dispatches all Tier A/B records to subscribers in canonical order (§4.4.1).
        /// Handles second-order BFS re-entrant publishes up to <c>MaxEventDispatchDepth</c>.
        /// Allocates 0 bytes (FR-EVT-049); uses stackalloc scratch for sort indices.
        /// </summary>
        public static void DrainTick()
        {
            using var _ = s_drainTickMarker.Auto();
            EventLedger.DrainTick();
        }

        /// <summary>
        /// Called at Snapshot phase. Writes EventLedgerRecord bytes into <paramref name="dst"/>
        /// (domain tag + count + Tier A/B records in FM-017-002 order). Returns bytes written.
        /// Allocates 0 bytes (FR-EVT-050). §4.4.2 / FM-017-001.
        /// </summary>
        public static int SerializeLedger(Span<byte> dst)
        {
            using var _ = s_serializeLedgerMarker.Auto();
            return EventLedger.SerializeLedger(dst);
        }

        /// <summary>
        /// Called at end of Snapshot phase (after SerializeLedger).
        /// Resets per-tick state: queue pointers, intraPhaseDrawIndex counters, and the
        /// Tier C publication-count table (FR-EVT-025 / §4.4.3).
        /// </summary>
        public static void OnTickBoundary()
        {
            EventLedger.OnTickBoundary();
            CosmeticChannel.ResetPublicationCounts();
        }

        // ── Publish (Tier A) ─────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues a Tier A event into the ring buffer for delivery during this tick's
        /// Events phase. Debug builds assert the current phase is a valid Tier A producer phase.
        /// Allocates 0 bytes (FR-EVT-048). §3.2.1 / §3.2.3.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct, IEventA
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug-only producer-phase assertion (§3.2.1): verify the call comes from the
            // registered producer phase. Stripped from release builds (FR-EVT-048 zero-alloc).
            byte dbgOrdinal = EventOrdinalCache<T>.Ordinal;
            UnityEngine.Debug.Assert(
                (byte)EventLedger.CurrentPhase == EventRegistry.GetProducerPhaseIndex(dbgOrdinal),
                "EventBus.Publish<T>: Tier A event published from incorrect producer phase.");
#endif
            PublishAuthoritative(in evt);
        }

        /// <summary>
        /// Enqueues a Tier B event into the ring buffer. Behaves identically to Tier A
        /// at Stage 0; Tier B tolerance-path activation is Stage 5+ (§3.1.3 / KD-3).
        /// Allocates 0 bytes (FR-EVT-048). §3.2.1.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct, IEventB
        {
            PublishAuthoritative(in evt);
        }

        /// <summary>
        /// Immediately dispatches a Tier C event via CosmeticChannel (no queue, no digest).
        /// Applies the deterministic drop predicate (§3.6.2). Allocates 0 bytes (FR-EVT-048).
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct, IEventC
        {
            CosmeticChannel.Publish(in evt);
        }

        // ── Subscribe (Tier A) ────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a Tier A subscriber. MUST be called before the first Events phase
        /// (boot phase; FR-EVT-020). Raises ERR_EVT_REGISTRATION_PHASE if called after boot.
        /// Returns an opaque <see cref="SubscriptionToken"/> (no class allocation; FR-EVT-073).
        /// §3.2.2 / §4.3.1.
        /// </summary>
        public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
            where T : struct, IEventA
        {
            EnforceBootPhase();
            byte ordinal = EventOrdinalCache<T>.Ordinal;
            if (ordinal == 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): " + typeof(T).Name +
                    " subscribed before EventBusRegistrar.Initialize() — ordinal cache is 0. " +
                    "Call the owning spec's EventBusRegistrar.Initialize() during boot phase (FR-EVT-020).");
            return EventLedger.Subscribe<T>(handler, ordinal,
                EventSystemConstants.MaxHandlersPerEventType);
        }

        /// <summary>
        /// Registers a Tier B subscriber. Boot-phase restriction applies (FR-EVT-020).
        /// No Tier B events are registered at Stage 0; declared for Stage 5+ consumers.
        /// §3.2.2.
        /// </summary>
        public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
            where T : struct, IEventB
        {
            EnforceBootPhase();
            byte ordinal = EventOrdinalCache<T>.Ordinal;
            if (ordinal == 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): " + typeof(T).Name +
                    " subscribed before EventBusRegistrar.Initialize() — ordinal cache is 0. " +
                    "Call the owning spec's EventBusRegistrar.Initialize() during boot phase (FR-EVT-020).");
            return EventLedger.Subscribe<T>(handler, ordinal,
                EventSystemConstants.MaxHandlersPerEventType);
        }

        /// <summary>
        /// Registers a Tier C subscriber. Delegates to CosmeticChannel.Subscribe.
        /// Permitted at any time during match (FR-EVT-022). §3.2.2 / §4.3.2.
        /// </summary>
        public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
            where T : struct, IEventC
        {
            return CosmeticChannel.Subscribe(handler);
        }

        // ── Internal publish helper ───────────────────────────────────────────────────

        private static void PublishAuthoritative<T>(in T evt) where T : struct
        {
            byte ordinal = EventOrdinalCache<T>.Ordinal;

            // Zero-ordinal guard (AR-2 fix: replaces debug-only Debug.Assert to eliminate eager string
            // allocation on the hot path — C# evaluates Assert arguments eagerly even when the condition
            // is true. Unconditional throw catches the error in release builds too. FR-EVT-020).
            if (ordinal == 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): " + typeof(T).Name +
                    " published before EventBusRegistrar.Initialize() — ordinal cache is 0. " +
                    "Call the owning spec's EventBusRegistrar.Initialize() during boot phase (FR-EVT-020).");

            if (EventLedger.QueueCount >= EventSystemConstants.EventQueueCapacity)
                throw new InvalidOperationException(
                    "ERR_EVT_QUEUE_OVERFLOW (0x1701): ring buffer full. " +
                    "EventQueueCapacity=" + EventSystemConstants.EventQueueCapacity);

            // Out-degree cap (FR-EVT-046a): during BFS dispatch, a handler may publish
            // at most one secondary Tier A/B event per invocation.
            if (EventLedger.InDrainDispatch)
            {
                EventLedger.HandlerSecondaryPublishCount++;
                if (EventLedger.HandlerSecondaryPublishCount > 1)
                    throw new InvalidOperationException(
                        "ERR_EVT_QUEUE_OVERFLOW (0x1701): per-handler Tier A/B out-degree > 1 (FR-EVT-046a).");
            }

            int slotIndex  = EventLedger.QueueCount++;
            int slotOffset = slotIndex * EventSystemConstants.MaxEventSlotBytes;

            // Copy struct bytes to ring buffer slot (no GC allocation — value copy on stack).
            T copy = evt;
            int structSize = EventRegistry.GetStructSize(ordinal);
            if (structSize <= 0)
            {
                // Fallback: estimate via Unsafe.SizeOf<T>() for types registered via RegisterRowRaw.
                structSize = Unsafe.SizeOf<T>();
            }

            MemoryMarshal.Write(
                new Span<byte>(EventLedger.PayloadBuffer, slotOffset, structSize),
                ref copy);

            // Overwrite header fields in ring buffer (FR-EVT-002: EventBus sets header bytes).
            byte phaseIdx = (byte)EventLedger.CurrentPhase;
            ushort drawIdx = EventLedger.PhaseDrawIndices[phaseIdx]++;
            ushort subsysOrd = EventRegistry.GetSubsystemOrdinal(ordinal);
            uint tick = EventLedger.CurrentTick;

            byte[] buf = EventLedger.PayloadBuffer;
            buf[slotOffset]      = ordinal;
            buf[slotOffset + 1]  = EventRegistry.GetVersion(ordinal);
            buf[slotOffset + 2]  = 0; // _reserved (canonical zero)
            buf[slotOffset + 3]  = 0; // _reserved
            buf[slotOffset + 4]  = (byte)tick;
            buf[slotOffset + 5]  = (byte)(tick >> 8);
            buf[slotOffset + 6]  = (byte)(tick >> 16);
            buf[slotOffset + 7]  = (byte)(tick >> 24);
            buf[slotOffset + 8]  = (byte)subsysOrd;
            buf[slotOffset + 9]  = (byte)(subsysOrd >> 8);
            buf[slotOffset + 10] = (byte)drawIdx;
            buf[slotOffset + 11] = (byte)(drawIdx >> 8);

            // Store sort key metadata for DrainTick.
            EventLedger.SlotMeta[slotIndex] = new EventSlotMeta
            {
                ProducingPhaseIndex  = phaseIdx,
                SubsystemOrdinal     = subsysOrd,
                EntityId             = 0, // Stage 0: entity resolution is Stage 1 (§3.2.4)
                EventTypeOrdinal     = ordinal,
                IntraPhaseDrawIndex  = drawIdx,
                StructSize           = structSize,
            };
        }

        // ── Lifecycle guard ───────────────────────────────────────────────────────────

        private static void EnforceBootPhase()
        {
            if (EventLedger.BootPhaseComplete)
                throw new InvalidOperationException(
                    "ERR_EVT_REGISTRATION_PHASE (0x1705): Tier A/B subscriber registered " +
                    "after boot phase ended. Register before first DrainTick call (FR-EVT-020/021).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-30 | —      | AR-1 M-2: added #if UNITY_EDITOR||DEVELOPMENT_BUILD phase assertion  |
// |         |            |        | to Publish<T>(IEventA) as documented in the method XML doc.          |
// | 1.2     | 2026-05-30 | —      | AR-1 fix: added zero-ordinal guard in PublishAuthoritative (debug    |
// |         |            |        | builds) — catches Tier A/B publish before EventBusRegistrar.Init().  |
// | 1.3     | 2026-05-30 | —      | AR-2 fix: zero-ordinal guard in PublishAuthoritative promoted to      |
// |         |            |        | unconditional if/throw (eliminates eager string alloc on hot path,    |
// |         |            |        | FR-EVT-048). Same guard added to Subscribe<IEventA/B> (FR-EVT-020).  |
#endregion
