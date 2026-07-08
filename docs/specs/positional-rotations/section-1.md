# Positional Rotations Specification #25 — Section 1: Introduction, Scope, Dependencies

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3

---

## 1.1 Purpose and scope

**In scope:** the `RotationController` (pairwise `SlotIndex` swap of two outfield agents), the
static per-`FormationFamily` rotation-adjacency table, the trigger predicate + two-sided
hysteresis, the `RotationFreedom` dial, the interaction contract with `ShapeAnalyzer`'s existing
line/lane dwell logic, and routing/serialization. **Out of scope:** three-agent (cyclic)
rotations, role/duty changes on rotation (each agent keeps its `PlayerTactic` — only the *slot
binding* swaps), out-of-possession rotations, substitution-driven reassignment, and GK involvement
of any kind.

## 1.2 Coordinate / convention inheritance

Project conventions verbatim (corner-origin pitch; fatigue 0 = rested; 10 Hz / 60 Hz separation).
All geometry in this spec is team-relative, inheriting #12's frame; §5 carries an away-team mirror
case per the ERR-008-002 lesson.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Positioning AI #12 | it → this | owns `FormationSlotRecord`/`AgentPositioningData.SlotIndex`/`SlotComposer`/`ShapeAnalyzer`; hosts the controller |
| Tactical Instructions #21 | this → it | `TeamTactic` gains `RotationFreedom` (back-prop ERR, §2.4) |
| Deterministic Sim #16 | it → this | per-pair rotation state serialization; `SNAPSHOT_SCHEMA_VERSION` bump at wiring |
| Code Standards #20 | governs | catalogue placement, tags, zero-alloc |
| Match Engine (design note) | it → this | Phase-D writer populates the routing field |

Perception #7 / Decision Tree #8 / Defensive AI #14 are untouched. No opponent data is consumed
(KD-7's invariant satisfied trivially).

## 1.4 Key decisions

- **KD-1 — Static adjacency table** (design-note open question 3 resolved to the cheaper answer):
  which slot pairs may rotate is a static per-`FormationFamily` `[GT]` table (Appendix A), not
  tactic-configurable and not computed. This bounds the evaluation set (the supplement's O(n²)
  concern) to ≤ `ROTATION_MAX_PAIRS_PER_FAMILY` small, hand-audited pairs.
- **KD-2 — A rotation is exactly one atomic pairwise `SlotIndex` swap.** Both agents' bindings
  exchange in the same heartbeat, before `SlotComposer` runs; no intermediate state where two
  agents share a slot is ever observable (§3.3). Slot records themselves are immutable; the *only*
  mutation this spec introduces anywhere is which agent holds which `SlotIndex`.
- **KD-3 — Organic-exchange trigger.** A pair rotates when each agent is *already* closer to the
  other's composed slot target than to its own by `ROTATION_ADVANTAGE_M` (§3.1) — the controller
  ratifies an exchange play has produced rather than choreographing one. This keeps the trigger a
  pure geometric predicate with no new intent model.
- **KD-4 — Two-sided hysteresis.** Trigger must hold for `ROTATION_TRIGGER_DWELL_TICKS`
  consecutive heartbeats to commit; a committed rotation holds for at least `ROTATION_HOLD_TICKS`
  before it may revert (revert = the mirrored predicate + its own dwell). Parallel in shape to
  `AgentHysteresisState`'s line/lane dwell pattern.
- **KD-5 — ShapeAnalyzer interaction contract.** Rotation runs strictly **before** `ShapeAnalyzer`'s
  line/lane re-sort each heartbeat and must not thrash against it:
  `ROTATION_HOLD_TICKS ≥ ShapeAnalyzer's line-dwell constant` is a `[DERIVED]` lower-bound
  constraint (Appendix D derivation), locked by a `BalancePassInvariants`-style test so the two
  hysteresis systems cannot oscillate against each other (the supplement's KD-4(c) contract).
- **KD-6 — Churn caps.** At most `ROTATION_MAX_PER_TICK` (= 1) rotation commits per team per
  heartbeat, evaluated in ascending pair-table order (deterministic priority); an agent may appear
  in at most one *committed* rotation at a time (§3.3 partner-lock).
- **KD-7 — Perception-boundary invariant** (cited verbatim from
  `advanced-positional-behaviors-design.md` §2 KD-5, per its own requirement): *"No new subsystem in
  this group may read another team's internal AI directive struct (`MarkAssignment`,
  `PressDirective`, `AttackDirective`) directly. All opponent-derived signals must route through
  `Perception System #7`'s `FilteredView`."* This spec consumes own-team geometry only.
- **KD-8 — Default-neutral; serialized state.** `RotationFreedom.Off = 0` is the identity (no
  trigger evaluation at all). Per-pair state + the current slot-binding permutation are serialized
  at wiring with a schema bump — the permutation is genuinely new snapshot content: unlike #23/#24,
  restoring a match mid-rotation must restore *who holds which slot*, not just counters.

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Slot geometry/records | never mutates records | #12 owns tables |
| 2 | Slot **binding** (`SlotIndex`) | owns the swap operation | #12 owns the field + `SeedFromFormation` |
| 3 | Line/lane re-sort + dwell | ordering + inequality contract (KD-5) | #12 `ShapeAnalyzer` owns re-sort |
| 4 | Player role/duty | untouched on rotation | #21 owns `PlayerTactic` |
| 5 | Tactic dial | requests one field | #21 owns `TeamTactic` |
| 6 | Snapshot framing | owns pair-state + permutation block | #16 owns codec |
| 7 | RNG | none | #16 |

## 1.6 Stage binding

Stage-1 forward spec; recommended to land **after** #23/#24 (the supplement's sequencing —
largest risk last, after the KD-pattern is de-risked). No behaviour until `RotationFreedom ≠ Off`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial section; KD-5 inequality contract and KD-6 caps encode the supplement's KD-4(a)–(c). |
#endregion
