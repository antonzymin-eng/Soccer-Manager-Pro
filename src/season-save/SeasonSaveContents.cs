// File:     src/season-save/SeasonSaveContents.cs
// Created:  2026-07-22
// Modified: 2026-08-06 (#29/#41 T1: carries the reconstructed training and medical state)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / G4 / KD-3;
//           Season & Competition Loop #30 FR-SN-021; Training System #29 FR-TR-019;
//           Injuries & Medical #41 FR-MD-018; Code Standards #20
// Purpose:  The reconstructed contents of a season save: the living-world WorldStore (always), the
//           season state (always), the per-club #29 training and #41 medical states (always, possibly
//           empty), and the in-progress MatchEngine (null when the season carried no match).
//           SeasonSaveManager.Load returns this.

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The parts a season save reconstructs (unified-season-save-design.md §4 + #30 FR-SN-021): the
    /// living-world <see cref="World"/> and the <see cref="Season"/> (never null — a season save always
    /// carries both), the per-club <see cref="TrainingClubs"/> / <see cref="MedicalClubs"/> (never null,
    /// empty until #29/#41 are wired at T2), and the in-progress <see cref="Match"/> (<c>null</c> when
    /// the save carried no match, KD-3). Returned by <see cref="SeasonSaveManager.Load"/>; the caller
    /// checks <see cref="Match"/> for null before using it.
    /// </summary>
    public readonly struct SeasonSaveContents
    {
        /// <summary>The reconstructed living-world store. Never null.</summary>
        public readonly WorldStore World;

        /// <summary>The reconstructed season state (fixtures, table, calendar cursor, board). Never null.</summary>
        public readonly SeasonState Season;

        /// <summary>The reconstructed per-club #29 training states, ascending by club id. Never null;
        /// empty when the save tracked no training state (FR-TR-019).</summary>
        public readonly ClubTrainingStates[] TrainingClubs;

        /// <summary>The reconstructed per-club #41 medical states, ascending by club id. Never null;
        /// empty when the save tracked no medical state (FR-MD-018).</summary>
        public readonly ClubInjuryStates[] MedicalClubs;

        /// <summary>The reconstructed in-progress match, or <c>null</c> if the season had no match.</summary>
        public readonly MatchEngine.MatchEngine Match;

        /// <summary>Constructs the reconstructed season contents.</summary>
        public SeasonSaveContents(
            WorldStore world,
            SeasonState season,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            MatchEngine.MatchEngine match)
        {
            World = world;
            Season = season;
            TrainingClubs = trainingClubs;
            MedicalClubs = medicalClubs;
            Match = match;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1 (FR-SN-021): gains the reconstructed Season (never null). |
// | 1.2     | 2026-08-06 | —      | #29/#41 T1: gains the reconstructed per-club TrainingClubs /     |
// |         |            |        | MedicalClubs (never null; empty until T2 wires the producers).   |
#endregion
