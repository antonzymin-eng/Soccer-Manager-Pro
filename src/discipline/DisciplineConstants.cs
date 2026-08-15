// File:     src/discipline/DisciplineConstants.cs
// Created:  2026-08-13
// Modified: 2026-08-15 (#44 C1/C2 adversarial review round 4, M24/L18/ERR-017-004 — v1.2: the three
//           card-kind constants are now real [CROSS] mirrors of the new
//           EventSystemConstants.CARD_KIND_YELLOW/RED/SECOND_YELLOW declaration ([FIXED], ALL_CAPS —
//           Event System #17 §3.10) instead of independent literals citing #17 in prose only; class
//           doc's region-order line corrected Fixed → GT to Fixed → Cross → GT (L18, stale since L17
//           added the Cross region one revision ago).
//           Prior: 2026-08-13 (#44 C1/C2 adversarial review round 5, L17 — the three card-kind constants
//           re-tagged [FIXED] -> [CROSS] and renamed PascalCase, moved into a new Cross region — v1.1)
// Author:   —
// Spec:     Discipline & Suspensions #44 Appendix A (constant catalogue) + §3.2/§3.3; ERR-044-001
//           (the magic-before-version rule, ERR-029-005/ERR-041-009 class); Code Standards #20
// Purpose:  Every numeric constant for #44 accumulation, ban lengths, the partition key and the save
//           block's identity. No magic literals in DisciplineRules or DisciplineSaveCodec.

