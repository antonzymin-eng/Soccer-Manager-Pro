// File:     src/match-engine/tests/MatchEngineEntryFatigueTests.cs
// Created:  2026-08-06
// Author:   —
// Spec:     Training System #29 §3.3 / §4.3 (KD-1, the match-boot fatigue seam); path-to-playable D2
//           (T2); Code Standards #20
// Purpose:  Locks the four-argument ConfigureSquads overload: entry fatigue lands on the starter's
//           aerobic reservoir on the project's 0-rested / 1-fatigued convention, null and an all-zero
//           array are both exactly the rested boot, a non-zero value actually reaches the simulation,
//           it survives a save/restore, and every malformed array fails loud rather than being clamped.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// #29 T2 locks for the four-argument <c>MatchEngine.ConfigureSquads</c>.
    /// <para>
    /// The rosters are the KD-L5 position-coherent 18 that <c>MatchEngineSquadTests</c> uses, for which
    /// all-neutral lineup selection reproduces roster order — so starter slot <c>k</c> holds local
    /// index <c>k</c> and a per-local-index assertion is meaningful without reaching into the internal
    /// selector.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineEntryFatigueTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

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

        private static Squad CoherentSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(
                    clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);
                players[k] = p;
            }

            return new Squad(clubId, players);
        }

        /// <summary>The two rosters this fixture configures, as the resolver a restore needs (#27 T3 / KD-3).</summary>
        private sealed class TwoClubProvider : ISquadProvider
        {
            public Squad ResolveByClubId(int clubId) =>
                clubId == 1 || clubId == 2 ? CoherentSquad(clubId) : null;
        }

        private static float[] Rested() => new float[RequiredCount];

        private static List<byte[]> DigestChain(MatchEngine engine, int ticks)
        {
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }

            return chain;
        }

        // ── the seam itself ────────────────────────────────────────────────────────────────

        [Test]
        public void EntryFatigue_LandsOnTheStartersAerobicReservoir()
        {
            var engine = new MatchEngine(MatchSeed);
            float[] home = Rested();
            home[3] = 0.25f;   // one defender arrives a quarter spent

            engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), home, null);

            Assert.AreEqual(0.75f, engine.AgentView(3).AerobicPool, 1e-6f,
                "AerobicPool must be 1 − fatigue (0 = rested, 1 = fully fatigued).");
            Assert.AreEqual(1f, engine.AgentView(2).AerobicPool, 1e-6f,
                "A team-mate with zero entry fatigue must still boot fully rested.");
        }

        [Test]
        public void EntryFatigue_IsPerTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            float[] away = Rested();
            away[0] = 1f;   // the away keeper arrives fully spent

            engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), null, away);

            int awayKeeper = MatchEngineConstants.PLAYERS_PER_TEAM;
            Assert.AreEqual(0f, engine.AgentView(awayKeeper).AerobicPool, 1e-6f);
            Assert.AreEqual(1f, engine.AgentView(0).AerobicPool, 1e-6f,
                "The home side was passed null and must be untouched.");
        }

        // ── the neutrality floor ───────────────────────────────────────────────────────────

        [Test]
        public void NullFatigue_IsExactlyTheRestedBoot()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), null, null);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(1f, engine.AgentView(i).AerobicPool, 0f,
                    $"Agent {i}: no entry fatigue must leave the boot reservoir untouched.");
            }
        }

        [Test]
        public void AllZeroFatigue_IsDigestIdenticalToTheTwoArgumentOverload()
        {
            // The load-bearing neutrality claim: every player starts on Balanced, whose daily training
            // load equals the passive recovery exactly, so #29 projects 0 for a whole career of
            // untouched focuses — and a season wired through this seam must then be indistinguishable
            // from one that never had it. Compared over a digest chain rather than the reservoir alone,
            // so a stray write anywhere else in ApplySquad would show up too.
            var wired = new MatchEngine(MatchSeed);
            wired.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), Rested(), Rested());

            var bare = new MatchEngine(MatchSeed);
            bare.ConfigureSquads(CoherentSquad(1), CoherentSquad(2));

            List<byte[]> wiredChain = DigestChain(wired, 40);
            List<byte[]> bareChain = DigestChain(bare, 40);

            for (int i = 0; i < bareChain.Count; i++)
            {
                CollectionAssert.AreEqual(bareChain[i], wiredChain[i],
                    $"Tick {i + 1}: an all-rested entry-fatigue array must be indistinguishable from "
                    + "passing none.");
            }
        }

        [Test]
        public void NonZeroFatigue_ReachesTheSimulation()
        {
            // The counterpart of the neutrality lock, and the one that would fail if the seam were
            // written but never read: a fatigued side must diverge from a rested one. Without this,
            // "all-rested is identical" is satisfiable by a seam that does nothing at all.
            var fatigued = new MatchEngine(MatchSeed);
            float[] home = Rested();
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                home[k] = 0.9f;
            }

            fatigued.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), home, null);

            var rested = new MatchEngine(MatchSeed);
            rested.ConfigureSquads(CoherentSquad(1), CoherentSquad(2));

            DigestChain(fatigued, 240);
            DigestChain(rested, 240);

            // Asserted on POSITION rather than on the digest: the reservoir is itself part of the
            // serialized state, so a digest would differ even from a seam that were written and never
            // read. A position can only diverge through AgentLocomotion.CalculateAerobicModifier —
            // that is, through the reservoir actually being consumed.
            bool moved = false;
            for (int i = 0; i < MatchEngineConstants.PLAYERS_PER_TEAM && !moved; i++)
            {
                moved = rested.AgentView(i).Position.x != fatigued.AgentView(i).Position.x
                        || rested.AgentView(i).Position.y != fatigued.AgentView(i).Position.y;
            }

            Assert.IsTrue(moved,
                "A side booted at 0.9 fatigue must move differently from a rested one — otherwise the "
                + "reservoir is being written and never read.");
        }

        [Test]
        public void EntryFatigue_SurvivesASaveRestoreRoundTrip()
        {
            // The reservoir was already serialized, which is exactly why this seam needs no schema
            // change — but "already serialized" is the sort of claim worth executing once.
            var engine = new MatchEngine(MatchSeed);
            float[] home = Rested();
            home[7] = 0.4f;
            engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), home, null);
            engine.RunTick();

            byte[] blob = MatchSaveManager.Encode(engine);
            MatchEngine restored = MatchSaveManager.Restore(blob, new TwoClubProvider());

            Assert.AreEqual(
                engine.AgentView(7).AerobicPool, restored.AgentView(7).AerobicPool, 0f,
                "A boot-fatigued reservoir must round-trip exactly.");
        }

        // ── the fail-loud gates ────────────────────────────────────────────────────────────

        [Test]
        public void WrongLengthFatigueArray_FailsLoud()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(
                    CoherentSquad(1), CoherentSquad(2), new float[RequiredCount - 1], null),
                "A length mismatch means the array was built against a different squad — the one "
                + "failure mode that silently mis-fatigues the wrong player.");
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void OutOfRangeFatigue_FailsLoud(float value)
        {
            var engine = new MatchEngine(MatchSeed);
            float[] home = Rested();
            home[5] = value;

            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), home, null));
        }

        [Test]
        public void RefusedFatigueArray_LeavesTheEngineUnconfigured()
        {
            // The validate-both-before-write discipline ConfigureSquads already applies to squads:
            // a refused AWAY array must not leave the home side half-applied.
            var engine = new MatchEngine(MatchSeed);
            byte[] before = engine.CurrentSnapshotDigest;

            float[] badAway = Rested();
            badAway[0] = 2f;
            Assert.Throws<System.ArgumentException>(() =>
                engine.ConfigureSquads(CoherentSquad(1), CoherentSquad(2), Rested(), badAway));

            CollectionAssert.AreEqual(before, engine.CurrentSnapshotDigest,
                "A refused call must leave the engine exactly as it was.");
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(1f, engine.AgentView(i).AerobicPool, 0f,
                    $"Agent {i}: a refused call must not have written a reservoir.");
            }
        }

    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial (#29 T2): the match-boot entry-fatigue seam — the          |
// |         |            |        | reservoir mapping, per-team routing, the all-rested digest-        |
// |         |            |        | identity floor AND its counterpart (a fatigued side must actually  |
// |         |            |        | diverge), the save/restore round-trip, and the fail-loud gates.    |
#endregion
