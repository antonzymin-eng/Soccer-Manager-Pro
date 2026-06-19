// File:     src/pass-mechanics/Tests/PassExecutorStateTests.cs
// Created:  2026-06-19
// Modified: 2026-06-19
// Author:   —
// Spec:     Pass Mechanics #5 §3.8; Match Engine design note §2.6 (Phase C step C0); Code Standards #20
// Purpose:  Locks the Phase C C0 PassExecutor snapshot seam: (1) a PassExecutorState survives a
//           CanonicalSerializer write/read round-trip byte-for-byte (the serialization the match-engine
//           snapshot layer performs at C5), and (2) CaptureState → RestoreState → CaptureState is the
//           identity on the executor's cross-tick fields (the seam is lossless, incl. the recomputed
//           internal PhysicalProfile path).

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PassMechanics.Tests
{
    [TestFixture]
    public sealed class PassExecutorStateTests
    {
        // Distinct, finite, non-default value in every field so a dropped or misordered field
        // surfaces as an inequality. (Avoid -0.0 / NaN — WriteF32 Tier A round-trips raw bits but
        // -0.0 is normalized; finite distinct values keep the equality assertions exact.)
        private static PassExecutorState MakePopulatedState()
        {
            var request = new PassRequest
            {
                AgentId          = 7,
                PassType         = PassType.Cross,
                CrossSubType     = CrossSubType.Whipped,
                TargetAgentId    = 13,
                TargetPosition   = new Vector3(40.5f, 22.25f, 0.0f),
                IntendedDistance = 18.75f,
                Urgency          = 0.6f,
                IsWeakFoot       = true,
                TeamId           = 1,
                FrameNumber      = 4321
            };

            var lastResult = new PassResult
            {
                Outcome          = PassOutcome.Completed,
                FinalVelocity    = new Vector3(11.0f, -3.5f, 6.25f),
                FinalSpin        = new Vector3(0.0f, 0.0f, 12.5f),
                AimPoint         = new Vector3(41.0f, 23.0f, 0.0f),
                ErrorAngleDeg    = 2.75f,
                LeadDistance     = 1.5f,
                PassType         = PassType.Cross,
                ContactFrame     = 4327,
                ContactMatchTime = 72.05f
            };

            return new PassExecutorState(
                state: 1, // Windup
                request: request,
                effectiveSubType: CrossSubType.Whipped,
                kickSpeed: 14.5f,
                launchAngleDeg: 8.0f,
                spinVector: new Vector3(0.0f, 0.0f, 15.0f),
                baseKickDirection: new Vector3(0.8f, 0.6f, 0.0f),
                aimPoint: new Vector3(41.0f, 23.0f, 0.0f),
                leadDistance: 1.5f,
                cachedPassing: 14.0f,
                cachedFatigue: 0.3f,
                cachedBodyAngleDeg: 17.5f,
                cachedIsWeakFoot: true,
                cachedWeakFootRating: 4,
                windupFramesRemaining: 9,
                followThroughFramesRemaining: 5,
                lastResult: lastResult);
        }

        // Mirrors the field order the match-engine snapshot layer will use at C5.
        private static void Serialize(byte[] buf, ref int o, in PassExecutorState s)
        {
            CanonicalSerializer.WriteI32(buf, ref o, s.State);

            // Request
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.AgentId);
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.Request.PassType);
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.Request.CrossSubType);
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.TargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.TargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.TargetPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.TargetPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.IntendedDistance);
            CanonicalSerializer.WriteF32(buf, ref o, s.Request.Urgency);
            CanonicalSerializer.WriteBool(buf, ref o, s.Request.IsWeakFoot);
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.TeamId);
            CanonicalSerializer.WriteI32(buf, ref o, s.Request.FrameNumber);

            CanonicalSerializer.WriteI32(buf, ref o, (int)s.EffectiveSubType);
            CanonicalSerializer.WriteF32(buf, ref o, s.KickSpeed);
            CanonicalSerializer.WriteF32(buf, ref o, s.LaunchAngleDeg);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.SpinVector.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.BaseKickDirection.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.BaseKickDirection.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.BaseKickDirection.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.AimPoint.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.AimPoint.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.AimPoint.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LeadDistance);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedPassing);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedFatigue);
            CanonicalSerializer.WriteF32(buf, ref o, s.CachedBodyAngleDeg);
            CanonicalSerializer.WriteBool(buf, ref o, s.CachedIsWeakFoot);
            CanonicalSerializer.WriteI32(buf, ref o, s.CachedWeakFootRating);
            CanonicalSerializer.WriteI32(buf, ref o, s.WindupFramesRemaining);
            CanonicalSerializer.WriteI32(buf, ref o, s.FollowThroughFramesRemaining);

            // LastResult
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.LastResult.Outcome);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalVelocity.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.FinalSpin.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.AimPoint.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.AimPoint.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.AimPoint.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.ErrorAngleDeg);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.LeadDistance);
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.LastResult.PassType);
            CanonicalSerializer.WriteI32(buf, ref o, s.LastResult.ContactFrame);
            CanonicalSerializer.WriteF32(buf, ref o, s.LastResult.ContactMatchTime);
        }

        private static PassExecutorState Deserialize(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);

            var request = new PassRequest
            {
                AgentId          = CanonicalSerializer.ReadI32(buf, ref o),
                PassType         = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                CrossSubType     = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o),
                TargetAgentId    = CanonicalSerializer.ReadI32(buf, ref o),
                TargetPosition   = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDistance = CanonicalSerializer.ReadF32(buf, ref o),
                Urgency          = CanonicalSerializer.ReadF32(buf, ref o),
                IsWeakFoot       = CanonicalSerializer.ReadBool(buf, ref o),
                TeamId           = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber      = CanonicalSerializer.ReadI32(buf, ref o)
            };

            var effectiveSubType = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o);
            float kickSpeed      = CanonicalSerializer.ReadF32(buf, ref o);
            float launchAngleDeg = CanonicalSerializer.ReadF32(buf, ref o);
            var spinVector = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));
            var baseKickDirection = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));
            var aimPoint = new Vector3(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));
            float leadDistance       = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedPassing      = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedFatigue      = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedBodyAngleDeg = CanonicalSerializer.ReadF32(buf, ref o);
            bool cachedIsWeakFoot    = CanonicalSerializer.ReadBool(buf, ref o);
            int cachedWeakFootRating = CanonicalSerializer.ReadI32(buf, ref o);
            int windupRemaining      = CanonicalSerializer.ReadI32(buf, ref o);
            int followThroughRemaining = CanonicalSerializer.ReadI32(buf, ref o);

            var lastResult = new PassResult
            {
                Outcome       = (PassOutcome)CanonicalSerializer.ReadI32(buf, ref o),
                FinalVelocity = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                FinalSpin = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                AimPoint = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                ErrorAngleDeg    = CanonicalSerializer.ReadF32(buf, ref o),
                LeadDistance     = CanonicalSerializer.ReadF32(buf, ref o),
                PassType         = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                ContactFrame     = CanonicalSerializer.ReadI32(buf, ref o),
                ContactMatchTime = CanonicalSerializer.ReadF32(buf, ref o)
            };

            return new PassExecutorState(
                state, in request, effectiveSubType,
                kickSpeed, launchAngleDeg, spinVector, baseKickDirection, aimPoint, leadDistance,
                cachedPassing, cachedFatigue, cachedBodyAngleDeg, cachedIsWeakFoot, cachedWeakFootRating,
                windupRemaining, followThroughRemaining, in lastResult);
        }

        private static void AssertStateEquals(in PassExecutorState expected, in PassExecutorState actual)
        {
            Assert.AreEqual(expected.State, actual.State, "State");

            Assert.AreEqual(expected.Request.AgentId, actual.Request.AgentId, "Request.AgentId");
            Assert.AreEqual(expected.Request.PassType, actual.Request.PassType, "Request.PassType");
            Assert.AreEqual(expected.Request.CrossSubType, actual.Request.CrossSubType, "Request.CrossSubType");
            Assert.AreEqual(expected.Request.TargetAgentId, actual.Request.TargetAgentId, "Request.TargetAgentId");
            Assert.AreEqual(expected.Request.TargetPosition, actual.Request.TargetPosition, "Request.TargetPosition");
            Assert.AreEqual(expected.Request.IntendedDistance, actual.Request.IntendedDistance, "Request.IntendedDistance");
            Assert.AreEqual(expected.Request.Urgency, actual.Request.Urgency, "Request.Urgency");
            Assert.AreEqual(expected.Request.IsWeakFoot, actual.Request.IsWeakFoot, "Request.IsWeakFoot");
            Assert.AreEqual(expected.Request.TeamId, actual.Request.TeamId, "Request.TeamId");
            Assert.AreEqual(expected.Request.FrameNumber, actual.Request.FrameNumber, "Request.FrameNumber");

            Assert.AreEqual(expected.EffectiveSubType, actual.EffectiveSubType, "EffectiveSubType");
            Assert.AreEqual(expected.KickSpeed, actual.KickSpeed, "KickSpeed");
            Assert.AreEqual(expected.LaunchAngleDeg, actual.LaunchAngleDeg, "LaunchAngleDeg");
            Assert.AreEqual(expected.SpinVector, actual.SpinVector, "SpinVector");
            Assert.AreEqual(expected.BaseKickDirection, actual.BaseKickDirection, "BaseKickDirection");
            Assert.AreEqual(expected.AimPoint, actual.AimPoint, "AimPoint");
            Assert.AreEqual(expected.LeadDistance, actual.LeadDistance, "LeadDistance");
            Assert.AreEqual(expected.CachedPassing, actual.CachedPassing, "CachedPassing");
            Assert.AreEqual(expected.CachedFatigue, actual.CachedFatigue, "CachedFatigue");
            Assert.AreEqual(expected.CachedBodyAngleDeg, actual.CachedBodyAngleDeg, "CachedBodyAngleDeg");
            Assert.AreEqual(expected.CachedIsWeakFoot, actual.CachedIsWeakFoot, "CachedIsWeakFoot");
            Assert.AreEqual(expected.CachedWeakFootRating, actual.CachedWeakFootRating, "CachedWeakFootRating");
            Assert.AreEqual(expected.WindupFramesRemaining, actual.WindupFramesRemaining, "WindupFramesRemaining");
            Assert.AreEqual(expected.FollowThroughFramesRemaining, actual.FollowThroughFramesRemaining, "FollowThroughFramesRemaining");

            Assert.AreEqual(expected.LastResult.Outcome, actual.LastResult.Outcome, "LastResult.Outcome");
            Assert.AreEqual(expected.LastResult.FinalVelocity, actual.LastResult.FinalVelocity, "LastResult.FinalVelocity");
            Assert.AreEqual(expected.LastResult.FinalSpin, actual.LastResult.FinalSpin, "LastResult.FinalSpin");
            Assert.AreEqual(expected.LastResult.AimPoint, actual.LastResult.AimPoint, "LastResult.AimPoint");
            Assert.AreEqual(expected.LastResult.ErrorAngleDeg, actual.LastResult.ErrorAngleDeg, "LastResult.ErrorAngleDeg");
            Assert.AreEqual(expected.LastResult.LeadDistance, actual.LastResult.LeadDistance, "LastResult.LeadDistance");
            Assert.AreEqual(expected.LastResult.PassType, actual.LastResult.PassType, "LastResult.PassType");
            Assert.AreEqual(expected.LastResult.ContactFrame, actual.LastResult.ContactFrame, "LastResult.ContactFrame");
            Assert.AreEqual(expected.LastResult.ContactMatchTime, actual.LastResult.ContactMatchTime, "LastResult.ContactMatchTime");
        }

        [Test]
        public void PassExecutorState_SurvivesCanonicalSerializerRoundTrip()
        {
            PassExecutorState original = MakePopulatedState();

            byte[] buf = new byte[512];
            int writeOffset = 0;
            Serialize(buf, ref writeOffset, in original);

            int readOffset = 0;
            PassExecutorState reconstructed = Deserialize(buf, ref readOffset);

            Assert.AreEqual(writeOffset, readOffset, "read offset must consume exactly the written bytes");
            AssertStateEquals(in original, in reconstructed);
        }

        [Test]
        public void PassExecutor_CaptureRestoreCapture_IsIdentity()
        {
            // Deps are null: CaptureState/RestoreState never touch them (RestoreState recomputes the
            // profile via the pure PassTypeProfiles.GetProfile, no dependency call).
            var executor = new PassExecutor(null, null, null);
            PassExecutorState seeded = MakePopulatedState();

            executor.RestoreState(in seeded);
            PassExecutorState recaptured = executor.CaptureState();

            AssertStateEquals(in seeded, in recaptured);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-19 | —      | Initial implementation — Phase C C0 PassExecutor snapshot-seam |
// |         |            |        | round-trip + Capture/Restore identity locks.                  |
#endregion
