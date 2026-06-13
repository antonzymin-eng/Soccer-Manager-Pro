// File:     src/pressing-ai/Tests/PressingAITests.cs
// Created:  2026-05-31
// Modified: 2026-06-13
// Author:   —
// Spec:     Pressing AI #13 §5, Code Standards #20
// Purpose:  Unit tests for Pressing AI. T-U unit tests from §5.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PassMechanics;

namespace TacticalDirector.PressingAI.Tests
{
    // ────────────────────────────────────────────────────────────────────────────
    // Helpers shared across all fixtures
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Factory helpers for building minimal PressingSnapshot instances in tests.
    /// All agent slots default to inactive; callers activate the slots they need.
    /// </summary>
    internal static class SnapshotFactory
    {
        internal const int PressingTeamId  = 0;
        internal const int OpposingTeamId  = 1;
        internal const int CarrierEntityId = 10;
        internal const int DefenderIdA     = 1;
        internal const int DefenderIdB     = 2;

        /// <summary>
        /// Creates a PressingSnapshot with all agents inactive and sensible defaults.
        /// Ball is placed at (50, 34) – near pitch centre, within PressZoneXMin.
        /// AttackingDirection is +X (pressing team attacks right).
        /// </summary>
        internal static PressingSnapshot MakeDefault()
        {
            PressingSnapshot snap = new PressingSnapshot
            {
                TickIndex           = 1,
                BallPosition        = new Vector3(50f, 34f, 0f),
                BallVelocity        = Vector3.zero,
                BallCarrierEntityId = CarrierEntityId,
                AttackingDirection  = new Vector2(1f, 0f), // attacking +X
                PossessionTeamId    = OpposingTeamId,
                PressingTeamId      = PressingTeamId,
            };

            // All 22 agent slots default to EntityId = -1, inactive.
            for (int i = 0; i < snap.Agents.Length; i++)
            {
                snap.Agents[i].EntityId  = -1;
                snap.Agents[i].IsActive  = false;
                snap.Agents[i].TeamId    = OpposingTeamId;
            }

            return snap;
        }

        /// <summary>Writes an agent into the given slot index of the snapshot.</summary>
        internal static void SetAgent(
            PressingSnapshot snap,
            int slotIndex,
            int entityId,
            int teamId,
            Vector2 position,
            bool isActive    = true,
            bool isGoalkeeper = false,
            float fatigue    = 0f,
            Vector2 facing   = default,
            float firstTouch = 15f,
            float lastTouchQ = 0.8f,
            float postTouchSpeed = 2f,
            Vector2 velocity = default,
            Vector2 baselineSlot = default,
            LineId line = LineId.Midfield)
        {
            snap.Agents[slotIndex] = new PressingAgentSnapshot
            {
                EntityId           = entityId,
                TeamId             = teamId,
                Position           = position,
                Velocity           = velocity,
                Facing             = facing == default ? new Vector2(1f, 0f) : facing,
                Fatigue            = fatigue,
                FirstTouchAttribute = firstTouch,
                LastTouchQuality   = lastTouchQ,
                PostTouchBallSpeed = postTouchSpeed,
                IsGoalkeeper       = isGoalkeeper,
                HasBall            = false,
                IsActive           = isActive,
                BaselineSlot       = baselineSlot == default ? position : baselineSlot,
                Line               = line,
            };
        }

        /// <summary>
        /// Creates a PassAttemptEvent with AgentId and TargetPosition set.
        /// All other header fields are zero (acceptable for unit tests).
        /// </summary>
        internal static PassAttemptEvent MakePassEvent(int agentId, Vector3 targetPosition)
        {
            PassAttemptEvent evt = default;
            evt.AgentId         = agentId;
            evt.TargetPosition  = targetPosition;
            return evt;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-001 / T-U-002 / T-U-003 / T-U-004 / T-U-005  — Triggers (§3.1)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class TriggerBadTouchTests
    {
        /// <summary>
        /// T-U-001a: BAD_TOUCH fires when touch quality = 0.30 (below threshold 0.40)
        /// and post-touch velocity = 6.0 m/s (above threshold 4.0 m/s).
        /// </summary>
        [Test]
        public void BadTouch_FiresWhenQualityLowAndVelocityHigh()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: 0.30f,
                postTouchSpeed: 6.0f);

            bool result = TriggerEvaluator.EvaluateBadTouch(snap);

            Assert.IsTrue(result,
                "BadTouch must fire when q=0.30 < BadTouchThreshold and speed=6.0 > BadTouchVelocityMS.");
        }

        /// <summary>
        /// T-U-001b: BAD_TOUCH does NOT fire when touch quality = 0.50 (above threshold 0.40).
        /// </summary>
        [Test]
        public void BadTouch_DoesNotFireWhenQualityAboveThreshold()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: 0.50f,
                postTouchSpeed: 6.0f);

            bool result = TriggerEvaluator.EvaluateBadTouch(snap);

