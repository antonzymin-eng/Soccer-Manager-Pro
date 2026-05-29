// File:     src/decision-tree/TacticalContext.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.6, Code Standards #20
// Purpose:  Team tactical instructions delivered to each agent's Decision Tree.
//           Stage 0: hardcoded defaults via Stage0Default(formationSlot).
//           Stage 1+: Formation System (Positioning AI #12) populates live values.

using UnityEngine;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Team tactical instructions for one agent at one heartbeat.
    /// Stage 0: both teams use Stage0Default (MEDIUM pressing, MIXED passing, 0.5 depth).
    /// Stage 1+: Positioning AI #12 populates live per-team values.
    /// Decision Tree #8 §2.2.6.
    /// </summary>
    public struct TacticalContext
    {
        // ── Team Instructions ─────────────────────────────────────────────────

        /// <summary>Team pressing intensity. Governs PRESS/INTERCEPT/HOLD multipliers (§3.4.3).</summary>
        public PressingMode Pressing;

        /// <summary>Team passing style. Governs long/short PASS and HOLD multipliers (§3.4.4).</summary>
        public PassingStyle Passing;

        /// <summary>
        /// Defensive line depth [0.0 = deepest, 1.0 = highest line]. Default 0.5.
        /// Adjusts formation slot Y positions (§3.4.5).
        /// </summary>
        public float DefensiveLineDepth;

        // ── Formation Slot ────────────────────────────────────────────────────

        /// <summary>
        /// Pre-computed formation slot for this agent. Set by Stage0Default or by
        /// Positioning AI #12 at Stage 1. Used by MOVE_TO_POSITION option generation (§3.1.7).
        /// </summary>
        private Vector2 _formationSlot;

        // ── Stage 1+ Stub Fields ──────────────────────────────────────────────
        // These fields are null at Stage 0. Defined here per spec amendments:
        //   ERR-014-001: TacticalContext.MarkDirective? (null at Stage 0)
        //   ERR-015-002: TacticalContext.AttackIntent[]? (null at Stage 0)
        // Concrete types will be provided by Defensive AI #14 and Attacking AI #15.

        /// <summary>Stage 1+: mark directive from Defensive AI #14. Always null at Stage 0. ERR-014-001.</summary>
        public bool HasMarkDirective;       // stub; Stage 1+ replaces with MarkDirective?

        /// <summary>Stage 1+: attack intent array from Attacking AI #15. Always false at Stage 0. ERR-015-002.</summary>
        public bool HasAttackIntent;        // stub; Stage 1+ replaces with AttackIntent[]?

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the Stage 0 default for a single agent.
        /// formationSlot: this agent's positional anchor on the pitch (XY pitch space).
        /// Both teams use identical defaults at Stage 0 (§2.2.6).
        /// </summary>
        public static TacticalContext Stage0Default(Vector2 formationSlot)
        {
            return new TacticalContext
            {
                Pressing           = PressingMode.MEDIUM,
                Passing            = PassingStyle.MIXED,
                DefensiveLineDepth = 0.5f,
                _formationSlot     = formationSlot,
                HasMarkDirective   = false,
                HasAttackIntent    = false
            };
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the formation slot target for the given agent.
        /// Stage 0: returns the per-agent slot set at construction.
        /// Stage 1+: Formation System dynamically adjusts slots.
        /// §3.1.7.2.
        /// </summary>
        public Vector2 GetFormationSlot(int agentId) => _formationSlot;

        /// <summary>
        /// Formation slot adjusted for DefensiveLineDepth offset.
        /// Returns _formationSlot.y ± (DefensiveLineDepth − 0.5) × DEFENSIVE_LINE_DEPTH_RANGE.
        /// §3.4.5.
        /// </summary>
        public Vector2 GetAdjustedFormationSlot(int agentId)
        {
            float yAdjust = (DefensiveLineDepth - 0.5f) * TacticalWeights.DefensiveLineDepthRange;
            return new Vector2(_formationSlot.x, _formationSlot.y + yAdjust);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
