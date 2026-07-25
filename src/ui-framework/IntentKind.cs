// File:     src/ui-framework/IntentKind.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.3 (FR-UI-012/013/014, failure mode F3),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20
// Purpose:  Discriminates the closed set of manager intents the UI may express. Each maps onto exactly
//           one EXISTING public engine seam; there is no kind for a seam that does not already exist,
//           which is the enum-level expression of "the UI invents no mutation path" (KD-4).

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// The closed set of manager-intent kinds (#38 §3.3). Each maps onto one verified public
    /// <c>MatchEngine</c> seam: <c>SetTeamTactic</c>, <c>SetPlayerTactic</c>, <c>SubstitutePlayer</c>.
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

        // DELIBERATELY ABSENT: #38 §2.2's enum sketch also lists AdvanceRound. Its seam is the #30
        // Season & Competition Loop's AdvanceAndPlayNextRound, and #30 has no src/ assembly — a kind
        // here would be a phantom the dispatcher could only throw on (FR-UI-020 / KD-4). It is added
        // when #30 is built, by APPENDING ordinal 4.
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): three intent kinds, one per         |
// |         |            |        | verified public seam, plus the None=0 zero-value sentinel.     |
#endregion
