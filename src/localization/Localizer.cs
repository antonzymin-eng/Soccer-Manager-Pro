// File:     src/localization/Localizer.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, FR-LC-007-011, ERR-049-005/006
// Purpose:  Production display-side localizer with base-count selection and fail-safe fallback.

using System;
using System.Collections.Generic;

namespace TacticalDirector.Localization
{
    /// <summary>Immutable production implementation of the existing two-method localization seam.</summary>
    public sealed class Localizer : ILocalizer
    {
        private readonly TemplateCatalogue _baseCatalogue;
        private readonly TemplateCatalogue _selected;

        /// <summary>
        /// Validates base indices, selector restrictions, caller coverage and selected-locale identities.
        /// Null selected content means base-only rendering; null coverage is a configuration error.
        /// Concurrent reads require the catalogue's supplied plural rule to be pure and total.
        /// </summary>
        public Localizer(TemplateCatalogue baseCatalogue, TemplateCatalogue selected, CatalogueCoverage coverage)
        {
            if (baseCatalogue == null)
            {
                throw new ArgumentException("Base catalogue is required.", nameof(baseCatalogue));
            }

            if (coverage == null)
            {
                throw new ArgumentException("Explicit catalogue coverage is required.", nameof(coverage));
            }

            var errors = new List<string>();
            baseCatalogue.ValidateBase(errors);
            coverage.Validate(baseCatalogue, errors);
            if (selected != null && !ReferenceEquals(selected, baseCatalogue))
            {
                selected.ValidateTranslation(baseCatalogue, errors);
            }

            CatalogueValidation.ThrowIfAny(errors);
            _baseCatalogue = baseCatalogue;
            _selected = selected ?? baseCatalogue;
        }

        /// <inheritdoc/>
        public string Resolve(LocalizationKey key)
        {
            if (_selected.TryGetStatic(key, out string text) || _baseCatalogue.TryGetStatic(key, out text))
            {
                return text;
            }

            // ERR-049-005: required keys cannot reach this path; unknown keys never expose key text.
            return string.Empty;
        }

        /// <inheritdoc/>
        public string Render(in LocalizedTextRequest request)
        {
            int count = _baseCatalogue.VariantCount(request.Id);
            if (count == 0)
            {
                return string.Empty;
            }

            int index = (int)(request.SelectionDraw % (ulong)count);
            TemplateCatalogue content = _selected;
            if (!content.TryGetVariant(request.Id, index, out TemplateVariant variant))
            {
                content = _baseCatalogue;
                content.TryGetVariant(request.Id, index, out variant);
            }

            string text = content.Expand(variant, request);
            if (request.HasCitedEpisode)
            {
                var key = new TemplateCatalogue.ClauseKey(request.Id.ProducerTag, request.CitationKind);
                if (_selected.TryGetClause(key, out string clause) || _baseCatalogue.TryGetClause(key, out clause))
                {
                    text += " " + clause;
                }
            }

            return text;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial L2 localizer and production terminal paths. |
#endregion
