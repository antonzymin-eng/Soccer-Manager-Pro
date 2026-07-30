// File:     src/training-system/TrainingStep.cs
// Created:  2026-07-30
// Modified: 2026-07-30
// Spec:     Training System #29 §2.1 / §3.1 / §3.3 / §3.4; Code Standards #20
// Purpose:  Deterministic daily training-state evolution and pure cross-system projections.

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Implements the deterministic Stage-2 daily training core.</summary>
    public static class TrainingStep
    {
        /// <summary>Advances one player's state by exactly one world day.</summary>
        public static void AdvanceTrainingDay(
            ref TrainingState state,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            uint worldDay)
        {
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

            state.Condition = Clamp(
                state.Condition + GetFocusConditionDelta(state.Focus) + AttributeConditioningBonus(in attributes),
                TrainingSystemConstants.CONDITION_MIN,
                TrainingSystemConstants.CONDITION_MAX);
            state.TrainingFatigue = Clamp(
                state.TrainingFatigue + GetFocusFatigueDelta(state.Focus) - TrainingSystemConstants.FATIGUE_DAILY_RECOVERY,
                0,
                TrainingSystemConstants.TRAINING_FATIGUE_MAX);
            state.LastAdvancedWorldDay = worldDay;
        }

        /// <summary>Projects world-tick training fatigue into a match-boot starting fatigue offset.</summary>
        public static float ProjectMatchEntryFatigue(in TrainingState state)
        {
            float fatigue = (float)state.TrainingFatigue / TrainingSystemConstants.TRAINING_FATIGUE_MAX
                * TrainingSystemConstants.MATCH_ENTRY_FATIGUE_SCALE;
            return Clamp01(fatigue);
        }

        /// <summary>Calculates the read-only training contribution to the future injury model.</summary>
        public static InjuryRiskContribution ComputeInjuryRisk(in TrainingState state, in PlayerAttributes attributes)
        {
            int risk = TrainingSystemConstants.FATIGUE_RISK_WEIGHT * state.TrainingFatigue
                + TrainingSystemConstants.LOW_CONDITION_RISK_WEIGHT * (TrainingSystemConstants.CONDITION_MAX - state.Condition)
                - RobustnessMitigation(in attributes);
            return new InjuryRiskContribution(Clamp(risk, 0, TrainingSystemConstants.INJURY_RISK_MAX));
        }

        /// <summary>Validates persisted or caller-supplied focus values at the consuming seam.</summary>
        public static void ValidateFocus(TrainingFocus focus)
        {
            if (focus < TrainingFocus.Balanced || focus > TrainingFocus.Tactical)
            {
                throw new ArgumentOutOfRangeException(nameof(focus), focus, "Training focus is outside the supported catalogue.");
            }
        }

        private static int GetFocusConditionDelta(TrainingFocus focus)
        {
            switch (focus)
            {
                case TrainingFocus.Balanced:
                    return 70;
                case TrainingFocus.Rest:
                    return 30;
                case TrainingFocus.Fitness:
                    return 120;
                case TrainingFocus.Technical:
                    return 80;
                case TrainingFocus.Physical:
                    return 100;
                case TrainingFocus.Tactical:
                    return 70;
                default:
                    throw new ArgumentOutOfRangeException(nameof(focus));
            }
        }

        private static int GetFocusFatigueDelta(TrainingFocus focus)
        {
            switch (focus)
            {
                case TrainingFocus.Balanced:
                    return 220;
                case TrainingFocus.Rest:
                    return 0;
                case TrainingFocus.Fitness:
                    return 300;
                case TrainingFocus.Technical:
                    return 200;
                case TrainingFocus.Physical:
                    return 280;
                case TrainingFocus.Tactical:
                    return 180;
                default:
                    throw new ArgumentOutOfRangeException(nameof(focus));
            }
        }

        private static int AttributeConditioningBonus(in PlayerAttributes attributes)
        {
            return attributes.WorkRate + attributes.Stamina;
        }

        private static int RobustnessMitigation(in PlayerAttributes attributes)
        {
            return attributes.Strength + attributes.Stamina;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
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
#endregion