// File:     src/event-system/EventLedger.cs
// Created:  2026-05-30
// Modified: 2026-06-15
// Author:   —
// Spec:     Event System #17 §3.2.3, §3.2.4, §3.2.5, §3.4.2, §4.4, Code Standards #20
// Purpose:  Tier A/B ring buffer, typed dispatch infrastructure, and per-tick serialization.
//           Internal — all public API goes through EventBus.
//           Stage 0 notes:
//             - EntityId in sort key is always 0 (entity resolution is Stage 1).
//             - SerializeLedger writes raw struct bytes (including padding); strict
//               canonical no-padding form (FR-EVT-006) is Stage 0+1.
//             - Virtual dispatch in EventTypeDispatcher is an accepted Stage 0 compromise
//               against FR-CS-068; a Stage 1 optimization will replace with function pointers.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.EventSystem
{
    // ── Sort key metadata stored per ring-buffer slot ────────────────────────────────

    internal struct EventSlotMeta
    {
        internal byte   ProducingPhaseIndex;
        internal ushort SubsystemOrdinal;
        internal int    EntityId;            // always 0 at Stage 0; §3.2.4 TODO Stage 1
        internal byte   EventTypeOrdinal;
        internal ushort IntraPhaseDrawIndex;
        internal int    StructSize;          // sizeof(T); used by SerializeLedger

        internal int CompareKey(in EventSlotMeta other)
        {
            int cmp = ProducingPhaseIndex.CompareTo(other.ProducingPhaseIndex);
            if (cmp != 0) return cmp;
            cmp = SubsystemOrdinal.CompareTo(other.SubsystemOrdinal);
            if (cmp != 0) return cmp;
            cmp = EntityId.CompareTo(other.EntityId);
            if (cmp != 0) return cmp;
            return IntraPhaseDrawIndex.CompareTo(other.IntraPhaseDrawIndex);
        }
    }

    // ── Typed-dispatch abstract base and generic sealed subclass ─────────────────────
    // Stage 0 note: virtual dispatch violates FR-CS-068 in the per-event inner loop.
    // Accepted Stage 0 compromise (spec §3.2.5 BFS needs type-erased dispatch).
    // Stage 1 will replace with managed function pointers (delegate* <...>) or
    // source-generated switch tables to eliminate the virtual call.

    internal abstract class EventTypeDispatchBase
    {
        internal int HandlerCount;
        internal abstract void Dispatch(byte[] buffer, int slotOffset, int structSize);
        internal abstract void Dispatch(ReadOnlySpan<byte> slotBytes, int structSize);
        internal abstract void RemoveHandler(ushort index);
    }

    internal sealed class EventTypeDispatcher<T> : EventTypeDispatchBase
        where T : struct
    {
        private readonly EventHandler<T>[] _handlers;

        internal EventTypeDispatcher(int capacity)
        {
            _handlers = new EventHandler<T>[capacity];
        }

        /// <summary>
        /// Adds <paramref name="handler"/> to the dispatcher and returns the slot index it
        /// occupies. AR-7 M-1: scans for a null slot left by a prior RemoveHandler before
        /// appending at HandlerCount. Without slot reuse Tier C subscribe/unsubscribe cycling
        /// (FR-EVT-022) exhausts MaxTierCHandlersPerType after that many cycles even when the
        /// net subscriber count is 0.
        /// </summary>
        internal ushort AddHandler(EventHandler<T> handler)
        {
            for (int i = 0; i < HandlerCount; i++)
            {
                if (_handlers[i] == null)
                {
                    _handlers[i] = handler;
                    return (ushort)i;
                }
            }
            if (HandlerCount >= _handlers.Length)
                throw new InvalidOperationException(
                    EventSystemConstants.ErrPrefixQueueOverflow + ": subscriber handler capacity exceeded. " +
                    "Increase MaxHandlersPerEventType or MaxTierCHandlersPerType.");
            ushort idx = (ushort)HandlerCount;
            _handlers[HandlerCount++] = handler;
            return idx;
        }

        internal override void RemoveHandler(ushort index)
        {
            if (index < HandlerCount)
                _handlers[index] = null; // null-slot skipped in Dispatch; count not decremented
        }

        internal override void Dispatch(byte[] buffer, int slotOffset, int structSize)
        {
            Dispatch(new ReadOnlySpan<byte>(buffer, slotOffset, structSize), structSize);
        }

        internal override void Dispatch(ReadOnlySpan<byte> slotBytes, int structSize)
        {
            T evt = MemoryMarshal.Read<T>(slotBytes.Slice(0, structSize));
            for (int i = 0; i < HandlerCount; i++)
            {
                if (_handlers[i] != null)
                {
                    // Reset per-handler (FR-EVT-046a): each handler invocation gets its own
                    // out-degree budget of exactly 1 secondary Tier A/B publish.
                    EventLedger.HandlerSecondaryPublishCount = 0;
                    _handlers[i](in evt);
                }
            }
        }
    }

    // ── Ring buffer and phase draw index state ────────────────────────────────────────

    /// <summary>
    /// Internal ring buffer for Tier A/B events. Owns storage, dispatch table,
    /// DrainTick, and SerializeLedger. Access only via EventBus.
    /// Event System #17 §3.2.3 / §3.4.2 / §4.4.
    /// </summary>
    internal static class EventLedger
    {
        // Pre-allocated ring buffer: EventQueueCapacity × MaxEventSlotBytes bytes.
        internal static readonly byte[] PayloadBuffer =
            new byte[EventSystemConstants.EventQueueCapacity * EventSystemConstants.MaxEventSlotBytes];

        // Per-slot metadata (sort key + struct size).
        internal static readonly EventSlotMeta[] SlotMeta =
            new EventSlotMeta[EventSystemConstants.EventQueueCapacity];

        // Per-phase intraPhaseDrawIndex counters. Index by PhaseId ordinal (0–6).
        internal static readonly ushort[] PhaseDrawIndices = new ushort[7];

        // Type-erased dispatch table indexed by eventTypeOrdinal (0x00–0xFF).
        internal static readonly EventTypeDispatchBase[] Dispatchers =
            new EventTypeDispatchBase[256];

        // Current count of slots written this tick.
        internal static int QueueCount;

        // Current pipeline phase (set by EventBus.BeginPhase).
        internal static PhaseId CurrentPhase;

        // Current tick (set by EventBus.BeginTick).
        internal static uint CurrentTick;

        // Boot phase complete flag. True after first DrainTick call.
        internal static bool BootPhaseComplete;

        // Out-degree tracking during DrainTick (FR-EVT-046a).
        internal static bool InDrainDispatch;
        internal static int  HandlerSecondaryPublishCount; // reset per-handler invocation

        // AR-9 M-1: boot-time invariant — EventQueueCapacity is runtime-tunable [GT] (Stage 1
        // config loader); the per-tick sort-index stackalloc in DrainTick/SerializeLedger is
        // capped at MAX_QUEUE_SORT_INTS to bound the stack footprint. If a future config bump
        // pushes EventQueueCapacity past the cap, the sort buffer could not cover all queued
        // slots — fail fast at boot rather than silently truncating canonical order.
        static EventLedger()
        {
            if (EventSystemConstants.EventQueueCapacity > EventSystemConstants.MAX_QUEUE_SORT_INTS)
                throw new InvalidOperationException(
                    EventSystemConstants.ErrPrefixQueueOverflow + ": EventQueueCapacity (" +
                    EventSystemConstants.EventQueueCapacity +
                    ") exceeds MAX_QUEUE_SORT_INTS (" +
                    EventSystemConstants.MAX_QUEUE_SORT_INTS +
                    "). Either raise MAX_QUEUE_SORT_INTS (and audit stack footprint, ~4 bytes " +
                    "× MAX_QUEUE_SORT_INTS × 2 concurrent calls) or lower EventQueueCapacity.");
        }

        // ── Sort scratch buffer (reused each DrainTick call) ─────────────────────────

        // Not stackalloc'd here because DrainTick does it per-call to stay zero-alloc
        // and avoid pinning the buffer across GC safepoints.

        // ── Phase reset ──────────────────────────────────────────────────────────────

        internal static void BeginPhase(PhaseId phase)
        {
            CurrentPhase = phase;
            PhaseDrawIndices[(byte)phase] = 0;
        }

        // ── DrainTick: sort + BFS dispatch ───────────────────────────────────────────

        internal static void DrainTick()
        {
            BootPhaseComplete = true; // first drain marks boot phase complete

            // AR-9 M-1: stackalloc capped at compile-time MAX_QUEUE_SORT_INTS (8 KB);
            // static-ctor invariant guarantees EventQueueCapacity <= MAX_QUEUE_SORT_INTS,
            // so the buffer always covers QueueCount.
            Span<int> sortBuf = stackalloc int[EventSystemConstants.MAX_QUEUE_SORT_INTS];

            int batchStart = 0;
            int depth      = 0;

            while (batchStart < QueueCount)
            {
                if (depth >= EventSystemConstants.MaxEventDispatchDepth)
                    throw new InvalidOperationException(
                        EventSystemConstants.ErrPrefixQueueOverflow + ": BFS dispatch depth exceeded " +
                        EventSystemConstants.MaxEventDispatchDepth);

                int batchEnd  = QueueCount; // snapshot before dispatch (handlers may append)
                int batchSize = batchEnd - batchStart;

                // Populate sort indices for this batch.
                for (int i = 0; i < batchSize; i++)
                    sortBuf[i] = batchStart + i;

                // Insertion sort by FM-017-002 key. O(n²) is acceptable at Stage 0
                // (typical batch sizes are ≪ EventQueueCapacity).
                InsertionSort(sortBuf.Slice(0, batchSize));

                // Dispatch in canonical sort order.
                InDrainDispatch = true;
                try
                {
                    for (int i = 0; i < batchSize; i++)
                    {
                        int slotIndex = sortBuf[i];
                        ref EventSlotMeta meta = ref SlotMeta[slotIndex];
                        EventTypeDispatchBase dispatcher = Dispatchers[meta.EventTypeOrdinal];
                        if (dispatcher == null)
                            continue; // no subscribers for this event type; still ledgered

                        dispatcher.Dispatch(
                            PayloadBuffer,
                            slotIndex * EventSystemConstants.MaxEventSlotBytes,
                            meta.StructSize);
                    }
                }
                finally
                {
                    // Always clear re-entrant flag so subsequent ticks are not affected if
                    // a handler throws. try/finally is permitted outside inner loops (FR-CS-069).
                    InDrainDispatch = false;
                }

                batchStart = batchEnd;
                depth++;
            }
        }

        private static void InsertionSort(Span<int> indices)
        {
            for (int i = 1; i < indices.Length; i++)
            {
                int key = indices[i];
                int j   = i - 1;
                while (j >= 0 && SlotMeta[indices[j]].CompareKey(in SlotMeta[key]) > 0)
                {
                    indices[j + 1] = indices[j];
                    j--;
                }
                indices[j + 1] = key;
            }
        }

        // ── SerializeLedger: emit EventLedgerRecord bytes into caller span ───────────

        internal static int SerializeLedger(Span<byte> dst)
        {
            // FM-017-001: PhaseScopeFields[Events] = SerializeCanonical(
            //     DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord)
            //
            // Stage 0 note: writes raw struct bytes (with padding) per slot.
            // FR-EVT-006 (no implicit padding) is Stage 0+1; the canonical serializer
            // (§3.4.2) will strip padding in future passes. The domain tag, count header,
            // and FM-017-002 canonical sort order are always correct.
            //
            // AR-4 fix: sort Tier A/B slot indices by FM-017-002 key before emitting.
            // SerializeLedger previously iterated slots in raw insertion order, making
            // the digest bytes publication-order-dependent and contradicting the XML doc
            // claim and EventBus.SerializeLedger XML doc ("FM-017-002 order").
            // Uses the same InsertionSort as DrainTick; stackalloc cost is identical
            // to the DrainTick sort budget (FR-EVT-049/050).

            int offset = 0;

            // AR-10 L-2: defensive bound check before writing the 5-byte fixed header
            // (1-byte domain tag + 4-byte LE count). Without this, a too-small dst
            // threw an opaque IndexOutOfRangeException at dst[0] / dst[countOffset+3]
            // rather than the diagnostic ArgumentException the payload-copy path
            // emits below. Symmetric with the per-record bound check at line ~320.
            const int FixedHeaderBytes = 5; // 1-byte domain tag + 4-byte LE count
            if (dst.Length < FixedHeaderBytes)
                throw new ArgumentException(
                    "SerializeLedger: destination span too small for fixed 5-byte header " +
                    "(domain tag + u32 LE count). dst.Length=" + dst.Length);

            // Domain tag (1 byte).
            dst[offset++] = EventSystemConstants.DomainTagEventLedger;

            // Count (u32 LE) — only Tier A and Tier B records (FR-EVT-012).
            int countOffset = offset;
            offset += 4; // reserve count field; will fill after counting
            uint count = 0;

            // Build sorted index list for Tier A/B slots only, then sort by FM-017-002 key.
            // AR-9 M-1: stackalloc capped at compile-time MAX_QUEUE_SORT_INTS.
            Span<int> sortBuf = stackalloc int[EventSystemConstants.MAX_QUEUE_SORT_INTS];
            int sortCount = 0;
            // AR-6 L-1: tier comparison uses DeterminismTier enum values rather than the
            // magic literals 0/1 (FR-CS-016). #16 §3.2 owns the enum and its byte ordinals.
            for (int s = 0; s < QueueCount; s++)
            {
                byte tier = EventRegistry.GetTier(SlotMeta[s].EventTypeOrdinal);
                if (tier == (byte)DeterminismTier.TierA || tier == (byte)DeterminismTier.TierB)
                    sortBuf[sortCount++] = s;
            }
            InsertionSort(sortBuf.Slice(0, sortCount));

            for (int i = 0; i < sortCount; i++)
            {
                int slotIndex = sortBuf[i];
                ref EventSlotMeta meta = ref SlotMeta[slotIndex];
                int slotOffset = slotIndex * EventSystemConstants.MaxEventSlotBytes;
                int bytesToCopy = meta.StructSize;

                if (offset + bytesToCopy > dst.Length)
                    throw new ArgumentException(
                        "SerializeLedger: destination span too small for event record.");

                new ReadOnlySpan<byte>(PayloadBuffer, slotOffset, bytesToCopy)
                    .CopyTo(dst.Slice(offset, bytesToCopy));
                offset += bytesToCopy;
                count++;
            }

            // Fill in count field (u32 LE).
            dst[countOffset]     = (byte)count;
            dst[countOffset + 1] = (byte)(count >> 8);
            dst[countOffset + 2] = (byte)(count >> 16);
            dst[countOffset + 3] = (byte)(count >> 24);

            return offset;
        }

        // ── OnTickBoundary: reset per-tick state ─────────────────────────────────────

        internal static void OnTickBoundary()
        {
            QueueCount = 0;
            for (int i = 0; i < PhaseDrawIndices.Length; i++)
                PhaseDrawIndices[i] = 0;
            // Payload bytes intentionally left in place; overwritten on next publish.

            // AR-8 M-2: reset lifecycle state to fail-fast sentinels so a stale Publish
            // between OnTickBoundary and the next BeginTick/BeginPhase trips an
            // IndexOutOfRangeException on PhaseDrawIndices[0xFF] in PublishAuthoritative
            // and (in dev builds) misses the producer-phase assert with the sentinel
            // phase value. CurrentTick=uint.MaxValue makes a stale publish's tick header
            // visibly bogus if the bounds check is somehow bypassed. BeginTick /
            // BeginPhase overwrite both fields before the next valid publish.
            CurrentPhase = (PhaseId)0xFF;
            CurrentTick  = uint.MaxValue;
        }

        // ── Subscribe: add handler to dispatcher (called by EventBus) ────────────────

        internal static SubscriptionToken Subscribe<T>(
            EventHandler<T> handler, byte ordinal, int maxHandlers)
            where T : struct
        {
            if (Dispatchers[ordinal] == null)
                Dispatchers[ordinal] = new EventTypeDispatcher<T>(maxHandlers);

            // AR-5 M-2: symmetric to AR-4 L fix in CosmeticChannel.Subscribe. If two
            // distinct IEventA/B types share an ordinal via duplicate RegisterExternalRow
            // calls (a real Stage 1 boot-wiring risk), hard-cast threw an opaque
            // InvalidCastException with no ordinal context. 'as' + null-check emits
            // the same ERR_EVT_ORDINAL_COLLISION (0x1707) diagnostic used by Tier C.
            var typed = Dispatchers[ordinal] as EventTypeDispatcher<T>;
            if (typed == null)
                throw new InvalidOperationException(
                    EventSystemConstants.ErrPrefixOrdinalCollision + ": ordinal 0x" + ordinal.ToString("X2") +
                    " is already bound to a different Tier A/B event type. " +
                    typeof(T).Name + " and the existing type share the same ordinal — " +
                    "check for duplicate ordinal in RegisterExternalRow calls.");

            // AR-7 M-1: use the index returned by AddHandler (may reuse a null slot from a
            // prior RemoveHandler) rather than the pre-add HandlerCount value.
            ushort idx = typed.AddHandler(handler);
            return new SubscriptionToken(ordinal, idx);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-30 | —      | Added Dispatch(ReadOnlySpan<byte>, int) overload; eliminated         |
// |         |            |        | ToArray() allocation path in CosmeticChannel.                        |
// | 1.2     | 2026-05-30 | —      | AR-1 H-2: HandlerSecondaryPublishCount reset moved inside            |
// |         |            |        | per-handler loop (FR-EVT-046a per-invocation semantics).             |
// |         |            |        | AR-1 H-3: InDrainDispatch wrapped in try/finally to prevent          |
// |         |            |        | flag corruption on handler exception.                                |
// |         |            |        | AR-1 M-1: bounds check added to AddHandler.                          |
// | 1.3     | 2026-05-31 | —      | AR-4 H-1: EventTypeOrdinal removed from CompareKey — it was the 4th |
// |         |            |        | key between EntityId and IntraPhaseDrawIndex, violating FM-017-002   |
// |         |            |        | canonical order (ProducingPhaseIndex, SubsystemOrdinal, EntityId,    |
// |         |            |        | IntraPhaseDrawIndex). Fix: drop the EventTypeOrdinal comparison.      |
// |         |            |        | AR-4 H-2: SerializeLedger now sorts Tier A/B slot indices by         |
// |         |            |        | FM-017-002 key before emitting (was insertion order, making digest    |
// |         |            |        | bytes publication-order-dependent and contradicting the EventBus XML  |
// |         |            |        | doc "FM-017-002 order" claim). Uses same stackalloc InsertionSort.    |
// | 1.4     | 2026-06-02 | —      | AR-5 M-2: Subscribe<T> dispatcher cast replaced from hard            |
// |         |            |        | (EventTypeDispatcher<T>) to 'as' + null-check — symmetric to AR-4 L  |
// |         |            |        | fix in CosmeticChannel.Subscribe (Tier C). Emits diagnostic          |
// |         |            |        | ERR_EVT_ORDINAL_COLLISION (0x1707) when two IEventA/B types share    |
// |         |            |        | an ordinal via erroneous RegisterExternalRow calls, instead of an    |
// |         |            |        | opaque InvalidCastException with no ordinal context.                 |
// | 1.5     | 2026-06-02 | —      | AR-6 L-1: SerializeLedger tier filter replaced magic literals       |
// |         |            |        | tier==0 / tier==1 with (byte)DeterminismTier.TierA / .TierB per     |
// |         |            |        | FR-CS-016 (no magic literals; enum from #16 §3.2 owns the ordinals). |
// | 1.6     | 2026-06-02 | —      | AR-7 M-1: EventTypeDispatcher<T>.AddHandler now scans for and       |
// |         |            |        | reuses null slots left by RemoveHandler before appending at         |
// |         |            |        | HandlerCount. Returns the actual slot index so SubscriptionToken    |
// |         |            |        | points at the occupied slot. Without slot reuse, Tier C runtime     |
// |         |            |        | subscribe/unsubscribe cycling (FR-EVT-022) exhausted                 |
// |         |            |        | MaxTierCHandlersPerType after that many cycles even when the net    |
// |         |            |        | subscriber count was 0. EventLedger.Subscribe updated to consume    |
// |         |            |        | AddHandler's return value instead of reading HandlerCount before.   |
// | 1.7     | 2026-06-07 | —      | AR-8 M-2: OnTickBoundary now resets CurrentPhase to (PhaseId)0xFF   |
// |         |            |        | and CurrentTick to uint.MaxValue. A stale Publish between          |
// |         |            |        | OnTickBoundary and the next BeginTick/BeginPhase previously       |
// |         |            |        | recorded the prior tick's tick number and last phase into the      |
// |         |            |        | ring buffer — silently corrupting the next tick's canonical        |
// |         |            |        | digest. With the sentinels, PhaseDrawIndices[0xFF] now throws       |
// |         |            |        | IndexOutOfRangeException in release builds and the producer-phase  |
// |         |            |        | assert fires in dev builds. BeginTick / BeginPhase overwrite both  |
// |         |            |        | fields atomically before the next valid publish.                    |
// | 1.8     | 2026-06-07 | —      | AR-9 M-1: DrainTick + SerializeLedger stackallocs switched from     |
// |         |            |        | runtime-sized EventQueueCapacity to compile-time MAX_QUEUE_SORT_INTS|
// |         |            |        | (= 2048; 8 KB stack). New static ctor asserts EventQueueCapacity <=|
// |         |            |        | MAX_QUEUE_SORT_INTS at boot so a Stage 1 config-loader bump fails  |
// |         |            |        | fast instead of risking StackOverflowException mid-pipeline.       |
// | 1.9     | 2026-06-07 | —      | AR-10 L-1: VersionHistory rows reordered — v1.7 (AR-8 M-2) and     |
// |         |            |        | v1.8 (AR-9 M-1) were appended in reverse chronological order in    |
// |         |            |        | the AR-9 pass, violating FR-CS-057 append-only ordering. No code   |
// |         |            |        | change. AR-10 L-2: SerializeLedger now validates dst.Length >= 5  |
// |         |            |        | (1-byte domain tag + 4-byte LE count header) before writing the   |
// |         |            |        | header; previously a too-small dst threw an opaque                |
// |         |            |        | IndexOutOfRangeException at dst[0] / dst[countOffset+3] rather    |
// |         |            |        | than the diagnostic ArgumentException the payload-copy path emits.|
// | 1.10    | 2026-06-15 | —      | AR-12 M-1: throw-site hex literals (0x1701 / 0x1707) replaced with |
// |         |            |        | EventSystemConstants.ErrPrefixQueueOverflow / ErrPrefixOrdinal-    |
// |         |            |        | Collision so the error codes are the single source of truth.      |
// |         |            |        | Rendered text byte-identical; no functional change.               |
#endregion
