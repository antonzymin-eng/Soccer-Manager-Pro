// File:     src/match-engine/tests/MatchBalanceDiagnosticTests.cs
// Created:  2026-07-26
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.11 / §5.Z.13;
//           path-to-playable roadmap A4a; Code Standards #20
// Purpose:  The MEASUREMENT half of the §5.Z.11 findings — a structural home/away scoring asymmetry
//           (~50x football's home advantage) and a goal rate ~10x football's — plus the §5.Z.9
//           contact-rate finding (17 hard cross-team from-behind contacts per second).
//
//           §5.Z.11 named two candidate causes for the asymmetry and the measurement that would
//           separate them: "the asymmetry either compounds over a FULL match or is specific to the
//           ConfigureSquads path". This driver runs BOTH configurations at FULL match length and
//           reports per team: possession, shots, passes, restarts, ball time in the third and the box
//           EACH TEAM ATTACKS, and goals — the per-team attribution the 9-minute absolute-x report
//           could not give. Half-by-half splits show whether the asymmetry compounds.
//
//           A second driver characterises the contact stream itself: contact type x cross/same team,
//           the pairwise-separation distribution, and agent speeds — the inputs needed to decide
//           whether 17/second comes from #12 agent spacing or #3's 60-degree BehindDotThreshold cone.
//
//           Env-gated and assertion-free, for the reason every diagnostic in this project is: pinning
//           measured-but-wrong behaviour would turn a defect into a contract.
//
//             TD_BALANCE_DIAGNOSTIC=1 dotnet test -c Release --filter MatchBalanceDiagnostic

