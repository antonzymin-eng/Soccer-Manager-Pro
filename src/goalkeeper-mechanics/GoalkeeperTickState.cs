// File:     src/goalkeeper-mechanics/GoalkeeperTickState.cs
// Created:  2026-07-23
// Modified: 2026-07-23
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2, §3.1–§3.8; gk-heading-engine-integration-design.md Phase 2;
//           Match Engine design note §2.6 (snapshot seam); Code Standards #20
// Purpose:  Read-only view bundling a GoalkeeperMechanics orchestrator's full per-GK cross-tick state
//           (state machine, intents + latches, dive/reaction/hold-rule buffers) so a host snapshot layer
//           can serialize it canonically for deterministic save/restore.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Immutable view over a <see cref="GoalkeeperMechanics"/>'s cross-tick state — every per-GK array the
    /// orchestrator carries across ticks. Produced by <see cref="GoalkeeperMechanics.CaptureState"/> for the
    /// Match Engine snapshot layer (design note §2.6), the goalkeeper analogue of the Pressing
    /// <see cref="TacticalDirector.PressingAI.PressingTickState"/> / Defensive / Attacking / Perception seams.
    ///
    /// The array fields are references to the live, allocated-once-per-match containers (not copies) when
    /// produced by <see cref="GoalkeeperMechanics.CaptureState"/>; the host reads them for serialization and
    /// MUST NOT mutate them outside a sanctioned restore path. On the read side the host constructs a fresh
    /// view whose arrays hold the deserialized values and hands it to
    /// <see cref="GoalkeeperMechanics.RestoreState"/>, which copies element-wise into the live containers.
    /// Every array is sized to <c>GoalkeeperConstants.MaxGkAgents</c> (keeper index == team id). The field
    /// declaration order matches the orchestrator's private-array declaration order.
    /// </summary>
    public readonly struct GoalkeeperTickState
    {
        /// <summary>Per-GK state-machine state.</summary>
        public readonly GoalkeeperState[] States;

        /// <summary>Per-GK attribute snapshot (read once per tactical tick, held for the 60 Hz phase).</summary>
        public readonly GoalkeeperAgentAttributes[] Attrs;

        /// <summary>Per-GK in-flight contact state (dive/airborne pipeline).</summary>
        public readonly GkContactState[] ContactStates;

        /// <summary>Per-GK committed save intent (valid while <see cref="SaveIntentActive"/>).</summary>
        public readonly SaveIntent[] SaveIntents;

        /// <summary>Per-GK save-intent-active latch.</summary>
        public readonly bool[] SaveIntentActive;

        /// <summary>Per-GK committed rush intent (valid while <see cref="RushIntentActive"/>).</summary>
        public readonly RushIntent[] RushIntents;

        /// <summary>Per-GK rush-intent-active latch.</summary>
        public readonly bool[] RushIntentActive;

        /// <summary>Per-GK committed distribution intent (valid while <see cref="DistributeIntentActive"/>).</summary>
        public readonly DistributeIntent[] DistributeIntents;

        /// <summary>Per-GK distribute-intent-active latch.</summary>
        public readonly bool[] DistributeIntentActive;

        /// <summary>Per-GK positioning contract (the KD-13 gkBaselineSlot carrier).</summary>
        public readonly GoalkeeperPositioningContract[] PositioningContracts;

        /// <summary>Per-GK dive launch frame (-1 = no dive in flight).</summary>
        public readonly int[] DiveLaunchFrames;

        /// <summary>Per-GK dive duration (frames).</summary>
        public readonly int[] DiveDurationFrames;

        /// <summary>Per-GK dive peak hand-Z (m).</summary>
        public readonly float[] DivePeakHandZ;

        /// <summary>Per-GK dive lateral direction.</summary>
        public readonly float[] DiveDirectionLateral;

        /// <summary>Per-GK rush launch speed (m/s).</summary>
        public readonly float[] RushLaunchMps;

        /// <summary>Per-GK initial rush attacker id (-1 = none).</summary>
        public readonly int[] RushInitialAttackerId;

        /// <summary>Per-GK shot-detected tick (ms).</summary>
        public readonly float[] ShotDetectedTickMs;

        /// <summary>Per-GK required-reaction (ms).</summary>
        public readonly float[] RequiredReactionMs;

        /// <summary>Per-GK shot-event-pending latch.</summary>
        public readonly bool[] ShotEventPending;

        /// <summary>Per-GK claim tick (-1 = not holding).</summary>
        public readonly int[] ClaimTick;

        /// <summary>Per-GK earliest release tick (int.MaxValue sentinel).</summary>
        public readonly int[] ReleaseTickEarliest;

        /// <summary>Per-GK recovery-cooldown end tick.</summary>
        public readonly int[] RecoveryCooldownEndTick;

        /// <summary>Bundles the live cross-tick array references/values into an immutable view. Field order
        /// matches the orchestrator's private-array declaration order.</summary>
        public GoalkeeperTickState(
            GoalkeeperState[] states,
            GoalkeeperAgentAttributes[] attrs,
            GkContactState[] contactStates,
            SaveIntent[] saveIntents,
            bool[] saveIntentActive,
            RushIntent[] rushIntents,
            bool[] rushIntentActive,
            DistributeIntent[] distributeIntents,
            bool[] distributeIntentActive,
            GoalkeeperPositioningContract[] positioningContracts,
            int[] diveLaunchFrames,
            int[] diveDurationFrames,
            float[] divePeakHandZ,
            float[] diveDirectionLateral,
            float[] rushLaunchMps,
            int[] rushInitialAttackerId,
            float[] shotDetectedTickMs,
            float[] requiredReactionMs,
            bool[] shotEventPending,
            int[] claimTick,
            int[] releaseTickEarliest,
            int[] recoveryCooldownEndTick)
        {
            States                  = states;
            Attrs                   = attrs;
            ContactStates           = contactStates;
            SaveIntents             = saveIntents;
            SaveIntentActive        = saveIntentActive;
            RushIntents             = rushIntents;
            RushIntentActive        = rushIntentActive;
            DistributeIntents       = distributeIntents;
            DistributeIntentActive  = distributeIntentActive;
            PositioningContracts    = positioningContracts;
            DiveLaunchFrames        = diveLaunchFrames;
            DiveDurationFrames      = diveDurationFrames;
            DivePeakHandZ           = divePeakHandZ;
            DiveDirectionLateral    = diveDirectionLateral;
            RushLaunchMps           = rushLaunchMps;
            RushInitialAttackerId   = rushInitialAttackerId;
            ShotDetectedTickMs      = shotDetectedTickMs;
            RequiredReactionMs      = requiredReactionMs;
            ShotEventPending        = shotEventPending;
            ClaimTick               = claimTick;
            ReleaseTickEarliest     = releaseTickEarliest;
            RecoveryCooldownEndTick = recoveryCooldownEndTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-23 | —      | Initial implementation — GK/Heading engine-integration Phase 2      |
// |         |            |        | snapshot view over the orchestrator's per-GK cross-tick arrays      |
// |         |            |        | (parallel to PressingTickState / DefensiveTickState seams).         |
#endregion
