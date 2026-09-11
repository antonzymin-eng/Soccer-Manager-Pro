// ============================================================================
// File:     src/club-finances/tests/ClubFinancesCritiqueTests.cs
// Created:  2026-09-06
// Modified: 2026-09-11 (#40 T3a — daily revenue accrual primitive locks)
// Author:   —
// Specs:    Club Finances & Economy #40 §5/§7.1; Code Standards #20
// Purpose:  Locks the current T3a dependency boundary, RNG-free save shape, upper
//           budget clamp, non-positive board-modifier failure, overflow-safe
//           board scaling, decode corruption guards, and daily revenue accrual semantics.
// §3.9.4 general-unit-test — allocation rules relaxed in test body
// ============================================================================

using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.ClubFinances.Tests
{
    /// <summary>Regression locks added by the PR #363 critique/revision pass and advanced through T3a.</summary>
    [TestFixture]
    public sealed class ClubFinancesCritiqueTests
    {
        /// <summary>T-FN-BOUND-002: the current T3a production assembly carries only dependencies it consumes.</summary>
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
            StringAssert.Contains("TacticalDirector.PlayerDatabase", references,
                "T2a's Squad.ClubId bootstrap consumer requires the planned #27 dependency");
            StringAssert.DoesNotContain("TacticalDirector.SeasonSave", references,
                "#30 composes #40; #40 must never reverse that ownership edge");

            int productionReferenceCount = references.Split(
                new[] { "TacticalDirector." },
                StringSplitOptions.None).Length - 1;
            Assert.That(productionReferenceCount, Is.EqualTo(3),
                "T3a may reference only DeterministicSim, ProjectConstants and PlayerDatabase; this also excludes #30/#31/#34/#45");
        }

        /// <summary>T-FN-DET-004: T3a remains draw-free; no cursor/action ordinal or promoted #40 RNG tag exists yet.</summary>
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

        /// <summary>Proves board scaling saturates before overflow while preserving exact integer-floor semantics below the cap.</summary>
        [Test]
        public void ScaleAndClampBudget_Int32ExtremeBase_DoesNotOverflowBeforeClamp()
        {
            MethodInfo method = typeof(FinanceStep).GetMethod(
                "ScaleAndClampBudget",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            const long int32ExtremeBaseCeiling = 4_611_688_161_616_067L;
            long saturated = (long)method.Invoke(null, new object[] { int32ExtremeBaseCeiling, 2_000 });
            Assert.That(saturated, Is.EqualTo(ClubFinancesConstants.ClubFinancesBudgetCeilingMax));

            long exact = (long)method.Invoke(null, new object[] { 1_234_567L, 1_250 });
            Assert.That(exact, Is.EqualTo(1_543_208L));
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

        /// <summary>T3a identity: the deep gate off returns the complete finance record field-identically.</summary>
        [Test]
        public void AccrueDailyRevenue_DeepOff_IsExactIdentity()
        {
            ClubFinances prior = new ClubFinances
            {
                Balance = 1_000,
                TransferBudget = 200,
                WageBudget = 300,
                WageBillAggregate = 400,
                SeasonRevenueAccrued = 500,
                FfpBalanceWindow = -600
            };

            ClubFinances result = FinanceStep.AccrueDailyRevenue(in prior, 70, 80, false);

            Assert.That(result.Balance, Is.EqualTo(prior.Balance));
            Assert.That(result.TransferBudget, Is.EqualTo(prior.TransferBudget));
            Assert.That(result.WageBudget, Is.EqualTo(prior.WageBudget));
            Assert.That(result.WageBillAggregate, Is.EqualTo(prior.WageBillAggregate));
            Assert.That(result.SeasonRevenueAccrued, Is.EqualTo(prior.SeasonRevenueAccrued));
            Assert.That(result.FfpBalanceWindow, Is.EqualTo(prior.FfpBalanceWindow));
        }

        /// <summary>T3a accounting: both daily revenue sources accrue once, while unrelated state is untouched.</summary>
        [Test]
        public void AccrueDailyRevenue_Enabled_AccruesBothComponentsOnly()
        {
            ClubFinances prior = new ClubFinances
            {
                Balance = 1_000,
                TransferBudget = 200,
                WageBudget = 300,
                WageBillAggregate = 400,
                SeasonRevenueAccrued = 75,
                FfpBalanceWindow = -25
            };

            ClubFinances result = FinanceStep.AccrueDailyRevenue(in prior, 120, 380, true);

            Assert.That(result.Balance, Is.EqualTo(1_500));
            Assert.That(result.SeasonRevenueAccrued, Is.EqualTo(575));
            Assert.That(result.TransferBudget, Is.EqualTo(prior.TransferBudget));
            Assert.That(result.WageBudget, Is.EqualTo(prior.WageBudget));
            Assert.That(result.WageBillAggregate, Is.EqualTo(prior.WageBillAggregate));
            Assert.That(result.FfpBalanceWindow, Is.EqualTo(prior.FfpBalanceWindow));
        }

        /// <summary>T3a refuses negative revenue components instead of silently turning revenue into expenditure.</summary>
        [TestCase(-1L, 0L)]
        [TestCase(0L, -1L)]
        public void AccrueDailyRevenue_EnabledNegativeComponent_FailsLoud(long sponsorship, long matchday)
        {
            ClubFinances prior = ClubFinances.CreateInitial(1_000L);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceStep.AccrueDailyRevenue(in prior, sponsorship, matchday, true));
        }

        /// <summary>T3a performs checked arithmetic before returning, so an overflow cannot wrap club cash.</summary>
        [Test]
        public void AccrueDailyRevenue_BalanceOverflow_FailsLoud()
        {
            ClubFinances prior = ClubFinances.CreateInitial(long.MaxValue);

            Assert.Throws<OverflowException>(
                () => FinanceStep.AccrueDailyRevenue(in prior, 1, 0, true));
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
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-06 | —      | Initial external-review regression locks for PR #363. |
// | 1.1     | 2026-09-07 | —      | Follow-up: negative board fails loud; header/template and asmdef rationale corrected. |
// | 1.2     | 2026-09-07 | —      | Locks overflow-safe board scaling at the documented Int32 tuning extreme and below-cap floor semantics. |
// | 1.4     | 2026-09-08 | —      | Corrected the version-history table to the required parseable pipe-row format. |
// | 1.5     | 2026-09-11 | —      | T2a: dependency lock now requires consumed PlayerDatabase edge and exactly three production refs. |
// | 1.6     | 2026-09-11 | OpenAI | T3a: lock identity, accrual isolation, negative-input refusal and overflow failure. |
#endregion
