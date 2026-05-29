// File: src/positioning-ai/LaneId.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.4
// Purpose: Five-bin lateral lane classification enum.

namespace TacticalDirector.PositioningAI
{
    /// <summary>Five lateral lanes, each 13.6 m wide across the 68 m pitch width.</summary>
    public enum LaneId : byte
    {
        /// <summary>Left Wing  Y ∈ [0.0, 13.6).</summary>
        LW = 0,

        /// <summary>Left Half  Y ∈ [13.6, 27.2).</summary>
        LH = 1,

        /// <summary>Center     Y ∈ [27.2, 40.8).</summary>
        C  = 2,

        /// <summary>Right Half Y ∈ [40.8, 54.4).</summary>
        RH = 3,

        /// <summary>Right Wing Y ∈ [54.4, 68.0].</summary>
        RW = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
