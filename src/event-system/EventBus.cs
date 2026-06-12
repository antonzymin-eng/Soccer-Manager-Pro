// File:     src/event-system/EventBus.cs
// Created:  2026-05-30
// Modified: 2026-06-07
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

using Unity.Profiling;

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

        // ── Publish (all tiers — single method, cached marker dispatch) ──────────────

        /// <summary>
        /// Publishes an event. Tier routing is resolved from the event's tier marker
        /// (IEventA / IEventB / IEventC) via <see cref="EventTierCache{T}"/> — C# forbids
        /// overloading on generic constraints alone (CS0111; ERR-017-002), so the §3.2.1
        /// three-overload surface is implemented as one method with cached-flag dispatch
        /// (boot-time type-init only; the JIT folds the flags to constants — zero
        /// steady-state cost). Tier A/B: enqueued for Events-phase delivery; debug builds
        /// assert the current phase is the registered producer phase. Tier C: immediate
        /// dispatch via CosmeticChannel with the deterministic drop predicate (§3.6.2).
        /// Throws if <typeparamref name="T"/> does not implement exactly one tier marker
        /// (FR-EVT-009a). Allocates 0 bytes (FR-EVT-048). §3.2.1 / §3.2.3.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
            EventRegistry.EnsureInitialized();

            if (!EventTierCache<T>.IsValid)
                ThrowTierContractViolation(typeof(T));

            if (EventTierCache<T>.IsTierC)
            {
                CosmeticChannel.Publish(in evt);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug-only producer-phase assertion (§3.2.1; AR-1 M-2 Tier A, AR-7 L-1 Tier B):
            // verify the call comes from the registered producer phase. Stripped from
            // release builds (FR-EVT-048 zero-alloc). Both message arms are constant
            // string literals — no allocation from the eager argument evaluation.
            byte dbgOrdinal = EventOrdinalCache<T>.Ordinal;
            UnityEngine.Debug.Assert(
                (byte)EventLedger.CurrentPhase == EventRegistry.GetProducerPhaseIndex(dbgOrdinal),
                EventTierCache<T>.IsTierB
                    ? "EventBus.Publish<T>: Tier B event published from incorrect producer phase."
                    : "EventBus.Publish<T>: Tier A event published from incorrect producer phase.");
#endif
            PublishAuthoritative(in evt);
        }

        // ── Subscribe (all tiers — single method, cached marker dispatch) ────────────

        /// <summary>
        /// Registers a subscriber. Tier routing as in <see cref="Publish{T}"/>
        /// (ERR-017-002 single-method dispatch). Tier A/B: MUST be called before the
        /// first Events phase (boot phase; FR-EVT-020) — raises ERR_EVT_REGISTRATION_PHASE
        /// after boot. No Tier B events are registered at Stage 0 (Stage 5+ consumers).
        /// Tier C: delegates to CosmeticChannel; permitted at any time during match
        /// (FR-EVT-022). Returns an opaque <see cref="SubscriptionToken"/> (no class
        /// allocation; FR-EVT-073). §3.2.2 / §4.3.1 / §4.3.2.
        /// </summary>
        public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
            where T : struct
        {
            EventRegistry.EnsureInitialized();

            if (!EventTierCache<T>.IsValid)
                ThrowTierContractViolation(typeof(T));

            if (EventTierCache<T>.IsTierC)
                return CosmeticChannel.SubscribeFromBus(handler);

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

        // ── Tier-contract guard ───────────────────────────────────────────────────────

        private static void ThrowTierContractViolation(Type eventType)
        {
            throw new InvalidOperationException(
                "EventBus tier contract violation (FR-EVT-009a / ERR-017-002): " +
                eventType.Name + " must implement exactly one of IEventA / IEventB / " +
                "IEventC to flow through EventBus.Publish/Subscribe.");
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

            // AR-5 M-1: structSize validation must precede QueueCount reservation. If a guard
            // throws after QueueCount++ has run, the slot is reserved but PayloadBuffer/SlotMeta
            // never populated; subsequent Publish writes to slotIndex+1 leaving slot N with
            // EventTypeOrdinal=0 (CLR default), which SerializeLedger classifies as Tier A and
            // emits as a zero-byte record, corrupting the canonical digest.
            int structSize = EventRegistry.GetStructSize(ordinal);

            // AR-4 fix: guard promoted from silent fallback to throw. The original
            // "Fallback via Unsafe.SizeOf<T> for RegisterRowRaw types" comment was
            // misleading — RegisterRowRaw types have ordinal=0 (EventOrdinalCache is never
            // set by RegisterRowRaw), so they are caught by the ordinal==0 guard above.
            // Any type reaching here with ordinal!=0 was registered via RegisterRow<T>,
            // which always stores Unsafe.SizeOf<T>() > 0. structSize<=0 is unreachable;
            // promoting to throw makes the invariant explicit (consistent with AR-3 in
            // CosmeticChannel.Publish).
            if (structSize <= 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): struct size is 0 for ordinal 0x" +
                    ordinal.ToString("X2") + ". Call EventBusRegistrar.Initialize() before publishing.");

            // AR-4 fix: upper-bound guard (symmetric with AR-3 fix in CosmeticChannel.Publish).
            // Without this guard, a struct > MaxEventSlotBytes bytes would overflow the ring-buffer
            // slot and corrupt adjacent slots silently (MemoryMarshal.Write's Span constructor only
            // throws when slotOffset+structSize > PayloadBuffer.Length, not when > MaxEventSlotBytes).
            if (structSize > EventSystemConstants.MaxEventSlotBytes)
                throw new InvalidOperationException(
                    "ERR_EVT_QUEUE_OVERFLOW (0x1701): Tier A/B struct size " + structSize +
                    " bytes exceeds MaxEventSlotBytes " + EventSystemConstants.MaxEventSlotBytes +
                    " for ordinal 0x" + ordinal.ToString("X2") +
                    ". Increase MaxEventSlotBytes or reduce struct size (§3.5.1).");

            // AR-10 M-1: phase validity guard must precede QueueCount reservation. The AR-8 M-2
            // sentinel CurrentPhase = (PhaseId)0xFF is the intended fail-fast trip for a stale
            // Publish between OnTickBoundary and BeginPhase, BUT the implicit IndexOutOfRangeException
            // at PhaseDrawIndices[0xFF] below (line ~263) fires AFTER QueueCount++ has run, leaving
            // the reserved slot un-populated. A release-build host that catches the IORE and
            // continues would then have subsequent Publish writes skip slot N (EventTypeOrdinal=0,
            // SerializeLedger classifies as Tier A zero-byte record, canonical digest corrupted) —
            // exactly the AR-5 M-1 hazard. Guard before reservation so the throw is recoverable
            // and parallel with the structSize / depth / overflow guards above.
            byte phaseIdxCheck = (byte)EventLedger.CurrentPhase;
            if (phaseIdxCheck >= EventLedger.PhaseDrawIndices.Length)
                throw new InvalidOperationException(
                    "ERR_EVT_QUEUE_OVERFLOW (0x1701): Publish<T> called with invalid CurrentPhase 0x" +
                    phaseIdxCheck.ToString("X2") + " (likely a stale publish between OnTickBoundary " +
                    "and the next BeginPhase). AR-8 M-2 sentinel — call BeginTick + BeginPhase first.");

            int slotIndex  = EventLedger.QueueCount++;
            int slotOffset = slotIndex * EventSystemConstants.MaxEventSlotBytes;

            // Copy struct bytes to ring buffer slot (no GC allocation — value copy on stack).
            T copy = evt;

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
// | 1.4     | 2026-05-31 | —      | AR-4 L-1: structSize<=0 fallback in PublishAuthoritative promoted     |
// |         |            |        | from silent Unsafe.SizeOf<T>() fallback to unconditional throw.      |
// |         |            |        | Fallback was dead code: ordinal!=0 guard guarantees structSize>0      |
// |         |            |        | (only RegisterRow<T> sets EventOrdinalCache, always with sizeof>0).   |
// |         |            |        | Misleading "RegisterRowRaw" comment removed; throw is consistent with  |
// |         |            |        | the AR-3 pattern applied to CosmeticChannel.Publish (FR-EVT-020).     |
// |         |            |        | AR-4 M-1: added upper-bound structSize > MaxEventSlotBytes guard to   |
// |         |            |        | PublishAuthoritative (Tier A/B path); AR-3 added the symmetric guard  |
// |         |            |        | to CosmeticChannel.Publish but omitted it here. Without the guard a   |
// |         |            |        | Tier A/B struct exceeding MaxEventSlotBytes would silently overflow    |
// |         |            |        | the ring-buffer slot into adjacent slots instead of throwing a         |
// |         |            |        | diagnostic ERR_EVT_QUEUE_OVERFLOW (0x1701) error (§3.5.1).            |
// | 1.5     | 2026-06-02 | —      | AR-5 M-1: structSize guards reordered to precede QueueCount++         |
// |         |            |        | reservation. If a guard threw after QueueCount++ (oversized or zero   |
// |         |            |        | struct size — both are registration errors but recoverable in some    |
// |         |            |        | hosts), the slot was reserved but PayloadBuffer/SlotMeta never        |
// |         |            |        | populated; subsequent Publish wrote to slotIndex+1, leaving slot N    |
// |         |            |        | with EventTypeOrdinal=0 which SerializeLedger classifies as Tier A    |
// |         |            |        | and emits as a zero-byte record, corrupting the canonical digest.    |
// | 1.5.1   | 2026-06-02 | —      | AR-6 M-1: header Modified date refreshed to match the latest         |
// |         |            |        | version-history row (FR-CS-057). AR-5 added the v1.5 row but left    |
// |         |            |        | the Modified header at 2026-05-31. No code change in this revision.  |
// | 1.6     | 2026-06-02 | —      | AR-7 L-1: added #if UNITY_EDITOR||DEVELOPMENT_BUILD producer-phase   |
// |         |            |        | assertion to Publish<T>(in T evt) where T : struct, IEventB —       |
// |         |            |        | symmetric to the AR-1 M-2 assertion on the Tier A overload. Both    |
// |         |            |        | tiers route through PublishAuthoritative and both have producer-     |
// |         |            |        | phase metadata in the registry; the asymmetry would have masked     |
// |         |            |        | Stage 5+ Tier B wiring mistakes (producer publishes from the wrong   |
// |         |            |        | phase). Debug-only — stripped from release builds (FR-EVT-048).     |
// | 1.7     | 2026-06-07 | —      | AR-10 M-1: phase validity guard added to PublishAuthoritative      |
// |         |            |        | before the QueueCount++ slot reservation. The AR-8 M-2 sentinel    |
// |         |            |        | CurrentPhase = (PhaseId)0xFF intentionally trips a stale Publish   |
// |         |            |        | between OnTickBoundary and the next BeginPhase, but the implicit  |
// |         |            |        | IndexOutOfRangeException at PhaseDrawIndices[0xFF] fired AFTER     |
// |         |            |        | QueueCount++ ran — a release-build host that caught the IORE and  |
// |         |            |        | continued would corrupt the canonical digest exactly as in the    |
// |         |            |        | AR-5 M-1 hazard (unpopulated slot, ordinal=0, classified Tier A   |
// |         |            |        | by SerializeLedger). Guard before reservation so the throw is     |
// |         |            |        | recoverable and parallel with the structSize / depth / overflow   |
// |         |            |        | guards. Header date refreshed 2026-06-02 → 2026-06-07.            |
// | 1.8     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->         |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is              |
// |         |            |        | Unity.Profiling; the old using was CS0246 under Unity and the      |
// |         |            |        | Linux compile gate alike, so this assembly could not have compiled |
// |         |            |        | in-engine. No functional change.                                   |
// | 1.9     | 2026-06-12 | —      | ERR-017-002 (H, dotnet CI gate): the three Publish<T> and three    |
// |         |            |        | Subscribe<T> overloads differed ONLY by generic constraint         |
// |         |            |        | (IEventA/IEventB/IEventC) — constraints are not part of a method   |
// |         |            |        | signature, so this was CS0111 under every C# compiler incl. Unity; |
// |         |            |        | the assembly never compiled. Spec §3.2.1/§3.2.2 specified the      |
// |         |            |        | illegal surface and is patched in the same commit. Replaced with   |
// |         |            |        | ONE Publish + ONE Subscribe (where T : struct) routing on new      |
// |         |            |        | EventTierCache<T> cached marker flags (boot-time type-init only;   |
// |         |            |        | JIT-folds to constants; zero steady-state cost, FR-EVT-048).       |
// |         |            |        | Exactly-one-marker contract enforced at entry (FR-EVT-009a);       |
// |         |            |        | violation throws via ThrowTierContractViolation. Tier C subscribe  |
// |         |            |        | routes through new internal CosmeticChannel.SubscribeFromBus seam  |
// |         |            |        | (public IEventC-constrained Subscribe surface unchanged). All call |
// |         |            |        | sites (EventBus.Publish(in evt) / Subscribe(handler)) compile      |
// |         |            |        | unchanged — dispatch moved from overload resolution to runtime     |
// |         |            |        | cached flags with identical per-tier behaviour.                    |
#endregion
