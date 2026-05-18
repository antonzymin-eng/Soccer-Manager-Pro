# Pressing AI Specification #13 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## 2.1 Functional Requirements

Conformance levels follow RFC 2119: **MUST** is normative;
**SHOULD** is a strong recommendation subject to documented
override; **MAY** is permissive. All citations resolve against
either a KD in §1.5 or a downstream section in this spec.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-PR-001 | Pressing AI runs on the 10 Hz tactical loop. | MUST | CLAUDE.md / KD-2 |
| FR-PR-002 | Output is one `PressDirective` per team per tick + one `PressAssignment` per agent per tick. | MUST | KD-2 / KD-3 |
| FR-PR-003 | Agent iteration order during assignment computation is EntityId-sorted ascending. | MUST | #16 §3.2.5 / KD-10 |
| FR-PR-004 | `PressDirective` and `PressAssignment` values contribute to the per-tick determinism digest, alongside the trigger debounce and role hysteresis state. | MUST | #16 §6.2 / KD-10 |
| FR-PR-005 | RNG calls use `DOMAIN_TAG_PRESSING_AI = 0x19` `[CROSS: #16 §3.4]` — ERR-013-005 resolved May 17, 2026; allocated in #16 §3.4 v1.0.3 within the ERR-012-001 Phase B/C block. | MUST | #16 §3.4 / KD-10 |
| FR-PR-006 | No heap allocation on the per-tick hot path. | MUST | #18 §3.7 |
| FR-PR-007 | All constants live in a single catalogue file `PressingAIConstants.cs`. | MUST | #20 FR-CS-025 / KD-15 |
| FR-PR-008 | Fatigue input convention is `0 = rested`, `1 = fatigued`. | MUST | CLAUDE.md / KD-1 |
| FR-PR-009 | Trigger `BAD_TOUCH` fires when #4-derived first-touch quality scalar is below `BAD_TOUCH_THRESHOLD [GT]` AND post-touch ball-velocity escape exceeds `BAD_TOUCH_VELOCITY_M_S [GT]`. | MUST | KD-7 / §3.1.1 |
| FR-PR-010 | Trigger `BACKWARD_PASS` fires when a `PassAttemptEvent` (#5 §2 FR-10) satisfies `dot(normalize((e.TargetPosition − passerPosition).xy), attackingDirection) < BACKWARD_PASS_THRESHOLD [GT]`, where `passerPosition = perception.agents[e.AgentID].position`. | MUST | KD-7 / §3.1.2 |
| FR-PR-011 | Trigger `SIDELINE_TRAP` fires when the ball is within `SIDELINE_TRAP_DISTANCE_M [GT]` of either touchline AND the ball-carrier's facing has a positive component toward that sideline. | MUST | KD-7 / §3.1.3 |
| FR-PR-012 | Trigger `WEAK_RECEIVER` fires when a candidate receiver's `FirstTouch` attribute is below `WEAK_RECEIVER_THRESHOLD [GT]` AND the receiver's perceived local pressure ≥ `WEAK_RECEIVER_PRESSURE [GT]`. | MUST | KD-7 / §3.1.4 |
| FR-PR-013 | Triggers debounce via dwell-time hysteresis (`TRIGGER_DWELL_TICKS [EST]` to fire, `TRIGGER_RELEASE_TICKS [EST]` to clear). | MUST | KD-9 / §3.2 |
| FR-PR-014 | Roles form a disjoint partition per agent per tick: `PRIMARY_PRESS ⊕ COVER_SHADOW ⊕ HOLD_SHAPE`. | MUST | KD-8 |
| FR-PR-015 | At most one `PRIMARY_PRESS` per team per tick. | MUST | KD-8 |
| FR-PR-016 | At most `MAX_COVER_SHADOWS [GT]` (Stage 1 default: 2) cover-shadow assignments per team per tick. | MUST | KD-8 |
| FR-PR-017 | The goalkeeper is always `HOLD_SHAPE` from #13's perspective and is excluded from `WEAK_RECEIVER` candidate sets. | MUST | KD-13 |
| FR-PR-018 | Anti-chaos: at most `MAX_PRESSERS_BALL_THIRD [GT]` (Stage 1 default: 3) agents with role ∈ {`PRIMARY_PRESS`, `COVER_SHADOW`} in the ball-side third. | MUST | KD-16 |
| FR-PR-019 | Anti-chaos: at least `MIN_BACKLINE_AGENTS [GT]` (Stage 1 default: 3) own-team agents whose #12 line membership is `Defense` must remain in their own defensive third. | MUST | KD-16 |
| FR-PR-020 | Anti-chaos: a `COVER_SHADOW` assignment whose target position is further than `MAX_PRESS_DISPLACEMENT_M [GT]` (Stage 1 default: 25 m) from the agent's #12 baseline anchor is rejected. | MUST | KD-16 |
| FR-PR-021 | Anti-chaos invariants are checked BEFORE the directive is published; on violation the directive falls back to all-`HOLD_SHAPE` for that tick (§2.4 F5). | MUST | KD-16 |
| FR-PR-022 | Primary-press target is the ball-carrier `EntityId`. | MUST | §3.3 |
| FR-PR-023 | Cover-shadow targets are the top-`MAX_COVER_SHADOWS` candidate-receivers ranked by descending `threatScore(r)` (§3.4). Defenders are assigned greedily to shadow these receivers in threat-score order, with `coverCost` minimised per slot and ties broken by EntityId ascending within `SPACING_EPSILON_M2`. | MUST | §3.4 |
| FR-PR-024 | Cover-shadow lane position lies on the geometric segment between ball-carrier and target receiver at offset `COVER_SHADOW_LANE_FRACTION [GT]` (Stage 1 default: 0.55). | MUST | §3.5 |
| FR-PR-025 | Role assignment uses cost-based selection (smallest required displacement wins) with EntityId terminal tie-break. | MUST | §3.4 / KD-9 |
| FR-PR-026 | Role transitions use dwell-time hysteresis `ROLE_DWELL_TICKS [EST]`. | MUST | KD-9 / §3.6 |
| FR-PR-027 | Stamina cost: `PRIMARY_PRESS` adds `STAMINA_COST_PRIMARY_PER_TICK [GT]` to the assigned agent's fatigue accumulator. | MUST | §3.7 |
| FR-PR-028 | Stamina cost: `COVER_SHADOW` adds `STAMINA_COST_SHADOW_PER_TICK [GT]` to the assigned agent's fatigue accumulator. | MUST | §3.7 |
| FR-PR-029 | An agent with fatigue ≥ `PRESS_FATIGUE_CEILING [GT]` (Stage 1 default: 0.85) is excluded from press roles. Cite-not-redefine #8 §3.1.8.1 stamina-gate logic — #13 layers an additional ceiling on top of #8's `PRESS_STAMINA_MINIMUM = 0.20`. | MUST | §3.7 / KD-1 |
| FR-PR-030 | Disengage: directive returns to all-`HOLD_SHAPE` if no trigger has fired for `DISENGAGE_TIMEOUT_TICKS [GT]` consecutive ticks. | MUST | §3.8 |
| FR-PR-031 | Disengage: directive returns to all-`HOLD_SHAPE` immediately if the ball leaves the `PRESS_ELIGIBLE_ZONE` polygon (defined by `PRESS_ZONE_X_MIN` / `PRESS_ZONE_X_MAX` `[GT]` in the team's attacking direction). | MUST | §3.8 |
| FR-PR-032 | Reset: after disengage, no new press fires for `RESET_LATENCY_TICKS [GT]` consecutive ticks. | MUST | §3.8 |
| FR-PR-033 | Phase gating: directive is all-`HOLD_SHAPE` if #12's local-phase output for this team is `InPoss` (no press from a team in possession). | MUST | KD-11 |
| FR-PR-034 | Every formula in §3 has a worked example in §3 or Appendix C. | MUST | CLAUDE.md |
| FR-PR-035 | **F1 — stale perception:** if the perception snapshot tick index precedes the current tick, the previous tick's `PressDirective` and per-agent `PressAssignment` are reused verbatim. | MUST | §2.4 |
| FR-PR-036 | **F2 — invalid trigger source:** any NaN in a trigger input (e.g., NaN quality scalar) suppresses that trigger for the tick. | MUST | §2.4 |
| FR-PR-037 | **F3 — mid-tick possession change:** trigger evaluation is deferred to the next tick boundary; no mid-tick re-read. | MUST | §2.4 |
| FR-PR-038 | **F4 — empty cover-shadow candidate set:** any unfilled cover-shadow slot demotes to `HOLD_SHAPE` (does NOT escalate other agents). | MUST | §2.4 / §3.4 |
| FR-PR-039 | **F5 — invariant violation at publication time:** fall back to all-`HOLD_SHAPE` for this tick; emit `dev-log` warning `PRESSING_INVARIANT_FALLBACK`. | MUST | §2.4 / KD-16 |
| FR-PR-040 | **F6 — #12 baseline slot unavailable** (e.g., #12 emits `SENTINEL_NO_SLOT` for the agent): no `PressAssignment` override is emitted for that agent; the agent's last `PressAssignment` is preserved. | MUST | §2.4 |
| FR-PR-041 | Every constant carries exactly one of `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`. | MUST | KD-14 |
| FR-PR-042 | No interface is produced against unspecified consumer specs (#14 / #15 at Stage 0 / Stage 1). | MUST | CLAUDE.md / KD-5 / KD-6 |
| FR-PR-043 | Stage-0 deliverable is spec text only; no runtime code. | MUST | KD-12 / §1.8 |
| FR-PR-044 | Stage-1 activation is gated on: (a) #8 ratifies the `ERR-013-001` mechanism (KD-3 / OI-001); (b) #12 reaches `APPROVED`; (c) `ERR-013-002` / `ERR-013-003` #17 channel rows land. | MUST | KD-12 / §7.1 |

## 2.2 Data Structures

### 2.2.1 `PressDirective` (Stage 1; spec'd at Stage 0)

```
PressDirective {
    TeamId team;
    EntityId? primaryPressAgent;      // null if no primary this tick
    EntityId? ballCarrier;            // null if loose ball
    CoverShadow[] coverShadows;       // length 0..MAX_COVER_SHADOWS
    bool disengageActive;             // true → all-HOLD_SHAPE
    int resetCooldownTicks;           // 0 → no cooldown
    TriggerFlags triggerSnapshot;     // bitmask of fired triggers this tick
}

CoverShadow {
    EntityId agent;
    EntityId receiver;
    Vector2 shadowLanePos;
}
```

### 2.2.2 `PressAssignment` (Stage 1; spec'd at Stage 0)

```
PressAssignment {
    EntityId agent;
    PressRole role;                   // PRIMARY_PRESS | COVER_SHADOW | HOLD_SHAPE
    EntityId? targetEntity;           // ball-carrier or candidate receiver
    Vector2?  targetPosition;         // shadow lane pos for COVER_SHADOW
    int       validThroughTick;       // == currentTick; defensive against stale reads
}
```

### 2.2.3 `PressTrigger` (Stage 1; #13-internal)

```
PressTrigger {
    TriggerFlags flags;               // bitmask: BAD_TOUCH | BACKWARD_PASS | SIDELINE_TRAP | WEAK_RECEIVER
    int[4] dwellCounters;             // per-flag dwell counter
    int[4] releaseCounters;           // per-flag release counter
}
```

### 2.2.4 `RoleHysteresisState` (Stage 1; digested per KD-10)

```
RoleHysteresisState {
    PressRole[22] lastRole;
    int[22]       roleDwellTicks;
    int           disengageDwellTicks;
    int           resetCooldownTicks;
}
```

### 2.2.5 `PressOverride` view (Stage 1; orchestrator-facing)

Read-only view over the current tick's `PressAssignment[]` that the
match orchestrator uses to compose the per-agent target position
ahead of #12's `formationSlot` forwarding (#12 §7.3 reservation
slot). See §4.4.

### 2.2.6 `PressDirectiveSnapshot` (Stage 1; #17-facing)

Read-only view exposed for `PRESS_TRIGGERED` / `PRESS_DISENGAGED`
channel emission and integration tests. Channels themselves are
deferred — see §7.5 / `ERR-013-002` / `ERR-013-003`.

### 2.2.7 Reserved Names (Stage 1+ — declared, not implemented)

| Name | Reserved For | Stage |
|---|---|---|
| `TrapZonePolygon` | Stage 1+ trap-zone authoring (§7.3) | 1+ |
| `PressStyle` enum | Named press styles (high / mid / low) (§7.2) | 1+ |

Declared per CLAUDE.md "Interface Design Principle" — names
reserved so Stage 1+ specs can bind without renegotiation; no type
or field is published at Stage 0.

## 2.3 Inputs (Read-Only at Tick Start)

| Source | Field | Type | Notes |
|---|---|---|---|
| #7 Perception §3.7 | per-agent positions | `Vector2[22]` | EntityId-keyed |
| #7 Perception §3.7 | ball position / velocity | `Vector3` / `Vector3` | sideline trap; Z ignored at Stage 0 |
| #7 Perception §3.9 | possession owner | `EntityId?` | `null` for loose ball |
| #7 Perception §3.10 | per-agent `isActive` | `bool` | substituted / red-carded excluded |
| #7 Perception §3.7–3.10 | per-agent `FirstTouch` attribute | `float` | `WEAK_RECEIVER` source |
| #4 First Touch (perception-propagated) | first-touch quality `q ∈ [0,1]` | `float` | `BAD_TOUCH` source (see Q2 note below) |
| #5 Pass Mechanics §2 FR-10 | `PassAttemptEvent` ring | events | `BACKWARD_PASS` source; payload: `AgentID`, `PassType`, `TargetPosition`, `FrameNumber`; #13 derives pass direction from `perception.agents[e.AgentID].position → e.TargetPosition` |
| #12 Positioning AI (read-only accessor) | baseline `formationSlot[id]` | `Vector2` | composition source for `HOLD_SHAPE` |
| #12 Positioning AI (read-only accessor) | local phase enum | `Phase` | KD-11 phase gating |
| #12 Positioning AI (read-only accessor) | line membership | `LineMembership` | KD-16 backline floor |
| Orchestrator | own-team attacking direction | `Vector2` (unit) | `BACKWARD_PASS` dot-product |
| #13-internal | prior `RoleHysteresisState` | struct | from previous tick |
| #13-internal | prior `PressTrigger` | struct | from previous tick |

**Q2 note (first-touch surface route).** Section-file draft greps
of `first-touch/section-3-1-to-3-5.md` show that the first-touch
control quality `q` and the `pressureScalar` it consumes are
computed **inside** First Touch as a per-touch event and are not
re-published on the per-tick perception snapshot in a form #13 can
read directly without a #4 touch occurring this tick. Section-file
draft therefore commits to: #13 reads the **post-touch ball-velocity
escape** plus the perception-propagated first-touch quality field
that #7 §3.10 carries forward for the most recent touch by each
agent. If #7's snapshot does not carry that field at section-file
draft time, OI-005 captures the open-question; the default is
perception-propagated per outline KD-7 / §1.3.

## 2.4 Failure Modes and Recovery

| ID | Failure | Detection | Recovery | Test Reference |
|---|---|---|---|---|
| F1 | Stale perception snapshot | `snapshot.tickIndex < currentTick` | Reuse previous-tick `PressDirective` + `PressAssignment[]`; emit dev-log warning | §5.2 unit; §5.3 integration |
| F2 | Invalid trigger source (NaN in `q`; null/invalid `TargetPosition` in `PassAttemptEvent`; etc.) | `float.IsNaN(...)` / null check per input | Suppress the affected trigger for this tick; other triggers proceed | §5.2 unit |
| F3 | Mid-tick possession change | Possession owner changes after tick start | Defer trigger evaluation to next tick boundary; emit previous-tick directive | §5.2 unit |
| F4 | Empty cover-shadow candidate set | `candidates.Count == 0` after §3.4 filtering | Demote unfilled slot to `HOLD_SHAPE`; do NOT escalate other agents | §5.2 unit |
| F5 | Anti-chaos invariant violation at publication | KD-16 check fails after §3.9 enforcement | Fall back to all-`HOLD_SHAPE` for this tick; emit `PRESSING_INVARIANT_FALLBACK` warning | §5.2 unit; §5.6 KD-16 corpus |
| F6 | #12 baseline slot unavailable (sentinel) | `PositioningAI.IsSentinel(slot)` for an agent | Skip override for that agent; preserve its last `PressAssignment` (typically `HOLD_SHAPE`) | §5.2 unit |

Substituted and red-carded agents are filtered upstream of trigger
evaluation; their `PressAssignment` is preserved at its
pre-substitution value (consistent with #12's
`SENTINEL_NO_SLOT` handling — F6 above).

## 2.5 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. 44 FRs enumerated. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H1: FR-08 → FR-10 citation in FR-PR-010. AR-S1-H2: FR-PR-010 rewritten to use `TargetPosition - passerPosition` direction instead of `passVelocity`; §2.3 inputs row for #5 updated; F2 failure mode updated. AR-S1-H5: FR-PR-023 rewritten to describe threat-score selection; removed category-error "not already pressed by primary" clause. AR-S1-M5: typo `PRessAssignment` → `PressAssignment` in FR-PR-040. |
| 0.3 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): FR-PR-005 `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]`; ERR-013-005 resolved. |
