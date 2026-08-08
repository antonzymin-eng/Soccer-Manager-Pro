// File:     src/season-save/SeasonSaveManager.cs
// Created:  2026-07-22
// Modified: 2026-08-08 (AR pass 6: the KD-4 invariant at Save + the bidirectional cursor gate — v1.15)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / KD-1 / KD-5..KD-8;
//           Training System #29 §4.4 / FR-TR-018/019; Injuries & Medical #41 §4.4 / FR-MD-017/018;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §4.6.1.1
//           (atomic-write contract); Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  The season save-file root — writes a season (the living-world WorldStore composite, the
//           season state, the #29 per-club training states, the #41 per-club medical states, and the
//           #30 per-club appearance records, plus
//           an optional in-progress MatchEngine) to disk as one file and reconstructs all of them. This
//           is the only assembly that may reference both match-engine and living-world (FR-LW-003 keeps
//           them independent; the season root sits above both, like match-viewer over match-engine).
//           Save captures every sub-blob, encodes the season frame (SeasonSaveCodec), and writes
//           atomically (temp -> fsync -> rename). Load reads the file, deframes it, and rebuilds the
//           WorldStore, the season state, and the training/medical states (always) plus the MatchEngine
//           (only when the save carried a match).

using System;
using System.IO;

using Unity.Profiling;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// On-disk save/load for a season: one file carrying the living-world <see cref="WorldStore"/>
    /// composite, the <see cref="SeasonState"/>, the #29 per-club training states, the #41 per-club
    /// medical states, and the #30 per-club appearance records — all five always present — and, when a match is in progress, a running
    /// <see cref="MatchEngine.MatchEngine"/> (unified-season-save-design.md). These are nested as
    /// opaque, independently version-gated sub-blobs (KD-2) — this root never parses any of them, it
    /// only frames/deframes and reconstructs.
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
        /// Captures <paramref name="world"/>, <paramref name="season"/>, <paramref name="trainingClubs"/>,
        /// <paramref name="medicalClubs"/> and (when present) <paramref name="matchOrNull"/>, encodes
        /// the season frame, and writes it to <paramref name="path"/> atomically (the §4.6.1.1 temp -> fsync -> rename
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
        /// <param name="trainingClubs">The per-club #29 training states. REQUIRED, and never null: pass
        /// <c>Array.Empty&lt;ClubTrainingStates&gt;()</c> to say "this season tracks no training state",
        /// which still writes a well-formed zero-club block rather than omitting one (FR-TR-018).</param>
        /// <param name="medicalClubs">The per-club #41 medical states, on the same terms
        /// (FR-MD-017).</param>
        /// <param name="appearanceClubs">The per-club #30 appearance states, on the same terms —
        /// REQUIRED, never null-meaning-empty (#30 Appendix B / ERR-041-010(b)).</param>
        public static void Save(
            WorldStore world,
            SeasonState season,
            MatchEngine.MatchEngine matchOrNull,
            string path,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs)
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
            // These two are REQUIRED and reject null rather than defaulting to the empty set. A default
            // of "null ⇒ empty" reads as a convenience, but what it actually means is that at T2 —
            // when these arrays finally hold a season's worth of conditioning, focus and injury
            // history — a call site that simply omits them still compiles, still saves, and still
            // loads, returning empty arrays that are indistinguishable from an unwired game. Nothing
            // throws and no assertion can fire: the state is just gone. The `season` parameter is
            // required for exactly this reason (FR-SN-019); these are no more optional than it is.
            // "This season tracks no training state" is a thing a caller says with Array.Empty, not a
            // thing it says by staying silent.
            if (trainingClubs == null)
            {
                throw new ArgumentNullException(nameof(trainingClubs),
                    "Pass Array.Empty<ClubTrainingStates>() for a season that tracks no training " +
                    "state — null is not the empty set (FR-TR-018).");
            }
            if (medicalClubs == null)
            {
                throw new ArgumentNullException(nameof(medicalClubs),
                    "Pass Array.Empty<ClubInjuryStates>() for a season that tracks no medical state " +
                    "— null is not the empty set (FR-MD-017).");
            }
            if (appearanceClubs == null)
            {
                throw new ArgumentNullException(nameof(appearanceClubs),
                    "Pass Array.Empty<ClubAppearanceStates>() for a season that tracks no appearance " +
                    "record — null is not the empty set (#30 Appendix B).");
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Save path must be non-empty.", nameof(path));
            }

            RequireCoherentCareerBlocks(trainingClubs, medicalClubs, appearanceClubs);
            RequireCareerCursorsWithinClock(
                world.CurrentWorldTick, trainingClubs, medicalClubs, appearanceClubs);

            // The FIRST cross-blob rule, symmetric at last (AR pass 6 M1): the KD-4 calendar-cursor
            // invariant was Load-only, three lines above a gate whose own doc states the
            // never-write-what-Load-refuses rule — so a caller who advanced the world past the
            // pending round got a green save of a career that could never be loaded again.
            if (!season.Calendar.SatisfiesCursorInvariant(world.CurrentWorldTick))
            {
                throw new InvalidOperationException(
                    "Season save would be incoherent: the next fixture day (" +
                    season.Calendar.NextFixtureDay() + ") is before the world day (" +
                    world.CurrentWorldTick + ") — the KD-4 cursor invariant (FR-SN-011); Load " +
                    "refuses this file, so Save must not write it.");
            }

            using var _ = s_saveMarker.Auto();

            byte[] worldBlob = world.Snapshot();
            byte[] seasonBlob = SeasonStateCodec.Encode(season);
            var trainingBlock = new TrainingBlock(TrainingSaveCodec.Encode(trainingClubs));
            var medicalBlock = new MedicalBlock(MedicalSaveCodec.Encode(medicalClubs));
            var appearanceBlock = new AppearanceBlock(AppearanceSaveCodec.Encode(appearanceClubs));
            byte[] matchBlob = matchOrNull != null ? MatchSaveManager.Encode(matchOrNull) : null;
            byte[] blob = SeasonSaveCodec.Encode(
                worldBlob, seasonBlob, in trainingBlock, in medicalBlock, in appearanceBlock, matchBlob);

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
        /// Saves a whole season through its loop — the form an external caller uses, because
        /// <see cref="PlayerCareerStates"/>'s block accessors are <c>internal</c> (they hand out the
        /// live state arrays, and a public accessor would make every holder of
        /// <see cref="SeasonLoop.Career"/> a second writer of #29/#41 state, defeating the FR-TR-004 /
        /// FR-TR-023 single-writer contract).
        /// <para>
        /// A loop with no career wired writes the two well-formed zero-club blocks a pre-T2 save
        /// carries — the same bytes <c>Array.Empty</c> produces through the long form.
        /// </para>
        /// </summary>
        /// <param name="loop">The season loop to capture: its world, its season state, and its career.</param>
        /// <param name="matchOrNull">The in-progress match, or null when there is none (KD-3).
        /// <b>Must be quiescent</b> — not being ticked while this call runs. Both this class and
        /// <see cref="SeasonLoop"/> declare no thread safety, and <see cref="MatchSaveManager.Encode"/>
        /// walks the whole engine field by field: encoding one mid-<c>RunTick</c> yields a blob mixing
        /// pre- and post-tick state, which restores cleanly into a world the simulation never occupied
        /// and diverges from there with every gate green.
        /// <para>
        /// <see cref="SeasonLoop.ActiveMatch"/> is <b>not</b> a safe source for this today: it is
        /// non-null only inside the synchronous <see cref="SeasonLoop.AdvanceAndPlayNextRound"/> call
        /// that is ticking it, so reading it at all means reading from another thread. A supported
        /// mid-match save needs a seam that can stop the match between ticks first; until one exists,
        /// pass an engine this caller owns and is not currently advancing.
        /// </para></param>
        /// <param name="path">The destination file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="loop"/> is null.</exception>
        public static void Save(SeasonLoop loop, MatchEngine.MatchEngine matchOrNull, string path)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            Save(
                loop.World,
                loop.State,
                matchOrNull,
                path,
                loop.Career != null
                    ? loop.Career.TrainingBlocks()
                    : Array.Empty<ClubTrainingStates>(),
                loop.Career != null
                    ? loop.Career.MedicalBlocks()
                    : Array.Empty<ClubInjuryStates>(),
                loop.Career != null
                    ? loop.Career.AppearanceBlocks()
                    : Array.Empty<ClubAppearanceStates>());
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
        /// here (the KD-4 cursor invariant, FR-SN-011 / F4 — one of the TWO cross-blob coherence rules
        /// this root owns, beside the career cursor-vs-clock rule, both enforced at Save and Load); and a distinct-squad match save
        /// loaded without (or with an incomplete) <paramref name="squads"/> throws from the match restore
        /// factory (KD-6 / R4). The match restore's fingerprint + MXCSR float-mode gates run here
        /// unchanged (KD-5).
        /// <para>
        /// <b>A save carrying a match additionally cross-checks the three career blocks</b>
        /// (<see cref="PlayerCareerStates.FromBlocks"/>), because the availability filter has to be
        /// rebuilt from them — see the match branch below. So a file whose training, medical and
        /// appearance blocks describe different squads is refused here rather than restoring a match
        /// against a career nothing else would have validated. A save with no match is untouched by this.
        /// </para>
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
            ClubAppearanceStates[] appearanceClubs = AppearanceSaveCodec.Decode(blobs.AppearanceBlob);

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

            // The second cross-blob rule (AR pass 5 M4): see RequireCareerCursorsWithinClock. Enforced
            // on load as well as save because a hand-edited or mispaired file arrives HERE.
            RequireCareerCursorsWithinClock(
                world.CurrentWorldTick, trainingClubs, medicalClubs, appearanceClubs);

            // The in-progress match was configured with the AVAILABILITY-FILTERED squad (#41 FR-MD-023,
            // #29/#41 T2), and the snapshot records only each team's ClubId — it cannot record "which
            // eighteen of the twenty-five". So restoring through the raw provider hands
            // ReprojectDistinctSquads the FULL roster, it re-runs LineupSelector over a different
            // candidate set, and the restored eleven is not the eleven that took the pitch: different
            // canonical attribute records on every slot, every gate green (the ClubId matches, the size
            // check passes), and the match then diverges from the pre-save run with nothing to announce
            // it. Re-applying the same filter — from the medical state carried in THIS file, which is
            // the state the match was configured against — reproduces the exact squad, so selection
            // lands on the same eleven.
            //
            // Pass-through for a club the career does not carry, which is every club of every save
            // written before T2 (both blocks empty): the decorator is then the identity and the restore
            // is bit-for-bit what it was.
            MatchEngine.MatchEngine match = null;
            if (blobs.MatchBlob != null)
            {
                // The dial position is irrelevant to this throwaway career — it only answers the
                // availability read; no day step ever runs on it — but it is constructed at the
                // production posture (armed, FR-MD-027) rather than encoding a stale default.
                var career = PlayerCareerStates.FromBlocks(
                    trainingClubs, medicalClubs, appearanceClubs, injuryOccurrenceEnabled: true);
                ISquadProvider asConfigured = squads == null
                    ? null
                    : new AvailabilityFilteredSquads(squads, career);
                match = MatchSaveManager.Restore(blobs.MatchBlob, asConfigured);
            }

            return new SeasonSaveContents(
                world, season, trainingClubs, medicalClubs, appearanceClubs, match);
        }

        /// <summary>
        /// An <see cref="ISquadProvider"/> that applies the #41 availability filter on the way out, so a
        /// restore re-selects from the same squad the match was configured with. Load-time only; never
        /// persisted (the <c>squads</c> / <c>canon</c> precedent).
        /// <para>
        /// It only reads the career, so the throwaway instance <see cref="Load"/> builds for this
        /// cannot disturb the blocks handed back in <see cref="SeasonSaveContents"/> — and would not
        /// reach them in any case, since <see cref="PlayerCareerStates.FromBlocks"/> copies the state
        /// arrays rather than borrowing them.
        /// </para>
        /// <para>
        /// A roster that has drifted from the save — a squad player the save's career carries no state
        /// for — surfaces as the filter's own fail-loud rather than being waved through. That is the
        /// right answer: the raw squad would restore a different eleven, silently, which is the whole
        /// defect this decorator exists to close.
        /// </para>
        /// </summary>
        private sealed class AvailabilityFilteredSquads : ISquadProvider
        {
            private readonly ISquadProvider _inner;
            private readonly PlayerCareerStates _career;

            internal AvailabilityFilteredSquads(ISquadProvider inner, PlayerCareerStates career)
            {
                _inner = inner;
                _career = career;
            }

            /// <inheritdoc />
            public Squad ResolveByClubId(int clubId)
            {
                Squad squad = _inner.ResolveByClubId(clubId);
                if (squad == null || !_career.CarriesClub(clubId))
                {
                    // Null is the provider's own "unknown club" answer and the restore factory's
                    // fail-loud input — do not turn it into an exception here, and do not filter a club
                    // this save carries no medical state for.
                    return squad;
                }

                return _career.SelectAvailable(squad);
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (Exception) { }
        }

        /// <summary>
        /// The three career block sets must describe the SAME squads before they are written (AR
        /// pass 3): without this, <c>Save</c> could write a file its own documented restore path —
        /// <see cref="TacticalDirector.SeasonSave.PlayerCareerStates.FromBlocks"/> — refuses on its
        /// coherence gate, the "Encode writes what Decode refuses" defect class the T1 AR filed
        /// against <c>TrainingSaveCodec</c>. Order-insensitive on clubs (the codecs canonicalize at
        /// encode), so it compares the sets by ascending ClubId, then each club's player ids
        /// pairwise.
        /// </summary>
        private static void RequireCoherentCareerBlocks(
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs)
        {
            if (trainingClubs.Length != medicalClubs.Length
                || trainingClubs.Length != appearanceClubs.Length)
            {
                throw new ArgumentException(
                    $"The training set carries {trainingClubs.Length} clubs, the medical set "
                    + $"{medicalClubs.Length} and the appearance set {appearanceClubs.Length}; the "
                    + "three describe one career and must agree — this file would be refused by "
                    + "PlayerCareerStates.FromBlocks on load.",
                    nameof(medicalClubs));
            }

            var t = (ClubTrainingStates[])trainingClubs.Clone();
            var m = (ClubInjuryStates[])medicalClubs.Clone();
            var a = (ClubAppearanceStates[])appearanceClubs.Clone();
            Array.Sort(t, (x, y) => x.ClubId.CompareTo(y.ClubId));
            Array.Sort(m, (x, y) => x.ClubId.CompareTo(y.ClubId));
            Array.Sort(a, (x, y) => x.ClubId.CompareTo(y.ClubId));

            for (int c = 0; c < t.Length; c++)
            {
                // Duplicate club ids pair arbitrarily across the three unstably-sorted sets, so the
                // gate's own diagnostics would name the wrong mismatch; refused by name here rather
                // than left to the codecs downstream (AR pass 6 L5).
                if (c > 0 && t[c].ClubId == t[c - 1].ClubId)
                {
                    throw new ArgumentException(
                        $"The training set carries club {t[c].ClubId} twice — a club has exactly one "
                        + "career block.",
                        nameof(trainingClubs));
                }

                // A default-valued block NREs at the clones below; refuse it by name instead (AR
                // pass 4 L1 — before this gate existed, the codecs produced the diagnosed refusal).
                if (t[c].PlayerIds == null || t[c].States == null
                    || m[c].PlayerIds == null || m[c].States == null
                    || a[c].PlayerIds == null || a[c].States == null)
                {
                    // The paramName names the OFFENDING set (AR pass 5 L10 — a defaulted training
                    // block used to be reported as 'medicalClubs').
                    string offender = t[c].PlayerIds == null || t[c].States == null
                        ? nameof(trainingClubs)
                        : m[c].PlayerIds == null || m[c].States == null
                            ? nameof(medicalClubs)
                            : nameof(appearanceClubs);
                    throw new ArgumentException(
                        $"Career block {c} is a default value, not a constructed one — a save cannot "
                        + "carry blocks that were never populated.",
                        offender);
                }

                if (t[c].ClubId != m[c].ClubId || t[c].ClubId != a[c].ClubId)
                {
                    throw new ArgumentException(
                        $"The three career block sets disagree on the club set (training club "
                        + $"{t[c].ClubId}, medical {m[c].ClubId}, appearance {a[c].ClubId} at "
                        + $"ascending position {c}).",
                        t[c].ClubId != m[c].ClubId ? nameof(medicalClubs) : nameof(appearanceClubs));
                }

                if (t[c].Count != m[c].Count || t[c].Count != a[c].Count)
                {
                    throw new ArgumentException(
                        $"Club {t[c].ClubId}: the training set carries {t[c].Count} players, the "
                        + $"medical set {m[c].Count}, the appearance set {a[c].Count}.",
                        t[c].Count != m[c].Count ? nameof(medicalClubs) : nameof(appearanceClubs));
                }

                // Order-insensitive within the club too: the codecs canonicalize per-club order at
                // Encode (each block's states travel WITH its own ids), so a caller may legitimately
                // hand the three sets in different member orders — only the member SETS must agree.
                var tIds = (int[])t[c].PlayerIds.Clone();
                var mIds = (int[])m[c].PlayerIds.Clone();
                var aIds = (int[])a[c].PlayerIds.Clone();
                Array.Sort(tIds);
                Array.Sort(mIds);
                Array.Sort(aIds);
                for (int i = 0; i < tIds.Length; i++)
                {
                    if (tIds[i] != mIds[i] || tIds[i] != aIds[i])
                    {
                        throw new ArgumentException(
                            $"Club {t[c].ClubId}: the training set holds player {tIds[i]} where the "
                            + $"medical set holds {mIds[i]} and the appearance set {aIds[i]} "
                            + "(compared ascending) — the three sets describe different squads.",
                            tIds[i] != mIds[i] ? nameof(medicalClubs) : nameof(appearanceClubs));
                    }
                }
            }

            // The ERR-041-019 half of FromBlocks' refusal surface (AR pass 4 M1): without this,
            // Save still wrote a file the restore path refuses — a cross-club duplicate PlayerId —
            // one predicate short of the gate's own stated contract, missed within a commit of the
            // gate being written. Same walk, same message, one owner.
            var clubIds = new int[t.Length];
            var idSets = new int[t.Length][];
            for (int c = 0; c < t.Length; c++)
            {
                clubIds[c] = t[c].ClubId;
                idSets[c] = t[c].PlayerIds;
            }

            PlayerCareerStates.RequireGloballyUniquePlayerIds(clubIds, idSets, nameof(trainingClubs));
        }

        /// <summary>
        /// The SECOND cross-blob coherence rule this root owns (AR pass 5 M4, beside FR-SN-011's
        /// calendar cursor): no persisted per-player world-day cursor may sit AHEAD of the world
        /// clock. A future-dated appearance anchor makes <c>AppearanceWindow.AppearanceDaysOn</c>
        /// throw at slot 4 with the dial armed, and because the career day-steps run BEFORE
        /// <c>WorldStore.AdvanceDay</c> the clock can never catch up — the career is wedged
        /// permanently while saving and reloading cleanly. The sibling cursors fail the OTHER way
        /// (a future-dated #29/#41 cursor is a silent per-player no-op until the clock catches up).
        /// Three persisted cursors, two failure modes, one owner: the only layer holding the world
        /// blob and the career blocks together. Checked at Save too, so this root never writes a
        /// file its own Load refuses (the T1 AR's Encode/Decode-asymmetry rule).
        /// </summary>
        private static void RequireCareerCursorsWithinClock(
            uint worldTick,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs)
        {
            for (int c = 0; c < trainingClubs.Length; c++)
            {
                for (int i = 0; i < trainingClubs[c].Count; i++)
                {
                    uint day = trainingClubs[c].States[i].LastAdvancedWorldDay;
                    if (day != TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL
                        && (day > worldTick || worldTick > day + 1))
                    {
                        // Both directions (AR pass 6 M2): AHEAD means the F6 idempotency silently
                        // skips the day step until the clock catches up; LAGGING by >= 2 is WORSE —
                        // the F7 gap refusal fires on every later advance and, because the day steps
                        // run before the clock increment, the career wedges permanently while saving
                        // cleanly. A legitimate save sits at worldTick - day in {0, 1} (or the
                        // sentinel); the appearance anchor below has no gap contract, so it is
                        // ahead-checked only.
                        throw new InvalidOperationException(
                            $"Career save is incoherent: club {trainingClubs[c].ClubId} player "
                            + $"{trainingClubs[c].PlayerIds[i]}'s training cursor ({day}) is "
                            + (day > worldTick ? "ahead of" : "more than one day behind")
                            + $" the world clock ({worldTick}) — a mispaired or hand-edited save.");
                    }
                }
            }

            for (int c = 0; c < medicalClubs.Length; c++)
            {
                for (int i = 0; i < medicalClubs[c].Count; i++)
                {
                    uint day = medicalClubs[c].States[i].LastAdvancedWorldDay;
                    if (day != InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL
                        && (day > worldTick || worldTick > day + 1))
                    {
                        throw new InvalidOperationException(
                            $"Career save is incoherent: club {medicalClubs[c].ClubId} player "
                            + $"{medicalClubs[c].PlayerIds[i]}'s medical cursor ({day}) is "
                            + (day > worldTick ? "ahead of" : "more than one day behind")
                            + $" the world clock ({worldTick}).");
                    }
                }
            }

            for (int c = 0; c < appearanceClubs.Length; c++)
            {
                for (int i = 0; i < appearanceClubs[c].Count; i++)
                {
                    uint day = appearanceClubs[c].States[i].BitsAsOfWorldDay;
                    if (day > worldTick)
                    {
                        throw new InvalidOperationException(
                            $"Career save is incoherent: club {appearanceClubs[c].ClubId} player "
                            + $"{appearanceClubs[c].PlayerIds[i]}'s appearance anchor ({day}) is ahead "
                            + $"of the world clock ({worldTick}) — with the occurrence dial armed, the "
                            + "slot-4 window read throws on every later day and the world clock can "
                            + "never advance again (the career wedges while saving cleanly).");
                    }
                }
            }
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
// | 1.5     | 2026-08-06 | —      | AR pass 1 (H): trainingClubs / medicalClubs become REQUIRED    |
// |         |            |        | and reject null. Defaulting them to the empty set meant a T2  |
// |         |            |        | call site could omit a season's training and injury history   |
// |         |            |        | and still compile, save and load — silently, with nothing to  |
// |         |            |        | distinguish the loss from an unwired game.                    |
// | 1.6     | 2026-08-06 | —      | Doc-drift fix (no code change): the file-header Purpose block |
// |         |            |        | and the class <summary> still described this file as writing |
// |         |            |        | only the WorldStore composite plus an optional MatchEngine —  |
// |         |            |        | stale since the #30 T1 season-state landing and now missing   |
// |         |            |        | the #29/#41 training and medical states too. Both corrected.  |
// | 1.7     | 2026-08-06 | —      | AR pass 1 over T2 (H): Load now re-applies the #41            |
// |         |            |        | availability filter when restoring an in-progress match. The  |
// |         |            |        | match was configured with the FILTERED squad and the snapshot |
// |         |            |        | records only each team's ClubId, so restoring through the raw  |
// |         |            |        | provider re-ran LineupSelector over the full roster and put a  |
// |         |            |        | DIFFERENT eleven's attribute records on the pitch — silently,  |
// |         |            |        | every gate green. The filter is rebuilt from the medical state |
// |         |            |        | in the same file, so the squad is reproduced exactly; a club   |
// |         |            |        | the career does not carry (every club of every pre-T2 save)    |
// |         |            |        | passes through unchanged. Also (M): a Save(SeasonLoop, match,  |
// |         |            |        | path) overload, so external callers can save without           |
// |         |            |        | PlayerCareerStates' block accessors being public — those hand  |
// |         |            |        | out the live state arrays, and a public accessor makes every   |
// |         |            |        | holder of SeasonLoop.Career a second writer of #29/#41 state.  |
// | 1.8     | 2026-08-06 | —      | T2 AR pass 3 (2L). The Save(SeasonLoop, …) overload's match   |
// |         |            |        | parameter documented "Pass SeasonLoop.ActiveMatch when saving |
// |         |            |        | mid-match" — but ActiveMatch is non-null only INSIDE the       |
// |         |            |        | synchronous AdvanceAndPlayNextRound that is ticking it, and   |
// |         |            |        | both types declare no thread safety, so the documented use    |
// |         |            |        | was a cross-thread walk of a live engine yielding a torn blob |
// |         |            |        | that restores cleanly into a state the sim never occupied.    |
// |         |            |        | Now states the quiescence precondition and withdraws the      |
// |         |            |        | suggestion. Also: the version rows below 1.5 were 1.7 then    |
// |         |            |        | 1.6, so reading the table bottom-up gave 1.6 as current, and  |
// |         |            |        | the header carried two Modified lines against the convention  |
// |         |            |        | of one matching the latest row. Both corrected.               |
// | 1.9     | 2026-08-06 | —      | T2 AR pass 6 (L, doc only): AvailabilityFilteredSquads        |
// |         |            |        | justified its safety by "can safely share arrays with the     |
// |         |            |        | blocks it hands back", which stopped being true at pass 3 —   |
// |         |            |        | FromBlocks copies the state arrays now. The conclusion holds  |
// |         |            |        | (the decorator only reads); the stated REASON would have told |
// |         |            |        | a reader that FromBlocks still borrows.                       |
// | 1.10    | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): Save/Load carry the third   |
// |         |            |        | (appearance) block on the same REQUIRED-never-null terms as   |
// |         |            |        | its siblings; the match-restore career is rebuilt from all    |
// |         |            |        | three; SeasonSaveContents gains AppearanceClubs.              |
// | 1.11    | 2026-08-07 | —      | Balance pass D4: Load's throwaway filter career is constructed |
// |         |            |        | at the armed production posture (the dial argument is now     |
// |         |            |        | required; irrelevant to a read-only filter, but a literal     |
// |         |            |        | false would encode a stale default).                          |
// | 1.12    | 2026-08-08 | —      | Balance-pass AR pass 3 (L7): Save gates the career block       |
// |         |            |        | triple's coherence (club sets + per-club player ids agree),   |
// |         |            |        | because it could write a file its own documented restore path |
// |         |            |        | (PlayerCareerStates.FromBlocks) refuses — the Encode-writes-  |
// |         |            |        | what-Decode-refuses class the T1 AR filed against the codec.  |
// |         |            |        | Also deletes v1.11's orphaned header fragment (AR pass 2).    |
// | 1.13    | 2026-08-08 | —      | Balance-pass AR pass 4 (M1 + L1): the gate gains the           |
// |         |            |        | ERR-041-019 half of FromBlocks' refusal surface (a cross-club |
// |         |            |        | duplicate id still saved cleanly — the gate's own defect      |
// |         |            |        | class, one predicate short, missed within a commit of the     |
// |         |            |        | gate being written), and refuses a default block by name      |
// |         |            |        | instead of NullReferenceException-ing at the clone.           |
// | 1.14    | 2026-08-08 | —      | Balance-pass AR pass 5 (M4 + L10): the cursor-vs-clock gate —  |
// |         |            |        | the SECOND cross-blob rule this root owns; a future-dated     |
// |         |            |        | appearance anchor wedges the career permanently at slot 4     |
// |         |            |        | with the dial armed (demonstrated), the sibling cursors       |
// |         |            |        | freeze a player silently — refused at Save AND Load. The      |
// |         |            |        | coherence gate's paramName now names the OFFENDING set.       |
// | 1.15    | 2026-08-08 | —      | Balance-pass AR pass 6 (M1 + M2 + L3 + L5): the KD-4 calendar  |
// |         |            |        | invariant is checked at Save too (it was Load-only, three     |
// |         |            |        | lines above a gate whose own doc states the never-write-what- |
// |         |            |        | Load-refuses rule); the cursor gate refuses LAGGING #29/#41   |
// |         |            |        | cursors (gap >= 2 wedges via F7 — demonstrated; ahead-only    |
// |         |            |        | before); duplicate ClubIds refused by name in the coherence   |
// |         |            |        | gate; header/summary docs mention the appearance block.       |
#endregion
