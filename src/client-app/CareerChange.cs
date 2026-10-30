// File:     src/client-app/CareerChange.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (Career Home "what changed?"),
//           Season & Competition Loop #30 §3.4 (MatchResult) / §3.5 (SeasonRollOutcome), Code Standards #20
// Purpose:  The record of the most recent career operation, carrying exactly the #30 outputs that
//           operation produced — the managed club's result for a round, the roll outcome for a boundary.

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The most recent career operation and what #30 returned for it (§5-P7). Only the fields its
    /// <see cref="Kind"/> names are meaningful: <see cref="ManagedResult"/> when
    /// <see cref="HasManagedResult"/>, <see cref="Roll"/> for <see cref="CareerChangeKind.SeasonRolled"/>.
    /// Session-scoped and never persisted — a resumed career starts with <see cref="CareerChangeKind.Resumed"/>.
    /// </summary>
    public readonly struct CareerChange
    {
        /// <summary>The operation.</summary>
        public readonly CareerChangeKind Kind;

        /// <summary>The world day after the operation.</summary>
        public readonly uint WorldDay;

        /// <summary>Number of fixtures resolved by the operation (a round's size; 0 otherwise).</summary>
        public readonly int FixturesResolved;

        /// <summary>True when <see cref="ManagedResult"/> holds the managed club's fixture of the played round.</summary>
        public readonly bool HasManagedResult;

        /// <summary>The managed club's result in the played round (valid when <see cref="HasManagedResult"/>).</summary>
        public readonly MatchResult ManagedResult;

        /// <summary>The boundary outcome (valid for <see cref="CareerChangeKind.SeasonRolled"/>).</summary>
        public readonly SeasonRollOutcome Roll;

        private CareerChange(
            CareerChangeKind kind,
            uint worldDay,
            int fixturesResolved,
            bool hasManagedResult,
            in MatchResult managedResult,
            in SeasonRollOutcome roll)
        {
            Kind = kind;
            WorldDay = worldDay;
            FixturesResolved = fixturesResolved;
            HasManagedResult = hasManagedResult;
            ManagedResult = managedResult;
            Roll = roll;
        }

        /// <summary>A change with no #30 payload (started, resumed, day advanced).</summary>
        public static CareerChange Plain(CareerChangeKind kind, uint worldDay) =>
            new CareerChange(kind, worldDay, 0, false, default, default);

        /// <summary>A played round, with the managed club's result picked out of <paramref name="results"/>.</summary>
        public static CareerChange RoundPlayed(uint worldDay, MatchResult[] results, int managedClubId)
        {
            int count = results == null ? 0 : results.Length;
            for (int i = 0; i < count; i++)
            {
                if (results[i].HomeClubId == managedClubId || results[i].AwayClubId == managedClubId)
                {
                    return new CareerChange(CareerChangeKind.RoundPlayed, worldDay, count, true, results[i], default);
                }
            }

            return new CareerChange(CareerChangeKind.RoundPlayed, worldDay, count, false, default, default);
        }

        /// <summary>A rolled season boundary.</summary>
        public static CareerChange SeasonRolled(uint worldDay, in SeasonRollOutcome roll) =>
            new CareerChange(CareerChangeKind.SeasonRolled, worldDay, 0, false, default, roll);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
