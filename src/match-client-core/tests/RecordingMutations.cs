// File:     src/match-client-core/tests/RecordingMutations.cs
// Created:  2026-07-24
// Modified: 2026-10-21 (Gate J J-02: optional typed substitution refusal)
// Modified (prior): 2026-10-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2), Code Standards #20
// Purpose:  Head-less ILiveMatchMutations test double — a fake mutation surface with a settable current
//           tick and match-ended flag that records each applied command. Lets the command-channel
//           determinism tests run without a Unity host or a real MatchEngine (§5-P2).

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    /// <summary>
    /// Records every mutator call as a string and exposes controllable <see cref="CurrentTick"/> /
    /// <see cref="MatchEnded"/> so a test can drive the driver's drain deterministically.
    /// <see cref="ThrowOnSetPlayerAgentId"/> makes one mutator refuse a specific agent id — the way the
    /// real engine fails loud on a bad index — so the driver's apply-time isolation can be exercised.
    /// </summary>
    internal sealed class RecordingMutations : ILiveMatchMutations
    {
        public ulong CurrentTickValue;
        public bool MatchEndedValue;
        public int ThrowOnSetPlayerAgentId = -1;
        /// <summary>When not Accepted, every substitution request is refused with this typed status.</summary>
        public SubstitutionRequestStatus RefuseSubstitutionsWith = SubstitutionRequestStatus.Accepted;
        public readonly List<string> Calls = new List<string>();

        public ulong CurrentTick => CurrentTickValue;
        public bool MatchEnded => MatchEndedValue;

        public void SetTeamTactic(int teamId, in TeamTactic tactic)
        {
            Calls.Add("team:" + teamId.ToString(CultureInfo.InvariantCulture));
        }

        public void SetPlayerTactic(int agentId, in PlayerTactic tactic)
        {
            if (agentId == ThrowOnSetPlayerAgentId)
            {
                // Mirrors MatchEngine.SetPlayerTactic: fail loud BEFORE recording/mutating.
                throw new ArgumentOutOfRangeException(nameof(agentId), agentId, "test-refused agent id");
            }
            Calls.Add("player:" + agentId.ToString(CultureInfo.InvariantCulture));
        }

        public SubstitutionRequestResult RequestSubstitution(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason)
        {
            Calls.Add(string.Format(
                CultureInfo.InvariantCulture, "sub:{0}:{1}:{2}:{3}", teamId, outSlotIndex, benchIndex, reason));
            return RefuseSubstitutionsWith == SubstitutionRequestStatus.Accepted
                ? SubstitutionRequestResult.Accept(1u)
                : SubstitutionRequestResult.Refuse(RefuseSubstitutionsWith);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-24 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
// | 1.2     | 2026-10-15 | —      | Match Flow §6.4: records RequestSubstitution and returns Accepted. |
// | 1.3     | 2026-10-21 | —      | Gate J J-02: RefuseSubstitutionsWith returns a typed refusal. |
#endregion
