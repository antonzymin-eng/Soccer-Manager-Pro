// File:     src/agent-movement/DecelerationMode.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.5.2, Code Standards #20
// Purpose:  Deceleration braking profile supplied by the AI command layer.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Deceleration mode supplied by the AI command layer (§3.5.2).
    /// </summary>
    public enum DecelerationMode
    {
        /// <summary>Controlled stop over 3.0–5.0 m (default).</summary>
        CONTROLLED,

        /// <summary>Emergency stop over 2.5–3.5 m; higher stumble risk.</summary>
        EMERGENCY
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-25 | —      | Extracted from AgentMovementState.cs (one-public-type-per-file, FR-CS rule).       |
#endregion
