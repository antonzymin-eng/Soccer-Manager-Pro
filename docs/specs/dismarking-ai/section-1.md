# Dismarking & Marker-Awareness AI Specification #23 — Section 1: Introduction, Scope, Dependencies

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3

---

## 1.1 Purpose and scope

This spec defines a per-agent **marking-pressure signal** and its two consumers. **In scope:** the
`MarkingPressure` formula and its per-agent dwell state; a dismarking offset stage inserted into
Positioning AI #12's `SlotComposer` pipeline; a marked-pass-target utility penalty in Decision Tree
#8's `UtilityScorer`; the `DismarkIntensity` tactic dial and its routing. **Out of scope:** marking
*assignment* (#14 owns `MarkAssignment` unchanged), the marker's own counter-behaviour (a Stage-2+
extension, §7.2), set-piece marking, and any change to Perception System #7's own outputs (this spec
only *consumes* `FilteredView`).

## 1.2 Coordinate / convention inheritance

Inherits the project conventions verbatim: corner-origin pitch (X 0–105 m goal-to-goal, Y 0–68 m
touchline; Ball Physics #1 §1.2); fatigue `0.0 = rested, 1.0 = fatigued`; 10 Hz tactical / 60 Hz
physics loop separation. All evaluation in this spec runs on the 10 Hz heartbeat.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Perception System #7 | this → it | sole input source: `FilteredView` (`VisibleOpponents`/`VisibleTeammates` `PerceivedAgent` entries) |
| Positioning AI #12 | it → this | hosts the `MarkingPressureEvaluator` + dismark offset stage in `SlotComposer` (§4.2) |
| Decision Tree #8 | it → this | `UtilityScorer` applies the marked-pass-target penalty (§3.4) |
| Tactical Instructions #21 | this → it | `TeamTactic` gains the `DismarkIntensity` dial (back-prop ERR, §2.4) |
| Deterministic Sim #16 | it → this | dwell state enters the canonical snapshot; `SNAPSHOT_SCHEMA_VERSION` bump at wiring (§2.3) |
| Defensive AI #14 | none | explicitly **not** a dependency — see KD-1 |
| Code Standards #20 | governs | layering, naming, constant tags, zero-alloc |
| Match Engine (design note) | it → this | Phase-D assembly layer populates the routing fields (§4.3) |

## 1.4 Key decisions

- **KD-1 — Perception-boundary invariant** (cited verbatim from
  `advanced-positional-behaviors-design.md` §2 KD-5, per its own requirement): *"No new subsystem in
  this group may read another team's internal AI directive struct (`MarkAssignment`,
  `PressDirective`, `AttackDirective`) directly. All opponent-derived signals must route through
  `Perception System #7`'s `FilteredView`, preserving the same invariant the project already relies
  on for #8's decision-making (an agent decides off what it perceives, never off omniscient
  state)."* Concretely: `MarkingPressure` is computed from `FilteredView.VisibleOpponents`
  `PerceivedPosition` entries only. A marker inside the agent's blind side and not yet perceived
  produces **no** pressure — that is correct behaviour, not a defect.
- **KD-2 — Pure deterministic signal; no RNG.** `MarkingPressure` is a pure function of the agent's
  `FilteredView` plus its own dwell counter. This spec registers no `DeterministicRngService` draw
  site and allocates no #16 domain tag (design-note open question 1, resolved as leaned).
- **KD-3 — One signal, two consumers, both default-off.** The #12 offset stage moves the *marked*
  agent; the #8 penalty informs the *passer*. Both are gated by `DismarkIntensity` and both are the
  multiplicative/positional identity at the default (`Off`).
- **KD-4 — Default-neutral via zero-value enum.** `DismarkIntensity.Off = 0` is the enum zero value
  **and** the identity row (offset scalar 0.0, penalty mult ×1.0), so an unseeded snapshot field is
  automatically safe — deliberately avoiding the ctor-seeding trap the project hit with
  `MarkingOrientation`/`LineOfEngagement` (whose zero values are non-identity).
- **KD-5 — Dwell state is serialized state.** The per-agent dwell counter + last-marker id persist
  across heartbeats, so they enter the canonical world-state snapshot when the match-engine writer
  lands, with a `SNAPSHOT_SCHEMA_VERSION` bump (same reasoning as every prior AI hysteresis
  surface). T0 code that lands before wiring carries no serialization obligation.
- **KD-6 — Applies to the in-possession team's off-ball outfielders only.** The ball carrier is
  excluded (its escape behaviours are #8 action selection — dribble/pass — not off-ball movement);
  the goalkeeper is excluded; out of possession the concept does not apply and the evaluator returns
  the identity (mirrors `RestDefenseEvaluator`'s phase gate exactly).
- **KD-7 — No new assembly.** Files land inside the extended assemblies (`src/positioning-ai/`,
  `src/decision-tree/`), mirroring `RestDefenseEvaluator.cs` placement, per the design supplement's
  §6 step 8 default plan. §4 records the placement decision explicitly.

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Marking assignment | excluded | #14 owns `MarkAssignment` |
| 2 | What an agent can see | consumes only | #7 owns `FilteredView` |
| 3 | Slot composition pipeline | adds one offset stage | #12 owns the pipeline + clamp |
| 4 | Utility scoring math | adds one multiplier | #8 owns the product + clamp |
| 5 | Tactic dial data model | requests one field | #21 owns `TeamTactic` (back-prop ERR) |
| 6 | Snapshot framing | owns the dwell-state field block | #16 owns the codec |
| 7 | RNG | none | #16 |

## 1.6 Stage binding

Spec authored at Stage 1 (third Stage-1 forward spec). Types + evaluator are authorable on approval;
routing/serialization land with the match-engine writer, gated exactly as #21's T2 seams were. No
runtime behaviour change exists until a manager sets `DismarkIntensity ≠ Off`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial section from supplement v0.3; KD-1 cites the perception-boundary invariant verbatim as the supplement requires. |
#endregion
