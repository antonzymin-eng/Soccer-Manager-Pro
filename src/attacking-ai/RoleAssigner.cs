// File:     src/attacking-ai/RoleAssigner.cs
// Created:  2026-05-29
// Modified: 2026-06-15
// Author:   —
// Spec:     Attacking AI #15 §3.3–§3.4, §3.12, FR-AT-003, FR-AT-017, Code Standards #20
// Purpose:  Iterates the attacking pool EntityId-ascending (single pass) and assigns each agent a
//           role using the four-priority order (§3.3). Every agent is re-evaluated every tick;
//           the §3.12 dwell hysteresis (AttackHysteresis.Update) retains the committed role across
//           boundary oscillation without skipping evaluation. Counts on the committed role so the
//           MAX_RUNNERS cap is seeded for later agents. Generates RunParameters for every RUNNER.

using System;
using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Role-assignment pipeline (§3.3) and run-parameter generation (§3.4). Pure static.
    /// Iterates <paramref name="pool"/> EntityId-ascending (FR-AT-003 / #16 §3.2.5) in a single
    /// pass. Every agent is evaluated with the four-priority order every tick; the §3.12 dwell
    /// hysteresis (<see cref="AttackHysteresis.Update"/>) retains the committed role across
    /// boundary oscillation without skipping evaluation (no is-stable short-circuit — that would
    /// permanently lock a role; ERR-015-009).
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

            // Single EntityId-ascending pass (§3.13 step 4). EVERY agent is evaluated every
            // tick — there is no "is-stable, skip evaluation" short-circuit. The §3.12
            // anti-thrash hysteresis lives entirely in AttackHysteresis.Update(): a role
            // transition commits only after a new candidate has been preferred for
            // ATTACK_DWELL_TICKS consecutive ticks, so CurrentRole is retained across the
            // boundary without skipping evaluation. Skipping evaluation while "stable" would
            // permanently lock an agent's role, because the candidate-dwell machinery could
            // never observe a newly-preferred role once DwellCounter crossed the threshold
            // (ERR-015-009). Counting on the committed (post-hysteresis) role also seeds the
            // MAX_RUNNERS cap correctly for later agents — including retained RUNNERs whose
            // candidate is mid-transition (ERR-015-007); residual over-cap from intra-tick
            // ordering is corrected by the §3.11 invariant pass.
            int runnerCount   = 0;
            int weakSideCount = 0;
            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry   entry = ref pool[i];
                ref AttackHysteresisState hyst = ref hysteresis[entry.EntityId];

                // Evaluate the preferred role (four-priority order §3.3) and feed it to the
                // dwell gate. A retained RUNNER re-prefers RUNNER while its slot is free,
                // refreshing its dwell; a displaced one begins a transition but stays RUNNER
                // (and counts) until the candidate commits.
                AttackRole candidate = EvaluateCandidate(
                    ref entry, snapshot, poolCount, styleProfile,
                    runnerCount, maxRunners, weakSideCount);

                // Update hysteresis; CurrentRole changes only on a committed transition.
                AttackHysteresis.Update(ref hyst, candidate);
                entry.AssignedRole = hyst.CurrentRole;

                // Count and parameterise based on the committed (post-hysteresis) role.
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
// | 1.2     | 2026-06-15 | —      | AR-4 H-1 (ERR-015-009): removed the is-stable short-circuit that skipped re-evaluation — once DwellCounter crossed the threshold an agent's role was permanently locked because the candidate-dwell machinery never saw a new preferred role. Collapsed to a single always-evaluate pass; hysteresis now lives entirely in Update(). Supersedes the AR-4 M-2 two-pass form (ERR-015-007) — stable runners are still counted because counting is on the committed role. |
#endregion
