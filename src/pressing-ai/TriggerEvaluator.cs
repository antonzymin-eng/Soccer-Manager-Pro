// File:     src/pressing-ai/TriggerEvaluator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.1–§3.2, Code Standards #20
// Purpose:  Pure static class: evaluates all four raw press-trigger conditions and
//           applies debounce hysteresis to produce committed TriggerFlags.

using UnityEngine;

using TacticalDirector.PassMechanics;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Evaluates the four canonical press triggers (§3.1) and runs the debounce
    /// algorithm (§3.2) to produce committed <see cref="TriggerFlags"/>.
    /// All methods are pure / side-effect-free on game state; callers mutate
    /// <see cref="PressTrigger"/> in place. Pressing AI #13 §3.1–§3.2.
    /// </summary>
    public static class TriggerEvaluator
    {
        /// <summary>
        /// Evaluates raw trigger conditions and updates debounce counters in <paramref name="state"/>.
        /// Returns the set of committed trigger flags after debounce.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="latestPass">Most recent pass event (valid = true when available).</param>
        /// <param name="hasLatestPass">True when <paramref name="latestPass"/> contains a valid event.</param>
        /// <param name="state">Persistent debounce state; mutated in place.</param>
        /// <returns>Committed <see cref="TriggerFlags"/> after debounce.</returns>
        public static TriggerFlags Evaluate(
            PressingSnapshot snapshot,
            PassAttemptEvent latestPass,
            bool hasLatestPass,
            ref PressTrigger state)
        {
            bool rawBadTouch     = EvaluateBadTouch(snapshot);
            bool rawBackwardPass = hasLatestPass && EvaluateBackwardPass(snapshot, latestPass);
            bool rawSidelineTrap = EvaluateSidelineTrap(snapshot);
            bool rawWeakReceiver = EvaluateWeakReceiver(snapshot);

            UpdateCounter(rawBadTouch,     ref state.BadTouchDwell,     ref state.BadTouchRelease);
            UpdateCounter(rawBackwardPass, ref state.BackwardPassDwell,  ref state.BackwardPassRelease);
            UpdateCounter(rawSidelineTrap, ref state.SidelineTrapDwell,  ref state.SidelineTrapRelease);
            UpdateCounter(rawWeakReceiver, ref state.WeakReceiverDwell,  ref state.WeakReceiverRelease);

            TriggerFlags committed = TriggerFlags.None;

            if (state.BadTouchDwell     >= PressingAIConstants.TriggerDwellTicks)
                committed |= TriggerFlags.BadTouch;
            if (state.BackwardPassDwell >= PressingAIConstants.TriggerDwellTicks)
                committed |= TriggerFlags.BackwardPass;
            if (state.SidelineTrapDwell >= PressingAIConstants.TriggerDwellTicks)
                committed |= TriggerFlags.SidelineTrap;
            if (state.WeakReceiverDwell >= PressingAIConstants.TriggerDwellTicks)
                committed |= TriggerFlags.WeakReceiver;

            return committed;
        }

        // ── §3.1.1 BadTouch ─────────────────────────────────────────────────

        internal static bool EvaluateBadTouch(PressingSnapshot snapshot)
        {
            // Find the ball carrier among pressing team's opponents.
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
                return false;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId != carrierId)
                    continue;
                if (!a.IsActive)
                    return false;

                return a.LastTouchQuality < PressingAIConstants.BadTouchThreshold
                    && a.PostTouchBallSpeed > PressingAIConstants.BadTouchVelocityMS;
            }

            return false;
        }

        // ── §3.1.2 BackwardPass ──────────────────────────────────────────────

        internal static bool EvaluateBackwardPass(PressingSnapshot snapshot, PassAttemptEvent evt)
        {
            // Resolve passer position.
            Vector2 passerPos = Vector2.zero;
            bool found = false;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == evt.AgentId)
                {
                    if (!a.IsActive)
                        break;
                    passerPos = a.Position;
                    found     = true;
                    break;
                }
            }

            if (!found)
                return false;

            Vector2 toTarget = new Vector2(
                evt.TargetPosition.x - passerPos.x,
                evt.TargetPosition.y - passerPos.y);

            float len = toTarget.magnitude;
            if (len * len < PressingAIConstants.SpacingEpsilonM2)
                return false;

            Vector2 passDir = toTarget / len;
            float dot = Vector2.Dot(passDir, snapshot.AttackingDirection);
            return dot < PressingAIConstants.BackwardPassThreshold;
        }

        // ── §3.1.3 SidelineTrap ──────────────────────────────────────────────

        internal static bool EvaluateSidelineTrap(PressingSnapshot snapshot)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
                return false;

            float ballY = snapshot.BallPosition.y;
            float yToBottom = ballY;
            float yToTop    = PressingAIConstants.PITCH_WIDTH_M - ballY;
            float nearSide  = yToBottom < yToTop ? yToBottom : yToTop;

            if (nearSide >= PressingAIConstants.SidelineTrapDistanceM)
                return false;

            // Direction toward the nearer touchline.
            Vector2 sidelineDir = yToBottom < yToTop
                ? new Vector2(0f, -1f)
                : new Vector2(0f,  1f);

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId != carrierId)
                    continue;
                if (!a.IsActive)
                    return false;

                float dot = Vector2.Dot(a.Facing, sidelineDir);
                return dot > 0f;
            }

            return false;
        }

        // ── §3.1.4 WeakReceiver ──────────────────────────────────────────────

        internal static bool EvaluateWeakReceiver(PressingSnapshot snapshot)
        {
            int carrierId      = snapshot.BallCarrierEntityId;
            int pressingTeamId = snapshot.PressingTeamId;

            // Identify the opposing GK to exclude.
            int opposingGkId = -1;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != pressingTeamId && a.IsGoalkeeper)
                {
                    opposingGkId = a.EntityId;
                    break;
                }
            }

            float radiusSq = PressingAIConstants.CoverShadowCandidateRadiusM
                           * PressingAIConstants.CoverShadowCandidateRadiusM;

            // Check each opponent receiver (not carrier, not GK, not inactive).
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot r = ref snapshot.Agents[i];
                if (r.TeamId == pressingTeamId)
                    continue;
                if (!r.IsActive)
                    continue;
                if (r.EntityId == carrierId || r.EntityId == opposingGkId)
                    continue;
                if (r.IsGoalkeeper)
                    continue;

                if (r.FirstTouchAttribute >= PressingAIConstants.WeakReceiverThreshold)
                    continue;

                float pressure = ComputeGeometricPressure(r.Position, snapshot, pressingTeamId, radiusSq);
                if (pressure >= PressingAIConstants.WeakReceiverPressure)
                    return true;
            }

            return false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Counts own-team outfield agents within <paramref name="radiusSq"/> of <paramref name="pos"/>
        /// and normalises by ThreatPressureNormalizer, clamped to [0, 1].
        /// §3.4 geometric pressure formula.
        /// </summary>
        internal static float ComputeGeometricPressure(
            Vector2 pos,
            PressingSnapshot snapshot,
            int pressingTeamId,
            float radiusSq)
        {
            int count = 0;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot d = ref snapshot.Agents[i];
                if (d.TeamId != pressingTeamId)
                    continue;
                if (!d.IsActive)
                    continue;
                if (d.IsGoalkeeper)
                    continue;

                float dx = d.Position.x - pos.x;
                float dy = d.Position.y - pos.y;
                if (dx * dx + dy * dy <= radiusSq)
                    count++;
            }

            float pressure = count / PressingAIConstants.ThreatPressureNormalizer;
            return pressure > 1f ? 1f : pressure;
        }

        /// <summary>
        /// Updates a single trigger's dwell/release counters per the §3.2 debounce algorithm.
        /// </summary>
        private static void UpdateCounter(bool rawCondition, ref int dwell, ref int release)
        {
            if (rawCondition)
            {
                dwell = dwell + 1;
                if (dwell > PressingAIConstants.TriggerDwellTicks)
                    dwell = PressingAIConstants.TriggerDwellTicks;
                release = 0;
            }
            else
            {
                release = release + 1;
                if (release > PressingAIConstants.TriggerReleaseTicks)
                    release = PressingAIConstants.TriggerReleaseTicks;

                if (release >= PressingAIConstants.TriggerReleaseTicks)
                    dwell = 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-2: fixed unit mismatch in EvaluateBackwardPass (len*len vs len). AR-1 H-1: added IsActive guards in BadTouch, BackwardPass, SidelineTrap, WeakReceiver, ComputeGeometricPressure. |
#endregion
