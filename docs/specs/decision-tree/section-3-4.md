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
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist)  
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

**Open Items (non-blocking integration tracking; not approval blockers):**
- `[ERR-007-TRACKED]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes` in Agent Movement §3.5.6. Amendment AM-002-001 adds them.
  Affects §3.5.3 weak foot determination logic (noted inline). Downgraded from
  PENDING to TRACKED on April 29, 2026: amendment is authored and consumed
  read-only by DT; resolution is bookkeeping in the Agent Movement spec, not a
  contract change. Tracked in `spec-error-log.md` and CLAUDE.md Open Issues.
- `[ERR-008-TRACKED]` — `PossessingAgentId` design (Option A vs B) unresolved.
  Does not affect Decision Tree dispatch; affects Pass Mechanics and Shot Mechanics
  possession validation only. Referenced inline for context. Downgraded from PENDING
  to TRACKED on April 29, 2026: outside DT scope; resolved in Pass/Shot specs at
  implementation time.

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

