from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text()
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: expected one match, found {count}: {old[:120]!r}")
    p.write_text(text.replace(old, new, 1))


# Collision System: preserve the old public call and add a feedback overload.
path = "src/collision-system/CollisionSystem.cs"
replace_once(
    path,
    '''        public void UpdateCollisions(
            AgentState[] agentStates,
            PlayerAttributes[] agentAttrs,
            int[] agentTeamIds,
            bool[] agentIsGoalkeeper,
            bool[] knockdownOut,
            float[] knockdownForceOut,
            bool[] stumbleOut,
            ref BallState ball,
            ulong matchSeed,
            int frameNumber,
            float matchTime,
            ICollisionEventConsumer eventConsumer)
        {
            using var _ = s_updateMarker.Auto();
''',
    '''        public void UpdateCollisions(
            AgentState[] agentStates,
            PlayerAttributes[] agentAttrs,
            int[] agentTeamIds,
            bool[] agentIsGoalkeeper,
            bool[] knockdownOut,
            float[] knockdownForceOut,
            bool[] stumbleOut,
            ref BallState ball,
            ulong matchSeed,
            int frameNumber,
            float matchTime,
            ICollisionEventConsumer eventConsumer)
        {
            UpdateCollisions(
                agentStates, agentAttrs, agentTeamIds, agentIsGoalkeeper,
                knockdownOut, knockdownForceOut, stumbleOut,
                ref ball, matchSeed, frameNumber, matchTime, eventConsumer,
                out _);
        }

        /// <summary>
        /// W4 composition overload. Runs the identical collision pipeline and reports whether at least
        /// one confirmed AGENT_BALL contact applied a real Ball Physics deflection during this call.
        /// The result is per-call output only: no latch is retained and CollisionEvent remains unchanged.
        /// </summary>
        /// <param name="ballDeflected">True iff BallCollisionHandler changed ball flight at least once.</param>
        public void UpdateCollisions(
            AgentState[] agentStates,
            PlayerAttributes[] agentAttrs,
            int[] agentTeamIds,
            bool[] agentIsGoalkeeper,
            bool[] knockdownOut,
            float[] knockdownForceOut,
            bool[] stumbleOut,
            ref BallState ball,
            ulong matchSeed,
            int frameNumber,
            float matchTime,
            ICollisionEventConsumer eventConsumer,
            out bool ballDeflected)
        {
            using var _ = s_updateMarker.Auto();
            ballDeflected = false;
''')
replace_once(
    path,
    '''                    bool collided = j == SpatialHashConstants.BALL_ENTITY_ID
                        ? ProcessAgentBall(agentTeamIds, agentIsGoalkeeper, i, ref ball, matchTime)
                        : ProcessAgentAgent(agentTeamIds, i, j, matchTime);
''',
    '''                    bool collided = j == SpatialHashConstants.BALL_ENTITY_ID
                        ? ProcessAgentBall(
                            agentTeamIds, agentIsGoalkeeper, i, ref ball, matchTime, ref ballDeflected)
                        : ProcessAgentAgent(agentTeamIds, i, j, matchTime);
''')
replace_once(
    path,
    '''        private bool ProcessAgentBall(
            int[] teamIds,
            bool[] isGoalkeeper,
            int agentId,
            ref BallState ball,
            float matchTime)
''',
    '''        private bool ProcessAgentBall(
            int[] teamIds,
            bool[] isGoalkeeper,
            int agentId,
            ref BallState ball,
            float matchTime,
            ref bool ballDeflected)
''')
replace_once(
    path,
    '''            BallCollisionHandler.OnAgentCollision(ref ball, in data);

            RecordEvent(matchTime, CollisionType.AGENT_BALL, agentId,
''',
    '''            // W4: contact truth and response truth are distinct. A confirmed overlap still
            // counts toward the collision valve and emits the existing CollisionEvent; only an
            // actually-applied Ball Physics response becomes the keeper new-threat signal.
            if (BallCollisionHandler.OnAgentCollision(ref ball, in data))
            {
                ballDeflected = true; // OR-reduce across AGENT_BALL contacts in this call.
            }

            RecordEvent(matchTime, CollisionType.AGENT_BALL, agentId,
''')


