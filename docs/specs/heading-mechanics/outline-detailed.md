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
**Version:** 1.1
**Status:** DRAFT — supersedes `outline.md` v0.1 (May 6, 2026); resolves
all 22 findings of the May 6, 2026 adversarial review attached to that
file; v1.1 (May 15, 2026, later same day) additionally resolves all 21
findings of `outline-detailed-pass-1-review.md` (5 H / 9 M / 7 L) and
the cross-cutting AM #2 jump-surface absence discovered during M-8
verification. Two additional KDs added (KD-17 intent staleness, KD-18
jump-kinematics ownership). KD count: 18.
**Specification Number:** 10 of 20 (Stage 0, Priority 3)
**Estimated Effort:** ~28 hours (section files), ~6 hours (pass-1
adversarial critique), ~4 hours (pass-2 fix cycle).
**Companion documents:** `outline.md` (high-level v0.1 with
adversarial-review appendix — retained for history; do not edit).

**Dependencies (direct, all APPROVED):**
- Ball Physics #1 (incoming `BallState`; `Ball.ApplyKick` output
  surface; coordinate-system authority).
- Agent Movement #2 (`Heading`, `Strength`, `Balance` attributes via
  `PlayerAttributes` struct at §3.5.6; `AgentMovementState` enum at
  §3.1.2; `GroundedReason.DIVING_HEADER` at §3.1.2; XY kinematic
  state via §3.5.1 `Agent` class). **Note:** AM #2 §3.6 explicitly
  defers Z>0 jumping to Stage 1+; jump kinematics at Stage 0 are
  owned by Heading #10 (see KD-18). #10 consumes #2's ground
  kinematic state and `PlayerAttributes`; #10 produces its own
  vertical-axis kinematics during the aerial-contact window.
- Collision System #3 (head-ball contact resolution; contested-duel
  contact data).
- First Touch Mechanics #4 (boundary partner — head-vs-non-head body
  part discrimination authority).
- Pass Mechanics #5 (canonical cross-delivery source; consumed
  read-only via `BallState`, not via Pass-specific labels).
- Shot Mechanics #6 (analogous output-interface model; KD-6 body-part
  routing authority establishing that ALL head contacts route here
  regardless of height).
- Decision Tree #8 (intent parameters: header target, power intent,
  contact-point intent).
- Deterministic Simulation #16 (RNG governance for tie-breaks;
  iteration-order discipline; `DOMAIN_TAG` allocation for
  `DeterministicRngService`).
- Event System #17 (`HeaderExecutedEvent` consumer; own-goal-shaped
  trajectory adjudication).

**Tractability cites (not direct dependencies; named here for context
only, not consumed at any interface):**
- Perception System #7 — supplies Decision Tree #8's inputs; relevant
  upstream of #8, not #10.

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
  point error (m) on the head surface. Named windows
  (`Early` / `OnTime` / `Late` — `OnTime` deliberately chosen over
  `Perfect` to avoid implying a quality gate, L-1 from
  `outline-detailed-pass-1-review.md`) are post-computation labels
  emitted into `HeaderExecutedEvent.contactQualityLabel` for
  telemetry, NEVER branched on by the physics formula. **Resolves
  finding 3.**

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
  `[DERIVED]` tag. Canonical formula (per H-2 fix from pass-1
  review): `JumpReach_m = JUMP_REACH_BASE_M + JUMP_REACH_K_STRENGTH ·
  Strength_norm + JUMP_REACH_K_BALANCE · Balance_norm +
  JUMP_REACH_K_HEADING · Heading_norm`, where `JUMP_REACH_BASE_M` is
  a `[FIXED]` anatomical baseline and `JUMP_REACH_K_*` are `[GT]`.
  The `Heading` term captures jump-timing skill (anticipating apex
  alignment with the ball arrival frame); a dedicated timing
  attribute is deferred until Stage 1+ validation data warrants
  separating reach and timing. Worked example and sensitivity
  analysis live in Appendix B. Reach is computed once per jump
  phase; not per-tick. **Resolves finding 5; preserves AM #2
  APPROVED status; resolves pass-1 H-2.**

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
  allocation (`DOMAIN_TAG_HEADING = 0x16`) is requested from #16
  §3.4 via a back-propagation entry filed under `ERR-010-001`
  (created during drafting). The allocation is a pure namespace
  amendment to APPROVED #16 (no `DETERMINISM_DIGEST_VERSION` bump),
  following the precedent set by Event System #17's
  `DOMAIN_TAG_EVENT_LEDGER = 0x15` patch on May 14, 2026 (#16 §3.4
  v1.0.1). Next free slot in #16 §3.4 catalogue is `0x16` (current
  allocations: `0x10`..`0x15`). Promotion of the `[CROSS-PENDING]`
  tag to `[CROSS]` is atomic with the back-prop landing. **Resolves
  finding 10; resolves pass-1 C-2.**

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
  from a cross because incoming spin / velocity / position are read
  uniformly from `BallState` regardless of delivery type. Any
  set-piece-specific in-swing / out-swing characteristics are
  produced by #5 on the kick and propagated through `BallState`
  before #10 sees the ball; #10 needs no set-piece-specific branch.
  Wall presence and defender-pile geometry are handled by Collision
  System #3 contact events, not #10. Spec #10 covers both open-play
  and set-piece headers. Set-piece taking (the kick itself) remains
  deferred to Stage 1+ per Shot Mechanics #6 §1.2. **Resolves
  finding 20; resolves pass-1 M-7.**

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

