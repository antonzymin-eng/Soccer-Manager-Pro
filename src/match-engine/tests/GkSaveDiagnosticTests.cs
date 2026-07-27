// File:     src/match-engine/tests/GkSaveDiagnosticTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.15 / §5.Z.17;
//           Goalkeeper Mechanics #11 §3.1–§3.5; path-to-playable roadmap A4a; Code Standards #20
// Purpose:  The MEASUREMENT half of the §5.Z.15 recorded lever — "the quality of the goalkeeper's save".
//
//           §5.Z.15 flipped #11 on and gave the keeper locomotion, and recorded that what remained was
//           the QUALITY of the save. That framing assumes saves HAPPEN. No instrument in the tree has
//           ever reported whether one does: MatchBalanceDiagnostic reports goals, possession, shots and
//           passes, but nothing about the goalkeeper. This driver closes that gap.
//
//           It walks the whole save pipeline and counts, per keeper, how many attempts survive each
//           stage — a funnel, because a funnel localises WHERE the chain breaks instead of reporting
//           only that the end of it is empty:
//
//               armed  ->  SAVE committed  ->  Anticipate  ->  Diving  ->  Airborne  ->  contact
//                                                                                        -> caught / parried / spilled
//
//           It also reports the per-state tick histogram (which states a keeper actually occupies over
//           a full match) and the reaction-window / handling-quality distributions at contact, because
//           those are the two inputs that decide a save's OUTCOME once contact happens — the thing
//           §5.Z.15 called "quality".
//
//           Env-gated and assertion-free, for the reason every diagnostic in this project is: pinning
//           measured-but-wrong behaviour would turn a defect into a contract (the ERR-030-014 lesson).
//           The acceptance BANDS live in a scenario, not here.
//
//             TD_GK_DIAGNOSTIC=1 dotnet test -c Release --filter GkSaveDiagnostic
//
//           Reading the funnel: the stage where the count collapses to zero is the defect. A break
//           BEFORE contact is a correctness bug in the pipeline; a break AFTER contact (contacts > 0
//           but no catches) is the [GT] balance question §5.Z.15 anticipated.

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class GkSaveDiagnosticTests
    {
        /// <summary>A full 90-minute match — §5.Z.11's standing instruction: measure over a FULL match.</summary>
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        private const float TicksPerSecond = 60.0f;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        /// <summary>Half the goal width (IFAB 7.32 m), for the on-target geometry test.</summary>
        private static readonly float GoalHalfWidthM =
            TacticalDirector.BallPhysics.BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f;

        /// <summary>Goal crossbar height (IFAB 2.44 m).</summary>
        private static readonly float GoalHeightM =
            TacticalDirector.BallPhysics.BallPhysicsConstants.Pitch.GOAL_HEIGHT;

        // ── The save funnel ─────────────────────────────────────────────────────────────────────

        [Test]
        [Category("Calibration")]
        public void GkSaveDiagnostic_ReportsSaveFunnel()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== §5.Z.15 follow-up: does the goalkeeper ever actually make a save? ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch} ({TicksPerMatch / TicksPerSecond / 60f:F0} min)  seeds={Seeds.Length}"));
            report.AppendLine();
            report.AppendLine("The funnel. Each stage is a prerequisite of the next, so the stage where the");
            report.AppendLine("count collapses is the defect:");
            report.AppendLine("  armed      : ticks the §4.1 SaveArmed geometry held (a loose ball driving at the goal)");
            report.AppendLine("  committed  : rising edges of the per-episode SAVE latch (the DT decided to save)");
            report.AppendLine("  anticipate : entries into GoalkeeperState.Anticipate (the only production gate to a dive)");
            report.AppendLine("  diving     : entries into Diving   (the dive was launched)");
            report.AppendLine("  airborne   : entries into Airborne (the reach envelope is live and testable)");
            report.AppendLine("  contact    : frames the ball fell inside the reach envelope");
            report.AppendLine("  caught     : entries into HandsOnBall (quality >= CatchThreshold)");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunFunnel(report, seed);
            }

            report.AppendLine("Reading it:");
            report.AppendLine("  * committed > 0 but anticipate == 0  => the state machine cannot be entered:");
            report.AppendLine("    a CORRECTNESS defect upstream of anything §5.Z.15 called 'quality'.");
            report.AppendLine("  * airborne > 0 but contact == 0      => the keeper dives but cannot reach:");
            report.AppendLine("    a reach-envelope / dive-direction defect.");
            report.AppendLine("  * contact > 0 but caught == 0        => the [GT] balance question §5.Z.15 named.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunFunnel(StringBuilder report, ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int teams = GoalkeeperConstants.MaxGkAgents;
            var f = new Funnel[teams];
            for (int t = 0; t < teams; t++)
            {
                f[t] = new Funnel();
            }

            var prevState = new GoalkeeperState[teams];
            var prevLatch = new bool[teams];
            var prevContactFrame = new int[teams];
            for (int t = 0; t < teams; t++)
            {
                prevState[t] = GoalkeeperState.Resting;
                prevContactFrame[t] = -1;
            }

            int prevHome = 0;
            int prevAway = 0;

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                UnityEngine.Vector3 ballVel = engine.BallView.Velocity;
                bool loose = engine.PossessingAgentId < 0;

                for (int t = 0; t < teams; t++)
                {
                    // Stage 1 — the §4.1 world-state geometry (recomputed here rather than read, so the
                    // instrument does not depend on a latch that a defect may never set).
                    float goalX = t == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
                    float distToGoalLine = Math.Abs(ballPos.x - goalX);
                    float towardGoal = (goalX - ballPos.x) * ballVel.x;
                    float planarSpeed = new UnityEngine.Vector2(ballVel.x, ballVel.y).magnitude;
                    bool armed = loose
                                 && distToGoalLine <= MatchEngineConstants.GkSaveTriggerRangeM
                                 && towardGoal > 0f
                                 && planarSpeed >= MatchEngineConstants.GkSaveTriggerMinBallSpeedMps;
                    if (armed)
                    {
                        f[t].ArmedTicks++;
                    }

                    // Is the §3.2 reaction pipeline actually being fed? Separates "OnShotExecutedEvent
                    // is never called" (shotDetected stays 0) from "it is called but the keeper reacts
                    // so late the window scores 0" — two very different defects with the same symptom.
                    float shotDetected = gk.ShotDetectedTickMs[t];
                    if (shotDetected > 0f && shotDetected != f[t].LastShotDetectedMs)
                    {
                        f[t].ShotsNotified++;
                        f[t].LastShotDetectedMs = shotDetected;
                        f[t].RequiredReactionSum += gk.RequiredReactionMs[t];
                    }

                    // Stage 2 — the DT's SAVE decision, counted on the latch's rising edge.
                    bool latch = engine.TestOnly_SaveCommittedForGk(t);
                    if (latch && !prevLatch[t])
                    {
                        f[t].Committed++;
                    }
                    prevLatch[t] = latch;

                    // Stages 3-5 + 7 — state entries.
                    GoalkeeperState s = gk.States[t];
                    f[t].StateTicks[(int)s]++;
                    if (s != prevState[t])
                    {
                        switch (s)
                        {
                            case GoalkeeperState.Anticipate:  f[t].Anticipate++; break;
                            case GoalkeeperState.Diving:      f[t].Diving++;     break;
                            case GoalkeeperState.Airborne:    f[t].Airborne++;   break;
                            case GoalkeeperState.HandsOnBall: f[t].Caught++;     break;
                            default: break;
                        }
                        prevState[t] = s;
                    }

                    // Stage 6 — hand-envelope contact. ActualContactFrame advances only when the reach
                    // gate passed, so a change in it is exactly one contact.
                    int cf = gk.ContactStates[t].ActualContactFrame;
                    if (cf != prevContactFrame[t] && cf >= 0)
                    {
                        f[t].Contacts++;
                        f[t].QualitySum += gk.ContactStates[t].HandlingQualityScalar;
                        f[t].ReactionSum += gk.ContactStates[t].ReactionWindowAchieved;
                        prevContactFrame[t] = cf;
                    }

                    // WHY a dive misses. Reproduces the orchestrator's own reach test each Airborne
                    // frame and records how far the envelope fell short — the measurement that
                    // separates "reach is slightly too small" from "the keeper is nowhere near it",
                    // and separates BOTH from "the keeper is in the right place but never dives that way".
                    if (s == GoalkeeperState.Airborne)
                    {
                        // How stale the shot is by the time the keeper is airborne. The §3.2 window
                        // peaks when elapsed == required and decays to 0 either side, so a large
                        // elapsed is a LATE keeper, not an unwired one.
                        if (gk.ShotDetectedTickMs[t] > 0f)
                        {
                            f[t].AirborneElapsedSum +=
                                (engine.CurrentTick * 1000f / TicksPerSecond) - gk.ShotDetectedTickMs[t];
                            f[t].AirborneElapsedSamples++;
                        }

                        int gkAgent = GkAgentId(engine, t);
                        if (gkAgent >= 0)
                        {
                            UnityEngine.Vector2 gkXY = engine.AgentView(gkAgent).Position;
                            float reachRadius = GoalkeeperDiveKinematics.ComputeReachRadius(gk.Attrs[t]);

                            // The reach envelope the orchestrator actually builds: the keeper's XY,
                            // displaced laterally along Y by the dive direction, at hand height.
                            int launch = gk.DiveLaunchFrames[t];
                            int dur = gk.DiveDurationFrames[t];
                            int frame = (int)engine.CurrentTick;
                            float handZ = GoalkeeperDiveKinematics.ComputeHandPathZ(
                                frame, launch, dur, gk.DivePeakHandZ[t]);
                            UnityEngine.Vector3 reachCentre = GoalkeeperDiveKinematics.ComputeReachCenter(
                                new UnityEngine.Vector3(gkXY.x, gkXY.y, 0f),
                                frame, launch, dur, gk.DiveDirectionLateral[t], handZ);

                            float miss = (ballPos - reachCentre).magnitude - reachRadius;
                            if (miss < f[t].BestMissM)
                            {
                                f[t].BestMissM = miss;
                            }
                            f[t].MissSum += miss;
                            f[t].MissSamples++;

                            // Lateral-only shortfall: how far the ball is across the goal mouth from
                            // the envelope centre. Isolates dive DIRECTION from dive REACH.
                            f[t].LateralMissSum += Math.Abs(ballPos.y - reachCentre.y);
                            f[t].DiveDirSum += Math.Abs(gk.DiveDirectionLateral[t]);
                        }
                    }
                }

                // On-target: the ball crossing the goal-line plane inside the frame. Counted for the
                // ATTACKING team, i.e. against the keeper defending that line.
                if (ballPos.x <= 0f && InGoalMouth(ballPos))    f[0].ConcededOnTarget++;
                if (ballPos.x >= MatchEngineConstants.PITCH_LENGTH_M && InGoalMouth(ballPos)) f[1].ConcededOnTarget++;

                if (engine.HomeScore != prevHome) { f[1].GoalsConceded += engine.HomeScore - prevHome; prevHome = engine.HomeScore; }
                if (engine.AwayScore != prevAway) { f[0].GoalsConceded += engine.AwayScore - prevAway; prevAway = engine.AwayScore; }
            }

            report.AppendLine(Inv($"seed 0x{seed:X16}   final score {engine.HomeScore}-{engine.AwayScore}   ")
                            + Inv($"total {engine.HomeScore + engine.AwayScore} goals"));
            report.AppendLine("  gk | armedTicks | committed | anticipate | diving | airborne | contact | caught | conceded");
            for (int t = 0; t < teams; t++)
            {
                Funnel x = f[t];
                report.AppendLine(
                    Inv($"   {t} | {x.ArmedTicks,10} | {x.Committed,9} | {x.Anticipate,10} | ")
                    + Inv($"{x.Diving,6} | {x.Airborne,8} | {x.Contacts,7} | {x.Caught,6} | {x.GoalsConceded,8}"));
            }

            // The state histogram: which states a keeper actually occupies over a full match.
            for (int t = 0; t < teams; t++)
            {
                var line = new StringBuilder(Inv($"  gk {t} state ticks:"));
                for (int s = 0; s < f[t].StateTicks.Length; s++)
                {
                    if (f[t].StateTicks[s] > 0)
                    {
                        line.Append(Inv($"  {(GoalkeeperState)s}={f[t].StateTicks[s]}"));
                    }
                }
                report.AppendLine(line.ToString());
            }

            // Contact-time quality, the §5.Z.15 "quality" question — reportable only if contacts happened.
            for (int t = 0; t < teams; t++)
            {
                Funnel x = f[t];
                if (x.Contacts > 0)
                {
                    report.AppendLine(Inv($"  gk {t} at contact: meanQuality={x.QualitySum / x.Contacts:F3} ")
                                    + Inv($"meanReactionWindow={x.ReactionSum / x.Contacts:F3} ")
                                    + Inv($"(CatchThreshold={GoalkeeperConstants.CatchThreshold:F2} ParryThreshold={GoalkeeperConstants.ParryThreshold:F2})"));
                }
                else
                {
                    report.AppendLine(Inv($"  gk {t} at contact: NO CONTACTS — 'save quality' is unmeasurable for this keeper."));
                }

                report.AppendLine(
                    Inv($"  gk {t} reaction pipeline: shotsNotified={x.ShotsNotified}  ")
                    + Inv($"meanRequired={(x.ShotsNotified > 0 ? x.RequiredReactionSum / x.ShotsNotified : 0f):F0} ms  ")
                    + Inv($"meanElapsedWhenAirborne={(x.AirborneElapsedSamples > 0 ? x.AirborneElapsedSum / x.AirborneElapsedSamples : 0f):F0} ms"));

                if (x.MissSamples > 0)
                {
                    report.AppendLine(
                        Inv($"  gk {t} dive misses over {x.MissSamples} airborne frames: ")
                        + Inv($"best={x.BestMissM:F2} m short  mean={x.MissSum / x.MissSamples:F2} m  ")
                        + Inv($"meanLateralOffset={x.LateralMissSum / x.MissSamples:F2} m  ")
                        + Inv($"mean|diveDir|={x.DiveDirSum / x.MissSamples:F3}"));
                }
            }
            report.AppendLine();
        }

        // ── The arithmetic ceiling on handling quality ───────────────────────────────────────────

        /// <summary>
        /// Reports the maximum handling quality reachable given the live constants, for a range of
        /// keepers and ball speeds. This is arithmetic, not simulation: it answers "if a keeper DID
        /// reach the ball, could it ever catch it?" — which the funnel above cannot answer while the
        /// contact count is zero, and which decides whether the remaining work is tuning or a fix.
        /// </summary>
        [Test]
        [Category("Calibration")]
        public void GkSaveDiagnostic_ReportsHandlingQualityCeiling()
        {
            RequireEnv();

            var report = new StringBuilder();
            report.AppendLine("=== Handling-quality ceiling: could a keeper that DOES reach the ball catch it? ===");
            report.AppendLine(Inv($"quality = alpha*rawHandling + (1-alpha)*reactionWindowAchieved,  alpha={GoalkeeperConstants.HandlingReactionBlendAlpha:F2}"));
            report.AppendLine(Inv($"CatchThreshold={GoalkeeperConstants.CatchThreshold:F2}  ParryThreshold={GoalkeeperConstants.ParryThreshold:F2}  DeflectThreshold={GoalkeeperConstants.DeflectThreshold:F2}"));
            report.AppendLine();
            report.AppendLine("Best case per keeper: zero noise, perfect contact point (pointQuality=1).");
            report.AppendLine(" handling | ballSpeed | attrFactor | speedFactor | rawHandling | q(rw=0) | q(rw=1) | best band at rw=0");
            report.AppendLine("----------|-----------|------------|-------------|-------------|---------|---------|-------------------");

            int[] handlingRatings = { 8, 12, 16, 20 };
            float[] speeds = { 10f, 20f, 25f, 30f };

            foreach (int h in handlingRatings)
            {
                foreach (float v in speeds)
                {
                    var attrs = new GoalkeeperAgentAttributes { Handling = h, Fatigue = 0f };

                    float attrFactor = GoalkeeperConstants.HandlingBase
                                     + GoalkeeperConstants.HandlingKAttr * attrs.HandlingNorm;
                    float excess = Math.Max(0f, v - GoalkeeperConstants.HandlingBallSpeedRefMps);
                    float speedFactor = Clamp01(1f - GoalkeeperConstants.HandlingKBallSpeed * excess
                                                     / GoalkeeperConstants.HandlingBallSpeedRefMps);
                    float raw = attrFactor * speedFactor;   // pointQuality = 1, noise = 0

                    float a = GoalkeeperConstants.HandlingReactionBlendAlpha;
                    float q0 = Clamp01(a * raw);            // reactionWindowAchieved = 0 (production today)
                    float q1 = Clamp01(a * raw + (1f - a)); // reactionWindowAchieved = 1 (perfect reaction)

                    report.AppendLine(
                        Inv($" {h,8} | {v,9:F0} | {attrFactor,10:F3} | {speedFactor,11:F3} | ")
                        + Inv($"{raw,11:F3} | {q0,7:F3} | {q1,7:F3} | {Band(q0)}"));
                }
            }

            report.AppendLine();
            report.AppendLine("Reading it: the q(rw=0) column is what production can reach today, because");
            report.AppendLine("reactionWindowAchieved is only ever written when _shotDetectedTickMs > 0, and that");
            report.AppendLine("field is set ONLY by GoalkeeperMechanics.OnShotExecutedEvent — which has no");
            report.AppendLine("production caller. If every q(rw=0) is below CatchThreshold, a catch is");
            report.AppendLine("ARITHMETICALLY impossible regardless of keeper quality or dive accuracy.");

            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        private static bool InGoalMouth(UnityEngine.Vector3 p)
        {
            float centreY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            return Math.Abs(p.y - centreY) <= GoalHalfWidthM && p.z <= GoalHeightM;
        }

        private static string Band(float q)
        {
            if (q >= GoalkeeperConstants.CatchThreshold)   return "Caught";
            if (q >= GoalkeeperConstants.ParryThreshold)   return "Parried";
            if (q >= GoalkeeperConstants.DeflectThreshold) return "Deflected";
            return "Spilled/Missed";
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_GK_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_GK_DIAGNOSTIC=1 to run the §5.Z.15 goalkeeper save-pipeline characterisation.");
            }
        }

        /// <summary>
        /// A position-coherent squad on the <c>ConfigureSquads</c> path. Mirrors the league-bootstrap
        /// recipe (a `[GT]` position template so <c>LineupSelector</c> can field a back four) without
        /// referencing season-save, which sits ABOVE match-engine.
        /// </summary>
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

        private sealed class Funnel
        {
            public int ArmedTicks;
            public int Committed;
            public int Anticipate;
            public int Diving;
            public int Airborne;
            public int Contacts;
            public int Caught;
            public int GoalsConceded;
            public int ConcededOnTarget;
            public float QualitySum;
            public float ReactionSum;
            public int ShotsNotified;
            public float LastShotDetectedMs = -1f;
            public float RequiredReactionSum;
            public float AirborneElapsedSum;
            public int AirborneElapsedSamples;
            public float BestMissM = float.MaxValue;
            public float MissSum;
            public int MissSamples;
            public float LateralMissSum;
            public float DiveDirSum;
            public readonly int[] StateTicks = new int[Enum.GetValues(typeof(GoalkeeperState)).Length];
        }

        /// <summary>The agent index of the given team's goalkeeper, or -1. Scans rather than reading a
        /// private roster so the instrument depends only on the public observation surface.</summary>
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

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. The measurement gap §5.Z.15 left open: no instrument in   |
// |         |            |        | the tree reported whether the goalkeeper makes a save at all, so   |
// |         |            |        | "save quality" was an untested premise. Reports the save pipeline  |
// |         |            |        | as a FUNNEL (armed -> committed -> Anticipate -> Diving ->         |
// |         |            |        | Airborne -> contact -> caught) so the stage that collapses         |
// |         |            |        | localises the defect, plus the per-state tick histogram and the    |
// |         |            |        | contact-time quality/reaction distributions. A second test reports |
// |         |            |        | the arithmetic ceiling on handling quality, which decides whether  |
// |         |            |        | the remaining work is [GT] tuning or a correctness fix. Asserts    |
// |         |            |        | nothing (the ERR-030-014 lesson).                                  |
#endregion
