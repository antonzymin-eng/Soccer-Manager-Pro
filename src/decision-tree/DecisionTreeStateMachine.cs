// File:     src/decision-tree/DecisionTreeStateMachine.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.7, Code Standards #20
// Purpose:  Per-agent state machine transitions. Pure state evaluator: transitions
//           are computed here, stored in DecisionTree per-agent state array. §3.7.

using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Pure state evaluator for the per-agent Decision Tree state machine.
    /// Computes the next DtState from current state and pipeline inputs.
    /// No side effects. Decision Tree #8 §3.7.
    /// </summary>
    internal static class DecisionTreeStateMachine
    {
        /// <summary>
        /// Returns the next DtState after ReceiveSnapshot is called with a standard heartbeat.
        /// §3.7.2 transition table.
        /// </summary>
        internal static DtState OnSnapshotReceived(
            DtState current,
            in FilteredView snapshot,
            bool validationPassed)
        {
            if (!validationPassed)
                return DtState.IDLE;

            switch (current)
            {
                case DtState.IDLE:
                case DtState.EXECUTING:
                case DtState.INTERRUPTED:
                    return DtState.EVALUATING;

                case DtState.EVALUATING:
                    // Already evaluating; forced refresh replaces in-progress evaluation
                    return snapshot.ForcedRefreshThisTick ? DtState.EVALUATING : DtState.EVALUATING;

                default:
                    return DtState.EVALUATING;
            }
        }

        /// <summary>
        /// Returns the next DtState after pipeline evaluation completes successfully.
        /// §3.7.2.
        /// </summary>
        internal static DtState OnEvaluationComplete(DtState current)
        {
            // After successful evaluation, agent transitions to EXECUTING
            return current == DtState.EVALUATING ? DtState.EXECUTING : current;
        }

        /// <summary>
        /// Returns the next DtState when an interrupt signal is received (tackle, collision).
        /// §3.7.2.
        /// </summary>
        internal static DtState OnInterrupt(DtState current)
        {
            return DtState.INTERRUPTED;
        }

        /// <summary>
        /// Returns the next DtState when the current action is completed by the execution system.
        /// §3.7.2.
        /// </summary>
        internal static DtState OnActionComplete(DtState current)
        {
            return current == DtState.EXECUTING ? DtState.IDLE : current;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
