// File:     src/training-system/TrainingStep.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §3.1–§3.4 (FR-TR-004..017, FR-TR-021/026), F1/F4/F6/F7; Code Standards #20
// Purpose:  The four #29 entry points: the mutating daily world-day step, the pure growth-input read,
//           the pure match-entry fatigue projection, and the pure injury-risk output #41 consumes.
//           The FR-TR-023 focus command lives on TrainingSchedule, which owns the club-scoped pairing.

using System;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// #29's algorithms (§3). Every method here is deterministic and integer (or a pure integer→float
    /// projection); <b>none of them draws</b> — #29 registers no RNG stream and issues no random value
    /// (FR-TR-008/009 / KD-6), so every per-player variation is a function of the player's own
    /// attributes.
    /// <para>
    /// <see cref="AdvanceTrainingDay"/> is the only mutating entry point here (FR-TR-004);
    /// <see cref="ComputeTrainingInput"/>, <see cref="ProjectMatchEntryFatigue"/> and
    /// <see cref="ComputeInjuryRisk"/> are pure reads. The other writer in this assembly is the
    /// FR-TR-023 focus command, <see cref="TrainingSchedule.TrySetFocus"/>, which lives on the
    /// club-scoped handle so its ids and states are provably the pair bound at construction.
    /// </para>
    /// </summary>
    public static class TrainingStep
    {
        /// <summary>
        /// The daily world-day step (§3.1), invoked at #30's slot-2 training seam: conditioning delta,
        /// then the training-fatigue accrual, then the idempotency cursor.
        /// <para>
        /// <b>Idempotent per world day (F6).</b> Re-running an already-advanced day is a no-op, so a
        /// mid-week save → restore → re-run cannot double-accrue. A <b>day gap</b> fails loud (F7 /
        /// FR-TR-026) rather than silently under-accruing: #30 advances one world day at a time, so a
        /// gap is a caller bug, not a catch-up case, and #29 deliberately has no rollover loop (KD-4).
        /// </para>
        /// </summary>
        /// <param name="state">The player's training state, mutated in place.</param>
        /// <param name="attributes">The player's #27 attributes, read-only — #29 never writes them (FR-TR-005).</param>
        /// <param name="coach">The KD-3 staff seam; <see cref="CoachingModifier.Identity"/> until #34 lands.</param>
        /// <param name="worldDay">The world day being advanced.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="state"/> carries an undefined <see cref="TrainingFocus"/> ordinal — an
        /// out-of-contract focus fails loud at the consuming seam rather than being clamped (F4 /
        /// FR-TR-021).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="worldDay"/> leaves a gap over the last-advanced day (F7 / FR-TR-026), or is
        /// itself the never-advanced sentinel — writing the sentinel back as a real advanced day would
        /// silently re-arm the day-0 trap on the next call.
        /// </exception>
        public static void AdvanceTrainingDay(
            ref TrainingState state,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            uint worldDay)
        {
            if (!TrainingSystemConstants.IsDefinedFocus(state.Focus))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state), state.Focus, "Undefined TrainingFocus on the advancing state (F4 / FR-TR-021).");
            }

            if (worldDay == TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentException(
                    "worldDay must not be the never-advanced sentinel; storing it would re-arm the day-0 trap (F6).",
                    nameof(worldDay));
            }

            if (state.LastAdvancedWorldDay != TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL)
            {
                if (worldDay <= state.LastAdvancedWorldDay)
                {
                    return;   // already advanced — idempotent no-op (F6)
                }

                if (worldDay > state.LastAdvancedWorldDay + 1)
                {
                    throw new ArgumentException(
                        "world-day gap: #29 does not batch-replay skipped days (F7 / FR-TR-026).",
                        nameof(worldDay));
                }
            }

            // 1. Conditioning delta — focus table + a deterministic own-attribute bonus, routed
            //    through the coaching seam (×1.0 at Identity), clamped at both bounds (F1).
            int conditionDelta = TrainingSystemConstants.ConditionDeltaFor(state.Focus)
                                 + AttributeConditioningBonus(attributes);
            conditionDelta = ApplyCoach(conditionDelta, coach);
            state.Condition = Clamp(
                state.Condition + conditionDelta,
                TrainingSystemConstants.ConditionMin,
                TrainingSystemConstants.ConditionMax);

            // 2. Training-fatigue accrual = the focus LOAD minus the passive daily recovery, which
            //    applies every day regardless of focus (§3.1) — this is the WORLD-TICK accumulator,
            //    never the match counter (FR-TR-011).
            int fatigueDelta = TrainingSystemConstants.FatigueLoadFor(state.Focus)
                               - TrainingSystemConstants.FatigueDailyRecovery;
            fatigueDelta = ApplyCoach(fatigueDelta, coach);
            state.TrainingFatigue = Clamp(
                state.TrainingFatigue + fatigueDelta,
                0,
                TrainingSystemConstants.TrainingFatigueMax);

            // 3. Advance the idempotency cursor.
            state.LastAdvancedWorldDay = worldDay;
        }

        /// <summary>
        /// The growth-input read (§3.2), gathered by #30 into the batch #28's
        /// <c>AdvanceDay(worldDay, trainingInputs)</c> consumes at the slot-1 progression seam.
        /// <para>
        /// Pure, and — the load-bearing part — <b>field-independent of the slot-2 step</b>: it reads
        /// only <see cref="TrainingState.Focus"/>, the attributes and the coaching seam, never
        /// <c>Condition</c> / <c>TrainingFatigue</c> / <c>LastAdvancedWorldDay</c> (FR-TR-006). That is
        /// what makes the two slots order-independent; purity alone would not, since a pure read of a
        /// field slot-2 mutates would still change with slot order.
        /// </para>
        /// <para>
        /// At Stage 2 <paramref name="deepTrainingEnabled"/> is off and the result is
        /// <see cref="TrainingInput.Neutral"/>, so #28's growth is byte-identical to the no-training
        /// path (KD-8 / FR-TR-007). <c>deepTrainingEnabled</c> is #29's OWN dial — not #28's
        /// <c>curveEnabled</c>, which independently governs whether #28 realizes a non-neutral input.
        /// </para>
        /// </summary>
        /// <param name="state">The player's training state, read-only.</param>
        /// <param name="attributes">The player's #27 attributes, read-only.</param>
        /// <param name="coach">The KD-3 staff seam.</param>
        /// <param name="deepTrainingEnabled">#29's own Stage-2/Stage-3 dial (off at Stage 2).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> carries an undefined focus (F4 / FR-TR-021).</exception>
        public static TrainingInput ComputeTrainingInput(
            in TrainingState state,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            bool deepTrainingEnabled)
        {
            if (!TrainingSystemConstants.IsDefinedFocus(state.Focus))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state), state.Focus, "Undefined TrainingFocus at the growth-input seam (F4 / FR-TR-021).");
            }

            if (!deepTrainingEnabled)
            {
                return TrainingInput.Neutral;
            }

            // Stage-3 (#29 T3) builds the deterministic per-attribute contribution here, weighted by
            // focus + coaching + #53's root-assembled facility term (FR-TR-005a). #28's TrainingInput
            // has no fields to populate yet, so the deep branch is the neutral value today — writing a
            // magnitude into a type that cannot carry it is the phantom class this project refuses.
            return TrainingInput.Neutral;
        }

        /// <summary>
        /// The match-entry fatigue projection (§3.3): the world-tick training-fatigue accumulator as a
        /// starting-fatigue offset in <c>[0,1]</c>, which the match-boot caller passes as the
        /// <c>float fatigue</c> argument #27's <c>PlayerAttributeProjection</c> seams already accept.
        /// <para>
        /// One-directional and <b>never stored</b>: it is recomputed from the serialized accumulator, so
        /// it is identical either side of a save→restore (KD-1). Match-tick fatigue
        /// (<c>1 − AerobicPool</c>) never writes back into <see cref="TrainingState.TrainingFatigue"/>
        /// (FR-TR-012), and #29 references no match-engine type at all (FR-TR-013).
        /// </para>
        /// <para>
        /// The fatigue convention is the project-wide one: <b>0 = fully rested, 1 = fully fatigued</b>.
        /// </para>
        /// </summary>
        /// <param name="state">The player's training state, read-only.</param>
        public static float ProjectMatchEntryFatigue(in TrainingState state)
        {
            // A non-positive ceiling is only reachable through a config override, and
            // TrainingSystemConstantsTests rejects it at the design-time fallback. What it would do
            // here is 0/0 = NaN, and a NaN reaching match boot would not fail loud — it would silently
            // poison every downstream attribute projection. Short-circuit to the RESTED end of the
            // scale instead: not a refusal, a bounded wrong answer chosen over an unbounded one.
            if (TrainingSystemConstants.TrainingFatigueMax <= 0)
            {
                return 0f;
            }

            float fraction = (float)state.TrainingFatigue / TrainingSystemConstants.TrainingFatigueMax;
            return Clamp01(fraction * TrainingSystemConstants.MatchEntryFatigueScale);
        }

        /// <summary>
        /// The injury-risk output (§3.4 / KD-5): training-fatigue plus the conditioning shortfall, less
        /// a deterministic own-attribute robustness mitigation, clamped to
        /// <c>[0, TrainingSystemConstants.InjuryRiskMax]</c>.
        /// <para>
        /// #41 consumes this scalar and owns occurrence, severity and recovery; #29 computes the input
        /// only and holds no injury model (FR-TR-017). No RNG — an injury draw is #41's, on #41's
        /// stream, keyed on the world day (#41 KD-1).
        /// </para>
        /// </summary>
        /// <param name="state">The player's training state, read-only.</param>
        /// <param name="attributes">The player's #27 attributes, read-only.</param>
        public static InjuryRiskContribution ComputeInjuryRisk(in TrainingState state, in PlayerAttributes attributes)
        {
            long risk = (long)TrainingSystemConstants.FatigueRiskWeight * state.TrainingFatigue
                        + (long)TrainingSystemConstants.LowConditionRiskWeight
                          * (TrainingSystemConstants.ConditionMax - state.Condition)
                        - RobustnessMitigation(attributes);

            return new InjuryRiskContribution(ClampLong(risk, 0, TrainingSystemConstants.InjuryRiskMax));
        }

        /// <summary>
        /// The deterministic own-attribute conditioning bonus (§3.1 step 1) — the mean of
        /// <c>WorkRate</c> and <c>Stamina</c>, weighted. Never RNG (FR-TR-009).
        /// </summary>
        /// <param name="attributes">The player's #27 attributes.</param>
        internal static int AttributeConditioningBonus(in PlayerAttributes attributes)
        {
            int mean = (attributes.WorkRate + attributes.Stamina) / 2;
            return mean * TrainingSystemConstants.ConditioningAttributeWeight;
        }

        /// <summary>
        /// The deterministic own-attribute injury mitigation (§3.4) — the mean of the three physical
        /// robustness attributes, weighted. Never RNG (FR-TR-009).
        /// <para>
        /// <b>#41 mitigates again, on the same three attributes, downstream of this.</b> Its own
        /// <c>[GT]</c> term (#41 FR-MD-015) is subtracted from the scalar this method has already
        /// reduced, so robustness is priced into the occurrence probability twice and the two tables
        /// are not independently tunable. Each spec mandates its own term, so the layering is
        /// spec-faithful — but its consequence is load-bearing and easy to miss: the value returned
        /// here can saturate <see cref="TrainingSystemConstants.InjuryRiskMax"/> while the risk #41
        /// finally draws against still falls short of its ceiling, because the <c>[1,20]</c> attribute
        /// floor guarantees #41 always subtracts at least its lowest row. Recorded under ERR-041-003
        /// for the balance pass; do not tune this weight without re-reading #41's.
        /// </para>
        /// </summary>
        /// <param name="attributes">The player's #27 attributes.</param>
        internal static int RobustnessMitigation(in PlayerAttributes attributes)
        {
            int mean = (attributes.Strength + attributes.Stamina + attributes.Balance) / 3;
            return mean * TrainingSystemConstants.RobustnessMitigationPerPoint;
        }

        /// <summary>
        /// The KD-3 coaching routing seam: ×1.0 under <see cref="CoachingModifier.Identity"/>, which is
        /// every call today. #34 lands the non-identity form here and nowhere else — one consumption
        /// point is what keeps #34 from becoming a second training-effectiveness path (#29 §7.3).
        /// </summary>
        /// <param name="delta">The daily delta being routed.</param>
        /// <param name="coach">The coaching modifier.</param>
        internal static int ApplyCoach(int delta, in CoachingModifier coach) => delta;

        /// <summary>Integer clamp to <c>[min, max]</c>.</summary>
        internal static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        /// <summary>Widened clamp to <c>[min, max]</c>, narrowing to <c>int</c> — the risk assembly sums weighted terms that can exceed <c>int</c> range before the clamp.</summary>
        internal static int ClampLong(long value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : (int)value;
        }

        /// <summary>Clamp to <c>[0,1]</c>. Hand-rolled rather than <c>Mathf.Clamp01</c> — this assembly declares <c>noEngineReferences</c> and consumes no UnityEngine type.</summary>
        internal static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0): §3.1–§3.4.                         |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (H): SetFocus moved to TrainingSchedule.TrySetFocus — the  |
// |         |            |        | two-array signature accepted one club's ids with another's states.  |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): RobustnessMitigation's doc said #41's term was tuned  |
// |         |            |        | independently; it compounds on the same attributes (ERR-041-003).   |
// |         |            |        | The §3.3 zero-ceiling comment said "refuse" of a branch that returns |
// |         |            |        | the rested value.                                                   |
#endregion
