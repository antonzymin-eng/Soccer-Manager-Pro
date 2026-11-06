// File:     src/match-engine/MatchEngine.KeeperHandControlLaw.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §§3.8.4–3.8.5; match-flow §5.3 (ERR-011-021).
// Purpose:  Resolve the eight-second hand-control offence and validate schema-v28 hand episodes.

using System;

using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;

namespace TacticalDirector.MatchEngine
{
    public sealed partial class MatchEngine
    {
        // Fixture controls/observations: inactive in ordinary matches, excluded from snapshot state.
        internal BallState? TestOnly_PostReattachmentBall;
        internal bool TestOnly_SuppressFeetGuard;
        internal int TestOnly_GkReleasedAgentId => _gkReleasedAgentId;
        internal ulong TestOnly_CardSeverityCursor => _rng.GetStreamState(_cardSeverityStreamIndex).RngCursor;
        internal void TestOnly_ResetGoalkeeperSlot(int team) => _goalkeeper.ResetSlot(team);
        internal static bool TestOnly_DistributionContactFits(int frame, long start, int windup) =>
            DistributionContactFits(frame, start, windup);
        internal int TestOnly_KeeperWhistleFinishCount { get; private set; }

        internal bool TestOnly_IsKeeperHandControlOffenceEligible(int keeper) =>
            IsKeeperHandControlOffenceEligible(keeper);

        private bool IsKeeperHandControlOffenceEligible(int keeper) =>
            keeper >= 0 && HasGoalkeeperHandControl(keeper)
            && _ball.State == BallStateType.Controlled
            && _goalkeeper.GetHandControlStartFrame(_teamIds[keeper]) >= 0
            && _pendingHandlingOffender < 0
            && !BallCollision.CheckBoundaries(_ball, _prevTickBallPosition, _teamIds[keeper]).isOut;

        private bool ApplyKeeperHandControlOffence()
        {
            int keeper = _possessingAgentId;
            if (!IsKeeperHandControlOffenceEligible(keeper)) return false;
            int team = _teamIds[keeper];
            if ((long)_clock.CurrentTick - _goalkeeper.GetHandControlStartFrame(team)
                <= GoalkeeperConstants.GK_HAND_CONTROL_MAX_FRAMES) return false;

            // Only X mirrors: Law 17 chooses the side nearest the keeper, with high-Y at the tie.
            var (spot, awardedTeam) = RestartResolver.Resolve(RestartType.Corner,
                new Vector2(OwnGoalX(team), _agents[keeper].Position.y), team);
            return FinishKeeperWhistle(spot, awardedTeam, RestartCue.Corner,
                (byte)RestartType.Corner, recordIndirectTaker: false);
        }

        // Run after the whole body is decoded: full time freezes clocks and WINDUP, but never
        // relaxes structural ownership. A terminal-pending cancellation is not an in-flight request.
        private void ValidateRestoredKeeperHandControl()
        {
            long frame = (long)_clock.CurrentTick;
            for (int team = 0; team < GoalkeeperConstants.MaxGkAgents; team++)
            {
                long start = _goalkeeper.GetHandControlStartFrame(team);
                bool hand = _goalkeeper.HasHandControl(team);
                bool active = start >= 0 && start <= frame;
                // Serialized on-pitch keeper roles may differ from constructor / pre-overlay slots.
                int keeper = FirstGoalkeeperOnTeam(team);
                if ((hand ? !active : start != -1)
                    || (hand && (!_matchEnded && frame - start > GoalkeeperConstants.GK_HAND_CONTROL_MAX_FRAMES))
                    || (hand && (keeper < 0 || keeper != _possessingAgentId || _ball.State != BallStateType.Controlled)))
                    throw new InvalidOperationException("Corrupt goalkeeper hand-control clock or ownership.");
            }
            for (int agent = 0; agent < MatchEngineConstants.SQUAD_SIZE; agent++)
            {
                if (!_passExecutors[agent].IsGoalkeeperDistributionInFlight) continue;
                int team = _teamIds[agent];
                long start = _goalkeeper.GetHandControlStartFrame(team);
                PassExecutorState executor = _passExecutors[agent].CaptureState();
                if (start < 0 || FirstGoalkeeperOnTeam(team) != agent || !_goalkeeper.HasHandControl(team)
                    || (!_matchEnded && frame + executor.WindupFramesRemaining + 1
                        > start + GoalkeeperConstants.GK_HAND_CONTROL_MAX_FRAMES))
                    throw new InvalidOperationException("Corrupt goalkeeper distribution hand-control budget.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0 | 2026-11-06 | — | Exact-frame Law-12 corner, defensive eligibility, v28 restore and full-time freeze. |
#endregion
