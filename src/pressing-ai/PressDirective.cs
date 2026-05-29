// File:     src/pressing-ai/PressDirective.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §4.2, Code Standards #20
// Purpose:  Output struct emitted each 10 Hz tick: primary presser EntityId,
//           up to two cover-shadow assignments, and active trigger flags.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Output produced by one Pressing AI tick. Carries the full press instruction set
    /// for the match orchestrator to relay to agents. Pressing AI #13 §4.2.
    /// Uses two explicit shadow fields (Shadow0 / Shadow1) to avoid array allocation
    /// on the hot path. MaxCoverShadows = 2 is enforced by construction.
    /// </summary>
    public struct PressDirective
    {
        /// <summary>EntityId of the agent assigned PrimaryPress role; -1 when no press is active.</summary>
        public int PrimaryPresserId;

        /// <summary>World-space (X, Y) projected interception target for the primary presser. §3.3.</summary>
        public Vector2 PrimaryTargetPosition;

        /// <summary>First cover-shadow assignment (valid when CoverShadowCount >= 1).</summary>
        public CoverShadow Shadow0;

        /// <summary>Second cover-shadow assignment (valid when CoverShadowCount == 2).</summary>
        public CoverShadow Shadow1;

        /// <summary>Number of active cover-shadow assignments [0, MaxCoverShadows]. §3.4.</summary>
        public int CoverShadowCount;

        /// <summary>Committed trigger flags that activated this directive. §3.2.</summary>
        public TriggerFlags ActiveTriggers;

        /// <summary>Returns an inactive (all-HOLD_SHAPE) directive.</summary>
        public static PressDirective Inactive => new PressDirective
        {
            PrimaryPresserId  = -1,
            PrimaryTargetPosition = Vector2.zero,
            Shadow0           = default,
            Shadow1           = default,
            CoverShadowCount  = 0,
            ActiveTriggers    = TriggerFlags.None,
        };

        /// <summary>Returns true when the directive has an active primary presser.</summary>
        public bool IsActive => PrimaryPresserId >= 0;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
