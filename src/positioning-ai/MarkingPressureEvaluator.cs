// File:     src/positioning-ai/MarkingPressureEvaluator.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Dismarking AI #23 §3.1–§3.3 (FM-DM-01/02), FR-DM-001..009, Code Standards #20
// Purpose:  Pure static dismarking math: marker search, dwell update, MarkingPressure, and the
//           dismark offset vector. No side effects, no RNG, no allocation (FR-DM-005).

using System;

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Pure static dismarking evaluator (#23 §3.1–§3.3), mirroring the <see cref="RestDefenseEvaluator"/>
    /// placement pattern (#23 §4.1). LAYERING NOTE: #23 §4.4 sources the opponent data from the
    /// agent's own perception <c>FilteredView</c> (KD-1 / FR-DM-001) — but <c>FilteredView</c> is an
    /// AI-layer type and this is a Mechanics assembly (Mechanics MUST NOT reference AI), so the
    /// signatures here take the perceived positions/ids as primitive spans. The sanctioned caller
    /// (the match-engine per-agent pass, #23 §4.4) extracts them from
    /// <c>FilteredView.VisibleOpponents[i].PerceivedPosition / .AgentId</c>; the signature admits no
    /// ground-truth source, preserving FR-DM-001's mechanical auditability at the call seam.
    /// </summary>
    public static class MarkingPressureEvaluator
    {
        /// <summary>
        /// Finds the nearest qualifying perceived marker (#23 §3.1): finite position, distance ≤
        /// <see cref="PositioningAIConstants.MARKING_RADIUS_M"/>. Non-finite entries are skipped
        /// per F1 (never propagate NaN). Returns false when no entry qualifies.
        /// </summary>
        /// <param name="agentPosition">The marked agent's own position (m, team-relative frame).</param>
        /// <param name="perceivedOpponentPositions">Perceived opponent positions (from the agent's FilteredView).</param>
        /// <param name="perceivedOpponentIds">Parallel agent ids; must be the same length.</param>
        /// <param name="markerId">AgentId of the nearest qualifying marker; <see cref="MarkingDwellState.NoMarker"/> when none.</param>
        /// <param name="markerPosition">Position of the nearest qualifying marker; undefined when none.</param>
        /// <param name="markerDistance">Distance to the nearest qualifying marker (m); undefined when none.</param>
        public static bool TryFindNearestMarker(
            Vector2 agentPosition,
            ReadOnlySpan<Vector2> perceivedOpponentPositions,
            ReadOnlySpan<int> perceivedOpponentIds,
            out int markerId,
            out Vector2 markerPosition,
            out float markerDistance)
        {
            if (perceivedOpponentPositions.Length != perceivedOpponentIds.Length)
            {
                throw new ArgumentException(
                    "perceivedOpponentPositions and perceivedOpponentIds must be parallel (same length).");
            }

            markerId = MarkingDwellState.NoMarker;
            markerPosition = default;
            markerDistance = float.PositiveInfinity;

            if (!IsFinite(agentPosition))
            {
                return false; // F1: corrupted own position ⇒ no marker this tick (decay path).
            }

            bool found = false;
            for (int i = 0; i < perceivedOpponentPositions.Length; i++)
            {
                Vector2 p = perceivedOpponentPositions[i];
                if (!IsFinite(p))
                {
                    continue; // F1: skip non-finite perceived entries.
                }
                float d = Vector2.Distance(agentPosition, p);
                // NaN-gate pattern: a NaN distance fails the qualifying compare below (compares false).
                if (d <= PositioningAIConstants.MARKING_RADIUS_M && d < markerDistance)
                {
                    found = true;
                    markerId = perceivedOpponentIds[i];
                    markerPosition = p;
                    markerDistance = d;
                }
            }
            return found;
        }

        /// <summary>
        /// Advances one agent's dwell state one heartbeat (#23 §3.2). Out of <see cref="Phase.InPoss"/>
        /// the state decays only (accumulation stops, <c>LastMarkerId</c> stays for cheap resume —
        /// FR-DM-006). A marker-identity switch does not reset dwell; <c>LastMarkerId</c> clears to
        /// <see cref="MarkingDwellState.NoMarker"/> only when dwell reaches zero unmarked.
        /// </summary>
        /// <param name="state">The agent's dwell state entering this heartbeat.</param>
        /// <param name="phase">The team's committed phase this tick.</param>
        /// <param name="markerExists">Whether §3.1's qualifying-marker test found a marker this tick.</param>
        /// <param name="markerId">The nearest qualifying marker's AgentId (ignored when none exists).</param>
        public static MarkingDwellState UpdateDwell(
            in MarkingDwellState state, Phase phase, bool markerExists, int markerId)
        {
            MarkingDwellState next = state;
            if (phase != Phase.InPoss)
            {
                next.DwellTicks = Math.Max(0, state.DwellTicks - PositioningAIConstants.MARKING_DWELL_DECAY_PER_TICK);
                return next; // LastMarkerId stays — cheap resume (#23 §3.2).
            }
            if (markerExists)
            {
                next.DwellTicks = Math.Min(PositioningAIConstants.MARKING_DWELL_FULL_TICKS, state.DwellTicks + 1);
                next.LastMarkerId = markerId;
                return next;
            }
            next.DwellTicks = Math.Max(0, state.DwellTicks - PositioningAIConstants.MARKING_DWELL_DECAY_PER_TICK);
            if (next.DwellTicks == 0)
            {
                next.LastMarkerId = MarkingDwellState.NoMarker;
            }
            return next;
        }

        /// <summary>
        /// MarkingPressure = proximity01 × dwell01 ∈ [0, 1] (#23 §3.1, FM-DM-01). Returns 0 out of
        /// <see cref="Phase.InPoss"/> (FR-DM-006 identity) or when no marker qualifies.
        /// Worked example (§3.1): marker at 1.2 m, dwell 7 → 0.6 × 0.7 = 0.42.
        /// </summary>
        /// <param name="phase">The team's committed phase this tick.</param>
        /// <param name="markerExists">Whether a qualifying marker was found (§3.1 d_marker test).</param>
        /// <param name="markerDistance">Distance to the nearest qualifying marker (m).</param>
        /// <param name="dwellTicks">The agent's current DwellTicks.</param>
        public static float ComputePressure(Phase phase, bool markerExists, float markerDistance, int dwellTicks)
        {
            if (phase != Phase.InPoss || !markerExists)
            {
                return 0f;
            }
            // NaN-gate: !(x > y) form so a NaN distance yields proximity 0, never NaN.
            float proximity01 = Mathf.Clamp01(1f - markerDistance / PositioningAIConstants.MARKING_RADIUS_M);
            if (!(proximity01 > 0f))
            {
                return 0f;
            }
            float dwell01 = Mathf.Min(1f, dwellTicks / (float)PositioningAIConstants.MARKING_DWELL_FULL_TICKS);
            return proximity01 * dwell01;
        }

        /// <summary>
        /// The dismark offset vector (#23 §3.3, FM-DM-02): directly away from the perceived marker,
        /// magnitude <c>DISMARK_OFFSET_MAX_M × pressure × DISMARK_INTENSITY_SCALAR[dial]</c>.
        /// Returns <see cref="Vector2.zero"/> (the exact identity) when the dial is
        /// <see cref="DismarkIntensity.Off"/> (FR-DM-012 — checked FIRST, §6.3 default-cheap), the
        /// pressure is at/below <see cref="PositioningAIConstants.DISMARK_PRESSURE_FLOOR"/>
        /// (FR-DM-009), the marker is degenerate-close (F3), or any input is non-finite (F1).
        /// Caller-side eligibility (outfield, not the carrier — FR-DM-007) is the stage's concern.
        /// Worked example (§3.3): pressure 0.42 at Aggressive → 2.5 × 0.42 × 1.0 = 1.05 m.
        /// </summary>
        /// <param name="agentPosition">The agent's own position (m).</param>
        /// <param name="markerPosition">The nearest qualifying marker's perceived position (m).</param>
        /// <param name="markingPressure">FM-DM-01 output ∈ [0, 1].</param>
        /// <param name="dial">The team's DismarkIntensity dial.</param>
        public static Vector2 ComputeOffset(
            Vector2 agentPosition, Vector2 markerPosition, float markingPressure, DismarkIntensity dial)
        {
            if (dial == DismarkIntensity.Off)
            {
                return Vector2.zero;
            }
            int dialIndex = (int)dial;
            if (dialIndex < 0 || dialIndex >= PositioningAIConstants.DISMARK_INTENSITY_SCALAR.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(dial), dial,
                    "Undefined DismarkIntensity at a consuming seam (F4: refuse, never clamp).");
            }
            // NaN-gate: !(x > floor) form — NaN pressure produces the identity, never a NaN offset.
            if (!(markingPressure > PositioningAIConstants.DISMARK_PRESSURE_FLOOR))
            {
                return Vector2.zero;
            }
            if (!IsFinite(agentPosition) || !IsFinite(markerPosition))
            {
                return Vector2.zero; // F1.
            }
            Vector2 u = agentPosition - markerPosition;
            float len = u.magnitude;
            if (!(len >= PositioningAIConstants.DISMARK_MIN_MARKER_DIST_EPS))
            {
                return Vector2.zero; // F3: coincident marker — deterministic no-op.
            }
            float mag = PositioningAIConstants.DISMARK_OFFSET_MAX_M
                        * Mathf.Clamp01(markingPressure)
                        * PositioningAIConstants.DISMARK_INTENSITY_SCALAR[dialIndex];
            return u * (mag / len);
        }

        private static bool IsFinite(Vector2 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                && !float.IsNaN(v.y) && !float.IsInfinity(v.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#23 T0 scaffolding): marker search, dwell |
// |         |            |        |   state machine, FM-DM-01 pressure, FM-DM-02 offset. Primitive-   |
// |         |            |        |   span signatures per the layering note (Mechanics cannot import  |
// |         |            |        |   the AI-layer FilteredView type; provenance enforced at the      |
// |         |            |        |   match-engine call seam per #23 §4.4).                           |
#endregion
