# Heading Mechanics Specification #10 — Section 1: Purpose & Scope

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Establish the scope of the Heading Mechanics specification,
out-of-scope items, the eighteen pre-committed Key Design Decisions
(KDs) that govern the entire spec, and the upstream/downstream
integration contracts. Subsequent section files (`section-2.md`
through `appendices.md`) build on the KDs declared here.

---

## 1.1 What This Specification Covers

Heading Mechanics #10 governs every ball contact whose contact body
part is `Head`. From the instant Decision Tree #8 commits an agent to
a header until the ball leaves the head, this spec owns the physical
model, the contact-quality computation, the contested-duel
resolution, the failed-attempt pipeline, and the telemetry surface
emitted on every attempt.

Governance areas:

- **Eligibility predicate** (§3.2) — determines whether a committed
  header may execute on a given physics frame.
- **Jump kinematics integration** (§3.3) — at Stage 0, owns the
  synthetic vertical-axis (Z) trajectory of the agent's head during
  the aerial-contact window per KD-18. AM #2 ground kinematics are
  consumed read-only.
- **Contact-quality computation** (§3.4) — produces a continuous
  scalar in `[0, 1]` from signed timing offset and 2-D point error.
- **Outcome generation** (§3.5, §3.6) — produces outgoing velocity,
  launch angle, and outgoing spin from intent parameters, contact
  quality, body mechanics, and fatigue.
- **Contested-duel resolution** (§3.7) — consumes Collision System
  #3 contact data; resolves multi-agent contests with deterministic
  tie-break.
- **Failed-attempt handling** (§3.9) — emits a structured failure
  event without modifying the ball state.
- **Telemetry surface** (§2.4) — counters / gauges / histograms
  routed to the trace pipeline.

Applicability: every ball contact whose contact body part is
predicted to be `Head`, per Shot Mechanics #6 KD-6 and the
reaffirmation in KD-3 below. This covers open play, set-piece
receptions (corners, free kicks; KD-13), defensive headed
clearances, attacking finishes, and goalkeeper headed contacts
(KD-7). The First Touch Mechanics #4 0.5 m height threshold does
NOT gate head contacts — diving headers and head-on-ground contacts
route here.

Pointer to §1.2 (out-of-scope items), §1.3 (KDs), §1.4 (dependency
and integration contract tables).

---

## 1.2 What Is Out of Scope

- **Goal detection** (whether a scored shot crossed the goal-line) →
  Event System #17 / Match Referee. #10 publishes
  `HeaderExecutedEvent.ownGoalShapedTrajectory: bool` but does not
  adjudicate.
- **Set-piece kick delivery** (the kick itself, not the headed
  reception). At Stage 0, free kicks and corners are kick variants
  consumed from Pass Mechanics #5 via `BallState` only (KD-5,
  KD-13). At Stage 1+ a dedicated set-piece spec may take over. The
  Stage 0 routing is canonical for #10's lifetime in Stage 0.
- **Goalkeeper-specific decision logic** (when to punch vs. catch
  vs. clear) → Goalkeeper Mechanics #11 (NOT STARTED). #10 owns the
  GK head-contact physics layer unchanged per KD-7.
- **Concussion / injury accumulation** — Stage 1+ Medical/Injury
  spec (no current spec slot; KD-15).
- **Weak-aerial-side asymmetry** — Stage 1+ once validation data
  exists (KD-14).
- **Header-pass labelling** (was this a clearance, flick-on,
  knock-down, glancing finish?) — downstream telemetry classifier
  consuming `HeaderExecutedEvent`; NOT a physics input or a #10
  publication at Stage 0 (§7.4, §7.11).

---

## 1.3 Key Design Decisions

Eighteen Key Design Decisions are pre-committed for this spec. Each
KD is restated below as: statement, rationale, and
consequence-if-violated. KD numbering is canonical and is cited by
FR rows in §2.1.

### KD-1 — Parameter-based contact model (no header-type enum)

**Statement.** No `HeaderType` / `HeaderClass` / `HeaderStyle` enum
is introduced at any layer. Decision Tree #8 supplies physical
intent parameters (`PowerIntent`, `ContactPointIntent`,
`TargetIntent`); the physics layer produces vectors; named outcome
labels (powered, glancing, defensive clearance) are telemetry only.

**Rationale.** The same trap was closed for Pass Mechanics #5
(`KickType`) and Shot Mechanics #6 (`ShotType`). Enum-based
discriminators in the physics layer fork the formula on label and
prevent continuous parameter interpolation. Resolves v0.1 findings
3, 4.

