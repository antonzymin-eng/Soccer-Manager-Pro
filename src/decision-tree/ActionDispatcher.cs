// File:     src/decision-tree/ActionDispatcher.cs
// Created:  2026-05-29
// Modified: 2026-07-23 (ERR-008-013: SAVE → IDtSaveDispatch)
// Author:   —
// Spec:     Decision Tree #8 §3.5, Code Standards #20
// Purpose:  Step 6 of the 6-step pipeline. Routes the selected AgentAction to the
//           appropriate execution system: PassExecutor (PASS), ShotExecutor (SHOOT),
//           IDtSaveDispatch (SAVE), or IDtMovementController (DRIBBLE/HOLD/MOVE/PRESS/INTERCEPT). §3.5.1.

using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 6: routes AgentAction to execution systems. Has side effects.
    /// PassExecutor and ShotExecutor are per-agent INSTANCES owned by the simulation
    /// orchestrator and injected through DecisionTree's constructor (§3.5.1 entry
    /// points; Pass #5 §4.1 / Shot #6 §4.1 define instance Execute methods).
    /// AR-2 H-1: the previous revision invoked PassExecutor.Execute / ShotExecutor.Execute
    /// as STATIC calls — CS0120; the decision-tree assembly had never compiled.
    /// Decision Tree #8 §3.5.
    /// </summary>
    internal static class ActionDispatcher
    {
        /// <summary>
        /// Dispatches action to the appropriate execution system.
        /// Movement actions are routed via IDtMovementController (§3.5 XC-3.5-10).
        /// passExecutor / shotExecutor may be null in test or bootstrap contexts —
        /// a PASS/SHOOT dispatch with a null executor is a wiring failure: it is
        /// logged (dev builds) and the request is dropped; the state machine still
        /// records the dispatch attempt and recovers via the §3.7 heartbeat flow.
        /// §3.5.1.
        /// </summary>
        internal static void Dispatch(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController movementController,
            PassExecutor passExecutor,
            ShotExecutor shotExecutor,
            IDtSaveDispatch saveDispatch = null)
        {
            switch (action.Type)
            {
                case ActionType.PASS:
                    DispatchPass(action, in ctx, passExecutor);
                    break;

                case ActionType.SHOOT:
                    DispatchShoot(action, in ctx, shotExecutor);
                    break;

                case ActionType.SAVE:
                    // ERR-008-013: the keeper committed to a save — hand off to the GK sink, which
                    // maps agent→GK slot, projects the attributes, and commits the SaveIntent. A null
                    // sink is a wiring drop (dev-logged), the null-executor precedent above.
                    if (saveDispatch != null)
                    {
                        saveDispatch.CommitSave(action.AgentId);
                    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    else
                    {
                        Debug.LogError($"[DT] {DecisionTreeConstants.WarnFmDt14} SAVE with no save sink for agent {action.AgentId}");
                    }
#endif
                    break;

                case ActionType.DRIBBLE:
                    DispatchDribble(action, in ctx, movementController);
                    break;

                case ActionType.HOLD:
                    DispatchHold(action, in ctx, movementController);
                    break;

                case ActionType.MOVE_TO_POSITION:
                    DispatchMove(action, in ctx, movementController);
                    break;

                case ActionType.PRESS:
                    DispatchPress(action, in ctx, movementController);
                    break;

                case ActionType.INTERCEPT:
                    DispatchIntercept(action, in ctx, movementController);
                    break;

                default:
                    // FM-DT-14 (§3.5.9; renumbered from the double-allocated FM-DT-09
                    // per ERR-008-007): unknown ActionType. Log and produce a HOLD-safe
                    // command so the agent does not act on a corrupt selection.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[DT] {DecisionTreeConstants.WarnFmDt14} unknown ActionType {action.Type} for agent {action.AgentId}");
#endif
                    movementController.SubmitCommand(action.AgentId,
                        MovementCommand.StrafeWhileWatching(
                            ctx.AgentPosition, ctx.MatchContext.BallPosition));
                    break;
            }
        }

        // ── §3.5.2 PASS ────────────────────────────────────────────────────────

        private static void DispatchPass(
            AgentAction action, in DecisionContext ctx, PassExecutor passExecutor)
        {
            // Finalise TeamId and FrameNumber fields that weren't available at SelectAction time
            PassRequest req = action.PassParams;
            req.TeamId      = ctx.AgentTeamId;
            req.FrameNumber = ctx.CurrentFrame;

            // FM-DT-10 (§3.5.9): pre-dispatch assertion on IntendedDistance.
            // Project NaN-gate pattern (!(d > 0f) — a plain d <= 0f passes NaN).
            if (!(req.IntendedDistance > 0.0f) || float.IsInfinity(req.IntendedDistance))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[DT] {DecisionTreeConstants.WarnFmDt10} PASS IntendedDistance {req.IntendedDistance} invalid for agent {action.AgentId}; recomputed");
#endif
                req.IntendedDistance = Vector2.Distance(ctx.AgentPosition, action.TargetPosition);
            }

            if (passExecutor == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] PASS dispatch with null PassExecutor for agent {action.AgentId} — orchestrator wiring missing (sanctioned only in test/bootstrap contexts)");
#endif
                return;
            }

            // §3.5.2: the DT does not inspect the result — PassExecutor owns all
            // failure modes (FM-DT-13: possession loss is rejected at its entry).
            passExecutor.Execute(in req);
        }

        // ── §3.5.3 SHOOT ───────────────────────────────────────────────────────

        private static void DispatchShoot(
            AgentAction action, in DecisionContext ctx, ShotExecutor shotExecutor)
        {
            ShotRequest req  = action.ShotParams;
            req.TeamId       = ctx.AgentTeamId;
            req.FrameNumber  = ctx.CurrentFrame;

            // FM-DT-11 (§3.5.9): pre-dispatch assertion on DistanceToGoal (NaN-gate form).
            if (!(req.DistanceToGoal > 0.0f) || float.IsInfinity(req.DistanceToGoal))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[DT] {DecisionTreeConstants.WarnFmDt11} SHOOT DistanceToGoal {req.DistanceToGoal} invalid for agent {action.AgentId}; recomputed");
#endif
                req.DistanceToGoal = Vector2.Distance(ctx.AgentPosition, ctx.OpponentGoalCentre);
            }

            // FM-DT-12 (§3.5.9): pre-dispatch clamp on PlacementTarget components.
            // Clamp01 passes NaN (project NaN-gate pattern), so non-finite components
            // are sanitised to goal centre (0.5) before the range clamp.
            Vector2 placement = req.PlacementTarget;
            float u = SanitisePlacementComponent(placement.x);
            float v = SanitisePlacementComponent(placement.y);
            if (u != placement.x || v != placement.y)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] {DecisionTreeConstants.WarnFmDt12} SHOOT PlacementTarget {placement} clamped for agent {action.AgentId}");
