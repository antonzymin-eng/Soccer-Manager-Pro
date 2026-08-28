// File:     src/deterministic-sim/SaveManager.cs
// Created:  2026-05-29
// Modified: 2026-08-22 (ERR-016-010: the on-disk record implements the §3.9.2 layout — magic +
//           file-format version, the EnvironmentFingerprint, the §2.3.2 buildHash, and the
//           after-the-payload currentSnapshotDigest + recordTrailer)
// Author:   —
// Spec:     Deterministic Simulation #16 §4.6.1, §4.6.1.1, §3.9.2, §3.9.2.1, §3.4 (FR-DS-006),
//           §4.8 (FR-DS-010), §2.3.2 (FR-DS-014), Code Standards #20
// Purpose:  Atomic save/load manager. Satisfies the §4.6.1.1 atomic-write contract:
//           same-volume write-then-rename, fsync barrier, atomic rename, directory fsync.
//           Returns ERR_DS_STORAGE_ATOMICITY on any violation of the contract.

using System;
using System.IO;
using System.Text;
using Unity.Profiling;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Atomic snapshot save/load manager.
    /// Commit satisfies §4.6.1.1: temp-write → fsync → atomic rename. The directory-fsync step is
    /// a documented Stage-0 Windows carve-out (NTFS rename semantics cover it) and is deferred to a
    /// Stage 1 P/Invoke on POSIX — see the §4.6.1.1 note in CommitAtomic.
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
        /// Atomically writes the snapshot record to disk in the §3.9.2 layout.
        /// Implements the §4.6.1.1 five-step atomic-write contract.
        /// Returns ERR_DS_STORAGE_ATOMICITY on any step failure; 0 on success.
        /// The destination file is named "snapshot_{tick:D10}.bin".
        /// <para>
        /// A malformed <paramref name="header"/> — a missing §2.3.2 build hash, a wrong-width digest
        /// array, an out-of-range payload length — THROWS rather than returning a code. That split is
        /// deliberate and mirrors <c>MatchSaveCodec</c>: a return code here means the storage layer
        /// failed, and reporting a caller's malformed header as "storage atomicity" would send the
        /// reader looking at the disk.
        /// </para>
        /// </summary>
        public ushort CommitAtomic(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_saveMarker.Auto();

            // Encoded (and validated) BEFORE the try, so a caller-side defect surfaces as an
            // exception rather than being mapped onto the storage error code.
            byte[] record = EncodeRecord(header, payload);

            string destPath = BuildSnapshotPath(header.Tick);
            string tempPath = destPath + ".tmp";

            try
            {
                // Step 1: same-volume temp write
                Directory.CreateDirectory(_saveDirectory);

                using (FileStream fs = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(record, 0, record.Length);

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
        /// Loads the serialized snapshot payload for the given tick, discarding the header.
        /// Returns ERR_DS_STORAGE_ATOMICITY if the file cannot be read (missing / IO error),
        /// ERR_DS_SCHEMA_INCOMPATIBLE if the file is present but malformed (truncated / oversize),
        /// 0 on success. Corresponds to replay lifecycle step 1 (§4.2.2).
        /// Prefer the (tick, headerOut, payloadOut) overload for replay: the digest-chain,
        /// schema, and cursor validation steps (§4.2.2 steps 2/4/7) require the loaded header.
        /// </summary>
        public ushort Load(ulong tick, SnapshotPayload payloadOut) =>
            Load(tick, new SnapshotHeader(), payloadOut);

        /// <summary>
        /// Loads the serialized snapshot for the given tick, reconstructing BOTH the header and
        /// the payload from disk. Without this, replay validation (ValidateHeader / ValidatePrevDigest
        /// / cursor step 7) would run against a placeholder header and the digest chain could not be
        /// verified across a process restart (AR H-1/H-2).
        /// Returns ERR_DS_STORAGE_ATOMICITY on read/IO failure, ERR_DS_SCHEMA_INCOMPATIBLE on a
        /// truncated/oversize file, 0 on success.
        /// Since ERR-016-010 the record carries the <see cref="EnvironmentFingerprint"/> (§4.8, as
        /// FR-DS-010 and the §3.9.2 layout always required) and the §2.3.2 <c>buildHash</c>, so a
        /// disk-loaded header is a complete one and the §4.2.2 step-3 environment check is a real
        /// check rather than a guaranteed fail-closed.
        /// </summary>
        public ushort Load(ulong tick, SnapshotHeader headerOut, SnapshotPayload payloadOut)
        {
            using var _ = s_loadMarker.Auto();

            string path = BuildSnapshotPath(tick);

            byte[] raw;
            try
            {
                raw = File.ReadAllBytes(path);
            }
            catch (Exception)
            {
                // Storage-layer failure (file missing, permissions, IO) — distinct from a
                // structurally invalid snapshot, which is reported as schema-incompatible below.
                return DeterministicSimConstants.ERR_DS_STORAGE_ATOMICITY;
            }

            try
            {
                DecodeRecord(raw, headerOut, payloadOut);
            }
            catch (Exception)
            {
                // Every structural violation DecodeRecord can raise — bad magic, unknown file-format
                // version, a length prefix past the end, an oversize payload, a trailer that does not
                // match the file length, an empty build hash — is one thing to the caller: the file is
                // present but is not a snapshot record this build can read. §4.2.2 step 1 / EC-016-002.
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }

            return 0;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private string BuildSnapshotPath(ulong tick) =>
            Path.Combine(_saveDirectory, $"snapshot_{tick:D10}.bin");

        // ── Record codec (§3.9.2) ─────────────────────────────────────────────────────
        //
        // Layout, in order (all multi-byte integers little-endian per §3.4
        // SNAPSHOT_PAYLOAD_ENDIANNESS; `string` = u32 length + ASCII bytes):
        //
        //   u32     SNAPSHOT_FILE_MAGIC              -- 'S''N''A''P'; says WHICH format
        //   u32     SNAPSHOT_FILE_FORMAT_VERSION     -- says which GENERATION of it
        //   -- SnapshotHeader block ------------------------------------------------
        //   u32     schemaVersion
        //   u16     digestVersion
        //   u64     tick
        //   32B     prevSnapshotDigest
        //   u64     cursor.tick
        //   u8      cursor.phaseOrdinal
        //   string  buildHash                        -- §2.3.2; MUST be non-empty
        //   u8      fingerprintPresent               -- 1 => the six §4.8 fields follow, 0 => null
        //     i32     workerCount
        //     string  schedulerPolicy
        //     string  reductionTopology
        //     string  simdFeatureLevel
        //     string  floatModelHash
        //     string  unicodeNormalizationVersion
        //   -- SnapshotPayload block -----------------------------------------------
        //   u32     payloadLength
        //   NB      payloadBytes
        //   -- Trailing sections (§3.9.2) ------------------------------------------
        //   32B     currentSnapshotDigest            -- after the payload, per §3.9.2: it is
        //                                               excluded from the preimage of its own
        //                                               computation, and storing it inside the
        //                                               header block obscured that
        //   u64     recordTrailer                    -- total record size, integrity check
        //
        // NOTE the split of concerns: this frame's version is SNAPSHOT_FILE_FORMAT_VERSION.
        // SNAPSHOT_SCHEMA_VERSION rides INSIDE the frame and inside the §3.2.3 digest preimage, so it
        // versions the authoritative state shape, not the file. Adding identity metadata to the file
        // is not a state-shape change and deliberately does not move it -- moving it would move every
        // snapshot digest and invalidate the certified golden-vector corpus. Same three-version split
        // MATCH_SAVE_FORMAT_VERSION already draws (match-save-file-design.md KD-1).

        private const int DigestBytes = DeterministicSimConstants.SHA256_BYTES;
        private const byte FingerprintAbsent  = 0;
        private const byte FingerprintPresent = 1;

        private const string RecordSubject = "Snapshot record";

        /// Builds the complete §3.9.2 record. Throws on a header this codec's own DecodeRecord would
        /// refuse -- a codec able to write a file its own reader rejects is a defect this project has
        /// filed against sibling codecs more than once.
        private static byte[] EncodeRecord(SnapshotHeader header, SnapshotPayload payload)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (header.PrevSnapshotDigest == null || header.PrevSnapshotDigest.Length != DigestBytes ||
                header.CurrentSnapshotDigest == null || header.CurrentSnapshotDigest.Length != DigestBytes)
            {
                throw new ArgumentException(
                    "SnapshotHeader digest arrays must each be exactly " + DigestBytes + " bytes.",
                    nameof(header));
            }
            if (string.IsNullOrEmpty(header.BuildHash))
            {
                throw new ArgumentException(
                    "SnapshotHeader.BuildHash is empty — a snapshot record must name the compiled " +
                    "binaries that wrote it (#16 §2.3.2 / FR-DS-014).", nameof(header));
            }
            if (payload.BytesWritten < 0 || payload.BytesWritten > payload.Capacity)
            {
                throw new ArgumentException(
                    "SnapshotPayload.BytesWritten (" + payload.BytesWritten + ") is out of range [0, " +
                    payload.Capacity + "].", nameof(payload));
            }

            EnvironmentFingerprint fp = header.Fingerprint;
            byte[] buf = new byte[ComputeRecordSize(fp, header.BuildHash, payload.BytesWritten)];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, DeterministicSimConstants.SNAPSHOT_FILE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, DeterministicSimConstants.SNAPSHOT_FILE_FORMAT_VERSION);

            CanonicalSerializer.WriteU32(buf, ref o, header.SchemaVersion);
            CanonicalSerializer.WriteU16(buf, ref o, header.DigestVersion);
            CanonicalSerializer.WriteU64(buf, ref o, header.Tick);
            Array.Copy(header.PrevSnapshotDigest, 0, buf, o, DigestBytes); o += DigestBytes;
            CanonicalSerializer.WriteU64(buf, ref o, header.Cursor.Tick);
            CanonicalSerializer.WriteU8 (buf, ref o, header.Cursor.PhaseOrdinal);
            CanonicalSerializer.WriteString(buf, ref o, header.BuildHash);

            if (fp != null)
            {
                CanonicalSerializer.WriteU8(buf, ref o, FingerprintPresent);
                CanonicalSerializer.WriteI32(buf,    ref o, fp.WorkerCount);
                CanonicalSerializer.WriteString(buf, ref o, fp.SchedulerPolicy);
                CanonicalSerializer.WriteString(buf, ref o, fp.ReductionTopology);
                CanonicalSerializer.WriteString(buf, ref o, fp.SimdFeatureLevel);
                CanonicalSerializer.WriteString(buf, ref o, fp.FloatModelHash);
                CanonicalSerializer.WriteString(buf, ref o, fp.UnicodeNormalizationVersion);
            }
            else
            {
                // A null fingerprint round-trips as null rather than being invented (the KD-3
                // presence-flag precedent). ReplayEngine step 3 then fails closed, which is the
                // correct outcome for a record whose environment is genuinely unknown.
                CanonicalSerializer.WriteU8(buf, ref o, FingerprintAbsent);
            }

            CanonicalSerializer.WriteU32(buf, ref o, (uint)payload.BytesWritten);
            Array.Copy(payload.PayloadBytes, 0, buf, o, payload.BytesWritten); o += payload.BytesWritten;

            Array.Copy(header.CurrentSnapshotDigest, 0, buf, o, DigestBytes); o += DigestBytes;
            CanonicalSerializer.WriteU64(buf, ref o, (ulong)buf.Length);

            if (o != buf.Length)
            {
                // Guards ComputeRecordSize against EncodeRecord drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "SaveManager.EncodeRecord wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — ComputeRecordSize is out of sync with EncodeRecord.");
            }
            return buf;
        }

        /// Inverse of <see cref="EncodeRecord"/>. Throws on every structural violation; the Load
        /// overload maps that to ERR_DS_SCHEMA_INCOMPATIBLE.
        private static void DecodeRecord(byte[] raw, SnapshotHeader headerOut, SnapshotPayload payloadOut)
        {
            int len = raw.Length;
            int o = 0;

            SaveBlobFramingHelpers.Require(o, 8, len, RecordSubject, "magic and file-format version");
            uint magic = CanonicalSerializer.ReadU32(raw, ref o);
            if (magic != DeterministicSimConstants.SNAPSHOT_FILE_MAGIC)
            {
                // Also the gate that refuses a file written by the pre-ERR-016-010 unversioned layout,
                // whose first four bytes were the schema version — refused, never mis-parsed.
                throw new InvalidOperationException(
                    "Snapshot record magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    DeterministicSimConstants.SNAPSHOT_FILE_MAGIC.ToString("X8") +
                    " — these bytes are not a snapshot record (#16 §3.9.2.1).");
            }
            uint fileVersion = CanonicalSerializer.ReadU32(raw, ref o);
            if (fileVersion != DeterministicSimConstants.SNAPSHOT_FILE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Snapshot record file-format version " + fileVersion + " != expected " +
                    DeterministicSimConstants.SNAPSHOT_FILE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0.");
            }

            SaveBlobFramingHelpers.Require(o, 4 + 2 + 8 + DigestBytes + 8 + 1, len, RecordSubject, "snapshot header");
            headerOut.SchemaVersion = CanonicalSerializer.ReadU32(raw, ref o);
            headerOut.DigestVersion = CanonicalSerializer.ReadU16(raw, ref o);
            headerOut.Tick          = CanonicalSerializer.ReadU64(raw, ref o);
            Array.Copy(raw, o, headerOut.PrevSnapshotDigest, 0, DigestBytes); o += DigestBytes;
            ulong cursorTick  = CanonicalSerializer.ReadU64(raw, ref o);
            byte  cursorPhase = CanonicalSerializer.ReadU8 (raw, ref o);
            headerOut.Cursor  = new ReplayCursor(cursorTick, cursorPhase);

            headerOut.BuildHash = ReadBoundedString(raw, ref o, len, "header buildHash");
            if (headerOut.BuildHash.Length == 0)
            {
                throw new InvalidOperationException(
                    "Snapshot record carries an empty buildHash — refusing a record whose build " +
                    "identity is unknown (#16 §2.3.2 / FR-DS-014).");
            }

            SaveBlobFramingHelpers.Require(o, 1, len, RecordSubject, "fingerprint-present flag");
            byte fpFlag = CanonicalSerializer.ReadU8(raw, ref o);
            if (fpFlag == FingerprintPresent)
            {
                SaveBlobFramingHelpers.Require(o, 4, len, RecordSubject, "fingerprint worker count");
                int    workerCount = CanonicalSerializer.ReadI32(raw, ref o);
                string scheduler   = ReadBoundedString(raw, ref o, len, "fingerprint schedulerPolicy");
                string reduction   = ReadBoundedString(raw, ref o, len, "fingerprint reductionTopology");
                string simd        = ReadBoundedString(raw, ref o, len, "fingerprint simdFeatureLevel");
                string floatModel  = ReadBoundedString(raw, ref o, len, "fingerprint floatModelHash");
                string unicode     = ReadBoundedString(raw, ref o, len, "fingerprint unicodeNormalizationVersion");
                var fp = new EnvironmentFingerprint(
                    workerCount, scheduler, reduction, simd, floatModel, unicode);
                fp.Lock(); // §4.8.1 lifecycle — a reconstructed fingerprint is already sealed
                headerOut.Fingerprint = fp;
            }
            else if (fpFlag == FingerprintAbsent)
            {
                headerOut.Fingerprint = null;
            }
            else
            {
                throw new InvalidOperationException(
                    "Snapshot record fingerprint-present flag " + fpFlag + " is neither 0 nor 1 — corrupt record.");
            }

            SaveBlobFramingHelpers.Require(o, 4, len, RecordSubject, "payload length");
            uint payloadLen = CanonicalSerializer.ReadU32(raw, ref o);
            if (payloadLen > (uint)payloadOut.Capacity)
            {
                throw new InvalidOperationException(
                    "Snapshot record payload length " + payloadLen + " exceeds SnapshotPayload capacity " +
                    payloadOut.Capacity + " — corrupt or incompatible record.");
            }
            SaveBlobFramingHelpers.Require(o, (int)payloadLen, len, RecordSubject, "payload body");
            Array.Copy(raw, o, payloadOut.PayloadBytes, 0, (int)payloadLen); o += (int)payloadLen;
            payloadOut.BytesWritten = (int)payloadLen;

            SaveBlobFramingHelpers.Require(o, DigestBytes + 8, len, RecordSubject, "current digest and record trailer");
            Array.Copy(raw, o, headerOut.CurrentSnapshotDigest, 0, DigestBytes); o += DigestBytes;
            ulong declaredSize = CanonicalSerializer.ReadU64(raw, ref o);

            if (declaredSize != (ulong)len)
            {
                throw new InvalidOperationException(
                    "Snapshot record trailer declares " + declaredSize + " bytes but the file is " + len +
                    " — truncated, padded, or corrupt (§3.9.2 record trailer).");
            }
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Snapshot record has " + (len - o) + " trailing byte(s) after the declared content.");
            }
        }

        private static int ComputeRecordSize(EnvironmentFingerprint fp, string buildHash, int payloadBytes)
        {
            int size = 4 + 4                   // magic + file-format version
                     + 4 + 2 + 8               // schemaVersion + digestVersion + tick
                     + DigestBytes             // prevSnapshotDigest
                     + 8 + 1                   // cursor.tick + cursor.phaseOrdinal
                     + StringSize(buildHash)
                     + 1;                      // fingerprintPresent flag
            if (fp != null)
            {
                size += 4                      // workerCount
                      + StringSize(fp.SchedulerPolicy)
                      + StringSize(fp.ReductionTopology)
                      + StringSize(fp.SimdFeatureLevel)
                      + StringSize(fp.FloatModelHash)
                      + StringSize(fp.UnicodeNormalizationVersion);
            }
            size += 4 + payloadBytes;          // payloadLength + body
            size += DigestBytes + 8;           // currentSnapshotDigest + recordTrailer
            return size;
        }

        // Mirrors CanonicalSerializer.WriteString's on-wire size (u32 length + ASCII bytes).
        private static int StringSize(string s) =>
            4 + (s == null ? 0 : Encoding.ASCII.GetByteCount(s));

        // Bounded ReadString: refuses a length prefix that would read past the record rather than
        // throwing IndexOutOfRange or returning garbage (the MatchSaveCodec KD-6 posture).
        private static string ReadBoundedString(byte[] raw, ref int o, int total, string what)
        {
            SaveBlobFramingHelpers.Require(o, 4, total, RecordSubject, what + " length");
            uint slen = CanonicalSerializer.ReadU32(raw, ref o);
            if (slen == 0u)
            {
                return string.Empty;
            }
            SaveBlobFramingHelpers.Require(o, (int)slen, total, RecordSubject, what + " body");
            string s = Encoding.ASCII.GetString(raw, o, (int)slen);
            o += (int)slen;
            return s;
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
// | 1.4     | 2026-06-15 | —      | AR fix L-2: Load() returns ERR_DS_STORAGE_ATOMICITY for a read/IO         |
// |         |            |        | failure (missing file etc.) and reserves ERR_DS_SCHEMA_INCOMPATIBLE for   |
// |         |            |        | a present-but-malformed file, instead of collapsing both. AR fix L-5:     |
// |         |            |        | class doc no longer asserts dir-fsync as part of the satisfied contract   |
// |         |            |        | (Stage-0 Windows carve-out; POSIX dir-fsync deferred to Stage 1).         |
// | 1.5     | 2026-06-16 | —      | AR fix H-1 (foundation review): new ReadHeaderBytes + Load(tick,          |
// |         |            |        | headerOut, payloadOut) overload reconstruct the SnapshotHeader from the   |
// |         |            |        | on-disk bytes so replay's ValidateHeader / ValidatePrevDigest / cursor    |
// |         |            |        | step-7 run against the LOADED header instead of a placeholder — the       |
// |         |            |        | digest chain is now verifiable across a process restart. Purely additive  |
// |         |            |        | (old payload-only Load delegates to the new overload; on-disk format      |
// |         |            |        | unchanged). Fingerprint stays null on a disk load (M-4: serializing it    |
// |         |            |        | needs a SNAPSHOT_SCHEMA_VERSION bump — filed for gate-verified follow-up). |
// | 1.6     | 2026-08-22 | —      | ERR-016-009: ReadHeaderBytes sets BuildHash = null explicitly and the     |
// |         |            |        | Load doc records that this Stage-0 87-byte layout carries neither the     |
// |         |            |        | fingerprint nor the §2.3.2 build hash. No format change — the layout is   |
// |         |            |        | untouched, so SNAPSHOT_SCHEMA_VERSION does not move.                      |
// | 1.7     | 2026-08-22 | —      | ERR-016-010: the fixed 87-byte layout is REPLACED by the §3.9.2 record.   |
// |         |            |        | It had contradicted that normative layout four ways at once — no          |
// |         |            |        | environmentFingerprint (which FR-DS-010 and §3.9.2 both require), no      |
// |         |            |        | recordTrailer, currentSnapshotDigest inside the header instead of after   |
// |         |            |        | the payload, and no format identifier at all. Now: magic-led              |
// |         |            |        | (SNAPSHOT_FILE_MAGIC) + SNAPSHOT_FILE_FORMAT_VERSION, then the header     |
// |         |            |        | block, the §2.3.2 buildHash (non-empty, refused at BOTH ends), the        |
// |         |            |        | presence-flagged fingerprint, a length-prefixed payload, then             |
// |         |            |        | currentSnapshotDigest and the u64 recordTrailer. Every bound is checked   |
// |         |            |        | through SaveBlobFramingHelpers.Require. SNAPSHOT_SCHEMA_VERSION does NOT  |
// |         |            |        | move: it versions the authoritative state shape and rides in the §3.2.3   |
// |         |            |        | digest preimage, so moving it would invalidate the certified golden       |
// |         |            |        | vectors — the file frame gets its own version instead (the               |
// |         |            |        | MATCH_SAVE_FORMAT_VERSION precedent). CommitAtomic now THROWS on a       |
// |         |            |        | malformed header rather than reporting it as a storage failure.          |
#endregion
