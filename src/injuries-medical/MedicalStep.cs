// File:     src/injuries-medical/MedicalStep.cs
// Created:  2026-08-05
// Modified: 2026-08-05
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
        /// <paramref name="state"/> is incoherent — recovery outstanding while healthy, or none while
        /// injured (F1); <paramref name="medical"/> has a zero recovery-speed multiplier
        /// (FR-MD-016 / F4); or <paramref name="worldDay"/> leaves a gap over the last-advanced day
        /// (F7) or is itself the never-advanced sentinel.
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
                    "worldDay must not be the never-advanced sentinel; storing it would re-arm the day-0 trap (F6).",
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
        /// The occurrence-risk assembly (§3.4) — pure, integer, and clamped to
        /// <c>[0, InjuriesMedicalConstants.InjuryRiskMax]</c>, the same scale the draw is taken on, so
        /// §3.1 compares the two directly with no scale factor between them.
        /// <para>
        /// The training term is #29's <b>already-published</b> scalar, read-only: #41 never reads or
        /// mutates #29's training-fatigue accumulator or the match engine's <c>AerobicPool</c>, so no
        /// counter is shared and a double count is not representable (KD-2 / FR-MD-009).
        /// </para>
        /// </summary>
        /// <param name="trainingRisk">#29's risk contribution.</param>
        /// <param name="load">Caller-supplied match participation.</param>
        /// <param name="attributes">The player's #27 attributes (the robustness mitigation).</param>
        /// <param name="medical">The staff seam; ×1.0 at <see cref="MedicalModifier.Identity"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="medical"/> has a zero recovery-speed multiplier — the <c>default(MedicalModifier)</c> trap (FR-MD-016 / F4).</exception>
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
        /// <param name="draw">The occurrence draw, already known to be below <paramref name="risk"/>.</param>
        /// <param name="risk">The assembled risk score the draw was compared against.</param>
        public static InjurySeverity ClassifySeverityFromDraw(int draw, int risk)
        {
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
        /// The keyed occurrence draw: a uniform in <c>[0, OccurrenceDrawDenom)</c> derived from
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
        internal static int DrawOccurrence(ulong worldSeed, int playerId, ulong actionOrdinal)
        {
            ulong h = Mix((ulong)InjuriesMedicalConstants.DomainTagInjuriesMedical ^ worldSeed);
            h = Mix(h ^ (ulong)(uint)playerId);
            h = Mix(h ^ actionOrdinal);

            return (int)(h % (ulong)InjuriesMedicalConstants.OccurrenceDrawDenom);
        }

        /// <summary>
        /// The deterministic own-attribute robustness mitigation (§3.4 / FR-MD-015) — never RNG. Stage
        /// 2 derives it from the three existing physical attributes; a dedicated #27
        /// <c>InjuryProneness</c> field is a recorded deep-tier deferral, deliberately not built here
        /// so the minimal tier causes no #27 schema ripple.
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
        /// <param name="severity">The assigned severity tier.</param>
        /// <param name="medical">The staff seam.</param>
        internal static int AssignRecoveryDays(InjurySeverity severity, in MedicalModifier medical)
        {
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
        /// The FR-MD-016 zero-value gate. <c>default(MedicalModifier)</c> is all-zero, which means ×0
        /// occurrence risk and a divide-by-zero recovery-days scale; it is never a valid runtime value,
        /// and the failure has to be loud because a silently-zero risk multiplier reads as "this game
        /// has no injuries" rather than as a bug.
        /// </summary>
        /// <param name="medical">The modifier to check.</param>
        private static void ValidateModifier(in MedicalModifier medical)
        {
            if (medical.RecoverySpeedMillMult == 0)
            {
                throw new ArgumentException(
                    "MedicalModifier.RecoverySpeedMillMult must not be zero; use MedicalModifier.Identity, " +
                    "never default(MedicalModifier) (FR-MD-016 / F4).",
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
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0): §3.1–§3.4 + the keyed draw. |
#endregion
