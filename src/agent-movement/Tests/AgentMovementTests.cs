// File:     src/agent-movement/Tests/AgentMovementTests.cs
// Created:  2026-05-26
// Modified: 2026-06-10 (T-AM-110..115 migrated to the #19 scenario harness)
// Author:   —
// Spec:     Agent Movement #2 test-plan.md (T-AM-001..018, T-AM-030..033, T-AM-040..043),
//           Code Standards #20
// Purpose:  Regression-anchored test roster for Spec #2. Every test ID below maps to a
//           specific adversarial-review (AR) finding documented in src/CLAUDE.md AR-3..AR-9.
//           Test IDs and anchors are catalogued in docs/specs/agent-movement/test-plan.md.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.AgentMovement.Tests
{
    // ── T-AM-001..006: AgentStateMachine.CalculateGroundedDwell unit tests ──

    /// <summary>
    /// Unit tests for the §3.1.5 dwell formula. Pure-function tests; no AgentState required.
    /// </summary>
    [TestFixture]
    internal sealed class AgentStateMachineDwellTests
    {
        private const float Tolerance = 0.001f;

        // T-AM-001 (AR-9 M-1 numeric anchor): default attrs + max force → 2.0 s
        [Test]
        public void CalculateGroundedDwell_DefaultAttrs_MaxForce_Returns2Seconds()
        {
            float dwell = AgentStateMachine.CalculateGroundedDwell(
                balance: 10, strength: 10,
                reason: GroundedReason.COLLISION, collisionForce: 1.0f);

            // combinedFactor = max(20/40, 0.05) = 0.5
            // dwell = (1.0 × 1.0) / 0.5 = 2.0; forceScale = 0.65 + 0.35 × 1.0 = 1.0; final = 2.0
            Assert.AreEqual(2.0f, dwell, Tolerance,
                "Default attrs (10/10) + max force should produce GroundedMinDwellBase / 0.5 = 2.0 s.");
        }

        // T-AM-002 (AR-9 M-1 numeric anchor): default attrs + zero force → 1.3 s
        [Test]
        public void CalculateGroundedDwell_DefaultAttrs_ZeroForce_ReturnsBaseTimesCollisionDwellMin()
        {
            float dwell = AgentStateMachine.CalculateGroundedDwell(
                balance: 10, strength: 10,
                reason: GroundedReason.COLLISION, collisionForce: 0.0f);

            // forceScale = 0.65 + 0.35 × 0 = 0.65; final = 2.0 × 0.65 = 1.3
            Assert.AreEqual(1.3f, dwell, Tolerance,
                "Force=0 should reduce dwell to base × CollisionDwellMin = 1.3 s "
                + "(the AR-9 M-1 regression value when uncached force=0 is forwarded).");
        }

        // T-AM-003 (AR-2 H-1): reason multiplier reaches the formula
        [Test]
        public void CalculateGroundedDwell_SlidingTackle_AppliesReasonMultiplier()
        {
            float dwell = AgentStateMachine.CalculateGroundedDwell(
                balance: 10, strength: 10,
                reason: GroundedReason.SLIDING_TACKLE, collisionForce: 1.0f);

            // reasonMult = 0.6; dwell = (1.0 × 0.6) / 0.5 = 1.2; SLIDING_TACKLE skips forceScale
            Assert.AreEqual(1.2f, dwell, Tolerance,
                "SLIDING_TACKLE should produce base × 0.6 / combinedFactor = 1.2 s.");
        }

        // T-AM-004 (AR-2 L-2): min attrs → upper-clamp engages
        [Test]
        public void CalculateGroundedDwell_MinAttrs_ClampedAtMax()
        {
            float dwell = AgentStateMachine.CalculateGroundedDwell(
                balance: 1, strength: 1,
                reason: GroundedReason.COLLISION, collisionForce: 1.0f);

            // combinedFactor = max(2/40, 0.05) = 0.05; raw dwell = 1.0/0.05 = 20.0; clamp → 2.5
            Assert.AreEqual(MovementThresholds.GroundedDwellClampMax, dwell, Tolerance,
                "Min attrs should saturate at GroundedDwellClampMax (2.5 s).");
        }

        // T-AM-005: max attrs produce a finite, meaningful dwell (no float blowup, no immediate release)
        [Test]
        public void CalculateGroundedDwell_MaxAttrs_ProducesFiniteDwellAtLeastMinClamp()
        {
            float dwell = AgentStateMachine.CalculateGroundedDwell(
                balance: 20, strength: 20,
                reason: GroundedReason.COLLISION, collisionForce: 1.0f);

            // combinedFactor = 1.0; dwell = 1.0; > GroundedDwellClampMin (0.5)
            Assert.That(dwell, Is.GreaterThanOrEqualTo(MovementThresholds.GroundedDwellClampMin),
                "Even elite players must dwell at least GroundedDwellClampMin (0.5 s) on max-force hits.");
            Assert.That(dwell, Is.LessThanOrEqualTo(MovementThresholds.GroundedDwellClampMax),
                "Dwell must not exceed the upper clamp.");
        }

        // T-AM-006 (AR-8 L-2 defence-in-depth): force > 1.0 clamps inside the formula too
        [Test]
        public void CalculateGroundedDwell_ForceAbove1_Clamps01InFormula()
        {
            float dwellAt1 = AgentStateMachine.CalculateGroundedDwell(
                balance: 10, strength: 10,
                reason: GroundedReason.COLLISION, collisionForce: 1.0f);
            float dwellAt2 = AgentStateMachine.CalculateGroundedDwell(
                balance: 10, strength: 10,
                reason: GroundedReason.COLLISION, collisionForce: 2.0f);

            Assert.AreEqual(dwellAt1, dwellAt2, Tolerance,
                "force > 1.0 must Clamp01 inside the formula (forceScale saturates at 1.0).");
        }
    }

    // ── T-AM-010..018: AgentMovementSystem.Update Step 3 collision integration ──

    /// <summary>
    /// Integration tests for the System.Update Step 3 collision/transition pipeline. Each test
    /// runs the real 12-step pipeline against a real AgentState; assertions inspect resulting
    /// fields rather than mocking internal calls.
    /// </summary>
    [TestFixture]
    internal sealed class AgentMovementSystemCollisionTests
    {
        private const float PhysicsHz = 60.0f;
        private const float Dt = 1.0f / 60.0f;
        private static readonly Vector2 PitchCentre = new Vector2(52.5f, 34.0f);

        private static AgentMovementSystem MakeSystem() => new AgentMovementSystem(PhysicsHz);

        private static AgentState MakeIdleAgent()
        {
            return AgentState.CreateAtPosition(PitchCentre, Vector2.up);
        }

        private static (PlayerAttributes attrs, PerformanceContext perf) DefaultAttrs()
        {
            return (PlayerAttributes.CreateDefault(), PerformanceContext.CreateNeutral());
        }

        // T-AM-010: entry frame writes the cache
        [Test]
        public void Update_KnockdownEntryFrame_CachesReasonAndForce()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);

            sys.Update(ref state, attrs, perf, cmd, Dt, currentTime: 0.0f,
                isCollisionKnockdown: true, collisionForce: 1.0f);

            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState);
            Assert.AreEqual(GroundedReason.COLLISION, state.GroundedReason);
            Assert.AreEqual(1.0f, state.CollisionForce, 0.0001f);
            Assert.AreEqual(0.0f, state.TimeInState, 0.0001f);
        }

        // T-AM-011 (PRIMARY AR-9 M-1 lock): GROUNDED holds for the cached-force dwell, not the
        // incoming-force dwell. At 1.5 s (90 frames) the AR-9 M-1 fix should still hold dwell;
        // a regression collapses dwell to ~1.3 s and the agent releases by frame 78.
        [Test]
        public void Update_GroundedDwellUsesCachedEntryForce_NotIncomingZeroForce()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);
            float t = 0.0f;

            // Frame 0: deliver max-force collision impulse.
            sys.Update(ref state, attrs, perf, cmd, Dt, t,
                isCollisionKnockdown: true, collisionForce: 1.0f);
            t += Dt;

            // Frames 1..89 (1.483 s of post-entry dwell). Spec #3 collisions are one-frame
            // impulses, so isCollisionKnockdown is false and incoming collisionForce is 0.
            for (int i = 1; i < 90; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t,
                    isCollisionKnockdown: false, collisionForce: 0.0f);
                t += Dt;
            }

            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "At t=1.5 s, dwell should still hold (cached force=1.0 → dwell=2.0 s). "
                + "If this fails the agent released at ~1.3 s — AR-9 M-1 regression.");
            Assert.AreEqual(GroundedReason.COLLISION, state.GroundedReason,
                "Reason must remain COLLISION throughout the dwell.");
        }

        // T-AM-012: upper anchor — release happens by ~2.2 s
        [Test]
        public void Update_GroundedDwell_ReleasesAfterFormulaDwellElapses()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);
            float t = 0.0f;

            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 1.0f);
            t += Dt;

            // Run 132 frames (~2.2 s) — past the 2.0 s dwell.
            for (int i = 1; i < 132; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t, false, 0.0f);
                t += Dt;
            }

            Assert.AreNotEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "After ~2.2 s the dwell must have expired and the agent must have released.");
        }

        // T-AM-013 (AR-5 M-2): second-hit refresh while still GROUNDED
        [Test]
        public void Update_SecondKnockdownWhileGrounded_RefreshesForceAndResetsDwellTimer()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);
            float t = 0.0f;

            // Initial hit at moderate force.
            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 0.5f);
            t += Dt;

            // Advance 20 frames inside the dwell.
            for (int i = 1; i <= 20; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t, false, 0.0f);
                t += Dt;
            }
            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState);
            Assert.AreEqual(0.5f, state.CollisionForce, 0.0001f, "First-hit force should be cached.");
            float timeBeforeSecondHit = state.TimeInState;
            Assert.That(timeBeforeSecondHit, Is.GreaterThan(0.1f),
                "Sanity check: dwell timer should have advanced before second hit.");

            // Second hit at higher force.
            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 1.0f);

            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState);
            Assert.AreEqual(GroundedReason.COLLISION, state.GroundedReason);
            Assert.AreEqual(1.0f, state.CollisionForce, 0.0001f,
                "Second-hit force must refresh the cache (AR-5 M-2).");
            Assert.AreEqual(0.0f, state.TimeInState, 0.0001f,
                "Dwell timer must reset on second hit so the new impulse extends recovery.");
        }

        // T-AM-014 (AR-8 L-2): cache write clamps force at 1.0
        [Test]
        public void Update_KnockdownForceAbove1_ClampedToOneOnCacheWrite()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);

            sys.Update(ref state, attrs, perf, cmd, Dt, 0.0f, true, collisionForce: 2.0f);

            Assert.AreEqual(1.0f, state.CollisionForce, 0.0001f,
                "Cache write must Clamp01 (AR-8 L-2 contract).");
        }

        // T-AM-015 (AR-6 M-1): no IDLE flicker when a fresh collision arrives at the dwell-expiry
        // boundary. The knockdown short-circuit must be unconditional, refreshing the GROUNDED
        // state instead of letting EvaluateFromGrounded → IDLE leak through.
        [Test]
        public void Update_KnockdownAtDwellExpiry_RefreshesGrounded_DoesNotFlickerIdle()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);
            float t = 0.0f;

            // Entry frame at force=1.0 → dwell 2.0 s (= 120 dt-increments at 60 Hz).
            // Frame timeline (post-entry):
            //   - entry frame leaves state.TimeInState = 0.
            //   - after the i-th post-entry frame, state.TimeInState = i × dt.
            //   - Frame 121 (i = 121) is the first frame that enters with TimeInState = 2.0,
            //     where EvaluateFromGrounded(2.0, …) returns IDLE — the dwell-expiry frame.
            //   - We land a fresh knockdown on exactly that frame. With AR-6 M-1 fixed, the
            //     unconditional knockdown short-circuit returns GROUNDED and the inner-else
            //     refresh branch fires. With AR-6 regressed (current != GROUNDED guard
            //     reinstated), EvaluateFromGrounded → IDLE leaks through and the outer
            //     transition produces an IDLE flicker mid-knockdown.
            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 1.0f);
            t += Dt;

            for (int i = 1; i <= 124; i++)
            {
                bool knockdownThisFrame = (i == 121);
                float force = knockdownThisFrame ? 1.0f : 0.0f;
                sys.Update(ref state, attrs, perf, cmd, Dt, t, knockdownThisFrame, force);
                t += Dt;
            }

            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "Fresh knockdown at the dwell-expiry boundary must keep state GROUNDED. "
                + "An IDLE here means AR-6 M-1 has regressed (current!=GROUNDED guard reintroduced).");
            Assert.AreEqual(GroundedReason.COLLISION, state.GroundedReason);
            Assert.AreEqual(1.0f, state.CollisionForce, 0.0001f);
        }

        // T-AM-016 (AR-6 M-2): OscillationGuard bypassed for collision-driven GROUNDED transitions
        [Test]
        public void Update_CollisionBypassesLockedOscillationGuard()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);

            // Force the guard into its locked state via 7 transitions within the rolling window.
            for (int i = 0; i < 7; i++)
            {
                state.OscillationGuard.RecordAndCheck(i * 0.1f);
            }
            // Sanity: guard should now reject a fresh transition.
            Assert.IsTrue(state.OscillationGuard.RecordAndCheck(0.7f),
                "Guard setup precondition: should be locked after 7 fast transitions.");

            // Deliver a knockdown while the guard is locked. The collision bypass must let the
            // GROUNDED transition through regardless of the lock state.
            sys.Update(ref state, attrs, perf, cmd, Dt, currentTime: 0.8f,
                isCollisionKnockdown: true, collisionForce: 1.0f);

            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "Knockdown must reach GROUNDED even when OscillationGuard is locked (AR-6 M-2).");
        }

        // T-AM-017 (AR-7 M-1): post-recovery GROUNDED→IDLE transition is not blocked by a stale lock
        [Test]
        public void Update_PostCollisionRecovery_NotBlockedByPriorLock()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);

            // Lock the guard, deliver knockdown.
            for (int i = 0; i < 7; i++)
            {
                state.OscillationGuard.RecordAndCheck(i * 0.1f);
            }
            float t = 0.8f;
            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 1.0f);
            t += Dt;
            Assert.AreEqual(AgentMovementState.GROUNDED, state.CurrentState);

            // Run past the dwell. AR-7 M-1 wipes the guard on collision entry, so the
            // GROUNDED→IDLE recovery transition must fire on the expiry frame.
            for (int i = 1; i < 135; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t, false, 0.0f);
                t += Dt;
            }

            Assert.AreNotEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "After dwell expires, recovery transition must not be blocked by the pre-collision lock.");
        }

        // T-AM-018 (AR-3 R3-M-1): exiting GROUNDED clears reason and force fields
        [Test]
        public void Update_ExitingGrounded_ClearsReasonAndForce()
        {
            var sys = MakeSystem();
            AgentState state = MakeIdleAgent();
            var (attrs, perf) = DefaultAttrs();
            MovementCommand cmd = MovementCommand.Stop(state.Position);
            float t = 0.0f;

            sys.Update(ref state, attrs, perf, cmd, Dt, t, true, 1.0f);
            t += Dt;

            for (int i = 1; i < 135; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t, false, 0.0f);
                t += Dt;
            }

            Assert.AreNotEqual(AgentMovementState.GROUNDED, state.CurrentState,
                "Precondition: dwell should have expired.");
            Assert.AreEqual(GroundedReason.NONE, state.GroundedReason,
                "GroundedReason invariant: must be NONE when CurrentState != GROUNDED.");
            Assert.AreEqual(0.0f, state.CollisionForce, 0.0001f,
                "CollisionForce must reset to 0 on exit from GROUNDED.");
        }
    }

    // ── T-AM-030..033: Safety override Step 11 cache preservation ──

    /// <summary>
    /// Tests the override-path branch in System.Update Step 11. The override branch is
    /// tooling-only (replay scrubber / editor injection); tests use the internal
    /// ToolingOverrideOnly_NaNInjection factory granted via InternalsVisibleTo.
    /// </summary>
    [TestFixture]
    internal sealed class AgentMovementSystemSafetyOverrideTests
    {
        private const float Dt = 1.0f / 60.0f;
        private static readonly Vector2 PitchCentre = new Vector2(52.5f, 34.0f);

        // T-AM-030 (AR-5 M-1 PRIMARY): NaN injection must NOT poison LastValid*
        [Test]
        public void Update_OverridePath_NaNInjection_PreservesLastValidCache()
        {
            var sys = new AgentMovementSystem(60.0f);
            AgentState state = AgentState.CreateAtPosition(PitchCentre, Vector2.up);
            state.LastValidPosition = new Vector2(10.0f, 10.0f);
            state.LastValidVelocity = new Vector2(1.0f, 0.0f);
            state.LastValidFacing = Vector2.right;

            // Pre-corrupt position so step 11 sees NaN.
            state.Position = new Vector2(float.NaN, 0.0f);

            var attrs = PlayerAttributes.CreateDefault();
            var perf = PerformanceContext.CreateNeutral();
            MovementCommand cmd = MovementCommand.ToolingOverrideOnly_NaNInjection(PitchCentre);

            sys.Update(ref state, attrs, perf, cmd, Dt, 0.0f, false, 0.0f);

            Assert.AreEqual(new Vector2(10.0f, 10.0f), state.LastValidPosition,
                "LastValidPosition must not be overwritten with NaN-injected post-integration state.");
            Assert.AreEqual(new Vector2(1.0f, 0.0f), state.LastValidVelocity,
                "LastValidVelocity must be preserved (AR-5 M-1).");
            Assert.AreEqual(Vector2.right, state.LastValidFacing,
                "LastValidFacing must be preserved.");
        }

        // T-AM-031 (AR-7 M-2): NaN injection must NOT poison state.Speed.
        // Setup note: state.Velocity is set non-zero so Step 8's RotateVelocityToward has a
        // valid currentDirection — otherwise both-degenerate (zero current, zero target) hits an
        // intentional Debug.Assert(false, …) at AgentDirectionalMovement.cs:173 that would noise
        // up the test runner. NaN reaches Step 11 via Step 9 (`Position += Velocity * dt` with
        // pre-corrupted state.Position).
        [Test]
        public void Update_OverridePath_NaNInjection_PreservesSpeedField()
        {
            var sys = new AgentMovementSystem(60.0f);
            AgentState state = AgentState.CreateAtPosition(PitchCentre, Vector2.up);
            state.LastValidPosition = new Vector2(10.0f, 10.0f);
            state.LastValidVelocity = new Vector2(7.0f, 7.0f);
            state.LastValidFacing = Vector2.right;
            state.Speed = 5.0f;                            // Distinct sentinel.
            state.Velocity = new Vector2(1.0f, 0.0f);      // Non-zero to dodge the Step 8 assert.
            state.Position = new Vector2(float.NaN, 0.0f); // NaN injection — flows to Step 11.

            var attrs = PlayerAttributes.CreateDefault();
            var perf = PerformanceContext.CreateNeutral();
            MovementCommand cmd = MovementCommand.ToolingOverrideOnly_NaNInjection(PitchCentre);

            sys.Update(ref state, attrs, perf, cmd, Dt, 0.0f, false, 0.0f);

            Assert.AreEqual(5.0f, state.Speed, 0.0001f,
                "state.Speed must be preserved when override-path integration produces invalid values. "
                + "AR-7 M-2 moved the Speed assignment inside the HasInvalidValues gate; "
                + "a regressed code path would set Speed = state.Velocity.magnitude (≠ 5.0).");
            Assert.That(state.Speed, Is.Not.NaN,
                "Speed must never be NaN — would silently flip next-frame EvaluateState branches.");
        }

        // T-AM-032 (AR-5 M-1 sibling): override happy path still refreshes LastValid*
        [Test]
        public void Update_OverridePath_ValidValues_RefreshesLastValidCache()
        {
            var sys = new AgentMovementSystem(60.0f);
            AgentState state = AgentState.CreateAtPosition(PitchCentre, Vector2.up);
            // Stale cache values that should be overwritten when post-integration state is valid.
            state.LastValidPosition = new Vector2(99.0f, 99.0f);
            state.LastValidVelocity = new Vector2(99.0f, 99.0f);
            state.LastValidFacing = new Vector2(0.0f, -1.0f);

            var attrs = PlayerAttributes.CreateDefault();
            var perf = PerformanceContext.CreateNeutral();
            MovementCommand cmd = MovementCommand.ToolingOverrideOnly_NaNInjection(PitchCentre);

            sys.Update(ref state, attrs, perf, cmd, Dt, 0.0f, false, 0.0f);

            // After Step 11 the cache should match the (valid) post-integration state.
            Assert.AreEqual(state.Position, state.LastValidPosition,
                "Override-path cache refresh must fire when values are finite (no false skip).");
            Assert.AreEqual(state.Velocity, state.LastValidVelocity);
            Assert.AreEqual(state.FacingDirection, state.LastValidFacing);
        }

        // T-AM-033 (AR-4 M-5 sibling): non-override path also preserves cache on recovery
        [Test]
        public void Update_NonOverridePath_NaNInPosition_RecoversAndDoesNotOverwriteCache()
        {
            var sys = new AgentMovementSystem(60.0f);
            AgentState state = AgentState.CreateAtPosition(PitchCentre, Vector2.up);
            var anchor = new Vector2(20.0f, 20.0f);
            state.LastValidPosition = anchor;
            state.LastValidVelocity = Vector2.zero;
            state.LastValidFacing = Vector2.up;

            // Pre-corrupt position so Step 10's Validate fires a recovery snap.
            state.Position = new Vector2(float.NaN, float.NaN);

            var attrs = PlayerAttributes.CreateDefault();
            var perf = PerformanceContext.CreateNeutral();
            MovementCommand cmd = MovementCommand.Stop(anchor);

            sys.Update(ref state, attrs, perf, cmd, Dt, 0.0f, false, 0.0f);

            Assert.AreEqual(anchor, state.Position,
                "Step 10 Validate must snap NaN position back to LastValidPosition.");
            Assert.AreEqual(anchor, state.LastValidPosition,
                "On recovery (wasRecovered=true), Step 11 must NOT overwrite the cache with the "
                + "post-recovery values — otherwise repeated NaN frames would keep snapping to "
                + "an updated cache instead of the original anchor.");
        }
    }

    // ── T-AM-040..043: OscillationGuard unit tests ──

    /// <summary>
    /// Unit tests for the §3.1.7 anti-flap guard. Pure struct under test; no AgentState required.
    /// </summary>
    [TestFixture]
    internal sealed class OscillationGuardTests
    {
        // T-AM-040: fresh Initialize + first transition is never blocked
        [Test]
        public void RecordAndCheck_FreshGuard_FirstTransitionNotBlocked()
        {
            var guard = new OscillationGuard();
            guard.Initialize();

            bool blocked = guard.RecordAndCheck(currentTime: 0.0f);

            Assert.IsFalse(blocked,
                "A fresh guard at t=0 must not block — Initialize seeds all slots to NegativeInfinity "
                + "to prevent false-positive lockout at match start.");
        }

        // T-AM-041: lock fires after MaxTransitionsPerSecond+1 transitions in the rolling window
        [Test]
        public void RecordAndCheck_SevenTransitionsInRollingWindow_LocksOnSeventh()
        {
            var guard = new OscillationGuard();
            guard.Initialize();

            // First 6 transitions (count <= MaxTransitionsPerSecond) — none should block.
            for (int i = 0; i < 6; i++)
            {
                bool b = guard.RecordAndCheck(i * 0.1f);
                Assert.IsFalse(b, $"Call {i + 1} at t={i * 0.1f:F1}s should not be blocked.");
            }

            // The 7th transition pushes recentCount over MaxTransitionsPerSecond (6) → lock.
            bool blockedOnSeventh = guard.RecordAndCheck(0.6f);
            Assert.IsTrue(blockedOnSeventh, "7th transition within the 1s window must trigger lock.");
        }

        // T-AM-042: transitions within the lock window are blocked
        [Test]
        public void RecordAndCheck_WithinLockWindow_RemainsBlocked()
        {
            var guard = new OscillationGuard();
            guard.Initialize();
            for (int i = 0; i < 7; i++)
            {
                guard.RecordAndCheck(i * 0.1f);
            }
            // Guard locked at t=0.6 for LockDuration=0.5 → locked until t=1.1.

            bool stillBlocked = guard.RecordAndCheck(0.9f);

            Assert.IsTrue(stillBlocked,
                "A transition at t=0.9s (inside the [0.6, 1.1) lock window) must be blocked.");
        }

        // T-AM-043 (AR-4 M-2): after lock expires AND ring buffer was reset on lock entry,
        // the next transition is NOT blocked (no indefinite re-lock)
        [Test]
        public void RecordAndCheck_AfterLockExpires_DoesNotImmediatelyReLock()
        {
            var guard = new OscillationGuard();
            guard.Initialize();
            for (int i = 0; i < 7; i++)
            {
                guard.RecordAndCheck(i * 0.1f);
            }
            // Locked until t = 0.6 + LockDuration = 1.1.

            // First transition after lock expiry. Pre-AR-4 the ring buffer retained the 7
            // pre-lock timestamps; subtracting from t=1.2 gave deltas of 0.6..1.2, of which
            // 6 are still under WindowSeconds=1.0, plus the new write → 7 recent → re-lock.
            // Post-AR-4 the buffer was reset on lock entry, so this transition succeeds.
            bool blockedAfterExpiry = guard.RecordAndCheck(1.2f);

            Assert.IsFalse(blockedAfterExpiry,
                "After LockDuration elapses, the next transition must succeed. "
                + "AR-4 M-2 reset the ring buffer on lock entry to close the indefinite-re-lock loop.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-26 | —      | Initial placeholder (cited non-existent §5.1 / "85 tests" — see test-plan.md §1).   |
// | 2.0     | 2026-06-04 | —      | Replace placeholder with the regression-anchored T-AM-001..018, T-AM-030..033,    |
// |         |            |        | T-AM-040..043 roster. Test plan and ID convention live in                          |
// |         |            |        | docs/specs/agent-movement/test-plan.md v0.1.                                       |
// | 2.1     | 2026-06-09 | —      | AR-12 fix pass: new AgentMovementSystemClosedLoopTests fixture (T-AM-110..113) —   |
// |         |            |        | launch-from-rest (H-1), jog-command band respect (H-2), bounded stop (H-3),        |
// |         |            |        | walk-band stability (H-2/WalkTo). First whole-seconds closed-loop coverage; every  |
// |         |            |        | prior test exercised a pure function or injected mid-flight state.                 |
// | 2.2     | 2026-06-09 | —      | AR-13 fix pass: T-AM-114 added — exhausted-agent command degradation (M-1);        |
// |         |            |        | T-AM-115 added — HOLD-at-own-position stays at rest (M-2 movement-intent gate).    |
// | 2.3     | 2026-06-10 | —      | AgentMovementSystemClosedLoopTests (T-AM-110..115) migrated to the Spec #19        |
// |         |            |        | ScenarioRunner harness as its first per-spec closed-loop fixture corpus — see      |
// |         |            |        | AgentMovementScenarios.cs (bodies + manifests) and AgentMovementScenarioTests.cs   |
// |         |            |        | (sim_-layer executable tests). Assertions unchanged in substance; requirement IDs  |
// |         |            |        | retained. This file keeps the unit/regression roster only.                        |
#endregion