- **KD-17 — Intent-staleness re-validation policy.** `HeaderIntent`
  is committed at the 10 Hz tactical tick (Decision Tree #8) and
  may be 1–18 physics frames stale by predicted contact frame.
  Policy: (a) `targetIntent` is **held fixed** after commit — the
  player chose to head it *there*; deviation from a re-evaluated
  ideal is what `contactQualityScalar` already captures via
  `pointError`. (b) `predictedContactFrame` is **re-evaluated every
  physics frame** in §3.2 until either contact occurs or the
  attempt window closes; if the new prediction falls outside the
  attempt window after commit, `HeaderAttemptFailedEvent` is emitted
  with `failureCause = MistimedEarly` / `MistimedLate` per the
  signed drift direction. (c) `powerIntent` and
  `contactPointIntent` are held fixed (locked at commit) — Decision
  Tree #8 does NOT re-issue these mid-attempt. **Resolves pass-1
  M-5.**

- **KD-18 — Stage 0 jump kinematics are Heading-owned.** Agent
  Movement #2 explicitly defers Z>0 movement to Stage 1+
  (`section-3-6-part-2.md` §3.6 comment). At Stage 0, #10 owns the
  full vertical-axis kinematic during the aerial-contact window: it
  derives a synthetic apex-altitude trajectory from `JumpReach` (per
  KD-4) and a `[GT]` jump duration profile, advances head position
  on the 60 Hz physics tick during the aerial phase, and exits back
  to the AM #2-owned XY ground state on landing (entering
  `GROUNDED` if a diving header was performed, per AM #2 §3.1.2
  `GroundedReason.DIVING_HEADER`). No amendment to APPROVED #2 is
  required at Stage 0. When AM #2 grows native Z kinematics at
  Stage 1+, #10's synthetic jump trajectory is the natural retire
  target; deferral logged in §7.8. **Resolves pass-1 cross-cutting
  AM #2 jump-surface absence; preserves AM #2 APPROVED status.**

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
- Set-piece kick delivery (the kick itself): at Stage 0 → Spec #5
  (Pass) covers free kicks and corners as kick variants; at Stage
  1+ a dedicated set-piece spec may take over. The Stage 0 routing
  is canonical for #10's lifetime in Stage 0.
- Goalkeeper-specific decision logic (when to punch vs. catch) →
  Goalkeeper Mechanics #11; physics layer remains #10.
- Concussion / injury accumulation → Stage 1+ Medical spec.
- Weak-aerial-side asymmetry → Stage 1+ (KD-14).
- Header-pass labelling (was this a clearance, flick-on, knock-down?)
  → telemetry classifier downstream of #10; not a physics input.

### 1.3 Key Design Decisions

**Subsection target length:** ~210 lines.

Eighteen KDs (KD-1 … KD-18) reproduced from the KEY DESIGN DECISIONS
block above, each formatted as: statement (1 sentence), rationale
(2–3 sentences), consequence-if-violated (1 sentence). KD numbering
is canonical for the spec and cited by FR rows in §2.

### 1.4 Dependencies and Integration Contracts

**Subsection target length:** ~60 lines.

**Content:**
- Upstream table (9 rows: #1, #2, #3, #4, #5, #6, #8, #16, #17),
  each row naming the consuming subsection of #10 and the **exact
  verified** section-level citation in the upstream spec
  (pinned per pass-1 M-8). Anchor cheatsheet:
  - #1 §1.2 (coordinate origin), §3.1.11.2 (`Ball.ApplyKick`).
  - #2 §3.1.2 (`AgentMovementState`, `GroundedReason`), §3.5.1
    (`Agent` class, attribute exposure), §3.5.6
    (`PlayerAttributes` struct — `Heading`, `Strength`, `Balance`
    field declarations); no jump-kinematics anchor — #10 owns Z
    kinematics at Stage 0 per KD-18.
  - #3 contact-event API (specific subsection pinned during
    `section-1.md` authoring).
  - #4 §1.2 (boundary statement reaffirming 0.5 m threshold does
    not apply to head contacts).
  - #5 — `BallState`-level consumption only (no #5 subsection
    coupling, per KD-5).
  - #6 §1.3 KD-6 (body-part discriminator authority — exact
    quote in `shot-mechanics/section-1.md:344`).
  - #8 §1.7.x (intent surface — anchor pinned during drafting).
  - #16 §3.2 (entity ordering), §3.4 (`DOMAIN_TAG` catalogue —
    pending `0x16` allocation per KD-10), §4.1 (RNG service),
    §4.5 (draw-site registry).
  - #17 event publish API (specific subsection pinned during
    drafting).
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
- `FR-HE-018` (MUST) — Intent-staleness policy per KD-17:
  `targetIntent`, `powerIntent`, `contactPointIntent` locked at
  commit; `predictedContactFrame` re-evaluated each physics tick.
  Source: KD-17.
- `FR-HE-019` (MUST) — Stage 0 jump kinematics owned by #10 per
  KD-18. Source: KD-18.
- `FR-HE-020` (MUST) — Telemetry label `OnTime` (not `Perfect`)
  used for centred quality bucket. Source: KD-2 / pass-1 L-1.
- `FR-HE-021` (MUST) — `JumpReach` includes a `Heading` term per
  revised KD-4 formula. Source: KD-4 / pass-1 H-2.
- `FR-HE-022` (MUST) — Asymmetric timing-tolerance windows: early
  and late tolerances are distinct `[GT]` constants. Source: pass-1
  H-1.
- `FR-HE-023` (MUST) — Duel tiebreak is governed by an explicit
  near-tie ε threshold; non-tie scores are NOT perturbed by RNG.
  Source: pass-1 H-5.
