// File:     src/pass-mechanics/PassExecutor.GoalkeeperDistribution.cs
// Created:  2026-09-26
// Modified: 2026-09-28 (W8 B review — reject non-keeper / wrong-team requests at INITIATING)
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.4, §3.8.13; Goalkeeper Mechanics #11 §3.8
// Purpose:  Dedicated goalkeeper-distribution execution mode for PassExecutor.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.PassMechanics
{
    public sealed partial class PassExecutor
    {
        private enum PassExecutionMode
        {
            Ordinary = 0,
            GoalkeeperDistribution = 1
        }

        private PassExecutionMode _mode = PassExecutionMode.Ordinary;
        private GoalkeeperDistributionRequest _goalkeeperRequest;
        private int _goalkeeperEffectiveTargetAgentId = PassMechanicsConstants.AGENT_ID_NONE;
        private Vector3 _goalkeeperEffectiveTargetPosition;
        private GoalkeeperDistributionFeedback _goalkeeperFeedback;
        private bool _goalkeeperFeedbackPending;

        /// <summary>True while an accepted goalkeeper-distribution request is in WINDUP or CONTACT.</summary>
        public bool IsGoalkeeperDistributionInFlight =>
            _mode == PassExecutionMode.GoalkeeperDistribution
            && _state != PassExecutionState.Idle;

        /// <summary>Validates and starts the dedicated goalkeeper-distribution windup, or returns typed rejection feedback.</summary>
        public bool TryStartGoalkeeperDistribution(
            in GoalkeeperDistributionRequest request,
            out GoalkeeperDistributionFeedback feedback)
        {
            feedback = default;
            if (_state != PassExecutionState.Idle || _goalkeeperFeedbackPending
                || !ValidateGoalkeeperDistributionRequest(in request)
                || !_agentQuery.IsGoalkeeperOfTeam(request.AgentId, request.TeamId)
                || !_ballSystem.IsBallPossessedBy(request.AgentId))
            {
                feedback.Kind = GoalkeeperDistributionFeedbackKind.Rejected;
                feedback.EffectiveTargetAgentId = request.TargetAgentId;
                feedback.EffectiveTargetPosition = request.TargetPosition;
                feedback.ContactFrame = -1;
                return false;
            }

            _mode = PassExecutionMode.GoalkeeperDistribution;
            _goalkeeperRequest = request;
            _goalkeeperEffectiveTargetAgentId = request.TargetAgentId;
            _goalkeeperEffectiveTargetPosition = request.TargetPosition;
            _goalkeeperFeedback = default;
            _goalkeeperFeedbackPending = false;
            _profile = GetGoalkeeperDistributionProfile(request.Delivery);
            _windupFramesRemaining = request.WindupFrames;
            _followThroughFramesRemaining = 0;
            _state = PassExecutionState.Windup;
            return true;
        }

        /// <summary>Cancels an accepted goalkeeper distribution before CONTACT and retains terminal feedback for the host.</summary>
        public bool CancelGoalkeeperDistribution()
        {
            if (_mode != PassExecutionMode.GoalkeeperDistribution
                || (_state != PassExecutionState.Windup && _state != PassExecutionState.Contact))
                return false;

            SetGoalkeeperDistributionTerminal(
                GoalkeeperDistributionFeedbackKind.Cancelled,
                _goalkeeperEffectiveTargetAgentId,
                _goalkeeperEffectiveTargetPosition,
                Vector3.zero, Vector3.zero, 0f, -1, 0f);
            return true;
        }

        /// <summary>Consumes one retained terminal result and returns the executor to ordinary mode.</summary>
        public bool TryConsumeGoalkeeperDistributionFeedback(out GoalkeeperDistributionFeedback feedback)
        {
            if (!_goalkeeperFeedbackPending)
            {
                feedback = default;
                return false;
            }

            feedback = _goalkeeperFeedback;
            _goalkeeperFeedback = default;
            _goalkeeperFeedbackPending = false;
            _goalkeeperRequest = default;
            _goalkeeperEffectiveTargetAgentId = PassMechanicsConstants.AGENT_ID_NONE;
            _goalkeeperEffectiveTargetPosition = default;
            _mode = PassExecutionMode.Ordinary;
            return true;
        }

        private void UpdateGoalkeeperDistributionWindup()
        {
            if (!_ballSystem.IsBallPossessedBy(_goalkeeperRequest.AgentId))
            {
                SetGoalkeeperDistributionTerminal(
                    GoalkeeperDistributionFeedbackKind.Cancelled,
                    _goalkeeperEffectiveTargetAgentId,
                    _goalkeeperEffectiveTargetPosition,
                    Vector3.zero, Vector3.zero, 0f, -1, 0f);
                return;
            }

            _windupFramesRemaining--;
            if (_windupFramesRemaining <= 0)
                _state = PassExecutionState.Contact;
        }

        private void ExecuteGoalkeeperDistributionContact(float matchTime, int frameNumber, ref BallState ball)
        {
            GoalkeeperDistributionRequest request = _goalkeeperRequest;
            if (!_ballSystem.IsBallPossessedBy(request.AgentId))
            {
                SetGoalkeeperDistributionTerminal(
                    GoalkeeperDistributionFeedbackKind.Cancelled,
                    _goalkeeperEffectiveTargetAgentId,
                    _goalkeeperEffectiveTargetPosition,
                    Vector3.zero, Vector3.zero, 0f, -1, 0f);
                return;
            }

            PassAgentState keeperState = _agentQuery.GetState(request.AgentId);
            PassAgentAttributes attrs = _agentQuery.GetAttributes(request.AgentId);
            int effectiveTargetAgentId = request.TargetAgentId;
            Vector3 effectiveTarget = request.TargetPosition;

            if (effectiveTargetAgentId >= 0
                && _agentQuery.IsEligibleGoalkeeperDistributionReceiver(effectiveTargetAgentId, request.TeamId))
            {
                PassAgentState receiverState = _agentQuery.GetState(effectiveTargetAgentId);
                effectiveTarget = new Vector3(receiverState.Position.x, receiverState.Position.y, 0f);
            }
            else
            {
                effectiveTargetAgentId = PassMechanicsConstants.AGENT_ID_NONE;
            }

            effectiveTarget = PassTargetResolver.ClampToPitchBounds(effectiveTarget);
            if (effectiveTargetAgentId == PassMechanicsConstants.AGENT_ID_NONE
                && _agentQuery.IsGoalkeeperDistributionOwnGoalLine(request.TeamId, effectiveTarget))
            {
                effectiveTarget = PassTargetResolver.ClampToPitchBounds(
                    _agentQuery.GetGoalkeeperDistributionFallbackPosition(request.TeamId));
            }

            _goalkeeperEffectiveTargetAgentId = effectiveTargetAgentId;
            _goalkeeperEffectiveTargetPosition = effectiveTarget;

            Vector3 releasePoint = new Vector3(
                keeperState.Position.x, keeperState.Position.y, request.ReleaseHeightM);
            PhysicalProfile profile = GetGoalkeeperDistributionProfile(request.Delivery);
            float distanceM = Vector2.Distance(
                keeperState.Position, new Vector2(effectiveTarget.x, effectiveTarget.y));
            float distanceForProfile = Mathf.Clamp(distanceM, 0.001f, profile.DistMax);
            float distanceFraction = distanceForProfile / profile.DistMax;
            float speedBase = profile.VOffset
                + request.EmittedPower01 * distanceFraction * (profile.VMax - profile.VOffset);
            float kickSpeed = Mathf.Clamp(speedBase, profile.VMin, profile.VMax);
            float launchAngleDeg = ComputeGoalkeeperDistributionLaunchAngle(
                request.Delivery, profile, distanceForProfile, distanceFraction);

            Vector3 baseKickDirection =
                PassTargetResolver.ComputeKickDirection(keeperState.Position, effectiveTarget);
            float bodyAngleDeg =
                PassTargetResolver.ComputeBodyAngle(keeperState.FacingDirection, baseKickDirection);
            float pressure = _collisionQuery.ComputePressureScalar(keeperState.Position, request.TeamId);
            PassType errorProfile = GetGoalkeeperDistributionErrorProfile(request.Delivery);
            float errorAngleDeg = PassErrorCalculator.ComputeErrorAngle(
                errorProfile, CrossSubType.Flat, attrs.Passing, pressure, attrs.Fatigue,
                bodyAngleDeg, 0f, false, 5);
            float errorDirection = PassErrorCalculator.ComputeErrorDirection(
                request.AgentId, request.FrameNumber,
                PassMechanicsConstants.GK_DISTRIBUTION_ERROR_HASH_DISCRIMINATOR);
            Vector3 finalDirection = PassTargetResolver.ApplyErrorToDirection(
                baseKickDirection, errorAngleDeg, errorDirection);
            Vector3 finalVelocity = PassVelocityCalculator.ConstructKickVelocity(
                kickSpeed, finalDirection, launchAngleDeg);

            if (!IsFiniteGoalkeeperVector(finalVelocity))
            {
                SetGoalkeeperDistributionTerminal(
                    GoalkeeperDistributionFeedbackKind.Cancelled,
                    effectiveTargetAgentId, effectiveTarget,
                    Vector3.zero, Vector3.zero, errorAngleDeg, -1, 0f);
                return;
            }

            ball.Position = releasePoint;
            ball.LastValidPosition = releasePoint;
            _ballSystem.ApplyKick(
                ref ball, finalVelocity, request.SpinIntent, request.AgentId, matchTime);

            SetGoalkeeperDistributionTerminal(
                GoalkeeperDistributionFeedbackKind.Completed,
                effectiveTargetAgentId, effectiveTarget, releasePoint, finalVelocity,
                errorAngleDeg, frameNumber, matchTime);
        }

        private void SetGoalkeeperDistributionTerminal(
            GoalkeeperDistributionFeedbackKind kind,
            int effectiveTargetAgentId,
            Vector3 effectiveTargetPosition,
            Vector3 releasePoint,
            Vector3 finalVelocity,
            float errorAngleDeg,
            int contactFrame,
            float contactMatchTime)
        {
            _goalkeeperFeedback = new GoalkeeperDistributionFeedback
            {
                Kind = kind,
                EffectiveTargetAgentId = effectiveTargetAgentId,
                EffectiveTargetPosition = effectiveTargetPosition,
                ReleasePoint = releasePoint,
                FinalVelocity = finalVelocity,
                ErrorAngleDeg = errorAngleDeg,
                ContactFrame = contactFrame,
                ContactMatchTime = contactMatchTime
            };
            _goalkeeperFeedbackPending = true;
            _state = PassExecutionState.Idle;
        }

        private static bool ValidateGoalkeeperDistributionRequest(in GoalkeeperDistributionRequest request)
        {
            return request.AgentId >= 0
                && request.TeamId >= 0
                && request.TargetAgentId >= PassMechanicsConstants.AGENT_ID_NONE
                && request.TargetAgentId != request.AgentId
                && IsValidGoalkeeperDeliveryVariant(request.Delivery)
                && IsFiniteGoalkeeperVector(request.TargetPosition)
                && IsFiniteGoalkeeperVector(request.SpinIntent)
                && float.IsFinite(request.EmittedPower01)
                && request.EmittedPower01 >= 0f
                && request.EmittedPower01 <= 1f
                && float.IsFinite(request.ReleaseHeightM)
                && request.ReleaseHeightM >= 0f
                && request.WindupFrames > 0
                && request.FrameNumber >= 0;
        }

        private static bool IsValidGoalkeeperDeliveryVariant(GoalkeeperDeliveryVariant delivery)
        {
            switch (delivery)
            {
                case GoalkeeperDeliveryVariant.Roll:
                case GoalkeeperDeliveryVariant.Throw:
                case GoalkeeperDeliveryVariant.Kick:
                    return true;
                default:
                    return false;
            }
        }

        private static PhysicalProfile GetGoalkeeperDistributionProfile(GoalkeeperDeliveryVariant delivery)
            => PassTypeProfiles.GetProfile(GetGoalkeeperDistributionErrorProfile(delivery));

        private static PassType GetGoalkeeperDistributionErrorProfile(GoalkeeperDeliveryVariant delivery)
        {
            switch (delivery)
            {
                case GoalkeeperDeliveryVariant.Roll: return PassType.Ground;
                case GoalkeeperDeliveryVariant.Throw: return PassType.Driven;
                case GoalkeeperDeliveryVariant.Kick: return PassType.Lofted;
                default: return PassType.Ground;
            }
        }

        private static float ComputeGoalkeeperDistributionLaunchAngle(
            GoalkeeperDeliveryVariant delivery,
            PhysicalProfile profile,
            float distanceForProfile,
            float distanceFraction)
        {
            if (delivery == GoalkeeperDeliveryVariant.Kick)
            {
                float angle = Mathf.Atan(
                    (4f * PassMechanicsConstants.ApexHeightLofted) / distanceForProfile)
                    * Mathf.Rad2Deg;
                return Mathf.Clamp(angle, profile.AngleMin, profile.AngleMax);
            }

            return profile.AngleMin
                + distanceFraction * (profile.AngleMax - profile.AngleMin);
        }

        private static bool IsFiniteGoalkeeperVector(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-26 | —      | W8 B dormant dedicated executor mode: exact windup, CONTACT receiver revalidation/fallback, #5 profile/error reuse, kick application, and retained typed terminal feedback. |
#endregion
