// File:     src/injuries-medical/InjuriesMedicalConstants.cs
// Created:  2026-08-05
// Modified: 2026-08-24 (Round-2 M/L pass, M9 — v1.9)
// Author:   —
// Spec:     Injuries & Medical #41 Appendix A (constant catalogue) + §3.1–§3.4; Code Standards #20
// Purpose:  Every numeric constant for #41 occurrence, severity bucketing and recovery. No magic
//           literals in MedicalStep.

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TrainingSystem;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// Constant catalogue for Injuries &amp; Medical #41 (Appendix A). Region order (Code Standards
    /// #20): Fixed → Cross → GT. The <c>[GT]</c> magnitudes carry the balance pass's first-guess fit
    /// (ERR-041-011), measured by the season-scale instrument — the shapes and directions are the
    /// reviewed contract.
    /// <para>
    /// <b>Every value here is an integer</b> (FR-MD-014). Nothing in #41 is a float, which keeps the
    /// whole system clear of float-mode / MXCSR sensitivity — the reason the severity split is a
    /// per-mille numerator resolved by cross-multiply rather than a fraction.
    /// </para>
    /// <para>
    /// The two tables are array-valued, and <c>GameplayConfig</c> has no array getter, so they stay
    /// compile-time literals under the documented <c>src/CLAUDE.md</c> carve-out. They are reached
    /// through accessors so no caller can hold a mutable reference to the backing array.
    /// </para>
    /// </summary>
    public static class InjuriesMedicalConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] The #41 sub-blob's leading self-identifying tag — ASCII <c>"MEDL"</c>, written
        /// before <see cref="MEDICAL_SAVE_FORMAT_VERSION"/> (ERR-041-009).
        /// <para>
        /// <b>Why a magic and not just the version.</b> Every sub-blob format in the save stack is
        /// currently at version 1, so a version gate distinguishes one generation of the SAME format
        /// from the next — never one format from another. The #29 and #41 blocks are the acute case:
        /// their layouts are byte-for-byte the same shape, so each decodes the other's bytes cleanly
        /// and silently. The magic makes the block say which format it is rather than trusting the
        /// frame to have handed it to the right reader. Deliberately NOT
        /// <c>DOMAIN_TAG_INJURIES_MEDICAL</c>: that tag names an RNG domain, and reusing it here would
        /// tie a save-format identifier to a draw-keying concern that can change independently.
        /// </para>
        /// </summary>
        public const uint MEDICAL_SAVE_MAGIC = 0x4D45444C;   // 'M''E''D''L'

        /// <summary>[FIXED] The #41 sub-blob version (KD-7 / FR-MD-017). Gates the generation of the format identified by <see cref="MEDICAL_SAVE_MAGIC"/> — the magic says WHICH format, this says WHICH VERSION of it.</summary>
        public const uint MEDICAL_SAVE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] "Never advanced" seed for <see cref="InjuryState.LastAdvancedWorldDay"/>.
        /// <c>uint.MaxValue</c> and NOT 0, so a legitimate world-day 0 cannot collide with the
        /// fresh-state value (the day-0 trap, F6 — the #28/#29 lifecycle precedent).
        /// </summary>
        public const uint MEDICAL_NOT_ADVANCED_SENTINEL = uint.MaxValue;

        /// <summary>[FIXED] Per-mille identity for both <see cref="MedicalModifier"/> multipliers (= ×1.0). <c>default(MedicalModifier)</c> is all-zero and therefore NOT valid (FR-MD-016 / F4).</summary>
        public const int MEDICAL_MODIFIER_IDENTITY_PERMILLE = 1000;

        /// <summary>[FIXED] Denominator for the integer per-mille severity bucketing — §3.2 tests <c>draw × DENOM &lt; risk × numerator</c> so no float division is needed.</summary>
        public const int SEVERITY_PERMILLE_DENOM = 1000;

        /// <summary>[FIXED] The sole Stage-2 draw-purpose ordinal. APPEND-only (FR-MD-008): a deep-tier purpose takes the next ordinal and never renumbers this one.</summary>
        public const int DRAW_PURPOSE_OCCURRENCE = 0;

        /// <summary>
        /// [FIXED] The radix of <see cref="MedicalStep.DeriveActionOrdinal"/>'s
        /// <c>worldDay × RADIX + purpose</c> bijection (§3.1.1). It MUST stay constant across every
        /// version and MUST exceed the largest purpose ordinal ever defined.
        /// <para>
        /// Using the growing purpose <i>count</i> instead would shift every prior
        /// <c>(worldDay, Occurrence)</c> ordinal the moment a purpose was appended, silently changing
        /// every historical draw and breaking replay/save parity — a fixed radix is what makes the
        /// APPEND-only rule actually parity-safe (FR-MD-008).
        /// </para>
        /// </summary>
        public const int DRAW_PURPOSE_RADIX = 16;

        /// <summary>
        /// [FIXED] The keyed occurrence draw's range: uniform in <c>[0, OCCURRENCE_DRAW_DENOM)</c>, so
        /// the daily occurrence probability is <c>risk / OCCURRENCE_DRAW_DENOM</c> at a resolution of
        /// one per-million (§3.1 / §3.4, ERR-041-011).
        /// <para>
        /// <b>[FIXED], and deliberately DECOUPLED from the <c>[GT]</c> <see cref="InjuryRiskMax"/> it
        /// was originally derived from.</b> The draw is <c>hash % denominator</c>, so the denominator
        /// determines the VALUE of every draw, not merely the threshold it is compared against — a
        /// config-tunable denominator would mean one config edit re-rolls every career's injury luck,
        /// with the save recording nothing about which config produced it (#50's
        /// <c>SaveOriginStamp</c> is unbuilt). With it pinned, config edits move only thresholds; the
        /// draw sequence is stable. The catalogue invariant <c>InjuryRiskMax ≤ OCCURRENCE_DRAW_DENOM</c>
        /// keeps every probability ≤ 1 and is enforced fail-loud at the draw site.
        /// </para>
        /// </summary>
        public const int OCCURRENCE_DRAW_DENOM = 1_000_000;

