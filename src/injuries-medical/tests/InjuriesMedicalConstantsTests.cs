// File:     src/injuries-medical/tests/InjuriesMedicalConstantsTests.cs
// Created:  2026-08-05
// Modified: 2026-08-07
// Author:   —
// Spec:     Injuries & Medical #41 Appendix A + §3.2/§3.4; Code Standards #20
// Purpose:  Catalogue invariants — the per-mille split is well-formed, the tier table covers every
//           severity ordinal, the [FIXED] draw denominator bounds the [GT] risk ceiling, and #29/#41 agree on the
//           risk scale they share.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.InjuriesMedical.Tests
{
    /// <summary>
    /// Catalogue invariants for #41.
    /// <para>
    /// <b>What these guard, precisely:</b> every <c>[GT]</c> read below resolves to its design-time
    /// FALLBACK, because <c>GameplayConfigHolder</c> is never bound in the gate. So they catch a bad
    /// fallback — someone editing <c>RecoveryDaysPerTickBase</c> to 0, or a per-mille split that
    /// leaves Serious unreachable — and they do <b>not</b> catch a config file that sets the same key
    /// to the same bad value at run time. Nothing validates a bound config at Stage 2. Believing
    /// otherwise is what made the original risk-scale equality test vacuous (ERR-041-003), so the
    /// scope is stated here rather than left to be re-derived.
    /// </para>
    /// </summary>
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
        public void DrawDenominator_IsFixed_AndBoundsTheRiskCeiling()
        {
            // ERR-041-011 replaced the old DENOM == InjuryRiskMax identity: the draw is
            // hash % denominator, so a config-tunable denominator re-rolls every career's draws.
            // The denominator is now [FIXED] and the ceiling must sit at or below it — the invariant
            // that keeps every daily probability <= 1, also enforced fail-loud at the draw site.
            Assert.AreEqual(1_000_000, InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM,
                "[FIXED]: changing this value re-rolls every keyed occurrence draw in every career — " +
                "it is not a tuning dial (ERR-041-011).");
            Assert.LessOrEqual(InjuriesMedicalConstants.InjuryRiskMax,
                InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM,
                "the [GT] risk ceiling must never exceed the [FIXED] draw denominator, or a clamped " +
                "risk silently means 'certain and then some'.");
        }

        [Test]
        public void RiskScale_MirrorsTrainingSystem_RatherThanDuplicatingIt()
        {
            // #29 produces InjuryRiskContribution.RiskScore on its own clamped scale and #41 passes it
            // straight through with weight 1, so a divergence would silently rescale every occurrence
            // probability. This assertion is TRUE BY CONSTRUCTION now, and that is the fix: the value
            // is a [CROSS] mirror of #29's, not a second [GT] with its own config key. It was a real
            // equality check before, and a useless one — the gate runs with GameplayConfigHolder
            // unbound, so both sides returned their fallback and it passed whatever a config said.
            // What it guards now is that nobody re-declares the mirror as an independent read.
            Assert.AreEqual(TrainingSystemConstants.InjuryRiskMax, InjuriesMedicalConstants.InjuryRiskMax,
                "#41 §3.4 pins its risk scale to #29's; one owner, one config key (ERR-041-003).");
        }

        [Test]
        public void RecoveryRate_IsPositive_OrEveryInjuryIsPermanent()
        {
            Assert.Greater(InjuriesMedicalConstants.RecoveryDaysPerTickBase, 0,
                "a non-positive per-tick decrement means RecoveryRemaining never falls, so no injury " +
                "ever ends and the career reaches a state nothing can recover from.");
            Assert.Greater(InjuriesMedicalConstants.RecoveryMax, 0);
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
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0).                                   |
// | 1.1     | 2026-08-05 | —      | AR pass 1: the risk-scale check restated for the [CROSS] mirror    |
// |         |            |        | (it was vacuous as an equality of two unbound config reads), plus  |
// |         |            |        | the RecoveryDaysPerTickBase > 0 guard.                             |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): the fixture now states that it pins the design-time |
// |         |            |        | fallbacks, not a bound config — the distinction ERR-041-003 turned |
// |         |            |        | on, and unstated in a fixture whose whole subject is [GT] values.  |
// | 1.3     | 2026-08-07 | —      | Balance pass D3 (ERR-041-011): the DENOM == InjuryRiskMax lock    |
// |         |            |        | becomes the [FIXED]-denominator pin + the ceiling <= denominator  |
// |         |            |        | invariant.                                                        |
#endregion