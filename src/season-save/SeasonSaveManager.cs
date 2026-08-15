// File:     src/season-save/SeasonSaveManager.cs
// Created:  2026-07-22
// Modified: 2026-08-15, later still (reviewed findings pass, L1+L2 — L2: three sites (the Save summary,
//           the header Purpose's Load-rebuild sentence, the Load summary) still described a five/six-blob
//           frame after two prior additions (appearance, progression); pointed each at
//           SeasonSaveBlobs / SeasonSaveContents, which already enumerate the full eight-blob frame,
//           rather than restating the list a fourth time. L1: the Load summary's closing sentence
//           claimed a save with no match is "untouched" by the career-block coherence gate, but
//           RequireCoherentCareerBlocks (verified at its call site, which precedes and sits outside the
//           `if (blobs.MatchBlob != null)` branch) runs unconditionally — corrected to say every save is checked,
//           match or not. No behaviour change — v1.28)
// Modified: 2026-08-15, later (reviewed findings pass, L4 — DisciplineConstants.LEAGUE_COMPETITION_KEY
//           reference renamed for that constant's ALL_CAPS -> LeagueCompetitionKey rename
//           (DisciplineConstants.cs v1.5). No behaviour change — v1.27)
// Modified: 2026-08-15 (L3, reviewed-findings pass — formatting only: trailing whitespace removed and
//           a method-closing brace moved from column 0 to column 8, per FR-CS-011/012 — v1.26)
//           Prior: 2026-08-13 (#44 C1/C2 adversarial review round 4 — H4/ERR-030-039: `disciplineWired` is
//           promoted onto the PUBLIC long form and the forwarding overload that hardcoded `true` is
//           deleted, so no entry point can assert wiring on the caller's behalf — v1.25)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / KD-1 / KD-5..KD-8;
//           Training System #29 §4.4 / FR-TR-018/019; Injuries & Medical #41 §4.4 / FR-MD-017/018;
//           Discipline & Suspensions #44 Appendix B;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §4.6.1.1
//           (atomic-write contract); Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  The season save-file root — writes a season (the living-world WorldStore composite, the
//           season state, the #29 per-club training states, the #41 per-club medical states, the
//           #30 per-club appearance records, the #28 progression store and the #44 discipline state, plus
//           an optional in-progress MatchEngine) to disk as one file and reconstructs all of them. This
//           is the only assembly that may reference both match-engine and living-world (FR-LW-003 keeps
//           them independent; the season root sits above both, like match-viewer over match-engine).
//           Save captures every sub-blob, encodes the season frame (SeasonSaveCodec), and writes
//           atomically (temp -> fsync -> rename). Load reads the file, deframes it, and rebuilds all
//           eight sub-blobs the frame carries (see SeasonSaveBlobs for the enumeration) plus the
//           MatchEngine (only when the save carried a match).

using System;
using System.IO;

using Unity.Profiling;

