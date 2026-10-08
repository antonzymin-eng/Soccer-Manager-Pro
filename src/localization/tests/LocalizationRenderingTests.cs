// File:     src/localization/tests/LocalizationRenderingTests.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 §5, L2 plan T1-T4/T7-T13/T16
// Purpose:  Exercise the production seam, fallback, selectors and non-recursive expansion.

using System;

using NUnit.Framework;

using static TacticalDirector.Localization.TemplateCatalogue;

namespace TacticalDirector.Localization.Tests
{
    public sealed class LocalizationRenderingTests
    {
        private readonly TextTemplateId _id = new TextTemplateId(1, 2);
        private readonly LocalizationKey _key = new LocalizationKey("ui.s0.start");
        [Test]
        public void T1_WorkedRender_SelectsSecondVariantAndExpandsExactBaseText()
        {
            var localizer = CreateBase("unused", "{subject} wants a word about playing time after the {score} against {opponent}.");
            var slots = new NamedSlotSet(new NamedSlot("subject", "Rooney"), new NamedSlot("opponent", "Everton"), new NamedSlot("score", "2-1"));
            Assert.That(localizer.Render(Request(5UL, slots)), Is.EqualTo("Rooney wants a word about playing time after the 2-1 against Everton."));
        }

        [TestCase(2147483648UL, "base-2")]
        [TestCase(18446744073709551614UL, "base-2")]
        [TestCase(18446744073709551615UL, "base-0")]
        public void T2_SelectionModulo_UsesFullUlongBeforeNarrowing(ulong draw, string expected)
        {
            Assert.That(CreateBase("base-0", "base-1", "base-2").Render(Request(draw)), Is.EqualTo(expected));
        }

        [TestCase(0UL, "selected-0")]
        [TestCase(1UL, "base-1")]
        [TestCase(2UL, "selected-2")]
        [TestCase(4UL, "base-1")]
        public void T3_SparseTranslation_FallsBackAtTheSameBaseIndex(ulong draw, string expected)
        {
            var selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { Row(0, "selected-0"), Row(2, "selected-2") });
            var localizer = new Localizer(Base("base-0", "base-1", "base-2"), selected, new CatalogueCoverage());
            Assert.That(localizer.Render(Request(draw)), Is.EqualTo(expected));
        }