**Consequence if violated.** Discrete-class branching reintroduces
the bugs #5 and #6 already paid to eliminate.

### KD-2 — Continuous contact-quality scalar

**Statement.** Contact quality is a continuous scalar in `[0, 1]`
derived from a signed timing offset (ms relative to ideal-contact
frame) and a 2-D contact-point error (m) on the head surface.
Named windows `Early` / `OnTime` / `Late` are post-computation
labels emitted into `HeaderExecutedEvent.contactQualityLabel` for
telemetry only, never branched on by the physics formula. (`OnTime`
chosen over `Perfect` per pass-1 L-1.)

**Rationale.** A scalar preserves continuity across the
quality manifold. Resolves v0.1 finding 3.

**Consequence if violated.** Quality cliffs at bucket boundaries
manufacture artificial gameplay discontinuities.

### KD-3 — Body-part discriminator inheritance

**Statement.** Routing inherits from Shot Mechanics #6 KD-6 — any
ball contact whose contact body part is `Head` routes to Spec #10,
regardless of ball height or agent posture. The First Touch #4
0.5 m height threshold does NOT apply to head contacts. Spec #10
owns this rule definitively; Spec #4 continues to gate non-head
body parts on the 0.5 m threshold.

**Rationale.** Diving headers, bicycle-kick head contacts, and
head-on-ground contacts must route uniformly to one physics layer.
Resolves v0.1 finding 15.

**Consequence if violated.** Same contact resolves to two different
physics layers depending on height, producing inconsistent outgoing
ball state for visually identical events.

### KD-4 — `JumpReach` is `[DERIVED]`, not a new `PlayerAttribute`

**Statement.** No revision to APPROVED Agent Movement #2 is
required. Spec #10 defines:

```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
            + JUMP_REACH_K_HEADING  · Heading_norm
```

where `JUMP_REACH_BASE_M` is `[FIXED]` (anatomical) and the three
`JUMP_REACH_K_*` are `[GT]`. `Heading_norm` captures jump-timing
skill until a dedicated timing attribute is justified at Stage 1+
(see §7.10).

**Rationale.** AM #2 is APPROVED. Adding a new
`PlayerAttribute` would require re-opening it. `[DERIVED]` lets us
publish a formula whose only inputs are already-canonical AM #2
attributes. Resolves v0.1 finding 5; resolves pass-1 H-2.

**Consequence if violated.** Reopens AM #2; risks cross-spec
renumbering / fabricated-checklist class of errors.

### KD-5 — Pass Mechanics #5 consumed via `BallState` only

**Statement.** Spec #10 reads incoming `BallState.velocity`,
`BallState.spin`, `BallState.position` at the predicted contact
frame. It does NOT consume Pass-specific labels (`CrossDelivery`,
`LowDriven`, `ChippedCross`).

**Rationale.** Insulates #10 from any future amendment to #5.
Originally motivated when #5 was SUSPENDED; remains sound now that
#5 is APPROVED. Resolves v0.1 finding 8.

**Consequence if violated.** #10 picks up #5's classification
surface and must re-version every time #5 amends its label space.

### KD-6 — Own-goal detection is NOT adjudicated here

**Statement.** Spec #10 produces an outgoing velocity vector and
flags `HeaderExecutedEvent.ownGoalShapedTrajectory: bool` when the
projection through the corner-origin coordinate space intersects
the defender's own goal-line bounding box within the next ballistic
phase. Whether a goal was scored is an Event System #17 / Match
Referee concern.

**Rationale.** Separates physics from adjudication. Resolves v0.1
finding 9.

**Consequence if violated.** Two specs end up adjudicating goals;
drift between them produces score discrepancies.

### KD-7 — Goalkeeper head-contact ownership

**Statement.** GK head contacts (punching, headed goal-line
clearance, goal-line head-on-ground contact) execute the Heading
Mechanics #10 pipeline. Goalkeeper Mechanics #11 (NOT STARTED) may
override `ContactPointIntent` / `PowerIntent` derivation via
GK-specific Decision Tree branches but inherits #10's physics
layer unchanged.

**Rationale.** Pre-commits to prevent re-litigation when #11 is
drafted. Resolves v0.1 finding 16.

**Consequence if violated.** GK head physics forks; calibration
required on two surfaces.

