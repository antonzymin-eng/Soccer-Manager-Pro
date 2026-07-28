// File:     src/ball-physics/BallCollision.cs
// Created:  2026-05-24
// Modified: 2026-07-27 (shot-outcome pass)
// Modified: 2026-07-28 (shot-speed pass: swept goal-frame collision + crossing-point adjudication (ERR-001-005))
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Goal-post collision, boundary detection, possession evaluation, and kick
//           application. Possession tracking is external to BallState (Option B).

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Collision detection, boundary checks, possession evaluation, and kick application.
    /// Possession tracking is EXTERNAL to BallState (Option B design — see §3.1.11).
    /// </summary>
    public static class BallCollision
    {
        /// <summary>
        /// Handles ball collision with a goal post or crossbar.
        /// </summary>
        public static void ApplyGoalPostCollision(
            ref BallState ball,
            Vector3 contactPoint,
            Vector3 postCenter,
            BallEventLogger logger,
            float matchTime)
        {
            Vector3 normal = (contactPoint - postCenter).normalized;

            float   vn       = Vector3.Dot(ball.Velocity, normal);
            Vector3 vt       = ball.Velocity - vn * normal;
            float   vn_after = -BallPhysicsConstants.GoalPost.CoefficientOfRestitution * vn;

            ball.Velocity        = vt + vn_after * normal;
            ball.AngularVelocity *= BallPhysicsConstants.GoalPost.SpinRetention;

            logger?.LogGoalPostHit(ball, contactPoint, matchTime);
        }

        /// <summary>
        /// Deflects the ball off an agent's body — the §3.1.10.1 agent-contact response
        /// (ERR-003-007 / shot-outcome design KD-6), the entry point
        /// <c>BallCollisionHandler.OnAgentCollision</c>'s TODO deferred. The agent is modelled as a
        /// vertical cylinder, so the surface normal is planar: from the agent's centre (at ball
        /// height) to the ball. Only an APPROACHING ball deflects (normal velocity &lt; 0) — on the
        /// kick-release frame the ball moves away from the kicker, so a shooter can never block
        /// their own shot, statelessly. The normal velocity component is reflected, total speed is
        /// scaled by the body part's speed retention and spin by its spin retention
        /// (<see cref="BodyPartCoefficients"/> — the §3.1.10.1 model; no separate restitution
        /// constant is invented). Agent-velocity momentum transfer is a Stage-1 refinement
        /// (agent speeds are second-order against shot speeds). Pure — no RNG, no logging state.
        /// </summary>
        /// <param name="ball">Ball state, modified in place.</param>
        /// <param name="agentPosition">Agent world-space position (Z ignored — cylindrical body).</param>
        /// <param name="bodyPart">Contacting body part (Stage 0 callers pass Torso).</param>
        /// <returns>True when a deflection was applied; false for a separating or degenerate contact.</returns>
        public static bool ApplyAgentDeflection(
            ref BallState ball,
            Vector3 agentPosition,
            BodyPart bodyPart)
        {
            Vector3 normal = ball.Position
                             - new Vector3(agentPosition.x, agentPosition.y, ball.Position.z);
            float mag = normal.magnitude;
            if (mag < BallPhysicsConstants.AgentDeflection.MIN_NORMAL_EPSILON)
                return false;   // degenerate: ball at the agent's exact centre — no defined normal

            normal /= mag;

            float vn = Vector3.Dot(ball.Velocity, normal);
            if (vn >= 0f)
                return false;   // separating contact — the ball is already leaving the body

            (float speedRetention, float spinRetention) = BodyPartCoefficients.Get(bodyPart);

            // Reflect the normal component (planar normal ⇒ vertical velocity passes through the
            // reflection unchanged), then scale the whole vector by the body's speed retention.
            Vector3 reflected = ball.Velocity - 2f * vn * normal;
            ball.Velocity        = reflected * speedRetention;
            ball.AngularVelocity *= spinRetention;
            return true;
        }

        /// <summary>
        /// Swept physical goal-frame collision (ERR-001-005 / shot-speed design KD-4) — the
        /// entry that finally gives <see cref="ApplyGoalPostCollision"/> a production caller.
        /// Tests the ball-centre SEGMENT <paramref name="prevPosition"/> → <c>ball.Position</c>
        /// against the six frame cylinders (two posts + one crossbar per goal), each inflated by
        /// the ball radius (<see cref="BallPhysicsConstants.GoalFrame.SweptTestRadius"/>): at
        /// football shot speeds the ball moves ~0.42 m per 60 Hz tick, so a discrete per-tick
        /// position test TUNNELS straight through a 0.12 m post — the segment test cannot. On the
        /// EARLIEST parametric hit the ball centre is placed at the contact parameter and the
        /// existing restitution/spin-retention response runs against the normal from the closest
        /// point on the struck cylinder's axis. Gates: a <c>Controlled</c> ball never deflects (a
        /// possessed ball is driven at the holder's feet — the ApplyAgentDeflection precedent);
        /// a degenerate segment is a no-op; an X-band prefilter
        /// (<see cref="BallPhysicsConstants.GoalFrame.SweptPrefilterBandM"/>) skips the cylinder
        /// tests when the segment is nowhere near a goal line. Pure — no RNG, no logging state.
        /// </summary>
        /// <param name="ball">Ball state, modified in place on a hit.</param>
        /// <param name="prevPosition">Ball centre at the start of this tick's integration.</param>
        /// <returns>True when a frame strike was applied; false otherwise.</returns>
        public static bool ApplySweptGoalFrameCollision(ref BallState ball, Vector3 prevPosition)
        {
            if (ball.State == BallStateType.Controlled)
                return false;

            Vector3 d = ball.Position - prevPosition;
            if (d.sqrMagnitude < BallPhysicsConstants.AgentDeflection.MIN_NORMAL_EPSILON
                                 * BallPhysicsConstants.AgentDeflection.MIN_NORMAL_EPSILON)
                return false;   // degenerate: the ball did not move this tick

            float band = BallPhysicsConstants.GoalFrame.SweptPrefilterBandM;
            float xMin = Mathf.Min(prevPosition.x, ball.Position.x);
            float xMax = Mathf.Max(prevPosition.x, ball.Position.x);

            bool bestFound = false;
            float bestT = float.MaxValue;
            Vector3 bestAxisPoint = default;

            // Test the near goal(s) only — the prefilter makes the common mid-pitch tick free.
            if (xMin <= band)
                TestGoalFrame(prevPosition, d, goalLineX: 0f, ref bestFound, ref bestT, ref bestAxisPoint);
            if (xMax >= BallPhysicsConstants.Pitch.LENGTH - band)
                TestGoalFrame(prevPosition, d, goalLineX: BallPhysicsConstants.Pitch.LENGTH,
                              ref bestFound, ref bestT, ref bestAxisPoint);

            if (!bestFound)
                return false;

            Vector3 contactPoint = prevPosition + bestT * d;
            ball.Position = contactPoint;
            ApplyGoalPostCollision(ref ball, contactPoint, bestAxisPoint, logger: null, matchTime: 0f);
            return true;
        }

        /// <summary>Tests one goal's two posts + crossbar against the segment, keeping the
        /// earliest hit. Post axes are vertical at y = centreY ± PostAxisOffsetY, capped
        /// z ∈ [0, FrameTopZ]; the bar axis runs along y between the post axes at
        /// z = BarAxisZ.</summary>
        private static void TestGoalFrame(
            Vector3 p0, Vector3 d, float goalLineX,
            ref bool found, ref float bestT, ref Vector3 bestAxisPoint)
        {
            float centreY = BallPhysicsConstants.Pitch.WIDTH * 0.5f;
            float offY    = BallPhysicsConstants.GoalFrame.PostAxisOffsetY;
            float radius  = BallPhysicsConstants.GoalFrame.SweptTestRadius;

            // Two posts: infinite cylinder in the XY plane, capped in z.
            for (int s = -1; s <= 1; s += 2)
            {
                float axisY = centreY + s * offY;
                if (TrySegmentCircle2D(
                        p0.x, p0.y, d.x, d.y, goalLineX, axisY, radius, out float t))
                {
                    float z = p0.z + t * d.z;
                    if (z >= 0f && z <= BallPhysicsConstants.GoalFrame.FrameTopZ && t < bestT)
                    {
                        found = true;
                        bestT = t;
                        bestAxisPoint = new Vector3(goalLineX, axisY, p0.z + t * d.z);
                    }
                }
            }

            // Crossbar: infinite cylinder in the XZ plane, capped in y between the post axes.
            float barZ = BallPhysicsConstants.GoalFrame.BarAxisZ;
            if (TrySegmentCircle2D(
                    p0.x, p0.z, d.x, d.z, goalLineX, barZ, radius, out float tBar))
            {
                float y = p0.y + tBar * d.y;
                if (y >= centreY - offY && y <= centreY + offY && tBar < bestT)
                {
                    found = true;
                    bestT = tBar;
                    bestAxisPoint = new Vector3(goalLineX, p0.y + tBar * d.y, barZ);
                }
            }
        }

        /// <summary>
        /// Earliest parametric intersection t ∈ [0, 1] of the 2-D segment (a0,b0) + t·(da,db)
        /// with the circle centred (ca,cb) of radius <paramref name="r"/> — the cylinder test
        /// projected onto the plane perpendicular to its axis. Returns false when the segment
        /// starts INSIDE the circle (the ball is already overlapping the frame — no crossing to
        /// resolve; the previous tick's test handled the approach) or never reaches it.
        /// </summary>
        private static bool TrySegmentCircle2D(
            float a0, float b0, float da, float db, float ca, float cb, float r, out float t)
        {
            t = 0f;
            float fa = a0 - ca;
            float fb = b0 - cb;
            float aa = da * da + db * db;
            if (aa <= 0f)
                return false;   // no planar motion — cannot cross the circle

            float bb = 2f * (fa * da + fb * db);
            float cc = fa * fa + fb * fb - r * r;
            if (cc < 0f)
                return false;   // starts inside — see summary

            float disc = bb * bb - 4f * aa * cc;
            if (disc < 0f)
                return false;

            float sqrtDisc = Mathf.Sqrt(disc);
            float t0 = (-bb - sqrtDisc) / (2f * aa);
            if (t0 < 0f || t0 > 1f)
                return false;

            t = t0;
            return true;
        }

        /// <summary>
        /// Checks if the ball has left the field of play, adjudicating goal-line crossings at the
        /// DETECTED position. Prefer the <paramref name="prevPosition"/> overload where a movement
        /// segment exists (ERR-001-005): at 25 m/s the detected position is up to ~0.42 m past the
        /// plane, which misclassifies a rising ball around the crossbar. This overload remains for
        /// per-position callers (<see cref="BallStateMachine.IsOutOfBounds"/> keeps the same
        /// out-NESS contract — the two predicates still agree on what is out; only the
        /// goal-vs-over/wide classification refines under the segment overload).
        /// </summary>
        public static (bool isOut, RestartType restart) CheckBoundaries(
            BallState ball,
            int lastTouchTeamID)
        {
            return CheckBoundaries(ball, ball.Position, lastTouchTeamID);
        }

        /// <summary>
        /// Checks if the ball has left the field of play.
        /// Ball must entirely cross the line — on the ground or IN THE AIR (Law 9; the former
        /// z &lt; Ball.Diameter gate made an airborne crossing neither a goal nor out of play —
        /// ERR-001-004 / shot-outcome design KD-5). A goal-line crossing between the posts and
        /// under the crossbar is a goal (Law 10, <see cref="IsBetweenPostsUnderCrossbar"/>),
        /// evaluated at the segment's interpolated CROSSING of the out-plane rather than at the
        /// detected (overshot) position (ERR-001-005 / shot-speed design KD-5 — at 25 m/s the
        /// difference is up to ~0.2 m of height, exactly the band around the crossbar); over the
        /// bar or wide of the posts is a corner / goal kick.
        /// Corner-region precedence (Stage 0 simplification): when both the goal line and
        /// a touchline are crossed in the same frame, the touchline check runs first and
        /// classifies the exit as ThrowIn even though geometric reasoning would prefer
        /// the goal-line classification. Trajectory-based corner resolution is a Stage
        /// 1+ deliverable.
        /// </summary>
        public static (bool isOut, RestartType restart) CheckBoundaries(
            BallState ball,
            Vector3 prevPosition,
            int lastTouchTeamID)
        {
            float x = ball.Position.x;
            float y = ball.Position.y;
            float r = BallPhysicsConstants.Ball.RADIUS;

            if (y < -r || y > BallPhysicsConstants.Pitch.WIDTH + r)
                return (true, RestartType.ThrowIn);

            // Home goal (x < −r): the Y/Z gates are identical to the away goal because
            // both goal mouths are centred at WIDTH/2 and capped at GOAL_HEIGHT; the X
            // half-space is what distinguishes them and the caller already verified it.
            if (x < -r)
            {
                if (IsBetweenPostsUnderCrossbar(InterpolateToPlane(prevPosition, ball.Position, -r)))
                    return (true, RestartType.KickOff);
                return (true, lastTouchTeamID == 0 ? RestartType.Corner : RestartType.GoalKick);
            }

            // Away goal (x > LENGTH + r): see comment on home-goal branch above.
            if (x > BallPhysicsConstants.Pitch.LENGTH + r)
            {
                if (IsBetweenPostsUnderCrossbar(InterpolateToPlane(
                        prevPosition, ball.Position, BallPhysicsConstants.Pitch.LENGTH + r)))
                    return (true, RestartType.KickOff);
                return (true, lastTouchTeamID == 1 ? RestartType.Corner : RestartType.GoalKick);
            }

            return (false, RestartType.None);
        }

        /// <summary>
        /// The segment's crossing point of the vertical plane x = <paramref name="planeX"/>,
        /// with t clamped to [0, 1] — a prev already beyond the plane (a rebound landing outside
        /// and re-crossing, or a position-only caller passing prev == pos) degenerates to the
        /// detected position, the pre-ERR-001-005 behaviour.
        /// </summary>
        private static Vector3 InterpolateToPlane(Vector3 prev, Vector3 pos, float planeX)
        {
            float dx = pos.x - prev.x;
            if (Mathf.Abs(dx) < BallPhysicsConstants.AgentDeflection.MIN_NORMAL_EPSILON)
                return pos;
            float t = Mathf.Clamp01((planeX - prev.x) / dx);
            return prev + t * (pos - prev);
        }

        private static bool IsBetweenPostsUnderCrossbar(Vector3 position)
        {
            float halfGoalWidth = BallPhysicsConstants.Pitch.GOAL_WIDTH / 2f;
            float centerY       = BallPhysicsConstants.Pitch.WIDTH / 2f;

            bool withinPosts   = position.y > centerY - halfGoalWidth
                              && position.y < centerY + halfGoalWidth;
            bool underCrossbar = position.z < BallPhysicsConstants.Pitch.GOAL_HEIGHT;

            return withinPosts && underCrossbar;
        }

        /// <summary>
        /// Returns true if an agent physically can take possession this frame.
        /// Does NOT modify BallState — pure predicate.
        /// Caller must: record possession in agent system, call SetBallControlled(), drive position.
        /// </summary>
        public static bool CheckPossession(
            BallState ball,
            Vector3 agentPosition,
            Vector3 agentVelocity)
        {
            // XY-plane distance only; height handled by ControlHeight check.
            float distance = Vector3.Distance(
                new Vector3(ball.Position.x, ball.Position.y, 0f),
                new Vector3(agentPosition.x, agentPosition.y, 0f));

            if (distance > BallPhysicsConstants.Possession.ControlRadius)
                return false;

            if ((ball.Velocity - agentVelocity).magnitude > BallPhysicsConstants.Possession.ControlVelocity)
                return false;

            if (ball.Position.z > BallPhysicsConstants.Possession.ControlHeight)
                return false;

            return true;
        }

        /// <summary>
        /// Transitions ball to Controlled state. Called by agent system after CheckPossession.
        /// Does NOT record which agent has possession (Option B — agent system owns that).
        /// </summary>
        public static void SetBallControlled(ref BallState ball)
        {
            ball.State           = BallStateType.Controlled;
            ball.Velocity        = Vector3.zero;
            ball.AngularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Applies a kick impulse to the ball, releasing it from Controlled.
        /// POSSESSION MODEL (Option B): transitions BallState out of Controlled as the signal
        /// the agent system polls to clear its possession record.
        /// Returns <see cref="KickResult.RejectedNonFiniteVelocity"/> without mutating ball
        /// state when the caller supplies NaN/Infinity velocity — caller MUST inspect the
        /// return value and either retry with a sanitized vector or abort the kick.
        /// </summary>
        public static KickResult ApplyKick(
            ref BallState ball,
            Vector3 velocity,
            Vector3 spin,
            int agentId,
            float matchTime,
            BallEventLogger logger = null)
        {
            if (!IsFiniteVector(velocity))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Diagnostic — string interpolation is permitted under FR-CS-031's
                // editor/development-build carve-out (same pattern as event-system
                // EventBus debug assertions).
                Debug.LogError(
                    $"[BallPhysics] ApplyKick: Invalid velocity {velocity} from agent {agentId}. Kick rejected.");
#endif
                return KickResult.RejectedNonFiniteVelocity;
            }

            if (!IsFiniteVector(spin))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Invalid spin {spin} from agent {agentId}. Spin zeroed.");
#endif
                spin = Vector3.zero;
            }

            if (velocity.magnitude > BallPhysicsConstants.Limits.MaxVelocity)
            {
                velocity = velocity.normalized * BallPhysicsConstants.Limits.MaxVelocity;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Velocity clamped to {BallPhysicsConstants.Limits.MaxVelocity} m/s.");
#endif
            }

            if (spin.magnitude > BallPhysicsConstants.Limits.MaxSpin)
            {
                spin = spin.normalized * BallPhysicsConstants.Limits.MaxSpin;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Spin clamped to {BallPhysicsConstants.Limits.MaxSpin} rad/s.");
#endif
            }

            ball.Velocity        = velocity;
            ball.AngularVelocity = spin;

            float horizontalSpeed = new Vector2(velocity.x, velocity.y).magnitude;

            if (velocity.z > 0f)
                ball.State = BallStateType.Airborne;
            else if (horizontalSpeed > BallPhysicsConstants.State.MinVelocity)
                ball.State = BallStateType.Rolling;
            else
                ball.State = BallStateType.Stationary;

            ball.LastValidPosition = ball.Position;
            ball.LastValidVelocity = ball.Velocity;

            logger?.LogKick(ball, agentId, ball.State, matchTime);

            return KickResult.Applied;
        }

        private static bool IsFiniteVector(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; ALL_CAPS       |
// |         |            |        | constant refs → PascalCase; Stage 0 lowEnough limitation          |
// |         |            |        | documented in CheckBoundaries XML doc; file header per FR-CS-056.  |
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected. M-1: using order      |
// |         |            |        | System → UnityEngine. M-2: _coefficients → s_coefficients. M-3:    |
// |         |            |        | dead isHomeGoal parameter removed. M-4: enum members PascalCase.   |
// |         |            |        | M-5: ApplyKick → KickResult. L-2: corner-precedence doc. L-4:      |
// |         |            |        | BodyPartCoefficients.Get throws on unknown enum values.            |
// | 1.3     | 2026-06-03 | —      | AR-2 fixes. L-1: IsInHomeGoal / IsInAwayGoal wrappers folded —    |
// |         |            |        | CheckBoundaries now calls IsBetweenPostsUnderCrossbar directly     |
// |         |            |        | with inline home-goal / away-goal comments; the two zero-info      |
// |         |            |        | wrappers are gone. L-2: BodyPart, RestartType, KickResult, and     |
// |         |            |        | BodyPartCoefficients extracted to their own files per src/CLAUDE.md|
// |         |            |        | FILE NAMING. Unused System.Collections.Generic using removed and   |
// |         |            |        | the UnityEngine. prefix dropped from Debug / Vector2 (already in   |
// |         |            |        | scope via the single UnityEngine using).                           |
// | 1.4     | 2026-06-03 | —      | AR-3 fixes. L-1: &lt; / &gt; XML entity escapes inside the two      |
// |         |            |        | // line comments at the home-goal / away-goal branches replaced   |
// |         |            |        | with raw < / > (the comments are plain source, not XML doc).      |
// |         |            |        | L-3: the four Debug.LogError / Debug.LogWarning emit blocks in    |
// |         |            |        | ApplyKick gated behind #if UNITY_EDITOR || DEVELOPMENT_BUILD —    |
// |         |            |        | mirrors the event-system EventBus debug-assertion pattern. Both   |
// |         |            |        | the return path (KickResult.RejectedNonFiniteVelocity) and the    |
// |         |            |        | functional clamping stay outside the gates. L-4: parallel         |
// |         |            |        | Debug.LogWarning added for spin clamping so the two symmetric     |
// |         |            |        | clamp operations now have symmetric diagnostics.                  |
// | 1.7     | 2026-07-27 | —      | Shot-outcome pass (ERR-001-004 / ERR-003-007, design KD-5/KD-6).  |
// |         |            |        | CheckBoundaries drops the z < Diameter gate — an airborne line    |
// |         |            |        | crossing is out per Law 9, and the goal-line branches adjudicate  |
// |         |            |        | goal vs over/wide via IsBetweenPostsUnderCrossbar (Law 10). New   |
// |         |            |        | ApplyAgentDeflection — the §3.1.10.1 agent-contact response       |
// |         |            |        | (reflect approaching normal component; BodyPartCoefficients      |
// |         |            |        | retention; separating/degenerate contact is a no-op).             |
// | 1.8     | 2026-07-28 | —      | Shot-speed pass (ERR-001-005, design KD-4/KD-5): ApplySweptGoalFrame-       |
// |         |            |        | Collision — segment-vs-six-cylinder test (posts + crossbars, inflated by    |
// |         |            |        | ball radius; a discrete test tunnels a 0.12 m post at shot speeds), first   |
// |         |            |        | production caller of ApplyGoalPostCollision. CheckBoundaries gains a        |
// |         |            |        | prevPosition overload adjudicating goal-line crossings at the interpolated  |
// |         |            |        | crossing point (detected position is up to ~0.42 m past the plane); the     |
// |         |            |        | position-only overload delegates with prev == pos (IsOutOfBounds contract   |
// |         |            |        | unchanged — out-ness identical, only goal-vs-over/wide refines).            |
#endregion
