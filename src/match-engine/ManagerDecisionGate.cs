// File:     src/match-engine/ManagerDecisionGate.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §3.2 (FM-TP-02, FR-TP-006/018/019, KD-3), F5; Code Standards #20
// Purpose:  The manager-decision cadence gate — a pure tick-count predicate (a gate, not a clock
//           file, KD-3). Fires at kickoff (first-ever evaluation) and every
//           ManagerDecisionIntervalTicks thereafter; half-time is gated on the engine modelling
//           halves (PASS-1 M-1) and ships later.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The #26 §3.2 decision gate (FM-TP-02). A pure function of the tick and the team's
    /// <see cref="ManagerState"/>; the match engine evaluates it only inside the AI-stride branch,
    /// BEFORE the FR-TI-027 pending→active tactic commit, so a decision made at tick N is staged
    /// at N and committed at the same stride boundary (FR-TP-018). Off-stride firing is impossible
    /// by construction (F5 — there is no other call site).
    ///
    /// SHIPPED TRIGGERS (per the §1.6 T2 prerequisite gate / PASS-1 M-1): the kickoff decision
    /// (the first-ever evaluation, <c>LastDecisionTick &lt; 0</c>) and the fixed interval. The
    /// half-time trigger (FR-TP-019) requires the engine to model halves, which it does not —
    /// it lands with that engine substrate, not here.
    /// </summary>
    public static class ManagerDecisionGate
    {
        /// <summary>
        /// True when a manager decision is due at <paramref name="tick"/> for a team in
        /// <paramref name="state"/>: Mode is AI (FR-TP-007 — Human never fires) AND either no
        /// decision has fired yet (the kickoff decision; a boot-path kickoff stamps
        /// <c>LastDecisionTick = 0</c> so it is not re-fired) or the fixed interval has elapsed
        /// since the last fired decision (§3.2 worked example: kickoff at tick 0, next no earlier
        /// than tick 18 000, evaluated at the first stride tick ≥ due).
        /// </summary>
        /// <param name="tick">The current 60 Hz tick (the caller's stride tick).</param>
        /// <param name="state">The team's manager state.</param>
        public static bool DecisionDue(int tick, in ManagerState state)
        {
            if (state.Mode != ManagerMode.AI)
            {
                return false;
            }
            return state.LastDecisionTick < 0
                || tick - state.LastDecisionTick >= TacticalPresetsConstants.ManagerDecisionIntervalTicks;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 T2 — kickoff + interval; half-time |
// |         |            |        |   gated on the engine halves model per PASS-1 M-1).            |
#endregion
