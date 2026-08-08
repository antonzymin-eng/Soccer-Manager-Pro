// File:     src/injuries-medical/MedicalSaveCodec.cs
// Created:  2026-08-06
// Modified: 2026-08-08 (AR pass 14 L1: the #29/#41 non-gate asymmetry stated — v1.3)
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 (the persisted medical block), §4.2 / §4.4 (the sub-blob codec),
//           FR-MD-017/018/019, F1/F3/F4/F5; ERR-041-008 (the §4.4 layout's missing club id) and
//           ERR-041-009 (the leading magic, without which #29's block decodes here silently);
//           Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the #41 medical sub-blob — one opaque, independently version-gated block
//           under #30's season save. Encodes the per-club per-player InjuryState map in canonical key
//           order and reads it back, fail-loud on any version / length-bound / ordering / coherence /
//           trailing-byte violation. No RNG cursor exists to serialize (FR-MD-007). No file I/O (that is
//           SeasonSaveManager), so the codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// Encodes / decodes the medical sub-blob (#41 §4.4). The blob is one
    /// <see cref="InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION"/>-gated payload carrying every
    /// club's per-player <see cref="InjuryState"/>.
    /// <para><b>Layout</b> (§4.4 as corrected by ERR-041-008):</para>
    /// <code>
    /// u32  MEDICAL_SAVE_MAGIC         ("MEDL")
    /// u32  MEDICAL_SAVE_FORMAT_VERSION
    /// u32  clubCount
    /// per club, ascending ClubId:
    ///     i32  clubId
    ///     u32  playerCount
    ///     per player, ascending PlayerId:
    ///         i32  playerId
    ///         u8   severity               (InjurySeverity ordinal)
    ///         i32  recoveryRemaining
    ///         i32  injuryCount
    ///         u32  lastAdvancedWorldDay
    /// </code>
    /// <para>
    /// <b>The block says which format it is.</b> The leading
    /// <see cref="InjuriesMedicalConstants.MEDICAL_SAVE_MAGIC"/> is not decoration: every sub-blob
    /// format in the save stack sits at version 1, and the #29 training block has this block's exact
    /// byte shape, so without a magic each codec decodes the other's bytes cleanly and silently — a
    /// training focus arrives as an injury tier and a conditioning cursor as a recovery counter, with
    /// no gate tripped anywhere. A version gate cannot catch that; it distinguishes generations of one
    /// format, never one format from another. See ERR-041-009 / ERR-029-005.
    /// </para>
    /// <para>
    /// <b>The club id is written, not implied by position.</b> §4.4's sketch grouped the blocks by club
    /// without naming one, so club identity would be carried only by list order across a save boundary —
    /// an implicit agreement with a sibling blob this codec is forbidden to read (KD-2 blob
    /// independence, unified-season-save-design.md §KD-2). Four bytes per club buys a self-describing
    /// block and a duplicate check (ERR-041-008).
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> The block is a map keyed by <c>(clubId, playerId)</c>, so
    /// <see cref="Encode"/> canonicalizes to ascending keys — two equal state sets always produce
    /// identical bytes regardless of the caller's roster order — and <see cref="Decode"/> requires that
    /// order, so a hand-edited or corrupt blob cannot smuggle in a duplicate key. Encoding a decoded
    /// blob reproduces it byte for byte.
    /// </para>
    /// <para>
    /// <b>No RNG cursor is persisted</b> (KD-1 / FR-MD-007): every occurrence draw is keyed on
    /// <c>(playerId, worldDay, purpose)</c> rather than advanced from a stream, so a save/restore
    /// boundary cannot shift a future draw and there is nothing beyond <see cref="InjuryState"/> to
    /// write.
    /// </para>
    /// <para>
    /// Off the 60 Hz hot path (a save is a host action), so allocation is permitted.
    /// </para>
    /// </summary>
    public static class MedicalSaveCodec
    {
        // Layout widths, named so the size computation reads as the layout it mirrors.
        private const int BytesPerClubHeader = 4 + 4;          // clubId i32 + playerCount u32
        private const int BytesPerPlayer = 4 + 1 + 4 + 4 + 4;  // playerId, severity, recovery, count, lastDay

        /// <summary>
        /// Encodes every club's medical states into the #41 sub-blob, canonicalized to ascending club id
        /// and, within a club, ascending player id. The caller's array and its inner arrays are never
        /// mutated. The buffer is sized exactly to the content; see <see cref="Decode"/> for the inverse
        /// (kept adjacent so a layout change is edited in one place).
        /// </summary>
        /// <param name="clubs">The clubs to persist. An empty array is legal and encodes a well-formed,
        /// zero-club block — the honest state of a game that tracks no medical state yet, not a missing
        /// blob.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> is null, or a club's arrays
        /// were never bound (a <c>default(ClubInjuryStates)</c> element).</exception>
        /// <exception cref="ArgumentException">Two clubs share a club id, or two players within one club
        /// share a player id — a duplicate key has no defined winner and is a roster-lifecycle bug
        /// (FR-MD-025), never silently resolved.</exception>
        /// <exception cref="InvalidOperationException">A state being encoded violates the F1 coherence
        /// invariant (<c>RecoveryRemaining > 0</c> exactly when <c>Severity != None</c>) or carries an
        /// undefined <see cref="InjurySeverity"/> (F4). An incoherent state is a bug upstream, and
        /// writing it would launder that bug into a file that decodes back to the same contradiction.</exception>
        public static byte[] Encode(ClubInjuryStates[] clubs)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            int[] clubOrder = SaveBlobFramingHelpers.CanonicalOrder(
                ClubIdsOf(clubs), "medical", "club id", "FR-MD-025");

            int size = 4 + 4 + 4;   // magic + version + clubCount
            for (int i = 0; i < clubs.Length; i++)
            {
                size += BytesPerClubHeader + clubs[i].Count * BytesPerPlayer;
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, InjuriesMedicalConstants.MEDICAL_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubs.Length);

            for (int c = 0; c < clubOrder.Length; c++)
            {
                ClubInjuryStates club = clubs[clubOrder[c]];
                int[] playerOrder = SaveBlobFramingHelpers.CanonicalOrder(
                    RequireBound(club), "medical", "player id", "FR-MD-025");

                CanonicalSerializer.WriteI32(buf, ref o, club.ClubId);
                CanonicalSerializer.WriteU32(buf, ref o, (uint)club.Count);

                for (int p = 0; p < playerOrder.Length; p++)
                {
                    int index = playerOrder[p];
                    int playerId = club.PlayerIds[index];
                    InjuryState state = club.States[index];

                    // The F1/F4 gates run on the WAY OUT as well as the way in. A codec that only
                    // validates on decode will happily write a state no decode of it could accept —
                    // and the failure then surfaces at load, one session away from the bug.
                    RequireDefinedSeverity(state.Severity, playerId);
                    RequireCoherent(state.Severity, state.RecoveryRemaining, playerId);

                    CanonicalSerializer.WriteI32(buf, ref o, playerId);
                    CanonicalSerializer.WriteU8(buf, ref o, (byte)state.Severity);
                    CanonicalSerializer.WriteI32(buf, ref o, state.RecoveryRemaining);
                    CanonicalSerializer.WriteI32(buf, ref o, state.InjuryCount);
                    CanonicalSerializer.WriteU32(buf, ref o, state.LastAdvancedWorldDay);
                }
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "MedicalSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }

            return buf;
        }

        /// <summary>
        /// Decodes a medical sub-blob produced by <see cref="Encode"/> back into its per-club blocks, in
        /// ascending club id with each club's players in ascending player id.
        /// <para>
        /// Fail-loud (throws) on: a null blob; a
        /// <see cref="InjuriesMedicalConstants.MEDICAL_SAVE_MAGIC"/> mismatch (these bytes are some
        /// other format — checked first, since a foreign block can carry a matching version); a
        /// <see cref="InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION"/> mismatch (F3 — no Stage-0
        /// migration); a truncated read or a length prefix that would read past the blob (F5); club or
        /// player ids that are not strictly ascending (a duplicate or reordered key, which
        /// <see cref="Encode"/> cannot produce); a severity byte outside <see cref="InjurySeverity"/>
        /// (F4); a negative recovery counter or injury count; a state violating the F1 coherence
        /// invariant; or trailing bytes after the declared content (F5).
        /// </para>
        /// <para>
        /// <b>What is deliberately NOT gated:</b> <see cref="InjuryState.RecoveryRemaining"/> against
        /// <c>RecoveryMax</c>. That ceiling is <c>[GT]</c> — a designer lowering it in config would
        /// otherwise make every existing save unloadable, turning a tuning edit into data loss. Only the
        /// structural floor (a day counter cannot be negative) and the F1 coherence rule are enforced
        /// here.
        /// <para>
        /// <b>The asymmetry with #29 (AR pass 14 L1):</b> #29's day step CLAMPS its ceilings, so a
        /// loaded out-of-band counter self-heals; #41's does not — <c>MedicalStep.ValidateState</c>
        /// REFUSES <c>RecoveryRemaining &gt; RecoveryMax</c> (F1). A save loaded cleanly here under a
        /// lowered ceiling therefore throws at slot 4 on every later advance until the config is
        /// restored: loud and config-reversible, but a career-halting edit, not a self-healing one.
        /// Lowering <c>RecoveryMax</c> is career-compatible only when nothing outstanding exceeds it.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="blob">The bytes to decode.</param>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Any framing, ordering, coherence, or
        /// value-contract violation listed above — all of them mean the blob is corrupt.</exception>
        public static ClubInjuryStates[] Decode(byte[] blob)
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
            // handed me the training block" (ERR-041-009).
            SaveBlobFramingHelpers.Require(o, 4, len, "Medical save", "format magic");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != InjuriesMedicalConstants.MEDICAL_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    "Medical save magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    InjuriesMedicalConstants.MEDICAL_SAVE_MAGIC.ToString("X8") +
                    " — these bytes are not a #41 medical block. A block of another sub-blob format " +
                    "(the #29 training block has this block's exact byte shape) would otherwise decode " +
                    "cleanly and silently.");
            }

            SaveBlobFramingHelpers.Require(o, 4, len, "Medical save", "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Medical save format version " + format + " != expected " +
                    InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (F3).");
            }

            int clubCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, BytesPerClubHeader, "Medical save", "club");
            var clubs = new ClubInjuryStates[clubCount];
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubCount; c++)
            {
                SaveBlobFramingHelpers.Require(o, 4, len, "Medical save", "club id");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(clubId, ref previousClubId, "Medical save", "club id", c);

                int playerCount = SaveBlobFramingHelpers.ReadCount(
                    blob, ref o, len, BytesPerPlayer, "Medical save", "player");
                var playerIds = new int[playerCount];
                var states = new InjuryState[playerCount];
                long previousPlayerId = long.MinValue;

                for (int p = 0; p < playerCount; p++)
                {
                    SaveBlobFramingHelpers.Require(o, BytesPerPlayer, len, "Medical save", "player medical state");

                    int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                    SaveBlobFramingHelpers.RequireAscending(
                        playerId, ref previousPlayerId, "Medical save", "player id in club " + clubId, p);

                    var severity = (InjurySeverity)CanonicalSerializer.ReadU8(blob, ref o);
                    RequireDefinedSeverity(severity, playerId);

                    int recoveryRemaining = CanonicalSerializer.ReadI32(blob, ref o);
                    if (recoveryRemaining < 0)
                    {
                        throw new InvalidOperationException(
                            "Medical save player " + playerId + " has recovery remaining " +
                            recoveryRemaining + " — a day counter's floor is 0 (§2.2); corrupt save.");
                    }

                    int injuryCount = CanonicalSerializer.ReadI32(blob, ref o);
                    if (injuryCount < 0)
                    {
                        throw new InvalidOperationException(
                            "Medical save player " + playerId + " has injury count " + injuryCount +
                            " — a cumulative career count cannot be negative; corrupt save.");
                    }

                    RequireCoherent(severity, recoveryRemaining, playerId);

                    uint lastAdvancedWorldDay = CanonicalSerializer.ReadU32(blob, ref o);

                    playerIds[p] = playerId;
                    states[p] = new InjuryState
                    {
                        Severity = severity,
                        RecoveryRemaining = recoveryRemaining,
                        InjuryCount = injuryCount,
                        LastAdvancedWorldDay = lastAdvancedWorldDay,
                    };
                }

                clubs[c] = new ClubInjuryStates(clubId, playerIds, states);
            }

            // ── Trailing-byte guard (F5) ──────────────────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Medical save blob has " + (len - o) + " trailing byte(s) after the declared " +
                    "content — truncated, padded, or corrupt.");
            }

            return clubs;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static int[] ClubIdsOf(ClubInjuryStates[] clubs)
        {
            var ids = new int[clubs.Length];
            for (int i = 0; i < clubs.Length; i++)
            {
                ids[i] = clubs[i].ClubId;
            }

            return ids;
        }

        // A default(ClubInjuryStates) has null arrays — Count reads 0, so it would encode as an empty
        // club rather than announcing itself. Refuse it: an unbound value in the save set is a caller
        // bug, and an empty club is a thing you have to ask for explicitly.
        private static int[] RequireBound(ClubInjuryStates club)
        {
            if (club.PlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(club),
                    "Club " + club.ClubId + " has no bound arrays (a default(ClubInjuryStates)) — " +
                    "construct it, or pass an explicitly empty one.");
            }

            return club.PlayerIds;
        }

        private static void RequireDefinedSeverity(InjurySeverity severity, int playerId)
        {
            if (!InjuriesMedicalConstants.IsDefinedSeverity(severity))
            {
                throw new InvalidOperationException(
                    "Medical save player " + playerId + " has severity ordinal " + (byte)severity +
                    " — not a defined InjurySeverity (F4); corrupt save.");
            }
        }

        // F1: RecoveryRemaining > 0 exactly when Severity != None. A healthy player with days left, or
        // an injured one with none, is a contradiction with no safe repair — fail loud at both ends.
        private static void RequireCoherent(InjurySeverity severity, int recoveryRemaining, int playerId)
        {
            bool injured = severity != InjurySeverity.None;
            if ((recoveryRemaining > 0) != injured)
            {
                throw new InvalidOperationException(
                    "Medical save player " + playerId + " has severity " + severity +
                    " with recovery remaining " + recoveryRemaining +
                    " — RecoveryRemaining is positive if and only if the player is injured (F1); " +
                    "incoherent state.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial implementation (#41 T1, FR-MD-017/018/019): the MEDICAL_   |
// |         |            |        | SAVE_FORMAT_VERSION sub-blob, canonical key order, the overflow-   |
// |         |            |        | safe ReadCount bound, the F1 coherence gate on BOTH sides, and the |
// |         |            |        | trailing-byte guard. Layout corrected by ERR-041-008.              |
// | 1.1     | 2026-08-06 | —      | ERR-041-009 (AR pass 1, H): the block gains a leading MEDICAL_     |
// |         |            |        | SAVE_MAGIC, checked before the version. Without it the #29         |
// |         |            |        | training block — same byte shape, same version 1 — decoded here    |
// |         |            |        | cleanly and silently, turning focuses into injury tiers.           |
// | 1.2     | 2026-08-06 | —      | Review fix (L): the class doc's blob-independence citation         |
// |         |            |        | corrected from "KD-7" to KD-2 (unified-season-save-design.md       |
// |         |            |        | §KD-2) — that document's KD-7 is "codec split from disk I/O", a    |
// |         |            |        | different decision. Review fix (M): CanonicalOrder /               |
// |         |            |        | RequireAscending / ReadCount / Require moved to the new shared     |
// |         |            |        | TacticalDirector.DeterministicSim.SaveBlobFramingHelpers — this    |
// |         |            |        | file and TrainingSaveCodec were byte-identical duplicates of those |
// |         |            |        | four helpers with nothing mechanical keeping them in sync.         |
// | 1.3     | 2026-08-08 | —      | Balance-pass AR pass 14 (L1, doc): the non-gate rationale         |
// |         |            |        | inherited #29's clamp claim — #41's day step REFUSES an           |
// |         |            |        | out-of-band counter, so a lowered ceiling halts a career loudly   |
// |         |            |        | rather than self-healing; the asymmetry stated.                   |
#endregion
