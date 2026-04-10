# Decision Tree Specification #8 — Section 3.4 through 3.8

**File:** `Decision_Tree_Spec_Section_3_4_to_3_8_v1_1.md`  
**Purpose:** Defines the remaining five subsections of Section 3 (Technical Specifications)
for Decision Tree Specification #8. These sections cover: (3.4) how `TacticalContext`
instructions are translated into utility multipliers; (3.5) the execution dispatch
layer — the field-by-field population of `PassRequest` and `ShotRequest`, and the
`MovementCommand` construction for all five Agent Movement actions; (3.6) the
`PerceptionSnapshot` intake interface (`ReceiveSnapshot()`), resolving the interface
deferred by Perception System §4.5.3; (3.7) the four-state Decision Tree state machine
with full transition semantics; and (3.8) the edge case catalogue covering all
foreseeable abnormal decision conditions. Section 3.5 includes field-level
cross-references to Pass Mechanics §2.4.1, Shot Mechanics §2.4.1, and Agent Movement
§3.5.4. This file completes Section 3 of the Decision Tree specification.

**Created:** March 05, 2026, 12:00 PM PST  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections:**
- Section 1 v1.1 — KD-1 through KD-7; Stage 0 action set; known limitations
- Section 2 v1.1 — FR-01 through FR-12; all data structures including `TacticalContext`,
  `DecisionContext`, `AgentAction`, `ActionOption`, `DecisionMadeEvent`
- Section 3.1 v1.1 — Option generation; `GenerateOptions()` algorithm
- Section 3.2 v1.2 — Utility scoring; `ScoreOptions()`; all `BaseUtility` formulas
- Section 3.3 v1.0 — `SelectAction()`; Composure noise model; FR-09 derived thresholds

**Cross-Specification References (authoritative sources):**
- `PassRequest` struct: Pass Mechanics Spec #5, §2.4.1 (authoritative definition)
- `ShotRequest` struct: Shot Mechanics Spec #6, §2.4.1 (authoritative definition)
- `MovementCommand` struct: Agent Movement Spec #2, §3.5.4 (v1.4)
- `AgentMovementState` enum: Agent Movement Spec #2, §3.1 (v1.2)
- `FacingMode` enum: Agent Movement Spec #2, §3.5.4 (v1.4)
- `DecelerationMode` enum: Agent Movement Spec #2, §3.5.4 (v1.4)
- `PassExecutor.Execute()` entry point: Pass Mechanics Spec #5, §4.1
- `ShotExecutor.Execute()` entry point: Shot Mechanics Spec #6, §4.1
- `TacticalContext` struct: this specification, §2.2.6 (authoritative definition)
- `DecisionContext` struct: this specification, §2.2.4

**Open Items (non-blocking at v1.0):**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes` in Agent Movement §3.5.6. Amendment AM-002-001 adds them.
  Affects §3.5.3 weak foot determination logic (noted inline).
- `[ERR-008-PENDING]` — `PossessingAgentId` design (Option A vs B) unresolved.
  Does not affect Decision Tree dispatch; affects Pass Mechanics and Shot Mechanics
  possession validation only. Referenced inline for context.

**Version History:**

| Version | Date | Changes |
|---|---|---|
| 1.0 | March 05, 2026 | Initial draft — §3.4 through §3.8 complete |
| 1.1 | March 05, 2026 | Four post-critique fixes: (1) Added football rationale for DRIBBLE suppression under PressingMode.HIGH (§3.4.3). (2) Replaced `FormationSlot.ToVector3()` with explicit `new Vector3(slot.x, 0f, slot.y)` constructor; added coordinate axis convention note (§3.5.6). (3) Added 7 missing constants to §3.4.7 table: `URGENCY_PRESSURE_SCALE`, `SPIN_INTENT_BELOW_CENTRE`, `SPIN_INTENT_OFF_CENTRE`, `PLACEMENT_CORNER_OFFSET`, `MOVE_SPRINT_THRESHOLD`, `MOVE_JOG_THRESHOLD`; corrected constant count from 19 to 23. (4) Replaced duplicate freestanding `const` declarations in §3.5.6 with a reference table pointing to §3.4.7. |

---

## Table of Contents

- [3.4 Tactical Context Integration](#34-tactical-context-integration)
  - [3.4.1 Purpose and Scope](#341-purpose-and-scope)
  - [3.4.2 Instruction-to-Utility Modification Table](#342-instruction-to-utility-modification-table)
  - [3.4.3 PressingMode Multipliers](#343-pressingmode-multipliers)
  - [3.4.4 PassingStyle Multipliers](#344-passingstyle-multipliers)
  - [3.4.5 DefensiveLineDepth Multipliers](#345-defensivelinedepth-multipliers)
  - [3.4.6 Possession-Phase Action Prioritisation](#346-possession-phase-action-prioritisation)
  - [3.4.7 Stage 0 Constant Summary](#347-stage-0-constant-summary)
- [3.5 Execution Dispatch](#35-execution-dispatch)
  - [3.5.1 Dispatch Overview](#351-dispatch-overview)
  - [3.5.2 PassRequest Population (PASS)](#352-passrequest-population-pass)
  - [3.5.3 ShotRequest Population (SHOOT)](#353-shotrequest-population-shoot)
  - [3.5.4 MovementCommand Construction — DRIBBLE](#354-movementcommand-construction--dribble)
  - [3.5.5 MovementCommand Construction — HOLD](#355-movementcommand-construction--hold)
  - [3.5.6 MovementCommand Construction — MOVE_TO_POSITION](#356-movementcommand-construction--move_to_position)
  - [3.5.7 MovementCommand Construction — PRESS](#357-movementcommand-construction--press)
  - [3.5.8 MovementCommand Construction — INTERCEPT](#358-movementcommand-construction--intercept)
  - [3.5.9 Dispatch Failure Modes](#359-dispatch-failure-modes)
  - [3.5.10 Cross-Specification Validation Checks](#3510-cross-specification-validation-checks)
- [3.6 PerceptionSnapshot Intake Interface](#36-perceptionsnapshot-intake-interface)
  - [3.6.1 Interface Definition](#361-interface-definition)
  - [3.6.2 Delivery Contract](#362-delivery-contract)
  - [3.6.3 Forced Refresh Handling](#363-forced-refresh-handling)
  - [3.6.4 Snapshot Lifetime Constraint](#364-snapshot-lifetime-constraint)
- [3.7 State Machine](#37-state-machine)
  - [3.7.1 States](#371-states)
  - [3.7.2 Transition Table](#372-transition-table)
  - [3.7.3 Transition Invariants](#373-transition-invariants)
  - [3.7.4 State Machine Diagram](#374-state-machine-diagram)
- [3.8 Edge Cases](#38-edge-cases)
  - [3.8.1 Edge Case Catalogue](#381-edge-case-catalogue)
  - [3.8.2 Edge Cases Deliberately Not Handled](#382-edge-cases-deliberately-not-handled)

---

## 3.4 Tactical Context Integration

### 3.4.1 Purpose and Scope

`TacticalContext` carries team tactical instructions into the Decision Tree pipeline.
At Stage 0, these are hardcoded defaults (§2.2.6 `Stage0Default()`). At Stage 1, the
Formation System populates real team-specific values.

The tactical integration step occurs in `ScoreOptions()` (§3.2) as a multiplicative
modifier applied to each candidate's `BaseUtility` after the attribute and context
multipliers. The sequencing is:

```
ScoredUtility = BaseUtility
              × AttributeMultiplier(...)
              × ContextMultiplier(...)
              × TacticalModifier(ActionType, TacticalContext)   ← this section
              × (1 − RiskPenalty)
