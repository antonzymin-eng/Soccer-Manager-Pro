# Tactical Instructions Specification #21 — Section 1: Introduction, Scope, Dependencies

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW
**Source:** `docs/tracking/tactical-instruction-layer-design.md` v0.3

---

## 1.1 Purpose and scope

This spec defines the **input layer** a manager (human or AI) sets to express a tactic, and the seams
that route it into the existing Mechanics/AI subsystems. **In scope:** the instruction data model
(enums + aggregate structs), the deterministic mapping/translation functions that turn instructions
into existing subsystem inputs, the role→utility-weight model, and the routing contract. **Out of
scope:** the subsystem behaviours themselves (#8/#11/#12/#13/#14/#15 are consumed, not modified beyond
the named seam additions), match presentation/UI, and set-piece *execution* (only set-piece *duty
assignment* fields are defined, at Stage 1+).

## 1.2 Coordinate / convention inheritance

Inherits the project conventions verbatim: corner-origin pitch (X 0–105 m goal-to-goal, Y 0–68 m
touchline; Ball Physics #1 §1.2); fatigue `0.0 = rested, 1.0 = fatigued` (CLAUDE.md); 10 Hz tactical /
60 Hz physics loop separation. This layer touches no physics and adds no coordinate semantics.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| `project-constants` | this → it | only reference the new assembly makes |
| Decision Tree #8 | it → this | consumes `TeamTactic`/`PlayerTactic` via `TacticalContext`; `UtilityScorer`/`OptionGenerator` seams |
| Positioning AI #12 | it → this | width/role/duty fields on `ContextModifierInputs` / snapshot |
| Pressing AI #13 | it → this | trigger-mask + line-of-engagement fields; counter-press gate |
| Defensive AI #14 | it → this | man-mark override + offside toggle |
| Attacking AI #15 | it → this | style/overload/width |
| Goalkeeper #11 | it → this | `DistributeIntent` policy default |
| Deterministic Sim #16 | it → this | snapshot field-set + `SNAPSHOT_SCHEMA_VERSION` bump (§2.5 / FR-TI-028) |
| Code Standards #20 | governs | layering, naming, constant tags, zero-alloc |
| Match Engine (design note) | it → this | Phase-D assembly layer is the sole populator of the routing fields |

No physics-layer dependency. Per CLAUDE.md "Interface Design Principle," no interface is written against
an unspecified consumer (FR-TI-029).

## 1.4 Relationship to the match-engine roadmap

Runtime activation is **gated** (KD-8): the consumers (#8/#11–#15) are EventBus-lifecycle-only stubs
until match-engine Phase C (Resolve) and Phase D (AI), and instruction values cannot be injected until
the `[GT]` config-loader exists (`src/CLAUDE.md` "WHAT IS NOT HERE YET"). This spec is therefore a
**Stage-1 forward spec**: the data types are authorable now (T0); the seams land as each consumer is
wired (T2–T3 in §7.2).

## 1.5 Key decisions

- **KD-1 — Input-only.** The layer produces no per-tick directive. `MarkDirective`/`AttackDirective`/
  `PressDirective`/`AgentAction` remain owned and produced by their subsystems.
- **KD-2 — Bottom-layer assembly + downward translation.** `TacticalDirector.TacticalInstructions`
  references only `project-constants`. It declares its **own** instruction enums; where a subsystem
  already owns a parallel enum (`PassingStyle`/`PressingMode`/`TriggerFlags`/`FormationFamily`), this
  layer declares an analogue (`TacticPassing`/`TacticPressing`/`TacticTriggerMask`/`TacticFormation`)
  and the **consumer** translates downward. This keeps the `Physics ← Mechanics ← AI` graph acyclic and
  re-homes no approved enum (the alternative — making this layer the canonical owner — would migrate
  enums embedded in approved event payloads/hash inputs; rejected).
- **KD-3 — `PlayerRole` ≠ `RoleId`.** `RoleId {GK..ST}` (#12) is a *position*. Behavioural roles
  (Poacher, Mezzala, …) are modifiers layered on a position; declared as a new `PlayerRole` enum. This
  layer never references `RoleId`.
- **KD-4 — Two-path routing.** #8 receives tactics via `TacticalContext` (already AI-layer). #12–#15
  receive them via new fields on their own per-tick snapshots. The match-engine Phase-D assembly layer
  is the single populator of both paths.
- **KD-5 — Translate-once.** Snapshots store the subsystem's **local** enum; the assembly layer runs
  translation once on a tactic-change, never per agent per tick. No hot-path mapping.
- **KD-6 — No RNG.** Instruction application is deterministic; this layer registers no
  `DeterministicRngService` draw site and allocates no domain tag.
- **KD-7 — Stride-boundary application.** In-match tactic changes apply only at a 10 Hz tactical-stride
  tick boundary, never mid-physics-frame, to preserve replay determinism.
- **KD-8 — Gated activation.** Runtime use is gated on the `[GT]` config-loader and match-engine
  Phase C/D.
- **KD-9 — Manager intent vs. safety floor.** A forced man-mark (FR-TI-023) is honoured **only** within
  #14's §3.10 anti-chaos invariants; the safety floor wins on conflict (§3.5 precedence). This is a
  deliberate decision, not an omission.
- **KD-10 — Identity defaults.** `Balanced`/`Default` factories reproduce the current no-instruction
  baseline exactly (FR-TI-031), so landing the layer with default tactics is a behavioural no-op.
- **KD-11 — `FocusPlay`/`Tempo`/`RoleWeightModifiers` are new logic.** These have no existing #8 hook
  (`OptionGenerator` generates from geometry, not directional preference; `ActionSelector` picks pure max
  EffectiveUtility, so there is no decision threshold for `Tempo` to move); they are new branches and
  carry the heavier review burden (§5.6 / §3.3). `Width`/`DefWidth` differ — they add a field that feeds
  #12's **existing** compactness scaling, not a new branch.
- **KD-12 — Schema ownership.** When Phase D serializes tactics, the field set + ordering is owned here
  (§2.5 / Appendix B) and pinned before T2 to avoid a later `SNAPSHOT_SCHEMA_VERSION` churn.

## 1.6 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Per-tick directive production | excluded | #14/#15/#13 own it |
| 2 | Utility scoring math | adds multiplier inputs only | #8 owns the product |
| 3 | Formation slot geometry | selects family + role offset | #12 owns the table |
| 4 | Press trigger geometry | scales distances / gates mask | #13 owns the predicates |
| 5 | Man-mark assignment | requests override | #14 adjudicates within invariants |
| 6 | GK distribution kinematics | sets policy default | #11 owns execution |
| 7 | RNG | none | #16 |
| 8 | Snapshot framing | owns the tactics field block | #16 owns the codec |
| 9 | Config loading | consumes loader output | Stage-1 `[GT]` loader |
| 10 | Routing/distribution | defines the contract | match-engine Phase D populates |

## 1.7 Stage binding

Spec authored at Stage 0+1; **types** land at T0 (now); **seams/routing** land at Stage 1 with the
match-engine integration phases. No runtime behaviour exists until KD-8 prerequisites clear.

## 1.8 Naming reconciliation

The design supplement used the shorthand folder `tactics/` / assembly `TacticalDirector.Tactics`.
**Canonical here:** folder `tactical-instructions/`, assembly `TacticalDirector.TacticalInstructions`,
catalogue `TacticalInstructionsConstants.cs`. The supplement is superseded by this spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Initial section from supplement v0.3. |
| 0.2 | 2026-06-20 | — | PASS-1 fix pass: KD-11 adds Tempo to the new-logic list; Width/DefWidth clarified as field-feeds-existing (H-1/M-1). |
#endregion
