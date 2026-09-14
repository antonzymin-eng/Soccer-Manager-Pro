#!/usr/bin/env python3
from pathlib import Path
import sys

repo = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
p = repo / "src/match-engine/tests/MatchEngineInPossGateScenarios.cs"
s = p.read_text(encoding="utf-8")

start = s.index("        private static void RunInPossGate(ScenarioContext context)\n")
end = s.index("        private static void PlayOne(\n", start)
new_run = '''        private static void RunInPossGate(ScenarioContext context)
        {
            string inv(int v) => v.ToString(CultureInfo.InvariantCulture);
            string f3(double v) => v.ToString("F3", CultureInfo.InvariantCulture);
            int totalSamples = 0;

            // W6 post-wire evidence only: retain the governed seeds and 0.70 predicate, but evaluate
            // each seed independently so one healthy trajectory cannot mask a stalled trajectory.
            for (int s = 0; s < Seeds.Length; s++)
            {
                int samples = 0;
                int homeViewPossession = 0;
                int awayViewPossession = 0;
                ulong seed = Seeds[s];
                PlayOne(seed, ref samples, ref homeViewPossession, ref awayViewPossession);
                totalSamples += samples;

                float homeShare = samples > 0 ? (float)homeViewPossession / samples : 0f;
                float awayShare = samples > 0 ? (float)awayViewPossession / samples : 0f;
                Console.WriteLine(
                    "W6_ARMED_INPOSS seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                    + " samples=" + inv(samples)
                    + " homeShare=" + f3(homeShare)
                    + " awayShare=" + f3(awayShare));

                string suffix = "-seed-0x" + seed.ToString("X16", CultureInfo.InvariantCulture);
                context.Envelope.CheckTrue("final-third-play-is-somebodys-possession-home-view" + suffix,
                    homeShare > 0.70f,
                    "homeShare=" + f3(homeShare) + " (bound 0.70)");
                context.Envelope.CheckTrue("final-third-play-is-somebodys-possession-away-view" + suffix,
                    awayShare > 0.70f,
                    "awayShare=" + f3(awayShare) + " (bound 0.70)");
            }

            // Preserve the existing corpus-level non-vacuity predicate unchanged.
            context.Envelope.CheckTrue("final-third-phase-samples-are-taken",
                totalSamples >= 20000,
                "samples=" + inv(totalSamples));
        }

'''
s = s[:start] + new_run + s[end:]

old_arm = '''            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            for (int tick = 0; tick < NumTicks; tick++)
'''
new_arm = '''            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));
            // Measurement-only seam: production remains shipped at radius 0.
            engine.TestOnly_ArmTackleChallenge(MatchEngineConstants.LooseBallPickupRadiusM);

            for (int tick = 0; tick < NumTicks; tick++)
'''
if s.count(old_arm) != 1:
    raise SystemExit(f"expected one PlayOne arming anchor, found {s.count(old_arm)}")
s = s.replace(old_arm, new_arm, 1)
p.write_text(s, encoding="utf-8", newline="\n")
