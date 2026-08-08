// File:     src/season-save/AppearanceBlock.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Season & Competition Loop #30 Appendix B; ERR-029-005 (the typed-frame-handle rule);
//           Code Standards #20
// Purpose:  A typed handle on the #30 appearance sub-blob's bytes at the season-frame boundary — the
//           TrainingBlock / MedicalBlock discipline applied to the frame's third confusable payload.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #30 appearance sub-blob, as bytes, at the point it enters the season frame. Opaque here —
    /// the frame never parses it (KD-2); this type carries no knowledge of the layout, only of which
    /// block these bytes are.
    /// <para>
    /// Typed for ERR-029-005's reason: <see cref="SeasonSaveCodec.Encode"/> otherwise grows another
    /// bare <c>byte[]</c> in a parameter list that already burned once on a silent transposition. The
    /// leading <c>APPEARANCE_SAVE_MAGIC</c> catches a mix-up at load; this catches it at compile time.
    /// </para>
    /// </summary>
    public readonly struct AppearanceBlock
    {
        /// <summary>The encoded #30 appearance block (<see cref="AppearanceSaveCodec.Encode"/>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded appearance block.</summary>
        /// <param name="bytes">The block's bytes. An empty set is a well-formed zero-club block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries an appearance block (#30 Appendix B).</exception>
        public AppearanceBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries an appearance block — an empty one is a zero-club " +
                "block, not a null.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2): the typed frame handle  |
// |         |            |        | for the appearance sub-blob (ERR-029-005 discipline).             |
#endregion
