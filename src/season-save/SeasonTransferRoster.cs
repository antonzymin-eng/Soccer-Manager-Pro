// File:     src/season-save/SeasonTransferRoster.cs
// Created:  2026-10-19
// Modified: 2026-10-20
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §3.4 / §4.5 / KD-7 (as amended by ERR-031-005/007),
//           §7.1 T2, FR-TX-021..023; Season & Competition Loop #30 (the mid-season roster commit,
//           ERR-030-005); Player Progression #28 KD-4 (the store is the roster); Code Standards #20
// Purpose:  The production #30 roster owner behind #31's ITransferRosterPort: reads ownership, stock and
//           capacity off the #28 store (the roster authority, KD-4) and re-homes a player — record,
//           lifecycle and his #29/#41/#30 career rows — under his unchanged PlayerId.

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.Transfers;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// #30's implementation of #31's consumer-owned roster port (§3.4 / §7.1 T2). Composition-root code:
    /// #31 never references #30, so the adapter lives here and is handed to <c>TransferCommands</c>.
    /// <para>
    /// <b>Preview ⇒ commit is infallible by construction.</b> <see cref="TryPreviewRosterCommit"/> asks
    /// exactly the predicates <see cref="RequestRosterCommit"/>'s two writers require
    /// (<see cref="ProgressionEngine.CanMovePlayer"/> and, when a career is wired,
    /// <see cref="PlayerCareerStates.CanMovePlayerRows"/>), and nothing mutates the roster between them
    /// inside <c>SubmitBid</c>. The commit re-checks both before either write, so even a caller that
    /// skips the preview cannot leave the store moved and the career rows behind.
    /// </para>
    /// <para>
    /// <b>No PlayerId-keyed state migrates</b> (ERR-031-005): the id is unchanged, so #44's tally, #41's
    /// draw key and #31's contracts follow the player by construction. Only the per-club rows move —
    /// the #28 record + lifecycle and the #29/#41/#30 rows — because those are keyed by club too.
    /// </para>
    /// </summary>
    internal sealed class SeasonTransferRoster : ITransferRosterPort
    {
        private readonly ProgressionEngine _progression;
        private readonly PlayerCareerStates _careerOrNull;

        /// <summary>Adapts the loop's roster authority (and, when wired, its career rows).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="progression"/> is null — transfers
        /// re-home records, so they need the #28 store that owns them (KD-4).</exception>
        internal SeasonTransferRoster(ProgressionEngine progression, PlayerCareerStates careerOrNull)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _careerOrNull = careerOrNull;
        }

        /// <inheritdoc />
        public bool TryGetPlayer(int playerId, out PlayerRecord player, out int clubId) =>
            _progression.TryFindPlayer(playerId, out player, out clubId);

        /// <inheritdoc />
        public bool CarriesClub(int clubId) => _progression.CarriesClub(clubId);

        /// <inheritdoc />
        public int CountPlayersAtPosition(int clubId, PlayerPosition position)
        {
            Squad squad = _progression.SquadFor(clubId);
            if (squad == null)
            {
                throw new ArgumentException($"No roster for club {clubId}.", nameof(clubId));
            }

            int count = 0;
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).Position == position)
                {
                    count++;
                }
            }

            return count;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <c>false</c> only when the club left behind could not name a matchday squad (ERR-031-007). An
        /// unknown club, or a player who is not on that club's roster, is an integration defect and throws —
        /// it must never surface to the manager as "squad depleted".
        /// </remarks>
        public bool CanRelease(int clubId, int playerId)
        {
            Squad squad = _progression.SquadFor(clubId);
            if (squad == null)
            {
                throw new InvalidOperationException($"CanRelease asked about club {clubId}, which is not in this league.");
            }

            var remaining = new PlayerRecord[squad.Count > 0 ? squad.Count - 1 : 0];
            int w = 0;
            bool found = false;
            for (int i = 0; i < squad.Count; i++)
            {
                PlayerRecord rec = squad.GetPlayer(i);
                if (rec.PlayerId == playerId)
                {
                    found = true;
                    continue;
                }

                if (w < remaining.Length)
                {
                    remaining[w++] = rec;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException($"CanRelease asked about player {playerId}, who is not on club {clubId}'s roster.");
            }

            // ERR-031-007: the club left behind must still be able to name a matchday squad — the rule
            // #30 §2.3 F9's back-fill relies on. Asked of the engine's own selector, never a head count.
            return remaining.Length > 0 && SquadRating.CanFieldStartingEleven(new Squad(clubId, remaining));
        }

        /// <inheritdoc />
        /// <remarks>
        /// ERR-031-010: <c>false</c> only when the destination roster is at <c>CLUB_SQUAD_SIZE</c>; any
        /// other impossibility (unknown club, player not on the source, career rows disagreeing with the
        /// store) is an integration defect and throws, so it can never reach the manager as "squad full".
        /// </remarks>
        public bool TryPreviewRosterCommit(int fromClubId, int toClubId, int playerId)
        {
            if (!_progression.CarriesClub(fromClubId) || !_progression.CarriesClub(toClubId) || fromClubId == toClubId
                || !_progression.TryFindPlayer(playerId, out PlayerRecord _, out int owner) || owner != fromClubId)
            {
                throw new InvalidOperationException(
                    $"Preview of an impossible move: player {playerId} from club {fromClubId} to club {toClubId} "
                    + "(unknown club, same club, or the player is not on the source roster).");
            }

            Squad destination = _progression.SquadFor(toClubId);
            if (destination.Count >= PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                return false;
            }

            // Below capacity, the move MUST be possible in both writers — if either refuses, the store and
            // the career disagree about who is where, which is a defect, not a full squad.
            if (!_progression.CanMovePlayer(playerId, fromClubId, toClubId)
                || (_careerOrNull != null && !_careerOrNull.CanMovePlayerRows(playerId, fromClubId, toClubId)))
            {
                throw new InvalidOperationException(
                    $"The #28 store and the #29/#41/#30 career rows disagree about player {playerId}'s move.");
            }

            return true;
        }

        /// <inheritdoc />
        public void RequestRosterCommit(int fromClubId, int toClubId, int playerId)
        {
            // Both predicates before either write: a breach throws with nothing moved.
            if (!_progression.CanMovePlayer(playerId, fromClubId, toClubId)
                || (_careerOrNull != null && !_careerOrNull.CanMovePlayerRows(playerId, fromClubId, toClubId)))
            {
                throw new InvalidOperationException(
                    $"Roster commit of player {playerId} from club {fromClubId} to club {toClubId} was not "
                    + "previewable — RequestRosterCommit requires a successful preview (#31 §3.4).");
            }

            _progression.MovePlayer(playerId, fromClubId, toClubId);
            _careerOrNull?.MovePlayerRows(playerId, fromClubId, toClubId);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-19 | —      | Initial implementation (#31 T2): the production roster port over |
// |         |            |        | the #28 store + #29/#41/#30 career rows; stable-id re-home       |
// |         |            |        | (ERR-031-005); source-depletion guard (ERR-031-007); preview     |
// |         |            |        | false only when full, impossible moves throw (ERR-031-010).      |
// | 1.1     | 2026-10-20 | —      | AR round 2 L3: CanRelease throws for an unknown club or a player not on the roster (false is depletion only, ITransferRosterPort contract). |
#endregion
