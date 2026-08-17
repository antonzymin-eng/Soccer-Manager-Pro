// File:     src/positioning-ai/Tests/TacticTranslationTests.cs
// Created:  2026-06-29
// Modified: 2026-08-08
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-016 / FR-TI-031; Positioning AI #12 §3.5; Code Standards #20
// Purpose:  Locks the #21 → #12 T2 consumer seam: TacticWidth / TacticDefWidth → lateral-
//           compactness scalar validity + F5 clamp, the Standard identity row, the §3.4 monotone
//           shape, and the ContextModifierInputs Standard-seed behaviour-neutrality contract.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class TacticTranslationTests
    {
        // ── §3.4: each width maps onto its catalogue scalar (direct ordinal lookup) ──

        [Test]
        public void WidthCompactnessScalar_MapsEachWidthToItsCatalogueRow()
        {
            Assert.AreEqual(TacticalInstructionsConstants.WidthScalar[(int)TacticWidth.VeryNarrow],
                            TacticTranslation.WidthCompactnessScalar(TacticWidth.VeryNarrow));
            Assert.AreEqual(TacticalInstructionsConstants.WidthScalar[(int)TacticWidth.Standard],
                            TacticTranslation.WidthCompactnessScalar(TacticWidth.Standard));
            Assert.AreEqual(TacticalInstructionsConstants.WidthScalar[(int)TacticWidth.Wide],
                            TacticTranslation.WidthCompactnessScalar(TacticWidth.Wide));
        }

        [Test]
        public void DefWidthCompactnessScalar_MapsEachWidthToItsCatalogueRow()
        {
            Assert.AreEqual(TacticalInstructionsConstants.DefWidthScalar[(int)TacticDefWidth.Narrow],
                            TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Narrow));
            Assert.AreEqual(TacticalInstructionsConstants.DefWidthScalar[(int)TacticDefWidth.Standard],
                            TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Standard));
            Assert.AreEqual(TacticalInstructionsConstants.DefWidthScalar[(int)TacticDefWidth.Wide],
                            TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Wide));
        }

        // ── §3.4 / FR-TI-031: Standard is the exact identity (×1.0), so the seam is a no-op ──

        [Test]
        public void Standard_IsExactlyOne_BothWidths()
        {
            Assert.AreEqual(1.0f, TacticTranslation.WidthCompactnessScalar(TacticWidth.Standard));
            Assert.AreEqual(1.0f, TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Standard));
        }

        // The 3-arg ContextModifierInputs ctor (pre-#21 surface) must seed Standard so existing
        // callers resolve to the identity scalar — the zero-value enum defaults are VeryNarrow / Narrow.
        [Test]
        public void IdentitySeedingCtor_SeedsStandard_ResolvesToIdentityScalar()
        {
            ContextModifierInputs inputs = new ContextModifierInputs(0, 0f, 0.5f);
            Assert.AreEqual(TacticWidth.Standard, inputs.Width);
            Assert.AreEqual(TacticDefWidth.Standard, inputs.DefensiveWidth);
            Assert.AreEqual(1.0f, TacticTranslation.WidthCompactnessScalar(inputs.Width));
            Assert.AreEqual(1.0f, TacticTranslation.DefWidthCompactnessScalar(inputs.DefensiveWidth));
        }

        // ── §3.4 shape: the scalar is monotone in the width (the reviewable contract) ──

        [Test]
        public void WidthCompactnessScalar_IsMonotoneIncreasing()
        {
            Assert.Less(TacticTranslation.WidthCompactnessScalar(TacticWidth.VeryNarrow),
                        TacticTranslation.WidthCompactnessScalar(TacticWidth.Standard));
            Assert.Less(TacticTranslation.WidthCompactnessScalar(TacticWidth.Standard),
                        TacticTranslation.WidthCompactnessScalar(TacticWidth.Wide));

            Assert.Less(TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Narrow),
                        TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Standard));
            Assert.Less(TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Standard),
                        TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Wide));
        }

        // ── §3.1 F5: a widened (out-of-range) value clamps to the boldest peer ──

        [Test]
        public void WidenedValue_ClampsToBoldestPeer()
        {
            Assert.AreEqual(TacticTranslation.WidthCompactnessScalar(TacticWidth.VeryWide),
                            TacticTranslation.WidthCompactnessScalar((TacticWidth)99));
            Assert.AreEqual(TacticTranslation.DefWidthCompactnessScalar(TacticDefWidth.Wide),
                            TacticTranslation.DefWidthCompactnessScalar((TacticDefWidth)99));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-29 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
