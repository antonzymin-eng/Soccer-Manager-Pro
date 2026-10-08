// File:     src/localization/TemplateVariant.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 KD-3, FR-LC-009/011
// Purpose:  Immutable parsed variant with at most one plural or gender selector.

using System;
using System.Collections.Generic;

namespace TacticalDirector.Localization
{
    /// <summary>A plain template or a bounded selector with a mandatory fallback form.</summary>
    public sealed class TemplateVariant
    {
        private readonly string _selectorName;
        private readonly TemplateSelectorKind _kind;
        private readonly ParsedTemplate _defaultForm;
        private readonly Dictionary<int, ParsedTemplate> _forms;

        /// <summary>Creates a plain variant; malformed braces fail at construction.</summary>
        public TemplateVariant(string text)
        {
            _defaultForm = TemplateExpander.Parse(text);
            _forms = new Dictionary<int, ParsedTemplate>();
        }

        private TemplateVariant(string selectorName, TemplateSelectorKind kind, TemplateForm[] forms)
        {
            if (string.IsNullOrWhiteSpace(selectorName))
            {
                throw new ArgumentException("Selector name must be non-empty.", nameof(selectorName));
            }

            _selectorName = selectorName;
            _kind = kind;
            _forms = new Dictionary<int, ParsedTemplate>();
            var errors = new List<string>();
            foreach (TemplateForm form in forms ?? Array.Empty<TemplateForm>())
            {
                if (form.Text == null || form.Kind != TemplateSelectorKind.None && form.Kind != kind)
                {
                    errors.Add(selectorName + ": invalid or mixed selector form");
                    continue;
                }

                ParsedTemplate parsed;
                try
                {
                    parsed = TemplateExpander.Parse(form.Text);
                }
                catch (ArgumentException exception)
                {
                    errors.Add(selectorName + ": " + exception.Message);
                    continue;
                }

                if (form.Kind == TemplateSelectorKind.None)
                {
                    if (_defaultForm != null)
                    {
                        errors.Add(selectorName + ": duplicate default form");
                    }
                    else
                    {
                        _defaultForm = parsed;
                    }
                }
                else if (_forms.ContainsKey(form.Category))
                {
                    errors.Add(selectorName + ": duplicate category " + form.Category);
                }
                else
                {
                    _forms.Add(form.Category, parsed);
                }
            }

            if (_defaultForm == null)
            {
                errors.Add(selectorName + ": missing default form");
            }

            CatalogueValidation.ThrowIfAny(errors);
        }

        /// <summary>Creates one plural selector with keyed forms and one default form.</summary>
        public static TemplateVariant ForPlural(string selectorName, params TemplateForm[] forms)
        {
            return new TemplateVariant(selectorName, TemplateSelectorKind.Plural, forms);
        }

        /// <summary>Creates one gender selector with keyed forms and one default form.</summary>
        public static TemplateVariant ForGender(string selectorName, params TemplateForm[] forms)
        {
            return new TemplateVariant(selectorName, TemplateSelectorKind.Gender, forms);
        }

        internal bool HasSelector => _kind != TemplateSelectorKind.None;
        internal bool HasPluralSelector => _kind == TemplateSelectorKind.Plural;

        internal ParsedTemplate Select(in NamedSelectorSet selectors, Func<long, PluralCategory> pluralRule)
        {
            if (!HasSelector || !selectors.TryGetValue(_selectorName, out SelectorOperand operand))
            {
                return _defaultForm;
            }

            int category;
            if (_kind == TemplateSelectorKind.Plural && operand.HasCardinal)
            {
                PluralCategory plural = pluralRule(operand.CardinalValue);
                if (plural < PluralCategory.Other || plural > PluralCategory.Many)
                {
                    return _defaultForm;
                }

                category = (int)plural;
            }
            else if (_kind == TemplateSelectorKind.Gender && operand.HasGender)
            {
                category = (int)operand.Gender;
            }
            else
            {
                return _defaultForm;
            }

            return _forms.TryGetValue(category, out ParsedTemplate form) ? form : _defaultForm;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial immutable bounded variant model. |
#endregion
