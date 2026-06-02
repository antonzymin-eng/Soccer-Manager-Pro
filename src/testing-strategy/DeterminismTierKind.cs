// File:     src/testing-strategy/DeterminismTierKind.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2.2, Deterministic Simulation #16 §5, Code Standards #20
// Purpose:  Canonical tier order of #16 §5's regression suite, consumed by
//           DeterminismGate per FR-TS-011 (every CI pipeline runs tiers in this
//           order; Spec #19 does not introduce new tier categories — FR-TS-018).

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Canonical tier order of #16 §5's regression suite per FR-TS-011.
    /// Spec #19 consumes this order; new tier categories require a #16 §5 revision (FR-TS-018).
    /// Ordinals match the canonical sequence: Unit → Integration → Scenario → Soak.
    /// Testing Strategy &amp; Framework #19 §3.2.2 / Deterministic Simulation #16 §5.
    /// </summary>
    public enum DeterminismTierKind : byte
    {
        /// <summary>Per-method bitwise determinism unit tests (#16 §5 unit tier).</summary>
        Unit = 0,

        /// <summary>Cross-subsystem digest-chain checks (#16 §5 integration tier).</summary>
        Integration = 1,

        /// <summary>Scenario-replay digest equivalence (#16 §5 scenario tier).</summary>
        Scenario = 2,

        /// <summary>Long-horizon soak determinism (#16 §5 soak tier).</summary>
        Soak = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
