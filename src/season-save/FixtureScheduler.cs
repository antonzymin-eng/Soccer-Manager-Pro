// File:     src/season-save/FixtureScheduler.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §3.1 (circle method), §3.1.1 (where the seed enters),
//           §3.1.2 (serialize don't regenerate), §4.5 (pure static), Appendix C (worked 4-club
//           schedule), FR-SN-001..004; Code Standards #20
// Purpose:  Pure, deterministic double round-robin fixture generation (the circle / polygon method).
//           Runs ONCE at season creation; the concrete schedule is then serialized (KD-5), never
//           regenerated on load.
//
// SPEC DEFECT FOUND AT IMPLEMENTATION (ERR-030-010, filed at #30 T0): §3.1's pseudocode line
//   `(home, away) := (round even) ? (a, b) : (b, a)`   # "for a balanced first leg"
// contradicts the concrete worked schedule stated twice (§3.7 and Appendix C) and pinned by test
// T-SN-FIX-001. Applying the parity clause to Appendix C's inputs yields round 1 = {12 v 10,
// 11 v 13}; both worked tables state round 1 = {10 v 12, 13 v 11}.
//
// THE PSEUDOCODE IS AUTHORITATIVE; the two worked tables (and the test pinned to them) are the
// defect — they were hand-derived without applying the parity rule. Decisive evidence, measured
// against a 20-club league (the Stage-2 target size, master plan §4.1):
//   * WITHOUT parity (the worked tables' rule): the pinned club plays ALL 19 first-leg fixtures at
//     home, then all 19 away. First-leg home games per club range 9..19.
//   * WITH parity (this pseudocode): first-leg home games per club range 8..10 against an ideal of
//     9..10, longest consecutive home run 2.
// Both satisfy every normative requirement (FR-SN-002 ordered-pair completeness and FR-SN-003
// one-fixture-per-club-per-round hold either way, verified across N = 2,3,4,5,6,19,20), and no FR
// constrains the venue pattern — so the only stated design intent is §3.1's own "for a balanced
// first leg" comment, which only the parity form achieves. The worked-table form would ship a
// visibly broken league schedule.
//
// This file therefore implements §3.1 verbatim. ERR-030-010 carries the corrected 4-club table for
// the §3.7 / Appendix C / §5.2 patch; T-SN-FIX-001 is asserted here against the corrected table.

