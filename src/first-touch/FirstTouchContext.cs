// File:     src/first-touch/FirstTouchContext.cs
// Created:  2026-05-25
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §4.3.1, Code Standards #20
// Purpose:  Input context struct containing all per-touch data needed by the first-touch pipeline.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// All per-touch input data consumed by EvaluateFirstTouch. First Touch Mechanics #4 §4.3.1.
    /// Passed by value (read-only snapshot for one touch event).
    /// </summary>
    public struct FirstTouchContext
    {
        /// <summary>ID of the agent attempting the touch.</summary>
        public int AgentID;

        /// <summary>Team identifier for the receiving agent. 0 = home, 1 = away.</summary>
        public int TeamID;

        /// <summary>Technique attribute of the agent [1–20].</summary>
        public int Technique;

        /// <summary>FirstTouch attribute of the agent [1–20].</summary>
        public int FirstTouchAttribute;

        /// <summary>World position of the agent at the moment of touch (m).</summary>
        public Vector3 AgentPosition;

        /// <summary>World velocity of the agent at the moment of touch (m/s).</summary>
        public Vector3 AgentVelocity;

        /// <summary>Facing direction of the agent; unit vector in XY plane (Z = 0).</summary>
        public Vector3 AgentFacing;

        /// <summary>Intended touch direction; unit vector in XY plane (Z = 0). Defaults to AgentFacing when HasMovementTarget is false.</summary>
        public Vector3 IntendedTouchDirection;

        /// <summary>True if the agent has an active movement target at time of contact.</summary>
        public bool HasMovementTarget;

        /// <summary>World position of the ball at the moment of touch (m).</summary>
        public Vector3 BallPosition;

        /// <summary>World velocity of the ball at the moment of touch (m/s).</summary>
        public Vector3 BallVelocity;

        /// <summary>Ball height above pitch surface (ball centre Z minus Ball.RADIUS). Typically 0 for ground balls.</summary>
        public float BallHeight;

        /// <summary>True if the ball was in AIRBORNE state at contact.</summary>
        public bool BallIsAirborne;

        /// <summary>Pre-computed pressure scalar [0,1] from PressureEvaluator for this agent's position.</summary>
        public float PressureScalar;

        /// <summary>True when the nearest opponent is within PressureRadius. Informational at Stage 0 —
        /// the §3.4.2 interception gate is anchored at the displaced ball position via
        /// NearestOpponentPositionXY (ERR-004-004), not this agent-anchored flag.</summary>
        public bool HasNearbyOpponent;

        /// <summary>Distance from the agent to the nearest opponent over ALL opponents (m); +inf when none present. Validity gate for NearestOpponentPositionXY.
        /// DEFAULT-VALUE BYPASS (AR-9 L-1): a default-constructed context carries 0 here — finite, i.e. "opponent present at (0,0)".
        /// Producers MUST populate this pair from PressureEvaluator (or set +inf explicitly); treat default-valued contexts as malformed.</summary>
        public float NearestOpponentDistance;

        /// <summary>XY world position of the nearest opponent (m). Only meaningful when NearestOpponentDistance is finite.
        /// Consumed by the §3.4.2 ball-anchored interception check and the §3.4.5 interception velocity redirect.</summary>
        public Vector2 NearestOpponentPositionXY;

        /// <summary>True when OrientationDetector determined the agent is half-turn oriented relative to ball travel. §3.6.</summary>
        public bool IsHalfTurnOriented;

        /// <summary>True if the receiving agent is a goalkeeper. Informational only at Stage 0.</summary>
        public bool IsGoalkeeper;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                                  |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                         |
// | 1.1     | 2026-05-26 | —      | M-1 fix: Added TeamID, BallHeight, BallIsAirborne, HasMovementTarget, IsGoalkeeper; renamed FirstTouchAttr→FirstTouchAttribute, IntendedDirection→IntendedTouchDirection; AgentFacing Vector2→Vector3; IntendedTouchDirection Vector2→Vector3. |
// | 1.2     | 2026-06-10 | —      | AR-7 M-1 (ERR-004-004): NearestOpponentPositionXY added (validity gated on finite NearestOpponentDistance); HasNearbyOpponent doc-noted as informational (interception gate is ball-anchored); NearestOpponentDistance doc updated to global-nearest semantics. |
// | 1.3     | 2026-06-10 | —      | AR-9 L-1 (doc-only): default-value bypass note on NearestOpponentDistance — struct default 0 is finite and reads as an opponent at (0,0); producers must populate the pair or set +inf (parallels #19 AR-2 L-6 default-bypass notes). |
#endregion
