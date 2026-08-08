// File:     src/tactical-instructions/Tests/TacticPresetLibraryTests.cs
// Created:  2026-07-10
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2 (FR-TP-001/002/003/013/014), Appendix A.1, Code Standards #20
// Purpose:  Locks the #26 T0 catalogue: the pinned A.1 ladder order and compositions, the KD-7
//           non-listed-dials-stay-identity rule, the Balanced-verbatim identity preset, and the
//           TacticPreset construction/validation gates.

using System;

using NUnit.Framework;

namespace TacticalDirector.TacticalInstructions.Tests
{
    [TestFixture]
    internal class TacticPresetLibraryTests
    {
        // ---------- A.1 pinned ladder order (FR-TP-013 [FIXED] ordering contract) ----------

        [Test]
        public void Catalogue_LadderOrderIsPinned_DefensiveToAttacking()
        {
            Assert.AreEqual(5, TacticPresetLibrary.Count);
            Assert.AreEqual("ParkTheBus", TacticPresetLibrary.Presets[TacticPresetLibrary.ParkTheBusOrdinal].Name);
            Assert.AreEqual("CounterAttack", TacticPresetLibrary.Presets[TacticPresetLibrary.CounterAttackOrdinal].Name);
            Assert.AreEqual("Balanced", TacticPresetLibrary.Presets[TacticPresetLibrary.BalancedOrdinal].Name);
            Assert.AreEqual("Possession", TacticPresetLibrary.Presets[TacticPresetLibrary.PossessionOrdinal].Name);
            Assert.AreEqual("Gegenpress", TacticPresetLibrary.Presets[TacticPresetLibrary.GegenpressOrdinal].Name);
            Assert.AreEqual(2, (int)TacticPresetLibrary.BalancedOrdinal, "Balanced is the catalogue midpoint (A.1)");
        }

        // ---------- A.1 compositions ----------

        // Each composition test also locks every dial its A.1 row does NOT list to the Balanced
        // identity value (T0 AR-1 L-1): the globally-untouched dials are covered once by
        // NonListedDials_StayAtBalancedIdentity_OnEveryPreset, so here each preset asserts only
        // the dials some OTHER preset touches — together they lock the Compose-defaults-are-
        // Balanced coherence the library doc claims.

        [Test]
        public void ParkTheBus_MatchesA1Composition()
        {
            TeamTactic b = TeamTactic.Balanced;
            TeamTactic t = TacticPresetLibrary.Presets[TacticPresetLibrary.ParkTheBusOrdinal].Team;
            Assert.AreEqual(Mentality.VeryDefensive, t.Mentality);
            Assert.AreEqual(TacticPressing.Low, t.Pressing);
            Assert.AreEqual(LineOfEngagement.Low, t.LineOfEngagement);
            Assert.AreEqual(0.30f, t.DefensiveLine);
            Assert.AreEqual(3, t.TimeWasting);
            Assert.AreEqual(TransitionPlan.HoldShape, t.TransitionWon);
            // Inherited dials (touched by other presets) stay at Balanced identity.
            Assert.AreEqual(b.Tempo, t.Tempo);
            Assert.AreEqual(b.Passing, t.Passing);
            Assert.AreEqual(b.Width, t.Width);
            Assert.AreEqual(b.TransitionLost, t.TransitionLost);
        }

        [Test]
        public void CounterAttack_MatchesA1Composition()
        {
            TeamTactic b = TeamTactic.Balanced;
            TeamTactic t = TacticPresetLibrary.Presets[TacticPresetLibrary.CounterAttackOrdinal].Team;
            Assert.AreEqual(Mentality.Defensive, t.Mentality);
            Assert.AreEqual(TransitionPlan.CounterAttack, t.TransitionWon);
            Assert.AreEqual(Tempo.Fast, t.Tempo);
            Assert.AreEqual(TacticPassing.Direct, t.Passing);
            Assert.AreEqual(0.40f, t.DefensiveLine);
            // Inherited dials stay at Balanced identity.
            Assert.AreEqual(b.Pressing, t.Pressing);
            Assert.AreEqual(b.LineOfEngagement, t.LineOfEngagement);
            Assert.AreEqual(b.Width, t.Width);
            Assert.AreEqual(b.TransitionLost, t.TransitionLost);
            Assert.AreEqual(b.TimeWasting, t.TimeWasting);
        }