```

`TacticalModifier` is always positive and non-zero. It may be less than 1.0 (suppressing
an action type), equal to 1.0 (neutral), or greater than 1.0 (boosting an action type).
It never drives a utility score to zero — that responsibility belongs to option
availability gates in `GenerateOptions()` (§3.1).

**Scope boundary:** Tactical modifiers adjust *how much* an action is favoured given team
instructions. They do not gate whether an action is available. An action not generated
by §3.1 cannot be rescued by a large tactical modifier. An action generated by §3.1 cannot
be suppressed to zero by a small tactical modifier.

---

### 3.4.2 Instruction-to-Utility Modification Table

The complete Stage 0 tactical modification matrix. All values are [GT] (gameplay-tuned).
All constants reside in `TacticalWeights.cs`. No inline literals.

Each cell shows the multiplier applied to `BaseUtility` for that action when that
instruction is active. Unlisted action-instruction combinations use a multiplier of
`1.0` (neutral).

**PressingMode modifiers** (applies to all agents regardless of possession):

| ActionType | PressingMode.HIGH | PressingMode.MEDIUM | PressingMode.LOW |
|---|---|---|---|
| PRESS | 1.4 [GT] | 1.0 | 0.6 [GT] |
| INTERCEPT | 1.2 [GT] | 1.0 | 0.9 [GT] |
| HOLD | 0.7 [GT] | 1.0 | 1.2 [GT] |
| PASS | 1.0 | 1.0 | 1.0 |
| SHOOT | 1.0 | 1.0 | 1.0 |
| DRIBBLE | 0.9 [GT] | 1.0 | 1.0 |
| MOVE_TO_POSITION | 1.0 | 1.0 | 1.0 |

**PassingStyle modifiers** (applies when agent has possession):

| ActionType | PassingStyle.DIRECT | PassingStyle.MIXED | PassingStyle.SHORT |
|---|---|---|---|
| PASS (long-range ≥ 20m) | 1.3 [GT] | 1.0 | 0.6 [GT] |
| PASS (short-range < 20m) | 0.9 [GT] | 1.0 | 1.3 [GT] |
| HOLD | 0.7 [GT] | 1.0 | 1.2 [GT] |
| DRIBBLE | 0.9 [GT] | 1.0 | 1.0 |
| SHOOT | 1.0 | 1.0 | 1.0 |

Long/short range classification uses the same distance metric as `GenerateOptions()`
§3.1 pass candidate generation — `IntendedDistance` derived from
`PerceivedAgent.Position` relative to `AgentState.Position` at the time of scoring.

The PASS modifiers apply independently to each pass candidate in the scored list. A
DIRECT instruction does not uniformly boost all PASS candidates — it specifically
boosts long-range options and suppresses short ones, producing natural variation in
pass selection rather than a blanket bias.

---

### 3.4.3 PressingMode Multipliers

**Design rationale:**

`PressingMode.HIGH` is a manager instruction meaning "apply pressure to the ball
immediately." It increases PRESS and INTERCEPT utility, ensuring that agents within
`PRESS_TRIGGER_DISTANCE` (§3.2 §3.1) are more likely to select pressing actions over
HOLD or passive movement. The HOLD suppression (×0.7) prevents agents from being
comfortable sitting back when the team instruction demands aggression. The mild DRIBBLE
suppression (×0.9) reflects the football principle that a high-press team plays directly
and immediately — carrying the ball delays the collective press trigger and invites the
opponent to reset; the in-possession agent should release the ball quickly rather than
advance into contact.

`PressingMode.LOW` is the inverse: the manager wants shape retention over immediate
pressure. HOLD and passive movement are favoured; PRESS is suppressed to avoid agents
leaving formation to chase the ball.

At Stage 0, both teams use `PressingMode.MEDIUM` (`TacticalContext.Stage0Default()`).
The multipliers are defined and tested; they simply have no observable inter-team
difference until Stage 1.

**Implementation note:** `PressingMode` multipliers apply to all agents, whether they
have possession or not. A pressing instruction affects the entire team's disposition,
not just out-of-possession agents. The utility scoring context (`DecisionContext.HasPossession`)
already gates which actions are *available* — the tactical modifier operates on top of
the available set, not in place of availability gating.

---

### 3.4.4 PassingStyle Multipliers

**Design rationale:**

`PassingStyle.DIRECT` increases the utility of long passes relative to short ones,
producing a team that favours vertical progression and quick transitions. The HOLD
suppression (×0.7) prevents agents from dwelling on the ball when the team instruction
demands tempo.

`PassingStyle.SHORT` is the possession-football instruction: it boosts short passing
and ball retention (HOLD ×1.2) while discouraging long-range attempts that risk
dispossession. The DRIBBLE modifier remains neutral under all three styles at Stage 0
— individual dribble decisions are governed by `SpaceScore` and attribute multipliers,
not team passing philosophy.

**Long/short range threshold:** `PASS_LONG_SHORT_THRESHOLD = 20.0m` [GT]. This is
the only range-conditional tactical modifier at Stage 0. It uses the same distance
computation as option generation — no duplicate calculation is required.

```
// TacticalWeights.cs
// Controls: divides PassingStyle boost between short and long pass options.
// Effect of increasing: moving threshold up classifies more passes as "long" — 
//   DIRECT teams get more boosted candidates, SHORT teams get fewer boosted candidates.
// Safe tuning range: [12.0m, 30.0m] without structural formula changes.
public const float PASS_LONG_SHORT_THRESHOLD = 20.0f; // [GT]
```

---

### 3.4.5 DefensiveLineDepth Multipliers

`DefensiveLineDepth` is a continuous float [0.0–1.0] (§2.2.6) that controls how deep
or high the team's defensive line sits. At Stage 0 it is `0.5` (MEDIUM) for both teams.

`DefensiveLineDepth` does not modify utility scores directly. It modifies the
**target position** for `MOVE_TO_POSITION` option generation in §3.1: the formation
slot computed for a given agent is shifted upfield or downfield proportionally to
`DefensiveLineDepth`. The utility formula for `MOVE_TO_POSITION` depends on
`distance_to_formation_slot` (§3.2), which is derived from the adjusted slot position.

**Effect on utility:** A higher `DefensiveLineDepth` pushes formation slots upfield
for all players, making out-of-possession players urgently chase a forward defensive
position. A lower `DefensiveLineDepth` pulls slots toward the agent's own goal, making
deep defensive compactness the dominant movement directive.

```
// Formation slot vertical adjustment (applied in GenerateOptions §3.1, not here)
// DefensiveLineDepth = 0.0 → formation slot at 20% pitch depth (deepest line)
// DefensiveLineDepth = 0.5 → formation slot at 50% pitch depth (MEDIUM default)
// DefensiveLineDepth = 1.0 → formation slot at 80% pitch depth (highest line)
adjustedSlotY = baseSlotY + (DefensiveLineDepth - 0.5f) × DEFENSIVE_LINE_DEPTH_RANGE;
// DEFENSIVE_LINE_DEPTH_RANGE = 30.0m [GT] — full displacement range at extremes ±15m
```

`DEFENSIVE_LINE_DEPTH_RANGE = 30.0f` [GT]. Controls: the total pitch-depth range
over which the defensive line can be shifted. Effect of increasing: more dramatic
difference between deepest and highest line settings. Safe tuning range: [20.0m, 40.0m].

---

### 3.4.6 Possession-Phase Action Prioritisation

Tactical context modifiers interact with possession phase. The Decision Tree does not
apply possession-phase modifiers as a separate layer — they are already embedded in the
`BaseUtility` values and `ContextMultiplier` (§3.2). However, `TacticalContext` provides
one additional possession-conditional modifier:

When `DecisionContext.PossessionState == PossessionState.OPPONENT`:
- `PRESS` `TacticalModifier` is multiplied by an additional `PRESS_URGENCY_FACTOR = 1.2f`
  [GT] on top of the `PressingMode` multiplier.
- This is additive urgency: under opponent possession, all pressing instructions shift
  upward. HIGH pressing under opponent possession produces `1.4 × 1.2 = 1.68` effective
  multiplier; MEDIUM produces `1.0 × 1.2 = 1.2`; LOW produces `0.6 × 1.2 = 0.72`.

No other possession-conditional tactical modifier exists at Stage 0.

```
// TacticalWeights.cs
// Controls: extra urgency to press when team does not have possession.
// Effect of increasing: out-of-possession agents become significantly more
//   aggressive in pressing regardless of PressingMode setting.
// Safe tuning range: [1.0, 1.5] — above 1.5 creates unrealistic pressing saturation.
public const float PRESS_URGENCY_FACTOR = 1.2f; // [GT]
```

---

### 3.4.7 Stage 0 Constant Summary

All tactical constants reside exclusively in `TacticalWeights.cs`. The table below
lists every constant defined in this section.

| Constant | Value | Tag | Controls |
|---|---|---|---|
| `PRESSING_HIGH_PRESS_MOD` | 1.4 | [GT] | PRESS utility under HIGH pressing |
| `PRESSING_LOW_PRESS_MOD` | 0.6 | [GT] | PRESS utility under LOW pressing |
| `PRESSING_HIGH_INTERCEPT_MOD` | 1.2 | [GT] | INTERCEPT utility under HIGH pressing |
| `PRESSING_LOW_INTERCEPT_MOD` | 0.9 | [GT] | INTERCEPT utility under LOW pressing |
| `PRESSING_HIGH_HOLD_MOD` | 0.7 | [GT] | HOLD suppression under HIGH pressing |
| `PRESSING_LOW_HOLD_MOD` | 1.2 | [GT] | HOLD boost under LOW pressing |
| `PRESSING_HIGH_DRIBBLE_MOD` | 0.9 | [GT] | Mild DRIBBLE suppression under HIGH pressing |
| `PASSING_DIRECT_LONG_MOD` | 1.3 | [GT] | Long pass boost under DIRECT style |
| `PASSING_DIRECT_SHORT_MOD` | 0.9 | [GT] | Short pass suppression under DIRECT style |
| `PASSING_DIRECT_HOLD_MOD` | 0.7 | [GT] | HOLD suppression under DIRECT style |
| `PASSING_SHORT_LONG_MOD` | 0.6 | [GT] | Long pass suppression under SHORT style |
| `PASSING_SHORT_SHORT_MOD` | 1.3 | [GT] | Short pass boost under SHORT style |
| `PASSING_SHORT_HOLD_MOD` | 1.2 | [GT] | HOLD boost under SHORT style |
| `PASS_LONG_SHORT_THRESHOLD` | 20.0m | [GT] | Long/short pass classification boundary |
| `DEFENSIVE_LINE_DEPTH_RANGE` | 30.0m | [GT] | Formation slot adjustment range |
| `PRESS_URGENCY_FACTOR` | 1.2 | [GT] | PRESS extra multiplier under opponent possession |
| `URGENCY_PRESSURE_SCALE` | 1.0 | [GT] | Maps PressureScalar P to PassRequest.Urgency (§3.5.2) |
| `SPIN_INTENT_BELOW_CENTRE` | 0.6 | [GT] | Default SpinIntent for BelowCentre ContactZone (§3.5.3) |
| `SPIN_INTENT_OFF_CENTRE` | 0.8 | [GT] | Default SpinIntent for OffCentre ContactZone (§3.5.3) |
| `PLACEMENT_CORNER_OFFSET` | 0.1 | [GT] | Inward nudge from post/bar for PlacementTarget (§3.5.3) |
| `MOVE_SPRINT_THRESHOLD` | 15.0m | [GT] | Distance above which agent sprints to formation slot (§3.5.6) |
| `MOVE_JOG_THRESHOLD` | 6.0m | [GT] | Distance above which agent jogs to formation slot (§3.5.6) |

**Total §3.4–3.5 constants: 23 [GT].** (16 tactical + 7 dispatch/movement)  
All constants reside in `TacticalWeights.cs` (§3.4 constants) or `UtilityWeights.cs`
(§3.5 constants), as noted in the defining subsection.

---

## 3.5 Execution Dispatch

### 3.5.1 Dispatch Overview

`DispatchAction()` (Step 6 of the pipeline, §2.1.2) receives the `AgentAction` selected
by `SelectAction()` (§3.3) and routes it to the appropriate execution system. This is a
**pure routing function** — it performs no utility evaluation, no attribute reads, and no
physics computation. Its responsibility is:

1. Construct the execution request struct appropriate to `ActionType`.
2. Populate all required fields from `DecisionContext` and `AgentAction.Payload`.
3. Call the entry point of the appropriate execution system.
4. Return without waiting for execution completion.

All execution systems are synchronous at Stage 0. `DispatchAction()` returns as soon as
the execution system's entry-point method returns. For Pass Mechanics and Shot Mechanics,
this means returning after `WINDUP` state entry (not after ball contact). For Agent
Movement, this means returning after the `MovementCommand` is accepted by the movement
controller.

**Ownership boundary:** Once `DispatchAction()` returns, all responsibility for the
action's physical outcome belongs to the receiving execution system. The Decision Tree
does not monitor ball trajectory, does not receive pass completion callbacks, and does not
modify execution state. This is KD-5 (§1.4).

**AR-5 risk (outline):** Incorrect field population is the highest-risk failure mode in
this section. A `PassRequest` with a bad `IntendedDistance` produces wrong velocity; a
`ShotRequest` with an out-of-range `PlacementTarget` triggers Shot Mechanics validation
failure (VR-05/VR-06). Every field below has an explicit source and constraint. Unit
tests in §5.5 verify each field for both valid and boundary inputs.

---

### 3.5.2 PassRequest Population (PASS)

**Execution system:** Pass Mechanics Spec #5, `PassExecutor.Execute(PassRequest)`  
**Entry point:** `Pass_Mechanics_Spec_Section_4_v1_0 §4.1`  
**PassRequest struct definition:** Pass Mechanics Spec #5, §2.4.1 (authoritative)

```csharp
// ============================================================
// PassRequest population — Decision Tree §3.5.2
// Source struct: Pass Mechanics §2.4.1
// Called by: ActionDispatcher.cs, DispatchAction(), PASS branch
// ============================================================

