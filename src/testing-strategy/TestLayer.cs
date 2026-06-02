// File:     src/testing-strategy/TestLayer.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.1.1, Code Standards #20
// Purpose:  Five-layer test taxonomy per §3.1.1. Exhaustive and mutually exclusive
//           (FR-TS-001): every executable test belongs to exactly one layer.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Five-layer test taxonomy per §3.1.1. Every executable test belongs to exactly one layer.
    /// The <see cref="Determinism"/> layer is owned by Spec #16 §5 and consumed by Spec #19;
    /// listed here for completeness per KD-2.
    /// Testing Strategy &amp; Framework #19 §3.1.1 / FR-TS-001.
    /// </summary>
    public enum TestLayer : byte
    {
        /// <summary>Single struct or method. Zero-alloc body, sub-millisecond wall-time per §3.1.1.</summary>
        Unit = 0,

        /// <summary>Two-to-five subsystems wired without a Unity scene. May allocate.</summary>
        Integration = 1,

        /// <summary>Full subsystem stack under a scripted scenario; rendering disabled.</summary>
        Simulation = 2,

        /// <summary>Owned by Deterministic Simulation #16 §5; consumed by Spec #19 per KD-2.</summary>
        Determinism = 3,

        /// <summary>Long-horizon run ≥ one full 90-minute match of in-game time per §3.1.1.</summary>
        EndToEndSoak = 4,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
