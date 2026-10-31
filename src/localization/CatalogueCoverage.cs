// File:     src/localization/CatalogueCoverage.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-008a, ERR-049-005
// Purpose:  Copy caller-admitted identities and enforce complete base coverage before rendering.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.Localization
{
    /// <summary>Immutable caller-supplied admission requirements; the core knows no producer roster.</summary>
    public sealed class CatalogueCoverage
    {
        private readonly LocalizationKey[] _staticKeys;
        private readonly TextTemplateId[] _templateIds;
        private readonly TemplateCatalogue.ClauseKey[] _clauseKeys;

        /// <summary>Copies required identities. Empty coverage must be chosen explicitly by the caller.</summary>
        public CatalogueCoverage(LocalizationKey[] staticKeys = null, TextTemplateId[] templateIds = null, TemplateCatalogue.ClauseKey[] clauseKeys = null)
        {
            _staticKeys = staticKeys == null ? Array.Empty<LocalizationKey>() : (LocalizationKey[])staticKeys.Clone();
            _templateIds = templateIds == null ? Array.Empty<TextTemplateId>() : (TextTemplateId[])templateIds.Clone();
            _clauseKeys = clauseKeys == null ? Array.Empty<TemplateCatalogue.ClauseKey>() : (TemplateCatalogue.ClauseKey[])clauseKeys.Clone();
            var errors = new List<string>();
            foreach (LocalizationKey key in _staticKeys)
            {
                if (!key.IsValid)
                {
                    errors.Add(CatalogueValidation.StaticIdentity(key) + ": invalid required identity");
                }
            }

            foreach (TextTemplateId id in _templateIds)
            {
                if (!id.IsValid)
                {
                    errors.Add(CatalogueValidation.TemplateIdentity(id) + ": invalid required identity");
                }
            }

            foreach (TemplateCatalogue.ClauseKey key in _clauseKeys)
            {
                if (!key.IsValid)
                {
                    errors.Add(CatalogueValidation.ClauseIdentity(key) + ": invalid required identity");
                }
            }

            CatalogueValidation.ThrowIfAny(errors);
        }

        internal void Validate(TemplateCatalogue baseCatalogue, List<string> errors)
        {
            foreach (LocalizationKey key in _staticKeys)
            {
                if (!baseCatalogue.TryGetStatic(key, out _))
                {
                    errors.Add(CatalogueValidation.StaticIdentity(key) + ": missing required base row");
                }
            }

            foreach (TextTemplateId id in _templateIds)
            {
                if (baseCatalogue.VariantCount(id) == 0)
                {
                    errors.Add(CatalogueValidation.TemplateIdentity(id) + ": missing required base row");
                }
            }

            foreach (TemplateCatalogue.ClauseKey key in _clauseKeys)
            {
                if (!baseCatalogue.TryGetClause(key, out _))
                {
                    errors.Add(CatalogueValidation.ClauseIdentity(key) + ": missing required base row");
                }
            }
        }
    }

    internal static class CatalogueValidation
    {
        internal static string StaticIdentity(LocalizationKey key) => "static:" + key.Value;
        internal static string TemplateIdentity(TextTemplateId id) => "template:" + id.ProducerTag.ToString(CultureInfo.InvariantCulture) + "/" + id.LocalOrdinal.ToString(CultureInfo.InvariantCulture);
        internal static string ClauseIdentity(TemplateCatalogue.ClauseKey key) => "clause:" + key.ProducerTag.ToString(CultureInfo.InvariantCulture) + "/" + key.CitationKind.ToString(CultureInfo.InvariantCulture);
        internal static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0)
            {
                return;
            }

            errors.Sort(StringComparer.Ordinal);
            throw new ArgumentException(string.Join("\n", errors));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial caller-admitted coverage, ERR-049-005. |
#endregion
