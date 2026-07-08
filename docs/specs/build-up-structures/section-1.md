# Scripted Build-Up Structures Specification #24 — Section 1: Introduction, Scope, Dependencies

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3

---

## 1.1 Purpose and scope

**In scope:** the `BuildUpStructure` tactic dial and its structure catalogue (per-structure,
per-zone anchor-offset tables); the ball-progression **zone classifier** with hysteresis; the
overlay stage in #12 `SlotComposer`; the `TransitionWon` post-regain suppression window; routing +
serialization contracts. **Out of scope:** any new `ActionType` or Decision Tree branch (KD-1),
pass-pattern scripting (players still choose actions via #8 — this spec shapes *where they stand*,
not *what they do*), set-piece routines, and out-of-possession structures (#14's line/block already
owns that space).

## 1.2 Coordinate / convention inheritance

Project conventions verbatim: corner-origin pitch, X 0–105 m; fatigue 0 = rested; 10 Hz tactical /
60 Hz physics separation. Zones are **team-relative** (measured from the team's own goal line in
its attack-toward-+X canonical frame), inheriting #12's existing team-relative machinery — the
home/away mirror hazard class (ERR-008-002) is addressed by construction, and §5 carries an
away-team case for it anyway.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Positioning AI #12 | it → this | hosts the zone classifier + overlay stage in `SlotComposer`; owns the pipeline and clamp |
| Tactical Instructions #21 | this → it | `TeamTactic` gains `BuildUpStructure` (back-prop ERR, §2.4); consumes existing `TransitionWon` (FR-TI-020) read-only |
| Deterministic Sim #16 | it → this | zone/suppression state serialization; `SNAPSHOT_SCHEMA_VERSION` bump at wiring |
| Event System #17 | this → it | consumes the existing `PossessionChangedEvent` (0x04) to open the post-regain window — the only producer wired today (verified July 7, 2026) |
| Code Standards #20 | governs | catalogue placement, tags, zero-alloc |
| Match Engine (design note) | it → this | Phase-D writer populates routing fields |

Perception System #7 is **not** a dependency: the overlay consumes own-team snapshot + ball
position only, so the perception-boundary invariant (§1.4 KD-5) is satisfied trivially.

## 1.4 Key decisions

- **KD-1 — Formation-table extension, not a new action type** (design-supplement KD-3). No
  `ActionType` is added; the Decision Tree is untouched. The overlay is an additional offset stage
  of the same shape as `ContextModifier`'s compactness stage.
- **KD-2 — Discrete 3-row zone catalogue.** Ball-progression zone ∈ {OwnThird, MiddleThird,
  FinalThird} by team-relative ball X (design-note open question 2 resolved to the discrete
  catalogue, per the `PositioningAIConstants` formation-table precedent), with a hysteresis band so
  a ball oscillating on a boundary cannot flap the whole team's structure (§3.1).
- **KD-3 — Opt-in dial + `TransitionWon` suppression window (refines the supplement).** The
  supplement said "gated by `TeamTactic.TransitionWon`". Used as the *sole* gate that breaks
  default-neutrality: `TeamTactic.Balanced` already sets a non-`CounterAttack` `TransitionWon`, so
  the overlay would activate on a default tactic. This spec therefore splits the gate: activation
  requires the new opt-in `BuildUpStructure ≠ None`; `TransitionWon` instead modulates the
  **post-regain window** — a `CounterAttack`/`CounterPress` plan suppresses the patient overlay for
  `REGAIN_SUPPRESS_TICKS` after each possession regain (the team is breaking, not building), while
  `HoldShape`/`Regroup` apply the overlay immediately. `TransitionWon` thereby gains its first
  AI-side consumer exactly as the supplement's KD-3 seam-claim intended, without sacrificing
  FR-TI-031-class default identity. The supplement is superseded on this point.
- **KD-4 — Overlay is additive and clamp-bounded.** Per-slot offsets add to the composed anchor
  after `ContextModifier`, before `SpacingResolver` and the pitch clamp — structure proposes,
  spacing and the clamp still dispose (§3.2; exact stage order in §4.2, coordinated with #23's
  stage via a shared pinned order if both land).
- **KD-5 — Perception-boundary invariant** (cited verbatim from
  `advanced-positional-behaviors-design.md` §2 KD-5, per its own requirement): *"No new subsystem in
  this group may read another team's internal AI directive struct (`MarkAssignment`,
  `PressDirective`, `AttackDirective`) directly. All opponent-derived signals must route through
  `Perception System #7`'s `FilteredView`."* This spec consumes no opponent data at all, which is
  the strongest possible conformance.
- **KD-6 — Default-neutral via zero-value enum.** `BuildUpStructure.None = 0` is the identity (all
  offsets zero). Per-team zone/suppression state is serialized at wiring (schema bump), like every
  prior AI state surface.
- **KD-7 — No RNG.** Pure functions of ball X, phase, tick counts; no draw site, no domain tag.

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Action selection | excluded | #8 |
| 2 | Slot composition pipeline | adds one offset stage + zone state | #12 owns pipeline/clamp |
| 3 | Formation slot tables | adds overlay tables keyed by existing `LineId`/`LaneId` | #12 owns base tables |
| 4 | Tactic dial data model | requests one field; reads `TransitionWon` | #21 owns `TeamTactic` |
| 5 | Possession-change signal | consumes | #17/match-engine own the producer |
| 6 | Snapshot framing | owns zone/suppression field block | #16 owns codec |
| 7 | RNG | none | #16 |

## 1.6 Stage binding

Stage-1 forward spec; overlay tables + classifier authorable at approval; routing/serialization at
match-engine wiring. No behaviour change until a manager sets `BuildUpStructure ≠ None`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial section; KD-3 records the deliberate refinement of the supplement's TransitionWon gating. |
#endregion