using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// On-disk save/load for a season: one file carrying the living-world <see cref="WorldStore"/>
    /// composite, the <see cref="SeasonState"/>, the #29 per-club training states, the #41 per-club
    /// medical states, the #30 per-club appearance records, the #28 progression store and the #44
    /// discipline state — all seven always present — and, when a match is in progress, a running
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
        /// Captures <paramref name="world"/>, <paramref name="season"/>, this root's five other REQUIRED
        /// sub-blob sources, and (when present) <paramref name="matchOrNull"/>; encodes the season
        /// frame; and writes it to <paramref name="path"/> atomically (the §4.6.1.1 temp -> fsync ->
        /// rename contract). See <see cref="SeasonSaveBlobs"/> for the full eight-blob enumeration this
        /// frame carries — restating that list in a doc comment has drifted from the real parameter set
        /// twice before (v1.6, v1.17), so this summary points at the one place it is kept exact rather
        /// than repeating it a fourth time. Every sub-blob is captured and the frame encoded BEFORE the file is opened (the
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
        /// <param name="progression">The #28 career store — REQUIRED, never null-meaning-empty; this
        /// block IS the roster (KD-4).</param>
        /// <param name="discipline">The #44 discipline state (the sparse per-player tally). REQUIRED,
        /// never null: pass <c>new DisciplineState()</c> to say "no cards recorded yet", which still
        /// writes a well-formed zero-entry block rather than omitting one (#44 Appendix B), on the same
        /// terms as <paramref name="trainingClubs"/> / <paramref name="medicalClubs"/> /
        /// <paramref name="appearanceClubs"/> / <paramref name="progression"/> above.</param>
        /// <param name="disciplineWired">Whether a #44 discipline subsystem is driven behind this save
        /// at all — the fact <see cref="RequireDestinationCarriesNoDiscipline"/> keys on (ERR-030-038),
        /// and a REQUIRED parameter on this, the outermost write entry point (ERR-030-039).
        /// <para>
        /// It is <b>not</b> derivable from <paramref name="discipline"/>: FR-DC-017 drops a row the
        /// instant it reaches <c>(0, 0)</c> — mid-season when a ban is served out with no residual
        /// yellows, and again for every yellows-only row at the boundary sweep — so <c>Count == 0</c>
        /// is the NORMAL state of a correctly wired tally, not evidence that one was dropped. Pass
        /// <c>true</c> when the caller drives a tally (including one that has legitimately drained to
        /// zero entries), <c>false</c> when there is no discipline subsystem behind this save, which is
        /// the sole composition from which an empty <c>DISC</c> block is evidence of a drop.
        /// </para>
        /// <para>
        /// Never defaulted, and never asserted on the caller's behalf, for the same reason the five
        /// block parameters above reject null: a caller that can stay silent about wiring is a caller
        /// whose silence is indistinguishable from the loss. ERR-030-039 is what happens when one entry
        /// point does assert it — this method previously forwarded from a nine-argument public form
        /// that hardcoded <c>true</c>, on the reasoning that handing over a tally proves the caller
        /// drives #44. It proves only that the tally is not null: <c>new DisciplineState()</c> is
        /// exactly what an unwired caller passes (this parameter's own documentation sanctions it), and
        /// over a populated destination that is the ERR-030-036 loss with the guard bypassed.
        /// </para></param>
        public static void Save(
            WorldStore world,
            SeasonState season,
            MatchEngine.MatchEngine matchOrNull,
            string path,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs,
            ProgressionEngine progression,
            DisciplineState discipline,
            bool disciplineWired)
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
            // Required for the same reason as the three above, and more sharply: this block is the
            // ROSTER (#28 KD-4). A null-means-empty default would let a call site omit it, save
            // cleanly, and reload a career whose players have reverted to their day-0 attributes —
            // with the world, season and career blocks all intact around them, so nothing would look
            // wrong. "This season tracks no careers" is said with an empty ProgressionEngine.
            if (progression == null)
            {
                throw new ArgumentNullException(nameof(progression),
                    "Pass a ProgressionEngine (empty if the season tracks no careers) — null is not " +
                    "the empty set, and this block carries the roster (FR-PG-017 / #28 KD-4).");
            }
            // Required on the same terms as its four siblings above (#44 Appendix B): a defaulted
            // null-meaning-empty parameter would let a call site omit a season's discipline history and
            // still compile, save and load — indistinguishable from a game that never showed a card.
            if (discipline == null)
            {
                throw new ArgumentNullException(nameof(discipline),
                    "Pass a DisciplineState (new DisciplineState() if the season tracks no cards) — " +
                    "null is not the empty set (#44 Appendix B).");
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Save path must be non-empty.", nameof(path));
            }

            RequireCoherentCareerBlocks(trainingClubs, medicalClubs, appearanceClubs, progression);
            RequireCareerCursorsWithinClock(
                world.CurrentWorldTick, trainingClubs, medicalClubs, appearanceClubs, progression);

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
            var progressionBlock = new ProgressionBlock(progression.Snapshot());
            var disciplineBlock = new DisciplineBlock(DisciplineSaveCodec.Encode(discipline));
            byte[] matchBlob = matchOrNull != null ? MatchSaveManager.Encode(matchOrNull) : null;
            byte[] blob = SeasonSaveCodec.Encode(
                worldBlob, seasonBlob, in trainingBlock, in medicalBlock, in appearanceBlock,
                in progressionBlock, in disciplineBlock, matchBlob);

            // ERR-028-008: refuse to overwrite a roster with an empty one. #28's block is the
            // serialized roster (KD-4), so writing a zero-club block over a file that carries one
            // deletes a career's banked growth with EVERY gate green — the world, season, training,
            // medical and appearance blocks all intact around the hole, the frame still v5, Load still
            // succeeding. The realistic way to reach it is a resume that never threaded
            // SeasonSaveContents.Progression back into its loop, which no signature can force. It IS
            // detectable here, because the destination is readable: an empty store may create a file
            // and may overwrite an empty one, never a populated one.
            if (progression.ClubCount == 0)
            {
                RequireDestinationCarriesNoRoster(path);
            }

            // The SIBLING half, and it is the same defect one block-family over (AR pass 8). The guard
            // above protects the ROSTER against an empty store; nothing protected the #29/#41/#30 career
            // TRIPLE against empty arrays. That hole did not exist when the roster guard was written —
            // ERR-028-013 CREATED it by making "populated store, no career" a legal composition, and the
            // AR pass 5 carve-out (`&& trainingClubs.Length > 0`, below) then made that composition
            // SAVEABLE. Neither change revisited this guard.
            //
            // Demonstrated end to end: load a populated save, resume through the blessed progression-only
            // constructor, save back to the same path — no throw, and the reload returns
            // TrainingClubs=0, MedicalClubs=0, AppearanceClubs=0 beside Progression=4. A season of
            // conditioning, injury history and appearance records deleted with every gate green, the
            // world and season and roster intact around the hole. That is ERR-028-008's measured shape
            // (clubs 4 -> 0) exactly, in the block family its fix did not visit.
            if (trainingClubs.Length == 0 && medicalClubs.Length == 0 && appearanceClubs.Length == 0)
            {
                RequireDestinationCarriesNoCareer(path);
            }

            // The THIRD instance of the same defect, in the third block family (ERR-030-036, the C1/C2
            // AR's H1). Neither guard above can see it: the roster guard fires only when the #28 store
            // is empty, the career guard only when all three career blocks are, and the reachable case
            // is a resumed career with BOTH populated and the tally dropped — so a save sailed through
            // both and deleted every ban and every yellow in the destination, frame still v6, Load still
            // succeeding. FR-DC-014 retains no ledgers (§4.5), so the cards exist nowhere else and
            // nothing downstream can recompute them; a forgiven ban is simply gone.
            //
            // The realistic way to reach it was structural rather than careless: SeasonLoop.Restore had
            // no discipline parameter at all until this fix, so the documented resume path could not
            // carry SeasonSaveContents.Discipline even deliberately. That half is fixed at the source;
            // this half is the one that survives a future call site forgetting the argument, the same
            // argument the required `discipline` parameter above already makes for null.
            //
            // ERR-030-038: keyed on WIRING, not on the tally being empty — that inference was this
            // guard's own defect. It holds for the two siblings, whose families have no legitimate
            // empty state (a career roster never empties), and it is FALSE for #44: FR-DC-017 makes
            // the empty tally the canonical clean state, dropping a row the instant it reaches (0,0) —
            // mid-season the moment a served ban leaves no residual yellows, and every yellows-only row
            // at the boundary sweep. Keyed on Count, a correctly wired and correctly resumed loop whose
            // last row cleared could never save over its own file again, and stayed unsaveable until
            // fresh cards accrued, because the destination keeps the old rows forever — with the thrown
            // message telling the operator to do the very thing they had already done. `disciplineWired`
            // is the fact that actually distinguishes the loss, and it exists only at the call site.
            //
            // ERR-030-039: and it is now asked of EVERY caller. ERR-030-038 landed the flag on an
            // INTERNAL overload behind a public nine-argument form that passed `true` unconditionally,
            // so the guard was live on one of the two write entry points and bypassed on the other —
            // and the bypassed one is the form every external caller reaches. The reasoning ("a caller
            // that hands over a tally drives #44 by construction") excludes only null: `new
            // DisciplineState()` is exactly what an unwired caller passes, because the `discipline`
            // parameter's own doc sanctions it as the way to say "no cards recorded yet". Measured
            // against the built assemblies: two identical public calls, the second with a fresh tally,
            // took the destination from 1 row to 0 with no refusal. Appendix B.1's "passed to the
            // write, not re-derived from the block" is a rule about the WRITE, so it holds only when no
            // entry point can answer for the caller.
            if (!disciplineWired)
            {
                RequireDestinationCarriesNoDiscipline(path);
            }

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
        /// A loop with no career wired writes the three well-formed zero-club career blocks a
        /// careerless save carries — the same bytes <c>Array.Empty</c> produces through the long form.
        /// (A literally pre-T2 FILE is a v3 frame, which F3 refuses outright.)
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
                    : Array.Empty<ClubAppearanceStates>(),
                // A loop with no #28 store saves a well-formed EMPTY block — that is the honest
                // pre-#28 composition (a career whose rosters come from the bootstrap), and it
                // round-trips correctly. What must never happen is an empty block overwriting a file
                // whose roster came from #28; that is guarded at the write itself, below, because it is
                // a property of the DESTINATION rather than of this loop.
                loop.Progression ?? ProgressionEngine.Empty,
                // A loop with no #44 state saves a well-formed EMPTY block, the same honest posture the
                // progression block takes above: a career composed without discipline round-trips to
                // the zero-entry block it was built from. When discipline IS wired this is the live
                // tally, and it must be — a save that silently wrote an empty block would forgive
                // every outstanding suspension in the career, and FR-DC-014 rules out recomputing them
                // (no ledgers are retained, so the cards exist nowhere else). That hazard was stated
                // here one line above the call that caused it and nothing enforced it (ERR-030-036);
                // it is a property of the DESTINATION, not of this loop, so it is guarded at the write
                // itself — RequireDestinationCarriesNoDiscipline — exactly as the roster is.
                //
                // The `??` is where the two cases become indistinguishable, so the fact is passed
                // alongside it rather than re-derived from the result (ERR-030-038): `loop.Discipline`
                // is null for a loop that drives no discipline and non-null — possibly with zero
                // entries, which FR-DC-017 makes the ordinary state — for one that does.
                loop.Discipline ?? new DisciplineState(),
                disciplineWired: loop.Discipline != null);
        }

        // Reads the destination's progression block, if the destination exists and is a well-formed
        // season save. A file that cannot be read at all is not this guard's business — an unreadable
        // or foreign destination is simply overwritten, exactly as before.
        private static void RequireDestinationCarriesNoRoster(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            int existingClubs;
            try
            {
                SeasonSaveBlobs existing = SeasonSaveCodec.Decode(File.ReadAllBytes(path));
                ClubCareerStates[] blocks =
                    ProgressionSaveCodec.Decode(existing.ProgressionBlob, out _);
                existingClubs = blocks.Length;
            }
            catch (Exception)
            {
                // Not a readable season save (corrupt, truncated, a different format, or a v4 file).
                // Nothing to protect.
                return;
            }

            if (existingClubs > 0)
            {
                throw new InvalidOperationException(
                    "Refusing to overwrite " + path + ": it carries a career roster for " +
                    existingClubs + " club(s) and this save has none. #28's block IS the roster " +
                    "(KD-4), so this write would delete it silently. Resume the loop with the " +
                    "ProgressionEngine from SeasonSaveContents.Progression rather than rebuilding " +
                    "the league from the world seed.");
            }
        }

        /// <summary>
        /// The <see cref="RequireDestinationCarriesNoRoster"/> sibling, for the #29/#41/#30 career
        /// triple. An empty triple may create a file and may overwrite an empty one, never a populated
        /// one — the same rule, the same reason, the block family the roster guard did not cover.
        /// <para>
        /// Deliberately keyed on the TRIPLE being empty rather than on any one block: the three are
        /// written and read as one career by <see cref="RequireCoherentCareerBlocks"/>, so a partially
        /// empty set is a different (and already gated) error, and refusing on one empty block alone
        /// would reject the honest "this season tracks no medical state" composition.
        /// </para>
        /// </summary>
        private static void RequireDestinationCarriesNoCareer(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            int existingTraining;
            int existingMedical;
            int existingAppearance;
            try
            {
                SeasonSaveBlobs existing = SeasonSaveCodec.Decode(File.ReadAllBytes(path));
                existingTraining = TrainingSaveCodec.Decode(existing.TrainingBlob).Length;
                existingMedical = MedicalSaveCodec.Decode(existing.MedicalBlob).Length;
                existingAppearance = AppearanceSaveCodec.Decode(existing.AppearanceBlob).Length;
            }
            catch (Exception)
            {
                // Not a readable season save (corrupt, truncated, a different format, or an older
                // frame). Nothing to protect — the roster guard reasons identically.
                return;
            }

            int existingClubs = existingTraining > 0 ? existingTraining
                : existingMedical > 0 ? existingMedical
                : existingAppearance;

            if (existingClubs > 0)
            {
                throw new InvalidOperationException(
                    "Refusing to overwrite " + path + ": it carries #29/#41/#30 career state for " +
                    existingClubs + " club(s) and this save has none. A season of conditioning, injury " +
                    "history and appearance records would be deleted silently, with every other block " +
                    "intact around the hole. Resume the loop with the career from " +
                    "SeasonSaveContents (its TrainingClubs / MedicalClubs / AppearanceClubs) rather " +
                    "than composing a progression-only loop over a populated file.");
            }
        }

        /// <summary>
        /// The THIRD sibling (ERR-030-036), for the #44 discipline tally. A save that drives no
        /// discipline subsystem may create a file and may overwrite a card-less one, never one that
        /// carries rows.
        /// <para>
        /// Invoked on <c>disciplineWired == false</c>, NOT on the tally being empty (ERR-030-038) —
        /// and <c>disciplineWired</c> is a required parameter of the one public long-form
        /// <see cref="Save(WorldStore,SeasonState,MatchEngine.MatchEngine,string,ClubTrainingStates[],ClubInjuryStates[],ClubAppearanceStates[],ProgressionEngine,DisciplineState,bool)"/>,
        /// never asserted on a caller's behalf by a forwarding overload (ERR-030-039). The
        /// two sibling guards may key on emptiness because their families have no legitimate empty
        /// state — a career roster never empties — but #44's does: FR-DC-017 drops a row the instant it
        /// reaches <c>(0, 0)</c>, so the empty tally is the canonical clean state and reads as a drop
        /// only when nothing was driving discipline in the first place.
        /// </para>
        /// <para>
        /// Its subject is a family the two guards above share no key with — #44's state is keyed by
        /// <c>(PlayerId, CompetitionId)</c> with no club dimension at all — which is precisely why a
        /// resumed career with a populated store AND populated career blocks passed both while dropping
        /// every ban.
        /// </para>
        /// <para>
        /// The loss this refuses is unrecoverable in a way the sibling guards' is not: FR-DC-014 keeps
        /// no card ledgers, only the running <c>(Yellows, BanMatchesRemaining)</c> tally, so a forgiven
        /// suspension cannot be re-derived from anything else in the file.
        /// </para>
        /// <para>
        /// <b>Recorded, not fixed:</b> a loop wired with a FRESH <see cref="DisciplineState"/> instead
        /// of the file's is wired, so this guard passes it — and it is indistinguishable at this
        /// boundary from a tally that drained legitimately, since both present as "wired, zero rows".
        /// The documented resume path (<c>SeasonLoop.Restore</c>'s <c>disciplineOrNull</c>, fed from
        /// <see cref="SeasonSaveContents.Discipline"/>) is what closes that case; no destination
        /// predicate can.
        /// </para>
        /// </summary>
        private static void RequireDestinationCarriesNoDiscipline(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            int existingEntries;
            try
            {
                SeasonSaveBlobs existing = SeasonSaveCodec.Decode(File.ReadAllBytes(path));
                existingEntries = DisciplineSaveCodec.Decode(existing.DisciplineBlob).Count;
            }
            catch (Exception)
            {
                // Not a readable season save (corrupt, truncated, a different format, or a pre-v6
                // frame). Nothing to protect — both sibling guards reason identically.
                return;
            }

            if (existingEntries > 0)
            {
                throw new InvalidOperationException(
                    "Refusing to overwrite " + path + ": it carries #44 discipline state for " +
                    existingEntries + " player(s) and this save drives no discipline subsystem at " +
                    "all. Every outstanding suspension and every yellow would be forgiven silently, " +
                    "and FR-DC-014 retains no card ledgers, so nothing can recompute them. Resume the " +
                    "loop with the tally from SeasonSaveContents.Discipline (SeasonLoop.Restore's " +
                    "disciplineOrNull, or the constructor's seventh argument) rather than composing a " +
                    "discipline-less loop over a populated file. A wired tally that has legitimately " +
                    "drained to zero rows (FR-DC-017) is NOT this case and saves freely.");
            }
        }

        /// <summary>
        /// Reads the season save file at <paramref name="path"/>, deframes it, and reconstructs the full
        /// <see cref="SeasonSaveContents"/> this method returns — see its own summary for the complete
        /// enumeration (the world, the season, the per-club #29 training / #41 medical / #30 appearance
        /// states, the #28 progression store, and the #44 discipline state) — plus the
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
        /// against a career nothing else would have validated. Every save is checked, match or not; a
        /// save carrying a match additionally needs the blocks to rebuild the availability filter.
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
            ProgressionEngine progression = ProgressionEngine.Restore(blobs.ProgressionBlob);
            DisciplineState discipline = DisciplineSaveCodec.Decode(blobs.DisciplineBlob);

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
                world.CurrentWorldTick, trainingClubs, medicalClubs, appearanceClubs, progression);

            // M3, load side. Save runs the same four-way gate, so a file written by this build cannot
            // reach here incoherent — but a hand-edited or mispaired one can, and the symptom without
            // this is a null squad part-way through a restore rather than a refusal at the boundary
            // that owns the rule. Unconditional: the progression block is mandatory in the frame.
            RequireCoherentCareerBlocks(
                trainingClubs, medicalClubs, appearanceClubs, progression);

            // The in-progress match was configured with the COMPOSED availability filter (#30 §3.4 —
            // #41's FR-MD-023 injury removals AND #44's FR-DC-010 suspension removals since the C1/C2
            // landing), and the snapshot records only each team's ClubId — it cannot record "which
            // eighteen of the twenty-five". So restoring through the raw provider hands
            // ReprojectDistinctSquads the FULL roster, it re-runs LineupSelector over a different
            // candidate set, and the restored eleven is not the eleven that took the pitch: different
            // canonical attribute records on every slot, every gate green (the ClubId matches, the size
            // check passes), and the match then diverges from the pre-save run with nothing to announce
            // it. Re-applying the same filter — over the medical state AND the discipline tally carried
            // in THIS file, which is the state the match was configured against — reproduces the exact
            // squad, so selection lands on the same eleven.
            //
            // BOTH contributors, not just #41 (the C1/C2 AR's H2): the decorator originally called
            // PlayerCareerStates.SelectAvailable, which composes with `discipline: null`, while
            // SeasonLoop.BootFixtureEngine configured the engine through SelectAvailable WITH the
            // tally. A fixture in which a suspension changed the eleven therefore restored a strictly
            // larger candidate set and re-selected a DIFFERENT eleven — the identical defect this
            // decorator was created to close, reopened one contributor later. It also retires the
            // inherited proof for the snapshot's `_slotPlayerIds` exclusion: that exclusion is
            // mechanically sound (both reprojection paths write it and `_activeBenchSlot` is
            // serialized), but its safety PREMISE is "the provider yields the squad the match was
            // configured with", and only a composed filter makes that true again.
            //
            // Pass-through for a club the career does not carry, which is every club of every
            // careerless save (all three career blocks empty — a literally pre-T2 v3 FILE is refused
            // by F3). #44 has no club dimension — its tally is keyed (PlayerId, CompetitionId) — so
            // its removals still apply there; with an empty tally that is the identity and the restore
            // is bit-for-bit what it was.
            MatchEngine.MatchEngine match = null;
            if (blobs.MatchBlob != null)
            {
                // The dial position is irrelevant to this throwaway career — it only answers the
                // availability read; no day step ever runs on it — but it is constructed at the
                // production posture (armed, FR-MD-027) rather than encoding a stale default.
                var career = PlayerCareerStates.FromBlocks(
                    trainingClubs, medicalClubs, appearanceClubs, injuryOccurrenceEnabled: true);

                // #28 T2a: the roster comes from THIS FILE's career block whenever the file carries one,
                // never from the caller's provider. The caller's is the bootstrap rebuilt from the world
                // seed — day-0 attributes — and since KD-4 moved roster authority into the save, the two
                // differ by exactly the growth this career has banked. Restoring a match against the
                // bootstrap would put the same players on the pitch with the wrong attributes on every
                // slot, every gate green (the ClubIds match, the sizes match), and the match would
                // diverge from the pre-save run with nothing to announce it. Same reasoning as the
                // availability filter below, one layer further back: rebuild from the file, not from a
                // provider that merely looks like the right one.
                ISquadProvider rosterSource = progression.ClubCount > 0
                    ? new ProgressionSquads(progression)
                    : squads;
                ISquadProvider asConfigured = rosterSource == null
                    ? null
                    : new AvailabilityFilteredSquads(rosterSource, career, discipline);
                match = MatchSaveManager.Restore(blobs.MatchBlob, asConfigured);
            }

            return new SeasonSaveContents(
                world, season, trainingClubs, medicalClubs, appearanceClubs, progression, discipline,
                match);
        }

        /// <summary>
        /// An <see cref="ISquadProvider"/> that applies #30 §3.4's COMPOSED availability filter on the
        /// way out — both contributors, #41's injury removals and #44's suspension removals — so a
        /// restore re-selects from the same squad the match was configured with. Load-time only; never
        /// persisted (the <c>squads</c> / <c>canon</c> precedent).
        /// <para>
        /// <b>Both contributors, because the boot uses both</b> (the C1/C2 AR's H2).
        /// <see cref="SeasonLoop.BootFixtureEngine"/> filters through the same
        /// <see cref="AvailabilityComposition.Compose"/> call carrying the loop's tally, so a decorator
        /// that composed with <c>discipline: null</c> handed the re-selection a strictly LARGER
        /// candidate set on any fixture a suspension had touched — a different eleven on the pitch,
        /// ClubId matching, size gate passing, digest diverging with nothing to announce it. That is
        /// the defect this decorator was created to close, reopened one contributor later.
        /// </para>
        /// <para>
        /// It only reads the career and the tally, so the throwaway instances <see cref="Load"/> builds
        /// for this cannot disturb what is handed back in <see cref="SeasonSaveContents"/> — and would
        /// not reach the career blocks in any case, since
        /// <see cref="PlayerCareerStates.FromBlocks"/> copies the state arrays rather than borrowing
        /// them. The tally IS the instance handed back, which is safe for the same reason: every read
        /// here goes through <see cref="AvailabilityComposition"/>, which only queries it.
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
            private readonly DisciplineState _discipline;

            internal AvailabilityFilteredSquads(
                ISquadProvider inner, PlayerCareerStates career, DisciplineState discipline)
            {
                _inner = inner;
                _career = career;
                _discipline = discipline;
            }

            /// <inheritdoc />
            public Squad ResolveByClubId(int clubId)
            {
                Squad squad = _inner.ResolveByClubId(clubId);
                if (squad == null)
                {
                    // Null is the provider's own "unknown club" answer and the restore factory's
                    // fail-loud input — do not turn it into an exception here.
                    return squad;
                }

                // #41's contributor is club-keyed and fail-louds on a club it does not carry, so it is
                // withheld for a club this save has no career state for (every club of a careerless
                // save). #44's is not club-keyed at all — its tally is (PlayerId, CompetitionId) — so
                // it applies either way, and an empty tally makes Compose return the same instance.
                PlayerCareerStates careerOrNull = _career.CarriesClub(clubId) ? _career : null;
                return AvailabilityComposition.Compose(
                    squad, careerOrNull, _discipline, DisciplineConstants.LeagueCompetitionKey);
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
            ClubAppearanceStates[] appearanceClubs,
            ProgressionEngine progression)
        {
            // M3: the FOURTH career set joins the gate. Until it did, a file whose PROG block described
            // clubs {0,1} while the three career blocks described {0,1,2,3} was written and accepted —
            // failing later and elsewhere (a null squad mid-restore, or the SeasonLoop constructor
            // throwing at composition), long after the file had been declared good. The gate exists so
            // Save never writes what Load refuses; a set added without joining it is outside that
            // guarantee. Skipped for an EMPTY store, which is the honest pre-#28 composition — a career
            // whose rosters come from the bootstrap, not from #28.
            //
            // ALSO skipped when the three career sets are empty and the store is not. That is the
            // MIRROR composition, and it is legal by construction: ERR-028-013 made "populated #28
            // store, no #29/#41 career" a supported wiring, and `SeasonLoop` locks that it can advance
            // days and play a round. The carve-out above was written for one direction only, so the
            // blessed composition could run and play and then throw on Save — `Save(SeasonLoop, …)`
            // feeds `Array.Empty<ClubTrainingStates>()` for a careerless loop, and the length compare
            // below fired unconditionally. A career you can play and cannot persist is worse than one
            // that refuses at composition, because the loss lands only when the player saves.
            // The two sets describe one career ONLY when both exist; when one is absent there is
            // nothing to agree with, which is exactly what the empty-store case already says.
            if (progression != null && progression.ClubCount > 0 && trainingClubs.Length > 0)
            {
                ClubCareerStates[] prog = progression.ToBlocks();
                if (prog.Length != trainingClubs.Length)
                {
                    throw new ArgumentException(
                        $"The progression set carries {prog.Length} clubs but the three career sets "
                        + $"carry {trainingClubs.Length}; all four describe one career.",
                        nameof(progression));
                }

                var byClub = new System.Collections.Generic.Dictionary<int, ClubCareerStates>();
                for (int c = 0; c < prog.Length; c++)
                {
                    byClub[prog[c].ClubId] = prog[c];
                }

                for (int c = 0; c < trainingClubs.Length; c++)
                {
                    if (!byClub.TryGetValue(trainingClubs[c].ClubId, out ClubCareerStates pc))
                    {
                        throw new ArgumentException(
                            $"The progression set carries no club {trainingClubs[c].ClubId}, which the "
                            + "training/medical/appearance sets do.",
                            nameof(progression));
                    }

                    if (pc.Count != trainingClubs[c].Count)
                    {
                        throw new ArgumentException(
                            $"Club {pc.ClubId}: the progression set carries {pc.Count} players, the "
                            + $"career sets {trainingClubs[c].Count}.",
                            nameof(progression));
                    }

                    var progIds = new int[pc.Count];
                    for (int i = 0; i < pc.Count; i++)
                    {
                        progIds[i] = pc.Records[i].PlayerId;
                    }
                    var careerIds = (int[])trainingClubs[c].PlayerIds.Clone();
                    Array.Sort(progIds);
                    Array.Sort(careerIds);
                    for (int i = 0; i < progIds.Length; i++)
                    {
                        if (progIds[i] != careerIds[i])
                        {
                            throw new ArgumentException(
                                $"Club {pc.ClubId}: the progression set and the career sets describe "
                                + "different players.",
                                nameof(progression));
                        }
                    }
                }
            }

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
        /// Four persisted cursors, two failure modes, one owner: the only layer holding the world
        /// blob and the career blocks together. Checked at Save too, so this root never writes a
        /// file its own Load refuses (the T1 AR's Encode/Decode-asymmetry rule).
        /// </summary>
        private static void RequireCareerCursorsWithinClock(
            uint worldTick,
            ClubTrainingStates[] trainingClubs,
            ClubInjuryStates[] medicalClubs,
            ClubAppearanceStates[] appearanceClubs,
            ProgressionEngine progression)
        {
            // One predicate set, one owner (AR pass 9 M1): the per-cursor rules live on
            // PlayerCareerStates beside the composition-boundary walk, so the two gates cannot
            // drift — the RequireGloballyUniquePlayerIds shape. The three kinds iterate
            // independently because the coherence gate accepts permuted club order per kind.
            for (int c = 0; c < trainingClubs.Length; c++)
            {
                for (int i = 0; i < trainingClubs[c].Count; i++)
                {
                    PlayerCareerStates.RequireTrainingCursorWithinClock(
                        worldTick, trainingClubs[c].ClubId, trainingClubs[c].PlayerIds[i],
                        trainingClubs[c].States[i].LastAdvancedWorldDay, "Career save");
                }
            }

            for (int c = 0; c < medicalClubs.Length; c++)
            {
                for (int i = 0; i < medicalClubs[c].Count; i++)
                {
                    PlayerCareerStates.RequireMedicalCursorWithinClock(
                        worldTick, medicalClubs[c].ClubId, medicalClubs[c].PlayerIds[i],
                        medicalClubs[c].States[i].LastAdvancedWorldDay, "Career save");
                }
            }

            for (int c = 0; c < appearanceClubs.Length; c++)
            {
                for (int i = 0; i < appearanceClubs[c].Count; i++)
                {
                    PlayerCareerStates.RequireAppearanceAnchorWithinClock(
                        worldTick, appearanceClubs[c].ClubId, appearanceClubs[c].PlayerIds[i],
                        appearanceClubs[c].States[i].BitsAsOfWorldDay, "Career save");
                }
            }

            // The FOURTH persisted per-player cursor (ERR-028-007). Added to this walker rather than
            // checked separately, because a second walk over the same rule is the parallel-surface
            // defect AR pass 9 collapsed for the first three.
            if (progression != null)
            {
                ClubCareerStates[] careerBlocks = progression.ToBlocks();
                for (int c = 0; c < careerBlocks.Length; c++)
                {
                    for (int p = 0; p < careerBlocks[c].Count; p++)
                    {
                        PlayerCareerStates.RequireProgressionCursorWithinClock(
                            worldTick,
                            careerBlocks[c].ClubId,
                            careerBlocks[c].Records[p].PlayerId,
                            careerBlocks[c].Lifecycles[p].LastAdvancedWorldDay,
                            "Season save");

                        // M2(b): the sibling check, same walk, the file-boundary twin of the
                        // SeasonLoop composition check — one owner, both boundaries delegating.
                        PlayerCareerStates.RequireBirthWorldDayWithinClock(
                            worldTick,
                            careerBlocks[c].ClubId,
                            careerBlocks[c].Records[p].PlayerId,
                            careerBlocks[c].Lifecycles[p].BirthWorldDay,
                            "Season save");
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
// | 1.16    | 2026-08-08 | —      | Balance-pass AR pass 9 (M1): RequireCareerCursorsWithinClock's   |
// |         |            |        | three hand-copied predicate loops now delegate to                |
// |         |            |        | PlayerCareerStates' shared per-cursor statics — one rule, one    |
// |         |            |        | owner; the local copy's medical-lag clause had no isolating      |
// |         |            |        | case at Save or Load (deleting it left the suite green).         |
// | 1.17    | 2026-08-08 | —      | Balance-pass AR pass 13 (L2, doc): two stale counts — "the two    |
// |         |            |        | zero-club blocks" (three since D2) and "every save written        |
// |         |            |        | before T2" (frame v4 refuses a pre-T2 file; the live case is a    |
// |         |            |        | careerless save).                                                  |
// | 1.18    | 2026-08-08 | —      | #28 T2a: Save takes a required ProgressionEngine and writes the   |
// |         |            |        | PROG sub-blob; Load restores it and prefers it over the caller's  |
// |         |            |        | provider as the match-restore roster source; the career-cursor    |
// |         |            |        | walker gains #28's fourth cursor (ERR-028-007); Save refuses to   |
// |         |            |        | overwrite a populated roster with an empty one (ERR-028-008).     |
// | 1.19    | 2026-08-10 | —      | Doc-only: RequireCareerCursorsWithinClock's XML doc still said    |
// |         |            |        | "Three persisted cursors" after v1.18 made the method walk four   |
// |         |            |        | (ERR-028-007's progression cursor). Corrected to "Four" — no      |
// |         |            |        | logic change.                                                      |
// | 1.20    | 2026-08-10 | —      | AR pass 5 (High): RequireCoherentCareerBlocks carved out an EMPTY |
// |         |            |        | store (the honest pre-#28 composition) but never its mirror — a   |
// |         |            |        | populated #28 store beside an empty #29/#41 career, the           |
// |         |            |        | composition ERR-028-013 blessed and SeasonLoop locks can advance  |
// |         |            |        | and play. Save(SeasonLoop, …) feeds Array.Empty<ClubTrainingStates>|
// |         |            |        | for a careerless loop, so the length compare fired unconditionally|
// |         |            |        | and a progression-only career could not be saved at all. The      |
// |         |            |        | carve-out is now symmetric (&& trainingClubs.Length > 0).          |
// |         |            |        | Mutation-verified: reverting the predicate fails the new           |
// |         |            |        | SeasonLoopProgressionTests lock.                                   |
// | 1.21    | 2026-08-11 | —      | AR pass 6, M2(b). The block-level cursor walk gains              |
// |         |            |        | PlayerCareerStates.RequireBirthWorldDayWithinClock beside          |
// |         |            |        | RequireProgressionCursorWithinClock — the file-boundary twin of    |
// |         |            |        | SeasonLoop.cs v1.19's composition-boundary check. One owner, both  |
// |         |            |        | boundaries delegating (the AR pass-9 M1 shape).                    |
// | 1.22    | 2026-08-13 | —      | #44 T1 (roadmap C1): Save gains a required DisciplineState        |
// |         |            |        | parameter (rejects null, the progression/appearance precedent)    |
// |         |            |        | and encodes it as the new mandatory discipline sub-blob; Load     |
// |         |            |        | decodes it and threads it into SeasonSaveContents. The             |
// |         |            |        | Save(SeasonLoop, …) forwarding overload passes a fresh             |
// |         |            |        | new DisciplineState() — #44 has no SeasonLoop seam yet and         |
// |         |            |        | SeasonLoop.cs is out of scope for this landing (concurrent edit).  |
// |         |            |        | Deliberately NOT added to RequireCoherentCareerBlocks /            |
// |         |            |        | RequireCareerCursorsWithinClock: DisciplineState carries no        |
// |         |            |        | per-player world-day cursor and is keyed by                        |
// |         |            |        | (PlayerId, CompetitionId) rather than by club, so neither of the   |
// |         |            |        | existing cross-block coherence walks has a natural analogue for    |
// |         |            |        | it; flagged for owner review rather than invented here.            |
// | 1.23    | 2026-08-13 | —      | #44 C1/C2 adversarial review, H1 + H2.                             |
// |         |            |        | H1 (ERR-030-036): new RequireDestinationCarriesNoDiscipline,       |
// |         |            |        | invoked when the tally is empty — the third sibling of the roster  |
// |         |            |        | and career-triple guards, in the third block family, and the one   |
// |         |            |        | neither of those could see: they key on #28's club count and on    |
// |         |            |        | the three career blocks, so a resumed career with BOTH populated   |
// |         |            |        | and the tally dropped passed both and deleted every ban and every  |
// |         |            |        | yellow in the destination — a hazard this file's own comment       |
// |         |            |        | stated verbatim one line above the call that caused it. Paired     |
// |         |            |        | with SeasonLoop.Restore's new disciplineOrNull (SeasonLoop v1.21), |
// |         |            |        | without which the documented resume path could not carry the       |
// |         |            |        | tally at all.                                                      |
// |         |            |        | H2: AvailabilityFilteredSquads composes BOTH contributors. It      |
// |         |            |        | called PlayerCareerStates.SelectAvailable (discipline: null) while |
// |         |            |        | SeasonLoop.BootFixtureEngine configured the engine WITH the tally, |
// |         |            |        | so a mid-match save of a fixture a suspension had touched restored |
// |         |            |        | a strictly larger candidate set and re-selected a different eleven |
// |         |            |        | — the exact H2 the #29/#41 T2 AR closed for injuries, reopened one |
// |         |            |        | contributor later. Now one AvailabilityComposition.Compose call,   |
// |         |            |        | mirroring the boot's.                                              |
// | 1.24    | 2026-08-13 | —      | #44 C1/C2 adversarial review round 3, H3 (ERR-030-038): v1.23's    |
// |         |            |        | own guard refused a LEGITIMATE save and made a career's file       |
// |         |            |        | permanently unsaveable. It keyed on discipline.Count == 0 and read |
// |         |            |        | that as proof the tally was dropped — true for the roster and the  |
// |         |            |        | career triple, FALSE for #44, where FR-DC-017 makes the empty      |
// |         |            |        | tally the canonical clean state (a served ban with no residual     |
// |         |            |        | yellows is dropped mid-season; the boundary sweep drops every      |
// |         |            |        | yellows-only row). A correctly wired, correctly resumed loop whose |
// |         |            |        | last row cleared could never overwrite its own file again, and     |
// |         |            |        | stayed unsaveable until fresh cards accrued. Re-keyed on WIRING:   |
// |         |            |        | a new internal Save overload takes a required disciplineWired      |
// |         |            |        | flag, the public long form passes true (a caller that hands over a |
// |         |            |        | tally drives #44 by construction — null is refused), and           |
// |         |            |        | Save(SeasonLoop, …) passes loop.Discipline != null, which is where |
// |         |            |        | the `??` would otherwise discard the distinction. Mutation-        |
// |         |            |        | verified: restoring the Count == 0 predicate fails both new locks. |
// | 1.25    | 2026-08-13 | —      | #44 C1/C2 adversarial review round 4, H4 (ERR-030-039): a defect   |
// |         |            |        | in v1.24's own fix. The flag landed on an INTERNAL overload while  |
// |         |            |        | the public nine-argument form forwarded disciplineWired: true      |
// |         |            |        | unconditionally, so the guard was live on one write entry point    |
// |         |            |        | and bypassed on the other — the one every external caller reaches. |
// |         |            |        | The stated reasoning ("a caller that hands over a tally drives #44 |
// |         |            |        | by construction") excludes only null, and the `discipline`         |
// |         |            |        | parameter's own doc sanctions new DisciplineState() as the way to  |
// |         |            |        | say "no cards recorded yet" — which over a populated destination   |
// |         |            |        | is the ERR-030-036 loss with the guard bypassed (measured against  |
// |         |            |        | the built assemblies: file rows 1 -> 0, no refusal). It also       |
// |         |            |        | contradicted #30 Appendix B.1 v1.4's own MUST, which requires the  |
// |         |            |        | wiring fact to be PASSED to the write rather than re-derived —     |
// |         |            |        | satisfied at one of the two entry points and re-derived from       |
// |         |            |        | nothing at all at the other. Fixed by promoting disciplineWired    |
// |         |            |        | onto the PUBLIC long form (required, never defaulted — the five    |
// |         |            |        | block parameters' own convention) and deleting the forwarding      |
// |         |            |        | overload, so one long form exists and no entry point can answer    |
// |         |            |        | for its caller. Locked by                                          |
// |         |            |        | Save_ThePublicLongForm_DrivingNoDiscipline_CannotEmptyAPopulated-  |
// |         |            |        | Tally; mutation-verified by reinstating the hardcode inside the    |
// |         |            |        | promoted method (disciplineWired = true).                          |
// | 1.26    | 2026-08-15 | —      | L3 (reviewed-findings pass), formatting only: trailing whitespace  |
// |         |            |        | removed after RequireCareerCursorsWithinClock's appearance-anchor  |
// |         |            |        | loop, and that method's closing brace moved from column 0 to       |
// |         |            |        | column 8 to match its own opening indent (FR-CS-011/012). No       |
// |         |            |        | behaviour change.                                                  |
// | 1.27    | 2026-08-15, later | — | Reviewed findings pass, L4. DisciplineConstants.                |
// |         |            |        | LEAGUE_COMPETITION_KEY -> LeagueCompetitionKey rename. One         |
// |         |            |        | reference updated. No behaviour change.                            |
// | 1.28    | 2026-08-15, later still | — | Reviewed findings pass, L1+L2 (doc only).                 |
// |         |            |        | L1: Load's summary said a save with no match is "untouched" by     |
// |         |            |        | the career-coherence gate; RequireCoherentCareerBlocks actually    |
// |         |            |        | runs unconditionally, before and outside the MatchBlob != null     |
// |         |            |        | branch — corrected to say every save is checked, only the          |
// |         |            |        | availability-filter rebuild (PlayerCareerStates.FromBlocks) is     |
// |         |            |        | match-only. L2: the Save summary, the header Purpose's             |
// |         |            |        | Load-rebuild sentence, and the Load summary each still enumerated  |
// |         |            |        | a five/six-blob frame after two later additions (appearance,       |
// |         |            |        | progression) — this file's own third and fourth instance of that   |
// |         |            |        | class (v1.6, v1.17). Pointed each at SeasonSaveBlobs /              |
// |         |            |        | SeasonSaveContents, whose own summaries already enumerate all      |
// |         |            |        | eight sub-blobs, rather than restating the list a fifth time. No   |
// |         |            |        | behaviour change.                                                   |
#endregion
