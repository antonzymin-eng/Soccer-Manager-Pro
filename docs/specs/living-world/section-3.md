# Living World System Specification #22 — Section 3: Algorithms

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.6 — PASS-5 fix pass: §3.6 scopes persisted `SpawnCause` to arcs,
interaction provenance implicit (AR5-M1); §3.1 decay worked example corrected to the geometric ~0.016 (AR5-L1))
**Last Updated (prior):** June 21, 2026 (v0.5 — PASS-4 fix pass: §3.2 worked-example depth marked illustrative vs the catalogue default (AR4-L3))
**Version:** 0.7
**Status:** IN REVIEW (June 21, 2026)

> All formulas state units and input ranges and carry a worked example (FR-LW-033). Constants reference
> Appendix A. `[GT]` magnitudes here are **illustrative pending the §7 balance pass** — the spec's
> contract is the shapes/directions (precedent: #21 G2, #8 draft-level approval).

---

## 3.1 Edge-model extension (KD-3)

The canonical player edge `PlayerEdge ∈ [0,1]` and its clique rule (vol-2 §2.1: cliques form at mutual
> 0.6) are **unchanged**. This layer adds two parallel layers on the same scale, stored per ordered
pair:

- `Affinity ∈ [0,1]` — manager↔non-player personal relationship.
- `Trust ∈ [0,1]` — directional; `Trust(A→B)` = will B act on A's word.

**Event update (FR-LW-005; reduces to a no-op under FR-LW-034 when no event/decay applies).** An
off-pitch event with signed impact `δ ∈ [−1,1]` and a `[GT]` layer volatility `v ∈ (0,1]` updates a
layer value `x` toward its event target, clamped. It is applied **only to this layer's owned layers
(`Affinity`, `Trust`)** that are active for the edge's node-type (§2.2.1); **`PlayerEdge` is never written
here** — it is a read-only mirror of vol-2's authoritative social-graph edge (FR-LW-004, KD-9):

```
x' = clamp01( x + v · δ · (1 − x)   if δ ≥ 0
              x + v · δ · x          if δ < 0 )
```

(asymptotic toward the [0,1] bounds — never overshoots). **Decay** toward a per-entity baseline `b` at
`[GT]` rate `r` per `worldTick`: `x' = x + r·(b − x)`.

**Worked example.** `Trust = 0.50`, a public manager defence `δ = +0.4`, `v = 0.3`:
`x' = 0.50 + 0.3·0.4·(1−0.50) = 0.50 + 0.06 = 0.56`. Over 30 idle days at `r = 0.01`, `b = 0.5`, the
geometric recurrence gives `x = 0.5 + 0.06·(0.99)^30 ≈ 0.544` — i.e. **~0.016** closed back toward
baseline (the linear estimate `r·n·gap = 0.018` overstates it).

## 3.2 Episodic memory (KD-1 gap)

Each significant edge holds a bounded ring buffer of `MemoryEpisode` (depth `[GT]` 8–16). On a new
event:

1. Construct `episode = (episodeId = nextId(edge), kind, salience0, worldTick, managerChoiceId)`.
2. Append; if the buffer is full, evict the **lowest-salience** episode **that is not arc-pinned**
   (FR-LW-010/018). If all are pinned, the buffer grows transiently — bounded by the count of
   simultaneous arc pins on that edge and capped by the §4.5 budget — and shrinks back to depth as arcs
   resolve and unpin (so FR-LW-008 "bounded" holds in steady state).
3. Each `worldTick`, decay every episode's salience: `s' = s · (1 − decayRate)` (`[GT]`).

`episodeId` is monotonic per edge and is the durable handle an arc pins and that survives save/load.

**Referencing.** When an interaction is generated, episodes with `salience ≥ refThreshold` `[GT]` are
eligible to be cited in surface text (§3.3) — this is what makes the context a line is built on never
identical twice.

**Worked example** (illustrative depth 8; the catalogue default is `MEMORY_BUFFER_DEPTH = 12`). Buffer
full, salience floor episode `e3 (s=0.05)` unpinned → evicted when a new `s=0.6` episode arrives; a
pinned `e1 (s=0.02)` is skipped and the next-lowest unpinned is chosen.

## 3.3 Procedural text: `InteractionIntent` ≠ surface (KD-6)

**Intent** is selected from graph/event state (e.g. *media wants to provoke on title-pressure* — a
vol-2 §7.1 Press-Conference-Trap class). **Surface text** is deterministic template/grammar expansion:

```
template = SelectTemplate(intent, rng.DrawReserved(world.text))   // deterministic, isolated sub-stream
text     = Expand(template, slots)                                 // slots from real facts + §3.2 memory
```

`SelectTemplate` indexes the static authored corpus; `rng` draws from the **`world.text` sub-stream**,
separate from the tick-driven `world.arcs` sub-stream (FR-LW-020) so that **aperiodic, player-triggered**
generation never perturbs the arc/world cursor. **Slots** are filled only from facts the match engine
emits (opponent, scoreline, the chance a player missed) and from a referenced episode (§3.2) — **no
assumed derived stats** (FR-LW-013). **No model inference runs here** (FR-LW-012); the corpus is authored
offline (§7, KD-6).

**Determinism.** Same `(intent, world.text cursor, slots)` ⇒ identical string (verified by T-LW-DET-003);
the `world.text`/`world.arcs` separation makes this hold regardless of when in the calendar the player
triggers generation.

## 3.4 Emergent arcs (KD-1 gap)

**Spawn.** Each `worldTick`, after the canonical human-systems update, evaluate arc triggers. Entity-
scoped triggers are evaluated in canonical entity-ID order; squad/board-level triggers in fixed
`ArcKind` ordinal order (FR-LW-017/021). When a trigger crosses its `[GT]` threshold:

1. Construct `cause = SpawnCause(triggerId, inputs, snapshotRef, worldTick)` (FR-LW-016, KD-8).
2. Snapshot the facts the arc will reference and **pin** its source episodes (FR-LW-018).
3. Instantiate `Arc(kind, state0, cause, pinnedEpisodes, spawnTick, spawnTick + maxLifetime)`.

**Lifecycle.** An arc is a small state machine; while active it biases which `InteractionIntent`s fire,
and it resolves or escalates on subsequent results/manager choices. Every spawnable arc has a `[GT]`
`maxLifetime` so it cannot remain unresolved indefinitely (§6.2 soak bound).

**Canonical triggers / routing.**

| Arc | Spawn trigger (reads canon) | Routes into |
|---|---|---|
| `DressingRoomSplit` | vol-2 §2.2 pulse divergence across cliques + §2.4 ego clash | squad happiness (vol-2 §1.1) |
| `MediaVendetta` | repeated low-`Affinity` journalist episodes (§3.2) after §7.1 traps | vol-2 §7 media events |
| `BoardPatienceCollapse` | results vs. the vol-3 §4.1 archetype patience profile (Tycoon 3–5 bad results; Sustainable trend-tolerant) | vol-3 §4 (sack/backing), §4.2 takeover |
| `WonderkidVsVeteran` | vol-2 §2.4 ego-clash on an overlapping high-reputation pair | squad + minutes |

Arcs **read** canon and route into it; they never become a second authority over morale (FR-LW-027,
KD-9).

## 3.5 Two-tier LOD, cold-store, rehydration (KD-7)

**Deep tier** (active set): full §3.1–§3.4. **Background tier** (everyone else): a cheap deterministic
update of summary state only — no per-edge memory, no arcs — under the same RNG-service determinism rules
(a periodic, tick-driven sub-stream) and a bounded per-tick cost (FR-LW-024). The background tier **reflects/summarises** outcomes produced by the
(abstracted) club-AI and the canonical systems (transfers, sackings, form swings); it is **not** the
authority for those outcomes — transfers/recruitment stay owned by vol-3 §2 and governance by vol-3 §4
(KD-9). It records *that* a club sacked its manager, it does not decide it.

**Demotion (active → background).** Compress the edge's memory to a `ColdSummary`
(`NetRelationship` + top-N salient episodes; schema deferred §7) and drop live buffers.

**Promotion (background → active / rehydration).** Expand the `ColdSummary` back into a live
`RelationshipEdge` (layers from `NetRelationship`, memory from `RetainedEpisodes`).

Both transitions are **deterministic and lossless within the retained-fields contract** — round-trip
through cold-store and back yields an edge equal on all retained fields (F5, T-LW-FAIL-005). The
`[GT]` save-size budget (§4.5) covers live edges + live episodes + cold summaries together.

## 3.6 Provenance and inspection (KD-8)

`SpawnCause` is captured inline at every **arc** creation (§3.4 step 1) and is never reconstructed after
the fact. It is the data source for the §7 inspector's time-scrub/replay-step and "why did this arc
fire?" causal tracing. Because it references a snapshot, the full causal state is recoverable
deterministically from the world store.

**Generated interactions** carry no separate persisted `SpawnCause` (there is no interaction record type
in §2.2). Their provenance is **implicit**: an interaction is a deterministic function of
`(InteractionIntent, RNG cursor, snapshotRef)` (§3.3), so the inspector reconstructs "why this line
surfaced" from the snapshot + RNG cursor. An optional inspector interaction-log may persist that
lightweight `(intent, cursor, snapshotRef)` tuple, but it is not part of the core serialised model
(FR-LW-016).