# Goalkeeper Mechanics: a changed flight restarts reaction timing without pretending it was a shot.
path = "src/goalkeeper-mechanics/GoalkeeperMechanics.cs"
marker = '''        /// <summary>
        /// Notifies that a shot has been struck at the specified GK's goal, opening the §3.2 reaction
'''
addition = '''        /// <summary>
        /// W4 new-threat seam: a real body deflection changed the live ball flight during Resolve.
        /// Unlike <see cref="OnThreatArmed"/>, this deliberately overwrites an already-live detection
        /// and required-reaction stamp. Unlike <see cref="OnShotExecutedEvent"/>, it does NOT set
        /// <c>_shotEventPending</c>: a deflection is not a newly struck shot.
        /// </summary>
        public void OnThreatDeflected(
            int gkIndex, float matchTimeMs, float ballSpeedMps, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _attrs[gkIndex] = attrs;
            _shotDetectedTickMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(matchTimeMs, attrs);
            _requiredReactionMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeedMps, _states[gkIndex]);
        }

'''
replace_once(path, marker, addition + marker)


# Unit lock on the dedicated deflection seam.
path = "src/goalkeeper-mechanics/Tests/GoalkeeperConversionTests.cs"
marker = '''        [Test]
        public void ClearSaveIntent_ClearsDetectionStamp()
'''
test = '''        [Test]
        public void OnThreatDeflected_OverwritesArmingStamp_WithoutShotPending()
        {
            GoalkeeperMechanics gk = NewGk();

            gk.OnThreatArmed(Gk0, 1000f, 20f, MidAttrs());
            float first = gk.CaptureState().ShotDetectedTickMs[Gk0];
            gk.OnThreatDeflected(Gk0, 1400f, 18f, MidAttrs());

            GoalkeeperTickState state = gk.CaptureState();
            float expected = GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(1400f, MidAttrs());
            Assert.AreEqual(expected, state.ShotDetectedTickMs[Gk0], 1e-3f,
                "W4: a real deflection must overwrite the live reaction stamp.");
            Assert.AreNotEqual(first, state.ShotDetectedTickMs[Gk0],
                "W4: the changed flight must not inherit the pre-deflection timing episode.");
            Assert.IsFalse(state.ShotEventPending[Gk0],
                "W4: a body deflection is not a newly struck shot.");
        }

'''
replace_once(path, marker, test + marker)


# Match Engine: LOS gates only DT SAVE. Raw SaveArmed remains episode geometry and rush veto.
path = "src/match-engine/MatchEngine.cs"
replace_once(
    path,
    '''                        bool armed = GkHeadingIntentSource.SaveArmed(
                            t, in _ball.Position, in _ball.Velocity, loose);
                        ctx.SaveAvailable = armed;
''',
    '''                        bool armed = GkHeadingIntentSource.SaveArmed(
                            t, in _ball.Position, in _ball.Velocity, loose);
                        // W4: SAVE emission is perception-aware, but the raw threat episode remains
                        // geometry-owned. TryCommitRushIntents MUST keep vetoing on raw SaveArmed:
                        // being unsighted does not make charging at a goal-bound ball safe.
                        ctx.SaveAvailable = KeeperPerceptionGate.SaveAvailable(
                            t, i, _agents[i].Position,
                            _ball.Position, _ball.Velocity, loose,
                            _agents, _isSentOff);
''')
replace_once(
    path,
    '''                frameNumber: frameNumber,
                matchTime: matchTime,
                eventConsumer: _eventConsumer);

            // Match-flow completion (design note §3): apply the (at most one) foul candidate the
''',
    '''                frameNumber: frameNumber,
                matchTime: matchTime,
                eventConsumer: _eventConsumer,
                ballDeflected: out bool ballDeflected);

            // W4: consume an APPLIED flight change immediately in this Resolve phase. No pending
            // deflection latch survives the tick; existing GK reaction fields remain the only state.
            if (_gkHeadingEnabled && ballDeflected)
            {
                ResetKeeperReactionAfterDeflection();
            }

            // Match-flow completion (design note §3): apply the (at most one) foul candidate the
''')
phase_marker = '''        /// <summary>Phase 4 — Resolve. Runs collision (×22), advances the in-flight pass/shot executor
'''
helper = '''        /// <summary>
        /// W4 same-Resolve deflection consumer. A changed flight restarts reaction timing only for
        /// the keeper whose goal the POST-deflection ball threatens under raw SaveArmed geometry.
        /// LOS deliberately gates DT SAVE emission, not existence of the reaction episode.
        /// </summary>
        private void ResetKeeperReactionAfterDeflection()
        {
            // Resolve may publish a queued substitution after Physics last refreshed this map.
            RefreshGkAgentIds();
            bool loose = _possessingAgentId == MatchEngineConstants.NO_POSSESSION;

            for (int k = 0; k < _gkAgentIds.Length; k++)
            {
                int agentId = _gkAgentIds[k];
                if (agentId < 0 || _isSentOff[agentId])
                {
                    continue;
                }

                if (!GkHeadingIntentSource.SaveArmed(
                        k, in _ball.Position, in _ball.Velocity, loose))
                {
                    continue;
                }

                _goalkeeper.OnThreatDeflected(
                    k,
                    _clock.CurrentMatchTimeMs,
                    _ball.Velocity.magnitude,
                    PlayerAttributeProjection.ToGoalkeeper(
                        in _canonicalAttrs[agentId], k, fatigue: 0f));
            }
        }

'''
replace_once(path, phase_marker, helper + phase_marker)