- (~12 more FRs covering specific formula behaviors, telemetry
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
  `contactQualityLabel` (telemetry enum: `Early` / `OnTime` /
  `Late` — emitted, not consumed; `OnTime` chosen over `Perfect`
  per pass-1 L-1), `contactPoint`, `incomingBallState`,
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
  resolved per §3.7, NOT a failure; **winner-only emits
  `HeaderExecutedEvent`; all losers emit `HeaderAttemptFailedEvent`
  with `failureCause = DisturbedInDuel`** (wording aligned with
  §3.7 step 4 per pass-1 L-5).
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
- `heading.contact.quality.label` (counter, 3 buckets: `Early` /
  `OnTime` / `Late` — pass-1 L-1).
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

**Inventory discipline (pass-1 M-1 closure):** every symbol that
appears in §3.2–§3.8 pseudocode bodies MUST be a row in this table
with a source tag, OR be a per-call output / local variable
explicitly named as such in the relevant §3.X subsection. No magic
numbers in pseudocode.

Constants to enumerate (~35 rows, expanded per pass-1 M-1):
- `HEAD_CONTACT_VOLUME_RADIUS_M` `[GT]` (0.18 m candidate).
- `HEAD_CONTACT_VOLUME_HEIGHT_M` `[GT]`.
- `MAX_EARLY_TOLERANCE_MS` `[GT]` (asymmetric timing window —
  pass-1 H-1).
- `MAX_LATE_TOLERANCE_MS` `[GT]` (asymmetric timing window —
  pass-1 H-1; numerically smaller than `MAX_EARLY_TOLERANCE_MS`
  reflecting that late headers degrade faster than early ones).
- `EARLY_LABEL_THRESHOLD_MS` `[GT]` (telemetry-bucket boundary; NOT
  a formula gate per KD-2).
- `LATE_LABEL_THRESHOLD_MS` `[GT]` (telemetry-bucket boundary; NOT
  a formula gate per KD-2).
- `TIMING_POINT_BLEND_ALPHA` `[GT]` (the `α` in §3.4 blend;
  `timingQuality` weight in the convex combination with
  `pointQuality`).
- `MIN_CONTACT_QUALITY` `[GT]` (§3.7 cutoff: duel loser below this
  threshold emits `HeaderAttemptFailedEvent` rather than a poor-
  quality `HeaderExecutedEvent`).
- `FRAME_MS` `[DERIVED]` from `TICK_RATE_PHYSICS_HZ`
  (`FRAME_MS = 1000 / TICK_RATE_PHYSICS_HZ ≈ 16.67`); formula in
  §3.4.
- `JUMP_REACH_BASE_M` `[FIXED]` (anatomical).
- `JUMP_REACH_K_STRENGTH` `[GT]`.
- `JUMP_REACH_K_BALANCE` `[GT]`.
- `JUMP_REACH_K_HEADING` `[GT]` (added per pass-1 H-2; covers
  jump-timing skill until a dedicated timing attribute exists).
- `JUMP_PHASE_DURATION_MS` `[GT]` (Stage 0 jump-trajectory profile
  duration per KD-18; total ground-to-ground aerial-phase length).
- `JUMP_APEX_FRACTION` `[GT]` (Stage 0 apex location along the
  jump phase as a fraction of `JUMP_PHASE_DURATION_MS`; `[GT]` not
  `[FIXED]` because Stage 0 trajectory is synthetic per KD-18,
  not physical).
- `POWER_BASE_MPS` `[GT]`.
- `POWER_K_STRENGTH` `[GT]`.
- `POWER_K_HEADING` `[GT]`.
- `POWER_FATIGUE_COEFF` `[GT]`.
- `CONTACT_POINT_ERROR_SIGMA_M` `[GT]` (mean point-error scale;
  also baseline for the `pointQuality` denominator).
- `CONTACT_POINT_NOISE_SIGMA_M` `[GT]` (added per pass-1 M-4 —
  amplitude of the per-attempt point-error noise term injected via
  `DRAW_SITE_CONTACT_POINT_ERROR`).
- `TIMING_JITTER_SIGMA_MS` `[GT]` (added per pass-1 M-4 — amplitude
  of the per-attempt timing-noise term injected via
  `DRAW_SITE_TIMING_JITTER`).
- `CONTACT_POINT_HEADING_ATTR_COEFF` `[GT]` (Heading-attribute
  scaling of `CONTACT_POINT_ERROR_SIGMA_M`; higher Heading → tighter
  point-error distribution).
- `SPIN_TRANSFER_COEFF` `[GT]` (multiplier on derived
  `headAngularVelocity` contribution to outgoing spin).
- `SPIN_PRESERVATION_BASE` `[GT]` (the `spinPreservationFactor`
  scale-factor base; the §3.6 working form is
  `spinPreservationFactor = SPIN_PRESERVATION_BASE · (1 -
  contactPointAxialOffset_m / SPIN_TRANSFER_REVERSAL_THRESHOLD)`,
  with formula and worked example in Appendix A.3).
- `SPIN_TRANSFER_REVERSAL_THRESHOLD` `[GT]` (contact-point offset
  beyond which spin reverses).
- `DUEL_BALANCE_WEIGHT` `[GT]` (`w_B` in §3.7).
- `DUEL_STRENGTH_WEIGHT` `[GT]` (`w_S` in §3.7).
- `DUEL_HEADING_WEIGHT` `[GT]` (`w_H` in §3.7).
- `DUEL_TIEBREAK_EPSILON` `[GT]` (near-tie threshold; below this
  score-gap the RNG perturbation is invoked — pass-1 H-5).
- `DUEL_TIEBREAK_NOISE_AMPLITUDE` `[GT]` (RNG perturbation
  amplitude applied only when `|scoreA - scoreB| <
  DUEL_TIEBREAK_EPSILON` — pass-1 H-5).
- `DUEL_DISTURBANCE_MAX` `[GT]`.
- `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` `[GT]` (time horizon).
- `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M` `[GT]` (distance
  horizon — pass-1 L-7; flag invocation uses
  `min(time, distance)` to handle flat headers correctly).
- `GRAVITY_MPS2` `[CROSS]` (Ball Physics #1).
- `PITCH_LENGTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `PITCH_WIDTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `DOMAIN_TAG_HEADING = 0x16` `[CROSS-PENDING]` (Deterministic
  Simulation #16 §3.4 — back-prop ERR-010-001; allocation slot
  per #17 `0x15` precedent).
- `TICK_RATE_TACTICAL_HZ` `[CROSS]` (CLAUDE.md).
- `TICK_RATE_PHYSICS_HZ` `[CROSS]` (CLAUDE.md).

**Removed from §3.1 in v1.1:**
- `IDEAL_CONTACT_FRAME_OFFSET` — was `[DERIVED]` but is a per-jump
  computed value, not a project-level constant; relocated to §3.2
  as a per-call output of the eligibility predicate (pass-1 M-2).
- `GLANCING_ANGLE_THRESHOLD_RAD` — no caller; dead constant
  (pass-1 L-3). Glancing-vs-direct classification is a downstream
  telemetry concern, not a #10 publication.

### 3.2 Eligibility Predicate

**Inputs:** agent kinematic state (Agent Movement #2 §3.5.1),
`BallState` (Ball Physics #1), `HeaderIntent` (Decision Tree #8),
Stage 0 synthetic jump trajectory (#10-owned per KD-18).
**Outputs:** `bool isEligible`, `int predictedContactFrame`,
`int idealContactFrame` (the apex-aligned target frame against
which `timingOffsetMs` is measured in §3.4; per-call value, not a
constant — relocated from §3.1 per pass-1 M-2).

Pseudocode covering: aerial-phase check (Stage 0 aerial-phase is
the #10-owned synthetic jump phase per KD-18; AM #2 ground state
must be exitable, i.e. not `GROUNDED` / `STUMBLING`), predicted
contact body part = `Head`, ball trajectory intersects
`HEAD_CONTACT_VOLUME` within attempt-window frames.

**Intent-staleness handling (KD-17 — pass-1 M-5 closure):**
`predictedContactFrame` is re-evaluated at every 60 Hz physics
tick from `attemptCommittedTick` until contact or window-close.
`targetIntent`, `powerIntent`, `contactPointIntent` are locked at
commit and never re-issued. If the re-evaluated
`predictedContactFrame` drifts outside the attempt window
(`[idealContactFrame - MAX_EARLY_TOLERANCE_MS,
idealContactFrame + MAX_LATE_TOLERANCE_MS]` converted to frames),
the §3.9 failed-attempt pipeline is invoked with `failureCause`
set by drift direction.

Worked example: corner cross at 8 m/s; defender jump-committed at
tick T; predicted contact at frame T+9; idealContactFrame T+9;
re-evaluated at T+1, T+2, … to T+9; if at T+5 the ball deflects
off a defender and re-prediction yields T+14 (outside window),
emit `HeaderAttemptFailedEvent` with `MistimedLate`.

### 3.3 Jump Kinematics Integration

`JumpReach` `[DERIVED]` formula (KD-4, pass-1 H-2 fix):
```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
            + JUMP_REACH_K_HEADING  · Heading_norm
```

**Stage 0 jump-trajectory ownership (KD-18):** AM #2 §3.6 does not
publish Z>0 kinematics; Stage 0 jump trajectory is synthesized
inside #10. Synthetic profile:
```
phase_t           = (currentFrame - jumpStartFrame) · FRAME_MS
apexFrame         = jumpStartFrame + round(JUMP_PHASE_DURATION_MS
                                            · JUMP_APEX_FRACTION / FRAME_MS)
agentHeadZ(frame) = parabolic interpolation peaking at apexFrame
                    with peak value JumpReach_m
```
At Stage 1+ when AM #2 grows native Z kinematics, this synthetic
trajectory retires; the surface shifts to reading AM #2 apex-frame
`agentZ` and adding the anatomical head-above-COM offset
(deferred per §7.8).

Worked example with sensitivity analysis (Appendix B; ablation
includes `Heading` coefficient sweep per KD-4 / H-2).

### 3.4 Contact-Quality Scalar (KD-2)

**Asymmetric timing tolerance (pass-1 H-1 fix).** Late headers
degrade faster than early ones, so the early/late tolerances are
separate `[GT]` constants and the formula is piecewise:

```
timingJitterMs = TIMING_JITTER_SIGMA_MS
                 · rng.NextGaussian(DRAW_SITE_TIMING_JITTER)
timingOffsetMs = (actualContactFrame - idealContactFrame) · FRAME_MS
                 + timingJitterMs
if timingOffsetMs <= 0:
    timingQuality = 1 - clamp01(-timingOffsetMs / MAX_EARLY_TOLERANCE_MS)
else:
    timingQuality = 1 - clamp01( timingOffsetMs / MAX_LATE_TOLERANCE_MS)

pointNoiseM    = CONTACT_POINT_NOISE_SIGMA_M
                 · rng.NextGaussian(DRAW_SITE_CONTACT_POINT_ERROR)
pointError     = ||contactPointActual - contactPointIntent|| + pointNoiseM
pointQuality   = 1 - clamp01(pointError /
                            (CONTACT_POINT_ERROR_SIGMA_M
                             · headingAttrScale(agent)))
contactQualityScalar = TIMING_POINT_BLEND_ALPHA · timingQuality
                       + (1 - TIMING_POINT_BLEND_ALPHA) · pointQuality
```

`headingAttrScale(agent) = 1 + CONTACT_POINT_HEADING_ATTR_COEFF ·
(Heading_norm - 0.5)` — higher Heading attribute tightens the
point-error distribution.

**RNG draw-site wiring (pass-1 M-4 closure):** the timing-jitter
and contact-point-noise terms above are the call sites for
`DRAW_SITE_TIMING_JITTER` and `DRAW_SITE_CONTACT_POINT_ERROR`
declared in §4.4. Both draws are Gaussian (#16 RNG service
provides `NextGaussian(drawSiteId)`).

Telemetry label assignment (pass-1 L-1 — `Perfect` → `OnTime`):
- `Early`  if `timingOffsetMs < -EARLY_LABEL_THRESHOLD_MS`.
- `Late`   if `timingOffsetMs > +LATE_LABEL_THRESHOLD_MS`.
- `OnTime` otherwise. **Labels are NEVER consumed by §3.5–§3.7.**

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

**`headAngularVelocity` derivation (pass-1 H-3 closure).** AM #2
does not publish a head-segment angular velocity. #10 derives it
locally from already-available data, avoiding any upstream
amendment to APPROVED #2:

```
headAngularVelocity = neckRotationRate
                    + finiteDifference(headOrientation,
                                       prevFrameHeadOrientation,
                                       FRAME_MS)
```

where `headOrientation` is computed at each frame from `agent.facing`
(AM #2 §3.5.1) and the per-frame `contactPointIntent` aim vector;
`neckRotationRate` is `[DERIVED]` from agent angular velocity (AM
#2 XY-plane yaw rate via finite difference of `agent.facing`)
projected onto the head-aim vector. This is a Stage 0 approximation;
at Stage 1+ if AM #2 publishes a head-segment skeletal API, the
derivation simplifies to a direct read (deferred to §7.9).

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset_m
                              / SPIN_TRANSFER_REVERSAL_THRESHOLD)
reversalTerm           = max(0, -spinPreservationFactor) · incomingSpin
                       (when contactPointAxialOffset exceeds
                        SPIN_TRANSFER_REVERSAL_THRESHOLD,
                        spinPreservationFactor goes negative →
                        outgoing spin component opposes incoming)
outgoingSpin           = SPIN_TRANSFER_COEFF · headAngularVelocity
                       + (incomingSpin · spinPreservationFactor)
                       - reversalTerm
```

`spinPreservationFactor` formula is closed-form (no magic
numbers — pass-1 M-1 closure for §3.6 symbols). Worked example:
incoming topspin 8 rad/s, contact-point axial offset 0.02 m → with
`SPIN_PRESERVATION_BASE = 0.6` and
`SPIN_TRANSFER_REVERSAL_THRESHOLD = 0.015 m`, factor =
`0.6 · (1 - 0.02/0.015) = -0.2`, reversalTerm =
`0.2 · 8 = 1.6 rad/s`, contribution =
`(8 · -0.2) - 1.6 = -3.2 rad/s` (backspin reversal).

### 3.7 Contested Duel Resolution (KD-8)

Inputs: Collision System #3 contact-event list at the candidate
contact frame; participating agents within `HEAD_CONTACT_VOLUME`.

Algorithm (pass-1 H-5 fix — tiebreak semantics now explicit and
all constants tagged):

1. Iterate participants in #16 §3.2 entity order.
2. Compute base score for each participant:
   `baseScore = DUEL_BALANCE_WEIGHT  · Balance_norm
              + DUEL_STRENGTH_WEIGHT · Strength_norm
              + DUEL_HEADING_WEIGHT  · Heading_norm`.
3. Rank participants by `baseScore` descending. If the gap between
   the top two scorers is `< DUEL_TIEBREAK_EPSILON`, invoke the
   tiebreak perturbation: each participant within
   `DUEL_TIEBREAK_EPSILON` of the leader receives an additive
   `DUEL_TIEBREAK_NOISE_AMPLITUDE · rng.NextFloat(
   DRAW_SITE_DUEL_TIEBREAK)` and the ranking is recomputed. Non-tie
   scores are NEVER perturbed.
4. Highest scorer wins; emits `HeaderExecutedEvent`. Losers receive
   `disturbanceFactor ∈ [0, DUEL_DISTURBANCE_MAX]` applied to their
   `contactQualityScalar`; if reduced below `MIN_CONTACT_QUALITY`
   the loser emits `HeaderAttemptFailedEvent` instead of a poor-
   quality executed event.
5. **Multi-way (3+) duels: winner-only emits `HeaderExecutedEvent`;
   all losers emit `HeaderAttemptFailedEvent`** (wording aligned
   with §2.3 F-04 per pass-1 L-5).

Worked example: two strikers + one defender contesting a corner;
defender wins with `duelScore = 0.72` vs. strikers' 0.65, 0.61.

### 3.8 Own-Goal-Shape Flag Computation (KD-6)

```
horizon_s = OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S
horizon_m = OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M
flag = projectTrajectory(outgoingVelocity, contactPosition,
                          min_horizon(horizon_s, horizon_m))
       intersects ownGoalBoundingBox(agent.team)
```
**Dual horizon (pass-1 L-7):** projection terminates at whichever
of (a) `horizon_s` elapsed simulated time or (b) `horizon_m`
travelled-distance arc-length is reached first. A flat header
travels much further per second than a looping header, so a pure
time horizon over-reaches on flat trajectories and under-reaches
on loops; the distance cap binds the flat case, the time cap binds
the loop case. Flag is published; not adjudicated.

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

Method signatures (consumed). Anchor pinning per pass-1 M-8:
- `BallPhysics.GetBallState(matchTime) → BallState` — Spec #1
  (subsection pinned during `section-1.md` authoring of #10).
- `Agent` class state (XY kinematic state, `facing`, attribute
  exposure) — Spec #2 §3.5.1 (`section-3-5-part-1.md` lines
  112–610). No `GetKinematicState(agentId, frame)` getter is
  cited because AM #2 publishes per-agent state via the `Agent`
  instance, not a registry getter; #10 consumes the agent reference
  passed in by the simulation scheduler.
- `PlayerAttributes` struct (`Heading`, `Strength`, `Balance` field
  reads) — Spec #2 §3.5.6 (`section-3-5-part-2.md` line 230 onward;
  `PlayerAttributes` struct declared line 259). §3.5.6 is the
  declaration site; field reads are unqualified struct access, not
  a getter call.
- `AgentMovementState` enum + `GroundedReason` enum (for
  aerial-phase exit and `DIVING_HEADER` ground re-entry) — Spec #2
  §3.1.2 (`section-3-1-part-2.md` lines 23–105). Note: no
  `Jumping` member exists; Stage 0 aerial phase is owned by #10
  per KD-18 and is invisible to the AM #2 state machine.
- `CollisionSystem.GetContactEventsAtFrame(frame) →
  ReadOnlySpan<ContactEvent>` — Spec #3 (subsection pinned during
  drafting).
- `DecisionTree.GetHeaderIntent(agentId, tick) → HeaderIntent?`
  — Spec #8 §1.7.x (subsection pinned during drafting).
- `DeterministicRng.NextFloat(drawSiteId) → float`,
  `DeterministicRng.NextGaussian(drawSiteId) → float` — Spec #16
  §4.1 / §4.5.

### 4.3 Output Interface Contracts

Method signatures (emitted):
- `Ball.ApplyKick(velocity, spin, agentId, matchTime)` — Spec #1
  §3.1.11.2.
- `EventBus.Publish<HeaderExecutedEvent>(evt)` — Spec #17 §3.x.
- `EventBus.Publish<HeaderAttemptFailedEvent>(evt)` — Spec #17.

### 4.4 Determinism Compliance Surface

Listing of all #10 → #16 touchpoints:
- `DOMAIN_TAG_HEADING = 0x16` allocation request (back-prop
  ERR-010-001; pure namespace amendment per #17 `0x15` precedent).
- Registered draw sites (3, all wired to §3 callers per pass-1
  M-4): `DRAW_SITE_DUEL_TIEBREAK` (§3.7 step 3 — near-tie
  perturbation), `DRAW_SITE_CONTACT_POINT_ERROR` (§3.4 point-noise
  Gaussian), `DRAW_SITE_TIMING_JITTER` (§3.4 timing-jitter
  Gaussian). No phantom draw sites.
- Entity-iteration order in §3.7.

### 4.5 Performance Compliance Surface

Pre-commitments referenced from #18 §6 ratify-not-override (KD-2
of #18). **Budget framing (pass-1 H-4 fix — steady-state vs. p99
tail):**
- 0-byte hot-path allocation budget (#18 §3.10 `[FIXED]`).
- **Steady-state per-tick cost budget**: ≤80 µs (`[EST]`) at
  22-agent match peak under non-duel-frame load. This is the
  budget the per-tick path is tuned to and the value carried into
  #18 §6 ratification.
- **p99 duel-frame tail budget**: ≤180 µs (`[EST]`) at duel
  frames (3-way duel with tiebreak perturbation). Justified by
  §6.3 component-cost breakdown; carried into #18 §6 as a
  separate p99 spike row. The 80 µs steady-state ceiling does not
  bind at duel frames; the tail budget binds instead.
- Both numbers are `[EST]` and not credible until
  `certification-platform.md` Stage-0 host pin lands (lead-
  developer task per CLAUDE.md OPEN ISSUES);
  `FR-PO-052` Stage 0+1 perf-gate activation is gated on that
  pin and not on #10 sign-off.
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

- 5.3.1 22-agent match peak: 10-minute simulation with ~3 headers
  expected (pass-1 M-3 recalibration — ~28 headers per full
  90-minute match per published Opta / StatsBomb baselines, scaled
  linearly); verify telemetry distribution (`OnTime` / `Early` /
  `Late` ratios match a designer-set target).
- 5.3.2 Corner-routine A/B: same delivery, two striker profiles
  (Heading 75 vs. 90) → measurable outcome divergence.
- 5.3.3 Fatigue gradient: header power at fatigue=0.0 vs. 1.0 →
  ~12% outgoing speed reduction (validation against KD-9 plus
  `POWER_FATIGUE_COEFF`).

### 5.4 Cross-Spec Conformance Tests

- 5.4.1 No `HeaderType`/`HeaderClass` symbol exists in `src/` (grep
  gate; KD-1). This gate is #10-specific (the symbol grep targets
  are owned here) and lives in #10's test plan.
- 5.4.2 Every constant in `HeadingConstants.cs` has a source tag
  comment (KD-11; programmatic verification per #20 §3.4).
- 5.4.3 Every RNG call uses `DeterministicRng.NextFloat(drawSiteId)`
  or `DeterministicRng.NextGaussian(drawSiteId)` (KD-10).
- (pass-1 M-9: former §5.4.4 "no `System.Random`/`DateTime.Now`"
  gate **removed** — that is a project-wide CI gate owned by #19
  §3.x / #20 §3.x, and re-asserting it here would duplicate the
  authoritative gate and risk drift.)

---

## SECTION 6 — PERFORMANCE ANALYSIS & BUDGETS (`section-6.md`)

**Subsection target length:** ~120 lines.

### 6.1 Per-Tick Budget

**Steady-state budget**: ≤80 µs per 60 Hz physics tick at 22-agent
match peak under non-duel-frame load.
**p99 duel-frame tail budget**: ≤180 µs at duel-resolution frames
(3-way duel + near-tie tiebreak invocation).

Justification: ≤22 simultaneous eligibility checks (one per
agent), ≤4 simultaneous contact resolutions (per #3 typical
contact-event count), ≤1 contested duel resolution at p99. All
struct-based; 0-byte hot-path allocation per #18 §3.10 (KD-11
ratifies; does not override). Component-cost decomposition lives
in §6.3 and is the source-of-truth for both numbers (pass-1 H-4
reconciliation: §6.1 budget rows match §6.3 component sum exactly,
no implicit overrun).

### 6.2 Hot-Path Allocation Discipline

- No `new` in formula files (`HeadingContactQuality.cs` etc.).
- `ReadOnlySpan<>` for contact-event consumption.
- Struct return types for `HeaderIntent`, `HeaderContactState`.
- Cite #18 §3.10 channel-registry and 0-byte budget.

### 6.3 Scaling Analysis

- 22-agent match peak: 22 eligibility checks × per-tick frequency.
- p99 contested duels: estimated ≤0.5 per match minute (~28
  headers per full match; ~10% contested; ~45 duels per 90 min;
  ≈0.5/min — pass-1 M-3 recalibration against Opta / StatsBomb
  baselines, replacing the pre-fix estimate of ≤3/min which was
  ~6× too high).
- Estimated worst-case cost (3-way duel + near-tie tiebreak
  invocation): ~180 µs at the duel-resolution frame (revised up
  from ~120 µs to account for the new Gaussian noise draws in §3.4
  and the conditional ranking re-computation in §3.7 step 3; folds
  back into §6.1 p99 budget).

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
- 7.8 AM #2 native Z kinematics — Stage 1+ (KD-18). When AM #2
  publishes a vertical-axis kinematic surface, retire the
  #10-owned synthetic jump trajectory and read apex-frame `agentZ`
  from AM #2 instead.
- 7.9 AM #2 head-segment skeletal API — Stage 1+ (pass-1 H-3
  resolution). When AM #2 publishes per-segment angular velocity,
  retire the #10-owned `headAngularVelocity` derivation in §3.6
  and read it directly.
- 7.10 Dedicated jump-timing attribute — Stage 1+. When validation
  data warrants separating reach (`JumpReach`) from anticipation/
  timing, introduce a new `PlayerAttribute` and split the `Heading`
  term in the §3.3 formula across the two attributes.
- 7.11 Glancing/direct telemetry classifier — Stage 1+. The dead
  `GLANCING_ANGLE_THRESHOLD_RAD` constant removed from §3.1 in v1.1
  (pass-1 L-3) becomes relevant when a downstream telemetry
  classifier needs an angle threshold; not a #10 publication at
  Stage 0.

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

Pre-identified anchor set (pass-1 L-6 — six anchors named at
outline stage so §9 audit does not surface a sparseness finding;
DOIs verified during `section-8.md` authoring):

- Bull (1985) — coefficient-of-restitution for head-ball impacts.
- Auger & Pellegrini (2007) — head kinematics under jumping
  contact.
- Shewchenko, Withnall, Keown, Gittens & Dvorak (2005) — heading
  in soccer: dynamic, mechanical, and player-perception data
  (relevant for §3.4 timing/point error scales).
- Naunheim, Bayly, Standeven, Neubauer, Lewis & Genin (2003) —
  linear and angular head accelerations during heading
  (relevant for §3.6 `headAngularVelocity` magnitudes).
- Kirkendall & Garrett (2001) — heading in adult soccer
  (relevant for §6.3 / §5.3.1 header-frequency baselines —
  pass-1 M-3 recalibration source).
- Opta / StatsBomb match-level header statistics (modern
  empirical baseline for §5.3.1 expected-header-count target).

### 8.4 Typed Cross-References

Allocated IDs (`XC-010-NNN`, `FM-010-NNN`, `EC-010-NNN`). Pass-1
M-6 drop: former `XC-010-001` (AM #2 §2.5 EntityId no-reuse) was
unmotivated — #10 consumes `agentId` only within single contact
frames, never caches across despawn boundaries — and is removed.
Remaining bindings renumbered:
- `XC-010-001` — to Ball Physics #1 §1.2 coordinate origin
  (was `-002`).
- `XC-010-002` — to Shot #6 §1.3 KD-6 body-part routing
  (was `-003`).
- `XC-010-003` — to First Touch #4 §1.2 0.5 m boundary
  reaffirmation (was `-004`).
- `XC-010-004` — to Determinism #16 §3.4 `DOMAIN_TAG_HEADING`
  catalogue row (was `-005`; `0x16` allocation per KD-10).
- `XC-010-005` — to Event System #17 own-goal adjudication
  (was `-006`).
- `XC-010-006` — to Performance Optimization #18 §3.10 trace
  channel registry (was `-007`).
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

### Appendix E — Mapping Table to v0.1 Adversarial Review Findings

Two-column table: finding number (1–22 from `outline.md`
adversarial-review appendix) → resolution location in this outline
(KD-N or section ID). Used by §9 to programmatically confirm every
finding is addressed.

### Appendix F — Mapping Table to Pass-1 Review Findings (v1.0 → v1.1)

Two-column table covering the 21 findings of
`outline-detailed-pass-1-review.md` (5 H / 9 M / 7 L) plus the
cross-cutting AM #2 jump-surface absence:

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| H-1 timing tolerance | HIGH | §3.4 asymmetric piecewise formula; §3.1 keeps `MAX_EARLY_TOLERANCE_MS` and `MAX_LATE_TOLERANCE_MS` as distinct `[GT]` rows |
| H-2 JumpReach Heading term | HIGH | KD-4 revised; §3.3 formula gains `JUMP_REACH_K_HEADING · Heading_norm`; §3.1 adds `JUMP_REACH_K_HEADING` |
| H-3 headAngularVelocity source | HIGH | §3.6 derives `headAngularVelocity` from AM #2 `agent.facing` finite-difference + projected neck rotation; no #2 amendment; §7.9 deferral for Stage 1+ direct read |
| H-4 §6.1 vs §6.3 budget contradiction | HIGH | §6.1 split into steady-state (≤80 µs) and p99 duel-frame tail (≤180 µs); §6.3 component breakdown is source-of-truth |
| H-5 magic `0.01` in duel tiebreak | HIGH | §3.7 step 3 rewritten using `DUEL_TIEBREAK_EPSILON` and `DUEL_TIEBREAK_NOISE_AMPLITUDE` (both `[GT]` in §3.1); non-tie scores never perturbed |
| M-1 missing §3.1 constants | MEDIUM | §3.1 expanded from ~28 to ~35 rows; every §3.4–§3.7 symbol enumerated or tagged `[DERIVED]` |
| M-2 IDEAL_CONTACT_FRAME_OFFSET miscategorised | MEDIUM | Removed from §3.1; relocated to §3.2 as per-call output of eligibility predicate |
| M-3 header-count expectations off by 3–4× | MEDIUM | §6.3 (≤0.5 duels/min) and §5.3.1 (~3 headers/10 min) recalibrated against Opta / StatsBomb; cite added to §8.3 |
| M-4 phantom draw sites | MEDIUM | §3.4 injects Gaussian noise via `DRAW_SITE_CONTACT_POINT_ERROR` and `DRAW_SITE_TIMING_JITTER`; §3.1 adds `CONTACT_POINT_NOISE_SIGMA_M`, `TIMING_JITTER_SIGMA_MS`; §4.4 marks all three draw sites as wired |
| M-5 intent-staleness policy | MEDIUM | New KD-17; §3.2 intent-staleness handling subsection added; FR-HE-018 covers the contract |
| M-6 XC-010-001 unmotivated | MEDIUM | XC-010-001 (EntityId no-reuse) removed; remaining cross-refs renumbered 001–006 |
| M-7 KD-13 set-piece scope | MEDIUM | KD-13 rationale expanded — spin-on-arrival propagates through `BallState`; wall/pile geometry handled by #3; no set-piece-specific #10 branch needed |
| M-8 AM #2 anchor pinning | MEDIUM | §1.4 dependency table now lists exact subsections (§3.5.1, §3.5.6, §3.1.2); §4.2 explains why no `GetKinematicState(agentId, frame)` getter exists |
| M-9 §5.4.4 duplicate gate | MEDIUM | §5.4.4 removed; CI gate ownership retained at #19 / #20 |
| L-1 `Perfect` label naming | LOW | Renamed `Perfect` → `OnTime` across KD-2, §2.2 enum, §3.4 telemetry, §5.3.1 |
| L-2 §1.2 ambiguous "or" | LOW | Disambiguated: Stage 0 set-piece kicks routed to #5; Stage 1+ may take over |
| L-3 dead GLANCING_ANGLE_THRESHOLD_RAD | LOW | Removed from §3.1; deferred to §7.11 as a Stage 1+ telemetry-classifier concern |
| L-4 #7 in direct-dependency list | LOW | Moved to "Tractability cites" footnote; upstream table reduced to 9 direct deps |
| L-5 §3.7 step 4 / F-04 wording drift | LOW | Both phrasings aligned to "winner-only emits HeaderExecutedEvent; all losers emit HeaderAttemptFailedEvent" |
| L-6 sparse §8.3 anchors | LOW | Six anchors named at outline stage (Bull, Auger & Pellegrini, Shewchenko et al., Naunheim et al., Kirkendall & Garrett, Opta/StatsBomb) |
| L-7 own-goal projection horizon semantics | LOW | §3.1 adds `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M`; §3.8 uses `min(time, distance)` dual-horizon |
| AM #2 jump-surface absence (cross-cutting) | HIGH | New KD-18; §3.3 owns synthetic jump trajectory at Stage 0; §7.8 deferral when AM #2 grows Z kinematics; no #2 amendment required |

---

## OPEN-ITEMS TRACKER

Status at outline-detailed v1.1:

| ID | Item | Owner | Status |
|----|------|-------|--------|
| OI-001 | `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 | back-prop ERR-010-001 | pending — to be filed when `section-3.md` lands; precedent: #17 `DOMAIN_TAG_EVENT_LEDGER = 0x15` patch |
| OI-002 | `#18 §3.10` trace channel rows for `heading.*` channels | back-prop | pending — to be filed when `section-2.md` §2.4 lands |
| OI-003 | DOI verification for §8.3 external references (six anchors named in v1.1) | drafter | pending |
| OI-004 | Goalkeeper #11 interface confirmation | post-#11 IN REVIEW | not blocking |
| OI-005 | Pin #3 contact-event API subsection and #8 intent-surface §1.7.x exact anchor | drafter | pending — pin during `section-1.md` authoring |
| OI-006 | `certification-platform.md` Stage-0 host pin | lead developer | not blocking #10 spec sign-off; blocks `FR-PO-052` perf-gate activation only |

---

## VERSION HISTORY

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 6, 2026 | initial | `outline.md` high-level; 22-finding adversarial review attached | (review applied in this doc) |
| 1.0 | May 15, 2026 | this document | Detailed outline supersedes v0.1; 22 findings all resolved via KD-1…KD-16 + section-plan remap; dependencies fully enumerated; output interface defined; ready for section-file authoring | superseded by v1.1 |
| 1.1 | May 15, 2026 (later) | this document | Resolves all 21 pass-1 adversarial review findings (5 H / 9 M / 7 L) per `outline-detailed-pass-1-review.md`; adds KD-17 (intent-staleness re-validation) and KD-18 (Stage 0 jump kinematics owned by #10 — closes AM #2 jump-surface absence discovered during M-8 verification); §3.1 expanded ~28 → ~35 rows with full M-1 inventory closure; §3.4 asymmetric timing tolerance; §3.4 noise draws wired (M-4); §3.6 `headAngularVelocity` derivation specified; §3.7 tiebreak semantics tagged and constants tagged; §3.8 dual-horizon own-goal projection; §6.1/§6.3 budget reconciled (steady-state vs. p99 tail); §1.4 / §4.2 AM #2 anchors pinned to verified subsections; XC-010-001 dropped; six §8.3 academic anchors named; Appendix F mapping table added | pending |
