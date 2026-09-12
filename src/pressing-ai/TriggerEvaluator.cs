// File:     src/pressing-ai/TriggerEvaluator.cs
// Created:  2026-05-29
// Modified: 2026-09-11 (W5 second review / ERR-013-011: consume only the prior completed 60 Hz stride and latch a qualifying discrete pass only through its required two-heartbeat dwell)
// Modified: 2026-06-15
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
            bool rawBadTouch = EvaluateBadTouch(snapshot);

            // ERR-013-011: the retained ring event is stamped in the 60 Hz EventBus clock,
            // while snapshot.TickIndex is a 10 Hz tactical heartbeat. AI runs before Resolve/Events,
            // so only the previous completed physics interval [windowStart, PhysicsTick) may START
            // BACKWARD_PASS dwell.
            bool freshBackwardPass = hasLatestPass
                && latestPass.Tick >= snapshot.PassEventWindowStartTick
                && latestPass.Tick < snapshot.PhysicsTick
                && EvaluateBackwardPass(snapshot, latestPass);

            // BACKWARD_PASS is a one-shot event but §3.2 requires two tactical heartbeats of dwell.
            // Once a qualifying event starts dwell, keep that already-started dwell raw-true only
            // until it reaches the threshold. A stale retained ring entry cannot start another dwell.
            bool pendingBackwardPassDwell = state.BackwardPassDwell > 0
                && state.BackwardPassDwell < PressingAIConstants.TriggerDwellTicks;
            bool rawBackwardPass = freshBackwardPass || pendingBackwardPassDwell;

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

                // §3.1.2 F2: explicitly suppress on non-finite inputs. The `<`/`>`
                // comparisons already evaluate false for NaN, but the explicit gate makes
                // the suppression contract robust against future refactors that might
                // invert a comparison.
                if (float.IsNaN(a.LastTouchQuality) || float.IsNaN(a.PostTouchBallSpeed))
                    return false;

                return a.LastTouchQuality < PressingAIConstants.BadTouchThreshold
                    && a.PostTouchBallSpeed > PressingAIConstants.BadTouchVelocityMS;
            }

            return false;
        }

        // ── §3.1.2 BackwardPass ──────────────────────────────────────────────

        internal static bool EvaluateBackwardPass(PressingSnapshot snapshot, PassAttemptEvent evt)
        {
            // Resolve passer position. W5 routes only OPPONENT passes into this team's ring,
            // matching #13 §4.4.2. Keep the passer-team check as a fail-safe against a malformed/manual
            // ring write: an own-team pass must never become a press trigger.
            Vector2 passerPos = Vector2.zero;
            bool found = false;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == evt.AgentId)
                {
                    if (!a.IsActive)
                        break;
                    if (a.TeamId == snapshot.PressingTeamId)
                        break; // own-team pass — not a press trigger
                    passerPos = a.Position;
                    found     = true;
                    break;
                }
            }

            if (!found)
                return false;

            // The EventBus/ring retains the authoritative WORLD-FRAME event. Pressing snapshots are
            // canonicalized so the acting pressing team attacks +X; normalize only the point consumed
            // by this geometric comparison. Keeping the stored struct homogeneous avoids a serialized
            // hybrid where TargetPosition is canonical but FinalVelocity/FinalSpin remain world-frame.
            float targetX = evt.TargetPosition.x;
            float targetY = evt.TargetPosition.y;
            if (snapshot.PressingTeamId == 1)
            {
                targetX = PressingAIConstants.PITCH_LENGTH_M - targetX;
                targetY = PressingAIConstants.PITCH_WIDTH_M - targetY;
            }

            // §3.1.2 F2: suppress on non-finite passer/target coordinates.
            if (float.IsNaN(passerPos.x) || float.IsNaN(passerPos.y)
                || float.IsNaN(targetX) || float.IsNaN(targetY))
                return false;

            Vector2 toTarget = new Vector2(
                targetX - passerPos.x,
                targetY - passerPos.y);

            float len = toTarget.magnitude;
            if (len * len < PressingAIConstants.SpacingEpsilonM2)
                return false;

            // AR-3 H (ERR-013-009): "backward" is relative to the BALL-CARRIER's (possessing
            // team's) attacking direction, which is the opposite of the pressing team's.
            // snapshot.AttackingDirection is the pressing team's (its XML doc; also the frame
            // the §3.8/§3.9 zone checks require), so negate it here to get the possessing
            // team's forward. A backward pass therefore moves opposite the possessing team's
            // attack — i.e., toward the pressing team's attacking half / the possessing team's
            // own goal — and dot(passDir, carrierForward) < threshold fires.
            Vector2 carrierForward = -snapshot.AttackingDirection;
            Vector2 passDir = toTarget / len;
            float dot = Vector2.Dot(passDir, carrierForward);
            return dot < PressingAIConstants.BackwardPassThreshold;
        }

        // ── §3.1.3 SidelineTrap ──────────────────────────────────────────────

        internal static bool EvaluateSidelineTrap(PressingSnapshot snapshot)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
                return false;

            float ballY = snapshot.BallPosition.y;

            // §3.1.2 F2: a NaN ballY would slip past the `nearSide >= distance` early-out
            // (NaN comparisons are false) and fall through to the facing test, which could
            // spuriously fire. Suppress explicitly.
            if (float.IsNaN(ballY))
                return false;

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

                // §3.1.2 F2: a NaN first-touch attribute fails the `>=` weak-gate (NaN
                // comparisons are false), which would let it fall through as "weak" and
                // possibly fire. Treat non-finite skill as not-weak and skip.
                if (float.IsNaN(r.FirstTouchAttribute)
                    || r.FirstTouchAttribute >= PressingAIConstants.WeakReceiverThreshold)
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
// | 1.2     | 2026-06-15 | —      | AR-2 L-1: explicit §3.1.2 F2 NaN suppression — BadTouch (touch/speed), BackwardPass (positions), SidelineTrap (ballY, which previously could fall through to a spurious fire), WeakReceiver (first-touch attribute, likewise). |
// | 1.3     | 2026-06-15 | —      | AR-3 H (ERR-013-009): BackwardPass now evaluates the possessing team's frame (negated AttackingDirection); a pressing-team passer is ignored. Corrects the home/away inversion class. |
// | 1.4     | 2026-09-11 | —      | W5 doc alignment: the production ring is opponent-routed; retained the own-team passer guard as defensive validation. |
// | 1.5     | 2026-09-11 | —      | ERR-013-011 / PR #398 review: require latestPass.Tick == snapshot.TickIndex and normalize the world-frame target only at BackwardPass evaluation, keeping the retained event frame-homogeneous. |
// | 1.6     | 2026-09-11 | —      | ERR-013-011 second review: replace impossible 60 Hz == 10 Hz equality with [windowStart,physicsTick) acceptance plus bounded discrete-event dwell completion. |
#endregion
