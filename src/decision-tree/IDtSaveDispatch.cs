// File:     src/decision-tree/IDtSaveDispatch.cs
// Created:  2026-07-23
// Author:   —
// Spec:     Decision Tree #8 §3.5 (ERR-008-013), Goalkeeper Mechanics #11 §2.2.1, Code Standards #20
// Purpose:  Dispatch boundary from the Decision Tree to the goalkeeper save sink. When a keeper's
//           DT selects SAVE, ActionDispatcher calls CommitSave(agentId); the host (MatchEngine)
//           maps the agent to its GK slot, projects the goalkeeper attributes, and commits the
//           SaveIntent to GoalkeeperMechanics — the "DT commits the save" path the #11 SaveIntent
//           doc always anticipated. Primitives only: the DT owns the decision, the sink owns the
//           GK geometry / projection / orchestrator commit (no GoalkeeperMechanics type crosses here).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Dispatch boundary between the Decision Tree and the goalkeeper save orchestrator.
    /// Injected into <see cref="DecisionTree"/> alongside the pass/shot executors; may be null in
    /// test/bootstrap contexts (a SAVE dispatch with a null sink is a logged wiring drop, the
    /// null-executor precedent). Decision Tree #8 §3.5 (ERR-008-013).
    /// </summary>
    public interface IDtSaveDispatch
    {
        /// <summary>
        /// Commit a goalkeeper save for the given agent. Called once per heartbeat for the keeper
        /// whose selected action is SAVE (the sole off-ball option while a save is available). The
        /// implementation is responsible for the per-episode commit latch, the attribute projection,
        /// and the <c>GoalkeeperMechanics.CommitSaveIntent</c> call.
        /// </summary>
        void CommitSave(int agentId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-23 | —      | Initial — the DT→GK save dispatch seam (ERR-008-013).            |
#endregion
