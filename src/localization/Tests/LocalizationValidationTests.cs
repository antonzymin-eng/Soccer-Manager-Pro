// ============================================================================
// File:     src/localization/Tests/LocalizationValidationTests.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2/§3.5/§4.2
// Purpose:  Regression coverage for malformed localization boundary values.
// ============================================================================
// §3.9.4 general-unit-test — allocation rules relaxed in test body

using System;

using NUnit.Framework;

namespace TacticalDirector.Localization.Tests
{
    /// <summary>Locks fifty malformed-input regressions at the localization boundary.</summary>
    [TestFixture]
    public sealed class LocalizationValidationTests
    {
        /// <summary>Rejects malformed locale tags rather than creating unreachable catalogue identities.</summary>
        [TestCase(null)]                 // 01
        [TestCase("")]                   // 02
        [TestCase(" ")]                  // 03
        [TestCase("e")]                  // 04
        [TestCase("engl")]               // 05
        [TestCase("-en")]                // 06
        [TestCase("en-")]                // 07
        [TestCase("en--US")]             // 08
        [TestCase("en_US")]              // 09
        [TestCase("e1")]                 // 10
        [TestCase("12")]                 // 11
        [TestCase("en-abcdefghi")]       // 12
        [TestCase("en-@")]               // 13
        [TestCase("en US")]              // 14
        [TestCase("en\nUS")]             // 15
        [TestCase("en.US")]              // 16
        [TestCase("en/US")]              // 17
        [TestCase("en\\US")]             // 18
        [TestCase("en-US-")]             // 19
        [TestCase("abcdefghijklmnopqrstuvwxyzabcdefghij")] // 20
        public void LocaleId_RejectsMalformedCode(string value)
        {
            Assert.That(() => new LocaleId(value), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>Rejects malformed static keys before catalogue lookup can silently miss.</summary>
        [TestCase(null)]                 // 21
        [TestCase("")]                   // 22
        [TestCase(" ")]                  // 23
        [TestCase(".")]                  // 24
        [TestCase("menu.")]              // 25
        [TestCase(".menu")]              // 26
        [TestCase("menu..title")]        // 27
        [TestCase("menu title")]         // 28
        [TestCase("menu/{title}")]       // 29
        [TestCase("menu\\title")]        // 30
        [TestCase("menu:title")]         // 31
        [TestCase("menu\ntitle")]        // 32
        [TestCase("menu\ttitle")]        // 33
        [TestCase("menu/title")]         // 34
        [TestCase("menu.😀")]             // 35
        public void LocalizationKey_RejectsMalformedPath(string value)
        {
            Assert.That(() => new LocalizationKey(value), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>Rejects names that cannot safely and unambiguously identify a placeholder.</summary>
        [TestCase(null)]                 // 36
        [TestCase("")]                   // 37
        [TestCase(" ")]                  // 38
        [TestCase("1subject")]           // 39
        [TestCase("-subject")]           // 40
        [TestCase("subject.name")]       // 41
        [TestCase("{subject}")]          // 42
        [TestCase("subject name")]       // 43
        [TestCase("subject-name")]       // 44
        [TestCase("subject/name")]       // 45
        [TestCase("subject\nname")]      // 46
        [TestCase("😀subject")]           // 47
        [TestCase("subject$")]           // 48
        [TestCase("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789___")] // 49
        public void NamedSlot_RejectsUnsafePlaceholderName(string value)
        {
            Assert.That(() => new NamedSlot(value, "value"), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>Rejects a clause key whose presence bit says it must be ignored.</summary>
        [Test] // 50
        public void LocalizedTextRequest_RejectsCitationKindWithoutCitation()
        {
            TextTemplateId id = new TextTemplateId(1, 0);

            Assert.That(
                () => new LocalizedTextRequest(id, 0UL, default, false, 1),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("citationKind"));
        }

        /// <summary>Rejects a partially default slot instead of admitting a null replacement.</summary>
        [Test]
        public void NamedSlotSet_RejectsDefaultSlot()
        {
            Assert.That(() => new NamedSlotSet(default(NamedSlot)), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>Retains the useful identifier shapes expected by current producers.</summary>
        [Test]
        public void Constructors_AcceptCanonicalValues()
        {
            Assert.That(new LocaleId("pt-BR").Value, Is.EqualTo("pt-BR"));
            Assert.That(new LocaleId("zh-Hant-TW").IsValid, Is.True);
            Assert.That(new LocalizationKey("match.score_title-2").IsValid, Is.True);
            Assert.That(new NamedSlot("subject_name2", "Ada").Name, Is.EqualTo("subject_name2"));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                               |
// | 1.0     | 2026-09-10 | —      | Added fifty malformed-value regression cases.       |
#endregion
