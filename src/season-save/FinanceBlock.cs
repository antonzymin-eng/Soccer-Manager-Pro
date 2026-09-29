// File:     src/season-save/FinanceBlock.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Club Finances & Economy #40 §4.4 / FR-FN-020/021/022 (finance persistence), §7.1 T1b;
//           Season & Competition Loop #30 Appendix B (frame), ERR-030-049;
//           ERR-029-005 (the typed-frame-handle rule); Code Standards #20
// Purpose:  A typed handle on the #40 finance sub-blob's bytes at the season-frame boundary — the
//           TrainingBlock / MedicalBlock / AppearanceBlock / ProgressionBlock / DisciplineBlock
//           discipline applied to the frame's eighth confusable payload.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #40 finance sub-blob, as bytes, at the point it enters the season frame. Opaque here —
    /// the frame never parses it (KD-2); this type carries no knowledge of the layout, only of which
    /// block these bytes are.
    /// <para>
    /// Typed for ERR-029-005's reason: <see cref="SeasonSaveCodec.Encode"/> otherwise grows another
    /// bare <c>byte[]</c> in a parameter list that already burned once on a silent transposition. The
    /// leading <c>FINANCE_SAVE_MAGIC</c> catches a mix-up at load; this catches it at compile time.
    /// </para>
    /// </summary>
    public readonly struct FinanceBlock
    {
        /// <summary>The encoded #40 finance block
        /// (<c>ClubFinancesSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded finance block.</summary>
        /// <param name="bytes">The block's bytes. A season whose clubs have no finance entries yet
        /// (every season before #40 T2 bootstraps them) is a well-formed zero-club block, never
        /// null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries a finance block (#40 FR-FN-020 / #30 Appendix B).</exception>
        public FinanceBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a finance block — an empty one is a zero-club " +
                "block, not a null.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-09-10 | —      | Initial implementation (#40 T1b, ERR-030-049): the typed frame   |
// |         |            |        | handle for the finance sub-blob (ERR-029-005 discipline),        |
// |         |            |        | mirroring TrainingBlock/MedicalBlock/AppearanceBlock/            |
// |         |            |        | ProgressionBlock/DisciplineBlock.                                 |
#endregion
