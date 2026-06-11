// File:     src/decision-tree/DecisionTreeStateMachine.cs
// Created:  2026-05-29
// Modified: 2026-06-11 (audit AR-2 fix pass)
// Author:   —
// Spec:     Decision Tree #8 §3.7, Code Standards #20
// Purpose:  Per-agent state machine transitions. Pure state evaluator: transitions
//           are computed here, stored in DecisionTree per-agent state. §3.7.

using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Pure state evaluator for the per-agent Decision Tree state machine.
    /// Computes the next DtState from current state and pipeline inputs.
    /// No side effects. Decision Tree #8 §3.7.
    ///
    /// SPEC-DEVIATION NOTE (Stage 0, AR-2 H-3): §3.7.2 row 5 lists only HOLD and
    /// MOVE_TO_POSITION as continuous (re-evaluated each heartbeat) and implies
    /// DRIBBLE/PRESS/INTERCEPT hold EXECUTING until a completion signal. No Stage 0
    /// execution system emits completion signals for movement-routed actions (Agent
    /// Movement #2 consumes commands without acknowledgement), so holding EXECUTING
    /// would freeze an agent after its first movement dispatch. Stage 0 therefore
    /// treats ALL movement-routed actions (DRIBBLE/MOVE/PRESS/INTERCEPT) as
    /// continuous; PASS and SHOOT — whose executors have a real windup lifecycle and
    /// whose §3.7.2 contract explicitly forbids per-tick re-dispatch — remain in
    /// EXECUTING until NotifyActionComplete / NotifyInterrupt / forced refresh.
    /// Filed as ERR-008-008; §3.7.2 patched with the same note in this commit.
    /// </summary>
    internal static class DecisionTreeStateMachine
    {
        /// <summary>True for actions whose Stage 0 lifecycle is per-heartbeat re-dispatch.</summary>
        internal static bool IsContinuousAction(ActionType type)
            => type != ActionType.PASS && type != ActionType.SHOOT;

        /// <summary>
        /// Returns the next DtState after ReceiveSnapshot is called.
        /// §3.7.2 transition table.
        /// </summary>
        /// <param name="lastDispatchedType">
        /// ActionType of the in-flight dispatched action; meaningful only when
        /// <paramref name="hasDispatchedAction"/> is true and current is EXECUTING.
        /// </param>
        internal static DtState OnSnapshotReceived(
            DtState current,
            in FilteredView snapshot,
            bool validationPassed,
            ActionType lastDispatchedType,
            bool hasDispatchedAction)
        {
            if (!validationPassed)
            {
                // §3.8 FM-04: a malformed snapshot aborts THIS TICK's evaluation only.
                // AR-2 H-3: the previous revision reset any state to IDLE — destroying
                // the EXECUTING record of an in-flight action on a transient bad
                // snapshot. State is preserved; INTERRUPTED still decays to IDLE per
                // the §3.7.3 invariant-2 one-frame lifetime.
                return current == DtState.INTERRUPTED ? DtState.IDLE : current;
            }

            switch (current)
            {
                case DtState.IDLE:
                    return DtState.EVALUATING;

                case DtState.INTERRUPTED:
                    // §3.7.3 invariant 2: INTERRUPTED → IDLE is automatic after one
                    // frame; the next heartbeat then enters EVALUATING. Both hops are
                    // folded into this transition (the intermediate IDLE is not
                    // observable between heartbeats).
                    return DtState.EVALUATING;

                case DtState.EXECUTING:
                    if (hasDispatchedAction && !IsContinuousAction(lastDispatchedType))
                    {
                        // §3.7.2: PASS/SHOOT are NOT re-evaluated on the heartbeat
                        // cadence — only a forced refresh re-enters the pipeline
                        // (§3.6.3). Completion arrives via NotifyActionComplete.
                        return snapshot.ForcedRefreshThisTick
                            ? DtState.EVALUATING
                            : DtState.EXECUTING;
                    }
                    // Continuous actions: re-evaluate every heartbeat (§3.7.2 row 5
                    // + Stage 0 deviation note above).
                    return DtState.EVALUATING;

                case DtState.EVALUATING:
                default:
                    // Re-entrant call while evaluating (§3.6.3 forced refresh replaces
                    // the in-progress evaluation).
                    return DtState.EVALUATING;
            }
        }

        /// <summary>
        /// Returns the next DtState after pipeline evaluation completes successfully.
        /// §3.7.2 rows 2–3: a dispatched execution-system or movement action enters
        /// EXECUTING; HOLD (including fallback-to-HOLD) engages no execution system
        /// and lands in IDLE.
        /// </summary>
        internal static DtState OnEvaluationComplete(DtState current, ActionType selectedType)
        {
            if (current != DtState.EVALUATING) return current;
            return selectedType == ActionType.HOLD ? DtState.IDLE : DtState.EXECUTING;
        }

        /// <summary>
        /// Returns the next DtState when an interrupt signal is received (tackle, collision).
        /// §3.7.2 row 6 defines the transition from EXECUTING only — an interrupt with
        /// no in-flight action has nothing to cancel (AR-2 H-3: previously
        /// unconditional, so a tackle on an IDLE agent parked it in INTERRUPTED).
        /// </summary>
        internal static DtState OnInterrupt(DtState current)
        {
            return current == DtState.EXECUTING ? DtState.INTERRUPTED : current;
        }

        /// <summary>
        /// Returns the next DtState when the current action is completed by the execution system.
        /// §3.7.2 row 4.
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
// | 1.1     | 2026-06-11 | —      | Audit AR-2 H-3: §3.7.2 semantics implemented — PASS/SHOOT hold EXECUTING      |
// |         |            |        |   between heartbeats (re-evaluation only on forced refresh per §3.6.3; the     |
// |         |            |        |   previous unconditional EXECUTING→EVALUATING re-ran the full pipeline every   |
// |         |            |        |   tick mid-windup, and the ForcedRefreshThisTick branch was a no-op ternary);  |
// |         |            |        |   HOLD lands in IDLE (row 3); invalid snapshot preserves state instead of      |
// |         |            |        |   stomping EXECUTING to IDLE; OnInterrupt gated to EXECUTING (row 6).          |
// |         |            |        |   Stage 0 SPEC-DEVIATION NOTE: DRIBBLE/PRESS/INTERCEPT treated as continuous   |
// |         |            |        |   alongside HOLD/MOVE (no movement completion signal exists; ERR-008-008).     |
#endregion
