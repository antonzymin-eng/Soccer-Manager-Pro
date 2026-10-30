// File:     src/client-app/S0TextFormatter.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §§4.1–4.4, Localization #49 KD-5, Code Standards #20
// Purpose:  Admit all authored patterns before publishing a fixed-context formatter; cache typed role output.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TacticalDirector.Localization;

namespace TacticalDirector.ClientApp
{
    /// <summary>Client formatter over Resolve; no procedural producer identity, draw or locale-change signal.</summary>
    public sealed class S0TextFormatter
    {
        private readonly Dictionary<string, S0TextRole> _roles = new Dictionary<string, S0TextRole>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _patterns = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        private sealed class CacheEntry
        {
            internal string Role;
            internal object[] Arguments;
            internal string Text;
        }

        /// <summary>Validates base and EVERY authored selected pattern before constructing the production Localizer.</summary>
        public S0TextFormatter(TemplateCatalogue.StaticRow[] selectedRows = null, LocaleId? selectedLocale = null) : this(S0ScreenContent.CreateBaseCatalogue(), selectedRows, selectedLocale)
        {
        }

        /// <summary>Admits equivalent punctuation fallbacks before Resolve/hash when a packaged font lacks a glyph.</summary>
        public static S0TextFormatter WithGlyphCoverage(Func<char, bool> glyphAvailable)
        {
            if (glyphAvailable == null)
                throw new ArgumentNullException(nameof(glyphAvailable));
            var rows = new TemplateCatalogue.StaticRow[S0ScreenContent.All.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                S0TextRole role = S0ScreenContent.All[i];
                var pattern = new StringBuilder();
                foreach (char c in role.BasePattern)
                {
                    if (glyphAvailable(c))
                    {
                        pattern.Append(c);
                        continue;
                    }

                    string replacement;
                    switch (c)
                    {
                        case '↔':
                            replacement = "<->";
                            break;
                        case '→':
                            replacement = "->";
                            break;
                        case '←':
                            replacement = "<-";
                            break;
                        case '×':
                            replacement = "x";
                            break;
                        case '–':
                        case '—':
                            replacement = "-";
                            break;
                        case '‘':
                        case '’':
                            replacement = "'";
                            break;
                        case '“':
                        case '”':
                            replacement = "\"";
                            break;
                        case '…':
                            replacement = "...";
                            break;
                        case '•':
                            replacement = "*";
                            break;
                        default:
                            throw new ArgumentException("Font lacks a required S0 character in " + role.Key.Value);
                    }

                    foreach (char fallback in replacement)
                        if (!glyphAvailable(fallback))
                            throw new ArgumentException("Font lacks an S0 fallback character.");
                    pattern.Append(replacement);
                }

                rows[i] = new TemplateCatalogue.StaticRow(role.Key, pattern.ToString());
            }

            return new S0TextFormatter(new TemplateCatalogue(LocaleId.BaseLocale, rows), null, null);
        }

