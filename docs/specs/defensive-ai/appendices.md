# Defensive AI Specification #14 — Appendices

**Created:** May 17, 2026
**Last Updated:** May 18, 2026 (v0.3 — FAIL-4 fix: Appendix F glossary `DOMAIN_TAG_DEFENSIVE_AI` entry promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]`)
**Version:** 0.3
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## Appendix A — Derivations for [GT] Constants

This appendix is the formal derivation record for all 22 `[GT]` constants
in §6.1. Each entry constitutes the promotion from `[EST]` (outline stage)
to `[GT]` (section-file stage) per KD-13. All values live in
`DefensiveAIConstants.cs` at Stage 1.

---

### A.1 `MAN_MARK_CANDIDATE_RADIUS_M = 15.0 m`

**Rationale:** A standard 4-man backline covers Y = 0–68 m with four agents
spaced at roughly 17 m intervals. A 15 m scan radius lets each defender
detect an opponent in the adjacent zone. Reducing to 10 m risks missing
wide threats; increasing to 20 m risks triggering man-mark assignments
across the full width of the pitch (span ≈ 1.2 zones), degrading shape
coherence.

**Sensitivity:** ±5 m variation has low impact on typical compact-line scenarios;
high impact in wide-formation scenarios (3-5-2, 5-4-1 with deep wing-backs).
Per-archetype tuning is a Stage 2+ extension (§7.8).

**Stage 0 default:** 15.0 m.

---

### A.2 `RUNNER_VELOCITY_THRESHOLD_M_S = 3.0 m/s`

**Rationale:** A jogging tempo in football is approximately 3 m/s. Players
below this speed are walking, jogging in place, or decelerating — not making
a genuine run. Above 3 m/s a player is accelerating into space and represents
a time-sensitive interceptable threat. Elite sprint speeds are 9–11 m/s per
GPS match-tracking research; 3 m/s is a conservative lower bound for
"running" that avoids false positives on crowded midfield jostling.

**Sensitivity:** lowering to 2 m/s causes every player in loose possession to
qualify as an INTERCEPT_RUNNER candidate (false positive flood). Raising to
4 m/s misses early runs before the player reaches full pace.

**Stage 0 default:** 3.0 m/s.

---

### A.3 `LAST_MAN_BALL_BUFFER_M = 5.0 m`

**Rationale:** At the last man's typical closing speed (≈ 5 m/s), a 5 m
buffer provides approximately one 10 Hz tick of reaction time per tick
(5 m/s × 0.1 s = 0.5 m of ball travel per tick). The buffer therefore
represents approximately 10 ticks of warning before the ball reaches the
last man's feet — enough for the emergency INTERCEPT_RUNNER assignment
to take effect even accounting for `MARK_DWELL_TICKS = 4` (the emergency
override bypasses dwell, so the buffer is a safety margin rather than a
dwell accommodation).

**Sensitivity:** too small (< 3 m) → emergency fires too late; the ball may
already be past the last man before the assignment commits. Too large (> 8 m)
→ emergency fires during routine hold-up play deep in the half, repeatedly
disrupting shape without genuine through-ball threat.

**Stage 0 default:** 5.0 m.

---

### A.4 `LAST_MAN_OWN_HALF_MIN_X = 5.0 m`

**Rationale:** Prevents the last-man emergency from triggering when the ball
is within 5 m of the own goal-line (e.g., during a corner-kick scramble,
goal kick, or goalkeeper distribution). Within 5 m of the goal-line, the
goalkeeper's positioning (owned by #11) is the primary defensive response.
5 m ≈ the width of the 6-yard box, making it a natural threshold for
"goalkeeper territory."

**Sensitivity:** if set to 0 m, the emergency fires during every goalkeeper
possession — overwhelming the overlay. If set to 20 m+, it masks genuine
counter-attack threats in the penalty area approach.

**Stage 0 default:** 5.0 m.

---

### A.5 `OFFSIDE_BALL_SPEED_THRESHOLD_M_S = 4.0 m/s`

**Rationale:** At 4.0 m/s the ball covers 0.4 m per 10 Hz tick. The
defensive line, advancing at a brisk walking pace (1.5–2.0 m/s), advances
0.15–0.20 m per tick. A ball moving at 4 m/s therefore widens the gap
between the line and the ball at roughly 0.2–0.25 m per tick — meaning
stepping the line behind such a ball is very dangerous. A ball at < 4 m/s
is slow enough that the step-up can keep pace with the ball while it is
in flight.

**Sensitivity:** raising to 6 m/s permits traps behind medium-paced through
balls (exploit risk: EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY). Lowering to 2 m/s
restricts traps to nearly dead-ball situations only (low tactical value).

**Stage 0 default:** 4.0 m/s.

---

### A.6 `OFFSIDE_STEP_SIZE_M = 3.0 m`

**Rationale:** A 3 m advance per trap execution is observable as a deliberate
coordinated step (≈ one long stride). It is meaningful enough to catch a
flat-footed attacker offside but small enough to remain recoverable if the
trap is beaten. With the `OFFSIDE_RESET_COOLDOWN_TICKS = 10` cooldown, the
maximum step-rate is 3 m per second (3 m × 10 Hz / 10 ticks), well within
the speed of a human defender walking backward. Chaining 10 consecutive
successful steps would advance the line 30 m — structurally impossible given
the 1-second cooldown (10 ticks between each step), so no runaway scenario
exists.

**Stage 0 default:** 3.0 m.

---

### A.7 `OFFSIDE_MAX_DEPTH_M = 45.0 m`

**Rationale:** #12's `DefensiveLineDepth = 1.0` (maximum) corresponds to the
line sitting at approximately 50 m from the goal-line (near or slightly beyond
midfield) for the most aggressive team configuration. The 45 m ceiling prevents
the offside trap from pushing the line past the midfield area even if
`DefensiveLineDepth` is at maximum and the trap fires repeatedly. At 45 m,
the trap leaves a 60 m counter-attack zone behind the line — aggressive but
not structurally suicidal. Exceeding 52.5 m (`HALF_LINE_X`) would mean
stepping into opponent territory, which is nonsensical for a defensive trap.

**Sensitivity:** raising to 50 m+ risks a high-line overexposure scenario
(EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY becomes easier to exploit). Lowering to
35 m makes the trap ineffective for high-pressing teams.

**Stage 0 default:** 45.0 m.

---

### A.8 `OFFSIDE_TRAP_DWELL_TICKS = 3 ticks (300 ms)`

**Rationale:** Three consecutive 10 Hz ticks (300 ms) ensures the ball has
been slow for at least 0.3 seconds before the step-up commits. This filters
out transient slowdowns (e.g., a ball bouncing off a player) while remaining
responsive to genuine set-play / dead-ball situations where the trap is
tactically appropriate. Fewer than 3 ticks risks reactive traps during
normal play; more than 5 ticks (500 ms) misses the optimal tactical window
before the ball is played forward.

**Stage 0 default:** 3 ticks.

---

### A.9 `OFFSIDE_RESET_COOLDOWN_TICKS = 10 ticks (1,000 ms)`

**Rationale:** After a coordinated step-up, the defensive line needs time to
re-establish shape before a second trap can fire. 1,000 ms is approximately
the time for a restart pass or a brief possession phase to develop.
It also prevents the line from pulsing forward repeatedly in rapid succession,
which would produce an unrealistic "conveyor belt" offside trap effect.

**Stage 0 default:** 10 ticks.

---

### A.10 `LINE_COHERENCE_THRESHOLD_M = 8.0 m`

**Rationale:** A 4-man backline with average inter-agent spacing of ~17 m
(68 m ÷ 4) has a healthy x-spread of < 5 m when all four are roughly aligned.
An x-spread of 8 m indicates one agent is significantly straggling behind
(by half the average inter-agent gap). Stepping with a broken line creates
uneven coverage — the lagging agent creates a pocket behind the step-up line.
The threshold is set at half the average gap to catch genuine line-breaking
scenarios without triggering on minor positional noise.

**Stage 0 default:** 8.0 m.

---

### A.11 `MARK_DWELL_TICKS = 4 ticks (400 ms)`

**Rationale:** Reuses the dwell-time pattern from Agent Movement #2 §3.1
(KD-11). 400 ms is the lower end of the #2 §3.1 recommended range of
300–800 ms for tactical state transitions. At 10 Hz, 4 ticks represents
approximately 4 touch cycles in typical possession play — long enough to
confirm that an opponent is genuinely gaining positional advantage over
another (vs. two opponents temporarily crossing paths), but short enough
to adapt to sustained positional changes.

Emergency overrides (§3.8, §3.9) bypass dwell entirely, so the 400 ms
dwell does not delay critical responses.

**Stage 0 default:** 4 ticks.

---

### A.12 `TACKLE_ELIGIBLE_RADIUS_M = 3.0 m`

**Rationale:** Standing tackle reach in football is approximately 1–2 m
(leg extension plus body lean). 3 m provides a decision-making window: the
intent is evaluated at 10 Hz (every 100 ms), but the actual tackle movement
occurs over the following 6–10 physics frames (at 60 Hz, 100–167 ms). A
3 m radius allows the agent to declare tackle intent while still in the
approaching phase, giving #8 / #3 enough frames to execute the physical action.

**Stage 0 default:** 3.0 m.

---

### A.13 `TACKLE_COMMIT_COVERAGE_FLOOR = 1`

**Rationale:** A minimum of one teammate behind the tackling agent ensures
a failed tackle does not result in a clean through-on-goal. With zero teammates
behind (the last-man scenario), committing to a lunge is always prohibited by
the §3.6.2 last-man special case regardless of this constant. This constant
governs the general case (non-last-man agents): at least one cover player
in the depth corridor behind the tackler.

**Stage 0 default:** 1.

---

### A.14 `TACKLE_JOCKEY_ANGLE_RAD = 0.35 rad (~20°)`

**Rationale:** An approach angle of less than 20° means the defending agent's
velocity vector is nearly parallel to the attacker's direction of travel. At
this geometry, a standing challenge is awkward (the defender is running in
the same direction as the attacker), and a mistimed lunge risks being beaten
on the blind side. Shadowing (JOCKEY) at a sub-20° angle allows the defender
to maintain pursuit without over-committing. Above 20°, the defender has a
genuine angle for a challenge.

**Stage 0 default:** 0.35 rad (20.05°).

---

### A.15 `COVERAGE_DEPTH_CORRIDOR_M = 5.0 m`

**Rationale:** A 5 m half-width y-corridor (10 m total) captures teammates
in the same vertical channel while excluding teammates in adjacent lanes.
A defender at y = 34 m and a coverage-depth check with 5 m half-width covers
y ∈ [29, 39] m — approximately the central zone of the pitch. Wider corridors
(10 m+) over-count wide teammates as "behind" the tackler when they are not
in the relevant defensive channel. Narrower corridors (2 m) miss legitimate
cover from centrally positioned teammates at a slight lateral offset.

**Stage 0 default:** 5.0 m.

---

### A.16 `MIN_BACKLINE_AGENTS = 3`

**Rationale:** The minimum viable backline in any mainstream formation is
3 defenders (3-at-the-back formations: 3-5-2, 3-4-3, etc.). With 2 or fewer
backline agents in ZONAL, the team is exposed to a simple wide run or a
striker dropping off. 3 is the lowest common denominator across all formation
types (3-back through 5-back). A 5-at-the-back formation naturally satisfies
this constraint with 2 ZONAL defenders to spare.

**Stage 0 default:** 3.

---

### A.17 `MAX_MAN_MARK_ASSIGNMENTS = 4`

**Rationale:** In a 10-player outfield team, 4 simultaneous man-mark
assignments represents 40% man-marking. This leaves 6 agents in ZONAL,
maintaining sufficient shape coverage. The constraint prevents the entire
midfield from pulling out of position to track individual opponents.
Interaction rule (Appendix D): `MAX_MAN_MARK_ASSIGNMENTS` should not
exceed `poolSize − MIN_BACKLINE_AGENTS`; with pool = 10 and
`MIN_BACKLINE_AGENTS = 3`, the effective ceiling is 7 — so 4 is well within
the structurally safe range.

**Stage 0 default:** 4.

---

### A.18 `MAX_MARK_DISPLACEMENT_M = 20.0 m`

**Rationale:** 20 m is approximately one-fifth of the pitch length (105 m).
Beyond this displacement from the #12 baseline anchor, an agent is
effectively abandoning their formation zone rather than making a targeted
defensive adjustment. For comparison, the distance from a central midfielder
(y ≈ 34 m) to the far touchline (y = 68 m) is 34 m; 20 m therefore covers
lateral cross-pitch assignments to adjacent zones but not full-pitch pulls.

**Stage 0 default:** 20.0 m.

---

### A.19 `GK_EXPECTED_ZONE_MIN_X = −2.0 m`

**Rationale:** Allows the goalkeeper to stand slightly behind the goal-line
(as during a goal-kick wind-up or during goalkeeper distribution preparation)
without triggering a false `COVER_GK_ZONE` override. The −2 m margin accounts
for approximately 2 backward steps behind the line. This constant is not
used in the Stage 0 trigger logic (the trigger uses `GK_EXPECTED_ZONE_MAX_X`
as an upper bound only); it is declared for Stage 1+ zone visualisation
completeness.

**Stage 0 default:** −2.0 m.

---

### A.20 `GK_EXPECTED_ZONE_MAX_X = 15.0 m`

**Rationale:** Derived from two sources:
1. #12's `GK_DEPTH_M = 5.5 m` (from #12 §3.3.3) + `GK_ADVANCE_FACTOR`
   (approximately 8.0 m estimated; not yet ratified in #12 as of IN REVIEW).
   Expected maximum GK depth under normal play ≈ 5.5 + 8.0 = 13.5 m.
2. A 1.5 m buffer above 13.5 m gives 15.0 m to account for GK positioning
   variance (e.g., claiming a cross near the penalty spot at ≈ 11 m, with
   a small approach run overshoot).
A GK at `distToOwnGoal > 15.0 m` is genuinely out of position (e.g., rushing
a penalty-area cross and missing, or advancing for a high ball in the midfield
zone — a rare but dangerous occurrence).

**Stage 0 default:** 15.0 m. Revisit once #12's `GK_ADVANCE_FACTOR` is ratified.

---

### A.21 `COVER_GK_ZONE_MAX_TICKS = 20 ticks (2,000 ms)`

**Rationale:** 2 seconds is sufficient time for a goalkeeper who has briefly
advanced (e.g., claiming a corner cross or rushing a penalty-area ball) to
recover to their expected zone. If the GK has not returned within 2 seconds,
the situation indicates a genuinely extended absence (injury, unusual
positioning choice, or goalkeeper leaving the pitch temporarily) — in which
case the outfield cover agent should revert to their formation role to
prevent structural collapse rather than permanently abandoning their zone.

**Stage 0 default:** 20 ticks.

---

### A.22 `REASSIGN_LATENCY_TICKS = 2 ticks (200 ms)`

**Rationale:** The exploit-resistance criterion for
`EXPLOIT_SWITCH_THROUGH_HOLE` (§5.6.2 T-DA-EXP-002; Appendix E.2).
The nearest HOLD_SHAPE agent must update its `targetPosition` toward the
newly exposed zone within 200 ms of the ball switch. This is the
position-update latency (ZONAL assignment `targetPosition` updates
immediately at the start of each tick); it is distinct from the
`MARK_DWELL_TICKS = 4` mode-label commit latency (which governs when
the `MarkAssignment.mode` label transitions from ZONAL to MAN_MARK/INTERCEPT_RUNNER).

**Interaction:** a 200 ms position-update response (this constant)
combined with 400 ms mode-label commit (MARK_DWELL_TICKS) means that
the covering agent's physical movement begins within 200 ms while the
formal assignment label catches up within 400 ms. This is acceptable: the
agent is moving toward cover before the label formally commits.

**Stage 0 default:** 2 ticks.

---

## Appendix B — Last-Man Predicate Reference Card

Formal definitions in both team orientations with three canonical test inputs.

### B.1 Formal Definitions

**For team defending x = 0 (own goal-line at x = 0):**

```
distToOwnGoal(a) = a.position.x         // x = 0 is own goal

