// File:     src/event-system/CosmeticChannel.cs
// Created:  2026-05-30
// Modified: 2026-06-07
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
        /// <remarks>
        /// ERR-017-002: constraint relaxed from <c>struct, IEventC</c> to <c>struct</c> —
        /// this internal entry is reached only through EventBus's single-method tier
        /// dispatch, which enforces the FR-EVT-009a exactly-one-marker contract before
        /// routing Tier C here. The public Subscribe surface keeps the IEventC constraint.
        /// </remarks>
        internal static void Publish<T>(in T evt) where T : struct
        {
            byte ordinal     = EventOrdinalCache<T>.Ordinal;

            // Zero-ordinal guard (AR-2 fix: replaces debug-only Debug.Assert — C# evaluates Assert
            // arguments eagerly, causing string alloc on every Tier C publish. Unconditional throw
            // also catches the error in release builds. FR-EVT-020 / FR-EVT-048).
            if (ordinal == 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): " + typeof(T).Name +
                    " published before EventBusRegistrar.Initialize() — ordinal cache is 0. " +
                    "Call the owning spec's EventBusRegistrar.Initialize() during boot phase (FR-EVT-020).");

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

            // AR-3 fix: promote the structSize <= 0 guard from silent drop to throw — silent data
            // loss is worse than an exception for a registration error (AR-1 open finding).
            if (structSize <= 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): Tier C struct size is 0 for ordinal 0x" +
                    ordinal.ToString("X2") + ". Call EventBusRegistrar.Initialize() before publishing.");

            // AR-3 fix: upper-bound guard — stackSlot is MaxEventSlotBytes bytes; Slice(0, structSize)
            // throws ArgumentOutOfRangeException if structSize > MaxEventSlotBytes (FR-EVT-048 / §3.5.1).
            if (structSize > EventSystemConstants.MaxEventSlotBytes)
                throw new InvalidOperationException(
                    "ERR_EVT_QUEUE_OVERFLOW (0x1701): Tier C struct size " + structSize +
                    " bytes exceeds MaxEventSlotBytes " + EventSystemConstants.MaxEventSlotBytes +
                    " for ordinal 0x" + ordinal.ToString("X2") +
                    ". Increase MaxEventSlotBytes or reduce struct size (§3.5.1).");

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
            return SubscribeFromBus(handler);
        }

        /// <summary>
        /// Internal Tier C subscribe seam for EventBus's single-method tier dispatch
        /// (ERR-017-002). EventBus validates the FR-EVT-009a exactly-one-marker contract
        /// before routing here, so this entry carries no compile-time IEventC constraint;
        /// the public <see cref="Subscribe{T}"/> surface keeps it. Same body and
        /// semantics as the pre-ERR-017-002 Subscribe. §3.2.2 / §4.3.2.
        /// </summary>
        internal static SubscriptionToken SubscribeFromBus<T>(EventHandler<T> handler)
            where T : struct
        {
            EventRegistry.EnsureInitialized();
            byte ordinal = EventOrdinalCache<T>.Ordinal;

            // Zero-ordinal guard (AR-2 fix: replaces debug-only Debug.Assert — handler would be
            // stored at slot 0 and orphaned once Initialize() runs with the real ordinal. FR-EVT-020).
            if (ordinal == 0)
                throw new InvalidOperationException(
                    "ERR_EVT_UNREGISTERED_ORDINAL (0x1706): " + typeof(T).Name +
                    " subscribed before EventBusRegistrar.Initialize() — ordinal cache is 0. " +
                    "Call the owning spec's EventBusRegistrar.Initialize() during boot phase (FR-EVT-020).");

            if (s_dispatchers[ordinal] == null)
                s_dispatchers[ordinal] = new EventTypeDispatcher<T>(
                    EventSystemConstants.MaxTierCHandlersPerType);

            // AR-4 fix: use 'as' instead of hard cast so an ordinal collision (two different
            // IEventC types registered to the same ordinal via erroneous RegisterExternalRow
            // calls) throws a diagnostic InvalidOperationException rather than a generic
            // InvalidCastException with no ordinal context.
            var typed = s_dispatchers[ordinal] as EventTypeDispatcher<T>;
            if (typed == null)
                throw new InvalidOperationException(
                    "ERR_EVT_ORDINAL_COLLISION (0x1707): ordinal 0x" + ordinal.ToString("X2") +
                    " is already bound to a different Tier C event type. " +
                    typeof(T).Name + " and the existing type share the same ordinal — " +
                    "check for duplicate ordinal in RegisterExternalRow calls.");

            // AR-7 M-1: use the index returned by AddHandler (may reuse a null slot from a
            // prior Unsubscribe) rather than the pre-add HandlerCount value. This is the Tier C
            // hot path for the exhaustion bug — FR-EVT-022 explicitly permits Tier C runtime
            // subscribe/unsubscribe, and without slot reuse the handler array fills with nulls.
            ushort idx = typed.AddHandler(handler);
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

        // ── Test-only reset ───────────────────────────────────────────────────────────

        /// <summary>
        /// AR-9 L-1: clears <c>s_dispatchers</c> and <c>s_pubCounts</c> so unit tests start
        /// from a clean Tier C state. Production code MUST NOT call this — Tier C
        /// subscribers are expected to outlive a tick. Exposed via InternalsVisibleTo to
        /// TacticalDirector.EventSystem.Tests only.
        /// </summary>
        internal static void ResetForTests()
        {
            Array.Clear(s_dispatchers, 0, s_dispatchers.Length);
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
// | 1.3     | 2026-05-30 | —      | AR-1 fix: added zero-ordinal guards in Publish<T> and Subscribe<T>    |
// |         |            |        | (debug builds) — catches Tier C use before EventBusRegistrar.Init().  |
// | 1.4     | 2026-05-30 | —      | AR-2 fix: guards promoted from debug-only Debug.Assert to             |
// |         |            |        | unconditional if/throw — eliminates eager string alloc on hot path    |
// |         |            |        | (FR-EVT-048) and catches errors in release builds (FR-EVT-020).       |
// | 1.5     | 2026-05-30 | —      | AR-3 fix: structSize <= 0 guard changed from silent return to throw   |
// |         |            |        | (silent data-loss worse than exception for registration error);       |
// |         |            |        | added upper-bound guard structSize > MaxEventSlotBytes (prevents       |
// |         |            |        | ArgumentOutOfRangeException from stackSlot.Slice on oversized structs).|
// | 1.6     | 2026-05-31 | —      | AR-4 L: Subscribe<T> dispatcher cast replaced from hard              |
// |         |            |        | (EventTypeDispatcher<T>) to 'as' + null-check: emits diagnostic        |
// |         |            |        | ERR_EVT_ORDINAL_COLLISION (0x1707) when two IEventC types share        |
// |         |            |        | the same ordinal via erroneous RegisterExternalRow calls, rather       |
// |         |            |        | than an opaque InvalidCastException with no ordinal context.           |
// | 1.7     | 2026-06-02 | —      | AR-7 M-1: Subscribe<T> now consumes the slot index returned by       |
// |         |            |        | AddHandler (which AR-7 M-1 changed to scan-and-reuse null slots)      |
// |         |            |        | rather than reading HandlerCount before the call. Closes the Tier C  |
// |         |            |        | exhaustion path under FR-EVT-022 subscribe/unsubscribe cycling.       |
// | 1.8     | 2026-06-07 | —      | AR-9 L-1: added internal ResetForTests() that clears s_dispatchers   |
// |         |            |        | + s_pubCounts. EventTestHelper.ResetLedger previously only cleared   |
// |         |            |        | pub counts, so Tier C subscribers leaked across tests and would     |
// |         |            |        | eventually exhaust MaxTierCHandlersPerType (64).                     |
// | 1.9     | 2026-06-12 | —      | ERR-017-002: internal Publish<T> constraint relaxed struct,IEventC   |
// |         |            |        | -> struct (EventBus single-method tier dispatch enforces FR-EVT-009a |
// |         |            |        | before routing here); public Subscribe<T> keeps the IEventC          |
// |         |            |        | constraint and now delegates to new internal SubscribeFromBus<T>     |
// |         |            |        | (where T : struct) carrying the original body - the seam EventBus's  |
// |         |            |        | unified Subscribe routes Tier C through.                             |
#endregion
