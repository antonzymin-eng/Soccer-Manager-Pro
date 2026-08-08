// File:     src/season-save/MedicalBlock.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Injuries & Medical #41 FR-MD-017; ERR-041-009; Code Standards #20
// Purpose:  A typed handle on the #41 medical sub-blob's bytes at the season-frame boundary. The
//           counterpart to TrainingBlock — together they make a transposition of the two
//           byte-shape-identical blocks a build error.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #41 medical sub-blob, as bytes, at the point it enters the season frame. Opaque here — the
    /// frame never parses it (KD-2). See <see cref="TrainingBlock"/> for why the two blocks are wrapped
    /// rather than passed as bare <c>byte[]</c>.
    /// </summary>
    public readonly struct MedicalBlock
    {
        /// <summary>The encoded #41 block (<c>MedicalSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded #41 medical block.</summary>
        /// <param name="bytes">The block's bytes. An empty set is a well-formed zero-club block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries a medical block (FR-MD-017).</exception>
        public MedicalBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a medical block — an empty one is a zero-club block, " +
                "not a null (FR-MD-017).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | ERR-041-009 (AR pass 1, H): the typed frame handle that makes a    |
// |         |            |        | training/medical transposition a build error rather than a         |
// |         |            |        | silently mis-decoded save.                                         |
#endregion
