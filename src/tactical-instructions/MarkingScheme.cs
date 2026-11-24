// File:     src/tactical-instructions/MarkingScheme.cs
// Created:  2026-11-23
// Modified: 2026-11-23
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.1, Defensive AI #14 §3.13.1
// Purpose:  Stable team marking scheme ordinals for configuration and snapshots.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>The defensive movement scheme selected by a team.</summary>
    public enum MarkingScheme : byte
    {
        /// <summary>Use the standard marking movement and limits.</summary>
        Mixed = 0,
        /// <summary>Keep zonal movement and legacy tackle assignments.</summary>
        Zonal = 1,
        /// <summary>Use marking movement with the man-oriented invariant limits.</summary>
        ManOriented = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-11-23 | —      | Extracted the existing W13.2 scheme ordinals.       |
#endregion
