// ============================================================================
// File:     src/localization/CitationClauseKey.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, FR-LC-010
// Purpose:  Producer-scoped identity of a citation clause.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Producer-scoped clause identity <c>(ProducerTag, CitationKind)</c> (FR-LC-010), so two producers'
    /// citation kinds never collide.
    /// </summary>
    public readonly struct CitationClauseKey : IEquatable<CitationClauseKey>
    {
        /// <summary>Creates a clause key.</summary>
        public CitationClauseKey(int producerTag, int citationKind)
        {
            if (producerTag <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(producerTag), "Producer tag must be positive.");
            }

            ProducerTag = producerTag;
            CitationKind = citationKind;
        }

        /// <summary>Producer namespace, as in <see cref="TextTemplateId.ProducerTag"/>.</summary>
        public int ProducerTag { get; }

        /// <summary>Producer-native citation kind ordinal.</summary>
        public int CitationKind { get; }

        /// <summary>True for a key built through the constructor.</summary>
        public bool IsValid => ProducerTag > 0;

        /// <inheritdoc />
        public bool Equals(CitationClauseKey other)
        {
            return ProducerTag == other.ProducerTag && CitationKind == other.CitationKind;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CitationClauseKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LocalizationHash.Combine(ProducerTag, CitationKind);
        }

        /// <summary>Compares two clause keys by value.</summary>
        public static bool operator ==(CitationClauseKey left, CitationClauseKey right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two clause keys by value.</summary>
        public static bool operator !=(CitationClauseKey left, CitationClauseKey right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 producer-scoped clause key (localization-l2-plan v0.5 §5.1). |
#endregion
