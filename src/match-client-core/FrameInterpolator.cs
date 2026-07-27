// File:     src/match-client-core/FrameInterpolator.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P3, §7
//           "Interpolation"), Code Standards #20
// Purpose:  Pure math for rendering between the last two captured live frames, so motion is smooth
//           despite the 10 Hz tactical / 60 Hz physics cadence. A View concern only — nothing here
//           is fed back into the simulation.

using System;

using UnityEngine;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Blends two captured <see cref="LiveMatchFrame"/>s into the positions a renderer draws this
    /// display frame (§5-P3).
    ///
    /// <para><b>The View renders one frame interval behind, on purpose.</b> Blending between the two
    /// most recent captures is interpolation; blending past the newest one is extrapolation, which
    /// invents positions the simulation never produced and then visibly corrects them when the real
    /// frame arrives. A fixed sub-frame of latency nobody can perceive is the better trade.</para>
    ///
    /// <para><b>Discontinuities are snapped, not blended.</b> Restarts teleport the ball and
    /// substitutions swap who occupies a roster slot; interpolating across either draws a smooth
    /// glide where the truth is a jump. Displacements past the
    /// <c>MatchClientConstants.*SnapDistanceM</c> thresholds take the newer frame outright.</para>
    ///
    /// <para>Pure and allocation-free: agent output is written into a caller-supplied buffer. Nothing
    /// here reads a clock or an engine.</para>
    /// </summary>
    public static class FrameInterpolator
    {
        /// <summary>
        /// Blend factor for a render happening <paramref name="secondsSinceCurrentFrame"/> after the
        /// newer frame was captured: 0 draws the older frame, 1 the newer.
        ///
        /// <para>Fails to 1 — the freshest state the client actually knows — for every degenerate
        /// input (non-advancing ticks, a non-positive tick rate, a non-finite elapsed time). Failing
        /// to 0 would rewind the render, and propagating a NaN would put agents nowhere at all.</para>
        /// </summary>
        /// <param name="previousTick">Tick of the older captured frame.</param>
        /// <param name="currentTick">Tick of the newer captured frame.</param>
        /// <param name="secondsSinceCurrentFrame">Wall-clock seconds since the newer frame arrived.</param>
        /// <param name="effectiveTicksPerSecond">
        /// Simulation ticks per wall-clock second AS PACED — i.e. the engine tick rate times the
        /// streamer's speed multiplier. At 3× the same tick interval passes in a third of the time,
        /// and an interpolator handed the unscaled rate would run a third as fast as the sim and lag
        /// further behind every frame.
        /// </param>
        public static float ComputeAlpha(
            ulong previousTick,
            ulong currentTick,
            float secondsSinceCurrentFrame,
            float effectiveTicksPerSecond)
        {
            if (currentTick <= previousTick) return 1f;
            if (!(effectiveTicksPerSecond > 0f) || float.IsInfinity(effectiveTicksPerSecond)) return 1f;
            if (!float.IsFinite(secondsSinceCurrentFrame)) return 1f;
            if (secondsSinceCurrentFrame <= 0f) return 0f;

            float intervalSeconds = (currentTick - previousTick) / effectiveTicksPerSecond;
            if (!(intervalSeconds > 0f)) return 1f;

            return Mathf.Clamp01(secondsSinceCurrentFrame / intervalSeconds);
        }

        /// <summary>
        /// Ball position to draw. Snaps to <paramref name="current"/> when the two frames disagree by
        /// more than <c>MatchClientConstants.BallSnapDistanceM</c> (a restart) or when either position
        /// is non-finite.
        /// </summary>
        public static Vector3 BallAt(in LiveMatchFrame previous, in LiveMatchFrame current, float alpha)
        {
            Vector3 a = previous.BallPosition;
            Vector3 b = current.BallPosition;

            if (!IsFinite(a) || !IsFinite(b)) return b;

            float snap = MatchClientConstants.BallSnapDistanceM;
            if ((b - a).sqrMagnitude > snap * snap) return b;

            return Vector3.Lerp(a, b, SanitiseAlpha(alpha));
        }

        /// <summary>
        /// Writes each agent's drawn position into <paramref name="destination"/>.
        ///
        /// <para>Snaps a slot to <paramref name="current"/> when it moved further than
        /// <c>MatchClientConstants.AgentSnapDistanceM</c> — a substitution changed who is in it — and
        /// snaps EVERY slot when the two frames carry different roster lengths, since slot <c>i</c> in
        /// one is then not necessarily slot <c>i</c> in the other and blending them pairs up
        /// unrelated players.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">A frame carries no agent array, or destination is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="destination"/> is shorter than the newer frame's roster — writing a partial
        /// set would leave stale positions behind for the remaining slots.
        /// </exception>
        public static void AgentsAt(
            in LiveMatchFrame previous, in LiveMatchFrame current, float alpha, Vector2[] destination)
        {
            Vector2[] a = previous.AgentPositions;
            Vector2[] b = current.AgentPositions;

            if (a == null) throw new ArgumentNullException(nameof(previous), "previous.AgentPositions is null.");
            if (b == null) throw new ArgumentNullException(nameof(current), "current.AgentPositions is null.");
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            if (destination.Length < b.Length)
            {
                throw new ArgumentException(
                    "destination holds " + destination.Length + " slots; the current frame carries " +
                    b.Length + ". A short write would leave the remaining slots showing an old frame.",
                    nameof(destination));
            }

            bool rosterChanged = a.Length != b.Length;
            float t = SanitiseAlpha(alpha);
            float snapSq = MatchClientConstants.AgentSnapDistanceM * MatchClientConstants.AgentSnapDistanceM;

            for (int i = 0; i < b.Length; i++)
            {
                Vector2 to = b[i];

                if (rosterChanged || !IsFinite(to))
                {
                    destination[i] = to;
                    continue;
                }

                Vector2 from = a[i];
                if (!IsFinite(from) || (to - from).sqrMagnitude > snapSq)
                {
                    destination[i] = to;
                    continue;
                }

                destination[i] = Vector2.Lerp(from, to, t);
            }
        }

        /// <summary>Non-finite or out-of-range alpha collapses to the newer frame (see ComputeAlpha).</summary>
        private static float SanitiseAlpha(float alpha)
        {
            if (!float.IsFinite(alpha)) return 1f;
            return Mathf.Clamp01(alpha);
        }

        private static bool IsFinite(Vector3 v) =>
            float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        private static bool IsFinite(Vector2 v) =>
            float.IsFinite(v.x) && float.IsFinite(v.y);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P3): pure frame-blend math — speed-aware     |
// |         |            |        | alpha, ball and per-agent blending with snap-on-discontinuity  |
// |         |            |        | for restarts and substitutions, all failing to the newer frame.|
#endregion
