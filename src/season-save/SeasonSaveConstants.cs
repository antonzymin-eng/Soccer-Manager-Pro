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
        /// [FIXED] The season save-file FRAMING version — the outermost format version in the save
        /// stack (KD-4). It gates only the season frame (the <c>matchPresent</c> flag + the three
        /// length-prefixed sub-blobs); the inner versions ride inside their own sub-blobs and are
        /// re-checked by <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> /
        /// <see cref="SeasonStateCodec.Decode"/> / <c>MatchSaveCodec.Decode</c> themselves. A mismatch
        /// fails loud on load — no cross-version migration at Stage 0. Bump only on a season-frame
        /// layout change. Value: 2.
        /// <para>
        /// <b>1 → 2 at #30 T1 (FR-SN-020).</b> The frame gained the season-state sub-blob between the
        /// world and match blocks (#30 Appendix B). The world blob
        /// (<c>WORLD_STORE_FORMAT_VERSION</c>) and match blob (<c>MATCH_SAVE_FORMAT_VERSION</c>) are
        /// byte-untouched by that change — only the frame around them moved, which is exactly what
        /// this version gates. A v1 file is rejected fail-loud (no migration at Stage 0).
        /// </para>
        /// </summary>
        public const uint SEASON_SAVE_FORMAT_VERSION = 2;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1 (FR-SN-020): SEASON_SAVE_FORMAT_VERSION 1 -> 2 — the      |
// |         |            |        | frame gained the season-state sub-blob between the world and     |
// |         |            |        | match blocks; both of those blobs stay byte-untouched.           |
#endregion
