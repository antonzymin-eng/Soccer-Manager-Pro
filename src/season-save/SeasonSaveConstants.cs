// File:     src/season-save/SeasonSaveConstants.cs
// Created:  2026-07-22
// Modified: 2026-08-16, later (round-4 reviewed-findings pass over the ERR-030-046 landing — the
//           EXTREMIS_SEARCH_CANDIDATE_CAP doc's "self-healing" sentence conflated the search
//           resuming beyond the cap with the guarantee resuming; corrected to say the search resumes
//           but a greedily committed candidate is never revisited, so the composition can remain
//           above the minimum for that fixture — v1.10)
// Prior-Modified: 2026-08-16 (ERR-030-046 — + EXTREMIS_SEARCH_CANDIDATE_CAP, the depleted-squad back-fill's
//           exhaustive-search probe budget — v1.9)
// Prior-Modified: 2026-08-13 (#44 T1, roadmap C1 — v1.8)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) KD-4; Discipline &
//           Suspensions #44 Appendix B; Season & Competition Loop #30 §3.4 (the depleted-squad
//           back-fill's search bound — ERR-030-046); Code Standards #20
// Purpose:  Constant catalogue for the season save-file frame. Holds the season-frame format version —
//           distinct from every version the frame nests: WORLD_STORE_FORMAT_VERSION,
//           SEASON_STATE_FORMAT_VERSION, TRAINING_SAVE_FORMAT_VERSION, MEDICAL_SAVE_FORMAT_VERSION,
//           APPEARANCE_SAVE_FORMAT_VERSION, PROGRESSION_SAVE_FORMAT_VERSION and
//           DISCIPLINE_SAVE_FORMAT_VERSION at the sub-blob level, MATCH_SAVE_FORMAT_VERSION for the
//           optional match block, and — a level deeper still — the two snapshot schema versions nested
//           inside the world and match blobs. Also holds the one non-format constant this assembly
//           owns: the availability composition's exhaustive-search candidate cap.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for the unified season save file (unified-season-save-design.md §3 layout / KD-4).
    /// </summary>
    public static class SeasonSaveConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] The season save-file FRAMING version — the outermost format version in the save
        /// stack (KD-4). It gates only the season frame (the <c>matchPresent</c> flag + the eight
        /// length-prefixed sub-blobs — the living-world composite, the season state, the #29 training
        /// block, the #41 medical block, the #30 appearance block, the #28 career-state block, the #44
        /// discipline block, and the
        /// optional match block); the
        /// inner versions ride inside
        /// their own sub-blobs and are re-checked by
        /// <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> /
        /// <see cref="SeasonStateCodec.Decode"/> /
        /// <see cref="TacticalDirector.TrainingSystem.TrainingSaveCodec.Decode"/> /
        /// <see cref="TacticalDirector.InjuriesMedical.MedicalSaveCodec.Decode"/> /
        /// <see cref="AppearanceSaveCodec.Decode"/> /
        /// <see cref="TacticalDirector.PlayerProgression.ProgressionSaveCodec.Decode"/> /
        /// <see cref="TacticalDirector.Discipline.DisciplineSaveCodec.Decode"/> /
        /// <c>MatchSaveCodec.Decode</c> themselves. A mismatch fails loud on load — no cross-version
        /// migration at Stage 0. Bump only on a season-frame layout change. Value: 6.
        /// <para>
        /// <b>1 → 2 at #30 T1 (FR-SN-020).</b> The frame gained the season-state sub-blob between the
        /// world and match blocks (#30 Appendix B). The world blob
        /// (<c>WORLD_STORE_FORMAT_VERSION</c>) and match blob (<c>MATCH_SAVE_FORMAT_VERSION</c>) are
        /// byte-untouched by that change — only the frame around them moved, which is exactly what
        /// this version gates. A v1 file is rejected fail-loud (no migration at Stage 0).
        /// </para>
        /// <para>
        /// <b>2 → 3 at #29/#41 T1 (FR-TR-018 / FR-MD-017).</b> The frame gained two further sub-blobs —
        /// the training block (<c>TRAINING_SAVE_FORMAT_VERSION</c>) and the medical block
        /// (<c>MEDICAL_SAVE_FORMAT_VERSION</c>) — between the season block and the optional match block.
        /// Both are always present (an empty block is a zero-club block, not an absent one), so the
        /// optional match block stays last and keeps its presence flag. Every pre-existing blob is
        /// byte-untouched by that change; only the frame around them moved. A v2 file is rejected
        /// fail-loud.
        /// </para>
        /// <para>
        /// <b>3 → 4 at the #29/#41 balance pass (ERR-041-010(b)).</b> The frame gained the #30
        /// appearance sub-blob (<see cref="APPEARANCE_SAVE_FORMAT_VERSION"/>) between the medical block
        /// and the optional match block — mandatory for the same reason the #29/#41 blocks are: an
        /// appearance record has no absent case, only an empty one. A v3 file is rejected fail-loud.
        /// </para>
        /// <para>
        /// <b>4 → 5 at #28 T1 (FR-PG-017).</b> The frame gained the #28 career-state sub-blob
        /// (<c>PROGRESSION_SAVE_FORMAT_VERSION</c>) between the appearance block and the optional match
        /// block — mandatory on the same argument, and load-bearing in a way the sibling career blocks
        /// are not: it carries the <b>evolving <c>PlayerRecord</c> set itself</b> (#28 KD-4), so from
        /// this version on the roster is a function of the SAVE rather than of the world seed. A v4 file
        /// is rejected fail-loud. This retires the roadmap-A3 property that a career could be reopened
        /// from the seed alone.
        /// </para>
        /// <para>
        /// <b>5 → 6 at #44 T1 (roadmap C1).</b> The frame gained the mandatory #44 discipline sub-blob
        /// (<c>DISCIPLINE_SAVE_FORMAT_VERSION</c>, magic <c>DISCIPLINE_SAVE_MAGIC</c>) between the #28
        /// career-state block and the optional match block — last of the mandatory set, on the same
        /// "an empty set is a well-formed zero-entry block, not an absent one" argument as its siblings.
        /// Every pre-existing blob is byte-untouched by that change; only the frame around them moved. A
        /// v5 file is rejected fail-loud.
        /// </para>
        /// </summary>
        public const uint SEASON_SAVE_FORMAT_VERSION = 6;

        /// <summary>
        /// [FIXED] The #30 appearance sub-blob's leading self-identifying tag — ASCII <c>"APPR"</c>,
        /// written before <see cref="APPEARANCE_SAVE_FORMAT_VERSION"/> (the ERR-029-005 rule: a format
        /// version distinguishes generations of ONE format, never one format from another, and every
        /// sub-blob format in this stack sits at version 1).
        /// </summary>
        public const uint APPEARANCE_SAVE_MAGIC = 0x41505052;   // 'A''P''P''R'

        /// <summary>[FIXED] The #30 appearance sub-blob version. Gates the generation of the format identified by <see cref="APPEARANCE_SAVE_MAGIC"/>.</summary>
        public const uint APPEARANCE_SAVE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] The structural ceiling of the appearance day-bitmask window, in world-days: bit 0
        /// is the anchor day itself — which the FR-MD-010 window never counts — so a u32 mask can
        /// answer a window of at most 31 PRIOR days. <c>AppearanceWindow.RequireValidWindow</c> reads
        /// this; the <c>[GT]</c> <c>AppearanceWindowDays</c> must stay within it (AR pass 5 — the
        /// bound was previously a bare literal at the guard and in its catalogue lock, free to drift).
        /// </summary>
        public const int APPEARANCE_BITMASK_MAX_WINDOW_DAYS = 31;

        /// <summary>
        /// [FIXED] The largest number of still-removed suspended candidates
        /// <c>AvailabilityComposition.ChooseSuspendedCandidate</c> will search exhaustively
        /// (ERR-030-046). At or below this count the back-fill enumerates every subset of the
        /// candidates and returns a choice that provably minimises how many reinstated-suspended
        /// players end up in the starting eleven; above it, the pass degrades to the ascending-rank
        /// greedy with no minimality claim. The exact search resumes once the candidate count falls
        /// back within the bound, but the guarantee does not: a greedily committed candidate is not
        /// revisited, so the composition can remain above the minimum for that fixture.
        /// <para>
        /// <b>This is an algorithmic budget, not a gameplay dial, and it is deliberately not
        /// <c>[GT]</c>.</b> The subset enumeration is <c>O(2^m)</c>: the value bounds the probe count
        /// at <c>m·2^(m+1)</c> selection walks (98,304 at the cap), which is the only thing it decides.
        /// It changes no football outcome that the search itself does not already decide, so it is not
        /// subject to KD-W1's calibration freeze and there is nothing for a balance pass to fit. Raise
        /// it only against a measured probe-cost budget. Value: 12 — one below the 2^13 probe bound,
        /// and far above the concurrent-suspension count any measured card rate produces.
        /// </para>
        /// </summary>
        public const int EXTREMIS_SEARCH_CANDIDATE_CAP = 12;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1 (FR-SN-020): SEASON_SAVE_FORMAT_VERSION 1 -> 2 — the      |
