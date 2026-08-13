// File:     src/discipline/tests/DisciplineRulesTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13 (#44 C1/C2 adversarial review round 3, L11 — the non-discriminating kind-2
//           order test replaced with two tests driven through the new internal ApplySecondYellow
//           seam — v1.2)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.2 (thresholds & bans) / §3.3 (serving) / §3.4 (boundary &
//           hygiene); FR-DC-006/007/011/012/013/017; F2/F4; §5 T-DC-BAN-001/002/003, T-DC-HYG-001;
//           Code Standards #20
// Purpose:  Unit tests for DisciplineRules — the FR-DC-006 card-kind dispatch (including the §3.2
//           worked example and the residual-kept assertion that distinguishes it from a reset), ban
//           stacking, the [GT] guard routing/atomicity/direct-reachability trio (M4/L5), club-fixture
//           serving (including the FR-DC-017 mid-season drop and the M5 two-row descending-walk lock),
//           the season boundary sweep (with its own M5 lock), player-id migration, retirement drop, and
//           multi-competition independence.

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

        private const int Competition = DisciplineConstants.LEAGUE_COMPETITION_KEY;

        private static DisciplineRules NewRules() => new DisciplineRules(new DisciplineState());

        // ── ApplyCard kind semantics (FR-DC-006) ──────────────────────────────────────

        [Test]
        public void ApplyCard_Kind0_AddsExactlyOneYellow_NoBan()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(1, entry.Yellows);
            Assert.AreEqual(0, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplyCard_Kind1_StraightRed_AddsBanOnly_NoYellow()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_RED);

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(0, entry.Yellows, "kind 1 must carry NO yellow (FR-DC-006)");
            Assert.AreEqual(DisciplineConstants.StraightRedBanMatches, entry.BanMatchesRemaining);
        }

        [Test]
        public void ApplyCard_Kind2_SecondYellow_AddsOneYellowAndASecondYellowBan()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_SECOND_YELLOW);

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
                rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);
            }
            Assert.AreEqual(4, rules.State.EntryFor(p, Competition).Yellows, "pre-condition: 4 yellows banked");

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // the 5th ⇒ crosses

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
                rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);
            }

            // The 5th yellow (from the kind-2) crosses the threshold AND the kind-2 adds its own ban —
            // both bans stack additively (FR-DC-007).
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_SECOND_YELLOW);

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
                rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);
            }

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_RED);

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

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // 7 + 1 = 8 >= 5: crosses

            DisciplineEntry entry = rules.State.EntryFor(p, Competition);
            Assert.AreEqual(3, entry.Yellows,
                "8 - threshold(5) = 3 residual kept — 'Yellows = 0' on a crossing would read 0 here instead");
            Assert.AreEqual(DisciplineConstants.AccumBanMatches, entry.BanMatchesRemaining);
        }

        // ── Bans stack additively from any source ──────────────────────────────────

        [Test]
        public void Bans_StackAdditively_AcrossMultipleSources()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_RED);            // + StraightRedBanMatches
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_RED);            // + StraightRedBanMatches again
            rules.AddBan(p, Competition, 3);                                               // + 3 direct

            int expected = 2 * DisciplineConstants.StraightRedBanMatches + 3;
            Assert.AreEqual(expected, rules.State.EntryFor(p, Competition).BanMatchesRemaining);
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
        public void ApplyCard_Kind1_RoutesStraightRedBanMatchesThroughRequireBanLength()
        {
            // M4: at today's non-negative default this is a behaviour-preserving routing change —
            // confirmed by the existing WorkedExample_Kind1_AddsBanPlus2_YellowsUntouched test still
            // passing unmodified. RequireBanLength's own direct tests above prove the guard fires; this
            // one proves ApplyCard actually calls it (not just AddBan's separate `matches < 0` check).
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_RED);

            Assert.AreEqual(DisciplineConstants.StraightRedBanMatches,
                rules.State.EntryFor(p, Competition).BanMatchesRemaining);
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

            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_SECOND_YELLOW);

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
            // Confirms ApplyCard's kind-2 case genuinely DELEGATES to ApplySecondYellow rather than
            // carrying a parallel copy of the same three lines — the parallel-surface defect class this
            // repo keeps filing (#29/#41 T2 AR's H3, this landing's own D2/M9).
            DisciplineRules viaApplyCard = NewRules();
            DisciplineRules viaDirectCall = NewRules();
            int p = PlayerId(0, 1);

            viaApplyCard.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_SECOND_YELLOW);
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

            rules.OnClubFixturePlayed(0);

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

            rules.OnClubFixturePlayed(0);   // 1 -> 0, drops the row
            // A second played fixture with no outstanding ban must not underflow — the row is gone,
            // so this call has nothing to touch, and must not throw or resurrect a negative row.
            Assert.DoesNotThrow(() => rules.OnClubFixturePlayed(0));

            Assert.IsFalse(rules.State.HasEntry(p, Competition));
        }

        [Test]
        public void OnClubFixturePlayed_DropsARowThatReachesZeroZero_MidSeason()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.AddBan(p, Competition, 1);   // Yellows 0, ban 1 — serving this to 0 makes an all-zero row

            rules.OnClubFixturePlayed(0);

            Assert.IsFalse(rules.State.HasEntry(p, Competition),
                "FR-DC-017: a row that reaches (0,0) MID-SEASON must be dropped immediately");
        }

        [Test]
        public void OnClubFixturePlayed_NegativeClubId_Throws()
        {
            DisciplineRules rules = NewRules();
            Assert.Throws<ArgumentOutOfRangeException>(() => rules.OnClubFixturePlayed(-1));
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

            rules.OnClubFixturePlayed(0);

            Assert.IsFalse(rules.State.HasEntry(p1, Competition), "p1's one-match ban must be served and the row dropped");
            Assert.IsFalse(rules.State.HasEntry(p2, Competition), "p2's one-match ban must ALSO be served in the same call");
        }

        // ── RollToNextSeason ────────────────────────────────────────────────────────

        [Test]
        public void RollToNextSeason_UnservedBanCarries_YellowsResetButRowSurvives()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);
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
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // Yellows 1, ban 0

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
            rules.ApplyCard(p1, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // Yellows 1, ban 0
            rules.ApplyCard(p2, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // Yellows 1, ban 0

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
            rules.ApplyCard(oldId, Competition, DisciplineConstants.CARD_KIND_YELLOW);
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
            rules.ApplyCard(oldId, Competition, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(newId, Competition, DisciplineConstants.CARD_KIND_YELLOW);   // target already carries a row here

            Assert.Throws<ArgumentException>(() => rules.MigratePlayerId(oldId, newId));
        }

        [Test]
        public void MigratePlayerId_ToTheSameId_IsANoOp()
        {
            DisciplineRules rules = NewRules();
            int p = PlayerId(0, 1);
            rules.ApplyCard(p, Competition, DisciplineConstants.CARD_KIND_YELLOW);

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
            rules.ApplyCard(target, 0, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(target, 1, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(other, 0, DisciplineConstants.CARD_KIND_YELLOW);

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

            rules.ApplyCard(p, 0, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(p, 0, DisciplineConstants.CARD_KIND_YELLOW);
            rules.ApplyCard(p, 1, DisciplineConstants.CARD_KIND_YELLOW);

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
#endregion
