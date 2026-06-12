// File:     src/ball-physics/BallGroundInteraction.cs
// Created:  2026-05-24
// Modified: 2026-06-09 (AR-7 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Impulse-based ground bounce and rolling-friction force calculations.
//           Ground normal is always UP for regulation football pitches.

using UnityEngine;
using Unity.Profiling;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Ground contact physics: impulse-based bounce and rolling friction.
    /// </summary>
    public static class BallGroundInteraction
    {
        private static readonly ProfilerMarker s_bounceMarker =
            new ProfilerMarker("BallPhysics.Bounce");

        /// <summary>
        /// Handles ball bounce on ground contact using impulse-based contact mechanics.
        /// Ground normal is always UP (+Z) for regulation football pitches
        /// (FIFA allows max 1% slope for drainage — negligible).
        /// PRECONDITIONS: caller is the Bouncing branch of UpdateBallPhysics — the state
        /// machine guarantees Velocity.z &lt; 0 on entry (restitution would amplify an
        /// upward Velocity.z) and ground proximity (Position.z is snapped to RADIUS
        /// unconditionally; invoking this on a high airborne ball teleports it down).
        /// </summary>
        public static void ApplyBounce(
            ref BallState ball,
            SurfaceType surface,
            BallEventLogger logger,
            float matchTime)
        {
            using var _ = s_bounceMarker.Auto();

            // +Z is up in this project (Ball Physics #1 §1.2 / Appendix C).
            // Unity's Vector3.up is +Y — the touchline axis here — and using it
            // reflected lateral velocity instead of vertical (AR-7 H-1 / ERR-001-001).
            Vector3 normal = new Vector3(0f, 0f, 1f);

            float e             = SurfaceProperties.GetCoefficientOfRestitution(surface);
            float mu            = SurfaceProperties.GetFrictionCoefficient(surface);
            float spinRetention = SurfaceProperties.GetSpinRetention(surface);

            Vector3 v     = ball.Velocity;
            Vector3 omega = ball.AngularVelocity;
            float   r     = BallPhysicsConstants.Ball.RADIUS;
            float   m     = BallPhysicsConstants.Ball.MASS;

            // Decompose velocity into normal and tangent components.
            float   vn = Vector3.Dot(v, normal);   // negative = into ground
            Vector3 vt = v - vn * normal;

            // Contact point velocity: v_contact = vt + ω × r_contact
            Vector3 rContact         = -r * normal;
            Vector3 spinContribution = Vector3.Cross(omega, rContact);
            Vector3 contactVelocity  = vt + spinContribution;

            // Normal impulse (restitution).
            float vn_after = -e * vn;

            // Tangential friction impulse.
            float   J_n     = (1f + e) * m * Mathf.Abs(vn);
            float   J_t_max = mu * J_n;
            Vector3 vt_after = vt;

            if (contactVelocity.magnitude > BallPhysicsConstants.Bounce.MinContactSpeed)
            {
                // Friction changes both v_t and ω, so the impulse that zeroes
                // contact-point slip is m·|v_contact| / (1 + m·r²/I) — the undivided
                // form would reverse the slip by ~150% for a hollow sphere
                // (AR-7 M-1 / ERR-001-002).
                float   J_t_required   = m * contactVelocity.magnitude
                                       / BallPhysicsConstants.Bounce.StickImpulseCouplingDivisor;
                float   J_t            = Mathf.Min(J_t_max, J_t_required);
                Vector3 frictionDir    = -contactVelocity.normalized;
                Vector3 frictionImpulse = frictionDir * J_t;

                vt_after = vt + frictionImpulse / m;

                Vector3 angularImpulse = Vector3.Cross(rContact, frictionImpulse);
                omega += angularImpulse / BallPhysicsConstants.Ball.MomentOfInertia;
            }

            ball.Velocity        = vt_after + vn_after * normal;
            ball.AngularVelocity = omega * spinRetention;
            ball.Position        = new Vector3(ball.Position.x, ball.Position.y, r);

            logger?.LogBounce(ball, surface, e, vn, vn_after, matchTime);
        }

        /// <summary>
        /// Calculates rolling friction deceleration force.
        /// Only applied when ball state is Rolling.
        /// </summary>
        public static Vector3 CalculateRollingFriction(Vector3 velocity, SurfaceType surface)
        {
            float speed = velocity.magnitude;
            if (speed < BallPhysicsConstants.State.MinVelocity)
                return Vector3.zero;

            float mu_r           = SurfaceProperties.GetRollingResistance(surface);
            float forceMagnitude = mu_r
                                 * BallPhysicsConstants.Ball.MASS
                                 * BallPhysicsConstants.Environment.GRAVITY;

            return -velocity.normalized * forceMagnitude;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; ALL_CAPS       |
// |         |            |        | constant refs → PascalCase; ProfilerMarker replaces #if            |
// |         |            |        | DEVELOPMENT_BUILD Profiler.BeginSample/EndSample per FR-CS-070;    |
// |         |            |        | file header added per FR-CS-056/057.                               |
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | M-4 follow-on: doc updated to Rolling (PascalCase enum member).    |
// | 1.3     | 2026-06-09 | —      | AR-7 fixes. H-1: bounce normal Vector3.up (Unity +Y) → (0,0,1) —   |
// |         |            |        | project is Z-up; restitution/friction acted on the touchline axis  |
// |         |            |        | and a vertically falling ball never rebounded (ERR-001-001; spec    |
// |         |            |        | §3.1.8.1 pseudocode patched in the same commit). M-1: friction     |
// |         |            |        | stick impulse divided by StickImpulseCouplingDivisor (1 + m·r²/I)  |
// |         |            |        | (ERR-001-002). L-1: 0.01f slip threshold → Bounce.MinContactSpeed. |
// | 1.4     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->         |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is              |
// |         |            |        | Unity.Profiling; the old using was CS0246 under Unity and the      |
// |         |            |        | Linux compile gate alike, so this assembly could not have compiled |
// |         |            |        | in-engine. No functional change.                                   |
// | 1.3.1   | 2026-06-09 | —      | AR-8 L-1: ApplyBounce XML doc records its preconditions (entry     |
// |         |            |        | Velocity.z < 0 and ground proximity, both guaranteed by the state  |
// |         |            |        | machine; the unconditional Position.z = RADIUS snap makes direct   |
// |         |            |        | mid-air invocation a caller error). Doc-only.                      |
#endregion