### KD-8 — Contested duel consumes Collision System #3, does not redefine it

**Statement.** Duel resolution reads contact data emitted by
Collision System #3 (contact normal, relative velocity, impulse
budget). Spec #10 layers a Heading-specific resolution on top
(Balance/Strength/Heading scoring, disturbance-factor application
to `ContactQualityScalar`). Spec #3 contact-event interface is
consumed as-published; no #3 revision required.

**Rationale.** Resolves v0.1 finding 17; preserves #3 APPROVED
status.

**Consequence if violated.** Reopens #3.

### KD-9 — Project-invariants citation block

**Statement.** Spec #10 cites and does not restate:
- corner-origin coordinates (Ball Physics #1 §1.2);
- fatigue convention `0 = rested, 1 = fatigued` (CLAUDE.md);
- tick-rate split — 10 Hz tactical for intent selection, 60 Hz
  physics for jump kinematics, contact resolution, and ball-velocity
  emission (CLAUDE.md).

**Rationale.** Pre-committing prevents drift. Resolves v0.1
findings 11, 12, 13.

**Consequence if violated.** Convention drift produces inverted
fatigue / wrong-origin / wrong-tick bugs already seen in this
project's history.

### KD-10 — Determinism governance

**Statement.** All randomness routes through
`DeterministicRngService` (Deterministic Simulation #16 §4.1) with
registered draw-site IDs (#16 §4.5). Iteration over contested-duel
participants follows #16 §3.2 entity ordering. A new domain-tag
allocation `DOMAIN_TAG_HEADING = 0x16` is requested from #16 §3.4
via back-propagation entry `ERR-010-001` (filed in
`docs/tracking/spec-error-log.md` as part of the v0.2 PASS-1 fix
pass; v0.1 declared the entry as "created during section
authoring" but the row was not present in the log — v0.2 M-1
closure). The allocation is a pure namespace amendment to
APPROVED #16 (no `DETERMINISM_DIGEST_VERSION` bump), following the
precedent set by Event System #17's `DOMAIN_TAG_EVENT_LEDGER = 0x15`
patch on May 14, 2026 (#16 §3.4 v1.0.1). Next free slot in #16 §3.4
catalogue is `0x16` (current allocations: `0x10`..`0x15`).
Promotion of the `[CROSS-PENDING]` tag to `[CROSS]` was completed
May 16, 2026 atomically with the #16 §3.5 v1.0.2 patch landing.

**Rationale.** Determinism is a Stage 0 hard requirement. Resolves
v0.1 finding 10 and pass-1 C-2.

**Consequence if violated.** Replay drift; cross-run divergence.

### KD-11 — Constant-tag policy

**Statement.** Every numeric constant published by Spec #10
carries exactly one of `[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` /
`[CROSS]` / `[CROSS-PENDING]`. §9 Approval Checklist programmatically
verifies every constant in `section-3.md` and `appendices.md`
before approval. No magic numbers in formula code.

**Rationale.** Closes ERR-005 fabricated-checklist trap. Resolves
v0.1 finding 14.

**Consequence if violated.** Magic numbers re-enter; tunability
audit fails.

### KD-12 — Failed-attempt physics is well-defined

**Statement.** A header attempt that fails contact (mistimed jump;
ball passes through head zone within tolerance but is not touched)
produces: NO `Ball.ApplyKick` call; ball trajectory unchanged; a
`HeaderAttemptFailedEvent` published with timing and miss-distance
telemetry.

**Rationale.** Eliminates a silent-failure class. Resolves v0.1
finding 19.

**Consequence if violated.** Mistimed headers either silently
alter the ball or silently drop telemetry.

### KD-13 — Set-piece headers ARE in Stage 0 scope

**Statement.** The cross (free kick, corner) is delivered by Pass
Mechanics #5; the header off that cross is mechanically identical
to an open-play header from a cross because incoming spin /
velocity / position are read uniformly from `BallState` regardless
of delivery type. Any set-piece-specific in-swing / out-swing
characteristics are produced by #5 on the kick and propagated
through `BallState` before #10 sees the ball. Wall presence and
defender-pile geometry are handled by Collision System #3 contact
events, not #10. Spec #10 covers both open-play and set-piece
headers; set-piece kick delivery (the kick itself) remains deferred
to Stage 1+ per Shot Mechanics #6 §1.2.

**Rationale.** Resolves v0.1 finding 20; resolves pass-1 M-7.

**Consequence if violated.** Set-piece headers fork from open-play
headers — two surfaces to calibrate.

### KD-14 — Weak-aerial-side handling deferred to Stage 1+

**Statement.** No `WeakAerialSide` attribute is introduced at
Stage 0. §7.1 records the deferral.

**Rationale.** Validation data for the asymmetry premise is
unavailable. Resolves v0.1 finding 18.

**Consequence if violated.** Pre-data `[EST]` constants enter the
catalogue with no upgrade path.

### KD-15 — Concussion / injury modeling deferred to Stage 1+

**Statement.** No injury-system spec exists in the 20-spec set.
§7.2 records the deferral.

**Rationale.** Resolves v0.1 finding 21.

**Consequence if violated.** Modelling injury without a Medical
spec produces inconsistent injury surfaces across specs.

### KD-16 — Spin transfer is Heading-owned

**Statement.** Outgoing spin computation lives in Spec #10 §3.6
(not Ball Physics #1). Ball Physics receives the final spin vector
via `Ball.ApplyKick`; Spec #10 transforms incoming spin + contact
geometry + head angular velocity into outgoing spin.

**Rationale.** Spin transfer depends on contact point and head
velocity vector, which only #10 knows. Resolves v0.1 finding 22.

**Consequence if violated.** Ball Physics #1 must learn about head
geometry, polluting a pure-physics spec.

### KD-17 — Intent-staleness re-validation policy

**Statement.** `HeaderIntent` is committed at the 10 Hz tactical
tick (Decision Tree #8) and may be 1–18 physics frames stale by
the predicted contact frame. Policy:

(a) `targetIntent` is **held fixed** after commit — the player
chose to head it *there*; deviation from a re-evaluated ideal is
what `contactQualityScalar` already captures via `pointError`.

(b) `predictedContactFrame` is **re-evaluated every physics
frame** in §3.2 until either contact occurs or the attempt window
closes; if the new prediction falls outside the attempt window
after commit, `HeaderAttemptFailedEvent` is emitted with
`failureCause = MistimedEarly` / `MistimedLate` per the signed
drift direction.

(c) `powerIntent` and `contactPointIntent` are **held fixed**
(locked at commit) — Decision Tree #8 does NOT re-issue these
mid-attempt.

**Rationale.** Resolves pass-1 M-5.

**Consequence if violated.** Intent thrashing across physics
frames; non-deterministic outcomes when DT re-evaluates mid-attempt.

### KD-18 — Stage 0 jump kinematics are Heading-owned

**Statement.** Agent Movement #2 explicitly defers Z>0 movement to
Stage 1+ (per AM #2 `section-3-6-part-2.md` §3.6 comment). At
Stage 0, #10 owns the full vertical-axis kinematic during the
aerial-contact window: it derives a synthetic apex-altitude
trajectory from `JumpReach` (per KD-4) and a `[GT]` jump-duration
profile, advances head position on the 60 Hz physics tick during
the aerial phase, and exits back to the AM #2-owned XY ground
state on landing (entering `GROUNDED` if a diving header was
performed, per AM #2 §3.1.2 `GroundedReason.DIVING_HEADER`). No
amendment to APPROVED #2 is required at Stage 0. When AM #2 grows
native Z kinematics at Stage 1+, #10's synthetic jump trajectory is
the natural retire target (§7.8).

**Rationale.** Resolves pass-1 cross-cutting AM #2 jump-surface
absence; preserves AM #2 APPROVED status.

**Consequence if violated.** Reopens AM #2; risks cross-spec
renumbering / fabricated-checklist trap class.

---

## 1.4 Dependencies and Integration Contracts

### Upstream Specs (direct dependencies, all APPROVED)

| Spec | Consuming §10 surface | Upstream anchor |
|------|----------------------|-----------------|
| Ball Physics #1 | Coordinate-system authority; `BallState` snapshot at predicted contact frame; `Ball.ApplyKick` output. | §1.2 (origin); §3.1.11.2 (`Ball.ApplyKick`). |
| Agent Movement #2 | `Agent` class XY kinematic state and `facing`; `PlayerAttributes` (`Heading`, `Strength`, `Balance`); `AgentMovementState` enum + `GroundedReason.DIVING_HEADER`. NO jump-kinematics anchor — Z is owned by #10 at Stage 0 per KD-18. | §3.1.2 (state machine); §3.5.1 (`Agent` class); §3.5.6 (`PlayerAttributes` struct). |
| Collision System #3 | Head-ball contact resolution via push-API `ICollisionEventConsumer` (KD-8). #10 implements the consumer interface and buffers events per-frame (§4.2.1). | §3.4.2 (`section-3-4.md` lines 387–445; `ICollisionEventConsumer` interface + `CollisionEvent` struct). v0.3 OI-005 closure. |
| First Touch Mechanics #4 | Boundary partner — head-vs-non-head body-part discrimination. | §1.2 (boundary statement reaffirming 0.5 m threshold does not apply to head). |
| Pass Mechanics #5 | Canonical cross-delivery source consumed read-only via `BallState`, not Pass-specific labels (KD-5). | No #5 subsection coupling. |
| Shot Mechanics #6 | Analogous output-interface model; KD-6 body-part routing authority. | §1.3 KD-6. |
| Decision Tree #8 | `HeaderIntent` at 10 Hz — **Stage 0+1 activation** (DT-side wiring lands atomic with #8 §7 stub promotion). | §1.7.2 row "Heading Mechanics #10 (Stage 1) — HEADER action type and dispatch interface — Not defined at Stage 0; stub placeholder in §7." v0.3 OI-005 closure: anchored to #8's existing Stage 0 deferral row; activation framing in §4.6.1. |
| Deterministic Simulation #16 | RNG service + draw-site registry; entity ordering; `DOMAIN_TAG_HEADING = 0x16` (`[CROSS]` post May 16, 2026). | §3.2 (ordering); §3.4 (`DOMAIN_TAG_HEADING` row, `[CROSS]` post #16 §3.5 v1.0.2); §4.1 (RNG service); §4.5 (draw-site registry). ERR-010-001 RESOLVED. |
| Event System #17 | `HeaderExecutedEvent` (Tier B) and `HeaderAttemptFailedEvent` (Tier C) publication. | §3.2.1 `Publish API surface` (`section-3.md` lines 104–127). v0.3 OI-005 closure. |

**Tractability cites (not consumed at any interface — named for context only):**
- Perception System #7 — supplies Decision Tree #8's inputs; relevant
  upstream of #8, not of #10.

### Downstream Specs (consumers; specs NOT STARTED — interface declared here, not negotiated)

| Spec | Consumed surface |
|------|------------------|
| Goalkeeper Mechanics #11 | `HeaderExecutedEvent` for reaction-trigger; head-contact ownership delineated in KD-7. |
| Positioning AI #12 | `HeaderExecutedEvent` for marking re-acquisition; no interface surface beyond the event. |
| Defensive AI #14 | Aggregate header statistics via `HeaderExecutedEvent`; no per-call interface. |
| Attacking AI #15 | Aggregate header statistics via `HeaderExecutedEvent`; no per-call interface. |

### Pass Mechanics #5 Amendment-Insulation Note (KD-5)

#10 consumes `BallState.velocity`, `BallState.spin`,
`BallState.position` only. Any future amendment to #5's classification
labels (`CrossDelivery`, `LowDriven`, …) does not propagate to #10.
Set-piece in-swing/out-swing characteristics are encoded in the
ball's spin and velocity by #5 before #10 reads `BallState`.

### NOT-STARTED Downstream Note

Goalkeeper #11, Positioning AI #12, Defensive AI #14, Attacking AI
#15 are NOT STARTED. The `HeaderExecutedEvent` /
`HeaderAttemptFailedEvent` interface is declared here and is
consumed as-published by the downstream specs when they reach
drafting.

---

## 1.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | section authoring | Initial draft from `outline-detailed.md` v1.1. All eighteen KDs reproduced. Dependency tables enumerated with verified upstream anchors where pinned; remaining anchors marked TBD per OI-005. | pending |
| 0.2 | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: KD-10 wording adjusted to reflect actual back-prop filing status (M-1) — `ERR-010-001` now filed in `spec-error-log.md` atomically with this revision. No KD set changes; KD-10's design intent unchanged. | pending |
| 0.3 | May 16, 2026 | drafter | APPROVAL. §1.4 dependency table re-anchored to APPROVED upstream surfaces: #3 §3.4.2 `ICollisionEventConsumer`; #8 §1.7.2 Stage 0 deferral row (DT-side wiring is Stage 0+1); #16 `DOMAIN_TAG_HEADING = 0x16` row `[CROSS]` post #16 §3.5 v1.0.2 patch; #17 §3.2.1 `Publish API surface`. ERR-010-001 RESOLVED. | granted |
