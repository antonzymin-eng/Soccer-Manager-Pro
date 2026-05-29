// File:     src/attacking-ai/RoleAssigner.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.3–§3.4, §3.12, FR-AT-003, FR-AT-017, Code Standards #20
// Purpose:  Iterates the attacking pool EntityId-ascending and assigns each agent a role using
//           the four-priority order (§3.3). Stable agents retain their current role (§3.12).
//           Stable runners ARE counted against MAX_RUNNERS before non-stable assignment begins.
//           Generates RunParameters for every committed RUNNER (stable or newly assigned).

using System;
using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Role-assignment pipeline (§3.3) and run-parameter generation (§3.4). Pure static.
    /// Iterates <paramref name="pool"/> EntityId-ascending (FR-AT-003 / #16 §3.2.5).
    /// Applies hysteresis (§3.12): stable agents retain their role; non-stable agents are
    /// evaluated with the four-priority order and their hysteresis state is updated.
    /// Attacking AI #15 §3.3–§3.4 / §3.12.
    /// </summary>
    internal static class RoleAssigner
    {
        /// <summary>
        /// Assigns roles to all pool entries, updating their
        /// <see cref="AttackPoolEntry.AssignedRole"/> and run-parameter fields in place.
        /// Also updates <paramref name="hysteresis"/> for each entity.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot (ball carrier position, ball position).</param>
        /// <param name="pool">Attacking pool; EntityId-ascending; entries are mutated in place.</param>
        /// <param name="poolCount">Number of valid entries in <paramref name="pool"/>.</param>
        /// <param name="styleProfile">Active team-style profile.</param>
        /// <param name="hysteresis">Per-entity hysteresis state array; indexed by EntityId.</param>
        /// <param name="cosAngle">cos(teamAttackAngle), pre-computed once per tick.</param>
        /// <param name="sinAngle">sin(teamAttackAngle), pre-computed once per tick.</param>
        /// <param name="currentTick">Current tick index.</param>
        public static void Assign(
            AttackingSnapshot        snapshot,
            AttackPoolEntry[]        pool,
            int                      poolCount,
            StyleProfile             styleProfile,
            AttackHysteresisState[]  hysteresis,
            float                    cosAngle,
            float                    sinAngle,
            int                      currentTick)
        {
            int maxRunners = styleProfile.MaxRunners;

            // Pass 1: count stable agents' roles to correctly seed runnerCount and weakSideCount
            // before evaluating non-stable agents. This prevents stable RUNNERs from being
            // invisible to the MAX_RUNNERS cap when processing non-stable agents.
            int runnerCount   = 0;
            int weakSideCount = 0;
            for (int i = 0; i < poolCount; i++)
            {
                int eid = pool[i].EntityId;
                ref AttackHysteresisState hyst = ref hysteresis[eid];
                if (AttackHysteresis.IsStable(ref hyst))
                {
                    if (hyst.CurrentRole == AttackRole.Runner)   runnerCount++;
                    if (hyst.CurrentRole == AttackRole.WeakSide) weakSideCount++;
                }
            }

            // Pass 2: assign roles (EntityId-ascending pass already enforced by pool sort).
            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry   entry = ref pool[i];
                int                   eid   = entry.EntityId;
                ref AttackHysteresisState hyst = ref hysteresis[eid];

                if (AttackHysteresis.IsStable(ref hyst))
                {
                    // Retain committed role; increment dwell counter without changing role.
                    entry.AssignedRole = hyst.CurrentRole;
                    AttackHysteresis.Update(ref hyst, hyst.CurrentRole);

                    // Stable RUNNERs still need fresh RunParameters each tick.
                    if (hyst.CurrentRole == AttackRole.Runner)
                    {
                        GenerateRunParams(ref entry, snapshot, cosAngle, sinAngle,
                                          currentTick, styleProfile);
                    }
                    continue;
                }

                // Evaluate candidate role (priority order §3.3).
                AttackRole candidate = EvaluateCandidate(
                    ref entry, snapshot, poolCount, styleProfile,
                    runnerCount, maxRunners, weakSideCount);

                // Update hysteresis; CurrentRole may change on transition commit.
                AttackHysteresis.Update(ref hyst, candidate);
                entry.AssignedRole = hyst.CurrentRole;

                // Update counts based on committed (post-hysteresis) role.
                if (hyst.CurrentRole == AttackRole.Runner)
                {
                    runnerCount++;
                    GenerateRunParams(ref entry, snapshot, cosAngle, sinAngle,
                                      currentTick, styleProfile);
                }
                else if (hyst.CurrentRole == AttackRole.WeakSide)
                {
                    weakSideCount++;
                }
            }
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private static AttackRole EvaluateCandidate(
            ref AttackPoolEntry    entry,
            AttackingSnapshot      snapshot,
            int                    poolCount,
            StyleProfile           styleProfile,
            int                    runnerCount,
            int                    maxRunners,
            int                    weakSideCount)
        {
            // Priority a: RUNNER — line must be MIDFIELD or ATTACK; cap must not be met.
            if ((entry.Line == LineId.Midfield || entry.Line == LineId.Attack)
                && runnerCount < maxRunners)
            {
                return AttackRole.Runner;
            }

            // Priority b: SUPPORT_BALL — within effective support radius.
            if (SupportHeuristic.IsWithinSupportRadius(
                    entry.Position, snapshot.BallCarrierPosition, styleProfile))
            {
                return AttackRole.SupportBall;
            }

            // Priority c: WEAK_SIDE — pool large enough, slot not yet filled, agent on far side.
            if (poolCount >= AttackingAIConstants.MinWeakSideAgentThreshold
                && weakSideCount == 0
                && IsOnWeakSide(entry.Position.y, snapshot.BallPosition.y))
            {
                return AttackRole.WeakSide;
            }

            // Priority d: HOLD_WIDTH (default).
            return AttackRole.HoldWidth;
        }

        // ── Run parameter generation (§3.4) ─────────────────────────────────────

        private static void GenerateRunParams(
            ref AttackPoolEntry   entry,
            AttackingSnapshot     snapshot,
            float                 cosAngle,
            float                 sinAngle,
            int                   currentTick,
            StyleProfile          styleProfile)
        {
            // Step 1: Raw offsets.
            float depthRaw    = AttackingAIConstants.BaseRunDepthM * styleProfile.DepthMult;
            float depthOffset = Mathf.Clamp(depthRaw,
                                    AttackingAIConstants.MinRunDepthM,
                                    AttackingAIConstants.MaxRunDepthM);

            float centeredPct   = entry.LateralPct - 0.5f;
            float lateralRaw    = centeredPct * AttackingAIConstants.PITCH_WIDTH_M
                                  * AttackingAIConstants.LateralScale;
            float lateralOffset = Mathf.Clamp(lateralRaw,
                                    -AttackingAIConstants.MaxLateralOffsetM,
                                     AttackingAIConstants.MaxLateralOffsetM);

            float delayRaw    = AttackingAIConstants.BaseRunTriggerDelayTicks * styleProfile.TimingMult;
            int   delay       = Math.Max(1, Mathf.RoundToInt(delayRaw));
            int   triggerTick = currentTick + delay;

            // Step 2: Run target in pitch-frame.
            // depthVec = (cosAngle, sinAngle) * depthOffset
            // lateralVec = (-sinAngle, cosAngle) * lateralOffset  (90° CCW rotation)
            float targetX = snapshot.BallCarrierPosition.x
                            + cosAngle  * depthOffset
                            + (-sinAngle) * lateralOffset;
            float targetY = snapshot.BallCarrierPosition.y
                            + sinAngle  * depthOffset
                            + cosAngle  * lateralOffset;

            // Step 3: Clamp to pitch boundary.
            targetX = Mathf.Clamp(targetX, 0f, AttackingAIConstants.PITCH_LENGTH_M);
            targetY = Mathf.Clamp(targetY, 0f, AttackingAIConstants.PITCH_WIDTH_M);

            entry.HasRunParams       = true;
            entry.DepthOffsetM       = depthOffset;
            entry.LateralOffsetM     = lateralOffset;
            entry.RunTriggerTick     = triggerTick;
            entry.RunTargetPosition  = new Vector2(targetX, targetY);
        }

        // Agents on the "weak side" are on the opposite pitch half from the ball (§3.3 / §3.7).
        private static bool IsOnWeakSide(float agentY, float ballY)
        {
            float halfWidth = AttackingAIConstants.PITCH_WIDTH_M * 0.5f;
            return (ballY >= halfWidth) ? (agentY < halfWidth) : (agentY >= halfWidth);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-2: replaced magic literals with catalogue constants (MinRunDepthM, MaxRunDepthM, MaxLateralOffsetM). AR-1 M-3: Math.Round(double) → Mathf.RoundToInt. |
#endregion
