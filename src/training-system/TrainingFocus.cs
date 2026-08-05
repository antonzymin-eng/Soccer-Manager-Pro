// File:     src/training-system/TrainingFocus.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 (data structures), FR-TR-003 / FR-TR-023; Code Standards #20
// Purpose:  The per-player training focus. Persistent state living on TrainingState.Focus — the single
//           source of truth (FR-TR-003); TrainingSchedule is a read-only view over it, never a copy.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// The per-player training emphasis (§2.2). <see cref="Balanced"/> is ordinal 0 and is the value
    /// <see cref="TrainingState.Create"/> seeds for a regen (FR-TR-025), so the zero value is a legal
    /// focus — unlike <c>default(TrainingState)</c>, which is NOT a valid runtime state because its
    /// <c>LastAdvancedWorldDay</c> would be day 0 rather than the never-advanced sentinel.
    /// <para>
    /// Ordinals are APPEND-only: the per-focus <c>[GT]</c> tables in
    /// <see cref="TrainingSystemConstants"/> are indexed by these ordinals, and a persisted
    /// <see cref="TrainingState"/> stores the ordinal, so renumbering would silently re-point every
    /// saved player's focus.
    /// </para>
    /// </summary>
    public enum TrainingFocus : byte
    {
        /// <summary>Default; no dominant emphasis.</summary>
        Balanced = 0,

        /// <summary>Recovery emphasis — lowers training-fatigue fastest, small conditioning gain.</summary>
        Rest = 1,

        /// <summary>Conditioning emphasis.</summary>
        Fitness = 2,

        /// <summary>Deep tier (#29 T3): weights technical attributes in the growth input.</summary>
        Technical = 3,

        /// <summary>Deep tier (#29 T3): weights physical attributes in the growth input.</summary>
        Physical = 4,

        /// <summary>Deep tier (#29 T3): weights mental/positional attributes in the growth input.</summary>
        Tactical = 5,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                     |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, §2.2).    |
#endregion
