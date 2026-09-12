// ============================================================================
// File:     src/localization/tests/LocalizationCoreContractTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §5, FR-LC-001-005/009/010/012/014/020
// Purpose:  L1 contract, value-safety, selector-shape and dependency-boundary tests.
// ============================================================================

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        public void LocalizationKey_UsesExactOrdinalIdentityAndRejectsSurroundingWhitespace()
        {
            LocalizationKey lower = new LocalizationKey("menu.load");
            LocalizationKey upper = new LocalizationKey("MENU.LOAD");

            Assert.That(lower, Is.Not.EqualTo(upper));
            Assert.Throws<ArgumentException>(() => new LocalizationKey(" menu.load"));
            Assert.Throws<ArgumentException>(() => new LocalizationKey("menu.load "));
        }

        [Test]
        public void LocaleId_CanonicalizesCaseAndSurroundingWhitespaceOnly()
        {
            LocaleId canonical = new LocaleId("en");

            Assert.That(new LocaleId("EN"), Is.EqualTo(canonical));
            Assert.That(new LocaleId(" en "), Is.EqualTo(canonical));
            Assert.That(canonical.Value, Is.EqualTo("en"));
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
                new NamedSelector("count", SelectorOperand.FromCardinal(2L)),
                new NamedSelector("subject", SelectorOperand.FromGender(GrammaticalGender.Feminine))
            };
            NamedSelectorSet set = new NamedSelectorSet(source);
            NamedSelectorSet reversed = new NamedSelectorSet(source[1], source[0]);

            source[0] = new NamedSelector("count", SelectorOperand.FromCardinal(99L));

            Assert.That(set, Is.EqualTo(reversed));
            Assert.That(set.GetHashCode(), Is.EqualTo(reversed.GetHashCode()));
            Assert.That(set.TryGetValue("count", out SelectorOperand operand), Is.True);
            Assert.That(operand.HasCardinal, Is.True);
            Assert.That(operand.CardinalValue, Is.EqualTo(2L));
        }

        [Test]
        public void SelectorOperand_ZeroCardinalIsUnambiguousAndLocaleNeutral()
        {
            SelectorOperand zero = SelectorOperand.FromCardinal(0L);
            PropertyInfo[] properties = typeof(SelectorOperand).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Assert.That(zero.IsValid, Is.True);
            Assert.That(zero.HasCardinal, Is.True);
            Assert.That(zero.CardinalValue, Is.EqualTo(0L));
            Assert.That(properties.Any(property => property.PropertyType == typeof(string)), Is.False);
            Assert.That(properties.Any(property => property.PropertyType == typeof(LocaleId)), Is.False);
        }

        [Test]
        public void Request_CarriesTypedSelectorsWithoutRenderingPolicy()
        {
            NamedSelectorSet selectors = new NamedSelectorSet(
                new NamedSelector("count", SelectorOperand.From(3L, GrammaticalGender.Neutral)));
            LocalizedTextRequest request = new LocalizedTextRequest(
                new TextTemplateId(7, 2),
                ulong.MaxValue,
                new NamedSlotSet(new NamedSlot("club", "Dynamo")),
                selectors,
                true,
                5);

            Assert.That(request.Selectors, Is.EqualTo(selectors));
            Assert.That(request.SelectionDraw, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void SelectionDraw_PublicContractRemainsUlongEndToEnd()
        {
            PropertyInfo property = typeof(LocalizedTextRequest).GetProperty(nameof(LocalizedTextRequest.SelectionDraw));
            ConstructorInfo constructor = typeof(LocalizedTextRequest).GetConstructors().Single();
            ParameterInfo selectionParameter = constructor.GetParameters()
                .Single(parameter => parameter.Name == "selectionDraw");

            Assert.That(property, Is.Not.Null);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(ulong)));
            Assert.That(selectionParameter.ParameterType, Is.EqualTo(typeof(ulong)));

            LocalizedTextRequest request = new LocalizedTextRequest(
                new TextTemplateId(1, 1),
                ulong.MaxValue,
                default(NamedSlotSet),
                default(NamedSelectorSet),
                false,
                0);
            Assert.That(request.SelectionDraw, Is.EqualTo(ulong.MaxValue));
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
        public void ILocalizer_ExposesOnlyApprovedResolveAndRenderSurfaceWithoutBakedStringInput()
        {
            MethodInfo[] methods = typeof(ILocalizer).GetMethods();
            Assert.That(methods.Length, Is.EqualTo(2));

            MethodInfo resolve = methods.Single(method => method.Name == "Resolve");
            MethodInfo render = methods.Single(method => method.Name == "Render");

            Assert.That(resolve.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(resolve.GetParameters().Single().ParameterType, Is.EqualTo(typeof(LocalizationKey)));
            Assert.That(render.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(render.GetParameters().Single().ParameterType, Is.EqualTo(typeof(LocalizedTextRequest).MakeByRefType()));
            Assert.That(methods.SelectMany(method => method.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(string)), Is.False);
        }

        [Test]
        public void CoreAsmdef_DeclaresNoProjectReferences()
        {
            string repoRoot = FindRepositoryRoot();
            string asmdef = File.ReadAllText(Path.Combine(repoRoot, "src", "localization", "localization.asmdef"));
            string compact = new string(asmdef.Where(character => !char.IsWhiteSpace(character)).ToArray());

            Assert.That(compact, Does.Contain("\"references\":[]"));
        }

        [Test]
        public void NoOtherProductionAsmdef_ReferencesLocalizationAtL1()
        {
            string srcRoot = Path.Combine(FindRepositoryRoot(), "src");
            string testSegment = Path.DirectorySeparatorChar + "tests" + Path.DirectorySeparatorChar;
            string[] offenders = Directory.GetFiles(srcRoot, "*.asmdef", SearchOption.AllDirectories)
                .Where(path => path.IndexOf(testSegment, StringComparison.OrdinalIgnoreCase) < 0)
                .Where(path => !string.Equals(Path.GetFileName(path), "localization.asmdef", StringComparison.Ordinal))
                .Where(path => File.ReadAllText(path).Contains("TacticalDirector.Localization"))
                .ToArray();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void PublicCoreTypeShape_ContainsOnlySystemOrLocalizationTypes()
        {
            Assembly assembly = typeof(ILocalizer).Assembly;
            Type[] publicTypes = assembly.GetExportedTypes();

            foreach (Type type in publicTypes)
            {
                foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        AssertContractType(parameter.ParameterType, assembly, type.FullName + " constructor");
                    }
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    AssertContractType(method.ReturnType, assembly, type.FullName + "." + method.Name + " return");
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AssertContractType(parameter.ParameterType, assembly, type.FullName + "." + method.Name);
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    AssertContractType(property.PropertyType, assembly, type.FullName + "." + property.Name);
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    AssertContractType(field.FieldType, assembly, type.FullName + "." + field.Name);
                }
            }
        }

        [Test]
        public void CoreContracts_HaveNoMutableStaticRngOrPersistenceState()
        {
            Assembly assembly = typeof(ILocalizer).Assembly;
            Type[] authoredTypes = assembly.GetTypes()
                .Where(type => type.Namespace != null
                    && (string.Equals(type.Namespace, "TacticalDirector.Localization", StringComparison.Ordinal)
                        || type.Namespace.StartsWith("TacticalDirector.Localization.", StringComparison.Ordinal)))
                .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .ToArray();
            FieldInfo[] fields = authoredTypes
                .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .ToArray();
            string[] mutableStaticFields = fields
                .Where(field => field.IsStatic && !field.IsLiteral && !field.IsInitOnly)
                .Select(field => field.DeclaringType.FullName + "." + field.Name)
                .ToArray();

            Assert.That(
                mutableStaticFields,
                Is.Empty,
                "Authored L1 localization types must not contain mutable static fields. Compiler-generated and coverage-instrumentation types are not localization-owned state.");
            Assert.That(fields.Any(field => ContainsForbiddenStateName(field.FieldType)), Is.False);
            Assert.That(assembly.GetExportedTypes().Any(HasExplicitSerializableAttribute), Is.False);
        }

        [Test]
        public void BaseLocale_IsFixedEnglishIdentity()
        {
            Assert.That(LocalizationConstants.BASE_LOCALE, Is.EqualTo("en"));
            Assert.That(LocaleId.BaseLocale, Is.EqualTo(new LocaleId("en")));
        }

        [Test]
        public void StableHashAlgorithm_IsPinnedByGoldenContractValues()
        {
            Assert.That(new LocalizationKey("menu.load").GetHashCode(), Is.EqualTo(-618008796));
            Assert.That(new LocaleId("EN").GetHashCode(), Is.EqualTo(19578));
            Assert.That(new TextTemplateId(3, 11).GetHashCode(), Is.EqualTo(1196));
            Assert.That(SelectorOperand.FromCardinal(2L).GetHashCode(), Is.EqualTo(104494767));
        }

        [Test]
        public void DuplicateSlotAndSelectorNames_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new NamedSlotSet(
                new NamedSlot("name", "one"),
                new NamedSlot("name", "two")));
            Assert.Throws<ArgumentException>(() => new NamedSelectorSet(
                new NamedSelector("count", SelectorOperand.FromCardinal(1L)),
                new NamedSelector("count", SelectorOperand.FromCardinal(2L))));
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            Assert.Fail("Could not locate repository root from the test directory.");
            return string.Empty;
        }

        private static void AssertContractType(Type type, Assembly localizationAssembly, string context)
        {
            if (type.IsByRef || type.IsArray || type.IsPointer)
            {
                AssertContractType(type.GetElementType(), localizationAssembly, context);
                return;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                bool genericAllowed = definition.Assembly == localizationAssembly
                    || definition.Namespace != null && definition.Namespace.StartsWith("System", StringComparison.Ordinal);
                Assert.That(genericAllowed, Is.True, context + " leaked external generic type " + definition.FullName);
                foreach (Type argument in type.GetGenericArguments())
                {
                    AssertContractType(argument, localizationAssembly, context);
                }
                return;
            }

            bool allowed = type == typeof(void)
                || type.Assembly == localizationAssembly
                || type.Namespace != null && type.Namespace.StartsWith("System", StringComparison.Ordinal);
            Assert.That(allowed, Is.True, context + " leaked external type " + type.FullName);
        }

        private static bool ContainsForbiddenStateName(Type type)
        {
            string name = type.FullName ?? type.Name;
            return name.IndexOf("Random", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rng", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasExplicitSerializableAttribute(Type type)
        {
            return type.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType == typeof(SerializableAttribute));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 contract and dependency-boundary coverage. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Close §5.3/§5.4 evidence gaps: asmdef direction, type-shape, ulong, pass-through, state, identity and golden hashes. |
// | 1.2     | 2026-09-11 | GPT-5.6 Sol | Scope mutable-static lock to authored types; report exact offenders while excluding compiler-generated delegate/cache artifacts. |
// | 1.3     | 2026-09-11 | GPT-5.6 Sol | Scope authored-state reflection to localization-owned namespaces so coverage instrumentation is ignored. |
#endregion