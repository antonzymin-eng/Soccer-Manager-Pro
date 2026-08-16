// File:     src/match-analytics/MatchAnalyticsAggregator.cs
// Created:  2026-07-27
// Modified: 2026-08-16, later (L-6, adversarial review — v1.1: the card routing's final `else` still
//           absorbed any FUTURE fourth CardKind as a plain yellow, even after M4/ERR-037-003 made the
//           SecondYellow branch explicit — the spec's card table (section-3.md) is exhaustive over
//           #17's three-value domain and states no fourth-value posture. Widened to an explicit
//           CardKindYellow branch (Yellow is no longer the implicit catch-all) plus a fail-loud
//           ArgumentOutOfRangeException naming the kind for anything else, mirroring the #44
//           CardLedgerFold.RequireKnownCardKind (F4) precedent over the identical domain. Behaviour
//           change: a CardKind outside {0, 1, 2} now throws instead of silently counting as a yellow —
//           unreachable today (#17's own producer emits only 0/1/2), so no live match is affected.)
// Modified: 2026-08-16 (reviewed-findings pass, M4/ERR-037-003 — CardIssuedEvent routing widened from
//           a two-way `CardKind == CardKindRed ? red : yellow` test to a three-way switch over #17's
//           full CardKind domain: a kind-2 (second-yellow dismissal) event now increments BOTH
//           YellowCards and RedCards, instead of silently falling into the yellow-only else branch and
//           never touching RedCards at all. Behaviour change: any match with a second-yellow dismissal
//           now reports a nonzero RedCards for that team, matching MatchStatline.RedCards's documented
//           contract. New MatchAnalyticsConstants.CardKindSecondYellow (and CardKindYellow) consumed.)
// Author:   —
// Spec:     Match Analytics & Statistics #37 §3.1–§3.5 (KD-3), FR-AN-003..012 / 016..018,
//           Code Standards #20
// Purpose:  The KD-3 aggregation core: pumped once per engine tick over the read-only ledger tap and
//           positional sample, it accumulates every statistic #37 can derive today and projects them
//           into the #38 view models on demand.

