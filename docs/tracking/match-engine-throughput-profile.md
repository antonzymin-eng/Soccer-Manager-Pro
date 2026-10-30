# Match engine headless throughput — profile and ablation results

> **Created:** October 14, 2026
> **Purpose:** Records where a full headless `MatchEngine` match spends its time, measured by CPU
> sampling and by timed ablations. Written to answer one question: is the per-match cost that rules
> out simulating background fixtures through the real engine (roadmap C1) caused by the physics
> simulation, or by something else?
> **Measured source:** `8344024` (main after PR #504). Nothing in `src/` was changed by this record.
> **Status:** descriptive measurement record. It proposes candidate fixes; it decides nothing, tunes
> nothing, and is not a spec. Non-certifying (Linux .NET 8), single seed — see §6 Limits.

## 1. Summary

The short answer is **no: physics is not where the time goes.** On one full 324,000-tick match,
about two-thirds of the wall time is spent on work that does not simulate football:

| Variant (cumulative, sequential runs) | s / match | ms / tick | Saved | Share of baseline |
|---|---:|---:|---:|---:|
| Baseline | **78.1** | 0.2409 | — | — |
| + order-preserving perception spatial query | 53.2 | 0.1643 | 24.9 s | 32% |
| + per-tick snapshot digest (SHA-256) skipped | 39.9 | 0.1231 | 13.3 s | 17% |
| + per-tick world-state serialization skipped | **26.2** | 0.0810 | 13.7 s | 18% |
| Remaining (the simulation itself) | 26.2 | — | — | 34% |

1. **The perception spatial query is wasteful: 32% of the match.** It is removable with **no
   behaviour change**. The variant produced a byte-identical final snapshot digest over the whole
   match (§3.1).
2. **The per-tick snapshot costs 35% of the match.** That is serializing a 35,252-byte world state and
   hashing it every tick. It is replay/desync bookkeeping and does not feed back into play. **All of
   the match's 11.5 GB of heap allocation comes from the digest step** (§3.2).
3. **The simulation itself is about 26 s.** Perception's own per-agent pipeline is roughly half of
   that. Physics (agent movement, collision, ball flight) is a minority share, approximately 15–25% of
   the remainder, i.e. **≈ 5–8% of the baseline** (§4).

The hypothesis that prompted this measurement was "FM is fast because it has no physics engine". It
is not supported for this codebase. Fixing (1) and making (2) on-demand would make a match about
3× cheaper without simplifying any football. That still leaves a 10-fixture round at several minutes
single-threaded. The next lever after those two is perception, not physics.

## 2. Method and environment

- **Host:** cloud container, Intel Xeon @ 2.10 GHz (`sha_ni` present), 4 vCPU. .NET SDK 8.0.131
  (Ubuntu package), CoreCLR, tiered PGO on. Release build of the generated solution
  (`tools/dotnet-ci/generate_projects.py`). This is the non-certifying Linux lane, **not** the
  pinned Windows/Unity/Mono host.
- **Harness:** a scratch console program (Appendix A, not added to `src/`). It constructs
  `new MatchEngine(seed)`, calls `ConfigureSquads` with two `RosterGenerator` squads (3 GK / 8 DEF /
  8 MID / rest FWD), and calls `RunTick()` until `MatchEnded`. The stopwatch covers construction plus
  the whole tick loop. It reads the final `CurrentSnapshotDigest` and the last tick's payload size.
- **Seed:** `0x9E3779B97F4A7C15`. Every run below ended **8–4** with **324,000** ticks. Scorelines
  are not the subject of this record and say nothing about realism here.
- **CPU profile:** `dotnet-trace collect --profile dotnet-common,dotnet-sampled-thread-time`
  (≈100 Hz managed stack sampling), converted to Speedscope. Inclusive time per frame and
  parent→child edges were aggregated with a short script.
- **Ablations:** three environment-variable switches compiled into a **scratch worktree copy**
  (Appendix B). Each switch is off by default, so the worktree baseline is the production code path.
  The final table in §1 comes from four **sequential** single-match runs of one build, to avoid
  contention between concurrent runs.

### 2.1 All runs, for variance

| Run | Conditions | s / match | Notes |
|---|---|---:|---|
| Main checkout, engine-default squads | sequential | 72.9 | score 8–6; 11,524 MB allocated; 84 gen-0 GCs |
| Main checkout, configured squads | sequential | 77.1 | digest `3A03004D…A85586`; payload 35,252 B/tick |
| Worktree baseline | 2 concurrent runs | 73.9 | identical digest to the main checkout |
| Fast query only (over-broad variant) | 2 concurrent | 56.3 | identical digest |
| Digest skipped | 2 concurrent | 62.0 | 3 MB allocated, 0 gen-0 GCs |
| Digest + serialization skipped | 2 concurrent | 46.9 | |
| All three (over-broad fast query) | single | 27.5 | collision slower than baseline; see §3.1 |
| **Worktree baseline** | **sequential** | **78.1** | **§1 table** |
| **Fast query (threshold variant)** | **sequential** | **53.2** | **identical digest** |
| **+ digest skipped** | **sequential** | **39.9** | |
| **+ serialization skipped** | **sequential** | **26.2** | |

Run-to-run spread for the same configuration is about ±5% (72.9 / 73.9 / 77.1 / 78.1). The savings
are far larger than that spread, and the concurrent and sequential series agree on each step's size.

## 3. Findings

### 3.1 Perception spatial query — 24.9 s / match, removable without behaviour change

`PerceptionSystem.RunAgentPipeline` calls
`SpatialHashGrid.Query(observerPos, PerceptionConstants.MaxPerceptionRange)` once per agent per AI
heartbeat (`src/perception-system/PerceptionSystem.cs:328`). Three constants interact here:

- `MaxPerceptionRange` = **120 m** (`PerceptionConstants.cs:187`);
- the grid's `CellSize` = **1 m** on a 106 × 69 grid (`CollisionSystemConstants.cs:40–54`);
- `Query` walks every cell in the square of half-width `ceil(radius / CellSize)`
  (`SpatialHashGrid.cs:112–143`).

So each query iterates 241 columns for each in-grid row: about 16,600 cell-index steps, to find at
most 22 agents. With 54,000 heartbeats × 22 agents ≈ **1.19 M queries per match**, that is roughly
20 billion loop iterations. A 120 m radius covers the whole pitch, so the grid prunes nothing for
this caller.

**Ablation:** when the query square is larger than 4× the occupied-cell count, iterate only the
occupied cells inside the square, sorted by cell index. That reproduces the original scan order
exactly. **The final snapshot digest was byte-identical to baseline**
(`3A03004D4418AA571C31842B9F40AD9973CAC0149C847B6537B6FDBC47A85586`). The digest chains through
`PrevSnapshotDigest` every tick, so equality covers all 324,000 per-tick preimages, not just the
final state.

**Negative result kept on purpose:** the first variant applied the occupied-cell path to every query.
That made `CollisionSystem.UpdateCollisions` *slower*: its per-agent query uses the hitbox radius
(`CollisionSystem.cs:349`), a 3 × 3 cell window that the original loop already handles cheaply, and
the scan-plus-sort cost more. Any real fix must keep the cheap path for small radii, or give
perception its own all-agents enumeration.

### 3.2 Per-tick snapshot serialization + digest — 27.0 s / match

Every tick, `TickOrchestrator.RunTick` runs the Snapshot phase (`TickOrchestrator.cs:166–176`):

- `MatchEngine.RunSnapshotPhase` → `SerializeWorldState` writes the canonical world state plus the
  event ledger into the payload. That is **35,252 bytes per tick** at this commit (~11.4 GB per match).
- `SnapshotCodec.Encode` → `ComputeSnapshotDigest` SHA-256-hashes the header preimage plus the payload
  and chains the result (`SnapshotCodec.cs:64–113`).

Measured: serialization **13.7 s**, digest **13.3 s**.

**Allocation:** the baseline allocates **11,524 MB per match** (≈ 35.6 KB per tick, the payload size)
and runs 84 gen-0 collections. With the digest skipped this falls to **3 MB** and **0** gen-0
collections, so the allocation is inside `ComputeSnapshotDigest`. Its size is consistent with
`HashAlgorithm.TransformFinalBlock`, which returns a copy of its input buffer. That attribution is
**inferred** from size and the ablation, not traced to the allocation site. Two other allocations
also happen per tick but are small: `SHA256.Create()` and the `sha.Hash` result.

This is in tension with `TickOrchestrator.RunTick`'s own summary, which reads "Zero heap allocation.
§3.1.2", and with the `[FIXED]` 0 bytes/tick hot-path allocation budget cited by
`MatchEngineCapstonePerfHarness` (`allocBytes = 0`). Not filed as an ERR by this record; see §7.

**Scope** (answers "match or world?"): the per-tick digest belongs to the **match engine only**.
`TickOrchestrator` is owned by `MatchEngine`. The season/world day loop (`SeasonLoop`,
`WorldStore`) has no per-tick or per-day digest; season state is encoded only when saved
(`SeasonStateCodec`). Quick-sim fixtures (`RoundResolutionModel`) incur none of this cost.

**Is it required?** Deterministic Simulation #16 §3 mandates per-tick phase digests ("MUST be emitted
on every non-stride tick so that phase digests … appear in the digest rollup at every tick"). Making
the digest on-demand, e.g. only for watched, saved or replay-verified matches, is therefore a **#16
spec decision, not a code switch.** Serialization is separately needed for mid-match save/restore
(`CaptureDurablePayload`), but a save needs it at the save tick, not at all 324,000 ticks.

### 3.3 Perception: what it reads and who consumes it

This answers whether perception works from raw physics or from pre-filtered data. **It does both,
in sequence, and only part of the AI uses its output.**

- **Input is ground truth:** `_perception.OnHeartbeat(heartbeat, _agents, _ball, _perceptionAttrs,
  _hasPossession)` (`MatchEngine.cs:3329`) receives true agent and ball state from physics.
- **Output is a per-agent `FilteredView`**, shaped by:
  - range;
  - field of view narrowed by the **Decisions** attribute and the pressure scalar
    (`FovCalculator.ComputeEffectiveFoV`);
  - body occlusion (`OcclusionFilter`);
  - half-turn orientation;
  - a shoulder-check scheduler driven by **Anticipation**;
  - latency/staleness.
- **Who reads `FilteredView`:**
  - Each player's `DecisionTree` (`MatchEngine.cs:3367`, `ReceiveSnapshot(view, …)`).
  - The dismarking/marker-pressure input to positioning (`ExtractPerceivedOpponents`,
    `MatchEngine.cs:4202`).
- **Who reads ground truth directly:** team-level AI. `FillPressingSnapshot`,
  `FillDefensiveSnapshot` and `FillAttackingSnapshot` (`MatchEngine.cs:4222–4470`) read `_agents[…]`
  positions with no `FilteredView` use. Individual choices are perception-limited; team shape is
  omniscient.

The cost of perception after §3.1 is the per-agent geometric pipeline: FoV tests, occlusion and
shoulder-check scheduling for 22 observers × ~23 entities at 10 Hz. That is about 27 M
observer–entity evaluations per match.

## 4. What the remaining ~26 s is made of

This comes from a CPU profile of the all-three-switches variant, which ran 30.5 s under the
profiler. **Caveat:** that profile used the over-broad fast-query variant, which inflated collision
by roughly 4 s (§3.1). Shares are therefore approximate.

| Area | Inclusive share | Notes |
|---|---:|---|
| Perception `RunAgentPipeline` | ~45% | of which `OcclusionFilter.IsOccludedCore` ~12% and residual query ~3.5%; the rest is the pipeline itself |
| Resolve phase (`CollisionSystem.UpdateCollisions`) | ~22% | inflated by the over-broad query variant; ≈ 2–3 s once corrected |
| Physics phase (`AgentMovementSystem.UpdateAllAgents` ~11%) | ~14% | ball physics `UpdateBallPhysics` ~0.3% |
| Decision tree (`ReceiveSnapshot`, utility scoring) | ~5% | |
| Mechanics AI, first touch, team AI (pressing/defensive/positioning/attacking) | ~10% combined | |
| `Console.WriteLine` from `FirstTouchSystem.ApplyTouchResult` logging | ~3.5% | ~875 lines per match on the shim; Unity `Debug.Log` cost differs |

**Disagreement recorded:** the *baseline* profile attributed only ~1.7% to physics + resolve
together (≈ 1.4 s of 83 s). The ablation remainder implies more, ≈ 4–7 s. ~100 Hz thread-time
sampling with inlined callees is not reliable at that resolution. The ablation timings in §1 are the
primary evidence; profile shares are secondary.

## 5. Consequences for background-fixture simulation (roadmap C1)

- **Per-match cost at the ablated floor:** ~26 s single-threaded on this host. A 10-fixture round
  takes about 4.4 min serially. With 8 parallel workers it would take about 33 s, but parallel
  matches in one process are blocked today by the process-static `EventBus`
  (`src/event-system/EventBus.cs:28`; spec-mandated, #17 §3.2.1 KD-4/KD-8).
- **The C1 lower-bound history stands corrected:** the 154 s estimate and the August capture's
  ~90 s/match were both well above what this host measures at the same engine stage (~73–78 s). That
  difference is unexplained here and may come from host, runtime or engine changes since August.
- **Physics fidelity is not the binding cost.** If a lighter "unwatched match" mode is pursued later,
  the evidence points at **perception** (and team AI cadence) as the place to simplify, not ball or
  agent physics.
- **Data for FM-style statistics is a separate gap, not a speed one.** The engine publishes only 9
  event kinds (#37 §1.4). Shots, passes, tackles and saves have registered ordinals but no producer,
  so even the user's own match cannot yet feed data-hub statistics.

## 6. Limits

- **Single seed, single host, non-certifying runtime.** CoreCLR with tiered PGO is not Unity Mono or
  IL2CPP. Absolute numbers will differ on the shipping runtime. Treat ratios as the result and
  absolute seconds as indicative.
- One configured-squad setup (`RosterGenerator`), not league squads from `League` or `SeasonLoop`.
  `PlayThroughEngine` additionally pumps the discipline tap per tick; that was not measured.
- The digest-skipped and serialization-skipped variants cannot be digest-compared, because they remove
  the digest. Behavioural equivalence there rests on identical scorelines and on neither step feeding
  back into simulation state. That second point was checked by reading the phase order, not proven by
  a test.
- The allocation-site attribution in §3.2 is inferred, not traced.

## 7. Candidate follow-ups (not decided here)

1. **Perception query fix.** Behaviour-neutral by the §3.1 digest check. It needs a real
   implementation (radius-gated, or a dedicated all-agents path for perception), a test locking digest
   identity across the seed set, and a Unity compile. Largest single saving; touches no `[GT]`, no
   schema and no RNG.
2. **Digest allocation fix.** Feeding the hash through `IncrementalHash` (available on .NET Standard
   2.1) or span-based APIs should produce the identical digest without the per-tick payload copy.
   **Not measured.** It would bring `RunTick` back in line with its stated zero-allocation contract.
   Whether the current behaviour is an ERR against #16 §3.1.2 / #18's 0 B/tick budget is for the
   owner or `err-file-and-backprop` to decide.
3. **#16 decision: on-demand snapshot/digest for headless matches.** This is worth up to 35% and is a
   spec change.
4. **Perception pipeline profiling at function level** before considering any lighter background
   mode.
5. **Stale comments noticed during the investigation** (not fixed here):
   - `RoundResolutionModel.cs:46` still says calibration is "NOT DONE", although
     `SeasonLoopConstants` carries the August 12 fitted values.
   - `RoundResolutionMode.cs` cites the superseded "~2 minutes / ≥ 16 hours" sizing.

## Appendix A — harness (`Program.cs`, scratch, net8.0 console referencing `match-engine.gen.csproj`)

```csharp
// Scratch profiling harness: plays full matches headless and reports wall time per match.
using System;
using System.Diagnostics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

static class Program
{
    static Squad BuildSquad(ulong seed, int clubId)
    {
        var rng = new DeterministicRngService(seed ^ (ulong)clubId);
        int stream = rng.RegisterStream("prof.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);
        var t = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
        int i = 0;
        for (int k = 0; k < 3; k++) t[i++] = PlayerPosition.Goalkeeper;
        for (int k = 0; k < 8; k++) t[i++] = PlayerPosition.Defender;
        for (int k = 0; k < 8; k++) t[i++] = PlayerPosition.Midfielder;
        while (i < t.Length) t[i++] = PlayerPosition.Forward;
        return RosterGenerator.Generate(rng, stream, clubId, t);
    }

    static int Main(string[] args)
    {
        int matches = args.Length > 0 ? int.Parse(args[0]) : 3;
        bool configured = args.Length > 1 && args[1] == "squads";
        Console.WriteLine($"matches={matches} configuredSquads={configured} ticksPerMatch={MatchEngineConstants.MATCH_TICKS_TOTAL}");
        double total = 0;
        for (int m = 0; m < matches; m++)
        {
            ulong seed = 0x9E3779B97F4A7C15UL * (ulong)(m + 1);
            var sw = Stopwatch.StartNew();
            var e = new MatchEngine(seed);
            if (configured) e.ConfigureSquads(BuildSquad(seed, 1), BuildSquad(seed, 2));
            long ticks = 0;
            while (!e.MatchEnded) { e.RunTick(); ticks++; }
            sw.Stop();
            var orch = typeof(MatchEngine).GetField("_orchestrator", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(e);
            var payload = orch.GetType().GetProperty("CurrentPayload").GetValue(orch);
            var bt = payload.GetType(); int bytes = (int)((object)bt.GetProperty("BytesWritten")?.GetValue(payload) ?? bt.GetField("BytesWritten").GetValue(payload));
            Console.WriteLine($"  payloadBytes/tick={bytes} finalDigest={Convert.ToHexString(e.CurrentSnapshotDigest)}");
            total += sw.Elapsed.TotalSeconds;
            Console.WriteLine($"match {m}: {sw.Elapsed.TotalSeconds:F1}s ticks={ticks} ms/tick={sw.Elapsed.TotalMilliseconds / ticks:F4} score={e.HomeScore}-{e.AwayScore} gc0={GC.CollectionCount(0)} alloc={GC.GetTotalAllocatedBytes() / 1e6:F0}MB");
        }
        Console.WriteLine($"mean {total / matches:F1}s/match");
        return 0;
    }
}
```

Invocation: `dotnet bin/Release/net8.0/matchprof.dll 1 squads`, with optional
`TD_PROF_FASTQUERY=1`, `TD_PROF_NODIGEST=1`, `TD_PROF_NOSERIALIZE=1` against the Appendix B build.

## Appendix B — ablation switches (scratch worktree at `8344024`; never committed to `src/`)

```diff
--- a/src/collision-system/SpatialHashGrid.cs
+++ b/src/collision-system/SpatialHashGrid.cs
@@ -31,6 +31,8 @@ public sealed class SpatialHashGrid
         private readonly List<int> _occupiedCells;
+        private readonly List<int> _profScratch = new List<int>(64);
+        private static readonly bool s_profFast = System.Environment.GetEnvironmentVariable("TD_PROF_FASTQUERY") == "1";
@@ -121,6 +123,24 @@ public List<int> Query(Vector3 position, float radius)
             int gh = SpatialHashConstants.GridHeight;
 
+            if (s_profFast && (2 * cr + 1) * (2 * cr + 1) > 4 * _occupiedCells.Count)
+            {
+                _profScratch.Clear();
+                for (int k = 0; k < _occupiedCells.Count; k++)
+                {
+                    int idx = _occupiedCells[k];
+                    int ox = idx % gw, oy = idx / gw;
+                    if (ox >= cx - cr && ox <= cx + cr && oy >= cy - cr && oy <= cy + cr) _profScratch.Add(idx);
+                }
+                _profScratch.Sort();
+                for (int k = 0; k < _profScratch.Count; k++)
+                {
+                    List<int> c = _cells[_profScratch[k]];
+                    for (int i = 0; i < c.Count; i++) _queryBuffer.Add(c[i]);
+                }
+                return _queryBuffer;
+            }
--- a/src/deterministic-sim/TickOrchestrator.cs
+++ b/src/deterministic-sim/TickOrchestrator.cs
+        private static readonly bool s_profNoDigest = System.Environment.GetEnvironmentVariable("TD_PROF_NODIGEST") == "1";
         public void RunTick()
@@
-                _codec.Encode(_snapshotHeader, _snapshotPayload);
+                if (!s_profNoDigest) _codec.Encode(_snapshotHeader, _snapshotPayload);
--- a/src/match-engine/MatchEngine.cs
+++ b/src/match-engine/MatchEngine.cs
+        private static readonly bool s_profNoSerialize = System.Environment.GetEnvironmentVariable("TD_PROF_NOSERIALIZE") == "1";
         private void RunSnapshotPhase(SnapshotPayload payload)
@@
-            SerializeWorldState(payload);
+            if (!s_profNoSerialize) SerializeWorldState(payload);
```

The over-broad variant in §2.1 / §3.1 is the same diff without the
`(2 * cr + 1) * (2 * cr + 1) > 4 * _occupiedCells.Count` condition.

## Version History

| Version | Date | Change |
|---|---|---|
| v1.0 | October 14, 2026 | Created: CPU profile and timed ablations of one full headless match at `8344024` on the Linux .NET 8 lane. |
