// File:     src/first-touch/PressureResult.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §3.5, Code Standards #20
// Purpose:  Output struct returned by PressureEvaluator summarising nearby-opponent pressure.

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

        /// <summary>Distance to the nearest opponent (m); positive infinity when none present. §3.5.</summary>
        public float NearestOpponentDistance;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