        internal S0TextFormatter(TemplateCatalogue baseCatalogue, TemplateCatalogue.StaticRow[] selectedRows, LocaleId? selectedLocale)
        {
            foreach (S0TextRole role in S0ScreenContent.All)
                _roles.Add(role.Key.Value.Substring("ui.s0.".Length), role);
            TemplateCatalogue selected = null;
            if (selectedRows != null)
            {
                foreach (TemplateCatalogue.StaticRow row in selectedRows)
                {
                    string suffix = row.Key.Value.StartsWith("ui.s0.", StringComparison.Ordinal) ? row.Key.Value.Substring("ui.s0.".Length) : "";
                    if (!_roles.TryGetValue(suffix, out S0TextRole role))
                        throw new ArgumentException("Unknown S0 authored role: " + row.Key.Value);
                    ValidatePattern(role, row.Text);
                }

                selected = new TemplateCatalogue(selectedLocale ?? new LocaleId("qps-ploc"), selectedRows);
            }

            var localizer = new Localizer(baseCatalogue, selected, S0ScreenContent.Coverage());
            var canonical = new StringBuilder();
            var baseOnly = new Localizer(baseCatalogue, null, S0ScreenContent.Coverage());
            foreach (S0TextRole role in S0ScreenContent.All)
            {
                // Validate base even when a translation would hide its defect.
                ValidatePattern(role, baseOnly.Resolve(role.Key));
                string pattern = localizer.Resolve(role.Key);
                ValidatePattern(role, pattern);
                string suffix = role.Key.Value.Substring("ui.s0.".Length);
                _patterns.Add(suffix, pattern);
                canonical.Append(role.Key.Value).Append('|').Append(role.ArgumentTypes).Append('|').Append(pattern.Length).Append(':').Append(pattern).Append('\n');
            }

            using (SHA256 sha = SHA256.Create())
            {
                var hex = new StringBuilder();
                foreach (byte b in sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString())))
                    hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                ContentSha256 = hex.ToString();
            }
        }

        /// <summary>Canonical manifest identity derived from the actual loaded, admitted role patterns.</summary>
        public string ContentSha256 { get; }

        /// <summary>Static label from the lifetime-fixed localizer context.</summary>
        public string Label(string suffix)
        {
            if (!_roles.TryGetValue(suffix, out S0TextRole role) || role.ArgumentTypes.Length != 0)
                throw new ArgumentException("Expected a static S0 label.");
            return _patterns[suffix];
        }

        /// <summary>Reuses an unchanged integer cell before boxing or allocating an argument array.</summary>
        public string Format(string cell, string suffix, int argument)
        {
            if (argument >= 0 && _cache.TryGetValue(cell, out CacheEntry cached) && cached.Role == suffix && cached.Arguments.Length == 1 && cached.Arguments[0] is int value && value == argument)
                return cached.Text;
            return Format(cell, suffix, new object[] { argument });
        }

        /// <summary>Reuses an unchanged finite float cell before boxing or allocating an argument array.</summary>
        public string Format(string cell, string suffix, float argument)
        {
            if (_cache.TryGetValue(cell, out CacheEntry cached) && cached.Role == suffix && cached.Arguments.Length == 1 && cached.Arguments[0] is float value && value == argument)
                return cached.Text;
            return Format(cell, suffix, new object[] { argument });
        }

        /// <summary>Reuses an unchanged string cell before allocating an argument array.</summary>
        public string Format(string cell, string suffix, string argument)
        {
            if (_cache.TryGetValue(cell, out CacheEntry cached) && cached.Role == suffix && cached.Arguments.Length == 1 && cached.Arguments[0] is string value && value == argument)
                return cached.Text;
            return Format(cell, suffix, new object[] { argument });
        }

        /// <summary>Formats validated typed arguments once per changed value set in a named presentation cell.</summary>
        public string Format(string cell, string suffix, params object[] arguments)
        {
            if (!_roles.TryGetValue(suffix, out S0TextRole role))
                throw new ArgumentException("Unknown S0 role.");
            RequireArguments(role, arguments);
            if (_cache.TryGetValue(cell, out CacheEntry cached) && cached.Role == suffix && Same(cached.Arguments, arguments))
                return cached.Text;
            string text = string.Format(CultureInfo.InvariantCulture, _patterns[suffix], arguments);
            _cache[cell] = new CacheEntry
            {
                Role = suffix,
                Arguments = (object[])arguments.Clone(),
                Text = text
            };
            return text;
        }

        /// <summary>Drops match/report cells at teardown; shell labels remain immutable content.</summary>
        public void ClearMatch() => _cache.Clear();
        private static bool Same(object[] a, object[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (!Equals(a[i], b[i]))
                    return false;
            return true;
        }

        private static void RequireArguments(S0TextRole role, object[] arguments)
        {
            if (arguments == null || arguments.Length != role.ArgumentTypes.Length)
                throw new ArgumentException("S0 argument count differs from the role schema.");
            for (int i = 0; i < arguments.Length; i++)
            {
                object value = arguments[i];
                char type = role.ArgumentTypes[i];
                if ((type == 'S' && !(value is string)) || (type == 'I' && (!(value is int n) || n < 0)) || (type == 'F' && (!(value is float f) || float.IsNaN(f) || float.IsInfinity(f))))
                    throw new ArgumentException("S0 argument type/value differs from the role schema.");
            }
        }

        /// <summary>Rejects malformed braces, missing/extra indices, alignment and forbidden type/format combinations.</summary>
        public static void ValidatePattern(S0TextRole role, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Empty S0 pattern: " + role.Key.Value);
            var seen = new bool[role.ArgumentTypes.Length];
            for (int p = 0; p < pattern.Length; p++)
            {
                char c = pattern[p];
                if (c != '{' && c != '}')
                    continue;
                if (p + 1 < pattern.Length && pattern[p + 1] == c)
                {
                    p++;
                    continue;
                }

                if (c == '}')
                    throw new ArgumentException("Unmatched S0 brace: " + role.Key.Value);
                int end = pattern.IndexOf('}', p + 1);
                if (end < 0)
                    throw new ArgumentException("Unmatched S0 brace: " + role.Key.Value);
                string token = pattern.Substring(p + 1, end - p - 1);
                string[] parts = token.Split(':');
                if (parts.Length > 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int index) || index < 0 || index >= seen.Length)
                    throw new ArgumentException("Invalid S0 argument index: " + role.Key.Value);
                string format = parts.Length == 2 ? parts[1] : "";
                char type = role.ArgumentTypes[index];
                if (format.Length != 0 && !((type == 'I' && format == "D") || (type == 'F' && format == "F1")))
                    throw new ArgumentException("Invalid S0 argument format: " + role.Key.Value);
                seen[index] = true;
                p = end;
            }

            foreach (bool present in seen)
                if (!present)
                    throw new ArgumentException("Missing S0 argument index: " + role.Key.Value);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Strict admission, invariant typed formatting, cell caches and loaded manifest. |
#endregion
