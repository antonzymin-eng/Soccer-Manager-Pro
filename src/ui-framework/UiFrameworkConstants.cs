// File:     src/ui-framework/UiFrameworkConstants.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §4.2 (FR-UI-016/022),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20 (constant
//           catalogue; no magic numbers)
// Purpose:  Constant catalogue for the UI framework. Presentation-only — nothing here reaches the
//           simulation, the snapshot digest, or any determinism surface.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Constant catalogue for the UI framework (#38 §4.2). Every value is presentation feel, never a sim
    /// constant: #38 registers no RNG stream, no domain tag, no <c>SubsystemOrdinal</c>, and holds no
    /// persistent sim state (FR-UI-022).
    /// </summary>
    public static class UiFrameworkConstants
    {
        #region GT — render cadence

        /// <summary>
        /// [GT] Match-view refresh rate, hertz — how often the view re-projects the latest published
        /// frame. Deliberately independent of the 60 Hz physics and 10 Hz AI loops: the view samples
        /// whatever has been published, so this trades render cost against motion smoothness and cannot
        /// affect the sim (FR-UI-016).
        /// <para>
        /// DECLARED, NOT YET CONSUMED: at T0 nothing renders — the UGUI binding that drives the refresh
        /// loop is Unity-host-gated (#38 §4.3/§7.2) and is this value's consumer. It is catalogued now
        /// so the cadence is a tunable from the moment the binding lands, rather than arriving as a
        /// literal inside a <c>MonoBehaviour</c>.
        /// </para>
        /// </summary>
        public static readonly float MatchViewRefreshHz = Config.GetFloat("ui-framework", "MatchViewRefreshHz", 30f);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): the [GT] match-view refresh cadence, |
// |         |            |        | doc-noted declared-but-unconsumed pending the §7.2 UGUI        |
// |         |            |        | binding (plan AR-1 L-2).                                       |
#endregion
