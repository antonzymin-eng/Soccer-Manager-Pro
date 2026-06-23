// File:     src/match-engine/tests/MatchEngineMechanicsTests.cs
// Created:  2026-06-22
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase D (D2), Code Standards #20
// Purpose:  Phase D step D2 tests — proves the mechanics AI (Positioning AI #12) feeds live formation
//           slots into each agent's decision context, that the away team's slots mirror the home team's
//           (the ERR-008-002 home/away-asymmetry guard), and that the wiring is deterministic.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase D step D2 mechanics-AI wiring tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineMechanicsTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // Roster layout (MatchEngine.InitializeKickoffState): index 0 of each team is the goalkeeper;
        // home team is agents 0..10, away team is agents 11..21.
        private const int HomeGoalkeeper = 0;
        private const int AwayGoalkeeper = MatchEngineConstants.PLAYERS_PER_TEAM;       // 11
        private const int HomeDefender   = 1;                                           // slot 1 (LB), deep
        private const int HomeStriker    = 9;                                           // slot 9 (ST), advanced

        // Two stride ticks (12 = 2 × AI_PHASE_STRIDE) so the AI phase has run and refreshed slots.
        private static readonly int TwoStrideTicks = 2 * DeterministicSimConstants.AI_PHASE_STRIDE;

        private static MatchEngine RunTo(int ticks)
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
            }
            return engine;
        }

        [Test]
        public void PositioningAI_FeedsFormationSlots_IntoDecisionContext()
        {
            MatchEngine engine = RunTo(TwoStrideTicks);

            Vector2 defenderSlot = engine.TestOnly_FormationSlot(HomeDefender);
            Vector2 strikerSlot  = engine.TestOnly_FormationSlot(HomeStriker);

            // Slots are real on-pitch points (not the SENTINEL_NO_SLOT −∞ fallback or NaN).
            foreach (Vector2 slot in new[] { defenderSlot, strikerSlot })
            {
                Assert.IsTrue(float.IsFinite(slot.x) && float.IsFinite(slot.y),
                    "Formation slot must be a finite on-pitch position.");
                Assert.GreaterOrEqual(slot.x, 0f, "Formation slot X must be inside the pitch length.");
                Assert.LessOrEqual(slot.x, MatchEngineConstants.PITCH_LENGTH_M,
                    "Formation slot X must be inside the pitch length.");
                Assert.GreaterOrEqual(slot.y, 0f, "Formation slot Y must be inside the pitch width.");
                Assert.LessOrEqual(slot.y, MatchEngineConstants.PITCH_WIDTH_M,
                    "Formation slot Y must be inside the pitch width.");
            }

            // The home team attacks +X: its defender sits in the home half and its striker in the
            // attacking half — proving real formation shape (not the kickoff scaffold line) feeds the DT.
            float midline = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            Assert.Less(defenderSlot.x, midline,
                "Home defender formation slot must be in the home half (#12 places defenders deep).");
            Assert.Greater(strikerSlot.x, midline,
                "Home striker formation slot must be in the attacking half (#12 advances forwards).");
        }

        [Test]
        public void AwayTeamFormationSlots_MirrorHomeTeam()
        {
            // The #12 formation table is authored attack-toward-+X; the host maps the away team into that
            // canonical frame and back (180° pitch rotation). With the ball on the centre spot, both GK
            // slots are computed from the same centre-symmetric input, so the away GK slot must be the
            // exact pitch-mirror of the home GK slot. Locks the ERR-008-002 home/away-asymmetry guard.
            MatchEngine engine = RunTo(TwoStrideTicks);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(HomeGoalkeeper) && engine.TestOnly_IsGoalkeeper(AwayGoalkeeper),
                "Roster indices 0 and 11 must be the goalkeepers.");

            Vector2 homeGk = engine.TestOnly_FormationSlot(HomeGoalkeeper);
            Vector2 awayGk = engine.TestOnly_FormationSlot(AwayGoalkeeper);

            float midline = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            Assert.Less(homeGk.x, midline,
                "Home goalkeeper formation slot must sit in the home half (near x = 0).");
            Assert.Greater(awayGk.x, midline,
                "Away goalkeeper formation slot must sit in the away half (near x = LENGTH) — the mirror.");

            // Exact pitch mirror: away = (LENGTH − homeX, WIDTH − homeY).
            Assert.AreEqual(MatchEngineConstants.PITCH_LENGTH_M - homeGk.x, awayGk.x, 0.5f,
                "Away GK slot X must be the pitch-mirror of the home GK slot X.");
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M - homeGk.y, awayGk.y, 0.5f,
                "Away GK slot Y must be the pitch-mirror of the home GK slot Y.");
        }

        [Test]
        public void PositioningWiring_IsDeterministic_AcrossSameSeedRuns()
        {
            MatchEngine engineA = RunTo(TwoStrideTicks);
            MatchEngine engineB = RunTo(TwoStrideTicks);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 a = engineA.TestOnly_FormationSlot(i);
                Vector2 b = engineB.TestOnly_FormationSlot(i);
                Assert.AreEqual(a.x, b.x,
                    $"Formation slot X for agent {i} diverged across two same-seed runs.");
                Assert.AreEqual(a.y, b.y,
                    $"Formation slot Y for agent {i} diverged across two same-seed runs.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Initial Phase D step D2 mechanics-AI wiring tests: Positioning  |
// |         |            |        | AI #12 feeds formation slots into the decision context (home    |
// |         |            |        | defender deep / striker advanced); away-team slots mirror the   |
// |         |            |        | home team (ERR-008-002 guard, exact GK pitch-mirror); and the   |
// |         |            |        | wiring is deterministic across two same-seed runs.              |
#endregion
