// File:     src/first-touch/PressureResult.cs
// Created:  2026-05-25
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §3.5, Code Standards #20
// Purpose:  Output struct returned by PressureEvaluator summarising nearby-opponent pressure.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Aggregated pressure data for one agent's position at one touch evaluation.
    /// Produced by PressureEvaluator.Evaluate. First Touch Mechanics #4 §3.5.
    /// </summary>
    public struct PressureResult
    {
        /// <summary>Summed and clamped pressure scalar [0,1]. §3.5.3.</summary>
        public float PressureScalar;

        /// <summary>True when at least one opponent is within PressureRadius. §3.5.</summary>
        public bool HasNearbyOpponent;

        /// <summary>Distance to the nearest opponent over ALL supplied opponents (m), not truncated at PressureRadius; positive infinity when none present. §3.5, §3.4.2.</summary>
        public float NearestOpponentDistance;

        /// <summary>XY world position of the nearest opponent (m). Only meaningful when NearestOpponentDistance is finite. Consumed by the §3.4.2 ball-anchored interception check and the §3.4.5 interception velocity redirect.</summary>
        public Vector2 NearestOpponentPositionXY;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
// | 1.1     | 2026-06-10 | —      | AR-7 M-1 (ERR-004-004): NearestOpponentPositionXY added; NearestOpponentDistance semantics widened to global nearest (no PressureRadius truncation). |
#endregion
