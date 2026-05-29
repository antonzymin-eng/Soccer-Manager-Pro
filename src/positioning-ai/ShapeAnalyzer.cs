// File: src/positioning-ai/ShapeAnalyzer.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.3, §3.4
// Purpose: Resolves per-agent line and lane membership with dead-zone + dwell hysteresis,
//          called AFTER spacing and pitch-bound clamp (AR-S1-03 critical ordering rule).

using System;

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Resolves line membership (§3.3) and lane assignment (§3.4) with hysteresis.
    /// Must be called after SpacingResolver and pitch-bound clamp per AR-S1-03.
    /// All methods are pure static; mutable state lives in HysteresisState.Agents[].
    /// </summary>
    public static class ShapeAnalyzer
    {
        // ── Line membership ───────────────────────────────────────────────────

        /// <summary>
        /// Determines each outfield agent's committed line membership.
        /// Line assignment is structurally determined by the k=3 partition from the formation's
        /// lineCutIndices; agents are sorted by current slot.x and the bottom firstMid agents
        /// are Defense, the next group Midfield, the last Attack.
        /// Hysteresis: the boundary position is the midpoint of adjacent agents at each cut;
        /// a change only commits if the agent's slot is LINE_HYSTERESIS_M beyond that boundary
        /// for LINE_DWELL_TICKS consecutive ticks.
        /// </summary>
        /// <param name="outSlots">Final clamped slots; length = SQUAD_SIZE (GK at index 0).</param>
        /// <param name="archetype">Formation family for lineCutIndices.</param>
        /// <param name="hyst">Mutated in-place.</param>
        public static void ResolveAllLines(
            Vector2[] outSlots,
            FormationFamily archetype,
            HysteresisState hyst)
        {
            (int firstMid, int firstAtk) = PositioningAIConstants.GetLineCuts(archetype);

            // Sort outfield slot indices (1..10) by current slot.x to identify line boundaries.
            // Use insertion sort (stable, in-place, ≤10 elements, zero-allocation).
            Span<int> order = stackalloc int[PositioningAIConstants.SQUAD_SIZE - 1]; // 10 outfield
            for (int i = 0; i < order.Length; i++) order[i] = i + 1; // indices 1..10

            for (int i = 1; i < order.Length; i++)
            {
                int key = order[i];
                int j   = i - 1;
                while (j >= 0 && outSlots[order[j]].x > outSlots[key].x)
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = key;
            }

            // Assign structural line membership from sort position.
            // order[0..firstMid-2]           → Defense
            // order[firstMid-1..firstAtk-2]  → Midfield
            // order[firstAtk-1..]            → Attack
            // (subtract 1 because order holds outfield indices 1..10, not 0..9)
            int outfieldCount = PositioningAIConstants.SQUAD_SIZE - 1;
            for (int sortPos = 0; sortPos < outfieldCount; sortPos++)
            {
                int slotIdx        = order[sortPos];
                LineId newLine     = SortPositionToLine(sortPos, firstMid - 1, firstAtk - 1);
                LineId currentLine = hyst.Agents[slotIdx].CurrentLine;

                if (newLine == currentLine)
                {
                    // Still in same zone — reset dwell counter.
                    hyst.Agents[slotIdx].LineDwellCount = 0;
                    hyst.Agents[slotIdx].CandidateLine  = currentLine;
                    continue;
                }

                // Compute line boundary x-position for dead-zone check.
                float boundaryX = ComputeLineBoundaryX(outSlots, order, sortPos, firstMid - 1, firstAtk - 1);
                float slotX     = outSlots[slotIdx].x;
                float distFromBoundary = Math.Abs(slotX - boundaryX);

                if (distFromBoundary < PositioningAIConstants.LINE_HYSTERESIS_M)
                {
                    // Inside dead zone — no candidate advancement.
                    hyst.Agents[slotIdx].LineDwellCount = 0;
                    continue;
                }

                // Outside dead zone — advance dwell counter.
                if (newLine == hyst.Agents[slotIdx].CandidateLine)
                {
                    hyst.Agents[slotIdx].LineDwellCount++;
                    if (hyst.Agents[slotIdx].LineDwellCount >= PositioningAIConstants.LINE_DWELL_TICKS)
                        hyst.Agents[slotIdx].CurrentLine = newLine;
                }
                else
                {
                    hyst.Agents[slotIdx].CandidateLine  = newLine;
                    hyst.Agents[slotIdx].LineDwellCount = 1;
                }
            }
        }

        // ── Lane membership ───────────────────────────────────────────────────

        /// <summary>
        /// Determines each outfield agent's committed lane membership by mapping slot.y
        /// into the five 13.6 m bins, with a LANE_HYSTERESIS_M dead zone at bin boundaries.
        /// </summary>
        public static void ResolveAllLanes(
            Vector2[] outSlots,
            HysteresisState hyst)
        {
            // GK excluded — start at index 1.
            for (int i = 1; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                // Sentinel agents were already skipped in PositioningAITick; guard here too.
                if (float.IsNegativeInfinity(outSlots[i].x)) continue;

                LaneId newLane     = ClassifyLane(outSlots[i].y);
                LaneId currentLane = hyst.Agents[i].CurrentLane;

                if (newLane == currentLane)
                {
                    hyst.Agents[i].LaneDwellCount = 0;
                    hyst.Agents[i].CandidateLane  = currentLane;
                    continue;
                }

                float boundaryY  = GetLaneBoundaryY(currentLane, newLane);
                float dist       = Math.Abs(outSlots[i].y - boundaryY);

                if (dist < PositioningAIConstants.LANE_HYSTERESIS_M)
                {
                    hyst.Agents[i].LaneDwellCount = 0;
                    continue;
                }

                if (newLane == hyst.Agents[i].CandidateLane)
                {
                    hyst.Agents[i].LaneDwellCount++;
                    if (hyst.Agents[i].LaneDwellCount >= PositioningAIConstants.LANE_DWELL_TICKS)
                        hyst.Agents[i].CurrentLane = newLane;
                }
                else
                {
                    hyst.Agents[i].CandidateLane  = newLane;
                    hyst.Agents[i].LaneDwellCount = 1;
                }
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static LineId SortPositionToLine(int sortPos, int firstMidIdx, int firstAtkIdx)
        {
            if (sortPos < firstMidIdx)  return LineId.Defense;
            if (sortPos < firstAtkIdx)  return LineId.Midfield;
            return LineId.Attack;
        }

        /// <summary>
        /// Returns the boundary x value between the agent's current line and the candidate line.
        /// Uses the midpoint between the two agents straddling the cut.
        /// </summary>
        private static float ComputeLineBoundaryX(
            Vector2[] outSlots, Span<int> order, int sortPos,
            int firstMidIdx, int firstAtkIdx)
        {
            // Identify the relevant cut index (boundary between current and candidate line).
            int cutIdx = sortPos < firstMidIdx ? firstMidIdx : firstAtkIdx;

            // Midpoint of adjacent agents at the cut.
            if (cutIdx > 0 && cutIdx < order.Length)
            {
                float xBelow = outSlots[order[cutIdx - 1]].x;
                float xAbove = outSlots[order[cutIdx]].x;
                return (xBelow + xAbove) * 0.5f;
            }
            // Edge case: cut is at array boundary.
            return cutIdx < order.Length ? outSlots[order[cutIdx]].x : outSlots[order[cutIdx - 1]].x;
        }

        private static LaneId ClassifyLane(float y)
        {
            float[] edges = PositioningAIConstants.LaneEdgesM;
            if (y < edges[1]) return LaneId.LW;
            if (y < edges[2]) return LaneId.LH;
            if (y < edges[3]) return LaneId.C;
            if (y < edges[4]) return LaneId.RH;
            return LaneId.RW;
        }

        /// <summary>Returns the shared boundary Y between two adjacent lanes.</summary>
        private static float GetLaneBoundaryY(LaneId a, LaneId b)
        {
            int hi = (int)a > (int)b ? (int)a : (int)b;
            return PositioningAIConstants.LaneEdgesM[hi]; // edge index = higher lane index
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
