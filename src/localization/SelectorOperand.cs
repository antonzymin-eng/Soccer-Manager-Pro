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
        /// <summary>Creates a cardinal-only selector operand.</summary>
        public SelectorOperand(long cardinalValue)
        {
            HasCardinal = true;
            CardinalValue = cardinalValue;
            HasGender = false;
            Gender = GrammaticalGender.Unspecified;
        }

        /// <summary>Creates a gender-only selector operand.</summary>
        public SelectorOperand(GrammaticalGender gender)
        {
            ValidateGender(gender);
            HasCardinal = false;
            CardinalValue = 0;
            HasGender = true;
            Gender = gender;
        }

        /// <summary>Creates a selector operand carrying both cardinal and gender values.</summary>
        public SelectorOperand(long cardinalValue, GrammaticalGender gender)
        {
            ValidateGender(gender);
            HasCardinal = true;
            CardinalValue = cardinalValue;
            HasGender = true;
            Gender = gender;
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
#endregion
