// File:     src/client-app/tests/S0MovementDiagnosticTests.cs
// Created:  2026-10-27
// Modified: 2026-10-27
// Author:   —
// Spec:     UX detailed plan Gate J (docs/design/ux-detailed-plan.md); P5b real-client acceptance
//           (docs/tracking/p5b-real-client-acceptance.md); Code Standards #20
// Purpose:  Measures how far players move in the S0 demo match, through the same MatchSession and
//           LiveMatchFrame positions the Unity client renders, so a "players barely move" observation
//           can be attributed to the engine or to the client. Env-gated and assertion-free.
//
//             TD_MOVEMENT_DIAGNOSTIC=1 bash tools/dotnet-ci/run-gate.sh --fast \
//               --test-filter "FullyQualifiedName~S0MovementDiagnostic"

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    internal sealed class S0MovementDiagnosticTests
    {
        private const float TICKS_PER_SECOND = 60f;

        // A per-tick step above this is a restart/set-piece reposition, not running (36 m/s ≫ any sprint).
        private const float TELEPORT_STEP_M = 0.6f;

        // Below this speed a player counts as standing (walking is roughly 1–2 m/s).
        private const float STANDING_SPEED_MPS = 0.5f;

        // Window for "how far from where they were N minutes ago".
        private static readonly int[] CheckMinutes = { 1, 5, 15, 30, 45, 60, 90 };

        private static readonly ulong[] ExtraSeeds = { 0x0F1E2D3C4B5A6978UL, 0x00000000D1A6D05EUL };

        [Test]
        [Category("Calibration")]
        [Timeout(7200000)]
        public void S0MovementDiagnostic_ReportsPlayerTravelAndSpread()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_MOVEMENT_DIAGNOSTIC")))
                Assert.Ignore("Set TD_MOVEMENT_DIAGNOSTIC=1 to run the S0 player-movement measurement.");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            MatchSetup approved = S0DemoFixture.CreateApproved().BuildSetup(Mentality.Balanced);
            var report = new StringBuilder();
            report.AppendLine("=== S0 player movement (MatchSession -> LiveMatchFrame.AgentPositions, the client's source) ===");
            report.AppendLine("Pitch 105 x 68 m. Agents 0-10 home (attacks +X), 11-21 away. Distances in metres per full match.");
            Run(report, approved, "approved S0 fixture (seed " + approved.Seed + ")");
            foreach (ulong seed in ExtraSeeds)
            {
                var variant = new MatchSetup(seed, approved.HomeSquad, approved.AwaySquad, approved.HomeTactic,
                    approved.AwayTactic, approved.HomeManagerMode, approved.AwayManagerMode,
                    approved.HomeManagerProfile, approved.AwayManagerProfile, approved.GkHeadingEnabled);
                Run(report, variant, "same squads, seed 0x" + seed.ToString("X", CultureInfo.InvariantCulture));
            }

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void Run(StringBuilder report, MatchSetup setup, string label)
        {
            int n = MatchEngineConstants.SQUAD_SIZE;
            var session = new MatchSession(setup);
            var distance = new float[n];
            var teleports = new int[n];
            var standingTicks = new long[n];
            var observedTicks = new long[n];
            var minX = new float[n];
            var maxX = new float[n];
            var minY = new float[n];
            var maxY = new float[n];
            var sumX = new double[n];
            var sumY = new double[n];
            var sumX2 = new double[n];
            var sumY2 = new double[n];
            var kickoff = new Vector2[n];
            var previous = new Vector2[n];
            var fromKickoff = new float[CheckMinutes.Length, n];
            for (int i = 0; i < n; i++)
            {
                minX[i] = minY[i] = float.MaxValue;
                maxX[i] = maxY[i] = float.MinValue;
            }

            bool first = true;
            try
            {
                LiveMatchFrame frame = default;
                ulong lastTick = (ulong)MatchEngineConstants.MATCH_TICKS_TOTAL;
                while (session.CurrentTick < lastTick && !frame.MatchEnded)
                {
                    frame = session.TickOnce();
                    for (int i = 0; i < n; i++)
                    {
                        if (frame.AgentCues[i].IsSentOff)
                            continue;
                        Vector2 p = frame.AgentPositions[i];
                        if (first)
                        {
                            kickoff[i] = p;
                            previous[i] = p;
                        }
                        float step = (p - previous[i]).magnitude;
                        if (step > TELEPORT_STEP_M)
                            teleports[i]++;
                        else
                        {
                            distance[i] += step;
                            if (step * TICKS_PER_SECOND < STANDING_SPEED_MPS)
                                standingTicks[i]++;
                        }
                        previous[i] = p;
                        observedTicks[i]++;
                        minX[i] = Math.Min(minX[i], p.x);
                        maxX[i] = Math.Max(maxX[i], p.x);
                        minY[i] = Math.Min(minY[i], p.y);
                        maxY[i] = Math.Max(maxY[i], p.y);
                        sumX[i] += p.x;
                        sumY[i] += p.y;
                        sumX2[i] += (double)p.x * p.x;
                        sumY2[i] += (double)p.y * p.y;
                    }
                    first = false;
                    for (int c = 0; c < CheckMinutes.Length; c++)
                    {
                        if (frame.Tick == (ulong)(CheckMinutes[c] * 60 * TICKS_PER_SECOND))
                            for (int i = 0; i < n; i++)
                                fromKickoff[c, i] = (frame.AgentPositions[i] - kickoff[i]).magnitude;
                    }
                }

                report.AppendLine();
                report.AppendLine(Inv($"--- {label}: final tick {frame.Tick}, score {frame.Score.Home}-{frame.Score.Away} ---"));
                report.AppendLine("agent team GK  distance_m  standing%  teleports  x_range      y_range      x_sd  y_sd  kickoff(x,y)");
                double outfieldSum = 0, gkSum = 0, standingSum = 0;
                int outfieldCount = 0, gkCount = 0;
                for (int i = 0; i < n; i++)
                {
                    long t = Math.Max(1, observedTicks[i]);
                    double mx = sumX[i] / t, my = sumY[i] / t;
                    double sx = Math.Sqrt(Math.Max(0, sumX2[i] / t - mx * mx));
                    double sy = Math.Sqrt(Math.Max(0, sumY2[i] / t - my * my));
                    bool gk = frame.AgentCues[i].IsGoalkeeper;
                    double standing = 100.0 * standingTicks[i] / t;
                    report.AppendLine(Inv($"{i,5} {(i < 11 ? "H" : "A"),4} {(gk ? "Y" : "-"),2} {distance[i],10:F0} {standing,9:F1} {teleports[i],10} {minX[i],5:F1}-{maxX[i],5:F1}  {minY[i],5:F1}-{maxY[i],5:F1}  {sx,4:F1}  {sy,4:F1}  ({kickoff[i].x:F1},{kickoff[i].y:F1})"));
                    if (gk) { gkSum += distance[i]; gkCount++; }
                    else { outfieldSum += distance[i]; outfieldCount++; standingSum += standing; }
                }
                report.AppendLine(Inv($"mean outfield distance {outfieldSum / Math.Max(1, outfieldCount):F0} m; mean GK distance {gkSum / Math.Max(1, gkCount):F0} m; mean outfield standing {standingSum / Math.Max(1, outfieldCount):F1}%"));
                report.AppendLine("distance from kickoff spot at minute (outfield mean / max):");
                for (int c = 0; c < CheckMinutes.Length; c++)
                {
                    double sum = 0, max = 0;
                    int count = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (frame.AgentCues[i].IsGoalkeeper) continue;
                        sum += fromKickoff[c, i];
                        max = Math.Max(max, fromKickoff[c, i]);
                        count++;
                    }
                    report.AppendLine(Inv($"  min {CheckMinutes[c],2}: mean {sum / Math.Max(1, count):F1} m, max {max:F1} m"));
                }
            }
            finally
            {
                session.Stop();
            }
        }

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-27 | —      | Initial env-gated S0 player-movement measurement (owner observation: players barely move on the pinned host). |
#endregion
