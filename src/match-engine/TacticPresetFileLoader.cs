// File:     src/match-engine/TacticPresetFileLoader.cs
// Created:  2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 / §4.4 (FR-TP-002/013/017), KD-6; Match Engine design note §5;
//           Code Standards #20
// Purpose:  The on-disk → ITacticPresetCatalogue parser swap (the deferred #26 KD-6 disk loader). Parses
//           the Stage-0 human-authoring preset text format into a ready-to-inject catalogue.

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Parses the Stage-0 manager-preset text file into an <see cref="ITacticPresetCatalogue"/> — the
    /// KD-6 "parser swap producing the catalogue" the injectable <see cref="ArrayTacticPresetCatalogue"/>
    /// was authored to receive (the #19 <c>ScenarioIndex</c> / #21 <c>TeamTacticFileLoader</c> precedent).
    /// The result can be injected wherever the manager AI resolves preset ordinals.
    /// </summary>
    /// <remarks>
    /// FORMAT (Stage 0): line-oriented, case-insensitive <c>key = value</c>; blank lines and <c>#</c>
    /// comments ignored. Top-level keys (before the first section) carry catalogue metadata — currently
    /// the REQUIRED <c>balancedOrdinal = N</c>. One <c>[preset N]</c> section per ladder ordinal, with
    /// <c>N = 0, 1, 2, …</c> ascending and contiguous (a gap or re-order fails loud — the ordinal is the
    /// serialized preset identity, FR-TP-013 APPEND-only). Each section carries:
    /// <list type="bullet">
    /// <item>a REQUIRED <c>name</c> (authoring metadata);</item>
    /// <item>the three REQUIRED A.3 scoring keys <c>baseFit</c>/<c>aggrAffinity</c>/<c>cautAffinity</c>
    /// (floats in [−1, +1]) — unlike a team dial an affinity scalar has no neutral identity, so an
    /// omitted key fails loud rather than defaulting to 0;</item>
    /// <item>the <see cref="TeamTactic"/> dials, reusing <see cref="TacticFileGrammar"/> exactly — an
    /// omitted dial inherits the <see cref="TeamTactic.Balanced"/> identity (KD-7).</item>
    /// </list>
    /// Per-agent <c>Players</c> are Stage-0-deferred (every parsed preset has <c>Players = null</c>,
    /// matching the current in-code catalogue); a per-player grammar is added only when the catalogue
    /// carries per-agent tactics.
    ///
    /// FAIL LOUD (<see cref="FormatException"/>): empty / comment-only / null input (a catalogue has no
    /// identity default — it must have at least one preset); a malformed line / section header; a
    /// non-ascending or gapped ordinal; a missing <c>name</c> / affinity key; an out-of-range affinity;
    /// an unknown key or leftover section key; an absent or out-of-range <c>balancedOrdinal</c>, one off
    /// the pinned Balanced midpoint, or one whose section is not <see cref="TeamTactic.Balanced"/>.
    ///
    /// SCOPE — the human-authoring text format, NOT a determinism-pinned wire format: only the resulting
    /// <see cref="TacticPreset"/> VALUES (and the ordinals) feed the digest, never the grammar.
    /// </remarks>
    public static class TacticPresetFileLoader
    {
        private const string PresetSectionKeyword = "preset";
        private const string KeyName = "name";
        private const string KeyBaseFit = "baseFit";
        private const string KeyAggrAffinity = "aggrAffinity";
        private const string KeyCautAffinity = "cautAffinity";
        private const string KeyBalancedOrdinal = "balancedOrdinal";
        private const float AffinityMin = -1f;
        private const float AffinityMax = 1f;

        /// <summary>Parses <paramref name="text"/> into an injectable catalogue (fail loud on any defect).</summary>
        /// <exception cref="FormatException">A malformed / empty file, unknown key/section, or bad value.</exception>
        public static ITacticPresetCatalogue Parse(string text)
        {
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sections = new List<Dictionary<string, string>>();
            Dictionary<string, string> current = meta; // top-level keys until the first [preset N].

            string[] lines = (text ?? string.Empty).Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = TacticFileGrammar.StripComment(lines[n]).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    current = OpenPresetSection(line, n, sections);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    throw new FormatException(
                        $"Preset file line {n + 1}: expected 'key = value' or a [preset N] header.");
                }
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length == 0)
                {
                    throw new FormatException($"Preset file line {n + 1}: empty key.");
                }
                if (current.ContainsKey(key))
                {
                    throw new FormatException($"Preset file line {n + 1}: duplicate key '{key}' in the same section.");
                }
                current[key] = value;
            }

            if (sections.Count == 0)
            {
                throw new FormatException(
                    "Preset file: no [preset N] sections — a catalogue must have at least one preset.");
            }

            var presets = new List<TacticPreset>(sections.Count);
            for (int ord = 0; ord < sections.Count; ord++)
            {
                presets.Add(BuildPreset(sections[ord], ord));
            }

            byte balancedOrdinal = ResolveBalancedOrdinal(meta, presets);
            return new ArrayTacticPresetCatalogue(presets, balancedOrdinal);
        }

        // Validates the [preset N] header (N must be the next expected ordinal — ascending, contiguous)
        // and pushes a fresh section map.
        private static Dictionary<string, string> OpenPresetSection(
            string line, int n, List<Dictionary<string, string>> sections)
        {
            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                throw new FormatException($"Preset file line {n + 1}: malformed section header '{line}'.");
            }
            string inner = line.Substring(1, line.Length - 2).Trim();
            string[] parts = inner.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !string.Equals(parts[0], PresetSectionKeyword, StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ordinal))
            {
                throw new FormatException(
                    $"Preset file line {n + 1}: expected a '[preset N]' header, got '[{inner}]'.");
            }
            if (ordinal != sections.Count)
            {
                throw new FormatException(
                    $"Preset file line {n + 1}: preset ordinal {ordinal} out of order — expected {sections.Count} " +
                    "(ordinals must be 0,1,2,… ascending with no gaps; FR-TP-013).");
            }
            var section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            sections.Add(section);
            return section;
        }

        // Builds one preset from a section map, consuming name + the three affinity keys, then the
        // TeamTactic dials; any key left over is unknown and fails loud.
        private static TacticPreset BuildPreset(Dictionary<string, string> kv, int ordinal)
        {
            if (!kv.TryGetValue(KeyName, out string name) || string.IsNullOrEmpty(name))
            {
                throw new FormatException($"Preset {ordinal}: required key '{KeyName}' is missing or empty.");
            }
            kv.Remove(KeyName);

            float baseFit = TacticFileGrammar.RequiredFloatInRange(kv, KeyBaseFit, AffinityMin, AffinityMax);
            float aggr = TacticFileGrammar.RequiredFloatInRange(kv, KeyAggrAffinity, AffinityMin, AffinityMax);
            float caut = TacticFileGrammar.RequiredFloatInRange(kv, KeyCautAffinity, AffinityMin, AffinityMax);

            TeamTactic team = TacticFileGrammar.BuildTeamTactic(kv);

            if (kv.Count > 0)
            {
                var unknown = new List<string>(kv.Keys);
                throw new FormatException(
                    $"Preset {ordinal} ('{name}'): unknown key(s) '{string.Join(", ", unknown)}'.");
            }
            return new TacticPreset(name, team, baseFit, aggr, caut);
        }

        // Consumes and validates the required top-level balancedOrdinal: in range, equal to the pinned
        // Balanced midpoint (the ladder is [FIXED] APPEND-only, so Balanced stays at its ordinal), and
        // its section must be TeamTactic.Balanced (never assume — validate, KD-6).
        private static byte ResolveBalancedOrdinal(Dictionary<string, string> meta, List<TacticPreset> presets)
        {
            if (!meta.TryGetValue(KeyBalancedOrdinal, out string raw))
            {
                throw new FormatException($"Preset file: required top-level key '{KeyBalancedOrdinal}' is missing.");
            }
            meta.Remove(KeyBalancedOrdinal);
            if (meta.Count > 0)
            {
                var unknown = new List<string>(meta.Keys);
                throw new FormatException($"Preset file: unknown top-level key(s) '{string.Join(", ", unknown)}'.");
            }
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ordinal)
                || ordinal < 0 || ordinal >= presets.Count)
            {
                throw new FormatException(
                    $"Preset file: '{KeyBalancedOrdinal} = {raw}' is not a valid ordinal in [0, {presets.Count}).");
            }
            if (ordinal != TacticPresetLibrary.BalancedOrdinal)
            {
                throw new FormatException(
                    $"Preset file: {KeyBalancedOrdinal} = {ordinal} does not match the pinned Balanced midpoint " +
                    $"{TacticPresetLibrary.BalancedOrdinal} (FR-TP-013 — the ladder is APPEND-only).");
            }
            if (!IsBalancedEquivalent(presets[ordinal].Team))
            {
                throw new FormatException(
                    $"Preset file: the preset at {KeyBalancedOrdinal} = {ordinal} ('{presets[ordinal].Name}') " +
                    "is not TeamTactic.Balanced-equivalent.");
            }
            return (byte)ordinal;
        }

        private static bool IsBalancedEquivalent(in TeamTactic t)
        {
            TeamTactic b = TeamTactic.Balanced;
            return t.Mentality == b.Mentality
                && t.Formation == b.Formation
                && t.Tempo == b.Tempo
                && t.Width == b.Width
                && t.Passing == b.Passing
                && t.Pressing == b.Pressing
                && t.LineOfEngagement == b.LineOfEngagement
                && t.DefensiveLine == b.DefensiveLine
                && t.DefensiveWidth == b.DefensiveWidth
                && t.TransitionWon == b.TransitionWon
                && t.TransitionLost == b.TransitionLost
                && t.OffsideTrap == b.OffsideTrap
                && t.TriggerPressMask == b.TriggerPressMask
                && t.FocusPlay == b.FocusPlay
                && t.GkDistribution == b.GkDistribution
                && t.TimeWasting == b.TimeWasting
                && t.MarkingOrientation == b.MarkingOrientation
                && t.DismarkIntensity == b.DismarkIntensity
                && t.BuildUpStructure == b.BuildUpStructure
                && t.RotationFreedom == b.RotationFreedom;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1 / #26 KD-6): the on-disk preset-catalogue parser |
// |         |            |        |   swap — [preset N] sections + required top-level              |
// |         |            |        |   balancedOrdinal; reuses TacticFileGrammar; fail-loud gates.  |
#endregion
