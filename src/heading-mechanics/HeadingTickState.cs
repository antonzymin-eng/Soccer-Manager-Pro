// File:     src/heading-mechanics/HeadingTickState.cs
// Created:  2026-07-23
// Modified: 2026-07-23
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §3.2–§3.9; gk-heading-engine-integration-design.md Phase 2;
//           Match Engine design note §2.6 (snapshot seam); Code Standards #20
// Purpose:  Read-only view bundling a HeadingMechanics orchestrator's full per-agent cross-tick state
//           (intents + latches, per-frame contact state, ball-snapshot frames, attributes) so a host
//           snapshot layer can serialize it canonically for deterministic save/restore.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Immutable view over a <see cref="HeadingMechanics"/>'s cross-tick state — every per-agent array the
    /// orchestrator carries across ticks. Produced by <see cref="HeadingMechanics.CaptureState"/> for the
    /// Match Engine snapshot layer (design note §2.6), the heading analogue of the Pressing
    /// <see cref="TacticalDirector.PressingAI.PressingTickState"/> / Defensive / Attacking / Perception seams.
    ///
    /// The array fields are references to the live, allocated-once-per-match containers (not copies) when
    /// produced by <see cref="HeadingMechanics.CaptureState"/>; the host reads them for serialization and
    /// MUST NOT mutate them outside a sanctioned restore path. On the read side the host constructs a fresh
    /// view whose arrays hold the deserialized values and hands it to
    /// <see cref="HeadingMechanics.RestoreState"/>, which copies element-wise into the live containers.
    /// Every array is sized to <c>HeadingMechanicsConstants.MaxAgents</c>. The field declaration order
    /// matches the orchestrator's private-array declaration order.
    /// </summary>
    public readonly struct HeadingTickState
    {
        /// <summary>Per-agent committed header intent (valid while <see cref="IntentActive"/>).</summary>
        public readonly HeaderIntent[] Intents;

        /// <summary>Per-agent in-flight contact state (jump/contact pipeline; carries PrevFrameFacingDirection).</summary>
        public readonly HeaderContactState[] ContactStates;

        /// <summary>Per-agent intent-active latch.</summary>
        public readonly bool[] IntentActive;

        /// <summary>Per-agent ball-snapshot frame index.</summary>
        public readonly int[] BallSnapshotFrames;

        /// <summary>Per-agent attribute snapshot (read once per jump, held for the pipeline).</summary>
        public readonly HeadingAgentAttributes[] AgentAttrs;

        /// <summary>Bundles the live cross-tick array references/values into an immutable view. Field order
        /// matches the orchestrator's private-array declaration order.</summary>
        public HeadingTickState(
            HeaderIntent[] intents,
            HeaderContactState[] contactStates,
            bool[] intentActive,
            int[] ballSnapshotFrames,
            HeadingAgentAttributes[] agentAttrs)
        {
            Intents            = intents;
            ContactStates      = contactStates;
            IntentActive       = intentActive;
            BallSnapshotFrames = ballSnapshotFrames;
            AgentAttrs         = agentAttrs;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-23 | —      | Initial implementation — GK/Heading engine-integration Phase 2      |
// |         |            |        | snapshot view over the orchestrator's per-agent cross-tick arrays   |
// |         |            |        | (parallel to PressingTickState / DefensiveTickState seams).         |
#endregion
