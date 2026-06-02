// File:     src/testing-strategy/TestingStrategyConstants.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.10, Code Standards #20
// Purpose:  Constant catalogue for Spec #19 governance constants (pyramid bounds,
//           coverage thresholds, flake-quarantine windows, pre-commit budget).
//           Spec #19 declares no physical constants; all values are governance
//           per §3.10. Game-layer assemblies MUST NOT import this assembly.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Constant catalogue for Testing Strategy &amp; Framework #19.
    /// All constants are infrastructure governance values per §3.10.
    /// Testing Strategy &amp; Framework #19 §3.10.
    /// </summary>
    public static class TestingStrategyConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] End-to-end / soak match length in minutes (Laws of football).
        /// Not designer-tunable. §3.1.1 / §3.10 / FR-TS-006.
        /// </summary>
        public const int MATCH_LENGTH_MINUTES = 90;
        #endregion

        #region GT
        /// <summary>
        /// [GT] Unit-tier minimum fraction of total test count (0.60 = 60%).
        /// Floor in the pyramid contract per §3.1.2. Revisited Stage 1 against actual code.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float UnitPyramidFloorFraction = 0.60f;

        /// <summary>
        /// [GT] Integration-tier maximum fraction of total test count (0.25 = 25%).
        /// Ceiling in the pyramid contract per §3.1.2.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float IntegrationPyramidCeilingFraction = 0.25f;

        /// <summary>
        /// [GT] Simulation-tier maximum fraction of total test count (0.12 = 12%).
        /// Ceiling in the pyramid contract per §3.1.2.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float SimulationPyramidCeilingFraction = 0.12f;

        /// <summary>
        /// [GT] End-to-end / soak-tier maximum fraction of total test count (0.03 = 3%).
        /// Ceiling in the pyramid contract per §3.1.2.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float EndToEndPyramidCeilingFraction = 0.03f;

        /// <summary>
        /// [GT] Tier A authoritative-hard line coverage minimum (0.98 = 98%).
        /// Per KD-9 / §3.6.2 / FR-TS-053.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierALineCoverageMin = 0.98f;

        /// <summary>
        /// [GT] Tier A authoritative-hard branch coverage minimum (0.95 = 95%).
        /// Per KD-9 / §3.6.2 / FR-TS-053.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierABranchCoverageMin = 0.95f;

        /// <summary>
        /// [GT] Tier B bounded-authoritative line coverage minimum (0.90 = 90%).
        /// Per KD-9 / §3.6.2 / FR-TS-054.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierBLineCoverageMin = 0.90f;

        /// <summary>
        /// [GT] Tier B bounded-authoritative branch coverage minimum (0.80 = 80%).
        /// Per KD-9 / §3.6.2 / FR-TS-054.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierBBranchCoverageMin = 0.80f;

        /// <summary>
        /// [GT] Unit-tier wall-time budget in milliseconds (1 ms).
        /// Sub-millisecond fast-feedback bound per §3.1.1 / FR-TS-002 / §3.10.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float UnitWallTimeBoundMs = 1.0f;

        /// <summary>
        /// [GT] Pre-commit pipeline wall-time budget in seconds (60 s).
        /// Local-feedback budget per §4.5.1 / §3.10.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float PreCommitWallTimeBoundSeconds = 60.0f;

        /// <summary>
        /// [GT] Quarantine auto-expiry window in days (14 days).
        /// After this window, the test MUST be fixed or deleted; permanent quarantine forbidden.
        /// §3.7.3 / FR-TS-063 / FR-TS-064.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int QuarantineExpiryDays = 14;

        /// <summary>
        /// [GT] Eviction quarantine-count threshold (3 strikes).
        /// A test quarantined this many times in <see cref="EvictionWindowDays"/> is deleted.
        /// §3.7.4 / FR-TS-065.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int EvictionQuarantineCount = 3;

        /// <summary>
        /// [GT] Eviction observation window in days (90 days = one calendar quarter).
        /// §3.7.4 / FR-TS-065.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int EvictionWindowDays = 90;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