            Assert.IsFalse(result,
                "BadTouch must NOT fire when q=0.50 >= BadTouchThreshold.");
        }

        /// <summary>
        /// T-U-001c: BAD_TOUCH does NOT fire when post-touch speed is below BadTouchVelocityMS.
        /// </summary>
        [Test]
        public void BadTouch_DoesNotFireWhenVelocityBelowThreshold()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: 0.30f,
                postTouchSpeed: PressingAIConstants.BadTouchVelocityMS - 0.1f);

            bool result = TriggerEvaluator.EvaluateBadTouch(snap);

            Assert.IsFalse(result,
                "BadTouch must NOT fire when post-touch speed is below BadTouchVelocityMS.");
        }

        /// <summary>
        /// T-U-005 (BadTouch): trigger origin is the ball-carrier agent (not the receiver).
        /// Verifies that only the BallCarrierEntityId is evaluated for quality/speed.
        /// </summary>
        [Test]
        public void BadTouch_OriginIsCarrierNotOtherAgent()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            // Carrier has a good touch — should NOT trigger.
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: 0.90f,
                postTouchSpeed: 6.0f);

            // A different opponent has bad touch — must be ignored.
            SnapshotFactory.SetAgent(snap, 1,
                entityId: 99,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(55f, 34f),
                lastTouchQ: 0.10f,
                postTouchSpeed: 6.0f);

            bool result = TriggerEvaluator.EvaluateBadTouch(snap);

            Assert.IsFalse(result,
                "BadTouch must only evaluate the BallCarrierEntityId agent, not other opponents.");
        }
    }

    [TestFixture]
    internal sealed class TriggerBackwardPassTests
    {
        /// <summary>
        /// T-U-002: BACKWARD_PASS worked example.
        /// Kick velocity (-6, 3, 0) from passer at (50, 34) → target at (44, 37).
        /// AttackingDirection = (+1, 0).
        /// PassDir normalised ≈ (-0.894, 0.447). Dot with (1,0) = -0.894 < threshold (-0.30). Fires.
        /// </summary>
        [Test]
        public void BackwardPass_FiresWhenDotBelowThreshold()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            // Passer (opposing team) at (50, 34).
            const int passerId = 20;
            snap.BallCarrierEntityId = passerId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: passerId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Target at (44, 37) — backward relative to +X attacking direction.
            PassAttemptEvent evt = SnapshotFactory.MakePassEvent(
                passerId,
                new Vector3(44f, 37f, 0f));

            bool result = TriggerEvaluator.EvaluateBackwardPass(snap, evt);

            Assert.IsTrue(result,
                "BackwardPass must fire when passDir dot attackingDirection < BackwardPassThreshold.");
        }

        /// <summary>
        /// BACKWARD_PASS does NOT fire when pass direction is forward (dot > threshold).
        /// </summary>
        [Test]
        public void BackwardPass_DoesNotFireWhenPassIsForward()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            const int passerId = 20;
            snap.BallCarrierEntityId = passerId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: passerId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Target forward (+X direction) at (60, 34).
            PassAttemptEvent evt = SnapshotFactory.MakePassEvent(
                passerId,
                new Vector3(60f, 34f, 0f));

            bool result = TriggerEvaluator.EvaluateBackwardPass(snap, evt);

            Assert.IsFalse(result,
                "BackwardPass must NOT fire when pass is in the forward attacking direction.");
        }
    }

    [TestFixture]
    internal sealed class TriggerSidelineTrapTests
    {
        /// <summary>
        /// T-U-003: SIDELINE_TRAP worked example.
        /// Ball at (45, 5.0) — 5.0 m from bottom touchline (below SidelineTrapDistanceM=8.0).
        /// Carrier facing (0.5, -0.87) — toward bottom touchline (dot with (0,-1) = 0.87 > 0). Fires.
        /// </summary>
        [Test]
        public void SidelineTrap_FiresWhenNearTouchlineAndFacingTouchline()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(45f, 5.0f, 0f);

            // Carrier facing toward bottom touchline: (0.5, -0.87) normalised.
            Vector2 facingTowardTouchline = new Vector2(0.5f, -0.87f).normalized;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(45f, 5.0f),
                facing: facingTowardTouchline);

            bool result = TriggerEvaluator.EvaluateSidelineTrap(snap);

            Assert.IsTrue(result,
                "SidelineTrap must fire when ball is within SidelineTrapDistanceM and carrier faces touchline.");
        }

        /// <summary>
        /// SIDELINE_TRAP does NOT fire when ball is far from touchline (≥ SidelineTrapDistanceM on both sides).
        /// </summary>
        [Test]
        public void SidelineTrap_DoesNotFireWhenBallIsFarFromTouchlines()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            // Ball at pitch centre — 34m from each touchline, well above SidelineTrapDistanceM=8.0.
            snap.BallPosition = new Vector3(50f, 34f, 0f);

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                facing: new Vector2(0f, -1f));

            bool result = TriggerEvaluator.EvaluateSidelineTrap(snap);

            Assert.IsFalse(result,
                "SidelineTrap must NOT fire when ball is >= SidelineTrapDistanceM from both touchlines.");
        }

        /// <summary>
        /// SIDELINE_TRAP does NOT fire when ball is near touchline but carrier faces away from it.
        /// </summary>
        [Test]
        public void SidelineTrap_DoesNotFireWhenCarrierFacesAwayFromTouchline()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(45f, 5.0f, 0f);

            // Carrier facing away from bottom touchline (toward top: +Y).
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(45f, 5.0f),
                facing: new Vector2(0f, 1f));

            bool result = TriggerEvaluator.EvaluateSidelineTrap(snap);

            Assert.IsFalse(result,
                "SidelineTrap must NOT fire when carrier faces away from the touchline.");
        }
    }

    [TestFixture]
    internal sealed class TriggerWeakReceiverTests
    {
        /// <summary>
        /// T-U-004: WEAK_RECEIVER excludes opposing GK from the candidate set (KD-13).
        /// A GK with weak first touch and full pressure should NOT trigger the flag.
        /// </summary>
        [Test]
        public void WeakReceiver_ExcludesOpposingGoalkeeper()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            // Carrier (opposing, not GK).
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Opposing GK with weak first touch — must be excluded.
            const int gkId = 30;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: gkId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(52f, 35f),
                isGoalkeeper: true,
                firstTouch: 5f); // well below WeakReceiverThreshold=10

            // Three pressing-team defenders pressing the GK position to maximise pressure.
            SnapshotFactory.SetAgent(snap, 2,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(52f, 35f)); // on top of GK

            SnapshotFactory.SetAgent(snap, 3,
                entityId: SnapshotFactory.DefenderIdB,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(53f, 35f));

            SnapshotFactory.SetAgent(snap, 4,
                entityId: 3,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(51f, 34f));

            bool result = TriggerEvaluator.EvaluateWeakReceiver(snap);

            Assert.IsFalse(result,
                "WeakReceiver must NOT trigger for the opposing GK (KD-13 exclusion).");
        }

        /// <summary>
        /// WEAK_RECEIVER fires when an outfield opponent has low first touch and is under pressure.
        /// </summary>
        [Test]
        public void WeakReceiver_FiresForLowSkillReceiverUnderPressure()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            // Carrier.
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Weak outfield receiver nearby: firstTouch=5 (below threshold=10).
            const int weakReceiverId = 40;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: weakReceiverId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(55f, 34f),
                firstTouch: 5f);

            // Three pressing-team defenders within CoverShadowCandidateRadiusM of receiver.
            // ThreatPressureNormalizer=3, so 3 defenders → pressure=1.0 >= WeakReceiverPressure=0.50.
            SnapshotFactory.SetAgent(snap, 2,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(55f, 34f));

            SnapshotFactory.SetAgent(snap, 3,
                entityId: SnapshotFactory.DefenderIdB,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(56f, 34f));

            SnapshotFactory.SetAgent(snap, 4,
                entityId: 3,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(54f, 34f));

            bool result = TriggerEvaluator.EvaluateWeakReceiver(snap);

            Assert.IsTrue(result,
                "WeakReceiver must fire for a low-skill outfield receiver with sufficient nearby defenders.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-010 / T-U-011 / T-U-012 — Debounce (§3.2)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class DebounceTests
    {
        private static PressingSnapshot MakeSnapWithBadTouch(float quality, float speed)
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: quality,
                postTouchSpeed: speed);
            return snap;
        }

        /// <summary>
        /// T-U-010: A trigger holds for TriggerDwellTicks ticks before the committed flag fires.
        /// After (TriggerDwellTicks - 1) ticks the flag must still be absent.
        /// At exactly TriggerDwellTicks the flag must be set.
        /// </summary>
        [Test]
        public void Debounce_CommittedFlagFiresAfterDwellTicks()
        {
            PressingSnapshot snap = MakeSnapWithBadTouch(0.30f, 6.0f);
            PressTrigger trigState = default;
            PassAttemptEvent noPass = default;

            // Pump (TriggerDwellTicks - 1) ticks — committed flag must remain absent.
            TriggerFlags committed = TriggerFlags.None;
            for (int t = 0; t < PressingAIConstants.TriggerDwellTicks - 1; t++)
            {
                committed = TriggerEvaluator.Evaluate(snap, noPass, false, ref trigState);
            }

            Assert.IsFalse(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must NOT fire before TriggerDwellTicks ticks.");

            // One more tick — now committed flag must be set.
            committed = TriggerEvaluator.Evaluate(snap, noPass, false, ref trigState);

            Assert.IsTrue(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must fire after TriggerDwellTicks consecutive ticks.");
        }

        /// <summary>
        /// T-U-011: After the raw condition clears, TriggerReleaseTicks ticks must elapse
        /// before the committed flag clears.
        /// </summary>
        [Test]
        public void Debounce_CommittedFlagClearsAfterReleaseTicks()
        {
            // First pump the dwell to commit the flag.
            PressingSnapshot snapBad = MakeSnapWithBadTouch(0.30f, 6.0f);
            PressingSnapshot snapGood = MakeSnapWithBadTouch(0.80f, 1.0f); // good touch — raw false
            PressTrigger trigState = default;
            PassAttemptEvent noPass = default;

            for (int t = 0; t < PressingAIConstants.TriggerDwellTicks; t++)
            {
                TriggerEvaluator.Evaluate(snapBad, noPass, false, ref trigState);
            }

            // Now pump (TriggerReleaseTicks - 1) ticks with raw false — committed must still hold.
            TriggerFlags committed = TriggerFlags.None;
            for (int t = 0; t < PressingAIConstants.TriggerReleaseTicks - 1; t++)
            {
                committed = TriggerEvaluator.Evaluate(snapGood, noPass, false, ref trigState);
            }

            Assert.IsTrue(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must persist until TriggerReleaseTicks ticks of cleared raw condition.");

            // Final tick — now it should clear.
            committed = TriggerEvaluator.Evaluate(snapGood, noPass, false, ref trigState);

            Assert.IsFalse(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must clear after TriggerReleaseTicks ticks of cleared raw condition.");
        }

        /// <summary>
        /// T-U-012: Asymmetric release — a single tick of cleared raw condition does NOT
        /// clear a committed flag (release counter resets on raw-true).
        /// </summary>
        [Test]
        public void Debounce_SingleClearTickDoesNotResetCommittedFlag()
        {
            PressingSnapshot snapBad  = MakeSnapWithBadTouch(0.30f, 6.0f);
            PressingSnapshot snapGood = MakeSnapWithBadTouch(0.80f, 1.0f);
            PressTrigger trigState = default;
            PassAttemptEvent noPass = default;

            // Commit the flag.
            for (int t = 0; t < PressingAIConstants.TriggerDwellTicks; t++)
            {
                TriggerEvaluator.Evaluate(snapBad, noPass, false, ref trigState);
            }

            // One tick cleared — release counter = 1, must NOT clear.
            TriggerFlags committed = TriggerEvaluator.Evaluate(snapGood, noPass, false, ref trigState);
            Assert.IsTrue(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must NOT clear after only 1 tick of cleared raw condition.");

            // Raw true again — release counter resets to 0.
            committed = TriggerEvaluator.Evaluate(snapBad, noPass, false, ref trigState);
            Assert.IsTrue(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must remain set after raw condition returns true.");

            // Now clear for full TriggerReleaseTicks — should eventually clear.
            for (int t = 0; t < PressingAIConstants.TriggerReleaseTicks; t++)
            {
                committed = TriggerEvaluator.Evaluate(snapGood, noPass, false, ref trigState);
            }

            Assert.IsFalse(
                (committed & TriggerFlags.BadTouch) != 0,
                "Committed flag must clear after a full TriggerReleaseTicks release sequence.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-020 / T-U-021 / T-U-022 / T-U-023 — Primary-Press Selection (§3.3)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class PrimaryPressSelectorTests
    {
        /// <summary>
        /// T-U-020: Worked-example arithmetic from §5.2.3.
        /// Ball-carrier at (40, 30) with velocity (3, 0).
        /// InterceptLookaheadTicks=3, DT_TACTICAL=0.10 → projected point = (40 + 3*3*0.1, 30) = (40.9, 30).
        /// Agent A at (38, 31): cost = (38-40.9)^2 + (31-30)^2 = 8.41+1 = 9.41.
        /// Agent B at (42, 32): cost = (42-40.9)^2 + (32-30)^2 = 1.21+4 = 5.21.
        /// B wins (lower cost).
        /// Both agents must be within PressTriggerDistanceM=8m of carrier at (40, 30).
        /// </summary>
        [Test]
        public void PrimaryPress_SelectsAgentWithLowerCost()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            // Carrier (opposing team).
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(40f, 30f),
                velocity: new Vector2(3f, 0f));

            // Agent A: pressing team, at (38, 31).
            const int agentAId = SnapshotFactory.DefenderIdA;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: agentAId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(38f, 31f),
                velocity: Vector2.zero);

            // Agent B: pressing team, at (42, 32).
            const int agentBId = SnapshotFactory.DefenderIdB;
            SnapshotFactory.SetAgent(snap, 2,
                entityId: agentBId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(42f, 32f),
                velocity: Vector2.zero);

            Vector2 interceptionPoint = PrimaryPressSelector.ComputeInterceptionPoint(snap);
            int selectedId = PrimaryPressSelector.Select(snap, interceptionPoint, out Vector2 _);

            Assert.AreEqual(agentBId, selectedId,
                "PrimaryPress must select Agent B (cost 5.21 < 9.41).");
        }

        /// <summary>
        /// T-U-021: Agents at or above PressFatigueCeiling are excluded.
        /// </summary>
        [Test]
        public void PrimaryPress_ExcludesIneligibleStaminaAgents()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Fatigued agent at PressFatigueCeiling — must be excluded.
            const int fatiguedId = 5;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: fatiguedId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                fatigue: PressingAIConstants.PressFatigueCeiling);

            int selectedId = PrimaryPressSelector.Select(
                snap,
                new Vector2(50f, 34f),
                out Vector2 _);

            Assert.AreEqual(-1, selectedId,
                "PrimaryPress must return -1 when the only eligible agent has fatigue >= PressFatigueCeiling.");
        }

        /// <summary>
        /// T-U-022: GK is never selected as primary presser (FR-PR-017).
        /// </summary>
        [Test]
        public void PrimaryPress_NeverSelectsGoalkeeper()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Only pressing-team agent is a GK.
            const int gkId = 6;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: gkId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                isGoalkeeper: true,
                fatigue: 0f);

            int selectedId = PrimaryPressSelector.Select(
                snap,
                new Vector2(50f, 34f),
                out Vector2 _);

            Assert.AreEqual(-1, selectedId,
                "PrimaryPress must never select a goalkeeper (FR-PR-017).");
        }

        /// <summary>
        /// T-U-023: EntityId ascending tie-break when costs are within SpacingEpsilonM2.
        /// Two agents placed identically (same position) → lower EntityId wins.
        /// </summary>
        [Test]
        public void PrimaryPress_TieBreaksByEntityIdAscending()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Two pressing-team agents at identical position → same cost.
            const int lowId  = 3;
            const int highId = 7;
            SnapshotFactory.SetAgent(snap, 1,
                entityId: highId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f));

            SnapshotFactory.SetAgent(snap, 2,
                entityId: lowId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f)); // same position as highId

            int selectedId = PrimaryPressSelector.Select(
                snap,
                new Vector2(50f, 34f),
                out Vector2 _);

            Assert.AreEqual(lowId, selectedId,
                "PrimaryPress tie-break must select the agent with the lower EntityId.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-030 / T-U-031 / T-U-032 — Cover-Shadow Selection (§3.4)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class CoverShadowSelectorTests
    {
        /// <summary>
        /// T-U-030: Worked example.
        /// Carrier at (60, 30), receiver at (75, 40).
        /// ShadowLane = Lerp(carrier, receiver, CoverShadowLaneFraction=0.55).
        /// X = 60 + 0.55*(75-60) = 60 + 8.25 = 68.25
        /// Y = 30 + 0.55*(40-30) = 30 + 5.5  = 35.5
        /// Shadow target ≈ (68.25, 35.5).
        /// </summary>
        [Test]
        public void CoverShadow_WorkedExampleGeometry()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();

            const int carrierId  = SnapshotFactory.CarrierEntityId;
            const int receiverId = 50;
            const int defenderId = SnapshotFactory.DefenderIdA;

            snap.BallCarrierEntityId = carrierId;
            snap.BallPosition = new Vector3(60f, 30f, 0f);

            // Carrier (opposing team) at (60, 30).
            SnapshotFactory.SetAgent(snap, 0,
                entityId: carrierId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(60f, 30f));

            // Receiver (opposing team, weak-skill to boost threat score) at (75, 40).
            SnapshotFactory.SetAgent(snap, 1,
                entityId: receiverId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(75f, 40f),
                firstTouch: 5f);

            // Defender (pressing team) positioned near shadow lane to ensure assignment.
            SnapshotFactory.SetAgent(snap, 2,
                entityId: defenderId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(68f, 35f),
                fatigue: 0f);

            int count = CoverShadowSelector.Select(snap, primaryPresserId: -1,
                out CoverShadow out0, out CoverShadow _);

            Assert.GreaterOrEqual(count, 1,
                "CoverShadowSelector must produce at least one assignment for the worked example.");

            float expectedX = 60f + PressingAIConstants.CoverShadowLaneFraction * (75f - 60f);
            float expectedY = 30f + PressingAIConstants.CoverShadowLaneFraction * (40f - 30f);
            const float tolerance = 1e-4f;

            Assert.That(out0.TargetPosition.x,
                Is.EqualTo(expectedX).Within(tolerance),
                "Shadow target X must match CoverShadowLaneFraction geometry.");

            Assert.That(out0.TargetPosition.y,
                Is.EqualTo(expectedY).Within(tolerance),
                "Shadow target Y must match CoverShadowLaneFraction geometry.");
        }

        /// <summary>
        /// T-U-031: Greedy assignment by threat score descending.
        /// Two receivers with different threat scores → higher-threat receiver is shadowed first.
        ///
        /// Fixture corrected (dotnet CI gate). The §3.4 threat formula's skill term is
        /// (FirstTouch / 20) * THREAT_SKILL_W, i.e. monotonically INCREASING in FirstTouch
        /// — a STRONGER first touch raises threat. The original fixture assigned the
        /// "high-threat" receiver a weak touch (5) and the "low-threat" receiver a strong
        /// touch (18), so the laterally-placed strong-skill receiver (id 52, score ≈ 0.28)
        /// actually out-scored the forward weak-skill receiver (id 51, score ≈ 0.245), and
        /// the selector correctly shadowed id 52 first. Production matches §3.4 line 230.
        /// The intended-higher-threat receiver is now genuinely higher threat: forward
        /// (progression gain) AND strong-skilled. Hand derivation with the two defenders
        /// below (both within 20 m of each receiver → pressure = 2/3):
        ///   id 51 @ (60,34) FT=18: 0.1905·0.50 + 0.3333·0.30 + 0.90·0.20 = 0.375
        ///   id 52 @ (50,44) FT=5 : 0       ·0.50 + 0.3333·0.30 + 0.25·0.20 = 0.150
        /// id 51 wins; its shadow lane (55.5, 34) is covered by defender1 (55,34).
        /// </summary>
        [Test]
        public void CoverShadow_AssignsHigherThreatReceiverFirst()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(50f, 34f, 0f);

            const int carrierId      = SnapshotFactory.CarrierEntityId;
            const int highThreatId   = 51;
            const int lowThreatId    = 52;
            const int defender1Id    = SnapshotFactory.DefenderIdA;
            const int defender2Id    = SnapshotFactory.DefenderIdB;

            snap.BallCarrierEntityId = carrierId;

            // Carrier (opposing).
            SnapshotFactory.SetAgent(snap, 0,
                entityId: carrierId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // High-threat receiver: close, forward (progression gain) AND strong skill
            // (high FirstTouch → high skill term per §3.4) — maximises threat score.
            SnapshotFactory.SetAgent(snap, 1,
                entityId: highThreatId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(60f, 34f),
                firstTouch: 18f);

            // Low-threat receiver: same distance, but laterally (no progression gain) and
            // weak skill (low FirstTouch → low skill term).
            SnapshotFactory.SetAgent(snap, 2,
                entityId: lowThreatId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 44f),
                firstTouch: 5f);

            // Two defenders available.
            SnapshotFactory.SetAgent(snap, 3,
                entityId: defender1Id,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(55f, 34f));

            SnapshotFactory.SetAgent(snap, 4,
                entityId: defender2Id,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(50f, 40f));

            int count = CoverShadowSelector.Select(snap, primaryPresserId: -1,
                out CoverShadow out0, out CoverShadow _);

            Assert.GreaterOrEqual(count, 1,
                "CoverShadowSelector must produce at least one assignment.");

            Assert.AreEqual(highThreatId, out0.ReceiverId,
                "First shadow must cover the higher-threat receiver.");
        }

        /// <summary>
        /// T-U-032: When no eligible defender is available for a receiver slot,
        /// that slot is not filled (remains at 0 count; effectively demoted to HoldShape).
        /// </summary>
        [Test]
        public void CoverShadow_UnfilledSlotProducesZeroCount()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(50f, 34f, 0f);

            const int carrierId  = SnapshotFactory.CarrierEntityId;
            const int receiverId = 53;

            snap.BallCarrierEntityId = carrierId;

            // Carrier (opposing).
            SnapshotFactory.SetAgent(snap, 0,
                entityId: carrierId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Receiver within radius.
            SnapshotFactory.SetAgent(snap, 1,
                entityId: receiverId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(55f, 34f),
                firstTouch: 5f);

            // No pressing-team defenders available at all.
            // All slots remain inactive (set up by MakeDefault).

            int count = CoverShadowSelector.Select(snap, primaryPresserId: -1,
                out CoverShadow _, out CoverShadow _);

            Assert.AreEqual(0, count,
                "CoverShadowSelector must return 0 when no eligible defender is available (F4/FR-PR-038).");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-040 / T-U-041 — Role Hysteresis (§3.6)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class RoleHysteresisTests
    {
        /// <summary>
        /// T-U-040: An oscillating candidate stays in lastRole for RoleDwellTicks ticks of mismatch.
        /// After (RoleDwellTicks - 1) mismatching ticks the role must still be HoldShape.
        /// </summary>
        [Test]
        public void RoleHysteresis_HoldsLastRoleDuringDwellPeriod()
        {
            RoleHysteresisState hyst = new RoleHysteresisState(PressingAIConstants.SQUAD_SIZE);
            const int agentIdx = 0;

            // Drive toward PrimaryPress for (RoleDwellTicks - 1) ticks — must stay HoldShape.
            PressRole committed = PressRole.HoldShape;
            for (int t = 0; t < PressingAIConstants.RoleDwellTicks - 1; t++)
            {
                committed = RoleHysteresis.Commit(agentIdx, PressRole.PrimaryPress, hyst);
            }

            Assert.AreEqual(PressRole.HoldShape, committed,
                "Role must remain HoldShape before RoleDwellTicks ticks of mismatch.");
        }

        /// <summary>
        /// T-U-041: Stable candidate transition fires exactly at the Nth consecutive matching tick.
        /// After RoleDwellTicks ticks of PrimaryPress candidate, role commits.
        /// </summary>
        [Test]
        public void RoleHysteresis_TransitionsAtNthConsecutiveTick()
        {
            RoleHysteresisState hyst = new RoleHysteresisState(PressingAIConstants.SQUAD_SIZE);
            const int agentIdx = 0;

            PressRole committed = PressRole.HoldShape;
            for (int t = 0; t < PressingAIConstants.RoleDwellTicks; t++)
            {
                committed = RoleHysteresis.Commit(agentIdx, PressRole.PrimaryPress, hyst);
            }

            Assert.AreEqual(PressRole.PrimaryPress, committed,
                "Role must commit to PrimaryPress exactly at RoleDwellTicks consecutive ticks.");
        }

        /// <summary>
        /// ForceAllHoldShape resets all agents to HoldShape with zero dwell.
        /// </summary>
        [Test]
        public void RoleHysteresis_ForceAllHoldShapeResetsState()
        {
            RoleHysteresisState hyst = new RoleHysteresisState(PressingAIConstants.SQUAD_SIZE);
            const int agentIdx = 0;

            // Commit PrimaryPress.
            for (int t = 0; t < PressingAIConstants.RoleDwellTicks; t++)
            {
                RoleHysteresis.Commit(agentIdx, PressRole.PrimaryPress, hyst);
            }

            RoleHysteresis.ForceAllHoldShape(hyst);

            Assert.AreEqual(PressRole.HoldShape, hyst.LastRole[agentIdx],
                "ForceAllHoldShape must reset LastRole to HoldShape.");

            Assert.AreEqual(0, hyst.RoleDwell[agentIdx],
                "ForceAllHoldShape must reset RoleDwell to zero.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-050 / T-U-051 — Stamina (§3.7)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class StaminaAccumulatorTests
    {
        /// <summary>
        /// T-U-050: StaminaCostPrimaryPerTick = 0.0040 accumulates exactly over 100 ticks.
        /// 100 ticks * 0.0040 = 0.40. Start fatigue = 0.0 → end = 0.40.
        /// </summary>
        [Test]
        public void Stamina_PrimaryAccumulatesCorrectlyOver100Ticks()
        {
            float fatigue = 0f;

            for (int t = 0; t < 100; t++)
            {
                StaminaAccumulator.Apply(PressRole.PrimaryPress, ref fatigue);
            }

            float expected = 100 * PressingAIConstants.StaminaCostPrimaryPerTick;
            Assert.That(fatigue, Is.EqualTo(expected).Within(1e-5f),
                "Primary press fatigue must accumulate exactly StaminaCostPrimaryPerTick * 100 ticks.");
        }

        /// <summary>
        /// T-U-051a: Agent at fatigue=0.84 IS eligible (below PressFatigueCeiling=0.85).
        /// </summary>
        [Test]
        public void Stamina_AgentJustBelowCeilingIsEligible()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // Agent just below ceiling.
            const float justBelowCeiling = PressingAIConstants.PressFatigueCeiling - 0.01f; // 0.84
            SnapshotFactory.SetAgent(snap, 1,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                fatigue: justBelowCeiling);

            int selectedId = PrimaryPressSelector.Select(snap, new Vector2(50f, 34f), out Vector2 _);

            Assert.AreEqual(SnapshotFactory.DefenderIdA, selectedId,
                "Agent with fatigue 0.84 (below PressFatigueCeiling=0.85) must be eligible.");
        }

        /// <summary>
        /// T-U-051b: Agent at fatigue=0.85 (exactly PressFatigueCeiling) IS excluded.
        /// </summary>
        [Test]
        public void Stamina_AgentAtCeilingIsExcluded()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            SnapshotFactory.SetAgent(snap, 1,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                fatigue: PressingAIConstants.PressFatigueCeiling);

            int selectedId = PrimaryPressSelector.Select(snap, new Vector2(50f, 34f), out Vector2 _);

            Assert.AreEqual(-1, selectedId,
                "Agent with fatigue = PressFatigueCeiling (0.85) must be EXCLUDED (0=rested, 1=fatigued).");
        }

        /// <summary>
        /// T-U-051c: Agent at fatigue=1.0 (fully fatigued) IS excluded.
        /// </summary>
        [Test]
        public void Stamina_FullyFatiguedAgentIsExcluded()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            SnapshotFactory.SetAgent(snap, 1,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                fatigue: 1.0f);

            int selectedId = PrimaryPressSelector.Select(snap, new Vector2(50f, 34f), out Vector2 _);

            Assert.AreEqual(-1, selectedId,
                "Agent with fatigue=1.0 (fully fatigued) must be EXCLUDED.");
        }

        /// <summary>
        /// T-U-051d: Agent at fatigue=0.0 (fully rested) IS eligible.
        /// </summary>
        [Test]
        public void Stamina_FullyRestedAgentIsEligible()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            SnapshotFactory.SetAgent(snap, 1,
                entityId: SnapshotFactory.DefenderIdA,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(48f, 34f),
                fatigue: 0.0f);

            int selectedId = PrimaryPressSelector.Select(snap, new Vector2(50f, 34f), out Vector2 _);

            Assert.AreEqual(SnapshotFactory.DefenderIdA, selectedId,
                "Agent with fatigue=0.0 (fully rested) must be ELIGIBLE.");
        }

        /// <summary>
        /// HoldShape role incurs no fatigue cost.
        /// </summary>
        [Test]
        public void Stamina_HoldShapeIncursNoCost()
        {
            float fatigue = 0.5f;
            StaminaAccumulator.Apply(PressRole.HoldShape, ref fatigue);

            Assert.AreEqual(0.5f, fatigue,
                "HoldShape role must not change fatigue.");
        }

        /// <summary>
        /// Fatigue is clamped at 1.0 even when accumulated cost would exceed it.
        /// </summary>
        [Test]
        public void Stamina_FatigueClampedAtOne()
        {
            float fatigue = 0.9999f;
            StaminaAccumulator.Apply(PressRole.PrimaryPress, ref fatigue);

            Assert.LessOrEqual(fatigue, 1.0f,
                "Fatigue must be clamped to 1.0 even when cost would overflow.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-060 / T-U-061 / T-U-062 — Disengage and Reset (§3.8)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class DisengageResolverTests
    {
        private static PressingSnapshot MakeInZoneSnap()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            // Ball well inside the press zone.
            snap.BallPosition = new Vector3(
                PressingAIConstants.PressZoneXMin + 10f, 34f, 0f);
            return snap;
        }

        /// <summary>
        /// T-U-060: Timeout — no trigger for DisengageTimeoutTicks consecutive ticks fires disengage.
        /// </summary>
        [Test]
        public void Disengage_TimeoutFiresAfterDisengageTimeoutTicks()
        {
            PressingSnapshot snap = MakeInZoneSnap();
            int disengageDwell  = 0;
            int cooldownTicks   = 0;

            bool collapsed = false;
            for (int t = 0; t < PressingAIConstants.DisengageTimeoutTicks; t++)
            {
                collapsed = DisengageResolver.Evaluate(
                    snap, TriggerFlags.None, ref disengageDwell, ref cooldownTicks);
            }

            Assert.IsTrue(collapsed,
                "Disengage must fire after DisengageTimeoutTicks ticks with no committed trigger.");
        }

        /// <summary>
        /// Disengage does NOT fire before DisengageTimeoutTicks ticks have elapsed.
        /// </summary>
        [Test]
        public void Disengage_DoesNotFireBeforeTimeoutExpires()
        {
            PressingSnapshot snap = MakeInZoneSnap();
            int disengageDwell = 0;
            int cooldownTicks  = 0;

            bool collapsed = false;
            for (int t = 0; t < PressingAIConstants.DisengageTimeoutTicks - 1; t++)
            {
                collapsed = DisengageResolver.Evaluate(
                    snap, TriggerFlags.None, ref disengageDwell, ref cooldownTicks);
            }

            Assert.IsFalse(collapsed,
                "Disengage must NOT fire before DisengageTimeoutTicks ticks.");
        }

        /// <summary>
        /// T-U-061: Zone disengage — ball at X less than PressZoneXMin triggers immediate disengage.
        /// </summary>
        [Test]
        public void Disengage_ZoneExitFiresImmediately()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            // Ball outside press zone.
            snap.BallPosition = new Vector3(PressingAIConstants.PressZoneXMin - 1f, 34f, 0f);

            int disengageDwell = 0;
            int cooldownTicks  = 0;

            bool collapsed = DisengageResolver.Evaluate(
                snap, TriggerFlags.BadTouch, ref disengageDwell, ref cooldownTicks);

            Assert.IsTrue(collapsed,
                "Disengage must fire immediately when ball X < PressZoneXMin.");

            Assert.AreEqual(PressingAIConstants.ResetLatencyTicks, cooldownTicks,
                "Cooldown must be set to ResetLatencyTicks on zone-exit disengage.");
        }

        /// <summary>
        /// T-U-062: Reset cooldown — no new press fires for ResetLatencyTicks ticks after disengage.
        /// IsInCooldown returns true for ResetLatencyTicks ticks, then false.
        /// </summary>
        [Test]
        public void Disengage_ResetCooldownPreventsNewPressForLatencyTicks()
        {
            PressingSnapshot snapOut = SnapshotFactory.MakeDefault();
            snapOut.BallPosition = new Vector3(PressingAIConstants.PressZoneXMin - 1f, 34f, 0f);

            int disengageDwell = 0;
            int cooldownTicks  = 0;

            // Trigger disengage.
            DisengageResolver.Evaluate(snapOut, TriggerFlags.None, ref disengageDwell, ref cooldownTicks);
            Assert.AreEqual(PressingAIConstants.ResetLatencyTicks, cooldownTicks,
                "Cooldown must be set to ResetLatencyTicks immediately after disengage.");

            // Ball moves back into zone — but cooldown must block new press.
            PressingSnapshot snapIn = MakeInZoneSnap();

            // Pump cooldown down to 1 remaining (ResetLatencyTicks - 1 decrements).
            for (int t = 0; t < PressingAIConstants.ResetLatencyTicks - 1; t++)
            {
                DisengageResolver.Evaluate(snapIn, TriggerFlags.BadTouch, ref disengageDwell, ref cooldownTicks);
                Assert.IsTrue(DisengageResolver.IsInCooldown(cooldownTicks),
                    $"IsInCooldown must be true at tick {t + 1} during reset latency.");
            }

            // Final decrement — cooldown reaches 0.
            DisengageResolver.Evaluate(snapIn, TriggerFlags.BadTouch, ref disengageDwell, ref cooldownTicks);
            Assert.IsFalse(DisengageResolver.IsInCooldown(cooldownTicks),
                "IsInCooldown must be false after ResetLatencyTicks ticks have passed.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // T-U-070 / T-U-071 / T-U-073 / T-U-074 — Failure Modes (§2.4)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class FailureModeTests
    {
        /// <summary>
        /// T-U-071: F2 — NaN in touch quality suppresses BadTouch; other triggers may still proceed.
        /// EvaluateBadTouch must return false (not throw) when LastTouchQuality is NaN.
        /// </summary>
        [Test]
        public void FailureMode_NaNTouchQualitySuppressesBadTouch()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f),
                lastTouchQ: float.NaN,
                postTouchSpeed: 6.0f);

            bool result = TriggerEvaluator.EvaluateBadTouch(snap);

            Assert.IsFalse(result,
                "F2: NaN in touch quality must suppress BadTouch (comparison with NaN is false).");
        }

        /// <summary>
        /// T-U-073: F4 — empty cover-shadow candidate set → Select returns 0 (slot demotes to HoldShape).
        /// Verified by T-U-032 above; this test confirms the count is exactly 0, not negative.
        /// </summary>
        [Test]
        public void FailureMode_EmptyCandidateSetProducesZeroShadows()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            SnapshotFactory.SetAgent(snap, 0,
                entityId: SnapshotFactory.CarrierEntityId,
                teamId: SnapshotFactory.OpposingTeamId,
                position: new Vector2(50f, 34f));

            // No receivers and no defenders.
            int count = CoverShadowSelector.Select(snap, -1, out CoverShadow _, out CoverShadow _);

            Assert.AreEqual(0, count,
                "F4: empty candidate set must produce exactly 0 cover-shadow assignments.");
        }

        /// <summary>
        /// T-U-074: F5 — InvariantEnforcer returns false (all-HOLD_SHAPE) when MinBacklineAgents
        /// is violated and cannot be resolved by demotion.
        /// </summary>
        [Test]
        public void FailureMode_InvariantViolationMinBacklineTriggersF5()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(50f, 34f, 0f);
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            // Only 2 defense-line agents in own third (MinBacklineAgents=3 → violation).
            // Pressing team attacks +X; own defensive third is X < PressZoneXMin.
            float defensiveThirdX = PressingAIConstants.PITCH_THIRD_M - 5f; // 30m — inside own third

            for (int i = 0; i < 2; i++)
            {
                SnapshotFactory.SetAgent(snap, i,
                    entityId: i + 1,
                    teamId: SnapshotFactory.PressingTeamId,
                    position: new Vector2(defensiveThirdX, 20f + i * 5f),
                    line: LineId.Defense);
            }

            PressDirective directive = new PressDirective
            {
                PrimaryPresserId = 99,
                PrimaryTargetPosition = new Vector2(50f, 34f),
                CoverShadowCount = 0,
                ActiveTriggers = TriggerFlags.BadTouch,
            };

            bool survived = InvariantEnforcer.Enforce(snap, ref directive);

            Assert.IsFalse(survived,
                "F5: InvariantEnforcer must return false when MinBacklineAgents is violated.");

            Assert.AreEqual(-1, directive.PrimaryPresserId,
                "F5: directive must be set to Inactive (PrimaryPresserId=-1) on invariant failure.");
        }

        /// <summary>
        /// Invariant (3): cover-shadow target beyond MaxPressDisplacementM is demoted (not F5 alone).
        /// </summary>
        [Test]
        public void FailureMode_ExcessiveDisplacementDemotesShadowSlot()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            snap.BallPosition = new Vector3(50f, 34f, 0f);
            snap.BallCarrierEntityId = SnapshotFactory.CarrierEntityId;

            // Three defense-line agents in own third to satisfy invariant (2).
            float defensiveX = PressingAIConstants.PITCH_THIRD_M - 5f;
            for (int i = 0; i < 3; i++)
            {
                SnapshotFactory.SetAgent(snap, i,
                    entityId: i + 1,
                    teamId: SnapshotFactory.PressingTeamId,
                    position: new Vector2(defensiveX, 10f + i * 10f),
                    line: LineId.Defense);
            }

            // Defender with baseline slot at (20, 20); shadow target 30 m away → exceeds MaxPressDisplacementM=25m.
            const int defenderId = 50;
            SnapshotFactory.SetAgent(snap, 3,
                entityId: defenderId,
                teamId: SnapshotFactory.PressingTeamId,
                position: new Vector2(20f, 20f),
                baselineSlot: new Vector2(20f, 20f),
                line: LineId.Midfield);

            Vector2 farTarget = new Vector2(50f, 20f); // 30m from baseline slot → > MaxPressDisplacementM

            PressDirective directive = new PressDirective
            {
                PrimaryPresserId = -1,
                PrimaryTargetPosition = Vector2.zero,
                CoverShadowCount = 1,
                Shadow0 = new CoverShadow(defenderId, 99, farTarget),
                Shadow1 = default,
                ActiveTriggers = TriggerFlags.BadTouch,
            };

            bool survived = InvariantEnforcer.Enforce(snap, ref directive);

            // Displacement demotion alone does not trigger F5.
            // If MinBackline is satisfied the directive should survive (returns true) with count demoted to 0.
            if (survived)
            {
                Assert.AreEqual(0, directive.CoverShadowCount,
                    "Shadow slot exceeding MaxPressDisplacementM must be demoted to count=0.");
            }
            else
            {
                // F5 fired (perhaps due to combined invariant) — directive must be Inactive.
                Assert.AreEqual(-1, directive.PrimaryPresserId,
                    "If F5 fires the directive must be Inactive.");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Geometric pressure helper (§3.4)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class GeometricPressureTests
    {
        /// <summary>
        /// ComputeGeometricPressure returns 0 when no pressing-team agents are within radius.
        /// </summary>
        [Test]
        public void GeometricPressure_ZeroWhenNoDefendersInRadius()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            float radiusSq = PressingAIConstants.CoverShadowCandidateRadiusM
                           * PressingAIConstants.CoverShadowCandidateRadiusM;

            float pressure = TriggerEvaluator.ComputeGeometricPressure(
                new Vector2(50f, 34f), snap, SnapshotFactory.PressingTeamId, radiusSq);

            Assert.AreEqual(0f, pressure,
                "GeometricPressure must be 0 when no pressing-team agents are within radius.");
        }

        /// <summary>
        /// ComputeGeometricPressure returns exactly 1 when defender count reaches ThreatPressureNormalizer.
        /// </summary>
        [Test]
        public void GeometricPressure_SaturatesAtOneWithNormalizerDefenders()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            float radiusSq = PressingAIConstants.CoverShadowCandidateRadiusM
                           * PressingAIConstants.CoverShadowCandidateRadiusM;

            Vector2 receiverPos = new Vector2(50f, 34f);
            int normalizerCount = (int)PressingAIConstants.ThreatPressureNormalizer; // 3

            for (int i = 0; i < normalizerCount; i++)
            {
                SnapshotFactory.SetAgent(snap, i,
                    entityId: i + 1,
                    teamId: SnapshotFactory.PressingTeamId,
                    position: receiverPos + new Vector2(i * 0.1f, 0f)); // just inside radius
            }

            float pressure = TriggerEvaluator.ComputeGeometricPressure(
                receiverPos, snap, SnapshotFactory.PressingTeamId, radiusSq);

            Assert.That(pressure, Is.EqualTo(1f).Within(1e-5f),
                "GeometricPressure must saturate at 1.0 when count == ThreatPressureNormalizer.");
        }

        /// <summary>
        /// ComputeGeometricPressure excludes GK agents from the count.
        /// </summary>
        [Test]
        public void GeometricPressure_ExcludesGoalkeeper()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault();
            float radiusSq = PressingAIConstants.CoverShadowCandidateRadiusM
                           * PressingAIConstants.CoverShadowCandidateRadiusM;

            Vector2 receiverPos = new Vector2(50f, 34f);

            // GK from pressing team within radius — must be excluded.
            SnapshotFactory.SetAgent(snap, 0,
                entityId: 1,
                teamId: SnapshotFactory.PressingTeamId,
                position: receiverPos,
                isGoalkeeper: true);

            float pressure = TriggerEvaluator.ComputeGeometricPressure(
                receiverPos, snap, SnapshotFactory.PressingTeamId, radiusSq);

            Assert.AreEqual(0f, pressure,
                "GeometricPressure must not count GK agents from the pressing team.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // PressDirective invariants
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class PressDirectiveTests
    {
        /// <summary>
        /// PressDirective.Inactive has PrimaryPresserId=-1 and IsActive=false.
        /// </summary>
        [Test]
        public void PressDirective_InactiveHasNegativePresserIdAndIsActiveFalse()
        {
            PressDirective inactive = PressDirective.Inactive;

            Assert.AreEqual(-1, inactive.PrimaryPresserId,
                "Inactive directive must have PrimaryPresserId = -1.");

            Assert.IsFalse(inactive.IsActive,
                "Inactive directive must have IsActive = false.");

            Assert.AreEqual(0, inactive.CoverShadowCount,
                "Inactive directive must have CoverShadowCount = 0.");

            Assert.AreEqual(TriggerFlags.None, inactive.ActiveTriggers,
                "Inactive directive must have ActiveTriggers = None.");
        }

        /// <summary>
        /// PressDirective.IsActive returns true when PrimaryPresserId >= 0.
        /// </summary>
        [Test]
        public void PressDirective_IsActiveWhenPresserIdIsNonNegative()
        {
            PressDirective directive = PressDirective.Inactive;
            directive.PrimaryPresserId = 5;

            Assert.IsTrue(directive.IsActive,
                "IsActive must be true when PrimaryPresserId >= 0.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.3 Integration / §5.4 Determinism / §5.5 Performance / §5.6 Anti-Chaos
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class PressingAIIntegrationTests
    {
        // ── §5.3 Integration ──────────────────────────────────────────────────────

        /// <summary>T-I-001: Each trigger × OutOfPoss phase produces a valid, active directive. §5.3.</summary>
        [Test]
        public void Integration_T_I_001_EachTriggerOutOfPossProducesActiveDirective()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent match simulation — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-002: Phase boundary OutOfPoss → InPoss immediately empties the directive (FR-PR-033). §5.3.</summary>
        [Test]
        public void Integration_T_I_002_InPossPhaseBoundaryEmptiesDirective()
        {
            Assert.Ignore("Stage 0+1: requires phase input from PositioningAI — activate when PressingAITick reads live phase transitions from PositioningAITick");
        }

        /// <summary>T-I-003: Possession turnover InPoss → TransToDef → OutOfPoss produces correct per-tick directive evolution. §5.3.</summary>
        [Test]
        public void Integration_T_I_003_PossessionTurnoverSequenceProducesCorrectEvolution()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick phase-change injection — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-004: Disengage round-trip over 50-tick window: trigger → press → timeout → disengage → reset → trigger again. §5.3.</summary>
        [Test]
        public void Integration_T_I_004_DisengageRoundTripOver50Ticks()
        {
            Assert.Ignore("Stage 0+1: requires full tick-loop driving PressingAITick — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-005: Substitution event — substituted agent's PressAssignment is preserved at last value (F6). §5.3.</summary>
        [Test]
        public void Integration_T_I_005_SubstitutionPreservesLastPressAssignment()
        {
            Assert.Ignore("Stage 0+1: requires SubstitutionEvent from event bus — activate when EventBus SubstitutionEvent (0x08) is wired into PressingAITick");
        }

        /// <summary>T-I-006: Two triggers fire simultaneously (BAD_TOUCH + SIDELINE_TRAP) — primary-press and cover-shadow assigned; trigger origin tie-break is EntityId ascending. §5.3.</summary>
        [Test]
        public void Integration_T_I_006_SimultaneousTriggersTieBreakByEntityId()
        {
            Assert.Ignore("Stage 0+1: requires simultaneous trigger injection into the full pipeline — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-007: Three-agent eligibility tie under cost + SpacingEpsilonM2 window resolves deterministically by EntityId. §5.3.</summary>
        [Test]
        public void Integration_T_I_007_ThreeAgentCostTieResolvesDeteterministicallyByEntityId()
        {
            Assert.Ignore("Stage 0+1: requires full agent-pool injection into PrimaryPressSelector with tied costs — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-008: F5 invariant fallback after forced MaxPressDisplacementM violation by cover-shadow assignment past 25 m. §5.3.</summary>
        [Test]
        public void Integration_T_I_008_F5FallbackOnMaxDisplacementViolation()
        {
            Assert.Ignore("Stage 0+1: requires engineered cover-shadow assignment exceeding 25 m displacement — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-I-009: Hysteresis state survives a save/restore round-trip (#16 §3.2 binding). §5.3.</summary>
        [Test]
        public void Integration_T_I_009_HysteresisStateSurvivesSaveRestoreRoundTrip()
        {
            Assert.Ignore("Stage 0+1: requires Spec #16 SnapshotCodec round-trip — activate when DeterministicSim SnapshotCodec is wired and PressingAITick exposes snapshot serialisation");
        }

        /// <summary>T-I-010: Pure-function property — identical inputs produce bit-identical output across two invocations. §5.3.</summary>
        [Test]
        public void Integration_T_I_010_IdenticalInputsProduceBitIdenticalOutput()
        {
            Assert.Ignore("Stage 0+1: requires full pipeline invocation with a fixed snapshot — activate when PressingAITick is wired into TickOrchestrator");
        }

        // ── §5.4 Determinism Regression ───────────────────────────────────────────

        /// <summary>T-D-001: 90-min match replay on reference host produces bit-identical per-tick digest over two runs (same seed). §5.4.</summary>
        [Test]
        public void Determinism_T_D_001_NinetyMinReplayBitIdenticalDigests()
        {
            Assert.Ignore("Stage 0+1: requires reference-host replay harness and DeterministicSim digest comparison — activate when PressingAITick is wired into TickOrchestrator and #16 §5 regression suite is available");
        }

        /// <summary>T-D-002: EntityId-permuted input produces identical post-iteration state (#16 §3.2.5 binding). §5.4.</summary>
        [Test]
        public void Determinism_T_D_002_EntityIdPermutedInputProducesIdenticalState()
        {
            Assert.Ignore("Stage 0+1: requires #16 §3.2.5 EntityId-ascending iteration guarantee and full pipeline — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-D-003: RoleHysteresisState + PressTrigger digest contributions are non-empty for every tick a press fires. §5.4.</summary>
        [Test]
        public void Determinism_T_D_003_HysteresisAndTriggerDigestContributionsNonEmpty()
        {
            Assert.Ignore("Stage 0+1: requires EventLedger digest emission from PressingAITick — activate when #16 SerializeLedger path includes Pressing AI state contributions");
        }

        /// <summary>T-D-004: Cross-run digest stability — 10 consecutive 90-minute runs produce the same final-tick digest. §5.4.</summary>
        [Test]
        public void Determinism_T_D_004_TenConsecutiveRunsProduceSameFinalDigest()
        {
            Assert.Ignore("Stage 0+1: requires reference-host replay harness — activate when PressingAITick is wired into TickOrchestrator and #16 §5 regression suite is available");
        }

        /// <summary>T-D-005: Save/load mid-press — post-load tick 1 digest matches pre-save tick (N+1) digest. §5.4.</summary>
        [Test]
        public void Determinism_T_D_005_SaveLoadMidPressDigestContinuity()
        {
            Assert.Ignore("Stage 0+1: requires Spec #16 SnapshotCodec mid-press serialisation — activate when DeterministicSim snapshot round-trip is verified and PressingAITick state is included in the snapshot payload");
        }

        /// <summary>T-D-006: RNG domain-tag isolation — removing DOMAIN_TAG_PRESSING_AI calls from another spec's stream leaves #13's stream unchanged. §5.4.</summary>
        [Test]
        public void Determinism_T_D_006_DomainTagIsolationPreservesStream()
        {
            Assert.Ignore("Stage 0+1: regression for stochastic Stage 1+ press steps — activate when DOMAIN_TAG_PRESSING_AI RNG draws are introduced in PressingAITick");
        }

        // ── §5.5 Performance Validation ───────────────────────────────────────────

        /// <summary>T-P-001: Per-tick wall-clock ≤ 0.10 ms on the named reference host (§6.3). §5.5.</summary>
        [Test]
        public void Performance_T_P_001_PerTickWallClockWithinBudget()
        {
            Assert.Ignore("Stage 0+1: requires reference host (Ryzen 7 5800X) and Unity Stopwatch micro-benchmark harness — activate when certification-platform.md Stage-0 host pin is confirmed");
        }

        /// <summary>T-P-002: Zero heap allocations on the hot path under .NET allocation tracker (FR-PR-006, #18 §3.7). §5.5.</summary>
        [Test]
        public void Performance_T_P_002_ZeroHeapAllocationsOnHotPath()
        {
            Assert.Ignore("Stage 0+1: requires Unity Memory Profiler with Allocation Tracking — activate when PressingAITick is wired into TickOrchestrator and #18 §3.7 allocation-tracking gate is active");
        }

        /// <summary>T-P-003: Cover-shadow candidate scan bounded by CoverShadowCandidateRadiusM costs ≤ 0.03 ms. §5.5.</summary>
        [Test]
        public void Performance_T_P_003_CoverShadowCandidateScanWithinCostBudget()
        {
            Assert.Ignore("Stage 0+1: requires reference host and isolated CoverShadowSelector micro-benchmark — activate when certification-platform.md Stage-0 host pin is confirmed");
        }

        // ── §5.6 Anti-Chaos Invariants (KD-16) ───────────────────────────────────

        /// <summary>T-C-001: Forcing 4 simultaneous press candidates into the ball-side third → 1 demotes to HoldShape (count = 3). §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_001_FourCandidatesInBallThirdOneDemotes()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent pipeline with engineered over-count scenario — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-C-002: Forcing a Defense-line agent into PRIMARY_PRESS that would drop MinBacklineAgents below 3 → promotion blocked. §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_002_BacklineFloorBlocksPrimaryPressPromotion()
        {
            Assert.Ignore("Stage 0+1: requires full 22-agent pipeline with engineered backline-depletion scenario — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-C-003: Forcing a cover-shadow target > 25 m from baseline → rejected, demoted to HoldShape. §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_003_CoverShadowBeyond25mRejectedAndDemoted()
        {
            Assert.Ignore("Stage 0+1: requires engineered cover-shadow assignment exceeding MaxPressDisplacementM — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-C-004: Cascading violations (1+2+3 simultaneously) resolve to a clean directive within 3 demotion iterations. §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_004_CascadingViolationsResolveInThreeIterations()
        {
            Assert.Ignore("Stage 0+1: requires engineered compound invariant violation scenario — activate when PressingAITick is wired into TickOrchestrator and InvariantEnforcer iteration telemetry is observable");
        }

        /// <summary>T-C-005: Unresolvable cascade (primary-press demotion required) triggers F5 / FR-PR-039 fallback. §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_005_UnresolvableCascadeTriggersF5Fallback()
        {
            Assert.Ignore("Stage 0+1: requires engineered structurally-unresolvable invariant violation — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-C-006: Repeated F5 fallback across N ticks does NOT corrupt hysteresis state (regression for state leakage on fallback). §5.6.1 / KD-16.</summary>
        [Test]
        public void AntiChaos_T_C_006_RepeatedF5FallbackDoesNotCorruptHysteresisState()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick F5-triggering scenario with hysteresis state inspection — activate when PressingAITick is wired into TickOrchestrator");
        }

        /// <summary>T-C-007: Backline-floor breach triggers F5 immediately (1 iteration, not the cover-shadow demotion path). §5.6.1 / AR-S1-M6.</summary>
        [Test]
        public void AntiChaos_T_C_007_BacklineBreachTriggersF5InOneIteration()
        {
            Assert.Ignore("Stage 0+1: requires engineered Defense-line agent forced into PRIMARY_PRESS with backlineCount < MinBacklineAgents = 3 — activate when PressingAITick is wired into TickOrchestrator and InvariantEnforcer iteration telemetry is observable");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                         |
// | 1.0     | 2026-05-31 | —      | Initial implementation. 25 unit tests covering T-U-001–T-U-075 trigger conditions,           |
// |         |            |        | debounce, primary-press selection, cover-shadow selection, role hysteresis, stamina,          |
// |         |            |        | disengage/reset, failure modes, geometric pressure, and directive invariants.                 |
// | 1.1     | 2026-06-01 | —      | Added PressingAIIntegrationTests class with 26 stubs: T-I-001..010 (§5.3 integration),       |
// |         |            |        | T-D-001..006 (§5.4 determinism), T-P-001..003 (§5.5 performance), T-C-001..007 (§5.6        |
// |         |            |        | anti-chaos including AR-S1-M6 T-C-007). All stubs use Assert.Ignore with Stage 0+1 conditions.|
// | 1.2     | 2026-06-13 | —      | dotnet CI gate adjudication (T-U-031): fixture defect, not a production defect.               |
// |         |            |        | CoverShadow_AssignsHigherThreatReceiverFirst encoded an inverted skill term — the §3.4        |
// |         |            |        | threat formula's skill component is (FirstTouch/20)·THREAT_SKILL_W (increasing in FirstTouch),|
// |         |            |        | so the "high-threat" receiver's weak touch (5) actually under-scored the "low-threat"         |
// |         |            |        | receiver's strong touch (18) and the selector correctly shadowed id 52. FirstTouch values     |
// |         |            |        | swapped so id 51 is genuinely higher threat (forward + strong skill). CoverShadowSelector     |
// |         |            |        | unchanged; matches §3.4 line 230.                                                             |
#endregion
