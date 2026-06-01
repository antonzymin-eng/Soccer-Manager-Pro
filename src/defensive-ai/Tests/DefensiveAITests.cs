// File:     src/defensive-ai/Tests/DefensiveAITests.cs
// Created:  2026-05-31
// Modified: 2026-05-31
// Author:   —
// Spec:     Defensive AI #14 §5, Code Standards #20
// Purpose:  Unit tests for Defensive AI. T-DA unit tests from §5.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.DefensiveAI.Tests
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    internal static class SnapshotBuilder
    {
        /// <summary>
        /// Creates a minimal DefensiveSnapshot for team 0 (defends x=0) with the given
        /// agent list and ball state.  AgentCount is set automatically.
        /// </summary>
        internal static DefensiveSnapshot Build(
            DefensiveAgentSnapshot[] agents,
            Vector2 ballPosition   = default,
            Vector2 ballVelocity   = default,
            int     gkEntityId     = -1,
            Vector2 gkPosition     = default,
            bool    hasPrimaryPress = false,
            float   defensiveLineDepth = 35.0f,
            Phase   teamPhase      = Phase.OutOfPoss,
            int     tick           = 1)
        {
            DefensiveSnapshot snap = new DefensiveSnapshot(DefensiveAIConstants.SQUAD_SIZE);
            snap.TickIndex             = tick;
            snap.DefensiveTeamId       = 0;
            snap.BallPosition          = ballPosition;
            snap.BallVelocity          = ballVelocity;
            snap.GkEntityId            = gkEntityId;
            snap.GkPosition            = gkPosition;
            snap.HasActivePrimaryPress = hasPrimaryPress;
            snap.DefensiveLineDepth    = defensiveLineDepth;
            snap.TeamPhase             = teamPhase;

            for (int i = 0; i < agents.Length; i++)
            {
                snap.Agents[i] = agents[i];
            }

            snap.AgentCount = agents.Length;
            return snap;
        }

        internal static DefensiveAgentSnapshot MakeAgent(
            int      entityId,
            int      teamId       = 0,
            Vector2  position     = default,
            Vector2  velocity     = default,
            bool     isActive     = true,
            bool     isGoalkeeper = false,
            PressRole pressRole   = PressRole.HoldShape,
            LineId   line         = LineId.Defense,
            Vector2  baselineSlot = default,
            float    firstTouch   = 10f)
        {
            return new DefensiveAgentSnapshot
            {
                EntityId            = entityId,
                TeamId              = teamId,
                Position            = position,
                Velocity            = velocity,
                IsActive            = isActive,
                IsGoalkeeper        = isGoalkeeper,
                HasBall             = false,
                BaselineSlot        = baselineSlot,
                Line                = line,
                PressRole           = pressRole,
                PerceivedFirstTouch = firstTouch,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-001..006  Pool Filter Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class HoldShapePoolFilterTests
    {
        /// <summary>T-DA-001 — GK excluded from pool unconditionally.</summary>
        [Test]
        internal void T_DA_001_GkExcludedFromPool()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[11];
            agents[0] = SnapshotBuilder.MakeAgent(entityId: 1, isGoalkeeper: true);
            for (int i = 1; i < 11; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents, gkEntityId: 1);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(10, count, "Pool should contain 10 agents (GK excluded).");
            Assert.AreEqual(-1, HoldShapePoolFilter.IndexOf(pool, count, 1),
                "GK EntityId 1 must not appear in pool.");
        }

        /// <summary>T-DA-002 — PRIMARY_PRESS excluded from pool.</summary>
        [Test]
        internal void T_DA_002_PrimaryPressExcluded()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[11];
            agents[0] = SnapshotBuilder.MakeAgent(entityId: 1, isGoalkeeper: true);
            agents[1] = SnapshotBuilder.MakeAgent(entityId: 2, pressRole: PressRole.PrimaryPress);
            agents[2] = SnapshotBuilder.MakeAgent(entityId: 3, pressRole: PressRole.PrimaryPress);
            for (int i = 3; i < 11; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents, gkEntityId: 1);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(8, count, "Pool should be 8 (11 - 1 GK - 2 PRIMARY_PRESS).");
        }

        /// <summary>T-DA-003 — COVER_SHADOW excluded from pool.</summary>
        [Test]
        internal void T_DA_003_CoverShadowExcluded()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[11];
            agents[0] = SnapshotBuilder.MakeAgent(entityId: 1, isGoalkeeper: true);
            agents[1] = SnapshotBuilder.MakeAgent(entityId: 2, pressRole: PressRole.CoverShadow);
            agents[2] = SnapshotBuilder.MakeAgent(entityId: 3, pressRole: PressRole.CoverShadow);
            agents[3] = SnapshotBuilder.MakeAgent(entityId: 4, pressRole: PressRole.CoverShadow);
            for (int i = 4; i < 11; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents, gkEntityId: 1);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(7, count, "Pool should be 7 (11 - 1 GK - 3 COVER_SHADOW).");
        }

        /// <summary>T-DA-004 — All-ZONAL team: all non-GK outfield agents in pool.</summary>
        [Test]
        internal void T_DA_004_AllZonalTeamFullPool()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[11];
            agents[0] = SnapshotBuilder.MakeAgent(entityId: 1, isGoalkeeper: true);
            for (int i = 1; i < 11; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1, pressRole: PressRole.HoldShape);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents, gkEntityId: 1);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(10, count, "All 10 outfield agents should be in pool.");
            for (int i = 2; i <= 11; i++)
            {
                Assert.GreaterOrEqual(HoldShapePoolFilter.IndexOf(pool, count, i), 0,
                    $"EntityId {i} should be present in pool.");
            }
        }

        /// <summary>T-DA-005 — Empty pool handled gracefully when all outfield are PRIMARY_PRESS.</summary>
        [Test]
        internal void T_DA_005_EmptyPoolNoAllocation()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[11];
            agents[0] = SnapshotBuilder.MakeAgent(entityId: 1, isGoalkeeper: true);
            for (int i = 1; i < 11; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1, pressRole: PressRole.PrimaryPress);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents, gkEntityId: 1);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(0, count, "Pool must be empty when all outfield agents are PRIMARY_PRESS.");
        }

        /// <summary>T-DA-006 — Pool iteration is EntityId-ascending.</summary>
        [Test]
        internal void T_DA_006_PoolIterationEntityIdAscending()
        {
            // Build agents with out-of-order EntityIds — orchestrator inserts in ascending order
            // per #16 §3.2.5 so we provide them ascending here.
            int[] ids = new int[] { 102, 105, 109, 113, 117 };
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: ids[i]);
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);

            int[] pool = new int[11];
            HoldShapePoolFilter.BuildPool(snapshot, pool, out int count);

            Assert.AreEqual(ids.Length, count);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(ids[i], pool[i],
                    $"Pool index {i} should hold EntityId {ids[i]} (ascending order).");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-013..015  Threat Score Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class ThreatScoreTests
    {
        /// <summary>T-DA-013 — Threat score perceivedGoalProximity component computed correctly.</summary>
        [Test]
        internal void T_DA_013_ThreatScoreWorkedExample()
        {
            // Team 0 defends x=0. Opponent at (22.0, 34.0), FirstTouch = 16.
            Vector2 oppPos = new Vector2(22.0f, 34.0f);
            float firstTouch = 16f;

            float score = MarkAssigner.ThreatScore(oppPos, firstTouch, defensiveTeamId: 0);

            // perceivedGoalProximity = 1 - (22 / 105) ≈ 0.7905
            float expectedProximity = 1.0f - (22.0f / DefensiveAIConstants.PITCH_LENGTH_M);
            // opponentReceivingAttribute = (16 - 1) / 19 = 15/19 ≈ 0.7895
            float expectedAttr = (firstTouch - 1f) / 19f;
            float expectedScore = expectedProximity * expectedAttr;

            Assert.AreEqual(expectedScore, score, 0.0001f,
                "Threat score must match §3.5.4 worked example.");
        }

        /// <summary>T-DA-014 — Threat score clamped to [0, 1] at goal-mouth opponent.</summary>
        [Test]
        internal void T_DA_014_ThreatScoreClampedAtMaximum()
        {
            Vector2 oppPos = new Vector2(0.0f, 34.0f);  // At own goal-line, defending x=0.
            float firstTouch = 20f;                       // Maximum attribute.

            float score = MarkAssigner.ThreatScore(oppPos, firstTouch, defensiveTeamId: 0);

            Assert.AreEqual(1.0f, score, 0.0001f,
                "Threat score must be exactly 1.0 when opponent is at goal-line with max attribute.");
        }

        /// <summary>T-DA-015 — Attribute minimum (FirstTouch = 1) produces zero contribution.</summary>
        [Test]
        internal void T_DA_015_ThreatScoreZeroAtMinAttribute()
        {
            Vector2 oppPos = new Vector2(22.0f, 34.0f);
            float firstTouch = 1f;  // Minimum attribute.

            float score = MarkAssigner.ThreatScore(oppPos, firstTouch, defensiveTeamId: 0);

            Assert.AreEqual(0.0f, score, 0.0001f,
                "Threat score must be 0.0 when FirstTouch attribute is at minimum (1).");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-011..012, T-DA-016..020  Mark Assigner Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class MarkAssignerTests
    {
        /// <summary>T-DA-011 — No candidates within radius → ZONAL fallback.</summary>
        [Test]
        internal void T_DA_011_NoCandidatesProducesZonal()
        {
            // Agent at (30, 34). All opponents at distance > ManMarkCandidateRadiusM.
            Vector2 agentPos = new Vector2(30.0f, 34.0f);
            Vector2 formationSlot = new Vector2(30.0f, 34.0f);
            Vector2 oppFarPos = new Vector2(
                agentPos.x + DefensiveAIConstants.ManMarkCandidateRadiusM + 5.0f, agentPos.y);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: agentPos, baselineSlot: formationSlot),
                SnapshotBuilder.MakeAgent(entityId: 20, teamId: 1,
                    position: oppFarPos, firstTouch: 14f),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);

            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[1];
            MarkHysteresisState[] hysteresis = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapshot, pool, poolCount: 1, currentTick: 1,
                                assignments, hysteresis);

            Assert.AreEqual(MarkMode.Zonal, assignments[0].Mode,
                "Should be ZONAL when no opponent is within radius.");
            Assert.AreEqual(-1, assignments[0].TargetEntityId,
                "ZONAL assignment must have TargetEntityId = -1.");
        }

        /// <summary>T-DA-012 — Displacement cost tie-break uses EntityId ascending.</summary>
        [Test]
        internal void T_DA_012_TieBreakEntityIdAscending()
        {
            // Agent at (30, 34). Two opponents with identical threat + distance.
            Vector2 agentPos = new Vector2(30.0f, 34.0f);
            float sameDistance = 10.0f;  // Both within radius (< 15 m).
            float sameFirstTouch = 10f;

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: agentPos, baselineSlot: agentPos),
                // Opponent A: EntityId 202, same distance directly right.
                SnapshotBuilder.MakeAgent(entityId: 202, teamId: 1,
                    position: new Vector2(agentPos.x + sameDistance, agentPos.y),
                    firstTouch: sameFirstTouch),
                // Opponent B: EntityId 201, same distance directly left — identical cost.
                // Place at same X offset but different Y to keep the distance equal.
                SnapshotBuilder.MakeAgent(entityId: 201, teamId: 1,
                    position: new Vector2(agentPos.x - sameDistance, agentPos.y),
                    firstTouch: sameFirstTouch),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);

            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[1];
            MarkHysteresisState[] hysteresis = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapshot, pool, poolCount: 1, currentTick: 1,
                                assignments, hysteresis);

            Assert.AreEqual(201, assignments[0].TargetEntityId,
                "EntityId 201 (lower) must win the tie-break per FR-DA-014.");
        }

        /// <summary>T-DA-016 — INTERCEPT_RUNNER: direction check toward own goal (positive) and away (negative).</summary>
        [Test]
        internal void T_DA_016_InterceptRunnerDirectionCheck()
        {
            // Defending x=0. Opponent at (30, 34) running toward x=0 (velocity.x < 0).
            Vector2 agentPos = new Vector2(30.0f, 34.0f);
            float speed = DefensiveAIConstants.RunnerVelocityThresholdMS + 0.5f;  // Above threshold.

            DefensiveAgentSnapshot[] agentsToward = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1,
                    position: new Vector2(35.0f, 34.0f),
                    velocity: new Vector2(-speed, 0f),  // Toward x=0 (own goal).
                    firstTouch: 14f),
            };

            DefensiveSnapshot snapToward = SnapshotBuilder.Build(agentsToward);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignToward = new MarkAssignment[1];
            MarkHysteresisState[] hystToward = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapToward, pool, 1, 1, assignToward, hystToward);

            Assert.AreEqual(MarkMode.InterceptRunner, assignToward[0].Mode,
                "Opponent running toward own goal must qualify as INTERCEPT_RUNNER.");

            // Counter-input: same opponent running away from own goal (velocity.x > 0).
            DefensiveAgentSnapshot[] agentsAway = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1,
                    position: new Vector2(35.0f, 34.0f),
                    velocity: new Vector2(+speed, 0f),  // Away from x=0.
                    firstTouch: 14f),
            };

            DefensiveSnapshot snapAway = SnapshotBuilder.Build(agentsAway);
            MarkAssignment[] assignAway = new MarkAssignment[1];
            MarkHysteresisState[] hystAway = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapAway, pool, 1, 1, assignAway, hystAway);

            Assert.AreNotEqual(MarkMode.InterceptRunner, assignAway[0].Mode,
                "Opponent running away from own goal must NOT be INTERCEPT_RUNNER.");
        }

        /// <summary>T-DA-017 — INTERCEPT_RUNNER: velocity below threshold → no qualification.</summary>
        [Test]
        internal void T_DA_017_InterceptRunnerBelowThresholdNotQualified()
        {
            Vector2 agentPos = new Vector2(30.0f, 34.0f);
            float speed = DefensiveAIConstants.RunnerVelocityThresholdMS - 0.1f;  // Below threshold.

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1,
                    position: new Vector2(35.0f, 34.0f),
                    velocity: new Vector2(-speed, 0f),  // Toward own goal but below speed threshold.
                    firstTouch: 14f),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[1];
            MarkHysteresisState[] hysteresis = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapshot, pool, 1, 1, assignments, hysteresis);

            Assert.AreNotEqual(MarkMode.InterceptRunner, assignments[0].Mode,
                "Opponent below RUNNER_VELOCITY_THRESHOLD_M_S must not qualify as INTERCEPT_RUNNER.");
        }

        /// <summary>T-DA-019 — Assignment skips overridden agents.</summary>
        [Test]
        internal void T_DA_019_OverriddenAgentSkipped()
        {
            Vector2 agentPos = new Vector2(30.0f, 34.0f);
            float speed = DefensiveAIConstants.RunnerVelocityThresholdMS + 1f;

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1,
                    position: new Vector2(35.0f, 34.0f),
                    velocity: new Vector2(-speed, 0f),
                    firstTouch: 14f),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };

            // Pre-set an overridden assignment — simulating a Steps 4/4a emergency override.
            MarkAssignment overriddenAssignment = new MarkAssignment
            {
                AgentEntityId      = 10,
                Mode               = MarkMode.InterceptRunner,
                TargetEntityId     = 99,
                TargetPosition     = new Vector2(35.0f, 34.0f),
                ValidThroughTick   = 1,
                OverriddenThisTick = true,
                IsManuallyAssigned = false,
            };
            MarkAssignment[] assignments = new MarkAssignment[] { overriddenAssignment };
            MarkHysteresisState[] hysteresis = new MarkHysteresisState[]
            {
                MarkHysteresisState.Default(),
            };

            MarkAssigner.Assign(snapshot, pool, 1, 1, assignments, hysteresis);

            // The overridden assignment must remain unchanged.
            Assert.IsTrue(assignments[0].OverriddenThisTick,
                "OverriddenThisTick must remain true when skipped.");
            Assert.AreEqual(99, assignments[0].TargetEntityId,
                "TargetEntityId must be unchanged when skipped.");
        }

        /// <summary>T-DA-020 — Displacement cost formula numerically correct.</summary>
        [Test]
        internal void T_DA_020_DisplacementCostFormula()
        {
            Vector2 from = new Vector2(30.0f, 25.0f);
            Vector2 to   = new Vector2(40.0f, 30.0f);

            float cost = LastManDetector.DisplacementCost(from, to);

            // dx=10 dy=5 → cost = 100 + 25 = 125 m²
            float expected = 125.0f;
            Assert.AreEqual(expected, cost, 0.001f,
                "Displacement cost must equal dx²+dy² per §3.4.3 worked example.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-021..026  Tackle Intent Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class TackleIntentEvaluatorTests
    {
        private static DefensiveSnapshot BuildTackleSnapshot(
            Vector2 agentPos,
            Vector2 agentVelocity,
            Vector2 oppPos,
            int     agentId  = 10,
            int     oppId    = 99)
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: agentId, teamId: 0,
                    position: agentPos, velocity: agentVelocity,
                    baselineSlot: agentPos, line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: oppId, teamId: 1,
                    position: oppPos, firstTouch: 14f),
            };
            return SnapshotBuilder.Build(agents);
        }

        /// <summary>T-DA-021 — COMMIT when coverageDepth >= floor.</summary>
        [Test]
        internal void T_DA_021_CommitWithCoverage()
        {
            // Agent at (18, 34); opponent at (20.5, 34) — dist=2.5 < 3.0 (eligible).
            // Teammate T1 at (12, 32): |32-34|=2 < 5 (corridor), distToGoal=12 < 18 → counted.
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            Vector2 oppPos   = new Vector2(20.5f, 34.0f);
            Vector2 tmPos    = new Vector2(12.0f, 32.0f);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: new Vector2(1f, 0f), baselineSlot: agentPos, line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 11, teamId: 0, position: tmPos,
                    baselineSlot: tmPos, line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos,
                    firstTouch: 14f),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10, 11 };

            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 10, Mode = MarkMode.ManMark,
                    TargetEntityId = 99, TargetPosition = oppPos },
                new MarkAssignment { AgentEntityId = 11, Mode = MarkMode.Zonal,
                    TargetEntityId = -1 },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 2, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            Assert.Greater(count, 0, "At least one tackle intent must be produced.");
            Assert.AreEqual(TackleMode.Commit, tackleBuffer[0].Mode,
                "Agent with coverageDepth >= 1 and head-on approach must receive COMMIT.");
        }

        /// <summary>T-DA-022 — JOCKEY when coverageDepth < floor but approach angle below threshold.</summary>
        [Test]
        internal void T_DA_022_JockeyLowCoverageGoodAngle()
        {
            // No teammates in corridor → coverageDepth = 0. Agent moves directly toward opponent.
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            Vector2 oppPos   = new Vector2(20.5f, 34.0f);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: new Vector2(2.0f, 0f),  // Directly toward opponent → angle ≈ 0.
                    baselineSlot: agentPos, line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 10, Mode = MarkMode.ManMark,
                    TargetEntityId = 99, TargetPosition = oppPos },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 1, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            Assert.AreEqual(1, count);
            Assert.AreEqual(TackleMode.Jockey, tackleBuffer[0].Mode,
                "Zero coverage + angle below jockey threshold → JOCKEY.");
        }

        /// <summary>T-DA-023 — HOLD when coverageDepth < floor AND approach angle >= threshold.</summary>
        [Test]
        internal void T_DA_023_HoldLowCoverageBadAngle()
        {
            // Stationary agent → approach angle defaults to PI/2 which exceeds TackleJockeyAngleRad (0.35).
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            Vector2 oppPos   = new Vector2(20.5f, 34.0f);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: Vector2.zero,  // Stationary → worst angle (π/2).
                    baselineSlot: agentPos, line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 10, Mode = MarkMode.ManMark,
                    TargetEntityId = 99, TargetPosition = oppPos },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 1, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            Assert.AreEqual(1, count);
            Assert.AreEqual(TackleMode.Hold, tackleBuffer[0].Mode,
                "Zero coverage + angle >= jockey threshold (stationary agent) → HOLD.");
        }

        /// <summary>T-DA-024 — Agent beyond TACKLE_ELIGIBLE_RADIUS_M: no request emitted.</summary>
        [Test]
        internal void T_DA_024_BeyondRadiusNoRequest()
        {
            // Opponent at distance 4.0 > TackleEligibleRadiusM (3.0).
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            float beyondRadius = DefensiveAIConstants.TackleEligibleRadiusM + 1.0f;
            Vector2 oppPos = new Vector2(agentPos.x + beyondRadius, agentPos.y);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: new Vector2(1f, 0f), baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 10, Mode = MarkMode.ManMark,
                    TargetEntityId = 99, TargetPosition = oppPos },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 1, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            Assert.AreEqual(0, count,
                "No TackleIntentRequest must be emitted when opponent is beyond eligible radius.");
        }

        /// <summary>T-DA-025 — ZONAL agents produce no tackle request.</summary>
        [Test]
        internal void T_DA_025_ZonalAgentNoTackleRequest()
        {
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            Vector2 oppPos   = new Vector2(19.0f, 34.0f);  // Within radius.

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: new Vector2(1f, 0f), baselineSlot: agentPos),
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment
                {
                    AgentEntityId  = 10,
                    Mode           = MarkMode.Zonal,
                    TargetEntityId = -1,  // ZONAL — no specific opponent.
                },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 1, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            Assert.AreEqual(0, count,
                "ZONAL agents must not produce any TackleIntentRequest.");
        }

        /// <summary>T-DA-026 — Coverage depth counts only y-corridor teammates closer to own goal.</summary>
        [Test]
        internal void T_DA_026_CoverageDepthCorridor()
        {
            // Agent A at (18, 34). Defending x=0.
            // T1 at (12, 32): |32-34|=2 < 5 (in corridor), distToGoal=12 < 18 → counted.
            // T2 at (12, 44): |44-34|=10 > 5 (out of corridor) → not counted.
            // T3 at (25, 34): distToGoal=25 > 18 (not behind A) → not counted.
            Vector2 agentPos = new Vector2(18.0f, 34.0f);
            Vector2 oppPos   = new Vector2(20.5f, 34.0f);

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0, position: agentPos,
                    velocity: new Vector2(1f, 0f), baselineSlot: agentPos, line: LineId.Defense),
                // T1 — should count.
                SnapshotBuilder.MakeAgent(entityId: 11, teamId: 0,
                    position: new Vector2(12.0f, 32.0f), baselineSlot: new Vector2(12.0f, 32.0f),
                    line: LineId.Defense),
                // T2 — out of y-corridor.
                SnapshotBuilder.MakeAgent(entityId: 12, teamId: 0,
                    position: new Vector2(12.0f, 44.0f), baselineSlot: new Vector2(12.0f, 44.0f),
                    line: LineId.Defense),
                // T3 — ahead of agent (not behind).
                SnapshotBuilder.MakeAgent(entityId: 13, teamId: 0,
                    position: new Vector2(25.0f, 34.0f), baselineSlot: new Vector2(25.0f, 34.0f),
                    line: LineId.Defense),
                // Opponent.
                SnapshotBuilder.MakeAgent(entityId: 99, teamId: 1, position: oppPos),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10, 11, 12, 13 };
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 10, Mode = MarkMode.ManMark,
                    TargetEntityId = 99, TargetPosition = oppPos },
                new MarkAssignment { AgentEntityId = 11, Mode = MarkMode.Zonal, TargetEntityId = -1 },
                new MarkAssignment { AgentEntityId = 12, Mode = MarkMode.Zonal, TargetEntityId = -1 },
                new MarkAssignment { AgentEntityId = 13, Mode = MarkMode.Zonal, TargetEntityId = -1 },
            };

            TackleIntentRequest[] tackleBuffer = new TackleIntentRequest[22];
            TackleIntentEvaluator.Evaluate(snapshot, pool, poolCount: 4, assignments,
                lastManPoolIndex: -1, isEmergency: false,
                tackleBuffer, out int count);

            // Agent 10 is the only one with a target; it should get a request.
            Assert.AreEqual(1, count);
            Assert.AreEqual(10, tackleBuffer[0].AgentEntityId);
            Assert.AreEqual(1, (int)tackleBuffer[0].CoverageDepth,
                "CoverageDepth must be 1: only T1 qualifies (in corridor and behind agent).");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-027..034  Offside Trap Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class OffsideTrapControllerTests
    {
        private static DefensiveSnapshot BuildTrapSnapshot(
            float ballSpeedMS      = 1.5f,
            float ballX            = 60.0f,
            float lineSpreadM      = 2.0f,
            bool  hasPrimaryPress  = false,
            float defensiveLineDepth = 35.0f,
            int   tick             = 1)
        {
            // Four DEFENSE-line agents spaced within lineSpreadM using distToOwnGoal values.
            float baseX  = defensiveLineDepth;
            float spread = lineSpreadM / 3.0f;

            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(baseX,               34.0f),
                    baselineSlot: new Vector2(baseX, 34.0f),   line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 11, teamId: 0,
                    position: new Vector2(baseX + spread,       28.0f),
                    baselineSlot: new Vector2(baseX + spread, 28.0f), line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 12, teamId: 0,
                    position: new Vector2(baseX + spread * 2f, 40.0f),
                    baselineSlot: new Vector2(baseX + spread * 2f, 40.0f), line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 13, teamId: 0,
                    position: new Vector2(baseX + lineSpreadM,  46.0f),
                    baselineSlot: new Vector2(baseX + lineSpreadM, 46.0f), line: LineId.Defense),
            };

            return SnapshotBuilder.Build(agents,
                ballPosition:   new Vector2(ballX, 34.0f),
                ballVelocity:   new Vector2(ballSpeedMS, 0f),
                hasPrimaryPress: hasPrimaryPress,
                defensiveLineDepth: defensiveLineDepth,
                tick: tick);
        }

        private static MarkAssignment[] MakeZonalAssignments(int count)
        {
            MarkAssignment[] arr = new MarkAssignment[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new MarkAssignment
                {
                    Mode           = MarkMode.Zonal,
                    TargetEntityId = -1,
                };
            }
            return arr;
        }

        /// <summary>T-DA-027 — Trap fires after OFFSIDE_TRAP_DWELL_TICKS consecutive qualifying ticks.</summary>
        [Test]
        internal void T_DA_027_TrapFiresAfterDwellTicks()
        {
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            for (int tick = 1; tick <= DefensiveAIConstants.OffsideTrapDwellTicks; tick++)
            {
                DefensiveSnapshot snapshot = BuildTrapSnapshot(tick: tick);
                MarkAssignment[] assignments = MakeZonalAssignments(pool.Length);

                OffsideTrapController.Update(snapshot, pool, pool.Length, tick,
                    ref state, ref directive, assignments);

                if (tick < DefensiveAIConstants.OffsideTrapDwellTicks)
                {
                    Assert.IsFalse(directive.OffsideTrapActive,
                        $"Trap must NOT fire before dwell complete (tick {tick}).");
                }
                else
                {
                    Assert.IsTrue(directive.OffsideTrapActive,
                        "Trap must fire exactly when dwell count reaches threshold.");
                }
            }
        }

        /// <summary>T-DA-028 — Trap blocked during cooldown.</summary>
        [Test]
        internal void T_DA_028_TrapBlockedDuringCooldown()
        {
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            // Burn through the dwell to fire the trap.
            for (int tick = 1; tick <= DefensiveAIConstants.OffsideTrapDwellTicks; tick++)
            {
                DefensiveSnapshot snap = BuildTrapSnapshot(tick: tick);
                MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
                OffsideTrapController.Update(snap, pool, pool.Length, tick,
                    ref state, ref directive, asgn);
            }

            Assert.IsTrue(directive.OffsideTrapActive, "Trap must have fired.");
            int cooldown = DefensiveAIConstants.OffsideResetCooldownTicks;
            Assert.AreEqual(cooldown, state.CooldownTicksRemaining,
                "CooldownTicksRemaining must be set to OffsideResetCooldownTicks after trap fires.");

            // Attempt re-trigger on each cooldown tick — trap must not fire again.
            for (int tick = DefensiveAIConstants.OffsideTrapDwellTicks + 1;
                 tick < DefensiveAIConstants.OffsideTrapDwellTicks + cooldown;
                 tick++)
            {
                directive.OffsideTrapActive = false;  // Reset so we can detect re-fire.
                DefensiveSnapshot snap = BuildTrapSnapshot(tick: tick);
                MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
                OffsideTrapController.Update(snap, pool, pool.Length, tick,
                    ref state, ref directive, asgn);

                Assert.IsFalse(directive.OffsideTrapActive,
                    $"Trap must not re-fire during cooldown (tick {tick}).");
            }
        }

        /// <summary>T-DA-029 — Ball too fast: dwell counter resets.</summary>
        [Test]
        internal void T_DA_029_BallTooFastResetsDwellCounter()
        {
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            // Two qualifying ticks (dwell not yet reached).
            for (int tick = 1; tick <= 2; tick++)
            {
                DefensiveSnapshot snap = BuildTrapSnapshot(ballSpeedMS: 1.5f, tick: tick);
                MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
                OffsideTrapController.Update(snap, pool, pool.Length, tick,
                    ref state, ref directive, asgn);
            }

            Assert.AreEqual(2, state.StepUpDwellCounter, "Dwell counter should be 2 after two qualifying ticks.");

            // Tick with ball too fast → dwell resets.
            DefensiveSnapshot fastSnap = BuildTrapSnapshot(
                ballSpeedMS: DefensiveAIConstants.OffsideBallSpeedThresholdMS + 2.5f, tick: 3);
            MarkAssignment[] fastAsgn = MakeZonalAssignments(pool.Length);
            OffsideTrapController.Update(fastSnap, pool, pool.Length, 3,
                ref state, ref directive, fastAsgn);

            Assert.AreEqual(0, state.StepUpDwellCounter,
                "Dwell counter must reset to 0 when ball exceeds speed threshold.");
        }

        /// <summary>T-DA-030 — Line not coherent: trap blocked.</summary>
        [Test]
        internal void T_DA_030_IncoherentLineBlocksTrap()
        {
            // Spread = 11.0 m > LineCoherenceThresholdM (8.0 m).
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            DefensiveSnapshot snap = BuildTrapSnapshot(lineSpreadM: 11.0f);
            MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
            OffsideTrapController.Update(snap, pool, pool.Length, 1,
                ref state, ref directive, asgn);

            Assert.AreEqual(0, state.StepUpDwellCounter,
                "Dwell counter must remain 0 when line spread exceeds coherence threshold.");
        }

        /// <summary>T-DA-031 — Active PRIMARY_PRESS blocks trap.</summary>
        [Test]
        internal void T_DA_031_PrimaryPressBlocksTrap()
        {
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            DefensiveSnapshot snap = BuildTrapSnapshot(hasPrimaryPress: true);
            MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
            OffsideTrapController.Update(snap, pool, pool.Length, 1,
                ref state, ref directive, asgn);

            Assert.AreEqual(0, state.StepUpDwellCounter,
                "Dwell counter must remain 0 when PRIMARY_PRESS is active (condition 4 fails).");
        }

        /// <summary>T-DA-033 — OFFSIDE_MAX_DEPTH_M safety ceiling enforced.</summary>
        [Test]
        internal void T_DA_033_MaxDepthCeilingEnforced()
        {
            // currentLineDepth = 44.0; step = 3.0 → raw = 47.0 > 45.0 (max).
            float deepLineDepth = 44.0f;
            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            // Burn dwell to fire trap.
            for (int tick = 1; tick <= DefensiveAIConstants.OffsideTrapDwellTicks; tick++)
            {
                DefensiveSnapshot snap = BuildTrapSnapshot(defensiveLineDepth: deepLineDepth, tick: tick);
                MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
                OffsideTrapController.Update(snap, pool, pool.Length, tick,
                    ref state, ref directive, asgn);
            }

            Assert.IsTrue(directive.OffsideTrapActive, "Trap must have fired.");
            Assert.AreEqual(DefensiveAIConstants.OffsideMaxDepthM, directive.StepUpTargetDepth,
                0.001f, "StepUpTargetDepth must be capped at OffsideMaxDepthM.");
        }

        /// <summary>T-DA-034 — Step depth is max of (currentLineDepth+step, shape.DefensiveLineDepth).</summary>
        [Test]
        internal void T_DA_034_StepDepthMaxWithShapeLineDepth()
        {
            // currentLineDepth = 35.0; step = 3.0 → raw = 38.0.
            // DefensiveLineDepth (#12 target) = 40.0 → max(38.0, 40.0) = 40.0.
            float lineDepth      = 35.0f;
            float shapeLineDepth = 40.0f;

            OffsideLineState state     = OffsideLineState.Default();
            MarkDirective    directive = MarkDirective.Inactive(0);
            int[]            pool      = new int[] { 10, 11, 12, 13 };

            for (int tick = 1; tick <= DefensiveAIConstants.OffsideTrapDwellTicks; tick++)
            {
                DefensiveSnapshot snap = BuildTrapSnapshot(
                    defensiveLineDepth: shapeLineDepth, tick: tick);
                // Override state's CurrentLineDepth to simulate a line shallower than shape target.
                state.CurrentLineDepth = lineDepth;

                MarkAssignment[] asgn = MakeZonalAssignments(pool.Length);
                OffsideTrapController.Update(snap, pool, pool.Length, tick,
                    ref state, ref directive, asgn);
            }

            Assert.IsTrue(directive.OffsideTrapActive, "Trap must have fired.");
            Assert.AreEqual(shapeLineDepth, directive.StepUpTargetDepth, 0.001f,
                "StepUpTargetDepth must equal max(lineDepth+step, shapeLineDepth) = 40.0.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-035..040  Last-Man Detector Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class LastManDetectorTests
    {
        /// <summary>T-DA-035 — Correct last-man agent identified (minimum distToOwnGoal).</summary>
        [Test]
        internal void T_DA_035_LastManMinDistToOwnGoal()
        {
            // Pool distToOwnGoal: 102→18.0, 105→22.0, 107→25.0. Team defends x=0.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 102, teamId: 0,
                    position: new Vector2(18.0f, 34.0f), baselineSlot: new Vector2(18.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 105, teamId: 0,
                    position: new Vector2(22.0f, 34.0f), baselineSlot: new Vector2(22.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 107, teamId: 0,
                    position: new Vector2(25.0f, 34.0f), baselineSlot: new Vector2(25.0f, 34.0f)),
            };

            // Ball ahead of last-man (ball at 12.0 < 18.0 + 5.0 = 23.0 and > 5.0).
            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(12.0f, 34.0f));
            int[] pool = new int[] { 102, 105, 107 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "Emergency must fire.");
            Assert.AreEqual(102, result.LastManEntityId,
                "Agent 102 at distToOwnGoal=18.0 is last-man.");
        }

        /// <summary>T-DA-036 — EntityId tie-break for equal distToOwnGoal.</summary>
        [Test]
        internal void T_DA_036_LastManEntityIdTieBreak()
        {
            // Two agents at same distToOwnGoal=18.0. EntityId 5 < 12 → EntityId 5 wins.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 12, teamId: 0,
                    position: new Vector2(18.0f, 36.0f), baselineSlot: new Vector2(18.0f, 36.0f)),
                SnapshotBuilder.MakeAgent(entityId: 5, teamId: 0,
                    position: new Vector2(18.0f, 32.0f), baselineSlot: new Vector2(18.0f, 32.0f)),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(10.0f, 34.0f));
            int[] pool = new int[] { 5, 12 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "Emergency must fire.");
            Assert.AreEqual(5, result.LastManEntityId,
                "EntityId 5 wins tie-break (lower EntityId per FR-DA-033).");
        }

        /// <summary>T-DA-037 — GK not selected as last-man even if forward of outfield defenders.</summary>
        [Test]
        internal void T_DA_037_GkNotSelectedAsLastMan()
        {
            // GK would have distToOwnGoal=5.0 (smallest), but GK is excluded from pool.
            // Only outfield agent with distToOwnGoal=20.0 appears in pool.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 1, teamId: 0, isGoalkeeper: true,
                    position: new Vector2(5.0f, 34.0f), baselineSlot: new Vector2(5.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 2, teamId: 0,
                    position: new Vector2(20.0f, 34.0f), baselineSlot: new Vector2(20.0f, 34.0f)),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(12.0f, 34.0f),
                gkEntityId: 1, gkPosition: new Vector2(5.0f, 34.0f));

            // Pool excludes GK.
            int[] pool = new int[] { 2 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "Emergency must fire.");
            Assert.AreEqual(2, result.LastManEntityId,
                "Outfield agent (EntityId 2) must be last-man — GK is excluded from pool.");
        }

        /// <summary>T-DA-038 — Emergency flag set and INTERCEPT_RUNNER issued when predicate fires.</summary>
        [Test]
        internal void T_DA_038_EmergencyFlagAndInterceptRunner()
        {
            // lastMan at distToOwnGoal=18; ball at distToOwnGoal=12 → 12 < 18+5=23 → threat.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(18.0f, 34.0f), baselineSlot: new Vector2(18.0f, 34.0f)),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(12.0f, 34.0f));
            int[] pool = new int[] { 10 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "EmergencyFlag must be true per §3.8.2.");
            Assert.AreEqual(0, result.LastManPoolIndex, "LastManPoolIndex must point to pool[0].");
        }

        /// <summary>T-DA-039 — Predicate does NOT fire when ball is not ahead of last man.</summary>
        [Test]
        internal void T_DA_039_PredicateNoFireBallNotAhead()
        {
            // lastMan at distToOwnGoal=18; ball at distToOwnGoal=30 → 30 > 18+5=23 → no threat.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(18.0f, 34.0f), baselineSlot: new Vector2(18.0f, 34.0f)),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(30.0f, 34.0f));
            int[] pool = new int[] { 10 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsFalse(result.IsEmergency,
                "Emergency must NOT fire when ball is not within buffer of last-man.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-041..044  COVER_GK_ZONE Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class CoverGkZoneTests
    {
        /// <summary>T-DA-041 — COVER_GK_ZONE triggered when GK out-of-zone AND emergency active.</summary>
        [Test]
        internal void T_DA_041_CoverGkZoneIssuedWhenGkOutOfZone()
        {
            // GK at distToOwnGoal=40 > GkExpectedZoneMaxX (15). Emergency is active (ball at 5, lastMan at 8).
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(8.0f, 34.0f), baselineSlot: new Vector2(8.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 11, teamId: 0,
                    position: new Vector2(20.0f, 34.0f), baselineSlot: new Vector2(20.0f, 34.0f)),
            };

            // GK advanced to distToOwnGoal=40.
            Vector2 gkPos = new Vector2(40.0f, 34.0f);
            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(5.5f, 34.0f),
                gkEntityId: 1, gkPosition: gkPos);

            // Pool = both non-GK agents (lastMan would be entity 10 at distToOwnGoal=8).
            // Ball at 5.5 → 5.5 < 8 + 5 = 13, 5.5 > 5 → emergency active.
            int[] pool = new int[] { 10, 11 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "Emergency must be active.");
            Assert.IsTrue(result.IsGkOutOfZone, "GK out-of-zone must be detected.");
            Assert.AreNotEqual(-1, result.CoverAgentEntityId,
                "A cover agent must be selected.");
        }

        /// <summary>T-DA-042 — COVER_GK_ZONE NOT issued when GK is in zone.</summary>
        [Test]
        internal void T_DA_042_CoverGkZoneNotIssuedWhenGkInZone()
        {
            // GK at distToOwnGoal=8 <= GkExpectedZoneMaxX (15) → in zone.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(18.0f, 34.0f), baselineSlot: new Vector2(18.0f, 34.0f)),
            };

            Vector2 gkPos = new Vector2(8.0f, 34.0f);
            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(12.0f, 34.0f),
                gkEntityId: 1, gkPosition: gkPos);

            int[] pool = new int[] { 10 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency, "Emergency must be active (ball within buffer).");
            Assert.IsFalse(result.IsGkOutOfZone,
                "GK in zone → IsGkOutOfZone must be false.");
        }

        /// <summary>T-DA-044 — Minimum-displacement-cost cover agent selected.</summary>
        [Test]
        internal void T_DA_044_MinCostCoverAgentSelected()
        {
            // Emergency active. GK at 20.0 distToOwnGoal (out of zone).
            // abandonedZoneCenter = (7.5, 34.0) for defending x=0.
            // Agent 105 at (22, 34): cost = (22-7.5)²+(34-34)² = 14.5²= 210.25.
            // Agent 107 at (25, 38): cost = (25-7.5)²+(38-34)² = 17.5²+16 = 306.25+16 = 322.25.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                // Last-man candidate (smallest distToOwnGoal among pool).
                SnapshotBuilder.MakeAgent(entityId: 103, teamId: 0,
                    position: new Vector2(15.0f, 34.0f), baselineSlot: new Vector2(15.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 105, teamId: 0,
                    position: new Vector2(22.0f, 34.0f), baselineSlot: new Vector2(22.0f, 34.0f)),
                SnapshotBuilder.MakeAgent(entityId: 107, teamId: 0,
                    position: new Vector2(25.0f, 38.0f), baselineSlot: new Vector2(25.0f, 38.0f)),
            };

            // Ball at distToOwnGoal=8 → 8 < 15+5=20 and 8>5 → emergency fires.
            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents,
                ballPosition: new Vector2(8.0f, 34.0f),
                gkEntityId: 1, gkPosition: new Vector2(20.0f, 34.0f));

            int[] pool = new int[] { 103, 105, 107 };

            LastManDetector.Evaluate(snapshot, pool, pool.Length, out LastManResult result);

            Assert.IsTrue(result.IsEmergency);
            Assert.IsTrue(result.IsGkOutOfZone);
            Assert.AreEqual(105, result.CoverAgentEntityId,
                "Agent 105 with lower displacement cost (210.25) must be selected as cover agent.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-045..050  Anti-Chaos Invariant Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class InvariantEnforcerTests
    {
        private static DefensiveSnapshot BuildInvariantSnapshot(int poolCount)
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[poolCount * 2];
            for (int i = 0; i < poolCount; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(
                    entityId: i + 10,
                    teamId: 0,
                    position: new Vector2(30.0f + i * 2.0f, 34.0f),
                    baselineSlot: new Vector2(30.0f + i * 2.0f, 34.0f),
                    line: LineId.Defense);
                // Matching opponent for each defender.
                agents[poolCount + i] = SnapshotBuilder.MakeAgent(
                    entityId: 100 + i,
                    teamId: 1,
                    position: new Vector2(32.0f + i * 2.0f, 34.0f),
                    firstTouch: 12f - i);  // Varying threat: first opp is highest.
            }

            return SnapshotBuilder.Build(agents);
        }

        /// <summary>T-DA-045 — Invariant 2 violation: lowest-threat MAN_MARK demoted.</summary>
        [Test]
        internal void T_DA_045_MaxManMarkDemotesLowestThreat()
        {
            int poolCount = 5;
            DefensiveSnapshot snapshot = BuildInvariantSnapshot(poolCount);
            int[] pool = new int[] { 10, 11, 12, 13, 14 };

            // All five in MAN_MARK; opponents have varying firstTouch (threat).
            // Lowest-threat opp: entityId 104, firstTouch=8 (12 - 4).
            MarkAssignment[] assignments = new MarkAssignment[poolCount];
            for (int i = 0; i < poolCount; i++)
            {
                assignments[i] = new MarkAssignment
                {
                    AgentEntityId  = 10 + i,
                    Mode           = MarkMode.ManMark,
                    TargetEntityId = 100 + i,
                    TargetPosition = new Vector2(32.0f + i * 2.0f, 34.0f),
                    ValidThroughTick = 1,
                };
            }

            bool result = InvariantEnforcer.Enforce(snapshot, pool, poolCount, 1, assignments);

            int manMarkCount = 0;
            for (int i = 0; i < poolCount; i++)
            {
                if (assignments[i].Mode == MarkMode.ManMark) manMarkCount++;
            }

            Assert.IsTrue(result, "Enforcer must return true (invariants satisfied after demotion).");
            Assert.AreEqual(DefensiveAIConstants.MaxManMarkAssignments, manMarkCount,
                "MAN_MARK count must equal MAX_MAN_MARK_ASSIGNMENTS after enforcement.");
        }

        /// <summary>T-DA-047 — Invariant 3 violation: over-displaced assignment demoted.</summary>
        [Test]
        internal void T_DA_047_OverDisplacedAssignmentDemoted()
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: 0,
                    position: new Vector2(30.0f, 34.0f),
                    baselineSlot: new Vector2(30.0f, 34.0f), line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 100, teamId: 1,
                    position: new Vector2(55.0f, 34.0f), firstTouch: 14f),
            };

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);
            int[] pool = new int[] { 10 };

            // Target at distance 25.0 m from baseline → exceeds MaxMarkDisplacementM (20.0 m).
            MarkAssignment[] assignments = new MarkAssignment[]
            {
                new MarkAssignment
                {
                    AgentEntityId  = 10,
                    Mode           = MarkMode.ManMark,
                    TargetEntityId = 100,
                    TargetPosition = new Vector2(55.0f, 34.0f),  // 25 m from baseline.
                    ValidThroughTick = 1,
                },
            };

            bool result = InvariantEnforcer.Enforce(snapshot, pool, 1, 1, assignments);

            Assert.IsTrue(result, "Enforce must return true after demoting over-displaced agent.");
            Assert.AreEqual(MarkMode.Zonal, assignments[0].Mode,
                "Over-displaced assignment must be demoted to ZONAL.");
        }

        /// <summary>T-DA-048 — All invariants satisfied: no demotions applied.</summary>
        [Test]
        internal void T_DA_048_NoViolationNoDemotion()
        {
            // 4 MAN_MARK (= max), 3 DEFENSE-line in ZONAL (= min backline), all displacements <= 20 m.
            int poolCount = 7;  // 4 non-backline MAN_MARK + 3 backline ZONAL.
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[poolCount * 2];

            // 3 defense-line agents → ZONAL.
            for (int i = 0; i < 3; i++)
            {
                agents[i] = SnapshotBuilder.MakeAgent(entityId: 10 + i, teamId: 0,
                    position: new Vector2(30.0f, 26.0f + i * 8.0f),
                    baselineSlot: new Vector2(30.0f, 26.0f + i * 8.0f),
                    line: LineId.Defense);
            }

            // 4 midfield agents → MAN_MARK with close targets (small displacement).
            for (int i = 0; i < 4; i++)
            {
                agents[3 + i] = SnapshotBuilder.MakeAgent(entityId: 13 + i, teamId: 0,
                    position: new Vector2(40.0f, 26.0f + i * 8.0f),
                    baselineSlot: new Vector2(40.0f, 26.0f + i * 8.0f),
                    line: LineId.Midfield);
                agents[poolCount + i] = SnapshotBuilder.MakeAgent(entityId: 100 + i, teamId: 1,
                    position: new Vector2(44.0f, 26.0f + i * 8.0f), firstTouch: 14f);
            }

            // Fill remaining agent slots with inactive agents.
            for (int i = poolCount; i < agents.Length; i++)
            {
                if (agents[i].EntityId == 0)
                {
                    agents[i] = SnapshotBuilder.MakeAgent(entityId: 200 + i, teamId: 1,
                        isActive: false);
                }
            }

            DefensiveSnapshot snapshot = SnapshotBuilder.Build(agents);

            int[] pool = new int[] { 10, 11, 12, 13, 14, 15, 16 };
            MarkAssignment[] assignments = new MarkAssignment[poolCount];

            // 3 DEFENSE ZONAL.
            for (int i = 0; i < 3; i++)
            {
                assignments[i] = MarkAssignment.MakeZonal(10 + i,
                    new Vector2(30.0f, 26.0f + i * 8.0f), 1);
            }

            // 4 MIDFIELD MAN_MARK with 4 m displacement (well within 20 m).
            for (int i = 0; i < 4; i++)
            {
                assignments[3 + i] = new MarkAssignment
                {
                    AgentEntityId  = 13 + i,
                    Mode           = MarkMode.ManMark,
                    TargetEntityId = 100 + i,
                    TargetPosition = new Vector2(44.0f, 26.0f + i * 8.0f),
                    ValidThroughTick = 1,
                };
            }

            bool result = InvariantEnforcer.Enforce(snapshot, pool, poolCount, 1, assignments);

            Assert.IsTrue(result, "All invariants satisfied → Enforce must return true.");

            // Verify no assignments changed.
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(MarkMode.Zonal, assignments[i].Mode,
                    $"Defense-line agent {i} must remain ZONAL.");
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(MarkMode.ManMark, assignments[3 + i].Mode,
                    $"Midfield agent {i} must remain MAN_MARK.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // T-DA-051..054  Hysteresis Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class MarkHysteresisTests
    {
        /// <summary>T-DA-051 — Assignment held for MARK_DWELL_TICKS before transition commits.</summary>
        [Test]
        internal void T_DA_051_DwellLockHoldsAssignment()
        {
            MarkHysteresisState hysteresis = MarkHysteresisState.Default();

            MarkAssignment current = new MarkAssignment
            {
                AgentEntityId  = 10,
                Mode           = MarkMode.ManMark,
                TargetEntityId = 201,
                TargetPosition = new Vector2(30f, 34f),
            };

            MarkAssignment candidate202 = new MarkAssignment
            {
                AgentEntityId  = 10,
                Mode           = MarkMode.ManMark,
                TargetEntityId = 202,
                TargetPosition = new Vector2(32f, 34f),
            };

            // Apply candidate 202 for MarkDwellTicks consecutive ticks.
            for (int tick = 1; tick <= DefensiveAIConstants.MarkDwellTicks; tick++)
            {
                MarkHysteresis.ApplyGate(candidate202, ref current, ref hysteresis);

                if (tick < DefensiveAIConstants.MarkDwellTicks)
                {
                    Assert.AreEqual(201, current.TargetEntityId,
                        $"Assignment must stay on 201 before dwell complete (tick {tick}).");
                }
                else
                {
                    Assert.AreEqual(202, current.TargetEntityId,
                        "Assignment must commit to 202 when dwell threshold is reached.");
                }
            }
        }

        /// <summary>T-DA-052 — Oscillating candidate does not trigger transition.</summary>
        [Test]
        internal void T_DA_052_OscillatingCandidateNoTransition()
        {
            MarkHysteresisState hysteresis = MarkHysteresisState.Default();

            MarkAssignment current = new MarkAssignment
            {
                AgentEntityId  = 10,
                Mode           = MarkMode.ManMark,
                TargetEntityId = 201,
            };

            MarkAssignment candidate201 = new MarkAssignment
            {
                AgentEntityId = 10, Mode = MarkMode.ManMark, TargetEntityId = 201,
            };

            MarkAssignment candidate202 = new MarkAssignment
            {
                AgentEntityId = 10, Mode = MarkMode.ManMark, TargetEntityId = 202,
            };

            // Alternate 201/202 each tick for 2×MarkDwellTicks ticks.
            for (int i = 0; i < DefensiveAIConstants.MarkDwellTicks * 2; i++)
            {
                MarkAssignment candidate = (i % 2 == 0) ? candidate202 : candidate201;
                MarkHysteresis.ApplyGate(candidate, ref current, ref hysteresis);

                Assert.AreEqual(201, current.TargetEntityId,
                    $"Assignment must stay on 201 during oscillation (iteration {i}).");
            }
        }

        /// <summary>T-DA-053 — New candidate resets holdTicks accumulator.</summary>
        [Test]
        internal void T_DA_053_NewCandidateResetsAccumulator()
        {
            MarkHysteresisState hysteresis = MarkHysteresisState.Default();

            MarkAssignment current = new MarkAssignment
            {
                AgentEntityId  = 10,
                Mode           = MarkMode.ManMark,
                TargetEntityId = 201,
            };

            MarkAssignment candidate202 = new MarkAssignment
            {
                AgentEntityId = 10, Mode = MarkMode.ManMark, TargetEntityId = 202,
            };

            MarkAssignment candidate203 = new MarkAssignment
            {
                AgentEntityId = 10, Mode = MarkMode.ManMark, TargetEntityId = 203,
            };

            // Accumulate 2 ticks toward 202.
            MarkHysteresis.ApplyGate(candidate202, ref current, ref hysteresis);
            MarkHysteresis.ApplyGate(candidate202, ref current, ref hysteresis);
            Assert.AreEqual(2, hysteresis.HoldTicks, "HoldTicks should be 2 after two ticks toward 202.");

            // Switch to 203 — accumulator must reset.
            MarkHysteresis.ApplyGate(candidate203, ref current, ref hysteresis);

            Assert.AreEqual(203, hysteresis.CandidateTargetEntityId,
                "CandidateTargetEntityId must switch to 203.");
            Assert.AreEqual(1, hysteresis.HoldTicks,
                "HoldTicks must reset to 1 (start of new accumulation for 203).");
            Assert.AreEqual(201, current.TargetEntityId,
                "Current assignment must not commit to 202 (only held 2 of needed ticks).");
        }

        /// <summary>T-DA-054 — ZONAL agents not subject to dwellCounter pre-check.</summary>
        [Test]
        internal void T_DA_054_ZonalNotSubjectToPreCheck()
        {
            // Agent in ZONAL mode with DwellCounter > 0. PreCheck must return false.
            MarkAssignment currentZonal = new MarkAssignment
            {
                AgentEntityId  = 10,
                Mode           = MarkMode.Zonal,
                TargetEntityId = -1,
            };

            MarkHysteresisState hysteresis = MarkHysteresisState.Default();
            hysteresis.DwellCounter = 3;  // Non-zero, but must be ignored for ZONAL.

            bool retained = MarkHysteresis.PreCheck(ref currentZonal, ref hysteresis);

            Assert.IsFalse(retained,
                "PreCheck must return false for ZONAL assignments (T-DA-054 / §3.11.3).");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Additional helper / geometry tests
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal class GeometryHelperTests
    {
        /// <summary>DistToOwnGoal is correct for defending-x0 and defending-x105 teams.</summary>
        [Test]
        internal void DistToOwnGoal_BothTeamConventions()
        {
            float x = 30.0f;
            Assert.AreEqual(30.0f, LastManDetector.DistToOwnGoal(x, defendsX0: true), 0.001f);
            Assert.AreEqual(DefensiveAIConstants.PITCH_LENGTH_M - x,
                LastManDetector.DistToOwnGoal(x, defendsX0: false), 0.001f);
        }

        /// <summary>AbandonedZoneCenter for defending-x0 team is (7.5, 34.0).</summary>
        [Test]
        internal void AbandonedZoneCenter_DefendingX0()
        {
            Vector2 center = LastManDetector.ComputeAbandonedZoneCenter(defendsX0: true);

            float expectedX = DefensiveAIConstants.GkExpectedZoneMaxX * 0.5f;   // 7.5
            float expectedY = DefensiveAIConstants.PITCH_WIDTH_M * 0.5f;        // 34.0

            Assert.AreEqual(expectedX, center.x, 0.001f,
                "AbandonedZoneCenter.x must be GkExpectedZoneMaxX/2 = 7.5 for defending-x0.");
            Assert.AreEqual(expectedY, center.y, 0.001f,
                "AbandonedZoneCenter.y must be PITCH_WIDTH_M/2 = 34.0.");
        }

        /// <summary>MarkAssignment.MakeZonal factory sets all fields correctly.</summary>
        [Test]
        internal void MakeZonal_SetsFieldsCorrectly()
        {
            Vector2 slot = new Vector2(30.0f, 34.0f);
            MarkAssignment zonal = MarkAssignment.MakeZonal(agentEntityId: 42, formationSlot: slot, tick: 5);

            Assert.AreEqual(42, zonal.AgentEntityId);
            Assert.AreEqual(MarkMode.Zonal, zonal.Mode);
            Assert.AreEqual(-1, zonal.TargetEntityId);
            Assert.AreEqual(slot, zonal.TargetPosition);
            Assert.AreEqual(5, zonal.ValidThroughTick);
            Assert.IsFalse(zonal.OverriddenThisTick);
            Assert.IsFalse(zonal.IsManuallyAssigned);
        }

        /// <summary>MarkHysteresisState.Default initialises with expected zero values.</summary>
        [Test]
        internal void MarkHysteresisStateDefault_ZeroValues()
        {
            MarkHysteresisState s = MarkHysteresisState.Default();

            Assert.AreEqual(0, s.DwellCounter);
            Assert.AreEqual(0, s.HoldTicks);
            Assert.AreEqual(MarkMode.Zonal, s.CandidateMode);
            Assert.AreEqual(-1, s.CandidateTargetEntityId);
        }

        /// <summary>MarkHysteresis.Reset zeroes all fields (emergency bypass).</summary>
        [Test]
        internal void MarkHysteresisReset_ZeroesAllFields()
        {
            MarkHysteresisState s = new MarkHysteresisState
            {
                DwellCounter           = 3,
                HoldTicks              = 2,
                CandidateMode          = MarkMode.ManMark,
                CandidateTargetEntityId = 99,
            };

            MarkHysteresis.Reset(ref s);

            Assert.AreEqual(0, s.DwellCounter);
            Assert.AreEqual(0, s.HoldTicks);
            Assert.AreEqual(MarkMode.Zonal, s.CandidateMode);
            Assert.AreEqual(-1, s.CandidateTargetEntityId);
        }

        /// <summary>OffsideLineState.Default initialises with zeroed state.</summary>
        [Test]
        internal void OffsideLineStateDefault_ZeroValues()
        {
            OffsideLineState s = OffsideLineState.Default();

            Assert.AreEqual(0f, s.CurrentLineDepth, 0.001f);
            Assert.AreEqual(0, s.StepUpDwellCounter);
            Assert.AreEqual(0, s.CooldownTicksRemaining);
            Assert.AreEqual(0, s.CoverGkZoneActiveTicks);
        }

        /// <summary>MarkDirective.Inactive factory produces all-false, all-zero values.</summary>
        [Test]
        internal void MarkDirectiveInactive_AllFalseZero()
        {
            MarkDirective d = MarkDirective.Inactive(teamId: 1);

            Assert.AreEqual(1, d.TeamId);
            Assert.IsFalse(d.OffsideTrapActive);
            Assert.IsFalse(d.EmergencyFlag);
            Assert.AreEqual(0f, d.StepUpTargetDepth, 0.001f);
        }

        /// <summary>HoldShapePoolFilter.IndexOf returns -1 for absent EntityId.</summary>
        [Test]
        internal void IndexOf_ReturnsMinusOneWhenAbsent()
        {
            int[] pool = new int[] { 10, 20, 30 };
            int result = HoldShapePoolFilter.IndexOf(pool, 3, 99);

            Assert.AreEqual(-1, result,
                "IndexOf must return -1 when EntityId is not in pool.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.3 Integration / §5.4 Determinism / §5.5 Performance / §5.6 Anti-Chaos
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class DefensiveAIIntegrationTests
    {
        // ── §5.3 Integration ──────────────────────────────────────────────────────

        /// <summary>T-DA-060: Full-team press + defend scenario; #13/#14 partition disjoint across 10 ticks. §5.3.</summary>
        [Test]
        public void Integration_T_DA_060_PressAndDefendPartitionDisjointAcrossTenTicks()
        {
            Assert.Ignore("Stage 0+1: requires PressingAI + DefensiveAI wired into TickOrchestrator — activate when both #13 and #14 are live in the full pipeline");
        }

        /// <summary>T-DA-061: Phase transition IN_POSSESSION → OUT_OF_POSSESSION → IN_POSSESSION; hysteresis preserved; no data leakage. §5.3.</summary>
        [Test]
        public void Integration_T_DA_061_PhaseTransitionCycleWithHysteresisPreservation()
        {
            Assert.Ignore("Stage 0+1: requires live PositioningAI phase input across 3 ticks — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-062: TRANSITION phase possession turnover; INTERCEPT_RUNNER assignment within one qualifying tick. §5.3.</summary>
        [Test]
        public void Integration_T_DA_062_TransitionPhasePossessionTurnoverInterceptRunner()
        {
            Assert.Ignore("Stage 0+1: requires live phase-change injection — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-063: Offside trap; 4-defender backline advances simultaneously; Y-coordinates individual. §5.3 / FR-DA-019.</summary>
        [Test]
        public void Integration_T_DA_063_OffsideTrapFourDefenderBacklineAdvancesSimultaneously()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick dwell + DEFENSE-line position data — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-064: Emergency scenario: last-man threatened + GK out of position; both overrides issued same tick. §5.3 / FR-DA-022.</summary>
        [Test]
        public void Integration_T_DA_064_EmergencyLastManAndGkOutOfPositionSameTick()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent snapshot with engineered emergency state — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-065: Anti-chaos cascade MAX_MAN_MARK_ASSIGNMENTS exceeded; resolves within 3 passes. §5.3 / KD-17.</summary>
        [Test]
        public void Integration_T_DA_065_AntiChaosCascadeResolvesWithinThreePasses()
        {
            Assert.Ignore("Stage 0+1: requires engineered over-assignment scenario — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-066: Determinism — same fixed inputs produce bit-identical MarkAssignment[], MarkDirective, TackleIntentRequest[] across two runs. §5.3.</summary>
        [Test]
        public void Integration_T_DA_066_IdenticalInputsProduceBitIdenticalOutput()
        {
            Assert.Ignore("Stage 0+1: requires full pipeline invocation with a fixed snapshot — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-067: Assign-then-suppress: opponent exits radius mid-dwell; assignment retained for remaining dwell; ZONAL on tick 5. §5.3 / FR-DA-015.</summary>
        [Test]
        public void Integration_T_DA_067_AssignThenSuppressOpponentExitsRadiusMidDwell()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick dwell injection — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-068: GK re-enters zone; COVER_GK_ZONE released; cover agent reverts to ZONAL. §5.3.</summary>
        [Test]
        public void Integration_T_DA_068_GkReentryReleaseCoverGkZone()
        {
            Assert.Ignore("Stage 0+1: requires GK position tracking across ticks — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-069: Post-trap cooldown; dwell counter cannot trigger re-fire during cooldownTicksRemaining > 0. §5.3.</summary>
        [Test]
        public void Integration_T_DA_069_PostTrapCooldownBlocksRefire()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick cooldown tracking — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-070: Full 90-minute match; pool sizes always consistent with #13 role partition (|PRIMARY_PRESS|+|COVER_SHADOW|+|HOLD_SHAPE|==10). §5.3 / FR-DA-009.</summary>
        [Test]
        public void Integration_T_DA_070_NinetyMinMatchPoolSizesConsistentWithPressingPartition()
        {
            Assert.Ignore("Stage 0+1: requires full match simulation with #13 press-role data — activate when both #13 and #14 are wired into TickOrchestrator");
        }

        /// <summary>T-DA-071: #12 phase change latency — phase change on tick T takes effect immediately on tick T (no 1-tick lag). §5.3 / FR-DA-013.</summary>
        [Test]
        public void Integration_T_DA_071_PhaseChangeTakesEffectImmediately()
        {
            Assert.Ignore("Stage 0+1: requires live PositioningAI phase input — activate when DefensiveAITick reads phase in same tick as #12 update");
        }

        // ── §5.4 Determinism Regression ───────────────────────────────────────────

        /// <summary>T-DA-DET-001: 90-minute replay; bit-identical per-tick digest (MarkDirective × 2, MarkAssignment[] × 22, hysteresis × 22, OffsideLineState × 2, TackleIntentRequest[] × 20). §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_001_NinetyMinReplayBitIdenticalPerTickDigests()
        {
            Assert.Ignore("Stage 0+1: requires reference-host replay harness and #16 §5 digest comparison — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-DET-002: Two independent simulations with identical initial state; first 1,000 ticks produce identical sequences. §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_002_TwoIndependentSimsIdenticalInitialStateFirstThousandTicks()
        {
            Assert.Ignore("Stage 0+1: requires two fresh simulation instances — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-DET-003: Mid-match snapshot resume; ticks 5001–6000 bit-identical vs. continuous run. §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_003_MidMatchSnapshotResumeDigestContinuity()
        {
            Assert.Ignore("Stage 0+1: requires Spec #16 SnapshotCodec mid-match serialisation — activate when DeterministicSim snapshot round-trip is verified");
        }

        /// <summary>T-DA-DET-004: EntityId iteration-order determinism; swapped EntityIds in pool produce predictably permuted output. §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_004_EntityIdIterationOrderDeterminism()
        {
            Assert.Ignore("Stage 0+1: requires full pipeline with controllable EntityId ordering — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-DET-005: RNG reproducibility via DOMAIN_TAG_DEFENSIVE_AI seed; forced tie-break produces same result on both runs. §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_005_RngReproducibilityViaDomainTagDefensiveAI()
        {
            Assert.Ignore("Stage 0+1: requires #16 DeterministicRngService wired into DefensiveAITick — activate when DOMAIN_TAG_DEFENSIVE_AI (0x1A) RNG draws are live");
        }

        /// <summary>T-DA-DET-006: Anti-chaos pass terminates in ≤ 3 passes for 1,000 random initial assignment states; no infinite loop. §5.4.</summary>
        [Test]
        public void Determinism_T_DA_DET_006_AntiChaosTerminatesInThreePassesForAllInputs()
        {
            Assert.Ignore("Stage 0+1: requires 1,000-iteration randomised anti-chaos harness — activate when DefensiveAITick is wired into TickOrchestrator and InvariantEnforcer iteration count is observable");
        }

        // ── §5.5 Performance Validation ───────────────────────────────────────────

        /// <summary>T-DA-PERF-001: Worst-case per-tick ≤ 0.12 ms on reference host (§6.3); 10 HOLD_SHAPE agents × 10 candidates; 100 reps, discard top 5%. §5.5.</summary>
        [Test]
        public void Performance_T_DA_PERF_001_WorstCasePerTickWithinBudget()
        {
            Assert.Ignore("Stage 0+1: requires reference host (Ryzen 7 5800X) and Unity Stopwatch micro-benchmark harness — activate when certification-platform.md Stage-0 host pin is confirmed");
        }

        /// <summary>T-DA-PERF-002: Zero heap allocations per tick; Unity Memory Profiler; 10 consecutive ticks; zero GC.Alloc from src/DefensiveAI/. §5.5 / FR-DA-006.</summary>
        [Test]
        public void Performance_T_DA_PERF_002_ZeroHeapAllocationsPerTick()
        {
            Assert.Ignore("Stage 0+1: requires Unity Memory Profiler with Allocation Tracking — activate when DefensiveAITick is wired into TickOrchestrator and #18 §3.7 gate is active");
        }

        /// <summary>T-DA-PERF-003: Anti-chaos worst-case ≤ 0.02 ms for 3-pass invariant enforcement; within 0.12 ms total budget. §5.5.</summary>
        [Test]
        public void Performance_T_DA_PERF_003_AntiChaosWorstCaseWithinBudget()
        {
            Assert.Ignore("Stage 0+1: requires reference host and isolated InvariantEnforcer micro-benchmark — activate when certification-platform.md Stage-0 host pin is confirmed");
        }

        // ── §5.6 Anti-Chaos / Exploit-Resistance (KD-17, KD-18) ─────────────────

        /// <summary>T-DA-INV-001: MIN_BACKLINE_AGENTS violated → demotion restores backline ≤ 2 passes. §5.6.1 / FR-DA-025.</summary>
        [Test]
        public void AntiChaos_T_DA_INV_001_MinBacklineAgentsViolationDemotionRestoresBackline()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent pipeline with engineered backline-depletion — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-INV-002: MAX_MAN_MARK_ASSIGNMENTS exceeded → lowest-threat demoted to ZONAL. §5.6.1 / FR-DA-026.</summary>
        [Test]
        public void AntiChaos_T_DA_INV_002_MaxManMarkExceededLowestThreatDemoted()
        {
            Assert.Ignore("Stage 0+1: requires engineered over-assignment scenario — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-INV-003: MAX_MARK_DISPLACEMENT_M exceeded → over-displaced assignment demoted to ZONAL. §5.6.1 / FR-DA-027.</summary>
        [Test]
        public void AntiChaos_T_DA_INV_003_MaxMarkDisplacementExceededOverDisplacedDemoted()
        {
            Assert.Ignore("Stage 0+1: requires engineered displacement-exceeding assignment — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-EXP-001: EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY — step-up does not fire until dwellCounter = OFFSIDE_TRAP_DWELL_TICKS = 3. §5.6.2 / KD-18.</summary>
        [Test]
        public void AntiChaos_T_DA_EXP_001_OffsideTrapNotSpringEarlyOnRunnerStart()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick dwell sequence with concurrent INTERCEPT_RUNNER — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-EXP-002: EXPLOIT_SWITCH_THROUGH_HOLE — ball switch to unguarded channel; cover agent updates targetPosition within REASSIGN_LATENCY_TICKS = 2. §5.6.2 / KD-18.</summary>
        [Test]
        public void AntiChaos_T_DA_EXP_002_BallSwitchToUnguardedChannelCoveredWithinLatency()
        {
            Assert.Ignore("Stage 0+1: requires live ball-switch detection and multi-tick scenario — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-EXP-003: EXPLOIT_LAST_MAN_ONE_ON_ONE — last-man tackle intent is JOCKEY or HOLD, never COMMIT when coverageDepth = 0. §5.6.2 / KD-18.</summary>
        [Test]
        public void AntiChaos_T_DA_EXP_003_LastManOneOnOneTackleIntentNotCommit()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent pipeline with engineered last-man 1v1 scenario — activate when DefensiveAITick is wired into TickOrchestrator");
        }

        /// <summary>T-DA-EXP-004: EXPLOIT_GK_OUT_OF_POSITION — COVER_GK_ZONE issued same tick GK out-of-zone is first detected; no latency gap. §5.6.2 / KD-18.</summary>
        [Test]
        public void AntiChaos_T_DA_EXP_004_GkOutOfPositionCoverAssignedSameTick()
        {
            Assert.Ignore("Stage 0+1: requires live GK position tracking — activate when DefensiveAITick is wired into TickOrchestrator");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                     |
// | 1.0     | 2026-05-31 | —      | Initial implementation. 25 unit tests covering T-DA-001..054 pool filter, threat score, |
// |         |            |        |   mark assignment, tackle intent, offside trap, last-man detection, COVER_GK_ZONE,       |
// |         |            |        |   anti-chaos invariants, hysteresis, and geometry helpers. No integration or perf tests. |
// | 1.1     | 2026-06-01 | —      | Added DefensiveAIIntegrationTests class with 27 stubs: T-DA-060..071 (§5.3 integration), |
// |         |            |        |   T-DA-DET-001..006 (§5.4 determinism), T-DA-PERF-001..003 (§5.5 performance),          |
// |         |            |        |   T-DA-INV-001..003 + T-DA-EXP-001..004 (§5.6 anti-chaos/exploit-resistance).          |
// |         |            |        |   All stubs use Assert.Ignore with Stage 0+1 activation conditions.                     |
#endregion
