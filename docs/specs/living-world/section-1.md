# Living World System Specification #22 — Section 1: Introduction, Scope, Dependencies

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.3 — PASS-6 fix pass: KD-5 split into `world.arcs`/`world.text` RNG sub-streams (AR6-M1))
**Last Updated (prior):** June 21, 2026 (v0.2 — PASS-4 fix pass: KD-3 read-only `PlayerEdge` mirror clause; KD-9 no-write-back extended to vol-2 §2.1 (AR4-L2))
**Version:** 0.3
**Status:** IN REVIEW (June 21, 2026)
**Source:** `docs/tracking/living-world-system-design.md` v0.7

---

## 1.1 Purpose and scope

This spec defines the **off-pitch interaction-generation layer**: how the existing human-systems state
(happiness, the social graph, board/media relationships) is turned into *non-repeating* interactions —
press conferences, player/staff conversations, contract talks — and the narrative arcs that emerge from
them.

**In scope:** per-edge episodic memory; the `InteractionIntent` model and deterministic procedural-text
expansion; emergent narrative arcs and their lifecycle; the additive relationship layers for non-player
nodes; the two-tier level-of-detail (LOD) model with cold-store/rehydration; the deterministic
season-calendar loop; and the verification harness contract.

**Out of scope:** the human-systems model itself (vol-2/vol-3 are consumed, not redesigned); the match
engine and all physics/AI subsystems (#1–#15); UI/presentation; and the *content* of the template and
arc corpora (authored separately per §7, KD-6).

## 1.2 Convention inheritance

Inherits project conventions verbatim: fatigue `0.0 = rested, 1.0 = fatigued` (CLAUDE.md); deterministic
time only (no `DateTime.Now`); `DeterministicRngService` only (no `System.Random`). This layer touches
no pitch coordinates and runs no physics. It introduces one **new** time base — the season-calendar
clock (§4.2, KD-4) — distinct from the match `MatchClock`.

## 1.3 Canon consumed as-is (no redesign — KD-1)

This spec introduces **no mechanic that the master volumes already own**. The following are consumed
read-only:

| Concern | Canonical owner (authoritative) | Relationship |
|---|---|---|
| Happiness / psychological state | vol-2 §1.1 H-Gate (`H = f(Narrative_Stress, Mental_Noise, Acoustic_Distortion, Social_Support)`; Confidence vs. Self-Efficacy) | input |
| Social graph (nodes, edges, cliques) | vol-2 §2.1 (edge 0.0–1.0; clique at mutual > 0.6) | consume; extend per §3.1 |
| Morale spread | vol-2 §2.2 Pulse Propagation (intra 90–100%/instant; inter 40–60% via bridge, 1–2 day latency) | consume as-is — not re-derived |
| Isolation / ego-clash / hazing | vol-2 §2.3 / §2.4 / §2.5 | input |
| Media & narrative events | vol-2 §7 (Press Conference Traps, Transfer Rumor Mill, Commentator Bias, New-Manager Bounce) | consume; arcs sit on top |
| Board / governance | vol-3 §4 (Board Archetypes, Takeover Cycles, DoF Veto) | consume; board arcs route here |
| Supporters / crowd | vol-2 §4.1 Supporter Trust, §5.1 Crowd Dynamics | consume; fan node is an aggregate view |

## 1.4 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| `project-constants` | this → it | one of two downward references |
| Human-systems model (vol-2/vol-3 impl.) | this → it | reads H-Gate, social graph, propagation results, board/media state |
| Deterministic Sim #16 | this → it | `DeterministicRngService` (dedicated `world.arcs`/`world.text` sub-streams); snapshot/replay model; `SNAPSHOT_SCHEMA_VERSION` |
| Testing Strategy #19 | this → it | `ScenarioRunner` closed-loop harness; `tools/spec-stress/` |
| Performance #18 | governs | hot-path/cold-path tagging (this layer is entirely cold-path / slow-loop) |
| Code Standards #20 | governs | layering, naming, constant tags, ordinal stability |
| Match Engine (design note) | this → it | structured match-outcome events are the world loop's input |

No physics-layer dependency. Per CLAUDE.md "Interface Design Principle," no interface is written against
an unspecified consumer (FR-LW-031).

## 1.5 Key decisions

- **KD-1 — Supplement, not redesign.** vol-2/vol-3 human-systems are consumed read-only (§1.3). This
  layer adds only episodic memory (§3.2), procedural text (§3.3), and arcs (§3.4).
- **KD-2 — Top-layer off-pitch assembly.** `TacticalDirector.LivingWorld` references the human-systems
  /data assemblies and `project-constants` **downward only** and is referenced by **nothing** in the
  match hot path (`Physics ← Mechanics ← AI`). The match engine never calls into it; it consumes the
  engine's outcome events. Keeps the reference graph acyclic and the 10/60 Hz path free of off-pitch code.
- **KD-3 — Edge model reconciliation.** The canonical edge is vol-2 §2.1's single 0.0–1.0 scalar with
  the > 0.6 clique threshold; this layer **adopts it unchanged**. Where richer manager↔non-player
  relationships are needed (journalists, board, staff), it adds **parallel layers on the same 0.0–1.0
  scale** (`Affinity`, directional `Trust`) — never a re-scaling of the player edge. `PlayerEdge` is a
  **read-only mirror** of vol-2's authoritative edge (never mutated here; vol-2 owns its evolution);
  only `Affinity`/`Trust` are owned/written. `Affinity` is a *personal* relationship, not a second
  board-confidence or supporter-trust authority (KD-9).
- **KD-4 — Slow loop on a season-calendar clock.** The world ticks on a deterministic season-calendar
  clock (own loop, distinct from `MatchClock`), event- and day-driven; one `worldTick` = one calendar
  day, the unit vol-2 §2.2 latencies are expressed in. It runs **never** inside the 10/60 Hz match loops.
- **KD-5 — Determinism.** All stochastic selection draws from **dedicated `DeterministicRngService`
  sub-streams** (periodic `world.arcs` and aperiodic `world.text` are separate, so player-triggered text
  generation never perturbs the tick cursor); every graph pass iterates in a **canonical order keyed on a
  stable entity ID**; arcs not scoped to a single entity evaluate in fixed `ArcKind` ordinal order; all
  state is snapshot-serialised (single-machine determinism; cross-platform parity stays Stage 5+ per
  CLAUDE.md).
- **KD-6 — Deterministic text.** Surface text is **deterministic template/grammar expansion** with
  selection drawn from the RNG stream. **No generative-model/LLM inference on any path whose output is
  persisted in saved state.** Model assistance is an **offline authoring tool** that produces the static
  corpus only.
- **KD-7 — Two-tier LOD.** The **deep tier** (memory, text, arcs) runs only for the human manager's
  **active set**; every other entity runs an **abstracted background tier** (summary state, no per-edge
  memory or arcs). Departed contacts are **cold-stored** (compressed, not evicted) and **rehydrate** on
  re-entry; tier promotion/demotion is deterministic and lossless.
- **KD-8 — Provenance at spawn.** Every arc and generated interaction records, at creation, its
  **trigger inputs and the state-snapshot reference** that caused it — powering causal tracing/replay
  (§7) and cheap inline but impossible to reconstruct later.
- **KD-9 — Boundary discipline.** The world loop only **reads** canonical human-systems state and
  match-outcome events; it never writes back into the H-Gate, the vol-2 §2.1 social-graph edge, or §2.2
  propagation math. It must not become a second authority over morale.
- **KD-10 — Stage-1+ gating.** Runtime activation is gated on: a persistent world store + season loop;
  vol-2/vol-3 implemented; the `[GT]` config-loader; and structured match-outcome events. Data types and
  algorithms are authorable now; activation lands as the prerequisites do (§7).

## 1.6 Relationship to the stage roadmap

This is a **Stage-1 forward spec** (Priority 6 in `SPEC_INDEX.md`, parallel to #21). None of its
prerequisites (KD-10) exist at Stage 0; it blocks no Stage-0 spec and is blocked by none. The data
types, mappings, and harness contract are specified now so that implementation is a wiring exercise once
the prerequisites land.

## 1.7 Naming reconciliation

Folder `living-world/`, assembly `TacticalDirector.LivingWorld`. The design supplement
`docs/tracking/living-world-system-design.md` (v0.7) is **superseded** by these section files for
normative purposes and retained for history.
