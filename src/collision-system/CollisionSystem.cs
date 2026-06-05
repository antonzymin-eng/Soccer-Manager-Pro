// File:     src/collision-system/CollisionSystem.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.3]
// Author:   —
// Spec:     Collision System #3 §3.4.1, §4.1.3, §4.4.4, Code Standards #20
// Purpose:  Main collision system — orchestrates spatial hash, narrow phase, and response.
//           Call UpdateCollisions() once per 60 Hz frame after Agent Movement and Ball Physics.

using System;
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
        private readonly Vector3[] _pendingVelocityImpulse;
        private readonly Vector3[] _pendingPositionCorrection;
        private readonly bool[] _pendingGrounded;
        private readonly bool[] _pendingStumble;
        private readonly float[] _pendingImpactForce;

        // Per-frame agent physical snapshot cache — populated once per frame in the insert pass
        // and reused by the broad-phase and narrow-phase loops (avoids 3× recomputation per agent).
        private readonly AgentPhysicalProperties[] _snapshots;
        private readonly bool[] _snapshotValid;

        /// <summary>
        /// Constructs the system with pre-allocated buffers. Call once per match.
        /// </summary>
        /// <param name="agentCapacity">Number of agents. MUST equal SpatialHashConstants.AgentCapacity —
        /// the CollisionPairBitfield virtual-slot layout (ball mapped to BallVirtualIndex = AgentCapacity)
        /// and the 256-bit bitfield budget assume this binding.</param>
        /// <exception cref="ArgumentException">Thrown when agentCapacity does not match the bitfield slot layout.</exception>
        public CollisionSystem(int agentCapacity = 22) // default = SpatialHashConstants.AgentCapacity; literal required for default param
        {
            if (agentCapacity != SpatialHashConstants.AgentCapacity)
            {
                throw new ArgumentException(
                    $"agentCapacity ({agentCapacity}) must equal SpatialHashConstants.AgentCapacity " +
                    $"({SpatialHashConstants.AgentCapacity}); CollisionPairBitfield slot layout is wired " +
                    $"to BallVirtualIndex = AgentCapacity.",
                    nameof(agentCapacity));
            }
            if (SpatialHashConstants.BallVirtualIndex != SpatialHashConstants.AgentCapacity)
            {
                throw new InvalidOperationException(
                    $"SpatialHashConstants invariant violated: BallVirtualIndex " +
                    $"({SpatialHashConstants.BallVirtualIndex}) must equal AgentCapacity " +
                    $"({SpatialHashConstants.AgentCapacity}). See CollisionPairBitfield pair-index formula.");
            }

            _spatialHash = new SpatialHashGrid();
            _processedPairs = new CollisionPairBitfield();
            _eventBuffer = new CollisionEvent[SpatialHashConstants.MaxCollisionPairs];

            int n = agentCapacity;
            _pendingVelocityImpulse = new Vector3[n];
            _pendingPositionCorrection = new Vector3[n];
            _pendingGrounded = new bool[n];
            _pendingStumble = new bool[n];
            _pendingImpactForce = new float[n];
            _snapshots = new AgentPhysicalProperties[n];
            _snapshotValid = new bool[n];
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

            // AR-2 M-1/L-1: validate all per-agent input/output array lengths against the
            // ctor-fixed buffer capacity. The ctor guarantees _snapshots.Length == AgentCapacity;
            // an oversize agentStates would otherwise IOOR on _snapshotValid[i] with an opaque
            // diagnostic before the broad phase runs.
            if (count > _snapshots.Length
                || agentAttrs.Length < count
                || agentTeamIds.Length < count
                || agentIsGoalkeeper.Length < count
                || knockdownOut.Length < count
                || knockdownForceOut.Length < count
                || stumbleOut.Length < count)
            {
                throw new ArgumentException(
                    $"Per-agent array length mismatch. count={count}; capacity={_snapshots.Length}; " +
                    $"attrs={agentAttrs.Length}; teams={agentTeamIds.Length}; " +
                    $"gk={agentIsGoalkeeper.Length}; knockdown={knockdownOut.Length}; " +
                    $"knockdownForce={knockdownForceOut.Length}; stumble={stumbleOut.Length}.",
                    nameof(agentStates));
            }

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
                _snapshotValid[i] = false;
                knockdownOut[i] = false;
                knockdownForceOut[i] = 0f;
                stumbleOut[i] = false;
            }

            // Phase 1 — populate spatial hash and snapshot cache (single pass).
            _spatialHash.Clear();

            for (int i = 0; i < count; i++)
            {
                AgentPhysicalProperties snap =
                    AgentPhysicalProperties.From(in agentStates[i], in agentAttrs[i]);
                if (IsInvalidPosition(snap.Position)) continue;

                _snapshots[i] = snap;
                _snapshotValid[i] = true;
                _spatialHash.Insert(i, snap.Position, snap.HitboxRadius);
            }

            // Ball only contributes to the broad phase when it is within agent-reach height —
            // a high aerial ball cannot collide with any ground agent (CheckAgentBallCollision
            // would reject the narrow-phase test anyway; skipping insert avoids wasted query work).
            if (!IsInvalidPosition(ball.Position)
                && ball.Position.z <= CollisionPhysicsConstants.AgentReachHeight)
            {
                _spatialHash.Insert(SpatialHashConstants.BALL_ENTITY_ID, ball.Position,
                    SpatialHashConstants.BallRadius);
            }

            // Phases 2/3/4 — broad phase query, narrow phase, response.
            int pairs = 0;
            bool pairLimitExceeded = false;

            for (int i = 0; i < count && !pairLimitExceeded; i++)
            {
                if (!_snapshotValid[i]) continue;

                AgentPhysicalProperties snapI = _snapshots[i];
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
                        pairLimitExceeded = true;
                        break;
                    }

                    if (j == SpatialHashConstants.BALL_ENTITY_ID)
                    {
                        ProcessAgentBall(agentTeamIds, agentIsGoalkeeper, i, ref ball, matchTime);
                    }
                    else
                    {
                        ProcessAgentAgent(agentTeamIds, i, j, matchTime);
                    }
                }
            }

            // Apply accumulated impulses and corrections to agent states.
            for (int i = 0; i < count; i++)
            {
                if (_pendingVelocityImpulse[i].sqrMagnitude > CollisionPhysicsConstants.MinResponseSqrMagnitude)
                {
                    ref AgentState s = ref agentStates[i];
                    s.Velocity += new Vector2(
                        _pendingVelocityImpulse[i].x,
                        _pendingVelocityImpulse[i].y);
                }

                if (_pendingPositionCorrection[i].sqrMagnitude > CollisionPhysicsConstants.MinResponseSqrMagnitude)
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
            int[] teamIds,
            int id1,
            int id2,
            float matchTime)
        {
            AgentPhysicalProperties snap1 = _snapshots[id1];
            AgentPhysicalProperties snap2 = _snapshots[id2];

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
            int[] teamIds,
            bool[] isGoalkeeper,
            int agentId,
            ref BallState ball,
            float matchTime)
        {
            AgentPhysicalProperties snap = _snapshots[agentId];

            if (!CollisionDetection.CheckAgentBallCollision(in snap, in ball, out Vector3 contactPoint))
            {
                return;
            }

            // BodyPart hardcoded to Torso at Stage 0 — body-part classification is deferred until
            // BallCollisionHandler.OnAgentCollision is wired to BallPhysics.BallCollision.ApplyAgentContact
            // (see BallCollisionHandler.cs TODO; Collision System #3 §3.4.3 / Stage 0+1).
            var data = new AgentBallCollisionData
            {
                ContactPoint = contactPoint,
                AgentVelocity = snap.Velocity,
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

            // Sort so Entity1ID <= Entity2ID (BALL_ENTITY_ID = -1 sorts first); CollisionEvent
            // XML doc binds consumers to this ordering invariant.
            int lo = e1 <= e2 ? e1 : e2;
            int hi = e1 <= e2 ? e2 : e1;

            _eventBuffer[_eventCount++] = new CollisionEvent
            {
                MatchTime = matchTime,
                Type = type,
                Entity1ID = lo,
                Entity2ID = hi,
                ContactPoint = contactPoint,
                ImpactForce = impactForce,
                FoulData = foulData
            };
        }

        private static bool IsInvalidPosition(Vector3 p)
        {
            return !float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z);
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
// | 1.2     | 2026-05-25 | —      | Pass-3 fix. P3-1: 0.0001f literals replaced with                                        |
// |         |            |        | CollisionPhysicsConstants.MinResponseSqrMagnitude (FR-CS-016).                          |
// | 1.3     | 2026-06-05 | —      | AR-1 fix pass. H-1: ctor validates agentCapacity == SpatialHashConstants.AgentCapacity  |
// |         |            |        | (throws ArgumentException) and BallVirtualIndex == AgentCapacity invariant (L-6).       |
// |         |            |        | M-1: AgentPhysicalProperties[] snapshot cache populated once per frame; ProcessAgent*   |
// |         |            |        | read cached snapshots instead of recomputing per pair.                                  |
// |         |            |        | M-2: ball insert gated on ball.Position.z <= AgentReachHeight (aerial ball skips broad  |
// |         |            |        | phase since narrow phase would reject it).                                              |
// |         |            |        | L-3: RecordEvent sorts (e1, e2) so Entity1ID <= Entity2ID matches XML doc.              |
// |         |            |        | L-4: goto PublishEvents replaced with pairLimitExceeded bool + outer-loop guard.        |
// |         |            |        | L-5: ProcessAgentBall gains Stage 0 deferral comment beside hardcoded BodyPart.Torso.   |
// |         |            |        | IsInvalidPosition rewritten with !float.IsFinite (parity with SpatialHashGrid M-3).     |
// | 1.4     | 2026-06-05 | —      | AR-2 fix pass. M-1/L-1: UpdateCollisions validates count <= _snapshots.Length and every |
// |         |            |        | per-agent input/output array (attrs/teams/gk/knockdown/knockdownForce/stumble) is >=    |
// |         |            |        | count before iterating; oversize agentStates previously IOOR'd on _snapshotValid[i]     |
// |         |            |        | with an opaque diagnostic. Now throws ArgumentException naming all mismatched lengths.  |
#endregion
