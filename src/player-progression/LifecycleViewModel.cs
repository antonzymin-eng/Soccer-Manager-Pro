// File:     src/player-progression/LifecycleViewModel.cs
// Created:  2026-08-08
// Modified: 2026-08-24 (round-2 finding classifyageband-public-with-no-callers-and-a-silently-changed-
//           meaning: + AgeBand — v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §2.2 / §4.2 / KD-7, FR-PG-023 (observer-neutral read-only
//           surface); Code Standards #20
// Purpose:  The read-only value-copy observation surface over one player's lifecycle state, for #31
//           (valuations) and #38 (UI). Reading it cannot mutate anything — the MatchEngine.BallView
//           posture.

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// A read-only snapshot of one player's lifecycle state (§2.2 / KD-7). Every field is a value copy
    /// taken at the call, so an observer can never reach the engine's arrays and reading is
    /// digest-neutral (FR-PG-023) — the same discipline as <c>SeasonViewModel</c> / <c>TrainingViewModel</c>.
    /// </summary>
    public readonly struct LifecycleViewModel
    {
        /// <summary>The player this view describes.</summary>
        public readonly int PlayerId;

        /// <summary>Current derived age in whole years (§3.1.1) — never the new-game seed.</summary>
        public readonly int Age;

        /// <summary>The derived CurrentAbility summary on the <c>[0, ABILITY_MAX]</c> scale.</summary>
        public readonly int CurrentAbility;

        /// <summary>The PotentialAbility ceiling.</summary>
        public readonly int PotentialAbility;

        /// <summary>
        /// The sign of THIS CALENDAR YEAR's net accrual — <c>AbilityModel.ClassifyAgeBand(Age)</c>, read
        /// once at the call (round-2 finding classifyageband-public-with-no-callers-and-a-silently-
        /// changed-meaning). This is the #31/#38 read surface for it: <c>ClassifyAgeBand</c> itself is
        /// internal, since this field — computed here, once per view — is its only production caller.
        /// <b>Year-granular, not "is he growing today":</b> inside a ramp the per-day accrual is
        /// <c>{0, ±1}</c> and adjacent days can differ even though this field, re-derived tomorrow, reads
        /// the same band all year (see <c>AbilityModel.ClassifyAgeBand</c>'s own doc, §3.1.3).
        /// </summary>
        public readonly AbilityModel.AgeBand AgeBand;

        /// <summary>True once the player has crossed <c>RETIREMENT_AGE</c> (FR-PG-013); he stays selectable until the season boundary (FR-PG-014).</summary>
        public readonly bool RetirementFlag;

        /// <summary>The world day <see cref="RetirementFlag"/> was set (0 if not flagged).</summary>
        public readonly uint RetirementDay;

        /// <summary>Builds the value-copy view.</summary>
        public LifecycleViewModel(
            int playerId,
            int age,
            int currentAbility,
            int potentialAbility,
            AbilityModel.AgeBand ageBand,
            bool retirementFlag,
            uint retirementDay)
        {
            PlayerId = playerId;
            Age = age;
            CurrentAbility = currentAbility;
            PotentialAbility = potentialAbility;
            AgeBand = ageBand;
            RetirementFlag = retirementFlag;
            RetirementDay = retirementDay;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1: the FR-PG-023 observer-neutral read surface.           |
// | 1.1     | 2026-08-24 | —      | Round-2 finding classifyageband-public-with-no-callers-and-a-  |
// |         |            |        | silently-changed-meaning. + AgeBand field: the M7 DECISION was |
// |         |            |        | to keep AbilityModel.AgeBand PUBLIC (a #38 read surface) but   |
// |         |            |        | give it a real caller here, alongside RetirementFlag/          |
// |         |            |        | RetirementDay, rather than leave AbilityModel.ClassifyAgeBand  |
// |         |            |        | public with zero callers anywhere in src/. Constructor gains   |
// |         |            |        | the ageBand parameter (ProgressionEngine.LifecycleView is the  |
// |         |            |        | one production call site). No format version — this type is    |
// |         |            |        | never serialized (FR-PG-023).                                  |
#endregion
