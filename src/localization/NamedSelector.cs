// ============================================================================
// File:     src/localization/NamedSelector.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-004/009, ERR-049-004
// Purpose:  Immutable name-to-typed-selector entry.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>One immutable named typed selector operand.</summary>
    public readonly struct NamedSelector : IEquatable<NamedSelector>
    {
        /// <summary>Creates a named selector entry.</summary>
        public NamedSelector(string name, SelectorOperand operand)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Selector name must be non-empty.", nameof(name));
            }

            if (!operand.IsValid)
            {
                throw new ArgumentException("Selector operand must carry a typed value.", nameof(operand));
            }

            Name = name;
            Operand = operand;
        }

        /// <summary>Gets the stable selector name.</summary>
        public string Name { get; }

        /// <summary>Gets the typed selector operand.</summary>
        public SelectorOperand Operand { get; }

        /// <summary>Gets whether this value is a constructed selector entry.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Name) && Operand.IsValid;

        /// <inheritdoc />
        public bool Equals(NamedSelector other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal) && Operand.Equals(other.Operand);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NamedSelector other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LocalizationHash.Combine(LocalizationHash.StringOrdinal(Name), Operand.GetHashCode());
        }

        /// <summary>Compares two named selectors by value.</summary>
        public static bool operator ==(NamedSelector left, NamedSelector right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two named selectors by value.</summary>
        public static bool operator !=(NamedSelector left, NamedSelector right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 named selector entry. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Add value equality operators. |
#endregion
