// ============================================================================
// File:     src/localization/tests/LocalizerTests.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, §3.5, §3.6, KD-3, KD-5,
//           FR-LC-007/008/008a/009/010/011/020, ERR-049-005, ERR-049-006
// Purpose:  L2 catalogue construction, Resolve/Render fallback, selector and expansion tests
//           (localization-l2-plan v0.5 §6, T1–T16).
// ============================================================================

using System;

using NUnit.Framework;

namespace TacticalDirector.Localization.Tests
{
    public sealed class LocalizerTests
    {
        private const int Tag = 7;
        private const int OtherTag = 8;
        private static readonly TextTemplateId PlayingTime = new TextTemplateId(Tag, 2);
        private static readonly TextTemplateId Unadmitted = new TextTemplateId(Tag, 99);
        private static readonly LocaleId French = new LocaleId("fr");
        private static readonly LocalizationKey MenuStart = new LocalizationKey("ui.s0.menu.start");
        private static readonly LocalizationKey Scoreline = new LocalizationKey("ui.s0.scoreline");

        // ---- helpers -------------------------------------------------------------------------

        private static TemplateCatalogue Base(
            TemplateVariant[] variants,
            StaticTextEntry[] statics = null,
            CitationClause[] clauses = null)
        {
            return new TemplateCatalogue(
                LocaleId.BaseLocale,
                statics ?? new StaticTextEntry[0],
                variants,
                clauses ?? new CitationClause[0]);
        }

        private static TemplateCatalogue Selected(
            LocaleId locale,
            TemplateVariant[] variants,
            StaticTextEntry[] statics = null,
            CitationClause[] clauses = null,
            Func<long, PluralCategory> pluralRule = null)
        {
            return new TemplateCatalogue(
                locale,
                statics ?? new StaticTextEntry[0],
                variants,
                clauses ?? new CitationClause[0],
                pluralRule);
        }

        private static CatalogueCoverage NoCoverage()
        {
            return new CatalogueCoverage(new LocalizationKey[0], new TextTemplateId[0], new CitationClauseKey[0]);
        }

        private static TemplateVariant Plain(int index, string text)
        {
            return new TemplateVariant(PlayingTime, index, text);
        }

        private static LocalizedTextRequest Request(
            ulong draw,
            NamedSlotSet slots,
            NamedSelectorSet selectors = default,
            bool hasCitedEpisode = false,
            int citationKind = 0,
            TextTemplateId? id = null)
        {
            return new LocalizedTextRequest(id ?? PlayingTime, draw, slots, selectors, hasCitedEpisode, citationKind);
        }

        private static NamedSlotSet Slots(string subject, string opponent, string score)
        {
            return new NamedSlotSet(
                new NamedSlot("subject", subject),
                new NamedSlot("opponent", opponent),
                new NamedSlot("score", score));
        }

        private static NamedSlotSet RooneySlots() => Slots("Rooney", "Everton", "2-1");

        private static string ChainedLivingWorldOrder(string template, string subject, string opponent, string score)
        {
            return template.Replace("{subject}", subject).Replace("{opponent}", opponent).Replace("{score}", score);
        }

        private static string ChainedSortedOrder(string template, string subject, string opponent, string score)
        {
            return template.Replace("{opponent}", opponent).Replace("{score}", score).Replace("{subject}", subject);
        }

        // ---- T1–T4: variant selection ----------------------------------------------------------

        [Test]
        public void T1_WorkedRender_BaseOnly_IsByteIdenticalToSpecExample()
        {
            var localizer = new Localizer(
                Base(new[]
                {
                    Plain(0, "{subject} is unhappy with the {score} against {opponent}."),
                    Plain(1, "{subject} wants a word about playing time after the {score} against {opponent}.")
                }),
                null,
                NoCoverage());

            string text = localizer.Render(Request(5UL, RooneySlots()));

            Assert.That(text, Is.EqualTo("Rooney wants a word about playing time after the 2-1 against Everton."));
        }

