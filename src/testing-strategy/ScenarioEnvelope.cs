// File:     src/testing-strategy/ScenarioEnvelope.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.2 / Appendix A.1 / FR-TS-030,
//           §4.3.2 (AssertEnvelope), Code Standards #20
// Purpose:  Executable expected_outcome_envelope. Scenario bodies record bounded
//           predicate outcomes here; the scenario fails if any predicate fails OR if
//           zero predicates were recorded ("implicit pass" is forbidden, FR-TS-030).
//           Check methods mirror the A.1 predicate ops (equals / in_range / boolean).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Recorder for the bounded predicate set of one scenario run (§3.3.2 / A.1).
    /// Not thread-safe; one instance per <see cref="ScenarioRunner"/> invocation
    /// (hermeticity per §3.3.3).
    /// Testing Strategy &amp; Framework #19 §3.3.2 / FR-TS-030 / §4.3.2.
    /// </summary>
    public sealed class ScenarioEnvelope
    {
        private readonly List<string> _failureLines = new List<string>();
        private int _predicateCount;

        /// <summary>Total predicates recorded so far. Zero at body completion ⇒ Failed (FR-TS-030).</summary>
        public int PredicateCount => _predicateCount;

        /// <summary>Failed predicates recorded so far.</summary>
        public int FailureCount => _failureLines.Count;

        /// <summary>
        /// Records a boolean predicate. <paramref name="detail"/> is emitted into
        /// diagnostics on failure (actual values, units); pass a value summary, not prose.
        /// </summary>
        public void CheckTrue(string predicateId, bool passed, string detail)
        {
            RecordOutcome(predicateId, passed, "expected=true " + (detail ?? string.Empty));
        }

        /// <summary>Records an A.1 "equals" predicate over integral values (enum ordinals, counts).</summary>
        public void CheckEquals(string predicateId, long actual, long expected)
        {
            RecordOutcome(
                predicateId,
                actual == expected,
                "op=equals expected=" + expected.ToString(CultureInfo.InvariantCulture)
                    + " actual=" + actual.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Records an A.1 "in_range" predicate (both bounds inclusive). NaN values fail
        /// the predicate — boundary saturation MUST NOT produce a silent pass (§3.4.4).
        /// </summary>
        public void CheckInRange(string predicateId, float value, float minInclusive, float maxInclusive)
        {
            // AR-2 L-1: a NaN bound is a harness-authoring error, not a scenario
            // outcome — NaN comparisons are false, so it would slip past the min>max
            // guard and masquerade as a failing predicate.
            if (float.IsNaN(minInclusive) || float.IsNaN(maxInclusive))
            {
                throw new ArgumentException(
                    "in_range bounds must not be NaN for predicate '" + predicateId + "'.",
                    nameof(minInclusive));
            }
            if (minInclusive > maxInclusive)
            {
                throw new ArgumentException(
                    "minInclusive=" + minInclusive.ToString(CultureInfo.InvariantCulture)
                        + " exceeds maxInclusive=" + maxInclusive.ToString(CultureInfo.InvariantCulture)
                        + " for predicate '" + predicateId + "'.",
                    nameof(minInclusive));
            }

            // NaN comparisons are false, so a NaN value falls through to a recorded failure.
            bool passed = value >= minInclusive && value <= maxInclusive;
            RecordOutcome(
                predicateId,
                passed,
                "op=in_range min=" + minInclusive.ToString(CultureInfo.InvariantCulture)
                    + " max=" + maxInclusive.ToString(CultureInfo.InvariantCulture)
                    + " actual=" + value.ToString(CultureInfo.InvariantCulture));
        }

        private void RecordOutcome(string predicateId, bool passed, string detail)
        {
            if (string.IsNullOrEmpty(predicateId))
            {
                throw new ArgumentException("Predicate ID must be non-empty.", nameof(predicateId));
            }

            _predicateCount++;
            if (!passed)
            {
                _failureLines.Add(
                    "failed predicate=" + SanitizeValue(predicateId) + " " + SanitizeValue(detail));
            }
        }

        /// <summary>
        /// Flattens CR/LF out of a diagnostics value. The §3.3.3 diagnostics contract
        /// is line-oriented key=value; an unescaped newline in a predicate detail or
        /// exception message would corrupt the encoding for every downstream parser
        /// (AR-1 M-3).
        /// </summary>
        internal static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            if (value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0)
            {
                return value;
            }
            return value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }

        /// <summary>Appends one diagnostics line per failed predicate, in recording order.</summary>
        internal void AppendFailureLines(StringBuilder builder)
        {
            for (int i = 0; i < _failureLines.Count; i++)
            {
                builder.Append('\n').Append(_failureLines[i]);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-10 | —      | AR-1 M-3: SanitizeValue flattens CR/LF in predicate IDs + details  |
// |         |            |        | before they enter the line-oriented key=value diagnostics.         |
// | 1.2     | 2026-06-10 | —      | AR-2: L-1 NaN in_range bounds now throw (authoring error, not a    |
// |         |            |        | scenario outcome); L-2 min>max exception message switched to       |
// |         |            |        | InvariantCulture float formatting.                                 |
#endregion
