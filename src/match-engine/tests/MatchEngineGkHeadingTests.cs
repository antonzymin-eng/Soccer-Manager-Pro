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
            // §5.Z.15: the default flipped OFF → ON. Goalkeeper Mechanics #11 was built, wired,
            // snapshot-safe and switched off, so every match was played without a keeper who could
            // attempt a save — a large part of a goal rate ~10x football's. Both toggles are locked
            // here so a silent revert of the default fails.
            var engine = new MatchEngine(MatchSeed);
            Assert.IsTrue(engine.TestOnly_GkHeadingEnabled, "#11/#10 are ON by default since §5.Z.15.");
            engine.DisableGkHeading();
            Assert.IsFalse(engine.TestOnly_GkHeadingEnabled);
            engine.EnableGkHeading();
            Assert.IsTrue(engine.TestOnly_GkHeadingEnabled);
        }

        // ── flag OFF: byte-identical default, commits nothing ─────────────────────────

        [Test]
        public void FlagOff_TicksDeterministically_AndCommitsNoIntent()
        {
            // The two runs are SEQUENTIAL, not interleaved. The EventBus is a process-static singleton
            // (#17 §3.2.1 KD-4/KD-8) reset per match by Boot's ResetForNewMatch, so exactly one engine may
            // be ticking at a time: interleaved ticks share one ledger and one tick/phase cursor. That was
            // invisible until §5.Z Phase H, because before it no production event was ever published (no
            // possession ever changed, and no goal was ever scored) — so an interleaved loop silently
            // worked. It now diverges on tick 1, which is a property of the shared bus, not of the engine.
            // §5.Z.15 flipped the default to ON, so a test named FlagOff must now say so explicitly —
            // otherwise it silently becomes a second flag-ON determinism test and its no-commit
            // assertions stop meaning what they claim.
            var a = new MatchEngine(MatchSeed);
            a.DisableGkHeading();
            var chainA = new byte[TickCount][];
            for (int i = 0; i < TickCount; i++)
            {
                a.RunTick();
                chainA[i] = (byte[])a.CurrentSnapshotDigest.Clone();
            }

            var b = new MatchEngine(MatchSeed);
            b.DisableGkHeading();
            for (int i = 0; i < TickCount; i++)
            {
                b.RunTick();
                CollectionAssert.AreEqual(chainA[i], b.CurrentSnapshotDigest,
                    $"A flag-off engine must stay deterministic — diverged at tick {i + 1}.");
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

        /// <summary>A ball parked deep in the keeper's ATTACKING half (far from the x = 0 goal team 0
        /// defends), loose and stationary — <see cref="GkHeadingIntentSource.SaveArmed"/> returns false, so
        /// <c>RunMechanicsAI</c> clears the per-episode latch. Re-forced each tick so physics / first-touch
        /// cannot re-develop an armed geometry.</summary>
        private static readonly Vector3 DisarmBallPos = new Vector3(95f, 34f, 0.11f);

        /// <summary>Ticks with the ball held in a not-save-armed geometry until the per-episode latch clears
        /// (RunMechanicsAI runs on the AI stride), bounded by two AI strides. Returns true if it cleared.</summary>
        private static bool DriveUntilLatchClears(MatchEngine engine)
        {
            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.TestOnly_ForceBallLoose(DisarmBallPos, Vector3.zero);
                engine.RunTick();
                if (!engine.TestOnly_SaveCommittedForGk(0))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Ticks with the save-armed geometry until the team-0 per-episode latch is SET, bounded by
        /// two AI strides. The latch is set ONLY inside <c>HostSaveDispatch.CommitSave</c> after its
        /// latch-is-clear check, so a false → true transition is a genuine commit (unlike the sticky
        /// <see cref="MatchEngine.TestOnly_LastCommittedSaveAttrs"/>, which never resets between episodes).</summary>
        private static bool DriveUntilLatchSet(MatchEngine engine)
        {
            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.TestOnly_ForceBallLoose(SaveBallPos, SaveBallVel);
                engine.RunTick();
                if (engine.TestOnly_SaveCommittedForGk(0))
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void SaveEpisode_ReArmsAfterBallResolves_CommitsAgain()
        {
            // Regression lock for the latch-clear → re-commit path (the `if (!armed) _saveCommittedForGk[t]
            // = false;` line in RunMechanicsAI): SAVE is a CONTINUOUS DT action, so the per-episode latch is
            // the sole guard against a re-commit. A keeper must save a first shot, then — once that ball
            // resolves (no longer armed) and a second shot arrives — save AGAIN. Without the latch clear the
            // keeper would commit once and never again. The commit signal is the latch false → true edge (set
            // only inside CommitSave); TestOnly_LastCommittedSaveAttrs is sticky and cannot distinguish a
            // second commit from the first.
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();

            // Episode 1: shot arrives, keeper commits, latch set (from the boot-clear false).
            Assert.IsTrue(DriveUntilLatchSet(engine), "First shot must commit a save (latch set).");
            Assert.IsTrue(engine.TestOnly_LastCommittedSaveAttrs.HasValue,
                "The first commit must have projected and stored the GK attrs.");

            // Ball resolves (moves out of the armed geometry): RunMechanicsAI must clear the latch.
            Assert.IsTrue(DriveUntilLatchClears(engine),
                "Once the ball is no longer save-armed, RunMechanicsAI must clear the per-episode latch.");

            // Episode 2: a fresh shot arrives — the keeper must re-arm and commit a SECOND save (latch set
            // again, which can only happen through a fresh CommitSave now that the latch was cleared).
            Assert.IsTrue(DriveUntilLatchSet(engine),
                "A second shot after the latch cleared must commit again — the keeper saves more than once.");
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
            engine.DisableGkHeading();   // §5.Z.15 — default is now ON; this test is about the OFF path.
            engine.RunTick();
            Assert.DoesNotThrow(() => engine.CaptureDurableHeader(),
                "A flag-off engine must still support the durable capture path.");
            Assert.DoesNotThrow(() => engine.CaptureDurablePayload());
        }

        // ── §5.Z.15 six-second rule (Laws of the Game, Law 12) ────────────────────────

        [Test]
        public void GoalkeeperHoldingTheBall_IsForcedToReleaseAtSixSeconds()
        {
            // The regression this locks is a MATCH STALL, not a rules detail. Making the keeper a live,
            // mobile agent let it win possession, and nothing could make it give the ball up — measured,
            // a keeper held the ball for 33.5% of one second half.
            var engine = new MatchEngine(MatchSeed);
            int keeper = FirstGoalkeeperAgent(engine);
            Assert.GreaterOrEqual(keeper, 0, "fixture needs a goalkeeper");

            // Possession is re-asserted at the TOP of every tick so ordinary play cannot take the ball
            // away and leave the test measuring nothing — the release must come from the rule. The
            // observation is made after RunTick and before the next re-assert, so the tick on which the
            // rule fires is visible. Two things are locked, and the first is what makes it non-vacuous:
            // the release must actually happen, and it must NOT happen before six seconds.
            // The rule is a STALL BACKSTOP, and measured, healthy play never reaches it: a keeper
            // distributes after ~54 ticks, well inside Law 12's 360. So this locks BOTH halves.
            //
            // (1) The release branch, driven directly through its own seam — otherwise the branch that
            //     exists solely to break the stall would itself be untested.
            engine.RunTick();
            engine.TestOnly_SetPossession(keeper);

            for (int i = 0; i < MatchEngineConstants.GkMaxHoldTicks - 1; i++)
            {
                engine.TestOnly_RunGoalkeeperReleaseRule();
            }
            Assert.AreEqual(keeper, engine.TestOnly_PossessingAgentId,
                "the keeper must still hold the ball before Law 12's six seconds elapse.");
            Assert.AreEqual(0, engine.TestOnly_GkReleaseCooldownRemaining);

            engine.TestOnly_RunGoalkeeperReleaseRule();
            Assert.AreNotEqual(keeper, engine.TestOnly_PossessingAgentId,
                "the keeper must be forced to release the ball at six seconds (Law 12).");
            Assert.Greater(engine.TestOnly_GkReleaseCooldownRemaining, 0,
                "the re-collect cooldown must arm on release, or the keeper picks the ball straight back up.");
        }

        [Test]
        public void GoalkeeperHold_NeverExceedsTheLawLimit_InComposedPlay()
        {
            // (2) The invariant over composed play — the counter running unbounded IS the stall that was
            //     measured at 33.5% of a second half. Non-vacuous: the run is asserted to have actually
            //     put the ball in a keeper's hands at some point.
            var engine = new MatchEngine(MatchSeed);
            int keeper = FirstGoalkeeperAgent(engine);
            int maxHold = 0;

            for (int i = 0; i < 4 * MatchEngineConstants.GkMaxHoldTicks; i++)
            {
                engine.TestOnly_SetPossession(keeper);
                engine.RunTick();
                if (engine.TestOnly_GkHoldTicks > maxHold)
                {
                    maxHold = engine.TestOnly_GkHoldTicks;
                }
                Assert.LessOrEqual(engine.TestOnly_GkHoldTicks, MatchEngineConstants.GkMaxHoldTicks,
                    "a goalkeeper held the ball past Law 12's six seconds — the §5.Z.15 stall is back.");
            }

            Assert.Greater(maxHold, 0, "the run never had a keeper holding the ball — assertion was vacuous.");
        }

        private static int FirstGoalkeeperAgent(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static readonly TacticalDirector.PlayerDatabase.PlayerAttributes NeutralCanonical =
            TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
        /// <summary>
        /// §5.Z.17 / ERR-011-002 coupling lock. The engine keys keeper index directly on team id, but
        /// `MaxGkAgents` is a `[GT]` config read while `TEAM_COUNT` is `[FIXED]` — nothing structural
        /// keeps them equal. If a future config or default breaks the identity, a keeper defends the
        /// wrong end of the pitch (the exact ERR-011-002 defect), so the boot gate must fire.
        /// </summary>
        [Test]
        public void KeeperIndexIsTeamId_CouplingHolds()
        {
            Assert.AreEqual(
                MatchEngineConstants.TEAM_COUNT,
                GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents,
                "Keeper index == team id underpins NotifyKeeperOfShot, HostSaveDispatch and #11's "
                + "own-goal derivation; MatchEngine's boot gate refuses a mismatch.");
        }

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
// | 1.3     | 2026-07-23 | —      | + SaveEpisode_ReArmsAfterBallResolves_CommitsAgain (AR follow- |
// |         |            |        | up): locks the per-episode latch clear → re-commit path (SAVE  |
// |         |            |        | is continuous, so the latch is the sole re-commit guard). Uses |
// |         |            |        | the new TestOnly_SaveCommittedForGk latch seam via the false → |
// |         |            |        | true edge (the sticky LastCommittedSaveAttrs cannot distinguish|
// |         |            |        | a second commit).                                              |
#endregion
