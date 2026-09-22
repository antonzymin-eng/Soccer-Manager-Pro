// File:     src/match-engine/tests/FoulRateDiagnosticTests.cs
// Created:  2026-07-26
// Modified: 2026-09-21 (#435 §2.1 source-complete foul/card measurement instrument; measurement-only)
// Author:   —
// Spec:     foul-card-w3-w9-preregistration.md §2.1 / §3;
//           Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.7 item 1 / §5.Z.9;
//           Tactical Instructions #21 §5.6 (the balance-pass precedent); Code Standards #20
// Purpose:  Source-complete foul/card measurement required by #435 §2.1. Runs the frozen six
//           full-match seeds and reports both live discipline sources (collision FROM_BEHIND and
//           already-adjudicated W2 SLIDE_TACKLE), their cooldown/single-slot interaction, exact
//           applied foul/card events, and the qualifying collision-force distribution.
//
//           The historical offline (threshold, cooldown) collision replay is retained as descriptive
//           bracketing only; it is not the live source-complete numerator and does not propose a [GT].
//
//           Env-gated because the corpus is six composed 90-minute matches. It is a DIAGNOSTIC, not
//           a rate lock: only taxonomy/identity reconciliation is asserted; measured rates are not.
//
//             TD_FOUL_DIAGNOSTIC=1 dotnet test -c Release --filter FoulRateDiagnostic

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.CollisionSystem;
using TacticalDirector.EventSystem;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class FoulRateDiagnosticTests
    {
        /// <summary>
        /// Frozen #435 §3 corpus: every seed runs one complete 90-minute match at 60 Hz. Six seeds
        /// therefore produce six match-equivalents; no seed is added, removed, or shortened after
        /// results are observed.
        /// </summary>
        private const int TicksPerSeed = 324000;

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

        /// <summary>
        /// Descriptive cooldown replay ladder. #435 §3 requires the live production value (180) beside
        /// the historical 60/300/600 points; this is not a search over candidate gameplay constants.
        /// </summary>
        private static readonly int[] CooldownLadderTicks = { 60, 180, 300, 600 };

        private static readonly byte FoulCommittedOrdinal = EventRegistry.GetOrdinal<FoulCommittedEvent>();
        private static readonly byte CardIssuedOrdinal = EventRegistry.GetOrdinal<CardIssuedEvent>();

        [Test]
        [Category("Calibration")]
        public void FoulRateDiagnostic_ReportsRateVersusThreshold()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_FOUL_DIAGNOSTIC")))
            {
                Assert.Ignore(
                    "Set TD_FOUL_DIAGNOSTIC=1 to run the #435 §2.1 source-complete foul/card measurement.");
            }

            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup — an ordinary match event since Phase H.
            // The diagnostic declares that known log noise while preserving all gameplay behaviour.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            try
            {
                var report = new StringBuilder();
                // Keep the measurement-lane catalog sentinel stable; the next line names the expanded contract.
                report.AppendLine("=== §5.Z.9 foul-rate measurement ===");
                report.AppendLine("=== #435 §2.1 source-complete foul/card measurement ===");
                report.AppendLine(
                    Invariant($"seeds={Seeds.Length} ticksPerSeed={TicksPerSeed} ")
                    + Invariant($"({TicksPerSeed / TicksPerSecond / 60f:F1} match-minutes each)"));
                report.AppendLine();

                // Preserve the original offline replay population: the strongest cross-team FROM_BEHIND
                // force per tick before the production force threshold/cooldown. Concatenating seeds is
                // approximate at the five seed seams because replay cooldown state is not reset there.
                var peakForcePerTick = new float[Seeds.Length * TicksPerSeed];
                int writeCursor = 0;

                // #435 §2.1's force distribution is a different population: every FROM_BEHIND contact
                // that clears type/force/team/participation at the live production threshold.
                var qualifyingContactForces = new List<float>();

                int totalAgentAgentContacts = 0;
                int totalFromBehindCandidates = 0;
                int totalFromBehindCalled = 0;
                int totalSlideTackleCandidates = 0;
                int totalSlideTackleCalled = 0;
                int totalCandidateDisplacedByDecided = 0;
                int totalCooldownSuppressionsFromBehind = 0;
                int totalSlideTackleCallsDuringCooldown = 0;
                int totalFouls = 0;
                int totalYellowCards = 0;
                int totalStraightReds = 0;
                int totalSecondYellowDismissals = 0;
                int totalDismissals = 0;
                int totalPlayedTicks = 0;

                var structuralFindings = new List<string>();

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
                    qualifyingContactForces.AddRange(probe.QualifyingContactForces);

                    totalAgentAgentContacts += probe.AgentAgentContacts;
                    totalFromBehindCandidates += probe.FromBehindCandidates;
                    totalFromBehindCalled += probe.FromBehindCalled;
                    totalSlideTackleCandidates += probe.SlideTackleCandidates;
                    totalSlideTackleCalled += probe.SlideTackleCalled;
                    totalCandidateDisplacedByDecided += probe.CandidateDisplacedByDecided;
                    totalCooldownSuppressionsFromBehind += probe.FoulCooldownSuppressionsFromBehind;
                    totalSlideTackleCallsDuringCooldown += probe.SlideTackleCallsDuringFoulCooldown;
                    totalFouls += probe.TotalFouls;
                    totalYellowCards += probe.YellowCards;
                    totalStraightReds += probe.StraightReds;
                    totalSecondYellowDismissals += probe.SecondYellowDismissals;
                    totalDismissals += probe.TotalDismissals;
                    totalPlayedTicks += probe.PlayedTicks;

                    if (probe.FromBehindCalled + probe.SlideTackleCalled != probe.TotalFouls)
                    {
                        structuralFindings.Add(
                            Invariant($"seed 0x{seed:X16}: applied-foul identity failed: ")
                            + Invariant($"fromBehindCalled={probe.FromBehindCalled} + ")
                            + Invariant($"slideTackleCalled={probe.SlideTackleCalled} != totalFouls={probe.TotalFouls}; ")
                            + "stop and localize a third production foul source.");
                    }
                    if (probe.TotalDismissals != probe.StraightReds + probe.SecondYellowDismissals)
                    {
                        structuralFindings.Add(
                            Invariant($"seed 0x{seed:X16}: dismissal identity failed: ")
                            + Invariant($"totalDismissals={probe.TotalDismissals}, straightReds={probe.StraightReds}, ")
                            + Invariant($"secondYellowDismissals={probe.SecondYellowDismissals}."));
                    }
                    if (probe.UnknownFoulSources != 0 || probe.UnknownCardKinds != 0)
                    {
                        structuralFindings.Add(
                            Invariant($"seed 0x{seed:X16}: unknown discipline ordinals: ")
                            + Invariant($"foulSources={probe.UnknownFoulSources}, cardKinds={probe.UnknownCardKinds}; ")
                            + "stop and localize before calibration.");
                    }
                    if (engine.TestOnly_TackleSlideTackleFouls != probe.SlideTackleCalled)
                    {
                        structuralFindings.Add(
                            Invariant($"seed 0x{seed:X16}: live SLIDE_TACKLE counter ")
                            + Invariant($"{engine.TestOnly_TackleSlideTackleFouls} != ledger count {probe.SlideTackleCalled}."));
                    }

                    report.AppendLine(Invariant($"seed 0x{seed:X16}:"));
                    report.AppendLine(
                        Invariant($"  fromBehindCandidates={probe.FromBehindCandidates} ")
                        + Invariant($"fromBehindCalled={probe.FromBehindCalled} ")
                        + Invariant($"slideTackleCandidates={probe.SlideTackleCandidates} ")
                        + Invariant($"slideTackleCalled={probe.SlideTackleCalled}"));
                    report.AppendLine(
                        Invariant($"  candidateDisplacedByDecided={probe.CandidateDisplacedByDecided} ")
                        + Invariant($"foulCooldownSuppressionsFromBehind={probe.FoulCooldownSuppressionsFromBehind} ")
                        + Invariant($"slideTackleCallsDuringFoulCooldown={probe.SlideTackleCallsDuringFoulCooldown}"));
                    report.AppendLine(
                        Invariant($"  totalFouls={probe.TotalFouls} yellowCards={probe.YellowCards} ")
                        + Invariant($"straightReds={probe.StraightReds} ")
                        + Invariant($"secondYellowDismissals={probe.SecondYellowDismissals} ")
                        + Invariant($"totalDismissals={probe.TotalDismissals} playedTicks={probe.PlayedTicks}"));
                    report.AppendLine("  qualifyingContactForce distribution (N):");
                    AppendDistribution(report, "    ", probe.QualifyingContactForces);
                }

                report.AppendLine();
                report.AppendLine("--- aggregate #435 §2.1 discipline stream ---");
                report.AppendLine(
                    Invariant($"fromBehindCandidates={totalFromBehindCandidates} ")
                    + Invariant($"fromBehindCalled={totalFromBehindCalled} ")
                    + Invariant($"slideTackleCandidates={totalSlideTackleCandidates} ")
                    + Invariant($"slideTackleCalled={totalSlideTackleCalled}"));
                report.AppendLine(
                    Invariant($"candidateDisplacedByDecided={totalCandidateDisplacedByDecided} ")
                    + Invariant($"foulCooldownSuppressionsFromBehind={totalCooldownSuppressionsFromBehind} ")
                    + Invariant($"slideTackleCallsDuringFoulCooldown={totalSlideTackleCallsDuringCooldown}"));
                report.AppendLine(
                    Invariant($"totalFouls={totalFouls} yellowCards={totalYellowCards} ")
                    + Invariant($"straightReds={totalStraightReds} ")
                    + Invariant($"secondYellowDismissals={totalSecondYellowDismissals} ")
                    + Invariant($"totalDismissals={totalDismissals} playedTicks={totalPlayedTicks} ")
                    + Invariant($"agentAgentContacts={totalAgentAgentContacts}"));
                report.AppendLine(
                    Invariant($"SHIPPED per-90-min rates: fouls={PerMatch(totalFouls):F2} ")
                    + Invariant($"yellows={PerMatch(totalYellowCards):F2} ")
                    + Invariant($"straightReds={PerMatch(totalStraightReds):F3} ")
                    + Invariant($"secondYellowDismissals={PerMatch(totalSecondYellowDismissals):F3} ")
                    + Invariant($"totalDismissals={PerMatch(totalDismissals):F3}"));

                report.AppendLine();
                report.AppendLine("--- qualifyingContactForce distribution (FROM_BEHIND, N) ---");
                AppendDistribution(report, "  ", qualifyingContactForces);
                report.AppendLine();

                report.AppendLine("--- fouls per 90 minutes, collision gate replayed offline ---");
                report.AppendLine("threshold(N)  " + string.Join("  ", Array.ConvertAll(
                    CooldownLadderTicks, cooldown => Invariant($"cd={cooldown,4}"))));
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
                report.AppendLine("--- CANDIDATE MODEL: force-scaled collision call probability (§5.Z.9) ---");
                report.AppendLine("A qualifying collision contact is WHISTLED with probability");
                report.AppendLine("  p(F) = min(1, callProbability * F / threshold)");
                report.AppendLine("The replay is collision-only; the live source-complete counts above are authoritative");
                report.AppendLine("for W2 tackle interaction, cooldown bypass, restarts, and card decomposition.");
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

                TestContext.WriteLine(report.ToString());

                // These are reconciliation/shape checks from #435 §2.1, not rate assertions. A failure
                // means the measurement taxonomy no longer describes production and calibration must stop.
                if (totalFromBehindCalled + totalSlideTackleCalled != totalFouls)
                {
                    structuralFindings.Add(
                        Invariant($"aggregate applied-foul identity failed: {totalFromBehindCalled} + ")
                        + Invariant($"{totalSlideTackleCalled} != {totalFouls}."));
                }
                if (totalDismissals != totalStraightReds + totalSecondYellowDismissals)
                {
                    structuralFindings.Add(
                        Invariant($"aggregate dismissal identity failed: {totalDismissals} != ")
                        + Invariant($"{totalStraightReds} + {totalSecondYellowDismissals}."));
                }
                if (totalPlayedTicks != Seeds.Length * TicksPerSeed)
                {
                    structuralFindings.Add(
                        Invariant($"playedTicks={totalPlayedTicks} != frozen denominator ")
                        + Invariant($"{Seeds.Length * TicksPerSeed}."));
                }

                if (structuralFindings.Count != 0)
                {
                    Assert.Fail(
                        "Source-complete measurement contract no longer reconciles production:\n"
                        + string.Join("\n", structuralFindings));
                }

                Assert.Pass("Diagnostic only — measured rates are intentionally assertion-free; see run output.");
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        /// <summary>
        /// Replays the production foul gate offline: walk ticks in order, and whenever the cooldown has
        /// expired and this tick carried a qualifying contact at or above <paramref name="thresholdN"/>,
        /// count a foul and re-arm the cooldown. This is exactly <c>MatchFlowCollisionConsumer</c> +
        /// <c>ApplyFoulIfCaptured</c>'s temporal behaviour, which is why a plain histogram would
        /// overcount: the configured debounce discards most of a burst.
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

        private static void AppendDistribution(
            StringBuilder output,
            string indent,
            List<float> qualifyingContactForces)
        {
            if (qualifyingContactForces.Count == 0)
            {
                output.AppendLine(indent + "(no production-threshold FROM_BEHIND candidates observed)");
                return;
            }

            float[] forces = qualifyingContactForces.ToArray();
            Array.Sort(forces);

            output.AppendLine(indent + Invariant($"candidates={forces.Length}"));
            AppendPercentile(output, indent, forces, "p50", 0.50f);
            AppendPercentile(output, indent, forces, "p75", 0.75f);
            AppendPercentile(output, indent, forces, "p90", 0.90f);
            AppendPercentile(output, indent, forces, "p95", 0.95f);
            AppendPercentile(output, indent, forces, "p99", 0.99f);
            AppendPercentile(output, indent, forces, "p99.9", 0.999f);
            output.AppendLine(indent + Invariant($"max   = {forces[forces.Length - 1],12:F0} N"));
        }

        private static void AppendPercentile(
            StringBuilder output,
            string indent,
            float[] sorted,
            string label,
            float quantile)
        {
            int index = Math.Min(sorted.Length - 1, (int)(quantile * (sorted.Length - 1)));
            output.AppendLine(indent + Invariant($"{label,-5} = {sorted[index],12:F0} N"));
        }

        private static float PerMatch(int countOverRun) =>
            countOverRun * (float)TicksPerMatch / (Seeds.Length * (float)TicksPerSeed);

        private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// #435 §2.1 measurement probe. The collision callback mirrors the production foul consumer's
        /// type/force/team gates and the application site's participation gate, while the end-of-tick
        /// ledger tap records what production actually applied. It never writes engine state.
        ///
        /// <para>The probe deliberately keeps two force populations. <see cref="PeakForcePerTick"/> is
        /// threshold-free so the historical offline threshold replay remains usable.
        /// <see cref="QualifyingContactForces"/> contains only live-threshold FROM_BEHIND candidates and
        /// is the preregistered p50/p75/p90/p95/p99/p99.9/max population.</para>
        /// </summary>
        private sealed class FoulCandidateProbe : ICollisionEventConsumer
        {
            private readonly MatchEngine _engine;
            private int _tick;
            private int _cooldownAtTickStart;
            private int _tackleFoulsAtTickStart;
            private int _previousTackleFouls;

            public FoulCandidateProbe(MatchEngine engine, int tickCapacity)
            {
                _engine = engine;
                PeakForcePerTick = new float[tickCapacity];
                QualifyingContactForces = new List<float>(tickCapacity);
            }

            public float[] PeakForcePerTick { get; }
            public List<float> QualifyingContactForces { get; }

            public int AgentAgentContacts { get; private set; }
            public int FromBehindCandidates { get; private set; }
            public int FromBehindCalled { get; private set; }
            public int SlideTackleCandidates { get; private set; }
            public int SlideTackleCalled { get; private set; }
            public int CandidateDisplacedByDecided { get; private set; }
            public int FoulCooldownSuppressionsFromBehind { get; private set; }
            public int SlideTackleCallsDuringFoulCooldown { get; private set; }
            public int TotalFouls { get; private set; }
            public int YellowCards { get; private set; }
            public int StraightReds { get; private set; }
            public int SecondYellowDismissals { get; private set; }
            public int TotalDismissals { get; private set; }
            public int PlayedTicks { get; private set; }
            public int UnknownFoulSources { get; private set; }
            public int UnknownCardKinds { get; private set; }

            public void BeginTick(int tick)
            {
                _tick = tick;
                // TryResolveTackles runs in AI before Resolve decrements the foul cooldown. Capturing the
                // pre-tick value therefore tells us whether a decided tackle foul was RAISED through the
                // current production bypass while the collision source was still under cooldown.
                _cooldownAtTickStart = _engine.TestOnly_FoulCooldownRemaining;
                _tackleFoulsAtTickStart = _engine.TestOnly_TackleOutcomeCounts.Foul;
            }

            public void EndTick()
            {
                var outcomes = _engine.TestOnly_TackleOutcomeCounts;
                int newTackleCandidates = outcomes.Foul - _previousTackleFouls;
                if (newTackleCandidates < 0)
                {
                    throw new InvalidOperationException(
                        "FoulRateDiagnostic: tackle foul counter moved backwards inside one engine run.");
                }

                SlideTackleCandidates += newTackleCandidates;
                _previousTackleFouls = outcomes.Foul;

                // TickLedgerSnapshot is captured after Resolve and before EventBus resets the tick, so it
                // is the exact production event stream without process-static subscription leakage.
                int records = _engine.TickLedgerCount;
                for (int i = 0; i < records; i++)
                {
                    byte ordinal = _engine.TickLedgerOrdinal(i);
                    if (ordinal == FoulCommittedOrdinal)
                    {
                        FoulCommittedEvent foul = _engine.TickLedgerRecord<FoulCommittedEvent>(i);
                        TotalFouls++;

                        if (foul.FoulKind == (byte)ContactType.FROM_BEHIND)
                        {
                            FromBehindCalled++;
                        }
                        else if (foul.FoulKind == (byte)ContactType.SLIDE_TACKLE)
                        {
                            SlideTackleCalled++;

                            // The tackle decision was made in AI, before Resolve's cooldown decrement.
                            // This is the preregistered bypass count even when a pre-tick value of 1
                            // reaches 0 before ApplyFoulIfCaptured later in the same tick.
                            if (_cooldownAtTickStart > 0)
                            {
                                SlideTackleCallsDuringFoulCooldown++;
                            }
                        }
                        else
                        {
                            UnknownFoulSources++;
                        }

                        continue;
                    }

                    if (ordinal != CardIssuedOrdinal)
                    {
                        continue;
                    }

                    CardIssuedEvent card = _engine.TickLedgerRecord<CardIssuedEvent>(i);
                    if (card.CardKind == MatchEngineConstants.CardKindYellow)
                    {
                        YellowCards++;
                    }
                    else if (card.CardKind == MatchEngineConstants.CardKindRed)
                    {
                        StraightReds++;
                        TotalDismissals++;
                    }
                    else if (card.CardKind == MatchEngineConstants.CardKindSecondYellow)
                    {
                        // #435 freezes the existing scenario semantics: the second caution is one
                        // additional yellow AND one dismissal, even though production publishes one
                        // CARD_KIND_SECOND_YELLOW event rather than yellow-then-red.
                        YellowCards++;
                        SecondYellowDismissals++;
                        TotalDismissals++;
                    }
                    else
                    {
                        UnknownCardKinds++;
                    }
                }

                PlayedTicks++;
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

                // Threshold-free per-tick peak for the legacy descriptive threshold replay.
                if (_tick >= 0 && _tick < PeakForcePerTick.Length
                    && foul.ForceMagnitude > PeakForcePerTick[_tick])
                {
                    PeakForcePerTick[_tick] = foul.ForceMagnitude;
                }

                // Everything below is the source-complete production candidate population.
                if (foul.ForceMagnitude < MatchEngineConstants.FoulImpactForceThresholdN)
                {
                    return;
                }

                FromBehindCandidates++;
                QualifyingContactForces.Add(foul.ForceMagnitude);

                // MatchFlowCollisionConsumer checks cooldown BEFORE it checks the single-slot decided
                // candidate. If both conditions are true, production suppresses at the cooldown site,
                // so the categories stay mutually faithful to the current control-flow order.
                if (_engine.TestOnly_FoulCooldownRemaining > 0)
                {
                    FoulCooldownSuppressionsFromBehind++;
                    return;
                }

                // AI precedes Physics/Resolve. If #14's cumulative foul outcome advanced since
                // BeginTick, RaiseDecidedFoulCandidate has already seated a decided tackle candidate
                // in the one per-tick slot. The consumer's next early return is therefore the exact
                // #435 §2.1 same-tick displacement site.
                if (_engine.TestOnly_TackleOutcomeCounts.Foul > _tackleFoulsAtTickStart)
                {
                    CandidateDisplacedByDecided++;
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
// | 1.1     | 2026-09-21 | —      | #435 §2.1/§3: source-complete live discipline census over the frozen     |
// |         |            |        | six full-match corpus. Splits collision/tackle candidates + applied     |
// |         |            |        | calls, measures cooldown suppression/bypass and same-tick displacement, |
// |         |            |        | decomposes card events (yellow/straight-red/second-yellow), reports the |
// |         |            |        | qualifying-force quantiles, and adds live cooldown 180 to the replay.   |
// |         |            |        | Measurement-only: no gameplay [GT] or production behaviour changed.     |
#endregion
