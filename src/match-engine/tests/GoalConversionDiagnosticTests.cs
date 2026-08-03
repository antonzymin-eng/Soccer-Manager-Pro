// File:     src/match-engine/tests/GoalConversionDiagnosticTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5, Decision Tree #8 §3.2, Testing Strategy #19 (instrument class)
// Purpose:  Env-gated (TD_CONVERSION_DIAGNOSTIC=1) instrument for the two residuals
//           gk-contact-rate-design.md §7 recorded and did not fix:
//
//             §7 item 1 — CONVERSION AT CONTACT. §5.Z.22 tripled the keeper's contact rate
//               (~35% -> ~72%) and the goal count did not move (14 -> 15 over the corpus). The
//               recorded explanation is that the added contacts are "marginal, end-of-envelope
//               touches whose parries and spills keep the ball alive in the box". That is a
//               PREMISE, not a measurement: no instrument in the tree reports what a contact
//               does. Report A measures it — per contact, how marginal it actually was (the
//               real hand-envelope offset, which §3.5.1's pointQuality term does NOT consume),
//               what band it resolved to, where the ball went, and whether a goal followed.
//               It also reports goal PROVENANCE, which is the number that decides whether the
//               residual is at the keeper's hands at all: what share of goals follow a contact.
//
//             §7 item 4 — CLOSE-CHANCE CREATION / POSSESSION CHURN (§5.Z.21's residual). The
//               shot-volume ladder refused count ~25 AND mean <=22 m jointly because volume is
//               bounded by close-chance creation at ~3x football's final-third churn. Report B
//               is the creation funnel behind that claim: possession chains and their length,
//               final-third entries, PENALTY-BOX entries, and the conversion of each stage into
//               the next.
//
//           Asserts nothing (the ERR-030-014 convention) — pinning measured-but-wrong behaviour
//           turns a defect into a contract. Acceptance predicates live in scenarios, not here.
//
//           Run:
//             TD_CONVERSION_DIAGNOSTIC=1 dotnet test -c Release --filter GoalConversionDiagnostic

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Conversion-at-contact anatomy and the close-chance creation funnel, over full 90-minute
    /// matches on the <c>ConfigureSquads</c> path (the §5.Z.20–§5.Z.22 measurement population).
    /// See file header.
    /// </summary>
    [TestFixture]
    internal class GoalConversionDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        private const float TicksPerSecond = 60.0f;

        /// <summary>Window after a keeper contact inside which the ball's fate is attributed to that
        /// contact (5 s at 60 Hz — a rebound plus the immediate scramble).</summary>
        private const int ContactFateWindowTicks = 300;

        /// <summary>Offset at which the post-contact ball position is sampled (0.5 s) — long enough
        /// for the rebound to travel, short enough that it is still the keeper's touch being read.</summary>
        private const int ReboundSampleOffsetTicks = 30;

        /// <summary>Window before a goal inside which a keeper contact counts as that goal's
        /// antecedent (10 s at 60 Hz — the shot, the save, and the follow-up).</summary>
        private const int GoalProvenanceWindowTicks = 600;

        /// <summary>IFAB penalty-area depth from the goal line (m). Local to the instrument: the
        /// engine has no penalty-area constant, and a diagnostic must not invent a production one.</summary>
        private const float PenaltyAreaDepthM = 16.5f;

        /// <summary>IFAB penalty-area half-width (m): 40.32 m total / 2.</summary>
        private const float PenaltyAreaHalfWidthM = 20.16f;

        /// <summary>The §5.Z.20–§5.Z.22 seeds, so pre/post numbers are same-population comparable.</summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        [Test]
        [Category("Calibration")]
        public void GoalConversionDiagnostic_ReportsContactFateAndCreationFunnel()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== gk-contact-rate §7: conversion AT contact, and close-chance creation ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}  fateWindow={ContactFateWindowTicks} ticks"));
            report.AppendLine(Inv($"bands: catch>={GoalkeeperConstants.CatchThreshold:F2}  parry>={GoalkeeperConstants.ParryThreshold:F2}  ")
                            + Inv($"deflect>={GoalkeeperConstants.DeflectThreshold:F2}  spill>={GoalkeeperConstants.MinHandlingQuality:F2}"));
            report.AppendLine();
            report.AppendLine("REPORT A — what a keeper contact DOES.");
            report.AppendLine("  marginality = |hand-envelope centre - ball| at contact / reach radius, in [0,1].");
            report.AppendLine("  A fingertip touch at full stretch is ~1; a comfortable body-line catch is ~0.");
            report.AppendLine("  §3.5.1's pointQuality term does NOT consume this — it feeds itself the ball");
            report.AppendLine("  position as BOTH contact anchors, so quality should be FLAT across marginality.");
            report.AppendLine("  reboundY = |ball.y - 34| at +0.5 s: small = parried back into the central channel.");
            report.AppendLine();

            var all = new List<Contact>();
            var funnels = new List<Funnel>();

            foreach (ulong seed in Seeds)
            {
                RunMatch(report, seed, all, funnels);
            }

            AppendBandTable(report, all);
            AppendMarginalityTable(report, all);
            AppendFunnelSummary(report, funnels);

            report.AppendLine("Reading it:");
            report.AppendLine("  * mean quality FLAT across marginality terciles => the §3.5.1 point-quality term is");
            report.AppendLine("    a lottery blind to how good the contact was: the dominant term is missing.");
            report.AppendLine("  * high goalsAfterContact share => the residual really is at the keeper's hands.");
            report.AppendLine("    LOW share => the surplus is created elsewhere and §7 item 1 is the wrong lever.");
            report.AppendLine("  * small reboundY on parries/spills => nothing steers a parry away from the mouth.");
            report.AppendLine("  * shots per BOX entry far below football's => close-chance creation is the bound.");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void RunMatch(StringBuilder report, ulong seed, List<Contact> all, List<Funnel> funnels)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int teams = GoalkeeperConstants.MaxGkAgents;
            var prevContactFrame = new int[teams];
            var live = new List<Contact>();
            var matchContacts = new List<Contact>();
            for (int t = 0; t < teams; t++)
            {
                prevContactFrame[t] = -1;
            }

            var f = new Funnel { Seed = seed };
            int prevHome = 0, prevAway = 0;
            int prevShotContacts = 0;
            float prevBallSpeed = 0f;
            int prevHolder = -1;
            int prevHolderTeam = -1;
            int chainStartTick = 0;
            var wasInThird = new bool[teams];
            var wasInBox = new bool[teams];

            // Rolling record of the most recent contact tick at each end, for goal provenance.
            var lastContactTick = new int[teams];
            for (int t = 0; t < teams; t++)
            {
                lastContactTick[t] = int.MinValue;
            }

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                prevBallSpeed = engine.BallView.Velocity.magnitude;
                engine.RunTick();

                GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                UnityEngine.Vector3 ballVel = engine.BallView.Velocity;
                int holder = engine.PossessingAgentId;
                int holderTeam = holder >= 0 ? engine.AgentTeamId(holder) : -1;

                // ── Report A: new contacts ───────────────────────────────────────────
                for (int t = 0; t < teams; t++)
                {
                    int cf = gk.ContactStates[t].ActualContactFrame;
                    if (cf == prevContactFrame[t] || cf < 0)
                    {
                        continue;
                    }

                    prevContactFrame[t] = cf;
                    lastContactTick[t] = tick;

                    float reach = GoalkeeperDiveKinematics.ComputeReachRadius(gk.Attrs[t]);
                    float envOffset = gk.ContactStates[t].ContactPointError.magnitude;

                    var c = new Contact
                    {
                        Team = t,
                        ContactTick = tick,
                        Quality = gk.ContactStates[t].HandlingQualityScalar,
                        ReactionWindow = gk.ContactStates[t].ReactionWindowAchieved,
                        EnvOffsetM = envOffset,
                        ReachM = reach,
                        Marginality = reach > 1e-4f ? Clamp01(envOffset / reach) : 0f,
                        IncomingSpeed = prevBallSpeed,
                        OutgoingSpeed = ballVel.magnitude,
                        Outcome = Fate.Pending,
                        ReboundYOffset = float.NaN,
                        ReboundDistToGoal = float.NaN,
                    };
                    live.Add(c);
                }

                // ── Report A: resolve live contacts ──────────────────────────────────
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    Contact c = live[i];
                    int age = tick - c.ContactTick;
                    float goalX = c.Team == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;

                    if (age == ReboundSampleOffsetTicks)
                    {
                        c.ReboundYOffset = Math.Abs(ballPos.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f);
                        c.ReboundDistToGoal = Dist2D(ballPos, goalX, MatchEngineConstants.PITCH_WIDTH_M * 0.5f);
                    }

                    // Conceding at this keeper's end = the OTHER team's score advancing.
                    int concededNow = c.Team == 0
                        ? engine.AwayScore - prevAway
                        : engine.HomeScore - prevHome;

                    if (concededNow > 0)
                    {
                        c.Outcome = Fate.Goal;
                    }
                    else if (holderTeam == c.Team)
                    {
                        c.Outcome = Fate.DefenceRecovered;
                    }
                    else if (holderTeam >= 0)
                    {
                        c.Outcome = Fate.AttackRetained;
                    }
                    else if (engine.RestartAppliedThisTick == RestartCue.Corner
                             || engine.RestartAppliedThisTick == RestartCue.GoalKick
                             || engine.RestartAppliedThisTick == RestartCue.ThrowIn)
                    {
                        c.Outcome = engine.RestartAppliedThisTick == RestartCue.Corner
                            ? Fate.RestartCorner
                            : Fate.RestartOut;
                    }
                    else if (age >= ContactFateWindowTicks)
                    {
                        c.Outcome = Fate.StillLoose;
                    }

                    live[i] = c;

                    if (c.Outcome != Fate.Pending && (age >= ReboundSampleOffsetTicks || c.Outcome == Fate.Goal))
                    {
                        matchContacts.Add(c);
                        live.RemoveAt(i);
                    }
                }

                // ── Goal provenance ──────────────────────────────────────────────────
                if (engine.HomeScore != prevHome)
                {
                    f.Goals += engine.HomeScore - prevHome;
                    // Home scored => the AWAY keeper (team 1) conceded.
                    if (tick - lastContactTick[1] <= GoalProvenanceWindowTicks) f.GoalsAfterContact++;
                    prevHome = engine.HomeScore;
                }
                if (engine.AwayScore != prevAway)
                {
                    f.Goals += engine.AwayScore - prevAway;
                    if (tick - lastContactTick[0] <= GoalProvenanceWindowTicks) f.GoalsAfterContact++;
                    prevAway = engine.AwayScore;
                }

                // ── Report B: creation funnel ────────────────────────────────────────
                int shots = engine.TestOnly_ShotContacts;
                if (shots != prevShotContacts)
                {
                    f.Shots += shots - prevShotContacts;
                    prevShotContacts = shots;
                }

                if (holder != prevHolder)
                {
                    if (holder >= 0)
                    {
                        f.Settles++;
                        if (prevHolderTeam != holderTeam)
                        {
                            if (prevHolderTeam >= 0)
                            {
                                f.ChainTicks += tick - chainStartTick;
                                f.Chains++;
                            }
                            chainStartTick = tick;
                        }
                        prevHolderTeam = holderTeam;
                    }
                    prevHolder = holder;
                }

                for (int t = 0; t < teams; t++)
                {
                    // Attacking third / box AT the goal team t defends (i.e. the other team attacking).
                    float goalX = t == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
                    float depth = Math.Abs(ballPos.x - goalX);

                    bool inThird = depth <= MatchEngineConstants.PITCH_LENGTH_M / 3f;
                    if (inThird && !wasInThird[t]) f.ThirdEntries++;
                    wasInThird[t] = inThird;

                    bool inBox = depth <= PenaltyAreaDepthM
                                 && Math.Abs(ballPos.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f)
                                    <= PenaltyAreaHalfWidthM;
                    if (inBox && !wasInBox[t]) f.BoxEntries++;
                    wasInBox[t] = inBox;
                }
            }

            // Anything still unresolved at full time is reported as such rather than dropped.
            for (int i = 0; i < live.Count; i++)
            {
                Contact c = live[i];
                c.Outcome = Fate.StillLoose;
                matchContacts.Add(c);
            }

            all.AddRange(matchContacts);
            funnels.Add(f);

            report.AppendLine(Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}   contacts={matchContacts.Count}"));
            report.AppendLine(Inv($"  goals={f.Goals}  goalsAfterContact(<=10s)={f.GoalsAfterContact}  ")
                            + Inv($"share={(f.Goals > 0 ? (float)f.GoalsAfterContact / f.Goals : 0f):P0}"));
            report.AppendLine(Inv($"  shots={f.Shots}  thirdEntries={f.ThirdEntries}  boxEntries={f.BoxEntries}  ")
                            + Inv($"chains={f.Chains}  settles={f.Settles}"));
            report.AppendLine();
        }

        // ── Aggregate tables ────────────────────────────────────────────────────────────────────

        private static void AppendBandTable(StringBuilder report, List<Contact> all)
        {
            report.AppendLine("A1. Contact outcome by handling band (corpus aggregate):");
            report.AppendLine("  vIn/vOut = ball speed the tick BEFORE the contact / at the end of the contact tick.");
            report.AppendLine("  A band that stops the shot must show vOut << vIn. A CATCH showing vOut ~= vIn");
            report.AppendLine("  is a claim that does not touch the ball.");
            report.AppendLine("  band     |  n | meanQual | meanMarg | vIn  | vOut | meanReboundY | goal | atkRetained | defRecovered | corner | out | loose");

            var bands = new[]
            {
                HandlingQualityLabel.Caught,
                HandlingQualityLabel.Parried,
                HandlingQualityLabel.Deflected,
                HandlingQualityLabel.Spilled,
                HandlingQualityLabel.Missed,
            };

            foreach (HandlingQualityLabel band in bands)
            {
                var rows = new List<Contact>();
                for (int i = 0; i < all.Count; i++)
                {
                    if (BandOf(all[i].Quality) == band) rows.Add(all[i]);
                }

                report.AppendLine(FormatRow(band.ToString(), rows));
            }

            report.AppendLine(FormatRow("ALL", all));
            report.AppendLine();
        }

        private static void AppendMarginalityTable(StringBuilder report, List<Contact> all)
        {
            report.AppendLine("A2. THE LOTTERY TEST — is contact quality related to how good the contact was?");
            report.AppendLine("  Contacts split by marginality tercile. If §3.5.1 consumed the real contact");
            report.AppendLine("  geometry, meanQual would FALL as marginality rises. Flat => a blind lottery.");
            report.AppendLine("  tercile          |  n | meanMarg | meanQual | catch% | parry+% | meanReboundY");

            var sorted = new List<Contact>(all);
            sorted.Sort((a, b) => a.Marginality.CompareTo(b.Marginality));

            int n = sorted.Count;
            if (n == 0)
            {
                report.AppendLine("  (no contacts measured)");
                report.AppendLine();
                return;
            }

            string[] names = { "low  (tight)", "mid", "high (fingertip)" };
            for (int k = 0; k < 3; k++)
            {
                int lo = n * k / 3;
                int hi = n * (k + 1) / 3;
                var bucket = sorted.GetRange(lo, hi - lo);

                float mq = 0f, mm = 0f, mry = 0f;
                int catches = 0, parryPlus = 0, ryN = 0;
                for (int i = 0; i < bucket.Count; i++)
                {
                    mq += bucket[i].Quality;
                    mm += bucket[i].Marginality;
                    if (bucket[i].Quality >= GoalkeeperConstants.CatchThreshold) catches++;
                    if (bucket[i].Quality >= GoalkeeperConstants.ParryThreshold) parryPlus++;
                    if (!float.IsNaN(bucket[i].ReboundYOffset)) { mry += bucket[i].ReboundYOffset; ryN++; }
                }

                int c = Math.Max(1, bucket.Count);
                report.AppendLine(
                    Inv($"  {names[k],-16} | {bucket.Count,2} | {mm / c,8:F3} | {mq / c,8:F3} | ")
                    + Inv($"{(float)catches / c,6:P0} | {(float)parryPlus / c,7:P0} | {(ryN > 0 ? mry / ryN : 0f),12:F2}"));
            }

            report.AppendLine();
        }

        private static void AppendFunnelSummary(StringBuilder report, List<Funnel> funnels)
        {
            report.AppendLine("REPORT B — the close-chance creation funnel (§5.Z.21's recorded residual).");
            report.AppendLine("  Football reference (recorded in §5.Z.21): ~0.2 shots per final-third entry;");
            report.AppendLine("  this engine measured ~0.05 there, with final-third churn at ~3x football's.");
            report.AppendLine("  A team chain is one unbroken spell of team possession; settles/chain is how many");
            report.AppendLine("  distinct players touched it inside that spell (football: several; 1.0 = no team");
            report.AppendLine("  ever completes a pass to a team-mate before losing the ball).");
            report.AppendLine("  seed             | chains | meanChain(s) | settles/chain | thirdEntries | boxEntries | shots | sh/third | sh/box | goals");

            int nChains = 0, nSettles = 0, nThird = 0, nBox = 0, nShots = 0, nGoals = 0;
            float chainTicks = 0f;

            for (int i = 0; i < funnels.Count; i++)
            {
                Funnel f = funnels[i];
                nChains += f.Chains;
                nSettles += f.Settles;
                nThird += f.ThirdEntries;
                nBox += f.BoxEntries;
                nShots += f.Shots;
                nGoals += f.Goals;
                chainTicks += f.ChainTicks;

                report.AppendLine(
                    Inv($"  0x{f.Seed:X16} | {f.Chains,6} | {(f.Chains > 0 ? f.ChainTicks / f.Chains / TicksPerSecond : 0f),12:F2} | ")
                    + Inv($"{(f.Chains > 0 ? (float)f.Settles / f.Chains : 0f),13:F2} | {f.ThirdEntries,12} | {f.BoxEntries,10} | {f.Shots,5} | ")
                    + Inv($"{(f.ThirdEntries > 0 ? (float)f.Shots / f.ThirdEntries : 0f),8:F3} | ")
                    + Inv($"{(f.BoxEntries > 0 ? (float)f.Shots / f.BoxEntries : 0f),6:F3} | {f.Goals,5}"));
            }

            int m = Math.Max(1, funnels.Count);
            report.AppendLine(
                Inv($"  per match          | {(float)nChains / m,6:F1} | {(nChains > 0 ? chainTicks / nChains / TicksPerSecond : 0f),12:F2} | ")
                + Inv($"{(nChains > 0 ? (float)nSettles / nChains : 0f),13:F2} | {(float)nThird / m,12:F1} | {(float)nBox / m,10:F1} | {(float)nShots / m,5:F1} | ")
                + Inv($"{(nThird > 0 ? (float)nShots / nThird : 0f),8:F3} | ")
                + Inv($"{(nBox > 0 ? (float)nShots / nBox : 0f),6:F3} | {(float)nGoals / m,5:F1}"));
            report.AppendLine();
        }

        private static string FormatRow(string label, List<Contact> rows)
        {
            int n = rows.Count;
            int c = Math.Max(1, n);
            float mq = 0f, mm = 0f, mry = 0f, vin = 0f, vout = 0f;
            int ryN = 0;
            int goal = 0, atk = 0, def = 0, corner = 0, outp = 0, loose = 0;

            for (int i = 0; i < n; i++)
            {
                mq += rows[i].Quality;
                mm += rows[i].Marginality;
                vin += rows[i].IncomingSpeed;
                vout += rows[i].OutgoingSpeed;
                if (!float.IsNaN(rows[i].ReboundYOffset)) { mry += rows[i].ReboundYOffset; ryN++; }
                switch (rows[i].Outcome)
                {
                    case Fate.Goal: goal++; break;
                    case Fate.AttackRetained: atk++; break;
                    case Fate.DefenceRecovered: def++; break;
                    case Fate.RestartCorner: corner++; break;
                    case Fate.RestartOut: outp++; break;
                    default: loose++; break;
                }
            }

            return Inv($"  {label,-8} | {n,2} | {mq / c,8:F3} | {mm / c,8:F3} | {vin / c,4:F1} | {vout / c,4:F1} | ")
                 + Inv($"{(ryN > 0 ? mry / ryN : 0f),12:F2} | ")
                 + Inv($"{goal,4} | {atk,11} | {def,12} | {corner,6} | {outp,3} | {loose,5}");
        }

        // ── Types ───────────────────────────────────────────────────────────────────────────────

        private enum Fate
        {
            Pending,
            Goal,
            AttackRetained,
            DefenceRecovered,
            RestartCorner,
            RestartOut,
            StillLoose,
        }

        private struct Contact
        {
            public int Team;
            public int ContactTick;
            public float Quality;
            public float ReactionWindow;
            public float EnvOffsetM;
            public float ReachM;
            public float Marginality;
            public float IncomingSpeed;
            public float OutgoingSpeed;
            public float ReboundYOffset;
            public float ReboundDistToGoal;
            public Fate Outcome;
        }

        private sealed class Funnel
        {
            public ulong Seed;
            public int Goals;
            public int GoalsAfterContact;
            public int Shots;
            public int ThirdEntries;
            public int BoxEntries;
            public int Chains;
            public int Settles;
            public float ChainTicks;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        private static HandlingQualityLabel BandOf(float q)
        {
            if (q >= GoalkeeperConstants.CatchThreshold) return HandlingQualityLabel.Caught;
            if (q >= GoalkeeperConstants.ParryThreshold) return HandlingQualityLabel.Parried;
            if (q >= GoalkeeperConstants.DeflectThreshold) return HandlingQualityLabel.Deflected;
            if (q >= GoalkeeperConstants.MinHandlingQuality) return HandlingQualityLabel.Spilled;
            return HandlingQualityLabel.Missed;
        }

        private static float Dist2D(UnityEngine.Vector3 p, float x, float y)
        {
            float dx = p.x - x;
            float dy = p.y - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_CONVERSION_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_CONVERSION_DIAGNOSTIC=1 to run the conversion-at-contact instrument.");
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the §5.Z.20–§5.Z.22
        /// measurement recipe (a position template so LineupSelector can field a back four).</summary>
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

        private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-03 | —      | Initial. The measurement gap gk-contact-rate-design.md §7 opens     |
// |         |            |        | with: §5.Z.22's "the added contacts are marginal touches whose      |
// |         |            |        | parries keep the ball alive" is a premise no instrument reports.    |
// |         |            |        | Report A: per-contact marginality (the real hand-envelope offset    |
// |         |            |        | §3.5.1's pointQuality does not consume), band, rebound placement    |
// |         |            |        | and fate, plus goal provenance. Report B: the creation funnel       |
// |         |            |        | (chains, third entries, BOX entries, shots per stage) behind        |
// |         |            |        | §5.Z.21's churn-bounded finding. Assertion-free (ERR-030-014).      |
#endregion