PassRequest request = new PassRequest
{
    // ── Field: AgentID ─────────────────────────────────────────────────────
    // Type: int
    // Constraint: Must be valid agent ID [0–21]
    // Source: DecisionContext.AgentState.AgentId (read-only; set by orchestrator)
    // Validation performed by: Pass Mechanics §4.1 (possession check)
    AgentID = context.AgentState.AgentId,

    // ── Field: PassType ────────────────────────────────────────────────────
    // Type: PassType enum {Ground, Driven, Lofted, ThroughBall, Cross, Chip}
    // Source: AgentAction.Payload.PassType
    //   Populated by GenerateOptions() §3.1 during option generation.
    //   Selection logic per §3.1.2: pass type is determined from IntendedDistance
    //   and PerceivedTeammate geometry at option-generation time, not here.
    // Constraint: Must be a valid PassType enum member.
    PassType = context.SelectedAction.Payload.PassType,

    // ── Field: CrossSubType ────────────────────────────────────────────────
    // Type: CrossSubType enum {Flat, Whipped, High}; default = Flat
    // Source: AgentAction.Payload.CrossSubType
    //   Set by GenerateOptions() only when PassType == Cross.
    //   For all non-Cross pass types: CrossSubType is ignored by Pass Mechanics.
    //   Set to default value (Flat) for non-Cross types — never left uninitialised.
    // Cross-spec note: Pass Mechanics §3.1 warns that a non-null CrossSubType
    //   on a non-Cross PassType does not cause failure (ignored), but this spec
    //   sets it to Flat defensively to avoid confusion in diagnostics.
    CrossSubType = context.SelectedAction.Payload.CrossSubType,  // default Flat

    // ── Field: TargetAgentID ───────────────────────────────────────────────
    // Type: int; -1 means space-targeted
    // Source: AgentAction.Payload.TargetAgentId
    //   For player-targeted passes: the perceived teammate's AgentId, as read
    //   from PerceptionSnapshot.VisibleTeammates[n].AgentId.
    //   For through-balls into space: -1 (GenerateOptions §3.1.3 sets this).
    // Constraint: If ≥ 0, must be a teammate agent [0–21] currently in the
    //   simulation. Decision Tree cannot verify possession state at dispatch;
    //   Pass Mechanics §4.1 validates possession ownership.
    TargetAgentID = context.SelectedAction.Payload.TargetAgentId,

    // ── Field: TargetPosition ──────────────────────────────────────────────
    // Type: Vector3 (world space)
    // Source: AgentAction.Payload.TargetPosition
    //   Player-targeted: ignored by Pass Mechanics (TargetAgentID takes precedence).
    //     Set to Vector3.zero defensively.
    //   Space-targeted (TargetAgentID == -1): the projected through-ball destination
    //     computed in GenerateOptions §3.1.3 from receiver velocity extrapolation.
    //     Must be a valid pitch-space coordinate.
    // Constraint: Only meaningful when TargetAgentID == -1.
    TargetPosition = context.SelectedAction.Payload.TargetPosition,

    // ── Field: IntendedDistance ────────────────────────────────────────────
    // Type: float (metres); must be > 0
    // Source: Euclidean distance between AgentState.Position and TargetPosition
    //   (or TargetAgent.Position if player-targeted), computed at option-generation
    //   time and stored in AgentAction.Payload.IntendedDistance.
    //   NOT recomputed here — agent or target may have moved between option
    //   generation and dispatch, but recomputation is not permitted (determinism).
    // Constraint: > 0. Pass Mechanics §3.2 will log FM-07 and reject if ≤ 0.
    //   GenerateOptions() guarantees this is populated with a valid distance.
    IntendedDistance = context.SelectedAction.Payload.IntendedDistance,

    // ── Field: Urgency ─────────────────────────────────────────────────────
    // Type: float [0.0–1.0]
    // Source: AgentAction.Payload.Urgency
    //   Derived in GenerateOptions §3.1 from PressureScalar P (§3.2).
    //   High pressure → high urgency. Formula:
    //     Urgency = clamp(P × URGENCY_PRESSURE_SCALE, 0.0f, 1.0f)
    //     URGENCY_PRESSURE_SCALE = 1.0f [GT] (stored in UtilityWeights.cs)
    //   This creates a direct mapping: P=0.0 → Urgency=0.0, P=1.0 → Urgency=1.0.
    //   Urgency feeds Pass Mechanics error model §3.5 and windup duration §3.8.
    //   Designers may tune URGENCY_PRESSURE_SCALE to control how aggressively
    //   pressure translates to rushed passing.
    Urgency = context.SelectedAction.Payload.Urgency,

    // ── Field: IsWeakFoot ──────────────────────────────────────────────────
    // Type: bool
    // Source: Computed in GenerateOptions §3.1 per weak-foot determination rule:
    //   IsWeakFoot = (RequiredFoot(targetDirection) != AgentAttributes.PreferredFoot)
    //   Stored in AgentAction.Payload.IsWeakFoot.
    // Note [ERR-007-PENDING]: PreferredFoot determination requires WeakFootRating
    //   from PlayerAttributes (AM-002-001). Temporary proxy: if WeakFootRating not
    //   available, IsWeakFoot = false (conservative — avoids phantom penalties).
    // Cross-spec note: Pass Mechanics §3.7 applies the weak foot error multiplier;
    //   the Decision Tree determines the flag, not the magnitude.
    IsWeakFoot = context.SelectedAction.Payload.IsWeakFoot,

    // ── Field: FrameNumber ─────────────────────────────────────────────────
    // Type: int; must be > 0
    // Source: DecisionContext.CurrentFrame (simulation frame counter; set by
    //   orchestrator each heartbeat).
    // Purpose: Required by Pass Mechanics §3.5 deterministic error hash:
    //   hashInput = AgentId × 73856093 XOR FrameNumber × 19349663 XOR PassTypeIndex × 83492791
    //   (Pass Mechanics Appendix B §App-B-3.5). Same-frame, same-agent, same-type
    //   → identical error angle. Cross-tick determinism requires this field.
    FrameNumber = context.CurrentFrame
};
```

**Post-population call:**

```csharp
PassResult result = PassExecutor.Execute(request);
// Decision Tree does not inspect result. PassExecutor handles all failure modes.
// PassAttemptEvent or PassCancelledEvent published by Pass Mechanics §4.6.
```

**Payload fields required on `AgentAction` for PASS dispatch:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PassType` | `PassType` | §3.1 option gen | Valid enum member |
| `Payload.CrossSubType` | `CrossSubType` | §3.1 (Cross only) | Valid enum; default Flat |
| `Payload.TargetAgentId` | `int` | §3.1 option gen | ≥ 0 or -1 (space pass) |
| `Payload.TargetPosition` | `Vector3` | §3.1 option gen | Valid if TargetAgentId == -1 |
| `Payload.IntendedDistance` | `float` | §3.1 option gen | > 0 |
| `Payload.Urgency` | `float` | §3.1 (from P) | [0.0, 1.0] |
| `Payload.IsWeakFoot` | `bool` | §3.1 option gen | — |

