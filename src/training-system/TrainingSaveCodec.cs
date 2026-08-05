// File:     src/training-system/TrainingSaveCodec.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Training System #29 §2.2 (the persisted training block), §4.2 / §4.4 (the sub-blob codec),
//           FR-TR-018/019, F3/F5; ERR-029-004 (the §4.4 layout that was never pinned) and
//           ERR-029-005 (the leading magic, without which #41's block decodes here silently);
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
    /// u32  TRAINING_SAVE_MAGIC        ("TRNG")
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
    /// <b>The block says which format it is.</b> The leading
    /// <see cref="TrainingSystemConstants.TRAINING_SAVE_MAGIC"/> is not decoration: every sub-blob
    /// format in the save stack sits at version 1, and the #41 medical block has this block's exact
    /// byte shape, so without a magic each codec decodes the other's bytes cleanly and silently — an
    /// injury tier arrives as a training focus and a recovery counter as a conditioning cursor, with
    /// no gate tripped anywhere. A version gate cannot catch that; it distinguishes generations of one
    /// format, never one format from another. See ERR-029-005 / ERR-041-009.
    /// </para>
    /// <para>
    /// <b>The club id is written, not implied by position.</b> #41's §4.4 sketch grouped the blocks by
    /// club without naming one, which leaves club identity carried only by list order across a save
    /// boundary — an implicit agreement with a sibling blob that the codec is explicitly forbidden to
    /// read (KD-2 blob independence, unified-season-save-design.md §KD-2). Four bytes per club buys a
    /// self-describing block and a duplicate check. See ERR-029-004 / ERR-041-008.
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
        /// <exception cref="InvalidOperationException">A state being encoded carries an undefined
        /// <see cref="TrainingFocus"/> ordinal, or a negative <see cref="TrainingState.TrainingFatigue"/>
        /// — both are contracts <see cref="Decode"/> itself refuses, so writing either would produce a
        /// file no load of it could accept.</exception>
        public static byte[] Encode(ClubTrainingStates[] clubs)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            int[] clubOrder = SaveBlobFramingHelpers.CanonicalOrder(
                ClubIdsOf(clubs), "training", "club id", "FR-TR-025");

            int size = 4 + 4 + 4;   // magic + version + clubCount
            for (int i = 0; i < clubs.Length; i++)
            {
                size += BytesPerClubHeader + clubs[i].Count * BytesPerPlayer;
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, TrainingSystemConstants.TRAINING_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubs.Length);

            for (int c = 0; c < clubOrder.Length; c++)
            {
                ClubTrainingStates club = clubs[clubOrder[c]];
                int[] playerOrder = SaveBlobFramingHelpers.CanonicalOrder(
                    RequireBound(club), "training", "player id", "FR-TR-025");

                CanonicalSerializer.WriteI32(buf, ref o, club.ClubId);
                CanonicalSerializer.WriteU32(buf, ref o, (uint)club.Count);

                for (int p = 0; p < playerOrder.Length; p++)
                {
                    int index = playerOrder[p];
                    int playerId = club.PlayerIds[index];
                    TrainingState state = club.States[index];

                    // The focus and fatigue gates run on the WAY OUT as well as the way in. A codec
                    // that only validates on decode will happily write a state no decode of it could
                    // accept — and the failure then surfaces at load, one session away from the bug.
                    RequireDefinedFocus(state.Focus, playerId);
                    RequireNonNegativeFatigue(state.TrainingFatigue, playerId);

                    CanonicalSerializer.WriteI32(buf, ref o, playerId);
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
        /// <see cref="TrainingSystemConstants.TRAINING_SAVE_MAGIC"/> mismatch (these bytes are some
        /// other format — checked first, since a foreign block can carry a matching version); a
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

            // The magic is checked BEFORE the version, because a foreign block can easily carry a
            // matching version (every sub-blob format in the stack is at version 1) and the resulting
            // "version 1 == version 1, proceed" is exactly the silent mis-decode this gate exists to
            // stop. Naming the observed magic in the message turns a mystery corruption into "you
            // handed me the medical block" (ERR-029-005).
            SaveBlobFramingHelpers.Require(o, 4, len, "Training save", "format magic");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != TrainingSystemConstants.TRAINING_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    "Training save magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    TrainingSystemConstants.TRAINING_SAVE_MAGIC.ToString("X8") +
                    " — these bytes are not a #29 training block. A block of another sub-blob format " +
                    "(the #41 medical block has this block's exact byte shape) would otherwise decode " +
                    "cleanly and silently.");
            }

            SaveBlobFramingHelpers.Require(o, 4, len, "Training save", "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Training save format version " + format + " != expected " +
                    TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (F3).");
            }

            int clubCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, BytesPerClubHeader, "Training save", "club");
            var clubs = new ClubTrainingStates[clubCount];
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubCount; c++)
            {
                SaveBlobFramingHelpers.Require(o, 4, len, "Training save", "club id");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(clubId, ref previousClubId, "Training save", "club id", c);

                int playerCount = SaveBlobFramingHelpers.ReadCount(
                    blob, ref o, len, BytesPerPlayer, "Training save", "player");
                var playerIds = new int[playerCount];
                var states = new TrainingState[playerCount];
                long previousPlayerId = long.MinValue;

                for (int p = 0; p < playerCount; p++)
                {
                    SaveBlobFramingHelpers.Require(o, BytesPerPlayer, len, "Training save", "player training state");

                    int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                    SaveBlobFramingHelpers.RequireAscending(
                        playerId, ref previousPlayerId, "Training save", "player id in club " + clubId, p);

                    var focus = (TrainingFocus)CanonicalSerializer.ReadU8(blob, ref o);
                    RequireDefinedFocus(focus, playerId);

                    int condition = CanonicalSerializer.ReadI32(blob, ref o);
                    int trainingFatigue = CanonicalSerializer.ReadI32(blob, ref o);
                    RequireNonNegativeFatigue(trainingFatigue, playerId);

                    uint lastAdvancedWorldDay = CanonicalSerializer.ReadU32(blob, ref o);

                    playerIds[p] = playerId;
                    states[p] = new TrainingState
                    {
                        Focus = focus,
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

        // Both gates run on the WAY OUT (Encode) as well as the way IN (Decode) — see the comment at the
        // Encode call site. A codec that only validates on decode will happily write a state no decode
        // of it could accept, and the failure then surfaces at load, one session away from the bug.

        private static void RequireDefinedFocus(TrainingFocus focus, int playerId)
        {
            if (!TrainingSystemConstants.IsDefinedFocus(focus))
            {
                throw new InvalidOperationException(
                    "Training save player " + playerId + " has focus ordinal " + (byte)focus +
                    " — not a defined TrainingFocus; corrupt save.");
            }
        }

        private static void RequireNonNegativeFatigue(int trainingFatigue, int playerId)
        {
            if (trainingFatigue < 0)
            {
                throw new InvalidOperationException(
                    "Training save player " + playerId + " has training fatigue " + trainingFatigue +
                    " — the accumulator's floor is 0 (§2.2); corrupt save.");
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
// | 1.1     | 2026-08-06 | —      | ERR-029-005 (AR pass 1, H): the block gains a leading TRAINING_    |
// |         |            |        | SAVE_MAGIC, checked before the version. Without it the #41 medical |
// |         |            |        | block — same byte shape, same version 1 — decoded here cleanly and |
// |         |            |        | silently, turning injury tiers into training focuses.              |
// | 1.2     | 2026-08-06 | —      | Review fix (M): Encode now runs the same focus-ordinal and         |
// |         |            |        | non-negative-fatigue gates Decode already ran, so a caller cannot  |
// |         |            |        | write a state its own Decode would refuse (RequireDefinedFocus /   |
// |         |            |        | RequireNonNegativeFatigue, shared by both directions). Review fix  |
// |         |            |        | (M): CanonicalOrder / RequireAscending / ReadCount / Require moved |
// |         |            |        | to the new shared TacticalDirector.DeterministicSim.               |
// |         |            |        | SaveBlobFramingHelpers — this file and MedicalSaveCodec were byte- |
// |         |            |        | identical duplicates of those four helpers with nothing mechanical |
// |         |            |        | keeping them in sync.                                              |
#endregion
