// File:     src/performance-optimization/IBudgetSource.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §4.4, Code Standards #20
// Purpose:  Implemented by each per-spec §6 metadata extractor; consumed by the
//           tools/hot-path-union.json builder (tools/budget-auditor.py §3.7.2).
//           Both producer and consumer are specified → interface permitted per
//           CLAUDE.md "Interface Design Principle".

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Implemented by each per-spec §6 metadata extractor.
    /// Consumed by <c>tools/budget-auditor.py</c> to build <c>tools/hot-path-union.json</c>
    /// per §3.7.2. Both producer and consumer are specified per §4.4.
    /// Performance Optimization Strategy #18 §4.4.
    /// </summary>
    public interface IBudgetSource
    {
        /// <summary>
        /// Spec identifier for all entries returned by this source (e.g., <c>#6</c>).
        /// </summary>
        string SpecId { get; }

        /// <summary>
        /// Returns all §6 budget entries declared by the owning spec.
        /// Each entry maps to one row in the §3.1.3 roll-up table (Appendix C).
        /// The returned array is allocated once; callers must not mutate it.
        /// </summary>
        BudgetRollupEntry[] GetEntries();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
