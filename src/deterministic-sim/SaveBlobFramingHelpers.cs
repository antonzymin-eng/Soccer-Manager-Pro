// File:     src/deterministic-sim/SaveBlobFramingHelpers.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer, which every method here builds
//           on); Training System #29 §4.4, Injuries & Medical #41 §4.4 (the two current callers — a
//           canonical-ordered, ascending-key, length-bounded sub-blob of `{ i32 key, u32 count, ... }`
//           blocks); Code Standards #20
// Purpose:  Shared framing helpers for a save sub-blob shaped as a canonical-ordered map keyed by one
//           or two ascending integer ids (club id, then player id within a club): sorting a caller's
//           key array into canonical order without mutating it, refusing a decode-side key that is not
//           strictly ascending, bounding a length-prefixed element count against the bytes that remain,
//           and the overflow-safe truncation guard both of those build on. Extracted from
//           TrainingSaveCodec and MedicalSaveCodec (ERR-029-004 / ERR-041-008 landed them byte-layout-
//           identical; nothing mechanical enforced that until this file existed). Does NOT extract
//           `Require`/`ReadCount` from the older MatchSaveCodec / SeasonSaveCodec / SeasonStateCodec —
//           that is a pre-existing, deliberately un-consolidated repo convention; this file is for new
//           callers only.

