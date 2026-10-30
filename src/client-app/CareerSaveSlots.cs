// File:     src/client-app/CareerSaveSlots.cs
// Created:  2026-10-18
// Modified: 2026-10-29
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2 save promise, D-10 breakable
//           saves through PM-3), ux-experience-architecture.md §7, Season & Competition Loop #30 §4
//           (SeasonSaveManager), Code Standards #20
// Purpose:  The client-owned career slot store over one directory: where each slot's season save
//           lives, the sidecar that lets the picker and Continue read a slot without decoding it, and
//           crash-safe replacement of both.

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerProgression;
using TacticalDirector.SeasonSave;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The career save slots in one directory (§5-P7 D-P7-2). Each slot is two files: the season save
    /// <c>SeasonSaveManager</c> writes (<c>&lt;slot&gt;.tdsave</c>, format owned by #30) and a small
    /// client-owned text sidecar (<c>&lt;slot&gt;.slot</c>) recording the write sequence and the
    /// career facts the picker shows.
    /// <para>
    /// <b>Why a sidecar.</b> The picker must not decode four whole careers to draw four rows, and
    /// Continue needs "the most recent save" without reading the wall clock or file timestamps — a
    /// store-wide monotonic <see cref="CareerSlotSummary.Sequence"/> answers it deterministically.
    /// </para>
    /// <para>
    /// <b>Crash safety.</b> Both files are written in full (and flushed to disk) to temporary siblings
    /// before either destination is touched; the save is then swapped in with <c>File.Replace</c>
    /// (keeping the previous save as a backup) and the sidecar after it (keeping the previous sidecar
    /// as a backup too, since a failed replace can leave the replaced file only under the backup name).
    /// A failure at any step therefore leaves the previous save + sidecar pair in place: a failed
    /// sidecar swap puts both backups back. A process death between the two swaps leaves the new save
    /// beside its staged sidecar temp; opening the store recovers it. Each sidecar records the SHA-256
    /// of the save it describes, so recovery pairs files by content rather than by guessing which step
    /// was interrupted: a staged sidecar that matches the save is swapped in; a slot whose write
    /// committed (its sidecar in place, or set aside by <see cref="MarkDamaged"/>) keeps its pair and
    /// drops its backups; and otherwise backups that pair are put back. Once the new pair is live,
    /// removing the backups is best effort, so a committed write is never reported as failed.
    /// </para>
    /// <para>
    /// <b>D-10.</b> Saves are breakable through PM-3, so a slot whose sidecar records another
    /// <c>SEASON_SAVE_FORMAT_VERSION</c>, or another version of any sub-blob nested inside it (KD-2:
    /// each is gated independently, so an inner bump leaves the outer version alone), is
    /// <see cref="CareerSlotState.Incompatible"/>: listed, never loaded, never picked by Continue, and
    /// freely overwritten. A slot whose save fails to load anyway is invalidated by
    /// <see cref="MarkDamaged"/>, so it cannot keep winning Continue. Invalidation is reversible: the
    /// sidecar is set aside rather than deleted, and <see cref="RestoreDamaged"/> puts it back while it
    /// still describes the save.
    /// </para>
    /// Not thread-safe; the client drives one store from one thread.
    /// </summary>
    public sealed class CareerSaveSlots
    {
        private const string SAVE_EXTENSION = ".tdsave";
        private const string SIDECAR_EXTENSION = ".slot";
        private const string TEMP_SUFFIX = ".tmp";
        private const string BACKUP_SUFFIX = ".bak";
        private const string DAMAGED_SUFFIX = ".damaged";

        // Every format gate a client-written save passes on load besides the outer
        // SEASON_SAVE_FORMAT_VERSION: the eight mandatory sub-blobs and the world snapshot nested in the
        // world blob. The match blob is absent: the client never writes one and Resume refuses one.
        // Recorded in each sidecar and compared whole, so a bump to any of them makes older saves
        // Incompatible rather than Ready-then-failing (D-10).
        internal static readonly string NestedFormatStamp =
            "world=" + LivingWorldConstants.WORLD_STORE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",worldSnapshot=" + LivingWorldConstants.WORLD_SNAPSHOT_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",seasonState=" + SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",training=" + TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",medical=" + InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",appearance=" + SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",progression=" + PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",discipline=" + DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)
            + ",finance=" + ClubFinancesConstants.FINANCE_SAVE_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture);

        // No byte-order mark: a BOM would prefix the first key and the sidecar would never parse.
        private static readonly Encoding SidecarEncoding = new UTF8Encoding(false);

        private readonly string _directory;

        /// <summary>
        /// Opens (and creates if needed) the slot store rooted at <paramref name="directory"/>, recovering
        /// any slot whose last write was interrupted between its two swaps (see the class remarks).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="directory"/> is null or empty.</exception>
        public CareerSaveSlots(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The save directory must be non-empty.", nameof(directory));
            }

            _directory = directory;
            Directory.CreateDirectory(_directory);
            for (int i = 0; i < SlotCount; i++)
            {
                RecoverInterruptedWrite((CareerSaveSlot)i);
            }
        }

        /// <summary>The directory this store owns.</summary>
        public string DirectoryPath => _directory;

        /// <summary>Number of slots (autosave + manual).</summary>
        public static int SlotCount => 1 + ClientAppConstants.CAREER_MANUAL_SLOT_COUNT;

        /// <summary>Every slot's summary, indexed by <see cref="CareerSaveSlot"/> ordinal.</summary>
        public CareerSlotSummary[] List()
        {
            var rows = new CareerSlotSummary[SlotCount];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = Describe((CareerSaveSlot)i);
            }

            return rows;
        }

        /// <summary>The summary of one slot.</summary>
        public CareerSlotSummary Describe(CareerSaveSlot slot)
        {
            RequireSlot(slot);
            string savePath = SavePath(slot);
            string sidecarPath = SidecarPath(slot);
            bool hasSave = File.Exists(savePath);
            bool hasSidecar = File.Exists(sidecarPath);

            if (!hasSave && !hasSidecar)
            {
                return CareerSlotSummary.Without(slot, CareerSlotState.Empty);
            }

            if (!hasSave || !hasSidecar || !TryReadSidecar(slot, sidecarPath, out CareerSlotSummary summary))
            {
                return CareerSlotSummary.Without(slot, CareerSlotState.Damaged);
            }

            return summary;
        }

        /// <summary>
        /// The most recently written <see cref="CareerSlotState.Ready"/> slot — what Continue loads
        /// (D-P7-2). False when no slot is loadable.
        /// </summary>
        public bool TryGetLatest(out CareerSlotSummary latest)
        {
            latest = default;
            bool found = false;
            foreach (CareerSlotSummary row in List())
            {
                if (row.IsLoadable && (!found || row.Sequence > latest.Sequence))
                {
                    latest = row;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Writes <paramref name="loop"/>'s career to <paramref name="slot"/>, replacing whatever was
        /// there, and returns the new summary. The loop must be quiescent (no match being ticked).
        /// </summary>
        /// <param name="slot">The destination slot.</param>
        /// <param name="loop">The career's season loop.</param>
        /// <param name="worldSeed">The career's world seed (the loop keeps its world store internal).</param>
        /// <param name="managedClubName">The managed club's display name, recorded for the picker.</param>
        /// <exception cref="ArgumentNullException"><paramref name="loop"/> is null.</exception>
        public CareerSlotSummary Write(CareerSaveSlot slot, SeasonLoop loop, ulong worldSeed, string managedClubName)
        {
            RequireSlot(slot);
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            long sequence = NextSequence();
            SeasonState season = loop.State;
            var summary = new CareerSlotSummary(
                slot,
                CareerSlotState.Ready,
                sequence,
                SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION,
                worldSeed,
                season.ManagedClubId,
                managedClubName,
                season.SeasonNumber,
                loop.CurrentWorldDay,
                season.Calendar.NextRoundIndex,
                season.Calendar.RoundCount,
                season.PositionOf(season.ManagedClubId));

            string savePath = SavePath(slot);
            string saveTemp = savePath + TEMP_SUFFIX;
            string saveBackup = savePath + BACKUP_SUFFIX;
            string sidecarPath = SidecarPath(slot);
            string sidecarTemp = sidecarPath + TEMP_SUFFIX;
            string sidecarBackup = sidecarPath + BACKUP_SUFFIX;
            try
            {
                // Everything that can fail on content is done before a destination is touched.
                SeasonSaveManager.Save(loop, null, saveTemp);
                WriteDurably(sidecarTemp, FormatSidecar(in summary, DigestOf(saveTemp)));

                // Backups left by an interrupted earlier write belong to the pair this write replaces;
                // SwapIn clears them, so a backup restored below is always this write's own.
                bool hadSave = File.Exists(savePath);
                SwapIn(saveTemp, savePath, saveBackup);
                try
                {
                    SwapIn(sidecarTemp, sidecarPath, sidecarBackup);
                }
                catch
                {
                    RestorePreviousPair(hadSave, saveBackup, savePath, sidecarBackup, sidecarPath);
                    throw;
                }

                // The new pair is live. Cleanup is best effort: a file held open (by a virus scanner,
                // say) must not report a committed write as failed, and recovery at the next open
                // never revives what is left from it.
                TryDeleteIfPresent(saveBackup);
                TryDeleteIfPresent(sidecarBackup);
                // A sidecar set aside by MarkDamaged described the save this write just replaced.
                TryDeleteIfPresent(DamagedSidecarPath(slot));
            }
            finally
            {
                // Never a stray half-written temp. The backup is kept only if restoring it failed.
                DeleteIfPresent(saveTemp);
                DeleteIfPresent(sidecarTemp);
            }

            return summary;
        }

        /// <summary>
        /// Invalidates <paramref name="slot"/> after its save failed to load, so it reads as
        /// <see cref="CareerSlotState.Damaged"/> and Continue moves on to the next loadable slot. Nothing
        /// is deleted: the save bytes stay, and the sidecar is set aside so that
        /// <see cref="RestoreDamaged"/> can undo this (a load can fail on this build's code rather than
        /// on the file). The slot is freely overwritten. Best effort: false when the sidecar could not
        /// be set aside.
        /// </summary>
        public bool MarkDamaged(CareerSaveSlot slot)
        {
            RequireSlot(slot);
            try
            {
                string sidecarPath = SidecarPath(slot);
                if (File.Exists(sidecarPath))
                {
                    SwapIn(sidecarPath, DamagedSidecarPath(slot), null);
                }

                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Undoes <see cref="MarkDamaged"/>: puts the set-aside sidecar back, so the slot reads as it did
        /// before it was invalidated. Only while that sidecar still describes the save byte for byte
        /// (its recorded digest) and the slot has not been written since. Best effort: false when there
        /// is nothing to restore, it no longer matches, or the files could not be read or moved.
        /// <para>
        /// Groundwork: nothing in the client calls this yet. A digest match cannot tell a save that
        /// failed on an old build's code from one that is corrupt, so retrying set-aside slots needs a
        /// player-facing action (with approved copy) rather than an automatic restore at open.
        /// </para>
        /// </summary>
        public bool RestoreDamaged(CareerSaveSlot slot)
        {
            RequireSlot(slot);
            string savePath = SavePath(slot);
            string sidecarPath = SidecarPath(slot);
            string damagedPath = DamagedSidecarPath(slot);
            try
            {
                if (File.Exists(sidecarPath) || !File.Exists(savePath)
                    || !SidecarDescribes(slot, damagedPath, DigestOf(savePath)))
                {
                    return false;
                }

                File.Move(damagedPath, sidecarPath);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Loads the season save in <paramref name="slot"/>.</summary>
        /// <exception cref="InvalidOperationException">The slot is not <see cref="CareerSlotState.Ready"/>.</exception>
        /// <remarks>Decode failures from #30's codecs propagate unchanged; the caller reports them persistently.</remarks>
        public SeasonSaveContents Load(CareerSaveSlot slot)
        {
            CareerSlotSummary summary = Describe(slot);
            if (!summary.IsLoadable)
            {
                throw new InvalidOperationException(
                    "Slot " + slot + " is " + summary.State + " and cannot be loaded.");
            }

            return SeasonSaveManager.Load(SavePath(slot));
        }

        /// <summary>The season-save path of <paramref name="slot"/>.</summary>
        public string SavePath(CareerSaveSlot slot) => Path.Combine(_directory, FileStem(slot) + SAVE_EXTENSION);

        private string SidecarPath(CareerSaveSlot slot) => Path.Combine(_directory, FileStem(slot) + SIDECAR_EXTENSION);

        private string DamagedSidecarPath(CareerSaveSlot slot) => SidecarPath(slot) + DAMAGED_SUFFIX;

        private static string FileStem(CareerSaveSlot slot) =>
            slot == CareerSaveSlot.Autosave
                ? "autosave"
                : "manual-" + ((int)slot).ToString(CultureInfo.InvariantCulture);

        private long NextSequence()
        {
            long max = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = (CareerSaveSlot)i;
                // A set-aside sidecar counts too: RestoreDamaged may bring it back, and its sequence
                // must not then tie with one handed out meanwhile.
                max = Math.Max(max, SequenceIn(slot, SidecarPath(slot)));
                max = Math.Max(max, SequenceIn(slot, DamagedSidecarPath(slot)));
            }

            return max + 1;
        }

        private static long SequenceIn(CareerSaveSlot slot, string sidecarPath) =>
            File.Exists(sidecarPath) && TryReadSidecar(slot, sidecarPath, out CareerSlotSummary row) ? row.Sequence : 0;

        // Moves tempPath into destinationPath, replacing any file there. With a backup path, a stale
        // backup is cleared first and the replaced file is kept under that name; File.Replace can fail
        // after that rename, leaving the replaced file only as the backup (see MoveBackIfMissing).
        private static void SwapIn(string tempPath, string destinationPath, string backupPath)
        {
            if (backupPath != null)
            {
                DeleteIfPresent(backupPath);
            }

            if (File.Exists(destinationPath))
            {
                File.Replace(tempPath, destinationPath, backupPath);
            }
            else
            {
                File.Move(tempPath, destinationPath);
            }
        }

        // Puts a file back from its backup when a replace stopped after moving it aside.
        private static void MoveBackIfMissing(string path, string backupPath)
        {
            if (!File.Exists(path) && File.Exists(backupPath))
            {
                File.Move(backupPath, path);
            }
        }

        // Runs once per slot when the store opens. Only a process death leaves these files behind:
        // Write removes its temps on any exception. Pairs by content (each sidecar records its save's
        // digest), so it never needs to know which step was interrupted. Best effort: on an IO failure
        // the files stay as they are and the slot reads as whatever Describe makes of them.
        private void RecoverInterruptedWrite(CareerSaveSlot slot)
        {
            string savePath = SavePath(slot);
            string saveTemp = savePath + TEMP_SUFFIX;
            string saveBackup = savePath + BACKUP_SUFFIX;
            string sidecarPath = SidecarPath(slot);
            string sidecarTemp = sidecarPath + TEMP_SUFFIX;
            string sidecarBackup = sidecarPath + BACKUP_SUFFIX;
            string damagedPath = DamagedSidecarPath(slot);
            // SeasonSaveManager.Save stages its own atomic write beside the path it is given.
            string managerTemp = saveTemp + TEMP_SUFFIX;
            if (!File.Exists(saveTemp) && !File.Exists(managerTemp) && !File.Exists(sidecarTemp)
                && !File.Exists(saveBackup) && !File.Exists(sidecarBackup))
            {
                return;
            }

            try
            {
                // A save temp never became the slot's save: the write stopped before its first swap.
                DeleteIfPresent(managerTemp);
                DeleteIfPresent(saveTemp);

                // A save swap that stopped after moving the previous save aside left it only as the
                // backup (and the set-aside sidecar of a Damaged slot may describe it).
                MoveBackIfMissing(savePath, saveBackup);

                string liveDigest = File.Exists(savePath) ? DigestOf(savePath) : null;
                if (liveDigest != null && SidecarDescribes(slot, sidecarTemp, liveDigest))
                {
                    // The save swap landed and the sidecar swap did not: finish the write, as Write does.
                    SwapIn(sidecarTemp, sidecarPath, null);
                    DeleteIfPresent(saveBackup);
                    DeleteIfPresent(sidecarBackup);
                    DeleteIfPresent(damagedPath);
                    return;
                }

                // A staged sidecar read in full that does not describe the live save is never needed again.
                DeleteIfPresent(sidecarTemp);

                // The live save's write committed: its sidecar is in place, or MarkDamaged has set it
                // aside since. The backups belong to the pair that write replaced, so they go, and a
                // set-aside slot is never revived from them.
                if (liveDigest != null && (SidecarDescribes(slot, sidecarPath, liveDigest)
                    || (!File.Exists(sidecarPath) && SidecarDescribes(slot, damagedPath, liveDigest))))
                {
                    DeleteIfPresent(saveBackup);
                    DeleteIfPresent(sidecarBackup);
                    return;
                }

                // A sidecar swap that stopped after moving the previous sidecar aside, with the save
                // already put back, left that sidecar only as the backup.
                if (liveDigest != null && !File.Exists(sidecarPath) && SidecarDescribes(slot, sidecarBackup, liveDigest))
                {
                    MoveBackIfMissing(sidecarPath, sidecarBackup);
                    DeleteIfPresent(saveBackup);
                    return;
                }

                if (File.Exists(saveBackup))
                {
                    // The new save swapped in but its sidecar was lost: put the previous pair back, its
                    // sidecar either still in place or left only as the backup.
                    string backupDigest = DigestOf(saveBackup);
                    if (SidecarDescribes(slot, sidecarPath, backupDigest)
                        || (!File.Exists(sidecarPath) && SidecarDescribes(slot, sidecarBackup, backupDigest)))
                    {
                        SwapIn(saveBackup, savePath, null);
                        MoveBackIfMissing(sidecarPath, sidecarBackup);
                        DeleteIfPresent(sidecarBackup);
                        return;
                    }
                }

                // Otherwise nothing pairs and the slot reads as Damaged. A sidecar backup describing
                // neither save is useless on its own and would re-run this recovery at every open; a
                // save backup is save data and stays until the slot is next written.
                DeleteIfPresent(sidecarBackup);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // Recovery's read: unlike TryReadSidecar it lets an IO failure propagate, so a sidecar that is
        // merely unreadable right now (locked, say) aborts recovery with every file kept, rather than
        // being judged "does not describe" and deleted.
        private static bool SidecarDescribes(CareerSaveSlot slot, string sidecarPath, string saveDigest) =>
            File.Exists(sidecarPath)
            && TryParseSidecar(slot, File.ReadAllLines(sidecarPath, SidecarEncoding), out _, out string recorded)
            && recorded == saveDigest;

        // Lower-case hex SHA-256 of a file's bytes (the S0DemoFixture/S0TextFormatter idiom). Off the
        // hot path: once per write, and at open only for a slot an interrupted write left behind.
        private static string DigestOf(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        // Puts the previous pair back after the new sidecar could not be swapped in. The save comes back
        // from its backup; the sidecar comes back from its backup only if the failed replace left it
        // under that name alone (File.Replace can fail after renaming the replaced file). The sidecar
        // swap's own failure is the one reported; if a restore fails too, its backup stays on disk for
        // recovery at the next open rather than being deleted.
        private static void RestorePreviousPair(
            bool hadSave, string saveBackup, string savePath, string sidecarBackup, string sidecarPath)
        {
            try
            {
                if (hadSave)
                {
                    SwapIn(saveBackup, savePath, null);
                }
                else
                {
                    DeleteIfPresent(savePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                MoveBackIfMissing(sidecarPath, sidecarBackup);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // Flushed to disk before it is swapped in, as SeasonSaveManager flushes the save (§4.6.1.1): a
        // sidecar renamed into place must not come back empty after a power loss.
        private static void WriteDurably(string path, string text)
        {
            byte[] bytes = SidecarEncoding.GetBytes(text);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void TryDeleteIfPresent(string path)
        {
            try
            {
                DeleteIfPresent(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void RequireSlot(CareerSaveSlot slot)
        {
            if ((int)slot < 0 || (int)slot >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Not a career save slot.");
            }
        }

        private static string FormatSidecar(in CareerSlotSummary s, string saveDigest)
        {
            var b = new StringBuilder();
            b.Append("format=").Append(ClientAppConstants.CAREER_SLOT_META_FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("sequence=").Append(s.Sequence.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("seasonSaveFormat=").Append(s.SeasonSaveFormatVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("worldSeed=").Append(s.WorldSeed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("managedClubId=").Append(s.ManagedClubId.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("seasonNumber=").Append(s.SeasonNumber.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("worldDay=").Append(s.WorldDay.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("roundsPlayed=").Append(s.RoundsPlayed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("roundCount=").Append(s.RoundCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("leaguePosition=").Append(s.LeaguePosition.ToString(CultureInfo.InvariantCulture)).Append('\n');
            b.Append("nestedFormats=").Append(NestedFormatStamp).Append('\n');
            b.Append("saveDigest=").Append(saveDigest).Append('\n');
            // Last, so a name containing '=' is still read whole (the parser splits on the FIRST '=').
            b.Append("managedClubName=").Append(s.ManagedClubName.Replace('\n', ' ').Replace('\r', ' ')).Append('\n');
            return b.ToString();
        }

        // Describe's read: an unreadable sidecar is reported like an invalid one (the slot reads Damaged).
        private static bool TryReadSidecar(CareerSaveSlot slot, string path, out CareerSlotSummary summary)
        {
            summary = default;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, SidecarEncoding);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return TryParseSidecar(slot, lines, out summary, out _);
        }

        private static bool TryParseSidecar(CareerSaveSlot slot, string[] lines, out CareerSlotSummary summary, out string saveDigest)
        {
            summary = default;
            saveDigest = null;
            long format = -1, sequence = -1, seasonSaveFormat = -1, managedClubId = -1, seasonNumber = -1;
            long worldDay = -1, roundsPlayed = -1, roundCount = -1, leaguePosition = -1;
            ulong worldSeed = 0;
            bool hasSeed = false;
            string name = null;
            string nestedFormats = null;
            string digest = null;

            foreach (string line in lines)
            {
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).TrimStart('﻿');
                string value = line.Substring(eq + 1);
                switch (key)
                {
                    case "format": format = ParseLong(value); break;
                    case "sequence": sequence = ParseLong(value); break;
                    case "seasonSaveFormat": seasonSaveFormat = ParseLong(value); break;
                    case "worldSeed": hasSeed = ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out worldSeed); break;
                    case "managedClubId": managedClubId = ParseLong(value); break;
                    case "seasonNumber": seasonNumber = ParseLong(value); break;
                    case "worldDay": worldDay = ParseLong(value); break;
                    case "roundsPlayed": roundsPlayed = ParseLong(value); break;
                    case "roundCount": roundCount = ParseLong(value); break;
                    case "leaguePosition": leaguePosition = ParseLong(value); break;
                    case "nestedFormats": nestedFormats = value; break;
                    case "saveDigest": digest = value; break;
                    case "managedClubName": name = value; break;
                }
            }

            if (format < 1 || sequence < 1 || seasonSaveFormat < 0 || seasonSaveFormat > uint.MaxValue || !hasSeed
                || managedClubId < 0 || managedClubId > int.MaxValue || seasonNumber < 0 || seasonNumber > int.MaxValue
                || worldDay < 0 || worldDay > uint.MaxValue || roundsPlayed < 0 || roundsPlayed > int.MaxValue
                || roundCount < 0 || roundCount > int.MaxValue || leaguePosition < 1 || leaguePosition > int.MaxValue
                || name == null)
            {
                return false;
            }

            // A sidecar of this layout always records the stamp and the digest; one of an older layout
            // never did, and is Incompatible on its format alone.
            bool currentLayout = format == ClientAppConstants.CAREER_SLOT_META_FORMAT_VERSION;
            if (currentLayout && (nestedFormats == null || digest == null))
            {
                return false;
            }

            bool compatible = currentLayout
                && seasonSaveFormat == SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION
                && nestedFormats == NestedFormatStamp;

            summary = new CareerSlotSummary(
                slot,
                compatible ? CareerSlotState.Ready : CareerSlotState.Incompatible,
                sequence,
                (uint)seasonSaveFormat,
                worldSeed,
                (int)managedClubId,
                name,
                (int)seasonNumber,
                (uint)worldDay,
                (int)roundsPlayed,
                (int)roundCount,
                (int)leaguePosition);
            saveDigest = digest;
            return true;
        }

        private static long ParseLong(string value) =>
            long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long parsed) ? parsed : -1;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): four-slot store, sidecar summaries,  |
// |         |            |        | sequence-based latest, crash-safe swap, D-10 Incompatible.     |
// | 1.1     | 2026-10-19 | —      | Self-review: Write deletes both temp files on any failure.     |
// | 1.2     | 2026-10-27 | —      | PR 519 review: Write prepares both temps before touching a     |
// |         |            |        | destination and restores the backed-up save if the sidecar     |
// |         |            |        | swap fails (the pair never splits); the sidecar records the    |
// |         |            |        | nested sub-blob format stamp (layout v2) so an inner bump is   |
// |         |            |        | Incompatible (D-10); MarkDamaged invalidates an unloadable slot. |
// | 1.3     | 2026-10-27 | —      | PR 519 review 2: the sidecar records the save's SHA-256, and   |
// |         |            |        | opening the store recovers a write interrupted between its two |
// |         |            |        | swaps (staged sidecar swapped in, or the backup put back).     |
// | 1.4     | 2026-10-27 | —      | PR 519 review 3: recovery's sidecar reads propagate IO errors, |
// |         |            |        | so a locked staged sidecar aborts recovery instead of being    |
// |         |            |        | deleted; parsing split out of TryReadSidecar (TryParseSidecar). |
// | 1.5     | 2026-10-27 | —      | PR 519 review 4: MarkDamaged sets the sidecar aside (.damaged) |
// |         |            |        | and RestoreDamaged puts it back while its digest matches;      |
// |         |            |        | NextSequence counts set-aside sidecars; the sidecar swap keeps |
// |         |            |        | a backup, restored on failure and by recovery; the sidecar is  |
// |         |            |        | flushed to disk before it is swapped in.                       |
// | 1.6     | 2026-10-29 | —      | PR 522 review: recovery never revives a set-aside slot from a  |
// |         |            |        | stale backup (a committed write keeps its pair, set aside or   |
// |         |            |        | not), puts a save left only as its backup back, and drops a    |
// |         |            |        | sidecar backup that pairs with nothing; finishing a write      |
// |         |            |        | clears .damaged; post-commit cleanup is best effort; one       |
// |         |            |        | SwapIn(temp, dest, backup) and MoveBackIfMissing helper;       |
// |         |            |        | RestoreDamaged documented as groundwork (no caller yet).       |
#endregion
