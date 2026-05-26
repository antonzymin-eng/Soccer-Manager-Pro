// File:     src/first-touch/FirstTouchResult.cs
// Created:  2026-05-25
// Modified: 2026-05-26
// Author:   —
// Spec:     First Touch Mechanics #4 §4.3.2, Code Standards #20
// Purpose:  Output struct returned by EvaluateFirstTouch containing all computed touch results.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// All per-touch output data produced by EvaluateFirstTouch. First Touch Mechanics #4 §4.3.2.
    /// Applied to simulation state by ApplyTouchResult.
    /// </summary>
    public struct FirstTouchResult
    {
        /// <summary>Computed control quality [0,1] before thunderbolt cap. §3.1.</summary>
        public float ControlQuality;

        /// <summary>Computed touch radius (m) for this touch. §3.2.</summary>
        public float TouchRadius;

        /// <summary>New ball world position after touch (m). §3.3.4.</summary>
        public Vector3 NewBallPosition;

        /// <summary>New ball world velocity after touch (m/s). §3.3.5.</summary>
        public Vector3 NewBallVelocity;

        /// <summary>Classified outcome of the touch attempt. §4.2.1.</summary>
        public TouchResult PossessionOutcome;

        /// <summary>Agent ID that gains possession; AGENT_ID_NONE if LOOSE_BALL or DEFLECTION. §4.3.2.</summary>
        public int PossessingAgentID;

        /// <summary>Agent ID of the intercepting opponent; AGENT_ID_NONE unless INTERCEPTION. §4.3.2.</summary>
        public int InterceptingAgentID;

        /// <summary>True if Agent Movement must activate DribblingModifier for PossessingAgentID. Only true when PossessionOutcome == CONTROLLED.</summary>
        public bool TriggeredDribblingState;

        /// <summary>Magnitude of ball velocity at moment of contact (m/s). Diagnostic field.</summary>
        public float IncomingBallSpeed;

        /// <summary>WeightedAttr × (1 + orientationBonus) × (1 - pressureScalar × PressureWeight) before difficulty division. Diagnostic field for designer tuning. §3.1.1 Steps 1–3.</summary>
        public float EffectiveAttribute;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                             |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                    |
// | 1.1     | 2026-05-26 | —      | M-2 fix: Renamed DisplacementRadius→TouchRadius, Outcome→PossessionOutcome; added TriggeredDribblingState, IncomingBallSpeed, EffectiveAttribute. |
#endregion
