# Pressing AI Specification #13 — Section 3: Core Formulas and Algorithms

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

This section publishes the per-tick computation pipeline. Every
formula carries units, valid input ranges, and at least one worked
example (FR-PR-034 / CLAUDE.md "When Writing or Editing Specs").

The per-tick pseudocode is in §3.11; §3.1–§3.10 define each step.

## 3.1 Trigger Detection

Four canonical triggers (KD-7). Each produces a boolean flag plus a
trigger-origin `EntityId` (the ball-carrier or the candidate
receiver). All thresholds are `[GT]` (designer-tunable, FR-PR-041);
they are listed in §6.1 and inlined here for clarity.

### 3.1.1 `BAD_TOUCH` (source: First Touch #4 §3.1 / §3.5)

```
trigger.BAD_TOUCH =
    (touch.q < BAD_TOUCH_THRESHOLD)
    AND (||touch.postTouchBallVelocity|| > BAD_TOUCH_VELOCITY_M_S)
```

`BAD_TOUCH_THRESHOLD = 0.40 [GT]` — below 0.4 the touch is
classified loose by #4's own taxonomy; #13 piggybacks on that
boundary. `BAD_TOUCH_VELOCITY_M_S = 4.0 m/s [GT]` — the
post-touch escape velocity above which the loose ball is
recoverable by an alert presser.

**Trigger origin:** the agent who attempted the touch (current
ball-carrier as of the touch).

### 3.1.2 `BACKWARD_PASS` (source: Pass Mechanics #5 §2 FR-08)

```
e = mostRecentPassAttemptEventThisTick
trigger.BACKWARD_PASS =
    (e != null)
    AND (dot(normalize(e.passVelocity.xy), attackingDirection) < BACKWARD_PASS_THRESHOLD)
```

`BACKWARD_PASS_THRESHOLD = -0.30 [GT]` — dimensionless dot-product
threshold; values more negative than -0.3 are clearly retreating
passes. The `attackingDirection` unit vector is supplied by the
orchestrator (own goal → opponent goal in pitch X).

**Trigger origin:** the receiver `EntityId` named in
`PassAttemptEvent` (or the passer if the receiver field is null —
e.g., an aimless clearance).

**Worked example.** Own team attacks `+X`; `attackingDirection =
(+1, 0)`. Pass kick velocity `(−6.0, 3.0, 0)` m/s. Horizontal unit
vector `(−6, 3)/||(−6,3)|| = (−0.894, 0.447)`. Dot with `(+1, 0)`
is `−0.894`. `−0.894 < −0.30` → trigger fires.

### 3.1.3 `SIDELINE_TRAP` (source: Ball Physics #1 §1.2 + #7)

```
yToBottom = ball.y                          // distance to y=0 touchline
yToTop    = PITCH_WIDTH_M - ball.y          // distance to y=68 touchline
nearSide  = min(yToBottom, yToTop)
sidelineDir = (yToBottom < yToTop) ? (0,-1) : (0,+1)
carrierFacingDot = dot(ballCarrier.facing, sidelineDir)

trigger.SIDELINE_TRAP =
    (nearSide < SIDELINE_TRAP_DISTANCE_M)
    AND (carrierFacingDot > 0)
```

`SIDELINE_TRAP_DISTANCE_M = 8.0 m [GT]` — outside the conventional
"trap zone" hugging the touchline. `PITCH_WIDTH_M = 68.0 [FIXED]`
cited from Ball Physics #1 §1.2 (`XC-013-001`).

**Trigger origin:** the ball-carrier.

**Worked example.** Ball at `(45.0, 5.0)`, carrier facing
`(0.5, −0.87)` (south-east, toward the y=0 touchline). `yToBottom
= 5.0 < SIDELINE_TRAP_DISTANCE_M = 8.0`. `sidelineDir = (0, −1)`.
`carrierFacingDot = −0.87 × −1 = 0.87 > 0`. Trigger fires.

### 3.1.4 `WEAK_RECEIVER` (source: Perception #7 §3.7–§3.10)

