// File:     src/match-engine/tests/TackleIntentDiagnosticTests.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6, Testing Strategy #19 (instrument class),
//           match-engine wiring backlog (docs/tracking/match-engine-wiring-backlog.md) W2 / §1.1
// Purpose:  Env-gated (TD_TACKLE_DIAGNOSTIC=1) instrument for wiring backlog W2 — tackles.
//           Reports, over full 90-minute matches, how many #14 tackle intents the engine produces,
//           how many are COMMIT-mode, and — the number the whole wiring turns on — how many name
//           the agent who ACTUALLY has the ball.
//
//           Why this runs BEFORE the wiring rather than after it. #14's TackleIntentEvaluator emits
//           an intent against each HOLD_SHAPE agent's MARK ASSIGNMENT target whenever that target is
//           inside TackleEligibleRadiusM (3 m); possession is not one of its gates. A tackle can only
//           ever be attempted on the carrier. Nothing in this tree has ever measured how often those
//           two coincide, so "wire the tackle" is, until this runs, a plan resting on an unmeasured
//           premise — which is precisely how C1 landed a real gate whose consumers turned out to be
//           inert (wiring backlog §3 C1, retracted in place). The backlog's own §1.1 asks for exactly
//           this class of firing-rate measurement and books it as W12.
//
//           A zero has to arrive with its cause attached, because three unrelated things produce one
//           and they want opposite fixes: nobody is ever near the carrier (a positioning problem
//           upstream of W2); somebody is, but he is the primary presser, whom #14 §3.2's FR-DA-010
//           rule excludes from the HOLD_SHAPE pool — so the player #13 sends AT the ball is by
//           construction the one who cannot produce tackle intent; or a pool-eligible agent is near
//           him and #14 still declines to COMMIT. Each has its own counter.
//
//           Reported per DEFENDING team and never pooled (ERR-008-002 — three home/away asymmetry
//           defects shipped here because every fixture used the home team), and in POSSESSION
//           EPISODES rather than strides, since a 10 Hz stride count over ~54 000 strides measures
//           the sampling rate and is not comparable with football's per-90 figures.
//
//           Asserts nothing (the ERR-030-014 convention) — pinning a measured-but-wrong number turns
//           a defect into a contract. Acceptance predicates live in scenarios, not here.
//
//           Run:
//             TD_TACKLE_DIAGNOSTIC=1 dotnet test -c Release --filter TackleIntentDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Tackle-intent census over full matches on the <c>ConfigureSquads</c> path (the
    /// §5.Z.20 measurement population). See file header.</summary>
    [TestFixture]
    internal class TackleIntentDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        /// <summary>The §5.Z.20 seeds, so these numbers are same-population comparable with every
        /// other match-engine instrument in this tree.</summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        [Test]
        [Category("Calibration")]
        public void TackleIntentDiagnostic_ReportsHowOftenAnIntentNamesTheCarrier()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== Tackle-intent census (wiring backlog W2) ===");
            report.AppendLine(
                Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}  ")
                + Inv($"eligibleRadius={DefensiveAIConstants.TackleEligibleRadiusM:F1} m  ")
                + Inv($"commitCoverageFloor={DefensiveAIConstants.TackleCommitCoverageFloor}  ")
                + Inv($"jockeyAngle={DefensiveAIConstants.TackleJockeyAngleRad:F2} rad"));
            report.AppendLine();
            report.AppendLine("Counted in POSSESSION EPISODES, not strides: at 10 Hz a match is ~54 000");
            report.AppendLine("strides, so a stride count measures the sampling rate, not the football.");
            report.AppendLine();
            report.AppendLine("episodes : spells in which the OTHER team held the ball (this team defending)");
            report.AppendLine("<=3m     : of those, episodes where any outfielder of this team came within");
            report.AppendLine("           #14's own TackleEligibleRadiusM of the carrier");
            report.AppendLine("<=2m/1.5m: the same, at two tighter bands — the contact radius a wired tackle");
            report.AppendLine("           would use is a design decision this instrument informs, so the");
            report.AppendLine("           bands are reported rather than one being picked here");
            report.AppendLine("poolElig : of the <=3m episodes, those where the nearby man was HOLD_SHAPE-");
            report.AppendLine("           eligible — the only population #14 can produce tackle intent for");
            report.AppendLine("presser  : of the <=3m episodes, those where he carried a #13 press role.");
            report.AppendLine("           FR-DA-010 excludes exactly these from the pool, so an episode here");
            report.AppendLine("           and NOT in poolElig is a challenge the current design forbids");
            report.AppendLine("intent   : episodes with >=1 tackle intent naming the carrier");
            report.AppendLine("COMMIT   : of those, >=1 in Commit mode — the population a wired tackle,");
            report.AppendLine("           gated as W2 proposes, could act on at all");
            report.AppendLine("ballGap  : episodes where the CARRIER was ever >1 m from the ball he holds.");
            report.AppendLine("           Possession is a flag, not a kinematic constraint (backlog W6), so");
            report.AppendLine("           tackler-to-carrier and tackler-to-BALL are different distances");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed);
            }

            report.AppendLine("Reading it — a zero must arrive with its cause attached:");
            report.AppendLine("  * COMMIT == 0 and <=3m ~ 0  => nobody is ever near the man on the ball.");
            report.AppendLine("    W2 is dead UPSTREAM, at positioning; wiring the chain changes nothing.");
            report.AppendLine("  * COMMIT == 0, <=3m large, poolElig ~ 0, presser large => the only player");
            report.AppendLine("    near the carrier is the one #13 sent at him, and FR-DA-010 excludes him");
            report.AppendLine("    from the pool by construction. Then the tackle producer belongs in #13,");
            report.AppendLine("    or #14's pool exclusion is wrong for tackles — a SPEC question, not an");
            report.AppendLine("    engine one, and not something a call site may decide by itself.");
            report.AppendLine("  * COMMIT == 0, poolElig large, intent ~ 0 => MarkAssigner is not marking");
            report.AppendLine("    the carrier. It never reads the ball, so this is the expected shape.");
            report.AppendLine("  * intent large, COMMIT == 0 => the CoverageDepth floor or the last-man");
            report.AppendLine("    override is the bound; both are #14 [GT]s frozen under KD-W1.");
            report.AppendLine("  * ballGap large => calibrate the contact gate on the distance the");
            report.AppendLine("    mechanism will actually use, and make this instrument report that one.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunMatch(StringBuilder report, ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();
            }

            // Bank the episode still open at the final whistle, or the census is short by one.
            engine.TestOnly_FinalizeTackleCensus();

            report.AppendLine(
                Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}"));
            report.AppendLine(
                "  def | episodes | <=3m | <=2m | <=1.5m | poolElig | presser | intent | COMMIT | ballGap");

            // Reported per DEFENDING team and never pooled: the coverage-depth "goal-side" term and
            // LastManDetector.DefendsX0 are team-relative, and three home/away asymmetry defects have
            // shipped in this tree because a fixture only ever used the home team (ERR-008-002).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                TackleIntentCensus c = engine.TestOnly_TackleCensus(t);
                report.AppendLine(
                    Inv($"   {t}  | {c.DefendEpisodes,8} | {c.EpisodesWithin3M,4} | {c.EpisodesWithin2M,4} | ")
                    + Inv($"{c.EpisodesWithin1P5M,6} | {c.EpisodesPoolEligibleWithin3M,8} | ")
                    + Inv($"{c.EpisodesPresserWithin3M,7} | {c.EpisodesIntentOnCarrier,6} | ")
                    + Inv($"{c.EpisodesCommitOnCarrier,6} | {c.EpisodesCarrierBallGapOverThreshold,7}"));
            }

            var oc = engine.TestOnly_TackleOutcomeCounts;
            int resolved = oc.Won + oc.Loose + oc.Foul + oc.Missed;
            int decisive = oc.Won + oc.Loose;
            report.AppendLine(
                Inv($"  CHALLENGES RESOLVED (both teams): {resolved}  ")
                + Inv($"won={oc.Won} loose={oc.Loose} foul={oc.Foul} missed={oc.Missed}"));
            if (resolved > 0)
            {
                report.AppendLine(
                    Inv($"    engage rate {100.0 * (resolved - oc.Missed) / resolved:F1}%  ")
                    + Inv($"of which foul {100.0 * oc.Foul / System.Math.Max(1, resolved - oc.Missed):F1}%  ")
                    + Inv($"clean-win share of decisive {100.0 * oc.Won / System.Math.Max(1, decisive):F1}%"));
                report.AppendLine(
                    Inv($"    per team per 90: challenges {resolved / 2.0:F1}, ")
                    + Inv($"dispossessions {decisive / 2.0:F1}, fouls {oc.Foul / 2.0:F1}"));
            }
            report.AppendLine("  mode split over on-carrier intents (strides, ratio only — NOT a per-90 rate):");
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                TackleIntentCensus c = engine.TestOnly_TackleCensus(t);
                report.AppendLine(
                    Inv($"   {t}  | allIntents={c.StrideIntentsTotal}  onCarrier: ")
                    + Inv($"COMMIT={c.StrideOnCarrierCommit} Jockey={c.StrideOnCarrierJockey} ")
                    + Inv($"Hold={c.StrideOnCarrierHold}"));
            }
            report.AppendLine();
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

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_TACKLE_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_TACKLE_DIAGNOSTIC=1 to run the tackle-intent census instrument.");
            }
        }

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-12 | —      | Initial. Wiring backlog W2: the tackle-intent census — total /     |
// |         |            |        | COMMIT / on-carrier / both / separation bands, over the §5.Z.20    |
// |         |            |        | seed corpus. Runs BEFORE the wiring, because whether a wired       |
// |         |            |        | tackle can fire at all rests on a rate nobody had measured (the    |
// |         |            |        | C1 lesson; backlog §1.1 / W12). Assertion-free.                    |
#endregion
