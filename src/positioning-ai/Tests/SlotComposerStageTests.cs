// File:     src/positioning-ai/Tests/SlotComposerStageTests.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Build-Up #24 §3.2/§4.2 (FM-BU-02, T-BU-U-005/006/007, T-BU-I-001/003); Dismarking #23
//           §3.3/§4.2 (FM-DM-02, T-DM-U-008/010, T-DM-I-001/002); #12 §3.7.1 (ERR-012-007/008)
// Purpose:  Locks the two SlotComposer stage insertions — the #24 build-up overlay (after
//           ContextModifier, before spacing) and the #23 dismark offset (after spacing, before
//           the pitch clamp) — including the shared stage-order contract and the zero-dial
//           identities.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class SlotComposerStageTests
    {
        private const int SquadSize = PositioningAIConstants.SQUAD_SIZE;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PositioningPerceptionSnapshot MakeSnapshot()
        {
            var snap = new PositioningPerceptionSnapshot(SquadSize)
            {
                TickIndex               = 1,
                BallPosition            = new Vector3(52.5f, 34f, 0f),
                BallVxFiltered          = 0f,
                PossessionOwnerEntityId = 6,      // CM1 carries the ball (own team in possession)
                PossessionOwnerIsOwnTeam = true,
                HasTeamPossession = true,         // ERR-012-011 — carrier ⇒ team possession
                TeamPossessionIsOwnTeam = true,
                ActiveOutfieldCount     = 10,
            };
            FormationSlotRecord[] f = PositioningAIConstants.Family442;
            for (int i = 0; i < SquadSize; i++)
            {
                snap.Agents[i] = new AgentPositioningData(
                    entityId:     i,
                    slotIndex:    i,
                    position:     new Vector2(f[i].LongPct * 105f, f[i].LateralPct * 68f),
                    isActive:     true,
                    role:         f[i].Role,
                    isGoalkeeper: f[i].IsGoalkeeper);
            }
            return snap;
        }

        private static Vector2[] Compose(PositioningPerceptionSnapshot snap, Phase phase)
        {
            var hyst = new HysteresisState(SquadSize);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);
            var outSlots  = new Vector2[SquadSize];
            var anchorBuf = new Vector2[SquadSize];
            var entityIds = new int[SquadSize];
            var modifiers = new ContextModifierInputs(0, 0f, 0f);
            SlotComposer.Compose(snap, FormationFamily.F442, modifiers, phase, hyst,
                outSlots, anchorBuf, entityIds);
            return outSlots;
        }

        private static void AssertSlotsEqual(Vector2[] expected, Vector2[] actual, string message)
        {
            for (int i = 0; i < SquadSize; i++)
            {
                Assert.AreEqual(expected[i].x, actual[i].x, 0f, $"{message} (slot {i} x)");
                Assert.AreEqual(expected[i].y, actual[i].y, 0f, $"{message} (slot {i} y)");
            }
        }

        // ── #24 build-up overlay stage ────────────────────────────────────────

        [Test]
        public void BuildUp_BackThreeOwnThird_AppliesExactCatalogueOffsets()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.InPoss);

            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.BackThree;
            snap.BuildUpCommittedZone = BuildUpZone.OwnThird;
            snap.BuildUpSuppressed    = false;
            Vector2[] overlay = Compose(snap, Phase.InPoss);

            // Appendix A.1 (ERR-024-001 keys): LB (slot 1, DEF LH, y < 34 ⇒ toward centre = +6);
            // RB (slot 4, DEF RH, y > 34 ⇒ −6); CM1/CM2 (slots 6/7, MID C) drop (−4, 0).
            // The F442 anchors are well spread, so spacing resolves identically in both runs and
            // the difference is the exact catalogue Δ.
            Assert.AreEqual(-4.0f, overlay[1].x - control[1].x, 1e-4f, "LB Δx");
            Assert.AreEqual(+6.0f, overlay[1].y - control[1].y, 1e-4f, "LB Δy (toward centre from y<34)");
            Assert.AreEqual(-4.0f, overlay[4].x - control[4].x, 1e-4f, "RB Δx");
            Assert.AreEqual(-6.0f, overlay[4].y - control[4].y, 1e-4f, "RB Δy (toward centre from y>34)");
            Assert.AreEqual(-4.0f, overlay[6].x - control[6].x, 1e-4f, "CM1 Δx");
            Assert.AreEqual(0.0f,  overlay[6].y - control[6].y, 1e-4f, "CM1 Δy");
            Assert.AreEqual(-4.0f, overlay[7].x - control[7].x, 1e-4f, "CM2 Δx");

            // GK receives no overlay (FR-BU-009); ST slots have no BackThree row.
            Assert.AreEqual(control[0], overlay[0], "GK slot must be untouched");
            Assert.AreEqual(control[9].x, overlay[9].x, 1e-4f, "ST1 has no BackThree row");

            // Stage-order property (overlay BEFORE spacing, FR-BU-007): the hard-spacing
            // invariant still holds over the overlaid shape.
            for (int i = 0; i < SquadSize; i++)
            {
                for (int j = i + 1; j < SquadSize; j++)
                {
                    float d = Vector2.Distance(overlay[i], overlay[j]);
                    Assert.GreaterOrEqual(d + 1e-3f, PositioningAIConstants.MIN_AGENT_SEPARATION_M,
                        $"spacing must be enforced after the overlay (slots {i},{j})");
                }
            }
        }

        [Test]
        public void BuildUp_NoneDial_IsExactIdentity()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.InPoss);

            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.None;   // the FR-BU-005 identity
            snap.BuildUpCommittedZone = BuildUpZone.OwnThird;
            Vector2[] result = Compose(snap, Phase.InPoss);

            AssertSlotsEqual(control, result, "BuildUpStructure.None must be bitwise identity");
        }

        [Test]
        public void BuildUp_GatedOff_OutOfPhase_Suppressed_FinalThird()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.OutOfPoss);

            // Out of phase (FR-BU-004: InPoss only).
            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.BackThree;
            snap.BuildUpCommittedZone = BuildUpZone.OwnThird;
            AssertSlotsEqual(control, Compose(snap, Phase.OutOfPoss),
                "overlay must not apply out of Phase.InPoss");

            // Suppression window open (FM-BU-03 gate).
            Vector2[] controlIn = Compose(MakeSnapshot(), Phase.InPoss);
            snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.BackThree;
            snap.BuildUpCommittedZone = BuildUpZone.OwnThird;
            snap.BuildUpSuppressed    = true;
            AssertSlotsEqual(controlIn, Compose(snap, Phase.InPoss),
                "overlay must not apply while the post-regain window is open");

            // FinalThird (FR-BU-004: own/middle-third concept).
            snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.BackThree;
            snap.BuildUpCommittedZone = BuildUpZone.FinalThird;
            AssertSlotsEqual(controlIn, Compose(snap, Phase.InPoss),
                "overlay must not apply in the final third");
        }

        // ── #23 dismark offset stage ──────────────────────────────────────────

        [Test]
        public void Dismark_MarkedAgent_NudgedAwayFromMarker_ExactMagnitude()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.InPoss);

            // ST1 (slot 9) marked from +x: marker 1.2 m away ⇒ û = (−1, 0);
            // §3.3 worked example: mag = 2.5 × 0.42 × 1.0 = 1.05 m at Aggressive.
            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.DismarkIntensity  = DismarkIntensity.Aggressive;
            Vector2 agentPos       = snap.Agents[9].Position;
            snap.HasMarker[9]       = true;
            snap.MarkingPressure[9] = 0.42f;
            snap.MarkerPosition[9]  = agentPos + new Vector2(1.2f, 0f);
            Vector2[] result = Compose(snap, Phase.InPoss);

            // The dismark stage runs AFTER spacing (FR-DM-008), so the difference from the control
            // compose is the exact FM-DM-02 offset (the clamp is not binding here).
            Assert.AreEqual(-1.05f, result[9].x - control[9].x, 1e-4f, "dismark Δx (away from +x marker)");
            Assert.AreEqual(0f,     result[9].y - control[9].y, 1e-4f, "dismark Δy");

            // All other slots untouched.
            for (int i = 0; i < SquadSize; i++)
            {
                if (i == 9) continue;
                Assert.AreEqual(control[i], result[i], $"unmarked slot {i} must be untouched");
            }
        }

        [Test]
        public void Dismark_OffDial_IsExactIdentity()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.InPoss);

            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.DismarkIntensity   = DismarkIntensity.Off;   // FR-DM-012 identity
            snap.HasMarker[9]       = true;
            snap.MarkingPressure[9] = 1.0f;
            snap.MarkerPosition[9]  = snap.Agents[9].Position + new Vector2(0.5f, 0f);
            Vector2[] result = Compose(snap, Phase.InPoss);

            AssertSlotsEqual(control, result, "DismarkIntensity.Off must be bitwise identity");
        }

        [Test]
        public void Dismark_BallCarrierAndOutOfPhase_Excluded()
        {
            Vector2[] control = Compose(MakeSnapshot(), Phase.InPoss);

            // FR-DM-007: the carrier (CM1, entity 6, slot 6) is excluded from the offset stage.
            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.DismarkIntensity   = DismarkIntensity.Aggressive;
            snap.HasMarker[6]       = true;
            snap.MarkingPressure[6] = 1.0f;
            snap.MarkerPosition[6]  = snap.Agents[6].Position + new Vector2(1.0f, 0f);
            AssertSlotsEqual(control, Compose(snap, Phase.InPoss),
                "the ball carrier must take no dismark offset (FR-DM-007)");

            // FR-DM-006: no offset out of Phase.InPoss (even with pressure carried).
            Vector2[] controlOut = Compose(MakeSnapshot(), Phase.OutOfPoss);
            snap = MakeSnapshot();
            snap.DismarkIntensity   = DismarkIntensity.Aggressive;
            snap.HasMarker[9]       = true;
            snap.MarkingPressure[9] = 1.0f;
            snap.MarkerPosition[9]  = snap.Agents[9].Position + new Vector2(1.0f, 0f);
            AssertSlotsEqual(controlOut, Compose(snap, Phase.OutOfPoss),
                "the dismark stage must not apply out of Phase.InPoss (FR-DM-006)");
        }

        [Test]
        public void Dismark_AndOverlay_OutputsStayOnPitch()
        {
            // FR-DM-008 / FR-PA-033: the pitch clamp runs LAST — with both stages active every
            // composed slot stays inside the clamp bounds.
            PositioningPerceptionSnapshot snap = MakeSnapshot();
            snap.BuildUpStructure     = BuildUpStructure.InvertedFullBacks;
            snap.BuildUpCommittedZone = BuildUpZone.MiddleThird;
            snap.DismarkIntensity     = DismarkIntensity.Aggressive;
            for (int i = 1; i < SquadSize; i++)
            {
                snap.HasMarker[i]       = true;
                snap.MarkingPressure[i] = 1.0f;
                snap.MarkerPosition[i]  = snap.Agents[i].Position + new Vector2(0.3f, 0.3f);
            }
            Vector2[] result = Compose(snap, Phase.InPoss);

            for (int i = 0; i < SquadSize; i++)
            {
                Assert.GreaterOrEqual(result[i].x, PositioningAIConstants.SLOT_X_MIN, $"slot {i} x ≥ min");
                Assert.LessOrEqual   (result[i].x, PositioningAIConstants.SLOT_X_MAX, $"slot {i} x ≤ max");
                Assert.GreaterOrEqual(result[i].y, PositioningAIConstants.SLOT_Y_MIN, $"slot {i} y ≥ min");
                Assert.LessOrEqual   (result[i].y, PositioningAIConstants.SLOT_Y_MAX, $"slot {i} y ≤ max");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-11 | —      | Initial implementation: #24 overlay (exact catalogue Δ, None     |
// |         |            |        |   identity, phase/suppression/final-third gates, post-overlay    |
// |         |            |        |   spacing invariant) + #23 dismark (exact FM-DM-02 offset, Off   |
// |         |            |        |   identity, carrier/phase exclusion, clamp bounds).              |
#endregion
