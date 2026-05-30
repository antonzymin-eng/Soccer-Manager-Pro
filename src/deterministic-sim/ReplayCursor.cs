// File:     src/deterministic-sim/ReplayCursor.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.4, §4.2.2 (step 7), Code Standards #20
// Purpose:  Replay cursor struct identifying the exact tick + phase boundary at which a snapshot
//           was committed. Step 7 of the replay lifecycle asserts this is EndOfSnapshot[T].

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Identifies the exact (tick, phaseOrdinal) boundary at which a snapshot was committed.
    /// Legal boundaries are phase end-of-pipeline positions.
    /// Step 7 of the replay lifecycle asserts PhaseOrdinal == END_OF_SNAPSHOT_PHASE_ORDINAL.
    /// Deterministic Simulation #16 §4.2.2 step 7.
    /// </summary>
    public readonly struct ReplayCursor
    {
        /// <summary>Physics tick index (60 Hz, 0-based from match start) at which the snapshot was committed.</summary>
        public readonly ulong Tick;

        /// <summary>Phase ordinal at the snapshot boundary. Must equal DeterministicSimConstants.END_OF_SNAPSHOT_PHASE_ORDINAL for a valid save point.</summary>
        public readonly byte PhaseOrdinal;

        /// <summary>Constructs a replay cursor at the given tick and phase boundary.</summary>
        public ReplayCursor(ulong tick, byte phaseOrdinal)
        {
            Tick         = tick;
            PhaseOrdinal = phaseOrdinal;
        }

        /// <summary>Returns true if this cursor is at the EndOfSnapshot[T] boundary (step 7 assertion). §4.2.2.</summary>
        public bool IsAtEndOfSnapshot =>
            PhaseOrdinal == DeterministicSimConstants.END_OF_SNAPSHOT_PHASE_ORDINAL;

        /// <summary>The canonical end-of-snapshot cursor for the given tick.</summary>
        public static ReplayCursor EndOfSnapshot(ulong tick) =>
            new ReplayCursor(tick, DeterministicSimConstants.END_OF_SNAPSHOT_PHASE_ORDINAL);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