#endif
                req.PlacementTarget = new Vector2(u, v);
            }

            if (shotExecutor == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] SHOOT dispatch with null ShotExecutor for agent {action.AgentId} — orchestrator wiring missing (sanctioned only in test/bootstrap contexts)");
#endif
                return;
            }

            shotExecutor.Execute(in req);
        }

        // FM-DT-12 helper: non-finite → 0.5 (goal centre); else clamp to [0, 1].
        private static float SanitisePlacementComponent(float c)
        {
            if (float.IsNaN(c) || float.IsInfinity(c)) return 0.5f;
            return Mathf.Clamp01(c);
        }

        // ── §3.5.4 DRIBBLE ─────────────────────────────────────────────────────

        private static void DispatchDribble(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // DRIBBLE: JOGGING state, AUTO_ALIGN facing (§3.5.4)
            MovementCommand cmd = MovementCommand.MoveTo(action.TargetPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.5 HOLD ────────────────────────────────────────────────────────

        private static void DispatchHold(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // HOLD: TARGET_LOCK facing toward ball (§3.5.5).
            // SPEC-DEVIATION NOTE (Stage 0): §3.5.5 requests DesiredState = IDLE;
            // the implemented shape is StrafeWhileWatching at the agent's own position
            // (JOGGING strafe with a degenerate target offset). Agent Movement #2
            // AR-13 explicitly accommodates this shape — the degenerate offset keeps
            // resting agents at rest — so the composed behaviour matches §3.5.5 intent
            // (stationary, ball-watching) without a dedicated IDLE+TARGET_LOCK factory.
            Vector2 ballPos = ctx.MatchContext.BallPosition;
            MovementCommand cmd = MovementCommand.StrafeWhileWatching(ctx.AgentPosition, ballPos);
            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.6 MOVE_TO_POSITION ────────────────────────────────────────────

        private static void DispatchMove(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            float dist = Vector2.Distance(ctx.AgentPosition, action.TargetPosition);

            // §3.5.6 tiered pace: ≥15m sprint / ≥6m jog / below: WALKING toward the
            // slot. AR-2 M-5: the near band previously issued Stop(currentPosition),
            // so agents within 6 m of their slot never closed the residual offset —
            // §3.5.6 specifies WALKING ("minor adjustment"), expressible since the
            // AM AR-13 WalkTo factory landed.
            MovementCommand cmd;
            if (dist >= TacticalWeights.MoveSprintThreshold)
                cmd = MovementCommand.SprintUrgent(action.TargetPosition);
            else if (dist >= TacticalWeights.MoveJogThreshold)
                cmd = MovementCommand.MoveTo(action.TargetPosition);
            else
                cmd = MovementCommand.WalkTo(action.TargetPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.7 PRESS ───────────────────────────────────────────────────────

        private static void DispatchPress(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // PRESS: SPRINTING, EMERGENCY braking, TARGET_LOCK on the press target
            // (§3.5.7; UT-DP-04). AR-2 M-5: previously SprintUrgent —
            // CONTROLLED + AUTO_ALIGN — losing both the emergency-stop licence and
            // the eyes-on-ball-carrier facing.
            MovementCommand cmd = MovementCommand.PressSprint(action.TargetPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.8 INTERCEPT ───────────────────────────────────────────────────

        private static void DispatchIntercept(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // INTERCEPT: SPRINTING to the intercept point, CONTROLLED stop (protects
            // first-touch quality), TARGET_LOCK watching the BALL (§3.5.8).
            // AR-2 M-5: previously SprintUrgent (AUTO_ALIGN — ball not watched).
            MovementCommand cmd = MovementCommand.SprintWhileWatching(
                action.TargetPosition, ctx.MatchContext.BallPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2 fix pass. H-1: PassExecutor/ShotExecutor are instance classes —    |
// |         |            |        |   the static calls never compiled (CS0120); executors now injected through     |
// |         |            |        |   Dispatch (null-tolerant for test/bootstrap, logged as wiring failure).       |
// |         |            |        |   M-5: PRESS → PressSprint (EMERGENCY + TARGET_LOCK, §3.5.7/UT-DP-04);        |
// |         |            |        |   INTERCEPT → SprintWhileWatching ball (CONTROLLED + TARGET_LOCK, §3.5.8);     |
// |         |            |        |   MOVE near band → WalkTo (§3.5.6 WALKING, was Stop). M-10: FM-DT-10/11/12     |
// |         |            |        |   pre-dispatch assertions implemented (NaN-gate pattern); unknown-type default |
// |         |            |        |   branch now produces the §3.5.9 HOLD-safe command under FM-DT-14 (renumbered  |
// |         |            |        |   from the double-allocated FM-DT-09, ERR-008-007). All emits FR-CS-031 gated. |
// |         |            |        |   L: HOLD JOGGING-strafe SPEC-DEVIATION NOTE (AM AR-13 accommodation).         |
#endregion
