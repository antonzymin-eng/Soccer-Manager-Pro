// File:     src/attacking-ai/AttackDirective.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.1, §3.8, §3.9, FR-AT-036, Code Standards #20
// Purpose:  Team-level per-tick attacking output. One instance per possessing team per tick.
//           overloadFlank is undefined when OverloadActive is false; consumers must not read it.

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Team-level attacking coordination output for one 10 Hz tick. Written by
    /// <see cref="AttackingAITick"/>; read by the match orchestrator.
    /// Contributes to the per-tick determinism digest (FR-AT-004). Attacking AI #15 §2.2.1.
    /// </summary>
    public readonly struct AttackDirective
    {
        /// <summary>EntityId of the team this directive applies to.</summary>
        public int  TeamId             { get; }

        /// <summary>
        /// True when the overload condition (FR-AT-016 / §3.8) is met: ≥ OVERLOAD_COUNT
        /// non-WEAK_SIDE agents within the OVERLOAD_ZONE_WIDTH_M Y-corridor.
        /// </summary>
        public bool OverloadActive     { get; }

        /// <summary>
        /// Which lateral half the overload is on. Meaningful ONLY when
        /// <see cref="OverloadActive"/> is true (§2.2.1).
        /// </summary>
        public Flank OverloadFlank     { get; }

        /// <summary>
        /// Countdown ticks remaining on the possession-loss freeze (§3.9 / §2.2.1).
        /// 0 when IN_POSSESSION or countdown expired.
        /// </summary>
        public int  TransitionHoldTick { get; }

        /// <summary>Constructs an AttackDirective.</summary>
        public AttackDirective(int teamId, bool overloadActive, Flank overloadFlank, int transitionHoldTick)
        {
            TeamId             = teamId;
            OverloadActive     = overloadActive;
            OverloadFlank      = overloadFlank;
            TransitionHoldTick = transitionHoldTick;
        }

        /// <summary>
        /// Empty directive: no overload, no transition hold. Emitted when OUT_OF_POSSESSION,
        /// when TRANSITION countdown expires, or on failure modes F2/F3/F4.
        /// </summary>
        public static AttackDirective Empty => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
