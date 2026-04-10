# Shot Mechanics Specification #6 — Section 1: Purpose & Scope

**File:** `Shot_Mechanics_Spec_Section_1_v1_1.md`
**Purpose:** Defines the complete scope boundary for Shot Mechanics Specification #6 —
what this system owns, what it explicitly does not own, all key architectural decisions,
relationships to adjacent systems, and dependency contracts required before Section 3
can be drafted.

**Created:** February 22, 2026, 10:30 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Supersedes:** Shot_Mechanics_Spec_Section_1_v1_0.md (contained ShotType enum — eliminated
in outline v1.2. That file should be considered void.)

**Dependencies confirmed stable:**
- Ball Physics Specification #1 (approved)
- Agent Movement Specification #2 (approved — v1.3 includes Finishing, LongShots,
  Composure, KickPower, WeakFootRating in PlayerAttributes §3.5.6)
- Collision System Specification #3 (approved)
- Pass Mechanics Specification #5 (approved — interface patterns reused directly)
- Shot Mechanics Outline v1.2 (approved — ShotType enum eliminated, OI-006 resolved)

**Open Dependency Flags:** None.

---

## Table of Contents

- [1.1 Document Purpose](#11-document-purpose)
- [1.2 What This Specification Covers](#12-what-this-specification-covers)
- [1.3 What Is Out of Scope](#13-what-is-out-of-scope)
  - [1.3.1 Responsibilities Owned by Other Specifications](#131-responsibilities-owned-by-other-specifications)
  - [1.3.2 Features Deferred to Stage 1+](#132-features-deferred-to-stage-1)
  - [1.3.3 Permanent Exclusions](#133-permanent-exclusions)
- [1.4 Key Design Decisions](#14-key-design-decisions)
- [1.5 Relationship to Adjacent Systems](#15-relationship-to-adjacent-systems)
  - [1.5.1 System Context](#151-system-context)
  - [1.5.2 What Shot Mechanics Is Not Responsible For](#152-what-shot-mechanics-is-not-responsible-for)
- [1.6 Dependencies and Integration Contracts](#16-dependencies-and-integration-contracts)
  - [1.6.1 Hard Dependencies — Must Be Stable Before Section 3](#161-hard-dependencies--must-be-stable-before-section-3)
  - [1.6.2 Soft Dependencies — Forward References to Unwritten Specifications](#162-soft-dependencies--forward-references-to-unwritten-specifications)
- [1.7 Version History](#17-version-history)

---

## 1.1 Document Purpose

This section defines the scope boundaries for Shot Mechanics Specification #6. It serves
as the authoritative reference for:

1. **What this specification owns** — Translation of physical intent parameters into
   `Ball.ApplyKick()` arguments: velocity magnitude, launch direction, and spin vector.
2. **What other specifications own** — The decision to shoot, ball trajectory after
   contact, goalkeeper response, goal detection, and all head-contact attempts.
3. **Key design decisions** — Seven architectural decisions, locked before technical
   drafting begins, with full rationale.
4. **Implementation timeline** — Stage 0 deliverables versus deferred and permanently
   excluded features.
5. **Dependencies** — Required interfaces from upstream specifications and forward
   contracts with downstream specifications not yet written.

Shot Mechanics is the most physically extreme action system in Stage 0. A shot demands
maximum physical output from an agent, produces the highest ball velocities in normal
play (up to 35 m/s), and determines the most consequential single outcome in a match.
The central design problem is modelling the tension between **power** and **placement**:
a player who strikes the ball at maximum force necessarily sacrifices accuracy, and this
trade-off must be expressed explicitly in the mathematics, not corrected for after the
fact.

This specification covers only the interval from shot decision to ball departure from
foot. Everything before (decision-making, parameter selection) and everything after
(ball flight, goalkeeper response, goal detection) is owned by adjacent systems with
defined boundary interfaces.

**On shot labels and physics:** Terms like "driven shot," "chip," and "finesse" are
Decision Tree vocabulary describing what kind of shot an agent intends to play. They are
not enums passed to or processed by this system. Shot Mechanics receives physical intent
parameters — how hard, where on the ball, how much spin — and produces physical outputs:
a velocity vector and a spin vector. The ball's resulting behaviour (low and hard,
looping with backspin, curling to the far post) emerges from those vectors as Ball
Physics simulates them. Named shot labels carry zero physics weight in this system.

---

## 1.2 What This Specification Covers

Shot Mechanics Specification #6 governs the translation of a `ShotRequest` — submitted
by the Decision Tree — into a fully-specified ball state via a call to `Ball.ApplyKick()`.
This specification owns everything that happens between the moment a shooting decision
is made and the moment the ball leaves the agent's foot.

**Specifically, this specification covers:**

**ShotRequest intake and validation.** The system receives a `ShotRequest` struct
containing `PowerIntent`, `ContactZone`, `SpinIntent`, `PlacementTarget`, `IsWeakFoot`,
`DistanceToGoal`, and `MatchTime`. These are physical intent parameters supplied by the
Decision Tree. The system validates ranges and rejects malformed requests with a log
entry; it does not re-derive or override caller-supplied values.

**Ball velocity model.** The governing formula maps `PowerIntent`, a distance-blended
effective attribute (`Finishing` / `LongShots` sigmoid blend), `KickPower`, `ContactZone`,
`SpinIntent`, fatigue, contact quality, and `WeakFootRating` to a scalar ball exit speed.
The formula is fully derived in Appendix A. Velocity is emergent from parameters — it is
not looked up from a type table.

**Placement model.** The shooter specifies an intended target as a normalised coordinate
`(u, v)` in goal-relative space, where `u ∈ [0, 1]` is left-to-right and `v ∈ [0, 1]`
is low-to-high on the goal mouth. Error is applied as an offset in this goal-relative
coordinate space before conversion to a world-space velocity direction. This enables
clean "aimed at the bottom corner but skied it" outcomes without world-space error
geometry.

**Launch angle derivation.** Launch angle emerges from `ContactZone`, `PowerIntent`,
`SpinIntent`, body lean, and body mechanics score. It is not assigned from a per-type
table. `BelowCentre` contact combined with high `SpinIntent` produces chip-arc
trajectories; `Centre` contact at high `PowerIntent` produces low, driven trajectories.
The body lean penalty is the primary source of shots skied over the bar.

**Spin vector calculation.** Three spin components are modelled: topspin (dipping
trajectory), sidespin (curl), and backspin (loft and check on landing). Magnitudes are
driven by `ContactZone` and `SpinIntent`; sidespin additionally scales with the `Technique`
attribute. The resulting `Vector3` is passed directly to `Ball.ApplyKick()` and processed
by Ball Physics's Magnus effect model.

**Shot error model.** Inaccuracy is deterministic, generated from a hash of
`matchSeed + agentId + frameNumber`. Error magnitude is a function of `Finishing`,
`Composure`, `PowerIntent` (power–accuracy trade-off), pressure scalar, fatigue, and
body mechanics score. The result is an angular offset applied to the intended placement
direction before velocity composition.

**Body mechanics evaluation.** Run-up angle relative to ball-to-target bearing, plant
foot lateral offset, agent velocity at contact, and body lean are read from
`AgentPhysicalProperties` and collapsed to a scalar `BodyMechanicsScore ∈ [0.0, 1.0]`.
This score modulates contact quality, error magnitude, launch angle deviation, and —
when critically low combined with high `PowerIntent` — triggers a stumble signal to
Agent Movement.

**Weak foot penalty.** When `ShotRequest.IsWeakFoot` is true, a penalty multiplier
derived from `WeakFootRating` (Agent Movement §3.5.6) is applied to both velocity and
the error cone. The multiplier model is identical in structure to Pass Mechanics §3.7;
the error cone expansion is larger than in passing because shots demand more precise
contact.

**Shot execution state machine.** The system moves through states:
`IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE`. WINDUP duration
scales with `PowerIntent` and `SpinIntent` — not with a type label. Cancellation from a
tackle interrupt is handled at `WINDUP` via a flag from Collision System #3, using the
identical pattern established in Pass Mechanics. A `STUMBLING` transition may occur at
`FOLLOW_THROUGH` when body mechanics and power thresholds are met simultaneously.

**Ball.ApplyKick() call.** The sole physical output of this system:
```
Ball.ApplyKick(velocity: Vector3, spin: Vector3, agentId: int, matchTime: float)
```
This interface is defined in Ball Physics §3.1.11.2. Shot Mechanics constructs `velocity`
and `spin`; the remaining parameters pass through from `ShotRequest`.

**ShotExecutedEvent publication.** Upon reaching `CONTACT` state, the system publishes
a `ShotExecutedEvent` struct to the event bus. The struct carries trajectory and quality
data sufficient for Goalkeeper Mechanics (#11) to calculate save difficulty when that
specification is written. No `IGkResponseSystem` interface is defined here.

**Performance compliance.** The entire execution pipeline — from `ShotRequest` receipt
to `Ball.ApplyKick()` call and event publication — must complete within **0.05 ms** on
the reference CPU at 200 agents on-pitch. Shot Mechanics is a discrete event, not a
per-frame system. Budget analysis is in Section 6.

---

## 1.3 What Is Out of Scope

### 1.3.1 Responsibilities Owned by Other Specifications

The following responsibilities are explicitly excluded. Each is owned by a named
specification with a defined boundary interface. Any feature from this list found in
Section 3 or later is a scope violation.

| Responsibility | Owning Specification | Boundary Interface |
|---|---|---|
| Decision to shoot; selection of `PowerIntent`, `ContactZone`, `SpinIntent`, target; intent parameter values | Decision Tree Spec #8 | Decision Tree constructs and submits `ShotRequest` |
| Ball trajectory after kick — aerodynamics, Magnus effect, bounce, roll | Ball Physics Spec #1 | Shot Mechanics calls `Ball.ApplyKick()`; Ball Physics owns simulation from that point |
| Goalkeeper positioning, dive decision, save attempt, and outcome resolution | Goalkeeper Mechanics Spec #11 | Shot Mechanics publishes `ShotExecutedEvent`; GK Mechanics subscribes when written |
| All ball contacts where contact body part is the **head**, regardless of ball height or agent posture | Heading Mechanics Spec #10 | Contact body part is the sole discriminator (KD-6) |
| Collision detection during shot windup — tackle interrupt detection | Collision System Spec #3 | Collision System sets a flag; Shot Mechanics reads it at `WINDUP` state |
| Goal detection and match state update | Match Referee / Event System Spec #17 | `ShotExecutedEvent` carries trajectory data; goal detection is downstream |
| Set piece shots — penalties, direct and indirect free kicks, corners | Set Pieces System (Stage 1+) | Set pieces require distinct pre-kick ritual and pressure modelling |
| Agent animation during shot execution | Animation System (Stage 1+) | `ShotAnimationData` struct populated but unconsumed at Stage 0 |
| xG model and shot quality statistics | Statistics Engine (Stage 1+) | `ShotExecutedEvent` carries fields required for future xG; Statistics Engine subscribes in Stage 1+ |
| Goalkeeper distribution — goal kicks, punts, foot-struck throws | Goalkeeper Mechanics Spec #11 | These do not enter the Shot Mechanics state machine at Stage 0 |

### 1.3.2 Features Deferred to Stage 1+

The following are within the long-term scope of Shot Mechanics but explicitly deferred
from Stage 0. Architectural hooks are documented in Section 7 to prevent structural
rework.

**Set piece shot variants.** Penalty kicks, free kick curls, and driven corners require
pre-kick ritual modelling, psychological pressure modifiers, and referee delay variance
that are out of Stage 0 scope. `ContactZone` and `SpinIntent` parameters will extend
naturally to set piece shots without structural change.

**Form modifier integration.** `PlayerAttributes.FormModifier` is defined but set to
`1.0f` at Stage 0. When the Form System is implemented, the `EffectiveAttribute` formula
in §3.2 incorporates this modifier without structural change to Shot Mechanics.

**Psychology and H-Gate pressure.** `MatchStressLevel` from the H-Gate System (Master
Volume 2 — not yet designed) will add a multiplier to the pressure scalar in the error
formula. A `PsychologyPressureScale` field defaults to `1.0f` at Stage 0.

**Injury-affected shot mechanics.** `PlayerAttributes.InjuryLevel` is read but its
effect is not applied at Stage 0. A zero-injury assumption holds. Stage 2 will apply
injury as a degradation to both velocity and `BodyMechanicsScore`.

**Extended ContactZone resolution.** Three contact zones (Centre, BelowCentre, OffCentre)
are used at Stage 0. Full biomechanical contact-point modelling — toe/instep/outside-foot
discrimination, exact contact geometry — is deferred to Stage 1+. The `ContactZone` enum
is designed for extension without breaking the Stage 0 interface.

**Off-balance and diving shot variants.** Shots are only initiated from `RUNNING`,
`IDLE`, or `JOGGING` locomotion states at Stage 0. Shots requested from other states are
rejected with a log entry. Stage 2 will extend body mechanics evaluation to cover
`STUMBLING` and `DIVING` initiation states.

### 1.3.3 Permanent Exclusions

**Probabilistic outcome rolls.** Shot outcomes are never determined by a random number
generator. All outputs are deterministic given identical inputs. This is a hard
requirement for replay determinism (Master Vol 1 §1.3). The deterministic hash approach
produces apparent variance without non-determinism.

**Goal-or-no-goal adjudication.** This system determines where the ball goes after
contact. Whether that trajectory scores a goal requires boundary checking against goal
geometry and is a Match Referee function.

**Post-shot possession transfer.** Whether a shot becomes a save, corner, goal kick, or
rebound is determined by Collision System, Goalkeeper Mechanics, and Ball Physics in
combination. Shot Mechanics publishes the trajectory and terminates.

**Tactical shot selection heuristics.** Shoot vs. pass decisions, target selection, and
intent parameter value choices are Decision Tree concerns. Shot Mechanics receives a
committed request and executes it.

---

## 1.4 Key Design Decisions

The following decisions were resolved during outline development (Outline v1.1 and v1.2,
all six open issues closed). They are locked before technical drafting. Any change
requires an amendment to this section and the outline, with a version bump, before
Section 3 drafting continues.

---

**KD-1: Power–accuracy trade-off is explicit in the formula.**

A player cannot simultaneously maximise velocity and minimise error. A `PowerPenalty`
scalar widens the error cone as `PowerIntent` approaches 1.0, using a quadratic form:

```
PowerPenalty(p) = 1 + POWER_PENALTY_COEFFICIENT × p²
```

Error grows slowly at moderate power and sharply at maximum effort. This is grounded in
biomechanics literature (Lees & Nolan 1998) and is the primary mechanism producing
realistic shot inaccuracy at high power.

---

**KD-2: Placement intent is goal-relative, not world-space.**

The `ShotRequest` specifies a target as `PlacementTarget: Vector2`, a normalised
coordinate `(u, v)` on the goal mouth. `u = 0` is the left post, `u = 1` is the right
post, `v = 0` is the ground, `v = 1` is the crossbar. Error is applied as an offset in
this coordinate space before conversion to a world-space velocity direction.

Goal-relative error produces immediately interpretable outcomes: positive `v` error is
"ball skied high," negative `u` error is "pulled left." This simplifies Appendix B
numerical verification and provides a clean basis for future xG statistics without
requiring world-space angular error geometry.

---

**KD-3: Named shot labels are Decision Tree vocabulary. Shot Mechanics is parameter-based.**

The Decision Tree selects what kind of shot to play and encodes that intent as physical
parameters: `PowerIntent`, `ContactZone`, and `SpinIntent`. Shot Mechanics receives
these parameters and produces physical output. It does not maintain a shot type
taxonomy, does not accept a shot type enum, and does not classify intent from context.

The physical output — velocity magnitude, launch angle, spin vector — emerges from the
parameter combination. A high-`PowerIntent`, Centre-contact, low-`SpinIntent` request
produces a fast, low trajectory. A moderate-`PowerIntent`, BelowCentre-contact,
high-`SpinIntent` request produces a looping backspin trajectory. These are not named
shot types inside this system; they are outcomes of the physics.

This is consistent with the KickType resolution established during Pass Mechanics
drafting: Ball Physics operates on velocity and spin vectors only. A type enum passed
through to Ball Physics would carry zero physics weight and create false coupling between
two systems that do not need it.

The Decision Tree is responsible for translating tactical intent ("chip over the
goalkeeper") into parameter values. Shot Mechanics is responsible for translating those
parameter values into physics.

---

**KD-4: All error is deterministic. No probabilistic dice rolls.**

Shot error is generated by a deterministic hash function seeded with
`matchSeed + agentId + frameNumber`. Given identical seeds and inputs, output is always
identical. This guarantees replay fidelity at the physics level.

The three-component seed is the minimum required for correctness: `matchSeed + agentId`
alone repeats the same error direction for a given player every frame; `matchSeed +
frameNumber` alone produces identical errors for two agents shooting on the same frame.
The combined seed prevents both failure modes. The hash signature is intentionally
identical to Spec #15 RNG for drop-in migration.

*(Resolved: OI-004 — Option A)*

---

**KD-5: STUMBLING is a conditional post-shot trigger, not a guaranteed effect.**

When `BodyMechanicsScore < STUMBLE_THRESHOLD` AND `PowerIntent > STUMBLE_POWER_THRESHOLD`,
the system emits a stumble signal to Agent Movement at `FOLLOW_THROUGH` state. Agent
Movement handles the locomotion consequence using its existing hysteresis pattern
(Agent Movement §3.1) — no new interface is required.

The dual threshold ensures stumbling is only triggered when genuine instability is
present: poor stance combined with maximum-effort contact. A moderate-power shot struck
slightly off-balance should not produce a stumble. A maximum-power attempt with the
plant foot mispositioned should.

Threshold values are gameplay-tuned [GT] constants documented in §3.6 and Appendix C.

*(Resolved: OI-003)*

---

**KD-6: Contact body part is the sole discriminator between Shot Mechanics and Heading Mechanics.**

Foot contact → Shot Mechanics, unconditionally.
Head contact → Heading Mechanics, unconditionally.

Agent airborne state, ball height, and locomotion state are irrelevant to this routing
decision. The 0.5m contact-height threshold used in First Touch Mechanics #4 does not
apply here and must not be imported into any section of this specification.

Contact body part is an unambiguous discrete discriminator determined at the moment of
contact, requiring no ongoing evaluation. This maps cleanly to real football adjudication
(whether a goal stands is determined by what body part struck the ball, not how high
it was) and eliminates all boundary ambiguity.

*(Resolved: OI-002)*

---

**KD-7: ContactZone is a physical descriptor, not a shot label.**

Three contact zones are defined: `Centre`, `BelowCentre`, `OffCentre`. These describe
where on the ball face the foot makes contact — they are physical geometry descriptors,
not intent labels. `Centre` contact produces topspin-dominant, driven trajectories.
`BelowCentre` contact imparts backspin and lift. `OffCentre` contact imparts sidespin
and curl.

`ContactZone` is a 3-value enum at Stage 0, selected by the Decision Tree based on the
contact the agent is attempting to make. It is designed for extension in Stage 1+ to
a finer-grained or continuous representation without breaking the Stage 0 interface.

Full biomechanical contact-point modelling deferred to Stage 1+.

*(Resolved: OI-006 — ShotType enum elimination)*

---

## 1.5 Relationship to Adjacent Systems

### 1.5.1 System Context

Shot Mechanics occupies a narrow but critical position in the Stage 0 system graph.
All upstream dependencies are approved; downstream consumers are either approved
(Ball Physics) or not yet written (Goalkeeper Mechanics, Event System).

```
Decision Tree (#8)
        │
        │  ShotRequest {PowerIntent, ContactZone, SpinIntent,
        │               PlacementTarget, IsWeakFoot, DistanceToGoal, MatchTime}
        ▼
Shot Mechanics (#6) ── reads ──► Agent Movement (#2) [attributes, physical state]
        │             ── reads ──► Collision System (#3) [tackle interrupt flag]
        │
        │  Ball.ApplyKick(velocity: Vector3, spin: Vector3, agentId, matchTime)
        ▼
Ball Physics (#1)

Shot Mechanics (#6) ── publishes ──► ShotExecutedEvent
        │                                    │
        │                                    ├──► Event System (#17) [when written]
        │                                    └──► Goalkeeper Mechanics (#11) [when written]
        │
        └── emits (conditional) ──► Agent Movement (#2) [stumble signal at FOLLOW_THROUGH]
```

### 1.5.2 What Shot Mechanics Is Not Responsible For

Once `Ball.ApplyKick()` is called, Shot Mechanics' responsibility ends. It does not
monitor subsequent ball trajectory, evaluate whether the shot was on-target, assess
goalkeeper response, or update match state.

Shot Mechanics does not make decisions. It receives a committed `ShotRequest` and
executes it faithfully. Suboptimal decisions — wrong parameters for the situation,
poor target selection — are Decision Tree concerns.

Shot Mechanics does not own the agent's return to normal locomotion after a shot.
The `COMPLETE` state emits a locomotion release signal; Agent Movement manages the
transition back. If a stumble is triggered at `FOLLOW_THROUGH`, Agent Movement manages
the stumble and recovery. Shot Mechanics has no further involvement after emitting the
signal.

---

## 1.6 Dependencies and Integration Contracts

### 1.6.1 Hard Dependencies — Must Be Stable Before Section 3

All hard dependencies are confirmed stable. No blocking items exist.

| Dependency | Specification | Status | Fields Required |
|---|---|---|---|
| `Ball.ApplyKick()` interface | Ball Physics #1 §3.1.11.2 | ✅ Approved | `velocity: Vector3`, `spin: Vector3`, `agentId: int`, `matchTime: float` |
| `PlayerAttributes` struct | Agent Movement #2 §3.5.6 (v1.3) | ✅ Approved | `Finishing`, `LongShots`, `KickPower`, `WeakFootRating`, `Technique`, `Composure` |
| `AgentPhysicalProperties` struct | Agent Movement #2 §3.5.4 | ✅ Approved | `Position`, `Velocity`, `LocomotionState`, `FacingDirection` |
| Fatigue scalar | Agent Movement #2 §3.5.6 | ✅ Approved | `Fatigue: float [0–1]` |
| Tackle interrupt flag | Collision System #3 | ✅ Approved | Cancellation flag read at `WINDUP` state |
| Weak foot penalty model pattern | Pass Mechanics #5 §3.7 | ✅ Approved | Pattern reused directly; no new interface |
| Stumble trigger pattern | Agent Movement #2 §3.1 | ✅ Approved | Existing hysteresis pattern reused; no new interface |

**Attribute read contract:** Attributes and agent state are read **once**, at `INITIATING`
state, before `WINDUP` begins. They are captured and held constant for the duration of
the execution pipeline. A mid-shot attribute change — not possible in Stage 0, possible
in Stage 3+ — cannot corrupt the in-flight calculation.

### 1.6.2 Soft Dependencies — Forward References to Unwritten Specifications

| Consumer Specification | What Shot Mechanics Produces | Interface Status |
|---|---|---|
| Decision Tree Spec #8 | `ShotRequest` struct — Section 3.1 defines the full struct the Decision Tree must satisfy | Caller contract; Decision Tree implements it |
| Goalkeeper Mechanics Spec #11 | `ShotExecutedEvent` struct (KD-7 defines all fields) | Event struct only — no `IGkResponseSystem` |
| Event System Spec #17 | `ShotExecutedEvent` publication to event bus | Event bus subscription pattern confirmed in Section 4 |
| Statistics Engine (Stage 1+) | `ShotExecutedEvent` fields sufficient for xG calculation | `BodyMechanicsScore`, `PowerIntent`, `EstimatedTarget`, `KickVelocity` reserved |
| Animation System (Stage 1+) | `ShotAnimationData` struct — populated but unconsumed at Stage 0 | Stub struct defined in Section 3 |
| Fixed64 Math Library Spec #9 | Trig functions in spin and angle derivations | Float arithmetic at Stage 0; migration documented in Section 7 |

---

## 1.7 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 22, 2026, 9:30 PM PST | Claude (AI) / Anton | Initial draft — contained ShotType enum. Voided by outline v1.2. |
| 1.1 | February 22, 2026, 10:30 PM PST | Claude (AI) / Anton | Full redraft. ShotType enum eliminated. Parameter-based model throughout. KD-3 rewritten. KD-7 added (ContactZone as physical descriptor). All 6 OIs reflected. |
| 1.2 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: Decision Tree #7→#8, Heading Mechanics #9→#10, Goalkeeper Mechanics #10→#11, Fixed64 Math Library #8→#9 (spec renumbering cascade per PROGRESS.md and Perception System §7). |

---

## Section 1 Summary

Shot Mechanics Specification #6 governs translation of physical intent parameters
(`PowerIntent`, `ContactZone`, `SpinIntent`, `PlacementTarget`) into ball state via
`Ball.ApplyKick()`. It owns velocity calculation, placement resolution, spin vector
derivation, error modelling, body mechanics evaluation, state machine execution, and
event publication. It does not own the decision to shoot, parameter value selection,
ball trajectory physics, goalkeeper response, goal detection, or any head-contact action.

Named shot labels (driven, chip, finesse, etc.) are Decision Tree vocabulary and have no
representation inside this system. Physical output emerges from parameter combinations.
This is consistent with the KickType resolution from Pass Mechanics.

Seven key design decisions are locked. All hard dependencies are confirmed stable. No
blocking items exist.

**Next:** Section 2 — System Overview and Functional Requirements.

---

*End of Section 1 — Shot Mechanics Specification #6*
*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*