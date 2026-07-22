// File:     src/match-engine/MatchSaveManager.cs
// Created:  2026-07-21
// Modified: 2026-07-22
// Author:   —
// Spec:     On-disk match save file (docs/tracking/match-save-file-design.md) §4 / KD-5 / KD-6;
//           Unified season save file (docs/tracking/unified-season-save-design.md) KD-5 (public blob API);
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §4.6.1.1
//           (atomic-write contract); Code Standards #20
// Purpose:  Writes a running MatchEngine to disk as a single save file and reconstructs it. Save
//           captures a durable snapshot, encodes it (MatchSaveCodec), and writes atomically
//           (temp -> fsync -> rename, the §4.6.1.1 contract). Load reads the file, decodes it, and
//           returns a ready-to-tick engine via MatchEngine.RestoreFromSnapshot. This is the on-disk
//           "SaveManager fold" the snapshot-deserialize note (N1 / Phase G-Phase 3) deferred.

using System;
using System.IO;

using Unity.Profiling;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// On-disk save/load for a <see cref="MatchEngine"/>. A save is one file: the boot seed +
    /// snapshot (header, payload) blob (<see cref="MatchSaveCodec"/>), written atomically. Load
    /// reconstructs a ready-to-tick engine — a distinct-squad save additionally needs the caller's
    /// <see cref="ISquadProvider"/> (the roster store; the file references rosters only by ClubId —
    /// match-save-file-design.md KD-4).
    ///
    /// Static (no injected state — the destination is a per-call path, unlike the deterministic-sim
    /// <see cref="SaveManager"/> which injects a directory). Off the 60 Hz hot path, so the copy /
    /// blob allocations are fine.
    /// </summary>
    public static class MatchSaveManager
    {
        private static readonly ProfilerMarker s_saveMarker = new ProfilerMarker("MatchEngine.Save");
        private static readonly ProfilerMarker s_loadMarker = new ProfilerMarker("MatchEngine.Load");

        /// <summary>
        /// Captures a durable snapshot of <paramref name="engine"/> at its current tick and encodes it as
        /// a match save blob (<see cref="MatchSaveCodec.Encode"/>) — the "match save as a value" surface.
        /// This is the encode half of <see cref="Save"/> without the file I/O: the season save-file root
        /// (<c>TacticalDirector.SeasonSave</c>) composes this blob into the unified season save, treating
        /// it as opaque, without reaching the internal capture seams (match-save-file-design.md KD-5 /
        /// unified-season-save-design.md KD-5).
        /// </summary>
        public static byte[] Encode(MatchEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            SnapshotHeader  header  = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            return MatchSaveCodec.Encode(engine.MatchSeed, header, payload);
        }

        /// <summary>
        /// Reconstructs a ready-to-tick <see cref="MatchEngine"/> from a match save blob produced by
        /// <see cref="Encode"/> (or <see cref="MatchSaveCodec.Encode"/>): decodes it and hands the
        /// (seed, header, payload) triple to <see cref="MatchEngine.RestoreFromSnapshot"/>. The decode
        /// half of <see cref="Load"/> without the file I/O — the season save-file root uses it to
        /// reconstruct the match half of the unified save. Fail-loud: a corrupt / version-mismatched /
        /// trailing-byte blob throws from <see cref="MatchSaveCodec.Decode"/>; a distinct-squad blob with
        /// a missing / incomplete <paramref name="squads"/> throws from the restore factory (KD-4 / R4).
        /// </summary>
        public static MatchEngine Restore(byte[] blob, ISquadProvider squads = null)
        {
            MatchSaveContents contents = MatchSaveCodec.Decode(blob);
            return MatchEngine.RestoreFromSnapshot(
                contents.Header, contents.Payload, contents.MatchSeed, squads);
        }

        /// <summary>
        /// Captures a durable snapshot of <paramref name="engine"/> at its current tick and writes it to
        /// <paramref name="path"/> atomically (the §4.6.1.1 temp -> fsync -> rename contract). The temp
        /// file is <c>path + ".tmp"</c> in the same directory (= same volume, §4.6.1.1 requirement 1);
        /// a failure cleans up the temp and rethrows, leaving any existing destination untouched.
        /// </summary>
        public static void Save(MatchEngine engine, string path)
        {
            // Guard engine before path so the argument-validation order is identical to the pre-Encode
            // refactor (a both-invalid call still throws ArgumentNullException, not ArgumentException).
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Save path must be non-empty.", nameof(path));
            }

            using var _ = s_saveMarker.Auto();

            byte[] blob = Encode(engine); // Encode re-guards engine (harmless backstop).

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
        /// Reads the save file at <paramref name="path"/> and reconstructs a ready-to-tick
        /// <see cref="MatchEngine"/> via <see cref="MatchEngine.RestoreFromSnapshot"/>. Fail-loud: a
        /// missing / unreadable file surfaces the IO exception; a corrupt / version-mismatched / trailing-
        /// byte blob throws from <see cref="MatchSaveCodec.Decode"/>; a distinct-squad save loaded without
        /// (or with an incomplete) <paramref name="squads"/> throws from the restore factory (KD-4 / R4).
        /// </summary>
        /// <param name="path">The save file to read.</param>
        /// <param name="squads">The ClubId -> Squad resolver for a distinct-squad save; ignored (may be
        /// null) for a neutral / unconfigured-squad save.</param>
        public static MatchEngine Load(string path, ISquadProvider squads = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Load path must be non-empty.", nameof(path));
            }

            using var _ = s_loadMarker.Auto();

            byte[] blob = File.ReadAllBytes(path);
            return Restore(blob, squads);
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (Exception) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-21 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-22 | —      | Public blob API (unified-season-save-design.md KD-5): Encode   |
// |         |            |        | (capture + MatchSaveCodec.Encode) + Restore (Decode +          |
// |         |            |        | RestoreFromSnapshot) exposed so the season save-file root can  |
// |         |            |        | compose the match blob without the internal capture seams;     |
// |         |            |        | Save/Load refactored to delegate (behaviour-identical).        |
// | 1.2     | 2026-07-22 | —      | Code AR L-1: restore the engine-before-path argument-guard     |
// |         |            |        | order in Save (the v1.1 delegation had flipped it — a both-    |
// |         |            |        | invalid call must throw ArgumentNullException, not Argument-   |
// |         |            |        | Exception), keeping the refactor exactly behaviour-identical.  |
#endregion
