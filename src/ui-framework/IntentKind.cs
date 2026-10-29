// File:     src/ui-framework/IntentKind.cs
// Created:  2026-07-25
// Modified: 2026-10-18
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.3 (FR-UI-012/013/014, failure mode F3),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20,
//           docs/tracking/interactive-unity-client-design.md §5-P7 (season kinds 4–6)
// Purpose:  Discriminates the closed set of manager intents the UI may express. Each maps onto exactly
//           one EXISTING public engine seam; there is no kind for a seam that does not already exist,
//           which is the enum-level expression of "the UI invents no mutation path" (KD-4).

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// The closed set of manager-intent kinds (#38 §3.3). Each maps onto one verified public seam:
    /// the three <b>match</b> kinds onto <c>MatchEngine</c> (<c>SetTeamTactic</c>,
    /// <c>SetPlayerTactic</c>, <c>SubstitutePlayer</c>), the three <b>season</b> kinds onto #30's
    /// <c>SeasonLoop</c> (<c>AdvanceDays(1)</c>, <c>AdvanceAndPlayNextRound()</c>,
    /// <c>RollToNextSeason()</c>). <see cref="IntentRouting"/> is the one place that says which
    /// dispatcher owns which kind (#38 Appendix C splits them by owning surface).
    /// <para>
    /// <see cref="None"/> is the zero value on purpose: a <c>default(ManagerIntent)</c> — an unfilled
    /// array slot, a <c>new ManagerIntent()</c> — therefore reads as <see cref="None"/> and is refused
    /// fail-loud at dispatch (F3), rather than silently mapping onto a real seam with a malformed
    /// payload. This mirrors <c>ManagerCommandKind.None</c>, whose absence was a real defect caught in
    /// that type's own AR pass.
    /// </para>
    /// <para>
    /// Playback controls (pause / resume / speed) are deliberately absent: they are presentation pacing
    /// on the streamer's own surface and never touch the sim, so they are not manager intent.
    /// A screen needing a seam that does not exist yet (transfers, training, scouting) files it against
    /// the owning spec — it does NOT get a kind here (FR-UI-020 / KD-4).
    /// </para>
    /// </summary>
    public enum IntentKind
    {
        /// <summary>The uninitialized sentinel (zero value). Not a real intent — refused at dispatch.</summary>
        None = 0,

        /// <summary>Change a team's tactic (→ <c>MatchEngine.SetTeamTactic</c>).</summary>
        SetTeamTactic = 1,

        /// <summary>Change one agent's tactic (→ <c>MatchEngine.SetPlayerTactic</c>).</summary>
        SetPlayerTactic = 2,

        /// <summary>Swap a bench player onto the pitch (→ <c>MatchEngine.SubstitutePlayer</c>).</summary>
        Substitute = 3,

        /// <summary>
        /// Resolve the round at the cursor (→ #30 <c>SeasonLoop.AdvanceAndPlayNextRound()</c>, the #38
        /// Appendix C season row). Season dispatcher only.
        /// </summary>
        AdvanceRound = 4,

        /// <summary>
        /// Advance the career exactly one calendar day (→ #30 <c>SeasonLoop.AdvanceDays(1)</c>). The
        /// day-granular progress action the owner chose for Early Access (§5-P7 D-P7-3; ERR-038-005).
        /// Season dispatcher only.
        /// </summary>
        AdvanceDay = 5,

        /// <summary>
        /// Run the season-boundary roll (→ #30 <c>SeasonLoop.RollToNextSeason()</c>; ERR-038-005).
        /// Season dispatcher only.
        /// </summary>
        StartNextSeason = 6,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): three intent kinds, one per         |
// |         |            |        | verified public seam, plus the None=0 zero-value sentinel.     |
// | 1.1     | 2026-10-18 | —      | §5-P7 / ERR-038-005: APPEND the three season kinds now that #30 |
// |         |            |        | is built — AdvanceRound (4, the #38 Appendix C row),           |
// |         |            |        | AdvanceDay (5) and StartNextSeason (6). Ordinals 1–3 unchanged. |
#endregion
