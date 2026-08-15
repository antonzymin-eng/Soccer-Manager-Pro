// File:     src/discipline/tests/CardLedgerFoldTests.cs
// Created:  2026-08-13
// Modified: 2026-08-15 (reviewed-findings pass, L22 — v1.4: new SubstitutionAndCard_InTheSameTick_
//           AttributesToIncoming case authoring a substitution and card in the SAME ObserveTick call,
//           pairing the CardLedgerFold.cs doc fix naming the MatchEngine.RunResolvePhase ordering
//           dependency this fold's occupancy correctness rests on.)
//           Prior: 2026-08-15 (reviewed-findings pass, M25 — v1.3: three new isolating tests —
//           Constructor_EmptyOccupancySeed_Throws, Constructor_NegativeNonSentinelSeedEntry_Throws,
//           Substitution_WithUnmappedOutgoing_Throws — for guards the reviewer's mutation pass found
//           had no test that failed under deletion.)
//           Prior: 2026-08-13 (#44 C1/C2 adversarial review round 5, M19(b) — isolating cases for the
//           accumBan and straightRedBan RequireCommittableConfig guards, which had none — v1.2)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.1 (the occupancy fold) / §4.3 (the tap read);
//           FR-DC-002/003/004/005/006/010; F1/F4; §5 T-DC-FOLD-001/002/003, T-DC-DET-001;
//           Code Standards #20
// Purpose:  Unit tests for CardLedgerFold, driven by a hand-authored fake IDisciplineTickLedgerTap
//           (the #37 FakeTap pattern) — card-to-occupant attribution, the occupancy shift across a
//           substitution (before/after), the FR-DC-004 unknown-ordinal ignore posture, F1/F4 fail-loud
//           gates, the buffer-then-commit-once contract (a mid-fixture state must stay empty), the
//           double-commit/observe-after-commit refusals, Commit's atomicity under a bad [GT] (M13),
//           and two-run determinism.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.EventSystem;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>A tap holding authored records — mirrors #37's <c>MatchAnalyticsAggregatorTests.FakeTap</c>.</summary>
    internal sealed class FakeLedgerTap : IDisciplineTickLedgerTap
    {
        private readonly List<byte> _ordinals = new List<byte>();
        private readonly List<object> _records = new List<object>();

        public int RecordCount => _ordinals.Count;

        public byte OrdinalAt(int index) => _ordinals[index];

        public T RecordAt<T>(int index) where T : struct => (T)_records[index];

        public FakeLedgerTap Add<T>(in T record) where T : struct
        {
            _ordinals.Add(EventRegistry.GetOrdinal<T>());
            _records.Add(record);
            return this;
        }

        /// <summary>Adds a record under an ordinal #44 does not fold (the FR-DC-004 path).</summary>
        public FakeLedgerTap AddUnknown(byte ordinal)
        {
            _ordinals.Add(ordinal);
            _records.Add(default(CardIssuedEvent));   // never read — the fold must skip it by ordinal alone
            return this;
        }
    }

    /// <summary>Tests for <see cref="CardLedgerFold"/>.</summary>
    [TestFixture]
    internal sealed class CardLedgerFoldTests
    {
        private const int Competition = DisciplineConstants.LEAGUE_COMPETITION_KEY;
        private const int SquadSize = 22;     // on-pitch slots [0, 22)
        private const int OccupancyLength = 36;   // + bench ids [22, 36) — 22 + teamId*7 + benchIndex

        private static int BenchId(int teamId, int benchIndex) => SquadSize + teamId * 7 + benchIndex;

        private static int[] Occupancy(params (int agentId, int playerId)[] mapped)
        {
            var arr = new int[OccupancyLength];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = CardLedgerFold.NO_PLAYER;
            }
            foreach ((int agentId, int playerId) in mapped)
            {
                arr[agentId] = playerId;
            }
            return arr;
        }

        private static CardIssuedEvent Card(int recipient, byte kind = DisciplineConstants.CardKindYellow) =>
            new CardIssuedEvent(recipient, kind, foulOrdinal: 0xFFFF);

        private static SubstitutionEvent Sub(int outgoing, int incoming) =>
            new SubstitutionEvent(outgoing, incoming, team: 0, substitutionReason: 0);

        // ── Constructor guards (M25) ──────────────────────────────────────────────
        //
        // Mutation-verified: neutering either guard below (replacing the `if` with `if (false)`) left
        // the whole 105-test suite green before this pair existed — nothing exercised the empty seed
        // or a negative-non-NO_PLAYER seed entry.

        [Test]
        public void Constructor_EmptyOccupancySeed_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CardLedgerFold(Array.Empty<int>(), Competition));
        }

        [Test]
        public void Constructor_NegativeNonSentinelSeedEntry_Throws()
        {
            // -1 is CardLedgerFold.NO_PLAYER (legal, an unused slot). -2 is neither a valid player id
            // nor the sentinel.
            var seed = new[] { 100, -2 };
            Assert.Throws<ArgumentException>(() => new CardLedgerFold(seed, Competition));
        }

        // ── Basic attribution ─────────────────────────────────────────────────────

        [Test]
        public void Card_AttributesToTheOccupantOfRecipient()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            var tap = new FakeLedgerTap().Add(Card(5));

            fold.ObserveTick(tap);

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);
            fold.Commit(rules);

            Assert.AreEqual(1, state.EntryFor(100, Competition).Yellows, "slot 5's occupant, player 100, must be booked");
        }

        // ── The occupancy test that matters: before/after a substitution ─────────

        [Test]
        public void CardBeforeSubstitution_AttributesToOutgoing_CardAfter_AttributesToIncoming()
        {
            int outgoingPlayer = 200;
            int incomingPlayer = 300;
            int slot = 5;
            int bench = BenchId(teamId: 0, benchIndex: 0);   // 22

            var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), Competition);

            // Tick 1: a card at `slot` while the outgoing player still occupies it.
            fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)));
            // Tick 2: the substitution moves `slot`'s occupancy to the incoming player.
            fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: bench)));
            // Tick 3: a second card at the SAME slot, now occupied by the incoming player.
            fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)));

            var state = new DisciplineState();
            fold.Commit(new DisciplineRules(state));

            Assert.AreEqual(1, state.EntryFor(outgoingPlayer, Competition).Yellows,
                "the card BEFORE the substitution must attribute to the player who was subbed off");
            Assert.AreEqual(1, state.EntryFor(incomingPlayer, Competition).Yellows,
                "the card AFTER the substitution, at the same slot, must attribute to the player who came on");
        }

        [Test]
        public void SubstitutionAndCard_InTheSameTick_AttributesToIncoming()
        {
            // L22: this fold's occupancy correctness rests on an engine event-ordering guarantee it
            // does not itself enforce — MatchEngine.RunResolvePhase flushes a queued SubstitutionEvent
            // BEFORE issuing cards within the same phase/tick, so a substitution and a card that both
            // land in ONE ObserveTick call (as the tap's own canonical publish order, not split across
            // ticks like the before/after test above) must still attribute the card to the INCOMING
            // player.
            int outgoingPlayer = 200;
            int incomingPlayer = 300;
            int slot = 5;
            int bench = BenchId(teamId: 0, benchIndex: 0);

            var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), Competition);

            // One tick: the substitution record precedes the card record, matching the tap's own
            // canonical publish order within the phase.
            var tap = new FakeLedgerTap()
                .Add(Sub(outgoing: slot, incoming: bench))
                .Add(Card(slot));
            fold.ObserveTick(tap);

            var state = new DisciplineState();
            fold.Commit(new DisciplineRules(state));

            Assert.IsFalse(state.HasEntry(outgoingPlayer, Competition),
                "the outgoing player must carry NO card from this tick — he was replaced before the card.");
            Assert.AreEqual(1, state.EntryFor(incomingPlayer, Competition).Yellows,
                "the card, issued in the same tick as the substitution, must attribute to the player who came on.");
        }

        // ── FR-DC-004: unknown ordinals ignored, known ones still fold in the same batch ──

        [Test]
        public void UnknownOrdinal_IsIgnored_KnownOrdinalInTheSameBatchStillFolds()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            // 0x01 is neither CardIssuedEvent's ordinal (0x06, #17 Appendix A) nor SubstitutionEvent's
            // (0x08) — an ordinary forward-compatibility case, e.g. a producer landed after this fold
            // was written.
            var tap = new FakeLedgerTap()
                .AddUnknown(0x01)
                .Add(Card(5));

            Assert.DoesNotThrow(() => fold.ObserveTick(tap));

            var state = new DisciplineState();
            fold.Commit(new DisciplineRules(state));

            // Non-vacuity: if the unknown-ordinal branch quietly ate the WHOLE tick instead of skipping
            // just that one record, this would also read 0 — so the known card must actually land.
            Assert.AreEqual(1, state.EntryFor(100, Competition).Yellows,
                "the unknown record must be skipped, not the whole tick");
        }

        // ── F1 ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Card_ForAnAgentIdWithNoPlayerOccupancy_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);   // slot 6 is unmapped (NO_PLAYER)
            var tap = new FakeLedgerTap().Add(Card(6));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        [Test]
        public void Card_ForAnOutOfRangeAgentId_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            var tap = new FakeLedgerTap().Add(Card(999));   // far past OccupancyLength

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        [Test]
        public void Substitution_WithUnmappedIncoming_Throws()
        {
            int bench = BenchId(0, 0);
            // Slot 5 is mapped; the bench id it substitutes onto is NOT — an incomplete lineup seed.
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            var tap = new FakeLedgerTap().Add(Sub(outgoing: 5, incoming: bench));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        [Test]
        public void Substitution_WithUnmappedOutgoing_Throws()
        {
            // M25: the mirror case — the INCOMING bench id is mapped, but the OUTGOING on-pitch slot
            // is not. ApplySubstitution's OccupantOf(outgoingAgentId, "SubstitutionEvent.Outgoing")
            // read exists purely to fail loud here; mutation-verified by deleting that call, which left
            // the whole suite green before this test existed (nothing else exercises an unmapped
            // outgoing slot with a validly-mapped incoming one).
            int bench = BenchId(0, 0);
            var fold = new CardLedgerFold(Occupancy((bench, 300)), Competition);   // slot 5 is unmapped
            var tap = new FakeLedgerTap().Add(Sub(outgoing: 5, incoming: bench));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        // ── F4: fails at ObserveTick, not deferred to Commit ──────────────────────

        [Test]
        public void CardKind3_ThrowsAtObserveTick_NotDeferredToCommit()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            var tap = new FakeLedgerTap().Add(Card(5, kind: 3));

            Assert.Throws<ArgumentOutOfRangeException>(() => fold.ObserveTick(tap));

            // The record never entered the buffer — proving F4 fired at the tap, not merely that a
            // later Commit would also have refused it.
            Assert.AreEqual(0, fold.PendingCardCount,
                "an unknown card kind must be refused before it is ever buffered");
        }

        // ── Buffering: mid-fixture, the state stays EMPTY; only Commit writes it ──

        [Test]
        public void ObserveTick_BuffersWithoutWritingTheState_OnlyCommitWrites()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100), (6, 101)), Competition);

            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));
            fold.ObserveTick(new FakeLedgerTap().Add(Card(6)));

            Assert.AreEqual(2, fold.PendingCardCount, "both cards are buffered");

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            // The anti-mid-fixture-save property: nothing has reached the state yet, so a save taken at
            // this exact moment (before Commit) would see no discipline changes from this fixture at all.
            Assert.AreEqual(0, state.Count, "no card may reach persisted state before Commit");

            int applied = fold.Commit(rules);

            Assert.AreEqual(2, applied);
            Assert.AreEqual(2, state.Count, "both cards land only once Commit runs");
        }

        // ── Commit / ObserveTick sequencing ───────────────────────────────────────

        [Test]
        public void Commit_Twice_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));
            fold.Commit(new DisciplineRules(new DisciplineState()));

            Assert.Throws<InvalidOperationException>(() => fold.Commit(new DisciplineRules(new DisciplineState())));
        }

        [Test]
        public void ObserveTick_AfterCommit_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));
            fold.Commit(new DisciplineRules(new DisciplineState()));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(new FakeLedgerTap()));
        }

        [Test]
        public void Commit_NullRules_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            Assert.Throws<ArgumentNullException>(() => fold.Commit(null));
        }

        [Test]
        public void ObserveTick_NullTap_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            Assert.Throws<ArgumentNullException>(() => fold.ObserveTick(null));
        }

        // ── M13: Commit is atomic — a bad [GT] refuses the WHOLE list, not card k onward ──
        //
        // DisciplineConstants' [GT] fields are `public static readonly`, resolved once at type
        // initialisation to their non-negative defaults — no test in this process can bind a bad
        // config value before that first read happens (the same L5/DisciplineRulesTests constraint).
        // CommitWithExplicitConfig takes the four guarded values as parameters for exactly this
        // reason, so these tests drive the real Commit body through an explicit invalid value.

        [Test]
        public void Commit_WithAnInvalidYellowThreshold_RefusesBeforeApplyingAnyCard_AndLeavesTheFoldUncommitted()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100), (6, 101)), Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).Add(Card(6)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            Assert.Throws<InvalidOperationException>(() => fold.CommitWithExplicitConfig(
                rules,
                yellowThreshold: 0,   // invalid — RequireYellowThreshold refuses below 1
                accumBan: DisciplineConstants.AccumBanMatches,
                secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.AreEqual(0, state.Count,
                "Neither buffered card may reach persisted state when the [GT] guard refuses the "
                + "commit — M13's atomicity property.");
            Assert.AreEqual(2, fold.PendingCardCount, "the buffer itself must be untouched by the refusal.");

            // The refused attempt must not have latched _committed = true either — a genuinely atomic
            // guard runs before ANY state is touched, including the fold's own commit flag.
            int applied = fold.Commit(rules);
            Assert.AreEqual(2, applied, "a subsequent, correctly-configured Commit must still succeed.");
            Assert.AreEqual(2, state.Count);
        }

        [Test]
        public void Commit_WithAnInvalidBanLength_RefusesBeforeApplyingAnyCard()
        {
            // The RequireBanLength sibling of the test above — a second-yellow card mid-list must not
            // have its yellow committed (AddYellow's effect) while the whole card is refused for the
            // ban it also carries (the M4 atomicity property, one layer up at the fold's own commit).
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            fold.ObserveTick(
                new FakeLedgerTap().Add(Card(5, kind: DisciplineConstants.CardKindSecondYellow)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            Assert.Throws<InvalidOperationException>(() => fold.CommitWithExplicitConfig(
                rules,
                yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                accumBan: DisciplineConstants.AccumBanMatches,
                secondYellowBan: -1,   // invalid
                straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.AreEqual(0, state.Count, "the card's yellow must not land while its ban length is refused.");
            Assert.AreEqual(1, fold.PendingCardCount);
        }

        // ── M19(b): the two guards with no prior isolating case ───────────────────────
        //
        // A reviewer-executed mutant deleting the accumBan or straightRedBan guard from
        // RequireCommittableConfig survived 96/96 — no test here passed an invalid value for either,
        // only for yellowThreshold and secondYellowBan. RequireCommittableConfig validates all four
        // unconditionally before the loop runs (M17), so an ordinary card is enough to exercise it —
        // the refused fixture's own cards need not touch the guarded constant.

        [Test]
        public void Commit_WithAnInvalidAccumBan_RefusesBeforeApplyingAnyCard()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            Assert.Throws<InvalidOperationException>(() => fold.CommitWithExplicitConfig(
                rules,
                yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                accumBan: -1,   // invalid
                secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.AreEqual(0, state.Count, "no card may reach persisted state while accumBan is refused.");
            Assert.AreEqual(1, fold.PendingCardCount, "the buffer itself must be untouched by the refusal.");
        }

        [Test]
        public void Commit_WithAnInvalidStraightRedBan_RefusesBeforeApplyingAnyCard()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            Assert.Throws<InvalidOperationException>(() => fold.CommitWithExplicitConfig(
                rules,
                yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                accumBan: DisciplineConstants.AccumBanMatches,
                secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                straightRedBan: -1));   // invalid

            Assert.AreEqual(0, state.Count, "no card may reach persisted state while straightRedBan is refused.");
            Assert.AreEqual(1, fold.PendingCardCount, "the buffer itself must be untouched by the refusal.");
        }

        // ── Determinism (FR-DC-021) ────────────────────────────────────────────────

        [Test]
        public void SameRecordSequence_FoldedTwiceIntoFreshStates_YieldsIdenticalEncodedBytes()
        {
            int slot = 5;
            int bench = BenchId(0, 0);
            int outgoingPlayer = 200;
            int incomingPlayer = 300;

            byte[] EncodeOneRun()
            {
                var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), Competition);
                fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)));
                fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: bench)));
                fold.ObserveTick(new FakeLedgerTap().Add(Card(slot, kind: DisciplineConstants.CardKindRed)));

                var state = new DisciplineState();
                fold.Commit(new DisciplineRules(state));
                return DisciplineSaveCodec.Encode(state);
            }

            byte[] first = EncodeOneRun();
            byte[] second = EncodeOneRun();

            CollectionAssert.AreEqual(first, second, "the same fixture events must produce byte-identical state");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial suite (#44 T-phase tests): basic attribution, the        |
// |         |            |        | before/after substitution occupancy test, FR-DC-004's unknown-   |
// |         |            |        | ordinal-ignored/known-still-folds pairing, F1 (unmapped agent,   |
// |         |            |        | out-of-range agent, unmapped substitution incoming), F4 firing   |
// |         |            |        | at ObserveTick, the buffer-then-commit-once anti-mid-fixture-save|
// |         |            |        | property, double-commit/observe-after-commit refusals, and       |
// |         |            |        | two-run determinism via DisciplineSaveCodec.Encode.               |
// | 1.1     | 2026-08-13 | —      | AR round 3 fix (M13): two new tests drive Commit's atomicity      |
// |         |            |        | through CommitWithExplicitConfig with an explicit invalid          |
// |         |            |        | yellow threshold / ban length — DisciplineConstants' [GT]s cannot  |
// |         |            |        | be rebound in this process, so the public Commit alone could not   |
// |         |            |        | exercise the guard. Each asserts the pending cards never reach     |
// |         |            |        | DisciplineState and the fold survives to Commit successfully once  |
// |         |            |        | given a valid config.                                              |
// | 1.2     | 2026-08-13 | —      | AR round 5 fix (M19(b)): two new tests, Commit_WithAnInvalid-      |
// |         |            |        | AccumBan_RefusesBeforeApplyingAnyCard and Commit_WithAnInvalid-     |
// |         |            |        | StraightRedBan_RefusesBeforeApplyingAnyCard — a reviewer-executed   |
// |         |            |        | mutant deleting either guard from RequireCommittableConfig survived |
// |         |            |        | 96/96 because no test drove an invalid value through them; the      |
// |         |            |        | yellowThreshold and secondYellowBan guards already had isolating    |
// |         |            |        | cases (v1.1), these two did not.                                    |
// | 1.3     | 2026-08-15 | —      | Reviewed-findings fix (M25). Three new tests, each mutation-       |
// |         |            |        | verified against the guard it locks: Constructor_EmptyOccupancy-   |
// |         |            |        | Seed_Throws (the :100 empty-seed ArgumentException), Constructor_  |
// |         |            |        | NegativeNonSentinelSeedEntry_Throws (the :113 negative-non-        |
// |         |            |        | NO_PLAYER ArgumentException), Substitution_WithUnmappedOutgoing_   |
// |         |            |        | Throws (the F1 OccupantOf(outgoingAgentId, ...) read at            |
// |         |            |        | ApplySubstitution's :318, mirroring the existing incoming-side     |
// |         |            |        | test). All three replicate a reviewer finding that neutering the   |
// |         |            |        | guard left 105/105 tests green.                                    |
// | 1.4     | 2026-08-15 | —      | Reviewed-findings fix (L22): new                                   |
// |         |            |        | SubstitutionAndCard_InTheSameTick_AttributesToIncoming — a          |
// |         |            |        | substitution and a card in ONE ObserveTick call (the before/after   |
// |         |            |        | test above splits them across three ticks). Pairs the CardLedger    |
// |         |            |        | Fold.cs doc fix naming MatchEngine.RunResolvePhase's flush-before-  |
// |         |            |        | cards ordering as the guarantee this fold's occupancy correctness   |
// |         |            |        | rests on but does not itself enforce.                               |
#endregion
