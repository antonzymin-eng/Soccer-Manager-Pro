// File:     src/client-app/ClientChangeRecord.cs
// Created:  2026-10-08
// Modified: 2026-10-15 (Match Flow §6: accepted is not executed)
// Modified (prior): 2026-10-08
// Author:   —
// Spec:     S0 journey §14.5, binding contracts §2.3, Code Standards #20
// Purpose:  Session-local request identity and persistent outcome without prediction or inferred refusal reasons.

using TacticalDirector.MatchClientCore;

namespace TacticalDirector.ClientApp
{
    /// <summary>Read-only request history row; only its owning adapter can settle it.</summary>
    public sealed class ClientChangeRecord
    {
        /// <summary>The exact command captured before its one dispatch.</summary>
        public ManagerCommand Command { get; }
        /// <summary>Captured outgoing identity; later pitch occupants cannot rewrite history.</summary>
        public MatchPlayerIdentity Outgoing { get; }
        /// <summary>Captured bench identity.</summary>
        public MatchPlayerIdentity Incoming { get; }
        /// <summary>Persistent outcome; Pending is an enqueue fact only.</summary>
        public ClientChangeStatus Status { get; internal set; }
        /// <summary>Driver-stamped outcome tick; meaningful only for Applied/Refused.</summary>
        public ulong OutcomeTick { get; internal set; }
        internal bool EngineAccepted { get; set; }
        internal ulong AcceptedTick { get; set; }

        internal ClientChangeRecord(in ManagerCommand command, in MatchPlayerIdentity outgoing, in MatchPlayerIdentity incoming)
        {
            Command = command;
            Outgoing = outgoing;
            Incoming = incoming;
            Status = ClientChangeStatus.Pending;
        }
    }

    /// <summary>Client request outcomes independent of the simulation's command enum.</summary>
    public enum ClientChangeStatus
    {
        /// <summary>Registered and dispatched; no outcome suffix yet.</summary>
        Pending,
        /// <summary>Exact new applied suffix evidence.</summary>
        Applied,
        /// <summary>Exact new refused suffix evidence; no reason is provided by the engine.</summary>
        Refused,
        /// <summary>Still unmatched after the completed end barrier.</summary>
        NotApplied,
        /// <summary>Dispatch threw locally.</summary>
        SendFailure
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Captured payload/identity and persistent request status. |
// | 1.1     | 2026-10-15 | —      | Match Flow §6 (PR #505): internal EngineAccepted / AcceptedTick, so an accepted substitution stays Pending until roster evidence shows it executed. |
#endregion
