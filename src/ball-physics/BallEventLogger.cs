// File:     src/ball-physics/BallEventLogger.cs
// Created:  2026-05-24
// Modified: 2026-06-02
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Records ball events (kicks, bounces, goals, snapshots) for replay
//           reconstruction. Stage 0: unbounded list; ring buffer at Stage 1+.

using System.Collections.Generic;

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
    /// <summary>Ball event types for replay and analytics logging.</summary>
    public enum BallEventType
    {
        PositionSnapshot,
        Kick,
        Header,
        Bounce,
        Deflection,
        GoalPostHit,
        OutOfPlay,
        Goal,
        PossessionChange
    }

    /// <summary>
    /// Single ball event record stored by BallEventLogger.
    /// Detail fields are typed (no Detail string) so logging stays zero-alloc on the
    /// 60 Hz hot path; only fields relevant to the event Type carry meaningful values.
    /// </summary>
    public struct BallEvent
    {
        public float         Timestamp;
        public BallEventType Type;
        public Vector3       Position;
        public Vector3       Velocity;
        public Vector3       AngularVelocity;
        public int           AgentID;

        public Vector3       ContactPoint;     // GoalPostHit
        public SurfaceType   Surface;          // Bounce
        public float         RestitutionUsed;  // Bounce
        public float         VnBefore;         // Bounce
        public float         VnAfter;          // Bounce
        public BallStateType ResultingState;   // Kick (post-kick state machine target)
        public int           TeamID;           // Goal
    }

    /// <summary>
    /// Records ball events for replay reconstruction.
    /// Stage 0: unbounded List&lt;BallEvent&gt; — bounded by match duration, acceptable for
    /// single-player prototype. Ring buffer required at Stage 1+ (concurrent logging).
    /// Hot-path discipline (FR-CS-031): no string formatting in any log method —
    /// structured fields on <see cref="BallEvent"/> carry all per-event detail and any
    /// diagnostic formatting is performed off the hot path via <see cref="FormatDetail"/>.
    /// </summary>
    public sealed class BallEventLogger
    {
        // -1f is "never snapshotted" — first call always emits, since (matchTime + 1f) >= SnapshotInterval.
        private const float NeverSnapshotted = -1f;

        private readonly List<BallEvent> _events = new List<BallEvent>();
        private float _lastSnapshotTime = NeverSnapshotted;

        /// <summary>Logs a position snapshot if the snapshot interval has elapsed.</summary>
        public void TryLogSnapshot(BallState ball, float matchTime)
        {
            if (_lastSnapshotTime == NeverSnapshotted
                || matchTime - _lastSnapshotTime >= BallPhysicsConstants.Logging.SnapshotInterval)
            {
                _events.Add(new BallEvent
                {
                    Timestamp       = matchTime,
                    Type            = BallEventType.PositionSnapshot,
                    Position        = ball.Position,
                    Velocity        = ball.Velocity,
                    AngularVelocity = ball.AngularVelocity,
                    AgentID         = -1
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
                Timestamp       = matchTime,
                Type            = BallEventType.Bounce,
                Position        = ball.Position,
                Velocity        = ball.Velocity,
                AngularVelocity = ball.AngularVelocity,
                AgentID         = -1,
                Surface         = surface,
                RestitutionUsed = cor,
                VnBefore        = vnBefore,
                VnAfter         = vnAfter
            });
        }

        /// <summary>Logs a goal post or crossbar contact event.</summary>
        public void LogGoalPostHit(BallState ball, Vector3 contactPoint, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp       = matchTime,
                Type            = BallEventType.GoalPostHit,
                Position        = ball.Position,
                Velocity        = ball.Velocity,
                AngularVelocity = ball.AngularVelocity,
                AgentID         = -1,
                ContactPoint    = contactPoint
            });
        }

        /// <summary>Logs a kick event from a specific agent.</summary>
        public void LogKick(BallState ball, int agentID, BallStateType resultingState, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp       = matchTime,
                Type            = BallEventType.Kick,
                Position        = ball.Position,
                Velocity        = ball.Velocity,
                AngularVelocity = ball.AngularVelocity,
                AgentID         = agentID,
                ResultingState  = resultingState
            });
        }

        /// <summary>Logs a goal scored event.</summary>
        public void LogGoal(BallState ball, int scorerID, int teamID, float matchTime)
        {
            _events.Add(new BallEvent
            {
                Timestamp       = matchTime,
                Type            = BallEventType.Goal,
                Position        = ball.Position,
                Velocity        = ball.Velocity,
                AngularVelocity = ball.AngularVelocity,
                AgentID         = scorerID,
                TeamID          = teamID
            });
        }

        /// <summary>
        /// Returns a snapshot copy of all recorded events.
        /// Allocates a new <see cref="List{BallEvent}"/> per call — invoke off the hot
        /// path only (match end, debug export). NOT to be called from
        /// <see cref="BallPhysicsCore.UpdateBallPhysics"/> or any 60 Hz consumer.
        /// </summary>
        public List<BallEvent> ExportEvents() => new List<BallEvent>(_events);

        /// <summary>Total event count without copying. Safe to call anywhere.</summary>
        public int Count => _events.Count;

        /// <summary>Clears all recorded events and resets the snapshot timer.</summary>
        public void Clear()
        {
            _events.Clear();
            _lastSnapshotTime = NeverSnapshotted;
        }

        /// <summary>
        /// Formats a single event's typed detail fields as a diagnostic string.
        /// Off-hot-path utility — allocates a string per call; do not invoke from
        /// the 60 Hz loop.
        /// </summary>
        public static string FormatDetail(in BallEvent evt)
        {
            return evt.Type switch
            {
                BallEventType.Bounce =>
                    $"Surface:{evt.Surface},CoR:{evt.RestitutionUsed:F2},Vn:{evt.VnBefore:F1}→{evt.VnAfter:F1}",
                BallEventType.Kick =>
                    $"ApplyKick|v={evt.Velocity.magnitude:F1}m/s|s={evt.AngularVelocity.magnitude:F1}rad/s|→{evt.ResultingState}",
                BallEventType.GoalPostHit =>
                    $"Contact:({evt.ContactPoint.x:F1},{evt.ContactPoint.y:F1},{evt.ContactPoint.z:F1})",
                BallEventType.Goal => $"Team:{evt.TeamID}",
                _ => string.Empty
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics;                |
// |         |            |        | SNAPSHOT_INTERVAL → SnapshotInterval; file header per FR-CS-056.   |
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-1: per-event string interpolation in LogBounce /     |
// |         |            |        | LogKick / LogGoalPostHit removed — BallEvent gains typed Surface / |
// |         |            |        | RestitutionUsed / VnBefore / VnAfter / ContactPoint /              |
// |         |            |        | ResultingState / TeamID / AngularVelocity fields so the 60 Hz hot  |
// |         |            |        | path no longer allocates strings per call (FR-CS-031). LogKick     |
// |         |            |        | signature: string kickType → BallStateType resultingState.         |
// |         |            |        | FormatDetail() is an off-hot-path helper for diagnostic export.    |
// |         |            |        | H-2: file header path corrected to src/ball-physics/. M-1: using   |
// |         |            |        | order System → UnityEngine (FR-CS-006). M-4: BallEventType members |
// |         |            |        | renamed to PascalCase. M-6: class sealed (FR-CS-068). M-7: -999f   |
// |         |            |        | magic literal replaced with the NeverSnapshotted = -1f named const |
// |         |            |        | and an explicit "first call always emits" sentinel check.          |
// |         |            |        | L-3: ExportEvents XML doc warns about per-call List allocation.    |
#endregion
