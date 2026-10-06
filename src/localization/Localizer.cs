// ============================================================================
// File:     src/localization/Localizer.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, KD-5, FR-LC-005/007/008/008a/009/010/011,
//           ERR-049-005, ERR-049-006
// Purpose:  The production ILocalizer over an immutable base and optional selected catalogue.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// The production <see cref="ILocalizer"/> (§3.2). Resolution order is selected locale, then base
    /// locale (KD-5). A key, template or clause absent from both catalogues is reachable only for an
    /// identity the caller never admitted; it yields <see cref="string.Empty"/> (or no clause) without
    /// throwing or mutating anything (FR-LC-011, ERR-049-005). Construction proves that every admitted
    /// identity exists in the base catalogue.
    /// </summary>
    /// <remarks>
    /// Immutable after construction. Concurrent reads are safe provided each catalogue's plural rule is
    /// pure and total (a caller contract this type cannot enforce). The renderer draws no random
    /// numbers, advances no tick and writes no state (FR-LC-005).
    /// </remarks>
    public sealed class Localizer : ILocalizer
    {
        private readonly TemplateCatalogue _base;
        private readonly TemplateCatalogue _selected;

        /// <summary>
        /// Creates a localizer. <paramref name="selected"/> may be null or the base catalogue itself for
        /// base-only rendering. Throws <see cref="ArgumentException"/> listing every violation, in ordinal
        /// order, when the base catalogue misses an admitted identity, has a gap in a template's variant
        /// indices or declares a selector, or when the selected catalogue contains rows the base lacks or
        /// a variant index outside the base range.
        /// </summary>
        public Localizer(TemplateCatalogue baseCatalogue, TemplateCatalogue selected, CatalogueCoverage coverage)
        {
            if (baseCatalogue == null)
            {
                throw new ArgumentNullException(nameof(baseCatalogue));
            }

            if (coverage == null)
            {
                throw new ArgumentNullException(nameof(coverage));
            }

            if (baseCatalogue.Locale != LocaleId.BaseLocale)
            {
                throw new ArgumentException(
                    "The base catalogue must use the base locale '" + LocalizationConstants.BASE_LOCALE + "'.",
                    nameof(baseCatalogue));
            }

            if (selected != null && !ReferenceEquals(selected, baseCatalogue) && selected.Locale == LocaleId.BaseLocale)
            {
                throw new ArgumentException(
                    "A selected catalogue other than the base catalogue cannot use the base locale.",
                    nameof(selected));
            }

            _base = baseCatalogue;
            _selected = ReferenceEquals(selected, baseCatalogue) ? null : selected;

            var violations = new List<string>();
            ValidateBase(violations);
            ValidateCoverage(coverage, violations);
            if (_selected != null)
            {
                ValidateSelected(violations);
            }

            if (violations.Count > 0)
            {
                violations.Sort(StringComparer.Ordinal);
                var distinct = new List<string>(violations.Count);
                for (int i = 0; i < violations.Count; i++)
                {
                    if (i == 0 || !string.Equals(violations[i], violations[i - 1], StringComparison.Ordinal))
                    {
                        distinct.Add(violations[i]);
                    }
                }

                throw new ArgumentException("Localizer construction failed:\n" + string.Join("\n", distinct));
            }
        }

        /// <inheritdoc />
        public string Resolve(LocalizationKey key)
        {
            if (_selected != null && _selected.TryGetStatic(key, out string selectedText))
            {
                return selectedText;
            }

            return _base.TryGetStatic(key, out string baseText) ? baseText : string.Empty;
        }

        /// <inheritdoc />
        public string Render(in LocalizedTextRequest request)
        {
            int count = _base.VariantCount(request.Id);
            if (count == 0)
            {
                return string.Empty;
            }

            int index = (int)(request.SelectionDraw % (ulong)count);
            TemplateVariant variant = _selected?.GetVariant(request.Id, index);
            TemplateCatalogue owner = _selected;
            if (variant == null)
            {
                variant = _base.GetVariant(request.Id, index);
                owner = _base;
            }

            TemplateExpander.Segment[] segments = variant.SelectSegments(request.Selectors, owner.PluralRule);
            var output = new StringBuilder();
            TemplateExpander.Expand(segments, request.Slots, output);

            if (request.HasCitedEpisode)
            {
                var clauseKey = new CitationClauseKey(request.Id.ProducerTag, request.CitationKind);
                if ((_selected != null && _selected.TryGetClause(clauseKey, out string clause))
                    || _base.TryGetClause(clauseKey, out clause))
                {
                    output.Append(' ').Append(clause);
                }
            }

            return output.ToString();
        }

        private void ValidateBase(List<string> violations)
        {
            foreach (KeyValuePair<TextTemplateId, TemplateVariant[]> pair in _base.VariantsById)
            {
                TemplateVariant[] sorted = pair.Value;
                for (int i = 0; i < sorted.Length; i++)
                {
                    if (sorted[i].Index != i)
                    {
                        violations.Add("base template " + TemplateCatalogue.Describe(pair.Key)
                            + " variant indices are not contiguous from 0 (missing index "
                            + i.ToString(CultureInfo.InvariantCulture) + ")");
                        break;
                    }
                }

                for (int i = 0; i < sorted.Length; i++)
                {
                    if (sorted[i].SelectorKind != SelectorKind.None)
                    {
                        violations.Add("base template " + TemplateCatalogue.Describe(pair.Key) + " variant "
                            + sorted[i].Index.ToString(CultureInfo.InvariantCulture)
                            + " declares a " + sorted[i].SelectorKind.ToString() + " selector; the base locale declares none");
                    }
                }
            }
        }

        private void ValidateCoverage(CatalogueCoverage coverage, List<string> violations)
        {
            for (int i = 0; i < coverage.StaticKeyCount; i++)
            {
                LocalizationKey key = coverage.StaticKey(i);
                if (!_base.HasStatic(key))
                {
                    violations.Add("base catalogue is missing required static key '" + key.Value + "'");
                }
            }

            for (int i = 0; i < coverage.TemplateIdCount; i++)
            {
                TextTemplateId id = coverage.TemplateId(i);
                if (_base.VariantCount(id) == 0)
                {
                    violations.Add("base catalogue is missing required template " + TemplateCatalogue.Describe(id));
                }
            }

            for (int i = 0; i < coverage.ClauseKeyCount; i++)
            {
                CitationClauseKey key = coverage.ClauseKey(i);
                if (!_base.HasClause(key))
                {
                    violations.Add("base catalogue is missing required clause " + TemplateCatalogue.Describe(key));
                }
            }
        }

        private void ValidateSelected(List<string> violations)
        {
            string prefix = "selected catalogue '" + _selected.Locale.Value + "' ";
            foreach (LocalizationKey key in _selected.StaticKeys)
            {
                if (!_base.HasStatic(key))
                {
                    violations.Add(prefix + "has static key '" + key.Value + "' absent from the base catalogue");
                }
            }

            foreach (KeyValuePair<TextTemplateId, TemplateVariant[]> pair in _selected.VariantsById)
            {
                int baseCount = _base.VariantCount(pair.Key);
                if (baseCount == 0)
                {
                    violations.Add(prefix + "has template " + TemplateCatalogue.Describe(pair.Key) + " absent from the base catalogue");
                    continue;
                }

                TemplateVariant[] sorted = pair.Value;
                for (int i = 0; i < sorted.Length; i++)
                {
                    if (sorted[i].Index >= baseCount)
                    {
                        violations.Add(prefix + "template " + TemplateCatalogue.Describe(pair.Key) + " variant "
                            + sorted[i].Index.ToString(CultureInfo.InvariantCulture)
                            + " is outside the base range of " + baseCount.ToString(CultureInfo.InvariantCulture) + " variants");
                    }
                }
            }

            foreach (CitationClauseKey key in _selected.ClauseKeys)
            {
                if (!_base.HasClause(key))
                {
                    violations.Add(prefix + "has clause " + TemplateCatalogue.Describe(key) + " absent from the base catalogue");
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 production ILocalizer: KD-5 fallback, coverage at construction, ERR-049-005 terminal results, single-pass expansion (localization-l2-plan v0.5 §5). |
#endregion
