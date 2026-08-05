// File:     src/decision-tree/UtilityScorer.cs
// Created:  2026-05-29
// Modified: 2026-06-28 (#21 T2 mentality risk multiplier)
// Modified: 2026-07-07 (cheap-item addition: rest-defense risk dampener)
// Modified: 2026-07-28 (ERR-008-017 — ScoreShoot gains the DistanceQuality_SHOOT term (shot-volume design KD-V2/KD-V3))
// Modified: 2026-08-04 (ERR-008-018 — ScoreDribble gains the DirectionQuality_DRIBBLE term (close-chance-creation design KD-CC2))
// Modified: 2026-08-05 (ERR-008-019 — ScoreShoot midfield long-shot gate: hard threshold → linear ramp (judgment-proxy doctrine P1/P5))
// Modified: 2026-08-05 (ERR-008-019 owner revision — comment updated for the full-range half-width; formula unchanged)
// Author:   —
// Spec:     Decision Tree #8 §3.2, §3.4, new §3.2/§7.7, Tactical Instructions #21 §3.2, Code Standards #20
// Purpose:  Step 4 of the 6-step pipeline. Applies the utility scoring model to each
//           ActionOption, populating BaseUtility. Pure function: no side effects.

using UnityEngine;

using TacticalDirector.PerceptionSystem;
using TacticalDirector.TacticalInstructions;

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
                case ActionType.PASS: u = ScorePass(ref opt, in ctx); break;
                case ActionType.SHOOT: u = ScoreShoot(ref opt, in ctx); break;
                case ActionType.DRIBBLE: u = ScoreDribble(ref opt, in ctx); break;
                case ActionType.HOLD: u = ScoreHold(ref opt, in ctx); break;
                case ActionType.MOVE_TO_POSITION: u = ScoreMove(ref opt, in ctx); break;
                case ActionType.PRESS: u = ScorePress(ref opt, in ctx); break;
                case ActionType.INTERCEPT: u = ScoreIntercept(ref opt, in ctx); break;
                case ActionType.SAVE: u = ScoreSave(ref opt, in ctx); break;
                default: u = UtilityWeights.UTILITY_FLOOR; break;
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
            // ERR-008-013: SAVE is exempt from the per-agent tactic product. A keeper's save is not
            // shaped by RiskyPasses / attacking tempo, and — load-bearing — RoleWeightModifiers /
            // TempoActionBias are 7-wide [role/tempo][action] tables indexed by the action ordinal, so
            // a=SAVE(7) would read out of bounds (IndexOutOfRangeException). Skipping is both correct
            // and safe. SAVE reaches this scorer only as the sole off-ball option (OptionGenerator).
            if (opt.Type != ActionType.SAVE)
                u *= TacticTranslation.PlayerTacticActionMultiplier(
                    ctx.TacticalContext.PlayerTactic, ctx.TacticalContext.Tempo, opt.Type);

            // Cheap-item addition (new §3.2/§7.7, redesigned after user review): Positioning AI #12's
            // rest-defense coverage check only matters if THIS agent — the ball carrier, since PASS/
            // SHOOT/DRIBBLE only score when AgentHasBall — actually has the tactical awareness to
            // perceive the thin cover. An unaware player (low Decisions/Anticipation) takes the risky
            // action anyway; the insufficient cover is then a genuine tactical flaw in team setup or
            // instructions for the manager to address, not something the AI silently corrects for. A
            // fully aware player (awareness = 1.0) gets the full dampener; an oblivious one (0.0) gets
            // none. Sufficient coverage (the Stage0Default identity, true) applies no dampening either
            // way — byte-identical to pre-addition.
            if (!ctx.TacticalContext.RestDefenseSufficient
                && (opt.Type == ActionType.PASS || opt.Type == ActionType.SHOOT || opt.Type == ActionType.DRIBBLE))
            {
                float awareness = (ctx.A_Decisions + ctx.A_Anticipation) * 0.5f;
                u *= Mathf.Lerp(1.0f, TacticalWeights.RestDefenseRiskMult, awareness);
            }

            // Dismarking #23 §3.4 (FM-DM-03, anchored at #8 §3.2.2.1): the marked-pass-target
            // penalty, applied next to the Mentality / rest-defense multipliers BEFORE the clamp.
            // Both terms come from the PASSER's own FilteredView (FR-DM-011 — the perceived
            // teammate position is the option's TargetPosition, generated from VisibleTeammates;
            // the opponent scan below reads the same view's VisibleOpponents) and the penalty is
            // scaled by the PASSER's awareness — an unaware passer plays the marked pass anyway
            // (FR-DM-010, mirroring the rest-defense design above). Off is the exact ×1.0 identity
            // (FR-DM-012).
            if (opt.Type == ActionType.PASS
                && ctx.TacticalContext.DismarkIntensity != DismarkIntensity.Off)
            {
                u *= MarkedPassTargetMultiplier(ref opt, in ctx);
            }

            float clamped = Mathf.Clamp(u, UtilityWeights.UTILITY_FLOOR, UtilityWeights.UTILITY_CEILING);

            // AR-3 L: Mathf.Clamp passes NaN through (NaN comparisons are false), so a
            // non-finite utility — from a corrupt attribute or position upstream — would
            // otherwise survive into selection, where ActionSelector picks index 0 when
            // every comparison is false and could dispatch the NaN option. Fail closed to
            // the floor (project NaN-gate pattern: AM AR-10 / CS AR-7 / FT AR-8).
            return float.IsNaN(clamped) ? UtilityWeights.UTILITY_FLOOR : clamped;
        }

        // ── Dismarking #23 §3.4 (FM-DM-03) helper ─────────────────────────────

        /// <summary>
        /// Marked-pass-target multiplier ∈ [TargetMarkedUtilityMult, 1.0]:
        /// Lerp(1.0, TargetMarkedUtilityMult, targetProximity01 × awareness01), where
        /// targetProximity01 = Clamp01(1 − d_t / MarkedPassRadiusM) with d_t the minimum distance
        /// from the option's target position to any passer-perceived opponent (no dwell term —
        /// the passer judges a snapshot, #23 §3.4), and awareness01 = the passer's mean of
        /// Decisions/Anticipation. No visible opponents (or a non-finite distance — F1 NaN gate)
        /// resolves to the ×1.0 identity. Worked example (#23 §3.4): opponent 0.9 m from the
        /// teammate ⇒ prox 0.7; awareness 0.8 ⇒ Lerp(1.0, 0.7, 0.56) = 0.832.
        /// </summary>
        private static float MarkedPassTargetMultiplier(ref ActionOption opt, in DecisionContext ctx)
        {
            FilteredView snap = ctx.Snapshot;
            float dMin = float.PositiveInfinity;
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                float d = Vector2.Distance(opt.TargetPosition, snap.VisibleOpponents[i].PerceivedPosition);
                if (d < dMin) dMin = d; // NaN compares false — non-finite entries never win (F1)
            }
            if (float.IsPositiveInfinity(dMin)) return 1.0f; // no perceived opponent ⇒ free target

            float targetProx01 = Mathf.Clamp01(1.0f - dMin / TacticalWeights.MarkedPassRadiusM);
            // NaN-gate: !(x > 0) form — a NaN proximity resolves to the identity, never NaN utility.
            if (!(targetProx01 > 0.0f)) return 1.0f;

            float awareness01 = Mathf.Clamp01((ctx.A_Decisions + ctx.A_Anticipation) * 0.5f);
            return Mathf.Lerp(1.0f, TacticalWeights.TargetMarkedUtilityMult, targetProx01 * awareness01);
        }

        // ── §3.2.2 PASS ────────────────────────────────────────────────────────

        private static float ScorePass(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(opt.Type, ctx.BallZone);

            // Shifted attribute form: (0.5 + A × 0.5)^exp
            float visionFactor = Mathf.Pow(0.5f + ctx.A_Vision * 0.5f, UtilityWeights.PASS_VISION_EXP);
            float techniqueFactor = Mathf.Pow(0.5f + ctx.A_Passing * 0.5f, UtilityWeights.PASS_TECHNIQUE_EXP);
            float am = visionFactor * techniqueFactor;

            float contextM = opt.AdjustedPassLaneScore;

            float p = ctx.PressureScalar;
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
                // §3.2.3.1 midfield long-shot ramp (ERR-008-019 / judgment-proxy doctrine P1+P5).
                // The former hard gate — SHOOT_ZONE_MID_LONG strictly above LONG_SHOT_THRESHOLD,
                // _SHORT at or below it — stepped the zone modifier 11× (0.05 → 0.55) across one
                // raw LongShots point. Now a linear ramp in the SHIFTED form (0.5 + A × 0.5 —
                // AR-2 M-4's correction still applies), centred on the old threshold with
                // half-width LONG_SHOT_RAMP_HALF_WIDTH. At the owner-directed full-range value
                // (0.25) the ramp spans the whole attribute: raw 1 is exactly _SHORT, raw 20
                // exactly _LONG, and every raw point in between moves the modifier ≈ 0.026 —
                // no plateaus. The exact SHORT/LONG midpoint sits at the old cliff position,
                // so the uniform-population mean reproduces the old step's (P5 pivot).
                float shifted = 0.5f + ctx.A_LongShots * 0.5f;
                float t = Mathf.InverseLerp(
                    UtilityWeights.LONG_SHOT_THRESHOLD - UtilityWeights.LONG_SHOT_RAMP_HALF_WIDTH,
                    UtilityWeights.LONG_SHOT_THRESHOLD + UtilityWeights.LONG_SHOT_RAMP_HALF_WIDTH,
                    shifted);
                zoneM = Mathf.Lerp(
                    UtilityWeights.SHOOT_ZONE_MID_SHORT, UtilityWeights.SHOOT_ZONE_MID_LONG, t);
            }
            else
            {
                zoneM = UtilityWeights.SHOOT_ZONE_DEF;
            }

            float finishFactor = Mathf.Pow(0.5f + ctx.A_Finishing * 0.5f, UtilityWeights.SHOOT_FINISHING_EXP);
            float composureFactor = Mathf.Pow(0.5f + ctx.A_Composure * 0.5f, UtilityWeights.SHOOT_COMPOSURE_EXP);
            float am = finishFactor * composureFactor;

            // GoalOpeningScore is floored at GOAL_OPENING_MIN inside
            // ComputeGoalOpeningScore (§3.2.3.2 step 5); defensive re-floor retained
            // for direct-injection test paths.
            float goalOpeningScore = Mathf.Max(opt.GoalOpeningScore, UtilityWeights.GOAL_OPENING_MIN);

            // AR-2 M-3: §3.2.3.1 RiskPenalty_SHOOT = (1 − GoalOpeningScore) × P × coeff
            // (a blocked shot is the risk driver). Previous form used (1 − A_Finishing).
            float p = ctx.PressureScalar;
            float risk = (1.0f - goalOpeningScore) * p * UtilityWeights.SHOOT_RISK_COEFF;

            // §3.2.3.1 DistanceQuality_SHOOT (ERR-008-017 / shot-volume design KD-V2): 1.0 inside
            // the sweet range, hyperbolic decay beyond it. GoalOpeningScore is scale-free (the
            // goal arc and a near-goal blocker's occlusion both shrink ~1/d), so without this
            // term a 34 m shot scored identically to a 10 m one and measured shots clustered at
            // the range-gate boundary (means 30–34 m vs football's ~17). Direct-injection test
            // options that never set DistanceToGoal read 0 ⇒ distQ = 1.0 (KD-V3 contract) —
            // every pre-existing unit expectation stands unmodified.
            float beyond = opt.DistanceToGoal - UtilityWeights.SHOOT_SWEET_RANGE_M;
            float distQ = beyond <= 0.0f
                ? 1.0f
                : UtilityWeights.SHOOT_DIST_FALLOFF_M / (UtilityWeights.SHOOT_DIST_FALLOFF_M + beyond);

            float baseU = UtilityWeights.U_BASE_SHOOT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.SHOOT, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * goalOpeningScore * distQ * tactM * (1.0f - risk);
        }

        // ── §3.2.4 DRIBBLE ─────────────────────────────────────────────────────

        private static float ScoreDribble(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.DRIBBLE, ctx.BallZone);

            // Raw attribute form (no 0.5 shift per §3.2.4 design note)
            float dribblingFactor = Mathf.Pow(ctx.A_Dribbling, UtilityWeights.DRIBBLE_DRIBBLING_EXP);
            float agilityFactor = Mathf.Pow(ctx.A_Agility, UtilityWeights.DRIBBLE_AGILITY_EXP);
            float am = dribblingFactor * agilityFactor;

            float contextM = opt.SpaceScore;

            float p = ctx.PressureScalar;
            float risk = p * (1.0f - ctx.A_Dribbling) * UtilityWeights.DRIBBLE_RISK_COEFF;

            // §3.2.4.1 DirectionQuality_DRIBBLE (ERR-008-018 / close-chance-creation design KD-CC2):
            // §3.1.5.2 picks best_direction by free space alone and defers the directional-to-goal
            // modifier to this stage; the stage never had one. SpaceScore is direction-blind by
            // construction (it measures only how clear a sector is), so without this factor a dribble
            // back toward the halfway line scored exactly as well as the same dribble at goal — and in
            // the final third, where the space IS behind the carrier, that is the direction the
            // argmax picks. This suppresses the retreating dribble rather than redirecting it (the
            // generator emits one option), which is precisely what §3.1.5.2 delegates here: the
            // carrier is pushed onto its PASS / SHOOT / HOLD alternatives instead.
            float dirQ = ComputeDribbleDirectionQuality(ref opt, in ctx);

            float baseU = UtilityWeights.U_BASE_DRIBBLE * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.DRIBBLE, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * contextM * dirQ * tactM * (1.0f - risk);
        }

        /// <summary>
        /// DirectionQuality_DRIBBLE ∈ [DRIBBLE_GOAL_DIR_MIN_MODIFIER, 1.0]: the same linear-in-cosine
        /// shape §3.1.3.5 already uses for the PASS goal-direction modifier, applied to the chosen
        /// dribble direction. Straight at the opponent goal ⇒ 1.0; straight away ⇒ the floor.
        ///
        /// <para>A degenerate <see cref="ActionOption.BestDribbleDirection"/> — the zero vector, which
        /// is what a direct-injection test option that never sets it carries — resolves to the exact
        /// ×1.0 identity rather than to the mid-cosine value. That is the ERR-008-017 / KD-V3 contract
        /// restated: an unset field must not silently reprice an option, so every pre-existing unit
        /// expectation stands unmodified. Live options always carry a unit-length direction from
        /// §3.1.5.2's sector scan.</para>
        /// </summary>
        private static float ComputeDribbleDirectionQuality(ref ActionOption opt, in DecisionContext ctx)
        {
            Vector2 dir = opt.BestDribbleDirection;
            Vector2 toGoal = ctx.OpponentGoalCentre - ctx.AgentPosition;

            float dirLen = dir.magnitude;
            float goalLen = toGoal.magnitude;
            if (!(dirLen > 1e-4f) || !(goalLen > 1e-4f)) return 1.0f;   // NaN-safe !(x > 0) form

            float cosine = Vector2.Dot(dir / dirLen, toGoal / goalLen);
            if (float.IsNaN(cosine)) return 1.0f;

            return UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER
                 + ((cosine + 1.0f) * 0.5f) * (1.0f - UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER);
        }

        // ── §3.2.5 HOLD ────────────────────────────────────────────────────────

        private static float ScoreHold(ref ActionOption opt, in DecisionContext ctx)
        {
            float zoneM = GetZoneModifier(ActionType.HOLD, ctx.BallZone);

            float composureFactor = Mathf.Pow(0.5f + ctx.A_Composure * 0.5f, UtilityWeights.HOLD_COMPOSURE_EXP);

            float p = ctx.PressureScalar;
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
            float workRateFactor = Mathf.Pow(0.5f + ctx.A_WorkRate * 0.5f, UtilityWeights.MOVE_WORKRATE_EXP);
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
            float workRateFactor = Mathf.Pow(0.5f + ctx.A_WorkRate * 0.5f, UtilityWeights.PRESS_WORKRATE_EXP);
            float staminaFactor = Mathf.Pow(0.5f + ctx.A_Stamina * 0.5f, UtilityWeights.PRESS_STAMINA_EXP);
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
            float anticipationFactor = Mathf.Pow(ctx.A_Anticipation, UtilityWeights.INTERCEPT_ANTICIPATION_EXP);
            float paceFactor = Mathf.Pow(0.5f + ctx.A_Pace * 0.5f, UtilityWeights.INTERCEPT_PACE_EXP);
            float am = anticipationFactor * paceFactor;

            float contextM = opt.InterceptFeasibilityScore;

            // AR-2 M-6: §3.2.8.1 pressure term is (1 − P × INTERCEPT_PRESSURE_COEFF),
            // independent of Anticipation. The previous (1 − A_Anticipation) factor is
            // not in the spec formula.
            float p = ctx.PressureScalar;
            float risk = p * UtilityWeights.INTERCEPT_PRESSURE_COEFF;

            float baseU = UtilityWeights.U_BASE_INTERCEPT * zoneM;
            float tactM = TacticalModifierResolver.Resolve(
                ActionType.INTERCEPT, in ctx.TacticalContext, ctx.OpponentHasBall, 0.0f);

            return baseU * am * contextM * tactM * (1.0f - risk);
        }

        // ── §3.2.10 SAVE (ERR-008-013) ────────────────────────────────────────

        /// <summary>
        /// Scores the goalkeeper SAVE option. SAVE reaches this method only as the SOLE off-ball
        /// option (OptionGenerator gates it on <see cref="TacticalContext.SaveAvailable"/>), so the
        /// value is not load-bearing for selection — it is always chosen. Returns the flat
        /// <see cref="UtilityWeights.U_BASE_SAVE"/> ceiling (no attribute / risk / tactical modulation
        /// at Stage 0; attribute-modulated commit is a named future refinement). The ComputeUtility
        /// caller exempts SAVE from the per-agent tactic product (the 7-wide-table OOB guard).
        /// </summary>
        private static float ScoreSave(ref ActionOption opt, in DecisionContext ctx)
        {
            return UtilityWeights.U_BASE_SAVE;
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
// | 1.8     | 2026-07-07 | —      | Cheap-item addition: PASS/SHOOT/DRIBBLE × RestDefenseRiskMult when            |
// |         |            |        |   TacticalContext.RestDefenseSufficient is false (new §3.2/§7.7). Sufficient |
// |         |            |        |   (the Stage0Default identity) applies no dampening.                          |
// | 1.9     | 2026-07-07 | —      | Redesign after user review: the rest-defense dampener is no longer a flat     |
// |         |            |        |   multiplier — it is Lerp'd by the ball carrier's own (Decisions +            |
// |         |            |        |   Anticipation) / 2 awareness. An unaware carrier gets no dampening (the      |
// |         |            |        |   thin cover is a real tactical flaw exposed, not silently corrected); a      |
// |         |            |        |   fully aware one gets the full RestDefenseRiskMult. Also REVERTS the half-   |
// |         |            |        |   spaces PASS bonus (v1.9-as-first-written) — per user review, half-spaces    |
// |         |            |        |   are an exploitable gap requiring tactical/player instructions, not a flat   |
// |         |            |        |   passing bonus; ScorePass no longer reads AgentLane.                        |
// | 1.10    | 2026-07-11 | —      | Dismarking #23 §3.4 (FM-DM-03 / #8 §3.2.2.1): PASS options × the marked-      |
// |         |            |        |   pass-target multiplier (target proximity to passer-perceived opponents ×   |
// |         |            |        |   passer awareness), before the clamp. Off dial ⇒ exact ×1.0 identity        |
// |         |            |        |   (FR-DM-012) — a default match is byte-identical.                           |
// | 1.11    | 2026-07-23 | —      | ERR-008-013: + ScoreSave (returns U_BASE_SAVE; SAVE reaches the scorer only  |
// |         |            |        |   as the sole off-ball option, so not load-bearing for selection); SAVE is   |
// |         |            |        |   EXEMPTED from PlayerTacticActionMultiplier in ComputeUtility — its 7-wide  |
// |         |            |        |   #21 tables are indexed by the action ordinal, so a=SAVE(7) would read OOB. |
// | 1.12    | 2026-07-28 | —      | ERR-008-017 (shot-volume design KD-V2/KD-V3): ScoreShoot gains the           |
// |         |            |        |   DistanceQuality_SHOOT factor — 1.0 inside SHOOT_SWEET_RANGE_M, hyperbolic  |
// |         |            |        |   decay beyond (U_SHOOT previously had no distance term; measured shots      |
// |         |            |        |   clustered at the range-gate boundary, means 30–34 m vs football's ~17).    |
// | 1.13    | 2026-08-04 | —      | ERR-008-018 (close-chance-creation design KD-CC2/KD-CC4): ScoreDribble      |
// |         |            |        |   gains the DirectionQuality_DRIBBLE factor — the §3.1.3.5 PASS shape,       |
// |         |            |        |   linear in the cosine between the chosen dribble direction and the         |
// |         |            |        |   direction to the opponent goal. §3.1.5.2 had delegated this modifier to   |
// |         |            |        |   "the scoring stage (§3.2.2)" — the PASS section — so DRIBBLE's own        |
// |         |            |        |   formula never received it and a dribble toward halfway scored exactly     |
// |         |            |        |   as well as the same dribble at goal (measured: 40% of final-third         |
// |         |            |        |   carrier decisions, mean cosine to goal −0.30 over six full matches). A    |
// |         |            |        |   zero BestDribbleDirection resolves to the exact ×1.0 identity, so every   |
// |         |            |        |   pre-existing direct-injection expectation is unchanged (KD-V3 restated).  |
// | 1.14    | 2026-08-05 | —      | ERR-008-019 (judgment-proxy doctrine P1/P5): ScoreShoot's midfield zone     |
// |         |            |        |   modifier is a linear ramp in shifted LongShots — SHORT at/below           |
// |         |            |        |   THRESHOLD − HALF_WIDTH, LONG at/above THRESHOLD + HALF_WIDTH — replacing  |
// |         |            |        |   the hard LONG_SHOT_THRESHOLD step that jumped the modifier 11× across     |
// |         |            |        |   one raw attribute point. Ramp centred on the old cliff: endpoints and     |
// |         |            |        |   the population-integrated modifier reproduce the old behaviour (P5).      |
// | 1.15    | 2026-08-05 | —      | ERR-008-019 owner revision (comment only — the formula is unchanged): the   |
// |         |            |        |   half-width [GT] moved to the full-range 0.25, so the branch comment now   |
// |         |            |        |   describes the plateau-free full-attribute ramp rather than the 8.6–12.4   |
// |         |            |        |   band.                                                                     |
#endregion
