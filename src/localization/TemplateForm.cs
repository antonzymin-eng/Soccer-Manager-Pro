// File:     src/localization/TemplateForm.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 KD-3, FR-LC-009
// Purpose:  Immutable default or typed selector sub-form for one variant.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>One authored default, plural or gender sub-form.</summary>
    public readonly struct TemplateForm
    {
        /// <summary>Creates the required fallback form.</summary>
        public TemplateForm(string text) : this(TemplateSelectorKind.None, 0, text)
        {
        }

        private TemplateForm(TemplateSelectorKind kind, int category, string text)
        {
            if (text == null)
            {
                throw new ArgumentException("A template form requires text.", nameof(text));
            }

            Kind = kind;
            Category = category;
            Text = text;
        }

        /// <summary>Creates a form keyed by a supported plural category.</summary>
        public static TemplateForm FromPlural(PluralCategory category, string text)
        {
            if (category < PluralCategory.Other || category > PluralCategory.Many)
            {
                throw new ArgumentException("Undefined plural category.", nameof(category));
            }

            return new TemplateForm(TemplateSelectorKind.Plural, (int)category, text);
        }

        /// <summary>Creates a form keyed by a defined grammatical gender.</summary>
        public static TemplateForm FromGender(GrammaticalGender gender, string text)
        {
            if (gender < GrammaticalGender.Masculine || gender > GrammaticalGender.Other)
            {
                throw new ArgumentException("Undefined gender category.", nameof(gender));
            }

            return new TemplateForm(TemplateSelectorKind.Gender, (int)gender, text);
        }

        /// <summary>Gets the authored template text.</summary>
        public string Text { get; }
        internal TemplateSelectorKind Kind { get; }
        internal int Category { get; }
    }

    internal enum TemplateSelectorKind
    {
        None,
        Plural,
        Gender
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial L2 form model. |
#endregion
