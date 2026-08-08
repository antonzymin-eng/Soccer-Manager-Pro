// File:     src/injuries-medical/MedicalStep.cs
// Created:  2026-08-05
// Modified: 2026-08-08 (AR pass 11 L3: the guard mirrors all three lock predicates — v1.8)
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
        /// <param name="trainingRisk">#29's already-published risk scalar, read-only (FR-MD-009). #41 never touches #29's fatigue accumulator.</param>
        /// <param name="recentMatchLoad">Caller-supplied match participation (FR-MD-010); <see cref="MatchLoad.None"/> when there is none.</param>
        /// <param name="medical">The KD-5 staff seam; <see cref="MedicalModifier.Identity"/> until #34 lands.</param>
        /// <param name="worldDay">The world day being advanced.</param>
        /// <param name="worldSeed">The world seed the draw key is derived from — the career's seed, not a per-match one.</param>
        /// <param name="occurrenceEnabled">The KD-8 dial. Off reduces the step to recovery-only with no draw at all (FR-MD-027).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> carries an undefined <see cref="InjurySeverity"/> (F4).</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="state"/> is incoherent — recovery outstanding while healthy, none while
        /// injured, negative, or above the ceiling (F1); either <paramref name="medical"/> multiplier
        /// is non-positive (FR-MD-016 / F4); or <paramref name="worldDay"/> leaves a gap over the
        /// last-advanced day (F7) or is itself the never-advanced sentinel.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The draw denominator is not positive — propagated from <see cref="DrawOccurrence"/>; a
        /// catalogue integrity failure, not a caller error.
        /// </exception>
        public static void AdvanceMedicalDay(
            ref InjuryState state,
            int playerId,
            in PlayerAttributes attributes,
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
                int risk = AssembleRiskScore(trainingRisk, recentMatchLoad, attributes, medical);
                ulong actionOrdinal = DeriveActionOrdinal(worldDay, InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE);
                int draw = DrawOccurrence(worldSeed, playerId, actionOrdinal);

                if (draw < risk)
                {
                    // The SAME draw that confirmed the occurrence also classifies it (§3.2) — Stage 2
                    // consumes exactly one draw per occurrence-eligible day, never two.
                    InjurySeverity severity = ClassifySeverityFromDraw(draw, risk);

                    state.Severity = severity;
                    state.RecoveryRemaining = AssignRecoveryDays(severity, medical);
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
        /// <b><c>BaselineDailyRisk</c> sits BEFORE the mitigation, normatively</b> (§3.4 /
        /// ERR-041-011): the exposure-independent floor is discriminated by robustness — a frail
        /// player's quiet week is riskier than an iron man's — which an after-the-clamp addition
        /// could not be. It is also what keeps a fit player on the default focus from being
        /// injury-proof forever, the third absurdity the fifth AR pass measured.
        /// </para>
        /// </summary>
        /// <param name="trainingRisk">#29's risk contribution.</param>
        /// <param name="load">Caller-supplied match participation.</param>
        /// <param name="attributes">The player's #27 attributes (the robustness mitigation).</param>
        /// <param name="medical">The staff seam; ×1.0 at <see cref="MedicalModifier.Identity"/>.</param>
        /// <exception cref="ArgumentException">Either <paramref name="medical"/> multiplier is non-positive — the <c>default(MedicalModifier)</c> trap, and the negative one that produces no crash to announce it (FR-MD-016 / F4).</exception>
        public static int AssembleRiskScore(
            in InjuryRiskContribution trainingRisk,
            in MatchLoad load,
            in PlayerAttributes attributes,
            in MedicalModifier medical)
        {
            ValidateModifier(medical);

            long risk = (long)InjuriesMedicalConstants.TrainingRiskPassthroughWeight * trainingRisk.RiskScore
                        + (long)InjuriesMedicalConstants.AppearanceLoadWeight * load.AppearanceDays
                        + (long)InjuriesMedicalConstants.HardContactWeight * load.HardContacts
                        + InjuriesMedicalConstants.BaselineDailyRisk
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
            // CAN still break is the invariant the decoupling introduced: a ceiling raised past the
            // denominator makes a clamped risk mean "certain and then some", silently. One comparison
            // at the one drawing site.
            if (InjuriesMedicalConstants.InjuryRiskMax > InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM)
            {
                throw new InvalidOperationException(
                    "InjuryRiskMax exceeds OCCURRENCE_DRAW_DENOM — the probability ceiling would pass " +
                    "1; catalogue/config integrity failure (ERR-041-011, §3.4).");
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
#endregion
