// File:     src/testing-strategy/ScenarioIndexEntry.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.6 / §4.4.1 / FR-TS-028, Code Standards #20
// Purpose:  One scenario-index row: logical manifest path + manifest + executable
//           scenario. Extracted from ScenarioIndex.cs in AR-1 L-4 (FILE NAMING
//           precedent: BallCollision AR-2 L-2, TraceChannel AR-1 H-1).

using System;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One index row: logical manifest path + manifest + executable scenario. The
    /// manifest travels here rather than on <see cref="IScenario"/> so the §4.4.1
    /// interface keeps its single-method contract.
    ///
    /// <para><b>Manifest coherence (AR-1 M-1).</b> The runner validates
    /// <see cref="Manifest"/> at load time, but a <see cref="ClosedLoopScenario"/>
    /// executes — and reports diagnostics from — the manifest it was constructed
    /// with. To prevent a mis-wired registration from passing validation against one
    /// manifest and running under another, the constructor rejects a
    /// <see cref="ClosedLoopScenario"/> whose manifest is not the same instance as
    /// <paramref name="manifest"/>. Other <see cref="IScenario"/> implementations
    /// carry no readable manifest and accept this coherence obligation by contract.</para>
    /// Testing Strategy &amp; Framework #19 §3.3.6 / FR-TS-028.
    /// </summary>
    public sealed class ScenarioIndexEntry
    {
        /// <summary>Logical manifest path per §3.3.5 layout, e.g.
        /// "tests/scenarios/agent-movement/launch-from-rest". Extension-free at Stage 0
        /// (on-disk encoding pinned at Stage 0+1, D1).</summary>
        public string ManifestPath { get; }

        /// <summary>Manifest entry per Appendix A.1.</summary>
        public ScenarioManifest Manifest { get; }

        /// <summary>Executable scenario registered under this entry.</summary>
        public IScenario Scenario { get; }

        public ScenarioIndexEntry(string manifestPath, ScenarioManifest manifest, IScenario scenario)
        {
            if (string.IsNullOrEmpty(manifestPath))
            {
                throw new ArgumentException("Manifest path must be non-empty.", nameof(manifestPath));
            }

            ManifestPath = manifestPath;
            Manifest     = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Scenario     = scenario ?? throw new ArgumentNullException(nameof(scenario));

            if (scenario is ClosedLoopScenario closedLoop
                && !ReferenceEquals(closedLoop.Manifest, manifest))
            {
                throw new ArgumentException(
                    "ClosedLoopScenario for '" + manifestPath + "' was constructed with a "
                        + "different manifest instance than the one registered on this entry; "
                        + "load-time validation would run against a manifest the scenario does "
                        + "not execute (AR-1 M-1).",
                    nameof(scenario));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Extracted from ScenarioIndex.cs (AR-1 L-4); AR-1 M-1 manifest-     |
// |         |            |        | coherence guard added (ClosedLoopScenario must be registered under |
// |         |            |        | the same manifest instance it executes).                           |
#endregion
