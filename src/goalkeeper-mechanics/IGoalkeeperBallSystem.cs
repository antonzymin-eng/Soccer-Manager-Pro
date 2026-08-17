// File:     src/goalkeeper-mechanics/IGoalkeeperBallSystem.cs
// Created:  2026-05-28
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.2 / §4.3, Ball Physics #1 §3.1.11.2, Code Standards #20
// Purpose:  Goalkeeper Mechanics' read/write boundary to Ball Physics #1.
//           Written because both sides (GoalkeeperMechanics producer, BallPhysics consumer) are specified.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Ball physics interface boundary for Goalkeeper Mechanics.
    /// Both sides are specified (GoalkeeperMechanics #11, Ball Physics #1), so the interface is valid
    /// per CLAUDE.md interface-design principle (FR-CS-048/049).
    /// Goalkeeper Mechanics #11 §4.2 / §4.3.
    /// </summary>
    public interface IGoalkeeperBallSystem
    {
        /// <summary>
        /// Applies a kick velocity and spin to the ball.
        /// Ball Physics #1 §3.1.11.2.
        /// </summary>
        void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs);

        /// <summary>
        /// Sets the ball possessor agent ID (catch path).
        /// Ball Physics #1 §3.1 possession surface — OI-006 verification posture per §4.3.
        /// </summary>
        void SetPossessor(int agentId);

        /// <summary>
        /// Arrests the ball in the keeper's hands on a successful claim — the SECOND half of
        /// §3.5.2's catch branch (<c>ball.velocity = gkHandVelocity</c>, "parked at hand position"),
        /// alongside <see cref="SetPossessor"/>. Zeroes linear and angular velocity.
        /// <para>ERR-011-008: without this the claim sets a possession FLAG only. Possession is not a
        /// kinematic constraint in this engine — the ball integrates unconditionally and the goal
        /// check adjudicates on ball POSITION — so a "caught" shot flew on into the net (measured:
        /// ball speed 11.1 m/s in, 10.8 m/s out, and 7 of 10 catches followed by a goal within 5 s).</para>
        /// <para>Stage 0 reads <c>gkHandVelocity</c> as zero: the ball is at rest in the keeper's
        /// frame. Carrying the hand's world velocity is a Stage-1 refinement (§7), as is holding the
        /// ball at hand height rather than letting it settle. Deliberately does NOT enter
        /// <c>BallStateType.Controlled</c> — no production path leaves that state except a kick, and
        /// this engine has never produced a Controlled ball.</para>
        /// Goalkeeper Mechanics #11 §3.5.2. Ball Physics #1 §3.1.
        /// </summary>
        void ParkBall();

        /// <summary>
        /// Returns the current ball possessor agent ID, or -1 if the ball is loose.
        /// Ball Physics #1 §3.1.
        /// </summary>
        int GetBallPossessorId();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1     | 2026-08-03 | —      | ERR-011-008: + ParkBall() — the ball-side half of §3.5.2's claim, |
// |         |            |        | which had no seam (ApplyKick is a kick, and the possession flag  |
// |         |            |        | alone leaves the ball travelling). See the conversion-at-contact |
// |         |            |        | pass, docs/tracking/gk-conversion-at-contact-design.md.           |
#endregion
