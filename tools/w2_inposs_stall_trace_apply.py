#!/usr/bin/env python3
"""Patch the InPoss scenario into a measurement-only stall trace for seed 0F1E.

The trace samples every tactical cadence (0.1 s), not only final-third points, so
reported streak durations are actual contiguous match time. No production behavior
is changed.
"""
from pathlib import Path
import sys

repo = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
p = repo / "src/match-engine/tests/MatchEngineInPossGateScenarios.cs"
s = p.read_text(encoding="utf-8")

start = s.index("        private static void RunInPossGate(ScenarioContext context)\n")
end = s.index("        /// <summary>A committed phase", start)

replacement = r'''        private static void RunInPossGate(ScenarioContext context)
        {
            const ulong seed = InPossGateSeed;
            const int LongRunThresholdSamples = 300; // 30.0 s at 0.1 s cadence
            const int PeriodicSamples = 600;         // report each additional 60.0 s

            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int currentNoSignal = 0;
            int maxNoSignal = 0;
            int runStartTick = -1;
            int longRuns = 0;
            int firstLongStartTick = -1;
            int firstLongEndTick = -1;

            string f3(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
            string StateLine(string kind, int tick, int streak)
            {
                UnityEngine.Vector3 pos = engine.BallView.Position;
                UnityEngine.Vector3 vel = engine.BallView.Velocity;
                return "W6_STALL_TRACE kind=" + kind
                    + " tick=" + tick.ToString(CultureInfo.InvariantCulture)
                    + " seconds=" + (tick / 60.0).ToString("F1", CultureInfo.InvariantCulture)
                    + " streakSamples=" + streak.ToString(CultureInfo.InvariantCulture)
                    + " holder=" + engine.PossessingAgentId.ToString(CultureInfo.InvariantCulture)
                    + " passReceiver=" + engine.TestOnly_PassInFlightReceiverId.ToString(CultureInfo.InvariantCulture)
                    + " ballState=" + engine.BallView.State.ToString()
                    + " ballPos=(" + f3(pos.x) + "," + f3(pos.y) + "," + f3(pos.z) + ")"
                    + " ballSpeed=" + f3(vel.magnitude)
                    + " phase0=" + engine.TestOnly_PositioningPhase(0).ToString()
                    + " phase1=" + engine.TestOnly_PositioningPhase(1).ToString();
            }

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();
                if (tick % SampleStrideTicks != 0) continue;

                bool signal = engine.PossessingAgentId >= 0 || engine.TestOnly_PassInFlightReceiverId >= 0;
                if (signal)
                {
                    if (currentNoSignal >= LongRunThresholdSamples)
                    {
                        longRuns++;
                        if (firstLongEndTick < 0) firstLongEndTick = tick;
                        Console.WriteLine(StateLine("END", tick, currentNoSignal));
                    }
                    currentNoSignal = 0;
                    runStartTick = -1;
                    continue;
                }

                if (currentNoSignal == 0)
                {
                    runStartTick = tick;
                }
                currentNoSignal++;
                if (currentNoSignal > maxNoSignal) maxNoSignal = currentNoSignal;

                if (currentNoSignal == LongRunThresholdSamples)
                {
                    if (firstLongStartTick < 0) firstLongStartTick = runStartTick;
                    Console.WriteLine(StateLine("THRESHOLD", tick, currentNoSignal));
                }
                else if (currentNoSignal > LongRunThresholdSamples
                    && (currentNoSignal - LongRunThresholdSamples) % PeriodicSamples == 0)
                {
                    Console.WriteLine(StateLine("PERIODIC", tick, currentNoSignal));
                }
            }

            if (currentNoSignal >= LongRunThresholdSamples)
            {
                longRuns++;
                if (firstLongEndTick < 0) firstLongEndTick = NumTicks - 1;
                Console.WriteLine(StateLine("MATCH_END", NumTicks - 1, currentNoSignal));
            }

            Console.WriteLine(
                "W6_STALL_TRACE_SUMMARY seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                + " longRuns=" + longRuns.ToString(CultureInfo.InvariantCulture)
                + " maxNoSignalSamples=" + maxNoSignal.ToString(CultureInfo.InvariantCulture)
                + " firstLongStartTick=" + firstLongStartTick.ToString(CultureInfo.InvariantCulture)
                + " firstLongEndTick=" + firstLongEndTick.ToString(CultureInfo.InvariantCulture)
                + " firstLongElapsedSeconds="
                + (firstLongStartTick >= 0 && firstLongEndTick >= firstLongStartTick
                    ? ((firstLongEndTick - firstLongStartTick) / 60.0).ToString("F1", CultureInfo.InvariantCulture)
                    : "n/a"));

            context.Envelope.CheckTrue("stall-trace-completes", true, "measurement-only trace");
        }

'''

s = s[:start] + replacement + s[end:]
p.write_text(s, encoding="utf-8", newline="\n")
