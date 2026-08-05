// File:     src/injuries-medical/tests/InjuriesMedicalConstantsTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 Appendix A + §3.2/§3.4; Code Standards #20
// Purpose:  Catalogue invariants — the per-mille split is well-formed, the tier table covers every
//           severity ordinal, the draw denominator tracks the risk ceiling, and #29/#41 agree on the
//           risk scale they share.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.InjuriesMedical.Tests
{
    [TestFixture]
    public sealed class InjuriesMedicalConstantsTests
    {
        [Test]
        public void SeverityTierTable_CoversEveryDefinedOrdinal()
        {
            Array ordinals = Enum.GetValues(typeof(InjurySeverity));

            Assert.AreEqual(ordinals.Length, InjuriesMedicalConstants.SeverityTierCount);

            foreach (InjurySeverity severity in ordinals)
            {
                Assert.IsTrue(InjuriesMedicalConstants.IsDefinedSeverity(severity));
                Assert.DoesNotThrow(() => InjuriesMedicalConstants.RecoveryDaysFor(severity));
            }

            Assert.AreEqual(0, InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.None),
                "a healthy player has no recovery outstanding — the F1 coherence invariant starts here.");
        }

        [Test]
        public void RecoveryDays_RiseWithSeverity_AndFitUnderTheCeiling()
        {
            int minor = InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.Minor);
            int moderate = InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.Moderate);
            int serious = InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.Serious);

            Assert.Greater(minor, 0);
            Assert.Greater(moderate, minor);
            Assert.Greater(serious, moderate);
            Assert.LessOrEqual(serious, InjuriesMedicalConstants.RecoveryMax,
                "a Stage-2 Serious injury must sit well inside the clamp, or the clamp is silently " +
                "shortening the longest tier.");
        }

        [Test]
        public void UndefinedSeverity_FailsLoud_F4()
        {
            var undefined = (InjurySeverity)200;

            Assert.IsFalse(InjuriesMedicalConstants.IsDefinedSeverity(undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => InjuriesMedicalConstants.RecoveryDaysFor(undefined));
        }

        [Test]
        public void SeverityPermilleSplit_IsWellFormed()
        {
            Assert.Greater(InjuriesMedicalConstants.SeverityMinorPermille, 0);
            Assert.Greater(InjuriesMedicalConstants.SeverityModeratePermille, 0);
            Assert.LessOrEqual(
                InjuriesMedicalConstants.SeverityMinorPermille + InjuriesMedicalConstants.SeverityModeratePermille,
                InjuriesMedicalConstants.SEVERITY_PERMILLE_DENOM,
                "Minor + Moderate must leave room for Serious — a sum over the denominator would make " +
                "the Serious tier unreachable (the Appendix A catalogue invariant).");
        }

        [Test]
        public void DrawDenominator_TracksTheRiskCeiling()
        {
            Assert.AreEqual(InjuriesMedicalConstants.InjuryRiskMax, InjuriesMedicalConstants.OccurrenceDrawDenom,
                "§3.4: the assembled risk is compared directly against the draw, so any gap between " +
                "these two silently rescales every occurrence probability.");
            Assert.Greater(InjuriesMedicalConstants.OccurrenceDrawDenom, 0);
        }

        [Test]
        public void RiskScale_AgreesWithTrainingSystem()
        {
            // #29 produces InjuryRiskContribution.RiskScore on its own clamped scale and #41 passes it
            // straight through with weight 1. If the two ceilings ever diverge, a maxed-out #29 risk
            // stops meaning "certain occurrence" here and nothing else would notice.
            Assert.AreEqual(TrainingSystemConstants.InjuryRiskMax, InjuriesMedicalConstants.InjuryRiskMax,
                "#41 §3.4 pins its risk scale to #29's; the two [GT] rows must hold the same value.");
        }

        [Test]
        public void DomainTag_MirrorsTheDeterministicSimAllocation()
        {
            Assert.AreEqual(DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL,
                            InjuriesMedicalConstants.DomainTagInjuriesMedical,
                "a [CROSS] mirror must not diverge from its source (#16 §3.4, ERR-041-001).");
            Assert.AreEqual(0x2A, (int)InjuriesMedicalConstants.DomainTagInjuriesMedical);
        }

        [Test]
        public void DrawPurposeRadix_LeavesHeadroomAboveEveryDefinedPurpose()
        {
            Assert.Less(InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE, InjuriesMedicalConstants.DRAW_PURPOSE_RADIX);
            Assert.Greater(InjuriesMedicalConstants.DRAW_PURPOSE_RADIX, 1,
                "the radix must exceed every purpose ordinal ever defined, with room for the deep tier " +
                "(FR-MD-008 — shrinking it would collide one day's ordinals with the next day's).");
        }

        [Test]
        public void NotAdvancedSentinel_IsNotADay0Collision()
        {
            Assert.AreEqual(uint.MaxValue, InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL);
        }

        [Test]
        public void RobustnessMitigation_IsMonotoneAndClampedAtBothEnds()
        {
            int previous = -1;
            for (int mean = 0; mean <= InjuriesMedicalConstants.RobustnessMeanMax; mean++)
            {
                int mitigation = InjuriesMedicalConstants.RobustnessMitigationFor(mean);
                Assert.GreaterOrEqual(mitigation, previous, "a more robust player must never be mitigated less.");
                previous = mitigation;
            }

            Assert.AreEqual(InjuriesMedicalConstants.RobustnessMitigationFor(0),
                            InjuriesMedicalConstants.RobustnessMitigationFor(-5));
            Assert.AreEqual(InjuriesMedicalConstants.RobustnessMitigationFor(InjuriesMedicalConstants.RobustnessMeanMax),
                            InjuriesMedicalConstants.RobustnessMitigationFor(999));
        }

        [Test]
        public void RobustnessMitigation_ReproducesTheWorkedExampleExactly()
        {
            Assert.AreEqual(400, InjuriesMedicalConstants.RobustnessMitigationFor(14),
                "§3.6 pins mean robustness 14 ⇒ 400; the table is calibrated so the worked example is " +
                "exact rather than approximately reproduced.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                            |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0). |
#endregion
