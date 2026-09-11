// ============================================================================
// File:     src/localization/LocaleId.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §4.2, FR-LC-011/018
// Purpose:  Immutable display-locale identity without expanding locale conformance policy.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Stable client-local display-locale identity.</summary>
    public readonly struct LocaleId : IEquatable<LocaleId>
    {
        private readonly string _value;

        /// <summary>Creates a locale identity from a non-empty opaque code.</summary>
        public LocaleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("LocaleId requires a non-empty locale code.", nameof(value));
            }

            _value = value;
        }

        /// <summary>Gets the opaque locale code.</summary>
        public string Value => _value ?? string.Empty;

        /// <summary>Gets whether this value is a constructed locale identity.</summary>
        public bool IsValid => !string.IsNullOrEmpty(_value);

        /// <summary>Gets the fixed base-locale identity.</summary>
        public static LocaleId BaseLocale => new LocaleId(LocalizationConstants.BASE_LOCALE);

        /// <inheritdoc />
        public bool Equals(LocaleId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LocaleId other && Equals(other);
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial minimal L1 locale identity. |
#endregion