---

### 3.5.3 ShotRequest Population (SHOOT)

**Execution system:** Shot Mechanics Spec #6, `ShotExecutor.Execute(ShotRequest)`  
**Entry point:** Shot Mechanics Spec #6, §4.1  
**ShotRequest struct definition:** Shot Mechanics Spec #6, §2.4.1 (authoritative)

```csharp
// ============================================================
// ShotRequest population — Decision Tree §3.5.3
// Source struct: Shot Mechanics §2.4.1
// Called by: ActionDispatcher.cs, DispatchAction(), SHOOT branch
// ============================================================

ShotRequest request = new ShotRequest
{
    // ── Field: AgentId ─────────────────────────────────────────────────────
    // Type: int; must be > 0 (Shot Mechanics VR-01)
    // Source: DecisionContext.AgentState.AgentId
    // Validation: Shot Mechanics §3.1 VR-01 confirms agent has possession.
    AgentId = context.AgentState.AgentId,

    // ── Field: PowerIntent ─────────────────────────────────────────────────
    // Type: float [0.0, 1.0] (Shot Mechanics VR-02)
    // Source: AgentAction.Payload.PowerIntent
    //   Derived in GenerateOptions §3.1 from the SHOOT utility score.
    //   Mapping: PowerIntent = clamp(GoalOpeningScore × A_Finishing_norm, 0.1f, 1.0f)
    //   High GoalOpeningScore + high Finishing → high power intent (commit to shot).
    //   Low GoalOpeningScore → agent reduces power to increase placement precision.
    //   Minimum 0.1f: prevents zero-power shots from being generated.
    //   The power/accuracy trade-off curve is owned by Shot Mechanics §3.2.
    PowerIntent = context.SelectedAction.Payload.PowerIntent,

    // ── Field: ContactZone ─────────────────────────────────────────────────
    // Type: ContactZone enum {Centre, BelowCentre, OffCentre} (VR-04)
    // Source: AgentAction.Payload.ContactZone
    //   Determined in GenerateOptions §3.1 by shot intent classification:
    //     Centre     → driven low shot (default; most common)
    //     BelowCentre → chip/loft (when GoalkeeperId detected in line of shot
    //                   and GoalOpeningScore is low overhead)
    //     OffCentre  → curl (when agent's LongShots > CURL_LONGSHOTS_THRESHOLD
    //                   and PlacementTarget is near a post)
    //   The threshold constants are defined in §3.1 and live in UtilityWeights.cs.
    //   Shot Mechanics KD-1 explicitly states: DT selects physical parameters;
    //   Shot Mechanics does NOT interpret a shot type label. This field IS the
    //   type indicator — it encodes intent as physics input, not as enum intent.
    ContactZone = context.SelectedAction.Payload.ContactZone,

    // ── Field: SpinIntent ──────────────────────────────────────────────────
    // Type: float [0.0, 1.0] (VR-03)
    // Source: AgentAction.Payload.SpinIntent
    //   Set in GenerateOptions §3.1 based on ContactZone selection:
    //     Centre     → SpinIntent = 0.0f (minimal deliberate spin; natural topspin)
    //     BelowCentre → SpinIntent = 0.6f [GT] (deliberate backspin / float)
    //     OffCentre  → SpinIntent = 0.8f [GT] (deliberate sidespin / curl)
    //   These are starting values for option generation; the Composure noise
    //   model (§3.3) does not modify SpinIntent — it only modifies EffectiveUtility.
    //   SpinIntent variations therefore reflect only the agent's deliberate intent,
    //   not execution noise (execution noise lives in Shot Mechanics §3.3).
    SpinIntent = context.SelectedAction.Payload.SpinIntent,

    // ── Field: PlacementTarget ─────────────────────────────────────────────
    // Type: Vector2; components each [0.0, 1.0] (VR-05, VR-06)
    //   u = [0.0, 1.0]: left post → right post
    //   v = [0.0, 1.0]: ground → crossbar
    // Source: AgentAction.Payload.PlacementTarget
    //   Derived in GenerateOptions §3.1 using goal geometry:
    //     1. Identify visible goal mouth extent from PerceptionSnapshot.
    //     2. Identify highest-value unguarded zone (fewest goalkeeper/defender
    //        positions in the visible path).
    //     3. Select corner of highest-value zone:
    //        PlacementTarget = unguardedCorner + PLACEMENT_CORNER_OFFSET [GT]
    //        PLACEMENT_CORNER_OFFSET nudges target inward from posts/bar by 0.1
    //        to avoid near-post/crossbar clipping in error scenarios.
    //   Error is applied by Shot Mechanics §3.3 in placement-target space —
    //   the Decision Tree provides intent; Shot Mechanics provides execution
    //   imperfection.
    // Constraint: Both components must be in [0.0, 1.0]. Clamped in §3.1 before
    //   being stored in payload. Shot Mechanics VR-05/VR-06 will reject out-of-range.
    PlacementTarget = context.SelectedAction.Payload.PlacementTarget,

    // ── Field: IsWeakFoot ──────────────────────────────────────────────────
    // Type: bool
    // Source: Same determination as PASS (§3.5.2 IsWeakFoot note above).
    //   Required shooting direction relative to agent facing and preferred foot.
    // Note [ERR-007-PENDING]: same proxy applies — false if WeakFootRating unavailable.
    // Shot Mechanics §3.3 applies the weak-foot accuracy penalty.
    IsWeakFoot = context.SelectedAction.Payload.IsWeakFoot,

    // ── Field: DistanceToGoal ──────────────────────────────────────────────
    // Type: float; must be > 0 (VR-07)
    // Source: Computed in GenerateOptions §3.1 as Euclidean distance from
    //   AgentState.Position to the nearest goal-post midpoint.
    //   Stored in AgentAction.Payload.DistanceToGoal.
    //   NOT recomputed at dispatch (same determinism rationale as IntendedDistance).
    // Purpose: Shot Mechanics §3.2 uses this for Finishing/LongShots sigmoid blend.
    DistanceToGoal = context.SelectedAction.Payload.DistanceToGoal,

    // ── Field: FrameNumber ─────────────────────────────────────────────────
    // Type: int; must be > 0 (VR-08)
    // Source: DecisionContext.CurrentFrame
    // Purpose: Shot Mechanics §3.3 deterministic error seed:
    //   matchSeed + agentId + frameNumber → deterministic error vector (OI-004).
    //   Consistent with Pass Mechanics pattern (§3.5.2) and §3.3 SplitMix64 hash.
    FrameNumber = context.CurrentFrame
};
```

**Post-population call:**

```csharp
ShotResult result = ShotExecutor.Execute(request);
// Decision Tree does not inspect result. Shot Mechanics handles all failure modes.
// ShotAttemptEvent published by Shot Mechanics §4.6.
```

**Payload fields required on `AgentAction` for SHOOT dispatch:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PowerIntent` | `float` | §3.1 option gen | [0.0, 1.0] min 0.1 |
| `Payload.ContactZone` | `ContactZone` | §3.1 option gen | Valid enum |
| `Payload.SpinIntent` | `float` | §3.1 option gen | [0.0, 1.0] |
| `Payload.PlacementTarget` | `Vector2` | §3.1 option gen | Both components [0.0, 1.0] |
| `Payload.IsWeakFoot` | `bool` | §3.1 option gen | — |
| `Payload.DistanceToGoal` | `float` | §3.1 option gen | > 0 |

---

### 3.5.4 MovementCommand Construction — DRIBBLE

**Execution system:** Agent Movement Spec #2, movement controller  
**Interface:** `MovementCommand` struct submitted to movement system per Agent Movement §3.5.4 (v1.4)

DRIBBLE means the agent has possession and is moving with the ball at feet. The agent's
goal is to advance toward a dribble target position while maintaining possession.

```csharp
// ── DRIBBLE dispatch ──────────────────────────────────────────────────────
// Agent has possession and chooses to advance with the ball.
// Target: AgentAction.Payload.DribbleTarget — a position computed in
//   GenerateOptions §3.1 as the optimal space to advance into:
//   - Selected from visible open lanes in PerceptionSnapshot
//   - Limited to DRIBBLE_MAX_TARGET_DISTANCE [GT] = 8.0m from agent position
//   - Direction biased toward goal if in attacking third, otherwise
//     toward formation slot to maintain positional discipline

