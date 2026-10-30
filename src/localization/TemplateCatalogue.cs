// File:     src/localization/TemplateCatalogue.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-007/008/008a/009/010
// Purpose:  Own immutable per-locale static, indexed variant and producer-scoped clause rows.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.Localization
{
    /// <summary>Host-free, immutable content for one locale; supplied arrays are never retained.</summary>
    public sealed class TemplateCatalogue
    {
        private readonly Dictionary<LocalizationKey, string> _staticRows;
        private readonly Dictionary<TextTemplateId, Dictionary<int, TemplateVariant>> _variants;
        private readonly Dictionary<ClauseKey, string> _clauses;
        private readonly Func<long, PluralCategory> _pluralRule;

        /// <summary>
        /// Copies content and rejects malformed or duplicate rows. A plural selector requires a pure,
        /// total rule; concurrent rendering is safe only when the caller satisfies that contract.
        /// Real locale rules, content loaders and locale validation are deferred to Wave 8.
        /// </summary>
        public TemplateCatalogue(LocaleId locale, StaticRow[] staticRows = null, VariantRow[] variants = null, ClauseRow[] clauses = null, Func<long, PluralCategory> pluralRule = null)
        {
            Locale = locale;
            _pluralRule = pluralRule;
            _staticRows = new Dictionary<LocalizationKey, string>();
            _variants = new Dictionary<TextTemplateId, Dictionary<int, TemplateVariant>>();
            _clauses = new Dictionary<ClauseKey, string>();
            var errors = new List<string>();
            if (!locale.IsValid)
            {
                errors.Add("locale: invalid identity");
            }

            foreach (StaticRow row in staticRows ?? Array.Empty<StaticRow>())
            {
                string identity = CatalogueValidation.StaticIdentity(row.Key);
                if (!row.Key.IsValid || row.Text == null)
                {
                    errors.Add(identity + ": invalid row");
                }
                else if (_staticRows.ContainsKey(row.Key))
                {
                    errors.Add(identity + ": duplicate row");
                }
                else
                {
                    _staticRows.Add(row.Key, row.Text);
                }
            }

            foreach (VariantRow row in variants ?? Array.Empty<VariantRow>())
            {
                string identity = CatalogueValidation.TemplateIdentity(row.Id) + "/" + row.Index.ToString(CultureInfo.InvariantCulture);
                if (!row.Id.IsValid || row.Index < 0 || row.Variant == null)
                {
                    errors.Add(identity + ": invalid variant row");
                    continue;
                }

                if (!_variants.TryGetValue(row.Id, out Dictionary<int, TemplateVariant> indexed))
                {
                    indexed = new Dictionary<int, TemplateVariant>();
                    _variants.Add(row.Id, indexed);
                }

                if (indexed.ContainsKey(row.Index))
                {
                    errors.Add(identity + ": duplicate row");
                }
                else
                {
                    indexed.Add(row.Index, row.Variant);
                }

                if (row.Variant.HasPluralSelector && pluralRule == null)
                {
                    errors.Add(identity + ": plural selector requires a rule");
                }
            }

            foreach (ClauseRow row in clauses ?? Array.Empty<ClauseRow>())
            {
                string identity = CatalogueValidation.ClauseIdentity(row.Key);
                if (!row.Key.IsValid || row.Text == null)
                {
                    errors.Add(identity + ": invalid row");
                }
                else if (_clauses.ContainsKey(row.Key))
                {
                    errors.Add(identity + ": duplicate row");
                }
                else
                {
                    _clauses.Add(row.Key, row.Text);
                }
            }

            CatalogueValidation.ThrowIfAny(errors);
        }

        /// <summary>Gets the immutable catalogue locale identity.</summary>
        public LocaleId Locale { get; }

        internal bool TryGetStatic(LocalizationKey key, out string text) => _staticRows.TryGetValue(key, out text);
        internal bool TryGetClause(ClauseKey key, out string text) => _clauses.TryGetValue(key, out text);
        internal int VariantCount(TextTemplateId id) => _variants.TryGetValue(id, out Dictionary<int, TemplateVariant> indexed) ? indexed.Count : 0;
        internal bool TryGetVariant(TextTemplateId id, int index, out TemplateVariant variant)
        {
            variant = null;
            return _variants.TryGetValue(id, out Dictionary<int, TemplateVariant> indexed) && indexed.TryGetValue(index, out variant);
        }

        internal string Expand(TemplateVariant variant, in LocalizedTextRequest request)
        {
            return TemplateExpander.Expand(variant.Select(request.Selectors, _pluralRule), request.Slots);
        }

        internal void ValidateBase(List<string> errors)
        {
            if (Locale != LocaleId.BaseLocale)
            {
                errors.Add("locale: base catalogue must be en");
            }

            foreach (KeyValuePair<TextTemplateId, Dictionary<int, TemplateVariant>> entry in _variants)
            {
                string identity = CatalogueValidation.TemplateIdentity(entry.Key);
                for (int index = 0; index < entry.Value.Count; index++)
                {
                    if (!entry.Value.ContainsKey(index))
                    {
                        errors.Add(identity + ": missing base index " + index.ToString(CultureInfo.InvariantCulture));
                    }
                }

                foreach (KeyValuePair<int, TemplateVariant> indexed in entry.Value)
                {
                    if (indexed.Value.HasSelector)
                    {
                        errors.Add(identity + "/" + indexed.Key.ToString(CultureInfo.InvariantCulture) + ": base selector forbidden");
                    }
                }
            }
        }

        internal void ValidateTranslation(TemplateCatalogue baseCatalogue, List<string> errors)
        {
            foreach (LocalizationKey key in _staticRows.Keys)
            {
                if (!baseCatalogue._staticRows.ContainsKey(key))
                {
                    errors.Add(CatalogueValidation.StaticIdentity(key) + ": orphan translation");
                }
            }

            foreach (ClauseKey key in _clauses.Keys)
            {
                if (!baseCatalogue._clauses.ContainsKey(key))
                {
                    errors.Add(CatalogueValidation.ClauseIdentity(key) + ": orphan translation");
                }
            }

            foreach (KeyValuePair<TextTemplateId, Dictionary<int, TemplateVariant>> entry in _variants)
            {
                int count = baseCatalogue.VariantCount(entry.Key);
                string identity = CatalogueValidation.TemplateIdentity(entry.Key);
                if (count == 0)
                {
                    errors.Add(identity + ": orphan translation");
                }
                else
                {
                    foreach (int index in entry.Value.Keys)
                    {
                        if (index >= count)
                        {
                            errors.Add(identity + "/" + index.ToString(CultureInfo.InvariantCulture) + ": outside base range");
                        }
                    }
                }
            }
        }

        /// <summary>One static row. Text is opaque, including composite-format braces.</summary>
        public readonly struct StaticRow
        {
            /// <summary>Creates an authored static row.</summary>
            public StaticRow(LocalizationKey key, string text)
            {
                Key = key;
                Text = text;
            }

            /// <summary>Gets the static identity.</summary>
            public LocalizationKey Key { get; }
            /// <summary>Gets the authored text.</summary>
            public string Text { get; }
        }

        /// <summary>One indexed template variant.</summary>
        public readonly struct VariantRow
        {
            /// <summary>Creates a variant row. The catalogue validates identity, index and content.</summary>
            public VariantRow(TextTemplateId id, int index, TemplateVariant variant)
            {
                Id = id;
                Index = index;
                Variant = variant;
            }

            /// <summary>Gets the producer-scoped identity.</summary>
            public TextTemplateId Id { get; }
            /// <summary>Gets the stable base-locale index.</summary>
            public int Index { get; }
            /// <summary>Gets the immutable variant.</summary>
            public TemplateVariant Variant { get; }
        }

        /// <summary>Clause identity scoped by producer, with no producer enum dependency.</summary>
        public readonly struct ClauseKey : IEquatable<ClauseKey>
        {
            /// <summary>Creates a clause identity; citation kinds remain producer-native integers.</summary>
            public ClauseKey(int producerTag, int citationKind)
            {
                if (producerTag <= 0)
                {
                    throw new ArgumentException("Clause producer tag must be positive.", nameof(producerTag));
                }

                ProducerTag = producerTag;
                CitationKind = citationKind;
            }

            /// <summary>Gets the producer namespace.</summary>
            public int ProducerTag { get; }
            /// <summary>Gets the producer-native clause ordinal.</summary>
            public int CitationKind { get; }
            /// <summary>Gets whether this is a constructed identity.</summary>
            public bool IsValid => ProducerTag > 0;

            /// <inheritdoc/>
            public bool Equals(ClauseKey other) => ProducerTag == other.ProducerTag && CitationKind == other.CitationKind;
            /// <inheritdoc/>
            public override bool Equals(object obj) => obj is ClauseKey other && Equals(other);
            /// <inheritdoc/>
            public override int GetHashCode() => LocalizationHash.Combine(ProducerTag, CitationKind);
        }

        /// <summary>One authored clause; appended verbatim without placeholder parsing.</summary>
        public readonly struct ClauseRow
        {
            /// <summary>Creates a clause row.</summary>
            public ClauseRow(ClauseKey key, string text)
            {
                Key = key;
                Text = text;
            }

            /// <summary>Gets the producer-scoped clause identity.</summary>
            public ClauseKey Key { get; }
            /// <summary>Gets the authored clause text.</summary>
            public string Text { get; }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial immutable catalogue and construction validation. |
#endregion
