// File:     src/match-engine/tests/MatchEngineBallLooseReleaseTests.cs
// Created:  2026-11-18
// Author:   —
// Spec:     Defensive AI #14 §3.6.5.8 (A′, ERR-014-011), w-440-ball-loose-design.md v1.6 D1 section,
//           Code Standards #20
// Purpose:  Correctness acceptance for A′ (owner decision D1): the BallLoose knock from duel geometry,
//           the frame-local carrier fence on every ordinary recovery path, and the fence's lifetime
//           (zero at Snapshot and off-tick, so restore continues exactly). Each case is built to fail
//           if the mechanism it names is removed; the controls run the same staging without it.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>A′ (#14 §3.6.5.8) through a real <c>MatchEngine</c>.</summary>
    [TestFixture]
    internal class MatchEngineBallLooseReleaseTests
    {
        private const ulong Seed = 0x0A11CE5EED440A11UL;

        // Roster convention: 0..10 home, 11..21 away. Index 0 / 11 are the kickoff keepers.
        private const int HomePlayer = 5;
        private const int AwayPlayer = 16;

        private const float FieldLength = 105f;

        /// <summary>Stages carrier and tackler on an otherwise empty corner of the pitch: every other
        /// agent is parked far away, so any admission the fence refuses cannot fall to a bystander.</summary>
        private static MatchEngine Stage(
            int carrier, Vector2 carrierPos, Vector2 carrierVel, Vector2 carrierFacing,
            int tackler, Vector2 tacklerPos, Vector2 tacklerVel, bool pastKickoff = false)
        {
            var engine = new MatchEngine(Seed);
            if (pastKickoff)
            {
                // One real tick first, so the opening kickoff award is behind us before staging.
                engine.RunTick();
            }
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i == carrier || i == tackler) continue;
                float x = i < MatchEngineConstants.PLAYERS_PER_TEAM ? 2f + i : 60f + i;
                engine.TestOnly_SetAgent(i, AgentState.CreateAtPosition(new Vector2(x, 66f), Vector2.right));
            }

            AgentState c = AgentState.CreateAtPosition(carrierPos, carrierFacing);
            c.Velocity = carrierVel;
            c.Speed = carrierVel.magnitude;
            engine.TestOnly_SetAgent(carrier, c);

            AgentState t = AgentState.CreateAtPosition(tacklerPos, (carrierPos - tacklerPos).normalized);
            t.Velocity = tacklerVel;
            t.Speed = tacklerVel.magnitude;
            engine.TestOnly_SetAgent(tackler, t);

            engine.TestOnly_SetPossession(carrier);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State, "staging: carrier controls the ball");
            return engine;
        }

        private static Vector2 Mirror(Vector2 p) => new Vector2(FieldLength - p.x, p.y);
        private static Vector2 MirrorVel(Vector2 v) => new Vector2(-v.x, v.y);

        // ── Knock ────────────────────────────────────────────────────────────

        [TestCase(true)]
        [TestCase(false)]
        public void BallLoose_KnocksBallWithGeometryVelocity_MirroredByTeam(bool homeCarrier)
        {
            // Lunge from behind (§3.6.5.8): carrier at 3 m/s, tackler 0.6 m behind at 8.5 m/s ⇒
            // c = 5.5, v = 0.6·5.5·n + 0.5·v_c = (4.8, 0) toward the attacking end. Mirrored for away.
            int carrier = homeCarrier ? HomePlayer : AwayPlayer;
            int tackler = homeCarrier ? AwayPlayer : HomePlayer;
            Vector2 cPos = new Vector2(30f, 8f), cVel = new Vector2(3f, 0f), tPos = new Vector2(29.4f, 8f), tVel = new Vector2(8.5f, 0f);
            Vector2 facing = Vector2.right;
            if (!homeCarrier)
            {
                cPos = Mirror(cPos); cVel = MirrorVel(cVel); tPos = Mirror(tPos); tVel = MirrorVel(tVel); facing = Vector2.left;
            }
            MatchEngine engine = Stage(carrier, cPos, cVel, facing, tackler, tPos, tVel);

            engine.TestOnly_ApplyBallLooseOutcome(tackler, carrier);

            float sign = homeCarrier ? 1f : -1f;
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId, "nobody holds a knocked ball");
            Assert.AreEqual(BallStateType.Rolling, engine.BallView.State, "the knocked ball rolls; it is not left at rest");
            Assert.That(engine.BallView.Velocity.x, Is.EqualTo(sign * 4.8f).Within(1e-4f), "poke + carry along the attack");
            Assert.That(engine.BallView.Velocity.y, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(engine.BallView.Velocity.z, Is.EqualTo(0f).Within(1e-6f), "the knock is along the ground");
            Assert.AreEqual(1u << carrier, engine.TestOnly_BallLooseFenceMask, "exactly the carrier is fenced");
        }

        // ── Fence on each ordinary recovery path ─────────────────────────────

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Pickup_RefusesFencedCarrier_ControlPicksUp(bool homeCarrier, bool fenced)
        {
            int carrier = homeCarrier ? HomePlayer : AwayPlayer;
            int tackler = homeCarrier ? AwayPlayer : HomePlayer;
            Vector2 cPos = homeCarrier ? new Vector2(30f, 8f) : Mirror(new Vector2(30f, 8f));
            // The tackler is staged out of pickup reach so only the carrier could claim a resting ball.
            Vector2 tPos = homeCarrier ? new Vector2(27.5f, 8f) : Mirror(new Vector2(27.5f, 8f));
            MatchEngine engine = Stage(carrier, cPos, Vector2.zero, homeCarrier ? Vector2.right : Vector2.left,
                tackler, tPos, Vector2.zero);

            var rest = new Vector3(cPos.x, cPos.y, MatchEngineConstants.BALL_REST_HEIGHT_M);
            if (fenced)
            {
                engine.TestOnly_ApplyBallLooseOutcome(tackler, carrier);
            }
            // Re-stage a ball AT REST at the carrier's feet: the pre-A′ episode shape exactly.
            engine.TestOnly_ForceBallLoose(rest, Vector3.zero);

            engine.TestOnly_RunResolvePhase();

            if (fenced)
            {
                Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                    "the dispossessed carrier must not pick the ball back up in the tackle frame");
            }
            else
            {
                Assert.AreEqual(carrier, engine.TestOnly_PossessingAgentId,
                    "control: without the fence the same staging is a passive pickup by the carrier");
            }
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void FirstTouch_RefusesFencedCarrier_ControlTouches(bool homeCarrier, bool fenced)
        {
            int carrier = homeCarrier ? HomePlayer : AwayPlayer;
            int tackler = homeCarrier ? AwayPlayer : HomePlayer;
            Vector2 cPos = homeCarrier ? new Vector2(30f, 8f) : Mirror(new Vector2(30f, 8f));
            Vector2 tPos = homeCarrier ? new Vector2(27.5f, 8f) : Mirror(new Vector2(27.5f, 8f));
            MatchEngine engine = Stage(carrier, cPos, Vector2.zero, homeCarrier ? Vector2.right : Vector2.left,
                tackler, tPos, Vector2.zero);

            if (fenced)
            {
                engine.TestOnly_ApplyBallLooseOutcome(tackler, carrier);
            }
            // A ball rolling INTO the carrier from 0.6 m away along y: a first-touch receive for him.
            var ballPos = new Vector3(cPos.x, cPos.y + 0.6f, MatchEngineConstants.BALL_REST_HEIGHT_M);
            var ballVel = new Vector3(0f, -2f, 0f);
            engine.TestOnly_ForceBallLoose(ballPos, ballVel);

            engine.TestOnly_RunResolvePhase();

            bool touched = engine.TestOnly_PossessingAgentId == carrier
                || (engine.BallView.Velocity - ballVel).sqrMagnitude > 1e-8f;
            if (fenced)
            {
                Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                    "the fenced carrier must not receive the ball");
                Assert.IsFalse(touched, "no first touch may run for the fenced carrier");
            }
            else
            {
                Assert.IsTrue(touched, "control: without the fence the carrier makes the first touch");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void KeeperHandContact_RefusedForFencedKeeper_ControlAccepted(int team)
        {
            var engine = new MatchEngine(Seed);
            int keeper = -1;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == team) { keeper = i; break; }
            }
            Assert.GreaterOrEqual(keeper, 0, "staging: keeper found");
            int opponent = team == 0 ? AwayPlayer : HomePlayer;

            Vector2 kPos = team == 0 ? new Vector2(8f, 34f) : Mirror(new Vector2(8f, 34f));
            engine.TestOnly_SetAgent(keeper, AgentState.CreateAtPosition(kPos, team == 0 ? Vector2.right : Vector2.left));
            engine.TestOnly_SetAgent(opponent, AgentState.CreateAtPosition(
                kPos + new Vector2(team == 0 ? 0.6f : -0.6f, 0f), Vector2.zero));
            var inArea = new Vector3(kPos.x, kPos.y, MatchEngineConstants.BALL_REST_HEIGHT_M);

            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(keeper, inArea),
                "control: an unfenced keeper inside his own area makes hand contact");

            engine.TestOnly_SetPossession(keeper);
            engine.TestOnly_ApplyBallLooseOutcome(opponent, keeper);

            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(keeper, inArea),
                "a keeper dispossessed this frame must not take the knocked ball back in his hands");
        }

        // ── Fence lifetime ───────────────────────────────────────────────────

        [Test]
        public void Fence_DoesNotSurviveIntoTheNextFrame()
        {
            // Armed off-tick by the test seam, then a real RunTick: Input clears it, so the carrier
            // standing over a resting ball takes it in that tick, and the mask is zero afterwards.
            Vector2 cPos = new Vector2(30f, 8f);
            MatchEngine engine = Stage(HomePlayer, cPos, Vector2.zero, Vector2.right,
                AwayPlayer, new Vector2(27.5f, 8f), Vector2.zero, pastKickoff: true);
            engine.TestOnly_ApplyBallLooseOutcome(AwayPlayer, HomePlayer);
            Assert.AreNotEqual(0u, engine.TestOnly_BallLooseFenceMask, "staging: fence armed");
            engine.TestOnly_ForceBallLoose(
                new Vector3(cPos.x, cPos.y, MatchEngineConstants.BALL_REST_HEIGHT_M), Vector3.zero);

            engine.RunTick();

            Assert.AreEqual(0u, engine.TestOnly_BallLooseFenceMask, "no fence after RunTick returns");
            Assert.AreEqual(HomePlayer, engine.TestOnly_PossessingAgentId,
                "a stale fence from an earlier frame must not block ordinary recovery");
        }

        // ── Composed play: zero same-frame regains, exact restore at the tackle frame ──

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "tackle.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);
            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;
            return RosterGenerator.Generate(rng, stream, clubId, template);
        }

        private sealed class TwoClubProvider : ISquadProvider
        {
            private readonly ulong _seed;
            public TwoClubProvider(ulong seed) { _seed = seed; }
            public Squad ResolveByClubId(int clubId) => BuildSquad(_seed, clubId);
        }

        // The #435/#442 frozen corpus seed with the most tackles in the W2 census.
        private const ulong CorpusSeed = 0x0F1E2D3C4B5A6978UL;

        [Test]
        [Timeout(7200000)]
        public void ComposedPlay_NoSameFrameRegain_FenceZeroAtEveryBoundary_RestoreExactAtTackleFrame()
        {
            var engine = new MatchEngine(CorpusSeed);
            engine.ConfigureSquads(BuildSquad(CorpusSeed, 1), BuildSquad(CorpusSeed, 2));

            // Restore check, run SEQUENTIALLY: the EventBus ledger is process-static, so two engines
            // ticked interleaved in one process corrupt each other's digest chain whatever the code
            // under test does. The uninterrupted run is recorded first; the restored run follows alone.
            int looseEpisodes = 0;
            int firstLooseTick = -1;
            byte[] blobAtFirstLoose = null;
            byte[] uninterruptedAfter600 = null;
            const int MaxTicks = 150_000;
            for (int t = 0; t < MaxTicks && (looseEpisodes < 4 || uninterruptedAfter600 == null); t++)
            {
                int holderBefore = engine.TestOnly_PossessingAgentId;
                int looseBefore = engine.TestOnly_TackleOutcomeCounts.Loose;

                engine.RunTick();
                Assert.AreEqual(0u, engine.TestOnly_BallLooseFenceMask, $"fence live after RunTick at tick {t}");

                if (firstLooseTick >= 0 && uninterruptedAfter600 == null && t == firstLooseTick + 600)
                {
                    uninterruptedAfter600 = MatchSaveManager.Encode(engine);
                }

                if (engine.TestOnly_TackleOutcomeCounts.Loose == looseBefore)
                {
                    continue;
                }

                looseEpisodes++;
                // Law awards in the same frame are censored, not policed (D1 policy scope).
                if (engine.RestartAppliedThisTick == RestartCue.None)
                {
                    Assert.AreNotEqual(holderBefore, engine.TestOnly_PossessingAgentId,
                        $"BallLoose at tick {t}: the carrier re-acquired in the tackle frame");
                }
                if (blobAtFirstLoose == null)
                {
                    blobAtFirstLoose = MatchSaveManager.Encode(engine);
                    firstLooseTick = t;
                }
            }

            Assert.That(looseEpisodes, Is.GreaterThan(0),
                "no BallLoose occurred, so the same-frame and restore assertions proved nothing");
            Assert.IsNotNull(uninterruptedAfter600, "the uninterrupted run never reached 600 ticks past the first BallLoose");

            // Restore at the BallLoose frame's committed boundary and replay alone: the save 600 ticks
            // later must be byte-identical to the uninterrupted run's, so the fence left no hidden state.
            MatchEngine restored = MatchSaveManager.Restore(blobAtFirstLoose, new TwoClubProvider(CorpusSeed));
            for (int t = 0; t < 600; t++)
            {
                restored.RunTick();
            }
            CollectionAssert.AreEqual(uninterruptedAfter600, MatchSaveManager.Encode(restored),
                "restore at the BallLoose frame diverged from uninterrupted play");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-18 | —      | A′ / ERR-014-011 (owner decision D1): knock velocity and mirror; fence on pickup, first touch and keeper hand contact, each with an unfenced control; fence lifetime; composed zero same-frame regain and tackle-frame restore. |
#endregion
