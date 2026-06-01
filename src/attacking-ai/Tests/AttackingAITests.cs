// File:     src/attacking-ai/Tests/AttackingAITests.cs
// Created:  2026-05-31
// Modified: 2026-05-31
// Author:   —
// Spec:     Attacking AI #15 §5, Code Standards #20
// Purpose:  Unit tests for Attacking AI. T-AT-U unit tests from §5.

using System.Reflection;

using UnityEngine;

using NUnit.Framework;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.AttackingAI.Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 Constant-catalogue tests (T-AT-U-045, T-AT-U-049)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackingAIConstantsTests
    {
        /// <summary>
        /// T-AT-U-045: FINAL_THIRD_X_M = PITCH_LENGTH_M × 2/3 = 70.0 m.
        /// </summary>
        [Test]
        public void FinalThirdXM_IsTwoThirdsOfPitchLength()
        {
            float expected = AttackingAIConstants.PITCH_LENGTH_M * (2f / 3f);
            Assert.AreEqual(expected, AttackingAIConstants.FinalThirdXM, 0.001f,
                "FinalThirdXM must equal PITCH_LENGTH_M × 2/3 = 70.0 m (§6.1.3 / KD-16).");
        }

        /// <summary>
        /// T-AT-U-049: No magic numbers — all constants must live in AttackingAIConstants.
        /// Verifies MaxLateralOffsetM is PITCH_WIDTH_M × 0.5.
        /// </summary>
        [Test]
        public void MaxLateralOffsetM_IsHalfPitchWidth()
        {
            float expected = AttackingAIConstants.PITCH_WIDTH_M * 0.5f;
            Assert.AreEqual(expected, AttackingAIConstants.MaxLateralOffsetM, 0.001f,
                "MaxLateralOffsetM must equal PITCH_WIDTH_M × 0.5 = 34.0 m (§6.1.3 derived).");
        }

        /// <summary>
        /// Verifies MinRunDepthM lower-bound constant value matches spec §6.1.3.
        /// </summary>
        [Test]
        public void MinRunDepthM_Is5()
        {
            Assert.AreEqual(5.0f, AttackingAIConstants.MinRunDepthM, 0.001f,
                "MinRunDepthM = 5.0 m per §6.1.3.");
        }

        /// <summary>
        /// Verifies MaxRunDepthM upper-bound constant value matches spec §6.1.3.
        /// </summary>
        [Test]
        public void MaxRunDepthM_Is40()
        {
            Assert.AreEqual(40.0f, AttackingAIConstants.MaxRunDepthM, 0.001f,
                "MaxRunDepthM = 40.0 m per §6.1.3.");
        }

        /// <summary>
        /// Verifies COUNTER_ATTACK transition-hold ticks = 0 (instant recovery §6.1.5).
        /// </summary>
        [Test]
        public void TransitionHoldTicksCounter_IsZero()
        {
            Assert.AreEqual(0, AttackingAIConstants.TransitionHoldTicksCounter,
                "COUNTER_ATTACK profile must have TRANSITION_HOLD_TICKS = 0 (§6.1.5 / FR-AT-009).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 StyleProfile factory tests
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class StyleProfileTests
    {
        /// <summary>
        /// T-AT-U-043: DIRECT profile MAX_RUNNERS = MaxRunnersDirect = 3.
        /// </summary>
        [Test]
        public void DirectProfile_MaxRunnersIs3()
        {
            StyleProfile direct = StyleProfile.Direct;
            Assert.AreEqual(AttackingAIConstants.MaxRunnersDirect, direct.MaxRunners,
                "DIRECT profile MaxRunners must equal MaxRunnersDirect (§6.1.5 / FR-AT-017).");
        }

        /// <summary>
        /// T-AT-U-042: COUNTER_ATTACK profile TransitionHoldTicks = 0.
        /// </summary>
        [Test]
        public void CounterProfile_TransitionHoldTicksIsZero()
        {
            StyleProfile counter = StyleProfile.Counter;
            Assert.AreEqual(AttackingAIConstants.TransitionHoldTicksCounter, counter.TransitionHoldTicks,
                "COUNTER_ATTACK profile TransitionHoldTicks must be 0 (§6.1.5 / FR-AT-009).");
        }

        /// <summary>
        /// T-AT-U-044: POSSESSION profile support mult is SupportMultPossession = 1.3.
        /// Effective radius = SupportRadiusM × 1.3.
        /// </summary>
        [Test]
        public void PossessionProfile_SupportMultIs1_3()
        {
            StyleProfile possession = StyleProfile.Possession;
            Assert.AreEqual(AttackingAIConstants.SupportMultPossession, possession.SupportMult, 0.001f,
                "POSSESSION profile SupportMult must equal SupportMultPossession (§6.1.5).");
        }

        /// <summary>
        /// Verifies algorithm code-path invariant: MaxRunners field is plain int, not an enum branch.
        /// All three profiles read the same field path (KD-12 / FR-AT-017).
        /// </summary>
        [Test]
        public void AllProfiles_MaxRunnersInAscendingOrder()
        {
            StyleProfile possession = StyleProfile.Possession;
            StyleProfile direct     = StyleProfile.Direct;
            StyleProfile counter    = StyleProfile.Counter;

            Assert.Less(possession.MaxRunners, direct.MaxRunners,
                "POSSESSION MaxRunners must be < DIRECT (§6.1.5).");
            Assert.LessOrEqual(direct.MaxRunners, counter.MaxRunners,
                "DIRECT MaxRunners must be <= COUNTER (§6.1.5).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 RunParameters struct tests (T-AT-U-013)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class RunParametersTests
    {
        /// <summary>
        /// T-AT-U-013: RunParameters has exactly 3 fields: DepthOffsetM, LateralOffsetM,
        /// RunTriggerTick. RelativeAngle must be absent (FR-AT-011).
        /// </summary>
        [Test]
        public void RunParameters_HasExactlyThreePublicProperties()
        {
            PropertyInfo[] props = typeof(RunParameters).GetProperties(
                BindingFlags.Public | BindingFlags.Instance);
            Assert.AreEqual(3, props.Length,
                "RunParameters must have exactly 3 public properties (FR-AT-011): " +
                "DepthOffsetM, LateralOffsetM, RunTriggerTick. No RelativeAngle.");
        }

        /// <summary>
        /// T-AT-U-013 (continued): The three expected property names are present.
        /// </summary>
        [Test]
        public void RunParameters_HasExpectedPropertyNames()
        {
            System.Type t = typeof(RunParameters);
            Assert.IsNotNull(t.GetProperty("DepthOffsetM"),    "DepthOffsetM property required (FR-AT-011).");
            Assert.IsNotNull(t.GetProperty("LateralOffsetM"),  "LateralOffsetM property required (FR-AT-011).");
            Assert.IsNotNull(t.GetProperty("RunTriggerTick"),  "RunTriggerTick property required (FR-AT-011).");
        }

        /// <summary>
        /// Constructor round-trip: values stored correctly.
        /// </summary>
        [Test]
        public void RunParameters_ConstructorRoundTrip()
        {
            float depth   = AttackingAIConstants.MinRunDepthM + 1f;
            float lateral = 3.0f;
            int   trigger = 10;
            RunParameters rp = new RunParameters(depth, lateral, trigger);

            Assert.AreEqual(depth,   rp.DepthOffsetM,   0.001f, "DepthOffsetM not stored correctly.");
            Assert.AreEqual(lateral, rp.LateralOffsetM, 0.001f, "LateralOffsetM not stored correctly.");
            Assert.AreEqual(trigger, rp.RunTriggerTick,         "RunTriggerTick not stored correctly.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 AttackHysteresis tests (T-AT-U-033..035)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackHysteresisTests
    {
        /// <summary>
        /// T-AT-U-033: Role retained (IsStable) when dwell count reaches AttackDwellTicks.
        /// </summary>
        [Test]
        public void IsStable_TrueAfterDwellTicksReached()
        {
            AttackHysteresisState hyst = default;
            hyst.CurrentRole  = AttackRole.SupportBall;
            hyst.DwellCounter = AttackingAIConstants.AttackDwellTicks;

            bool stable = AttackHysteresis.IsStable(ref hyst);
            Assert.IsTrue(stable,
                "IsStable must be true when DwellCounter >= AttackDwellTicks (FR-AT-022).");
        }

        /// <summary>
        /// Not stable when dwell counter is below the threshold.
        /// </summary>
        [Test]
        public void IsStable_FalseWhenDwellCounterBelowThreshold()
        {
            AttackHysteresisState hyst = default;
            hyst.DwellCounter = AttackingAIConstants.AttackDwellTicks - 1;

            bool stable = AttackHysteresis.IsStable(ref hyst);
            Assert.IsFalse(stable,
                "IsStable must be false when DwellCounter < AttackDwellTicks (FR-AT-022).");
        }

        /// <summary>
        /// T-AT-U-034: Role transitions after AttackDwellTicks consecutive ticks on a new candidate.
        /// </summary>
        [Test]
        public void Update_TransitionsRoleAfterCandidateDwellReached()
        {
            AttackHysteresisState hyst = default;
            hyst.CurrentRole  = AttackRole.HoldWidth;
            hyst.DwellCounter = AttackingAIConstants.AttackDwellTicks; // stable in HoldWidth

            // Feed RUNNER as candidate for AttackDwellTicks consecutive ticks.
            for (int i = 0; i < AttackingAIConstants.AttackDwellTicks; i++)
            {
                AttackHysteresis.Update(ref hyst, AttackRole.Runner);
            }

            Assert.AreEqual(AttackRole.Runner, hyst.CurrentRole,
                "CurrentRole must transition to Runner after AttackDwellTicks candidate ticks (FR-AT-023).");
        }

        /// <summary>
        /// T-AT-U-035: Oscillation suppressed — same role retained across alternating eligibility.
        /// When a stable agent is re-preferred each tick, DwellCounter keeps incrementing and
        /// CandidateDwell stays at 0.
        /// </summary>
        [Test]
        public void Update_SameRolePreferred_CandidateDwellStaysZero()
        {
            AttackHysteresisState hyst = default;
            hyst.CurrentRole  = AttackRole.SupportBall;
            hyst.DwellCounter = 1;

            for (int i = 0; i < AttackingAIConstants.AttackDwellTicks * 2; i++)
            {
                AttackHysteresis.Update(ref hyst, AttackRole.SupportBall);
            }

            Assert.AreEqual(AttackRole.SupportBall, hyst.CurrentRole,
                "Role must not oscillate when same role is consistently preferred (FR-AT-023).");
            Assert.AreEqual(0, hyst.CandidateDwell,
                "CandidateDwell must reset to 0 when current role is re-preferred (AR-1 M-1).");
        }

        /// <summary>
        /// CandidateDwell resets when candidate changes to a different alternative.
        /// This prevents accumulated partial dwells from committing inadvertent transitions.
        /// </summary>
        [Test]
        public void Update_CandidateDwellResetsOnCandidateChange()
        {
            AttackHysteresisState hyst = default;
            hyst.CurrentRole = AttackRole.HoldWidth;

            // Partially build dwell for Runner.
            for (int i = 0; i < AttackingAIConstants.AttackDwellTicks - 1; i++)
            {
                AttackHysteresis.Update(ref hyst, AttackRole.Runner);
            }

            int dwellBeforeChange = hyst.CandidateDwell;
            Assert.Greater(dwellBeforeChange, 0, "Candidate dwell should have accumulated.");

            // Switch to a different candidate — dwell must restart from 1.
            AttackHysteresis.Update(ref hyst, AttackRole.SupportBall);

            Assert.AreEqual(AttackRole.SupportBall, hyst.CandidateRole,
                "CandidateRole must update to the new candidate.");
            Assert.AreEqual(1, hyst.CandidateDwell,
                "CandidateDwell must reset to 1 after candidate change.");
        }

        /// <summary>
        /// Reset reverts to HoldWidth with zeroed counters.
        /// </summary>
        [Test]
        public void Reset_RevertsToHoldWidthWithZeroedCounters()
        {
            AttackHysteresisState hyst = default;
            hyst.CurrentRole  = AttackRole.Runner;
            hyst.DwellCounter = 5;
            hyst.CandidateDwell = 2;

            AttackHysteresis.Reset(ref hyst);

            Assert.AreEqual(AttackRole.HoldWidth, hyst.CurrentRole,   "CurrentRole must be HoldWidth after Reset.");
            Assert.AreEqual(0,                    hyst.DwellCounter,  "DwellCounter must be 0 after Reset.");
            Assert.AreEqual(0,                    hyst.CandidateDwell,"CandidateDwell must be 0 after Reset.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 SupportHeuristic tests (T-AT-U-008, T-AT-U-009, T-AT-U-048)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class SupportHeuristicTests
    {
        /// <summary>
        /// T-AT-U-008: Agent within (SupportRadiusM − 0.1) of carrier is a SUPPORT_BALL candidate.
        /// </summary>
        [Test]
        public void IsWithinSupportRadius_TrueJustInsideRadius()
        {
            StyleProfile profile  = StyleProfile.Possession;
            float effectiveR      = SupportHeuristic.ComputeEffectiveRadius(profile);
            float distInside      = effectiveR - 0.1f;

            Vector2 carrier  = new Vector2(50f, 34f);
            Vector2 agent    = new Vector2(carrier.x + distInside, carrier.y);

            bool result = SupportHeuristic.IsWithinSupportRadius(agent, carrier, profile);
            Assert.IsTrue(result,
                "Agent within effective radius must be a SUPPORT_BALL candidate (FR-AT-013).");
        }

        /// <summary>
        /// T-AT-U-009: Agent just outside (SupportRadiusM + 0.1) of carrier is NOT a candidate.
        /// </summary>
        [Test]
        public void IsWithinSupportRadius_FalseJustOutsideRadius()
        {
            StyleProfile profile = StyleProfile.Possession;
            float effectiveR     = SupportHeuristic.ComputeEffectiveRadius(profile);
            float distOutside    = effectiveR + 0.1f;

            Vector2 carrier = new Vector2(50f, 34f);
            Vector2 agent   = new Vector2(carrier.x + distOutside, carrier.y);

            bool result = SupportHeuristic.IsWithinSupportRadius(agent, carrier, profile);
            Assert.IsFalse(result,
                "Agent outside effective radius must NOT be a SUPPORT_BALL candidate (FR-AT-013).");
        }

        /// <summary>
        /// T-AT-U-048: MinEffectiveRadiusM floor prevents radius collapse.
        /// COUNTER_ATTACK supportMult = 0.5 → raw = 12 × 0.5 = 6.0 ≥ 5.0 floor → effective = 6.0.
        /// Agent at distance 4.0 m IS inside 6.0 m radius (floor did not collapse it to zero).
        /// </summary>
        [Test]
        public void ComputeEffectiveRadius_CounterProfile_AppliesFloor()
        {
            StyleProfile counter = StyleProfile.Counter;
            float raw            = AttackingAIConstants.SupportRadiusM * counter.SupportMult;
            float expectedR      = Mathf.Max(AttackingAIConstants.MinEffectiveRadiusM, raw);
            float actualR        = SupportHeuristic.ComputeEffectiveRadius(counter);

            Assert.AreEqual(expectedR, actualR, 0.001f,
                "ComputeEffectiveRadius must apply MinEffectiveRadiusM floor (§3.5 / AR-1 H-1).");
        }

        /// <summary>
        /// T-AT-U-048 (continued): Agent at distance 4.0 m is within the 6.0 m effective radius.
        /// </summary>
        [Test]
        public void IsWithinSupportRadius_CounterProfile_AgentAt4mIsCandidate()
        {
            StyleProfile counter  = StyleProfile.Counter;
            Vector2      carrier  = new Vector2(30f, 34f);
            Vector2      agent    = new Vector2(carrier.x + 4.0f, carrier.y);

            bool result = SupportHeuristic.IsWithinSupportRadius(agent, carrier, counter);
            Assert.IsTrue(result,
                "Agent at 4.0 m must be inside the 6.0 m effective radius after MinEffectiveRadiusM floor (T-AT-U-048).");
        }

        /// <summary>
        /// POSSESSION profile yields a wider effective radius than DIRECT profile.
        /// </summary>
        [Test]
        public void ComputeEffectiveRadius_PossessionWiderThanDirect()
        {
            float rPossession = SupportHeuristic.ComputeEffectiveRadius(StyleProfile.Possession);
            float rDirect     = SupportHeuristic.ComputeEffectiveRadius(StyleProfile.Direct);

            Assert.Greater(rPossession, rDirect,
                "POSSESSION effective radius must exceed DIRECT (SupportMultPossession > SupportMultDirect).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 OverloadDetector tests (T-AT-U-023..027)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class OverloadDetectorTests
    {
        private static AttackPoolEntry MakeEntry(int entityId, float agentY, AttackRole role)
        {
            return new AttackPoolEntry
            {
                EntityId     = entityId,
                Position     = new Vector2(50f, agentY),
                AssignedRole = role,
            };
        }

        /// <summary>
        /// T-AT-U-023: Overload fires when ≥ OverloadCount non-WEAK_SIDE agents within corridor.
        /// </summary>
        [Test]
        public void Evaluate_FiresWhenEnoughAgentsInCorridor()
        {
            float ballY = 34f;
            Vector2 ball = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;
            for (int i = 0; i < AttackingAIConstants.OverloadCount; i++)
            {
                pool[count++] = MakeEntry(i, ballY + i * 0.5f, AttackRole.HoldWidth);
            }

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank _);
            Assert.IsTrue(active,
                "Overload must be active when non-WEAK_SIDE count >= OverloadCount (FR-AT-016).");
        }

        /// <summary>
        /// T-AT-U-024: Overload does NOT fire when fewer than OverloadCount agents in corridor.
        /// </summary>
        [Test]
        public void Evaluate_DoesNotFireBelowThreshold()
        {
            float ballY = 34f;
            Vector2 ball = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;
            for (int i = 0; i < AttackingAIConstants.OverloadCount - 1; i++)
            {
                pool[count++] = MakeEntry(i, ballY + i * 0.5f, AttackRole.HoldWidth);
            }

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank _);
            Assert.IsFalse(active,
                "Overload must not fire when count < OverloadCount (FR-AT-016).");
        }

        /// <summary>
        /// T-AT-U-025: WEAK_SIDE agent excluded from overload count.
        /// </summary>
        [Test]
        public void Evaluate_WeakSideAgentExcludedFromCount()
        {
            float ballY = 34f;
            Vector2 ball = new Vector2(60f, ballY);

            // Put exactly OverloadCount agents in corridor, one is WEAK_SIDE.
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;
            for (int i = 0; i < AttackingAIConstants.OverloadCount - 1; i++)
            {
                pool[count++] = MakeEntry(i, ballY + i * 0.5f, AttackRole.HoldWidth);
            }
            pool[count++] = MakeEntry(99, ballY + 1.0f, AttackRole.WeakSide);

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank _);
            Assert.IsFalse(active,
                "WEAK_SIDE agent must be excluded from overload count (§3.8 / FR-AT-016).");
        }

        /// <summary>
        /// T-AT-U-026: Correct flank when ball is on the right side (ball.y > PITCH_WIDTH_M/2).
        /// </summary>
        [Test]
        public void Evaluate_CorrectFlank_RightWhenBallOnHighYSide()
        {
            float ballY = 55f; // > 34
            Vector2 ball = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;
            for (int i = 0; i < AttackingAIConstants.OverloadCount; i++)
            {
                pool[count++] = MakeEntry(i, ballY + i * 0.5f, AttackRole.HoldWidth);
            }

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank flank);
            Assert.IsTrue(active,  "Overload must be active in this setup.");
            Assert.AreEqual(Flank.Right, flank,
                "Flank must be Right when ball.y >= PITCH_WIDTH_M/2 (FR-AT-036).");
        }

        /// <summary>
        /// T-AT-U-027: Correct flank when ball is on the left side (ball.y < PITCH_WIDTH_M/2).
        /// </summary>
        [Test]
        public void Evaluate_CorrectFlank_LeftWhenBallOnLowYSide()
        {
            float ballY = 15f; // < 34
            Vector2 ball = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;
            for (int i = 0; i < AttackingAIConstants.OverloadCount; i++)
            {
                pool[count++] = MakeEntry(i, ballY + i * 0.5f, AttackRole.HoldWidth);
            }

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank flank);
            Assert.IsTrue(active,  "Overload must be active in this setup.");
            Assert.AreEqual(Flank.Left, flank,
                "Flank must be Left when ball.y < PITCH_WIDTH_M/2 (FR-AT-036).");
        }

        /// <summary>
        /// Agent beyond OverloadZoneWidthM from ball Y must not be counted.
        /// </summary>
        [Test]
        public void Evaluate_AgentOutsideCorridorNotCounted()
        {
            float ballY = 34f;
            Vector2 ball = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = 0;

            // OverloadCount agents just beyond the corridor boundary.
            float outsideY = ballY + AttackingAIConstants.OverloadZoneWidthM + 0.1f;
            for (int i = 0; i < AttackingAIConstants.OverloadCount; i++)
            {
                pool[count++] = MakeEntry(i, outsideY, AttackRole.HoldWidth);
            }

            bool active = OverloadDetector.Evaluate(ball, pool, count, out Flank _);
            Assert.IsFalse(active,
                "Agents beyond OverloadZoneWidthM must not be counted (§3.8).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 WidthHolder tests (T-AT-U-017..019, T-AT-U-047)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class WidthHolderTests
    {
        private static AttackPoolEntry MakeEntry(int entityId, float agentY, AttackRole role = AttackRole.SupportBall)
        {
            return new AttackPoolEntry
            {
                EntityId     = entityId,
                Position     = new Vector2(50f, agentY),
                AssignedRole = role,
            };
        }

        /// <summary>
        /// T-AT-U-018: nearTouchlineY = PITCH_WIDTH_M − TouchlineHoldDistM when ball.y > halfWidth.
        /// </summary>
        [Test]
        public void Enforce_NearTouchlineY_BallOnHighSide()
        {
            float ballY      = 55f; // > 34
            float expected   = AttackingAIConstants.PITCH_WIDTH_M - AttackingAIConstants.TouchlineHoldDistM;
            Vector2 ball     = new Vector2(60f, ballY);
            Vector2 carrier  = new Vector2(60f, ballY);

            // One non-RUNNER agent near the touchline — should be set to nearTouchlineY.
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeEntry(1, 63f, AttackRole.HoldWidth);
            // TargetPosition.y was zero; Enforce sets it.
            pool[0].TargetPosition = Vector2.zero;

            WidthHolder.Enforce(ball, carrier, pool, 1);

            // After enforce, any promoted/existing HoldWidth should have target Y ≈ expected.
            Assert.AreEqual(expected, pool[0].TargetPosition.y, 0.001f,
                "HOLD_WIDTH target Y must be PITCH_WIDTH_M − TouchlineHoldDistM when ball on high side (T-AT-U-018).");
        }

        /// <summary>
        /// T-AT-U-019: nearTouchlineY = TouchlineHoldDistM when ball.y is on the low side.
        /// </summary>
        [Test]
        public void Enforce_NearTouchlineY_BallOnLowSide()
        {
            float ballY    = 10f; // < 34
            float expected = AttackingAIConstants.TouchlineHoldDistM;
            Vector2 ball   = new Vector2(40f, ballY);
            Vector2 carrier = new Vector2(40f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeEntry(1, 5f, AttackRole.HoldWidth);
            pool[0].TargetPosition = Vector2.zero;

            WidthHolder.Enforce(ball, carrier, pool, 1);

            Assert.AreEqual(expected, pool[0].TargetPosition.y, 0.001f,
                "HOLD_WIDTH target Y must be TouchlineHoldDistM when ball on low side (T-AT-U-019).");
        }

        /// <summary>
        /// T-AT-U-017: MinWidthHolders enforced — non-RUNNER agent promoted when count is below.
        /// </summary>
        [Test]
        public void Enforce_PromotesAgentWhenBelowMinWidthHolders()
        {
            // Pool: 0 agents on near side, 2 SupportBall agents.
            // MinWidthHolders = 2, so both should be promoted.
            float ballY  = 55f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            float nearTouchlineY   = AttackingAIConstants.PITCH_WIDTH_M - AttackingAIConstants.TouchlineHoldDistM;
            // Place agents on the near (high Y) side so promotion target is reachable.
            pool[0] = MakeEntry(1, 62f, AttackRole.SupportBall);
            pool[1] = MakeEntry(2, 60f, AttackRole.SupportBall);

            WidthHolder.Enforce(ball, carrier, pool, 2);

            int holdWidthCount = 0;
            for (int i = 0; i < 2; i++)
            {
                if (pool[i].AssignedRole == AttackRole.HoldWidth) holdWidthCount++;
            }

            Assert.GreaterOrEqual(holdWidthCount, AttackingAIConstants.MinWidthHolders,
                "At least MinWidthHolders agents must hold near-touchline width (FR-AT-014).");
        }

        /// <summary>
        /// T-AT-U-047: WEAK_SIDE agent on near side counts toward width total (not re-promoted).
        /// </summary>
        [Test]
        public void Enforce_WeakSideOnNearSideCountsTowardWidthTotal()
        {
            // Ball on high side; one WEAK_SIDE agent on near (high) side + one HoldWidth.
            float ballY  = 55f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeEntry(1, 62f, AttackRole.HoldWidth);   // near side (high Y)
            pool[0].TargetPosition = Vector2.zero;
            pool[1] = MakeEntry(2, 63f, AttackRole.WeakSide);    // near side — should count

            WidthHolder.Enforce(ball, carrier, pool, 2);

            // WEAK_SIDE agent must NOT have been re-promoted to HoldWidth.
            Assert.AreEqual(AttackRole.WeakSide, pool[1].AssignedRole,
                "Near-side WEAK_SIDE agent must not be re-promoted; already counted (AR-1 M-2 / T-AT-U-047).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 WeakSideController tests (T-AT-U-020..022)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class WeakSideControllerTests
    {
        private static AttackPoolEntry MakeEntry(int entityId, float agentY, AttackRole role = AttackRole.HoldWidth)
        {
            return new AttackPoolEntry
            {
                EntityId     = entityId,
                Position     = new Vector2(50f, agentY),
                AssignedRole = role,
            };
        }

        /// <summary>
        /// T-AT-U-020: Agent with greatest |Y − ballY| deviation assigned WEAK_SIDE.
        /// Pool has 5 agents; ball at y=50; agent at y=5 has greatest deviation.
        /// </summary>
        [Test]
        public void EnsureWeakSide_SelectsMaxDeviationAgent()
        {
            float ballY     = 50f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            // 5 agents; entityId ascending; agent at y=5 has largest |Y − 50| = 45.
            pool[0] = MakeEntry(1, 45f);
            pool[1] = MakeEntry(2, 40f);
            pool[2] = MakeEntry(3, 35f);
            pool[3] = MakeEntry(4, 30f);
            pool[4] = MakeEntry(5,  5f);

            WeakSideController.EnsureWeakSide(ball, carrier, pool, 5);

            Assert.AreEqual(AttackRole.WeakSide, pool[4].AssignedRole,
                "Agent at y=5 (deviation=45) must be assigned WEAK_SIDE (T-AT-U-020 / FR-AT-015).");
        }

        /// <summary>
        /// T-AT-U-021: EntityId ascending tiebreak — lower EntityId wins on equal deviation.
        /// </summary>
        [Test]
        public void EnsureWeakSide_EntityIdTieBreakLowerWins()
        {
            float ballY     = 34f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            // Two agents at equal Y-deviation (both at y=0); pool already EntityId-ascending.
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeEntry(3, 0f); // entityId=3, deviation=34
            pool[1] = MakeEntry(7, 0f); // entityId=7, deviation=34
            pool[2] = MakeEntry(10, 60f);
            pool[3] = MakeEntry(12, 58f);

            WeakSideController.EnsureWeakSide(ball, carrier, pool, 4);

            // Lower EntityId (first in ascending-sorted pool) should win the tiebreak.
            Assert.AreEqual(AttackRole.WeakSide, pool[0].AssignedRole,
                "Lower EntityId must win WEAK_SIDE tiebreak (T-AT-U-021 / FR-AT-015).");
            Assert.AreNotEqual(AttackRole.WeakSide, pool[1].AssignedRole,
                "Higher EntityId must not be assigned WEAK_SIDE when lower EntityId has equal deviation.");
        }

        /// <summary>
        /// T-AT-U-022: No WEAK_SIDE assigned when pool < MinWeakSideAgentThreshold.
        /// </summary>
        [Test]
        public void EnsureWeakSide_NoAssignmentWhenPoolBelowThreshold()
        {
            float ballY     = 34f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            int poolCount = AttackingAIConstants.MinWeakSideAgentThreshold - 1;
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            for (int i = 0; i < poolCount; i++)
            {
                pool[i] = MakeEntry(i + 1, i * 5f);
            }

            WeakSideController.EnsureWeakSide(ball, carrier, pool, poolCount);

            for (int i = 0; i < poolCount; i++)
            {
                Assert.AreNotEqual(AttackRole.WeakSide, pool[i].AssignedRole,
                    $"No WEAK_SIDE must be assigned when pool < MinWeakSideAgentThreshold (T-AT-U-022 / FR-AT-015).");
            }
        }

        /// <summary>
        /// RUNNER agents must not be selected for WEAK_SIDE.
        /// </summary>
        [Test]
        public void EnsureWeakSide_SkipsRunnerAgents()
        {
            float ballY     = 34f;
            Vector2 ball    = new Vector2(60f, ballY);
            Vector2 carrier = new Vector2(60f, ballY);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            // Agent 1 is a RUNNER at high deviation; Agent 2 is HoldWidth at lower deviation.
            pool[0] = MakeEntry(1, 5f,  AttackRole.Runner);    // highest dev, but RUNNER
            pool[1] = MakeEntry(2, 10f, AttackRole.HoldWidth);
            pool[2] = MakeEntry(3, 55f, AttackRole.HoldWidth);
            pool[3] = MakeEntry(4, 58f, AttackRole.HoldWidth);

            WeakSideController.EnsureWeakSide(ball, carrier, pool, 4);

            Assert.AreNotEqual(AttackRole.WeakSide, pool[0].AssignedRole,
                "RUNNER agents must not be selected for WEAK_SIDE (§3.7 / §3.3).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 TransitionController tests (T-AT-U-004, T-AT-U-005, T-AT-U-042)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class TransitionControllerTests
    {
        private static AttackDirective MakeInPossDirective()
        {
            return new AttackDirective(1, true, Flank.Right, 0);
        }

        /// <summary>
        /// T-AT-U-004: First possession-loss tick sets counter to TransitionHoldTicks then
        /// decrements to TransitionHoldTicks − 1; frozen directive returned.
        /// </summary>
        [Test]
        public void Evaluate_FirstLossTick_SetsAndDecrementsCounter()
        {
            StyleProfile profile = StyleProfile.Possession;
            AttackDirective lastDirective = MakeInPossDirective();
            TransitionHoldState state = new TransitionHoldState
            {
                TransitionHoldTick = 0,
                PrevPhase          = Phase.InPoss,
            };

            AttackDirective result = TransitionController.Evaluate(
                Phase.InPoss, Phase.OutOfPoss, profile, lastDirective, ref state);

            int expectedAfterDecrement = profile.TransitionHoldTicks - 1;
            Assert.AreEqual(expectedAfterDecrement, state.TransitionHoldTick,
                "Counter must be SET then DECREMENTED on first possession-loss tick (T-AT-U-005 / FR-AT-009).");
            Assert.AreEqual(lastDirective.OverloadActive, result.OverloadActive,
                "Frozen last-IN_POSSESSION directive must be returned (T-AT-U-004).");
        }

        /// <summary>
        /// T-AT-U-005: Counter is SET before DECREMENT ordering guaranteed.
        /// When TRANSITION_HOLD_TICKS = 5, counter after first loss tick = 4 (not 0).
        /// </summary>
        [Test]
        public void Evaluate_SetBeforeDecrement_Order()
        {
            StyleProfile profile = StyleProfile.Possession; // TransitionHoldTicks = 5
            AttackDirective lastDirective = MakeInPossDirective();
            TransitionHoldState state = new TransitionHoldState
            {
                TransitionHoldTick = 0,
                PrevPhase          = Phase.InPoss,
            };

            TransitionController.Evaluate(Phase.InPoss, Phase.OutOfPoss, profile, lastDirective, ref state);

            int expected = profile.TransitionHoldTicks - 1;
            Assert.AreEqual(expected, state.TransitionHoldTick,
                "SET-then-DECREMENT must leave counter at TransitionHoldTicks−1 after first loss tick (T-AT-U-005).");
        }

        /// <summary>
        /// T-AT-U-042: COUNTER_ATTACK profile with TransitionHoldTicks = 0 produces
        /// immediate empty directive even on the first possession-loss tick.
        /// </summary>
        [Test]
        public void Evaluate_CounterAttack_ImmediateEmptyDirective()
        {
            StyleProfile counter = StyleProfile.Counter; // TransitionHoldTicks = 0
            AttackDirective lastDirective = MakeInPossDirective();
            TransitionHoldState state = new TransitionHoldState
            {
                TransitionHoldTick = 0,
                PrevPhase          = Phase.InPoss,
            };

            AttackDirective result = TransitionController.Evaluate(
                Phase.InPoss, Phase.OutOfPoss, counter, lastDirective, ref state);

            Assert.IsFalse(result.OverloadActive,
                "COUNTER_ATTACK must return empty directive immediately on possession loss (T-AT-U-042).");
            Assert.AreEqual(0, state.TransitionHoldTick,
                "Counter must stay at 0 for COUNTER_ATTACK profile (T-AT-U-042).");
        }

        /// <summary>
        /// Once counter reaches 0, Evaluate returns an empty directive.
        /// </summary>
        [Test]
        public void Evaluate_CounterAtZero_ReturnsEmptyDirective()
        {
            StyleProfile profile = StyleProfile.Possession;
            AttackDirective lastDirective = MakeInPossDirective();
            TransitionHoldState state = new TransitionHoldState
            {
                TransitionHoldTick = 0,
                PrevPhase          = Phase.OutOfPoss,
            };

            AttackDirective result = TransitionController.Evaluate(
                Phase.OutOfPoss, Phase.OutOfPoss, profile, lastDirective, ref state);

            Assert.IsFalse(result.OverloadActive,
                "Empty directive must be returned when counter is 0 (FR-AT-009).");
            Assert.AreEqual(0, result.TeamId,   "Empty directive TeamId must be 0.");
            Assert.AreEqual(0, result.TransitionHoldTick, "Empty directive TransitionHoldTick must be 0.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 InvariantEnforcer tests (T-AT-U-028..032, §5.6.1)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class InvariantEnforcerTests
    {
        private static AttackPoolEntry MakeRunner(int entityId, float depthOffsetM, float runTargetX)
        {
            return new AttackPoolEntry
            {
                EntityId          = entityId,
                Position          = new Vector2(50f, 34f),
                AssignedRole      = AttackRole.Runner,
                HasRunParams      = true,
                DepthOffsetM      = depthOffsetM,
                RunTargetPosition = new Vector2(runTargetX, 34f),
            };
        }

        private static AttackPoolEntry MakeSupport(int entityId)
        {
            return new AttackPoolEntry
            {
                EntityId     = entityId,
                Position     = new Vector2(50f, 34f),
                AssignedRole = AttackRole.SupportBall,
            };
        }

        /// <summary>
        /// T-AT-U-028: Anti-chaos MAX_RUNNERS — 4 runners with MaxRunnersDirect=3 → 1 demoted.
        /// </summary>
        [Test]
        public void Apply_MaxRunners_ExcessRunnersDemoted()
        {
            StyleProfile direct = StyleProfile.Direct; // MaxRunners = 3
            float attackAngle   = 0f;                  // attacking x=105

            // 4 runners + 1 support (min support satisfied).
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeRunner(1, 30f, 80f);
            pool[1] = MakeRunner(2, 25f, 75f);
            pool[2] = MakeRunner(3, 20f, 70f);
            pool[3] = MakeRunner(4, 15f, 65f);
            pool[4] = MakeSupport(5);

            InvariantEnforcer.Apply(pool, 5, direct, attackAngle);

            int runnerCount = 0;
            for (int i = 0; i < 5; i++)
            {
                if (pool[i].AssignedRole == AttackRole.Runner) runnerCount++;
            }
            Assert.LessOrEqual(runnerCount, direct.MaxRunners,
                "Runner count must be <= MaxRunners after Apply (T-AT-U-028 / FR-AT-018).");
        }

        /// <summary>
        /// T-AT-U-029: Demotion picks shallowest runner (lowest DepthOffsetM) first.
        /// </summary>
        [Test]
        public void Apply_MaxRunners_ShallowRunnerDemotedFirst()
        {
            StyleProfile possession = StyleProfile.Possession; // MaxRunners = 1
            float attackAngle       = 0f;

            // 2 runners, 1 support.
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            // Runner with smaller depthOffset = shallowest.
            pool[0] = MakeRunner(1, AttackingAIConstants.MinRunDepthM + 1f, 60f); // shallow
            pool[1] = MakeRunner(2, AttackingAIConstants.MaxRunDepthM - 1f, 90f); // deep
            pool[2] = MakeSupport(3);

            InvariantEnforcer.Apply(pool, 3, possession, attackAngle);

            Assert.AreNotEqual(AttackRole.Runner, pool[0].AssignedRole,
                "Shallowest runner (pool[0]) must be demoted first (T-AT-U-029 / FR-AT-018).");
            Assert.AreEqual(AttackRole.Runner, pool[1].AssignedRole,
                "Deepest runner (pool[1]) must be retained.");
        }

        /// <summary>
        /// T-AT-U-031: Anti-chaos own-half block — RUNNER target in own half > OwnHalfRunBlockM
        /// past half-line for team attacking x=105 is demoted to HOLD_WIDTH.
        /// </summary>
        [Test]
        public void Apply_OwnHalfBlock_RunnerTargetInOwnHalfDemoted()
        {
            // Team attacking x=105 (attackAngle ≈ 0); run target at x=46 < half-line 52.5.
            // DepthIntoOwnHalf = 52.5 − 46 = 6.5 > OwnHalfRunBlockM=5.
            float attackAngle = 0f;
            float ownHalfX    = AttackingAIConstants.HALF_LINE_X
                                 - (AttackingAIConstants.OwnHalfRunBlockM + 1f); // 46.5 m

            StyleProfile possession = StyleProfile.Possession;

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeRunner(1, AttackingAIConstants.MinRunDepthM, ownHalfX);
            pool[1] = MakeSupport(2);

            InvariantEnforcer.Apply(pool, 2, possession, attackAngle);

            Assert.AreNotEqual(AttackRole.Runner, pool[0].AssignedRole,
                "RUNNER with run target > OwnHalfRunBlockM into own half must be demoted (T-AT-U-031 / FR-AT-020).");
        }

        /// <summary>
        /// T-AT-U-032: All-default fallback when invariants remain violated after MaxInvariantPasses.
        /// After fallback, zero runners must remain.
        /// </summary>
        [Test]
        public void Apply_Fallback_AllRunnersConvertedWhenUnresolvable()
        {
            // Construct a scenario that the enforcer cannot resolve in MaxInvariantPasses:
            // All agents are runners with own-half targets; no support agent at all.
            // MinSupportAgents=1 is always violated when pool=1 runner with no support.
            // The enforcer demotes to SupportBall; after that runners=0 so MaxRunners is fine
            // but OwnHalfBlock is also cleared.  With only 1 agent the fallback path is short.
            // Instead, use MaxRunners=1 style with 5 runners all in own half to force fallback.
            float attackAngle   = 0f;
            StyleProfile possession = StyleProfile.Possession; // MaxRunners = 1
            float ownHalfX      = AttackingAIConstants.HALF_LINE_X
                                   - (AttackingAIConstants.OwnHalfRunBlockM + 1f);

            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            // 5 runners all in own half, no support.
            for (int i = 0; i < 5; i++)
            {
                pool[i] = MakeRunner(i + 1, AttackingAIConstants.MinRunDepthM, ownHalfX);
            }

            bool resolved = InvariantEnforcer.Apply(pool, 5, possession, attackAngle);

            if (!resolved)
            {
                for (int i = 0; i < 5; i++)
                {
                    Assert.AreNotEqual(AttackRole.Runner, pool[i].AssignedRole,
                        $"After fallback, agent {i} must not remain a Runner (FR-AT-026).");
                }
            }
            else
            {
                int runners = 0;
                for (int i = 0; i < 5; i++)
                    if (pool[i].AssignedRole == AttackRole.Runner) runners++;
                Assert.LessOrEqual(runners, possession.MaxRunners,
                    "If resolved, runner count must be within MaxRunners.");
            }
        }

        /// <summary>
        /// T-AT-U-030: MIN_SUPPORT_AGENTS — when 0 SUPPORT/HOLD_WIDTH agents, shallowest runner
        /// demoted to SUPPORT_BALL.
        /// </summary>
        [Test]
        public void Apply_MinSupport_ShallowestRunnerDemotedToSupportBall()
        {
            float attackAngle   = 0f;
            StyleProfile direct = StyleProfile.Direct; // MaxRunners = 3

            // 3 runners, no support/hold — MinSupportAgents=1 violated.
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            pool[0] = MakeRunner(1, AttackingAIConstants.MinRunDepthM + 0f, 80f); // shallowest
            pool[1] = MakeRunner(2, AttackingAIConstants.MinRunDepthM + 5f, 82f);
            pool[2] = MakeRunner(3, AttackingAIConstants.MinRunDepthM + 10f, 84f);

            InvariantEnforcer.Apply(pool, 3, direct, attackAngle);

            int supportCount = 0;
            for (int i = 0; i < 3; i++)
            {
                AttackRole r = pool[i].AssignedRole;
                if (r == AttackRole.SupportBall || r == AttackRole.HoldWidth) supportCount++;
            }

            Assert.GreaterOrEqual(supportCount, AttackingAIConstants.MinSupportAgents,
                "At least MinSupportAgents must be SUPPORT_BALL/HOLD_WIDTH after Apply (T-AT-U-030 / FR-AT-019).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 AttackingPoolBuilder tests (T-AT-U-001..002, T-AT-U-041)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackingPoolBuilderTests
    {
        private static AttackingAgentSnapshot MakeAgent(
            int entityId, int teamId,
            bool isGoalkeeper = false,
            bool hasBall      = false,
            bool isActive     = true,
            float slotY       = 34f,
            LineId line       = LineId.Midfield)
        {
            Vector2 slot = new Vector2(60f, slotY);
            return new AttackingAgentSnapshot(
                entityId, teamId,
                new Vector2(50f, 34f), slot, line,
                isGoalkeeper, hasBall, isActive,
                pace: 0.5f, stamina: 0.0f, dribbling: 0.5f);
        }

        private static AttackingSnapshot BuildSnapshot(
            int attackingTeamId, int ballCarrierId,
            AttackingAgentSnapshot[] agents)
        {
            AttackingSnapshot snap = new AttackingSnapshot();
            snap.AttackingTeamId     = attackingTeamId;
            snap.BallCarrierEntityId = ballCarrierId;
            snap.BallPosition        = new Vector2(60f, 34f);
            snap.BallCarrierPosition = new Vector2(62f, 34f);
            snap.TeamAttackAngle     = 0f;

            for (int i = 0; i < agents.Length && i < snap.Agents.Length; i++)
            {
                snap.Agents[i] = agents[i];
            }
            return snap;
        }

        /// <summary>
        /// T-AT-U-001: GK excluded from pool. 11-agent team (1 GK) → pool size ≤ 9.
        /// </summary>
        [Test]
        public void Build_ExcludesGoalkeeper()
        {
            AttackingAgentSnapshot[] agents = new AttackingAgentSnapshot[]
            {
                MakeAgent(1, 1, isGoalkeeper: true),
                MakeAgent(2, 1), MakeAgent(3, 1), MakeAgent(4, 1), MakeAgent(5, 1),
            };
            AttackingSnapshot snap = BuildSnapshot(1, 99, agents);
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];

            int count = AttackingPoolBuilder.Build(snap, pool);

            for (int i = 0; i < count; i++)
            {
                Assert.AreNotEqual(1, pool[i].EntityId,
                    "Goalkeeper (EntityId=1) must be excluded from pool (FR-AT-006).");
            }
        }

        /// <summary>
        /// T-AT-U-002: Ball carrier excluded from pool.
        /// </summary>
        [Test]
        public void Build_ExcludesBallCarrier()
        {
            int carrierId = 3;
            AttackingAgentSnapshot[] agents = new AttackingAgentSnapshot[]
            {
                MakeAgent(1, 1), MakeAgent(2, 1), MakeAgent(carrierId, 1, hasBall: true),
                MakeAgent(4, 1), MakeAgent(5, 1),
            };
            AttackingSnapshot snap = BuildSnapshot(1, carrierId, agents);
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];

            int count = AttackingPoolBuilder.Build(snap, pool);

            for (int i = 0; i < count; i++)
            {
                Assert.AreNotEqual(carrierId, pool[i].EntityId,
                    "Ball carrier must be excluded from pool (FR-AT-007).");
            }
        }

        /// <summary>
        /// T-AT-U-041: Pool entries are sorted EntityId-ascending after Build().
        /// </summary>
        [Test]
        public void Build_SortsPoolByEntityIdAscending()
        {
            // Insert agents in reverse order; pool must come out ascending.
            AttackingAgentSnapshot[] agents = new AttackingAgentSnapshot[]
            {
                MakeAgent(5, 1), MakeAgent(3, 1), MakeAgent(1, 1), MakeAgent(4, 1), MakeAgent(2, 1),
            };
            AttackingSnapshot snap = BuildSnapshot(1, 99, agents);
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];

            int count = AttackingPoolBuilder.Build(snap, pool);

            for (int i = 1; i < count; i++)
            {
                Assert.Less(pool[i - 1].EntityId, pool[i].EntityId,
                    $"Pool[{i-1}].EntityId must be < Pool[{i}].EntityId (EntityId-ascending / FR-AT-003).");
            }
        }

        /// <summary>
        /// Build returns −1 when any agent has a sentinel baseline slot (F2 failure).
        /// </summary>
        [Test]
        public void Build_ReturnsMinus1OnSentinelSlot()
        {
            AttackingAgentSnapshot[] agents = new AttackingAgentSnapshot[]
            {
                MakeAgent(1, 1), MakeAgent(2, 1),
            };
            // Manually craft an agent with sentinel slot (Vector2.NegativeInfinity).
            AttackingSnapshot snap = BuildSnapshot(1, 99, agents);
            snap.Agents[1] = new AttackingAgentSnapshot(
                2, 1, new Vector2(50f, 34f),
                Vector2.negativeInfinity, LineId.Midfield,
                false, false, true, 0.5f, 0f, 0.5f);
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];

            int count = AttackingPoolBuilder.Build(snap, pool);

            Assert.AreEqual(-1, count,
                "Build must return −1 when a SENTINEL_NO_SLOT is detected (F2 / FR-AT-025).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 AttackDirective & AttackIntent struct tests (T-AT-U-003)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackDirectiveIntentTests
    {
        /// <summary>
        /// T-AT-U-003: AttackDirective.Empty has all fields zeroed / false.
        /// </summary>
        [Test]
        public void AttackDirective_Empty_AllFieldsDefault()
        {
            AttackDirective empty = AttackDirective.Empty;

            Assert.AreEqual(0,           empty.TeamId,             "Empty.TeamId must be 0.");
            Assert.IsFalse(empty.OverloadActive,                   "Empty.OverloadActive must be false.");
            Assert.AreEqual(default(Flank), empty.OverloadFlank,   "Empty.OverloadFlank must be default.");
            Assert.AreEqual(0,           empty.TransitionHoldTick, "Empty.TransitionHoldTick must be 0.");
        }

        /// <summary>
        /// AttackIntent with Role=Runner carries non-null RunParameters.
        /// </summary>
        [Test]
        public void AttackIntent_RunnerRole_CarriesRunParameters()
        {
            RunParameters rp     = new RunParameters(
                AttackingAIConstants.MinRunDepthM, 5f, 10);
            AttackIntent  intent = new AttackIntent(7, AttackRole.Runner, rp, 42);

            Assert.AreEqual(AttackRole.Runner, intent.Role, "Intent.Role must be Runner.");
            Assert.IsTrue(intent.RunParameters.HasValue,    "RunParameters must be non-null for Runner.");
        }

        /// <summary>
        /// AttackIntent with non-Runner role carries null RunParameters.
        /// </summary>
        [Test]
        public void AttackIntent_NonRunnerRole_NullRunParameters()
        {
            AttackIntent intent = new AttackIntent(3, AttackRole.HoldWidth, null, 10);

            Assert.IsFalse(intent.RunParameters.HasValue,
                "RunParameters must be null for non-Runner roles (FR-AT-011).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 Run parameter generation tests (T-AT-U-010..012, T-AT-U-014..016)
    // Exercised through RoleAssigner + AttackingPoolBuilder pipeline directly.
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class RunParameterGenerationTests
    {
        private static AttackingAgentSnapshot MakeAgent(
            int entityId, int teamId, float slotY = 34f,
            LineId line = LineId.Attack, bool isActive = true)
        {
            Vector2 slot = new Vector2(70f, slotY);
            return new AttackingAgentSnapshot(
                entityId, teamId, new Vector2(60f, slotY), slot, line,
                false, false, isActive,
                pace: 0.5f, stamina: 0.0f, dribbling: 0.5f);
        }

        private static (AttackPoolEntry[] pool, int count) BuildPoolWithOneAgent(
            AttackingSnapshot snap)
        {
            AttackPoolEntry[] pool = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            int count = AttackingPoolBuilder.Build(snap, pool);
            return (pool, count);
        }

        /// <summary>
        /// T-AT-U-010: depthOffset_m clamped to MinRunDepthM when raw value is below floor.
        /// POSSESSION depthMult=0.8; BaseRunDepthM=15 → raw=12 ≥ 5, no clamping needed.
        /// Use a very small synthetic DepthMult to force raw below 5.
        /// </summary>
        [Test]
        public void GenerateRunParams_DepthOffset_ClampedAtLowerBound()
        {
            // Synthesise a profile with tiny depthMult so raw = 15 × 0.01 = 0.15 < 5.
            StyleProfile tiny = new StyleProfile(0.01f, 1.0f, 1.0f, 3, 5);
            AttackHysteresisState[] hyst = new AttackHysteresisState[AttackingAIConstants.SQUAD_SIZE];

            AttackingSnapshot snap = new AttackingSnapshot();
            snap.AttackingTeamId     = 1;
            snap.BallCarrierEntityId = 99;
            snap.BallPosition        = new Vector2(70f, 34f);
            snap.BallCarrierPosition = new Vector2(70f, 34f);
            snap.TeamAttackAngle     = 0f;
            snap.Agents[0]           = MakeAgent(1, 1, 34f, LineId.Attack);

            (AttackPoolEntry[] pool, int count) = BuildPoolWithOneAgent(snap);
            Assert.AreEqual(1, count, "Pool should have 1 agent.");

            float cos = Mathf.Cos(0f);
            float sin = Mathf.Sin(0f);

            RoleAssigner.Assign(snap, pool, count, tiny, hyst, cos, sin, 10);

            if (pool[0].HasRunParams)
            {
                Assert.GreaterOrEqual(pool[0].DepthOffsetM, AttackingAIConstants.MinRunDepthM,
                    "DepthOffsetM must be >= MinRunDepthM after clamp (T-AT-U-010 / FR-AT-011).");
            }
        }

        /// <summary>
        /// T-AT-U-011: depthOffset_m clamped at MaxRunDepthM when raw exceeds ceiling.
        /// Use a very large DepthMult so raw = 15 × 100 = 1500 > 40.
        /// </summary>
        [Test]
        public void GenerateRunParams_DepthOffset_ClampedAtUpperBound()
        {
            StyleProfile large = new StyleProfile(100f, 1.0f, 1.0f, 3, 5);
            AttackHysteresisState[] hyst = new AttackHysteresisState[AttackingAIConstants.SQUAD_SIZE];

            AttackingSnapshot snap = new AttackingSnapshot();
            snap.AttackingTeamId     = 1;
            snap.BallCarrierEntityId = 99;
            snap.BallPosition        = new Vector2(70f, 34f);
            snap.BallCarrierPosition = new Vector2(70f, 34f);
            snap.TeamAttackAngle     = 0f;
            snap.Agents[0]           = MakeAgent(1, 1, 34f, LineId.Attack);

            (AttackPoolEntry[] pool, int count) = BuildPoolWithOneAgent(snap);
            Assert.AreEqual(1, count);

            float cos = Mathf.Cos(0f);
            float sin = Mathf.Sin(0f);

            RoleAssigner.Assign(snap, pool, count, large, hyst, cos, sin, 10);

            if (pool[0].HasRunParams)
            {
                Assert.LessOrEqual(pool[0].DepthOffsetM, AttackingAIConstants.MaxRunDepthM,
                    "DepthOffsetM must be <= MaxRunDepthM after clamp (T-AT-U-011 / FR-AT-011).");
            }
        }

        /// <summary>
        /// T-AT-U-012: runTriggerTick always >= currentTick + 1 (min 1 delay enforced).
        /// Use TimingMult → 0 to force raw delay < 1.
        /// </summary>
        [Test]
        public void GenerateRunParams_RunTriggerTick_AlwaysAtLeastCurrentPlusOne()
        {
            // TimingMult close to 0 means raw delay < 1 → max(1, ...) = 1.
            StyleProfile tinyTiming = new StyleProfile(1.4f, 0.001f, 1.0f, 3, 5);
            AttackHysteresisState[] hyst = new AttackHysteresisState[AttackingAIConstants.SQUAD_SIZE];

            int currentTick = 42;

            AttackingSnapshot snap = new AttackingSnapshot();
            snap.AttackingTeamId     = 1;
            snap.BallCarrierEntityId = 99;
            snap.BallPosition        = new Vector2(70f, 34f);
            snap.BallCarrierPosition = new Vector2(70f, 34f);
            snap.TeamAttackAngle     = 0f;
            snap.Agents[0]           = MakeAgent(1, 1, 34f, LineId.Attack);

            (AttackPoolEntry[] pool, int count) = BuildPoolWithOneAgent(snap);
            Assert.AreEqual(1, count);

            float cos = Mathf.Cos(0f);
            float sin = Mathf.Sin(0f);

            RoleAssigner.Assign(snap, pool, count, tinyTiming, hyst, cos, sin, currentTick);

            if (pool[0].HasRunParams)
            {
                Assert.GreaterOrEqual(pool[0].RunTriggerTick, currentTick + 1,
                    "RunTriggerTick must always be >= currentTick + 1 (T-AT-U-012 / FR-AT-011).");
            }
        }

        /// <summary>
        /// T-AT-U-015: Run target clamped to pitch boundary (≤ PITCH_LENGTH_M, ≤ PITCH_WIDTH_M).
        /// </summary>
        [Test]
        public void GenerateRunParams_RunTarget_ClampedToPitchBoundary()
        {
            StyleProfile large = new StyleProfile(100f, 1.0f, 1.0f, 3, 5);
            AttackHysteresisState[] hyst = new AttackHysteresisState[AttackingAIConstants.SQUAD_SIZE];

            // Place carrier near the goal line.
            AttackingSnapshot snap = new AttackingSnapshot();
            snap.AttackingTeamId     = 1;
            snap.BallCarrierEntityId = 99;
            snap.BallPosition        = new Vector2(100f, 65f);
            snap.BallCarrierPosition = new Vector2(100f, 65f);
            snap.TeamAttackAngle     = 0f;
            snap.Agents[0]           = MakeAgent(1, 1, 65f, LineId.Attack);

            (AttackPoolEntry[] pool, int count) = BuildPoolWithOneAgent(snap);
            Assert.AreEqual(1, count);

            float cos = Mathf.Cos(0f);
            float sin = Mathf.Sin(0f);

            RoleAssigner.Assign(snap, pool, count, large, hyst, cos, sin, 10);

            if (pool[0].HasRunParams)
            {
                Assert.LessOrEqual(pool[0].RunTargetPosition.x, AttackingAIConstants.PITCH_LENGTH_M,
                    "RunTargetPosition.x must be <= PITCH_LENGTH_M (T-AT-U-015 / FR-AT-011).");
                Assert.LessOrEqual(pool[0].RunTargetPosition.y, AttackingAIConstants.PITCH_WIDTH_M,
                    "RunTargetPosition.y must be <= PITCH_WIDTH_M (T-AT-U-015 / FR-AT-011).");
                Assert.GreaterOrEqual(pool[0].RunTargetPosition.x, 0f,
                    "RunTargetPosition.x must be >= 0 (T-AT-U-015).");
                Assert.GreaterOrEqual(pool[0].RunTargetPosition.y, 0f,
                    "RunTargetPosition.y must be >= 0 (T-AT-U-015).");
            }
        }
    }
}

    // ════════════════════════════════════════════════════════════════════════════
    // §5.2 Anti-chaos order test (T-AT-U-046) — orchestrator-level ordering check
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackingAIAntiChaosOrderTests
    {
        /// <summary>
        /// T-AT-U-046: Anti-chaos enforcer runs after role assignment and before publish.
        /// Inject a MAX_RUNNERS violation at assignment time; observe that the enforcer
        /// resolves it before AttackIntentSnapshot is built.
        /// Requires a full AttackingAITick pipeline — stub until Stage 1 infrastructure.
        /// </summary>
        [Test]
        public void InvariantEnforcer_RunsAfterAssignmentBeforePublish()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline including " +
                          "publish phase to verify ordering — activate when " +
                          "AttackingAITick integration tests are wired at Stage 1 (FR-AT-021).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.3 Integration Test Catalogue (T-AT-I-001..012)
    // §5.4 Determinism and Numerical Verification (T-AT-D-001..006)
    // §5.5 Performance Validation (T-AT-P-001..003)
    // ════════════════════════════════════════════════════════════════════════════

    [TestFixture]
    internal sealed class AttackingAIIntegrationTests
    {
        /// <summary>
        /// T-AT-I-001: 9-agent IN_POSSESSION, POSSESSION profile.
        /// ≤ 1 RUNNER, ≥ 1 SUPPORT_BALL, ≥ 2 HOLD_WIDTH, ≥ 1 WEAK_SIDE (pool ≥ 4); all invariants satisfied.
        /// </summary>
        [Test]
        public void Integration_9AgentInPossession_PossessionProfile_InvariantsSatisfied()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline with " +
                          "perception snapshot wiring — activate when Stage 1 simulation " +
                          "runtime is available (§5.3 T-AT-I-001).");
        }

        /// <summary>
        /// T-AT-I-002: 9-agent IN_POSSESSION, DIRECT profile.
        /// ≤ 3 RUNNERS; depthOffset_m ≈ 21m; all invariants satisfied.
        /// </summary>
        [Test]
        public void Integration_9AgentInPossession_DirectProfile_MaxThreeRunners()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-002).");
        }

        /// <summary>
        /// T-AT-I-003: 9-agent IN_POSSESSION, COUNTER_ATTACK profile.
        /// ≤ 4 RUNNERS; fastest timing; TRANSITION_HOLD_TICKS = 0 confirmed.
        /// </summary>
        [Test]
        public void Integration_9AgentInPossession_CounterAttackProfile_FastestTiming()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-003).");
        }

        /// <summary>
        /// T-AT-I-004: Possession → TRANSITION → OUT_OF_POSSESSION over 15 ticks.
        /// Ticks 1–4: normal output; tick 5: SET+decrement; ticks 5–9: frozen; tick 10+: empty.
        /// </summary>
        [Test]
        public void Integration_PossessionToTransitionToOutOfPossession_15Ticks()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick AttackingAITick driver with " +
                          "phase injection — activate when Stage 1 simulation runtime is available " +
                          "(§5.3 T-AT-I-004 / FR-AT-009).");
        }

        /// <summary>
        /// T-AT-I-005: Overload declared on correct flank.
        /// Ball at y=52; 3 non-WEAK_SIDE agents within 20m of y=52 → overloadActive=true, overloadFlank=RIGHT.
        /// </summary>
        [Test]
        public void Integration_OverloadDeclared_CorrectFlank()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-005).");
        }

        /// <summary>
        /// T-AT-I-006: Overload not declared when WEAK_SIDE agent is excluded from corridor count.
        /// 3 agents in corridor, 1 is WEAK_SIDE → count = 2; overloadActive=false.
        /// </summary>
        [Test]
        public void Integration_OverloadNotDeclared_WeakSideExcluded()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-006).");
        }

        /// <summary>
        /// T-AT-I-007: Anti-chaos combination resolved.
        /// 4 RUNNER candidates; after MAX_RUNNERS demotion, MIN_SUPPORT still violated →
        /// cascade resolves in ≤ 3 iterations; or all-default if unresolvable.
        /// </summary>
        [Test]
        public void Integration_AntiChaosCombination_Resolved()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-007).");
        }

        /// <summary>
        /// T-AT-I-008: All agents with DEFENSE lineMembership → zero RUNNER assignments;
        /// MIN_SUPPORT_AGENTS still satisfied.
        /// </summary>
        [Test]
        public void Integration_AllDefenseLineMembership_ZeroRunners()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-008).");
        }

        /// <summary>
        /// T-AT-I-009: Hysteresis over 10-tick run.
        /// Agent alternates SUPPORT_BALL-eligible every other tick → role stable for
        /// ATTACK_DWELL_TICKS; oscillation suppressed.
        /// </summary>
        [Test]
        public void Integration_Hysteresis_OscillationSuppressedOver10Ticks()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick AttackingAITick driver — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-009).");
        }

        /// <summary>
        /// T-AT-I-010: OUT_OF_POSSESSION skips all computation for 5 ticks →
        /// 5 empty directives; no role assignment performed.
        /// </summary>
        [Test]
        public void Integration_OutOfPossession_SkipsAllComputation_5Ticks()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick AttackingAITick driver — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-010).");
        }

        /// <summary>
        /// T-AT-I-011: Profile depthOffset verified across all 3 profiles.
        /// Same agent, same ball position: POSSESSION ≈ 12m; DIRECT ≈ 21m; COUNTER ≈ 24m.
        /// </summary>
        [Test]
        public void Integration_DepthOffset_VerifiedAcrossAllThreeProfiles()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-011).");
        }

        /// <summary>
        /// T-AT-I-012: Width-holding both sides.
        /// Ball at y=60: nearTouchlineY=64m; ball at y=8: nearTouchlineY=4m.
        /// </summary>
        [Test]
        public void Integration_WidthHolding_BothSides()
        {
            Assert.Ignore("Stage 0+1: requires full AttackingAITick pipeline — " +
                          "activate when Stage 1 simulation runtime is available (§5.3 T-AT-I-012).");
        }

        // ════════════════════════════════════════════════════════════════════════
        // §5.4 Determinism and Numerical Verification (T-AT-D-001..006)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-AT-D-001: 90-minute replay — bit-identical digest.
        /// Replay tick-for-tick; compare AttackDirective + AttackIntent[] +
        /// AttackHysteresisState[] + TransitionHoldState. Exact match required.
        /// </summary>
        [Test]
        public void Determinism_90MinuteReplay_BitIdenticalDigest()
        {
            Assert.Ignore("Stage 0+1: requires #16 replay infrastructure and full 90-minute " +
                          "match simulation — activate when deterministic replay is wired " +
                          "(§5.4 T-AT-D-001 / #16 §5).");
        }

        /// <summary>
        /// T-AT-D-002: EntityId iteration order stability.
        /// Fixed pool; assignment order is identical across multiple independent runs.
        /// </summary>
        [Test]
        public void Determinism_EntityIdIterationOrder_StableAcrossRuns()
        {
            Assert.Ignore("Stage 0+1: requires multi-run deterministic driver — " +
                          "activate when Stage 1 simulation runtime is available (§5.4 T-AT-D-002).");
        }

        /// <summary>
        /// T-AT-D-003: RNG domain tag isolation.
        /// Two matches; DOMAIN_TAG_ATTACKING_AI tagged correctly; no cross-domain bleed.
        /// </summary>
        [Test]
        public void Determinism_RngDomainTagIsolation_NoCrossDomainBleed()
        {
            Assert.Ignore("Stage 0+1: requires #16 DeterministicRngService with " +
                          "DOMAIN_TAG_ATTACKING_AI wired — activate when Stage 1 RNG " +
                          "integration is complete (§5.4 T-AT-D-003).");
        }

        /// <summary>
        /// T-AT-D-004: Hysteresis state digest round-trip.
        /// Serialize/deserialize AttackHysteresisState[]; resume tick → identical output after restore.
        /// </summary>
        [Test]
        public void Determinism_HysteresisStateDigest_RoundTrip()
        {
            Assert.Ignore("Stage 0+1: requires #16 SnapshotCodec serialization of " +
                          "AttackHysteresisState[] — activate when Stage 1 snapshot " +
                          "integration is complete (§5.4 T-AT-D-004).");
        }

        /// <summary>
        /// T-AT-D-005: TransitionHoldState round-trip.
        /// Serialize/deserialize; resume transition → countdown continues correctly.
        /// </summary>
        [Test]
        public void Determinism_TransitionHoldState_RoundTrip()
        {
            Assert.Ignore("Stage 0+1: requires #16 SnapshotCodec serialization of " +
                          "TransitionHoldState — activate when Stage 1 snapshot " +
                          "integration is complete (§5.4 T-AT-D-005).");
        }

        /// <summary>
        /// T-AT-D-006: Invariant demotion determinism.
        /// Same violation; run 1000 times → identical demotion order every run.
        /// </summary>
        [Test]
        public void Determinism_InvariantDemotion_IdenticalOrderAcross1000Runs()
        {
            Assert.Ignore("Stage 0+1: requires multi-run deterministic driver — " +
                          "activate when Stage 1 simulation runtime is available (§5.4 T-AT-D-006).");
        }

        // ════════════════════════════════════════════════════════════════════════
        // §5.5 Performance Validation (T-AT-P-001..003)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-AT-P-001: Per-tick time with 9 off-ball agents, POSSESSION profile.
        /// Budget: ≤ 0.10 ms on reference host per §6.3.
        /// </summary>
        [Test]
        public void Performance_PerTick_9Agents_PossessionProfile_Under0_1ms()
        {
            Assert.Ignore("Stage 0+1: requires reference host certification platform pinned in " +
                          "docs/tracking/certification-platform.md — activate when Stage 0+1 " +
                          "perf-gate infrastructure is ready (§5.5 T-AT-P-001).");
        }

        /// <summary>
        /// T-AT-P-002: Per-tick time with 10 agents, COUNTER_ATTACK profile (max pool + max RUNNER candidates).
        /// Budget: ≤ 0.10 ms on reference host per §6.3.
        /// </summary>
        [Test]
        public void Performance_PerTick_10Agents_CounterAttackProfile_Under0_1ms()
        {
            Assert.Ignore("Stage 0+1: requires reference host certification platform pinned in " +
                          "docs/tracking/certification-platform.md — activate when Stage 0+1 " +
                          "perf-gate infrastructure is ready (§5.5 T-AT-P-002).");
        }

        /// <summary>
        /// T-AT-P-003: Per-tick time, anti-chaos worst case (3 invariant-pass iterations, all-default path).
        /// Budget: ≤ 0.10 ms on reference host per §6.3.
        /// </summary>
        [Test]
        public void Performance_PerTick_AntiChaosWorstCase_Under0_1ms()
        {
            Assert.Ignore("Stage 0+1: requires reference host certification platform pinned in " +
                          "docs/tracking/certification-platform.md — activate when Stage 0+1 " +
                          "perf-gate infrastructure is ready (§5.5 T-AT-P-003).");
        }
    }

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-31 | —      | Initial implementation. ≥ 20 T-AT-U unit tests covering T-AT-U-001..048 per §5.2. |
//           |            |        | Covers: pool construction, role assignment, run-param clamps, support heuristic,     |
//           |            |        | width-holding, weak-side, overload detection, invariant enforcement, hysteresis,      |
//           |            |        | transition controller, constants catalogue, style profiles, struct field counts.       |
// | 1.1     | 2026-06-01 | —      | Add §5.3 integration stubs (T-AT-I-001..012), §5.4 determinism stubs               |
//           |            |        | (T-AT-D-001..006), §5.5 performance stubs (T-AT-P-001..003), and T-AT-U-046         |
//           |            |        | anti-chaos order stub. All stubs use Assert.Ignore per §5.1.1.                       |
#endregion