using TacticalDirector.EventSystem;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// Constant catalogue for Discipline &amp; Suspensions #44 (Appendix A). Region order (Code
    /// Standards #20): Fixed → Cross → GT (L18: this line said "Fixed → GT" after L17 had already
    /// added the Cross region below — corrected to match the actual region order in this file).
    /// <para>
    /// <b>Every value here is an integer</b> (FR-DC-020). Nothing in #44 is a float — the whole
    /// subsystem is counting, and the integer posture keeps it clear of float-mode sensitivity for
    /// free.
    /// </para>
    /// <para>
    /// The four <c>[GT]</c> magnitudes are <b>illustrative</b> (Appendix A's tag note, the #21 G2
    /// precedent): the reviewed contract is the SHAPE — threshold-and-residual accumulation, additive
    /// stacking, one decrement per played club fixture — not the numbers. They are deliberately NOT
    /// calibrated at this landing: <b>KD-W1</b> forbids fitting a <c>[GT]</c> against an incompletely
    /// wired engine, and the foul population #44 reads is pre-tackle (<c>defensive-ai</c>'s challenge
    /// ships at <c>TackleContactRadiusM = 0</c>, W2) and 4× football on red cards.
    /// </para>
    /// <para>
    /// <b>Config keys are guarded at their consuming sites, not here</b> (ERR-041-003, and AR pass
    /// 10's severity-split finding twice-filed): the locks below run config-unbound, so they see the
    /// fallbacks forever while a shipped config violates them. <see cref="DisciplineRules"/> fails
    /// loud at the site that would otherwise write the breach.
    /// </para>
    /// </summary>
    public static class DisciplineConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Self-identifying magic leading the discipline sub-blob's bytes — ASCII
        /// <c>'D' 'I' 'S' 'C'</c>. Written and checked BEFORE
        /// <see cref="DISCIPLINE_SAVE_FORMAT_VERSION"/>.
        /// <para>
        /// <b>A format version is not a format identifier</b> (ERR-029-005 / ERR-041-009, now a MUST
        /// in #29 §4.4 and #41 §4.4; ERR-028-004 is the third instance). Every sub-blob format under
        /// the season frame sits at version 1, and #44's <c>version | entryCount | entries…</c> prefix
        /// is byte-shaped exactly like the progression, training and medical blocks — so without the
        /// magic a transposed block decodes cleanly, completely and silently as the wrong subsystem's
        /// state. #44 Appendix B specified the block version-first with no magic; that is the defect
        /// <b>ERR-044-001</b> corrects, and this constant is the correction's load-time half. The
        /// compile-time half is <c>SeasonSave.DisciplineBlock</c>.
        /// </para>
        /// <para>Deliberately NOT an RNG domain tag — #44 has no draw site at all (FR-DC-019).</para>
        /// </summary>
        public const uint DISCIPLINE_SAVE_MAGIC = 0x44495343;   // 'D''I''S''C'

        /// <summary>
        /// [FIXED] The discipline sub-blob version (#44 Appendix B). Gates the generation of the
        /// format identified by <see cref="DISCIPLINE_SAVE_MAGIC"/>. Deep extensions (per-offence
        /// ban classes) APPEND behind this gate.
        /// </summary>
        public const uint DISCIPLINE_SAVE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] The minimal-tier <c>CompetitionId</c> partition key (FR-DC-012). Aligns with #43's
        /// <c>LEAGUE_COMPETITION_ID = 0</c>, carried here as a plain <c>int</c> so #44 needs no #43
        /// assembly reference. Per-competition tallies are a T3 partition activation over the
        /// <c>(PlayerId, CompetitionId)</c> key, not a rewrite.
        /// </summary>
        public const int LEAGUE_COMPETITION_KEY = 0;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Card kind: a first (or non-promoting) caution — <c>CardIssuedEvent.CardKind</c>'s own
        /// domain ordinal 0 (Event System #17 Appendix A row 0x06: "Card kind: 0=Yellow, 1=Red,
        /// 2=SecondYellow (domain ordinal)"). #44 consumes this value read-only off the tap and never
        /// sets it independently (L17) — the single-consumer mirror rule (root <c>CLAUDE.md</c> tag
        /// table; <c>src/CLAUDE.md</c> §4.2), mirrored directly rather than through
        /// <c>ProjectConstants.cs</c> because #44 is the only consuming assembly.
        /// <para>
        /// M24/ERR-017-004: was an independent literal citing #17 in prose only — nothing bound it to
        /// #17's own declaration, so this catalogue and <c>MatchEngineConstants</c> (the producer) could
        /// drift from each other or from #17 with no compiler to catch it. Now a real mirror of
        /// <c>EventSystemConstants.CARD_KIND_YELLOW</c>, #17's own <c>[FIXED]</c> catalogue declaration
        /// (§3.10) — not designer-tunable, so <c>[GT]</c> would be the wrong tag there too.
        /// </para>
        /// </summary>
        public const byte CardKindYellow = EventSystemConstants.CARD_KIND_YELLOW;

        /// <summary>
        /// [CROSS] Card kind: a straight red — <c>CardIssuedEvent.CardKind</c> domain ordinal 1 (#17
        /// Appendix A row 0x06). Carries NO yellow (FR-DC-006). Single-consumer mirror of
        /// <c>EventSystemConstants.CARD_KIND_RED</c>, as <see cref="CardKindYellow"/>. M24/ERR-017-004.
        /// </summary>
        public const byte CardKindRed = EventSystemConstants.CARD_KIND_RED;

        /// <summary>
        /// [CROSS] Card kind: a second caution, promoted to a dismissal — <c>CardIssuedEvent.CardKind</c>
        /// domain ordinal 2 (#17 Appendix A row 0x06), and the engine emits it as <b>one</b> event, never
        /// a yellow-then-red pair — verified at <c>MatchEngine.ApplyCardAndCheckSentOff</c>, which
        /// returns the actual kind and publishes exactly one <c>CardIssuedEvent</c> (KD-5 / FR-DC-006).
        /// So a kind-2 is one yellow AND one dismissal ban; #44 must never synthesize the missing red.
        /// Single-consumer mirror of <c>EventSystemConstants.CARD_KIND_SECOND_YELLOW</c>, as
        /// <see cref="CardKindYellow"/>. M24/ERR-017-004.
        /// </summary>
        public const byte CardKindSecondYellow =
            EventSystemConstants.CARD_KIND_SECOND_YELLOW;

        #endregion

        #region GT

        /// <summary>
        /// [GT] Yellows accumulated (within one <c>CompetitionId</c>) before an accumulation ban is
        /// added and the count is reduced by this threshold, residual kept (§3.2, FR-DC-007).
        /// Config key <c>[discipline] YellowAccumulationThreshold</c>.
        /// <para>
        /// Illustrative pending the balance pass. Worth knowing where it sits: at the engine's last
        /// measured yellow rate a Stage-0 starter accrues yellows of the same order as this threshold
        /// over a managed season, i.e. the dial sits near the mean of the distribution it cuts — the
        /// most sensitive place it could sit. Do not read that as a reason to move it (KD-W1); read it
        /// as the reason the balance pass needs the per-player histogram, not the mean.
        /// </para>
        /// <para>Guarded <c>&gt;= 1</c> at <see cref="DisciplineRules"/>'s accumulation site: at 0 the
        /// residual subtraction never terminates the crossing and every yellow bans.</para>
        /// </summary>
        public static readonly int YellowAccumulationThreshold =
            Config.GetInt("discipline", "YellowAccumulationThreshold", 5);   // TODO: balance pass

        /// <summary>
        /// [GT] Ban length, in the club's own played fixtures, for crossing
        /// <see cref="YellowAccumulationThreshold"/> (§3.2).
        /// Config key <c>[discipline] AccumBanMatches</c>.
        /// </summary>
        public static readonly int AccumBanMatches =
            Config.GetInt("discipline", "AccumBanMatches", 1);   // TODO: balance pass

        /// <summary>
        /// [GT] Ban length for a kind-2 dismissal (a promoted second caution). Stacks additively with
        /// the accumulation ban the same event's yellow may itself trigger (§3.2's worked example).
        /// Config key <c>[discipline] SecondYellowBanMatches</c>.
        /// </summary>
        public static readonly int SecondYellowBanMatches =
            Config.GetInt("discipline", "SecondYellowBanMatches", 1);   // TODO: balance pass

        /// <summary>
        /// [GT] Ban length for a kind-1 straight red. Longer than a second caution by design — the
        /// offence class is worse — though #44 cannot yet tell violent conduct from a professional
        /// foul (the engine emits no offence data; #44 §7.2 defers offence classes).
        /// Config key <c>[discipline] StraightRedBanMatches</c>.
        /// </summary>
        public static readonly int StraightRedBanMatches =
            Config.GetInt("discipline", "StraightRedBanMatches", 2);   // TODO: balance pass

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0/T1, roadmap C1): Appendix A's     |
// |         |            |        | catalogue, plus DISCIPLINE_SAVE_MAGIC (ERR-044-001 — Appendix B  |
// |         |            |        | specified the block version-first, the ERR-029-005 defect's      |
// |         |            |        | fourth instance) and the three CardIssuedEvent kind ordinals.    |
// | 1.1     | 2026-08-13 | —      | AR round 5 fix (L17): CARD_KIND_YELLOW/RED/SECOND_YELLOW were    |
// |         |            |        | [FIXED] in the Fixed region but are #17 CardIssuedEvent.CardKind |
// |         |            |        | domain ordinals (Appendix A row 0x06) #44 consumes read-only and |
// |         |            |        | never sets independently — the root CLAUDE.md tag table makes    |
// |         |            |        | that [CROSS], not [FIXED]. Moved to a new Cross region (Fixed ->  |
// |         |            |        | Derived -> Cross -> GT per src/CLAUDE.md's region order) and      |
// |         |            |        | renamed CardKindYellow/CardKindRed/CardKindSecondYellow           |
// |         |            |        | (PascalCase, src/CLAUDE.md §3.2.3); every reference updated.      |
// | 1.2     | 2026-08-15 | —      | AR round 4 fixes (M24 + L18, ERR-017-004). M24: the three         |
// |         |            |        | CardKind* constants were [CROSS]-tagged but declared as           |
// |         |            |        | independent literals citing #17 in PROSE only — nothing bound     |
// |         |            |        | them to #17's own declaration, and MatchEngineConstants (the      |
// |         |            |        | producer) carried a second independent set under [FIXED]. #17 had |
// |         |            |        | no catalogue home for the encoding it owns. Fixed: EventSystem-   |
// |         |            |        | Constants.CARD_KIND_YELLOW/RED/SECOND_YELLOW declared once, as    |
// |         |            |        | [FIXED] ALL_CAPS not [GT] (§3.10 patched); this catalogue's own   |
// |         |            |        | [CROSS]/PascalCase constants now read them directly (new           |
// |         |            |        | `using TacticalDirector.EventSystem;`) instead of 0/1/2 literals.  |
// |         |            |        | No value change. L18: the class doc's region-                     |
// |         |            |        | order line still said "Fixed → GT", stale since v1.1 added the    |
// |         |            |        | Cross region — corrected to "Fixed → Cross → GT".                 |
#endregion
