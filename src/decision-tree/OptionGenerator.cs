// File:     src/decision-tree/OptionGenerator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Modified: 2026-07-26 (ERR-008-014 — loose-ball collect emitted as the SOLE off-ball option for the host-designated collector)
// Modified: 2026-07-28 (ERR-008-016 — PowerIntent floor-plus-modulation (shot-speed design KD-1))
// Modified: 2026-08-04 (ERR-008-020 — CountInterceptors → ComputeLaneThreat: continuous falloff × Vision-read Anticipation/Pace ability)
// Modified: 2026-08-05 (ERR-008-021 — shot lane: wedge-containment cliff → true angular overlap × Vision-read Anticipation/Positioning ability)
// Modified: 2026-08-06 (ERR-008-022 — the lane's far bound moves to the goal-line plane; the near bound and the goalkeeper read become ramps)
// Modified: 2026-08-07 (ERR-008-023 — the keeper occludes with a BODY radius; his reach is #11's under P3. Fixes goals-still-scored = 0)
// Modified: 2026-08-09 (ERR-008-024 — §3.1.5.2 sector scan breaks EXACT space ties by DirectionQuality_DRIBBLE; the strict > on saturated space always kept sector 0 = AgentFacingDirection. Wider product-ranking form reverted at the gate: it stalled play and scored 0 goals)
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
            // Pass lane viability (§3.1.3.3, ERR-008-020)
            float laneThreat = ComputeLaneThreat(in ctx, tm.PerceivedPosition);
            float passLaneScore = Mathf.Clamp01(1.0f - laneThreat / UtilityWeights.PASS_LANE_DIVISOR);

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

        // §3.1.3.3 pass-lane threat (ERR-008-020, judgment-proxy doctrine §6.4). Each visible
        // opponent near the lane contributes falloff × perceivedAbility instead of a binary
        // 1-in / 0-out count: the falloff kills the 2 cm positional cliff at the old 0.8 m
        // corridor edge (doctrine P1), and the ability term reads the defender's
        // Anticipation/Pace through the passer's Vision fidelity (doctrine P2) — a slow,
        // poor-anticipation defender no longer blocks a lane as hard as an elite one, but
        // only a sharp-eyed passer fully exploits the difference.
        private static float ComputeLaneThreat(in DecisionContext ctx, Vector2 targetPos)
        {
            Vector2 laneVec = targetPos - ctx.AgentPosition;
            float laneLen2  = laneVec.sqrMagnitude;
            float threat    = 0.0f;

            float fidelity = UtilityWeights.LANE_VISION_FIDELITY_FLOOR
                + (1.0f - UtilityWeights.LANE_VISION_FIDELITY_FLOOR) * ctx.A_Vision;

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

                if (tClamped <= UtilityWeights.PASS_LANE_ENDPOINT_MARGIN
                    || tClamped >= 1.0f - UtilityWeights.PASS_LANE_ENDPOINT_MARGIN)
                {
                    continue;
                }

                float falloff = Mathf.Clamp01(
                    (UtilityWeights.PASS_LANE_FALLOFF_END - perpDist)
                    / (UtilityWeights.PASS_LANE_FALLOFF_END
                       - UtilityWeights.PASS_LANE_CORE_HALF_WIDTH));
                if (falloff <= 0.0f)
                {
                    continue;
                }

                threat += falloff * PerceivedInterceptAbility(
                    in ctx, snap.VisibleOpponents[i].AgentId, fidelity);
            }

            return threat;
        }

        // The defender's true interception ability (Anticipation+Pace mean mapped to
        // ABILITY_MIN..MAX; league-average ⇒ exactly 1.0), blended toward neutral by the
        // passer's Vision fidelity — perceived = 1 + fidelity × (true − 1), doctrine P2.
        // A null attribute view (unwired host / legacy test) reads every opponent as 1.0,
        // which is the pre-ERR-008-020 attribute-blind weighting.
        private static float PerceivedInterceptAbility(
            in DecisionContext ctx, int opponentId, float fidelity)
        {
            DtAgentAttributes[] all = ctx.AllAgentAttributes;
            if (all == null || opponentId < 0 || opponentId >= all.Length)
            {
                return 1.0f;
            }

            float aAnticipation = (all[opponentId].Anticipation - DecisionTreeConstants.AttributeNormMin)
                / DecisionTreeConstants.AttributeNormRange;
            float aPace = (all[opponentId].Pace - DecisionTreeConstants.AttributeNormMin)
                / DecisionTreeConstants.AttributeNormRange;
            float mean01 = 0.5f * (aAnticipation + aPace);

            float trueAbility = UtilityWeights.INTERCEPTOR_ABILITY_MIN
                + (UtilityWeights.INTERCEPTOR_ABILITY_MAX - UtilityWeights.INTERCEPTOR_ABILITY_MIN)
                  * mean01;

            return 1.0f + fidelity * (trueAbility - 1.0f);
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
            float goalOpeningScore = ComputeGoalOpeningScore(in ctx);
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

        // §3.2.3.2 goal-opening derivation (ERR-008-021, judgment-proxy doctrine §6.4 —
        // the shot lane ERR-008-020 deliberately left behind). Two changes to the blocked
        // arc, the shot-lane twins of the pass-lane fix:
        //
        //   P1 — the wedge-containment test was binary: a blocker whose angular CENTRE
        //   sat inside the goal arc contributed its whole width even where half that
        //   width spilled past the post, and one whose centre sat a hair outside
        //   contributed nothing at all despite standing squarely across the near post.
        //   Replaced by the true angular OVERLAP of the blocking disc with the goal arc.
        //   That is continuous by construction — no ramp constant, no tolerance epsilon —
        //   and it is also the geometrically honest answer, so it fixes the over- and
        //   under-blocking together with the cliff.
        //
        //   P2 — the width was body radius alone, so the shooter's read of the goal was
        //   blind to who was in front of him: a defender who neither reads the shot nor
        //   gets his body across shut the goal off exactly as hard as one who does. The
        //   overlap is now scaled by the blocker's Anticipation/Positioning ability, read
        //   through the shooter's own Vision as discrimination fidelity.
        //
        // P3 (ownership ledger): the GOALKEEPER is exempt from the ability term and
        // occludes on geometry alone. Keeper shot-stopping quality is Goalkeeper
        // Mechanics #11's to price (§3.5 save model, and the #11 §3.7.0 rush that sets
        // this very geometry); pricing it here too would charge the shooter twice for the
        // same keeper. Stage 0 has no GK role flag, so "is he the keeper" is a proximity
        // read — and therefore a SCALAR (ERR-008-022): the exemption fades in with it
        // rather than switching on at a boundary.
        private static float ComputeGoalOpeningScore(in DecisionContext ctx)
        {
            Vector2 agentPos  = ctx.AgentPosition;
            Vector2 goalPostL = ctx.OpponentGoalPostL;
            Vector2 goalPostR = ctx.OpponentGoalPostR;

            float totalArc   = AngularSpan(agentPos, goalPostL, goalPostR);
            if (totalArc <= 0.0f) return 0.0f;   // degenerate: agent on the goal line

            Vector2 dirL = (goalPostL - agentPos).normalized;
            Vector2 dirR = (goalPostR - agentPos).normalized;

            // The goal arc measured about its own bisector is exactly [−halfArc, +halfArc],
            // which is what makes the overlap below a plain interval intersection. The
            // bisector degenerates only at a 180° arc — unreachable from anywhere a shot
            // is generated, but guarded rather than left to produce a zero vector.
            Vector2 bisector = dirL + dirR;
            if (bisector.sqrMagnitude < DecisionTreeConstants.BisectorDegenerateSqrThreshold)
            {
                return 0.0f;   // as a zero arc
            }
            bisector = bisector.normalized;

            float halfArc   = 0.5f * totalArc;
            float goalLineX = goalPostL.x;

            float fidelity = UtilityWeights.LANE_VISION_FIDELITY_FLOOR
                + (1.0f - UtilityWeights.LANE_VISION_FIDELITY_FLOOR) * ctx.A_Vision;

            float blockedArc = 0.0f;
            FilteredView snap = ctx.Snapshot;
            for (int i = 0; i < snap.VisibleOpponentsCount; i++)
            {
                Vector2 oPos = snap.VisibleOpponents[i].PerceivedPosition;

                // How much of this opponent's occlusion the lane admits at all
                // (§3.1.4.3 ShotPathWeight): 0 behind the shooter or past the goal line,
                // ramping to 1 with lane depth.
                float laneWeight = ShotPathWeight(agentPos, ctx.OpponentGoalCentre, oPos,
                    goalLineX, UtilityWeights.GOAL_MIN_SHOT_DIST);
                if (laneWeight <= 0.0f) continue;

                float dist = Vector2.Distance(agentPos, oPos);

                // §3.2.3.2 GK read: distance to the GOAL LINE (X axis) — not to the goal
                // centre, which misclassified a goal-line keeper positioned wide of centre
                // as a 0.5 m outfield blocker (AR-2 M-7). A SCALAR, not a predicate, so the
                // heuristic's boundary is a slope rather than the 0.768 ⇒ 0.311 step over
                // 2 cm it was (ERR-008-022). It now lerps the P3 ability exemption ONLY.
                float gkness = 1.0f - Mathf.Clamp01(
                    (Mathf.Abs(oPos.x - goalLineX)
                     - (UtilityWeights.GK_PROXIMITY_TO_GOAL - 0.5f * UtilityWeights.GK_PROXIMITY_FADE_M))
                    / UtilityWeights.GK_PROXIMITY_FADE_M);

                // ERR-008-023: every blocker occludes with the same BODY radius, keeper
                // included. The keeper's reach beyond his body is shot-stopping, which P3
                // assigns to Goalkeeper Mechanics #11 (§3.5/§3.7.0) and which #11 already
                // prices at contact — charging it again here read it twice. The old
                // keeper-only 1.5 m disc was never exercised: the pre-ERR-008-022 lane bound
                // dropped a goal-line keeper for every shooter position, so it went live for
                // the first time at that landing and removed ~42% of the goal arc on every
                // shot, which is what took `goals-still-scored` to zero.
                float radius = UtilityWeights.BLOCKER_RADIUS_M;

                // The disc subtends [centre − halfWidth, centre + halfWidth]; intersect
                // it with the goal arc [−halfArc, +halfArc].
                Vector2 dirO    = (oPos - agentPos) / dist;
                float halfWidth = Mathf.Atan2(radius, dist) * Mathf.Rad2Deg;
                float centre    = SignedAngleDeg(bisector, dirO);

                float overlap = Mathf.Min(centre + halfWidth, halfArc)
                              - Mathf.Max(centre - halfWidth, -halfArc);
                if (overlap <= 0.0f) continue;

                // P3 ownership: the keeper's own shot-stopping is Goalkeeper Mechanics
                // #11's to price, so the ability term fades out as gkness fades in.
                float ability = Mathf.Lerp(
                    PerceivedBlockAbility(in ctx, snap.VisibleOpponents[i].AgentId, fidelity),
                    1.0f, gkness);

                blockedArc += Mathf.Min(overlap * ability * laneWeight, totalArc);
            }

            blockedArc = Mathf.Min(blockedArc, totalArc);
            float score = (totalArc - blockedArc) / totalArc;

            // Floor at GOAL_OPENING_MIN ("a tiny gap always exists"). The floor (0.05)
            // sits BELOW MIN_GOAL_VISIBILITY (0.12), so a fully occluded lane still fails
            // the §3.1.4.1 gate (4) and generates no SHOOT — the floor bounds the score,
            // it does not keep the option alive.
            return Mathf.Clamp(score, UtilityWeights.GOAL_OPENING_MIN, 1.0f);
        }

        // The blocker's true ability to actually get across the shot (Anticipation +
        // Positioning mean mapped to ABILITY_MIN..MAX; league-average ⇒ exactly 1.0, so an
        // average blocker occludes precisely the bare geometric arc), blended toward
        // neutral by the shooter's Vision fidelity — perceived = 1 + fidelity × (true − 1),
        // doctrine P2. A null attribute view (unwired host / legacy test) reads every
        // blocker as 1.0, which is the pre-ERR-008-021 geometry-only occlusion.
        private static float PerceivedBlockAbility(
            in DecisionContext ctx, int opponentId, float fidelity)
        {
            DtAgentAttributes[] all = ctx.AllAgentAttributes;
            if (all == null || opponentId < 0 || opponentId >= all.Length)
            {
                return 1.0f;
            }

            float aAnticipation = (all[opponentId].Anticipation - DecisionTreeConstants.AttributeNormMin)
                / DecisionTreeConstants.AttributeNormRange;
            float aPositioning = (all[opponentId].Positioning - DecisionTreeConstants.AttributeNormMin)
                / DecisionTreeConstants.AttributeNormRange;
            float mean01 = 0.5f * (aAnticipation + aPositioning);

            float trueAbility = UtilityWeights.SHOT_BLOCKER_ABILITY_MIN
                + (UtilityWeights.SHOT_BLOCKER_ABILITY_MAX - UtilityWeights.SHOT_BLOCKER_ABILITY_MIN)
                  * mean01;

            return 1.0f + fidelity * (trueAbility - 1.0f);
        }

        private static float AngularSpan(Vector2 origin, Vector2 a, Vector2 b)
        {
            Vector2 dirA = (a - origin).normalized;
            Vector2 dirB = (b - origin).normalized;
            return Mathf.Acos(Mathf.Clamp(Vector2.Dot(dirA, dirB), -1.0f, 1.0f)) * Mathf.Rad2Deg;
        }

        // Signed angle from `from` to `to` in degrees, in (−180, 180]. Computed inline
        // rather than via Vector2.SignedAngle so the shot-lane overlap depends on one
        // arithmetic path we control. Which rotation sense counts as positive does NOT
        // matter to the caller: the goal arc is symmetric about its bisector, so the
        // overlap is an even function of this value.
        private static float SignedAngleDeg(Vector2 from, Vector2 to)
        {
            float cross = from.x * to.y - from.y * to.x;
            float dot   = from.x * to.x + from.y * to.y;
            return Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;
        }

        // The fraction of his occlusion an opponent contributes to the shot lane
        // (§3.1.4.3). Two ERR-008-022 corrections to the boolean this replaces:
        //
        //   The far bound was `proj < distToGoal` — a plane through the GOAL CENTRE,
        //   perpendicular to the shooting axis. For any off-centre shooter that plane
        //   cuts diagonally across the goal mouth, so it discarded the FAR-POST blocker
        //   while keeping the near-post one (measured: 100% of sampled off-centre
        //   shooters), and it dropped a keeper standing on his line at goal centre for
        //   every shooter position (proj == distToGoal exactly). Its mirror admitted an
        //   opponent standing BEHIND the goal line — in the net — and handed him the
        //   keeper's blocking radius. The bound is now the goal-line PLANE, which is the
        //   surface the shot actually has to reach.
        //
        //   The near bound was a hard predicate that flipped a whole SHOOT decision on
        //   one centimetre of blocker position (see SHOT_BLOCKER_NEAR_FADE_M). It now
        //   ramps — doctrine P1.
        private static float ShotPathWeight(Vector2 agentPos, Vector2 goalCentre,
            Vector2 oppPos, float goalLineX, float minDist)
        {
            float shooterToLine = goalLineX - agentPos.x;
            float blockerToLine = goalLineX - oppPos.x;

            if (shooterToLine * blockerToLine < 0.0f) return 0.0f;   // past the goal line
            if (Mathf.Abs(blockerToLine) > Mathf.Abs(shooterToLine)) return 0.0f;  // behind the shooter

            Vector2 shotDir = (goalCentre - agentPos).normalized;
            float proj = Vector2.Dot(oppPos - agentPos, shotDir);

            // The ramp is CENTRED on the old predicate (half its width either side), so a
            // uniformly-placed blocker contributes the same integrated occlusion as before
            // — the doctrine P5 pivot, and the shape ERR-008-019 and ERR-008-020 both used.
            // A ramp running from the old threshold upward would have been a one-sided
            // reduction dressed as a continuity fix.
            float rampStart = minDist - 0.5f * UtilityWeights.SHOT_BLOCKER_NEAR_FADE_M;
            return Mathf.Clamp01((proj - rampStart) / UtilityWeights.SHOT_BLOCKER_NEAR_FADE_M);
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

            // 8-sector space scan (§3.1.5.2): highest free space wins, ties to the earliest sector.
            //
            // ERR-008-024 IS A KNOWN DEFECT IN THIS SCAN, RECORDED AND DELIBERATELY NOT FIXED HERE.
            // `spaceInSector` saturates at exactly 1.0 for every sector holding no opponent inside
            // DRIBBLE_THREAT_RADIUS, and the strict `>` below therefore resolves every tie to the
            // first sector scanned — which is AgentFacingDirection by construction. So whenever two
            // or more sectors are clear, the common case in the final third, the carrier dribbles
            // wherever he already faces and goal direction never enters the choice. That is why
            // ERR-008-018's DirectionQuality_DRIBBLE can suppress a retreating dribble but never
            // redirect it (close-chance-creation KD-CC3 / §7 item 6).
            //
            // THE FIX WAS IMPLEMENTED, MEASURED AND REFUSED — the KD-CC7 pattern. Breaking the tie
            // toward the more goalward sector does fix the symptom: sim_match_engine_close_chance
            // goes from meanCosine −0.165 / goalwardShare 0.407 (both failing) to PASS. But the same
            // build stalls play outright — sim_match_engine_play_develops dies with the ball last
            // moving at tick 18465 of 32400 — and sim_match_engine_shot_outcomes scores ZERO goals.
            // A wider form ranking on `space × DirectionQuality` produced the identical stall at the
            // identical tick, which is what localises the cause to the tie-break itself rather than
            // to how much space it trades away.
            //
            // Do not re-attempt this in isolation. Sending dribbles goalward is only safe once the
            // engine can survive the congestion that follows, and the measured blockers are recorded
            // in close-chance-creation-design.md §10 — nobody can receive a ball above 0.5 m, and no
            // composed slot reaches the penalty area.
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
// | 1.6     | 2026-08-04 | —      | ERR-008-020 (judgment-proxy doctrine §6.4): CountInterceptors →             |
// |         |            |        | ComputeLaneThreat. Per-opponent weight = linear falloff (core 0.4 m,        |
// |         |            |        | zero at 1.2 m — ramp centred on the old 0.8 m cliff, integrated threat      |
// |         |            |        | preserved) × PerceivedInterceptAbility (Anticipation+Pace → 0.6..1.4,       |
// |         |            |        | blended toward 1.0 by the passer's Vision fidelity). Null attribute view    |
// |         |            |        | ⇒ ability 1.0 = the old attribute-blind weighting. §3.1.4's                 |
// |         |            |        | ComputeGoalOpeningScore (shot lane) deliberately untouched (deferred).      |
// | 1.7     | 2026-08-05 | —      | ERR-008-021 (the deferral above, now closed): ComputeGoalOpeningScore's     |
// |         |            |        | binary wedge-containment test (angular centre in the goal arc ⇒ the WHOLE   |
// |         |            |        | disc width, outside ⇒ nothing) becomes the true angular OVERLAP of the      |
// |         |            |        | disc with the arc — continuous by construction, and integrating to the      |
// |         |            |        | identical occlusion over a uniformly-placed blocker (P5). Overlap scaled    |
// |         |            |        | by PerceivedBlockAbility (Anticipation+Positioning → 0.6..1.4, blended      |
// |         |            |        | toward 1.0 by the SHOOTER's Vision fidelity — P2). Goalkeeper exempt from   |
// |         |            |        | the ability term: #11 owns keeper shot-stopping (P3, no double-count).      |
// |         |            |        | + SignedAngleDeg; the ArcOverlapToleranceDeg epsilon the containment test   |
// |         |            |        | needed is gone with it. Null attribute view ⇒ ability 1.0 = geometry only.  |
// | 1.8     | 2026-08-06 | —      | ERR-008-022 (adversarial review over the -021 landing). IsInShotPath →      |
// |         |            |        | ShotPathWeight. Far bound was a plane through the GOAL CENTRE, which cuts   |
// |         |            |        | diagonally across the goal mouth for an off-centre shooter: it discarded    |
// |         |            |        | the FAR-POST blocker on 100% of 20,213 sampled in-range off-centre          |
// |         |            |        | shooters, dropped a keeper on his line at goal centre for EVERY shooter     |
// |         |            |        | (proj == distToGoal exactly), and admitted an opponent behind the goal      |
// |         |            |        | line at the keeper's radius. Now bounded by the goal-line plane. Near       |
// |         |            |        | bound was a hard predicate flipping the score 1.000 → 0.050 (and the        |
// |         |            |        | SHOOT option's existence) across 1 cm; now ramps over                       |
// |         |            |        | SHOT_BLOCKER_NEAR_FADE_M. The isGk predicate flipped radius 0.5 ⇒ 1.5 m     |
// |         |            |        | and the P3 ability exemption together (0.768 ⇒ 0.311 over 2 cm); now a      |
// |         |            |        | scalar gkness lerping both over GK_PROXIMITY_FADE_M. Also: the              |
// |         |            |        | BisectorDegenerateSqrLen local promoted to a tagged catalogue constant;     |
// |         |            |        | the dead dist<0.001 guard removed (ShotPathWeight now proves dist > 1 m);   |
// |         |            |        | the false 'sign convention is load-bearing' comment on SignedAngleDeg       |
// |         |            |        | corrected (the overlap is even in that value); the stale gate-4 comment     |
// |         |            |        | corrected — GOAL_OPENING_MIN 0.05 is BELOW MIN_GOAL_VISIBILITY 0.12, so a   |
// |         |            |        | fully occluded lane does still fail the gate.                               |
// | 1.9     | 2026-08-06 | —      | ERR-008-022 AR-2: both new ramps RE-CENTRED on the predicate they replace   |
// |         |            |        | (half-width either side) rather than running upward from it. As first       |
// |         |            |        | landed they were a one-sided reduction in occlusion dressed as a            |
// |         |            |        | continuity fix — the P5 pivot ERR-008-019 and -020 both observed. Value     |
// |         |            |        | locks unchanged (all sit outside the ramp bands); the two continuity        |
// |         |            |        | sweeps re-ranged to span the centred bands.                                |
// | 1.10    | 2026-08-07 | —      | ERR-008-023 (acceptance-scenario regression from the -022 landing).        |
// |         |            |        | ComputeGoalOpeningScore no longer lerps a keeper-only blocking radius:     |
// |         |            |        | every blocker occludes with BLOCKER_RADIUS_M, the goalkeeper included.     |
// |         |            |        | A keeper's reach beyond his body is shot-stopping, which P3 assigns to     |
// |         |            |        | Goalkeeper Mechanics #11 and #11 already prices at contact — the read      |
// |         |            |        | charged him twice. The 1.5 m disc had never been exercised (the pre-022    |
// |         |            |        | lane bound discarded a goal-line keeper for every shooter position), so    |
// |         |            |        | it went live at -022 and removed ~42% of the goal arc on every shot.       |
// |         |            |        | sim_match_engine_shot_outcomes measured goals-still-scored = 0 over four   |
// |         |            |        | seeds x 18 min. gkness survives, lerping the P3 exemption alone.           |
// | 1.11    | 2026-08-09 | —      | ERR-008-024. §3.1.5.2's 8-sector scan now ranks on spaceInSector ×         |
// |         |            |        | UtilityWeights.DribbleDirectionQuality(sectorDir, toGoal) instead of       |
// |         |            |        | spaceInSector alone with a strict > test. spaceInSector saturates at 1.0   |
// |         |            |        | for any sector clear of DRIBBLE_THREAT_RADIUS, and sector 0 is             |
// |         |            |        | AgentFacingDirection by construction, so whenever two or more sectors      |
// |         |            |        | were clear — the common case in the final third — the winner was always   |
// |         |            |        | sector 0: the carrier dribbled wherever he already faced and goal          |
// |         |            |        | direction had no influence. Ranking on the same term §3.2.4.1 already      |
// |         |            |        | applies at scoring means generation now proposes the candidate the        |
// |         |            |        | scorer would prefer. No new constant — the floor is ERR-008-018's          |
// |         |            |        | DRIBBLE_GOAL_DIR_MIN_MODIFIER, unchanged, so direction can outrank at      |
// |         |            |        | most a 20% space deficit (KD-CC6 preserved). Closes                        |
// |         |            |        | close-chance-creation-design.md §7 item 6 / KD-CC3.                        |
// |         |            |        | sim_match_engine_close_chance: meanCosine −0.165 → PASS (bound −0.16),     |
// |         |            |        | goalwardShare 0.407 → PASS (bound 0.42); neither bound moved.              |
#endregion
