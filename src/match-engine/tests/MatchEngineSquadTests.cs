// File:     src/match-engine/tests/MatchEngineSquadTests.cs
// Created:  2026-07-17
// Modified: 2026-07-17
// Author:   —
// Spec:     Player-attribute projection design supplement §7/§7.1/§9 (KD-P7/KD-P10); Squad/Player
//           Data Layer design supplement (#27) §4 T1; Code Standards #20
// Purpose:  #27 T1 engine-integration locks — ConfigureSquads routes canonical records into every
//           per-slot attribute surface, the no-squad/all-default path stays byte-identical
//           (digest-locked), a distinct squad diverges by design yet deterministically, the
//           substitution bench-swap carries the canonical record, and every fail-loud gate throws.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DecisionTree;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// #27 T1 integration tests for <see cref="MatchEngine.ConfigureSquads"/>.
    /// (Both <c>PlayerAttributes</c> types are reachable from this assembly; the fixture
    /// fully-qualifies the canonical one throughout — the KD-P6 CS0104 discipline.)
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineSquadTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private static int RequiredCount =>
            MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;

        /// <summary>An all-identity squad of exactly the consumed size (starters + bench).</summary>
        private static Squad DefaultSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                players[k] = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
            }
            return new Squad(clubId, players);
        }

        /// <summary>
        /// A squad whose local index <paramref name="distinctLocalIndex"/> carries strongly
        /// non-neutral attributes (every consumed group perturbed); everyone else is identity.
        /// </summary>
        private static Squad SquadWithDistinctPlayer(int clubId, int distinctLocalIndex)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                players[k] = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
            }
            var a = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            a.Pace = 19; a.Acceleration = 18; a.Stamina = 17;          // #2 physical
            a.Passing = 16; a.Technique = 15;                          // #5
            a.Finishing = 14; a.LongShots = 13;                        // #6
            a.Decisions = 12; a.Anticipation = 11;                     // #7/#8
            a.Dribbling = 9;                                           // #8 + attacking normalized
            a.FirstTouchAbility = 8;                                   // #13/#14/#4
            a.WeakFootRating = 5;
            players[distinctLocalIndex].Attributes = a;
            return new Squad(clubId, players);
        }

        private static List<byte[]> RunChain(int ticks, System.Action<MatchEngine> configure)
        {
            var engine = new MatchEngine(MatchSeed);
            configure?.Invoke(engine);
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        // ── KD-P7: the default path is byte-identical (no digest rebaseline) ───────────

        [Test]
        public void AllDefaultSquads_AreBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2; // a couple of strides past the first AI tick

            List<byte[]> unconfigured = RunChain(ticks, configure: null);
            List<byte[]> configured   = RunChain(ticks, configure: e =>
                e.ConfigureSquads(DefaultSquad(7), DefaultSquad(8)));

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(unconfigured[i], configured[i],
                    $"All-CreateDefault squads perturbed the digest at tick {i + 1} — KD-P7 violated.");
            }
        }

        // ── T1 wiring is live: canonical records reach every per-slot surface ──────────

        [Test]
        public void ConfigureSquads_RoutesDistinctRecord_ToEveryPerSlotSurface()
        {
            var engine = new MatchEngine(MatchSeed);
            // Home: distinct outfielder (local 2 ⇒ slot 2). Away: distinct GK-slot player
            // (local 0 ⇒ slot 11) — proves per-team slot mapping and the no-GK-gate rule (KD-P5).
            engine.ConfigureSquads(
                SquadWithDistinctPlayer(7, distinctLocalIndex: 2),
                SquadWithDistinctPlayer(8, distinctLocalIndex: 0));

            int homeSlot = 2;
            int awaySlot = MatchEngineConstants.PLAYERS_PER_TEAM;

            // Canonical record landed on the right slots (and only those).
            Assert.AreEqual(19, engine.TestOnly_CanonicalAttributes(homeSlot).Pace);
            Assert.AreEqual(19, engine.TestOnly_CanonicalAttributes(awaySlot).Pace);
            Assert.AreEqual(10, engine.TestOnly_CanonicalAttributes(0).Pace, "Identity slot must stay neutral.");

            // #2 locomotion projection.
            Assert.AreEqual(19, engine.TestOnly_MovementAttributes(homeSlot).Pace);
            Assert.AreEqual(17, engine.TestOnly_MovementAttributes(homeSlot).Stamina);

            // #8 DT projection (with the slot's real match teamId).
            DtAgentAttributes dt = engine.TestOnly_DtAttributes(awaySlot);
            Assert.AreEqual(12, dt.Decisions);
            Assert.AreEqual(9,  dt.Dribbling);
            Assert.AreEqual(1,  dt.TeamId, "DT TeamId must stay the match-scoped slot identity (KD-P4).");

            // #7 Perception projection.
            Assert.AreEqual(11, engine.TestOnly_PerceptionAttributes(homeSlot).Anticipation);
            Assert.AreEqual(0,  engine.TestOnly_PerceptionAttributes(homeSlot).TeamId);

            // #5/#6 live builders (KickPower derived per KD-P1).
            Assert.AreEqual(16f, engine.TestOnly_PassAttributes(homeSlot).Passing);
            Assert.AreEqual((16 + 15) * 0.5f, engine.TestOnly_PassAttributes(homeSlot).KickPower);
            Assert.AreEqual(5, engine.TestOnly_PassAttributes(homeSlot).WeakFootRating);
            Assert.AreEqual(14, engine.TestOnly_ShotAttributes(awaySlot).Finishing);
        }

        // ── The point of T1: a distinct squad changes the match — deterministically ────

        [Test]
        public void DistinctSquad_DivergesFromDefault_ByDesign()
        {
            const int ticks = 10 * 6; // ten strides — AI + movement act on the varied attributes

            List<byte[]> defaultChain = RunChain(ticks, configure: null);
            List<byte[]> variedChain  = RunChain(ticks, configure: e =>
                e.ConfigureSquads(SquadWithDistinctPlayer(7, 2), SquadWithDistinctPlayer(8, 5)));

            bool diverged = false;
            for (int i = 0; i < ticks && !diverged; i++)
            {
                byte[] a = defaultChain[i];
                byte[] b = variedChain[i];
                for (int j = 0; j < a.Length; j++)
                {
                    if (a[j] != b[j]) { diverged = true; break; }
                }
            }
            Assert.IsTrue(diverged,
                "A strongly non-neutral squad never moved the digest — the T1 wiring is not live.");
        }

        [Test]
        public void DistinctSquad_TwoRuns_AreDeterministic()
        {
            const int ticks = 5 * 6;
            System.Action<MatchEngine> configure = e =>
                e.ConfigureSquads(SquadWithDistinctPlayer(7, 2), SquadWithDistinctPlayer(8, 5));

            List<byte[]> first  = RunChain(ticks, configure);
            List<byte[]> second = RunChain(ticks, configure);

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(first[i], second[i],
                    $"Same squads, same seed diverged at tick {i + 1} — squad sourcing is not deterministic.");
            }
        }

        // ── Substitution carries the canonical bench record (the v2.20 hazard lock) ────

        [Test]
        public void SubstitutePlayer_CopiesCanonicalBenchRecord_AndReprojects()
        {
            var engine = new MatchEngine(MatchSeed);
            // Home bench slot 0 = local index PLAYERS_PER_TEAM carries the distinct record.
            engine.ConfigureSquads(
                SquadWithDistinctPlayer(7, MatchEngineConstants.PLAYERS_PER_TEAM),
                DefaultSquad(8));

            const int outSlot = 3;
            Assert.AreEqual(10, engine.TestOnly_CanonicalAttributes(outSlot).Pace, "Pre-substitution: neutral starter.");

            engine.SubstitutePlayer(0, outSlot, benchIndex: 0, SubstitutionReason.Tactical);

            Assert.AreEqual(19, engine.TestOnly_CanonicalAttributes(outSlot).Pace,
                "The canonical bench record must land on the outgoing slot.");
            Assert.AreEqual(19, engine.TestOnly_MovementAttributes(outSlot).Pace,
                "#2 attrs must be the bench projection.");
            Assert.AreEqual(12, engine.TestOnly_DtAttributes(outSlot).Decisions,
                "#8 attrs must be RE-PROJECTED at substitution (boot-seeded surface).");
            Assert.AreEqual(0, engine.TestOnly_DtAttributes(outSlot).TeamId);
            Assert.AreEqual(11, engine.TestOnly_PerceptionAttributes(outSlot).Anticipation,
                "#7 attrs must be RE-PROJECTED at substitution (boot-seeded surface).");
            Assert.AreEqual(16f, engine.TestOnly_PassAttributes(outSlot).Passing,
                "#5 builder must read the swapped canonical record.");
        }

        // ── Fail-loud gates ────────────────────────────────────────────────────────────

        [Test]
        public void ConfigureSquads_NullSquad_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentNullException>(() => engine.ConfigureSquads(null, DefaultSquad(8)));
            Assert.Throws<System.ArgumentNullException>(() => engine.ConfigureSquads(DefaultSquad(7), null));
        }

        [Test]
        public void ConfigureSquads_TooSmallSquad_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            var small = new Squad(9, new[] { PlayerRecord.CreateDefault(0) });
            Assert.Throws<System.ArgumentException>(() => engine.ConfigureSquads(small, DefaultSquad(8)));
        }

        [Test]
        public void ConfigureSquads_OutOfRangeAttribute_Throws_AndLeavesEngineUntouched()
        {
            var engine = new MatchEngine(MatchSeed);
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                players[k] = PlayerRecord.CreateDefault(k);
            }
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.Vision = 21; // outside [1,20]
            players[4].Attributes = bad;

            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(new Squad(9, players), DefaultSquad(8)));
            // Validate-before-write: no half-applied squad.
            Assert.AreEqual(10, engine.TestOnly_CanonicalAttributes(0).Pace);
        }

        [Test]
        public void ConfigureSquads_InvalidAwaySquad_LeavesHomeUnapplied()
        {
            // The AR-1 M-1 lock of this landing: validation must run for BOTH squads before ANY
            // write — per-team validate-then-apply would land the home squad before refusing away.
            var engine = new MatchEngine(MatchSeed);
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                players[k] = PlayerRecord.CreateDefault(k);
            }
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.Strength = 0; // outside [1,20]
            players[6].Attributes = bad;

            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(SquadWithDistinctPlayer(7, 2), new Squad(9, players)));
            Assert.AreEqual(10, engine.TestOnly_CanonicalAttributes(2).Pace,
                "The valid home squad must NOT have been applied when the away squad is refused.");
        }

        [Test]
        public void ConfigureSquads_WeakFootOutOfRange_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                players[k] = PlayerRecord.CreateDefault(k);
            }
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.WeakFootRating = 6; // outside [1,5]
            players[0].Attributes = bad;

            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(new Squad(9, players), DefaultSquad(8)));
        }

        [Test]
        public void ConfigureSquads_AfterFirstTick_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.RunTick();
            Assert.Throws<System.InvalidOperationException>(() =>
                engine.ConfigureSquads(DefaultSquad(7), DefaultSquad(8)));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-17 | —      | Initial implementation (#27 T1 engine-integration locks).      |
#endregion
