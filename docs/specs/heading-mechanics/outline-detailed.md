# Heading Mechanics Specification #10 — Detailed Outline

**Purpose:** Section-by-section draft plan for Heading Mechanics #10.
For every subsection: the rules/formulas/data it will publish, the
upstream citations it will carry, and the cross-references it will
emit. Detailed enough that section files (`section-1.md` …
`section-9-approval-checklist.md`, `appendices.md`) can be drafted
mechanically from this document, with no further outlining work
required.

This document is **not normative** — FR text, formulas, and constant
values land in the section files. Detailed outline records intent,
provenance, and structural mapping only.

**Created:** May 15, 2026, 11:00 PM PST
**Version:** 1.0
**Status:** DRAFT — supersedes `outline.md` v0.1 (May 6, 2026); resolves
all 22 findings of the May 6, 2026 adversarial review attached to that
file.
**Specification Number:** 10 of 20 (Stage 0, Priority 3)
**Estimated Effort:** ~28 hours (section files), ~6 hours (pass-1
adversarial critique), ~4 hours (pass-2 fix cycle).
**Companion documents:** `outline.md` (high-level v0.1 with
adversarial-review appendix — retained for history; do not edit).

**Dependencies (all APPROVED):**
- Ball Physics #1 (incoming `BallState`; `Ball.ApplyKick` output
  surface; coordinate-system authority).
- Agent Movement #2 (jump kinematics; `Heading`, `Strength`,
  `Balance` attributes; `AgentPhysicalProperties`).
- Collision System #3 (head-ball contact resolution; contested-duel
  contact data).
- First Touch Mechanics #4 (boundary partner — head-vs-non-head body
  part discrimination authority).
- Pass Mechanics #5 (canonical cross-delivery source; consumed
  read-only via `BallState`, not via Pass-specific labels).
- Shot Mechanics #6 (analogous output-interface model; KD-6 body-part
  routing authority establishing that ALL head contacts route here
  regardless of height).
- Perception System #7 (Decision Tree input provenance; not a direct
  Heading dependency, cited for tractability).
- Decision Tree #8 (intent parameters: header target, power intent,
  contact-point intent).
- Deterministic Simulation #16 (RNG governance for tie-breaks;
  iteration-order discipline; `DOMAIN_TAG` allocation for
  `DeterministicRngService`).
- Event System #17 (`HeaderExecutedEvent` consumer; own-goal-shaped
  trajectory adjudication).

**Downstream (consumers; specs NOT STARTED — interface declared here,
not negotiated):**
- Goalkeeper Mechanics #11 — consumes `HeaderExecutedEvent` for
  reaction-trigger; GK head-contact ownership delineated in KD-7.
- Positioning AI #12 — consumes `HeaderExecutedEvent` for marking
  re-acquisition; no interface surface beyond the event.
- Defensive AI #14, Attacking AI #15 — consume aggregate header
  statistics; no per-call interface.

**Pass Mechanics #5 status note:** the May 6 review was filed when
#5 was SUSPENDED. #5 re-approved May 6, 2026 (same day, later). The
SUSPENDED-risk mitigation (consume only `BallState`-level data, not
Pass-specific labels) is retained as KD-5 because it is sound
independent of #5 status.

---

## EXECUTIVE SUMMARY

Heading Mechanics governs every ball contact made with the head —
from the moment an agent commits to a header until the ball leaves
the head. Per Shot Mechanics #6 KD-6, contact body part is the sole
discriminator; the First Touch #4 0.5 m height threshold does NOT
gate head contacts. Diving headers, bicycle-kick headers, and
head-on-ground contacts therefore route here, not to First Touch.

The core model:

```
HeaderResult = f(PowerIntent, ContactPointIntent, TargetIntent, IncomingBall)
             × ContactQualityScalar
             × BodyMechanics
             × Fatigue
```

