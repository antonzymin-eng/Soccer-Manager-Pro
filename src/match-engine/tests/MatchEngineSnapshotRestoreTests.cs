// File:     src/match-engine/tests/MatchEngineSnapshotRestoreTests.cs
// Created:  2026-07-20
// Author:   —
// Spec:     Snapshot-deserialize design note (docs/tracking/snapshot-deserialize-design.md) §5 Phase 1/2
//           (G3 round-trip determinism, KD-1/KD-3/KD-4/KD-5/KD-8); Match Engine design note §5 Phase G; Code Standards #20
// Purpose:  Phase 1/2 acceptance tests for the snapshot-deserialize reader — the G3 round-trip determinism
//           contract: save at tick N -> RestoreFromSnapshot -> tick to N+K produces a digest chain
//           byte-identical to an uninterrupted run ticked to N+K (KD-5), for the neutral path AND a
//           distinct-squad match (Phase 2 re-projection via ISquadProvider, incl. mid-match, post-restore,
//           and goalkeeper substitutions). Plus the KD-1 version-gate / trailing-byte fail-loud guards and
//           the KD-3 distinct-squad provider gates.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase 1/2 snapshot-deserialize (save/restore) tests for <see cref="MatchEngine"/>. The central
    /// property is G3 round-trip determinism (<see cref="AssertRoundTripDeterministic"/>): a restored
    /// engine continues the digest chain byte-for-byte, which is the single test that proves the reader
    /// captured EVERY cross-tick field — any omitted/mis-ordered field diverges the chain within K ticks.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineSnapshotRestoreTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // A bold, fully non-Balanced tactic (mirrors MatchEngineTacticTests.Attacking()).
        private static TeamTactic Attacking() => new TeamTactic(
            Mentality.VeryAttacking, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Direct, TacticPressing.High, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        private static TeamTactic Defending() => new TeamTactic(
            Mentality.VeryDefensive, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Short, TacticPressing.Low, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        /// <summary>
        /// The G3 acceptance driver. Boots engine A with the given pre-kickoff <paramref name="setup"/>,
        /// ticks it to N (optionally running <paramref name="midRun"/> at <paramref name="midRunTick"/>),
        /// captures a durable (header, payload) save, then:
        ///  (a) continues A for K ticks to build the REFERENCE digest chain (N+1 … N+K), and
        ///  (b) produces a fresh engine C via <see cref="MatchEngine.RestoreFromSnapshot"/> and ticks it K
        ///      times, asserting C's digest at every tick equals A's at the same tick.
        /// C is produced solely by the factory — no separately-ticked engine is needed; A is kept running
        /// only to produce the reference chain to compare against (design note KD-5).
        /// </summary>
        private static void AssertRoundTripDeterministic(
            Action<MatchEngine> setup, int n, int k,
            ISquadProvider squads = null, Action<MatchEngine> midRun = null, int midRunTick = 0,
            Action<MatchEngine> afterSaveBoth = null)
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            setup?.Invoke(a);

            for (int i = 0; i < n; i++)
            {
                if (midRun != null && a.CurrentTick == (ulong)midRunTick)
                {
                    midRun(a);
                }
                a.RunTick();
            }
            Assert.AreEqual((ulong)n, a.CurrentTick, "Engine A must be at tick N after N ticks.");

            SnapshotHeader header = a.TestOnly_CaptureDurableHeader();
            SnapshotPayload payload = a.TestOnly_CaptureDurablePayload();

            // afterSaveBoth applies the SAME action to the reference engine A (at tick N) and to the restored
            // engine C (at tick N, post-restore), so the compared chains stay aligned — it exercises
            // operations that depend on restored-but-not-serialized state (e.g. a post-restore substitution
            // pulling a re-projected bench record + its bench-GK flag).
            afterSaveBoth?.Invoke(a);

            var reference = new List<byte[]>(k);
            for (int i = 0; i < k; i++)
            {
                a.RunTick();
                reference.Add(a.CurrentSnapshotDigest);
            }

            MatchEngine c = MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed, squads);
            Assert.AreEqual((ulong)n, c.CurrentTick,
                "The restored engine's clock must resume at the saved tick N (KD-5).");
            afterSaveBoth?.Invoke(c);

            for (int i = 0; i < k; i++)
            {
                c.RunTick();
                CollectionAssert.AreEqual(
                    reference[i], c.CurrentSnapshotDigest,
                    $"Round-trip digest diverged at tick {n + i + 1} — the restore is not byte-identical " +
                    "to an uninterrupted run (a cross-tick field was omitted or mis-ordered).");
            }
        }

        [Test]
        public void RoundTrip_NeutralKickoff_IsDeterministic()
        {
            // The bulk-of-value case: every match that never calls ConfigureSquads (the Phase 1 scope).
            AssertRoundTripDeterministic(setup: null, n: 300, k: 120);
        }

        [Test]
        public void RoundTrip_MidMatchTacticsChanged_IsDeterministic()
        {
            // Exercises the active + pending TeamTactic / PlayerTactic serialization (v9/v10): a tactic
            // staged mid-match must survive the save so a restored match resumes with the same active
            // (and, if between a SetTeamTactic and its stride commit, the same pending) tactic.
            AssertRoundTripDeterministic(
                setup: e =>
                {
                    e.SetTeamTactic(0, Attacking());
                    e.SetTeamTactic(1, Defending());
                },
                n: 250, k: 120);
        }

        [Test]
        public void RoundTrip_BookingCursorBeforeSnapshot_IsDeterministic()
        {
            // The KD-8 / AR-3 H-1 regression: the match-flow.card-severity RNG stream cursor is cross-tick
            // state serialized at v17 and fed into the digest EVERY tick. A match with a booking before the
            // snapshot has a non-zero cursor; if the reader failed to restore it, the restored engine would
            // serialize cursor 0 while the reference serializes the saved cursor, diverging the very first
            // compared tick. Pre-advancing the cursor here directly locks that the reader restores it.
            AssertRoundTripDeterministic(
                setup: e => e.TestOnly_SetCardSeverityStreamCursor(rngCursor: 12345UL, actionOrdinal: 7UL),
                n: 200, k: 90);
        }

        [Test]
        public void RestoreFromSnapshot_WrongSchemaVersion_FailsLoud()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < 60; i++) a.RunTick();

            SnapshotHeader header = a.TestOnly_CaptureDurableHeader();
            SnapshotPayload payload = a.TestOnly_CaptureDurablePayload();

            // Corrupt the schema version (the first u32 of the payload) — the reader's first read.
            payload.PayloadBytes[0] ^= 0xFF;

            Assert.Throws<InvalidOperationException>(
                () => MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed),
                "A schema-version mismatch must fail loud (KD-1 — no cross-version migration at Stage 0).");
        }

        [Test]
        public void RestoreFromSnapshot_TrailingByteMismatch_FailsLoud()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < 60; i++) a.RunTick();

            SnapshotHeader header = a.TestOnly_CaptureDurableHeader();
            SnapshotPayload payload = a.TestOnly_CaptureDurablePayload();

            // Claim one extra byte — the reader consumes the true field set (o < BytesWritten) and must
            // fail loud rather than silently accept a short/long read (KD-1 / R1 trailing-byte guard).
            payload.BytesWritten += 1;

            Assert.Throws<InvalidOperationException>(
                () => MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed),
                "A payload byte-count mismatch must fail loud (writer/reader field drift, R1).");
        }

        [Test]
        public void RoundTrip_DistinctSquad_IsDeterministic()
        {
            // Phase 2 (#27 T3 / KD-3): a ConfigureSquads-booted match with DISTINCT (varied-attribute)
            // squads. The attribute records are not serialized, so the restore must re-project them from
            // the rosters the provider supplies. If re-projection used CreateDefault() (or the wrong
            // roster), the varied attributes would drive different agent behaviour and diverge the chain.
            Squad home = DistinctSquad(1);
            Squad away = DistinctSquad(2);
            AssertRoundTripDeterministic(
                setup: e => e.ConfigureSquads(home, away),
                n: 200, k: 90,
                squads: Provider(home, away));
        }

        [Test]
        public void RoundTrip_DistinctSquadWithSubstitution_IsDeterministic()
        {
            // Bench-swap fidelity (KD-3 / KD-P10): a mid-match substitution changes which player occupies a
            // pitch slot. Only the serialized _activeBenchSlot records that; on restore the bench player's
            // attributes must be re-projected onto the swapped slot (ReprojectSubstitutions), not the
            // starter's. A wrong replay diverges the chain within a few ticks of the restore.
            Squad home = DistinctSquad(1);
            Squad away = DistinctSquad(2);
            AssertRoundTripDeterministic(
                setup: e => e.ConfigureSquads(home, away),
                n: 200, k: 90,
                squads: Provider(home, away),
                midRun: e => e.SubstitutePlayer(0, 5, 0, SubstitutionReason.Tactical),
                midRunTick: 50);
        }

        [Test]
        public void RoundTrip_DistinctSquad_PostRestoreSubstitution_IsDeterministic()
        {
            // Post-restore bench-attribute fidelity: the substitution happens AFTER the restore (on both the
            // reference and the restored engine), so it pulls the re-projected bench record. If the bench
            // attribute arrays were not re-projected, the swapped-in player's behaviour would diverge.
            Squad home = DistinctSquad(1);
            Squad away = DistinctSquad(2);
            AssertRoundTripDeterministic(
                setup: e => e.ConfigureSquads(home, away),
                n: 150, k: 90,
                squads: Provider(home, away),
                afterSaveBoth: e => e.SubstitutePlayer(0, 5, 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void RoundTrip_DistinctSquad_PostRestoreGkSubstitution_IsDeterministic()
        {
            // Locks the bench-GK-flag re-projection: _benchIsGoalkeeper is a boot-constant NOT serialized, so
            // a restored engine that failed to re-project it would bring the spare goalkeeper on (onto the
            // goalkeeper slot 0) as an OUTFIELDER (IsGoalkeeper == false), flipping slot 0's GK-ness and
            // diverging from the reference. With the re-projection the flag is restored, slot 0 stays a
            // goalkeeper, and the chain matches. Bench slot 0 is the spare goalkeeper by construction
            // (SquadWithBenchGoalkeeper); the substitution is a realistic keeper-for-keeper swap.
            Squad home = SquadWithBenchGoalkeeper(1);
            Squad away = DistinctSquad(2);
            AssertRoundTripDeterministic(
                setup: e => e.ConfigureSquads(home, away),
                n: 150, k: 90,
                squads: Provider(home, away),
                afterSaveBoth: e => e.SubstitutePlayer(0, 0, 0, SubstitutionReason.Injury));
        }

        [Test]
        public void RestoreFromSnapshot_DistinctSquadNoProvider_FailsLoud()
        {
            // A ConfigureSquads-booted match references a distinct roster; restoring without a provider
            // cannot re-project it and must fail loud rather than silently fall back to CreateDefault()
            // and diverge (KD-3 / R4).
            (SnapshotHeader header, SnapshotPayload payload) = SaveConfiguredMatch();

            Assert.Throws<NotSupportedException>(
                () => MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed),
                "A distinct-squad snapshot restored with no ISquadProvider must fail loud (KD-3).");
        }

        [Test]
        public void RestoreFromSnapshot_DistinctSquadUnknownClubId_FailsLoud()
        {
            // The provider does not know the referenced ClubId — it returns null, which the factory must
            // refuse rather than paper over with a default roster (KD-3 / R4).
            (SnapshotHeader header, SnapshotPayload payload) = SaveConfiguredMatch();

            Assert.Throws<NotSupportedException>(
                () => MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed, new EmptySquadProvider()),
                "A provider that cannot resolve a referenced ClubId must fail loud (KD-3).");
        }

        [Test]
        public void RestoreFromSnapshot_DistinctSquadMismatchedRoster_FailsLoud()
        {
            // A resolver-contract violation: the provider returns a Squad whose ClubId is not the one
            // requested. The factory verifies the returned ClubId and refuses the mismatch.
            (SnapshotHeader header, SnapshotPayload payload) = SaveConfiguredMatch();

            Assert.Throws<InvalidOperationException>(
                () => MatchEngine.RestoreFromSnapshot(
                    header, payload, MatchSeed, new WrongClubIdSquadProvider(DistinctSquad(99))),
                "A provider returning a Squad with a mismatched ClubId must fail loud (KD-3 resolver contract).");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

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

        /// <summary>A position-coherent squad whose players carry DISTINCT (varied but in-range) attributes,
        /// so a restore that failed to re-project the real roster would diverge from the saved run.</summary>
        private static Squad DistinctSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);

                int[] a = new int[31];
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = 1 + ((k * 7 + f * 3 + clubId) % 20);   // deterministic, in [1,20]
                }
                var attrs = new PlayerAttributes();
                attrs.FromArray(a);
                attrs.WeakFootRating = 1 + ((k + clubId) % 5);    // in [1,5]
                p.Attributes = attrs;

                players[k] = p;
            }
            return new Squad(clubId, players);
        }

        /// <summary>A distinct squad carrying a SPARE goalkeeper who lands on bench slot 0: local 0 is the
        /// top-rated goalkeeper (starts), local 11 is a slightly-lower goalkeeper who is the best remaining
        /// after the eleven starters are chosen (so LineupSelector places it in bench slot 0,
        /// <c>BenchIsGoalkeeper[0] == true</c>). Both keepers rate above every outfielder.</summary>
        private static Squad SquadWithBenchGoalkeeper(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = k == 11 ? PlayerPosition.Goalkeeper : PosFor(k);

                int fill = k == 0 ? 20 : (k == 11 ? 19 : 1 + ((k * 7 + clubId) % 15));
                int[] a = new int[31];
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = fill;
                }
                var attrs = new PlayerAttributes();
                attrs.FromArray(a);
                attrs.WeakFootRating = 1 + ((k + clubId) % 5);
                p.Attributes = attrs;

                players[k] = p;
            }
            return new Squad(clubId, players);
        }

        private static ISquadProvider Provider(params Squad[] squads)
        {
            var p = new DictionarySquadProvider();
            foreach (Squad s in squads)
            {
                p.Add(s);
            }
            return p;
        }

        /// <summary>Boots + configures + ticks a distinct-squad match and captures a durable save
        /// (header, payload) — the setup shared by the distinct-squad fail-loud tests.</summary>
        private static (SnapshotHeader, SnapshotPayload) SaveConfiguredMatch()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            a.ConfigureSquads(DistinctSquad(1), DistinctSquad(2));
            for (int i = 0; i < 30; i++) a.RunTick();
            return (a.TestOnly_CaptureDurableHeader(), a.TestOnly_CaptureDurablePayload());
        }

        // ── ISquadProvider test doubles ───────────────────────────────────────────

        private sealed class DictionarySquadProvider : ISquadProvider
        {
            private readonly Dictionary<int, Squad> _byClubId = new Dictionary<int, Squad>();
            public void Add(Squad s) => _byClubId[s.ClubId] = s;
            public Squad ResolveByClubId(int clubId) =>
                _byClubId.TryGetValue(clubId, out Squad s) ? s : null;
        }

        private sealed class EmptySquadProvider : ISquadProvider
        {
            public Squad ResolveByClubId(int clubId) => null;
        }

        private sealed class WrongClubIdSquadProvider : ISquadProvider
        {
            private readonly Squad _always;
            public WrongClubIdSquadProvider(Squad always) => _always = always;
            public Squad ResolveByClubId(int clubId) => _always;   // ignores the requested ClubId
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-20 | —      | Initial Phase 1 snapshot-deserialize acceptance tests — G3     |
// |         |            |        | round-trip determinism (neutral kickoff / mid-match tactics /  |
// |         |            |        | booking-cursor KD-8 regression) + KD-1 version-gate &          |
// |         |            |        | trailing-byte fail-loud + KD-3 distinct-squad refusal.         |
// | 1.1     | 2026-07-20 | —      | Phase 2 (#27 T3 / KD-3): the distinct-squad-refusal test is    |
// |         |            |        | replaced with the re-projection suite — G3 round-trip for a    |
// |         |            |        | distinct (varied-attribute) squad, a mid-match substitution,   |
// |         |            |        | a post-restore substitution (bench-attr fidelity), and a       |
// |         |            |        | post-restore keeper-for-keeper substitution (bench-GK flag),   |
// |         |            |        | plus fail-loud on no provider / unknown ClubId / mismatched    |
// |         |            |        | roster. AssertRoundTripDeterministic gains an ISquadProvider + |
// |         |            |        | mid-run + after-save hooks; DistinctSquad / SquadWithBench-    |
// |         |            |        | Goalkeeper + provider test doubles added.                      |
#endregion
