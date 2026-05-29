// File:     src/decision-tree/DecisionTree.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.1.2, §3.6, §4.1–4.3, Code Standards #20
// Purpose:  Orchestrator-facing entry point. Runs the 6-step pipeline for one agent
//           per ReceiveSnapshot() call. Zero heap allocation on the hot path.
//           One instance per simulation agent (AgentId 0–21). §4.1.

using UnityEngine;
using Unity.Profiling;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Per-agent Decision Tree orchestrator. Runs the 6-step pipeline each 10 Hz heartbeat.
    /// Construction injects all dependencies; hot-path methods are zero-allocation.
    /// Decision Tree #8 §2.1.2, §4.1.
    /// </summary>
    public sealed class DecisionTree
    {
        // ── Profiling ──────────────────────────────────────────────────────────
        private static readonly ProfilerMarker s_PipelineMarker =
            new ProfilerMarker("DT.Pipeline");

        // ── Per-agent pre-allocated buffers (INV-10 zero-allocation) ──────────
        private readonly ActionOption[] _optionBuffer =
            new ActionOption[DecisionTreeConstants.MaxOptions];

        // ── State ──────────────────────────────────────────────────────────────
        private DtState _state = DtState.IDLE;
        private AgentAction _lastAction;

        // ── Dependencies (constructor-injected) ───────────────────────────────
        private readonly IDtMovementController _movementController;
        private readonly int _agentId;
        private ulong _matchSeed;

        public DecisionTree(int agentId, IDtMovementController movementController, ulong matchSeed)
        {
            _agentId            = agentId;
            _movementController = movementController;
            _matchSeed          = matchSeed;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Current state machine state. §3.7.</summary>
        public DtState State => _state;

        /// <summary>Last selected action. Valid after at least one successful evaluation.</summary>
        public AgentAction LastAction => _lastAction;

        /// <summary>
        /// Entry point called by the simulation orchestrator each heartbeat.
        /// Passes by value to satisfy §3.6.4 snapshot lifetime constraint.
        /// Steps: Validate → Assemble → Generate → Score → Select → Dispatch. §3.6.
        /// </summary>
        public void ReceiveSnapshot(
            FilteredView snapshot,
            MatchContext matchContext,
            TacticalContext tacticalContext,
            DtAgentAttributes attributes,
            AgentState agentState,
            float pressureScalar)
        {
            using var marker = s_PipelineMarker.Auto();

            // ── Step 1: Validate (§3.6) ───────────────────────────────────────
            bool valid = SnapshotValidator.Validate(in snapshot, _agentId);
            _state = DecisionTreeStateMachine.OnSnapshotReceived(_state, in snapshot, valid);

            if (!valid)
                return;

            if (_state != DtState.EVALUATING)
                return;

            // ── Step 2: Assemble context (§2.2.4) ────────────────────────────
            DecisionContext ctx = DecisionContextAssembler.Assemble(
                snapshot, matchContext, tacticalContext,
                attributes, agentState, pressureScalar, _matchSeed);

            // §3.1.1.3: AgentHasBall=true + BallVisible=false is physically implausible
            if (ctx.AgentHasBall && !snapshot.BallVisible)
                Debug.LogWarning($"[DT] {DecisionTreeConstants.WarnFmDt09} agent {_agentId}");

            // ── Step 3: Generate options (§3.1) ───────────────────────────────
            int optionCount = OptionGenerator.GenerateOptions(in ctx, _optionBuffer);

            // ── Step 4: Score options (§3.2) ──────────────────────────────────
            UtilityScorer.ScoreOptions(_optionBuffer, optionCount, in ctx);

            // ── Step 5: Select action (§3.3) ──────────────────────────────────
            AgentAction selected = ActionSelector.SelectAction(
                _optionBuffer, optionCount, in ctx,
                out bool tiebreakerApplied,
                out bool fallbackToHold);

            _lastAction = selected;
            _state = DecisionTreeStateMachine.OnEvaluationComplete(_state);

            // ── Publish event (Stage 0: no-op) ────────────────────────────────
            EventBusStub.Publish(new DecisionMadeEvent(
                _agentId, selected, selected.UtilityScore,
                optionCount, ctx.CurrentFrame,
                tiebreakerApplied, fallbackToHold));

            // ── Step 6: Dispatch (§3.5) ───────────────────────────────────────
            ActionDispatcher.Dispatch(selected, in ctx, _movementController);
        }

        /// <summary>
        /// Signals an interrupt (tackle/collision) to the state machine.
        /// Called by the simulation orchestrator when an execution system signals failure.
        /// §3.7.
        /// </summary>
        public void NotifyInterrupt()
        {
            _state = DecisionTreeStateMachine.OnInterrupt(_state);
        }

        /// <summary>
        /// Signals the current action completed successfully.
        /// §3.7.
        /// </summary>
        public void NotifyActionComplete()
        {
            _state = DecisionTreeStateMachine.OnActionComplete(_state);
        }

        /// <summary>Updates the per-match seed (called when a new match begins).</summary>
        public void SetMatchSeed(ulong seed)
        {
            _matchSeed = seed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
