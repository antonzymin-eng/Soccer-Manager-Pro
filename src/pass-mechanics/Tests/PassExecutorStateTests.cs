// File:     src/pass-mechanics/Tests/PassExecutorStateTests.cs
// Created:  2026-06-19
// Modified: 2026-09-28 (W8 B review — IPassAgentQuery identity stub)
// Modified: 2026-09-26 (W8 B — dedicated state serializer/identity coverage + 27-field omission guard)
// Modified: 2026-06-19
// Author:   —
// Spec:     Pass Mechanics #5 §3.8; Match Engine design note §2.6 (Phase C step C0); Code Standards #20
// Purpose:  Locks the Phase C C0 PassExecutor snapshot seam: (1) a PassExecutorState survives a
//           CanonicalSerializer write/read round-trip byte-for-byte (the serialization the match-engine
//           snapshot layer performs at C5), and (2) CaptureState → RestoreState → CaptureState is the
//           identity on the executor's cross-tick fields (the seam is lossless, incl. the recomputed
//           internal PhysicalProfile path).

using System.Reflection;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.BallPhysics;
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

            var goalkeeperRequest = new GoalkeeperDistributionRequest
            {
                AgentId = 1, TeamId = 0, Delivery = GoalkeeperDeliveryVariant.Throw,
                TargetAgentId = 4, TargetPosition = new Vector3(28f, 18f, 0f),
                EmittedPower01 = 0.75f, SpinIntent = new Vector3(1f, 2f, 3f),
                ReleaseHeightM = 1.8f, WindupFrames = 24, FrameNumber = 4300
            };
            var goalkeeperFeedback = new GoalkeeperDistributionFeedback
            {
                Kind = GoalkeeperDistributionFeedbackKind.Completed,
                EffectiveTargetAgentId = 4,
                EffectiveTargetPosition = new Vector3(29f, 19f, 0f),
                ReleasePoint = new Vector3(8f, 34f, 1.8f),
                FinalVelocity = new Vector3(15f, 2f, 3f),
                ErrorAngleDeg = 1.25f, ContactFrame = 4324, ContactMatchTime = 72.4f
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
                lastResult: lastResult,
                executionMode: 1,
                goalkeeperRequest: goalkeeperRequest,
                goalkeeperEffectiveTargetAgentId: 4,
                goalkeeperEffectiveTargetPosition: new Vector3(29f, 19f, 0f),
                goalkeeperFeedbackPending: true,
                goalkeeperFeedback: goalkeeperFeedback);
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

            CanonicalSerializer.WriteI32(buf, ref o, s.ExecutionMode);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperRequest.AgentId);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperRequest.TeamId);
            CanonicalSerializer.WriteU8(buf, ref o, (byte)s.GoalkeeperRequest.Delivery);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperRequest.TargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.TargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.TargetPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.TargetPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.EmittedPower01);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.SpinIntent.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.SpinIntent.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.SpinIntent.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperRequest.ReleaseHeightM);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperRequest.WindupFrames);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperRequest.FrameNumber);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperEffectiveTargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperEffectiveTargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperEffectiveTargetPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperEffectiveTargetPosition.z);
            CanonicalSerializer.WriteBool(buf, ref o, s.GoalkeeperFeedbackPending);
            CanonicalSerializer.WriteU8(buf, ref o, (byte)s.GoalkeeperFeedback.Kind);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperFeedback.EffectiveTargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.EffectiveTargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.EffectiveTargetPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.EffectiveTargetPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.ReleasePoint.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.ReleasePoint.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.ReleasePoint.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.FinalVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.FinalVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.FinalVelocity.z);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.ErrorAngleDeg);
            CanonicalSerializer.WriteI32(buf, ref o, s.GoalkeeperFeedback.ContactFrame);
            CanonicalSerializer.WriteF32(buf, ref o, s.GoalkeeperFeedback.ContactMatchTime);
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

            int executionMode = CanonicalSerializer.ReadI32(buf, ref o);
            var goalkeeperRequest = new GoalkeeperDistributionRequest
            {
                AgentId = CanonicalSerializer.ReadI32(buf, ref o),
                TeamId = CanonicalSerializer.ReadI32(buf, ref o),
                Delivery = (GoalkeeperDeliveryVariant)CanonicalSerializer.ReadU8(buf, ref o),
                TargetAgentId = CanonicalSerializer.ReadI32(buf, ref o),
                TargetPosition = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                EmittedPower01 = CanonicalSerializer.ReadF32(buf, ref o),
                SpinIntent = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                ReleaseHeightM = CanonicalSerializer.ReadF32(buf, ref o),
                WindupFrames = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber = CanonicalSerializer.ReadI32(buf, ref o)
            };
            int goalkeeperEffectiveTargetAgentId = CanonicalSerializer.ReadI32(buf, ref o);
            var goalkeeperEffectiveTargetPosition = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            bool goalkeeperFeedbackPending = CanonicalSerializer.ReadBool(buf, ref o);
            var goalkeeperFeedback = new GoalkeeperDistributionFeedback
            {
                Kind = (GoalkeeperDistributionFeedbackKind)CanonicalSerializer.ReadU8(buf, ref o),
                EffectiveTargetAgentId = CanonicalSerializer.ReadI32(buf, ref o),
                EffectiveTargetPosition = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                ReleasePoint = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                FinalVelocity = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                ErrorAngleDeg = CanonicalSerializer.ReadF32(buf, ref o),
                ContactFrame = CanonicalSerializer.ReadI32(buf, ref o),
                ContactMatchTime = CanonicalSerializer.ReadF32(buf, ref o)
            };

            return new PassExecutorState(
                state, in request, effectiveSubType,
                kickSpeed, launchAngleDeg, spinVector, baseKickDirection, aimPoint, leadDistance,
                cachedPassing, cachedFatigue, cachedBodyAngleDeg, cachedIsWeakFoot, cachedWeakFootRating,
                windupRemaining, followThroughRemaining, in lastResult, executionMode, in goalkeeperRequest,
                goalkeeperEffectiveTargetAgentId, goalkeeperEffectiveTargetPosition,
                goalkeeperFeedbackPending, in goalkeeperFeedback);
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
            Assert.AreEqual(expected.ExecutionMode, actual.ExecutionMode, "ExecutionMode");
            Assert.AreEqual(expected.GoalkeeperRequest.AgentId, actual.GoalkeeperRequest.AgentId, "GoalkeeperRequest.AgentId");
            Assert.AreEqual(expected.GoalkeeperRequest.TeamId, actual.GoalkeeperRequest.TeamId, "GoalkeeperRequest.TeamId");
            Assert.AreEqual(expected.GoalkeeperRequest.Delivery, actual.GoalkeeperRequest.Delivery, "GoalkeeperRequest.Delivery");
            Assert.AreEqual(expected.GoalkeeperRequest.TargetAgentId, actual.GoalkeeperRequest.TargetAgentId, "GoalkeeperRequest.TargetAgentId");
            Assert.AreEqual(expected.GoalkeeperRequest.TargetPosition, actual.GoalkeeperRequest.TargetPosition, "GoalkeeperRequest.TargetPosition");
            Assert.AreEqual(expected.GoalkeeperRequest.EmittedPower01, actual.GoalkeeperRequest.EmittedPower01, "GoalkeeperRequest.EmittedPower01");
            Assert.AreEqual(expected.GoalkeeperRequest.SpinIntent, actual.GoalkeeperRequest.SpinIntent, "GoalkeeperRequest.SpinIntent");
            Assert.AreEqual(expected.GoalkeeperRequest.ReleaseHeightM, actual.GoalkeeperRequest.ReleaseHeightM, "GoalkeeperRequest.ReleaseHeightM");
            Assert.AreEqual(expected.GoalkeeperRequest.WindupFrames, actual.GoalkeeperRequest.WindupFrames, "GoalkeeperRequest.WindupFrames");
            Assert.AreEqual(expected.GoalkeeperRequest.FrameNumber, actual.GoalkeeperRequest.FrameNumber, "GoalkeeperRequest.FrameNumber");
            Assert.AreEqual(expected.GoalkeeperEffectiveTargetAgentId, actual.GoalkeeperEffectiveTargetAgentId, "GoalkeeperEffectiveTargetAgentId");
            Assert.AreEqual(expected.GoalkeeperEffectiveTargetPosition, actual.GoalkeeperEffectiveTargetPosition, "GoalkeeperEffectiveTargetPosition");
            Assert.AreEqual(expected.GoalkeeperFeedbackPending, actual.GoalkeeperFeedbackPending, "GoalkeeperFeedbackPending");
            Assert.AreEqual(expected.GoalkeeperFeedback.Kind, actual.GoalkeeperFeedback.Kind, "GoalkeeperFeedback.Kind");
            Assert.AreEqual(expected.GoalkeeperFeedback.EffectiveTargetAgentId, actual.GoalkeeperFeedback.EffectiveTargetAgentId, "GoalkeeperFeedback.EffectiveTargetAgentId");
            Assert.AreEqual(expected.GoalkeeperFeedback.EffectiveTargetPosition, actual.GoalkeeperFeedback.EffectiveTargetPosition, "GoalkeeperFeedback.EffectiveTargetPosition");
            Assert.AreEqual(expected.GoalkeeperFeedback.ReleasePoint, actual.GoalkeeperFeedback.ReleasePoint, "GoalkeeperFeedback.ReleasePoint");
            Assert.AreEqual(expected.GoalkeeperFeedback.FinalVelocity, actual.GoalkeeperFeedback.FinalVelocity, "GoalkeeperFeedback.FinalVelocity");
            Assert.AreEqual(expected.GoalkeeperFeedback.ErrorAngleDeg, actual.GoalkeeperFeedback.ErrorAngleDeg, "GoalkeeperFeedback.ErrorAngleDeg");
            Assert.AreEqual(expected.GoalkeeperFeedback.ContactFrame, actual.GoalkeeperFeedback.ContactFrame, "GoalkeeperFeedback.ContactFrame");
            Assert.AreEqual(expected.GoalkeeperFeedback.ContactMatchTime, actual.GoalkeeperFeedback.ContactMatchTime, "GoalkeeperFeedback.ContactMatchTime");
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

        // Stubs let a real Execute() populate the in-flight fields from genuine computation rather
        // than a hand-built DTO — this is what upgrades the round-trip from "my DTO survives bytes"
        // to "CaptureState reflects what Execute() actually produced". The lifecycle stays in WINDUP
        // so no CONTACT publish is reached (the executor's EventBus publish needs a booted registry;
        // full CONTACT-through-publish behavioural parity is exercised at Phase C C3's
        // MatchEngineResolveTests where the Resolve phase boots the EventBus).
        private sealed class StubBall : IPassBallSystem
        {
            public bool IsBallPossessedBy(int agentId) => true;
            public void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime) { }
        }

        private sealed class StubAgent : IPassAgentQuery
        {
            public PassAgentAttributes GetAttributes(int agentId) => new PassAgentAttributes
            {
                Passing = 14f, Technique = 12f, KickPower = 13f, WeakFootRating = 3, Crossing = 14f, Fatigue = 0.2f
            };
            public PassAgentState GetState(int agentId) => new PassAgentState
            {
                Position = new Vector2(30f, 34f), Velocity = Vector2.zero, FacingDirection = new Vector2(1f, 0f)
            };

            public bool IsGoalkeeperOfTeam(int agentId, int teamId) => true;

            public bool IsEligibleGoalkeeperDistributionReceiver(int agentId, int teamId) => true;

            public bool IsGoalkeeperDistributionOwnGoalLine(int teamId, Vector3 targetPosition)
                => teamId == 0 ? targetPosition.x == 0f : targetPosition.x == 105f;

            public Vector3 GetGoalkeeperDistributionFallbackPosition(int teamId)
                => teamId == 0 ? new Vector3(35f, 34f, 0f) : new Vector3(70f, 34f, 0f);
        }

        private sealed class StubCollision : IPassCollisionQuery
        {
            public bool GetAndClearTackleFlag(int agentId) => false;
            public float ComputePressureScalar(Vector2 passerPosition, int passerTeamId) => 0.3f;
        }

        [Test]
        public void PassExecutor_RealExecuteThenCaptureRestore_PreservesComputedState()
        {
            var ball = new StubBall();
            var agent = new StubAgent();
            var collision = new StubCollision();

            var request = new PassRequest
            {
                AgentId          = 7,
                PassType         = PassType.Ground,
                CrossSubType     = CrossSubType.Flat,
                TargetAgentId    = 13,
                TargetPosition   = Vector3.zero,
                IntendedDistance = 15f,
                Urgency          = 0.2f,
                IsWeakFoot       = false,
                TeamId           = 1,
                FrameNumber      = 100
            };

            var executorA = new PassExecutor(ball, agent, collision);
            PassResult initiated = executorA.Execute(in request);
            Assert.AreEqual(PassOutcome.Initiated, initiated.Outcome, "Execute should begin windup");
            Assert.IsFalse(executorA.IsIdle, "executor should be mid-windup");

            // Capture the genuinely-computed in-flight state, restore into a fresh executor, recapture.
            PassExecutorState captured = executorA.CaptureState();
            var executorB = new PassExecutor(null, null, null);
            executorB.RestoreState(in captured);
            PassExecutorState recaptured = executorB.CaptureState();

            AssertStateEquals(in captured, in recaptured);
            // Sanity: the computed kick speed is real (not a default), so the seam is moving live data.
            Assert.Greater(captured.KickSpeed, 0f, "Execute must have computed a positive kick speed");
        }

        [Test]
        public void PassExecutor_InstanceFieldCount_MatchesSerializedSet()
        {
            // Mechanical coupling guard, the analogue of the B0 OscillationGuard BufferSize assert:
            // if a maintainer adds cross-tick in-flight state to PassExecutor without extending
            // PassExecutorState / CaptureState / RestoreState, the snapshot silently drops it and
            // replay diverges (invisible to same-seed in-process tests — the §2.6 trap). This count
            // (3 injected deps + 18 ordinary fields + 6 goalkeeper-mode fields) trips first.
            int fieldCount = typeof(PassExecutor)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Length;

            Assert.AreEqual(27, fieldCount,
                "PassExecutor instance field count changed. If you added cross-tick in-flight state, " +
                "extend PassExecutorState + CaptureState + RestoreState, then update this count.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-19 | —      | Initial implementation — Phase C C0 PassExecutor snapshot-seam |
// |         |            |        | round-trip + Capture/Restore identity locks.                  |
// | 1.1     | 2026-06-19 | —      | C0 AR-1: added M-1 real-Execute capture/restore preservation  |
// |         |            |        | test (genuine computed state, stays in WINDUP) + M-2 reflection|
// |         |            |        | field-count lock (silent-omission guard, B0 BufferSize analogue|
// |         |            |        | ). Added stub IPass* implementations.                         |
// | 1.2     | 2026-09-26 | —      | W8 B: serializer/deserialize and Capture/Restore identity cover the appended dedicated-distribution fields; reflection omission guard advances 21 → 27 fields. |
// | 1.3     | 2026-09-28 | —      | W8 B review: test IPassAgentQuery stub implements the new goalkeeper/team identity query; snapshot field set remains unchanged. |
#endregion
