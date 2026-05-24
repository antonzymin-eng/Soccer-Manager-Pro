// File:     src/Core/Physics/Ball/BallEventLogger.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Records ball events (kicks, bounces, goals, snapshots) for replay
//           reconstruction. Stage 0: unbounded list; ring buffer at Stage 1+.

using UnityEngine;
using System.Collections.Generic;

namespace TacticalDirector.BallPhysics
{
    /// <summary>Ball event types for replay and analytics logging.</summary>
    public enum BallEventType
    {
        POSITION_SNAPSHOT,
        KICK,
        HEADER,
        BOUNCE,
        DEFLECTION,
        GOAL_POST_HIT,
        OUT_OF_PLAY,
        GOAL,
        POSSESSION_CHANGE
    }

    /// <summary>Single ball event record stored by BallEventLogger.</summary>
    public struct BallEvent
    {
        public float        Timestamp;
        public BallEventType Type;
        public Vector3      Position;
        public Vector3      Velocity;
        public int          AgentID;
        public string       Detail;
    }

    /// <summary>
    /// Records ball events for replay reconstruction.
    /// Stage 0: unbounded List&lt;BallEvent&gt; — bounded by match duration, acceptable for
    /// single-player prototype. Ring buffer required at Stage 1+ (concurrent logging).
    /// </summary>
    public class BallEventLogger
    {
        private readonly List<BallEvent> _events = new List<BallEvent>();
        private float _lastSnapshotTime = -999f;

        /// <summary>Logs a position snapshot if the snapshot interval has elapsed.</summary>
        public void TryLogSnapshot(BallState ball, float matchTime)
        {
            if (matchTime - _lastSnapshotTime >= BallPhysicsConstants.Logging.SnapshotInterval)
            {
                _events.Add(new BallEvent
                {
                    Timestamp = matchTime,
                    Type      = BallEventType.POSITION_SNAPSHOT,
                    Position  = ball.Position,
                    Velocity  = ball.Velocity,
                    AgentID   = -1,
                    Detail    = ""
                });
                _lastSnapshotTime = matchTime;
            }
        }

        /// <summary>Logs a ground-contact bounce event.</summary>
        public void LogBounce(
            BallState ball,
            SurfaceType surface,
            float cor,
            float vnBefore,
            float vnAfter,
            float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp = matchTime,
                Type      = BallEventType.BOUNCE,
                Position  = ball.Position,
                Velocity  = ball.Velocity,
                AgentID   = -1,
                Detail    = $"Surface:{surface},CoR:{cor:F2},Vn:{vnBefore:F1}→{vnAfter:F1}"
            });
        }

        /// <summary>Logs a goal post or crossbar contact event.</summary>
        public void LogGoalPostHit(BallState ball, Vector3 contactPoint, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp = matchTime,
                Type      = BallEventType.GOAL_POST_HIT,
                Position  = ball.Position,
                Velocity  = ball.Velocity,
                AgentID   = -1,
                Detail    = $"Contact:({contactPoint.x:F1},{contactPoint.y:F1},{contactPoint.z:F1})"
            });
        }

        /// <summary>Logs a kick event from a specific agent.</summary>
        public void LogKick(BallState ball, int agentID, string kickType, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp = matchTime,
                Type      = BallEventType.KICK,
                Position  = ball.Position,
                Velocity  = ball.Velocity,
                AgentID   = agentID,
                Detail    = kickType
            });
        }

        /// <summary>Logs a goal scored event.</summary>
        public void LogGoal(BallState ball, int scorerID, int teamID, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp = matchTime,
                Type      = BallEventType.GOAL,
                Position  = ball.Position,
                Velocity  = ball.Velocity,
                AgentID   = scorerID,
                Detail    = $"Team:{teamID}"
            });
        }

        /// <summary>Returns a snapshot copy of all recorded events.</summary>
        public List<BallEvent> ExportEvents() => new List<BallEvent>(_events);

        /// <summary>Clears all recorded events and resets the snapshot timer.</summary>
        public void Clear()
        {
            _events.Clear();
            _lastSnapshotTime = -999f;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics;                |
// |         |            |        | SNAPSHOT_INTERVAL → SnapshotInterval; file header per FR-CS-056.   |
#endregion
