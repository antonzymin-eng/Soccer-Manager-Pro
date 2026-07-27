// File:     src/goalkeeper-mechanics/GoalkeeperStateMachine.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Modified: 2026-07-27 (§5.Z.17 / ERR-011-002: parameters renamed from the KEEPER's perspective, new Anticipate → Set exit, Recovering → Resting re-anchored to the far third)
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1, Code Standards #20
// Purpose:  Pure state evaluator for the GK state machine. Implements both the 10 Hz tactical
//           transition pass and the 60 Hz physics transition pass. No side effects.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Pure state evaluator for the GK state machine (§3.1).
    /// EvaluateTacticalTransition handles 10 Hz triggers; EvaluatePhysicsTransition handles 60 Hz triggers.
    /// No side effects — callers apply the returned state to the GK agent record.
    /// Goalkeeper Mechanics #11 §3.1.
    /// </summary>
    public static class GoalkeeperStateMachine
    {
        /// <summary>
        /// Evaluates all 10 Hz tactical-tick state transitions per §3.1.1.
        /// Called once per GK per tactical tick in #16 §3.2 entity order.
        /// Returns the new state; caller stores result. No side effects.
        /// Goalkeeper Mechanics #11 §3.1.2 / §4.6.1.
        /// </summary>
        /// <param name="currentState">Current GK state.</param>
        /// <param name="ballState">Current BallState snapshot for this tick. Reserved for future formula use; ball-third checks use pre-computed booleans.</param>
        /// <param name="hasSaveIntent">True if Decision Tree has committed a SaveIntent this tick.</param>
        /// <param name="hasRushIntent">True if Decision Tree has committed a RushIntent this tick.</param>
        /// <param name="hasDistributeIntent">True if Decision Tree has committed a DistributeIntent this tick.</param>
        /// <param name="anticipationScore">Decision Tree anticipation score [0, 1] for this GK. §3.1.</param>
        /// <param name="rushCommitmentLevel">RushIntent commitment level [0, 1] (0 if no intent). §3.1.</param>
        /// <param name="currentTick">Current 10 Hz tick index.</param>
        /// <param name="claimTick">Tick at which HandsOnBall was entered (-1 if not applicable). §3.1.</param>
        /// <param name="releaseTickEarliest">Earliest tick at which distribution may be released. §3.1.</param>
        /// <param name="recoveryCooldownEndTick">Tick at which recovery cooldown expires. §3.1.</param>
        /// <param name="gkPosition">Current GK XY position in metres. §3.1 / §3.3.0.</param>
        /// <param name="gkBaselineSlot">GK baseline slot from Positioning AI #12. §3.3.0.</param>
        /// <param name="ballThreateningOwnGoal">True if the ball is in the third in front of the goal THIS
        /// keeper defends. Named from the KEEPER's perspective, not a team's: the previous name
        /// ("ballInAttackingThird") read as "my team's attacking third" at the call site and as "the
        /// attacker's attacking third" here — opposite ends of the pitch — and the caller supplied the
        /// former while this machine assumed the latter (ERR-011-002). §3.1.</param>
        /// <param name="ballSafelyUpfield">True if the ball is in the far third, away from the goal this
        /// keeper defends: play is at the other end and the keeper may stand down. §3.1.</param>
        public static GoalkeeperState EvaluateTacticalTransition(
            GoalkeeperState currentState,
            BallState ballState,
            bool hasSaveIntent,
            bool hasRushIntent,
            bool hasDistributeIntent,
            float anticipationScore,
            float rushCommitmentLevel,
            int currentTick,
            int claimTick,
            int releaseTickEarliest,
            int recoveryCooldownEndTick,
            Vector2 gkPosition,
            Vector2 gkBaselineSlot,
            bool ballThreateningOwnGoal,
            bool ballSafelyUpfield)
        {
            switch (currentState)
            {
                case GoalkeeperState.Resting:
                {
                    // Resting → Set: the ball enters the third in front of the goal this keeper defends
                    if (ballThreateningOwnGoal)
                    {
                        return GoalkeeperState.Set;
                    }
                    return GoalkeeperState.Resting;
                }

                case GoalkeeperState.Set:
                {
                    // Set → Anticipate: anticipation score exceeds threshold
                    if (anticipationScore > GoalkeeperConstants.AnticipateThreshold)
                    {
                        return GoalkeeperState.Anticipate;
                    }
                    // Set → Rushing: rush intent committed with sufficient commitment
                    if (hasRushIntent && rushCommitmentLevel > GoalkeeperConstants.RushCommitThreshold)
                    {
                        return GoalkeeperState.Rushing;
                    }
                    // Set → Resting: the ball no longer threatens
                    if (!ballThreateningOwnGoal)
                    {
                        return GoalkeeperState.Resting;
                    }
                    return GoalkeeperState.Set;
                }

                case GoalkeeperState.Anticipate:
                {
                    // Anticipate → Diving: SaveIntent committed with valid target hand
                    if (hasSaveIntent)
                    {
                        return GoalkeeperState.Diving;
                    }
                    // Anticipate → Rushing: rush intent takes priority per #8 priority rules
                    if (hasRushIntent && rushCommitmentLevel > GoalkeeperConstants.RushCommitThreshold)
                    {
                        return GoalkeeperState.Rushing;
                    }
                    // Anticipate → Set: the threat has passed. ERR-011-002 — Anticipate previously had NO
                    // exit but a dive or a rush, so a keeper that entered it never left: measured, keepers
                    // held Anticipate for 76-92% of every match. Anticipate is a coiled, committed posture;
                    // standing in it for eighty minutes is neither football nor what §3.1 describes.
                    if (!ballThreateningOwnGoal)
                    {
                        return GoalkeeperState.Set;
                    }
                    return GoalkeeperState.Anticipate;
                }

                case GoalkeeperState.HandsOnBall:
                {
                    // HandsOnBall → Distributing: DistributeIntent committed and release tick reached
                    if (hasDistributeIntent && currentTick >= releaseTickEarliest)
                    {
                        return GoalkeeperState.Distributing;
                    }
                    // HandsOnBall → HandsOnBall: forced release after 6-second rule (handled by orchestrator,
                    // but state remains HandsOnBall until distribution is resolved — transition happens in 60 Hz)
                    if (claimTick >= 0 && (currentTick - claimTick) >= GoalkeeperConstants.GK_HOLD_MAX_TICKS)
                    {
                        // Forced release — return Distributing to signal orchestrator must distribute
                        return GoalkeeperState.Distributing;
                    }
                    return GoalkeeperState.HandsOnBall;
                }

                case GoalkeeperState.Recovering:
                {
                    // Recovering → Set: cooldown elapsed OR already at baseline
                    bool cooldownElapsed = currentTick >= recoveryCooldownEndTick;
                    float distToBaseline = Vector2.Distance(gkPosition, gkBaselineSlot);
                    bool atBaseline = distToBaseline <= GoalkeeperConstants.GkReactiveRadiusM;

                    if (cooldownElapsed || atBaseline)
                    {
                        return GoalkeeperState.Set;
                    }
                    // Recovering → Resting: play is at the far end, so there is nothing to recover FOR.
                    //
                    // This is a deliberate CHANGE OF REGION, not just a rename (ERR-011-002). The old
                    // parameter (`ballInDefensiveThird`) was computed as the keeper's OWN defensive
                    // third — right for its name, but it made the keeper stand fully down while the ball
                    // was in its own box, which is the same wrongness ERR-011-002 names on the entry
                    // side. `Resting` is the stand-down state; the ball being at the OTHER end is what
                    // licenses it. Note this transition stays reachable either way: `Recovering → Set`
                    // above is gated on the recovery cooldown and baseline distance, not on the ball.
                    if (ballSafelyUpfield)
                    {
                        return GoalkeeperState.Resting;
                    }
                    return GoalkeeperState.Recovering;
                }

                case GoalkeeperState.OneOnOne:
                {
                    // OneOnOne → Diving: SaveIntent committed (1v1 dive path; KD-20 coefficients apply)
                    if (hasSaveIntent)
                    {
                        return GoalkeeperState.Diving;
                    }
                    return GoalkeeperState.OneOnOne;
                }

                // Diving, Airborne, Smothered, Distributing, Rushing transitions are 60 Hz physics-driven.
                default:
                    return currentState;
            }
        }

        /// <summary>
        /// Evaluates all 60 Hz physics-tick state transitions per §3.1.1.
        /// Handles contact events, ground re-entry, rush contact detection, etc.
        /// Returns the new state; caller stores result. No side effects.
        /// Goalkeeper Mechanics #11 §3.1.2 / §4.6.2.
        /// </summary>
        /// <param name="currentState">Current GK state.</param>
        /// <param name="handBallContactOccurred">True if Collision System #3 reports a hand-ball event this frame.</param>
        /// <param name="handlingQualityScalar">Handling quality scalar [0, 1] from §3.5 (if contact occurred). §3.1.</param>
        /// <param name="groundReEntry">True if GK Z ≤ 0 (dive ended without contact). §3.3.5.</param>
        /// <param name="distributionReleaseReached">True if windup elapsed; ball release frame reached. §3.1.</param>
        /// <param name="rushBallIntercepted">True if F-08 ball-interception abort condition met. §3.7.3.</param>
        /// <param name="attackerWithinOneVsOneRadius">True if attacker with ball is within ONE_VS_ONE_TRIGGER_RADIUS_M. §3.7.</param>
        /// <param name="gkWithinSmotherRadius">True if GK is within SMOTHER_TRIGGER_RADIUS_M of attacker. §3.7.</param>
        /// <param name="shotEventDetected">True if ShotExecutedEvent was consumed this frame (Anticipate early trigger). §3.1.</param>
        public static GoalkeeperState EvaluatePhysicsTransition(
            GoalkeeperState currentState,
            bool handBallContactOccurred,
            float handlingQualityScalar,
            bool groundReEntry,
            bool distributionReleaseReached,
            bool rushBallIntercepted,
            bool attackerWithinOneVsOneRadius,
            bool gkWithinSmotherRadius,
            bool shotEventDetected)
        {
            switch (currentState)
            {
                case GoalkeeperState.Set:
                {
                    // Set → Anticipate early: ShotExecutedEvent received (predictive path)
                    if (shotEventDetected)
                    {
                        return GoalkeeperState.Anticipate;
                    }
                    return GoalkeeperState.Set;
                }

                case GoalkeeperState.Anticipate:
                {
                    // Anticipate → Anticipate: ShotExecutedEvent triggers early anticipate (already in state)
                    return GoalkeeperState.Anticipate;
                }

                case GoalkeeperState.Diving:
                {
                    // Diving → Airborne: dive launch impulse has been applied (caller applies this frame)
                    return GoalkeeperState.Airborne;
                }

                case GoalkeeperState.Airborne:
                {
                    if (handBallContactOccurred)
                    {
                        // Airborne → HandsOnBall: caught
                        if (handlingQualityScalar >= GoalkeeperConstants.CatchThreshold)
                        {
                            return GoalkeeperState.HandsOnBall;
                        }
                        // Airborne → Recovering: parry / deflect / spill / miss
                        return GoalkeeperState.Recovering;
                    }
                    // Airborne → Recovering: ground re-entry without contact (F-01 / F-02 / F-03)
                    if (groundReEntry)
                    {
                        return GoalkeeperState.Recovering;
                    }
                    return GoalkeeperState.Airborne;
                }

                case GoalkeeperState.Distributing:
                {
                    // Distributing → Recovering: release frame reached
                    if (distributionReleaseReached)
                    {
                        return GoalkeeperState.Recovering;
                    }
                    return GoalkeeperState.Distributing;
                }

                case GoalkeeperState.Rushing:
                {
                    // Rushing → Recovering: F-08 ball intercepted (only abort trigger per KD-15)
                    if (rushBallIntercepted)
                    {
                        return GoalkeeperState.Recovering;
                    }
                    // Rushing → Smothered: hand-ball contact during rush
                    if (handBallContactOccurred)
                    {
                        return GoalkeeperState.Smothered;
                    }
                    // Rushing → OneOnOne: attacker with ball enters trigger radius
                    if (attackerWithinOneVsOneRadius)
                    {
                        return GoalkeeperState.OneOnOne;
                    }
                    return GoalkeeperState.Rushing;
                }

                case GoalkeeperState.Smothered:
                {
                    if (handBallContactOccurred)
                    {
                        // Smothered → HandsOnBall: contact resolves as catch
                        if (handlingQualityScalar >= GoalkeeperConstants.CatchThreshold)
                        {
                            return GoalkeeperState.HandsOnBall;
                        }
                        // Smothered → Recovering: contact below catch threshold
                        return GoalkeeperState.Recovering;
                    }
                    return GoalkeeperState.Smothered;
                }

                case GoalkeeperState.OneOnOne:
                {
                    // OneOnOne → Smothered: GK closes within smother radius
                    if (gkWithinSmotherRadius)
                    {
                        return GoalkeeperState.Smothered;
                    }
                    return GoalkeeperState.OneOnOne;
                }

                default:
                    return currentState;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1 | 2026-07-27 | — | ERR-011-002. ballInAttackingThird/ballInDefensiveThird renamed|
// |     |            |   | ballThreateningOwnGoal/ballSafelyUpfield — the old names read one way at the|
// |     |            |   | call site and the opposite way here, and the caller supplied the former while|
// |     |            |   | this machine assumed the latter. New Anticipate → Set exit (KD-S4): Anticipate|
// |     |            |   | had NO exit but a dive or a rush, so keepers held it 76-92% of every match.|
// |     |            |   | Recovering → Resting re-anchored to ballSafelyUpfield — a deliberate change of|
// |     |            |   | REGION, recorded in the design note §4.3: standing fully down while the ball is|
// |     |            |   | in your own box is the same wrongness ERR-011-002 names on the entry side.|
#endregion
