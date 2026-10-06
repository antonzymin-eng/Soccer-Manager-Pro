// ============================================================================
// File:     src/localization/TemplateCatalogue.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, §4.2, KD-3, KD-5, FR-LC-007/008/009/010
// Purpose:  One locale's immutable in-memory static rows, template variants and clauses.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One locale's immutable content: static rows, template variants by <c>(Id, Index)</c>, and
    /// producer-scoped clauses, plus an optional plural rule. Checks that depend on the base catalogue
    /// (index contiguity, coverage, orphans) run in <see cref="Localizer"/>.
    /// </summary>
    /// <remarks>
    /// The plural rule must be a pure, total function of its argument. Concurrent reads are safe only
    /// under that caller contract; this type cannot enforce it.
    /// </remarks>
    public sealed class TemplateCatalogue
    {
        private readonly Dictionary<LocalizationKey, string> _static;
        private readonly Dictionary<TextTemplateId, TemplateVariant[]> _variants;
        private readonly Dictionary<CitationClauseKey, string> _clauses;
        private readonly Func<long, PluralCategory> _pluralRule;

        /// <summary>
        /// Creates a catalogue. Rejects default rows, duplicate keys, duplicate <c>(Id, Index)</c> pairs,
        /// and a plural selector without a plural rule. Caller arrays are copied.
        /// </summary>
        public TemplateCatalogue(
            LocaleId locale,
            StaticTextEntry[] staticTexts,
            TemplateVariant[] variants,
            CitationClause[] clauses,
            Func<long, PluralCategory> pluralRule = null)
        {
            if (!locale.IsValid)
            {
                throw new ArgumentException("A catalogue requires a valid locale.", nameof(locale));
            }

            if (staticTexts == null)
            {
                throw new ArgumentNullException(nameof(staticTexts));
            }

            if (variants == null)
            {
                throw new ArgumentNullException(nameof(variants));
            }

            if (clauses == null)
            {
                throw new ArgumentNullException(nameof(clauses));
            }

            Locale = locale;
            _pluralRule = pluralRule;

            _static = new Dictionary<LocalizationKey, string>(staticTexts.Length);
            for (int i = 0; i < staticTexts.Length; i++)
            {
                if (!staticTexts[i].IsValid)
                {
                    throw new ArgumentException("A catalogue cannot contain a default static row.", nameof(staticTexts));
                }

                if (_static.ContainsKey(staticTexts[i].Key))
                {
                    throw new ArgumentException("Duplicate static key '" + staticTexts[i].Key.Value + "'.", nameof(staticTexts));
                }

                _static.Add(staticTexts[i].Key, staticTexts[i].Text);
            }

            var byId = new Dictionary<TextTemplateId, List<TemplateVariant>>();
            for (int i = 0; i < variants.Length; i++)
            {
                TemplateVariant variant = variants[i] ?? throw new ArgumentException("A catalogue cannot contain a null variant.", nameof(variants));
                if (variant.SelectorKind == SelectorKind.Plural && pluralRule == null)
                {
                    throw new ArgumentException(
                        "Template " + Describe(variant.Id) + " declares a plural selector, but the catalogue has no plural rule.",
                        nameof(pluralRule));
                }

                if (!byId.TryGetValue(variant.Id, out List<TemplateVariant> list))
                {
                    list = new List<TemplateVariant>();
                    byId.Add(variant.Id, list);
                }

                list.Add(variant);
            }

            // Stored sorted by index rather than slot-indexed, so an authored index never sizes an
            // allocation; contiguity against the base catalogue is checked by Localizer.
            _variants = new Dictionary<TextTemplateId, TemplateVariant[]>(byId.Count);
            foreach (KeyValuePair<TextTemplateId, List<TemplateVariant>> pair in byId)
            {
                TemplateVariant[] sorted = pair.Value.ToArray();
                Array.Sort(sorted, CompareByIndex);
                for (int i = 1; i < sorted.Length; i++)
                {
                    if (sorted[i].Index == sorted[i - 1].Index)
                    {
                        throw new ArgumentException(
                            "Duplicate variant index " + sorted[i].Index.ToString(CultureInfo.InvariantCulture)
                            + " for template " + Describe(pair.Key) + ".",
                            nameof(variants));
                    }
                }

                _variants.Add(pair.Key, sorted);
            }

            _clauses = new Dictionary<CitationClauseKey, string>(clauses.Length);
            for (int i = 0; i < clauses.Length; i++)
            {
                if (!clauses[i].IsValid)
                {
                    throw new ArgumentException("A catalogue cannot contain a default clause row.", nameof(clauses));
                }

                if (_clauses.ContainsKey(clauses[i].Key))
                {
                    throw new ArgumentException("Duplicate clause " + Describe(clauses[i].Key) + ".", nameof(clauses));
                }

                _clauses.Add(clauses[i].Key, clauses[i].Text);
            }
        }

        /// <summary>Locale this catalogue supplies.</summary>
        public LocaleId Locale { get; }

        internal Func<long, PluralCategory> PluralRule => _pluralRule;

        internal IEnumerable<LocalizationKey> StaticKeys => _static.Keys;

        /// <summary>Each template id with its variants sorted by index (possibly sparse).</summary>
        internal IEnumerable<KeyValuePair<TextTemplateId, TemplateVariant[]>> VariantsById => _variants;

        internal IEnumerable<CitationClauseKey> ClauseKeys => _clauses.Keys;

        internal bool TryGetStatic(LocalizationKey key, out string text) => _static.TryGetValue(key, out text);

        internal bool HasStatic(LocalizationKey key) => _static.ContainsKey(key);

        internal bool HasClause(CitationClauseKey key) => _clauses.ContainsKey(key);

        internal bool TryGetClause(CitationClauseKey key, out string text) => _clauses.TryGetValue(key, out text);

        /// <summary>Number of variants authored for <paramref name="id"/>; zero when absent.</summary>
        internal int VariantCount(TextTemplateId id)
        {
            return _variants.TryGetValue(id, out TemplateVariant[] sorted) ? sorted.Length : 0;
        }

        /// <summary>The variant at <paramref name="index"/> for <paramref name="id"/>, or null if not authored.</summary>
        internal TemplateVariant GetVariant(TextTemplateId id, int index)
        {
            if (!_variants.TryGetValue(id, out TemplateVariant[] sorted))
            {
                return null;
            }

            int low = 0;
            int high = sorted.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                int found = sorted[middle].Index;
                if (found == index)
                {
                    return sorted[middle];
                }

                if (found < index)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return null;
        }

        private static int CompareByIndex(TemplateVariant left, TemplateVariant right)
        {
            return left.Index.CompareTo(right.Index);
        }

        internal static string Describe(TextTemplateId id)
        {
            return "(" + id.ProducerTag.ToString(CultureInfo.InvariantCulture) + ", "
                + id.LocalOrdinal.ToString(CultureInfo.InvariantCulture) + ")";
        }

        internal static string Describe(CitationClauseKey key)
        {
            return "(" + key.ProducerTag.ToString(CultureInfo.InvariantCulture) + ", "
                + key.CitationKind.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 immutable per-locale catalogue (localization-l2-plan v0.5 §5.1, §5.2). |
#endregion
