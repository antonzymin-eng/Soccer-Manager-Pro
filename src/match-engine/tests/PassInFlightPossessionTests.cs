// File:     src/match-engine/tests/PassInFlightPossessionTests.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Positioning AI #12 §3.0.1 / §3.0.2 / FR-PA-022 (ERR-012-011); Match Engine composition
//           root (docs/tracking/match-engine-design.md §2.6); Testing Strategy #19; Code Standards #20
// Purpose:  Locks for wiring-backlog C1 — the pass-in-flight possession latch that gives #12 a TEAM
//           possession answer instead of an on-ball-carrier one.
//
//           The behaviour these pin: A TEAM KEEPS THE BALL WHILE A PASS IT PLAYED IS TRAVELLING TO A
//           TEAM-MATE. The engine clears _possessingAgentId at every ApplyKick and re-acquires it only
//           on physical receipt, so before this landing the whole flight of every pass read as a loose
//           ball and #12 §3.0.2's velocity branch classified a passing team as being in transition —
//           measured InPoss on 7.5% of final-third samples.
//
//           Three layers, because a defect at any one of them is invisible at the others:
//             (1) the latch's own lifecycle — armed at the kick, and every way it ends;
//             (2) the composition into the #12 snapshot, which is where the union of the two
//                 possession surfaces is formed, mirrored HOME and AWAY (the phase is computed from a
//                 mirrored snapshot with a hand-written BallVxFiltered sign flip, and three home/away
//                 asymmetry defects have shipped here because every fixture used the home team);
//             (3) the v20 snapshot round trip taken WHILE A PASS IS IN FLIGHT — a round trip taken at
//                 the latch's default value would round-trip 0/-1 either way and stay green with the
//                 serialization deleted.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>ERR-012-011 locks — the pass-in-flight latch, its composition into #12's snapshot,
    /// and its survival across a restore.</summary>
    [TestFixture]
    public sealed class PassInFlightPossessionTests
    {
        private const ulong MatchSeed = 0x0F1E2D3C4B5A6978UL;

        // Two home team-mates and one away agent. Home occupies roster slots [0..10].
        private const int HomeAgentA = 3;
        private const int HomeAgentB = 7;
        private const int AwayAgentA = 14;
        private const int AwayAgentB = 18;

        private static PassRequest GroundPass(int from, int to, int teamId) => new PassRequest
        {
            AgentId          = from,
            PassType         = PassType.Ground,
            CrossSubType     = CrossSubType.Flat,
            TargetAgentId    = to,
            TargetPosition   = Vector3.zero,
            IntendedDistance = 10f,
            Urgency          = 0.5f,
            IsWeakFoot       = false,
            TeamId           = teamId,
            FrameNumber      = 1
        };

        /// <summary>
        /// Drives a scripted pass to the CONTACT kick and stops on the FIRST tick at which the ball has
        /// actually left — which is the tick the latch arms. Returns false if the kick never happened,
        /// so a fixture that silently failed to produce a pass can never be read as a passing test.
        /// </summary>
        private static bool RunToKick(MatchEngine engine, int passer, int maxTicks = 200)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                engine.RunTick();
                if (engine.TestOnly_PossessingAgentId == MatchEngineConstants.NO_POSSESSION
                    && engine.TestOnly_BallSnapshot.Velocity.sqrMagnitude > 0f)
                {
                    return true;
                }
                if (engine.TestOnly_PassExecutorIdle(passer) && i > 2)
                {
                    return false;   // executor finished without ever releasing the ball
                }
            }
            return false;
        }

        // ── Layer 1: the latch lifecycle ─────────────────────────────────────────────────

        [Test]
        public void Kick_ArmsTheLatchOnTheIntendedReceiver()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(HomeAgentA);
            PassRequest request = GroundPass(HomeAgentA, HomeAgentB, teamId: 0);
            Assert.AreEqual(PassOutcome.Initiated,
                engine.TestOnly_InitiatePass(HomeAgentA, in request).Outcome,
                "Fixture must actually initiate a pass.");

            Assert.IsTrue(RunToKick(engine, HomeAgentA), "The scripted pass must reach its CONTACT kick.");

            Assert.AreEqual(HomeAgentB, engine.TestOnly_PassInFlightReceiverId,
                "The kick must latch the team-mate the pass was aimed at — this is the whole fix.");
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "Precondition: nobody is ON the ball during the flight. If this ever fails the test "
                + "above is passing for the wrong reason.");
        }

        [Test]
        public void ArmPassInFlight_RefusesATargetThatIsNotALiveTeamMate()
        {
            var engine = new MatchEngine(MatchSeed);

            engine.TestOnly_ArmPassInFlight(HomeAgentA, AwayAgentA);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "An opponent is not a team-mate — a ball played to him is not this team's possession.");

            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentA);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "A pass to oneself is not a pass.");

            engine.TestOnly_ArmPassInFlight(HomeAgentA, MatchEngineConstants.NO_POSSESSION);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "A pass aimed at nobody leaves the ball loose, which is the honest classification.");

            engine.TestOnly_ArmPassInFlight(HomeAgentA, MatchEngineConstants.SQUAD_SIZE + 5);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "An out-of-range target must be refused, not indexed.");

            // The positive control: without it every assertion above could pass on a latch that never
            // arms at all, which is this project's recorded tautology class.
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);
            Assert.AreEqual(HomeAgentB, engine.TestOnly_PassInFlightReceiverId,
                "A live team-mate MUST arm the latch — otherwise the refusals above prove nothing.");
        }

        [Test]
        public void Latch_ClearsWhenAnybodyEstablishesPossession()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);
            Assert.AreEqual(HomeAgentB, engine.TestOnly_PassInFlightReceiverId, "Precondition: armed.");

            engine.TestOnly_SetPossession(HomeAgentB);
            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "The pass is over once somebody has the ball — received, intercepted or picked up.");
        }

        [Test]
        public void Latch_ClearsWhenTheBallIsNoLongerTravellingToTheReceiver()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);

            // Drive the ball hard AWAY from the receiver, from a position well clear of every agent so
            // no first touch intervenes and decides this for a different reason.
            Vector2 receiver = engine.TestOnly_AgentSnapshot(HomeAgentB).Position;
            Vector2 away = (receiver - engine.TestOnly_AgentSnapshot(HomeAgentA).Position).normalized;
            Vector3 ballPos = new Vector3(
                receiver.x + away.x * 20f, receiver.y + away.y * 20f,
                MatchEngineConstants.BALL_REST_HEIGHT_M);

            // Ball 20 m PAST the receiver and still running away from him, clear of everyone.
            engine.TestOnly_ForceBallLoose(ballPos, new Vector3(away.x, away.y, 0f) * 15f);
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);
            Assert.AreEqual(HomeAgentB, engine.TestOnly_PassInFlightReceiverId, "Precondition: armed.");

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "A ball travelling away from its intended receiver is a loose ball, not possession.");
        }

        [Test]
        public void Latch_ClearsWhenTheBallHasStopped()
        {
            var engine = new MatchEngine(MatchSeed);
            // A stationary ball is not approaching anybody: dot == 0 ⇒ not approaching. This is the
            // overhit / ran-out-of-momentum case, and it is why the expiry needs no timeout [GT].
            engine.TestOnly_ForceBallLoose(new Vector3(52.5f, 34f, MatchEngineConstants.BALL_REST_HEIGHT_M),
                                           Vector3.zero);
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);
            Assert.AreEqual(HomeAgentB, engine.TestOnly_PassInFlightReceiverId, "Precondition: armed.");

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "A pass that ran out of momentum short of its target has ended.");
        }

        [Test]
        public void Latch_IsMonotone_NothingButAKickCanReArmIt()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_ForceBallLoose(new Vector3(52.5f, 34f, MatchEngineConstants.BALL_REST_HEIGHT_M),
                                           Vector3.zero);
            engine.TestOnly_ArmPassInFlight(HomeAgentA, HomeAgentB);
            engine.RunTick();
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "Precondition: the latch expired.");

            // Twenty ticks of ordinary play must not resurrect it. The expiry being monotone is what
            // makes a dot ≈ 0 flicker a one-shot early clear rather than an oscillation — and #12's
            // PHASE_HYSTERESIS_TICKS is NOT a safety net for oscillation: PhaseClassifier resets its
            // dwell count whenever the candidate differs, so an alternating candidate never commits.
            for (int i = 0; i < 20; i++)
            {
                engine.RunTick();
                Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                    $"The latch re-armed without a kick at tick {i} — the expiry is not monotone.");
            }
        }

        [Test]
        public void Shot_EndsThePreviousPass_EvenWhenTheBallStillFliesTowardTheReceiver()
        {
            // ISOLATION. Deleting ClearPassInFlight() from the shot adapter left every other lock in
            // this file green, because in the ordinary case the struck ball flies somewhere else and
            // the receding rule clears the latch anyway. That made the explicit clear a claim with no
            // test behind it — the "which test fails if the fix is reverted" question, failed. This
            // case constructs the one geometry where the receding rule CANNOT do the job: the ball is
            // driven at goal and the intended receiver is the most advanced team-mate, i.e. goal-side
            // of the shooter, so the shot's own velocity still points at him.
            var engine = new MatchEngine(MatchSeed);

            // Kickoff puts every home outfielder on ONE line (x = 26.25), and a shot outruns all of
            // them within the kick tick, so no team-mate is ever downrange at tick 0. Let the shape
            // develop first, then pick a shooter with a team-mate genuinely ahead of him toward the
            // goal home attacks. Both attempts at deriving this geometry by reasoning were wrong and
            // were caught by the adequacy assertion below — that assertion, not this comment, is what
            // makes the test honest.
            for (int i = 0; i < 900; i++) engine.RunTick();
            PickShooterWithDownrangeTeamMate(engine, out int shooter, out int receiver);

            // The shot must start at the shooter's feet, or the "downrange" relation is measured from
            // wherever the ball happened to be instead.
            Vector2 shooterPos = engine.TestOnly_AgentSnapshot(shooter).Position;
            engine.TestOnly_ForceBallLoose(
                new Vector3(shooterPos.x, shooterPos.y, MatchEngineConstants.BALL_REST_HEIGHT_M),
                Vector3.zero);

            engine.TestOnly_SetPossession(shooter);
            var shot = new ShotRequest
            {
                AgentId         = shooter,
                PowerIntent     = 0.7f,
                ContactZone     = ContactZone.Centre,
                SpinIntent      = 0f,
                PlacementTarget = new Vector2(0.5f, 0.5f),
                IsWeakFoot      = false,
                DistanceToGoal  = 20f,
                TeamId          = 0,
                FrameNumber     = 1
            };
            Assert.AreEqual(ShotOutcome.Initiated, engine.TestOnly_InitiateShot(shooter, in shot).Outcome,
                "Fixture must initiate a shot.");

            // Re-arm every tick through the windup so the latch is genuinely live at the moment the
            // shot's ApplyKick fires, then stop on that tick.
            bool kicked = false;
            for (int i = 0; i < 200 && !kicked; i++)
            {
                engine.TestOnly_ArmPassInFlight(shooter, receiver);
                Vector3 before = engine.TestOnly_BallSnapshot.Velocity;
                engine.RunTick();
                Vector3 after = engine.TestOnly_BallSnapshot.Velocity;
                kicked = (after - before).sqrMagnitude > 1f
                         && engine.TestOnly_PossessingAgentId == MatchEngineConstants.NO_POSSESSION;
            }
            Assert.IsTrue(kicked, "The scripted shot must reach its CONTACT kick.");

            // FIXTURE ADEQUACY, asserted rather than assumed: if the struck ball were running away
            // from the receiver, the receding rule would clear the latch and this test would pass
            // with the shot-adapter clear deleted — exactly the hole it exists to close.
            Vector2 ballVel = new Vector2(engine.TestOnly_BallSnapshot.Velocity.x,
                                          engine.TestOnly_BallSnapshot.Velocity.y);
            Vector2 toReceiver = engine.TestOnly_AgentSnapshot(receiver).Position
                                 - new Vector2(engine.TestOnly_BallSnapshot.Position.x,
                                               engine.TestOnly_BallSnapshot.Position.y);
            Assert.Greater(Vector2.Dot(ballVel, toReceiver), 0f,
                "Inadequate fixture: the struck ball must still be travelling TOWARD the intended "
                + "receiver, or the receding rule — not the shot adapter — is what clears the latch.");

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PassInFlightReceiverId,
                "A shot is a strike on the ball, and any strike ends the previous pass. The ball is "
                + "no longer 'travelling to a team-mate' just because a team-mate is downrange of it.");
        }

        /// <summary>Picks a home outfielder to shoot and a home team-mate at least
        /// <c>MinDownrangeM</c> further toward the goal home attacks (x = PITCH_LENGTH), so a shot
        /// driven at that goal is still travelling toward the team-mate after the kick.</summary>
        private static void PickShooterWithDownrangeTeamMate(
            MatchEngine engine, out int shooter, out int receiver)
        {
            const float MinDownrangeM = 25f;

            shooter = -1; receiver = -1;
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < MatchEngineConstants.PLAYERS_PER_TEAM; i++)
            {
                if (engine.TestOnly_IsGoalkeeper(i) || engine.TestOnly_IsSentOff(i)) continue;
                float x = engine.TestOnly_AgentSnapshot(i).Position.x;
                if (x < minX) { minX = x; shooter = i; }
                if (x > maxX) { maxX = x; receiver = i; }
            }

            Assert.GreaterOrEqual(shooter, 0, "Fixture needs a home outfielder to shoot.");
            Assert.AreNotEqual(shooter, receiver, "Fixture needs two distinct home outfielders.");
            Assert.Greater(maxX - minX, MinDownrangeM,
                "Fixture needs a home team-mate well downrange of the shooter; the developed shape "
                + "did not provide one, so this test would not isolate the shot-adapter clear.");
        }

        // ── Layer 2: composition into the #12 snapshot, home and away ────────────────────

        [TestCase(0, HomeAgentA, HomeAgentB)]
        [TestCase(1, AwayAgentA, AwayAgentB)]
        public void PassInFlight_CommitsInPossForThePassingTeamAndOutOfPossForTheOther(
            int passingTeam, int passer, int receiver)
        {
            var engine = new MatchEngine(MatchSeed);
            int otherTeam = 1 - passingTeam;

            // The engine boots with the kickoff taker ON the ball, so put the ball into the state this
            // test is about first: in flight, nobody holding it.
            KeepBallTravellingToward(engine, receiver);
            engine.TestOnly_ArmPassInFlight(passer, receiver);
            Assert.AreEqual(receiver, engine.TestOnly_PassInFlightReceiverId, "Precondition: armed.");
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "Precondition: nobody is ON the ball — this is exactly the state that used to read "
                + "as a transition for both teams.");

            // Hold the latch across enough ticks for #12's hysteresis to commit the candidate, keeping
            // the ball moving toward the receiver so the expiry does not fire mid-test.
            for (int i = 0; i < PositioningAI.PositioningAIConstants.PHASE_HYSTERESIS_TICKS * 8; i++)
            {
                KeepBallTravellingToward(engine, receiver);
                engine.TestOnly_ArmPassInFlight(passer, receiver);
                engine.RunTick();
            }

            Assert.AreEqual(PositioningAI.Phase.InPoss, engine.TestOnly_PositioningPhase(passingTeam),
                $"Team {passingTeam} played the ball to its own team-mate and is still in possession "
                + "of it. Before ERR-012-011 this read as a transition.");
            Assert.AreEqual(PositioningAI.Phase.OutOfPoss, engine.TestOnly_PositioningPhase(otherTeam),
                $"Team {otherTeam} does not have the ball, so it is out of possession — not in "
                + "transition. Mirrored case: the phase is computed from a MIRRORED snapshot.");
        }

        /// <summary>Places the ball short of <paramref name="receiver"/> moving toward him, without
        /// letting it arrive — so the latch's expiry rules stay unfired and the test measures the
        /// composition rather than the expiry.</summary>
        private static void KeepBallTravellingToward(MatchEngine engine, int receiver)
        {
            Vector2 r = engine.TestOnly_AgentSnapshot(receiver).Position;
            Vector2 from = new Vector2(r.x - 12f, r.y);
            Vector2 dir = (r - from).normalized;
            engine.TestOnly_ForceBallLoose(
                new Vector3(from.x, from.y, MatchEngineConstants.BALL_REST_HEIGHT_M),
                new Vector3(dir.x, dir.y, 0f) * 8f);
        }

        [Test]
        public void NoTeamInPossession_StillFallsThroughToTheVelocityBranch()
        {
            // The half of §3.0.2 this landing must NOT have changed: a genuinely loose ball is still
            // classified by where it is going. Without this, "everything is InPoss now" would pass
            // every other case in this file.
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < PositioningAI.PositioningAIConstants.PHASE_HYSTERESIS_TICKS * 8; i++)
            {
                engine.TestOnly_ForceBallLoose(
                    new Vector3(52.5f, 34f, MatchEngineConstants.BALL_REST_HEIGHT_M),
                    new Vector3(12f, 0f, 0f));   // hard toward team 0's attacking goal
                engine.RunTick();
            }

            Assert.AreEqual(PositioningAI.Phase.TransToAtk, engine.TestOnly_PositioningPhase(0),
                "A loose ball running toward team 0's target goal is still TransToAtk for team 0.");
            Assert.AreEqual(PositioningAI.Phase.TransToDef, engine.TestOnly_PositioningPhase(1),
                "...and TransToDef for team 1. The velocity branch is untouched by ERR-012-011.");
        }

        // ── Layer 3: v20 — the latch survives a restore ──────────────────────────────────

        [Test]
        public void RoundTrip_TakenWhileAPassIsInFlight_IsDeterministic()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            a.TestOnly_SetPossession(HomeAgentA);
            PassRequest request = GroundPass(HomeAgentA, HomeAgentB, teamId: 0);
            Assert.AreEqual(PassOutcome.Initiated,
                a.TestOnly_InitiatePass(HomeAgentA, in request).Outcome, "Fixture must initiate a pass.");
            Assert.IsTrue(RunToKick(a, HomeAgentA), "The scripted pass must reach its CONTACT kick.");

            // NON-VACUITY. A round trip taken at the latch's default would serialize -1 either way and
            // stay green with the v20 field deleted. The save must happen with a pass genuinely in the
            // air, and this assertion is what makes the rest of the test mean anything.
            Assert.GreaterOrEqual(a.TestOnly_PassInFlightReceiverId, 0,
                "The save must be taken WHILE a pass is in flight, or this locks nothing.");

            SnapshotHeader header = a.CaptureDurableHeader();
            SnapshotPayload payload = a.CaptureDurablePayload();

            const int k = 90;
            var reference = new byte[k][];
            for (int i = 0; i < k; i++)
            {
                a.RunTick();
                reference[i] = a.CurrentSnapshotDigest;
            }

            MatchEngine c = MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed);
            Assert.AreEqual(HomeAgentB, c.TestOnly_PassInFlightReceiverId,
                "The restored engine must resume with the SAME pass in flight. Dropping the v20 field "
                + "re-classifies #12's phase for the remainder of the pass.");

            for (int i = 0; i < k; i++)
            {
                c.RunTick();
                CollectionAssert.AreEqual(reference[i], c.CurrentSnapshotDigest,
                    $"Round-trip digest diverged at tick {i + 1} after a save taken mid-pass.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-08 | —      | ERR-012-011 (wiring backlog C1): the pass-in-flight possession   |
// |         |            |        |   latch. Lifecycle (arm, refusals with a positive control, all   |
// |         |            |        |   three expiries, monotonicity), composition into #12's snapshot |
// |         |            |        |   MIRRORED home and away, the untouched velocity branch, and the |
// |         |            |        |   v20 round trip taken WHILE A PASS IS IN FLIGHT with an         |
// |         |            |        |   explicit non-vacuity precondition.                             |
#endregion
