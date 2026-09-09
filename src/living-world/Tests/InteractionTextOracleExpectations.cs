// File:     src/living-world/Tests/InteractionTextOracleExpectations.cs
// Created:  2026-09-07
// Modified: 2026-09-08
// Author:   —
// Spec:     Localization & Accessibility #49 FR-LC-016 / §7.5; Living World System #22 §3.3;
//           Testing Strategy #19; Code Standards #20
// Purpose:  Migration-independent frozen expectations for the pre-localization English oracle.
//           L3B may repoint the test harness, but MUST NOT edit this file.

using System;

namespace TacticalDirector.LivingWorld.Tests
{
    /// <summary>
    /// Pure expected data only: no reference to InteractionTextCorpus, InteractionTextGenerator,
    /// InteractionIntent, EventKind, or any future localization type. The L3B migration is allowed
    /// to replace the harness that consumes these values, but these values themselves remain the
    /// independent judge. A zero diff to this file is therefore a mechanical migration invariant.
    /// </summary>
    internal static class InteractionTextOracleExpectations
    {
        internal const string Subject = "Kade Moreno";
        internal const string Opponent = "Halden Rovers";

        internal const int DefinedIntentCount = 4;
        internal const int DefinedEventKindCount = 7;
        internal const int CitableEventKindCount = 6;
        internal static readonly int[] VariantCountsByIntentOrdinal = { 0, 3, 2, 2 };

        internal static readonly string[][] TemplatesByIntentOrdinal =
        {
            Array.Empty<string>(),
            new[]
            {
                "Journalists press {subject} on the title race after the {score} against {opponent}.",
                "The press corner {subject}: is the title slipping after the {score} against {opponent}?",
                "A pointed question lands on {subject} — title pressure, and that {score} against {opponent}.",
            },
            new[]
            {
                "{subject} asks why the minutes have dried up since the {opponent} match.",
                "{subject} wants a word about playing time after the {score} against {opponent}.",
            },
            new[]
            {
                "The board signals confidence in {subject} despite the {score} against {opponent}.",
                "Upstairs, patience holds: {subject} keeps the board's backing after {opponent}.",
            },
        };

        internal static readonly string[] CitationClausesByEventKindOrdinal =
        {
            null,
            "The public criticism still hangs over the exchange.",
            "Your public defence is remembered.",
            "The benching has not been forgotten.",
            "The contract snub colours the mood.",
            "The transfer rumour lingers in the room.",
            "The last press-conference trap is fresh in mind.",
        };

        internal static readonly string[][] ExpandedVariantsByIntentOrdinal =
        {
            Array.Empty<string>(),
            new[]
            {
                "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers.",
                "The press corner Kade Moreno: is the title slipping after the 2-1 against Halden Rovers?",
                "A pointed question lands on Kade Moreno — title pressure, and that 2-1 against Halden Rovers.",
            },
            new[]
            {
                "Kade Moreno asks why the minutes have dried up since the Halden Rovers match.",
                "Kade Moreno wants a word about playing time after the 2-1 against Halden Rovers.",
            },
            new[]
            {
                "The board signals confidence in Kade Moreno despite the 2-1 against Halden Rovers.",
                "Upstairs, patience holds: Kade Moreno keeps the board's backing after Halden Rovers.",
            },
        };

        internal const string MediaVariant0Scoreless =
            "Journalists press Kade Moreno on the title race after the 0-0 against Halden Rovers.";

        internal static readonly string[] CitedMediaVariant0ByEventKindOrdinal =
        {
            null,
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. The public criticism still hangs over the exchange.",
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. Your public defence is remembered.",
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. The benching has not been forgotten.",
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. The contract snub colours the mood.",
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. The transfer rumour lingers in the room.",
            "Journalists press Kade Moreno on the title race after the 2-1 against Halden Rovers. The last press-conference trap is fresh in mind.",
        };

        internal const string MediaVariant0MultiDigitCriticism =
            "Journalists press Kade Moreno on the title race after the 12-10 against Halden Rovers. The public criticism still hangs over the exchange.";
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-07 | —      | L3A frozen oracle expectations. This file is intentionally     |
// |         |            |        | migration-independent and must remain byte-unchanged at L3B.   |
// | 1.1     | 2026-09-08 | —      | Freeze current InteractionIntent/EventKind roster sizes so     |
// |         |            |        | append-only enum growth cannot escape the pre-L3B oracle.      |
#endregion
