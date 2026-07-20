// File:     src/match-engine/tests/MatchEngineSnapshotRestoreTests.cs
// Created:  2026-07-20
// Author:   —
// Spec:     Snapshot-deserialize design note (docs/tracking/snapshot-deserialize-design.md) §5 Phase 1
//           (G3 round-trip determinism, KD-1/KD-4/KD-5/KD-8); Match Engine design note §5 Phase G; Code Standards #20
// Purpose:  Phase 1 acceptance tests for the snapshot-deserialize reader — the G3 round-trip determinism
//           contract: save at tick N -> RestoreFromSnapshot -> tick to N+K produces a digest chain
//           byte-identical to an uninterrupted run ticked to N+K (KD-5). Plus the KD-1 version-gate /
//           trailing-byte fail-loud guards and the KD-3 distinct-squad refusal.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase 1 snapshot-deserialize (save/restore) tests for <see cref="MatchEngine"/>. The central
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
        /// ticks it to N, captures a durable (header, payload) save, then:
        ///  (a) continues A for K ticks to build the REFERENCE digest chain (N+1 … N+K), and
        ///  (b) produces a fresh engine C via <see cref="MatchEngine.RestoreFromSnapshot"/> and ticks it K
        ///      times, asserting C's digest at every tick equals A's at the same tick.
        /// C is produced solely by the factory — no separately-ticked engine is needed; A is kept running
        /// only to produce the reference chain to compare against (design note KD-5).
        /// </summary>
        private static void AssertRoundTripDeterministic(Action<MatchEngine> setup, int n, int k)
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            setup?.Invoke(a);

            for (int i = 0; i < n; i++)
            {
                a.RunTick();
            }
            Assert.AreEqual((ulong)n, a.CurrentTick, "Engine A must be at tick N after N ticks.");

            SnapshotHeader header = a.TestOnly_CaptureDurableHeader();
            SnapshotPayload payload = a.TestOnly_CaptureDurablePayload();

            var reference = new List<byte[]>(k);
            for (int i = 0; i < k; i++)
            {
                a.RunTick();
                reference.Add(a.CurrentSnapshotDigest);
            }

            MatchEngine c = MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed);
            Assert.AreEqual((ulong)n, c.CurrentTick,
                "The restored engine's clock must resume at the saved tick N (KD-5).");

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
        public void RestoreFromSnapshot_DistinctSquadRoster_FailsLoud()
        {
            // A ConfigureSquads-booted match carries a non-sentinel roster reference whose per-slot
            // attribute records must be re-projected from the actual Squad (the #27 T3 consumer, Phase 2).
            // Phase 1 refuses rather than silently falling back to CreateDefault() and diverging (KD-3 / R4).
            MatchEngine a = new MatchEngine(MatchSeed);
            a.ConfigureSquads(NeutralSquad(1), NeutralSquad(2));
            for (int i = 0; i < 30; i++) a.RunTick();

            SnapshotHeader header = a.TestOnly_CaptureDurableHeader();
            SnapshotPayload payload = a.TestOnly_CaptureDurablePayload();

            Assert.Throws<NotSupportedException>(
                () => MatchEngine.RestoreFromSnapshot(header, payload, MatchSeed),
                "A distinct-squad snapshot must refuse Phase-1 restore (KD-3 fail-loud) until the T3 " +
                "roster re-projection lands (Phase 2).");
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

        /// <summary>An all-identity, position-coherent squad of the consumed size (KD-L5 layout).</summary>
        private static Squad NeutralSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);
                players[k] = p;
            }
            return new Squad(clubId, players);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-20 | —      | Initial Phase 1 snapshot-deserialize acceptance tests — G3     |
// |         |            |        | round-trip determinism (neutral kickoff / mid-match tactics /  |
// |         |            |        | booking-cursor KD-8 regression) + KD-1 version-gate &          |
// |         |            |        | trailing-byte fail-loud + KD-3 distinct-squad refusal.         |
#endregion
