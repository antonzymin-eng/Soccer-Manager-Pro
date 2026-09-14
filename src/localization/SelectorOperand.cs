// ============================================================================
// File:     src/localization/SelectorOperand.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-009, ERR-049-004
// Purpose:  Typed locale-neutral cardinal/gender input for later L2 rendering selection.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Locale-neutral typed operand for bounded plural/gender selection.</summary>
    public readonly struct SelectorOperand : IEquatable<SelectorOperand>
    {
        private SelectorOperand(bool hasCardinal, long cardinalValue, bool hasGender, GrammaticalGender gender)
        {
            HasCardinal = hasCardinal;
            CardinalValue = cardinalValue;
            HasGender = hasGender;
            Gender = gender;
        }

        /// <summary>Creates a cardinal-only selector operand.</summary>
        public static SelectorOperand FromCardinal(long cardinalValue)
        {
            return new SelectorOperand(true, cardinalValue, false, GrammaticalGender.Unspecified);
        }

        /// <summary>Creates a gender-only selector operand.</summary>
        public static SelectorOperand FromGender(GrammaticalGender gender)
        {
            ValidateGender(gender);
            return new SelectorOperand(false, 0, true, gender);
        }

        /// <summary>Creates a selector operand carrying both cardinal and gender values.</summary>
        public static SelectorOperand From(long cardinalValue, GrammaticalGender gender)
        {
            ValidateGender(gender);
            return new SelectorOperand(true, cardinalValue, true, gender);
        }

        /// <summary>Gets whether a cardinal value is present.</summary>
        public bool HasCardinal { get; }

        /// <summary>Gets the cardinal value when <see cref="HasCardinal"/> is true.</summary>
        public long CardinalValue { get; }

        /// <summary>Gets whether a grammatical-gender value is present.</summary>
        public bool HasGender { get; }

        /// <summary>Gets the grammatical-gender value when <see cref="HasGender"/> is true.</summary>
        public GrammaticalGender Gender { get; }

        /// <summary>Gets whether at least one typed selector dimension is present.</summary>
        public bool IsValid => HasCardinal || HasGender;

        /// <inheritdoc />
        public bool Equals(SelectorOperand other)
        {
            return HasCardinal == other.HasCardinal
                && CardinalValue == other.CardinalValue
                && HasGender == other.HasGender
                && Gender == other.Gender;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SelectorOperand other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = LocalizationHash.Combine(HasCardinal ? 1 : 0, LocalizationHash.Long(CardinalValue));
            hash = LocalizationHash.Combine(hash, HasGender ? 1 : 0);
            return LocalizationHash.Combine(hash, (int)Gender);
        }

        /// <summary>Compares two selector operands by value.</summary>
        public static bool operator ==(SelectorOperand left, SelectorOperand right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two selector operands by value.</summary>
        public static bool operator !=(SelectorOperand left, SelectorOperand right)
        {
            return !left.Equals(right);
        }

        private static void ValidateGender(GrammaticalGender gender)
        {
            if (gender < GrammaticalGender.Masculine || gender > GrammaticalGender.Other)
            {
                throw new ArgumentOutOfRangeException(nameof(gender), "A gender selector requires a defined non-default category.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial typed selector operand; no rendering behavior. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Make construction unambiguous with named factories; retain direct enum range validation. |
#endregion
