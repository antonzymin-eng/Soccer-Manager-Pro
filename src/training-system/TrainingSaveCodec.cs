// File:     src/training-system/TrainingSaveCodec.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Training System #29 §2.2 (the persisted training block), §4.2 / §4.4 (the sub-blob codec),
//           FR-TR-018/019, F3/F5; ERR-029-004 (the §4.4 layout that was never pinned);
//           Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the #29 training sub-blob — one opaque, independently version-gated
//           block under #30's season save. Encodes the per-club per-player TrainingState map in
//           canonical key order and reads it back, fail-loud on any version / length-bound / ordering /
//           value-contract / trailing-byte violation. No file I/O (that is SeasonSaveManager), so the
//           codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// Encodes / decodes the training sub-blob (#29 §4.4). The blob is one
    /// <see cref="TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION"/>-gated payload carrying every
    /// club's per-player <see cref="TrainingState"/> — focus included, since
    /// <see cref="TrainingState.Focus"/> is the single source of truth and
    /// <see cref="TrainingSchedule"/> is a derived view that is never persisted (FR-TR-003/019).
    /// <para>
    /// <b>Layout</b> (ERR-029-004 pinned it; the sibling <c>MedicalSaveCodec</c> block is byte-for-byte
    /// the same shape with #41's four fields):
    /// </para>
    /// <code>
    /// u32  TRAINING_SAVE_FORMAT_VERSION
    /// u32  clubCount
    /// per club, ascending ClubId:
    ///     i32  clubId
    ///     u32  playerCount
    ///     per player, ascending PlayerId:
    ///         i32  playerId
    ///         u8   focus                  (TrainingFocus ordinal)
    ///         i32  condition
    ///         i32  trainingFatigue
    ///         u32  lastAdvancedWorldDay
    /// </code>
    /// <para>
    /// <b>The club id is written, not implied by position.</b> #41's §4.4 sketch grouped the blocks by
    /// club without naming one, which leaves club identity carried only by list order across a save
    /// boundary — an implicit agreement with a sibling blob that the codec is explicitly forbidden to
    /// read (KD-2 blob independence). Four bytes per club buys a self-describing block and a duplicate
    /// check. See ERR-029-004 / ERR-041-008.
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> The block is a map keyed by <c>(clubId, playerId)</c>, so
    /// <see cref="Encode"/> canonicalizes to ascending keys — two equal state sets always produce
    /// identical bytes regardless of the caller's roster order — and <see cref="Decode"/> requires that
    /// order, so a hand-edited or corrupt blob cannot smuggle in a duplicate key. Encoding a decoded
    /// blob reproduces it byte for byte.
    /// </para>
    /// <para>
    /// <b>No RNG cursor</b> is persisted: #29 registers no stream (KD-6 / ERR-029-001), so
    /// <see cref="TrainingState"/> is the whole of #29's saved surface.
    /// </para>
    /// <para>
    /// Off the 60 Hz hot path (a save is a host action), so allocation is permitted.
    /// </para>
    /// </summary>
    public static class TrainingSaveCodec
    {
        // Layout widths, named so the size computation reads as the layout it mirrors.
        private const int BytesPerClubHeader = 4 + 4;          // clubId i32 + playerCount u32
        private const int BytesPerPlayer = 4 + 1 + 4 + 4 + 4;  // playerId, focus, condition, fatigue, lastDay

        /// <summary>
        /// Encodes every club's training states into the #29 sub-blob, canonicalized to ascending club
        /// id and, within a club, ascending player id. The caller's array and its inner arrays are never
        /// mutated. The buffer is sized exactly to the content; see <see cref="Decode"/> for the inverse
        /// (kept adjacent so a layout change is edited in one place).
        /// </summary>
        /// <param name="clubs">The clubs to persist. An empty array is legal and encodes a well-formed,
        /// zero-club block — that is the honest state of a game that tracks no training yet, not a
        /// missing blob.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> is null, or a club's arrays
        /// were never bound (a <c>default(ClubTrainingStates)</c> element).</exception>
        /// <exception cref="ArgumentException">Two clubs share a club id, or two players within one club
        /// share a player id — a duplicate key has no defined winner and is a roster-lifecycle bug
        /// (FR-TR-025), never silently resolved.</exception>
        public static byte[] Encode(ClubTrainingStates[] clubs)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            int[] clubOrder = CanonicalOrder(ClubIdsOf(clubs), "club id");

            int size = 4 + 4;   // version + clubCount
            for (int i = 0; i < clubs.Length; i++)
            {
                size += BytesPerClubHeader + clubs[i].Count * BytesPerPlayer;
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubs.Length);

            for (int c = 0; c < clubOrder.Length; c++)
            {
                ClubTrainingStates club = clubs[clubOrder[c]];
                int[] playerOrder = CanonicalOrder(RequireBound(club), "player id");

                CanonicalSerializer.WriteI32(buf, ref o, club.ClubId);
                CanonicalSerializer.WriteU32(buf, ref o, (uint)club.Count);

                for (int p = 0; p < playerOrder.Length; p++)
                {
                    int index = playerOrder[p];
                    TrainingState state = club.States[index];

                    CanonicalSerializer.WriteI32(buf, ref o, club.PlayerIds[index]);
                    CanonicalSerializer.WriteU8(buf, ref o, (byte)state.Focus);
                    CanonicalSerializer.WriteI32(buf, ref o, state.Condition);
                    CanonicalSerializer.WriteI32(buf, ref o, state.TrainingFatigue);
                    CanonicalSerializer.WriteU32(buf, ref o, state.LastAdvancedWorldDay);
                }
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "TrainingSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }

            return buf;
        }

        /// <summary>
        /// Decodes a training sub-blob produced by <see cref="Encode"/> back into its per-club blocks,
        /// in ascending club id with each club's players in ascending player id.
        /// <para>
        /// Fail-loud (throws) on: a null blob; a
        /// <see cref="TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION"/> mismatch (F3 — no Stage-0
        /// migration); a truncated read or a length prefix that would read past the blob (F5); club or
        /// player ids that are not strictly ascending (a duplicate or reordered key, which
        /// <see cref="Encode"/> cannot produce); a focus byte outside
        /// <see cref="TrainingFocus"/>; a negative training-fatigue accumulator; or trailing bytes after
        /// the declared content (F5).
        /// </para>
        /// <para>
        /// <b>What is deliberately NOT gated:</b> <see cref="TrainingState.Condition"/> against
        /// <c>ConditionMin</c>/<c>ConditionMax</c>, and training fatigue against
        /// <c>TrainingFatigueMax</c>. Those bounds are <c>[GT]</c> — a designer lowering a ceiling in
        /// config would otherwise make every existing save unloadable, turning a tuning edit into data
        /// loss. Only the structural bound (fatigue is an accumulator, so it can never be negative,
        /// §2.2) is enforced here; the <c>[GT]</c> ceilings are the day step's clamp to apply, not the
        /// codec's to police.
        /// </para>
        /// </summary>
        /// <param name="blob">The bytes to decode.</param>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Any framing, ordering, or value-contract
        /// violation listed above — all of them mean the blob is corrupt.</exception>
        public static ClubTrainingStates[] Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int len = blob.Length;
            int o = 0;

            Require(o, 4, len, "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Training save format version " + format + " != expected " +
                    TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (F3).");
            }

            int clubCount = ReadCount(blob, ref o, len, BytesPerClubHeader, "club");
            var clubs = new ClubTrainingStates[clubCount];
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubCount; c++)
            {
                Require(o, 4, len, "club id");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                RequireAscending(clubId, ref previousClubId, "club id", c);

                int playerCount = ReadCount(blob, ref o, len, BytesPerPlayer, "player");
                var playerIds = new int[playerCount];
                var states = new TrainingState[playerCount];
                long previousPlayerId = long.MinValue;

                for (int p = 0; p < playerCount; p++)
                {
                    Require(o, BytesPerPlayer, len, "player training state");

                    int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                    RequireAscending(playerId, ref previousPlayerId, "player id in club " + clubId, p);

                    byte focus = CanonicalSerializer.ReadU8(blob, ref o);
                    if (!TrainingSystemConstants.IsDefinedFocus((TrainingFocus)focus))
                    {
                        throw new InvalidOperationException(
                            "Training save player " + playerId + " has focus ordinal " + focus +
                            " — not a defined TrainingFocus; corrupt save.");
                    }

                    int condition = CanonicalSerializer.ReadI32(blob, ref o);
                    int trainingFatigue = CanonicalSerializer.ReadI32(blob, ref o);
                    if (trainingFatigue < 0)
                    {
                        throw new InvalidOperationException(
                            "Training save player " + playerId + " has training fatigue " +
                            trainingFatigue + " — the accumulator's floor is 0 (§2.2); corrupt save.");
                    }

                    uint lastAdvancedWorldDay = CanonicalSerializer.ReadU32(blob, ref o);

                    playerIds[p] = playerId;
                    states[p] = new TrainingState
                    {
                        Focus = (TrainingFocus)focus,
                        Condition = condition,
                        TrainingFatigue = trainingFatigue,
                        LastAdvancedWorldDay = lastAdvancedWorldDay,
                    };
                }

                clubs[c] = new ClubTrainingStates(clubId, playerIds, states);
            }

            // ── Trailing-byte guard (F5) ──────────────────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Training save blob has " + (len - o) + " trailing byte(s) after the declared " +
                    "content — truncated, padded, or corrupt.");
            }

            return clubs;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static int[] ClubIdsOf(ClubTrainingStates[] clubs)
        {
            var ids = new int[clubs.Length];
            for (int i = 0; i < clubs.Length; i++)
            {
                ids[i] = clubs[i].ClubId;
            }

            return ids;
        }

        // A default(ClubTrainingStates) has null arrays — Count reads 0, so it would encode as an empty
        // club rather than announcing itself. Refuse it: an unbound value in the save set is a caller
        // bug, and an empty club is a thing you have to ask for explicitly.
        private static int[] RequireBound(ClubTrainingStates club)
        {
            if (club.PlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(club),
                    "Club " + club.ClubId + " has no bound arrays (a default(ClubTrainingStates)) — " +
                    "construct it, or pass an explicitly empty one.");
            }

            return club.PlayerIds;
        }

        // Returns the indices of `keys` in ascending key order, refusing duplicates. Sorting here is
        // what makes the bytes canonical: the block is a map keyed by id (FR-TR-019), so the caller's
        // roster order is not part of the state and must not be part of the bytes. A duplicate key IS a
        // defect — there is no defined winner — so it throws rather than being deduplicated.
        private static int[] CanonicalOrder(int[] keys, string what)
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
                        "Duplicate " + what + " " + sorted[i] + " in the training save set — a " +
                        "duplicate key has no defined winner (FR-TR-025).");
                }
            }

            return order;
        }

        // Decode-side mirror of the CanonicalOrder guarantee. `previous` is a long so that the first
        // comparison can start below int.MinValue without a separate "is this the first?" flag.
        private static void RequireAscending(int value, ref long previous, string what, int index)
        {
            if (value <= previous)
            {
                throw new InvalidOperationException(
                    "Training save " + what + " at index " + index + " is " + value +
                    ", which does not exceed the preceding " + previous +
                    " — the block is written in strictly ascending key order; corrupt save.");
            }

            previous = value;
        }

        // Reads a u32 length prefix and refuses a count whose elements could not possibly be backed by
        // the bytes that remain. The bound is expressed in ELEMENTS (`remaining / bytesPerElement`)
        // rather than as a byte product, because that product can overflow int for a large blob and a
        // crafted count and wrap back to a small positive value that slips past a byte-wise guard.
        // `bytesPerElement` is the element's MINIMUM width (a club header for a club, which may then
        // carry zero players), so the bound is conservative and never rejects a well-formed blob — the
        // WorldStateSerializer.ReadCount / SeasonStateCodec posture.
        private static int ReadCount(byte[] blob, ref int o, int total, int bytesPerElement, string what)
        {
            Require(o, 4, total, what + " count");
            uint raw = CanonicalSerializer.ReadU32(blob, ref o);
            int maxCount = (total - o) / bytesPerElement;
            if (raw > (uint)maxCount)
            {
                throw new InvalidOperationException(
                    "Training save " + what + " count " + raw + " exceeds the " + maxCount +
                    " element(s) the " + (total - o) + " remaining byte(s) at offset " + o +
                    " could hold — corrupt save.");
            }

            return (int)raw;
        }

        private static void Require(int offset, int need, int total, string what)
        {
            // Overflow-safe (the MatchSaveCodec / SeasonSaveCodec posture): compare against
            // (total - offset) rather than (offset + need), since a corrupt length prefix can push
            // `need` near int.MaxValue and `offset + need` would wrap negative and slip past the guard.
            // `offset` is always in [0, total] here (every read is guarded), so `total - offset` is a
            // safe non-negative int.
            if (need < 0 || offset > total || need > total - offset)
            {
                throw new InvalidOperationException(
                    "Training save blob truncated reading " + what + " (need " + need +
                    " byte(s) at offset " + offset + " of " + total + ").");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial implementation (#29 T1, FR-TR-018/019): the TRAINING_SAVE_ |
// |         |            |        | FORMAT_VERSION sub-blob, canonical key order, the overflow-safe    |
// |         |            |        | ReadCount bound, the ascending-key decode gate, and the trailing-  |
// |         |            |        | byte guard. Layout pinned by ERR-029-004.                          |
#endregion
