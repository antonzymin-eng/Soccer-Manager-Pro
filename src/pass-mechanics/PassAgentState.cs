// File:     src/pass-mechanics/PassAgentState.cs
// Created:  2026-05-26
// Modified: 2026-06-11
// Author:   —
// Spec:     Pass Mechanics #5 §4.3.2, Code Standards #20
// Purpose:  PassAgentState struct: Pass Mechanics' snapshot of agent position,
//           velocity, and facing direction consumed from Agent Movement.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' snapshot of the agent state consumed from Agent Movement.
    /// Pass Mechanics #5 §4.3.2.
    /// </summary>
    public struct PassAgentState
    {
        /// <summary>
        /// World XY position of the agent (metres). Height (Z) is not represented —
        /// this is a Vector2; agent height is a Stage 1+ concern.
        /// Agent Movement §3.5.3. Coordinate system: X=pitch length, Y=pitch width (CLAUDE.md §Coordinate System).
        /// </summary>
        public Vector2 Position;

        /// <summary>Agent velocity in XY plane (m/s). Agent Movement §3.5.3.</summary>
        public Vector2 Velocity;

        /// <summary>Facing direction in XY plane (normalised unit vector). Agent Movement §3.5.3.</summary>
        public Vector2 FacingDirection;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-26 | —      | Extracted from IPassAgentQuery.cs per one-type-per-file rule (H5). |
// | 1.1     | 2026-06-11 | —      | AR-9 L-4 (doc-only): Position doc no longer claims a "Z component" |
// |         |            |        |     on a Vector2; height is recorded as a Stage 1+ concern.        |
#endregion
