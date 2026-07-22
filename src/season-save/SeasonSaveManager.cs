// File:     src/season-save/SeasonSaveManager.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / KD-1 / KD-5..KD-8;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §4.6.1.1
//           (atomic-write contract); Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  The season save-file root — writes a season (the living-world WorldStore composite plus an
//           optional in-progress MatchEngine) to disk as one file and reconstructs both. This is the
//           only assembly that may reference both match-engine and living-world (FR-LW-003 keeps them
//           independent; the season root sits above both, like match-viewer over match-engine). Save
//           captures both composites, encodes the season frame (SeasonSaveCodec), and writes atomically
//           (temp -> fsync -> rename). Load reads the file, deframes it, and rebuilds the WorldStore
//           (always) and the MatchEngine (only when the save carried a match).

using System;
using System.IO;

using Unity.Profiling;

using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// On-disk save/load for a season: one file carrying the living-world <see cref="WorldStore"/>
    /// composite and, when a match is in progress, a running <see cref="MatchEngine.MatchEngine"/>
    /// (unified-season-save-design.md). The two are nested as opaque, independently version-gated
    /// sub-blobs (KD-2) — this root never parses either, it only frames/deframes and reconstructs.
    ///
    /// Static (no injected state — the destination is a per-call path). Off the 60 Hz hot path, so the
    /// copy / blob allocations are fine (the <see cref="MatchSaveManager"/> / <c>WorldStore.Snapshot</c>
    /// precedent).
    /// </summary>
    public static class SeasonSaveManager
    {
        private static readonly ProfilerMarker s_saveMarker = new ProfilerMarker("SeasonSave.Save");
        private static readonly ProfilerMarker s_loadMarker = new ProfilerMarker("SeasonSave.Load");

        /// <summary>
        /// Captures <paramref name="world"/> and (when present) <paramref name="matchOrNull"/>, encodes
        /// the season frame, and writes it to <paramref name="path"/> atomically (the §4.6.1.1
        /// temp -> fsync -> rename contract). Both sub-blobs are captured and the frame encoded BEFORE
        /// the file is opened (the <see cref="MatchSaveManager.Save"/> blob-before-file precedent);
        /// neither capture mutates its source, so a write failure leaves the live objects and any
        /// existing destination untouched (KD-8 / AR-2 L-1). Pass <c>null</c> for
        /// <paramref name="matchOrNull"/> when the season has no in-progress match (KD-3).
        /// </summary>
        public static void Save(WorldStore world, MatchEngine.MatchEngine matchOrNull, string path)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world),
                    "A season save always carries a living-world store (KD-3).");
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Save path must be non-empty.", nameof(path));
            }

            using var _ = s_saveMarker.Auto();

            byte[] worldBlob = world.Snapshot();
            byte[] matchBlob = matchOrNull != null ? MatchSaveManager.Encode(matchOrNull) : null;
            byte[] blob = SeasonSaveCodec.Encode(worldBlob, matchBlob);

            string tempPath = path + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (FileStream fs = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(blob, 0, blob.Length);
                    fs.Flush(flushToDisk: true); // fsync barrier (§4.6.1.1)
                }

                // Atomic rename. File.Move(string,string,bool) is .NET Core 3.0+ only (absent from
                // netstandard2.1 / Unity's BCL — the SaveManager v1.3 lesson), so File.Replace covers
                // replace-existing and plain File.Move covers first-save.
                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        /// <summary>
        /// Reads the season save file at <paramref name="path"/>, deframes it, and reconstructs the
        /// living-world <see cref="WorldStore"/> (always) and the in-progress
        /// <see cref="MatchEngine.MatchEngine"/> (only when the save carried a match — otherwise
        /// <see cref="SeasonSaveContents.Match"/> is null, KD-3). Fail-loud: a missing / unreadable file
        /// surfaces the IO exception; a corrupt / version-mismatched / trailing-byte season frame throws
        /// from <see cref="SeasonSaveCodec.Decode"/>; a corrupt inner blob throws from
        /// <see cref="WorldStore.Restore"/> / the match restore path; and a distinct-squad match save
        /// loaded without (or with an incomplete) <paramref name="squads"/> throws from the match restore
        /// factory (KD-6 / R4). The match restore's fingerprint + MXCSR float-mode gates run here
        /// unchanged (KD-5).
        /// </summary>
        /// <param name="path">The season save file to read.</param>
        /// <param name="squads">The ClubId -> Squad resolver for a distinct-squad match save; ignored
        /// (may be null) for a neutral match or a no-match season (KD-6 / R4).</param>
        public static SeasonSaveContents Load(string path, ISquadProvider squads = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Load path must be non-empty.", nameof(path));
            }

            using var _ = s_loadMarker.Auto();

            byte[] blob = File.ReadAllBytes(path);
            SeasonSaveBlobs blobs = SeasonSaveCodec.Decode(blob);

            WorldStore world = WorldStore.Restore(blobs.WorldBlob);
            MatchEngine.MatchEngine match = blobs.MatchBlob != null
                ? MatchSaveManager.Restore(blobs.MatchBlob, squads)
                : null;

            return new SeasonSaveContents(world, match);
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (Exception) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-22 | —      | Initial implementation. |
#endregion
