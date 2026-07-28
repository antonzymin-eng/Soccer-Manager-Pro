// File:     src/decision-tree/OptionGenerator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Modified: 2026-07-26 (ERR-008-014 — loose-ball collect emitted as the SOLE off-ball option for the host-designated collector)
// Modified: 2026-07-28 (ERR-008-016 — PowerIntent floor-plus-modulation (shot-speed design KD-1))
// Author:   —
// Spec:     Decision Tree #8 §3.1, Code Standards #20
// Purpose:  Step 3 of the 6-step pipeline. Generates all eligible ActionOption
//           candidates from DecisionContext. Pure function: no side effects, zero heap
//           allocation (writes into caller-provided fixed-size array). §3.1.0.

using System;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 3: generates all eligible ActionOption candidates for one agent at one heartbeat.
    /// Pure, deterministic, side-effect-free. Writes into a caller-provided fixed array.
    /// Decision Tree #8 §3.1.
    /// </summary>
    internal static class OptionGenerator
    {
        /// <summary>
        /// Generates all eligible options and writes them into optionBuffer[0..count-1].
        /// Returns the number of options written. Maximum is DecisionTreeConstants.MaxOptions.
        /// §3.1.0, §3.1.2.
        /// </summary>
        internal static int GenerateOptions(
            in DecisionContext ctx,
            ActionOption[] optionBuffer)
        {
            int count = 0;

            if (ctx.AgentHasBall)
                count = GeneratePossessionBranch(in ctx, optionBuffer, count);
            else
                count = GenerateOffBallBranch(in ctx, optionBuffer, count);

            return count;
        }

        // ── Possession Branch (§3.1.2) ─────────────────────────────────────────

        private static int GeneratePossessionBranch(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            count = GeneratePassCandidates(in ctx, buf, count);  // §3.1.3
            count = GenerateShootCandidate(in ctx, buf, count);  // §3.1.4
            count = GenerateDribbleCandidate(in ctx, buf, count); // §3.1.5
            count = GenerateHoldCandidate(in ctx, buf, count);    // §3.1.6
            return count;
        }

        // ── Off-Ball Branch (§3.1.2) ───────────────────────────────────────────

        private static int GenerateOffBallBranch(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            // #11/#10 (ERR-008-013): a threatened keeper commits to the save — SAVE is the SOLE
            // off-ball option so it is selected regardless of composure noise / mentality / role
            // tiebreak (a must-happen, geometry-gated action must not depend on out-scoring a
            // tiebreak-disadvantaged competitor; AR-4). SaveAvailable is true only for the flag-on
            // keeper (MatchEngine.RunMechanicsAI under EnableGkHeading), so every other agent /
            // flag-off path is byte-identical to pre-integration.
            if (ctx.TacticalContext.SaveAvailable)
                return GenerateSaveCandidate(in ctx, buf, count); // §3.1.10 (new)

            // ERR-008-014: this agent is the designated collector of a loose ball lying at rest — emit
            // the collect as the SOLE option, exactly as SAVE is emitted above and for the same reason
            // (AR-4's finding, restated): an action that MUST happen must not depend on out-scoring a
            // competitor under composure noise. Measured, it does not: the collect scores ~0.35 against
            // MOVE_TO_POSITION's ~0.21 on neutral attributes, a gap of 0.14 that sits INSIDE the ±0.15
            // noise band, so the designated collector flip-flopped between chasing the ball and
            // returning to its formation slot and never covered the last few metres. Play stopped with
            // one agent dithering next to a ball nobody else was allowed to fetch.
            if (ctx.TacticalContext.LooseBallCollector)
                return GenerateLooseBallCollectCandidate(in ctx, buf, count); // §3.1.9 (loose-ball case)

            count = GenerateMoveCandidate(in ctx, buf, count);      // §3.1.7
            count = GeneratePressCandidate(in ctx, buf, count);     // §3.1.8
            count = GenerateInterceptCandidate(in ctx, buf, count); // §3.1.9
            return count;
        }

        // ── §3.1.10 SAVE (ERR-008-013) ──────────────────────────────────────────

        /// <summary>
        /// Emits the single SAVE candidate for a threatened goalkeeper (gated by
        /// <see cref="TacticalContext.SaveAvailable"/>, set only under the opt-in GK/Heading flag).
        /// TargetPosition carries the perceived ball position for observability only — the save
        /// intent parameters are Stage-0 constants applied by the match-engine dispatch sink
        /// (the #11 SaveIntent doc's anticipated "DT commits the save" path). §3.1.10.
        /// </summary>
        private static int GenerateSaveCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            buf[count++] = new ActionOption
            {
                Type           = ActionType.SAVE,
                TargetAgentId  = -1,
                TargetPosition = ctx.Snapshot.BallPerceivedPosition
            };

            return count;
        }

        // ── §3.1.3 PASS ────────────────────────────────────────────────────────

        private static int GeneratePassCandidates(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            FilteredView snap = ctx.Snapshot;

            // Gate (§3.1.3.1)
            if (!ctx.AgentHasBall) return count;
            if (snap.VisibleTeammatesCount == 0) return count;
            if (ctx.MatchContext.Phase != MatchPhase.OPEN_PLAY) return count;

            // Decisions attribute candidate cap (§3.1.3.6)
            int decisionsCap = Mathf.FloorToInt(2.0f + ctx.A_Decisions * 8.0f);
            int totalTeammates = snap.VisibleTeammatesCount;
            int maxCandidates = Mathf.Min(totalTeammates, decisionsCap);

            Vector2 goalDir = (ctx.OpponentGoalCentre - ctx.AgentPosition).normalized;
            float urgency = Mathf.Clamp01(ctx.PressureScalar * TacticalWeights.UrgencyPressureScale);

            if (maxCandidates >= totalTeammates)
            {
                // Cap non-binding: snapshot order (§3.1.3.2 — order has no scoring
                // significance when every visible teammate is evaluated).
                for (int i = 0; i < totalTeammates && count < buf.Length; i++)
                    count = TryAddPassCandidate(in ctx, snap.VisibleTeammates[i],
                        goalDir, urgency, buf, count);
            }
            else
            {
                // §3.1.3.6: when the cap binds, teammates are evaluated in PROXIMITY
                // order (closest first) — a cognitive scope limit, not a scoring
                // decision. AR-2 M-9: the previous form iterated in snapshot order,
                // so low-Decisions agents dropped teammates by array position rather
                // than distance. Selection scan is O(cap × N) with N ≤ 21; stackalloc
                // marks keep the hot path zero-heap-allocation (INV-10).
                Span<bool> taken = stackalloc bool[totalTeammates];
                for (int k = 0; k < maxCandidates && count < buf.Length; k++)
                {
                    int   bestIdx = -1;
                    float bestSqr = float.MaxValue;
                    for (int i = 0; i < totalTeammates; i++)
                    {
                        if (taken[i]) continue;
                        float sqr = (snap.VisibleTeammates[i].PerceivedPosition
                                     - ctx.AgentPosition).sqrMagnitude;
                        if (sqr < bestSqr) { bestSqr = sqr; bestIdx = i; }
                    }
                    if (bestIdx < 0) break;
                    taken[bestIdx] = true;
                    count = TryAddPassCandidate(in ctx, snap.VisibleTeammates[bestIdx],
                        goalDir, urgency, buf, count);
                }
            }

            return count;
        }

        // One teammate → zero or one PASS candidate (§3.1.3.2–§3.1.3.5).
        private static int TryAddPassCandidate(
            in DecisionContext ctx,
            PerceivedAgent tm,
            Vector2 goalDir,
            float urgency,
            ActionOption[] buf,
            int count)
        {
            // Pass lane viability (§3.1.3.3)
            int interceptorCount = CountInterceptors(in ctx, tm.PerceivedPosition);
            float passLaneScore = Mathf.Clamp01(1.0f - interceptorCount / UtilityWeights.PASS_LANE_DIVISOR);

            // Goal-direction modifier (§3.1.3.5)
            Vector2 passDir = (tm.PerceivedPosition - ctx.AgentPosition).normalized;
            float cosine = Vector2.Dot(passDir, goalDir);
            float goalDirMod = UtilityWeights.GOAL_DIR_MIN_MODIFIER
                + ((cosine + 1.0f) / 2.0f) * (1.0f - UtilityWeights.GOAL_DIR_MIN_MODIFIER);
            float adjustedScore = passLaneScore * goalDirMod;

            if (adjustedScore < UtilityWeights.MIN_PASS_LANE_SCORE) return count;

            float dist = Vector2.Distance(ctx.AgentPosition, tm.PerceivedPosition);
            PassType passType = DerivePassType(dist, passDir, ctx.AgentFacingDirection,
                tm.PerceivedVelocity, goalDir);
            CrossSubType crossSub = CrossSubType.Flat; // Stage 0: always Flat; Whipped/High derived at Stage 1

            buf[count++] = new ActionOption
            {
                Type                  = ActionType.PASS,
                TargetAgentId         = tm.AgentId,
                TargetPosition        = tm.PerceivedPosition,
                PassLaneScore         = passLaneScore,
                AdjustedPassLaneScore = adjustedScore,
                GoalDirectionCosine   = cosine,
                DerivedPassType       = passType,
                DerivedCrossSubType   = crossSub,
                IntendedDistance      = dist,
                Urgency               = urgency,
                IsWeakFoot            = false  // ERR-007-TRACKED: WeakFootRating absent; conservative default
            };

            return count;
        }

        private static int CountInterceptors(in DecisionContext ctx, Vector2 targetPos)
        {
            Vector2 laneVec = targetPos - ctx.AgentPosition;
            float laneLen2  = laneVec.sqrMagnitude;
            int interceptors = 0;

            FilteredView snap = ctx.Snapshot;
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                Vector2 oPos = snap.VisibleOpponents[i].PerceivedPosition;
                float tProj  = laneLen2 > 0.0001f
                    ? Vector2.Dot(oPos - ctx.AgentPosition, laneVec) / laneLen2
                    : 0.0f;
                float tClamped  = Mathf.Clamp01(tProj);
                Vector2 closest = ctx.AgentPosition + tClamped * laneVec;
                float perpDist  = Vector2.Distance(oPos, closest);

                if (perpDist < UtilityWeights.PASS_LANE_WIDTH_HALF
                    && tClamped > UtilityWeights.PASS_LANE_ENDPOINT_MARGIN
                    && tClamped < 1.0f - UtilityWeights.PASS_LANE_ENDPOINT_MARGIN)
                {
                    interceptors++;
                }
            }

            return interceptors;
        }

        // SPEC-DEVIATION NOTE (Stage 0, AR-2 L): §3.1.3.4 additionally gates CROSS on
        // the agent being in a wide channel ("AgentPosition.x in WIDE_ZONE" — the spec
        // text says x, but wide channels are touchline-relative, i.e. the Y axis;
        // ERR-008-006). WIDE_ZONE is not declared in any §3 constant table, so the
        // Stage 0 implementation classifies CROSS from range + facing angle alone and
        // the Crossing attribute is not yet consumed here (DtAgentAttributes.Crossing
        // doc-noted declared-but-unconsumed). The dead aCrossing/agentPos parameters
        // this note replaces were the vestige of that unimplemented gate.
        private static PassType DerivePassType(
            float dist,
            Vector2 passDir,
            Vector2 agentFacing,
            Vector2 tmVelocity,
            Vector2 goalDir)
        {
            if (dist <= UtilityWeights.SHORT_PASS_MAX_DISTANCE)
                return PassType.Ground;

            if (dist <= UtilityWeights.MEDIUM_PASS_MAX_DISTANCE)
            {
                float velTowardGoal = Vector2.Dot(tmVelocity, goalDir);
                return velTowardGoal > UtilityWeights.THROUGH_BALL_VEL_THRESHOLD
                    ? PassType.ThroughBall
                    : PassType.Ground;
            }

            // Long range: check cross angle
            float angleFromFwd = Vector2.Angle(agentFacing, passDir);
            if (angleFromFwd > UtilityWeights.CROSS_ANGLE_THRESHOLD)
                return PassType.Cross;

            return PassType.Lofted;
        }

        // ── §3.1.4 SHOOT ────────────────────────────────────────────────────────

        private static int GenerateShootCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            // Gate (§3.1.4.1)
            if (!ctx.AgentHasBall) return count;
            if (!ctx.Snapshot.BallVisible) return count;
            if (ctx.MatchContext.Phase != MatchPhase.OPEN_PLAY) return count;

            float distToGoal = Vector2.Distance(ctx.AgentPosition, ctx.OpponentGoalCentre);

            // Range gate (§3.1.4.2)
            float shootingRange = UtilityWeights.BASE_SHOOT_RANGE + ctx.A_LongShots * UtilityWeights.LONGSHOT_RANGE_BONUS;
            if (distToGoal > shootingRange) return count;

            // Goal visibility (§3.1.4.3)
            float goalOpeningScore = ComputeGoalOpeningScore(in ctx, distToGoal);
            if (goalOpeningScore < UtilityWeights.MIN_GOAL_VISIBILITY) return count;

            // PowerIntent (§3.5.3, ERR-008-016): floor-plus-modulation. The former
            // clamp(goalOpening × A_Finishing, 0.1, 1.0) pinned nearly every shot at its own 0.1
            // clamp floor (a product of two [0,1] factors, one of which is ≈ 0.47 for a neutral
            // player), which composed with #6's velocity assembly into measured shot speeds of
            // 7–10 m/s against football's ~25. A deliberate shot is always struck hard; the
            // opening × finishing product now modulates the band ABOVE the floor, keeping the old
            // direction (better opening + better finisher ⇒ harder strike, up to 1.0). The clamp
            // is retained as the VR-02 range guarantee (the expression cannot leave [floor, 1] for
            // in-range inputs; NaN hygiene falls to the pre-dispatch FM-DT assertions as before).
            float powerIntent = Mathf.Clamp(
                UtilityWeights.POWER_INTENT_FLOOR
                + (1.0f - UtilityWeights.POWER_INTENT_FLOOR) * goalOpeningScore * ctx.A_Finishing,
                UtilityWeights.POWER_INTENT_FLOOR, 1.0f);

            // PlacementTarget: aim toward far post based on agent position relative to goal centre
            float agentGoalRelY = ctx.AgentPosition.y - ctx.OpponentGoalCentre.y;
            float u = agentGoalRelY >= 0.0f
                ? TacticalWeights.PlacementCornerOffset                        // aim low side
                : 1.0f - TacticalWeights.PlacementCornerOffset;                // aim high side
            float v = 0.5f; // mid-height default
            Vector2 placement = new Vector2(u, v);

            buf[count++] = new ActionOption
            {
                Type               = ActionType.SHOOT,
                TargetAgentId      = -1,
                TargetPosition     = ctx.OpponentGoalCentre,
                GoalOpeningScore   = goalOpeningScore,
                PowerIntent        = powerIntent,
                DerivedContactZone = ContactZone.Centre,
                SpinIntent         = 0.0f,
                PlacementTarget    = placement,
                DistanceToGoal     = distToGoal
            };

            return count;
        }

        private static float ComputeGoalOpeningScore(in DecisionContext ctx, float distToGoal)
        {
            // Tolerance for the §3.2.3.2 step 4 wedge-containment test (degrees).
            // Absorbs float error in the two-angle sum; well below any meaningful
            // occlusion width. Named local const per the project magic-number rule.
            const float ArcOverlapToleranceDeg = 0.01f;

            Vector2 agentPos  = ctx.AgentPosition;
            Vector2 goalPostL = ctx.OpponentGoalPostL;
            Vector2 goalPostR = ctx.OpponentGoalPostR;

            float totalArc   = AngularSpan(agentPos, goalPostL, goalPostR);
            if (totalArc <= 0.0f) return 0.0f;   // degenerate: agent on the goal line

            Vector2 dirL = (goalPostL - agentPos).normalized;
            Vector2 dirR = (goalPostR - agentPos).normalized;
            float goalLineX = goalPostL.x;

            float blockedArc = 0.0f;
            FilteredView snap = ctx.Snapshot;
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                Vector2 oPos = snap.VisibleOpponents[i].PerceivedPosition;

                // Only opponents between agent and goal (§3.1.4.3 IsInShotPath)
                if (!IsInShotPath(agentPos, ctx.OpponentGoalCentre, oPos,
                    distToGoal, UtilityWeights.GOAL_MIN_SHOT_DIST)) continue;

                float dist = Vector2.Distance(agentPos, oPos);
                if (dist < 0.001f) continue;

                // §3.2.3.2 step 4 overlap test: count an opponent only when its
                // angular centre lies within the goal arc [angleR, angleL]. The
                // opponent direction is inside the wedge iff its angles to the two
                // post directions sum to the wedge span. AR-2 M-7: previously every
                // opponent in the shot corridor contributed occlusion even when its
                // angular centre was outside the goal arc (over-blocking from wide).
                Vector2 dirO = (oPos - agentPos) / dist;
                if (Vector2.Angle(dirL, dirO) + Vector2.Angle(dirR, dirO)
                    > totalArc + ArcOverlapToleranceDeg) continue;

                // §3.2.3.2 step 3 GK heuristic: distance to the GOAL LINE (X axis) —
                // not to the goal centre, which misclassified a goal-line keeper
                // positioned wide of centre as a 0.5 m outfield blocker (AR-2 M-7).
                bool isGk = Mathf.Abs(oPos.x - goalLineX)
                            <= UtilityWeights.GK_PROXIMITY_TO_GOAL;
                float radius = isGk ? UtilityWeights.GK_BLOCKER_RADIUS_M
                                    : UtilityWeights.BLOCKER_RADIUS_M;

                float occlusionAngle = 2.0f * Mathf.Atan2(radius, dist) * Mathf.Rad2Deg;
                blockedArc += Mathf.Min(occlusionAngle, totalArc);   // step 3 per-opponent clamp
            }

            blockedArc = Mathf.Min(blockedArc, totalArc);            // step 4 sum clamp
            float score = (totalArc - blockedArc) / totalArc;

            // §3.2.3.2 step 5: floor at GOAL_OPENING_MIN ("a tiny gap always exists").
            // With the floor inside the derivation, the §3.1.4.1 gate (4) rejects only
            // the degenerate zero-arc case — as the spec intends.
            return Mathf.Clamp(score, UtilityWeights.GOAL_OPENING_MIN, 1.0f);
        }

        private static float AngularSpan(Vector2 origin, Vector2 a, Vector2 b)
        {
            Vector2 dirA = (a - origin).normalized;
            Vector2 dirB = (b - origin).normalized;
            return Mathf.Acos(Mathf.Clamp(Vector2.Dot(dirA, dirB), -1.0f, 1.0f)) * Mathf.Rad2Deg;
        }

        private static bool IsInShotPath(Vector2 agentPos, Vector2 goalCentre,
            Vector2 oppPos, float distToGoal, float minDist)
        {
            Vector2 shotDir = (goalCentre - agentPos).normalized;
            float proj = Vector2.Dot(oppPos - agentPos, shotDir);
            return proj > minDist && proj < distToGoal;
        }

        // ── §3.1.5 DRIBBLE ──────────────────────────────────────────────────────

        private static int GenerateDribbleCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            // Gate (§3.1.5.1)
            if (!ctx.AgentHasBall) return count;
            if (!ctx.Snapshot.BallVisible) return count;
            if (ctx.AgentState.CurrentState == AgentMovementState.GROUNDED) return count;
            if (ctx.MatchContext.Phase != MatchPhase.OPEN_PLAY) return count;

            // 8-sector space scan (§3.1.5.2)
            float bestSpace = 0.0f;
            Vector2 bestDir = ctx.AgentFacingDirection;

            for (int s = 0; s < 8; s++)
            {
                float angleDeg = s * 45.0f;
                Vector2 sectorDir = RotateVector(ctx.AgentFacingDirection, angleDeg);

                float spaceInSector = 1.0f;
                FilteredView snap = ctx.Snapshot;
                for (int i = 0; i < snap.VisibleOpponentsCount; i++)
                {
                    Vector2 oDir = snap.VisibleOpponents[i].PerceivedPosition - ctx.AgentPosition;
                    float oAngle = Vector2.Angle(sectorDir, oDir);
                    if (oAngle < UtilityWeights.DRIBBLE_SECTOR_HALF_ANGLE)
                    {
                        float oDist = oDir.magnitude;
                        float sectorSpace = Mathf.Clamp01(oDist / UtilityWeights.DRIBBLE_THREAT_RADIUS);
                        if (sectorSpace < spaceInSector) spaceInSector = sectorSpace;
                    }
                }

                if (spaceInSector > bestSpace)
                {
                    bestSpace = spaceInSector;
                    bestDir   = sectorDir;
                }
            }

            if (bestSpace < UtilityWeights.MIN_DRIBBLE_SPACE) return count;

            // INV-GEN-06 (§3.1.10): TargetPosition stays within pitch bounds — the
            // 5 m look-ahead from a touchline-adjacent carrier can otherwise leave
            // the pitch (AR-2 L). The direction intent is preserved; only the
            // indicator point is clamped.
            Vector2 dribbleTarget = PitchGeometry.ClampToPitch(
                ctx.AgentPosition + bestDir * UtilityWeights.DRIBBLE_LOOKAHEAD_M);

            buf[count++] = new ActionOption
            {
                Type               = ActionType.DRIBBLE,
                TargetAgentId      = -1,
                TargetPosition     = dribbleTarget,
                SpaceScore         = bestSpace,
                BestDribbleDirection = bestDir
            };

            return count;
        }

        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ── §3.1.6 HOLD ─────────────────────────────────────────────────────────

        private static int GenerateHoldCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;
            if (!ctx.AgentHasBall) return count;

            // HOLD is always generated in the possession branch (no gate conditions)
            buf[count++] = new ActionOption
            {
                Type          = ActionType.HOLD,
                TargetAgentId = -1,
                TargetPosition = ctx.AgentPosition
            };

            return count;
        }

        // ── §3.1.7 MOVE_TO_POSITION ─────────────────────────────────────────────

        private static int GenerateMoveCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;
            if (ctx.AgentHasBall) return count;

            // MOVE_TO_POSITION is always generated in the off-ball branch (no gate conditions)
            // §3.4.5 depth adjustment is team-signed along X (AR-2 M-2); INV-GEN-06
            // clamp keeps an extreme-depth slot on the pitch.
            Vector2 formationSlot = PitchGeometry.ClampToPitch(
                ctx.TacticalContext.GetAdjustedFormationSlot(ctx.AgentId, ctx.AgentTeamId));
            float distToSlot = Vector2.Distance(ctx.AgentPosition, formationSlot);

            buf[count++] = new ActionOption
            {
                Type           = ActionType.MOVE_TO_POSITION,
                TargetAgentId  = -1,
                TargetPosition = formationSlot,
                DistanceToSlot = distToSlot
            };

            return count;
        }

        // ── §3.1.8 PRESS ────────────────────────────────────────────────────────

        private static int GeneratePressCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            // Gate (§3.1.8.1)
            if (ctx.AgentHasBall) return count;
            if (ctx.Snapshot.VisibleOpponentsCount == 0) return count;
            if (!ctx.StaminaAvailable) return count;
            if (ctx.MatchContext.Phase != MatchPhase.OPEN_PLAY) return count;

            // Press target selection (§3.1.8.2)
            int pressTargetId   = -1;
            Vector2 pressTargetPos = Vector2.zero;
            float bestDist = float.MaxValue;

            FilteredView snap = ctx.Snapshot;
            int possessorId   = ctx.MatchContext.PossessingAgentId;

            // Priority 1: ball carrier within range
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                PerceivedAgent opp = snap.VisibleOpponents[i];
                if (opp.AgentId != possessorId) continue;
                float dist = Vector2.Distance(ctx.AgentPosition, opp.PerceivedPosition);
                if (dist <= UtilityWeights.PRESS_TRIGGER_DISTANCE)
                {
                    pressTargetId  = opp.AgentId;
                    pressTargetPos = opp.PerceivedPosition;
                    bestDist       = dist;
                    break;
                }
            }

            // Priority 2: nearest opponent within range
            if (pressTargetId == -1)
            {
                for (int i = 0; i < snap.VisibleOpponentsCount; i++)
                {
                    PerceivedAgent opp = snap.VisibleOpponents[i];
                    float dist = Vector2.Distance(ctx.AgentPosition, opp.PerceivedPosition);
                    if (dist <= UtilityWeights.PRESS_TRIGGER_DISTANCE && dist < bestDist)
                    {
                        bestDist       = dist;
                        pressTargetId  = opp.AgentId;
                        pressTargetPos = opp.PerceivedPosition;
                    }
                }
            }

            if (pressTargetId == -1) return count; // Gate 3: no valid target

            float proximity = Mathf.Clamp01(1.0f - bestDist / UtilityWeights.PRESS_TRIGGER_DISTANCE);

            buf[count++] = new ActionOption
            {
                Type           = ActionType.PRESS,
                TargetAgentId  = pressTargetId,
                TargetPosition = pressTargetPos,
                ProximityScore = proximity
            };

            return count;
        }

        // ── §3.1.9 INTERCEPT ────────────────────────────────────────────────────

        private static int GenerateInterceptCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            // Gate (§3.1.9.1)
            if (ctx.AgentHasBall) return count;
            if (!ctx.Snapshot.BallVisible) return count;
            if (ctx.Snapshot.BallStalenessFrames != 0) return count;
            if (ctx.MatchContext.Phase != MatchPhase.OPEN_PLAY) return count;

            Vector3 ballVel3 = ctx.MatchContext.BallVelocity;
            float ballSpeed  = new Vector2(ballVel3.x, ballVel3.y).magnitude;

            // §3.1.9.1 minimum-ball-speed gate — DELIBERATELY UNCHANGED by ERR-008-014 (match-engine
            // design note §5.Z Phase H). It rejects every slow ball, possessed or loose, and that is
            // correct: no slow ball should reach the §3.1.9.2 look-ahead geometry, where at v ≈ 0 every
            // projected point collapses onto the ball's own position and the MAX_INTERCEPT_TIME cap makes
            // a ball more than ~10 m away un-chaseable by anyone.
            //
            // ERR-008-014 was the observation that this left the tree with NO action that fetches a loose
            // ball lying at rest (PRESS targets an opponent, MOVE_TO_POSITION the formation slot, and
            // INTERCEPT bails out here), so a pass that simply ran out of momentum ended the match. The
            // fix is the collect short-circuit at the top of GenerateOffBallBranch, NOT a loosened gate
            // here — loosening it would make EVERY off-ball agent eligible to chase a resting ball, which
            // is precisely the converge-and-dither behaviour the single designated collector exists to
            // prevent (see GenerateLooseBallCollectCandidate).
            //
            // Consequence, accepted: a loose ball in the narrow band between FIRST_TOUCH_MIN_BALL_SPEED_M_S
            // and INTERCEPT_MIN_BALL_SPEED is claimable by nobody for the fraction of a second it takes to
            // decelerate below the pickup/collector gate — too fast for the host's loose-ball pickup and
            // collector designation, too slow for INTERCEPT. It is transient and self-healing (drag only
            // ever carries the ball DOWN through the band), so no mechanic is needed to cover it.
            if (ballSpeed < UtilityWeights.INTERCEPT_MIN_BALL_SPEED) return count;

            // Intercept geometry (§3.1.9.2)
            Vector2 ballPos  = ctx.Snapshot.BallPerceivedPosition;
            Vector2 ballDir  = new Vector2(ballVel3.x, ballVel3.y).normalized;
            float agentSpeed = AgentMaxSpeed(ctx.A_Pace);

            float bestTime  = float.MaxValue;
            Vector2 bestPt  = Vector2.zero;

            for (int step = 1; step <= DecisionTreeConstants.InterceptStepCount; step++)
            {
                float t = step * DecisionTreeConstants.InterceptStepSeconds;
                float decay      = Mathf.Exp(-UtilityWeights.DRAG_APPROX * t);
                float disp       = (ballSpeed / UtilityWeights.DRAG_APPROX) * (1.0f - decay);
                Vector2 projBall = ballPos + ballDir * disp;

                float travelDist = Vector2.Distance(projBall, ctx.AgentPosition);
                float travelTime = travelDist / Mathf.Max(agentSpeed, 0.01f);

                if (travelTime <= t && t < bestTime)
                {
                    bestTime = t;
                    bestPt   = projBall;
                    break;   // t ascends monotonically — first feasible t is minimal
                }
            }

            if (bestTime > UtilityWeights.MAX_INTERCEPT_TIME) return count; // Gate 5

            float feasibility = Mathf.Clamp01(1.0f - bestTime / UtilityWeights.MAX_INTERCEPT_TIME);
            if (feasibility <= 0.0f) return count;

            buf[count++] = new ActionOption
            {
                Type                      = ActionType.INTERCEPT,
                TargetAgentId             = -1,
                // INV-GEN-06: a projected intercept point beyond the boundary means
                // the ball is leaving play — intercept at the boundary (AR-2 L).
                TargetPosition            = PitchGeometry.ClampToPitch(bestPt),
                InterceptFeasibilityScore = feasibility,
                TimeToIntercept           = bestTime
            };

            return count;
        }

        /// <summary>
        /// ERR-008-014 — emits the "collect the loose ball" candidate: an INTERCEPT whose target is the
        /// ball where it lies. Reached only via the off-ball short-circuit, i.e. only when the host has set
        /// <see cref="TacticalContext.LooseBallCollector"/> for this agent, so it is always the sole
        /// option.
        ///
        /// <para>The §3.1.9.2 look-ahead geometry is deliberately NOT used here: at v ≈ 0 every projected
        /// point is the ball's own position, and that path's <c>MAX_INTERCEPT_TIME</c> feasibility cap
        /// (~10 m at a typical top speed) made a ball resting any further away un-chaseable by anyone —
        /// measured composed, that is what stopped the match once a pass ran out of momentum in space.
        /// Feasibility is 1.0 because, for a stationary ball, being the nearest player IS the
        /// feasibility.</para>
        /// </summary>
        private static int GenerateLooseBallCollectCandidate(
            in DecisionContext ctx,
            ActionOption[] buf,
            int count)
        {
            if (count >= buf.Length) return count;

            // The authoritative ball position, not the perceived one: the HOST designated this agent
            // from ground truth, so deriving the target from a (possibly stale) perceived position could
            // send the designated collector somewhere the ball is not.
            Vector2 ballPos = ctx.MatchContext.BallPosition;
            float myDist    = Vector2.Distance(ctx.AgentPosition, ballPos);

            buf[count++] = new ActionOption
            {
                Type                      = ActionType.INTERCEPT,
                TargetAgentId             = -1,
                TargetPosition            = PitchGeometry.ClampToPitch(ballPos),
                InterceptFeasibilityScore = 1.0f,
                TimeToIntercept           = myDist / Mathf.Max(AgentMaxSpeed(ctx.A_Pace), 0.01f)
            };

            return count;
        }

        // Returns approximate agent max speed (m/s) from normalised Pace attribute.
        // [CROSS — Agent Movement #2 §3.2.4]: Pace=1 → AGENT_SPEED_MIN_MPS, Pace=20 → AGENT_SPEED_MAX_MPS.
        private static float AgentMaxSpeed(float aNormPace)
        {
            return UtilityWeights.AGENT_SPEED_MIN_MPS
                + aNormPace * (UtilityWeights.AGENT_SPEED_MAX_MPS - UtilityWeights.AGENT_SPEED_MIN_MPS);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 M-2: Add using TacticalDirector.AgentMovement.                            |
// |         |            |        |   L-1: Replace magic numbers with named constants (DRIBBLE_SECTOR_HALF_ANGLE,  |
// |         |            |        |   PASS_LANE_ENDPOINT_MARGIN, InterceptStepSeconds/Count, AGENT_SPEED_*).       |
// |         |            |        |   L-4: CrossSubType ternary (always Flat) simplified to direct assignment.      |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: M-9 §3.1.3.6 proximity-ordered cap (closest-first when the          |
// |         |            |        |   Decisions cap binds; stackalloc marks, zero heap); M-7 ComputeGoalOpening-    |
// |         |            |        |   Score gains the §3.2.3.2 step-4 angular-overlap test, per-opponent clamp,     |
// |         |            |        |   goal-line (not goal-centre) GK heuristic, and the step-5 GOAL_OPENING_MIN     |
// |         |            |        |   floor; M-2 MOVE slot uses team-signed GetAdjustedFormationSlot(agentId,       |
// |         |            |        |   teamId); L INV-GEN-06 ClampToPitch on dribble/intercept/move targets; L dead  |
// |         |            |        |   DerivePassType params dropped with §3.1.3.4 WIDE_ZONE deviation note; L       |
// |         |            |        |   intercept scan breaks at first feasible t.                                    |
// | 1.3     | 2026-07-23 | —      | ERR-008-013: GenerateOffBallBranch short-circuits to GenerateSaveCandidate      |
// |         |            |        |   (SAVE alone) when TacticalContext.SaveAvailable — the DT-emitted goalkeeper   |
// |         |            |        |   save, robustly selected. SaveAvailable is flag-gated (keeper only), so the    |
// |         |            |        |   default off-ball branch is byte-identical.                                    |
// | 1.4     | 2026-07-26 | —      | ERR-008-014 (match-engine §5.Z Phase H): GenerateOffBallBranch  |
// |         |            |        |   short-circuits to GenerateLooseBallCollectCandidate (the      |
// |         |            |        |   collect ALONE) when TacticalContext.LooseBallCollector — the  |
// |         |            |        |   tree previously had no action at all that fetches a loose     |
// |         |            |        |   ball lying at rest, so play died the first time a pass ran    |
// |         |            |        |   out of momentum. Sole-option per the §3.1.13 SAVE precedent.  |
// |         |            |        |   The §3.1.9.1 minimum-ball-speed gate is deliberately          |
// |         |            |        |   UNCHANGED (its comment records why loosening it was refused). |
// | 1.5     | 2026-07-28 | —      | ERR-008-016 (shot-speed design KD-1): PowerIntent = clamp(FLOOR +           |
// |         |            |        | (1−FLOOR) × goalOpening × A_Finishing, FLOOR, 1) — the former product of    |
// |         |            |        | two [0,1] fractions pinned nearly every shot at the 0.1 clamp floor         |
// |         |            |        | (measured shot-tick means 7–10 m/s vs football ~25).                        |
#endregion
