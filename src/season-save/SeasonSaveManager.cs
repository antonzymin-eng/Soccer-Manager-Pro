// File:     src/season-save/SeasonSaveManager.cs
// Created:  2026-07-22
// Modified: 2026-08-06 (#29/#41 T1: the training and medical sub-blobs are composed in)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / KD-1 / KD-5..KD-8;
//           Training System #29 §4.4 / FR-TR-018/019; Injuries & Medical #41 §4.4 / FR-MD-017/018;
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

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.TrainingSystem;

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
        /// Captures <paramref name="world"/>, <paramref name="season"/>, the #29 training and #41
        /// medical state, and (when present) <paramref name="matchOrNull"/>, encodes the season frame,
        /// and writes it to <paramref name="path"/> atomically (the §4.6.1.1 temp -> fsync -> rename
        /// contract). Every sub-blob is captured and the frame encoded BEFORE the file is opened (the
        /// <see cref="MatchSaveManager.Save"/> blob-before-file precedent, restated by FR-SN-021); no
        /// capture mutates its source, so a write failure leaves the live objects and any existing
        /// destination untouched (KD-8 / AR-2 L-1). Pass <c>null</c> for
        /// <paramref name="matchOrNull"/> when the season has no in-progress match (KD-3); the
        /// <paramref name="season"/> is never optional (FR-SN-019).
        /// </summary>
        /// <param name="world">The living-world store to capture. Never null.</param>
        /// <param name="season">The season state to capture. Never null (FR-SN-019).</param>
        /// <param name="matchOrNull">The in-progress match, or null when there is none (KD-3).</param>
        /// <param name="path">The destination file.</param>
        /// <param name="trainingClubs">The per-club #29 training states. <c>null</c> (the default) means
        /// the empty set, which still writes a well-formed zero-club block — it does NOT omit the block.
        /// That is the honest state today: nothing constructs #29 state until its T2 wiring, so every
        /// save written now carries an empty training block rather than a special case (FR-TR-018).</param>
        /// <param name="medicalClubs">The per-club #41 medical states, on the same terms
        /// (FR-MD-017).</param>
        public static void Save(
            WorldStore world,
            SeasonState season,
            MatchEngine.MatchEngine matchOrNull,
            string path,
            ClubTrainingStates[] trainingClubs = null,
            ClubInjuryStates[] medicalClubs = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world),
                    "A season save always carries a living-world store (KD-3).");
            }
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season),
                    "A season save always carries a season state (FR-SN-019).");
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Save path must be non-empty.", nameof(path));
            }

            using var _ = s_saveMarker.Auto();

            byte[] worldBlob = world.Snapshot();
            byte[] seasonBlob = SeasonStateCodec.Encode(season);
            byte[] trainingBlob = TrainingSaveCodec.Encode(
                trainingClubs ?? Array.Empty<ClubTrainingStates>());
            byte[] medicalBlob = MedicalSaveCodec.Encode(
                medicalClubs ?? Array.Empty<ClubInjuryStates>());
            byte[] matchBlob = matchOrNull != null ? MatchSaveManager.Encode(matchOrNull) : null;
            byte[] blob = SeasonSaveCodec.Encode(
                worldBlob, seasonBlob, trainingBlob, medicalBlob, matchBlob);

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
        /// living-world <see cref="WorldStore"/>, the <see cref="SeasonState"/> and the per-club #29
        /// training / #41 medical state (all always — the last two possibly empty) and the
        /// in-progress <see cref="MatchEngine.MatchEngine"/> (only when the save carried a match —
        /// otherwise <see cref="SeasonSaveContents.Match"/> is null, KD-3). Fail-loud: a missing /
        /// unreadable file surfaces the IO exception; a corrupt / version-mismatched / trailing-byte
        /// season frame throws from <see cref="SeasonSaveCodec.Decode"/>; a corrupt inner blob throws
        /// from <see cref="WorldStore.Restore"/> / <see cref="SeasonStateCodec.Decode"/> /
        /// <see cref="TrainingSaveCodec.Decode"/> / <see cref="MedicalSaveCodec.Decode"/> / the match
        /// restore path; a season whose next fixture day is already behind the restored world day throws
        /// here (the KD-4 cursor invariant, FR-SN-011 / F4 — the only cross-blob coherence rule, and this
        /// root is the only layer holding both blobs); and a distinct-squad match save
        /// loaded without (or with an incomplete) <paramref name="squads"/> throws from the match restore
        /// factory (KD-6 / R4). The match restore's fingerprint + MXCSR float-mode gates run here
        /// unchanged (KD-5).
        /// </summary>
        /// <param name="path">The season save file to read.</param>
        /// <param name="squads">The ClubId -> Squad resolver for a distinct-squad match save; ignored
        /// (may be null) for a neutral match or a no-match season (KD-6 / R4).</param>
        /// <param name="canon">The arc-trigger canon source re-attached to the restored world
        /// (arc-triggers-design §8.9(a)); a Load-time parameter, never persisted (the
        /// <paramref name="squads"/> precedent). Pass the same source the saved world was evaluating with
        /// so a flag-on world keeps spawning arcs after the season restore; <c>null</c> (the default)
        /// restores a flag-off world (arc evaluation stays skipped, its correct state).</param>
        public static SeasonSaveContents Load(string path, ISquadProvider squads = null, ArcCanonSource canon = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Load path must be non-empty.", nameof(path));
            }

            using var _ = s_loadMarker.Auto();

            byte[] blob = File.ReadAllBytes(path);
            SeasonSaveBlobs blobs = SeasonSaveCodec.Decode(blob);

            WorldStore world = WorldStore.Restore(blobs.WorldBlob, canon);
            SeasonState season = SeasonStateCodec.Decode(blobs.SeasonBlob);
            ClubTrainingStates[] trainingClubs = TrainingSaveCodec.Decode(blobs.TrainingBlob);
            ClubInjuryStates[] medicalClubs = MedicalSaveCodec.Decode(blobs.MedicalBlob);

            // FR-SN-011 (MUST) / F4: the KD-4 cursor invariant is the one coherence rule that spans the
            // world and season blobs, so it can only be checked HERE — the two codecs each see one blob,
            // and this root is the only layer that holds both. A season whose next fixture day has
            // already passed on the world clock would make T2's day-advance loop (FR-SN-010, which
            // advances UP TO the next fixture day) undefined, so the save is rejected at load rather
            // than surfacing later as a stuck or skipped round.
            if (!season.Calendar.SatisfiesCursorInvariant(world.CurrentWorldTick))
            {
                // NextFixtureDay() is safe here: the invariant is vacuously true for a completed season,
                // so reaching this branch means the cursor still points at a round.
                throw new InvalidOperationException(
                    "Season save is incoherent: the next fixture day (" +
                    season.Calendar.NextFixtureDay() + ") is before the restored world day (" +
                    world.CurrentWorldTick + ") — the KD-4 cursor invariant (FR-SN-011) is violated.");
            }

            MatchEngine.MatchEngine match = blobs.MatchBlob != null
                ? MatchSaveManager.Restore(blobs.MatchBlob, squads)
                : null;

            return new SeasonSaveContents(world, season, trainingClubs, medicalClubs, match);
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (Exception) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                       |
// | 1.1     | 2026-07-24 | —      | Arc-triggers E2 §8.9(a): Load gains an optional ArcCanonSource |
// |         |            |        | threaded into WorldStore.Restore (never persisted, the        |
// |         |            |        | ISquadProvider precedent) so a flag-on world keeps evaluating |
// |         |            |        | after a season restore.                                       |
// | 1.2     | 2026-07-25 | —      | #30 T1 (FR-SN-021): Save gains the season parameter and Load  |
// |         |            |        | reconstructs the SeasonState; the season blob is captured     |
// |         |            |        | before the file is opened, alongside the other two.           |
// | 1.3     | 2026-07-25 | —      | #30 T1 AR pass 2: Load enforces the KD-4 cursor invariant     |
// |         |            |        | (FR-SN-011 MUST / F4) — the one coherence rule spanning the   |
// |         |            |        | world and season blobs, so only this root can check it.       |
// | 1.4     | 2026-08-06 | —      | #29/#41 T1: Save gains the optional per-club training /       |
// |         |            |        | medical state (null ⇒ the empty set, still a written block)   |
// |         |            |        | and Load reconstructs both alongside the world and season.    |
#endregion
