// File:     src/localization/TextTemplateId.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §2.2/§4.1, FR-LC-004/012/014, Code Standards #20
// Purpose:  Producer-agnostic template identity that keeps the localization core free of sim enum types.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Generic identity for one producer-owned template family: <c>(producerTag, localOrdinal)</c>.
    /// </summary>
    public readonly struct TextTemplateId : IEquatable<TextTemplateId>
    {
        /// <summary>
        /// Creates a template identity. Producer tags are positive; local ordinals are zero or greater.
        /// </summary>
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

        /// <summary>
        /// Gets the producer-family tag that scopes template and clause identities.
        /// </summary>
        public int ProducerTag { get; }

        /// <summary>
        /// Gets the producer's local template-family ordinal.
        /// </summary>
        public int LocalOrdinal { get; }

        /// <summary>
        /// Returns whether this value is a constructed template identity rather than <c>default</c>.
        /// </summary>
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
            unchecked
            {
                return (ProducerTag * 397) ^ LocalOrdinal;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ProducerTag.ToString() + ":" + LocalOrdinal.ToString();
        }

        /// <summary>
        /// Compares two template identities by producer tag and local ordinal.
        /// </summary>
        public static bool operator ==(TextTemplateId left, TextTemplateId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two template identities by producer tag and local ordinal.
        /// </summary>
        public static bool operator !=(TextTemplateId left, TextTemplateId right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                            |
// | 1.0     | 2026-09-04 | —      | Initial generic producer-scoped template id.    |
#endregion
