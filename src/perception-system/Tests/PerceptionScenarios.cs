// File:     src/perception-system/Tests/PerceptionScenarios.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Perception System #7 §3.0/§3.8/§4.5.2/§5.11.4,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #7 (owned by #7 per #19
//           §3.3.1 / KD-8). Drives the REAL 22-agent PerceptionSystem.OnHeartbeat
//           orchestrator headlessly on the #19 ScenarioRunner — the production entry
//           point that had zero executing coverage (every §5.11 multi-agent
//           integration test was an Assert.Ignore("requires Play Mode") stub, and
//           OnHeartbeat is called nowhere in the unit suite). The ScenarioRunner is
//           exactly the headless mechanism those stubs deferred to.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.CollisionSystem;
using TacticalDirector.TestingStrategy;

// The class TacticalDirector.PerceptionSystem.PerceptionSystem shares its leaf name
// with its own namespace; alias it (the match-engine composition-root precedent) so an
// unqualified reference from this nested Tests namespace is unambiguous.
using PerceptionSubsystem = TacticalDirector.PerceptionSystem.PerceptionSystem;

namespace TacticalDirector.PerceptionSystem.Tests
{
    /// <summary>
    /// Builds the Spec #7 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 because initial state is constructed in code
    /// (canonical fixture files land at Stage 0+1 per #19 KD-10). The perception pipeline
    /// draws no per-tick randomness at Stage 0 (recognition latency is a deterministic
    /// Wang/Jenkins hash), so recorded seeds are canonical identifiers per FR-TS-025, not
    /// behaviour inputs. Tier B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class PerceptionScenarios
    {
        public const string AllSnapshotsPath = "tests/scenarios/perception-system/full-heartbeat-all-snapshots";
        public const string ColdStartPath    = "tests/scenarios/perception-system/awareness-cold-start-builds";
        public const string DeterministicPath = "tests/scenarios/perception-system/two-instance-determinism";

        public const ulong AllSnapshotsSeed  = 1UL;
        public const ulong ColdStartSeed     = 2UL;
        public const ulong DeterministicSeed = 3UL;

        // A two-line facing world: team 0 on x=35 facing +X, team 1 on x=70 facing −X,
        // sharing the same y-lines so each agent looks directly across at an opponent.
        private static readonly Vector2 TeamAFacing = Vector2.right;
        private static readonly Vector2 TeamBFacing = Vector2.left;

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(AllSnapshotsPath,  "full-heartbeat-all-snapshots", AllSnapshotsSeed,  FullHeartbeatAllSnapshots),
                Entry(ColdStartPath,     "awareness-cold-start-builds",  ColdStartSeed,     AwarenessColdStartBuilds),
                Entry(DeterministicPath, "two-instance-determinism",     DeterministicSeed, TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 7 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── world construction + driving ────────────────────────────────────────────────

        private static void BuildWorld(out AgentState[] agents, out PerceptionAgentAttributes[] attrs,
            out bool[] possession, out BallState ball)
        {
            int n = PerceptionConstants.MaxAgents;
            int half = n / 2;
            agents = new AgentState[n];
            attrs = new PerceptionAgentAttributes[n];
            possession = new bool[n];
            for (int i = 0; i < n; i++)
            {
                bool teamA = i < half;
                int lineIndex = teamA ? i : i - half;
                float x = teamA ? 35.0f : 70.0f;
                float y = 8.0f + lineIndex * 5.0f; // spread across the 68 m width, within bounds
                agents[i] = AgentState.CreateAtPosition(new Vector2(x, y), teamA ? TeamAFacing : TeamBFacing);
                attrs[i] = PerceptionAgentAttributes.CreateDefault();
                attrs[i].TeamId = teamA ? 0 : 1;
                possession[i] = false;
            }
            ball = BallState.CreateAtPosition(new Vector3(52.5f, 34.0f, 0.11f));
        }

        // Mirrors MatchEngine.PopulatePerceptionGrid — the host owns the broad-phase grid and
        // re-inserts every agent each heartbeat (point insert; the 120 m query window covers the pitch).
        private static void PopulateGrid(SpatialHashGrid grid, AgentState[] agents)
        {
            grid.Clear();
            for (int i = 0; i < agents.Length; i++)
            {
                Vector2 p = agents[i].Position;
                grid.Insert(i, new Vector3(p.x, p.y, 0.0f), 0.0f);
            }
        }

        private static PerceptionSubsystem RunHeartbeats(int heartbeats)
        {
            BuildWorld(out AgentState[] agents, out PerceptionAgentAttributes[] attrs,
                out bool[] possession, out BallState ball);
            var grid = new SpatialHashGrid();
            var sys = new PerceptionSubsystem(grid);
            for (int h = 0; h < heartbeats; h++)
            {
                PopulateGrid(grid, agents);
                sys.OnHeartbeat(h, agents, ball, attrs, possession);
            }
            return sys;
        }

        private static int TotalPerceived(PerceptionSubsystem sys)
        {
            int total = 0;
            for (int i = 0; i < PerceptionConstants.MaxAgents; i++)
            {
                FilteredView v = sys.GetFilteredView(i);
                total += v.VisibleOpponentsCount + v.VisibleTeammatesCount;
            }
            return total;
        }

        // FNV-1a-64 over the integer content of every FilteredView (ids + counts + ball flags) —
        // a divergence-sensitive digest that is float-free, so it is a pure determinism signal.
        private static ulong DigestViews(PerceptionSubsystem sys)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < PerceptionConstants.MaxAgents; i++)
            {
                FilteredView v = sys.GetFilteredView(i);
                h = Mix(h, v.ObserverId);
                h = Mix(h, v.FrameNumber);
                h = Mix(h, v.BallVisible ? 1 : 0);
                h = Mix(h, v.BallStalenessFrames);
                h = Mix(h, v.VisibleOpponentsCount);
                h = Mix(h, v.VisibleTeammatesCount);
                h = Mix(h, v.BlindSidePerceivedAgentsCount);
                for (int k = 0; k < v.VisibleOpponentsCount; k++) h = Mix(h, v.VisibleOpponents[k].AgentId);
                for (int k = 0; k < v.VisibleTeammatesCount; k++) h = Mix(h, v.VisibleTeammates[k].AgentId);
            }
            return h;
        }