lastManCandidate = argmin{ distToOwnGoal(b) : b ∈ holdShapePool }
    // Tie-break: lowest EntityId (FR-DA-033)

IsLastManThreat(ballPos, lastMan) :=
    distToOwnGoal(ballPos) < distToOwnGoal(lastMan) + LAST_MAN_BALL_BUFFER_M
    AND distToOwnGoal(ballPos) > LAST_MAN_OWN_HALF_MIN_X
```

**For team defending x = 105 (own goal-line at x = 105):**

```
distToOwnGoal(a) = 105.0 - a.position.x    // normalised scalar

lastManCandidate = argmin{ distToOwnGoal(b) : b ∈ holdShapePool }
    // Tie-break: lowest EntityId (FR-DA-033)

IsLastManThreat(ballPos, lastMan) :=
    (105.0 - ballPos.x) < distToOwnGoal(lastMan) + LAST_MAN_BALL_BUFFER_M
    AND (105.0 - ballPos.x) > LAST_MAN_OWN_HALF_MIN_X
```

**GK exclusion invariant:** Even if the GK's x-position yields a lower
`distToOwnGoal` than any outfield defender (e.g., GK at x = 5 m defending
x = 0 vs. defenders at x ≥ 18 m), the GK EntityId is excluded from
`holdShapePool` entirely (FR-DA-009). The last-man algorithm always returns
the rearmost *outfield* defender.

### B.2 Canonical Test Inputs

**Case 1 — Single clear last man, predicate fires:**

Setup: team defending x = 0. HOLD_SHAPE pool:
- Agent 102: x = 18.0 m (distToOwnGoal = 18.0)
- Agent 105: x = 22.0 m (distToOwnGoal = 22.0)
- Agent 107: x = 25.0 m (distToOwnGoal = 25.0)

Ball position: (12.0, 34.0) m. `distToOwnGoal(ball) = 12.0 m`.

```
lastMan = Agent 102 (minimum = 18.0)

