// File:     src/localization/TemplateExpander.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 §3.5, ERR-049-006
// Purpose:  Parse authored tokens once and expand them without re-scanning slot values.

using System;
using System.Collections.Generic;
using System.Text;

namespace TacticalDirector.Localization
{
    internal static class TemplateExpander
    {
        internal static ParsedTemplate Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentException("Template text is missing.", nameof(text));
            }

            var segments = new List<TemplateSegment>();
            int literalStart = 0;
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '}')
                {
                    throw new ArgumentException("Unmatched closing brace in template: " + text, nameof(text));
                }

                if (text[index] != '{')
                {
                    continue;
                }

                int opening = index;
                if (opening > literalStart)
                {
                    segments.Add(new TemplateSegment(text.Substring(literalStart, opening - literalStart), null));
                }

                int closing = opening + 1;
                while (closing < text.Length && text[closing] != '{' && text[closing] != '}')
                {
                    closing++;
                }

                if (closing == opening + 1 || closing == text.Length || text[closing] != '}')
                {
                    throw new ArgumentException("Malformed placeholder in template: " + text, nameof(text));
                }

                string token = text.Substring(opening, closing - opening + 1);
                segments.Add(new TemplateSegment(token, text.Substring(opening + 1, closing - opening - 1)));
                index = closing;
                literalStart = closing + 1;
            }

            if (literalStart < text.Length)
            {
                segments.Add(new TemplateSegment(text.Substring(literalStart), null));
            }

            return new ParsedTemplate(text, segments.ToArray());
        }

        internal static string Expand(ParsedTemplate template, in NamedSlotSet slots)
        {
            var result = new StringBuilder(template.Text.Length);
            foreach (TemplateSegment segment in template.Segments)
            {
                // ERR-049-006: inserted values are opaque, even if they contain another token.
                result.Append(segment.Name != null && slots.TryGetValue(segment.Name, out string value) ? value : segment.Text);
            }

            return result.ToString();
        }
    }

    internal sealed class ParsedTemplate
    {
        internal readonly string Text;
        internal readonly TemplateSegment[] Segments;
        internal ParsedTemplate(string text, TemplateSegment[] segments)
        {
            Text = text;
            Segments = segments;
        }
    }

    internal readonly struct TemplateSegment
    {
        internal readonly string Text;
        internal readonly string Name;
        internal TemplateSegment(string text, string name)
        {
            Text = text;
            Name = name;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial single-pass parser/expander, ERR-049-006. |
#endregion
