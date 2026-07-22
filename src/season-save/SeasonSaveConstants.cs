// File:     src/season-save/SeasonSaveConstants.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-4; Code Standards #20
// Purpose:  Constant catalogue for the season save-file frame. Holds the season-frame format version —
//           a fourth version, distinct from every version the frame nests (the two snapshot schema
//           versions, MATCH_SAVE_FORMAT_VERSION, and WORLD_STORE_FORMAT_VERSION).

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for the unified season save file (unified-season-save-design.md §3 layout / KD-4).
    /// </summary>
    public static class SeasonSaveConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] The season save-file FRAMING version — the fourth distinct format version in the
        /// save stack (KD-4). It gates only the season frame (the <c>matchPresent</c> flag + the two
        /// length-prefixed sub-blobs); the four inner versions ride inside their own sub-blobs and are
        /// re-checked by <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> /
        /// <c>MatchSaveCodec.Decode</c> themselves. A mismatch fails loud on load — no cross-version
        /// migration at Stage 0. Bump only on a season-frame layout change. Value: 1.
        /// </summary>
        public const uint SEASON_SAVE_FORMAT_VERSION = 1;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-22 | —      | Initial implementation. |
#endregion
