// File:     src/match-engine/tests/CloseChanceDiagnosticTests.cs
// Created:  2026-08-03
// Modified: 2026-08-08
// Author:   —
// Spec:     Decision Tree #8 §3.1, Positioning AI #12 §3.2, Attacking AI #15 §3.4, Pass Mechanics #5 §3.1.2, Testing Strategy #19 (instrument class)
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
//                 all. Report C4 (v1.2) follows the PASS itself once the carrier chooses one —
//                 what type #8 derives, whether the two DerivePassType gates that block
//                 ThroughBall/Cross are ever satisfied, how the pass resolves, how long the
//                 ball takes to settle, and how far the intended receiver was from it when it
//                 did. C2 also gained a counterfactual race: is the space 8 m goalward of the
//                 ball one the attacking side would actually win a foot race to?
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

using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PassMechanics;
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

        /// <summary>Number of teams (Report C4 arrays are indexed by team so home/away never
        /// pool silently). [CROSS] MatchEngineConstants.TEAM_COUNT.</summary>
        private static readonly int TeamCount = MatchEngineConstants.TEAM_COUNT;

        /// <summary>Number of <see cref="PassType"/> members. Read off the enum itself rather than
        /// written down as a literal, so a future append to PassType.cs (permitted — see that
        /// file's ORDINAL STABILITY note) cannot silently desync this instrument's array sizes.</summary>
        private static readonly int PassTypeCount = Enum.GetValues(typeof(PassType)).Length;

        /// <summary>
        /// Report C2 item 7 (v1.2): distance (m) projected goalward of the ball for the
        /// counterfactual-race sample. ARBITRARY-BY-INSTRUMENT — this is not a production value
        /// and answers no spec formula; it exists only to ask "is there space someone would win
        /// a foot race to" at a distance further than a give-and-go but short of a hopeful long ball.
        /// </summary>
        private const float CounterfactualRaceProjectionM = 8.0f;

        /// <summary>Report C4 item 4: coarse dwell buckets, named directly in this file's own brief
        /// rather than chosen freely — &lt;0.5 s / 0.5-1 s / 1-2 s / 2-4 s / &gt;4 s (4 boundaries,
        /// 5 buckets). Not a production threshold.</summary>
        private static readonly float[] TimeToRestBucketBoundariesSec = { 0.5f, 1f, 2f, 4f };

        /// <summary>Bucket count implied by <see cref="TimeToRestBucketBoundariesSec"/> (boundaries + 1).
        /// Named separately rather than re-deriving `.Length + 1` at every array-sizing call site.</summary>
        private const int TimeToRestBucketCount = 5;

        /// <summary>Report C4 item 5: coarse receiver-to-ball-gap buckets (m). ARBITRARY-BY-INSTRUMENT —
        /// the brief asks for "median-ish buckets" without naming edges; chosen to span
        /// walking-pace-recoverable (&lt;1 m) through clearly-lost (&gt;10 m).</summary>
        private static readonly float[] ReceiverGapBucketBoundariesM = { 1f, 2f, 5f, 10f };

        /// <summary>Bucket count implied by <see cref="ReceiverGapBucketBoundariesM"/> (boundaries + 1).</summary>
        private const int ReceiverGapBucketCount = 5;

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
            AppendPassOutcomeTables(report, seasons);

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
            report.AppendLine("  * (v1.2) ThroughBall/Cross% ~ 0 in C4a => check inBand15-30%/fastReceiver%");
            report.AppendLine("    to see which of DerivePassType's two gates never opens; if inBand is high");
            report.AppendLine("    but fastReceiver is ~0, no team-mate is ever moving toward goal fast enough");
            report.AppendLine("    when a pass is chosen — a support-geometry/movement defect, not #8's own.");
            report.AppendLine("  * (v1.2) C4c completion% far below football's for a given type => that type");
            report.AppendLine("    is being selected somewhere it should not be (#8), not merely under-hit.");
            report.AppendLine("  * (v1.2) attackerNearer% (C2 item 7) well below 50% at the box edge => a ball");
            report.AppendLine("    played into the space the funnel needs would usually be lost anyway — the");
            report.AppendLine("    bottleneck is the RACE, not the decision to play the ball forward at all.");

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

            // ── Report C4 (v1.2) state, carried across ticks ──────────────────
            // prevHolderForLaunch/prevPassReceiverForLaunch are last tick's SETTLED values (read
            // again at the bottom of the loop body), so a launch is detected on the -1 -> R
            // transition exactly as TestOnly_PassInFlightReceiverId's own doc describes it.
            int prevHolderForLaunch = MatchEngineConstants.NO_POSSESSION;
            int prevPassReceiverForLaunch = MatchEngineConstants.NO_POSSESSION;
            PendingPass pendingPass = PendingPass.None;

            for (int tick = 0; tick < TicksPerMatch; tick++)
            {
                engine.RunTick();

                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                int holder = engine.PossessingAgentId;
                int holderTeam = holder >= 0 ? engine.AgentTeamId(holder) : -1;
                int passReceiverNow = engine.TestOnly_PassInFlightReceiverId;

                int shotsNow = engine.TestOnly_ShotContacts;
                int shotsThisTick = shotsNow - prevShots;
                prevShots = shotsNow;

                int goalsThisTick = (engine.HomeScore - prevHome) + (engine.AwayScore - prevAway);
                prevHome = engine.HomeScore;
                prevAway = engine.AwayScore;

                // ── Report C4: resolve a pass tracked since an earlier tick ──────
                // Runs every tick regardless of where the ball now is — a pass launched inside
                // the final third can be intercepted and cleared well outside it.
                if (pendingPass.Active)
                {
                    ResolvePendingPass(engine, m, ref pendingPass, tick, holder, holderTeam, goalsThisTick);
                }

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

                // ── Report C4: pass launch detection ─────────────────────────────
                // The -1 -> R transition on the latch (see TestOnly_PassInFlightReceiverId's own
                // doc). Unconditional on inThird — TryLaunchPass applies the "kicker's own
                // attacking third" restriction itself, using the attackingEnd just computed above,
                // and is a no-op for anything outside it.
                if (prevPassReceiverForLaunch == MatchEngineConstants.NO_POSSESSION
                    && passReceiverNow != MatchEngineConstants.NO_POSSESSION)
                {
                    TryLaunchPass(engine, m, ref pendingPass, tick, prevHolderForLaunch, passReceiverNow, attackingEnd);
                }

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
                        SampleSupport(engine, m, attackingEnd, ballPos);
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
                        // Attribute the exit by TEAM POSSESSION, not the on-ball holder (v1.2 fix).
                        // holderTeam comes from TestOnly_PossessingAgentId, which is -1 for the
                        // ENTIRE FLIGHT of every pass — Report C2's own ownerless% column measures
                        // that gap directly and it reads ~86% of final-third samples. Under the OLD
                        // holderTeam-only rule this column therefore read cleared% ~= 99%,
                        // turnover% ~= 0%, retreat% ~= 0% on every one of the six seeds: it was
                        // measuring the ownerless gap, not football. Composed exactly as
                        // MatchEngine.FillPositioningSnapshot does post ERR-012-011 (MatchEngine.cs
                        // ~line 3106): the on-ball holder if there is one, ELSE the pass-in-flight
                        // receiver, ELSE genuinely nobody. docs/tracking/close-chance-creation-
                        // design.md's existing numbers for this column predate this correction and
                        // are not comparable to a re-run.
                        int teamPossessionAgent = holder >= 0 ? holder : passReceiverNow;
                        int teamPossessionTeam =
                            teamPossessionAgent >= 0 ? engine.AgentTeamId(teamPossessionAgent) : -1;
                        EpisodeOutcome outcome =
                            teamPossessionTeam < 0 ? EpisodeOutcome.Cleared
                            : teamPossessionTeam == ep.End ? EpisodeOutcome.Retreated
                            : EpisodeOutcome.TurnedOver;
                        CloseEpisode(m, ref ep, tick - EpisodeExitDwellTicks, outcome);
                    }
                }

                prevHolderForLaunch = holder;
                prevPassReceiverForLaunch = passReceiverNow;
            }

            if (ep.Open) CloseEpisode(m, ref ep, TicksPerMatch, EpisodeOutcome.Cleared);

            // A pass still in flight (or still loose, pre-resolution) at full time. Every launched
            // pass gets exactly one outcome, so the match itself closes out whatever Report C4 was
            // still tracking rather than dropping it silently.
            if (pendingPass.Active)
            {
                FinalizePending(engine, m, ref pendingPass, PassOutcomeKind.Unresolved, TicksPerMatch);
            }

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
        private static void SampleSupport(
            MatchEngine engine, MatchTally m, int attackingTeam, UnityEngine.Vector3 ballPos)
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

            SampleCounterfactualRace(engine, m, attackingTeam, ballPos);
        }

        /// <summary>
        /// Report C2 item 7 (v1.2): the counterfactual race. Projects a point
        /// <see cref="CounterfactualRaceProjectionM"/> goalward of the ball — along the same
        /// ball-to-goal-centre direction production already reasons about (mirrors the "goal"
        /// point used in <see cref="SampleCarrierDecision"/>) — and asks who is nearer to it: the
        /// nearest ATTACKING outfielder or the nearest DEFENDING one. If the defender usually wins
        /// that race, a ball played into that space is not a chance the geometry supports, whatever
        /// #8 decides to do with it.
        /// </summary>
        private static void SampleCounterfactualRace(
            MatchEngine engine, MatchTally m, int attackingTeam, UnityEngine.Vector3 ballPos)
        {
            float goalX = attackingTeam == 0 ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            float halfWidth = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            var goal = new UnityEngine.Vector2(goalX, halfWidth);
            var ball2 = new UnityEngine.Vector2(ballPos.x, ballPos.y);

            UnityEngine.Vector2 toGoal = goal - ball2;
            // A ball sitting exactly on the goal centre has no direction to project along — falls
            // back to the attacking team's own goal-to-goal axis rather than leaving the point
            // undefined. Never observed in practice (the goal centre is inside the goal frame).
            UnityEngine.Vector2 dir = toGoal.sqrMagnitude > 1e-6f
                ? toGoal.normalized
                : (attackingTeam == 0 ? UnityEngine.Vector2.right : UnityEngine.Vector2.left);

            UnityEngine.Vector2 projected = ball2 + dir * CounterfactualRaceProjectionM;
            projected.x = UnityEngine.Mathf.Clamp(projected.x, 0f, MatchEngineConstants.PITCH_LENGTH_M);
            projected.y = UnityEngine.Mathf.Clamp(projected.y, 0f, MatchEngineConstants.PITCH_WIDTH_M);

            int defendingTeam = 1 - attackingTeam;
            float nearestAttacker = float.MaxValue;
            float nearestDefender = float.MaxValue;

            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int ai = attackingTeam * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                if (!engine.TestOnly_IsGoalkeeper(ai) && !engine.TestOnly_IsSentOff(ai))
                {
                    float d = UnityEngine.Vector2.Distance(engine.TestOnly_AgentSnapshot(ai).Position, projected);
                    if (d < nearestAttacker) nearestAttacker = d;
                }

                int di = defendingTeam * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                if (!engine.TestOnly_IsGoalkeeper(di) && !engine.TestOnly_IsSentOff(di))
                {
                    float d = UnityEngine.Vector2.Distance(engine.TestOnly_AgentSnapshot(di).Position, projected);
                    if (d < nearestDefender) nearestDefender = d;
                }
            }

            // Every outfielder sent off or a keeper on both sides at once — degenerate; skip
            // rather than let a float.MaxValue pollute the mean.
            if (nearestAttacker == float.MaxValue || nearestDefender == float.MaxValue) return;

            m.RaceSamplesByTeam[attackingTeam]++;
            m.RaceAttackerDistSumByTeam[attackingTeam] += nearestAttacker;
            m.RaceDefenderDistSumByTeam[attackingTeam] += nearestDefender;
            if (nearestAttacker < nearestDefender) m.RaceAttackerNearerByTeam[attackingTeam]++;
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

        // ── Report C4: pass outcomes (v1.2) ────────────────────────────────────────────────────

        /// <summary>
        /// Report C4 launch detection: fires on the pass-in-flight latch's -1 -&gt; R transition
        /// (see <see cref="MatchEngine.TestOnly_PassInFlightReceiverId"/>'s own doc), restricted to
        /// launches while the ball is in the KICKER's attacking third per this report's brief.
        /// <para>
        /// The kicker's identity is not recorded anywhere ON THE LATCH itself — ArmPassInFlight's
        /// own <c>agentId</c> parameter is the ground truth and is not observable from outside
        /// MatchEngine. This is therefore a DERIVATION, not a recorded fact: <paramref
        /// name="prevPossessingAgent"/> is whoever the engine reports as holding the ball on the
        /// tick immediately before the latch armed. Under today's flow that is exact rather than a
        /// guess — <c>PassWorldAdapter.ApplyKick</c> calls <c>ReleasePossessionOnKick(agentId)</c>
        /// (releasing exactly that agent, and only if he currently holds the ball) one statement
        /// before <c>ArmPassInFlight(agentId, ...)</c> — but it would mislead if some future caller
        /// armed the latch without the kicker having held the ball, which nothing today does.
        /// </para>
        /// </summary>
        private static void TryLaunchPass(
            MatchEngine engine, MatchTally m, ref PendingPass pending, int tick,
            int prevPossessingAgent, int receiverId, int attackingEnd)
        {
            if (pending.Active)
            {
                // Should not happen under today's flow (see the class doc on PendingPass): the
                // previous pending pass would already have resolved via ResolvePendingPass's
                // holder branch the tick its own kicker gained the ball, since gaining the ball is
                // a precondition for kicking it again. Recorded rather than silently overwritten
                // so a future regression here is visible instead of quietly corrupting the outcome
                // counts.
                m.LaunchesSupersededPending++;
            }

            if (prevPossessingAgent < 0)
            {
                // No recorded holder the tick before the latch armed — the derivation this
                // method's own doc describes has nothing to derive from. Cannot attribute a kicker
                // or a kicking team, so the launch can be neither zone-filtered nor classified;
                // skipped rather than guessed. Not attributable to a team, hence a match-level
                // counter rather than one of the per-team arrays below.
                m.LaunchesWithNoKickerDerived++;
                pending = PendingPass.None;
                return;
            }

            int kickerTeam = engine.AgentTeamId(prevPossessingAgent);
            if (attackingEnd != kickerTeam)
            {
                // Launched while the ball was in the DEFENDING third (or midfield) relative to the
                // kicker — out of scope for this report's brief ("restricted to passes launched
                // while the ball is in the passing team's ATTACKING THIRD"). Not tracked at all,
                // not even as a diagnostic-quality miss.
                pending = PendingPass.None;
                return;
            }

            AgentAction action = engine.TestOnly_DtLastAction(prevPossessingAgent);
            bool typeKnown = action.Type == ActionType.PASS;
            if (!typeKnown)
            {
                // The kicker's last stamped DT decision was not the PASS that led to this kick —
                // plausible only if a later heartbeat decision was stamped mid-windup without
                // re-triggering the executor. Distance and pass-type measures below fall back to a
                // position-based recomputation rather than being skipped outright.
                m.LaunchesWithUnknownTypeByTeam[kickerTeam]++;
            }

            UnityEngine.Vector2 kickerPos = engine.TestOnly_AgentSnapshot(prevPossessingAgent).Position;
            UnityEngine.Vector2 receiverPos = engine.TestOnly_AgentSnapshot(receiverId).Position;
            float intendedDistance = typeKnown
                ? action.PassParams.IntendedDistance
                : UnityEngine.Vector2.Distance(kickerPos, receiverPos);

            m.LaunchesTotalByTeam[kickerTeam]++;
            if (typeKnown)
            {
                m.PassTypeCountsByTeam[kickerTeam][(int)action.PassParams.PassType]++;
            }

            // Item 2: the near-miss counter. Decision Tree #8's OptionGenerator.DerivePassType
            // (§3.1.3.4) gates ThroughBall on exactly these two predicates — a pass in the
            // (SHORT_PASS_MAX_DISTANCE, MEDIUM_PASS_MAX_DISTANCE] band AND the target's
            // velocity-toward-goal exceeding THROUGH_BALL_VEL_THRESHOLD — reused here by name
            // ([CROSS], not re-declared) so a zero ThroughBall count can be attributed to whichever
            // predicate is never true, rather than left as one unexplained zero.
            if (intendedDistance > UtilityWeights.SHORT_PASS_MAX_DISTANCE
                && intendedDistance <= UtilityWeights.MEDIUM_PASS_MAX_DISTANCE)
            {
                m.LaunchesInBandByTeam[kickerTeam]++;
            }

            // The velocity term. DerivePassType dots the RECEIVING teammate's velocity against the
            // direction from the PASSER's position to the opponent goal centre (#8's own goalDir,
            // §3.1.3 — not a direction anchored on the receiver). Reproduced exactly here. What
            // production actually evaluates this against is the PERCEIVED velocity from the
            // perception system at option-generation time (several ticks before physical contact,
            // across the pass's windup) — that value is not retained anywhere past option
            // generation, so this measures the receiver's raw physics velocity AT THE PHYSICAL
            // KICK instead. The two can differ; treat this as the closest available proxy, not a
            // bit-exact replay of what #8 saw.
            float goalX = kickerTeam == 0 ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            float halfWidth = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            UnityEngine.Vector2 goalCentre = new UnityEngine.Vector2(goalX, halfWidth);
            UnityEngine.Vector2 goalDir = (goalCentre - kickerPos).normalized;
            UnityEngine.Vector2 receiverVel = engine.TestOnly_AgentSnapshot(receiverId).Velocity;
            float velTowardGoal = UnityEngine.Vector2.Dot(receiverVel, goalDir);

            m.ReceiverVelSumByTeam[kickerTeam] += velTowardGoal;
            if (velTowardGoal > m.ReceiverVelMaxByTeam[kickerTeam]) m.ReceiverVelMaxByTeam[kickerTeam] = velTowardGoal;
            if (velTowardGoal > UtilityWeights.THROUGH_BALL_VEL_THRESHOLD)
            {
                m.LaunchesFastReceiverByTeam[kickerTeam]++;
            }

            pending = new PendingPass
            {
                Active = true,
                LaunchTick = tick,
                KickerTeam = kickerTeam,
                ReceiverId = receiverId,
                Type = typeKnown ? action.PassParams.PassType : default,
                TypeKnown = typeKnown,
                TimeToRestCaptured = false,
            };
        }

        /// <summary>
        /// Report C4 per-tick resolution of a pass tracked since <see cref="TryLaunchPass"/>. Time-
        /// to-rest (item 4) is captured independently of the final outcome — the FIRST tick at
        /// which either "somebody now holds the ball" or "the ball's speed is below the engine's
        /// own at-rest threshold" holds — and the outcome itself (item 3) is decided by whichever
        /// of "somebody now holds the ball", "a restart or goal intervened", or "still neither" is
        /// true this tick. A settled holder takes priority over a same-tick restart/goal signal: by
        /// the time either can fire, the shooter or scorer already held the ball, so the pass this
        /// pending object tracks would already have resolved via the holder branch on an earlier
        /// tick — see <see cref="PendingPass"/>.
        /// </summary>
        private static void ResolvePendingPass(
            MatchEngine engine, MatchTally m, ref PendingPass pending, int tick,
            int holder, int holderTeam, int goalsThisTick)
        {
            if (!pending.TimeToRestCaptured)
            {
                // Ball Physics' own Rolling -> Stationary predicate (BallStateMachine.cs), not an
                // instrument-invented threshold — "the engine's own at-rest threshold" per item 4.
                bool atRest = engine.BallView.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity;
                if (holder >= 0 || atRest)
                {
                    RecordTimeToRest(m, in pending, tick);
                    pending.TimeToRestCaptured = true;
                }
            }

            if (holder >= 0)
            {
                PassOutcomeKind outcome =
                    holder == pending.ReceiverId ? PassOutcomeKind.Completed
                    : holderTeam == pending.KickerTeam ? PassOutcomeKind.TurnoverTeammate
                    : PassOutcomeKind.Intercepted;
                FinalizePending(engine, m, ref pending, outcome, tick);
                return;
            }

            if (goalsThisTick > 0 || engine.RestartAppliedThisTick != RestartCue.None)
            {
                FinalizePending(engine, m, ref pending, PassOutcomeKind.Dead, tick);
            }
        }

        private static void RecordTimeToRest(MatchTally m, in PendingPass pending, int tick)
        {
            float seconds = (tick - pending.LaunchTick) / TicksPerSecond;
            m.TimeToRestSamplesByTeam[pending.KickerTeam]++;
            m.TimeToRestSecondsSumByTeam[pending.KickerTeam] += seconds;
            m.TimeToRestHistByTeam[pending.KickerTeam][TimeToRestBucketIndex(seconds)]++;
        }

        /// <summary>
        /// Finalizes a pending pass into item 3 (outcome), item 5 (receiver-to-ball gap), and — as
        /// a fallback only — item 4 (time-to-rest, for the rare pass that resolves via DEAD or
        /// UNRESOLVED before either of item 4's own conditions ever fires, e.g. an overhit cross
        /// driven straight out of play still at speed). Every launched pass ends up contributing
        /// exactly one sample to each of items 3, 4 and 5.
        /// </summary>
        private static void FinalizePending(
            MatchEngine engine, MatchTally m, ref PendingPass pending, PassOutcomeKind outcome, int tick)
        {
            if (!pending.TimeToRestCaptured)
            {
                RecordTimeToRest(m, in pending, tick);
            }

            switch (outcome)
            {
                case PassOutcomeKind.Completed:
                    m.OutcomeCompletedByTeam[pending.KickerTeam]++;
                    if (pending.TypeKnown) m.CompletedByTypeByTeam[pending.KickerTeam][(int)pending.Type]++;
                    break;
                case PassOutcomeKind.Intercepted:
                    m.OutcomeInterceptedByTeam[pending.KickerTeam]++;
                    break;
                case PassOutcomeKind.TurnoverTeammate:
                    m.OutcomeTurnoverByTeam[pending.KickerTeam]++;
                    break;
                case PassOutcomeKind.Dead:
                    m.OutcomeDeadByTeam[pending.KickerTeam]++;
                    break;
                default:
                    m.OutcomeUnresolvedByTeam[pending.KickerTeam]++;
                    break;
            }

            // Item 5: how far the intended receiver was from the ball the moment it stopped being
            // live — measured for every outcome, DEAD and UNRESOLVED included, because "would a
            // run have saved it" is exactly as meaningful when the ball went out of play near him
            // as when an opponent beat him to it.
            UnityEngine.Vector2 receiverPos = engine.TestOnly_AgentSnapshot(pending.ReceiverId).Position;
            UnityEngine.Vector3 ballPos3 = engine.BallView.Position;
            float gap = UnityEngine.Vector2.Distance(receiverPos, new UnityEngine.Vector2(ballPos3.x, ballPos3.y));
            m.ReceiverGapSamplesByTeam[pending.KickerTeam]++;
            m.ReceiverGapSumByTeam[pending.KickerTeam] += gap;
            m.ReceiverGapHistByTeam[pending.KickerTeam][ReceiverGapBucketIndex(gap)]++;

            pending = PendingPass.None;
        }

        private static int TimeToRestBucketIndex(float seconds)
        {
            for (int i = 0; i < TimeToRestBucketBoundariesSec.Length; i++)
            {
                if (seconds < TimeToRestBucketBoundariesSec[i]) return i;
            }
            return TimeToRestBucketBoundariesSec.Length;
        }

        private static int ReceiverGapBucketIndex(float metres)
        {
            for (int i = 0; i < ReceiverGapBucketBoundariesM.Length; i++)
            {
                if (metres < ReceiverGapBucketBoundariesM[i]) return i;
            }
            return ReceiverGapBucketBoundariesM.Length;
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

            // C4's pooling helper also carries item 7's race fields (SampleCounterfactualRace
            // writes into the same per-seed MatchTally the rest of C2 does) — a second small pass
            // over ms rather than folding into the scalar accumulation above, which predates it.
            for (int i = 0; i < ms.Count; i++) AddPassTally(pooled, ms[i]);

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

            // ── Item 7 (v1.2): the counterfactual race ──────────────────────
            // Lives in C2, not C4, because it samples in lockstep with the rest of this report —
            // same stride, same "ball in a final third" gate — and is about the SPACE itself,
            // independent of whether any pass is ever aimed into it.
            report.AppendLine(Inv($"  Item 7: the counterfactual race — a point {CounterfactualRaceProjectionM:F0} m ")
                             + "goalward of the ball (arbitrary-by-instrument; see");
            report.AppendLine("  CounterfactualRaceProjectionM's own doc), and who would reach it first.");
            report.AppendLine("  attackerNearer% below 50% means the defending side already owns that space");
            report.AppendLine("  before anyone plays a ball there — by attacking team, per seed then pooled.");
            report.AppendLine("  seed/team        | samples | meanAttackerDist | meanDefenderDist | attackerNearer%");

            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                for (int t = 0; t < TeamCount; t++)
                {
                    report.AppendLine(FormatRaceRow(Inv($"0x{m.Seed:X16}/{TeamLabel(t)}"), m, t));
                }
            }
            for (int t = 0; t < TeamCount; t++)
            {
                report.AppendLine(FormatRaceRow(Inv($"pooled/{TeamLabel(t)}"), pooled, t));
            }

            int raceNAll = pooled.RaceSamplesByTeam[0] + pooled.RaceSamplesByTeam[1];
            float raceAAll = pooled.RaceAttackerDistSumByTeam[0] + pooled.RaceAttackerDistSumByTeam[1];
            float raceDAll = pooled.RaceDefenderDistSumByTeam[0] + pooled.RaceDefenderDistSumByTeam[1];
            int raceNearAll = pooled.RaceAttackerNearerByTeam[0] + pooled.RaceAttackerNearerByTeam[1];
            int raceDAllDenom = Math.Max(1, raceNAll);
            report.AppendLine(Inv($"  {"pooled/ALL",-16} | {raceNAll,7} | {raceAAll / raceDAllDenom,16:F2} | ")
                             + Inv($"{raceDAll / raceDAllDenom,16:F2} | {(float)raceNearAll / raceDAllDenom,15:P0}"));
            report.AppendLine();
        }

        private static string FormatRaceRow(string label, MatchTally m, int team)
        {
            int s = Math.Max(1, m.RaceSamplesByTeam[team]);
            return Inv($"  {label,-16} | {m.RaceSamplesByTeam[team],7} | ")
                 + Inv($"{m.RaceAttackerDistSumByTeam[team] / s,16:F2} | ")
                 + Inv($"{m.RaceDefenderDistSumByTeam[team] / s,16:F2} | ")
                 + Inv($"{(float)m.RaceAttackerNearerByTeam[team] / s,15:P0}");
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

        private static void AppendPassOutcomeTables(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4. PASS OUTCOMES (v1.2) — launched only while the ball is in the PASSING");
            report.AppendLine("  team's own attacking third, per this report's brief. home = team 0, away =");
            report.AppendLine("  team 1, split throughout: this project has shipped three home/away");
            report.AppendLine("  asymmetry defects from home-only fixtures and measurement (#8 ERR-008-002).");
            report.AppendLine();

            AppendPassTypeTable(report, ms);
            AppendPassOutcomeTable(report, ms);
            AppendCompletionByTypeTable(report, ms);
            AppendTimeToRestTable(report, ms);
            AppendReceiverGapTable(report, ms);

            int noKicker = 0, superseded = 0;
            for (int i = 0; i < ms.Count; i++)
            {
                noKicker += ms[i].LaunchesWithNoKickerDerived;
                superseded += ms[i].LaunchesSupersededPending;
            }
            report.AppendLine(Inv($"  instrument health: launchesWithNoKickerDerived={noKicker}  ")
                             + Inv($"launchesSupersededPending={superseded} (both expected 0 — see"));
            report.AppendLine("  TryLaunchPass's doc; non-zero here means some launches above are");
            report.AppendLine("  undercounted, not that none were launched).");
            report.AppendLine("  PassOutcome.Invalid: NOT OBSERVABLE from this test assembly today — no");
            report.AppendLine("  TestOnly_ accessor exposes _passExecutors[agentId].LastResult.Outcome.");
            report.AppendLine("  Would need `internal PassOutcome TestOnly_PassLastOutcome(int agentId) =>");
            report.AppendLine("  _passExecutors[agentId].LastResult.Outcome;` on MatchEngine; not added here");
            report.AppendLine("  — this pass is measurement-only, no production accessor additions.");
            report.AppendLine();
        }

        private static void AppendPassTypeTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4a. PASS-TYPE DISTRIBUTION + the ThroughBall near-miss gate (#8 §3.1.3.4,");
            report.AppendLine("  DerivePassType). Percentages are of that row's own launched count (n).");
            report.AppendLine("  inBand15-30% = the IntendedDistance gate ThroughBall needs; fastReceiver% =");
            report.AppendLine("  the velocity-toward-goal gate (see file header on why this is a proxy for,");
            report.AppendLine("  not a replay of, what #8 itself evaluated at option-generation time). Both");
            report.AppendLine("  true is necessary for ThroughBall; a zero ThroughBall% with a non-zero");
            report.AppendLine("  fastReceiver% here convicts the DISTANCE gate instead, and vice versa.");
            report.AppendLine("  seed/team        |   n | Ground | Driven | Lofted | ThruBall | AerThru | Cross | Chip | unkType | inBand | fastRcv | meanVel | maxVel");

            var pooled = new MatchTally();
            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                AddPassTally(pooled, m);
                for (int t = 0; t < TeamCount; t++)
                {
                    report.AppendLine(FormatPassTypeRow(Inv($"0x{m.Seed:X16}/{TeamLabel(t)}"),
                        m.LaunchesTotalByTeam[t], m.PassTypeCountsByTeam[t], m.LaunchesWithUnknownTypeByTeam[t],
                        m.LaunchesInBandByTeam[t], m.LaunchesFastReceiverByTeam[t],
                        m.ReceiverVelSumByTeam[t], m.ReceiverVelMaxByTeam[t]));
                }
            }
            for (int t = 0; t < TeamCount; t++)
            {
                report.AppendLine(FormatPassTypeRow(Inv($"pooled/{TeamLabel(t)}"),
                    pooled.LaunchesTotalByTeam[t], pooled.PassTypeCountsByTeam[t], pooled.LaunchesWithUnknownTypeByTeam[t],
                    pooled.LaunchesInBandByTeam[t], pooled.LaunchesFastReceiverByTeam[t],
                    pooled.ReceiverVelSumByTeam[t], pooled.ReceiverVelMaxByTeam[t]));
            }

            var allType = new int[PassTypeCount];
            int allN = 0, allUnknown = 0, allInBand = 0, allFast = 0;
            float allVelSum = 0f, allVelMax = float.MinValue;
            for (int t = 0; t < TeamCount; t++)
            {
                allN += pooled.LaunchesTotalByTeam[t];
                allUnknown += pooled.LaunchesWithUnknownTypeByTeam[t];
                allInBand += pooled.LaunchesInBandByTeam[t];
                allFast += pooled.LaunchesFastReceiverByTeam[t];
                allVelSum += pooled.ReceiverVelSumByTeam[t];
                if (pooled.ReceiverVelMaxByTeam[t] > allVelMax) allVelMax = pooled.ReceiverVelMaxByTeam[t];
                for (int k = 0; k < PassTypeCount; k++) allType[k] += pooled.PassTypeCountsByTeam[t][k];
            }
            report.AppendLine(FormatPassTypeRow("pooled/ALL", allN, allType, allUnknown, allInBand, allFast, allVelSum, allVelMax));
            report.AppendLine();
        }

        private static string FormatPassTypeRow(
            string label, int n, int[] type, int unknown, int inBand, int fast, float velSum, float velMax)
        {
            int d = Math.Max(1, n);
            return Inv($"  {label,-16} | {n,3} | {(float)type[(int)PassType.Ground] / d,6:P0} | ")
                 + Inv($"{(float)type[(int)PassType.Driven] / d,6:P0} | {(float)type[(int)PassType.Lofted] / d,6:P0} | ")
                 + Inv($"{(float)type[(int)PassType.ThroughBall] / d,8:P0} | {(float)type[(int)PassType.AerialThrough] / d,7:P0} | ")
                 + Inv($"{(float)type[(int)PassType.Cross] / d,5:P0} | {(float)type[(int)PassType.Chip] / d,4:P0} | ")
                 + Inv($"{(float)unknown / d,7:P0} | {(float)inBand / d,6:P0} | {(float)fast / d,7:P0} | ")
                 + Inv($"{velSum / d,7:F2} | {velMax,6:F2}");
        }

        private static void AppendPassOutcomeTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4b. PASS OUTCOME (item 3): COMPLETED (the intended receiver gains");
            report.AppendLine("  possession), INTERCEPTED (an opponent does), TURNOVER (a different");
            report.AppendLine("  team-mate does), DEAD (a restart or goal intervenes first), UNRESOLVED");
            report.AppendLine("  (none of the above by full time). n = the sum of the five outcome counts,");
            report.AppendLine("  which should equal C4a's launched n for the same row — a mismatch would");
            report.AppendLine("  mean a launched pass fell through every branch in ResolvePendingPass.");
            report.AppendLine("  seed/team        |   n | completed% | intercepted% | turnover% |  dead% | unresolved%");

            var pooled = new MatchTally();
            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                AddPassTally(pooled, m);
                for (int t = 0; t < TeamCount; t++)
                {
                    report.AppendLine(FormatOutcomeRow(Inv($"0x{m.Seed:X16}/{TeamLabel(t)}"),
                        m.OutcomeCompletedByTeam[t], m.OutcomeInterceptedByTeam[t], m.OutcomeTurnoverByTeam[t],
                        m.OutcomeDeadByTeam[t], m.OutcomeUnresolvedByTeam[t]));
                }
            }
            for (int t = 0; t < TeamCount; t++)
            {
                report.AppendLine(FormatOutcomeRow(Inv($"pooled/{TeamLabel(t)}"),
                    pooled.OutcomeCompletedByTeam[t], pooled.OutcomeInterceptedByTeam[t], pooled.OutcomeTurnoverByTeam[t],
                    pooled.OutcomeDeadByTeam[t], pooled.OutcomeUnresolvedByTeam[t]));
            }

            int c = 0, ic = 0, tu = 0, dd = 0, ur = 0;
            for (int t = 0; t < TeamCount; t++)
            {
                c += pooled.OutcomeCompletedByTeam[t]; ic += pooled.OutcomeInterceptedByTeam[t];
                tu += pooled.OutcomeTurnoverByTeam[t]; dd += pooled.OutcomeDeadByTeam[t];
                ur += pooled.OutcomeUnresolvedByTeam[t];
            }
            report.AppendLine(FormatOutcomeRow("pooled/ALL", c, ic, tu, dd, ur));
            report.AppendLine();
        }

        private static string FormatOutcomeRow(
            string label, int completed, int intercepted, int turnover, int dead, int unresolved)
        {
            int n = completed + intercepted + turnover + dead + unresolved;
            int d = Math.Max(1, n);
            return Inv($"  {label,-16} | {n,3} | {(float)completed / d,10:P0} | {(float)intercepted / d,12:P0} | ")
                 + Inv($"{(float)turnover / d,9:P0} | {(float)dead / d,6:P0} | {(float)unresolved / d,11:P0}");
        }

        private static void AppendCompletionByTypeTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4c. COMPLETION RATE BY PASS TYPE — the headline number of this exercise.");
            report.AppendLine("  Pooled only (all 6 seeds): most per-seed per-type cells have single-digit");
            report.AppendLine("  denominators or none at all, which a per-seed breakdown here would present");
            report.AppendLine("  as a misleadingly precise percentage; see C4a/C4b for the per-seed raw");
            report.AppendLine("  launched/outcome counts this table pools.");
            report.AppendLine("  type          |  n(home) | comp%(home) |  n(away) | comp%(away) |  n(all) | comp%(all)");

            var pooled = new MatchTally();
            for (int i = 0; i < ms.Count; i++) AddPassTally(pooled, ms[i]);

            var types = (PassType[])Enum.GetValues(typeof(PassType));
            for (int i = 0; i < types.Length; i++)
            {
                PassType type = types[i];
                int nHome = pooled.PassTypeCountsByTeam[0][(int)type];
                int cHome = pooled.CompletedByTypeByTeam[0][(int)type];
                int nAway = pooled.PassTypeCountsByTeam[1][(int)type];
                int cAway = pooled.CompletedByTypeByTeam[1][(int)type];
                int nAll = nHome + nAway;
                int cAll = cHome + cAway;
                report.AppendLine(Inv($"  {type,-13} | {nHome,8} | {(float)cHome / Math.Max(1, nHome),11:P0} | ")
                                 + Inv($"{nAway,8} | {(float)cAway / Math.Max(1, nAway),11:P0} | ")
                                 + Inv($"{nAll,7} | {(float)cAll / Math.Max(1, nAll),10:P0}"));
            }
            report.AppendLine();
        }

        private static void AppendTimeToRestTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4d. TIME-TO-REST (item 4): elapsed seconds from launch to whichever comes");
            report.AppendLine("  first — possession established (by anyone), or ball speed below Ball");
            report.AppendLine("  Physics' own Rolling -> Stationary threshold");
            report.AppendLine("  (BallPhysicsConstants.State.MinVelocity) — with a fallback sample at");
            report.AppendLine("  resolution for the rare pass where neither ever fires before a restart.");
            report.AppendLine("  seed/team        |   n | mean(s) |  <0.5s |  0.5-1s |   1-2s |   2-4s |    >4s");

            var pooled = new MatchTally();
            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                AddPassTally(pooled, m);
                for (int t = 0; t < TeamCount; t++)
                {
                    report.AppendLine(FormatTimeToRestRow(Inv($"0x{m.Seed:X16}/{TeamLabel(t)}"),
                        m.TimeToRestSamplesByTeam[t], m.TimeToRestSecondsSumByTeam[t], m.TimeToRestHistByTeam[t]));
                }
            }
            for (int t = 0; t < TeamCount; t++)
            {
                report.AppendLine(FormatTimeToRestRow(Inv($"pooled/{TeamLabel(t)}"),
                    pooled.TimeToRestSamplesByTeam[t], pooled.TimeToRestSecondsSumByTeam[t], pooled.TimeToRestHistByTeam[t]));
            }

            int nAll = 0; float sumAll = 0f;
            var histAll = new int[TimeToRestBucketCount];
            for (int t = 0; t < TeamCount; t++)
            {
                nAll += pooled.TimeToRestSamplesByTeam[t];
                sumAll += pooled.TimeToRestSecondsSumByTeam[t];
                for (int b = 0; b < TimeToRestBucketCount; b++) histAll[b] += pooled.TimeToRestHistByTeam[t][b];
            }
            report.AppendLine(FormatTimeToRestRow("pooled/ALL", nAll, sumAll, histAll));
            report.AppendLine();
        }

        private static string FormatTimeToRestRow(string label, int n, float secondsSum, int[] hist)
        {
            int d = Math.Max(1, n);
            return Inv($"  {label,-16} | {n,3} | {secondsSum / d,7:F2} | ")
                 + Inv($"{(float)hist[0] / d,6:P0} | {(float)hist[1] / d,7:P0} | {(float)hist[2] / d,6:P0} | ")
                 + Inv($"{(float)hist[3] / d,6:P0} | {(float)hist[4] / d,6:P0}");
        }

        private static void AppendReceiverGapTable(StringBuilder report, List<MatchTally> ms)
        {
            report.AppendLine("C4e. RECEIVER-TO-BALL GAP AT RESOLUTION (item 5): metres between the");
            report.AppendLine("  intended receiver and the ball the moment the pass stopped being live, for");
            report.AppendLine("  every outcome including DEAD/UNRESOLVED — 'would a run have saved it' is");
            report.AppendLine("  exactly as meaningful there as when an opponent beat him to it.");
            report.AppendLine("  seed/team        |   n | mean(m) |   <1m |   1-2m |   2-5m |  5-10m |   >10m");

            var pooled = new MatchTally();
            for (int i = 0; i < ms.Count; i++)
            {
                MatchTally m = ms[i];
                AddPassTally(pooled, m);
                for (int t = 0; t < TeamCount; t++)
                {
                    report.AppendLine(FormatReceiverGapRow(Inv($"0x{m.Seed:X16}/{TeamLabel(t)}"),
                        m.ReceiverGapSamplesByTeam[t], m.ReceiverGapSumByTeam[t], m.ReceiverGapHistByTeam[t]));
                }
            }
            for (int t = 0; t < TeamCount; t++)
            {
                report.AppendLine(FormatReceiverGapRow(Inv($"pooled/{TeamLabel(t)}"),
                    pooled.ReceiverGapSamplesByTeam[t], pooled.ReceiverGapSumByTeam[t], pooled.ReceiverGapHistByTeam[t]));
            }

            int nAll = 0; float sumAll = 0f;
            var histAll = new int[ReceiverGapBucketCount];
            for (int t = 0; t < TeamCount; t++)
            {
                nAll += pooled.ReceiverGapSamplesByTeam[t];
                sumAll += pooled.ReceiverGapSumByTeam[t];
                for (int b = 0; b < ReceiverGapBucketCount; b++) histAll[b] += pooled.ReceiverGapHistByTeam[t][b];
            }
            report.AppendLine(FormatReceiverGapRow("pooled/ALL", nAll, sumAll, histAll));
            report.AppendLine();
        }

        private static string FormatReceiverGapRow(string label, int n, float metresSum, int[] hist)
        {
            int d = Math.Max(1, n);
            return Inv($"  {label,-16} | {n,3} | {metresSum / d,7:F2} | ")
                 + Inv($"{(float)hist[0] / d,5:P0} | {(float)hist[1] / d,6:P0} | {(float)hist[2] / d,6:P0} | ")
                 + Inv($"{(float)hist[3] / d,6:P0} | {(float)hist[4] / d,6:P0}");
        }

        private static string TeamLabel(int team) => team == 0 ? "home" : "away";

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

        /// <summary>Report C4 item 3's five outcomes, exactly as specified: the intended receiver
        /// gaining possession, an opponent gaining it, a different team-mate gaining it, a
        /// restart/goal intervening before anyone does, or none of the above by full time.</summary>
        private enum PassOutcomeKind
        {
            Completed,
            Intercepted,
            TurnoverTeammate,
            Dead,
            Unresolved,
        }

        /// <summary>
        /// Report C4's carried state for the ONE pass currently being tracked. Only one, because
        /// there is only one ball: under today's flow a new launch is only possible once the
        /// previous pending pass has already resolved (see <see cref="TryLaunchPass"/>'s doc) —
        /// <c>LaunchesSupersededPending</c> exists precisely to make a violation of that assumption
        /// visible instead of silently dropping data.
        /// </summary>
        private struct PendingPass
        {
            public bool Active;
            public int LaunchTick;
            public int KickerTeam;
            public int ReceiverId;          // authoritative — read off the latch itself, not derived
            public PassType Type;           // meaningless unless TypeKnown
            public bool TypeKnown;
            public bool TimeToRestCaptured;

            public static PendingPass None => default;
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

            // C4 (v1.2) — every array below is indexed [teamId], and the type/bucket ones
            // [teamId][typeOrBucket], so home/away are never pooled before the report chooses to.
            public readonly int[][] PassTypeCountsByTeam = MakeTeamByType();
            public readonly int[][] CompletedByTypeByTeam = MakeTeamByType();
            public readonly int[] LaunchesTotalByTeam = new int[TeamCount];
            public readonly int[] LaunchesInBandByTeam = new int[TeamCount];
            public readonly int[] LaunchesFastReceiverByTeam = new int[TeamCount];
            public readonly float[] ReceiverVelSumByTeam = new float[TeamCount];
            public readonly float[] ReceiverVelMaxByTeam = MakeFilledMinValue();
            public readonly int[] LaunchesWithUnknownTypeByTeam = new int[TeamCount];

            public readonly int[] OutcomeCompletedByTeam = new int[TeamCount];
            public readonly int[] OutcomeInterceptedByTeam = new int[TeamCount];
            public readonly int[] OutcomeTurnoverByTeam = new int[TeamCount];
            public readonly int[] OutcomeDeadByTeam = new int[TeamCount];
            public readonly int[] OutcomeUnresolvedByTeam = new int[TeamCount];

            public readonly int[] TimeToRestSamplesByTeam = new int[TeamCount];
            public readonly float[] TimeToRestSecondsSumByTeam = new float[TeamCount];
            public readonly int[][] TimeToRestHistByTeam = MakeTeamByBucket(TimeToRestBucketCount);

            public readonly int[] ReceiverGapSamplesByTeam = new int[TeamCount];
            public readonly float[] ReceiverGapSumByTeam = new float[TeamCount];
            public readonly int[][] ReceiverGapHistByTeam = MakeTeamByBucket(ReceiverGapBucketCount);

            // Instrument-health counters (expected to read 0 — see TryLaunchPass's doc). Neither
            // is attributable to a team: the failure mode in both cases is precisely that the
            // team cannot be determined or the previous pending's identity was about to be lost.
            public int LaunchesWithNoKickerDerived;
            public int LaunchesSupersededPending;

            // C2 item 7 (v1.2) — the counterfactual race, by ATTACKING team.
            public readonly float[] RaceAttackerDistSumByTeam = new float[TeamCount];
            public readonly float[] RaceDefenderDistSumByTeam = new float[TeamCount];
            public readonly int[] RaceSamplesByTeam = new int[TeamCount];
            public readonly int[] RaceAttackerNearerByTeam = new int[TeamCount];

            private static int[][] MakeTeamByType()
            {
                var a = new int[TeamCount][];
                for (int t = 0; t < TeamCount; t++) a[t] = new int[PassTypeCount];
                return a;
            }

            private static int[][] MakeTeamByBucket(int bucketCount)
            {
                var a = new int[TeamCount][];
                for (int t = 0; t < TeamCount; t++) a[t] = new int[bucketCount];
                return a;
            }

            private static float[] MakeFilledMinValue()
            {
                var a = new float[TeamCount];
                for (int t = 0; t < TeamCount; t++) a[t] = float.MinValue;
                return a;
            }
        }

        /// <summary>Pools every Report-C4 and C2-item-7 field of <paramref name="src"/> into
        /// <paramref name="dst"/>. The C1-C3 tables pool by hand-summing scalars at their call
        /// site (the established pattern in this file); C4 has enough per-team/per-type/per-bucket
        /// arrays that hand-summing would itself become the least-reviewed code in the file, so it
        /// is hoisted to one loop used for every "pooled" row this report prints.</summary>
        private static void AddPassTally(MatchTally dst, MatchTally src)
        {
            for (int t = 0; t < TeamCount; t++)
            {
                for (int k = 0; k < PassTypeCount; k++)
                {
                    dst.PassTypeCountsByTeam[t][k] += src.PassTypeCountsByTeam[t][k];
                    dst.CompletedByTypeByTeam[t][k] += src.CompletedByTypeByTeam[t][k];
                }

                dst.LaunchesTotalByTeam[t] += src.LaunchesTotalByTeam[t];
                dst.LaunchesInBandByTeam[t] += src.LaunchesInBandByTeam[t];
                dst.LaunchesFastReceiverByTeam[t] += src.LaunchesFastReceiverByTeam[t];
                dst.ReceiverVelSumByTeam[t] += src.ReceiverVelSumByTeam[t];
                if (src.ReceiverVelMaxByTeam[t] > dst.ReceiverVelMaxByTeam[t])
                {
                    dst.ReceiverVelMaxByTeam[t] = src.ReceiverVelMaxByTeam[t];
                }
                dst.LaunchesWithUnknownTypeByTeam[t] += src.LaunchesWithUnknownTypeByTeam[t];

                dst.OutcomeCompletedByTeam[t] += src.OutcomeCompletedByTeam[t];
                dst.OutcomeInterceptedByTeam[t] += src.OutcomeInterceptedByTeam[t];
                dst.OutcomeTurnoverByTeam[t] += src.OutcomeTurnoverByTeam[t];
                dst.OutcomeDeadByTeam[t] += src.OutcomeDeadByTeam[t];
                dst.OutcomeUnresolvedByTeam[t] += src.OutcomeUnresolvedByTeam[t];

                dst.TimeToRestSamplesByTeam[t] += src.TimeToRestSamplesByTeam[t];
                dst.TimeToRestSecondsSumByTeam[t] += src.TimeToRestSecondsSumByTeam[t];
                for (int b = 0; b < TimeToRestBucketCount; b++)
                {
                    dst.TimeToRestHistByTeam[t][b] += src.TimeToRestHistByTeam[t][b];
                }

                dst.ReceiverGapSamplesByTeam[t] += src.ReceiverGapSamplesByTeam[t];
                dst.ReceiverGapSumByTeam[t] += src.ReceiverGapSumByTeam[t];
                for (int b = 0; b < ReceiverGapBucketCount; b++)
                {
                    dst.ReceiverGapHistByTeam[t][b] += src.ReceiverGapHistByTeam[t][b];
                }

                dst.RaceAttackerDistSumByTeam[t] += src.RaceAttackerDistSumByTeam[t];
                dst.RaceDefenderDistSumByTeam[t] += src.RaceDefenderDistSumByTeam[t];
                dst.RaceSamplesByTeam[t] += src.RaceSamplesByTeam[t];
                dst.RaceAttackerNearerByTeam[t] += src.RaceAttackerNearerByTeam[t];
            }

            dst.LaunchesWithNoKickerDerived += src.LaunchesWithNoKickerDerived;
            dst.LaunchesSupersededPending += src.LaunchesSupersededPending;
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
// | 1.2     | 2026-08-08 | —      | Report C4: follows each launched pass (the pass-in-flight latch's   |
// |         |            |        | -1 -> R transition, kicker derived from the previous tick's         |
// |         |            |        | possessor — see TryLaunchPass) restricted to the passing team's own |
// |         |            |        | attacking third. C4a pass-type distribution + the ThroughBall       |
// |         |            |        | near-miss gate (#8 §3.1.3.4's 15-30 m band and 1.0 m/s              |
// |         |            |        | velocity-toward-goal predicates, measured separately); C4b outcome  |
// |         |            |        | (completed/intercepted/turnover/dead/unresolved); C4c completion    |
// |         |            |        | rate by pass type (the headline); C4d time-to-rest against Ball     |
// |         |            |        | Physics' own MinVelocity; C4e receiver-to-ball gap at resolution.   |
// |         |            |        | PassOutcome.Invalid recorded as NOT OBSERVABLE (no accessor exposes |
// |         |            |        | it) rather than skipped silently. C2 gained item 7, the             |
// |         |            |        | counterfactual race: nearest attacker/defender to a point           |
// |         |            |        | CounterfactualRaceProjectionM (arbitrary-by-instrument) goalward of |
// |         |            |        | the ball. Every new table is split by team AND by seed, pooled both |
// |         |            |        | ways (#8 ERR-008-002 precedent). FIX: the C1 episode-exit           |
// |         |            |        | turnover/retreat/cleared attribution read the on-ball HOLDER        |
// |         |            |        | (TestOnly_PossessingAgentId), which is -1 for the entire flight of  |
// |         |            |        | every pass — C2's own ownerless% already measured that gap at ~86%  |
// |         |            |        | of samples — so it read cleared% ~= 99% on every seed and measured  |
// |         |            |        | the ownerless gap, not football. Now composed as TEAM possession    |
// |         |            |        | exactly as MatchEngine.FillPositioningSnapshot does post            |
// |         |            |        | ERR-012-011 (holder, else the pass-in-flight receiver, else         |
// |         |            |        | nobody). docs/tracking/close-chance-creation-design.md's existing   |
// |         |            |        | numbers for that column predate this correction. Assertion-free    |
// |         |            |        | throughout (ERR-030-014); no production files touched.              |
#endregion
