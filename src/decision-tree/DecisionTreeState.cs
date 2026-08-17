// File:     src/decision-tree/DecisionTreeState.cs
// Created:  2026-06-22
// Modified: 2026-06-22
// Author:   —
// Spec:     Decision Tree #8 §3.7; Match Engine design note §2.6 (Phase D step D0); Code Standards #20
// Purpose:  Plain-data snapshot of a DecisionTree's cross-tick state-machine state, exposing it so the
//           match-engine snapshot layer can serialize per-agent decision state canonically for
//           deterministic replay (parallel to the Pass/Shot executor C0 seams, DeterministicSim
//           RngStreamState, and AgentMovement OscillationGuardState). Pure data: no behaviour.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Immutable snapshot of <see cref="DecisionTree"/>'s cross-tick state machine — the
    /// <see cref="DtState"/> ordinal, the last selected <see cref="AgentAction"/>, and the §3.7.2
    /// "DispatchedActionType" record flag. A PASS/SHOOT decision is taken on one 10 Hz heartbeat and the
    /// EXECUTING state persists across the intervening 60 Hz physics ticks until the executor completes
    /// or is interrupted, so this is simulation state that survives across ticks — not scratch. Produced
    /// by <see cref="DecisionTree.CaptureState"/> and consumed by <see cref="DecisionTree.RestoreState"/>.
    ///
    /// This is the decision-tree analogue of the Phase C0 <see cref="PassMechanics.PassExecutorState"/> /
    /// <see cref="ShotMechanics.ShotExecutorState"/> executor seams and the Phase B0
    /// <see cref="AgentMovement.OscillationGuardState"/> seam: the DecisionTree holds this state in
    /// private fields with no get/restore accessor, so the Match Engine snapshot payload (design note
    /// §2.6) cannot serialize it canonically without it. Named Capture/Restore for symmetry with those
    /// executor seams.
    ///
    /// EXCLUDED FIELD — <c>_matchSeed</c>: it is NOT carried here. The seed is set at construction (and
    /// via <see cref="DecisionTree.SetMatchSeed"/> when a match begins) and is constant for the whole
    /// match, so a save/restore reconstructs it identically at boot — the design note §2.6 "fully
    /// recomputed before its first read" / boot-deterministic exclusion (parallel to the host
    /// <c>_attrs</c>/<c>_perfs</c> exclusion and the Pass executor's recomputed <c>PhysicalProfile</c>).
    /// The per-tick <c>_optionBuffer</c> is likewise excluded — it is overwritten by OptionGenerator
    /// before it is read within the same <see cref="DecisionTree.ReceiveSnapshot"/> call, so it carries
    /// nothing across ticks.
    /// </summary>
    public readonly struct DecisionTreeState
    {
        /// <summary>State-machine ordinal: 0=IDLE, 1=EVALUATING, 2=EXECUTING, 3=INTERRUPTED.
        /// ORDINAL STABILITY: this value mirrors the <see cref="DtState"/> enum and is digest-load-bearing
        /// once the C5/D-snapshot serializes it — the enum is APPEND-only (reordering it silently breaks
        /// replay compatibility for persisted snapshots).</summary>
        public readonly int State;

        /// <summary>The last action selected by the pipeline. While <see cref="State"/> is EXECUTING this is
        /// the in-flight action whose ActionType the §3.6.3 forced-refresh suppression compares against.</summary>
        public readonly AgentAction LastAction;

        /// <summary>The §3.7.2 "DispatchedActionType" record flag — true once an action has been dispatched
        /// and not yet cleared by completion (§3.7.2 row 4) or interrupt (row 8). Gates forced-refresh
        /// re-dispatch.</summary>
        public readonly bool HasDispatchedAction;

        /// <summary>Constructs an immutable decision-tree-state snapshot from the captured field set.</summary>
        public DecisionTreeState(int state, in AgentAction lastAction, bool hasDispatchedAction)
        {
            State               = state;
            LastAction          = lastAction;
            HasDispatchedAction = hasDispatchedAction;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Initial implementation — Match Engine Phase D step D0 decision-|
// |         |            |        | tree snapshot seam DTO (parallel to Pass/Shot ExecutorState +  |
// |         |            |        | OscillationGuardState / RngStreamState). _matchSeed and the    |
// |         |            |        | per-tick _optionBuffer excluded (boot-deterministic / scratch).|
#endregion