// |         |            |        | frame gained the season-state sub-blob between the world and     |
// |         |            |        | match blocks; both of those blobs stay byte-untouched.           |
// | 1.2     | 2026-08-06 | —      | #29/#41 T1 (FR-TR-018 / FR-MD-017): 2 -> 3 — the frame gained    |
// |         |            |        | the training and medical sub-blobs between the season block and  |
// |         |            |        | the optional match block; the other three stay byte-untouched.   |
// | 1.3     | 2026-08-06 | —      | Doc-drift fix (no code/value change): the SEASON_SAVE_FORMAT_    |
// |         |            |        | VERSION summary still said "Value: 2" and "three sub-blobs", and |
// |         |            |        | its <see cref> list omitted TrainingSaveCodec.Decode /           |
// |         |            |        | MedicalSaveCodec.Decode; the file-header Purpose block named     |
// |         |            |        | only three of the five nested versions. Both corrected.          |
// | 1.4     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): 3 -> 4 — the frame gained the  |
// |         |            |        | mandatory #30 appearance sub-blob between the medical block and  |
// |         |            |        | the optional match block; + APPEARANCE_SAVE_MAGIC ("APPR") and   |
// |         |            |        | APPEARANCE_SAVE_FORMAT_VERSION = 1 (the ERR-029-005 rule).       |
// | 1.5     | 2026-08-08 | —      | Balance-pass AR pass 5 (L4): + [FIXED]                          |
// |         |            |        | APPEARANCE_BITMASK_MAX_WINDOW_DAYS = 31 — the bitmask's        |
// |         |            |        | structural window bound, previously a bare literal at the     |
// |         |            |        | AppearanceWindow guard and in its catalogue lock.             |
// | 1.6     | 2026-08-08 | —      | Balance-pass AR pass 9 (L3): SEASON_SAVE_FORMAT_VERSION's       |
// |         |            |        | inner-decoder cref list gains AppearanceSaveCodec.Decode — the  |
// |         |            |        | v1.3 row fixed this same omission for the training/medical     |
// |         |            |        | codecs; the appearance codec added at v1.4 repeated it.        |
// | 1.7     | 2026-08-08 | —      | #28 T1: SEASON_SAVE_FORMAT_VERSION 4 -> 5 for the mandatory PROG|
// |         |            |        | career-state sub-blob (FR-PG-017) — the first sub-blob to carry |
// |         |            |        | roster data, which retires roadmap A3's from-the-seed rebuild.  |
// | 1.8     | 2026-08-13 | —      | #44 T1: SEASON_SAVE_FORMAT_VERSION 5 -> 6 for the mandatory DISC|
// |         |            |        | sub-blob (roadmap C1).                                           |
// | 1.9     | 2026-08-16 | —      | ERR-030-046: + [FIXED] EXTREMIS_SEARCH_CANDIDATE_CAP = 12, the  |
// |         |            |        | probe budget for AvailabilityComposition's exhaustive           |
// |         |            |        | clean-completion search. An algorithmic bound (2^13 probes),    |
// |         |            |        | NOT a gameplay dial: it decides only how many subsets are       |
// |         |            |        | enumerated before the pass degrades to ascending-rank greedy,   |
// |         |            |        | so it is not [GT] and is not subject to KD-W1. First non-format |
// |         |            |        | constant in this catalogue; the file Purpose says so.           |
// | 1.10    | 2026-08-16, later | — | Round-4 reviewed-findings pass over the ERR-030-046 landing     |
// |         |            |        | (Medium, one of four sites). EXTREMIS_SEARCH_CANDIDATE_CAP's    |
// |         |            |        | doc said the beyond-cap greedy fallback "self-heals ... the     |
// |         |            |        | count falls back within the bound" — true of the SEARCH, false  |
// |         |            |        | of the OUTCOME: a candidate the greedy pass commits is never    |
// |         |            |        | un-committed, so the composition can stay above the minimum for |
// |         |            |        | that fixture even after the exact search resumes. Reworded; the |
// |         |            |        | mirrored wording at AvailabilityComposition.cs (both sites),    |
// |         |            |        | section-3.md §3.4, and discipline-suspensions/{section-2,       |
// |         |            |        | section-7}.md corrected in the same pass.                       |
#endregion
