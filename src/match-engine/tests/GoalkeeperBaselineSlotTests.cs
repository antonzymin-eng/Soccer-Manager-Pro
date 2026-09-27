// File:     src/match-engine/tests/GoalkeeperBaselineSlotTests.cs
// Created:  2026-09-27
// Modified: 2026-09-27
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1.1 (Recovering → Set), §3.3.0 / KD-13 (Positioning AI #12
//           consumer contract); Positioning AI #12 §3.3.3; Code Standards #20
// Purpose:  Locks the composition-root wiring of #11's baseline slot to the keeper's #12 slot (world
//           space), and that a keeper displaced from that slot stays Recovering until
//           RecoveryCooldownTicks elapse. Before this wiring the slot was the keeper's own position, the
//           at-baseline test was identically true, and every recovery ended on the next tactical pass.
//           Mirrored for both teams.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>#11 §3.3.0 baseline-slot wiring and the recovery cooldown it makes observable.</summary>
    [TestFixture]
    public sealed class GoalkeeperBaselineSlotTests
    {
        private const ulong MatchSeed = 0x0BADF00DDEADBEEFUL;

        // A tactical tick far from kickoff; only differences from it matter.
        private const int RecoveryStartTick = 1000;

        // Lateral displacement of the keeper from its slot: well outside the reactive radius, still inside
        // the goal mouth's neighbourhood and far from the ball, so nothing but recovery can act on it.
        private const float DisplacementM = 6f;

        [TestCase(0)]
        [TestCase(1)]
        public void BaselineSlot_IsTheKeepersPositioningSlot_InWorldSpace(int teamId)
        {
            // Displace the keeper from its spawn before the stride: the keeper spawns on its slot, so
            // without this the old wiring (the keeper's own position) would pass the equality below too.
            MatchEngine engine = NewEngine();
            int keeper = GoalkeeperForTeam(engine, teamId);
            Vector2 spawn = engine.TestOnly_AgentSnapshot(keeper).Position;
            PlaceKeeper(engine, teamId, spawn + LateralTowardCentre(spawn) * DisplacementM);
            RunOneStride(engine);

            Vector2 baseline = engine.TestOnly_GoalkeeperState.PositioningContracts[teamId].GkBaselineSlot;
            Vector2 anchor = engine.TestOnly_FormationSlot(keeper);
            Vector2 keeperPosition = engine.TestOnly_AgentSnapshot(keeper).Position;
            Assert.Greater(Vector2.Distance(keeperPosition, anchor), GoalkeeperConstants.GkReactiveRadiusM,
                "fixture: after one stride the keeper is still well off its slot");

            // The slot #11 measures recovery against is the slot the keeper's MOVE_TO_POSITION walks to.
            Assert.AreEqual(anchor.x, baseline.x, 1e-5f, "baseline x is the #12 world-space anchor");
            Assert.AreEqual(anchor.y, baseline.y, 1e-5f, "baseline y is the #12 world-space anchor");

            // And it is in front of the goal this keeper defends — the away slot is mapped out of #12's
            // canonical attack-toward-+X frame, not left in it.
            float ownGoalX = teamId == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
            Assert.Less(Mathf.Abs(baseline.x - ownGoalX), 20f, "the slot is at the keeper's own goal");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void DisplacedKeeper_StaysRecovering_UntilTheCooldownElapses(int teamId)
        {
            MatchEngine engine = NewEngineAfterOneStride();
            Vector2 baseline = engine.TestOnly_GoalkeeperState.PositioningContracts[teamId].GkBaselineSlot;
            PlaceKeeper(engine, teamId, baseline + LateralTowardCentre(baseline) * DisplacementM);
            int cooldownEndTick = StageRecovering(engine, teamId);

            for (int tick = RecoveryStartTick + 1; tick < cooldownEndTick; tick++)
            {
                DriveTacticalAt(engine, tick);
                Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(teamId),
                    "a keeper off its slot recovers for the full cooldown (tick " + tick + ")");
            }

            DriveTacticalAt(engine, cooldownEndTick);
            Assert.AreEqual(GoalkeeperState.Set, engine.TestOnly_GkState(teamId),
                "the cooldown boundary ends recovery");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void KeeperOnItsSlot_LeavesRecovering_OnTheNextPass(int teamId)
        {
            // The OR arm of Recovering → Set (§3.1.1, AR-S1-M5) still fires against the real slot.
            MatchEngine engine = NewEngineAfterOneStride();
            Vector2 baseline = engine.TestOnly_GoalkeeperState.PositioningContracts[teamId].GkBaselineSlot;
            PlaceKeeper(engine, teamId, baseline);
            StageRecovering(engine, teamId);

            DriveTacticalAt(engine, RecoveryStartTick + 1);
            Assert.AreEqual(GoalkeeperState.Set, engine.TestOnly_GkState(teamId),
                "a keeper already at its slot has nothing to recover to");
        }

        // ── Fixture helpers ──────────────────────────────────────────────────────────────

        private static MatchEngine NewEngine()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            return engine;
        }

        /// <summary>A GK-enabled engine run for exactly one AI stride, so #12 has composed live slots and
        /// the composition root has fed them to #11 through the production stride order.</summary>
        private static MatchEngine NewEngineAfterOneStride()
        {
            MatchEngine engine = NewEngine();
            RunOneStride(engine);
            return engine;
        }

        private static void RunOneStride(MatchEngine engine)
        {
            for (int i = 0; i < DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.RunTick();
            }

            // Neither the threat nor the stand-down predicate may be live, or Recovering could exit to
            // Resting / the keeper could anticipate instead of exercising the cooldown.
            float ballX = engine.BallView.Position.x;
            Assert.Greater(ballX, MatchEngineConstants.PITCH_LENGTH_M / 3f, "fixture: ball outside team 0's third");
            Assert.Less(ballX, 2f * MatchEngineConstants.PITCH_LENGTH_M / 3f, "fixture: ball outside team 1's third");
        }

        private static void PlaceKeeper(MatchEngine engine, int teamId, Vector2 position)
        {
            int keeper = GoalkeeperForTeam(engine, teamId);
            AgentState state = engine.TestOnly_AgentSnapshot(keeper);
            state.Position = position;
            engine.TestOnly_SetAgent(keeper, in state);
        }

        /// <summary>Stages the keeper in Recovering at <see cref="RecoveryStartTick"/> with the ordinary
        /// cooldown, through #11's own restore seam; returns the cooldown's end tick.</summary>
        private static int StageRecovering(MatchEngine engine, int teamId)
        {
            engine.TestOnly_SetPhysicsFrame(RecoveryStartTick * GoalkeeperConstants.FramesPerTacticalTick);
            GoalkeeperTickState staged = Clone(engine.TestOnly_GoalkeeperState);
            int cooldownEndTick = RecoveryStartTick + GoalkeeperConstants.RecoveryCooldownTicks;
            staged.States[teamId] = GoalkeeperState.Recovering;
            staged.RecoveryCooldownEndTick[teamId] = cooldownEndTick;
            engine.TestOnly_RestoreGoalkeeperState(in staged);
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(teamId), "fixture: staged");
            return cooldownEndTick;
        }

        private static Vector2 LateralTowardCentre(Vector2 slot) =>
            slot.y <= MatchEngineConstants.PITCH_WIDTH_M / 2f ? Vector2.up : Vector2.down;

        private static void DriveTacticalAt(MatchEngine engine, int tacticalTick)
        {
            engine.TestOnly_SetPhysicsFrame(tacticalTick * GoalkeeperConstants.FramesPerTacticalTick);
            engine.TestOnly_DriveGkHeadingTactical();
        }

        private static int GoalkeeperForTeam(MatchEngine engine, int teamId)
        {
            int start = teamId * MatchEngineConstants.PLAYERS_PER_TEAM;
            int end = start + MatchEngineConstants.PLAYERS_PER_TEAM;
            for (int i = start; i < end; i++)
            {
                if (engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            Assert.Fail("fixture: team " + teamId + " has a keeper");
            return -1;
        }

        /// <summary>Deep copy: CaptureState exposes #11's live arrays, which a fixture must not write.</summary>
        private static GoalkeeperTickState Clone(GoalkeeperTickState s) =>
            new GoalkeeperTickState(
                (GoalkeeperState[])s.States.Clone(),
                (GoalkeeperAgentAttributes[])s.Attrs.Clone(),
                (GkContactState[])s.ContactStates.Clone(),
                (SaveIntent[])s.SaveIntents.Clone(),
                (bool[])s.SaveIntentActive.Clone(),
                (ClaimIntent[])s.ClaimIntents.Clone(),
                (bool[])s.ClaimIntentActive.Clone(),
                (RushIntent[])s.RushIntents.Clone(),
                (bool[])s.RushIntentActive.Clone(),
                (DistributeIntent[])s.DistributeIntents.Clone(),
                (bool[])s.DistributeIntentActive.Clone(),
                (GoalkeeperPositioningContract[])s.PositioningContracts.Clone(),
                (int[])s.DiveLaunchFrames.Clone(),
                (int[])s.DiveDurationFrames.Clone(),
                (float[])s.DivePeakHandZ.Clone(),
                (float[])s.DiveDirectionLateral.Clone(),
                (float[])s.RushLaunchMps.Clone(),
                (int[])s.RushInitialAttackerId.Clone(),
                (float[])s.ShotDetectedTickMs.Clone(),
                (float[])s.RequiredReactionMs.Clone(),
                (bool[])s.ShotEventPending.Clone(),
                (int[])s.ClaimTick.Clone(),
                (int[])s.ReleaseTickEarliest.Clone(),
                (int[])s.RecoveryCooldownEndTick.Clone());
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-09-27 | —      | Initial: #11 baseline slot is the keeper's #12 world-space slot; a displaced |
// |         |            |        | keeper stays Recovering until RecoveryCooldownTicks; an on-slot keeper exits |
// |         |            |        | on the next pass. Mirrored for both teams.                                   |
#endregion
