// File:     src/testing-strategy/ClosedLoopScenario.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.3 / §4.4.1 / FR-TS-023 / FR-TS-030 / KD-7,
//           Deterministic Simulation #16 §3.2 / §4.8, Code Standards #20
// Purpose:  Standard IScenario implementation for closed-loop scenarios: a body drives
//           a real subsystem loop from manifest-described initial state for whole
//           simulated seconds and records emergent-behaviour predicates into the
//           envelope. This is the harness class motivated by the Ball Physics AR-7 /
//           Agent Movement AR-12 pattern — three consecutive specs where H-class
//           defects were encoded by pure-function unit suites rather than caught by
//           them; only a closed-loop run verifies the spec as composed.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Closed-loop scenario: (manifest, body) pair. Each <see cref="Run"/> invocation
    /// builds a fresh <see cref="ScenarioContext"/> — RNG seeded first (KD-7), then the
    /// body constructs all subsystem state itself (hermeticity, FR-TS-023) — and
    /// evaluates the envelope: zero recorded predicates is a failure ("implicit pass"
    /// forbidden, FR-TS-030), as is any failed predicate or a thrown body.
    /// Testing Strategy &amp; Framework #19 §3.3.3 / §4.4.1.
    /// </summary>
    public sealed class ClosedLoopScenario : IScenario
    {
        private readonly ScenarioManifest _manifest;
        private readonly ScenarioBody _body;

        public ClosedLoopScenario(ScenarioManifest manifest, ScenarioBody body)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _body     = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <inheritdoc />
        public ScenarioResult Run(ulong seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // KD-7: the RNG service is seeded with the verbatim run seed before any
            // subsystem is initialised (the body performs all subsystem construction).
            var rng      = new DeterministicRngService(seed);
            var envelope = new ScenarioEnvelope();
            var context  = new ScenarioContext(_manifest, seed, rng, envelope);

            string exceptionLine = null;
            try
            {
                _body(context);
            }
            catch (Exception ex)
            {
                exceptionLine = "exception=" + ex.GetType().Name + " message=" + ex.Message;
            }

            stopwatch.Stop();

            bool implicitPass = exceptionLine == null && envelope.PredicateCount == 0;
            bool passed = exceptionLine == null && !implicitPass && envelope.FailureCount == 0;

            var diagnostics = new StringBuilder(256);
            diagnostics.Append("scenario=").Append(_manifest.Name);
            diagnostics.Append("\nrun_seed=").Append(seed.ToString(CultureInfo.InvariantCulture));
            diagnostics.Append("\nmanifest_seed=").Append(_manifest.Seed.ToString(CultureInfo.InvariantCulture));
            diagnostics.Append("\npredicates_total=").Append(envelope.PredicateCount);
            diagnostics.Append("\npredicates_failed=").Append(envelope.FailureCount);
            if (exceptionLine != null)
            {
                diagnostics.Append('\n').Append(exceptionLine);
            }
            if (implicitPass)
            {
                diagnostics.Append("\nimplicit_pass_forbidden=FR-TS-030");
            }
            envelope.AppendFailureLines(diagnostics);

            // Duration overflow is unreachable for scenario-scale runs; clamp defensively
            // rather than wrapping negative through the int cast.
            long elapsedMs = stopwatch.ElapsedMilliseconds;
            int durationMs = elapsedMs > int.MaxValue ? int.MaxValue : (int)elapsedMs;

            return new ScenarioResult(
                passed ? ScenarioStatus.Passed : ScenarioStatus.Failed,
                diagnostics.ToString(),
                durationMs,
                EnvironmentFingerprint.CreateStage0Dev());
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
