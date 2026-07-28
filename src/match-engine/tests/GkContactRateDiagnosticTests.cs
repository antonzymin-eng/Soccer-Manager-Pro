// File:     src/match-engine/tests/GkContactRateDiagnosticTests.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11, Positioning AI #12, Testing Strategy #19 (instrument class)
// Purpose:  Env-gated (TD_GK_DIAGNOSTIC=1) contact-rate anatomy instrument for the keeper-contact
//           pass (gk-contact-rate-design.md). Decomposes every goalward threat episode into WHY the
//           keeper did or did not contact the ball: no dive at all, dive landed before the ball
//           arrived (timing), dive launched after it crossed (timing), or airborne at the crossing
//           but laterally short (position/reach). Asserts nothing (the ERR-030-014 convention) —
//           a diagnostic must not turn a defect into a contract.
//
//           gk-catch-parry-conversion §7.1 recorded the contact rate as lever (c)'s residual with a
//           partially measured anatomy (mean lateral offset while airborne 1.7-4.6 m vs 2.2 m dive
//           displacement + ~1.35 m reach). That number aggregates over airborne FRAMES; it cannot
//           say whether the binding deficit is the #12 slot's lateral positioning or the #11
//           commit-to-arrival timing. This instrument classifies per EPISODE, at the ball's actual
//           goal-plane crossing, which is the moment a save either happens or does not.
//
//           Run:
//             TD_GK_DIAGNOSTIC=1 dotnet test -c Release --filter GkContactRateDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Per-episode keeper contact-rate decomposition over full 90-minute matches on the
    /// <c>ConfigureSquads</c> path (the §5.Z.20 measurement population). See file header.
    /// </summary>
    [TestFixture]
    internal class GkContactRateDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        private const float TicksPerSecond = 60.0f;

        /// <summary>The §5.Z.20 seeds, so pre/post numbers are same-population comparable.</summary>
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
        public void GkContactRateDiagnostic_ReportsEpisodeAnatomy()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== Keeper contact-rate anatomy: WHY does the keeper miss three quarters of on-target shots? ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}  diveDurationMs={GoalkeeperConstants.DivePhaseDurationMs:F0}  displacement={GoalkeeperConstants.DiveLaunchDisplacementM:F2} m"));
            report.AppendLine();
            report.AppendLine("Episode = armed-geometry rising edge (loose ball driving at a goal) -> goal-plane");
            report.AppendLine("crossing / disarm. Classification is at the CROSSING, the moment a save happens or not:");
            report.AppendLine("  contact       : the hand envelope met the ball during the episode");
            report.AppendLine("  no-dive       : the ball crossed the plane and the keeper never went airborne");
            report.AppendLine("  dive-early    : the dive was launched AND OVER before the ball crossed (timing)");
            report.AppendLine("  dive-late     : the dive launched after the crossing (timing)");
            report.AppendLine("  lateral-miss  : airborne AT the crossing but the envelope was laterally short (position)");
            report.AppendLine("  faded         : the episode disarmed before any crossing (deflection, pickup, slowdown)");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed);
            }

            report.AppendLine("Reading it:");
            report.AppendLine("  * dive-early dominating  => the #11 commit-to-arrival timing is the mass: the keeper");
            report.AppendLine("    dives the moment SAVE commits, and the 600 ms envelope closes before the ball arrives.");
            report.AppendLine("  * lateral-miss / no-dive with large |ballY-gkY| at crossing => the #12 GK slot's");
            report.AppendLine("    lateral positioning is the mass: the keeper is not on the shot line to begin with.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunMatch(StringBuilder report, ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int teams = GoalkeeperConstants.MaxGkAgents;
            var agg = new Agg[teams];
            var ep = new Episode[teams];
            var prevBall = engine.BallView.Position;
            var prevAirborne = new bool[teams];
            var prevContactFrame = new int[teams];
            for (int t = 0; t < teams; t++)
            {
                agg[t] = new Agg();
                ep[t] = new Episode();
                prevContactFrame[t] = -1;
            }

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                UnityEngine.Vector3 ballVel = engine.BallView.Velocity;
                bool loose = engine.PossessingAgentId < 0;

                for (int t = 0; t < teams; t++)
                {
                    float goalX = t == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
                    float distToGoalLine = Math.Abs(ballPos.x - goalX);
                    float towardGoal = (goalX - ballPos.x) * ballVel.x;
                    float planarSpeed = new UnityEngine.Vector2(ballVel.x, ballVel.y).magnitude;
                    bool armed = loose
                                 && distToGoalLine <= MatchEngineConstants.GkSaveTriggerRangeM
                                 && towardGoal > 0f
                                 && planarSpeed >= MatchEngineConstants.GkSaveTriggerMinBallSpeedMps;

                    int gkAgent = GkAgentId(engine, t);
                    UnityEngine.Vector2 gkXY = gkAgent >= 0
                        ? engine.AgentView(gkAgent).Position
                        : new UnityEngine.Vector2(goalX, MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

                    // ── Episode lifecycle ────────────────────────────────────────────
                    if (!ep[t].Live && armed)
                    {
                        ep[t] = new Episode
                        {
                            Live = true,
                            StartTick = tick,
                            StartGkY = gkXY.y,
                            StartBallY = ballPos.y,
                            LaunchTick = -1,
                        };
                    }

                    if (ep[t].Live)
                    {
                        // Dive launch/land tracking (Airborne rising edge).
                        bool airborne = gk.States[t] == GoalkeeperState.Airborne;
                        if (airborne && !prevAirborne[t])
                        {
                            ep[t].LaunchTick = tick;
                            ep[t].DurTicks = gk.DiveDurationFrames[t];
                            ep[t].LaunchGkY = gkXY.y;
                        }
                        prevAirborne[t] = airborne;

                        // Contact: ActualContactFrame advances exactly when the reach gate passed.
                        int cf = gk.ContactStates[t].ActualContactFrame;
                        if (cf != prevContactFrame[t] && cf >= 0)
                        {
                            ep[t].Contacted = true;
                            prevContactFrame[t] = cf;
                        }

                        // Goal-plane crossing (interpolated between prev and current tick).
                        bool crossed = t == 0
                            ? prevBall.x > 0f && ballPos.x <= 0f
                            : prevBall.x < MatchEngineConstants.PITCH_LENGTH_M
                              && ballPos.x >= MatchEngineConstants.PITCH_LENGTH_M;

                        if (crossed)
                        {
                            float dx = ballPos.x - prevBall.x;
                            float alpha = Math.Abs(dx) > 1e-6f ? (goalX - prevBall.x) / dx : 0f;
                            float yCross = prevBall.y + (ballPos.y - prevBall.y) * alpha;
                            float zCross = prevBall.z + (ballPos.z - prevBall.z) * alpha;
                            bool inMouth = Math.Abs(yCross - MatchEngineConstants.PITCH_WIDTH_M * 0.5f) <= GoalHalfWidthM
                                           && zCross <= GoalHeightM;

                            Classify(agg[t], ep[t], tick, yCross, gkXY.y, inMouth,
                                     gk.States[t] == GoalkeeperState.Airborne);
                            ep[t] = default;
                        }
                        else if (!armed && !ep[t].Contacted)
                        {
                            // Disarmed without crossing and without a contact: faded (deflected,
                            // picked up, or ran out of pace). A contacted episode that then disarms
                            // is a SAVE and was already counted at the contact.
                            agg[t].Faded++;
                            ep[t] = default;
                        }
                        else if (!armed)
                        {
                            agg[t].ContactEpisodes++;
                            ep[t] = default;
                        }
                    }
                }

                prevBall = ballPos;
            }

            report.AppendLine(Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}"));
            report.AppendLine("  gk | episodes | faded | contact | crossed:on-target | no-dive | dive-early | dive-late | lateral-miss | meanEarlyByMs | mean|ballY-gkY|@cross | meanFlightMs | mean|gkY-34|@start");
            for (int t = 0; t < teams; t++)
            {
                Agg x = agg[t];
                int crossed = x.NoDive + x.DiveEarly + x.DiveLate + x.LateralMiss;
                report.AppendLine(
                    Inv($"   {t} | {x.Episodes(),8} | {x.Faded,5} | {x.ContactEpisodes,7} | {crossed,4}:{x.OnTarget,-12} | ")
                    + Inv($"{x.NoDive,7} | {x.DiveEarly,10} | {x.DiveLate,9} | {x.LateralMiss,12} | ")
                    + Inv($"{(x.DiveEarly > 0 ? x.EarlyByMsSum / x.DiveEarly : 0f),13:F0} | ")
                    + Inv($"{(crossed > 0 ? x.LateralGapSum / crossed : 0f),21:F2} | ")
                    + Inv($"{(crossed > 0 ? x.FlightMsSum / crossed : 0f),12:F0} | ")
                    + Inv($"{(crossed > 0 ? x.GkOffsetStartSum / crossed : 0f),17:F2}"));
            }
            report.AppendLine();
        }

        private static void Classify(
            Agg agg, Episode ep, int crossTick, float yCross, float gkY, bool inMouth, bool airborneNow)
        {
            if (inMouth)
            {
                agg.OnTarget++;
            }

            if (ep.Contacted)
            {
                agg.ContactEpisodes++;
                return;
            }

            agg.FlightMsSum += (crossTick - ep.StartTick) * (1000f / TicksPerSecond);
            agg.LateralGapSum += Math.Abs(yCross - gkY);
            agg.GkOffsetStartSum += Math.Abs(ep.StartGkY - MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

            if (ep.LaunchTick < 0)
            {
                agg.NoDive++;
            }
            else if (ep.LaunchTick + ep.DurTicks < crossTick)
            {
                agg.DiveEarly++;
                agg.EarlyByMsSum += (crossTick - (ep.LaunchTick + ep.DurTicks)) * (1000f / TicksPerSecond);
            }
            else if (ep.LaunchTick > crossTick)
            {
                agg.DiveLate++;
            }
            else
            {
                agg.LateralMiss++;
            }
        }

        private struct Episode
        {
            public bool Live;
            public int StartTick;
            public float StartGkY;
            public float StartBallY;
            public int LaunchTick;
            public int DurTicks;
            public float LaunchGkY;
            public bool Contacted;
        }

        private sealed class Agg
        {
            public int Faded;
            public int ContactEpisodes;
            public int OnTarget;
            public int NoDive;
            public int DiveEarly;
            public int DiveLate;
            public int LateralMiss;
            public float EarlyByMsSum;
            public float LateralGapSum;
            public float FlightMsSum;
            public float GkOffsetStartSum;

            public int Episodes() => Faded + ContactEpisodes + NoDive + DiveEarly + DiveLate + LateralMiss;
        }

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

        private static int GkAgentId(MatchEngine engine, int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_GK_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_GK_DIAGNOSTIC=1 to run the keeper contact-rate anatomy instrument.");
            }
        }

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. The gk-catch-parry-conversion §7.1 residual instrument:   |
// |         |            |        | per-episode classification (contact / no-dive / dive-early /       |
// |         |            |        | dive-late / lateral-miss / faded) at the ball's goal-plane          |
// |         |            |        | crossing, separating the #12 positioning deficit from the #11      |
// |         |            |        | commit-timing deficit. Assertion-free (ERR-030-014 convention).    |
#endregion
