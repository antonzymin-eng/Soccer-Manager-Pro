// File:     src/match-engine/tests/GkRushTriggerTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Keeper rush trigger design supplement (docs/tracking/gk-rush-trigger-design.md) §2;
//           Goalkeeper Mechanics #11 §3.7 / KD-15; Testing Strategy #19; Code Standards #20
// Purpose:  Locks for wiring backlog W1 — the trigger that gave GoalkeeperMechanics.CommitRushIntent
//           its first production caller. Two layers: the pure §4.4 geometry (RushArmed and the
//           intercept race behind it), and the composed chain predicate → commit → Rushing driven
//           through a real MatchEngine.
//
//           Every geometry case is mirrored home and away. Three home/away asymmetry defects have
//           shipped in this project because every spec example and every fixture used the home team
//           (#8 ERR-008-002), and this predicate branches on the defended goal line.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>W1 rush-trigger locks — pure geometry, then the composed commit chain.</summary>
    [TestFixture]
    public sealed class GkRushTriggerTests
    {
        private const float PitchY = 34f;

        /// <summary>A rush speed comfortably above any ball speed used below, so a refusal in these
        /// fixtures is always the predicate's decision and never an accidental lost race.</summary>
        private const float RushSpeed = 5.5f;

        /// <summary>Nobody is anywhere near the ball, so the last-man test always passes.</summary>
        private const float NoCover = 1000f;

        private static float GoalX(int keeperTeam) =>
            keeperTeam == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;

        /// <summary>A point <paramref name="metres"/> in front of the goal this keeper defends.</summary>
        private static float OutFromGoal(int keeperTeam, float metres) =>
            keeperTeam == 0 ? metres : MatchEngineConstants.PITCH_LENGTH_M - metres;

        // ── §4.4 RushArmed: the cases a keeper SHOULD come for ───────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_LooseBallInFrontOfGoalWithNoCover_Arms(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed, "a loose ball in front of the goal with no cover is the sweeper case");
            Assert.AreEqual(OutFromGoal(keeperTeam, 12f), target.x, 0.01f,
                "a stationary ball's meeting point is the ball itself");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_UnattendedOpponentInPossession_Arms(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed, "an attacker through on goal with no defender nearer the ball is a 1v1");
            Assert.AreEqual(OutFromGoal(keeperTeam, 12f), target.x, 0.01f,
                "against a carrier the target is the ball — the 1v1 and smother radii do the rest");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_MovingLooseBall_LocksTheMeetingPointNotTheBall(int keeperTeam)
        {
            // The ball rolls across the face of goal at 2 m/s — slow enough that a 5.5 m/s keeper wins
            // the race, which a perpendicular 6 m/s ball at this separation correctly would not be.
            // KD-15 locks the target at commit, so aiming at where the ball IS sends him to where it was.
            float ballX = OutFromGoal(keeperTeam, 10f);

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(ballX, PitchY, 0.11f),
                new Vector3(0f, 2f, 0f),
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed);
            Assert.Greater(target.y, PitchY,
                "the locked target must lead the ball along its travel, not sit on its current position");
        }

        // ── §4.4 RushArmed: the cases a keeper should NOT come for ───────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_OwnTeamHasTheBall_DoesNotArm(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: true,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "there is nothing to come for when a team-mate has the ball");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_DefenderNearerTheBall_DoesNotArm(int keeperTeam)
        {
            // The last-man test, and the reason the predicate needs no case analysis: a chasing defender
            // 2 m off the ball is exactly the signal that the keeper should hold his line.
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: 2f,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "a team-mate nearer the ball than the keeper deals with it");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_BallTooFarFromTheDefendedGoal_DoesNotArm(int keeperTeam)
        {
            float far = MatchEngineConstants.GkRushTriggerRangeM + 10f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, far - 2f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, far), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "keepers do not sweep at the halfway line");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_HighBall_DoesNotArm(int keeperTeam)
        {
            float high = MatchEngineConstants.GkRushMaxBallHeightM + 1f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, high),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "that ball is a cross to be claimed (backlog W3), not one to be swept");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_ClearanceOutrunningTheKeeper_DoesNotArm(int keeperTeam)
        {
            // A cleared ball leaving at 25 m/s cannot be caught by a keeper running at 5.5 m/s, so the
            // intercept quadratic has no non-negative root. The solve IS the guard: without it the
            // keeper would set off after every clearance and never come back.
            float away = keeperTeam == 0 ? +25f : -25f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 10f), PitchY, 0.11f),
                new Vector3(away, 0f, 0f),
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "a ball outrunning the keeper is not a race he can win");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_RaceLongerThanTheCap_DoesNotArm(int keeperTeam)
        {
            // Inside the trigger range but far enough that the run exceeds GkRushMaxInterceptS at this
            // speed: 21 m at 5.5 m/s is 3.8 s against a 2.0 s cap.
            float gkOut = MatchEngineConstants.GkRushTriggerRangeM - 1f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(GoalX(keeperTeam), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, gkOut), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                nearestOutfieldTeammateDistM: NoCover,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "the keeper does not commit to a race longer than GkRushMaxInterceptS");
        }

        // ── The intercept solve ──────────────────────────────────────────────────────────

        [Test]
        public void TrySolveRushIntercept_StationaryBall_MeetsItWhereItLies()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(11f, 0f, 0.11f), Vector3.zero, 5.5f,
                out float t, out Vector3 meet);

            Assert.IsTrue(solved);
            Assert.AreEqual(2.0f, t, 0.01f, "11 m at 5.5 m/s is 2 s");
            Assert.AreEqual(11f, meet.x, 0.01f);
        }

        [Test]
        public void TrySolveRushIntercept_BallAtTheKeeper_SolvesAtZero()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(10f, 34f, 0f), new Vector3(10f, 34f, 0.11f), Vector3.zero, 5.5f,
                out float t, out _);

            Assert.IsTrue(solved);
            Assert.AreEqual(0f, t, 1e-4f, "already there — the earliest meeting time is now");
        }

        [Test]
        public void TrySolveRushIntercept_BallFasterAndReceding_HasNoSolution()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0.11f), new Vector3(20f, 0f, 0f), 5.5f,
                out _, out _);

            Assert.IsFalse(solved, "a receding ball faster than the keeper is never met");
        }

        [Test]
        public void TrySolveRushIntercept_BallFasterButClosing_IsMet()
        {
            // A fast ball travelling TOWARD the keeper is caught even though he could never chase it —
            // the sign of the closing term, not the speed comparison, is what decides.
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(20f, 0f, 0.11f), new Vector3(-20f, 0f, 0f), 5.5f,
                out float t, out Vector3 meet);

            Assert.IsTrue(solved);
            Assert.Greater(t, 0f);
            Assert.Less(meet.x, 20f, "they meet somewhere between them");
        }

        [Test]
        public void TrySolveRushIntercept_ZeroRushSpeed_HasNoSolution()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0.11f), Vector3.zero, 0f, out _, out _);

            Assert.IsFalse(solved, "a keeper who cannot move reaches nothing");
        }

        // ── The composed chain: predicate → commit → Rushing ─────────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_UncoveredLooseBallInTheBox_CommitsARushAndLaunchesIt(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);

            int keeper = KeeperAgentId(engine, keeperTeam);
            Assert.GreaterOrEqual(keeper, 0, "fixture needs a keeper on the pitch");

            // Put the keeper on his line and every team-mate at the halfway line, so the last-man test
            // is unambiguous rather than dependent on the kickoff formation.
            engine.TestOnly_SetAgent(keeper, AgentAt(OutFromGoal(keeperTeam, 5.5f), PitchY));
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i != keeper && engine.AgentTeamId(i) == keeperTeam)
                {
                    engine.TestOnly_SetAgent(i, AgentAt(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, PitchY));
                }
            }

            // A stationary loose ball 11 m out: inside GkRushTriggerRangeM, a 5.5 m run that any keeper
            // wins inside the intercept cap, and too slow to arm SaveArmed (so the save exclusion, which
            // outranks the rush, is not what we are measuring here).
            engine.TestOnly_ForceBallLoose(
                new Vector3(OutFromGoal(keeperTeam, 11f), PitchY, 0.11f), Vector3.zero);

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "no rush has ever been committed before the drive — the W1 baseline for every match " +
                "this engine has ever played");

            // Three tactical drives: Resting → Set (commit happens here, from a state that has a
            // → Rushing row) → Anticipate → Rushing. The commit precedes the tactical tick inside the
            // drive, which is why the intent is seen the same stride it is made.
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();

            Assert.AreEqual(1, engine.TestOnly_RushCommitCount,
                "the trigger must commit exactly once per episode (KD-15 locks the target at commit)");
            Assert.AreEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "a committed rush above RushCommitThreshold must launch");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_BallCoveredByADefender_CommitsNothing(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);

            int keeper = KeeperAgentId(engine, keeperTeam);
            engine.TestOnly_SetAgent(keeper, AgentAt(OutFromGoal(keeperTeam, 5.5f), PitchY));

            // One team-mate right on the ball; everyone else out of the way. The keeper stays.
            bool placedCover = false;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i == keeper || engine.AgentTeamId(i) != keeperTeam)
                {
                    continue;
                }
                if (!placedCover)
                {
                    engine.TestOnly_SetAgent(i, AgentAt(OutFromGoal(keeperTeam, 11.5f), PitchY));
                    placedCover = true;
                }
                else
                {
                    engine.TestOnly_SetAgent(i, AgentAt(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, PitchY));
                }
            }
            Assert.IsTrue(placedCover, "fixture needs at least one outfielder to act as cover");

            engine.TestOnly_ForceBallLoose(
                new Vector3(OutFromGoal(keeperTeam, 11f), PitchY, 0.11f), Vector3.zero);

            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "the last-man test must keep the keeper home when a defender is nearer the ball");
            Assert.AreNotEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam));
        }

        // ── Fixture ──────────────────────────────────────────────────────────────────────

        private static TacticalDirector.AgentMovement.AgentState AgentAt(float x, float y)
        {
            var a = new TacticalDirector.AgentMovement.AgentState();
            a.Position = new Vector2(x, y);
            return a;
        }

        private static int KeeperAgentId(MatchEngine engine, int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial. Wiring backlog W1: RushArmed's arming and refusing    |
// |         |            |        | cases (every one mirrored home and away), the intercept solve  |
// |         |            |        | including the receding-clearance no-solution guard, and the    |
// |         |            |        | composed predicate → commit → Rushing chain through a real     |
// |         |            |        | MatchEngine with its last-man refusal.                         |
#endregion
