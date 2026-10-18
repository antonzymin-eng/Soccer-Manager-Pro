// File:     src/match-engine/MatchEngine.KeeperHandlingLaw.cs
// Created:  2026-10-16
// Modified: 2026-10-18
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §§3.1.3–3.1.4; match-flow §§5.1–5.2; IFAB Laws 12 / 13.
// Purpose:  Enforce own-release re-handling and outside-area hand contacts or continuous carries.

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

        // Actual #11 catch/parry/deflection/spill contacts and continuous carries share this gate.
        // Queue the offence; ball mutation and its Tier A restart occur only in Resolve.
        private bool TryKeeperHandContact(int agentId, Vector3 position)
        {
            if (_pendingHandlingOffender >= 0) return false;
            bool restricted = _releasedHandKeeper == agentId;
            if (restricted || !IsInsideOwnPenaltyArea(_teamIds[agentId], position))
            {
                if (restricted) ObserveW8(W8StageAKind.RestrictedHandContact, agentId);
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

        private static bool IsInsideOwnPenaltyArea(int team, Vector3 position)
        {
            float depth = Mathf.Abs(position.x - OwnGoalX(team));
            float lateral = Mathf.Abs(position.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f);
            return position.x >= 0f && position.x <= MatchEngineConstants.PITCH_LENGTH_M &&
                depth <= GoalkeeperConstants.PenaltyAreaDepthM &&
                lateral <= GoalkeeperConstants.PenaltyAreaDepthM + BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f;
        }

        // The contact gate handles catches/parries; a held ball remains a hand contact as it moves.
        // Use the settled ball position, including Resolve's collision correction of its carrier.
        private void CheckKeeperHandCarry()
        {
            int holder = _possessingAgentId;
            if (holder >= 0 && _ball.State == BallStateType.Controlled && HasGoalkeeperHandControl(holder) &&
                !IsInsideOwnPenaltyArea(_teamIds[holder], _ball.Position))
                TryKeeperHandContact(holder, _ball.Position);
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
            bool inside = IsInsideOwnPenaltyArea(team, contact);
            Vector2 spot = new Vector2(contact.x, contact.y);
            // Law 13: an attacking IFK inside the goal area is taken on its parallel boundary.
            if (inside && depth < MatchEngineConstants.GOAL_AREA_DEPTH_M &&
                lateral <= MatchEngineConstants.GOAL_AREA_DEPTH_M + halfGoal)
                spot.x = ownX + AttackDirectionX(team) * MatchEngineConstants.GOAL_AREA_DEPTH_M;

            // The handling whistle wins this frame. A captured AI-phase tackle candidate must
            // not survive into a second restart/card after play has already restarted.
            _foulCandidateFound = false;
            _foulCandidateIsDecided = false;
            ApplyRestart(spot, 1 - team, RestartCue.FreeKick);
            _gkHoldTicks = 0;
            _gkReleasedAgentId = MatchEngineConstants.NO_POSSESSION;
            _gkReleaseCooldownRemaining = 0;
            if (inside) _indirectKickTaker = _possessingAgentId;
            var evt = new RestartAwardedEvent(
                inside ? RestartAwardedEvent.INDIRECT_FREE_KICK : RestartAwardedEvent.DIRECT_FREE_KICK,
                (byte)(1 - team), new Vector3(spot.x, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
            EventBus.Publish(in evt);
            // Both Physics carries and Resolve collision corrections must end the frame before
            // any stale executor can release the ball or another captured foul can act.
            Array.Clear(_isCollisionKnockdown, 0, _isCollisionKnockdown.Length);
            Array.Clear(_collisionForces, 0, _collisionForces.Length);
            Array.Clear(_stumbleScratch, 0, _stumbleScratch.Length);
            UpdateMatchContext();
            if (_possessingAgentId >= 0) _lastHolderAgentId = _possessingAgentId;
            PublishPossessionChangeIfChanged();
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
                (_pendingHandlingOffender >= 0 &&
                    (!_isGoalkeeper[_pendingHandlingOffender] ||
                        (_pendingHandlingOffender != _releasedHandKeeper &&
                            IsInsideOwnPenaltyArea(_teamIds[_pendingHandlingOffender], _pendingHandlingPosition)))) ||
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
// | 1.1 | 2026-10-16 | — | ERR-011-020: feet-only avoidance, contact/miss and foul-order regression coverage; observational law census. |
// | 1.2 | 2026-10-18 | — | D-07 option A: general outside-area contacts/carries, shared geometry and pending-state validation. |
#endregion
