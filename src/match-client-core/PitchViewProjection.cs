// File:     src/match-client-core/PitchViewProjection.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Coordinate mapping"), Ball Physics #1 §1.2 (corner-origin frame), Code Standards #20
// Purpose:  The one documented adapter between the engine's corner-origin pitch metres and the
//           client's centre-origin world, in both directions — including the ray/ground-plane
//           intersection a pointer click needs under a tilted camera. Host-free so it is gate-tested.

using UnityEngine;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Maps between the two coordinate frames the client straddles (§7 "Coordinate mapping").
    ///
    /// <para><b>Engine frame (the input).</b> Corner-origin metres, Ball Physics #1 §1.2: X 0–105
    /// goal-to-goal, Y 0–68 touchline-to-touchline, Z up from the ground. Every position in a
    /// <c>LiveMatchFrame</c>, every camera target from <see cref="FollowBallCamera"/>, and every
    /// marking from <see cref="PitchMarkings"/> is in this frame.</para>
    ///
    /// <para><b>World frame (the output).</b> Unity's convention — <b>+X along the pitch's length,
    /// +Z across its width, +Y up</b> — centred on the centre spot, at one world unit per metre. So
    /// the pitch is the rectangle [−52.5, +52.5] × [−34, +34] on the <b>y = 0 ground plane</b>, and
    /// ball height is a genuine third axis rather than something faked.</para>
    ///
    /// <para><b>Note the axis swap.</b> The engine's Y (across the pitch) becomes the world's Z, and
    /// the engine's Z (up) becomes the world's Y. Getting that backwards lays the pitch on its side,
    /// which is exactly why the conversion lives at one site with a round-trip test rather than being
    /// written out at each call — "wrong coordinate origin" is already one of this project's recorded
    /// recurring traps, and an axis swap is the same trap wearing a different hat.</para>
    ///
    /// <para><b>Why centre-origin.</b> It makes the frame symmetric about both axes, so a home-end
    /// position and its away-end mirror differ only in sign — which is what makes the mirrored
    /// assertions in the tests cheap to write, and this repo has shipped three home/away asymmetry
    /// defects (#8 ERR-008-002) for want of them.</para>
    ///
    /// <para><b>Two mappings, and they are inverses.</b> <see cref="ToWorld"/> places a pitch position
    /// in the world; <see cref="TryGroundHit"/> brings a world-space ray back to a pitch position.
    /// An earlier flat-view pair (<c>ToView</c>/<c>ToPitch</c>, both 2D) was kept alongside them
    /// through the tilted-view revision and had no caller left: <c>ToView</c> was
    /// <see cref="ToWorld"/> with the height dropped, and the inverse a click needs is a ray
    /// intersection, not a subtraction. A second name for one mapping is the drift this class exists
    /// to prevent, so they are gone.</para>
    ///
    /// <para>Pure, stateless, allocation-free. The forward mappings do not gate their inputs —
    /// <see cref="MatchRenderProjection"/> refuses a non-finite position before it gets here, and
    /// re-checking would put a branch on the per-agent render path. <see cref="TryGroundHit"/> is the
    /// exception: it takes a ray from outside this assembly and reports failure rather than
    /// producing a garbage pitch position.</para>
    /// </summary>
    public static class PitchViewProjection
    {
        /// <summary>Half the pitch's goal-to-goal extent (m) — the world-X origin shift.</summary>
        public static float HalfLengthM => MatchEngineConstants.PITCH_LENGTH_M * 0.5f;

        /// <summary>Half the pitch's touchline-to-touchline extent (m) — the world-Z origin shift.</summary>
        public static float HalfWidthM => MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

        /// <summary>
        /// Places a pitch position at a given height in the world frame: pitch X → world X, pitch Y →
        /// world Z, and <paramref name="heightM"/> → world Y. This is what a renderer assigns to
        /// <c>transform.position</c>.
        /// </summary>
        /// <param name="pitchXY">Position in the pitch plane, corner-origin metres.</param>
        /// <param name="heightM">Height above the turf in metres; 0 is on the ground.</param>
        public static Vector3 ToWorld(Vector2 pitchXY, float heightM)
        {
            return new Vector3(pitchXY.x - HalfLengthM, heightM, pitchXY.y - HalfWidthM);
        }

        /// <summary>
        /// Places the ground point beneath a ball position — the pitch point it is actually over,
        /// with its height discarded — on the world's ground plane. This is where the shadow goes,
        /// and it is the position every gameplay judgement was made against.
        /// </summary>
        public static Vector3 ToWorldGround(Vector3 pitchXYZ)
        {
            return new Vector3(pitchXYZ.x - HalfLengthM, 0f, pitchXYZ.y - HalfWidthM);
        }

        /// <summary>
        /// Intersects a world-space ray with the ground plane and reports where it lands in pitch
        /// metres. This is the direction a pointer click travels: a screen point becomes a ray
        /// (Unity's <c>Camera.ScreenPointToRay</c>), and the ray becomes a pitch position here.
        ///
        /// <para><b>Why a ray rather than an inverse offset.</b> Under the tilted camera the client
        /// uses, screen position is not an affine function of pitch position — the same pixel maps to
        /// a different metre depending on depth. A subtraction cannot express that; a ray/plane
        /// intersection can, and it is pure math, so it stays on this side of the boundary where the
        /// CI gate can test it. <c>Camera</c> itself is not in the shim and never will be, so the
        /// Unity side supplies the ray and decides nothing.</para>
        /// </summary>
        /// <param name="rayOrigin">Ray origin in world space (the camera's position).</param>
        /// <param name="rayDirection">Ray direction in world space. Need not be normalised.</param>
        /// <param name="pitchXY">Where the ray meets the ground plane, in corner-origin metres.</param>
        /// <returns>
        /// False — with <paramref name="pitchXY"/> set to <c>Vector2.zero</c> — when the ray cannot
        /// meet the ground ahead of its origin: a non-finite input, a direction parallel to the
        /// ground, or a hit behind the origin (looking up, or a camera below the turf). The result is
        /// NOT required to be on the pitch: a click past the touchline is a real answer, and
        /// <see cref="IsOnPitch"/> is the separate question.
        /// </returns>
        public static bool TryGroundHit(Vector3 rayOrigin, Vector3 rayDirection, out Vector2 pitchXY)
        {
            pitchXY = Vector2.zero;

            if (!IsFinite(rayOrigin) || !IsFinite(rayDirection)) { return false; }

            // Ground plane is y = 0, so the parameter along the ray is where its Y reaches zero.
            // A zero (or denormal) Y direction is a ray running parallel to the turf: no intersection
            // exists, and dividing would hand back an infinity dressed up as a position.
            if (rayDirection.y == 0f) { return false; }

            float t = -rayOrigin.y / rayDirection.y;
            if (!float.IsFinite(t) || t <= 0f) { return false; }

            float worldX = rayOrigin.x + rayDirection.x * t;
            float worldZ = rayOrigin.z + rayDirection.z * t;

            if (!float.IsFinite(worldX) || !float.IsFinite(worldZ)) { return false; }

            pitchXY = new Vector2(worldX + HalfLengthM, worldZ + HalfWidthM);
            return true;
        }

        /// <summary>
        /// True when a pitch-plane position lies within the touchlines and goal lines, boundaries
        /// included. A loose ball can legitimately be outside them between a boundary exit and the
        /// restart, so this reports rather than refuses.
        /// </summary>
        public static bool IsOnPitch(Vector2 pitchXY)
        {
            return pitchXY.x >= 0f && pitchXY.x <= MatchEngineConstants.PITCH_LENGTH_M
                && pitchXY.y >= 0f && pitchXY.y <= MatchEngineConstants.PITCH_WIDTH_M;
        }

        private static bool IsFinite(Vector3 v) =>
            float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): corner-origin ⇄ centre-origin view      |
