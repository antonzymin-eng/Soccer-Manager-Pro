// ============================================================================
// File:     src/localization/TextTemplateId.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2/§4.1, FR-LC-004/010/012/014
// Purpose:  Producer-agnostic procedural text template identity.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Generic procedural template identity scoped by producer and local ordinal.</summary>
    public readonly struct TextTemplateId : IEquatable<TextTemplateId>
    {
        /// <summary>Creates a template identity with a positive producer tag and non-negative ordinal.</summary>
        public TextTemplateId(int producerTag, int localOrdinal)
        {
            if (producerTag <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(producerTag), "Producer tag must be positive.");
            }

            if (localOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localOrdinal), "Local ordinal cannot be negative.");
            }

            ProducerTag = producerTag;
            LocalOrdinal = localOrdinal;
        }

        /// <summary>Gets the producer-family namespace tag.</summary>
        public int ProducerTag { get; }

        /// <summary>Gets the producer's local template ordinal.</summary>
        public int LocalOrdinal { get; }

        /// <summary>Gets whether this value is a constructed template identity.</summary>
        public bool IsValid => ProducerTag > 0 && LocalOrdinal >= 0;

        /// <inheritdoc />
        public bool Equals(TextTemplateId other)
        {
            return ProducerTag == other.ProducerTag && LocalOrdinal == other.LocalOrdinal;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TextTemplateId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LocalizationHash.Combine(ProducerTag, LocalOrdinal);
        }

        /// <summary>Compares two template identities.</summary>
        public static bool operator ==(TextTemplateId left, TextTemplateId right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two template identities.</summary>
        public static bool operator !=(TextTemplateId left, TextTemplateId right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 producer-scoped template identity. |
#endregion
