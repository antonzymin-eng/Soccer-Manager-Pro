// File: src/positioning-ai/FormationFamily.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §2.1 FR-PA-007, §6.1
// Purpose: Three Stage-0 formation archetype identifiers.

namespace TacticalDirector.PositioningAI
{
    /// <summary>Stage-0 formation archetypes. Stage-1 expands to 10 named variants per §7.6.</summary>
    public enum FormationFamily : byte
    {
        /// <summary>4-4-2 Flat. lineCutIndices = (4, 8). Defense=4 / Midfield=4 / Attack=2.</summary>
        F442  = 0,

        /// <summary>4-3-3. lineCutIndices = (4, 7). Defense=4 / Midfield=3 / Attack=3.</summary>
        F433  = 1,

        /// <summary>4-2-3-1. lineCutIndices = (4, 9). Defense=4 / Midfield=5 / Attack=1.</summary>
        F4231 = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
