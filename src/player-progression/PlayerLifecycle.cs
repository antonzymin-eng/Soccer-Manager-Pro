// File:     src/player-progression/PlayerLifecycle.cs
// Created:  2026-07-24
// Modified: 2026-08-08
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
        /// <para>
        /// <b>Signed, and that is load-bearing (ERR-028-006).</b> §3.1.1 pins the anchor as
        /// <c>newGameDay − Age0 · DAYS_PER_YEAR</c>, and a new world starts on day <b>0</b> — so for
        /// every player with a non-zero generated age the anchor is NEGATIVE. Held as a <c>uint</c> it
        /// had to be clamped to 0, which made the derived age <c>worldDay / 365</c>: the entire league
        /// became age 0 on the first daily step, the Decline band was unreachable and
        /// <c>RETIREMENT_AGE</c> could never fire. A player born before the epoch is an ordinary state,
        /// not an edge case, so the anchor must be able to represent it.
        /// </para>
        /// </summary>
        public long BirthWorldDay;

        /// <summary>Set on the world tick at RETIREMENT_AGE (FR-PG-013); the player stays selectable until the season boundary.</summary>
        public bool RetirementFlag;

        /// <summary>The world-day <see cref="RetirementFlag"/> was set (0 if not flagged).</summary>
        public uint RetirementDay;

        /// <summary>
        /// The last world day this player's daily step ran, or
        /// <see cref="PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL"/> when it never has.
        /// <para>
        /// <b>Load-bearing for idempotency, not bookkeeping.</b> #30's <c>RunCareerDaySteps</c> runs a
        /// fixture day's slots TWICE — once pre-round and once from the advance loop (ERR-030-027) — and
        /// documents that each subsystem's own per-player cursor is what makes the second call a no-op.
        /// Without this field #28's cursor would accrue twice on every fixture day, which is a silent
        /// ~11% growth-rate error rather than a crash. The sibling #29/#41 states carry the same field
        /// for the same reason.
        /// </para>
        /// <para>
        /// The sentinel is <c>uint.MaxValue</c>, not 0: day 0 is a legitimate world day, so a zero
        /// default would read as "already advanced on day 0" and silently skip a real first step — the
        /// day-0 trap #29's <c>TRAINING_NOT_ADVANCED_SENTINEL</c> exists to avoid.
        /// </para>
        /// </summary>
        public uint LastAdvancedWorldDay;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation.                                        |
// | 1.1     | 2026-08-08 | —      | #28 T1: + LastAdvancedWorldDay (sentinel uint.MaxValue). #30's |
// |         |            |        | RunCareerDaySteps runs a fixture day's slots twice             |
// |         |            |        | (ERR-030-027) and relies on each subsystem's own cursor for    |
// |         |            |        | idempotency; without it the growth cursor double-accrues on    |
// |         |            |        | every fixture day, silently. Serialized by ProgressionSaveCodec.|
#endregion
