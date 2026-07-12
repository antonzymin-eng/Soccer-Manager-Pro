// File:     src/match-engine/ManagerDecisionGate.cs
// Created:  2026-07-11
// Modified: 2026-07-11 (half-time trigger activated — the engine halves model landed, FR-TP-019)
// Author:   —
// Spec:     Tactical Presets #26 §3.2 (FM-TP-02, FR-TP-006/018/019, KD-3), F5; Code Standards #20
// Purpose:  The manager-decision cadence gate — a pure tick-count predicate (a gate, not a clock
//           file, KD-3). Fires at kickoff (first-ever evaluation), at the first evaluation at or
//           after the half-time boundary, and every ManagerDecisionIntervalTicks thereafter.

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The #26 §3.2 decision gate (FM-TP-02). A pure function of the tick and the team's
    /// <see cref="ManagerState"/>; the match engine evaluates it only inside the AI-stride branch,
    /// BEFORE the FR-TI-027 pending→active tactic commit, so a decision made at tick N is staged
    /// at N and committed at the same stride boundary (FR-TP-018). Off-stride firing is impossible
    /// by construction (F5 — the only production call site is RunAiPhase's stride branch).
    ///
    /// SHIPPED TRIGGERS: the kickoff decision (the first-ever evaluation,
    /// <c>LastDecisionTick &lt; 0</c>), the half-time decision (FR-TP-019 — activated 2026-07-11
    /// with the engine's Stage-0 halves model, <c>MatchEngineConstants.HALF_TIME_BOUNDARY_TICK</c>;
    /// the boundary derives from the engine-owned match-length constants, and the gate adds no
    /// clock state beyond <c>LastDecisionTick</c>), and the fixed interval. Per the §3.2 worked
    /// example the half-time decision fires regardless of interval position — it may land within
    /// an interval of the previous decision.
    /// </summary>
    public static class ManagerDecisionGate
    {
        /// <summary>
        /// True when a manager decision is due at <paramref name="tick"/> for a team in
        /// <paramref name="state"/>: Mode is AI (FR-TP-007 — Human never fires) AND any §3.2 term
        /// holds — (a) kickoff: no decision has fired yet (a boot-path kickoff stamps
        /// <c>LastDecisionTick = 0</c> so it is not re-fired); (b) half-time: the tick is at or
        /// past <c>HALF_TIME_BOUNDARY_TICK</c> and no decision has fired at or past it yet (fires
        /// exactly once — the fired decision stamps <c>LastDecisionTick ≥ boundary</c>, closing the
        /// term); (c) interval: the fixed interval has elapsed since the last fired decision (§3.2
        /// worked example: kickoff at tick 0, next no earlier than tick 18 000, evaluated at the
        /// first stride tick ≥ due).
        /// </summary>
        /// <param name="tick">The current 60 Hz tick (the caller's stride tick).</param>
        /// <param name="state">The team's manager state.</param>
        public static bool DecisionDue(int tick, in ManagerState state)
        {
            if (state.Mode != ManagerMode.AI)
            {
                return false;
            }
            if (state.LastDecisionTick < 0)
            {
                return true;  // kickoff — first-ever evaluation
            }
            if (tick >= MatchEngineConstants.HALF_TIME_BOUNDARY_TICK
                && state.LastDecisionTick < MatchEngineConstants.HALF_TIME_BOUNDARY_TICK)
            {
                return true;  // half-time — first evaluation at/after the boundary (FR-TP-019)
            }
            return tick - state.LastDecisionTick >= TacticalPresetsConstants.ManagerDecisionIntervalTicks;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 T2 — kickoff + interval; half-time |
// |         |            |        |   gated on the engine halves model per PASS-1 M-1).            |
// | 1.1     | 2026-07-11 | —      | Half-time trigger activated (FR-TP-019): the engine substrate  |
// |         |            |        |   landed HALF_TIME_BOUNDARY_TICK (Stage-0 halves model —       |
// |         |            |        |   boundary from the match-length constants). Fires exactly once,|
// |         |            |        |   at the first stride evaluation at/after the boundary,        |
// |         |            |        |   regardless of interval position (§3.2 worked example); adds  |
// |         |            |        |   no clock state beyond LastDecisionTick.                      |
#endregion
