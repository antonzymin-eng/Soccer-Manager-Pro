// File:     src/testing-strategy/IScenario.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §4.4.1, Code Standards #20
// Purpose:  Scenario contract per §4.4.1. Producer: scenario authors (per-spec §5 and
//           Spec #19 §3 cross-spec scenarios). Consumer: ScenarioRunner (§3.3.3).
//           Both sides are specified in Spec #19, so declaration is permitted under
//           the CLAUDE.md Interface Design Principle.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One executable scenario per §4.4.1. Single method by contract; the manifest a
    /// scenario was registered under travels in <see cref="ScenarioIndexEntry"/>, not
    /// here, so this interface stays exactly the §4.4.1 surface.
    /// Testing Strategy &amp; Framework #19 §4.4.1.
    /// </summary>
    public interface IScenario
    {
        /// <summary>
        /// Runs the scenario under <paramref name="seed"/> (passed verbatim to
        /// DeterministicRngService before any subsystem initialisation; KD-7) and
        /// returns the structured §3.3.3 result. Implementations MUST be hermetic:
        /// no state survives between invocations (FR-TS-023).
        /// </summary>
        ScenarioResult Run(ulong seed);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