Cond 1: 12.0 < 18.0 + 5.0 = 23.0  →  TRUE
Cond 2: 12.0 > 5.0  →  TRUE

IsLastManThreat = TRUE
```

Expected: `directive.emergencyFlag = true`; Agent 102 → `INTERCEPT_RUNNER`.

---

**Case 2 — EntityId tie-break:**

Setup: two agents both at x = 18.0 m (distToOwnGoal = 18.0).
EntityIds: 5 and 12. Ball at x = 12.0 m. `IsLastManThreat` would fire.

```
argmin{ distToOwnGoal } = {Agent 5, Agent 12}  (tie at 18.0 m)
Tie-break: lower EntityId → lastMan = Agent 5
```

Expected: Agent 5 receives `INTERCEPT_RUNNER` override; Agent 12 proceeds to
normal assignment loop.

---

**Case 3 — GK forward, GK excluded:**

Setup: team defending x = 0.
- GK at x = 5.0 m (excluded from pool by FR-DA-009).
- Outfield defenders: Agent 102 at x = 20.0 m, Agent 105 at x = 24.0 m.

Ball at x = 14.0 m.

```
holdShapePool = [Agent 102, Agent 105]   // GK (EntityId 1) absent
lastMan = Agent 102 (min distToOwnGoal = 20.0 among pool)

