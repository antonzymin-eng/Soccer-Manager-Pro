// ============================================================================
// File:     src/localization/TemplateForm.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §1 KD-3, FR-LC-009
// Purpose:  One keyed sub-form of a selector-bearing template variant.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One authored sub-form of a selector variant, keyed by a plural category or a grammatical
    /// gender. The zero key of each kind (<see cref="PluralCategory.Other"/>,
    /// <see cref="GrammaticalGender.Unspecified"/>) is that selector's required default form.
    /// </summary>
    public readonly struct TemplateForm
    {
        private TemplateForm(SelectorKind kind, PluralCategory plural, GrammaticalGender gender, string text)
        {
            Kind = kind;
            Plural = plural;
            Gender = gender;
            Text = text;
        }

        /// <summary>Creates a plural-keyed form.</summary>
        public static TemplateForm ForPlural(PluralCategory category, string text)
        {
            if (category < PluralCategory.Other || category > PluralCategory.Many)
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Plural form requires a defined category.");
            }

            return new TemplateForm(SelectorKind.Plural, category, GrammaticalGender.Unspecified, RequireText(text));
        }

        /// <summary>Creates a gender-keyed form; <see cref="GrammaticalGender.Unspecified"/> is the default form.</summary>
        public static TemplateForm ForGender(GrammaticalGender gender, string text)
        {
            if (gender < GrammaticalGender.Unspecified || gender > GrammaticalGender.Other)
            {
                throw new ArgumentOutOfRangeException(nameof(gender), "Gender form requires a defined gender.");
            }

            return new TemplateForm(SelectorKind.Gender, PluralCategory.Other, gender, RequireText(text));
        }

        /// <summary>Selector kind this form belongs to; <see cref="SelectorKind.None"/> for a default struct.</summary>
        public SelectorKind Kind { get; }

        /// <summary>Plural key; meaningful only when <see cref="Kind"/> is <see cref="SelectorKind.Plural"/>.</summary>
        public PluralCategory Plural { get; }

        /// <summary>Gender key; meaningful only when <see cref="Kind"/> is <see cref="SelectorKind.Gender"/>.</summary>
        public GrammaticalGender Gender { get; }

        /// <summary>Authored template text for this form.</summary>
        public string Text { get; }

        /// <summary>True for a form built through a factory.</summary>
        public bool IsValid => Kind != SelectorKind.None && Text != null;

        /// <summary>True when this is its selector's required default form.</summary>
        public bool IsDefault => Kind == SelectorKind.Plural
            ? Plural == PluralCategory.Other
            : Kind == SelectorKind.Gender && Gender == GrammaticalGender.Unspecified;

        internal int KeyOrdinal => Kind == SelectorKind.Plural ? (int)Plural : (int)Gender;

        private static string RequireText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return text;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 keyed template form (localization-l2-plan v0.5 §5.1, Q2). |
#endregion
