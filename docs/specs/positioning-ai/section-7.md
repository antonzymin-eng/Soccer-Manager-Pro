# Positioning AI Specification #12 — Section 7: Future Extensions

**Created:** May 15, 2026
**Last Updated:** July 7, 2026 (v0.4 — Cheap-item addition: new §7.13 (Rest-Defense Coverage Check — LANDED) appended below.)
**Version:** 0.4
**Status:** DRAFT

---

All Stage 0 deferrals enumerated below per KD-11. Each subsection
states the deferred scope, the stage gate, and the named binding
slot reserved by Stage 0 work (CLAUDE.md "Interface Design
Principle" — no interface written against unspecified consumers).

## 7.1 Stage 1+ — Authoring Tools and Coach UI

Tactical-instruction sliders, formation editor, per-position
"Individual Instructions" (overlap / hold inside / cut in) per
`master-development-plan.md` §3.2 (Month 5–6 Team Instructions).
Stage 0 substitutes a per-archetype `[GT]` default for
`tacticalIntensity` (FR-PA-032).

## 7.2 Stage 1+ — Set-Piece Positioning

Corner kicks, free kicks, throw-ins, penalty boxes, walls. Stage 0
produces only the open-play baseline shape. Set-piece positioning
will be a separate sub-system attached to `PositioningAI` and gated
on its own tactical-AI spec.

## 7.3 Stage 1+ — `PressOverride` Writer Layer (#13 Binding Slot)

Pressing AI #13 (now `APPROVED` May 17, 2026) publishes a `PressOverride`
displacement layer at Stage 1+ that mutates the per-agent `formationSlot`
BEFORE the orchestrator forwards into `TacticalContext.FormationSlot`.
The composition order at Stage 1+ is:

```
1. #12 computes baseline formationSlot (this spec).
2. #13 applies PressOverride displacement (Stage 1+).
3. Orchestrator forwards into #8 TacticalContext.
```

**Reserved name:** `PressOverride` (§2.2.6; #13 §7.1). NOT implemented at
Stage 0. `PressDirective?` field added to `TacticalContext` (#8 §2.2.6
v1.1.1) via ERR-013-001 Option B; #13 writes this field per-tick at
Stage 1+, DT reads it to adjust PRESS utility (#8 §3.2.7).

## 7.4 Stage 1+ — `RunIntent` Writer Layer (#15 Binding Slot)

Attacking AI #15 may publish a `RunIntent` displacement for
off-ball runs. Same composition pattern as §7.3.

**Reserved name:** `RunIntent` (§2.2.6). NOT implemented at Stage 0.

## 7.5 Stage 1+ — `MarkAssignment` Reader Layer (#14 Binding Slot)

Defensive AI #14 may consume `LineMembership` and `LaneAssignment`
to drive mark/cover responsibilities. The Stage 1+ accessors are
declared in §4.5.2.

**Reserved name:** `MarkAssignment` (§2.2.6). NOT implemented at
Stage 0.

## 7.6 Stage 1+ — Ten Named Formation Variants

Per `master-development-plan.md` §3.2 lines 441–449
(`FormationSystem.cs` Month 3–4 deliverable):

1. 4-4-2 Flat
2. 4-4-2 Diamond
3. 4-3-3 Attack
4. 4-3-3 Holding
5. 4-2-3-1 Wide
6. 4-2-3-1 Narrow
7. 3-5-2
8. 5-3-2
9. 3-4-3
10. 4-1-4-1

Stage 0 ships three families (4-4-2 / 4-3-3 / 4-2-3-1) that cover
the structural patterns (two-striker / front-three / single-striker
with AM). In-family variants gate on tactical-instruction
infrastructure (overlap / hold inside / cut in) which the planning
document defers to Stage 1 ("Month 5–6: Team Instructions" +
"Individual Instructions").

## 7.7 Stage 1+ — Mid-Match Formation Switch

Stage 0 fixes the archetype per side per match (FR-PA-039). Stage
1+ will add a mid-match switch path with its own hysteresis
(`FORMATION_SWITCH_DWELL_TICKS`, value `_TBD_`) and a one-time
re-derivation of all `anchorDwellTicks`.

## 7.8 Stage 1+ — Telemetry Channels via #17 Back-Prop

Two debug-overlay telemetry channels are deferred to Stage 1+, each
requiring an atomic back-propagation patch into #17 §3.10 (same
pattern as `ERR-017-001`):

| Channel (proposed name) | Purpose | Stage |
|---|---|---|
| `SHAPE_TRANSITION` | Phase transitions for debug overlay (Appendix C) | 1+ |
| `LINE_BREACH_ALERT` | Line-membership oscillation for authoring-tool surface | 1+ |

Both channels are Stage 1+ (AR-S1-16: prior "Stage 0+1" labelling
on `SHAPE_TRANSITION` was inconsistent with KD-10 "no #17 channels
at Stage 0" and with §7's Stage 1+ deferral scope). Neither is
produced or consumed at Stage 0.

## 7.9 Stage 2+ — ML-Tuned `[GT]` Parameter Fitting

Pull factors, compactness bases, and lane overload costs are
hand-tuned at Stage 0 and Stage 1. Stage 2+ may apply offline ML
to fit the `[GT]` table from match-event data. The constant tags
remain `[GT]` either way — only the source of the values changes.

## 7.10 Stage 5+ — Fixed64 Migration per #9

`float` arithmetic at Stage 0 (per CLAUDE.md "When Writing Code"
and #9 §8.1). Stage 5+ binds the Fixed64 library when cross-platform
multiplayer becomes a requirement. The `SPACING_EPSILON_M2`
constant (KD-16) will be re-derived in fixed-point ULPs at the
Stage 5 migration.

## 7.11 Stage 5+ — Cross-Platform Determinism

Single-machine determinism is achieved at Stage 0 via state
snapshots (`HysteresisState` is digested per §4.6). Cross-platform
bit-exact parity is deferred to Stage 5+ when Fixed64 lands.

## 7.12 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-16 §7.8 `SHAPE_TRANSITION` retagged Stage 1+ (was inconsistent "Stage 0+1"). |
| 0.3 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | ERR-013-001 back-prop: §7.3 updated — #13 `APPROVED`; `PressDirective?` field in #8 §2.2.6 via Option B; composition contract language made present-tense. |
| 0.4 | 2026-07-07 | AI agent | Cheap-item addition: new §7.13 (Rest-Defense Coverage Check — LANDED) appended below. |

## 7.13 Rest-Defense Coverage Check — LANDED (cheap-item addition, July 7, 2026)

Unlike §7.1–§7.11 above (Stage 1+/2+/5+ deferrals), this is a **landed** Stage-0 addition, motivated
by a tactical-theory cross-reference pass: "rest defence" ("restverteidigung") — the defensive
structure a team leaves behind while attacking, so a counter-attack loss finds cover instead of open
space.

`RestDefenseEvaluator.Evaluate(snapshot, phase)` (pure static, `src/positioning-ai/RestDefenseEvaluator.cs`)
counts active outfield agents (goalkeeper excluded) at or behind `REST_DEFENSE_DEPTH_M`
(`[DERIVED]` = `PITCH_LENGTH_M × REST_DEFENSE_DEPTH_FRACTION` [GT], own-goal-relative, canonical
attack-toward-+X frame) while the team is `IN_POSSESSION`; returns `true` (sufficient / no dampening)
for any other phase, since the concept only applies while attacking. `PositioningAITick.Tick()` runs
it each tick; `GetRestDefenseSufficient()` exposes the result to the match orchestrator, which routes
it into Decision Tree #8's `TacticalContext.RestDefenseSufficient` (new §3.2/§7.7 in #8) — insufficient
coverage dampens PASS/SHOOT/DRIBBLE utility via `TacticalWeights.RestDefenseRiskMult`. Sufficient
(the `Stage0Default` identity, `true`) applies no dampening — byte-identical to pre-addition.
