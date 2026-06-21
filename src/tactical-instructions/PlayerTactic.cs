// File:     src/tactical-instructions/PlayerTactic.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.3, Code Standards #20
// Purpose:  Per-agent tactic: behavioural role + duty + individual instructions.
//           Immutable-per-match input carrier with an identity factory.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Per-agent tactic (§2.2.3): a behavioural <see cref="PlayerRole"/>, a <see cref="Duty"/>, and the
    /// agent's <see cref="PlayerInstructions"/>. Property order matches the Appendix B canonical
    /// snapshot order. <see cref="Default(PlayerRole)"/> is the behavioural identity (FR-TI-031).
    /// </summary>
    /// <remarks>
    /// HAZARD — <c>default(PlayerTactic)</c> is NOT the identity: its embedded
    /// <see cref="PlayerInstructions"/> defaults to a man-mark on agent 0 (see that type). Use
    /// <see cref="Default(PlayerRole)"/>; treat default-valued instances as malformed.
    /// </remarks>
    public readonly struct PlayerTactic
    {
        /// <summary>Behavioural role (modifier on a position; KD-3). §2.2.3.</summary>
        public PlayerRole Role { get; }

        /// <summary>Per-agent duty. §2.2.3.</summary>
        public Duty Duty { get; }

        /// <summary>Per-agent individual instructions. §2.2.3.</summary>
        public PlayerInstructions Instructions { get; }

        /// <summary>Constructs a <see cref="PlayerTactic"/>.</summary>
        public PlayerTactic(PlayerRole role, Duty duty, PlayerInstructions instructions)
        {
            Role = role;
            Duty = duty;
            Instructions = instructions;
        }

        /// <summary>
        /// Identity tactic for the given role (§2.2.3): <see cref="Duty.Support"/> and
        /// <see cref="PlayerInstructions.Default"/>. With <see cref="PlayerRole.Default"/> this is the
        /// full no-instruction baseline (FR-TI-031).
        /// </summary>
        public static PlayerTactic Default(PlayerRole role) =>
            new PlayerTactic(role, Duty.Support, PlayerInstructions.Default);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).                               |
// | 1.1     | 2026-06-21 | —      | AR-1 L-1: <remarks> default(PlayerTactic) is not the identity. |
#endregion
