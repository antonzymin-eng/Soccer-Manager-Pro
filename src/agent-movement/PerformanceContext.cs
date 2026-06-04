// File:     src/agent-movement/PerformanceContext.cs
// Created:  2026-05-22
// Modified: 2026-06-03 (AR-7 fix pass)
// Author:   —
// Spec:     Agent Movement #2 §3.2.1–§3.2.2, Code Standards #20
// Purpose:  Attribute gateway applying form/context/career modifiers to raw player attributes.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Lightweight value type that gates all attribute reads through three modifier layers:
    /// Form (match-to-match fitness), Context (match situation), Career (age/condition).
    /// All three are in [0.0, 1.0]; combined = form × context × career.
    /// Locomotion systems call EvaluateAttribute() rather than reading attributes directly.
    /// Agent Movement #2 §3.2.1.
    /// </summary>
    public readonly struct PerformanceContext
    {
        /// <summary>Match-to-match form modifier [0.0–1.0]. 1.0 = peak form.</summary>
        public readonly float FormModifier;

        /// <summary>In-match situation modifier [0.0–1.0]. 1.0 = fully focused.</summary>
        public readonly float ContextModifier;

        /// <summary>Long-term career condition modifier [0.0–1.0]. 1.0 = prime career.</summary>
        public readonly float CareerModifier;

        private PerformanceContext(float form, float context, float career)
        {
            FormModifier = Mathf.Clamp01(form);
            ContextModifier = Mathf.Clamp01(context);
            CareerModifier = Mathf.Clamp01(career);
        }

        /// <summary>All modifiers at 1.0; used in neutral match conditions and tests.</summary>
        public static PerformanceContext CreateNeutral()
        {
            return new PerformanceContext(1.0f, 1.0f, 1.0f);
        }

        /// <summary>Construct with explicit modifier values.</summary>
        public static PerformanceContext Create(float form, float context, float career)
        {
            return new PerformanceContext(form, context, career);
        }

        /// <summary>
        /// Scales a raw integer attribute (1–20) by all three modifiers.
        /// Result is a float effective attribute in [0.0, 20.0].
        /// Agent Movement #2 §3.2.2.
        /// </summary>
        public float EvaluateAttribute(int rawAttribute)
        {
            // Spec #2 §3.5.1 pins raw player attributes to integer [1, 20]. Out-of-range input
            // is a caller-side programmer error (e.g., default(PlayerAttributes) leaves fields
            // at 0 and would propagate degenerate effective values into clamped formulas);
            // catch in development builds instead of silently absorbing it.
            Debug.Assert(rawAttribute >= PlayerAttributeConstants.AttributeMinInt
                         && rawAttribute <= PlayerAttributeConstants.AttributeMaxInt,
                "PerformanceContext.EvaluateAttribute: rawAttribute must be in [1, 20] per Spec #2 §3.5.1.");

            float combined = FormModifier * ContextModifier * CareerModifier;
            return rawAttribute * combined;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/. |
// | 1.2     | 2026-06-03 | —      | AR-5 fix: L-4 EvaluateAttribute asserts rawAttribute in [1, 20] (Spec #2 §3.5.1). Default-     |
// |         |            |        | initialised PlayerAttributes (all zeros) or other out-of-range inputs propagated degenerate    |
// |         |            |        | effective values into clamped locomotion formulas; the assert catches it in dev builds.        |
// | 1.3     | 2026-06-03 | —      | AR-7 fix: L-1 assert now uses canonical PlayerAttributeConstants.AttributeMinInt /             |
// |         |            |        | AttributeMaxInt (newly added) instead of (int)AttributeMin / (int)AttributeMax casts —        |
// |         |            |        | symmetric with the existing AttributeMinInt pattern at AgentTurning call sites.                |
#endregion
