// File:     src/player-progression/tests/ProgressionEngineTests.cs
// Created:  2026-08-08
// Modified: 2026-08-11 (AR pass 8 — L-3's SeedFrom overflow lock; L-4's DefaultLife() CurrentAbility
//           retune to keep every FromBlocks(..., DefaultLife()) call legal — v1.8)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1 / §3.4 / §3.5 / §5, KD-4 / KD-7,
//           FR-PG-011/013/014/019/021/022/023; ERR-029-006 (the batch entry point);
//           ERR-030-027 (the twice-per-fixture-day call); ERR-028-006 (the signed age anchor);
//           ERR-028-009 (the sentinel is not a legal world day, F8);
//           ERR-028-014 (the never-advanced sentinel retired from the legal store states);
//           Code Standards #20
// Purpose:  T-PG-DET-001/002, T-PG-RET-001, T-PG-SAVE-001, plus the locks this landing needs that #28
//           §5 does not list: per-day idempotency, the batch's key-agreement refusals, the KD-4
//           projection — the one that fails if roster authority is moved back off #28's block — the
//           signed BirthWorldDay anchor (ERR-028-006), and the F8 sentinel guard (ERR-028-009). v1.2
//           adds the mutation-audit locks for seven FromBlocks/ToBlocks guards a mutation sweep proved
//           dead (deleting each left the whole suite green): ascending club/player ids, the id-cursor
//           guard, copy-not-borrow on both FromBlocks and ToBlocks, FromBlocks's own cross-club
//           uniqueness call, and ValidateBatch's positional club-id check. v1.3 (ERR-028-014) rewrites
//           the seed-day/retirement-day/one-below-the-sentinel cases for the anchored cursor and adds
//           the FromBlocks sentinel-refusal lock. v1.4 (AR pass 4) isolates the else-branch `return;`
//           from its `if` condition, and adds duplicate-club-id / null-argument / unbound-element
//           guard locks for SeedFrom, FromBlocks and ValidateBatch that had no isolating test, plus the
//           SeedFrom sentinel guard's narrowness proof.

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
            // The ERR-030-027 production shape: the same day called twice. Kept because it is what
            // #30 actually does on every fixture day — but it is a BOUNDARY case, not the lock on the
            // guard, and saying so is the point (ERR-028-015). The replay loop is empty by
            // construction at `worldDay == cursor`, so this passes with the guard deleted; the old
            // version of this test claimed to lock the guard and did not, and a mutation run proved
            // it by leaving all 469 tests across both suites green. The discriminating case is
            // AdvanceDay_BackwardCall_DoesNotRegressTheCursor, below.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            engine.AdvanceDay(BaseDay + 1, TrainingInputBatch.Neutral);
            int afterFirst = AttributeSum(engine);
            long cursorAfterFirst = CursorOf(engine);
            Assert.AreNotEqual(0L, cursorAfterFirst,
                "precondition: the first call must actually have accrued, or the repeat below is "
                + "comparing two untouched states.");

            engine.AdvanceDay(BaseDay + 1, TrainingInputBatch.Neutral);

            Assert.AreEqual(cursorAfterFirst, CursorOf(engine),
                "a second AdvanceDay for the SAME world day must accrue nothing (the ERR-030-027 " +
                "pre-round re-run is a cursor no-op).");
            Assert.AreEqual(afterFirst, AttributeSum(engine));
        }

        [Test]
        public void AdvanceDay_BackwardCall_DoesNotRegressTheCursor()
        {
            // THE lock on the idempotency guard (ERR-028-015). Repeating a day cannot distinguish the
            // guard from its absence — the replay loop is empty either way. A BACKWARD call can:
            // without the guard, the assignment after the loop rewinds LastAdvancedWorldDay to the
            // earlier day, and the next forward advance replays days already banked, silently doubling
            // that stretch of growth.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            engine.AdvanceDay(BaseDay + 10, TrainingInputBatch.Neutral);
            long cursorAtTen = CursorOf(engine);
            // AR pass 5: +1 band-step per player. SeedLifecycle now credits the seed day's own band
            // step instead of starting GrowthCursor at 0, so the ten REPLAYED days (BaseDay+1..+10)
            // sit on top of one already-banked day from the seed itself — 11, not 10. Arithmetic
            // window shift, not a semantic change.
            Assert.AreEqual(11 * SquadSize, cursorAtTen,
                "precondition: ten lived days banked, so a replay of any of them would be visible.");

            engine.AdvanceDay(BaseDay + 3, TrainingInputBatch.Neutral);   // backward — must do nothing
            Assert.AreEqual(cursorAtTen, CursorOf(engine),
                "a backward call must not accrue.");

            engine.AdvanceDay(BaseDay + 10, TrainingInputBatch.Neutral);  // forward again to the same day
            Assert.AreEqual(cursorAtTen, CursorOf(engine),
                "…and must not have rewound the cursor either: if it had, this advance would replay "
                + "days 4..10 and bank them a second time.");
        }

        [Test]
        public void AdvanceDay_BackwardCall_DoesNotEvaluateRetirement()
        {
            // AR pass 4. Isolates the bare `return;` in AdvancePlayerTo's else-branch from the `if`
            // CONDITION above it, which AdvanceDay_BackwardCall_DoesNotRegressTheCursor already locks.
            // The condition alone stops cursor regression — the assignment sits inside the `if`, so a
            // backward call never reaches it whether or not the else-branch has a `return;`. Deleting
            // ONLY the `return;` (leaving the if/else shell intact) does not regress the cursor, so the
            // sibling lock stays green — but it lets the §3.4 retirement evaluation below the if/else
            // run on a call that advanced nothing. A player not yet flagged whose age already satisfies
            // RETIREMENT_AGE would then be flagged on a BACKWARD call, stamping RetirementDay with a day
            // earlier than his own cursor — nonsense state this test catches.
            var records = new[] { Player(900, age: PlayerProgressionConstants.RETIREMENT_AGE) };
            var lifecycles = new[] { DefaultLife(lastAdvanced: BaseDay) };
            var club = new ClubCareerStates(ClubId, records, lifecycles);
            ProgressionEngine engine = ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 901);

            engine.AdvanceDay(BaseDay - 100, TrainingInputBatch.Neutral);   // backward — advances nothing

            LifecycleViewModel view = engine.LifecycleView(ClubId, 900);
            Assert.IsFalse(view.RetirementFlag,
                "a non-advancing (backward) call must not reach the §3.4 retirement evaluation — only a "
                + "call that actually advances the cursor may flag a player.");
        }

        [Test]
        public void AdvanceDay_FirstCall_ReplaysFromTheSeedDay()
        {
            // INVERTED at ERR-028-014, and the inversion is the fix. This case used to assert that the
            // first call banks exactly ONE day "however far ahead the first call's world day is —
            // the store cannot know which day the career began accruing on." Both halves were wrong:
            // SeedFrom is handed newGameWorldDay, so the store CAN know, and collapsing an arbitrary
            // span into one day's accrual — while every player's DERIVED age jumped by the whole span
            // — was the silent-data defect. The seed day is now the cursor, so there is no first-call
            // special case left: the first advance replays from the seed day like any other gap.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            // Stays inside the Growth band (age is DERIVED from the world day, FR-PG-005, so a day far
            // enough ahead would put these players in Decline and flip the sign — which is correct
            // behaviour, and exactly why the band is held fixed here so the COUNT is what is asserted).
            engine.AdvanceDay(BaseDay + 300, TrainingInputBatch.Neutral);

            // AR pass 5: +1 band-step per player. SeedLifecycle now credits the seed day's own band
            // step (previously 0), so the store carries one already-banked day before the 300 REPLAYED
            // days (BaseDay+1..+300) are added on top — 301, not 300. Arithmetic window shift, not a
            // semantic change.
            Assert.AreEqual(301 * SquadSize, CursorOf(engine),
                "the 300 days between the seed day and the first advance must be REPLAYED, not "
                + "collapsed into one — the seed day is the cursor, so the span is known.");
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

            // The FIRST LIVED day, not the seed day (ERR-028-014). The seed day is the cursor now, so
            // advancing to it is a no-op — and the retirement test lives inside the advance step, so a
            // player generated already at RETIREMENT_AGE is flagged on the first day the world actually
            // lives rather than on the day he was generated. The one-day delay is harmless: the flag
            // is consumed at the season boundary (FR-PG-014), not the day it is set.
            engine.AdvanceDay(BaseDay + 1, TrainingInputBatch.Neutral);

            LifecycleViewModel view = engine.LifecycleView(ClubId, FirstPlayerId);
            Assert.IsTrue(view.RetirementFlag, "a player at RETIREMENT_AGE is flagged on the world tick.");
            Assert.AreEqual(BaseDay + 1, view.RetirementDay);
            Assert.AreEqual(SquadSize, engine.SquadFor(ClubId).Count,
                "flagging must not remove him — he stays selectable until the season boundary (FR-PG-014).");
        }

        [Test]
        public void AdvanceDay_BelowRetirementAge_DoesNotFlag()
        {
            // The FIRST LIVED day (ERR-028-015). Its positive sibling above was bumped to BaseDay + 1
            // when ERR-028-014 made the seed day a no-op; this control case was left behind, and a
            // control that never runs the code is worse than none — deleting the age comparison
            // outright, so that EVERY player retires on every advance, left the whole suite green.
            // Verified by mutation.
            ProgressionEngine engine = SeedOneClub(
                ageAtBase: PlayerProgressionConstants.RETIREMENT_AGE - 1);

            engine.AdvanceDay(BaseDay + 1, TrainingInputBatch.Neutral);

            Assert.IsFalse(engine.LifecycleView(ClubId, FirstPlayerId).RetirementFlag,
                "the retirement test is hard AT RETIREMENT_AGE, not below it.");
        }

        // ── ERR-028-006: the signed age anchor ────────────────────────────────────────

        [Test]
        public void SeedFrom_AtWorldDayZero_ThenAdvanceDay_PreservesEachPlayersBootstrapAge()
        {
            // ERR-028-006: with BirthWorldDay held as uint, SeedLifecycle's anchor
            // (newGameDay − age·DAYS_PER_YEAR) clamped to 0 for every player with a non-zero generated
            // age, because a new world starts on day 0 and that anchor is NEGATIVE for anyone but a
            // newborn. The clamp made the derived age worldDay/365 — the entire league read age 0 after
            // the very first daily step. Varied, non-zero ages at world day 0 are exactly the case a
            // uint anchor could not represent.
            int[] ages = { 17, 22, 28, 34 };
            var players = new PlayerRecord[ages.Length];
            for (int i = 0; i < ages.Length; i++)
            {
                players[i] = Player(FirstPlayerId + i, ages[i]);
            }
            var squad = new Squad(ClubId, players);

            ProgressionEngine engine = ProgressionEngine.SeedFrom(new[] { squad }, newGameWorldDay: 0u);

            // Day 1, not day 0 (ERR-028-015). ERR-028-014 made the seed day the cursor, so
            // `AdvanceDay(0)` became a no-op and this case stopped executing the code it exists to
            // guard: `LifecycleView.Age` reads the raw PlayerRecord.Age field, which is only ever
            // recomputed FROM BirthWorldDay inside the daily step. With the step skipped, the test
            // read back its own input and the ERR-028-006 clamp no longer failed it — verified by
            // mutation. Advancing one real day forces the derivation this test is named for.
            engine.AdvanceDay(1, TrainingInputBatch.Neutral);

            for (int i = 0; i < ages.Length; i++)
            {
                LifecycleViewModel view = engine.LifecycleView(ClubId, FirstPlayerId + i);
                Assert.AreEqual(ages[i], view.Age,
                    $"player {FirstPlayerId + i}'s age must survive the first daily step at world day " +
                    "0 — a clamped uint anchor would read every one of these non-zero ages as 0 here.");
            }
        }

        [Test]
        public void AdvanceDay_AtWorldDayZero_EachAgeBandAccruesItsOwnStepFromTheBootstrapAge()
        {
            // The unit-level twin of the bootstrap-league proof in
            // SeasonLoopProgressionTests.AdvanceDays_DrivesSlot1_AndEachPlayerAccruesHisOwnBandStep —
            // this one is hand-built rather than sourced from LeagueBootstrap, since player-progression
            // cannot reference season-save. Fails exactly as that one would if ERR-028-006 recurred:
            // a clamped anchor puts every age at 0, which is always Growth, so the Stable and Decline
            // assertions below would go red first.
            var players = new[]
            {
                Player(FirstPlayerId + 0, age: 18),   // Growth: < GROWTH_AGE (24)
                Player(FirstPlayerId + 1, age: 27),   // Stable: GROWTH_AGE..DECLINE_AGE (24..30)
                Player(FirstPlayerId + 2, age: 34),   // Decline: > DECLINE_AGE (30)
            };
            var squad = new Squad(ClubId, players);
            ProgressionEngine engine = ProgressionEngine.SeedFrom(new[] { squad }, newGameWorldDay: 0u);

            // Day 1, not day 0 (ERR-028-014): the seed day is now the cursor, so advancing to day 0 is
            // a no-op — the generated state already describes the roster as of day 0. Day 1 is the
            // first day actually lived, and it is one band step, which is what this case is about.
            engine.AdvanceDay(1, TrainingInputBatch.Neutral);

            // AR pass 5: +1 band-step per player, Growth and Decline only. SeedLifecycle now credits
            // the seed day (world day 0) with its own band step, so the Growth and Decline players
            // each carry one already-banked step before the ONE REPLAYED day (day 1) adds a second —
            // ±2, not ±1. The Stable player's seed step is 0 either way (Stable never accrues), so his
            // expectation is unchanged. Arithmetic window shift, not a semantic change.
            ClubCareerStates[] blocks = engine.ToBlocks();
            Assert.AreEqual(+2L, blocks[0].Lifecycles[0].GrowthCursor, "Growth band (age 18): +1/day, plus the seed day's own step.");
            Assert.AreEqual(0L, blocks[0].Lifecycles[1].GrowthCursor, "Stable band (age 27): no change.");
            Assert.AreEqual(-2L, blocks[0].Lifecycles[2].GrowthCursor, "Decline band (age 34): -1/day, plus the seed day's own step.");
        }

        [Test]
        public void FromBlocks_AGrowthCursorBeyondOneWholePoint_IsRefused()
        {
            // AR pass 6 (High), the boundary half. Pass 5 called BirthWorldDay "the ONLY lifecycle field
            // with no range gate" — checkable, and false: GrowthCursor had none either, and it is the one
            // accumulator every attribute change flows through. Out of range it did not corrupt data, it
            // WEDGED the day step (the drain loop had no failure exit), from a save file that round-tripped
            // byte-exact.
            //
            // This lock exists because the mutation run that verified the loop exits ALSO disabled this
            // gate and nothing went red — the gate had no test of its own. The band needs no [GT]
            // judgement: both loops leave |cursor| <= POINT_COST - 1 after any completed step and
            // SeedLifecycle writes ±1, so |cursor| < POINT_COST is exactly the serialized invariant.
            var records = new[] { Player(410, age: 20) };
            var lifecycles = new[] { DefaultLife() };
            lifecycles[0].GrowthCursor = -1000L * PlayerProgressionConstants.POINT_COST;
            var club = new ClubCareerStates(9, records, lifecycles);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 411),
                "a cursor no completed step could have produced must be refused where it enters — "
                + "otherwise it reaches the day step, which is where it stops being a diagnosable error.");
        }

        [Test]
        public void FromBlocks_ABirthWorldDayBelowTheDerivableFloor_IsRefused()
        {
            // AR pass 5 (recorded), fixed. BirthWorldDay was the ONLY lifecycle field with no range
            // gate, and it is the AUTHORITATIVE age anchor — every other age in the model is a derived
            // cache of it. Probe-verified before the fix: this anchor was accepted at every boundary,
            // the daily step narrowed the derived age to int.MinValue, ClassifyAgeBand read that as
            // GROWTH (so the player grows forever and RETIREMENT_AGE can never fire — ERR-028-006's
            // failure mode through a different door), and Snapshot() then refused the negative age —
            // a career that loaded, advanced and projected fine, permanently unsavable.
            var records = new[] { Player(400, age: 20) };
            var lifecycles = new[] { DefaultLife() };
            lifecycles[0].BirthWorldDay = -(long)int.MaxValue * PlayerProgressionConstants.DAYS_PER_YEAR
                                          - PlayerProgressionConstants.DAYS_PER_YEAR;
            var club = new ClubCareerStates(9, records, lifecycles);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 401),
                "an anchor whose derived age cannot be represented must be refused where it enters, "
                + "not discovered at the save that can no longer be written.");
        }

        [Test]
        public void FromBlocks_AnIdCursorAtOrBelowACarriedId_IsRefusedByTheCodecToo()
        {
            // AR pass 5 (recorded), fixed. FR-PG-011 lived ONLY in FromBlocks, so both codec sides
            // admitted a bad cursor — and Restore is Decode + FromBlocks, so Encode could write a blob
            // that loads NEVER. Probe-verified: a club carrying {10, 11} with nextPlayerId 0 encoded
            // cleanly, decoded cleanly, and threw at Restore forever. One owner now; this locks the
            // WRITE side, which is the half that had no enforcement at all.
            ClubCareerStates club = TwoMemberClub(clubId: 9, playerIdA: 10, playerIdB: 11);

            Assert.Throws<ArgumentException>(
                () => ProgressionSaveCodec.Encode(new[] { club }, nextPlayerId: 0),
                "the writer must refuse a cursor that would collide with a live player — otherwise the "
                + "file is written and its own Restore refuses it forever.");
        }

        [Test]
        public void SaveRestore_ANegativeBirthWorldDayBeyondInt32Range_SurvivesTheCodec()
        {
            // The i64 field width is the point, not merely the sign (ERR-028-006). An anchor this far
            // negative does not fit in 32 bits at all, so a codec that still read/wrote 32 bits — the
            // field widened but the wire format left behind — would truncate it silently rather than
            // throwing; the round trip below catches that either way.
            const int ExtremeAge = 10_000_000;   // birthWorldDay = -3,650,000,000 — outside int32's range
            var squad = new Squad(ClubId, new[] { Player(FirstPlayerId, ExtremeAge) });
            ProgressionEngine engine = ProgressionEngine.SeedFrom(new[] { squad }, newGameWorldDay: 0u);

            long birthWorldDay = engine.ToBlocks()[0].Lifecycles[0].BirthWorldDay;
            Assert.Less(birthWorldDay, (long)int.MinValue,
                "precondition: this anchor must not fit in 32 bits, or the test proves nothing about " +
                "the field width the fix is about.");

            ProgressionEngine restored = ProgressionEngine.Restore(engine.Snapshot());

            Assert.AreEqual(birthWorldDay, restored.ToBlocks()[0].Lifecycles[0].BirthWorldDay,
                "a BirthWorldDay outside the int32 range must round-trip exactly — the i64 field width " +
                "is what ERR-028-006 buys over the old uint, which could not represent a pre-epoch " +
                "birth at all.");
        }

        // ── ERR-028-009: the sentinel is not a legal world day (F8) ───────────────────

        [Test]
        public void AdvanceDay_AtTheSentinelWorldDay_IsRefused()
        {
            // F8: storing the sentinel would re-arm the day-0 trap (a player anchored there reads as
            // never-advanced forever) and the gap-replay loop in AdvancePlayerTo would wrap at
            // uint.MaxValue and never terminate. #29's TrainingStep and #41's MedicalStep refuse it for
            // the same reason; #28 adopted their sentinel and must adopt their guard.
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.AdvanceDay(
                    PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL, TrainingInputBatch.Neutral),
                "the never-advanced sentinel is not a legal world day (F8).");
        }

        [Test]
        public void AdvanceDay_OneDayBelowTheSentinel_StillAdvancesNormally()
        {
            // The guard must be narrow: it refuses ONLY the sentinel value, not "large" world days in
            // general — a day one below it is an ordinary (if extreme) world day.
            //
            // The store is seeded TWO days below the sentinel, not at BaseDay (ERR-028-014). Since
            // SeedFrom now anchors the cursor at the seed day, a store seeded at BaseDay and advanced
            // to sentinel-1 is a ~4.29-billion-day gap, and AdvanceDay replays a gap day by day — so
            // the old fixture turned this case from one step into an effectively unbounded loop. It
            // hung the suite. Seeding beside the target keeps the case's actual subject — the F8
            // guard's narrowness — and makes the advance one ordinary day.
            const uint SeedDay = PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL - 2u;
            ProgressionEngine engine = SeedOneClubAt(SeedDay, ageAtBase: 18);

            Assert.DoesNotThrow(
                () => engine.AdvanceDay(SeedDay + 1u, TrainingInputBatch.Neutral),
                "the guard must refuse only the sentinel itself, not merely a large world day.");
            // AR pass 5: +1 band-step per player. SeedLifecycle now credits the seed day's own band
            // step, so the store already carries one banked step before the ONE REPLAYED day
            // (SeedDay+1) adds a second — 2, not 1. Arithmetic window shift, not a semantic change.
            Assert.AreEqual(2 * SquadSize, Math.Abs(CursorOf(engine)),
                "one band-step from the seed day itself plus one band-step for the day advanced.");
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
        public void AdvanceDay_BatchEntryUnbound_IsRefused()
        {
            // AR pass 4 (task 3d). default(ClubTrainingInputs) skips the constructor that rejects null
            // arrays, so PlayerIds/Inputs are both null — ValidateBatch's own bind check must catch it.
            // Club id 0 is deliberate: default(ClubTrainingInputs) carries ClubId 0, and ValidateBatch's
            // positional club-id check (club.ClubId != _clubIds[i]) runs BEFORE the bind check — seeding
            // at any other club id would let that earlier check fire first and never isolate the guard
            // under test.
            var squad = new Squad(0, new[] { Player(1, age: 20) });
            ProgressionEngine engine = ProgressionEngine.SeedFrom(new[] { squad }, BaseDay);
            var batch = new TrainingInputBatch(new ClubTrainingInputs[1]);   // never bound

            Assert.Throws<ArgumentException>(
                () => engine.AdvanceDay(BaseDay, batch),
                "a batch entry that was never bound (default(ClubTrainingInputs)) must be refused.");
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
            // A SHAPE lock, not the FR-PG-009 identity lock (L1). `TrainingInput` is an empty struct
            // today and `DailyPoints` ignores its `training` parameter entirely, so ANY batch content
            // equals Neutral — this cannot enforce FR-PG-009 until #28 T3 gives the type a field. What
            // it does enforce is that the two supply routes are accepted and agree, which is what the
            // slot-1 composition depends on. #28 §5 records that the real identity lock lands with the
            // first TrainingInput field.
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

        [Test]
        public void SeedFrom_ADuplicateClubId_IsRefused()
        {
            // AR pass 4 (task 3a): distinct from the duplicate-PLAYER-id lock above — this is
            // SeedFrom's own duplicate-CLUB-id enforcement (a career carries one roster per club).
            // Proven by mutation against the underlying storage, not just the explicit ContainsKey
            // check: byClub.Add already throws on a duplicate key, so deleting only the explicit
            // check leaves this test green; the discriminating mutation replaces Add with a silent
            // overwrite (byClub[id] = squad), which this test does catch.
            var clubA = new Squad(ClubId, new[] { Player(FirstPlayerId, age: 20) });
            var clubB = new Squad(ClubId, new[] { Player(FirstPlayerId + 1, age: 22) });

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.SeedFrom(new[] { clubA, clubB }, BaseDay),
                "two squads sharing a club id must be refused — a career carries one roster per club.");
        }

        // ── Id-cursor overflow (L-3, AR pass 8) ────────────────────────────────────────

        [Test]
        public void SeedFrom_APlayerAtIntMaxValue_IsRefused()
        {
            // L-3: SeedFrom was the only construction boundary NOT delegating to the shared
            // id-cursor owner — it wrote `maxPlayerId + 1` straight to `_nextPlayerId` with no check
            // at all. At maxPlayerId == int.MaxValue that addition overflows (silently; this project
            // runs unchecked arithmetic by default) to a negative cursor, producing a store that
            // seeds, advances and plays but can NEVER be saved — RequireIdCursorAheadOfCarriedIds
            // refuses the wrapped-negative cursor at Encode, forever. Refused here instead, at the
            // one site that can still recover from it.
            var squad = new Squad(ClubId, new[] { Player(int.MaxValue, age: 20) });

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.SeedFrom(new[] { squad }, BaseDay),
                "a player carrying int.MaxValue overflows the id-cursor computation to a negative " +
                "value on the next allocation — refused here rather than producing an unsaveable store.");
        }

        [Test]
        public void SeedFrom_NullSquadsArray_IsRefused()
        {
            // AR pass 4 (task 3b).
            Assert.Throws<ArgumentNullException>(() => ProgressionEngine.SeedFrom(null, BaseDay));
        }

        [Test]
        public void SeedFrom_ANullSquadElement_IsRefused()
        {
            // AR pass 4 (task 3b).
            var squads = new[] { OneClubSquad(ageAtBase: 18)[0], null };

            Assert.Throws<ArgumentNullException>(() => ProgressionEngine.SeedFrom(squads, BaseDay));
        }

        // ── FromBlocks / ToBlocks structural guards (mutation-audit locks) ───────────
        //
        // A mutation audit proved that deleting each guard below leaves the whole suite green —
        // nothing here previously exercised FromBlocks or ToBlocks directly, only through
        // SeedFrom/Snapshot/Restore round trips that never construct a deliberately malformed block.
        // Each test below drives FromBlocks (or ToBlocks) directly against a hand-built
        // ClubCareerStates so the specific guard is the only thing standing between the input and a
        // successful construction.

        [Test]
        public void FromBlocks_NullClubsArray_IsRefused()
        {
            // AR pass 4 (task 3c).
            Assert.Throws<ArgumentNullException>(
                () => ProgressionEngine.FromBlocks(null, nextPlayerId: 0));
        }

        [Test]
        public void FromBlocks_AnUnboundClubElement_IsRefused()
        {
            // AR pass 4 (task 3c). default(ClubCareerStates) skips the constructor that rejects null
            // arrays, so Records/Lifecycles are both null — the RequireBound-shaped guard FromBlocks
            // must run before anything reads ClubId off the element (an unbound element and a real club
            // id 0 would otherwise both key identically).
            var clubs = new ClubCareerStates[1];   // default(ClubCareerStates) — never bound

            Assert.Throws<ArgumentNullException>(
                () => ProgressionEngine.FromBlocks(clubs, nextPlayerId: 0));
        }

        [Test]
        public void FromBlocks_NonAscendingClubIds_IsRefused()
        {
            ClubCareerStates clubHigh = OneMemberClub(clubId: 5, playerId: 500);
            ClubCareerStates clubLow = OneMemberClub(clubId: 3, playerId: 300);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { clubHigh, clubLow }, nextPlayerId: 501),
                "club ids must strictly ascend — every lookup FromBlocks builds is a binary search " +
                "over that invariant.");
        }

        [Test]
        public void FromBlocks_NonAscendingPlayerIdsWithinAClub_IsRefused()
        {
            ClubCareerStates club = TwoMemberClub(clubId: 9, playerIdA: 50, playerIdB: 40);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 51),
                "player ids within a club must strictly ascend — an unordered block makes a carried " +
                "player un-findable, and the miss reads as 'new'.");
        }

        [Test]
        public void AdvanceDay_AWholeGrowthBandTraversal_GainsExactlyOnePointPerYear_AndLeavesNoResidue()
        {
            // AR pass 5's High, and the assertion this suite did not have. Every existing growth lock
            // measures a hand-placed 365-day window in MID-band; none measures a band TRAVERSAL, so
            // none could see that the accrual window was shifted one day right against a fixed band
            // edge. Measured before the fix: 8 years of Growth gave 7 points and a 364 residue; a
            // 23-year-old with one year left gave ZERO.
            //
            // Two assertions, and the residue one is the load-bearing half: points-gained alone would
            // still pass if a future change re-introduced a residue while rounding the count back up.
            // The residue is what silently eats the first year of the Decline band later.
            foreach (int seedAge in new[] { 16, 20, 23 })
            {
                PlayerRecord rec = Player(1, age: seedAge);
                var squad = new Squad(clubId: 1, new[] { rec });
                ProgressionEngine engine = ProgressionEngine.SeedFrom(new[] { squad }, newGameWorldDay: 0u);

                int yearsInBand = PlayerProgressionConstants.GROWTH_AGE - seedAge;
                int before = AttributeSum(engine, clubIndex: 0, playerIndex: 0);

                engine.AdvanceDay(
                    (uint)(yearsInBand * PlayerProgressionConstants.DAYS_PER_YEAR),
                    TrainingInputBatch.Neutral);

                int after = AttributeSum(engine, clubIndex: 0, playerIndex: 0);
                PlayerLifecycle life = engine.ToBlocks()[0].Lifecycles[0];

                Assert.AreEqual(yearsInBand, after - before,
                    $"a player seeded at {seedAge} spends {yearsInBand} years in the Growth band and "
                    + "Appendix A / KD-8 promise exactly one attribute point per year.");
                Assert.AreEqual(0L, life.GrowthCursor,
                    $"…and he must leave the band with NO residue: seeded at {seedAge}, a leftover "
                    + "cursor survives the Stable band unspendable and then cancels the first year of "
                    + "Decline.");
            }
        }

        private static int AttributeSum(ProgressionEngine engine, int clubIndex, int playerIndex)
        {
            int[] values = engine.ToBlocks()[clubIndex].Records[playerIndex].Attributes.ToArray();
            int sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        [Test]
        public void FromBlocks_OutOfRangeValues_AreRefusedAtConstruction_NotOnlyAtSave()
        {
            // AR pass 5. The PREVIOUS pass gave the four value ranges one owner and wired the codec's
            // Encode and Decode to it — and stopped there, in a commit whose own message said
            // "ProgressionEngine.FromBlocks gates none either". So the breach was caught only at the
            // save: a store built from these blocks advanced, played and projected a squad perfectly,
            // and could never be persisted. Proven before the fix by probe:
            //     FromBlocks(PA = 0) -> accepted; AdvanceDay(6) -> ok; SquadFor -> non-null;
            //     Snapshot() -> "carries potentialAbility 0, outside [4000, 10000] — corrupt save."
            // default(PlayerLifecycle) is the live trigger — PotentialAbility 0 against PA_MIN 4000 —
            // and FromBlocks is public and documented as THE restore path, so a #47 authored-data
            // loader or a tool hits this first.
            var records = new[] { Player(500, age: 20) };
            var lifecycles = new[] { default(PlayerLifecycle) };
            var club = new ClubCareerStates(9, records, lifecycles);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 501),
                "a boundary that can admit a breach must refuse it — catching this only at Save means "
                + "catching it where nothing can be recovered.");
        }

        [Test]
        public void SeedFrom_OutOfRangeValues_AreRefusedAtConstruction_NotOnlyAtSave()
        {
            // The sibling boundary, same rule, same owner. A squad whose record carries an
            // out-of-range weak-foot seeds a store the codec will refuse to write.
            PlayerRecord bad = Player(700, age: 20);
            PlayerAttributes attrs = bad.Attributes;
            attrs.WeakFootRating = 0;                      // WEAK_FOOT_MIN is 1
            bad.Attributes = attrs;

            var squad = new Squad(clubId: 4, new[] { bad });

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.SeedFrom(new[] { squad }, newGameWorldDay: 0u),
                "SeedFrom is the new-game entry point; a career seeded out of range is unsaveable from "
                + "its first day.");
        }

        [Test]
        public void FromBlocks_IdCursorAtOrBelowHighestCarriedPlayerId_IsRefused()
        {
            ClubCareerStates club = OneMemberClub(clubId: 9, playerId: 500);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 500),
                "a cursor at or behind an id the store already carries would let the next allocation " +
                "collide with a live player — exactly what serializing the cursor exists to prevent " +
                "(FR-PG-011).");
        }

        [Test]
        public void SeedFrom_AtTheSentinelWorldDay_IsRefused()
        {
            // ERR-028-015: anchoring the cursor at the seed day made SeedFrom a second way to write
            // the one value FromBlocks refuses. Seeding there produces a store that cannot be saved,
            // restored or advanced — and fails a long way from the call that caused it.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ProgressionEngine.SeedFrom(
                    OneClubSquad(ageAtBase: 18),
                    PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL),
                "the sentinel is not a legal seed day.");
        }

        [Test]
        public void SeedFrom_OneDayBelowTheSentinelWorldDay_Succeeds()
        {
            // AR pass 4 (task 4a): the guard above proves it FIRES on the sentinel; this proves it is
            // NARROW — it refuses only the sentinel itself, not "large" world days in general. Mirrors
            // AdvanceDay_OneDayBelowTheSentinel_StillAdvancesNormally's proof of the sibling guard.
            const uint SeedDay = PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL - 1u;

            Assert.DoesNotThrow(
                () => ProgressionEngine.SeedFrom(OneClubSquad(ageAtBase: 18), SeedDay),
                "the guard must refuse only the sentinel itself, not merely a large world day.");
        }

        [Test]
        public void FromBlocks_ANeverAdvancedSentinelCursor_IsRefused()
        {
            // ERR-028-014: the sentinel is a refused WORLD DAY (F8), never a legal STORE state.
            // SeedFrom anchors the cursor at the seed day, so nothing writes it — and admitting one
            // here would restore the defect through the only entry point that could still carry it: a
            // store whose lived history starts nowhere checkable, which the cursor-vs-clock gate then
            // waves through at any clock while the first advance banks a single day for the whole span.
            var records = new[] { Player(800, age: 20) };
            var lifecycles = new[]
            {
                DefaultLife(PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
            };
            var club = new ClubCareerStates(9, records, lifecycles);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 801),
                "a career's lived history must start somewhere the world clock can be checked against.");
        }

        [Test]
        public void FromBlocks_AnEmptyClub_IsRefused()
        {
            // AR pass 6, M3. PlayerDatabase.Squad's own constructor requires at least 1 player; #28's
            // block IS the roster (KD-4), and grep CLUB_SQUAD_SIZE src/player-progression/ returned
            // NOTHING before this fix — so the store carried clubs the projection could not build.
            // Probe-verified before the fix: a 0-player club built, AdvanceDay'd and round-tripped
            // through Snapshot/Restore cleanly, and SquadFor then threw ArgumentException("Squad must
            // have between 1 and 25 players; got 0") — mid-round, inside
            // ISquadProvider.ResolveByClubId, after earlier fixtures in that round had already been
            // applied to the table.
            var club = new ClubCareerStates(
                9, System.Array.Empty<PlayerRecord>(), System.Array.Empty<PlayerLifecycle>());

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 1),
                "an empty club is state SquadFor cannot project — never let it past the boundary that "
                + "can still refuse it.");
        }

        [Test]
        public void FromBlocks_AClubAboveClubSquadSize_IsRefused()
        {
            // The mirror of the case above: PlayerDatabase.Squad's constructor also refuses ABOVE
            // PlayerDatabaseConstants.CLUB_SQUAD_SIZE (25). Probe-verified before the fix: a 30-player
            // club round-tripped identically and SquadFor threw "got 30" only when something finally
            // asked to PLAY.
            int oversized = PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 5;
            var records = new PlayerRecord[oversized];
            var lifecycles = new PlayerLifecycle[oversized];
            for (int i = 0; i < oversized; i++)
            {
                records[i] = Player(900 + i, age: 20);
                lifecycles[i] = DefaultLife();
            }
            var club = new ClubCareerStates(9, records, lifecycles);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 900 + oversized),
                "a club above CLUB_SQUAD_SIZE is state SquadFor cannot project either.");
        }

        [Test]
        public void FromBlocks_CopiesTheStateArrays_NotBorrowsThem()
        {
            var records = new[] { Player(700, age: 20) };
            var lifecycles = new[] { DefaultLife() };
            var club = new ClubCareerStates(9, records, lifecycles);

            ProgressionEngine engine = ProgressionEngine.FromBlocks(new[] { club }, nextPlayerId: 701);

            // Mutate the CALLER's arrays after FromBlocks has returned.
            records[0].Age = 999;
            lifecycles[0].CurrentAbility = 123456;

            LifecycleViewModel view = engine.LifecycleView(9, 700);
            Assert.AreNotEqual(999, view.Age,
                "FromBlocks must copy the state arrays — mutating the caller's arrays after " +
                "construction must not reach a running career (the #29/#41 AR pass-3 finding, closed " +
                "on the save route and left open on this one).");
            Assert.AreNotEqual(123456, view.CurrentAbility);
        }

        [Test]
        public void FromBlocks_ACrossClubDuplicatePlayerId_IsRefused()
        {
            // SeedFrom's twin of this lock already exists (SeedFrom_ACrossClubDuplicatePlayerId_IsRefused
            // above); this is the OTHER route in — FromBlocks has its own call to the same guard.
            ClubCareerStates clubA = OneMemberClub(clubId: 9, playerId: 500);
            ClubCareerStates clubB = OneMemberClub(clubId: 10, playerId: 500);

            Assert.Throws<ArgumentException>(
                () => ProgressionEngine.FromBlocks(new[] { clubA, clubB }, nextPlayerId: 501),
                "two clubs sharing a player id via the restore route must be refused just as SeedFrom " +
                "refuses it (ERR-041-019 / ERR-027-004).");
        }

        [Test]
        public void ToBlocks_ReturnsCopies_NotTheStoresLiveArrays()
        {
            ProgressionEngine engine = SeedOneClub(ageAtBase: 18);
            ClubCareerStates[] blocks = engine.ToBlocks();

            blocks[0].Records[0].Age = 999;
            blocks[0].Lifecycles[0].CurrentAbility = 123456;

            ClubCareerStates[] blocksAgain = engine.ToBlocks();
            Assert.AreNotEqual(999, blocksAgain[0].Records[0].Age,
                "ToBlocks must hand out copies — mutating the returned arrays must not reach the " +
                "store (FR-PG-022: the store is the single writer, and every other caller must not " +
                "become a second one).");
            Assert.AreNotEqual(123456, blocksAgain[0].Lifecycles[0].CurrentAbility);
        }

        [Test]
        public void AdvanceDay_BatchClubIdMismatchAtAnIndex_IsRefused_EvenWhenPlayerIdsAndLengthsAgree()
        {
            // Isolates ValidateBatch's positional club-id check from the downstream player-id / length
            // checks: the two clubs' batch entries are swapped, but each entry still carries the
            // PLAYER ids and count that genuinely belong at that index — so if only the club-id check
            // is deleted, nothing else in ValidateBatch notices the drift and AdvanceDay silently
            // trains the right players' club under the wrong club's identity.
            ProgressionEngine engine = SeedTwoClubs();
            ClubTrainingInputs clubAInputs = NeutralInputsFor(engine, ClubId);
            ClubTrainingInputs clubBInputs = NeutralInputsFor(engine, ClubId + 1);

            var swapped = new TrainingInputBatch(new[]
            {
                new ClubTrainingInputs(ClubId + 1, clubAInputs.PlayerIds, clubAInputs.Inputs),
                new ClubTrainingInputs(ClubId, clubBInputs.PlayerIds, clubBInputs.Inputs),
            });

            Assert.Throws<ArgumentException>(
                () => engine.AdvanceDay(BaseDay, swapped),
                "the batch's ClubId at each index must match the store's own club there, even when " +
                "the player ids and lengths at that index still agree — both sides order clubs by " +
                "ascending id, so a mismatch means the two roster views have drifted apart.");
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
            SeedOneClubAt(BaseDay, ageAtBase);

        // Seeds on an explicit world day. Since SeedFrom anchors the cursor at the seed day
        // (ERR-028-014), a case that advances to a far-off day must seed NEAR it — AdvanceDay replays
        // a gap day by day, so seeding at BaseDay and advancing near uint.MaxValue is not an extreme
        // input, it is an unbounded loop.
        private static ProgressionEngine SeedOneClubAt(uint seedDay, int ageAtBase) =>
            ProgressionEngine.SeedFrom(OneClubSquad(ageAtBase), seedDay);

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

        // L-4 (AR pass 8): CurrentAbility must equal ComputeCA(attributes) exactly — every fixture in
        // this file builds records via Player(), which never touches attributes beyond Age/Position, so
        // the recomputed value is the same constant for every one of them (uniform default attributes
        // make the position-weighted mean position-INVARIANT: every weight cancels against the same
        // base value). Computed rather than a bare literal so it cannot silently drift from
        // AbilityModel/PlayerDatabaseConstants if either changes.
        private static readonly PlayerAttributes PlayerAttributes_Default = PlayerAttributes.CreateDefault();

        private static readonly int DefaultComputedCurrentAbility =
            AbilityModel.ComputeCA(in PlayerAttributes_Default, PlayerPosition.Midfielder);

        // The cursor defaults to day 0, a legal world day — NOT the never-advanced sentinel, which
        // FromBlocks refuses since ERR-028-014. Cases that want the sentinel must ask for it, which is
        // the point: it is no longer a legal store state, so a fixture must not reach it by default.
        private static PlayerLifecycle DefaultLife(uint lastAdvanced = 0u) =>
            new PlayerLifecycle
            {
                PotentialAbility = 5000,
                // L-4 (AR pass 8): was a bare 3000, mismatched against Player()'s default attributes
                // (which recompute to DefaultComputedCurrentAbility) — DescribeOutOfRangeValues now
                // refuses that mismatch at every boundary (FromBlocks included), so this literal must
                // track the same computed value or every FromBlocks(..., DefaultLife()) call in this
                // file throws.
                CurrentAbility = DefaultComputedCurrentAbility,
                GrowthCursor = 0,
                BirthWorldDay = 0,
                RetirementFlag = false,
                RetirementDay = 0,
                LastAdvancedWorldDay = lastAdvanced
            };

        private static ClubCareerStates OneMemberClub(int clubId, int playerId)
        {
            var records = new[] { Player(playerId, age: 20) };
            var lifecycles = new[] { DefaultLife() };
            return new ClubCareerStates(clubId, records, lifecycles);
        }

        private static ClubCareerStates TwoMemberClub(int clubId, int playerIdA, int playerIdB)
        {
            var records = new[] { Player(playerIdA, age: 20), Player(playerIdB, age: 22) };
            var lifecycles = new[] { DefaultLife(), DefaultLife() };
            return new ClubCareerStates(clubId, records, lifecycles);
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
// | 1.1     | 2026-08-08 | —      | Locks for five fixes just applied. ERR-028-006 (the signed age |
// |         |            |        | anchor): the day-0 bootstrap-age regression lock, the per-band |
// |         |            |        | step at world day 0 (the unit-level twin of                    |
// |         |            |        | SeasonLoopProgressionTests' bootstrap-league proof), and a     |
// |         |            |        | save/restore round trip past int32's range (the i64 field      |
// |         |            |        | width). ERR-028-009 (F8): the sentinel-world-day refusal, plus |
// |         |            |        | a one-below-the-sentinel PASS case so the guard is proven       |
// |         |            |        | narrow.                                                         |
// | 1.2     | 2026-08-09 | —      | Mutation-audit locks for seven FromBlocks/ToBlocks guards       |
// |         |            |        | proven dead by deleting each one and observing the whole suite |
// |         |            |        | stay green: the ascending club-id and player-id checks, the    |
// |         |            |        | id-cursor-vs-highest-carried-id guard, FromBlocks copying (not |
// |         |            |        | borrowing) its input arrays, FromBlocks's own cross-club        |
// |         |            |        | uniqueness call (SeedFrom's twin was already locked), ToBlocks  |
// |         |            |        | returning copies, and ValidateBatch's positional club-id check  |
// |         |            |        | isolated from the downstream player-id/length checks via a      |
// |         |            |        | two-club swap that keeps every OTHER field correct. Each was    |
// |         |            |        | proven by deleting the guard, confirming exactly the new test   |
// |         |            |        | failed, and restoring it.                                       |
// | 1.3     | 2026-08-09 | —      | ERR-028-014: three tests changed meaning, two of them INVERTED  |
// |         |            |        | — they had been locking the defect as intended behaviour.       |
// |         |            |        | AdvanceDay_FirstCall_AdvancesExactlyOneDay renamed               |
// |         |            |        | AdvanceDay_FirstCall_ReplaysFromTheSeedDay: it asserted one day |
// |         |            |        | of accrual "however far ahead the first call's world day is —   |
// |         |            |        | the store cannot know which day the career began accruing on",  |
// |         |            |        | which was false (SeedFrom is handed the seed day) — now asserts |
// |         |            |        | the full 300-day replay. AdvanceDay_AtRetirementAge_Flags-       |
// |         |            |        | ButDoesNotRemove advances one further day (BaseDay + 1, not     |
// |         |            |        | BaseDay), since the seed day is now a no-op. AdvanceDay_OneDay-  |
// |         |            |        | BelowTheSentinel_StillAdvancesNormally had to RESEED near the    |
// |         |            |        | sentinel: with the cursor anchored, seeding at BaseDay and       |
// |         |            |        | advancing to sentinel-1 became a ~4.29-billion-day replay (Ad-  |
// |         |            |        | vanceDay replays a gap day by day) — it HUNG the suite; new      |
// |         |            |        | SeedOneClubAt helper seeds two days below the sentinel instead.  |
// |         |            |        | + FromBlocks_ANeverAdvancedSentinelCursor_IsRefused, the new     |
// |         |            |        | lock on FromBlocks' refusal of the sentinel cursor.              |
// | 1.4     | 2026-08-10 | —      | AR pass 3 (ERR-028-015): three locks ERR-028-014 had silently    |
// |         |            |        | DISARMED, each because AdvanceDay(seedDay) became a no-op and    |
// |         |            |        | the test called exactly that. Mutation-verified: deleting the    |
// |         |            |        | retirement age comparison left 85/85 green; reinstating the      |
// |         |            |        | ERR-028-006 clamp no longer failed its own designated lock.      |
// |         |            |        | All three now advance to a LIVED day. + the backward-call lock  |
// |         |            |        | (the same-day repeat CANNOT discriminate the idempotency guard)  |
// |         |            |        | and SeedFrom_AtTheSentinelWorldDay_IsRefused.                    |
// | 1.5     | 2026-08-10 | —      | AR pass 4. AdvanceDay_BackwardCall_DoesNotEvaluateRetirement      |
// |         |            |        | isolates the else-branch bare `return;` from the `if` condition  |
// |         |            |        | above it — the condition alone guards cursor regression (already |
// |         |            |        | locked); `return;` additionally stops the §3.4 retirement        |
// |         |            |        | evaluation running on a non-advancing backward call. New guard   |
// |         |            |        | locks with no prior isolating test: SeedFrom_ADuplicateClubId_    |
// |         |            |        | IsRefused, SeedFrom_NullSquadsArray_IsRefused, SeedFrom_ANull-    |
// |         |            |        | SquadElement_IsRefused, FromBlocks_NullClubsArray_IsRefused,      |
// |         |            |        | FromBlocks_AnUnboundClubElement_IsRefused, AdvanceDay_BatchEntry- |
// |         |            |        | Unbound_IsRefused. + SeedFrom_OneDayBelowTheSentinelWorldDay_     |
// |         |            |        | Succeeds, proving the SeedFrom sentinel guard is narrow (mirrors  |
// |         |            |        | AdvanceDay's existing one-below-the-sentinel case). Every new     |
// |         |            |        | lock proven by mutation: guard deleted, new test observed to fail |
// |         |            |        | and no other test to fail, guard restored.                        |
// | 1.6     | 2026-08-10 | —      | AR pass 5 (time/arithmetic axis), High (ERR-028-018). Five        |
// |         |            |        | locks rebaselined by exactly +1 day of accrual now that the seed  |
// |         |            |        | day's own band step is credited: AdvanceDay_FirstCall_Replays-    |
// |         |            |        | FromTheSeedDay (300 -> 301 * SquadSize), AdvanceDay_BackwardCall_ |
// |         |            |        | DoesNotRegressTheCursor (10 -> 11 * SquadSize), AdvanceDay_One-   |
// |         |            |        | DayBelowTheSentinel_StillAdvances (1 -> 2 * SquadSize), and       |
// |         |            |        | AdvanceDay_AtWorldDayZero_EachAgeBand... (+1/0/-1 -> +2/0/-2).    |
// |         |            |        | + new lock AdvanceDay_AWholeGrowthBandTraversal_GainsExactlyOne-  |
// |         |            |        | PointPerYear_AndLeavesNoResidue — none of the existing growth     |
// |         |            |        | locks measured a full band TRAVERSAL against the fixed band edge, |
// |         |            |        | which is exactly what let the seed-day-uncredited defect through  |
// |         |            |        | five prior AR passes. Asserts both points gained AND a zero       |
// |         |            |        | residue at the band edge — a points-only assertion would still    |
// |         |            |        | pass a future regression that reintroduced a residue while        |
// |         |            |        | rounding the count back up. Mutation-verified: reverting the      |
// |         |            |        | ProgressionEngine seed credit fails all five rebaselined locks    |
// |         |            |        | plus the new traversal lock (6 of 109), and nothing else.         |
// | 1.7     | 2026-08-11 | —      | AR pass 6, M3: + FromBlocks_AnEmptyClub_IsRefused and              |
// |         |            |        | FromBlocks_AClubAboveClubSquadSize_IsRefused — a club outside      |
// |         |            |        | [1, CLUB_SQUAD_SIZE] must be refused at FromBlocks, not left to    |
// |         |            |        | throw from SquadFor mid-round. Mutation-verified: reverting        |
// |         |            |        | ProgressionEngine's RequireClubSizeInRange call fails both.        |
// | 1.8     | 2026-08-11 | —      | AR pass 8. **L-3:** + SeedFrom_APlayerAtIntMaxValue_IsRefused —    |
// |         |            |        | locks ProgressionEngine.cs 1.6's fix (SeedFrom's id-cursor         |
// |         |            |        | overflow now delegates to the shared gate). Mutation-verified:     |
// |         |            |        | reverting the delegation fails exactly this new lock. **L-4:**     |
// |         |            |        | DefaultLife()'s CurrentAbility was a bare 3000, mismatched against |
// |         |            |        | Player()'s default attributes (which every Player()-built record   |
// |         |            |        | in this file shares) — ProgressionSaveCodec.cs 1.5's new           |
// |         |            |        | CurrentAbility == ComputeCA(attributes) gate refused every         |
// |         |            |        | FromBlocks(..., DefaultLife()) call in this file. Retuned to a     |
// |         |            |        | computed DefaultComputedCurrentAbility field (position-invariant   |
// |         |            |        | for uniform default attributes, so one value covers every fixture  |
// |         |            |        | here) rather than a second magic literal. No test assertions       |
// |         |            |        | changed — this is the fixture keeping pace with a production gate. |
#endregion
