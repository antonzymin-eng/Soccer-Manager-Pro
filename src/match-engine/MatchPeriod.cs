// File:     src/match-engine/MatchPeriod.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1 KD-P1-2,
//           Code Standards #20
// Modified: 2026-07-27 (AR-3: header corrected — the period derives from the two transition FLAGS, not
//           from CurrentTick, which is the whole point of KD-P1-2)
// Purpose:  Which period of the match the clock is in, for the presentation layer's HUD. Derived
//           read-only from the transition flags MatchEngine already owns and serializes
//           (_secondHalfStarted / _matchEnded) — never stored here, never separately serialized.

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The period of play a match clock is in, as reported by <see cref="MatchEngine.CurrentPeriod"/>.
    ///
    /// <para><b>Why this is not called <c>MatchPhase</c> (KD-P1-2).</b>
    /// <c>TacticalDirector.DecisionTree.MatchPhase</c> already exists and means something entirely
    /// different — the phase of PLAY (<c>OPEN_PLAY</c>…) that the Decision Tree reasons about — and both
    /// types are in scope together inside <c>match-engine</c>. This project has hit the resulting CS0104
    /// ambiguity three times (<c>TacticTranslation</c>, <c>PlayerAttributes</c>, and the §5.Z.9
    /// foul-probability helper), so the collision is avoided by name rather than by qualification.</para>
    ///
    /// <para><b>Why there is no <c>HalfTime</c> member.</b> The Stage-0 halves model has no break:
    /// <c>CheckMatchFlowTransitions</c> resets the ball at <c>HALF_TIME_BOUNDARY_TICK</c> and play
    /// continues on the very next tick (FR-TP-019 — no interval, no ends swap). A <c>HalfTime</c> member
    /// would name a state the engine cannot be in, and a View binding it would render a pause that never
    /// happens. It gets added when the engine grows a real interval, not before.</para>
    ///
    /// <para>ORDINAL STABILITY: not required — this value is derived per call and is never serialized,
    /// never drawn against, and never embedded in an event payload.</para>
    /// </summary>
    public enum MatchPeriod
    {
        /// <summary>Before the half-time boundary tick.</summary>
        FirstHalf = 0,

        /// <summary>At or after the half-time boundary tick, before full time has fired.</summary>
        SecondHalf = 1,

        /// <summary>Full time has fired; gameplay is frozen (the tick loop keeps advancing).</summary>
        FullTime = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame, KD-P1-2): the   |
// |         |            |        | derived match-period readout for the presentation layer.       |
// | 1.1     | 2026-07-27 | —      | AR-3 (doc): the file header claimed derivation from            |
// |         |            |        | CurrentTick. It derives from the two transition flags, which   |
// |         |            |        | is exactly KD-P1-2 — one reader of the boundary rule, and a    |
// |         |            |        | period that cannot disagree with what the engine did.          |
#endregion
