// File:     src/match-client-core/MatchRenderProjection.cs
// Created:  2026-08-03
// Modified: 2026-08-16 (P4b AR round 5, M23: ProjectBall's M17 floor is raised from the drawn radius
//           to the drawn radius plus the topmost M12/M16 ground layer, which round 4's M19 rescale
//           had left passing through the ball)
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
        /// <para><b>M17/M23: the drawn ball's world Y is floored on its DRAWN radius PLUS the topmost
        /// ground layer, not on its raw height.</b> <see cref="BallRenderModel.Radius"/> is
        /// <c>MatchClientConstants.BallMarkerRadiusM</c> — a legibility figure (0.35 m by default), not
        /// the engine's physical ball radius (0.11 m at rest, Ball Physics #1 §1.2/Appendix C) — and the
        /// prefab contract scales the ball uniformly on all three axes by that radius
        /// (<c>src/match-client-unity/README.md</c> §1 clause 2b), so it
        /// is drawn as a genuine sphere of that size centred on
        /// <see cref="BallRenderModel.WorldPosition"/>. Centring that sphere on the engine's raw,
        /// physically-correct height sinks its lower hemisphere below the turf whenever the raw height
        /// is under the drawn radius — which is most of a match, including its single most common state,
        /// resting on the ground at 0.11 m.</para>
        ///
        /// <para>The exact formula is
        /// <c>Y == max(HeightM sanitised to non-negative, Radius + MatchClientConstants.AgentMarkerLayerHeightM)</c>.
        /// M17 originally floored on <c>Radius</c> alone, which cleared the bare ground PLANE — but the
        /// M12/M16 ground layers do not sit on that plane, and round 4's M19 rescaled all four of them
        /// from millimetres to centimetres (marking band base 0 m, ball shadow 0.08 m, possession ring
        /// 0.081 m, agent marker 0.082 m by default). A floor of <c>Radius</c> alone leaves the drawn
        /// sphere's underside at world Y = 0 while the shadow it casts is drawn at 0.08 m and the agent
        /// marker at 0.082 m — i.e. every one of those layers passes visibly THROUGH the ball at rest.
        /// Adding <see cref="MatchClientConstants.AgentMarkerLayerHeightM"/>, the HIGHEST of the four
        /// ordered layers, floors the ball's CENTRE high enough that its lowest point clears all of them,
        /// not merely the turf. It is added to the floor only — a ball genuinely above that height rides
        /// on its own physics height untouched.</para>
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

            float rawHeight       = pitchBallPosition.z;
            float sanitizedHeight = float.IsFinite(rawHeight) && rawHeight > 0f ? rawHeight : 0f;
            float radius          = MatchClientConstants.BallMarkerRadiusM;

            // M17/M23: floored on the DRAWN radius PLUS the topmost ground layer (not zero, and not
            // the radius alone), so a sphere of that radius centred at world Y never dips below the
            // highest thing drawn on the turf. Its lowest point is worldY - radius, which this makes
            // >= AgentMarkerLayerHeightM unconditionally.
            //
            // M17 floored on the radius alone, which cleared the bare ground PLANE. That was already
            // only just enough when the four M12 ground layers were millimetre-scale, and M19 (round 4)
            // rescaled them to centimetres — the shadow now sits at 0.08 m and the agent marker at
            // 0.082 m by default, both INSIDE a 0.35 m-radius sphere whose centre is floored at 0.35 m.
            // AgentMarkerLayerHeightM is the highest of the four, so clearing it clears all of them.
            float worldY = Mathf.Max(sanitizedHeight, radius + MatchClientConstants.AgentMarkerLayerHeightM);

            var world = new Vector3(shadow.x, worldY, shadow.z);

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
// | 1.2     | 2026-08-04 | —      | Tilted-view revision (owner call): HeightScale is DELETED and   |
// |         |            |        | positions are projected with ToWorld / ToWorldGround instead of |
// |         |            |        | the flat view plane. The ball is placed at its real height on   |
// |         |            |        | world Y and the camera conveys altitude, so no sprite lift and  |
// |         |            |        | no size ramp are computed here at all; the shadow stays,        |
// |         |            |        | because perspective cannot say which pitch point the ball is    |
// |         |            |        | over. Ball HEIGHT keeps its graceful degradation from v1.1.     |
// |         |            |        | (Row written 2026-08-04 in the following AR pass — the v1.2     |
// |         |            |        | edit landed without one, leaving v1.1 as the newest row while   |
// |         |            |        | describing a HeightScale the file no longer had.)               |
// | 1.3     | 2026-08-16 | —      | P4b AR round 3, M17: ProjectBall's world Y is now floored on    |
// |         |            |        | the DRAWN radius (BallMarkerRadiusM, a legibility figure) via   |
// |         |            |        | Mathf.Max, not on zero. BallRenderModel.Radius is 0.35 m by     |
// |         |            |        | default, not the engine's physical 0.11 m rest height, and the  |
// |         |            |        | prefab contract scales the ball uniformly by that radius (clause|
// |         |            |        | 2b) — so a sphere of that radius centred on the RAW physics     |
// |         |            |        | height had its lower hemisphere below the turf at every height  |
// |         |            |        | under the radius, including rest, its single most common state, |
// |         |            |        | visibly sinking through the ground/markings/M12 layers and       |
// |         |            |        | swallowing its own shadow. HeightM and ShadowPosition are        |
// |         |            |        | untouched — HeightM stays the engine's raw unsanitised height,   |
// |         |            |        | ShadowPosition stays the true ground point.                      |
// | 1.4     | 2026-08-16 | —      | P4b AR round 5, M23: the M17 floor above cleared only the bare   |
// |         |            |        | ground PLANE, and round 4's M19 rescaled the four M12/M16 ground |
// |         |            |        | layers from millimetres to centimetres (shadow 0.08 m,           |
// |         |            |        | possession ring 0.081 m, agent marker 0.082 m) without moving    |
// |         |            |        | the ball's floor with them — so at rest a 0.35 m sphere centred  |
// |         |            |        | at 0.35 m had its own shadow, the agent marker and the marking   |
// |         |            |        | band all passing visibly THROUGH it. The floor is now            |
// |         |            |        | radius + AgentMarkerLayerHeightM (the HIGHEST of the four        |
// |         |            |        | layers, so clearing it clears all of them). Floor only — a ball  |
// |         |            |        | genuinely above that height still rides on its raw physics       |
// |         |            |        | height, and HeightM/ShadowPosition are again untouched.          |
#endregion
