// File:     src/shot-mechanics/ShotEventEmitter.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.10, §4.7, Code Standards #20
// Purpose:  Constructs and publishes ShotExecutedEvent at CONTACT state completion
//           and ShotCancelledEvent on WINDUP tackle interrupt. Stage 0: publishes via
//           EventBusStub (no-op). Must not publish events for Invalid outcomes. §3.10.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Constructs and publishes ShotExecutedEvent and ShotCancelledEvent. §3.10, §4.7.
    /// Shot Mechanics #6 §3.10.
    /// </summary>
    public static class ShotEventEmitter
    {
        /// <summary>
        /// Publishes ShotExecutedEvent after Ball.ApplyKick() completes at CONTACT state.
        /// Must be called AFTER Ball.ApplyKick() — §3.10.2.
        /// Not called for Cancelled or Invalid outcomes. §4.7.1.
        /// </summary>
        public static void PublishShotExecuted(
            in ShotRequest request,
            in ShotResult  result,
            float          matchTime)
        {
            EventBusStub.Publish(new ShotExecutedEvent
            {
                ShootingAgentId   = request.AgentId,
                TeamId            = request.TeamId,
                KickVelocity      = result.FinalVelocity,
                KickSpin          = result.FinalSpin,
                IntendedTarget    = request.PlacementTarget,
                FinalDirection    = result.FinalDirection,
                BodyMechanicsScore = result.BodyMechanicsScore,
                PowerIntent       = request.PowerIntent,
                ContactZone       = request.ContactZone,
                DistanceToGoal    = request.DistanceToGoal,
                MatchTime         = matchTime,
                ContactFrame      = result.ContactFrame,
                StumbleTriggered  = result.StumbleTriggered
            });
        }

        /// <summary>
        /// Publishes ShotCancelledEvent when a tackle interrupt fires during WINDUP.
        /// Not published for ShotOutcome.Invalid (programming error, not a game event). §4.7.1.
        /// </summary>
        public static void PublishShotCancelled(
            in ShotRequest request,
            int            cancelFrame)
        {
            EventBusStub.Publish(new ShotCancelledEvent
            {
                AgentId     = request.AgentId,
                TeamId      = request.TeamId,
                CancelFrame = cancelFrame,
                Reason      = ShotCancelReason.TackleInterrupt
            });
        }

        /// <summary>
        /// Populates and publishes ShotAnimationData stub at CONTACT state. §2.4.4.
        /// Unconsumed at Stage 0; Animation System subscribes at Stage 1+.
        /// </summary>
        public static void PublishAnimationData(
            in ShotRequest request,
            float          bodyMechanicsScore,
            int            windupFrames)
        {
            EventBusStub.Publish(new ShotAnimationData
            {
                AgentId            = request.AgentId,
                ContactZone        = request.ContactZone,
                PowerIntent        = request.PowerIntent,
                BodyMechanicsScore = bodyMechanicsScore,
                IsWeakFoot         = request.IsWeakFoot,
                WindupFrames       = windupFrames
            });
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
