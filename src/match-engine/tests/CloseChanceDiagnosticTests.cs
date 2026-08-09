// File:     src/match-engine/tests/CloseChanceDiagnosticTests.cs
// Created:  2026-08-03
// Modified: 2026-08-08
// Author:   —
// Spec:     Decision Tree #8 §3.1, Positioning AI #12 §3.2, Attacking AI #15 §3.4, Testing Strategy #19 (instrument class)
// Purpose:  Env-gated (TD_CREATION_DIAGNOSTIC=1) instrument for the residual
//           gk-conversion-at-contact-design.md §7 item 4 recorded and did not fix:
//           CLOSE-CHANCE CREATION, re-localized there to the final-third -> penalty-area
//           transition (measured 6.5% against football's ~40%).
//
//           That localization rests on two numbers from the §5.Z.23 funnel — 306.7
//           final-third entries and 20.0 box entries per match — and BOTH are premises this
//           file exists to test rather than inherit:
//
//             (a) "306.7 final-third entries" is a raw boundary-crossing count. A ball
//                 oscillating across x = 35 inflates it without any football happening, and
//                 an inflated denominator manufactures the 6.5%. Report C1 re-counts entries
//                 as DWELL-FILTERED EPISODES beside the raw crossings, so the ratio can be
//                 read against a denominator that survives chatter.
//
//             (b) "the transition is the bottleneck" says nothing about WHY. Two mechanisms
//                 would both produce it and want opposite fixes: nobody is in the box to
//                 receive (a support-geometry defect, #12/#15), or somebody is but the
//                 carrier never plays them in (a decision defect, #8). Report C2 measures the
//                 first — attacker occupancy of the penalty area, and the deepest attacking
//                 TARGET SLOT beside the deepest attacker, which separates "players cannot
//                 get there" from "players are not told to go there". Report C3 measures the
//                 second — what the carrier in the final third actually chooses, whether its
//                 passes progress toward goal, and whether its dribbles point at the goal at
//                 all.
//
//           Asserts nothing (the ERR-030-014 convention) — pinning measured-but-wrong
//           behaviour turns a defect into a contract. Acceptance predicates live in
//           scenarios, not here.
//
//           Run:
//             TD_CREATION_DIAGNOSTIC=1 dotnet test -c Release --filter CloseChanceDiagnostic

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The close-chance creation funnel at the stage §5.Z.23 localized it to: final third to
    /// penalty area. Full 90-minute matches on the <c>ConfigureSquads</c> path (the §5.Z.20–§5.Z.23
    /// measurement population, so the numbers are same-corpus comparable). See file header.
    /// </summary>
    [TestFixture]
    internal class CloseChanceDiagnosticTests
    {
        private static readonly int TicksPerMatch = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;

        private const float TicksPerSecond = 60.0f;

        /// <summary>IFAB penalty-area depth from the goal line (m). Local to the instrument: the
        /// engine has no penalty-area constant, and a diagnostic must not invent a production one.
        /// Same value as the §5.Z.23 instrument, so box counts are directly comparable.</summary>
        private const float PenaltyAreaDepthM = 16.5f;

        /// <summary>IFAB penalty-area half-width (m): 40.32 m total / 2.</summary>
        private const float PenaltyAreaHalfWidthM = 20.16f;

        /// <summary>Final-third depth from the defended goal line (m) — PITCH_LENGTH / 3.</summary>
        private const float FinalThirdDepthM = MatchEngineConstants.PITCH_LENGTH_M / 3.0f;

        /// <summary>Ticks the ball must stay OUT of the final third before an episode is declared
        /// closed (1.0 s at 60 Hz). Without it, a ball rattling across x = 35 opens a fresh episode
        /// on every crossing — which is exactly the artifact hypothesis (a) above.</summary>
        private const int EpisodeExitDwellTicks = 60;

        /// <summary>Stride at which the support-geometry sample is taken while the ball is in the
        /// final third (6 ticks = 0.1 s = one AI heartbeat, so each stride is sampled once).</summary>
        private const int SupportSampleStrideTicks = 6;

        /// <summary>
        /// The three §5.Z.20–§5.Z.23 seeds (so every number here is same-population comparable with
        /// that chain) plus three more. The extension is not cosmetic: measured on the standing three,
        /// a single <c>[GT]</c> rung spanned 15 to 65 shots and produced a 1–10 scoreline, so the
        /// per-seed spread dwarfed the between-rung difference and no dial could be fitted against it.
        /// This is the §5.Z.23 AR-1 finding restated one level up — there the estimator's WINDOW was
        /// too thin for a mean, here the CORPUS is too thin for a ladder.
        /// </summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
            0x5EED000000000004UL,
            0x00000000D1A6D05FUL,
            0x1A2B3C4D5E6F7081UL,
        };

        [Test]
        [Category("Calibration")]
        public void CloseChanceDiagnostic_ReportsFinalThirdToBoxFunnel()
        {
            RequireEnv();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var report = new StringBuilder();
            report.AppendLine("=== close-chance creation: the final-third -> penalty-area transition ===");
            report.AppendLine(Inv($"ticksPerMatch={TicksPerMatch}  seeds={Seeds.Length}  ")
                            + Inv($"thirdDepth={FinalThirdDepthM:F1} m  boxDepth={PenaltyAreaDepthM:F1} m  ")
                            + Inv($"episodeExitDwell={EpisodeExitDwellTicks} ticks"));
            report.AppendLine();

            var seasons = new List<MatchTally>();
            foreach (ulong seed in Seeds)
            {
                seasons.Add(RunMatch(report, seed));
            }

            AppendEpisodeTable(report, seasons);
            AppendSupportTable(report, seasons);
            AppendActionTable(report, seasons);

            report.AppendLine("Reading it:");
            report.AppendLine("  * raw crossings >> episodes => the 306.7 third-entry count is boundary");
            report.AppendLine("    chatter and the 6.5% ratio was computed against an inflated denominator.");
            report.AppendLine("  * attackersInBox ~ 0 while the ball is in the final third => there is nobody");
            report.AppendLine("    to pass to, and the bottleneck is SUPPORT GEOMETRY (#12/#15), not choice.");
            report.AppendLine("  * deepestSlot shallower than the box edge => the players are not merely slow");
            report.AppendLine("    to arrive, they are never TOLD to go: no target slot is inside the area.");
            report.AppendLine("  * progressivePass% low, or meanDribbleCos ~ 0 => the carrier's own choice is");
            report.AppendLine("    the bound and the defect is in #8's option scoring (the ERR-008-017 shape:");
            report.AppendLine("    a formula that omits the goal-direction term it should be dominated by).");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            TestContext.WriteLine(report.ToString());
            Assert.Pass("Diagnostic only — see the run output.");
        }

        // ── One match ───────────────────────────────────────────────────────────────────────────

        private static MatchTally RunMatch(StringBuilder report, ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            var m = new MatchTally { Seed = seed };

            int agentCount = MatchEngineConstants.TEAM_COUNT * MatchEngineConstants.PLAYERS_PER_TEAM;
            var prevHeartbeat = new int[agentCount];
            for (int i = 0; i < agentCount; i++)
            {
                prevHeartbeat[i] = int.MinValue;
            }

            bool wasInThird = false;
            int outOfThirdTicks = 0;
            int prevShots = 0;
            int prevHome = 0, prevAway = 0;
            var ep = Episode.None;

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                int holder = engine.PossessingAgentId;
                int holderTeam = holder >= 0 ? engine.AgentTeamId(holder) : -1;

                int shotsNow = engine.TestOnly_ShotContacts;
                int shotsThisTick = shotsNow - prevShots;
                prevShots = shotsNow;

                int goalsThisTick = (engine.HomeScore - prevHome) + (engine.AwayScore - prevAway);
                prevHome = engine.HomeScore;
                prevAway = engine.AwayScore;

                // ── Which end is the ball in? ────────────────────────────────────
                // Attacking third of team 0 is team 1's end (goal at x = PITCH_LENGTH) and
                // vice versa. Depth is measured from the DEFENDED goal line.
                int attackingEnd = -1;                     // team whose ATTACKING third the ball is in
                float depthHome = MatchEngineConstants.PITCH_LENGTH_M - ballPos.x;  // team 0 attacks x = 105
                float depthAway = ballPos.x;                                        // team 1 attacks x = 0
                float depth;
                if (depthHome <= FinalThirdDepthM) { attackingEnd = 0; depth = depthHome; }
                else if (depthAway <= FinalThirdDepthM) { attackingEnd = 1; depth = depthAway; }
                else { depth = float.MaxValue; }

                bool inThird = attackingEnd >= 0;
                bool inBox = inThird
                             && depth <= PenaltyAreaDepthM
                             && Math.Abs(ballPos.y - MatchEngineConstants.PITCH_WIDTH_M * 0.5f)
                                <= PenaltyAreaHalfWidthM;

                if (inThird && !wasInThird) m.RawCrossings++;
                wasInThird = inThird;

                // ── Episode bookkeeping (dwell-filtered) ─────────────────────────
                if (inThird)
                {
                    outOfThirdTicks = 0;
                    if (!ep.Open)
                    {
                        ep = new Episode
                        {
                            Open = true,
                            End = attackingEnd,
                            StartTick = tick,
                            MinDepth = depth,
                            ReachedBox = false,
                            Shots = 0,
                            Goals = 0,
                        };
                    }

                    // An episode is attributed to ONE end; a ball crossing straight from one
                    // final third to the other (a long clearance) closes the first and opens
                    // the second rather than merging into a single mislabelled episode.
                    if (ep.End != attackingEnd)
                    {
                        CloseEpisode(m, ref ep, tick, EpisodeOutcome.Cleared);
                        ep = new Episode
                        {
                            Open = true,
                            End = attackingEnd,
                            StartTick = tick,
                            MinDepth = depth,
                            ReachedBox = false,
                            Shots = 0,
                            Goals = 0,
                        };
                    }

                    if (depth < ep.MinDepth) ep.MinDepth = depth;
                    if (inBox) ep.ReachedBox = true;
                    ep.Shots += shotsThisTick;
                    ep.Goals += goalsThisTick;

                    // ── Report C2: support geometry ──────────────────────────────
                    if (tick % SupportSampleStrideTicks == 0)
                    {
                        SampleSupport(engine, m, attackingEnd);
                    }

                    // ── Report C3: what the carrier decided ──────────────────────
                    if (holderTeam == attackingEnd && holder >= 0)
                    {
                        SampleCarrierDecision(engine, m, holder, attackingEnd, prevHeartbeat);
                    }
                }
                else if (ep.Open)
                {
                    outOfThirdTicks++;
                    if (outOfThirdTicks >= EpisodeExitDwellTicks)
                    {
                        // Attribute the exit: possession with the defending team is a turnover;
                        // possession retained is a retreat; nobody holding it is a loose exit
                        // (a clearance or a restart).
                        EpisodeOutcome outcome =
                            holderTeam < 0 ? EpisodeOutcome.Cleared
                            : holderTeam == ep.End ? EpisodeOutcome.Retreated
                            : EpisodeOutcome.TurnedOver;
                        CloseEpisode(m, ref ep, tick - EpisodeExitDwellTicks, outcome);
                    }
                }
            }

            if (ep.Open) CloseEpisode(m, ref ep, TicksPerMatch, EpisodeOutcome.Cleared);

            m.Goals = engine.HomeScore + engine.AwayScore;
            m.Shots = prevShots;

            report.AppendLine(Inv($"seed 0x{seed:X16}   final {engine.HomeScore}-{engine.AwayScore}   ")
                            + Inv($"rawCrossings={m.RawCrossings}  episodes={m.Episodes}  ")
                            + Inv($"boxEpisodes={m.BoxEpisodes}  shots={m.Shots}"));
            return m;
        }

        private static void CloseEpisode(MatchTally m, ref Episode ep, int endTick, EpisodeOutcome outcome)
        {
            m.Episodes++;
            m.EpisodeTicks += endTick - ep.StartTick;
            m.MinDepthSum += ep.MinDepth;
            if (ep.ReachedBox) m.BoxEpisodes++;
            if (ep.Shots > 0) m.ShotEpisodes++;
            m.EpisodeShots += ep.Shots;
            switch (outcome)
            {
                case EpisodeOutcome.TurnedOver: m.EndTurnover++; break;
                case EpisodeOutcome.Retreated: m.EndRetreat++; break;
                default: m.EndCleared++; break;
            }
            ep = Episode.None;
        }

        /// <summary>
        /// Report C2 sample: how many of the attacking team's outfield players are inside the
        /// penalty area, how deep the deepest one is, and — the discriminator — how deep the
        /// deepest attacking TARGET SLOT is. A deepest slot outside the box means the shape never
        /// asks anyone to enter it, which no amount of player speed or decision quality can fix.
        /// </summary>
        private static void SampleSupport(MatchEngine engine, MatchTally m, int attackingTeam)
        {
            float goalX = attackingTeam == 0 ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            float halfWidth = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

            int inBox = 0;
            float deepestAgent = float.MaxValue;   // depth from the attacked goal line
            float deepestSlot = float.MaxValue;
            int runners = 0;
            float runnerSlot = float.MaxValue;

            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int i = attackingTeam * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                if (engine.TestOnly_IsGoalkeeper(i) || engine.TestOnly_IsSentOff(i)) continue;

                UnityEngine.Vector2 p = engine.TestOnly_AgentSnapshot(i).Position;
                float d = Math.Abs(p.x - goalX);
                if (d < deepestAgent) deepestAgent = d;
                if (d <= PenaltyAreaDepthM && Math.Abs(p.y - halfWidth) <= PenaltyAreaHalfWidthM) inBox++;

                UnityEngine.Vector2 slot = engine.TestOnly_FormationSlot(i);
                float ds = Math.Abs(slot.x - goalX);
                if (ds < deepestSlot) deepestSlot = ds;

                // The #15 run gate, measured rather than assumed: a committed RUNNER is the only
                // role that carries run parameters, and only an IN_POSSESSION team emits any.
                if (engine.TestOnly_AttackIntent(attackingTeam, i).RunParameters.HasValue)
                {
                    runners++;
                    if (ds < runnerSlot) runnerSlot = ds;
                }
            }

            PositioningAI.Phase phase = engine.TestOnly_PositioningPhase(attackingTeam);
            m.PhaseHist[(int)phase]++;

            // The InPoss gate defect's own quantities, sampled in lockstep with PhaseHist so every
            // new column shares C2's existing denominator (m.SupportSamples). ownerless = the engine
            // holds no possessor at all, which is what makes the phase classifier fall through to its
            // ball-velocity branch instead of the possession-based one. inFlight is read alongside it
            // because a pass in flight is the one case where "nobody owns it yet" is not the defect —
            // the receiver is about to.
            if (engine.TestOnly_PossessingAgentId < 0) m.OwnerlessSamples++;
            if (engine.TestOnly_PassInFlightReceiverId >= 0) m.PassInFlightSamples++;

            // The DEFENDING team's own phase, computed from the MIRRORED snapshot (MatchEngine.cs
            // ~line 3053 hand-flips BallVxFiltered's sign for the away side) — an away-side sign
            // error there is invisible in the attacking-only PhaseHist above.
            PositioningAI.Phase defPhase = engine.TestOnly_PositioningPhase(1 - attackingTeam);
            m.DefPhaseHist[(int)defPhase]++;

            if (phase == PositioningAI.Phase.InPoss)
            {
                // The conditional that actually matters. Unconditional box occupancy pools settled
                // attacks with the far more common "ball is in this third but nobody owns it", and
                // football does not fill the box in the second case either. The question a support-
                // geometry defect has to answer is what happens when a team IS in possession there.
                m.InPossSamples++;
                m.InPossInBoxSum += inBox;
                m.InPossDeepestSlotSum += deepestSlot;
            }

            if (runners > 0)
            {
                m.RunnerSamples++;
                m.RunnerCountSum += runners;
                m.RunnerSlotDepthSum += runnerSlot;
            }

            m.SupportSamples++;
            m.AttackersInBoxSum += inBox;
            if (inBox < m.AttackersInBoxHist.Length) m.AttackersInBoxHist[inBox]++;
            else m.AttackersInBoxHist[m.AttackersInBoxHist.Length - 1]++;
            m.DeepestAgentDepthSum += deepestAgent;
            m.DeepestSlotDepthSum += deepestSlot;
            if (deepestSlot <= PenaltyAreaDepthM) m.SamplesWithSlotInBoxDepth++;
        }

        /// <summary>
        /// Report C3 sample: the carrier's newly-stamped heartbeat decision while it holds the ball
        /// in the final third. Sampled on HeartbeatTick CHANGE so one decision is counted once, not
        /// once per physics tick it survives.
        /// </summary>
        private static void SampleCarrierDecision(
            MatchEngine engine, MatchTally m, int holder, int attackingTeam, int[] prevHeartbeat)
        {
            AgentAction action = engine.TestOnly_DtLastAction(holder);
            if (action.HeartbeatTick == prevHeartbeat[holder]) return;
            prevHeartbeat[holder] = action.HeartbeatTick;

            float goalX = attackingTeam == 0 ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            float halfWidth = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            var goal = new UnityEngine.Vector2(goalX, halfWidth);

            UnityEngine.Vector2 pos = engine.TestOnly_AgentSnapshot(holder).Position;
            float dSelf = UnityEngine.Vector2.Distance(pos, goal);

            switch (action.Type)
            {
                case ActionType.PASS:
                {
                    m.CarrierPass++;
                    float dTarget = UnityEngine.Vector2.Distance(action.TargetPosition, goal);
                    float gain = dSelf - dTarget;             // > 0 = the ball advances toward goal
                    m.PassGainSum += gain;
                    if (gain > 0f) m.PassProgressive++;
                    float tDepth = Math.Abs(action.TargetPosition.x - goalX);
                    if (tDepth <= PenaltyAreaDepthM
                        && Math.Abs(action.TargetPosition.y - halfWidth) <= PenaltyAreaHalfWidthM)
                    {
                        m.PassIntoBox++;
                    }
                    break;
                }
                case ActionType.DRIBBLE:
                {
                    m.CarrierDribble++;
                    UnityEngine.Vector2 move = action.TargetPosition - pos;
                    UnityEngine.Vector2 toGoal = goal - pos;
                    float lm = move.magnitude, lg = toGoal.magnitude;
                    if (lm > 1e-4f && lg > 1e-4f)
                    {
                        float cos = UnityEngine.Vector2.Dot(move / lm, toGoal / lg);
                        m.DribbleCosSum += cos;
                        m.DribbleCosN++;
                        if (cos > 0f) m.DribbleForward++;
                    }
                    break;
                }
                case ActionType.SHOOT:
                    m.CarrierShoot++;
                    m.ShootDistSum += dSelf;
                    break;
                case ActionType.HOLD:
                    m.CarrierHold++;
                    break;
                default:
                    m.CarrierOther++;
                    break;
            }
        }

        // ── Aggregate tables ────────────────────────────────────────────────────────────────────

        private static void AppendEpisodeTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C1. FINAL-THIRD EPISODES — the denominator, re-counted.");
            report.AppendLine("  rawCross = the §5.Z.23 boundary-crossing count (comparable to its 306.7).");
            report.AppendLine("  episodes = the same spells with a 1 s exit dwell, so a ball rattling across");
            report.AppendLine("  x = 35 counts once. box% / shot% are per EPISODE. Football reference:");
            report.AppendLine("  ~110 final-third entries, ~45 box entries (~40%), ~25 shots per match.");
            report.AppendLine("  seed             | rawCross | episodes | meanLen(s) | meanMinDepth | box% | shot% | shots | turnover% | retreat% | cleared%");

            int nRaw = 0, nEp = 0, nBox = 0, nShotEp = 0, nShots = 0, nTo = 0, nRet = 0, nCl = 0;
            float epTicks = 0f, minDepth = 0f;

            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                nRaw += m.RawCrossings; nEp += m.Episodes; nBox += m.BoxEpisodes;
                nShotEp += m.ShotEpisodes; nShots += m.Shots;
                nTo += m.EndTurnover; nRet += m.EndRetreat; nCl += m.EndCleared;
                epTicks += m.EpisodeTicks; minDepth += m.MinDepthSum;

                report.AppendLine(FormatEpisodeRow(Inv($"0x{m.Seed:X16}"), m.RawCrossings, m.Episodes,
                    m.EpisodeTicks, m.MinDepthSum, m.BoxEpisodes, m.ShotEpisodes, m.Shots,
                    m.EndTurnover, m.EndRetreat, m.EndCleared));
            }

            int matches = Math.Max(1, ms.Count);
            report.AppendLine(FormatEpisodeRow("per match         ", nRaw / matches, nEp / matches,
                epTicks / matches, minDepth / matches, nBox / matches, nShotEp / matches, nShots / matches,
                nTo / matches, nRet / matches, nCl / matches));
            report.AppendLine();
        }

        private static string FormatEpisodeRow(
            string label, int raw, int eps, float epTicks, float minDepthSum,
            int box, int shotEps, int shots, int to, int ret, int cl)
        {
            int e = Math.Max(1, eps);
            return Inv($"  {label,-16} | {raw,8} | {eps,8} | {epTicks / e / TicksPerSecond,10:F2} | ")
                 + Inv($"{minDepthSum / e,12:F1} | {(float)box / e,4:P0} | {(float)shotEps / e,5:P0} | ")
                 + Inv($"{shots,5} | {(float)to / e,9:P0} | {(float)ret / e,8:P0} | {(float)cl / e,8:P0}");
        }

        private static void AppendSupportTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C2. SUPPORT GEOMETRY while the ball is in the final third (0.1 s samples).");
            report.AppendLine("  attackersInBox  = attacking outfield players inside the 16.5 x 40.32 m area.");
            report.AppendLine("  deepestAgent    = distance from the attacked goal line to the most advanced");
            report.AppendLine("                    attacker. deepestSlot = the same for the most advanced");
            report.AppendLine("                    TARGET SLOT #12 composed for them this heartbeat.");
            report.AppendLine(Inv($"  A deepestSlot above {PenaltyAreaDepthM:F1} m means no player is being ASKED into the box."));
            report.AppendLine("  Football: 2-4 attackers in the box on a settled final-third possession.");
            report.AppendLine("  runner% = share of samples where #15 had a LIVE committed RUNNER (an intent");
            report.AppendLine("  whose ValidThroughTick is this stride); runnerSlot = that runner's own target.");
            report.AppendLine("  seed             | samples | meanInBox | inBox=0 | =1 | =2 | 3+ | deepestAgent | deepestSlot | slotInBox% | runner% | runnerSlot");

            int nS = 0, nBoxSum = 0, nSlotIn = 0, nInPoss = 0, nRunS = 0, nIpBox = 0;
            int nOwnerless = 0, nPassInFlight = 0;
            var hist = new int[4];
            var phist = new int[4];
            var defPhist = new int[4];
            float dAgent = 0f, dSlot = 0f, dRunSlot = 0f, ipSlot = 0f;

            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                nS += m.SupportSamples; nBoxSum += m.AttackersInBoxSum; nSlotIn += m.SamplesWithSlotInBoxDepth;
                dAgent += m.DeepestAgentDepthSum; dSlot += m.DeepestSlotDepthSum;
                nInPoss += m.InPossSamples; nRunS += m.RunnerSamples; dRunSlot += m.RunnerSlotDepthSum;
                nIpBox += m.InPossInBoxSum; ipSlot += m.InPossDeepestSlotSum;
                nOwnerless += m.OwnerlessSamples; nPassInFlight += m.PassInFlightSamples;
                for (int k = 0; k < hist.Length; k++) hist[k] += m.AttackersInBoxHist[k];
                for (int k = 0; k < phist.Length; k++) phist[k] += m.PhaseHist[k];
                for (int k = 0; k < defPhist.Length; k++) defPhist[k] += m.DefPhaseHist[k];

                report.AppendLine(FormatSupportRow(Inv($"0x{m.Seed:X16}"), m));
            }

            var pooled = new MatchTally
            {
                SupportSamples = nS, AttackersInBoxSum = nBoxSum, SamplesWithSlotInBoxDepth = nSlotIn,
                DeepestAgentDepthSum = dAgent, DeepestSlotDepthSum = dSlot,
                InPossSamples = nInPoss, RunnerSamples = nRunS, RunnerSlotDepthSum = dRunSlot,
                InPossInBoxSum = nIpBox, InPossDeepestSlotSum = ipSlot,
            };
            for (int k = 0; k < hist.Length; k++) pooled.AttackersInBoxHist[k] = hist[k];

            report.AppendLine(FormatSupportRow("pooled           ", pooled));
            report.AppendLine();

            // The conditional cut. The unconditional row above pools settled attacks with the far
            // more common "the ball is in this third and nobody owns it"; football does not fill the
            // box in the second case either, so only this row can convict the support geometry.
            int ps = Math.Max(1, nS);
            int ip = Math.Max(1, nInPoss);
            report.AppendLine("  #12 committed phase while the ball is in the final third, and the");
            report.AppendLine("  IN_POSSESSION-conditional support numbers:");
            report.AppendLine(Inv($"    InPoss {(float)phist[0] / ps,6:P1} | OutOfPoss {(float)phist[1] / ps,6:P1} | ")
                            + Inv($"TransToAtk {(float)phist[2] / ps,6:P1} | TransToDef {(float)phist[3] / ps,6:P1}"));
            report.AppendLine(Inv($"    while IN_POSSESSION: meanAttackersInBox={(float)nIpBox / ip:F2}  ")
                            + Inv($"deepestSlot={ipSlot / ip:F1} m  (box edge {PenaltyAreaDepthM:F1} m)"));
            report.AppendLine();

            // ── The InPoss gate defect, sized ──────────────────────────────
            // ownerless% is the share of these same samples where the engine holds NO possessor at
            // all (TestOnly_PossessingAgentId < 0) — that is exactly what makes #12's phase
            // classifier fall through to its ball-velocity branch instead of a possession-based one,
            // and it is the quantity a fix converts. inFlight% is read alongside it because a pass
            // in flight (TestOnly_PassInFlightReceiverId >= 0) is the one case where "nobody owns it
            // yet" is not itself the defect — a receiver is about to. Pre-fix, inFlight% reads 0%
            // everywhere: the accessor is new and nothing wires a receiver id yet.
            //
            // ATK is broken out per seed here for the first time (previously pooled only), and a DEF
            // row is added beside it. The defending team's phase is read off a MIRRORED snapshot with
            // a hand-written BallVxFiltered sign flip at MatchEngine.cs:3053 — an away-side sign error
            // there would be invisible in the attacking-only figure above, but shows up here as a
            // DEF histogram that does not mirror the ATK one the way it should.
            report.AppendLine("  #12 InPoss-gate sizing, per seed then pooled:");
            report.AppendLine("  seed             | samples | ownerless% | inFlight% | ATK: InPoss/OutOfPoss/TransAtk/TransDef | DEF: InPoss/OutOfPoss/TransAtk/TransDef");

            for (int i = 0; i < ms.Count; i++)
            {
                report.AppendLine(FormatGateRow(Inv($"0x{ms[i].Seed:X16}"), ms[i]));
            }

            report.AppendLine(FormatGateRow("pooled           ",
                nS, nOwnerless, nPassInFlight, phist, defPhist));
            report.AppendLine();
        }

        private static string FormatGateRow(string label, MatchTally m) =>
            FormatGateRow(label, m.SupportSamples, m.OwnerlessSamples, m.PassInFlightSamples,
                m.PhaseHist, m.DefPhaseHist);

        private static string FormatGateRow(
            string label, int samples, int ownerless, int passInFlight, int[] atkPhase, int[] defPhase)
        {
            int s = Math.Max(1, samples);
            return Inv($"  {label,-16} | {samples,7} | {(float)ownerless / s,10:P1} | ")
                 + Inv($"{(float)passInFlight / s,9:P1} | ")
                 + Inv($"{(float)atkPhase[0] / s,4:P0}/{(float)atkPhase[1] / s,4:P0}/")
                 + Inv($"{(float)atkPhase[2] / s,4:P0}/{(float)atkPhase[3] / s,4:P0} | ")
                 + Inv($"{(float)defPhase[0] / s,4:P0}/{(float)defPhase[1] / s,4:P0}/")
                 + Inv($"{(float)defPhase[2] / s,4:P0}/{(float)defPhase[3] / s,4:P0}");
        }

        private static string FormatSupportRow(string label, MatchTally m)
        {
            int s = Math.Max(1, m.SupportSamples);
            int r = Math.Max(1, m.RunnerSamples);
            int[] hist = m.AttackersInBoxHist;
            return Inv($"  {label,-16} | {m.SupportSamples,7} | {(float)m.AttackersInBoxSum / s,9:F2} | ")
                 + Inv($"{(float)hist[0] / s,7:P0} | {(float)hist[1] / s,3:P0} | {(float)hist[2] / s,3:P0} | ")
                 + Inv($"{(float)hist[3] / s,3:P0} | {m.DeepestAgentDepthSum / s,12:F1} | ")
                 + Inv($"{m.DeepestSlotDepthSum / s,11:F1} | {(float)m.SamplesWithSlotInBoxDepth / s,10:P0} | ")
                 + Inv($"{(float)m.RunnerSamples / s,7:P0} | {m.RunnerSlotDepthSum / r,10:F1}");
        }

        private static void AppendActionTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C3. WHAT THE CARRIER DECIDES in the final third (one row per heartbeat decision).");
            report.AppendLine("  passGain  = carrier's distance to goal MINUS the pass target's, in metres:");
            report.AppendLine("              positive means the ball is being moved toward the goal.");
            report.AppendLine("  intoBox%  = share of passes whose target position is inside the penalty area.");
            report.AppendLine("  dribCos   = mean cosine between the chosen dribble direction and the direction");
            report.AppendLine("              to the goal. ~0 means the choice is blind to where the goal is.");
            report.AppendLine("  seed             | decisions | pass% | drib% | shoot% | hold% | passGain(m) | prog% | intoBox% | dribCos | dribFwd% | shootDist");

            int nDec = 0, nP = 0, nD = 0, nS = 0, nH = 0, nO = 0, nProg = 0, nIntoBox = 0, nFwd = 0, nCosN = 0;
            float gain = 0f, cos = 0f, shootDist = 0f;

            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                nP += m.CarrierPass; nD += m.CarrierDribble; nS += m.CarrierShoot;
                nH += m.CarrierHold; nO += m.CarrierOther;
                nProg += m.PassProgressive; nIntoBox += m.PassIntoBox; nFwd += m.DribbleForward;
                nCosN += m.DribbleCosN;
                gain += m.PassGainSum; cos += m.DribbleCosSum; shootDist += m.ShootDistSum;
                nDec += m.Decisions;

                report.AppendLine(FormatActionRow(Inv($"0x{m.Seed:X16}"), m));
            }

            var pooled = new MatchTally
            {
                CarrierPass = nP, CarrierDribble = nD, CarrierShoot = nS, CarrierHold = nH,
                CarrierOther = nO, PassProgressive = nProg, PassIntoBox = nIntoBox,
                DribbleForward = nFwd, DribbleCosN = nCosN, PassGainSum = gain,
                DribbleCosSum = cos, ShootDistSum = shootDist,
            };
            report.AppendLine(FormatActionRow("pooled           ", pooled));
            report.AppendLine();
        }

        private static string FormatActionRow(string label, MatchTally m)
        {
            int dec = Math.Max(1, m.Decisions);
            int p = Math.Max(1, m.CarrierPass);
            int c = Math.Max(1, m.DribbleCosN);
            int s = Math.Max(1, m.CarrierShoot);
            return Inv($"  {label,-16} | {m.Decisions,9} | {(float)m.CarrierPass / dec,5:P0} | ")
                 + Inv($"{(float)m.CarrierDribble / dec,5:P0} | {(float)m.CarrierShoot / dec,6:P0} | ")
                 + Inv($"{(float)m.CarrierHold / dec,5:P0} | {m.PassGainSum / p,11:F2} | ")
                 + Inv($"{(float)m.PassProgressive / p,5:P0} | {(float)m.PassIntoBox / p,8:P0} | ")
                 + Inv($"{m.DribbleCosSum / c,7:F3} | {(float)m.DribbleForward / c,8:P0} | ")
                 + Inv($"{m.ShootDistSum / s,9:F1}");
        }

        // ── Types ───────────────────────────────────────────────────────────────────────────────

        private enum EpisodeOutcome
        {
            TurnedOver,
            Retreated,
            Cleared,
        }

        private struct Episode
        {
            public bool Open;
            public int End;
            public int StartTick;
            public float MinDepth;
            public bool ReachedBox;
            public int Shots;
            public int Goals;

            public static Episode None => default;
        }

        private sealed class MatchTally
        {
            public ulong Seed;
            public int Goals;
            public int Shots;

            // C1
            public int RawCrossings;
            public int Episodes;
            public float EpisodeTicks;
            public float MinDepthSum;
            public int BoxEpisodes;
            public int ShotEpisodes;
            public int EpisodeShots;
            public int EndTurnover;
            public int EndRetreat;
            public int EndCleared;

            // C2
            public int SupportSamples;
            public int AttackersInBoxSum;
            public readonly int[] AttackersInBoxHist = new int[4];
            public float DeepestAgentDepthSum;
            public float DeepestSlotDepthSum;
            public int SamplesWithSlotInBoxDepth;
            public int InPossSamples;
            public int RunnerSamples;
            public int RunnerCountSum;
            public float RunnerSlotDepthSum;
            public int InPossInBoxSum;
            public float InPossDeepestSlotSum;
            public readonly int[] PhaseHist = new int[4];   // InPoss / OutOfPoss / TransToAtk / TransToDef
            public int OwnerlessSamples;
            public int PassInFlightSamples;
            public readonly int[] DefPhaseHist = new int[4];   // InPoss / OutOfPoss / TransToAtk / TransToDef

            // C3
            public int CarrierPass;
            public int CarrierDribble;
            public int CarrierShoot;
            public int CarrierHold;
            public int CarrierOther;
            public int PassProgressive;
            public int PassIntoBox;
            public float PassGainSum;
            public int DribbleForward;
            public int DribbleCosN;
            public float DribbleCosSum;
            public float ShootDistSum;

            public int Decisions =>
                CarrierPass + CarrierDribble + CarrierShoot + CarrierHold + CarrierOther;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        private static void RequireEnv()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_CREATION_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_CREATION_DIAGNOSTIC=1 to run the close-chance creation instrument.");
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the §5.Z.20–§5.Z.23
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
// | 1.0     | 2026-08-03 | —      | Initial. The two premises gk-conversion-at-contact-design.md §7     |
// |         |            |        | item 4 leaves untested: that 306.7 raw final-third crossings are a  |
// |         |            |        | valid denominator, and that "the transition is the bottleneck"      |
// |         |            |        | identifies a mechanism. C1 re-counts entries as dwell-filtered      |
// |         |            |        | episodes with their outcomes; C2 measures attacker occupancy of the |
// |         |            |        | box and — the discriminator — the deepest composed TARGET SLOT;     |
// |         |            |        | C3 measures the carrier's own decision mix, pass progression and    |
// |         |            |        | dribble goal-direction cosine. Assertion-free (ERR-030-014).        |
// | 1.1     | 2026-08-08 | —      | C2: added ownerless% (share of samples with no possessor at all —   |
// |         |            |        | TestOnly_PossessingAgentId < 0, the InPoss gate defect's own        |
// |         |            |        | quantity) and inFlight% (TestOnly_PassInFlightReceiverId >= 0, the  |
// |         |            |        | new accessor landing alongside this file — reads 0% pre-fix); split |
// |         |            |        | the pooled-only #12 phase histogram out per seed for the attacking  |
// |         |            |        | team, and added the defending team's phase histogram (per seed and |
// |         |            |        | pooled) via TestOnly_PositioningPhase(1 - attackingTeam), to make   |
// |         |            |        | an away-side sign error in the MatchEngine.cs:3053 mirrored         |
// |         |            |        | BallVxFiltered flip visible. Assertion-free throughout.             |
#endregion
