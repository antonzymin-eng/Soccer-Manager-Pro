// File:     src/attacking-ai/AttackPoolEntry.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.2–§3.11, Code Standards #20
// Purpose:  Internal per-agent scratch entry used during one tick's pool construction and
//           role-assignment pipeline. Not part of the public output; mutated in-place by
//           RoleAssigner, WidthHolder, WeakSideController, and InvariantEnforcer.

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Scratch entry for one agent in the attacking pool during a single 10 Hz tick.
    /// Pre-allocated array of these lives in <see cref="AttackingAITick"/>; fields are
    /// written and updated by each pipeline stage (§3.2–§3.11). Not exposed publicly.
    /// </summary>
    internal struct AttackPoolEntry
    {
        /// <summary>Agent entity identifier.</summary>
        public int       EntityId;

        /// <summary>Agent's current pitch position (X, Y) in metres.</summary>
        public Vector2   Position;

        /// <summary>
        /// Fractional Y position within the pitch: BaselineSlot.y / PITCH_WIDTH_M.
        /// Derived from #12 formation slot; used for lateralOffset_m in §3.4.
        /// Valid range: [0, 1].
        /// </summary>
        public float     LateralPct;

        /// <summary>Line membership (Defense / Midfield / Attack). Controls RUNNER eligibility (§3.3).</summary>
        public LineId    Line;

        /// <summary>Role assigned to this agent for the current tick. Updated in place.</summary>
        public AttackRole AssignedRole;

        /// <summary>True when <see cref="DepthOffsetM"/> / <see cref="LateralOffsetM"/> /
        /// <see cref="RunTriggerTick"/> are valid RUNNER parameters.</summary>
        public bool      HasRunParams;

        /// <summary>depthOffset_m from §3.4. Valid only when <see cref="HasRunParams"/> is true. [5.0, 40.0] m.</summary>
        public float     DepthOffsetM;

        /// <summary>lateralOffset_m from §3.4. Valid only when <see cref="HasRunParams"/> is true. [−34.0, 34.0] m.</summary>
        public float     LateralOffsetM;

        /// <summary>runTriggerTick from §3.4. Valid only when <see cref="HasRunParams"/> is true.</summary>
        public int       RunTriggerTick;

        /// <summary>
        /// Run target pitch position used for Invariant 3 own-half check (§3.11).
        /// Valid only when <see cref="HasRunParams"/> is true.
        /// </summary>
        public Vector2   RunTargetPosition;

        /// <summary>
        /// Non-runner target position (HOLD_WIDTH or WEAK_SIDE).
        /// Set by <see cref="WidthHolder"/> and <see cref="WeakSideController"/>.
        /// Zero when role is RUNNER or SUPPORT_BALL.
        /// </summary>
        public Vector2   TargetPosition;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
