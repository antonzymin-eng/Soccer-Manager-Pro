// File:     src/match-engine/TeamTacticFileLoader.cs
// Created:  2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2 (T2 runtime activation), FR-TI-027/031, FR-CS-019; Match Engine design note §5; Code Standards #20
// Purpose:  The on-disk → TeamTacticConfig parser swap (the deferred half of the #21 config loader).
//           Parses the Stage-0 human-authoring text format into the immutable TeamTacticConfig that
//           TeamTacticConfigApplier feeds into MatchEngine.SetTeamTactic before kickoff.

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Parses the Stage-0 manager-tactic text file into a <see cref="TeamTacticConfig"/> — the
    /// "pure parser swap" the in-code <see cref="TeamTacticConfig"/> / <see cref="TeamTacticConfigApplier"/>
    /// were authored to receive (the #19 <c>ScenarioIndex</c> D1 precedent). The result is fed to
    /// <see cref="TeamTacticConfigApplier.Apply"/> unchanged.
    /// </summary>
    /// <remarks>
    /// FORMAT (Stage 0): a line-oriented, case-insensitive <c>key = value</c> grammar under two section
    /// headers, <c>[home]</c> (teamId 0) and <c>[away]</c> (teamId 1). Blank lines and <c>#</c> comments
    /// are ignored. Every <see cref="TeamTactic"/> field has a key; an OMITTED key inherits the
    /// <see cref="TeamTactic.Balanced"/> identity value, so an empty file (or one with no sections) parses
    /// to <see cref="TeamTacticConfig.Default"/> — applying it is behaviour-neutral (FR-TI-031). Enum
    /// values are the enum member names (e.g. <c>mentality = Attacking</c>); scalars use the invariant
    /// culture. An unknown key, an unparsable value, or a duplicate/unknown section throws
    /// <see cref="FormatException"/> (a config typo must fail loudly, not silently fall back).
    ///
    /// SCOPE — this is the human-authoring text format, NOT a determinism-pinned binary wire format. The
    /// file is read once before kickoff; only the resulting <see cref="TeamTactic"/> VALUES enter the
    /// per-tick snapshot digest (via the v9 serialization), never the file grammar. The Stage-1 <c>[GT]</c>
    /// loader (FR-CS-019) may replace the grammar; it still produces a <see cref="TeamTacticConfig"/> and
    /// leaves <see cref="TeamTacticConfigApplier"/> untouched.
    /// </remarks>
    public static class TeamTacticFileLoader
    {
        private const string HomeSection = "home";
        private const string AwaySection = "away";

        /// <summary>
        /// Parses <paramref name="text"/> (the full file contents) into a <see cref="TeamTacticConfig"/>.
        /// </summary>
        /// <param name="text">The tactic-file contents. <c>null</c> is treated as empty (all Balanced).</param>
        /// <exception cref="FormatException">A malformed line, unknown key/section, or unparsable value.</exception>
        public static TeamTacticConfig Parse(string text)
        {
            var home = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var away = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> current = null; // keys before any section header are rejected.

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
                    current = SelectSection(line, n, home, away);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    throw new FormatException($"Tactic file line {n + 1}: expected 'key = value' or a [section] header.");
                }
                if (current == null)
                {
                    throw new FormatException($"Tactic file line {n + 1}: a key appears before any [home]/[away] section header.");
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length == 0)
                {
                    throw new FormatException($"Tactic file line {n + 1}: empty key.");
                }
                if (current.ContainsKey(key))
                {
                    throw new FormatException($"Tactic file line {n + 1}: duplicate key '{key}' in the same section.");
                }
                current[key] = value;
            }

            return new TeamTacticConfig(BuildTactic(home), BuildTactic(away));
        }

        private static Dictionary<string, string> SelectSection(
            string line, int n,
            Dictionary<string, string> home, Dictionary<string, string> away)
        {
            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                throw new FormatException($"Tactic file line {n + 1}: malformed section header '{line}'.");
            }
            string name = line.Substring(1, line.Length - 2).Trim();
            if (string.Equals(name, HomeSection, StringComparison.OrdinalIgnoreCase))
            {
                return home;
            }
            if (string.Equals(name, AwaySection, StringComparison.OrdinalIgnoreCase))
            {
                return away;
            }
            throw new FormatException($"Tactic file line {n + 1}: unknown section '[{name}]' (expected [home] or [away]).");
        }

        private static string StripComment(string line)
        {
            int hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }

        // Builds one TeamTactic from a section's key/value map. Each field defaults to the Balanced
        // identity value when its key is absent (FR-TI-031), so a partial section is well-defined and an
        // empty section reproduces TeamTactic.Balanced exactly. Every consumed key is removed; any key
        // left over is an unknown field and fails loudly.
        private static TeamTactic BuildTactic(Dictionary<string, string> kv)
        {
            TeamTactic d = TeamTactic.Balanced;

            var tactic = new TeamTactic(
                mentality:        ParseEnum(kv, "mentality",        d.Mentality),
                formation:        ParseEnum(kv, "formation",        d.Formation),
                tempo:            ParseEnum(kv, "tempo",            d.Tempo),
                width:            ParseEnum(kv, "width",            d.Width),
                passing:          ParseEnum(kv, "passing",          d.Passing),
                pressing:         ParseEnum(kv, "pressing",         d.Pressing),
                lineOfEngagement: ParseEnum(kv, "lineOfEngagement", d.LineOfEngagement),
                defensiveLine:    Float(kv, "defensiveLine",   d.DefensiveLine),
                defensiveWidth:   ParseEnum(kv, "defensiveWidth",   d.DefensiveWidth),
                transitionWon:    ParseEnum(kv, "transitionWon",    d.TransitionWon),
                transitionLost:   ParseEnum(kv, "transitionLost",   d.TransitionLost),
                offsideTrap:      Bool(kv, "offsideTrap",      d.OffsideTrap),
                triggerPressMask: ParseEnum(kv, "triggerPressMask", d.TriggerPressMask),
                focusPlay:        ParseEnum(kv, "focusPlay",        d.FocusPlay),
                gkDistribution:   ParseEnum(kv, "gkDistribution",   d.GkDistribution),
                timeWasting:      TimeWasting(kv, "timeWasting", d.TimeWasting));

            if (kv.Count > 0)
            {
                var unknown = new List<string>(kv.Keys);
                throw new FormatException($"Tactic file: unknown key(s) '{string.Join(", ", unknown)}'.");
            }
            return tactic;
        }

        private static T ParseEnum<T>(Dictionary<string, string> kv, string key, T fallback) where T : struct
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (System.Enum.TryParse(raw, ignoreCase: true, out T parsed) && System.Enum.IsDefined(typeof(T), parsed))
            {
                return parsed;
            }
            throw new FormatException($"Tactic file: '{raw}' is not a valid {typeof(T).Name} for key '{key}'.");
        }

        private static float Float(Dictionary<string, string> kv, string key, float fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            throw new FormatException($"Tactic file: '{raw}' is not a valid number for key '{key}'.");
        }

        private static bool Bool(Dictionary<string, string> kv, string key, bool fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (bool.TryParse(raw, out bool parsed))
            {
                return parsed;
            }
            throw new FormatException($"Tactic file: '{raw}' is not a valid bool (true/false) for key '{key}'.");
        }

        private static byte TimeWasting(Dictionary<string, string> kv, string key, byte fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed)
                && parsed <= TacticalInstructionsConstants.TIME_WASTING_MAX)
            {
                return parsed;
            }
            throw new FormatException(
                $"Tactic file: '{raw}' is not a valid TimeWasting dial [0..{TacticalInstructionsConstants.TIME_WASTING_MAX}] for key '{key}'.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-29 | —      | Initial implementation — Stage-0 text → TeamTacticConfig parser swap (#21).  |
#endregion
