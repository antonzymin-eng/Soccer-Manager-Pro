// File:     src/match-engine/tests/GkRushDiagnosticTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.7, Testing Strategy #19 (instrument class),
//           keeper rush trigger design supplement (docs/tracking/gk-rush-trigger-design.md) §6
// Purpose:  Env-gated (TD_GK_DIAGNOSTIC=1) instrument for wiring backlog W1 — the keeper rush.
//           Reports, per keeper over full 90-minute matches: rushes committed, rushes launched, how
//           each rush chain ended, ticks spent off the line, and how far off it the keeper got.
//           Asserts nothing (the ERR-030-014 convention) — a diagnostic must not turn a defect into
//           a contract.
//
//           The baseline it is measured against is exactly zero: before W1,
//           GoalkeeperMechanics.CommitRushIntent had no production caller, so every keeper in every
//           match this engine has ever played stayed on his line. A run at the pre-W1 commit
//           therefore reports commits=0 across the corpus by construction.
//
//           Run:
//             TD_GK_DIAGNOSTIC=1 dotnet test -c Release --filter GkRushDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Per-keeper rush anatomy over full matches on the <c>ConfigureSquads</c> path (the
    /// §5.Z.20 measurement population). See file header.</summary>
    [TestFixture]
    internal class GkRushDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        /// <summary>The §5.Z.20 seeds, so pre/post numbers are same-population comparable with every
        /// other goalkeeper instrument in this tree.</summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        [Test]
        [Category("Calibration")]
        public void GkRushDiagnostic_ReportsRushAnatomy()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== Keeper rush anatomy (wiring backlog W1) ===");
            report.AppendLine(
                Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}  ")
                + Inv($"commitBase={GoalkeeperConstants.RushCommitBaseM:F1} m  ")
                + Inv($"commitK1v1={GoalkeeperConstants.RushCommitKOneVsOne:F1}  ")
                + Inv($"coverCorridor={MatchEngineConstants.GkRushCoverCorridorHalfWidthM:F1} m  ")
                + Inv($"maxIntercept={MatchEngineConstants.GkRushMaxInterceptS:F1} s  ")
                + Inv($"commitment={MatchEngineConstants.GkRushCommitment:F2}  ")
                + Inv($"reachedRadius={GoalkeeperConstants.RushTargetReachedRadiusM:F2} m"));
            report.AppendLine();
            report.AppendLine("commits   : RushIntents committed (engine-wide; the pre-W1 value is 0 by");
            report.AppendLine("            construction — CommitRushIntent had no production caller)");
            report.AppendLine("launches  : rising edges into Rushing (a commit the state machine acted on)");
            report.AppendLine("ended:sm  : rush chains ending in HandsOnBall — the keeper claimed it");
            report.AppendLine("ended:rec : rush chains ending in Recovering — reached, aborted or failed");
            report.AppendLine("offLine   : ticks in Rushing/OneOnOne/Smothered");
            report.AppendLine("maxOut    : furthest the keeper got from his own goal line (m)");
            report.AppendLine("            — compare against the §3.7.0 commit distance the keepers'");
            report.AppendLine("            attributes produce: a maxOut far below it means the trigger is");
            report.AppendLine("            arming only on balls already at the keeper's feet.");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed);
            }

            report.AppendLine("Reading it:");
            report.AppendLine("  * commits > 0 and launches ≈ commits => the trigger fires AND the state");
            report.AppendLine("    machine acts on it. commits >> launches => intents are being committed");
            report.AppendLine("    from states that cannot launch, or disarmed before the next tick.");
            report.AppendLine("  * offLine large with maxOut small => the keeper is committing to targets");
            report.AppendLine("    he is already on top of; the trigger is arming on nothing.");
            report.AppendLine("  * a keeper stuck in Rushing shows as offLine climbing without new launches");
            report.AppendLine("    — that is ERR-011-009 regressing.");

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
            var prevState = new GoalkeeperState[teams];
            for (int t = 0; t < teams; t++)
            {
                agg[t] = new Agg();
                prevState[t] = GoalkeeperState.Resting;
            }

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                for (int t = 0; t < teams; t++)
                {
                    GoalkeeperState state = engine.TestOnly_GkState(t);
                    bool inChain = state == GoalkeeperState.Rushing
                                   || state == GoalkeeperState.OneOnOne
                                   || state == GoalkeeperState.Smothered;
                    bool wasInChain = prevState[t] == GoalkeeperState.Rushing
                                      || prevState[t] == GoalkeeperState.OneOnOne
                                      || prevState[t] == GoalkeeperState.Smothered;

                    if (state == GoalkeeperState.Rushing && prevState[t] != GoalkeeperState.Rushing
                        && !wasInChain)
                    {
                        agg[t].Launches++;
                    }

                    if (inChain)
                    {
                        agg[t].OffLineTicks++;

                        int gkAgent = GkAgentId(engine, t);
                        if (gkAgent >= 0)
                        {
                            float goalX = t == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
                            float outM = Math.Abs(engine.AgentView(gkAgent).Position.x - goalX);
                            if (outM > agg[t].MaxOutM)
                            {
                                agg[t].MaxOutM = outM;
                            }
                        }
                    }
                    else if (wasInChain)
                    {
                        if (state == GoalkeeperState.HandsOnBall)
                        {
                            agg[t].EndedClaimed++;
                        }
                        else
                        {
                            agg[t].EndedRecovering++;
                        }
                    }

                    prevState[t] = state;
                }
            }

            report.AppendLine(
                Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}   ")
                + Inv($"commits={engine.TestOnly_RushCommitCount}"));
            report.AppendLine("  gk | launches | ended:sm | ended:rec | offLine | maxOut");
            for (int t = 0; t < teams; t++)
            {
                Agg x = agg[t];
                report.AppendLine(
                    Inv($"   {t} | {x.Launches,8} | {x.EndedClaimed,8} | {x.EndedRecovering,9} | ")
                    + Inv($"{x.OffLineTicks,7} | {x.MaxOutM,6:F1}"));
            }
            report.AppendLine();
        }

        private sealed class Agg
        {
            public int Launches;
            public int EndedClaimed;
            public int EndedRecovering;
            public int OffLineTicks;
            public float MaxOutM;
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
                Assert.Ignore("Set TD_GK_DIAGNOSTIC=1 to run the keeper rush anatomy instrument.");
            }
        }

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-04 | —      | Initial. Wiring backlog W1: commits / launches / chain endings /  |
// |         |            |        | ticks off the line / furthest displacement, per keeper, over the   |
// |         |            |        | §5.Z.20 seed corpus. Assertion-free.                              |
#endregion
