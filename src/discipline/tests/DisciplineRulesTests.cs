// File:     src/discipline/tests/DisciplineRulesTests.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest (carried L2, re-rated Medium, closed in full — v1.10: two new locks.
//           AddBan_NearIntMaxValue_ThrowsOverflowException_EntryUnmodified, in the shape of v1.9's
//           AddYellow_AtIntMaxValue lock, for AddBan's own (already-routed) addition, which had no
//           test of this shape. AddYellow_CrossingThresholdWithBanNearIntMaxValue_
//           ThrowsOverflowException_EntryUnmodified locks the actual scenario the finding named: a
//           threshold-crossing yellow whose SECOND addition — the accumulation-ban `+=`, one line below
//           the `+1` v1.9 fixed — was still unrouted through int.MaxValue and left the row half-applied
//           on the old code (DisciplineRules.cs v1.10 fixes it).)
// Modified: 2026-08-16, yet later (final fixer pass — L2's second half wiring: one new lock,
//           AddYellow_AtIntMaxValue_ThrowsOverflowException_EntryUnmodified, for
//           DisciplineRules.AddYellow now routing through the guarded DisciplineEntry.YellowsPlusOne
//           helper — v1.9)
// Modified: 2026-08-16 (ERR-044-014, adversarial-review H1 — every OnClubFixturePlayed call site takes
//           the club's roster ids through the new RosterOf helper, plus three new locks: the
//           roster-disagrees-with-the-packing case, the agrees-on-today's-ids case, and the null-roster
//           refusal — v1.8)
// Modified: 2026-08-15, later (reviewed findings pass, L4 — v1.7: the Competition constant's
//           DisciplineConstants.LEAGUE_COMPETITION_KEY reference renamed for that constant's ALL_CAPS ->
//           LeagueCompetitionKey rename (DisciplineConstants.cs v1.5). No behaviour change.)
// Modified: 2026-08-15 (reviewed-findings pass, L20 + M25 — v1.6: OnClubFixturePlayed_
//           AFieldedPlayerWithNoBan_IsUnaffected_NoRowInvented was vacuous (rules fresh,
//           _state.Count == 0, WasFielded never called); rewritten to give both a fielded and a
//           not-fielded player a yellows-only row so the walk actually reaches them, mutation-verified
//           (L20). Two new isolating tests, AddBan_NegativeMatches_Throws and Constructor_NullState_
//           Throws, for guards the reviewer's mutation pass found had no failing test under deletion
//           (M25).
//           Prior: 2026-08-15 (AR round 4 fix, M23 — v1.5: corrected the two "genuinely DELEGATES" claims on
//           ApplyStraightRed_WithAValidBanLength_MatchesApplyCardsKind1Behaviour and
//           ApplySecondYellow_WithAValidBanLength_MatchesApplyCardsKind2Behaviour — both were mutation-
//           falsified (an inline copy of the delegated-to method's body passes either test) and are the
//           THIRD generation of this overclaim in this file. Comments corrected to state the tests
//           establish result equivalence only; no test behaviour change, no new lock manufactured.
//           Prior: 2026-08-15 (ERR-044-003 stage 1 — every OnClubFixturePlayed call site updated for the
//           new required fieldedPlayerIds parameter, plus five new tests locking the fielded-eleven
//           exemption itself — v1.4)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.2 (thresholds & bans) / §3.3 (serving) / §3.4 (boundary &
//           hygiene); FR-DC-006/007/011/012/013/017; F2/F4; §5 T-DC-BAN-001/002/003, T-DC-HYG-001;
//           ERR-044-003 (the fielded-eleven serving exemption); Code Standards #20
// Purpose:  Unit tests for DisciplineRules — the FR-DC-006 card-kind dispatch (including the §3.2
//           worked example and the residual-kept assertion that distinguishes it from a reset), ban
//           stacking, the [GT] guard routing/atomicity/direct-reachability trio (M4/L5), club-fixture
//           serving (including the FR-DC-017 mid-season drop, the M5 two-row descending-walk lock, and
//           the ERR-044-003 fielded-eleven exemption), the season boundary sweep (with its own M5
//           lock), player-id migration, retirement drop, and multi-competition independence.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>Tests for <see cref="DisciplineRules"/>.</summary>
    [TestFixture]
    internal sealed class DisciplineRulesTests
    {
        // Player ids are club-scoped per #27 KD-3: clubId * CLUB_SQUAD_SIZE + local.
        private static int PlayerId(int clubId, int local) =>
            clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + local;

        /// <summary>
        /// A club's roster as <c>OnClubFixturePlayed</c> now takes it (ERR-044-014) — the full packed
        /// squad, which is what a real <c>Squad</c> resolved from the league yields today. Used
        /// everywhere below EXCEPT the two tests that exist precisely to separate this from the
        /// retired <c>PlayerId / CLUB_SQUAD_SIZE</c> derivation.
        /// </summary>
        private static int[] RosterOf(int clubId)
        {
            var ids = new int[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            for (int local = 0; local < ids.Length; local++)
            {
                ids[local] = PlayerId(clubId, local);
            }

            return ids;
        }

        private const int Competition = DisciplineConstants.LeagueCompetitionKey;

        private static DisciplineRules NewRules() => new DisciplineRules(new DisciplineState());

        // ── ApplyCard kind semantics (FR-DC-006) ──────────────────────────────────────

        [Test]
        public void ApplyCard_Kind0_AddsExactlyOneYellow_NoBan()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(1, entry.Yellows);
            Assert.AreEqual(0, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplyCard_Kind1_StraightRed_AddsBanOnly_NoYellow()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows, "kind 1 must carry NO yellow (FR-DC-006)");
            Assert.AreEqual(DisciplineConstants.StraightRedBanMatches, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplyCard_Kind2_SecondYellow_AddsOneYellowAndASecondYellowBan()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindSecondYellow);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(1, entry.Yellows, "kind 2 IS one yellow — never zero, never synthesized away");
            Assert.AreEqual(DisciplineConstants.SecondYellowBanMatches, entry.BanMatchesRemaining);
        }

        // ── §3.2 worked example, verbatim ──────────────────────────────────────────

        [Test]
        public void WorkedExample_FourYellows_PlusKind0_EndsAtYellows0Ban1()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            // Defaults: threshold 5, accum 1. 4 straight yellows leave Yellows=4, no ban yet.
            for (int i = 0; i < 4; i++)
            {
                rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);
            }
            Assert.AreEqual(4, rules.State.EntryFor(p, Competition).Yellows, "pre-condition: 4 yellows banked");

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);   // the 5th ⇒ crosses

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows, "5 - threshold(5) = 0 residual");
            Assert.AreEqual(DisciplineConstants.AccumBanMatches, entry.BanMatchesRemaining);
        }

        [Test]
        public void WorkedExample_FourYellows_PlusKind2_EndsAtYellows0Ban2_Stacking()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            for (int i = 0; i < 4; i++)
            {
                rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);
            }

            // The 5th yellow (from the kind-2) crosses the threshold AND the kind-2 adds its own ban —
            // both bans stack additively (FR-DC-007).
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindSecondYellow);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows);
            Assert.AreEqual(
                DisciplineConstants.AccumBanMatches + DisciplineConstants.SecondYellowBanMatches,
                entry.BanMatchesRemaining,
                "the accumulation ban and the second-yellow ban must both land — 1 + 1 = 2 at defaults");
        }

        [Test]
        public void WorkedExample_Kind1_AddsBanPlus2_YellowsUntouched()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            for (int i = 0; i < 4; i++)
            {
                rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);
            }

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(4, entry.Yellows, "a straight red does not touch the yellow tally at all");
            Assert.AreEqual(DisciplineConstants.StraightRedBanMatches, entry.BanMatchesRemaining);
        }

        // ── Residual is KEPT, not reset — the assertion that distinguishes -= from = 0 ──

        [Test]
        public void ResidualIsKept_NotReset_WhenACrossingLandsAboveTheThreshold()
        {
            // AddYellow always adds exactly ONE, so a threshold crossing reached purely by repeated
            // ApplyCard calls always lands EXACTLY on the threshold (residual 0 either way — "-= threshold"
            // and "= 0" agree there, which is why the worked-example tests above cannot distinguish them).
            // To force a residual, seed a row already ABOVE the threshold directly (DisciplineState.Upsert
            // is internal — reachable via InternalsVisibleTo) and apply one more yellow: threshold 5, seed
            // 7, +1 -> 8 crosses with 8 - 5 = 3 residual under the correct rule, vs 0 under a "= 0" bug.
            var state = new DisciplineState();
            var rules = new DisciplineRules(state);
            int p = PlayerId(0, 1);
            int threshold = DisciplineConstants.YellowAccumulationThreshold;
            state.Upsert(new DisciplineEntry(p, Competition, threshold + 2, 0));   // 7 at the default threshold 5

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);   // 7 + 1 = 8 >= 5: crosses

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(3, entry.Yellows,
                "8 - threshold(5) = 3 residual kept — 'Yellows = 0' on a crossing would read 0 here instead");
            Assert.AreEqual(DisciplineConstants.AccumBanMatches, entry.BanMatchesRemaining);
        }

        // ── L2 (final fixer pass): the accumulator overflow guard ────────────────────
        //
        // AddYellow's `entry.Yellows + 1` now routes through DisciplineEntry.YellowsPlusOne (wired at
        // DisciplineRules.cs v1.9), which refuses BEFORE the addition rather than letting it wrap
        // int.MaxValue to a negative value that DisciplineEntry's OWN constructor guard would then
        // misreport as "a negative tally is a counting bug" — true of the symptom, false of the cause.
        // A row at Yellows == int.MaxValue is not reachable through ApplyCard/AddYellow themselves (each
        // call adds exactly one, so ~2^31 calls would be needed) — it is the shape a DECODED save could
        // carry (DisciplineSaveCodec only refuses Yellows < 0, not an unreasonably large positive one),
        // so the row is seeded directly via the internal Upsert seam, as ResidualIsKept_NotReset_
        // WhenACrossingLandsAboveTheThreshold above does for the same reason.

        [Test]
        public void AddYellow_AtIntMaxValue_ThrowsOverflowException_EntryUnmodified()
        {
            var state = new DisciplineState();
            var rules = new DisciplineRules(state);
            int p = PlayerId(0, 1);
            state.Upsert(new DisciplineEntry(p, Competition, int.MaxValue, 0));

            Assert.Throws<OverflowException>(() => rules.AddYellow(p, Competition));

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(int.MaxValue, entry.Yellows,
                "the refused increment must not have written a wrapped (or any other) value — "
                + "DisciplineEntry.YellowsPlusOne refuses BEFORE the addition, not after.");
            Assert.AreEqual(0, entry.BanMatchesRemaining,
                "the row must be untouched on the refused side too, not just the Yellows field.");
        }

        // ── Carried L2, re-rated Medium: the accumulation-ban addition, one line below the increment
        // v1.9 fixed, was still unguarded until DisciplineRules.cs v1.10. AddBan's own addition was
        // already routed through DisciplineEntry.BanMatchesPlus at v1.9 (it needed no code change here),
        // but had no test in the AddYellow_AtIntMaxValue lock's shape — this closes that gap, and the
        // second test below locks the actual scenario the finding named: a threshold-crossing yellow
        // whose accumulation-ban addition, not its `+1` increment, is the one that would have overflowed.

        [Test]
        public void AddBan_NearIntMaxValue_ThrowsOverflowException_EntryUnmodified()
        {
            var state = new DisciplineState();
            var rules = new DisciplineRules(state);
            int p = PlayerId(0, 1);
            state.Upsert(new DisciplineEntry(p, Competition, 4, int.MaxValue - 1));

            Assert.Throws<OverflowException>(() => rules.AddBan(p, Competition, 3));

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(4, entry.Yellows,
                "the refused call must not have written anything, not just left BanMatchesRemaining alone.");
            Assert.AreEqual(int.MaxValue - 1, entry.BanMatchesRemaining,
                "the refused addition must not have written a wrapped (or any other) value — "
                + "DisciplineEntry.BanMatchesPlus refuses BEFORE the addition, not after.");
        }

        [Test]
        public void AddYellow_CrossingThresholdWithBanNearIntMaxValue_ThrowsOverflowException_EntryUnmodified()
        {
            // THE scenario the carried finding named verbatim: a player at BanMatchesRemaining ==
            // int.MaxValue whose 5th yellow crosses the threshold used to hit AddYellow's SECOND,
            // unrouted addition (`ban += RequireBanLength(AccumBanMatches)`) — a plain `+=` one line
            // below the guarded `+1` v1.9 fixed. Before DisciplineRules.cs v1.10 this threw
            // ArgumentOutOfRangeException from DisciplineEntry's constructor (the wrapped-negative value
            // misreported as "a counting bug") with the row left HALF-applied: Yellows already reduced
            // by the threshold (the residual subtraction, which happens before the ban addition), ban
            // untouched. Now DisciplineEntry.BanMatchesPlus refuses before that addition runs, so the
            // WHOLE call is refused and neither field moves.
            var state = new DisciplineState();
            var rules = new DisciplineRules(state);
            int p = PlayerId(0, 1);
            int threshold = DisciplineConstants.YellowAccumulationThreshold;
            state.Upsert(new DisciplineEntry(p, Competition, threshold - 1, int.MaxValue));

            Assert.Throws<OverflowException>(() => rules.AddYellow(p, Competition));

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(threshold - 1, entry.Yellows,
                "the refused crossing must not have applied the residual subtraction either — the call "
                + "is refused as a whole, not half-applied with Yellows already reduced.");
            Assert.AreEqual(int.MaxValue, entry.BanMatchesRemaining,
                "the refused addition must not have written a wrapped (or any other) value.");
        }

        // ── Bans stack additively from any source ──────────────────────────────────

        [Test]
        public void Bans_StackAdditively_AcrossMultipleSources()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);            // + StraightRedBanMatches
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);            // + StraightRedBanMatches again
            rules.AddBan(p, Competition, 3);                                               // + 3 direct

            int expected = 2 * DisciplineConstants.StraightRedBanMatches + 3;
            Assert.AreEqual(expected, rules.State.EntryFor(p, Competition).BanMatchesRemaining);
        }

        [Test]
        public void AddBan_NegativeMatches_Throws()
        {
            // M25: neutering AddBan's own `matches < 0` guard (replacing the `if` with `if (false)`)
            // does NOT fail a bare Assert.Throws<ArgumentOutOfRangeException> here — DisciplineEntry's
            // OWN constructor guard (M1) is a backstop that fires next, on the negative
            // BanMatchesRemaining AddBan would otherwise pass it, throwing the identical exception TYPE
            // from a different site. Asserting the ParamName distinguishes them: AddBan's own guard
            // names "matches" (nameof(matches)); DisciplineEntry's backstop names "banMatchesRemaining"
            // (nameof(banMatchesRemaining)) — mutation-verified, this ParamName assertion fails under
            // the neutered guard while a bare exception-type assertion does not.
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => rules.AddBan(p, Competition, -1));
            Assert.AreEqual("matches", ex.ParamName,
                "the exception must come from AddBan's OWN guard, not DisciplineEntry's constructor "
                + "backstop one level down — a bare exception-TYPE assertion cannot tell them apart.");

            Assert.IsFalse(rules.State.HasEntry(p, Competition),
                "the refused call must not have created a row.");
        }

        // ── Constructor guard (M25) ────────────────────────────────────────────────

        [Test]
        public void Constructor_NullState_Throws()
        {
            // M25: mutation-verified — neutering this guard (replacing the `if` with `if (false)`) left
            // the whole 105-test suite green before this test existed.
            Assert.Throws<ArgumentNullException>(() => new DisciplineRules(null));
        }

        // ── ApplyCard F4 ────────────────────────────────────────────────────────────

        [Test]
        public void ApplyCard_Kind3_Throws()
        {
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => rules.ApplyCard(PlayerId(0, 1), Competition, 3));
        }

        [Test]
        public void ApplyCard_Kind255_Throws()
        {
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => rules.ApplyCard(PlayerId(0, 1), Competition, 255));
        }

        // ── M4/L5: [GT] guard routing, atomicity, and direct reachability ────────────
        //
        // DisciplineConstants' [GT] fields are `public static readonly`, resolved once at type
        // initialisation, at their fixed non-negative defaults — no test in this process can bind a
        // bad config value before that first read happens (GameplayConfigHolder's lock-on-first-read
        // contract makes the ordering unenforceable across independent test fixtures). Driving these
        // guards through ApplyCard/AddYellow with the real catalogue can therefore never observe a
        // config-driven breach. RequireYellowThreshold and RequireBanLength are `internal` precisely so
        // the identical guarded code is reachable directly, with an explicit value (L5).

        [Test]
        public void RequireYellowThreshold_BelowOne_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DisciplineRules.RequireYellowThreshold(0));
        }

        [Test]
        public void RequireYellowThreshold_AtLeastOne_ReturnsItUnchanged()
        {
            Assert.AreEqual(1, DisciplineRules.RequireYellowThreshold(1));
            Assert.AreEqual(5, DisciplineRules.RequireYellowThreshold(5));
        }

        [Test]
        public void RequireBanLength_Negative_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DisciplineRules.RequireBanLength(-1, "TestBanLength"));
        }

        [Test]
        public void RequireBanLength_NonNegative_ReturnsItUnchanged()
        {
            Assert.AreEqual(0, DisciplineRules.RequireBanLength(0, "TestBanLength"));
            Assert.AreEqual(3, DisciplineRules.RequireBanLength(3, "TestBanLength"));
        }

        [Test]
        public void ApplyCard_Kind1_SuccessfulCard_BanLands()
        {
            // M19(a): renamed from ApplyCard_Kind1_RoutesStraightRedBanMatchesThroughRequireBanLength,
            // whose own comment claimed "this one proves ApplyCard actually calls [RequireBanLength]" —
            // false, since at today's non-negative default RequireBanLength never refuses, so this
            // assertion is identical whether or not ApplyCard's kind-1 branch calls the guard at all
            // (a reviewer-executed mutant deleting that call survived). See
            // ApplyStraightRed_WithAnInvalidBanLength_Throws below for the test that actually
            // discriminates it, the ApplySecondYellow/L11 shape applied to kind-1.
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);

            Assert.AreEqual(DisciplineConstants.StraightRedBanMatches,
                rules.State.EntryFor(p, Competition).BanMatchesRemaining);
        }

        [Test]
        public void ApplyStraightRed_WithAnInvalidBanLength_Throws()
        {
            // M19(a): DisciplineConstants.StraightRedBanMatches is a public static readonly, resolved
            // once at its non-negative default — no test in this process can drive ApplyCard's real
            // kind-1 dispatch through a bad value (the same L5/L11 constraint the kind-2 tests document
            // above). ApplyStraightRed takes the ban length as a parameter for exactly this reason.
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            Assert.Throws<InvalidOperationException>(
                () => rules.ApplyStraightRed(p, Competition, straightRedBanMatches: -1));

            Assert.AreEqual(0, rules.State.EntryFor(p, Competition).BanMatchesRemaining,
                "the refused ban length must not have been added.");
        }

        [Test]
        public void ApplyStraightRed_WithAValidBanLength_MatchesApplyCardsKind1Behaviour()
        {
            // M23 (round 4 AR): this asserts RESULT EQUIVALENCE only — that ApplyCard's kind-1 case
            // produces the same DisciplineEntry as calling ApplyStraightRed directly, for the same
            // inputs. It does NOT, and cannot, prove ApplyCard genuinely DELEGATES rather than carrying
            // an inline copy of ApplyStraightRed's three lines: mutation-verified by replacing
            // ApplyCard's kind-1 branch with a pasted-in copy of ApplyStraightRed's body and observing
            // the whole suite, this test included, still pass — an exact copy produces identical state
            // by definition. Two independently constructed DisciplineRules instances driven through two
            // different call paths can only ever compare OUTPUTS; nothing here (or reachable without
            // adding test-only instrumentation ApplyCard does not carry, e.g. a call counter) can
            // distinguish "ApplyCard calls ApplyStraightRed" from "ApplyCard reimplements it verbatim".
            // The prior comment claimed the stronger thing three times running (M23 is the third
            // generation of this overclaim in this file) — recorded honestly here rather than
            // manufacturing a lock that cannot exist.
            DisciplineRules viaApplyCard = NewRules();
            DisciplineRules viaDirectCall = NewRules();
            int p = PlayerId(0, 1);

            viaApplyCard.ApplyCard(p, Competition, DisciplineConstants.CardKindRed);
            viaDirectCall.ApplyStraightRed(p, Competition, DisciplineConstants.StraightRedBanMatches);

            DisciplineEntry expected = viaApplyCard.State.EntryFor(p, Competition);
            DisciplineEntry actual = viaDirectCall.State.EntryFor(p, Competition);
            Assert.AreEqual(expected.Yellows, actual.Yellows);
            Assert.AreEqual(expected.BanMatchesRemaining, actual.BanMatchesRemaining);
        }

        [Test]
        public void ApplyCard_Kind2_SuccessfulCard_YellowAndBanBothLand()
        {
            // The successful-path assertion the old (misnamed) version of this test actually verified:
            // at today's non-negative default the [GT] guard never fires, so this cannot discriminate
            // ORDER — see ApplySecondYellow_WithAnInvalidBanLength_RefusesTheWholeCardAtomically below
            // for that.
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindSecondYellow);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(1, entry.Yellows, "the yellow from a successful kind-2 must land");
            Assert.AreEqual(DisciplineConstants.SecondYellowBanMatches, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplySecondYellow_WithAnInvalidBanLength_RefusesTheWholeCardAtomically()
        {
            // L11: replaces ApplyCard_Kind2_ValidatesSecondYellowBanMatches_BeforeAddYellowRuns, which
            // could not discriminate order — DisciplineConstants.SecondYellowBanMatches is a public
            // static readonly, resolved once at its non-negative default, so no test in this process
            // can drive ApplyCard's real dispatch through a bad value (GameplayConfigHolder's
            // lock-on-first-read contract, same as the M4/L5 guard tests above). ApplySecondYellow
            // takes the ban length as a parameter for exactly this reason, so this test supplies an
            // explicit -1 and can genuinely tell "validated before AddYellow" from "validated after":
            // under the pre-M4 order this would leave Yellows == 1 despite the throw.
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            Assert.Throws<InvalidOperationException>(
                () => rules.ApplySecondYellow(p, Competition, secondYellowBanMatches: -1));

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows,
                "AddYellow's effect must not be committed when the ban length is refused — the "
                + "validation runs BEFORE AddYellow, not after (M4).");
            Assert.AreEqual(0, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplySecondYellow_WithAValidBanLength_MatchesApplyCardsKind2Behaviour()
        {
            // M23 (round 4 AR): result equivalence only, same limitation as its kind-1 twin
            // (ApplyStraightRed_WithAValidBanLength_MatchesApplyCardsKind1Behaviour above) — this cannot
            // prove ApplyCard genuinely DELEGATES to ApplySecondYellow rather than carrying an inline
            // copy of its three lines. Mutation-verified: pasting ApplySecondYellow's body into
            // ApplyCard's kind-2 branch in place of the call leaves this test (and the whole suite)
            // green, because two exact copies produce identical DisciplineEntry state by definition.
            // The prior comment's "genuinely DELEGATES ... parallel-surface defect class" claim is the
            // THIRD generation of this same overclaim in this file (round 3's L11 fix already renamed
            // one predecessor test for making an equivalent false claim) — corrected here rather than
            // building a lock this pair of tests structurally cannot support.
            DisciplineRules viaApplyCard = NewRules();
            DisciplineRules viaDirectCall = NewRules();
            int p = PlayerId(0, 1);

            viaApplyCard.ApplyCard(p, Competition, DisciplineConstants.CardKindSecondYellow);
            viaDirectCall.ApplySecondYellow(p, Competition, DisciplineConstants.SecondYellowBanMatches);

            DisciplineEntry expected = viaApplyCard.State.EntryFor(p, Competition);
            DisciplineEntry actual = viaDirectCall.State.EntryFor(p, Competition);
            Assert.AreEqual(expected.Yellows, actual.Yellows);
            Assert.AreEqual(expected.BanMatchesRemaining, actual.BanMatchesRemaining);
        }

        // ── OnClubFixturePlayed ─────────────────────────────────────────────────────

        [Test]
        public void OnClubFixturePlayed_DecrementsOnlyThatClubsPlayers()
        {
            DisciplineRules rules = NewRules();
            int clubAPlayer = PlayerId(0, 1);
            int clubBPlayer = PlayerId(1, 1);
            rules.AddBan(clubAPlayer, Competition, 2);
            rules.AddBan(clubBPlayer, Competition, 2);

            rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>());

            Assert.AreEqual(1, rules.State.EntryFor(clubAPlayer, Competition).BanMatchesRemaining,
                "club 0's fixture serves club 0's ban");
            Assert.AreEqual(2, rules.State.EntryFor(clubBPlayer, Competition).BanMatchesRemaining,
                "a DIFFERENT club's player must be untouched by this call");
        }

        [Test]
        public void OnClubFixturePlayed_NeverGoesBelowZero()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.AddBan(p, Competition, 1);

            rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>());   // 1 -> 0, drops the row
            // A second played fixture with no outstanding ban must not underflow — the row is gone,
            // so this call has nothing to touch, and must not throw or resurrect a negative row.
            Assert.DoesNotThrow(() => rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>()));

            Assert.IsFalse(rules.State.HasEntry(p, Competition));
        }

        [Test]
        public void OnClubFixturePlayed_DropsARowThatReachesZeroZero_MidSeason()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.AddBan(p, Competition, 1);   // Yellows 0, ban 1 — serving this to 0 makes an all-zero row

            rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>());

            Assert.IsFalse(rules.State.HasEntry(p, Competition),
                "FR-DC-017: a row that reaches (0,0) MID-SEASON must be dropped immediately");
        }

        [Test]
        public void OnClubFixturePlayed_NegativeClubId_Throws()
        {
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => rules.OnClubFixturePlayed(-1, RosterOf(0), Array.Empty<int>()));
        }

        [Test]
        public void OnClubFixturePlayed_TwoSameClubRowsBothReachZeroZero_InOneCall_BothDropped()
        {
            // M5: the descending walk's correctness has no test that fails when reverted UNLESS at
            // least two rows of the SAME club both empty out in one call — every other test here has
            // exactly one droppable row per call. Under an ascending walk, removing the first row
            // (Upsert drops an empty entry) shifts the second row down into the just-vacated index,
            // which the ascending loop's incremented cursor then steps straight over — verified by
            // reverting the loop to ascending and watching this go red (see the fix report).
            DisciplineRules rules = NewRules();
            int p1 = PlayerId(0, 1);
            int p2 = PlayerId(0, 2);
            rules.AddBan(p1, Competition, 1);
            rules.AddBan(p2, Competition, 1);

            rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>());

            Assert.IsFalse(rules.State.HasEntry(p1, Competition), "p1's one-match ban must be served and the row dropped");
            Assert.IsFalse(rules.State.HasEntry(p2, Competition), "p2's one-match ban must ALSO be served in the same call");
        }

        // ── ERR-044-003 stage 1: the fielded-eleven exemption ─────────────────────────

        [Test]
        public void OnClubFixturePlayed_ExemptsAFieldedBannedPlayer_ButStillServesAnUnfieldedTeammate()
        {
            // ERR-044-003 stage 1: a ban is served by the club playing WITHOUT the banned player.
            // #30 §2.3 F9's extremis back-fill can field one anyway (AvailabilityComposition.
            // Reinstate) — his ban must NOT serve that fixture. Asserted in the SAME call as an
            // unfielded team-mate who DOES serve, so this cannot pass by the method skipping
            // everybody (or by only ever exercising the trivial one-player case).
            DisciplineRules rules = NewRules();
            int fielded = PlayerId(0, 1);
            int notFielded = PlayerId(0, 2);
            rules.AddBan(fielded, Competition, 2);
            rules.AddBan(notFielded, Competition, 2);

            rules.OnClubFixturePlayed(0, RosterOf(0), new[] { fielded });

            Assert.AreEqual(2, rules.State.EntryFor(fielded, Competition).BanMatchesRemaining,
                "a banned player who WAS fielded (the extremis back-fill) must not have his ban "
                + "served — he played, so this fixture is not one of his ban's matches.");
            Assert.AreEqual(1, rules.State.EntryFor(notFielded, Competition).BanMatchesRemaining,
                "a banned team-mate who was NOT fielded must still serve one match — otherwise this "
                + "test would pass even if OnClubFixturePlayed exempted everybody unconditionally.");
        }

        [Test]
        public void OnClubFixturePlayed_AFieldedPlayerWithNoBan_IsUnaffected_NoRowInvented()
        {
            // L20: the original version left `rules` fresh (_state.Count == 0), so the descending walk
            // never executed its body at all and WasFielded was never called — it passed regardless of
            // what WasFielded returned. Fixed by giving BOTH players a YELLOWS-ONLY row
            // (BanMatchesRemaining == 0) so the walk actually reaches them: each must be skipped on the
            // `entry.BanMatchesRemaining <= 0` guard, and both rows must survive completely untouched.
            // Mutation-verified two ways. (1) Deleting the `BanMatchesRemaining <= 0` guard outright —
            // leaving only the club and WasFielded checks — makes this test FAIL: notFieldedNoBan is not
            // in fieldedPlayerIds, so with the guard gone the walk falls through to the decrement and
            // DisciplineEntry's own constructor throws ArgumentOutOfRangeException on BanMatchesRemaining
            // == -1 (confirmed by running the mutant). (2) A pure REORDER — moving the same guard to run
            // textually after the WasFielded check, with both checks still present — does NOT fail this
            // test, because the two conditions are independent `continue`s and either one skips the row
            // regardless of which is evaluated first; confirmed by running that mutant too. The
            // notFieldedNoBan row is what makes (1) observable: the fielded row alone is also skipped by
            // WasFielded on its own, so it cannot distinguish "guarded" from "guard deleted".
            DisciplineRules rules = NewRules();
            int fielded = PlayerId(0, 1);
            int notFieldedNoBan = PlayerId(0, 2);
            rules.ApplyCard(fielded, Competition, DisciplineConstants.CardKindYellow);           // Yellows 1, ban 0
            rules.ApplyCard(notFieldedNoBan, Competition, DisciplineConstants.CardKindYellow);   // Yellows 1, ban 0

            rules.OnClubFixturePlayed(0, RosterOf(0), new[] { fielded });

            DisciplineEntry fieldedEntry = rules.State.EntryFor(fielded, Competition);
            Assert.AreEqual(1, fieldedEntry.Yellows, "a fielded player's yellows-only row must survive untouched");
            Assert.AreEqual(0, fieldedEntry.BanMatchesRemaining,
                "WasFielded is consulted only for rows that already carry an outstanding ban "
                + "(BanMatchesRemaining > 0) — this row has none, so the walk must skip it on that "
                + "guard before ever reaching WasFielded, never as a reason to create or touch a ban.");
            DisciplineEntry notFieldedEntry = rules.State.EntryFor(notFieldedNoBan, Competition);
            Assert.AreEqual(1, notFieldedEntry.Yellows,
                "the NOT-fielded teammate's yellows-only row must ALSO survive untouched — he has no "
                + "outstanding ban either, so the same guard must skip him regardless of WasFielded.");
            Assert.AreEqual(0, notFieldedEntry.BanMatchesRemaining);
        }

        [Test]
        public void OnClubFixturePlayed_NullFieldedPlayerIds_Throws()
        {
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentNullException>(() => rules.OnClubFixturePlayed(0, RosterOf(0), null));
        }

        // ── ERR-044-014: membership is the ROSTER, not the id packing ────────────────

        [Test]
        public void OnClubFixturePlayed_ServesByRosterMembership_NotByThePackedIdDerivation()
        {
            // THE lock for ERR-044-014. Both players' ids are deliberately packed into the WRONG
            // club for the roster they are on, so the retired derivation and the landed presence
            // match give opposite answers for both of them at once:
            //   - servedByRoster's id derives to club 1, but he is on club 0's roster  -> MUST serve;
            //   - ignoredByRoster's id derives to club 0, but he is NOT on the roster  -> MUST NOT.
            // Under `entry.PlayerId / CLUB_SQUAD_SIZE == clubId` this test fails twice over. That is
            // not a hypothetical id layout: it is exactly what a #31 transfer produces (a player at a
            // new club carrying the id his old one packed) and what ERR-044-003 stage 3's id-space
            // widening produces by construction, and nothing anywhere validated the packing —
            // MigratePlayerId, the rule that was supposed to keep ids current, has no production
            // caller at all.
            DisciplineRules rules = NewRules();
            int servedByRoster = PlayerId(1, 3);
            int ignoredByRoster = PlayerId(0, 3);
            rules.AddBan(servedByRoster, Competition, 2);
            rules.AddBan(ignoredByRoster, Competition, 2);

            rules.OnClubFixturePlayed(0, new[] { servedByRoster }, Array.Empty<int>());

            Assert.AreEqual(1, rules.State.EntryFor(servedByRoster, Competition).BanMatchesRemaining,
                "a player ON the club's roster must serve, whatever his id divides to — otherwise a "
                + "transferred player's ban never decrements again and he is suspended forever, "
                + "silently (ERR-044-014).");
            Assert.AreEqual(2, rules.State.EntryFor(ignoredByRoster, Competition).BanMatchesRemaining,
                "a player NOT on the club's roster must be untouched, whatever his id divides to — "
                + "membership has exactly one source, the roster the removal half walks.");
        }

        [Test]
        public void OnClubFixturePlayed_OverAPackedRoster_ServesExactlyWhatTheRetiredDerivationServed()
        {
            // The behaviour-preservation half: on today's id scheme, over a full packed roster, the
            // roster match and the retired `PlayerId / CLUB_SQUAD_SIZE == clubId` derivation select
            // the same set. Probed at both ends of club 0's range (local 0 and the last local) plus
            // the neighbouring club's first id, which is the id one off the end of the range and the
            // one an off-by-one in either rule would move.
            DisciplineRules rules = NewRules();
            int first = PlayerId(0, 0);
            int last = PlayerId(0, PlayerDatabaseConstants.CLUB_SQUAD_SIZE - 1);
            int nextClubsFirst = PlayerId(1, 0);
            rules.AddBan(first, Competition, 2);
            rules.AddBan(last, Competition, 2);
            rules.AddBan(nextClubsFirst, Competition, 2);

            rules.OnClubFixturePlayed(0, RosterOf(0), Array.Empty<int>());

            Assert.AreEqual(1, rules.State.EntryFor(first, Competition).BanMatchesRemaining,
                "local 0 is club 0's under both rules");
            Assert.AreEqual(1, rules.State.EntryFor(last, Competition).BanMatchesRemaining,
                "the last local is still club 0's under both rules");
            Assert.AreEqual(2, rules.State.EntryFor(nextClubsFirst, Competition).BanMatchesRemaining,
                "the next club's first id is outside both the range and the roster");
        }

        [Test]
        public void OnClubFixturePlayed_NullClubPlayerIds_Throws()
        {
            // The same caller-contract posture as the null fielded eleven (F2, ERR-044-007): a caller
            // who cannot name the roster cannot name whose ban this fixture served, and BOTH silent
            // readings of the unknown case are wrong — "serve everybody" is the defect ERR-044-014
            // closes, "serve nobody" is a permanently suspended squad.
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentNullException>(
                () => rules.OnClubFixturePlayed(0, null, Array.Empty<int>()));
        }

        [Test]
        public void OnClubFixturePlayed_ExemptionDoesNotBlockTheFRDC017DropForANonFieldedPlayer()
        {
            // The exemption only skips the decrement for a player IN fieldedPlayerIds — it must not
            // interfere with FR-DC-017's immediate mid-season drop for anyone else served in the same
            // call. fieldedNoBan is present in the array precisely so the exemption branch actually
            // runs during this call, not merely so the array is non-empty.
            DisciplineRules rules = NewRules();
            int fieldedNoBan = PlayerId(0, 1);
            int notFieldedOneMatch = PlayerId(0, 2);
            rules.AddBan(notFieldedOneMatch, Competition, 1);

            rules.OnClubFixturePlayed(0, RosterOf(0), new[] { fieldedNoBan });

            Assert.IsFalse(rules.State.HasEntry(notFieldedOneMatch, Competition),
                "FR-DC-017: a row that reaches (0, 0) mid-season must still be dropped immediately — "
                + "the exemption running for a DIFFERENT player in the same call must not suppress it.");
        }

        // ── RollToNextSeason ────────────────────────────────────────────────────────

        [Test]
        public void RollToNextSeason_UnservedBanCarries_YellowsResetButRowSurvives()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);
            rules.AddBan(p, Competition, 3);   // an unserved ban, e.g. a red in the last round of May

            rules.RollToNextSeason();

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows, "yellows always reset at the boundary");
            Assert.AreEqual(3, entry.BanMatchesRemaining, "an unserved ban MUST carry into the new season");
        }

        [Test]
        public void RollToNextSeason_ARowThatBecomesZeroZero_IsDropped()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);   // Yellows 1, ban 0

            rules.RollToNextSeason();

            Assert.IsFalse(rules.State.HasEntry(p, Competition));
        }

        [Test]
        public void RollToNextSeason_TwoRowsBothBecomeZeroZero_InOneCall_BothDropped()
        {
            // M5: same shape as the OnClubFixturePlayed case above, for RollToNextSeason's own
            // descending walk — two droppable rows in one call, which reverting the loop to ascending
            // fails (verified; see the fix report).
            DisciplineRules rules = NewRules();
            int p1 = PlayerId(0, 1);
            int p2 = PlayerId(0, 2);
            rules.ApplyCard(p1, Competition, DisciplineConstants.CardKindYellow);   // Yellows 1, ban 0
            rules.ApplyCard(p2, Competition, DisciplineConstants.CardKindYellow);   // Yellows 1, ban 0

            rules.RollToNextSeason();

            Assert.IsFalse(rules.State.HasEntry(p1, Competition), "p1's yellow resets to 0, row becomes (0,0) and drops");
            Assert.IsFalse(rules.State.HasEntry(p2, Competition), "p2's row must ALSO drop in the same call");
        }

        // ── MigratePlayerId ─────────────────────────────────────────────────────────

        [Test]
        public void MigratePlayerId_MovesTallyAndUnservedBanVerbatim()
        {
            DisciplineRules rules = NewRules();
            int oldId = PlayerId(0, 1);
            int newId = PlayerId(1, 5);
            rules.ApplyCard(oldId, Competition, DisciplineConstants.CardKindYellow);
            rules.AddBan(oldId, Competition, 2);

            rules.MigratePlayerId(oldId, newId);

            Assert.IsFalse(rules.State.HasEntry(oldId, Competition), "the source row must be gone");
            DisciplineEntry moved = rules.State.EntryFor(newId, Competition);
            Assert.AreEqual(1, moved.Yellows);
            Assert.AreEqual(2, moved.BanMatchesRemaining);
        }

        [Test]
        public void MigratePlayerId_ToALowerId_MovesEVERYCompetitionsRows()
        {
            // The index-shift case, found by self-review before the AR pass. Re-keying DOWNWARD
            // inserts the new row ahead of a descending cursor and shifts the rows between them up by
            // one, so a walk-and-rewrite loop steps over one of them. With a single competition that
            // is invisible; with two it strands a whole competition's bans on an id nobody will ever
            // look up again. Two competitions and a LOWER target id are exactly what it takes to see
            // it — the pre-fix implementation leaves competition 0's row behind on the old id.
            DisciplineRules rules = NewRules();
            int oldId = PlayerId(1, 5);
            int newId = PlayerId(0, 1);   // strictly lower, so the insertion lands ahead of the cursor
            Assert.Less(newId, oldId, "Precondition: the target id must be lower than the source.");

            rules.AddBan(oldId, 0, 3);
            rules.AddBan(oldId, 1, 4);

            rules.MigratePlayerId(oldId, newId);

            Assert.IsFalse(rules.State.HasEntry(oldId, 0),
                "competition 0's row was left behind on the old id — the walk stepped over it after "
                + "the lower-id insertion shifted the list.");
            Assert.IsFalse(rules.State.HasEntry(oldId, 1), "competition 1's row was left behind.");
            Assert.AreEqual(3, rules.State.EntryFor(newId, 0).BanMatchesRemaining);
            Assert.AreEqual(4, rules.State.EntryFor(newId, 1).BanMatchesRemaining);
        }

        [Test]
        public void MigratePlayerId_WithOneConflictingCompetition_WritesNOTHING()
        {
            // F2's refusal must be atomic. A player carrying rows in two competitions, one of which
            // collides at the target, would otherwise have the clean one already re-keyed when the
            // throw lands — a half-migrated player, which is worse than either outcome the caller was
            // choosing between.
            DisciplineRules rules = NewRules();
            int oldId = PlayerId(0, 1);
            int newId = PlayerId(1, 5);

            rules.AddBan(oldId, 0, 2);
            rules.AddBan(oldId, 1, 3);

            // The collision sits in competition 0 — the LOWEST key, and therefore the last one a
            // descending walk reaches. Put it in the highest key instead and a non-atomic
            // implementation still passes, because it throws before it has written anything: the
            // assertion would be satisfied by luck of iteration order rather than by the property.
            rules.AddBan(newId, 0, 1);

            Assert.Throws<ArgumentException>(() => rules.MigratePlayerId(oldId, newId));

            Assert.AreEqual(2, rules.State.EntryFor(oldId, 0).BanMatchesRemaining,
                "the refusal must leave the tally exactly as it found it.");
            Assert.AreEqual(3, rules.State.EntryFor(oldId, 1).BanMatchesRemaining,
                "competition 1 was migrated before the competition-0 conflict was discovered — a "
                + "half-migrated player, which is worse than either outcome the caller was choosing "
                + "between.");
            Assert.AreEqual(1, rules.State.EntryFor(newId, 0).BanMatchesRemaining);
            Assert.IsFalse(rules.State.HasEntry(newId, 1), "nothing may be written onto the target.");
        }

        [Test]
        public void MigratePlayerId_NegativeTargetId_ThrowsAndWritesNothing()
        {
            // M15: round-1's M1 taught DisciplineEntry's constructor to refuse a negative PlayerId, but
            // the write loop below didn't gain a matching pre-check — it removed the source's first row,
            // THEN let the constructor throw on the negative target, deleting that row and aborting
            // with the player's other competitions untouched. Two competitions makes the loss visible:
            // a single-competition player would just look like an ordinary refusal.
            DisciplineRules rules = NewRules();
            int oldId = PlayerId(0, 1);
            rules.AddBan(oldId, 0, 2);
            rules.AddBan(oldId, 1, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => rules.MigratePlayerId(oldId, -1));

            Assert.AreEqual(2, rules.State.Count,
                "the refusal must leave BOTH of the source player's rows intact — a negative target id "
                + "must be refused before the first row is ever removed.");
            Assert.AreEqual(2, rules.State.EntryFor(oldId, 0).BanMatchesRemaining);
            Assert.AreEqual(3, rules.State.EntryFor(oldId, 1).BanMatchesRemaining);
        }

        [Test]
        public void MigratePlayerId_NoRows_IsANoOp()
        {
            DisciplineRules rules = NewRules();
            int clean = PlayerId(0, 9);
            int target = PlayerId(1, 9);

            Assert.DoesNotThrow(() => rules.MigratePlayerId(clean, target));
            Assert.IsFalse(rules.State.HasEntry(target, Competition), "nothing to migrate, nothing created");
        }

        [Test]
        public void MigratePlayerId_ConflictingTargetRow_Throws()
        {
            DisciplineRules rules = NewRules();
            int oldId = PlayerId(0, 1);
            int newId = PlayerId(1, 1);
            rules.ApplyCard(oldId, Competition, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(newId, Competition, DisciplineConstants.CardKindYellow);   // target already carries a row here

            Assert.Throws<ArgumentException>(() => rules.MigratePlayerId(oldId, newId));
        }

        [Test]
        public void MigratePlayerId_ToTheSameId_IsANoOp()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.ApplyCard(p, Competition, DisciplineConstants.CardKindYellow);

            Assert.DoesNotThrow(() => rules.MigratePlayerId(p, p));
            Assert.AreEqual(1, rules.State.EntryFor(p, Competition).Yellows, "the row must be untouched");
        }

        // ── DropPlayer ──────────────────────────────────────────────────────────────

        [Test]
        public void DropPlayer_RemovesEveryRowForThatPlayer_AcrossCompetitions_LeavesOthersAlone()
        {
            DisciplineRules rules = NewRules();
            int target = PlayerId(0, 1);
            int other = PlayerId(0, 2);
            rules.ApplyCard(target, 0, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(target, 1, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(other, 0, DisciplineConstants.CardKindYellow);

            rules.DropPlayer(target);

            Assert.IsFalse(rules.State.HasEntry(target, 0));
            Assert.IsFalse(rules.State.HasEntry(target, 1));
            Assert.IsTrue(rules.State.HasEntry(other, 0), "an unrelated player must be untouched");
        }

        // ── Multi-competition independence ─────────────────────────────────────────

        [Test]
        public void SamePlayer_DifferentCompetitions_TalliesAreIndependent_BothSurvive()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, 0, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(p, 0, DisciplineConstants.CardKindYellow);
            rules.ApplyCard(p, 1, DisciplineConstants.CardKindYellow);

            Assert.AreEqual(2, rules.State.EntryFor(p, 0).Yellows);
            Assert.AreEqual(1, rules.State.EntryFor(p, 1).Yellows);
            Assert.AreEqual(2, rules.State.Count, "both rows must exist independently");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial suite (#44 T-phase tests): FR-DC-006 card-kind dispatch, |
// |         |            |        | the §3.2 worked example verbatim, the residual-vs-reset          |
// |         |            |        | distinguishing assertion, additive ban stacking, F4, club-scoped |
// |         |            |        | serving with the FR-DC-017 mid-season drop, the season boundary  |
// |         |            |        | sweep, player-id migration (F2), retirement drop, and            |
// |         |            |        | multi-competition independence.                                  |
// | 1.1     | 2026-08-13 | —      | AR fixes. M4: added ApplyCard kind-1/kind-2 routing + atomicity  |
// |         |            |        | tests. L5: added direct RequireYellowThreshold/RequireBanLength  |
// |         |            |        | guard tests (now internal — reachable without depending on       |
// |         |            |        | GameplayConfigHolder binding before DisciplineConstants' static  |
// |         |            |        | readonly fields resolve). M5: added the two-same-row-in-one-call |
// |         |            |        | locks for OnClubFixturePlayed and RollToNextSeason's descending  |
// |         |            |        | walks — verified red under a reverted ascending walk, then       |
// |         |            |        | restored.                                                        |
// | 1.2     | 2026-08-13 | —      | AR round 3 fix (L11): ApplyCard_Kind2_ValidatesSecondYellowBan-  |
// |         |            |        | Matches_BeforeAddYellowRuns claimed to prove the M4 ORDER but    |
// |         |            |        | could not — at the non-negative default the guard never fires,  |
// |         |            |        | so both assertions held identically either order. Renamed to    |
// |         |            |        | ApplyCard_Kind2_SuccessfulCard_YellowAndBanBothLand (what it     |
// |         |            |        | actually asserts) and paired with two new tests driven through  |
// |         |            |        | the new DisciplineRules.ApplySecondYellow seam (L11, this file's|
// |         |            |        | own change): one supplies an explicit invalid ban length and     |
// |         |            |        | asserts AddYellow's effect is absent (genuine order              |
// |         |            |        | discrimination), one proves ApplyCard's kind-2 case actually     |
// |         |            |        | delegates rather than duplicating the logic.                    |
// | 1.3     | 2026-08-13 | —      | AR round 5 fixes. M15: MigratePlayerId_NegativeTargetId_Throws-  |
// |         |            |        | AndWritesNothing locks the new newPlayerId >= 0 guard — the      |
// |         |            |        | write loop used to remove the source's first row and THEN let    |
// |         |            |        | DisciplineEntry's constructor throw on a negative target,        |
// |         |            |        | deleting that row and aborting with the rest of the player's     |
// |         |            |        | history stranded. M19(a): ApplyCard_Kind1_RoutesStraightRedBan-  |
// |         |            |        | MatchesThroughRequireBanLength renamed to ApplyCard_Kind1_       |
// |         |            |        | SuccessfulCard_BanLands (its comment claimed to prove ApplyCard  |
// |         |            |        | calls RequireBanLength, but at the non-negative default it       |
// |         |            |        | cannot — a reviewer-executed mutant deleting that call survived  |
// |         |            |        | 96/96); paired with two new tests through the new                |
// |         |            |        | DisciplineRules.ApplyStraightRed seam (the kind-1 twin of L11's  |
// |         |            |        | ApplySecondYellow), mirroring that pair exactly. L17: every      |
// |         |            |        | CARD_KIND_* reference renamed to DisciplineConstants.CardKind*   |
// |         |            |        | (the constants are now [CROSS], PascalCase per src/CLAUDE.md     |
// |         |            |        | §3.2.3).                                                          |
// | 1.4     | 2026-08-15 | —      | ERR-044-003 stage 1: DisciplineRules.OnClubFixturePlayed gained  |
// |         |            |        | a required int[] fieldedPlayerIds parameter (the fielded eleven  |
// |         |            |        | is exempt from that fixture's ban-serving decrement). Every      |
// |         |            |        | existing call site updated to pass Array.Empty<int>() (unchanged |
// |         |            |        | intent: nobody was fielded, so everybody serves). New:            |
// |         |            |        | OnClubFixturePlayed_ExemptsAFieldedBannedPlayer_ButStillServes-  |
// |         |            |        | AnUnfieldedTeammate (both halves in ONE call, so the test cannot  |
// |         |            |        | pass by the method doing nothing); ...AFieldedPlayerWithNoBan_   |
// |         |            |        | IsUnaffected_NoRowInvented; ...NullFieldedPlayerIds_Throws;       |
// |         |            |        | ...ExemptionDoesNotBlockTheFRDC017DropForANonFieldedPlayer.       |
// | 1.5     | 2026-08-15 | —      | AR round 4 fix (M23). Both "proves it delegates" comments were    |
// |         |            |        | mutation-falsified: replacing ApplyCard's kind-1/kind-2 branches   |
// |         |            |        | with an inline copy of ApplyStraightRed's/ApplySecondYellow's     |
// |         |            |        | body passes the whole suite, because an exact copy produces        |
// |         |            |        | identical state. Third generation of the same overclaim (v1.2's   |
// |         |            |        | L11 fix and v1.3's M19(a) fix each corrected a predecessor test    |
// |         |            |        | making the identical false claim). Corrected the comments to      |
// |         |            |        | state what the tests actually establish — result equivalence      |
// |         |            |        | between ApplyCard's dispatch and the directly-called internal     |
// |         |            |        | method, not a call-graph lock. No test behaviour changed, and no  |
// |         |            |        | new lock was manufactured to replace the false claim — none is    |
// |         |            |        | reachable without test-only instrumentation ApplyCard does not    |
// |         |            |        | carry.                                                             |
// | 1.6     | 2026-08-15 | —      | Reviewed-findings fixes. L20: OnClubFixturePlayed_                |
// |         |            |        | AFieldedPlayerWithNoBan_IsUnaffected_NoRowInvented was vacuous —   |
// |         |            |        | rules fresh, _state.Count == 0, the walk's body never executed and|
// |         |            |        | WasFielded was never called. Rewritten to give both a fielded and |
// |         |            |        | a not-fielded player a yellows-only row (BanMatchesRemaining == 0)|
// |         |            |        | so the walk reaches both; mutation-verified two ways (deleting the|
// |         |            |        | BanMatchesRemaining <= 0 guard fails the test via                 |
// |         |            |        | ArgumentOutOfRangeException; a pure reorder of the same guard past|
// |         |            |        | the WasFielded check does not, since both are independent         |
// |         |            |        | continues). M25: two new isolating tests, AddBan_NegativeMatches_ |
// |         |            |        | Throws and Constructor_NullState_Throws, for guards a reviewer    |
// |         |            |        | mutation pass found had no test that failed under `if (false)`.   |
// |         |            |        | AddBan_NegativeMatches_Throws needed a ParamName assertion, not   |
// |         |            |        | just the exception type — DisciplineEntry's own constructor guard |
// |         |            |        | (M1) is a backstop that throws the identical ArgumentOutOfRange-  |
// |         |            |        | Exception from a different site once AddBan's guard is neutered,  |
// |         |            |        | so a bare Assert.Throws<...> does not discriminate them.          |
// | 1.7     | 2026-08-15, later | — | Reviewed findings pass, L4. Competition constant's              |
// |         |            |        | DisciplineConstants reference renamed LEAGUE_COMPETITION_KEY ->  |
// |         |            |        | LeagueCompetitionKey. No behaviour change.                        |
// | 1.8     | 2026-08-16 | —      | ERR-044-014 (adversarial review, H1). New RosterOf(clubId)       |
// |         |            |        | helper supplies the full packed roster to every existing         |
// |         |            |        | OnClubFixturePlayed call site, so each keeps the behaviour it     |
// |         |            |        | asserted. Three new tests: ...ServesByRosterMembership_NotByThe- |
// |         |            |        | PackedIdDerivation, which puts BOTH its players' ids in the      |
// |         |            |        | wrong club for the roster they are on and so fails twice under   |
// |         |            |        | the retired derivation; ...OverAPackedRoster_ServesExactlyWhat-  |
// |         |            |        | TheRetiredDerivationServed, the behaviour-preservation lock on   |
// |         |            |        | today's ids probed at both ends of a club's range and one past   |
// |         |            |        | it; and ...NullClubPlayerIds_Throws for the new F2-class guard.  |
// | 1.9     | 2026-08-16, yet later | — | Final fixer pass — L2's second half wiring. New:         |
// |         |            |        | AddYellow_AtIntMaxValue_ThrowsOverflowException_EntryUnmodified — |
// |         |            |        | seeds a row at Yellows == int.MaxValue via the internal Upsert    |
// |         |            |        | seam (the shape a decoded save could carry; not reachable through |
// |         |            |        | ApplyCard/AddYellow's own +1-per-call arithmetic) and asserts the |
// |         |            |        | next AddYellow throws OverflowException with the row completely   |
// |         |            |        | untouched — DisciplineRules.cs v1.9 now routes AddYellow's        |
// |         |            |        | `entry.Yellows + 1` through DisciplineEntry.YellowsPlusOne, which |
// |         |            |        | refuses BEFORE the addition rather than letting it wrap negative  |
// |         |            |        | and surface as a misleading "counting bug" range refusal.         |
// | 1.10    | 2026-08-16, latest | — | Carried L2, re-rated Medium, closed in full. New:               |
// |         |            |        | AddBan_NearIntMaxValue_ThrowsOverflowException_EntryUnmodified,   |
// |         |            |        | mirroring v1.9's AddYellow_AtIntMaxValue lock for AddBan's own    |
// |         |            |        | addition (already routed through BanMatchesPlus at v1.9, but with |
// |         |            |        | no test of this shape until now). New:                            |
// |         |            |        | AddYellow_CrossingThresholdWithBanNearIntMaxValue_                |
// |         |            |        | ThrowsOverflowException_EntryUnmodified — the finding's own        |
// |         |            |        | scenario: seeds BanMatchesRemaining == int.MaxValue and a row one |
// |         |            |        | yellow below threshold, so the 5th yellow crosses and the SECOND, |
// |         |            |        | previously-unrouted addition (the accumulation-ban `+=`) is the   |
// |         |            |        | one that overflows. DisciplineRules.cs v1.10 now routes it through|
// |         |            |        | DisciplineEntry.BanMatchesPlus, so the whole call is refused with |
// |         |            |        | neither field moved, rather than leaving Yellows already reduced  |
// |         |            |        | by the residual subtraction while the ban addition was lost.      |
#endregion
