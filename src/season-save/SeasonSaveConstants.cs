// File:     src/season-save/SeasonSaveConstants.cs
// Created:  2026-07-22
// Modified: 2026-08-07 (balance pass D2: SEASON_SAVE_FORMAT_VERSION 3 -> 4 + the APPR magic/version)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-4; Code Standards #20
// Purpose:  Constant catalogue for the season save-file frame. Holds the season-frame format version —
//           distinct from every version the frame nests: WORLD_STORE_FORMAT_VERSION,
//           SEASON_STATE_FORMAT_VERSION, TRAINING_SAVE_FORMAT_VERSION, MEDICAL_SAVE_FORMAT_VERSION and
//           APPEARANCE_SAVE_FORMAT_VERSION at the sub-blob level, MATCH_SAVE_FORMAT_VERSION for the
//           optional match block, and — a level deeper still — the two snapshot schema versions nested
//           inside the world and match blobs.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for the unified season save file (unified-season-save-design.md §3 layout / KD-4).
    /// </summary>
    public static class SeasonSaveConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] The season save-file FRAMING version — the outermost format version in the save
        /// stack (KD-4). It gates only the season frame (the <c>matchPresent</c> flag + the six
        /// length-prefixed sub-blobs — the living-world composite, the season state, the #29 training
        /// block, the #41 medical block, the #30 appearance block, and the optional match block); the
        /// inner versions ride inside
        /// their own sub-blobs and are re-checked by
        /// <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> /
        /// <see cref="SeasonStateCodec.Decode"/> /
        /// <see cref="TacticalDirector.TrainingSystem.TrainingSaveCodec.Decode"/> /
        /// <see cref="TacticalDirector.InjuriesMedical.MedicalSaveCodec.Decode"/> /
        /// <c>MatchSaveCodec.Decode</c> themselves. A mismatch fails loud on load — no cross-version
        /// migration at Stage 0. Bump only on a season-frame layout change. Value: 4.
        /// <para>
        /// <b>1 → 2 at #30 T1 (FR-SN-020).</b> The frame gained the season-state sub-blob between the
        /// world and match blocks (#30 Appendix B). The world blob
        /// (<c>WORLD_STORE_FORMAT_VERSION</c>) and match blob (<c>MATCH_SAVE_FORMAT_VERSION</c>) are
        /// byte-untouched by that change — only the frame around them moved, which is exactly what
        /// this version gates. A v1 file is rejected fail-loud (no migration at Stage 0).
        /// </para>
        /// <para>
        /// <b>2 → 3 at #29/#41 T1 (FR-TR-018 / FR-MD-017).</b> The frame gained two further sub-blobs —
        /// the training block (<c>TRAINING_SAVE_FORMAT_VERSION</c>) and the medical block
        /// (<c>MEDICAL_SAVE_FORMAT_VERSION</c>) — between the season block and the optional match block.
        /// Both are always present (an empty block is a zero-club block, not an absent one), so the
        /// optional match block stays last and keeps its presence flag. Every pre-existing blob is
        /// byte-untouched by that change; only the frame around them moved. A v2 file is rejected
        /// fail-loud.
        /// </para>
        /// <para>
        /// <b>3 → 4 at the #29/#41 balance pass (ERR-041-010(b)).</b> The frame gained the #30
        /// appearance sub-blob (<see cref="APPEARANCE_SAVE_FORMAT_VERSION"/>) between the medical block
        /// and the optional match block — mandatory for the same reason the #29/#41 blocks are: an
        /// appearance record has no absent case, only an empty one. A v3 file is rejected fail-loud.
        /// </para>
        /// </summary>
        public const uint SEASON_SAVE_FORMAT_VERSION = 4;

        /// <summary>
        /// [FIXED] The #30 appearance sub-blob's leading self-identifying tag — ASCII <c>"APPR"</c>,
        /// written before <see cref="APPEARANCE_SAVE_FORMAT_VERSION"/> (the ERR-029-005 rule: a format
        /// version distinguishes generations of ONE format, never one format from another, and every
        /// sub-blob format in this stack sits at version 1).
        /// </summary>
        public const uint APPEARANCE_SAVE_MAGIC = 0x41505052;   // 'A''P''P''R'

        /// <summary>[FIXED] The #30 appearance sub-blob version. Gates the generation of the format identified by <see cref="APPEARANCE_SAVE_MAGIC"/>.</summary>
        public const uint APPEARANCE_SAVE_FORMAT_VERSION = 1;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1 (FR-SN-020): SEASON_SAVE_FORMAT_VERSION 1 -> 2 — the      |
// |         |            |        | frame gained the season-state sub-blob between the world and     |
// |         |            |        | match blocks; both of those blobs stay byte-untouched.           |
// | 1.2     | 2026-08-06 | —      | #29/#41 T1 (FR-TR-018 / FR-MD-017): 2 -> 3 — the frame gained    |
// |         |            |        | the training and medical sub-blobs between the season block and  |
// |         |            |        | the optional match block; the other three stay byte-untouched.   |
// | 1.3     | 2026-08-06 | —      | Doc-drift fix (no code/value change): the SEASON_SAVE_FORMAT_    |
// |         |            |        | VERSION summary still said "Value: 2" and "three sub-blobs", and |
// |         |            |        | its <see cref> list omitted TrainingSaveCodec.Decode /           |
// |         |            |        | MedicalSaveCodec.Decode; the file-header Purpose block named     |
// |         |            |        | only three of the five nested versions. Both corrected.          |
// | 1.4     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): 3 -> 4 — the frame gained the  |
// |         |            |        | mandatory #30 appearance sub-blob between the medical block and  |
// |         |            |        | the optional match block; + APPEARANCE_SAVE_MAGIC ("APPR") and   |
// |         |            |        | APPEARANCE_SAVE_FORMAT_VERSION = 1 (the ERR-029-005 rule).       |
#endregion
