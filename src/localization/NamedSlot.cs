// ============================================================================
// File:     src/localization/NamedSlot.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2, FR-LC-004/009/014
// Purpose:  Immutable name-to-preformatted-string slot entry.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>One immutable named display-string slot.</summary>
    public readonly struct NamedSlot : IEquatable<NamedSlot>
    {
        /// <summary>Creates a named slot whose value was formatted by the boundary adapter.</summary>
        public NamedSlot(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Slot name must be non-empty.", nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = name;
            Value = value;
        }

        /// <summary>Gets the stable placeholder name.</summary>
        public string Name { get; }

        /// <summary>Gets the already-formatted display value.</summary>
        public string Value { get; }

        /// <summary>Gets whether this value is a constructed slot.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Name) && Value != null;

        /// <inheritdoc />
        public bool Equals(NamedSlot other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NamedSlot other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LocalizationHash.Combine(
                LocalizationHash.StringOrdinal(Name),
                LocalizationHash.StringOrdinal(Value));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 named slot entry. |
#endregion
