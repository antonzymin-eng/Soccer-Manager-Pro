// File:     src/season-save/TransfersBlock.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Transfers, Contracts & Negotiation #31 §4.4 / FR-TX-012 / Appendix B, §7.1 T1;
//           Season & Competition Loop #30 Appendix B (frame); ERR-029-005 (the typed-frame-handle rule);
//           Code Standards #20
// Purpose:  A typed handle on the #31 transfers sub-blob's bytes at the season-frame boundary — the
//           TrainingBlock / MedicalBlock / AppearanceBlock / ProgressionBlock / DisciplineBlock /
//           FinanceBlock discipline applied to the frame's ninth confusable payload.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #31 transfers sub-blob, as bytes, at the point it enters the season frame. Opaque here — the
    /// frame never parses it (KD-2, FR-TX-012); this type carries no knowledge of the layout, only of
    /// which block these bytes are.
    /// <para>
    /// Typed for ERR-029-005's reason: <see cref="SeasonSaveCodec.Encode"/> otherwise grows a seventh bare
    /// <c>byte[]</c> in a parameter list that already burned once on a silent transposition. The leading
    /// <c>TRANSFERS_SAVE_MAGIC</c> catches a mix-up at load; this catches it at compile time.
    /// </para>
    /// </summary>
    public readonly struct TransfersBlock
    {
        /// <summary>The encoded #31 block (<c>TransfersSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded transfers block.</summary>
        /// <param name="bytes">The block's bytes. A save with no #31 subsystem wired is a well-formed
        /// zero-club block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save always
        /// carries a transfers block (#31 FR-TX-012 / #30 Appendix B).</exception>
        public TransfersBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a transfers block — an unwired one is a zero-club block, " +
                "not a null.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-18 | —      | Initial implementation (#31 T1): the typed frame handle for the  |
// |         |            |        | transfers sub-blob (ERR-029-005 discipline).                     |
#endregion
