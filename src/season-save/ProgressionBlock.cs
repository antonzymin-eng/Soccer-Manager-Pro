// File:     src/season-save/ProgressionBlock.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Player Progression & Lifecycle #28 FR-PG-017; #30 Appendix B; ERR-029-005; Code Standards #20
// Purpose:  A typed handle on the #28 career-state sub-blob's bytes at the season-frame boundary. Exists
//           so SeasonSaveCodec.Encode cannot be handed a sibling career block in its place — the frame
//           now carries four same-shaped opaque byte[] payloads and a positional mistake is otherwise
//           invisible to the compiler.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #28 career-state sub-blob, as bytes, at the point it enters the season frame. Opaque here —
    /// the frame never parses it (KD-2 / FR-PG-017); this type carries no knowledge of the layout, only
    /// of which block these bytes are.
    /// <para>
    /// <b>Why this is not just a <c>byte[]</c> parameter.</b> The same argument that produced
    /// <see cref="TrainingBlock"/> / <see cref="MedicalBlock"/> / <see cref="AppearanceBlock"/>
    /// (ERR-029-005), now with a fourth confusable payload in the list. The leading magic in each block
    /// catches a transposition at LOAD; the type catches it at COMPILE, which is where a positional
    /// mistake should die.
    /// </para>
    /// </summary>
    public readonly struct ProgressionBlock
    {
        /// <summary>The encoded #28 block (<c>ProgressionSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded #28 career-state block.</summary>
        /// <param name="bytes">The block's bytes. An empty set is a well-formed zero-club block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries a progression block (FR-PG-017).</exception>
        public ProgressionBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a progression block — an empty one is a zero-club block, " +
                "not a null (FR-PG-017).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-08 | —      | #28 T1: the typed frame handle for the career-state sub-blob,      |
// |         |            |        | mirroring TrainingBlock/MedicalBlock/AppearanceBlock (ERR-029-005).|
#endregion
