// File:     src/match-engine/MatchEngine.KeeperHandlingLaw.cs
// Created:  2026-10-16
// Modified: 2026-10-16
// Author:   —
// Spec:     Owner-approved separate re-handling fix (October 16, 2026); IFAB Laws 12.2 / 13.
// Purpose:  Enforce own-release re-handling without changing recovery or distribution policy.

using System;

using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    public sealed partial class MatchEngine
    {
        // Canonical v27 state. A keeper's OWN foot/body touches do not discharge the restriction.
        private int _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;
        private int _pendingHandlingOffender = MatchEngineConstants.NO_POSSESSION;
        private Vector3 _pendingHandlingPosition;
        private int _indirectKickTaker = MatchEngineConstants.NO_POSSESSION;

        private void RecordPlayerBallTouch(int agentId)
        {
            if (_pendingHandlingOffender >= 0 || agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE) return;
            if (_releasedHandKeeper != agentId) _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;
            if (_indirectKickTaker != agentId) _indirectKickTaker = MatchEngineConstants.NO_POSSESSION;
        }

        private void RecordHandRelease(int agentId)
        {
            if (!_gkHeadingEnabled || agentId < 0 || !_isGoalkeeper[agentId]) return;
            int team = _teamIds[agentId];
            GoalkeeperState state = _goalkeeper.GetState(team);
            if (_gkAgentIds[team] == agentId &&
                (state == GoalkeeperState.HandsOnBall || state == GoalkeeperState.Distributing))
                _releasedHandKeeper = agentId;
        }

        // Called only after #11 has established an actual catch/parry/deflection/spill contact.
        // Queue the offence in Physics; ball mutation and its Tier A restart occur in Resolve.
        private bool TryKeeperHandContact(int agentId, Vector3 position)
        {
            if (_pendingHandlingOffender >= 0) return false;
            if (_releasedHandKeeper == agentId)
            {
                // Once the whole ball has left play, the existing boundary restart takes priority.
                if (BallCollision.CheckBoundaries(_ball, _prevTickBallPosition, _teamIds[agentId]).isOut)
                    return false;
                _pendingHandlingOffender = agentId;
                _pendingHandlingPosition = position;
                return false;
            }
            RecordPlayerBallTouch(agentId);
            return true;
        }

        private bool ApplyPendingKeeperHandlingOffence()
        {
            if (_pendingHandlingOffender < 0) return false;
            int offender = _pendingHandlingOffender;
            Vector3 contact = _pendingHandlingPosition;
            int team = _teamIds[offender];
            float ownX = OwnGoalX(team);
            float depth = Mathf.Abs(contact.x - ownX);
            float lateral = Mathf.Abs(contact.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f);
            float halfGoal = BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f;
            bool onPitch = contact.x >= 0f && contact.x <= MatchEngineConstants.PITCH_LENGTH_M;
            bool inside = onPitch && depth <= GoalkeeperConstants.PenaltyAreaDepthM &&
                lateral <= GoalkeeperConstants.PenaltyAreaDepthM + halfGoal;
            Vector2 spot = new Vector2(contact.x, contact.y);
            // Law 13: an attacking IFK inside the goal area is taken on its parallel boundary.
            if (inside && depth < MatchEngineConstants.GOAL_AREA_DEPTH_M &&
                lateral <= MatchEngineConstants.GOAL_AREA_DEPTH_M + halfGoal)
                spot.x = ownX + AttackDirectionX(team) * MatchEngineConstants.GOAL_AREA_DEPTH_M;

            ApplyRestart(spot, 1 - team, RestartCue.FreeKick);
            _gkHoldTicks = 0;
            _gkReleasedAgentId = MatchEngineConstants.NO_POSSESSION;
            _gkReleaseCooldownRemaining = 0;
            if (inside) _indirectKickTaker = _possessingAgentId;
            var evt = new RestartAwardedEvent(
                inside ? RestartAwardedEvent.INDIRECT_FREE_KICK : RestartAwardedEvent.DIRECT_FREE_KICK,
                (byte)(1 - team), new Vector3(spot.x, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
            EventBus.Publish(in evt);
            return true;
        }

        private void ClearHandlingLawAtRestart()
        {
            _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;
            _pendingHandlingOffender = MatchEngineConstants.NO_POSSESSION;
            _pendingHandlingPosition = Vector3.zero;
            _indirectKickTaker = MatchEngineConstants.NO_POSSESSION;
        }

        private void WriteHandlingLawState(byte[] buffer, ref int offset)
        {
            CanonicalSerializer.WriteI32(buffer, ref offset, _releasedHandKeeper);
            CanonicalSerializer.WriteI32(buffer, ref offset, _pendingHandlingOffender);
            CanonicalSerializer.WriteF32(buffer, ref offset, _pendingHandlingPosition.x);
            CanonicalSerializer.WriteF32(buffer, ref offset, _pendingHandlingPosition.y);
            CanonicalSerializer.WriteF32(buffer, ref offset, _pendingHandlingPosition.z);
            CanonicalSerializer.WriteI32(buffer, ref offset, _indirectKickTaker);
        }

        private void ReadHandlingLawState(byte[] buffer, ref int offset)
        {
            _releasedHandKeeper = CanonicalSerializer.ReadI32(buffer, ref offset);
            _pendingHandlingOffender = CanonicalSerializer.ReadI32(buffer, ref offset);
            _pendingHandlingPosition = new Vector3(CanonicalSerializer.ReadF32(buffer, ref offset),
                CanonicalSerializer.ReadF32(buffer, ref offset), CanonicalSerializer.ReadF32(buffer, ref offset));
            _indirectKickTaker = CanonicalSerializer.ReadI32(buffer, ref offset);
            if (!IsLawAgentId(_releasedHandKeeper) || !IsLawAgentId(_pendingHandlingOffender) ||
                !IsLawAgentId(_indirectKickTaker) || !IsFiniteLawPosition(_pendingHandlingPosition) ||
                (_pendingHandlingOffender >= 0 && _pendingHandlingOffender != _releasedHandKeeper) ||
                (_pendingHandlingOffender < 0 && (_pendingHandlingPosition.x != 0f ||
                    _pendingHandlingPosition.y != 0f || _pendingHandlingPosition.z != 0f)))
                throw new InvalidOperationException("Corrupt goalkeeper handling-law state.");
        }

        private static bool IsLawAgentId(int id) => id >= MatchEngineConstants.NO_POSSESSION &&
            id < MatchEngineConstants.SQUAD_SIZE;

        private static bool IsFiniteLawPosition(Vector3 p) =>
            !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
            !float.IsNaN(p.z) && !float.IsInfinity(p.z);

        internal int TestOnly_ReleasedHandKeeper => _releasedHandKeeper;
        internal int TestOnly_IndirectKickTaker => _indirectKickTaker;
        internal bool TestOnly_TryKeeperHandContact(int agentId, Vector3 position) => TryKeeperHandContact(agentId, position);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0 | 2026-10-16 | — | Separate own-release re-handling law; canonical history and IFK restart. |
#endregion
