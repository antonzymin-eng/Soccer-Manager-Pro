// File:     src/defensive-ai/MarkAssigner.cs
// Created:  2026-05-29
// Modified: 2026-07-07
// Author:   —
// Spec:     Defensive AI #14 §3.3, §3.4, §3.5, §4.3, Code Standards #20; Tactical Instructions #21 §3.4 (FR-TI-033)
// Purpose:  Pure static module: implements the mark-mode assignment algorithm (§3.3),
//           displacement cost (§3.4), and threat score (§3.5) for the HOLD_SHAPE pool.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Computes per-agent mark assignments for the HOLD_SHAPE pool (§3.3–§3.5).
    /// Priority: INTERCEPT_RUNNER &gt; MAN_MARK &gt; ZONAL (§3.3.2).
    /// Ties broken first by lowest displacement cost, then by lowest EntityId (FR-DA-014).
    /// Pure static; no side effects except via ref parameters. Defensive AI #14 §3.3–§3.5 / §4.3.
    /// </summary>
    public static class MarkAssigner
    {
        // ── Public entry ──────────────────────────────────────────────────────

        /// <summary>
        /// Runs the regular assignment loop (§3.3) for all pool agents not already
        /// overridden by emergency steps (§3.13 Steps 4/4a).
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid pool entries.</param>
        /// <param name="currentTick">Tick index for ValidThroughTick stamping.</param>
        /// <param name="assignments">Output assignments buffer (one slot per pool agent).</param>
        /// <param name="hysteresis">Per-agent hysteresis state array (one slot per pool agent).</param>
        public static void Assign(
            DefensiveSnapshot      snapshot,
            int[]                  poolBuffer,
            int                    poolCount,
            int                    currentTick,
            MarkAssignment[]       assignments,
            MarkHysteresisState[]  hysteresis)
        {
            bool defendsX0 = LastManDetector.DefendsX0(snapshot.DefensiveTeamId);

            for (int p = 0; p < poolCount; p++)
            {
                // Skip agents already set by emergency overrides (Steps 4/4a).
                if (assignments[p].OverriddenThisTick)
                    continue;

                int agentId = poolBuffer[p];
                int aIdx    = HoldShapePoolFilter.SnapshotIndexOf(snapshot, agentId);
                if (aIdx < 0)
                    continue;

                ref readonly DefensiveAgentSnapshot agent = ref snapshot.Agents[aIdx];

                // Hysteresis pre-check (§3.11): retain current assignment during dwell lock.
                // ValidThroughTick must be refreshed even when skipping re-evaluation so that
                // consumers reading ValidThroughTick < currentTick don't see retained assignments
                // as stale (FR-DA-029 / §2.2.2).
                if (MarkHysteresis.PreCheck(ref assignments[p], ref hysteresis[p]))
                {
                    assignments[p].ValidThroughTick = currentTick;
                    continue;
                }

                // Evaluate candidates.
                SelectBestCandidate(
                    snapshot, agent, defendsX0, currentTick,
                    out MarkAssignment candidate);

                // §3.3.3 Step 6: a ZONAL fallback commits DIRECTLY, bypassing the
                // hysteresis gate — only non-ZONAL candidates route through Step 7.
                // Routing ZONAL through the gate stranded the default-constructed
                // assignment record (Mode 0 / TargetEntityId 0) forever: the gate
                // dwell-held the MakeZonal candidate (target −1) against the default
                // (target 0) and no ZONAL assignment ever published a valid record.
                if (candidate.Mode == MarkMode.Zonal)
                {
                    assignments[p] = candidate;
                    // ZONAL has no dwell semantics (§3.11.3): clear any candidate accumulator left
                    // over from a prior non-ZONAL build-up so a brief ZONAL gap cannot let the next
                    // re-acquisition commit MAN_MARK without re-serving the dwell window.
                    MarkHysteresis.Reset(ref hysteresis[p]);
                }
                else
                {
                    // Apply hysteresis gate (§3.11) before committing.
                    MarkHysteresis.ApplyGate(candidate, ref assignments[p], ref hysteresis[p]);
                }

                // Stamp the agent EntityId and tick on the committed assignment.
                assignments[p].AgentEntityId    = agentId;
                assignments[p].ValidThroughTick = currentTick;
            }
        }

        // ── Candidate selection ───────────────────────────────────────────────

        private static void SelectBestCandidate(
            DefensiveSnapshot              snapshot,
            in DefensiveAgentSnapshot      agent,
            bool                           defendsX0,
            int                            currentTick,
            out MarkAssignment             candidate)
        {
            MarkMode bestMode    = MarkMode.Zonal;
            int      bestTargetId = -1;
            float    bestScore   = -1f;
            float    bestCost    = float.MaxValue;
            Vector2  bestPos     = Vector2.zero;

            int defensiveTeam = snapshot.DefensiveTeamId;
            // Cheap-item addition (FR-TI-033): the #21 MarkingOrientation dial scales the MAN_MARK
            // candidate radius (Balanced ⇒ ×1.0, identity — byte-identical to pre-addition).
            float orientationScalar = TacticTranslation.MarkRadiusScalar(snapshot.MarkingOrientation);
            float manMarkRadius = DefensiveAIConstants.ManMarkCandidateRadiusM * orientationScalar;
            float manMarkRadSq = manMarkRadius * manMarkRadius;

            for (int i = 0; i < snapshot.AgentCount; i++)
            {
                ref readonly DefensiveAgentSnapshot opp = ref snapshot.Agents[i];
                if (opp.TeamId == defensiveTeam)
                    continue;
                if (!opp.IsActive)
                    continue;

                float threat = ThreatScore(opp.Position, opp.PerceivedFirstTouch, defensiveTeam);

                // Check INTERCEPT_RUNNER eligibility (§3.3.3 Step 3).
                float speed = opp.Velocity.magnitude;
                if (speed > DefensiveAIConstants.RunnerVelocityThresholdMS)
                {
                    // Opponent velocity must point toward own goal (dot product with own-goal direction).
                    Vector2 ownGoalDir = defendsX0 ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
                    Vector2 velNorm    = opp.Velocity / speed;
                    float   dot        = Vector2.Dot(velNorm, ownGoalDir);

                    if (dot > 0f)
                    {
                        // Running toward own goal — qualifies as INTERCEPT_RUNNER.
                        float cost = LastManDetector.DisplacementCost(agent.Position, opp.Position);
                        if (IsBetter(MarkMode.InterceptRunner, threat, cost, opp.EntityId,
                                     bestMode, bestScore, bestCost, bestTargetId))
                        {
                            bestMode     = MarkMode.InterceptRunner;
                            bestTargetId = opp.EntityId;
                            bestScore    = threat;
                            bestCost     = cost;
                            bestPos      = opp.Position;
                        }
                        continue; // INTERCEPT_RUNNER supersedes MAN_MARK for this opponent.
                    }
                }

                // Check MAN_MARK eligibility only when no INTERCEPT_RUNNER has been found (§3.3.2).
                if (bestMode != MarkMode.InterceptRunner)
                {
                    float dx    = agent.Position.x - opp.Position.x;
                    float dy    = agent.Position.y - opp.Position.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= manMarkRadSq)
                    {
                        float cost = distSq; // distSq doubles as displacement cost here.
                        if (IsBetter(MarkMode.ManMark, threat, cost, opp.EntityId,
                                     bestMode, bestScore, bestCost, bestTargetId))
                        {
                            bestMode     = MarkMode.ManMark;
                            bestTargetId = opp.EntityId;
                            bestScore    = threat;
                            bestCost     = cost;
                            bestPos      = opp.Position;
                        }
                    }
                }
            }

            // If no eligible opponent found: ZONAL targeting formation slot.
            if (bestMode == MarkMode.Zonal || bestTargetId < 0)
            {
                candidate = MarkAssignment.MakeZonal(agent.EntityId, agent.BaselineSlot, currentTick);
                return;
            }

            candidate = new MarkAssignment
            {
                AgentEntityId      = agent.EntityId,
                Mode               = bestMode,
                TargetEntityId     = bestTargetId,
                TargetPosition     = bestPos,
                ValidThroughTick   = currentTick,
                OverriddenThisTick = false,
                IsManuallyAssigned = false,
            };
        }

        // ── Threat Score (§3.5) ───────────────────────────────────────────────

        /// <summary>
        /// Computes the threat score for an opponent. Defensive AI #14 §3.5.
        /// Output range [0, 1]; product of goal proximity and receiving attribute.
        /// </summary>
        public static float ThreatScore(Vector2 oppPos, float perceivedFirstTouch, int defensiveTeamId)
        {
            bool defendsX0 = LastManDetector.DefendsX0(defensiveTeamId);

            // Goal proximity: 1 - (xDist / PITCH_LENGTH_M). X-axis only (Stage 0).
            float ownGoalX = defendsX0 ? 0f : DefensiveAIConstants.PITCH_LENGTH_M;
            float xDist    = UnityEngine.Mathf.Abs(oppPos.x - ownGoalX);
            float proximity = 1f - (xDist / DefensiveAIConstants.PITCH_LENGTH_M);
            proximity = UnityEngine.Mathf.Clamp01(proximity);

            // Opponent receiving attribute normalised [0, 1]: (attr - 1) / 19.
            float attr = UnityEngine.Mathf.Clamp(perceivedFirstTouch, 1f, 20f);
            float receivingAttr = (attr - 1f) / 19f;

            return proximity * receivingAttr;
        }

        // ── Tie-break comparator ──────────────────────────────────────────────

        /// <summary>
        /// Returns true when (newMode, newScore, newCost, newId) strictly beats
        /// (curMode, curScore, curCost, curId) under the priority rules (§3.3.2 / FR-DA-014).
        /// Priority: INTERCEPT_RUNNER &gt; MAN_MARK &gt; ZONAL, then highest threat score,
        /// then lowest displacement cost, then lowest EntityId.
        /// </summary>
        private static bool IsBetter(
            MarkMode newMode,  float newScore, float newCost, int newId,
            MarkMode curMode,  float curScore, float curCost, int curId)
        {
            // Mode priority first.
            int newPri = ModePriority(newMode);
            int curPri = ModePriority(curMode);
            if (newPri != curPri)
                return newPri > curPri;

            // Same mode: higher threat score wins.
            if (newScore > curScore + 1e-6f)
                return true;
            if (curScore > newScore + 1e-6f)
                return false;

            // Equal score: lower displacement cost.
            if (newCost < curCost - 1e-6f)
                return true;
            if (curCost < newCost - 1e-6f)
                return false;

            // Equal cost: lower EntityId (FR-DA-014 terminal tie-break).
            return newId < curId;
        }

        private static int ModePriority(MarkMode mode)
        {
            switch (mode)
            {
                case MarkMode.InterceptRunner: return 2;
                case MarkMode.ManMark:         return 1;
                default:                       return 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                             |
// | 1.1     | 2026-05-29 | —      | AR-1 H-2: refresh ValidThroughTick when PreCheck returns true (dwell lock retained); |
// |         |            |        |   without this external consumers see stale tick on retained assignments.           |
// | 1.2     | 2026-06-12 | —      | Dotnet-CI quarantine adjudication (PRODUCTION-DEFECT, T-DA-011): §3.3.3 Step 6       |
// |         |            |        |   commits a ZONAL fallback DIRECTLY (assignments[agent] = …; continue) — only        |
// |         |            |        |   non-ZONAL candidates route through the Step 7 hysteresis gate. Assign() routed     |
// |         |            |        |   ZONAL through ApplyGate, so the default-constructed assignment record              |
// |         |            |        |   (TargetEntityId 0) was dwell-held forever and no valid ZONAL record (target −1,    |
// |         |            |        |   formation-slot position) ever published. ZONAL now bypasses the gate per spec;     |
// |         |            |        |   hysteresis state untouched on the bypass path, matching the Step 6 pseudocode.     |
// | 1.3     | 2026-06-15 | —      | AR-2 sweep L: ZONAL bypass now resets hysteresis[p] (§3.11.3). A non-ZONAL candidate  |
// |         |            |        |   accumulator (HoldTicks/CandidateMode) surviving a ZONAL tick let the next            |
// |         |            |        |   re-acquisition of the same opponent commit MAN_MARK immediately, skipping the dwell. |
// | 1.4     | 2026-07-07 | —      | Cheap-item addition (FR-TI-033): MAN_MARK candidate radius scaled by                  |
// |         |            |        |   TacticTranslation.MarkRadiusScalar(snapshot.MarkingOrientation); Balanced ⇒ ×1.0.     |
#endregion
