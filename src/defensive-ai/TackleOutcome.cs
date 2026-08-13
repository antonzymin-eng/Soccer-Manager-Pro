// File:     src/defensive-ai/TackleOutcome.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6.5, Code Standards #20
// Purpose:  The four ways a committed tackle can end. Ordinals are stable — they are a
//           digest-visible classification of a match event.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// How a committed tackle resolved (#14 §3.6.5).
    ///
    /// <para>Four outcomes, not three. The obvious model — won, fouled, missed — has no way to express
    /// the most common thing that happens when a defender gets a foot in: the ball goes somewhere
    /// neither player controls. Collapsing that into "won" makes every successful challenge a clean
    /// turnover, and collapsing it into "missed" makes it invisible. <see cref="BallLoose"/> is its own
    /// outcome, and it is expected to be the commonest of the three non-null ones.</para>
    ///
    /// <para>Ordinals are stable and append-only: the outcome reaches match flow and is therefore
    /// digest-visible, so renumbering would silently invalidate every recorded replay.</para>
    /// </summary>
    public enum TackleOutcome : byte
    {
        /// <summary>The challenge did not connect. Possession is unchanged and nothing is emitted —
        /// a mistimed tackle that touches nobody is not an event, it is an absence.</summary>
        Missed = 0,

        /// <summary>The tackler takes the ball cleanly and becomes the holder.</summary>
        BallWon = 1,

        /// <summary>The carrier is dispossessed but nobody gains control — the ball is knocked free and
        /// becomes a loose ball, to be resolved by the ordinary loose-ball paths (first touch, pickup)
        /// like any other. Neither player is favoured; that is what makes it a 50-50.</summary>
        BallLoose = 2,

        /// <summary>The challenge is a foul on the tackler. The carrier's team gets the restart; the
        /// card is drawn by the existing match-flow discipline path, which stays the single
        /// authority on cards.</summary>
        Foul = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-12 | —      | Initial. #14 §3.6.5 — the tackle outcome enum (wiring backlog    |
// |         |            |        | W2). Four outcomes; BallLoose exists because a tackle that       |
// |         |            |        | knocks the ball free is neither a clean win nor a miss.          |
#endregion
