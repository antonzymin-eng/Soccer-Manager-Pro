// File:     src/match-engine/tests/ShotOutcomeDiagnosticTests.cs
// Created:  2026-07-27
// Modified: 2026-07-28 (shot-speed pass: + woodworkStrikes report line)
// Author:   —
// Spec:     Shot-outcome distribution design (docs/tracking/shot-outcome-distribution-design.md) §4;
//           Match Engine design note §5.Z.17; path-to-playable roadmap A4a; Code Standards #20
// Purpose:  The MEASUREMENT half of the §5.Z.17 residual — the shot-outcome distribution.
//
//           §5.Z.17 recorded that a shot essentially cannot miss (aim 0.732 m inside the post, the
//           vertical half of the error model inert), there is no crossbar (every boundary test gated
//           on z < 0.22 m), and there are no blocked shots (the agent-ball deflection is an empty
//           TODO). In football roughly 30% of shots are blocked and 30% miss the target; here both
//           read ~0. No instrument in the tree reports any of those numbers — this driver closes
//           that gap, before and after the fixes, so the fixes' effect is attributable.
//
//           Per full 90-minute match it reports:
//             shots            — rising edges of each keeper's ShotDetectedTickMs (#11 is notified
//                                once per completed shot at the defended end)
//             goals            — score deltas (and goals per shot)
//             on-target        — goal-line plane crossings inside the mouth (posts + bar)
//             off-target exits — goal-kick / corner restarts within a window after a shot
//             blocked contacts — AGENT_BALL collision events at ball speed >= the KD-6 deflection
//                                gate within the same window (pre-fix these are pass-throughs;
//                                post-fix they are deflections)
//             shot-speed range — min/mean/max ball speed measured on the shot tick (sanity: the
//                                KD-6 gate must sit below it)
//
//           Env-gated and assertion-free, for the reason every diagnostic in this project is:
//           pinning measured-but-wrong behaviour would turn a defect into a contract (the
//           ERR-030-014 lesson). The acceptance predicates live in the
//           match-engine-shot-outcomes scenario, not here.
//
//             TD_SHOT_DIAGNOSTIC=1 dotnet test -c Release --filter ShotOutcomeDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.CollisionSystem;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class ShotOutcomeDiagnosticTests
    {
        /// <summary>A full 90-minute match — §5.Z.11's standing instruction: measure over a FULL match.</summary>
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        /// <summary>Episode window after a shot inside which a restart / fast contact is attributed
        /// to that shot (10 s at 60 Hz — a shot's flight plus the immediate scramble).</summary>
        private const int ShotEpisodeTicks = 600;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        private static readonly float GoalHalfWidthM =
            TacticalDirector.BallPhysics.BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f;

        private static readonly float GoalHeightM =
            TacticalDirector.BallPhysics.BallPhysicsConstants.Pitch.GOAL_HEIGHT;

        [Test]
        [Category("Calibration")]
        public void ShotOutcomeDiagnostic_ReportsDistribution()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== §5.Z.17 residual: the shot-outcome distribution ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}"));
            report.AppendLine("Football reference: ~2.7 goals/match, ~30% of shots blocked, ~30% off target.");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed);
            }

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunMatch(StringBuilder report, ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            var observer = new FastBallContactObserver(engine);
            engine.TestOnly_SetCollisionObserver(observer);

            var m = new Tally();
            var prevShotMs = new float[] { -1f, -1f };
            int lastShotTick = int.MinValue;
            float prevBallX = engine.BallView.Position.x;
            int prevHome = 0, prevAway = 0;

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                observer.CurrentBallSpeed = engine.BallView.Velocity.magnitude;
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                for (int t = 0; t < GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents; t++)
                {
                    float ms = gk.ShotDetectedTickMs[t];
                    if (ms > 0f && ms != prevShotMs[t])
                    {
                        prevShotMs[t] = ms;
                        m.Shots++;
                        lastShotTick = tick;
                        float speed = engine.BallView.Velocity.magnitude;
                        m.ShotSpeedSum += speed;
                        if (speed < m.ShotSpeedMin) m.ShotSpeedMin = speed;
                        if (speed > m.ShotSpeedMax) m.ShotSpeedMax = speed;
                    }
                }

                // On-target: a goal-line plane CROSSING inside the mouth (counted once per crossing,
                // not once per tick beyond the plane — pre-fix the ball can fly on past the line).
                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                bool crossedHome = prevBallX > 0f && ballPos.x <= 0f;
                bool crossedAway = prevBallX < MatchEngineConstants.PITCH_LENGTH_M
                                   && ballPos.x >= MatchEngineConstants.PITCH_LENGTH_M;
                if ((crossedHome || crossedAway) && InGoalMouth(ballPos))
                {
                    m.OnTargetCrossings++;
                }
                prevBallX = ballPos.x;

                // Off-target exits attributed to the last shot: goal kick or corner inside the window.
                if (tick - lastShotTick <= ShotEpisodeTicks
                    && (engine.RestartAppliedThisTick == RestartCue.GoalKick
                        || engine.RestartAppliedThisTick == RestartCue.Corner))
                {
                    m.OffTargetExits++;
                    lastShotTick = int.MinValue; // one exit per episode
                }

                // Blocked contacts attributed to the last shot.
                if (observer.FastBallContactThisTick && tick - lastShotTick <= ShotEpisodeTicks)
                {
                    m.BlockedContacts++;
                }
                observer.FastBallContactThisTick = false;

                if (engine.HomeScore != prevHome) { m.Goals += engine.HomeScore - prevHome; prevHome = engine.HomeScore; }
                if (engine.AwayScore != prevAway) { m.Goals += engine.AwayScore - prevAway; prevAway = engine.AwayScore; }
            }

            report.AppendLine(Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}"));
            report.AppendLine(Inv($"  shots={m.Shots}  goals={m.Goals}  goals/shot={(m.Shots > 0 ? (float)m.Goals / m.Shots : 0f):F2}"));
            report.AppendLine(Inv($"  onTargetCrossings={m.OnTargetCrossings}  offTargetExits={m.OffTargetExits}  fastBallContacts={m.BlockedContacts}  (allFastContacts={observer.TotalFastContacts})"));
            report.AppendLine(Inv($"  woodworkStrikes={engine.TestOnly_WoodworkStrikes}"));
            if (m.Shots > 0)
            {
                report.AppendLine(Inv($"  shot-tick ball speed: min={m.ShotSpeedMin:F1} mean={m.ShotSpeedSum / m.Shots:F1} max={m.ShotSpeedMax:F1} m/s ")
                                + Inv($"(deflection gate {TacticalDirector.BallPhysics.BallPhysicsConstants.AgentDeflection.MinBallSpeedMps:F1} m/s)"));
            }
            report.AppendLine();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        /// <summary>Counts AGENT_BALL collision events whose pre-tick ball speed clears the KD-6
        /// deflection gate — pre-fix these are pass-throughs, post-fix they are deflections.</summary>
        private sealed class FastBallContactObserver : ICollisionEventConsumer
        {
            private readonly MatchEngine _engine;

            public float CurrentBallSpeed;
            public bool FastBallContactThisTick;
            public int TotalFastContacts;

            public FastBallContactObserver(MatchEngine engine)
            {
                _engine = engine;
            }

            public void OnCollisionEvent(in CollisionEvent evt)
            {
                if (evt.Type != CollisionType.AGENT_BALL)
                {
                    return;
                }

                // Ball speed sampled before the tick ran: the collision fires mid-tick, and a
                // post-deflection read would under-report the incoming speed.
                if (CurrentBallSpeed
                    >= TacticalDirector.BallPhysics.BallPhysicsConstants.AgentDeflection.MinBallSpeedMps)
                {
                    FastBallContactThisTick = true;
                    TotalFastContacts++;
                }
            }
        }

        private static bool InGoalMouth(UnityEngine.Vector3 p)
        {
            float centreY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            return Math.Abs(p.y - centreY) <= GoalHalfWidthM && p.z <= GoalHeightM;
        }

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_SHOT_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_SHOT_DIAGNOSTIC=1 to run the shot-outcome-distribution characterisation.");
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the GkSaveDiagnosticTests
        /// recipe (a position template so LineupSelector can field a back four).</summary>
        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "diagnostic.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);

            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;

            return RosterGenerator.Generate(rng, stream, clubId, template);
        }

        private sealed class Tally
        {
            public int Shots;
            public int Goals;
            public int OnTargetCrossings;
            public int OffTargetExits;
            public int BlockedContacts;
            public float ShotSpeedSum;
            public float ShotSpeedMin = float.MaxValue;
            public float ShotSpeedMax;
        }

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. The measurement gap the shot-outcome pass opens with: no  |
// |         |            |        | instrument reported shots, on/off-target shares or blocked         |
// |         |            |        | contacts. Reports the full distribution per match, before and      |
// |         |            |        | after the KD-1..KD-7 fixes. Asserts nothing (ERR-030-014 lesson).  |
// | 1.1     | 2026-07-28 | —      | Shot-speed pass: + woodworkStrikes report line (TestOnly_WoodworkStrikes — |
// |         |            |        | the KD-6 diagnostic counter).                                              |
#endregion
