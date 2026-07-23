// File:     src/match-engine/tests/MatchEngineGkHeadingTests.cs
// Created:  2026-07-22
// Author:   —
// Spec:     GK/Heading engine-integration design supplement
//           (docs/tracking/gk-heading-engine-integration-design.md) §7, Phase 1; Code Standards #20
// Purpose:  Phase-1 locks for the opt-in GK (#11) / Heading (#10) wiring: the projections are a LIVE
//           consumer (a save/header intent is committed seeded from ToGoalkeeper/ToHeading), a
//           flag-on engine is forward-deterministic, a flag-off engine ticks unchanged and commits
//           nothing, and the durable snapshot path fails loud while the flag is on (KD-11 / §6).

using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Phase-1 integration locks for the GK/Heading opt-in wiring.</summary>
    [TestFixture]
    public sealed class MatchEngineGkHeadingTests
    {
        private const ulong MatchSeed = 0x0BADF00DDEADBEEFUL;
        private const int   TickCount = 120;

        private static int RequiredCount =>
            MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;

        private static PlayerPosition PosFor(int localIndex)
        {
            if (localIndex == 0)  return PlayerPosition.Goalkeeper;
            if (localIndex <= 4)  return PlayerPosition.Defender;
            if (localIndex <= 8)  return PlayerPosition.Midfielder;
            if (localIndex <= 10) return PlayerPosition.Forward;
            switch ((localIndex - 11) % 3)
            {
                case 0:  return PlayerPosition.Defender;
                case 1:  return PlayerPosition.Midfielder;
                default: return PlayerPosition.Forward;
            }
        }

        private static PlayerRecord[] CoherentPlayers(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);
                players[k] = p;
            }
            return players;
        }

        private static Squad DefaultSquad(int clubId) => new Squad(clubId, CoherentPlayers(clubId));

        /// <summary>A squad whose GK (local index 0) carries a recognisably non-neutral Pace (19), so the
        /// committed save intent's projected Pace reveals whether the roster value flowed through.</summary>
        private static Squad SquadWithDistinctGoalkeeper(int clubId)
        {
            PlayerRecord[] players = CoherentPlayers(clubId);
            var gk = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            gk.Pace = 19;   // ToGoalkeeper copies Pace → the committed attrs must carry 19f
            players[0].Attributes = gk;
            return new Squad(clubId, players);
        }

        private static int FirstOutfieldAgent(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        // ── flag semantics ──────────────────────────────────────────────────────────

        [Test]
        public void EnableGkHeading_SetsTheFlag()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.IsFalse(engine.TestOnly_GkHeadingEnabled, "Wiring is opt-in — default off (KD-11).");
            engine.EnableGkHeading();
            Assert.IsTrue(engine.TestOnly_GkHeadingEnabled);
        }

        // ── flag OFF: byte-identical default, commits nothing ─────────────────────────

        [Test]
        public void FlagOff_TicksDeterministically_AndCommitsNoIntent()
        {
            var a = new MatchEngine(MatchSeed);
            var b = new MatchEngine(MatchSeed);
            for (int i = 0; i < TickCount; i++)
            {
                a.RunTick();
                b.RunTick();
                CollectionAssert.AreEqual(a.CurrentSnapshotDigest, b.CurrentSnapshotDigest,
                    $"Default (flag-off) engine must stay deterministic — diverged at tick {i + 1}.");
            }
            Assert.IsFalse(a.TestOnly_LastCommittedSaveAttrs.HasValue,
                "A flag-off engine must never commit a save intent.");
            Assert.IsFalse(a.TestOnly_LastCommittedHeaderAttrs.HasValue,
                "A flag-off engine must never commit a header intent.");
        }

        // ── flag ON: the projections are a LIVE consumer ──────────────────────────────

        /// <summary>Team 0's keeper defends x = 0: a loose ball 5 m out driving at the goal at 10 m/s.</summary>
        private static readonly Vector3 SaveBallPos = new Vector3(5f, 34f, 0.11f);
        private static readonly Vector3 SaveBallVel = new Vector3(-10f, 0f, 0f);

        /// <summary>ERR-008-013: the save is now a DT-emitted action, committed inside the AI phase of a
        /// natural <see cref="MatchEngine.RunTick"/> (RunMechanicsAI sets SaveAvailable → the keeper's
        /// DecisionTree emits SAVE → HostSaveDispatch commits). Re-force the loose ball each tick (physics
        /// integrates it / first-touch could claim it otherwise) and tick until the commit lands or a bound
        /// of two AI strides elapses — guaranteeing a stride tick runs RunAiPhase. Returns true if committed.</summary>
        private static bool DriveUntilSaveCommitted(MatchEngine engine)
        {
            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.TestOnly_ForceBallLoose(SaveBallPos, SaveBallVel);
                engine.RunTick();
                if (engine.TestOnly_LastCommittedSaveAttrs.HasValue)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void SaveTrigger_CommitsGoalkeeperProjection()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();

            Assert.IsTrue(DriveUntilSaveCommitted(engine),
                "The keeper's DT must emit SAVE and commit a SaveIntent seeded from ToGoalkeeper (the live consumer).");
            TacticalDirector.GoalkeeperMechanics.GoalkeeperAgentAttributes committed =
                engine.TestOnly_LastCommittedSaveAttrs.Value;
            TacticalDirector.GoalkeeperMechanics.GoalkeeperAgentAttributes expected =
                PlayerAttributeProjection.ToGoalkeeper(
                    in NeutralCanonical, teamId: 0, fatigue: 0f);
            Assert.AreEqual(expected.Reflexes, committed.Reflexes, 0f);
            Assert.AreEqual(expected.Handling, committed.Handling, 0f);
            Assert.AreEqual(expected.Kicking,  committed.Kicking,  0f);
            Assert.AreEqual(0, committed.TeamId, "Team 0's keeper committed the save.");
        }

        [Test]
        public void SaveDecision_SurvivesAdversarialTactic()
        {
            // AR-4 regression lock (integration level): SAVE is the SOLE off-ball option when available,
            // so it is selected regardless of a non-identity per-agent tactic. A BallWinningMid role
            // weights INTERCEPT high (RoleWeightModifiers up to 2.0) — exactly the input that, under the
            // rejected scoring-dominance approach, would have lifted INTERCEPT to the clamp ceiling and
            // won the lower-ordinal tiebreak (a missed save). The keeper must still commit the save.
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            int homeKeeper = HomeGoalkeeper(engine);
            Assert.GreaterOrEqual(homeKeeper, 0);
            engine.SetPlayerTactic(homeKeeper, TacticalDirector.TacticalInstructions.PlayerTactic.Default(
                TacticalDirector.TacticalInstructions.PlayerRole.BallWinningMid));

            Assert.IsTrue(DriveUntilSaveCommitted(engine),
                "SAVE (sole off-ball option) must win regardless of tactic — no missed save under an INTERCEPT-weighted role.");
        }

        private static int HomeGoalkeeper(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        [Test]
        public void HeaderTrigger_CommitsHeadingProjection()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();

            int agent = FirstOutfieldAgent(engine);
            Assert.GreaterOrEqual(agent, 0);
            Vector2 pos = engine.AgentView(agent).Position;
            int team = engine.AgentTeamId(agent);

            // Loose airborne ball at the agent's feet-plus-head: within head range, above control height.
            engine.TestOnly_ForceBallLoose(new Vector3(pos.x, pos.y, 1.0f), Vector3.zero);
            engine.TestOnly_DriveGkHeadingTactical();

            Assert.IsTrue(engine.TestOnly_LastCommittedHeaderAttrs.HasValue,
                "The header trigger must commit a HeaderIntent seeded from ToHeading (the live consumer).");
            TacticalDirector.HeadingMechanics.HeadingAgentAttributes committed =
                engine.TestOnly_LastCommittedHeaderAttrs.Value;
            TacticalDirector.HeadingMechanics.HeadingAgentAttributes expected =
                PlayerAttributeProjection.ToHeading(in NeutralCanonical, team, fatigue: 0f);
            Assert.AreEqual(expected.Heading,  committed.Heading);
            Assert.AreEqual(expected.Strength, committed.Strength);
            Assert.AreEqual(expected.Balance,  committed.Balance);
            Assert.AreEqual(team, committed.TeamId, "The committed header carries the heading agent's team.");
        }

        [Test]
        public void DistinctSquad_SaveCommitsRosterGoalkeeperAttrs()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureSquads(SquadWithDistinctGoalkeeper(7), DefaultSquad(8));   // home GK Pace = 19
            engine.EnableGkHeading();

            Assert.IsTrue(DriveUntilSaveCommitted(engine));
            TacticalDirector.GoalkeeperMechanics.GoalkeeperAgentAttributes committed =
                engine.TestOnly_LastCommittedSaveAttrs.Value;
            Assert.AreEqual(19f, committed.Pace, 0f,
                "The roster GK's real Pace (19) must flow through ToGoalkeeper into the committed intent.");
            Assert.AreEqual(10f, committed.Reflexes, 0f,
                "Fields the roster left at neutral stay neutral (no accidental cross-field swap).");
        }

        // ── flag ON: forward determinism ──────────────────────────────────────────────

        [Test]
        public void FlagOn_TwoRuns_AreForwardDeterministic()
        {
            List<byte[]> a = RunFlagOnChain();
            List<byte[]> b = RunFlagOnChain();
            for (int i = 0; i < TickCount; i++)
            {
                CollectionAssert.AreEqual(a[i], b[i],
                    $"A flag-on engine must be forward-deterministic — diverged at tick {i + 1}.");
            }
        }

        private static List<byte[]> RunFlagOnChain()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            var chain = new List<byte[]>(TickCount);
            for (int i = 0; i < TickCount; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        // ── flag ON: durable snapshot is now supported (Phase 2 — the guard is removed) ─

        [Test]
        public void FlagOn_DurableCapture_Succeeds()
        {
            // Phase 2 (v18): the GK/Heading cross-tick state is serialized, so a flag-on engine is
            // snapshot-safe — the durable-capture seams no longer fail loud (the KD-11 Phase-1 guard is gone).
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            engine.RunTick();
            Assert.DoesNotThrow(() => engine.CaptureDurableHeader(),
                "A flag-on engine must support the durable header capture at Phase 2.");
            Assert.DoesNotThrow(() => engine.CaptureDurablePayload(),
                "A flag-on engine must support the durable payload capture at Phase 2.");
        }

        [Test]
        public void FlagOff_DurableCapture_Succeeds()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.RunTick();
            Assert.DoesNotThrow(() => engine.CaptureDurableHeader(),
                "A default (flag-off) engine must still support the durable capture path.");
            Assert.DoesNotThrow(() => engine.CaptureDurablePayload());
        }

        private static readonly TacticalDirector.PlayerDatabase.PlayerAttributes NeutralCanonical =
            TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-22 | —      | Initial — Phase-1 GK/Heading opt-in wiring integration locks.  |
// | 1.1     | 2026-07-23 | —      | Phase 2 (v18): the durable-capture-fails-loud lock replaced by |
// |         |            |        | FlagOn_DurableCapture_Succeeds (the guard is removed; a flag-  |
// |         |            |        | on engine is now snapshot-safe). Round-trip determinism is     |
// |         |            |        | locked in MatchEngineSnapshotRestoreTests.                      |
// | 1.2     | 2026-07-23 | —      | ERR-008-013: the save is now a DT-emitted SAVE action. The two |
// |         |            |        | save-commit tests drive through the natural RunTick DT path    |
// |         |            |        | (DriveUntilSaveCommitted); + SaveDecision_SurvivesAdversarial-  |
// |         |            |        | Tactic (the AR-4 sole-option missed-save regression lock).     |
#endregion
