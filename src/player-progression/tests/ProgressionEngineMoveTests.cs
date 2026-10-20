// File:     src/player-progression/tests/ProgressionEngineMoveTests.cs
// Created:  2026-10-20
// Modified: 2026-10-20 (AR round 3 M9 - v1.0)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 FR-PG-011 / FR-PG-022 / §3.4 (as amended by ERR-031-005 and
//           ERR-028-027); Transfers #31 §3.4 (the roster port's re-home); Code Standards #20
// Purpose:  Locks ProgressionEngine.MovePlayer / CanMovePlayer directly (#31 T2's stable-id re-home). Until now
//           nothing exercised them below the season-save integration suite, so deleting MovePlayer's
//           BeginMutation() left every progression test green: a staged SeasonBoundaryPlan prepared BEFORE a
//           transfer could then be committed AFTER it and silently resurrect the pre-transfer rosters.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class ProgressionEngineMoveTests
    {
        private const uint Day = 400;

        // Club ids deliberately differ from array indices (the SeasonBoundaryTests lesson).
        private const int ClubA = 3;      // 5 players: 75..79
        private const int ClubB = 7;      // 5 players: 175..179
        private const int ClubOne = 11;   // 1 player:  275
        private const int ClubFull = 15;  // CLUB_SQUAD_SIZE players: 375..399

        private const int Mover = 76;     // ClubA's defender; his lifecycle is made distinctive below
        private const int MoverGrowthCursor = 5;

        // ── (a) a move stales a staged boundary plan ─────────────────────────────────

        [Test]
        public void Move_AfterAPlanWasPrepared_MakesTheCommitRefuse_AndTheStoreKeepsTheMove()
        {
            ProgressionEngine store = Store(flagMover: false, flagBRetiree: true);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(1UL, 1, Day);

            store.MovePlayer(Mover, ClubA, ClubB);
            byte[] moved = store.Snapshot();

            Assert.Throws<ArgumentException>(() => store.CommitSeasonBoundary(plan),
                "MovePlayer is a store mutation: it must invalidate a plan prepared before it (the P-M1 generation)");
            Assert.That(store.Snapshot(), Is.EqualTo(moved), "a refused commit writes nothing");
            Assert.That(IdsOf(store, ClubB), Has.Member(Mover), "the transfer survives the refused commit");
        }

        // ── (b) what a move does, and what it must not touch ─────────────────────────

        [Test]
        public void Move_CarriesTheRecordAndLifecycleUnchanged_KeepsBothClubsAscending_AndLeavesTheCursor()
        {
            ProgressionEngine store = Store(flagMover: false, flagBRetiree: false);
            int cursor = store.NextPlayerId;
            byte[] before = Fingerprint(store, ClubA, Mover);
            Assert.That(FindLifecycle(store, ClubA, Mover).GrowthCursor, Is.EqualTo((long)MoverGrowthCursor),
                "the fixture made his lifecycle distinctive, so an identity check below is not vacuous");

            store.MovePlayer(Mover, ClubA, ClubB);

            Assert.That(Fingerprint(store, ClubB, Mover), Is.EqualTo(before),
                "FR-PG-011: the record and lifecycle travel byte-identically under the unchanged id");
            Assert.That(IdsOf(store, ClubA), Is.EqualTo(new[] { 75, 77, 78, 79 }), "the source is compacted");
            Assert.That(IdsOf(store, ClubB), Is.EqualTo(new[] { 76, 175, 176, 177, 178, 179 }),
                "the destination is re-sorted ascending - the mover sorts first, not appended last");
            Assert.That(store.NextPlayerId, Is.EqualTo(cursor), "FR-PG-011: a transfer allocates nothing");
            Assert.That(store.TryFindPlayer(Mover, out _, out int owner), Is.True);
            Assert.That(owner, Is.EqualTo(ClubB));
        }

        [Test]
        public void Move_LeavesEveryOtherClubAndPlayerUntouched()
        {
            ProgressionEngine store = Store(flagMover: false, flagBRetiree: false);
            byte[] otherBefore = Fingerprint(store, ClubB, 177);
            byte[] fullBefore = Fingerprint(store, ClubFull, 380);

            store.MovePlayer(Mover, ClubA, ClubB);

            Assert.That(Fingerprint(store, ClubB, 177), Is.EqualTo(otherBefore));
            Assert.That(Fingerprint(store, ClubFull, 380), Is.EqualTo(fullBefore));
            Assert.That(IdsOf(store, ClubOne), Is.EqualTo(new[] { 275 }));
        }

        // ── (c) the refusals: CanMovePlayer false <=> MovePlayer throws, with no mutation ──

        [Test]
        public void CanMovePlayer_IsTrue_ForTheFixturesGoodMove()
        {
            Assert.That(Store(false, false).CanMovePlayer(Mover, ClubA, ClubB), Is.True);
        }

        [TestCase(Mover, ClubA, ClubA, TestName = "SameClub")]
        [TestCase(Mover, ClubA, 99, TestName = "UnknownDestinationClub")]
        [TestCase(Mover, 99, ClubB, TestName = "UnknownSourceClub")]
        [TestCase(177, ClubA, ClubB, TestName = "PlayerOnAnotherClub")]
        [TestCase(9999, ClubA, ClubB, TestName = "PlayerOnNoClub")]
        [TestCase(275, ClubOne, ClubB, TestName = "SourceOfSizeOne_WouldEmptyTheClub")]
        [TestCase(Mover, ClubA, ClubFull, TestName = "DestinationAtClubSquadSize")]
        public void ARefusedMove_IsFalseFromCanMovePlayer_ThrowsFromMovePlayer_AndMutatesNothing(
            int playerId, int fromClubId, int toClubId)
        {
            ProgressionEngine store = Store(flagMover: false, flagBRetiree: true);
            // A plan prepared first proves a refused move does not even bump the mutation generation.
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(1UL, 1, Day);
            byte[] before = store.Snapshot();

            Assert.That(store.CanMovePlayer(playerId, fromClubId, toClubId), Is.False);
            Assert.Throws<InvalidOperationException>(() => store.MovePlayer(playerId, fromClubId, toClubId));

            Assert.That(store.Snapshot(), Is.EqualTo(before), "a refused move writes nothing");
            Assert.DoesNotThrow(() => store.CommitSeasonBoundary(plan),
                "a refused move is not a mutation: the plan prepared before it is still installable");
        }

        // ── (d) a re-homed retiree is retired and replaced at the club carrying him ──

        [Test]
        public void ARetirementFlaggedPlayer_MovedBeforeTheBoundary_IsReplacedAtHisNewClub()
        {
            ProgressionEngine store = Store(flagMover: true, flagBRetiree: false);
            int cursor = store.NextPlayerId;
            PlayerPosition moverPosition = FindRecord(store, ClubA, Mover).Position;
            store.MovePlayer(Mover, ClubA, ClubB);

            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(0xB0DA2028UL, 2, Day);

            Assert.That(plan.RetiredPlayerIds, Is.EqualTo(new[] { Mover }), "he is retired under his new club");
            Assert.That(plan.RegenClubIds, Is.EqualTo(new[] { ClubB }), "ClubA, which no longer carries him, gets no regen");
            Assert.That(plan.RegenPlayerIds, Is.EqualTo(new[] { cursor }));

            store.CommitSeasonBoundary(plan);

            Squad a = store.SquadFor(ClubA);
            Squad b = store.SquadFor(ClubB);
            Assert.That(a.Count, Is.EqualTo(4), "ClubA is not topped back up to five");
            Assert.That(b.Count, Is.EqualTo(6), "ClubB: five of its own, plus the mover's replacement");
            Assert.That(IndexOf(b, Mover), Is.LessThan(0), "the retiree is gone");
            int r = IndexOf(b, cursor);
            Assert.That(r, Is.GreaterThanOrEqualTo(0), "the regen joined club 7");
            Assert.That(b.GetPlayer(r).Position, Is.EqualTo(moverPosition),
                "ERR-028-024: the regen takes the retiree's position, wherever he now plays");
        }

        // ── fixtures ─────────────────────────────────────────────────────────────────

        // ClubA: 75..79 (Goalkeeper, Defender, Midfielder, Forward, Midfielder) - Mover = 76 is the Defender.
        // ClubB: 175..179. ClubOne: 275. ClubFull: 375..399.
        private static ProgressionEngine Store(bool flagMover, bool flagBRetiree)
        {
            ProgressionEngine seeded = ProgressionEngine.SeedFrom(
                new[] { Club(ClubA, 5), Club(ClubB, 5), Club(ClubOne, 1), Club(ClubFull, PlayerDatabaseConstants.CLUB_SQUAD_SIZE) },
                Day);
            ClubCareerStates[] blocks = seeded.ToBlocks();

            int mover = Array.FindIndex(blocks[0].Records, r => r.PlayerId == Mover);
            blocks[0].Lifecycles[mover].GrowthCursor = MoverGrowthCursor;
            blocks[0].Lifecycles[mover].PotentialAbility = PlayerProgressionConstants.PA_MIN + 7;
            if (flagMover)
            {
                blocks[0].Lifecycles[mover].RetirementFlag = true;
                blocks[0].Lifecycles[mover].RetirementDay = blocks[0].Lifecycles[mover].LastAdvancedWorldDay;
            }

            if (flagBRetiree)
            {
                int b = Array.FindIndex(blocks[1].Records, r => r.PlayerId == 178);
                blocks[1].Lifecycles[b].RetirementFlag = true;
                blocks[1].Lifecycles[b].RetirementDay = blocks[1].Lifecycles[b].LastAdvancedWorldDay;
            }

            return ProgressionEngine.FromBlocks(blocks, seeded.NextPlayerId);
        }

        private static Squad Club(int clubId, int count)
        {
            PlayerPosition[] cycle =
            {
                PlayerPosition.Goalkeeper, PlayerPosition.Defender, PlayerPosition.Midfielder,
                PlayerPosition.Forward, PlayerPosition.Midfielder
            };
            var players = new PlayerRecord[count];
            for (int i = 0; i < count; i++)
            {
                players[i] = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + i);
                players[i].Position = cycle[i % cycle.Length];
            }

            return new Squad(clubId, players);
        }

        private static ClubCareerStates BlockOf(ProgressionEngine store, int clubId)
        {
            foreach (ClubCareerStates block in store.ToBlocks())
            {
                if (block.ClubId == clubId)
                {
                    return block;
                }
            }

            throw new InvalidOperationException("fixture club " + clubId + " is not carried");
        }

        private static int[] IdsOf(ProgressionEngine store, int clubId)
        {
            PlayerRecord[] records = BlockOf(store, clubId).Records;
            var ids = new int[records.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = records[i].PlayerId;
            }

            return ids;
        }

        private static PlayerRecord FindRecord(ProgressionEngine store, int clubId, int playerId)
        {
            ClubCareerStates block = BlockOf(store, clubId);
            return block.Records[Array.FindIndex(block.Records, r => r.PlayerId == playerId)];
        }

        private static PlayerLifecycle FindLifecycle(ProgressionEngine store, int clubId, int playerId)
        {
            ClubCareerStates block = BlockOf(store, clubId);
            return block.Lifecycles[Array.FindIndex(block.Records, r => r.PlayerId == playerId)];
        }

        // The save codec's encoding of one player's record + lifecycle: a field-complete fingerprint that does
        // not depend on which club holds him or where in the club he sits.
        private static byte[] Fingerprint(ProgressionEngine store, int clubId, int playerId)
        {
            PlayerRecord record = FindRecord(store, clubId, playerId);
            PlayerLifecycle life = FindLifecycle(store, clubId, playerId);
            return ProgressionSaveCodec.Encode(
                new[] { new ClubCareerStates(0, new[] { record }, new[] { life }) }, playerId + 1);
        }

        private static int IndexOf(Squad squad, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-20 | —      | AR round 3 M9 (ERR-028-027): the first direct locks on ProgressionEngine.MovePlayer / CanMovePlayer. (a) a move stales a plan prepared before it (deleting BeginMutation() in MovePlayer fails it); (b) record + lifecycle byte-identical after the move, destination ascending (the mover sorts first), source compacted, NextPlayerId unchanged, other clubs untouched; (c) every CanMovePlayer false case (same club, unknown source/destination, player not on the source, source of size 1, destination at CLUB_SQUAD_SIZE) throws InvalidOperationException with the snapshot and the mutation generation untouched; (d) a retirement-flagged player moved club 3 -> 7 is retired and replaced at club 7 in his position, club 3 gets no regen. |
#endregion
