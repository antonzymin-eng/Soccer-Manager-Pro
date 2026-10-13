// File:     src/client-app/ClientChangeRequests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 journey §14.5, Code Standards #20
// Purpose:  Consume only new exact-payload suffixes; serialize one Pending request per kind on the client thread.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TacticalDirector.MatchClientCore;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;
using TacticalDirector.MatchViewer;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.ClientApp
{
    /// <summary>One session's sole home command producer and evidence consumer.</summary>
    public sealed class ClientChangeRequests
    {
        private readonly List<ClientChangeRecord> _records = new List<ClientChangeRecord>();
        private int _appliedCursor;
        private int _refusedCursor;
        private ClientChangeRecord _mentalityPending;
        private ClientChangeRecord _substitutionPending;
        private TeamTactic _currentTactic;
        private bool _completed;
        /// <summary>Stable chronology; records expose no public mutators.</summary>
        public ReadOnlyCollection<ClientChangeRecord> Records { get; }
        /// <summary>Only setup or evidenced Applied updates this value.</summary>
        public Mentality CurrentMentality => _currentTactic.Mentality;
        /// <summary>Whether another Mentality request must wait.</summary>
        public bool IsMentalityPending => _mentalityPending != null;
        /// <summary>Whether another substitution request must wait.</summary>
        public bool IsSubstitutionPending => _substitutionPending != null;
        /// <summary>Changes only on registration or new outcome evidence.</summary>
        public int Revision { get; private set; }

        internal ClientChangeRequests(in TeamTactic initialTactic)
        {
            _currentTactic = initialTactic;
            Records = _records.AsReadOnly();
        }

        internal TeamTactic WithMentality(Mentality mentality)
        {
            TeamTactic b = _currentTactic;
            return new TeamTactic(mentality, b.Formation, b.Tempo, b.Width, b.Passing, b.Pressing, b.LineOfEngagement, b.DefensiveLine, b.DefensiveWidth, b.TransitionWon, b.TransitionLost, b.OffsideTrap, b.TriggerPressMask, b.FocusPlay, b.GkDistribution, b.TimeWasting, b.MarkingOrientation, b.DismarkIntensity, b.BuildUpStructure, b.RotationFreedom);
        }

        internal ClientChangeRecord Submit(in ManagerCommand command, in ManagerIntent intent, in MatchPlayerIdentity outgoing, in MatchPlayerIdentity incoming, ICommandDispatcher dispatcher)
        {
            if (_completed || command.TeamId != 0 || (command.Kind != ManagerCommandKind.SetTeamTactic && command.Kind != ManagerCommandKind.Substitute))
                throw new InvalidOperationException("Unsupported S0 request.");
            bool mentality = command.Kind == ManagerCommandKind.SetTeamTactic;
            if (mentality ? IsMentalityPending : IsSubstitutionPending)
                throw new InvalidOperationException("A request of this kind is already Pending.");
            var row = new ClientChangeRecord(in command, in outgoing, in incoming);
            _records.Add(row);
            if (mentality)
                _mentalityPending = row;
            else
                _substitutionPending = row;
            Revision++;
            try
            {
                dispatcher.Dispatch(in intent);
            }
            catch
            {
                Settle(row, ClientChangeStatus.SendFailure, 0);
            }

            return row;
        }

        internal void Reconcile(IReadOnlyList<TickStampedCommand> applied, IReadOnlyList<TickStampedCommand> refused, bool completed)
        {
            if (applied.Count < _appliedCursor || refused.Count < _refusedCursor)
                throw new InvalidOperationException("Session command evidence shrank.");
            while (_appliedCursor < applied.Count)
                Consume(applied[_appliedCursor++], ClientChangeStatus.Applied);
            while (_refusedCursor < refused.Count)
                Consume(refused[_refusedCursor++], ClientChangeStatus.Refused);
            if (!completed)
                return;
            _completed = true;
            if (_mentalityPending != null)
                Settle(_mentalityPending, ClientChangeStatus.NotApplied, 0);
            if (_substitutionPending != null)
                Settle(_substitutionPending, ClientChangeStatus.NotApplied, 0);
        }

        private void Consume(TickStampedCommand evidence, ClientChangeStatus status)
        {
            ManagerCommand command = evidence.Command;
            if (command.TeamId != 0)
                return; // The opponent is independently AI managed.
            ClientChangeRecord pending = command.Kind == ManagerCommandKind.SetTeamTactic ? _mentalityPending : _substitutionPending;
            if (pending == null || !Matches(pending.Command, command))
                throw new InvalidOperationException("Foreign or unmatched home command evidence.");
            if (status == ClientChangeStatus.Applied && command.Kind == ManagerCommandKind.SetTeamTactic)
                _currentTactic = command.NewTeamTactic;
            if (status == ClientChangeStatus.Applied && command.Kind == ManagerCommandKind.Substitute)
            {
                // The driver proves request acceptance, not a roster replacement.
                pending.EngineAccepted = true;
                pending.AcceptedTick = evidence.AppliedTick;
                Revision++;
                return;
            }
            Settle(pending, status, evidence.AppliedTick);
        }

        /// <summary>
        /// Settles a previously accepted substitution from authoritative roster cues.
        /// Never predicts execution from request acceptance or a restart banner alone.
        /// </summary>
        internal void ObserveFrame(in LiveMatchFrame frame)
        {
            ClientChangeRecord pending = _substitutionPending;
            if (pending == null || !pending.EngineAccepted || frame.Tick <= pending.AcceptedTick)
                return;
            LiveAgentCue cue = frame.AgentCues[pending.Command.OutSlotIndex];
            if (cue.BenchSlot == pending.Command.BenchIndex)
                Settle(pending, ClientChangeStatus.Applied, frame.Tick);
            else if (cue.IsSentOff || WasRemovedFromAuthoritativeQueue(in frame, pending))
                Settle(pending, ClientChangeStatus.NotApplied, frame.Tick);
        }

        private static bool WasRemovedFromAuthoritativeQueue(
            in LiveMatchFrame frame, ClientChangeRecord row)
        {
            PendingSubstitution[] requests = frame.PendingSubstitutions;
            if (requests == null)
                return false; // Older snapshots do not carry a queue; do not infer cancellation.
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].Team == row.Command.TeamId &&
                    requests[i].OutSlot == row.Command.OutSlotIndex &&
                    requests[i].Bench == row.Command.BenchIndex)
                    return false;
            }
            return true;
        }

        private void Settle(ClientChangeRecord row, ClientChangeStatus status, ulong tick)
        {
            row.Status = status;
            row.OutcomeTick = tick;
            if (ReferenceEquals(row, _mentalityPending))
                _mentalityPending = null;
            if (ReferenceEquals(row, _substitutionPending))
                _substitutionPending = null;
            Revision++;
        }

        private static bool Matches(in ManagerCommand a, in ManagerCommand b)
        {
            if (a.Kind != b.Kind || a.TeamId != b.TeamId)
                return false;
            return a.Kind == ManagerCommandKind.SetTeamTactic ? a.NewTeamTactic.Equals(b.NewTeamTactic) : a.Kind == ManagerCommandKind.Substitute && a.OutSlotIndex == b.OutSlotIndex && a.BenchIndex == b.BenchIndex && a.Reason == b.Reason;
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Exact suffix reconciliation, duplicate-kind guard and end settlement. |
#endregion
