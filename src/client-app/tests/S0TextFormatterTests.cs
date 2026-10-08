// File:     src/client-app/tests/S0TextFormatterTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §4, I-Q16, Code Standards #20
// Purpose:  Prove exact typed/reordered single-pass output, strict content admission and cache lifetime.

using System;
using NUnit.Framework;
using TacticalDirector.Localization;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class S0TextFormatterTests
    {
        [Test]
        public void LoadedCompiledCatalogueHasStableManifestAndEveryRoleIsCovered()
        {
            var a = new S0TextFormatter();
            var b = new S0TextFormatter();
            Assert.AreEqual(64, a.ContentSha256.Length);
            Assert.AreEqual(a.ContentSha256, b.ContentSha256);
            Assert.AreEqual(141, S0ScreenContent.All.Count);
            var localizer = new Localizer(S0ScreenContent.CreateBaseCatalogue(), null, S0ScreenContent.Coverage());
            foreach (S0TextRole role in S0ScreenContent.All)
                Assert.AreEqual(role.BasePattern, localizer.Resolve(role.Key), role.Key.Value);
        }

        [TestCase("{0", "S")]
        [TestCase("{0}}", "S")]
        [TestCase("{1}", "S")]
        [TestCase("{0:D}", "S")]
        [TestCase("{0,4}", "I")]
        [TestCase("{0:F2}", "F")]
        [TestCase("plain", "I")]
        [TestCase("{0} {2}", "II")]
        public void MalformedSchemasCannotPublish(string pattern, string types)
        {
            Assert.Throws<ArgumentException>(() => new S0TextRole("test", pattern, types));
        }

        [Test]
        public void MissingBaseIsRejectedEvenIfSelectedWouldHideIt()
        {
            var empty = new TemplateCatalogue(LocaleId.BaseLocale);
            Assert.Throws<ArgumentException>(() => new S0TextFormatter(empty, new[] { new TemplateCatalogue.StaticRow(new LocalizationKey("ui.s0.action.start"), "Start") }, new LocaleId("qps")));
        }

        [Test]
        public void BadSelectedPatternIsRejectedAndMissingSelectedRolesFallBack()
        {
            Assert.Throws<ArgumentException>(() => new S0TextFormatter(new[] { new TemplateCatalogue.StaticRow(new LocalizationKey("ui.s0.minute"), "Missing minute") }));
            var sparse = new S0TextFormatter(new[] { new TemplateCatalogue.StaticRow(new LocalizationKey("ui.s0.action.start"), "[Start translated]") });
            Assert.AreEqual("[Start translated]", sparse.Label("action.start"));
            Assert.AreEqual("Home", sparse.Label("team.home"));
            Assert.AreEqual("Minute 12", sparse.Format("clock", "minute", 12));
        }

        [Test]
        public void ReorderedSubstitutionUsesFlatNamesShirtsBenchMinuteAndLiteralBraces()
        {
            var key = new LocalizationKey("ui.s0.feedback.substitution_applied");
            var f = new S0TextFormatter(new[] { new TemplateCatalogue.StaticRow(key, "{4}/{3} #{5} bench {6} replaces {1}/{0} #{2} at {7}") });
            Assert.AreEqual("West/Miles #13 bench 2 replaces Calder/{7} #2 at 18", f.Format("outcome", "feedback.substitution_applied", "{7}", "Calder", 2, "Miles", "West", 13, 2, 18));
        }

        [Test]
        public void EverySubstitutionOutcomeHasCompleteArgumentsAndTypedAdmission()
        {
            var f = new S0TextFormatter();
            foreach (string suffix in new[]
            {
                "pending",
                "pending_paused",
                "refused",
                "not_applied",
                "send_failure"
            }

            )
            {
                string text = f.Format(suffix, "feedback.substitution_" + suffix, "Ben", "Calder", 2, "Miles", "West", 13, 2);
                StringAssert.Contains("Ben Calder", text);
                StringAssert.Contains("Miles West", text);
                StringAssert.Contains("13", text);
            }

            string applied = f.Format("applied", "feedback.substitution_applied", "Ben", "Calder", 2, "Miles", "West", 13, 2, 18);
            StringAssert.Contains("minute 18", applied);
            Assert.Throws<ArgumentException>(() => f.Format("bad", "minute", "18"));
            Assert.Throws<ArgumentException>(() => f.Format("bad", "value.decimal", float.NaN));
        }

        [Test]
        public void FixedInvariantFormattingAndCachesRefreshAndClear()
        {
            var f = new S0TextFormatter();
            string first = f.Format("decimal", "value.decimal", 12.5f);
            Assert.AreEqual("12.5", first);
            Assert.AreSame(first, f.Format("decimal", "value.decimal", 12.5f));
            Assert.AreEqual("13.5", f.Format("decimal", "value.decimal", 13.5f));
            string before = f.Format("clock", "minute", 1234);
            f.ClearMatch();
            string after = f.Format("clock", "minute", 1234);
            Assert.AreEqual(before, after);
            Assert.AreNotSame(before, after);
        }

        [TestCase(1f)]
        [TestCase(1.5f)]
        [TestCase(2f)]
        public void ApprovedScaleIsFixed(float scale) => Assert.AreEqual(scale, new S0PresentationConfiguration(scale).TextScale);
        [TestCase(0f)]
        [TestCase(0.99f)]
        [TestCase(2.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidScaleIsRejected(float scale) => Assert.Throws<ArgumentOutOfRangeException>(() => new S0PresentationConfiguration(scale));
        [Test]
        public void MissingDecorativeGlyphsUseAdmittedSemanticFallbacksAndHashActualContent()
        {
            var normal = new S0TextFormatter();
            var fallback = S0TextFormatter.WithGlyphCoverage(c => c < 128);
            StringAssert.Contains("<->", fallback.Format("marker", "pitch.marker_substitute", "H", 13));
            Assert.AreNotEqual(normal.ContentSha256, fallback.ContentSha256);
            Assert.AreEqual(normal.ContentSha256, S0TextFormatter.WithGlyphCoverage(c => true).ContentSha256);
            Assert.Throws<ArgumentException>(() => S0TextFormatter.WithGlyphCoverage(c => c != 'H'));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Admission, fallback, reordered outcomes and fixed-context caches. |
#endregion
