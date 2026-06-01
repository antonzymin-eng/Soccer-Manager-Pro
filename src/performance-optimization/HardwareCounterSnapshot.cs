// File:     src/performance-optimization/HardwareCounterSnapshot.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.2, Appendix A, Code Standards #20
// Purpose:  Immutable hardware-counter snapshot captured at session start.
//           Required field in SessionManifest; sessions missing this are rejected
//           by the §3.4.4 baseline validator.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Immutable snapshot of hardware performance counters captured at profiling session start.
    /// Required field in the §3.3.2 session manifest per Appendix A.
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
            CpuModel     = cpuModel;
            CoreCount    = coreCount;
            ThermalState = thermalState;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
