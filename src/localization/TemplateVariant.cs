// ============================================================================
// File:     src/localization/TemplateVariant.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, §3.5, KD-3, FR-LC-007/008/009
// Purpose:  One authored variant of a template, plain or with one bounded selector.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One authored variant <c>(Id, Index)</c> of a template. A variant is either plain text or declares
    /// exactly one selector (KD-3) whose keyed forms must include that selector's default form.
    /// All text is parsed at construction, so malformed braces fail here, not at render time.
    /// </summary>
    public sealed class TemplateVariant
    {
        private readonly TemplateExpander.Segment[] _plain;
        private readonly TemplateForm[] _forms;
        private readonly TemplateExpander.Segment[][] _formSegments;
        private readonly int _defaultForm;

        /// <summary>Creates a plain variant with no selector.</summary>
        public TemplateVariant(TextTemplateId id, int index, string text)
        {
            Id = RequireId(id);
            Index = RequireIndex(index);
            SelectorKind = SelectorKind.None;
            _plain = TemplateExpander.Parse(text);
        }

        /// <summary>Creates a variant whose form is chosen by the named selector operand.</summary>
        public TemplateVariant(TextTemplateId id, int index, string selectorName, SelectorKind kind, params TemplateForm[] forms)
        {
            Id = RequireId(id);
            Index = RequireIndex(index);
            if (string.IsNullOrWhiteSpace(selectorName))
            {
                throw new ArgumentException("A selector variant requires a selector name.", nameof(selectorName));
            }

            if (kind != SelectorKind.Plural && kind != SelectorKind.Gender)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "A selector variant requires a plural or gender selector.");
            }

            if (forms == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            SelectorName = selectorName;
            SelectorKind = kind;
            _forms = new TemplateForm[forms.Length];
            Array.Copy(forms, _forms, forms.Length);
            _formSegments = new TemplateExpander.Segment[_forms.Length][];
            _defaultForm = -1;
            for (int i = 0; i < _forms.Length; i++)
            {
                if (!_forms[i].IsValid || _forms[i].Kind != kind)
                {
                    throw new ArgumentException("Every form must be valid and match the variant's selector kind.", nameof(forms));
                }

                for (int j = 0; j < i; j++)
                {
                    if (_forms[j].KeyOrdinal == _forms[i].KeyOrdinal)
                    {
                        throw new ArgumentException("A selector variant cannot declare two forms with the same key.", nameof(forms));
                    }
                }

                if (_forms[i].IsDefault)
                {
                    _defaultForm = i;
                }

                _formSegments[i] = TemplateExpander.Parse(_forms[i].Text);
            }

            if (_defaultForm < 0)
            {
                throw new ArgumentException("A selector variant must author its default form (plural Other / gender Unspecified).", nameof(forms));
            }
        }

        /// <summary>Template identity this variant belongs to.</summary>
        public TextTemplateId Id { get; }

        /// <summary>Zero-based variant index within <see cref="Id"/>.</summary>
        public int Index { get; }

        /// <summary>Declared selector kind; <see cref="SelectorKind.None"/> for a plain variant.</summary>
        public SelectorKind SelectorKind { get; }

        /// <summary>Selector operand name in <see cref="LocalizedTextRequest.Selectors"/>; null for a plain variant.</summary>
        public string SelectorName { get; }

        /// <summary>
        /// Chooses the parsed segments for a request. Falls back to the default form when the operand is
        /// absent, lacks the needed field, maps to an undefined category, or has no matching form
        /// (FR-LC-011: never throws for request content).
        /// </summary>
        internal TemplateExpander.Segment[] SelectSegments(in NamedSelectorSet selectors, Func<long, PluralCategory> pluralRule)
        {
            if (SelectorKind == SelectorKind.None)
            {
                return _plain;
            }

            if (!selectors.TryGetValue(SelectorName, out SelectorOperand operand))
            {
                return _formSegments[_defaultForm];
            }

            int key;
            if (SelectorKind == SelectorKind.Plural)
            {
                if (!operand.HasCardinal || pluralRule == null)
                {
                    return _formSegments[_defaultForm];
                }

                PluralCategory category = pluralRule(operand.CardinalValue);
                if (category < PluralCategory.Other || category > PluralCategory.Many)
                {
                    return _formSegments[_defaultForm];
                }

                key = (int)category;
            }
            else
            {
                if (!operand.HasGender)
                {
                    return _formSegments[_defaultForm];
                }

                key = (int)operand.Gender;
            }

            for (int i = 0; i < _forms.Length; i++)
            {
                if (_forms[i].KeyOrdinal == key)
                {
                    return _formSegments[i];
                }
            }

            return _formSegments[_defaultForm];
        }

        private static TextTemplateId RequireId(TextTemplateId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A template variant requires a valid template id.", nameof(id));
            }

            return id;
        }

        private static int RequireIndex(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Variant index cannot be negative.");
            }

            return index;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 template variant with one bounded selector (localization-l2-plan v0.5 §5.1, §5.4, Q2). |
#endregion