        [Test]
        public void Possession_MatchesA1Composition()
        {
            TeamTactic b = TeamTactic.Balanced;
            TeamTactic t = TacticPresetLibrary.Presets[TacticPresetLibrary.PossessionOrdinal].Team;
            Assert.AreEqual(Mentality.Positive, t.Mentality);
            Assert.AreEqual(TacticPassing.Short, t.Passing);
            Assert.AreEqual(Tempo.Slow, t.Tempo);
            Assert.AreEqual(TacticWidth.Wide, t.Width);
            Assert.AreEqual(0.55f, t.DefensiveLine);
            // Inherited dials stay at Balanced identity.
            Assert.AreEqual(b.Pressing, t.Pressing);
            Assert.AreEqual(b.LineOfEngagement, t.LineOfEngagement);
            Assert.AreEqual(b.TransitionWon, t.TransitionWon);
            Assert.AreEqual(b.TransitionLost, t.TransitionLost);
            Assert.AreEqual(b.TimeWasting, t.TimeWasting);
        }

        [Test]
        public void Gegenpress_MatchesA1Composition()
        {
            TeamTactic b = TeamTactic.Balanced;
            TeamTactic t = TacticPresetLibrary.Presets[TacticPresetLibrary.GegenpressOrdinal].Team;
            Assert.AreEqual(Mentality.Attacking, t.Mentality);
            Assert.AreEqual(TacticPressing.High, t.Pressing);
            Assert.AreEqual(LineOfEngagement.High, t.LineOfEngagement);
            Assert.AreEqual(TransitionPlan.CounterPress, t.TransitionLost);
            Assert.AreEqual(0.65f, t.DefensiveLine);
            // Inherited dials stay at Balanced identity.
            Assert.AreEqual(b.Tempo, t.Tempo);
            Assert.AreEqual(b.Passing, t.Passing);
            Assert.AreEqual(b.Width, t.Width);
            Assert.AreEqual(b.TransitionWon, t.TransitionWon);
            Assert.AreEqual(b.TimeWasting, t.TimeWasting);
        }

        // ---------- KD-7 / FR-TI-031 identity discipline ----------

        [Test]
        public void BalancedPreset_IsTeamTacticBalancedVerbatim()
        {
            AssertTacticsEqual(TeamTactic.Balanced,
                TacticPresetLibrary.Presets[TacticPresetLibrary.BalancedOrdinal].Team);
        }

        [Test]
        public void NonListedDials_StayAtBalancedIdentity_OnEveryPreset()
        {
            // A.1: "non-listed dials stay at their TeamTactic.Balanced identity values" (KD-7).
            TeamTactic b = TeamTactic.Balanced;
            foreach (TacticPreset preset in TacticPresetLibrary.Presets)
            {
                TeamTactic t = preset.Team;
                // Dials no A.1 composition touches:
                Assert.AreEqual(b.Formation, t.Formation, preset.Name);
                Assert.AreEqual(b.DefensiveWidth, t.DefensiveWidth, preset.Name);
                Assert.AreEqual(b.OffsideTrap, t.OffsideTrap, preset.Name);
                Assert.AreEqual(b.TriggerPressMask, t.TriggerPressMask, preset.Name);
                Assert.AreEqual(b.FocusPlay, t.FocusPlay, preset.Name);
                Assert.AreEqual(b.GkDistribution, t.GkDistribution, preset.Name);
                Assert.AreEqual(b.MarkingOrientation, t.MarkingOrientation, preset.Name);
                Assert.AreEqual(b.DismarkIntensity, t.DismarkIntensity, preset.Name);
                Assert.AreEqual(b.BuildUpStructure, t.BuildUpStructure, preset.Name);
                Assert.AreEqual(b.RotationFreedom, t.RotationFreedom, preset.Name);
            }
        }

        [Test]
        public void Stage0Presets_CarryNoPerAgentTactics()
        {
            foreach (TacticPreset preset in TacticPresetLibrary.Presets)
            {
                Assert.IsNull(preset.Players, preset.Name);
                preset.ValidatePlayers(11); // null Players always validates (FR-TP-014).
            }
        }

        [Test]
        public void EveryPreset_SeedsA3ScoringRowFromConstants()
        {
            // WS-1: each preset's A.3 kickoff-scoring row is seeded from the ordinal-indexed
            // TacticalPresetsConstants tables — the values are unchanged, so the default kickoff
            // selection is byte-identical (the faithful-pass-through of the affinity move).
            for (int p = 0; p < TacticPresetLibrary.Count; p++)
            {
                TacticPreset preset = TacticPresetLibrary.Presets[p];
                Assert.AreEqual(TacticalPresetsConstants.BaseFit[p], preset.BaseFit, preset.Name);
                Assert.AreEqual(TacticalPresetsConstants.AggrAffinity[p], preset.AggrAffinity, preset.Name);
                Assert.AreEqual(TacticalPresetsConstants.CautAffinity[p], preset.CautAffinity, preset.Name);
            }
        }

