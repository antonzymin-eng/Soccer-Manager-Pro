// File:     src/season-save/DisciplineBlock.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Discipline & Suspensions #44 Appendix B; ERR-044-001 (the magic-before-version correction);
//           ERR-029-005 (the typed-frame-handle rule); Code Standards #20
// Purpose:  A typed handle on the #44 discipline sub-blob's bytes at the season-frame boundary — the
//           TrainingBlock / MedicalBlock / AppearanceBlock / ProgressionBlock discipline applied to the
//           frame's seventh confusable payload.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #44 discipline sub-blob, as bytes, at the point it enters the season frame. Opaque here —
    /// the frame never parses it (KD-2); this type carries no knowledge of the layout, only of which
    /// block these bytes are.
    /// <para>
    /// Typed for ERR-029-005's reason: <see cref="SeasonSaveCodec.Encode"/> otherwise grows another
    /// bare <c>byte[]</c> in a parameter list that already burned once on a silent transposition. The
    /// leading <c>DISCIPLINE_SAVE_MAGIC</c> catches a mix-up at load; this catches it at compile time.
    /// </para>
    /// </summary>
    public readonly struct DisciplineBlock
    {
        /// <summary>The encoded #44 discipline block (<c>DisciplineSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded discipline block.</summary>
        /// <param name="bytes">The block's bytes. An empty (no cards ever shown) set is a well-formed
        /// zero-entry block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries a discipline block (#44 Appendix B).</exception>
        public DisciplineBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a discipline block — an empty one is a zero-entry " +
                "block, not a null.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T1, roadmap C1): the typed frame      |
// |         |            |        | handle for the discipline sub-blob (ERR-029-005 discipline),      |
// |         |            |        | mirroring TrainingBlock/MedicalBlock/AppearanceBlock/             |
// |         |            |        | ProgressionBlock.                                                  |
#endregion