MovementCommand dribbleCmd = new MovementCommand
{
    TargetPosition = context.SelectedAction.Payload.DribbleTarget,
    DesiredState   = AgentMovementState.JOGGING,        // Ball-at-feet pace
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.AUTO_ALIGN,             // Face direction of travel
    FacingTarget   = Vector3.zero,                      // Unused with AUTO_ALIGN
    OverrideSafetyConstraints = false,                  // NEVER override in gameplay
    DebugLabel     = "Dribble"                          // Interned string; no GC alloc
};

MovementController.SubmitCommand(context.AgentState.AgentId, dribbleCmd);
```

**`JOGGING` pace rationale:** Dribbling is intentionally constrained to `JOGGING` at
Stage 0. An agent holding the ball at sprint speed while maintaining precise control is
a Stage 1 concern (dribbling speed modifier). Forcing `JOGGING` prevents the pathological
case of an agent sprinting away from all opponents indefinitely — at `JOGGING` pace,
opponents can close. This is a deliberate Stage 0 gameplay constraint, documented in
§1.6 known limitations.

**Note on dribble speed:** Agent Movement §3.5.4 states: if the requested `DesiredState`
is not achievable, movement system uses the highest achievable state. A JOGGING request
from a STUMBLING agent will be reduced to WALKING. The Decision Tree does not compensate
for this — it always requests JOGGING and trusts Agent Movement's feasibility logic.

---

### 3.5.5 MovementCommand Construction — HOLD

HOLD means the agent has possession but chooses not to advance. The agent stops moving
(or decelerates to idle) while maintaining possession. Facing is locked toward the ball's
last known position to preserve situational awareness.

```csharp
// ── HOLD dispatch ─────────────────────────────────────────────────────────
// Agent has possession and chooses to hold in place.
// Agent decelerates to IDLE. Facing locked toward ball position.
// Note: HOLD is always generated by GenerateOptions §3.1. It is the
//   no-move fallback. Its low BaseUtility (0.25 [GT]) ensures it wins
//   only when all other scored candidates are lower.

MovementCommand holdCmd = new MovementCommand
{
    TargetPosition = context.AgentState.Position,       // Stay in place
    DesiredState   = AgentMovementState.IDLE,
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.TARGET_LOCK,            // Watch ball while holding
    FacingTarget   = context.MatchContext.BallPosition, // Ball position from MatchContext
    OverrideSafetyConstraints = false,
    DebugLabel     = "Hold"
};

MovementController.SubmitCommand(context.AgentState.AgentId, holdCmd);
```

**`BallPosition` source:** `MatchContext.BallPosition` is a public game-state value
(§2.2.5). The Decision Tree is permitted to read ball position from `MatchContext` — it
is not required to route through `PerceptionSnapshot` for publicly available match state.
This is consistent with §1.3.1 ("game-state information publicly available to all agents
may be accessed via `MatchContext`").

---

### 3.5.6 MovementCommand Construction — MOVE_TO_POSITION

MOVE_TO_POSITION instructs the agent to return to or advance toward its formation slot.
The formation slot is provided by `TacticalContext.FormationSlot` (§2.2.6), adjusted for
`DefensiveLineDepth` (§3.4.5).

```csharp
// ── MOVE_TO_POSITION dispatch ─────────────────────────────────────────────
// Agent moves toward formation slot. Pace is determined by urgency:
//   - Distance > MOVE_SPRINT_THRESHOLD [GT] (15m): SPRINTING — urgently out of shape
//   - Distance > MOVE_JOG_THRESHOLD [GT] (6m): JOGGING — moderate repositioning
//   - Distance ≤ MOVE_JOG_THRESHOLD: WALKING — minor adjustment
// This tiered pace avoids agents sprinting across the pitch for small corrections.

float distToSlot = Vector3.Distance(
    context.AgentState.Position,
    new Vector3(                                         // Vector2 slot → world Vector3 (Z=0)
        context.TacticalContext.FormationSlot.x,
        0f,
        context.TacticalContext.FormationSlot.y
    )
);

AgentMovementState pace = distToSlot > MOVE_SPRINT_THRESHOLD
    ? AgentMovementState.SPRINTING
    : distToSlot > MOVE_JOG_THRESHOLD
        ? AgentMovementState.JOGGING
        : AgentMovementState.WALKING;

MovementCommand moveCmd = new MovementCommand
{
    TargetPosition = new Vector3(                          // Vector2 → world Vector3 (Z=0)
        context.TacticalContext.FormationSlot.x, 0f,
        context.TacticalContext.FormationSlot.y),
    DesiredState   = pace,
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.AUTO_ALIGN,
    FacingTarget   = Vector3.zero,
    OverrideSafetyConstraints = false,
    DebugLabel     = "MoveToPosition"
};

MovementController.SubmitCommand(context.AgentState.AgentId, moveCmd);
```

**Threshold constants** — listed in §3.4.7 constant table; defined in `UtilityWeights.cs`:

| Constant | Value | Tuning range | Constraint |
|---|---|---|---|
| `MOVE_SPRINT_THRESHOLD` | 15.0m | [10.0m, 25.0m] | Must be > `MOVE_JOG_THRESHOLD` |
| `MOVE_JOG_THRESHOLD` | 6.0m | [3.0m, 12.0m] | Must be < `MOVE_SPRINT_THRESHOLD` |

**`FormationSlot` Vector2→Vector3 conversion:** `TacticalContext.FormationSlot` is `Vector2`
(pitch XZ-plane position). Conversion is `new Vector3(slot.x, 0f, slot.y)` — Y=0 for
ground-plane movement. There is no `.ToVector3()` helper on Unity's `Vector2`; the
explicit constructor is required. This is consistent with Agent Movement's 2D-primary,
3D-secondary coordinate convention (Agent Movement §3.5.3 `FacingDirection` is XY-plane).
**Note on axis convention:** Agent Movement uses XY for the pitch plane; Ball Physics
uses XZ. This section uses XZ convention (Y=0) consistent with Ball Physics Spec #1
§1.2 which is the authoritative coordinate system source. Confirm against Agent Movement
§3.5.3 before implementation.

---

### 3.5.7 MovementCommand Construction — PRESS

PRESS instructs the agent to close down an opponent who has or is receiving the ball.
The press target is an `AgentId` from the agent's visible opponents list.

```csharp
// ── PRESS dispatch ────────────────────────────────────────────────────────
// Agent closes down a visible opponent. Sprint pace; emergency braking.
// PressTargetId: set in GenerateOptions §3.1 as the opponent with possession
//   (or nearest opponent to ball) within PRESS_TRIGGER_DISTANCE (§3.2).
// The pressing agent runs to the opponent's last perceived position
//   (from PerceptionSnapshot.VisibleOpponents[n].Position).
// It does NOT extrapolate opponent movement — it chases the last known position.
// This is intentional: pressing should be reactionary, not perfectly predictive.

Vector3 pressTargetPos = context.SelectedAction.Payload.PressTargetPosition;
// Note: PressTargetPosition is the perceived position of the target agent
//   at option-generation time. Stored in AgentAction.Payload to preserve
//   determinism — opponent may have moved since option generation.

MovementCommand pressCmd = new MovementCommand
{
    TargetPosition = pressTargetPos,
    DesiredState   = AgentMovementState.SPRINTING,      // Full press = sprint
    DecelerationMode = DecelerationMode.EMERGENCY,      // May need to stop fast
    FacingMode     = FacingMode.TARGET_LOCK,            // Keep eyes on ball carrier
    FacingTarget   = pressTargetPos,                    // Face the opponent
    OverrideSafetyConstraints = false,
    DebugLabel     = "Press"
};

MovementController.SubmitCommand(context.AgentState.AgentId, pressCmd);
```

**`EMERGENCY` deceleration rationale:** A pressing agent may need to stop suddenly —
e.g., the opponent passes before the pressing agent arrives. `EMERGENCY` braking gives
the movement system licence to accept stumble risk in exchange for fast stopping. This is
intentional: a committed press that leads to a stumble if the ball is played away is
realistic football behaviour.

**Payload field required:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PressTargetPosition` | `Vector3` | §3.1 option gen | Must be within PRESS_TRIGGER_DISTANCE of agent |

---

### 3.5.8 MovementCommand Construction — INTERCEPT

INTERCEPT instructs the agent to move to an intercept point on the ball's projected
trajectory, aiming to reach that point before the ball does.

```csharp
// ── INTERCEPT dispatch ────────────────────────────────────────────────────
// Agent moves to a ball trajectory intercept point.
// InterceptPoint: computed in GenerateOptions §3.1 via:
//   1. Project ball trajectory from BallState.Position + BallState.Velocity × t
//   2. Compute agent travel time to candidate intercept points: t_agent = dist / AgentMaxSpeed
//   3. Select intercept point where t_ball ≈ t_agent (feasibility window)
//   4. If no feasible intercept point exists, INTERCEPT option is not generated.
//      (Feasibility is a GenerateOptions gate — by the time we reach dispatch,
//       the intercept is deemed feasible at option-generation time.)
// Stored in AgentAction.Payload.InterceptPoint (world space Vector3).

MovementCommand interceptCmd = new MovementCommand
{
    TargetPosition = context.SelectedAction.Payload.InterceptPoint,
    DesiredState   = AgentMovementState.SPRINTING,      // Always sprint to intercept
    DecelerationMode = DecelerationMode.CONTROLLED,     // Controlled stop at point
    FacingMode     = FacingMode.TARGET_LOCK,            // Watch ball during run
    FacingTarget   = context.MatchContext.BallPosition,
    OverrideSafetyConstraints = false,
    DebugLabel     = "Intercept"
};

MovementController.SubmitCommand(context.AgentState.AgentId, interceptCmd);
```

