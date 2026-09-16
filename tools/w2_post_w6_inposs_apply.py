#!/usr/bin/env python3
"""Apply the post-W6 per-seed InPoss evidence transform in armed or disarmed mode.

This edits only the checked-out measurement copy of MatchEngineInPossGateScenarios.cs.
Production source is never modified by this driver.
"""
from pathlib import Path
import sys

if len(sys.argv) not in (2, 3):
    raise SystemExit("usage: w2_post_w6_inposs_apply.py <repo> [armed|disarmed]")

repo = Path(sys.argv[1])
mode = sys.argv[2] if len(sys.argv) == 3 else "armed"
if mode not in {"armed", "disarmed"}:
    raise SystemExit(f"unsupported mode: {mode}")

p = repo / "src/match-engine/tests/MatchEngineInPossGateScenarios.cs"
s = p.read_text(encoding="utf-8")

start = s.index("        private static void RunInPossGate(ScenarioContext context)\n")
end = s.index("        private static void PlayOne(\n", start)
label = "W2_POST_W6_ARMED_INPOSS" if mode == "armed" else "W2_POST_W6_DISARMED_INPOSS"
new_run = f'''        private static void RunInPossGate(ScenarioContext context)
        {{
            string inv(int v) => v.ToString(CultureInfo.InvariantCulture);
            string f3(double v) => v.ToString("F3", CultureInfo.InvariantCulture);
            int totalSamples = 0;

            // Post-W6 W2 evidence: retain the governed seeds and strict 0.70 predicate, but evaluate
            // each seed independently so one healthy trajectory cannot mask a stalled trajectory.
            for (int s = 0; s < Seeds.Length; s++)
            {{
                int samples = 0;
                int homeViewPossession = 0;
                int awayViewPossession = 0;
                ulong seed = Seeds[s];
                PlayOne(seed, ref samples, ref homeViewPossession, ref awayViewPossession);
                totalSamples += samples;

                float homeShare = samples > 0 ? (float)homeViewPossession / samples : 0f;
                float awayShare = samples > 0 ? (float)awayViewPossession / samples : 0f;
                Console.WriteLine(
                    "{label} seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
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
            }}

            // Preserve the existing corpus-level non-vacuity predicate unchanged.
            context.Envelope.CheckTrue("final-third-phase-samples-are-taken",
                totalSamples >= 20000,
                "samples=" + inv(totalSamples));
        }}

'''
s = s[:start] + new_run + s[end:]

arm_anchor = '''            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            for (int tick = 0; tick < NumTicks; tick++)
'''
if s.count(arm_anchor) != 1:
    raise SystemExit(f"expected one PlayOne arming anchor, found {s.count(arm_anchor)}")

if mode == "armed":
    replacement = '''            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));
            // Measurement-only seam: production remains shipped at radius 0.
            engine.TestOnly_ArmTackleChallenge(MatchEngineConstants.LooseBallPickupRadiusM);

            for (int tick = 0; tick < NumTicks; tick++)
'''
    s = s.replace(arm_anchor, replacement, 1)
else:
    # Deliberately leave the exact production default untouched. The only transform in disarmed mode
    # is the same per-seed reporting/assertion split used by armed mode.
    if "TestOnly_ArmTackleChallenge" in s:
        raise SystemExit("disarmed source unexpectedly contains a tackle-arm seam")

p.write_text(s, encoding="utf-8", newline="\n")
