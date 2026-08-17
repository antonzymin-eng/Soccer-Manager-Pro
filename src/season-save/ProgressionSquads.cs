// File:     src/season-save/ProgressionSquads.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 KD-4 / FR-PG-022 (the career-state roster is #28's, and
//           #28 is its single over-time writer); Season & Competition Loop #30 §3.3; Code Standards #20
// Purpose:  The ISquadProvider projection over a ProgressionEngine — the adapter that makes #28's block
//           the one roster authority every consumer reads through. Lives here, not in
//           player-progression, because ISquadProvider is a match-engine type and #28 §4.1 forbids #28
//           referencing the match engine.

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Presents a <see cref="ProgressionEngine"/>'s career state as an <see cref="ISquadProvider"/>.
    /// <para>
    /// <b>This adapter is what makes KD-4 true rather than merely stated.</b> Before it, the only
    /// provider was the bootstrap product (<see cref="League"/>), whose squads are generated from the
    /// world seed and never change; #28 could evolve attributes all it liked and every consumer —
    /// <c>SquadRating</c>, <c>LineupSelector</c>, <c>MatchEngine.ConfigureSquads</c> — would keep
    /// reading day-0 values. Routing the provider through #28 means the roster a consumer sees IS the
    /// roster #28 has advanced, at every moment rather than at save/load boundaries.
    /// </para>
    /// <para>
    /// <b>Read-only by construction.</b> <see cref="ProgressionEngine.SquadFor"/> builds a fresh
    /// <see cref="Squad"/> from copies on every call, so nothing a caller does to the returned squad can
    /// reach the store — the single-writer rule (FR-PG-022) survives the adapter.
    /// </para>
    /// <para>
    /// <b>Not cached.</b> A cache would have to be invalidated on every daily step, and a stale entry
    /// here is precisely the defect this type exists to remove. Squad projection is a per-call array
    /// copy on the world tick, never on the 60 Hz path.
    /// </para>
    /// </summary>
    public sealed class ProgressionSquads : ISquadProvider
    {
        private readonly ProgressionEngine _progression;

        /// <summary>Wraps a career store as a squad provider.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="progression"/> is null.</exception>
        public ProgressionSquads(ProgressionEngine progression)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
        }

        /// <summary>
        /// The club's current roster, or <c>null</c> when the career carries no such club (the
        /// <see cref="ISquadProvider"/> contract — the caller decides whether a miss is fatal).
        /// </summary>
        public Squad ResolveByClubId(int clubId) => _progression.SquadFor(clubId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-08 | —      | #28 T2a: the projection that moves roster authority off the      |
// |         |            |        | world seed and onto #28's serialized block (KD-4).               |
#endregion
