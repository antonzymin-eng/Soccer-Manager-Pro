# Pressing AI Specification #13 — Appendices

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## Appendix A — Derivations

This appendix accumulates derivation entries that promote
outline-stage `[EST]` constants to `[GT]` per CLAUDE.md "When
Writing or Editing Specs" and KD-14. Each entry must justify the
chosen value with worked reasoning, citation, or sensitivity
analysis. All entries are PENDING at v0.1 and gate the §9.3 (d)
precondition.

### A.1 `TRIGGER_DWELL_TICKS = 2` (PENDING)

Rationale to be derived: 2 ticks at 10 Hz = 200 ms — the typical
perception-latency window for a coordinated team response. Long
enough to filter single-tick noise; short enough to feel
responsive. To be confirmed against #2 §3.1 dwell-time analysis
and against §5.6 KD-17 corpus pass-rate data.

### A.2 `TRIGGER_RELEASE_TICKS = 3` (PENDING)

Rationale: asymmetric release (longer than commit) prevents a
committed press from oscillating against a brief raw-condition
clear. 300 ms is the typical "did the cue genuinely end?" window.

### A.3 `ROLE_DWELL_TICKS = 3` (PENDING)

Rationale: 300 ms suppresses role-thrash between near-equal cost
candidates. Sensitivity to ±1 tick is measured in Appendix C.

### A.4 `INTERCEPT_LOOKAHEAD_TICKS = 3` (PENDING)

Rationale: 300 ms of forward projection along ball-carrier velocity
captures the carrier's near-term position without over-committing
to a velocity that will change on a touch.

## Appendix B — Trigger Catalog Reference Cards

One card per trigger (FR-PR-009..012). Each card lists the input
surface, threshold, debounce, worked example, and test reference.

**Drift risk.** Values in these cards mirror §3.1.x prose and §6.1
constant catalogue. Single source of truth is `PressingAIConstants.cs`
at Stage 1+. If a threshold is re-tuned in §6.1, update the
corresponding card here in the same commit.

### B.1 `BAD_TOUCH`

| Field | Value |
|---|---|
| Source | First Touch #4 §3.1 (`q`) + §3.5 (`pressureScalar` already folded into `q`) |
| Inputs | `q ∈ [0,1]`; post-touch ball-velocity magnitude (m/s) |
| Threshold | `q < BAD_TOUCH_THRESHOLD = 0.40 [GT]` AND `||v|| > BAD_TOUCH_VELOCITY_M_S = 4.0 [GT]` |
| Debounce | `TRIGGER_DWELL_TICKS = 2`; `TRIGGER_RELEASE_TICKS = 3` |
| Trigger origin | Ball-carrier who attempted the touch |
| Worked example | `q = 0.30`, `||v|| = 6.0` → fires |
| Test | T-U-001 |

### B.2 `BACKWARD_PASS`

| Field | Value |
|---|---|
| Source | Pass Mechanics #5 §2 FR-10 `PassAttemptEvent` at `CONTACT` |
| Inputs | `e.AgentID` (passer lookup in perception); `e.TargetPosition`; `attackingDirection` (orchestrator) |
| Threshold | `dot(normalize((e.TargetPosition − passerPosition).xy), attackingDirection) < BACKWARD_PASS_THRESHOLD = −0.30 [GT]`; `passerPosition = perception.agents[e.AgentID].position` |
| Debounce | `TRIGGER_DWELL_TICKS = 2`; `TRIGGER_RELEASE_TICKS = 3` |
| Trigger origin | Passer `AgentID` |
| Worked example | Passer at `(45, 30)`, `TargetPosition = (39, 33)`, attackingDir `(+1, 0)` → direction delta `(−6, 3)` → unit `(−0.894, 0.447)` → dot = `−0.894` → fires |
| Test | T-U-002 |

### B.3 `SIDELINE_TRAP`

| Field | Value |
|---|---|
| Source | Ball Physics #1 §1.2 (pitch geometry) + Perception #7 (positions / facing) |
| Inputs | `ball.position`, `ballCarrier.facing` |
| Threshold | `min(ball.y, 68 − ball.y) < SIDELINE_TRAP_DISTANCE_M = 8.0 [GT]` AND `dot(facing, sidelineDir) > 0` |
| Debounce | `TRIGGER_DWELL_TICKS = 2`; `TRIGGER_RELEASE_TICKS = 3` |
| Trigger origin | Ball-carrier |
| Worked example | ball `(45, 5)`, facing `(0.5, −0.87)` → fires |
| Test | T-U-003 |

### B.4 `WEAK_RECEIVER`

