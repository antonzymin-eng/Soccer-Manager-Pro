// ============================================================================
// File:     src/club-finances/tests/ClubFinancesCritiqueTests.cs
// Created:  2026-09-06
// Modified: 2026-09-07
// Author:   OpenAI
// Specs:    Club Finances & Economy #40 §5; Code Standards #20
// Purpose:  Locks the T0/T1a dependency boundary, RNG-free save shape, upper
//           budget clamp, non-positive board-modifier failure, and decode
//           corruption guards identified by external review of PR #363.
// §3.9.4 general-unit-test — allocation rules relaxed in test body
// ============================================================================

using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.ClubFinances.Tests
{
    /// <summary>Regression locks added by the PR #363 critique/revision pass.</summary>
    [TestFixture]
    public sealed class ClubFinancesCritiqueTests
    {
        /// <summary>T-FN-BOUND-002: the current T0/T1a production assembly carries only dependencies it consumes.</summary>
        [Test]
        public void AssemblyReferences_AreExactlyCurrentPhaseDependencies()
        {
            DirectoryInfo root = FindRepoRoot();
            Assert.That(root, Is.Not.Null, "could not locate repository root; dependency lock must fail loud");

            // Read the asmdef itself rather than Assembly.GetReferencedAssemblies(): runtime reflection can
            // elide an unused/dead reference, which is exactly the edge this structural lock must detect.
            // The checkout-root dependency is therefore intentional and fails loud outside a repo checkout.
            string path = Path.Combine(root.FullName, "src", "club-finances", "club-finances.asmdef");
            string asmdef = File.ReadAllText(path);
            int referencesStart = asmdef.IndexOf("\"references\"", StringComparison.Ordinal);
            int referencesEnd = asmdef.IndexOf(']', referencesStart);
            Assert.That(referencesStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(referencesEnd, Is.GreaterThan(referencesStart));

            string references = asmdef.Substring(referencesStart, referencesEnd - referencesStart);
            StringAssert.Contains("TacticalDirector.DeterministicSim", references);
            StringAssert.Contains("TacticalDirector.ProjectConstants", references);
            StringAssert.DoesNotContain("TacticalDirector.PlayerDatabase", references,
                "PlayerDatabase is a T2 dependency and must not land before the Squad.ClubId consumer");

            int productionReferenceCount = references.Split(
                new[] { "TacticalDirector." },
                StringSplitOptions.None).Length - 1;
            Assert.That(productionReferenceCount, Is.EqualTo(2),
                "T0/T1a may reference only DeterministicSim and ProjectConstants; this also excludes #30/#31/#34/#45");
        }

        /// <summary>T-FN-DET-004: persisted finance state and production source contain no cursor/draw-order field or promoted #40 RNG tag.</summary>
        [Test]
        public void MinimalFinanceSaveShape_HasNoRngCursorOrActionOrdinal()
        {
            Type[] persistedTypes = { typeof(ClubFinanceEntry), typeof(ClubFinances) };
            foreach (Type type in persistedTypes)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.That(field.Name, Is.Not.EqualTo("RngCursor"));
                    Assert.That(field.Name, Is.Not.EqualTo("actionOrdinal"));
                }
            }

            DirectoryInfo root = FindRepoRoot();
            Assert.That(root, Is.Not.Null, "could not locate repository root; RNG-shape lock must fail loud");
            DirectoryInfo financeDir = new DirectoryInfo(Path.Combine(root.FullName, "src", "club-finances"));
            FileInfo[] productionFiles = financeDir.GetFiles("*.cs", SearchOption.TopDirectoryOnly);
            Assert.That(productionFiles.Length, Is.GreaterThan(0));

            foreach (FileInfo file in productionFiles)
            {
                string source = File.ReadAllText(file.FullName);
                StringAssert.DoesNotContain("RngCursor", source, file.Name);
                StringAssert.DoesNotContain("actionOrdinal", source, file.Name);
                StringAssert.DoesNotContain("DOMAIN_TAG_CLUB_FINANCES", source, file.Name);
                StringAssert.DoesNotContain("SubsystemOrdinals.ClubFinances", source, file.Name);
            }
        }

        /// <summary>Proves the upper sanity clamp executes for both projected budget ceilings.</summary>
        [Test]
        public void SettleFinances_ExtremePositiveBoardModifier_ClampsBothBudgetsToCeiling()
        {
            ClubFinances prior = ClubFinances.CreateInitial(0L);
            BoardModifier modifier = new BoardModifier(200_000);

            ClubFinances result = FinanceStep.SettleFinances(in prior, 1, 20, in modifier);

            Assert.That(result.TransferBudget, Is.EqualTo(ClubFinancesConstants.ClubFinancesBudgetCeilingMax));
            Assert.That(result.WageBudget, Is.EqualTo(ClubFinancesConstants.ClubFinancesBudgetCeilingMax));
        }

        /// <summary>Proves an explicit negative board multiplier is a caller-contract error rather than clamp input.</summary>
        [Test]
        public void SettleFinances_NegativeBoardModifier_FailsLoud()
        {
            ClubFinances prior = ClubFinances.CreateInitial(0L);
            BoardModifier modifier = new BoardModifier(-1_000);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceStep.SettleFinances(in prior, 1, 20, in modifier));
        }

        /// <summary>Proves decode rejects duplicate/non-ascending ClubIds even when the byte length is otherwise valid.</summary>
        [Test]
        public void Decode_RejectsNonAscendingClubIds()
        {
            ClubFinanceEntry[] entries =
            {
                Entry(1),
                Entry(2)
            };
            byte[] blob = ClubFinancesSaveCodec.Encode(entries);
            int secondClubIdOffset = ClubFinancesConstants.FINANCE_SAVE_HEADER_BYTES
                + ClubFinancesConstants.FINANCE_SAVE_RECORD_BYTES;
            WriteI32(blob, secondClubIdOffset, 1);

            Assert.Throws<InvalidOperationException>(() => ClubFinancesSaveCodec.Decode(blob));
        }

        /// <summary>Proves framing rejects blobs shorter than the mandatory magic/version prefix.</summary>
        [TestCase(0)]
        [TestCase(7)]
        public void Decode_RejectsShortHeader(int length)
        {
            Assert.Throws<InvalidOperationException>(() => ClubFinancesSaveCodec.Decode(new byte[length]));
        }

        /// <summary>Proves a declared record that is truncated by one byte fails before partial state can escape.</summary>
        [Test]
        public void Decode_RejectsTruncatedRecord()
        {
            byte[] canonical = ClubFinancesSaveCodec.Encode(new[] { Entry(1) });
            Array.Resize(ref canonical, canonical.Length - 1);

            Assert.Throws<InvalidOperationException>(() => ClubFinancesSaveCodec.Decode(canonical));
        }

        private static ClubFinanceEntry Entry(int clubId)
        {
            ClubFinances finances = ClubFinances.CreateInitial(0L);
            return new ClubFinanceEntry(clubId, in finances);
        }

        private static void WriteI32(byte[] bytes, int offset, int value)
        {
            uint raw = unchecked((uint)value);
            bytes[offset] = (byte)raw;
            bytes[offset + 1] = (byte)(raw >> 8);
            bytes[offset + 2] = (byte)(raw >> 16);
            bytes[offset + 3] = (byte)(raw >> 24);
        }

        private static DirectoryInfo FindRepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "tools")))
                {
                    return dir;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}

#region VersionHistory
// Version | Date       | Author | Change
// --------|------------|--------|----------------------------------------------
// 1.0     | 2026-09-06 | —      | Initial external-review regression locks for PR #363.
// 1.1     | 2026-09-07 | OpenAI | Follow-up: negative board fails loud; header/template and asmdef rationale corrected.
#endregion
