// File:     src/defensive-ai/MarkAssigner.cs
// Created:  2026-05-29
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14 §3.3, §3.4, §3.5, §4.3, ERR-014-008, ERR-014-009, Code Standards #20; Tactical Instructions #21 §3.4 (FR-TI-033)
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
        /// <summary>
        /// Unit vector pointing toward the defended goal in the canonical frame (own goal at x = 0).
        /// A frame invariant of the snapshot (#14 §2.3), not a per-team value (ERR-014-008).
        /// </summary>
        private static readonly Vector2 OwnGoalDirection = new Vector2(-1f, 0f);

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
                    // ERR-014-009 / §2.2.2: a retained mark publishes the target's CURRENT position, so
                    // Invariant 3 and any consumer see the live point rather than the commit-time one.
                    RefreshRetained(snapshot, agent, currentTick, ref assignments[p], ref hysteresis[p]);
                    assignments[p].ValidThroughTick = currentTick;
                    continue;
                }

                // Evaluate candidates.
                SelectBestCandidate(
                    snapshot, agent, currentTick,
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

                    // The gate may retain the current assignment (same target, or a competing
                    // candidate still inside its dwell): refresh it as on the dwell-lock path.
                    RefreshRetained(snapshot, agent, currentTick, ref assignments[p], ref hysteresis[p]);
                }

                // Stamp the agent EntityId and tick on the committed assignment.
                assignments[p].AgentEntityId    = agentId;
                assignments[p].ValidThroughTick = currentTick;
            }
        }

        // ── Retained-assignment refresh (ERR-014-009) ─────────────────────────

        /// <summary>
        /// Refreshes a retained MAN_MARK / INTERCEPT_RUNNER assignment from the live snapshot (#14 §2.2.2:
        /// <c>TargetPosition</c> is the opponent's current perceived position, not the commit-time one).
        /// When the retained target is absent from the snapshot or not active (sent off, substituted) there
        /// is no position to publish, so the mark is released to ZONAL immediately (target -1, own
        /// formation slot) and the hysteresis state is reset exactly as on the §3.3.3 Step 6 ZONAL bypass
        /// (dwell counter AND candidate accumulator, so re-acquiring any mark pays a full
        /// <see cref="DefensiveAIConstants.MarkDwellTicks"/> again) —
        /// the dwell lock exists to suppress candidate thrash, not to hold a mark on a player who is gone.
        /// ZONAL and COVER_GK_ZONE assignments are left untouched. Pure; no allocation.
        /// </summary>
        private static void RefreshRetained(
            DefensiveSnapshot            snapshot,
            in DefensiveAgentSnapshot    agent,
            int                          currentTick,
            ref MarkAssignment           assignment,
            ref MarkHysteresisState      hysteresis)
        {
            if (assignment.Mode != MarkMode.ManMark && assignment.Mode != MarkMode.InterceptRunner)
                return;

            int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignment.TargetEntityId);
            if (oIdx >= 0 && snapshot.Agents[oIdx].IsActive)
            {
                assignment.TargetPosition = snapshot.Agents[oIdx].Position;
                return;
            }

            assignment = MarkAssignment.MakeZonal(agent.EntityId, agent.BaselineSlot, currentTick);
            MarkHysteresis.Reset(ref hysteresis);
        }

        // ── Candidate selection ───────────────────────────────────────────────

        private static void SelectBestCandidate(
            DefensiveSnapshot              snapshot,
            in DefensiveAgentSnapshot      agent,
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

                float threat = ThreatScore(opp.Position, opp.PerceivedFirstTouch);

                // Check INTERCEPT_RUNNER eligibility (§3.3.3 Step 3).
                float speed = opp.Velocity.magnitude;
                if (speed > DefensiveAIConstants.RunnerVelocityThresholdMS)
                {
                    // Opponent velocity must point toward own goal (dot product with own-goal direction).
                    // Canonical frame: the defended goal is at x = 0, so "toward own goal" is -X.
                    Vector2 ownGoalDir = OwnGoalDirection;
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
        /// <param name="oppPos">Opponent position in the canonical frame (m); the defended goal is x = 0.</param>
        /// <param name="perceivedFirstTouch">Perceived FirstTouch on the 1-20 scale.</param>
        public static float ThreatScore(Vector2 oppPos, float perceivedFirstTouch)
        {
            // Goal proximity: 1 - (xDist / PITCH_LENGTH_M). X-axis only (Stage 0). Own goal at x = 0.
            float xDist    = UnityEngine.Mathf.Abs(oppPos.x);
            float proximity = 1f - (xDist / DefensiveAIConstants.PITCH_LENGTH_M);
            proximity = UnityEngine.Mathf.Clamp01(proximity);

            // Opponent receiving attribute normalised [0, 1]: (attr - 1) / 19.
            float attr = UnityEngine.Mathf.Clamp(perceivedFirstTouch, 1f, 20f);
            float receivingAttr = (attr - 1f) / 19f;

            return proximity * receivingAttr;
        }

        /// <summary>
        /// COMPATIBILITY SHIM — the <paramref name="defensiveTeamId"/> argument is IGNORED. #14 is
        /// canonical-only (the defended goal is x = 0 for every team, ERR-014-008), so team 0 and team 1
        /// score identically. Kept <c>internal</c> solely because the pre-existing ThreatScoreTests call
        /// it with a named <c>defensiveTeamId:</c> argument; production code uses
        /// <see cref="ThreatScore(Vector2, float)"/>.
        /// </summary>
        internal static float ThreatScore(Vector2 oppPos, float perceivedFirstTouch, int defensiveTeamId)
        {
            return ThreatScore(oppPos, perceivedFirstTouch);
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
// | 1.5     | 2026-11-10 | —      | ERR-014-008: defendsX0 taken from DefensiveSnapshot.IsOwnGoalAtX0 (canonical frame),  |
// |         |            |        |   not teamId == 0. ThreatScore gains a bool defendsX0 overload; the int teamId overload   |
// |         |            |        |   is kept for source compatibility and ignores the id. The away team previously ranked    |
// |         |            |        |   threat and runner direction against x = 105 while its snapshot has the goal at x = 0.   |
// | 1.6     | 2026-11-10 | —      | ERR-014-009: a retained non-ZONAL assignment (dwell lock or same-target/held-candidate gate |
// |         |            |        |   path) now refreshes TargetPosition from the live snapshot each tick (§2.2.2), so Invariant 3  |
// |         |            |        |   tests the live point. A retained target that is absent or inactive releases the mark to    |
// |         |            |        |   ZONAL at once and resets hysteresis. COVER_GK_ZONE / ZONAL untouched.                      |
// | 1.7     | 2026-11-10 | —      | W13.0 review M1/L1/L2: canonical-only — the bool defendsX0 plumbing and the x = 105 branches  |
// |         |            |        |   are removed (ThreatScore(oppPos, firstTouch); OwnGoalDirection). The int teamId ThreatScore    |
// |         |            |        |   overload is now an INTERNAL compatibility shim that ignores the id. RefreshRetained's release  |
// |         |            |        |   also resets the candidate accumulator, so re-acquisition pays a full MarkDwellTicks.          |
#endregion