**`CONTROLLED` deceleration rationale for INTERCEPT:** Unlike PRESS (which uses
`EMERGENCY`), the agent intercepting a ball needs to control their stop at the intercept
point to make a first touch. `CONTROLLED` deceleration reduces stumble risk and gives
First Touch Mechanics (#4) a better quality of contact when the collision occurs.

**Payload field required:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.InterceptPoint` | `Vector3` | §3.1 option gen | Feasible intercept point |

---

### 3.5.9 Dispatch Failure Modes

| ID | Scenario | Detection | Resolution |
|---|---|---|---|
| FM-DT-09 | `AgentAction.ActionType` does not match any dispatch branch | Exhaustive switch default | Log error with `AgentId` and `ActionType`. Produce HOLD command to keep agent safe. This should not occur if §3.1 option generation is correct. |
| FM-DT-10 | `Payload.IntendedDistance ≤ 0` for PASS dispatch | Pre-dispatch assertion | Log error. Replace with `Vector3.Distance(agent, target)` as fallback. |
| FM-DT-11 | `Payload.DistanceToGoal ≤ 0` for SHOOT dispatch | Pre-dispatch assertion | Log error. Compute from `AgentState.Position` to goal centre. |
| FM-DT-12 | `Payload.PlacementTarget` component out of [0.0, 1.0] for SHOOT | Pre-dispatch clamp | Clamp to [0.0, 1.0]. Log warning. Do not reject — clamped value is valid for Shot Mechanics. |
| FM-DT-13 | PASS or SHOOT selected but agent no longer has possession at dispatch | Detected by Pass/Shot Mechanics VR at entry | Pass/Shot Mechanics rejects request (FM-01 / VR-01). Decision Tree receives no success confirmation. Agent remains in EXECUTING state until next heartbeat tick re-evaluates. |

**FM-DT-13 note:** This failure is not preventable by the Decision Tree. Possession
can be lost between option generation (Step 3) and dispatch (Step 6) if a tackle event
arrives mid-pipeline. The correct resolution is Pass/Shot Mechanics rejection at their
entry point, followed by DT re-evaluation on the next heartbeat. The DT must not
attempt possession re-validation at dispatch — that would duplicate Pass/Shot Mechanics
responsibility.

---

### 3.5.10 Cross-Specification Validation Checks

The following cross-specification checks must be completed before §3.5 can be approved.
Each identifies a field, interface, or constant defined in another specification that
§3.5 depends on.

| Check ID | What to Verify | Source Spec | §3.5 Dependency | Status |
|---|---|---|---|---|
| XC-3.5-01 | `PassRequest.AgentID` is `int`, not `AgentId` struct (field name casing) | Pass Mechanics §2.4.1 | §3.5.2 `AgentID =` | ✅ Verified: `public int AgentID` |
| XC-3.5-02 | `PassRequest.FrameNumber` is `int` (not `uint` or `long`) | Pass Mechanics §2.4.1 | §3.5.2 `FrameNumber =` | ✅ Verified: `public int FrameNumber` |
| XC-3.5-03 | `ShotRequest.AgentId` is `int` (casing: lowercase 'd') | Shot Mechanics §2.4.1 | §3.5.3 `AgentId =` | ✅ Verified: `public int AgentId` — NOTE: casing inconsistency between PassRequest (`AgentID`) and ShotRequest (`AgentId`). Log as ERR-011 (documentation inconsistency — non-blocking). |
| XC-3.5-04 | `ShotRequest.PlacementTarget` is `Vector2` (not `Vector3`) | Shot Mechanics §2.4.1 | §3.5.3 `PlacementTarget =` | ✅ Verified: `public Vector2 PlacementTarget` |
| XC-3.5-05 | `MovementCommand.DesiredState` uses `AgentMovementState` enum from Agent Movement §3.1 | Agent Movement §3.1 v1.2 | §3.5.4–3.5.8 | ✅ Verified: `public AgentMovementState DesiredState` |
| XC-3.5-06 | `MovementCommand.FacingMode` has exactly 2 values: `AUTO_ALIGN` and `TARGET_LOCK` (v1.4 alignment) | Agent Movement §3.5.4 v1.4 | §3.5.4–3.5.8 | ✅ Verified: §3.5.4 Issue #3 fix aligned FacingMode to two values |
| XC-3.5-07 | `PassRequest` has no `HeightHint` field (outline §3.5.2 listed it; §2.4.1 does not define it) | Pass Mechanics §2.4.1 | §3.5.2 | ✅ Verified: No `HeightHint` field in Pass Mechanics §2.4.1. Outline §3.5.2 was preliminary and is superseded by this section. |
| XC-3.5-08 | `PassRequest.TargetType` field exists (outline referenced it; §2.4.1 uses TargetAgentID=-1 pattern instead) | Pass Mechanics §2.4.1 | §3.5.2 target encoding | ✅ Verified: Pass Mechanics §2.4.1 uses `TargetAgentID == -1` for space-targeted passes. There is no `TargetType` enum field. Outline §3.5.1 routing table reference to `TargetType` is superseded by this section. |
| XC-3.5-09 | `AgentMovementState.JOGGING` exists (not RUNNING or JOG) | Agent Movement §3.1 v1.2 | §3.5.4 DRIBBLE dispatch | ✅ Verified: `JOGGING` is defined at line 82 of §3.1 v1.2 |
| XC-3.5-10 | `MovementController.SubmitCommand(int agentId, MovementCommand cmd)` is the correct interface | Agent Movement §3.5.4 / §4 | §3.5.4–3.5.8 | ⚠ Forward reference — Agent Movement §4 v1.1 documents the orchestrator command interface but the exact method name is implementation-defined. §3.5 uses `SubmitCommand()` as a placeholder. Verify method name in Agent Movement §4 before this section is approved. |

**ERR-011 (new — §3.5 discovery):** `PassRequest.AgentID` uses uppercase `ID` while
`ShotRequest.AgentId` uses mixed-case `Id`. These are different struct definitions in
different specifications. This is a naming inconsistency that will cause confusion at
implementation. Logged here for Spec Error Log addition. Non-blocking — both are
syntactically valid; the inconsistency is cosmetic but should be harmonised before
implementation begins.

---

## 3.6 PerceptionSnapshot Intake Interface

### 3.6.1 Interface Definition

This section provides the authoritative definition of `ReceiveSnapshot()`, resolving
the interface that Perception System Specification #7, §4.5.3 explicitly deferred to
this specification.

```csharp
// ============================================================
// File: DecisionTree.cs
// Method: ReceiveSnapshot
// Called by: SimulationLoopOrchestrator — once per agent per heartbeat tick,
//            after the Perception System batch completes for that heartbeat.
// NOT a Unity event or message bus subscription.
// NOT a coroutine. Synchronous execution required.
// ============================================================

/// <summary>
/// Receives a PerceptionSnapshot for this agent and runs the full
/// 6-step Decision Tree pipeline synchronously.
/// </summary>
/// <param name="snapshot">
/// The PerceptionSnapshot produced by the Perception System for this agent
/// this heartbeat tick. Passed BY VALUE — DT receives a copy, not a reference.
/// The snapshot is valid only for the duration of this method call. The DT
/// must not cache a reference or retain any ReadOnlySpan fields from the
/// snapshot after this method returns.
/// </param>
public void ReceiveSnapshot(PerceptionSnapshot snapshot);
```

**Key properties of this interface:**

- **Direct method call.** No event bus, no subscription, no shared buffer (OQ-1 resolution,
  §1.4 and Outline §OQ-1). The orchestrator calls this method explicitly for each agent in
  ascending `AgentId` order, after all 22 Perception System evaluations complete for the
  tick. This is KD-7 (§1.4) — sequential evaluation order.

- **Pass by value.** `PerceptionSnapshot` is a value struct. The DT receives its own copy.
  No two agents' DT evaluations share snapshot memory. This resolves AR-3 (outline):
  `ReadOnlySpan<PerceivedAgent>` lifetime concerns are eliminated because the DT works
  on a copy, not on a span into the Perception System's buffer.

- **Synchronous, single-frame return.** The entire 6-step pipeline (Steps 1–6, §2.1.2)
  executes inside this method call. `DispatchAction()` (§3.5) is called before the method
  returns. The orchestrator does not advance to the next agent until `ReceiveSnapshot()`
  returns.

- **No return value.** The method has no return value. Dispatch side effects (movement
  commands, Pass/Shot request submission) are the observable outputs. `DecisionMadeEvent`
  (§2.2.7) is published internally.

---

### 3.6.2 Delivery Contract

The orchestrator must satisfy the following constraints when calling `ReceiveSnapshot()`:

1. **Ordering:** Perception System completes all 22 agent snapshots for tick N before
   any `ReceiveSnapshot()` call for tick N is made. No agent's DT evaluation begins
   until all Perception evaluations for that tick are done. This is KD-1 and KD-7.

2. **Per-tick delivery:** Each agent receives exactly one `ReceiveSnapshot()` call per
   heartbeat tick. The sole exception is forced refresh (§3.6.3).

3. **Agent ID ordering:** Agents are called in ascending `AgentId` order (0 through 21).
   This guarantees determinism — combined with the per-option noise hash (§3.3.3), any
   given match seed and tick number produces an identical 22-agent decision sequence.

4. **Frame counter:** `DecisionContext.CurrentFrame` must be set to the orchestrator's
   current simulation frame before each `ReceiveSnapshot()` call. The frame counter is
   monotonically increasing across the simulation lifetime.

---

### 3.6.3 Forced Refresh Handling

Perception System §4.5.2 warns that forced-refresh snapshots may arrive outside the
normal 10Hz heartbeat cadence — e.g., immediately after a goal, a corner kick set piece,
or a tackle. The DT must accept these without error.

**Handling rule:**

- A forced-refresh snapshot for agent N uses the same `ReceiveSnapshot()` method
  signature. No separate interface exists for forced refresh.
- On forced refresh, the DT re-evaluates **that agent only**. The other 21 agents are
  not re-evaluated.
- If the agent is currently in `EXECUTING` state (a dispatched action is in progress),
  the forced refresh does not cancel the in-flight execution. The DT evaluates fresh
  options, compares the selected action with the in-flight action, and dispatches a new
  command only if the selected action differs.
- If the in-flight action and the re-evaluated action are identical `ActionType`, the DT
  does not re-dispatch. Redundant dispatch of the same action type is suppressed to
  prevent thrashing in execution systems.

**Frame counter on forced refresh:** The orchestrator must use the current simulation
frame number when calling `ReceiveSnapshot()` for a forced refresh, not the tick-boundary
frame number. This ensures the noise hash in §3.3.3 produces a distinct result for the
forced refresh evaluation.

---

### 3.6.4 Snapshot Lifetime Constraint

This constraint restates AR-3 from the outline as an implementation requirement.

> **The DT must not retain any reference to snapshot data beyond the scope of
> `ReceiveSnapshot()`.** All data needed for the pipeline must be copied into
> `DecisionContext` (Step 2, §2.1.2) during the method call. After `ReceiveSnapshot()`
> returns, the snapshot copy is invalid from the DT's perspective.

**Rationale:** Even though the snapshot is passed by value (§3.6.1), it may contain
`ReadOnlySpan<T>` fields pointing into the Perception System's per-tick buffer.
Retaining a reference to the `PerceptionSnapshot` struct does not extend the lifetime
of those spans. After the Perception System tick buffer is reused for the next tick,
any retained span reference becomes a dangling reference — a silent data corruption.

**Enforcement:** The §5 integration tests include a test (IT-DT-AR3) that verifies the
DT does not store any `PerceptionSnapshot` field in a class-level variable accessible
outside `ReceiveSnapshot()`. This must be a static analysis check in CI, not just a
runtime test.

---

## 3.7 State Machine

### 3.7.1 States

The Decision Tree maintains a per-agent state machine. Each of the 22 agents has its own
independent `DtState` value; no shared state machine exists across agents.

| State | Meaning | Entry Condition |
|---|---|---|
| `IDLE` | No action in progress. Agent awaits the next heartbeat. | Initial state; post-action-completion; post-interrupt recovery. |
| `EVALUATING` | Pipeline running. Steps 1–6 executing inside `ReceiveSnapshot()`. | Heartbeat tick received; `ReceiveSnapshot()` called by orchestrator. |
| `EXECUTING` | Action dispatched. Execution system is performing the action. | `DispatchAction()` completed successfully (Step 6 returned). |
| `INTERRUPTED` | Execution cancelled by external event. Transitioning to IDLE. | `TackleContactEvent` or forced-refresh-with-new-action during EXECUTING. |

**IDLE vs EXECUTING distinction:** An agent in `IDLE` has no dispatched action. An agent
in `EXECUTING` has a dispatched action that the execution system is running (e.g., pass
windup in progress). At Stage 0, HOLD and MOVE_TO_POSITION are continuous — they are
re-dispatched every heartbeat tick. PASS and SHOOT have windup/contact phases that span
multiple simulation frames; the DT remains in EXECUTING across those frames.

---

### 3.7.2 Transition Table

| From | To | Trigger | Guard | Action on Transition |
|---|---|---|---|---|
| `IDLE` | `EVALUATING` | Heartbeat tick received (`ReceiveSnapshot()` called) | None | Begin Step 1 of pipeline |
| `EVALUATING` | `EXECUTING` | `DispatchAction()` completes (Step 6 returns) | Action dispatched to an execution system | Publish `DecisionMadeEvent`; record `DispatchedActionType` |
| `EVALUATING` | `IDLE` | `DispatchAction()` produces HOLD (no execution system engaged) | HOLD selected or fallback-to-HOLD (FR-08) | Publish `DecisionMadeEvent` with `FallbackToHold = true` if applicable |
| `EXECUTING` | `IDLE` | Execution system confirms action complete | Completion signal received (stub at Stage 0) | Clear `DispatchedActionType` |
| `EXECUTING` | `IDLE` | Next heartbeat tick — continuous actions (HOLD, MOVE) | `ActionType` in {HOLD, MOVE_TO_POSITION} | Re-dispatch same or new action type; re-enter EVALUATING then EXECUTING |
| `EXECUTING` | `INTERRUPTED` | `TackleContactEvent` received from Collision System | Agent is in EXECUTING and tackle flag set | Cancel in-flight action signal to execution system |
| `EXECUTING` | `INTERRUPTED` | Forced refresh produces different ActionType | Forced refresh re-evaluates to different action | Cancel in-flight action; prepare new dispatch |
| `INTERRUPTED` | `IDLE` | One frame elapsed after interrupt | — | Clear interrupt flag; clear `DispatchedActionType` |

**Continuous action re-dispatch note:** HOLD and MOVE_TO_POSITION are logically
continuous but architecturally single-tick. Every heartbeat, an agent selecting HOLD
transitions EVALUATING → EXECUTING → IDLE → EVALUATING in sequence. This appears as
a rapid cycle but is correct — the agent re-evaluates each tick and re-issues the
movement command. The execution system (Agent Movement) receives a repeated HOLD command
each tick, which is idempotent from its perspective.

PASS and SHOOT are not re-dispatched on each tick. Once PASS enters WINDUP state in
Pass Mechanics, the DT is in EXECUTING until the execution system signals completion
(or the DT receives an interrupt). The DT does not re-evaluate during PASS execution
unless a forced refresh arrives.

---

### 3.7.3 Transition Invariants

1. **No direct IDLE → EXECUTING transition.** Every dispatch must pass through EVALUATING.
   EVALUATING is the only state in which pipeline logic runs.

2. **INTERRUPTED has a one-frame lifetime.** The DT never remains in INTERRUPTED for more
   than one simulation frame. INTERRUPTED → IDLE is automatic; no external signal is
   required. This ensures the agent returns to evaluation within one frame of any interrupt.

3. **No simultaneous multi-state condition.** Each agent has exactly one `DtState` at
   any moment. The state machine is fully deterministic given the same input sequence.

4. **EVALUATING is not externally visible.** External systems (Collision System, Pass
   Mechanics) do not query DT state. The only externally relevant states are IDLE and
   EXECUTING: IDLE means "no dispatched action" and EXECUTING means "action in progress."
   EVALUATING and INTERRUPTED are internal transient states.

---

### 3.7.4 State Machine Diagram

```
                    ┌─────────────────────────────────────────────┐
                    │         Heartbeat tick received              │
                    │       (ReceiveSnapshot() called)             │
                    ▼                                              │
              ┌──────────┐                                         │
   ──────────►│   IDLE   │◄────────────────────────────────────────┤
   (initial   └──────────┘   Execution complete / HOLD dispatched  │
    state)         │                                               │
                   │ [always]                                      │
                   ▼                                               │
            ┌────────────┐                                         │
            │ EVALUATING │                                         │
            └────────────┘                                         │
                   │                                               │
          ┌────────┴────────┐                                      │
          │                 │                                      │
     [HOLD/fallback]   [PASS/SHOOT/DRIBBLE/                        │
          │             MOVE/PRESS/INTERCEPT]                      │
          │                 │                                      │
          ▼                 ▼                                      │
        IDLE◄──────── ┌──────────┐ ◄── Tackle / forced refresh ───┤
                      │EXECUTING │                                  │
                      └──────────┘                                 │
                            │                                      │
                     [Tackle / forced                              │
                      refresh w/ new                              │
                      ActionType]                                  │
                            │                                      │
                            ▼                                      │
                     ┌─────────────┐                               │
                     │ INTERRUPTED │──── (1 frame) ───────────────►┘
                     └─────────────┘
```

---

## 3.8 Edge Cases

### 3.8.1 Edge Case Catalogue

The following edge cases define expected Decision Tree behaviour under abnormal but
foreseeable conditions. Each must be handled without requiring external intervention.

---

**EC-DT-01 — Agent has possession but no visible teammates and not in shooting range.**

> Condition: `DecisionContext.HasPossession = true`;
> `PerceptionSnapshot.VisibleTeammateCount = 0`; `GoalOpeningScore < SHOOT_MIN_THRESHOLD` [GT].
>
> Expected behaviour: `GenerateOptions()` §3.1 generates DRIBBLE and HOLD candidates
> (possession-available actions that do not require a visible target). PASS and SHOOT
> are not generated (no teammates / goal not viable). DRIBBLE utility depends on
> `SpaceScore`; HOLD utility is always generated at `BaseUtility = 0.25`. The agent
> dribbles if space exists, holds if under pressure.
>
> This is not a failure mode. A striker isolated at the back with no open passing option
> should hold or dribble. The utility model produces this outcome without special handling.

---

**EC-DT-02 — Agent in penalty box with ball; goal visible.**

> Condition: `FieldZone == ATTACKING`; `HasPossession = true`; goal visible in snapshot;
> `DistanceToGoal ≤ PENALTY_BOX_DIST` [GT] ≈ 18m.
>
> Expected behaviour: SHOOT `BaseUtility` uses the `ATTACKING` zone multiplier (1.0 in
> zone table, §3.2). `GoalOpeningScore` is high if goalkeeper is not well-positioned.
> SHOOT utility should dominate unless pressure is very high (`P ≥ 0.8`) and a clear
> pass lane exists (high-Vision teammate in space). This is the intended behaviour —
> agents in the box should shoot.
>
> Validation: Balance test BAL-DT-02 (§5.7) verifies that under A_Finishing ≥ 12,
> A_Composure ≥ 10, `GoalOpeningScore ≥ 0.6`, and `P ≤ 0.4`, SHOOT is selected as the
> highest-utility action in ≥ 80% of simulated heartbeats.

---

**EC-DT-03 — All opponents out of visible range (agent perceives no opponents).**

> Condition: `PerceptionSnapshot.VisibleOpponentCount = 0`.
>
> Expected behaviour: `PressureScalar P = 0.0` (no visible pressure). Risk penalties
> are minimised. MOVE_TO_POSITION urgency based on formation slot distance dominates
> for out-of-possession agents (no pressing opportunity exists with zero visible
> opponents). For in-possession agents, PASS and DRIBBLE risk penalties drop to their
> minimums, making progressive actions more viable.
>
> This scenario occurs legitimately for deep midfielders in wide formations or agents
> near the pitch boundary. No special handling required — the utility model responds
> naturally to P = 0.

---

**EC-DT-04 — `PerceptionSnapshot.BallVisible = false`.**

> Condition: Agent cannot perceive the ball (outside field of view; occluded).
>
> Expected behaviour: All possession-dependent actions (PASS, SHOOT, DRIBBLE, HOLD,
> INTERCEPT) are either not generated (PASS/SHOOT/DRIBBLE — require active possession
> or ball visibility) or scored at minimum utility (INTERCEPT — no intercept point
> derivable). MOVE_TO_POSITION and PRESS are not ball-dependent. The agent falls back
> to MOVE_TO_POSITION or PRESS depending on `PossessionState` and pressing instruction.
>
> This is correct: an agent who cannot see the ball should reposition, not attempt to
> intercept or pass. The utility model handles this without special-case branching.

---

**EC-DT-05 — Forced refresh snapshot arrives mid-heartbeat (agent already EXECUTING).**

> Condition: `ReceiveSnapshot()` is called by the orchestrator while the agent is in
> EXECUTING state (pass windup in progress; forced refresh triggered by goal or set piece).
>
> Expected behaviour: per §3.6.3 — DT re-evaluates. If the re-evaluated action is a
> different `ActionType` than the in-flight action: transition to INTERRUPTED, cancel
> in-flight execution, dispatch new action. If the re-evaluated action is the same
> `ActionType`: suppress re-dispatch; continue current execution.
>
> The in-flight execution cancellation signal is a documented responsibility of this
> specification. The mechanism (method call vs. shared flag) is an implementation
> detail deferred to §4.

---

**EC-DT-06 — Two agents both select PRESS on the same opponent (same-tick).**

> Condition: Agents A and B both evaluate at the same heartbeat tick and both select
> PRESS targeting opponent C.
>
> Expected behaviour: Both agents issue `MovementCommand` PRESS commands targeting
> opponent C's last-known position. Agent Movement (#2) and the Collision System (#3)
> are responsible for resolving physical convergence on the same space. The Decision
> Tree does not attempt to coordinate PRESS assignments between agents — this is
> explicitly out of scope at Stage 0 (Formation System, Stage 1+).
>
> This produces occasional double-pressing which is realistic: real teams sometimes send
> two players to the same ball. Stage 1 Formation System will add press-assignment
> coordination.

---

**EC-DT-07 — Agent with `Composure = 1` under maximum pressure (P = 1.0).**

> Condition: Lowest possible Composure attribute (1/20); PressureScalar `P = 1.0`.
>
> Expected behaviour: `ComposureNoiseBound` reaches its maximum (NOISE_MAX = 0.20,
> §3.3.4). `EffectiveUtility` values are significantly perturbed from `ScoredUtility`.
> The agent may select a suboptimal action — for example, SHOOT from a low-probability
> position instead of PASS to an unmarked teammate.
>
> This is **correct behaviour, not a bug** (outline §3.8 and KD-3 §1.4). Low-Composure
> agents under extreme pressure are supposed to make poor decisions. The noise model
> exists to produce this differentiation. If the outcome appears pathological (SHOOT
> from own half), balance tests BAL-DT-03 and BAL-DT-04 (§5.7) will catch it by
> verifying that AR-4 noise bounds prevent catastrophic choices even at minimum Composure.

---

**EC-DT-08 — `GenerateOptions()` returns zero candidates.**

> Condition: All seven action types fail their availability gates in §3.1.
>
> Expected behaviour: FR-08 fallback (§2.3.4) — produce `AgentAction` with
> `ActionType = HOLD`, `BaseUtility = 0.0`. Log as failure mode FM-DT-08.
> Publish `DecisionMadeEvent` with `FallbackToHold = true`.
>
> This should not occur in normal play — HOLD and MOVE_TO_POSITION are always generated
> by §3.1 (they have no hard availability gate). Zero candidates requires HOLD generation
> to also fail, which requires `HasPossession = false` AND `PossessionState == OWN_TEAM`
> (the only condition under which HOLD is suppressed). This combination should not arise
> from valid match state. If FM-DT-08 fires in play, it indicates a bug in §3.1 option
> generation, not a valid edge case. It must be investigated and logged.

---

### 3.8.2 Edge Cases Deliberately Not Handled

The following scenarios are explicitly outside Decision Tree scope at Stage 0. They are
listed to prevent implementers from attempting to handle them here.

| Scenario | Owner | Stage |
|---|---|---|
| Press assignment coordination (no two agents pressing same target) | Formation System Spec (Stage 1) | Stage 1 |
| Goalkeeper specialist decision logic (dive, punch, distribute) | Goalkeeper Mechanics Spec (Stage 1) | Stage 1 |
| Set piece execution (corner, free kick, throw-in) | Set Piece System (Stage 2) | Stage 2 |
| Off-ball run scheduling (overlap, third-man, underlap) | Formation System / Run Coordination (Stage 1) | Stage 1 |
| Communication between agents (signalling for the ball) | Agent Communication System (Stage 2) | Stage 2 |
| Fatigue-based decision degradation beyond Composure model | Agent Attributes Extension (Stage 3) | Stage 3 |

---

## Section 3 Completion Summary

With §3.4 through §3.8 delivered, Section 3 of Decision Tree Specification #8 is now
complete. The full six-step pipeline is specified:

| Step | Location | Status |
|---|---|---|
| Step 1 — Validate snapshot | §3.1 (Section 3.1 v1.1) | ✅ Complete |
| Step 2 — Assemble DecisionContext | §2.1.2, §2.2.4 (Section 2 v1.1) | ✅ Complete |
| Step 3 — Generate options | §3.1 (Section 3.1 v1.1) | ✅ Complete |
| Step 4 — Score options | §3.2 (Section 3.2 v1.2) + §3.4 (this file) | ✅ Complete |
| Step 5 — Select action | §3.3 (Section 3.3 v1.0) | ✅ Complete |
| Step 6 — Dispatch action | §3.5 (this file) | ✅ Complete |
| Intake interface | §3.6 (this file) | ✅ Complete |
| State machine | §3.7 (this file) | ✅ Complete |
| Edge cases | §3.8 (this file) | ✅ Complete |

**New items logged:**
- ERR-011: `PassRequest.AgentID` (uppercase) vs `ShotRequest.AgentId` (mixed case) —
  naming inconsistency. Non-blocking. Should be harmonised before implementation.
- XC-3.5-10: `MovementController.SubmitCommand()` method name requires verification
  against Agent Movement §4 before §3.5 approval.

**Constant count (this file):** 16 §3.4 tactical constants + 7 §3.5 dispatch/movement
constants (`URGENCY_PRESSURE_SCALE`, `SPIN_INTENT_BELOW_CENTRE`, `SPIN_INTENT_OFF_CENTRE`,
`PLACEMENT_CORNER_OFFSET`, `MOVE_SPRINT_THRESHOLD`, `MOVE_JOG_THRESHOLD`, and
`PRESS_URGENCY_FACTOR` already counted in §3.4) = **23 new [GT] constants**, all in
`TacticalWeights.cs` or `UtilityWeights.cs` as indicated in §3.4.7.

**Next:** Section 4 — Architecture and Integration.

---

*End of Section 3.4 through 3.8 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*  
*v1.0 — March 05, 2026, 12:00 PM PST*
