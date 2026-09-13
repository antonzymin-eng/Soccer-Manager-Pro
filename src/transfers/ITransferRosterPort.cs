// ============================================================================
// File:     src/transfers/ITransferRosterPort.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.5.1-§3.5.3 (specified consumer-owned interface)
//           Spec #31 §3.3-§3.4, KD-7 / FR-TX-021..023 (future #30 roster commit seam)
// Purpose:  Declares the #31-owned consumer port that T2 composition will adapt to #30 roster ownership.
// ============================================================================

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>
    /// Consumer-owned seam for reading a player and preflighting/committing the #30-owned roster re-key.
    /// The T2 producer is already specified by #30/#31; T0 supplies no season-loop implementation.
    /// </summary>
    public interface ITransferRosterPort
    {
        /// <summary>Reads the current canonical player record without mutation.</summary>
        bool TryGetPlayer(int playerId, out PlayerRecord player);

        /// <summary>
        /// Validates source ownership and destination capacity without mutation and returns the exact new
        /// player id that a subsequent commit will allocate.
        /// </summary>
        bool TryPreviewRosterCommit(int fromClubId, int toClubId, int playerId, out int newPlayerId);

        /// <summary>
        /// Performs the previously previewed roster move. After a successful preview this operation must be
        /// infallible and must return the same new player id, preserving #31's validate-all-first transaction.
        /// </summary>
        int RequestRosterCommit(int fromClubId, int toClubId, int playerId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 consumer port for the already-specified #30 T2 roster seam. |
#endregion
