// File:     src/match-engine/MatchEngine.GoalkeeperDistribution.cs
// Created:  2026-10-15
// Modified: 2026-10-15
// Author:   —
// Spec:     #21 §3.4.1, #11 §3.8, #5 §3.8.13; match-engine-design W8 B contract
// Purpose:  Engine-owned six-policy hand-distribution producer and terminal handoff to #11.

using System;

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.EventSystem;
using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    public sealed partial class MatchEngine
    {
        private static readonly ProfilerMarker s_distributionMarker = new ProfilerMarker("MatchEngine.GoalkeeperDistribution");

        private enum PossessionChangeCause
        {
            Ordinary,
            GoalkeeperDistributionContact
        }

        // Observation only; never read by gameplay or serialized.
        internal Action<int, GkDistributionPolicy, GoalkeeperDistributionRequest> TestOnly_W8BCommitObserver;

        /// <summary>Runs only the production selector/producer for RNG and retry-budget locks.</summary>
        internal void TestOnly_RunGoalkeeperDistributionProducer() => TryCommitGoalkeeperDistributions();

        private bool HasGoalkeeperHandControl(int agentId) =>
            _gkHeadingEnabled && agentId == _possessingAgentId && _isGoalkeeper[agentId]
            && _gkAgentIds[_teamIds[agentId]] == agentId && _goalkeeper.HasHandControl(_teamIds[agentId]);

        // Fixed-end helpers: these are the shared direction source for producer and CONTACT fallback.
        private static float OwnGoalX(int teamId) => teamId == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
        private static float AttackDirectionX(int teamId) => teamId == 0 ? 1f : -1f;
        private static Vector3 GoalkeeperDistributionFallback(int teamId) => new Vector3(
            OwnGoalX(teamId) + AttackDirectionX(teamId) * TacticalInstructionsConstants.GK_DIST_FALLBACK_ADVANCE_M,
            MatchEngineConstants.PITCH_WIDTH_M * 0.5f, 0f);

        private void TryCommitGoalkeeperDistributions()
        {
            using var marker = s_distributionMarker.Auto();
            if (!_gkHeadingEnabled) return;
            int frame = (int)_clock.CurrentTick;
            int tick = (int)_clock.CurrentTacticalTick;
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                int keeper = _gkAgentIds[team];
                if (keeper < 0 || _isSentOff[keeper] || !HasGoalkeeperHandControl(keeper)
                    || !_goalkeeper.TryGetDistributionGate(team, out int claimTick, out int releaseTick))
                    continue;

                GkDistributionPolicy policy = _activeTeamTactics[team].GkDistribution;
                GetDistributionPolicy(policy, out DeliveryKind delivery, out int delay, out float power);
                if (tick < releaseTick || tick < claimTick + delay) continue;

                int windup = Mathf.CeilToInt(GoalkeeperDistribution.ComputeWindupMs(delivery)
                    * DeterministicSimConstants.PHYSICS_TICK_HZ / 1000f);
                if (!DistributionContactFits(frame, claimTick, windup)) continue;

                int receiver = SelectDistributionReceiver(keeper, team, policy);
                Vector3 target = receiver < 0 ? GoalkeeperDistributionFallback(team)
                    : new Vector3(_agents[receiver].Position.x, _agents[receiver].Position.y, 0f);
                var intent = new DistributeIntent
                {
                    DeliveryKind = delivery,
                    TargetReceiverId = receiver < 0 ? (int?)null : receiver,
                    TargetPoint = target,
                    PowerIntent = power,
                    SpinIntent = TacticalInstructionsConstants.GK_DIST_SPIN
                };
                var request = new GoalkeeperDistributionRequest
                {
                    AgentId = keeper,
                    TeamId = team,
                    Delivery = delivery == DeliveryKind.Roll ? GoalkeeperDeliveryVariant.Roll
                        : delivery == DeliveryKind.Throw ? GoalkeeperDeliveryVariant.Throw : GoalkeeperDeliveryVariant.Kick,
                    TargetAgentId = receiver,
                    TargetPosition = target,
                    EmittedPower01 = Mathf.Clamp01(power * GoalkeeperDistribution.ComputeAccuracyCoeff(
                        delivery, _goalkeeper.CaptureState().Attrs[team])),
                    SpinIntent = intent.SpinIntent,
                    ReleaseHeightM = GoalkeeperDistribution.ComputeReleasePoint(Vector3.zero, delivery).z,
                    WindupFrames = windup,
                    FrameNumber = frame
                };
                _goalkeeper.CommitDistributeIntent(team, intent);
                bool accepted = StartGoalkeeperDistributionAtHost(in request, out _);
                if (accepted)
                {
                    _goalkeeper.AcceptDistribution(team);
                    TestOnly_W8BCommitObserver?.Invoke(keeper, policy, request);
                }
            }
        }

        private bool DistributionContactFits(int frame, int claimTick, int windup)
        {
            int contact = frame + windup + 1;
            int handBoundary = (claimTick + GoalkeeperConstants.GK_HOLD_MAX_TICKS)
                * GoalkeeperConstants.FramesPerTacticalTick;
            // Resolve has not yet incremented the inherited possession guard on this frame.
            int dropBoundary = frame + MatchEngineConstants.GkMaxHoldTicks - _gkHoldTicks - 1;
            return contact < handBoundary && contact < dropBoundary;
        }

        private int SelectDistributionReceiver(int keeper, int team, GkDistributionPolicy policy)
        {
            if (policy == GkDistributionPolicy.LongKick) return PassMechanicsConstants.AGENT_ID_NONE;
            int best = PassMechanicsConstants.AGENT_ID_NONE;
            float bestDistance = float.PositiveInfinity;
            float bestWidth = -1f;
            float radius = TacticalInstructionsConstants.GK_DIST_LOCAL_RECEIVER_RADIUS_M;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_teamIds[i] != team || _isSentOff[i] || _isGoalkeeper[i]) continue;
                float distance = (_agents[i].Position - _agents[keeper].Position).sqrMagnitude;
                if (distance > radius * radius) continue;
                float width = Mathf.Abs(_agents[i].Position.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f);
                bool wider = policy == GkDistributionPolicy.ThrowOut;
                if (best < 0 || (wider && width > bestWidth)
                    || ((!wider || width == bestWidth) && distance < bestDistance))
                {
                    best = i; bestDistance = distance; bestWidth = width;
                }
            }
            return best;
        }

        private static void GetDistributionPolicy(GkDistributionPolicy policy,
            out DeliveryKind delivery, out int delay, out float power)
        {
            delay = TacticalInstructionsConstants.GK_DIST_STANDARD_DELAY_TICKS;
            switch (policy)
            {
                case GkDistributionPolicy.SlowDown:
                    delivery = DeliveryKind.Roll; delay = TacticalInstructionsConstants.GK_DIST_SLOW_DELAY_TICKS;
                    power = TacticalInstructionsConstants.GK_DIST_SLOW_POWER; return;
                case GkDistributionPolicy.Quick:
                    delivery = DeliveryKind.Throw; delay = TacticalInstructionsConstants.GK_DIST_QUICK_DELAY_TICKS;
                    power = TacticalInstructionsConstants.GK_DIST_QUICK_POWER; return;
                case GkDistributionPolicy.ShortKick:
                    delivery = DeliveryKind.Kick; power = TacticalInstructionsConstants.GK_DIST_SHORT_KICK_POWER; return;
                case GkDistributionPolicy.LongKick:
                    delivery = DeliveryKind.Kick; power = TacticalInstructionsConstants.GK_DIST_LONG_KICK_POWER; return;
                case GkDistributionPolicy.RollOut:
                    delivery = DeliveryKind.Roll; power = TacticalInstructionsConstants.GK_DIST_ROLL_OUT_POWER; return;
                case GkDistributionPolicy.ThrowOut:
                    delivery = DeliveryKind.Throw; power = TacticalInstructionsConstants.GK_DIST_THROW_OUT_POWER; return;
                default: throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        private void CancelExpiredGoalkeeperDistributions()
        {
            if (!_gkHeadingEnabled) return;
            GoalkeeperTickState state = _goalkeeper.CaptureState();
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                int keeper = _gkAgentIds[team];
                if (keeper < 0 || !state.DistributionPending[team]) continue;
                int boundary = (state.ClaimTick[team] + GoalkeeperConstants.GK_HOLD_MAX_TICKS)
                    * GoalkeeperConstants.FramesPerTacticalTick;
                if ((int)_clock.CurrentTick >= boundary || _gkHoldTicks + 1 >= MatchEngineConstants.GkMaxHoldTicks)
                    _passExecutors[keeper].CancelGoalkeeperDistribution();
            }
        }

        private void CompleteGoalkeeperDistribution(int keeper, in GoalkeeperDistributionRequest request,
            in GoalkeeperDistributionFeedback feedback)
        {
            bool completed = feedback.Kind == GoalkeeperDistributionFeedbackKind.Completed;
            if (completed)
            {
                var evt = new DistributionExecutedEvent
                {
                    AgentId = keeper,
                    MatchTimeMs = feedback.ContactMatchTime * 1000f,
                    DeliveryKind = request.Delivery == GoalkeeperDeliveryVariant.Roll ? DeliveryKind.Roll
                        : request.Delivery == GoalkeeperDeliveryVariant.Throw ? DeliveryKind.Throw : DeliveryKind.Kick,
                    ReleasePoint = feedback.ReleasePoint,
                    TargetReceiverId = feedback.EffectiveTargetAgentId,
                    TargetPoint = feedback.EffectiveTargetPosition,
                    EmittedPowerIntent = request.EmittedPower01,
                    WindupMs = request.WindupFrames * 1000f / DeterministicSimConstants.PHYSICS_TICK_HZ
                };
                EventBus.Publish(in evt);
            }
            int team = _teamIds[keeper];
            if (_gkAgentIds[team] == keeper)
                _goalkeeper.FinishDistribution(team, completed, _possessingAgentId == keeper, (int)_clock.CurrentTick);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-15 | —      | W8 B producer, deterministic selector, live guard budget and Resolve completion. |
#endregion
