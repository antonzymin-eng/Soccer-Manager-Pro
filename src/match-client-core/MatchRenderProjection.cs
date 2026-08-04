// File:     src/match-client-core/MatchRenderProjection.cs
// Created:  2026-08-03
// Modified: 2026-08-03
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
    /// <para><b>Fail-loud on shape.</b> A buffer that is too short, a cue array that does not match
    /// its positions, a roster smaller than the frame — each is a wiring bug that would otherwise
    /// surface as agents drawn at stale positions or annotated with another player's cards. The
    /// render loop is the worst place to find that out quietly, which is the same reasoning
    /// <c>MatchFrameView</c>'s constructor gates on.</para>
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
        /// is smaller than the frame's roster.
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

            int possessing = frame.PossessingAgentId;
            float markerRadius = MatchClientConstants.AgentMarkerRadiusM;
            float ringRadius   = MatchClientConstants.PossessionRingRadiusM;

            for (int i = 0; i < count; i++)
            {
                LiveAgentCue cue = cues[i];

                destination[i] = new AgentRenderModel(
                    i,
                    roster.TeamId(i),
                    roster.ShirtNumber(i),
                    PitchViewProjection.ToView(pitchPositions[i]),
                    markerRadius,
                    i == possessing ? ringRadius : 0f,
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
        /// <para>A non-finite or negative height is treated as ground level: a ball cannot be below
        /// the turf, and a NaN lift would put the sprite nowhere while the shadow stayed correct —
        /// the one failure mode where drawing something slightly wrong beats drawing nothing. The
        /// ground position is passed through untouched; the producers upstream refuse to publish a
        /// non-finite coordinate at all.</para>
        /// </summary>
        public static BallRenderModel ProjectBall(Vector3 pitchBallPosition)
        {
            Vector2 shadow = PitchViewProjection.ToViewGround(pitchBallPosition);

            float rawHeight = pitchBallPosition.z;
            float height    = float.IsFinite(rawHeight) && rawHeight > 0f ? rawHeight : 0f;

            Vector2 sprite = new Vector2(
                shadow.x,
                shadow.y + height * MatchClientConstants.BallHeightViewOffsetPerMetre);

            float baseRadius = MatchClientConstants.BallMarkerRadiusM;
            float scale      = HeightScale(height);

            return new BallRenderModel(shadow, sprite, rawHeight, baseRadius * scale, baseRadius);
        }

        /// <summary>
        /// Ball-sprite scale for a height in metres: 1 at ground level, growing by
        /// <c>BallHeightScalePerMetre</c> per metre, capped at <c>BallMaxHeightScale</c>. Exposed
        /// because it is the curve the tests assert against rather than one sampled value of it.
        /// </summary>
        public static float HeightScale(float heightM)
        {
            if (!float.IsFinite(heightM) || heightM <= 0f) { return 1f; }

            float scale = 1f + heightM * MatchClientConstants.BallHeightScalePerMetre;
            float cap   = MatchClientConstants.BallMaxHeightScale;

            // A cap below 1 would shrink a grounded ball; treat a misconfigured cap as no cap
            // rather than letting a config typo make the ball vanish.
            if (!float.IsFinite(cap) || cap < 1f) { return scale; }

            return Mathf.Min(scale, cap);
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
#endregion
