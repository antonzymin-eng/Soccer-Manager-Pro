// ============================================================================
// File:     src/transfers/ITransferRosterPort.cs
// Created:  2026-09-12
// Modified: 2026-10-19
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

        /// <summary>Counts a club's current players in the requested coarse #27 position, without mutation.</summary>
        int CountPlayersAtPosition(int clubId, PlayerPosition position);

        /// <summary>
        /// Validates, without mutation, that <paramref name="playerId"/> is on
        /// <paramref name="fromClubId"/>'s roster and that <paramref name="toClubId"/>'s roster has room
        /// below <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>. False means the destination is
        /// full (F5, an ordinary <see cref="TransferSubmissionOutcome.SquadFull"/>).
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
#endregion
