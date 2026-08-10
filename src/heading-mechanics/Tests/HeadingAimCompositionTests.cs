// File:     src/heading-mechanics/Tests/HeadingAimCompositionTests.cs
// Created:  2026-08-09
// Modified: 2026-08-09
// Author:   —
// Spec:     Heading Mechanics #10 §3.5.1 (ERR-010-002), FR-HE-036, §5.2, Code Standards #20
// Purpose:  Drives a header all the way to CONTACT through the real 60 Hz orchestrator, so the
//           §3.5.1 aim is locked by the production composition rather than by a helper that
//           re-assembles the same three calls inside the test.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.HeadingMechanics.Tests
{
    /// <summary>
    /// AR pass 2, M-1. Every other §3.5.1 lock lives in <c>HeadingAimTests</c>, which composes
    /// <c>ComputeAimDirection</c> → <c>ComputeAimNormal</c> → <c>ComputeAchievedNormal</c> → the §3.5
    /// reflection <b>inside the test</b> (its own <c>OutgoingDirectionFor</c> helper). That is
    /// deliberate there — it keeps those locks from verifying production geometry through production
    /// geometry — but it leaves a hole the reviewer named precisely: nothing drove
    /// <see cref="HeadingMechanics.Update"/> to an actual contact, so
    /// <c>ResolveContactGeometry</c> could have been unwired (forcing <c>aimNormal = Vector3.zero</c>,
    /// which reverts the entire ERR-010-002 landing) and every suite in the tree would still be green.
    ///
    /// <para>A lock whose expectation is computed through the same walk the production code uses is
    /// this repository's most-repeated defect — filed against <c>LineupSelector.CanSelect</c>, the
    /// cursor-vs-clock pair, and the T2 appearance-recording walk. This fixture closes it from the
    /// other side: it asserts on the velocity the orchestrator actually hands to Ball Physics.</para>
    ///
    /// <para>The existing <c>HeadingScenarios</c> orchestrator run is deliberately publish-free — the
    /// ball is parked out of reach so no contact is ever made. This is the first test anywhere that
    /// takes a header through to <c>ApplyKick</c>.</para>
    /// </summary>
    [TestFixture]
    public sealed class HeadingAimCompositionTests
    {
        private const int HeaderAgentId = 3;
        private const int StartFrame = 0;

        /// <summary>
        /// A successful contact publishes <c>HeaderExecutedEvent</c>, so #10's ordinals must be
        /// registered and the bus reset per test. <c>Initialize</c> is idempotent (FR-EVT-020); the
        /// Physics phase declaration reproduces the production context (#10's Update runs inside
        /// <c>MatchEngine.RunPhysicsPhase</c> → <c>DriveGkHeadingPhysics</c>). This is the
        /// <c>GoalkeeperRushTests</c> pattern — and the reason no heading fixture needed it before is
        /// that no heading test had ever reached a contact.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            EventBusRegistrar.Initialize();
            EventBus.ResetForNewMatch();
            EventBus.BeginPhase(PhaseId.Physics);
        }

        [TearDown]
        public void TearDown() => EventBus.ResetForNewMatch();

        /// <summary>
        /// Captures the outgoing velocity the orchestrator commits to Ball Physics, and stands in for
        /// the ball integrator the orchestrator does NOT own — #10 is handed a ball each frame, it
        /// never advances one. The first cut of this fixture passed the same <c>BallState</c> on every
        /// call and no header ever reached contact, which is why
        /// <see cref="Composition_DrivesAHeaderThroughToContact"/> exists as its own precondition:
        /// a setup that stops reaching contact must fail loudly, not pass vacuously.
        /// </summary>
        private sealed class CapturingBallSystem : IHeadingBallSystem
        {
            public BallState Current;

            public int ApplyKickCount;
            public Vector3 LastVelocity;

            public CapturingBallSystem(BallState ball)
            {
                Current = ball;
            }

            public BallState GetBallState(float matchTime) => Current;

            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTime)
            {
                ApplyKickCount++;
                LastVelocity = velocity;
            }
        }

        /// <summary>
        /// The same gravity-only model §3.2's own <c>PredictBallPosition</c> uses, restated here rather
        /// than called: this fixture must not advance the ball through the very prediction the
        /// orchestrator is being tested against.
        /// </summary>
        private static BallState BallAt(in BallState start, int framesElapsed)
        {
            float t = framesElapsed * HeadingMechanicsConstants.FrameS;
            float g = HeadingMechanicsConstants.GravityMps2;

            BallState ball = start;
            ball.Position = new Vector3(
                start.Position.x + start.Velocity.x * t,
                start.Position.y + start.Velocity.y * t,
                start.Position.z + start.Velocity.z * t
                    - HeadingMechanicsConstants.KINEMATIC_HALF_COEFF * g * t * t);
            ball.Velocity = new Vector3(
                start.Velocity.x,
                start.Velocity.y,
                start.Velocity.z - g * t);
            return ball;
        }

        /// <summary>Constant returns; §3.5.1 is draw-free, so the aim must not depend on these.</summary>
        private sealed class FixedRng : IHeadingRngService
        {
            public float NextFloat(int drawSiteId) => 0.5f;
            public float NextGaussian(int drawSiteId) => 0.0f;
        }

        private static HeadingAgentAttributes Attrs()
        {
            return new HeadingAgentAttributes
            {
                Heading = 20, Strength = 14, Balance = 14, Fatigue = 0.0f, TeamId = 0
            };
        }

        private static AgentState[] BuildAgents()
        {
            var agents = new AgentState[HeadingMechanicsConstants.MaxAgents];
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i] = AgentState.CreateAtPosition(new Vector2(52f, 34f), Vector2.right);
            }
            agents[HeaderAgentId].CurrentState = AgentMovementState.JOGGING;
            return agents;
        }

        /// <summary>
        /// Builds a ball whose gravity-only trajectory puts it exactly on the head point at the apex
        /// frame — the one frame §3.2's staleness window accepts as on-time. Solved from the jump
        /// kinematics rather than hardcoded, so a re-tuned jump does not silently stop this test
        /// reaching contact (it would fail loud on the ApplyKick assertion instead).
        /// </summary>
        private static BallState BuildArrivingBall(Vector3 incomingVelocity, out Vector3 contactPoint)
        {
            float reach = HeadingJumpKinematics.ComputeJumpReach(Attrs());
            int apex = HeadingJumpKinematics.ComputeApexFrame(StartFrame);
            float headZ = HeadingJumpKinematics.ComputeHeadZ(StartFrame, reach, apex);
            float dt = (apex - StartFrame) * HeadingMechanicsConstants.FrameS;

            // Deliberately NOT the head centre. A ball arriving dead-centre gives
            // delta = ballPos − headCentre = 0, so ResolveContactGeometry's geometric normal is zero,
            // ComputeAchievedNormal's degenerate-input guard returns that zero unchanged, and the aim
            // is silently discarded — two targets 60 m apart then produce byte-identical outgoing
            // velocities. The first cut of this fixture did exactly that and read 0.0° of separation.
            // Real contacts land somewhere on the head surface; this one lands on the approach side,
            // well inside HeadContactVolumeRadiusM.
            Vector3 approachOffset = -incomingVelocity.normalized
                                   * (HeadingMechanicsConstants.HeadContactVolumeRadiusM * 0.6f);

            contactPoint = new Vector3(52f, 34f, headZ) + approachOffset;

            float gravityDrop = HeadingMechanicsConstants.KINEMATIC_HALF_COEFF
                              * HeadingMechanicsConstants.GravityMps2 * dt * dt;

            Vector3 start = contactPoint - incomingVelocity * dt + new Vector3(0.0f, 0.0f, gravityDrop);

            BallState ball = BallState.CreateAtPosition(start);
            ball.Velocity = incomingVelocity;
            return ball;
        }

        private static Vector3 RunToContact(Vector3 target, Vector3 incomingVelocity, out int kicks)
        {
            BallState start = BuildArrivingBall(incomingVelocity, out _);
            var ballSystem = new CapturingBallSystem(start);
            var heading = new HeadingMechanics(ballSystem, new FixedRng());

            AgentState[] agents = BuildAgents();

            var intent = new HeaderIntent
            {
                PowerIntent = 0.7f,
                ContactPointIntent = Vector2.zero,
                TargetIntent = target,
                AttemptCommittedTick = 0,
                SetPieceContext = SetPieceContext.OpenPlay
            };

            heading.CommitIntent(HeaderAgentId, intent, Attrs(), start, StartFrame);

            int landing = HeadingJumpKinematics.ComputeLandingFrame(StartFrame);
            for (int frame = StartFrame; frame <= landing; frame++)
            {
                BallState now = BallAt(in start, frame - StartFrame);
                ballSystem.Current = now;
                heading.Update(agents, now, frame, frame * HeadingMechanicsConstants.FrameMs);
            }

            kicks = ballSystem.ApplyKickCount;
            return ballSystem.LastVelocity;
        }

        /// <summary>
        /// Precondition for everything below, asserted separately so a setup that stops reaching
        /// contact reports as a setup failure rather than as a silently vacuous aim comparison.
        /// </summary>
        [Test]
        public void Composition_DrivesAHeaderThroughToContact()
        {
            Vector3 outgoing = RunToContact(
                new Vector3(90f, 34f, 0.0f), new Vector3(9.0f, 0.0f, -3.0f), out int kicks);

            Assert.That(kicks, Is.EqualTo(1), "the orchestrator must reach exactly one header contact");
            Assert.That(outgoing.magnitude, Is.GreaterThan(0.0f));
            Assert.That(float.IsFinite(outgoing.x) && float.IsFinite(outgoing.y) && float.IsFinite(outgoing.z));
        }

        /// <summary>
        /// THE lock AR pass 2 asked for. Identical agent, identical ball, identical attributes,
        /// identical frame sequence — only <c>TargetIntent</c> differs, and it differs across the
        /// pitch. The outgoing velocity the orchestrator hands Ball Physics must differ materially.
        ///
        /// <para>Unwiring §3.5.1 anywhere in the production path — forcing <c>aimNormal</c> to zero in
        /// <c>ResolveContactGeometry</c>, reverting <c>ComputeAchievedNormal</c> to the geometric
        /// normal, or restoring the pre-ERR-010-002 mirror — makes both runs return the IDENTICAL
        /// vector and fails this test. No helper in this fixture re-derives the aim; the expectation
        /// is a difference, not a value, so nothing here can agree with production by construction.</para>
        /// </summary>
        [Test]
        public void TwoTargets_ThroughTheRealOrchestrator_ProduceDifferentOutgoingVelocities()
        {
            Vector3 incoming = new Vector3(9.0f, 0.0f, -3.0f);

            Vector3 towardLeftTouchline = RunToContact(
                new Vector3(90f, 4f, 0.0f), incoming, out int leftKicks);
            Vector3 towardRightTouchline = RunToContact(
                new Vector3(90f, 64f, 0.0f), incoming, out int rightKicks);

            Assert.That(leftKicks, Is.EqualTo(1), "left-aimed run must reach contact");
            Assert.That(rightKicks, Is.EqualTo(1), "right-aimed run must reach contact");

            Assert.That(Vector3.Angle(towardLeftTouchline, towardRightTouchline), Is.GreaterThan(5.0f),
                "TargetIntent must reach the outgoing velocity through the production composition; "
                + "a mirror-only model returns the same vector for both targets");

            Assert.That(towardLeftTouchline.y, Is.LessThan(towardRightTouchline.y),
                "aiming at the lower touchline must send the ball further toward it than aiming at the upper one");
        }

        /// <summary>
        /// The vertical half of the same claim, through the orchestrator: a DESCENDING ball aimed at a
        /// distant ground target must leave the head RISING. Pre-ERR-010-002 the reflection normal was
        /// pinned horizontal by the 2-D head-local round trip, so <c>reflected.z == v̂_in.z</c> and a
        /// dropping ball was headed further down — no header could lift the ball. That defect lived in
        /// <c>HeadingMechanics.Update</c> pass 2, which is code this fixture executes and
        /// <c>HeadingAimTests</c> does not.
        /// </summary>
        [Test]
        public void DescendingBall_ThroughTheRealOrchestrator_LeavesTheHeadRising()
        {
            Vector3 descending = new Vector3(6.0f, 0.0f, -8.0f);

            Vector3 outgoing = RunToContact(
                new Vector3(95f, 34f, 0.0f), descending, out int kicks);

            Assert.That(kicks, Is.EqualTo(1));
            Assert.That(outgoing.z, Is.GreaterThan(0.0f),
                "a clearance aimed long must come off the head going up, not follow the ball down");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                             |
// | 1.0     | 2026-08-09 | —      | Initial. AR pass 2 M-1: every §3.5.1 lock composed the three aim  |
// |         |            |        |   steps inside the test helper, so forcing aimNormal = zero in    |
// |         |            |        |   ResolveContactGeometry — which reverts the whole ERR-010-002    |
// |         |            |        |   landing — left every suite in the tree green. This fixture      |
// |         |            |        |   drives HeadingMechanics.Update to an actual contact (the first  |
// |         |            |        |   test anywhere to reach ApplyKick; HeadingScenarios' run is      |
// |         |            |        |   deliberately publish-free) and asserts on the velocity handed   |
// |         |            |        |   to Ball Physics: two targets, one geometry, materially          |
// |         |            |        |   different outgoing vectors, plus the descending-ball lift.      |
#endregion
