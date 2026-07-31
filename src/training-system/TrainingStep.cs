// File:     src/training-system/TrainingStep.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.1 / §3.1 / §3.2 / §3.3 / §3.4; Code Standards #20
// Purpose:  Deterministic daily training-state evolution and pure cross-system projections —
//           the two FR-TR-004 entry points (the pure ComputeTrainingInput that feeds #28 at #30's
//           slot-1 seam, and the mutating AdvanceTrainingDay that is #30's slot-2 world-day step).

using System;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// Implements the deterministic Stage-2 daily training core. Every method is a pure function of
    /// its arguments — no RNG stream is registered and none is drawn (FR-TR-008 / KD-6), so
    /// save→restore→re-run is byte-exact.
    /// </summary>
    /// <remarks>
    /// FR-TR-016 specifies the <c>in CoachingModifier</c> parameter as "defaulting to
    /// <c>Identity</c>". It is NOT expressed as a C# optional parameter here, and that is a deliberate,
    /// recorded deviation rather than an omission: in both §3.1's and §3.2's pinned parameter orders the
    /// coach is followed by a REQUIRED parameter (<c>worldDay</c> / <c>deepTrainingEnabled</c>), and C#
    /// forbids an optional parameter ahead of a required one (CS1737). Reordering to suit the language
    /// would silently diverge the code from the spec's own pseudocode signature, and an extra
    /// coach-less overload would give one operation two call shapes. Callers pass
    /// <see cref="CoachingModifier.Identity"/> explicitly — which IS <c>default</c> — so the identity
    /// stays one token away and visible at the call site.
    /// </remarks>
    public static class TrainingStep
    {
        /// <summary>Advances one player's state by exactly one world day (§3.1, #30's slot-2).</summary>
        /// <param name="state">The player's #29-owned training state; mutated in place.</param>
        /// <param name="attributes">The player's own attributes — read only, never written (FR-TR-005).</param>
        /// <param name="coach">
        /// The #34 routing seam; <see cref="CoachingModifier.Identity"/> until Staff &amp; Backroom #34
        /// becomes a producer (KD-3 / FR-TR-016). See the type remarks for why it carries no C# default.
        /// </param>
        /// <param name="worldDay">
        /// The world-day being advanced. MUST be <c>LastAdvancedWorldDay + 1</c> in normal operation
        /// (FR-TR-026); an already-advanced day is a no-op (F6) and a gap fails loud (F7).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="worldDay"/> is
        /// <see cref="TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL"/> (outside the world-day
        /// domain — see below), or <paramref name="state"/> carries an out-of-catalogue focus (F4).
        /// </exception>
        /// <exception cref="ArgumentException">A day gap over the last-advanced day (F7 / FR-TR-026).</exception>
        public static void AdvanceTrainingDay(
            ref TrainingState state,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            uint worldDay)
        {
            // The sentinel is NOT a world day — it is the "never advanced" marker (Appendix A). Writing
            // it into LastAdvancedWorldDay would leave an advanced state indistinguishable from a fresh
            // one, re-arming the very day-0 double-accrual trap the sentinel exists to prevent: the same
            // day would accrue again, and any later (smaller) day would REWIND the cursor and accrue a
            // third time. Refused fail-loud at the boundary — ahead of every other check and every write
            // — rather than clamped: an out-of-domain day is a caller bug (the F7 posture / FR-TR-021).
            if (worldDay == TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldDay),
                    worldDay,
                    "The never-advanced sentinel is not a valid world day.");
            }

            ValidateFocus(state.Focus);

            if (state.LastAdvancedWorldDay != TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL)
            {
                if (worldDay <= state.LastAdvancedWorldDay)
                {
                    return;
                }

                if (worldDay > state.LastAdvancedWorldDay + 1)
                {
                    throw new ArgumentException("Training cannot skip a world day.", nameof(worldDay));
                }
            }

            // 1. Conditioning delta — the focus table plus the deterministic own-attribute bonus, then
            //    coaching (×1.0 at Identity).
            int conditionDelta = ApplyCoach(
                FocusConditionDelta(state.Focus) + AttributeConditioningBonus(in attributes),
                in coach);
            state.Condition = Clamp(
                state.Condition + conditionDelta,
                TrainingSystemConstants.ConditionMin,
                TrainingSystemConstants.ConditionMax);

            // 2. Training-fatigue accrual = load − passive recovery (the world-tick accumulator,
            //    FR-TR-011 — never the match counter). Recovery applies every day regardless of focus.
            int fatigueDelta = ApplyCoach(
                FocusFatigueDelta(state.Focus) - TrainingSystemConstants.FatigueDailyRecovery,
                in coach);
            state.TrainingFatigue = Clamp(
                state.TrainingFatigue + fatigueDelta,
                0,
                TrainingSystemConstants.TrainingFatigueMax);

            // 3. Advance the idempotency cursor (F6).
            state.LastAdvancedWorldDay = worldDay;
        }

        /// <summary>
        /// The pure growth-input read (§3.2, #30's slot-1) — the seam #29 exists for: the ONLY way #29
        /// influences attributes is by populating #28's <see cref="TrainingInput"/> for #28's
        /// <c>GrowthProjection</c> to consume, so #28 stays the sole attribute writer (FR-TR-005).
        /// </summary>
        /// <remarks>
        /// Pure and deterministic: it never mutates <paramref name="state"/>, draws nothing, and reads
        /// only fields <see cref="AdvanceTrainingDay"/> does NOT mutate — <c>Focus</c>, the player's
        /// attributes, and the coach. That field-independence, not purity alone, is what makes the
        /// slot-1 read order-independent of the slot-2 mutation (FR-TR-006 / KD-2), so it must never
        /// grow a read of <c>Condition</c> / <c>TrainingFatigue</c> / <c>LastAdvancedWorldDay</c>.
        /// <para>
        /// STAGE-2 SCOPE. With <paramref name="deepTrainingEnabled"/> off this returns exactly
        /// <see cref="TrainingInput.Neutral"/>, so #28's growth is byte-identical to the no-training
        /// path (FR-TR-007 / KD-8 / Appendix C) — that is the shipped Stage-2 contract, and it is
        /// test-locked. With the dial ON it also returns <c>Neutral</c> today: §3.2's deep
        /// <c>BuildTrainingInput</c> weighting is a Stage-3 deliverable and is deliberately NOT built
        /// here, so the dial is currently an inert identity rather than a second behaviour. It does not
        /// throw, because FR-TR-007's contract concerns the OFF state and a #30 integration that sets
        /// the dial early must not crash — but a dial-on caller gets no deep contribution until Stage-3
        /// lands, and the neutrality lock will need re-anchoring at that point.
        /// </para>
        /// <para>
        /// DEFERRED PARAMETER (FR-TR-005a / ERR-029-003, ◑ spec-text-first): §3.2's signature also takes
        /// #53's training-ground <c>FacilityModifier</c> as a SECOND root-assembled term alongside the
        /// coach. That parameter lands at #29's Stage-3 tier together with <c>BuildTrainingInput</c> —
        /// the only consumer that could read it — since adding it now would be a parameter no code path
        /// reads, typed against a #53 surface that does not exist. Neutral facilities are specified to
        /// be behaviour-neutral, so its arrival cannot change the value returned here.
        /// </para>
        /// </remarks>
        /// <param name="state">The player's training state; read only — never mutated.</param>
        /// <param name="attributes">The player's own attributes (FR-TR-009 — variation is attribute-driven, never RNG).</param>
        /// <param name="coach">The #34 routing seam; <see cref="CoachingModifier.Identity"/> today (KD-3).</param>
        /// <param name="deepTrainingEnabled">
        /// #29's OWN Stage-2/Stage-3 dial (FR-TR-007) — explicitly NOT #28's independent
        /// <c>curveEnabled</c>, which separately governs whether #28 realizes a non-neutral input.
        /// </param>
        /// <returns>The player's growth contribution for the day; <see cref="TrainingInput.Neutral"/> at Stage-2.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> carries an out-of-catalogue focus (F4).</exception>
        public static TrainingInput ComputeTrainingInput(
            in TrainingState state,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            bool deepTrainingEnabled)
        {
            // An out-of-contract focus fails loud at the consuming seam, not silently (F4 / FR-TR-021).
            ValidateFocus(state.Focus);

            if (!deepTrainingEnabled)
            {
                return TrainingInput.Neutral; // KD-8 identity — #28's growth == the no-training path.
            }

            // Stage-3: return BuildTrainingInput(state.Focus, attributes, coach, facilities) — the
            // deterministic per-attribute weighting. Not built at Stage-2 (see the remarks); the dial
            // is an inert identity until it lands.
            return TrainingInput.Neutral;
        }

        /// <summary>Projects world-tick training fatigue into a match-boot starting fatigue offset (§3.3).</summary>
        /// <remarks>
        /// One-directional and NOT stored — recomputed from the serialized accumulator, so it is
        /// identical before and after a save→restore (KD-1). Match-tick fatigue never writes back into
        /// <see cref="TrainingState.TrainingFatigue"/>, and #29 never touches <c>AerobicPool</c>
        /// (FR-TR-012 / FR-TR-013).
        /// </remarks>
        public static float ProjectMatchEntryFatigue(in TrainingState state)
        {
            float fatigue = (float)state.TrainingFatigue / TrainingSystemConstants.TrainingFatigueMax
                * TrainingSystemConstants.MatchEntryFatigueScale;
            return Clamp01(fatigue);
        }

        /// <summary>Calculates the read-only training contribution to the future injury model (§3.4).</summary>
        /// <remarks>
        /// #29 owns only this input; Injuries &amp; Medical #41 owns occurrence, severity and recovery
        /// and pulls this value (KD-5 — no #41 interface is built).
        /// </remarks>
        public static InjuryRiskContribution ComputeInjuryRisk(in TrainingState state, in PlayerAttributes attributes)
        {
            // Accumulated in `long`: the state's fields are public-mutable, so an out-of-contract
            // TrainingFatigue could wrap an `int` sum NEGATIVE and clamp to 0 — the most fatigued
            // representable player reporting zero risk, i.e. failing OPEN in the direction that hides
            // danger. A wide accumulator leaves the clamp as the only bound that decides. Unreachable
            // from states evolved by AdvanceTrainingDay (whose cursors are clamped), but the posture
            // here is fail-safe rather than fail-open.
            long risk = (long)TrainingSystemConstants.FatigueRiskWeight * state.TrainingFatigue
                + (long)TrainingSystemConstants.LowConditionRiskWeight
                    * (TrainingSystemConstants.ConditionMax - (long)state.Condition)
                - RobustnessMitigation(in attributes);
            return new InjuryRiskContribution(Clamp(risk, 0, TrainingSystemConstants.InjuryRiskMax));
        }

        /// <summary>Validates persisted or caller-supplied focus values at the consuming seam (F4).</summary>
        public static void ValidateFocus(TrainingFocus focus)
        {
            if (focus < TrainingFocus.Balanced || focus > TrainingFocus.Tactical)
            {
                throw new ArgumentOutOfRangeException(nameof(focus), focus, "Training focus is outside the supported catalogue.");
            }
        }

        // The §3.1 coaching hook. Identity (×1.0) at Stage-2: Staff & Backroom #34 is the future
        // producer of a non-identity CoachingModifier (KD-3 / FR-TR-016; the modifier's per-mille field
        // shape and #29's consumption of it land together as ERR-029-002, deferred to #34 T3). Kept as
        // a named routing function rather than an elided multiplication so §3.1's pseudocode shape is
        // structurally present and #34 has exactly ONE insertion point covering both deltas.
        private static int ApplyCoach(int delta, in CoachingModifier coach)
        {
            return delta;
        }

        // Appendix A per-focus tables, indexed by the focus ORDINAL. Every production call site already
        // sits behind ValidateFocus, so the index is in range by construction; the explicit re-check
        // keeps a future caller from indexing past the table instead of failing loud.
        private static int FocusConditionDelta(TrainingFocus focus)
        {
            ValidateFocus(focus);
            return TrainingSystemConstants.FocusConditionDelta[(int)focus];
        }

        private static int FocusFatigueDelta(TrainingFocus focus)
        {
            ValidateFocus(focus);
            return TrainingSystemConstants.FocusFatigueDelta[(int)focus];
        }

        // Deterministic own-attribute conditioning bonus (§3.1) — a pure function of the player's own
        // attributes, never an RNG draw (FR-TR-009). Weights are catalogued so the sum is tunable.
        private static int AttributeConditioningBonus(in PlayerAttributes attributes)
        {
            return TrainingSystemConstants.ConditioningBonusWorkRateWeight * attributes.WorkRate
                + TrainingSystemConstants.ConditioningBonusStaminaWeight * attributes.Stamina;
        }

        // Deterministic own-attribute injury mitigation (§3.4). Same weighting discipline as the
        // conditioning bonus — the two sums are independent and must not drift implicitly.
        private static int RobustnessMitigation(in PlayerAttributes attributes)
        {
            return TrainingSystemConstants.RobustnessStrengthWeight * attributes.Strength
                + TrainingSystemConstants.RobustnessStaminaWeight * attributes.Stamina;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static int Clamp(long value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : (int)value;
        }

        private static float Clamp01(float value)
        {
            if (value < 0.0f)
            {
                return 0.0f;
            }

            return value > 1.0f ? 1.0f : value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 H-1/M-1/M-3/L-1/L-2/L-3/L-4. H-1: `ComputeTrainingInput` implemented
// |         |            |        | (§3.2 / FR-TR-004) — the pure slot-1 growth-input read returning #28's
// |         |            |        | `TrainingInput`, gated by the #29-owned `deepTrainingEnabled` dial; the
// |         |            |        | Stage-3 `BuildTrainingInput` weighting and the FR-TR-005a facility
// |         |            |        | parameter are doc-noted deferrals, not silent omissions. M-1: the
// |         |            |        | never-advanced sentinel is refused as a `worldDay` ahead of every check
// |         |            |        | and every write (as a value it re-armed "never advanced" and
// |         |            |        | double-accrued — proven by execution). M-3: the two focus switch
// |         |            |        | statements replaced by reads of the Appendix A catalogue tables, and
// |         |            |        | both own-attribute sums now carry catalogued weights. L-1: the §3.1
// |         |            |        | `ApplyCoach` hook exists as a named identity routing both deltas (#34 /
// |         |            |        | ERR-029-002 cited), so the coach is routed rather than
// |         |            |        | accepted-and-ignored. L-2: FR-TR-016's "defaulting to Identity" recorded
// |         |            |        | as a deviation with its CS1737 reason (type remarks). L-3: the
// |         |            |        | injury-risk sum accumulates in `long`, so an out-of-contract fatigue
// |         |            |        | cannot wrap negative and clamp to zero risk. L-4: `Author:` header.
#endregion
