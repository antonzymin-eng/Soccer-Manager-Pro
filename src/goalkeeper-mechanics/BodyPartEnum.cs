// File:     src/goalkeeper-mechanics/BodyPartEnum.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.6.1, KD-14, Code Standards #20
// Purpose:  Body-part label for cross-claim / aerial duel contact resolution.
//           Determined by physical geometry, NOT by intent (FR-GK-022).

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Body-part label for the contact point in a cross-claim or aerial duel.
    /// Determined by physical geometry (capsule/sphere intersection) — never by intent (FR-GK-022).
    /// Drives duel routing: Hand → §3.5 handling pipeline; Head → Heading #10 §3.7 duel.
    /// Goalkeeper Mechanics #11 §3.6.1 / KD-14.
    /// </summary>
    public enum BodyPartEnum
    {
        /// <summary>Contact via hand/arm capsule collider. §3.6.1 / §3.6.2.</summary>
        Hand,

        /// <summary>Contact via head sphere collider. §3.6.2 → deferred to Heading #10 §3.7.</summary>
        Head,

        /// <summary>Contact via body collider (torso hit). §3.6.1.</summary>
        Body,

        /// <summary>Contact via foot (e.g. blocked shot). §3.6.1.</summary>
        Foot
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
