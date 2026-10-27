// File:     src/client-app/CareerSaveSlots.cs
// Created:  2026-10-18
// Modified: 2026-10-27
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
    /// <b>Crash safety.</b> Both files are written in full to temporary siblings before either
    /// destination is touched; the save is then swapped in with <c>File.Replace</c> (keeping the
    /// previous save as a backup) and the sidecar after it. A failure at any step therefore leaves the
    /// previous save + sidecar pair in place: a failed sidecar swap puts the backed-up save back. Only
    /// a process death between the two swaps leaves a new save under the previous sidecar — still a
    /// loadable save, reported with the facts of the one it replaced.
    /// </para>
    /// <para>
    /// <b>D-10.</b> Saves are breakable through PM-3, so a slot whose sidecar records another
    /// <c>SEASON_SAVE_FORMAT_VERSION</c>, or another version of any sub-blob nested inside it (KD-2:
    /// each is gated independently, so an inner bump leaves the outer version alone), is
    /// <see cref="CareerSlotState.Incompatible"/>: listed, never loaded, never picked by Continue, and
    /// freely overwritten. A slot whose save fails to load anyway is invalidated by
    /// <see cref="MarkDamaged"/>, so it cannot keep winning Continue.
    /// </para>
    /// Not thread-safe; the client drives one store from one thread.
    /// </summary>
    public sealed class CareerSaveSlots
    {
        private const string SAVE_EXTENSION = ".tdsave";
        private const string SIDECAR_EXTENSION = ".slot";
        private const string TEMP_SUFFIX = ".tmp";
        private const string BACKUP_SUFFIX = ".bak";

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

        /// <summary>Opens (and creates if needed) the slot store rooted at <paramref name="directory"/>.</summary>
        /// <exception cref="ArgumentException"><paramref name="directory"/> is null or empty.</exception>
        public CareerSaveSlots(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The save directory must be non-empty.", nameof(directory));
            }

            _directory = directory;
            Directory.CreateDirectory(_directory);
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
            try
            {
                // Everything that can fail on content is done before a destination is touched.
                SeasonSaveManager.Save(loop, null, saveTemp);
                File.WriteAllText(sidecarTemp, FormatSidecar(in summary), SidecarEncoding);

                bool hadSave = File.Exists(savePath);
                if (hadSave)
                {
                    // A backup left by an interrupted earlier write belongs to a pair already replaced.
                    DeleteIfPresent(saveBackup);
                    File.Replace(saveTemp, savePath, saveBackup);
                }
                else
                {
                    File.Move(saveTemp, savePath);
                }

                try
                {
                    SwapIn(sidecarTemp, sidecarPath);
                }
                catch
                {
                    RestorePreviousSave(hadSave, saveBackup, savePath);
                    throw;
                }

                DeleteIfPresent(saveBackup);
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
        /// <see cref="CareerSlotState.Damaged"/> and Continue moves on to the next loadable slot. The
        /// save bytes are kept (only the sidecar is removed); the slot is freely overwritten. Best
        /// effort: false when the sidecar could not be removed.
        /// </summary>
        public bool MarkDamaged(CareerSaveSlot slot)
        {
            RequireSlot(slot);
            try
            {
                DeleteIfPresent(SidecarPath(slot));
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
                string sidecarPath = SidecarPath(slot);
                if (File.Exists(sidecarPath) && TryReadSidecar(slot, sidecarPath, out CareerSlotSummary row) && row.Sequence > max)
                {
                    max = row.Sequence;
                }
            }

            return max + 1;
        }

        private static void SwapIn(string tempPath, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Replace(tempPath, destinationPath, null);
            }
            else
            {
                File.Move(tempPath, destinationPath);
            }
        }

        // Puts the previous save back after its sidecar could not be swapped in, so the slot stays a
        // matching pair. The sidecar's own failure is the one reported; if this restore fails too, the
        // backup stays on disk rather than being deleted.
        private static void RestorePreviousSave(bool hadSave, string saveBackup, string savePath)
        {
            try
            {
                if (hadSave)
                {
                    File.Replace(saveBackup, savePath, null);
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
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void RequireSlot(CareerSaveSlot slot)
        {
            if ((int)slot < 0 || (int)slot >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Not a career save slot.");
            }
        }

        private static string FormatSidecar(in CareerSlotSummary s)
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
            // Last, so a name containing '=' is still read whole (the parser splits on the FIRST '=').
            b.Append("managedClubName=").Append(s.ManagedClubName.Replace('\n', ' ').Replace('\r', ' ')).Append('\n');
            return b.ToString();
        }

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

            long format = -1, sequence = -1, seasonSaveFormat = -1, managedClubId = -1, seasonNumber = -1;
            long worldDay = -1, roundsPlayed = -1, roundCount = -1, leaguePosition = -1;
            ulong worldSeed = 0;
            bool hasSeed = false;
            string name = null;
            string nestedFormats = null;

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

            // A sidecar of this layout always records the stamp; one of an older layout never did, and is
            // Incompatible on its format alone.
            bool currentLayout = format == ClientAppConstants.CAREER_SLOT_META_FORMAT_VERSION;
            if (currentLayout && nestedFormats == null)
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
#endregion
