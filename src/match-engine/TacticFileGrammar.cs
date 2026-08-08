// File:     src/match-engine/TacticFileGrammar.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2, Tactical Presets #26 KD-6, FR-CS-019; Code Standards #20
// Purpose:  The shared line-oriented tactic-file grammar helpers (WS-1). Both TeamTacticFileLoader and
//           TacticPresetFileLoader call these, so the two Stage-0 text formats can never drift and no
//           second hand-rolled enum/scalar parser exists.

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Shared parse helpers for the Stage-0 human-authoring tactic text formats (the
    /// <c>TeamTacticFileLoader</c> / <c>TacticPresetFileLoader</c> grammar). Each <c>Xxx(kv, key, …)</c>
    /// helper reads and REMOVES the key from the section map (so a leftover key is an unknown field the
    /// caller can fail loud on) and returns the parsed value or the supplied identity fallback; malformed
    /// values throw <see cref="FormatException"/>. NOT a determinism-pinned wire format — only the parsed
    /// tactic VALUES enter the digest, never the grammar.
    /// </summary>
    internal static class TacticFileGrammar
    {
        /// <summary>Strips a trailing <c>#</c> comment from a line.</summary>
        internal static string StripComment(string line)
        {
            int hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }

        /// <summary>Reads an enum-valued key (member name, case-insensitive); absent ⇒ fallback.</summary>
        internal static T ParseEnum<T>(Dictionary<string, string> kv, string key, T fallback) where T : struct
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

        /// <summary>Reads an invariant-culture float key; absent ⇒ fallback.</summary>
        internal static float Float(Dictionary<string, string> kv, string key, float fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            return ParseFloatOrThrow(raw, key);
        }

        /// <summary>
        /// Reads a REQUIRED invariant-culture float key bounded to [<paramref name="min"/>,
        /// <paramref name="max"/>]; an absent key, an unparsable value, or an out-of-range value each
        /// throws (used for the preset A.3 scoring row, which has no neutral identity).
        /// </summary>
        internal static float RequiredFloatInRange(Dictionary<string, string> kv, string key, float min, float max)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                throw new FormatException($"Tactic file: required key '{key}' is missing.");
            }
            kv.Remove(key);
            float parsed = ParseFloatOrThrow(raw, key);
            // Negated compare fails loud on NaN too.
            if (!(parsed >= min && parsed <= max))
            {
                throw new FormatException(
                    $"Tactic file: '{raw}' is out of range [{min}, {max}] for key '{key}'.");
            }
            return parsed;
        }

        /// <summary>Reads a bool key (true/false); absent ⇒ fallback.</summary>
        internal static bool Bool(Dictionary<string, string> kv, string key, bool fallback)
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

        /// <summary>Reads a TimeWasting dial [0..TIME_WASTING_MAX]; absent ⇒ fallback.</summary>
        internal static byte TimeWasting(Dictionary<string, string> kv, string key, byte fallback)
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

        /// <summary>
        /// Builds one <see cref="TeamTactic"/> from a section's key/value map — every field defaults to
        /// the <see cref="TeamTactic.Balanced"/> identity when its key is absent (FR-TI-031), so a
        /// partial section is well-defined and an empty section reproduces Balanced exactly. Each
        /// consumed key is removed; the CALLER performs the leftover-unknown-key check (after consuming
        /// any of its own non-TeamTactic keys, e.g. the preset name/affinity).
        /// </summary>
        internal static TeamTactic BuildTeamTactic(Dictionary<string, string> kv)
        {
            TeamTactic d = TeamTactic.Balanced;

            return new TeamTactic(
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
                timeWasting:      TimeWasting(kv, "timeWasting", d.TimeWasting),
                markingOrientation: ParseEnum(kv, "markingOrientation", d.MarkingOrientation),
                dismarkIntensity:   ParseEnum(kv, "dismarkIntensity",   d.DismarkIntensity),
                buildUpStructure:   ParseEnum(kv, "buildUpStructure",   d.BuildUpStructure),
                rotationFreedom:    ParseEnum(kv, "rotationFreedom",    d.RotationFreedom));
        }

        private static float ParseFloatOrThrow(string raw, string key)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            throw new FormatException($"Tactic file: '{raw}' is not a valid number for key '{key}'.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): extracted the shared line-oriented grammar     |
// |         |            |        |   helpers (StripComment/ParseEnum/Float/Bool/TimeWasting/      |
// |         |            |        |   BuildTeamTactic) from TeamTacticFileLoader so TacticPreset-  |
// |         |            |        |   FileLoader reuses them; + RequiredFloatInRange for the       |
// |         |            |        |   preset A.3 scoring keys (no neutral identity).               |
#endregion
