// File:     src/match-engine/tests/MatchEngineSquadTests.cs
// Created:  2026-07-17
// Modified: 2026-07-18 (#27 T3 — roster-reference locks; the T1 KD-P7 all-default byte-identity lock superseded by KD-T3-2)
// Modified: 2026-07-18 (#27 T3 post-landing code AR — observable-state behavioural-neutrality lock restored)
// Modified: 2026-07-19 (#27 lineup selection Plan-3 — squads now position-coherent (KD-L5 layout); distinct-player routing follows selection (best-of-line → first slot); substitution forces the distinct record onto the bench; mis-ordered-GK lock (KD-L4))
// Author:   —
// Spec:     Player-attribute projection design supplement §7/§7.1/§9 (KD-P7/KD-P10); Squad/Player
//           Data Layer design supplement (#27) §4 T1/T3; squad-roster-reference-design.md (T3,
//           KD-T3-1/KD-T3-2); Code Standards #20
// Purpose:  #27 T1/T3 engine-integration locks — ConfigureSquads routes canonical records into every
//           per-slot attribute surface, a distinct squad diverges by design yet deterministically, the
//           substitution bench-swap carries the canonical record, every fail-loud gate throws, and the
//           T3 per-team roster reference captures squad identity (a configured squad is digest-
//           distinguishable from unconfigured while behaviour stays neutral).

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

        /// <summary>
        /// Coarse position for a standard position-coherent roster of exactly <see cref="RequiredCount"/>
        /// (18) players, ordered so that all-neutral lineup selection reproduces roster order (KD-L5):
        /// slot 0 = goalkeeper, 1..4 = defenders, 5..8 = midfielders, 9..10 = forwards (the F442 starter
        /// shape), 11..17 = a position-varied bench (all higher PlayerId, so they never displace an
        /// intended starter of the same position). No bench goalkeeper — matching the boot all-false
        /// <c>_benchIsGoalkeeper</c> so a config-default squad stays behaviour-neutral.
        /// </summary>
        private static PlayerPosition PosFor(int localIndex)
        {
            if (localIndex == 0)  return PlayerPosition.Goalkeeper;
            if (localIndex <= 4)  return PlayerPosition.Defender;
            if (localIndex <= 8)  return PlayerPosition.Midfielder;
            if (localIndex <= 10) return PlayerPosition.Forward;
            switch ((localIndex - 11) % 3)   // bench filler 11..17: Def / Mid / Fwd, no GK
            {
                case 0:  return PlayerPosition.Defender;
                case 1:  return PlayerPosition.Midfielder;
                default: return PlayerPosition.Forward;
            }
        }

        /// <summary>An all-identity, position-coherent roster of the consumed size (KD-L5 layout).</summary>
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

        /// <summary>Strongly non-neutral attributes (every consumed group perturbed) — the recognizable
        /// distinct record the routing/substitution locks detect. Mean rating ≈ 11.35 (&gt; neutral 10).</summary>
        private static TacticalDirector.PlayerDatabase.PlayerAttributes DistinctAttributes()
        {
            var a = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            a.Pace = 19; a.Acceleration = 18; a.Stamina = 17;          // #2 physical
            a.Passing = 16; a.Technique = 15;                          // #5
            a.Finishing = 14; a.LongShots = 13;                        // #6
            a.Decisions = 12; a.Anticipation = 11;                     // #7/#8
            a.Dribbling = 9;                                           // #8 + attacking normalized
            a.FirstTouchAbility = 8;                                   // #13/#14/#4
            a.WeakFootRating = 5;
            return a;
        }

        /// <summary>An all-identity, position-coherent squad of exactly the consumed size (starters + bench).</summary>
        private static Squad DefaultSquad(int clubId)
        {
            return new Squad(clubId, CoherentPlayers(clubId));
        }

        /// <summary>
        /// A position-coherent squad whose local index <paramref name="distinctLocalIndex"/> carries the
        /// strongly non-neutral <see cref="DistinctAttributes"/>; everyone else is identity. Because the
        /// distinct player is the highest-rated of its coarse position, proper selection places it on the
        /// FIRST slot of its line (KD-L2) — the caller reads its resulting slot accordingly.
        /// </summary>
        private static Squad SquadWithDistinctPlayer(int clubId, int distinctLocalIndex)
        {
            PlayerRecord[] players = CoherentPlayers(clubId);
            players[distinctLocalIndex].Attributes = DistinctAttributes();
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

        // ── #27 T3 (KD-T3-2): the roster reference captures squad identity ─────────────
        // Supersedes the T1 KD-P7 "config-default == unconfigured" byte-identity lock: T3 serializes
        // the per-team roster reference (v16), so a configured match is digest-distinguishable from an
        // unconfigured one BY DESIGN (the reference is identity, not attributes). Behavioural
        // neutrality still holds — an all-CreateDefault squad moves agents identically — so the ONLY
        // digest difference is the identity field, present from tick 1 (before any behavioural
        // divergence could exist).

        [Test]
        public void ConfiguredDefaultSquad_CapturesRosterIdentity_DivergesFromUnconfiguredAtTick1()
        {
            List<byte[]> unconfigured = RunChain(1, configure: null);
            List<byte[]> configured   = RunChain(1, configure: e =>
                e.ConfigureSquads(DefaultSquad(7), DefaultSquad(8)));

            // Neutral attributes ⇒ identical movement, so the tick-1 world state is behaviourally
            // identical; ANY tick-1 digest difference is the v16 roster reference alone (KD-T3-2).
            CollectionAssert.AreNotEqual(unconfigured[0], configured[0],
                "A configured squad was digest-identical to unconfigured at tick 1 — the v16 roster " +
                "reference is not captured (KD-T3-2).");
        }

        [Test]
        public void ConfiguredDefaultSquad_IsBehaviourNeutral_ObservableStateMatchesUnconfigured()
        {
            // KD-T3-2 behavioural half: T3 makes the config-default DIGEST diverge from unconfigured
            // (the roster reference), but that difference must be NON-behavioural — an all-CreateDefault
            // squad projects to the neutral seeds (KD-P7), so gameplay is unchanged. Locked here at the
            // observable level (stronger than the digest, which the roster field deliberately perturbs):
            // ball + every agent position match tick-for-tick. This restores the behavioural-neutrality
            // guarantee the superseded T1 byte-identity digest lock provided — minus the roster field a
            // digest comparison can no longer exclude. (Vector Equals is exact; two same-seed, same-
            // neutral-attribute runs are bit-identical, so exact equality holds.)
            const int ticks = 2 * 6 * 2;

            var unconfigured = new MatchEngine(MatchSeed);
            var configured   = new MatchEngine(MatchSeed);
            configured.ConfigureSquads(DefaultSquad(7), DefaultSquad(8));

            for (int i = 0; i < ticks; i++)
            {
                unconfigured.RunTick();
                configured.RunTick();

                Assert.AreEqual(unconfigured.BallView.Position, configured.BallView.Position,
                    $"Ball position diverged at tick {i + 1} — a config-default squad is not behaviour-neutral.");
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    Assert.AreEqual(unconfigured.AgentView(a).Position, configured.AgentView(a).Position,
                        $"Agent {a} position diverged at tick {i + 1} — a config-default squad is not behaviour-neutral.");
                }
            }
        }

        [Test]
        public void ConfiguredDefaultSquads_SameClubIds_AreDeterministic()
        {
            // Behavioural neutrality survives T3: same squads, same seed ⇒ byte-identical every tick.
            const int ticks = 5 * 6;
            System.Action<MatchEngine> configure = e =>
                e.ConfigureSquads(DefaultSquad(7), DefaultSquad(8));

            List<byte[]> first  = RunChain(ticks, configure);
            List<byte[]> second = RunChain(ticks, configure);

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(first[i], second[i],
                    $"Same squads, same seed diverged at tick {i + 1} — configuration is not deterministic.");
            }
        }

        [Test]
        public void ConfiguredSquads_DistinctClubIds_DivergeAtTick1()
        {
            // The reference records the ACTUAL club id, not merely "configured vs not": two all-neutral
            // configurations differing ONLY in ClubId diverge from the first tick (KD-T3-1).
            List<byte[]> clubsA = RunChain(1, e => e.ConfigureSquads(DefaultSquad(7),   DefaultSquad(8)));
            List<byte[]> clubsB = RunChain(1, e => e.ConfigureSquads(DefaultSquad(100), DefaultSquad(101)));

            CollectionAssert.AreNotEqual(clubsA[0], clubsB[0],
                "Two all-neutral squads differing only in ClubId were digest-identical — the roster " +
                "reference does not record the actual club id (KD-T3-1).");
        }

        [Test]
        public void RosterReference_SentinelBeforeConfigure_ClubIdAfterConfigure()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.AreEqual(MatchEngineConstants.NO_ROSTER_CLUB_ID, engine.TestOnly_RosterClubId(0));
            Assert.AreEqual(MatchEngineConstants.NO_ROSTER_CLUB_ID, engine.TestOnly_RosterClubId(1));

            engine.ConfigureSquads(DefaultSquad(7), DefaultSquad(8));
            Assert.AreEqual(7, engine.TestOnly_RosterClubId(0));
            Assert.AreEqual(8, engine.TestOnly_RosterClubId(1));
        }

        // ── T1 wiring is live: canonical records reach every per-slot surface ──────────

        [Test]
        public void ConfigureSquads_RoutesDistinctRecord_ToEveryPerSlotSurface()
        {
            var engine = new MatchEngine(MatchSeed);
            // Home: distinct DEFENDER (local 2). As the highest-rated defender it is selected for the
            // first defender slot — pitch slot 1 (KD-L2), not roster slot 2. Away: distinct GOALKEEPER
            // (local 0) ⇒ the GK slot (global slot 11) — proves per-team slot mapping through selection.
            engine.ConfigureSquads(
                SquadWithDistinctPlayer(7, distinctLocalIndex: 2),
                SquadWithDistinctPlayer(8, distinctLocalIndex: 0));

            int homeSlot = 1;
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
            // Under proper selection a high-rated player STARTS, so to land the distinct record on the
            // bench it must be an excess at its position, out-rated by the starters there: the two
            // starter forward slots are taken by two rating-12 forwards (indices 9,10), pushing the
            // DISTINCT forward (rating ≈ 11.35, index 11) onto the bench — where it is the best
            // remaining player, so bench slot 0 (KD-L3 best-remaining bench fill).
            PlayerRecord[] players = CoherentPlayers(7);
            var strongFwd = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            int[] all12 = new int[TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.ATTRIBUTE_COUNT];
            for (int f = 0; f < all12.Length; f++) { all12[f] = 12; }
            strongFwd.FromArray(all12);                                  // rating 12 > distinct ≈ 11.35
            players[9].Attributes  = strongFwd;
            players[10].Attributes = strongFwd;
            players[11].Position   = PlayerPosition.Forward;             // excess forward → benched
            players[11].Attributes = DistinctAttributes();
            engine.ConfigureSquads(new Squad(7, players), DefaultSquad(8));

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

        // ── Lineup selection places the goalkeeper by position, not roster index (KD-L4) ─

        [Test]
        public void MisOrderedSquad_SelectsGoalkeeperForGkSlot_NotIndexZero()
        {
            // A squad whose goalkeeper is NOT at roster index 0. The old roster-order mapping flagged
            // pitch slot 0 as GK while seeding it with the index-0 outfielder (a silent mis-seed);
            // proper selection puts the actual goalkeeper in the GK slot and flags it (KD-L4).
            var engine = new MatchEngine(MatchSeed);
            PlayerRecord[] players = CoherentPlayers(7);
            players[0].Position = PlayerPosition.Defender;            // index 0 is now an outfielder
            var gk = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            gk.Pace = 13;                                             // recognizable GK record
            players[7].Position   = PlayerPosition.Goalkeeper;        // the only goalkeeper, at index 7
            players[7].Attributes = gk;
            engine.ConfigureSquads(new Squad(7, players), DefaultSquad(8));

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(0),
                "Pitch slot 0 must be flagged goalkeeper.");
            Assert.AreEqual(13, engine.TestOnly_CanonicalAttributes(0).Pace,
                "The GK slot must carry the actual goalkeeper (index 7), not the index-0 outfielder (KD-L4).");
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
            PlayerRecord[] players = CoherentPlayers(9);   // coherent ⇒ selection succeeds, the bounds gate fires
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.Vision = 21; // outside [1,20]
            players[4].Attributes = bad;   // index 4 (a defender) is a selected starter

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
            PlayerRecord[] players = CoherentPlayers(9);   // coherent away squad ⇒ the bounds gate fires
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.Strength = 0; // outside [1,20]
            players[6].Attributes = bad;   // index 6 (a midfielder) is a selected starter

            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(SquadWithDistinctPlayer(7, 2), new Squad(9, players)));
            // The valid home squad's distinct defender would land on pitch slot 1 if applied; it stays
            // neutral, proving home was NOT applied when the away squad is refused.
            Assert.AreEqual(10, engine.TestOnly_CanonicalAttributes(1).Pace,
                "The valid home squad must NOT have been applied when the away squad is refused.");
            // #27 T3: a refused ConfigureSquads leaves the roster reference at the sentinel (set only
            // after both squads validate-and-apply).
            Assert.AreEqual(MatchEngineConstants.NO_ROSTER_CLUB_ID, engine.TestOnly_RosterClubId(0),
                "The roster reference must stay the sentinel when ConfigureSquads is refused.");
        }

        [Test]
        public void ConfigureSquads_WeakFootOutOfRange_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            PlayerRecord[] players = CoherentPlayers(9);   // coherent ⇒ selection succeeds, the bounds gate fires
            var bad = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            bad.WeakFootRating = 6; // outside [1,5]
            players[0].Attributes = bad;   // index 0 (the goalkeeper) is a selected starter

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
// | 1.1     | 2026-07-18 | —      | #27 T3: the T1 AllDefaultSquads_AreBehaviourNeutral_Digest-     |
// |         |            |        | Unchanged lock (config-default == unconfigured) is SUPERSEDED   |
// |         |            |        | by KD-T3-2 — replaced with ConfiguredDefaultSquad_Captures-     |
// |         |            |        | RosterIdentity_DivergesFromUnconfiguredAtTick1, ConfiguredDef-  |
// |         |            |        | aultSquads_SameClubIds_AreDeterministic, ConfiguredSquads_      |
// |         |            |        | DistinctClubIds_DivergeAtTick1, RosterReference_SentinelBefore- |
// |         |            |        | Configure_ClubIdAfterConfigure; the invalid-away refusal test   |
// |         |            |        | also asserts the reference stays the sentinel.                 |
// | 1.2     | 2026-07-18 | —      | #27 T3 post-landing code AR (0H+0M+1L): replacing the T1        |
// |         |            |        | byte-identity digest lock dropped the DIRECT match-level proof  |
// |         |            |        | that config-default is behaviourally identical to unconfigured  |
// |         |            |        | (the roster field is the SOLE, non-behavioural difference — the |
// |         |            |        | new digest-divergence tests prove divergence, not that it is    |
// |         |            |        | non-behavioural). Added ConfiguredDefaultSquad_IsBehaviour-     |
// |         |            |        | Neutral_ObservableStateMatchesUnconfigured — ball + every agent |
// |         |            |        | position match tick-for-tick (observable level, which the       |
// |         |            |        | roster field does not touch), restoring the KD-T3-2 neutrality  |
// |         |            |        | half at a signal a digest comparison can no longer isolate.     |
// | 1.3     | 2026-07-19 | —      | #27 lineup selection Plan-3: helpers build position-coherent    |
// |         |            |        | squads (PosFor/CoherentPlayers, KD-L5 layout) so all-neutral    |
// |         |            |        | selection reproduces roster order (config-default stays         |
// |         |            |        | behaviour-neutral). Routing lock reads the distinct defender at |
// |         |            |        | its line's first slot (slot 1); substitution lock forces the    |
// |         |            |        | distinct record onto the bench via two stronger starter         |
// |         |            |        | forwards; fail-loud attribute/weak-foot gates use coherent      |
// |         |            |        | squads so the bounds gate fires (not the position gate); added  |
// |         |            |        | MisOrderedSquad_SelectsGoalkeeperForGkSlot_NotIndexZero (KD-L4).|
#endregion