using System;
using System.Collections.Generic;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// Accumulates one match's statistics from the two read-only taps (§3.5).
    ///
    /// <para><b>Two cadences, and the difference matters (§3.5).</b> Event consumption is
    /// <b>lossless</b> — a skipped tick loses that tick's goal / foul / card / restart outright, so
    /// <see cref="ObserveTick"/> refuses a non-consecutive tick (F6) rather than silently
    /// under-counting. The positional accrual is a frequency estimate, so it MAY stride
    /// (<see cref="MatchAnalyticsConstants.TERRITORIAL_SAMPLE_STRIDE"/>); the stride lives inside
    /// <see cref="ObserveTick"/>, which is still called every tick, so it never trips F6.</para>
    ///
    /// <para><b>Read-only (FR-AN-001 / FR-AN-017).</b> Nothing here calls a mutating engine method, so
    /// the match digest is identical whether or not analytics run — the <c>match-viewer</c>
    /// observer-neutrality precedent.</para>
    ///
    /// <para><b>Forward-compatible (F5).</b> A Tier A ordinal this aggregator does not recognize is
    /// ignored, so landing a new producer (§7.2's deferred `ShotAttemptedEvent` and friends) never
    /// breaks an un-extended #37 — it just does not count yet.</para>
    ///
    /// <para>Single-threaded: the sim thread that ticks the engine is the thread that pumps this.</para>
    /// </summary>
    public sealed class MatchAnalyticsAggregator
    {
        /// <summary>Possession bucket for "no settled holder" (F3) — dead-ball and contested time.</summary>
        private const int LooseBucket = MatchAnalyticsConstants.TEAM_COUNT;

        private const int PossessionBuckets = MatchAnalyticsConstants.TEAM_COUNT + 1;

        // ── §3.2 per-team tallies ────────────────────────────────────────────────────────────────
        private readonly int[] _goals         = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _fouls         = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _yellowCards   = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _redCards      = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _offsides      = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _corners       = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _throwIns      = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _goalKicks     = new int[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[] _substitutions = new int[MatchAnalyticsConstants.TEAM_COUNT];

        // ── §3.1 possession accrual ──────────────────────────────────────────────────────────────
        private readonly long[] _possessionTicks = new long[PossessionBuckets];
        private long _observedTicks;
        private int  _currentHolderBucket = LooseBucket;

        // ── §3.4 positional accrual ──────────────────────────────────────────────────────────────
        private readonly long[] _territorialSamples = new long[MatchAnalyticsConstants.TEAM_COUNT];
        private readonly int[][] _heatmapBins;
        private long _positionalSamples;

        // ── §3.2 location maps ───────────────────────────────────────────────────────────────────
        private readonly List<StatPoint> _goalMap    = new List<StatPoint>();
        private readonly List<StatPoint> _foulMap    = new List<StatPoint>();
        private readonly List<StatPoint> _offsideMap = new List<StatPoint>();

        // ── F6 lossless-pump enforcement ─────────────────────────────────────────────────────────
        private bool  _hasObserved;
        private ulong _lastObservedTick;

        // Set for the duration of a tick's application and cleared only once the whole tick landed,
        // so an exception part-way through leaves it set permanently (see ObserveTick).
        private bool _faulted;

        /// <summary>Creates an empty aggregator. Reusable only via a fresh instance — a match's
        /// statistics are the whole state, so there is deliberately no Reset().</summary>
        public MatchAnalyticsAggregator()
        {
            _heatmapBins = new int[MatchAnalyticsConstants.TEAM_COUNT][];
            for (int t = 0; t < MatchAnalyticsConstants.TEAM_COUNT; t++)
            {
                _heatmapBins[t] = AdvancedStatline.NewHeatmapGrid();
            }
        }

        /// <summary>Ticks observed so far — the denominator of every possession share.</summary>
        public long ObservedTicks => _observedTicks;

        /// <summary>
        /// Consumes one engine tick: routes that tick's ledger records, accrues a possession tick, and
        /// — on stride ticks — accrues the positional sample (§3.5).
        /// </summary>
        /// <param name="tap">This tick's Tier A/B records, in canonical order.</param>
        /// <param name="sample">This tick's positional world state.</param>
        /// <param name="currentTick">
        /// The engine tick being observed. Must be exactly one more than the previous call's.
        /// </param>
        /// <exception cref="ArgumentNullException">Either tap is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// F6 — <paramref name="currentTick"/> is not consecutive with the last observed tick. A gap
        /// means the caller dropped a tick's records; counting on regardless would produce a statline
        /// that looks plausible and is wrong, which is precisely what fail-loud exists to prevent.
        /// Also thrown when an earlier call failed part-way through a tick: the counts are incomplete
        /// from that point on, so this aggregator refuses every subsequent tick.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">F1 — a record names an out-of-range agent.</exception>
        /// <exception cref="ArgumentException">F2 — a sampled or recorded position is non-finite.</exception>
        public void ObserveTick(ITickLedgerTap tap, IWorldStateSample sample, ulong currentTick)
        {
            if (tap == null) throw new ArgumentNullException(nameof(tap));
            if (sample == null) throw new ArgumentNullException(nameof(sample));

            RequireNotFaulted();
            RequireConsecutive(currentTick);

            // A tick is applied across several accumulators, so an F1/F2 throw part-way through
            // leaves it HALF applied — some of its records counted, the rest lost, its possession
            // tick un-accrued. F6 alone does not cover that: the consecutive-tick guard has already
            // advanced, so the next tick would be accepted and the run would carry on producing a
            // statline that looks plausible and is wrong — exactly what F6 exists to prevent. Once
            // the lossless contract is broken the counts are not trustworthy, so the aggregator
            // latches and refuses rather than continuing from a partial tick.
            _faulted = true;

            int recordCount = tap.RecordCount;
            for (int i = 0; i < recordCount; i++)
            {
                RouteRecord(tap, sample, i);
            }

            // §3.1 — every observed tick is attributed to whoever holds the ball AFTER this tick's
            // transitions, i.e. the holder in effect for the span this tick opens.
            _possessionTicks[_currentHolderBucket]++;
            _observedTicks++;

            if (currentTick % (ulong)MatchAnalyticsConstants.TERRITORIAL_SAMPLE_STRIDE == 0UL)
            {
                AccruePositional(sample);
            }

            // Reached only when the WHOLE tick applied; any escape above leaves the flag set. No
            // try/finally: a finally would clear it on the exception path too, which is the one
            // path it exists for.
            _faulted = false;
        }

        /// <summary>
        /// Projects the accumulators into the #38 view models. Idempotent and side-effect-free — the
        /// accumulators are not consumed, so a live HUD may call this every render frame (§3.5).
        /// </summary>
        public MatchAnalyticsResult Build()
        {
            return new MatchAnalyticsResult(
                BuildStatline(0),
                BuildStatline(1),
                BuildAdvanced(0),
                BuildAdvanced(1),
                _goalMap.ToArray(),
                _foulMap.ToArray(),
                _offsideMap.ToArray());
        }

        // ── §3.5 F6 ──────────────────────────────────────────────────────────────────────────────

        private void RequireNotFaulted()
        {
            if (_faulted)
            {
                throw new InvalidOperationException(
                    "MatchAnalyticsAggregator.ObserveTick: a previous tick failed part-way through, " +
                    "so this match's counts are incomplete and every further tick would compound a " +
                    "statline that is silently wrong (#37 §3.5). Build() still returns what was " +
                    "accumulated up to the failure; a new match needs a new aggregator.");
            }
        }

        private void RequireConsecutive(ulong currentTick)
        {
            if (_hasObserved && currentTick != _lastObservedTick + 1UL)
            {
                throw new InvalidOperationException(
                    "MatchAnalyticsAggregator.ObserveTick: tick " + currentTick +
                    " is not consecutive with the last observed tick " + _lastObservedTick +
                    ". Event consumption is lossless (#37 §3.5) — pump once per engine tick.");
            }

            _hasObserved      = true;
            _lastObservedTick = currentTick;
        }

        // ── §3.2 routing ─────────────────────────────────────────────────────────────────────────

        private void RouteRecord(ITickLedgerTap tap, IWorldStateSample sample, int index)
        {
            byte ordinal = tap.RecordOrdinal(index);

            // Ordinals come from the live #17 registry rather than a local table of literals, so this
            // switch cannot drift out of step with Appendix A.
            if (ordinal == EventRegistry.GetOrdinal<PossessionChangedEvent>())
            {
                // §3.1 known handler — drives holder state, not a tally.
                PossessionChangedEvent evt = tap.ReadRecord<PossessionChangedEvent>(index);
                _currentHolderBucket = ResolveHolderBucket(sample, evt.NewHolder);
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<GoalAwardedEvent>())
            {
                GoalAwardedEvent evt = tap.ReadRecord<GoalAwardedEvent>(index);
                int team = RequireTeam(evt.ScoringTeam, "GoalAwardedEvent.ScoringTeam");
                _goals[team]++;
                _goalMap.Add(MakePoint(team, evt.BallPosition, "GoalAwardedEvent.BallPosition"));
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<FoulCommittedEvent>())
            {
                FoulCommittedEvent evt = tap.ReadRecord<FoulCommittedEvent>(index);
                int team = TeamOfAgent(sample, evt.Offender, "FoulCommittedEvent.Offender");
                _fouls[team]++;
                _foulMap.Add(MakePoint(team, evt.Location, "FoulCommittedEvent.Location"));
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<CardIssuedEvent>())
            {
                CardIssuedEvent evt = tap.ReadRecord<CardIssuedEvent>(index);
                int team = TeamOfAgent(sample, evt.Recipient, "CardIssuedEvent.Recipient");

                // ERR-037-003 (reviewed-findings pass M4): CardKind is #17's three-value domain ordinal
                // (0=Yellow, 1=Red, 2=SecondYellow — EventSystemConstants.CARD_KIND_*), not a boolean.
                // The old `== red ? red : yellow` test had no branch for kind 2 at all, so a
                // second-yellow dismissal fell into the else and counted as a plain yellow — RedCards
                // stayed 0 for every match with one, contradicting MatchStatline.RedCards's own
                // documented contract ("Red cards received (including second-yellow dismissals)").
                //
                // A kind-2 event still counts toward BOTH tallies, not just red: the producer
                // (MatchEngine.ApplyCardAndCheckSentOff) emits it as ONE event standing in for the
                // second caution AND the resulting dismissal, never a yellow-then-red pair, so nothing
                // else will ever increment a yellow for that caution. Splitting it into a yellow and a
                // red here is what makes the box score match the two bookings a real match report would
                // show (the player's first caution earlier in the match already published its own
                // separate kind-0 event and is counted there). This mirrors the discipline #44 precedent
                // — DisciplineRules.ApplyCard's kind-2 branch is documented as "one yellow AND one
                // dismissal ban" for the identical reason.
                //
                // L-6 (reviewed findings pass): the routing is now the SAME three-way exhaustive mapping
                // #37 section-3.md's CardIssuedEvent row states — Yellow, Red, SecondYellow, each its
                // own explicit branch — rather than two explicit checks with Yellow left as the catch-all
                // else. That else silently absorbed any FUTURE fourth CardKind value as a plain yellow,
                // which is wrong in a different way than the ERR-037-003 defect this same block already
                // fixed once. The spec's table is exhaustive over #17's three-value domain and states no
                // fourth-value posture; F5 is a different failure mode (an unrecognized ORDINAL, handled
                // by ignoring the whole record, FR-AN-019) and does not license silently ignoring a
                // malformed VALUE within a record #37 DOES recognize. Fails loud instead, naming the
                // value — the #44 CardLedgerFold.RequireKnownCardKind (F4) precedent for the identical
                // domain.
                if (evt.CardKind == MatchAnalyticsConstants.CardKindYellow)
                {
                    _yellowCards[team]++;
                }
                else if (evt.CardKind == MatchAnalyticsConstants.CardKindRed)
                {
                    _redCards[team]++;
                }
                else if (evt.CardKind == MatchAnalyticsConstants.CardKindSecondYellow)
                {
                    _yellowCards[team]++;
                    _redCards[team]++;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(evt.CardKind), evt.CardKind,
                        "MatchAnalyticsAggregator: CardIssuedEvent.CardKind must be 0 (yellow), 1 (red) "
                        + "or 2 (second yellow) — #37 section-3.md's card table is exhaustive over #17's "
                        + "three-value domain and states no fourth-value posture; absorbing an unknown "
                        + "kind as a plain yellow would silently corrupt the box score.");
                }
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<OffsideCalledEvent>())
            {
                OffsideCalledEvent evt = tap.ReadRecord<OffsideCalledEvent>(index);
                int team = RequireTeam(evt.Team, "OffsideCalledEvent.Team");
                _offsides[team]++;
                _offsideMap.Add(MakePoint(team, evt.Location, "OffsideCalledEvent.Location"));
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<RestartAwardedEvent>())
            {
                RestartAwardedEvent evt = tap.ReadRecord<RestartAwardedEvent>(index);
                int team = RequireTeam(evt.AwardedTeam, "RestartAwardedEvent.AwardedTeam");
                switch (evt.RestartKind)
                {
                    case MatchAnalyticsConstants.RESTART_KIND_THROW_IN:  _throwIns[team]++;  break;
                    case MatchAnalyticsConstants.RESTART_KIND_GOAL_KICK: _goalKicks[team]++; break;
                    case MatchAnalyticsConstants.RESTART_KIND_CORNER:    _corners[team]++;   break;
                    // Kick-off and None carry no statline column; ignored by design, not by omission.
                }
                return;
            }

            if (ordinal == EventRegistry.GetOrdinal<SubstitutionEvent>())
            {
                SubstitutionEvent evt = tap.ReadRecord<SubstitutionEvent>(index);
                int team = RequireTeam(evt.Team, "SubstitutionEvent.Team");
                _substitutions[team]++;
                return;
            }

            // MatchPhaseChangedEvent and every future producer land here: recognized-as-unrecognized.
            // F5 — ignoring the record is what lets a new Tier A event ship without a lockstep #37
            // change (§7.2). It is NOT a silent failure: nothing #37 reports depends on it yet.
        }

        // ── §3.1 possession ──────────────────────────────────────────────────────────────────────

        private static int ResolveHolderBucket(IWorldStateSample sample, int newHolder)
        {
            // F3 — no settled holder. Attributed to neither team (and NOT dropped: the loose bucket is
            // what makes the three shares sum to 100 instead of silently inflating the two teams').
            if (newHolder < 0)
            {
                return LooseBucket;
            }

            return TeamOfAgent(sample, newHolder, "PossessionChangedEvent.NewHolder");
        }

        // ── §3.4 positional ──────────────────────────────────────────────────────────────────────

        private void AccruePositional(IWorldStateSample sample)
        {
            Vector2 ball = sample.BallPosition;
            RequireFinite(ball, "IWorldStateSample.BallPosition");

            // Territorial dominance = play in the OPPONENT's half. Team 0 attacks +x, so it is
            // credited past the halfway line; everything else falls to team 1.
            //
            // NOTE (#37 §3.4): the spec's prose says "team 0 when x > L/2, team 1 when x < L/2" while
            // the sentence after it requires the split to be TOTAL ("no double-count, no gap"). Those
            // two cannot both hold at exactly x == L/2. Totality is the load-bearing property — it is
            // what makes territorial%[0] + territorial%[1] == 100 — so the strict `>` decides and the
            // halfway line itself falls to team 1. See ERR-037-002.
            if (ball.x > MatchAnalyticsConstants.PITCH_LENGTH_M * 0.5f)
            {
                _territorialSamples[0]++;
            }
            else
            {
                _territorialSamples[1]++;
            }

            int agentCount = sample.AgentCount;
            for (int i = 0; i < agentCount; i++)
            {
                // A dismissed agent stays on the pitch as a collision body (§5.Z Phase H) and stops
                // moving, so binning them would pile every remaining tick of the match into the one
                // cell they froze in. They are no longer playing; the heatmap should not say they are.
                if (!sample.AgentIsActive(i)) { continue; }

                Vector2 pos = sample.AgentPosition(i);
                RequireFinite(pos, "IWorldStateSample.AgentPosition");

                int team = RequireTeam(sample.AgentTeamId(i), "IWorldStateSample.AgentTeamId");
                int bin  = HeatmapBin(pos);
                if (bin >= 0)
                {
                    _heatmapBins[team][bin]++;
                }
            }

            _positionalSamples++;
        }

        /// <summary>
        /// Fixed-grid cell containing <paramref name="position"/>, or −1 when it lies off the pitch.
        ///
        /// <para>Off-pitch is dropped rather than clamped: an agent momentarily over the touchline is
        /// not occupying the touchline cell, and clamping would pile the whole margin onto the edge
        /// bins and make every heatmap look like it has hot rails. It is not an error either — the
        /// engine's own on-pitch bound is a soft one.</para>
        /// </summary>
        private static int HeatmapBin(Vector2 position)
        {
            int col = (int)(position.x / MatchAnalyticsConstants.PITCH_LENGTH_M
                            * MatchAnalyticsConstants.HEATMAP_COLS);
            int row = (int)(position.y / MatchAnalyticsConstants.PITCH_WIDTH_M
                            * MatchAnalyticsConstants.HEATMAP_ROWS);

            if (position.x < 0f || position.y < 0f) return -1;
            if (col < 0 || col >= MatchAnalyticsConstants.HEATMAP_COLS) return -1;
            if (row < 0 || row >= MatchAnalyticsConstants.HEATMAP_ROWS) return -1;

            return row * MatchAnalyticsConstants.HEATMAP_COLS + col;
        }

        // ── Projection ───────────────────────────────────────────────────────────────────────────

        private MatchStatline BuildStatline(int team)
        {
            return new MatchStatline(
                teamId:                 (byte)team,
                goals:                  _goals[team],
                possessionSharePercent: Percent(_possessionTicks[team], _observedTicks),
                fouls:                  _fouls[team],
                yellowCards:            _yellowCards[team],
                redCards:               _redCards[team],
                offsides:               _offsides[team],
                corners:                _corners[team],
                throwIns:               _throwIns[team],
                goalKicks:              _goalKicks[team],
                substitutions:          _substitutions[team]);
        }

        private AdvancedStatline BuildAdvanced(int team)
        {
            return new AdvancedStatline(
                teamId:             (byte)team,
                territorialPercent: Percent(_territorialSamples[team], _positionalSamples),
                // F4 / KD-2 — no shot-origin producer exists, so there is no live xG and the flag says
                // so rather than a zero pretending to be a measurement.
                liveXgAvailable:    false,
                xgSum:              0f,
                heatmapBins:        _heatmapBins[team]);
        }

        /// <summary>
        /// <paramref name="part"/> as a percentage of <paramref name="whole"/>, clamped into the
        /// [0, 100] the view models require. A zero denominator (nothing observed yet) yields 0 —
        /// a HUD asking before kickoff gets an honest zero, not a NaN the statline would refuse.
        /// </summary>
        private static float Percent(long part, long whole)
        {
            if (whole <= 0L) return 0f;

            // FR-CS-071 carve-out: `double` for the ratio only, never stored. A full match is
            // ~324 000 ticks, past float's 24-bit integer-exact range, so `100f * part` would start
            // rounding the numerator before the division and drift the share by a tenth of a point.
            // The result is narrowed back to float immediately and reaches no sim path.
            return Mathf.Clamp((float)(100.0d * part / whole), 0f, 100f);
        }

        // ── Gates (F1 / F2) ──────────────────────────────────────────────────────────────────────

        private static int TeamOfAgent(IWorldStateSample sample, int agentId, string what)
        {
            if (agentId < 0 || agentId >= sample.AgentCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(agentId), agentId,
                    what + " is outside [0, " + sample.AgentCount + ") — F1.");
            }

            return RequireTeam(sample.AgentTeamId(agentId), what + " → AgentTeamId");
        }

        private static int RequireTeam(int team, string what)
        {
            if (team < 0 || team >= MatchAnalyticsConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(team), team, what + " is not a known team (0 or 1) — F1.");
            }

            return team;
        }

        private static StatPoint MakePoint(int team, Vector3 location, string what)
        {
            RequireFinite(new Vector2(location.x, location.y), what);
            return new StatPoint((byte)team, location.x, location.y);
        }

        private static void RequireFinite(Vector2 v, string what)
        {
            // Negated compare so NaN fails closed (the project's NaN-gate convention).
            if (!(float.IsFinite(v.x) && float.IsFinite(v.y)))
            {
                throw new ArgumentException(what + " is not finite (" + v + ") — F2.", what);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): the KD-3 aggregation core — §3.1    |
// |         |            |        | tick-weighted possession, the §3.2 routing table over the      |
// |         |            |        | KD-7 tap, §3.4 territorial + heatmap binning, F1/F2/F3/F5/F6.  |
// | 1.1     | 2026-07-27 | —      | AR-1 M-1: a tick that throws part-way through now LATCHES and  |
// |         |            |        | refuses every further tick. F6 checks continuity, which a      |
// |         |            |        | half-applied tick satisfies — the guard had already advanced,  |
// |         |            |        | so the run carried on with some of that tick's records counted |
// |         |            |        | and the rest lost. AR-1 M-2: dismissed agents are skipped in   |
// |         |            |        | the §3.4 binning loop (they are frozen ON the pitch per §5.Z   |
// |         |            |        | Phase H, so one red card dominated the heatmap thereafter).    |
// |         |            |        | AR-1 L: FR-CS-071 justification recorded for Percent's double. |
// | 1.2     | 2026-08-15 | —      | Reviewed-findings pass (L2). CARD_KIND_RED reference updated   |
// |         |            |        | to MatchAnalyticsConstants.CardKindRed (PascalCase rename of   |
// |         |            |        | the [CROSS] mirror). No behaviour change.                      |
// | 1.3     | 2026-08-16 | —      | Reviewed-findings pass (M4/ERR-037-003). CardIssuedEvent        |
// |         |            |        | routing widened to a three-way switch over #17's full CardKind |
// |         |            |        | domain (0/1/2): a kind-2 second-yellow dismissal now increments|
// |         |            |        | BOTH YellowCards and RedCards, instead of falling into the     |
// |         |            |        | yellow-only else branch and leaving RedCards at 0 for every    |
// |         |            |        | such match — MatchStatline.RedCards's own documented contract  |
// |         |            |        | ("including second-yellow dismissals") was being contradicted. |
// |         |            |        | Behaviour change (RedCards is now nonzero where it wasn't).    |
// | 1.4     | 2026-08-16, later | — | L-6 (adversarial review). The v1.3 routing's final `else`   |
// |         |            |        | still absorbed any FUTURE fourth CardKind as a plain yellow —  |
// |         |            |        | the spec's card table (section-3.md) is exhaustive over #17's  |
// |         |            |        | three-value domain and states no fourth-value posture, and F5  |
// |         |            |        | (an unrecognized ordinal, ignored) does not license ignoring a |
// |         |            |        | malformed value inside a record #37 DOES recognize. Widened to |
// |         |            |        | an explicit CardKindYellow branch plus a fail-loud             |
// |         |            |        | ArgumentOutOfRangeException naming the kind for anything else, |
// |         |            |        | mirroring #44 CardLedgerFold.RequireKnownCardKind (F4) over    |
// |         |            |        | the identical domain. Test: Card_WithAnUnknownKind_ThrowsAnd-  |
// |         |            |        | NamesTheKind. Unreachable today (#17's own producer emits only |
// |         |            |        | 0/1/2), so no live match is affected.                           |
#endregion
