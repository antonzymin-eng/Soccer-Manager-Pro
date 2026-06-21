# Living World System Specification #22 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.7 — PASS-6 fix pass: FR-LW-020 split into separate `world.arcs`/`world.text`
RNG sub-streams to remove the periodic/aperiodic draw-interleaving hazard (AR6-M1))
**Last Updated (prior):** June 21, 2026 (v0.6 — PASS-5 fix pass: FR-LW-016 scoped — durable `SpawnCause` on arcs;
interaction provenance is implicit (no interaction record type) (AR5-M1))
**Last Updated (prior):** June 21, 2026 (v0.5 — PASS-4 fix pass: FR-LW-027 no-write-back scope extended to the
vol-2 §2.1 social-graph edge (`PlayerEdge` read-only) (AR4-L1))
**Last Updated (prior):** June 21, 2026 (v0.4 — PASS-3 fix pass: `PlayerEdge` pinned as a read-only mirror of
vol-2's authoritative edge — never mutated here, removing the double-authority hazard (AR3-M1, FR-LW-004);
`ActiveLayers` bit positions tied to `RelationshipLayer` ordinals (AR3-L1); `ColdSummary` retains
`ActiveLayers` for rehydration (AR3-L2))
**Version:** 0.7
**Status:** IN REVIEW (June 21, 2026)

---

## 2.1 Functional Requirements

