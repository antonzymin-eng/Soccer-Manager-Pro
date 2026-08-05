// File:     src/injuries-medical/MedicalViewModel.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 KD-8 / FR-MD-024; Code Standards #20
// Purpose:  The read-only observer surface #38 pulls. Value copies only — constructing one cannot
//           mutate the InjuryState it was read from.

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// A read-only value-copy view of one player's medical state (KD-8 / FR-MD-024), for #38. It holds
    /// no reference to the underlying <see cref="InjuryState"/>, so an observer cannot reach a mutation
    /// path — <see cref="MedicalStep.AdvanceMedicalDay"/> stays the sole writer (FR-MD-003).
    /// </summary>
    public readonly struct MedicalViewModel
    {
        /// <summary>The active injury tier.</summary>
        public readonly InjurySeverity Severity;

        /// <summary>World-days of recovery left.</summary>
        public readonly int RecoveryRemaining;

        /// <summary>Cumulative career injuries.</summary>
        public readonly int InjuryCount;

        /// <summary>Whether the player is selectable — the same predicate #30's squad selection reads (FR-MD-023).</summary>
        public readonly bool Available;

        /// <summary>Copies the observable fields out of a medical state.</summary>
        /// <param name="severity">The active injury tier.</param>
        /// <param name="recoveryRemaining">World-days of recovery left.</param>
        /// <param name="injuryCount">Cumulative career injuries.</param>
        /// <param name="available">Whether the player is selectable.</param>
        public MedicalViewModel(InjurySeverity severity, int recoveryRemaining, int injuryCount, bool available)
        {
            Severity = severity;
            RecoveryRemaining = recoveryRemaining;
            InjuryCount = injuryCount;
            Available = available;
        }

        /// <summary>
        /// Reads a value-copy view of <paramref name="state"/>. Pure — it never mutates the state.
        /// <see cref="Available"/> is derived through <see cref="MedicalStep.IsAvailable"/> rather than
        /// recomputed here, so the observer and #30's selection can never disagree about who is fit.
        /// </summary>
        /// <param name="state">The state to observe.</param>
        public static MedicalViewModel Create(in InjuryState state) =>
            new MedicalViewModel(state.Severity, state.RecoveryRemaining, state.InjuryCount, MedicalStep.IsAvailable(state));
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0, KD-8). |
#endregion
