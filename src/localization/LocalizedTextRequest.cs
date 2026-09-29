// ============================================================================
// File:     src/localization/LocalizedTextRequest.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2, FR-LC-004/005/009/010/020, ERR-049-004
// Purpose:  Immutable locale-neutral request envelope for later L2 rendering.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Locale-neutral procedural-text request assembled outside simulation producers.</summary>
    public readonly struct LocalizedTextRequest : IEquatable<LocalizedTextRequest>
    {
        /// <summary>Creates a fully typed procedural-text request.</summary>
        public LocalizedTextRequest(
            TextTemplateId id,
            ulong selectionDraw,
            NamedSlotSet slots,
            NamedSelectorSet selectors,
            bool hasCitedEpisode,
            int citationKind)
        {
            Id = id;
            SelectionDraw = selectionDraw;
            Slots = slots;
            Selectors = selectors;
            HasCitedEpisode = hasCitedEpisode;
            CitationKind = citationKind;
        }

        /// <summary>Gets the producer-scoped generic template identity.</summary>
        public TextTemplateId Id { get; }

        /// <summary>Gets the producer-owned deterministic locale-independent selection value verbatim.</summary>
        public ulong SelectionDraw { get; }

        /// <summary>Gets the immutable preformatted string slots.</summary>
        public NamedSlotSet Slots { get; }

        /// <summary>Gets immutable typed locale-neutral selector operands.</summary>
        public NamedSelectorSet Selectors { get; }

        /// <summary>Gets whether this request cites a producer episode/event.</summary>
        public bool HasCitedEpisode { get; }

        /// <summary>Gets the producer-native citation-kind ordinal; the producer namespace is <see cref="Id"/>.</summary>
        public int CitationKind { get; }

        /// <inheritdoc />
        public bool Equals(LocalizedTextRequest other)
        {
            return Id.Equals(other.Id)
                && SelectionDraw == other.SelectionDraw
                && Slots.Equals(other.Slots)
                && Selectors.Equals(other.Selectors)
                && HasCitedEpisode == other.HasCitedEpisode
                && CitationKind == other.CitationKind;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LocalizedTextRequest other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = LocalizationHash.Combine(Id.GetHashCode(), LocalizationHash.Long(unchecked((long)SelectionDraw)));
            hash = LocalizationHash.Combine(hash, Slots.GetHashCode());
            hash = LocalizationHash.Combine(hash, Selectors.GetHashCode());
            hash = LocalizationHash.Combine(hash, HasCitedEpisode ? 1 : 0);
            return LocalizationHash.Combine(hash, CitationKind);
        }

        /// <summary>Compares two requests by value.</summary>
        public static bool operator ==(LocalizedTextRequest left, LocalizedTextRequest right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two requests by value.</summary>
        public static bool operator !=(LocalizedTextRequest left, LocalizedTextRequest right)
        {
            return !left.Equals(right);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 request with typed selector operands. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Add explicit deterministic value equality/hash and operators. |
#endregion
