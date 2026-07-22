// File:     src/match-engine/PlayerAttributeProjection.cs
// Created:  2026-07-17
// Modified: 2026-07-22 (GK/Heading engine integration — ToGoalkeeper/ToHeading added, KD-P8 phantom bar lifted)
// Author:   —
// Spec:     Player-attribute projection design supplement (docs/tracking/player-attribute-projection-design.md)
//           §3 (field-by-field mapping), §4 (KickPower derivation, KD-P1), §5 (runtime split, KD-P4);
//           Squad/Player Data Layer design supplement (#27 candidate) §4 T1/T2; Code Standards #20
// Purpose:  Pure projections from the canonical PlayerDatabase.PlayerAttributes record into the
//           per-spec attribute structs MatchEngine actually seeds (#2/#8/#7/#5/#6 + the three
//           FirstTouchAbility sites). One auditable seam for the whole attribute-sourcing surface.

using UnityEngine;

using TacticalDirector.DecisionTree;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.HeadingMechanics;
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
    /// GK (#11) / Heading (#10) projections landed 2026-07-22 with the #10/#11 engine wiring
    /// (docs/tracking/gk-heading-engine-integration-design.md) — the KD-P8 phantom-consumer bar is
    /// lifted now that <c>MatchEngine</c> constructs and drives both orchestrators and commits intents
    /// seeded from <see cref="ToGoalkeeper"/> / <see cref="ToHeading"/>.
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
        /// Projection into Heading Mechanics #10 attributes (projection design §3.6). Raw <c>int</c>
        /// copy of the three identically-named fields <c>Heading</c> / <c>Strength</c> / <c>Balance</c>;
        /// <paramref name="teamId"/> is match-scoped runtime identity from the caller and
        /// <paramref name="fatigue"/> is live runtime state, never sourced from the roster (KD-P4).
        /// Called for any outfield agent that heads the ball (GK/Heading engine-integration KD-8).
        /// </summary>
        public static HeadingAgentAttributes ToHeading(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, int teamId, float fatigue)
        {
            return new HeadingAgentAttributes
            {
                Heading  = c.Heading,
                Strength = c.Strength,
                Balance  = c.Balance,
                Fatigue  = fatigue,
                TeamId   = teamId
            };
        }

        /// <summary>
        /// Projection into Goalkeeper Mechanics #11 attributes (projection design §3.7). Lossless
        /// <c>int → float</c> widening of the ten identically-named canonical <c>[1,20]</c> fields
        /// (<c>Reflexes</c> / <c>Handling</c> / <c>Composure</c> / <c>Strength</c> / <c>Aerial</c> /
        /// <c>Balance</c> / <c>OneVsOne</c> / <c>Pace</c> / <c>Throwing</c> / <c>Kicking</c>); the
        /// struct's own <c>*Norm</c> accessors do the ÷ ATTR_MAX (20) conversion, so the projection
        /// writes raw values like every other <c>To*</c> here. <paramref name="teamId"/> is
        /// match-scoped runtime identity and <paramref name="fatigue"/> is live runtime state (KD-P4).
        /// Called ONLY for the goalkeeper slot (GK routing gate, engine-integration KD-8 / design §6).
        /// </summary>
        public static GoalkeeperAgentAttributes ToGoalkeeper(
            in TacticalDirector.PlayerDatabase.PlayerAttributes c, int teamId, float fatigue)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes  = c.Reflexes,
                Handling  = c.Handling,
                Composure = c.Composure,
                Strength  = c.Strength,
                Aerial    = c.Aerial,
                Balance   = c.Balance,
                OneVsOne  = c.OneVsOne,
                Pace      = c.Pace,
                Throwing  = c.Throwing,
                Kicking   = c.Kicking,
                Fatigue   = fatigue,
                TeamId    = teamId
            };
        }

        /// <summary>
        /// The sole <c>[1,20] → [0,1]</c> scale conversion (projection design §2 / KD-P3), for the
        /// pre-normalized <c>AttackingAgentSnapshot</c> pace/dribbling pair: <c>÷ ATTRIBUTE_MAX</c>
        /// (20), so neutral 10 → 0.5 — exactly the pre-T1 <c>STAGE0_NEUTRAL_NORMALIZED</c> seed.
        /// (The struct's original <c>(raw−1)/19</c> doc convention — the flagged pre-existing
        /// mismatch, design §2 — was aligned to this live ÷20 convention in the T1 repeat-AR
        /// (<c>AttackingAgentSnapshot.cs</c> v1.1); switching the MATH to <c>(raw−1)/19</c> remains a
        /// recorded deferred design question, since it moves the neutral off 0.5.)
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
// | 1.1     | 2026-07-17 | —      | AR-4 (doc-only): ToNormalized note updated — the Attacking     |
// |         |            |        | struct's (raw−1)/19 doc mismatch is now aligned to the live    |
// |         |            |        | ÷20 convention (AttackingAgentSnapshot.cs v1.1); the math      |
// |         |            |        | switch stays a deferred design question.                       |
// | 1.2     | 2026-07-22 | —      | GK/Heading engine integration: ToHeading (#10, raw int copy    |
// |         |            |        | Heading/Strength/Balance) + ToGoalkeeper (#11, int→float widen |
// |         |            |        | of the ten GK fields) added; KD-P8 "deliberately ABSENT" note  |
// |         |            |        | removed now that MatchEngine gives both a live consumer.       |
#endregion
