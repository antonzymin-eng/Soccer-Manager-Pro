// W8 Stage A: read-only, nonserialized, env-gated baseline. Definitions: docs/tracking/w8-stage-a-preregistration.md.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using TacticalDirector.DecisionTree;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.TacticalInstructions;
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class W8StageADiagnosticTests
    {
        private const int TicksPerSeed = 324000;
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL, 0x00000000D1A6D05EUL,
            0x0000000000000001UL, 0x00000000ABCDEF12UL,
            0x0000000099887766UL, 0x000000005A5A5A5AUL
        };

        [Test, Category("Calibration")]
        public void W8StageADiagnostic_ReportsFrozenSixSeedBaseline()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_W8_STAGE_A")))
                Assert.Ignore("Set TD_W8_STAGE_A=1 to run the preregistered W8 Stage A census.");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                var report = new StringBuilder();
                report.AppendLine("=== W8 Stage A frozen six-seed baseline ===");
                report.AppendLine("seed,keeper,team,kind,startFrame,endFrame,durationFrames,endReason,lastTouchAgent,lastTouchKind,lastTouchTeam,keeperArea,keeperX,keeperY,contactArea,contactX,contactY,lastDecision,claimTick,lastTransitionFrame,claimTacticalTick,dryTarget,dryDistanceM,dryFallback,dryPolicy");
                foreach (ulong seed in Seeds)
                {
                    var engine = new MatchEngine(seed);
                    engine.EnableGkHeading();
                    var census = new Census(engine, seed, report);
                    engine.TestOnly_W8StageAObserver = census.Observe;
                    for (int frame = 0; frame < TicksPerSeed; frame++)
                    {
                        engine.RunTick();
                        census.AfterTick();
                    }
                    engine.TestOnly_W8StageAObserver = null;
                    census.Finish();
                    report.AppendLine(Inv($"seedEnd,0x{seed:X16},tick={engine.CurrentTick},digest={Digest(engine.CurrentSnapshotDigest)}"));
                }
                TestContext.WriteLine(report.ToString());
            }
            finally { UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
        }

        [Test]
        public void W8Observer_DoesNotEnterSnapshotOrChangeSixHundredTickDigest()
        {
            // No baseline figures are printed or inspected by this structural gate.
            const ulong seed = 0x0F1E2D3C4B5A6978UL;
            var observed = new MatchEngine(seed);
            observed.EnableGkHeading();
            observed.TestOnly_W8StageAObserver = _ => { };
            var digests = new byte[600][];
            for (int frame = 0; frame < 600; frame++)
            {
                observed.RunTick();
                digests[frame] = observed.CurrentSnapshotDigest;
            }
            observed.TestOnly_W8StageAObserver = null;

            // EventBus is process-global: construct and run the second engine only after the first
            // chain has finished, as the existing deterministic replay probes do.
            var plain = new MatchEngine(seed);
            plain.EnableGkHeading();
            for (int frame = 0; frame < 600; frame++)
            {
                plain.RunTick();
                CollectionAssert.AreEqual(plain.CurrentSnapshotDigest, digests[frame],
                    $"observer altered snapshot at frame {frame + 1}");
            }
        }

        private static string Inv(FormattableString value) => FormattableString.Invariant(value);
        private static string Digest(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        private sealed class Census
        {
            private readonly MatchEngine _engine;
            private readonly ulong _seed;
            private readonly StringBuilder _output;
            private readonly Dictionary<int, Episode> _open = new Dictionary<int, Episode>();
            private readonly Dictionary<int, int> _recentDrop = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _keepers = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _eligibleFrames = new Dictionary<int, int>();
            private readonly Dictionary<int, GoalkeeperState> _tacticalBefore = new Dictionary<int, GoalkeeperState>();
            private readonly Dictionary<int, ActionType> _decision = new Dictionary<int, ActionType>();
            private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
            private int _lastTouch = -1;
            private string _lastTouchKind = "unobserved";
            private int _lastTouchTeam = -1;
            private int _pendingDrop = -1;
            private int _pendingKick = -1;
            private string _pendingKickKind;
            private int _lastFrame;
            private int _reclaimsHand, _reclaimsFeet, _drops;
            private int _totalClaims;
            private int _unknownTouchClaims;
            private int _lastTouchFrame = -1;
            private int _pendingPassTarget = -2;
            private string _pendingPassKind;

            internal Census(MatchEngine engine, ulong seed, StringBuilder output)
            {
                _engine = engine;
                _seed = seed;
                _output = output;
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                    if (engine.TestOnly_IsGoalkeeper(i))
                        _keepers[i] = engine.TestOnly_W8TeamId(i);
            }

            private void Count(string key)
            {
                _counts.TryGetValue(key, out int old);
                _counts[key] = old + 1;
            }

            private void Touch(int agent, string kind)
            {
                _lastTouch = agent;
                _lastTouchKind = kind;
                _lastTouchTeam = agent >= 0 ? _engine.TestOnly_W8TeamId(agent) : -1;
                _lastTouchFrame = (int)_engine.CurrentTick;
            }

            internal void Observe(W8StageAEvent e)
            {
                _lastFrame = e.Frame;
                switch (e.Kind)
                {
                    case W8StageAKind.HandClaim:
                        ResolvePass("interrupted-by-claim");
                        Open(e.Agent, "hand", e);
                        Touch(e.Agent, "hand-claim");
                        break;
                    case W8StageAKind.Acquire:
                        if (_pendingPassTarget != -2)
                            ResolvePass(e.Agent == _pendingPassTarget ? "target-controlled" : "other-controlled");
                        if (_keepers.ContainsKey(e.Agent) && !_open.ContainsKey(e.Agent))
                            Open(e.Agent, "feet", e);
                        if (e.Other >= 0 && e.Other != e.Agent)
                            Close(e.Other, e.Frame, "tackle-or-acquisition");
                        if (!_keepers.ContainsKey(e.Agent) && !(_lastTouch == e.Agent && _lastTouchFrame == e.Frame))
                            Touch(e.Agent, "controlled-acquisition");
                        break;
                    case W8StageAKind.Release:
                        if (e.Agent >= 0)
                            Close(e.Agent, e.Frame, e.Agent == _pendingDrop ? "engine-360-drop"
                                : e.Agent == _pendingKick ? _pendingKickKind : "other-release");
                        _pendingDrop = -1;
                        _pendingKick = -1;
                        break;
                    case W8StageAKind.KickRelease:
                        if (e.Agent == e.HolderBefore)
                            Close(e.Agent, e.Frame, _pendingKickKind ?? "kick");
                        _pendingKick = -1;
                        break;
                    case W8StageAKind.SixSecondDrop:
                        _pendingDrop = e.Agent;
                        _recentDrop[e.Agent] = e.Frame;
                        _drops++;
                        break;
                    case W8StageAKind.Restart:
                        ResolvePass("restart");
                        Count("restart-" + e.Cue);
                        if (e.Other >= 0) Close(e.Other, e.Frame, "restart-" + e.Cue);
                        Touch(-1, "restart-placement");
                        break;
                    case W8StageAKind.PassKick:
                    case W8StageAKind.ShotKick:
                    case W8StageAKind.GkHeadingKick:
                        if (e.Kind == W8StageAKind.PassKick && _open.TryGetValue(e.Agent, out Episode passerEpisode))
                        {
                            ResolvePass("replaced-by-pass");
                            _pendingPassTarget = e.Other;
                            _pendingPassKind = passerEpisode.Kind;
                            Count("pass-contact-" + _pendingPassKind + (e.Other < 0 ? "-zone" : "-receiver"));
                        }
                        else if (_pendingPassTarget != -2) ResolvePass("interrupted-by-kick");
                        _pendingKick = e.Agent;
                        _pendingKickKind = e.Kind.ToString();
                        Touch(e.Agent, _pendingKickKind);
                        break;
                    case W8StageAKind.FirstTouch:
                        Touch(e.Agent, "first-touch-" + e.Other);
                        break;
                    case W8StageAKind.LoosePickup:
                        Touch(e.Agent, "loose-pickup");
                        break;
                    case W8StageAKind.UnattributedDeflection:
                        Touch(-1, "unattributed-collision-deflection");
                        break;
                    case W8StageAKind.TacticalBefore:
                        if (e.Agent >= 0)
                            _tacticalBefore[e.Agent] = _engine.TestOnly_GoalkeeperState.States[e.Other];
                        break;
                    case W8StageAKind.TacticalAfter:
                        if (e.Agent >= 0 && _tacticalBefore.TryGetValue(e.Agent, out GoalkeeperState prior))
                        {
                            GoalkeeperTickState state = _engine.TestOnly_GoalkeeperState;
                            GoalkeeperState after = state.States[e.Other];
                            if (prior != after)
                            {
                                Count("transition-" + prior + "-to-" + after);
                                _output.AppendLine(Inv($"transition,0x{_seed:X16},keeper={e.Agent},frame={e.Frame},from={prior},to={after},storedClaimTick={state.ClaimTick[e.Other]},normalizedTacticalTick={e.Frame / 6},elapsedFrames={(_open.TryGetValue(e.Agent, out Episode held) ? e.Frame - held.Start : -1)}"));
                                if (_open.TryGetValue(e.Agent, out Episode episode))
                                    episode.LastTransition = e.Frame;
                            }
                        }
                        break;
                    case W8StageAKind.KeeperDecision:
                        if (_open.ContainsKey(e.Agent))
                        {
                            ActionType action = (ActionType)e.Other;
                            _decision[e.Agent] = action;
                            Count("keeper-decision-" + action);
                        }
                        break;
                }
            }

            private void Open(int agent, string kind, W8StageAEvent e)
            {
                if (_open.ContainsKey(agent)) Close(agent, e.Frame, "replacement-claim");
                int team = _keepers[agent];
                var p = _engine.TestOnly_AgentSnapshot(agent).Position;
                string area = Area(team, p);
                int target = SelectTarget(agent, team, p, out float distance);
                int slot = team;
                var state = _engine.TestOnly_GoalkeeperState;
                var episode = new Episode
                {
                    Agent = agent, Team = team, Kind = kind, Start = e.Frame,
                    LastAgent = _lastTouch, LastKind = _lastTouchKind, LastTeam = _lastTouchTeam,
                    Area = area, X = p.x, Y = p.y,
                    ContactArea = Area(team, new Vector2(e.BallPosition.x, e.BallPosition.y)),
                    ContactX = e.BallPosition.x, ContactY = e.BallPosition.y,
                    Target = target, Distance = distance,
                    Policy = _engine.TestOnly_W8DistributionPolicy(team),
                    ClaimTick = kind == "hand" ? -1 : state.ClaimTick[slot], LastTransition = -1
                };
                _open[agent] = episode;
                Count("episode-" + kind + "-team-" + team);
                if (kind == "hand")
                {
                    _totalClaims++;
                    Count("claim-keeper-" + agent);
                    Count("claim-keeper-area-" + area);
                    Count("claim-contact-area-" + episode.ContactArea);
                    if (_lastTouch < 0) _unknownTouchClaims++;
                    if (_lastTouchTeam == team && _lastTouch >= 0 && _lastTouch != agent)
                    {
                        Count("claim-after-teammate-touch");
                        if (_lastTouchKind == "PassKick" || _lastTouchKind == "ShotKick")
                            Count("claim-after-teammate-deliberate-kick-candidate");
                    }
                }
                if (_recentDrop.TryGetValue(agent, out int dropFrame) && e.Frame - dropFrame <= 300)
                {
                    if (kind == "hand") _reclaimsHand++; else _reclaimsFeet++;
                    _recentDrop.Remove(agent);
                }
                Count(target < 0 ? "dry-zone-fallback" : "dry-receiver");
            }

            private int SelectTarget(int keeper, int team, Vector2 position, out float distance)
            {
                int selected = -1;
                float bestSquared = 25f * 25f;
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    if (i == keeper || _engine.TestOnly_IsGoalkeeper(i)
                        || _engine.TestOnly_W8TeamId(i) != team || _engine.TestOnly_IsSentOff(i))
                        continue;
                    float squared = (_engine.TestOnly_AgentSnapshot(i).Position - position).sqrMagnitude;
                    if (squared > bestSquared) continue;
                    if (selected < 0 || squared < bestSquared)
                    {
                        selected = i;
                        bestSquared = squared;
                    }
                }
                distance = selected < 0 ? -1f : Mathf.Sqrt(bestSquared);
                return selected;
            }

            private static string Area(int team, Vector2 p)
            {
                float edge = team == 0 ? 16.5f : 105f - 16.5f;
                bool xInside = team == 0 ? p.x < edge : p.x > edge;
                bool xAt = p.x == edge;
                bool yInside = p.y > 34f - 20.16f && p.y < 34f + 20.16f;
                bool yAt = p.y == 34f - 20.16f || p.y == 34f + 20.16f;
                if ((xInside || xAt) && (yInside || yAt) && p.x >= 0f && p.x <= 105f)
                    return xAt || yAt ? "boundary" : "inside";
                return "outside";
            }

            private void ResolvePass(string result)
            {
                if (_pendingPassTarget == -2) return;
                Count("pass-outcome-" + _pendingPassKind + "-" + result);
                _pendingPassTarget = -2;
                _pendingPassKind = null;
            }

            private void Close(int agent, int frame, string reason)
            {
                if (!_open.TryGetValue(agent, out Episode episode)) return;
                _open.Remove(agent);
                Count("end-" + episode.Kind + "-" + reason);
                string action = _decision.TryGetValue(agent, out ActionType dt) ? dt.ToString() : "none";
                string fallback = episode.Target < 0 ? "zone" : "receiver";
                string target = episode.Target < 0
                    ? (episode.Team == 0 ? "zone_35_34" : "zone_70_34")
                    : episode.Target.ToString(CultureInfo.InvariantCulture);
                _output.AppendLine(Inv($"0x{_seed:X16},{episode.Agent},{episode.Team},{episode.Kind},{episode.Start},{frame},{frame - episode.Start},{reason},{episode.LastAgent},{episode.LastKind},{episode.LastTeam},{episode.Area},{episode.X:F3},{episode.Y:F3},{episode.ContactArea},{episode.ContactX:F3},{episode.ContactY:F3},{action},{episode.ClaimTick},{episode.LastTransition},{episode.Start / 6},{target},{episode.Distance:F3},{fallback},{episode.Policy}"));
                _decision.Remove(agent);
            }

            internal void AfterTick()
            {
                _lastFrame = (int)_engine.CurrentTick;
                foreach (int keeper in _keepers.Keys)
                    if (!_engine.TestOnly_IsSentOff(keeper))
                    {
                        _eligibleFrames.TryGetValue(keeper, out int frames);
                        _eligibleFrames[keeper] = frames + 1;
                    }
                GoalkeeperTickState state = _engine.TestOnly_GoalkeeperState;
                foreach (var pair in _open)
                    if (pair.Value.Kind == "hand" && pair.Value.ClaimTick < 0)
                        pair.Value.ClaimTick = state.ClaimTick[pair.Value.Team];
            }

            internal void Finish()
            {
                ResolvePass("censored-at-fulltime");
                foreach (int keeper in new List<int>(_open.Keys)) Close(keeper, _lastFrame, "censored-at-fulltime");
                _output.AppendLine(Inv($"summary,0x{_seed:X16},claims={_totalClaims},unknownLastTouch={_unknownTouchClaims},drops={_drops},sameKeeperReclaimHandWithin300={_reclaimsHand},sameKeeperReclaimFeetWithin300={_reclaimsFeet},exposureKeeperMatchMinutes=90"));
                var keys = new List<string>(_counts.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (string key in keys)
                    _output.AppendLine(Inv($"count,0x{_seed:X16},{key},{_counts[key]}"));
                foreach (int keeper in _keepers.Keys)
                {
                    _eligibleFrames.TryGetValue(keeper, out int frames);
                    _output.AppendLine(Inv($"exposure,0x{_seed:X16},keeper={keeper},team={_keepers[keeper]},eligibleFrames={frames},eligibleMinutes={frames / 3600f:F4}"));
                }
            }

            private sealed class Episode
            {
                internal int Agent, Team, Start, LastAgent, LastTeam, Target, ClaimTick, LastTransition;
                internal float X, Y, ContactX, ContactY, Distance;
                internal string Kind, LastKind, Area, ContactArea;
                internal GkDistributionPolicy Policy;
            }
        }
    }
}
