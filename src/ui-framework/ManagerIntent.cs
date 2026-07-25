// File:     src/ui-framework/ManagerIntent.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.3 (FR-UI-012/013/014, failure mode F3),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U2, Code Standards #20
// Purpose:  One typed manager intent — the ONLY thing a dispatcher accepts. A pure payload: it knows
//           what the manager asked for, and nothing about how or where it is applied. The mapping onto
//           an engine seam lives in the dispatcher (KD-U2), so this type carries no channel dependency
//           and stays usable by a future non-match dispatcher.

using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// An immutable manager intent (#38 §2.2). A value type discriminated by <see cref="Kind"/>; only
    /// the fields that kind names are meaningful, the rest are <c>default</c> and never read.
    /// <para>
    /// WHY THIS IS NOT <c>MatchClientCore.ManagerCommand</c> (KD-U2): the two overlap today, but they
    /// are different vocabularies at different layers. <see cref="ManagerIntent"/> is what the *client*
    /// can express, across every screen — #38 §2.2 already anticipates an <c>AdvanceRound</c> kind,
    /// which is a season-loop seam and would never belong on a live-match command channel.
    /// <c>ManagerCommand</c> is specifically the match channel's wire type. The dispatcher translates
    /// intent → command for the kinds the match channel carries, and that translation is the single
    /// place the two vocabularies meet (locked by
    /// <c>CommandDispatchTests.EveryMatchIntentKind_MapsToACommandKind</c>, so the pair cannot drift
    /// apart silently).
    /// </para>
    /// <para>
    /// A <c>default(ManagerIntent)</c> is <see cref="IntentKind.None"/> and is refused fail-loud at
    /// dispatch (F3) rather than mapping onto a real seam with a zero payload.
    /// </para>
    /// </summary>
    public readonly struct ManagerIntent
    {
        /// <summary>Which seam this intent asks for.</summary>
        public readonly IntentKind Kind;

        /// <summary>Team id (<see cref="IntentKind.SetTeamTactic"/> / <see cref="IntentKind.Substitute"/>).</summary>
        public readonly int TeamId;

        /// <summary>Agent (roster) index (<see cref="IntentKind.SetPlayerTactic"/>).</summary>
        public readonly int AgentId;

        /// <summary>The team tactic to stage (<see cref="IntentKind.SetTeamTactic"/>).</summary>
        public readonly TeamTactic NewTeamTactic;

        /// <summary>The player tactic to stage (<see cref="IntentKind.SetPlayerTactic"/>).</summary>
        public readonly PlayerTactic NewPlayerTactic;

        /// <summary>Outgoing on-pitch slot index (<see cref="IntentKind.Substitute"/>).</summary>
        public readonly int OutSlotIndex;

        /// <summary>Incoming bench index (<see cref="IntentKind.Substitute"/>).</summary>
        public readonly int BenchIndex;

        /// <summary>Reason recorded on the substitution (<see cref="IntentKind.Substitute"/>).</summary>
        public readonly SubstitutionReason Reason;

        private ManagerIntent(
            IntentKind kind,
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

        /// <summary>Intent to change <paramref name="teamId"/>'s team tactic.</summary>
        public static ManagerIntent SetTeamTactic(int teamId, in TeamTactic tactic) =>
            new ManagerIntent(IntentKind.SetTeamTactic, teamId, 0, tactic, default, 0, 0, default);

        /// <summary>Intent to change <paramref name="agentId"/>'s player tactic.</summary>
        public static ManagerIntent SetPlayerTactic(int agentId, in PlayerTactic tactic) =>
            new ManagerIntent(IntentKind.SetPlayerTactic, 0, agentId, default, tactic, 0, 0, default);

        /// <summary>Intent to swap bench player <paramref name="benchIndex"/> onto pitch slot <paramref name="outSlotIndex"/>.</summary>
        public static ManagerIntent Substitute(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason) =>
            new ManagerIntent(IntentKind.Substitute, teamId, 0, default, default, outSlotIndex, benchIndex, reason);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): the typed intent payload + one      |
// |         |            |        | factory per kind. Translation to the match channel's           |
// |         |            |        | ManagerCommand lives in the dispatcher (plan AR-1 M-1), so     |
// |         |            |        | this type carries no channel dependency.                       |
#endregion
