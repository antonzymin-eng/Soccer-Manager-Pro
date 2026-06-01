// File:     src/performance-optimization/IPerfHarness.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §4.3.1, §4.4, Code Standards #20
// Purpose:  Consumed by per-spec benchmark runners (tests/perf/) and by
//           Spec #19 ScenarioRunner. Producer: Spec #18 §3.3 harness authors.
//           Both sides specified → interface permitted per CLAUDE.md "Interface Design Principle".

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Session API consumed by per-spec benchmark runners and by Spec #19 <c>ScenarioRunner</c>.
    /// Single concrete implementation per §4.3.1 — no IoC container.
    /// Producer: Spec #18 §3.3 harness. Consumer: Spec #19 scenario runner.
    /// Performance Optimization Strategy #18 §4.3.1 / §4.4.
    /// </summary>
    public interface IPerfHarness
    {
        /// <summary>
        /// Opens a profiling session. Must be called before any <see cref="RecordTickSample"/> call.
        /// The manifest MUST be complete per <see cref="SessionManifest.IsComplete"/>; the §3.4.4
        /// baseline validator rejects records from incomplete sessions.
        /// </summary>
        /// <param name="manifest">§3.3.2 session manifest with all required fields.</param>
        void BeginSession(SessionManifest manifest);

        /// <summary>
        /// Records a single per-tick timing and allocation sample within the open session.
        /// Multiple calls accumulate the sample corpus; <see cref="FinalizeSession"/> computes
        /// p50/p99 percentiles from the accumulated corpus.
        /// </summary>
        /// <param name="declared">The §6 budget entry this sample is measured against.</param>
        /// <param name="actualMs">Measured wall-clock duration of this tick/frame in milliseconds.</param>
        /// <param name="allocBytes">Managed bytes allocated during this tick/frame (0 on hot paths).</param>
        void RecordTickSample(in BudgetRollupEntry declared, float actualMs, uint allocBytes);

        /// <summary>
        /// Closes the session, computes p50/p99 over the accumulated sample corpus, and
        /// returns an immutable <see cref="BaselineRecord"/> suitable for storage under
        /// <c>docs/specs/performance-optimization/baselines/</c>.
        /// Calling this without a preceding <see cref="BeginSession"/> is an error.
        /// </summary>
        BaselineRecord FinalizeSession();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
