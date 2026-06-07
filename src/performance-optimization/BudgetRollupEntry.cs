// File:     src/performance-optimization/BudgetRollupEntry.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §4.3.3, §3.1.3, Code Standards #20
// Purpose:  Read-only view onto a single per-spec §6 budget declaration.
//           Recomputed at build time by tools/budget-auditor.py (§5.3); never edited by hand.
//           Field layout matches the §3.1.3 roll-up table columns.

using System;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Read-only view onto a single per-spec §6 budget entry.
    /// Populated by <see cref="IBudgetSource.GetEntries"/> and aggregated into
    /// the Appendix C roll-up table by <c>tools/budget-auditor.py</c>.
    /// Performance Optimization Strategy #18 §4.3.3 / §3.1.3.
    /// </summary>
    public readonly struct BudgetRollupEntry
    {
        /// <summary>Owning spec identifier (e.g., <c>#1</c>, <c>#17</c>).</summary>
        public string SpecId { get; }

        /// <summary>Subroutine or pipeline step name within the spec.</summary>
        public string SubroutineName { get; }

        /// <summary>Loop this budget entry belongs to (KD-8).</summary>
        public LoopTag Loop { get; }

        /// <summary>Declared per-tick budget in milliseconds. [GT] or [EST] per owning spec.</summary>
        public float BudgetMs { get; }

        /// <summary>
        /// Declared allocation budget in bytes per tick.
        /// MUST be 0 for every hot-path entry per §3.7.3 / FR-PO-032.
        /// Positive values require <see cref="HotPathAllocExemptAttribute"/>.
        /// </summary>
        public uint AllocBudgetBytes { get; }

        /// <summary>Citation path to the owning spec §6 section (e.g., <c>shot-mechanics/section-4.md §4.5</c>).</summary>
        public string Citation { get; }

        /// <summary>
        /// Initialises a budget roll-up entry from a per-spec §6 declaration.
        /// </summary>
        public BudgetRollupEntry(
            string specId,
            string subroutineName,
            LoopTag loop,
            float budgetMs,
            uint allocBudgetBytes,
            string citation)
        {
            if (specId == null)
            {
                throw new ArgumentNullException(nameof(specId));
            }

            if (specId.Length == 0)
            {
                throw new ArgumentException("specId must be non-empty.", nameof(specId));
            }

            if (subroutineName == null)
            {
                throw new ArgumentNullException(nameof(subroutineName));
            }

            if (subroutineName.Length == 0)
            {
                throw new ArgumentException("subroutineName must be non-empty.", nameof(subroutineName));
            }

            if (citation == null)
            {
                throw new ArgumentNullException(nameof(citation));
            }

            if (citation.Length == 0)
            {
                throw new ArgumentException("citation must be non-empty.", nameof(citation));
            }

            if (!float.IsFinite(budgetMs) || budgetMs < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(budgetMs), budgetMs,
                    "budgetMs must be finite and non-negative.");
            }

            SpecId          = specId;
            SubroutineName  = subroutineName;
            Loop            = loop;
            BudgetMs        = budgetMs;
            AllocBudgetBytes = allocBudgetBytes;
            Citation        = citation;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-3 full-surface M-2: constructor enforces non-null + non-empty   |
// |         |            |        | specId / subroutineName / citation and finite, non-negative        |
// |         |            |        | budgetMs. Closes null-from-construction NPE source for downstream  |
// |         |            |        | tools/budget-auditor.py + IPerfHarness consumers.                  |
#endregion
