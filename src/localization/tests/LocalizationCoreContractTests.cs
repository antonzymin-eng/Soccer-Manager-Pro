// ============================================================================
// File:     src/localization/tests/LocalizationCoreContractTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §5, FR-LC-001-005/009/010/012/014/020
// Purpose:  L1 contract, value-safety, selector-shape and dependency-boundary tests.
// ============================================================================

using System;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.Localization.Tests
{
    public sealed class LocalizationCoreContractTests
    {
        [Test]
        public void DefaultIdentities_DoNotAliasConstructedIdentities()
        {
            LocalizationKey key = new LocalizationKey("ui.continue");
            LocaleId locale = new LocaleId("en");
            TextTemplateId template = new TextTemplateId(1, 0);

            Assert.That(default(LocalizationKey).IsValid, Is.False);
            Assert.That(default(LocaleId).IsValid, Is.False);
            Assert.That(default(TextTemplateId).IsValid, Is.False);
            Assert.That(default(LocalizationKey), Is.Not.EqualTo(key));
            Assert.That(default(LocaleId), Is.Not.EqualTo(locale));
            Assert.That(default(TextTemplateId), Is.Not.EqualTo(template));
        }

        [Test]
        public void NamedSlotSet_DefensivelyCopiesAndCanonicalizesCallerArray()
        {
            NamedSlot[] source =
            {
                new NamedSlot("player", "Marta"),
                new NamedSlot("club", "Dynamo")
            };
            NamedSlotSet set = new NamedSlotSet(source);
            NamedSlotSet reversed = new NamedSlotSet(source[1], source[0]);

            source[0] = new NamedSlot("player", "Changed");

            Assert.That(set, Is.EqualTo(reversed));
            Assert.That(set.GetHashCode(), Is.EqualTo(reversed.GetHashCode()));
            Assert.That(set.TryGetValue("player", out string value), Is.True);
            Assert.That(value, Is.EqualTo("Marta"));
        }

        [Test]
        public void NamedSelectorSet_CarriesTypedValuesAndDefensivelyCopiesCallerArray()
        {
            NamedSelector[] source =
            {
                new NamedSelector("count", new SelectorOperand(2L)),
                new NamedSelector("subject", new SelectorOperand(GrammaticalGender.Feminine))
            };
            NamedSelectorSet set = new NamedSelectorSet(source);
            NamedSelectorSet reversed = new NamedSelectorSet(source[1], source[0]);

            source[0] = new NamedSelector("count", new SelectorOperand(99L));

            Assert.That(set, Is.EqualTo(reversed));
            Assert.That(set.GetHashCode(), Is.EqualTo(reversed.GetHashCode()));
            Assert.That(set.TryGetValue("count", out SelectorOperand operand), Is.True);
            Assert.That(operand.HasCardinal, Is.True);
            Assert.That(operand.CardinalValue, Is.EqualTo(2L));
        }

        [Test]
        public void Request_CarriesTypedSelectorsWithoutRenderingPolicy()
        {
            NamedSelectorSet selectors = new NamedSelectorSet(
                new NamedSelector("count", new SelectorOperand(3L, GrammaticalGender.Neutral)));
            LocalizedTextRequest request = new LocalizedTextRequest(
                new TextTemplateId(7, 2),
                42UL,
                new NamedSlotSet(new NamedSlot("club", "Dynamo")),
                selectors,
                true,
                5);

            Assert.That(request.Selectors, Is.EqualTo(selectors));
            Assert.That(request.SelectionDraw, Is.EqualTo(42UL));
        }

        [Test]
        public void CitationNamespace_IncludesProducerTagThroughTemplateIdentity()
        {
            LocalizedTextRequest first = new LocalizedTextRequest(
                new TextTemplateId(1, 4), 0UL, default(NamedSlotSet), default(NamedSelectorSet), true, 9);
            LocalizedTextRequest second = new LocalizedTextRequest(
                new TextTemplateId(2, 4), 0UL, default(NamedSlotSet), default(NamedSelectorSet), true, 9);

            Assert.That(first.CitationKind, Is.EqualTo(second.CitationKind));
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(first.Id.ProducerTag, Is.Not.EqualTo(second.Id.ProducerTag));
        }

        [Test]
        public void ILocalizer_ExposesOnlyApprovedResolveAndRenderSurface()
        {
            MethodInfo[] methods = typeof(ILocalizer).GetMethods();
            Assert.That(methods.Length, Is.EqualTo(2));

            MethodInfo resolve = methods.Single(method => method.Name == "Resolve");
            MethodInfo render = methods.Single(method => method.Name == "Render");

            Assert.That(resolve.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(resolve.GetParameters().Single().ParameterType, Is.EqualTo(typeof(LocalizationKey)));
            Assert.That(render.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(render.GetParameters().Single().ParameterType, Is.EqualTo(typeof(LocalizedTextRequest).MakeByRefType()));
        }

        [Test]
        public void CoreAssembly_HasNoOtherTacticalDirectorAssemblyReference()
        {
            string[] projectReferences = typeof(ILocalizer).Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name.StartsWith("TacticalDirector.", StringComparison.Ordinal))
                .ToArray();

            Assert.That(projectReferences, Is.Empty);
        }

        [Test]
        public void BaseLocale_IsFixedEnglishIdentity()
        {
            Assert.That(LocalizationConstants.BASE_LOCALE, Is.EqualTo("en"));
            Assert.That(LocaleId.BaseLocale, Is.EqualTo(new LocaleId("en")));
        }

        [Test]
        public void EqualContractValues_ProduceEqualStableHashes()
        {
            LocalizationKey firstKey = new LocalizationKey("menu.load");
            LocalizationKey secondKey = new LocalizationKey("menu.load");
            TextTemplateId firstTemplate = new TextTemplateId(3, 11);
            TextTemplateId secondTemplate = new TextTemplateId(3, 11);

            Assert.That(firstKey.GetHashCode(), Is.EqualTo(secondKey.GetHashCode()));
            Assert.That(firstTemplate.GetHashCode(), Is.EqualTo(secondTemplate.GetHashCode()));
        }

        [Test]
        public void DuplicateSlotAndSelectorNames_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new NamedSlotSet(
                new NamedSlot("name", "one"),
                new NamedSlot("name", "two")));
            Assert.Throws<ArgumentException>(() => new NamedSelectorSet(
                new NamedSelector("count", new SelectorOperand(1L)),
                new NamedSelector("count", new SelectorOperand(2L))));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 contract and dependency-boundary coverage. |
#endregion
