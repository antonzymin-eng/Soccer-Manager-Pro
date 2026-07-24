// File:     src/match-engine/tests/TacticPresetFileLoaderTests.cs
// Created:  2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 / §4.4 (FR-TP-002/013/017), KD-6; Code Standards #20
// Purpose:  WS-1 loader tests — the canonical five-preset file round-trips DEEP-EQUAL to the default
//           in-code catalogue (the load-bearing acceptance test, an explicit comparator not struct ==),
//           plus every fail-loud gate.

using System;

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class TacticPresetFileLoaderTests
    {
        // The five Appendix A.1 presets authored as text (dials + affinity + names), so the parsed
        // catalogue must reproduce the in-code default exactly.
        private const string CanonicalFile = @"
balancedOrdinal = 2

[preset 0]
name = ParkTheBus
baseFit = -0.30
aggrAffinity = -0.50
cautAffinity = 0.60
mentality = VeryDefensive
pressing = Low
lineOfEngagement = Low
defensiveLine = 0.30
timeWasting = 3
transitionWon = HoldShape

[preset 1]
name = CounterAttack
baseFit = 0.10
aggrAffinity = 0.10
cautAffinity = 0.30
mentality = Defensive
transitionWon = CounterAttack
tempo = Fast
passing = Direct
defensiveLine = 0.40

[preset 2]
name = Balanced
baseFit = 0.50
aggrAffinity = 0.00
cautAffinity = 0.00

[preset 3]
name = Possession
baseFit = 0.20
aggrAffinity = 0.30
cautAffinity = -0.10
mentality = Positive
passing = Short
tempo = Slow
width = Wide
defensiveLine = 0.55

[preset 4]
name = Gegenpress
baseFit = 0.10
aggrAffinity = 0.80
cautAffinity = -0.40
mentality = Attacking
pressing = High
lineOfEngagement = High
transitionLost = CounterPress
defensiveLine = 0.65
";

        // A minimal VALID catalogue for the negative tests: three presets, Balanced at the pinned
        // midpoint (ordinal 2). Mutated per test via string replacement.
        private const string MinimalValid = @"
balancedOrdinal = 2
[preset 0]
name = A
baseFit = -0.30
aggrAffinity = -0.50
cautAffinity = 0.60
mentality = VeryDefensive
[preset 1]
name = B
baseFit = 0.10
aggrAffinity = 0.10
cautAffinity = 0.30
[preset 2]
name = Balanced
baseFit = 0.50
aggrAffinity = 0.00
cautAffinity = 0.00
";

        [Test]
        public void Canonical_RoundTripsDeepEqualToDefaultCatalogue()
        {
            ITacticPresetCatalogue actual = TacticPresetFileLoader.Parse(CanonicalFile);
            ITacticPresetCatalogue expected = new InCodeTacticPresetCatalogue();

            Assert.AreEqual(expected.Count, actual.Count, "Count");
            Assert.AreEqual(expected.BalancedOrdinal, actual.BalancedOrdinal, "BalancedOrdinal");
            for (int p = 0; p < expected.Count; p++)
            {
                TacticPreset e = expected.Presets[p];
                TacticPreset a = actual.Presets[p];

                Assert.AreEqual(e.Name, a.Name, "Name @" + p);
                // Players deferred at Stage 0 — both null (null-or-elementwise).
                Assert.IsNull(a.Players, "Players @" + p);
                Assert.IsNull(e.Players, "Players @" + p);

                Assert.AreEqual(e.BaseFit, a.BaseFit, "BaseFit @" + p);
                Assert.AreEqual(e.AggrAffinity, a.AggrAffinity, "AggrAffinity @" + p);
                Assert.AreEqual(e.CautAffinity, a.CautAffinity, "CautAffinity @" + p);

                AssertTeamEqual(e.Team, a.Team, p);
            }
        }

        [Test]
        public void Canonical_FeedsKickoffScoringIdentically()
        {
            // The load-bearing consumer check: the loaded catalogue scores kickoff exactly as the
            // in-code default (the B.1 worked example) — Aggressive selects Gegenpress at 0.66.
            ITacticPresetCatalogue loaded = TacticPresetFileLoader.Parse(CanonicalFile);
            ManagerProfile agg = ManagerProfile.FromArchetype(TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            Assert.AreEqual(0.66f,
                ManagerAdaptation.KickoffScore(in agg, TacticPresetLibrary.GegenpressOrdinal, loaded), 1e-4f);
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal,
                ManagerAdaptation.SelectKickoffPreset(in agg, loaded));
        }

        // ── Fail-loud gates ───────────────────────────────────────────────────────────

        [Test]
        public void Empty_CommentOnly_And_Null_FailLoud()
        {
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(string.Empty));
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse("   \n  \n"));
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse("# just a comment\n# another\n"));
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(null));
        }

        [Test]
        public void MissingRequiredAffinityKey_FailsLoud()
        {
            string bad = MinimalValid.Replace("cautAffinity = 0.60", "");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void OutOfRangeAffinity_FailsLoud()
        {
            string bad = MinimalValid.Replace("baseFit = -0.30", "baseFit = 2.0");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void MissingName_FailsLoud()
        {
            string bad = MinimalValid.Replace("name = A", "");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void UnknownKey_FailsLoud()
        {
            string bad = MinimalValid.Replace("mentality = VeryDefensive", "mentality = VeryDefensive\nfoo = bar");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void DuplicateKey_FailsLoud()
        {
            string bad = MinimalValid.Replace("name = A", "name = A\nname = AA");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void OutOfOrderOrGappedOrdinal_FailsLoud()
        {
            string bad = MinimalValid.Replace("[preset 1]", "[preset 2]"); // 0, 2, 2 — the second is out of order
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void MissingBalancedOrdinal_FailsLoud()
        {
            string bad = MinimalValid.Replace("balancedOrdinal = 2", "");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void BalancedOrdinalOffPinnedMidpoint_FailsLoud()
        {
            string bad = MinimalValid.Replace("balancedOrdinal = 2", "balancedOrdinal = 1");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void BalancedOrdinalSectionNotBalanced_FailsLoud()
        {
            // The section at the declared balancedOrdinal must be TeamTactic.Balanced-equivalent.
            string bad = MinimalValid.Replace("name = Balanced", "name = Balanced\nmentality = Attacking");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        [Test]
        public void UnknownSectionHeader_FailsLoud()
        {
            string bad = MinimalValid.Replace("[preset 0]", "[squad 0]");
            Assert.Throws<FormatException>(() => TacticPresetFileLoader.Parse(bad));
        }

        private static void AssertTeamEqual(in TeamTactic e, in TeamTactic a, int p)
        {
            Assert.AreEqual(e.Mentality, a.Mentality, "Mentality @" + p);
            Assert.AreEqual(e.Formation, a.Formation, "Formation @" + p);
            Assert.AreEqual(e.Tempo, a.Tempo, "Tempo @" + p);
            Assert.AreEqual(e.Width, a.Width, "Width @" + p);
            Assert.AreEqual(e.Passing, a.Passing, "Passing @" + p);
            Assert.AreEqual(e.Pressing, a.Pressing, "Pressing @" + p);
            Assert.AreEqual(e.LineOfEngagement, a.LineOfEngagement, "LineOfEngagement @" + p);
            Assert.AreEqual(e.DefensiveLine, a.DefensiveLine, "DefensiveLine @" + p);
            Assert.AreEqual(e.DefensiveWidth, a.DefensiveWidth, "DefensiveWidth @" + p);
            Assert.AreEqual(e.TransitionWon, a.TransitionWon, "TransitionWon @" + p);
            Assert.AreEqual(e.TransitionLost, a.TransitionLost, "TransitionLost @" + p);
            Assert.AreEqual(e.OffsideTrap, a.OffsideTrap, "OffsideTrap @" + p);
            Assert.AreEqual(e.TriggerPressMask, a.TriggerPressMask, "TriggerPressMask @" + p);
            Assert.AreEqual(e.FocusPlay, a.FocusPlay, "FocusPlay @" + p);
            Assert.AreEqual(e.GkDistribution, a.GkDistribution, "GkDistribution @" + p);
            Assert.AreEqual(e.TimeWasting, a.TimeWasting, "TimeWasting @" + p);
            Assert.AreEqual(e.MarkingOrientation, a.MarkingOrientation, "MarkingOrientation @" + p);
            Assert.AreEqual(e.DismarkIntensity, a.DismarkIntensity, "DismarkIntensity @" + p);
            Assert.AreEqual(e.BuildUpStructure, a.BuildUpStructure, "BuildUpStructure @" + p);
            Assert.AreEqual(e.RotationFreedom, a.RotationFreedom, "RotationFreedom @" + p);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1 / #26 KD-6): canonical five-preset round-trip    |
// |         |            |        |   deep-equal to the default catalogue + kickoff-scoring parity |
// |         |            |        |   + every fail-loud gate (empty/affinity/name/unknown/dup/     |
// |         |            |        |   ordinal-order/balancedOrdinal missing/off-midpoint/not-Bal). |
#endregion
