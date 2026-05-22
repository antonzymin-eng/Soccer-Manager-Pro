using UnityEngine;

namespace TacticalDirector.Core.Physics.Ball
{
    /// <summary>
    /// Ground contact physics: impulse-based bounce and rolling friction.
    /// </summary>
    public static class BallGroundInteraction
    {
        /// <summary>
        /// Handles ball bounce on ground contact using impulse-based contact mechanics.
        /// Ground normal is always UP for regulation football pitches
        /// (FIFA allows max 1% slope for drainage — negligible).
        /// </summary>
        public static void ApplyBounce(
            ref BallState ball,
            SurfaceType surface,
            BallEventLogger logger,
            float matchTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Bounce");
#endif
            Vector3 normal = Vector3.up;

            float e            = SurfaceProperties.GetCoefficientOfRestitution(surface);
            float mu           = SurfaceProperties.GetFrictionCoefficient(surface);
            float spinRetention = SurfaceProperties.GetSpinRetention(surface);

            Vector3 v     = ball.Velocity;
            Vector3 omega = ball.AngularVelocity;
            float   r     = BallPhysicsConstants.Ball.RADIUS;
            float   m     = BallPhysicsConstants.Ball.MASS;

            // Decompose velocity into normal and tangent components
            float   vn = Vector3.Dot(v, normal);   // negative = into ground
            Vector3 vt = v - vn * normal;

            // Contact point velocity: v_contact = vt + ω × r_contact
            Vector3 rContact        = -r * normal;
            Vector3 spinContribution = Vector3.Cross(omega, rContact);
            Vector3 contactVelocity  = vt + spinContribution;

            // Normal impulse (restitution)
            float vn_after = -e * vn;

            // Tangential friction impulse
            float   J_n     = (1 + e) * m * Mathf.Abs(vn);
            float   J_t_max = mu * J_n;
            Vector3 vt_after = vt;

            if (contactVelocity.magnitude > 0.01f)
            {
                float   J_t_required  = m * contactVelocity.magnitude;
                float   J_t           = Mathf.Min(J_t_max, J_t_required);
                Vector3 frictionDir   = -contactVelocity.normalized;
                Vector3 frictionImpulse = frictionDir * J_t;

                vt_after = vt + frictionImpulse / m;

                Vector3 angularImpulse = Vector3.Cross(rContact, frictionImpulse);
                omega += angularImpulse / BallPhysicsConstants.Ball.MOMENT_OF_INERTIA;
            }

            ball.Velocity        = vt_after + vn_after * normal;
            ball.AngularVelocity = omega * spinRetention;
            ball.Position        = new Vector3(ball.Position.x, ball.Position.y, r);

            logger?.LogBounce(ball, surface, e, vn, vn_after, matchTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        /// <summary>
        /// Calculates rolling friction deceleration force.
        /// Only applied when ball state is ROLLING.
        /// </summary>
        public static Vector3 CalculateRollingFriction(Vector3 velocity, SurfaceType surface)
        {
            float speed = velocity.magnitude;
            if (speed < BallPhysicsConstants.State.MIN_VELOCITY)
                return Vector3.zero;

            float mu_r         = SurfaceProperties.GetRollingResistance(surface);
            float forceMagnitude = mu_r
                                 * BallPhysicsConstants.Ball.MASS
                                 * BallPhysicsConstants.Environment.GRAVITY;

            return -velocity.normalized * forceMagnitude;
        }
    }
}
