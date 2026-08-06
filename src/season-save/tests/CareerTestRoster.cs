// File:     src/season-save/tests/CareerTestRoster.cs
// Created:  2026-08-06
// Author:   —
// Spec:     Training System #29 FR-TR-025; Injuries & Medical #41 FR-MD-025; path-to-playable D2/D3
//           (T2); Code Standards #20
// Purpose:  Test rosters for the #29/#41 T2 fixtures — a position-coherent squad builder and a squad
//           provider whose rosters can be changed between calls, which is what makes the season-
//           boundary roster reconciliation testable at all before #28's regen/retire churn exists.

using System.Collections.Generic;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// Roster fixtures for the career-state suites.
    /// </summary>
    internal static class CareerTestRoster
    {
        /// <summary>The fewest players <c>ConfigureSquads</c> accepts — starters plus bench.</summary>
        internal static int MinimumSquad =>
            MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;

        /// <summary>
        /// Coarse position for a position-coherent roster in the KD-L5 order lineup selection
        /// reproduces: slot 0 goalkeeper, 1–4 defenders, 5–8 midfielders, 9–10 forwards, the rest a
        /// position-varied bench.
        /// </summary>
        internal static PlayerPosition PosFor(int localIndex)
        {
            if (localIndex == 0)  return PlayerPosition.Goalkeeper;
            if (localIndex <= 4)  return PlayerPosition.Defender;
            if (localIndex <= 8)  return PlayerPosition.Midfielder;
            if (localIndex <= 10) return PlayerPosition.Forward;
            switch ((localIndex - 11) % 3)
            {
                case 0:  return PlayerPosition.Defender;
                case 1:  return PlayerPosition.Midfielder;
                default: return PlayerPosition.Forward;
            }
        }

        /// <summary>
        /// A squad of <paramref name="count"/> players for <paramref name="clubId"/>.
        /// <para>
        /// <b>Two indices, and only one of them is <paramref name="localIndices"/>.</b> Roster SLOT
        /// <c>k</c> (0..count-1) decides the player's <see cref="PlayerPosition"/> (<see cref="PosFor"/>)
        /// and his <c>Pace</c>; <paramref name="localIndices"/> decides only his <c>PlayerId</c>, as
        /// <c>clubId * CLUB_SQUAD_SIZE + localIndices[k]</c>, defaulting to <c>k</c> when not supplied.
        /// That split is what makes a one-out/one-in roster swap expressible — a club already at
        /// CLUB_SQUAD_SIZE cannot simply grow — while keeping the squad position-coherent: substituting
        /// an id at slot <c>k</c> leaves a player of the same position and rating in that slot.
        /// </para>
        /// <para>
        /// Every attribute is mid-range except <c>Pace</c>, which ramps with the roster slot (saturating
        /// at <c>ATTRIBUTE_MAX</c> beyond slot 19) — enough for lineup selection to have a strict
        /// preference order, so a test can injure the best players and watch the rating move.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club these players belong to.</param>
        /// <param name="count">How many players; must not exceed <c>CLUB_SQUAD_SIZE</c>.</param>
        /// <param name="localIndices">Per-slot id suffixes, or null for <c>0..count-1</c>.</param>
        internal static Squad Build(int clubId, int count, int[] localIndices = null)
        {
            var players = new PlayerRecord[count];
            for (int k = 0; k < count; k++)
            {
                int local = localIndices == null ? k : localIndices[k];
                PlayerRecord p = PlayerRecord.CreateDefault(
                    clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + local);
                p.Position = PosFor(k);

                PlayerAttributes attributes = PlayerAttributes.CreateDefault();
                // 1..20 across the roster, saturating at the ceiling for a roster longer than 20.
                attributes.Pace = k + 1 > PlayerDatabaseConstants.ATTRIBUTE_MAX
                    ? PlayerDatabaseConstants.ATTRIBUTE_MAX
                    : k + 1;
                p.Attributes = attributes;

                players[k] = p;
            }

            return new Squad(clubId, players);
        }

        /// <summary>
        /// An <see cref="ISquadProvider"/> whose rosters can be replaced between calls.
        /// <para>
        /// The season-boundary handoff (FR-TR-025 / FR-MD-025) reconciles the career state against
        /// whatever roster the provider reports, and #28's regen / retirement churn — the thing that
        /// would move a roster in production — is unwired (roadmap D1). Without a provider that can
        /// change, the reconciliation could only ever be asserted to be a no-op.
        /// </para>
        /// </summary>
        internal sealed class MutableSquadProvider : ISquadProvider
        {
            private readonly Dictionary<int, Squad> _squads = new Dictionary<int, Squad>();

            /// <summary>Installs or replaces one club's roster.</summary>
            internal void Set(Squad squad)
            {
                _squads[squad.ClubId] = squad;
            }

            /// <summary>Removes a club entirely, so the provider can no longer resolve it.</summary>
            internal void Remove(int clubId)
            {
                _squads.Remove(clubId);
            }

            /// <summary>The club ids currently installed, ascending.</summary>
            internal int[] ClubIds()
            {
                var ids = new int[_squads.Count];
                _squads.Keys.CopyTo(ids, 0);
                System.Array.Sort(ids);
                return ids;
            }

            /// <inheritdoc />
            public Squad ResolveByClubId(int clubId) =>
                _squads.TryGetValue(clubId, out Squad squad) ? squad : null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial (#29/#41 T2): the position-coherent squad builder and the  |
// |         |            |        | mutable provider the roster-reconciliation tests need.             |
// | 1.1     | 2026-08-06 | —      | T2 AR pass 3 (L, doc only): Build's summary said Pace ramps with   |
// |         |            |        | "the local index", which is false whenever localIndices is         |
// |         |            |        | supplied — position and rating follow the roster SLOT, and         |
// |         |            |        | localIndices feeds only the PlayerId. Both indices now named.      |
#endregion
