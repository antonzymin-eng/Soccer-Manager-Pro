// File:     src/project-constants/GameplayConfigFileLoader.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019 [GT] config loading), §4.2
// Purpose:  Parses the on-disk [GT] config text format into an immutable GameplayConfig — the Stage-1
//           "config loader" the [GT] catalogues read at boot (parser swap; values feed the sim, grammar does not).

using System;
using System.Collections.Generic;

namespace TacticalDirector.ProjectConstants
{
    /// <summary>
    /// Parses the Stage-1 <c>[GT]</c> config text file into an immutable <see cref="GameplayConfig"/>.
    /// This is the FR-CS-019 "loaded from a tunable config source at boot" mechanism: a catalogue's
    /// <c>[GT]</c> constants read their values from the resulting <see cref="GameplayConfig"/> instead of
    /// being compile-time literals.
    /// </summary>
    /// <remarks>
    /// FORMAT — line-oriented, case-insensitive <c>key = value</c> pairs grouped under <c>[section]</c>
    /// headers (the section is the owning catalogue, conventionally its spec folder name, e.g.
    /// <c>[ball-physics]</c>). Blank lines and <c>#</c> comments are ignored. An OMITTED key inherits the
    /// reader's design-time fallback, so an empty / comment-only / <c>null</c> file parses to
    /// <see cref="GameplayConfig.Empty"/> — applying it is behaviour-neutral. Values are typed at the READ
    /// site (<see cref="GameplayConfig.GetFloat"/> etc.), not here; the loader stores raw strings.
    ///
    /// FAIL LOUD — a key before any section header, an empty key, a duplicate <c>section.key</c>, a
    /// malformed section header, or a line that is neither a comment, a header, nor <c>key = value</c>
    /// throws <see cref="FormatException"/>. A config typo must never be silently dropped. Unlike a
    /// closed-vocabulary loader (e.g. the #21 team-tactic loader's fixed <c>[home]</c>/<c>[away]</c>),
    /// any section name is accepted — the valid sections are the union of every catalogue, not known here.
    ///
    /// SCOPE — the human-authoring text grammar, NOT a determinism-pinned wire format (only the resulting
    /// VALUES reach a simulation, via the consuming catalogue's <c>static readonly</c> fields). A future
    /// binary or richer grammar may replace this loader; it still produces a <see cref="GameplayConfig"/>
    /// and the catalogues that read from it are untouched (the #19 <c>ScenarioIndex</c> / #21
    /// <c>TeamTacticFileLoader</c> parser-swap precedent).
    /// </remarks>
    public static class GameplayConfigFileLoader
    {
        /// <summary>
        /// Parses <paramref name="text"/> (the full file contents) into a <see cref="GameplayConfig"/>.
        /// </summary>
        /// <param name="text">File contents. <c>null</c> or empty yields <see cref="GameplayConfig.Empty"/>.</param>
        /// <exception cref="FormatException">A malformed line, key before any section, or duplicate key.</exception>
        public static GameplayConfig Parse(string text)
        {
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string section = null; // keys before any section header are rejected.

            string[] lines = (text ?? string.Empty).Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = StripComment(lines[n]).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    section = ReadSection(line, n);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 0)
                {
                    throw new FormatException(
                        $"Config line {n + 1}: expected 'key = value' or a [section] header.");
                }
                if (section == null)
                {
                    throw new FormatException(
                        $"Config line {n + 1}: a key appears before any [section] header.");
                }

                string key = line.Substring(0, eq).Trim();
                if (key.Length == 0)
                {
                    throw new FormatException($"Config line {n + 1}: empty key.");
                }

                string composed = section + "." + key;
                if (entries.ContainsKey(composed))
                {
                    throw new FormatException(
                        $"Config line {n + 1}: duplicate key '{key}' in section '[{section}]'.");
                }
                entries[composed] = line.Substring(eq + 1).Trim();
            }

            return entries.Count == 0 ? GameplayConfig.Empty : new GameplayConfig(entries);
        }

        private static string ReadSection(string line, int n)
        {
            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                throw new FormatException($"Config line {n + 1}: malformed section header '{line}'.");
            }
            string name = line.Substring(1, line.Length - 2).Trim();
            if (name.Length == 0)
            {
                throw new FormatException($"Config line {n + 1}: empty section header.");
            }
            return name;
        }

        private static string StripComment(string line)
        {
            int hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-30 | —      | Initial implementation — FR-CS-019 on-disk [GT] config text → GameplayConfig parser. |
#endregion