Conformance per RFC 2119. Citations resolve to a KD in §1.5 or a downstream section.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-LW-001 | The layer is a supplement: it consumes vol-2/vol-3 human-systems state read-only and introduces no mechanic canon already owns. | MUST | KD-1 |
| FR-LW-002 | All living-world types live in one assembly `TacticalDirector.LivingWorld` that references only the human-systems/data assemblies and `project-constants`, downward. | MUST | KD-2 / #20 §3.5.2 |
| FR-LW-003 | No match hot-path assembly (`Physics`/`Mechanics`/`AI`) may reference this assembly; the match engine consumes nothing from it and is only read by it via outcome events. | MUST | KD-2 / KD-9 |
| FR-LW-004 | The relationship edge adopts vol-2 §2.1's 0.0–1.0 scalar unchanged, including the > 0.6 clique threshold. `PlayerEdge` is consumed **read-only** — this layer mirrors it for arc reads but never mutates it (vol-2 owns its evolution); only `Affinity`/`Trust` are owned/written here. | MUST | KD-3 / KD-9 |
| FR-LW-005 | New relationship layers (`Affinity`, directional `Trust`) are additive parallel layers on the same 0.0–1.0 scale; the player edge is never re-scaled. `Trust` is stored per ordered pair (A→B ≠ B→A). | MUST | KD-3 |
| FR-LW-006 | `Affinity` is the manager's personal relationship with an individual; it is NOT a board-confidence or supporter-trust authority (those stay owned by vol-3 §4 / vol-2 §4.1). | MUST | KD-9 |
| FR-LW-007 | Morale spread is vol-2 §2.2 Pulse Propagation consumed as-is; this layer defines no contagion rule. Faction outcomes are modelled only as arcs (§3.4) that read the propagation result. | MUST | KD-1 |
| FR-LW-008 | Each significant edge carries a bounded ring buffer of `MemoryEpisode`; buffer depth is a `[GT]` constant (target 8–16). | MUST | §3.2 |
| FR-LW-009 | Every `MemoryEpisode` carries a stable per-edge `episodeId` (monotonic within the edge) that survives save/load and is what an arc pins. | MUST | §3.2 |
| FR-LW-010 | Episode salience and salience-decay are `[GT]`; lowest-salience episodes evict first, **except** episodes pinned by a live arc. | MUST | §3.2 / FR-LW-018 |
| FR-LW-011 | Surface text is produced by deterministic template/grammar expansion; template selection draws from the dedicated `DeterministicRngService` world stream. | MUST | KD-6 |
| FR-LW-012 | No generative-model/LLM inference runs on any path whose output is persisted in saved state. Model assistance is offline authoring of the static corpus only. | MUST | KD-6 |
| FR-LW-013 | `InteractionIntent` is graph-/event-driven and separate from surface text; one intent maps to many phrasings. Slot facts are limited to data the match engine actually emits (no assumed derived stats). | MUST | §3.3 |
| FR-LW-014 | An arc spawns when canonical state crosses a `[GT]` threshold; it is a serialised state machine with a defined resolved/escalated lifecycle. | MUST | §3.4 |
| FR-LW-015 | Board arcs read and route into vol-3 §4 (archetype patience, takeover, DoF veto); they do not introduce a board node. The fan node is an aggregate view over vol-2 §4.1 / §5.1. | MUST | KD-1 / KD-9 |
| FR-LW-016 | Every **arc** records a `SpawnCause` (trigger rule/threshold, the input values at spawn, the state-snapshot reference, `worldTick`) at creation. A generated **interaction** carries no separate persisted `SpawnCause`: its provenance is **implicit** — a deterministic function of `(InteractionIntent, RNG cursor, snapshotRef)` reconstructable from the snapshot + cursor (an optional inspector interaction-log MAY store that lightweight tuple). | MUST | KD-8 |
| FR-LW-017 | Arcs not scoped to a single entity (squad/board-level) are evaluated in fixed `ArcKind` ordinal order so spawn order, RNG draw order, and episode pinning are deterministic. | MUST | KD-5 |
| FR-LW-018 | An arc snapshots the facts it needs at spawn and pins its source episodes non-evictable until it resolves; salience eviction may never leave a live arc referencing a dropped episode. | MUST | §3.4 / KD-8 |
| FR-LW-019 | The world ticks on a deterministic season-calendar clock distinct from `MatchClock`; one `worldTick` = one calendar day. The loop never runs inside the 10 Hz / 60 Hz match loops. | MUST | KD-4 |
| FR-LW-020 | All stochastic selection draws from dedicated `DeterministicRngService` world **sub-streams** via `Reserve`/`DrawReserved`/`Skip`; no `System.Random`, no wall-clock. The **periodic** tick-driven draws (`world.arcs`) and the **aperiodic** interaction-text draws (`world.text`) use **separate** sub-streams so player-triggered text generation never perturbs the tick/arc cursor (no cross-source interleaving). | MUST | KD-5 / #16 |
| FR-LW-021 | Every pass that walks the graph iterates nodes/edges in a canonical order keyed on a stable entity ID, never on dictionary/hashset enumeration order. | MUST | KD-5 |
| FR-LW-022 | All living-world state (edges, layers, memory buffers, arcs, cold summaries) is plain serialisable value state; off-pitch save/load, replay, and debug-rewind derive from the snapshot model. | MUST | KD-5 |
| FR-LW-023 | The deep tier (memory, text, arcs) runs only for the human manager's active set. The active set = own-club players/staff/board + a bounded per-manager set of external contacts. | MUST | KD-7 |
| FR-LW-024 | Off-active-set entities run the abstracted background tier: cheap, deterministic, summary state only, no per-edge memory or arcs. It obeys the same RNG/iteration determinism rules and a bounded per-tick cost. | MUST | KD-7 |
| FR-LW-025 | A contact leaving the active set is compressed to a cold-stored summary (not hard-evicted); on re-entry the summary rehydrates into live edges/episodes. Tier promotion/demotion is deterministic and neither loses nor duplicates state. | MUST | KD-7 / F5 |
| FR-LW-026 | One `[GT]` save-size budget caps total live state — live edges + live episodes + cold summaries — together; eviction is governed by §3.2. | MUST | §3.2 / §4.5 |
| FR-LW-027 | The world loop only reads canonical human-systems state and match-outcome events; it never writes back into the H-Gate, the vol-2 §2.1 social-graph edge (`PlayerEdge` is read-only), or vol-2 §2.2 propagation math. | MUST | KD-9 |
| FR-LW-028 | Every ordinal-stable enum (`EventKind`, `ArcKind`, `InteractionIntent`, `RelationshipLayer`), the per-kind `Arc.State` byte values, and stable IDs (`episodeId`, `managerChoiceId`, arc reference IDs) are APPEND-only and carry a stability test. | MUST | #16 §6.2 |
| FR-LW-029 | Every constant carries exactly one tag (`[GT]`/`[FIXED]`/`[DERIVED]`/`[CROSS]`); no `[EST]` remains at `APPROVED`; all live in one catalogue `LivingWorldConstants.cs`. | MUST | CLAUDE.md / #20 |
| FR-LW-030 | The system is validated by the §6 automated harnesses (invariant fuzzing, soak, coverage/gap, determinism replay) on the #19 ScenarioRunner; structural conformance is machine-checked, quality is human-reviewed. | MUST | §5 / §6 |
| FR-LW-031 | No interface or accessor is produced against an unspecified consumer (no phantom interfaces). | MUST | CLAUDE.md / #20 FR-CS-048 |
| FR-LW-032 | Stage-1 activation is gated on KD-10 prerequisites (world store + season loop; vol-2/vol-3 impl.; `[GT]` config-loader; structured match-outcome events). | MUST | KD-10 / §7 |
| FR-LW-033 | Every §3 mapping/formula includes units, valid input ranges, and at least one worked example (inline or Appendix A). | MUST | CLAUDE.md |
| FR-LW-034 | **Additive-only identity:** a world with no recorded episodes and no spawned arcs produces the canonical human-systems behaviour exactly — this layer only *adds* on top and never alters a baseline outcome. Identity is asserted on the **human-systems/world-state subset** digest, NOT the full snapshot payload (which necessarily differs once FR-LW-022/§4.6 adds the living-world block + `SNAPSHOT_SCHEMA_VERSION` bump — cf. #21 DET-002). The §3.1 update/decay rule reduces to a no-op when no event/decay applies. | MUST | KD-1 / KD-9 / §2.3 |

## 2.2 Data structures

All are Stage-1 value types per #20 §4.2. Field order is the canonical snapshot order (Appendix B) once
FR-LW-022 activates serialisation into the world store.

### 2.2.1 `RelationshipEdge` (per ordered node pair)

| Field | Type | Notes |
|---|---|---|
| FromId / ToId | `EntityId` | directed; symmetric relations = two equal directed edges |
| ActiveLayers | `byte` | bitmask of which layers are meaningful for this pairing (FR-LW-005) |
| PlayerEdge | `float` 0.0–1.0 | **read-only projection** of vol-2 §2.1's authoritative edge — mirrored for arc reads, **never mutated here** (vol-2 owns its evolution) |
| Affinity | `float` 0.0–1.0 | manager↔non-player personal relationship (KD-3) |
| Trust | `float` 0.0–1.0 | directional; will `ToId` act on `FromId`'s word |
| Memory | `MemoryEpisode[]` | bounded ring buffer (FR-LW-008) |

**Layer applicability by node-type (FR-LW-005).** Not every layer is active on every edge. `PlayerEdge`
is valid **only** on player↔player pairs (it owns the vol-2 clique math); `Affinity` is valid **only**
on manager↔non-player pairs (journalist/board/staff); `Trust` is valid on manager↔contact pairs. The
`ActiveLayers` bitmask records which layers are meaningful (its **bit positions = `RelationshipLayer`
ordinals**, FR-LW-028); **inactive layers hold a defined `0.0`** and are excluded from updates and from
the F6 invariant **by mask** (not by a NaN sentinel — NaN would break the F5 bitwise round-trip and the
snapshot digest). `PlayerEdge` is a **read-only mirror** of vol-2's authoritative edge even when active —
this layer's update (§3.1) writes only its **owned** layers (`Affinity`, `Trust`). The active-layer
matrix per node-type pairing is pinned in Appendix C.

### 2.2.2 `MemoryEpisode`

| Field | Type | Notes |
|---|---|---|
| EpisodeId | `uint` | stable per-edge handle, monotonic (FR-LW-009) |
| Kind | `EventKind` | ordinal-stable enum |
| Salience | `float` 0.0–1.0 | decays per `[GT]` rate (FR-LW-010) |
| WorldTick | `uint` | calendar day of the episode (FR-LW-019) |
| ManagerChoiceId | `ushort` | which manager response produced it (stable ID) |

### 2.2.3 `SpawnCause` (provenance — KD-8)

| Field | Type | Notes |
|---|---|---|
| TriggerId | `ushort` | which rule/threshold fired |
| Inputs | `(short key, float value)[]` | the input values at spawn |
| SnapshotRef | `ulong` | world-state snapshot reference |
| WorldTick | `uint` | calendar day |

### 2.2.4 `Arc`

| Field | Type | Notes |
|---|---|---|
| Kind | `ArcKind` | ordinal-stable |
| State | `byte` | arc-specific lifecycle state (APPEND-only per kind) |
| Cause | `SpawnCause` | provenance (FR-LW-016) |
| PinnedEpisodes | `(EntityId edge, uint episodeId)[]` | non-evictable until resolved (FR-LW-018) |
| SpawnTick / MaxLifetimeTick | `uint` | liveness bound (FR-LW-014 / §6 soak) |

### 2.2.5 `ColdSummary` (departed contact)

| Field | Type | Notes |
|---|---|---|
| EntityId | `EntityId` | the cold-stored contact |
| ActiveLayers | `byte` | retained so rehydration (§3.5) can reconstruct which layers to populate |
| NetRelationship | `float` 0.0–1.0 | compressed standing |
| RetainedEpisodes | `MemoryEpisode[]` | top-N by salience (full schema deferred, §7 residue A) |

### 2.2.6 Enums (all `byte`-backed, APPEND-only, ordinal-stable — FR-LW-028)

`EventKind`, `ArcKind`, `InteractionIntent`, `RelationshipLayer { PlayerEdge, Affinity, Trust }`.

`EventKind` and `InteractionIntent` are **open rosters** finalised at implementation (Appendix C); the
FR-LW-028 stability test **grows with the roster** — it locks the ordinal of every member that exists
and new members are APPEND-only, so deferred membership does not weaken the contract for existing
members.

## 2.3 Identity / neutral state

A fresh `RelationshipEdge` initialises every layer to the vol-2 baseline (strangers = 0.0 for new
contacts; existing canonical relationship strength for known players) with an empty memory buffer and no
arcs. A world with no recorded episodes and no spawned arcs reproduces the canonical human-systems
behaviour exactly (this layer only *adds* on top) — the normative additive-only identity contract
(**FR-LW-034**), asserted on the human-systems/world-state **subset** digest (not the full snapshot
payload, which gains the living-world block per §4.6) and verified by T-LW-DET-007.

## 2.4 Failure modes

| ID | Failure | Detection | Recovery | Test |
|---|---|---|---|---|
| F1 | Dangling `episodeId` — arc references an evicted episode | invariant fuzzer (§6.1) | episode pinning (FR-LW-018) prevents; rehydrate or resolve-on-missing as fallback | T-LW-FAIL-001 |
| F2 | Non-deterministic graph iteration | determinism replay (§6.4) divergence | canonical entity-ID / `ArcKind` ordering (FR-LW-021/017) | T-LW-FAIL-002 |
| F3 | Runtime model inference on a saved-state path | static gate / code review; FR-LW-012 | forbidden by construction; offline authoring only | T-LW-FAIL-003 |
| F4 | Save-size budget overflow | budget check each tick (FR-LW-026) | salience eviction (arc-pinned excepted) | T-LW-FAIL-004 |
| F5 | Tier transition loses/duplicates state on cold-store↔rehydrate | round-trip equality check | deterministic, lossless transition contract (FR-LW-025) | T-LW-FAIL-005 |
| F6 | An **active** layer value (per `ActiveLayers`) escapes [0.0, 1.0] | invariant fuzzer (§6.1) | clamp + author-error log; inactive layers are not checked | T-LW-FAIL-006 |
