// File:     src/goalkeeper-mechanics/Tests/GoalkeeperClaimTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5.2 (ERR-011-008), Testing Strategy #19, Code Standards #20
// Purpose:  Unit locks for the §3.5.2 band-to-action contract, which ERR-011-008 found
//           half-implemented: the catch branch is TWO statements — the possession record AND the
//           velocity park ("parked at hand position") — and only the first was written. Possession
//           is a flag in the composed engine, not a kinematic constraint, so a claimed shot kept
//           travelling and went in (measured over three full matches: ball speed 11.1 m/s in,
//           10.8 m/s out of a catch, 7 of 10 catches followed by a goal within 5 s).
//
//           The locks assert the contract, not a band mix: every contact resolves to EXACTLY ONE
//           ball-side action, a claim parks and never kicks, and a non-claim kicks and never parks.

using System.Globalization;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    [TestFixture]
    internal class GoalkeeperClaimTests
    {
        private const int Gk0 = 0;
        private const int Gk1 = 1;
        private const int AgentCount = 22;

        private static readonly int[] GkAgentIds = { Gk0, Gk1 };

        /// <summary>A contact publishes SaveAttemptedEvent (and BallClaimedEvent on a claim), unlike
        /// the deliberately publish-free sibling fixtures — so the #11 ordinals are registered and
        /// the bus reset per test. <c>Initialize</c> is idempotent (FR-EVT-020).</summary>
        [SetUp]
        public void SetUp()
        {
            EventBusRegistrar.Initialize();
            EventBus.ResetForNewMatch();

            // #11's Update runs inside the engine's Physics phase (MatchEngine.RunPhysicsPhase →
            // DriveGkHeadingPhysics), and a Tier A event asserts on its producer phase. Declaring
            // the phase here reproduces the production context rather than muting the assertion.
            EventBus.BeginPhase(PhaseId.Physics);
        }

        [TearDown]
        public void TearDown() => EventBus.ResetForNewMatch();

        // ── ERR-011-008: a claim arrests the ball ────────────────────────────────────────

        [Test]
        public void Claim_ParksTheBall_AndDoesNotKickIt()
        {
            var ball = new RecordingBallSystem();
            GoalkeeperMechanics gk = new GoalkeeperMechanics(ball, new ZeroRng());

            float quality = DriveContact(gk, EliteAttrs());

            // Elite handling forces the catch band regardless of the reaction window: attrFactor is
            // HandlingBase + HandlingKAttr = 1.20 at Handling_norm 1, so the blend floor
            // (HANDLING_REACTION_BLEND_ALPHA × 1.20 = 0.84) already clears CatchThreshold.
            Assert.That(quality, Is.GreaterThanOrEqualTo(GoalkeeperConstants.CatchThreshold),
                "fixture no longer produces a claim; quality=" + Inv(quality));

            Assert.AreEqual(1, ball.ParkBallCount, "a claim must park the ball (ERR-011-008)");
            Assert.AreEqual(1, ball.SetPossessorCount, "a claim must record possession");
            Assert.AreEqual(0, ball.ApplyKickCount, "a claim must not kick the ball away");
        }

        [Test]
        public void NonClaimContact_KicksTheBall_AndDoesNotPark()
        {
            var ball = new RecordingBallSystem();
            GoalkeeperMechanics gk = new GoalkeeperMechanics(ball, new ZeroRng());

            float quality = DriveContact(gk, PoorAttrs());

            Assert.That(quality, Is.LessThan(GoalkeeperConstants.CatchThreshold),
                "fixture no longer produces a non-claim; quality=" + Inv(quality));
            Assert.That(quality, Is.GreaterThanOrEqualTo(GoalkeeperConstants.MinHandlingQuality),
                "fixture produced a MISS, not a parry/deflect/spill; quality=" + Inv(quality));

            Assert.AreEqual(0, ball.ParkBallCount, "only a claim parks the ball");
            Assert.AreEqual(1, ball.ApplyKickCount, "a parry/deflect/spill redirects the ball");
        }

        [Test]
        public void EveryContact_ResolvesToExactlyOneBallSideAction()
        {
            // The §3.5.2 contract as a whole: park XOR kick, never both, never neither. Run both
            // attribute regimes through one assertion so a future band retune cannot silently
            // create a contact that does nothing to the ball — the ERR-011-008 shape.
            GoalkeeperAgentAttributes[] regimes = { EliteAttrs(), PoorAttrs() };

            for (int i = 0; i < regimes.Length; i++)
            {
                var ball = new RecordingBallSystem();
                GoalkeeperMechanics gk = new GoalkeeperMechanics(ball, new ZeroRng());

                float quality = DriveContact(gk, regimes[i]);

                Assert.That(quality, Is.GreaterThan(0f), "regime " + i + " produced no contact");
                Assert.AreEqual(1, ball.ParkBallCount + ball.ApplyKickCount,
                    "regime " + i + ": exactly one ball-side action per contact (park=" +
                    ball.ParkBallCount + " kick=" + ball.ApplyKickCount + ")");
            }
        }

        // ── Fixture ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drives the orchestrator until the keeper's hand envelope meets the ball, and returns the
        /// resolved handlingQualityScalar. The ball sits ~0.45 m off GK0 — inside every reach
        /// envelope (base 1.35 m) — and CLOSES on the goal plane so the §3.3.6 commit gate opens
        /// (ERR-011-007: a non-closing ball is correctly held). Physics is never integrated, so the
        /// geometry is constant across the run.
        /// </summary>
        private static float DriveContact(GoalkeeperMechanics gk, GoalkeeperAgentAttributes attrs)
        {
            var agents = new AgentState[AgentCount];
            agents[Gk0] = AgentState.CreateAtPosition(new Vector2(2f, 34f), Vector2.right);
            agents[Gk1] = AgentState.CreateAtPosition(new Vector2(103f, 34f), Vector2.left);

            BallState ball = BallState.CreateAtPosition(new Vector3(2.4f, 34.2f, 0.30f));
            ball.Velocity = new Vector3(-12f, 0f, 0f);

            float frameMs = GoalkeeperConstants.FrameMs;
            int framesPerTick = GoalkeeperConstants.FramesPerTacticalTick;

            for (int t = 0; t < 12; t++)
            {
                gk.TacticalTick(t, agents, ball, GkAgentIds);

                if (t == 2)
                {
                    gk.OnThreatArmed(Gk0, t * framesPerTick * frameMs, 12f, attrs);
                }

                if (t == 3)
                {
                    gk.CommitSaveIntent(Gk0, Intent(t), attrs);
                }

                for (int f = 0; f < framesPerTick; f++)
                {
                    int frame = t * framesPerTick + f;
                    gk.Update(frame, frame * frameMs, agents, ball, GkAgentIds);
                }

                float q = gk.CaptureState().ContactStates[Gk0].HandlingQualityScalar;
                if (gk.CaptureState().ContactStates[Gk0].ActualContactFrame >= 0)
                {
                    return q;
                }
            }

            return 0f;
        }

        private static SaveIntent Intent(int committedTick) => new SaveIntent
        {
            TargetHand = HandEnum.Right,
            ClutchFirmness = 0.6f,
            DeflectionTarget = null,
            AttemptCommittedTick = committedTick
        };

        private static GoalkeeperAgentAttributes EliteAttrs() => Attrs(20f);

        private static GoalkeeperAgentAttributes PoorAttrs() => Attrs(1f);

        private static GoalkeeperAgentAttributes Attrs(float v) => new GoalkeeperAgentAttributes
        {
            Reflexes = v,
            Handling = v,
            Composure = v,
            Strength = v,
            Aerial = v,
            Balance = v,
            OneVsOne = v,
            Pace = v,
            Throwing = v,
            Kicking = v,
            Fatigue = 0.0f,
            TeamId = 0
        };

        /// <summary>Counts the three ball-side effects §3.5.2 can produce.</summary>
        private sealed class RecordingBallSystem : IGoalkeeperBallSystem
        {
            public int ApplyKickCount;
            public int SetPossessorCount;
            public int ParkBallCount;

            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs)
                => ApplyKickCount++;

            public void SetPossessor(int agentId) => SetPossessorCount++;

            public void ParkBall() => ParkBallCount++;

            public int GetBallPossessorId() => -1;
        }

        private sealed class ZeroRng : IGoalkeeperRngService
        {
            public float NextFloat(int drawSiteId, uint domainTag) => 0.5f;
            public float NextGaussian(int drawSiteId, uint domainTag) => 0.0f;
        }

        private static string Inv(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-03 | —      | Initial. ERR-011-008 locks: a claim parks the ball and does not    |
// |         |            |        | kick it; a non-claim kicks and does not park; every contact        |
// |         |            |        | resolves to exactly one ball-side action (park XOR kick).          |
#endregion
