// File:     src/match-engine/RestartCue.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1 KD-P1-5,
//           Code Standards #20
// Purpose:  What kind of restart the engine applied, for the presentation layer's HUD. Reported
//           per-tick through MatchEngine.RestartAppliedThisTick; never serialized, never drawn against.

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The kind of restart <c>ApplyRestart</c> placed the ball for, as reported for the current tick by
    /// <see cref="MatchEngine.RestartAppliedThisTick"/>.
    ///
    /// <para><b>Why this is not Ball Physics' <c>RestartType</c> (KD-P1-5).</b>
    /// <c>TacticalDirector.BallPhysics.RestartType</c> classifies <em>boundary exits</em> —
    /// <c>None</c>/<c>ThrowIn</c>/<c>GoalKick</c>/<c>Corner</c>/<c>KickOff</c> — and carries an explicit
    /// ORDINAL STABILITY contract because its values are embedded in the Tier A
    /// <c>RestartAwardedEvent</c> (0x19) payload that the digest-load-bearing event ledger serializes.
    /// Fouls and offside produce <b>free kicks</b>, which are not boundary exits and have no member
    /// there. Adding one would widen a digest-bearing domain owned by another spec purely to serve a
    /// presentation need, so <c>match-engine</c> declares this parallel presentation enum instead and
    /// maps the physics value into it at the single site that has one.</para>
    ///
    /// <para>ORDINAL STABILITY: not required — never serialized and never embedded in an event payload.
    /// It is a per-tick observation value only.</para>
    /// </summary>
    public enum RestartCue
    {
        /// <summary>No restart was applied this tick.</summary>
        None = 0,

        /// <summary>Kickoff — the boot kickoff, the second-half restart, or the restart after a goal.</summary>
        KickOff = 1,

        /// <summary>Throw-in (ball left play over a touchline).</summary>
        ThrowIn = 2,

        /// <summary>Goal kick (ball left play over a goal line, last touched by the attacking side).</summary>
        GoalKick = 3,

        /// <summary>Corner (ball left play over a goal line, last touched by the defending side).</summary>
        Corner = 4,

        /// <summary>Free kick — awarded for a foul or for offside.</summary>
        FreeKick = 5
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame, KD-P1-5): the   |
// |         |            |        | presentation restart-cue enum, kept off Ball Physics'          |
// |         |            |        | ordinal-stable RestartType so no digest-bearing domain widens. |
#endregion
