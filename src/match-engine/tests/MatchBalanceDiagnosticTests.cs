// File:     src/match-engine/tests/MatchBalanceDiagnosticTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z; path-to-playable
//           roadmap A4a; Code Standards #20
// Purpose:  Characterise the scoreline asymmetry the KD-8 Step 0 pilot exposed. Step 0 (20 full
//           90-minute matches, run 2026-07-26 after the §5.Z.9 balance pass) found:
//
//              strong-at-home:  home 15-39 goals, away 0 — every match
//              strong-away:     home  0-4  goals, away 0 — every match
//
//           The AWAY TEAM NEVER SCORES, in either configuration, and the home side's totals are an
//           order of magnitude above football. Step 0's own assertion passes (the ramp extremes are
//           distinguishable), so nothing in the tree flags it: this driver exists to localise it.
//
//           It reports, per team, possession share and where the ball actually spends its time, which
//           separates the two candidate causes — "the away team never gets near the goal it attacks"
//           from "it gets there and the goal is not credited".
//
//           Env-gated and assertion-free, for the reason every diagnostic in this project is: pinning
//           measured-but-wrong behaviour would turn a defect into a contract.
//
//             TD_BALANCE_DIAGNOSTIC=1 dotnet test -c Release --filter MatchBalanceDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class MatchBalanceDiagnosticTests
    {
        /// <summary>9 minutes per seed — at the measured home rate that is already several goals.</summary>
        private const int TicksPerSeed = 32400;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
        };

        [Test]
        [Category("Calibration")]
        public void MatchBalanceDiagnostic_ReportsScorelineAsymmetry()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_BALANCE_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_BALANCE_DIAGNOSTIC=1 to run the scoreline-asymmetry characterisation.");
            }

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== scoreline asymmetry characterisation ===");
            report.AppendLine("Team 0 attacks +X (its target goal is at x = 105); team 1 attacks -X (x = 0).");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                var engine = new MatchEngine(seed);

                int ticksTeam0Possess = 0;
                int ticksTeam1Possess = 0;
                int ticksInOwnThird = 0;    // x < 35
                int ticksInMiddle = 0;      // 35 <= x <= 70
                int ticksInFinalThird = 0;  // x > 70
                int ticksNearTeam1Target = 0;  // x < 16.5 — the box team 1 attacks
                int ticksNearTeam0Target = 0;  // x > 88.5 — the box team 0 attacks
                float minBallX = float.MaxValue;
                float maxBallX = float.MinValue;

                for (int t = 0; t < TicksPerSeed; t++)
                {
                    engine.RunTick();

                    int holder = engine.PossessingAgentId;
                    if (holder >= 0)
                    {
                        if (engine.AgentTeamId(holder) == 0)
                        {
                            ticksTeam0Possess++;
                        }
                        else
                        {
                            ticksTeam1Possess++;
                        }
                    }

                    float x = engine.BallView.Position.x;
                    if (x < minBallX) minBallX = x;
                    if (x > maxBallX) maxBallX = x;

                    if (x < 35f) ticksInOwnThird++;
                    else if (x > 70f) ticksInFinalThird++;
                    else ticksInMiddle++;

                    if (x < 16.5f) ticksNearTeam1Target++;
                    if (x > 88.5f) ticksNearTeam0Target++;
                }

                report.AppendLine(Inv($"seed 0x{seed:X16}"));
                report.AppendLine(Inv($"  score            : {engine.HomeScore} - {engine.AwayScore}"));
                report.AppendLine(
                    Inv($"  possession ticks : team0 {ticksTeam0Possess} ")
                    + Inv($"({Pct(ticksTeam0Possess)}%)  team1 {ticksTeam1Possess} ")
                    + Inv($"({Pct(ticksTeam1Possess)}%)"));
                report.AppendLine(
                    Inv($"  ball x thirds    : x<35 {Pct(ticksInOwnThird)}%  ")
                    + Inv($"35-70 {Pct(ticksInMiddle)}%  x>70 {Pct(ticksInFinalThird)}%"));
                report.AppendLine(
                    Inv($"  ball in the boxes: team1's target (x<16.5) {Pct(ticksNearTeam1Target)}%  ")
                    + Inv($"team0's target (x>88.5) {Pct(ticksNearTeam0Target)}%"));
                report.AppendLine(Inv($"  ball x range     : {minBallX:F1} .. {maxBallX:F1}"));
                report.AppendLine();
            }

            report.AppendLine("Reading it: comparable possession with the ball only ever in team 0's target");
            report.AppendLine("box means both teams attack the SAME goal (a direction defect). Lopsided");
            report.AppendLine("possession means team 1 never gets to attack at all (a different defect).");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static string Pct(int ticks) =>
            (100f * ticks / TicksPerSeed).ToString("F1", CultureInfo.InvariantCulture);

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial: localises the Step 0 scoreline asymmetry (away team never |
// |         |            |        | scores; home totals an order of magnitude above football) by       |
// |         |            |        | reporting per-team possession share against where the ball spends  |
// |         |            |        | its time. Asserts nothing.                                        |
#endregion
