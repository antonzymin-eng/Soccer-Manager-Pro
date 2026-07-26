// File:     src/season-save/RoundResolutionMode.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4.1 (FR-SN-013a/013b), KD-9; path-to-playable roadmap C1;
//           Code Standards #20
// Purpose:  The §3.4.1 dial: how a round's fixtures are resolved. The spec frames the choice as
//           "a SeasonState/config dial, not a rewrite" — this enum is that dial.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// How <see cref="SeasonLoop.AdvanceAndPlayNextRound"/> resolves the fixtures of a round
    /// (#30 §3.4.1). Every mode produces the same <see cref="MatchResult"/> shape and applies it to the
    /// table identically (FR-SN-012); only the producer differs.
    /// <para>
    /// ORDINAL STABILITY: these values are APPEND-only. The mode is not serialized in the season blob
    /// today (it is a per-session composition choice, like the <c>ISquadProvider</c>), but it selects
    /// which producer generates a persisted scoreline, so a reorder would silently change what a given
    /// caller means.
    /// </para>
    /// </summary>
    public enum RoundResolutionMode
    {
        /// <summary>
        /// The FR-SN-013b default: the <c>ManagedClubId</c>'s fixture runs through the full
        /// <c>MatchEngine</c> under the human's tactical influence; every other fixture resolves through
        /// <see cref="RoundResolutionModel"/>.
        /// <para>
        /// Cost: one real 90-minute match per round (roadmap C1 — ~2 minutes of compute), plus
        /// microseconds for the rest.
        /// </para>
        /// </summary>
        ManagedThroughEngine = 0,

        /// <summary>
        /// Every fixture — including the managed club's — resolves through
        /// <see cref="RoundResolutionModel"/>. This is the "quick-resolve the round" path a manager who
        /// does not want to watch their own match takes, and the mode season-scale tests use: a whole
        /// 38-round season resolves in microseconds instead of ~13 hours.
        /// </summary>
        QuickSimAll = 1,

        /// <summary>
        /// The §3.4.1 <b>minimal identity</b>: every fixture runs through the full <c>MatchEngine</c>.
        /// Correct and deterministic, and unusably slow — roadmap C1 measures a 20-club season at
        /// <b>≥ 16 hours</b> of compute. It exists as the verification mode the quick-sim is calibrated
        /// against (roadmap A4a), never as a default.
        /// </summary>
        FullEngine = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 T2): the §3.4.1 resolution dial.        |
#endregion