Where:
- `PowerIntent ∈ [0,1]` — how hard the agent attempts to strike (from
  Decision Tree #8).
- `ContactPointIntent` — intended contact location on the head
  (forehead-centre, forehead-edge, temple); a continuous 2-D
  parameter, not an enum.
- `TargetIntent` — intended ball destination (corner-origin
  coordinate or angle-from-agent).
- `IncomingBall` — `BallState` snapshot at predicted contact frame
  (position, velocity, spin) from Ball Physics #1.
- `ContactQualityScalar ∈ [0,1]` — continuous quality derived from
  contact-timing offset (signed ms vs. ideal-contact frame) and
  contact-point error; named windows (early/perfect/late) are
  telemetry labels emitted from the scalar, NOT gates on the formula.
- `BodyMechanics` — jump phase, body orientation, vertical reach,
  duel-disturbance factor.
- `Fatigue ∈ [0,1]` — `0 = rested, 1 = fatigued` (CLAUDE.md
  convention; pre-committed in KD-9).

Physical output — velocity magnitude, launch angle, and spin vector
— emerges from these parameters. Named outcome labels (powered
header, glancing header, defensive clearance) are downstream
telemetry, NOT inputs.

**Output interface (mirrors Shot Mechanics #6 §4):**
```
Ball.ApplyKick(velocity: Vector3, spin: Vector3, agentId: int, matchTime: float)
HeaderExecutedEvent { agentId, matchTime, contactQuality, contactPoint,
                      incomingBallState, outgoingVelocity, outgoingSpin,
                      contestedDuelId?, ownGoalShapedTrajectory: bool, ... }
```

A header attempt that misses the ball entirely (mistimed jump, ball
passes through the head zone untouched) emits a
`HeaderAttemptFailedEvent` and does NOT invoke `Ball.ApplyKick`.
Ball trajectory is unchanged.

---

## KEY DESIGN DECISIONS (pre-committed)

Each KD is restated at the top of `section-1.md` §1.3 with rationale
and consequence-if-violated. Pre-committing here removes ambiguity
during drafting.

- **KD-1 — Parameter-based contact model (no header-type enum).** No
  `HeaderType` / `HeaderClass` / `HeaderStyle` enum is introduced at
  any layer. Decision Tree #8 supplies physical intent
  (`PowerIntent`, `ContactPointIntent`, `TargetIntent`); physics
  produces vectors; named outcomes are telemetry only. Same trap
  closed for #5 (`KickType`) and #6 (`ShotType`). **Resolves
  adversarial findings 3, 4.**

- **KD-2 — Continuous contact-quality scalar.** Contact quality is a
  continuous scalar ∈ [0,1] derived from a signed timing offset
  (frames or ms relative to ideal-contact frame) and a 2-D contact-
  point error (m) on the head surface. Named windows (early /
  perfect / late) are post-computation labels emitted into
  `HeaderExecutedEvent.contactQualityLabel` for telemetry, NEVER
  branched on by the physics formula. **Resolves finding 3.**

- **KD-3 — Body-part discriminator inheritance.** Routing inherits
  from Shot Mechanics #6 KD-6: any ball contact whose contact body
  part is `Head` routes to Spec #10, regardless of ball height or
  agent posture. The First Touch #4 0.5 m height threshold does NOT
  apply to head contacts. Spec #10 owns this rule definitively;
  Spec #4 will continue to gate non-head body parts on the 0.5 m
  threshold. **Resolves finding 15.**

- **KD-4 — `JumpReach` is `[DERIVED]`, not a new `PlayerAttribute`.**
  No revision to APPROVED Agent Movement #2 is required. Spec #10
  defines `JumpReach = f(Strength, Balance, Heading)` with a
  `[DERIVED]` tag (formula candidate: `JumpReach_m = base +
  k_strength · Strength_norm + k_balance · Balance_norm`, where
  `base` is a `[FIXED]` anatomical baseline and `k_*` are `[GT]`).
  Worked example and sensitivity analysis live in Appendix B.
  Reach is computed once per jump phase; not per-tick. **Resolves
  finding 5; preserves AM #2 APPROVED status.**

- **KD-5 — Pass Mechanics #5 consumed via `BallState` only.** Spec
  #10 reads incoming `BallState.velocity` / `.spin` / `.position` at
  the predicted contact frame. It does NOT consume Pass-specific
  labels (`CrossDelivery`, `LowDriven`, `ChippedCross`). This
  insulates #10 from any future amendment to #5. **Resolves finding
  8** (and remains valid now that #5 is APPROVED).

- **KD-6 — Own-goal detection is NOT adjudicated here.** Spec #10
  produces an outgoing velocity vector and flags
  `HeaderExecutedEvent.ownGoalShapedTrajectory` when the outgoing
  velocity's projection through the corner-origin coordinate space
  intersects the defender's own goal-line bounding box within the
  next ballistic phase. Whether a goal was actually scored is an
  Event System #17 / Match Referee concern. **Resolves finding 9.**

- **KD-7 — Goalkeeper head-contact ownership.** GK head contacts
  (punching, headed goal-line clearance, goal-line head-on-ground
  contact) execute the Heading Mechanics #10 pipeline. Goalkeeper
  Mechanics #11 (NOT STARTED) may override `ContactPointIntent` /
  `PowerIntent` derivation via GK-specific Decision Tree branches
  but inherits #10's physics layer unchanged. This pre-commitment
  prevents re-litigation when #11 is drafted. **Resolves finding
  16.**

- **KD-8 — Contested duel consumes Collision System #3, does not
  redefine it.** Duel resolution reads contact data emitted by
  Collision System #3 (contact normal, relative velocity, impulse
  budget). Spec #10 layers a Heading-specific resolution on top
  (Balance/Strength tie-break, disturbance-factor application to
  `ContactQualityScalar`). Spec #3 contact-event interface is
  consumed as-published; no #3 revision required. **Resolves
  finding 17.**

- **KD-9 — Project-invariants citation block.** Spec #10 cites and
  does not restate: corner-origin coordinates (Ball Physics #1
  §1.2); fatigue `0=rested, 1=fatigued` (CLAUDE.md); tick-rate split
  (10 Hz tactical for intent selection; 60 Hz physics for jump
  kinematics, contact resolution, ball-velocity emission;
  CLAUDE.md). Pre-committing this block here prevents drift.
  **Resolves findings 11, 12, 13.**

- **KD-10 — Determinism governance.** All randomness routes through
  `DeterministicRngService` (Deterministic Simulation #16 §4.1)
  with registered draw-site IDs (#16 §4.5). Iteration over duel
  participants follows #16 §3.2 entity-ordering. A new `DOMAIN_TAG`
  allocation (`DOMAIN_TAG_HEADING = 0x??`) is requested from #16
  §3.4 via a back-propagation entry filed under `ERR-010-001`
  (created during drafting); promotion of the `[CROSS-PENDING]` tag
  to `[CROSS]` is atomic with the back-prop landing. **Resolves
  finding 10.**

- **KD-11 — Constant-tag policy.** Every numeric constant published
  by Spec #10 carries exactly one of `[GT] / [EST] / [FIXED] /
  [DERIVED] / [CROSS] / [CROSS-PENDING]`. §9 Approval Checklist
  programmatically verifies every constant in `section-3.md` and
  `appendices.md` against this rule before approval. No magic
  numbers in formula code. **Resolves finding 14; closes ERR-005
  fabricated-checklist trap class.**

- **KD-12 — Failed-attempt physics is well-defined.** A header
  attempt that fails contact (jump mistimed; ball passes through
  head zone within tolerance but not touched) produces: NO
  `Ball.ApplyKick` call; ball trajectory unchanged; a
  `HeaderAttemptFailedEvent` published with timing and miss-distance
  telemetry. **Resolves finding 19.**

- **KD-13 — Set-piece headers ARE in Stage 0 scope.** The cross
  (free kick, corner) is delivered by Pass Mechanics #5; the header
  off that cross is mechanically identical to an open-play header
  from a cross. Spec #10 covers both. Set-piece taking (the kick
  itself) remains deferred to Stage 1+ per Shot Mechanics #6 §1.2.
  **Resolves finding 20.**

- **KD-14 — Weak-aerial-side handling deferred to Stage 1+.** No
  `WeakAerialSide` attribute is introduced at Stage 0. §7 records
  the deferral with rationale: validation data for the asymmetry
  premise is unavailable; introducing it pre-data would be `[EST]`
  with no upgrade path. **Resolves finding 18.**

- **KD-15 — Concussion / injury modeling deferred to Stage 1+.** No
  injury-system spec exists in the 20-spec set. §7 records the
  deferral pointing forward to a future Medical/Injury spec.
  **Resolves finding 21.**

- **KD-16 — Spin transfer is Heading-owned.** Outgoing spin
  computation lives in Spec #10 §3.6 (not Ball Physics #1). Ball
  Physics receives the final spin vector via `Ball.ApplyKick`;
  Spec #10 is responsible for transforming incoming spin + contact
  geometry + head angular velocity into outgoing spin. Rationale:
  spin transfer depends on contact-point and head-velocity vector
  which only #10 knows. **Resolves finding 22.**

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~50 lines.

**Content:**
- Opening declarative scope statement.
- Bullet list of governance areas (7 items): eligibility, jump
  kinematics integration, contact-quality computation, outcome
  generation (velocity / spin / launch angle), contested-duel
  resolution, failed-attempt handling, telemetry surface.
- Applicability block: every ball contact whose contact body part is
  `Head` (per Shot #6 KD-6 / KD-3 here); covers open play, set-piece
  receptions, defensive clearances, attacking finishes.
- Closing pointer to §1.2 (out-of-scope), §1.3 (KDs), §1.4
  (dependencies).

### 1.2 What Is Out of Scope

**Subsection target length:** ~30 lines.

**Content (one-line entries with owning document):**
- Goal detection (own-goal or otherwise) → Event System #17 / Match
  Referee.
- Set-piece kick delivery (the kick itself) → Spec #5 (Pass) or
  Stage 1+ set-piece spec.
- Goalkeeper-specific decision logic (when to punch vs. catch) →
  Goalkeeper Mechanics #11; physics layer remains #10.
- Concussion / injury accumulation → Stage 1+ Medical spec.
- Weak-aerial-side asymmetry → Stage 1+ (KD-14).
- Header-pass labelling (was this a clearance, flick-on, knock-down?)
  → telemetry classifier downstream of #10; not a physics input.

### 1.3 Key Design Decisions

**Subsection target length:** ~180 lines.

Sixteen KDs (KD-1 … KD-16) reproduced from the KEY DESIGN DECISIONS
block above, each formatted as: statement (1 sentence), rationale
(2–3 sentences), consequence-if-violated (1 sentence). KD numbering
is canonical for the spec and cited by FR rows in §2.

### 1.4 Dependencies and Integration Contracts

**Subsection target length:** ~60 lines.

**Content:**
- Upstream table (10 rows: #1, #2, #3, #4, #5, #6, #7, #8, #16, #17),
  each row naming the consuming subsection of #10 and the
  section-level citation in the upstream spec. Format mirrors Shot
  Mechanics #6 §2.5.
- Downstream table (4 rows: #11, #12, #14, #15) — interface surface
  is `HeaderExecutedEvent` + `HeaderAttemptFailedEvent` only.
- Pass Mechanics #5 amendment-insulation note (KD-5).
- Goalkeeper #11 / Positioning AI #12 / Defensive AI #14 / Attacking
  AI #15 NOT-STARTED-status note: interface declared here, not
  negotiated; downstream specs consume as-published.

### 1.5 Version History

Standard 5-column table (Version | Date | Author | Notes | Reviewer).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES & FAILURE MODES (`section-2.md`)

### 2.1 Functional Requirements Catalogue

**Subsection target length:** ~140 lines.

`FR-HE-001` … `FR-HE-NN` table. Each row: ID, statement, conformance
level (MUST / SHOULD / MAY), source KD, target subsection.
Projected count: ~35 FRs.

Anchor FRs to write first:
- `FR-HE-001` (MUST) — Eligibility: a header is eligible iff agent
  is in `Jumping` or `Standing-High-Reach` aerial phase AND ball
  position within `HEAD_CONTACT_VOLUME` AND contact body part
  predicted to be `Head`. Source: KD-3.
- `FR-HE-002` (MUST) — Contact quality is computed as a continuous
  scalar; no branching on early/perfect/late labels. Source: KD-2.
- `FR-HE-003` (MUST) — No `HeaderType`/`HeaderClass` enum at any
  layer. Source: KD-1.
- `FR-HE-004` (MUST) — `JumpReach` is `[DERIVED]`; no new
  `PlayerAttribute` on Agent Movement #2. Source: KD-4.
- `FR-HE-005` (MUST) — Pass Mechanics #5 inputs consumed via
  `BallState` only. Source: KD-5.
- `FR-HE-006` (MUST) — Failed attempt → no `Ball.ApplyKick`; emit
  `HeaderAttemptFailedEvent`. Source: KD-12.
- `FR-HE-007` (MUST) — Own-goal-shaped trajectory FLAGGED, NOT
  adjudicated. Source: KD-6.
- `FR-HE-008` (MUST) — All randomness via `DeterministicRngService`
  with registered draw-site IDs. Source: KD-10.
- `FR-HE-009` (MUST) — GK head contacts execute the #10 pipeline.
  Source: KD-7.
- `FR-HE-010` (MUST) — Contested duel consumes #3 contact data; no
  redefinition. Source: KD-8.
- `FR-HE-011` (MUST) — Fatigue convention `0=rested, 1=fatigued`.
  Source: KD-9.
- `FR-HE-012` (MUST) — Corner-origin coordinates. Source: KD-9.
- `FR-HE-013` (MUST) — Tick-rate split: 10 Hz intent / 60 Hz
  physics. Source: KD-9.
- `FR-HE-014` (MUST) — Every published constant carries a source
  tag. Source: KD-11.
- `FR-HE-015` (MUST) — Outgoing spin computed by #10; passed via
  `Ball.ApplyKick`. Source: KD-16.
- `FR-HE-016` (MUST) — Set-piece headers covered at Stage 0; the
  kick itself is not. Source: KD-13.
- `FR-HE-017` (SHOULD) — Iteration over contested-duel participants
  follows #16 §3.2 ordering.
- (~18 more FRs covering specific formula behaviors, telemetry
  contracts, edge cases, and Stage 1+ deferrals.)

### 2.2 Data Structures

**Subsection target length:** ~120 lines.

Structs to define (struct-based, zero-allocation per CLAUDE.md):
- `HeaderIntent` — Decision Tree #8 output consumed by #10.
  Fields: `powerIntent: float`, `contactPointIntent: Vector2`
  (head-local coordinates), `targetIntent: Vector3` (corner-origin),
  `attemptCommittedTick: int` (10 Hz tick at which Decision Tree
  committed).
- `HeaderContactState` — internal per-frame structure during
  60 Hz contact resolution. Fields: `predictedContactFrame: int`,
  `actualContactFrame: int`, `timingOffsetMs: float`,
  `contactPointError: Vector2`, `contactQualityScalar: float`,
  `disturbanceFactor: float`.
- `HeaderExecutedEvent` — published on every contacted header.
  Fields (mirrors Shot Mechanics #6 §4.5 / `ShotExecutedEvent`):
  `agentId`, `matchTime`, `contactQualityScalar`,
  `contactQualityLabel` (telemetry enum: Early / Perfect / Late —
  emitted, not consumed), `contactPoint`, `incomingBallState`,
  `outgoingVelocity`, `outgoingSpin`, `contestedDuelId: int?`,
  `ownGoalShapedTrajectory: bool`, `setPieceContext: enum?`
  (OpenPlay / Corner / FreeKick — telemetry only).
- `HeaderAttemptFailedEvent` — published on missed-contact. Fields:
  `agentId`, `matchTime`, `missDistanceM`, `timingOffsetMs`,
  `failureCause: enum` (MistimedEarly / MistimedLate /
  PositionedPoorly / DisturbedInDuel).
- `ContestedDuelContext` — populated when ≥2 agents are within
  `HEAD_CONTACT_VOLUME` simultaneously. Fields: `duelId`,
  `participantAgentIds: ReadOnlySpan<int>`, `winnerAgentId`,
  `disturbanceFactorByAgent`.

### 2.3 Failure Modes

**Subsection target length:** ~80 lines.

Catalogue of expected failure modes, each with detection rule,
recovery behavior, and telemetry tag:
- F-01: Mistimed jump (ball passed contact volume before jump
  apex). Detection: `timingOffsetMs > MAX_LATE_TOLERANCE_MS`.
  Recovery: `HeaderAttemptFailedEvent`. KD-12.
- F-02: Jump apex below ball altitude. Detection: `JumpReach <
  ballZ_at_contactFrame`. Recovery: `HeaderAttemptFailedEvent`.
- F-03: Contact body part is `Head` but ball position outside
  `HEAD_CONTACT_VOLUME` at all frames in attempt window.
  Recovery: `HeaderAttemptFailedEvent`.
- F-04: Two simultaneous eligible headers (contested duel) →
  resolved per §3.7, NOT a failure; emits both
  `HeaderExecutedEvent` (winner) and `HeaderAttemptFailedEvent`
  (losers with `failureCause=DisturbedInDuel`).
- F-05: Decision Tree #8 supplied a `targetIntent` outside the
  pitch bounding box. Recovery: clamp to nearest in-bounds point;
  emit telemetry warning; NOT a hard failure.
- F-06: `BallState` snapshot stale (>1 physics frame old).
  Recovery: re-query Ball Physics #1; do not extrapolate.
- F-07: `contactPointIntent` outside head-local coordinate envelope
  (forehead / temple range). Recovery: clamp to envelope edge;
  apply contact-quality penalty per §3.4.

### 2.4 Telemetry Surface

**Subsection target length:** ~40 lines.

Counters and gauges emitted on the trace pipeline (Performance
Optimization #18 §3.10 channel registry — cited; channel rows
allocated via #18 §3.10 back-prop):
- `heading.contact.quality.scalar` (histogram).
- `heading.contact.quality.label` (counter, 3 buckets: Early /
  Perfect / Late).
- `heading.duel.outcome` (counter, win/loss/disturbed).
- `heading.attempt.failed.cause` (counter, 4 buckets).
- `heading.own_goal_shaped.flag` (counter).

---

## SECTION 3 — CORE FORMULAS, ALGORITHMS, PSEUDOCODE (`section-3.md`)

**Subsection target length:** ~600 lines (largest section).

### 3.1 Master Physical Profile Table

Per-row constants table (mirrors Pass Mechanics #5 §3.1.4 structure
post-F-A01 fix): one column per tunable constant, source-tag column,
unit column, valid-range column, citation column.

Constants to enumerate (~28 rows):
- `HEAD_CONTACT_VOLUME_RADIUS_M` `[GT]` (0.18 m candidate).
- `HEAD_CONTACT_VOLUME_HEIGHT_M` `[GT]`.
- `MAX_EARLY_TOLERANCE_MS` `[GT]`.
- `MAX_LATE_TOLERANCE_MS` `[GT]`.
- `IDEAL_CONTACT_FRAME_OFFSET` `[DERIVED]` from jump apex (#2).
- `JUMP_REACH_BASE_M` `[FIXED]` (anatomical).
- `JUMP_REACH_K_STRENGTH` `[GT]`.
- `JUMP_REACH_K_BALANCE` `[GT]`.
- `POWER_BASE_MPS` `[GT]`.
- `POWER_K_STRENGTH` `[GT]`.
- `POWER_K_HEADING` `[GT]`.
- `POWER_FATIGUE_COEFF` `[GT]`.
- `CONTACT_POINT_ERROR_SIGMA_M` `[GT]`.
- `CONTACT_POINT_HEADING_ATTR_COEFF` `[GT]`.
- `GLANCING_ANGLE_THRESHOLD_RAD` `[GT]` (telemetry-only; not a gate
  per KD-1/KD-2).
- `SPIN_TRANSFER_COEFF` `[GT]`.
- `SPIN_TRANSFER_REVERSAL_THRESHOLD` `[GT]`.
- `DUEL_BALANCE_WEIGHT` `[GT]`.
- `DUEL_STRENGTH_WEIGHT` `[GT]`.
- `DUEL_HEADING_WEIGHT` `[GT]`.
- `DUEL_DISTURBANCE_MAX` `[GT]`.
- `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` `[GT]`.
- `GRAVITY_MPS2` `[CROSS]` (Ball Physics #1).
- `PITCH_LENGTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `PITCH_WIDTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `DOMAIN_TAG_HEADING` `[CROSS-PENDING]` (Deterministic Simulation
  #16 §3.4 — back-prop ERR-010-001).
- `TICK_RATE_TACTICAL_HZ` `[CROSS]` (CLAUDE.md).
- `TICK_RATE_PHYSICS_HZ` `[CROSS]` (CLAUDE.md).

### 3.2 Eligibility Predicate

**Inputs:** agent kinematic state (Agent Movement #2), `BallState`
(Ball Physics #1), `HeaderIntent` (Decision Tree #8).
**Output:** `bool isEligible`, `int predictedContactFrame`.

Pseudocode covering: aerial-phase check, predicted contact body
part = `Head`, ball trajectory intersects `HEAD_CONTACT_VOLUME`
within attempt-window frames. Worked example: corner cross at
8 m/s; defender jump-committed at tick T; predicted contact at
frame T+9.

### 3.3 Jump Kinematics Integration

`JumpReach` `[DERIVED]` formula (KD-4):
```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
```

Coupling to Agent Movement #2 jump trajectory: jump apex frame
computed by #2; #10 reads apex-frame `agentZ` and adds anatomical
head-above-COM offset to determine `ballZ` reachable.

Worked example with sensitivity analysis (Appendix B).

### 3.4 Contact-Quality Scalar (KD-2)

```
timingOffsetMs = (actualContactFrame - idealContactFrame) · FRAME_MS
timingQuality  = 1 - clamp01(|timingOffsetMs| / MAX_TOLERANCE_MS)
pointError     = ||contactPointActual - contactPointIntent||
pointQuality   = 1 - clamp01(pointError / CONTACT_POINT_ERROR_SIGMA_M)
contactQualityScalar = α · timingQuality + (1-α) · pointQuality
```
where `α = [GT]`. Telemetry label assignment:
- `Early` if `timingOffsetMs < -EARLY_LABEL_THRESHOLD_MS`.
- `Late` if `timingOffsetMs > +LATE_LABEL_THRESHOLD_MS`.
- `Perfect` otherwise. **Labels are NEVER consumed by §3.5–§3.7.**

### 3.5 Power & Launch-Angle Generation

```
EffectiveAttribute = Heading_norm × (1 - POWER_FATIGUE_COEFF · fatigue)
PowerMps           = POWER_BASE_MPS
                   + POWER_K_STRENGTH · Strength_norm
                   + POWER_K_HEADING  · EffectiveAttribute
outgoingSpeed      = PowerMps · PowerIntent · contactQualityScalar
launchAngle        = f(contactPointActual, headVelocityVector, incomingBallVelocity)
```

Launch-angle derivation: reflection-style geometry off the head
contact point, modulated by head angular velocity. Worked example:
header from a 20 m corner with `PowerIntent=0.8`, contact-quality
0.92 → `outgoingSpeed ≈ 14 m/s`, launch angle ~12° above horizontal.

### 3.6 Spin Transfer (KD-16)

```
outgoingSpin = SPIN_TRANSFER_COEFF · headAngularVelocity
             + (incomingSpin · spinPreservationFactor)
             - reversalTerm
```
where `spinPreservationFactor` depends on contact-point offset from
head-rotation axis. Worked example: incoming topspin 8 rad/s,
contact-point 0.02 m offset → outgoing backspin 3 rad/s (reversal).

### 3.7 Contested Duel Resolution (KD-8)

Inputs: Collision System #3 contact-event list at the candidate
contact frame; participating agents within `HEAD_CONTACT_VOLUME`.
Algorithm:
1. Iterate participants in #16 §3.2 entity order.
2. Compute `duelScore = w_B·Balance + w_S·Strength + w_H·Heading
   + 0.01 · rng.NextFloat(DRAW_SITE_DUEL_TIEBREAK)` — RNG draw site
   registered with #16 §4.5.
3. Highest scorer wins; emits `HeaderExecutedEvent`. Losers receive
   `disturbanceFactor ∈ [0, DUEL_DISTURBANCE_MAX]` applied to their
   `contactQualityScalar`; if reduced below `MIN_CONTACT_QUALITY`
   they emit `HeaderAttemptFailedEvent` instead.
4. Multi-way (3+) duels: winner-only emits `HeaderExecutedEvent`;
   all losers emit failed events.

Worked example: two strikers + one defender contesting a corner;
defender wins with `duelScore = 0.72` vs. strikers' 0.65, 0.61.

### 3.8 Own-Goal-Shape Flag Computation (KD-6)

```
flag = projectTrajectory(outgoingVelocity, contactPosition,
                          OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S)
       intersects ownGoalBoundingBox(agent.team)
```
Flag is published; not adjudicated.

### 3.9 Failed-Attempt Pipeline (KD-12)

Pseudocode for failed-attempt emission: no `Ball.ApplyKick`; ball
state unchanged; `HeaderAttemptFailedEvent` published with
`failureCause` from F-01…F-04 / F-07.

### 3.10 Boundary Algorithms

- Boundary with First Touch #4 (KD-3): single-paragraph statement
  + pseudocode predicate `if contactBodyPart == Head → Spec #10`.
- Boundary with Goalkeeper #11 (KD-7): GK identifier check;
  pipeline executed unchanged.
- Boundary with Collision System #3 (KD-8): contact-event API
  consumed read-only.

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS (`section-4.md`)

**Subsection target length:** ~250 lines.

### 4.1 File Layout (under `src/Gameplay/Heading/`)

- `HeadingMechanics.cs` — orchestrator; consumed by simulation
  scheduler at 60 Hz physics tick.
- `HeadingConstants.cs` — every constant from §3.1 with its source
  tag; no magic numbers in formula files (KD-11).
- `HeadingEligibility.cs` — §3.2.
- `HeadingJumpKinematics.cs` — §3.3.
- `HeadingContactQuality.cs` — §3.4.
- `HeadingPowerAngle.cs` — §3.5.
- `HeadingSpinTransfer.cs` — §3.6.
- `HeadingDuelResolution.cs` — §3.7.
- `HeadingTelemetry.cs` — §2.4 surface emission.
- Test layout: `tests/Gameplay/Heading/` with one file per
  `HeadingMechanics.cs` source file (Spec #19 §3.x).

### 4.2 Input Interface Contracts

Method signatures (consumed):
- `BallPhysics.GetBallState(matchTime) → BallState` — Spec #1.
- `AgentMovement.GetKinematicState(agentId, frame) →
  KinematicState` — Spec #2.
- `AgentMovement.GetPlayerAttributes(agentId) → PlayerAttributes`
  — Spec #2 §3.5.6 (`Heading`, `Strength`, `Balance`).
- `CollisionSystem.GetContactEventsAtFrame(frame) →
  ReadOnlySpan<ContactEvent>` — Spec #3.
- `DecisionTree.GetHeaderIntent(agentId, tick) → HeaderIntent?`
  — Spec #8.
- `DeterministicRng.NextFloat(drawSiteId) → float` — Spec #16
  §4.1.

### 4.3 Output Interface Contracts

Method signatures (emitted):
- `Ball.ApplyKick(velocity, spin, agentId, matchTime)` — Spec #1
  §3.1.11.2.
- `EventBus.Publish<HeaderExecutedEvent>(evt)` — Spec #17 §3.x.
- `EventBus.Publish<HeaderAttemptFailedEvent>(evt)` — Spec #17.

### 4.4 Determinism Compliance Surface

Listing of all #10 → #16 touchpoints:
- `DOMAIN_TAG_HEADING` allocation request (back-prop ERR-010-001).
- Registered draw sites (3 candidates): `DRAW_SITE_DUEL_TIEBREAK`,
  `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`.
- Entity-iteration order in §3.7.

### 4.5 Performance Compliance Surface

Pre-commitments referenced from #18 §6 ratify-not-override (KD-2
of #18):
- 0-byte hot-path allocation budget (#18 §3.10 `[FIXED]`).
- Per-tick cost budget candidate: ≤80 µs at 22-agent match peak
  (`[EST]`; validated against #18 §5 baseline).
- No `HotPathAllocExempt` attribute uses required (struct-based
  data flow).

### 4.6 Tick-Scheduling Surface

- 10 Hz tactical loop: Decision Tree #8 produces `HeaderIntent`.
- 60 Hz physics loop: eligibility → jump kinematics → contact
  resolution → output emission. Sequence diagram (ASCII) in §4.6.

---

## SECTION 5 — TEST PLAN (`section-5.md`)

**Subsection target length:** ~300 lines.

### 5.1 Unit Tests

One sub-section per §3 algorithm; ~6–10 test cases each. Examples:
- 5.1.1 Eligibility predicate (truth-table over aerial-phase,
  contact-volume, body-part).
- 5.1.2 JumpReach formula (sensitivity ±10% per input attribute).
- 5.1.3 Contact-quality scalar (signed timing offset sweep
  -200 ms…+200 ms; point-error sweep 0…0.05 m).
- 5.1.4 Power & launch-angle generation (PowerIntent sweep
  0.1…1.0; fatigue sweep 0…1).
- 5.1.5 Spin transfer (incoming spin direction sweep; contact-point
  offset sweep).
- 5.1.6 Duel resolution (2-way, 3-way; tiebreaker invocation
  count; iteration-order determinism).
- 5.1.7 Failed-attempt emission (each F-01…F-07 cause).
- 5.1.8 Own-goal-shape flag (positive cases + true-negatives where
  trajectory passes through opponent goal).

### 5.2 Integration Tests

- 5.2.1 Open-play header from a Pass Mechanics #5 cross (consumes
  `BallState`, no Pass-label coupling — verifies KD-5).
- 5.2.2 Corner-kick header (set-piece pathway, KD-13).
- 5.2.3 Free-kick header (set-piece pathway).
- 5.2.4 Goalkeeper headed clearance (GK pipeline, KD-7).
- 5.2.5 Contested 2-way duel (defender vs. striker).
- 5.2.6 Contested 3-way duel.
- 5.2.7 Mistimed jump → failed attempt → no ball state change.
- 5.2.8 Own-goal-shape flag → Event System #17 receives and
  adjudicates (mock).
- 5.2.9 Deterministic replay: 1000-tick scenario producing
  identical `HeaderExecutedEvent` sequence across runs.

### 5.3 Validation Scenarios (match-feel)

- 5.3.1 22-agent match peak: 10-minute simulation with ~15 headers
  expected; verify telemetry distribution
  (Perfect/Early/Late ratios match a designer-set target).
- 5.3.2 Corner-routine A/B: same delivery, two striker profiles
  (Heading 75 vs. 90) → measurable outcome divergence.
- 5.3.3 Fatigue gradient: header power at fatigue=0.0 vs. 1.0 →
  ~12% outgoing speed reduction (validation against KD-9 plus
  `POWER_FATIGUE_COEFF`).

### 5.4 Cross-Spec Conformance Tests

- 5.4.1 No `HeaderType`/`HeaderClass` symbol exists in `src/` (grep
  gate; KD-1).
- 5.4.2 Every constant in `HeadingConstants.cs` has a source tag
  comment (KD-11; programmatic verification per #20 §3.4).
- 5.4.3 Every RNG call uses `DeterministicRng.NextFloat(drawSiteId)`
  (KD-10).
- 5.4.4 No `System.Random` / `DateTime.Now` usage (CLAUDE.md gate).

---

## SECTION 6 — PERFORMANCE ANALYSIS & BUDGETS (`section-6.md`)

**Subsection target length:** ~120 lines.

### 6.1 Per-Tick Budget

Budget candidate: ≤80 µs per 60 Hz physics tick at 22-agent match
peak. Justification: ≤22 simultaneous eligibility checks (one per
agent), ≤4 simultaneous contact resolutions (per #3 typical
contact-event count), ≤1 contested duel resolution at p99. All
struct-based; 0-byte hot-path allocation per #18 §3.10 (KD-11
ratifies; does not override).

### 6.2 Hot-Path Allocation Discipline

- No `new` in formula files (`HeadingContactQuality.cs` etc.).
- `ReadOnlySpan<>` for contact-event consumption.
- Struct return types for `HeaderIntent`, `HeaderContactState`.
- Cite #18 §3.10 channel-registry and 0-byte budget.

### 6.3 Scaling Analysis

- 22-agent match peak: 22 eligibility checks × per-tick frequency.
- p99 contested duels: estimated ≤3 per match minute.
- Estimated worst-case cost (3-way duel + tiebreaker RNG):
  ~120 µs at the duel-resolution frame.

### 6.4 Profiling Compliance (KD-6 of #18)

- Determinism-aware profiling hooks at §3.7 entry and §4.3 emission.
- Trace channel allocations declared in §2.4.

### 6.5 Stage 0 → Stage 1 Performance Migration Notes

Single paragraph: Fixed64 binding deferred to Stage 5+ (Spec #9
§8.1); `float` is canonical at Stage 0.

---

## SECTION 7 — FUTURE EXTENSIONS & STAGE 1+ DEFERRALS (`section-7.md`)

**Subsection target length:** ~90 lines.

Each deferral has: ID, statement, rationale, candidate Stage.

- 7.1 Weak-aerial-side asymmetry (KD-14) — Stage 1+ once
  validation data exists.
- 7.2 Concussion / injury accumulation (KD-15) — gated on Medical
  spec.
- 7.3 Bicycle-kick / overhead-kick distinct kinematics — Stage 1+;
  Stage 0 routes overhead-kick head contacts through #10 with
  posture data from #2 but does not introduce new formula branches.
- 7.4 Headed-pass intent classification (clearance / flick-on /
  knock-down) — telemetry classifier downstream; no spec needed at
  Stage 0.
- 7.5 Set-piece kick generation (the kick itself) — Stage 1+
  set-piece spec.
- 7.6 Aerial-attribute introduction to AM #2 — Stage 1+ contingent
  on `[DERIVED]` `JumpReach` proving insufficient (KD-4).
- 7.7 Concession-time / pressure / referee-decision interaction —
  Stage 2+ match-state spec.

---

## SECTION 8 — REFERENCES, CITATIONS, DOI VERIFICATION (`section-8.md`)

**Subsection target length:** ~70 lines.

### 8.1 Project Documents Cited

- `CLAUDE.md` (coordinate, fatigue, tick-rate, constant-tag
  invariants).
- `SPEC_INDEX.md` (numbering authority).
- `docs/tracking/spec-error-log.md` (ERR-010-001 back-prop entry).

### 8.2 Upstream Specs Cited (section-level)

Table: spec #, section/subsection, citation purpose. ~20 rows
covering #1 §1.2, §3.1.11.2; #2 §3.5.6, §3.5.8 (jump kinematics);
#3 §3.x contact-event API; #4 §1.2 (boundary statement); #6 §1.2
KD-6; #8 §1.7.x (intent surface); #16 §3.2 / §3.4 / §4.1 / §4.5;
#17 §3.x event publish API; #18 §3.10 / §6; #19 §3.x test framework
APIs; #20 §3.x constant-tag verification.

### 8.3 External References (Academic / Empirical)

- Bull (1985) — coefficient-of-restitution for head-ball impacts
  (with DOI).
- Auger & Pellegrini (2007) — head kinematics under jumping
  contact (placeholder until DOI verified during drafting).
- (Up to 6 references; all DOIs verified before §9 approval.)

### 8.4 Typed Cross-References

Allocated IDs (`XC-010-NNN`, `FM-010-NNN`, `EC-010-NNN`):
- `XC-010-001` — to AM #2 §2.5 EntityId no-reuse.
- `XC-010-002` — to Ball Physics #1 §1.2 coordinate origin.
- `XC-010-003` — to Shot #6 KD-6 body-part routing.
- `XC-010-004` — to First Touch #4 §1.2 0.5 m boundary
  reaffirmation.
- `XC-010-005` — to Determinism #16 §3.4 `DOMAIN_TAG_HEADING`.
- `XC-010-006` — to Event System #17 own-goal adjudication.
- `XC-010-007` — to #18 §3.10 trace channel registry.
- `FM-010-001` — `JumpReach` (§3.3).
- `FM-010-002` — `contactQualityScalar` (§3.4).
- `FM-010-003` — `outgoingSpeed` (§3.5).
- `FM-010-004` — `outgoingSpin` (§3.6).
- `FM-010-005` — `duelScore` (§3.7).
- `EC-010-001..007` — F-01…F-07 from §2.3.

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

**Subsection target length:** ~140 lines.

### 9.1 Constant-Tag Verification (KD-11)

Per-constant programmatic check: every entry in `HeadingConstants.cs`
mirror (or §3.1 master table) has exactly one tag in
`{[GT], [EST], [FIXED], [DERIVED], [CROSS], [CROSS-PENDING]}`.
Verification is grep-based; no fabricated checklist entries (closes
ERR-005 trap class).

### 9.2 Cross-Spec Reference Verification

Every `XC-010-NNN` resolves to a specific section in the named spec.
Verification: grep target spec; section must exist.

### 9.3 Sign-Off Requirements

- Lead-developer sign-off.
- Physics-owner sign-off (head-ball contact geometry).
- Determinism-owner sign-off (KD-10 governance: draw-site IDs,
  iteration order, `DOMAIN_TAG_HEADING` allocation).

### 9.4 Outstanding Items at Approval Time

- `DOMAIN_TAG_HEADING` `[CROSS-PENDING]` → `[CROSS]` atomic with
  #16 back-prop ERR-010-001.
- Trace channel rows allocated in #18 §3.10.

### 9.5 Cross-Spec Re-Audit (pre-`APPROVED`)

Verify against APPROVED versions of #1, #2, #3, #4, #5, #6, #8,
#16, #17 that no upstream surface cited has shifted between draft
start and approval.

### 9.6 Post-Approval Follow-ups (not gating)

- Comprehensive audit (per Decision Tree #8 precedent); not
  required for APPROVED transition.
- Goalkeeper #11 integration verification once #11 reaches IN
  REVIEW.

---

## APPENDICES (`appendices.md`)

**Target length:** ~250 lines total.

### Appendix A — Derivations

- A.1 `JumpReach` derivation from first principles + ablation.
- A.2 `contactQualityScalar` linearity proof.
- A.3 Spin-transfer reversal boundary.
- A.4 Own-goal-shape projection geometry.

### Appendix B — Sensitivity Tables

- B.1 `JumpReach` over Strength × Balance grid (11 × 11).
- B.2 `outgoingSpeed` over `PowerIntent` × fatigue × Heading
  grid.
- B.3 Duel-score `Heading × Strength × Balance` sensitivity
  rankings.

### Appendix C — Exemplar Tuning Profiles

Three preset header-style profiles (high-leap centre-back,
glancing-finish forward, balanced midfielder). Each profile sets
the `[GT]` constants in §3.1 to feel-target values. Profiles are
illustrative; designer-authored values supersede at Stage 1+.

### Appendix D — Glossary

`HEAD_CONTACT_VOLUME`, `ContactPointIntent`, `ContactQualityScalar`,
`HeaderIntent`, `HeaderExecutedEvent`, `HeaderAttemptFailedEvent`,
`ContestedDuelContext`, `OwnGoalShapedTrajectory`, etc.

### Appendix E — Mapping Table to Adversarial Review Findings

Two-column table: finding number (1–22 from `outline.md`
adversarial-review appendix) → resolution location in this outline
(KD-N or section ID). Used by §9 to programmatically confirm every
finding is addressed.

---

## OPEN-ITEMS TRACKER

Status at outline-detailed v1.0:

| ID | Item | Owner | Status |
|----|------|-------|--------|
| OI-001 | `DOMAIN_TAG_HEADING` allocation in #16 §3.4 | back-prop ERR-010-001 | pending — to be filed when `section-3.md` lands |
| OI-002 | `#18 §3.10` trace channel rows for `heading.*` channels | back-prop | pending — to be filed when `section-2.md` §2.4 lands |
| OI-003 | DOI verification for §8.3 external references | drafter | pending |
| OI-004 | Goalkeeper #11 interface confirmation | post-#11 IN REVIEW | not blocking |

---

## VERSION HISTORY

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 6, 2026 | initial | `outline.md` high-level; 22-finding adversarial review attached | (review applied in this doc) |
| 1.0 | May 15, 2026 | this document | Detailed outline supersedes v0.1; 22 findings all resolved via KD-1…KD-16 + section-plan remap; dependencies fully enumerated; output interface defined; ready for section-file authoring | pending |
