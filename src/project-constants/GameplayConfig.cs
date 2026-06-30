// File:     src/project-constants/GameplayConfig.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019 [GT] config loading), §4.2
// Purpose:  Immutable boot-time key/value store of [GT] design-tuned values loaded from a tunable
//           config source. A [GT] catalogue reads its constants from here at boot (never per frame).

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.ProjectConstants
{
    /// <summary>
    /// Immutable store of <c>[GT]</c> design-tuned values, keyed by <c>(section, key)</c>, that a
    /// constant catalogue reads at boot to satisfy FR-CS-019 (a <c>[GT]</c> constant is loaded from a
    /// tunable config source, not a compile-time literal). Built by <see cref="GameplayConfigFileLoader"/>
    /// from the on-disk text format, or constructed directly in tests / tooling.
    /// </summary>
    /// <remarks>
    /// CONTRACT — an ABSENT key returns the caller's design-time fallback, so a partial config file (or
    /// the <see cref="Empty"/> config a missing file produces) leaves every constant at its baseline —
    /// the config never has to mention a value to keep today's behaviour (parallels the FR-TI-031
    /// identity default). A PRESENT key whose value does not parse to the requested type throws
    /// <see cref="FormatException"/> — a config typo fails loudly at boot, never silently falls back.
    ///
    /// SCOPE — this is boot-time infrastructure: a catalogue reads each constant ONCE into a
    /// <c>public static readonly</c> field at startup; the getters are never called on the 60 Hz path,
    /// so the backing <see cref="Dictionary{TKey,TValue}"/> + string keys do not touch the zero-alloc
    /// budget (FR-CS-066). Instances are immutable and passed by reference (constructor injection) — NOT
    /// a static mutable singleton — so they stay clear of the banned-pattern set (FR-CS-051..054). The
    /// disk grammar is a human-authoring format, NOT a determinism-pinned wire format: only the resulting
    /// VALUES feed a simulation, never the file text.
    /// </remarks>
    public sealed class GameplayConfig
    {
        // Backing map keyed by the composed "section.key", case-insensitive (matching the loader grammar).
        private readonly Dictionary<string, string> _entries;

        /// <summary>An empty config: every getter returns its caller-supplied fallback.</summary>
        public static GameplayConfig Empty { get; } =
            new GameplayConfig(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Builds a config from a map of composed <c>"section.key"</c> → raw value strings. Copies the
        /// input into a case-insensitive store; the source map is not retained.
        /// </summary>
        /// <param name="entries">Composed-key → raw-value pairs. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
        public GameplayConfig(IReadOnlyDictionary<string, string> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries = new Dictionary<string, string>(entries.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> pair in entries)
            {
                _entries[pair.Key] = pair.Value;
            }
        }

        // Private ctor for Empty: takes ownership of an already-case-insensitive dictionary (no copy).
        private GameplayConfig(Dictionary<string, string> ownedEntries)
        {
            _entries = ownedEntries;
        }

        /// <summary>Number of stored entries (diagnostic only).</summary>
        public int Count => _entries.Count;

        /// <summary>True if <paramref name="section"/>/<paramref name="key"/> is present.</summary>
        /// <param name="section">Catalogue section (e.g. the spec folder name). Must not be null/empty.</param>
        /// <param name="key">Constant key within the section. Must not be null/empty.</param>
        public bool Has(string section, string key) => _entries.ContainsKey(Compose(section, key));

        /// <summary>Reads a <c>float</c> <c>[GT]</c> value, or <paramref name="fallback"/> if absent.</summary>
        /// <param name="section">Catalogue section. Must not be null/empty.</param>
        /// <param name="key">Constant key. Must not be null/empty.</param>
        /// <param name="fallback">Design-time default returned when the key is absent.</param>
        /// <exception cref="FormatException">The key is present but its value is not a valid float.</exception>
        public float GetFloat(string section, string key, float fallback)
        {
            if (!_entries.TryGetValue(Compose(section, key), out string raw))
            {
                return fallback;
            }
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }
            throw Malformed(section, key, raw, "float");
        }

        /// <summary>Reads an <c>int</c> <c>[GT]</c> value, or <paramref name="fallback"/> if absent.</summary>
        /// <param name="section">Catalogue section. Must not be null/empty.</param>
        /// <param name="key">Constant key. Must not be null/empty.</param>
        /// <param name="fallback">Design-time default returned when the key is absent.</param>
        /// <exception cref="FormatException">The key is present but its value is not a valid int.</exception>
        public int GetInt(string section, string key, int fallback)
        {
            if (!_entries.TryGetValue(Compose(section, key), out string raw))
            {
                return fallback;
            }
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
            throw Malformed(section, key, raw, "int");
        }

        /// <summary>Reads a <c>bool</c> <c>[GT]</c> value, or <paramref name="fallback"/> if absent.</summary>
        /// <param name="section">Catalogue section. Must not be null/empty.</param>
        /// <param name="key">Constant key. Must not be null/empty.</param>
        /// <param name="fallback">Design-time default returned when the key is absent.</param>
        /// <exception cref="FormatException">The key is present but its value is not <c>true</c>/<c>false</c>.</exception>
        public bool GetBool(string section, string key, bool fallback)
        {
            if (!_entries.TryGetValue(Compose(section, key), out string raw))
            {
                return fallback;
            }
            if (bool.TryParse(raw, out bool value))
            {
                return value;
            }
            throw Malformed(section, key, raw, "bool (true/false)");
        }

        /// <summary>Reads a raw <c>string</c> <c>[GT]</c> value, or <paramref name="fallback"/> if absent.</summary>
        /// <param name="section">Catalogue section. Must not be null/empty.</param>
        /// <param name="key">Constant key. Must not be null/empty.</param>
        /// <param name="fallback">Design-time default returned when the key is absent.</param>
        public string GetString(string section, string key, string fallback)
            => _entries.TryGetValue(Compose(section, key), out string raw) ? raw : fallback;

        // Composes the lookup key. Validates inputs so a caller bug surfaces as an ArgumentException
        // (an empty section/key cannot occur in a loaded config — the loader rejects both).
        private static string Compose(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Config section must be non-empty.", nameof(section));
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Config key must be non-empty.", nameof(key));
            }
            return section + "." + key;
        }

        private static FormatException Malformed(string section, string key, string raw, string type)
            => new FormatException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "GameplayConfig: '{0}' for [{1}] {2} is not a valid {3}.",
                    raw, section, key, type));
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-30 | —      | Initial implementation — FR-CS-019 immutable [GT] config store.             |
#endregion
