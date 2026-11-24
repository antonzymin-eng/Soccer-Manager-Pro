// File:     src/agent-movement/Tests/AgentMovementTrackingTests.cs
// Created:  2026-11-21
// Modified: 2026-11-21
// Author:   —
// Spec:     Agent Movement #2 §3.5.3.1 (tracking command), ERR-002-001, Code Standards #20
// Purpose:  Unit and closed-loop tests of the tracking command: the pure law, stationary arrival without orbit,
//           moving-point following, staleness freeze, determinism, and the non-tracking factories' unchanged fields.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.AgentMovement.Tests
{
    /// <summary>Agent Movement #2 §3.5.3.1 — arrival braking + target-velocity feed-forward.</summary>
    [TestFixture]
    public sealed class AgentMovementTrackingTests
    {
        private const float Dt = 1.0f / 60.0f;
        private const int FramesPerHeartbeat = 6;

        // ── the pure law ─────────────────────────────────────────────────────────

        /// <summary>Worked examples of §3.5.3.1: v_arr = √(2·2.5·(d − 0.1)) toward the point, plus the feed-forward.</summary>
        [Test]
        public void Law_WorkedExamples()
        {
            MovementCommand still = MovementCommand.Track(new Vector2(11.0f, 0.0f), Vector2.zero, 0.0f);
            Vector2 v = AgentMovementSystem.CalculateTrackDesiredVelocity(in still, new Vector2(10.0f, 0.0f), 0.0f);
            Assert.AreEqual(Mathf.Sqrt(2.0f * 2.5f * 0.9f), v.x, 1e-4f);   // 2.121 m/s
            Assert.AreEqual(0.0f, v.y, 1e-6f);

            MovementCommand moving = MovementCommand.Track(new Vector2(10.5f, 0.0f), new Vector2(3.0f, 0.0f), 0.0f);
            Vector2 w = AgentMovementSystem.CalculateTrackDesiredVelocity(in moving, new Vector2(10.0f, 0.0f), 0.0f);
            Assert.AreEqual(3.0f + Mathf.Sqrt(2.0f * 2.5f * 0.4f), w.x, 1e-4f);   // 4.414 m/s
        }

        /// <summary>Inside the settle tolerance (0.1 m) the arrival term is zero: a stationary point gives zero velocity, a moving one exactly its velocity.</summary>
        [Test]
        public void Law_InsideTheSettleTolerance_IsTheFeedForwardOnly()
        {
            MovementCommand still = MovementCommand.Track(new Vector2(10.05f, 0.0f), Vector2.zero, 0.0f);
            Assert.AreEqual(Vector2.zero, AgentMovementSystem.CalculateTrackDesiredVelocity(in still, new Vector2(10.0f, 0.0f), 0.0f));
            MovementCommand moving = MovementCommand.Track(new Vector2(10.05f, 0.0f), new Vector2(0.0f, 2.0f), 0.0f);
            Assert.AreEqual(new Vector2(0.0f, 2.0f), AgentMovementSystem.CalculateTrackDesiredVelocity(in moving, new Vector2(10.0f, 0.0f), 0.0f));
        }

        /// <summary>The point is extrapolated by its velocity over the command's age (here 0.1 s ⇒ +0.3 m), and the agent standing on the extrapolated point gets exactly the feed-forward.</summary>
        [Test]
        public void Law_ExtrapolatesThePointOverTheCommandAge()
        {
            MovementCommand c = MovementCommand.Track(new Vector2(10.0f, 0.0f), new Vector2(3.0f, 0.0f), 5.0f);
            Vector2 v = AgentMovementSystem.CalculateTrackDesiredVelocity(in c, new Vector2(10.3f, 0.0f), 5.1f);
            Assert.AreEqual(3.0f, v.x, 1e-3f);
            Assert.AreEqual(0.0f, v.y, 1e-3f);
        }

        /// <summary>A command older than TRACK_EXTRAPOLATION_MAX_S (0.2 s) was not refreshed: the point freezes at the 0.2 s extrapolation and the feed-forward drops to zero.</summary>
        [Test]
        public void Law_StaleCommand_FreezesThePointAndDropsTheFeedForward()
        {
            Assert.AreEqual(0.2f, LocomotionConstants.TRACK_EXTRAPOLATION_MAX_S);
            MovementCommand c = MovementCommand.Track(new Vector2(10.0f, 0.0f), new Vector2(3.0f, 0.0f), 5.0f);
            // Standing on the 0.2 s extrapolation (10.6), one second later: nothing to do.
            Assert.AreEqual(Vector2.zero, AgentMovementSystem.CalculateTrackDesiredVelocity(in c, new Vector2(10.6f, 0.0f), 6.0f));
            // Exactly one heartbeat old (the oldest a refreshed command gets): still fresh.
            Vector2 v = AgentMovementSystem.CalculateTrackDesiredVelocity(in c, new Vector2(10.3f, 0.0f), 5.0f + 6.0f * Dt);
            Assert.AreEqual(3.0f, v.x, 1e-2f);
        }

        /// <summary>Every pre-existing factory leaves the tracking fields false / zero (their behaviour is unchanged); Track sets them and the SPRINTING band ceiling.</summary>
        [Test]
        public void Factories_TrackingFields()
        {
            Vector2 p = new Vector2(3.0f, 4.0f);
            foreach (MovementCommand c in new[]
            {
                MovementCommand.WalkTo(p), MovementCommand.MoveTo(p), MovementCommand.SprintUrgent(p), MovementCommand.Stop(p),
                MovementCommand.PressSprint(p), MovementCommand.SprintWhileWatching(p, p), MovementCommand.StrafeWhileWatching(p, p),
                MovementCommand.ReconstructFromSnapshot(p, AgentMovementState.JOGGING, DecelerationMode.CONTROLLED, FacingMode.AUTO_ALIGN, p, false),
                default(MovementCommand),
            })
            {
                Assert.IsFalse(c.IsTracking);
                Assert.AreEqual(Vector2.zero, c.TargetVelocity);
                Assert.AreEqual(0.0f, c.IssueTimeS);
            }
            MovementCommand t = MovementCommand.Track(p, new Vector2(1.0f, 2.0f), 7.5f);
            Assert.IsTrue(t.IsTracking);
            Assert.AreEqual(new Vector2(1.0f, 2.0f), t.TargetVelocity);
            Assert.AreEqual(7.5f, t.IssueTimeS);
            Assert.AreEqual(AgentMovementState.SPRINTING, t.DesiredState);
            Assert.AreEqual(DecelerationMode.CONTROLLED, t.DecelerationMode);
            Assert.AreEqual(FacingMode.AUTO_ALIGN, t.FacingMode);
            MovementCommand r = MovementCommand.ReconstructFromSnapshot(
                t.TargetPosition, t.DesiredState, t.DecelerationMode, t.FacingMode, t.FacingTarget, t.OverrideSafetyConstraints,
                t.IsTracking, t.TargetVelocity, t.IssueTimeS);
            Assert.AreEqual(t, r, "full-field restore factory round-trips");
        }

        // ── closed loop through AgentMovementSystem.Update ───────────────────────

        [TestCase(AgentMovementState.IDLE, 0.0f)]
        [TestCase(AgentMovementState.WALKING, 0.3f)]
        public void MovingTrack_UsesPhysicalAccelerationOnlyWhenRequested(AgentMovementState state, float speed)
        {
            var system = new AgentMovementSystem();
            var attrs = PlayerAttributes.CreateDefault();
            var perf = PerformanceContext.CreateNeutral();
            var ordinary = AgentState.CreateAtPosition(new Vector2(20.0f, 34.0f), Vector2.right);
            ordinary.CurrentState = state;
            ordinary.Speed = speed;
            ordinary.Velocity = speed * Vector2.right;
            var tight = ordinary;
            var command = MovementCommand.Track(ordinary.Position, Vector2.right, 0.0f);

            system.Update(ref ordinary, in attrs, in perf, in command, Dt, 0.0f, false, 0.0f);
            system.Update(ref tight, in attrs, in perf, in command, Dt, 0.0f, false, 0.0f,
                accelerateMovingTrack: true);

            Assert.AreEqual(speed + LocomotionConstants.WALK_ACCELERATION * Dt, ordinary.Speed, 1e-5f);
            Assert.Greater(tight.Speed, ordinary.Speed);
            Assert.LessOrEqual(tight.Speed, 1.0f);
        }

        private struct Sim
        {
            public AgentState State;
            public int StateChanges;
            public bool EverLocked;
        }

        /// <summary>Runs the real 60 Hz pipeline for <paramref name="frames"/> frames; the command is re-issued every heartbeat from <paramref name="pointAt"/> / <paramref name="velocityAt"/>.</summary>
        private static Sim Run(AgentState start, System.Func<float, Vector2> pointAt, System.Func<float, Vector2> velocityAt, int frames)
        {
            var system = new AgentMovementSystem();
            PlayerAttributes attrs = PlayerAttributes.CreateDefault();
            PerformanceContext perf = PerformanceContext.CreateNeutral();
            Sim sim = new Sim { State = start };
            MovementCommand cmd = default;
            for (int f = 0; f < frames; f++)
            {
                float now = f * Dt;
                if (f % FramesPerHeartbeat == 0)
                    cmd = MovementCommand.Track(pointAt(now), velocityAt(now), now);
                AgentMovementState before = sim.State.CurrentState;
                system.Update(ref sim.State, in attrs, in perf, in cmd, Dt, now, false, 0.0f);
                if (sim.State.CurrentState != before) sim.StateChanges++;
                sim.EverLocked |= sim.State.OscillationGuard.GetState().IsLocked;
            }
            return sim;
        }

        /// <summary>
        /// W13-F7: from rest, 1 / 3 / 8 m from a stationary point, the agent arrives and STAYS — over the last 2 s of a 10 s
        /// run he is within the settle tolerance (+1 cm) of the point at below IdleEnter speed. Never guard-locked.
        /// </summary>
        [TestCase(1.0f)]
        [TestCase(3.0f)]
        [TestCase(8.0f)]
        public void Stationary_ArrivesAndSettles_NoOrbit(float distance)
        {
            Vector2 point = new Vector2(50.0f + distance, 34.0f);
            AgentState s = AgentState.CreateAtPosition(new Vector2(50.0f, 34.0f), Vector2.right);
            var system = new AgentMovementSystem();
            PlayerAttributes attrs = PlayerAttributes.CreateDefault();
            PerformanceContext perf = PerformanceContext.CreateNeutral();
            bool locked = false;
            for (int f = 0; f < 600; f++)
            {
                float now = f * Dt;
                MovementCommand cmd = MovementCommand.Track(point, Vector2.zero, now - (f % FramesPerHeartbeat) * Dt);
                system.Update(ref s, in attrs, in perf, in cmd, Dt, now, false, 0.0f);
                locked |= s.OscillationGuard.GetState().IsLocked;
                if (f >= 480)
                {
                    Assert.LessOrEqual(Vector2.Distance(s.Position, point), LocomotionConstants.MinStoppingDistanceM + 0.01f, $"frame {f}");
                    Assert.Less(s.Speed, MovementThresholds.IdleEnter, $"frame {f}");
                }
            }
            Assert.IsFalse(locked, "OscillationGuard locked");
        }

        /// <summary>
        /// W13-F6: following a point moving at a constant 2.6 / 3.5 / 5.0 m/s (the jog band, where the W13.1c gate flapped), the
        /// agent — started level with it at the same speed — keeps the point within 0.25 m over 6 s, changes locomotion state at
        /// most twice, and never locks the OscillationGuard.
        /// </summary>
        [TestCase(2.6f)]
        [TestCase(3.5f)]
        [TestCase(5.0f)]
        public void ConstantVelocityPoint_FollowedWithoutChurn(float speed)
        {
            Vector2 v = new Vector2(speed, 0.0f);
            Vector2 p0 = new Vector2(20.0f, 34.0f);
            AgentState s = AgentState.CreateAtPosition(p0, Vector2.right);
            s.Velocity = v; s.Speed = speed;
            s.CurrentState = speed > MovementThresholds.JogEnter ? AgentMovementState.JOGGING : AgentMovementState.WALKING;
            Sim sim = Run(s, t => p0 + v * t, _ => v, 360);
            float err = Vector2.Distance(sim.State.Position, p0 + v * (359 * Dt + Dt));
            Assert.Less(err, 0.25f, $"following error {err:F3} m");
            Assert.LessOrEqual(sim.StateChanges, 2, "locomotion state changes");
            Assert.IsFalse(sim.EverLocked, "OscillationGuard locked");
        }

        /// <summary>
        /// ERR-002-001 review amendment (PR #562): a tracking agent that braked in DECELERATING onto a steady point — on it,
        /// at its speed — must leave DECELERATING for the band of that speed: SPRINTING above SprintEnter (draining the
        /// sprint reservoir), JOGGING below it. Before the amendment it stayed DECELERATING for good: the brake floor holds
        /// speed at the tracked speed, the lag stays inside the settle tolerance, so commandSpeed never exceeded
        /// speed + CommandSpeedHysteresis, and the agent took jogging fatigue at sprint speed.
        /// </summary>
        [TestCase(6.5f, AgentMovementState.SPRINTING)]
        [TestCase(4.0f, AgentMovementState.JOGGING)]
        public void DeceleratingOntoASteadyPoint_AtItsSpeed_ReentersTheRunningBand(float pointSpeed, AgentMovementState band)
        {
            Vector2 v = new Vector2(pointSpeed, 0.0f);
            Vector2 p0 = new Vector2(20.0f, 34.0f);
            AgentState s = AgentState.CreateAtPosition(p0, Vector2.right);
            s.Velocity = v; s.Speed = pointSpeed;
            s.CurrentState = AgentMovementState.DECELERATING;

            Sim at1s = Run(s, t => p0 + v * t, _ => v, 60);
            Assert.AreEqual(band, at1s.State.CurrentState, "1 s in");
            Assert.AreEqual(pointSpeed, at1s.State.Speed, 0.05f, "speed stays on the point's");

            Sim at3s = Run(s, t => p0 + v * t, _ => v, 180);
            Assert.AreEqual(band, at3s.State.CurrentState, "3 s in");
            Assert.LessOrEqual(at3s.StateChanges, 1, "one exit, no churn");
            Assert.IsFalse(at3s.EverLocked, "OscillationGuard locked");
            float err = Vector2.Distance(at3s.State.Position, p0 + v * (180 * Dt));
            Assert.Less(err, 0.25f, $"following error {err:F3} m");
            if (band == AgentMovementState.SPRINTING)
                Assert.Less(at3s.State.SprintReservoir, at1s.State.SprintReservoir, "sprint speed drains the sprint reservoir");
        }

        /// <summary>Two runs of the same closed-loop scenario are bit-identical (no hidden state, no RNG).</summary>
        [Test]
        public void ClosedLoop_IsDeterministic()
        {
            Vector2 v = new Vector2(-2.0f, 1.0f);
            Vector2 p0 = new Vector2(60.0f, 30.0f);
            Sim a = Run(AgentState.CreateAtPosition(new Vector2(55.0f, 28.0f), Vector2.right), t => p0 + v * t, _ => v, 300);
            Sim b = Run(AgentState.CreateAtPosition(new Vector2(55.0f, 28.0f), Vector2.right), t => p0 + v * t, _ => v, 300);
            Assert.AreEqual(a.State.Position, b.State.Position);
            Assert.AreEqual(a.State.Velocity, b.State.Velocity);
            Assert.AreEqual(a.StateChanges, b.StateChanges);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-11-21 | —      | W13-F5/F6/F7: tracking-command law, settle, follow, staleness, determinism tests. |
// | 1.1     | 2026-11-23 | —      | ERR-002-002: ordinary and accelerated low-speed tracking use distinct rates. |
// | 1.2     | 2026-11-24 | —      | ERR-002-001 review amendment (PR #562): DeceleratingOntoASteadyPoint_AtItsSpeed_ReentersTheRunningBand (6.5 m/s ⇒ SPRINTING, 4.0 m/s ⇒ JOGGING); fails on the pre-amendment source (stays DECELERATING). |
#endregion
