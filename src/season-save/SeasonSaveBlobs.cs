// File:     src/season-save/SeasonSaveBlobs.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-2 / KD-7 / §4;
//           Code Standards #20
// Purpose:  The deframe result of a season save blob: the two opaque sub-blobs (the living-world
//           composite and, when present, the match save). Pure bytes — the codec never reconstructs
//           objects (that is SeasonSaveManager), so it stays free of match-engine / living-world types.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The two byte sub-blobs a season save carries (unified-season-save-design.md KD-2): the
    /// living-world composite (<see cref="WorldBlob"/>, always present) and the match save
    /// (<see cref="MatchBlob"/>, <c>null</c> when the season had no in-progress match — KD-3). Produced
    /// by <see cref="SeasonSaveCodec.Decode"/>; <see cref="SeasonSaveManager"/> reconstructs the actual
    /// <c>WorldStore</c> / <c>MatchEngine</c> from them. Kept opaque so the codec never parses either
    /// blob's internals (each keeps its own version gate).
    /// </summary>
    public readonly struct SeasonSaveBlobs
    {
        /// <summary>The living-world composite blob (<c>WorldStore.Snapshot()</c>). Never null.</summary>
        public readonly byte[] WorldBlob;

        /// <summary>The match save blob (<c>MatchSaveManager.Encode</c>), or <c>null</c> if the season
        /// had no in-progress match (KD-3).</summary>
        public readonly byte[] MatchBlob;

        /// <summary>Constructs the deframed sub-blobs.</summary>
        public SeasonSaveBlobs(byte[] worldBlob, byte[] matchBlob)
        {
            WorldBlob = worldBlob;
            MatchBlob = matchBlob;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-22 | —      | Initial implementation. |
#endregion
