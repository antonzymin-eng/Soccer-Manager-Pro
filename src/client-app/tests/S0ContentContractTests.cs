// File:     src/client-app/tests/S0ContentContractTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §§4.2–4.3, Code Standards #20
// Purpose:  Lock every compiled S0 base pattern to the approved binding-contract register.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class S0ContentContractTests
    {
        private const string CONTRACT_PATH = "docs/design/ux-s0-binding-contracts.md";
        private const string KEY_PREFIX = "ui.s0.";
        private static readonly Regex ArrowEntry = new Regex(@"`([a-z0-9_.]+)` → (.*?)(?=; `|$)");
        private static readonly Regex MentalityRow = new Regex(@"^\| [A-Za-z]+ \| `([a-z_]+)` → (.+?) \| `([a-z_]+)` → (.+?) \|$");
        private static readonly Regex DynamicRow = new Regex(@"^\| `([a-z0-9_.]+)` \|(?: [^|]+ \|)? (.+?) \|$");

        [Test]
        public void EveryCompiledBasePatternMatchesTheApprovedContractVerbatim()
        {
            Dictionary<string, string> contract = ReadContract();
            var mismatches = new List<string>();
            var compiled = new HashSet<string>();
            foreach (S0TextRole role in S0ScreenContent.All)
            {
                string suffix = role.Key.Value.Substring(KEY_PREFIX.Length);
                compiled.Add(suffix);
                if (!contract.TryGetValue(suffix, out string approved))
                    mismatches.Add(suffix + ": not in the approved register");
                else if (!string.Equals(approved, role.BasePattern, StringComparison.Ordinal))
                    mismatches.Add(suffix + ": compiled \"" + role.BasePattern + "\" != approved \"" + approved + "\"");
            }

            foreach (string suffix in contract.Keys)
                if (!compiled.Contains(suffix))
                    mismatches.Add(suffix + ": approved but not compiled");
            Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
            Assert.AreEqual(141, compiled.Count, "The reviewed register has 141 roles.");
        }

        [Test]
        public void ParserReadsSemicolonsInsideApprovedSentences()
        {
            // The truncation defect cut these sentences at an internal semicolon.
            Dictionary<string, string> contract = ReadContract();
            StringAssert.EndsWith("; it does not wait for a stoppage.", contract["context.substitution"]);
            StringAssert.EndsWith("; Away A attacks left. Goals, penalty areas and ball shown.", contract["pitch.description"]);
            StringAssert.EndsWith("; the two teams need not total 100%.", contract["statistics.loose_ball"]);
            Assert.AreEqual("Least risk in on-ball choices; deepest defensive line.", contract["effect.very_defensive"]);
        }

        private static Dictionary<string, string> ReadContract()
        {
            string[] lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), CONTRACT_PATH));
            var roles = new Dictionary<string, string>(StringComparer.Ordinal);
            string section = null;
            foreach (string line in lines)
            {
                if (line.StartsWith("### ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
                    section = line;
                if (section == null || !line.StartsWith("| ", StringComparison.Ordinal))
                    continue;
                if (section.StartsWith("### 4.2", StringComparison.Ordinal))
                {
                    Match mentality = MentalityRow.Match(line);
                    if (mentality.Success)
                    {
                        Add(roles, "mentality." + mentality.Groups[1].Value, mentality.Groups[2].Value);
                        Add(roles, "effect." + mentality.Groups[3].Value, mentality.Groups[4].Value);
                        continue;
                    }

                    string[] cells = line.Split('|');
                    if (cells.Length < 4)
                        continue;
                    foreach (Match entry in ArrowEntry.Matches(cells[2].Trim()))
                        Add(roles, entry.Groups[1].Value, entry.Groups[2].Value);
                }
                else if (section.StartsWith("### 4.3", StringComparison.Ordinal))
                {
                    Match dynamic = DynamicRow.Match(line);
                    if (dynamic.Success)
                        Add(roles, dynamic.Groups[1].Value, dynamic.Groups[2].Value);
                }
            }

            Assert.That(roles.Count, Is.GreaterThan(0), "No roles parsed from " + CONTRACT_PATH);
            return roles;
        }

        private static void Add(Dictionary<string, string> roles, string suffix, string pattern)
        {
            if (roles.ContainsKey(suffix))
                Assert.Fail("Duplicate approved role " + suffix);
            roles.Add(suffix, pattern);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src"))
                    && File.Exists(Path.Combine(directory.FullName, CONTRACT_PATH)))
                    return directory.FullName;
                directory = directory.Parent;
            }

            Assert.Fail("Could not locate repository root from the test directory.");
            return string.Empty;
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Verbatim 141-role contract parity, including semicolon-bearing sentences that were previously truncated. |
#endregion
