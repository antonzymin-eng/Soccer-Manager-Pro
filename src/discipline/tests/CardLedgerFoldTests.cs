// File:     src/discipline/tests/CardLedgerFoldTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13 (#44 C1/C2 adversarial review round 3, M13 — two Commit atomicity tests via
//           CommitWithExplicitConfig — v1.1)
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

        private static CardIssuedEvent Card(int recipient, byte kind = DisciplineConstants.CARD_KIND_YELLOW) =>
            new CardIssuedEvent(recipient, kind, foulOrdinal: 0xFFFF);

        private static SubstitutionEvent Sub(int outgoing, int incoming) =>
            new SubstitutionEvent(outgoing, incoming, team: 0, substitutionReason: 0);

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
                new FakeLedgerTap().Add(Card(5, kind: DisciplineConstants.CARD_KIND_SECOND_YELLOW)));

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
                fold.ObserveTick(new FakeLedgerTap().Add(Card(slot, kind: DisciplineConstants.CARD_KIND_RED)));

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
#endregion
