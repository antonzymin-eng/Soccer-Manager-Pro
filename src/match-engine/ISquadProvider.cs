// File:     src/match-engine/ISquadProvider.cs
// Created:  2026-07-20
// Author:   —
// Spec:     Snapshot-deserialize design note (docs/tracking/snapshot-deserialize-design.md) §5 Phase 2
//           (KD-3 distinct-squad re-projection); Squad/Player Data Layer #27 T3; Code Standards #20
// Purpose:  The caller-supplied roster resolver the RestoreFromSnapshot factory threads into the #27 T3
//           distinct-squad re-projection. A snapshot carries only each team's roster IDENTITY (its
//           Squad.ClubId, serialized at v16), not the per-slot attribute VALUES (the boot-constant
//           exclusion); to restore attribute fidelity the reader re-projects from the actual Squad, which
//           the caller resolves here. Both producer (the factory) and consumer (this) are specified, so
//           the interface is not a phantom (CLAUDE.md "Interface Design Principle").

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Resolves a <see cref="TacticalDirector.PlayerDatabase.Squad"/> from the <c>ClubId</c> a snapshot
    /// recorded for a team (<c>_rosterClubId</c>, v16). Supplied to
    /// <see cref="MatchEngine.RestoreFromSnapshot(in DeterministicSim.SnapshotHeader, DeterministicSim.SnapshotPayload, ulong, ISquadProvider)"/>
    /// so a distinct-squad match restores its per-slot attribute records by re-projecting them from the
    /// resolved roster (#27 T3 / snapshot-deserialize KD-3).
    /// <para>
    /// <b>Contract:</b> the returned <see cref="TacticalDirector.PlayerDatabase.Squad"/> must be the SAME
    /// roster (identical players, attributes, and <c>PlayerId</c>s) that the saved match loaded through
    /// <see cref="MatchEngine.ConfigureSquads"/>. Restore re-runs the deterministic lineup selection and
    /// attribute projection against it, so any difference in the roster diverges the restored match from
    /// the saved one on the next tick (a broken round-trip, exactly the fidelity gap KD-3 refuses to paper
    /// over). Return <c>null</c> for an unknown <paramref name="clubId"/> — the factory fails loud rather
    /// than silently substituting a default roster.
    /// </para>
    /// </summary>
    public interface ISquadProvider
    {
        /// <summary>
        /// Returns the club roster whose <see cref="TacticalDirector.PlayerDatabase.Squad.ClubId"/> equals
        /// <paramref name="clubId"/>, or <c>null</c> if no such roster is known.
        /// </summary>
        /// <param name="clubId">The <c>ClubId</c> a snapshot recorded for a team (never the
        /// <see cref="MatchEngineConstants.NO_ROSTER_CLUB_ID"/> sentinel — the factory consults the provider
        /// only for teams with a distinct roster).</param>
        TacticalDirector.PlayerDatabase.Squad ResolveByClubId(int clubId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-20 | —      | Initial: the ISquadProvider seam for snapshot-deserialize      |
// |         |            |        | Phase 2 (KD-3) — the caller-supplied ClubId -> Squad resolver   |
// |         |            |        | the RestoreFromSnapshot factory uses to re-project distinct     |
// |         |            |        | squads (#27 T3).                                                |
#endregion
