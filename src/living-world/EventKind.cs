// File:     src/living-world/EventKind.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 §2.2.2, §2.2.6, Appendix C, Code Standards #20
// Purpose:  EventKind enum: the off-pitch event taxonomy recorded in a MemoryEpisode.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Classifies the off-pitch event an episode records. #22 §2.2.2 / Appendix C.
    ///
    /// OPEN ROSTER (#22 §2.2.6): the full taxonomy is finalised at implementation from the vol-2 §7
    /// media/narrative event set + manager-choice outcomes. ORDINAL STABILITY (FR-LW-028): members are
    /// embedded in saved MemoryEpisode state — APPEND-only; the stability test grows with the roster and
    /// locks every member that exists. The seed members below are illustrative and APPEND-only.
    /// </summary>
    public enum EventKind : byte
    {
        /// <summary>No / unspecified event (neutral default).</summary>
        None = 0,

        /// <summary>Manager publicly criticised the entity.</summary>
        ManagerCriticism = 1,

        /// <summary>Manager publicly defended the entity.</summary>
        ManagerDefence = 2,

        /// <summary>Entity (player) was benched / dropped.</summary>
        Benching = 3,

        /// <summary>A contract or transfer request was snubbed.</summary>
        ContractSnub = 4,

        /// <summary>Media generated a transfer rumour (vol-2 §7.3).</summary>
        TransferRumour = 5,

        /// <summary>A press-conference trap exchange (vol-2 §7.1).</summary>
        PressConferenceTrap = 6,
    }
}
