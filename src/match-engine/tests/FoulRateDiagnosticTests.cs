// File:     src/match-engine/tests/FoulRateDiagnosticTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.7 item 1 / §5.Z.9;
//           Tactical Instructions #21 §5.6 (the balance-pass precedent); Code Standards #20
// Purpose:  The MEASUREMENT half of the §5.Z.9 foul-rate balance pass. Phase H left the foul heuristic
//           issuing ~7 red cards per 9 minutes — every player on the pitch dismissed inside a full match —
//           and recorded that fixing it needs "a foul-rate target and a measurement pass, not a guess
//           folded into a correctness fix". This driver is that measurement.
//
//           It runs real composed matches with an observer attached to every collision event, records
//           per tick the strongest cross-team FROM_BEHIND contact, and then replays the foul gate
//           OFFLINE over a ladder of (threshold, cooldown) pairs. One match run therefore yields the
//           whole rate-vs-threshold curve rather than one point, which is what makes the pass tractable:
//           the constants are `static readonly` bound at static-init, so an in-process sweep of the
//           real gate is impossible.
//
//           Env-gated: a run is minutes of composed match time across several seeds. It is a
//           DIAGNOSTIC, not a lock — it asserts nothing about the foul rate, because pinning the
//           measured behaviour is the acceptance scenario's job (MatchEngineDisciplineScenarios),
//           not the measuring instrument's.
//
//             TD_FOUL_DIAGNOSTIC=1 dotnet test -c Release --filter FoulRateDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.CollisionSystem;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class FoulRateDiagnosticTests
    {
        /// <summary>
        /// Ticks per seed. 54 000 = 15 minutes of match time at 60 Hz; six seeds therefore total one
        /// full match-equivalent of play. Sample size is the binding constraint here rather than run
        /// length: at the target rate a 90-minute match contains only ~22 fouls, so a shorter corpus
        /// cannot separate a correct rate from a wrong one.
        /// </summary>
        private const int TicksPerSeed = 54000;

        /// <summary>Physics ticks per second (Ball Physics #1 / Deterministic Sim #16: 60 Hz).</summary>
        private const float TicksPerSecond = 60.0f;

        /// <summary>Ticks in a full 90-minute match — the denominator every reported rate scales to.</summary>
        private const int TicksPerMatch = 324000;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x0000000000000001UL,
            0x00000000ABCDEF12UL,
            0x0000000099887766UL,
            0x000000005A5A5A5AUL,
        };

        /// <summary>
        /// The candidate force thresholds to replay the gate against, in newtons. Spans two orders of
        /// magnitude around the shipped 1200 N so the curve's shape — not just its value at one point —
        /// is visible, including whether the distribution has a usable tail at all.
        /// </summary>
        private static readonly float[] ThresholdLadderN =
        {
            600f, 1200f, 2000f, 3000f, 4000f, 5000f, 6000f, 8000f, 10000f, 14000f, 20000f, 30000f, 50000f,
        };

        /// <summary>Candidate global foul-detection cooldowns, in ticks (60 = 1 s, the shipped value).</summary>
        private static readonly int[] CooldownLadderTicks = { 60, 300, 600 };

        [Test]
        [Category("Calibration")]
        public void FoulRateDiagnostic_ReportsRateVersusThreshold()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_FOUL_DIAGNOSTIC")))
            {
                Assert.Ignore(
                    "Set TD_FOUL_DIAGNOSTIC=1 to run the §5.Z.9 foul-rate measurement (several composed matches).");
            }

            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup — an ordinary match event since Phase H,
            // and a frequent one here because fouls award restarts. §5.Z.7 item 3 records that the log
            // LEVEL is the stale part, not the cancel path; the composed runs declare it meanwhile.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== §5.Z.9 foul-rate measurement ===");
            report.AppendLine(
                Invariant($"seeds={Seeds.Length} ticksPerSeed={TicksPerSeed} ")
                + Invariant($"({TicksPerSeed / TicksPerSecond / 60f:F1} match-minutes each)"));
            report.AppendLine();

            // Per-tick peak qualifying force, accumulated across every seed end-to-end. Replaying the
            // gate over the concatenation is exact for every threshold except at the four seed seams,
            // where a cooldown could carry across — negligible against 4 x 32 400 ticks.
            var peakForcePerTick = new float[Seeds.Length * TicksPerSeed];
            int writeCursor = 0;

            int totalObservedFouls = 0;
            int totalObservedYellows = 0;
            int totalObservedReds = 0;
            int totalAgentAgentContacts = 0;
            int totalQualifyingContacts = 0;

            foreach (ulong seed in Seeds)
            {
                var engine = new MatchEngine(seed);
                var probe = new FoulCandidateProbe(engine, TicksPerSeed);
                engine.TestOnly_SetCollisionObserver(probe);

                for (int t = 0; t < TicksPerSeed; t++)
                {
                    probe.BeginTick(t);
                    engine.RunTick();
                    probe.EndTick();
                }

                engine.TestOnly_SetCollisionObserver(null);

                Array.Copy(probe.PeakForcePerTick, 0, peakForcePerTick, writeCursor, TicksPerSeed);
                writeCursor += TicksPerSeed;

                totalAgentAgentContacts += probe.AgentAgentContacts;
                totalQualifyingContacts += probe.QualifyingContacts;

                int yellows = 0;
                int reds = 0;
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    yellows += engine.TestOnly_YellowCards(a);
                    if (engine.TestOnly_IsSentOff(a))
                    {
                        reds++;
                    }
                }

                totalObservedYellows += yellows;
                totalObservedReds += reds;
                totalObservedFouls += probe.FoulsApplied;

                report.AppendLine(
                    Invariant($"seed 0x{seed:X16}: agentAgentContacts={probe.AgentAgentContacts} ")
                    + Invariant($"qualifyingFromBehind={probe.QualifyingContacts} ")
                    + Invariant($"foulsApplied={probe.FoulsApplied} yellows={yellows} sentOff={reds}"));
            }

            report.AppendLine();
            report.AppendLine(
                Invariant($"TOTALS over {Seeds.Length} seeds: contacts={totalAgentAgentContacts} ")
                + Invariant($"qualifying={totalQualifyingContacts} fouls={totalObservedFouls} ")
                + Invariant($"yellows={totalObservedYellows} reds={totalObservedReds}"));
            report.AppendLine(
                Invariant($"SHIPPED per-90-min rates: fouls={PerMatch(totalObservedFouls):F1} ")
                + Invariant($"yellows={PerMatch(totalObservedYellows):F1} ")
                + Invariant($"reds={PerMatch(totalObservedReds):F2}"));
            report.AppendLine();

            report.AppendLine("--- force distribution of cross-team FROM_BEHIND contacts (N) ---");
            report.AppendLine(DescribeDistribution(peakForcePerTick));
            report.AppendLine();

            report.AppendLine("--- fouls per 90 minutes, gate replayed offline ---");
            report.AppendLine("threshold(N)  " + string.Join("  ", Array.ConvertAll(
                CooldownLadderTicks, c => Invariant($"cd={c,4}"))));
            foreach (float threshold in ThresholdLadderN)
            {
                var row = new StringBuilder(Invariant($"{threshold,11:F0}  "));
                foreach (int cooldown in CooldownLadderTicks)
                {
                    int fouls = ReplayGate(peakForcePerTick, threshold, cooldown);
                    row.Append(Invariant($"{PerMatch(fouls),8:F1}  "));
                }
                report.AppendLine(row.ToString());
            }

            report.AppendLine();
            report.AppendLine("--- CANDIDATE MODEL: force-scaled call probability (§5.Z.9) ---");
            report.AppendLine("A qualifying contact is WHISTLED with probability");
            report.AppendLine("  p(F) = min(1, callProbability * F / threshold)");
            report.AppendLine("so the harder the challenge the likelier it is given, but a hard contact is");
            report.AppendLine("never automatically a foul. A no-call arms no cooldown.");
            report.AppendLine("Rates are per 90 minutes.");
            report.AppendLine();
            report.AppendLine("  thr(N)   callP   cd     fouls  yellows   reds");
            foreach (float threshold in new[] { 900f, 1200f, 1500f })
            {
                foreach (float callP in new[] { 0.01f, 0.02f, 0.03f, 0.04f, 0.06f, 0.10f })
                {
                    foreach (int cooldown in new[] { 60, 180 })
                    {
                        ReplayScaled(
                            peakForcePerTick, threshold, callP, cooldown,
                            out int fouls, out int yellows, out int reds);

                        report.AppendLine(
                            Invariant($"  {threshold,6:F0}  {callP,5:F3}  {cooldown,4}  ")
                            + Invariant($"{PerMatch(fouls),7:F1}  {PerMatch(yellows),7:F1}  ")
                            + Invariant($"{PerMatch(reds),6:F2}"));
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("Real-football reference: ~22 fouls, ~3.5 yellows, ~0.25 reds per match.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        /// <summary>
        /// Replays the production foul gate offline: walk ticks in order, and whenever the cooldown has
        /// expired and this tick carried a qualifying contact at or above <paramref name="thresholdN"/>,
        /// count a foul and re-arm the cooldown. This is exactly <c>MatchFlowCollisionConsumer</c> +
        /// <c>ApplyFoulIfCaptured</c>'s temporal behaviour, which is why a plain histogram would
        /// overcount: the 1 s debounce discards most of a burst.
        ///
        /// The approximation: the recorded trajectory was produced under the SHIPPED threshold, so a
        /// different threshold would have awarded different free kicks and the match would have unfolded
        /// differently. It is a starting value, verified afterwards by a real run at the chosen setting.
        /// </summary>
        private static int ReplayGate(float[] peakForcePerTick, float thresholdN, int cooldownTicks)
        {
            int fouls = 0;
            int cooldownRemaining = 0;

            for (int t = 0; t < peakForcePerTick.Length; t++)
            {
                if (cooldownRemaining > 0)
                {
                    cooldownRemaining--;
                    continue;
                }

                if (peakForcePerTick[t] >= thresholdN)
                {
                    fouls++;
                    cooldownRemaining = cooldownTicks;
                }
            }

            return fouls;
        }

        /// <summary>
        /// Replays the CANDIDATE model offline: a qualifying contact is whistled with a force-scaled
        /// probability, a no-call arms no cooldown, and one uniform draw both decides the call and — when
        /// it is a call — selects the card severity from the rescaled remainder (the single-draw
        /// partition the production change uses, so this sweep and the shipped code share their arithmetic).
        /// The local SplitMix64 is a calibration instrument, not the production stream: it exists to make
        /// the sweep repeatable, and the chosen values are verified afterwards by a real composed run.
        /// </summary>
        private static void ReplayScaled(
            float[] peakForcePerTick,
            float thresholdN,
            float callProbability,
            int cooldownTicks,
            out int fouls,
            out int yellows,
            out int reds)
        {
            fouls = 0;
            yellows = 0;
            reds = 0;

            // Candidate card bands for the sweep — the real-football ratios the pass targets
            // (~16% of fouls booked, ~1% dismissed).
            const float redBand = 0.01f;
            const float yellowBand = 0.15f;

            ulong rng = 0x9E3779B97F4A7C15UL;
            int cooldownRemaining = 0;

            for (int t = 0; t < peakForcePerTick.Length; t++)
            {
                if (cooldownRemaining > 0)
                {
                    cooldownRemaining--;
                    continue;
                }

                float force = peakForcePerTick[t];
                if (force < thresholdN)
                {
                    continue;
                }

                float p = Math.Min(1f, callProbability * (force / thresholdN));

                float u = NextUnitFloat(ref rng);
                if (u >= p)
                {
                    continue; // waved on — no whistle, no cooldown, no card.
                }

                fouls++;
                cooldownRemaining = cooldownTicks;

                float v = u / p; // uniform on [0,1) conditional on the call.
                if (v < redBand)
                {
                    reds++;
                }
                else if (v < redBand + yellowBand)
                {
                    yellows++;
                }
            }
        }

        /// <summary>SplitMix64 → [0,1). Calibration-local; see <see cref="ReplayScaled"/>.</summary>
        private static float NextUnitFloat(ref ulong state)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug.
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (z % 1_000_000UL) / 1_000_000f;
            }
        }

        private static string DescribeDistribution(float[] peakForcePerTick)
        {
            int nonZero = 0;
            for (int i = 0; i < peakForcePerTick.Length; i++)
            {
                if (peakForcePerTick[i] > 0f)
                {
                    nonZero++;
                }
            }

            if (nonZero == 0)
            {
                return "  (no cross-team FROM_BEHIND contacts observed)";
            }

            var forces = new float[nonZero];
            int w = 0;
            for (int i = 0; i < peakForcePerTick.Length; i++)
            {
                if (peakForcePerTick[i] > 0f)
                {
                    forces[w++] = peakForcePerTick[i];
                }
            }
            Array.Sort(forces);

            var sb = new StringBuilder();
            sb.AppendLine(
                Invariant($"  ticks with a qualifying contact: {nonZero} of {peakForcePerTick.Length} ")
                + Invariant($"({100f * nonZero / peakForcePerTick.Length:F2}%)"));
            foreach (float q in new[] { 0.50f, 0.75f, 0.90f, 0.95f, 0.99f, 0.999f, 1.0f })
            {
                int idx = Math.Min(forces.Length - 1, (int)(q * (forces.Length - 1)));
                sb.AppendLine(Invariant($"  p{q * 100f,6:F1} = {forces[idx],12:F0} N"));
            }
            return sb.ToString().TrimEnd();
        }

        private static float PerMatch(int countOverRun) =>
            countOverRun * (float)TicksPerMatch / (Seeds.Length * (float)TicksPerSeed);

        private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Records, per tick, the largest <c>ForceMagnitude</c> among cross-team FROM_BEHIND agent-agent
        /// contacts whose participants are both still on the pitch — the exact predicate
        /// <see cref="MatchEngine"/>'s foul consumer + application site jointly apply, minus the force
        /// threshold and the cooldown, which is what the offline replay sweeps.
        /// </summary>
        private sealed class FoulCandidateProbe : ICollisionEventConsumer
        {
            private readonly MatchEngine _engine;
            private int _tick;

            public FoulCandidateProbe(MatchEngine engine, int tickCapacity)
            {
                _engine = engine;
                PeakForcePerTick = new float[tickCapacity];
            }

            public float[] PeakForcePerTick { get; }
            public int AgentAgentContacts { get; private set; }
            public int QualifyingContacts { get; private set; }

            /// <summary>Fouls the production gate actually applied, inferred from the cooldown re-arming.</summary>
            public int FoulsApplied { get; private set; }

            private int _lastCooldownSeen;

            public void BeginTick(int tick) => _tick = tick;

            /// <summary>
            /// Counts applied fouls. The cooldown is re-armed only by <c>ApplyFoulIfCaptured</c> and
            /// decremented by one per tick, so an INCREASE across a tick is exactly one applied foul.
            /// Sampled AFTER the tick, not before: sampling first would miss a foul given on the run's
            /// final tick, and the acceptance scenario counts the same way — a measuring instrument that
            /// disagrees with the thing it calibrates is worse than no instrument.
            /// </summary>
            public void EndTick()
            {
                int cooldown = _engine.TestOnly_FoulCooldownRemaining;
                if (cooldown > _lastCooldownSeen)
                {
                    FoulsApplied++;
                }
                _lastCooldownSeen = cooldown;
            }

            public void OnCollisionEvent(in CollisionEvent evt)
            {
                if (evt.Type != CollisionType.AGENT_AGENT)
                {
                    return;
                }

                AgentAgentContacts++;

                ContactForceData foul = evt.FoulData;
                if (foul.Type != ContactType.FROM_BEHIND)
                {
                    return;
                }
                if (_engine.AgentTeamId(foul.InstigatorAgentID) == _engine.AgentTeamId(foul.VictimAgentID))
                {
                    return;
                }
                if (_engine.TestOnly_IsSentOff(foul.InstigatorAgentID)
                    || _engine.TestOnly_IsSentOff(foul.VictimAgentID))
                {
                    return;
                }

                QualifyingContacts++;

                if (_tick >= 0 && _tick < PeakForcePerTick.Length
                    && foul.ForceMagnitude > PeakForcePerTick[_tick])
                {
                    PeakForcePerTick[_tick] = foul.ForceMagnitude;
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-07-26 | —      | Initial: the §5.Z.9 foul-rate measurement driver. Env-gated, asserts    |
// |         |            |        | nothing about the rate — it measures the force distribution and replays |
// |         |            |        | the gate offline across a (threshold, cooldown) ladder so one composed  |
// |         |            |        | run yields the whole curve.                                             |
#endregion
