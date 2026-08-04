// File:     src/match-client-core/PitchViewProjection.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Coordinate mapping"), Ball Physics #1 §1.2 (corner-origin frame), Code Standards #20
// Purpose:  The one documented adapter between the engine's corner-origin pitch metres and the
//           client's centre-origin view plane, in both directions. Host-free so the mapping — and
//           its inverse, which turns a click back into a pitch position — is gate-tested.

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
    /// <para><b>View frame (the output).</b> Centre-origin, one view unit per metre, +X along the
    /// pitch's length and +Y across its width — so the pitch spans [−52.5, +52.5] × [−34, +34] and
    /// the centre spot sits on the origin. The scale is deliberately 1:1: a renderer that wants the
    /// pitch bigger moves the camera, and keeping metres as the unit means an on-screen distance can
    /// be reasoned about against the engine's own constants without a conversion in the way.</para>
    ///
    /// <para><b>Why re-origin at all, rather than passing metres straight through.</b> "Wrong
    /// coordinate origin" is one of this project's recorded recurring traps, and the defence is a
    /// single conversion site rather than an assumption repeated at every call. Centring also makes
    /// the frame symmetric about both axes, so a home-end position and its away-end mirror differ
    /// only in sign — which is what makes the mirrored assertions in the tests cheap to write, and
    /// this repo has shipped three home/away asymmetry defects (#8 ERR-008-002) for want of them.</para>
    ///
    /// <para>Pure, stateless, allocation-free. Inputs are not gated: a <c>LiveMatchFrame</c> is
    /// NaN-gated where it is produced and again at <c>MatchFrameView</c>, and re-checking here would
    /// put a branch on the per-agent render path to catch something two layers have already
    /// refused. Non-finite in, non-finite out.</para>
    /// </summary>
    public static class PitchViewProjection
    {
        /// <summary>Half the pitch's goal-to-goal extent (m) — the X origin shift.</summary>
        public static float HalfLengthM => MatchEngineConstants.PITCH_LENGTH_M * 0.5f;

        /// <summary>Half the pitch's touchline-to-touchline extent (m) — the Y origin shift.</summary>
        public static float HalfWidthM => MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

        /// <summary>
        /// Projects a pitch-plane position (corner-origin metres) into the centre-origin view plane.
        /// </summary>
        public static Vector2 ToView(Vector2 pitchXY)
        {
            return new Vector2(pitchXY.x - HalfLengthM, pitchXY.y - HalfWidthM);
        }

        /// <summary>
        /// Projects the ground point beneath a ball position into the view plane. The Z component is
        /// dropped here on purpose — height is a separate render cue (<see cref="BallRenderModel"/>),
        /// not a displacement of where the ball actually is.
        /// </summary>
        public static Vector2 ToViewGround(Vector3 pitchXYZ)
        {
            return new Vector2(pitchXYZ.x - HalfLengthM, pitchXYZ.y - HalfWidthM);
        }

        /// <summary>
        /// Inverse of <see cref="ToView"/>: turns a view-plane point back into corner-origin pitch
        /// metres. This is the direction a pointer click travels — a screen position becomes a world
        /// position becomes a pitch position — so it exists now, with the forward mapping and its
        /// round-trip test, rather than being re-derived by whoever first wires up input.
        /// </summary>
        public static Vector2 ToPitch(Vector2 viewXY)
        {
            return new Vector2(viewXY.x + HalfLengthM, viewXY.y + HalfWidthM);
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): corner-origin ⇄ centre-origin view      |
// |         |            |        | mapping at 1 unit per metre, both directions, plus the         |
// |         |            |        | on-pitch predicate. The single site §7 requires the mapping to |
// |         |            |        | live at.                                                       |
#endregion
