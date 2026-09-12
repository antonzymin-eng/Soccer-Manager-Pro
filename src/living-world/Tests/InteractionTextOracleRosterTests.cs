// File:     src/living-world/Tests/InteractionTextOracleRosterTests.cs
// Created:  2026-09-08
// Modified: 2026-09-08
// Author:   —
// Spec:     Localization & Accessibility #49 FR-LC-016 / §7.5; Living World System #22 FR-LW-028;
//           Testing Strategy #19; Code Standards #20
// Purpose:  Freeze the complete current InteractionIntent/EventKind rosters so append-only growth
//           cannot add unmigrated localization content between L3A capture and L3B migration.

using System;

using NUnit.Framework;

using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public sealed class InteractionTextOracleRosterTests
    {
        [Test]
        public void ProducerEnumRosters_AreFrozenUntilL3B()
        {
            Assert.AreEqual(
                InteractionTextOracleExpectations.DefinedIntentCount,
                Enum.GetValues(typeof(InteractionIntent)).Length,
                "InteractionIntent roster changed after the L3A oracle was frozen");
            Assert.AreEqual(
                InteractionTextOracleExpectations.DefinedEventKindCount,
                Enum.GetValues(typeof(EventKind)).Length,
                "EventKind roster changed after the L3A oracle was frozen");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-08 | —      | L3A roster lock: production enum sizes must match the frozen  |
// |         |            |        | expectations until the L3B migration completes.              |
#endregion
