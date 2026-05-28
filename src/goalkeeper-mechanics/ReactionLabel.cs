// File:     src/goalkeeper-mechanics/ReactionLabel.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.2, KD-2, Code Standards #20
// Purpose:  Telemetry-only label for the §3.2.4 reaction-window bucket assignment.
//           Physics NEVER branches on this label (KD-2).

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Telemetry label for GK shot-reaction window quality bucket.
    /// Assigned from reactionWindowAchieved thresholds; never consumed by physics formulas (KD-2).
    /// Goalkeeper Mechanics #11 §3.2.4.
    /// </summary>
    public enum ReactionLabel
    {
        /// <summary>reactionWindowAchieved &gt; REFLEXIVE_LABEL_THRESHOLD. §3.2.4.</summary>
        Reflexive,

        /// <summary>reactionWindowAchieved in [SLUGGISH_LABEL_THRESHOLD, REFLEXIVE_LABEL_THRESHOLD]. §3.2.4.</summary>
        Standard,

        /// <summary>reactionWindowAchieved &lt; SLUGGISH_LABEL_THRESHOLD. §3.2.4.</summary>
        Sluggish
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
