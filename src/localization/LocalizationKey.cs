// File:     src/localization/LocalizationKey.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §2.1/§2.2, FR-LC-002/003, Code Standards #20
// Purpose:  Immutable stable identity for one static user-facing string.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Stable catalogue key for a static localized string.
    /// </summary>
    public readonly struct LocalizationKey : IEquatable<LocalizationKey>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a stable localization key.
        /// </summary>
        public LocalizationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("LocalizationKey requires a non-empty key.", nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// Gets the key text. A default instance returns an empty string and is invalid catalogue input.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// Returns whether this value contains a usable catalogue key.
        /// </summary>
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
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Compares two keys using ordinal identity.
        /// </summary>
        public static bool operator ==(LocalizationKey left, LocalizationKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two keys using ordinal identity.
        /// </summary>
        public static bool operator !=(LocalizationKey left, LocalizationKey right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                      |
// | 1.0     | 2026-09-04 | —      | Initial static localization key contract.  |
#endregion
