// File:     src/defensive-ai/InvariantEnforcer.cs
// Created:  2026-05-29
// Modified: 2026-11-12 (W13.1 review M2: Invariant order 1-3-4-2 when MarkMovementActive; 1-2-3 unchanged otherwise)
// Modified: 2026-11-10 (W13.1 / ERR-014-010 — Invariant 4: one marker per opponent, gated by DefensiveSnapshot.MarkMovementActive)
// Author:   —
// Spec:     Defensive AI #14 §3.10, §4.3, FR-DA-024–028, FR-DA-039, ERR-014-008, ERR-014-010, Code Standards #20
// Purpose:  Pure static module: enforces the three anti-chaos invariants (§3.10 / KD-17) plus the gated
//           Invariant 4 (one marker per opponent) after the full assignment pass, before directive publication.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Enforces the three anti-chaos invariants after all assignments are computed (§3.10 / KD-17).
    /// Applied BEFORE directive publication (FR-DA-024). Up to 3 demotion passes (FR-DA-028).
    ///
    /// Invariant 1: DEFENSE-line agents in ZONAL &gt;= MIN_BACKLINE_AGENTS (FR-DA-025).
    /// Invariant 2: MAN_MARK assignments &lt;= MAX_MAN_MARK_ASSIGNMENTS (FR-DA-026).
    /// Invariant 3: non-ZONAL displacement from baseline &lt;= MAX_MARK_DISPLACEMENT_M (FR-DA-027).
    /// Invariant 4 (W13.1, ERR-014-010, FR-DA-039): at most one marker per opponent — runs ONLY when
    /// <see cref="DefensiveSnapshot.MarkMovementActive"/> is true, so the default pipeline is exactly the
    /// three-invariant one (order 1, 2, 3). With it active the order is 1, 3, 4, 2: Invariant 4 precedes the
    /// MAN_MARK cap so duplicate markers do not count against it (review amendment).
    ///
    /// Returns false on F4 (hard fallback: emit all-ZONAL per FR-DA-032).
    /// Pure static; zero allocation. Defensive AI #14 §3.10 / §4.3.
    /// </summary>
    public static class InvariantEnforcer
    {
        private const int MaxPasses = 3;

        /// <summary>Outcome of a single invariant-enforcement helper.</summary>
        private enum InvariantStep
        {
            /// <summary>Invariant already held; nothing demoted.</summary>
            Satisfied,
            /// <summary>One or more violating assignments were demoted to ZONAL.</summary>
            Demoted,
            /// <summary>Invariant violated but no eligible demotion candidate exists (F4).</summary>
            Stuck,
        }

        /// <summary>
        /// Enforces all invariants (the three standing ones, plus Invariant 4 when active) in place, demoting violating assignments to ZONAL.
        ///
        /// Each helper fully enforces its own invariant (demoting EVERY violation, not just
        /// the first) before returning. The outer loop re-runs until a pass makes no further
        /// change (FR-DA-028: bounded at <see cref="MaxPasses"/>); a final
        /// <see cref="AreAllSatisfied"/> check guards against a pass-budget exhaustion.
        /// Demotions are monotonic (every demotion moves an assignment toward ZONAL, which can
        /// only help the other two invariants), so convergence within one pass is the norm.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid pool entries.</param>
        /// <param name="currentTick">Tick index for any produced ZONAL assignments.</param>
        /// <param name="assignments">Assignment buffer (modified in place).</param>
        /// <returns>True when all invariants are satisfied; false when F4 hard fallback is required.</returns>
        public static bool Enforce(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                bool changed = false;

                // --- Invariant 1: minimum DEFENSE-line agents in ZONAL ---
                InvariantStep s1 = EnforceMinBackline(snapshot, poolBuffer, poolCount, currentTick, assignments);
                if (s1 == InvariantStep.Stuck)
                    return false; // Backline minimum unreachable — F4 hard fallback (FR-DA-032).
                changed |= s1 == InvariantStep.Demoted;

                if (!snapshot.MarkMovementActive)
                {
                    // --- Invariant 2: maximum MAN_MARK assignments ---
                    changed |= EnforceMaxManMark(snapshot, poolBuffer, poolCount, currentTick, assignments);

                    // --- Invariant 3: maximum displacement ---
                    changed |= EnforceMaxDisplacement(snapshot, poolBuffer, poolCount, currentTick, assignments);
                }
                else
                {
                    // W13.1 (ERR-014-010 review amendment): with mark movement active the order is 1 -> 3 -> 4 -> 2.
                    // Invariant 4 must run BEFORE the MAN_MARK cap: a duplicate marker is removed anyway, so
                    // counting it against the cap would demote a lone-marker opponent's marker needlessly
                    // (5 marks, two on one man: the cap would drop the lowest-threat man's marker and
                    // Invariant 4 then drop the duplicate, leaving 3 marks and a man unmarked).
                    // Demotions are monotonic, so no step can re-violate an earlier one.
                    changed |= EnforceMaxDisplacement(snapshot, poolBuffer, poolCount, currentTick, assignments);
                    changed |= EnforceOneMarkerPerOpponent(snapshot, poolBuffer, poolCount, currentTick, assignments);
                    changed |= EnforceMaxManMark(snapshot, poolBuffer, poolCount, currentTick, assignments);
                }

                if (!changed)
                    break; // Stable: no demotion this pass.
            }

            // Final verification (covers the pass-budget-exhaustion corner).
            return AreAllSatisfied(snapshot, poolBuffer, poolCount, assignments);
        }

        // ── Invariant 1: MinBacklineAgents ───────────────────────────────────

        /// <summary>
        /// Demotes lowest-threat non-emergency DEFENSE-line marks to ZONAL until the backline
        /// minimum is met. Returns <see cref="InvariantStep.Stuck"/> when the minimum cannot be
        /// reached because every remaining non-ZONAL DEFENSE agent is an emergency override.
        /// </summary>
        private static InvariantStep EnforceMinBackline(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            bool demotedAny = false;

            while (true)
            {
                int defensiveTotal = 0;
                int defensiveZonal = 0;

                for (int p = 0; p < poolCount; p++)
                {
                    int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                    if (aIdx < 0) continue;
                    if (snapshot.Agents[aIdx].Line != TacticalDirector.PositioningAI.LineId.Defense) continue;
                    defensiveTotal++;
                    if (assignments[p].Mode == MarkMode.Zonal) defensiveZonal++;
                }

                // Satisfied, or unsatisfiable (too few DEFENSE-line agents to ever reach the floor).
                if (defensiveZonal >= DefensiveAIConstants.MinBacklineAgents
                 || defensiveTotal < DefensiveAIConstants.MinBacklineAgents)
                    return demotedAny ? InvariantStep.Demoted : InvariantStep.Satisfied;

                // Find lowest-threat non-ZONAL DEFENSE-line assignment that is not an emergency
                // override and has a valid target (not COVER_GK_ZONE with no targetEntityId).
                int   bestPoolIdx  = -1;
                float lowestThreat = float.MaxValue;

                for (int p = 0; p < poolCount; p++)
                {
                    if (assignments[p].Mode == MarkMode.Zonal) continue;
                    if (assignments[p].OverriddenThisTick) continue;
                    if (assignments[p].TargetEntityId < 0) continue; // COVER_GK_ZONE excluded.

                    int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                    if (aIdx < 0) continue;
                    if (snapshot.Agents[aIdx].Line != TacticalDirector.PositioningAI.LineId.Defense) continue;

                    float threat = TargetThreat(snapshot, assignments[p].TargetEntityId);

                    if (threat < lowestThreat || (threat == lowestThreat && (bestPoolIdx < 0 || poolBuffer[p] < poolBuffer[bestPoolIdx])))
                    {
                        lowestThreat = threat;
                        bestPoolIdx  = p;
                    }
                }

                if (bestPoolIdx < 0)
                    return InvariantStep.Stuck; // All non-ZONAL DEFENSE agents are emergency overrides.

                DemoteToZonal(snapshot, poolBuffer[bestPoolIdx], currentTick, ref assignments[bestPoolIdx]);
                demotedAny = true;
            }
        }

        // ── Invariant 2: MaxManMarkAssignments ───────────────────────────────

        /// <summary>Demotes lowest-threat MAN_MARK assignments to ZONAL until the cap is met. Returns true if any demotion occurred.</summary>
        private static bool EnforceMaxManMark(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            bool demotedAny = false;

            while (true)
            {
                int manMarkCount = 0;
                for (int p = 0; p < poolCount; p++)
                    if (assignments[p].Mode == MarkMode.ManMark) manMarkCount++;

                if (manMarkCount <= DefensiveAIConstants.MaxManMarkAssignments)
                    return demotedAny; // Satisfied.

                // Demote lowest-threat MAN_MARK assignment.
                int   bestPoolIdx  = -1;
                float lowestThreat = float.MaxValue;

                for (int p = 0; p < poolCount; p++)
                {
                    if (assignments[p].Mode != MarkMode.ManMark) continue;

                    float threat = TargetThreat(snapshot, assignments[p].TargetEntityId);

                    if (threat < lowestThreat || (threat == lowestThreat && (bestPoolIdx < 0 || poolBuffer[p] < poolBuffer[bestPoolIdx])))
                    {
                        lowestThreat = threat;
                        bestPoolIdx  = p;
                    }
                }

                if (bestPoolIdx < 0)
                    return demotedAny; // Defensive guard — unreachable while manMarkCount > cap.

                DemoteToZonal(snapshot, poolBuffer[bestPoolIdx], currentTick, ref assignments[bestPoolIdx]);
                demotedAny = true;
            }
        }

        // ── Invariant 3: MaxMarkDisplacementM ────────────────────────────────

        /// <summary>Demotes every over-displaced non-emergency assignment to ZONAL. Returns true if any demotion occurred.</summary>
        private static bool EnforceMaxDisplacement(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            bool demotedAny = false;

            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode == MarkMode.Zonal) continue;
                if (assignments[p].OverriddenThisTick) continue;

                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0) continue;

                Vector2 baseline  = snapshot.Agents[aIdx].BaselineSlot;
                Vector2 target    = assignments[p].TargetPosition;
                float   dx        = target.x - baseline.x;
                float   dy        = target.y - baseline.y;
                float   dist      = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > DefensiveAIConstants.MaxMarkDisplacementM)
                {
                    DemoteToZonal(snapshot, poolBuffer[p], currentTick, ref assignments[p]);
                    demotedAny = true;
                }
            }

            return demotedAny;
        }

        // ── Invariant 4: one marker per opponent (W13.1, ERR-014-010) ─────────

        /// <summary>
        /// Demotes every marker that duplicates another marker's target opponent, keeping exactly one per
        /// opponent (#14 §3.10.2 Invariant 4). Applies to every non-ZONAL assignment that names a target
        /// (MAN_MARK and INTERCEPT_RUNNER; COVER_GK_ZONE has none). The keeper of a duplicated target is
        /// chosen by a strict total order: an emergency override (<c>OverriddenThisTick</c>) first, then the
        /// lowest displacement cost <c>|agent − opponent|²</c> (§3.4, live snapshot positions), then the
        /// lowest EntityId (FR-DA-014). Returns true if any demotion occurred.
        /// </summary>
        private static bool EnforceOneMarkerPerOpponent(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            bool demotedAny = false;

            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode == MarkMode.Zonal) continue;
                int target = assignments[p].TargetEntityId;
                if (target < 0) continue;

                float costP = MarkerCost(snapshot, poolBuffer[p], assignments[p]);

                for (int q = 0; q < poolCount; q++)
                {
                    if (q == p) continue;
                    if (assignments[q].Mode == MarkMode.Zonal) continue;
                    if (assignments[q].TargetEntityId != target) continue;

                    if (IsBetterMarker(
                            assignments[q].OverriddenThisTick, MarkerCost(snapshot, poolBuffer[q], assignments[q]), poolBuffer[q],
                            assignments[p].OverriddenThisTick, costP, poolBuffer[p]))
                    {
                        DemoteToZonal(snapshot, poolBuffer[p], currentTick, ref assignments[p]);
                        demotedAny = true;
                        break;
                    }
                }
            }

            return demotedAny;
        }

        /// <summary>
        /// Strict total order on duplicate markers of one opponent: true when marker A beats marker B
        /// (emergency override, then lower cost, then lower EntityId). Exact float comparison — an epsilon
        /// would make the order non-transitive and could leave two keepers.
        /// </summary>
        private static bool IsBetterMarker(
            bool aOverride, float aCost, int aId,
            bool bOverride, float bCost, int bId)
        {
            if (aOverride != bOverride) return aOverride;
            if (aCost != bCost)         return aCost < bCost;
            return aId < bId;
        }

        /// <summary>Squared distance (m²) from the marker to the opponent it marks (§3.4); a marker or target absent from the snapshot costs float.MaxValue.</summary>
        private static float MarkerCost(DefensiveSnapshot snapshot, int markerEntityId, in MarkAssignment assignment)
        {
            int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, markerEntityId);
            int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignment.TargetEntityId);
            if (aIdx < 0 || oIdx < 0)
                return float.MaxValue;

            Vector2 a  = snapshot.Agents[aIdx].Position;
            Vector2 o  = snapshot.Agents[oIdx].Position;
            float   dx = a.x - o.x;
            float   dy = a.y - o.y;
            return dx * dx + dy * dy;
        }

        // ── Post-loop verification ────────────────────────────────────────────

        private static bool AreAllSatisfied(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            MarkAssignment[]  assignments)
        {
            int defensiveTotal = 0;
            int defensiveZonal = 0;
            int manMarkCount   = 0;

            for (int p = 0; p < poolCount; p++)
            {
                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx >= 0 && snapshot.Agents[aIdx].Line == TacticalDirector.PositioningAI.LineId.Defense)
                {
                    defensiveTotal++;
                    if (assignments[p].Mode == MarkMode.Zonal) defensiveZonal++;
                }
                if (assignments[p].Mode == MarkMode.ManMark) manMarkCount++;

                if (assignments[p].Mode != MarkMode.Zonal && !assignments[p].OverriddenThisTick)
                {
                    if (aIdx >= 0)
                    {
                        Vector2 baseline = snapshot.Agents[aIdx].BaselineSlot;
                        Vector2 target   = assignments[p].TargetPosition;
                        float   dx       = target.x - baseline.x;
                        float   dy       = target.y - baseline.y;
                        if (Mathf.Sqrt(dx * dx + dy * dy) > DefensiveAIConstants.MaxMarkDisplacementM)
                            return false;
                    }
                }
            }

            bool inv1 = defensiveZonal >= DefensiveAIConstants.MinBacklineAgents
                     || defensiveTotal < DefensiveAIConstants.MinBacklineAgents;
            bool inv2 = manMarkCount   <= DefensiveAIConstants.MaxManMarkAssignments;
            bool inv4 = !snapshot.MarkMovementActive
                     || !HasDuplicateMarkTarget(poolCount, assignments);
            return inv1 && inv2 && inv4;
        }

        /// <summary>True when two non-ZONAL assignments name the same target opponent (Invariant 4 violated).</summary>
        private static bool HasDuplicateMarkTarget(int poolCount, MarkAssignment[] assignments)
        {
            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode == MarkMode.Zonal || assignments[p].TargetEntityId < 0) continue;
                for (int q = p + 1; q < poolCount; q++)
                {
                    if (assignments[q].Mode != MarkMode.Zonal
                     && assignments[q].TargetEntityId == assignments[p].TargetEntityId)
                        return true;
                }
            }

            return false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Threat score of the opponent targeted by an assignment; 0 when the target is not on the pitch.</summary>
        private static float TargetThreat(DefensiveSnapshot snapshot, int targetEntityId)
        {
            int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, targetEntityId);
            if (oIdx < 0)
                return 0f;

            ref readonly DefensiveAgentSnapshot opp = ref snapshot.Agents[oIdx];
            return MarkAssigner.ThreatScore(opp.Position, opp.PerceivedFirstTouch);
        }

        private static void DemoteToZonal(
            DefensiveSnapshot snapshot,
            int               entityId,
            int               currentTick,
            ref MarkAssignment assignment)
        {
            int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, entityId);
            Vector2 slot = aIdx >= 0 ? snapshot.Agents[aIdx].BaselineSlot : Vector2.zero;
            assignment = MarkAssignment.MakeZonal(entityId, slot, currentTick);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                              |