#endregion

        #region Cross

        /// <summary>
        /// [CROSS] The #41 subsystem domain tag, folded into every occurrence-draw key so #41's draws
        /// are domain-separated from every other subsystem's.
        /// Authoritative source: <c>DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL</c>.
        /// Deterministic Simulation #16 §3.4 (ERR-041-001). Value: 0x2A.
        /// </summary>
        public static readonly byte DomainTagInjuriesMedical = DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL;

        /// <summary>
        /// [CROSS] The occurrence-risk clamp ceiling — the maximum <see cref="MedicalStep.AssembleRiskScore"/>
        /// can return, and therefore the daily probability ceiling
        /// <c>InjuryRiskMax / OCCURRENCE_DRAW_DENOM</c> (1.6% at today's values).
        /// Authoritative source: <c>TrainingSystemConstants.InjuryRiskMax</c>. Training System #29
        /// Appendix A; consumed here per #41 §3.4.
        /// <para>
        /// <b>Mirrored, not re-declared.</b> §3.4 passes #29's <c>RiskScore</c> through with weight 1,
        /// so producer and consumer clamp on one scale by contract; a second config key would let one
        /// side be set without the other (ERR-041-003, the ERR-037-001 precedent). One owner, one key.
        /// </para>
        /// <para>
        /// <b>No longer the draw's denominator</b> (ERR-041-011, the balance pass): the draw range is
        /// the <c>[FIXED]</c> <see cref="OCCURRENCE_DRAW_DENOM"/>, so tuning this ceiling moves the
        /// probability ceiling without re-rolling any career's draws. Invariant:
        /// <c>InjuryRiskMax ≤ OCCURRENCE_DRAW_DENOM</c>, enforced fail-loud at the draw site.
        /// </para>
        /// </summary>
        public static readonly int InjuryRiskMax = TrainingSystemConstants.InjuryRiskMax;

        #endregion

        #region GT

        /// <summary>[GT] Ceiling on <see cref="InjuryState.RecoveryRemaining"/> in world-days (F1 clamp). Generously bounds even a deep-tier recurrence-extended recovery. Config key [injuries-medical] RecoveryMax.</summary>
        public static readonly int RecoveryMax = Config.GetInt("injuries-medical", "RecoveryMax", 240);

        /// <summary>
        /// [GT] The Stage-2 linear recovery-countdown rate: a fixed integer number of recovery-days
        /// consumed per world day. Staff recovery-speed does NOT scale this per-tick — against a base
        /// of 1 an integer multiply would truncate every fractional rate to a no-op, so it scales the
        /// assigned tier-days once at injury time instead (§3.3 / FR-MD-014).
        /// Config key [injuries-medical] RecoveryDaysPerTickBase.
        /// </summary>
        public static readonly int RecoveryDaysPerTickBase = Config.GetInt("injuries-medical", "RecoveryDaysPerTickBase", 1);

        /// <summary>[GT] Per-mille numerator of the sub-threshold draw range classified <see cref="InjurySeverity.Minor"/> (§3.2). Config key [injuries-medical] SeverityMinorPermille.</summary>
        public static readonly int SeverityMinorPermille = Config.GetInt("injuries-medical", "SeverityMinorPermille", 600);

        /// <summary>[GT] Per-mille numerator classified <see cref="InjurySeverity.Moderate"/> (cumulative with Minor); the remainder is <see cref="InjurySeverity.Serious"/>. Minor + Moderate MUST be strictly &lt; <see cref="SEVERITY_PERMILLE_DENOM"/> — a catalogue invariant (at exactly 1000 the §3.2 second bucket saturates and Serious becomes unreachable). Config key [injuries-medical] SeverityModeratePermille.</summary>
        public static readonly int SeverityModeratePermille = Config.GetInt("injuries-medical", "SeverityModeratePermille", 300);

        /// <summary>[GT] Integer weight on #29's already-published <c>InjuryRiskContribution.RiskScore</c> in the risk assembly (§3.4). Config key [injuries-medical] TrainingRiskPassthroughWeight.</summary>
        public static readonly int TrainingRiskPassthroughWeight = Config.GetInt("injuries-medical", "TrainingRiskPassthroughWeight", 1);

        /// <summary>
        /// [GT] Risk contribution per <see cref="MatchLoad.AppearanceDays"/> — the Stage-2 match-load
        /// term, on the <see cref="OCCURRENCE_DRAW_DENOM"/> per-million scale (ERR-041-011). One
        /// appearance contributes for the whole <see cref="AppearanceWindowDays"/> window, so its
        /// cumulative per-match probability is ≈ <c>window × weight / OCCURRENCE_DRAW_DENOM</c>
        /// (≈ 3.9% at today's 7 × 5600 — first-guess, fitted so an ever-present starter carries
        /// ~1.5 match-driven injuries over a 38-round season, the E-1 match:training split).
        /// Config key [injuries-medical] AppearanceLoadWeight.
        /// </summary>
        public static readonly int AppearanceLoadWeight = Config.GetInt("injuries-medical", "AppearanceLoadWeight", 5600);

        /// <summary>
        /// [GT] The exposure-independent daily base risk added inside §3.4's sum, BEFORE the
        /// <c>OccurrenceRiskMillMult</c> scaling and BEFORE the clamp (position is normative;
        /// ERR-041-011 as corrected by ERR-041-021 — its original wording, "before the robustness
        /// mitigation, so robustness discriminates it", is arithmetically inert: the mitigation is
        /// SUBTRACTED and addition commutes), on the <see cref="OCCURRENCE_DRAW_DENOM"/> per-million scale. First-guess
        /// 4000 (0.4%/day gross, ~0.37% net of average mitigation) targets ~1 injury per season for a
        /// non-playing squad member — the training-ground floor that keeps the default Balanced focus
        /// from converging on an injury-proof player (the fifth AR pass's third measured absurdity).
        /// <para>
        /// <b>Note for the research-alignment supplement (its §10):</b> this is the exposure-
        /// independent term. R-2's under-exposure arm must RE-FIT against it rather than add beside
        /// it, or the left tail is priced three times (low-Condition + under-exposure + baseline).
        /// </para>
        /// Config key [injuries-medical] BaselineDailyRisk.
        /// </summary>
        public static readonly int BaselineDailyRisk = Config.GetInt("injuries-medical", "BaselineDailyRisk", 4000);

        /// <summary>
        /// [GT] The <see cref="MatchLoad.AppearanceDays"/> window in world-days (FR-MD-010 /
        /// ERR-041-010(b)): an appearance counts toward the occurrence risk for this many days AFTER
        /// the match, never including the current day (ERR-030-027 — the draw runs pre-round, so the
        /// window must not be able to contain a match not yet played). #41 owns this because it defines
        /// what the <c>MatchLoad</c> input MEANS; #30 owns the record that supplies it
        /// (<c>SeasonSave.AppearanceWindow</c>), which structurally bounds the value to <c>[1, 31]</c>
        /// (a u32 bitmask) and fails loud outside it. Config key [injuries-medical] AppearanceWindowDays.
        /// </summary>
        public static readonly int AppearanceWindowDays = Config.GetInt("injuries-medical", "AppearanceWindowDays", 7);

        /// <summary>[GT] Risk contribution per <see cref="MatchLoad.HardContacts"/>. Zero at Stage 2 — the field is deep-tier only (KD-3), so a non-zero value is a config change rather than a formula rewrite. Config key [injuries-medical] HardContactWeight.</summary>
        public static readonly int HardContactWeight = Config.GetInt("injuries-medical", "HardContactWeight", 0);

        /// <summary>
        /// [GT] The age at which the §3.4 age term contributes exactly zero (ERR-041-020) — the P5
        /// pivot, and the reason adding age to the assembly does not move the league aggregate.
        /// <para>
        /// 26 is the MEAN of the bootstrap roster's age distribution, which
        /// <c>RosterGenerator</c> draws uniformly on
        /// <c>[PlayerDatabaseConstants.AgeMin, AgeMax]</c> = <c>[17, 35]</c>. Because the term is
        /// linear and anti-symmetric about this pivot, the age contributions over that population sum
        /// to zero: the squad-wide injury rate is unchanged and only its DISTRIBUTION across the squad
        /// moves, which is the whole content of the finding. Deliberately NOT mirrored from #27 as a
        /// <c>[CROSS]</c> derivation — the pivot is a balance choice about where risk should be
        /// neutral, and pinning it to the generator's bounds would silently re-pivot #41 the day #47's
        /// authored database replaces them.
        /// </para>
        /// <para>
        /// <b>Deliberately the one dial of the three (with <see cref="AgeRiskPerYearFromPivot"/> and
        /// <see cref="AgeRiskSpan"/>) that carries NO runtime non-negativity guard</b>
        /// (agerisk-int-subtraction-and-both-dials). Those two are guarded because a negative value
        /// breaks a catalogue invariant — an inverted slope or a clamp whose min exceeds its max. No
        /// value of this pivot does that: every pivot, however extreme or mistyped, still produces a
        /// well-defined term once <see cref="MedicalStep.TestOnly_AgeRiskFor(int, int, int, int)"/>'s subtraction
        /// is evaluated in <c>long</c> (fixed alongside this note — the un-widened form could overflow
        /// and invert the term's sign at an extreme pivot). An unusually large pivot degenerates the
        /// term to a constant (e.g. a 260-for-26 typo saturates every player at
        /// <c>−AgeRiskSpan</c>), which is a balance-quality problem the season-scale instrument would
        /// surface, not a config-integrity failure this catalogue can fail loud on.
        /// </para>
        /// Config key [injuries-medical] AgeRiskPivotYears.
        /// </summary>
        public static readonly int AgeRiskPivotYears = Config.GetInt("injuries-medical", "AgeRiskPivotYears", 26);

        /// <summary>
        /// [GT] Risk contribution per year of age away from <see cref="AgeRiskPivotYears"/>, on the
        /// <see cref="OCCURRENCE_DRAW_DENOM"/> per-million scale (ERR-041-020). Positive above the
        /// pivot, negative below, linear throughout — there is no age THRESHOLD anywhere in the term
        /// (doctrine P1), which is what separates this from the age-band cliff its sibling #28 carried
        /// (ERR-028-020).
        /// <para>
        /// First-guess 150: a 34-year-old assembles +1200 against a 20-year-old's −900 on a typical
        /// ~6600 assembly, so the oldest squad member carries roughly a third more daily risk than the
        /// youngest, at a size that cannot dominate the exposure terms it sits beside. Real
        /// calibration waits for the complete-engine pass (P5 / KD-W1).
        /// </para>
        /// <para>
        /// <b>Only the VETERAN half of this term follows the evidence</b> (ERR-041-021). The
        /// research-alignment supplement's E-4 is rated <i>Strong</i> and is <b>U-shaped</b>:
        /// musculoskeletal maturity continues to ~24–25 and the 16–20 band carries ELEVATED risk at
        /// adult match intensity. A monotone term about pivot 26 makes exactly those players the
        /// safest in the league — a 19-year-old receives −1050. That inversion is deliberate and
        /// deliberately temporary: the U-shape is the supplement's R-1 design, it is awaiting owner
        /// sign-off, and re-shaping shipped football behaviour is the owner's call. R-1's surviving
        /// scope (back-prop <c>ERR-041-013</c>) is precisely the young-tail arm; its age-plumbing
        /// half landed here as ERR-041-020. That refit RE-SHAPES this term rather than adding beside
        /// it, exactly as its R-2 arm must against <see cref="BaselineDailyRisk"/>.
        /// </para>
        /// MUST be non-negative — a negative slope makes veterans the most durable players in the
        /// league, which is not an intent this system can express; enforced fail-loud at the
        /// computing site. Config key [injuries-medical] AgeRiskPerYearFromPivot.
        /// </summary>
        public static readonly int AgeRiskPerYearFromPivot = Config.GetInt("injuries-medical", "AgeRiskPerYearFromPivot", 150);

        /// <summary>
        /// [GT] Symmetric magnitude cap on the §3.4 age term, per-million (ERR-041-020) — it saturates
        /// this many points above and below zero, i.e. <c>AgeRiskSpan / AgeRiskPerYearFromPivot</c>
        /// years either side of the pivot: at 1800 and a slope of 150, ±12 years of the pivot, i.e.
        /// below 14 and above 38. Bounds the term for an out-of-range derived age (a career run far past
        /// any football span still assembles a finite risk).
        /// <para>
        /// <b>The plateau is REACHABLE inside the football-playing population today, and is
        /// KNOWINGLY accepted rather than out of range</b> (agerisk-span-plateau-reachable — the prior
        /// wording here claimed the cap "puts no cliff inside the football range", which is false on
        /// both halves: 36–40 is ordinary football, not "far past any football span", and 38+ is
        /// reachable in a live career because #28's retirement is FLAG-ONLY today —
        /// <c>ProgressionEngine.SquadFor</c> returns every carried record regardless of
        /// <c>RetirementFlag</c>, and roster removal (the actual departure) is the explicitly deferred
        /// half of roadmap D1. With ages advancing daily and no removal, every player eventually crosses
        /// 38 and stays on the roster, so a long-running career's whole veteran tail converges on the
        /// identical +1800 plateau and the term stops discriminating among them. Accepted, not fixed,
        /// until #28's retiree removal lands and bounds how old a rostered player can actually be — at
        /// which point this note should be revisited to confirm the plateau again sits outside the
        /// reachable range, or the span should be widened instead.</b>
        /// </para>
        /// <para>
        /// <b>Zero is the exact pre-fix identity</b> — the term vanishes for every player and
        /// <see cref="MedicalStep.AssembleRiskScore"/> reproduces its ERR-041-011 form byte-for-byte
        /// (the FR-MD-027 dial posture, locked both ways).
        /// </para>
        /// MUST be non-negative; enforced fail-loud at the computing site. Config key
        /// [injuries-medical] AgeRiskSpan.
        /// </summary>
        public static readonly int AgeRiskSpan = Config.GetInt("injuries-medical", "AgeRiskSpan", 1800);

        // [GT] Fixed recovery-days per severity tier (Appendix A), indexed by InjurySeverity ordinal.
        // Index 0 (None) is 0 by the F1 coherence invariant: a healthy player has no recovery
        // outstanding. TODO: replace with config loader (Stage 1) — array-valued, see the carve-out.
        private static readonly int[] s_recoveryDaysForTier = { 0, 7, 21, 60 };

        // [GT] Injury-risk mitigation indexed by the player's MEAN robustness attribute [0,20]
        // (Strength / Stamina / Balance). Linear at 400/14 per point, rounded — pinned so §3.6's
        // worked example (mean 14 ⇒ 400) is exact rather than approximately reproduced.
        // TODO: replace with config loader (Stage 1) — array-valued, see the carve-out.
        private static readonly int[] s_robustnessMitigationByMean =
        {
            0, 29, 57, 86, 114, 143, 171, 200, 229, 257,
            286, 314, 343, 371, 400, 429, 457, 486, 514, 543, 571,
        };

        #endregion

        /// <summary>The number of defined <see cref="InjurySeverity"/> ordinals — the length the tier table MUST have.</summary>
        public static int SeverityTierCount => s_recoveryDaysForTier.Length;

        /// <summary>The highest mean-robustness index the mitigation table covers (the #27 attribute ceiling).</summary>
        public static int RobustnessMeanMax => s_robustnessMitigationByMean.Length - 1;

        /// <summary>
        /// [GT] The fixed recovery-days for a severity tier (§3.2), before the staff recovery-speed
        /// scaling applied once at injury time (§3.1 step 2).
        /// </summary>
        /// <param name="severity">The assigned severity tier.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="severity"/> is not a defined ordinal — an out-of-contract severity fails
        /// loud rather than being clamped (F4).
        /// </exception>
        public static int RecoveryDaysFor(InjurySeverity severity)
        {
            int ordinal = (int)severity;
            if ((uint)ordinal >= (uint)s_recoveryDaysForTier.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Undefined InjurySeverity ordinal (F4).");
            }

            return s_recoveryDaysForTier[ordinal];
        }

        /// <summary>
        /// [GT] The deterministic risk mitigation for a mean robustness attribute value (§3.4 /
        /// FR-MD-015). Never RNG. Values outside the table are clamped to its ends — unlike an
        /// undefined enum, an out-of-range attribute mean is a magnitude, and the nearest defined
        /// mitigation is its correct reading rather than a bug.
        /// </summary>
        /// <param name="meanRobustness">The mean of the player's physical robustness attributes.</param>
        public static int RobustnessMitigationFor(int meanRobustness)
        {
            if (meanRobustness < 0)
            {
                return s_robustnessMitigationByMean[0];
            }

            if (meanRobustness > RobustnessMeanMax)
            {
                return s_robustnessMitigationByMean[RobustnessMeanMax];
            }

            return s_robustnessMitigationByMean[meanRobustness];
        }

        /// <summary>True iff <paramref name="severity"/> is a defined <see cref="InjurySeverity"/> ordinal (the F4 predicate).</summary>
        public static bool IsDefinedSeverity(InjurySeverity severity) => (uint)severity < (uint)s_recoveryDaysForTier.Length;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0): Appendix A catalogue.              |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (H): InjuryRiskMax re-tagged [GT] -> [CROSS], mirroring   |
