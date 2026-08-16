// File:     src/discipline/tests/CardLedgerFoldTests.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest of all and later still (round-4 reviewed-findings pass, L-D/M-C — v1.10:
//           L-D — new Constructor_NullOccupancySeed_Throws beside Constructor_EmptyOccupancySeed_Throws;
//           the null-seed guard had no isolating lock (mutation-verified: deleting it left the suite
//           green). M-C — Substitution_WithAnOccupiedOnPitchIncoming_ThrowsInsteadOfDestroyingThe-
//           OutgoingsMapping's "guard runs before any write" proof was FALSE: it inferred "nothing was
//           written" by building a SECOND, fresh fold from the same seed and checking that one committed
//           correctly, but the constructor copies the seed array, so a fresh fold is pristine regardless
//           of ordering within the faulted instance — the two assertions passed identically either way.
//           Corrected the comment to state what the test actually establishes (the malformed Incoming
//           is refused, the refusal names the id) and to record that pre-write ordering is unobservable
//           through the public surface (M3's poison latch closes the fold before Commit is reachable).
//           Locked ordering for real via the new CardLedgerFold.OccupancyAt(int) internal accessor
//           (CardLedgerFold.cs v1.13): asserts the SAME faulted fold's occupancy at both slots 5 and 6
//           is unchanged. Mutation-verified: moving both ERR-044-022 guards to after the write/clear
//           pair made this test fail (slot 5 read 101, not 100) against the unmutated 145/146-passing
//           baseline; reverted after observing the failure.)
// Modified: 2026-08-16, latest of all (M-B, adversarial review — v1.9: the onPitchAgentIdCount range
//           guard (constructor's ArgumentOutOfRangeException) had no lock — deleting it left all 143
//           discipline tests green. New Constructor_OnPitchAgentIdCountOutOfRange_Throws (T-DC-FOLD-003)
//           isolates both edges, 0 and seed.Length + 1, plus a negative value, mutation-verified against
//           the guard it locks.)
// Modified: 2026-08-16, latest again (reviewed findings pass, finding A — v1.8: every CardLedgerFold
//           constructor call updated for the new required onPitchAgentIdCount parameter (ERR-044-022),
//           passing SquadSize (22, this file's existing on-pitch-boundary constant). Two new locks:
//           Substitution_WithAnOccupiedOnPitchIncoming_ThrowsInsteadOfDestroyingTheOutgoingsMapping
//           drives the finding's own exact probe ({5->100, 6->101, 22->102}, Sub(Outgoing=5,
//           Incoming=6)) and proves the refused call left player 100's mapping untouched;
//           Substitution_WithABenchOutgoing_Throws isolates the mirror guard (Outgoing must be
//           on-pitch) with a bench-vs-bench pair so the Incoming guard cannot mask it.
//           Constructor_NegativeNonSentinelSeedEntry_Throws now passes seed.Length (2), not SquadSize,
//           as the new parameter — its 2-entry seed can never satisfy SquadSize's 22, and passing
//           SquadSize there would trip the new ERR-044-022 range guard before ever reaching the
//           negative-entry check this test exists to isolate.
// Modified: 2026-08-16, later (reviewed findings pass, M1/M3 — v1.7: FakeLedgerTap gained CurrentTick/
//           AtTick(...) (M3); every pre-existing multi-call-on-one-fold test now chains consecutive
//           ticks. New M1 locks: a doubly-mapped construction seed throws and names both agent ids plus
//           the player id; a card naming a just-vacated substitution slot throws F1 instead of
//           misattributing; a self-colliding substitution (outgoing == incoming) throws. New M3 locks:
//           a skipped tick throws and names both ticks; the first call accepts any starting tick;
//           repeating the same tick throws; a part-way tick failure latches the fold shut against even
//           a consecutive follow-up tick.)
// Modified: 2026-08-16 (adversarial-review M2 — the four RequireCommittableConfig rejection cases each
//           assert the refusal NAMES its own [GT], and the section comment records the executed
//           per-guard mutation verification that each of the four arguments is isolating — v1.6)
// Modified: 2026-08-15, later (reviewed findings pass, L4 — v1.5: the Competition constant's
//           DisciplineConstants.LEAGUE_COMPETITION_KEY reference renamed for that constant's ALL_CAPS ->
//           LeagueCompetitionKey rename (DisciplineConstants.cs v1.5). No behaviour change.)
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

        // M3: defaults to 0. A fold's FIRST ObserveTick call accepts any tick (first-call anchoring —
        // CardLedgerFold.RequireConsecutive), so single-call tests need not set this at all; a test that
        // calls ObserveTick more than once on the same fold must chain .AtTick(...) with consecutive
        // values, or the M3 guard refuses it exactly as it should.
        public ulong CurrentTick { get; private set; }

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

        /// <summary>Sets the tick this tap reports as <see cref="CurrentTick"/> (M3).</summary>
        public FakeLedgerTap AtTick(ulong tick)
        {
            CurrentTick = tick;
            return this;
        }
    }

    /// <summary>Tests for <see cref="CardLedgerFold"/>.</summary>
    [TestFixture]
    internal sealed class CardLedgerFoldTests
    {
        private const int Competition = DisciplineConstants.LeagueCompetitionKey;
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
            Assert.Throws<ArgumentException>(() => new CardLedgerFold(Array.Empty<int>(), SquadSize, Competition));
        }

        // ── L-D (round-4 reviewed-findings pass): the null-seed guard had no isolating lock ──────
        //
        // Mutation-verified: deleting the `if (occupancyByAgentId == null) throw ...` guard (replacing
        // it with `if (false)`) left the whole suite green before this test existed — the empty-seed
        // and negative-entry tests above both pass a non-null array, so neither exercises this branch.
        // Every OTHER null guard in this assembly (Commit_NullRules_Throws, ObserveTick_NullTap_Throws)
        // already has one; this was the one gap.

        [Test]
        public void Constructor_NullOccupancySeed_Throws()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => new CardLedgerFold(null, SquadSize, Competition));

            Assert.AreEqual("occupancyByAgentId", ex.ParamName);
        }

        [Test]
        public void Constructor_NegativeNonSentinelSeedEntry_Throws()
        {
            // -1 is CardLedgerFold.NO_PLAYER (legal, an unused slot). -2 is neither a valid player id
            // nor the sentinel. onPitchAgentIdCount is the seed's own length (2), not SquadSize — a
            // 2-entry seed can never satisfy SquadSize's 22, so passing SquadSize here would trip the
            // ERR-044-022 range guard first and never isolate THIS guard at all.
            var seed = new[] { 100, -2 };
            Assert.Throws<ArgumentException>(() => new CardLedgerFold(seed, seed.Length, Competition));
        }

        // ── M-B (reviewed findings pass): the onPitchAgentIdCount range guard — T-DC-FOLD-003 ──────
        //
        // Mutation-verified: deleting the ArgumentOutOfRangeException guard in the constructor (the
        // `if (onPitchAgentIdCount <= 0 || onPitchAgentIdCount > occupancyByAgentId.Length)` check)
        // left all 143 pre-existing discipline tests green — nothing exercised either edge. Isolates
        // both boundaries (0, and seed.Length + 1) plus a negative value.

        [Test]
        public void Constructor_OnPitchAgentIdCountOutOfRange_Throws()
        {
            var seed = Occupancy((5, 100));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CardLedgerFold(seed, 0, Competition),
                "onPitchAgentIdCount == 0 must be refused — the boundary requires strictly greater than zero.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CardLedgerFold(seed, -1, Competition),
                "a negative onPitchAgentIdCount must be refused.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CardLedgerFold(seed, seed.Length + 1, Competition),
                "onPitchAgentIdCount beyond the seed's own length must be refused — every entry at or " +
                "past it is supposed to be one of the seed's own bench ids.");
        }

        // ── M1 (reviewed findings pass): the seed must be one-to-one ──────────────

        [Test]
        public void Constructor_SeedMapsOnePlayerToTwoAgentIds_ThrowsAndNamesBothIdsAndThePlayer()
        {
            // Agent ids 5 and 6 both map to player 100 — a malformed seed of exactly the shape M1
            // guards against (the Appendix C "slot 19" family, ERR-044-001).
            var seed = Occupancy((5, 100), (6, 100));

            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new CardLedgerFold(seed, SquadSize, Competition));

            Assert.That(ex.Message, Does.Contain("player 100"), "the refusal must name the duplicated player id");
            Assert.That(ex.Message, Does.Contain("(5 and 6)"), "the refusal must name both agent ids");
        }

        [Test]
        public void Constructor_SeedWithNO_PLAYERRepeated_DoesNotThrow()
        {
            // NO_PLAYER is the sentinel for "unused", not a player id — repeating it must not trip the
            // M1 one-to-one check (every unused slot legitimately shares the same sentinel).
            var seed = Occupancy((5, 100));   // every other slot in Occupancy(...) is NO_PLAYER by default

            Assert.DoesNotThrow(() => new CardLedgerFold(seed, SquadSize, Competition));
        }

        // ── Basic attribution ─────────────────────────────────────────────────────

        [Test]
        public void Card_AttributesToTheOccupantOfRecipient()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
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

            var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), SquadSize, Competition);

            // Tick 1: a card at `slot` while the outgoing player still occupies it.
            fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)).AtTick(1));
            // Tick 2: the substitution moves `slot`'s occupancy to the incoming player.
            fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: bench)).AtTick(2));
            // Tick 3: a second card at the SAME slot, now occupied by the incoming player.
            fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)).AtTick(3));

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

            var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), SquadSize, Competition);

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

        // ── M1 (reviewed findings pass): the vacated slot is cleared, not left double-booked ──

        [Test]
        public void Substitution_ClearsTheVacatedIncomingSlot_ALaterCardThereThrowsF1()
        {
            // Before the fix, incomingPlayer would occupy BOTH the new outgoing-slot mapping AND the
            // stale incoming (bench) id after a substitution — a card naming either would silently
            // attribute to him. After the fix the stale id is cleared, so a later record naming it must
            // throw F1 instead of misattributing (M1's "the card at the stale slot throws F1" lock).
            int outgoingPlayer = 200;
            int incomingPlayer = 300;
            int slot = 5;
            int bench = BenchId(teamId: 0, benchIndex: 0);

            var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: bench)).AtTick(1));

            // A malformed/later record naming the now-vacated bench id must fail loud, not attribute a
            // card to incomingPlayer a second time via a stale mapping.
            Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().Add(Card(bench)).AtTick(2)));
        }

        [Test]
        public void Substitution_OutgoingEqualsIncoming_Throws()
        {
            // A degenerate/malformed record naming the same agent id as both outgoing and incoming.
            // Without the guard, ApplySubstitution's write (outgoing <- player) and clear (incoming <-
            // NO_PLAYER) would target the same index and the clear would erase the write it just made —
            // silently losing that player's occupancy entirely.
            int slot = 5;
            var fold = new CardLedgerFold(Occupancy((slot, 100)), SquadSize, Competition);

            Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: slot))));
        }

        // ── ERR-044-022 (reviewed findings pass): Incoming must be a bench id, Outgoing must be ─
        // ── on-pitch — the boundary M1's seed-injectivity check alone could not enforce ──────────

        [Test]
        public void Substitution_WithAnOccupiedOnPitchIncoming_ThrowsInsteadOfDestroyingTheOutgoingsMapping()
        {
            // The exact probe the finding names: {5 -> 100, 6 -> 101, 22 -> 102}, Sub(Outgoing=5,
            // Incoming=6) — 6 is an ON-PITCH slot with a live occupant, not a bench id. Before this
            // guard existed this did not throw: _occupancy[5] <- 101 (overwriting player 100's mapping
            // entirely) and _occupancy[6] <- NO_PLAYER (erasing player 101's own mapping too), so a
            // card at slot 5 would land on player 101 while player 100 vanished from the fold with no
            // diagnostic at all.
            var seed = Occupancy((5, 100), (6, 101), (BenchId(0, 0), 102));
            var fold = new CardLedgerFold(seed, SquadSize, Competition);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: 5, incoming: 6))));

            Assert.That(ex.Message, Does.Contain("6"), "the refusal must name the offending Incoming id");

            // M-C (round-4 reviewed findings pass), CORRECTED. What this test establishes above is only
            // that the malformed Incoming is refused and the refusal names the offending id — the two
            // assertions immediately above this comment. Ordering — whether the guard ran BEFORE any
            // write versus after a partial one — is NOT observable through the public surface within
            // one fold: M3's poison latch closes a faulted fold to every further ObserveTick call the
            // instant any record in a tick throws, regardless of WHERE within that tick the throw
            // happened, so Commit is never reachable afterward to reveal what (if anything) got written.
            //
            // A PRIOR version of this test tried to infer "nothing was written" indirectly, by building
            // a SECOND, fresh fold from the identical seed and checking that IT committed correctly.
            // That was a false proof: the constructor COPIES the seed array, so a fresh fold is pristine
            // regardless of what the faulted instance did to its OWN _occupancy — the two assertions
            // passed identically whether or not the guard ran before the write. Mutation-verified: moving
            // both guards in ApplySubstitution to AFTER the write/clear pair left 144/144 tests green,
            // this one included.
            //
            // The ordering question genuinely IS observable through the internal surface this assembly
            // grants its own tests (InternalsVisibleTo, AssemblyInfo.cs) — CardLedgerFold.OccupancyAt
            // reads the SAME faulted fold's own array directly. Asserted for real here, and re-verified
            // against the identical mutation: with the guards moved after the write, OccupancyAt(5) and
            // OccupancyAt(6) below both fail (5 reads 101, 6 reads NO_PLAYER) — the strengthened test
            // genuinely catches the ordering defect the old proxy check could not. Reverted afterward.
            Assert.AreEqual(100, fold.OccupancyAt(5),
                "slot 5's occupancy must be unaffected by the refused substitution — the guard must run "
                + "before any write, not after a partial one.");
            Assert.AreEqual(101, fold.OccupancyAt(6),
                "slot 6's occupancy must be unaffected by the refused substitution — the guard must run "
                + "before any write, not after a partial one.");
        }

        [Test]
        public void Substitution_WithABenchOutgoing_Throws()
        {
            // The mirror of the case above: Outgoing must be an on-pitch id. A bench id going OFF is
            // meaningless — nobody occupies a bench slot in the sense a substitution can remove. Both
            // ids are bench here (Incoming a DIFFERENT bench id from Outgoing) so this isolates the
            // Outgoing guard specifically — an Incoming that is itself on-pitch would trip the OTHER
            // guard first and never reach this one.
            int outgoingBench = BenchId(teamId: 0, benchIndex: 0);
            int incomingBench = BenchId(teamId: 0, benchIndex: 1);
            var fold = new CardLedgerFold(
                Occupancy((outgoingBench, 300), (incomingBench, 101)), SquadSize, Competition);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: outgoingBench, incoming: incomingBench))));

            Assert.That(ex.Message, Does.Contain(outgoingBench.ToString()),
                "the refusal must name the offending Outgoing id");
        }

        // ── FR-DC-004: unknown ordinals ignored, known ones still fold in the same batch ──

        [Test]
        public void UnknownOrdinal_IsIgnored_KnownOrdinalInTheSameBatchStillFolds()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
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
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);   // slot 6 is unmapped (NO_PLAYER)
            var tap = new FakeLedgerTap().Add(Card(6));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        [Test]
        public void Card_ForAnOutOfRangeAgentId_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            var tap = new FakeLedgerTap().Add(Card(999));   // far past OccupancyLength

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        [Test]
        public void Substitution_WithUnmappedIncoming_Throws()
        {
            int bench = BenchId(0, 0);
            // Slot 5 is mapped; the bench id it substitutes onto is NOT — an incomplete lineup seed.
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
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
            var fold = new CardLedgerFold(Occupancy((bench, 300)), SquadSize, Competition);   // slot 5 is unmapped
            var tap = new FakeLedgerTap().Add(Sub(outgoing: 5, incoming: bench));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(tap));
        }

        // ── F4: fails at ObserveTick, not deferred to Commit ──────────────────────

        [Test]
        public void CardKind3_ThrowsAtObserveTick_NotDeferredToCommit()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
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
            var fold = new CardLedgerFold(Occupancy((5, 100), (6, 101)), SquadSize, Competition);

            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).AtTick(1));
            fold.ObserveTick(new FakeLedgerTap().Add(Card(6)).AtTick(2));

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

        // ── M3 (reviewed findings pass): lossless-consumption guard ────────────────

        [Test]
        public void ObserveTick_SkippedTick_ThrowsAndNamesBothTicks()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).AtTick(1));

            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().AtTick(3)));

            Assert.That(refusal.Message, Does.Contain("3"), "the refusal must name the offending tick");
            Assert.That(refusal.Message, Does.Contain("1"), "the refusal must name the last observed tick");

            // The skipped call must not have been silently accepted — nothing from it (there was
            // nothing to buffer anyway) may have landed, and the fold must still be usable for its
            // existing pending card.
            Assert.AreEqual(1, fold.PendingCardCount);
        }

        [Test]
        public void ObserveTick_FirstCall_AcceptsAnyTick()
        {
            // First-call anchoring (mirrors #37 MatchAnalyticsAggregator): there is no "last observed
            // tick" yet, so any starting tick is legal — a fixture need not begin at tick 0.
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);

            Assert.DoesNotThrow(() => fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).AtTick(4200)));
        }

        [Test]
        public void ObserveTick_SameTickTwice_Throws()
        {
            // Not just "must increase" — must be EXACTLY one more. Repeating the same tick is the
            // double-pump shape, not the skip shape, and both must be refused.
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).AtTick(1));

            Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().AtTick(1)));
        }

        [Test]
        public void ObserveTick_AfterPartialTickFailure_LatchesAndRefusesEvenAConsecutiveTick()
        {
            // Card(5) attributes fine; Card(6) has no occupancy mapping and throws F1 — so this tick
            // fails PART-WAY through, after the first record already landed in the buffer.
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            var badTap = new FakeLedgerTap().Add(Card(5)).Add(Card(6)).AtTick(1);

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(badTap));
            Assert.AreEqual(1, fold.PendingCardCount,
                "the first record in the failed tick was already buffered before the second one threw");

            // Even a perfectly consecutive, otherwise-valid next tick must now be refused — tick 1's
            // partial failure poisons the fold permanently, so its buffered cards (which the fold cannot
            // prove are complete) are never compounded with more.
            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).AtTick(2)));
            Assert.That(refusal.Message, Does.Contain("part-way"));
        }

        // ── Commit / ObserveTick sequencing ───────────────────────────────────────

        [Test]
        public void Commit_Twice_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));
            fold.Commit(new DisciplineRules(new DisciplineState()));

            Assert.Throws<InvalidOperationException>(() => fold.Commit(new DisciplineRules(new DisciplineState())));
        }

        [Test]
        public void ObserveTick_AfterCommit_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));
            fold.Commit(new DisciplineRules(new DisciplineState()));

            Assert.Throws<InvalidOperationException>(() => fold.ObserveTick(new FakeLedgerTap()));
        }

        [Test]
        public void Commit_NullRules_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            Assert.Throws<ArgumentNullException>(() => fold.Commit(null));
        }

        [Test]
        public void ObserveTick_NullTap_Throws()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
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
            var fold = new CardLedgerFold(Occupancy((5, 100), (6, 101)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)).Add(Card(6)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.CommitWithExplicitConfig(
                    rules,
                    yellowThreshold: 0,   // invalid — RequireYellowThreshold refuses below 1
                    accumBan: DisciplineConstants.AccumBanMatches,
                    secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                    straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.That(
                refusal.Message,
                Does.Contain(nameof(DisciplineConstants.YellowAccumulationThreshold)),
                "The refusal must name the [GT] that caused it — a bare InvalidOperationException "
                + "cannot tell one of RequireCommittableConfig's four guards from another.");

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
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(
                new FakeLedgerTap().Add(Card(5, kind: DisciplineConstants.CardKindSecondYellow)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.CommitWithExplicitConfig(
                    rules,
                    yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                    accumBan: DisciplineConstants.AccumBanMatches,
                    secondYellowBan: -1,   // invalid
                    straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.That(
                refusal.Message,
                Does.Contain(nameof(DisciplineConstants.SecondYellowBanMatches)),
                "The refusal must name the [GT] that caused it.");
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
        //
        // M2 (2026-08-16), re-verified by execution rather than re-asserted: all FOUR arguments of
        // RequireCommittableConfig(int,int,int,int) now have an isolating case. Each guard was deleted
        // in turn and the suite re-run; each deletion killed exactly one of the four tests above, and
        // each test was killed by exactly one deletion. Worth stating for the secondYellowBan case,
        // whose isolation is not obvious: with its pre-check deleted, the loop's ApplyCard reads the
        // real DisciplineConstants.SecondYellowBanMatches — the -1 reaches the pre-check ONLY — so no
        // throw follows and the test genuinely fails. The four Does.Contain(nameof(...)) assertions are
        // what stop a future mutant satisfying Assert.Throws from the wrong guard.
        //
        // The set of guarded [GT]s is locked separately, in DisciplineConfigCompletenessTests: an
        // isolating case per argument says nothing about a FIFTH constant added to the catalogue with
        // no guard at all, which is the failure this pair of checks exists to cover between them.

        [Test]
        public void Commit_WithAnInvalidAccumBan_RefusesBeforeApplyingAnyCard()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.CommitWithExplicitConfig(
                    rules,
                    yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                    accumBan: -1,   // invalid
                    secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                    straightRedBan: DisciplineConstants.StraightRedBanMatches));

            Assert.That(
                refusal.Message,
                Does.Contain(nameof(DisciplineConstants.AccumBanMatches)),
                "The refusal must name the [GT] that caused it.");
            Assert.AreEqual(0, state.Count, "no card may reach persisted state while accumBan is refused.");
            Assert.AreEqual(1, fold.PendingCardCount, "the buffer itself must be untouched by the refusal.");
        }

        [Test]
        public void Commit_WithAnInvalidStraightRedBan_RefusesBeforeApplyingAnyCard()
        {
            var fold = new CardLedgerFold(Occupancy((5, 100)), SquadSize, Competition);
            fold.ObserveTick(new FakeLedgerTap().Add(Card(5)));

            var state = new DisciplineState();
            var rules = new DisciplineRules(state);

            InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
                () => fold.CommitWithExplicitConfig(
                    rules,
                    yellowThreshold: DisciplineConstants.YellowAccumulationThreshold,
                    accumBan: DisciplineConstants.AccumBanMatches,
                    secondYellowBan: DisciplineConstants.SecondYellowBanMatches,
                    straightRedBan: -1));   // invalid

            Assert.That(
                refusal.Message,
                Does.Contain(nameof(DisciplineConstants.StraightRedBanMatches)),
                "The refusal must name the [GT] that caused it.");
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
                var fold = new CardLedgerFold(Occupancy((slot, outgoingPlayer), (bench, incomingPlayer)), SquadSize, Competition);
                fold.ObserveTick(new FakeLedgerTap().Add(Card(slot)).AtTick(1));
                fold.ObserveTick(new FakeLedgerTap().Add(Sub(outgoing: slot, incoming: bench)).AtTick(2));
                fold.ObserveTick(new FakeLedgerTap().Add(Card(slot, kind: DisciplineConstants.CardKindRed)).AtTick(3));

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
// | 1.5     | 2026-08-15, later | — | Reviewed findings pass, L4. Competition constant's                |
// |         |            |        | DisciplineConstants reference renamed LEAGUE_COMPETITION_KEY ->    |
// |         |            |        | LeagueCompetitionKey. No behaviour change.                          |
// | 1.6     | 2026-08-16 | —      | Adversarial review, M2 (minimal fix). Each of the four              |
// |         |            |        | RequireCommittableConfig rejection cases now asserts the refusal    |
// |         |            |        | message NAMES the [GT] that caused it, so Assert.Throws can no      |
// |         |            |        | longer be satisfied by the wrong guard. The section comment         |
// |         |            |        | records the executed verification behind M2's ask — each of the     |
// |         |            |        | four guards deleted in turn, each deletion killing exactly one of   |
// |         |            |        | the four tests — so no case was missing and none was added; the     |
// |         |            |        | secondYellowBan case's non-obvious isolation (the -1 reaches the    |
// |         |            |        | pre-check only; ApplyCard reads the real constant) is written       |
// |         |            |        | down rather than left to be re-derived. The complementary check     |
// |         |            |        | — that the guarded SET still equals the settable set — is the new   |
// |         |            |        | DisciplineConfigCompletenessTests, cross-referenced here.           |
// | 1.7     | 2026-08-16, later | — | Reviewed findings pass (M1/M3). FakeLedgerTap gained             |
// |         |            |        | CurrentTick/AtTick(ulong) (M3); every existing test that calls    |
// |         |            |        | ObserveTick more than once on the same fold now chains consecutive|
// |         |            |        | ticks, since the fold now enforces continuity. New M1 tests:      |
// |         |            |        | Constructor_SeedMapsOnePlayerToTwoAgentIds_ThrowsAndNamesBothIds- |
// |         |            |        | AndThePlayer, Constructor_SeedWithNO_PLAYERRepeated_DoesNotThrow, |
// |         |            |        | Substitution_ClearsTheVacatedIncomingSlot_ALaterCardThereThrowsF1,|
// |         |            |        | Substitution_OutgoingEqualsIncoming_Throws. New M3 tests:         |
// |         |            |        | ObserveTick_SkippedTick_ThrowsAndNamesBothTicks,                  |
// |         |            |        | ObserveTick_FirstCall_AcceptsAnyTick,                             |
// |         |            |        | ObserveTick_SameTickTwice_Throws,                                 |
// |         |            |        | ObserveTick_AfterPartialTickFailure_LatchesAndRefusesEvenA-       |
// |         |            |        | ConsecutiveTick.                                                   |
// | 1.8     | 2026-08-16, latest again | — | Reviewed findings pass, finding A (ERR-044-022).   |
// |         |            |        | Every CardLedgerFold construction updated for the new required    |
// |         |            |        | onPitchAgentIdCount parameter, passing SquadSize. New tests:       |
// |         |            |        | Substitution_WithAnOccupiedOnPitchIncoming_ThrowsInsteadOf-        |
// |         |            |        | DestroyingTheOutgoingsMapping (the finding's own exact probe —     |
// |         |            |        | {5->100, 6->101, 22->102}, Sub(Outgoing=5, Incoming=6) — now       |
// |         |            |        | throws and leaves player 100's mapping untouched, where it         |
// |         |            |        | previously destroyed it silently) and Substitution_WithABench-     |
// |         |            |        | Outgoing_Throws (the mirror guard, isolated with a bench-vs-bench  |
// |         |            |        | pair so the Incoming guard cannot mask it). Constructor_Negative-  |
// |         |            |        | NonSentinelSeedEntry_Throws now passes seed.Length instead of      |
// |         |            |        | SquadSize, since its 2-entry seed cannot satisfy SquadSize and     |
// |         |            |        | would otherwise trip the new range guard before ever reaching the  |
// |         |            |        | negative-entry check the test isolates.                            |
// | 1.9     | 2026-08-16, latest of all | — | M-B (adversarial review). The onPitchAgentIdCount  |
// |         |            |        | range guard (constructor's ArgumentOutOfRangeException) had no    |
// |         |            |        | lock — deleting it left the whole suite green. New Constructor_    |
// |         |            |        | OnPitchAgentIdCountOutOfRange_Throws (T-DC-FOLD-003) isolates both |
// |         |            |        | edges (0, seed.Length + 1) plus a negative value; mutation-        |
// |         |            |        | verified by neutering the guard (`if (false)`) and confirming the  |
// |         |            |        | new test fails, then restoring it and confirming green.            |
// | 1.10    | 2026-08-16, latest of all and later still | — | Round-4 reviewed-findings fix    |
// |         |            |        | (L-D/M-C). L-D: new Constructor_NullOccupancySeed_Throws beside    |
// |         |            |        | Constructor_EmptyOccupancySeed_Throws — the null-seed guard had no |
// |         |            |        | isolating lock (mutation-verified: deleting it left the suite      |
// |         |            |        | green). M-C: Substitution_WithAnOccupiedOnPitchIncoming_Throws-    |
// |         |            |        | InsteadOfDestroyingTheOutgoingsMapping's "guard runs before any    |
// |         |            |        | write" proof was FALSE — it inferred "nothing was written" from a  |
// |         |            |        | SECOND, fresh fold built off the same seed, which is pristine      |
// |         |            |        | regardless of guard ordering because the constructor copies the    |
// |         |            |        | seed array. Comment corrected to state what the test actually      |
// |         |            |        | proves (the malformed Incoming is refused and named) and that      |
// |         |            |        | pre-write ordering is unobservable through the public surface.     |
// |         |            |        | Ordering now locked for real through the new internal              |
// |         |            |        | CardLedgerFold.OccupancyAt(int) seam (CardLedgerFold.cs v1.13),    |
// |         |            |        | asserting the SAME faulted fold's occupancy at slots 5 and 6 is    |
// |         |            |        | unchanged. Mutation-verified: moving both ERR-044-022 guards to    |
// |         |            |        | after the write/clear pair made this test fail (slot 5 read 101,   |
// |         |            |        | not 100), 145/146 passing; reverted after observing the failure.   |
#endregion
