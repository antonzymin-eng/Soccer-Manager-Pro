// File:     src/decision-tree/UtilityScorer.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.2, §3.4, Code Standards #20
// Purpose:  Step 4 of the 6-step pipeline. Applies the utility scoring model to each
//           ActionOption, populating BaseUtility. Pure function: no side effects.

using UnityEngine;

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

            return Mathf.Clamp(u, UtilityWeights.UTILITY_FLOOR, UtilityWeights.UTILITY_CEILING);
        }

        // ── §3.2.2 PASS ────────────────────────────────────────────────────────

        private static float ScorePass(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(opt.Type, ctx.MatchContext.BallZone);

            // Shifted attribute form: (0.5 + A × 0.5)^exp
            float visionFactor    = Mathf.Pow(0.5f + ctx.A_Vision  * 0.5f, UtilityWeights.PASS_VISION_EXP);
            float techniqueFactor = Mathf.Pow(0.5f + ctx.A_Passing * 0.5f, UtilityWeights.PASS_TECHNIQUE_EXP);
            float am = visionFactor * techniqueFactor;

            float contextM = opt.AdjustedPassLaneScore;

            float p   = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Passing) * UtilityWeights.PASS_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_PASS * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.PASS, in ctx.TacticalContext, ctx.PossessedByTeam, opt.IntendedDistance);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── §3.2.3 SHOOT ───────────────────────────────────────────────────────

        private static float ScoreShoot(ref ActionOption opt, in DecisionContext ctx)
        {
            // Zone modifier: midfield depends on LongShots attribute
            float zoneM;
            FieldZone zone = ctx.MatchContext.BallZone;
            if (zone == FieldZone.ATTACKING)
            {
                zoneM = UtilityWeights.SHOOT_ZONE_ATT;
            }
            else if (zone == FieldZone.MIDFIELD)
            {
                // Long-shot capable agents can fire from midfield (§3.2.3)
                zoneM = ctx.A_LongShots >= UtilityWeights.LONG_SHOT_THRESHOLD
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

            float goalOpeningScore = Mathf.Max(opt.GoalOpeningScore, UtilityWeights.GOAL_OPENING_MIN);

            float p    = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Finishing) * UtilityWeights.SHOOT_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_SHOOT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.SHOOT, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

            return baseU * am * goalOpeningScore * tactM * (1.0f - risk);
        }

        // ── §3.2.4 DRIBBLE ─────────────────────────────────────────────────────

        private static float ScoreDribble(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.DRIBBLE, ctx.MatchContext.BallZone);

            // Raw attribute form (no 0.5 shift per §3.2.4 design note)
            float dribblingFactor = Mathf.Pow(ctx.A_Dribbling, UtilityWeights.DRIBBLE_DRIBBLING_EXP);
            float agilityFactor   = Mathf.Pow(ctx.A_Agility,   UtilityWeights.DRIBBLE_AGILITY_EXP);
            float am = dribblingFactor * agilityFactor;

            float contextM = opt.SpaceScore;

            float p    = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Dribbling) * UtilityWeights.DRIBBLE_RISK_COEFF;

            float baseU = UtilityWeights.U_BASE_DRIBBLE * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.DRIBBLE, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── §3.2.5 HOLD ────────────────────────────────────────────────────────

        private static float ScoreHold(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.HOLD, ctx.MatchContext.BallZone);

            float composureFactor = Mathf.Pow(0.5f + ctx.A_Composure * 0.5f, UtilityWeights.HOLD_COMPOSURE_EXP);

            float p    = ctx.PressureScalar;
            float risk = p * UtilityWeights.HOLD_PRESSURE_COEFF;

            float baseU = UtilityWeights.U_BASE_HOLD * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.HOLD, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

            return baseU * composureFactor * tactM * (1.0f - risk);
        }

        // ── §3.2.6 MOVE_TO_POSITION ────────────────────────────────────────────

        private static float ScoreMove(ref ActionOption opt, in DecisionContext ctx)
        {
            // Zone modifier: uniform (all 1.0)
            float zoneM = 1.0f;

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
                ActionType.MOVE_TO_POSITION, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

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
            float zoneM = GetZoneModifier(ActionType.PRESS, ctx.MatchContext.BallZone);

            // Shifted form for Aggression/WorkRate; Stamina: raw * exponent (spec §3.2.7 says "shifted")
            float aggressionFactor = Mathf.Pow(0.5f + ctx.A_Aggression * 0.5f, UtilityWeights.PRESS_AGGRESSION_EXP);
            float workRateFactor   = Mathf.Pow(0.5f + ctx.A_WorkRate   * 0.5f, UtilityWeights.PRESS_WORKRATE_EXP);
            float staminaFactor    = Mathf.Pow(0.5f + ctx.A_Stamina    * 0.5f, UtilityWeights.PRESS_STAMINA_EXP);
            float am = aggressionFactor * workRateFactor * staminaFactor;

            float contextM = opt.ProximityScore;

            // Tactical pressing modifier (§3.2.7 / §3.4.3)
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.PRESS, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

            float baseU = UtilityWeights.U_BASE_PRESS * zoneM;
            return baseU * am * contextM * tactM;
        }

        // ── §3.2.8 INTERCEPT ───────────────────────────────────────────────────

        private static float ScoreIntercept(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.INTERCEPT, ctx.MatchContext.BallZone);

            // Anticipation: raw form (§3.2.8 — no 0.5 shift for Anticipation)
            // Pace: shifted form
            float anticipationFactor = Mathf.Pow(ctx.A_Anticipation,              UtilityWeights.INTERCEPT_ANTICIPATION_EXP);
            float paceFactor         = Mathf.Pow(0.5f + ctx.A_Pace * 0.5f,        UtilityWeights.INTERCEPT_PACE_EXP);
            float am = anticipationFactor * paceFactor;

            float contextM = opt.InterceptFeasibilityScore;

            float p    = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Anticipation) * UtilityWeights.INTERCEPT_PRESSURE_COEFF;

            float baseU = UtilityWeights.U_BASE_INTERCEPT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.INTERCEPT, in ctx.TacticalContext, ctx.PossessedByTeam, 0.0f);

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

                default: // SHOOT handled separately; MOVE is always 1.0
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
#endregion
