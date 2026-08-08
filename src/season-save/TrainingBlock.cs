// File:     src/season-save/TrainingBlock.cs
// Created:  2026-08-06
// Modified: 2026-08-08 (AR pass 13 L3: the pass-12 header addition rowed — v1.1)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2;
//           Training System #29 FR-TR-018; ERR-029-005; Code Standards #20
// Purpose:  A typed handle on the #29 training sub-blob's bytes at the season-frame boundary. Exists so
//           SeasonSaveCodec.Encode cannot be handed the medical block in the training block's place —
//           the two payloads are byte-shape-identical, so a positional mistake is otherwise invisible
//           to the compiler.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The #29 training sub-blob, as bytes, at the point it enters the season frame. Opaque here — the
    /// frame never parses it (KD-2); this type carries no knowledge of the layout, only of which block
    /// these bytes are.
    /// <para>
    /// <b>Why this is not just a <c>byte[]</c> parameter.</b> The training and medical blocks have the
    /// same byte shape and the same format version, so before ERR-029-005 each codec decoded the
    /// other's bytes cleanly and silently. That is now caught at load by the leading magic — but a
    /// transposition at an <see cref="SeasonSaveCodec.Encode"/> call site had no compile-time signal at
    /// all, in a parameter list of five consecutive <c>byte[]</c>. Wrapping the two confusable ones
    /// makes the mistake a build error instead of a load-time one. It is the same discipline
    /// <c>ClubTrainingStates</c> applies one layer down, for the same reason: when two arguments have
    /// the same type and no length or shape check can separate them, the type has to.
    /// </para>
    /// </summary>
    public readonly struct TrainingBlock
    {
        /// <summary>The encoded #29 block (<c>TrainingSaveCodec.Encode</c>). Never null.</summary>
        public readonly byte[] Bytes;

        /// <summary>Wraps the encoded #29 training block.</summary>
        /// <param name="bytes">The block's bytes. An empty set is a well-formed zero-club block, never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null — a season save
        /// always carries a training block (FR-TR-018).</exception>
        public TrainingBlock(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes),
                "A season save always carries a training block — an empty one is a zero-club block, " +
                "not a null (FR-TR-018).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | ERR-029-005 (AR pass 1, H): the typed frame handle that makes a    |
// |         |            |        | training/medical transposition a build error rather than a         |
// |         |            |        | silently mis-decoded save.                                         |
// | 1.1     | 2026-08-08 | —      | AR pass 12 L4 added the missing Modified: header field; AR pass    |
// |         |            |        | 13 (L3) rows it — the addition had itself shipped rowless, the    |
// |         |            |        | FR-CS-057 class recurring inside its own fix.                     |
#endregion
