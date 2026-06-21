# Living World System Specification #22 — Section 3: Algorithms

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.2 — PASS-1 fix pass: background tier reframed as reflect/summarise,
not authority, for off-screen outcomes (M-4); §3.1 update rule bound to FR-LW-005/034 + active-layer
gating (L-3))
**Version:** 0.2
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
layer value `x` toward its event target, clamped (applied only to layers active for the edge's
node-type, §2.2.1):

```
x' = clamp01( x + v · δ · (1 − x)   if δ ≥ 0
              x + v · δ · x          if δ < 0 )
```

(asymptotic toward the [0,1] bounds — never overshoots). **Decay** toward a per-entity baseline `b` at
`[GT]` rate `r` per `worldTick`: `x' = x + r·(b − x)`.

**Worked example.** `Trust = 0.50`, a public manager defence `δ = +0.4`, `v = 0.3`:
`x' = 0.50 + 0.3·0.4·(1−0.50) = 0.50 + 0.06 = 0.56`. Over 30 idle days at `r = 0.01`, `b = 0.5`:
relaxes ~0.018 back toward baseline.

## 3.2 Episodic memory (KD-1 gap)

Each significant edge holds a bounded ring buffer of `MemoryEpisode` (depth `[GT]` 8–16). On a new
event:

1. Construct `episode = (episodeId = nextId(edge), kind, salience0, worldTick, managerChoiceId)`.
2. Append; if the buffer is full, evict the **lowest-salience** episode **that is not arc-pinned**
   (FR-LW-010/018). If all are pinned, grow transiently and flag against the §4.5 budget.
3. Each `worldTick`, decay every episode's salience: `s' = s · (1 − decayRate)` (`[GT]`).

`episodeId` is monotonic per edge and is the durable handle an arc pins and that survives save/load.

**Referencing.** When an interaction is generated, episodes with `salience ≥ refThreshold` `[GT]` are
eligible to be cited in surface text (§3.3) — this is what makes the context a line is built on never
identical twice.

**Worked example.** Buffer depth 8, full, salience floor episode `e3 (s=0.05)` unpinned → evicted when a
new `s=0.6` episode arrives; a pinned `e1 (s=0.02)` is skipped and the next-lowest unpinned is chosen.

## 3.3 Procedural text: `InteractionIntent` ≠ surface (KD-6)

**Intent** is selected from graph/event state (e.g. *media wants to provoke on title-pressure* — a
vol-2 §7.1 Press-Conference-Trap class). **Surface text** is deterministic template/grammar expansion:

```
template = SelectTemplate(intent, rng.DrawReserved(worldStream))   // deterministic
text     = Expand(template, slots)                                  // slots from real facts + §3.2 memory
```

`SelectTemplate` indexes the static authored corpus; `rng` is the dedicated world stream (FR-LW-020).
**Slots** are filled only from facts the match engine emits (opponent, scoreline, the chance a player
missed) and from a referenced episode (§3.2) — **no assumed derived stats** (FR-LW-013). **No model
inference runs here** (FR-LW-012); the corpus is authored offline (§7, KD-6).

**Determinism.** Same `(intent, world stream cursor, slots)` ⇒ identical string. Verified by T-LW-DET-*.

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
update of summary state only — no per-edge memory, no arcs — on the same RNG stream and a bounded
per-tick cost (FR-LW-024). The background tier **reflects/summarises** outcomes produced by the
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

`SpawnCause` is captured inline at every arc/interaction creation (§3.4 step 1) and is never
reconstructed after the fact. It is the data source for the §7 inspector's time-scrub/replay-step and
"why did this arc fire?" causal tracing. Because it references a snapshot, the full causal state is
recoverable deterministically from the world store.
