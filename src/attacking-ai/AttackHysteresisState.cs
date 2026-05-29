// File:     src/attacking-ai/AttackHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.4, §3.12, FR-AT-022, Code Standards #20
// Purpose:  Per-agent role-dwell state. Increments-based hysteresis: a transition fires only
//           after a candidate role is continuously preferred for ATTACK_DWELL_TICKS ticks.
//           Authoritative simulation state contributing to the per-tick digest (FR-AT-004).

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Per-agent dwell-lock state for attacking role transitions. Mutable authoritative
    /// simulation state (FR-AT-004 / #16 §3.2). Attacking AI #15 §2.2.4 / §3.12.
    ///
    /// <para><see cref="DwellCounter"/> accumulates while the same role is consistently preferred.
    /// <see cref="IsStable"/> fires when <see cref="DwellCounter"/> reaches
    /// <see cref="AttackingAIConstants.AttackDwellTicks"/>; while stable the role is retained
    /// without re-evaluation.</para>
    /// </summary>
    public struct AttackHysteresisState
    {
        /// <summary>Role this agent is currently committed to.</summary>
        public AttackRole CurrentRole;

        /// <summary>
        /// Ticks the current role has been stably preferred. Increments each tick while
        /// the same role is consistently chosen; resets to 0 on role transition.
        /// isStable = (DwellCounter >= ATTACK_DWELL_TICKS).
        /// </summary>
        public int DwellCounter;

        /// <summary>New role being evaluated for transition.</summary>
        public AttackRole CandidateRole;

        /// <summary>
        /// Consecutive ticks the candidate role has been preferred. Transition commits
        /// when CandidateDwell >= ATTACK_DWELL_TICKS; resets when CandidateRole changes.
        /// </summary>
        public int CandidateDwell;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
