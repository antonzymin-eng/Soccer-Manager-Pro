// File:     src/match-analytics/AdvancedStatline.cs
// Created:  2026-07-27
// Modified: 2026-07-27 (AR-2: copy-then-validate the heatmap)
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 / §3.4 (FR-AN-015, F4), Code Standards #20
// Purpose:  The immutable per-team advanced statline: territorial share, the positional heatmap bins,
//           and the xG total behind an explicit availability flag.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// One team's positional / model-layer statistics (§2.2).
    ///
    /// <para><b>Why xG hides behind a flag (KD-2 / F4).</b> <see cref="XgSum"/> is meaningless unless
    /// <see cref="LiveXgAvailable"/> is true, and at Stage 1 it is always false: scoring a shot needs its
    /// ORIGIN, and no producer supplies one (see <see cref="XgLocationModel"/>). A plain zero would be
    /// indistinguishable from a genuine "nobody threatened", so a screen binding this must branch on the
    /// flag and show "—" rather than "0.00". That is the whole reason the flag exists instead of the
    /// field simply being absent.</para>
    ///
    /// <para>The heatmap bins are copied at construction and exposed read-only, for the same reason the
    /// match view model copies its arrays: a <c>ReadOnlyCollection</c> over the producer's live buffer is
    /// a window, not a snapshot.</para>
    /// </summary>
    public readonly struct AdvancedStatline
    {
        // See MatchStatline for why this is stored as "has value" rather than "is unset" (AR-1 M-1).
        private readonly bool _hasValue;

        /// <summary>True when this is a zeroed struct that never went through the constructor.
        /// <see cref="MatchAnalyticsResult"/> refuses one.</summary>
        public bool IsUnset => !_hasValue;

        /// <summary>Team this line describes (0 = home, 1 = away).</summary>
        public readonly byte TeamId;

        /// <summary>Share of sampled ticks with the ball in this team's attacking half, percent in
        /// [0, 100] (§3.4). Deterministic: a fixed sample cadence and a strict-inequality half
        /// assignment, so the split is total — no double-count, no gap.</summary>
        public readonly float TerritorialPercent;

        /// <summary>True only when a real shot-origin producer fed the xG model. False at Stage 1
        /// (KD-2) — read <see cref="XgSum"/> only when this is true.</summary>
        public readonly bool LiveXgAvailable;

        /// <summary>Summed expected goals; 0 and meaningless while <see cref="LiveXgAvailable"/> is
        /// false.</summary>
        public readonly float XgSum;

        private readonly ReadOnlyCollection<int> _heatmapBins;

        /// <summary>
        /// Positional occupancy counts over the fixed <c>HEATMAP_COLS × HEATMAP_ROWS</c> grid, indexed
        /// <c>row * HEATMAP_COLS + col</c> (§3.4). Empty on a default-constructed value.
        /// </summary>
        public IReadOnlyList<int> HeatmapBins => _heatmapBins ?? EmptyBins;

        private static readonly ReadOnlyCollection<int> EmptyBins =
            new ReadOnlyCollection<int>(new int[0]);

        /// <summary>
        /// Constructs an advanced statline, copying the bins and gating every value (FR-AN-018).
        /// </summary>
        /// <param name="heatmapBins">Exactly <c>HEATMAP_BINS_PER_TEAM</c> non-negative counts.</param>
        public AdvancedStatline(
            byte teamId,
            float territorialPercent,
            bool liveXgAvailable,
            float xgSum,
            int[] heatmapBins)
        {
            if (teamId >= MatchAnalyticsConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(teamId), teamId, "teamId must be 0 or 1.");
            }
            if (float.IsNaN(territorialPercent) || float.IsInfinity(territorialPercent) ||
                territorialPercent < 0f || territorialPercent > 100f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(territorialPercent), territorialPercent,
                    "territorialPercent must be a finite value in [0, 100].");
            }
            if (float.IsNaN(xgSum) || float.IsInfinity(xgSum) || xgSum < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(xgSum), xgSum, "xgSum must be finite and non-negative.");
            }

            // F4 — an xG total with no producer behind it must not be presentable. Refusing the
            // incoherent combination here means no screen has to defend against it.
            if (!liveXgAvailable && xgSum != 0f)
            {
                throw new ArgumentException(
                    "xgSum must be 0 while LiveXgAvailable is false — a non-zero total with no shot-origin " +
                    "producer behind it would render as a real number (F4).", nameof(xgSum));
            }

            if (heatmapBins == null)
            {
                throw new ArgumentNullException(nameof(heatmapBins));
            }
            if (heatmapBins.Length != MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM)
            {
                throw new ArgumentException(
                    "heatmapBins carries " + heatmapBins.Length + " cells; expected " +
                    MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM + " (HEATMAP_COLS × HEATMAP_ROWS).",
                    nameof(heatmapBins));
            }

            // Copy FIRST, then validate the copy. Reading the caller's array twice — once to check and
            // once to copy — leaves the gate and the stored value looking at different reads, so a value
            // that changed in between is validated and then not stored (or the reverse). The project has
            // been here before at the same seam class (MatchReplay AR-3 M-1: validating a live handle
            // instead of a snapshot), and one read is both stricter and cheaper.
            var copy = new int[heatmapBins.Length];
            Array.Copy(heatmapBins, copy, heatmapBins.Length);
            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i] < 0)
                {
                    throw new ArgumentException(
                        "heatmapBins[" + i + "] is negative — occupancy counts cannot be.", nameof(heatmapBins));
                }
            }

            _hasValue = true;
            TeamId = teamId;
            TerritorialPercent = territorialPercent;
            LiveXgAvailable = liveXgAvailable;
            XgSum = xgSum;
            _heatmapBins = new ReadOnlyCollection<int>(copy);
        }

        /// <summary>An empty grid, sized for one team — what an aggregator starts from.</summary>
        public static int[] NewHeatmapGrid() => new int[MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM];
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.2     | 2026-07-27 | —      | AR-2: the heatmap is copied first and the COPY validated. The  |
// |         |            |        | previous form read each caller element twice — once to check,  |
// |         |            |        | once to store — so the gate and the stored value looked at     |
// |         |            |        | different reads (the MatchReplay AR-3 M-1 seam class).         |
// | 1.1     | 2026-07-27 | —      | AR-1 M-1: _hasValue discriminator (see MatchStatline v1.1).    |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): territorial share, copied heatmap   |
// |         |            |        | bins, and the F4 xG-availability gate that refuses a non-zero  |
// |         |            |        | total with no producer behind it.                              |
#endregion
