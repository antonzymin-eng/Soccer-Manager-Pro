// File:     src/player-progression/tests/ProgressionEngineTests.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1 / §3.4 / §3.5 / §5, KD-4 / KD-7,
//           FR-PG-011/013/014/019/021/022/023; ERR-029-006 (the batch entry point);
//           ERR-030-027 (the twice-per-fixture-day call); Code Standards #20
// Purpose:  T-PG-DET-001/002, T-PG-RET-001, T-PG-SAVE-001, plus the locks this landing needs that #28
//           §5 does not list: per-day idempotency, the batch's key-agreement refusals, and the KD-4
//           projection — the one that fails if roster authority is moved back off #28's block.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class ProgressionEngineTests
    {
        // Large enough that BirthWorldDay stays non-negative for every age used here.
        private const uint BaseDay = 100000;

        // ── The daily step (§3.1 / FR-PG-021) ─────────────────────────────────────────

        [Test]
        public void AdvanceDay_TwiceOnTheSameDay_IsANoOp()
        {
            // ERR-030-027: #30 runs a fixture day's slots TWICE — pre-round and from the advance loop —
            // and RunCareerDaySteps documents that each subsystem's own cursor is what makes the second
            // call a no-op. Without the cursor #28's growth would accrue twice on every fixture day: a
            // silent rate error, not a crash. This is the lock on that.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);
            int afterFirst = AttributeSum(engine);
            long cursorAfterFirst = CursorOf(engine);

            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);

            Assert.AreEqual(cursorAfterFirst, CursorOf(engine),
                "a second AdvanceDay for the SAME world day must accrue nothing (the ERR-030-027 " +
                "pre-round re-run is a cursor no-op).");
            Assert.AreEqual(afterFirst, AttributeSum(engine));
        }

        [Test]
        public void AdvanceDay_FirstCall_AdvancesExactlyOneDay()
        {
            // The first call on a never-advanced store has no prior cursor to measure a gap from, so it
            // advances exactly one day and ANCHORS the cursor. That is the right production semantic —
            // #30 calls this every world day — but it is an assumption the gap tests below depend on,
            // so it is locked here rather than left implicit.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            // Stays inside the Growth band (age is DERIVED from the world day, FR-PG-005, so a day far
            // enough ahead would put these players in Decline and flip the sign — which is correct
            // behaviour, and exactly why the band is held fixed here so the COUNT is what is asserted).
            engine.AdvanceDay(BaseDay + 300, TrainingInputBatch.Neutral);

            Assert.AreEqual(SquadSize, CursorOf(engine),
                "one day of accrual per player, however far ahead the first call's world day is — the " +
                "store cannot know which day the career began accruing on.");
        }

        [Test]
        public void AdvanceDay_AcrossAGap_MatchesDayByDay()
        {
            // #28 §5.2 T-PG-DET-002. Age is gap-independent because it is derived, but the growth cursor
            // is an ACCUMULATOR — a single call that advanced it once for a 400-day gap would lose 399
            // days. AdvanceDay replays the intervening days, which is what makes the spec's claim true.
            ProgressionEngine gapped = SeedOneClub(ageAtBase: 18);
            ProgressionEngine daily = SeedOneClub(ageAtBase: 18);

            // Anchor both on the same day first: the gap contract is about a jump made by a RUNNING
            // career, not about the first-ever call (locked directly above).
            gapped.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);
            daily.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);

            gapped.AdvanceDay(BaseDay + 400, TrainingInputBatch.Neutral);
            for (uint d = BaseDay + 1; d <= BaseDay + 400; d++)
            {
                daily.AdvanceDay(d, TrainingInputBatch.Neutral);
            }

            Assert.AreEqual(AttributeSum(daily), AttributeSum(gapped),
                "one long-gap advance must land on the same attributes as the day-by-day advance.");
            Assert.AreEqual(CursorOf(daily), CursorOf(gapped),
                "…and on the same growth cursor — the half a single-accrual step would lose.");
        }

        [Test]
        public void AdvanceDay_AGrowthBandYear_SpendsExactlyOnePoint()
        {
            // The KD-8 literal §4.3 identity, through the batch entry point rather than the raw
            // per-player projection — so this fails if slot 1 is wired to something that does not step.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);
            int before = AttributeSum(engine);

            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);          // anchor
            engine.AdvanceDay(BaseDay + 365, TrainingInputBatch.Neutral);    // a full year on

            Assert.AreEqual(before + SquadSize, AttributeSum(engine),
                "exactly one attribute point per player per year in the Growth band (KD-8).");
        }

        [Test]
        public void AdvanceDay_AtRetirementAge_FlagsButDoesNotRemove()
        {
            // T-PG-RET-001 (FR-PG-013/014): flagged, deterministic, no draw — and still carried, because
            // roster removal is the season boundary's and that is deliberately not in this landing.
            ProgressionEngine engine = SeedOneClub(
                ageAtBase: PlayerProgressionConstants.RETIREMENT_AGE);

            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);

            LifecycleViewModel view = engine.LifecycleView(ClubId, FirstPlayerId);
            Assert.IsTrue(view.RetirementFlag, "a player at RETIREMENT_AGE is flagged on the world tick.");
            Assert.AreEqual(BaseDay, view.RetirementDay);
            Assert.AreEqual(SquadSize, engine.SquadFor(ClubId).Count,
                "flagging must not remove him — he stays selectable until the season boundary (FR-PG-014).");
        }

        [Test]
        public void AdvanceDay_BelowRetirementAge_DoesNotFlag()
        {
            ProgressionEngine engine = SeedOneClub(
                ageAtBase: PlayerProgressionConstants.RETIREMENT_AGE - 1);

            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);

            Assert.IsFalse(engine.LifecycleView(ClubId, FirstPlayerId).RetirementFlag,
                "the retirement test is hard AT RETIREMENT_AGE, not below it.");
        }

        // ── The KD-4 projection — the authority lock ──────────────────────────────────

        [Test]
        public void SquadFor_ReflectsBankedGrowth_NotTheSeededRoster()
        {
            // THE load-bearing lock of this landing. Before #28 T2a the only provider was the bootstrap,
            // whose squads never change; if roster authority is moved back off #28's block — or if
            // SquadFor is re-pointed at the seed — every consumer silently reads day-0 attributes again
            // and this test is what goes red.
            Squad[] seed = OneClubSquad(ageAtBase: 18);
            int seededSum = SumAttributes(seed[0]);

            ProgressionEngine engine = ProgressionEngine.SeedFrom(seed, BaseDay);
            engine.AdvanceDay(BaseDay, TrainingInputBatch.Neutral);          // anchor
            engine.AdvanceDay(BaseDay + 365, TrainingInputBatch.Neutral);    // a full year on

            Squad projected = engine.SquadFor(ClubId);
            Assert.AreEqual(seededSum + SquadSize, SumAttributes(projected),
                "the projected squad must carry the growth the store banked, not the seeded values.");
            Assert.AreEqual(seededSum, SumAttributes(seed[0]),
                "…and the caller's original bootstrap squad must be untouched (Squad is immutable; the " +
                "store copied it in).");
        }

        [Test]
        public void SquadFor_UnknownClub_ReturnsNull()
        {
            // The ISquadProvider contract ProgressionSquads forwards: a miss is null, not a throw, so
            // the caller decides whether it is fatal.
            Assert.IsNull(SeedOneClub(ageAtBase: 18).SquadFor(ClubId + 99));
        }

        // ── The batch's key agreement (FR-PG-021) ─────────────────────────────────────

        [Test]
        public void AdvanceDay_BatchMissingAClub_IsRefused()
        {
            // A partial batch is refused rather than having its gaps filled with Neutral: a dropped club
            // would otherwise be indistinguishable from one that genuinely trained neutrally.
            ProgressionEngine engine = SeedTwoClubs();

            var partial = new TrainingInputBatch(new[] { NeutralInputsFor(engine, ClubId) });

            Assert.Throws<ArgumentException>(
                () => engine.AdvanceDay(BaseDay, partial),
                "a bound batch must cover every carried club.");
        }

        [Test]
        public void AdvanceDay_BatchWithAWrongPlayerId_IsRefused()
        {
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);
            ClubTrainingInputs good = NeutralInputsFor(engine, ClubId);

            var ids = (int[])good.PlayerIds.Clone();
            ids[0] = ids[0] + 1000;   // a player this career does not carry
            var wrong = new TrainingInputBatch(
                new[] { new ClubTrainingInputs(ClubId, ids, good.Inputs) });

            Assert.Throws<ArgumentException>(
                () => engine.AdvanceDay(BaseDay, wrong),
                "the batch must be keyed to the players being advanced — otherwise growth is " +
                "attributed to the wrong player, silently.");
        }

        [Test]
        public void AdvanceDay_BatchWithAWrongPlayerCount_IsRefused()
        {
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);
            ClubTrainingInputs good = NeutralInputsFor(engine, ClubId);

            var shortIds = new int[good.PlayerIds.Length - 1];
            Array.Copy(good.PlayerIds, shortIds, shortIds.Length);
            var shortInputs = new TrainingInput[shortIds.Length];
            var wrong = new TrainingInputBatch(
                new[] { new ClubTrainingInputs(ClubId, shortIds, shortInputs) });

            Assert.Throws<ArgumentException>(() => engine.AdvanceDay(BaseDay, wrong));
        }

        [Test]
        public void AdvanceDay_NeutralBatch_IsAcceptedAndIsTheIdentity()
        {
            // FR-PG-009: Neutral must be byte-identical to no training input. Both routes are exercised
            // here — the explicit Neutral batch and a fully-supplied batch of Neutral inputs.
            ProgressionEngine viaNeutral = SeedOneClub(ageAtBase: 18);
            ProgressionEngine viaFullBatch = SeedOneClub(ageAtBase: 18);

            viaNeutral.AdvanceDay(BaseDay + 364, TrainingInputBatch.Neutral);

            var full = new TrainingInputBatch(new[] { NeutralInputsFor(viaFullBatch, ClubId) });
            viaFullBatch.AdvanceDay(BaseDay + 364, full);

            Assert.AreEqual(AttributeSum(viaNeutral), AttributeSum(viaFullBatch),
                "a fully-supplied Neutral batch is the identity contribution (FR-PG-009).");
            Assert.AreEqual(CursorOf(viaNeutral), CursorOf(viaFullBatch));
        }

        // ── Persistence (§3.5 / T-PG-DET-001 / T-PG-SAVE-001) ─────────────────────────

        [Test]
        public void SaveRestore_ContinuesIdentically()
        {
            // T-PG-DET-001, the keystone: a save on any day restores to the identical continuation.
            ProgressionEngine uninterrupted = SeedOneClub(ageAtBase: 18);
            ProgressionEngine saved = SeedOneClub(ageAtBase: 18);

            for (uint d = BaseDay; d <= BaseDay + 500; d++)
            {
                uninterrupted.AdvanceDay(d, TrainingInputBatch.Neutral);
            }

            // Advance partway, round-trip through the codec, then finish.
            for (uint d = BaseDay; d <= BaseDay + 200; d++)
            {
                saved.AdvanceDay(d, TrainingInputBatch.Neutral);
            }
            ProgressionEngine restored = ProgressionEngine.Restore(saved.Snapshot());
            for (uint d = BaseDay + 201; d <= BaseDay + 500; d++)
            {
                restored.AdvanceDay(d, TrainingInputBatch.Neutral);
            }

            Assert.AreEqual(AttributeSum(uninterrupted), AttributeSum(restored),
                "a save mid-year must restore to the identical continuation (FR-PG-006).");
            Assert.AreEqual(CursorOf(uninterrupted), CursorOf(restored),
                "…including the cursor, which is what a lost LastAdvancedWorldDay would corrupt.");
        }

        [Test]
        public void SaveRestore_RoundTripsEveryField()
        {
            // T-PG-SAVE-001, field-identical — including NextPlayerId, which nothing else observes yet
            // and which a regen would silently reuse if it were dropped (FR-PG-011).
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);
            engine.AdvanceDay(BaseDay + 400, TrainingInputBatch.Neutral);

            ProgressionEngine restored = ProgressionEngine.Restore(engine.Snapshot());

            Assert.AreEqual(engine.NextPlayerId, restored.NextPlayerId);
            Assert.AreEqual(engine.ClubCount, restored.ClubCount);

            LifecycleViewModel before = engine.LifecycleView(ClubId, FirstPlayerId);
            LifecycleViewModel after = restored.LifecycleView(ClubId, FirstPlayerId);
            Assert.AreEqual(before.Age, after.Age);
            Assert.AreEqual(before.CurrentAbility, after.CurrentAbility);
            Assert.AreEqual(before.PotentialAbility, after.PotentialAbility);
            Assert.AreEqual(before.RetirementFlag, after.RetirementFlag);
            Assert.AreEqual(before.RetirementDay, after.RetirementDay);

            Assert.AreEqual(
                SumAttributes(engine.SquadFor(ClubId)), SumAttributes(restored.SquadFor(ClubId)),
                "the evolving attributes are the point of the block (KD-4) — they must survive it.");
        }

        [Test]
        public void Snapshot_IsCanonical_RegardlessOfSeedOrder()
        {
            // Order is not state: two stores holding equal state must produce identical bytes, so a save
            // file cannot depend on the order the bootstrap happened to hand clubs over in.
            Squad[] ascending = TwoClubSquads();
            var descending = new[] { ascending[1], ascending[0] };

            byte[] a = ProgressionEngine.SeedFrom(ascending, BaseDay).Snapshot();
            byte[] b = ProgressionEngine.SeedFrom(descending, BaseDay).Snapshot();

            CollectionAssert.AreEqual(a, b,
                "Encode canonicalizes to ascending club and player id (FR-PG-019).");
        }

        // ── Global id uniqueness (ERR-041-019 / ERR-027-004) ──────────────────────────

        [Test]
        public void SeedFrom_ACrossClubDuplicatePlayerId_IsRefused()
        {
            // #27 KD-3 promises club-scoped uniqueness; a career keyed (ClubId, PlayerId) — and #41's
            // club-less injury draw key — need it GLOBAL. #28 is the second id allocator to arrive, so
            // it enforces the precondition rather than inheriting an accident of today's formula.
            var clubA = new Squad(ClubId, new[] { Player(FirstPlayerId, age: 20) });
            var clubB = new Squad(ClubId + 1, new[] { Player(FirstPlayerId, age: 20) });

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.SeedFrom(new[] { clubA, clubB }, BaseDay),
                "two clubs sharing a player id would share career state silently.");
        }

        // ── Empty store ───────────────────────────────────────────────────────────────

        [Test]
        public void Empty_RoundTripsAsAZeroClubBlock()
        {
            // "No careers tracked" is a well-formed empty block, not a missing one — the argument that
            // makes the sub-blob mandatory in the frame.
            ProgressionEngine restored = ProgressionEngine.Restore(ProgressionEngine.Empty.Snapshot());
            Assert.AreEqual(0, restored.ClubCount);
        }

        // ── Fixtures ──────────────────────────────────────────────────────────────────

        private const int ClubId = 3;
        private const int SquadSize = 4;
        private const int FirstPlayerId = ClubId * 25;

        private static ProgressionEngine SeedOneClub(int ageAtBase) =>
            ProgressionEngine.SeedFrom(OneClubSquad(ageAtBase), BaseDay);

        private static ProgressionEngine SeedTwoClubs() =>
            ProgressionEngine.SeedFrom(TwoClubSquads(), BaseDay);

        private static Squad[] OneClubSquad(int ageAtBase)
        {
            var players = new PlayerRecord[SquadSize];
            for (int i = 0; i < SquadSize; i++)
            {
                players[i] = Player(FirstPlayerId + i, ageAtBase);
            }
            return new[] { new Squad(ClubId, players) };
        }

        private static Squad[] TwoClubSquads()
        {
            Squad first = OneClubSquad(ageAtBase: 18)[0];

            var players = new PlayerRecord[SquadSize];
            for (int i = 0; i < SquadSize; i++)
            {
                players[i] = Player((ClubId + 1) * 25 + i, age: 22);
            }
            return new[] { first, new Squad(ClubId + 1, players) };
        }

        private static PlayerRecord Player(int playerId, int age)
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(playerId);
            rec.Age = age;
            return rec;
        }

        private static ClubTrainingInputs NeutralInputsFor(ProgressionEngine engine, int clubId)
        {
            Squad squad = engine.SquadFor(clubId);
            var ids = new int[squad.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = squad.GetPlayer(i).PlayerId;
            }
            return new ClubTrainingInputs(clubId, ids, new TrainingInput[ids.Length]);
        }

        private static int AttributeSum(ProgressionEngine engine) => SumAttributes(engine.SquadFor(ClubId));

        private static int SumAttributes(Squad squad)
        {
            int sum = 0;
            for (int p = 0; p < squad.Count; p++)
            {
                int[] a = squad.GetPlayer(p).Attributes.ToArray();
                for (int i = 0; i < a.Length; i++)
                {
                    sum += a[i];
                }
            }
            return sum;
        }

        // The cursor is not on the observer surface (it is an internal accumulator), so the block is the
        // honest way to read it — and reading it through the codec also proves it is serialized.
        private static long CursorOf(ProgressionEngine engine)
        {
            ClubCareerStates[] blocks = engine.ToBlocks();
            long sum = 0;
            for (int c = 0; c < blocks.Length; c++)
            {
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    sum += blocks[c].Lifecycles[p].GrowthCursor;
                }
            }
            return sum;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1/T2a: the batch entry point, per-day idempotency        |
// |         |            |        | (ERR-030-027), gap-completeness (T-PG-DET-002), retirement     |
// |         |            |        | flagging, save/restore continuation, canonical bytes, global   |
// |         |            |        | id uniqueness, and the KD-4 projection lock.                   |
#endregion
