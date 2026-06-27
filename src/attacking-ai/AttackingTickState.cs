// File:     src/attacking-ai/AttackingTickState.cs
// Created:  2026-06-27
// Author:   —
// Spec:     Attacking AI #15 §3.3, §3.11; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view bundling an AttackingAITick's cross-tick state (per-agent role hysteresis,
//           per-team transition-hold state, frozen in-possession directive) so a host snapshot layer
//           can serialize it canonically for deterministic save/restore.

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Immutable view over an <see cref="AttackingAITick"/>'s cross-tick state — the per-agent
    /// <see cref="AttackHysteresisState"/> role dwell ledger, the per-team
    /// <see cref="TransitionHoldState"/> (possession-loss countdown + previous phase), and the frozen
    /// in-possession <see cref="AttackDirective"/> the transition controller emits during the hold window.
    /// Produced by <see cref="AttackingAITick.CaptureState"/> for the Match Engine snapshot layer
    /// (design note §2.6), the attacking analogue of the Positioning / Pressing / Defensive seams.
    ///
    /// <see cref="Hysteresis"/> is a reference to the live, allocated-once-per-match array keyed by
    /// EntityId (not a copy); the host reads it for serialization and MUST NOT mutate it outside a
    /// sanctioned restore path.
    /// </summary>
    public readonly struct AttackingTickState
    {
        /// <summary>Per-agent role hysteresis (current/candidate role + dwell counters), by EntityId.</summary>
        public readonly AttackHysteresisState[] Hysteresis;

        /// <summary>Per-team transition-hold countdown + previous committed phase.</summary>
        public readonly TransitionHoldState Transition;

        /// <summary>The last in-possession directive, frozen for emit during the transition-hold window.</summary>
        public readonly AttackDirective LastInPossDirective;

        /// <summary>Bundles the live cross-tick state references/values into an immutable view.</summary>
        public AttackingTickState(
            AttackHysteresisState[] hysteresis,
            in TransitionHoldState transition,
            in AttackDirective lastInPossDirective)
        {
            Hysteresis          = hysteresis;
            Transition          = transition;
            LastInPossDirective = lastInPossDirective;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | attacking-hysteresis snapshot view (parallel to Positioning /  |
// |         |            |        | Pressing / Defensive seams).                                  |
#endregion
