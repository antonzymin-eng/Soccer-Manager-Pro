// File:     src/decision-tree/ActionDispatcher.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.5, Code Standards #20
// Purpose:  Step 6 of the 6-step pipeline. Routes the selected AgentAction to the
//           appropriate execution system: PassExecutor (PASS), ShotExecutor (SHOOT),
//           or IDtMovementController (DRIBBLE/HOLD/MOVE/PRESS/INTERCEPT). §3.5.1.

using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 6: routes AgentAction to execution systems. Has side effects.
    /// Decision Tree #8 §3.5.
    /// </summary>
    internal static class ActionDispatcher
    {
        /// <summary>
        /// Dispatches action to the appropriate execution system.
        /// PassExecutor and ShotExecutor are called directly (static entry points).
        /// Movement actions are routed via IDtMovementController (§3.5 XC-3.5-10).
        /// §3.5.1.
        /// </summary>
        internal static void Dispatch(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController movementController)
        {
            switch (action.Type)
            {
                case ActionType.PASS:
                    DispatchPass(action, in ctx);
                    break;

                case ActionType.SHOOT:
                    DispatchShoot(action, in ctx);
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
                    Debug.LogWarning($"[DT] Unknown ActionType {action.Type} for agent {action.AgentId}");
                    break;
            }
        }

        // ── §3.5.2 PASS ────────────────────────────────────────────────────────

        private static void DispatchPass(AgentAction action, in DecisionContext ctx)
        {
            // Finalise TeamId and FrameNumber fields that weren't available at SelectAction time
            PassRequest req = action.PassParams;
            req.TeamId      = ctx.AgentTeamId;
            req.FrameNumber = ctx.CurrentFrame;

            PassExecutor.Execute(req);
        }

        // ── §3.5.3 SHOOT ───────────────────────────────────────────────────────

        private static void DispatchShoot(AgentAction action, in DecisionContext ctx)
        {
            ShotRequest req  = action.ShotParams;
            req.TeamId       = ctx.AgentTeamId;
            req.FrameNumber  = ctx.CurrentFrame;

            ShotExecutor.Execute(req);
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
            // HOLD: IDLE state, TARGET_LOCK facing toward ball (§3.5.5)
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

            MovementCommand cmd;
            if (dist >= TacticalWeights.MoveSprintThreshold)
                cmd = MovementCommand.SprintUrgent(action.TargetPosition);
            else if (dist >= TacticalWeights.MoveJogThreshold)
                cmd = MovementCommand.MoveTo(action.TargetPosition);
            else
                cmd = MovementCommand.Stop(ctx.AgentPosition);   // already near slot

            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.7 PRESS ───────────────────────────────────────────────────────

        private static void DispatchPress(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // PRESS: SPRINTING, AUTO_ALIGN (§3.5.7)
            MovementCommand cmd = MovementCommand.SprintUrgent(action.TargetPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }

        // ── §3.5.8 INTERCEPT ───────────────────────────────────────────────────

        private static void DispatchIntercept(
            AgentAction action,
            in DecisionContext ctx,
            IDtMovementController mc)
        {
            // INTERCEPT: SPRINTING toward intercept point (§3.5.8)
            MovementCommand cmd = MovementCommand.SprintUrgent(action.TargetPosition);
            mc.SubmitCommand(action.AgentId, cmd);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