For each candidate receiver `r` in the carrier's pass range
(visible per #7, opponent of the ball-carrier's team — wait,
candidate receivers are **teammates** of the ball-carrier; #13 is
the *defending* team scanning the *attacker's* options):

```
candidates = perception.visibleOpponents             // from #13's POV
for each r in candidates where r != ballCarrier && r != opposingGK {
    if (r.attribute.FirstTouch < WEAK_RECEIVER_THRESHOLD
        AND r.perceivedPressure   >= WEAK_RECEIVER_PRESSURE) {
        trigger.WEAK_RECEIVER = true
        weakReceiverList.Add(r.entityId)
    }
}
```

`WEAK_RECEIVER_THRESHOLD = 10 [GT]` (FirstTouch is a 1–20 attribute
scale). `WEAK_RECEIVER_PRESSURE = 0.50 [GT]` (perceived local
pressure scalar, 0–1).

**KD-13 enforcement:** the opposing goalkeeper is excluded from
candidate sets here. A pass back to the GK is captured by
`BACKWARD_PASS` (§3.1.2) instead, and the GK never becomes a
press target.

**Trigger origin:** the weakest qualifying receiver (lowest
`FirstTouch`); EntityId ascending as terminal tie-break.

### 3.1.5 One-Tick Latency by Design

Triggers fire on the tick **after** the originating event is
visible in the #7 perception snapshot. Perception filtering
already enforces this for opponent-side events; #13 inherits the
latency without adding its own.

## 3.2 Trigger Debounce (Hysteresis)

Each trigger flag is held for `TRIGGER_DWELL_TICKS [EST]`
consecutive ticks before firing the press; held for
`TRIGGER_RELEASE_TICKS [EST]` after the upstream condition clears
before considering the trigger "off". Binding to Agent Movement #2
§3.1 (KD-9). #13 does NOT define a new algorithm — it parameterises
the #2 pattern.

```
foreach (flag in TriggerFlags) {
    if (rawTriggerCondition[flag]) {
        dwellCounter[flag] = min(dwellCounter[flag] + 1, TRIGGER_DWELL_TICKS)
        releaseCounter[flag] = 0
    } else {
        releaseCounter[flag] = min(releaseCounter[flag] + 1, TRIGGER_RELEASE_TICKS)
        if (releaseCounter[flag] >= TRIGGER_RELEASE_TICKS)
            dwellCounter[flag] = 0
    }
}
committedFlag[flag] = (dwellCounter[flag] >= TRIGGER_DWELL_TICKS)
```

`TRIGGER_DWELL_TICKS = 2 [EST]` (200 ms) — long enough to filter
single-tick noise; short enough to feel responsive.
`TRIGGER_RELEASE_TICKS = 3 [EST]` (300 ms) — asymmetric (longer
release than commit) so a press, once committed, does not
oscillate against a brief release.

## 3.3 Primary-Press Selection

Of all eligible agents, select the one whose post-displacement
cost is lowest. EntityId terminal tie-break.

**Eligibility (intersection of three constraints):**

1. Stamina ≥ `PRESS_STAMINA_MINIMUM` per #8 §3.1.8.1 (cite-not-redefine).
2. Stamina ≤ `PRESS_FATIGUE_CEILING [GT]` (FR-PR-029; #13-added).
3. Within `PRESS_TRIGGER_DISTANCE` of the ball-carrier per #8
   §3.1.8.2 (cite-not-redefine; #8 uses 8.0 m).
4. Not the goalkeeper (KD-13 / FR-PR-017).
5. Own team is the defending side this tick (phase ≠ `InPoss`).

**Cost function:**

```
projInterceptionPoint = ballCarrier.pos + ballCarrier.velocity * INTERCEPT_LOOKAHEAD_TICKS * DT_TACTICAL
cost(a) = || a.pos - projInterceptionPoint ||²
```

`INTERCEPT_LOOKAHEAD_TICKS = 3 [EST]` (300 ms).
`DT_TACTICAL = 0.10 s [DERIVED]` from CLAUDE.md tick rate.
`primaryPress = argmin_a cost(a)`; EntityId ascending if `|cost(i)
− cost(j)| < SPACING_EPSILON_M2` per KD-9 / KD-14 reuse.

**Worked example.** Ball-carrier at `(40, 30)` with velocity
`(3, 0)`. `projInterceptionPoint = (40 + 3×3×0.10, 30) = (40.9,
30.0)`. Eligible defenders A=`(38, 31)`, B=`(42, 32)`.
`cost(A) = 2.9² + 1² = 9.41`; `cost(B) = 1.1² + 2² = 5.21`.
Primary = B.

## 3.4 Cover-Shadow Selection

Candidate receivers = opponents within
`COVER_SHADOW_CANDIDATE_RADIUS_M [GT]` of the ball-carrier and
visible per #7, excluding the opposing GK (KD-13).

For each candidate `r`, compute the shadow-lane position
(§3.5). Then for each eligible defender `d` (same five
eligibility conditions as §3.3, minus the primary-press selection)
compute:

```
coverCost(d, r) = || d.pos - shadowLane(r) ||²
```

Assign cover shadows **greedily** in order of descending
threat-score on `r`:

```
threatScore(r) =
    receiverProgressionGain(r) * THREAT_PROGRESSION_W
  + (1 - r.perceivedPressure)  * THREAT_OPEN_W
  + (r.attribute.FirstTouch /20) * THREAT_SKILL_W
```

`THREAT_PROGRESSION_W = 0.50 [GT]`, `THREAT_OPEN_W = 0.30 [GT]`,
`THREAT_SKILL_W = 0.20 [GT]`. `receiverProgressionGain(r)` is the
forward component of `(r.pos − ballCarrier.pos)` along
`attackingDirection`, clamped to `[0, 1]` after normalising by
half the pitch length.

For each `r` in descending threat order, up to
`MAX_COVER_SHADOWS [GT]` slots: pick the lowest-`coverCost` eligible
defender not already assigned. EntityId ascending as tie-break.

Any unfilled slot demotes to `HOLD_SHAPE` (F4 / FR-PR-038).

**Anti-chaos pre-check:** before committing a cover-shadow
assignment, evaluate KD-16 invariants (§3.9). Assignments that
would violate are rejected and the slot demotes to `HOLD_SHAPE`.

## 3.5 Cover-Shadow Lane Geometry

```
shadowLane(r) = lerp(ballCarrier.pos, r.pos, COVER_SHADOW_LANE_FRACTION)
              = ballCarrier.pos + (r.pos - ballCarrier.pos) * COVER_SHADOW_LANE_FRACTION
```

`COVER_SHADOW_LANE_FRACTION = 0.55 [GT]` — slightly past the
midpoint toward the receiver, biasing for interception over angle
denial. (At exactly 0.50 the shadow merely blocks line-of-sight;
at 0.55 the shadow is positioned to step into the pass on
release.)

**Worked example (outline reference).** Ball-carrier at `(60, 30)`,
receiver at `(75, 40)`.

```
delta = (75-60, 40-30) = (15, 10)
shadowLane = (60, 30) + (15, 10) × 0.55
           = (60, 30) + (8.25, 5.5)
           = (68.25, 35.5)
```

Matches outline §3.5. The shadow sits 9.0 m behind the receiver
along the pass line.

Additional configurations are worked in Appendix C (vertical,
diagonal, horizontal carrier+receiver geometries).

## 3.6 Role Transitions (Hysteresis)

A role transition for agent `a` from `lastRole[a]` to a new
candidate role commits only after the new candidate has been
preferred for `ROLE_DWELL_TICKS [EST]` consecutive ticks. Prevents
role-thrash when two cost candidates are near-equal.

```
candidate = roleFromSelection(a)             // from §3.3 / §3.4
if (candidate == lastRole[a]) {
    roleDwellTicks[a] = 0                    // stable
    committedRole[a] = lastRole[a]
} else {
    roleDwellTicks[a] += 1
    if (roleDwellTicks[a] >= ROLE_DWELL_TICKS) {
        committedRole[a] = candidate
        lastRole[a] = candidate
        roleDwellTicks[a] = 0
    } else {
        committedRole[a] = lastRole[a]       // hold for now
    }
}
```

`ROLE_DWELL_TICKS = 3 [EST]` (300 ms).

**Worked example.** A has `lastRole = COVER_SHADOW`; selection
proposes `PRIMARY_PRESS` for two ticks, then back to
`COVER_SHADOW`. With `ROLE_DWELL_TICKS = 3`, `roleDwellTicks`
reaches 2, then resets when the candidate matches `lastRole`
again. A never transitions — exactly the thrash-suppression
property the dwell counter exists for.

## 3.7 Stamina Costs

Per-tick fatigue accumulation by role:

```
if (role[a] == PRIMARY_PRESS) fatigue[a] += STAMINA_COST_PRIMARY_PER_TICK
if (role[a] == COVER_SHADOW)  fatigue[a] += STAMINA_COST_SHADOW_PER_TICK
// HOLD_SHAPE: no additional cost from #13; #2's locomotion model still applies
```

`STAMINA_COST_PRIMARY_PER_TICK = 0.0040 [GT]` (≈ 0.04 / s at
10 Hz; full-effort closing for 25 s consumes ~0.1 fatigue).
`STAMINA_COST_SHADOW_PER_TICK = 0.0020 [GT]` (≈ half of primary).

**Fatigue ceiling (FR-PR-029):** an agent with `fatigue[a] ≥
PRESS_FATIGUE_CEILING = 0.85 [GT]` is excluded from press roles
this tick. `PRESS_FATIGUE_CEILING` layers on top of #8 §3.1.8.1's
`PRESS_STAMINA_MINIMUM = 0.20` (where stamina is the complement of
fatigue; see CLAUDE.md fatigue convention). #13 does not redefine
either — it cites both.

**Worked example.** Agent A has `fatigue = 0.84`. Selection
proposes `PRIMARY_PRESS`. Ceiling check: `0.84 < 0.85` → eligible
this tick. After accumulation: `fatigue = 0.844`. Next tick:
`0.844 < 0.85` → still eligible. Two ticks later
(`fatigue = 0.852`): exceeds ceiling → demoted to `HOLD_SHAPE`,
selection re-runs for primary.

## 3.8 Disengage and Reset

Two disengage conditions, evaluated per tick after primary +
cover-shadow selection:

**(a) Timeout disengage:**

```
if (no committed trigger flag is set this tick) {
    disengageDwellTicks += 1
    if (disengageDwellTicks >= DISENGAGE_TIMEOUT_TICKS) {
        directive = AllHoldShape()
        resetCooldownTicks = RESET_LATENCY_TICKS
        disengageDwellTicks = 0
    }
} else {
    disengageDwellTicks = 0
}
```

`DISENGAGE_TIMEOUT_TICKS = 8 [GT]` (800 ms).

**(b) Zone disengage (immediate, no dwell):**

```
ballX = ball.position.x  (own attacking orientation)
if (ballX < PRESS_ZONE_X_MIN || ballX > PRESS_ZONE_X_MAX) {
    directive = AllHoldShape()
    resetCooldownTicks = RESET_LATENCY_TICKS
}
```

`PRESS_ZONE_X_MIN = 35.0 m [GT]`, `PRESS_ZONE_X_MAX = 105.0 m
[GT]` — high-press default eligible-zone. Lower-block styles will
shift the window at Stage 1+ via team-instruction parameters
(§7.2). The polygon is rectangular at Stage 0; arbitrary polygons
are Stage 1+.

**Reset (FR-PR-032):**

```
if (resetCooldownTicks > 0) {
    directive = AllHoldShape()
    resetCooldownTicks -= 1
}
```

`RESET_LATENCY_TICKS = 12 [GT]` (1.2 s).

**Worked example.** Press fires at tick T. Triggers clear at tick
T+5. `disengageDwellTicks` increments T+5..T+12; at T+12 reaches
8 → disengage fires, all-`HOLD_SHAPE` emitted, reset cooldown
loads 12. T+13..T+24: all-`HOLD_SHAPE` regardless of new triggers
(reset suppression). T+25: cooldown clear; new triggers may fire.

## 3.9 Anti-Chaos Invariant Enforcement (KD-16)

Three invariants. Applied in order; on violation, the
lowest-priority assignment demotes to `HOLD_SHAPE` and the check
re-runs. After at most `MAX_COVER_SHADOWS + 1 = 3` iterations the
set is clean (because each demotion strictly reduces the
violating count). If a primary-press demotion is required to
satisfy an invariant, the entire directive falls back to
all-`HOLD_SHAPE` per F5 / FR-PR-039.

**(1) Max pressers per ball-side third (FR-PR-018):**

```
ballSideThird = ballPosToThird(ball.position.x, attackingDirection)
presserCount  = count(a where role[a] in {PRIMARY_PRESS, COVER_SHADOW}
                       AND positionInThird(a) == ballSideThird)
if (presserCount > MAX_PRESSERS_BALL_THIRD) violation
```

`MAX_PRESSERS_BALL_THIRD = 3 [GT]`.

**(2) Backline floor (FR-PR-019):**

```
backlineCount = count(a where #12.line[a] == Defense
                          AND positionInThird(a) == ownDefensiveThird)
if (backlineCount < MIN_BACKLINE_AGENTS) violation
// promotion-blocked: a Defense-line agent who would drop the count below
// the floor cannot transition into PRIMARY_PRESS or COVER_SHADOW
```

`MIN_BACKLINE_AGENTS = 3 [GT]`.

**(3) Displacement cap (FR-PR-020):**

```
for each a where role[a] == COVER_SHADOW:
    displacement = || targetPosition[a] - baselineFormationSlot[a] ||
    if (displacement > MAX_PRESS_DISPLACEMENT_M) violation
    // → reject this cover-shadow assignment; demote to HOLD_SHAPE
```

`MAX_PRESS_DISPLACEMENT_M = 25.0 m [GT]`.

**Demotion priority order (lowest demotes first):**

```
1. Highest-cost COVER_SHADOW assignment
2. ...
3. Last-remaining COVER_SHADOW assignment
4. PRIMARY_PRESS (last resort; triggers F5 fallback)
```

## 3.10 Constants Catalogue (Forward Reference to §6.1)

All constants used by §3.0–§3.9 are catalogued in §6.1 with tags
and source-of-truth references. They live in
`src/PressingAI/PressingAIConstants.cs` (KD-15, FR-PR-007).

## 3.11 Pseudocode — Per-Tick Main Loop

```
void PressingAITick(
    in PerceptionSnapshot perception,
    in PassEventRing passEvents,
    in PositioningAIView pos12,             // #12 baseline + phase + line membership
    Vector2 attackingDirection,
    ref RoleHysteresisState roleHyst,
    ref PressTrigger triggerState,
    out PressDirective directive,
    Span<PressAssignment> outAssignments)    // length 22
{
    // F1 — stale perception
    if (perception.tickIndex < currentTick) {
        directive    = prevDirective;
        outAssignments.CopyFrom(prevAssignments);
        return;
    }

    // KD-11 phase gate (FR-PR-033): no press from in-possession team
    if (pos12.Phase(team) == Phase.InPoss) {
        directive = AllHoldShape(team);
        WriteHoldShape(outAssignments, pos12);
        return;
    }

    // §3.8 reset cooldown
    if (roleHyst.resetCooldownTicks > 0) {
        roleHyst.resetCooldownTicks -= 1;
        directive = AllHoldShape(team);
        WriteHoldShape(outAssignments, pos12);
        return;
    }

    // §3.1 raw triggers — one-tick latency by design
    var raw = EvaluateRawTriggers(perception, passEvents, attackingDirection);

    // §3.1.2 F2 NaN guard — suppress affected trigger
    SuppressNaNTriggers(ref raw);

    // §3.2 debounce
    UpdateTriggerDebounce(raw, ref triggerState);
    var fired = triggerState.CommittedFlags();

    // §3.8 timeout disengage
    if (fired == TriggerFlags.None) {
        roleHyst.disengageDwellTicks += 1;
        if (roleHyst.disengageDwellTicks >= DISENGAGE_TIMEOUT_TICKS) {
            roleHyst.resetCooldownTicks = RESET_LATENCY_TICKS;
            roleHyst.disengageDwellTicks = 0;
            directive = AllHoldShape(team);
            WriteHoldShape(outAssignments, pos12);
            return;
        }
        // no disengage yet; still no press this tick
        directive = AllHoldShape(team);
        WriteHoldShape(outAssignments, pos12);
        return;
    }
    roleHyst.disengageDwellTicks = 0;

    // §3.8 zone disengage (immediate)
    if (BallOutsidePressZone(perception.ball, attackingDirection)) {
        roleHyst.resetCooldownTicks = RESET_LATENCY_TICKS;
        directive = AllHoldShape(team);
        WriteHoldShape(outAssignments, pos12);
        return;
    }

    // §3.3 primary-press selection
    EntityId? primary = SelectPrimaryPress(perception, pos12);

    // §3.4 cover-shadow selection
    var coverShadows = SelectCoverShadows(perception, pos12, primary);

    // §3.6 role hysteresis
    var roles = ApplyRoleHysteresis(primary, coverShadows, ref roleHyst);

    // §3.9 anti-chaos invariants
    if (!EnforceInvariants(ref roles, perception, pos12)) {
        // F5 / FR-PR-039: invariant violation cannot be resolved by
        // cover-shadow demotion → fall back to all-HOLD_SHAPE
        DevLog("PRESSING_INVARIANT_FALLBACK");
        directive = AllHoldShape(team);
        WriteHoldShape(outAssignments, pos12);
        return;
    }

    // §3.7 stamina accumulation (against final roles)
    AccumulateStamina(roles, ref perception.fatigue);

    // Publish
    directive = BuildDirective(team, primary, coverShadows, fired);
    WriteAssignments(outAssignments, roles, primary, coverShadows, pos12);
}
```

Function purity: deterministic over `(perception, passEvents,
pos12, attackingDirection, prevRoleHyst, prevTriggerState)`. The
two `ref` mutations (`roleHyst`, `triggerState`) are the only side
effects and are themselves authoritative simulation state under
#16 §3.2 (FR-PR-004).

## 3.12 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. §3.0–§3.11 published with worked examples per FR-PR-034. |
