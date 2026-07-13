// File:     src/performance-optimization/StopwatchPerfHarness.cs
// Created:  2026-07-13
// Modified: 2026-07-13
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.5 (manual Stopwatch-based capture) /
//           §3.3.2 (session manifest) / §4.3.1 / §4.4, Code Standards #20
// Purpose:  The concrete IPerfHarness — a manual, Stopwatch-driven per-tick sample harness
//           (§3.3.5). Callers time each tick/frame themselves and feed the elapsed milliseconds
//           in via RecordTickSample; FinalizeSession computes the p50/p99 percentiles over the
//           accumulated corpus and projects an immutable BaselineRecord. This is the producer the
//           IPerfHarness interface was written against (the interface names it "Spec #18 §3.3
//           harness"); its single concrete implementation per §4.3.1 (no IoC container).
//
//           PERCENTILE METHOD: Spec #18 pins no computation rule (only a [GT] rolling-window
//           size). This harness pins NEAREST-RANK on the sorted corpus — rank(p) = ceil(p/100·N),
//           clamped to [1, N], value = sorted[rank-1] — computed with pure integer arithmetic so
//           the result is deterministic and free of float percentile interpolation / platform
//           rounding. Because rank is monotonic in p, p99 >= p50 always holds by construction.
//
//           NOTE: a number produced on the Linux compile/test gate is NON-certifying
//           (certification-platform.md). The certified per-tick baseline still requires a run on
//           the pinned host (cert-run-runbook.md Step 2); this class is the harness both paths use.

using System;
using System.Collections.Generic;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Manual Stopwatch-based <see cref="IPerfHarness"/> (Spec #18 §3.3.5). Accumulates per-tick
    /// wall-time samples between <see cref="BeginSession"/> and <see cref="FinalizeSession"/>, then
    /// computes nearest-rank p50/p99 and returns a <see cref="BaselineRecord"/>. Single concrete
    /// implementation per §4.3.1.
    /// </summary>
    public sealed class StopwatchPerfHarness : IPerfHarness
    {
        private readonly List<float> _samples = new List<float>();
        private readonly Dictionary<string, ulong> _allocByMethod = new Dictionary<string, ulong>();

        private SessionManifest _manifest;
        private string _thresholdCited;
        private LoopTag _loop;
        private bool _open;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manifest"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the manifest is incomplete.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a session is already open.</exception>
        public void BeginSession(SessionManifest manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            // The IPerfHarness contract requires a complete manifest, and SessionManifest is
            // immutable (there is nothing to fill in later), so enforce it here fail-fast rather
            // than admit an incomplete manifest that BaselineRecord would NOT reject (its ctor
            // null-guards the manifest reference but does not call IsComplete). Mirrors
            // CertifiedPerfBaseline.Certified.
            if (!manifest.IsComplete())
            {
                throw new ArgumentException(
                    "A profiling session requires a complete session manifest (§3.3.2).",
                    nameof(manifest));
            }

            if (_open)
            {
                throw new InvalidOperationException(
                    "A profiling session is already open; call FinalizeSession before beginning another.");
            }

            _manifest = manifest;
            _samples.Clear();
            _allocByMethod.Clear();
            _thresholdCited = null;
            _loop = default;
            _open = true;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when no session is open.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="actualMs"/> is non-finite or negative (a Stopwatch never
        /// yields these — this is a fail-closed guard against a corrupt sample).
        /// </exception>
        public void RecordTickSample(in BudgetRollupEntry declared, float actualMs, uint allocBytes)
        {
            if (!_open)
            {
                throw new InvalidOperationException(
                    "RecordTickSample requires an open session; call BeginSession first.");
            }

            if (!float.IsFinite(actualMs) || actualMs < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actualMs), actualMs, "A per-tick sample must be finite and non-negative.");
            }

            _samples.Add(actualMs);

            // dict[key] += x throws KeyNotFoundException on the absent-key read — accumulate safely.
            _allocByMethod.TryGetValue(declared.SubroutineName, out ulong prior);
            _allocByMethod[declared.SubroutineName] = prior + allocBytes;

            // A session cites one threshold and one loop (BaselineRecord carries a single
            // ThresholdCited / Loop); capture them from the first recorded sample.
            if (_samples.Count == 1)
            {
                _thresholdCited = declared.Citation;
                _loop = declared.Loop;
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when no session is open, or when the sample corpus is empty (an implicit pass is
        /// forbidden — a baseline with no measurement is a fabricated number).
        /// </exception>
        public BaselineRecord FinalizeSession()
        {
            if (!_open)
            {
                throw new InvalidOperationException(
                    "FinalizeSession requires an open session; call BeginSession first.");
            }

            if (_samples.Count == 0)
            {
                throw new InvalidOperationException(
                    "FinalizeSession requires at least one recorded sample; an empty corpus cannot " +
                    "project a baseline.");
            }

            var sorted = new List<float>(_samples);
            sorted.Sort();

            float p50 = Percentile(sorted, 50);
            float p99 = Percentile(sorted, 99);

            // Capture pass/fail is always Pass: establishing a baseline is not a gate (that is
            // RegressionGate's later job against a prior baseline), so capture never fabricates a
            // budget verdict — mirrors CertifiedPerfBaseline.TryBuildBaselineRecord.
            var record = new BaselineRecord(
                manifest: _manifest,
                loop: _loop,
                p50Ms: p50,
                p99Ms: p99,
                perMethodAllocBytes: new Dictionary<string, ulong>(_allocByMethod),
                passFailAtCapture: BaselinePassFail.Pass,
                thresholdCited: _thresholdCited);

            _open = false;
            return record;
        }

        /// <summary>
        /// Nearest-rank percentile over an ascending-sorted corpus: rank = ceil(p/100 · N) clamped
        /// to [1, N], value = sorted[rank-1]. Pure integer arithmetic (no float interpolation).
        /// Monotonic in <paramref name="percent"/>, so higher percentiles never fall below lower.
        /// </summary>
        private static float Percentile(List<float> sortedAscending, int percent)
        {
            int n = sortedAscending.Count;

            // ceil(percent/100 · n) via integer math: (percent·n + 99) / 100.
            long rank = ((long)percent * n + 99) / 100;
            if (rank < 1)
            {
                rank = 1;
            }
            else if (rank > n)
            {
                rank = n;
            }

            return sortedAscending[(int)(rank - 1)];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-13 | —      | Initial implementation. Concrete IPerfHarness (§3.3.5 manual        |
// |         |            |        | Stopwatch capture): BeginSession / RecordTickSample / Finalize-     |
// |         |            |        | Session with nearest-rank p50/p99 (spec pins no method; this pins   |
// |         |            |        | it), single-threshold/loop-per-session capture, empty-corpus and    |
// |         |            |        | double-session fail-closed guards, PassFail=Pass at capture.        |
#endregion