using System;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Shared framing helpers for a canonical-ordered, ascending-key sub-blob codec. Every method is a
    /// pure function over the caller-supplied buffer/array plus a caller-supplied message prefix, so the
    /// existing per-codec wording ("Training save …", "Medical save …") is preserved verbatim by the
    /// caller's arguments rather than hard-coded here.
    /// </summary>
    public static class SaveBlobFramingHelpers
    {
        /// <summary>
        /// Returns the indices of <paramref name="keys"/> in ascending key order, refusing duplicates.
        /// Sorts a COPY — the caller's array is never reordered, since a codec that sorted the live
        /// roster array in place would silently reindex whatever else is holding it (a schedule view,
        /// for one). A duplicate key IS a defect — there is no defined winner — so it throws rather than
        /// being deduplicated.
        /// </summary>
        /// <param name="keys">The keys to order. Not mutated.</param>
        /// <param name="setName">The save set's name as it reads mid-sentence, e.g. <c>"training"</c> in
        /// "… in the training save set …".</param>
        /// <param name="what">The key's name, e.g. <c>"club id"</c> or <c>"player id"</c>.</param>
        /// <param name="duplicateCitation">The FR- id to cite for the duplicate-key contract, e.g.
        /// <c>"FR-TR-025"</c>.</param>
        /// <exception cref="ArgumentException">Two elements of <paramref name="keys"/> are equal.</exception>
        public static int[] CanonicalOrder(int[] keys, string setName, string what, string duplicateCitation)
        {
            int n = keys.Length;
            var sorted = new int[n];
            var order = new int[n];
            for (int i = 0; i < n; i++)
            {
                sorted[i] = keys[i];
                order[i] = i;
            }

            Array.Sort(sorted, order);

            for (int i = 1; i < n; i++)
            {
                if (sorted[i] == sorted[i - 1])
                {
                    throw new ArgumentException(
                        "Duplicate " + what + " " + sorted[i] + " in the " + setName + " save set — a " +
                        "duplicate key has no defined winner (" + duplicateCitation + ").");
                }
            }

            return order;
        }

        /// <summary>
        /// Decode-side mirror of the <see cref="CanonicalOrder"/> guarantee: refuses a key that does not
        /// strictly exceed the previous one at this level, which is the only shape
        /// <see cref="CanonicalOrder"/> can ever have produced. <paramref name="previous"/> is a
        /// <c>long</c> so that the first comparison can start below <c>int.MinValue</c> without a
        /// separate "is this the first?" flag.
        /// </summary>
        /// <param name="value">The key just read.</param>
        /// <param name="previous">The previous key at this level, by reference; starts below
        /// <c>int.MinValue</c> and is advanced to <paramref name="value"/> on success.</param>
        /// <param name="subject">The message prefix, e.g. <c>"Training save"</c> or
        /// <c>"Medical save"</c>.</param>
        /// <param name="what">The key's name, e.g. <c>"club id"</c> or <c>"player id in club 7"</c>.</param>
        /// <param name="index">The zero-based position of this key within its level, for the message.</param>
        /// <exception cref="InvalidOperationException"><paramref name="value"/> does not exceed
        /// <paramref name="previous"/>.</exception>
        public static void RequireAscending(int value, ref long previous, string subject, string what, int index)
        {
            if (value <= previous)
            {
                throw new InvalidOperationException(
                    subject + " " + what + " at index " + index + " is " + value +
                    ", which does not exceed the preceding " + previous +
                    " — the block is written in strictly ascending key order; corrupt save.");
            }

            previous = value;
        }

        /// <summary>
        /// Reads a u32 length prefix and refuses a count whose elements could not possibly be backed by
        /// the bytes that remain. The bound is expressed in ELEMENTS (<c>remaining / bytesPerElement</c>)
        /// rather than as a byte product, because that product can overflow <c>int</c> for a large blob
        /// and a crafted count and wrap back to a small positive value that slips past a byte-wise guard.
        /// <paramref name="bytesPerElement"/> is the element's MINIMUM width (a club header for a club,
        /// which may then carry zero players), so the bound is conservative and never rejects a
        /// well-formed blob — the WorldStateSerializer.ReadCount / SeasonStateCodec posture.
        /// </summary>
        /// <param name="blob">The blob being read.</param>
        /// <param name="o">The current read offset, advanced past the count prefix.</param>
        /// <param name="total">The blob's total length.</param>
        /// <param name="bytesPerElement">The minimum byte width of one element at this level.</param>
        /// <param name="subject">The message prefix, e.g. <c>"Training save"</c> or
        /// <c>"Medical save"</c>.</param>
        /// <param name="what">The element's name, e.g. <c>"club"</c> or <c>"player"</c>.</param>
        /// <exception cref="InvalidOperationException">The blob is truncated reading the count prefix,
        /// or the declared count exceeds what the remaining bytes could hold.</exception>
        public static int ReadCount(byte[] blob, ref int o, int total, int bytesPerElement, string subject, string what)
        {
            Require(o, 4, total, subject, what + " count");
            uint raw = CanonicalSerializer.ReadU32(blob, ref o);
            int maxCount = (total - o) / bytesPerElement;
            if (raw > (uint)maxCount)
            {
                throw new InvalidOperationException(
                    subject + " " + what + " count " + raw + " exceeds the " + maxCount +
                    " element(s) the " + (total - o) + " remaining byte(s) at offset " + o +
                    " could hold — corrupt save.");
            }

            return (int)raw;
        }

        /// <summary>
        /// Refuses a read that would run past <paramref name="total"/>. Overflow-safe (the
        /// MatchSaveCodec / SeasonSaveCodec posture): compares against <c>(total - offset)</c> rather
        /// than <c>(offset + need)</c>, since a corrupt length prefix can push <paramref name="need"/>
        /// near <c>int.MaxValue</c> and <c>offset + need</c> would wrap negative and slip past the guard.
        /// <paramref name="offset"/> is always in <c>[0, total]</c> at every call site (every read is
        /// itself guarded), so <c>total - offset</c> is a safe non-negative <c>int</c>.
        /// </summary>
        /// <param name="offset">The current read offset.</param>
        /// <param name="need">The number of bytes the next read requires.</param>
        /// <param name="total">The blob's total length.</param>
        /// <param name="subject">The message prefix, e.g. <c>"Training save"</c> or
        /// <c>"Medical save"</c>.</param>
        /// <param name="what">The field being read, e.g. <c>"format magic"</c> or <c>"club id"</c>.</param>
        /// <exception cref="InvalidOperationException"><paramref name="need"/> is negative, or fewer than
        /// <paramref name="need"/> bytes remain at <paramref name="offset"/>.</exception>
        public static void Require(int offset, int need, int total, string subject, string what)
        {
            if (need < 0 || offset > total || need > total - offset)
            {
                throw new InvalidOperationException(
                    subject + " blob truncated reading " + what + " (need " + need +
                    " byte(s) at offset " + offset + " of " + total + ").");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial extraction (review finding 2): CanonicalOrder,             |
// |         |            |        | RequireAscending, ReadCount, and Require, factored out of          |
// |         |            |        | TrainingSaveCodec and MedicalSaveCodec, which were byte-identical  |
// |         |            |        | duplicates of each other modulo one message string. Both codecs    |
// |         |            |        | now call this file and no longer carry private copies.             |
#endregion
