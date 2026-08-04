// File:     src/match-client-core/MatchRenderProjection.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7,
//           §12 rule 1), Ball Physics #1 §1.2 (corner-origin frame), Code Standards #20
// Purpose:  Turns a live frame plus the interpolated positions drawn from it into the per-agent and
//           ball draw states a renderer binds. The last host-free step before pixels.

using System;

using UnityEngine;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Resolves a frame into draw states (§5-P4a) — the join between P3's interpolation math and the
    /// P4b render skin.
    ///
    /// <para><b>Two inputs, on purpose.</b> Positions come from the interpolator's output buffer,
    /// because those are the smoothed positions actually being drawn this display frame; every
    /// discrete cue — possession, cards, sendings-off, who is a substitute, who is the keeper —
    /// comes from the newest captured frame, because those do not interpolate. Blending a card into
    /// existence over 16 ms is meaningless; blending a position is the entire point.</para>
    ///
    /// <para><b>Fail-loud on shape, and on a coordinate that cannot be drawn.</b> A buffer that is
    /// too short, a cue array that does not match its positions, a roster smaller than the frame —
    /// each is a wiring bug that would otherwise surface as agents drawn at stale positions or
    /// annotated with another player's cards. A non-finite position is the same class one level
    /// down, and nothing upstream refuses it: neither <c>LiveMatchStreamer</c> nor
    /// <c>FrameInterpolator</c> gates coordinates (the interpolator deliberately propagates a
    /// non-finite value, treating it as a discontinuity to snap to), so without a gate here a NaN
    /// would reach <c>transform.position</c>. <c>MatchFrameView</c>'s constructor refuses exactly
    /// this on the screen-facing path; the pitch-facing path gets the same treatment rather than a
    /// quieter one.</para>
    ///
    /// <para>Allocation-free: both entry points write into caller-supplied storage or return a
    /// value type. Pure — nothing here reads a clock, and nothing reaches the simulation.</para>
    /// </summary>
    public static class MatchRenderProjection
    {
        /// <summary>
        /// Writes one <see cref="AgentRenderModel"/> per roster slot of <paramref name="frame"/> into
        /// <paramref name="destination"/>, and returns how many were written.
        /// </summary>
        /// <param name="pitchPositions">
        /// Positions to draw, in corner-origin pitch metres — normally
        /// <see cref="FrameInterpolator.AgentsAt"/>'s output buffer. Must hold at least as many
        /// entries as <paramref name="frame"/> has roster slots; extra trailing entries are ignored,
        /// since the interpolator's buffer is allowed to be larger than the roster.
        /// </param>
        /// <param name="frame">The newest captured frame — the source of every discrete cue.</param>
        /// <param name="roster">Match-constant per-slot data (team, shirt number).</param>
        /// <param name="destination">Buffer written into. May be longer than the roster.</param>
        /// <exception cref="ArgumentNullException">Any argument, or the frame's own arrays, is null.</exception>
        /// <exception cref="ArgumentException">
        /// The frame's cue array does not match its position array, or
        /// <paramref name="pitchPositions"/> / <paramref name="destination"/> / <paramref name="roster"/>
        /// is smaller than the frame's roster, or a position to draw is not finite. Nothing is
        /// written to <paramref name="destination"/> when any of these is refused.
        /// </exception>
        public static int ProjectAgents(
            Vector2[] pitchPositions,
            in LiveMatchFrame frame,
            MatchRoster roster,
            AgentRenderModel[] destination)
        {
            if (pitchPositions == null) { throw new ArgumentNullException(nameof(pitchPositions)); }
            if (roster == null)         { throw new ArgumentNullException(nameof(roster)); }
            if (destination == null)    { throw new ArgumentNullException(nameof(destination)); }

            Vector2[] framePositions = frame.AgentPositions;
            LiveAgentCue[] cues      = frame.AgentCues;

            if (framePositions == null)
            {
                throw new ArgumentNullException(nameof(frame), "frame.AgentPositions is null.");
            }
            if (cues == null)
            {
                throw new ArgumentNullException(nameof(frame), "frame.AgentCues is null.");
            }

            int count = framePositions.Length;

            if (cues.Length != count)
            {
                throw new ArgumentException(
                    "frame carries " + Inv(cues.Length) + " cues but " + Inv(count) +
                    " positions; the two would annotate different players.", nameof(frame));
            }
            RequireAtLeast(pitchPositions.Length, count, nameof(pitchPositions));
            RequireAtLeast(destination.Length, count, nameof(destination));
            RequireAtLeast(roster.AgentCount, count, nameof(roster));

            // Validated in a pass of its own, ahead of any write. Checking inside the write loop
            // would let a bad slot 5 leave slots 0..4 holding this frame and the rest holding the
            // last one — a half-written buffer behind a thrown exception, which is precisely what
            // every other guard in this method is arranged to avoid. Two passes over 22 Vector2s
            // costs nothing measurable and keeps the method all-or-nothing.
            for (int i = 0; i < count; i++)
            {
                RequireFiniteAgent(pitchPositions[i], i);
            }

            int possessing = frame.PossessingAgentId;
            float markerRadius = MatchClientConstants.AgentMarkerRadiusM;

            for (int i = 0; i < count; i++)
            {
                LiveAgentCue cue = cues[i];

                destination[i] = new AgentRenderModel(
                    i,
                    roster.TeamId(i),
                    roster.ShirtNumber(i),
                    PitchViewProjection.ToWorld(pitchPositions[i], 0f),
                    markerRadius,
                    i == possessing,
                    cue.IsGoalkeeper,
                    cue.YellowCards,
                    cue.IsSentOff,
                    cue.IsSubstitute);
            }

            return count;
        }

        /// <summary>
        /// Resolves the ball's draw state from the position being drawn this display frame
        /// (normally <see cref="FrameInterpolator.BallAt"/>'s result), in corner-origin metres.
        ///
        /// <para><b>Height and ground position are handled differently, on purpose.</b> A non-finite
        /// or negative HEIGHT places the ball on the turf: a ball cannot be below the ground, the
        /// ground position is still known and still true, and dropping it there loses nothing a
        /// viewer needed. A non-finite GROUND position has no such fallback — there is no "where the
        /// ball is" left to draw — so it is refused fail-loud, the same gate
        /// <c>MatchFrameView</c>'s constructor applies on the screen-facing path.</para>
        ///
        /// <para>No size or offset cue is computed here. The camera is tilted
        /// (<see cref="PitchCameraRig"/>), so height is a real world axis and perspective conveys it;
        /// the shadow supplies the one thing perspective cannot, which is the pitch point the ball is
        /// over.</para>
        /// </summary>
        /// <exception cref="ArgumentException">The ball's X or Y is not finite.</exception>
        public static BallRenderModel ProjectBall(Vector3 pitchBallPosition)
        {
            if (!float.IsFinite(pitchBallPosition.x) || !float.IsFinite(pitchBallPosition.y))
            {
                throw new ArgumentException(
                    "the ball's ground position is not finite: " + pitchBallPosition + ".",
                    nameof(pitchBallPosition));
            }

            Vector3 shadow = PitchViewProjection.ToWorldGround(pitchBallPosition);

            float rawHeight = pitchBallPosition.z;
            float height    = float.IsFinite(rawHeight) && rawHeight > 0f ? rawHeight : 0f;

            var world  = new Vector3(shadow.x, height, shadow.z);
            float radius = MatchClientConstants.BallMarkerRadiusM;

            return new BallRenderModel(world, shadow, rawHeight, radius, radius);
        }

        // Non-finite covers NaN AND ±Infinity — the same gate MatchFrameView applies, and for the
        // same reason: neither can be drawn, and a render loop is the worst place to find out.
        // Nothing upstream refuses them for us; FrameInterpolator explicitly propagates a non-finite
        // position (it treats one as a discontinuity and snaps to it), so this is the gate.
        //
        // The message is composed inside the branch, never per iteration: this runs once per agent
        // per display frame, and building a diagnostic string 22 times a frame to discard it would
        // break the zero-allocation property the class doc claims.
        private static void RequireFiniteAgent(Vector2 position, int index)
        {
            if (float.IsFinite(position.x) && float.IsFinite(position.y)) { return; }

            throw new ArgumentException(
                "pitchPositions[" + Inv(index) + "] is not finite: " + position + ".", "pitchPositions");
        }

        private static void RequireAtLeast(int actual, int required, string what)
        {
            if (actual < required)
            {
                throw new ArgumentException(
                    what + " holds " + Inv(actual) + " slots; the frame carries " + Inv(required) +
                    ". A short projection would leave the remaining slots showing an old frame.",
                    what);
            }
        }

        private static string Inv(int value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): frame + interpolated positions →         |
// |         |            |        | per-agent and ball draw states, allocation-free, fail-loud on   |
// |         |            |        | every shape mismatch.                                           |
// | 1.1     | 2026-08-04 | —      | AR pass M-2/M-3/M-4.                                           |
// |         |            |        | M-2: non-finite agent and ball GROUND positions are refused    |
// |         |            |        | fail-loud. Nothing upstream gated them — LiveMatchStreamer does |
// |         |            |        | not check and FrameInterpolator deliberately PROPAGATES one (it |
// |         |            |        | reads as a discontinuity and snaps to it) — so a NaN would have |
// |         |            |        | reached transform.position while the same frame was refused on |
// |         |            |        | MatchFrameView's screen-facing path. Ball HEIGHT keeps its     |
// |         |            |        | graceful degradation: a bad height still leaves a true ground  |
// |         |            |        | position to draw at, a bad position leaves nothing. The check  |
// |         |            |        | runs as its OWN pass ahead of any write (inside the write loop |
// |         |            |        | it would leave a half-written buffer behind the exception), and |
// |         |            |        | its message is composed inside the throw branch, never per     |
// |         |            |        | iteration.                                                     |
// |         |            |        | M-3: passes HasBall rather than a ring radius.                 |
// |         |            |        | M-4: the cap-below-1 repair branch is gone; MatchClientConstants |
// |         |            |        | refuses one at boot instead.                                   |
#endregion
