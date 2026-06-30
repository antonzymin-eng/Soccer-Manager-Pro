// File:     src/decision-tree/UtilityScorer.cs
// Created:  2026-05-29
// Modified: 2026-06-28 (#21 T2 mentality risk multiplier)
// Author:   —
// Spec:     Decision Tree #8 §3.2, §3.4, Tactical Instructions #21 §3.2, Code Standards #20
// Purpose:  Step 4 of the 6-step pipeline. Applies the utility scoring model to each
//           ActionOption, populating BaseUtility. Pure function: no side effects.

using UnityEngine;

using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 4: computes BaseUtility for every ActionOption using the §3.2 formulas.
    /// Applies zone modifier, attribute multiplier, context modifier, tactical modifier,
    /// and risk penalty. Clamps result to [UTILITY_FLOOR, UTILITY_CEILING]. §3.2.
    /// </summary>
    internal static class UtilityScorer
    {
        /// <summary>
        /// Scores all options in optionBuffer[0..count-1] in place.
        /// Sets ActionOption.BaseUtility for each entry. §3.2.
        /// </summary>
        internal static void ScoreOptions(
            ActionOption[] optionBuffer,
            int count,
            in DecisionContext ctx)
        {
            for (int i = 0; i < count; i++)
            {
                optionBuffer[i].BaseUtility = ComputeUtility(ref optionBuffer[i], in ctx);
            }
        }

        private static float ComputeUtility(ref ActionOption opt, in DecisionContext ctx)
        {
            float u;
            switch (opt.Type)
            {
                case ActionType.PASS:            u = ScorePass(ref opt, in ctx);      break;
                case ActionType.SHOOT:           u = ScoreShoot(ref opt, in ctx);     break;
                case ActionType.DRIBBLE:         u = ScoreDribble(ref opt, in ctx);   break;
                case ActionType.HOLD:            u = ScoreHold(ref opt, in ctx);      break;
                case ActionType.MOVE_TO_POSITION: u = ScoreMove(ref opt, in ctx);    break;
                case ActionType.PRESS:           u = ScorePress(ref opt, in ctx);     break;
                case ActionType.INTERCEPT:       u = ScoreIntercept(ref opt, in ctx); break;
                default:                         u = UtilityWeights.UTILITY_FLOOR;    break;
            }

            // #21 §3.2/§3.3 (T2): team mentality risk multiplier, applied to every scored option
            // BEFORE the [FLOOR, CEILING] clamp. Higher mentality lifts PASS/SHOOT/DRIBBLE relative
            // to HOLD. The Stage 0 / no-instruction default Mentality.Balanced resolves to ×1.00
            // (FR-TI-031), so this is exactly today's behaviour until the match-engine Phase-D writer
            // routes a live tactic in.
            u *= TacticTranslation.MentalityRiskMultiplier(ctx.TacticalContext.Mentality);

            // #21 §3.3 (T2): the per-agent role/duty/instruction × team-tempo utility product, applied to
            // every scored option BEFORE the clamp. The Stage 0 / no-instruction defaults (PlayerRole.
            // Default / Duty.Support / every InstrBias.Default at Tempo.Standard) resolve to exactly ×1.0
            // (FR-TI-031), so this is byte-identical to today's behaviour until the Phase-D writer routes a
            // live per-agent PlayerTactic / team Tempo. Magnitudes are the §5.6 / G2-pinned defaults.
            u *= TacticTranslation.PlayerTacticActionMultiplier(
                ctx.TacticalContext.PlayerTactic, ctx.TacticalContext.Tempo, opt.Type);

            float clamped = Mathf.Clamp(u, UtilityWeights.UTILITY_FLOOR, UtilityWeights.UTILITY_CEILING);

            // AR-3 L: Mathf.Clamp passes NaN through (NaN comparisons are false), so a
            // non-finite utility — from a corrupt attribute or position upstream — would
            // otherwise survive into selection, where ActionSelector picks index 0 when
            // every comparison is false and could dispatch the NaN option. Fail closed to
            // the floor (project NaN-gate pattern: AM AR-10 / CS AR-7 / FT AR-8).
            return float.IsNaN(clamped) ? UtilityWeights.UTILITY_FLOOR : clamped;
        }

        // ── §3.2.2 PASS ────────────────────────────────────────────────────────

        private static float ScorePass(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(opt.Type, ctx.BallZone);

            // Shifted attribute form: (0.5 + A × 0.5)^exp
            float visionFactor    = Mathf.Pow(0.5f + ctx.A_Vision  * 0.5f, UtilityWeights.PASS_VISION_EXP);
            float techniqueFactor = Mathf.Pow(0.5f + ctx.A_Passing * 0.5f, UtilityWeights.PASS_TECHNIQUE_EXP);
            float am = visionFactor * techniqueFactor;

            float contextM = opt.AdjustedPassLaneScore;

            float p   = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Passing) * UtilityWeights.PASS_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_PASS * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.PASS, in ctx.TacticalContext, ctx.OpponentHasBall, opt.IntendedDistance);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── §3.2.3 SHOOT ───────────────────────────────────────────────────────

        private static float ScoreShoot(ref ActionOption opt, in DecisionContext ctx)
        {
            // Zone modifier: midfield depends on LongShots attribute
            float zoneM;
            FieldZone zone = ctx.BallZone;
            if (zone == FieldZone.ATTACKING)
            {
                zoneM = UtilityWeights.SHOOT_ZONE_ATT;
            }
            else if (zone == FieldZone.MIDFIELD)
            {
                // Long-shot capable agents can fire from midfield (§3.2.3.1).
                // AR-2 M-4: the gate compares the SHIFTED form (0.5 + A × 0.5) against
                // LONG_SHOT_THRESHOLD = 0.75 — effective raw LongShots ≥ 11 (§3.2.3.4
                // explicitly derives this and rejects the raw-form reading, which
                // required raw ≥ 16 and suppressed midfield shots for raw 11–15).
                zoneM = (0.5f + ctx.A_LongShots * 0.5f) > UtilityWeights.LONG_SHOT_THRESHOLD
                    ? UtilityWeights.SHOOT_ZONE_MID_LONG
                    : UtilityWeights.SHOOT_ZONE_MID_SHORT;
            }
            else
            {
                zoneM = UtilityWeights.SHOOT_ZONE_DEF;
            }

            float finishFactor   = Mathf.Pow(0.5f + ctx.A_Finishing * 0.5f, UtilityWeights.SHOOT_FINISHING_EXP);
            float composureFactor = Mathf.Pow(0.5f + ctx.A_Composure * 0.5f, UtilityWeights.SHOOT_COMPOSURE_EXP);
            float am = finishFactor * composureFactor;

            // GoalOpeningScore is floored at GOAL_OPENING_MIN inside
            // ComputeGoalOpeningScore (§3.2.3.2 step 5); defensive re-floor retained
            // for direct-injection test paths.
            float goalOpeningScore = Mathf.Max(opt.GoalOpeningScore, UtilityWeights.GOAL_OPENING_MIN);

            // AR-2 M-3: §3.2.3.1 RiskPenalty_SHOOT = (1 − GoalOpeningScore) × P × coeff
            // (a blocked shot is the risk driver). Previous form used (1 − A_Finishing).
            float p    = ctx.PressureScalar;
            float risk = (1.0f - goalOpeningScore) * p * UtilityWeights.SHOOT_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_SHOOT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.SHOOT, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * goalOpeningScore * tactM * (1.0f - risk);
        }

        // ── §3.2.4 DRIBBLE ─────────────────────────────────────────────────────

        private static float ScoreDribble(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.DRIBBLE, ctx.BallZone);

            // Raw attribute form (no 0.5 shift per §3.2.4 design note)
            float dribblingFactor = Mathf.Pow(ctx.A_Dribbling, UtilityWeights.DRIBBLE_DRIBBLING_EXP);
            float agilityFactor   = Mathf.Pow(ctx.A_Agility,   UtilityWeights.DRIBBLE_AGILITY_EXP);
            float am = dribblingFactor * agilityFactor;

            float contextM = opt.SpaceScore;

            float p    = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Dribbling) * UtilityWeights.DRIBBLE_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_DRIBBLE * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.DRIBBLE, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── §3.2.5 HOLD ────────────────────────────────────────────────────────

        private static float ScoreHold(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.HOLD, ctx.BallZone);

            float composureFactor = Mathf.Pow(0.5f + ctx.A_Composure * 0.5f, UtilityWeights.HOLD_COMPOSURE_EXP);

            float p    = ctx.PressureScalar;
            float risk = p * UtilityWeights.HOLD_PRESSURE_COEFF;

            float baseU = UtilityWeights.U_BASE_HOLD * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.HOLD, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * composureFactor * tactM * (1.0f - risk);
        }

        // ── §3.2.6 MOVE_TO_POSITION ────────────────────────────────────────────

        private static float ScoreMove(ref ActionOption opt, in DecisionContext ctx)
        {
            // Zone modifier: uniform 1.0 at Stage 0 (§3.2.1.3 MOVE row) — read from
            // the catalogue so the MOVE_ZONE_* constants are the single live surface.
            float zoneM = GetZoneModifier(ActionType.MOVE_TO_POSITION, ctx.BallZone);

            float positioningFactor = Mathf.Pow(0.5f + ctx.A_Positioning * 0.5f, UtilityWeights.MOVE_POSITIONING_EXP);
            float workRateFactor    = Mathf.Pow(0.5f + ctx.A_WorkRate    * 0.5f, UtilityWeights.MOVE_WORKRATE_EXP);
            float am = positioningFactor * workRateFactor;

            // Distance modifier: urgency scales linearly to formation slot distance (§3.2.6)
            float distM = Mathf.Max(
                Mathf.Clamp01(opt.DistanceToSlot / UtilityWeights.MOVE_URGENCY_DIST_M),
                UtilityWeights.MOVE_DIST_MIN);

            // Phase modifier (§3.2.6) — use assembled possession state (handles CONTESTED)
            float phaseM = GetMovePhaseModifier(ctx.PossessedByTeam, ctx.AgentTeamId);

            // Press proximity penalty (§3.2.6)
            float pressM = ComputeMovePressProximity(in ctx);

            float baseU = UtilityWeights.U_BASE_MOVE * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.MOVE_TO_POSITION, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * distM * phaseM * pressM * tactM;
        }

        private static float GetMovePhaseModifier(PossessionState possession, int agentTeamId)
        {
            switch (possession)
            {
                case PossessionState.CONTESTED: return UtilityWeights.MOVE_PHASE_CONTESTED;
                case PossessionState.HOME_TEAM:
                    return agentTeamId == 0 ? UtilityWeights.MOVE_PHASE_OWN_TEAM : UtilityWeights.MOVE_PHASE_OPPONENT;
                case PossessionState.AWAY_TEAM:
                    return agentTeamId == 1 ? UtilityWeights.MOVE_PHASE_OWN_TEAM : UtilityWeights.MOVE_PHASE_OPPONENT;
                default:
                    return UtilityWeights.MOVE_PHASE_CONTESTED;
            }
        }

        private static float ComputeMovePressProximity(in DecisionContext ctx)
        {
            FilteredView snap = ctx.Snapshot;
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                float dist = Vector2.Distance(ctx.AgentPosition, snap.VisibleOpponents[i].PerceivedPosition);
                if (dist <= UtilityWeights.MOVE_PRESS_SUPPRESSION_DIST)
                    return UtilityWeights.MOVE_PRESS_SUPPRESSION_FACTOR;
            }
            return 1.0f;
        }

        // ── §3.2.7 PRESS ───────────────────────────────────────────────────────

        private static float ScorePress(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.PRESS, ctx.BallZone);

            // §3.2.7.1: all three attributes use the shifted form (0.5 + A × 0.5)
            float aggressionFactor = Mathf.Pow(0.5f + ctx.A_Aggression * 0.5f, UtilityWeights.PRESS_AGGRESSION_EXP);
            float workRateFactor   = Mathf.Pow(0.5f + ctx.A_WorkRate   * 0.5f, UtilityWeights.PRESS_WORKRATE_EXP);
            float staminaFactor    = Mathf.Pow(0.5f + ctx.A_Stamina    * 0.5f, UtilityWeights.PRESS_STAMINA_EXP);
            float am = aggressionFactor * workRateFactor * staminaFactor;

            float contextM = opt.ProximityScore;

            // Tactical pressing modifier (§3.2.7 / §3.4.3)
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.PRESS, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            float baseU = UtilityWeights.U_BASE_PRESS * zoneM;
            return baseU * am * contextM * tactM;
        }

        // ── §3.2.8 INTERCEPT ───────────────────────────────────────────────────

        private static float ScoreIntercept(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.INTERCEPT, ctx.BallZone);

            // Anticipation: raw form (§3.2.8 — no 0.5 shift for Anticipation)
            // Pace: shifted form
            float anticipationFactor = Mathf.Pow(ctx.A_Anticipation,              UtilityWeights.INTERCEPT_ANTICIPATION_EXP);
            float paceFactor         = Mathf.Pow(0.5f + ctx.A_Pace * 0.5f,        UtilityWeights.INTERCEPT_PACE_EXP);
            float am = anticipationFactor * paceFactor;

            float contextM = opt.InterceptFeasibilityScore;

            // AR-2 M-6: §3.2.8.1 pressure term is (1 − P × INTERCEPT_PRESSURE_COEFF),
            // independent of Anticipation. The previous (1 − A_Anticipation) factor is
            // not in the spec formula.
            float p    = ctx.PressureScalar;
            float risk = p * UtilityWeights.INTERCEPT_PRESSURE_COEFF;

            float baseU = UtilityWeights.U_BASE_INTERCEPT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.INTERCEPT, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── Zone modifier table (§3.2.1.3) ────────────────────────────────────

        private static float GetZoneModifier(ActionType type, FieldZone zone)
        {
            switch (type)
            {
                case ActionType.PASS:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.PASS_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.PASS_ZONE_ATT
                         : UtilityWeights.PASS_ZONE_MID;

                case ActionType.DRIBBLE:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.DRIBBLE_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.DRIBBLE_ZONE_ATT
                         : UtilityWeights.DRIBBLE_ZONE_MID;

                case ActionType.HOLD:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.HOLD_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.HOLD_ZONE_ATT
                         : UtilityWeights.HOLD_ZONE_MID;

                case ActionType.PRESS:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.PRESS_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.PRESS_ZONE_ATT
                         : UtilityWeights.PRESS_ZONE_MID;

                case ActionType.INTERCEPT:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.INTERCEPT_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.INTERCEPT_ZONE_ATT
                         : UtilityWeights.INTERCEPT_ZONE_MID;

                case ActionType.MOVE_TO_POSITION:
                    return zone == FieldZone.DEFENSIVE ? UtilityWeights.MOVE_ZONE_DEF
                         : zone == FieldZone.ATTACKING ? UtilityWeights.MOVE_ZONE_ATT
                         : UtilityWeights.MOVE_ZONE_MID;

                default: // SHOOT zone modifier handled in ScoreShoot (LongShots-conditional)
                    return 1.0f;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: ScoreMove uses ctx.PossessedByTeam (handles CONTESTED) instead of    |
// |         |            |        |   ctx.MatchContext.Possession.                                                  |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: H-2 all zone reads moved to team-relative ctx.BallZone; M-1        |
// |         |            |        |   TacticalModifierResolver calls pass ctx.OpponentHasBall; M-3 SHOOT risk uses |
// |         |            |        |   (1 − GoalOpeningScore) per §3.2.3.1; M-4 midfield long-shot gate compares    |
// |         |            |        |   the shifted attribute form (raw ≥ 11 per §3.2.3.4); M-6 INTERCEPT pressure   |
// |         |            |        |   term drops the non-spec (1 − A_Anticipation) factor; L MOVE zone modifier    |
// |         |            |        |   read from MOVE_ZONE_* catalogue entries.                                      |
// | 1.3     | 2026-06-12 | —      | Build fix (dotnet CI gate): missing 'using TacticalDirector.PerceptionSystem;' |
// |         |            |        | - FilteredView (ctx.Snapshot local, ScoreMove SS 3.2) was CS0246, so the       |
// |         |            |        | decision-tree assembly STILL did not compile after the June 11 AR-2 H-1        |
// |         |            |        | asmdef/static-call fixes. No functional change.                                |
// | 1.4     | 2026-06-14 | —      | Audit AR-3 L: ComputeUtility fails closed to UTILITY_FLOOR on a non-finite     |
// |         |            |        | result — Mathf.Clamp passes NaN, which would otherwise win selection           |
// |         |            |        | (ActionSelector picks index 0 when all comparisons are false). NaN-gate        |
// |         |            |        | pattern (AM AR-10 / CS AR-7 / FT AR-8).                                         |
// | 1.5     | 2026-06-28 | —      | #21 T2: per-option utility × Mentality risk multiplier (§3.2/§3.3) before the  |
// |         |            |        | clamp; resolved via TacticTranslation. Balanced default ⇒ ×1.0 (FR-TI-031),    |
// |         |            |        | behaviour-neutral until match-engine Phase-D routes a live tactic.             |
// | 1.6     | 2026-06-29 | —      | #21 §3.3: per-option × PlayerTacticActionMultiplier (per-agent role/duty/instr |
// |         |            |        | × team tempo product) before the clamp. Identity PlayerTactic + Tempo.Standard |
// |         |            |        | ⇒ ×1.0 (FR-TI-031), behaviour-neutral. Magnitudes illustrative (G2).           |
// | 1.7     | 2026-06-30 | —      | #21 §5.6 / G2 balance pass: doc reframed illustrative → pinned (no code change).|
#endregion
