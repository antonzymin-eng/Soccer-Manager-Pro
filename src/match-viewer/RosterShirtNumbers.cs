// File:     src/match-viewer/RosterShirtNumbers.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md),
//           Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Code Standards #20
// Purpose:  The one implementation of the Stage-0 shirt-numbering rule — per-team sequential from 1
//           in roster order — shared by every View that draws a number on a marker.

using System;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Assigns the shirt number drawn on a roster slot's marker.
    ///
    /// <para><b>Why this lives here rather than in either View.</b> Two surfaces draw the number —
    /// the browser viewer's canvas and the Unity client's <c>MatchRoster</c> — and for one landing
    /// each had its own copy of the rule (the viewer's in inline JavaScript, the client's in C#).
    /// Two implementations of one rule drift, and the rule is explicitly a placeholder: Stage 0
    /// carries no shirt-number field on a player, so when player records gain real numbers this must
    /// be the single place that changes. It sits in <c>match-viewer</c> because that is the lowest
    /// assembly both consumers can reach — <c>match-client-core</c> references this one, not the
    /// reverse.</para>
    ///
    /// <para><b>The rule.</b> Within each team, slots are numbered 1..n in roster order. The engine
    /// seeds each team's keeper at local index 0, so the keeper gets the 1 shirt without being
    /// special-cased. Numbers belong to the <b>slot</b>, not the player: a substitution puts a
    /// different person in the slot and they inherit its number. That is a known Stage-0
    /// simplification, not an oversight.</para>
    /// </summary>
    public static class RosterShirtNumbers
    {
        /// <summary>
        /// Returns the shirt number for each roster slot, given that slot's team id.
        /// </summary>
        /// <param name="teamIds">Team id per roster slot; every entry must be in [0, TEAM_COUNT).</param>
        /// <returns>A new array of the same length, each entry 1-based within its team.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="teamIds"/> is null.</exception>
        /// <exception cref="ArgumentException">An entry is not a valid team id. Silently bucketing an
        /// out-of-range id would hand two players the same number, or crash on the array index.</exception>
        public static int[] Assign(int[] teamIds)
        {
            if (teamIds == null) { throw new ArgumentNullException(nameof(teamIds)); }

            var shirtNumbers = new int[teamIds.Length];
            var assigned     = new int[MatchEngineConstants.TEAM_COUNT];

            for (int i = 0; i < teamIds.Length; i++)
            {
                int teamId = teamIds[i];
                if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
                {
                    throw new ArgumentException(
                        "teamIds[" + Inv(i) + "] is " + Inv(teamId) + "; expected [0, " +
                        Inv(MatchEngineConstants.TEAM_COUNT) + ").", nameof(teamIds));
                }

                shirtNumbers[i] = ++assigned[teamId];
            }

            return shirtNumbers;
        }

        private static string Inv(int value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (P4a AR pass, M-6): the shirt-numbering rule   |
// |         |            |        | had two implementations after P4a — MatchRoster's C# and the   |
// |         |            |        | browser viewer's inline computeJersey — while three documents  |
// |         |            |        | claimed it had MOVED into the former. One rule, one            |
// |         |            |        | implementation, in the lowest assembly both Views can reach.   |
#endregion
