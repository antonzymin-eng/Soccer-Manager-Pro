// File:     src/match-engine/PlayerAttributeProjection.cs
// Created:  2026-07-17
// Modified: 2026-07-17
// Author:   —
// Spec:     Player-attribute projection design supplement (docs/tracking/player-attribute-projection-design.md)
//           §3 (field-by-field mapping), §4 (KickPower derivation, KD-P1), §5 (runtime split, KD-P4);
//           Squad/Player Data Layer design supplement (#27 candidate) §4 T1/T2; Code Standards #20
// Purpose:  Pure projections from the canonical PlayerDatabase.PlayerAttributes record into the
//           per-spec attribute structs MatchEngine actually seeds (#2/#8/#7/#5/#6 + the three
//           FirstTouchAbility sites). One auditable seam for the whole attribute-sourcing surface.

using UnityEngine;

using TacticalDirector.DecisionTree;
using TacticalDirector.PassMechanics;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Pure, allocation-free projections from the canonical 31-field
    /// <see cref="TacticalDirector.PlayerDatabase.PlayerAttributes"/> record into the per-spec
    /// attribute structs <see cref="MatchEngine"/> seeds (projection design §3). Every method is a
    /// raw <c>[1,20]</c> value copy unless documented otherwise (KD-P2); the sole scale conversion
    /// is <see cref="ToNormalized"/> for the pre-normalized <c>AttackingAgentSnapshot</c>
    /// pace/dribbling pair (KD-P3, ÷ ATTRIBUTE_MAX so neutral 10 → 0.5). Runtime state
    /// (Fatigue / TeamId / IsHalfTurned) is caller-supplied, never sourced from a squad (KD-P4).
    /// The canonical-neutral record projects to exactly the pre-T1 <c>STAGE0_NEUTRAL_*</c> /
    /// <c>CreateDefault()</c> seeds at every live site, so the no-squad match stays byte-identical
    /// (KD-P7; locked by <c>PlayerAttributeProjectionTests</c>).
    /// CS0104 note (KD-P6): the canonical record's bare type name collides with
    /// <c>AgentMovement.PlayerAttributes</c>, so the canonical type is fully qualified throughout —
    /// this assembly must never add a <c>using TacticalDirector.PlayerDatabase;</c> directive.
    /// GK (#11) / Heading (#10) projections are deliberately ABSENT: MatchEngine builds neither
    /// struct today, and writing them here would be a phantom consumer (KD-P8); they land with
    /// those specs' engine integration per the design doc §3.6/§3.7 forward-compat mappings.
    /// </summary>
    public static class PlayerAttributeProjection
    {
        /// <summary>
        /// Projection into Agent Movement #2 locomotion attributes (starters and bench). Raw
        /// <c>int</c> copy of the six identically-named physical fields (projection design §3.1).
        /// </summary>
        public static AgentMovement.PlayerAttributes ToAgentMovement(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c)
        {
            return new AgentMovement.PlayerAttributes
            {
                Pace         = c.Pace,
                Acceleration = c.Acceleration,
                Agility      = c.Agility,
                Balance      = c.Balance,
                Strength     = c.Strength,
                Stamina      = c.Stamina
            };
        }

        /// <summary>
        /// Projection into Decision Tree #8 attributes. Raw <c>int</c> copy of the fifteen
        /// identically-named fields (projection design §3.2; <c>Crossing</c> stays
        /// declared-but-unconsumed per ERR-008-006). <paramref name="teamId"/> is match-scoped
        /// runtime identity from the caller, never the club roster (KD-P4 / #27 KD-3).
        /// </summary>
        public static DtAgentAttributes ToDecisionTree(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, int teamId)
        {
            return new DtAgentAttributes
            {
                Decisions    = c.Decisions,
                Vision       = c.Vision,
                Passing      = c.Passing,
                Finishing    = c.Finishing,
                Dribbling    = c.Dribbling,
                LongShots    = c.LongShots,
                Crossing     = c.Crossing,
                Composure    = c.Composure,
                Anticipation = c.Anticipation,
                Pace         = c.Pace,
                Agility      = c.Agility,
                WorkRate     = c.WorkRate,
                Stamina      = c.Stamina,
                Aggression   = c.Aggression,
                Positioning  = c.Positioning,
                TeamId       = teamId
            };
        }

        /// <summary>
        /// Projection into Perception #7 attributes. Raw <c>int</c> copy of Decisions/Anticipation
        /// (projection design §3.3). <paramref name="teamId"/> and <paramref name="isHalfTurned"/>
        /// are caller-supplied runtime state (KD-P4).
        /// </summary>
        public static PerceptionAgentAttributes ToPerception(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, int teamId, bool isHalfTurned)
        {
            return new PerceptionAgentAttributes
            {
                Decisions    = c.Decisions,
                Anticipation = c.Anticipation,
                TeamId       = teamId,
                IsHalfTurned = isHalfTurned
            };
        }

        /// <summary>
        /// Projection into Pass Mechanics #5 attributes (projection design §3.4). Lossless
        /// <c>int → float</c> widening of the raw <c>[1,20]</c> values; <c>WeakFootRating</c> stays
        /// on its own <c>[1,5]</c> scale (#27 KD-2). <c>KickPower</c> has no canonical source and is
        /// derived <c>(Passing + Technique) × 0.5</c> (KD-P1 — the [TEMPORARY-PROXY-ERR-007] formula
        /// computed from real varied attributes; any mean of neutral-10 inputs is 10, preserving the
        /// neutral seed). <paramref name="fatigue"/> is live runtime state (KD-P4).
        /// </summary>
        public static PassAgentAttributes ToPass(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, float fatigue)
        {
            return new PassAgentAttributes
            {
                Passing        = c.Passing,
                Technique      = c.Technique,
                KickPower      = (c.Passing + c.Technique) * 0.5f,
                WeakFootRating = c.WeakFootRating,
                Crossing       = c.Crossing,
                Fatigue        = fatigue
            };
        }

        /// <summary>
        /// Projection into Shot Mechanics #6 attributes (projection design §3.5). Raw <c>int</c>
        /// copies; <c>WeakFootRating</c> stays <c>[1,5]</c>. <c>KickPower</c> is derived
        /// <c>RoundToInt((Finishing + LongShots) × 0.5)</c> — the rounding mode is pinned to
        /// <see cref="Mathf.RoundToInt"/>, the exact call the pre-T1 neutral seed site used
        /// (design §4 L-1). <paramref name="fatigue"/> is live runtime state (KD-P4).
        /// </summary>
        public static ShotAgentAttributes ToShot(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, float fatigue)
        {
            return new ShotAgentAttributes
            {
                Finishing      = c.Finishing,
                LongShots      = c.LongShots,
                Composure      = c.Composure,
                KickPower      = Mathf.RoundToInt((c.Finishing + c.LongShots) * 0.5f),
                Technique      = c.Technique,
                WeakFootRating = c.WeakFootRating,
                Fatigue        = fatigue
            };
        }

        /// <summary>
        /// The first-touch-ability attribute consumed by the three live sites — Pressing #13
        /// <c>FirstTouchAttribute</c>, Defensive #14 <c>PerceivedFirstTouch</c> (both widen to
        /// <c>float</c> at the call site) and First Touch #4 <c>FirstTouchContext.FirstTouchAttribute</c>
        /// (<c>int</c>). Raw copy of canonical <c>FirstTouchAbility</c> (projection design §3.5a /
        /// KD-P9 — consumed, not RESERVED). Projected for every agent; no GK gate (KD-P5).
        /// </summary>
        public static int FirstTouchAbility(in TacticalDirector.PlayerDatabase.PlayerAttributes c)
        {
            return c.FirstTouchAbility;
        }

        /// <summary>
        /// The sole <c>[1,20] → [0,1]</c> scale conversion (projection design §2 / KD-P3), for the
        /// pre-normalized <c>AttackingAgentSnapshot</c> pace/dribbling pair: <c>÷ ATTRIBUTE_MAX</c>
        /// (20), so neutral 10 → 0.5 — exactly the pre-T1 <c>STAGE0_NEUTRAL_NORMALIZED</c> seed.
        /// (The struct-doc <c>(raw−1)/19</c> convention mismatch is a flagged pre-existing,
        /// unconsumed defect handled in its own pass — design §2.)
        /// </summary>
        public static float ToNormalized(int canonical1To20)
        {
            return canonical1To20 / (float)TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.ATTRIBUTE_MAX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-17 | —      | Initial implementation (#27 T1/T2 — projection design v0.3).   |
#endregion
