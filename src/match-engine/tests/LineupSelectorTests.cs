// File:     src/match-engine/tests/LineupSelectorTests.cs
// Created:  2026-07-19
// Modified: 2026-08-06 (T2 AR pass 1: + the CanSelect/Select equivalence lock)
// Author:   —
// Spec:     Lineup Selection (Plan-3) design supplement (docs/tracking/lineup-selection-design.md);
//           Code Standards #20
// Purpose:  Unit locks for the pure LineupSelector (#27 Plan-3): per-line greedy-by-rating starter pick
//           + PlayerId tie-break (KD-L2), the KD-L1 position mapping across all three families, fail-loud
//           on a short starter line (KD-L3), best-remaining bench fill + bench GK flag, a coherent squad
//           reproducing roster order (KD-L5), the mean-attribute rating, and two-call determinism.

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PositioningAI;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class LineupSelectorTests
    {
        private const int ClubId = 7;

        /// <summary>Builds a squad from an explicit (position, uniform-rating) spec; PlayerId ascends with index.</summary>
        private static Squad SquadFrom(params (PlayerPosition pos, int rating)[] spec)
        {
            var players = new PlayerRecord[spec.Length];
            for (int k = 0; k < spec.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(ClubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = spec[k].pos;
                int[] a = new int[PlayerDatabaseConstants.ATTRIBUTE_COUNT];
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = spec[k].rating;
                }
                p.Attributes.FromArray(a);   // WeakFootRating untouched (irrelevant to the rating)
                players[k] = p;
            }
            return new Squad(ClubId, players);
        }

        private static (PlayerPosition, int) P(PlayerPosition pos, int rating) => (pos, rating);

        /// <summary>The coarse position a formation slot requires (mirrors LineupSelector.RequiredPosition, KD-L1).</summary>
        private static PlayerPosition Expected(in FormationSlotRecord slot)
        {
            if (slot.IsGoalkeeper)
            {
                return PlayerPosition.Goalkeeper;
            }
            switch (slot.DefaultLine)
            {
                case LineId.Defense:  return PlayerPosition.Defender;
                case LineId.Midfield: return PlayerPosition.Midfielder;
                default:              return PlayerPosition.Forward;
            }
        }

        /// <summary>A position-coherent, all-neutral 18-player squad ordered [GK, Def×4, Mid×4, Fwd×2, bench].</summary>
        private static Squad CoherentSquad()
        {
            return SquadFrom(
                P(PlayerPosition.Goalkeeper, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Forward, 10), P(PlayerPosition.Forward, 10),
                // bench filler (7): higher PlayerId, never displaces an intended starter
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Forward, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Forward, 10),
                P(PlayerPosition.Midfielder, 10));
        }

        /// <summary>Enough of every position for all three families (F4231 needs 5 mids, F433 needs 3 fwds).</summary>
        private static Squad RichSquad()
        {
            var spec = new (PlayerPosition, int)[22];
            int i = 0;
            spec[i++] = P(PlayerPosition.Goalkeeper, 10); spec[i++] = P(PlayerPosition.Goalkeeper, 10);
            for (int d = 0; d < 6; d++) { spec[i++] = P(PlayerPosition.Defender, 10); }
            for (int m = 0; m < 8; m++) { spec[i++] = P(PlayerPosition.Midfielder, 10); }
            for (int f = 0; f < 6; f++) { spec[i++] = P(PlayerPosition.Forward, 10); }
            return SquadFrom(spec);
        }

        [Test]
        public void Select_PicksHighestRatedForLineFirstSlot()
        {
            // Defender at index 2 is the highest-rated defender ⇒ it must take the first defender slot
            // (pitch slot 1), ahead of the lower-index but equal-rated defenders 1/3/4 (KD-L2).
            Squad squad = SquadFrom(
                P(PlayerPosition.Goalkeeper, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 15),  // idx 2 = best defender
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Forward, 10), P(PlayerPosition.Forward, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10));

            LineupPlan plan = LineupSelector.Select(squad, FormationFamily.F442);

            Assert.AreEqual(2, plan.StarterLocalIndices[1], "The best defender must fill the first defender slot.");
        }

        [Test]
        public void Select_TieBreaksByAscendingPlayerId()
        {
            // All defenders equal-rated ⇒ the four defender slots take the four lowest-PlayerId defenders
            // in order (indices 1,2,3,4), reproducing roster order within the line (KD-L2 tie-break).
            LineupPlan plan = LineupSelector.Select(CoherentSquad(), FormationFamily.F442);

            Assert.AreEqual(1, plan.StarterLocalIndices[1]);
            Assert.AreEqual(2, plan.StarterLocalIndices[2]);
            Assert.AreEqual(3, plan.StarterLocalIndices[3]);
            Assert.AreEqual(4, plan.StarterLocalIndices[4]);
        }

        [Test]
        public void Select_MapsEachStarterSlotToRequiredPosition_AllFamilies()
        {
            foreach (FormationFamily family in new[]
                     { FormationFamily.F442, FormationFamily.F433, FormationFamily.F4231 })
            {
                Squad squad = RichSquad();
                LineupPlan plan = LineupSelector.Select(squad, family);
                FormationSlotRecord[] slots = PositioningAIConstants.GetFormationSlots(family);

                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    PlayerPosition got = squad.GetPlayer(plan.StarterLocalIndices[k]).Position;
                    Assert.AreEqual(Expected(in slots[k]), got,
                        $"{family} starter slot {k} got a {got}, not the required position (KD-L1).");
                    Assert.AreEqual(slots[k].IsGoalkeeper, plan.StarterIsGoalkeeper[k],
                        $"{family} slot {k} GK flag must equal the formation slot's own flag (KD-L4).");
                }
            }
        }

        [Test]
        public void Select_CoherentSquad_ReproducesRosterOrder()
        {
            // KD-L5: a coherent, best-rated-first, all-neutral squad selects index k for pitch slot k.
            LineupPlan plan = LineupSelector.Select(CoherentSquad(), FormationFamily.F442);

            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                Assert.AreEqual(k, plan.StarterLocalIndices[k], $"Coherent squad must map index {k} → slot {k}.");
            }
            Assert.IsTrue(plan.StarterIsGoalkeeper[0], "Slot 0 is the goalkeeper slot.");
        }

        [Test]
        public void Select_ThrowsWhenGoalkeeperMissing()
        {
            // 18 outfielders, no goalkeeper ⇒ the GK starter slot has no eligible player (KD-L3).
            var spec = new (PlayerPosition, int)[18];
            for (int k = 0; k < 6; k++)  { spec[k] = P(PlayerPosition.Defender, 10); }
            for (int k = 6; k < 12; k++) { spec[k] = P(PlayerPosition.Midfielder, 10); }
            for (int k = 12; k < 18; k++) { spec[k] = P(PlayerPosition.Forward, 10); }

            Assert.Throws<System.ArgumentException>(() =>
                LineupSelector.Select(SquadFrom(spec), FormationFamily.F442));
        }

        [Test]
        public void Select_ThrowsWhenLineTooShort()
        {
            // F442 needs two forwards; supply only one ⇒ the second forward slot fails loud (KD-L3).
            var spec = new (PlayerPosition, int)[18];
            spec[0] = P(PlayerPosition.Goalkeeper, 10);
            for (int k = 1; k < 9; k++)  { spec[k] = P(PlayerPosition.Defender, 10); }
            for (int k = 9; k < 17; k++) { spec[k] = P(PlayerPosition.Midfielder, 10); }
            spec[17] = P(PlayerPosition.Forward, 10);

            Assert.Throws<System.ArgumentException>(() =>
                LineupSelector.Select(SquadFrom(spec), FormationFamily.F442));
        }

        [Test]
        public void Select_FillsBenchWithBestRemaining()
        {
            // Two forwards out-rate a third, distinct-but-lower forward: the two start, the third is the
            // best remaining player ⇒ bench slot 0 (KD-L3 best-remaining bench, position-agnostic).
            Squad squad = SquadFrom(
                P(PlayerPosition.Goalkeeper, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Forward, 14), P(PlayerPosition.Forward, 14),  // idx 9,10 start
                P(PlayerPosition.Forward, 12),                                 // idx 11 benched, best remaining
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Forward, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Midfielder, 10));

            LineupPlan plan = LineupSelector.Select(squad, FormationFamily.F442);

            Assert.AreEqual(11, plan.BenchLocalIndices[0],
                "Bench slot 0 must be the highest-rated non-starter.");
            Assert.IsFalse(plan.BenchIsGoalkeeper[0], "A forward is not a goalkeeper.");
        }

        [Test]
        public void Select_FlagsBenchGoalkeeper()
        {
            // A second, lower-rated goalkeeper (index 11) benches; the starter GK slot takes the better
            // one (index 0). The bench GK flag must be set from the benched player's position (KD-L4).
            Squad squad = SquadFrom(
                P(PlayerPosition.Goalkeeper, 12),   // idx 0 starts (higher-rated)
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                P(PlayerPosition.Forward, 10), P(PlayerPosition.Forward, 10),
                P(PlayerPosition.Goalkeeper, 11),   // idx 11 benches; highest-rated non-starter ⇒ bench slot 0
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Defender, 10),
                P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Forward, 10),
                P(PlayerPosition.Defender, 10), P(PlayerPosition.Midfielder, 10));

            LineupPlan plan = LineupSelector.Select(squad, FormationFamily.F442);

            Assert.AreEqual(0, plan.StarterLocalIndices[0], "The higher-rated GK starts.");
            Assert.AreEqual(11, plan.BenchLocalIndices[0], "The second GK is the best remaining ⇒ bench slot 0.");
            Assert.IsTrue(plan.BenchIsGoalkeeper[0], "The benched goalkeeper's bench GK flag must be set (KD-L4).");
        }

        [Test]
        public void Select_IsDeterministic()
        {
            LineupPlan a = LineupSelector.Select(RichSquad(), FormationFamily.F433);
            LineupPlan b = LineupSelector.Select(RichSquad(), FormationFamily.F433);

            CollectionAssert.AreEqual(a.StarterLocalIndices, b.StarterLocalIndices);
            CollectionAssert.AreEqual(a.BenchLocalIndices, b.BenchLocalIndices);
        }

        [Test]
        public void MeanAttribute_IsArithmeticMeanOfThe31Fields()
        {
            var neutral = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault(); // all 10
            Assert.AreEqual(10f, LineupSelector.MeanAttribute(in neutral), 1e-6f);

            int[] all12 = new int[PlayerDatabaseConstants.ATTRIBUTE_COUNT];
            for (int f = 0; f < all12.Length; f++) { all12[f] = 12; }
            var raised = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            raised.FromArray(all12);
            Assert.AreEqual(12f, LineupSelector.MeanAttribute(in raised), 1e-6f);
        }

        // ── CanSelect must never disagree with Select (T2 AR pass 1) ──────────────────────

        /// <summary>
        /// The equivalence the availability filter's press-back-in loop rests on: its exit condition is
        /// <c>CanSelect</c>, and what it hands to <c>ConfigureSquads</c> is adjudicated by
        /// <c>Select</c>. If the two ever disagreed, the filter would stop pressing players in on a
        /// squad the engine then refuses — so this asserts the agreement directly, across squads that
        /// pass and squads that fail for each distinct reason.
        /// </summary>
        [TestCase(FormationFamily.F442)]
        [TestCase(FormationFamily.F433)]
        [TestCase(FormationFamily.F4231)]
        public void CanSelect_AgreesWithSelect_OnEverySquadShape(FormationFamily family)
        {
            Squad[] cases =
            {
                CoherentSquad(18),                 // exactly enough, every line covered
                CoherentSquad(25),                 // a full roster
                CoherentSquad(17),                 // one short of the bench
                CoherentSquad(11),                 // an XI with no bench at all
                SquadFrom(                          // eighteen fit outfielders and NO goalkeeper
                    P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                    P(PlayerPosition.Defender, 10), P(PlayerPosition.Defender, 10),
                    P(PlayerPosition.Defender, 10), P(PlayerPosition.Midfielder, 10),
                    P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                    P(PlayerPosition.Midfielder, 10), P(PlayerPosition.Midfielder, 10),
                    P(PlayerPosition.Forward, 10), P(PlayerPosition.Forward, 10),
                    P(PlayerPosition.Forward, 10), P(PlayerPosition.Forward, 10),
                    P(PlayerPosition.Defender, 10), P(PlayerPosition.Midfielder, 10),
                    P(PlayerPosition.Forward, 10), P(PlayerPosition.Defender, 10)),
            };

            foreach (Squad squad in cases)
            {
                bool canSelect = LineupSelector.CanSelect(squad, family);

                bool selectSucceeded;
                try
                {
                    LineupSelector.Select(squad, family);
                    selectSucceeded = true;
                }
                catch (System.ArgumentException)
                {
                    selectSucceeded = false;
                }

                Assert.AreEqual(selectSucceeded, canSelect,
                    $"CanSelect and Select disagreed on a {squad.Count}-player squad under {family} — "
                    + "the availability filter's exit condition and the engine's own gate must be the "
                    + "same question.");
            }
        }

        /// <summary>A position-coherent roster of <paramref name="count"/>: GK, four defenders, four midfielders, two forwards, then a Def/Mid/Fwd cycle.</summary>
        private static Squad CoherentSquad(int count)
        {
            var spec = new (PlayerPosition, int)[count];
            for (int k = 0; k < count; k++)
            {
                PlayerPosition pos;
                if (k == 0)       pos = PlayerPosition.Goalkeeper;
                else if (k <= 4)  pos = PlayerPosition.Defender;
                else if (k <= 8)  pos = PlayerPosition.Midfielder;
                else if (k <= 10) pos = PlayerPosition.Forward;
                else
                {
                    switch ((k - 11) % 3)
                    {
                        case 0:  pos = PlayerPosition.Defender; break;
                        case 1:  pos = PlayerPosition.Midfielder; break;
                        default: pos = PlayerPosition.Forward; break;
                    }
                }

                spec[k] = (pos, 10);
            }

            return SquadFrom(spec);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-19 | —      | Initial implementation (#27 lineup selection Plan-3 unit       |
// |         |            |        | locks): line greedy + PlayerId tie-break, KD-L1 mapping for    |
// |         |            |        | F442/F433/F4231, short-line fail-loud, best-remaining bench +  |
// |         |            |        | bench GK flag, KD-L5 roster-order reproduction, mean rating,   |
// |         |            |        | determinism.                                                   |
// | 1.1     | 2026-08-06 | —      | T2 AR pass 1 (H): + CanSelect_AgreesWithSelect_OnEverySquad-   |
// |         |            |        | Shape. CanSelect landed as a hand-copied re-walk of Select's   |
// |         |            |        | starter loop with nothing keeping the two in step; it is now   |
// |         |            |        | one walk (TrySelect), and this locks the equivalence the       |
// |         |            |        | availability filter's exit condition rests on — including the  |
// |         |            |        | case a player-count rule cannot see, eighteen fit outfielders  |
// |         |            |        | and no goalkeeper.                                             |
#endregion
