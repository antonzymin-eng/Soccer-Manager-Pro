# Living World System Specification #22 — Outline

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.1 — promoted from `docs/tracking/living-world-system-design.md` v0.7)
**Version:** 0.1
**Status:** IN REVIEW (June 21, 2026)
**Source:** `docs/tracking/living-world-system-design.md` v0.7 (June 21, 2026), four adversarial passes + five recorded scope decisions

---

## Purpose

Defines the **off-pitch interaction-generation layer** that defeats the management-game immersion
failure where every media/player/staff/board interaction begins repeating after a few seasons. The
canonical human-systems model — the H-Gate happiness system (vol-2 §1.1), the social graph + pulse
propagation (vol-2 §2), media & narrative systems (vol-2 §7), board/governance (vol-3 §4) — **already
exists in the master volumes and is consumed as-is**. This spec adds only the three constructs canon
lacks that actually break repetition: **per-edge episodic memory**, **intent≠surface procedural text**,
and **emergent narrative arcs** — plus the determinism, level-of-detail, and verification scaffolding
any implementation must honour.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-10), canon consume-as-is matrix, stage binding |
| 2 | Functional requirements (FR-LW-001..033), data structures, failure modes F1–F6 |
| 3 | Algorithms: edge-model extension, episodic memory, procedural text, arcs, LOD + cold-store, provenance |
| 4 | Architecture, assembly/file layout, the season-calendar loop, determinism boundaries |
| 5 | Test plan (unit / integration / simulation / determinism / failure / stress) + FR traceability |
| 6 | Performance budget (slow-loop, off hot path) |
| 7 | Future extensions and Stage-1+ deferrals (recorded scope decisions) |
| 8 | Cross-references (XC-022-NNN), ERR-022-NNN back-props, CLAUDE.md invariant binding |
| 9 | Approval checklist |
| Appendices | Constant catalogue + derivations; episode/arc/summary schema tables |

## Key decisions (summary; **full set KD-1..KD-10 in §1.5**)

- **KD-1** Supplement, not redesign — vol-2/vol-3 human-systems consumed read-only; this layer adds memory, text, arcs only.
- **KD-2** Top-layer off-pitch "world" assembly; references human-systems/data + `project-constants` **downward only**; referenced by nothing in the 10/60 Hz match hot path.
- **KD-3** Edge model reconciliation — adopt vol-2 §2.1's 0.0–1.0 scalar; `Affinity`/`Trust` are additive parallel layers, never a re-scaling of the player edge.
- **KD-4** Separate slow loop on a **deterministic season-calendar clock**; `worldTick` = one calendar day (vol-2 §2.2 latency unit).
- **KD-5** Determinism — dedicated `DeterministicRngService` stream; canonical entity-ID iteration; `ArcKind`-ordinal for non-entity arcs; single-machine snapshot determinism.
- **KD-6** Deterministic text — template/grammar expansion only; **no runtime model inference on saved-state paths**; AI is an offline authoring tool.
- **KD-7** Two-tier LOD — deep tier (human active set) vs. abstracted background tier; cold-store + rehydration; deterministic tier transitions.
- **KD-8** Provenance-at-spawn — every arc and generated interaction records trigger inputs + state-snapshot reference at creation.
- **KD-9** Boundary discipline — read-only over canonical human-systems state; never a second authority over morale.
- **KD-10** Stage-1+ gating — persistent world store + season loop + vol-2/vol-3 implemented + `[GT]` config-loader + structured match-outcome events.
