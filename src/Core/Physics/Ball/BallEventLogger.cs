using UnityEngine;
using System.Collections.Generic;

namespace TacticalDirector.Core.Physics.Ball
{
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

        public void TryLogSnapshot(BallState ball, float matchTime)
        {
            if (matchTime - _lastSnapshotTime >= BallPhysicsConstants.Logging.SNAPSHOT_INTERVAL)
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

        public List<BallEvent> ExportEvents() => new List<BallEvent>(_events);

        public void Clear()
        {
            _events.Clear();
            _lastSnapshotTime = -999f;
        }
    }
}
