// File:     src/testing-strategy/ScenarioResult.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.3 / FR-TS-024,
//           Deterministic Simulation #16 §4.8 (EnvironmentFingerprint), Code Standards #20
// Purpose:  Structured value returned by ScenarioRunner.Run per the §3.3.3 contract:
//           { status, diagnostics: MachineReadable, durationMs: int,
//             fingerprint: EnvironmentFingerprint }.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Result of one scenario invocation per §3.3.3. Immutable.
    ///
    /// <para><b>Diagnostics encoding (Stage 0 provisional).</b> Newline-separated
    /// <c>key=value</c> lines (e.g. <c>scenario=launch-from-rest</c>,
    /// <c>predicates_failed=1</c>, one <c>failed ...</c> line per failed predicate).
    /// Values are CR/LF-sanitized before emission (AR-1 M-3). A <c>Failed</c> status
    /// with <c>predicates_failed=0</c> carries its cause on a dedicated line:
    /// <c>exception=...</c> (+ <c>exception_stack=...</c>) for a thrown body, or
    /// <c>implicit_pass_forbidden=FR-TS-030</c> for a zero-predicate body.
    /// The final machine-readable encoding is pinned at Stage 0+1 alongside the D1
    /// manifest-encoding pin (§7.5); consumers MUST NOT parse beyond line-oriented
    /// <c>key=value</c> until then.</para>
    /// Testing Strategy &amp; Framework #19 §3.3.3 / FR-TS-024.
    /// </summary>
    public sealed class ScenarioResult
    {
        /// <summary>Outcome status per §3.3.3.</summary>
        public ScenarioStatus Status { get; }

        /// <summary>Machine-readable diagnostics; never null (empty string when nothing to report).</summary>
        public string Diagnostics { get; }

        /// <summary>Wall-time of the scenario body + envelope evaluation in milliseconds.
        /// Diagnostic only — never an assertion input (FR-TS-019 wall-clock ban).</summary>
        public int DurationMs { get; }

        /// <summary>Environment fingerprint of the executing host per #16 §4.8; never null.</summary>
        public EnvironmentFingerprint Fingerprint { get; }

        public ScenarioResult(
            ScenarioStatus status,
            string diagnostics,
            int durationMs,
            EnvironmentFingerprint fingerprint)
        {
            if (durationMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be non-negative.");
            }

            Status      = status;
            Diagnostics = diagnostics ?? string.Empty;
            DurationMs  = durationMs;
            Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-10 | —      | AR-1 M-3 doc: diagnostics values are CR/LF-sanitized; Failed with  |
// |         |            |        | predicates_failed=0 documented as exception / implicit-pass with   |
// |         |            |        | dedicated cause lines. Documentation-only change.                  |
#endregion
