// File:     src/localization/LocaleId.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §4.2, FR-LC-011/018, Appendix A, Code Standards #20
// Purpose:  Immutable locale identity for display-time catalogue selection and fallback.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Stable display-locale identity. Locale selection is client-local and outside deterministic state.
    /// </summary>
    public readonly struct LocaleId : IEquatable<LocaleId>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a locale identity from a non-empty stable code such as <c>en</c>.
        /// </summary>
        public LocaleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("LocaleId requires a non-empty locale code.", nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// Gets the stable locale code. A default instance returns an empty string and is not valid catalogue input.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// Gets the fixed base locale defined by #49 Appendix A.
        /// </summary>
        public static LocaleId Base => new LocaleId(LocalizationConstants.BASE_LOCALE);

        /// <summary>
        /// Returns whether this value contains a usable locale code.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(_value);

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
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Compares two locale identities by ordinal code equality.
        /// </summary>
        public static bool operator ==(LocaleId left, LocaleId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two locale identities by ordinal code inequality.
        /// </summary>
        public static bool operator !=(LocaleId left, LocaleId right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                      |
// | 1.0     | 2026-09-04 | —      | Initial immutable locale identity.         |
#endregion
