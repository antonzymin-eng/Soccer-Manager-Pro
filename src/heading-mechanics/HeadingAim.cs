// File:     src/heading-mechanics/HeadingAim.cs
// Created:  2026-08-09
// Modified: 2026-08-09
// Author:   —
// Spec:     Heading Mechanics #10 §3.5.1 (ERR-010-002), FR-HE-036, FR-HE-037, KD-9, Code Standards #20
// Purpose:  Realizes HeaderIntent.TargetIntent as an achieved contact point on the head surface, so a
//           header is aimed rather than mirrored. Pure geometry; no constants of its own.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// §3.5.1 (ERR-010-002) — the header aim.
    ///
    /// <para>Before ERR-010-002 the outgoing direction was pure specular reflection about
    /// <c>normalize(ballPosition − headCentre)</c>, so a header returned the ball roughly the way it
    /// came and the player had no influence on it whatsoever. §3.5 delegated the aim to Decision Tree
    /// #8 ("selects a contact point on the head surface such that the reflected vector points at the
    /// target"), but #8 cannot emit a header at all — <c>ActionType</c> ordinal 8 overflows the 3-bit
    /// composure-noise field (wiring backlog W9) — so the decision had no owner and
    /// <c>TargetIntent</c> reached no formula.</para>
    ///
    /// <para>#10 takes the derivation back. The aim is realized in three steps, each pure and each
    /// free of new constants:</para>
    /// <list type="number">
    /// <item><description><see cref="ComputeAimDirection"/> — the ballistic launch direction from the
    /// contact point to <c>TargetIntent</c> at the header's own outgoing speed. A destination is
    /// reached by an arc, not a straight line, so aiming along the straight line to a distant ground
    /// point would head the ball into the turf a few metres ahead.</description></item>
    /// <item><description><see cref="ComputeAimNormal"/> — the head-surface normal that reflects the
    /// incoming ball into that direction: the half-vector, which is the exact inverse of §3.5's
    /// reflection. No geometric bound is needed; see that method for why.</description></item>
    /// <item><description><see cref="ComputeAchievedNormal"/> — the achieved normal, the geometric
    /// normal steered toward the aim normal by the player's heading ability (P2: skill as
    /// discrimination fidelity, full-range and continuous — never a cliff).</description></item>
    /// </list>
    ///
    /// <para>The residual between aim and achieved is the §3.4 <c>pointError</c>, which is what makes
    /// that term an execution error for the first time: it was previously the distance between a
    /// hardcoded zero and a geometric fact. A header steered hard away from its natural rebound is
    /// therefore weaker as well as less accurate, which is the football.</para>
    /// </summary>
    public static class HeadingAim
    {
        /// <summary>
        /// Ballistic launch direction (unit) from <paramref name="contactPoint"/> to
        /// <paramref name="target"/> at <paramref name="speed"/>, under gravity alone — the same
        /// gravity-only model §3.8's own-goal projection and §3.2's eligibility scan already use.
        ///
        /// <para>Of the two launch angles that reach a reachable target, the LOW one is taken: a
        /// header is a driven contact, not a lob, and the flat solution also spends least time in the
        /// air. A target beyond ballistic range at this speed is unreachable, and the direction
        /// degrades continuously to the 45° maximum-range launch toward it rather than failing — P1,
        /// continuous never a cliff.</para>
        /// </summary>
        /// <param name="contactPoint">World-space contact point (m), corner-origin.</param>
        /// <param name="target">World-space intended destination (m), corner-origin.</param>
        /// <param name="speed">Outgoing speed (m/s) from FM-010-003.</param>
        /// <returns>Unit launch direction, or <c>Vector3.zero</c> when the geometry is degenerate.</returns>
        public static Vector3 ComputeAimDirection(Vector3 contactPoint, Vector3 target, float speed)
        {
            if (!IsFinite(contactPoint) || !IsFinite(target) || !float.IsFinite(speed))
            {
                return Vector3.zero;
            }

            float dx = target.x - contactPoint.x;
            float dy = target.y - contactPoint.y;
            float range = Mathf.Sqrt(dx * dx + dy * dy);

            // Degenerate: the target is directly above or below the contact point, so there is no
            // horizontal direction to launch along and no aim to realize.
            if (range < HeadingMechanicsConstants.SurfaceNormalEpsilon ||
                speed < HeadingMechanicsConstants.SurfaceNormalEpsilon)
            {
                return Vector3.zero;
            }

            float invRange = 1.0f / range;
            Vector3 flat = new Vector3(dx * invRange, dy * invRange, 0.0f);

            float g = HeadingMechanicsConstants.GravityMps2;
            float dz = target.z - contactPoint.z;
            float vSq = speed * speed;

            // Projectile launch-angle solve: tan(theta) = (v^2 +/- sqrt(v^4 - g(g*R^2 + 2*dz*v^2))) / (g*R).
            float discriminant = vSq * vSq - g * (g * range * range + HeadingMechanicsConstants.KINEMATIC_TWO_COEFF * dz * vSq);

            if (discriminant < 0.0f)
            {
                // Out of ballistic range at this speed. The best available launch toward the target is
                // the maximum-range angle, which for a level target is 45 degrees. Using it here keeps
                // the direction continuous as the target recedes past the reachable boundary.
                return new Vector3(
                    flat.x * HeadingMechanicsConstants.MaxRangeLaunchComponent,
                    flat.y * HeadingMechanicsConstants.MaxRangeLaunchComponent,
                    HeadingMechanicsConstants.MaxRangeLaunchComponent);
            }

            float tanTheta = (vSq - Mathf.Sqrt(discriminant)) / (g * range);

            // direction = normalize(flat + z*tan(theta)); dividing by the shared cos(theta) is exactly
            // this, and avoids an atan/sin/cos round trip.
            Vector3 dir = new Vector3(flat.x, flat.y, tanTheta);
            if (dir.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                return Vector3.zero;
            }

            return dir.normalized;
        }

        /// <summary>
        /// The head-surface normal that reflects <paramref name="incident"/> into
        /// <paramref name="desiredDirection"/> — the half-vector, which is the exact inverse of the
        /// §3.5 reflection formula.
        ///
        /// <para><b>No hemisphere bound is applied, and that is deliberate.</b> The first cut of this
        /// method carried one — "the struck surface must face the oncoming ball, or it is the back of
        /// the player's skull" — and it was removed before landing because it can never fire. For unit
        /// vectors, <c>dot(incident + desired, incident) = 1 + dot(desired, incident) ≥ 0</c>, so the
        /// half-vector is always in the forward hemisphere already. The equivalent statement from the
        /// other side: with <c>c = dot(incident, normal) ∈ [0, 1]</c>, the reflected direction satisfies
        /// <c>dot(reflected, incident) = 2c² − 1</c>, which sweeps the whole of [−1, 1] — every outgoing
        /// direction is producible off the front of the head. A guard on an unreachable branch ships
        /// green precisely because nothing can fire it, which this project has filed as a defect class
        /// in its own right, so it is recorded here rather than written.</para>
        ///
        /// <para>What genuinely bounds the aim is therefore not geometry but the player: how far he can
        /// meet the ball with the part of his head he wants to, which is
        /// <see cref="ComputeAchievedNormal"/>'s attribute-graded blend. The one case with no solution
        /// at all is <paramref name="desiredDirection"/> exactly opposite to
        /// <paramref name="incident"/> — asking for the ball to carry on through the head untouched —
        /// and that returns zero.</para>
        /// </summary>
        /// <param name="incident">Unit vector back along the incoming ball's path (−v̂), per §3.5.</param>
        /// <param name="desiredDirection">Unit desired outgoing direction.</param>
        /// <returns>Unit aim normal, or <c>Vector3.zero</c> when degenerate.</returns>
        public static Vector3 ComputeAimNormal(Vector3 incident, Vector3 desiredDirection)
        {
            if (!IsFinite(incident) || !IsFinite(desiredDirection))
            {
                return Vector3.zero;
            }

            Vector3 half = incident + desiredDirection;
            if (half.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                // desiredDirection is the exact reverse of incident: the ball would have to pass
                // through the head unchanged, which no reflection off a sphere produces.
                return Vector3.zero;
            }

            return half.normalized;
        }

        /// <summary>
        /// The achieved head-surface normal: the geometric normal steered toward the aim normal by the
        /// player's heading ability.
        ///
        /// <para><paramref name="steerAuthority"/> is the normalized Heading attribute, so the ramp
        /// spans the whole attribute range with no plateau at either end — raw 1 barely deflects the
        /// ball off its natural rebound, raw 20 places it. That is the FULL-RANGE ramp shape
        /// ERR-008-019 settled on, and it means the aim is skill, not a switch (P1/P2). No new
        /// constant: the attribute IS the dial.</para>
        ///
        /// <para>An <paramref name="aimNormal"/> of zero (degenerate aim) returns the geometric normal
        /// unchanged, which is the pre-ERR-010-002 behaviour — so an unaimable header still plays.</para>
        /// </summary>
        /// <param name="geometricNormal">Unit normal from head centre toward the ball (today's model).</param>
        /// <param name="aimNormal">Unit achievable aim normal from <see cref="ComputeAimNormal"/>.</param>
        /// <param name="steerAuthority">Normalized heading ability [0, 1].</param>
        /// <returns>Unit achieved normal.</returns>
        public static Vector3 ComputeAchievedNormal(
            Vector3 geometricNormal,
            Vector3 aimNormal,
            float steerAuthority)
        {
            if (!IsFinite(geometricNormal) ||
                geometricNormal.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                return geometricNormal;
            }

            if (!IsFinite(aimNormal) ||
                aimNormal.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ ||
                !float.IsFinite(steerAuthority))
            {
                return geometricNormal;
            }

            float authority = Mathf.Clamp01(steerAuthority);
            Vector3 blended = geometricNormal + authority * (aimNormal - geometricNormal);

            if (blended.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                // geometricNormal and aimNormal are exactly opposed and the blend passes through the
                // head centre. The player cannot get round the ball; he plays the natural rebound.
                return geometricNormal;
            }

            return blended.normalized;
        }

        private static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                            |
// | 1.0     | 2026-08-09 | —      | Initial implementation. ERR-010-002: §3.5's aim was delegated to Decision Tree    |
// |         |            |        |   #8, which structurally cannot emit a header (W9), so TargetIntent reached no    |
// |         |            |        |   formula and every header was a passive specular mirror. Three pure steps —      |
// |         |            |        |   ballistic launch direction to the target, the half-vector normal that realizes  |
// |         |            |        |   it bounded to the physically reachable hemisphere, and an attribute-steered     |
// |         |            |        |   blend from the geometric normal. No new constants: the Heading attribute is     |
// |         |            |        |   the dial (FULL-RANGE ramp, ERR-008-019 shape).                                  |
#endregion
