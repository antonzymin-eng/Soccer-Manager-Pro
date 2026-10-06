// ============================================================================
// File:     src/localization/TemplateExpander.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.5, KD-3, FR-LC-009, ERR-049-006
// Purpose:  Construction-time template parsing and single-pass named-slot expansion.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Parses authored template text once and expands it against a <see cref="NamedSlotSet"/>.
    /// A token is <c>{</c>, one or more characters other than <c>{</c> or <c>}</c>, then <c>}</c>;
    /// any other brace is rejected at parse time (there is no escape). Expansion is single-pass:
    /// each token is replaced once and substituted values are never re-scanned (ERR-049-006).
    /// </summary>
    internal static class TemplateExpander
    {
        /// <summary>One parsed piece of a template: literal text, or a token with its slot name.</summary>
        internal readonly struct Segment
        {
            internal Segment(string text, string slotName)
            {
                Text = text;
                SlotName = slotName;
            }

            /// <summary>Literal text, or the token's exact source text (used verbatim when the slot is absent).</summary>
            internal string Text { get; }

            /// <summary>Slot name for a token; null for a literal.</summary>
            internal string SlotName { get; }
        }

        /// <summary>Parses <paramref name="template"/>, throwing for any brace outside a well-formed token.</summary>
        internal static Segment[] Parse(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var segments = new List<Segment>();
            int literalStart = 0;
            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];
                if (c == '}')
                {
                    throw Malformed(template, i, "unmatched '}'");
                }

                if (c != '{')
                {
                    i++;
                    continue;
                }

                int close = i + 1;
                while (close < template.Length && template[close] != '}')
                {
                    if (template[close] == '{')
                    {
                        throw Malformed(template, close, "nested '{'");
                    }

                    close++;
                }

                if (close >= template.Length)
                {
                    throw Malformed(template, i, "unmatched '{'");
                }

                if (close == i + 1)
                {
                    throw Malformed(template, i, "empty token '{}'");
                }

                if (i > literalStart)
                {
                    segments.Add(new Segment(template.Substring(literalStart, i - literalStart), null));
                }

                segments.Add(new Segment(
                    template.Substring(i, close - i + 1),
                    template.Substring(i + 1, close - i - 1)));
                i = close + 1;
                literalStart = i;
            }

            if (literalStart < template.Length)
            {
                segments.Add(new Segment(template.Substring(literalStart), null));
            }

            return segments.ToArray();
        }

        /// <summary>
        /// Appends the single-pass expansion of <paramref name="segments"/> to <paramref name="output"/>.
        /// A token whose slot is absent is appended verbatim, as chained <c>.Replace</c> leaves it.
        /// </summary>
        internal static void Expand(Segment[] segments, in NamedSlotSet slots, StringBuilder output)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                Segment segment = segments[i];
                if (segment.SlotName != null && slots.TryGetValue(segment.SlotName, out string value))
                {
                    output.Append(value);
                }
                else
                {
                    output.Append(segment.Text);
                }
            }
        }

        private static ArgumentException Malformed(string template, int position, string reason)
        {
            return new ArgumentException(
                "Template text has " + reason + " at position " + position.ToString(CultureInfo.InvariantCulture)
                + "; braces are allowed only in well-formed {name} tokens: \"" + template + "\"");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 parser and single-pass expander (localization-l2-plan v0.5 §5.2, §5.4; ERR-049-006). |
#endregion
