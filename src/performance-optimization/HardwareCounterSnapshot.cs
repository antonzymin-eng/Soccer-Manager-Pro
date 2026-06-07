// File:     src/performance-optimization/HardwareCounterSnapshot.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.2, Appendix A, Code Standards #20
// Purpose:  Immutable hardware-counter snapshot captured at session start.
//           Required field in SessionManifest; sessions missing this are rejected
//           by the §3.4.4 baseline validator.

using System;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Immutable snapshot of hardware performance counters captured at profiling session start.
    /// Required field in the §3.3.2 session manifest per Appendix A.
    /// Note: <c>default(HardwareCounterSnapshot)</c> bypasses the explicit-ctor invariants
    /// (C# struct default-value semantics); <see cref="SessionManifest.IsComplete"/> catches
    /// the default-struct shape downstream as defence in depth.
    /// Performance Optimization Strategy #18 §3.3.2 / Appendix A.
    /// </summary>
    public readonly struct HardwareCounterSnapshot
    {
        /// <summary>CPU model string (e.g., "Intel Core i7-12700K").</summary>
        public string CpuModel { get; }

        /// <summary>Physical core count on the measurement machine.</summary>
        public int CoreCount { get; }

        /// <summary>
        /// Thermal state at session start (e.g., "nominal", "throttling").
        /// Throttled captures are marked advisory per §3.3.6 anti-patterns.
        /// </summary>
        public string ThermalState { get; }

        /// <summary>
        /// Initialises the snapshot with all required hardware fields.
        /// </summary>
        public HardwareCounterSnapshot(string cpuModel, int coreCount, string thermalState)
        {
            if (cpuModel == null)
            {
                throw new ArgumentNullException(nameof(cpuModel));
            }

            if (cpuModel.Length == 0)
            {
                throw new ArgumentException("cpuModel must be non-empty.", nameof(cpuModel));
            }

            if (thermalState == null)
            {
                throw new ArgumentNullException(nameof(thermalState));
            }

            if (thermalState.Length == 0)
            {
                throw new ArgumentException("thermalState must be non-empty.", nameof(thermalState));
            }

            if (coreCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coreCount), coreCount,
                    "coreCount must be > 0.");
            }

            CpuModel     = cpuModel;
            CoreCount    = coreCount;
            ThermalState = thermalState;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-3 full-surface L-1: explicit ctor enforces non-null + non-empty |
// |         |            |        | cpuModel / thermalState and coreCount > 0. Default(T) still        |
// |         |            |        | bypasses (C# struct default-value semantics); SessionManifest.     |
// |         |            |        | IsComplete catches the default shape as defence in depth.          |
#endregion
