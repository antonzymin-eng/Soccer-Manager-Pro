// File:     src/player-progression/PlayerLifecycle.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §2.2 (data structures); Code Standards #20
// Purpose:  The per-player lifecycle overlay #28 alone owns. The [1,20] attributes live on the
//           career-state PlayerRecord (#27) the T2 block also holds — NOT duplicated here (KD-1/KD-4).

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// The per-player over-time lifecycle overlay (§2.2). Held per <c>PlayerId</c> by the T2 progression
    /// block alongside the career-state <see cref="TacticalDirector.PlayerDatabase.PlayerRecord"/>.
    /// <see cref="GrowthCursor"/> is the ONLY accumulator (FR-PG-002); <see cref="CurrentAbility"/> is a
    /// derived cache of the [1,20] attributes, never a second accumulator (FR-PG-003).
    /// </summary>
    public struct PlayerLifecycle
    {
        /// <summary>The ability ceiling, wide integer [0, ABILITY_MAX]; generated once at regen/new-game, never rises (§3.2).</summary>
        public int PotentialAbility;

        /// <summary>DERIVED cache of the [1,20] attributes (recomputed each day; never a second accumulator, FR-PG-003).</summary>
        public int CurrentAbility;

        /// <summary>The ONLY accumulator — the integer fixed-point points pool (FR-PG-002); signed, so decline drains it.</summary>
        public long GrowthCursor;

        /// <summary>
        /// The authoritative age anchor (KD-A): age is DERIVED as <c>(worldDay − BirthWorldDay) /
        /// DAYS_PER_YEAR</c>, so there is no discrete year-rollover step to double-count (FR-PG-005).
        /// Pinned once at new-game from the generation-time age.
        /// </summary>
        public uint BirthWorldDay;

        /// <summary>Set on the world tick at RETIREMENT_AGE (FR-PG-013); the player stays selectable until the season boundary.</summary>
        public bool RetirementFlag;

        /// <summary>The world-day <see cref="RetirementFlag"/> was set (0 if not flagged).</summary>
        public uint RetirementDay;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
#endregion
