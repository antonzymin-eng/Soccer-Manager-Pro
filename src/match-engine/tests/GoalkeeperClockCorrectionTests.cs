// File:     src/match-engine/tests/GoalkeeperClockCorrectionTests.cs
// Created:  2026-09-26
// Modified: 2026-09-26 (v1.1 review closure: live distribution-intent teardown cases)
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.8 (clock domain, §3.8.3 possession-change teardown);
//           ERR-011-017; Code Standards #20
// Purpose:  Locks the W8 clock correction: #11 tactical transitions run on the 10 Hz tactical tick, and a
//           keeper's live hand episode ends when the composition root reports a loss of possession or a
//           restart. Mirrored for both teams.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>W8 / ERR-011-017 clock correction and its §3.8.3 hand-episode teardown companion.</summary>
    [TestFixture]
    public sealed class GoalkeeperClockCorrectionTests
    {
        private const ulong MatchSeed = 0x0BADF00DDEADBEEFUL;

        // Deep into a match and mid-way through a six-frame tactical cell: the raw 60 Hz frame (6003) and
        // the tactical tick (1000) differ by far more than the 60-tick hold limit, so a frame index passed
        // where a tick is expected cannot go unnoticed.
        private const int ClaimFrame = 6003;

        [Test]
        public void TacticalStride_MatchesGoalkeeperFramesPerTacticalTick()
        {
            // The engine derives #11's tick from its own clock stride; #11 converts claim/recovery frames
            // with its own constant. The two conversions must be the same division.
            Assert.AreEqual(
                DeterministicSimConstants.AI_PHASE_STRIDE,
                GoalkeeperConstants.FramesPerTacticalTick);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HandEpisode_HoldsAcrossTacticalPasses_UntilTheTacticalHoldLimit(int teamId)
        {
            MatchEngine engine = NewEngine();
            int claimTick = ClaimFrame / GoalkeeperConstants.FramesPerTacticalTick;
            StageHandEpisode(engine, teamId, ClaimFrame);

            // First tactical pass after the claim. Before ERR-011-017 the raw frame (6006) was compared
            // with the 10 Hz claim tick (1000), matured the 60-tick limit at once and forced Distributing.
            DriveTacticalAt(engine, (claimTick + 1) * GoalkeeperConstants.FramesPerTacticalTick);
            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(teamId),
                "one tactical pass after the claim is 0.1 s of hold, not six seconds");

            DriveTacticalAt(engine,
                (claimTick + GoalkeeperConstants.GK_HOLD_MAX_TICKS - 1) * GoalkeeperConstants.FramesPerTacticalTick);
            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(teamId),
                "one tick short of the hold limit the keeper still holds");

            DriveTacticalAt(engine,
                (claimTick + GoalkeeperConstants.GK_HOLD_MAX_TICKS) * GoalkeeperConstants.FramesPerTacticalTick);
            Assert.AreEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(teamId),
                "the inherited hold limit still fires, now measured in tactical ticks");
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void PossessionLoss_EndsHandEpisode_IntoRecovering(int teamId, bool ballLeftLoose)
        {
            MatchEngine engine = NewEngine();
            StageHandEpisode(engine, teamId, ClaimFrame);
            GoalkeeperState otherKeeperBefore = engine.TestOnly_GkState(1 - teamId);

            const int lossFrame = ClaimFrame + 7;
            engine.TestOnly_SetPhysicsFrame(lossFrame);
            engine.TestOnly_SetPossession(ballLeftLoose
                ? MatchEngineConstants.NO_POSSESSION
                : OutfieldTeammate(engine, teamId));

            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            Assert.AreEqual(GoalkeeperState.Recovering, state.States[teamId],
                "a keeper who no longer has the ball is not in a hand episode");
            Assert.AreEqual(
                lossFrame / GoalkeeperConstants.FramesPerTacticalTick + GoalkeeperConstants.RecoveryCooldownTicks,
                state.RecoveryCooldownEndTick[teamId],
                "recovery uses the ordinary cooldown, in tactical ticks from the loss");
            Assert.AreEqual(otherKeeperBefore, state.States[1 - teamId],
                "only the outgoing holder's episode ends");

            // The corrected clock must not re-enter or mature anything from the ended episode.
            DriveTacticalAt(engine, (lossFrame / GoalkeeperConstants.FramesPerTacticalTick + 1)
                * GoalkeeperConstants.FramesPerTacticalTick);
            Assert.AreNotEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(teamId));
            Assert.AreNotEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(teamId));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void PossessionLoss_ClearsALiveDistributeIntent(int teamId, bool alreadyDistributing)
        {
            // Both hand-episode states end on a loss: HandsOnBall with a committed intent, and the
            // Distributing windup in which the keeper is still the controlled holder (#11 §3.8.3).
            MatchEngine engine = NewEngine();
            GoalkeeperState handState = alreadyDistributing
                ? GoalkeeperState.Distributing
                : GoalkeeperState.HandsOnBall;
            StageHandEpisode(engine, teamId, ClaimFrame, handState, LiveIntent(engine, teamId));

            GoalkeeperTickState before = engine.TestOnly_GoalkeeperState;
            Assert.IsTrue(before.DistributeIntentActive[teamId], "fixture: a live intent is staged");
            Assert.Greater(before.DistributeIntents[teamId].PowerIntent, 0f, "fixture: a non-default intent");

            engine.TestOnly_SetPhysicsFrame(ClaimFrame + 7);
            engine.TestOnly_SetPossession(OutfieldTeammate(engine, teamId));

            GoalkeeperTickState after = engine.TestOnly_GoalkeeperState;
            Assert.AreEqual(GoalkeeperState.Recovering, after.States[teamId]);
            Assert.IsFalse(after.DistributeIntentActive[teamId],
                "an ordinary loss cancels the distribution: nothing may be released for a ball the keeper lost");
            Assert.AreEqual(0f, after.DistributeIntents[teamId].PowerIntent, "the cancelled intent is cleared");
            Assert.IsFalse(after.DistributeIntents[teamId].TargetReceiverId.HasValue,
                "the cancelled intent is cleared");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void UnchangedHolder_DoesNotEndHandEpisode(int teamId)
        {
            MatchEngine engine = NewEngine();
            StageHandEpisode(engine, teamId, ClaimFrame);

            engine.TestOnly_SetPossession(GoalkeeperForTeam(engine, teamId));

            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(teamId),
                "re-asserting the same holder is not a change of possession");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void KeeperWithoutHandEpisode_IsUntouchedByPossessionLoss(int teamId)
        {
            MatchEngine engine = NewEngine();
            engine.TestOnly_SetPhysicsFrame(ClaimFrame);
            int keeper = GoalkeeperForTeam(engine, teamId);
            GoalkeeperState before = engine.TestOnly_GkState(teamId);
            Assert.AreNotEqual(GoalkeeperState.HandsOnBall, before, "fixture: no hand episode staged");

            engine.TestOnly_SetPossession(keeper);
            engine.TestOnly_SetPossession(OutfieldTeammate(engine, teamId));

            Assert.AreEqual(before, engine.TestOnly_GkState(teamId),
                "feet possession lost by a keeper outside a hand episode is ordinary play");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Restart_EndsHandEpisode_EvenWhenTheSameKeeperTakesIt(int teamId)
        {
            MatchEngine engine = NewEngine();
            StageHandEpisode(engine, teamId, ClaimFrame);
            int keeper = GoalkeeperForTeam(engine, teamId);

            // Placed on the keeper, the keeper is the nearest eligible taker, so possession does not
            // change hands and only the restart itself can end the episode.
            engine.TestOnly_ApplyRestart(engine.AgentView(keeper).Position, teamId);

            Assert.AreEqual(keeper, engine.TestOnly_PossessingAgentId, "fixture: the keeper takes the restart");
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(teamId),
                "a restart is a dead ball: the hand episode is over whoever takes it");
        }

        // ── Fixture helpers ──────────────────────────────────────────────────────────────

        private static MatchEngine NewEngine()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            return engine;
        }

        /// <summary>Gives the team's keeper controlled possession and a live #11 hand episode claimed at
        /// <paramref name="claimFrame"/>, restored through #11's own snapshot seam. With no
        /// <paramref name="intent"/> the episode has no committed distribution.</summary>
        private static void StageHandEpisode(
            MatchEngine engine,
            int teamId,
            int claimFrame,
            GoalkeeperState handState = GoalkeeperState.HandsOnBall,
            DistributeIntent? intent = null)
        {
            engine.TestOnly_SetPhysicsFrame(claimFrame);
            int keeper = GoalkeeperForTeam(engine, teamId);
            Assert.GreaterOrEqual(keeper, 0, "fixture: team has a keeper");
            engine.TestOnly_SetPossession(keeper);

            GoalkeeperTickState staged = Clone(engine.TestOnly_GoalkeeperState);
            int claimTick = claimFrame / GoalkeeperConstants.FramesPerTacticalTick;
            staged.States[teamId] = handState;
            staged.ClaimTick[teamId] = claimTick;
            staged.ReleaseTickEarliest[teamId] = claimTick + 1;
            staged.DistributeIntents[teamId] = intent ?? default;
            staged.DistributeIntentActive[teamId] = intent.HasValue;
            engine.TestOnly_RestoreGoalkeeperState(in staged);

            Assert.AreEqual(handState, engine.TestOnly_GkState(teamId), "fixture: staged");
            Assert.AreEqual(keeper, engine.TestOnly_PossessingAgentId, "fixture: keeper holds the ball");
        }

        /// <summary>A committed throw to a team-mate. Receiver, target point and power are non-default, so
        /// clearing is observable (DeliveryKind.Throw and zero spin coincide with the default).</summary>
        private static DistributeIntent LiveIntent(MatchEngine engine, int teamId) =>
            new DistributeIntent
            {
                DeliveryKind = DeliveryKind.Throw,
                TargetReceiverId = OutfieldTeammate(engine, teamId),
                TargetPoint = new Vector3(30f, 20f, 0f),
                PowerIntent = 0.7f,
                SpinIntent = Vector3.zero
            };

        private static void DriveTacticalAt(MatchEngine engine, int frame)
        {
            engine.TestOnly_SetPhysicsFrame(frame);
            engine.TestOnly_DriveGkHeadingTactical();
        }

        private static int GoalkeeperForTeam(MatchEngine engine, int teamId)
        {
            int start = teamId * MatchEngineConstants.PLAYERS_PER_TEAM;
            int end = start + MatchEngineConstants.PLAYERS_PER_TEAM;
            for (int i = start; i < end; i++)
            {
                if (engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int OutfieldTeammate(MatchEngine engine, int teamId)
        {
            int start = teamId * MatchEngineConstants.PLAYERS_PER_TEAM;
            int end = start + MatchEngineConstants.PLAYERS_PER_TEAM;
            for (int i = start; i < end; i++)
            {
                if (!engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Deep copy: CaptureState exposes #11's live arrays, which a fixture must not write.</summary>
        private static GoalkeeperTickState Clone(GoalkeeperTickState s) =>
            new GoalkeeperTickState(
                (GoalkeeperState[])s.States.Clone(),
                (GoalkeeperAgentAttributes[])s.Attrs.Clone(),
                (GkContactState[])s.ContactStates.Clone(),
                (SaveIntent[])s.SaveIntents.Clone(),
                (bool[])s.SaveIntentActive.Clone(),
                (ClaimIntent[])s.ClaimIntents.Clone(),
                (bool[])s.ClaimIntentActive.Clone(),
                (RushIntent[])s.RushIntents.Clone(),
                (bool[])s.RushIntentActive.Clone(),
                (DistributeIntent[])s.DistributeIntents.Clone(),
                (bool[])s.DistributeIntentActive.Clone(),
                (GoalkeeperPositioningContract[])s.PositioningContracts.Clone(),
                (int[])s.DiveLaunchFrames.Clone(),
                (int[])s.DiveDurationFrames.Clone(),
                (float[])s.DivePeakHandZ.Clone(),
                (float[])s.DiveDirectionLateral.Clone(),
                (float[])s.RushLaunchMps.Clone(),
                (int[])s.RushInitialAttackerId.Clone(),
                (float[])s.ShotDetectedTickMs.Clone(),
                (float[])s.RequiredReactionMs.Clone(),
                (bool[])s.ShotEventPending.Clone(),
                (int[])s.ClaimTick.Clone(),
                (int[])s.ReleaseTickEarliest.Clone(),
                (int[])s.RecoveryCooldownEndTick.Clone());
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-09-26 | —      | Initial: ERR-011-017 tactical-tick clock lock + #11 §3.8.3 hand-episode      |
// |         |            |        | teardown locks (possession loss, unchanged holder, no-episode no-op, restart |
// |         |            |        | by the same keeper), mirrored for both teams.                                |
// | 1.1     | 2026-09-26 | —      | Review closure: the loss case no longer asserts an intent it staged false;    |
// |         |            |        | new PossessionLoss_ClearsALiveDistributeIntent stages a live non-default     |
// |         |            |        | intent in HandsOnBall and in Distributing and proves the teardown clears it. |
#endregion
