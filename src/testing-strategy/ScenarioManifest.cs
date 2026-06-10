// File:     src/testing-strategy/ScenarioManifest.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.2 / Appendix A.1 / FR-TS-025 / FR-TS-070,
//           Code Standards #20
// Purpose:  In-memory representation of one Appendix A.1 scenario-manifest entry.
//           Stage 0: manifests are authored in code because the on-disk extension and
//           encoding of index.<ext> are pinned at Stage 0+1 (D1 in §7.5); the field
//           set below mirrors A.1 so the Stage 0+1 file loader is a parser swap, not
//           a remodel. The expected_outcome_envelope is expressed as executable
//           predicate checks recorded into ScenarioEnvelope by the scenario body.

using System;
using System.Collections.Generic;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One scenario-manifest entry per §3.3.2 / Appendix A.1. Immutable; input arrays
    /// are defensively copied.
    ///
    /// <para><b>Stage 0 field coverage.</b> <c>name</c>, <c>owning_spec_ids</c>,
    /// <c>seed</c>, <c>tier_classification</c>, <c>fixture_refs</c>, and
    /// <c>format_version</c> are carried verbatim. <c>expected_outcome_envelope</c>
    /// is executable (the scenario body records predicate outcomes into
    /// <see cref="ScenarioEnvelope"/>; FR-TS-030 zero-predicate refusal is enforced by
    /// the runner). <c>provenance_edges</c> / <c>metadata</c> are optional per A.1 and
    /// deferred until fixture capture exists (§3.4.3, Stage 0+1).</para>
    /// Testing Strategy &amp; Framework #19 §3.3.2 / Appendix A.1.
    /// </summary>
    public sealed class ScenarioManifest
    {
        private readonly int[] _owningSpecIds;
        private readonly string[] _fixtureRefs;

        /// <summary>Scenario name; kebab-case, unique within the index (A.1).</summary>
        public string Name { get; }

        /// <summary>Owning spec numbers; ≥ 1 entry (per-spec) or ≥ 2 (cross-spec) per A.1.</summary>
        public IReadOnlyList<int> OwningSpecIds => _owningSpecIds;

        /// <summary>Canonical recorded seed, verbatim per FR-TS-025. The runner's caller-supplied
        /// seed is authoritative for a given run (enables seed sweeps); this field records the
        /// scenario's canonical seed and both are emitted in diagnostics.</summary>
        public ulong Seed { get; }

        /// <summary>Tier classification per #16 §1.1.1 (FR-TS-029).</summary>
        public TestTier TierClassification { get; }

        /// <summary>Fixture paths under tests/data/fixtures/. Stage 0: empty when the initial
        /// state is constructed in code; populated once #16 §3.2.4.1 fixture files land (KD-10).</summary>
        public IReadOnlyList<string> FixtureRefs => _fixtureRefs;

        /// <summary>Manifest schema version, validated by the runner per §3.3.4 / FR-TS-070.</summary>
        public int FormatVersion { get; }

        public ScenarioManifest(
            string name,
            int[] owningSpecIds,
            ulong seed,
            TestTier tierClassification,
            string[] fixtureRefs,
            int formatVersion)
        {
            Name               = name ?? throw new ArgumentNullException(nameof(name));
            Seed               = seed;
            TierClassification = tierClassification;
            FormatVersion      = formatVersion;

            if (owningSpecIds == null)
            {
                throw new ArgumentNullException(nameof(owningSpecIds));
            }
            if (fixtureRefs == null)
            {
                throw new ArgumentNullException(nameof(fixtureRefs));
            }

            _owningSpecIds = (int[])owningSpecIds.Clone();
            _fixtureRefs   = (string[])fixtureRefs.Clone();
        }

        /// <summary>
        /// Validates the load-time invariants from §3.3.2 / A.1. Returns the first
        /// violation as a human-readable message, or null when the manifest is valid.
        /// Format-version equality against the supported schema version is checked by
        /// <see cref="ScenarioRunner"/> (FR-TS-070), not here — this method validates
        /// fields whose constraints are version-independent.
        /// </summary>
        internal string ValidateForLoad()
        {
            if (!IsKebabCase(Name))
            {
                return "name '" + Name + "' is not kebab-case (A.1: lowercase a-z / 0-9 / single hyphens).";
            }
            if (_owningSpecIds.Length < 1)
            {
                return "owning_spec_ids must contain at least one spec number (A.1).";
            }
            for (int i = 0; i < _owningSpecIds.Length; i++)
            {
                if (_owningSpecIds[i] <= 0)
                {
                    return "owning_spec_ids[" + i + "]=" + _owningSpecIds[i] + " is not a valid spec number.";
                }
            }
            for (int i = 0; i < _fixtureRefs.Length; i++)
            {
                if (string.IsNullOrEmpty(_fixtureRefs[i]))
                {
                    return "fixture_refs[" + i + "] is null or empty.";
                }
            }
            if (FormatVersion <= 0)
            {
                return "format_version=" + FormatVersion + " must be a positive integer (A.1).";
            }
            return null;
        }

        private static bool IsKebabCase(string value)
        {
            if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
            {
                return false;
            }
            bool previousWasHyphen = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '-')
                {
                    if (previousWasHyphen)
                    {
                        return false;
                    }
                    previousWasHyphen = true;
                    continue;
                }
                previousWasHyphen = false;
                bool isLowerAlphaNumeric = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (!isLowerAlphaNumeric)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
