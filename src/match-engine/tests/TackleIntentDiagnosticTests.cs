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
//           The three separation bands are reported side by side deliberately: #14's 3 m radius is a
//           DECISION radius, a challenge is a contact event, and picking the contact radius is a
//           design decision this instrument exists to inform rather than pre-empt.
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
            report.AppendLine("total      : #14 TackleIntentRequests produced (all modes), counted once");
            report.AppendLine("             per intent per 10 Hz stride");
            report.AppendLine("commit     : of those, Mode == Commit (CoverageDepth >= floor, not last man)");
            report.AppendLine("onCarrier  : intents whose TargetEntityId IS the current ball holder");
            report.AppendLine("cmt+car    : both — the population a wired tackle could act on at all");
            report.AppendLine("<=2m / <=1m: of cmt+car, the tackler-to-carrier separation");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed);
            }

            report.AppendLine("Reading it:");
            report.AppendLine("  * cmt+car == 0 => W2 as specified cannot fire at all: #14 never aims a");
            report.AppendLine("    COMMIT at the man on the ball, and wiring the chain unchanged would");
            report.AppendLine("    land dead code on top of dead code. The gate would then have to widen");
            report.AppendLine("    (any mode, or any nearby opponent) and that is a #14 design question,");
            report.AppendLine("    not an engine one.");
            report.AppendLine("  * cmt+car large but <=1m ~ 0 => the intent exists but never at contact");
            report.AppendLine("    range; the challenge would need a pursuit step, not just a draw.");
            report.AppendLine("  * onCarrier >> cmt+car => the carrier IS being marked but COMMIT is being");
            report.AppendLine("    refused — the CoverageDepth floor or the last-man override is the bound,");
            report.AppendLine("    and both are #14 [GT]s frozen under KD-W1.");

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

            var c = engine.TestOnly_TackleIntentCensus;

            report.AppendLine(
                Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}"));
            report.AppendLine("  total | commit | onCarrier | cmt+car | <=2m | <=1m");
            report.AppendLine(
                Inv($"  {c.Total,5} | {c.Commit,6} | {c.OnCarrier,9} | {c.CommitOnCarrier,7} | ")
                + Inv($"{c.CommitOnCarrierWithin2M,4} | {c.CommitOnCarrierWithin1M,4}"));
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
