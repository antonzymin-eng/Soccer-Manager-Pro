# Living World System Specification #22 — Section 4: Architecture

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.3 — PASS-2 fix pass: §4.5 eviction extended to cold-summary
compaction, edges governed by the active-set cap not eviction (AR2-M3); §4.2 sequences the vol-2/vol-3
daily update as a prior phase the loop consumes (AR2-L3))
**Version:** 0.3
**Status:** IN REVIEW (June 21, 2026)

---

## 4.1 Assembly placement (KD-2)

`TacticalDirector.LivingWorld` is a **top-layer off-pitch assembly**. Its only outbound references are
**downward**, to the human-systems/data assemblies (the vol-2/vol-3 model) and `project-constants`. No
match hot-path assembly (`Physics`/`Mechanics`/`AI`/`MatchEngine`) references it (FR-LW-002/003). The
match engine is read **only** through structured outcome events the world loop consumes; the engine
never calls into the world. This keeps the reference graph acyclic and the 10/60 Hz hot path free of
off-pitch code (parallels #21's `tactics/` placement rule).

```
project-constants  ◄─── human-systems (vol-2/vol-3 model)  ◄─── LivingWorld (this)
match-engine ──(emits outcome events)──►  LivingWorld        (one-way, data only)
```

## 4.2 The season-calendar loop (KD-4)

A new deterministic **season-calendar clock** owns the world loop — distinct from
`src/deterministic-sim/MatchClock.cs` (match time only; no calendar clock exists today, so this layer
introduces one). The loop is event- and day-driven; `worldTick` = one calendar day, the unit vol-2
§2.2 latencies use. It runs **never** inside the 10 Hz tactical or 60 Hz physics loops (loop-conflation
hazard, CLAUDE.md). The vol-2/vol-3 daily human-systems update (H-Gate, §2.2 propagation) is a **prior
season-loop phase owned outside this layer**; the world loop runs after it and consumes its committed
output. Per-tick order: (1) ingest match-outcome events; (2) **read** the already-committed canonical
human-systems state for this tick (owned/run by vol-2/vol-3, not by this loop — FR-LW-027/KD-9; read and
route only); (3) memory salience decay (§3.2); (4) arc evaluation (§3.4); (5) background-tier update
(§3.5); (6) budget/eviction (§4.5).

## 4.3 File layout (one type per file, #20)

`LivingWorldConstants.cs` (catalogue); `RelationshipEdge.cs`, `MemoryEpisode.cs`, `SpawnCause.cs`,
`Arc.cs`, `ColdSummary.cs`; enums `EventKind.cs`, `ArcKind.cs`, `InteractionIntent.cs`,
`RelationshipLayer.cs`; services `WorldClock.cs`, `WorldLoop.cs`, `MemoryStore.cs`, `ArcEngine.cs`,
`InteractionTextGenerator.cs`, `BackgroundTierSim.cs`, `ColdStore.cs`. `AssemblyInfo.cs` with
`InternalsVisibleTo` for the test assembly.

## 4.4 Determinism boundaries (KD-5)

- **RNG:** one dedicated `DeterministicRngService` world stream, `Reserve`/`DrawReserved`/`Skip`
  (FR-LW-020). SplitMix64 is only that service's construction-time match-seed PRNG; per-draw is
  HKDF-SHA256 + SipHash-2-4-64 — not re-implemented here.
- **Iteration:** canonical entity-ID order for entity passes; fixed `ArcKind` ordinal for non-entity
  arcs (FR-LW-021/017).
- **Snapshot:** all state is serialised value state; single-machine snapshot determinism (replay,
  save/load, debug-rewind). Cross-platform bit-exact parity stays Stage 5+ (CLAUDE.md).
- **No write-back:** the loop reads canonical human-systems state and match-outcome events only; it
  never mutates the H-Gate or vol-2 §2.2 propagation math (FR-LW-027, KD-9).

## 4.5 Save-size budget and eviction (FR-LW-026)

One `[GT]` budget caps the three live-state classes together — **live edges + live episodes + cold
summaries**. Eviction order when exceeded: (1) lowest-salience **unpinned** episode (§3.2); (2) if relief
is still needed, **cold-summary compaction** — the lowest-`NetRelationship` summary drops its
lowest-salience `RetainedEpisode`, then the summary itself is dropped once empty. Live edges are **not**
an eviction target: they are bounded by the active-set cap (`ACTIVE_SET_EXTERNAL_CONTACTS_MAX`,
FR-LW-023), so the active set is the edge governor, not eviction. Arc-pinned episodes are exempt until
the arc resolves. The per-class budget split is **deferred** (§7 residue B): default is a single shared
pool + the above eviction order, split into sub-quotas only if §6 soak shows one class starving another.

## 4.6 Snapshot schema integration

Once the persistent world store exists (KD-10), living-world state enters the canonical snapshot field
set with a `SNAPSHOT_SCHEMA_VERSION` bump (#16); field order is pinned in Appendix B before first
serialisation. Until then the types are authorable and unit-testable in isolation (FR-LW-022).
