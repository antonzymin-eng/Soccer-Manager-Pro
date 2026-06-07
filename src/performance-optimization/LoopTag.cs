// File:     src/performance-optimization/LoopTag.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.2, Appendix A, Code Standards #20
// Purpose:  Identifies which simulation loop a budget entry or baseline record belongs to.
//           Every budget number in every spec §6 carries exactly one LoopTag (KD-8).

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Loop tag distinguishing the 10 Hz tactical loop from the 60 Hz physics/render loop.
    /// Carried in every <see cref="BaselineRecord"/> and <see cref="BudgetRollupEntry"/> per KD-8.
    /// On-disk string keys: <c>LOOP-TACTICAL-10HZ</c> / <c>LOOP-PHYSICS-60HZ</c> per §3.2.2.
    /// Performance Optimization Strategy #18 §3.2.
    ///
    /// ORDINAL STABILITY: this enum is embedded in three serialised types
    /// (<see cref="BaselineRecord"/> / <see cref="BudgetRollupEntry"/> / <see cref="HotPathEntry"/>).
    /// Tooling MAY persist the int ordinal alongside the on-disk string key for cross-validation.
    /// APPEND new members at the end ONLY — inserting in the middle shifts ordinals on existing
    /// on-disk baselines and decouples them from the LOOP_TAG_* string projections in §3.2.2.
    /// </summary>
    public enum LoopTag
    {
        /// <summary>
        /// 10 Hz AI/tactical heartbeat loop. Tick window: 100 ms.
        /// On-disk key: <see cref="PerformanceOptimizationConstants.LOOP_TAG_TACTICAL_10HZ"/>.
        /// </summary>
        TacticalTenHz,

        /// <summary>
        /// 60 Hz physics/render loop. Frame window: ~16.67 ms.
        /// On-disk key: <see cref="PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ"/>.
        /// </summary>
        PhysicsSixtyHz
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-4 L-1: ordinal-stability paragraph added — enum embedded in     |
// |         |            |        | BaselineRecord / BudgetRollupEntry / HotPathEntry on-disk schemas. |
#endregion
