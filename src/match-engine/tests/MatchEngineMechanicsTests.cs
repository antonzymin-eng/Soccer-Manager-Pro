// File:     src/match-engine/tests/MatchEngineMechanicsTests.cs
// Created:  2026-06-22
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase D (D2), Code Standards #20
// Purpose:  Phase D step D2 tests — proves the mechanics AI feeds live decision-context inputs: the
//           Positioning AI (#12) formation slots (D2a, incl. the away-team ERR-008-002 mirror) and the
//           Defensive (#14) / Attacking (#15) carriers (D2b), and that the wiring is deterministic.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase D step D2 mechanics-AI wiring tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineMechanicsTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // Roster layout (MatchEngine.InitializeKickoffState): index 0 of each team is the goalkeeper;
        // home team is agents 0..10, away team is agents 11..21.
        private const int HomeGoalkeeper = 0;
        private const int AwayGoalkeeper = MatchEngineConstants.PLAYERS_PER_TEAM;       // 11
        private const int HomeDefender   = 1;                                           // slot 1 (LB), deep
        private const int HomeStriker    = 9;                                           // slot 9 (ST), advanced

        // Two stride ticks (12 = 2 × AI_PHASE_STRIDE) so the AI phase has run and refreshed slots.
        private static readonly int TwoStrideTicks = 2 * DeterministicSimConstants.AI_PHASE_STRIDE;

        private static MatchEngine RunTo(int ticks)
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
            }
            return engine;
        }

        /// <summary>As <see cref="RunTo"/>, but clears possession before every tick so the world stays
        /// left/right symmetric — the precondition of the home↔away carrier mirror lock (§5.Z Phase H
        /// made the kickoff itself asymmetric by awarding it to one side).</summary>
        private static MatchEngine RunToWithBallLoose(int ticks)
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < ticks; i++)
            {
                engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
                engine.RunTick();
            }
            return engine;
        }

        [Test]
        public void PositioningAI_FeedsFormationSlots_IntoDecisionContext()
        {
            MatchEngine engine = RunTo(TwoStrideTicks);

            Vector2 defenderSlot = engine.TestOnly_FormationSlot(HomeDefender);
            Vector2 strikerSlot  = engine.TestOnly_FormationSlot(HomeStriker);

            // Slots are real on-pitch points (not the SENTINEL_NO_SLOT −∞ fallback or NaN).
            foreach (Vector2 slot in new[] { defenderSlot, strikerSlot })
            {
                Assert.IsTrue(float.IsFinite(slot.x) && float.IsFinite(slot.y),
                    "Formation slot must be a finite on-pitch position.");
                Assert.GreaterOrEqual(slot.x, 0f, "Formation slot X must be inside the pitch length.");
                Assert.LessOrEqual(slot.x, MatchEngineConstants.PITCH_LENGTH_M,
                    "Formation slot X must be inside the pitch length.");
                Assert.GreaterOrEqual(slot.y, 0f, "Formation slot Y must be inside the pitch width.");
                Assert.LessOrEqual(slot.y, MatchEngineConstants.PITCH_WIDTH_M,
                    "Formation slot Y must be inside the pitch width.");
            }

            // The home team attacks +X: its defender sits in the home half and its striker in the
            // attacking half — proving real formation shape (not the kickoff scaffold line) feeds the DT.
            float midline = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            Assert.Less(defenderSlot.x, midline,
                "Home defender formation slot must be in the home half (#12 places defenders deep).");
            Assert.Greater(strikerSlot.x, midline,
                "Home striker formation slot must be in the attacking half (#12 advances forwards).");
        }

        [Test]
        public void AwayTeamFormationSlots_MirrorHomeTeam()
        {
            // The #12 formation table is authored attack-toward-+X; the host maps the away team into that
            // canonical frame and back (180° pitch rotation). With the ball on the centre spot, both GK
            // slots are computed from the same centre-symmetric input, so the away GK slot must be the
            // exact pitch-mirror of the home GK slot. Locks the ERR-008-002 home/away-asymmetry guard.
            MatchEngine engine = RunTo(TwoStrideTicks);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(HomeGoalkeeper) && engine.TestOnly_IsGoalkeeper(AwayGoalkeeper),
                "Roster indices 0 and 11 must be the goalkeepers.");

            Vector2 homeGk = engine.TestOnly_FormationSlot(HomeGoalkeeper);
            Vector2 awayGk = engine.TestOnly_FormationSlot(AwayGoalkeeper);

            float midline = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            Assert.Less(homeGk.x, midline,
                "Home goalkeeper formation slot must sit in the home half (near x = 0).");
            Assert.Greater(awayGk.x, midline,
                "Away goalkeeper formation slot must sit in the away half (near x = LENGTH) — the mirror.");

            // Exact pitch mirror: away = (LENGTH − homeX, WIDTH − homeY).
            Assert.AreEqual(MatchEngineConstants.PITCH_LENGTH_M - homeGk.x, awayGk.x, 0.5f,
                "Away GK slot X must be the pitch-mirror of the home GK slot X.");
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M - homeGk.y, awayGk.y, 0.5f,
                "Away GK slot Y must be the pitch-mirror of the home GK slot Y.");
        }

        [Test]
        public void PositioningWiring_IsDeterministic_AcrossSameSeedRuns()
        {
            MatchEngine engineA = RunTo(TwoStrideTicks);
            MatchEngine engineB = RunTo(TwoStrideTicks);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 a = engineA.TestOnly_FormationSlot(i);
                Vector2 b = engineB.TestOnly_FormationSlot(i);
                Assert.AreEqual(a.x, b.x,
                    $"Formation slot X for agent {i} diverged across two same-seed runs.");
                Assert.AreEqual(a.y, b.y,
                    $"Formation slot Y for agent {i} diverged across two same-seed runs.");
            }
        }

        [Test]
        public void DefensiveAI_FeedsLineDepthCarrier_IntoDecisionContext()
        {
            // D2b: the Defensive AI (#14) MarkDirective.OffensiveLineDepth is folded into each agent's
            // TacticalContext.DefensiveLineDepth, and HasMarkDirective is raised for the team WITHOUT the
            // ball (the Stage-1 MarkDirective? = null shape for attackers). At Stage 0 the depth is the
            // passthrough default (STAGE0_DEFENSIVE_LINE_DEPTH) — the carrier path is real even though the
            // value is value-neutral until live tactical instructions wire in.
            MatchEngine engine = RunTo(TwoStrideTicks);
            int owner = engine.TestOnly_PossessingAgentId;

            foreach (int agent in new[] { HomeGoalkeeper, HomeDefender, HomeStriker, AwayGoalkeeper })
            {
                // The agent's team carries a mark directive iff the ball is not held by one of its own.
                bool ownTeamHasBall = owner >= 0 && SameTeam(agent, owner);
                Assert.AreEqual(!ownTeamHasBall, engine.TestOnly_HasMarkDirective(agent),
                    $"Agent {agent} HasMarkDirective must reflect its team being out of possession.");
                Assert.AreEqual(MatchEngineConstants.STAGE0_DEFENSIVE_LINE_DEPTH,
                    engine.TestOnly_DefensiveLineDepth(agent), 1e-6f,
                    $"Agent {agent} DefensiveLineDepth carrier must echo the Stage-0 default.");
            }
        }

        [Test]
        public void AwayTeamCarriers_MirrorHomeTeam()
        {
            // The three Mechanics-AI carriers are authored in the per-team canonical attack-+X frame; with a
            // centre-spot kickoff the pitch is mirror-symmetric, so each home agent and its away counterpart
            // (slot k ↔ PLAYERS_PER_TEAM + k) must carry identical carrier values. The D2b analogue of the
            // D2a exact-GK-pitch-mirror lock (ERR-008-002 guard at the carrier layer).
            //
            // §5.Z Phase H: the mirror only holds for a configuration that is itself symmetric, and the
            // Phase-H kickoff award is deliberately NOT — one side kicks off. Possession is therefore
            // cleared before every tick here, restoring the neutral loose-ball setup this geometric lock
            // was written against. (Possession asymmetry is exercised by the tests above, which read the
            // live owner.)
            MatchEngine engine = RunToWithBallLoose(TwoStrideTicks);

            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int home = k;
                int away = MatchEngineConstants.PLAYERS_PER_TEAM + k;
                Assert.AreEqual(engine.TestOnly_DefensiveLineDepth(home), engine.TestOnly_DefensiveLineDepth(away),
                    $"Slot {k}: away DefensiveLineDepth must mirror home.");
                Assert.AreEqual(engine.TestOnly_HasMarkDirective(home), engine.TestOnly_HasMarkDirective(away),
                    $"Slot {k}: away HasMarkDirective must mirror home.");
                Assert.AreEqual(engine.TestOnly_HasAttackIntent(home), engine.TestOnly_HasAttackIntent(away),
                    $"Slot {k}: away HasAttackIntent must mirror home.");
            }
        }

        private static bool SameTeam(int a, int b)
        {
            return (a / MatchEngineConstants.PLAYERS_PER_TEAM) == (b / MatchEngineConstants.PLAYERS_PER_TEAM);
        }

        [Test]
        public void MechanicsCarriers_AreDeterministic_AcrossSameSeedRuns()
        {
            // The full Positioning→Pressing→Defensive→Attacking chain (incl. each tick's internal
            // hysteresis) must be byte-stable across two same-seed runs, so the carriers it folds into the
            // decision context are identical.
            MatchEngine engineA = RunTo(TwoStrideTicks);
            MatchEngine engineB = RunTo(TwoStrideTicks);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(engineA.TestOnly_DefensiveLineDepth(i), engineB.TestOnly_DefensiveLineDepth(i),
                    $"DefensiveLineDepth carrier for agent {i} diverged across two same-seed runs.");
                Assert.AreEqual(engineA.TestOnly_HasMarkDirective(i), engineB.TestOnly_HasMarkDirective(i),
                    $"HasMarkDirective carrier for agent {i} diverged across two same-seed runs.");
                Assert.AreEqual(engineA.TestOnly_HasAttackIntent(i), engineB.TestOnly_HasAttackIntent(i),
                    $"HasAttackIntent carrier for agent {i} diverged across two same-seed runs.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Initial Phase D step D2 mechanics-AI wiring tests: Positioning  |
// |         |            |        | AI #12 feeds formation slots into the decision context (home    |
// |         |            |        | defender deep / striker advanced); away-team slots mirror the   |
// |         |            |        | home team (ERR-008-002 guard, exact GK pitch-mirror); and the   |
// |         |            |        | wiring is deterministic across two same-seed runs.              |
// | 1.1     | 2026-06-26 | —      | Phase D step D2b: added Defensive AI (#14) line-depth +         |
// |         |            |        | HasMarkDirective carrier test and a same-seed determinism lock  |
// |         |            |        | over all three Mechanics-AI carriers (DefensiveLineDepth /      |
// |         |            |        | HasMarkDirective / HasAttackIntent).                            |
// | 1.2     | 2026-06-26 | —      | D2b AR (2L): line-depth test now asserts HasMarkDirective       |
// |         |            |        | tracks possession (raised iff the agent's team is ball-less);   |
// |         |            |        | new AwayTeamCarriers_MirrorHomeTeam home↔away symmetry lock.    |
#endregion
