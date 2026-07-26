// File:     src/match-engine/tests/MatchEngineMatchContextTests.cs
// Created:  2026-06-22
// Modified: 2026-06-22
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase C (C4/C5), Code Standards #20
// Purpose:  Phase C C4/C5 tests — the host authors MatchContext each Resolve (possession state,
//           home-perspective ball zone), a scripted pass reaches CONTACT and possession is released
//           through the booted EventBus, and the C5 snapshot extension (executor state + MatchContext)
//           feeds the digest while staying deterministic across two same-seed runs.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;
using TacticalDirector.PassMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase C step C4/C5 MatchContext + snapshot-extension tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineMatchContextTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // Home outfielders (roster index 0 is the home goalkeeper); away outfielder (index 11 is the
        // away goalkeeper, so 12 is the first away outfielder).
        private const int HomeAgentA = 1;
        private const int HomeAgentB = 2;
        private const int AwayAgent  = 12;

        private static BallState StationaryBallAtX(float x)
        {
            return BallState.CreateAtPosition(new Vector3(x, MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M));
        }

        // ── C4: MatchContext authoring ────────────────────────────────────────────────

        [Test]
        public void MatchContext_BallZone_IsHomePerspective()
        {
            // Zones split the pitch into thirds from the HOME goal line (x=0): ≤35 DEFENSIVE,
            // ≥65 ATTACKING, else MIDFIELD. The host authors home-perspective only (ERR-008-002 guard).
            AssertBallZoneForX(20f, FieldZone.DEFENSIVE);
            AssertBallZoneForX(50f, FieldZone.MIDFIELD);
            AssertBallZoneForX(90f, FieldZone.ATTACKING);
        }

        private static void AssertBallZoneForX(float x, FieldZone expected)
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetBall(StationaryBallAtX(x)); // Stationary physics leaves x in place
            engine.RunTick();

            Assert.AreEqual(expected, engine.TestOnly_MatchContext.BallZone,
                $"Ball at x={x} must map to {expected} from the home perspective.");
            Assert.AreEqual(x, engine.TestOnly_MatchContext.BallPosition.x, 1e-4f,
                "MatchContext.BallPosition must track the authoritative ball.");
        }

        [Test]
        public void MatchContext_LooseBall_IsContested()
        {
            // §5.Z Phase H: the kickoff is AWARDED, so "loose" must now be established explicitly. The
            // ball is placed far from every agent as well as cleared, since the Phase-H loose-ball pickup
            // would otherwise hand a resting ball straight back to whoever is standing over it.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
            engine.TestOnly_SetBall(StationaryBallAtX(MatchEngineConstants.KickoffBallXM));
            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_MatchContext.PossessingAgentId,
                "A cleared ball with no agent in pickup reach stays loose.");
            Assert.AreEqual(PossessionState.CONTESTED, engine.TestOnly_MatchContext.Possession,
                "A loose ball is CONTESTED.");
        }

        [Test]
        public void MatchContext_Possession_FollowsPossessingAgentTeam()
        {
            var home = new MatchEngine(MatchSeed);
            home.TestOnly_SetPossession(HomeAgentA);
            home.RunTick();
            Assert.AreEqual(PossessionState.HOME_TEAM, home.TestOnly_MatchContext.Possession,
                "A home agent in possession ⇒ HOME_TEAM.");
            Assert.AreEqual(HomeAgentA, home.TestOnly_MatchContext.PossessingAgentId);

            var away = new MatchEngine(MatchSeed);
            away.TestOnly_SetPossession(AwayAgent);
            away.RunTick();
            Assert.AreEqual(PossessionState.AWAY_TEAM, away.TestOnly_MatchContext.Possession,
                "An away agent in possession ⇒ AWAY_TEAM.");
        }

        // ── C4: a scripted pass completes through the booted EventBus; possession releases ────────

        private static PassRequest GroundPassAToB()
        {
            return new PassRequest
            {
                AgentId          = HomeAgentA,
                PassType         = PassType.Ground,
                CrossSubType     = CrossSubType.Flat,
                TargetAgentId    = HomeAgentB,
                TargetPosition   = Vector3.zero,
                IntendedDistance = 10f,
                Urgency          = 0.5f,
                IsWeakFoot       = false,
                TeamId           = 0,
                FrameNumber      = 1
            };
        }

        [Test]
        public void ScriptedPass_ReachesContact_ReleasesPossession_AndKicksBall()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(HomeAgentA);

            PassRequest request = GroundPassAToB();
            PassResult initiated = engine.TestOnly_InitiatePass(HomeAgentA, in request);
            Assert.AreEqual(PassOutcome.Initiated, initiated.Outcome, "Scripted pass must initiate.");

            // Advance the executor lifecycle through WINDUP → CONTACT → FollowThrough → Idle. CONTACT
            // publishes PassAttemptEvent into the booted ledger (drained each Events phase) and releases
            // possession via the adapter's ApplyKick. A generous cap bounds the windup + follow-through.
            const int maxTicks = 200;
            int ticks = 0;
            while (!engine.TestOnly_PassExecutorIdle(HomeAgentA) && ticks < maxTicks)
            {
                engine.RunTick();
                ticks++;
            }

            Assert.IsTrue(engine.TestOnly_PassExecutorIdle(HomeAgentA),
                "Pass executor must return to Idle within the windup + follow-through budget.");
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "CONTACT must release possession (the ball was kicked).");
            Assert.AreEqual(PossessionState.CONTESTED, engine.TestOnly_MatchContext.Possession,
                "MatchContext reflects the post-kick loose ball.");
            Assert.Greater(engine.TestOnly_BallSnapshot.Velocity.sqrMagnitude, 0f,
                "The kicked ball must carry non-zero velocity.");
        }

        [Test]
        public void TwoSameSeedRuns_WithCompletedPass_ProduceIdenticalDigestChains()
        {
            const int tickCount = 120;
            List<byte[]> chainA = RunScriptedPassScenario(tickCount);
            List<byte[]> chainB = RunScriptedPassScenario(tickCount);

            for (int i = 0; i < tickCount; i++)
            {
                CollectionAssert.AreEqual(chainA[i], chainB[i],
                    $"Digest chain diverged at tick {i + 1} with a completed pass (live CONTACT publish).");
            }
        }

        private static List<byte[]> RunScriptedPassScenario(int tickCount)
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(HomeAgentA);
            PassRequest request = GroundPassAToB();
            engine.TestOnly_InitiatePass(HomeAgentA, in request);

            var chain = new List<byte[]>(tickCount);
            for (int i = 0; i < tickCount; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        // ── C5: the snapshot extension (MatchContext + executor state) feeds the digest ──────────

        [Test]
        public void Possession_FeedsSnapshotDigest_ViaMatchContext()
        {
            // Baseline: loose ball (CONTESTED). Perturbed: same world state except possession is set,
            // which changes ONLY MatchContext (the raw _possessingAgentId is not serialized directly,
            // and no pass is scripted so the executors stay idle/identical). The digest must differ.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetPossession(HomeAgentA);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Setting possession left the digest unchanged — MatchContext is not in the digest preimage (C5 regression).");
        }

        [Test]
        public void ExecutorState_FeedsSnapshotDigest()
        {
            // Both engines have possession set to the same agent ⇒ identical MatchContext. The ONLY
            // difference is that the perturbed engine has a pass mid-windup, so its serialized
            // PassExecutorState differs from the idle baseline. One tick keeps the pass below CONTACT.
            var baseline = new MatchEngine(MatchSeed);
            baseline.TestOnly_SetPossession(HomeAgentA);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetPossession(HomeAgentA);
            PassRequest request = GroundPassAToB();
            perturbed.TestOnly_InitiatePass(HomeAgentA, in request);
            perturbed.RunTick();

            Assert.IsFalse(perturbed.TestOnly_PassExecutorIdle(HomeAgentA),
                "Probe precondition: one tick must keep the pass mid-windup (below CONTACT).");
            Assert.AreEqual(HomeAgentA, perturbed.TestOnly_PossessingAgentId,
                "Probe precondition: possession unchanged (so MatchContext matches the baseline).");

            CollectionAssert.AreNotEqual(baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A mid-windup pass executor left the digest unchanged — the C5 executor state is not serialized.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Initial Phase C C4/C5 tests: MatchContext authoring (home-     |
// |         |            |        | perspective ball zone, possession-state derivation, loose =   |
// |         |            |        | CONTESTED), scripted pass reaches CONTACT + releases           |
// |         |            |        | possession through the booted EventBus, same-seed determinism |
// |         |            |        | with a live CONTACT publish, and C5 digest-preimage probes for |
// |         |            |        | MatchContext + executor state.                                 |
#endregion
