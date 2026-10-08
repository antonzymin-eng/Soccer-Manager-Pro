// File:     src/localization/tests/LocalizationCatalogueTests.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 §5, L2 plan T5/T6/T14/T15/T16
// Purpose:  Reject authoring defects before rendering and prove content immutability.

using System;

using NUnit.Framework;

using static TacticalDirector.Localization.TemplateCatalogue;

namespace TacticalDirector.Localization.Tests
{
    public sealed class LocalizationCatalogueTests
    {
        private readonly TextTemplateId _id = new TextTemplateId(1, 2);
        private readonly LocalizationKey _key = new LocalizationKey("ui.s0.start");
        private readonly ClauseKey _clause = new ClauseKey(1, 9);
        [TestCase("static")]
        [TestCase("template")]
        [TestCase("clause")]
        public void T5_EachMissingRequiredIdentity_RejectsConstructionEvenWithATranslation(string missing)
        {
            var staticRows = new[]
            {
                new StaticRow(_key, "base")
            };
            var variants = new[]
            {
                Row(0)
            };
            var clauses = new[]
            {
                new ClauseRow(_clause, "clause")
            };
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, staticRows: missing == "static" ? null : staticRows, variants: missing == "template" ? null : variants, clauses: missing == "clause" ? null : clauses);
            var selected = new TemplateCatalogue(new LocaleId("test"), staticRows, variants, clauses);
            var coverage = new CatalogueCoverage(new[] { _key }, new[] { _id }, new[] { _clause });
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(baseCatalogue, selected, coverage));
            Assert.That(error.Message, Does.Contain(missing + ":"));
            Assert.That(error.Message, Does.Contain("missing required base row"));
        }

        [Test]
        public void T6_RequiredTemplateWithNoRowsAtAll_FailsBeforeAnyRender()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale), null, new CatalogueCoverage(templateIds: new[] { _id })));
            Assert.That(error.Message, Is.EqualTo("template:1/2: missing required base row"));
        }

        [Test]
        public void T5_AllMissingIdentities_AreNamedInOrdinalOrder()
        {
            var coverage = new CatalogueCoverage(new[] { new LocalizationKey("z"), new LocalizationKey("a") }, new[] { new TextTemplateId(2, 0), _id }, new[] { _clause });
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale), null, coverage));
            Assert.That(error.Message, Is.EqualTo("clause:1/9: missing required base row\nstatic:a: missing required base row\nstatic:z: missing required base row\ntemplate:1/2: missing required base row\ntemplate:2/0: missing required base row"));
        }

        [Test]
        public void T14_WrongBaseLocaleAndBaseIndexGap_AreRejected()
        {
            ArgumentException wrongLocale = Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(new LocaleId("test")), null, new CatalogueCoverage()));
            Assert.That(wrongLocale.Message, Is.EqualTo("locale: base catalogue must be en"));
            var gap = new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { Row(0), Row(2) });
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(gap, null, new CatalogueCoverage()));
            Assert.That(error.Message, Does.Contain("template:1/2: missing base index 1"));
        }

        [TestCase("static")]
        [TestCase("template")]
        [TestCase("clause")]
        public void T14_DuplicateRows_AreRejectedByIdentity(string duplicate)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new TemplateCatalogue(LocaleId.BaseLocale, staticRows: duplicate == "static" ? new[] { new StaticRow(_key, "one"), new StaticRow(_key, "two") } : null, variants: duplicate == "template" ? new[] { Row(0), Row(0) } : null, clauses: duplicate == "clause" ? new[] { new ClauseRow(_clause, "one"), new ClauseRow(_clause, "two") } : null));
            Assert.That(error.Message, Does.Contain(duplicate + ":"));
            Assert.That(error.Message, Does.Contain("duplicate row"));
        }

        [TestCase("static")]
        [TestCase("template")]
        [TestCase("clause")]
        public void T14_SelectedOrphans_AreRejectedForEveryIdentityKind(string orphan)
        {
            var selected = new TemplateCatalogue(new LocaleId("test"), staticRows: orphan == "static" ? new[] { new StaticRow(_key, "one") } : null, variants: orphan == "template" ? new[] { Row(0) } : null, clauses: orphan == "clause" ? new[] { new ClauseRow(_clause, "one") } : null);
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale), selected, new CatalogueCoverage()));
            Assert.That(error.Message, Does.Contain(orphan + ":"));
            Assert.That(error.Message, Does.Contain("orphan translation"));
        }

        [Test]
        public void T14_SelectedIndexBeyondBaseRangeAndNegativeIndex_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { Row(-1) }));
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { Row(0) });
            var selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { Row(1) });
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(baseCatalogue, selected, new CatalogueCoverage()));
            Assert.That(error.Message, Does.Contain("template:1/2/1: outside base range"));
        }

        [TestCase("{")]
        [TestCase("}")]
        [TestCase("{subject")]
        [TestCase("subject}")]
        [TestCase("{{subject}}")]
        [TestCase("{}")]
        public void T14_MalformedBraces_AreRejectedInPlainAndSelectorForms(string text)
        {
            Assert.Throws<ArgumentException>(() => new TemplateVariant(text));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForGender("subject", new TemplateForm("default"), TemplateForm.FromGender(GrammaticalGender.Feminine, text)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void T14_BaseSelectors_AreRejectedForPluralAndGender(bool plural)
        {
            TemplateVariant variant = plural ? TemplateVariant.ForPlural("count", new TemplateForm("default")) : TemplateVariant.ForGender("subject", new TemplateForm("default"));
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { new VariantRow(_id, 0, variant) }, pluralRule: count => PluralCategory.Other);
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(baseCatalogue, null, new CatalogueCoverage()));
            Assert.That(error.Message, Is.EqualTo("template:1/2/0: base selector forbidden"));
        }

        [Test]
        public void T14_PluralRuleIsRequiredAndFormsRequireOneDefaultWithoutDuplicatesOrMixedKinds()
        {
            TemplateVariant plural = TemplateVariant.ForPlural("count", new TemplateForm("default"));
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(new LocaleId("test"), variants: new[] { new VariantRow(_id, 0, plural) }));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForPlural("count", TemplateForm.FromPlural(PluralCategory.One, "one")));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForGender("subject", new TemplateForm("a"), new TemplateForm("b")));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForPlural("count", new TemplateForm("default"), TemplateForm.FromPlural(PluralCategory.One, "a"), TemplateForm.FromPlural(PluralCategory.One, "b")));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForPlural("count", new TemplateForm("default"), TemplateForm.FromGender(GrammaticalGender.Feminine, "wrong")));
            Assert.Throws<ArgumentException>(() => TemplateForm.FromPlural((PluralCategory)99, "wrong"));
            Assert.Throws<ArgumentException>(() => TemplateForm.FromGender(GrammaticalGender.Unspecified, "wrong"));
        }

        [Test]
        public void T14_DefaultRowsIdentitiesFormsAndNullDependencies_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(default(LocaleId)));
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(LocaleId.BaseLocale, staticRows: new[] { default(StaticRow) }));
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { default(VariantRow) }));
            Assert.Throws<ArgumentException>(() => new TemplateCatalogue(LocaleId.BaseLocale, clauses: new[] { default(ClauseRow) }));
            Assert.Throws<ArgumentException>(() => new CatalogueCoverage(staticKeys: new[] { default(LocalizationKey) }));
            Assert.Throws<ArgumentException>(() => new CatalogueCoverage(templateIds: new[] { default(TextTemplateId) }));
            Assert.Throws<ArgumentException>(() => new CatalogueCoverage(clauseKeys: new[] { default(ClauseKey) }));
            Assert.Throws<ArgumentException>(() => TemplateVariant.ForPlural("count", default(TemplateForm)));
            Assert.Throws<ArgumentException>(() => new Localizer(null, null, new CatalogueCoverage()));
            Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale), null, null));
        }

        [Test]
        public void T15_AllCallerArrays_AreCopiedIncludingCoverageAndSelectorForms()
        {
            var staticRows = new[]
            {
                new StaticRow(_key, "base")
            };
            var variants = new[]
            {
                Row(0)
            };
            var clauses = new[]
            {
                new ClauseRow(_clause, "clause")
            };
            var requiredKeys = new[]
            {
                _key
            };
            var requiredIds = new[]
            {
                _id
            };
            var requiredClauses = new[]
            {
                _clause
            };
            var forms = new[]
            {
                new TemplateForm("default"),
                TemplateForm.FromPlural(PluralCategory.Few, "few")
            };
            TemplateVariant plural = TemplateVariant.ForPlural("count", forms);
            var translatedVariants = new[]
            {
                new VariantRow(_id, 0, plural)
            };
            var translatedStatic = new[]
            {
                new StaticRow(_key, "selected")
            };
            var translatedClauses = new[]
            {
                new ClauseRow(_clause, "selected-clause")
            };
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, staticRows, variants, clauses);
            var selected = new TemplateCatalogue(new LocaleId("test"), translatedStatic, translatedVariants, translatedClauses, count => PluralCategory.Few);
            var coverage = new CatalogueCoverage(requiredKeys, requiredIds, requiredClauses);
            staticRows[0] = default(StaticRow);
            variants[0] = default(VariantRow);
            clauses[0] = default(ClauseRow);
            requiredKeys[0] = new LocalizationKey("missing");
            requiredIds[0] = new TextTemplateId(99, 99);
            requiredClauses[0] = new ClauseKey(99, 99);
            forms[1] = TemplateForm.FromPlural(PluralCategory.Few, "changed");
            translatedVariants[0] = default(VariantRow);
            translatedStatic[0] = default(StaticRow);
            translatedClauses[0] = default(ClauseRow);
            var localizer = new Localizer(baseCatalogue, selected, coverage);
            var request = new LocalizedTextRequest(_id, 0UL, default(NamedSlotSet), new NamedSelectorSet(new NamedSelector("count", SelectorOperand.FromCardinal(3L))), true, 9);
            Assert.That(localizer.Resolve(_key), Is.EqualTo("selected"));
            Assert.That(localizer.Render(request), Is.EqualTo("few selected-clause"));
            Assert.That(new Localizer(baseCatalogue, null, coverage).Resolve(_key), Is.EqualTo("base"));
            Assert.That(new Localizer(baseCatalogue, null, coverage).Render(request), Is.EqualTo("base clause"));
            Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale), null, coverage));
        }

        [Test]
        public void T16_RequiredMissingClause_IsImpossibleAfterSuccessfulConstruction()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new Localizer(new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { Row(0) }), null, new CatalogueCoverage(clauseKeys: new[] { _clause })));
            Assert.That(error.Message, Is.EqualTo("clause:1/9: missing required base row"));
        }

        private VariantRow Row(int index) => new VariantRow(_id, index, new TemplateVariant("base"));
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial L2 construction acceptance tests. |
// | 1.1     | 2026-10-07 | —      | PR #487 review: pin missing-row, wrong-locale and base-selector failure reasons. |
#endregion
