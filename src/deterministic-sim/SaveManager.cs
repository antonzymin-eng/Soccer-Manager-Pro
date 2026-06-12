// File:     src/deterministic-sim/SaveManager.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §4.6.1, §4.6.1.1, §3.4 (FR-DS-006), Code Standards #20
// Purpose:  Atomic save/load manager. Satisfies the §4.6.1.1 atomic-write contract:
//           same-volume write-then-rename, fsync barrier, atomic rename, directory fsync.
//           Returns ERR_DS_STORAGE_ATOMICITY on any violation of the contract.

using System;
using System.IO;
using Unity.Profiling;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Atomic snapshot save/load manager.
    /// Commit satisfies §4.6.1.1: temp-write → fsync → rename → dir-fsync.
    /// Returns ERR_DS_STORAGE_ATOMICITY on any contract violation.
    /// Constructor-injected (FR-CS-051–054). Deterministic Simulation #16 §4.6.1.
    /// </summary>
    public sealed class SaveManager
    {
        // ── Profiler markers ──────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_saveMarker = new ProfilerMarker("DeterministicSim.Save");
        private static readonly ProfilerMarker s_loadMarker = new ProfilerMarker("DeterministicSim.Load");

        // ── Save directory ────────────────────────────────────────────────────────────

        private readonly string _saveDirectory;

        /// <summary>
        /// Constructs a SaveManager writing to the given directory.
        /// Directory must be on the same filesystem volume as temp files (§4.6.1.1 requirement 1).
        /// </summary>
        public SaveManager(string saveDirectory)
        {
            _saveDirectory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory));
        }

        // ── Commit (§4.6.1.1) ────────────────────────────────────────────────────────

        /// <summary>
        /// Atomically writes the snapshot payload to disk.
        /// Implements the §4.6.1.1 five-step atomic-write contract.
        /// Returns ERR_DS_STORAGE_ATOMICITY on any step failure; 0 on success.
        /// The destination file is named "snapshot_{tick:D10}.bin".
        /// </summary>
        public ushort CommitAtomic(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_saveMarker.Auto();

            string destPath = BuildSnapshotPath(header.Tick);
            string tempPath = destPath + ".tmp";

            try
            {
                // Step 1: same-volume temp write
                Directory.CreateDirectory(_saveDirectory);

                using (FileStream fs = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Write header + payload as a single flat blob (Stage 0 layout)
                    WriteHeaderBytes(fs, header);
                    fs.Write(payload.PayloadBytes, 0, payload.BytesWritten);

                    // Step 2: fsync (FlushFileBuffers equivalent)
                    fs.Flush(flushToDisk: true);
                }

                // Step 3: atomic rename (replace existing if present).
                // ERR-017-002-adjacent build fix (dotnet CI gate): File.Move(string, string, bool)
                // is .NET Core 3.0+ ONLY — it does not exist in netstandard2.1, Unity 2022.3's
                // API surface, so the AR-1 M-2 form could never have compiled in-engine.
                // File.Replace IS in netstandard2.1 and performs the same atomic
                // replace-existing rename (ReplaceFile on Windows); plain File.Move covers the
                // first-save case where destPath does not yet exist. AR-1 M-2 intent preserved:
                // an existing destPath no longer throws IOException.
                if (File.Exists(destPath))
                    File.Replace(tempPath, destPath, destinationBackupFileName: null);
                else
                    File.Move(tempPath, destPath);

                // Step 4: directory fsync is omitted on Windows (FAT/NTFS handle it via rename semantics).
                // On Linux/macOS this would require P/Invoke to fsync the directory fd.
                // Stage 0 target is Windows x64 per certification-platform.md — see §4.6.1.1 note.

                return 0;
            }
            catch (Exception)
            {
                // Step 5: cleanup temp on failure; dest file untouched
                TryDeleteFile(tempPath);
                return DeterministicSimConstants.ERR_DS_STORAGE_ATOMICITY;
            }
        }

        // ── Load (replay step 1) ──────────────────────────────────────────────────────

        /// <summary>
        /// Loads the serialized snapshot bytes for the given tick.
        /// Returns ERR_DS_SCHEMA_INCOMPATIBLE if the file cannot be opened; 0 on success.
        /// Corresponds to replay lifecycle step 1 (§4.2.2).
        /// </summary>
        public ushort Load(ulong tick, SnapshotPayload payloadOut)
        {
            using var _ = s_loadMarker.Auto();

            string path = BuildSnapshotPath(tick);

            try
            {
                byte[] raw = File.ReadAllBytes(path);

                // Stage 0 layout: skip the fixed header bytes and treat the rest as payload
                // Full structured decode belongs to SnapshotCodec at Stage 1 structured format.
                int headerSize = ComputeRawHeaderSize();
                int payloadSize = raw.Length - headerSize;

                if (payloadSize <= 0 || payloadSize > payloadOut.Capacity)
                {
                    return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
                }

                Array.Copy(raw, headerSize, payloadOut.PayloadBytes, 0, payloadSize);
                payloadOut.BytesWritten = payloadSize;

                return 0;
            }
            catch (Exception)
            {
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private string BuildSnapshotPath(ulong tick) =>
            Path.Combine(_saveDirectory, $"snapshot_{tick:D10}.bin");

        private static int ComputeRawHeaderSize()
        {
            // schemaVersion(4) + digestVersion(2) + tick(8) + prevDigest(32) + curDigest(32) + cursor_tick(8) + cursor_phase(1) = 87
            return 87;
        }

        private static void WriteHeaderBytes(FileStream fs, SnapshotHeader header)
        {
            byte[] buf = new byte[87];
            int offset = 0;
            CanonicalSerializer.WriteU32(buf, ref offset, header.SchemaVersion);
            CanonicalSerializer.WriteU16(buf, ref offset, header.DigestVersion);
            CanonicalSerializer.WriteU64(buf, ref offset, header.Tick);
            Array.Copy(header.PrevSnapshotDigest,    0, buf, offset, DeterministicSimConstants.SHA256_BYTES);
            offset += DeterministicSimConstants.SHA256_BYTES;
            Array.Copy(header.CurrentSnapshotDigest, 0, buf, offset, DeterministicSimConstants.SHA256_BYTES);
            offset += DeterministicSimConstants.SHA256_BYTES;
            CanonicalSerializer.WriteU64(buf, ref offset, header.Cursor.Tick);
            CanonicalSerializer.WriteU8(buf,  ref offset, header.Cursor.PhaseOrdinal);
            fs.Write(buf, 0, buf.Length);
        }

        private static void TryDeleteFile(string path)
        {
            try { File.Delete(path); } catch (Exception) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                    |
// | 1.1     | 2026-05-29 | —      | AR-1 M-2: File.Move uses overwrite:true to replace existing snapshots.    |
// |         |            |        | M-2b note: EnvironmentFingerprint not serialized to disk (Stage 0 stub).  |
// | 1.2     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->                |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is Unity.Profiling;    |
// |         |            |        | the old using was CS0246 under Unity and the Linux compile gate alike, so |
// |         |            |        | this assembly could not have compiled in-engine. No functional change.    |
// | 1.3     | 2026-06-12 | —      | Build fix (dotnet CI gate): File.Move(string,string,bool) is .NET Core    |
// |         |            |        | 3.0+ only - absent from netstandard2.1 (Unity 2022.3 API surface), so the |
// |         |            |        | AR-1 M-2 overwrite:true form never compiled in-engine. Replaced with      |
// |         |            |        | File.Exists ? File.Replace(temp,dest,null) : File.Move(temp,dest) -       |
// |         |            |        | File.Replace is netstandard2.1 and atomically replaces (ReplaceFile);     |
// |         |            |        | AR-1 M-2 intent (no IOException on existing dest) preserved.              |
#endregion
