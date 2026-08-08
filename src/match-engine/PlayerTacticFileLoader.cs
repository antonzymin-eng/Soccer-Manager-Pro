// File:     src/match-engine/PlayerTacticFileLoader.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Tactical Instructions #21 §3.3 (per-agent tactic config surface), FR-TI-031, FR-CS-019; Match Engine design note §5; Code Standards #20
// Purpose:  The on-disk → PlayerTacticConfig parser swap (the per-agent half of the #21 config loader,
//           sibling of TeamTacticFileLoader). Parses the Stage-0 human-authoring text format into the
//           immutable PlayerTacticConfig that PlayerTacticConfigApplier feeds into
//           MatchEngine.SetPlayerTactic before kickoff.

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Parses the Stage-0 per-agent tactic text file into a <see cref="PlayerTacticConfig"/> — the
    /// "pure parser swap" the in-code <see cref="PlayerTacticConfig"/> /
    /// <see cref="PlayerTacticConfigApplier"/> were authored to receive (the #19 <c>ScenarioIndex</c> D1
    /// precedent, mirroring <see cref="TeamTacticFileLoader"/>). The result is fed to
    /// <see cref="PlayerTacticConfigApplier.Apply"/> unchanged.
    /// </summary>
    /// <remarks>
    /// FORMAT (Stage 0): a line-oriented, case-insensitive <c>key = value</c> grammar under per-agent
    /// section headers <c>[agent N]</c> where <c>N</c> is the roster index
    /// <c>0..SQUAD_SIZE−1</c>. Blank lines and <c>#</c> comments are ignored. Every
    /// <see cref="PlayerTactic"/> / <see cref="PlayerInstructions"/> field has a key; an OMITTED key
    /// inherits the <see cref="PlayerTactic.Default(PlayerRole)"/> identity (with
    /// <see cref="PlayerRole.Default"/>), and an OMITTED agent section is the full identity tactic, so an
    /// empty file (or one with no sections) parses to <see cref="PlayerTacticConfig.Identity"/> — applying
    /// it is behaviour-neutral (FR-TI-031). Enum values are the enum member names (e.g.
    /// <c>role = Poacher</c>); <c>markTarget</c> is an integer (−1 = none, ≥0 = man-mark request) in the
    /// invariant culture. An unknown key, an unparsable value, a duplicate key, a duplicate/unknown
    /// section, or an out-of-range agent index throws <see cref="FormatException"/> (a config typo must
    /// fail loudly, not silently fall back).
    ///
    /// SCOPE — this is the human-authoring text format, NOT a determinism-pinned binary wire format. The
    /// file is read once before kickoff; only the resulting <see cref="PlayerTactic"/> VALUES enter the
    /// per-tick snapshot digest (via the v10 per-agent serialization), never the file grammar. The
    /// Stage-1 <c>[GT]</c> loader (FR-CS-019) may replace the grammar; it still produces a
    /// <see cref="PlayerTacticConfig"/> and leaves <see cref="PlayerTacticConfigApplier"/> untouched.
    /// </remarks>
    public static class PlayerTacticFileLoader
    {
        private const string AgentSectionPrefix = "agent";

        /// <summary>
        /// Parses <paramref name="text"/> (the full file contents) into a <see cref="PlayerTacticConfig"/>.
        /// </summary>
        /// <param name="text">The tactic-file contents. <c>null</c> is treated as empty (all identity).</param>
        /// <exception cref="FormatException">A malformed line, unknown key/section, out-of-range agent index, or unparsable value.</exception>
        public static PlayerTacticConfig Parse(string text)
        {
            // One key/value map per roster slot; a slot with no section stays null → identity tactic.
            var perAgent = new Dictionary<string, string>[MatchEngineConstants.SQUAD_SIZE];
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
                    current = SelectSection(line, n, perAgent);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    throw new FormatException($"Player tactic file line {n + 1}: expected 'key = value' or an [agent N] section header.");
                }
                if (current == null)
                {
                    throw new FormatException($"Player tactic file line {n + 1}: a key appears before any [agent N] section header.");
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length == 0)
                {
                    throw new FormatException($"Player tactic file line {n + 1}: empty key.");
                }
                if (current.ContainsKey(key))
                {
                    throw new FormatException($"Player tactic file line {n + 1}: duplicate key '{key}' in the same section.");
                }
                current[key] = value;
            }

            PlayerTactic[] byAgent = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                byAgent[i] = BuildTactic(perAgent[i]);
            }
            return new PlayerTacticConfig(byAgent);
        }

        // Selects (and lazily creates) the key/value map for the agent named by the header line. A second
        // header for the same agent is a duplicate-section error (fail loud).
        private static Dictionary<string, string> SelectSection(
            string line, int n, Dictionary<string, string>[] perAgent)
        {
            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                throw new FormatException($"Player tactic file line {n + 1}: malformed section header '{line}'.");
            }
            string name = line.Substring(1, line.Length - 2).Trim();
            if (!name.StartsWith(AgentSectionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException($"Player tactic file line {n + 1}: unknown section '[{name}]' (expected [agent N]).");
            }

            string indexText = name.Substring(AgentSectionPrefix.Length).Trim();
            if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int agentId)
                || agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new FormatException(
                    $"Player tactic file line {n + 1}: agent index '{indexText}' is not in [0, {MatchEngineConstants.SQUAD_SIZE}).");
            }
            if (perAgent[agentId] != null)
            {
                throw new FormatException($"Player tactic file line {n + 1}: duplicate section '[agent {agentId}]'.");
            }
            perAgent[agentId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return perAgent[agentId];
        }

        private static string StripComment(string line)
        {
            int hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }

        // Builds one PlayerTactic from an agent section's key/value map (null map = no section = identity).
        // Each field defaults to the PlayerTactic.Default(PlayerRole.Default) identity value when its key is
        // absent (FR-TI-031), so a partial section is well-defined and an empty/absent section reproduces the
        // identity exactly. Every consumed key is removed; any key left over is unknown and fails loudly.
        private static PlayerTactic BuildTactic(Dictionary<string, string> kv)
        {
            PlayerTactic identity = PlayerTactic.Default(PlayerRole.Default);
            if (kv == null)
            {
                return identity;
            }

            PlayerInstructions di = identity.Instructions;
            var instructions = new PlayerInstructions(
                riskyPasses:        ParseEnum(kv, "riskyPasses",        di.RiskyPasses),
                shootTendency:      ParseEnum(kv, "shootTendency",      di.ShootTendency),
                dribbleTendency:    ParseEnum(kv, "dribbleTendency",    di.DribbleTendency),
                crossTendency:      ParseEnum(kv, "crossTendency",      di.CrossTendency),
                positioningFreedom: ParseEnum(kv, "positioningFreedom", di.PositioningFreedom),
                closeDown:          ParseEnum(kv, "closeDown",          di.CloseDown),
                tightMarking:       Bool(kv, "tightMarking",      di.TightMarking),
                markTargetEntityId: MarkTarget(kv, "markTarget",  di.MarkTargetEntityId),
                setPieceRoles:      ParseEnum(kv, "setPieceRoles",      di.SetPieceRoles));

            var tactic = new PlayerTactic(
                role: ParseEnum(kv, "role", identity.Role),
                duty: ParseEnum(kv, "duty", identity.Duty),
                instructions: instructions);

            if (kv.Count > 0)
            {
                var unknown = new List<string>(kv.Keys);
                throw new FormatException($"Player tactic file: unknown key(s) '{string.Join(", ", unknown)}'.");
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
            throw new FormatException($"Player tactic file: '{raw}' is not a valid {typeof(T).Name} for key '{key}'.");
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
            throw new FormatException($"Player tactic file: '{raw}' is not a valid bool (true/false) for key '{key}'.");
        }

        private static int MarkTarget(Dictionary<string, string> kv, string key, int fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= TacticalInstructionsConstants.MARK_TARGET_NONE)
            {
                return parsed;
            }
            throw new FormatException(
                $"Player tactic file: '{raw}' is not a valid markTarget (−1 = none, ≥0 = entity id) for key '{key}'.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-30 | —      | Initial implementation — Stage-0 text → PlayerTacticConfig parser swap (#21 §3.3). |
#endregion
