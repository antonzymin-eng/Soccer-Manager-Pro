// File:     src/season-save/SeasonTransfersCoherence.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §4.4 / FR-TX-012/023/028, §7.1 T1;
//           Season & Competition Loop #30 Appendix B.1 (cross-blob rules); Code Standards #20
// Purpose:  The ONE owner of the cross-blob rule between the #31 transfers block and the rest of a
//           career: its managed club is the season's managed club, and every managed contract names a
//           player that club's roster actually carries. Called at loop composition, Save and Load, so
//           none of the three can drift (the SeasonFinanceCoherence posture).

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.Transfers;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Cross-blob coherence between #31 state and the season it rides with. The transfers codec can only
    /// check what its own block says; whether those contracts belong to THIS career needs the season
    /// (managed club) and the roster, which only the season root holds.
    /// </summary>
    internal static class SeasonTransfersCoherence
    {
        /// <summary>
        /// Refuses a #31 state that does not describe this season's career. A null state (no #31 wired)
        /// is always coherent.
        /// </summary>
        /// <param name="season">The season the state rides with.</param>
        /// <param name="transfersOrNull">The #31 state, or null.</param>
        /// <param name="rosterOrNull">The roster authority (the #28 projection or a bootstrap provider), or
        /// null when the caller holds none — then only the managed-club rule can be checked.</param>
        /// <param name="paramName">The caller's parameter name for the exception.</param>
        /// <exception cref="ArgumentException">The managed club differs, or a contract names a player the
        /// managed club's roster does not carry.</exception>
        internal static void Require(
            SeasonState season, TransfersState transfersOrNull, ISquadProvider rosterOrNull, string paramName)
        {
            if (transfersOrNull == null)
            {
                return;
            }

            if (transfersOrNull.ManagedClubId != season.ManagedClubId)
            {
                throw new ArgumentException(
                    $"The #31 transfers state belongs to managed club {transfersOrNull.ManagedClubId}, but this "
                    + $"season's managed club is {season.ManagedClubId}. Contracts are the managed club's "
                    + "(Spec #31 §2.2), so the two blocks describe different careers.",
                    paramName);
            }

            if (rosterOrNull == null)
            {
                return;
            }

            Squad squad = rosterOrNull.ResolveByClubId(season.ManagedClubId);
            if (squad == null)
            {
                throw new ArgumentException(
                    $"The roster carries no squad for managed club {season.ManagedClubId}, so its #31 "
                    + "contracts cannot be checked against it.",
                    paramName);
            }

            Contract[] contracts = transfersOrNull.ContractsSnapshot();
            for (int i = 0; i < contracts.Length; i++)
            {
                if (!SquadCarries(squad, contracts[i].PlayerId))
                {
                    throw new ArgumentException(
                        $"The #31 transfers state holds a contract for player {contracts[i].PlayerId}, whom "
                        + $"managed club {season.ManagedClubId}'s roster does not carry — a contract for a "
                        + "departed or foreign player (FR-TX-023/028).",
                        paramName);
                }
            }
        }

        private static bool SquadCarries(Squad squad, int playerId)
        {
            for (int p = 0; p < squad.Count; p++)
            {
                if (squad.GetPlayer(p).PlayerId == playerId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-18 | —      | Initial implementation (#31 T1): managed-club + contract-roster  |
// |         |            |        | membership rule, one owner for composition/Save/Load.            |
#endregion
