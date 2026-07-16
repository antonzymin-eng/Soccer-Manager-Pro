// File:     src/player-database/tests/SquadFileLoaderTests.cs
// Created:  2026-07-15
// Modified: 2026-07-16 (AR-3 M-2: age-bounds regression locks — out-of-range throws, boundary values parse)
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §5
// Purpose:  SquadFileLoader grammar round-trip, empty-file default squad, and every fail-loud gate.

using System;

using NUnit.Framework;

namespace TacticalDirector.PlayerDatabase.Tests
{
    [TestFixture]
    public sealed class SquadFileLoaderTests
    {
        [Test]
        public void Parse_NullText_AllDefaultSquad()
        {
            Squad squad = SquadFileLoader.Parse(null, clubId: 5);
            Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, squad.Count);
            PlayerRecord p0 = squad.GetPlayer(0);
            Assert.AreEqual("Player", p0.FirstName);
            Assert.AreEqual(PlayerPosition.Midfielder, p0.Position);
            Assert.AreEqual(PlayerDatabaseConstants.AttributeBaseMean, p0.Attributes.Pace);
        }

        [Test]
        public void Parse_EmptyOrCommentOnly_AllDefaultSquad()
        {
            Squad squad = SquadFileLoader.Parse("\n# just a comment\n\n", clubId: 0);
            Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, squad.Count);
        }

        [Test]
        public void Parse_FullRecord_RoundTrips()
        {
            const string text = "[player 0]\n"
                + "firstName = Alex\n"
                + "lastName = Rivera\n"
                + "age = 21\n"
                + "position = Forward\n"
                + "weakFoot = 4\n"
                + "pace = 18\n"
                + "finishing = 16\n";

            Squad squad = SquadFileLoader.Parse(text, clubId: 7);
            PlayerRecord p = squad.GetPlayer(0);

            Assert.AreEqual(7 * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 0, p.PlayerId);
            Assert.AreEqual("Alex", p.FirstName);
            Assert.AreEqual("Rivera", p.LastName);
            Assert.AreEqual(21, p.Age);
            Assert.AreEqual(PlayerPosition.Forward, p.Position);
            Assert.AreEqual(4, p.Attributes.WeakFootRating);
            Assert.AreEqual(18, p.Attributes.Pace);
            Assert.AreEqual(16, p.Attributes.Finishing);
            // Omitted key inherits the mid-range identity value.
            Assert.AreEqual(PlayerDatabaseConstants.AttributeBaseMean, p.Attributes.Passing);
        }

        [Test]
        public void Parse_OmittedSection_IsFullIdentity()
        {
            Squad squad = SquadFileLoader.Parse("[player 2]\nfirstName = Only\n", clubId: 0);
            Assert.AreEqual(3, squad.Count, "highest index 2 -> 3 players.");
            PlayerRecord p0 = squad.GetPlayer(0);
            Assert.AreEqual("Player", p0.FirstName, "player 0 section absent -> full identity.");
        }

        [Test]
        public void Parse_UnknownKey_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\nnotAKey = 5\n", 0));
        }

        [Test]
        public void Parse_UnknownSection_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[club 0]\nfirstName = X\n", 0));
        }

        [Test]
        public void Parse_DuplicateSection_Throws()
        {
            Assert.Throws<FormatException>(() =>
                SquadFileLoader.Parse("[player 0]\nfirstName = A\n[player 0]\nfirstName = B\n", 0));
        }

        [Test]
        public void Parse_DuplicateKey_Throws()
        {
            Assert.Throws<FormatException>(() =>
                SquadFileLoader.Parse("[player 0]\nfirstName = A\nfirstName = B\n", 0));
        }

        [Test]
        public void Parse_KeyBeforeSection_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("firstName = A\n", 0));
        }

        [Test]
        public void Parse_OutOfRangeAttribute_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\npace = 21\n", 0));
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\npace = 0\n", 0));
        }

        [Test]
        public void Parse_OutOfRangeWeakFoot_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\nweakFoot = 6\n", 0));
        }

        [Test]
        public void Parse_UnparsableInt_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\nage = notanumber\n", 0));
        }

        [Test]
        public void Parse_OutOfRangeAge_Throws()
        {
            // AR-3 M-2 regression lock: age was the one numeric key without a range check — a squad
            // file could author age = -3 or 99999 despite the "out-of-range int all throw" contract.
            string below = $"[player 0]\nage = {PlayerDatabaseConstants.AgeMin - 1}\n";
            string above = $"[player 0]\nage = {PlayerDatabaseConstants.AgeMax + 1}\n";
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse(below, 0));
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse(above, 0));
        }

        [Test]
        public void Parse_AgeAtBounds_Parses()
        {
            string text = $"[player 0]\nage = {PlayerDatabaseConstants.AgeMin}\n[player 1]\nage = {PlayerDatabaseConstants.AgeMax}\n";
            Squad squad = SquadFileLoader.Parse(text, 0);
            Assert.AreEqual(PlayerDatabaseConstants.AgeMin, squad.GetPlayer(0).Age);
            Assert.AreEqual(PlayerDatabaseConstants.AgeMax, squad.GetPlayer(1).Age);
        }

        [Test]
        public void Parse_UnknownPosition_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player 0]\nposition = Striker\n", 0));
        }

        [Test]
        public void Parse_MalformedSectionHeader_Throws()
        {
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player x]\nfirstName = A\n", 0));
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse("[player -1]\nfirstName = A\n", 0));
        }

        [Test]
        public void Parse_CommentsAndBlankLines_Ignored()
        {
            const string text = "# header comment\n\n[player 0]\n  # inline-ish comment line\nfirstName = A # trailing\n\n";
            Squad squad = SquadFileLoader.Parse(text, 0);
            Assert.AreEqual("A", squad.GetPlayer(0).FirstName);
        }

        [Test]
        public void Parse_IndexExceedsClubSquadSize_Throws()
        {
            string text = $"[player {PlayerDatabaseConstants.CLUB_SQUAD_SIZE}]\nfirstName = X\n";
            Assert.Throws<FormatException>(() => SquadFileLoader.Parse(text, 0));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-16 | —      | AR-3 M-2 locks: out-of-range age throws (both sides), and the |
// |         |            |        | [AgeMin, AgeMax] boundary values parse.                        |
#endregion