// |         |            |        | TrainingSystemConstants rather than taking a second config key      |
// |         |            |        | ([injuries-medical] vs [training-system]) for one contract scale.   |
// |         |            |        | ERR-041-003.                                                       |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): the InjuryRiskMax doc asserted a spec back-prop      |
// |         |            |        | without naming the id that tracks it; the type doc credited the     |
// |         |            |        | per-mille split to a pre-commit review pass this file's history     |
// |         |            |        | does not record.                                                   |
// | 1.3     | 2026-08-07 | —      | Balance pass D2+D3 (ERR-041-010(b) / ERR-041-011). D2: +           |
// |         |            |        | AppearanceWindowDays [GT] (the FR-MD-010 window unit). D3: the     |
// |         |            |        | draw denominator becomes the [FIXED] OCCURRENCE_DRAW_DENOM =       |
// |         |            |        | 1,000,000, DECOUPLED from the [GT] InjuryRiskMax — a config-       |
// |         |            |        | tunable denominator re-rolls every career's draws, a pinned one    |
// |         |            |        | moves only thresholds (the Derived region retires with the         |
// |         |            |        | property). + BaselineDailyRisk [GT] 4000 (before mitigation,       |
// |         |            |        | position normative); AppearanceLoadWeight 150 -> 5600 on the new   |
// |         |            |        | per-million scale. InjuryRiskMax stays the (1%) probability        |
// |         |            |        | ceiling through the invariant InjuryRiskMax <= DENOM.              |
// | 1.4     | 2026-08-07 | —      | Balance-pass AR pass 1 (M3, doc side of the headroom fix): the     |
// |         |            |        | InjuryRiskMax mirror doc's probability ceiling 1% -> 1.6% —        |
// |         |            |        | TrainingSystemConstants (the [CROSS] source) raised 10000 -> 16000 |
// |         |            |        | to restore discrimination headroom, superseding v1.3's "(1%)"      |
// |         |            |        | description. Row added at AR pass 2 (the edit shipped rowless).    |
// | 1.5     | 2026-08-08 | —      | Balance-pass AR pass 9 (L5, doc): Minor + Moderate must be         |
// |         |            |        | STRICTLY below the denominator — at exactly 1000 the §3.2 second  |
// |         |            |        | bucket saturates and Serious is unreachable with "<=" satisfied.   |
// | 1.6     | 2026-08-22 | —      | ERR-041-020 (football-judgment proxy review, batch 1): + AgeRiskPivot-
// |         |            |        | Years, AgeRiskPerYearFromPivot and AgeRiskSpan for §3.4's age term. The
// |         |            |        | pivot is #27's bootstrap mean age, so the term sums to zero over that
// |         |            |        | population (P5); AgeRiskSpan = 0 is the exact pre-fix identity. TWO of
// |         |            |        | the three dials (AgeRiskPerYearFromPivot, AgeRiskSpan) are guarded
// |         |            |        | non-negative fail-loud at the computing site; AgeRiskPivotYears is not
// |         |            |        | (claim corrected at v1.8 — see AgeRiskPivotYears's own doc for why).
// | 1.7     | 2026-08-22 | —      | ERR-041-021 (AR over the ERR-041-020 landing, H4 + H7). Doc only, two
// |         |            |        | corrections, both annotating rather than editing published text.
// |         |            |        | H4: BaselineDailyRisk's "before the mitigation, so robustness
// |         |            |        | discriminates it" (rows 1.3 / ERR-041-011) is inert — the mitigation is
// |         |            |        | SUBTRACTED and addition commutes; restated as before the
// |         |            |        | OccurrenceRiskMillMult scaling and before the clamp, which is what the
// |         |            |        | position actually buys. H7: AgeRiskPerYearFromPivot no longer claims
// |         |            |        | the epidemiology supports the whole term. E-4 (Strong) is U-SHAPED —
// |         |            |        | the 16-20 band is elevated — so the monotone form follows it above the
// |         |            |        | pivot and INVERTS it below. Not re-shaped here: the U-shape is the
// |         |            |        | research supplement's R-1 design, awaiting owner sign-off, and R-1's
// |         |            |        | surviving scope under ERR-041-013 is exactly that young-tail arm.
// | 1.8     | 2026-08-23 | —      | Group-B AR findings. AgeRiskPivotYears's doc now states explicitly why
// |         |            |        | it is the one dial of the three carrying no runtime non-negativity
// |         |            |        | guard (no config-integrity invariant it can break, once AgeRiskFor's
// |         |            |        | subtraction is widened — see MedicalStep.cs v1.15); v1.6's "guarded
// |         |            |        | non-negative" claim corrected from "both dials" to the actual two of
// |         |            |        | three. AgeRiskSpan's doc corrected: the +/-12-year plateau IS reachable
// |         |            |        | by a live career while #28's retirement stays FLAG-ONLY (roster removal
// |         |            |        | deferred), so "no cliff inside the football range" was false on both
// |         |            |        | halves (36-40 is ordinary football; unremoved retirees eventually all
// |         |            |        | sit on the plateau) — recorded as knowingly accepted pending #28's
// |         |            |        | retiree removal, not re-derived as still out of range
// |         |            |        | (agerisk-span-plateau-reachable).
// | 1.9     | 2026-08-24 | —      | Round-2 M/L pass, M9. Doc-only: the AgeRiskPivotYears cref to the
// |         |            |        | parameterised overload updated to its renamed form,
// |         |            |        | MedicalStep.TestOnly_AgeRiskFor(int, int, int, int).
#endregion