| Field | Value |
|---|---|
| Source | Perception #7 §3.7–§3.10 (visibility + attribute lookup) |
| Inputs | `r.attribute.FirstTouch` (via #7 §3.10 perception propagation); `r.perceivedPressure` (#7 §3.10 — defending team reads own perception of local pressure on `r`, NOT the receiver's self-assessment) |
| Threshold | `FirstTouch < WEAK_RECEIVER_THRESHOLD = 10 [GT]` AND `perceivedPressure ≥ WEAK_RECEIVER_PRESSURE = 0.50 [GT]` |
| Debounce | `TRIGGER_DWELL_TICKS = 2`; `TRIGGER_RELEASE_TICKS = 3` |
| Trigger origin | Lowest-`FirstTouch` qualifying receiver; EntityId ascending tie-break; opposing GK excluded (KD-13) |
| Worked example | receiver `FirstTouch = 7`, `perceivedPressure = 0.65` → fires |
| Test | T-U-004 |
| Stage 1+ note | `r.attribute.FirstTouch` is consumed with perfect-knowledge at Stage 0 (schema-only). Stage 1+ scouting-accuracy gating applies per #7 §3.10 propagation. Same Q2-style caveat as §2.3. |

## Appendix C — Cover-Shadow Lane Geometry (Worked Examples)

`shadowLane = lerp(carrier, receiver, COVER_SHADOW_LANE_FRACTION)`
where `COVER_SHADOW_LANE_FRACTION = 0.55 [GT]`. Three canonical
configurations:

### C.1 Vertical (carrier and receiver share X)

Carrier at `(50, 20)`, receiver at `(50, 40)`:

```
delta = (0, 20)
shadowLane = (50, 20) + (0, 20) × 0.55 = (50, 31.0)
```

Shadow sits 11.0 m up-pitch from carrier, 9.0 m short of receiver.
Arithmetic verified: `20 + 20 × 0.55 = 20 + 11 = 31`.

### C.2 Diagonal (carrier and receiver on a 45° line)

Carrier at `(60, 30)`, receiver at `(75, 40)` (outline reference):

```
delta = (15, 10)
shadowLane = (60, 30) + (15, 10) × 0.55
           = (60 + 8.25, 30 + 5.5)
           = (68.25, 35.5)
```

Distance carrier→shadow: `sqrt(8.25² + 5.5²) ≈ sqrt(68.0625 +
30.25) ≈ sqrt(98.3125) ≈ 9.915 m`. Shadow→receiver: `sqrt(6.75² +
4.5²) ≈ sqrt(45.5625 + 20.25) ≈ sqrt(65.8125) ≈ 8.112 m`.
Ratio 9.915 : 8.112 ≈ 55 : 45 ✓ (matches the lane fraction).

### C.3 Horizontal (carrier and receiver share Y)

Carrier at `(40, 30)`, receiver at `(70, 30)`:

```
delta = (30, 0)
shadowLane = (40, 30) + (30, 0) × 0.55 = (56.5, 30)
```

Shadow sits 16.5 m up-pitch from carrier along the X-axis,
13.5 m short of receiver. Arithmetic verified:
`40 + 30 × 0.55 = 40 + 16.5 = 56.5`.

## Appendix D — Anti-Chaos Sensitivity Analysis (KD-16)

Per-invariant sensitivity sweeps. Numerical sensitivity tables to
be filled in during the v0.2 fix pass against measured output
from §5.6.1 (T-C-001..006) and §5.6.2 (T-X-001..004).

| Constant | Perturbation | Affected output | Expected sensitivity |
|---|---|---|---|
| `MAX_PRESSERS_BALL_THIRD` | ±1 | press density in ball-side third; F5 fallback rate | high |
| `MIN_BACKLINE_AGENTS` | ±1 | backline-floor breach rate; `EXPLOIT_LONG_BALL_OVER_PRESSERS` pass | high |
| `MAX_PRESS_DISPLACEMENT_M` | ±5 m | cover-shadow demotion rate | medium |
| `COVER_SHADOW_LANE_FRACTION` | ±0.05 | interception rate vs angle-denial rate | medium |
| `DISENGAGE_TIMEOUT_TICKS` | ±2 ticks | press duration distribution | low |
| `RESET_LATENCY_TICKS` | ±3 ticks | re-press latency; `EXPLOIT_SWITCH_OF_PLAY` reset window | medium |
| `PRESS_FATIGUE_CEILING` | ±0.05 | end-of-match press-eligibility floor | high |

## Appendix E — Exploit-Resistance Playbook (KD-17 corpus)

Each scenario has an input sketch, expected directive evolution,
and pass criterion. Test references in §5.6.2.

### E.1 `EXPLOIT_LONG_BALL_OVER_PRESSERS` (T-X-001)

**Input:** Own team committed to a high press (3 pressers in
ball-side third); opponent's GK plays a long ball over the press
line directly to a striker behind the back four.

**Expected directive evolution:** `BACKWARD_PASS` does NOT fire
(ball moves forward toward opponent goal). `BAD_TOUCH` may fire
if the striker mis-controls. Critically: `MIN_BACKLINE_AGENTS = 3`
floor remains intact — no Defense-line agent was promoted to
`PRIMARY_PRESS` that would breach the floor (FR-PR-019).

**Pass criterion:** Backline count never drops below 3 during the
exploit window.

### E.2 `EXPLOIT_SWITCH_OF_PLAY` (T-X-002)

**Input:** Press committed to one flank. Opponent switches the
ball diagonally to the weak-side wing-back, who is isolated.

**Expected directive evolution:** Ball leaves the
`PRESS_ELIGIBLE_ZONE` (or the trigger raw condition clears).
Zone disengage fires immediately; reset cooldown loads 12 ticks.
The next press cannot fire for 12 ticks.

**Pass criterion:** New `PressDirective` emerges within
`RESET_LATENCY_TICKS = 12` ticks of the new ball position
becoming the carrier's settled state.

### E.3 `EXPLOIT_ONE_TWO_BOUNCE` (T-X-003)

**Input:** Drag-and-bounce one-two through the press: A passes to
B, B returns to A while A drifts behind the cover shadow.

**Expected directive evolution:** Role hysteresis (`ROLE_DWELL_TICKS
= 3`) prevents the cover-shadow from chasing the bounce mid-flight.
At least one defender remains behind the bounce per
`MIN_BACKLINE_AGENTS`.

**Pass criterion:** The press is NOT deterministically beaten —
at least one defender remains goal-side of the bounce in every
seeded replay.

### E.4 `EXPLOIT_GK_PIVOT` (T-X-004)

**Input:** Backward pass to opposing GK.

**Expected directive evolution:** `BACKWARD_PASS` fires
(dot-product strongly negative). Primary press may close the GK
but never beyond the halfway line — `PRESS_ZONE_X_MAX` and the
`MAX_PRESS_DISPLACEMENT_M = 25 m` cap from baseline anchor both
constrain commitment.

**Pass criterion:** Primary-press target position remains within
the team's attacking half AND within 25 m of the agent's #12
anchor. GK itself is never assigned a press role (KD-13).

## Appendix F — Glossary

| Term | Definition |
|---|---|
| **Anti-chaos invariant** | One of the three measurable constraints in KD-16 enforced before directive publication. |
| **Cover shadow** | Per-agent role: occupy the line between ball-carrier and a candidate receiver at `COVER_SHADOW_LANE_FRACTION = 0.55`. |
| **Directive** | The per-team `PressDirective` output for one tick. |
| **Disengage** | Transition from a non-trivial press to all-`HOLD_SHAPE`. |
| **Hold shape** | Default role; agent's slot remains the #12 `formationSlot`. |
| **Primary press** | Per-agent role: close ball-carrier directly. At most one per team per tick. |
| **Press-eligible zone** | Rectangular X-range (Stage 0) where pressing is permitted; arbitrary polygons Stage 1+. |
| **Reset latency** | Cooldown ticks following disengage during which no new press fires. |
| **Shadow lane** | Geometric segment carrier→receiver. |
| **Trap** | Coordinated press configuration funnelling carrier toward a sideline / zone. Stage 0 spec covers timing only. |
| **Trigger** | A boolean condition derived from upstream events that, after debounce, authorises a press. |

## Appendix G — Telemetry and Troubleshooting Playbook (Stage 1+ Deferred)

Stage 0 placeholder. Stage 1+ debug overlays:

| Overlay | Renders | Source |
|---|---|---|
| Trigger flags badge | Per-team bitmask of fired triggers this tick | §3.2 |
| Primary-press marker | Highlighted agent + line to ball-carrier | §3.3 |
| Cover-shadow lanes | Lines from each cover-shadow agent to `shadowLane(r)` and to receiver | §3.5 |
| Anti-chaos floor counts | Numeric overlay of presser count / backline count / max displacement | §3.9 |
| Disengage/reset state | Corner badge with `disengageDwellTicks` / `resetCooldownTicks` | §3.8 |

## Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial appendices draft from `outline-detailed.md` v1.0. Three worked examples in Appendix C (vertical, diagonal, horizontal) per FR-PR-034. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H2: B.2 source `FR-08` → `FR-10`; inputs changed from `passVelocity` to passer position → TargetPosition; worked example updated; trigger origin changed to passer AgentID. AR-S1-H6: B.4 attribute-access Stage 1+ caveat added. AR-S1-L2: Appendix B preamble drift-risk note added. |
