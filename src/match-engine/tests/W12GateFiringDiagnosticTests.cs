// File:     src/match-engine/tests/W12GateFiringDiagnosticTests.cs
// Created:  2026-09-13
// Author:   —
// Spec:     Match-engine wiring backlog §1.1 / W12
// Purpose:  Env-gated (TD_W12_GATE_DIAGNOSTIC=1), assertion-free full-match census of the
//           Pressing AI gate chain. Records phase/cooldown/disengage exits, raw trigger firings,
//           debounce commitments, and actually-active press directives so pre/post wiring runs
//           distinguish "producer never fired" from "downstream gate suppressed it".

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class W12GateFiringDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        private static readonly TriggerFlags[] TriggerKinds =
        {
            TriggerFlags.BadTouch,
            TriggerFlags.BackwardPass,
            TriggerFlags.SidelineTrap,
            TriggerFlags.WeakReceiver,
        };

        [Test]
        [Category("Calibration")]
        public void W12GateFiringDiagnostic_ReportsPressingGateAndTriggerRates()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== W12 gate-firing census ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}"));
            report.AppendLine("Each team/heartbeat is counted once. Raw trigger = formula true before debounce;");
            report.AppendLine("committed trigger = debounce live; Active = directive survived downstream gates.");
            report.AppendLine("A phase/cooldown exit occurs before trigger evaluation, so raw/committed are intentionally empty there.");
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

            PressingAITick[] pressing = GetPressingTicks(engine);
            var counts = new[] { new TeamCounts(), new TeamCounts() };
            var lastDiagnosticTick = new[] { -1, -1 };

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
                {
                    PressingTickDiagnostics d = pressing[team].LastDiagnostics;
                    if (d.Exit == PressingGateExit.NotRun || d.TickIndex == lastDiagnosticTick[team])
                    {
                        continue;
                    }

                    lastDiagnosticTick[team] = d.TickIndex;
                    counts[team].Observe(in d);
                }
            }

            report.AppendLine(Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}"));
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                TeamCounts c = counts[team];
                report.AppendLine(Inv($"  team {team}: samples={c.Samples} latestPass={c.HasLatestPass} active={c.Active} primaryAssigned={c.PrimaryAssigned} coverShadows={c.CoverShadows}"));
                report.Append("    phase:");
                foreach (KeyValuePair<Phase, long> pair in c.Phases)
                {
                    report.Append(Inv($" {pair.Key}={pair.Value}"));
                }
                report.AppendLine();

                report.Append("    exits:");
                foreach (KeyValuePair<PressingGateExit, long> pair in c.Exits)
                {
                    report.Append(Inv($" {pair.Key}={pair.Value}"));
                }
                report.AppendLine();

                report.Append("    raw:");
                for (int i = 0; i < TriggerKinds.Length; i++)
                {
                    report.Append(Inv($" {TriggerKinds[i]}={c.Raw[i]}"));
                }
                report.AppendLine();

                report.Append("    committed:");
                for (int i = 0; i < TriggerKinds.Length; i++)
                {
                    report.Append(Inv($" {TriggerKinds[i]}={c.Committed[i]}"));
                }
                report.AppendLine();
            }
            report.AppendLine();
        }

        private static PressingAITick[] GetPressingTicks(MatchEngine engine)
        {
            FieldInfo field = typeof(MatchEngine).GetField(
                "_pressing", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "W12 requires the MatchEngine-owned PressingAITick array.");

            var value = field.GetValue(engine) as PressingAITick[];
            Assert.IsNotNull(value, "MatchEngine._pressing must remain a PressingAITick array.");
            Assert.AreEqual(MatchEngineConstants.TEAM_COUNT, value.Length);
            return value;
        }

        private sealed class TeamCounts
        {
            public long Samples;
            public long HasLatestPass;
            public long Active;
            public long PrimaryAssigned;
            public long CoverShadows;
            public readonly long[] Raw = new long[TriggerKinds.Length];
            public readonly long[] Committed = new long[TriggerKinds.Length];
            public readonly Dictionary<Phase, long> Phases = new Dictionary<Phase, long>();
            public readonly Dictionary<PressingGateExit, long> Exits = new Dictionary<PressingGateExit, long>();

            public void Observe(in PressingTickDiagnostics d)
            {
                Samples++;
                if (d.HasLatestPass)
                    HasLatestPass++;
                if (d.PressActive)
                    Active++;
                if (d.PrimaryPresserId >= 0)
                    PrimaryAssigned++;
                CoverShadows += d.CoverShadowCount;

                Increment(Phases, d.Phase);
                Increment(Exits, d.Exit);

                for (int i = 0; i < TriggerKinds.Length; i++)
                {
                    TriggerFlags flag = TriggerKinds[i];
                    if ((d.RawTriggers & flag) != 0)
                        Raw[i]++;
                    if ((d.CommittedTriggers & flag) != 0)
                        Committed[i]++;
                }
            }

            private static void Increment<T>(Dictionary<T, long> map, T key)
            {
                map.TryGetValue(key, out long current);
                map[key] = current + 1;
            }
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
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_W12_GATE_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_W12_GATE_DIAGNOSTIC=1 to run the W12 gate-firing census.");
            }
        }

        private static string Inv(FormattableString value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-13 | —      | Initial W12 full-match gate/trigger census over the canonical three-seed corpus. |
#endregion
