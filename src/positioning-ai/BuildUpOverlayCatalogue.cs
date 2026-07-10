// File:     src/positioning-ai/BuildUpOverlayCatalogue.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §3.2 (FM-BU-02), Appendix A (row keys per ERR-024-001),
//           FR-BU-007/008/009, Code Standards #20
// Purpose:  The [GT] build-up overlay tables: additive per-slot displacements keyed by
//           (structure, zone, DefaultLine, lane class), lane-symmetric magnitudes with the
//           lateral sign resolved toward pitch centre per §3.2.

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Build-up overlay catalogue (#24 Appendix A; all rows [GT], metres, team-relative frame).
    /// Rows are addressed by the slot's EXISTING <see cref="FormationSlotRecord.DefaultLine"/> /
    /// <see cref="FormationSlotRecord.DefaultLane"/> (FR-BU-007) — no new slot identity. Lanes are
    /// grouped into symmetric classes (half-space = LH/RH; centre = C; wide = LW/RW) so one row
    /// serves both flanks; the Δy magnitude resolves toward pitch centre per lane side
    /// (y &lt; 34 ⇒ +, y &gt; 34 ⇒ −, 0 at exactly y = 34 — §3.2 / PASS-1 L-1).
    /// ROW-KEY NOTE (ERR-024-001): the spec's PASS-1 M-3 keys ("fullbacks occupy the wide L/R
    /// lanes") contradicted the actual family tables — every family records fullbacks at
    /// DefaultLane LH/RH and central mids at C — so the catalogue is keyed to the recorded values
    /// (fullback rows = DEF + half-space; central rows = C); Appendix A patched in the same commit.
    /// Unlisted (structure, zone, line, lane) combinations are (0, 0); FinalThird is all-zero by
    /// rule (FR-BU-004); the goalkeeper receives no offset (FR-BU-009 — caller excludes GK slots).
    /// Magnitudes illustrative pending the balance pass (#21 G2 precedent).
    /// </summary>
    public static class BuildUpOverlayCatalogue
    {
        /// <summary>
        /// Resolves the overlay displacement for one slot (FM-BU-02). Returns the signed
        /// team-relative Δ; <see cref="Vector2.zero"/> for <see cref="BuildUpStructure.None"/>
        /// (FR-BU-005 identity — checked first), FinalThird, and every unlisted combination.
        /// </summary>
        /// <param name="structure">The team's BuildUpStructure dial.</param>
        /// <param name="zone">The committed build-up zone this heartbeat (§3.1).</param>
        /// <param name="line">The slot's <see cref="FormationSlotRecord.DefaultLine"/>.</param>
        /// <param name="lane">The slot's <see cref="FormationSlotRecord.DefaultLane"/>.</param>
        /// <param name="slotYTeamRelative">The slot's team-relative y (m) — resolves the lateral sign.</param>
        public static Vector2 GetOffset(
            BuildUpStructure structure, BuildUpZone zone, LineId line, LaneId lane, float slotYTeamRelative)
        {
            if (structure == BuildUpStructure.None || zone == BuildUpZone.FinalThird)
            {
                return Vector2.zero;
            }

            float dx;
            float dyTowardCentre;
            switch (structure)
            {
                case BuildUpStructure.BackThree:
                    if (line == LineId.Defense && IsHalfSpace(lane))
                    {
                        // Fullbacks tuck beside the CBs (Appendix A.1).
                        (dx, dyTowardCentre) = zone == BuildUpZone.OwnThird ? (-4.0f, 6.0f) : (-2.0f, 3.0f);
                    }
                    else if (line == LineId.Midfield && lane == LaneId.C)
                    {
                        // Central mids drop toward the back line (Appendix A.1).
                        (dx, dyTowardCentre) = zone == BuildUpZone.OwnThird ? (-4.0f, 0.0f) : (-2.0f, 0.0f);
                    }
                    else
                    {
                        return Vector2.zero;
                    }
                    break;

                case BuildUpStructure.DoublePivot:
                    if (line == LineId.Midfield && lane == LaneId.C)
                    {
                        // Central mids form the deep pivot (Appendix A.2).
                        (dx, dyTowardCentre) = zone == BuildUpZone.OwnThird ? (-5.0f, 4.0f) : (-2.5f, 2.0f);
                    }
                    else if (line == LineId.Attack && lane == LaneId.C && zone == BuildUpZone.OwnThird)
                    {
                        // Central forwards drop to link (Appendix A.2).
                        (dx, dyTowardCentre) = (-3.0f, 0.0f);
                    }
                    else
                    {
                        return Vector2.zero;
                    }
                    break;

                case BuildUpStructure.InvertedFullBacks:
                    if (line == LineId.Defense && IsHalfSpace(lane))
                    {
                        // Fullbacks step in and up toward the half-space (Appendix A.3).
                        (dx, dyTowardCentre) = zone == BuildUpZone.OwnThird ? (2.0f, 6.0f) : (3.0f, 8.0f);
                    }
                    else
                    {
                        return Vector2.zero;
                    }
                    break;

                default:
                    return Vector2.zero; // Undefined structure bytes are refused at the routing seam (F3), never here.
            }

            return new Vector2(dx, dyTowardCentre * CentreSign(slotYTeamRelative));
        }

        /// <summary>Lateral sign toward pitch centre: +1 below y = 34, −1 above, 0 at exactly 34 (§3.2 / PASS-1 L-1).</summary>
        private static float CentreSign(float y)
        {
            if (y < PositioningAIConstants.PITCH_HALF_WIDTH_M)
            {
                return 1f;
            }
            if (y > PositioningAIConstants.PITCH_HALF_WIDTH_M)
            {
                return -1f;
            }
            return 0f;
        }

        private static bool IsHalfSpace(LaneId lane)
        {
            return lane == LaneId.LH || lane == LaneId.RH;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#24 T0 scaffolding). Row keys follow the  |
// |         |            |        |   actual FormationSlotRecord DefaultLane data, not the Appendix A |
// |         |            |        |   v0.2 lane labels (ERR-024-001 — the spec keys matched no slot   |
// |         |            |        |   and would have made the whole catalogue a structural no-op).    |
#endregion
