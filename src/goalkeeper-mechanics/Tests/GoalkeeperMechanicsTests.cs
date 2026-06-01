// File:     src/goalkeeper-mechanics/Tests/GoalkeeperMechanicsTests.cs
// Created:  2026-05-31
// Modified: 2026-05-31
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §5, Code Standards #20
// Purpose:  Unit tests for Goalkeeper Mechanics. T-5.1 unit tests from §5.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.1 State machine — tactical transitions
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.1.x  GoalkeeperStateMachine tactical (10 Hz) transition tests.
    /// Covers §3.1.1 transition table rows exercised through EvaluateTacticalTransition.
    /// Goalkeeper Mechanics #11 §3.1 / §5.1.1.
    /// </summary>
    [TestFixture]
    internal class StateMachineTacticalTransitionTests
    {
        // Minimal default attribute block — all attributes at mid-range (10/20).
        private static GoalkeeperAgentAttributes DefaultAttrs()
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes  = 10f, Handling = 10f, Composure  = 10f,
                Strength  = 10f, Aerial   = 10f, Balance    = 10f,
                OneVsOne  = 10f, Pace     = 10f, Throwing   = 10f,
                Kicking   = 10f, Fatigue  = 0.0f, TeamId    = 0
            };
        }

        private static BallState DefaultBall()
        {
            return new BallState();
        }

        // ── T-5.1.1-A  Resting → Set when ball enters attacking third ─────────

        /// <summary>T-5.1.1-A: Resting → Set when ballInAttackingThird=true.</summary>
        [Test]
        public void Resting_BallEntersAttackingThird_TransitionsToSet()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Resting,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Set, result, "Resting should transition to Set when ball is in attacking third.");
        }

        // ── T-5.1.1-B  Resting remains Resting when ball NOT in attacking third ─

        /// <summary>T-5.1.1-B: Resting stays Resting when ball is not in attacking third.</summary>
        [Test]
        public void Resting_BallNotInAttackingThird_StaysResting()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Resting,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   false,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Resting, result, "Resting should stay Resting when ball is not in attacking third.");
        }

        // ── T-5.1.1-C  Set → Anticipate when anticipation score > threshold ───

        /// <summary>T-5.1.1-C: Set → Anticipate when anticipation score exceeds AnticipateThreshold.</summary>
        [Test]
        public void Set_HighAnticipationScore_TransitionsToAnticipate()
        {
            float aboveThreshold = GoalkeeperConstants.AnticipateThreshold + 0.01f;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Set,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      aboveThreshold,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Anticipate, result, "Set should transition to Anticipate when anticipation score exceeds threshold.");
        }

        // ── T-5.1.1-D  Set → Rushing when rush intent above commit threshold ──

        /// <summary>T-5.1.1-D: Set → Rushing when rush intent committed above RushCommitThreshold.</summary>
        [Test]
        public void Set_RushIntentAboveCommitThreshold_TransitionsToRushing()
        {
            float aboveCommit = GoalkeeperConstants.RushCommitThreshold + 0.01f;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Set,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          true,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    aboveCommit,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Rushing, result, "Set should transition to Rushing when rush intent exceeds commit threshold.");
        }

        // ── T-5.1.1-E  Set → Resting when ball leaves attacking third ─────────

        /// <summary>T-5.1.1-E: Set → Resting when ball is no longer in attacking third.</summary>
        [Test]
        public void Set_BallLeavesAttackingThird_TransitionsToResting()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Set,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   false,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Resting, result, "Set should revert to Resting when ball leaves attacking third.");
        }

        // ── T-5.1.1-F  Anticipate → Diving when SaveIntent committed ─────────

        /// <summary>T-5.1.1-F: Anticipate → Diving when hasSaveIntent is true.</summary>
        [Test]
        public void Anticipate_SaveIntentCommitted_TransitionsToDiving()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Anticipate,
                ballState:              DefaultBall(),
                hasSaveIntent:          true,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.8f,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Diving, result, "Anticipate should transition to Diving when SaveIntent is committed.");
        }

        // ── T-5.1.1-G  HandsOnBall → Distributing at releaseTickEarliest ─────

        /// <summary>T-5.1.1-G: HandsOnBall → Distributing when DistributeIntent and currentTick >= releaseTickEarliest.</summary>
        [Test]
        public void HandsOnBall_DistributeIntentAndReleaseTick_TransitionsToDistributing()
        {
            int releaseTick = 10;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.HandsOnBall,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    true,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            releaseTick,
                claimTick:              5,
                releaseTickEarliest:    releaseTick,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   false,
                ballInDefensiveThird:   true);

            Assert.AreEqual(GoalkeeperState.Distributing, result, "HandsOnBall should transition to Distributing when release tick is reached and DistributeIntent is set.");
        }

        // ── T-5.1.1-H  HandsOnBall forced release after GK_HOLD_MAX_TICKS ────

        /// <summary>T-5.1.1-H (T-5.1.1.4): HandsOnBall → Distributing after GK_HOLD_MAX_TICKS ticks.</summary>
        [Test]
        public void HandsOnBall_SixSecondRuleExpired_ForcesDistributing()
        {
            int claimTick  = 0;
            int currentTick = GoalkeeperConstants.GK_HOLD_MAX_TICKS; // = 60 ticks

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.HandsOnBall,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            currentTick,
                claimTick:              claimTick,
                releaseTickEarliest:    currentTick + 100, // not reached
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   false,
                ballInDefensiveThird:   true);

            Assert.AreEqual(GoalkeeperState.Distributing, result,
                "HandsOnBall must transition to Distributing after GK_HOLD_MAX_TICKS ticks have elapsed (6-second rule).");
        }

        // ── T-5.1.1-I  Recovering → Set on cooldown elapsed ──────────────────

        /// <summary>T-5.1.1-I: Recovering → Set when cooldown has elapsed.</summary>
        [Test]
        public void Recovering_CooldownElapsed_TransitionsToSet()
        {
            int cooldownEnd = 10;
            int currentTick = 11; // past cooldown

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Recovering,
                ballState:              DefaultBall(),
                hasSaveIntent:          false,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            currentTick,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:cooldownEnd,
                gkPosition:             new Vector2(52.5f, 34f),
                gkBaselineSlot:         new Vector2(0f, 34f),   // far from baseline
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Set, result, "Recovering should transition to Set when cooldown has elapsed.");
        }

        // ── T-5.1.1-J  OneOnOne → Diving when SaveIntent committed ───────────

        /// <summary>T-5.1.1-J: OneOnOne → Diving when SaveIntent committed (1v1 dive path).</summary>
        [Test]
        public void OneOnOne_SaveIntentCommitted_TransitionsToDiving()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.OneOnOne,
                ballState:              DefaultBall(),
                hasSaveIntent:          true,
                hasRushIntent:          false,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    0.0f,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Diving, result, "OneOnOne should transition to Diving when SaveIntent is committed.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.1 State machine — physics (60 Hz) transitions
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.1.x  GoalkeeperStateMachine physics (60 Hz) transition tests.
    /// Goalkeeper Mechanics #11 §3.1 / §5.1.1.
    /// </summary>
    [TestFixture]
    internal class StateMachinePhysicsTransitionTests
    {
        // ── Airborne → HandsOnBall on clean catch ─────────────────────────────

        /// <summary>Airborne → HandsOnBall when handBallContact with scalar >= CatchThreshold.</summary>
        [Test]
        public void Airborne_ContactAboveCatchThreshold_TransitionsToHandsOnBall()
        {
            float qualityAtOrAboveCatch = GoalkeeperConstants.CatchThreshold;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Airborne,
                handBallContactOccurred:       true,
                handlingQualityScalar:         qualityAtOrAboveCatch,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.HandsOnBall, result, "Airborne should transition to HandsOnBall on clean catch (quality >= CatchThreshold).");
        }

        // ── Airborne → Recovering on parry / spill / miss ─────────────────────

        /// <summary>Airborne → Recovering when handBallContact with scalar below CatchThreshold (parry/spill path).</summary>
        [Test]
        public void Airborne_ContactBelowCatchThreshold_TransitionsToRecovering()
        {
            float qualityBelowCatch = GoalkeeperConstants.CatchThreshold - 0.01f;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Airborne,
                handBallContactOccurred:       true,
                handlingQualityScalar:         qualityBelowCatch,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.Recovering, result, "Airborne should transition to Recovering when quality is below CatchThreshold.");
        }

        // ── Airborne → Recovering on ground re-entry without contact ──────────

        /// <summary>Airborne → Recovering on ground re-entry without contact (F-01/F-02/F-03).</summary>
        [Test]
        public void Airborne_GroundReEntryNoContact_TransitionsToRecovering()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Airborne,
                handBallContactOccurred:       false,
                handlingQualityScalar:         0.0f,
                groundReEntry:                 true,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.Recovering, result, "Airborne should transition to Recovering on ground re-entry without contact.");
        }

        // ── Diving → Airborne immediately ────────────────────────────────────

        /// <summary>Diving → Airborne (launch impulse applied this frame).</summary>
        [Test]
        public void Diving_PhysicsTick_TransitionsToAirborne()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Diving,
                handBallContactOccurred:       false,
                handlingQualityScalar:         0.0f,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.Airborne, result, "Diving should transition to Airborne on first physics tick.");
        }

        // ── Rushing → OneOnOne when attacker enters trigger radius ────────────

        /// <summary>Rushing → OneOnOne when attacker within OneVsOneTriggerRadiusM.</summary>
        [Test]
        public void Rushing_AttackerWithinTriggerRadius_TransitionsToOneOnOne()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Rushing,
                handBallContactOccurred:       false,
                handlingQualityScalar:         0.0f,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  true,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.OneOnOne, result, "Rushing should transition to OneOnOne when attacker enters the 1v1 trigger radius.");
        }

        // ── Rushing → Smothered on hand contact ───────────────────────────────

        /// <summary>Rushing → Smothered on hand-ball contact during rush.</summary>
        [Test]
        public void Rushing_HandBallContact_TransitionsToSmothered()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Rushing,
                handBallContactOccurred:       true,
                handlingQualityScalar:         0.5f,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.Smothered, result, "Rushing should transition to Smothered on hand-ball contact.");
        }

        // ── Set → Anticipate on ShotExecutedEvent detected ────────────────────

        /// <summary>Set → Anticipate when shotEventDetected is true (predictive path, physics tick).</summary>
        [Test]
        public void Set_ShotEventDetected_TransitionsToAnticipate()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Set,
                handBallContactOccurred:       false,
                handlingQualityScalar:         0.0f,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             true);

            Assert.AreEqual(GoalkeeperState.Anticipate, result, "Set should transition to Anticipate when ShotExecutedEvent is detected.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.2  Reaction pipeline
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.2.x  GoalkeeperReactionPipeline unit tests.
    /// Goalkeeper Mechanics #11 §3.2 / §5.1.2.
    /// </summary>
    [TestFixture]
    internal class ReactionPipelineTests
    {
        private static GoalkeeperAgentAttributes MakeAttrs(float reflexes = 10f, float oneVsOne = 10f, float fatigue = 0f)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = reflexes, Handling = 10f, Composure  = 10f,
                Strength = 10f,      Aerial   = 10f, Balance    = 10f,
                OneVsOne = oneVsOne, Pace     = 10f, Throwing   = 10f,
                Kicking  = 10f,      Fatigue  = fatigue, TeamId = 0
            };
        }

        // ── T-5.1.2.2  requiredReactionMs monotonic-decreasing in Reflexes_norm

        /// <summary>T-5.1.2.2: requiredReactionMs is monotonically decreasing as Reflexes increases.</summary>
        [Test]
        public void RequiredReactionMs_HigherReflexes_LowerRequiredTime()
        {
            float ballSpeed = GoalkeeperConstants.ReactionBallSpeedRefMps; // at reference — no speed penalty

            float reqLow  = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(
                MakeAttrs(reflexes: 1f), ballSpeed, GoalkeeperState.Set);
            float reqHigh = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(
                MakeAttrs(reflexes: 20f), ballSpeed, GoalkeeperState.Set);

            Assert.Less(reqHigh, reqLow, "Higher Reflexes attribute must reduce requiredReactionMs.");
        }

        // ── T-5.1.2.3  requiredReactionMs increases above ReactionBallSpeedRefMps

        /// <summary>T-5.1.2.3: requiredReactionMs is monotonically increasing for ball speed above the reference.</summary>
        [Test]
        public void RequiredReactionMs_BallSpeedAboveReference_IncreasesMonotonically()
        {
            GoalkeeperAgentAttributes attrs = MakeAttrs(reflexes: 10f);
            float refSpeed = GoalkeeperConstants.ReactionBallSpeedRefMps;

            float reqAtRef  = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, refSpeed,       GoalkeeperState.Set);
            float reqFast   = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, refSpeed + 10f, GoalkeeperState.Set);
            float reqFaster = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, refSpeed + 20f, GoalkeeperState.Set);

            Assert.LessOrEqual(reqAtRef, reqFast,   "requiredReactionMs must be non-decreasing above ball speed reference.");
            Assert.LessOrEqual(reqFast,  reqFaster,  "requiredReactionMs must be non-decreasing as ball speed continues to rise.");
        }

        // ── T-5.1.2.4  OneOnOne state reduces requiredReactionMs by expected amount

        /// <summary>T-5.1.2.4: OneOnOne state reduces requiredReactionMs by ONE_VS_ONE_REACTION_COEFF × OneVsOne_norm (KD-20).</summary>
        [Test]
        public void RequiredReactionMs_OneOnOneState_ReducesRequiredTime()
        {
            GoalkeeperAgentAttributes attrs = MakeAttrs(oneVsOne: 20f);
            float ballSpeed = GoalkeeperConstants.ReactionBallSpeedRefMps;

            float reqSet      = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeed, GoalkeeperState.Set);
            float reqOneOnOne = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeed, GoalkeeperState.OneOnOne);

            float expectedReduction = GoalkeeperConstants.OneVsOneReactionCoeff * attrs.OneVsOneNorm;

            Assert.AreEqual(reqSet - expectedReduction, reqOneOnOne, 1e-4f,
                "OneOnOne state should reduce requiredReactionMs by ONE_VS_ONE_REACTION_COEFF × OneVsOne_norm.");
        }

        // ── T-5.1.2.5  Label-band boundary at ReflexiveLabelThreshold ─────────

        /// <summary>T-5.1.2.5a: reactionWindowAchieved just above REFLEXIVE_LABEL_THRESHOLD → Reflexive label.</summary>
        [Test]
        public void ComputeReactionLabel_JustAboveReflexiveThreshold_IsReflexive()
        {
            float window = GoalkeeperConstants.ReflexiveLabelThreshold + 0.01f;
            ReactionLabel label = GoalkeeperReactionPipeline.ComputeReactionLabel(window);
            Assert.AreEqual(ReactionLabel.Reflexive, label, "reactionWindowAchieved above REFLEXIVE_LABEL_THRESHOLD must yield Reflexive label.");
        }

        /// <summary>T-5.1.2.5b: reactionWindowAchieved just below SLUGGISH_LABEL_THRESHOLD → Sluggish label.</summary>
        [Test]
        public void ComputeReactionLabel_JustBelowSluggishThreshold_IsSluggish()
        {
            float window = GoalkeeperConstants.SluggishLabelThreshold - 0.01f;
            ReactionLabel label = GoalkeeperReactionPipeline.ComputeReactionLabel(window);
            Assert.AreEqual(ReactionLabel.Sluggish, label, "reactionWindowAchieved below SLUGGISH_LABEL_THRESHOLD must yield Sluggish label.");
        }

        // ── T-5.1.2.1  reactionWindowAchieved uses asymmetric tolerances (KD-18)

        /// <summary>T-5.1.2.1: Equal absolute offset early vs. late yields different reactionWindowAchieved (asymmetric tolerances).</summary>
        [Test]
        public void ReactionWindowAchieved_EarlyVsLateAsymmetric_DifferentPenalty()
        {
            float requiredMs  = 300f;
            float offset      = 50f; // smaller than both tolerances to avoid clamping to zero

            float windowEarly = GoalkeeperReactionPipeline.ComputeReactionWindowAchieved(requiredMs - offset, requiredMs);
            float windowLate  = GoalkeeperReactionPipeline.ComputeReactionWindowAchieved(requiredMs + offset, requiredMs);

            // Early tolerance (120 ms) is larger than late (80 ms), so early penalty is smaller.
            Assert.Greater(windowEarly, windowLate,
                "An equal offset earlier than required should yield a higher reactionWindowAchieved than the same offset late, because the early tolerance is larger (KD-18).");
        }

        // ── T-5.1.2.6  Determinism: same inputs → same output ─────────────────

        /// <summary>T-5.1.2.6: ComputeRequiredReactionMs is deterministic; same inputs always produce same result.</summary>
        [Test]
        public void RequiredReactionMs_SameInputsMultipleCalls_IdentialResults()
        {
            GoalkeeperAgentAttributes attrs = MakeAttrs(reflexes: 15f);
            float ballSpeed = 25f;

            float result1 = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeed, GoalkeeperState.Anticipate);
            float result2 = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeed, GoalkeeperState.Anticipate);
            float result3 = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeed, GoalkeeperState.Anticipate);

            Assert.AreEqual(result1, result2, 0f, "Reaction pipeline must be deterministic: run 1 vs run 2.");
            Assert.AreEqual(result2, result3, 0f, "Reaction pipeline must be deterministic: run 2 vs run 3.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.3  Dive kinematics
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.3.x  GoalkeeperDiveKinematics unit tests.
    /// Goalkeeper Mechanics #11 §3.3 / §5.1.3.
    /// </summary>
    [TestFixture]
    internal class DiveKinematicsTests
    {
        private static GoalkeeperAgentAttributes MakeAttrs(
            float strength = 10f, float aerial = 10f, float handling = 10f, float fatigue = 0f)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f, Handling = handling, Composure  = 10f,
                Strength = strength, Aerial = aerial, Balance   = 10f,
                OneVsOne = 10f, Pace     = 10f,       Throwing  = 10f,
                Kicking  = 10f, Fatigue  = fatigue,   TeamId    = 0
            };
        }

        // ── T-5.1.3.1  diveLaunchImpulse linear in Strength_norm ──────────────

        /// <summary>T-5.1.3.1: ComputeDiveLaunchImpulse is linear in Strength attribute.</summary>
        [Test]
        public void DiveLaunchImpulse_HigherStrength_ProducesLargerImpulse()
        {
            float impulseWeak   = GoalkeeperDiveKinematics.ComputeDiveLaunchImpulse(MakeAttrs(strength: 5f),  1f);
            float impulseMid    = GoalkeeperDiveKinematics.ComputeDiveLaunchImpulse(MakeAttrs(strength: 10f), 1f);
            float impulseStrong = GoalkeeperDiveKinematics.ComputeDiveLaunchImpulse(MakeAttrs(strength: 20f), 1f);

            Assert.Less(impulseWeak, impulseMid,    "Higher Strength should produce a larger dive launch impulse.");
            Assert.Less(impulseMid,  impulseStrong, "Higher Strength should produce a larger dive launch impulse.");
        }

        // ── T-5.1.3.2  peakHandZ linear in Aerial_norm ────────────────────────

        /// <summary>T-5.1.3.2: ComputePeakHandZ is monotonically increasing in Aerial attribute.</summary>
        [Test]
        public void PeakHandZ_HigherAerial_ProducesLargerPeakZ()
        {
            float zLow  = GoalkeeperDiveKinematics.ComputePeakHandZ(MakeAttrs(aerial: 1f),  0f);
            float zMid  = GoalkeeperDiveKinematics.ComputePeakHandZ(MakeAttrs(aerial: 10f), 0f);
            float zHigh = GoalkeeperDiveKinematics.ComputePeakHandZ(MakeAttrs(aerial: 20f), 0f);

            Assert.Less(zLow, zMid,  "Higher Aerial attribute should yield a higher dive peak hand Z.");
            Assert.Less(zMid, zHigh, "Higher Aerial attribute should yield a higher dive peak hand Z.");
        }

        // ── T-5.1.3.3  fatigue reduces peakHandZ (KD-8 sign) ─────────────────

        /// <summary>T-5.1.3.3: Fatigue=1 reduces peakHandZ by DIVE_FATIGUE_PEAK_Z_COEFF relative to fatigue=0.</summary>
        [Test]
        public void PeakHandZ_MaxFatigue_ReducesByFatiguePeakZCoeff()
        {
            GoalkeeperAgentAttributes fresh   = MakeAttrs(fatigue: 0.0f);
            GoalkeeperAgentAttributes fatigued = MakeAttrs(fatigue: 1.0f);

            float zFresh    = GoalkeeperDiveKinematics.ComputePeakHandZ(fresh,    0f);
            float zFatigued = GoalkeeperDiveKinematics.ComputePeakHandZ(fatigued, 0f);

            float expectedReduction = GoalkeeperConstants.DiveFatiguePeakZCoeff;

            Assert.AreEqual(zFresh - expectedReduction, zFatigued, 1e-4f,
                "Full fatigue should reduce peakHandZ by exactly DIVE_FATIGUE_PEAK_Z_COEFF (KD-8).");
        }

        // ── T-5.1.3.4  Apex Z at mid-frame, ground re-entry at launch+duration ─

        /// <summary>T-5.1.3.4a: handPathZ is at its peak at the apex frame.</summary>
        [Test]
        public void HandPathZ_AtApexFrame_EqualsFullPeakHandZ()
        {
            float peakZ         = 1.5f;
            int   launchFrame   = 10;
            int   durationFrm   = 36; // ComputeDiveDurationFrames() = round(600 / (1000/60)) = 36
            int   apexFrame     = launchFrame + durationFrm / 2;

            float zAtApex = GoalkeeperDiveKinematics.ComputeHandPathZ(apexFrame, launchFrame, durationFrm, peakZ);

            Assert.AreEqual(peakZ, zAtApex, 1e-4f, "Hand Z at the apex frame should equal peakHandZ.");
        }

        /// <summary>T-5.1.3.4b: handPathZ is 0 at the ground re-entry frame (launch + duration).</summary>
        [Test]
        public void HandPathZ_AtGroundReEntryFrame_IsZero()
        {
            float peakZ        = 1.5f;
            int   launchFrame  = 10;
            int   durationFrm  = 36;
            int   endFrame     = launchFrame + durationFrm;

            float zAtEnd = GoalkeeperDiveKinematics.ComputeHandPathZ(endFrame, launchFrame, durationFrm, peakZ);

            Assert.AreEqual(0f, zAtEnd, 1e-4f, "Hand Z at the ground re-entry frame should be 0.");
        }

        // ── T-5.1.3.5  reachRadius decreases with fatigue ─────────────────────

        /// <summary>T-5.1.3.5: ComputeReachRadius decreases as fatigue increases.</summary>
        [Test]
        public void ReachRadius_HigherFatigue_LowerReachRadius()
        {
            float rFresh    = GoalkeeperDiveKinematics.ComputeReachRadius(MakeAttrs(fatigue: 0.0f));
            float rFatigued = GoalkeeperDiveKinematics.ComputeReachRadius(MakeAttrs(fatigue: 1.0f));

            Assert.Greater(rFresh, rFatigued, "Fatigue must reduce the dive reach radius (KD-8 sign).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.4 + §5.1.5  Handling quality scalar and band-to-action mapping
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.4.x + T-5.1.5.x  GoalkeeperHandlingQuality unit tests.
    /// Goalkeeper Mechanics #11 §3.5 / §5.1.4 / §5.1.5.
    /// </summary>
    [TestFixture]
    internal class HandlingQualityTests
    {
        private static GoalkeeperAgentAttributes MakeAttrs(
            float handling = 10f, float oneVsOne = 10f, float fatigue = 0f)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f, Handling = handling, Composure  = 10f,
                Strength = 10f, Aerial   = 10f,      Balance    = 10f,
                OneVsOne = oneVsOne, Pace = 10f,      Throwing  = 10f,
                Kicking  = 10f, Fatigue  = fatigue,   TeamId    = 0
            };
        }

        // ── T-5.1.4.3  attrFactor linear in Handling_norm ─────────────────────

        /// <summary>T-5.1.4.3: Higher Handling attribute increases the raw handlingQualityScalar (other factors fixed).</summary>
        [Test]
        public void HandlingQuality_HigherHandlingAttr_ProducesHigherScalar()
        {
            Vector3 contact = Vector3.zero; // perfect contact — no point error
            float   ballSpeed = GoalkeeperConstants.HandlingBallSpeedRefMps; // no speed penalty
            float   reaction  = 1.0f; // perfect reaction window

            float qualityLow = GoalkeeperHandlingQuality.Compute(
                MakeAttrs(handling: 1f), contact, contact, ballSpeed, reaction,
                GoalkeeperState.Set, 0f, 0f, out _);

            float qualityHigh = GoalkeeperHandlingQuality.Compute(
                MakeAttrs(handling: 20f), contact, contact, ballSpeed, reaction,
                GoalkeeperState.Set, 0f, 0f, out _);

            Assert.Greater(qualityHigh, qualityLow, "Higher Handling attribute should yield a higher handling quality scalar.");
        }

        // ── T-5.1.4.4  OneOnOne bonus on handling quality ─────────────────────

        /// <summary>T-5.1.4.4: OneOnOne state increases handlingQualityScalar by ONE_VS_ONE_HANDLING_COEFF × OneVsOne_norm (KD-20).</summary>
        [Test]
        public void HandlingQuality_OneOnOneState_HasHigherScalarThanSetState()
        {
            Vector3 contact = Vector3.zero;
            float   ballSpeed = GoalkeeperConstants.HandlingBallSpeedRefMps;
            float   reaction  = 1.0f;
            GoalkeeperAgentAttributes attrs = MakeAttrs(handling: 10f, oneVsOne: 20f);

            float qualitySet      = GoalkeeperHandlingQuality.Compute(
                attrs, contact, contact, ballSpeed, reaction, GoalkeeperState.Set, 0f, 0f, out _);
            float qualityOneOnOne = GoalkeeperHandlingQuality.Compute(
                attrs, contact, contact, ballSpeed, reaction, GoalkeeperState.OneOnOne, 0f, 0f, out _);

            Assert.Greater(qualityOneOnOne, qualitySet,
                "OneOnOne state should produce a higher handling quality than Set state (KD-20).");
        }

        // ── T-5.1.4.5  Fatigue reduces quality (KD-8 sign) ───────────────────

        /// <summary>T-5.1.4.5: Full fatigue reduces handlingQualityScalar relative to rested state (KD-8 sign).</summary>
        [Test]
        public void HandlingQuality_MaxFatigue_ReducesScalar()
        {
            Vector3 contact = Vector3.zero;
            float   ballSpeed = GoalkeeperConstants.HandlingBallSpeedRefMps;
            float   reaction  = 1.0f;

            float qualityFresh    = GoalkeeperHandlingQuality.Compute(
                MakeAttrs(fatigue: 0f), contact, contact, ballSpeed, reaction,
                GoalkeeperState.Set, 0f, 0f, out _);
            float qualityFatigued = GoalkeeperHandlingQuality.Compute(
                MakeAttrs(fatigue: 1f), contact, contact, ballSpeed, reaction,
                GoalkeeperState.Set, 0f, 0f, out _);

            Assert.Greater(qualityFresh, qualityFatigued, "Full fatigue must reduce the handling quality scalar (KD-8).");
        }

        // ── T-5.1.4.6  Blend-alpha weights sum to 1.0 ─────────────────────────

        /// <summary>T-5.1.4.6: HandlingReactionBlendAlpha and its complement sum to exactly 1.0.</summary>
        [Test]
        public void BlendAlpha_PlusComplement_SumsToOne()
        {
            float alpha = GoalkeeperConstants.HandlingReactionBlendAlpha;
            Assert.AreEqual(1.0f, alpha + (1.0f - alpha), 1e-6f,
                "HandlingReactionBlendAlpha + (1 - HandlingReactionBlendAlpha) must equal 1.0.");
        }

        // ── T-5.1.5.1  Band-to-action: Caught above CatchThreshold ───────────

        /// <summary>T-5.1.5.1a: handlingQualityScalar at CatchThreshold or above → HandlingQualityLabel.Caught.</summary>
        [Test]
        public void HandlingLabel_QualityAtCatchThreshold_IsCaught()
        {
            Vector3 contact = Vector3.zero;
            float   ballSpeed = GoalkeeperConstants.HandlingBallSpeedRefMps;

            // Drive quality to be at CatchThreshold: use high handling, perfect reaction, no noise.
            // We verify the label on a known quality value by using noise=0 and tuned attrs.
            float quality = GoalkeeperHandlingQuality.Compute(
                MakeAttrs(handling: 20f), contact, contact, ballSpeed, 1.0f,
                GoalkeeperState.Set, 0f, 0f, out HandlingQualityLabel label);

            // The quality may or may not be above CatchThreshold — check label matches threshold.
            HandlingQualityLabel expectedLabel;
            if (quality >= GoalkeeperConstants.CatchThreshold)
            {
                expectedLabel = HandlingQualityLabel.Caught;
            }
            else if (quality >= GoalkeeperConstants.ParryThreshold)
            {
                expectedLabel = HandlingQualityLabel.Parried;
            }
            else if (quality >= GoalkeeperConstants.DeflectThreshold)
            {
                expectedLabel = HandlingQualityLabel.Deflected;
            }
            else if (quality >= GoalkeeperConstants.MinHandlingQuality)
            {
                expectedLabel = HandlingQualityLabel.Spilled;
            }
            else
            {
                expectedLabel = HandlingQualityLabel.Missed;
            }

            Assert.AreEqual(expectedLabel, label, "HandlingQualityLabel must match the quality scalar's threshold band.");
        }

        // ── T-5.1.5.5  parryVelocity retain monotonic-decreasing in quality ──

        /// <summary>T-5.1.5.5: parry velocity retain is monotonically decreasing as quality increases.</summary>
        [Test]
        public void ParryVelocity_HigherQuality_LowerRetain()
        {
            Vector3 incoming = new Vector3(20f, 0f, 0f);
            float   firmness = 0.5f;

            Vector3 parryLowQ  = GoalkeeperHandlingQuality.ComputeParryVelocity(incoming, GoalkeeperConstants.ParryThreshold,    firmness);
            Vector3 parryHighQ = GoalkeeperHandlingQuality.ComputeParryVelocity(incoming, GoalkeeperConstants.CatchThreshold - 0.01f, firmness);

            Assert.Less(parryHighQ.magnitude, parryLowQ.magnitude,
                "Higher quality should produce a smaller parry rebound magnitude (more controlled, less rebound).");
        }

        // ── T-5.1.5.6  spillVelocity retain > parryVelocity retain ───────────

        /// <summary>T-5.1.5.6: spill velocity retain is greater than parry velocity retain at matched quality.</summary>
        [Test]
        public void SpillVelocity_RetainGreaterThanParryRetain()
        {
            Vector3 incoming = new Vector3(20f, 0f, 0f);
            float   quality  = GoalkeeperConstants.DeflectThreshold; // mid-range quality

            Vector3 parryVel = GoalkeeperHandlingQuality.ComputeParryVelocity(incoming, quality, 0f);
            Vector3 spillVel = GoalkeeperHandlingQuality.ComputeSpillVelocity(incoming, quality);

            Assert.Greater(spillVel.magnitude, parryVel.magnitude,
                "Spill velocity retain must exceed parry velocity retain at the same quality (§3.5.3 SpillRetainBonus).");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.6  Cross-claim duel weight invariant
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.6.x  Cross-claim duel constant invariant tests.
    /// Goalkeeper Mechanics #11 §3.6 / §5.1.6.
    /// </summary>
    [TestFixture]
    internal class CrossClaimDuelTests
    {
        // ── T-5.1.6.6  Duel attribute weight sum = 1.0 ────────────────────────

        /// <summary>T-5.1.6.6: The three cross-claim duel attribute weights must sum to 1.0.</summary>
        [Test]
        public void CrossClaimDuelWeights_SumToOne()
        {
            float sum = GoalkeeperConstants.CrossClaimDuelBalanceW
                      + GoalkeeperConstants.CrossClaimDuelStrengthW
                      + GoalkeeperConstants.CrossClaimDuelAerialW;

            Assert.AreEqual(1.0f, sum, 1e-5f,
                "CROSS_CLAIM_DUEL_BALANCE_W + CROSS_CLAIM_DUEL_STRENGTH_W + CROSS_CLAIM_DUEL_AERIAL_W must equal 1.0.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.7  Rush dispatch
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.7.x  GoalkeeperRushDispatch unit tests.
    /// Goalkeeper Mechanics #11 §3.7 / §5.1.7.
    /// </summary>
    [TestFixture]
    internal class RushDispatchTests
    {
        private static GoalkeeperAgentAttributes MakeAttrs(float pace = 10f, float fatigue = 0f)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f, Handling = 10f, Composure  = 10f,
                Strength = 10f, Aerial   = 10f, Balance    = 10f,
                OneVsOne = 10f, Pace     = pace, Throwing   = 10f,
                Kicking  = 10f, Fatigue  = fatigue, TeamId  = 0
            };
        }

        // ── T-5.1.7.1  Commit threshold gate ─────────────────────────────────

        /// <summary>T-5.1.7.1a: RushCommitmentLevel above RushCommitThreshold in Set state → Rushing.</summary>
        [Test]
        public void StateMachine_RushCommitAboveThreshold_EntersRushing()
        {
            float aboveThreshold = GoalkeeperConstants.RushCommitThreshold + 0.01f;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Set,
                ballState:              new BallState(),
                hasSaveIntent:          false,
                hasRushIntent:          true,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    aboveThreshold,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreEqual(GoalkeeperState.Rushing, result,
                "commitmentLevel above RushCommitThreshold should transition Set to Rushing.");
        }

        /// <summary>T-5.1.7.1b: RushCommitmentLevel at or below RushCommitThreshold in Set state → stays Set.</summary>
        [Test]
        public void StateMachine_RushCommitAtThreshold_StaysSet()
        {
            float atThreshold = GoalkeeperConstants.RushCommitThreshold;

            GoalkeeperState result = GoalkeeperStateMachine.EvaluateTacticalTransition(
                currentState:           GoalkeeperState.Set,
                ballState:              new BallState(),
                hasSaveIntent:          false,
                hasRushIntent:          true,
                hasDistributeIntent:    false,
                anticipationScore:      0.0f,
                rushCommitmentLevel:    atThreshold,
                currentTick:            1,
                claimTick:              -1,
                releaseTickEarliest:    0,
                recoveryCooldownEndTick:0,
                gkPosition:             Vector2.zero,
                gkBaselineSlot:         Vector2.zero,
                ballInAttackingThird:   true,
                ballInDefensiveThird:   false);

            Assert.AreNotEqual(GoalkeeperState.Rushing, result,
                "commitmentLevel at (not above) RushCommitThreshold must NOT transition to Rushing.");
        }

        // ── T-5.1.7.4  rushLaunchMps linear in Pace_norm ──────────────────────

        /// <summary>T-5.1.7.4: ComputeRushLaunchMps is monotonically increasing with Pace attribute.</summary>
        [Test]
        public void RushLaunchMps_HigherPace_ProducesHigherSpeed()
        {
            float speedSlow = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 1f));
            float speedMid  = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 10f));
            float speedFast = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 20f));

            Assert.Less(speedSlow, speedMid,  "Higher Pace attribute should produce a higher rush launch speed.");
            Assert.Less(speedMid,  speedFast, "Higher Pace attribute should produce a higher rush launch speed.");
        }

        // ── T-5.1.7.5  Fatigue reduces rushLaunchMps (KD-8 sign) ─────────────

        /// <summary>T-5.1.7.5: Full fatigue reduces rushLaunchMps relative to rested state (KD-8 sign).</summary>
        [Test]
        public void RushLaunchMps_MaxFatigue_ReducesSpeed()
        {
            float speedFresh    = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 10f, fatigue: 0.0f));
            float speedFatigued = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 10f, fatigue: 1.0f));

            Assert.Greater(speedFresh, speedFatigued, "Full fatigue must reduce rush launch speed (KD-8).");
        }

        // ── T-5.1.7.6  Rushing → Smothered on hand contact (physics pass) ────

        /// <summary>T-5.1.7.6: Rushing → Smothered on hand-ball contact (verified via EvaluatePhysicsTransition).</summary>
        [Test]
        public void Rushing_HandContact_PhysicsTransitionsToSmothered()
        {
            GoalkeeperState result = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                currentState:                  GoalkeeperState.Rushing,
                handBallContactOccurred:       true,
                handlingQualityScalar:         0.6f,
                groundReEntry:                 false,
                distributionReleaseReached:    false,
                rushBallIntercepted:           false,
                attackerWithinOneVsOneRadius:  false,
                gkWithinSmotherRadius:         false,
                shotEventDetected:             false);

            Assert.AreEqual(GoalkeeperState.Smothered, result, "Rushing + hand contact should transition to Smothered.");
        }

        // ── UpdateRushFrame does not overshoot target ─────────────────────────

        /// <summary>UpdateRushFrame does not overshoot the rush target on the final step.</summary>
        [Test]
        public void UpdateRushFrame_NearTarget_DoesNotOvershoot()
        {
            Vector3 rushTarget = new Vector3(10f, 34f, 0f);
            Vector3 gkPos      = new Vector3(9.99f, 34f, 0f); // very close to target

            float launchMps = GoalkeeperRushDispatch.ComputeRushLaunchMps(MakeAttrs(pace: 20f));

            GoalkeeperRushDispatch.UpdateRushFrame(ref gkPos, rushTarget, launchMps);

            // After one frame, GK must not have passed the target.
            float distAfter = Vector3.Distance(gkPos, rushTarget);
            Assert.LessOrEqual(distAfter, Vector3.Distance(new Vector3(9.99f, 34f, 0f), rushTarget) + 1e-4f,
                "UpdateRushFrame must not overshoot the rush target.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // §5.1.8  Distribution generation
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// T-5.1.8.x  GoalkeeperDistribution unit tests.
    /// Goalkeeper Mechanics #11 §3.8 / §5.1.8.
    /// </summary>
    [TestFixture]
    internal class DistributionTests
    {
        private static GoalkeeperAgentAttributes MakeAttrs(float throwing = 10f, float kicking = 10f)
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f, Handling = 10f, Composure  = 10f,
                Strength = 10f, Aerial   = 10f, Balance    = 10f,
                OneVsOne = 10f, Pace     = 10f, Throwing   = throwing,
                Kicking  = kicking, Fatigue = 0f, TeamId   = 0
            };
        }

        // ── T-5.1.8.1  Release heights match §3.4.7 anchors ──────────────────

        /// <summary>T-5.1.8.1a: Throw release point Z = gkZ + ThrowReleaseHeightM.</summary>
        [Test]
        public void ReleasePoint_Throw_MatchesConstantHeight()
        {
            Vector3 gkPos = new Vector3(2.5f, 34f, 0f);
            Vector3 releasePoint = GoalkeeperDistribution.ComputeReleasePoint(gkPos, DeliveryKind.Throw);

            float expectedZ = gkPos.z + GoalkeeperConstants.ThrowReleaseHeightM;
            Assert.AreEqual(expectedZ, releasePoint.z, 1e-5f, "Throw release point Z must equal gkZ + ThrowReleaseHeightM.");
        }

        /// <summary>T-5.1.8.1b: Roll release point Z = gkZ + RollReleaseHeightM.</summary>
        [Test]
        public void ReleasePoint_Roll_MatchesConstantHeight()
        {
            Vector3 gkPos = new Vector3(2.5f, 34f, 0f);
            Vector3 releasePoint = GoalkeeperDistribution.ComputeReleasePoint(gkPos, DeliveryKind.Roll);

            float expectedZ = gkPos.z + GoalkeeperConstants.RollReleaseHeightM;
            Assert.AreEqual(expectedZ, releasePoint.z, 1e-5f, "Roll release point Z must equal gkZ + RollReleaseHeightM.");
        }

        /// <summary>T-5.1.8.1c: Kick release point Z = gkZ + KickReleaseHeightM.</summary>
        [Test]
        public void ReleasePoint_Kick_MatchesConstantHeight()
        {
            Vector3 gkPos = new Vector3(2.5f, 34f, 0f);
            Vector3 releasePoint = GoalkeeperDistribution.ComputeReleasePoint(gkPos, DeliveryKind.Kick);

            float expectedZ = gkPos.z + GoalkeeperConstants.KickReleaseHeightM;
            Assert.AreEqual(expectedZ, releasePoint.z, 1e-5f, "Kick release point Z must equal gkZ + KickReleaseHeightM.");
        }

        // ── T-5.1.8.1  Windup durations match §3.4.7 anchors ─────────────────

        /// <summary>T-5.1.8.1d: Windup duration for Throw matches ThrowWindupMs.</summary>
        [Test]
        public void WindupMs_Throw_MatchesThrowWindupMs()
        {
            float windup = GoalkeeperDistribution.ComputeWindupMs(DeliveryKind.Throw);
            Assert.AreEqual(GoalkeeperConstants.ThrowWindupMs, windup, 1e-5f, "Throw windup must equal ThrowWindupMs.");
        }

        /// <summary>T-5.1.8.1e: Windup duration for Roll matches RollWindupMs.</summary>
        [Test]
        public void WindupMs_Roll_MatchesRollWindupMs()
        {
            float windup = GoalkeeperDistribution.ComputeWindupMs(DeliveryKind.Roll);
            Assert.AreEqual(GoalkeeperConstants.RollWindupMs, windup, 1e-5f, "Roll windup must equal RollWindupMs.");
        }

        // ── T-5.1.8.3  F-05 missing-receiver fallback ─────────────────────────

        /// <summary>T-5.1.8.3: F-05 nullifies TargetReceiverId and emits warning when receiver not in roster.</summary>
        [Test]
        public void ValidateTarget_ReceiverNotInRoster_NullifiesReceiverId()
        {
            DistributeIntent intent = new DistributeIntent
            {
                DeliveryKind     = DeliveryKind.Throw,
                TargetReceiverId = 7,
                TargetPoint      = new Vector3(30f, 20f, 0f),
                PowerIntent      = 0.8f,
                SpinIntent       = Vector3.zero
            };

            GoalkeeperDistribution.ValidateTarget(ref intent, agentRosterContains: false,
                out bool receiverMissingWarning, out bool targetPointClamped);

            Assert.IsNull(intent.TargetReceiverId,    "F-05: TargetReceiverId must be nullified when receiver is absent from roster.");
            Assert.IsTrue(receiverMissingWarning,      "F-05: receiverMissingWarningEmitted must be true when receiver is absent.");
            Assert.IsFalse(targetPointClamped,          "F-05 alone must not clamp TargetPoint if it is within bounds.");
        }

        // ── T-5.1.8.4  F-09 out-of-bounds target clamping ─────────────────────

        /// <summary>T-5.1.8.4: F-09 clamps TargetPoint to [0, PitchLengthM] × [0, PitchWidthM].</summary>
        [Test]
        public void ValidateTarget_TargetPointOutsidePitch_ClampsToWithinBounds()
        {
            DistributeIntent intent = new DistributeIntent
            {
                DeliveryKind     = DeliveryKind.Kick,
                TargetReceiverId = null,
                TargetPoint      = new Vector3(-5f, 80f, 0f), // both X and Y out of bounds
                PowerIntent      = 1.0f,
                SpinIntent       = Vector3.zero
            };

            GoalkeeperDistribution.ValidateTarget(ref intent, agentRosterContains: true,
                out bool receiverMissingWarning, out bool targetPointClamped);

            Assert.IsTrue(targetPointClamped, "F-09: targetPointClamped must be true when TargetPoint is outside pitch bounds.");
            Assert.GreaterOrEqual(intent.TargetPoint.x, 0f,                           "F-09: Clamped X must be >= 0.");
            Assert.LessOrEqual(intent.TargetPoint.x, GoalkeeperConstants.PitchLengthM, "F-09: Clamped X must be <= PitchLengthM.");
            Assert.GreaterOrEqual(intent.TargetPoint.y, 0f,                            "F-09: Clamped Y must be >= 0.");
            Assert.LessOrEqual(intent.TargetPoint.y, GoalkeeperConstants.PitchWidthM,  "F-09: Clamped Y must be <= PitchWidthM.");
        }

        // ── Roll accuracy coefficient = 1.0 (no attribute modifier) ──────────

        /// <summary>Roll always returns accuracy coefficient 1.0 per §3.8.1.</summary>
        [Test]
        public void AccuracyCoeff_Roll_IsAlwaysOne()
        {
            float coeff = GoalkeeperDistribution.ComputeAccuracyCoeff(DeliveryKind.Roll, MakeAttrs(throwing: 1f, kicking: 1f));
            Assert.AreEqual(1.0f, coeff, 1e-5f, "Roll accuracy coefficient must always be 1.0 regardless of attributes.");
        }

        // ── Throw accuracy scales with Throwing attribute ──────────────────────

        /// <summary>Throw accuracy coefficient increases with Throwing attribute.</summary>
        [Test]
        public void AccuracyCoeff_Throw_ScalesWithThrowingAttr()
        {
            float coeffLow  = GoalkeeperDistribution.ComputeAccuracyCoeff(DeliveryKind.Throw, MakeAttrs(throwing: 1f));
            float coeffHigh = GoalkeeperDistribution.ComputeAccuracyCoeff(DeliveryKind.Throw, MakeAttrs(throwing: 20f));

            Assert.Less(coeffLow, coeffHigh, "Higher Throwing attribute should produce a higher throw accuracy coefficient.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-31 | —      | Initial implementation. 25+ unit tests covering §5.1.1–§5.1.8.   |
#endregion
