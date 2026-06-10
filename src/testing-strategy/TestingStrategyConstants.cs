// File:     src/testing-strategy/TestingStrategyConstants.cs
// Created:  2026-06-02
// Modified: 2026-06-10
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

        /// <summary>
        /// [FIXED] Scenario-manifest schema version accepted by <see cref="ScenarioRunner"/>
        /// (§3.3.2 format_version / §3.3.4 / FR-TS-070). A schema-protocol version, not a
        /// tunable: bumps are paired with a migration script per §3.8.3 and the validator
        /// rejects unknown versions — no silent migration.
        /// </summary>
        public const int SCENARIO_MANIFEST_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] §3.3.5 directory-layout prefix for cross-spec scenarios (owned by
        /// Spec #19 per KD-8). Manifest paths under this prefix MUST declare ≥ 2
        /// owning specs (A.1); enforced by <see cref="ScenarioRunner"/> at load time.
        /// A layout constant, not a tunable.
        /// </summary>
        public const string SCENARIO_PATH_CROSS_SPEC_PREFIX = "tests/scenarios/cross-spec/";
        #endregion

        #region GT
        /// <summary>
        /// [GT] Unit-tier minimum fraction of total test count (0.60 = 60%).
        /// Floor in the pyramid contract per §3.1.2. Stage-gated per KD-5 (activates at
        /// Stage 0+1); revisited Stage 1 against actual code.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float UnitPyramidFloorFraction = 0.60f;

        /// <summary>
        /// [GT] Integration-tier maximum fraction of total test count (0.25 = 25%).
        /// Ceiling in the pyramid contract per §3.1.2. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float IntegrationPyramidCeilingFraction = 0.25f;

        /// <summary>
        /// [GT] Simulation-tier maximum fraction of total test count (0.12 = 12%).
        /// Ceiling in the pyramid contract per §3.1.2. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float SimulationPyramidCeilingFraction = 0.12f;

        /// <summary>
        /// [GT] End-to-end / soak-tier maximum fraction of total test count (0.03 = 3%).
        /// Ceiling in the pyramid contract per §3.1.2. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float EndToEndPyramidCeilingFraction = 0.03f;

        /// <summary>
        /// [GT] Tier A authoritative-hard line coverage minimum (0.98 = 98%).
        /// Per KD-9 / §3.6.2 / FR-TS-053. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierALineCoverageMin = 0.98f;

        /// <summary>
        /// [GT] Tier A authoritative-hard branch coverage minimum (0.95 = 95%).
        /// Per KD-9 / §3.6.2 / FR-TS-053. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierABranchCoverageMin = 0.95f;

        /// <summary>
        /// [GT] Tier B bounded-authoritative line coverage minimum (0.90 = 90%).
        /// Per KD-9 / §3.6.2 / FR-TS-054. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierBLineCoverageMin = 0.90f;

        /// <summary>
        /// [GT] Tier B bounded-authoritative branch coverage minimum (0.80 = 80%).
        /// Per KD-9 / §3.6.2 / FR-TS-054. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float TierBBranchCoverageMin = 0.80f;

        /// <summary>
        /// [GT] Unit-tier wall-time budget in milliseconds (1 ms).
        /// Sub-millisecond fast-feedback bound per §3.1.1 / FR-TS-002 / §3.10. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float UnitWallTimeBoundMs = 1.0f;

        /// <summary>
        /// [GT] Pre-commit pipeline wall-time budget in seconds (60 s).
        /// Local-feedback budget per §4.5.1 / §3.10. Stage-gated per KD-5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float PreCommitWallTimeBoundSeconds = 60.0f;

        /// <summary>
        /// [GT] Quarantine auto-expiry window in days (14 days).
        /// After this window, the test MUST be fixed or deleted; permanent quarantine forbidden.
        /// §3.7.3 / FR-TS-063 / FR-TS-064. Stage-gated per KD-5 (§3.7 preamble).
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int QuarantineExpiryDays = 14;

        /// <summary>
        /// [GT] Eviction quarantine-count threshold (3 strikes).
        /// A test quarantined this many times in <see cref="EvictionWindowDays"/> is deleted.
        /// §3.7.4 / FR-TS-065. Stage-gated per KD-5 (§3.7 preamble).
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int EvictionQuarantineCount = 3;

        /// <summary>
        /// [GT] Eviction observation window in days (90 days = one calendar quarter).
        /// §3.7.4 / FR-TS-065. Stage-gated per KD-5 (§3.7 preamble).
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int EvictionWindowDays = 90;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 L-5: KD-5 "Stage-gated" annotation added to pyramid bound +   |
// |         |            |        | per-tier coverage XML docs (KD-5 governs activation; existing      |
// |         |            |        | KD-9 cite covers tier policy). Documentation-only change.          |
// | 1.2     | 2026-06-02 | —      | AR-2 L-4: KD-5 "Stage-gated" annotation extended to the remaining |
// |         |            |        | GT rows skipped by AR-1 L-5 — UnitWallTimeBoundMs, PreCommitWall- |
// |         |            |        | TimeBoundSeconds, and the §3.7 quarantine + eviction triple       |
// |         |            |        | (which §3.7 preamble explicitly tags Stage-gated per KD-5).        |
// | 1.3     | 2026-06-10 | —      | SCENARIO_MANIFEST_FORMAT_VERSION = 1 added for the Stage 0        |
// |         |            |        | ScenarioRunner (§3.3.2 / FR-TS-070 unknown-version rejection).     |
// | 1.4     | 2026-06-10 | —      | AR-1 M-4: SCENARIO_PATH_CROSS_SPEC_PREFIX [FIXED] added (§3.3.5   |
// |         |            |        | layout; backs the cross-spec ≥2 owning-spec arity check).          |
#endregion