        [TestCase(ulong.MaxValue, "v0")]
        [TestCase(ulong.MaxValue - 1UL, "v2")]
        [TestCase(2147483648UL, "v2")]
        [TestCase(4294967296UL, "v1")]
        [TestCase(4294967297UL, "v2")]
        public void T2_DrawModulo_IsComputedInUlongBeforeNarrowing(ulong draw, string expected)
        {
            var localizer = new Localizer(Base(new[] { Plain(0, "v0"), Plain(1, "v1"), Plain(2, "v2") }), null, NoCoverage());

            Assert.That(localizer.Render(Request(draw, default)), Is.EqualTo(expected));
        }

        [TestCase(0UL, "s0")]
        [TestCase(1UL, "b1")]
        [TestCase(2UL, "s2")]
        public void T3_SparseTranslation_FallsBackToBaseAtTheSameIndex(ulong draw, string expected)
        {
            var localizer = new Localizer(
                Base(new[] { Plain(0, "b0"), Plain(1, "b1"), Plain(2, "b2") }),
                Selected(French, new[] { Plain(0, "s0"), Plain(2, "s2") }),
                NoCoverage());

            Assert.That(localizer.Render(Request(draw, default)), Is.EqualTo(expected));
        }

        [Test]
        public void T4_VariantCount_ComesFromBaseNotSelected()
        {
            var localizer = new Localizer(
                Base(new[] { Plain(0, "b0"), Plain(1, "b1"), Plain(2, "b2") }),
                Selected(French, new[] { Plain(0, "s0"), Plain(1, "s1") }),
                NoCoverage());

            // A selected-locale count of 2 would map draw 2 to index 0 ("s0").
            Assert.That(localizer.Render(Request(2UL, default)), Is.EqualTo("b2"));
        }

        // ---- T5–T7: coverage and the ERR-049-005 terminal result ------------------------------

        [Test]
        public void T5_MissingRequiredStaticKey_FailsConstruction()
        {
            var coverage = new CatalogueCoverage(new[] { MenuStart }, new TextTemplateId[0], new CitationClauseKey[0]);

            var error = Assert.Throws<ArgumentException>(() => new Localizer(Base(new TemplateVariant[0]), null, coverage));
            Assert.That(error.Message, Does.Contain("missing required static key 'ui.s0.menu.start'"));
        }

        [Test]
        public void T5_MissingRequiredTemplate_FailsConstruction()
        {
            var coverage = new CatalogueCoverage(new LocalizationKey[0], new[] { PlayingTime }, new CitationClauseKey[0]);

            var error = Assert.Throws<ArgumentException>(() => new Localizer(Base(new TemplateVariant[0]), null, coverage));
            Assert.That(error.Message, Does.Contain("missing required template (7, 2)"));
        }

        [Test]
        public void T5_MissingRequiredClause_FailsConstruction()
        {
            var coverage = new CatalogueCoverage(
                new LocalizationKey[0], new TextTemplateId[0], new[] { new CitationClauseKey(Tag, 3) });

            var error = Assert.Throws<ArgumentException>(() => new Localizer(Base(new TemplateVariant[0]), null, coverage));
            Assert.That(error.Message, Does.Contain("missing required clause (7, 3)"));
        }

        [Test]
        public void T5_CoveredIdentities_Construct()
        {
            var coverage = new CatalogueCoverage(
                new[] { MenuStart }, new[] { PlayingTime }, new[] { new CitationClauseKey(Tag, 3) });

            Assert.DoesNotThrow(() => new Localizer(
                Base(
                    new[] { Plain(0, "x") },
                    new[] { new StaticTextEntry(MenuStart, "Start") },
                    new[] { new CitationClause(new CitationClauseKey(Tag, 3), "Clause.") }),
                null,
                coverage));
        }

        [Test]
        public void T5_EveryViolationIsListedInOrdinalOrder()
        {
            var coverage = new CatalogueCoverage(new[] { Scoreline, MenuStart }, new TextTemplateId[0], new CitationClauseKey[0]);

            var error = Assert.Throws<ArgumentException>(() => new Localizer(Base(new TemplateVariant[0]), null, coverage));
            int first = error.Message.IndexOf("'ui.s0.menu.start'", StringComparison.Ordinal);
            int second = error.Message.IndexOf("'ui.s0.scoreline'", StringComparison.Ordinal);
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            Assert.That(second, Is.GreaterThan(first));
        }

