// File:     src/testing-strategy/ScenarioStatus.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.3 / FR-TS-024, Code Standards #20
// Purpose:  Status component of ScenarioResult per the §3.3.3 runner contract:
//           { status: Passed | Failed | Quarantined, ... }.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Outcome status of one <see cref="ScenarioRunner"/> invocation per §3.3.3.
    /// <see cref="Quarantined"/> is produced only by the §3.7 flake-handling layer,
    /// which is Stage-gated per KD-5 (activates at Stage 0+1 with CI integration) —
    /// no Stage 0 code path emits it; the value exists so the contract shape is
    /// stable when §3.7 activates.
    /// Testing Strategy &amp; Framework #19 §3.3.3 / FR-TS-024.
    /// </summary>
    public enum ScenarioStatus : byte
    {
        /// <summary>Every envelope predicate evaluated and passed (≥ 1 predicate; FR-TS-030).</summary>
        Passed = 0,

        /// <summary>At least one predicate failed, the body threw, or zero predicates were evaluated.</summary>
        Failed = 1,

        /// <summary>Flake-quarantined per §3.7.3. Stage 0+1 only; never emitted at Stage 0.</summary>
        Quarantined = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
