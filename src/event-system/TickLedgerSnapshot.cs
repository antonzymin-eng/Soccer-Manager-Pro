// File:     src/event-system/TickLedgerSnapshot.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Event System #17 §3.2.3 / §4.4.2, Match Analytics & Statistics #37 §4.3 (KD-7),
//           Code Standards #20
// Purpose:  A reusable, pre-allocated copy of ONE tick's canonically-ordered Tier A/B ledger
//           records — the read-only observation surface #37's aggregator consumes. Filled by
//           EventBus.CaptureTickLedger between DrainTick and OnTickBoundary; never mutates the
//           ledger, the ring buffer, or the digest.

using System;
using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// A snapshot of the Tier A/B records the bus drained on one tick, in FM-017-002 canonical
    /// order, held as raw record bytes copied out of the ring buffer.
    ///
    /// <para><b>Why a copy and not a view (#37 §4.3, KD-7).</b> The ring buffer is process-static and
    /// its slot bytes are overwritten by the next tick's publishes; a consumer holding indices into
    /// it would read a different tick's data with no way to notice. Copying at capture time makes the
    /// "current-tick scoped" contract structural rather than documentary — the same reasoning that
    /// makes <c>MatchEngine.BallView</c> return a value copy rather than a reference.</para>
    ///
    /// <para><b>Capacity is structural, not a guess.</b> The ring buffer physically cannot hold more
    /// than <see cref="EventSystemConstants.EventQueueCapacity"/> slots on one tick, each at most
    /// <see cref="EventSystemConstants.MaxEventSlotBytes"/> long, so a snapshot sized to that product
    /// cannot overflow — there is no drop path to reason about and no silent truncation.</para>
    ///
    /// <para>Pre-allocated once and reused every tick: <see cref="EventBus.CaptureTickLedger"/>
    /// allocates nothing.</para>
    /// </summary>
    public sealed class TickLedgerSnapshot
    {
        private readonly byte[] _ordinals;
        private readonly int[]  _offsets;
        private readonly int[]  _sizes;
        private readonly byte[] _payload;

        private int _count;

        /// <summary>Creates an empty snapshot with buffers sized so capture can never overflow.</summary>
        public TickLedgerSnapshot()
        {
            int capacity = EventSystemConstants.EventQueueCapacity;
            _ordinals = new byte[capacity];
            _offsets  = new int[capacity];
            _sizes    = new int[capacity];
            _payload  = new byte[capacity * EventSystemConstants.MaxEventSlotBytes];
            _count    = 0;
        }

        /// <summary>Number of Tier A/B records captured for the tick.</summary>
        public int Count => _count;

        /// <summary>
        /// Appendix A event-type ordinal of the record at <paramref name="index"/>, in canonical order.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Index outside <c>[0, Count)</c>.</exception>
        public byte OrdinalAt(int index)
        {
            RequireIndex(index);
            return _ordinals[index];
        }

        /// <summary>
        /// Reads the record at <paramref name="index"/> back as a value copy of <typeparamref name="T"/>.
        /// The caller is expected to have branched on <see cref="OrdinalAt"/> first.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Index outside <c>[0, Count)</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <typeparamref name="T"/> is wider than the captured record — reading it would run past the
        /// record's own bytes into the next one, so it fails loud rather than returning a plausible
        /// value assembled from two records.
        /// </exception>
        public T ReadAt<T>(int index) where T : struct
        {
            RequireIndex(index);

            int size = _sizes[index];
            int want = EventRegistry.SizeOfStruct<T>();
            if (want > size)
            {
                throw new ArgumentException(
                    "TickLedgerSnapshot.ReadAt<" + typeof(T).Name + ">: the captured record at index " +
                    index + " (ordinal 0x" + _ordinals[index].ToString("X2") + ") is " + size +
                    " bytes; " + typeof(T).Name + " is " + want +
                    ". Branch on OrdinalAt(index) before choosing the record type.");
            }

            return MemoryMarshal.Read<T>(new ReadOnlySpan<byte>(_payload, _offsets[index], want));
        }

        /// <summary>Drops every captured record (the snapshot reports <c>Count == 0</c>).</summary>
        public void Clear()
        {
            _count = 0;
        }

        /// <summary>
        /// Appends one record's bytes. Called only by <see cref="EventBus.CaptureTickLedger"/>, which
        /// walks the ledger in FM-017-002 canonical order, so append order IS canonical order.
        /// </summary>
        internal void Append(byte ordinal, ReadOnlySpan<byte> record)
        {
            // Structurally unreachable: the ring holds at most EventQueueCapacity slots per tick and
            // the buffers are sized to that. Kept as a fail-loud assertion so a future capacity change
            // that forgets this type surfaces here rather than as a silent dropped record.
            if (_count >= _ordinals.Length)
            {
                throw new InvalidOperationException(
                    EventSystemConstants.ErrPrefixQueueOverflow +
                    ": TickLedgerSnapshot capacity (" + _ordinals.Length +
                    ") exceeded. It is sized from EventQueueCapacity — re-size it with that constant.");
            }

            int offset = _count == 0 ? 0 : _offsets[_count - 1] + _sizes[_count - 1];
            record.CopyTo(new Span<byte>(_payload, offset, record.Length));

            _ordinals[_count] = ordinal;
            _offsets[_count]  = offset;
            _sizes[_count]    = record.Length;
            _count++;
        }

        private void RequireIndex(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "Index must be in [0, " + _count + ").");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): the KD-7 read-only per-tick ledger  |
// |         |            |        | tap surface — a pre-allocated, reusable copy of one tick's     |
// |         |            |        | canonically-ordered Tier A/B records.                          |
#endregion
