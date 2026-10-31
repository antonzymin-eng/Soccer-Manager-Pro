// ============================================================================
// File:     src/localization/LocalizationKey.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.1-§2.2, FR-LC-002/003
// Purpose:  Immutable stable identity for one static user-facing string.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Stable catalogue identity for one static localized string.</summary>
    public readonly struct LocalizationKey : IEquatable<LocalizationKey>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a non-empty static localization key. Keys are ordinal and case-sensitive; surrounding
        /// whitespace is invalid rather than normalized so authoring mistakes cannot silently alias a key.
        /// </summary>
        public LocalizationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("LocalizationKey requires a non-empty key.", nameof(value));
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("LocalizationKey cannot contain surrounding whitespace.", nameof(value));
            }

            _value = value;
        }

        /// <summary>Gets the key text, or an empty string for the invalid default value.</summary>
        public string Value => _value ?? string.Empty;

        /// <summary>Gets whether this value is a constructed catalogue identity.</summary>
        public bool IsValid => !string.IsNullOrEmpty(_value);

        /// <inheritdoc />
        public bool Equals(LocalizationKey other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LocalizationKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LocalizationHash.StringOrdinal(_value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>Compares two keys using ordinal identity.</summary>
        public static bool operator ==(LocalizationKey left, LocalizationKey right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two keys using ordinal identity.</summary>
        public static bool operator !=(LocalizationKey left, LocalizationKey right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 static-key contract. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Freeze ordinal case-sensitive identity; reject surrounding whitespace. |
#endregion