        // ---------- TacticPreset gates ----------

        [Test]
        public void Preset_RefusesEmptyName_AndWrongRosterLength()
        {
            Assert.Throws<ArgumentException>(() => new TacticPreset(null, TeamTactic.Balanced, 0f, 0f, 0f));
            Assert.Throws<ArgumentException>(() => new TacticPreset(string.Empty, TeamTactic.Balanced, 0f, 0f, 0f));

            var preset = new TacticPreset("X", TeamTactic.Balanced, 0f, 0f, 0f, new PlayerTactic[5]);
            Assert.Throws<ArgumentException>(() => preset.ValidatePlayers(11));
            preset.ValidatePlayers(5); // exact length passes.
        }

        [Test]
        public void Preset_SnapshotsPlayersAtConstruction()
        {
            // T0 AR-1 M-1: a retained live caller array would let post-construction mutation
            // bypass the FR-TP-014 gate (the slice-2 AR-1 M-1 / match-viewer AR-3 M-1 class).
            var source = new PlayerTactic[3]
            {
                PlayerTactic.Default(PlayerRole.Default),
                PlayerTactic.Default(PlayerRole.Default),
                PlayerTactic.Default(PlayerRole.Default),
            };
            var preset = new TacticPreset("X", TeamTactic.Balanced, 0f, 0f, 0f, source);

            source[1] = new PlayerTactic(
                PlayerRole.Default, Duty.Attack, PlayerInstructions.Default);

            Assert.AreEqual(Duty.Support, preset.Players[1].Duty,
                "post-construction mutation of the caller's array must not reach the preset");
            Assert.AreNotSame(source, preset.Players);
        }

        private static void AssertTacticsEqual(in TeamTactic expected, in TeamTactic actual)
        {
            Assert.AreEqual(expected.Mentality, actual.Mentality);
            Assert.AreEqual(expected.Formation, actual.Formation);
            Assert.AreEqual(expected.Tempo, actual.Tempo);
            Assert.AreEqual(expected.Width, actual.Width);
            Assert.AreEqual(expected.Passing, actual.Passing);
            Assert.AreEqual(expected.Pressing, actual.Pressing);
            Assert.AreEqual(expected.LineOfEngagement, actual.LineOfEngagement);
            Assert.AreEqual(expected.DefensiveLine, actual.DefensiveLine);
            Assert.AreEqual(expected.DefensiveWidth, actual.DefensiveWidth);
            Assert.AreEqual(expected.TransitionWon, actual.TransitionWon);
            Assert.AreEqual(expected.TransitionLost, actual.TransitionLost);
            Assert.AreEqual(expected.OffsideTrap, actual.OffsideTrap);
            Assert.AreEqual(expected.TriggerPressMask, actual.TriggerPressMask);
            Assert.AreEqual(expected.FocusPlay, actual.FocusPlay);
            Assert.AreEqual(expected.GkDistribution, actual.GkDistribution);
            Assert.AreEqual(expected.TimeWasting, actual.TimeWasting);
            Assert.AreEqual(expected.MarkingOrientation, actual.MarkingOrientation);
            Assert.AreEqual(expected.DismarkIntensity, actual.DismarkIntensity);
            Assert.AreEqual(expected.BuildUpStructure, actual.BuildUpStructure);
            Assert.AreEqual(expected.RotationFreedom, actual.RotationFreedom);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial T0 suite (#26): pinned catalogue + identity discipline + gates. |
// | 1.1     | 2026-07-10 | —      | T0 AR-1: composition tests gain inherited-dial == Balanced locks (L-1 —  |
// |         |            |        |   the Compose-defaults coherence was claimed but unlocked for dials some |
// |         |            |        |   presets touch); + Preset_SnapshotsPlayersAtConstruction (M-1 lock).    |
// | 1.2     | 2026-07-24 | —      | WS-1 (#26 KD-6): TacticPreset ctor sites pass the three affinity        |
// |         |            |        |   scalars; + EveryPreset_SeedsA3ScoringRowFromConstants (the affinity   |
// |         |            |        |   move is a faithful seed from TacticalPresetsConstants).                |
#endregion