# Regression locks.
Path("src/collision-system/tests/CollisionDeflectionFeedbackTests.cs").write_text(r'''// File:     src/collision-system/tests/CollisionDeflectionFeedbackTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Collision System #3 §3.4.3; Match-engine wiring backlog W4; Code Standards #20
// Purpose:  Lock W4 per-call feedback: real response reports a deflection; separating overlap does not.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem.Tests
{
    [TestFixture]
    public sealed class CollisionDeflectionFeedbackTests
    {
        private static bool Run(Vector3 ballVelocity, out BallState ball)
        {
            var system = new CollisionSystem();
            var agents = new[] { AgentState.CreateAtPosition(new Vector2(10.3f, 34f), Vector2.left) };
            var attrs = new[] { PlayerAttributes.CreateDefault() };
            var teams = new[] { 0 };
            var keepers = new[] { false };
            var knockdown = new bool[1];
            var force = new float[1];
            var stumble = new bool[1];
            ball = new BallState {
                Position = new Vector3(10f, 34f, 0.5f),
                Velocity = ballVelocity,
                State = BallStateType.Airborne
            };

            system.UpdateCollisions(
                agents, attrs, teams, keepers, knockdown, force, stumble,
                ref ball, 123UL, 1, 0.016f, null, out bool deflected);
            return deflected;
        }

        [Test]
        public void AppliedAgentBallResponse_ReportsDeflection()
        {
            float speed = BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f;
            bool deflected = Run(new Vector3(speed, 0f, 0f), out BallState ball);
            Assert.IsTrue(deflected);
            Assert.Less(ball.Velocity.x, 0f);
        }

        [Test]
        public void SeparatingOverlap_DoesNotReportDeflection()
        {
            float speed = BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f;
            bool deflected = Run(new Vector3(-speed, 0f, 0f), out BallState ball);
            Assert.IsFalse(deflected);
            Assert.Less(ball.Velocity.x, 0f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-11 | —      | W4: applied-vs-overlap collision feedback regression locks. |
#endregion
''')
Path("src/collision-system/tests/CollisionDeflectionFeedbackTests.cs.meta").write_text(
    "fileFormatVersion: 2\nguid: 49bf0f10cdf34a6581ac427c7284317c\n")

Path("src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs").write_text(r'''// File:     src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs
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
        private static readonly Vector2 ScreenPosition = new Vector2(2.5f, 34f);

        private static int FindOutfielder(MatchEngine engine, int team)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) == team && !engine.AgentIsGoalkeeper(i)) return i;
            }
            return -1;
        }

        private static void StageOpponentScreen(MatchEngine engine, int screenAgent)
        {
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
            int screen = FindOutfielder(engine, AwayTeam);
            Assert.GreaterOrEqual(screen, 0);
            Assert.IsTrue(GkHeadingIntentSource.SaveArmed(
                HomeTeam, ThreatPosition, ThreatVelocity, ballLoose: true));

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                StageOpponentScreen(engine, screen);
                engine.RunTick();
            }

            Assert.IsFalse(engine.TestOnly_SaveCommittedForGk(HomeTeam),
                "W4: a body-screened goal-bound threat must not make the Decision Tree emit SAVE.");
        }

        [Test]
        public void OpponentScreenedGoalBoundThreat_DoesNotArmRush()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x55UL);
            int screen = FindOutfielder(engine, AwayTeam);
            Assert.GreaterOrEqual(screen, 0);
            Assert.IsTrue(GkHeadingIntentSource.SaveArmed(
                HomeTeam, ThreatPosition, ThreatVelocity, ballLoose: true));

            for (int i = 0; i < 5; i++)
            {
                StageOpponentScreen(engine, screen);
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
''')
Path("src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs.meta").write_text(
    "fileFormatVersion: 2\nguid: 77e54dbe8a254834ab0a595648ab2ac6\n")
