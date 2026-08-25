// File:     src/injuries-medical/MedicalStep.cs
// Created:  2026-08-05
// Modified: 2026-08-24 (Round-2 M/L pass, M9 — v1.16)
// Author:   —
// Spec:     Injuries & Medical #41 §3.1–§3.4 + Appendices A/B (FR-MD-003..016, FR-MD-023),
//           F1/F4/F6/F7; Code Standards #20
// Purpose:  The #41 world-day step — recovery countdown then the single keyed occurrence draw — plus
//           the pure risk assembly, severity bucketing and availability read.

using System;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// #41's algorithms (§3). All arithmetic is integer (FR-MD-014).
    /// <para>
    /// <b>There is exactly one stochastic surface</b>: the daily occurrence draw, and it is
    /// <i>keyed</i> on <c>(playerId, worldDay, purpose)</c> rather than advanced from a running cursor
    /// (KD-1). Two consequences fall out of that and are the reason the design is shaped this way:
    /// the same key reproduces the same draw no matter what order players or days were evaluated in,
    /// and there is <b>nothing to persist</b> for the stream — a save taken immediately after a draw
    /// carries no special state to lose (FR-MD-006/007).
    /// </para>
    /// <para>
    /// <b>The match tick never draws for #41</b> (FR-MD-005). This assembly references no match-engine
    /// type at all, so that is structural rather than a convention (FR-MD-011/026).
    /// </para>
    /// </summary>
    public static class MedicalStep
    {
        /// <summary>
        /// The daily world-day step (§3.1), invoked at #30's injuries slot: the recovery countdown
        /// FIRST, then the occurrence draw, then the idempotency cursor.
        /// <para>
        /// <b>The ordering guarantee (KD-6 / FR-MD-004).</b> The occurrence draw is gated on whether
        /// the player was healthy at call <i>entry</i> — captured before the countdown runs — not on
        /// the post-countdown state. So a player whose recovery completes on this very call cannot
        /// also be re-injured by it; they become eligible again on the NEXT call. Gating on the
        /// post-countdown state would let one call both heal and re-injure the same player, which
        /// reads to a user as an injury that never ended.
        /// </para>
        /// <para>
        /// <b>Idempotent per world day (F6)</b>, so a mid-recovery save → restore → re-run neither
        /// double-decrements nor re-draws. A <b>day gap fails loud</b> (F7 / FR-MD-021): #30 advances
        /// one world day at a time, and silently skipping days would under-advance recovery and drop
        /// an occurrence evaluation entirely.
        /// </para>
        /// </summary>
        /// <param name="state">
        /// The player's medical state, mutated in place. It MUST already exist — a player with no
        /// state is a roster-lifecycle bug (F2 / FR-MD-025), not something this step defaults, and the
        /// <c>ref</c> signature is what makes that the caller's contract rather than a silent insert.
        /// </param>
        /// <param name="playerId">The player's id — one of the three draw-key components.</param>
        /// <param name="attributes">The player's #27 attributes, read-only (the robustness term).</param>
        /// <param name="ageYears">The player's CURRENT age in whole years — #27's <c>PlayerRecord.Age</c>, which #28 keeps current as a derived cache (FR-PG-005). Feeds the §3.4 age term (ERR-041-020).</param>
        /// <param name="trainingRisk">#29's already-published risk scalar, read-only (FR-MD-009). #41 never touches #29's fatigue accumulator.</param>
        /// <param name="recentMatchLoad">Caller-supplied match participation (FR-MD-010); <see cref="MatchLoad.None"/> when there is none.</param>
        /// <param name="medical">The KD-5 staff seam; <see cref="MedicalModifier.Identity"/> until #34 lands.</param>
        /// <param name="worldDay">The world day being advanced.</param>
        /// <param name="worldSeed">The world seed the draw key is derived from — the career's seed, not a per-match one.</param>
        /// <param name="occurrenceEnabled">The KD-8 dial. Off reduces the step to recovery-only with no draw at all (FR-MD-027).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="state"/> carries an undefined <see cref="InjurySeverity"/> (F4); or
        /// <paramref name="ageYears"/> is negative (from <see cref="AgeRiskFor(int)"/>, reached only on
        /// an occurrence-eligible day — a derived age is never below zero, so a negative one is corrupt
        /// state, not a young player).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="state"/> is incoherent — recovery outstanding while healthy, none while
        /// injured, negative, or above the ceiling (F1); either <paramref name="medical"/> multiplier
        /// is non-positive (FR-MD-016 / F4); or <paramref name="worldDay"/> leaves a gap over the
        /// last-advanced day (F7) or is itself the never-advanced sentinel.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A catalogue/config integrity failure, not a caller error — one of the FIVE consuming-site
        /// guards fired: <see cref="InjuriesMedicalConstants.InjuryRiskMax"/> outside
        /// <c>(0, OCCURRENCE_DRAW_DENOM]</c> (from <see cref="DrawOccurrence"/>);
        /// <see cref="InjuriesMedicalConstants.RecoveryDaysPerTickBase"/> non-positive or
        /// <see cref="InjuriesMedicalConstants.RecoveryMax"/> below 1 (the countdown site); the
        /// severity split negative / summing past the denominator (from
        /// <see cref="ClassifySeverityFromDraw"/>); or
        /// <see cref="InjuriesMedicalConstants.AgeRiskPerYearFromPivot"/> /
        /// <see cref="InjuriesMedicalConstants.AgeRiskSpan"/> negative (from <see cref="AgeRiskFor(int)"/>).
        /// </exception>
        public static void AdvanceMedicalDay(
            ref InjuryState state,
            int playerId,
            in PlayerAttributes attributes,
            int ageYears,
            in InjuryRiskContribution trainingRisk,
            in MatchLoad recentMatchLoad,
            in MedicalModifier medical,
            uint worldDay,
            ulong worldSeed,
            bool occurrenceEnabled)
        {
            ValidateState(state);
            ValidateModifier(medical);

            if (worldDay == InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentException(
                    "worldDay must not be the never-advanced sentinel; storing it would re-arm the day-0 trap (F8).",
                    nameof(worldDay));
            }

            if (state.LastAdvancedWorldDay != InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL)
            {
                if (worldDay <= state.LastAdvancedWorldDay)
                {
                    return;   // already advanced — idempotent no-op (F6)
                }

                if (worldDay > state.LastAdvancedWorldDay + 1)
                {
                    throw new ArgumentException(
                        "world-day gap: #41 does not batch-replay skipped days (F7 / FR-MD-021).",
                        nameof(worldDay));
                }
            }

            // Captured BEFORE the countdown mutates anything — this is the KD-6 guarantee.
            bool wasAvailableAtEntry = IsAvailable(state);

            // 1. Recovery countdown — only while currently injured, a fixed integer decrement. Staff
            //    recovery-speed is deliberately NOT applied here: against a base of 1 an integer
            //    multiply truncates every fractional rate to a no-op, so it scales the assigned
            //    tier-days once at injury time instead (§3.3 / FR-MD-014).
            if (state.Severity != InjurySeverity.None)
            {
                // The recovery-rate invariant, enforced at the one site that counts down (AR pass 12
                // M3 — the DrawOccurrence guard posture, fourth instance): RecoveryDaysPerTickBase is
                // a [GT] config key and the catalogue lock only sees the fallback, so a shipped
                // config at 0 (or negative) would otherwise make EVERY injury permanent, silently —
                // the countdown never falls, Severity never returns to None, and the only symptom is
                // the depleted-squad back-fill quietly fielding whole squads.
                // (The RecoveryMax half of the pass-13 guard moved to AssignRecoveryDays at AR
                // pass 14 M1 — HERE it was provably dead: ValidateState has already refused any
                // injured state with RecoveryRemaining > RecoveryMax, and Severity != None forces
                // RecoveryRemaining >= 1, so RecoveryMax < 1 cannot reach this branch under ANY
                // config; the breach it names happens on the mutually exclusive draw branch.)
                if (InjuriesMedicalConstants.RecoveryDaysPerTickBase <= 0)
                {
                    throw new InvalidOperationException(
                        "RecoveryDaysPerTickBase must be positive — a non-positive decrement makes "
                        + "every injury permanent; catalogue/config integrity failure (§3.1, Appendix A).");
                }

                state.RecoveryRemaining = Clamp(
                    state.RecoveryRemaining - InjuriesMedicalConstants.RecoveryDaysPerTickBase,
                    0,
                    InjuriesMedicalConstants.RecoveryMax);

                if (state.RecoveryRemaining == 0)
                {
                    state.Severity = InjurySeverity.None;
                }
            }

            // 2. Occurrence draw — only for a player healthy at entry, and only when the dial is on.
            if (wasAvailableAtEntry && occurrenceEnabled)
            {
                int risk = AssembleRiskScore(trainingRisk, recentMatchLoad, attributes, ageYears, medical);
                ulong actionOrdinal = DeriveActionOrdinal(worldDay, InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE);
                int draw = DrawOccurrence(worldSeed, playerId, actionOrdinal);

                if (draw < risk)
                {
                    // The SAME draw that confirmed the occurrence also classifies it (§3.2) — Stage 2
                    // consumes exactly one draw per occurrence-eligible day, never two.
                    InjurySeverity severity = ClassifySeverityFromDraw(draw, risk);

                    // Fallible call FIRST, writes after (AR pass 15 M1): AssignRecoveryDays carries
                    // the RecoveryMax guard, and with Severity already written its throw would leave
                    // RecoveryRemaining == 0 beside a fresh severity IN THE LIVE CAREER — the exact
                    // breach being refused, surfacing a day later as a state-blaming refusal. A
                    // refused advance mutates nothing (the F7 standard); this branch was the one
                    // exception.
                    int recoveryDays = AssignRecoveryDays(severity, medical);

                    state.Severity = severity;
                    state.RecoveryRemaining = recoveryDays;
                    state.InjuryCount++;
                }
            }

            // 3. Advance the idempotency cursor.
            state.LastAdvancedWorldDay = worldDay;
        }

        /// <summary>
        /// The read-only availability view #30's squad selection reads (FR-MD-023): true iff the player
        /// carries no active injury. #41 owns no selection logic of its own — it answers this one
        /// question and #30 decides what to do about it.
        /// </summary>
        /// <param name="state">The player's medical state, read-only.</param>
        public static bool IsAvailable(in InjuryState state) => state.Severity == InjurySeverity.None;

        /// <summary>
        /// The occurrence-risk assembly (§3.4, as revised by ERR-041-011) — pure, integer, and clamped
        /// to <c>[0, InjuriesMedicalConstants.InjuryRiskMax]</c>. The draw is uniform in
        /// <c>[0, OCCURRENCE_DRAW_DENOM)</c> and §3.1 tests <c>draw &lt; risk</c>, so the result IS the
        /// daily probability numerator on the per-million scale, capped at the
        /// <c>InjuryRiskMax / OCCURRENCE_DRAW_DENOM</c> ceiling (1.6% at today's values).
        /// <para>
        /// The training term is #29's <b>already-published</b> scalar, read-only: #41 never reads or
        /// mutates #29's training-fatigue accumulator or the match engine's <c>AerobicPool</c>, so no
        /// counter is shared and a double count is not representable (KD-2 / FR-MD-009).
        /// </para>
        /// <para>
        /// <b><c>BaselineDailyRisk</c> and <c>AgeRiskFor</c> are both normatively positioned inside
        /// the sum, BEFORE the <c>OccurrenceRiskMillMult</c> scaling and BEFORE the clamp</b> (§3.4,
        /// as corrected by ERR-041-021). That is the whole of what their position buys, and both
        /// halves are load-bearing: before the scaling, the staff seam modulates them exactly as it
        /// modulates every other term rather than leaving two unmodulated islands in a scaled score;
        /// before the clamp, neither can lift the result past <c>InjuryRiskMax</c> and break the
        /// "every daily probability ≤ 1" invariant ERR-041-011 established at this ceiling.
        /// </para>
        /// <para>
        /// <b>Their position RELATIVE TO the mitigation is inert, and is no longer claimed.</b>
        /// <c>RobustnessMitigation</c> is SUBTRACTED and addition commutes, so moving either term to
        /// the far side of it changes no output for any input. ERR-041-011 and ERR-041-020 both
        /// asserted the opposite — "before the mitigation, so robustness discriminates it" — and the
        /// consequence is wrong in both readings: the age penalty is the same +1200 for a
        /// robustness-1, a robustness-14 and a robustness-20 player alike, and LARGER in relative
        /// terms for the more robust one, whose assembled score is smaller. The lock written to
        /// enforce the claim passed against a mutant that moved the term across the mitigation, which
        /// is how ERR-041-021 found it. What robustness genuinely does is lower every player's
        /// assembled score, including these two terms' contribution to it, wherever they sit in the
        /// sum.
        /// </para>
        /// <para>
        /// <c>BaselineDailyRisk</c> is additionally what keeps a fit player on the default focus from
        /// being injury-proof forever (the third absurdity the fifth AR pass measured); the age term
        /// exists because until ERR-041-020 this formula presented as multi-factor risk assembly
        /// while omitting one of the best-established real-world risk factors — one already carried
        /// on the <c>PlayerRecord</c> the caller was already resolving to read the attributes above.
        /// </para>
        /// </summary>
        /// <param name="trainingRisk">#29's risk contribution.</param>
        /// <param name="load">Caller-supplied match participation.</param>
        /// <param name="attributes">The player's #27 attributes (the robustness mitigation).</param>
        /// <param name="ageYears">The player's current age in whole years (the §3.4 age term, ERR-041-020).</param>
        /// <param name="medical">The staff seam; ×1.0 at <see cref="MedicalModifier.Identity"/>.</param>
        /// <exception cref="ArgumentException">Either <paramref name="medical"/> multiplier is non-positive — the <c>default(MedicalModifier)</c> trap, and the negative one that produces no crash to announce it (FR-MD-016 / F4).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ageYears"/> is negative — a derived age is never below zero (#28 §3.1.1 fails loud on a future-dated anchor before this is reached), so a negative one is corrupt state rather than a young player.</exception>
        /// <exception cref="InvalidOperationException">The <c>[GT]</c> age dials are negative (from <see cref="AgeRiskFor"/>) — see that method.</exception>
        public static int AssembleRiskScore(
            in InjuryRiskContribution trainingRisk,
            in MatchLoad load,
            in PlayerAttributes attributes,
            int ageYears,
            in MedicalModifier medical)
        {
            ValidateModifier(medical);

            long risk = (long)InjuriesMedicalConstants.TrainingRiskPassthroughWeight * trainingRisk.RiskScore
                        + (long)InjuriesMedicalConstants.AppearanceLoadWeight * load.AppearanceDays
                        + (long)InjuriesMedicalConstants.HardContactWeight * load.HardContacts
                        + InjuriesMedicalConstants.BaselineDailyRisk
                        + AgeRiskFor(ageYears)
                        - RobustnessMitigation(attributes);

            risk = risk * medical.OccurrenceRiskMillMult / InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE;

            return ClampLong(risk, 0, InjuriesMedicalConstants.InjuryRiskMax);
        }

        /// <summary>
        /// Buckets a confirmed occurrence into a Stage-2 severity tier (§3.2). This is <b>not</b> a
        /// second RNG draw — it re-reads the same draw value that confirmed the occurrence, by fixed
        /// per-mille proportions of the sub-threshold range.
        /// <para>
        /// The comparison is an integer cross-multiply (<c>draw × DENOM &lt; risk × numerator</c>)
        /// rather than a division, so no float and no rounding mode enters the classification. The
        /// products are widened to <c>long</c>: both operands are bounded by
        /// <c>InjuryRiskMax</c>, so the product fits comfortably in <c>int</c> at today's values, but
        /// a raised <c>[GT]</c> ceiling must not silently start overflowing.
        /// </para>
        /// </summary>
        /// <param name="draw">The occurrence draw, which MUST already be below <paramref name="risk"/>.</param>
        /// <param name="risk">The assembled risk score the draw was compared against.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="draw"/> is not below <paramref name="risk"/> — no occurrence was confirmed,
        /// so there is no tier to classify. Without this the method answers <c>Serious</c> for any
        /// draw at <c>risk == 0</c>, which is a plausible-looking wrong answer rather than a refusal.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <c>[GT]</c> severity numerators sum to <see cref="InjuriesMedicalConstants.SEVERITY_PERMILLE_DENOM"/>
        /// or past it — the <c>Serious</c> tier would be unreachable; a catalogue/config integrity
        /// failure rather than a bad argument (the <see cref="DrawOccurrence"/> guard posture).
        /// </exception>
        public static InjurySeverity ClassifySeverityFromDraw(int draw, int risk)
        {
            if (draw >= risk)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(draw), draw, "Severity is classified only for a CONFIRMED occurrence (draw < risk, §3.2).");
            }

            // The split invariant, enforced at the one site that classifies (the DrawOccurrence
            // denominator-guard posture, AR pass 10 M1; completed AR pass 11 L3): both numerators
            // are [GT] config-tunable and the catalogue suite only ever sees the fallbacks (the gate
            // runs config-unbound — ERR-041-003's class), so a shipped config summing to the
            // denominator or past it would otherwise delete the Serious tier silently — at a sum of
            // exactly 1000 the second bucket's bound IS this method's own precondition. Strict, per
            // Appendix A. The runtime guard mirrors ALL of the design-time lock's predicates, with
            // positivity relaxed to non-negativity — a zero tier is an expressible config intent,
            // but a NEGATIVE numerator makes its whole tier unreachable through the same silent
            // mechanism the sum guard exists to stop (Minor at -100 reads as a 0/20/80 split).
            if (InjuriesMedicalConstants.SeverityMinorPermille < 0
                || InjuriesMedicalConstants.SeverityModeratePermille < 0)
            {
                throw new InvalidOperationException(
                    "SeverityMinorPermille and SeverityModeratePermille must be non-negative — a "
                    + "negative numerator silently deletes its tier; catalogue/config integrity "
                    + "failure (§3.2, Appendix A).");
            }

            if (InjuriesMedicalConstants.SeverityMinorPermille
                + InjuriesMedicalConstants.SeverityModeratePermille
                >= InjuriesMedicalConstants.SEVERITY_PERMILLE_DENOM)
            {
                throw new InvalidOperationException(
                    "SeverityMinorPermille + SeverityModeratePermille must be strictly below "
                    + "SEVERITY_PERMILLE_DENOM — at or above it the Serious tier is unreachable; "
                    + "catalogue/config integrity failure (§3.2, Appendix A).");
            }

            long scaledDraw = (long)draw * InjuriesMedicalConstants.SEVERITY_PERMILLE_DENOM;

            if (scaledDraw < (long)risk * InjuriesMedicalConstants.SeverityMinorPermille)
            {
                return InjurySeverity.Minor;
            }

            long throughModerate = (long)risk
                                   * (InjuriesMedicalConstants.SeverityMinorPermille
                                      + InjuriesMedicalConstants.SeverityModeratePermille);

            return scaledDraw < throughModerate ? InjurySeverity.Moderate : InjurySeverity.Serious;
        }

        /// <summary>
        /// The <c>(worldDay, purpose) → u64</c> bijection that keys a draw (§3.1.1). A pure function,
        /// <b>not</b> an incrementing counter: the same pair always resolves to the same ordinal
        /// regardless of call order, which is what makes the stream position-independent and leaves it
        /// with nothing to persist.
        /// </summary>
        /// <param name="worldDay">The world day.</param>
        /// <param name="purpose">The draw-purpose ordinal; MUST be below <see cref="InjuriesMedicalConstants.DRAW_PURPOSE_RADIX"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The purpose is outside the fixed radix — appending one beyond it would collide with the next day's ordinals.</exception>
        public static ulong DeriveActionOrdinal(uint worldDay, int purpose)
        {
            if ((uint)purpose >= (uint)InjuriesMedicalConstants.DRAW_PURPOSE_RADIX)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purpose), purpose, "Draw purpose must stay below DRAW_PURPOSE_RADIX (§3.1.1).");
            }

            return (ulong)worldDay * (ulong)InjuriesMedicalConstants.DRAW_PURPOSE_RADIX + (ulong)purpose;
        }

        /// <summary>
        /// The keyed occurrence draw: a uniform in <c>[0, OCCURRENCE_DRAW_DENOM)</c> derived from
        /// <c>(worldSeed, playerId, actionOrdinal)</c> with the #41 domain tag folded in first, so
        /// #41's draws are domain-separated from every other subsystem's.
        /// <para>
        /// Each key component passes through a full SplitMix64 finalizer, so one-player-id or one-day
        /// difference avalanches — adjacent players must not share correlated injury luck.
        /// </para>
        /// <para>
        /// This is a <b>local keyed derivation</b>, not a registered <c>DeterministicRngService</c>
        /// stream, and that is the point: a registered stream carries a cursor, and a cursor is exactly
        /// what FR-MD-007 forbids persisting. It follows the #30 <c>RoundResolutionModel.FixtureKey</c>
        /// and <c>LeagueBootstrap</c> precedent (see ERR-041-002 for why §3.1's <c>rng.DrawKeyed</c>
        /// call could not be taken literally — #16 exposes no keyed-draw API).
        /// </para>
        /// </summary>
        /// <param name="worldSeed">The career's world seed.</param>
        /// <param name="playerId">The player being evaluated.</param>
        /// <param name="actionOrdinal">The <c>(worldDay, purpose)</c> ordinal from <see cref="DeriveActionOrdinal"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// The <c>[GT]</c> <see cref="InjuriesMedicalConstants.InjuryRiskMax"/> ceiling exceeds
        /// <see cref="InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM"/> — a catalogue/config integrity
        /// failure rather than a bad argument (ERR-041-011: the invariant that keeps every daily
        /// probability ≤ 1, checked at the one site that draws).
        /// </exception>
        internal static int DrawOccurrence(ulong worldSeed, int playerId, ulong actionOrdinal)
        {
            // The denominator is [FIXED] and positive by construction (ERR-041-011 retired the
            // [GT]-derived form whose negative-config trap the old guard existed for). What a config
            // CAN still break are the two invariants on the [GT] ceiling (AR pass 13 M1 widened the
            // guard to both sides): raised past the denominator, a clamped risk means "certain and
            // then some"; at zero or negative, every score clamps to 0 and the ARMED dial injures
            // nobody, forever, silently. One comparison pair at the one drawing site.
            if (InjuriesMedicalConstants.InjuryRiskMax <= 0
                || InjuriesMedicalConstants.InjuryRiskMax > InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM)
            {
                throw new InvalidOperationException(
                    "InjuryRiskMax must be positive and no greater than OCCURRENCE_DRAW_DENOM — " +
                    "non-positive, the armed dial injures nobody forever; past the denominator, the " +
                    "probability ceiling passes 1; catalogue/config integrity failure (ERR-041-011, §3.4).");
            }

            ulong h = Mix((ulong)InjuriesMedicalConstants.DomainTagInjuriesMedical ^ worldSeed);
            h = Mix(h ^ (ulong)(uint)playerId);
            h = Mix(h ^ actionOrdinal);

            return (int)(h % (ulong)InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM);
        }

        /// <summary>
        /// The deterministic own-attribute robustness mitigation (§3.4 / FR-MD-015) — never RNG. Stage
        /// 2 derives it from the three existing physical attributes; a dedicated #27
        /// <c>InjuryProneness</c> field is a recorded deep-tier deferral, deliberately not built here
        /// so the minimal tier causes no #27 schema ripple.
        /// <para>
        /// <b>This is the SECOND mitigation over those same three attributes.</b> #29's
        /// <c>TrainingStep.ComputeInjuryRisk</c> has already subtracted its own before clamping and
        /// publishing the scalar <see cref="AssembleRiskScore"/> passes through, so a robust player is
        /// priced down twice and the two <c>[GT]</c> tables cannot be tuned independently. Both specs
        /// mandate their term, so this is the contract rather than a defect — but it has a consequence
        /// worth knowing before tuning: because #27 attributes floor at 1, this term is never zero.
        /// It no longer keeps the worst case below the clamp, though — since the balance pass's AR
        /// raised the ceiling for discrimination headroom, a worst-case assembly (saturated #29 risk
        /// + baseline + a full appearance window) SATURATES at the
        /// <see cref="InjuriesMedicalConstants.InjuryRiskMax"/> clamp — and the clamp itself sits at
        /// 1.6% of <see cref="InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM"/> — so no player is ever
        /// remotely certain to be injured. Recorded under ERR-041-003; scale decoupled at ERR-041-011.
        /// </para>
        /// </summary>
        /// <param name="attributes">The player's #27 attributes.</param>
        internal static int RobustnessMitigation(in PlayerAttributes attributes)
        {
            int mean = (attributes.Strength + attributes.Stamina + attributes.Balance) / 3;
            return InjuriesMedicalConstants.RobustnessMitigationFor(mean);
        }

        /// <summary>
        /// The deterministic age term of §3.4's assembly (ERR-041-020) — linear in age, anti-symmetric
        /// about <see cref="InjuriesMedicalConstants.AgeRiskPivotYears"/>, saturating at
        /// ±<see cref="InjuriesMedicalConstants.AgeRiskSpan"/>. Never RNG, and never a threshold: every
        /// year of age moves the term by the same amount, so there is no age at which a player's risk
        /// steps (doctrine P1).
        /// <para>
        /// <b>Granularity, stated rather than glossed.</b> The input is whole years, because whole
        /// years is what #27 exposes — <c>PlayerRecord.Age</c>, kept current by #28's derived cache. A
        /// uniform one-year increment is not the defect this fix addresses: the pattern-(b) shape is a
        /// judgment collapsed onto ONE cutoff, and there is no cutoff here. Should a day-resolution age
        /// ever be wanted, #28's <c>BirthWorldDay</c> is its source and #41 would take days instead —
        /// but reaching for it now would mean #41 reading a #28 field for a term whose slope is a
        /// first-guess <c>[GT]</c>.
        /// </para>
        /// </summary>
        /// <param name="ageYears">The player's current age in whole years.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ageYears"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="InjuriesMedicalConstants.AgeRiskPerYearFromPivot"/> or
        /// <see cref="InjuriesMedicalConstants.AgeRiskSpan"/> is negative — a catalogue/config
        /// integrity failure rather than a caller error, checked at the one site that computes the term
        /// (the <see cref="DrawOccurrence"/> guard posture). A negative slope inverts the whole finding
        /// this term exists to fix, silently; a negative span makes the clamp's min exceed its max, so
        /// every player takes the maximum penalty regardless of age.
        /// </exception>
        /// <remarks>
        /// <c>internal</c>, not <c>public</c> (M9, round-2 AR): no cross-assembly caller exists —
        /// verified by grep over <c>src/</c> — and its twin term <see cref="RobustnessMitigation"/>,
        /// same sum, same file, is <c>internal</c> too. <see cref="AssembleRiskScore"/> is the sole
        /// production caller of both.
        /// </remarks>
        internal static int AgeRiskFor(int ageYears)
        {
            return TestOnly_AgeRiskFor(
                ageYears,
                InjuriesMedicalConstants.AgeRiskPivotYears,
                InjuriesMedicalConstants.AgeRiskPerYearFromPivot,
                InjuriesMedicalConstants.AgeRiskSpan);
        }

        /// <summary>
        /// <see cref="AgeRiskFor(int)"/> against explicit dials, so the <c>span = 0</c> pre-fix
        /// identity can be EXERCISED rather than asserted in prose. The catalogue values are
        /// <c>[GT]</c>s read once at static initialisation, so a test cannot vary them any other way —
        /// and this project's standing lesson is that an identity claim nothing executes is exactly the
        /// class of claim that gets falsified on first run (the ERR-008-021/-022 chain, three times).
        /// <para>
        /// <b>Named <c>TestOnly_</c>, not overloaded on <see cref="AgeRiskFor(int)"/> (M9, round-2
        /// AR).</b> Argument count alone did not mark this as a test affordance rather than a
        /// legitimate production call pinning the dials off — the house convention this repo already
        /// uses elsewhere (<c>MatchEngine.cs</c>'s ~40 <c>TestOnly_*</c> members;
        /// <c>agent-movement</c>'s <c>ToolingOverrideOnly_NaNInjection</c>). Not an overload also
        /// removes the resolution hazard where a future parameter added to <see cref="AgeRiskFor(int)"/>
        /// would silently rebind existing calls to this one.
        /// </para>
        /// </summary>
        /// <param name="ageYears">The player's current age in whole years.</param>
        /// <param name="pivotYears">
        /// The age at which the term is zero. Deliberately UNGUARDED here (agerisk-int-subtraction-and-
        /// both-dials): unlike <paramref name="perYear"/>/<paramref name="span"/>, no value of this
        /// dial breaks a catalogue/config invariant — every pivot, including an extreme or mistyped one,
        /// still produces a well-defined (if degenerate) term once the subtraction below is widened.
        /// </param>
        /// <param name="perYear">Risk contribution (per-million scale) per year away from the pivot.</param>
        /// <param name="span">Symmetric saturation magnitude.</param>
        internal static int TestOnly_AgeRiskFor(int ageYears, int pivotYears, int perYear, int span)
        {
            if (ageYears < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ageYears), ageYears, "A player's derived age is never negative (§3.4).");
            }

            if (perYear < 0 || span < 0)
            {
                throw new InvalidOperationException(
                    "AgeRiskPerYearFromPivot and AgeRiskSpan must be non-negative — a negative slope "
                    + "makes veterans the least injury-prone players in the league and a negative span "
                    + "inverts the clamp; catalogue/config integrity failure (§3.4, Appendix A).");
            }

            // Widened on BOTH operands (the ClassifySeverityFromDraw widening-comment standard,
            // agerisk-int-subtraction-and-both-dials): with only the product widened, a sufficiently
            // negative pivotYears overflows the int subtraction below before the cast ever runs — e.g.
            // ageYears=26, pivotYears=int.MinValue computes 26 - int.MinValue in int arithmetic, which
            // wraps to a large NEGATIVE value and inverts the term's sign league-wide. Widening the
            // subtraction itself removes the wrap for any int-range pivot.
            long term = (long)perYear * ((long)ageYears - pivotYears);

            return ClampLong(term, -span, span);
        }

        /// <summary>
        /// Assigns the recovery-days for a fresh injury (§3.1 step 2): the tier's fixed days, scaled
        /// ONCE by the staff recovery-speed, floored at 1.
        /// <para>
        /// The floor is load-bearing, not defensive politeness: an aggressive multiplier could divide
        /// the assigned days to 0, which would leave <c>RecoveryRemaining == 0</c> while
        /// <c>Severity != None</c> — a direct F1 coherence breach written into the save.
        /// </para>
        /// </summary>
        /// <param name="severity">The assigned severity tier; MUST NOT be <see cref="InjurySeverity.None"/>.</param>
        /// <param name="medical">The staff seam.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="severity"/> is <see cref="InjurySeverity.None"/>. The floor below would turn
        /// the None tier's 0 recovery-days into 1, inverting the very invariant it exists to protect:
        /// <c>RecoveryRemaining == 1</c> with <c>Severity == None</c> is an F1 breach in the other
        /// direction. Today the only caller passes a tier confirmed by
        /// <see cref="ClassifySeverityFromDraw"/>, which never returns None; this keeps that true for
        /// the next caller.
        /// </exception>
        internal static int AssignRecoveryDays(InjurySeverity severity, in MedicalModifier medical)
        {
            if (severity == InjurySeverity.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(severity), severity, "Recovery days are assigned only for a confirmed injury (F1).");
            }

            // The RecoveryMax >= 1 invariant, enforced at the one site whose clamp could breach it
            // (AR pass 14 M1 — pass 13 placed this on the countdown branch, where ValidateState
            // makes it unsatisfiable; with RecoveryMax < 1, ClampLong's value > max arm would
            // return RecoveryMax, an F1-breaching assignment). The caller sequences this call
            // BEFORE any state write (AR pass 15 M1), so the refusal leaves the career untouched
            // rather than half-injured — prevention is the ORDERING's property, not this guard's;
            // the guard alone only made the breach loud.
            if (InjuriesMedicalConstants.RecoveryMax < 1)
            {
                throw new InvalidOperationException(
                    "RecoveryMax must be at least 1 — below it the assignment clamp would produce "
                    + "RecoveryRemaining == 0 for a confirmed injury; catalogue/config integrity "
                    + "failure (§3.3, Appendix A).");
            }

            long scaled = (long)InjuriesMedicalConstants.RecoveryDaysFor(severity)
                          * InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE
                          / medical.RecoverySpeedMillMult;

            return ClampLong(scaled, 1, InjuriesMedicalConstants.RecoveryMax);
        }

        /// <summary>
        /// The F1/F4 entry gate: the severity must be a defined ordinal, and recovery-remaining must be
        /// positive exactly when the player is injured. An invalid combination is a bug — repairing it
        /// silently would hide whichever writer produced it.
        /// </summary>
        /// <param name="state">The state to check.</param>
        private static void ValidateState(in InjuryState state)
        {
            if (!InjuriesMedicalConstants.IsDefinedSeverity(state.Severity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state), state.Severity, "Undefined InjurySeverity on the advancing state (F4).");
            }

            if (state.RecoveryRemaining < 0)
            {
                throw new ArgumentException(
                    "InjuryState coherence violated: RecoveryRemaining is negative (F1). The iff check "
                    + "below cannot catch this — a negative counter reads as 'not recovering', which "
                    + "matches a healthy player exactly.",
                    nameof(state));
            }

            bool injured = state.Severity != InjurySeverity.None;
            if (injured != (state.RecoveryRemaining > 0))
            {
                throw new ArgumentException(
                    "InjuryState coherence violated: RecoveryRemaining > 0 iff Severity != None (F1).",
                    nameof(state));
            }

            if (state.RecoveryRemaining > InjuriesMedicalConstants.RecoveryMax)
            {
                throw new ArgumentException(
                    "InjuryState coherence violated: RecoveryRemaining exceeds RecoveryMax (F1).",
                    nameof(state));
            }
        }

        /// <summary>
        /// The FR-MD-016 zero-value gate, widened to non-positive on both fields.
        /// <para>
        /// <c>default(MedicalModifier)</c> is all-zero, which means ×0 occurrence risk and a
        /// divide-by-zero recovery-days scale. <b>A negative multiplier is the same trap with the same
        /// consequence and no crash to announce it:</b> a negative recovery speed makes the assigned
        /// days negative, which the floor turns into a one-day Serious injury; a negative or zero
        /// occurrence-risk multiplier drives the assembled risk below zero, which the clamp turns into
        /// "nobody is ever injured again". #34 is the declared future producer of these values, so a
        /// sign error there would ship a game with no injuries and a green suite. Neither is a value
        /// this system can represent an intent for — an injury-proof squad is a risk input, not a ×0
        /// staff multiplier — so both fail loud.
        /// </para>
        /// </summary>
        /// <param name="medical">The modifier to check.</param>
        private static void ValidateModifier(in MedicalModifier medical)
        {
            if (medical.OccurrenceRiskMillMult <= 0 || medical.RecoverySpeedMillMult <= 0)
            {
                throw new ArgumentException(
                    "MedicalModifier multipliers must both be positive per-mille values; use "
                    + "MedicalModifier.Identity, never default(MedicalModifier) (FR-MD-016 / F4).",
                    nameof(medical));
            }
        }

        /// <summary>SplitMix64's finalizing mix — the key-derivation step function.</summary>
        /// <remarks>
        /// A local copy, matching this project's accepted norm for keyed derivations across assemblies
        /// (<c>RoundResolutionModel</c>, <c>LeagueBootstrap</c>, <c>PlayerGenerationRng</c>): there is
        /// no shared helper on <c>deterministic-sim</c> to call. The constants are SplitMix64's, so a
        /// future shared helper is a drop-in replacement rather than a behaviour change.
        /// </remarks>
        /// <param name="value">The value to mix.</param>
        internal static ulong Mix(ulong value)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                ulong z = value + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Integer clamp to <c>[min, max]</c>.</summary>
        internal static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        /// <summary>Widened clamp to <c>[min, max]</c>, narrowing to <c>int</c> — the risk assembly and the recovery scaling both produce <c>long</c> intermediates.</summary>
        internal static int ClampLong(long value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : (int)value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0): §3.1–§3.4 + the keyed draw.        |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (2M): ValidateModifier widened to non-positive on BOTH    |
