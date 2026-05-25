// File:     src/collision-system/CollisionSystem.cs
// Created:  2026-05-25
// Modified: 2026-05-25  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.4.1, §4.1.3, §4.4.4, Code Standards #20
// Purpose:  Main collision system — orchestrates spatial hash, narrow phase, and response.
//           Call UpdateCollisions() once per 60 Hz frame after Agent Movement and Ball Physics.

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Frame orchestrator for collision detection and response.
    /// Zero heap allocations per frame after construction. Collision System #3 §3.4.1.
    /// Not thread-safe; single-threaded execution only.
    /// </summary>
    public sealed class CollisionSystem
    {
        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("CollisionSystem.Update");

        private readonly SpatialHashGrid _spatialHash;
        private CollisionPairBitfield _processedPairs;
        private DeterministicRNG _rng;

        private readonly CollisionEvent[] _eventBuffer;
        private int _eventCount;

        // Parallel output arrays — caller allocates once and passes each frame.
        // Indexed by agent index (0–21).
        private Vector3[] _pendingVelocityImpulse;
        private Vector3[] _pendingPositionCorrection;
        private bool[] _pendingGrounded;
        private bool[] _pendingStumble;
        private float[] _pendingImpactForce;

        /// <summary>
        /// Constructs the system with pre-allocated buffers. Call once per match.
        /// </summary>
        /// <param name="agentCapacity">Number of agents (typically 22).</param>
        public CollisionSystem(int agentCapacity = 22)
        {
            _spatialHash = new SpatialHashGrid();
            _processedPairs = new CollisionPairBitfield();
            _eventBuffer = new CollisionEvent[SpatialHashConstants.MaxCollisionPairs];

            int n = agentCapacity;
            _pendingVelocityImpulse = new Vector3[n];
            _pendingPositionCorrection = new Vector3[n];
            _pendingGrounded = new bool[n];
            _pendingStumble = new bool[n];
            _pendingImpactForce = new float[n];
        }

        /// <summary>
        /// Performs broad phase, narrow phase, response, and event publishing for one frame.
        /// Modifies agentStates (velocity, position) and collision output arrays in place.
        /// </summary>
        /// <param name="agentStates">In/out: agent kinematic state. Velocity and Position modified.</param>
        /// <param name="agentAttrs">Read-only agent attributes (Strength, Agility).</param>
        /// <param name="agentTeamIds">Read-only team identifiers per agent.</param>
        /// <param name="agentIsGoalkeeper">Read-only goalkeeper flags per agent.</param>
        /// <param name="knockdownOut">Out: true when collision triggers GROUNDED. Consumed by AgentMovementSystem.UpdateAllAgents().</param>
        /// <param name="knockdownForceOut">Out: normalised impact force [0,1] for GROUNDED dwell scaling. Consumed by AgentMovementSystem.</param>
        /// <param name="stumbleOut">Out: true when collision triggers STUMBLING (not GROUNDED). Consumed by AgentMovementSystem.UpdateAllAgents().</param>
        /// <param name="ball">In/out: modified when agent-ball contact is detected.</param>
        /// <param name="matchSeed">Match-level seed for deterministic RNG.</param>
        /// <param name="frameNumber">Current frame index for per-frame RNG seeding.</param>
        /// <param name="matchTime">Current match time (s) for event timestamps.</param>
        /// <param name="eventConsumer">Receives one CollisionEvent per confirmed collision.</param>
        public void UpdateCollisions(
            AgentState[] agentStates,
            PlayerAttributes[] agentAttrs,
            int[] agentTeamIds,
            bool[] agentIsGoalkeeper,
            bool[] knockdownOut,
            float[] knockdownForceOut,
            bool[] stumbleOut,
            ref BallState ball,
            ulong matchSeed,
            int frameNumber,
            float matchTime,
            ICollisionEventConsumer eventConsumer)
        {
            using var _ = s_updateMarker.Auto();

            int count = agentStates.Length;

            // Frame seed: XOR match seed with frame index in both halves for better distribution.
            ulong frameSeed = matchSeed ^ (ulong)frameNumber ^ ((ulong)frameNumber << 32);
            _rng = new DeterministicRNG(frameSeed);
            _processedPairs.Clear();
            _eventCount = 0;

            // Clear per-frame output arrays.
            for (int i = 0; i < count; i++)
            {
                _pendingVelocityImpulse[i] = Vector3.zero;
                _pendingPositionCorrection[i] = Vector3.zero;
                _pendingGrounded[i] = false;
                _pendingStumble[i] = false;
                _pendingImpactForce[i] = 0f;
                knockdownOut[i] = false;
                knockdownForceOut[i] = 0f;
                stumbleOut[i] = false;
            }

            // Phase 1 — populate spatial hash.
            _spatialHash.Clear();

            for (int i = 0; i < count; i++)
            {
                var snap = AgentPhysicalProperties.From(in agentStates[i], in agentAttrs[i]);
                if (IsInvalidPosition(snap.Position)) continue;
                _spatialHash.Insert(i, snap.Position, snap.HitboxRadius);
            }

            if (!IsInvalidPosition(ball.Position))
            {
                _spatialHash.Insert(SpatialHashConstants.BALL_ENTITY_ID, ball.Position,
                    SpatialHashConstants.BallRadius);
            }

            // Phases 2/3/4 — broad phase query, narrow phase, response.
            int pairs = 0;

            for (int i = 0; i < count; i++)
            {
                var snapI = AgentPhysicalProperties.From(in agentStates[i], in agentAttrs[i]);
                if (IsInvalidPosition(snapI.Position)) continue;

                List<int> nearby = _spatialHash.Query(snapI.Position, snapI.HitboxRadius);

                for (int k = 0; k < nearby.Count; k++)
                {
                    int j = nearby[k];
                    if (j == i) continue;

                    int lo = j < i ? j : i;
                    int hi = j < i ? i : j;

                    if (_processedPairs.IsSet(lo, hi)) continue;
                    _processedPairs.Set(lo, hi);

                    if (++pairs > SpatialHashConstants.MaxCollisionPairs)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning($"[CollisionSystem] MaxCollisionPairs ({SpatialHashConstants.MaxCollisionPairs}) exceeded");
#endif
                        goto PublishEvents;
                    }

                    if (j == SpatialHashConstants.BALL_ENTITY_ID)
                    {
                        ProcessAgentBall(agentStates, agentAttrs, agentTeamIds,
                            agentIsGoalkeeper, i, ref ball, matchTime);
                    }
                    else
                    {
                        ProcessAgentAgent(agentStates, agentAttrs, agentTeamIds, i, j, matchTime);
                    }
                }
            }

            PublishEvents:

            // Apply accumulated impulses and corrections to agent states.
            for (int i = 0; i < count; i++)
            {
                if (_pendingVelocityImpulse[i].sqrMagnitude > 0.0001f)
                {
                    ref AgentState s = ref agentStates[i];
                    s.Velocity += new Vector2(
                        _pendingVelocityImpulse[i].x,
                        _pendingVelocityImpulse[i].y);
                }

                if (_pendingPositionCorrection[i].sqrMagnitude > 0.0001f)
                {
                    ref AgentState s = ref agentStates[i];
                    s.Position += new Vector2(
                        _pendingPositionCorrection[i].x,
                        _pendingPositionCorrection[i].y);
                }

                if (_pendingGrounded[i])
                {
                    knockdownOut[i] = true;
                    knockdownForceOut[i] = Mathf.Clamp01(
                        _pendingImpactForce[i] / CollisionPhysicsConstants.MaxCollisionForceRef);
                }
                else if (_pendingStumble[i])
                {
                    stumbleOut[i] = true;
                }
            }

            // Publish events.
            for (int i = 0; i < _eventCount; i++)
            {
                eventConsumer?.OnCollisionEvent(_eventBuffer[i]);
            }
        }

        private void ProcessAgentAgent(
            AgentState[] states,
            PlayerAttributes[] attrs,
            int[] teamIds,
            int id1,
            int id2,
            float matchTime)
        {
            var snap1 = AgentPhysicalProperties.From(in states[id1], in attrs[id1]);
            var snap2 = AgentPhysicalProperties.From(in states[id2], in attrs[id2]);

            if (!CollisionDetection.CheckAgentAgentCollision(in snap1, in snap2, out CollisionManifold manifold))
            {
                return;
            }

            manifold.Entity1ID = id1;
            manifold.Entity2ID = id2;

            bool sameTeam = teamIds[id1] == teamIds[id2];

            AgentAgentCollisionResult response = CollisionResponse.CalculateAgentAgentResponse(
                in snap1, in snap2, in manifold, sameTeam, ref _rng);

            _pendingVelocityImpulse[id1] += response.VelocityImpulse1;
            _pendingVelocityImpulse[id2] += response.VelocityImpulse2;
            _pendingPositionCorrection[id1] += response.PositionCorrection1;
            _pendingPositionCorrection[id2] += response.PositionCorrection2;

            if (response.TriggerGrounded1)
            {
                _pendingGrounded[id1] = true;
                // Always keep the highest impact force across multiple same-frame collisions.
                if (response.ImpactForce > _pendingImpactForce[id1])
                {
                    _pendingImpactForce[id1] = response.ImpactForce;
                }
            }

            if (response.TriggerGrounded2)
            {
                _pendingGrounded[id2] = true;
                if (response.ImpactForce > _pendingImpactForce[id2])
                {
                    _pendingImpactForce[id2] = response.ImpactForce;
                }
            }

            if (!_pendingGrounded[id1] && response.TriggerStumble1)
            {
                _pendingStumble[id1] = true;
            }

            if (!_pendingGrounded[id2] && response.TriggerStumble2)
            {
                _pendingStumble[id2] = true;
            }

            ContactTypeClassifier.DetermineInstigatorAndVictim(
                in snap1, in snap2, manifold.Normal,
                out int instigatorIdx, out int victimIdx);

            int instigatorId = instigatorIdx == 0 ? id1 : id2;
            int victimId     = instigatorIdx == 0 ? id2 : id1;

            var foulSnapInstigator = instigatorIdx == 0 ? snap1 : snap2;
            var foulSnapVictim     = instigatorIdx == 0 ? snap2 : snap1;

            var foulData = new ContactForceData
            {
                ForceMagnitude = response.ImpactForce,
                ForceDirection = new Vector3(manifold.Normal.x, manifold.Normal.y, 0f),
                Type = ContactTypeClassifier.Classify(
                    in foulSnapInstigator, in foulSnapVictim, manifold.Normal),
                InstigatorAgentID = instigatorId,
                VictimAgentID = victimId,
                VictimHasBall = false,
                InstigatorPlayingBall = false
            };

            RecordEvent(matchTime, CollisionType.AGENT_AGENT, id1, id2,
                new Vector3(manifold.ContactPoint.x, manifold.ContactPoint.y, 0f),
                response.ImpactForce, foulData);
        }

        private void ProcessAgentBall(
            AgentState[] states,
            PlayerAttributes[] attrs,
            int[] teamIds,
            bool[] isGoalkeeper,
            int agentId,
            ref BallState ball,
            float matchTime)
        {
            var snap = AgentPhysicalProperties.From(in states[agentId], in attrs[agentId]);

            if (!CollisionDetection.CheckAgentBallCollision(in snap, in ball, out Vector3 contactPoint))
            {
                return;
            }

            var data = new AgentBallCollisionData
            {
                ContactPoint = contactPoint,
                AgentVelocity = new Vector3(states[agentId].Velocity.x,
                                            states[agentId].Velocity.y, 0f),
                BodyPart = BallPhysics.BodyPart.Torso,
                AgentID = agentId,
                TeamID = teamIds[agentId],
                IsGoalkeeper = isGoalkeeper[agentId]
            };

            BallCollisionHandler.OnAgentCollision(ref ball, in data);

            RecordEvent(matchTime, CollisionType.AGENT_BALL, agentId,
                SpatialHashConstants.BALL_ENTITY_ID, contactPoint, 0f, default);
        }

        private void RecordEvent(
            float matchTime,
            CollisionType type,
            int e1, int e2,
            Vector3 contactPoint,
            float impactForce,
            ContactForceData foulData)
        {
            if (_eventCount >= SpatialHashConstants.MaxCollisionPairs) return;

            _eventBuffer[_eventCount++] = new CollisionEvent
            {
                MatchTime = matchTime,
                Type = type,
                Entity1ID = e1,
                Entity2ID = e2,
                ContactPoint = contactPoint,
                ImpactForce = impactForce,
                FoulData = foulData
            };
        }

        private static bool IsInvalidPosition(Vector3 p)
        {
            return float.IsNaN(p.x) || float.IsInfinity(p.x)
                || float.IsNaN(p.y) || float.IsInfinity(p.y)
                || float.IsNaN(p.z) || float.IsInfinity(p.z);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                    |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                           |
// | 1.1     | 2026-05-25 | —      | Adversarial review fix pass. H-1: stumbleOut[] param added; _pendingStumble now surfaced.|
// |         |            |        | H-2: _pendingGroundedDuration[] replaced with _pendingImpactForce[]; knockdownForceOut   |
// |         |            |        | now stores normalised impact force via MaxCollisionForceRef (not normalised duration).   |
// |         |            |        | ProcessAgentAgent: accumulate highest ImpactForce per agent across multiple collisions.  |
#endregion
