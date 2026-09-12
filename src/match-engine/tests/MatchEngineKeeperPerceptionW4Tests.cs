// File:     src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs
// Created:  2026-09-11
// Modified: 2026-09-12
// Author:   —
// Spec:     Match-engine wiring backlog W4; Perception System #7 §3.2; Code Standards #20
// Purpose:  Composed W4 locks: live all-body LOS gates DT SAVE, screened time does not accrue keeper
//           reaction credit, raw SaveArmed still vetoes rush, and a real CollisionSystem deflection
//           reaches the post-deflection keeper reaction-reset sink in the same Resolve phase.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineKeeperPerceptionW4Tests
    {
        private const ulong MatchSeed = 0x57344B4552504552UL;
        private const int HomeTeam = 0;
        private const int AwayTeam = 1;
        private const float PitchY = 34f;
        private const float ThreatDistanceFromOwnGoalM = 5f;
        private const float KeeperDistanceFromOwnGoalM = 2f;
        private const float ScreenDistanceFromOwnGoalM = 3.5f;
        private const float DeflectorDistanceFromOwnGoalM = 5.3f;
        private const float ThreatSpeedMps = 3.5f;
        private const float DeflectionSpeedMps = 8f;

        private static float XFromOwnGoal(int keeperTeam, float distanceM) =>
            keeperTeam == HomeTeam
                ? distanceM
                : MatchEngineConstants.PITCH_LENGTH_M - distanceM;

        private static float TowardOwnGoalX(int keeperTeam, float speedMps) =>
            keeperTeam == HomeTeam ? -speedMps : speedMps;

        private static float AwayFromOwnGoalX(int keeperTeam, float speedMps) =>
            -TowardOwnGoalX(keeperTeam, speedMps);

        private static Vector3 ThreatPosition(int keeperTeam, float z = 0.11f) =>
            new Vector3(XFromOwnGoal(keeperTeam, ThreatDistanceFromOwnGoalM), PitchY, z);

        private static Vector2 KeeperPosition(int keeperTeam) =>
            new Vector2(XFromOwnGoal(keeperTeam, KeeperDistanceFromOwnGoalM), PitchY);

        private static Vector2 ScreenPosition(int keeperTeam) =>
            new Vector2(XFromOwnGoal(keeperTeam, ScreenDistanceFromOwnGoalM), PitchY);

        private static Vector2 DeflectorPosition(int keeperTeam) =>
            new Vector2(XFromOwnGoal(keeperTeam, DeflectorDistanceFromOwnGoalM), PitchY);

        private static int OtherTeam(int team) => team == HomeTeam ? AwayTeam : HomeTeam;

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

        private static void ParkOtherAgents(MatchEngine engine, int keeper, int specialAgent)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i == keeper || i == specialAgent)
                {
                    continue;
                }

                // Unique, widely spaced Y coordinates eliminate unrelated agent-agent overlaps while
                // keeping every parked body much farther from either keeper than the staged threat.
                float y = 2f + i * 3f;
                var parked = new Vector2(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, y);
                engine.TestOnly_SetAgent(i, AgentState.CreateAtPosition(parked, Vector2.right));
                engine.TestOnly_SetCommand(i, MovementCommand.Stop(parked));
            }
        }

        private static void StageGoalBoundThreat(
            MatchEngine engine, int keeperTeam, int keeper, int screenAgent, bool screened)
        {
            ParkOtherAgents(engine, keeper, screenAgent);

            Vector2 keeperPos = KeeperPosition(keeperTeam);
            engine.TestOnly_SetAgent(
                keeper,
                AgentState.CreateAtPosition(
                    keeperPos,
                    keeperTeam == HomeTeam ? Vector2.right : Vector2.left));
            engine.TestOnly_SetCommand(keeper, MovementCommand.Stop(keeperPos));

            Vector2 screenPos = screened
                ? ScreenPosition(keeperTeam)
                : new Vector2(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, 2f);
            engine.TestOnly_SetAgent(
                screenAgent,
                AgentState.CreateAtPosition(screenPos, Vector2.left));
            engine.TestOnly_SetCommand(screenAgent, MovementCommand.Stop(screenPos));

            engine.TestOnly_ForceBallLoose(
                ThreatPosition(keeperTeam),
                new Vector3(TowardOwnGoalX(keeperTeam, ThreatSpeedMps), 0f, 0f));
        }

        [TestCase(HomeTeam)]
        [TestCase(AwayTeam)]
        public void UnscreenedGoalBoundThreat_CommitsDtSave_PositiveControl(int keeperTeam)
        {
            var engine = new MatchEngine(MatchSeed ^ (ulong)(0x100 + keeperTeam));
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, keeperTeam);
            int screen = FindOutfielder(engine, OtherTeam(keeperTeam));
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);

            Vector3 ball = ThreatPosition(keeperTeam);
            Vector3 velocity = new Vector3(TowardOwnGoalX(keeperTeam, ThreatSpeedMps), 0f, 0f);
            Assert.IsTrue(GkHeadingIntentSource.SaveArmed(
                keeperTeam, ball, velocity, ballLoose: true),
                "Positive-control fixture must be raw save geometry before LOS is considered.");

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageGoalBoundThreat(engine, keeperTeam, keeper, screen, screened: false);
                engine.RunTick();
            }

            Assert.IsTrue(engine.TestOnly_SaveCommittedForGk(keeperTeam),
                "Same W4 staging without the screen must make the Decision Tree emit SAVE.");
        }

        [TestCase(HomeTeam)]
        [TestCase(AwayTeam)]
        public void OpponentScreenedGoalBoundThreat_DoesNotCommitDtSave_OrBankReaction(int keeperTeam)
        {
            var engine = new MatchEngine(MatchSeed ^ (ulong)(0x200 + keeperTeam));
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, keeperTeam);
            int screen = FindOutfielder(engine, OtherTeam(keeperTeam));
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageGoalBoundThreat(engine, keeperTeam, keeper, screen, screened: true);
                engine.RunTick();
            }

            Assert.IsFalse(engine.TestOnly_SaveCommittedForGk(keeperTeam),
                "W4: a body-screened goal-bound threat must not make the Decision Tree emit SAVE.");
            Assert.AreEqual(0f, engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[keeperTeam], 1e-6f,
                "W4: screened time must not accrue reaction credit before the keeper can see the threat.");
        }

        [TestCase(HomeTeam)]
        [TestCase(AwayTeam)]
        public void ClearingScreen_StartsReactionClockAtReveal_NotAtRawThreatOnset(int keeperTeam)
        {
            var engine = new MatchEngine(MatchSeed ^ (ulong)(0x300 + keeperTeam));
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, keeperTeam);
            int screen = FindOutfielder(engine, OtherTeam(keeperTeam));
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageGoalBoundThreat(engine, keeperTeam, keeper, screen, screened: true);
                engine.RunTick();
            }
            Assert.AreEqual(0f, engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[keeperTeam], 1e-6f,
                "Precondition: the persistent screen must leave no live reaction stamp.");

            bool stampedAfterReveal = false;
            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageGoalBoundThreat(engine, keeperTeam, keeper, screen, screened: false);
                engine.RunTick();
                if (engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[keeperTeam] > 0f)
                {
                    stampedAfterReveal = true;
                    break;
                }
            }

            Assert.IsTrue(stampedAfterReveal,
                "W4: once LOS clears, the visible threat must seed a fresh reaction episode.");
            Assert.Greater(engine.TestOnly_GoalkeeperState.RequiredReactionMs[keeperTeam], 0f,
                "The reveal must seed the real goalkeeper reaction pipeline, not merely a DT latch.");
        }

        [TestCase(HomeTeam)]
        [TestCase(AwayTeam)]
        public void ScreenedGoalBoundThreat_StillVetoesRush(int keeperTeam)
        {
            var engine = new MatchEngine(MatchSeed ^ (ulong)(0x400 + keeperTeam));
            engine.EnableGkHeading();
            int keeper = FindKeeper(engine, keeperTeam);
            int screen = FindOutfielder(engine, OtherTeam(keeperTeam));
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(screen, 0);

            for (int i = 0; i < 5; i++)
            {
                StageGoalBoundThreat(engine, keeperTeam, keeper, screen, screened: true);
                engine.TestOnly_DriveGkHeadingTactical();
            }

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "W4 invariant: an unsighted keeper still must not rush at a raw goal-bound save threat.");
        }

        [TestCase(HomeTeam)]
        [TestCase(AwayTeam)]
        public void AppliedBodyDeflection_ResetsOnlyPostDeflectionThreatenedKeeper_InSameResolve(int keeperTeam)
        {
            var engine = new MatchEngine(MatchSeed ^ (ulong)(0x500 + keeperTeam));
            engine.EnableGkHeading();
            // TestOnly_RunResolvePhase bypasses the normal tick-clock advance. Prime one ordinary
            // tick so frame 0 cannot alias any default ContactFrame sentinel in inactive shot results.
            // The staged collision below is applied only after this priming tick.
            engine.RunTick();
            int keeper = FindKeeper(engine, keeperTeam);
            int deflector = FindOutfielder(engine, OtherTeam(keeperTeam));
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(deflector, 0);

            ParkOtherAgents(engine, keeper, deflector);
            Vector2 keeperPos = KeeperPosition(keeperTeam);
            engine.TestOnly_SetAgent(
                keeper,
                AgentState.CreateAtPosition(
                    keeperPos,
                    keeperTeam == HomeTeam ? Vector2.right : Vector2.left));
            engine.TestOnly_SetCommand(keeper, MovementCommand.Stop(keeperPos));

            Vector2 deflectorPos = DeflectorPosition(keeperTeam);
            engine.TestOnly_SetAgent(
                deflector,
                AgentState.CreateAtPosition(deflectorPos, Vector2.left));
            engine.TestOnly_SetCommand(deflector, MovementCommand.Stop(deflectorPos));

            Vector3 preDeflectionPosition = ThreatPosition(keeperTeam, z: 0.5f);
            Vector3 preDeflectionVelocity =
                new Vector3(AwayFromOwnGoalX(keeperTeam, DeflectionSpeedMps), 0f, 0f);
            Assert.IsFalse(GkHeadingIntentSource.SaveArmed(
                keeperTeam, preDeflectionPosition, preDeflectionVelocity, ballLoose: true),
                "Precondition: before contact, this flight is moving away from the tested keeper's goal.");
            Assert.AreEqual(0f, engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[keeperTeam], 1e-6f);
            Assert.AreEqual(0f, engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[OtherTeam(keeperTeam)], 1e-6f);

            engine.TestOnly_ForceBallLoose(preDeflectionPosition, preDeflectionVelocity);
            engine.TestOnly_RunResolvePhase();

            Assert.Greater(engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[keeperTeam], 0f,
                "W4 wire: CollisionSystem's applied-deflection output must reach the keeper reaction reset in the same Resolve.");
            Assert.Greater(engine.TestOnly_GoalkeeperState.RequiredReactionMs[keeperTeam], 0f,
                "W4 wire: the post-deflection keeper must receive a full new reaction episode.");
            Assert.AreEqual(0f, engine.TestOnly_GoalkeeperState.ShotDetectedTickMs[OtherTeam(keeperTeam)], 1e-6f,
                "Only the keeper threatened by the post-deflection flight may be reset.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-09-11 | —      | W4 composed LOS-gated SAVE + raw-rush-veto locks.                    |
// | 1.1     | 2026-09-12 | —      | Review closure: paired positive control, reaction-clock reveal lock, |
// |         |            |        | same-Resolve deflection wire lock, and mirrored away-team coverage.  |
#endregion