Cond 1: 14.0 < 20.0 + 5.0 = 25.0  →  TRUE
Cond 2: 14.0 > 5.0  →  TRUE

IsLastManThreat = TRUE
lastMan = Agent 102, NOT the GK at x = 5.0
```

GK exclusion correctly prevents the goalkeeper from ever being identified
as the last-man candidate even when they are the most rearward agent.

---

## Appendix C — Offside Trap Algorithm Verification

Four canonical trigger inputs verifying the §3.7 algorithm.

### C.1 Case 1 — All Conditions Met; Trap Fires

**Setup:** team defending x = 0.
- `OFFSIDE_TRAP_DWELL_TICKS = 3`; `OFFSIDE_STEP_SIZE_M = 3.0 m`;
  `OFFSIDE_MAX_DEPTH_M = 45.0 m`.
- DEFENSE-line at x ≈ 35.0 m (distToOwnGoal ≈ 35.0 m).
- `shape.DefensiveLineDepth = 38.0 m`.

**Tick T:**
Cond 1: ball speed = 1.5 m/s < 4.0 ✓
Cond 2: ball.x = 60.0 > 52.5 (HALF_LINE_X) ✓
Cond 3: DEFENSE line x-spread = 2.5 m < 8.0 ✓
Cond 4: no PRIMARY_PRESS ✓
`stepUpDwellCounter → 1`.

**Tick T+1:** same conditions. `stepUpDwellCounter → 2`.

**Tick T+2:** same conditions. `stepUpDwellCounter → 3 = OFFSIDE_TRAP_DWELL_TICKS`.
`cooldownTicksRemaining == 0` → **TRAP FIRES.**

```
offsideStepDepth = 35.0 + 3.0 = 38.0
offsideStepDepth = max(38.0, 38.0) = 38.0  (shape.DefensiveLineDepth match)
offsideStepDepth = min(38.0, 45.0) = 38.0  (below safety ceiling)
targetX = 38.0 m
```

All DEFENSE-line agents receive `MarkAssignment.mode = ZONAL`,
`targetPosition.x = 38.0 m`. Y-components retain individual formationSlot
values. `directive.offsideTrapActive = true`.
`cooldownTicksRemaining = 10`. `stepUpDwellCounter = 0`.

**Tick T+3:** `cooldownTicksRemaining = 9`. Trap cannot re-fire until
`cooldownTicksRemaining = 0` (earliest: tick T+12).

---

### C.2 Case 2 — Ball Too Fast; Trap Blocked

**Setup:** all conditions except ball speed.
- Ball speed = 6.5 m/s > `OFFSIDE_BALL_SPEED_THRESHOLD_M_S = 4.0 m/s`.

Cond 1 FAILS on every tick. `stepUpDwellCounter` never increments.
Even if ticks T−2 and T−1 were qualifying (dwell = 2), a single fast-ball
tick resets `stepUpDwellCounter = 0`. Trap does not fire.

---

### C.3 Case 3 — Line Not Coherent; Trap Blocked

**Setup:** DEFENSE-line x-positions (team defending x = 0):
{30.0, 32.0, 38.0, 41.0}.

```
spread = max(41.0, 38.0, 32.0, 30.0) − min(41.0, 38.0, 32.0, 30.0)
       = 41.0 − 30.0 = 11.0 m