// |         |            |        | mapping at 1 unit per metre, both directions, plus the         |
// |         |            |        | on-pitch predicate. The single site §7 requires the mapping to |
// |         |            |        | live at.                                                       |
// | 1.1     | 2026-08-04 | —      | Tilted-view revision (owner call): the view is a 3D scene under |
// |         |            |        | a tilted camera, not a flat plane, so height is a real axis.    |
// |         |            |        | + ToWorld(pitch, height) and ToWorldGround — pitch X → world X, |
// |         |            |        | pitch Y → world Z, height → world Y — and + TryGroundHit, the   |
// |         |            |        | ray/ground-plane intersection a click needs now that screen     |
// |         |            |        | position is no longer affine in pitch position. ToViewGround    |
// |         |            |        | (Vector3 → Vector2) is replaced by ToWorldGround; ToView /      |
// |         |            |        | ToPitch stay as the plan-view pair. Camera is not in the CI     |
// |         |            |        | shim, so the Unity side supplies the ray and decides nothing.   |
// | 1.2     | 2026-08-04 | —      | AR pass 2, M-2: ToView / ToPitch DELETED. v1.1 kept them "as   |
// |         |            |        | the plan-view pair" and the revision left them with no          |
// |         |            |        | production caller — ToView was ToWorld with the height dropped, |
// |         |            |        | and the inverse a click needs is TryGroundHit, not a            |
// |         |            |        | subtraction. Two names for one mapping is precisely the drift   |
// |         |            |        | this single-site class exists to prevent. Their tests re-anchor |
// |         |            |        | onto ToWorld / ToWorldGround, which cover the same origin shift,|
// |         |            |        | scale and round trip.                                           |
#endregion
