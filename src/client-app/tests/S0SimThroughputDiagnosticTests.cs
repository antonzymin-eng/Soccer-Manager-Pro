// File:     src/client-app/tests/S0SimThroughputDiagnosticTests.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     P5b real-client acceptance Gate-J J-05 (docs/tracking/p5b-real-client-acceptance.md §3);
//           interactive Unity client design §6.4 (playback speed); Code Standards #20
// Purpose:  Host-free throughput benchmark for the S0 demo match, the measurement half of Gate-J J-05
//           ("make 10× really 10×"). The 10× rung asks the streamer's pacing thread for 600 ticks/s;
//           on the pinned host it delivered 315–340 because the sim thread could not keep up.
//
//           Unpaced: runs the S0 demo (S0DemoFixture, Balanced, the coordinator's exact composition)
//           as three sessions over the same seeded match — engine RunTick only; MatchSession.TickOnce
//           without analytics; and the production composition with SessionMatchAnalytics attached —
//           and reports ticks/s (wall and process-CPU), per-tick cost, the split engine vs frame
//           publication (+ command drain) vs analytics observer, and per-tick allocation.
//           Paced: starts the production composition on the streamer's background pacing thread at
//           the ladder's 1× and 10× rungs and reports the tick rate it actually delivers.
//
//           Env-gated and assertion-free like every diagnostic here: it measures a host, and host
//           timings are not a contract. Wall-clock rates on a busy host swing widely between windows,
//           so the report gives the median and best window and a process-CPU rate alongside.
//
//             TD_SIM_THROUGHPUT_DIAGNOSTIC=1 dotnet test -c Release --filter S0SimThroughput
//
//           Optional: TD_SIM_THROUGHPUT_TICKS (measured ticks per variant, default 18000),
//           TD_SIM_THROUGHPUT_WARMUP (default 3600), TD_SIM_THROUGHPUT_PACED_SECONDS (default 10).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    internal sealed class S0SimThroughputDiagnosticTests
    {
        private const string EnvSwitch = "TD_SIM_THROUGHPUT_DIAGNOSTIC";
        private const int WindowTicks = 1800; // 30 simulated seconds per timing window

        private enum Variant { EngineOnly, SessionWithoutAnalytics, Production }

        private struct VariantResult
        {
            public double WallSeconds;
            public double CpuSeconds;
            public long AllocatedBytes;
            public long LogMessages;
            public List<double> WindowRates;
        }

        [Test]
        [Category("Diagnostic")]
        public void S0SimThroughput_Unpaced_ReportsTicksPerSecondAndCostSplit()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            int warmup = EnvInt("TD_SIM_THROUGHPUT_WARMUP", 3600);
            int ticks = EnvInt("TD_SIM_THROUGHPUT_TICKS", 18000);

            var results = new Dictionary<Variant, VariantResult>();
            foreach (Variant variant in new[] { Variant.EngineOnly, Variant.SessionWithoutAnalytics, Variant.Production })
            {
                results[variant] = Measure(variant, warmup, ticks);
            }

            double engineUs = PerTickMicros(results[Variant.EngineOnly], ticks);
            double sessionUs = PerTickMicros(results[Variant.SessionWithoutAnalytics], ticks);
            double productionUs = PerTickMicros(results[Variant.Production], ticks);

            var report = new StringBuilder();
            report.AppendLine(F("S0 sim throughput (unpaced) — {0} measured ticks after {1} warm-up ticks, runtime {2}, {3} logical CPUs",
                ticks, warmup, Environment.Version, Environment.ProcessorCount));
            foreach (KeyValuePair<Variant, VariantResult> row in results)
            {
                VariantResult r = row.Value;
                r.WindowRates.Sort();
                report.AppendLine(F("  {0,-24} {1,7:F0} ticks/s wall  (median window {2,5:F0}, best {3,5:F0})  {4,7:F0} ticks/CPU-s  {5,7:F1} us/tick  {6,7} B/tick  {7,5:F1} logs/1k ticks",
                    row.Key, ticks / r.WallSeconds, Median(r.WindowRates), r.WindowRates[r.WindowRates.Count - 1],
                    ticks / r.CpuSeconds, PerTickMicros(r, ticks), r.AllocatedBytes / ticks, 1000.0 * r.LogMessages / ticks));
            }
            double engineCpuUs = PerTickCpuMicros(results[Variant.EngineOnly], ticks);
            double sessionCpuUs = PerTickCpuMicros(results[Variant.SessionWithoutAnalytics], ticks);
            double productionCpuUs = PerTickCpuMicros(results[Variant.Production], ticks);
            report.AppendLine(F("  split (process CPU, by difference): engine RunTick {0:F1} us | frame publication + drain {1:F1} us | analytics observer {2:F1} us",
                engineCpuUs, sessionCpuUs - engineCpuUs, productionCpuUs - sessionCpuUs));
            report.AppendLine(F("  split (wall, by difference): engine RunTick {0:F1} us | frame publication + drain {1:F1} us | analytics observer {2:F1} us",
                engineUs, sessionUs - engineUs, productionUs - sessionUs));
            report.AppendLine("  (wall > CPU means the process waited for a core: on a busy host read the CPU rates; a negative split is noise)");
            report.AppendLine(F("  10x rung needs 600 ticks/s = {0:F1} us/tick of sim-thread budget; production composition uses {1:F1} us/tick wall ({2:P0}), {3:F1} us/tick CPU ({4:P0})",
                1e6 / 600.0, productionUs, productionUs / (1e6 / 600.0), productionCpuUs, productionCpuUs / (1e6 / 600.0)));
            TestContext.WriteLine(report.ToString());
        }

        [Test]
        [Category("Diagnostic")]
        public void S0SimThroughput_Paced_ReportsDeliveredTicksPerSecondAtLadderRungs()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            int seconds = EnvInt("TD_SIM_THROUGHPUT_PACED_SECONDS", 10);

            S0DemoFixture fixture = S0DemoFixture.CreateApproved();
            var session = new MatchSession(fixture.BuildSetup(Mentality.Balanced));
            fixture.Bind(session);
            SessionMatchAnalytics.Attach(session);

            var report = new StringBuilder();
            report.AppendLine(F("S0 sim throughput (paced, production composition on the streamer's pacing thread), runtime {0}", Environment.Version));
            TextWriter realOut = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                session.Start();
                foreach (int rung in new[] { PlaybackSpeedLadder.RealTimeIndex, PlaybackSpeedLadder.FastestIndex })
                {
                    session.Streamer.SetSpeedMultiplier(PlaybackSpeedLadder.MultiplierAt(rung));
                    Thread.Sleep(1000); // settle into the new pacing epoch
                    ulong startTick = LatestTick(session);
                    var wall = Stopwatch.StartNew();
                    Thread.Sleep(seconds * 1000);
                    ulong endTick = LatestTick(session);
                    double delivered = (endTick - startTick) / wall.Elapsed.TotalSeconds;
                    float target = session.Streamer.EffectiveTicksPerSecond;
                    report.AppendLine(F("  {0,4:F0}x rung: target {1,5:F0} ticks/s, delivered {2,6:F1} ticks/s ({3:P1}) over {4:F1} s",
                        PlaybackSpeedLadder.MultiplierAt(rung), target, delivered, delivered / target, wall.Elapsed.TotalSeconds));
                }
            }
            finally
            {
                session.Stop();
                Console.SetOut(realOut);
            }
            TestContext.WriteLine(report.ToString());
        }

        private static VariantResult Measure(Variant variant, int warmup, int ticks)
        {
            S0DemoFixture fixture = S0DemoFixture.CreateApproved();
            var session = new MatchSession(fixture.BuildSetup(Mentality.Balanced));
            fixture.Bind(session);
            if (variant == Variant.Production)
            {
                SessionMatchAnalytics.Attach(session);
            }
            MatchEngine.MatchEngine engine = session.TestOnly_Engine;

            long logMessages = 0;
#if !UNITY_5_3_OR_NEWER
            // Development-build diagnostics log through Debug.*; the shim prints them, which would time
            // the console rather than the sim. Silence the console and count them instead.
            Action<UnityEngine.LogType, string> countLog = (type, message) => logMessages++;
            UnityEngine.ShimLog.MessageLogged += countLog;
#endif
            TextWriter realOut = Console.Out;
            Console.SetOut(TextWriter.Null);
            var result = new VariantResult { WindowRates = new List<double>() };
            try
            {
                for (int i = 0; i < warmup; i++) { Advance(variant, session, engine); }
                logMessages = 0;

                Process process = Process.GetCurrentProcess();
                process.Refresh();
                TimeSpan cpuBefore = process.TotalProcessorTime;
                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                var wall = Stopwatch.StartNew();
                var window = new Stopwatch();
                for (int done = 0; done < ticks; )
                {
                    int n = Math.Min(WindowTicks, ticks - done);
                    window.Restart();
                    for (int i = 0; i < n; i++) { Advance(variant, session, engine); }
                    result.WindowRates.Add(n / window.Elapsed.TotalSeconds);
                    done += n;
                }
                result.WallSeconds = wall.Elapsed.TotalSeconds;
                result.AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocBefore;
                process.Refresh();
                result.CpuSeconds = (process.TotalProcessorTime - cpuBefore).TotalSeconds;
            }
            finally
            {
                Console.SetOut(realOut);
#if !UNITY_5_3_OR_NEWER
                UnityEngine.ShimLog.MessageLogged -= countLog;
#endif
            }
            result.LogMessages = logMessages;
            return result;
        }

        private static void Advance(Variant variant, MatchSession session, MatchEngine.MatchEngine engine)
        {
            if (variant == Variant.EngineOnly) { engine.RunTick(); }
            else { session.TickOnce(); }
        }

        private static ulong LatestTick(MatchSession session) =>
            session.TryGetLatestFrame(out LiveMatchFrame frame) ? frame.Tick : 0UL;

        private static double PerTickMicros(VariantResult r, int ticks) => r.WallSeconds * 1e6 / ticks;

        private static double PerTickCpuMicros(VariantResult r, int ticks) => r.CpuSeconds * 1e6 / ticks;

        private static double Median(List<double> sorted) =>
            sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : 0.5 * (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]);

        private static int EnvInt(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v > 0
                ? v : fallback;

        private static string F(string format, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, format, args);

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvSwitch)))
            {
                Assert.Ignore("Set " + EnvSwitch + "=1 to run the Gate-J J-05 S0 sim-throughput benchmark.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-21 | —      | Initial creation (Gate-J J-05): unpaced three-variant S0        |
// |         |            |        | throughput with engine / frame / analytics split, allocation    |
// |         |            |        | and log counts, plus paced delivered rate at the 1x and 10x     |
// |         |            |        | ladder rungs.                                                   |
#endregion
