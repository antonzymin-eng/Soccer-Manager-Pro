// File:     src/performance-optimization/HotPathEntry.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.7.2, Code Standards #20
// Purpose:  Represents a single entry in the hot-path union set (§3.7.2).
//           The union is the set of all per-spec §6 budget-table entries; it is
//           materialised at build time into tools/hot-path-union.json by
//           tools/budget-auditor.py (Stage 0+1 deliverable).

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// A single method-level entry in the hot-path union set per §3.7.2.
    /// Every entry has a zero-byte allocation budget per §3.7.3; exemptions
    /// require <see cref="HotPathAllocExemptAttribute"/> with lead-developer sign-off.
    /// Performance Optimization Strategy #18 §3.7.2 / §3.7.3.
    /// </summary>
    public readonly struct HotPathEntry
    {
        /// <summary>Owning spec identifier (e.g., <c>#6</c>).</summary>
        public string SpecId { get; }

        /// <summary>Fully-qualified method name as it appears in the allocation tracker dump.</summary>
        public string MethodName { get; }

        /// <summary>Loop this hot-path entry runs on (KD-8).</summary>
        public LoopTag Loop { get; }

        /// <summary>Declared per-tick budget in milliseconds for this entry.</summary>
        public float BudgetMs { get; }

        /// <summary>
        /// True if this entry carries a <see cref="HotPathAllocExemptAttribute"/>.
        /// Allocation-tracker CI step excludes exempt entries from the zero-alloc diff.
        /// </summary>
        public bool HasAllocExemption { get; }

        /// <summary>
        /// Initialises a hot-path entry from a per-spec §6 subroutine row.
        /// </summary>
        public HotPathEntry(
            string specId,
            string methodName,
            LoopTag loop,
            float budgetMs,
            bool hasAllocExemption)
        {
            SpecId            = specId;
            MethodName        = methodName;
            Loop              = loop;
            BudgetMs          = budgetMs;
            HasAllocExemption = hasAllocExemption;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
