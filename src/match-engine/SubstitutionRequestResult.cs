// File:     src/match-engine/SubstitutionRequestResult.cs
// Created:  2026-10-13
// Spec:     Match Flow Completion §6 (deferred substitution contract), Code Standards #20
// Purpose:  Typed, non-throwing football-rule outcomes and immutable pending-request view.

namespace TacticalDirector.MatchEngine
{
    /// <summary>Rule-level outcome of a manager substitution request. Invalid indices still throw.</summary>
    public enum SubstitutionRequestStatus : byte
    {
        Accepted = 0,
        MatchEnded = 1,
        OutgoingSentOff = 2,
        OutgoingAlreadyReplaced = 3,
        OutgoingAlreadyPending = 4,
        BenchPlayerUnavailable = 5,
        LimitReached = 6,
        NoWindowsLeft = 7,
        GoalkeeperRoleMismatch = 8,
    }

    /// <summary>Result of request-time validation. Only accepted requests carry a sequence.</summary>
    public readonly struct SubstitutionRequestResult
    {
        public readonly SubstitutionRequestStatus Status;
        public readonly uint Sequence;

        public bool IsAccepted => Status == SubstitutionRequestStatus.Accepted;

        private SubstitutionRequestResult(SubstitutionRequestStatus status, uint sequence)
        {
            Status = status;
            Sequence = sequence;
        }

        public static SubstitutionRequestResult Accept(uint sequence) =>
            new SubstitutionRequestResult(SubstitutionRequestStatus.Accepted, sequence);

        public static SubstitutionRequestResult Refuse(SubstitutionRequestStatus reason) =>
            new SubstitutionRequestResult(reason, 0u);
    }

    /// <summary>Read-only stable projection of one queued, unapplied substitution.</summary>
    public readonly struct PendingSubstitution
    {
        public readonly uint Sequence;
        public readonly byte Team;
        public readonly byte OutSlot;
        public readonly byte Bench;
        public readonly SubstitutionReason Reason;

        public PendingSubstitution(uint sequence, byte team, byte outSlot, byte bench, SubstitutionReason reason)
        {
            Sequence = sequence;
            Team = team;
            OutSlot = outSlot;
            Bench = bench;
            Reason = reason;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-10-13 | —      | Typed request outcomes and immutable pending view. |
#endregion
