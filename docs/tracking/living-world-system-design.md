# Living World System — Design Supplement

> **Created:** June 21, 2026
> **Last Updated:** June 21, 2026 (v0.6 — PASS-4 adversarial fix pass on the v0.5 additions, L-only:
> §6 CI-gate claim softened to forward-looking "intended to run" (L1); coverage/gap detection gains an
> expected-rarity annotation so designed scarcity isn't flagged as a gap (L2); soak liveness reframed
> as a checkable per-instance max-arc-lifetime bound rather than an unprovable global "every arc
> resolves" (L3); save-size budget unified to one canonical scope — live edges + live episodes + cold
> summaries (L4); background-tier "rivalries forming" reworded to "form swings" to not imply an arc, and
> deterministic tier promotion/demotion pinned (L5); determinism-replay caveated to single-machine
> snapshot determinism. No High/Medium findings; convergence terminal.)
> **Last Updated (prior):** June 21, 2026 (v0.5 — folded in two resolved review decisions: new §6 **Verification
> & stress testing** (four automated script classes on the #19 ScenarioRunner + `tools/spec-stress/` —
> invariant fuzzing, long-horizon soak, coverage/gap detection, determinism replay — resolving §7 risk 2
> on the structural side); §4 reworked into a **two-tier LOD** model with an abstracted, deterministic
> **background simulation** for off-active-set entities plus **cold-store + rehydration** of departed
> contacts (resolving §7 risk 6); old §6 Risks renumbered §7 with risks 2 and 6 marked RESOLVED.)
> **Last Updated (prior):** June 21, 2026 (v0.4 — PASS-3 full-document adversarial fix pass: added a stable
> `episodeId` handle to the §3.1 episode tuple — the arc-pinning mechanism (M4) referenced "episode
> IDs" the data model never defined (M1); pinned a fixed `ArcKind`-ordinal evaluation order for
> non-entity-scoped (squad/board) arcs so spawn/RNG/pinning stay deterministic (L1); corrected
> "relationship-layer enums" to the layer-identifier enum, since `Affinity`/`Trust` are float scalars
> (L2); clarified `Affinity` is the manager's personal relationship, not a second board/supporter
> authority (L3). No High findings; convergence reached.)
> **Last Updated (prior):** June 21, 2026 (v0.3 — PASS-2 full-document adversarial fix pass: top-layer
> off-pitch "world" assembly placement declared (M1); a deterministic season-calendar clock defined
> and `worldTick` pinned to the calendar day to align with vol-2 §2.2 day-based latencies (M2);
> AI-managed clubs scoped to an abstracted memory/arc-free model — deep sim is the human active set
> only (M3); arc-vs-episode lifecycle pinned so arcs snapshot facts at spawn and cannot dangle on an
> evicted episode (M4); interaction-intent enum renamed `InteractionIntent` to avoid the existing
> `Intent`/`AttackIntent`/`DistributeIntent` collisions in `src` (L1); `[GT]` xG slot example softened
> to neutral match facts (L2); `managerChoiceId` added to the ordinal/stable-ID contract (L3). All
> seam claims fact-checked against source.)
> **Last Updated (prior):** June 21, 2026 (v0.2 — PASS-1 adversarial fix pass: reframed as a supplement
> layered on `master-vol-2-human-systems.md` / `master-vol-3-club-operations.md` rather than a
> greenfield proposal (H1); edge model reconciled to vol-2 §2.1's 0.0–1.0 scale with the extra axes
> demoted to an explicit additive extension (H2); the contagion section replaced by a consume-as-is
> reference to vol-2 §2.2 Pulse Propagation (H3); RNG seam corrected to `DeterministicRngService`
> (M1); deterministic text-generation constraint pinned (M2); deterministic graph-iteration ordering
> mandated (M3); node-population boundary + save budget defined (M4); board/fan routing into canon
> (L3/L4); minor trims (L1/L2). All seam claims fact-checked against source.)
> **Last Updated (prior):** June 21, 2026 (v0.1 — initial draft)
>
> **Status:** DESIGN SUPPLEMENT (forward-looking; **NOT** a formal approved spec, **NOT** yet
> implemented). Targets **Stage 1+** (manager-facing meta layer), sequenced after the match engine
> and the tactical-instruction layer (#21) land. No code is authored from this note until it is
> reviewed and, if promoted, written up as a formal spec with FR-IDs, a §5 test plan, and a §9
> approval checklist.
> **Author:** —
> **Purpose:** Propose the **immersion layer** that sits on top of the already-canonical human-systems
> model (`master-vol-2-human-systems.md`) to attack the central immersion failure of management games:
> *every off-pitch interaction (media, players, staff, board, fans) starts repeating itself after a few
> seasons.* The social graph, happiness gate, pulse propagation, and media systems **already exist in
> canon** — this note does not redesign them. It adds the three constructs canon lacks that actually
> defeat repetition: **per-edge episodic memory**, **intent≠surface procedural text**, and **emergent
> narrative arcs** — and pins the determinism/authoring constraints any implementation must honour.

---

## 0. Scope and governance

This supplement covers the **off-pitch interaction-generation layer**: how the existing human-systems
state (happiness, the social graph, board/media relationships) is turned into *non-repeating*
interactions — press conferences, player/staff conversations, contract talks, and the narrative arcs
that emerge from them. It does **not** redesign the human-systems model and it does **not** touch the
match engine, the physics/AI subsystems (#1–#15), or the tactical-instruction layer (#21); it consumes
their outputs and the canonical social state as inputs and produces *interaction content* + arc state
as output.

**This note introduces no new mechanics that canon already owns.** The following are **consume-as-is**:

| Concern | Canonical owner (authoritative) | This note's relationship |
|---|---|---|
| Happiness / psychological state | vol-2 §1.1 **H-Gate** (`H = f(Narrative_Stress, Mental_Noise, Acoustic_Distortion, Social_Support)`; Confidence vs. Self-Efficacy split) | consume as input |
| Social graph (nodes=players, edges, cliques) | vol-2 §2.1 **Structure** (edge weight 0.0–1.0; cliques at mutual >0.6) | consume; extend per §3 |
| Morale spread across the graph | vol-2 §2.2 **Pulse Propagation** (intra-clique 90–100%/instant; inter-clique 40–60% via bridge, 1–2 day latency) | consume as-is — **not** re-derived here |
| Isolation / ego-clash / hazing | vol-2 §2.3 / §2.4 / §2.5 | consume as input |
| Media & narrative events | vol-2 §7 (Press Conference Traps, Transfer Rumor Mill, Commentator Bias, New-Manager Bounce) | consume; arcs in §3.3 sit on top |
| Board / governance | vol-3 §4 (Board Archetypes, Takeover Cycles, DoF Veto) | consume; board arcs route here |
| Supporters / crowd | vol-2 §4.1 Supporter Trust, §5.1 Crowd Dynamics | consume; fan node is an aggregate view of these |

**What this note actually adds** (the gap canon leaves): §3.1 per-edge **episodic memory**, §3.2
**procedural interaction text** (intent vs. surface), §3.3 **emergent arc** state machines.

This is governance scaffolding parallel to `tactical-instruction-layer-design.md`; it is reviewed as a
design note, not a spec, and is **not** spec-template-complete.

**Hard prerequisites (this layer cannot be runtime-driven until these land):**

1. **A persistent world-state store and a season/calendar loop.** The living world is a *slow loop*
   (event- and day-driven), entirely separate from the 10 Hz tactical and 60 Hz physics match loops.
   No such loop exists yet.
2. **The human-systems model from vol-2/vol-3 implemented** (the H-Gate, social graph, pulse
   propagation). This layer reads that state; it cannot run before it exists.
3. **The `[GT]` config-loader mechanism** (`src/CLAUDE.md` "WHAT IS NOT HERE YET"): every memory
   salience weight, arc threshold, and decay rate added below is a `[GT]` tunable with nowhere to be
   injected until the loader exists.
4. **The match engine producing structured outcome events** (result, performance, injuries, cards) for
   the world loop to consume.

Read `CLAUDE.md`, `src/CLAUDE.md`, `master-vol-2-human-systems.md`, and `master-vol-3-club-operations.md`
first. Deterministic time only (no `DateTime.Now`), the injected `DeterministicRngService` only (no
`System.Random`), state-snapshot determinism, ordinal stability on every new enum. **All new state is
serialised**, which is what makes save/load, replay, and debug-rewind work off-pitch as they do in-match.

---

## 1. The problem, stated precisely

The repetition players perceive is **not** a content-volume problem — more canned lines only delay
onset. It is a **statelessness problem**. In current management games an interaction is selected from a
pool keyed on a shallow trigger (won/lost/big-result), so the same trigger yields the same line
regardless of relationship history; NPCs have no memory; nothing accumulates into a story.

Canon (vol-2) already gives us the *state* to fix this — a happiness gate and a weighted social graph
that evolve continuously. What canon does **not** specify is how that state is turned into *content*
without repeating. Three constructs close that gap, and they compose:

1. **Episodic memory** — so an interaction can reference its own history (§3.1).
2. **Procedural text** — so identical `InteractionIntent` never emits identical strings (§3.2).
3. **Emergent arcs** — so events accumulate into seasons-long stories (§3.3).

---

## 2. Reconciliation with the canonical social graph

The canonical edge model (vol-2 §2.1) is a **single relationship-strength scalar, 0.0–1.0** (strangers
→ best friends), with the clique-formation rule "cliques form when 3+ players have mutual edge weights
> 0.6" and the pulse-propagation retention percentages (§2.2) keyed to that same scale. **This note
adopts that model unchanged** — the earlier v0.1 signed −1..+1 multi-axis edge is withdrawn because it
would have silently broken the clique threshold and the propagation math.

**Additive extension (optional, non-breaking).** Where richer manager↔entity relationships are needed
(journalists, board members, staff — entities vol-2's *player* graph does not enumerate), this note
adds **parallel relationship layers on the same 0.0–1.0 scale**, never a re-scaling of the player edge:

| Layer | Applies to | Notes |
|---|---|---|
| `Affinity` (0.0–1.0) | manager ↔ {journalist, board member, staff} | the vol-2 player-edge analogue for non-player nodes |
| `Trust` (0.0–1.0) | directional: A→B ≠ B→A | will B act on A's word; **asymmetric, stored per direction** |

These are *additional* layers, not a replacement; the vol-2 player edge and its 0.6 clique threshold
remain authoritative and untouched. The graph is **directed**: every layer is stored per ordered pair,
and symmetric relationships are simply two equal directed edges (no shared/bidirectional storage).
`Affinity` is the manager's *personal* relationship with an individual (a board member, a journalist) —
it is **not** a second board-confidence or supporter-trust authority; collective board sentiment stays
owned by vol-3 §4 and supporter trust by vol-2 §4.1 (§7 risk 5).

Morale spread across this graph is **vol-2 §2.2 Pulse Propagation, consumed as-is** — this note does
**not** define its own contagion rule. Faction *outcomes* (a dressing-room split) are modelled here only
as an **arc** (§3.3) that *reads* the propagation result; the propagation itself stays in canon.

---

## 3. What this note adds (the concrete gap)

### 3.1 Per-edge episodic memory

Give each significant edge a **bounded ring buffer** of episodes:
`(episodeId, eventKind, salience, worldTick, managerChoiceId)`. When an interaction is generated,
recent high-salience episodes become eligible to be *referenced* ("after what you said about me in
March…"). Memory is what converts the graph from "current mood" into "a history," and it is the single
biggest lever against the repetition feel because the *context* a line is built on is never identical
twice.

- `episodeId` is a stable per-edge handle (monotonic within the edge); it is what an arc (§3.3) pins
  and what survives save/load — the tuple's other fields are content, not identity.
- Buffer depth is a small `[GT]` constant (target 8–16) **per significant edge only** — not every edge
  (see §4 node boundary), to bound save growth.
- `worldTick` is the deterministic season-calendar day (§4 clock), the same time base vol-2 §2.2 uses
  for its day-scaled propagation latencies — not a match tick.
- Salience and decay-of-salience are `[GT]`. Low-salience episodes age out first, **except** episodes
  pinned by a live arc (§3.3).
- Episodes are plain serialised structs; `eventKind` is an ordinal-stable enum and `managerChoiceId`
  is a stable persisted ID (§4 ordinal-stability contract).

### 3.2 Procedural interaction text (intent ≠ surface)

Separate **`InteractionIntent`** (graph-/event-driven: *media wants to provoke on title-pressure*, a
vol-2 §7.1 Press-Conference-Trap class) from **surface text** (slot-filled expansion from real match
facts — opponent, scoreline, the chance the player missed, the player referenced from §3.1 memory).
One intent → effectively unbounded phrasings, all anchored to true facts. Without this layer the graph
still feels repetitive, because identical intents emit identical strings. (`InteractionIntent` is named
to avoid the existing `Intent` / `AttackIntent` / `DistributeIntent` enums in the gameplay assemblies;
slot facts are limited to data the match engine actually emits — no assumed derived stats.)

> **Determinism constraint (load-bearing).** Surface text is generated by **deterministic template /
> grammar expansion** with template selection drawn from the injected `DeterministicRngService` (§4).
> **No generative-model / LLM inference is permitted on any path whose output is persisted in saved
> state** — that would be non-reproducible and would break snapshot/replay parity, which is a hard
> project requirement. If model-assisted text is ever explored, it is strictly an offline *authoring*
> tool that produces the static template corpus, never a runtime call.

### 3.3 Emergent narrative arcs

When canonical state crosses a threshold, **spawn an arc** with its own lifecycle state machine that
lives across multiple world-ticks, biases which interactions fire while active, and resolves or
escalates on subsequent results/choices. Arcs are what the player remembers as "that season when…".
Examples and their canonical triggers:

| Arc | Spawn trigger (reads canon) | Routes into |
|---|---|---|
| `DressingRoomSplit` | vol-2 §2.2 pulse divergence across cliques + §2.4 ego clash | squad happiness (vol-2 §1.1) |
| `MediaVendetta` | repeated low-`Affinity` journalist episodes (§3.1) after §7.1 traps | vol-2 §7 media events |
| `BoardPatienceCollapse` | results vs. the **vol-3 §4.1 archetype** patience profile (Tycoon 3–5 bad results; Sustainable trend-tolerant) | vol-3 §4 governance (sack/backing), §4.2 takeover |
| `WonderkidVsVeteran` | vol-2 §2.4 ego-clash trigger on overlapping high-reputation pair | squad + minutes |

Board arcs **do not** invent a board node — they read and route into vol-3 §4 (Board Archetypes,
Takeover Cycles, DoF Veto). The fan node is an **aggregate view** over vol-2 §4.1 Supporter Trust /
§5.1 Crowd Dynamics, not a new authority.

Arcs are small serialised state machines; `ArcKind` and arc-state enums are ordinal-stable. An arc
**snapshots the facts it needs at spawn** (the episode IDs and slot values it will reference), and those
source episodes are **pinned non-evictable** until the arc resolves — so the §4 salience eviction can
never leave a live arc referencing a dropped episode.

---

## 4. Determinism & architecture constraints (non-negotiable)

- **Layer placement.** This is a **top-layer off-pitch "world" assembly**. It references the
  human-systems/data assemblies (vol-2/vol-3 model) and `project-constants` **downward only**, and is
  referenced by **nothing** in the match hot path (`Physics ← Mechanics ← AI` graph). The match engine
  must never call into it; it only consumes the engine's outcome events. This keeps the reference graph
  acyclic and the 10/60 Hz hot path free of off-pitch code (parallels #21's `tactics/` placement rule).
- **Separate slow loop on a dedicated calendar clock.** The world graph ticks on a **deterministic
  season-calendar clock** (own loop, distinct from `src/deterministic-sim/MatchClock.cs`, which is match
  time only — no calendar clock exists yet, so this layer must introduce one). The loop is event- and
  day-driven; one `worldTick` = one calendar day, the same unit vol-2 §2.2 propagation latencies are
  expressed in. It runs **never** inside the 10 Hz / 60 Hz match loops — tag it to avoid the
  loop-conflation hazard in `CLAUDE.md`.
- **RNG seam.** All stochastic selection (template choice, salience tie-breaks, any nudge) draws from
  the injected **`DeterministicRngService`** via its `Reserve`/`DrawReserved`/`Skip` reservation API
  (`src/deterministic-sim/DeterministicRngService.cs`), on a **dedicated world-loop stream** — never
  `System.Random`, never wall-clock. (SplitMix64 is only that service's construction-time match-seed
  PRNG; the per-draw primitive is HKDF-SHA256 + SipHash-2-4-64 — do not re-implement it here.)
- **Deterministic graph iteration.** Every pass that walks the graph (memory aging, arc evaluation, and
  any read of vol-2 propagation results) MUST iterate nodes/edges in a **canonical order keyed on a
  stable entity ID**, not on dictionary/hashset enumeration order (which is not a stable contract in
  C#). Order-dependent results without a pinned ordering are a determinism defect — the same class as
  the EventRegistry static-init-order finding. **Arcs that are not entity-scoped** (squad- or
  board-level, e.g. `DressingRoomSplit` / `BoardPatienceCollapse`) are evaluated in a fixed `ArcKind`
  ordinal order so spawn order, RNG draw order, and episode-pinning are deterministic regardless of
  which entity triggered them.
- **State, not scripts.** Graph extensions, memory buffers, and arc state machines are plain
  serialisable structs; off-pitch save/load, replay, and debug-rewind fall out of the same
  state-snapshot model the match uses.
- **Node-population boundary (feasibility-critical).** A full graph over a multi-league world is O(N²)
  and infeasible. The **active set** is bounded: nodes are *own-club* players/staff/board + a small
  per-manager set of external contacts (journalists, rival managers, agents the manager has interacted
  with). Edges and §3.1 memory buffers exist **only within the active set**; world entities outside it
  carry no per-edge memory until they enter it. The `[GT]` **save-size budget** is canonical here and
  caps the three live-state classes together — **live edges + live episodes + cold summaries**; oldest
  low-salience episodes evict first (arc-pinned episodes excepted, §3.3).
- **Two-tier level-of-detail (LOD).** The world runs at two fidelities. The **deep tier** (per-edge
  memory, procedural text, arcs) runs **only for the human manager's active set**. Every other entity —
  AI-managed clubs, out-of-set contacts — runs an **abstracted background tier**: a cheap, deterministic
  simulation of living-world events between off-set entities (transfers, sackings, form swings) held as
  **summary state only — no per-edge memory or arcs**. The background tier obeys the same determinism
  rules (seeded `DeterministicRngService` stream, bounded per-tick cost) so the wider world stays
  reproducible. **Tier promotion/demotion is deterministic** — an entity crossing the active-set boundary
  transitions state at a defined point, never losing or duplicating it.
- **Cold-store + rehydration.** When a contact leaves the active set, its memory is **compressed to a
  cold-stored summary** (not hard-evicted) so the "journalist who's had it in for you across three clubs"
  fantasy survives; when that contact re-enters the active set, the summary **rehydrates** into live
  edges/episodes. Cold summaries count against the save-size budget above.
- **Zero magic numbers.** Every salience weight, decay rate, arc threshold, and buffer depth is a `[GT]`
  constant in a designated catalogue, validated by a balance pass (precedent: #21 G2 balance pass, #8
  draft-level approval).
- **Ordinal stability.** `eventKind`, `ArcKind`, `InteractionIntent`, and the **relationship-layer
  identifier enum** (which layer — `Affinity`/`Trust`/… — a stored 0.0–1.0 value belongs to) are
  embedded in saved state — reordering breaks save compatibility. They carry the ordinal-stability
  contract from day one; `episodeId`, `managerChoiceId`, and arc reference IDs are likewise stable
  persisted IDs.

---

## 5. Sequencing

**Stage 1+**, after: the vol-2/vol-3 human-systems model is implemented; the match engine produces
structured outcome events; the `[GT]` config-loader exists; and ideally after #21 so manager *tactical*
and manager *social* intent share the same config and meta-loop plumbing. It does **not** block, and is
not blocked by, any Stage-0 physics/AI spec. Promotion path mirrors #21: this note → adversarial passes
→ formal spec folder with FR-IDs, §5 test plan, §9 checklist.

Recommended phasing (all on the same state, no rework between phases):

| Phase | Adds | Payoff |
|---|---|---|
| A | §3.1 memory + §3.2 procedural text (on the existing vol-2 graph) | interactions stop repeating; history is felt |
| B | §3.3 arcs | events accumulate into seasons-long storylines |

Phase A is the load-bearing deliverable; B layers on with no rework. (No "contagion" phase — that is
vol-2 §2.2, already canonical.)

---

## 6. Verification & stress testing

Beyond the inspector view (§7 risk 2), the system is validated by automated harnesses on the existing
`#19 ScenarioRunner` + `tools/spec-stress/` infrastructure. Four script classes verify **structure**
(no gaps, no deadlocks, determinism) — they do **not** verify *quality* (tone/believability still needs
the inspector view + human review):

1. **Invariant / property fuzzing.** Random seeds drive the world loop; after every tick assert the
   never-violated rules: all edge/layer values stay in [0.0, 1.0]; no dangling `episodeId`; no orphan or
   unresolvable arc; live edges + cold summaries within the `[GT]` save-size budget; rehydration of a
   cold summary reproduces a valid edge.
2. **Long-horizon soak.** Run N seasons headless and assert no deadlock and no runaway/monotonic state
   drift; the checkable liveness rule is **per-instance**: no arc instance stays unresolved beyond a
   `[GT]` maximum lifetime (a finite soak cannot prove global "every arc resolves," but it can bound
   every instance it spawns).
3. **Coverage / gap detection.** Track which `InteractionIntent` / `ArcKind` actually fire across a large
   seeded corpus; **unreached content is the "gap"** — surfaced automatically rather than found in play.
   Content that is *intentionally* rare (e.g. takeovers) carries an expected-rarity annotation so the
   harness flags genuine unreachability, not designed scarcity.
4. **Determinism replay.** Same seed + snapshot-restore (deep tier *and* background tier) must produce
   bit-identical world state on the pinned host — the off-pitch analogue of the match determinism gate
   (single-machine snapshot determinism per `CLAUDE.md`; cross-platform parity stays Stage 5+).

These are **intended to run** in the non-certifying CI gate (`tools/dotnet-ci/`) alongside the existing
suites once the system is implemented; any invariant breach or determinism mismatch would fail the build.

---

## 7. Risks / open questions for review

1. **Authoring cost is the real risk.** The §3.2 template/grammar corpus and §3.3 arc library need
   sizeable, curated content plus tuning guardrails or they emit nonsense. Budget a content-authoring +
   balance pass comparable to a spec's `[GT]` validation — larger than the engineering.
2. **Tuning legibility vs. emergence — RESOLVED (direction set).** Addressed by a debug/inspector view of
   the graph + memory + arc state (also the replay-verification surface) **plus** the §6 automated
   stress/coverage harnesses. The inspector covers *quality* tuning; the harnesses cover *structural*
   gaps and determinism. Open residue: scope/effort of the inspector tooling.
3. **Combinatorial test surface.** "Interaction depends on full graph state" resists unit testing — see
   §6; verification is scenario- and property-based (envelope assertions), not exact-string.
4. **Localisation.** Procedural template expansion interacts badly with grammatical-gender/inflection
   languages; if multi-language is a goal the grammar must be designed for it up front, not retrofitted.
5. **Boundary discipline.** Confirm the world loop only ever *reads* match-outcome events and canonical
   human-systems state, and never writes back into vol-2's H-Gate / propagation math (it adds layers and
   arcs on top; it must not become a second authority over morale).
6. **Active-set churn — RESOLVED (direction set).** Contacts leaving the active set are **cold-stored as
   compressed summaries** (not hard-evicted) and **rehydrate** on re-entry; off-set entities run the
   **abstracted background tier** (§4 LOD). Open residue: summary compression schema + the `[GT]` budget
   split across live edges, live episodes, and cold summaries.
