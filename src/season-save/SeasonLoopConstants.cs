// File:     src/season-save/SeasonLoopConstants.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 Appendix A (constant catalogue); §3.2 (points);
//           §3.1.1 (permutation seed); Code Standards #20
// Purpose:  Constant catalogue for the season/competition loop (#30 Appendix A). Points values, the
//           season-state sub-blob format version, and the identity-permutation seed sentinel. The
//           [CROSS] determinism rows (DOMAIN_TAG_SEASON_LOOP / SUBSYSTEM_ORDINAL_SEASON_LOOP) are
//           DELIBERATELY ABSENT at T0 — ERR-030-001 is spec-text-first and pins the code const to
//           #30 T2's first draw site (registering a stream with zero draw sites is the phantom-surface
//           class FR-LW-031 forbids; the living-world `world.arcs` precedent).

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for the Season &amp; Competition Loop (#30 Appendix A).
    /// <para>
    /// Magnitudes are illustrative pending the Stage-2 balance pass (#30 Appendix A / the #21 §9.2
    /// precedent): the spec's contract is the shapes and directions, the <c>[GT]</c> numbers are tunable.
    /// </para>
    /// </summary>
    public static class SeasonLoopConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] The season-state sub-blob's own format version (#30 KD-1 / Appendix A). Distinct from
        /// the outer <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/> that frames it and from
        /// every version nested in the sibling world / match blobs. Gates the season block only; a
        /// mismatch fails loud on load (F3). Bump only on a season-block layout change.
        /// <para>
        /// Declared at T0 alongside the value types it will serialize; the codec that reads it
        /// (<c>SeasonStateCodec</c>) lands at #30 T1.
        /// </para>
        /// </summary>
        public const uint SEASON_STATE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] The seed value that selects the <b>identity</b> club-label permutation in
        /// <see cref="FixtureScheduler.Generate(int[], ulong)"/> (#30 §3.1.1 — "if the permutation is
        /// the identity (a documented Stage-0 option), the generator makes zero draws").
        /// <para>
        /// With this seed the generator is a pure fixed integer schedule with no permutation step, so
        /// FR-SN-027's "fixture generation needs no draw for the single-league case" holds exactly.
        /// </para>
        /// </summary>
        public const ulong IDENTITY_PERMUTATION_SEED = 0UL;
        #endregion

        #region GT
        /// <summary>[GT] Points awarded for a win (#30 Appendix A; the association-football 3/1/0
        /// convention, §8.2). A rules variant (e.g. 2/1/0) is a config change, not a code change —
        /// config key <c>[season-save] WinPoints</c>.</summary>
        public static readonly int WinPoints = Config.GetInt("season-save", "WinPoints", 3);

        /// <summary>[GT] Points awarded to each club for a draw (#30 Appendix A).
        /// Config key <c>[season-save] DrawPoints</c>.</summary>
        public static readonly int DrawPoints = Config.GetInt("season-save", "DrawPoints", 1);

        /// <summary>[GT] Points awarded for a loss (#30 Appendix A). Zero under the standard convention;
        /// named rather than implied so a rules variant has one place to change.
        /// Config key <c>[season-save] LossPoints</c>.</summary>
        public static readonly int LossPoints = Config.GetInt("season-save", "LossPoints", 0);

        /// <summary>
        /// [GT] The full scale for the board's job-security reading (#30 §2.2 / FR-SN-014). Job security
        /// is carried as an integer per-mille in <c>[0, JobSecurityScale]</c> rather than a float —
        /// see <see cref="BoardState"/> for why (the #41/#40/#33 integer-arithmetic convention).
        /// Config key <c>[season-save] JobSecurityScale</c>.
        /// </summary>
        public static readonly int JobSecurityScale = Config.GetInt("season-save", "JobSecurityScale", 1000);
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): Appendix A points + scale rows,   |
// |         |            |        | SEASON_STATE_FORMAT_VERSION, IDENTITY_PERMUTATION_SEED. The        |
// |         |            |        | [CROSS] determinism rows stay absent until #30 T2 (ERR-030-001).   |
// | 1.1     | 2026-07-25 | —      | AR pass 5: the four [GT] rows were `public const` ALL_CAPS under a |
// |         |            |        | `GameplayTuned` region — the pattern the June-30 FR-CS-019 pass    |
// |         |            |        | removed tree-wide (ALL_CAPS is [FIXED]-only; [GT] is static        |
// |         |            |        | readonly off GameplayConfig; the region is named `GT`). A const    |
// |         |            |        | cannot take Config.GetInt, so these four were structurally locked  |
// |         |            |        | out of a migration 17 other catalogues already completed.          |
#endregion
