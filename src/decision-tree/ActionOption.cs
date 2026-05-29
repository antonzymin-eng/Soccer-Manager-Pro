// File:     src/decision-tree/ActionOption.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.3, §3.1, §3.2, Code Standards #20
// Purpose:  Internal struct representing one candidate action. Extended beyond the
//           §2.2.3 definition to carry type-specific scoring inputs generated in §3.1
//           and consumed by §3.2 (UtilityScorer). Only fields relevant to the selected
//           ActionType are populated; all others default to zero.

using UnityEngine;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// One candidate action with all type-specific scoring data.
    /// Produced by OptionGenerator, scored by UtilityScorer, selected by ActionSelector.
    /// Internal: not exposed outside the decision-tree assembly.
    /// Decision Tree #8 §2.2.3, §3.1, §3.2.
    /// </summary>
    internal struct ActionOption
    {
        // ── Universal ─────────────────────────────────────────────────────────

        public ActionType Type;
        public int         TargetAgentId;       // PASS, PRESS: target agent; -1 otherwise
        public Vector2     TargetPosition;       // world-space XY target

        /// <summary>Computed by UtilityScorer (§3.2). 0 before scoring.</summary>
        public float BaseUtility;

        /// <summary>After noise injection by ActionSelector (§3.3). 0 before selection.</summary>
        public float EffectiveUtility;

        // ── PASS fields ───────────────────────────────────────────────────────

        /// <summary>Raw pass lane score before goal-direction modifier (§3.1.3.3).</summary>
        public float PassLaneScore;

        /// <summary>AdjustedPassLaneScore = PassLaneScore × GoalDirectionModifier. Used by §3.2.2.</summary>
        public float AdjustedPassLaneScore;

        /// <summary>Cosine similarity of pass direction vs goal direction (§3.1.3.5).</summary>
        public float GoalDirectionCosine;

        /// <summary>Pass type derived from distance/angle at generation time (§3.1.3.4).</summary>
        public PassType DerivedPassType;

        /// <summary>Cross sub-type; valid only when DerivedPassType == Cross (§3.1.3.4).</summary>
        public CrossSubType DerivedCrossSubType;

        /// <summary>Distance to target teammate (metres). Maps to PassRequest.IntendedDistance.</summary>
        public float IntendedDistance;

        /// <summary>Pass urgency [0,1]. Derived from PressureScalar × URGENCY_PRESSURE_SCALE (§3.5.2).</summary>
        public float Urgency;

        /// <summary>True if target direction requires the agent's non-preferred foot (§3.5.2).</summary>
        public bool IsWeakFoot;

        // ── SHOOT fields ──────────────────────────────────────────────────────

        /// <summary>Fraction of goal unobstructed by blockers (§3.1.4.3). Used by §3.2.3.</summary>
        public float GoalOpeningScore;

        /// <summary>Shooting power intent [0.1, 1.0]. Derived from GoalOpeningScore × A_Finishing (§3.5.3).</summary>
        public float PowerIntent;

        /// <summary>Ball contact zone — drives spin and launch angle (§3.5.3).</summary>
        public ContactZone DerivedContactZone;

        /// <summary>Spin intent [0,1]. Default depends on ContactZone (§3.5.3).</summary>
        public float SpinIntent;

        /// <summary>Goal-normalised placement target [0,1]×[0,1] (§3.5.3).</summary>
        public Vector2 PlacementTarget;

        /// <summary>Distance from agent to opponent goal centre at generation time.</summary>
        public float DistanceToGoal;

        // ── DRIBBLE fields ────────────────────────────────────────────────────

        /// <summary>Best directional space score [0,1] from 8-sector scan (§3.1.5.2).</summary>
        public float SpaceScore;

        /// <summary>Best dribble direction unit vector from 8-sector scan (§3.1.5.2).</summary>
        public Vector2 BestDribbleDirection;

        // ── PRESS fields ──────────────────────────────────────────────────────

        /// <summary>Proximity score = 1 − distance/PRESS_TRIGGER_DISTANCE (§3.1.8.3).</summary>
        public float ProximityScore;

        // ── INTERCEPT fields ──────────────────────────────────────────────────

        /// <summary>Time-feasibility score = 1 − timeToIntercept/MAX_INTERCEPT_TIME (§3.1.9.3).</summary>
        public float InterceptFeasibilityScore;

        /// <summary>Earliest time (seconds) at which agent can reach intercept point (§3.1.9.3).</summary>
        public float TimeToIntercept;

        // ── MOVE fields ───────────────────────────────────────────────────────

        /// <summary>Distance from agent position to formation slot (§3.1.7.3).</summary>
        public float DistanceToSlot;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
