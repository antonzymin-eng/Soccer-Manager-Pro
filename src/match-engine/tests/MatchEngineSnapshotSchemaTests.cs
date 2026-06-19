// File:     src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs
// Created:  2026-06-16
// Modified: 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2.6 / §5 Phase B (B3), Code Standards #20
// Purpose:  Phase B step B3 tests — proves the full §2.6 world-state field set (not just the B2
//           kinematic subset) feeds the snapshot digest: perturbing the embedded OscillationGuard
//           ring-buffer state (B0 seam) or the ball spin changes the digest, and the schema pin holds.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase B step B3 full-field-set snapshot-serialization tests for <see cref="MatchEngine"/>.
    /// Each "feeds the digest" test perturbs a single field that the B2 kinematic subset did NOT
    /// serialize and asserts the post-tick digest changes — the field is now in the digest preimage.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineSnapshotSchemaTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const int   OutfieldIndex = 1; // roster index 0 is the goalkeeper (skipped by movement)

        // A locked guard state with a finite far-future lock-until and a finite recorded timestamp,
        // distinct from a freshly initialised guard (all -Infinity, unlocked). Used to perturb one
        // agent's OscillationGuard without touching any other field.
        private static OscillationGuardState LockedGuardState()
        {
            return new OscillationGuardState(
                0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                writeIndex: 0, isLocked: true, lockUntilTime: 1000f);
        }

        [Test]
        public void SchemaVersion_IsPinned()
        {
            // The pin must change deliberately (with a field-set or ordering change), never by drift.
            Assert.AreEqual(1u, MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION,
                "SNAPSHOT_SCHEMA_VERSION drifted — bump it intentionally only with a field-set/order change.");
        }

        [Test]
        public void GuardState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (every guard freshly initialised, unlocked).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: identical kickoff state except one outfielder's OscillationGuard is locked.
            // The agent holds a Stop command at rest, so the state machine takes no transition this
            // tick and the guard passes through to the snapshot unchanged — a clean single-field probe.
            var perturbed = new MatchEngine(MatchSeed);
            AgentState a = perturbed.TestOnly_AgentSnapshot(OutfieldIndex);
            a.OscillationGuard.RestoreState(LockedGuardState());
            perturbed.TestOnly_SetAgent(OutfieldIndex, a);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the OscillationGuard ring-buffer state left the digest unchanged — " +
                "the B0 guard state is not in the digest preimage (B3 regression).");
        }

        [Test]
        public void BallSpin_FeedsSnapshotDigest()
        {
            // Baseline: stationary ball at the centre spot, zero spin.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: a STATIONARY ball with non-zero spin. The Stationary branch of
            // BallPhysicsCore returns early without clearing AngularVelocity, so the spin survives the
            // tick and reaches the snapshot — proving the expanded ball field set is serialized.
            var perturbed = new MatchEngine(MatchSeed);
            BallState spun = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M));
            spun.AngularVelocity = new Vector3(0f, 0f, 5f);
            perturbed.TestOnly_SetBall(spun);
            perturbed.RunTick();

            Assert.AreEqual(BallStateType.Stationary, perturbed.TestOnly_BallSnapshot.State,
                "Probe precondition: the ball must remain Stationary so spin is not physics-driven.");
            Assert.AreEqual(5f, perturbed.TestOnly_BallSnapshot.AngularVelocity.z, 1e-6f,
                "Probe precondition: Stationary physics must preserve the injected spin.");

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing ball AngularVelocity left the digest unchanged — the ball spin field is " +
                "not serialized (B2 serialized position only).");
        }

        [Test]
        public void TwoSameSeedRuns_WithLockedGuard_ProduceIdenticalDigestChains()
        {
            // The guard serialization must itself be deterministic: two same-seed runs that inject the
            // same locked guard must agree tick-for-tick.
            List<byte[]> chainA = RunWithLockedGuard();
            List<byte[]> chainB = RunWithLockedGuard();

            Assert.AreEqual(chainA.Count, chainB.Count);
            for (int i = 0; i < chainA.Count; i++)
            {
                CollectionAssert.AreEqual(
                    chainA[i], chainB[i],
                    $"Digest chain diverged at tick {i + 1} with a serialized locked OscillationGuard.");
            }
        }

        private static List<byte[]> RunWithLockedGuard()
        {
            const int ticks = 30;
            var engine = new MatchEngine(MatchSeed);
            AgentState a = engine.TestOnly_AgentSnapshot(OutfieldIndex);
            a.OscillationGuard.RestoreState(LockedGuardState());
            engine.TestOnly_SetAgent(OutfieldIndex, a);

            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-16 | —      | Initial Phase B step B3 tests: schema-version pin, OscillationGuard |
// |         |            |        | + ball-spin digest-preimage probes, and locked-guard determinism.  |
#endregion
