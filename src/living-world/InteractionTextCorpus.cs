// File:     src/living-world/InteractionTextCorpus.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §3.3 (KD-6), §7.2, FR-LW-012/013/028, Code Standards #20
// Purpose:  The Stage-0 in-code authored template corpus for §3.3 surface-text expansion: templates
//           indexed by InteractionIntent ordinal + episode-citation clauses per EventKind.

using System;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The static authored corpus <see cref="InteractionTextGenerator"/> expands from (#22 §3.3,
    /// KD-6). Authored offline — never model-generated at runtime (FR-LW-012). Stage 0 authors the
    /// corpus IN CODE per the #19 ScenarioIndex / #21 TeamTacticConfig precedent: the shipped §7.2
    /// AI-assisted corpus (curation + guardrail pass pending) is a pure data swap — the on-disk
    /// encoding is D1-pinned at Stage 0+1, so no file format is invented here.
    ///
    /// DETERMINISM CONTRACT: template order within an intent row is load-bearing — the drawn
    /// world.text value selects by index (draw mod count). Rows and templates are APPEND-only, the
    /// enum-roster parallel of FR-LW-028: reordering or removing entries changes which string a given
    /// RNG cursor produces and breaks T-LW-DET-003 replay parity. Slot tokens: {subject}, {opponent},
    /// {score}.
    /// </summary>
    internal static class InteractionTextCorpus
    {
        /// <summary>
        /// Templates per <see cref="InteractionIntent"/> ordinal. Row 0 (None) is intentionally empty
        /// — a neutral/unspecified intent has no authored phrasing and is refused at generation.
        /// </summary>
        private static readonly string[][] s_templates =
        {
            // InteractionIntent.None = 0 — no authored phrasing (refused by TemplatesFor).
            Array.Empty<string>(),

            // InteractionIntent.MediaProvokeTitlePressure = 1 (vol-2 §7.1 trap class).
            new[]
            {
                "Journalists press {subject} on the title race after the {score} against {opponent}.",
                "The press corner {subject}: is the title slipping after the {score} against {opponent}?",
                "A pointed question lands on {subject} — title pressure, and that {score} against {opponent}.",
            },

            // InteractionIntent.PlayerQuestionsMinutes = 2.
            new[]
            {
                "{subject} asks why the minutes have dried up since the {opponent} match.",
                "{subject} wants a word about playing time after the {score} against {opponent}.",
            },

            // InteractionIntent.BoardSignalsConfidence = 3.
            new[]
            {
                "The board signals confidence in {subject} despite the {score} against {opponent}.",
                "Upstairs, patience holds: {subject} keeps the board's backing after {opponent}.",
            },
        };

        /// <summary>
        /// Returns the authored template row for the intent. Throws on <see cref="InteractionIntent.None"/>
        /// and on out-of-roster ordinals (the ArcEngine MaxDefinedArcKind junk-cast gate parallel) —
        /// extend the array alongside any roster APPEND so this gate keeps refusing undefined values
        /// while admitting new members.
        /// </summary>
        internal static string[] TemplatesFor(InteractionIntent intent)
        {
            int ordinal = (int)intent;
            if (ordinal <= 0 || ordinal >= s_templates.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(intent), intent,
                    "InteractionTextCorpus: no authored phrasing for this intent (None and out-of-roster values are refused).");
            }
            return s_templates[ordinal];
        }

        /// <summary>
        /// The citation clause appended when a §3.2 episode is referenced (a full authored sentence
        /// per <see cref="EventKind"/>). Throws on <see cref="EventKind.None"/> / out-of-roster —
        /// citing a kindless episode is a caller authoring error. Extend alongside any EventKind
        /// APPEND (FR-LW-028).
        /// </summary>
        internal static string EpisodeClause(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.ManagerCriticism:
                    return "The public criticism still hangs over the exchange.";
                case EventKind.ManagerDefence:
                    return "Your public defence is remembered.";
                case EventKind.Benching:
                    return "The benching has not been forgotten.";
                case EventKind.ContractSnub:
                    return "The contract snub colours the mood.";
                case EventKind.TransferRumour:
                    return "The transfer rumour lingers in the room.";
                case EventKind.PressConferenceTrap:
                    return "The last press-conference trap is fresh in mind.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind,
                        "InteractionTextCorpus: no citation clause for this EventKind (None and out-of-roster values are refused).");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (slice 3): Stage-0 in-code authored    |
// |         |            |        | corpus — templates per intent + episode clauses per EventKind |
// |         |            |        | (APPEND-only order; KD-6 offline authoring, FR-LW-012).       |
#endregion