using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.CollisionSystem;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class MatchBalanceDiagnosticTests
    {
        /// <summary>A full 90-minute match. §5.Z.11's own instruction: measure over a FULL match, not nine minutes.</summary>
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        /// <summary>Physics ticks per second (Ball Physics #1 / Deterministic Sim #16: 60 Hz).</summary>
        private const float TicksPerSecond = 60.0f;

        /// <summary>Half-way tick, so the report can show whether the asymmetry compounds.</summary>
        private static readonly int HalfTimeTick = (int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
        };

        /// <summary>Penalty-area depth from each goal line (IFAB 16.5 m).</summary>
        private const float BoxDepthM = 16.5f;

        /// <summary>Attacking-third boundary as a fraction of pitch length.</summary>
        private const float ThirdFraction = 1f / 3f;

        // ── Test A: the home/away asymmetry and the goal rate ───────────────────────────────────

        [Test]
        [Category("Calibration")]
        public void MatchBalanceDiagnostic_ReportsScorelineAsymmetry()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== §5.Z.11 home/away asymmetry + goal rate: FULL-MATCH per-team measurement ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch} ({TicksPerMatch / TicksPerSecond / 60f:F0} minutes) seeds={Seeds.Length}"));
            report.AppendLine("Team 0 attacks +X (target goal x=105). Team 1 attacks -X (target goal x=0).");
            report.AppendLine("Every column is attributed to the team it belongs to — 'attThird'/'attBox' are");
            report.AppendLine("ticks with the ball in the third / penalty area THAT TEAM ATTACKS.");
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                RunOne(report, seed, distinctSquads: false, strengthDelta: 0);
                RunOne(report, seed, distinctSquads: true, strengthDelta: 0);
            }

            report.AppendLine("Reading it:");
            report.AppendLine("  * neutral asymmetric AND configured asymmetric  => structural, compounds over a match");
            report.AppendLine("  * neutral balanced but configured asymmetric    => the ConfigureSquads path");
            report.AppendLine("  * H1 vs H2 columns show whether it compounds or is present from the first minute.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunOne(StringBuilder report, ulong seed, bool distinctSquads, int strengthDelta)
        {
            var engine = new MatchEngine(seed);

            if (distinctSquads)
            {
                // Equal-strength squads on the ConfigureSquads path: isolates the PATH from the strength ramp.
                engine.ConfigureSquads(BuildSquad(seed, clubId: 1, delta: +strengthDelta),
                                       BuildSquad(seed, clubId: 2, delta: -strengthDelta));
            }

            var m = new TeamMetrics[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < m.Length; t++)
            {
                m[t] = new TeamMetrics();
            }

            var shotBusy = new bool[MatchEngineConstants.SQUAD_SIZE];
            var passBusy = new bool[MatchEngineConstants.SQUAD_SIZE];
            int prevHome = 0;
            int prevAway = 0;

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();
                int half = tick < HalfTimeTick ? 0 : 1;

                // Possession.
                int holder = engine.PossessingAgentId;
                if (holder >= 0)
                {
                    m[engine.AgentTeamId(holder)].PossessionTicks[half]++;
                    if (engine.AgentIsGoalkeeper(holder))
                    {
                        // §5.Z.15 follow-up: a keeper is a live agent now, so it can WIN possession —
                        // and #11's distribution is not engine-driven, so it may never release it.
                        m[engine.AgentTeamId(holder)].GkPossessionTicks[half]++;
                    }
                }

                // Shot / pass initiations: an executor going idle -> busy is one attempt.
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    bool sIdle = engine.TestOnly_ShotExecutorIdle(a);
                    if (!sIdle && shotBusy[a] == false)
                    {
                        m[engine.AgentTeamId(a)].Shots[half]++;
                    }
                    shotBusy[a] = !sIdle;

                    bool pIdle = engine.TestOnly_PassExecutorIdle(a);
                    if (!pIdle && passBusy[a] == false)
                    {
                        m[engine.AgentTeamId(a)].Passes[half]++;
                    }
                    passBusy[a] = !pIdle;
                }

                // Where the ball is, attributed per team's own attacking direction.
                float x = engine.BallView.Position.x;
                float len = MatchEngineConstants.PITCH_LENGTH_M;
                if (x > len * (1f - ThirdFraction)) m[0].AttackingThirdTicks[half]++;
                if (x < len * ThirdFraction) m[1].AttackingThirdTicks[half]++;
                if (x > len - BoxDepthM) m[0].AttackingBoxTicks[half]++;
                if (x < BoxDepthM) m[1].AttackingBoxTicks[half]++;

                m[0].BallXSum += x;
                if (x < m[0].BallXMin) m[0].BallXMin = x;
                if (x > m[0].BallXMax) m[0].BallXMax = x;

                // Goals, per half.
                if (engine.HomeScore != prevHome)
                {
                    m[0].Goals[half] += engine.HomeScore - prevHome;
                    prevHome = engine.HomeScore;
                }
                if (engine.AwayScore != prevAway)
                {
                    m[1].Goals[half] += engine.AwayScore - prevAway;
                    prevAway = engine.AwayScore;
                }
            }

            string config = distinctSquads
                ? Inv($"configured(+{strengthDelta}/-{strengthDelta})")
                : "neutral";

            report.AppendLine(Inv($"seed 0x{seed:X16}  config={config}  ")
                + Inv($"score {engine.HomeScore}-{engine.AwayScore}  ")
                + Inv($"ballX mean={m[0].BallXSum / TicksPerMatch:F1} range {m[0].BallXMin:F1}..{m[0].BallXMax:F1}"));
            report.AppendLine("   team | goals H1/H2 | poss% H1/H2 | GKposs% H1/H2 | shots H1/H2 | passes H1/H2 | attThird% | attBox%");
            for (int t = 0; t < m.Length; t++)
            {
                TeamMetrics x = m[t];
                report.AppendLine(
                    Inv($"      {t} |     {x.Goals[0],3}/{x.Goals[1],-3} | ")
                    + Inv($"{Pct(x.PossessionTicks[0]),5}/{Pct(x.PossessionTicks[1]),-5} | ")
                    + Inv($"{Pct(x.GkPossessionTicks[0]),6}/{Pct(x.GkPossessionTicks[1]),-6} | ")
                    + Inv($"{x.Shots[0],5}/{x.Shots[1],-5} | ")
                    + Inv($"{x.Passes[0],6}/{x.Passes[1],-6} | ")
                    + Inv($"{Pct(x.AttackingThirdTicks[0] + x.AttackingThirdTicks[1]) ,8} | ")
                    + Inv($"{Pct(x.AttackingBoxTicks[0] + x.AttackingBoxTicks[1]),6}"));
            }
            report.AppendLine();
        }

        // ── Test B: the contact stream underneath the refereeing model ──────────────────────────

        [Test]
        [Category("Calibration")]
        public void MatchBalanceDiagnostic_ReportsContactStream()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            // 9 minutes is ample: the finding is 17 qualifying contacts per SECOND.
            const int Ticks = 32400;
            const int SampleEvery = 60; // separation histogram once per second

            var report = new StringBuilder();
            report.AppendLine("=== §5.Z.9 recorded finding: the CONTACT STREAM itself ===");
            report.AppendLine(Inv($"ticksPerSeed={Ticks} ({Ticks / TicksPerSecond:F0} s) seeds={Seeds.Length}"));
            report.AppendLine();

            foreach (ulong seed in Seeds)
            {
                var engine = new MatchEngine(seed);
                var probe = new ContactStreamProbe(engine);
                engine.TestOnly_SetCollisionObserver(probe);

                // Pairwise separation + speed distribution, sampled off the collision stream so it
                // measures the CAUSE (how close agents stand) rather than the effect (contacts).
                var sepBuckets = new long[6]; // <0.85, <1.0, <1.5, <2.0, <3.0, >=3.0 (metres)
                double speedSum = 0;
                long speedSamples = 0;
                long pairSamples = 0;

                for (int t = 0; t < Ticks; t++)
                {
                    probe.BeginTick();
                    engine.RunTick();

                    if (t % SampleEvery != 0)
                    {
                        continue;
                    }

                    for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                    {
                        UnityEngine.Vector2 pa = engine.AgentView(a).Position;
                        speedSum += engine.AgentView(a).Velocity.magnitude;
                        speedSamples++;

                        for (int b = a + 1; b < MatchEngineConstants.SQUAD_SIZE; b++)
                        {
                            float d = (pa - engine.AgentView(b).Position).magnitude;
                            pairSamples++;
                            if (d < 0.85f) sepBuckets[0]++;
                            else if (d < 1.0f) sepBuckets[1]++;
                            else if (d < 1.5f) sepBuckets[2]++;
                            else if (d < 2.0f) sepBuckets[3]++;
                            else if (d < 3.0f) sepBuckets[4]++;
                            else sepBuckets[5]++;
                        }
                    }
                }

                engine.TestOnly_SetCollisionObserver(null);

                float seconds = Ticks / TicksPerSecond;
                report.AppendLine(Inv($"seed 0x{seed:X16}"));
                report.AppendLine(Inv($"  agent-agent contacts   : {probe.Total} ({probe.Total / seconds:F1}/s)"));
                report.AppendLine(Inv($"    FROM_BEHIND  cross={probe.CrossFromBehind} ({probe.CrossFromBehind / seconds:F1}/s)  same={probe.SameFromBehind}"));
                report.AppendLine(Inv($"    SHOULDER     cross={probe.CrossShoulder}  same={probe.SameShoulder}"));
                report.AppendLine(Inv($"    SIDE_IMPACT  cross={probe.CrossSide}  same={probe.SameSide}"));
                report.AppendLine(Inv($"  ticks with >=1 qualifying: {probe.TicksWithQualifying} ({100f * probe.TicksWithQualifying / Ticks:F1}% of ticks)"));
                report.AppendLine(Inv($"  mean agent speed        : {speedSum / Math.Max(1, speedSamples):F2} m/s"));
                report.AppendLine(Inv($"  pairwise separation over {pairSamples} sampled pairs:"));
                string[] labels = { "<0.85m (overlapping)", "0.85-1.0m", "1.0-1.5m", "1.5-2.0m", "2.0-3.0m", ">=3.0m" };
                for (int i = 0; i < sepBuckets.Length; i++)
                {
                    report.AppendLine(Inv($"    {labels[i],-22} {sepBuckets[i],9}  ({100.0 * sepBuckets[i] / Math.Max(1, pairSamples):F2}%)"));
                }
                report.AppendLine();
            }

            report.AppendLine("Reading it: agents at <0.85 m are inside the ~0.84 m combined hitbox — they are");
            report.AppendLine("in contact. A large share there points at #12 spacing; a large FROM_BEHIND share");
            report.AppendLine("of an otherwise ordinary contact count points at #3's 60-degree BehindDotThreshold cone.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_BALANCE_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_BALANCE_DIAGNOSTIC=1 to run the §5.Z.11 balance characterisation.");
            }
        }

        /// <summary>
        /// A position-coherent squad on the <c>ConfigureSquads</c> path, optionally strength-shifted.
        /// Mirrors the league-bootstrap recipe (a `[GT]` position template so `LineupSelector` can field
        /// a back four) without referencing season-save, which sits ABOVE match-engine.
        /// </summary>
        private static Squad BuildSquad(ulong seed, int clubId, int delta)
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

            Squad squad = RosterGenerator.Generate(rng, stream, clubId, template);
            if (delta == 0)
            {
                return squad;
            }

            var shifted = new PlayerRecord[squad.Count];
            for (int p = 0; p < shifted.Length; p++)
            {
                PlayerRecord r = squad.GetPlayer(p);
                int[] a = r.Attributes.ToArray();
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = Math.Min(PlayerDatabaseConstants.ATTRIBUTE_MAX,
                                    Math.Max(PlayerDatabaseConstants.ATTRIBUTE_MIN, a[f] + delta));
                }
                r.Attributes.FromArray(a);
                shifted[p] = r;
            }
            return new Squad(clubId, shifted);
        }

        private sealed class TeamMetrics
        {
            public readonly int[] Goals = new int[2];
            public readonly int[] PossessionTicks = new int[2];
            public readonly int[] GkPossessionTicks = new int[2];
            public readonly int[] Shots = new int[2];
            public readonly int[] Passes = new int[2];
            public readonly int[] AttackingThirdTicks = new int[2];
            public readonly int[] AttackingBoxTicks = new int[2];
            public double BallXSum;
            public float BallXMin = float.MaxValue;
            public float BallXMax = float.MinValue;
        }

        /// <summary>
        /// Counts every agent-agent contact by type and by cross-team-ness. Deliberately does NOT
        /// apply the production foul gate — the question here is the shape of the stream underneath it.
        /// </summary>
        private sealed class ContactStreamProbe : ICollisionEventConsumer
        {
            private readonly MatchEngine _engine;
            private bool _qualifyingThisTick;

            public ContactStreamProbe(MatchEngine engine) => _engine = engine;

            public int Total { get; private set; }
            public int CrossFromBehind { get; private set; }
            public int SameFromBehind { get; private set; }
            public int CrossShoulder { get; private set; }
            public int SameShoulder { get; private set; }
            public int CrossSide { get; private set; }
            public int SameSide { get; private set; }
            public int TicksWithQualifying { get; private set; }

            public void BeginTick()
            {
                if (_qualifyingThisTick)
                {
                    TicksWithQualifying++;
                }
                _qualifyingThisTick = false;
            }

            public void OnCollisionEvent(in CollisionEvent evt)
            {
                if (evt.Type != CollisionType.AGENT_AGENT)
                {
                    return;
                }

                Total++;

                ContactForceData foul = evt.FoulData;
                bool cross = _engine.AgentTeamId(foul.InstigatorAgentID) != _engine.AgentTeamId(foul.VictimAgentID);

                switch (foul.Type)
                {
                    case ContactType.FROM_BEHIND:
                        if (cross) { CrossFromBehind++; _qualifyingThisTick = true; } else SameFromBehind++;
                        break;
                    case ContactType.SHOULDER_TO_SHOULDER:
                        if (cross) CrossShoulder++; else SameShoulder++;
                        break;
                    default:
                        if (cross) CrossSide++; else SameSide++;
                        break;
                }
            }
        }

        private static string Pct(int ticks) =>
            (100f * ticks / TicksPerMatch).ToString("F1", CultureInfo.InvariantCulture);

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial: localises the Step 0 scoreline asymmetry (away team never |
// |         |            |        | scores; home totals an order of magnitude above football) by       |
// |         |            |        | reporting per-team possession share against where the ball spends  |
// |         |            |        | its time. Asserts nothing.                                        |
// | 1.1     | 2026-07-27 | —      | Rebuilt as the §5.Z.11 measurement pass. FULL-match length (was 9  |
// |         |            |        | min), per-team attribution against each team's OWN attacking       |
// |         |            |        | direction, half-by-half splits, shots/passes, and both the neutral |
// |         |            |        | and ConfigureSquads paths — the two candidates §5.Z.11 named.      |
// |         |            |        | Adds MatchBalanceDiagnostic_ReportsContactStream: contact type x   |
// |         |            |        | cross/same team, pairwise-separation histogram and agent speeds,   |
// |         |            |        | the §5.Z.9 recorded-not-fixed contact-rate finding. Still asserts  |
// |         |            |        | nothing.                                                          |
#endregion
