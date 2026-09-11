// ============================================================================
// File:     src/localization/LocalizedTextRequest.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2, FR-LC-004/005/009/010/020, ERR-049-004
// Purpose:  Immutable locale-neutral request envelope for later L2 rendering.
// ============================================================================

namespace TacticalDirector.Localization
{
    /// <summary>Locale-neutral procedural-text request assembled outside simulation producers.</summary>
    public readonly struct LocalizedTextRequest
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 request with typed selector operands. |
#endregion
