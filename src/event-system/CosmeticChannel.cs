// File:     src/event-system/CosmeticChannel.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.2.3, §3.5.3, §3.6.2, §4.3.2, Code Standards #20
// Purpose:  Tier C immediate-synchronous dispatch with deterministic drop predicate.
//           Maintains per-ordinal publication count table (u16[256]); resets each tick.
//           NOT a delivery buffer — Tier C is immediate-dispatch (§3.2.3).

using System;
using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Tier C cosmetic-event dispatch channel. Immediate synchronous dispatch on publish thread.
    /// Maintains publication-count table (u16[256]) for the §3.6.2 deterministic drop predicate.
    /// Tier C events are NOT included in the determinism digest or SnapshotPayload (FR-EVT-014/015).
    /// Event System #17 §3.2.3 / §3.5.3 / §3.6.2 / §4.3.2.
    /// </summary>
    public static class CosmeticChannel
    {
        // Per-ordinal publication count for this tick (u16, 256 slots = 512 bytes; FR-EVT-054).
        private static readonly ushort[] s_pubCounts = new ushort[256];

        // Tier C dispatcher table (indexed by eventTypeOrdinal).
        private static readonly EventTypeDispatchBase[] s_dispatchers = new EventTypeDispatchBase[256];

        // ── Publish (immediate synchronous dispatch) ──────────────────────────────────

        /// <summary>
        /// Immediately dispatches a Tier C event to all registered subscribers on the calling thread.
        /// Drop predicate (FR-EVT-043): if publication count for this ordinal this tick exceeds
        /// <c>maxPerTick</c>, the publish is silently dropped (no subscriber invocation).
        /// Allocates 0 bytes (FR-EVT-048).
        /// </summary>
        internal static void Publish<T>(in T evt) where T : struct, IEventC
        {
            byte ordinal     = EventOrdinalCache<T>.Ordinal;
            ushort maxPerTick = EventRegistry.GetMaxPerTick(ordinal);
            ushort count     = s_pubCounts[ordinal];

            // Deterministic drop predicate (FR-EVT-043): pure function of tick state only.
            // Does NOT read queue depth or any non-tick-deterministic state.
            // ">=" so that maxPerTick=N allows exactly N publishes before dropping.
            if (count >= maxPerTick)
                return; // dropped; logged to trace channel at Stage 0+1 (FR-EVT-045)

            s_pubCounts[ordinal] = (ushort)(count + 1);

            EventTypeDispatchBase dispatcher = s_dispatchers[ordinal];
            if (dispatcher == null)
                return; // no subscribers

            int structSize = EventRegistry.GetStructSize(ordinal);
            if (structSize <= 0)
                return; // ordinal registered via RegisterRowRaw without struct size (external spec)

            // Dispatch immediately (no queue). Writes struct to a stack-allocated slot
            // so MemoryMarshal.Read<T> can reconstruct the typed value.
            // Use a temporary stack buffer to pass bytes through the abstract dispatcher.
            Span<byte> stackSlot = stackalloc byte[EventSystemConstants.MaxEventSlotBytes];
            T copy = evt; // safe value copy; no GC allocation
            MemoryMarshal.Write(stackSlot.Slice(0, structSize), ref copy);

            // Zero-alloc dispatch via the span-based overload (FR-EVT-048).
            dispatcher.Dispatch(stackSlot.Slice(0, structSize), structSize);
        }

        // ── Subscribe / Unsubscribe ───────────────────────────────────────────────────

        /// <summary>
        /// Registers a Tier C subscriber. Permitted at any time during match (FR-EVT-022).
        /// Returns a <see cref="SubscriptionToken"/> for use with <see cref="Unsubscribe"/>.
        /// Allocates once per event type (handler array pre-allocated on first subscribe).
        /// Event System #17 §3.2.2 / §4.3.2.
        /// </summary>
        public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
            where T : struct, IEventC
        {
            byte ordinal = EventOrdinalCache<T>.Ordinal;
            if (s_dispatchers[ordinal] == null)
                s_dispatchers[ordinal] = new EventTypeDispatcher<T>(
                    EventSystemConstants.MaxTierCHandlersPerType);

            var typed = (EventTypeDispatcher<T>)s_dispatchers[ordinal];
            ushort idx = (ushort)typed.HandlerCount;
            typed.AddHandler(handler);
            return new SubscriptionToken(ordinal, idx);
        }

        /// <summary>
        /// Removes a previously registered Tier C subscriber.
        /// The token's handler slot is nulled out; the dispatcher skips null slots.
        /// Permitted at any time during match (FR-EVT-022).
        /// Event System #17 §3.2.2 / §4.3.2.
        /// </summary>
        public static void Unsubscribe(SubscriptionToken token)
        {
            EventTypeDispatchBase dispatcher = s_dispatchers[token.EventTypeOrdinal];
            dispatcher?.RemoveHandler(token.SubscriberIndex);
        }

        // ── Tick boundary reset ───────────────────────────────────────────────────────

        /// <summary>
        /// Resets the per-tick publication count table to zero (FR-EVT-025).
        /// Called by EventBus.OnTickBoundary at end of Snapshot phase.
        /// </summary>
        internal static void ResetPublicationCounts()
        {
            Array.Clear(s_pubCounts, 0, s_pubCounts.Length);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                  |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                |
// | 1.1     | 2026-05-30 | —      | Fixed ToArray() GC allocation in Publish<T>; added span overload.      |
// | 1.2     | 2026-05-30 | —      | AR-1 H-1: drop predicate corrected from > to >= maxPerTick             |
// |         |            |        | so maxPerTick=N allows exactly N publishes before drop.                |
#endregion
