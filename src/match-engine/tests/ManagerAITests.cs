// File:     src/match-engine/tests/ManagerAITests.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §3.1–§3.4, §5 (T-TP-U-002..012, T-TP-I-001/003/004, T-TP-DET-001/002),
//           Appendix B/C; Code Standards #20
// Purpose:  #26 manager-AI tests — FM-TP-01 projection, FM-TP-02 gate arithmetic, FM-TP-03 kickoff
//           scoring (Appendix B.1 exact), FM-TP-04 ladder + hold (Appendix B.2 cadence), the
//           fail-loud gates (F1/F2/F4), boot/mid-match routing through a real engine, and the
//           Human-identity + two-AI determinism locks.

using System;

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>#26 manager-AI unit + integration tests (see the §5 IDs on each test).</summary>
    [TestFixture]
    public sealed class ManagerAITests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const float Tol = 1e-4f;

        private static int HomeAgent => 0;
        private static int AwayAgent => MatchEngineConstants.PLAYERS_PER_TEAM;

        private static void TickToFirstStride(MatchEngine engine)
        {
            for (int i = 0; i < TacticalDirector.DeterministicSim.DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick, "Test must tick to an AI-stride boundary.");
        }

        private static ManagerProfile Aggressive => ManagerProfile.FromArchetype(TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
        private static ManagerProfile Pragmatic => ManagerProfile.FromArchetype(TacticalPresetsConstants.ARCHETYPE_PRAGMATIC);

        // ── Ordinals + tables ─────────────────────────────────────────────────────────

        [Test]
        public void ManagerMode_OrdinalStability()
        {
            // FR-TP-013 / T-TP-U-011: byte-backed, APPEND-only — serialized at v13.
            Assert.AreEqual(0, (int)ManagerMode.Human);
            Assert.AreEqual(1, (int)ManagerMode.AI);
        }

        [Test]
        public void AffinityAndArchetypeTables_ShapeAndBounds()
        {
            // FR-TP-020 / T-TP-U-010 shape tests: one row per preset, bounded [−1, +1]; archetype
            // columns one row per archetype, Aggression/Caution in [0, 1], Patience ≥ 1.
            Assert.AreEqual(TacticPresetLibrary.Count, TacticalPresetsConstants.BaseFit.Length);
            Assert.AreEqual(TacticPresetLibrary.Count, TacticalPresetsConstants.AggrAffinity.Length);
            Assert.AreEqual(TacticPresetLibrary.Count, TacticalPresetsConstants.CautAffinity.Length);
            for (int p = 0; p < TacticPresetLibrary.Count; p++)
            {
                Assert.GreaterOrEqual(TacticalPresetsConstants.BaseFit[p], -1f);
                Assert.LessOrEqual(TacticalPresetsConstants.BaseFit[p], 1f);
                Assert.GreaterOrEqual(TacticalPresetsConstants.AggrAffinity[p], -1f);
                Assert.LessOrEqual(TacticalPresetsConstants.AggrAffinity[p], 1f);
                Assert.GreaterOrEqual(TacticalPresetsConstants.CautAffinity[p], -1f);
                Assert.LessOrEqual(TacticalPresetsConstants.CautAffinity[p], 1f);
            }

            Assert.AreEqual(TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT,
                TacticalPresetsConstants.ArchetypeAggression.Length);
            Assert.AreEqual(TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT,
                TacticalPresetsConstants.ArchetypeCaution.Length);
            Assert.AreEqual(TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT,
                TacticalPresetsConstants.ArchetypePatienceIntervals.Length);
            for (int a = 0; a < TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT; a++)
            {
                Assert.GreaterOrEqual(TacticalPresetsConstants.ArchetypeAggression[a], 0f);
                Assert.LessOrEqual(TacticalPresetsConstants.ArchetypeAggression[a], 1f);
                Assert.GreaterOrEqual(TacticalPresetsConstants.ArchetypeCaution[a], 0f);
                Assert.LessOrEqual(TacticalPresetsConstants.ArchetypeCaution[a], 1f);
                Assert.GreaterOrEqual(TacticalPresetsConstants.ArchetypePatienceIntervals[a], 1);
            }
        }

        // ── ManagerProfile (F2/F4) ────────────────────────────────────────────────────

        [Test]
        public void Profile_RefusesNonFiniteAndOutOfRange()
        {
            // F4 / T-TP-U-010: the NaN-gate pattern (!(x >= 0 && x <= 1) fails for NaN).
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagerProfile(float.NaN, 0.5f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagerProfile(1.5f, 0.5f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagerProfile(0.5f, float.NaN, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagerProfile(0.5f, -0.1f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagerProfile(0.5f, 0.5f, 0));
        }

        [Test]
        public void Profile_FromArchetype_ReadsTableAndGatesOrdinal()
        {
            // A.2 row round-trip + the F2 out-of-range gate (T-TP-U-012).
            ManagerProfile p = Aggressive;
            Assert.AreEqual(0.8f, p.Aggression, Tol);
            Assert.AreEqual(0.2f, p.Caution, Tol);
            Assert.AreEqual(1, p.PatienceIntervals);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ManagerProfile.FromArchetype((byte)TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT));
        }

        // ── FM-TP-03 kickoff scoring (T-TP-U-005) ─────────────────────────────────────

        [Test]
        public void KickoffScoring_WorkedExampleB1_Aggressive()
        {
            // Appendix B.1 (authoritative derivation): Gegenpress 0.66, Balanced 0.50,
            // Possession 0.42, CounterAttack 0.24, ParkTheBus −0.58 → selects Gegenpress.
            ManagerProfile p = Aggressive;
            Assert.AreEqual(0.66f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.GegenpressOrdinal), Tol);
            Assert.AreEqual(0.50f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.BalancedOrdinal), Tol);
            Assert.AreEqual(0.42f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.PossessionOrdinal), Tol);
            Assert.AreEqual(0.24f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.CounterAttackOrdinal), Tol);
            Assert.AreEqual(-0.58f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.ParkTheBusOrdinal), Tol);
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal, ManagerAdaptation.SelectKickoffPreset(in p));
        }

        [Test]
        public void KickoffScoring_WorkedExampleB1_Pragmatic()
        {
            // Appendix B.1: Balanced 0.50 top (CounterAttack 0.34, Possession 0.22, Gegenpress 0.06,
            // ParkTheBus −0.03) → selects Balanced.
            ManagerProfile p = Pragmatic;
            Assert.AreEqual(0.50f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.BalancedOrdinal), Tol);
            Assert.AreEqual(0.34f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.CounterAttackOrdinal), Tol);
            Assert.AreEqual(0.06f, ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.GegenpressOrdinal), Tol);
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal, ManagerAdaptation.SelectKickoffPreset(in p));
        }

        [Test]
        public void KickoffSelection_TieBreaksToLowestOrdinal()
        {
            // KD-8 / FR-TP-009: with (Aggression 0.5, Caution 0) the A.3 rows give
            // Gegenpress = 0.1 + 0.5×0.8 = 0.5 == Balanced = 0.5 — an exact float tie
            // (0.8f × 0.5f and 0.1f + 0.4f both round exactly). The lower ordinal must win.
            ManagerProfile p = new ManagerProfile(0.5f, 0f, 1);
            float gegen = ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.GegenpressOrdinal);
            float balanced = ManagerAdaptation.KickoffScore(in p, TacticPresetLibrary.BalancedOrdinal);
            Assert.AreEqual(balanced, gegen, "Precondition: the two scores must tie exactly for this lock.");
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal, ManagerAdaptation.SelectKickoffPreset(in p));
        }

        // ── §3.4 StepToward + FM-TP-04 ladder (T-TP-U-006/007/009) ────────────────────

        [Test]
        public void StepToward_OneRungAndSaturates()
        {
            Assert.AreEqual(3, ManagerAdaptation.StepToward(true, 2));
            Assert.AreEqual(1, ManagerAdaptation.StepToward(false, 2));
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal,
                ManagerAdaptation.StepToward(true, TacticPresetLibrary.GegenpressOrdinal));
            Assert.AreEqual(TacticPresetLibrary.ParkTheBusOrdinal,
                ManagerAdaptation.StepToward(false, TacticPresetLibrary.ParkTheBusOrdinal));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ManagerAdaptation.StepToward(true, (byte)TacticPresetLibrary.Count));
        }

        [Test]
        public void Ladder_WorkedExampleB2()
        {
            // Appendix B.2: 0–1 down at 70′ (ticksRemaining 72 000 of 324 000, t01 ≈ 0.222):
            // Aggressive urgency 0.622 ≥ 0.35 → steps Balanced → Possession;
            // Pragmatic 0.233 < 0.35 → holds.
            ManagerProfile agg = Aggressive;
            ManagerProfile prg = Pragmatic;
            Assert.AreEqual(TacticPresetLibrary.PossessionOrdinal,
                ManagerAdaptation.EvaluateLadder(in agg, TacticPresetLibrary.BalancedOrdinal, -1, 72000L, 324000L));
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal,
                ManagerAdaptation.EvaluateLadder(in prg, TacticPresetLibrary.BalancedOrdinal, -1, 72000L, 324000L));
        }

        [Test]
        public void Ladder_LeadProtection_StepsDefensive()
        {
            // Leading +1 late (80′, t01 ≈ 0.111): Pragmatic protect = 0.7 × 0.889 × 1 ≈ 0.622 ≥ 0.35
            // → steps one rung defensive (Balanced → CounterAttack).
            ManagerProfile prg = Pragmatic;
            Assert.AreEqual(TacticPresetLibrary.CounterAttackOrdinal,
                ManagerAdaptation.EvaluateLadder(in prg, TacticPresetLibrary.BalancedOrdinal, +1, 36000L, 324000L));
        }

        [Test]
        public void Ladder_UrgencyDiffCap_ThreeGoalsScoreAsTwo()
        {
            // T-TP-U-009: min(−goalDiff, URGENCY_DIFF_CAP) — a −3 deficit is exactly a −2 deficit,
            // and either way the step is ONE rung.
            ManagerProfile agg = Aggressive;
            byte atMinus2 = ManagerAdaptation.EvaluateLadder(in agg, TacticPresetLibrary.BalancedOrdinal, -2, 72000L, 324000L);
            byte atMinus3 = ManagerAdaptation.EvaluateLadder(in agg, TacticPresetLibrary.BalancedOrdinal, -3, 72000L, 324000L);
            Assert.AreEqual(atMinus2, atMinus3);
            Assert.AreEqual(TacticPresetLibrary.PossessionOrdinal, atMinus3, "One rung per decision, never more.");
        }

        [Test]
        public void Ladder_RefusesNonPositiveMatchTotal()
        {
            ManagerProfile agg = Aggressive;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ManagerAdaptation.EvaluateLadder(in agg, TacticPresetLibrary.BalancedOrdinal, -1, 0L, 0L));
        }

        // ── FM-TP-02 gate (T-TP-U-003/004) ────────────────────────────────────────────

        [Test]
        public void Gate_KickoffAndIntervalArithmetic()
        {
            // §3.2 worked example: the kickoff decision is the first-ever evaluation
            // (LastDecisionTick < 0); the next interval decision is due no earlier than
            // ManagerDecisionIntervalTicks after the last fired one.
            int interval = TacticalPresetsConstants.ManagerDecisionIntervalTicks;
            ManagerState st = new ManagerState { Mode = ManagerMode.AI, LastDecisionTick = -1 };
            Assert.IsTrue(ManagerDecisionGate.DecisionDue(0, in st), "Kickoff (first-ever) decision must fire.");

            st.LastDecisionTick = 0;  // kickoff consumed (boot path or the tick-0 fire)
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(0, in st), "Kickoff fires once.");
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(interval - 1, in st));
            Assert.IsTrue(ManagerDecisionGate.DecisionDue(interval, in st));

            st.LastDecisionTick = interval;
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(interval + interval - 1, in st));
            Assert.IsTrue(ManagerDecisionGate.DecisionDue(interval + interval, in st));
        }

        [Test]
        public void Gate_HumanNeverFires()
        {
            // FR-TP-007 / T-TP-U-004: Human mode short-circuits the gate at any tick.
            ManagerState st = default;  // zero-init = Human = inert (KD-4)
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(0, in st));
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(TacticalPresetsConstants.ManagerDecisionIntervalTicks, in st));
            Assert.IsFalse(ManagerDecisionGate.DecisionDue(int.MaxValue, in st));
        }

        // ── FM-TP-01 projection (T-TP-U-002) ──────────────────────────────────────────

        [Test]
        public void Projection_FillsManagedTeamOnly_MissingPlayersAreIdentity()
        {
            TacticPreset gegenpress = TacticPresetLibrary.Presets[TacticPresetLibrary.GegenpressOrdinal];
            TeamTactic other = TeamTactic.Balanced;

            TacticPresetProjection.Project(in gegenpress, 1, in other,
                out TeamTacticConfig teamConfig, out PlayerTacticConfig playerConfig);

            Assert.AreEqual(Mentality.Attacking, teamConfig.ForTeam(1).Mentality, "Managed (away) team gets the preset.");
            Assert.AreEqual(Mentality.Balanced, teamConfig.ForTeam(0).Mentality, "The other team keeps its own tactic.");

            PlayerTactic identity = PlayerTactic.Default(PlayerRole.Default);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(identity.Role, playerConfig.ForAgent(i).Role,
                    "Missing preset Players ⇒ identity rows for every agent (FM-TP-01).");
            }
        }

        [Test]
        public void Projection_PlayersFillManagedBlock_WrongLengthThrows()
        {
            // F1 / FR-TP-014: a present Players array must be exactly PLAYERS_PER_TEAM long.
            PlayerTactic[] wrongLength = new PlayerTactic[3];
            TacticPreset bad = new TacticPreset("bad", TeamTactic.Balanced, wrongLength);
            TeamTactic other = TeamTactic.Balanced;
            Assert.Throws<ArgumentException>(() => TacticPresetProjection.Project(
                in bad, 0, in other, out _, out _));

            // A full-roster Players array fills ONLY the managed team's block.
            PlayerTactic[] eleven = new PlayerTactic[MatchEngineConstants.PLAYERS_PER_TEAM];
            for (int k = 0; k < eleven.Length; k++)
            {
                eleven[k] = PlayerTactic.Default(PlayerRole.Poacher);
            }
            TacticPreset withPlayers = new TacticPreset("with-players", TeamTactic.Balanced, eleven);
            TacticPresetProjection.Project(in withPlayers, 0, in other,
                out _, out PlayerTacticConfig playerConfig);
            Assert.AreEqual(PlayerRole.Poacher, playerConfig.ForAgent(0).Role);
            Assert.AreEqual(PlayerRole.Poacher, playerConfig.ForAgent(MatchEngineConstants.PLAYERS_PER_TEAM - 1).Role);
            Assert.AreEqual(PlayerRole.Default, playerConfig.ForAgent(MatchEngineConstants.PLAYERS_PER_TEAM).Role,
                "The unmanaged team's block stays identity.");
        }

        // ── FR-TP-011 hold + FR-TP-005 mid-match path (T-TP-U-008) ────────────────────

        [Test]
        public void RunDecisionPoint_HoldThenStep_B2Cadence()
        {
            // The B.2 cadence through the real mid-match path: switch at 70′ sets hold = 2×1;
            // the 75′ decision is held (hold → 1); at 80′ the hold reaches 0 and the switch is
            // evaluable at that same decision point (decrement-then-check, the #25 hold precedent).
            var engine = new MatchEngine(MatchSeed);
            ManagerState st = new ManagerState
            {
                Mode = ManagerMode.AI,
                ProfileOrdinal = TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE,
                CurrentPresetOrdinal = TacticPresetLibrary.BalancedOrdinal,
            };

            // 70′ — trailing 0–1: urgency 0.622 → step Balanced → Possession; hold = 2 × 1.
            ManagerAdaptation.RunDecisionPoint(engine, 0, ref st, 252000, -1, 72000L, 324000L);
            Assert.AreEqual(TacticPresetLibrary.PossessionOrdinal, st.CurrentPresetOrdinal);
            Assert.AreEqual(2, st.HoldIntervalsRemaining);
            Assert.AreEqual(252000, st.LastDecisionTick);

            // 75′ — still down, but held (hold 2 → 1, no evaluation).
            ManagerAdaptation.RunDecisionPoint(engine, 0, ref st, 270000, -1, 54000L, 324000L);
            Assert.AreEqual(TacticPresetLibrary.PossessionOrdinal, st.CurrentPresetOrdinal);
            Assert.AreEqual(1, st.HoldIntervalsRemaining);

            // 80′ — hold reaches 0 at this decision point: evaluable now → steps Possession →
            // Gegenpress (urgency 0.8 × 0.889 ≈ 0.711) and re-arms the hold.
            ManagerAdaptation.RunDecisionPoint(engine, 0, ref st, 288000, -1, 36000L, 324000L);
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal, st.CurrentPresetOrdinal);
            Assert.AreEqual(2, st.HoldIntervalsRemaining);

            // The switch reached the engine through SetTeamTactic (FR-TP-005) — staged pending,
            // committed at the next stride boundary.
            TickToFirstStride(engine);
            Assert.AreEqual(Mentality.Attacking, engine.TestOnly_Mentality(HomeAgent));
        }

        [Test]
        public void RunDecisionPoint_PatienceMultipliesHold()
        {
            // FR-TP-011: hold = MANAGER_SWITCH_HOLD_INTERVALS × PatienceIntervals (Pragmatic = 2 × 2).
            var engine = new MatchEngine(MatchSeed);
            ManagerState st = new ManagerState
            {
                Mode = ManagerMode.AI,
                ProfileOrdinal = TacticalPresetsConstants.ARCHETYPE_PRAGMATIC,
                CurrentPresetOrdinal = TacticPresetLibrary.BalancedOrdinal,
            };
            // Two down late: Pragmatic urgency = 0.3 × 0.889 × 2 ≈ 0.533 ≥ 0.35 → steps; hold = 4.
            ManagerAdaptation.RunDecisionPoint(engine, 0, ref st, 288000, -2, 36000L, 324000L);
            Assert.AreEqual(TacticPresetLibrary.PossessionOrdinal, st.CurrentPresetOrdinal);
            Assert.AreEqual(4, st.HoldIntervalsRemaining);
        }

        // ── ConfigureManager gates (T-TP-U-012) ───────────────────────────────────────

        [Test]
        public void ConfigureManager_GatesFailLoud()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.ConfigureManager(2, ManagerMode.AI, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.ConfigureManager(0, (ManagerMode)7, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.ConfigureManager(
                0, ManagerMode.AI, (byte)TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT));

            // Human resets to the inert zero-init identity (KD-4).
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            engine.ConfigureManager(0, ManagerMode.Human);
            ManagerState st = engine.TestOnly_ManagerState(0);
            Assert.AreEqual(ManagerMode.Human, st.Mode);
            Assert.AreEqual(0, st.LastDecisionTick);
            Assert.AreEqual(0, st.HoldIntervalsRemaining);
        }

        // ── Boot path (T-TP-I-001/003/004) ────────────────────────────────────────────

        [Test]
        public void ApplyKickoff_RoutesSelectedPresetThroughBootAppliers()
        {
            // T-TP-I-001: home Aggressive → Gegenpress selected at boot, staged via the real
            // appliers, committed at the first stride; the seeded state consumes the kickoff
            // decision (LastDecisionTick = 0, so the tick-0 gate does not re-fire).
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            ManagerAdaptation.ApplyKickoff(engine);

            ManagerState st = engine.TestOnly_ManagerState(0);
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal, st.CurrentPresetOrdinal);
            Assert.AreEqual(0, st.LastDecisionTick);

            TickToFirstStride(engine);
            Assert.AreEqual(Mentality.Attacking, engine.TestOnly_Mentality(HomeAgent), "Gegenpress reached the home agents.");
            Assert.AreEqual(Mentality.Balanced, engine.TestOnly_Mentality(AwayAgent), "The Human away team stays Balanced.");
            Assert.AreEqual(0, engine.TestOnly_ManagerState(0).LastDecisionTick,
                "The boot-stamped kickoff decision must not re-fire at the first stride.");
        }

        [Test]
        public void ApplyKickoff_TwoManagers_DecideIndependently()
        {
            // T-TP-I-003: each AI manager selects from its OWN profile (KD-5) — home Aggressive
            // → Gegenpress, away Pragmatic → Balanced.
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            engine.ConfigureManager(1, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_PRAGMATIC);
            ManagerAdaptation.ApplyKickoff(engine);
            TickToFirstStride(engine);

            Assert.AreEqual(Mentality.Attacking, engine.TestOnly_Mentality(HomeAgent));
            Assert.AreEqual(Mentality.Balanced, engine.TestOnly_Mentality(AwayAgent));
            Assert.AreEqual(TacticPresetLibrary.GegenpressOrdinal, engine.TestOnly_ManagerState(0).CurrentPresetOrdinal);
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal, engine.TestOnly_ManagerState(1).CurrentPresetOrdinal);
        }

        [Test]
        public void ApplyKickoff_PreservesHumanBaseline()
        {
            // T-TP-I-004: the Human team's baseline tactic survives an AI opponent's kickoff
            // selection untouched (the FM-TP-01 "other team keeps its own value" rule end-to-end).
            TeamTactic humanAway = new TeamTactic(
                Mentality.VeryDefensive, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
                TacticPassing.Short, TacticPressing.Low, LineOfEngagement.Standard, 0.5f,
                TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
                TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            ManagerAdaptation.ApplyKickoff(engine, new TeamTacticConfig(TeamTactic.Balanced, humanAway));
            TickToFirstStride(engine);

            Assert.AreEqual(Mentality.Attacking, engine.TestOnly_Mentality(HomeAgent));
            Assert.AreEqual(Mentality.VeryDefensive, engine.TestOnly_Mentality(AwayAgent),
                "The Human away team's own tactic must be preserved by the AI kickoff composition.");
        }

        [Test]
        public void Gate_FiresKickoffDecisionInEngine_WhenNotBootApplied()
        {
            // An AI team configured WITHOUT the boot kickoff path: the first stride evaluation
            // fires as the kickoff decision — the state is stamped with that stride tick, the
            // ladder is a no-op at the engine's level score (goalDiff 0), and the tactic stays
            // Balanced.
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            Assert.AreEqual(-1, engine.TestOnly_ManagerState(0).LastDecisionTick);

            TickToFirstStride(engine);
            Assert.AreEqual((int)engine.CurrentTick, engine.TestOnly_ManagerState(0).LastDecisionTick,
                "The first stride fire stamps the state with its own tick.");
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal, engine.TestOnly_ManagerState(0).CurrentPresetOrdinal);
            Assert.AreEqual(Mentality.Balanced, engine.TestOnly_Mentality(HomeAgent), "No switch at a level score.");
        }

        // ── Determinism (T-TP-DET-001/002) ────────────────────────────────────────────

        [Test]
        public void TwoAiManagers_TwoRuns_BitwiseIdenticalDigests()
        {
            // T-TP-DET-001: two same-seed runs with both managers AI produce identical digest chains.
            byte[] digestA = RunAiVsAi();
            byte[] digestB = RunAiVsAi();
            CollectionAssert.AreEqual(digestA, digestB);
        }

        [Test]
        public void ExplicitHuman_IsBehaviourNeutral_DigestUnchanged()
        {
            // T-TP-DET-002 (in-tree form): explicitly configuring Human/Human — and running the
            // no-manager boot path over the default baselines — is digest-identical to never
            // touching the manager surface at all (FR-TP-007 / KD-4).
            var untouched = new MatchEngine(MatchSeed);
            var explicitHuman = new MatchEngine(MatchSeed);
            explicitHuman.ConfigureManager(0, ManagerMode.Human);
            explicitHuman.ConfigureManager(1, ManagerMode.Human);
            ManagerAdaptation.ApplyKickoff(explicitHuman);

            for (int i = 0; i < 30; i++)
            {
                untouched.RunTick();
                explicitHuman.RunTick();
            }
            CollectionAssert.AreEqual(untouched.CurrentSnapshotDigest, explicitHuman.CurrentSnapshotDigest);
        }

        private static byte[] RunAiVsAi()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureManager(0, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_AGGRESSIVE);
            engine.ConfigureManager(1, ManagerMode.AI, TacticalPresetsConstants.ARCHETYPE_PRAGMATIC);
            ManagerAdaptation.ApplyKickoff(engine);
            for (int i = 0; i < 60; i++)
            {
                engine.RunTick();
            }
            byte[] copy = new byte[engine.CurrentSnapshotDigest.Length];
            Array.Copy(engine.CurrentSnapshotDigest, copy, copy.Length);
            return copy;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-11 | —      | Initial #26 T1–T4 suite (21 tests): B.1/B.2 worked examples,   |
// |         |            |        |   gate arithmetic, projection, hold cadence, fail-loud gates,  |
// |         |            |        |   boot/mid-match routing, Human identity + two-AI determinism. |
#endregion
