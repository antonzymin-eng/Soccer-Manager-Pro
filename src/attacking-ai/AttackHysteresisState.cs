// File:     src/attacking-ai/AttackHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-06-15
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
    /// <para><see cref="DwellCounter"/> accumulates while the same role is consistently preferred;
    /// <see cref="AttackHysteresis.IsStable"/> fires when it reaches
    /// <see cref="AttackingAIConstants.AttackDwellTicks"/> — a diagnostic signal only. Role
    /// retention is governed by <see cref="CandidateDwell"/> (a transition commits only after a
    /// different candidate persists the dwell window); agents are re-evaluated every tick.</para>
    /// </summary>
    public struct AttackHysteresisState
    {
        /// <summary>Role this agent is currently committed to.</summary>
        public AttackRole CurrentRole;

        /// <summary>
        /// Ticks the current role has been stably preferred. Increments each tick while
        /// the same role is consistently chosen; resets to 0 on role transition.
        /// IsStable = (DwellCounter >= ATTACK_DWELL_TICKS) — diagnostic only; does not gate
        /// re-evaluation (ERR-015-009).
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
// | 1.1     | 2026-06-15 | —      | AR-4 H-1 (ERR-015-009): DwellCounter/IsStable docs clarified as diagnostic-only; role retention is governed by CandidateDwell, not by skipping evaluation while stable. |
#endregion