        [Test]
        public void T6_RequiredTemplateWithNoRowsAtAll_FailsEvenWhenOtherTemplatesExist()
        {
            var otherTemplate = new TemplateVariant(new TextTemplateId(Tag, 1), 0, "other");
            var coverage = new CatalogueCoverage(new LocalizationKey[0], new[] { PlayingTime }, new CitationClauseKey[0]);

            Assert.Throws<ArgumentException>(() => new Localizer(Base(new[] { otherTemplate }), null, coverage));
        }

        [Test]
        public void T7_ResolveOfUnadmittedKey_ReturnsEmpty_WithoutThrowingOrMutating()
        {
            var localizer = new Localizer(
                Base(new TemplateVariant[0], new[] { new StaticTextEntry(MenuStart, "Start") }),
                Selected(French, new TemplateVariant[0], new[] { new StaticTextEntry(MenuStart, "Commencer") }),
                new CatalogueCoverage(new[] { MenuStart }, new TextTemplateId[0], new CitationClauseKey[0]));
            var unknown = new LocalizationKey("ui.s0.never.admitted");

            Assert.That(localizer.Resolve(unknown), Is.EqualTo(string.Empty));
            Assert.That(localizer.Resolve(unknown), Is.EqualTo(string.Empty));
            Assert.That(localizer.Resolve(MenuStart), Is.EqualTo("Commencer"));
            Assert.That(localizer.Resolve(default(LocalizationKey)), Is.EqualTo(string.Empty));
        }

        // ---- T8–T9: static rows -----------------------------------------------------------------

        [Test]
        public void T8_StaticKey_UsesSelectedThenBase()
        {
            var localizer = new Localizer(
                Base(new TemplateVariant[0], new[]
                {
                    new StaticTextEntry(MenuStart, "Start"),
                    new StaticTextEntry(Scoreline, "Score")
                }),
                Selected(French, new TemplateVariant[0], new[] { new StaticTextEntry(MenuStart, "Commencer") }),
                NoCoverage());

            Assert.That(localizer.Resolve(MenuStart), Is.EqualTo("Commencer"));
            Assert.That(localizer.Resolve(Scoreline), Is.EqualTo("Score"));
        }

        [Test]
        public void T9_StaticRows_AreOpaque_CompositePatternsReturnedVerbatim()
        {
            const string pattern = "{0} {1:D} – {{literal}} }{";
            var localizer = new Localizer(
                Base(new TemplateVariant[0], new[] { new StaticTextEntry(Scoreline, pattern) }),
                null,
                NoCoverage());

            Assert.That(localizer.Resolve(Scoreline), Is.EqualTo(pattern));
        }

        // ---- T10–T11: bounded selectors ---------------------------------------------------------

        private static PluralCategory SyntheticSlavicRule(long n)
        {
            long lastTwo = Math.Abs(n % 100);
            long last = Math.Abs(n % 10);
            if (last == 1 && lastTwo != 11)
            {
                return PluralCategory.One;
            }

            if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14))
            {
                return PluralCategory.Few;
            }

