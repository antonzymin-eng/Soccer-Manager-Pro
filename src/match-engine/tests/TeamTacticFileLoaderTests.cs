// File:     src/match-engine/tests/TeamTacticFileLoaderTests.cs
// Created:  2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2, FR-TI-031, FR-CS-019; Match Engine design note §5; Code Standards #20
// Purpose:  Locks the on-disk → TeamTacticConfig parser swap: round-trips the text grammar onto
//           TeamTactic fields, proves an empty/comment-only file is the Balanced identity (behaviour-
//           neutral), and that malformed input fails loudly rather than falling back silently.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class TeamTacticFileLoaderTests
    {
        [Test]
        public void Parse_FullSections_RoundTripsOntoFields()
        {
            const string text =
                "# sample tactic file\n" +
                "[home]\n" +
                "mentality = Attacking   # push on\n" +
                "pressing = High\n" +
                "tempo = VeryFast\n" +
                "defensiveLine = 0.7\n" +
                "offsideTrap = true\n" +
                "focusPlay = LeftFlank\n" +
                "timeWasting = 2\n" +
                "markingOrientation = ManOriented\n" +
                "\n" +
                "[away]\n" +
                "mentality = Defensive\n" +
                "lineOfEngagement = VeryLow\n";

            TeamTacticConfig config = TeamTacticFileLoader.Parse(text);

            TeamTactic home = config.ForTeam(0);
            Assert.AreEqual(Mentality.Attacking, home.Mentality);
            Assert.AreEqual(TacticPressing.High, home.Pressing);
            Assert.AreEqual(Tempo.VeryFast, home.Tempo);
            Assert.AreEqual(0.7f, home.DefensiveLine, 1e-6f);
            Assert.IsTrue(home.OffsideTrap);
            Assert.AreEqual(FocusPlay.LeftFlank, home.FocusPlay);
            Assert.AreEqual((byte)2, home.TimeWasting);
            Assert.AreEqual(MarkingOrientation.ManOriented, home.MarkingOrientation);
            // An omitted key inherits the Balanced identity value.
            Assert.AreEqual(TeamTactic.Balanced.Passing, home.Passing);

            TeamTactic away = config.ForTeam(1);
            Assert.AreEqual(Mentality.Defensive, away.Mentality);
            Assert.AreEqual(LineOfEngagement.VeryLow, away.LineOfEngagement);
            // Untouched fields are Balanced — a partial section is well-defined.
            Assert.AreEqual(TeamTactic.Balanced.Tempo, away.Tempo);
            Assert.AreEqual(TeamTactic.Balanced.Width, away.Width);
            Assert.AreEqual(TeamTactic.Balanced.MarkingOrientation, away.MarkingOrientation);
        }

        [Test]
        public void Parse_EmptyOrCommentsOnly_IsBalancedIdentity()
        {
            // No sections at all ⇒ both teams Balanced ⇒ applying it is behaviour-neutral (FR-TI-031).
            foreach (string text in new[] { "", "   ", null, "# only a comment\n\n# another\n" })
            {
                TeamTacticConfig config = TeamTacticFileLoader.Parse(text);
                AssertBalanced(config.ForTeam(0));
                AssertBalanced(config.ForTeam(1));
            }
        }

        [Test]
        public void Parse_UnknownKey_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("[home]\nnotAField = 3\n"));
        }

        [Test]
        public void Parse_UnknownEnumValue_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("[home]\nmentality = Berserk\n"));
        }

        [Test]
        public void Parse_UnknownSection_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("[midfield]\nmentality = Balanced\n"));
        }

        [Test]
        public void Parse_KeyBeforeSection_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("mentality = Balanced\n[home]\n"));
        }

        [Test]
        public void Parse_DuplicateKey_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("[home]\nmentality = Balanced\nmentality = Attacking\n"));
        }

        [Test]
        public void Parse_TimeWastingOutOfRange_Throws()
        {
            Assert.Throws<FormatException>(() =>
                TeamTacticFileLoader.Parse("[home]\ntimeWasting = 9\n"));
        }

        [Test]
        public void Parse_ResultFeedsApplier_RoutesPerTeam()
        {
            // The parser output is the applier's input unchanged: a parsed non-Balanced config routes to
            // the engine via SetTeamTactic, distinct per team.
            TeamTacticConfig config = TeamTacticFileLoader.Parse(
                "[home]\nmentality = Attacking\n[away]\nmentality = Defensive\n");

            var engine = new MatchEngine(0x0123456789ABCDEFUL);
            TeamTacticConfigApplier.Apply(engine, config);
            // Tick to the first AI-stride boundary so the pending → active commit runs (FR-TI-027).
            for (int i = 0; i < DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.RunTick();
            }

            Assert.AreEqual(Mentality.Attacking, engine.TestOnly_Mentality(0));
            Assert.AreEqual(Mentality.Defensive, engine.TestOnly_Mentality(MatchEngineConstants.PLAYERS_PER_TEAM));
        }

        private static void AssertBalanced(TeamTactic t)
        {
            TeamTactic b = TeamTactic.Balanced;
            Assert.AreEqual(b.Mentality, t.Mentality);
            Assert.AreEqual(b.Tempo, t.Tempo);
            Assert.AreEqual(b.Pressing, t.Pressing);
            Assert.AreEqual(b.Passing, t.Passing);
            Assert.AreEqual(b.Width, t.Width);
            Assert.AreEqual(b.DefensiveWidth, t.DefensiveWidth);
            Assert.AreEqual(b.LineOfEngagement, t.LineOfEngagement);
            Assert.AreEqual(b.DefensiveLine, t.DefensiveLine, 1e-6f);
            Assert.AreEqual(b.OffsideTrap, t.OffsideTrap);
            Assert.AreEqual(b.FocusPlay, t.FocusPlay);
            Assert.AreEqual(b.TimeWasting, t.TimeWasting);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-29 | —      | Initial loader tests: round-trip, Balanced identity, fail-loud, applier feed. |
// | 1.1     | 2026-07-07 | —      | Cheap-item addition: + markingOrientation key round-trip + omitted-key default. |
#endregion
