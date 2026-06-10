// File:     src/testing-strategy/ScenarioIndex.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.5 / §3.3.6 / FR-TS-027 / FR-TS-028,
//           Code Standards #20
// Purpose:  In-memory root manifest (index). The runner refuses to execute a scenario
//           whose entry is missing from the index (§3.3.6). Stage 0: the index is
//           built in code by per-spec scenario corpora because the on-disk
//           tests/scenarios/index.<ext> encoding is pinned at Stage 0+1 (D1 in §7.5);
//           manifest paths follow the §3.3.5 layout (tests/scenarios/<owning-spec>/<name>)
//           so the Stage 0+1 file loader is a parser swap, not a remodel.

using System;
using System.Collections.Generic;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One index row: logical manifest path + manifest + executable scenario. The
    /// manifest travels here rather than on <see cref="IScenario"/> so the §4.4.1
    /// interface keeps its single-method contract.
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
        }
    }

    /// <summary>
    /// Immutable scenario index per §3.3.6 (FR-TS-028). Duplicate manifest paths are
    /// rejected at construction (A.1: name unique within manifest); lookup is ordinal.
    /// Immutability keeps <see cref="ScenarioRunner"/> free of mutable global state
    /// (hermeticity per §3.3.3).
    /// Testing Strategy &amp; Framework #19 §3.3.6 / FR-TS-028.
    /// </summary>
    public sealed class ScenarioIndex
    {
        private readonly Dictionary<string, ScenarioIndexEntry> _entries;

        /// <summary>Number of registered scenarios.</summary>
        public int Count => _entries.Count;

        public ScenarioIndex(ScenarioIndexEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries = new Dictionary<string, ScenarioIndexEntry>(entries.Length, StringComparer.Ordinal);
            for (int i = 0; i < entries.Length; i++)
            {
                ScenarioIndexEntry entry = entries[i];
                if (entry == null)
                {
                    throw new ArgumentException("entries[" + i + "] is null.", nameof(entries));
                }
                if (_entries.ContainsKey(entry.ManifestPath))
                {
                    throw new ArgumentException(
                        "Duplicate manifest path '" + entry.ManifestPath
                            + "' (A.1: name unique within manifest).",
                        nameof(entries));
                }
                _entries.Add(entry.ManifestPath, entry);
            }
        }

        /// <summary>Resolves a manifest path; false when the entry is missing from the index.</summary>
        public bool TryResolve(string manifestPath, out ScenarioIndexEntry entry)
        {
            return _entries.TryGetValue(manifestPath, out entry);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
