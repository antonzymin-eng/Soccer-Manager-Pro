// File:     src/positioning-ai/Tests/RotationControllerTests.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Positional Rotations #25 §3.1–§3.4 (T-RO-U-001..008, T-RO-U-012 gates), FR-RO-004..013
// Purpose:  Unit tests for the RotationController: trigger predicate + dwell commit, hold/revert,
//           partner lock, per-tick commit cap, phase-exit freeze, Off identity, and the F2/F5/F6
//           validating restore seams.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class RotationControllerTests
    {
        private const int SquadSize = PositioningAIConstants.SQUAD_SIZE;

        private static readonly FormationSlotRecord[] F = PositioningAIConstants.Family442;

        private static Vector2 Anchor(int slot) =>
            new Vector2(F[slot].LongPct * 105f, F[slot].LateralPct * 68f);

        /// <summary>Anchor-slot array (indexed by slot) used to seed/refresh the target cache.</summary>
        private static Vector2[] AnchorSlots()
        {
            var slots = new Vector2[SquadSize];
            for (int i = 0; i < SquadSize; i++) slots[i] = Anchor(i);
            return slots;
        }

        /// <summary>
        /// Fills a snapshot the way the orchestrator does each heartbeat: agents at array position
        /// = roster index with slotIndex = roster index (the controller applies the binding).
        /// Every agent sits at its anchor except the overrides.
        /// </summary>
        private static PositioningPerceptionSnapshot MakeSnapshot(
            RotationFreedom freedom, params (int agent, Vector2 pos)[] overrides)
        {
            var snap = new PositioningPerceptionSnapshot(SquadSize)
            {
                TickIndex               = 1,
                BallPosition            = new Vector3(52.5f, 34f, 0f),
                PossessionOwnerEntityId = -1,
                RotationFreedom         = freedom,
                ActiveOutfieldCount     = 10,
            };
            for (int i = 0; i < SquadSize; i++)
            {
                Vector2 pos = Anchor(i);
                foreach ((int agent, Vector2 p) in overrides)
                {
                    if (agent == i) pos = p;
                }
                snap.Agents[i] = new AgentPositioningData(
                    i, i, pos, isActive: true, F[i].Role, F[i].IsGoalkeeper);
            }
            return snap;
        }

        /// <summary>One controller heartbeat: fresh snapshot fill → Run → cache write-back (the tick shape).</summary>
        private static PositioningPerceptionSnapshot Step(
            RotationController rot, RotationFreedom freedom, Phase phase,
            params (int agent, Vector2 pos)[] overrides)
        {
            PositioningPerceptionSnapshot snap = MakeSnapshot(freedom, overrides);
            rot.Run(snap, phase, F);
            rot.WriteBackComposedTargets(AnchorSlots());
            return snap;
        }

        private static RotationController MakeSeeded()
        {
            var rot = new RotationController(FormationFamily.F442, SquadSize);
            rot.WriteBackComposedTargets(AnchorSlots());   // boot cache = initial compose (anchors)
            return rot;
        }

        // F442 row 0 = LB(1) ↔ LM(5): the crossed geometry that satisfies FM-RO-01 on both sides.
        private static (int, Vector2)[] CrossedRow0 => new[]
        {
            (1, Anchor(5)),   // LB standing at LM's target
            (5, Anchor(1)),   // LM standing at LB's target
        };

        // ── FM-RO-01/02: trigger dwell and commit ─────────────────────────────

        [Test]
        public void Trigger_CommitsOnFifthConsecutiveHeartbeat_AtomicSwap()
        {
            RotationController rot = MakeSeeded();

            for (int n = 1; n <= 4; n++)
            {
                Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
                Assert.AreEqual(n, rot.GetPairState(0).TriggerDwellTicks, $"dwell after run {n}");
                Assert.IsFalse(rot.GetPairState(0).Rotated, $"no commit before ROTATION_TRIGGER_DWELL_TICKS (run {n})");
                Assert.AreEqual(1, rot.GetSlotOfAgent(1), "binding unchanged pre-commit");
            }

            PositioningPerceptionSnapshot snap = Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);

            Assert.IsTrue(rot.GetPairState(0).Rotated, "commit on the 5th consecutive heartbeat (FR-RO-005)");
            Assert.AreEqual(PositioningAIConstants.ROTATION_HOLD_TICKS,
                rot.GetPairState(0).HoldTicksRemaining, "hold armed at commit");
            Assert.AreEqual(0, rot.GetPairState(0).TriggerDwellTicks, "dwell reset at commit");

            // §3.3 atomic pairwise swap, visible to this tick's downstream consumers (FR-RO-008).
            Assert.AreEqual(5, rot.GetSlotOfAgent(1), "LB agent bound to LM slot");
            Assert.AreEqual(1, rot.GetSlotOfAgent(5), "LM agent bound to LB slot");
            Assert.AreEqual(5, snap.Agents[1].SlotIndex, "snapshot row rebound (agent 1 → slot 5)");
            Assert.AreEqual(F[5].Role, snap.Agents[1].Role, "role re-derived from the bound slot");
            Assert.AreEqual(1, snap.Agents[5].SlotIndex, "snapshot row rebound (agent 5 → slot 1)");
            Assert.AreEqual(0, snap.Agents[0].SlotIndex, "GK binding always identity (FR-RO-003)");
        }

        [Test]
        public void Trigger_DwellResetsOnAnyMiss()
        {
            RotationController rot = MakeSeeded();

            Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Assert.AreEqual(2, rot.GetPairState(0).TriggerDwellTicks);

            // Agents back at their own targets — predicate false ⇒ dwell resets (FR-RO-005).
            Step(rot, RotationFreedom.Free, Phase.InPoss);
            Assert.AreEqual(0, rot.GetPairState(0).TriggerDwellTicks, "any miss resets the dwell");
        }

        [Test]
        public void ConservativeDial_RaisesTheAdvantageBar()
        {
            // Geometry with per-side gain ≈ 5.0 m: above the Free bar (4.0), below the
            // Conservative bar (6.0). Place both agents just off the other's target so the
            // gain = |p−tOwn| − |p−tOther| lands between the two bars.
            Vector2 a1 = Anchor(1);
            Vector2 a5 = Anchor(5);
            Vector2 dir = (a5 - a1).normalized;
            // Agent 1 sits 1.0 m short of LM's target along the line: gain ≈ 31.7 − 1.0 − ... — too
            // big; instead park each agent at a point whose distances differ by exactly ~5 m:
            // 5.0 m from the other's target and 10.0 m from its own along the same line is not
            // available on the segment (|a5−a1| ≈ 31.7), so use points near the midpoint:
            // p = other + dir × d gives |p−own| − |p−other| = (L−d) − d = L − 2d; choose d so
            // L − 2d = 5 ⇒ d = (L−5)/2.
            float L = Vector2.Distance(a1, a5);
            float d = (L - 5f) * 0.5f;
            Vector2 p1 = a5 - dir * d;   // agent 1: 5 m net gain toward LM's target
            Vector2 p5 = a1 + dir * d;   // agent 5: symmetric

            var overrides = new[] { (1, p1), (5, p5) };

            RotationController free = MakeSeeded();
            for (int n = 0; n < PositioningAIConstants.ROTATION_TRIGGER_DWELL_TICKS; n++)
            {
                Step(free, RotationFreedom.Free, Phase.InPoss, overrides);
            }
            Assert.IsTrue(free.GetPairState(0).Rotated, "gain 5.0 ≥ Free advantage 4.0 ⇒ commits");

            RotationController cons = MakeSeeded();
            for (int n = 0; n < PositioningAIConstants.ROTATION_TRIGGER_DWELL_TICKS; n++)
            {
                Step(cons, RotationFreedom.Conservative, Phase.InPoss, overrides);
            }
            Assert.IsFalse(cons.GetPairState(0).Rotated,
                "gain 5.0 < Conservative advantage 6.0 ⇒ never triggers (FR-RO-011 scaling)");
            Assert.AreEqual(0, cons.GetPairState(0).TriggerDwellTicks);
        }

        // ── FM-RO-02: hold and revert ─────────────────────────────────────────

        [Test]
        public void Revert_BlockedByHold_ThenCommitsAfterMirroredDwell()
        {
            RotationController rot = MakeSeeded();

            // Commit at run 5.
            for (int n = 1; n <= 5; n++) Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Assert.IsTrue(rot.GetPairState(0).Rotated);

            // Positions flip back to anchors ⇒ the mirrored predicate is TRUE from run 6 on
            // (each agent now stands at the OTHER slot's target under the swapped binding).
            // Hold = 30: runs 6..34 only decrement; run 35 reaches 0 and starts the revert dwell.
            for (int n = 6; n <= 34; n++)
            {
                Step(rot, RotationFreedom.Free, Phase.InPoss);
                Assert.IsTrue(rot.GetPairState(0).Rotated,
                    $"no revert during the hold (run {n}, FR-RO-006)");
                Assert.AreEqual(0, rot.GetPairState(0).TriggerDwellTicks,
                    $"revert dwell not evaluable during the hold (run {n})");
            }

            // Runs 35–38: hold exhausted, revert dwell 1..4.
            for (int n = 35; n <= 38; n++)
            {
                Step(rot, RotationFreedom.Free, Phase.InPoss);
                Assert.IsTrue(rot.GetPairState(0).Rotated, $"revert dwell still accumulating (run {n})");
                Assert.AreEqual(n - 34, rot.GetPairState(0).TriggerDwellTicks);
            }

            // Run 39: revert commits — bindings back to identity.
            PositioningPerceptionSnapshot snap = Step(rot, RotationFreedom.Free, Phase.InPoss);
            Assert.IsFalse(rot.GetPairState(0).Rotated, "revert commits after the mirrored dwell");
            Assert.AreEqual(1, rot.GetSlotOfAgent(1));
            Assert.AreEqual(5, rot.GetSlotOfAgent(5));
            Assert.AreEqual(1, snap.Agents[1].SlotIndex, "snapshot row restored to identity");
        }

        // ── §3.3 partner lock + FR-RO-009 commit cap ──────────────────────────

        [Test]
        public void PartnerLock_RowSharingSlotWithRotatedRow_IsSkippedWithDwellReset()
        {
            RotationController rot = MakeSeeded();

            // Commit row 0 (LB1 ↔ LM5).
            for (int n = 1; n <= 5; n++) Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Assert.IsTrue(rot.GetPairState(0).Rotated);

            // Row 3 is LM(5) ↔ ST1(9) — it shares slot 5 with the rotated row 0. Give it
            // trigger-favourable geometry (slot 5's occupant is now agent 1; cross agent 1 and
            // agent 9 relative to their caches) and verify it never accumulates dwell.
            var row3Favourable = new[]
            {
                (1, Anchor(9)),   // slot 5's occupant (agent 1 post-swap) standing at ST1's target
                (9, Anchor(5)),   // ST1's occupant standing at the LM target — both gains large
            };
            for (int n = 0; n < 8; n++)
            {
                Step(rot, RotationFreedom.Free, Phase.InPoss, row3Favourable);
                Assert.AreEqual(0, rot.GetPairState(3).TriggerDwellTicks,
                    "a row sharing a slot with a committed rotation is skipped, dwell reset (§3.3)");
                Assert.IsFalse(rot.GetPairState(3).Rotated);
            }
        }

        [Test]
        public void CommitCap_OneRotationPerTeamPerHeartbeat()
        {
            RotationController rot = MakeSeeded();

            // Rows 0 (LB1↔LM5) and 1 (RB4↔RM8) are slot-disjoint; make both trigger-ready.
            var bothCrossed = new[]
            {
                (1, Anchor(5)), (5, Anchor(1)),
                (4, Anchor(8)), (8, Anchor(4)),
            };

            for (int n = 1; n <= 4; n++) Step(rot, RotationFreedom.Free, Phase.InPoss, bothCrossed);

            // Run 5: both reach the dwell threshold; only row 0 (lower table row = higher
            // priority) commits under ROTATION_MAX_PER_TICK = 1 (FR-RO-009).
            Step(rot, RotationFreedom.Free, Phase.InPoss, bothCrossed);
            Assert.IsTrue(rot.GetPairState(0).Rotated, "row 0 commits first (table-row priority)");
            Assert.IsFalse(rot.GetPairState(1).Rotated, "row 1 deferred by the per-tick cap");
            Assert.GreaterOrEqual(rot.GetPairState(1).TriggerDwellTicks,
                PositioningAIConstants.ROTATION_TRIGGER_DWELL_TICKS,
                "deferred row's dwell legitimately exceeds the threshold (F6 note)");

            // Run 6: the deferred row commits.
            Step(rot, RotationFreedom.Free, Phase.InPoss, bothCrossed);
            Assert.IsTrue(rot.GetPairState(1).Rotated, "deferred row commits on the next heartbeat");
        }

        // ── §3.4 phase-exit freeze ────────────────────────────────────────────

        [Test]
        public void PhaseExit_FreezesDwell_ContinuesHold()
        {
            RotationController rot = MakeSeeded();

            // Accumulate 3 dwell in phase.
            for (int n = 1; n <= 3; n++) Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Assert.AreEqual(3, rot.GetPairState(0).TriggerDwellTicks);

            // Out of phase: dwell FROZEN (not reset), no evaluation (FR-RO-010).
            for (int n = 0; n < 5; n++) Step(rot, RotationFreedom.Free, Phase.OutOfPoss, CrossedRow0);
            Assert.AreEqual(3, rot.GetPairState(0).TriggerDwellTicks, "dwell frozen out of phase");
            Assert.IsFalse(rot.GetPairState(0).Rotated);

            // Back in phase: dwell resumes 4, 5 ⇒ commit.
            Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Step(rot, RotationFreedom.Free, Phase.InPoss, CrossedRow0);
            Assert.IsTrue(rot.GetPairState(0).Rotated, "dwell resumes after phase re-entry");

            // Committed rotation persists out of phase; the hold countdown continues.
            int holdBefore = rot.GetPairState(0).HoldTicksRemaining;
            Step(rot, RotationFreedom.Free, Phase.TransToDef, CrossedRow0);
            Assert.IsTrue(rot.GetPairState(0).Rotated, "committed rotation persists on phase exit");
            Assert.AreEqual(holdBefore - 1, rot.GetPairState(0).HoldTicksRemaining,
                "hold countdown continues out of phase (§3.4)");
        }

        // ── FR-RO-011: Off identity + F5 dial gate ────────────────────────────

        [Test]
        public void OffDial_NoEvaluation_SnapshotUntouched()
        {
            RotationController rot = MakeSeeded();

            for (int n = 0; n < 10; n++)
            {
                PositioningPerceptionSnapshot snap = Step(rot, RotationFreedom.Off, Phase.InPoss, CrossedRow0);
                for (int i = 0; i < SquadSize; i++)
                {
                    Assert.AreEqual(i, snap.Agents[i].SlotIndex, $"identity binding untouched (agent {i})");
                }
            }
            Assert.AreEqual(0, rot.GetPairState(0).TriggerDwellTicks, "zero evaluations at Off (FR-RO-011)");
            Assert.IsFalse(rot.GetPairState(0).Rotated);
        }

        [Test]
        public void UndefinedDialByte_RefusedAtTheSeam()
        {
            RotationController rot = MakeSeeded();
            PositioningPerceptionSnapshot snap = MakeSnapshot((RotationFreedom)99);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => rot.Run(snap, Phase.InPoss, F),
                "an undefined RotationFreedom byte must be refused, never clamped (F5)");
        }

        // ── FR-RO-013 validating restore seams (F2/F6) ────────────────────────

        [Test]
        public void RestoreBinding_RefusesNonPermutationAndGkRebind()
        {
            RotationController rot = MakeSeeded();

            int[] duplicate = { 0, 5, 2, 3, 4, 5, 6, 7, 8, 9, 10 };   // slot 5 twice, slot 1 missing
            Assert.Throws<ArgumentException>(() => rot.RestoreBinding(duplicate), "non-permutation (F2)");

            int[] gkMoved = { 1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.Throws<ArgumentException>(() => rot.RestoreBinding(gkMoved), "GK rebind (FR-RO-003/F2)");

            int[] wrongLength = { 0, 1, 2 };
            Assert.Throws<ArgumentException>(() => rot.RestoreBinding(wrongLength), "length mismatch (F2)");

            // A legal permutation (row 0 swapped) round-trips through the accessors.
            int[] swapped = { 0, 5, 2, 3, 4, 1, 6, 7, 8, 9, 10 };
            rot.RestoreBinding(swapped);
            Assert.AreEqual(5, rot.GetSlotOfAgent(1));
            Assert.AreEqual(1, rot.GetSlotOfAgent(5));
        }

        [Test]
        public void RestorePairState_RefusesIncoherentState()
        {
            RotationController rot = MakeSeeded();

            Assert.Throws<ArgumentException>(() => rot.RestorePairState(0,
                new RotationPairState { TriggerDwellTicks = -1 }), "negative dwell (F6)");

            Assert.Throws<ArgumentException>(() => rot.RestorePairState(0,
                new RotationPairState { Rotated = true, HoldTicksRemaining = PositioningAIConstants.ROTATION_HOLD_TICKS + 1 }),
                "hold above ROTATION_HOLD_TICKS (F6)");

            Assert.Throws<ArgumentException>(() => rot.RestorePairState(0,
                new RotationPairState { Rotated = false, HoldTicksRemaining = 3 }),
                "positive hold while not rotated (F6)");

            Assert.Throws<ArgumentOutOfRangeException>(() => rot.RestorePairState(99,
                default), "row out of table range (F6)");

            // Legal state round-trips; dwell above the trigger threshold is legal (commit-cap deferral).
            var legal = new RotationPairState { TriggerDwellTicks = 7, Rotated = true, HoldTicksRemaining = 12 };
            rot.RestorePairState(2, in legal);
            Assert.AreEqual(7,  rot.GetPairState(2).TriggerDwellTicks);
            Assert.IsTrue(rot.GetPairState(2).Rotated);
            Assert.AreEqual(12, rot.GetPairState(2).HoldTicksRemaining);
        }

        [Test]
        public void RestoreLastComposedTarget_RefusesNonFinite_LoadsVerbatim()
        {
            RotationController rot = MakeSeeded();

            Assert.Throws<ArgumentException>(() =>
                rot.RestoreLastComposedTarget(3, new Vector2(float.NaN, 5f)), "NaN target (F6)");
            Assert.Throws<ArgumentException>(() =>
                rot.RestoreLastComposedTarget(3, new Vector2(float.PositiveInfinity, 5f)), "Inf target (F6)");
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                rot.RestoreLastComposedTarget(99, Vector2.zero), "roster index out of range");

            var target = new Vector2(12.25f, 33.5f);
            rot.RestoreLastComposedTarget(3, target);
            Assert.AreEqual(target, rot.GetLastComposedTarget(3),
                "the cache restores VERBATIM (FR-RO-013 — no re-seed)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-11 | —      | Initial implementation: FM-RO-01/02 trigger/dwell/commit/revert, |
// |         |            |        |   partner lock, commit cap, phase freeze, Off identity, F2/F5/F6 |
// |         |            |        |   validating seams.                                              |
#endregion
