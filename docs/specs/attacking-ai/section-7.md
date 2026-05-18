# Attacking AI Specification #15 — Section 7: Future Extensions

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 7.1 Stage 1 — Runtime Activation (KD-17)

The first code deliverable from this spec. Activates once all three
preconditions from FR-AT-033 / §1.8 are satisfied:

1. **ERR-015-002 ratified** — `TacticalContext.AttackIntent[]?` nullable
   field added to #8 §2.2.6, and #8 §3.1.7 MOVE_TO_POSITION utility
   updated to consume `runTargetPosition` when role = RUNNER. Option B
   selected (mirrors `PressDirective?` / `MarkDirective?` precedent).

2. **#12 Positioning AI reaches APPROVED** — `BaselineDefensiveShapeView`,
   `PositioningAI.GetPhase(TeamId)`, `PositioningAI.GetLine(EntityId)`,
   and the `RunIntent` writer-layer struct (§4.5.2 of #12) are all
   available for Stage 1 runtime use.

3. **ERR-015-003 / ERR-015-004 channel rows landed** — `ATTACK_RUN_STARTED`
   and `OVERLOAD_DECLARED` channel definitions registered in #17 §3.10
   channel registry.

Until all three clear, #15 ships as inert specification — exactly the
pattern established by Pressing AI #13 §1.8 and Defensive AI #14 §1.8.

---

## 7.2 Stage 1+ — Event System Integration

**`ATTACK_RUN_STARTED` event:**
Fired when an agent's `AttackIntent.role` transitions **to** `RUNNER`
(not on every tick the agent holds RUNNER — only on the transition tick).
The event payload includes: `agentEntityId`, `runTargetPosition Vector2`,
`runTriggerTick int`, `teamId`.

**`OVERLOAD_DECLARED` event:**
Fired when `AttackDirective.overloadActive` transitions from `false` to
`true` (not on every tick the overload holds). The event payload includes:
`teamId`, `overloadFlank` (LEFT/RIGHT), `tick int`.

Both channels require atomic back-prop into #17 §3.10 channel registry via
**ERR-015-003** (`ATTACK_RUN_STARTED`) and **ERR-015-004**
(`OVERLOAD_DECLARED`) at Stage 1 first commit. No channel emission at
Stage 0.

**Event System Appendix note:** `event-system/appendices.md` reserves byte
range `0x18…0x1B` for #14 event channels. If #14 occupies `0x1B`, #15
channels receive the next available block at Stage 1 first-commit per #17
§3.10 schema. Exact byte values are Stage-1 deliverables, not Stage-0 blockers.

---

## 7.3 Stage 1+ — #14 Emergency-Flag Consumer

When Defensive AI #14's `MarkDirective.emergencyFlag` is exposed by the
orchestrator (a `bool` field on `MarkDirective` per #14 FR-DA-034), #15
may consume it to zero `transitionHoldTick` immediately on a goal-risk
turnover — so agents collapse to their defensive baseline instantly instead
of holding the attack directive for `TRANSITION_HOLD_TICKS`.

This coupling is declared here as a **Stage 1+ boundary hint** per KD-6.
No interface is authored at Stage 0 (CLAUDE.md "Interface Design Principle"
— #14 is currently IN REVIEW). The coupling will be authored once both #14
and #15 reach APPROVED and Stage 1 implementation begins.

**Rationale:** The `emergencyFlag` represents a last-man / goal-threat
scenario where even a 5-tick hold of the attack directive (≈ 500 ms) is
dangerous. The COUNTER_ATTACK profile already sets `TRANSITION_HOLD_TICKS = 0`
unconditionally; the `emergencyFlag` consumer extends this immediate-recovery
behaviour to POSSESSION and DIRECT profiles in emergencies only.

---

## 7.4 Stage 1+ — xG-Model Integration

Replace the dangerous-zone surrogate (§5.7) with a proper expected-goals
(xG) model once Shot Mechanics #6 Stage 1+ xG surface is available per
#6 §7. The surrogate metric continues in parallel as a regression-detection
baseline — a cheap check that requires no xG model.

---

## 7.5 Stage 2+ — Set-Piece Attacking Positioning

Corner and free-kick attacking runs — runners to the near post, far post,
penalty spot, and decoy positions — are owned by #15 at Stage 2+ when the
set-piece event infrastructure is available. This is a permanent out-of-scope
item at Stage 0 and Stage 1 (KD-17).

Design note: the `RunParameters` parameterization (`depthOffset_m`,
`lateralOffset_m`, `runTriggerTick`) is general enough to express set-piece
run geometry without modification. The Stage 2+ extension adds a
`SetPieceRunIntent` variant of `AttackIntent` (or reuses `RunParameters`
with a set-piece context flag) when a dead-ball event is active.

---

## 7.6 Stage 2+ — Per-Archetype Attacking Profiles

Formation-specific default run-geometry presets, e.g.:
- 4-3-3: wide attackers use higher `lateralOffset_m`; striker uses
  `depthOffset_m ≈ 25m` (central channel deep run).
- 4-2-3-1: attacking midfielder uses `depthOffset_m ≈ 12m` (support
  ball); wider midfielders use overlap geometry.

Per-archetype presets are a Stage 2+ extension of the team-style profile
system (§3.10). They do not require algorithm changes — only additional
`[GT]` constant sets in `AttackingAIConstants.cs`.

---

## 7.7 Stage 2+ — Tactical Instruction Overlay

Per-match manager instructions wiring into style-profile selection and
individual run-instruction overrides. Requires coach-UI infrastructure
(Stage 2+). The mechanism at Stage 2+ is:
- Manager sets a style profile index via the tactics screen.
- The match configuration record loads the corresponding constant set
  at kick-off.
- In-match instruction changes reload the profile constant set atomically
  on the next tick.

No enum crosses the boundary into the physics/AI algorithm code (KD-8 /
KD-12 discipline preserved at Stage 2+).

---

## 7.8 Stage 2+ — ML-Tuned `[GT]` Parameter Fitting

Run depth, timing, support radius, and style-profile multipliers as
ML-fit parameters tuned against a large simulated-match corpus. The
`[GT]` tag on these constants explicitly marks them as gameplay-tunable;
ML fitting is the Stage 2+ mechanism for systematic tuning. The algorithm
is unchanged; only the constant values in `AttackingAIConstants.cs`
are updated with the ML-derived results.

---

## 7.9 Stage 5+ — Fixed64 Migration

All `float` arithmetic in the algorithm (§3.4–§3.8) migrates to `Fixed64`
types per Fixed64 Math Library #9 when cross-platform determinism becomes
a requirement at Stage 5+ (multiplayer). See #9 §8.1 and CLAUDE.md "When
Writing Code" for Stage-0/Stage-5+ Fixed64 scope.

Stage 0 achieves single-machine determinism via state snapshots per #16
§3.2; this spec conforms to that architecture.

---

## 7.10 Stage 5+ — Cross-Platform Determinism

Follows from §7.9. Bit-exact parity across platforms requires Fixed64
and the full #9 migration plan. Not a Stage 0–4 concern. Stage 0 per-tick
digest (`AttackHysteresisState[]`, `TransitionHoldState`, `AttackIntent[]`,
`AttackDirective`) uses `float` arithmetic and is deterministic only on the
pinned reference host per #16 §5.5 / `certification-platform.md`.

---

## 7.11 Permanent Exclusions (KD-17 items that do not promote)

These items are deferred not to a specific stage, but indefinitely or to
a separate future spec entirely:

- **Per-player run instructions from tactics screen** — individual player
  run customisation (e.g., "always make the overlap") is a UI/squad-management
  concern, not an AI algorithm concern. Out of scope for #15 permanently.
- **Save-game persistence** — `AttackHysteresisState[]` and
  `TransitionHoldState` are transient simulation state; they do not need
  to survive a save/load cycle. They are re-initialised at match load from
  the saved match state record. No persistence design in this spec.
- **GK in the attacking movement pool** — GK exclusion is permanent and
  unconditional (KD-7 / FR-AT-006). This does NOT promote at any stage.

---

## 7.12 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. §7.1–§7.12 authored. Stage 1 activation preconditions, Stage 1+ event channels, emergency-flag consumer, Stage 2+ extensions, Stage 5+ Fixed64 migration, and permanent exclusions all declared. |
