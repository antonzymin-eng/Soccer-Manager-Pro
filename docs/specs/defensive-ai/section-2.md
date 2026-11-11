# Defensive AI Specification #14 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** May 17, 2026
**Last Updated:** | 0.7 | November 11, 2026 | AI agent (W13.1) | `ERR-014-010`: FR-DA-038 / FR-DA-039 added (the file's own "37 FRs" statements in v0.1 and the §9 checklist are historical). §2.3 gains the `markMovementActive` per-tick input (default false, not serialized). Code: `MarkTargetComposer`, `InvariantEnforcer`, `DefensiveSnapshot.MarkMovementActive`. |
**Version:** 0.7
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## 2.1 Functional Requirements

Conformance levels follow RFC 2119: **MUST** is normative; **SHOULD** is a
strong recommendation subject to documented override; **MAY** is permissive.
All citations resolve against either a KD in §1.5 or a downstream section
in this spec.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-DA-001 | Defensive AI runs on the 10 Hz tactical loop. No per-frame (60 Hz) work is produced. | MUST | CLAUDE.md / KD-2 |
| FR-DA-002 | Output per tick is one `MarkDirective` per team plus one `MarkAssignment` per agent in the HOLD_SHAPE pool. | MUST | KD-2 |
| FR-DA-003 | Agent iteration order during assignment computation is EntityId-sorted ascending. | MUST | #16 §3.2.5 / KD-10 |
| FR-DA-004 | `MarkDirective` and `MarkAssignment` values, together with `MarkHysteresisState`, `OffsideLineState`, and `TackleIntentRequest`, contribute to the per-tick determinism digest. | MUST | #16 §6.2 / KD-10 |
| FR-DA-005 | Any RNG calls use `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` `[CROSS: #16 §3.4]` — ERR-014-004 resolved May 18, 2026; allocated in #16 §3.4 v1.0.5. | MUST | #16 §3.4 / KD-10 |
| FR-DA-006 | No heap allocation on the per-tick hot path. | MUST | #18 §3.7 |
| FR-DA-007 | All constants live in a single catalogue file `DefensiveAIConstants.cs`, organised into `#region` blocks per #20 §4.2 (FR-CS-025). | MUST | #20 FR-CS-025 / KD-14 |
| FR-DA-008 | Fatigue input convention is `0.0 = fully rested`, `1.0 = fully fatigued`. Any inversion is a critical error. | MUST | CLAUDE.md / KD-1 |
| FR-DA-009 | The goalkeeper is excluded from #14's assignment pool entirely; the GK never receives a `MarkAssignment`. | MUST | KD-7 |
| FR-DA-010 | Agents with #13 role `PRIMARY_PRESS` or `COVER_SHADOW` on the current tick are excluded from #14's assignment pool. | MUST | KD-4 |
| FR-DA-011 | The mark-mode enum contains exactly four values: `ZONAL`, `MAN_MARK`, `INTERCEPT_RUNNER`, `COVER_GK_ZONE`. | MUST | §3.3 |
| FR-DA-012 | `DefensiveLineDepth` is read from #12's field; #14 does not write or mutate it. | MUST | KD-3 |
| FR-DA-013 | Phase gating: if #12's phase for this team is `IN_POSSESSION`, #14 emits an all-`ZONAL` `MarkDirective` and returns without executing the assignment algorithm. | MUST | KD-19 |
| FR-DA-014 | The mark-assignment algorithm selects targets using displacement-based cost (`|agent.position − targetPos|²`) with EntityId ascending as the terminal tie-break. | MUST | §3.4 / KD-11 |
| FR-DA-015 | Assignment-mode transitions use the dwell-time hysteresis pattern from Agent Movement #2 §3.1, parameterised by `MARK_DWELL_TICKS [GT]`. | MUST | KD-11 / §3.11 |
| FR-DA-016 | The man-mark target for an agent is the opponent within `MAN_MARK_CANDIDATE_RADIUS_M [GT]` that has the highest threat score (§3.5). | MUST | §3.3 |
| FR-DA-017 | Threat score is `perceivedGoalProximity × opponentFirstTouch`. Both inputs are read from the #7 perception snapshot; #14 does not read opponent attributes directly. | MUST | §3.5 / KD-1 |
| FR-DA-018 | The offside trap fires when the ball velocity magnitude is below `OFFSIDE_BALL_SPEED_THRESHOLD_M_S [GT]` AND the step-up dwell counter has reached `OFFSIDE_DWELL_TICKS [GT]` AND the team phase is `OUT_OF_POSSESSION` or `TRANSITION`. | MUST | §3.7 / KD-9 |
| FR-DA-019 | When the offside trap fires, all agents with `LineMembership = DEFENSE` advance to `offsideStepDepth` simultaneously on the same tick. | MUST | §3.7 |
| FR-DA-020 | Offside-line adjudication (whether a striker is offside) is out of scope. #14 places defenders; a future referee spec adjudicates. | MUST | KD-9 |
| FR-DA-021 | The last-man predicate is computed deterministically per KD-12 using `IsLastManCandidate` and `IsLastManThreat`, with EntityId ascending as the tie-break. | MUST | KD-12 / §3.8 |
| FR-DA-022 | Emergency override: when `IsLastManThreat` is true for a tick, the identified last-man candidate's `MarkAssignment` mode is overridden to `INTERCEPT_RUNNER`. | MUST | §3.9 |
| FR-DA-023 | Tackle intent: for each HOLD_SHAPE agent within `TACKLE_ELIGIBLE_RADIUS_M [GT]` of its assigned opponent, #14 evaluates and produces a `TackleIntentRequest` with mode `COMMIT`, `JOCKEY`, or `HOLD`. | MUST | §3.6 / KD-6 |
| FR-DA-024 | All three anti-chaos invariants are checked BEFORE the `MarkDirective` is published for this tick. | MUST | KD-17 |
| FR-DA-025 | Anti-chaos invariant 1: the count of HOLD_SHAPE agents with `LineMembership = DEFENSE` must not fall below `MIN_BACKLINE_AGENTS [GT]` (Stage 1 default: 3). | MUST | KD-17 |
| FR-DA-026 | Anti-chaos invariant 2: the total count of agents assigned `MAN_MARK` mode must not exceed `MAX_MAN_MARK_ASSIGNMENTS [GT]` (Stage 1 default: 4). | MUST | KD-17 |
| FR-DA-027 | Anti-chaos invariant 3: a `MarkAssignment` that places an agent more than `MAX_MARK_DISPLACEMENT_M [GT]` (Stage 1 default: 20 m) from its #12 baseline anchor position is ineligible. | MUST | KD-17 |
| FR-DA-028 | When an anti-chaos invariant is violated, the costliest overriding assignment (highest displacement cost) is demoted to `ZONAL` and the invariant check is re-run until the directive is clean or no further demotions are possible. | MUST | KD-17 / §3.10 |
| FR-DA-029 | Failure mode F1 — stale perception: if the perception snapshot tick index precedes the current tick, the previous tick's `MarkDirective` and `MarkAssignment[]` are reused verbatim. | MUST | §2.4 |
| FR-DA-030 | Failure mode F2 — #12 slot unavailable: if `BaselineDefensiveShapeView` reports `SENTINEL_NO_SLOT` for an agent, emit an all-`ZONAL` `MarkDirective` for this tick (not just for the affected agent). | MUST | §2.4 |
| FR-DA-031 | Failure mode F3 — #13 directive unavailable: if the `PressAssignment` array for this team is absent or stale, treat all outfield non-GK agents as `HOLD_SHAPE` for this tick. | MUST | §2.4 |
| FR-DA-032 | Failure mode F4 — anti-chaos invariant violation persists after all demotions: fall back to an all-`ZONAL` `MarkDirective` for this tick and emit a `dev-log` warning `DEFENSIVE_INVARIANT_FALLBACK`. | MUST | §2.4 / KD-17 |
| FR-DA-033 | Failure mode F5 — last-man predicate tie with no EntityId ordering resolvable: use ascending EntityId as the canonical resolution. | MUST | §2.4 / KD-12 |
| FR-DA-034 | Every formula in §3 includes units, valid input ranges, and at least one worked example either inline in §3 or in Appendix A. | MUST | CLAUDE.md |
| FR-DA-035 | Every constant carries exactly one tag: `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, or `[CROSS-PENDING]`. | MUST | KD-13 |
| FR-DA-036 | No interface or accessor surface is produced against Attacking AI #15 at Stage 0. | MUST | CLAUDE.md / KD-8 |
| FR-DA-037 | Stage-1 activation is gated on all three preconditions being satisfied: (a) #8 ERR-014-001 amendment ratified; (b) #12 Positioning AI reaches `APPROVED`; (c) ERR-014-002 / ERR-014-003 #17 channel rows landed. | MUST | KD-16 / §7 |
| FR-DA-038 | Mark-target composition (W13.1, `ERR-014-010`): for a HOLD_SHAPE agent whose committed assignment is `MAN_MARK` / `INTERCEPT_RUNNER`, #14 composes the goal-side mark point of §3.13.1 (a point `MARK_STANDOFF_M [GT]` from the target's LIVE position toward the own-goal centre); `COVER_GK_ZONE` passes its zone centre through; `ZONAL`, a stale `validThroughTick`, or a missing / inactive target yields no target. Pure, canonical frame, no state, no RNG. | MUST | §3.13.1 / `ERR-014-010` |
| FR-DA-039 | Anti-chaos invariant 4 (W13.1, `ERR-014-010`): when `DefensiveSnapshot.markMovementActive` is true, at most one HOLD_SHAPE agent holds a non-ZONAL assignment on any one opponent; the keeper is an emergency override, else the lowest `|agent − opponent|²`, else the lowest EntityId. When `markMovementActive` is false the invariant does not run. | MUST | §3.10.2 / `ERR-014-010` |

## 2.2 Data Structures

All structures are Stage 1 (spec authored at Stage 0). All fields use the
C# value-type / readonly-struct convention per Code Standards #20 §4.2.
Field types use C# conventions (`float`, `int`, `bool`, `byte`).

### 2.2.1 `MarkDirective` (Stage 1; spec'd at Stage 0)

One instance per team per tick. Carries the team-level defensive
coordination parameters for this tick. Written by #14; read by the
match orchestrator and by #8 `TacticalContext.MarkDirective?` (once
ERR-014-001 is ratified).

```
readonly struct MarkDirective
{
    TeamId  team;
    float   offensiveLineDepth;    // x-coordinate read from #12 DefensiveLineDepth (m)
    bool    offsideTrapActive;     // true when step-up is executing this tick
    float   stepUpTargetDepth;     // x-coordinate target for DEFENSE-line advance (m)
    bool    emergencyFlag;         // true when last-man override is active this tick
}
```

`offensiveLineDepth` is read-only from #12; #14 never writes it (FR-DA-012).
`stepUpTargetDepth` is #14-computed only when `offsideTrapActive` is true;
otherwise it holds the value from the previous tick without semantic effect.

### 2.2.2 `MarkAssignment` (Stage 1; spec'd at Stage 0)

One instance per HOLD_SHAPE agent per tick. Carries the agent's assigned
mark mode and (where applicable) the target opponent and target position.
Written by #14; read by the orchestrator and by #8 §3.1.7 / §3.1.9.

```
readonly struct MarkAssignment
{
    EntityId    agent;
    MarkMode    mode;              // ZONAL | MAN_MARK | INTERCEPT_RUNNER | COVER_GK_ZONE
    EntityId?   targetEntityId;   // null for ZONAL and COVER_GK_ZONE; opponent EntityId otherwise
    Vector2?    targetPosition;   // null for ZONAL; opponent perceived pos or zone center otherwise
    int         validThroughTick; // equals currentTick; guard against stale reads
    bool        overriddenThisTick;  // per-tick transient flag; true when set by §3.8/§3.9
                                     // emergency overrides; reset to false at tick start
    bool        isManuallyAssigned;  // Stage 2+ manual assignment override;
                                     // always false at Stage 0–1 (§7.2)
}
```

`targetPosition` for `MAN_MARK` and `INTERCEPT_RUNNER` is the opponent's
current perceived position from the #7 snapshot — not a predicted future
position. `validThroughTick` is defensive hygiene; any consumer reading a
`MarkAssignment` with `validThroughTick < currentTick` must treat it as
stale.

**Retained assignments (`ERR-014-009`).** "Current" holds on **every tick the
assignment is published**, including ticks where the §3.11 hysteresis retains
it (dwell lock, or the same-target / held-candidate gate path): mode and
`targetEntityId` are retained, `targetPosition` is **refreshed from the live
snapshot** every tick, so §3.10 Invariant 3 and any consumer see where the
opponent is now, not where he was when the mark committed. If the retained
target is **absent from the snapshot or not `isActive`**, there is no position
to publish: the assignment is **released to ZONAL immediately** (`mode = ZONAL`,
`targetEntityId = null`, `targetPosition = formationSlot`) and the agent's
`MarkHysteresisState` is reset exactly as on the §3.3.3 Step 6 ZONAL bypass.
The release resets the **candidate accumulator** as well as the dwell counter
(`holdTicks = 0`, `candidateMode = ZONAL`, `candidateTargetId = null`), so
re-acquiring any mark — including one on the very opponent who was the standing
candidate when the old target vanished — pays a full `MARK_DWELL_TICKS` of
consecutive preference again. The dwell lock exists to suppress candidate
thrash; it does not keep a mark on a player who has left the pitch. The rule is a pure function of the snapshot and
the prior assignment (deterministic; no RNG, no new state). *Example:* agent at
slot (30.0, 34.0) marks opponent 40 committed at (40.0, 34.0) (10.0 m from the
slot); during the lock the opponent runs to (49.0, 34.0): published
`targetPosition = (49.0, 34.0)` (19.0 m, Invariant 3 satisfied); then to
(55.0, 34.0): published `(55.0, 34.0)`, 25.0 m > `MAX_MARK_DISPLACEMENT_M` →
demoted to ZONAL the same tick, although the lock still has ticks left (the
pre-fix stale 10.0 m point would have kept the mark).

### 2.2.3 `TackleIntentRequest` (Stage 1; spec'd at Stage 0)

One instance per eligible agent per tick (only produced for agents within
`TACKLE_ELIGIBLE_RADIUS_M [GT]` of their assigned opponent). Written by #14.

**Amended (KD-6 revised — `ERR-014-006`):** this struct's "read by" clause
previously read "read by #8, which translates it into an `AgentAction`
dispatched to Collision System #3" — that dispatch has no working delegate
(§1.5 KD-6). A `COMMIT` intent that the composition root turns into a
committed challenge is instead read by `TackleOutcomeResolver` (§2.2.8,
§3.6.5), together with the new `TackleDuelInputs` (§2.2.9). #8 and #3 are
no longer readers of this struct.

```
readonly struct TackleIntentRequest
{
    EntityId    agent;
    TackleMode  mode;             // COMMIT | JOCKEY | HOLD
    EntityId    targetEntityId;  // the opponent being tackled
    float       approachAngle;   // radians; angle between agent→opponent and agent velocity
    byte        coverageDepth;   // count of own-team agents between agent and own goal
                                 // within COVERAGE_DEPTH_CORRIDOR_M [GT] lateral band
}
```

`coverageDepth` is used to select `COMMIT` vs `JOCKEY` vs `HOLD` mode
(§3.6). `approachAngle` is the secondary discriminator.

### 2.2.4 `MarkHysteresisState` (Stage 1; digested per KD-10)

One instance per outfield agent (maintained across ticks). Tracks how
long the agent has held its current `MarkAssignment` mode; gates
transitions per the #2 §3.1 dwell-time pattern (KD-11).

```
struct MarkHysteresisState
{
    int         dwellCounter;          // ticks remaining to retain current assignment (§3.11 pre-check)
    MarkMode    candidateMode;         // leading candidate being evaluated for transition
    EntityId?   candidateTargetId;     // leading candidate's opponent EntityId (null if ZONAL candidate)
    int         holdTicks;             // consecutive ticks the leading candidate has been preferred
}
```

`dwellCounter` is set to `MARK_DWELL_TICKS` on mode transition and decrements each tick;
the pre-check in §3.11 retains the current assignment while `dwellCounter > 0`.
`holdTicks` accumulates when the same candidate is consistently preferred; when
`holdTicks >= MARK_DWELL_TICKS`, the transition commits and `dwellCounter` is reset.
Emergency overrides (`ResetHysteresis` in §3.8/§3.9) zero both counters immediately.

### 2.2.5 `OffsideLineState` (Stage 1; digested per KD-10)

One instance per team (maintained across ticks). Tracks the offensive
line depth, the step-up trigger dwell, and the post-trap cooldown.

```
struct OffsideLineState
{
    float   currentLineDepth;          // x-coordinate of current effective line (m)
    int     stepUpDwellCounter;        // ticks the step-up trigger condition has been met
    int     cooldownTicksRemaining;    // post-trap cooldown; no new trap while > 0
    int     coverGkZoneActiveTicks;    // consecutive ticks COVER_GK_ZONE override has been active;
                                       // resets to 0 when GK returns to zone or max reached (§3.9)
}
```

`currentLineDepth` is updated each tick by reading #12's `DefensiveLineDepth`
(#14 does not compute this value). `stepUpDwellCounter` increments when the
offside-trap trigger condition holds and resets when it clears.
`cooldownTicksRemaining` is set to `OFFSIDE_RESET_COOLDOWN_TICKS [GT]` when a
trap fires and decrements each tick until zero. `coverGkZoneActiveTicks` increments
each tick the COVER_GK_ZONE override is active and resets to 0 when the GK returns
to the expected zone or when `COVER_GK_ZONE_MAX_TICKS [GT]` is reached (§3.9.3).

### 2.2.6 `BaselineDefensiveShapeView` (Stage 1; read-only)

A read-only view struct over #12's `BaselineDefensiveShape`, consumed by #14
at tick start. Exposes per-agent `formationSlot` (type `Vector2`),
`lineMembership` (type `LineMembership` enum), and `laneAssignment`. Also
provides the team-level `phase` enum and `defensiveLineDepth`. #14 never
writes through this view.

This struct's concrete field surface is declared in #12 §4.5.2; #14 cites it
here as a boundary declaration. At Stage 0 no accessor code exists.

### 2.2.7 `MarkDirectiveSnapshot` (Stage 1; read-only)

A read-only view over the current tick's `MarkDirective` and `MarkAssignment[]`
array, exposed for `MARK_ASSIGNED` / `LINE_STEPPED` channel emission (#17,
Stage 1) and for integration tests (§5). The snapshot carries the same fields
as `MarkDirective` plus the full per-agent `MarkAssignment[]` slice.

Channels are deferred — see §7.5, ERR-014-002, ERR-014-003.

### 2.2.8 `TackleDuelInputs` (Stage 0; new — KD-6 revised, `ERR-014-006`)

The inputs to one §3.6.5 tackle duel: the two players' relevant abilities,
normalized to `[0.0, 1.0]`, plus the geometry of the challenge. Constructed
by the composition root from the #7 perception snapshot and #14's own
`TackleIntentRequest.approachAngle`; consumed by `TackleOutcomeResolver`
(§2.2.9). Projected floats only — no roster or engine identity crosses
this boundary (reference-direction rule: Player Database #27 sits above
Mechanics).

```
readonly struct TackleDuelInputs
{
    float   tacklerTackling;    // [0,1] — tackler's Tackling attribute
    float   tacklerAggression;  // [0,1] — tackler's Aggression attribute
    float   carrierDribbling;   // [0,1] — carrier's Dribbling attribute
    float   carrierBalance;     // [0,1] — carrier's Balance attribute
    float   approachAngle;      // [0,π] rad — §2.2.3's approachAngle, reused
    float   reachFraction;      // [0,1] — tackler-to-BALL separation as a
                                 // fraction of the contact radius (§3.6.5.4)
}
```

`Tackling` and `Aggression` are read from the tackler; `Dribbling` and
`Balance` from the carrier (§3.6.5.6, doctrine §6 P3). `approachAngle` is
not a from-behind indicator — see §3.6.5.6's note on `TackleIntentRequest`'s
corrected XML doc.

### 2.2.9 `TackleOutcome` (Stage 0; new — KD-6 revised, `ERR-014-006`)

The result of one resolved tackle duel (§3.6.5.2). A four-value byte enum,
produced by `TackleOutcomeResolver.Resolve(TackleDuelInputs, float uniform)`
from a single caller-supplied uniform draw — the resolver holds no RNG
service state and draws no second stream. Ordinals are stable and
append-only: the outcome reaches match flow and is digest-visible.

```
enum TackleOutcome : byte
{
    Missed    = 0,   // challenge did not connect; possession unchanged; nothing emitted
    BallWon   = 1,   // tackler takes the ball cleanly and becomes the holder
    BallLoose = 2,   // carrier dispossessed; ball resolves via ordinary loose-ball paths
    Foul      = 3,   // challenge is a foul on the tackler; match-flow discipline path owns the card
}
```

Implemented in `src/defensive-ai/TackleDuelInputs.cs`, `TackleOutcome.cs`,
and `TackleOutcomeResolver.cs` (wiring backlog W2, August 12, 2026).

## 2.3 Inputs (Read-Only at Tick Start)

All inputs below are consumed as read-only values captured at the start of the
10 Hz tactical tick. No mid-tick re-reads occur (FR-DA-029 F1 / FR-DA-031 F3).

| Source | Field | Type | Notes |
|---|---|---|---|
| #7 Perception §3.7 | Per-agent positions | `Vector2[N]` | EntityId-keyed; N ≤ 22 (both teams) |
| #7 Perception §3.7 | Ball position | `Vector3` | Z-component available; #14 uses X,Y only |
| #7 Perception §3.7 | Ball velocity | `Vector3` | Magnitude used for offside-trap trigger (FR-DA-018) |
| #7 Perception §3.9 | Possession owner | `EntityId?` | `null` for loose ball |
| #7 Perception §3.10 | Per-agent `isActive` | `bool` | Substituted / red-carded agents excluded from pool |
| #7 Perception §3.7–3.10 | Per-opponent `FirstTouch` attribute | `float` (normalised [0,1]) | Threat-score numerator (FR-DA-017); normalised as `(attr−1)/19` |
| #7 Perception §3.7–3.10 | Per-opponent `Tackling` attribute | `float` (normalised [0,1]) | Declared for future tackle-quality use; not consumed by §3.6 algorithm at Stage 0 |
| #12 Positioning AI (BaselineDefensiveShapeView) | Per-agent `formationSlot` | `Vector2` | Baseline anchor for displacement cost and anti-chaos check |
| #12 Positioning AI (BaselineDefensiveShapeView) | `defensiveLineDepth` | `float` | Read-only (FR-DA-012); written to `MarkDirective.offensiveLineDepth` |
| #12 Positioning AI (BaselineDefensiveShapeView) | Team phase enum | `Phase` | Phase gating: `IN_POSSESSION` suppresses algorithm (FR-DA-013 / KD-19) |
| #12 Positioning AI (BaselineDefensiveShapeView) | Per-agent `lineMembership` | `LineMembership` | Anti-chaos invariant 1 backline count (FR-DA-025) |
| #13 Pressing AI | Per-agent `PressAssignment.role` | `PressRole` | Role partition: exclude `PRIMARY_PRESS` / `COVER_SHADOW` from pool (FR-DA-010) |
| #14-internal | Prior `MarkHysteresisState[N]` | struct array | From previous tick; hysteresis transitions (FR-DA-015) |
| #14-internal | Prior `OffsideLineState` | struct | From previous tick; step-up dwell and cooldown state (FR-DA-018) |

**Frame contract (`ERR-014-008`).** Every position, velocity and formation slot
in the §2.3 inputs above — the ball, the GK, and **both** teams' agents,
opponents included — is expressed in the **defending team's canonical
attack-toward-+X frame**: the goal #14 defends is at **x = 0** and the goal it
attacks is at x = 105, for the home team **and** for the away team. The
composition root performs the away team's 180° pitch rotation
(`x' = 105 − x`, `y' = 68 − y`, velocities negated) before filling the
snapshot, so the snapshot is **not** world space. The team id says who is
defending and which agents are own-team; it does **not** say which goal is
defended. **#14 is canonical-only:** own goal at x = 0 is a fixed invariant of
its input, not a switch, and #14 has no "defends x = 105" mode. A producer
holding world-space data must rotate it into this frame before filling the
snapshot; nothing in #14 can be told otherwise. Every `team.defendsX0` in §3
(threat proximity §3.5.2, runner direction §3.3.3 Step 3, last-man and
advancing-attacker selection §3.8, GK-zone centre §3.9.2, tackle coverage depth
§3.6.2, trap half test, line spread and step-up x §3.7) is therefore the
constant **true**: the code uses x directly and does not implement the
`x = 105` alternatives, which are the formulas' generic two-orientation
statement. (They survive only as `internal` test-pinned helpers,
`LastManDetector.DistToOwnGoal(x, defendsX0)` / `ComputeAbandonedZoneCenter(defendsX0)`, unreachable from production.) It is never inferred from the
team id.

*Worked example.* Away defender at world (90.0, 30.0) with a home striker at
world (80.0, 40.0). Canonical: defender (15.0, 38.0), striker (25.0, 28.0).
Striker `distToOwnGoal = 25.0 m` and `threat` proximity
`1 − 25/105 = 0.762` — identical to a home defender whose snapshot holds the
same two canonical points. Resolving the goal from the team id (`teamId == 0`)
gives the away team `distToOwnGoal = 105 − 25 = 80.0 m` and proximity `0.238`
— the inverted value this section corrects. *Valid range:* every x ∈ [0, 105],
y ∈ [0, 68]; units metres.

**`markMovementActive` (W13.1, `ERR-014-010`).** A `bool` per-tick input on the
snapshot, default `false`: the producer sets it when it will consume this tick's
assignments as per-agent mark targets (§3.13.1). It activates §3.10.2
Invariant 4 and nothing else. It is read within the tick only and is **not**
part of any serialized or digested state (the committed `MarkAssignment[]` and
`MarkHysteresisState[]` are unchanged in shape). While `false` the pipeline is
exactly the pre-W13.1 one.

**Note on GK exclusion:** The GK's EntityId is known via #7 perception
(`isGoalkeeper` flag or team roster). #14 filters it out before building
the HOLD_SHAPE pool (FR-DA-009). The GK's position is still readable via
the #7 snapshot for the `COVER_GK_ZONE` trigger evaluation (§3.9), but
the GK itself is never placed in the assignment pool.

## 2.4 Failure Modes and Recovery

Each failure mode below includes its detection condition, recovery action,
and a reference to the test that verifies it. Test IDs are assigned in §5.

---

**F1 — Stale Perception Snapshot**

- **Detection:** `perceptionSnapshot.tickIndex < currentTick`.
- **Recovery:** Reuse the previous tick's `MarkDirective` and entire
  `MarkAssignment[]` verbatim. Do not invoke the assignment algorithm.
  Emit a `dev-log` warning with the delta `(currentTick − snapshot.tickIndex)`.
- **Test:** T-DA-F1-STALE (unit test; §5.2).

---

**F2 — #12 Slot Unavailable**

- **Detection:** `BaselineDefensiveShapeView.GetSlot(agent) == SENTINEL_NO_SLOT`
  for any agent in the initial HOLD_SHAPE pool (i.e., #12 is emitting
  `SENTINEL_NO_SLOT` for at least one agent this tick).
- **Recovery:** Emit an all-`ZONAL` `MarkDirective` for this tick without
  invoking the assignment algorithm. A single missing slot is sufficient
  to trigger this fallback because the displacement-cost baseline is
  undefined without an anchor. Do not preserve partial assignments.
- **Test:** T-DA-F2-SENTINEL (unit test; §5.2).

---

**F3 — #13 Directive Unavailable**

- **Detection:** The `PressAssignment` array for this team is absent,
  null, or has `tickIndex < currentTick - 1` (more than one tick stale).
- **Recovery:** Treat all outfield non-GK agents as `HOLD_SHAPE` for this
  tick (maximum pool). This is the safe fallback because it errs toward
  more coverage rather than less. Emit a `dev-log` warning
  `PRESS_DIRECTIVE_ABSENT`.
- **Test:** T-DA-F3-NOPRESS (unit test; §5.2).

---

**F4 — Anti-Chaos Invariant Violation at Publication**

- **Detection:** After the iterative demotion loop in §3.10, one or more
  invariants remain violated (i.e., no further cost-ranked demotion is
  available to fix the directive).
- **Recovery:** Discard the entire candidate directive and emit an all-`ZONAL`
  `MarkDirective` for this tick. Emit a `dev-log` warning
  `DEFENSIVE_INVARIANT_FALLBACK`. This situation should be extremely rare
  in production; its occurrence in testing indicates a logic error in
  the invariant-enforcement loop.
- **Test:** T-DA-F4-INVARIANT (unit test; §5.2).

---

**F5 — Last-Man Predicate Tie**

- **Detection:** Two or more outfield agents share the same minimum
  `distanceToOwnGoal` value at the tick when `IsLastManThreat` is also
  true, and there is no unique candidate.
- **Recovery:** Resolve deterministically by selecting the agent with the
  lowest EntityId value (ascending). This follows the EntityId tie-break
  rule established by #16 §3.2.5 and referenced as `XC-014-002`.
- **Test:** T-DA-F5-LASTMAN-TIE (unit test; §5.2).

---

Substituted and red-carded agents are filtered upstream via `isActive` from
the #7 perception snapshot. Their last `MarkAssignment` is preserved at its
pre-substitution value (consistent with the F2 sentinel behaviour in #12
and F6 in #13 §2.4). No special failure mode is required because the
`isActive` filter gates pool membership before any other logic runs.

## 2.5 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-defensive-ai) | Initial draft from `outline-detailed.md` v1.0. §2.1 (37 FRs FR-DA-001..037), §2.2 (7 structs), §2.3 (inputs table), §2.4 (F1–F5 failure modes) authored. Data structure field definitions follow `pressing-ai/section-2.md` v0.2 readonly-struct convention. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. H2: `MarkAssignment` struct (§2.2.2) now declares `overriddenThisTick` and `isManuallyAssigned` fields that were used in §3 algorithms but absent from struct definition; `targetEntityId` comment clarified to "null for ZONAL and COVER_GK_ZONE". H4: `MarkHysteresisState` struct (§2.2.4) rewritten to four-field definition matching §3.11.2 (`dwellCounter`, `candidateMode`, `candidateTargetId`, `holdTicks`); v0.1 had only `currentMode` + `dwellCounter`. M2: `OffsideLineState` struct (§2.2.5) now declares `coverGkZoneActiveTicks` field used in §3.9.2/§3.13 but missing from struct. M3: §2.3 inputs table row "Per-agent FirstTouch" corrected to "Per-opponent FirstTouch" (#14 reads opponents, not own-team agents). M6: `Tackling` description in §2.3 clarified to "Declared for future tackle-quality use; not consumed by §3.6 algorithm at Stage 0". |
| 0.3 | August 12, 2026 | AI agent (wiring backlog W2) | KD-6 revised (`ERR-014-006`): §2.2.3 `TackleIntentRequest`'s "read by #8, which translates it into an `AgentAction` dispatched to Collision System #3" clause corrected — that dispatch has no working delegate; a committed intent is now resolved by #14 itself. Added §2.2.8 `TackleDuelInputs` and §2.2.9 `TackleOutcome`, the two new public types backing §3.6.5's tackle-outcome resolution. Implemented in `src/defensive-ai/TackleDuelInputs.cs`, `TackleOutcome.cs`, `TackleOutcomeResolver.cs`. |
| 0.4 | November 10, 2026 | AI agent (W13.0) | `ERR-014-008`: §2.3 gains the "Frame contract" paragraph and worked example. The snapshot is the defending team's canonical attack-toward-+X frame for BOTH teams (own goal at x = 0), as `MatchEngine.FillDefensiveSnapshot` has always supplied it; `defendsX0` is read from the frame, never from the team id. §3's `team.defendsX0` pseudocode is annotated to match (section-3 v0.6). Code: `DefensiveSnapshot.IsOwnGoalAtX0`; `LastManDetector`, `MarkAssigner`, `TackleIntentEvaluator`, `OffsideTrapController`, `InvariantEnforcer`. |
| 0.5 | November 10, 2026 | AI agent (W13.0) | `ERR-014-009`: §2.2.2 gains "Retained assignments" — a retained `MAN_MARK` / `INTERCEPT_RUNNER` refreshes `targetPosition` from the live snapshot every tick, and a retained target that is absent or inactive releases the mark to ZONAL at once (hysteresis reset). Matches the §2.2.2 sentence that `targetPosition` is the opponent's current perceived position, which §3.3.3 Step 1 / §3.11.3 contradicted by retaining the whole record (section-3 v0.7). Code: `MarkAssigner.RefreshRetained`. |
| 0.6 | November 10, 2026 | AI agent (W13.0 review M1/L2) | §2.3 Frame contract reworded: #14 is canonical-only, the `x = 105` alternatives are not implemented, the "one place to change" claim and the `DefensiveSnapshot.IsOwnGoalAtX0` accessor are withdrawn (supersedes v0.4's wording). §2.2.2 Retained assignments: the vanished-target release resets the candidate accumulator (full `MARK_DWELL_TICKS` to re-acquire); covered by `VanishedTarget_ApplyGatePath_ReleasesAndResetsCandidateAccumulator`. |
