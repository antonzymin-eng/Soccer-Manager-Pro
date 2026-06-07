// File:     src/performance-optimization/HotPathAllocExemptAttribute.cs
// Created:  2026-05-30
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.7.5, Appendix B, Code Standards #20
// Purpose:  C# attribute marking a hot-path entry point as exempt from the zero-allocation
//           budget (FR-PO-032 / FR-CS-066). Every exemption requires lead-developer sign-off
//           recorded in the PR description per §3.7.5.
//           Governance identifier declared here (§3.7.5); definition was deferred from Stage 0.

using System;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Marks a method or class as exempt from the zero-allocation hot-path budget.
    /// Every use MUST have lead-developer sign-off recorded in the PR description.
    /// Violations of this requirement are tracked as ERR-018-NNN entries in spec-error-log.md.
    /// Performance Optimization Strategy #18 §3.7.5 / Appendix B.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = false,
        AllowMultiple = false)]
    public sealed class HotPathAllocExemptAttribute : Attribute
    {
        /// <summary>
        /// Human-readable justification for the exemption (required).
        /// Cite the spec constraint or architectural reason that forces the allocation.
        /// </summary>
        public string Justification { get; }

        /// <summary>
        /// Reference to the lead-developer sign-off (PR number, spec-error-log.md row ID,
        /// or GitHub issue). Must be populated before merging. §3.7.5.
        /// Enforcement of the "must be populated" rule is a deferred CI-checker
        /// responsibility (Stage 0+1 deliverable; tracked under tools/budget-auditor.py
        /// per §3.7.5 / Appendix B). The setter is required by attribute syntax
        /// (<c>[HotPathAllocExempt("…", SignOffRef = "…")]</c>) but the runtime cannot
        /// reject a missing value — Roslyn analyzer or the CI checker enforces it.
        /// </summary>
        public string SignOffRef { get; set; }

        /// <summary>
        /// Initialises the exemption attribute with a mandatory justification string.
        /// </summary>
        /// <param name="justification">
        /// Why this code path cannot avoid a managed allocation on the hot path.
        /// </param>
        public HotPathAllocExemptAttribute(string justification)
        {
            Justification = justification;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-05-30 | —      | Initial implementation. Attribute deferred from Stage 0 per §3.7.5 |
// |         |            |        | Appendix B; now declared as Stage 0+1 kick-off deliverable.        |
// | 1.1     | 2026-06-07 | —      | AR-3 L-5: SignOffRef XML doc records that "must be populated"      |
// |         |            |        | enforcement is a deferred CI-checker responsibility (tools/budget- |
// |         |            |        | auditor.py / Roslyn analyzer), not a runtime invariant the         |
// |         |            |        | attribute can enforce.                                              |
#endregion