            return PluralCategory.Many;
        }

        private static TemplateVariant GoalsVariant()
        {
            return new TemplateVariant(PlayingTime, 0, "goals", SelectorKind.Plural,
                TemplateForm.ForPlural(PluralCategory.Other, "{subject}: other"),
                TemplateForm.ForPlural(PluralCategory.One, "{subject}: one"),
                TemplateForm.ForPlural(PluralCategory.Few, "{subject}: few"),
                TemplateForm.ForPlural(PluralCategory.Many, "{subject}: many"));
        }

        [TestCase(1L, "Kade: one")]
        [TestCase(3L, "Kade: few")]
        [TestCase(12L, "Kade: many")]
        public void T10_SyntheticLocale_SelectsPluralCategoryThroughRealRenderer(long goals, string expected)
        {
            var localizer = new Localizer(
                Base(new[] { Plain(0, "{subject} scored.") }),
                Selected(new LocaleId("pl"), new[] { GoalsVariant() }, pluralRule: SyntheticSlavicRule),
                NoCoverage());
            var selectors = new NamedSelectorSet(new NamedSelector("goals", SelectorOperand.FromCardinal(goals)));

            Assert.That(localizer.Render(Request(0UL, new NamedSlotSet(new NamedSlot("subject", "Kade")), selectors)), Is.EqualTo(expected));
        }

        [Test]
        public void T10_BaseEnglish_TakesTheIdentityPathRegardlessOfSelectors()
        {
            var localizer = new Localizer(Base(new[] { Plain(0, "{subject} scored.") }), null, NoCoverage());
            var selectors = new NamedSelectorSet(new NamedSelector("goals", SelectorOperand.FromCardinal(3)));

            Assert.That(localizer.Render(Request(0UL, new NamedSlotSet(new NamedSlot("subject", "Kade")), selectors)), Is.EqualTo("Kade scored."));
        }

        [Test]
        public void T10_UndefinedCategoryFromRule_UsesDefaultForm()
        {
            var localizer = new Localizer(
                Base(new[] { Plain(0, "{subject} scored.") }),
                Selected(new LocaleId("pl"), new[] { GoalsVariant() }, pluralRule: n => (PluralCategory)42),
                NoCoverage());
            var selectors = new NamedSelectorSet(new NamedSelector("goals", SelectorOperand.FromCardinal(3)));

            Assert.That(localizer.Render(Request(0UL, new NamedSlotSet(new NamedSlot("subject", "Kade")), selectors)), Is.EqualTo("Kade: other"));
        }

        private static Localizer GenderLocalizer()
        {
            var variant = new TemplateVariant(PlayingTime, 0, "subject_gender", SelectorKind.Gender,
                TemplateForm.ForGender(GrammaticalGender.Unspecified, "default"),
                TemplateForm.ForGender(GrammaticalGender.Feminine, "feminine"),
                TemplateForm.ForGender(GrammaticalGender.Masculine, "masculine"));
            return new Localizer(
                Base(new[] { Plain(0, "base") }),
                Selected(new LocaleId("de"), new[] { variant }),
                NoCoverage());
        }

        [Test]
        public void T11_GenderSelector_ChoosesFormOrFallsBackToDefault()
        {
            Localizer localizer = GenderLocalizer();
            NamedSelectorSet Gender(GrammaticalGender g) =>
                new NamedSelectorSet(new NamedSelector("subject_gender", SelectorOperand.FromGender(g)));

            Assert.That(localizer.Render(Request(0UL, default, Gender(GrammaticalGender.Feminine))), Is.EqualTo("feminine"));
            Assert.That(localizer.Render(Request(0UL, default, Gender(GrammaticalGender.Masculine))), Is.EqualTo("masculine"));
            Assert.That(localizer.Render(Request(0UL, default, Gender(GrammaticalGender.Neutral))), Is.EqualTo("default"));
            Assert.That(localizer.Render(Request(0UL, default)), Is.EqualTo("default"));
            Assert.That(
                localizer.Render(Request(0UL, default, new NamedSelectorSet(new NamedSelector("subject_gender", SelectorOperand.FromCardinal(2))))),
                Is.EqualTo("default"));
        }

        [Test]
        public void T11_PluralSelectorWithGenderOnlyOperand_UsesDefaultForm()
        {
            var localizer = new Localizer(
                Base(new[] { Plain(0, "{subject} scored.") }),
                Selected(new LocaleId("pl"), new[] { GoalsVariant() }, pluralRule: SyntheticSlavicRule),
                NoCoverage());
            var selectors = new NamedSelectorSet(new NamedSelector("goals", SelectorOperand.FromGender(GrammaticalGender.Feminine)));

            Assert.That(localizer.Render(Request(0UL, new NamedSlotSet(new NamedSlot("subject", "Kade")), selectors)), Is.EqualTo("Kade: other"));
        }

        // ---- T12: clauses -------------------------------------------------------------------------

        [Test]
        public void T12_Clause_AppendedWithOneSpace_ProducerScoped_SelectedThenBase()
        {
            var clauseA = new CitationClauseKey(Tag, 3);
            var clauseB = new CitationClauseKey(Tag, 4);
            var otherProducerTemplate = new TextTemplateId(OtherTag, 2);
            var localizer = new Localizer(
                Base(
                    new[] { Plain(0, "Text."), new TemplateVariant(otherProducerTemplate, 0, "Other.") },
                    clauses: new[]
                    {
                        new CitationClause(clauseA, "Base A."),
                        new CitationClause(clauseB, "Base B."),
                        new CitationClause(new CitationClauseKey(OtherTag, 3), "Other producer A.")
                    }),
                Selected(French, new[] { Plain(0, "Texte.") }, clauses: new[] { new CitationClause(clauseA, "Choisi A.") }),
                NoCoverage());

            Assert.That(localizer.Render(Request(0UL, default, hasCitedEpisode: true, citationKind: 3)), Is.EqualTo("Texte. Choisi A."));
            Assert.That(localizer.Render(Request(0UL, default, hasCitedEpisode: true, citationKind: 4)), Is.EqualTo("Texte. Base B."));
            Assert.That(localizer.Render(Request(0UL, default, hasCitedEpisode: false, citationKind: 3)), Is.EqualTo("Texte."));
            Assert.That(
                localizer.Render(Request(0UL, default, hasCitedEpisode: true, citationKind: 3, id: otherProducerTemplate)),
                Is.EqualTo("Other. Other producer A."));
        }

        // ---- T13: single-pass expansion (ERR-049-006) -------------------------------------------

        [Test]
        public void T13a_SlotValueContainingAToken_IsNotReExpanded()
        {
            const string template = "{subject} faces {opponent}.";
            var localizer = new Localizer(Base(new[] { Plain(0, template) }), null, NoCoverage());

            string text = localizer.Render(Request(0UL, Slots("{opponent}", "Everton", "2-1")));

            Assert.That(text, Is.EqualTo("{opponent} faces Everton."));
            Assert.That(ChainedLivingWorldOrder(template, "{opponent}", "Everton", "2-1"), Is.EqualTo("Everton faces Everton."));
        }

        [Test]
        public void T13b_TokenFormedAcrossAdjacentSubstitutions_IsNotExpanded()
        {
            const string template = "{subject}{opponent}";
            var localizer = new Localizer(Base(new[] { Plain(0, template) }), null, NoCoverage());

            string text = localizer.Render(Request(0UL, Slots("{", "score}", "2-1")));

            Assert.That(text, Is.EqualTo("{score}"));
            Assert.That(ChainedLivingWorldOrder(template, "{", "score}", "2-1"), Is.EqualTo("2-1"));
        }

        [TestCase("{subject} wants a word about playing time after the {score} against {opponent}.", "Kade Moreno", "Halden Rovers", "2-1")]
        [TestCase("{opponent}{subject}{score}{subject}", "A", "B", "0-0")]
        [TestCase("No tokens here.", "Kade Moreno", "Halden Rovers", "3-2")]
        [TestCase("{score} {subject} {opponent} {unknown}", "Kade Moreno", "Halden Rovers", "10-12")]
        public void T13c_BraceFreeValuesAndWellFormedTemplates_MatchChainedReplaceInAnyOrder(
            string template, string subject, string opponent, string score)
        {
            var localizer = new Localizer(Base(new[] { Plain(0, template) }), null, NoCoverage());

            string text = localizer.Render(Request(0UL, Slots(subject, opponent, score)));

            Assert.That(text, Is.EqualTo(ChainedLivingWorldOrder(template, subject, opponent, score)));
            Assert.That(text, Is.EqualTo(ChainedSortedOrder(template, subject, opponent, score)));
        }

        [Test]
        public void T13d_UnknownPlaceholder_StaysVerbatim()
        {
            var localizer = new Localizer(Base(new[] { Plain(0, "{subject} and {nobody}") }), null, NoCoverage());

            Assert.That(localizer.Render(Request(0UL, Slots("Kade", "X", "1-0"))), Is.EqualTo("Kade and {nobody}"));
        }

        // ---- T14: construction rejections -------------------------------------------------------

        [Test]
        public void T14_BaseCatalogueMustUseBaseLocale()
        {
            Assert.Throws<ArgumentException>(() => new Localizer(Selected(French, new[] { Plain(0, "x") }), null, NoCoverage()));
        }

        [Test]
        public void T14_SelectedCatalogueOtherThanBaseCannotUseBaseLocale()
        {
            Assert.Throws<ArgumentException>(() => new Localizer(
                Base(new[] { Plain(0, "x") }), Base(new[] { Plain(0, "y") }), NoCoverage()));
            Assert.DoesNotThrow(() =>
            {
                TemplateCatalogue same = Base(new[] { Plain(0, "x") });
                new Localizer(same, same, NoCoverage());
            });
        }

        [Test]
        public void T14_DuplicateRows_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => Base(new TemplateVariant[0], new[]
            {
                new StaticTextEntry(MenuStart, "a"), new StaticTextEntry(MenuStart, "b")
            }));
            Assert.Throws<ArgumentException>(() => Base(new[] { Plain(0, "a"), Plain(0, "b") }));
            Assert.Throws<ArgumentException>(() => Base(new TemplateVariant[0], clauses: new[]
            {
                new CitationClause(new CitationClauseKey(Tag, 3), "a"), new CitationClause(new CitationClauseKey(Tag, 3), "b")
            }));
        }

        [Test]
        public void T14_GapInBaseIndices_IsRejected()
        {
            var error = Assert.Throws<ArgumentException>(() => new Localizer(
                Base(new[] { Plain(0, "a"), Plain(2, "c") }), null, NoCoverage()));
            Assert.That(error.Message, Does.Contain("missing index 1"));
        }

        [Test]
        public void T14_HugeBaseIndex_IsRejectedWithoutAllocatingBySize()
        {
            Assert.Throws<ArgumentException>(() => new Localizer(
                Base(new[] { Plain(0, "a"), Plain(int.MaxValue, "z") }), null, NoCoverage()));
        }

        [Test]
        public void T14_SelectedIndexAtOrAboveBaseCount_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new Localizer(
                Base(new[] { Plain(0, "a"), Plain(1, "b") }),
                Selected(French, new[] { Plain(2, "c") }),
                NoCoverage()));
        }

        [TestCase("{subject")]
        [TestCase("subject}")]
        [TestCase("{{subject}}")]
        [TestCase("{sub{ject}")]
        [TestCase("{}")]
        public void T14_MalformedTemplateBraces_AreRejected(string template)
        {
            Assert.Throws<ArgumentException>(() => Plain(0, template));
            Assert.Throws<ArgumentException>(() => new TemplateVariant(PlayingTime, 0, "g", SelectorKind.Gender,
                TemplateForm.ForGender(GrammaticalGender.Unspecified, template)));
        }

        [Test]
        public void T14_BaseVariantDeclaringPluralSelector_IsRejected()
        {
            var basePlural = new TemplateCatalogue(
                LocaleId.BaseLocale, new StaticTextEntry[0], new[] { GoalsVariant() }, new CitationClause[0], SyntheticSlavicRule);

            var error = Assert.Throws<ArgumentException>(() => new Localizer(basePlural, null, NoCoverage()));
            Assert.That(error.Message, Does.Contain("declares a Plural selector"));
        }

        [Test]
        public void T14_BaseVariantDeclaringGenderSelector_IsRejected()
        {
            var baseGender = Base(new[]
            {
                new TemplateVariant(PlayingTime, 0, "g", SelectorKind.Gender, TemplateForm.ForGender(GrammaticalGender.Unspecified, "x"))
            });

            var error = Assert.Throws<ArgumentException>(() => new Localizer(baseGender, null, NoCoverage()));
            Assert.That(error.Message, Does.Contain("declares a Gender selector"));
        }

        [Test]
        public void T14_SelectedPluralSelectorWithoutRule_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => Selected(new LocaleId("pl"), new[] { GoalsVariant() }));
        }

        [Test]
        public void T14_SelectorVariantWithoutDefaultForm_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new TemplateVariant(PlayingTime, 0, "goals", SelectorKind.Plural,
                TemplateForm.ForPlural(PluralCategory.One, "one")));
            Assert.Throws<ArgumentException>(() => new TemplateVariant(PlayingTime, 0, "g", SelectorKind.Gender,
                TemplateForm.ForGender(GrammaticalGender.Feminine, "f")));
        }

        [Test]
        public void T14_SelectedOrphans_AreRejected()
        {
            TemplateCatalogue baseCatalogue = Base(new[] { Plain(0, "a") });

            Assert.Throws<ArgumentException>(() => new Localizer(
                baseCatalogue, Selected(French, new TemplateVariant[0], new[] { new StaticTextEntry(MenuStart, "x") }), NoCoverage()));
            Assert.Throws<ArgumentException>(() => new Localizer(
                baseCatalogue, Selected(French, new[] { new TemplateVariant(new TextTemplateId(Tag, 5), 0, "x") }), NoCoverage()));
            Assert.Throws<ArgumentException>(() => new Localizer(
                baseCatalogue,
                Selected(French, new TemplateVariant[0], clauses: new[] { new CitationClause(new CitationClauseKey(Tag, 3), "x") }),
                NoCoverage()));
        }

        // ---- T15: immutability --------------------------------------------------------------------

        [Test]
        public void T15_CallerArraysMutatedAfterConstruction_DoNotChangeOutput()
        {
            var statics = new[] { new StaticTextEntry(MenuStart, "Start") };
            var variants = new[] { Plain(0, "{subject} v0") };
            var clauses = new[] { new CitationClause(new CitationClauseKey(Tag, 3), "Clause.") };
            var forms = new[]
            {
                TemplateForm.ForGender(GrammaticalGender.Unspecified, "default"),
                TemplateForm.ForGender(GrammaticalGender.Feminine, "feminine")
            };
            var genderVariant = new TemplateVariant(PlayingTime, 0, "g", SelectorKind.Gender, forms);
            var requiredKeys = new[] { MenuStart };
            var coverage = new CatalogueCoverage(requiredKeys, new TextTemplateId[0], new CitationClauseKey[0]);
            var baseCatalogue = new TemplateCatalogue(LocaleId.BaseLocale, statics, variants, clauses);
            var localizer = new Localizer(baseCatalogue, Selected(new LocaleId("de"), new[] { genderVariant }), coverage);
            var baseOnly = new Localizer(baseCatalogue, null, coverage);

            statics[0] = new StaticTextEntry(MenuStart, "Mutated");
            variants[0] = Plain(0, "Mutated");
            clauses[0] = new CitationClause(new CitationClauseKey(Tag, 3), "Mutated.");
            forms[1] = TemplateForm.ForGender(GrammaticalGender.Feminine, "mutated");
            requiredKeys[0] = Scoreline;
            var feminine = new NamedSelectorSet(new NamedSelector("g", SelectorOperand.FromGender(GrammaticalGender.Feminine)));

            Assert.That(localizer.Resolve(MenuStart), Is.EqualTo("Start"));
            Assert.That(localizer.Render(Request(0UL, default, feminine, hasCitedEpisode: true, citationKind: 3)), Is.EqualTo("feminine Clause."));
            Assert.That(baseOnly.Render(Request(0UL, Slots("Kade", "X", "1-0"), hasCitedEpisode: true, citationKind: 3)), Is.EqualTo("Kade v0 Clause."));
        }

        // ---- T16: defensive render paths ----------------------------------------------------------

        [Test]
        public void T16_UnadmittedTemplate_RendersEmpty_AndUnrequiredMissingClauseAppendsNothing()
        {
            var localizer = new Localizer(Base(new[] { Plain(0, "Text.") }), null, NoCoverage());

            Assert.That(localizer.Render(Request(3UL, default, id: Unadmitted)), Is.EqualTo(string.Empty));
            Assert.That(localizer.Render(default(LocalizedTextRequest)), Is.EqualTo(string.Empty));
            Assert.That(localizer.Render(Request(0UL, default, hasCitedEpisode: true, citationKind: 9)), Is.EqualTo("Text."));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 tests T1–T16 (localization-l2-plan v0.5 §6). |
#endregion
