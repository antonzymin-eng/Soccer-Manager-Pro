// File:     src/pressing-ai/PressingTickDiagnostics.cs
// Created:  2026-09-13
// Author:   —
// Spec:     Match-engine wiring backlog §1.1 / W12
// Purpose:  Observation-only result from one Pressing AI heartbeat. W12 uses this to distinguish
//           phase/cooldown/disengage gating from raw trigger firing, debounce commitment, and an
//           actually-active press without changing the production decision path.

using TacticalDirector.PositioningAI;

namespace TacticalDirector.PressingAI
{
    /// <summary>Where one Pressing AI heartbeat stopped.</summary>
    public enum PressingGateExit : byte
    {
        NotRun = 0,
        StaleTick = 1,
        InPossession = 2,
        Cooldown = 3,
        Disengaged = 4,
        NoCommittedTrigger = 5,
        InvariantRejected = 6,
        Active = 7,
    }

    /// <summary>
    /// Observation-only W12 record for the most recently processed Pressing AI heartbeat.
    /// It is deliberately absent from snapshot serialization: none of these values feeds gameplay,
    /// and every field is recomputed from authoritative runtime state on the next heartbeat.
    /// </summary>
    public readonly struct PressingTickDiagnostics
    {
        public readonly int TickIndex;
        public readonly Phase Phase;
        public readonly PressingGateExit Exit;
        public readonly bool HasLatestPass;
        public readonly TriggerFlags RawTriggers;
        public readonly TriggerFlags CommittedTriggers;
        public readonly int PrimaryPresserId;
        public readonly int CoverShadowCount;

        public PressingTickDiagnostics(
            int tickIndex,
            Phase phase,
            PressingGateExit exit,
            bool hasLatestPass,
            TriggerFlags rawTriggers,
            TriggerFlags committedTriggers,
            int primaryPresserId,
            int coverShadowCount)
        {
            TickIndex = tickIndex;
            Phase = phase;
            Exit = exit;
            HasLatestPass = hasLatestPass;
            RawTriggers = rawTriggers;
            CommittedTriggers = committedTriggers;
            PrimaryPresserId = primaryPresserId;
            CoverShadowCount = coverShadowCount;
        }

        public bool PressActive => Exit == PressingGateExit.Active;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-13 | —      | W12 observation-only gate/trigger diagnostic record. |
#endregion
