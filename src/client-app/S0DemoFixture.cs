// File:     src/client-app/S0DemoFixture.cs
// Created:  2026-10-06
// Modified: 2026-10-08 (P5b screens)
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Admits the exact approved names/defaults; the engine alone selects XI and bench.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.ClientApp
{
    /// <summary>Immutable admitted S0 content and setup builder. No roster RNG or lineup selection.</summary>
    public sealed class S0DemoFixture
    {
        private readonly MatchPlayerIdentity[] _identities;
        private readonly Squad _home;
        private readonly Squad _away;
        /// <summary>Canonical UTF-8 fixture content SHA-256, including all numeric inputs and shirts.</summary>
        public string ContentSha256 { get; }
        /// <summary>Admits immutable squad copies and matching authored metadata before session creation.</summary>
        public S0DemoFixture(Squad home, Squad away, MatchPlayerIdentity[] identities)
        {
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _away = away ?? throw new ArgumentNullException(nameof(away));
            if (identities == null) throw new ArgumentNullException(nameof(identities));
            if (home.Count != S0DemoConstants.PLAYERS_PER_SQUAD || away.Count != S0DemoConstants.PLAYERS_PER_SQUAD ||
                identities.Length != home.Count + away.Count)
                throw new ArgumentException("S0 requires 18 players and identities per team.");
            _identities = (MatchPlayerIdentity[])identities.Clone();
            var ids = new HashSet<int>();
            var shirts = new HashSet<int>[] { new HashSet<int>(), new HashSet<int>() };
            var canonical = new StringBuilder();
            canonical.Append(S0DemoConstants.FIXTURE_REVISION).Append('\n');
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                Squad squad = t == 0 ? home : away;
                canonical.Append(squad.ClubId.ToString(CultureInfo.InvariantCulture)).Append('\n');
                for (int i = 0; i < squad.Count; i++)
                {
                    PlayerRecord player = squad.GetPlayer(i);
                    MatchPlayerIdentity identity = FindIdentity(player.PlayerId);
                    if (!ids.Add(player.PlayerId) || identity.TeamId != t || !shirts[t].Add(identity.ShirtNumber) ||
                        player.FirstName != identity.FirstName || player.LastName != identity.LastName ||
                        !Enum.IsDefined(typeof(PlayerPosition), player.Position) ||
                        player.Age < PlayerDatabaseConstants.AgeMin || player.Age > PlayerDatabaseConstants.AgeMax)
                        throw new ArgumentException("Invalid or duplicate S0 player content.");
                    new MatchPlayerIdentity(identity.TeamId, identity.PlayerId,
                        identity.FirstName, identity.LastName, identity.ShirtNumber);
                    int[] attributes = player.Attributes.ToArray();
                    foreach (int attribute in attributes)
                        if (attribute < PlayerDatabaseConstants.ATTRIBUTE_MIN || attribute > PlayerDatabaseConstants.ATTRIBUTE_MAX)
                            throw new ArgumentException("Invalid S0 player attribute.");
                    if (player.Attributes.WeakFootRating < PlayerDatabaseConstants.WEAK_FOOT_MIN ||
                        player.Attributes.WeakFootRating > PlayerDatabaseConstants.WEAK_FOOT_MAX)
                        throw new ArgumentException("Invalid S0 weak foot.");
                    canonical.Append(player.PlayerId.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(player.FirstName).Append('|').Append(player.LastName).Append('|')
                        .Append(player.Age.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(((int)player.Position).ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(identity.ShirtNumber.ToString(CultureInfo.InvariantCulture));
                    foreach (int attribute in attributes) canonical.Append('|').Append(attribute.ToString(CultureInfo.InvariantCulture));
                    canonical.Append('|').Append(player.Attributes.WeakFootRating.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
            }
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                ContentSha256 = hex.ToString();
            }
        }

        /// <summary>Creates the exact 36 owner-approved synthetic records with existing numeric defaults.</summary>
        public static S0DemoFixture CreateApproved()
        {
            string[] home = { "Alex|Rowan", "Ben|Calder", "Chris|Vale", "Daniel|North", "Eli|Mercer", "Felix|Arden",
                "George|Lane", "Henry|Moss", "Ian|Parker", "Jordan|Bell", "Kit|Dawson", "Liam|Hart",
                "Miles|West", "Nico|Hale", "Oliver|Shaw", "Pat|Linden", "Robin|Ash", "Theo|Fox" };
            string[] away = { "Casey|Brooks", "Drew|Ellis", "Evan|Reed", "Finn|Hayes", "Gray|Nolan", "Hugo|Wells",
                "Isaac|Cole", "Jamie|Quinn", "Kai|Foster", "Leo|Marsh", "Morgan|Blake", "Noah|Finch",
                "Owen|Stone", "Perry|Ward", "Remy|Cross", "Sam|Rivers", "Taylor|Dale", "Will|Heath" };
            PlayerPosition[] positions = { PlayerPosition.Goalkeeper, PlayerPosition.Defender, PlayerPosition.Defender,
                PlayerPosition.Defender, PlayerPosition.Defender, PlayerPosition.Midfielder, PlayerPosition.Midfielder,
                PlayerPosition.Midfielder, PlayerPosition.Midfielder, PlayerPosition.Forward, PlayerPosition.Forward,
                PlayerPosition.Goalkeeper, PlayerPosition.Defender, PlayerPosition.Defender, PlayerPosition.Midfielder,
                PlayerPosition.Midfielder, PlayerPosition.Forward, PlayerPosition.Forward };
            var identities = new MatchPlayerIdentity[home.Length + away.Length];
            Squad homeSquad = MakeSquad(0, S0DemoConstants.HOME_CLUB_ID, home, positions, identities);
            Squad awaySquad = MakeSquad(1, S0DemoConstants.AWAY_CLUB_ID, away, positions, identities);
            return new S0DemoFixture(homeSquad, awaySquad, identities);
        }

        /// <summary>Builds from Balanced and changes only home Mentality; away retains AI/default profile.</summary>
        public MatchSetup BuildSetup(Mentality mentality)
        {
            if (!Enum.IsDefined(typeof(Mentality), mentality)) throw new ArgumentOutOfRangeException(nameof(mentality));
            TeamTactic b = TeamTactic.Balanced;
            var home = new TeamTactic(mentality, b.Formation, b.Tempo, b.Width, b.Passing, b.Pressing,
                b.LineOfEngagement, b.DefensiveLine, b.DefensiveWidth, b.TransitionWon, b.TransitionLost,
                b.OffsideTrap, b.TriggerPressMask, b.FocusPlay, b.GkDistribution, b.TimeWasting,
                b.MarkingOrientation, b.DismarkIntensity, b.BuildUpStructure, b.RotationFreedom);
            return new MatchSetup(S0DemoConstants.SEED, _home, _away, home, b,
                ManagerMode.Human, ManagerMode.AI, gkHeadingEnabled: false);
        }

        /// <summary>Consumes the real configured engine descriptor, not the fixture row order.</summary>
        public MatchIdentityContext Bind(MatchSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return new MatchIdentityContext(MatchRoster.FromStreamer(session.Streamer), session.BootRoster, _identities);
        }

        /// <summary>Immutable authored position for bench copy; live pitch keeper status remains frame-owned.</summary>
        public bool IsAuthoredGoalkeeper(int playerId)
        {
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                Squad squad = team == 0 ? _home : _away;
                for (int i = 0; i < squad.Count; i++)
                {
                    PlayerRecord player = squad.GetPlayer(i);
                    if (player.PlayerId == playerId) return player.Position == PlayerPosition.Goalkeeper;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        private MatchPlayerIdentity FindIdentity(int playerId)
        {
            bool found = false;
            MatchPlayerIdentity result = default;
            foreach (MatchPlayerIdentity identity in _identities)
                if (identity.PlayerId == playerId)
                {
                    if (found) throw new ArgumentException("Duplicate authored player identity.");
                    found = true;
                    result = identity;
                }
            if (!found) throw new ArgumentException("Missing authored player identity.");
            return result;
        }

        private static Squad MakeSquad(int teamId, int clubId, string[] names, PlayerPosition[] positions,
            MatchPlayerIdentity[] identities)
        {
            var players = new PlayerRecord[names.Length];
            for (int i = 0; i < players.Length; i++)
            {
                string[] name = names[i].Split('|');
                int playerId = clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + i;
                players[i] = new PlayerRecord { PlayerId = playerId, FirstName = name[0], LastName = name[1],
                    Age = S0DemoConstants.AGE, Position = positions[i], Attributes = PlayerAttributes.CreateDefault() };
                identities[teamId * names.Length + i] = new MatchPlayerIdentity(teamId, playerId, name[0], name[1], i + 1);
            }
            return new Squad(clubId, players);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
// | 1.1     | 2026-10-08 | —      | Authored bench position lookup for complete keeper chooser labels. |
#endregion
