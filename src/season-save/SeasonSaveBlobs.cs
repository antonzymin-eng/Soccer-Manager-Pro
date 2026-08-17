// File:     src/season-save/SeasonSaveBlobs.cs
// Created:  2026-07-22
// Modified: 2026-08-08
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-2 / KD-7 / §4;
//           Season & Competition Loop #30 Appendix B (frame), FR-SN-019; Training System #29 FR-TR-018;
//           Injuries & Medical #41 FR-MD-017; Code Standards #20
// Purpose:  The deframe result of a season save blob: the seven opaque sub-blobs (the living-world
//           composite, the season state, the #29 training block, the #41 medical block, the #30
//           appearance block, the #28 career-state block, and — when present — the match save). Pure bytes; the codec never reconstructs objects (that is
//           SeasonSaveManager), so it stays free of match-engine / living-world / season-state types.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The seven byte sub-blobs a season save carries (unified-season-save-design.md KD-2 + #30
    /// FR-SN-019 + #29 FR-TR-018 + #41 FR-MD-017 + #28 FR-PG-017): the living-world composite
    /// (<see cref="WorldBlob"/>), the season state (<see cref="SeasonBlob"/>), the #29 training block
    /// (<see cref="TrainingBlob"/>), the #41 medical block (<see cref="MedicalBlob"/>), the #30
    /// appearance block (<see cref="AppearanceBlob"/>) and the #28 career-state block
    /// (<see cref="ProgressionBlob"/>) — all six always present — and the match save (<see cref="MatchBlob"/>, <c>null</c> when the season had no
    /// in-progress match — KD-3). Produced by <see cref="SeasonSaveCodec.Decode"/>;
    /// <see cref="SeasonSaveManager"/> reconstructs the actual <c>WorldStore</c> /
    /// <see cref="SeasonState"/> / training + medical state / <c>MatchEngine</c> from them. Kept opaque
    /// so the codec never parses any blob's internals (each keeps its own version gate).
    /// </summary>
    public readonly struct SeasonSaveBlobs
    {
        /// <summary>The living-world composite blob (<c>WorldStore.Snapshot()</c>). Never null.</summary>
        public readonly byte[] WorldBlob;

        /// <summary>The season-state blob (<see cref="SeasonStateCodec.Encode"/>). Never null — unlike
        /// the match, a season save always carries a season (FR-SN-019/021).</summary>
        public readonly byte[] SeasonBlob;

        /// <summary>The #29 training block
        /// (<see cref="TacticalDirector.TrainingSystem.TrainingSaveCodec.Encode"/>). Never null; a game
        /// tracking no training state carries a well-formed zero-club block (FR-TR-018).</summary>
        public readonly byte[] TrainingBlob;

        /// <summary>The #41 medical block
        /// (<see cref="TacticalDirector.InjuriesMedical.MedicalSaveCodec.Encode"/>). Never null; a game
        /// tracking no medical state carries a well-formed zero-club block (FR-MD-017).</summary>
        public readonly byte[] MedicalBlob;

        /// <summary>The #30 appearance block (<see cref="AppearanceSaveCodec.Encode"/>). Never null; a
        /// game tracking no appearances carries a well-formed zero-club block (#30 Appendix B).</summary>
        public readonly byte[] AppearanceBlob;

        /// <summary>The #28 career-state block
        /// (<see cref="TacticalDirector.PlayerProgression.ProgressionSaveCodec.Encode"/>). Never null; a
        /// game tracking no careers carries a well-formed zero-club block (FR-PG-017). Unlike its
        /// siblings this block carries the evolving <c>PlayerRecord</c> set itself (#28 KD-4).</summary>
        public readonly byte[] ProgressionBlob;

        /// <summary>The match save blob (<c>MatchSaveManager.Encode</c>), or <c>null</c> if the season
        /// had no in-progress match (KD-3).</summary>
        public readonly byte[] MatchBlob;

        /// <summary>Constructs the deframed sub-blobs.</summary>
        public SeasonSaveBlobs(
            byte[] worldBlob,
            byte[] seasonBlob,
            byte[] trainingBlob,
            byte[] medicalBlob,
            byte[] appearanceBlob,
            byte[] progressionBlob,
            byte[] matchBlob)
        {
            WorldBlob = worldBlob;
            SeasonBlob = seasonBlob;
            TrainingBlob = trainingBlob;
            MedicalBlob = medicalBlob;
            AppearanceBlob = appearanceBlob;
            ProgressionBlob = progressionBlob;
            MatchBlob = matchBlob;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1: gains SeasonBlob (always present) — the third sub-blob.   |
// | 1.2     | 2026-08-06 | —      | #29/#41 T1: gains TrainingBlob + MedicalBlob (both always        |
// |         |            |        | present) — the fourth and fifth sub-blobs.                        |
// | 1.3     | 2026-08-07 | —      | Balance pass D2: gains AppearanceBlob (always present) — the     |
// |         |            |        | sixth sub-blob (ERR-041-010(b)).                                  |
// | 1.4     | 2026-08-08 | —      | #28 T1: gains ProgressionBlob (always present) — the seventh     |
// |         |            |        | sub-blob, and the first to carry roster data (KD-4).             |
#endregion