        private static ulong Mix(ulong h, int value)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                h ^= (uint)value;
                h *= 1099511628211UL;
            }
            return h;
        }

        // ── scenarios ────────────────────────────────────────────────────────────────────

        // The §5.11.4 FullHeartbeat_22Agents_AllSnapshotsProducedNoNulls stub, made real: every
        // agent's view is stamped with its own id + the heartbeat frame, perceived counts stay
        // within their pre-allocated array bounds (INV-10), and the composed pipeline actually
        // produces non-trivial perception (agents facing opponents across the pitch see them).
        private static void FullHeartbeatAllSnapshots(ScenarioContext context)
        {
            PerceptionSubsystem sys = RunHeartbeats(30); // 3 s — recognition latency warmed up

            bool allStamped = true;
            bool countsInBounds = true;
            for (int i = 0; i < PerceptionConstants.MaxAgents; i++)
            {
                FilteredView v = sys.GetFilteredView(i);
                allStamped &= v.ObserverId == i && v.FrameNumber == 29;
                countsInBounds &=
                    v.VisibleOpponentsCount >= 0 && v.VisibleOpponentsCount <= PerceptionConstants.MaxVisibleOpponents &&
                    v.VisibleTeammatesCount >= 0 && v.VisibleTeammatesCount <= PerceptionConstants.MaxVisibleTeammates &&
                    v.BlindSidePerceivedAgentsCount >= 0 && v.BlindSidePerceivedAgentsCount <= PerceptionConstants.MaxBlindSideAgents;
            }

            context.Envelope.CheckTrue("all-views-stamped-with-own-id-and-frame", allStamped,
                "every FilteredView carries ObserverId == agentId and the heartbeat FrameNumber");
            context.Envelope.CheckTrue("perceived-counts-within-array-bounds", countsInBounds,
                "VisibleOpponents/Teammates/BlindSide counts must never exceed their pre-allocated buffers");
            context.Envelope.CheckTrue("pipeline-perceives-entities", TotalPerceived(sys) > 0,
                "the 22-agent orchestrator produces non-trivial perception, total=" +
                    TotalPerceived(sys).ToString(CultureInfo.InvariantCulture));
        }

        // The §5.11.4 Awareness_BuildsOverFirstTicks_ColdStart stub, made real: from a cold start
        // recognition latency confirms nothing instantly; aggregate perception is non-decreasing as
        // latency counters elapse and is non-zero once warmed up.
        private static void AwarenessColdStartBuilds(ScenarioContext context)
        {
            BuildWorld(out AgentState[] agents, out PerceptionAgentAttributes[] attrs,
                out bool[] possession, out BallState ball);
            var grid = new SpatialHashGrid();
            var sys = new PerceptionSubsystem(grid);

            PopulateGrid(grid, agents);
            sys.OnHeartbeat(0, agents, ball, attrs, possession);
            int early = TotalPerceived(sys);

            for (int h = 1; h < 30; h++)
            {
                PopulateGrid(grid, agents);
                sys.OnHeartbeat(h, agents, ball, attrs, possession);
            }
            int late = TotalPerceived(sys);

            context.Envelope.CheckTrue("awareness-non-decreasing", late >= early,
                "cold-start awareness must not shrink as recognition latency elapses: early=" +
                    early.ToString(CultureInfo.InvariantCulture) + " late=" + late.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckTrue("awareness-nonzero-after-warmup", late > 0,
                "agents facing opponents in range confirm them within 3 s");
        }

        // Two independent PerceptionSystem instances driven over an identical world + heartbeat
        // sequence must produce byte-identical perception — the Simulation-layer determinism lock
        // (#19 §5) the pure-function unit suite cannot express.
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            ulong digestA = DigestViews(RunHeartbeats(10));
            ulong digestB = DigestViews(RunHeartbeats(10));

            context.Envelope.CheckTrue("two-instance-digest-equal", digestA == digestB,
                "digestA=" + digestA.ToString(CultureInfo.InvariantCulture) +
                    " digestB=" + digestB.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckTrue("digest-non-degenerate", digestA != 14695981039346656037UL,
                "the digest actually folded perception output (not the empty FNV basis)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-03 | —      | Initial implementation: Spec #7 closed-loop scenario corpus   |
// |         |            |        | on the #19 ScenarioRunner — drives the real 22-agent          |
// |         |            |        | OnHeartbeat orchestrator (previously zero executing coverage; |
// |         |            |        | the §5.11 integration tests are Play-Mode Assert.Ignore stubs).|
#endregion
