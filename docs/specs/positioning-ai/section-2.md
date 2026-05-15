# Positioning AI Specification #12 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** May 15, 2026
**Last Updated:** May 15, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.2)
**Version:** 0.1
**Status:** DRAFT

---

## 2.1 Functional Requirements

Conformance levels follow RFC 2119: **MUST** is normative;
**SHOULD** is a strong recommendation subject to documented
override; **MAY** is permissive. All citations resolve against either
a KD in §1.5 or a downstream section in this spec.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-PA-001 | Positioning AI runs on the 10 Hz tactical loop. | MUST | CLAUDE.md / KD-1 |
| FR-PA-002 | Output is one `Vector2 formationSlot` per agent per tick; orchestrator copies it into each agent's frozen `TacticalContext.FormationSlot` at #8 Step 2. | MUST | KD-2, KD-3 |
| FR-PA-003 | Agent iteration order during slot computation is EntityId-sorted ascending. | MUST | #16 §3.2.5 / KD-9 |
| FR-PA-004 | Per-agent `formationSlot` values contribute to the per-tick determinism digest. | MUST | #16 §6.2 / KD-9 |
| FR-PA-005 | RNG calls use `DOMAIN_TAG_POSITIONING_AI = 0x16` (`[CROSS-PENDING]` until `ERR-012-001` resolved). | MUST | #16 §3.4 / KD-9 |
| FR-PA-006 | No heap allocation on the per-tick hot path. | MUST | #18 §3.7 |
| FR-PA-007 | Three formation archetype families (4-4-2, 4-3-3, 4-2-3-1) are shipped at Stage 0; ten named variants ship at Stage 1 per `master-development-plan.md` §3.2. | MUST | KD-7 |
| FR-PA-008 | Anchor selection uses dwell-time hysteresis bound to #2 §3.1. | MUST | KD-8 |
| FR-PA-009 | Line-membership transitions use dead-zone hysteresis. | MUST | KD-8 |
| FR-PA-010 | Lane-occupation transitions use dead-zone hysteresis. | MUST | KD-8 |
| FR-PA-011 | All constants live in a single catalogue file `PositioningAIConstants.cs`. | MUST | #20 FR-CS-025 / KD-17 |
| FR-PA-012 | Hard inter-agent spacing `MIN_AGENT_SEPARATION_M` is enforced between any two computed slots. | MUST | §3.6 |
| FR-PA-013 | Soft spacing penalty applies when two agents share `(line, lane)`. | SHOULD | §3.6 |
| FR-PA-014 | Spacing-violation displacement uses cost-based tie-break (smaller required move displaces); EntityId is the terminal tie-break only when costs are within `SPACING_EPSILON_M2`. | MUST | KD-14 |
| FR-PA-015 | Float comparisons at the spacing boundary use squared distance with `SPACING_EPSILON_M2`. | MUST | KD-16 |
| FR-PA-016 | Fatigue input convention is `0 = rested`, `1 = fatigued`. | MUST | CLAUDE.md / KD-1 |
| FR-PA-017 | Score modifier input is clamped to `[-3, +3]` goal differential. | MUST | §3.5 |
| FR-PA-018 | Tactical-intensity input is clamped to `[0, 1]`. | MUST | §3.5 |
| FR-PA-019 | Anchor formula: `anchor = (pitchLengthM * formationOffset[role].x, pitchWidthM * formationOffset[role].y)`. | MUST | §3.1 |
| FR-PA-020 | Ball-relative offset is piecewise-linear in `ball.x` and `ball.y` with three break-points per axis. | MUST | §3.2 |
| FR-PA-021 | Pull-toward-ball strength is a per-role-per-phase `[GT]` lookup. | MUST | §3.2 |
| FR-PA-022 | Phase is computed locally from possession state and filtered ball longitudinal velocity. | MUST | KD-10 / §3.0 |
| FR-PA-023 | Phase transitions are hysteretic over `PHASE_HYSTERESIS_TICKS`. | MUST | §3.0 |
| FR-PA-024 | Line partition is a stable k=3 longitudinal partition (GK excluded). | MUST | §3.3 |
| FR-PA-025 | Lane partition is a 5-bin lateral classification. | MUST | §3.4 |
| FR-PA-026 | At most two agents per lane in the midfield third. | SHOULD | §3.4 |
| FR-PA-027 | At most three agents per lane anywhere. | MUST | §3.4 |
| FR-PA-028 | Context-modifier composition onto compactness is multiplicative. | MUST | §3.5 |
| FR-PA-029 | Score modifier scales attacking compactness linearly. | MUST | §3.5 |
| FR-PA-030 | Team-mean fatigue relaxes lateral compactness up to `FATIGUE_LATERAL_RELAX_M`. | MUST | §3.5 |
| FR-PA-031 | Tactical intensity scales the vertical compactness target. | MUST | §3.5 |
| FR-PA-032 | Tactical-intensity default source is a per-archetype `[GT]` field (no UI at Stage 0). | MUST | KD-11 |
| FR-PA-033 | All slot writes are clamped to pitch bounds with a 0.5 m touchline margin. | MUST | §2.4 F5 |
| FR-PA-034 | *(DELETED — `StableHash` field dropped per v1.2 outline resolution. #8 has no hysteresis on the slot.)* | — | — |
| FR-PA-035 | Goalkeeper slot is computed by a dedicated formula and is excluded from the line partition. | MUST | §3.3 |
| FR-PA-036 | Substituted and red-carded agents are excluded from compactness computation. | MUST | §2.4 |
| FR-PA-037 | The slot-computation function is pure: deterministic over `(perception, ball, phase, formation, modifiers, prevHysteresisState)`. | MUST | §4.1 |
| FR-PA-038 | Hysteresis state is authoritative simulation state and is digested. | MUST | §4.6 / KD-9 |
| FR-PA-039 | Formation archetype is fixed per side per match at Stage 0 (no in-match switch). | MUST | KD-11 |
| FR-PA-040 | Every constant carries exactly one of `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`. | MUST | KD-12 |
| FR-PA-041 | Every formula in §3 has a worked example in §3 or Appendix E. | MUST | CLAUDE.md |
| FR-PA-042 | **F1 — stale perception:** if the perception snapshot tick index precedes the current tick, the previous tick's `formationSlot` is reused for every agent. | MUST | §2.4 |
| FR-PA-043 | **F2 — invalid formation index:** unknown archetype falls back to 4-4-2 Family. | MUST | §2.4 |
| FR-PA-044 | **F3 — NaN intermediate:** any NaN intermediate (anchor, offset, or composed slot) is replaced with the raw role anchor. | MUST | §2.4 |
| FR-PA-045 | **F4 — mid-tick input change:** inputs arriving after the tick boundary are deferred to the next tick. | MUST | §2.4 |
| FR-PA-046 | **F5 — slot outside pitch:** out-of-bounds slots are clamped to `[0.5, 105 − 0.5] × [0.5, 68 − 0.5]`. | MUST | §2.4 |
| FR-PA-047 | **F6 — phase enum corruption:** invalid phase value falls back to `InPoss` (the least-aggressive shape). | MUST | §2.4 |
| FR-PA-048 | No interface is produced against unspecified consumer specs (#13/#14/#15 at Stage 0). | MUST | CLAUDE.md / KD-4..6 / KD-11 |

## 2.2 Data Structures

### 2.2.1 `PositioningOutput` (Stage 0; #12-internal)

```
PositioningOutput {
    Vector2 formationSlot;        // exposed to orchestrator → #8 TacticalContext
    LineMembership line;          // #12-internal; read-only accessor for Stage 1+ #14
    LaneAssignment lane;          // #12-internal; read-only accessor for Stage 1+ #14
}
```

Field set is owned entirely by #12. The orchestrator reads only
`formationSlot` for #8 forwarding; `line` and `lane` are exposed via
accessors on the `PositioningAI` subsystem itself, NOT via the
frozen `TacticalContext` schema (§4.5).

### 2.2.2 `FormationArchetype` (constant catalogue)

`static readonly` 11-row table per archetype family. Columns:

| Column | Type | Notes |
|---|---|---|
| `role` | `RoleId` enum | GK, CB1, CB2, LB, RB, DM, CM1, CM2, AM, LW, RW, ST1, ST2 — subset per archetype |
| `lateralPct` | `float` | 0–1; multiplied by pitch width 68 m for `anchor.y` |
| `longPct` | `float` | 0–1; multiplied by pitch length 105 m for `anchor.x` |
| `defaultLine` | `LineMembership` | starting partition class |
| `defaultLane` | `LaneAssignment` | starting lateral bin |

### 2.2.3 `TacticalContext` — CONSUMED ONLY (owned by #8 §2.2.6; schema frozen)

```
TacticalContext (#8-owned) {
    Vector2 FormationSlot;        // ← populated from #12.PositioningOutput.formationSlot
    PressingInstruction pressing;
    PassingInstruction passing;
    DefensiveLineDepth defensiveLineDepth;
    // FIELD SET IS FROZEN AT STAGE 0 — adding any field requires
    // a #8 specification amendment (#8 §2.2.6 L688–721).
}
```

#12 populates only the `FormationSlot` field via the orchestrator
calling `TacticalContext.Stage0Default(slot)` (the existing #8
factory). The other fields are not owned or read by #12 at Stage 0.

### 2.2.4 `ContextModifierInputs` (orchestrator-supplied; read-only)

```
ContextModifierInputs {
    int scoreDiff;                // own − opponent, clamped to [-3, +3]
    float teamMeanFatigue;        // [0, 1], 0 = rested
    float tacticalIntensity;      // [0, 1]; default sourced from archetype
}
```

### 2.2.5 `HysteresisState` (Stage 0; digested)

```
HysteresisState {
    int anchorDwellTicks[22];         // ticks since anchor last changed, per agent
    LineMembership lastLine[22];      // last committed line, per agent
    LaneAssignment lastLane[22];      // last committed lane, per agent
    int phaseDwellTicks;              // single team-wide phase counter
    Phase lastPhase;
}
```

All fields contribute to the per-tick determinism digest (FR-PA-038).

### 2.2.6 Reserved Names (Stage 1+ — declared, not implemented)

| Name | Reserved For | Stage |
|---|---|---|
| `BaselineDefensiveShape` (read-only view) | #14 Defensive AI consumption | 1+ |
| `PressOverride` | #13 Pressing AI writer layer | 1+ |
| `RunIntent` | #15 Attacking AI writer layer | 1+ |
| `MarkAssignment` | #14 reader layer | 1+ |

Declared here per CLAUDE.md "Interface Design Principle": names are
reserved so Stage 1+ specs can bind without renegotiation, but no
type, field, or method is published until the downstream consumer
spec reaches `IN REVIEW`.

## 2.3 Inputs (Read-Only at Tick Start)

| Source | Field | Type | Notes |
|---|---|---|---|
| #7 Perception §3.7 | per-agent positions | `Vector2[22]` | EntityId-keyed |
| #7 Perception §3.7 | ball position | `Vector3` | Z component ignored at Stage 0 |
| #7 Perception §3.7 | possession owner | `EntityId?` | `null` for loose ball |
| #7 Perception §3.7 | ball velocity (longitudinal) | `float` | for phase classification §3.0 |
| Orchestrator | `ContextModifierInputs` | struct | computed by match orchestrator |
| #12-internal | prior `HysteresisState` | struct | from previous tick |
| Constants | `FormationArchetype[3]` | `static readonly` | from `PositioningAIConstants.cs` |

## 2.4 Failure Modes and Recovery

| ID | Failure | Detection | Recovery | Test Reference |
|---|---|---|---|---|
| F1 | Stale perception (snapshot older than current tick) | `snapshot.tickIndex < currentTick` | Reuse previous-tick output verbatim; emit dev-log warning | §5.2 unit; §5.3 integration |
| F2 | Invalid formation archetype index | Index ∉ {0, 1, 2} | Fall back to archetype 0 (4-4-2 family); emit dev-log warning | §5.2 unit |
| F3 | NaN in any intermediate (anchor, offset, composed slot) | `float.IsNaN(slot.x) ‖ float.IsNaN(slot.y)` after each composition step | Replace the composed slot with the raw role anchor from §3.1 | §5.2 unit |
| F4 | Input change mid-tick | Input writer arrives after tick start | Defer write to next tick boundary; #12 does not re-read mid-tick | §5.2 unit |
| F5 | Slot outside pitch bounds | `slot.x ∉ [0.5, 104.5] ‖ slot.y ∉ [0.5, 67.5]` | Clamp to bounds with 0.5 m touchline margin | §5.2 unit |
| F6 | Phase enum corruption | Cast from arbitrary int yields invalid enum | Fall back to `InPoss` (least-aggressive shape); reset `phaseDwellTicks` to 0 | §5.2 unit |

Substituted and red-carded agents (FR-PA-036) are not failure modes;
they are filtered out of compactness computation at §3.5 input
preparation and contribute no slot output — their `formationSlot`
is written as `(NaN, NaN)` to the orchestrator's output buffer,
which the orchestrator interprets as "no slot this tick".

## 2.5 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. 48 FRs enumerated; FR-PA-034 marked DELETED. |
