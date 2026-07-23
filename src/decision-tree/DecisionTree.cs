// File:     src/decision-tree/DecisionTree.cs
// Created:  2026-05-29
// Modified: 2026-06-22 (Phase D D1 AR — HasDispatchedAction accessor)
// Author:   —
// Spec:     Decision Tree #8 §2.1.2, §3.6, §3.7, §4.1–4.3, Code Standards #20
// Purpose:  Orchestrator-facing entry point. Runs the 6-step pipeline for one agent
//           per ReceiveSnapshot() call. Zero heap allocation on the hot path.
//           One instance per simulation agent (AgentId 0–21). §4.1.

using UnityEngine;
using Unity.Profiling;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

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
        private bool _hasDispatchedAction;   // §3.7.2 "DispatchedActionType" record

        // ── Dependencies (constructor-injected) ───────────────────────────────
        private readonly IDtMovementController _movementController;
        private readonly PassExecutor _passExecutor;
        private readonly ShotExecutor _shotExecutor;
        private readonly IDtSaveDispatch _saveDispatch;
        private readonly int _agentId;
        private ulong _matchSeed;

        /// <summary>
        /// Creates the per-agent Decision Tree. The executors are this agent's
        /// PassExecutor / ShotExecutor instances (Pass #5 §4.1 / Shot #6 §4.1 — both
        /// are per-agent stateful windup machines). <paramref name="saveDispatch"/> is the
        /// goalkeeper save sink (ERR-008-013) — one shared instance across all agents. All three
        /// may be null in unit-test or bootstrap contexts where the corresponding action will never
        /// be dispatched or where a dropped dispatch is acceptable; ActionDispatcher logs the wiring
        /// failure. AR-2 H-1: the previous revision invoked the executors statically, which never
        /// compiled.
        /// </summary>
        public DecisionTree(
            int agentId,
            IDtMovementController movementController,
            ulong matchSeed,
            PassExecutor passExecutor = null,
            ShotExecutor shotExecutor = null,
            IDtSaveDispatch saveDispatch = null)
        {
            _agentId            = agentId;
            _movementController = movementController;
            _matchSeed          = matchSeed;
            _passExecutor       = passExecutor;
            _shotExecutor       = shotExecutor;
            _saveDispatch       = saveDispatch;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Current state machine state. §3.7.</summary>
        public DtState State => _state;

        /// <summary>Last selected action. Valid after at least one successful evaluation.</summary>
        public AgentAction LastAction => _lastAction;

        /// <summary>
        /// True once this agent has selected and dispatched at least one action (the §3.7.2
        /// DispatchedActionType record is set). Stays true thereafter. Lets a host verify the
        /// pipeline actually produced a decision rather than aborting at the validation gate.
        /// </summary>
        public bool HasDispatchedAction => _hasDispatchedAction;

        // ── Snapshot seam — Match Engine Phase D step D0 ─────────────────────────

        /// <summary>
        /// Captures the per-agent cross-tick state machine (state ordinal + last action + the §3.7.2
        /// DispatchedActionType flag) as a plain-data snapshot for canonical serialization / deterministic
        /// replay (Match Engine design note §2.6). Allocation-free (returns a value type). Parallel to the
        /// Pass/Shot executor C0 seams and <see cref="AgentMovement.OscillationGuard.GetState"/>. The
        /// per-match seed and the per-tick option buffer are excluded — see <see cref="DecisionTreeState"/>.
        /// </summary>
        public DecisionTreeState CaptureState()
        {
            return new DecisionTreeState((int)_state, in _lastAction, _hasDispatchedAction);
        }

        /// <summary>
        /// Restores the per-agent cross-tick state machine from a snapshot produced by
        /// <see cref="CaptureState"/> (replay / save-load). The match seed is not carried in the snapshot;
        /// it is re-established at boot via the constructor / <see cref="SetMatchSeed"/> (§2.6
        /// boot-deterministic exclusion), so a restore path must seed it separately if a new match context
        /// is being loaded.
        /// </summary>
        public void RestoreState(in DecisionTreeState state)
        {
            _state               = (DtState)state.State;
            _lastAction          = state.LastAction;
            _hasDispatchedAction = state.HasDispatchedAction;
        }

        /// <summary>
        /// Entry point called by the simulation orchestrator each heartbeat.
        /// Passes by value to satisfy §3.6.4 snapshot lifetime constraint.
        /// Steps: Validate → Assemble → Generate → Score → Select → Dispatch. §3.6.
        /// While a PASS/SHOOT is in flight (EXECUTING), the pipeline is skipped on
        /// the normal heartbeat cadence and re-entered only on forced refresh
        /// (§3.6.3 / §3.7.2); movement actions are continuous and re-evaluate every
        /// heartbeat (Stage 0 deviation note in DecisionTreeStateMachine).
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

            DtState prior = _state;
            _state = DecisionTreeStateMachine.OnSnapshotReceived(
                _state, in snapshot, valid, _lastAction.Type, _hasDispatchedAction);

            if (!valid)
                return;

            if (_state != DtState.EVALUATING)
                return;   // PASS/SHOOT in flight; §3.7.2 — no re-evaluation this tick

            // §3.6.3: a forced refresh that re-enters the pipeline over an in-flight
            // PASS/SHOOT compares the fresh selection with the in-flight action and
            // re-dispatches only when the ActionType differs.
            bool inFlightExecutionAction =
                prior == DtState.EXECUTING
                && _hasDispatchedAction
                && !DecisionTreeStateMachine.IsContinuousAction(_lastAction.Type)
                && snapshot.ForcedRefreshThisTick;

            // ── Step 2: Assemble context (§2.2.4) ────────────────────────────
            DecisionContext ctx = DecisionContextAssembler.Assemble(
                snapshot, matchContext, tacticalContext,
                attributes, agentState, pressureScalar, _matchSeed);

            // §3.1.1.3: AgentHasBall=true + BallVisible=false is physically implausible
            // (FM-DT-09 — warning only; world state is authoritative).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ctx.AgentHasBall && !snapshot.BallVisible)
                Debug.LogWarning($"[DT] {DecisionTreeConstants.WarnFmDt09} agent {_agentId}");
#endif

            // ── Step 3: Generate options (§3.1) ───────────────────────────────
            int optionCount = OptionGenerator.GenerateOptions(in ctx, _optionBuffer);

            // ── Step 4: Score options (§3.2) ──────────────────────────────────
            UtilityScorer.ScoreOptions(_optionBuffer, optionCount, in ctx);

            // ── Step 5: Select action (§3.3) ──────────────────────────────────
            AgentAction selected = ActionSelector.SelectAction(
                _optionBuffer, optionCount, in ctx,
                out bool tiebreakerApplied,
                out bool fallbackToHold);

            // §3.6.3 same-type suppression: identical ActionType to the in-flight
            // action — continue execution; no re-dispatch, no duplicate event.
            if (inFlightExecutionAction && selected.Type == _lastAction.Type)
            {
                _state = DtState.EXECUTING;
                return;
            }
            // Differing type over an in-flight PASS/SHOOT: §3.7.2 row 7
            // (EXECUTING → INTERRUPTED → new dispatch). The INTERRUPTED hop is
            // internal to this call. Stage 0 has no DT→executor cancel entry point —
            // the executor self-cancels via its own possession recheck / tackle-flag
            // paths (Pass #5 FM-08, §3.8.5); documented under ERR-008-008.

            _lastAction = selected;
            _hasDispatchedAction = true;
            _state = DecisionTreeStateMachine.OnEvaluationComplete(DtState.EVALUATING, selected.Type);

            // ── Publish event (Tier C, ordinal 0x11) ──────────────────────────
            EventBusStub.Publish(new DecisionMadeEvent(
                _agentId, selected, selected.UtilityScore,
                optionCount, ctx.CurrentFrame,
                tiebreakerApplied, fallbackToHold));

            // ── Step 6: Dispatch (§3.5) ───────────────────────────────────────
            ActionDispatcher.Dispatch(selected, in ctx, _movementController,
                _passExecutor, _shotExecutor, _saveDispatch);
        }

        /// <summary>
        /// Signals an interrupt (tackle/collision) to the state machine.
        /// Called by the simulation orchestrator when an execution system signals failure.
        /// Only an in-flight action can be interrupted (§3.7.2 row 6). §3.7.
        /// </summary>
        public void NotifyInterrupt()
        {
            DtState next = DecisionTreeStateMachine.OnInterrupt(_state);
            if (next == DtState.INTERRUPTED)
                _hasDispatchedAction = false;   // §3.7.2 row 8: clear DispatchedActionType
            _state = next;
        }

        /// <summary>
        /// Signals the current action completed successfully.
        /// §3.7.
        /// </summary>
        public void NotifyActionComplete()
        {
            DtState next = DecisionTreeStateMachine.OnActionComplete(_state);
            if (next == DtState.IDLE && _state == DtState.EXECUTING)
                _hasDispatchedAction = false;   // §3.7.2 row 4: clear DispatchedActionType
            _state = next;
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
// | 1.1     | 2026-06-11 | —      | Audit AR-2 fix pass. H-1: PassExecutor/ShotExecutor instances injected via    |
// |         |            |        |   constructor (optional; null tolerated for tests) and threaded to            |
// |         |            |        |   ActionDispatcher — the static calls had never compiled. H-3: §3.7.2/§3.6.3  |
// |         |            |        |   semantics — PASS/SHOOT hold EXECUTING across heartbeats (pipeline skipped), |
// |         |            |        |   forced refresh re-evaluates with same-ActionType re-dispatch suppression;   |
// |         |            |        |   HOLD lands in IDLE; _hasDispatchedAction tracks the §3.7.2                  |
// |         |            |        |   DispatchedActionType record. M-10: §3.1.1.3 warning now FR-CS-031 gated.    |
// | 1.2     | 2026-06-22 | —      | Match Engine Phase D step D0: CaptureState()/RestoreState(in DecisionTreeState)|
// |         |            |        |   snapshot get/restore seam over the cross-tick state machine (_state /        |
// |         |            |        |   _lastAction / _hasDispatchedAction), parallel to the Pass/Shot executor C0   |
// |         |            |        |   seams. _matchSeed (boot-deterministic) and _optionBuffer (per-tick scratch) |
// |         |            |        |   excluded per §2.6. No change to the ReceiveSnapshot pipeline.                |
// | 1.3     | 2026-06-22 | —      | Phase D D1 AR (L-1): public HasDispatchedAction accessor (parallel to State / |
// |         |            |        |   LastAction) so a host can verify the pipeline produced a decision rather    |
// |         |            |        |   than aborting at SnapshotValidator. Read-only over the existing field; no    |
// |         |            |        |   behaviour change.                                                            |
// | 1.4     | 2026-07-23 | —      | ERR-008-013: ctor gains an optional IDtSaveDispatch saveDispatch (the GK save |
// |         |            |        |   sink, parallel to the executors), threaded into ActionDispatcher.Dispatch.  |
// |         |            |        |   _saveDispatch is an injected dependency, not cross-tick state (excluded from |
// |         |            |        |   CaptureState, like the executors).                                           |
#endregion
