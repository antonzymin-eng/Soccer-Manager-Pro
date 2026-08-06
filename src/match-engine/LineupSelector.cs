// File:     src/match-engine/LineupSelector.cs
// Created:  2026-07-19
// Modified: 2026-08-06 (#41 T2: + TrySelect — the one selection walk — with Select and CanSelect as its two wrappers; the viability probe #30's availability filter loops on)
// Author:   —
// Spec:     Lineup Selection (Plan-3) design supplement (docs/tracking/lineup-selection-design.md);
//           Squad/Player Data Layer #27 (KD-4 deferred PlayerPosition→slot mapping); Code Standards #20
// Purpose:  Pure, deterministic lineup selection: pick the eleven starters + seven bench players from a
//           full club Squad and assign each to its formation slot by position (KD-L1/KD-L2). Replaces
//           the Stage-0 roster-order trust mapping in MatchEngine.ConfigureSquads. Boot-time only (not
//           the 60 Hz hot path) — the ToArray() rating allocation is off-hot-path, same class as
//           MatchEngine.ValidateCanonicalRecord. Ordering, not generation: draws NO RNG (KD-L2/AR-1 L-1).

using System;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PositioningAI;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The output of <see cref="LineupSelector.Select"/>: the ordered starter + bench squad-local
    /// indices and the per-slot goalkeeper flags. <see cref="StarterLocalIndices"/> is in
    /// formation-slot order (index k = the player chosen for pitch slot k); <see cref="BenchLocalIndices"/>
    /// is in bench-slot order. The GK flags are read straight into
    /// <c>MatchEngine._isGoalkeeper</c> / <c>_benchIsGoalkeeper</c> (KD-L4), replacing the boot
    /// <c>k == 0</c> seed. Arrays are freshly allocated per <see cref="LineupSelector.Select"/> call.
    /// </summary>
    internal readonly struct LineupPlan
    {
        /// <summary>Squad-local index chosen for each pitch slot (length <c>PLAYERS_PER_TEAM</c>).</summary>
        public readonly int[] StarterLocalIndices;

        /// <summary>Squad-local index chosen for each bench slot (length <c>SUBSTITUTES_PER_TEAM</c>).</summary>
        public readonly int[] BenchLocalIndices;

        /// <summary>Goalkeeper flag per pitch slot — the formation slot's own <c>IsGoalkeeper</c> (KD-L4).</summary>
        public readonly bool[] StarterIsGoalkeeper;

        /// <summary>Goalkeeper flag per bench slot — the chosen player's coarse position is Goalkeeper (KD-L4).</summary>
        public readonly bool[] BenchIsGoalkeeper;

        public LineupPlan(
            int[] starterLocalIndices, int[] benchLocalIndices,
            bool[] starterIsGoalkeeper, bool[] benchIsGoalkeeper)
        {
            StarterLocalIndices = starterLocalIndices;
            BenchLocalIndices   = benchLocalIndices;
            StarterIsGoalkeeper = starterIsGoalkeeper;
            BenchIsGoalkeeper   = benchIsGoalkeeper;
        }
    }

    /// <summary>
    /// Deterministic lineup selection over a <see cref="Squad"/> and a <see cref="FormationFamily"/>.
    /// Per-line greedy by rating (KD-L2): for each formation slot in order, choose the highest-rated
    /// not-yet-selected squad player whose coarse <see cref="PlayerPosition"/> matches the slot
    /// (KD-L1 — the slot's <c>DefaultLine</c> / goalkeeper flag mapped to <see cref="PlayerPosition"/>);
    /// tie-break by ascending <c>PlayerId</c> (a total order — no RNG). A starter slot with no eligible
    /// player fails loud (KD-L3). The seven bench slots are position-agnostic, filled by best remaining.
    /// The KD-L1 <c>DefaultLine → PlayerPosition</c> bridge lives here in the match-engine (the consumer),
    /// keeping <c>player-database</c> free of positioning-ai's <see cref="LineId"/> (KD-4's
    /// "no shared type, no cross-reference").
    /// </summary>
    internal static class LineupSelector
    {
        /// <summary>
        /// Selects the starters + bench for one squad under <paramref name="family"/>. Draws no RNG.
        /// </summary>
        /// <param name="squad">The full club roster (up to <c>CLUB_SQUAD_SIZE</c> players).</param>
        /// <param name="family">The formation whose slot positions the starters must fill.</param>
        /// <exception cref="ArgumentException">
        /// A starter slot's required position has no eligible unselected player (KD-L3), or the squad
        /// is too small to fill the bench (the caller's size gate is expected to catch this first).
        /// </exception>
        public static LineupPlan Select(Squad squad, FormationFamily family)
        {
            if (!TrySelect(squad, family, out LineupPlan plan, out string failure))
            {
                throw new ArgumentException(failure);
            }

            return plan;
        }

        /// <summary>
        /// The single selection walk (KD-L1/KD-L2/KD-L3), reporting rather than throwing.
        /// <see cref="Select"/> and <see cref="CanSelect"/> are both thin wrappers over it — there is
        /// exactly ONE implementation of "which eleven does this squad field", which is the point: a
        /// second copy would answer the old question the first time a selection rule changed, and
        /// <c>SelectAvailable</c>'s press-back-in loop would then exit on a squad
        /// <c>ConfigureSquads</c> refuses.
        /// </summary>
        /// <param name="squad">The full club roster.</param>
        /// <param name="family">The formation whose slot positions the starters must fill.</param>
        /// <param name="plan">The selected starters + bench; <c>default</c> when selection fails.</param>
        /// <param name="failure">Why selection failed; <c>null</c> on success.</param>
        internal static bool TrySelect(
            Squad squad, FormationFamily family, out LineupPlan plan, out string failure)
        {
            int starterCount = MatchEngineConstants.PLAYERS_PER_TEAM;
            int benchCount   = MatchEngineConstants.SUBSTITUTES_PER_TEAM;
            FormationSlotRecord[] slots = PositioningAIConstants.GetFormationSlots(family);

            int n = squad.Count;
            bool[] selected = new bool[n];

            int[]  starterLocal = new int[starterCount];
            bool[] starterGk    = new bool[starterCount];
            for (int s = 0; s < starterCount; s++)
            {
                FormationSlotRecord slot = slots[s];
                PlayerPosition required = RequiredPosition(in slot);
                int best = FindBest(squad, selected, matchPosition: true, required: required);
                if (best < 0)
                {
                    plan = default;
                    failure =
                        $"LineupSelector: no eligible {required} for starter slot {s} (formation "
                        + $"{family}) — the squad is position-incomplete for the required lineup (KD-L3).";
                    return false;
                }
                selected[best]    = true;
                starterLocal[s]   = best;
                starterGk[s]      = slot.IsGoalkeeper;   // KD-L4: GK identity from the selected slot.
            }

            int[]  benchLocal = new int[benchCount];
            bool[] benchGk    = new bool[benchCount];
            for (int b = 0; b < benchCount; b++)
            {
                int best = FindBest(squad, selected, matchPosition: false, required: default);
                if (best < 0)
                {
                    // Unreachable when the caller's size gate (Count >= starters + bench) has run, but
                    // report rather than emit a partial bench if this is called directly.
                    plan = default;
                    failure =
                        $"LineupSelector: only {n} players — too few to fill "
                        + $"{starterCount} starters + {benchCount} bench slots.";
                    return false;
                }
                selected[best]  = true;
                benchLocal[b]   = best;
                benchGk[b]      = squad.GetPlayer(best).Position == PlayerPosition.Goalkeeper;
            }

            plan = new LineupPlan(starterLocal, benchLocal, starterGk, benchGk);
            failure = null;
            return true;
        }

        /// <summary>
        /// Highest-rated unselected player (optionally filtered to <paramref name="required"/>), tie-broken
        /// by ascending <c>PlayerId</c>; −1 if none. Deterministic: equal-attribute players produce an
        /// exact-equal rating, so the <c>PlayerId</c> total order fully decides ties (KD-L2).
        /// </summary>
        private static int FindBest(Squad squad, bool[] selected, bool matchPosition, PlayerPosition required)
        {
            int   best       = -1;
            float bestRating = 0f;
            int   bestId     = 0;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i])
                {
                    continue;
                }
                PlayerRecord p = squad.GetPlayer(i);
                if (matchPosition && p.Position != required)
                {
                    continue;
                }
                float rating = MeanAttribute(in p.Attributes);
                if (best < 0
                    || rating > bestRating
                    || (rating == bestRating && p.PlayerId < bestId))
                {
                    best       = i;
                    bestRating = rating;
                    bestId     = p.PlayerId;
                }
            }
            return best;
        }

        /// <summary>
        /// Player rating = arithmetic mean of the 31 <c>[1,20]</c> attributes
        /// (<see cref="PlayerAttributes.ToArray"/>). <c>WeakFootRating</c>'s <c>[1,5]</c> scale is
        /// excluded (KD-2 / KD-L2). A coarse position-average, not a role-weighted overall — a
        /// role-weighted rating is a Stage-1 tuning concern, deliberately not invented here.
        /// </summary>
        public static float MeanAttribute(in PlayerAttributes attributes)
        {
            int[] values = attributes.ToArray();
            long sum = 0;
            for (int f = 0; f < values.Length; f++)
            {
                sum += values[f];
            }
            return (float)sum / values.Length;
        }

        /// <summary>
        /// The mean <see cref="MeanAttribute"/> rating over the eleven players <see cref="Select"/>
        /// chooses for <paramref name="family"/> — "how strong is the team this club actually fields".
        /// <para>
        /// The bench is deliberately excluded: squad depth must not make a club stronger on the day
        /// (league-bootstrap KD-7 AR-2 L-1). Pure and RNG-free, like everything else here.
        /// </para>
        /// <para>
        /// Public consumers reach this through <see cref="SquadRating"/>, which is the narrow seam #30's
        /// <c>SeasonLoop</c> (a different assembly) uses; keeping the formation-parameterized form
        /// internal keeps <see cref="FormationFamily"/> out of the public match-engine surface, so
        /// <c>season-save</c> needs no <c>positioning-ai</c> reference.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">The squad cannot field <paramref name="family"/> (KD-L3).</exception>
        internal static float StartingElevenMean(Squad squad, FormationFamily family)
        {
            LineupPlan plan = Select(squad, family);
            int[] starters = plan.StarterLocalIndices;
            float sum = 0f;
            for (int s = 0; s < starters.Length; s++)
            {
                PlayerRecord starter = squad.GetPlayer(starters[s]);
                sum += MeanAttribute(in starter.Attributes);
            }

            return sum / starters.Length;
        }

        /// <summary>
        /// Whether <see cref="Select"/> would succeed for <paramref name="family"/> — literally the
        /// same walk (<see cref="TrySelect"/>), reporting instead of throwing.
        /// <para>
        /// It exists for #30's availability filter (#41 FR-MD-023), which removes injured players and
        /// can leave a club position-incomplete; the filter presses the least-injured back in until the
        /// club can play, and needs to ask that question repeatedly rather than treat a thrown
        /// exception as a loop condition. Public consumers reach it through
        /// <see cref="SquadRating.CanFieldStartingEleven"/>.
        /// </para>
        /// </summary>
        /// <param name="squad">The roster to test.</param>
        /// <param name="family">The formation whose slots must be fillable.</param>
        internal static bool CanSelect(Squad squad, FormationFamily family) =>
            TrySelect(squad, family, out _, out _);

        /// <summary>
        /// KD-L1: the coarse <see cref="PlayerPosition"/> a formation slot requires — its own goalkeeper
        /// flag, else its <c>DefaultLine</c> (Defense→Defender, Midfield→Midfielder, Attack→Forward).
        /// </summary>
        private static PlayerPosition RequiredPosition(in FormationSlotRecord slot)
        {
            if (slot.IsGoalkeeper)
            {
                return PlayerPosition.Goalkeeper;
            }
            switch (slot.DefaultLine)
            {
                case LineId.Defense:  return PlayerPosition.Defender;
                case LineId.Midfield: return PlayerPosition.Midfielder;
                case LineId.Attack:   return PlayerPosition.Forward;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(slot), slot.DefaultLine, "Undefined LineId on a formation slot.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-19 | —      | Initial implementation (#27 lineup selection Plan-3): per-line |
// |         |            |        | greedy-by-rating starter pick (KD-L1/KD-L2), fail-loud on a    |
// |         |            |        | short line (KD-L3), best-remaining bench, GK flags from the    |
// |         |            |        | selection (KD-L4). Pure, no RNG.                                |
// | 1.1     | 2026-07-26 | —      | #30 T2 prerequisite (league-bootstrap KD-7 / AR-4 M-1): new    |
// |         |            |        | StartingElevenMean — the XI-mean rating the round-resolution   |
// |         |            |        | model consumes, exposed publicly via SquadRating rather than   |
// |         |            |        | re-implemented in season-save (the parallel-surface trap).      |
// | 1.2     | 2026-08-06 | —      | #41 T2: + CanSelect — Select's starter walk reporting instead   |
// |         |            |        | of throwing, for the availability filter's press-the-least-    |
// |         |            |        | injured-back-in loop. An injury list can leave a club           |
// |         |            |        | position-incomplete, which would otherwise stop the season.     |
// | 1.3     | 2026-08-06 | —      | AR pass 1 (H): v1.2 shipped CanSelect as a hand-copied re-walk |
// |         |            |        | of Select's starter loop — two implementations of "which       |
// |         |            |        | eleven does this squad field", with nothing keeping them in    |
// |         |            |        | step and no equivalence test. Any rule added to Select (a #44  |
// |         |            |        | ban filter is the near one) would leave CanSelect answering    |
// |         |            |        | the old question, and SelectAvailable's press-back-in loop     |
// |         |            |        | would exit on a squad ConfigureSquads then refuses — the       |
// |         |            |        | parallel-surface trap SquadRating exists to prevent. Collapsed |
// |         |            |        | to ONE walk: internal TrySelect(out plan, out failure); Select |
// |         |            |        | throws on false, CanSelect discards both.                      |
#endregion
