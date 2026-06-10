// File:     src/testing-strategy/ScenarioRunner.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.3 / §3.3.4 / §3.3.6 / §4.3.3 /
//           FR-TS-023 / FR-TS-024 / FR-TS-028 / FR-TS-070, Code Standards #20
// Purpose:  Single scenario entry point per §3.3.3: Run(manifestPath, seed). Resolves
//           the manifest through the index (refusing entries missing from it,
//           FR-TS-028), performs load-time validation (manifest fields + format
//           version, FR-TS-070 — load-time errors throw, they are not runtime
//           assertions per §3.3.4), then delegates to the scenario.

using System;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Scenario runner per §3.3.3 / FR-TS-024. Hermetic: holds only the immutable
    /// index; every invocation re-initialises all subsystem state inside the scenario
    /// (FR-TS-023).
    ///
    /// <para><b>Stage 0 entry-point shape.</b> §3.3.3 writes the contract as
    /// <c>ScenarioRunner.Run(manifestPath, seed)</c> with the on-disk root manifest as
    /// input. The on-disk index encoding is pinned at Stage 0+1 (D1 in §7.5; §3.3.6
    /// Stage 0 deliverable is schema only), so at Stage 0 the index is injected here
    /// as a constructed value and <c>Run</c> keeps the contract signature. The
    /// Stage 0+1 deliverable adds a file-loading factory that parses
    /// <c>tests/scenarios/index.&lt;ext&gt;</c> into a <see cref="ScenarioIndex"/> once
    /// D1 pins the encoding — a parser swap, not a contract change.</para>
    /// Testing Strategy &amp; Framework #19 §3.3.3 / §4.3.3.
    /// </summary>
    public sealed class ScenarioRunner
    {
        private readonly ScenarioIndex _index;

        public ScenarioRunner(ScenarioIndex index)
        {
            _index = index ?? throw new ArgumentNullException(nameof(index));
        }

        /// <summary>
        /// Runs the scenario registered under <paramref name="manifestPath"/> with the
        /// verbatim <paramref name="seed"/> (KD-7). Load-time refusals throw
        /// <see cref="ArgumentException"/> per §3.3.4 (load-time error, not a runtime
        /// assertion): unknown manifest path (FR-TS-028), invalid manifest fields
        /// (§3.3.2 / A.1), or unsupported format version (FR-TS-070 — no silent
        /// migration).
        /// </summary>
        public ScenarioResult Run(string manifestPath, ulong seed)
        {
            if (string.IsNullOrEmpty(manifestPath))
            {
                throw new ArgumentException("Manifest path must be non-empty.", nameof(manifestPath));
            }

            if (!_index.TryResolve(manifestPath, out ScenarioIndexEntry entry))
            {
                throw new ArgumentException(
                    "Scenario '" + manifestPath + "' is missing from the index; the runner "
                        + "refuses to execute unindexed scenarios (§3.3.6 / FR-TS-028).",
                    nameof(manifestPath));
            }

            string fieldError = entry.Manifest.ValidateForLoad();
            if (fieldError != null)
            {
                throw new ArgumentException(
                    "Manifest '" + manifestPath + "' failed load-time validation (§3.3.2 / A.1): "
                        + fieldError,
                    nameof(manifestPath));
            }

            if (entry.Manifest.FormatVersion != TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION)
            {
                throw new ArgumentException(
                    "Manifest '" + manifestPath + "' carries format_version="
                        + entry.Manifest.FormatVersion + " but this runner supports only version "
                        + TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION
                        + " (FR-TS-070: unknown versions are rejected; no silent migration).",
                    nameof(manifestPath));
            }

            return entry.Scenario.Run(seed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
