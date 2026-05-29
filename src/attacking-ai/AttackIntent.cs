// File:     src/attacking-ai/AttackIntent.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.2, FR-AT-002, FR-AT-011, Code Standards #20
// Purpose:  Per-agent attacking output. RunParameters is non-null only when Role == Runner.
//           ValidThroughTick equals currentTick; stale reads (<currentTick) fall back to #12 slot.

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Per-agent attacking role and optional run descriptor for one 10 Hz tick.
    /// Written by <see cref="AttackingAITick"/>; read by the match orchestrator.
    /// Contributes to the per-tick determinism digest (FR-AT-004). Attacking AI #15 §2.2.2.
    ///
    /// <para><see cref="RunParameters"/> is non-null only when
    /// <see cref="Role"/> == <see cref="AttackRole.Runner"/>. Consumers must null-check
    /// before reading.</para>
    ///
    /// <para><see cref="ValidThroughTick"/> equals currentTick. A consumer reading an intent
    /// where ValidThroughTick &lt; currentTick must treat it as stale and fall back to the
    /// agent's #12 baseline slot.</para>
    /// </summary>
    public readonly struct AttackIntent
    {
        /// <summary>EntityId of the agent this intent applies to.</summary>
        public int             AgentEntityId    { get; }

        /// <summary>Attacking role assigned for this tick.</summary>
        public AttackRole      Role             { get; }

        /// <summary>
        /// Run parameters; non-null only when <see cref="Role"/> == <see cref="AttackRole.Runner"/>.
        /// Null for SUPPORT_BALL, HOLD_WIDTH, and WEAK_SIDE agents.
        /// </summary>
        public RunParameters?  RunParameters    { get; }

        /// <summary>Tick index when this intent was produced. Equals currentTick (§2.2.2).</summary>
        public int             ValidThroughTick { get; }

        /// <summary>Constructs an AttackIntent with optional run parameters.</summary>
        public AttackIntent(int agentEntityId, AttackRole role, RunParameters? runParameters, int validThroughTick)
        {
            AgentEntityId    = agentEntityId;
            Role             = role;
            RunParameters    = runParameters;
            ValidThroughTick = validThroughTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