// |         |            |        | fields (a negative multiplier silently disabled injuries or         |
// |         |            |        | one-dayed a Serious one); ValidateState now rejects a negative      |
// |         |            |        | RecoveryRemaining, which the iff check structurally could not see.  |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): the two <exception> docs still described the v1.0    |
// |         |            |        | gate ("a zero recovery-speed multiplier") after v1.1 widened it to  |
// |         |            |        | non-positive on both fields — a caller reading them would believe a |
// |         |            |        | negative occurrence multiplier was accepted. RobustnessMitigation   |
// |         |            |        | now states that it is the second term over #29's attributes.        |
// | 1.3     | 2026-08-05 | —      | AR pass 5 (2L): DrawOccurrence refuses a non-positive denominator   |
// |         |            |        | (zero threw on its own; a NEGATIVE ceiling did not — the ulong cast |
// |         |            |        | made the modulo a no-op and yielded a signed garbage draw that      |
// |         |            |        | still classified). AssignRecoveryDays refuses the None tier, whose  |
// |         |            |        | 0 days the F1 floor would have raised to 1 against Severity None.   |
// |         |            |        | Also repairs a REGRESSION shipped in v1.2: that pass appended a     |
// |         |            |        | <para> to RobustnessMitigation's doc and dropped the closing        |
// |         |            |        | </summary>, leaving malformed XML (CS1570 under a doc-file build).  |
// | 1.4     | 2026-08-07 | —      | Balance pass D3 (ERR-041-011): AssembleRiskScore gains the         |
// |         |            |        | BaselineDailyRisk term BEFORE the mitigation (position normative);  |
// |         |            |        | the draw reduces into the [FIXED] OCCURRENCE_DRAW_DENOM instead of  |
// |         |            |        | the [GT]-derived OccurrenceDrawDenom, and the draw-site guard      |
// |         |            |        | becomes the new invariant InjuryRiskMax <= DENOM (the old negative- |
// |         |            |        | denominator trap is unrepresentable against a const).              |
// | 1.5     | 2026-08-07 | —      | Balance-pass AR passes 1+2 (doc only): the RobustnessMitigation     |
// |         |            |        | tuning note claimed the worst case "lands strictly below the       |
// |         |            |        | InjuryRiskMax clamp" at "1%" — false since the M3 headroom raise   |
// |         |            |        | (the worst-case assembly now SATURATES the clamp; ceiling 1.6%).   |
// |         |            |        | Pass 1's one-line doc edit here also shipped rowless; both under   |
// |         |            |        | this row.                                                          |
// | 1.6     | 2026-08-08 | —      | Balance-pass AR pass 9 (L4, message only): the sentinel-as-        |
// |         |            |        | worldDay refusal had NO normative source — #41 SS2.3 gains F8 and  |
// |         |            |        | SS3.1's pseudocode the guard line; the message cites F8, not F6.   |
// | 1.7     | 2026-08-08 | —      | Balance-pass AR pass 10 (M1): the severity-split invariant gains   |
// |         |            |        | its RUNTIME half — ClassifySeverityFromDraw fail-louds when the    |
// |         |            |        | [GT] numerators sum to the denominator or past it (the             |
// |         |            |        | DrawOccurrence guard posture; the catalogue lock only sees the     |
// |         |            |        | fallbacks, ERR-041-003's class).                                   |
// | 1.8     | 2026-08-08 | —      | Balance-pass AR pass 11 (L3): the runtime guard mirrors ALL the    |
// |         |            |        | design-time lock's predicates — a NEGATIVE [GT] numerator passed   |
// |         |            |        | the sum guard and silently deleted its own tier (Minor at -100 =   |
// |         |            |        | a 0/20/80 split); positivity relaxed to non-negativity, a zero     |
// |         |            |        | tier being an expressible intent.                                  |
// | 1.9     | 2026-08-08 | —      | Balance-pass AR pass 12 (M3): RecoveryDaysPerTickBase gains its    |
// |         |            |        | runtime guard at the countdown site — the one [GT] in the landing |
// |         |            |        | whose lock had no runtime mirror; at 0 every injury was permanent |
// |         |            |        | silently (the DrawOccurrence posture, fourth instance).            |
// | 1.10    | 2026-08-08 | —      | Balance-pass AR pass 13 (M1 + L6): the guard class COMPLETED —     |
// |         |            |        | RecoveryMax < 1 joins the countdown guard (a degenerate clamp     |
// |         |            |        | wrote RecoveryRemaining == 0 while injured — the F1 breach the    |
// |         |            |        | floor's own doc names) and DrawOccurrence refuses a non-positive  |
// |         |            |        | ceiling (armed dial injures nobody, forever, silently); the       |
// |         |            |        | entry point's exception doc names the four real guards, not the   |
// |         |            |        | retired denominator one.                                          |
// | 1.11    | 2026-08-08 | —      | Balance-pass AR pass 14 (M1): the pass-13 RecoveryMax guard was    |
// |         |            |        | PROVABLY DEAD where it sat — ValidateState makes RecoveryMax < 1  |
// |         |            |        | unsatisfiable on the countdown branch under any config, while the |
// |         |            |        | breach it names happens on the mutually exclusive draw branch     |
// |         |            |        | (demonstrated by model). Moved to AssignRecoveryDays, the one     |
// |         |            |        | site whose clamp can write the breach.                            |
// | 1.12    | 2026-08-08 | —      | Balance-pass AR pass 15 (M1): the pass-14 guard fired AFTER       |
// |         |            |        | Severity was written — the draw branch was the step's one         |
// |         |            |        | partial-write throw site, leaving the live career F1-incoherent   |
// |         |            |        | behind the very refusal (demonstrated by model). AssignRecovery-  |
// |         |            |        | Days now runs before any write; the branch is atomic; the guard's |
// |         |            |        | message stops claiming the prevention the ordering provides.      |
// | 1.13    | 2026-08-22 | —      | ERR-041-020. AdvanceMedicalDay and AssembleRiskScore take int ageYears;
// |         |            |        | + AgeRiskFor (public, plus an internal parameterised overload so the
// |         |            |        | zero-span identity can be EXERCISED — the [GT]s are read once at static
// |         |            |        | init and the gate runs config-unbound). The term sits inside the sum
// |         |            |        | BEFORE the mitigation, normatively, so robustness discriminates it.
// | 1.14    | 2026-08-22 | —      | ERR-041-021 (AR over the ERR-041-020 landing, H4). Doc only. Row 1.13's
// |         |            |        | last sentence — and ERR-041-011's identical claim for BaselineDailyRisk
// |         |            |        | — is CORRECTED here rather than edited in place: RobustnessMitigation is
// |         |            |        | SUBTRACTED and addition commutes, so a term's position relative to it is
// |         |            |        | a no-op for every input (+1200 age penalty at robustness 1, 14 and 20
// |         |            |        | alike, and larger in relative terms for the more robust player). The
// |         |            |        | normative position is restated as what is actually load-bearing: inside
// |         |            |        | the sum, BEFORE the OccurrenceRiskMillMult scaling and BEFORE the clamp.
// | 1.15    | 2026-08-23 | —      | Group-B AR findings (Medium guards-unexercised half + 3 Low). The
// |         |            |        | parameterised AgeRiskFor's subtraction is widened on BOTH operands —
// |         |            |        | (long)ageYears - pivotYears, not (ageYears - pivotYears) then cast — a
// |         |            |        | sufficiently negative pivotYears otherwise overflows int and inverts the
// |         |            |        | term's sign (agerisk-int-subtraction-and-both-dials); the pivotYears
// |         |            |        | param doc now states it is deliberately the one unguarded dial and why.
// |         |            |        | perYear's doc corrected from "per-mille-of-a-million" (1000x too small)
// |         |            |        | to "per-million" (medicalstep-contract-docs). AdvanceMedicalDay's
// |         |            |        | <exception> docs: the ArgumentOutOfRangeException tag now names the
// |         |            |        | negative-ageYears source alongside the undefined-severity one, and the
// |         |            |        | InvalidOperationException tag counts FIVE consuming-site guards (was
// |         |            |        | four — AgeRiskFor's own guard is now named) rather than four.
// | 1.16    | 2026-08-24 | —      | Round-2 M/L pass, M9. The 4-arg parameterised overload is renamed
// |         |            |        | TestOnly_AgeRiskFor (was an overload distinguished from the 1-arg
// |         |            |        | production form by argument count alone — the M3 house convention,
// |         |            |        | already used ~40x in MatchEngine.cs); stays internal, IVT unchanged.
// |         |            |        | AgeRiskFor(int) demoted public -> internal to match its twin term
// |         |            |        | RobustnessMitigation (same sum, same file) — verified by repo-wide
// |         |            |        | grep that neither has a cross-assembly caller; AssembleRiskScore is
// |         |            |        | the sole production caller of both.
#endregion
