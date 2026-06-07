// File:     src/performance-optimization/HotPathEntry.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.7.2, Code Standards #20
// Purpose:  Represents a single entry in the hot-path union set (§3.7.2).
//           The union is the set of all per-spec §6 budget-table entries; it is
//           materialised at build time into tools/hot-path-union.json by
//           tools/budget-auditor.py (Stage 0+1 deliverable).

using System;

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
            if (specId == null)
            {
                throw new ArgumentNullException(nameof(specId));
            }

            if (specId.Length == 0)
            {
                throw new ArgumentException("specId must be non-empty.", nameof(specId));
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (methodName.Length == 0)
            {
                throw new ArgumentException("methodName must be non-empty.", nameof(methodName));
            }

            if (!float.IsFinite(budgetMs) || budgetMs < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(budgetMs), budgetMs,
                    "budgetMs must be finite and non-negative.");
            }

            SpecId            = specId;
            MethodName        = methodName;
            Loop              = loop;
            BudgetMs          = budgetMs;
            HasAllocExemption = hasAllocExemption;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-3 full-surface M-3: constructor enforces non-null + non-empty   |
// |         |            |        | specId / methodName and finite, non-negative budgetMs. Parallel to |
// |         |            |        | BudgetRollupEntry hardening.                                       |
#endregion
