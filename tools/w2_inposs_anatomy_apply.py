#!/usr/bin/env python3
"""Patch the InPoss scenario into a measurement-only anatomy probe for the failing governed seed."""
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
            int samples = 0;
            int homePhasePoss = 0;
            int awayPhasePoss = 0;
            int holderSamples = 0;
            int passSamples = 0;
            int anySignalSamples = 0;
            int neitherSignalSamples = 0;
            int signalButTransitionSamples = 0;
            int noSignalButPossessionPhaseSamples = 0;
            int currentNoSignalRun = 0;
            int maxNoSignalRun = 0;
            int currentTransitionRun = 0;
            int maxTransitionRun = 0;

            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();
                if (tick % SampleStrideTicks != 0) continue;

                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                float depthHome = MatchEngineConstants.PITCH_LENGTH_M - ballPos.x;
                float depthAway = ballPos.x;
                if (depthHome > FinalThirdDepthM && depthAway > FinalThirdDepthM) continue;

                samples++;
                bool homePoss = IsPossessionPhase(engine.TestOnly_PositioningPhase(0));
                bool awayPoss = IsPossessionPhase(engine.TestOnly_PositioningPhase(1));
                if (homePoss) homePhasePoss++;
                if (awayPoss) awayPhasePoss++;

                bool holder = engine.PossessingAgentId >= 0;
                bool pass = engine.TestOnly_PassInFlightReceiverId >= 0;
                bool signal = holder || pass;
                if (holder) holderSamples++;
                if (pass) passSamples++;
                if (signal) anySignalSamples++;
                else neitherSignalSamples++;

                bool possessionPhase = homePoss && awayPoss;
                if (signal && !possessionPhase) signalButTransitionSamples++;
                if (!signal && possessionPhase) noSignalButPossessionPhaseSamples++;

                if (signal)
                {
                    currentNoSignalRun = 0;
                }
                else
                {
                    currentNoSignalRun++;
                    if (currentNoSignalRun > maxNoSignalRun) maxNoSignalRun = currentNoSignalRun;
                }

                if (possessionPhase)
                {
                    currentTransitionRun = 0;
                }
                else
                {
                    currentTransitionRun++;
                    if (currentTransitionRun > maxTransitionRun) maxTransitionRun = currentTransitionRun;
                }
            }

            string inv(int v) => v.ToString(CultureInfo.InvariantCulture);
            string f3(double v) => v.ToString("F3", CultureInfo.InvariantCulture);
            double denom = samples > 0 ? samples : 1;
            Console.WriteLine(
                "W2_INPOSS_ANATOMY seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                + " samples=" + inv(samples)
                + " homePhasePoss=" + f3(homePhasePoss / denom)
                + " awayPhasePoss=" + f3(awayPhasePoss / denom)
                + " holder=" + f3(holderSamples / denom)
                + " pass=" + f3(passSamples / denom)
                + " anySignal=" + f3(anySignalSamples / denom)
                + " neitherSignal=" + f3(neitherSignalSamples / denom)
                + " signalButTransition=" + f3(signalButTransitionSamples / denom)
                + " noSignalButPossessionPhase=" + f3(noSignalButPossessionPhaseSamples / denom)
                + " maxNoSignalSamples=" + inv(maxNoSignalRun)
                + " maxTransitionSamples=" + inv(maxTransitionRun));

            context.Envelope.CheckTrue("anatomy-final-third-samples-are-taken",
                samples >= 10000,
                "samples=" + inv(samples));
        }

'''

s = s[:start] + replacement + s[end:]
p.write_text(s, encoding="utf-8", newline="\n")
