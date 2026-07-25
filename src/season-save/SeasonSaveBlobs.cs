// File:     src/season-save/SeasonSaveBlobs.cs
// Created:  2026-07-22
// Modified: 2026-07-25 (#30 T1: the third sub-blob)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-2 / KD-7 / §4;
//           Season & Competition Loop #30 Appendix B (frame), FR-SN-019; Code Standards #20
// Purpose:  The deframe result of a season save blob: the three opaque sub-blobs (the living-world
//           composite, the season state, and — when present — the match save). Pure bytes; the codec
//           never reconstructs objects (that is SeasonSaveManager), so it stays free of match-engine /
//           living-world / season-state types.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The three byte sub-blobs a season save carries (unified-season-save-design.md KD-2 + #30
    /// FR-SN-019): the living-world composite (<see cref="WorldBlob"/>, always present), the season
    /// state (<see cref="SeasonBlob"/>, always present — a season save always has a season), and the
    /// match save (<see cref="MatchBlob"/>, <c>null</c> when the season had no in-progress match —
    /// KD-3). Produced by <see cref="SeasonSaveCodec.Decode"/>; <see cref="SeasonSaveManager"/>
    /// reconstructs the actual <c>WorldStore</c> / <see cref="SeasonState"/> / <c>MatchEngine</c> from
    /// them. Kept opaque so the codec never parses any blob's internals (each keeps its own version
    /// gate).
    /// </summary>
    public readonly struct SeasonSaveBlobs
    {
        /// <summary>The living-world composite blob (<c>WorldStore.Snapshot()</c>). Never null.</summary>
        public readonly byte[] WorldBlob;

        /// <summary>The season-state blob (<see cref="SeasonStateCodec.Encode"/>). Never null — unlike
        /// the match, a season save always carries a season (FR-SN-019/021).</summary>
        public readonly byte[] SeasonBlob;

        /// <summary>The match save blob (<c>MatchSaveManager.Encode</c>), or <c>null</c> if the season
        /// had no in-progress match (KD-3).</summary>
        public readonly byte[] MatchBlob;

        /// <summary>Constructs the deframed sub-blobs.</summary>
        public SeasonSaveBlobs(byte[] worldBlob, byte[] seasonBlob, byte[] matchBlob)
        {
            WorldBlob = worldBlob;
            SeasonBlob = seasonBlob;
            MatchBlob = matchBlob;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1: gains SeasonBlob (always present) — the third sub-blob.   |
#endregion