```

`11.0 > LINE_COHERENCE_THRESHOLD_M = 8.0` → Cond 3 FAILS.
`stepUpDwellCounter = 0`. Trap blocked even if conditions 1, 2, 4 hold.

---

### C.4 Case 4 — Active PRIMARY_PRESS; Trap Blocked

**Setup:** #13 returns 1 agent with `PressRole.PRIMARY_PRESS`.
`pressDir.primaryPressAgent != null` → Cond 4 FAILS.

Trap blocked regardless of ball speed, ball position, or line coherence.
Rationale: stepping the line behind an active press creates dangerous space
between the pressing and defensive lines (§3.7.2 condition 4 commentary).

---

## Appendix D — Anti-Chaos Sensitivity Analysis

| Invariant | Constant | Low value (aggressive) | High value (conservative) | Stage 0 default | Effect |
|---|---|---|---|---|---|
| Min backline | `MIN_BACKLINE_AGENTS` | 2 | 5 | 3 | Lower = more agents available for MAN_MARK; higher = more rigid backline. With 5, only the midfield and attackers can man-mark. |
| Max man-mark | `MAX_MAN_MARK_ASSIGNMENTS` | 2 | 6 | 4 | Lower = more zonal shape preservation; higher = tighter individual coverage. See interaction rule below. |
| Max displacement | `MAX_MARK_DISPLACEMENT_M` | 10 m | 30 m | 20 m | Lower = tighter formation adherence (man-mark limited to adjacent zone); higher = cross-pitch man-marking allowed. |

**Interaction rule:** `MAX_MAN_MARK_ASSIGNMENTS` must not exceed
`poolSize − MIN_BACKLINE_AGENTS`. With a 10-agent pool and
`MIN_BACKLINE_AGENTS = 3`, the safe ceiling is 7. Setting
`MAX_MAN_MARK_ASSIGNMENTS = 7` would allow all non-backline agents to
man-mark simultaneously, leaving only the 3 ZONAL backline agents —
structurally equivalent to a "high-press with no midfield" scenario.

**High-line profile example (Stage 2+ per §7.6):**
```
MIN_BACKLINE_AGENTS = 2    // allows one DEFENSE agent to mark
MAX_MAN_MARK_ASSIGNMENTS = 5
OFFSIDE_MAX_DEPTH_M = 48.0
OFFSIDE_TRAP_DWELL_TICKS = 2
```

**Deep-block profile example (Stage 2+ per §7.6):**
```
MIN_BACKLINE_AGENTS = 4    // rigid backline
MAX_MAN_MARK_ASSIGNMENTS = 3
OFFSIDE_MAX_DEPTH_M = 30.0
OFFSIDE_BALL_SPEED_THRESHOLD_M_S = 2.0  // very low (trap rarely fires)
```

---

## Appendix E — Exploit-Resistance Playbook (KD-18 Corpus)

Canonical four-exploit test scenarios. Each entry specifies: scenario,
initial state, expected output across ticks, and pass criterion.
These scenarios are the basis for §5.6.2 tests T-DA-EXP-001..004.

---

### E.1 `EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY`

**Scenario:** A striker times a forward run to occur *during* the offside-trap
dwell period (when `dwellCounter = 2`, before the required `OFFSIDE_TRAP_DWELL_TICKS = 3`).
The intent is to be in a running position when the trap fires on tick T+1,
exploiting the forward momentum advantage.

**Initial state:**
- DEFENSE-line at x ≈ 35.0 m.
- `stepUpDwellCounter = 2` (2 of 3 qualifying ticks elapsed).
- Striker at x = 54.0 m, starting a forward run at speed 4.5 m/s toward x = 20.0 m.

**Expected outputs:**
- **Tick T** (`dwellCounter = 2`): trap does NOT fire (counter has not reached 3).
  Striker qualifies as INTERCEPT_RUNNER (speed > 3.0 m/s; direction toward own half).
  An eligible HOLD_SHAPE defender receives `INTERCEPT_RUNNER` assignment targeting
  the striker. Defensive line stays at 35.0 m.
- **Tick T+1** (`dwellCounter = 3`): if all four trigger conditions still hold,
  trap fires. All DEFENSE-line agents advance to `offsideStepDepth = 38.0 m`.

**Pass criterion:** the step-up does NOT occur until `dwellCounter` reaches
`OFFSIDE_TRAP_DWELL_TICKS = 3`. Early-run exploitation does not compress the
defensive line prematurely. The INTERCEPT_RUNNER cover on tick T provides
defensive response during the dwell window.

---

### E.2 `EXPLOIT_SWITCH_THROUGH_HOLE`

**Scenario:** A long diagonal ball switches play from the left flank to an
unguarded right channel at tick T. No HOLD_SHAPE agent currently has a
MAN_MARK or INTERCEPT_RUNNER assignment covering the right channel.

**Initial state:**
- Right-channel HOLD_SHAPE agent (Agent 113) in `ZONAL` at #12 baseline
  `formationSlot = (35.0, 58.0)`.
- An opponent (EntityId 205) arrives in the right channel at
  approximately (35.0, 62.0) m — within `MAN_MARK_CANDIDATE_RADIUS_M = 15.0 m`
  of Agent 113.

**Expected outputs:**
- **Tick T:** Agent 113's `EvaluateCandidates` call detects opponent 205 within
  radius. Best candidate = 205 (highest threat in radius). `holdTicks` for
  candidate 205 = 1 (starts accumulating toward `MARK_DWELL_TICKS = 4`).
  `assignments[113].mode` remains `ZONAL` (mode-label commit not yet reached),
  but `targetPosition` updates to the zone where the ball arrived
  (ZONAL targetPosition = formationSlot — note: the ZONAL targetPosition is
  the agent's formationSlot; actual movement toward the ball channel is the
  result of #8 evaluating the ZONAL targetPosition which is updated by the
  formationSlot drift in #12 on the same tick when #12 detects the phase
  shift. The ZONAL position converges on the threat within 2 ticks as #12
  recalculates `formationSlot` for the changed phase).
- **Tick T+1:** `holdTicks = 2`; Agent 113 is physically moving toward the
  channel (via #8/MOVE_TO_POSITION with ZONAL targetPosition).
- **Tick T + REASSIGN_LATENCY_TICKS (T+2):** Agent 113's `targetPosition` is
  within the right channel zone (`REASSIGN_LATENCY_TICKS = 2` criterion met
  for position-update response). Mode-label commit (MAN_MARK) occurs at T+3
  or T+4 depending on candidate stability.

**Pass criterion:** within `REASSIGN_LATENCY_TICKS = 2` ticks, Agent 113's
`targetPosition` reflects coverage toward the ball channel. The right channel
is not exploitably open for more than 200 ms.

---

### E.3 `EXPLOIT_LAST_MAN_ONE_ON_ONE`

**Scenario:** An attacker beats the last outfield defender in a one-on-one
and is in behind with a clear run toward goal. The defender should shadow
(JOCKEY) rather than lunge (COMMIT), preserving shape and slowing the attack
until the goalkeeper (or a recovering teammate) can intervene.

**Initial state (team defending x = 0):**
- Attacker (EntityId 210) at x = 22.0 m; distToOwnGoal = 22.0 m.
- LastMan (Agent 102) at x = 20.0 m; distToOwnGoal = 20.0 m.
- Ball at x = 19.0 m; distToOwnGoal = 19.0 m.
- No HOLD_SHAPE teammates with distToOwnGoal < 20.0 m → `coverageDepth = 0`.

**Predicate evaluation:**
```
distToOwnGoal(ball) = 19.0
Cond 1: 19.0 < 20.0 + 5.0 = 25.0  →  TRUE
Cond 2: 19.0 > 5.0  →  TRUE
IsLastManThreat = TRUE
```

**Expected outputs:**
- `directive.emergencyFlag = true`.
- Agent 102 → `INTERCEPT_RUNNER` targeting attacker 210. `ResetHysteresis` called.
- §3.6 tackle intent for Agent 102: dist(102, 210) = distance from (20.0, ...) to (22.0, ...).
  If within `TACKLE_ELIGIBLE_RADIUS_M = 3.0 m`, tackle intent evaluated.
  Last-man special case (§3.6.2): `coverageDepth` forced to 0 for Agent 102
  regardless of corridor check. `coverageDepth (0) < TACKLE_COMMIT_COVERAGE_FLOOR (1)`.
  Approach angle check: if angle < 0.35 rad → JOCKEY; else → HOLD. In a
  straight-line pursuit, approach angle ≈ 0 → JOCKEY.
- `TackleIntentRequest.mode = JOCKEY`.

**Pass criterion:** `TackleIntentRequest.mode != COMMIT` for the last-man agent.
Committing when truly last-man with zero coverage is forbidden.

---

### E.4 `EXPLOIT_GK_OUT_OF_POSITION`

**Scenario:** The goalkeeper advances to claim a cross in the penalty area
and misses. The GK is now at x = 20.0 m (distToOwnGoal = 20.0 > 15.0 m
ceiling). The ball falls at x = 8.0 m with an advancing attacker.

**Initial state (team defending x = 0):**
- GK at position (20.0, 34.0); `distToOwnGoal(GK) = 20.0`.
- `GK_EXPECTED_ZONE_MAX_X = 15.0 m`.
- Ball at x = 8.0 m; lastMan at x = 18.0 m (distToOwnGoal = 18.0).

**Predicate evaluation:**
```
IsLastManThreat: 8.0 < 18.0 + 5.0 = 23.0 ✓; 8.0 > 5.0 ✓  →  TRUE
distToOwnGoal(GK) = 20.0 > 15.0  →  COVER_GK_ZONE condition 2 holds
```

**Expected outputs (same tick as out-of-zone first detected):**
- `directive.emergencyFlag = true`.
- Last-man (Agent 102) → `INTERCEPT_RUNNER` (Steps 4).
- `abandonedZoneCenter = (15.0 / 2.0, 34.0) = (7.5, 34.0)`.
- Cover agent selection (Step 4a): argmin displacement cost to (7.5, 34.0)
  among HOLD_SHAPE pool excluding Agent 102.
  Example: Agent 105 at (22.0, 34.0) → `cost = 210.25 m²`.
  Agent 107 at (25.0, 38.0) → `cost = 322.25 m²`.
  → `coverAgent = Agent 105`.
- `assignments[105].mode = COVER_GK_ZONE`;
  `assignments[105].targetPosition = (7.5, 34.0)`.
- `offsideState.coverGkZoneActiveTicks = 1`.

**Pass criterion:** `COVER_GK_ZONE` assignment issued to Agent 105 within the
same tick that the GK out-of-zone condition is first detected. No tick lag.

---

## Appendix F — Glossary

| Term | Definition |
|---|---|
| `MarkDirective` | Per-team per-tick output struct from #14. Fields: `team`, `offensiveLineDepth` (read from #12), `offsideTrapActive`, `stepUpTargetDepth`, `emergencyFlag`. |
| `MarkAssignment` | Per-agent per-tick output struct. `mode` is one of: `ZONAL`, `MAN_MARK`, `INTERCEPT_RUNNER`, `COVER_GK_ZONE`. `targetEntityId` and `targetPosition` are null for `ZONAL`. |
| `MarkHysteresisState` | Per-agent per-tick persistent state. Tracks `dwellCounter`, `candidateMode`, `candidateTargetId`, `holdTicks`. Prevents assignment thrash (§3.11). Digested per #16 §6.2. |
| `TackleIntentRequest` | Per-agent per-tick struct produced for agents within `TACKLE_ELIGIBLE_RADIUS_M` of their assigned opponent. `mode` is `COMMIT`, `JOCKEY`, or `HOLD`. Consumed by #8 → dispatched to #3. |
| `OffsideLineState` | Per-team persistent state. Tracks `currentLineDepth`, `stepUpDwellCounter`, `cooldownTicksRemaining`, `coverGkZoneActiveTicks`. Digested per #16 §6.2. |
| `HOLD_SHAPE pool` | Set of outfield agents not assigned to a press role (`PRIMARY_PRESS` / `COVER_SHADOW`) by #13 and excluding the GK. #14's exclusive assignment pool. |
| `ZONAL` | Mark mode: agent maintains position near their #12 `formationSlot` baseline. No specific opponent `EntityId` tracked. Default mode and safe fallback. |
| `MAN_MARK` | Mark mode: agent directly tracks a specific opponent `EntityId` within `MAN_MARK_CANDIDATE_RADIUS_M`. Target selected by highest threat score (§3.5). |
| `INTERCEPT_RUNNER` | Mark mode: agent positions to intercept an opponent making a qualifying-velocity run toward own half (§3.3.3). Also used as the emergency last-man override (§3.8). |
| `COVER_GK_ZONE` | Emergency mark mode: agent covers the GK's abandoned zone when `emergencyFlag` is active and GK is beyond `GK_EXPECTED_ZONE_MAX_X` (§3.9). |
| Offside trap | Tactical manoeuvre: all DEFENSE-line agents advance simultaneously to `offsideStepDepth` on the same tick (§3.7). #14 owns the step-up decision only; offside adjudication belongs to a future referee spec (KD-9). |
| Last-man predicate | Deterministic binary test (`IsLastManThreat`) that fires when the ball is ahead of (closer to own goal than) the rearmost outfield defender plus `LAST_MAN_BALL_BUFFER_M` (§3.8). |
| `distToOwnGoal` | Team-agnostic distance scalar: `|agent.position.x − ownGoalLine.x|`. Shared normalisation across §3.6, §3.8, §3.9, and Appendix B. |
| Coverage depth | Count of HOLD_SHAPE teammates between the tackling agent and their own goal within the `COVERAGE_DEPTH_CORRIDOR_M` y-corridor (§3.6.2). |
| Anti-chaos invariants | Three enforced constraints applied before directive publication: `MIN_BACKLINE_AGENTS` (invariant 1), `MAX_MAN_MARK_ASSIGNMENTS` (invariant 2), `MAX_MARK_DISPLACEMENT_M` (invariant 3). See §3.10 and Appendix D. |
| Assignment hysteresis | Dwell-time mechanism (§3.11) preventing assignment thrash. A new candidate must be consistently preferred for `MARK_DWELL_TICKS` consecutive ticks before the transition commits. Emergency overrides bypass hysteresis. |
| `DOMAIN_TAG_DEFENSIVE_AI` | RNG domain tag for stochastic tie-breaking in #14: `0x1A` `[CROSS: #16 §3.4]` — ERR-014-004 resolved May 18, 2026; allocated in #16 §3.4 v1.0.5 within the Phase B/C block. |

---

## Appendix G — Telemetry and Troubleshooting Playbook

### G.1 Stage 0

No runtime debug overlays at Stage 0. This appendix is a placeholder; the
overlay specification is a Stage 1 deliverable (first `src/DefensiveAI/`
commit).

### G.2 Stage 1+ Planned Overlays

The following debug visualisations are planned for Stage 1 development tools.
They do not affect the deterministic simulation output.

| Overlay | Visual | Activation |
|---|---|---|
| Per-agent mark assignment mode | Colour-coded arc over agent: ZONAL = white, MAN_MARK = yellow, INTERCEPT_RUNNER = orange, COVER_GK_ZONE = red | Dev debug toggle |
| Offside line | Horizontal line at `currentLineDepth` projected into pitch view; flashes on step-up tick | Dev debug toggle |
| Tackle intent indicator | Icon over eligible agent: shield = HOLD, jogging figure = JOCKEY, lunge silhouette = COMMIT | Dev debug toggle |
| Emergency overlay | Red flash on team's defensive half when `emergencyFlag = true` | Dev debug toggle |
| Dwell counter | Numeric overlay on each agent showing `hysteresis.holdTicks` / `MARK_DWELL_TICKS` | Dev debug toggle |
| Anti-chaos indicator | Yellow border around pool agents that were demoted during the anti-chaos pass this tick | Dev debug toggle |

### G.3 Known Diagnostic Signals

| Signal | Source | Meaning |
|---|---|---|
| `DEFENSIVE_AI_INVARIANT_FALLBACK` | dev-log WARN | Anti-chaos loop could not resolve invariants in 3 passes; all-ZONAL emitted (F4 fallback). Investigate invariant configuration or pool state. |
| `PRESS_DIRECTIVE_ABSENT` | dev-log WARN | #13 directive was stale or absent; all outfield non-GK treated as HOLD_SHAPE (F3 fallback). Investigate #13 scheduler order. |
| `STALE_PERCEPTION_TICK` | dev-log WARN (with tick delta) | Perception snapshot is older than current tick; previous `MarkDirective` reused (F1 fallback). Investigate #7 scheduler or tick synchronisation. |

---

## Appendix H — Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial appendices. Appendix A: 22 `[GT]` constant derivation entries (all promoted from `[EST]`; A.1–A.22). Appendix B: last-man predicate reference card with formal definitions (both team orientations) + 3 canonical test cases (single last man, EntityId tie, GK-forward exclusion). Appendix C: offside trap verification with 4 canonical cases (trap fires, ball too fast, incoherent line, active press). Appendix D: anti-chaos sensitivity analysis with high/low/default values + interaction rule + two named style profiles. Appendix E: 4 KD-18 exploit-resistance scenarios (E.1 early trap, E.2 switch through hole, E.3 last-man one-on-one, E.4 GK out of position) — tick-by-tick trace with pass criteria. Appendix F: 16-entry glossary. Appendix G: telemetry playbook (Stage 0 placeholder + Stage 1+ planned overlays + known diagnostic signals). |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. L4: Appendix A.9 typo corrected — "time for a restart restart pass" → "time for a restart pass". |
| 0.3 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): Appendix F glossary entry for `DOMAIN_TAG_DEFENSIVE_AI` — `[CROSS-PENDING]` (ERR-014-004) promoted to `[CROSS: #16 §3.4]`; ERR-014-004 resolved May 18, 2026. |
