// File:     src/localization/LocalizedTextRequest.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §2.2/§3.1, FR-LC-004/010/014/020, Code Standards #20
// Purpose:  Immutable producer-agnostic procedural-text request assembled outside simulation code.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Generic procedural localization request. The selection value is carried verbatim from its producer.
    /// </summary>
    public readonly struct LocalizedTextRequest
    {
        /// <summary>
        /// Creates a procedural text request from a generic template identity, raw selection value and named slots.
        /// </summary>
        public LocalizedTextRequest(
            TextTemplateId id,
            ulong selectionDraw,
            in NamedSlotSet slots,
            bool hasCitedEpisode = false,
            int citationKind = 0)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("LocalizedTextRequest requires a valid TextTemplateId.", nameof(id));
            }

            if (hasCitedEpisode && citationKind < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(citationKind), "Citation kind cannot be negative when a citation is present.");
            }

            Id = id;
            SelectionDraw = selectionDraw;
            Slots = slots;
            HasCitedEpisode = hasCitedEpisode;
            CitationKind = citationKind;
        }

        /// <summary>
        /// Gets the producer-scoped template family identity.
        /// </summary>
        public TextTemplateId Id { get; }

        /// <summary>
        /// Gets the producer's locale-independent selection value, carried verbatim as <see cref="ulong"/>.
        /// </summary>
        public ulong SelectionDraw { get; }

        /// <summary>
        /// Gets the immutable, already-formatted named slots.
        /// </summary>
        public NamedSlotSet Slots { get; }

        /// <summary>
        /// Gets whether a producer-scoped citation clause should be appended.
        /// </summary>
        public bool HasCitedEpisode { get; }

        /// <summary>
        /// Gets the producer's local clause key when <see cref="HasCitedEpisode"/> is true.
        /// </summary>
        public int CitationKind { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-09-04 | —      | Initial generic procedural-text request contract.   |
#endregion
