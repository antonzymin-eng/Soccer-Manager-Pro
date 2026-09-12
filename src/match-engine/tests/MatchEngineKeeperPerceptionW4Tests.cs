// File:     src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Match-engine wiring backlog W4; Perception System #7 §3.2; Code Standards #20
// Purpose:  Composed W4 locks: a screened goal-bound ball cannot emit DT SAVE, while the same raw
//           save threat continues to veto keeper rush.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineKeeperPerceptionW4Tests
    {
        private const ulong MatchSeed = 0x57344B4552504552UL;
        private const int HomeTeam = 0;
        private const int AwayTeam = 1;
        private static readonly Vector3 ThreatPosition = new Vector3(5f, 34f, 0.11f);
        private static readonly Vector3 ThreatVelocity = new Vector3(-3.5f, 0f, 0f);
        private static readonly Vector2 KeeperPosition = new Vector2(2f, 34f);
        private static readonly Vector2 ScreenPosition = new Vector2(2.5f, 34f);

        private static int FindKeeper(MatchEngine engine, int team)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) == team && engine.AgentIsGoalkeeper(i)) return i;
            }
            return -1;
        }

        private static int FindOutfielder(MatchEngine engine, int team)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) == team && !engine.AgentIsGoalkeeper(i)) return i;
            }
            return -1;
        }

        private static void StageOpponentScreen(MatchEngine engine, int keeper, int screenAgent)
        {
            engine.TestOnly_SetAgent(
                keeper,
                AgentState.CreateAtPosition(KeeperPosition, Vector2.right));
            engine.TestOnly_SetCommand(keeper, MovementCommand.Stop(KeeperPosition));
            engine.TestOnly_SetAgent(
                screenAgent,
                AgentState.CreateAtPosition(ScreenPosition, Vector2.left));
            engine.TestOnly_SetCommand(screenAgent, MovementCommand.Stop(ScreenPosition));
            engine.TestOnly_ForceBallLoose(ThreatPosition, ThreatVelocity);
        }

        [Test]
        public void OpponentScreenedGoalBoundThreat_DoesNotCommitDtSave()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, HomeTeam);
            int screen = FindOutfielder(engine, AwayTeam);
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);
            Assert.IsTrue(GkHeadingIntentSource.SaveArmed(
                HomeTeam, ThreatPosition, ThreatVelocity, ballLoose: true));

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageOpponentScreen(engine, keeper, screen);
                engine.RunTick();
            }

            Assert.IsFalse(engine.TestOnly_SaveCommittedForGk(HomeTeam),
                "W4: a body-screened goal-bound threat must not make the Decision Tree emit SAVE.");
        }

        [Test]
        public void OpponentScreenedGoalBoundThreat_DoesNotArmRush()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x55UL);
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, HomeTeam);
            int screen = FindOutfielder(engine, AwayTeam);
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);
            StageOpponentScreen(engine, keeper, screen);
            Assert.IsTrue(GkHeadingIntentSource.SaveArmed(
                HomeTeam, ThreatPosition, ThreatVelocity, ballLoose: true));

            for (int i = 0; i < 5; i++)
            {
                StageOpponentScreen(engine, keeper, screen);
                engine.TestOnly_DriveGkHeadingTactical();
            }

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "W4 invariant: an unsighted keeper still must not rush at a goal-bound save threat.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                               |
// | 1.0     | 2026-09-11 | —      | W4 composed LOS-gated SAVE + raw-rush-veto locks. |
#endregion
