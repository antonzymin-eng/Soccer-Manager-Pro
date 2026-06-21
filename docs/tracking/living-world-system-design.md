# Living World System — Design Supplement

> **Created:** June 21, 2026
> **Last Updated:** June 21, 2026 (v0.1 — initial draft)
>
> **Status:** DESIGN SUPPLEMENT (forward-looking; **NOT** a formal approved spec, **NOT** yet
> implemented). Targets **Stage 1+** (manager-facing meta layer), sequenced after the match engine
> and the tactical-instruction layer (#21) land. No code is authored from this note until it is
> reviewed and, if promoted, written up as a formal spec with FR-IDs, a §5 test plan, and a §9
> approval checklist.
> **Author:** —
> **Purpose:** Propose a graph-driven "living world" relationship and narrative system that attacks
> the central immersion failure of management games — *every off-pitch interaction (media, players,
> staff, board, fans) starts repeating itself after a few seasons.* Define the problem precisely,
> enumerate candidate solutions, recommend a composition, and pin the determinism / authoring
> constraints any implementation must honour.

---

## 0. Scope and governance

This supplement covers the **off-pitch interaction layer**: press conferences, player/staff
conversations, board confidence, fan sentiment, contract talks, dressing-room dynamics, and the
narrative arcs that emerge from them. It does **not** touch the match engine, the physics/AI
subsystems (#1–#15), or the tactical-instruction input layer (#21) — it consumes their *outcomes*
(results, minutes played, transfers, tactical changes) as inputs and produces *social state* as
output.

This note introduces **no spec number yet**. It is a candidate for promotion once reviewed. It is
**not** spec-template-complete. It is governance scaffolding parallel to
`tactical-instruction-layer-design.md` and `match-engine-design.md`, and is reviewed as a design
note, not as a spec.

**Hard prerequisites (this layer cannot be runtime-driven until these land):**

1. **A persistent world-state store and a season/calendar loop.** The living world is a *slow loop*
   (event- and day-driven), entirely separate from the 10 Hz tactical and 60 Hz physics match loops.
   No such loop exists yet.
2. **The `[GT]` config-loader mechanism** (`src/CLAUDE.md` "WHAT IS NOT HERE YET"): every edge gain,
   decay rate, and threshold below is a `[GT]` tunable with nowhere to be injected until the loader
   exists.
3. **The match engine producing structured outcome events** (result, scoreline context, individual
   performance, injuries, cards) for the world loop to consume.

Read `CLAUDE.md` and `src/CLAUDE.md` first. Every rule there applies — deterministic time only
(no `DateTime.Now`), SplitMix64 RNG only (no `System.Random`), state-snapshot determinism, and
ordinal stability on every new enum. **The entire world graph is serialised state**, which is what
makes save/load, replay, and debug-rewind work for off-pitch content exactly as they do for the match.

---

## 1. The problem, stated precisely

The repetition players perceive is not a content-volume problem — adding more canned lines only delays
the onset. It is a **statelessness problem**. In current management games an interaction is selected
from a pool keyed on a shallow trigger (won/lost/big-result), so:

- the *same trigger* produces the *same interaction* regardless of relationship history;
- NPCs have **no memory** — a journalist you humiliated last week greets you identically;
- outcomes don't *propagate* — one unhappy player never infects the squad;
- nothing *accumulates into a story* — there are events, but no arcs.

The fix is to make each interaction a **function of persistent, evolving state** rather than a draw
from a pool. The four levers below each attack one of those four failures, and they compose.

---

## 2. Candidate solutions

### 2.1 Multilayer relationship graph (the backbone)

Model the world as a directed, multilayer graph. **Nodes** are entities (each player, staff member,
board member, journalist/outlet, the fanbase as an aggregate node, rival clubs). **Edges** are typed,
weighted relationships carrying continuous state, e.g.:

| Edge axis | Range | Meaning |
|---|---|---|
| `Trust` | −1.0 … +1.0 | will they take you at your word |
| `Respect` | −1.0 … +1.0 | professional standing |
| `Warmth` | −1.0 … +1.0 | personal affinity |
| `Volatility` | 0.0 … 1.0 | how fast this edge moves per event `[GT]` |

"Multilayer" = the same node pair can carry several edge types on parallel layers (manager↔player has
a professional layer and a personal layer; they move at different rates). An interaction is **generated
from the current edge vector plus the triggering event**, not selected by the trigger alone. The same
loss yields a supportive exchange on a high-`Trust` edge and a pointed one on a low-`Trust` edge.

Edges **decay toward a per-entity baseline** each world-tick (relationships cool without contact) and
are **reinforced by events** (a public defence, a benching, a contract snub). Decay/reinforcement
constants are all `[GT]`. This is pure state evolution → fully deterministic and snapshot-friendly.

### 2.2 Episodic memory (kills "I've seen this line")

Give each edge a small bounded ring buffer of **episodes**: `(eventKind, salience, worldTick,
managerChoiceId)`. When an interaction is generated, recent high-salience episodes are eligible to be
*referenced* ("after what you said about me in March…"). Memory is what converts a graph from
"current mood" into "a history." Even an 8–16 slot buffer per significant edge removes most of the
repetition feel, because the *context* the line is built on is never identical twice.

### 2.3 Procedural narrative templates (intent ≠ surface text)

Separate **intent** (graph-driven: *media wants to provoke on title-pressure*) from **surface text**
(grammar/template expansion with slots filled from real facts — opponent name, the actual xG, the
player referenced from memory). One intent → effectively unbounded phrasings, all anchored to true
match facts. This is the layer that prevents the graph itself from feeling repetitive: without it,
identical intents still emit identical strings.

### 2.4 Emergent storyline arcs (events → stories)

When edge values cross thresholds, **spawn an arc** with its own lifecycle state (e.g.
`DressingRoomSplit`, `MediaVendetta`, `WonderkidVsVeteran`, `BoardPatienceCollapse`). An arc is a
small state machine that lives across multiple world-ticks, biases which interactions fire while
active, and resolves (or escalates) based on subsequent results and manager choices. Arcs are what the
player remembers as "that season when…".

### 2.5 Social contagion / factions (outcomes propagate)

Let opinion diffuse across the graph: an unhappy senior pro shifts teammates' edges toward the manager
by an amount scaled by their inter-player edge weight and the senior's influence. This produces
non-obvious, system-level outcomes (a mishandled benching becomes a dressing-room faction) that stay
interesting because they aren't a single scripted branch. Contagion is a bounded, deterministic
relaxation step over the graph each world-tick.

---

## 3. Recommended composition

**Backbone §2.1 + memory §2.2 + procedural text §2.3 are the minimum viable set** — the graph alone
still feels repetitive if its outputs are canned, so §2.3 is not optional. **§2.4 (arcs)** and
**§2.5 (contagion)** are high-value second-phase additions that turn the substrate into stories and
emergent drama.

Recommended phasing:

| Phase | Adds | Payoff |
|---|---|---|
| A | §2.1 graph + §2.2 memory + §2.3 templates | interactions stop repeating; history is felt |
| B | §2.4 arcs | events accumulate into seasons-long storylines |
| C | §2.5 contagion/factions | squad-level emergent drama |

Phase A is the load-bearing deliverable; B and C are layered on the same state with no rework.

---

## 4. Determinism & architecture constraints (non-negotiable)

- **Separate slow loop.** The world graph ticks on the season/calendar loop (event- and day-driven),
  **never** inside the 10 Hz / 60 Hz match loops. Tag it as such to avoid the loop-conflation hazard
  called out in `CLAUDE.md`.
- **RNG.** Any stochastic edge nudge or template selection draws from **SplitMix64** seeded from world
  state — never `System.Random`, never wall-clock. This keeps "what the journalist asked" replayable.
- **State, not scripts.** The graph, memory buffers, and arc state machines are plain serialisable
  structs. Off-pitch save/load, replay, and debug-rewind fall out of the same state-snapshot model the
  match uses (`CLAUDE.md` "When Writing Code").
- **Zero magic numbers.** Every gain, decay rate, baseline, volatility, threshold, and contagion
  coefficient is a `[GT]` constant in a designated catalogue, validated by a balance pass (precedent:
  #21 G2 balance pass, #8 draft-level approval).
- **Ordinal stability.** Edge-type, arc-kind, and intent enums are embedded in saved state and likely
  in any event digest — reordering breaks save compatibility. They carry the ordinal-stability contract
  from day one.

---

## 5. Sequencing

This is **Stage 1+**, after: the match engine produces structured outcome events; the `[GT]`
config-loader exists; and ideally after #21 (tactical instructions) so manager *tactical* intent and
manager *social* intent share the same config and meta-loop plumbing. It does **not** block, and is
not blocked by, any Stage-0 physics/AI spec. Promotion path mirrors #21: this note → adversarial
passes → formal spec folder with FR-IDs, §5 test plan, §9 checklist.

---

## 6. Risks / open questions for review

1. **Authoring cost is the real risk.** Emergent + procedural systems need a sizeable, curated
   template/grammar corpus and tuning guardrails or they emit nonsense. Budget a content-authoring and
   balance pass comparable to a spec's `[GT]` validation — this is larger than the engineering.
2. **Tuning legibility vs. emergence.** The more emergent the system, the harder designers can predict
   why an outcome happened. Need a debug/inspector view of the graph + arc state (also the natural
   home for replay verification).
3. **Combinatorial test surface.** "Interaction depends on full graph state" is hard to unit-test.
   Likely needs scenario-style fixtures (cf. the #19 ScenarioRunner pattern) that seed a graph,
   tick the world loop, and assert envelope properties of the generated intent — not exact strings.
4. **Aggregate vs. individual fan modelling.** Single fanbase node (cheap, less rich) vs. segmented
   fan factions (richer, more state). Recommend starting aggregate; segmentation is a contagion-layer
   extension.
5. **Localisation.** Procedural template expansion interacts badly with grammatical-gender/inflection
   languages. If multi-language is a goal, the template grammar must be designed for it up front, not
   retrofitted.
6. **Scope boundary with the match engine.** Confirm the world loop only ever *reads* match outcome
   events and never reaches into live match state — preserves the clean layer separation.
