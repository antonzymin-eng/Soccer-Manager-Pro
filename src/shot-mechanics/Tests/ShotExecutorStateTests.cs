// File:     src/shot-mechanics/Tests/ShotExecutorStateTests.cs
// Created:  2026-06-19
// Modified: 2026-06-19
// Author:   —
// Spec:     Shot Mechanics #6 §3.9; Match Engine design note §2.6 (Phase C step C0); Code Standards #20
// Purpose:  Locks the Phase C C0 ShotExecutor snapshot seam: (1) a ShotExecutorState survives a
//           CanonicalSerializer write/read round-trip byte-for-byte (the serialization the match-engine
//           snapshot layer performs at C5), and (2) CaptureState → RestoreState → CaptureState is the
//           identity on the executor's cross-tick fields.

using System.Reflection;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.ShotMechanics.Tests
{
    [TestFixture]
    public sealed class ShotExecutorStateTests
    {
        private static ShotExecutorState MakePopulatedState()
        {
            var request = new ShotRequest
            {
                AgentId         = 9,
                PowerIntent     = 0.82f,
                ContactZone     = ContactZone.BelowCentre,
                SpinIntent      = 0.45f,
                PlacementTarget = new Vector2(0.7f, 0.9f),
                IsWeakFoot      = true,
                DistanceToGoal  = 19.5f,
                TeamId          = 0,
                FrameNumber     = 5123
            };

            var bodyMechanics = new BodyMechanicsResult
            {
                Score                  = 0.73f,
                ContactQualityModifier = 1.12f,
                StumbleTriggered       = false
            };

            var lastResult = new ShotResult
            {
                Outcome             = ShotOutcome.Completed,
                FinalVelocity       = new Vector3(20.0f, -4.0f, 7.5f),
                FinalSpin           = new Vector3(0.0f, 0.0f, 30.0f),
                IntendedDirection   = new Vector3(0.9f, -0.1f, 0.0f),
                FinalDirection      = new Vector3(0.88f, -0.12f, 0.0f),
                ErrorOffset         = new Vector2(0.05f, -0.02f),
                BodyMechanicsScore  = 0.73f,
                PowerPenaltyApplied = 1.34f,
                KickSpeed           = 22.5f,
                LaunchAngleDeg      = 11.0f,
                StumbleTriggered    = false,
                ContactFrame        = 5130
            };

            return new ShotExecutorState(
                state: 2, // Contact
                request: request,
                kickSpeed: 22.5f,
                launchAngleDeg: 11.0f,
                spinVector: new Vector3(0.0f, 0.0f, 30.0f),
                intendedAimDirection: new Vector3(0.9f, -0.1f, 0.0f),
                bodyMechanics: bodyMechanics,
                weakFootErrorMultiplier: 1.25f,
                windupFrames: 14,
                cachedAgentPosition: new Vector3(85.5f, 34.0f, 0.0f),
                cachedFinishing: 16.0f,
                cachedLongShots: 12.0f,
                cachedComposure: 13.0f,
                cachedFatigue: 0.4f,
                windupFramesRemaining: 0,
                followThroughFramesRemaining: 6,
                lastResult: lastResult);
        }

        private static void Serialize(byte[] buf, ref int o, in ShotExecutorState s)
        {
            CanonicalSerializer.WriteI32(buf, ref o, s.State);

            CanonicalSerializer.WriteI32(buf, ref o, s.Request.AgentId);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.PowerIntent);
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.Request.ContactZone);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.SpinIntent);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.PlacementTarget.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.PlacementTarget.y);
            CanonicalSerializer.WriteBool(buf, ref o, s.Request.IsWeakFoot);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.DistanceToGoal);
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.TeamId);
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.FrameNumber);

            CanonicalSerializer.WriteF32(buf, ref o, s.KickSpeed);
            CanonicalSerializer.WriteF32(buf, ref o, s.LaunchAngleDeg);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.IntendedAimDirection.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.IntendedAimDirection.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.IntendedAimDirection.z);

            CanonicalSerializer.WriteF32(buf, ref o, s.BodyMechanics.Score);
            CanonicalSerializer.WriteF32(buf, ref o, s.BodyMechanics.ContactQualityModifier);
            CanonicalSerializer.WriteBool(buf, ref o, s.BodyMechanics.StumbleTriggered);

            CanonicalSerializer.WriteF32(buf, ref o, s.WeakFootErrorMultiplier);
            CanonicalSerializer.WriteI32(buf, ref o, s.WindupFrames);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedAgentPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedAgentPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedAgentPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedFinishing);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedLongShots);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedComposure);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedFatigue);
            CanonicalSerializer.WriteI32(buf, ref o, s.WindupFramesRemaining);
            CanonicalSerializer.WriteI32(buf, ref o, s.FollowThroughFramesRemaining);

            CanonicalSerializer.WriteI32(buf, ref o, (int)s.LastResult.Outcome);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.IntendedDirection.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.IntendedDirection.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.IntendedDirection.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalDirection.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalDirection.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalDirection.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.ErrorOffset.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.ErrorOffset.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.BodyMechanicsScore);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.PowerPenaltyApplied);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.KickSpeed);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.LaunchAngleDeg);
            CanonicalSerializer.WriteBool(buf, ref o, s.LastResult.StumbleTriggered);
            CanonicalSerializer.WriteI32(buf, ref o, s.LastResult.ContactFrame);
        }

        private static ShotExecutorState Deserialize(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);

            var request = new ShotRequest
            {
                AgentId     = CanonicalSerializer.ReadI32(buf, ref o),
                PowerIntent = CanonicalSerializer.ReadF32(buf, ref o),
                ContactZone = (ContactZone)CanonicalSerializer.ReadI32(buf, ref o),
                SpinIntent  = CanonicalSerializer.ReadF32(buf, ref o),
                PlacementTarget = new Vector2(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                IsWeakFoot     = CanonicalSerializer.ReadBool(buf, ref o),
                DistanceToGoal = CanonicalSerializer.ReadF32(buf, ref o),
                TeamId         = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber    = CanonicalSerializer.ReadI32(buf, ref o)
            };

            float kickSpeed      = CanonicalSerializer.ReadF32(buf, ref o);
            float launchAngleDeg = CanonicalSerializer.ReadF32(buf, ref o);
            var spinVector = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));
            var intendedAimDirection = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));

            var bodyMechanics = new BodyMechanicsResult
            {
                Score                  = CanonicalSerializer.ReadF32(buf, ref o),
                ContactQualityModifier = CanonicalSerializer.ReadF32(buf, ref o),
                StumbleTriggered       = CanonicalSerializer.ReadBool(buf, ref o)
            };

            float weakFootErrorMultiplier = CanonicalSerializer.ReadF32(buf, ref o);
            int windupFrames              = CanonicalSerializer.ReadI32(buf, ref o);
            var cachedAgentPosition = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));
            float cachedFinishing = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedLongShots = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedComposure = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedFatigue   = CanonicalSerializer.ReadF32(buf, ref o);
            int windupRemaining        = CanonicalSerializer.ReadI32(buf, ref o);
            int followThroughRemaining = CanonicalSerializer.ReadI32(buf, ref o);

            var lastResult = new ShotResult
            {
                Outcome       = (ShotOutcome)CanonicalSerializer.ReadI32(buf, ref o),
                FinalVelocity = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                FinalSpin = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDirection = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                FinalDirection = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                ErrorOffset = new Vector2(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                BodyMechanicsScore  = CanonicalSerializer.ReadF32(buf, ref o),
                PowerPenaltyApplied = CanonicalSerializer.ReadF32(buf, ref o),
                KickSpeed           = CanonicalSerializer.ReadF32(buf, ref o),
                LaunchAngleDeg      = CanonicalSerializer.ReadF32(buf, ref o),
                StumbleTriggered    = CanonicalSerializer.ReadBool(buf, ref o),
                ContactFrame        = CanonicalSerializer.ReadI32(buf, ref o)
            };

            return new ShotExecutorState(
                state, in request,
                kickSpeed, launchAngleDeg, spinVector, intendedAimDirection,
                in bodyMechanics, weakFootErrorMultiplier, windupFrames, cachedAgentPosition,
                cachedFinishing, cachedLongShots, cachedComposure, cachedFatigue,
                windupRemaining, followThroughRemaining, in lastResult);
        }

        private static void AssertStateEquals(in ShotExecutorState expected, in ShotExecutorState actual)
        {
            Assert.AreEqual(expected.State, actual.State, "State");

            Assert.AreEqual(expected.Request.AgentId, actual.Request.AgentId, "Request.AgentId");
            Assert.AreEqual(expected.Request.PowerIntent, actual.Request.PowerIntent, "Request.PowerIntent");
            Assert.AreEqual(expected.Request.ContactZone, actual.Request.ContactZone, "Request.ContactZone");
            Assert.AreEqual(expected.Request.SpinIntent, actual.Request.SpinIntent, "Request.SpinIntent");
            Assert.AreEqual(expected.Request.PlacementTarget, actual.Request.PlacementTarget, "Request.PlacementTarget");
            Assert.AreEqual(expected.Request.IsWeakFoot, actual.Request.IsWeakFoot, "Request.IsWeakFoot");
            Assert.AreEqual(expected.Request.DistanceToGoal, actual.Request.DistanceToGoal, "Request.DistanceToGoal");
            Assert.AreEqual(expected.Request.TeamId, actual.Request.TeamId, "Request.TeamId");
            Assert.AreEqual(expected.Request.FrameNumber, actual.Request.FrameNumber, "Request.FrameNumber");

            Assert.AreEqual(expected.KickSpeed, actual.KickSpeed, "KickSpeed");
            Assert.AreEqual(expected.LaunchAngleDeg, actual.LaunchAngleDeg, "LaunchAngleDeg");
            Assert.AreEqual(expected.SpinVector, actual.SpinVector, "SpinVector");
            Assert.AreEqual(expected.IntendedAimDirection, actual.IntendedAimDirection, "IntendedAimDirection");
            Assert.AreEqual(expected.BodyMechanics.Score, actual.BodyMechanics.Score, "BodyMechanics.Score");
            Assert.AreEqual(expected.BodyMechanics.ContactQualityModifier, actual.BodyMechanics.ContactQualityModifier, "BodyMechanics.ContactQualityModifier");
            Assert.AreEqual(expected.BodyMechanics.StumbleTriggered, actual.BodyMechanics.StumbleTriggered, "BodyMechanics.StumbleTriggered");
            Assert.AreEqual(expected.WeakFootErrorMultiplier, actual.WeakFootErrorMultiplier, "WeakFootErrorMultiplier");
            Assert.AreEqual(expected.WindupFrames, actual.WindupFrames, "WindupFrames");
            Assert.AreEqual(expected.CachedAgentPosition, actual.CachedAgentPosition, "CachedAgentPosition");
            Assert.AreEqual(expected.CachedFinishing, actual.CachedFinishing, "CachedFinishing");
            Assert.AreEqual(expected.CachedLongShots, actual.CachedLongShots, "CachedLongShots");
            Assert.AreEqual(expected.CachedComposure, actual.CachedComposure, "CachedComposure");
            Assert.AreEqual(expected.CachedFatigue, actual.CachedFatigue, "CachedFatigue");
            Assert.AreEqual(expected.WindupFramesRemaining, actual.WindupFramesRemaining, "WindupFramesRemaining");
            Assert.AreEqual(expected.FollowThroughFramesRemaining, actual.FollowThroughFramesRemaining, "FollowThroughFramesRemaining");

            Assert.AreEqual(expected.LastResult.Outcome, actual.LastResult.Outcome, "LastResult.Outcome");
            Assert.AreEqual(expected.LastResult.FinalVelocity, actual.LastResult.FinalVelocity, "LastResult.FinalVelocity");
            Assert.AreEqual(expected.LastResult.FinalSpin, actual.LastResult.FinalSpin, "LastResult.FinalSpin");
            Assert.AreEqual(expected.LastResult.IntendedDirection, actual.LastResult.IntendedDirection, "LastResult.IntendedDirection");
            Assert.AreEqual(expected.LastResult.FinalDirection, actual.LastResult.FinalDirection, "LastResult.FinalDirection");
            Assert.AreEqual(expected.LastResult.ErrorOffset, actual.LastResult.ErrorOffset, "LastResult.ErrorOffset");
            Assert.AreEqual(expected.LastResult.BodyMechanicsScore, actual.LastResult.BodyMechanicsScore, "LastResult.BodyMechanicsScore");
            Assert.AreEqual(expected.LastResult.PowerPenaltyApplied, actual.LastResult.PowerPenaltyApplied, "LastResult.PowerPenaltyApplied");
            Assert.AreEqual(expected.LastResult.KickSpeed, actual.LastResult.KickSpeed, "LastResult.KickSpeed");
            Assert.AreEqual(expected.LastResult.LaunchAngleDeg, actual.LastResult.LaunchAngleDeg, "LastResult.LaunchAngleDeg");
            Assert.AreEqual(expected.LastResult.StumbleTriggered, actual.LastResult.StumbleTriggered, "LastResult.StumbleTriggered");
            Assert.AreEqual(expected.LastResult.ContactFrame, actual.LastResult.ContactFrame, "LastResult.ContactFrame");
        }

        [Test]
        public void ShotExecutorState_SurvivesCanonicalSerializerRoundTrip()
        {
            ShotExecutorState original = MakePopulatedState();

            byte[] buf = new byte[512];
            int writeOffset = 0;
            Serialize(buf, ref writeOffset, in original);

            int readOffset = 0;
            ShotExecutorState reconstructed = Deserialize(buf, ref readOffset);

            Assert.AreEqual(writeOffset, readOffset, "read offset must consume exactly the written bytes");
            AssertStateEquals(in original, in reconstructed);
        }

        [Test]
        public void ShotExecutor_CaptureRestoreCapture_IsIdentity()
        {
            // Deps null: CaptureState/RestoreState never touch them. The ctor resolves the velocity
            // calculator to its singleton default, which the seam does not exercise.
            var executor = new ShotExecutor(null, null, null);
            ShotExecutorState seeded = MakePopulatedState();

            executor.RestoreState(in seeded);
            ShotExecutorState recaptured = executor.CaptureState();

            AssertStateEquals(in seeded, in recaptured);
        }

        // Stubs let a real Execute() populate the in-flight fields from genuine computation rather
        // than a hand-built DTO. The lifecycle stays in WINDUP so no CONTACT publish is reached
        // (publish needs a booted EventBus registry); full CONTACT-through-publish behavioural
        // parity is exercised at Phase C C3's MatchEngineResolveTests where Resolve boots the bus.
        private sealed class StubBall : IShotBallSystem
        {
            public bool IsBallPossessedBy(int agentId) => true;
            public void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime) { }
        }

        private sealed class StubAgent : IShotAgentQuery
        {
            public ShotAgentAttributes GetAttributes(int agentId) => new ShotAgentAttributes
            {
                Finishing = 16, LongShots = 12, Composure = 13, KickPower = 15,
                Technique = 11, WeakFootRating = 3, Fatigue = 0.2f
            };
            public ShotAgentState GetState(int agentId) => new ShotAgentState
            {
                Position        = new Vector3(85f, 34f, 0f),
                Velocity        = Vector3.zero,
                FacingDirection = new Vector2(-1f, 0f),
                CurrentState    = AgentMovementState.IDLE
            };
        }

        private sealed class StubCollision : IShotCollisionQuery
        {
            public bool GetAndClearTackleFlag(int agentId) => false;
            public float ComputePressureScalar(Vector3 shooterPosition, int shooterTeamId) => 0.3f;
        }

        [Test]
        public void ShotExecutor_RealExecuteThenCaptureRestore_PreservesComputedState()
        {
            var ball = new StubBall();
            var agent = new StubAgent();
            var collision = new StubCollision();

            var request = new ShotRequest
            {
                AgentId         = 9,
                PowerIntent     = 0.8f,
                ContactZone     = ContactZone.Centre,
                SpinIntent      = 0.3f,
                PlacementTarget = new Vector2(0.6f, 0.5f),
                IsWeakFoot      = false,
                DistanceToGoal  = 18f,
                TeamId          = 0,
                FrameNumber     = 100
            };

            var executorA = new ShotExecutor(ball, agent, collision);
            ShotResult initiated = executorA.Execute(in request);
            Assert.AreEqual(ShotOutcome.Initiated, initiated.Outcome, "Execute should begin windup");
            Assert.IsFalse(executorA.IsIdle, "executor should be mid-windup");

            ShotExecutorState captured = executorA.CaptureState();
            var executorB = new ShotExecutor(null, null, null);
            executorB.RestoreState(in captured);
            ShotExecutorState recaptured = executorB.CaptureState();

            AssertStateEquals(in captured, in recaptured);
            Assert.Greater(captured.KickSpeed, 0f, "Execute must have computed a positive kick speed");
        }

        [Test]
        public void ShotExecutor_InstanceFieldCount_MatchesSerializedSet()
        {
            // Mechanical coupling guard, the analogue of the B0 OscillationGuard BufferSize assert:
            // adding cross-tick in-flight state without extending ShotExecutorState / CaptureState /
            // RestoreState would silently drop it from the snapshot and diverge replay (§2.6 trap).
            // This count (4 injected deps + 17 captured in-flight fields) trips first.
            int fieldCount = typeof(ShotExecutor)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Length;

            Assert.AreEqual(21, fieldCount,
                "ShotExecutor instance field count changed. If you added cross-tick in-flight state, " +
                "extend ShotExecutorState + CaptureState + RestoreState, then update this count.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-19 | —      | Initial implementation — Phase C C0 ShotExecutor snapshot-seam |
// |         |            |        | round-trip + Capture/Restore identity locks.                  |
// | 1.1     | 2026-06-19 | —      | C0 AR-1: added M-1 real-Execute capture/restore preservation  |
// |         |            |        | test (genuine computed state, stays in WINDUP) + M-2 reflection|
// |         |            |        | field-count lock (silent-omission guard, B0 BufferSize analogue|
// |         |            |        | ). Added stub IShot* implementations.                         |
#endregion
