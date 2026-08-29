// File:     src/season-save/SeasonSaveContents.cs
// Created:  2026-07-22
// Modified: 2026-08-13 (#44 T1, roadmap C1 — gains the restored DisciplineState)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / G4 / KD-3;
//           Season & Competition Loop #30 FR-SN-021; Training System #29 FR-TR-019;
//           Injuries & Medical #41 FR-MD-018; Discipline & Suspensions #44 Appendix B; Code Standards #20
// Purpose:  The reconstructed contents of a season save: the living-world WorldStore (always), the
//           season state (always), the per-club #29 training, #41 medical and #30 appearance states
//           (always, possibly empty), the #28 career store and the #44 discipline state, and the
//           in-progress MatchEngine (null when the season carried
//           no match). SeasonSaveManager.Load returns this.

using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The parts a season save reconstructs (unified-season-save-design.md §4 + #30 FR-SN-021): the
    /// living-world <see cref="World"/> and the <see cref="Season"/> (never null — a season save always
    /// carries both), the per-club <see cref="TrainingClubs"/> / <see cref="MedicalClubs"/> /
    /// <see cref="AppearanceClubs"/> (never null, possibly empty), the restored <see cref="Progression"/> /
    /// <see cref="Discipline"/> state (never null), and the in-progress <see cref="Match"/> (<c>null</c> when
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

        /// <summary>The reconstructed per-club #30 appearance states, ascending by club id. Never
        /// null; empty when the save tracked no appearances (#30 Appendix B / ERR-041-010(b)).</summary>
        public readonly ClubAppearanceStates[] AppearanceClubs;

        /// <summary>
        /// The reconstructed #28 career store — the restored ROSTER, not merely an overlay on one
        /// (#28 KD-4). Never null. This is the authority a caller must resume the career against: the
        /// bootstrap product it was originally seeded from carries day-0 attributes and is stale by
        /// exactly the growth this store has banked.
        /// </summary>
        public readonly ProgressionEngine Progression;

        /// <summary>
        /// The reconstructed #44 discipline state — the sparse (PlayerId, CompetitionId) tally map.
        /// Never null; empty when the save tracked no cards (#44 Appendix B / FR-DC-017).
        /// </summary>
        public readonly DisciplineState Discipline;

        /// <summary>The reconstructed in-progress match, or <c>null</c> if the season had no match.</summary>
        public readonly MatchEngine.MatchEngine Match;

        /// <summary>Constructs the reconstructed season contents.</summary>
        public SeasonSaveContents(
            WorldStore world,
            SeasonState season,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs,
            ProgressionEngine progression,
            DisciplineState discipline,
            MatchEngine.MatchEngine match)
        {
            World = world;
            Season = season;
            TrainingClubs = trainingClubs;
            MedicalClubs = medicalClubs;
            AppearanceClubs = appearanceClubs;
            Progression = progression;
            Discipline = discipline;
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
// | 1.3     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): gains AppearanceClubs (never   |
// |         |            |        | null; the #30 appearance record).                                |
// | 1.4     | 2026-08-08 | —      | Balance-pass AR pass 7 (L3): the header Purpose block still       |
// |         |            |        | omitted the appearance records the class summary and Modified     |
// |         |            |        | line already named — the SeasonSaveCodec v1.5 doc-drift class,    |
// |         |            |        | one file over, again.                                             |
// | 1.5     | 2026-08-08 | —      | #28 T2a: gains the restored ProgressionEngine — the roster a    |
// |         |            |        | caller must resume against (#28 KD-4), not merely an overlay.   |
// | 1.6     | 2026-08-13 | —      | #44 T1 (roadmap C1): gains the restored DisciplineState (never  |
// |         |            |        | null; empty when the save tracked no cards).                    |
#endregion
