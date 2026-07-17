// File:     src/player-database/SquadFileLoader.cs
// Created:  2026-07-15
// Modified: 2026-07-16 (AR-3 fix pass: age range-checked to [AgeMin, AgeMax] (M-2); index-gap identity-fill behaviour documented, stale "contiguous" claim dropped (L-5))
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3, KD-8
// Purpose:  The Stage-0 human-authoring text -> Squad parser (mirrors TeamTacticFileLoader /
//           PlayerTacticFileLoader). NOT a determinism-pinned wire format — only the resulting
//           PlayerRecord values matter, never the grammar (KD-8).

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Parses the Stage-0 squad text format into a <see cref="Squad"/>.
    /// </summary>
    /// <remarks>
    /// FORMAT: a line-oriented, case-insensitive <c>key = value</c> grammar under <c>[player N]</c>
    /// section headers (N = roster index, 0-based; the squad length is the highest authored index
    /// + 1, and an index gap parses as a full-identity player — the per-section analogue of the
    /// omitted-key rule below, AR-3 L-5: the earlier "contiguous" wording contradicted this
    /// deliberate, test-locked behaviour). <c>#</c> comments and blank lines are ignored. Keys:
    /// <c>firstName</c>/<c>lastName</c> (string), <c>age</c> (int
    /// [<see cref="PlayerDatabaseConstants.AgeMin"/>, <see cref="PlayerDatabaseConstants.AgeMax"/>]),
    /// <c>position</c> (enum member name), <c>weakFoot</c> (int [1,5]), and one key per <see cref="AttrIdx"/> field
    /// name (int [1,20], e.g. <c>pace = 14</c>). An omitted key inherits the mid-range identity
    /// (<see cref="PlayerRecord.CreateDefault"/> field value); an omitted/empty file parses to an
    /// all-identity squad of <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/> players. Unknown
    /// key/section, duplicate key/section, unparsable value, and out-of-range int all throw
    /// <see cref="FormatException"/> (fail loud, never silent fallback) — same contract as
    /// <c>TeamTacticFileLoader</c>.
    /// </remarks>
    public static class SquadFileLoader
    {
        private static readonly Dictionary<string, int> s_attrKeyToIdx = BuildAttrKeyMap();

        /// <summary>Parses <paramref name="text"/> into a <see cref="Squad"/> for <paramref name="clubId"/>.</summary>
        /// <param name="text">The squad-file contents. <c>null</c> is treated as empty (all identity).</param>
        /// <exception cref="FormatException">A malformed line, unknown key/section, duplicate, or out-of-range value.</exception>
        public static Squad Parse(string text, int clubId)
        {
            var sections = new Dictionary<int, Dictionary<string, string>>();
            Dictionary<string, string> current = null;
            int maxIndex = -1;

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
                    int idx = ParsePlayerIndex(line, n);
                    if (sections.ContainsKey(idx))
                    {
                        throw new FormatException($"Squad file line {n + 1}: duplicate section '[player {idx}]'.");
                    }
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[idx] = current;
                    if (idx > maxIndex)
                    {
                        maxIndex = idx;
                    }
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    throw new FormatException($"Squad file line {n + 1}: expected 'key = value' or a [player N] header.");
                }
                if (current == null)
                {
                    throw new FormatException($"Squad file line {n + 1}: a key appears before any [player N] section header.");
                }
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length == 0)
                {
                    throw new FormatException($"Squad file line {n + 1}: empty key.");
                }
                if (current.ContainsKey(key))
                {
                    throw new FormatException($"Squad file line {n + 1}: duplicate key '{key}' in the same section.");
                }
                current[key] = value;
            }

            int count = maxIndex + 1;
            if (count <= 0)
            {
                count = PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
            }
            if (count > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new FormatException(
                    $"Squad file: highest [player N] index {maxIndex} exceeds CLUB_SQUAD_SIZE-1 ({PlayerDatabaseConstants.CLUB_SQUAD_SIZE - 1}).");
            }

            var players = new PlayerRecord[count];
            for (int i = 0; i < count; i++)
            {
                sections.TryGetValue(i, out Dictionary<string, string> kv);
                players[i] = BuildPlayer(clubId, i, kv);
            }
            return new Squad(clubId, players);
        }

        private static int ParsePlayerIndex(string line, int n)
        {
            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                throw new FormatException($"Squad file line {n + 1}: malformed section header '{line}'.");
            }
            string inner = line.Substring(1, line.Length - 2).Trim();
            const string prefix = "player ";
            if (inner.Length <= prefix.Length || !inner.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException($"Squad file line {n + 1}: unknown section '[{inner}]' (expected [player N]).");
            }
            string numPart = inner.Substring(prefix.Length).Trim();
            if (!int.TryParse(numPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) || idx < 0)
            {
                throw new FormatException($"Squad file line {n + 1}: '[{inner}]' has a non-numeric or negative player index.");
            }
            return idx;
        }

        // Builds one PlayerRecord from a section's key/value map (or an empty/absent section -> full identity).
        // Every consumed key is removed; any key left over is unknown and fails loudly. PlayerId is
        // club-scoped (design doc KD-3) to match RosterGenerator's convention, not the raw localIndex.
        private static PlayerRecord BuildPlayer(int clubId, int localIndex, Dictionary<string, string> kv)
        {
            int playerId = clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + localIndex;
            PlayerRecord d = PlayerRecord.CreateDefault(playerId);
            if (kv == null)
            {
                return d;
            }

            string firstName = StringVal(kv, "firstName", d.FirstName);
            string lastName = StringVal(kv, "lastName", d.LastName);
            // AR-3 M-2: age is range-checked like every other numeric key ([AgeMin, AgeMax] — the
            // same bounds RosterGenerator draws within); it previously accepted any int, silently
            // contradicting this class's own "out-of-range int all throw" contract.
            int age = IntVal(kv, "age", d.Age,
                PlayerDatabaseConstants.AgeMin, PlayerDatabaseConstants.AgeMax);
            var position = EnumVal(kv, "position", d.Position);
            int weakFoot = IntVal(kv, "weakFoot", d.Attributes.WeakFootRating,
                PlayerDatabaseConstants.WEAK_FOOT_MIN, PlayerDatabaseConstants.WEAK_FOOT_MAX);

            int[] attrs = d.Attributes.ToArray();
            foreach (KeyValuePair<string, int> entry in s_attrKeyToIdx)
            {
                attrs[entry.Value] = IntVal(kv, entry.Key, attrs[entry.Value],
                    PlayerDatabaseConstants.ATTRIBUTE_MIN, PlayerDatabaseConstants.ATTRIBUTE_MAX);
            }

            if (kv.Count > 0)
            {
                var unknown = new List<string>(kv.Keys);
                throw new FormatException($"Squad file [player {localIndex}]: unknown key(s) '{string.Join(", ", unknown)}'.");
            }

            var attributes = new PlayerAttributes();
            attributes.FromArray(attrs);
            attributes.WeakFootRating = weakFoot;

            return new PlayerRecord
            {
                PlayerId = d.PlayerId,
                FirstName = firstName,
                LastName = lastName,
                Age = age,
                Position = position,
                Attributes = attributes
            };
        }

        private static string StringVal(Dictionary<string, string> kv, string key, string fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            return raw;
        }

        private static int IntVal(Dictionary<string, string> kv, string key, int fallback, int min, int max)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= min && parsed <= max)
            {
                return parsed;
            }
            throw new FormatException($"Squad file: '{raw}' is not a valid integer in [{min},{max}] for key '{key}'.");
        }

        private static PlayerPosition EnumVal(Dictionary<string, string> kv, string key, PlayerPosition fallback)
        {
            if (!kv.TryGetValue(key, out string raw))
            {
                return fallback;
            }
            kv.Remove(key);
            if (Enum.TryParse(raw, ignoreCase: true, out PlayerPosition parsed) && Enum.IsDefined(typeof(PlayerPosition), parsed))
            {
                return parsed;
            }
            throw new FormatException($"Squad file: '{raw}' is not a valid PlayerPosition for key '{key}'.");
        }

        private static string StripComment(string line)
        {
            int hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }

        private static Dictionary<string, int> BuildAttrKeyMap()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "pace", AttrIdx.Pace }, { "acceleration", AttrIdx.Acceleration }, { "agility", AttrIdx.Agility },
                { "balance", AttrIdx.Balance }, { "strength", AttrIdx.Strength }, { "stamina", AttrIdx.Stamina },
                { "passing", AttrIdx.Passing }, { "technique", AttrIdx.Technique }, { "finishing", AttrIdx.Finishing },
                { "longShots", AttrIdx.LongShots }, { "dribbling", AttrIdx.Dribbling }, { "crossing", AttrIdx.Crossing },
                { "heading", AttrIdx.Heading }, { "decisions", AttrIdx.Decisions }, { "vision", AttrIdx.Vision },
                { "composure", AttrIdx.Composure }, { "anticipation", AttrIdx.Anticipation }, { "workRate", AttrIdx.WorkRate },
                { "aggression", AttrIdx.Aggression }, { "positioning", AttrIdx.Positioning }, { "reflexes", AttrIdx.Reflexes },
                { "handling", AttrIdx.Handling }, { "aerial", AttrIdx.Aerial }, { "oneVsOne", AttrIdx.OneVsOne },
                { "throwing", AttrIdx.Throwing }, { "kicking", AttrIdx.Kicking }, { "tackling", AttrIdx.Tackling },
                { "marking", AttrIdx.Marking }, { "concentration", AttrIdx.Concentration }, { "teamwork", AttrIdx.Teamwork },
                { "firstTouchAbility", AttrIdx.FirstTouchAbility }
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-15 | —      | Code-review pass: BuildPlayer now computes PlayerId from the   |
// |         |            |        | club-scoped formula (clubId * CLUB_SQUAD_SIZE + localIndex) —  |
// |         |            |        | it previously took the raw section-local index from            |
// |         |            |        | PlayerRecord.CreateDefault(localIndex), diverging from         |
// |         |            |        | RosterGenerator's KD-3 convention (caught by a round-trip      |
// |         |            |        | test that would have failed against the bug).                  |
// | 1.2     | 2026-07-16 | —      | AR-3 fix pass. M-2: age was the ONE numeric key with no range  |
// |         |            |        | check (int.MinValue/MaxValue bounds) — contradicting both the  |
// |         |            |        | class remarks ("out-of-range int all throw") and the           |
// |         |            |        | generator's [AgeMin, AgeMax] model; now bounded the same way   |
// |         |            |        | weakFoot and every attribute already were. L-5 (doc): the      |
// |         |            |        | remarks claimed section indices are "contiguous" while the     |
// |         |            |        | parser deliberately identity-fills gaps (test-locked by        |
// |         |            |        | Parse_OmittedSection_IsFullIdentity) — doc now states the      |
// |         |            |        | gap-fill rule instead.                                         |
#endregion
