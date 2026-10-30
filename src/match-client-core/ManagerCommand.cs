// File:     src/match-client-core/ManagerCommand.cs
// Created:  2026-07-24
// Modified: 2026-10-21 (Gate J J-02: Apply reports the engine's typed substitution refusal)
// Modified (prior): 2026-10-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2/§6),
//           Code Standards #20
// Purpose:  One typed manager game command — a value carrying only the data that maps onto a single
//           live engine mutator, plus the code to apply itself onto an ILiveMatchMutations surface.
//           Constructed on the UI thread via the factories, applied on the sim thread at the tick top.

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// An immutable manager game command. A value type (no per-command heap allocation on the enqueue
    /// path) discriminated by <see cref="Kind"/>; only the fields relevant to that kind are meaningful,
    /// the rest are <c>default</c> and never read by <see cref="Apply"/>. There is deliberately no path
    /// to poke engine internals — a command can only invoke one of the three live mutators (§3-2).
    /// </summary>
    public readonly struct ManagerCommand
    {
        /// <summary>Which live mutator this command drives.</summary>
        public readonly ManagerCommandKind Kind;

        /// <summary>Team id (<see cref="ManagerCommandKind.SetTeamTactic"/> / <see cref="ManagerCommandKind.Substitute"/>).</summary>
        public readonly int TeamId;

        /// <summary>Agent (roster) index (<see cref="ManagerCommandKind.SetPlayerTactic"/>).</summary>
        public readonly int AgentId;

        /// <summary>The team tactic to stage (<see cref="ManagerCommandKind.SetTeamTactic"/>).</summary>
        public readonly TeamTactic NewTeamTactic;

        /// <summary>The player tactic to stage (<see cref="ManagerCommandKind.SetPlayerTactic"/>).</summary>
        public readonly PlayerTactic NewPlayerTactic;

        /// <summary>Outgoing on-pitch slot index (<see cref="ManagerCommandKind.Substitute"/>).</summary>
        public readonly int OutSlotIndex;

        /// <summary>Incoming bench index (<see cref="ManagerCommandKind.Substitute"/>).</summary>
        public readonly int BenchIndex;

        /// <summary>Reason recorded on the substitution (<see cref="ManagerCommandKind.Substitute"/>).</summary>
        public readonly SubstitutionReason Reason;

        private ManagerCommand(
            ManagerCommandKind kind,
            int teamId,
            int agentId,
            in TeamTactic newTeamTactic,
            in PlayerTactic newPlayerTactic,
            int outSlotIndex,
            int benchIndex,
            SubstitutionReason reason)
        {
            Kind            = kind;
            TeamId          = teamId;
            AgentId         = agentId;
            NewTeamTactic   = newTeamTactic;
            NewPlayerTactic = newPlayerTactic;
            OutSlotIndex    = outSlotIndex;
            BenchIndex      = benchIndex;
            Reason          = reason;
        }

        /// <summary>A command to stage a team-level tactic change for <paramref name="teamId"/>.</summary>
        public static ManagerCommand SetTeamTactic(int teamId, in TeamTactic tactic) =>
            new ManagerCommand(ManagerCommandKind.SetTeamTactic, teamId, 0, tactic, default, 0, 0, default);

        /// <summary>A command to stage a per-agent tactic change for <paramref name="agentId"/>.</summary>
        public static ManagerCommand SetPlayerTactic(int agentId, in PlayerTactic tactic) =>
            new ManagerCommand(ManagerCommandKind.SetPlayerTactic, 0, agentId, default, tactic, 0, 0, default);

        /// <summary>A command to swap bench player <paramref name="benchIndex"/> onto pitch slot <paramref name="outSlotIndex"/>.</summary>
        public static ManagerCommand Substitute(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason) =>
            new ManagerCommand(ManagerCommandKind.Substitute, teamId, 0, default, default, outSlotIndex, benchIndex, reason);

        /// <summary>
        /// Applies this command onto <paramref name="mutations"/> via the single live mutator its
        /// <see cref="Kind"/> selects. Called on the sim thread by the driver's drain.
        /// </summary>
        public bool Apply(ILiveMatchMutations mutations) => Apply(mutations, out _);

        /// <summary>
        /// As <see cref="Apply(ILiveMatchMutations)"/>, also reporting the engine's typed rule outcome for
        /// a Substitute command. <paramref name="substitutionStatus"/> is
        /// <see cref="SubstitutionRequestStatus.Accepted"/> for every other kind and for accepted requests.
        /// </summary>
        public bool Apply(ILiveMatchMutations mutations, out SubstitutionRequestStatus substitutionStatus)
        {
            substitutionStatus = SubstitutionRequestStatus.Accepted;
            if (mutations == null) { throw new ArgumentNullException(nameof(mutations)); }
            switch (Kind)
            {
                case ManagerCommandKind.None:
                    // Defense in depth: an uninitialized (default-valued) command must never reach a
                    // mutator. ManagerCommandQueue.Enqueue rejects it at the misuse site; this guards
                    // any path that bypasses the queue.
                    throw new InvalidOperationException(
                        "ManagerCommand is uninitialized (default value); construct it via a factory.");
                case ManagerCommandKind.SetTeamTactic:
                {
                    TeamTactic tactic = NewTeamTactic;
                    mutations.SetTeamTactic(TeamId, in tactic);
                    return true;
                }
                case ManagerCommandKind.SetPlayerTactic:
                {
                    PlayerTactic tactic = NewPlayerTactic;
                    mutations.SetPlayerTactic(AgentId, in tactic);
                    return true;
                }
                case ManagerCommandKind.Substitute:
                {
                    SubstitutionRequestResult result = mutations.RequestSubstitution(TeamId, OutSlotIndex, BenchIndex, Reason);
                    substitutionStatus = result.Status;
                    return result.IsAccepted;
                }
                default:
                    throw new InvalidOperationException("Unknown ManagerCommandKind: " + ((int)Kind).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): value-type discriminated command with   |
// |         |            |        | one factory + Apply path per live mutator.                     |
// | 1.1     | 2026-07-24 | —      | AR pass-1 Medium: Apply fails loud on the None (default) kind   |
// |         |            |        | instead of falling through to a malformed mutator payload.     |
// | 1.2     | 2026-10-15 | —      | Match Flow §6.4: Apply returns bool; a Substitute command reports whether the engine accepted the request (rule refusals are typed results, not exceptions). |
// | 1.3     | 2026-10-21 | —      | Gate J J-02: Apply overload reports the engine's typed SubstitutionRequestStatus so the driver can record why a substitution was refused. |
#endregion
