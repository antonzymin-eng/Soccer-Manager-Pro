// File:     src/living-world/Tests/InteractionTextOracleTests.cs
// Created:  2026-09-07
// Modified: 2026-09-07
// Author:   —
// Spec:     Localization & Accessibility #49 FR-LC-016 / §7.5; Living World System #22 §3.3;
//           Testing Strategy #19; Code Standards #20
// Purpose:  Pre-localization-migration golden harness for every current interaction template/variant,
//           every citation clause, exact expansion/spacing, score formatting, and refusal cursors.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    /// <summary>
    /// Freezes the existing English/cursor behaviour before #49 moves authored surface text out of
    /// <c>living-world</c>. Expected data lives separately in
    /// <see cref="InteractionTextOracleExpectations"/> and is migration-independent: L3B may repoint
    /// this harness to the new boundary/renderer, but must not edit the frozen expectations file.
    /// </summary>
    [TestFixture]
    public sealed class InteractionTextOracleTests
    {
        // Chosen empirically against the current world.text stream so draw % rowCount lands on
        // variant index 0/1/2 respectively. A failure here therefore distinguishes draw movement
        // from an authored-row change instead of hiding the selection mapping behind a search loop.
        private const ulong Variant0Seed = 2UL;
        private const ulong Variant1Seed = 0xC0FFEE01UL;
        private const ulong Variant2Seed = 3UL;

        private static InteractionSlots StandardSlots()
            => new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                2,
                1);

        private static MemoryEpisode Episode(float salience, EventKind kind)
            => new MemoryEpisode(0u, kind, salience, 10u, 1);

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Corpus_ExactRowCountAndOrder_IsFrozen(int intentOrdinal)
        {
            string[] actual = InteractionTextCorpus.TemplatesFor((InteractionIntent)intentOrdinal);
            Assert.AreEqual(
                InteractionTextOracleExpectations.VariantCountsByIntentOrdinal[intentOrdinal],
                actual.Length,
                $"intent ordinal {intentOrdinal} variant count moved");
            CollectionAssert.AreEqual(
                InteractionTextOracleExpectations.TemplatesByIntentOrdinal[intentOrdinal],
                actual,
                $"intent ordinal {intentOrdinal} row changed, reordered, or deleted");
        }

        [Test]
        public void Corpus_EveryCitationClause_IsFrozen()
        {
            Assert.AreEqual(
                InteractionTextOracleExpectations.CitableEventKindCount + 1,
                InteractionTextOracleExpectations.CitationClausesByEventKindOrdinal.Length);

            for (int kindOrdinal = 1;
                kindOrdinal <= InteractionTextOracleExpectations.CitableEventKindCount;
                kindOrdinal++)
            {
                Assert.AreEqual(
                    InteractionTextOracleExpectations.CitationClausesByEventKindOrdinal[kindOrdinal],
                    InteractionTextCorpus.EpisodeClause((EventKind)kindOrdinal),
                    $"EventKind ordinal {kindOrdinal} citation clause changed");
            }
        }

        [Test]
        public void Generator_PinnedSeedsVisitEveryCurrentVariant_ByteExactly()
        {
            InteractionSlots slots = StandardSlots();

            AssertVariant(
                Variant0Seed,
                InteractionIntent.MediaProvokeTitlePressure,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[1][0]);
            AssertVariant(
                Variant1Seed,
                InteractionIntent.MediaProvokeTitlePressure,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[1][1]);
            AssertVariant(
                Variant2Seed,
                InteractionIntent.MediaProvokeTitlePressure,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[1][2]);

            AssertVariant(
                Variant0Seed,
                InteractionIntent.PlayerQuestionsMinutes,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[2][0]);
            AssertVariant(
                Variant1Seed,
                InteractionIntent.PlayerQuestionsMinutes,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[2][1]);

            AssertVariant(
                Variant0Seed,
                InteractionIntent.BoardSignalsConfidence,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[3][0]);
            AssertVariant(
                Variant1Seed,
                InteractionIntent.BoardSignalsConfidence,
                slots,
                InteractionTextOracleExpectations.ExpandedVariantsByIntentOrdinal[3][1]);
        }

        [Test]
        public void Generator_ScorelessScore_IsFrozen()
        {
            InteractionSlots slots = new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                0,
                0);

            AssertVariant(
                Variant0Seed,
                InteractionIntent.MediaProvokeTitlePressure,
                slots,
                InteractionTextOracleExpectations.MediaVariant0Scoreless);
        }

        [Test]
        public void Generator_EveryCitationClause_IsFrozenEndToEnd()
        {
            for (int kindOrdinal = 1;
                kindOrdinal <= InteractionTextOracleExpectations.CitableEventKindCount;
                kindOrdinal++)
            {
                MemoryEpisode episode = Episode(0.55f, (EventKind)kindOrdinal);
                InteractionSlots slots = new InteractionSlots(
                    InteractionTextOracleExpectations.Subject,
                    InteractionTextOracleExpectations.Opponent,
                    2,
                    1,
                    in episode);

                AssertVariant(
                    Variant0Seed,
                    InteractionIntent.MediaProvokeTitlePressure,
                    slots,
                    InteractionTextOracleExpectations.CitedMediaVariant0ByEventKindOrdinal[kindOrdinal]);
            }
        }

        [Test]
        public void Generator_MultiDigitScoreAndCitationSpacing_AreFrozen()
        {
            MemoryEpisode episode = Episode(0.55f, EventKind.ManagerCriticism);
            InteractionSlots slots = new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                12,
                10,
                in episode);

            Assert.AreEqual(
                InteractionTextOracleExpectations.MediaVariant0MultiDigitCriticism,
                GenerateAndAssertSingleAdvance(
                    Variant0Seed,
                    InteractionIntent.MediaProvokeTitlePressure,
                    slots));
        }

        [Test]
        public void Generator_EveryPreDrawRefusalClass_PreservesCursorAndActionOrdinal()
        {
            InteractionSlots valid = StandardSlots();
            AssertRefusalPreservesCursor(
                typeof(ArgumentOutOfRangeException), InteractionIntent.None, valid);
            AssertRefusalPreservesCursor(
                typeof(ArgumentOutOfRangeException), (InteractionIntent)200, valid);

            InteractionSlots malformed = default;
            AssertRefusalPreservesCursor(
                typeof(ArgumentException), InteractionIntent.PlayerQuestionsMinutes, malformed);

            MemoryEpisode belowThreshold = Episode(0.10f, EventKind.ManagerCriticism);
            InteractionSlots belowThresholdSlots = new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                2,
                1,
                in belowThreshold);
            AssertRefusalPreservesCursor(
                typeof(InvalidOperationException), InteractionIntent.PlayerQuestionsMinutes, belowThresholdSlots);

            MemoryEpisode nanSalience = Episode(float.NaN, EventKind.ManagerCriticism);
            InteractionSlots nanSlots = new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                2,
                1,
                in nanSalience);
            AssertRefusalPreservesCursor(
                typeof(InvalidOperationException), InteractionIntent.PlayerQuestionsMinutes, nanSlots);

            MemoryEpisode kindless = Episode(0.90f, EventKind.None);
            InteractionSlots kindlessSlots = new InteractionSlots(
                InteractionTextOracleExpectations.Subject,
                InteractionTextOracleExpectations.Opponent,
                2,
                1,
                in kindless);
            AssertRefusalPreservesCursor(
                typeof(ArgumentOutOfRangeException), InteractionIntent.PlayerQuestionsMinutes, kindlessSlots);
        }

        private static void AssertVariant(
            ulong seed,
            InteractionIntent intent,
            InteractionSlots slots,
            string expected)
        {
            Assert.AreEqual(expected, GenerateAndAssertSingleAdvance(seed, intent, slots));
        }

        private static string GenerateAndAssertSingleAdvance(
            ulong seed,
            InteractionIntent intent,
            InteractionSlots slots)
        {
            DeterministicRngService service = new DeterministicRngService(seed);
            InteractionTextGenerator generator = new InteractionTextGenerator(service);

            string text = generator.Generate(intent, slots);
            RngStreamState state = service.GetStreamState(generator.StreamIndex);
            Assert.AreEqual(1UL, state.RngCursor, "successful generation must consume exactly one world.text draw");
            Assert.AreEqual(1UL, state.ActionOrdinal, "successful generation must consume exactly one world.text action");
            return text;
        }

        private static void AssertRefusalPreservesCursor(
            Type expectedException,
            InteractionIntent intent,
            InteractionSlots slots)
        {
            DeterministicRngService service = new DeterministicRngService(Variant0Seed);
            InteractionTextGenerator generator = new InteractionTextGenerator(service);

            Assert.Throws(expectedException, () => generator.Generate(intent, slots));
            RngStreamState state = service.GetStreamState(generator.StreamIndex);
            Assert.AreEqual(0UL, state.RngCursor, "a pre-draw refusal must not consume the world.text cursor");
            Assert.AreEqual(0UL, state.ActionOrdinal, "a pre-draw refusal must not consume a world.text action");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-07 | —      | Localization L3A: pre-migration exact-English/cursor oracle.  |
// | 1.1     | 2026-09-07 | —      | Review: frozen expectations split from harness; added 0-0,    |
// |         |            |        | all-six-clause end-to-end coverage, seed rationale, and       |
// |         |            |        | independent per-intent row mutation diagnostics.              |
#endregion
