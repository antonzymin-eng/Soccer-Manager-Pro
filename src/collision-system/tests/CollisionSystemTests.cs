// File:     src/collision-system/tests/CollisionSystemTests.cs
// Created:  2026-05-31
// Modified: 2026-06-10  [v1.3]
// Author:   —
// Spec:     Collision System #3 §5.2, §5.3, Code Standards #20
// Purpose:  Unit tests for SpatialHashGrid, CollisionDetection, CollisionResponse,
//           fall/stumble logic, edge cases, and determinism. Tests SH-001..006,
//           CD-001..007, CR-001..005, FL-001..005, EC-001..004, DT-001..003.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem.Tests
{
    // ── SH: Spatial Hash Tests ────────────────────────────────────────────────

    [TestFixture]
    internal class SpatialHashTests
    {
        private SpatialHashGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new SpatialHashGrid();
        }

        [TearDown]
        public void TearDown()
        {
            _grid.Clear();
        }

        // SH-001
        [Test]
        public void SpatialHash_InsertAtGridCenter_QueryReturnsSelf()
        {
            _grid.Insert(0, new Vector3(52.5f, 34f, 0f), 0.40f);
            List<int> result = _grid.Query(new Vector3(52.5f, 34f, 0f), 0.40f);
            Assert.IsTrue(result.Contains(0), "SH-001: query at insert position must return self");
        }

        // SH-002
        [Test]
        public void SpatialHash_TwoAgentsClose_QueryReturnsBoth()
        {
            _grid.Insert(0, new Vector3(50.0f, 34.0f, 0f), 0.40f);
            _grid.Insert(1, new Vector3(50.5f, 34.0f, 0f), 0.40f);
            List<int> r0 = _grid.Query(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            List<int> r1 = _grid.Query(new Vector3(50.5f, 34.0f, 0f), 0.40f);
            Assert.IsTrue(r0.Contains(1), "SH-002: query from id0 must include id1");
            Assert.IsTrue(r1.Contains(0), "SH-002: query from id1 must include id0");
        }

        // SH-003
        [Test]
        public void SpatialHash_TwoAgentsFar_QueryReturnsOnlySelf()
        {
            _grid.Insert(0, new Vector3(50.0f, 34.0f, 0f), 0.40f);
            _grid.Insert(1, new Vector3(52.0f, 34.0f, 0f), 0.40f);
            List<int> r0 = _grid.Query(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            Assert.IsFalse(r0.Contains(1), "SH-003: distant agent must not appear in query");
        }

        // SH-004
        [Test]
        public void SpatialHash_InsertAtBoundary_NoIndexOutOfBounds()
        {
            Assert.DoesNotThrow(() =>
            {
                _grid.Insert(0, new Vector3(0.0f, 0.0f, 0f), 0.40f);
                _grid.Insert(1, new Vector3(105.0f, 68.0f, 0f), 0.40f);
                _grid.Insert(2, new Vector3(-0.5f, 34.0f, 0f), 0.40f);
                _grid.Insert(3, new Vector3(105.5f, 34.0f, 0f), 0.40f);
            }, "SH-004: out-of-bounds positions must not throw");
        }

        // SH-005
        [Test]
        public void SpatialHash_Insert22Agents_AllInserted()
        {
            for (int i = 0; i < 22; i++)
            {
                float x = (i % 11) * 9.5f + 5.0f;
                float y = (i / 11) * 34.0f + 17.0f;
                _grid.Insert(i, new Vector3(x, y, 0f), 0.40f);
            }
            int found = 0;
            for (int i = 0; i < 22; i++)
            {
                float x = (i % 11) * 9.5f + 5.0f;
                float y = (i / 11) * 34.0f + 17.0f;
                List<int> r = _grid.Query(new Vector3(x, y, 0f), 0.40f);
                if (r.Contains(i)) found++;
            }
            Assert.AreEqual(22, found, "SH-005: all 22 agents must be queryable at their positions");
        }

        // SH-006
        [Test]
        public void SpatialHash_EntityStraddlesCellBoundary_InsertedInMultipleCells()
        {
            // Position at cell boundary: 50.0 / CellSize=1.0 → exactly on boundary
            _grid.Insert(0, new Vector3(50.0f, 34.0f, 0f), 0.40f);

            List<int> fromPrimary  = _grid.Query(new Vector3(50.0f,  34.0f, 0f), 0.01f);
            List<int> fromAdjacent = _grid.Query(new Vector3(49.75f, 34.0f, 0f), 0.01f);

            Assert.IsTrue(fromPrimary.Contains(0),  "SH-006: entity found in primary cell");
            Assert.IsTrue(fromAdjacent.Contains(0), "SH-006: entity found in adjacent cell (boundary overlap)");
        }
    }

    // ── CD: Collision Detection Tests ─────────────────────────────────────────

    [TestFixture]
    internal class CollisionDetectionTests
    {
        // CD-001
        [Test]
        public void Detection_AgentsExactlyTouching_ReturnsCollision()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var a2 = MakeAgent(new Vector3(50.8f, 34.0f, 0f), 0.40f);
            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit, "CD-001: exactly touching agents must detect collision");
            Assert.That(m.PenetrationDepth, Is.EqualTo(0.0f).Within(0.001f), "CD-001: touching = zero penetration");
        }

        // CD-002
        [Test]
        public void Detection_AgentsNotTouching_ReturnsNoCollision()
        {
            var a1 = MakeAgent(new Vector3(50.00f, 34.0f, 0f), 0.40f);
            var a2 = MakeAgent(new Vector3(50.81f, 34.0f, 0f), 0.40f);
            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold _);
            Assert.IsFalse(hit, "CD-002: gap > combined radius must not detect collision");
        }

        // CD-003
        [Test]
        public void Detection_AgentsOverlapping_ReturnsPenetrationDepth()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var a2 = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit, "CD-003: same position must detect collision");
            Assert.That(m.PenetrationDepth, Is.EqualTo(0.80f).Within(0.001f), "CD-003: penetration must equal combined radii");
        }

        // CD-004
        [Test]
        public void Detection_AgentBallTouching_ReturnsCollision()
        {
            var agent = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var ball  = MakeBall(new Vector3(50.5f, 34.0f, 0.05f));
            bool hit = CollisionDetection.CheckAgentBallCollision(in agent, in ball, out Vector3 _);
            Assert.IsTrue(hit, "CD-004: agent within combined reach must detect ball collision");
        }

        // CD-005
        [Test]
        public void Detection_AgentBallNotTouching_ReturnsNoCollision()
        {
            var agent = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var ball  = MakeBall(new Vector3(50.52f, 34.0f, 0.05f));
            bool hit = CollisionDetection.CheckAgentBallCollision(in agent, in ball, out Vector3 _);
            Assert.IsFalse(hit, "CD-005: just outside combined radius must not detect ball collision");
        }

        // CD-006
        [Test]
        public void Detection_GroundedAgentStillDetectable()
        {
            var a1 = MakeGroundedAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var a2 = MakeAgent(new Vector3(50.5f, 34.0f, 0f), 0.40f);
            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold _);
            Assert.IsTrue(hit, "CD-006: grounded agent still counts as obstacle for detection");
        }

        // CD-007: ball above AgentReachHeight is not detectable
        [Test]
        public void Detection_BallAboveReachHeight_NotDetectable()
        {
            var agent = MakeAgent(new Vector3(50.0f, 34.0f, 0f), 0.40f);
            var ball  = MakeBall(new Vector3(50.3f, 34.0f, CollisionPhysicsConstants.AgentReachHeight + 0.1f));
            bool hit = CollisionDetection.CheckAgentBallCollision(in agent, in ball, out Vector3 _);
            Assert.IsFalse(hit, "CD-007: ball above reach height must not trigger agent-ball collision");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentPhysicalProperties MakeAgent(Vector3 pos, float radius) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = Vector3.zero,
                HitboxRadius = radius, Mass = 85f, Strength = 10, Agility = 10
            };

        private static AgentPhysicalProperties MakeGroundedAgent(Vector3 pos, float radius) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = Vector3.zero,
                HitboxRadius = radius, Mass = 85f, Strength = 10, Agility = 10,
                IsGrounded = true
            };

        private static BallState MakeBall(Vector3 pos)
        {
            var b = new BallState();
            b.Position = pos;
            return b;
        }
    }

    // ── CR: Collision Response Tests ──────────────────────────────────────────

    [TestFixture]
    internal class CollisionResponseTests
    {
        // CR-001: equal-mass head-on → velocities reverse.
        // Impulse applies when vRel = dot(v1-v2, n) > 0 (n = a1→a2; approaching; ERR-003-005).
        // Setup: a1 left of a2, both moving toward each other.
        // Normal = (a2-a1)/dist = (+1, 0); vRel = dot((10,0),(+1,0)) = +10 → impulse.
        // j = 1.3*10/(2/85) ≈ 552.5; Δv = 6.5 m/s each → ∓1.5 m/s final (reversed).
        [Test]
        public void Response_EqualMassHeadOn_VelocitiesReverse()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 5f, 0f, 0f), 85f, 0.40f, 10, false);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-5f, 0f, 0f), 85f, 0.40f, 10, false);

            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit, "CR-001: agents must overlap for response test");

            var rng = new DeterministicRNG(0UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            float v1x = a1.Velocity.x + r.VelocityImpulse1.x;
            float v2x = a2.Velocity.x + r.VelocityImpulse2.x;

            Assert.That(v1x, Is.EqualTo(-1.5f).Within(0.3f), "CR-001: a1 must reverse to ≈-1.5 m/s");
            Assert.That(v2x, Is.EqualTo( 1.5f).Within(0.3f), "CR-001: a2 must reverse to ≈+1.5 m/s");
        }

        // CR-002: heavy (100 kg) hits stationary light (72.5 kg).
        // a1=heavy left of a2=light, moving right: normal=(+1,0); vRel = dot((8,0),(+1,0)) = +8 → impulse.
        [Test]
        public void Response_HeavyHitsLight_HeavyBarelySlows()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3(8f, 0f, 0f), 100f, 0.40f, 20, false);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(0f, 0f, 0f),  72.5f, 0.35f, 1, false);

            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit, "CR-002: overlap required");

            var rng = new DeterministicRNG(1UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            float v1x = a1.Velocity.x + r.VelocityImpulse1.x;
            float v2x = a2.Velocity.x + r.VelocityImpulse2.x;

            Assert.Greater(v1x, 0f, "CR-002: heavy agent still moves forward (just slowed)");
            Assert.Less(v1x, a1.Velocity.x, "CR-002: heavy agent must be slowed");
            Assert.Greater(Mathf.Abs(v2x), 1.0f, "CR-002: light agent must be knocked back significantly");
        }

        // CR-003: same-team collision applies 30% impulse scale.
        [Test]
        public void Response_SameTeam_ReducedMomentum()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 5f, 0f, 0f), 85f, 0.40f, 10, false);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-5f, 0f, 0f), 85f, 0.40f, 10, false);

            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit);

            var rngOpp  = new DeterministicRNG(0UL);
            var rngSame = new DeterministicRNG(0UL);
            var rOpp  = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rngOpp);
            var rSame = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, true,  ref rngSame);

            float dvOpp  = Mathf.Abs(rOpp.VelocityImpulse1.x);
            float dvSame = Mathf.Abs(rSame.VelocityImpulse1.x);

            Assert.Greater(dvOpp, 0f, "CR-003: opposing team produces non-zero impulse");
            Assert.That(dvSame, Is.EqualTo(dvOpp * CollisionPhysicsConstants.SameTeamMomentumScale).Within(0.01f),
                "CR-003: same-team impulse must be 30% of opposing-team impulse");
        }

        // CR-004: penetration resolved → agents no longer overlap.
        [Test]
        public void Response_Separation_ResolvesPenetration()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), Vector3.zero, 85f, 0.40f, 10, false);
            var a2 = MakeAgent(new Vector3(50.6f, 34f, 0f), Vector3.zero, 85f, 0.40f, 10, false);

            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit, "CR-004: overlap required for separation test");

            var rng = new DeterministicRNG(0UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            Vector3 newPos1 = a1.Position + r.PositionCorrection1;
            Vector3 newPos2 = a2.Position + r.PositionCorrection2;
            float dist = Vector2.Distance(
                new Vector2(newPos1.x, newPos1.y),
                new Vector2(newPos2.x, newPos2.y));
            float combinedR = a1.HitboxRadius + a2.HitboxRadius;

            Assert.GreaterOrEqual(dist, combinedR * 0.99f,
                "CR-004: after separation, distance must be >= combined radii (within 1% margin)");
        }

        // CR-005: parallel velocities → no impulse (only separation).
        [Test]
        public void Response_ParallelVelocity_NoImpulse()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3(5f, 0f, 0f), 85f, 0.40f, 10, false);
            var a2 = MakeAgent(new Vector3(50.6f, 34f, 0f), new Vector3(5f, 0f, 0f), 85f, 0.40f, 10, false);

            bool hit = CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.IsTrue(hit);

            var rng = new DeterministicRNG(0UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            Assert.That(r.VelocityImpulse1.magnitude, Is.LessThan(0.01f),
                "CR-005: same-direction velocities produce no impulse");
            Assert.That(r.VelocityImpulse2.magnitude, Is.LessThan(0.01f),
                "CR-005: same-direction velocities produce no impulse");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentPhysicalProperties MakeAgent(Vector3 pos, Vector3 vel,
            float mass, float radius, int strength, bool grounded) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = vel,
                HitboxRadius = radius, Mass = mass,
                Strength = strength, Agility = strength,
                IsGrounded = grounded
            };
    }

    // ── FL: Fall / Stumble Logic Tests ────────────────────────────────────────

    [TestFixture]
    internal class FallStumbleTests
    {
        // FL-001: force below stumble threshold → no state change.
        [Test]
        public void FallLogic_ForceBelowStumbleThreshold_NoStateChange()
        {
            // Strength 10: fallThreshold = 500 + 10×50 = 1000 N; stumbleThreshold = 500 N.
            // F = j / ContactDurationS (0.15 s); equal mass (85 kg): j = 1.3·vRel·42.5 → F ≈ 368·vRel.
            // Target impactForce ≈ 330 N < 500 N → vRel ≈ 0.9 m/s.
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 0.45f, 0f, 0f), 85f, 0.40f, 10);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-0.45f, 0f, 0f), 85f, 0.40f, 10);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            var rng = new DeterministicRNG(12345UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            Assert.IsFalse(r.TriggerGrounded1, "FL-001: low force must not trigger grounded");
            Assert.IsFalse(r.TriggerStumble1,  "FL-001: low force must not trigger stumble");
        }

        // FL-002: force in stumble zone → ~50% stumble probability.
        [Test]
        public void FallLogic_ForceAtStumbleThreshold_StumbleProbabilityPositive()
        {
            // Strength 10: stumble zone = 500..1000 N. Target: ~760 N → prob ≈ 0.52.
            // F ≈ 368·vRel → vRel ≈ 2.06 m/s for ~759 N.
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.03f, 0f, 0f), 85f, 0.40f, 10);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.03f, 0f, 0f), 85f, 0.40f, 10);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);

            int stumbleCount = 0;
            for (ulong seed = 0; seed < 10000; seed++)
            {
                var rng = new DeterministicRNG(seed);
                var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);
                if (r.TriggerStumble1) stumbleCount++;
            }

            Assert.Greater(stumbleCount, 1000, "FL-002: stumble must occur in more than 10% of cases");
            Assert.Less(stumbleCount, 9000, "FL-002: stumble must not be near-certain at mid-range force");
        }

        // FL-003: force above fall threshold → ~50% fall probability.
        [Test]
        public void FallLogic_ForceAtFallThreshold_FallProbabilityPositive()
        {
            // Strength 10: fallThreshold = 1000 N. Target ≈ 1250 N → prob ≈ 0.5.
            // F ≈ 368·vRel → vRel ≈ 3.4 m/s (a genuine two-way running contact).
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.7f, 0f, 0f), 85f, 0.40f, 10);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.7f, 0f, 0f), 85f, 0.40f, 10);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);

            int groundedCount = 0;
            for (ulong seed = 0; seed < 10000; seed++)
            {
                var rng = new DeterministicRNG(seed);
                var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);
                if (r.TriggerGrounded1) groundedCount++;
                // When grounded triggers, stumble must NOT also trigger.
                if (r.TriggerGrounded1)
                    Assert.IsFalse(r.TriggerStumble1, "FL-003: grounded and stumble mutually exclusive");
            }

            Assert.Greater(groundedCount, 1000, "FL-003: fall must occur in more than 10% of high-force cases");
        }

        // FL-004: Strength 1 falls at 1000 N; Strength 20 does not.
        [Test]
        public void FallLogic_Strength20RequiresMoreForceThanStrength1()
        {
            // Strength 1: fallThreshold = 500 + 1×50 = 550 N. P(fall at ~1000 N) ≈ 0.9.
            // Strength 20: fallThreshold = 500 + 20×50 = 1500 N. P(fall at ~1000 N) = 0.
            // F ≈ 368·vRel → vRel ≈ 2.72 m/s for ~1002 N.
            var aStr1a = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.36f, 0f, 0f), 85f, 0.40f, 1);
            var aStr1b = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.36f, 0f, 0f), 85f, 0.40f, 1);

            var aStr20a = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.36f, 0f, 0f), 85f, 0.40f, 20);
            var aStr20b = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.36f, 0f, 0f), 85f, 0.40f, 20);

            CollisionDetection.CheckAgentAgentCollision(in aStr1a,  in aStr1b,  out CollisionManifold m1);
            CollisionDetection.CheckAgentAgentCollision(in aStr20a, in aStr20b, out CollisionManifold m20);

            int falls1 = 0, falls20 = 0;
            for (ulong seed = 0; seed < 100; seed++)
            {
                var rng1 = new DeterministicRNG(seed);
                var r1 = CollisionResponse.CalculateAgentAgentResponse(in aStr1a, in aStr1b, in m1, false, ref rng1);
                if (r1.TriggerGrounded1) falls1++;

                var rng20 = new DeterministicRNG(seed);
                var r20 = CollisionResponse.CalculateAgentAgentResponse(in aStr20a, in aStr20b, in m20, false, ref rng20);
                if (r20.TriggerGrounded1) falls20++;
            }

            Assert.Greater(falls1,  0, "FL-004: Strength 1 agent must fall at ~1000 N (P=0.9)");
            Assert.AreEqual(0, falls20, "FL-004: Strength 20 agent must not fall at ~1000 N (threshold 1500 N)");
        }

        // FL-005: TriggerGrounded sets correctly when force exceeds fall threshold.
        [Test]
        public void FallLogic_GroundedTriggered_WhenForceExceedsFallThreshold()
        {
            // Strength 1 (threshold 550 N): vRel = 3.0 m/s → F ≈ 1105 N; excess 555 N over the
            // 500 N probability range → P(fall) clamps to 1.0 (near-certain fall).
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3(1.5f, 0f, 0f), 85f, 0.40f, 1);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.5f, 0f, 0f), 85f, 0.40f, 1);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);

            bool anyGrounded = false;
            for (ulong seed = 0; seed < 20; seed++)
            {
                var rng = new DeterministicRNG(seed);
                var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);
                if (r.TriggerGrounded1) { anyGrounded = true; break; }
            }
            Assert.IsTrue(anyGrounded, "FL-005: very high force must eventually trigger grounded state");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentPhysicalProperties MakeAgent(Vector3 pos, Vector3 vel,
            float mass, float radius, int strength) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = vel,
                HitboxRadius = radius, Mass = mass,
                Strength = strength, Agility = strength
            };
    }

    // ── EC: Edge Case Tests ───────────────────────────────────────────────────

    [TestFixture]
    internal class CollisionEdgeCaseTests
    {
        // EC-001: NaN position → no crash, agent skipped.
        [Test]
        public void EdgeCase_NaNPosition_RecoveryWithoutCrash()
        {
            var grid = new SpatialHashGrid();
            Assert.DoesNotThrow(() =>
            {
                grid.Insert(0, new Vector3(float.NaN, 34.0f, 0f), 0.40f);
            }, "EC-001: NaN position must not throw");

            List<int> result = grid.Query(new Vector3(0.5f, 34.0f, 0f), 0.40f);
            Assert.IsFalse(result.Contains(0), "EC-001: NaN-positioned agent must not appear in queries");
        }

        // EC-002: excessive penetration → separation capped at MaxPenetrationDepth.
        [Test]
        public void EdgeCase_ExcessivePenetration_SeparationCapped()
        {
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), Vector3.zero, 85f, 0.40f);
            var a2 = MakeAgent(new Vector3(50.0f, 34f, 0f), Vector3.zero, 85f, 0.40f);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);
            Assert.Greater(m.PenetrationDepth, FallThresholdConstants.MaxPenetrationDepth,
                "EC-002: full overlap penetration must exceed MaxPenetrationDepth");

            var rng = new DeterministicRNG(0UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng);

            float totalSep = r.PositionCorrection1.magnitude + r.PositionCorrection2.magnitude;
            Assert.LessOrEqual(totalSep,
                FallThresholdConstants.MaxPenetrationDepth * CollisionPhysicsConstants.SeparationSlop + 0.01f,
                "EC-002: separation must be capped at MaxPenetrationDepth");
        }

        // EC-003: 10 clustered agents → all pairs processed without overflow.
        [Test]
        public void EdgeCase_MaxCollisionPairs_ProcessedWithoutOverflow()
        {
            var grid = new SpatialHashGrid();
            for (int i = 0; i < 10; i++)
            {
                float x = 50.0f + (i % 3) * 0.3f;
                float y = 34.0f + (i / 3) * 0.3f;
                grid.Insert(i, new Vector3(x, y, 0f), 0.40f);
            }

            Assert.DoesNotThrow(() =>
            {
                List<int> results = grid.Query(new Vector3(50.5f, 34.5f, 0f), 1.5f);
                Assert.LessOrEqual(results.Count, SpatialHashConstants.AgentCapacity * 9,
                    "EC-003: result count must be bounded");
            }, "EC-003: 10 clustered agents must not cause overflow or exception");
        }

        // EC-004: grounded agent collision → grounded receives no impulse.
        [Test]
        public void EdgeCase_GroundedAgentCollision_NoImpulseGenerated()
        {
            var grounded = new AgentPhysicalProperties
            {
                Position = new Vector3(50.0f, 34f, 0f), Velocity = Vector3.zero,
                HitboxRadius = 0.40f, Mass = 85f, Strength = 10, IsGrounded = true
            };
            var runner = new AgentPhysicalProperties
            {
                Position = new Vector3(50.6f, 34f, 0f), Velocity = new Vector3(-5f, 0f, 0f),
                HitboxRadius = 0.40f, Mass = 85f, Strength = 10, IsGrounded = false
            };

            CollisionDetection.CheckAgentAgentCollision(in grounded, in runner, out CollisionManifold m);
            var rng = new DeterministicRNG(0UL);
            var r = CollisionResponse.CalculateAgentAgentResponse(in grounded, in runner, in m, false, ref rng);

            Assert.That(r.VelocityImpulse1.magnitude, Is.LessThan(0.001f),
                "EC-004: grounded agent must receive no velocity impulse");
            Assert.That(r.PositionCorrection1.magnitude, Is.LessThan(0.001f),
                "EC-004: grounded agent must receive no position correction");
        }

        private static AgentPhysicalProperties MakeAgent(Vector3 pos, Vector3 vel,
            float mass, float radius) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = vel,
                HitboxRadius = radius, Mass = mass, Strength = 10, Agility = 10
            };
    }

    // ── DT: Determinism Tests ─────────────────────────────────────────────────

    [TestFixture]
    internal class CollisionDeterminismTests
    {
        // DT-001: same seed → identical fall outcomes.
        [Test]
        public void Determinism_SameSeed_IdenticalOutcomes()
        {
            // vRel = 3.4 m/s → F ≈ 1252 N → P(fall) ≈ 0.5 (stochastic band; outcome seed-driven).
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.7f, 0f, 0f), 85f, 0.40f, 10);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.7f, 0f, 0f), 85f, 0.40f, 10);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);

            const ulong FixedSeed = 12345UL;
            bool[] run1 = new bool[100];
            bool[] run2 = new bool[100];

            for (int i = 0; i < 100; i++)
            {
                var rng1 = new DeterministicRNG(FixedSeed + (ulong)i);
                run1[i] = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng1).TriggerGrounded1;
            }
            for (int i = 0; i < 100; i++)
            {
                var rng2 = new DeterministicRNG(FixedSeed + (ulong)i);
                run2[i] = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rng2).TriggerGrounded1;
            }

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(run1[i], run2[i], $"DT-001: outcome at index {i} must be identical for same seed");
        }

        // DT-002: different seeds → different outcome sequences.
        [Test]
        public void Determinism_DifferentSeeds_DifferentOutcomes()
        {
            // vRel = 3.4 m/s → P(fall) ≈ 0.5, so different seed streams must diverge.
            var a1 = MakeAgent(new Vector3(50.0f, 34f, 0f), new Vector3( 1.7f, 0f, 0f), 85f, 0.40f, 10);
            var a2 = MakeAgent(new Vector3(50.7f, 34f, 0f), new Vector3(-1.7f, 0f, 0f), 85f, 0.40f, 10);

            CollisionDetection.CheckAgentAgentCollision(in a1, in a2, out CollisionManifold m);

            bool[] seqA = new bool[100];
            bool[] seqB = new bool[100];

            for (int i = 0; i < 100; i++)
            {
                var rA = new DeterministicRNG(12345UL + (ulong)i);
                seqA[i] = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rA).TriggerGrounded1;

                var rB = new DeterministicRNG(99999UL + (ulong)i);
                seqB[i] = CollisionResponse.CalculateAgentAgentResponse(in a1, in a2, in m, false, ref rB).TriggerGrounded1;
            }

            int differences = 0;
            for (int i = 0; i < 100; i++)
                if (seqA[i] != seqB[i]) differences++;

            Assert.GreaterOrEqual(differences, 5, "DT-002: different seeds must produce different outcome sequences");
        }

        // DT-003: full-frame replay with fixed seed → bit-identical results.
        [Test]
        public void Determinism_FullFrameReplay_BitIdentical()
        {
            const ulong Seed = 99999UL;
            var agents = new AgentPhysicalProperties[4];
            for (int i = 0; i < 4; i++)
            {
                agents[i] = new AgentPhysicalProperties
                {
                    Position = new Vector3(50f + i * 0.3f, 34f, 0f),
                    Velocity = new Vector3((i % 2 == 0 ? 1f : -1f) * 0.2f, 0f, 0f),
                    HitboxRadius = 0.40f, Mass = 85f, Strength = 10, Agility = 10
                };
            }

            float[] impulses1 = RunFrame(agents, Seed);
            float[] impulses2 = RunFrame(agents, Seed);

            for (int i = 0; i < impulses1.Length; i++)
                Assert.AreEqual(impulses1[i], impulses2[i],
                    $"DT-003: impulse[{i}] must be bit-identical across replays");
        }

        private static float[] RunFrame(AgentPhysicalProperties[] agents, ulong seed)
        {
            var rng = new DeterministicRNG(seed);
            var impulses = new float[agents.Length];

            for (int i = 0; i < agents.Length; i++)
            {
                for (int j = i + 1; j < agents.Length; j++)
                {
                    if (CollisionDetection.CheckAgentAgentCollision(in agents[i], in agents[j], out CollisionManifold m))
                    {
                        var r = CollisionResponse.CalculateAgentAgentResponse(in agents[i], in agents[j], in m, false, ref rng);
                        impulses[i] += r.VelocityImpulse1.magnitude;
                        impulses[j] += r.VelocityImpulse2.magnitude;
                    }
                }
            }
            return impulses;
        }

        private static AgentPhysicalProperties MakeAgent(Vector3 pos, Vector3 vel,
            float mass, float radius, int strength) =>
            new AgentPhysicalProperties
            {
                Position = pos, Velocity = vel,
                HitboxRadius = radius, Mass = mass,
                Strength = strength, Agility = strength
            };
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.3 Integration Tests
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Integration tests for Collision System #3 §5.3.
    /// All require Play Mode with AgentMovement, BallPhysics, and EventBus wired.
    /// </summary>
    internal sealed class CollisionSystemIntegrationTests
    {
        /// <summary>Integration_TwoAgentsApproach_BothBounceBack. §5.3.</summary>
        [Test]
        public void TwoAgentsApproach_BothBounceBack()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with full CollisionSystem.Tick — §5.3");
        }

        /// <summary>Integration_SprintingHitsStationary_StationaryKnockedBack. §5.3.</summary>
        [Test]
        public void SprintingHitsStationary_StationaryKnockedBack()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with AgentMovement #2 wired — §5.3");
        }

        /// <summary>Integration_AgentHitsBall_BallOnCollisionCalled. §5.3.</summary>
        [Test]
        public void AgentHitsBall_BallCollisionHandlerCalled()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with BallPhysics #1 wired — §5.3");
        }

        /// <summary>Integration_ThreeAgentsCluster_AllProcessedNoLoop. §5.3.</summary>
        [Test]
        public void ThreeAgentsCluster_AllProcessedNoInfiniteLoop()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode 3-agent scenario — §5.3");
        }

        /// <summary>Integration_GroundedObstacle_RunnerStumbles. §5.3.</summary>
        [Test]
        public void GroundedObstacle_RunnerStumbles()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode stumble/grounded logic — §5.3");
        }

        /// <summary>Integration_FullMatch90Minutes_NoCrashNoNaN. §5.3.</summary>
        [Test]
        public void FullMatch90Minutes_NoCrashNoNaN()
        {
            Assert.Ignore("Stage 0+1: requires full 90-minute match simulation — §5.3");
        }

        /// <summary>Integration_CornerKickClustering_PerformanceUnder500us. §5.3.</summary>
        [Test]
        public void CornerKickClustering_PerformanceUnder500us()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with Profiler — §5.3");
        }

        /// <summary>Integration_SameTeamCollision_ReducedMomentumNoFall. §5.3.</summary>
        [Test]
        public void SameTeamCollision_ReducedMomentumNoFall()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with team-ID filtering — §5.3");
        }

        /// <summary>CrossSystem_CollisionTriggersAgentMovementState. §5.3.</summary>
        [Test]
        public void CrossSystem_CollisionTriggersAgentMovementStateChange()
        {
            Assert.Ignore("Stage 0+1: requires AgentMovement #2 and CollisionSystem #3 both wired — §5.3");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-31 | —      | Initial implementation. |
// | 1.1     | 2026-06-01 | —      | Add §5.3 integration test stubs (9 tests; all Assert.Ignore Stage 0+1). |
// | 1.2     | 2026-06-10 | —      | AR-7 H-1 follow-through (ERR-003-001): FL-001..005 + DT-001..002 closing speeds |
// |         |            |        | re-derived for F = j / ContactDurationS (≈368·vRel for the 85 kg pair) — the old |
// |         |            |        | 0.1–0.46 m/s band encoded the inflated j × 60 Hz scale and made the calibration |
// |         |            |        | defect invisible to the suite. Expected probabilities unchanged by design.      |
// | 1.3     | 2026-06-10 | —      | AR-8 H-1 follow-through (ERR-003-005): CR-001..003 / FL-001..005 / DT-001..002 |
// |         |            |        | / EC-004 setups flipped from outward ("passed-through") to genuinely           |
// |         |            |        | approaching velocities — the suite previously encoded the inverted gate (the   |
// |         |            |        | CR-001 comment rationalised separating agents as the impulse case). CR-001     |
// |         |            |        | post-collision expectations unchanged (a1 now starts at the left, +5 → −1.5).  |
// |         |            |        | AR-8 L-1: stray namespace-closing brace before the §5.3 integration section    |
// |         |            |        | removed — the namespace closed at the determinism fixture, leaving the         |
// |         |            |        | integration class at file scope and an unbalanced `}` at EOF (file could not   |
// |         |            |        | have compiled; latent because the Unity project is not yet initialised).       |
#endregion
