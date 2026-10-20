// ============================================================================
// File:     src/transfers/ITransferRosterPort.cs
// Created:  2026-09-12
// Modified: 2026-10-20
// Author:   —
// Specs:    Spec #20 §3.5.1-§3.5.3 (specified consumer-owned interface)
//           Spec #31 §3.1, §3.3-§3.4, KD-1/KD-7 / FR-TX-001..003/021..023 (as amended by ERR-031-005)
// Purpose:  Declares the #31-owned consumer port that #30's roster owner implements: read-only player,
//           club-membership and positional-stock queries plus the preflighted, infallible re-home.
// ============================================================================

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>
    /// Consumer-owned seam for read-only roster context plus preflighting/committing a #30-owned
    /// re-home. <b>PlayerIds are stable across a transfer</b> (ERR-031-005, owner decision October 19,
    /// 2026): a committed transfer moves the player's record to the destination club under the SAME id,
    /// so no PlayerId-keyed state anywhere needs migrating. Club membership is a roster fact this port
    /// answers; it is never derived from the id.
    /// </summary>
    public interface ITransferRosterPort
    {
        /// <summary>
        /// Reads the current canonical player record and the club whose roster carries him, without
        /// mutation. False when no club's roster carries <paramref name="playerId"/>.
        /// </summary>
        bool TryGetPlayer(int playerId, out PlayerRecord player, out int clubId);

        /// <summary>Whether <paramref name="clubId"/> is a club of this league's universe (F6, ERR-031-010).</summary>
        bool CarriesClub(int clubId);

        /// <summary>Counts a club's current players in the requested coarse #27 position, without mutation.</summary>
        int CountPlayersAtPosition(int clubId, PlayerPosition position);

        /// <summary>
        /// Whether <paramref name="clubId"/> can let <paramref name="playerId"/> go and still name a
        /// matchday squad (ERR-031-007). <b><c>false</c> means ONLY that</b> — the ordinary
        /// <see cref="TransferSubmissionOutcome.SquadDepleted"/>. A <paramref name="clubId"/> outside the
        /// league, or a <paramref name="playerId"/> who is not on that club's roster, is an integration
        /// defect and MUST throw <see cref="System.InvalidOperationException"/> (ERR-031-012), never answer
        /// <c>false</c>, which would be reported to the manager as a depleted squad.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Unknown club, or the player is not on it.</exception>
        bool CanRelease(int clubId, int playerId);

        /// <summary>
        /// Answers, without mutation, whether <paramref name="toClubId"/>'s roster has room below
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>. <b><c>false</c> means ONLY "destination
        /// full"</b> (F5, the ordinary <see cref="TransferSubmissionOutcome.SquadFull"/>). Everything else
        /// that would make the commit impossible — the player not on <paramref name="fromClubId"/>, either
        /// club outside the league — is an integration defect and MUST throw
        /// <see cref="System.InvalidOperationException"/>, never answer <c>false</c> (ERR-031-010: a
        /// <c>false</c> there would be reported to the manager as a full squad).
        /// </summary>
        bool TryPreviewRosterCommit(int fromClubId, int toClubId, int playerId);

        /// <summary>
        /// Re-homes the previously previewed player to <paramref name="toClubId"/> under his unchanged
        /// id. After a successful preview with no intervening roster mutation this operation must be
        /// infallible, preserving #31's validate-all-first transaction; #31 verifies the move landed
        /// (<see cref="TryGetPlayer"/> reports the destination) before applying any local state.
        /// </summary>
        void RequestRosterCommit(int fromClubId, int toClubId, int playerId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 consumer port for the already-specified #30 T2 roster seam. |
// | 1.1     | 2026-09-14 | —      | Add read-only positional-stock query for always-on T0 club-need valuation. |
// | 1.2     | 2026-10-19 | —      | ERR-031-005 (owner decision): stable PlayerIds. TryGetPlayer reports the owning club; the preview no longer allocates an id; the commit re-homes under the same id and returns nothing. |
// | 1.3     | 2026-10-20 | —      | #31 T2: CarriesClub (F6 counterparty universe, ERR-031-010) and CanRelease (source keeps a matchday squad, ERR-031-007); the preview's false means only 'destination full' — other failures throw (ERR-031-010). |
// | 1.4     | 2026-10-20 | —      | AR round 3 L3 (ERR-031-012): CanRelease's false means ONLY 'cannot field a matchday squad'; an unknown club or a player not on the roster throws. |
#endregion