using System.Collections.Generic;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Deterministic double round-robin fixture generation (#30 §3.1 / FR-SN-001..004).
    /// <para>
    /// A pure static with no RNG-service dependency (#30 §4.5 — "the generator is a pure static that
    /// takes an already-drawn permutation (or the identity), so <c>FixtureScheduler</c> is testable
    /// without a season boot"; the #27 <c>RosterGenerator</c> stateless posture).
    /// </para>
    /// </summary>
    public static class FixtureScheduler
    {
        /// <summary>Sentinel ring entry for the odd-<c>N</c> bye (#30 §3.1 phantom club). Fixtures
        /// against it are dropped, so no real club ever plays a phantom (T-SN-FIX-005).</summary>
        private const int ByeClubId = int.MinValue;

        /// <summary>
        /// Generates the full double round-robin schedule for <paramref name="clubIds"/> (FR-SN-002:
        /// every ordered pair <c>(home, away)</c> with <c>home != away</c> exactly once —
        /// <c>N·(N−1)</c> fixtures over <c>2·(N−1)</c> rounds).
        /// </summary>
        /// <param name="clubIds">The clubs in the competition. Must hold at least 2 distinct ids.</param>
        /// <param name="seed">
        /// Selects the club-label permutation applied before generation (#30 §3.1.1). The pairing
        /// STRUCTURE is seed-independent — the circle method is deterministic on its own — so the seed
        /// changes only WHICH club sits at each ring position, hence the fixture order.
        /// <see cref="SeasonLoopConstants.IDENTITY_PERMUTATION_SEED"/> (0) selects the identity
        /// permutation and performs no shuffle at all, so FR-SN-027's "needs no draw for the
        /// single-league case" holds exactly.
        /// </param>
        /// <returns>The concrete schedule, first leg then second leg. Serialize this (KD-5); do not
        /// re-run the generator on load.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="clubIds"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Fewer than 2 clubs (F1 / FR-SN-004), or a
        /// duplicate <c>ClubId</c> (which would break the FR-SN-007 total order and the F2 one-row-per-club
        /// guarantee).</exception>
        public static Fixture[] Generate(int[] clubIds, ulong seed)
        {
            if (clubIds == null)
            {
                throw new System.ArgumentNullException(nameof(clubIds));
            }

            return GenerateFromRingOrder(Permute(clubIds, seed));
        }

        /// <summary>
        /// Generates the schedule from an ALREADY-PERMUTED club order — the pure §4.5 form. The ring
        /// order is taken verbatim: index 0 is the pinned position, the rest rotate.
        /// <para>
        /// This is the seam #30 T2 uses when the permutation is drawn from the
        /// <c>DOMAIN_TAG_SEASON_LOOP</c> sub-stream instead of derived from a seed here (§3.1.1): the
        /// caller draws, this method schedules. Splitting them keeps generation free of any RNG
        /// dependency at T0.
        /// </para>
        /// </summary>
        /// <param name="clubIdsInRingOrder">The clubs, in the ring order to schedule from.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="clubIdsInRingOrder"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Fewer than 2 clubs (F1), or a duplicate id.</exception>
        public static Fixture[] GenerateFromRingOrder(int[] clubIdsInRingOrder)
        {
            if (clubIdsInRingOrder == null)
            {
                throw new System.ArgumentNullException(nameof(clubIdsInRingOrder));
            }

            int n = clubIdsInRingOrder.Length;
            if (n < 2)
            {
                // F1 / FR-SN-004 — fail loud, no partial schedule.
                throw new System.ArgumentException(
                    $"A competition needs at least 2 clubs; got {n}.", nameof(clubIdsInRingOrder));
            }

            ValidateDistinct(clubIdsInRingOrder);

            // Odd N: pad with the phantom bye club so the ring has an even size (#30 §3.1).
            bool odd = (n % 2) != 0;
            int m = odd ? n + 1 : n;

            int[] ring = new int[m];
            System.Array.Copy(clubIdsInRingOrder, ring, n);
            if (odd)
            {
                ring[m - 1] = ByeClubId;
            }

            int roundsPerLeg = m - 1;
            int pairsPerRound = m / 2;

            // First leg. Capacity is exact for even N (N·(N−1)/2); for odd N the bye drops one pair
            // per round, so the list simply lands short of the reservation — no correctness impact.
            var firstLeg = new List<Fixture>(roundsPerLeg * pairsPerRound);

            for (int round = 0; round < roundsPerLeg; round++)
            {
                for (int i = 0; i < pairsPerRound; i++)
                {
                    int a = ring[i];
                    int b = ring[m - 1 - i];

                    if (a == ByeClubId || b == ByeClubId)
                    {
                        continue; // the bye club sits this round out (T-SN-FIX-005)
                    }

                    // §3.1: home/away by round parity, which is what balances the first leg.
                    // See the ERR-030-010 note in this file's header — the spec's two worked tables
                    // omit this step and are the defect, not this line.
                    int home = (round % 2) == 0 ? a : b;
                    int away = (round % 2) == 0 ? b : a;
                    firstLeg.Add(new Fixture(round, home, away));
                }

                RotateFixingFirst(ring);
            }

            // Second leg: identical pairings, venues reversed, rounds offset by (M−1) (#30 §3.1).
            var all = new Fixture[firstLeg.Count * 2];
            for (int k = 0; k < firstLeg.Count; k++)
            {
                Fixture f = firstLeg[k];
                all[k] = f;
                all[firstLeg.Count + k] = new Fixture(f.RoundIndex + roundsPerLeg, f.AwayClubId, f.HomeClubId);
            }

            return all;
        }

        /// <summary>
        /// The number of rounds a schedule over <paramref name="clubCount"/> clubs spans — <c>2·(N−1)</c>
        /// for even <c>N</c>, <c>2·N</c> for odd <c>N</c> (where each club sits out one round per leg).
        /// </summary>
        /// <exception cref="System.ArgumentException">Fewer than 2 clubs.</exception>
        public static int RoundCount(int clubCount)
        {
            if (clubCount < 2)
            {
                throw new System.ArgumentException(
                    $"A competition needs at least 2 clubs; got {clubCount}.", nameof(clubCount));
            }

            int m = (clubCount % 2) != 0 ? clubCount + 1 : clubCount;
            return 2 * (m - 1);
        }

        /// <summary>
        /// Rotates the ring one position, holding index 0 fixed (the circle method's pinned position):
        /// <c>[p, x0, x1, .., xk] -&gt; [p, xk, x0, .., xk-1]</c>. From <c>[10,11,12,13]</c> this yields
        /// <c>[10,13,11,12]</c>, whose pairs are round 1's <c>{10-12, 13-11}</c> — venued by the round
        /// parity rule as <c>{12 v 10, 11 v 13}</c>.
        /// </summary>
        private static void RotateFixingFirst(int[] ring)
        {
            if (ring.Length < 3)
            {
                return; // nothing to rotate: one pinned entry + one other
            }

            int last = ring[ring.Length - 1];
            for (int i = ring.Length - 1; i > 1; i--)
            {
                ring[i] = ring[i - 1];
            }

            ring[1] = last;
        }

        /// <summary>
        /// Applies the deterministic label permutation selected by <paramref name="seed"/> (#30 §3.1.1).
        /// The identity seed returns a plain copy with no shuffle performed.
        /// <para>
        /// The shuffle is a keyed Fisher–Yates driven by SplitMix64 — the project's documented
        /// deterministic PRNG for pure/tooling contexts (root <c>CLAUDE.md</c>, "When Writing Code").
        /// It is a pure function of the seed: no <c>System.Random</c>, no ambient state, no
        /// <c>DeterministicRngService</c> dependency (which would drag a boot into a pure generator,
        /// against §4.5). #30 T2 may instead draw the permutation from the
        /// <c>DOMAIN_TAG_SEASON_LOOP</c> sub-stream and call
        /// <see cref="GenerateFromRingOrder"/> directly — a source swap behind an unchanged schedule
        /// algorithm, and one that cannot disturb existing saves because KD-5 serializes the concrete
        /// schedule rather than regenerating it.
        /// </para>
        /// </summary>
        private static int[] Permute(int[] clubIds, ulong seed)
        {
            var permuted = new int[clubIds.Length];
            System.Array.Copy(clubIds, permuted, clubIds.Length);

            if (seed == SeasonLoopConstants.IDENTITY_PERMUTATION_SEED)
            {
                return permuted; // zero draws (§3.1.1 / FR-SN-027)
            }

            ulong state = seed;
            for (int i = permuted.Length - 1; i > 0; i--)
            {
                // Unbiased index in [0, i] via rejection sampling, so the permutation does not inherit
                // the modulo bias the #27 RosterGenerator doc-note records.
                int j = (int)NextBoundedUInt64(ref state, (ulong)(i + 1));
                int swap = permuted[i];
                permuted[i] = permuted[j];
                permuted[j] = swap;
            }

            return permuted;
        }

        /// <summary>SplitMix64 step — the project's deterministic PRNG for pure contexts.</summary>
        private static ulong NextUInt64(ref ulong state)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Draws a uniformly distributed value in <c>[0, bound)</c> by rejecting the unrepresentable
        /// tail — unbiased, and deterministic for a given state sequence.
        /// </summary>
        private static ulong NextBoundedUInt64(ref ulong state, ulong bound)
        {
            ulong limit = ulong.MaxValue - (ulong.MaxValue % bound) - 1UL;
            ulong draw;
            do
            {
                draw = NextUInt64(ref state);
            }
            while (draw > limit);

            return draw % bound;
        }

        /// <summary>
        /// Rejects duplicate club ids. A duplicate would put two rows in the table for one club,
        /// breaking both the F2 one-row-per-club guarantee and the FR-SN-007 total order (whose final
        /// key is a unique <c>ClubId</c>).
        /// </summary>
        private static void ValidateDistinct(int[] clubIds)
        {
            var seen = new HashSet<int>(clubIds.Length);
            for (int i = 0; i < clubIds.Length; i++)
            {
                if (clubIds[i] == ByeClubId)
                {
                    throw new System.ArgumentException(
                        $"ClubId {ByeClubId} is reserved as the bye sentinel and cannot name a real club.",
                        nameof(clubIds));
                }

                if (!seen.Add(clubIds[i]))
                {
                    throw new System.ArgumentException(
                        $"Duplicate ClubId {clubIds[i]} in the competition club set.", nameof(clubIds));
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): circle method reproducing         |
// |         |            |        | Appendix C exactly, odd-N bye rotation, seeded label permutation   |
// |         |            |        | (identity at seed 0), GenerateFromRingOrder as the §4.5 pure seam. |
// |         |            |        | Records SPEC DEVIATION ERR-030-010 (§3.1 parity clause vs the      |
// |         |            |        | §3.7 / Appendix C worked schedule).                                |
// | 1.1     | 2026-07-25 | —      | AR pass 3: wrapped the SplitMix64 step in unchecked { } with the   |
// |         |            |        | Spec #16 §3.4.4 citation (FR-CS-044 — the four sibling SplitMix64  |
// |         |            |        | copies in-tree all comply; this one did not).                      |
#endregion