        [Test]
        public void T4_FullTranslation_UsesBaseCountAndPreservesIndexOrder()
        {
            var selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { Row(2, "selected-2"), Row(0, "selected-0"), Row(1, "selected-1") });
            var localizer = new Localizer(Base("0", "1", "2"), selected, new CatalogueCoverage());
            Assert.That(localizer.Render(Request(5UL)), Is.EqualTo("selected-2"));
        }

        [Test]
        public void T7_NonAdmittedStaticKey_ReturnsEmptyWithoutChangingOtherResults()
        {
            var localizer = new Localizer(new TemplateCatalogue(LocaleId.BaseLocale, staticRows: new[] { new StaticRow(_key, "Start") }), null, new CatalogueCoverage(staticKeys: new[] { _key }));
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Assert.That(localizer.Resolve(new LocalizationKey("ui.never-admitted")), Is.EqualTo(string.Empty));
                Assert.That(localizer.Resolve(_key), Is.EqualTo("Start"));
            }

            Assert.That(localizer.Resolve(default(LocalizationKey)), Is.EqualTo(string.Empty));
        }

        [Test]
        public void T8_StaticFallback_UsesSelectedThenBaseAndNullSelectedMeansBase()
        {
            var other = new LocalizationKey("ui.s0.report");
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, staticRows: new[] { new StaticRow(_key, "Start"), new StaticRow(other, "Report") });
            var selected = new TemplateCatalogue(new LocaleId("test"), staticRows: new[] { new StaticRow(_key, "Selected") });
            var localizer = new Localizer(baseCatalogue, selected, new CatalogueCoverage());
            Assert.That(localizer.Resolve(_key), Is.EqualTo("Selected"));
            Assert.That(localizer.Resolve(other), Is.EqualTo("Report"));
            Assert.That(new Localizer(baseCatalogue, null, new CatalogueCoverage()).Resolve(_key), Is.EqualTo("Start"));
            Assert.That(new Localizer(baseCatalogue, baseCatalogue, new CatalogueCoverage()).Resolve(_key), Is.EqualTo("Start"));
        }

        [Test]
        public void T9_StaticCompositePattern_IsReturnedByteIdenticalAndNeverParsed()
        {
            const string pattern = "{0}: {1:D} {{literal}} unmatched {";
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, staticRows: new[] { new StaticRow(_key, pattern) });
            var selected = new TemplateCatalogue(new LocaleId("test"), staticRows: new[] { new StaticRow(_key, pattern) });
            Assert.That(new Localizer(baseCatalogue, null, new CatalogueCoverage()).Resolve(_key), Is.EqualTo(pattern));
            Assert.That(new Localizer(baseCatalogue, selected, new CatalogueCoverage()).Resolve(_key), Is.EqualTo(pattern));
        }

        [Test]
        public void T10_PluralOperand_DrivesSyntheticRuleThroughRealRenderWhileBaseStaysPlain()
        {
            TemplateVariant plural = TemplateVariant.ForPlural("count", new TemplateForm("other {count}"), TemplateForm.FromPlural(PluralCategory.Few, "few {count}"));
            var selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { new VariantRow(_id, 0, plural) }, pluralRule: count => count == 3L ? PluralCategory.Few : PluralCategory.One);
            var baseCatalogue = Base("base {count}");
            var slots = new NamedSlotSet(new NamedSlot("count", "3"));
            var selectors = new NamedSelectorSet(new NamedSelector("count", SelectorOperand.FromCardinal(3L)));
            Assert.That(new Localizer(baseCatalogue, selected, new CatalogueCoverage()).Render(Request(0UL, slots, selectors)), Is.EqualTo("few 3"));
            Assert.That(new Localizer(baseCatalogue, null, new CatalogueCoverage()).Render(Request(0UL, slots, selectors)), Is.EqualTo("base 3"));
            selectors = new NamedSelectorSet(new NamedSelector("count", SelectorOperand.FromCardinal(1L)));
            Assert.That(new Localizer(baseCatalogue, selected, new CatalogueCoverage()).Render(Request(0UL, slots, selectors)), Is.EqualTo("other 3"));
        }

        [Test]
        public void T11_GenderAndPluralSelectors_FallBackForMissingWrongOrUnmatchedOperands()
        {
            TemplateVariant gender = TemplateVariant.ForGender("subject", new TemplateForm("default"), TemplateForm.FromGender(GrammaticalGender.Feminine, "feminine"));
            var selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { new VariantRow(_id, 0, gender) });
            var localizer = new Localizer(Base("base"), selected, new CatalogueCoverage());
            Assert.That(localizer.Render(Request(0UL, selectors: Gender(GrammaticalGender.Feminine))), Is.EqualTo("feminine"));
            Assert.That(localizer.Render(Request(0UL, selectors: Gender(GrammaticalGender.Other))), Is.EqualTo("default"));
            Assert.That(localizer.Render(Request(0UL)), Is.EqualTo("default"));
            Assert.That(localizer.Render(Request(0UL, selectors: new NamedSelectorSet(new NamedSelector("subject", SelectorOperand.FromCardinal(3L))))), Is.EqualTo("default"));
            TemplateVariant plural = TemplateVariant.ForPlural("subject", new TemplateForm("plural-default"), TemplateForm.FromPlural(PluralCategory.Few, "few"));
            selected = new TemplateCatalogue(new LocaleId("test"), variants: new[] { new VariantRow(_id, 0, plural) }, pluralRule: count => (PluralCategory)99);
            localizer = new Localizer(Base("base"), selected, new CatalogueCoverage());
            Assert.That(localizer.Render(Request(0UL, selectors: Gender(GrammaticalGender.Feminine))), Is.EqualTo("plural-default"));
            Assert.That(localizer.Render(Request(0UL)), Is.EqualTo("plural-default"));
            Assert.That(localizer.Render(Request(0UL, selectors: new NamedSelectorSet(new NamedSelector("subject", SelectorOperand.FromCardinal(3L))))), Is.EqualTo("plural-default"));
        }

        [Test]
        public void T12_Citations_UseProducerScopeAndSelectedThenBaseWithExactlyOneSpace()
        {
            var secondId = new TextTemplateId(2, 2);
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, variants: new[] { Row(0, "base"), Row(1, "second"), new VariantRow(secondId, 0, new TemplateVariant("producer-two")) }, clauses: new[] { Clause(1, 9, "base-clause"), Clause(1, 10, "fallback"), Clause(2, 9, "other-producer") });
            var selected = new TemplateCatalogue(new LocaleId("test"), clauses: new[] { Clause(1, 9, "selected-clause") });
            var localizer = new Localizer(baseCatalogue, selected, new CatalogueCoverage());
            Assert.That(localizer.Render(Request(0UL, cited: true, kind: 9)), Is.EqualTo("base selected-clause"));
            Assert.That(localizer.Render(Request(1UL, cited: true, kind: 9)), Is.EqualTo("second selected-clause"));
            Assert.That(localizer.Render(Request(0UL, cited: true, kind: 10)), Is.EqualTo("base fallback"));
            Assert.That(localizer.Render(Request(0UL, kind: 9)), Is.EqualTo("base"));
            var request = new LocalizedTextRequest(secondId, 0UL, default(NamedSlotSet), default(NamedSelectorSet), true, 9);
            Assert.That(localizer.Render(request), Is.EqualTo("producer-two other-producer"));
        }

        [Test]
        public void T13_Substitution_IsSinglePassForTokenValuesAndCrossBoundaryTokens()
        {
            var slots = new NamedSlotSet(new NamedSlot("subject", "{opponent}"), new NamedSlot("opponent", "Everton"));
            Assert.That(CreateBase("{subject} / {opponent}").Render(Request(0UL, slots)), Is.EqualTo("{opponent} / Everton"));
            slots = new NamedSlotSet(new NamedSlot("subject", "{"), new NamedSlot("opponent", "score}"), new NamedSlot("score", "2-1"));
            Assert.That(CreateBase("{subject}{opponent}").Render(Request(0UL, slots)), Is.EqualTo("{score}"));
        }

        [Test]
        public void T13_BraceFreeValuesAndWellFormedTokens_MatchBothChainedReplacementOrders()
        {
            const string template = "{subject}: {score} v {opponent}; {subject}; {unknown}";
            string expected = template.Replace("{subject}", "Rooney").Replace("{opponent}", "Everton").Replace("{score}", "2-1");
            string sorted = template.Replace("{opponent}", "Everton").Replace("{score}", "2-1").Replace("{subject}", "Rooney");
            var slots = new NamedSlotSet(new NamedSlot("subject", "Rooney"), new NamedSlot("opponent", "Everton"), new NamedSlot("score", "2-1"));
            string actual = CreateBase(template).Render(Request(0UL, slots));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.EqualTo(sorted));
            Assert.That(actual, Does.EndWith("{unknown}"));
        }

        [Test]
        public void T16_UnknownTemplateAndUnrequiredClause_UseDefinedTerminalResults()
        {
            var localizer = CreateBase("base");
            var unknown = new LocalizedTextRequest(new TextTemplateId(3, 99), ulong.MaxValue, default(NamedSlotSet), default(NamedSelectorSet), true, 9);
            Assert.That(localizer.Render(unknown), Is.EqualTo(string.Empty));
            Assert.That(localizer.Render(default(LocalizedTextRequest)), Is.EqualTo(string.Empty));
            Assert.That(localizer.Render(Request(0UL, cited: true, kind: 99)), Is.EqualTo("base"));
        }

        private Localizer CreateBase(params string[] texts) => new Localizer(Base(texts), null, new CatalogueCoverage());
        private TemplateCatalogue Base(params string[] texts)
        {
            var rows = new VariantRow[texts.Length];
            for (int index = 0; index < texts.Length; index++)
            {
                rows[index] = Row(index, texts[index]);
            }

            return new TemplateCatalogue(LocaleId.BaseLocale, variants: rows);
        }

        private VariantRow Row(int index, string text) => new VariantRow(_id, index, new TemplateVariant(text));
        private static ClauseRow Clause(int producer, int kind, string text) => new ClauseRow(new ClauseKey(producer, kind), text);
        private static NamedSelectorSet Gender(GrammaticalGender gender) => new NamedSelectorSet(new NamedSelector("subject", SelectorOperand.FromGender(gender)));
        private LocalizedTextRequest Request(ulong draw, NamedSlotSet slots = default(NamedSlotSet), NamedSelectorSet selectors = default(NamedSelectorSet), bool cited = false, int kind = 0) => new LocalizedTextRequest(_id, draw, slots, selectors, cited, kind);
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial L2 rendering acceptance tests. |
#endregion
