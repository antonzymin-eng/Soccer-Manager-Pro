// File:     src/injuries-medical/InjuriesMedicalConstants.cs
// Created:  2026-08-05
// Modified: 2026-08-05
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
    /// #20): Fixed → Derived → Cross → GT. The <c>[GT]</c> magnitudes are illustrative pending the
    /// Stage-2/3 balance pass — the shapes and directions are the reviewed contract.
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

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] The keyed occurrence draw's output range is <c>[0, OccurrenceDrawDenom)</c>.
        /// Formula: <c>OccurrenceDrawDenom = InjuryRiskMax</c>. §3.1 / §3.4.
        /// Source constants: <see cref="InjuryRiskMax"/>.
        /// <para>
        /// Deriving it rather than declaring a second number is what lets §3.1 compare the assembled
        /// risk score against the draw with no scale factor between them. It is a property, not a
        /// <c>static readonly</c> field, because a field in this region would initialise BEFORE the
        /// <c>Cross</c>-region field it reads and silently capture 0.
        /// </para>
        /// </summary>
        public static int OccurrenceDrawDenom => InjuryRiskMax;

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
        /// [CROSS] The occurrence-risk clamp ceiling, and — through <see cref="OccurrenceDrawDenom"/> —
        /// the keyed draw's range.
        /// Authoritative source: <c>TrainingSystemConstants.InjuryRiskMax</c>. Training System #29
        /// Appendix A; consumed here per #41 §3.4.
        /// <para>
        /// <b>Mirrored, not re-declared.</b> §3.4 passes #29's <c>RiskScore</c> through with weight 1 and
        /// compares it directly against a draw in <c>[0, OccurrenceDrawDenom)</c>, so the two are one
        /// scale by contract. Appendix A tags this <c>[GT]</c> in #41's own catalogue, which would give
        /// it a second config key (<c>[injuries-medical] InjuryRiskMax</c>) independent of
        /// <c>[training-system] InjuryRiskMax</c> — and setting one without the other silently rescales
        /// every occurrence probability while #29's maximum risk quietly stops meaning "certain". That
        /// is the duplicate-truth trap the <c>[CROSS]</c> routing rule exists to prevent (the
        /// ERR-037-001 precedent). One owner, one key.
        /// </para>
        /// <para>
        /// The divergence from #41's Appendix A is filed and resolved as <b>ERR-041-003</b>
        /// (<c>docs/tracking/spec-error-log.md</c>), which carries the back-prop re-tagging that row
        /// <c>[CROSS]</c> at the spec's next revision. Do not "restore" the <c>[GT]</c> read here to
        /// match the current spec text — read the ERR entry first.
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

        /// <summary>[GT] Per-mille numerator classified <see cref="InjurySeverity.Moderate"/> (cumulative with Minor); the remainder is <see cref="InjurySeverity.Serious"/>. Minor + Moderate MUST be ≤ <see cref="SEVERITY_PERMILLE_DENOM"/> — a catalogue invariant. Config key [injuries-medical] SeverityModeratePermille.</summary>
        public static readonly int SeverityModeratePermille = Config.GetInt("injuries-medical", "SeverityModeratePermille", 300);

        /// <summary>[GT] Integer weight on #29's already-published <c>InjuryRiskContribution.RiskScore</c> in the risk assembly (§3.4). Config key [injuries-medical] TrainingRiskPassthroughWeight.</summary>
        public static readonly int TrainingRiskPassthroughWeight = Config.GetInt("injuries-medical", "TrainingRiskPassthroughWeight", 1);

        /// <summary>[GT] Risk contribution per <see cref="MatchLoad.AppearanceDays"/> — the Stage-2 match-load term. Config key [injuries-medical] AppearanceLoadWeight.</summary>
        public static readonly int AppearanceLoadWeight = Config.GetInt("injuries-medical", "AppearanceLoadWeight", 150);

        /// <summary>[GT] Risk contribution per <see cref="MatchLoad.HardContacts"/>. Zero at Stage 2 — the field is deep-tier only (KD-3), so a non-zero value is a config change rather than a formula rewrite. Config key [injuries-medical] HardContactWeight.</summary>
        public static readonly int HardContactWeight = Config.GetInt("injuries-medical", "HardContactWeight", 0);

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
#endregion