// | 1.1     | 2026-06-15 | —      | AR-2 H-2: Enforce() returned true after a single demotion per invariant — the        |
// |         |            |        |   end-of-pass `return true` short-circuited the 3-pass loop, so states violating an   |
// |         |            |        |   invariant by >1 (e.g. 7 MAN_MARK vs cap 4) reported satisfied with no F4 fallback.   |
// |         |            |        |   Each helper now fully enforces its invariant (demotes ALL violations); the outer     |
// |         |            |        |   loop re-runs until a no-change pass; final AreAllSatisfied verifies. MinBackline      |
// |         |            |        |   returns an InvariantStep so Stuck (backline unreachable) maps to F4 distinctly.       |
// | 1.2     | 2026-11-10 | —      | ERR-014-008: TargetThreat (the demotion ranking of Invariants 1 and 2) takes defendsX0     |
// |         |            |        |   from DefensiveSnapshot.IsOwnGoalAtX0, not the team id, so the away team demotes the     |
// |         |            |        |   lowest-threat mark rather than the highest.                                              |
// | 1.3     | 2026-11-10 | —      | W13.0 review M1: TargetThreat uses the canonical ThreatScore(oppPos, firstTouch). |
// | 1.4     | 2026-11-10 | —      | W13.1 / ERR-014-010: Invariant 4 (one marker per opponent; keeper = emergency override, then lowest |
// |         |            |        |   squared marker-to-opponent distance, then lowest EntityId; losers demoted) inside the existing   |
// |         |            |        |   pass loop and the final check, active ONLY when DefensiveSnapshot.MarkMovementActive — default   |
// |         |            |        |   behaviour unchanged.                                                                              |
// | 1.5     | 2026-11-12 | —      | W13.1 review M2 (ERR-014-010 amendment): with MarkMovementActive the invariant order is 1, 3, 4, 2 (was 1, 2, 3, 4), so  |
// |         |            |        |   duplicate markers no longer count against the MAN_MARK cap; with it false the order is exactly 1, 2, 3 as before.      |
#endregion
