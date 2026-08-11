// File:     src/heading-mechanics/Tests/HeadingAimTests.cs
// Created:  2026-08-09
// Modified: 2026-08-09
// Author:   —
// Spec:     Heading Mechanics #10 §3.5.1 (ERR-010-002), §5, Code Standards #20
// Purpose:  Locks for the header aim — that TargetIntent reaches the outgoing direction at all, that
//           the aim is attribute-graded and physically bounded, and that a header can lift the ball.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.HeadingMechanics.Tests
{
    /// <summary>
    /// §3.5.1 (ERR-010-002) locks.
    ///
    /// <para>Every test here is written to FAIL against the pre-fix model, in which the outgoing
    /// direction was pure specular reflection about <c>normalize(ballPosition − headCentre)</c> and
    /// <c>TargetIntent</c> reached no formula. The load-bearing one is
    /// <see cref="TwoDifferentTargets_ProduceMateriallyDifferentDirections"/>: a mirror-only
    /// implementation returns the IDENTICAL vector for both targets, so that test cannot pass with the
    /// mechanism disabled. It is the anti-tautology guard this project's review loop keeps asking for —
    /// a lock that passes when the fix is reverted has been filed here at least four times.</para>
    /// </summary>
    [TestFixture]
    public sealed class HeadingAimTests
    {
        private const float Tolerance = 1e-3f;

        /// <summary>Integer form of the attribute bounds; the catalogue declares them as float.</summary>
        private const int AttrMinRaw = 1;

        /// <summary>Integer form of the attribute bounds; the catalogue declares them as float.</summary>
        private const int AttrMaxRaw = 20;

        /// <summary>An elite header of the ball — steer authority 1.0, so the aim is fully expressed.</summary>
        private static HeadingAgentAttributes Elite()
        {
            return new HeadingAgentAttributes
            {
                Heading = AttrMaxRaw,
                Strength = AttrMaxRaw,
                Balance = AttrMaxRaw,
                Fatigue = 0.0f,
                TeamId = 0,
            };
        }

        // ── §3.5.1 step 1: the ballistic launch direction ────────────────────────────────

        /// <summary>
        /// A reachable target is reached. Hand-derived, NOT computed through the production solve:
        /// at v = 12 m/s over a level range of R = 10 m, the low launch angle satisfies
        /// tanθ = (v² − sqrt(v⁴ − g²R²)) / (gR). With g = 9.81: v² = 144, v⁴ = 20736,
        /// g²R² = 9.81²·100 = 9623.61, sqrt(20736 − 9623.61) = sqrt(11112.39) = 105.4153,
        /// gR = 98.1 ⇒ tanθ = (144 − 105.4153)/98.1 = 38.5847/98.1 = 0.393320.
        /// Direction = normalize(1, 0, 0.393320) ⇒ z-component = 0.393320/sqrt(1 + 0.154701) = 0.365906.
        /// </summary>
        [Test]
        public void AimDirection_ReachableLevelTarget_MatchesHandDerivedLowAngle()
        {
            Vector3 contact = new Vector3(50.0f, 34.0f, 2.0f);
            Vector3 target = new Vector3(60.0f, 34.0f, 2.0f);

            Vector3 dir = HeadingAim.ComputeAimDirection(contact, target, 12.0f);

            Assert.That(dir.magnitude, Is.EqualTo(1.0f).Within(Tolerance), "direction must be a unit vector");
            Assert.That(dir.z, Is.EqualTo(0.365906f).Within(2e-3f),
                "low-angle ballistic launch: hand-derived tan(theta) = 0.393320");
            Assert.That(dir.y, Is.EqualTo(0.0f).Within(Tolerance));
            Assert.That(dir.x, Is.GreaterThan(0.0f), "must launch toward the target");
        }

        /// <summary>
        /// The whole reason the aim is a ballistic solve and not a straight line to the target: a
        /// distant ground-level target lies BELOW the contact point, so the straight line to it points
        /// downward and heading along it drives the ball into the turf. The launch must rise.
        /// </summary>
        [Test]
        public void AimDirection_DistantGroundTarget_LaunchesUpwardNotAlongTheStraightLine()
        {
            Vector3 contact = new Vector3(10.0f, 34.0f, 2.3f);
            Vector3 target = new Vector3(90.0f, 5.0f, 0.0f);

            Vector3 straightLine = (target - contact).normalized;
            Assert.That(straightLine.z, Is.LessThan(0.0f), "precondition: the straight line points down");

            Vector3 dir = HeadingAim.ComputeAimDirection(contact, target, 14.0f);

            Assert.That(dir.z, Is.GreaterThan(0.0f),
                "a clearance must be launched upward even though its destination is below the head");
        }

        /// <summary>
        /// Out of ballistic range degrades to the maximum-range launch. Hand-derived, NOT computed
        /// through production: contact at 2.3 m, target on the ground, so dz = −2.3 and the max-range
        /// angle is tanθ = v / sqrt(v² − 2·g·dz) = 6 / sqrt(36 + 2·9.81·2.3) = 6 / 9.00700 = 0.666152,
        /// θ = 33.66°. Direction = normalize(1, 0, 0.666152) ⇒ x = 0.832246, z = 0.554416.
        ///
        /// <para>The pre-AR code returned a flat 45° (0.70710678 in both), which is the max-range angle
        /// only for dz = 0 — never true of a header. This test fails against it.</para>
        /// </summary>
        [Test]
        public void AimDirection_UnreachableTarget_FallsBackToTheTrueMaxRangeLaunch()
        {
            Vector3 contact = new Vector3(5.0f, 34.0f, 2.3f);
            Vector3 target = new Vector3(100.0f, 34.0f, 0.0f);

            Vector3 dir = HeadingAim.ComputeAimDirection(contact, target, 6.0f);

            Assert.That(dir.z, Is.EqualTo(0.554416f).Within(2e-3f), "max-range angle for dz = -2.3 m");
            Assert.That(dir.x, Is.EqualTo(0.832246f).Within(2e-3f));
            Assert.That(dir.magnitude, Is.EqualTo(1.0f).Within(Tolerance));
            Assert.That(dir.z, Is.LessThan(0.70710678f - 2e-3f),
                "a flat 45 degrees is the dz = 0 answer and must not be returned for a target below the head");
        }

        /// <summary>
        /// P1, asserted rather than claimed: the direction must not STEP as the target recedes past the
        /// reachable boundary. §3.5.1 calls this branch "the ordinary case for a defensive clearance",
        /// so a discontinuity here sits on the production path, not an edge case.
        ///
        /// <para>Hand-derived boundary: at v = 8 m/s with dz = −2.3 m, the two launch angles coincide at
        /// R_max = (v/g)·sqrt(v² − 2·g·dz) = (8/9.81)·sqrt(64 + 45.126) = 0.81549 × 10.44634 = 8.5189 m.
        /// Straddling it with a 4 cm step gives 2.42° here and 9.98° against the pre-AR flat 45°, so the
        /// 4° bound separates them by more than 2× on either side.</para>
        ///
        /// <para>The bound is 4° rather than a fraction of a degree because the low root carries a
        /// square-root singularity at the boundary — <c>tanθ = (v² − sqrt(disc))/(g·R)</c> with
        /// <c>disc → 0</c> — so the direction is continuous there but not Lipschitz, and the last
        /// centimetres before the boundary genuinely do turn fast. That is a property of the physics,
        /// not of the fix; what the fix removed is the step ON TOP of it.</para>
        /// </summary>
        [Test]
        public void AimDirection_IsContinuousAcrossTheReachabilityBoundary()
        {
            Vector3 contact = new Vector3(50.0f, 34.0f, 2.3f);

            Vector3 justInside = HeadingAim.ComputeAimDirection(
                contact, new Vector3(50.0f + 8.50f, 34.0f, 0.0f), 8.0f);
            Vector3 justOutside = HeadingAim.ComputeAimDirection(
                contact, new Vector3(50.0f + 8.54f, 34.0f, 0.0f), 8.0f);

            Assert.That(justInside, Is.Not.EqualTo(Vector3.zero));
            Assert.That(justOutside, Is.Not.EqualTo(Vector3.zero));
            Assert.That(Vector3.Angle(justInside, justOutside), Is.LessThan(4.0f),
                "4 cm of target distance either side of the reachability boundary must not step the launch");
        }

        /// <summary>
        /// A target higher than this speed can reach at any launch angle has no horizontal solution that
        /// brings the ball nearer it, so the closest approach is straight up. Guards the radicand of the
        /// max-range formula, which goes non-positive exactly at dz ≥ v²/2g — 1.835 m at v = 6 m/s, and
        /// the target here sits 28 m above the contact.
        /// </summary>
        [Test]
        public void AimDirection_TargetAboveReach_LaunchesVertically()
        {
            Vector3 contact = new Vector3(50.0f, 34.0f, 2.0f);
            Vector3 target = new Vector3(60.0f, 34.0f, 30.0f);

            Vector3 dir = HeadingAim.ComputeAimDirection(contact, target, 6.0f);

            Assert.That(dir.z, Is.EqualTo(1.0f).Within(Tolerance));
            Assert.That(dir.x, Is.EqualTo(0.0f).Within(Tolerance));
        }

        // ── §3.5.1 step 2: the half-vector normal, and its physical bound ────────────────

        /// <summary>
        /// The half-vector inverts §3.5's reflection exactly: reflecting the incident about the returned
        /// normal must reproduce the requested direction. This is the property the whole aim rests on.
        /// </summary>
        [Test]
        public void AimNormal_ReflectsIncidentIntoTheDesiredDirection()
        {
            Vector3 incomingVelocity = new Vector3(8.0f, 2.0f, -6.0f);
            Vector3 incident = -incomingVelocity.normalized;
            Vector3 desired = new Vector3(0.6f, -0.3f, 0.74f).normalized;

            Vector3 normal = HeadingAim.ComputeAimNormal(incident, desired);
            Assert.That(normal, Is.Not.EqualTo(Vector3.zero));

            // The §3.5 reflection formula, restated here rather than called, so this lock does not
            // verify the production code through the production code.
            float dot = Vector3.Dot(incident, normal);
            Vector3 reflected = (2.0f * dot * normal - incident).normalized;

            Assert.That(reflected.x, Is.EqualTo(desired.x).Within(Tolerance));
            Assert.That(reflected.y, Is.EqualTo(desired.y).Within(Tolerance));
            Assert.That(reflected.z, Is.EqualTo(desired.z).Within(Tolerance));
        }

        /// <summary>
        /// The property that makes a hemisphere bound unnecessary, asserted rather than assumed —
        /// because the first cut of <see cref="HeadingAim.ComputeAimNormal"/> carried such a bound and
        /// it could never fire. Swept over the full range of desired directions: the returned normal is
        /// ALWAYS in the forward hemisphere, so every outgoing direction is producible off the front of
        /// the head and the aim's only real bound is the player's attribute.
        /// </summary>
        [Test]
        public void AimNormal_IsAlwaysInTheForwardHemisphere_SoNoGeometricBoundIsNeeded()
        {
            Vector3 incident = new Vector3(-1.0f, 0.0f, 0.0f);

            for (int degrees = 0; degrees < 360; degrees += 5)
            {
                float radians = degrees * Mathf.Deg2Rad;
                Vector3 desired = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0.0f);

                Vector3 normal = HeadingAim.ComputeAimNormal(incident, desired);
                if (normal == Vector3.zero)
                {
                    // The single degenerate direction: exactly opposite the incident.
                    Assert.That(Vector3.Dot(desired, incident), Is.EqualTo(-1.0f).Within(1e-2f));
                    continue;
                }

                Assert.That(Vector3.Dot(normal, incident), Is.GreaterThanOrEqualTo(-Tolerance),
                    $"desired at {degrees} degrees produced a rearward normal");
            }
        }

        /// <summary>Asking the ball to carry straight on through the head has no reflection solution.</summary>
        [Test]
        public void AimNormal_DirectionOppositeTheIncident_IsDegenerate()
        {
            Vector3 incident = new Vector3(-1.0f, 0.0f, 0.0f);

            Vector3 normal = HeadingAim.ComputeAimNormal(incident, -incident);

            Assert.That(normal, Is.EqualTo(Vector3.zero));
        }

        // ── §3.5.1 step 3: attribute-graded steering ─────────────────────────────────────

        /// <summary>
        /// Zero authority is exactly the pre-fix model — the geometric normal, untouched. This pins the
        /// bottom of the ramp, so a future change cannot quietly make a raw-1 header steer like a raw-20 one.
        /// </summary>
        [Test]
        public void AchievedNormal_ZeroAuthority_IsTheGeometricNormal()
        {
            Vector3 geometric = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 aim = new Vector3(1.0f, 0.0f, 0.0f);

            Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometric, aim, 0.0f);

            Assert.That(achieved.x, Is.EqualTo(geometric.x).Within(Tolerance));
            Assert.That(achieved.z, Is.EqualTo(geometric.z).Within(Tolerance));
        }

        /// <summary>Full authority reaches the aim normal exactly.</summary>
        [Test]
        public void AchievedNormal_FullAuthority_IsTheAimNormal()
        {
            Vector3 geometric = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 aim = new Vector3(1.0f, 0.0f, 0.0f);

            Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometric, aim, 1.0f);

            Assert.That(achieved.x, Is.EqualTo(aim.x).Within(Tolerance));
            Assert.That(achieved.z, Is.EqualTo(aim.z).Within(Tolerance));
        }

        /// <summary>
        /// P2 — skill discriminates, monotonically and across the whole attribute range. The better
        /// header of the ball gets nearer the normal he wanted, at every step. A plateau at either end
        /// would fail this (ERR-008-019's FULL-RANGE ramp shape).
        /// </summary>
        [Test]
        public void AchievedNormal_IsMonotoneInAuthority_AcrossTheWholeRange()
        {
            Vector3 geometric = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 aim = new Vector3(1.0f, 0.0f, 0.0f);

            float previousAlignment = -2.0f;
            for (int raw = AttrMinRaw; raw <= AttrMaxRaw; raw++)
            {
                float authority = raw / HeadingMechanicsConstants.ATTR_MAX;
                Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometric, aim, authority);
                float alignment = Vector3.Dot(achieved, aim);

                Assert.That(alignment, Is.GreaterThan(previousAlignment),
                    $"attribute {raw} must steer strictly nearer the aim than {raw - 1}");
                previousAlignment = alignment;
            }
        }

        /// <summary>A degenerate aim leaves the natural rebound intact, so an unaimable header still plays.</summary>
        [Test]
        public void AchievedNormal_DegenerateAim_FallsBackToTheGeometricNormal()
        {
            Vector3 geometric = new Vector3(0.0f, 0.0f, 1.0f);

            Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometric, Vector3.zero, 1.0f);

            Assert.That(achieved.z, Is.EqualTo(1.0f).Within(Tolerance));
        }

        /// <summary>
        /// The lock the one above could not be: a degenerate aim must reach
        /// <see cref="HeadingAim.ComputeAchievedNormal"/> AS a degenerate aim, through the composition
        /// the production code actually runs.
        ///
        /// <para>Pre-AR, <see cref="HeadingAim.ComputeAimNormal"/> handed a zero desired direction
        /// through as <c>normalize(incident + 0) == incident</c> — magnitude 1, so its own guard missed.
        /// The blend then steered toward the incident and at authority 1 reflected the ball straight
        /// back the way it came at full power: the MAXIMUM deflection, out of the branch documented as
        /// producing the natural rebound. The direct-call test above passed throughout, because it
        /// supplies the zero the composition never could.</para>
        /// </summary>
        [Test]
        public void DegenerateAimDirection_PropagatesAsDegenerate_AndPlaysTheNaturalRebound()
        {
            Vector3 incomingVelocity = new Vector3(7.0f, 0.0f, -5.0f);
            Vector3 incident = -incomingVelocity.normalized;
            Vector3 contact = new Vector3(50.0f, 34.0f, 2.3f);

            // A target horizontally coincident with the contact point: there is no bearing to launch
            // along, so step 1 has no answer.
            Vector3 degenerateAim = HeadingAim.ComputeAimDirection(contact, contact, 12.0f);
            Assert.That(degenerateAim, Is.EqualTo(Vector3.zero), "precondition: step 1 is degenerate");

            Vector3 aimNormal = HeadingAim.ComputeAimNormal(incident, degenerateAim);
            Assert.That(aimNormal, Is.EqualTo(Vector3.zero),
                "a degenerate aim must not be laundered into 'head it back where it came from'");

            Vector3 geometric = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometric, aimNormal, 1.0f);

            Assert.That(achieved.z, Is.EqualTo(1.0f).Within(Tolerance),
                "the natural rebound, not the incident");
            Assert.That(Vector3.Angle(achieved, incident), Is.GreaterThan(1.0f),
                "the pre-fix path returned the incident itself here");
        }

        // ── The anti-tautology lock ──────────────────────────────────────────────────────

        /// <summary>
        /// THE lock: identical contact geometry, two <c>TargetIntent</c>s more than 90° apart, must
        /// produce materially different outgoing directions.
        ///
        /// <para>Pre-fix, the outgoing direction was a function of ball position, head centre and
        /// incoming velocity ONLY — none of which differ between the two cases here — so a mirror-only
        /// implementation returns the identical vector twice and this test fails. That is what makes it
        /// a lock on the mechanism rather than a restatement of it.</para>
        /// </summary>
        [Test]
        public void TwoDifferentTargets_ProduceMateriallyDifferentDirections()
        {
            Vector3 incomingVelocity = new Vector3(6.0f, 0.0f, -4.0f);
            Vector3 incident = -incomingVelocity.normalized;
            Vector3 contact = new Vector3(50.0f, 34.0f, 2.3f);

            Vector3 leftTarget = new Vector3(60.0f, 5.0f, 0.0f);
            Vector3 rightTarget = new Vector3(60.0f, 63.0f, 0.0f);

            Vector3 leftDir = OutgoingDirectionFor(contact, leftTarget, incident, incomingVelocity);
            Vector3 rightDir = OutgoingDirectionFor(contact, rightTarget, incident, incomingVelocity);

            float separationDegrees = Vector3.Angle(leftDir, rightDir);
            Assert.That(separationDegrees, Is.GreaterThan(45.0f),
                "aiming at opposite touchlines from one contact must send the ball materially differently; "
                + "a mirror-only model returns the same vector for both");
        }

        /// <summary>
        /// The consequence of the 2-D head-local round trip that ERR-010-002 removed: with a horizontal
        /// normal, reflected.z == incoming.z, so a DESCENDING ball stayed descending and no header could
        /// ever lift the ball. A dropping cross aimed long must now come off the head going up.
        /// </summary>
        [Test]
        public void DescendingBall_AimedLong_LeavesTheHeadRising()
        {
            Vector3 incomingVelocity = new Vector3(4.0f, 0.0f, -9.0f);
            Vector3 incident = -incomingVelocity.normalized;
            Vector3 contact = new Vector3(12.0f, 34.0f, 2.3f);
            Vector3 target = new Vector3(90.0f, 60.0f, 0.0f);

            Vector3 outgoing = OutgoingDirectionFor(contact, target, incident, incomingVelocity);

            Assert.That(outgoing.z, Is.GreaterThan(0.0f),
                "a defensive clearance must rise; pre-fix the reflection normal was always horizontal "
                + "so a descending ball was headed further down");
        }

        /// <summary>
        /// The aim solve is reflection-symmetric about the halfway line: mirrored geometry produces the
        /// mirrored outgoing direction.
        ///
        /// <para><b>This is NOT the ERR-008-002 home/away lock it was originally labelled as</b>, and the
        /// original label was corrected at the adversarial review over the ERR-010-002 landing.
        /// <see cref="HeadingAim"/> takes no team id at all — it is team-agnostic pure geometry, and
        /// both sides of this test run at <c>TeamId = 0</c>, so no team-asymmetry defect could ever fail
        /// it. The team branch in this landing lives in <c>GkHeadingIntentSource.HeaderAimTarget</c>, and
        /// the real ERR-008-002 lock for it is in that assembly's own fixture.</para>
        /// </summary>
        [Test]
        public void AimGeometry_MirrorsAboutTheHalfwayLine()
        {
            Vector3 homeIncoming = new Vector3(5.0f, 1.0f, -7.0f);
            Vector3 homeContact = new Vector3(20.0f, 30.0f, 2.3f);
            Vector3 homeTarget = new Vector3(105.0f, 10.0f, 0.0f);

            // Mirror x about the halfway line (105/2), keep y, negate the x components of motion.
            Vector3 awayIncoming = new Vector3(-homeIncoming.x, homeIncoming.y, homeIncoming.z);
            Vector3 awayContact = new Vector3(105.0f - homeContact.x, homeContact.y, homeContact.z);
            Vector3 awayTarget = new Vector3(105.0f - homeTarget.x, homeTarget.y, 0.0f);

            Vector3 homeOut = OutgoingDirectionFor(homeContact, homeTarget, -homeIncoming.normalized, homeIncoming);
            Vector3 awayOut = OutgoingDirectionFor(awayContact, awayTarget, -awayIncoming.normalized, awayIncoming);

            Assert.That(awayOut.x, Is.EqualTo(-homeOut.x).Within(Tolerance), "x must mirror");
            Assert.That(awayOut.y, Is.EqualTo(homeOut.y).Within(Tolerance), "y must not");
            Assert.That(awayOut.z, Is.EqualTo(homeOut.z).Within(Tolerance), "z must not");
        }

        /// <summary>
        /// Composes the three §3.5.1 steps plus the §3.5 reflection, at full steer authority, the way
        /// <c>HeadingMechanics</c> does. The reflection is restated locally rather than called so these
        /// locks do not verify production geometry through production geometry.
        /// </summary>
        private static Vector3 OutgoingDirectionFor(
            Vector3 contact,
            Vector3 target,
            Vector3 incident,
            Vector3 incomingVelocity)
        {
            HeadingAgentAttributes attrs = Elite();
            var intent = new HeaderIntent { PowerIntent = 1.0f, TargetIntent = target };
            float speed = HeadingPowerAngle.ComputeOutgoingSpeed(
                attrs, intent, HeadingMechanicsConstants.PERFECT_CONTACT_QUALITY);

            Vector3 aimDir = HeadingAim.ComputeAimDirection(contact, target, speed);
            Vector3 aimNormal = HeadingAim.ComputeAimNormal(incident, aimDir);

            // The contact geometry is IDENTICAL between the cases these locks compare: the ball sits
            // directly along the incoming path from the head centre, so the geometric normal carries no
            // information about the target.
            Vector3 geometricNormal = incident;
            Vector3 achieved = HeadingAim.ComputeAchievedNormal(geometricNormal, aimNormal, 1.0f);

            float dot = Vector3.Dot(incident, achieved);
            return (2.0f * dot * achieved - incident).normalized;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                  |
// | 1.0     | 2026-08-09 | —      | Initial. ERR-010-002 §3.5.1 locks: hand-derived ballistic launch angle |
// |         |            |        |   (low root), upward launch to a distant ground target, 45° fallback   |
// |         |            |        |   out of range, half-vector inversion of the §3.5 reflection, grazing- |
// |         |            |        |   hemisphere bound, full-range monotone attribute steering, degenerate |
// |         |            |        |   fallbacks, the two-targets anti-tautology lock, the descending-ball  |
// |         |            |        |   lift lock (the 2-D round-trip consequence), and the home/away mirror |
// |         |            |        |   (ERR-008-002 class).                                                 |
// |         |            |        |   [CORRECTED at 1.1 — two claims here are false as published: there is |
// |         |            |        |   no "grazing hemisphere bound" (removed from HeadingAim before it     |
// |         |            |        |   landed), and the mirror test is NOT an ERR-008-002 lock, since       |
// |         |            |        |   HeadingAim takes no team id. Annotated, not rewritten.]              |
// | 1.1     | 2026-08-09 | —      | AR over the ERR-010-002 landing. FallsBackToMaxRangeLaunch renamed and |
// |         |            |        |   re-derived: it asserted a flat 45°, which is the max-range angle     |
// |         |            |        |   only for dz = 0 and so locked the wrong value on the production      |
// |         |            |        |   path. + IsContinuousAcrossTheReachabilityBoundary (the P1 property   |
// |         |            |        |   §3.5.1 claimed and did not have — 2.42° now vs 9.98° pre-fix        |
// |         |            |        |   against a 4° bound; the bound is loose because the low root has a    |
// |         |            |        |   sqrt singularity at the boundary, which is physics, not the defect), |
// |         |            |        |   + TargetAboveReach_LaunchesVertically (the new radicand guard), and  |
// |         |            |        |   + DegenerateAimDirection_PropagatesAsDegenerate, which is the lock   |
// |         |            |        |   AchievedNormal_DegenerateAim could not be: that one supplies a zero  |
// |         |            |        |   the composition could never produce, so it passed against a branch   |
// |         |            |        |   production was unable to enter. Mirror-test doc claim corrected.     |
#endregion
