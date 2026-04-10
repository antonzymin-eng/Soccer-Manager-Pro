# Pass Mechanics Specification #5 — Section 1: Purpose & Scope

**File:** `Pass_Mechanics_Spec_Section_1_v1_0.md`
**Purpose:** Defines the complete scope boundary for Pass Mechanics Specification #5 —
what this system owns, what it explicitly does not own, key architectural decisions,
relationships to adjacent systems, and dependency contracts required before Section 3
can be drafted.

**Created:** February 20, 2026, 2:00 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Dependencies:**
- Ball Physics Specification #1 (approved)
- Agent Movement Specification #2 (in review; amendments ERR-006, ERR-007, ERR-008 pending)
- Collision System Specification #3 (approved)
- First Touch Specification #4 (in review)
- Pass Mechanics Outline v1.0 (approved)
- Spec Error Log v1.4 (active)

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — KickPower, WeakFootRating, Crossing absent from PlayerAttributes
- `[ERR-008-PENDING]` — PossessingAgentId design decision (Option A vs B) unresolved;
  ApplyKick() amendment AM-001-001 pending correction before approval

---

## Table of Contents

- [1.1 What This Specification Covers](#11-what-this-specification-covers)
- [1.2 What Is Out of Scope](#12-what-is-out-of-scope)
  - [1.2.1 Responsibilities Owned by Other Specifications](#121-responsibilities-owned-by-other-specifications)
  - [1.2.2 Features Deferred to Stage 1+](#122-features-deferred-to-stage-1)
  - [1.2.3 Permanent Exclusions](#123-permanent-exclusions)
- [1.3 Key Design Decisions](#13-key-design-decisions)
- [1.4 Relationship to Adjacent Systems](#14-relationship-to-adjacent-systems)
  - [1.4.1 System Context](#141-system-context)
  - [1.4.2 What Pass Mechanics Is Not Responsible For](#142-what-pass-mechanics-is-not-responsible-for)
- [1.5 Dependencies and Integration Contracts](#15-dependencies-and-integration-contracts)
  - [1.5.1 Hard Dependencies — Must Be Stable Before Section 3](#151-hard-dependencies--must-be-stable-before-section-3)
  - [1.5.2 Soft Dependencies — Forward References to Unwritten Specifications](#152-soft-dependencies--forward-references-to-unwritten-specifications)
- [1.6 Version History](#16-version-history)

---

## 1.1 What This Specification Covers

Pass Mechanics Specification #5 governs the translation of a pass intent — expressed as a
`PassRequest` from the Decision Tree — into a fully-specified ball state via a single call
to `Ball.ApplyKick()`. This specification owns everything that happens between the moment
a passing decision is made and the moment the ball leaves the agent's foot. Nothing before
(decision-making) and nothing after (ball flight physics, reception quality) is within scope.

The core model is:

```
PassResult = PassType × (Velocity, Spin, LaunchAngle) × ErrorVector
```

Where `PassType` determines the physical profile, `Velocity`/`Spin`/`LaunchAngle` are
derived from agent attributes and intent, and `ErrorVector` applies deterministic
inaccuracy from attributes, pressure, fatigue, and orientation.

This specification covers the following responsibilities:

- **Pass Type Classification** — Recognition and parameterisation of all seven supported
  pass types: Ground, Driven, Lofted, Through Ball (Ground), Through Ball (Aerial), Cross,
  and Chip/Lobbed. Each type carries a distinct physical profile — velocity range, launch
  angle range, and dominant spin signature.

- **Ball Velocity Calculation** — Derivation of launch velocity from pass type, intended
  distance, the executing agent's `KickPower` attribute `[ERR-007-PENDING]`, and a fatigue
  modifier. Velocity is clamped to per-type min/max bounds documented in Section 3.2.

- **Launch Angle Derivation** — Calculation of the vertical launch angle in degrees,
  determined by pass type and distance. Ground passes use near-zero elevation (2°–5°);
  lofted passes reach 20°–45°; chips may reach 45°–65°. Full derivation in Appendix A.2.

- **Spin Vector Calculation** — Computation of the three-component spin vector (topspin,
  backspin, sidespin) passed to `Ball.ApplyKick()`. Spin magnitude scales with the
  `Technique` attribute. Sidespin on curling crosses is governed by a separate sub-model
  within Section 3.4.

- **Deterministic Error Model** — Application of an angular deviation to the pass
  direction, derived from agent `Passing` attribute, pressure from nearby opponents,
  fatigue level, and body orientation relative to target. Given identical inputs, the
  error vector is always identical — no random elements.

- **Target Resolution** — Resolution of the pass target: either a specific agent
  (player-targeted) or a position in space (space-targeted). For space-targeted passes,
  the nearest team-mate whose projected run intersects the targeted zone is selected.

- **Through Ball Lead Distance** — Calculation of how far ahead of the receiver's current
  position the ball should be directed. Uses linear receiver projection for Stage 0.
  Non-linear prediction deferred to Stage 1. Derivation in Appendix A.3.

- **Weak Foot Penalty Model** — Application of accuracy and power penalties when the
  executing agent uses their non-dominant foot. Penalty magnitude is governed by
  `WeakFootRating` `[ERR-007-PENDING]` (1–5 scale). The `IsWeakFoot` flag is set by the
  Decision Tree; Pass Mechanics does not reason about foot preference.

- **Pass Execution State Machine** — Management of the six-state execution lifecycle:
  `IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE`. Cancellation via
  tackle interrupt is handled during `WINDUP` state.

- **`Ball.ApplyKick()` Interface Call** — The single output operation of this system.
  Confirmed signature: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin,
  int agentId, float matchTime)`. Called exactly once per pass at `CONTACT` state.
  No `KickType` parameter — pass type is fully encoded in the velocity and spin vectors
  (resolution of ERR-005).

- **Event Publishing** — Publication of `PassAttemptEvent` at `CONTACT` state and
  `PassCancelledEvent` on tackle interrupt. `PassCompletedEvent` and
  `PassInterceptedEvent` are published downstream by First Touch and Ball Physics
  respectively — not by this system.

- **Performance Budget Compliance** — All operations execute as a discrete event
  (not per-frame). Target: complete execution within 0.05 ms on a reference CPU at
  200 agents on-pitch. Budget analysis in Section 6.

---

## 1.2 What Is Out of Scope

### 1.2.1 Responsibilities Owned by Other Specifications

The following responsibilities are explicitly excluded. Each is owned by a named
specification with a defined interface at the boundary.

| Responsibility | Owning Specification | Boundary Interface |
|---|---|---|
| Decision to pass; selection of pass type, target, and urgency | Decision Tree Spec #8 | Decision Tree constructs and submits `PassRequest` to Pass Mechanics |
| Ball trajectory physics after kick — aerodynamics, Magnus effect, bounce, roll | Ball Physics Spec #1 | Pass Mechanics calls `Ball.ApplyKick()`; Ball Physics simulates from that point forward |
| Reception quality — first touch, control, bobble, possession transfer | First Touch Spec #4 | First Touch observes ball state and executing agent ID; no direct call from Pass Mechanics |
| Collision detection during pass execution — tackle interrupt | Collision System Spec #3 | Collision System notifies Pass Mechanics via state flag during `WINDUP`; interface defined jointly in Section 4.4 |
| Agent animation during pass execution | Animation System (Stage 1) | `PassAnimationData` struct is populated but unconsumed in Stage 0 |
| Goalkeeper distribution — goal kicks, punts, throws | Goalkeeper Mechanics Spec #11 | Distribution actions do not enter the Pass Mechanics state machine |
| Heading passes and flick-ons | Heading Mechanics Spec #10 | All ball contacts above 0.5 m from ground are Heading Mechanics territory |
| Shot mechanics — intent, trajectory targeting, outcome evaluation | Shot Mechanics Spec #6 | Shots differ in power profile, trajectory intent, and post-contact outcome logic |
| Team passing style instructions — short, direct, counter | Formation & Instructions System (Stage 1) | Modifiers applied on top of Pass Mechanics base outputs in Stage 1 |

### 1.2.2 Features Deferred to Stage 1+

The following features are within the long-term scope of Pass Mechanics but are explicitly
deferred from Stage 0. Each deferral is documented here to prevent scope creep during
Stage 0 implementation and to establish clear upgrade hooks in Section 7.

| Feature | Deferral Reason | Target Stage |
|---|---|---|
| Body part selection — instep, outside, laces, heel | Stage 0 uses simplified single foot contact; body part adds per-contact error and spin modifiers | Stage 1 |
| Animation-driven pass timing — windup duration from animation frame data | Requires Animation System integration; Stage 0 uses fixed timing constants | Stage 1 |
| Team instruction modifiers — short, direct, counter-press passing styles | Requires Formation & Instructions System; Stage 0 uses neutral base model | Stage 1 |
| Per-player pass accuracy statistics — key passes, completion rate, error distribution | Requires Statistics Engine; Stage 0 publishes raw events only | Stage 1 |
| Non-linear through ball prediction — receiver re-routing, press evasion trajectories | Stage 0 uses linear receiver projection; non-linear requires Stage 1 movement prediction | Stage 1 |
| Surface condition effects on ball roll after landing | Requires Pitch Condition System; wet/heavy pitch effects are Ball Physics and First Touch concerns | Stage 2 |
| Curling free kicks with set-piece targeting | Set Piece System not yet designed; Pass Mechanics handles open-play kicks only | Stage 2 |
| Player-specific passing tendencies — positional preference, passing range preference | Requires extended player profile system beyond Stage 0 attribute model | Stage 2 |
| Fixed64 determinism — replacement of float arithmetic with Fixed64 types | Float arithmetic sufficient for Stage 0; full Fixed64 migration coordinated across all specs at Stage 5 | Stage 5 |

### 1.2.3 Permanent Exclusions

The following design approaches are permanently excluded — they will not appear in any
stage of Pass Mechanics regardless of future feature scope.

| Excluded Approach | Exclusion Rationale |
|---|---|
| Random dice rolls for pass error (e.g., Gaussian noise on inaccuracy) | Pass error is deterministic from physics inputs. Identical inputs must always produce identical outputs. Hard requirement for replay determinism (Master Vol 1 §1.3). |
| "Scripted" forced pass failures — a pass fails because the game decided it should | All outcomes emerge from the attribute and physics model. No external forcing function exists or will exist. |
| Ball-magnet / auto-completion — ball curves toward receiver on good passes | Physics governs. Pass Mechanics applies a velocity and spin vector; it does not adjust the trajectory mid-flight. |
| Tactical AI reasoning inside Pass Mechanics — e.g., checking if a pass is safe before executing | Pass Mechanics executes. Decision Tree (#8) reasons. This boundary is absolute and must not erode across any future stage. |
| Invisible inaccuracy floor — a cap that prevents world-class passers from ever failing | Attributes can produce very low error angles but not zero. Perfect passes emerge from extreme attribute values, not a cap. The model must remain physically grounded. |

---

## 1.3 Key Design Decisions

The following decisions are architectural commitments. Changing any of them requires a
formal amendment with full cross-spec impact assessment. They are recorded here because
understanding them is prerequisite to reading Section 3 correctly.

---

**KD-1 — Pass error is deterministic.**

Given identical inputs — agent attributes, pressure level, fatigue, body orientation,
target position, and frame number — the error vector is always identical. No
`System.Random`, no noise seeding, no platform-dependent values. This is mandatory
for replay determinism. See: Master Vol 1 §1.3.

---

**KD-2 — Pass type is set by the caller (Decision Tree), not inferred by Pass Mechanics.**

Pass Mechanics receives a `PassRequest` containing the pass type as an explicit enum
value. Pass Mechanics does not examine the context and decide "this should be a driven
pass." That reasoning belongs to Decision Tree (#8). This boundary is enforced by the
`PassRequest` struct contract defined in Section 3.1.

---

**KD-3 — `Ball.ApplyKick()` carries no `KickType` parameter.**

The pass type is fully encoded in the velocity and spin vectors passed to `ApplyKick()`.
Ball Physics does not need to know the caller's intent label to simulate correct
aerodynamics. Pass Mechanics' entire job is to map `PassType` to physical parameters.
Resolution of ERR-005 (Spec Error Log v1.0).

---

**KD-4 — Through ball targeting uses linear receiver projection in Stage 0.**

Lead distance is calculated by projecting the receiver's current velocity vector forward.
If the receiver changes direction after the pass is initiated, the ball will not adjust —
this is the correct failure mode, not a bug. It models the consequence of a pass that
misreads receiver movement. Non-linear prediction is a Stage 1 upgrade point (§7.1).

---

**KD-5 — `IsWeakFoot` flag is set by the Decision Tree, not computed by Pass Mechanics.**

Pass Mechanics applies the weak foot penalty when `IsWeakFoot = true` on the
`PassRequest`. Pass Mechanics does not inspect agent preferred foot data. The Decision
Tree is responsible for determining which foot the agent is using. Resolution of OQ-4
(Outline v1.0).

---

**KD-6 — Cross sub-type is an optional enum field on `PassRequest`, defaulting to Flat.**

Three cross types — Flat, Whipped, High — have genuinely distinct physical profiles
(velocity, launch angle, spin). A scalar `CrossCurlAmount` parameter was considered but
rejected: the discrete enum approach is more auditable, easier to test, and eliminates
the risk of continuous-parameter exploits. Resolution of OQ-1 (Outline v1.0).

---

**KD-7 — `Vision` attribute is proxied by `Technique` for Stage 0 through ball accuracy.**

`Vision` (anticipatory accuracy) is not present in Stage 0 `PlayerAttributes`. Through
ball lead distance accuracy uses `Technique` as a proxy — a reasonable approximation
given that technical quality correlates with passing vision at Stage 0 scale. This is
explicitly a Stage 1 upgrade point. Values derived using this proxy are marked
`[GAMEPLAY-TUNABLE]` in Section 3. Resolution of OQ-2 (Outline v1.0).

---

**KD-8 — `PassCancelledEvent` is published only on tackle interrupt, not on invalid request rejection.**

An invalid `PassRequest` is rejected silently with a logged error — it is a programming
error, not a game event. Only a tackle interrupt during `WINDUP` produces a
`PassCancelledEvent` that downstream systems need to observe. Resolution of OQ-5
(Outline v1.0).

---

## 1.4 Relationship to Adjacent Systems

Pass Mechanics occupies a narrow but critical position in the Stage 0 system graph. It
is a consumer of three upstream specifications and a producer for one downstream
specification. The boundaries below are the authoritative statement of this system's
interfaces.

### 1.4.1 System Context

| System | Direction | What Passes Across the Boundary |
|---|---|---|
| Decision Tree Spec #8 (upstream caller) | → into Pass Mechanics | `PassRequest` struct: `PassType`, `CrossSubType`, `TargetType` (agent or space), `TargetAgentId` or `TargetPosition`, `IntendedDistance`, `IsWeakFoot`, `UrgencyLevel`, `Frame` |
| Agent Movement Spec #2 (upstream data provider) | → read by Pass Mechanics | `PlayerAttributes` (`Passing`, `Technique`, `KickPower` [ERR-007], `WeakFootRating` [ERR-007], `Crossing` [ERR-007]) and `AgentState` (`Position`, `Velocity`, `FacingDirection`, `Fatigue`) |
| Collision System Spec #3 (upstream interrupt source) | → interrupt signal into Pass Mechanics | Tackle interrupt notification during `WINDUP` state. Interface form (callback vs state flag) defined jointly in Section 4.4 |
| Ball Physics Spec #1 (downstream target) | ← called by Pass Mechanics | `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)`. Called exactly once per pass at `CONTACT` state. Amendment AM-001-001 `[ERR-008-PENDING]` |
| First Touch Spec #4 (downstream, no direct call) | implicit — via ball state | Pass Mechanics does not call First Touch. First Touch observes `BallState` changes written by Ball Physics after `ApplyKick()` is called |
| Event System Spec #17 (downstream consumer, Stage 1) | ← events published by Pass Mechanics | `PassAttemptEvent` (at `CONTACT`), `PassCancelledEvent` (on tackle interrupt). Stage 0 stub publishes to a no-op receiver |
| Animation System (downstream consumer, Stage 1) | ← struct populated by Pass Mechanics | `PassAnimationData` struct populated at each state transition in Stage 0; consumer does not yet exist |

### 1.4.2 What Pass Mechanics Is Not Responsible For

A common source of boundary confusion is whether a particular concern belongs to Pass
Mechanics or to Decision Tree. The rule is simple: **Pass Mechanics executes, Decision
Tree reasons.** If a calculation requires knowing whether a pass is a good idea, it
belongs to Decision Tree. If it requires knowing how to translate a pass intent into
ball movement, it belongs here.

Pass Mechanics does not: evaluate whether the target is free; check for opponents in
the passing lane beyond computing pressure-based error; evaluate pass risk; consider
team tactical shape; or choose between pass types. All of those decisions have already
been made before `PassRequest` reaches this system.

A second confusion point concerns Ball Physics. Pass Mechanics calls `ApplyKick()` and
its responsibility ends. It does not track the ball after that call, does not evaluate
whether the ball reached the target, and does not publish `PassCompletedEvent` —
that event is owned by First Touch after reception occurs.

---

## 1.5 Dependencies and Integration Contracts

### 1.5.1 Hard Dependencies — Must Be Stable Before Section 3

The following items must be approved or formally accepted as known risks before
Section 3 (Technical Specifications) can be finalised.

| Dependency | Spec | What Is Needed | Status |
|---|---|---|---|
| `Ball.ApplyKick()` method definition | Ball Physics #1 | Confirmed signature: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)` — no `KickType` | DRAFT — AM-001-001 pending ERR-008 resolution ⚠ |
| `BallState` — `CONTROLLED` state exit logic | Ball Physics #1 | When `ApplyKick()` is called, ball transitions from `CONTROLLED` to `AIRBORNE` or `ROLLING`. Design decision required: does `BallState` carry `PossessingAgentId` (Option A) or is possession tracking external (Option B)? | OPEN — ERR-008 unresolved ⚠ |
| `PlayerAttributes.KickPower` (1–20) | Agent Movement #2 | Required for velocity calculation in Section 3.2. Amendment AM-002-001 adds this field. | DRAFT — AM-002-001 pending approval ⚠ |
| `PlayerAttributes.WeakFootRating` (1–5) | Agent Movement #2 | Required for weak foot penalty model in Section 3.6. Amendment AM-002-001 adds this field. | DRAFT — AM-002-001 pending approval ⚠ |
| `PlayerAttributes.Crossing` (1–20) | Agent Movement #2 | Required for cross accuracy model in Section 3.3. Amendment AM-002-001 adds this field. | DRAFT — AM-002-001 pending approval ⚠ |
| `AgentState` — `Position`, `Velocity`, `FacingDirection`, `Fatigue` | Agent Movement #2 | All fields exist in current §3.5.3. No amendment required. | STABLE ✓ |
| Collision System tackle interrupt notification mechanism | Collision System #3 | Interface form (callback vs polling flag) TBD. Section 4.4 defines jointly. | DEFERRED TO §4.4 |

> ⚠ **[ERR-007]** `KickPower`, `WeakFootRating`, and `Crossing` are not yet approved
> in `PlayerAttributes`. Amendment AM-002-001 is drafted but awaiting lead developer
> approval. Section 3 cannot be finalised until these attributes are confirmed.

> ⚠ **[ERR-008]** The design decision on `BallState.PossessingAgentId` (Option A: field
> on `BallState`; Option B: external tracking) must be resolved before amendment
> AM-001-001 can be approved. Does not block Section 1 or Section 2, but blocks Section 3.

### 1.5.2 Soft Dependencies — Forward References to Unwritten Specifications

The following specifications are referenced as consumers of Pass Mechanics outputs or
as future modifiers. Not required for Section 3 but must be considered when designing
extension hooks in Section 7.

| Specification | Relationship | Impact on This Spec |
|---|---|---|
| Decision Tree Spec #8 | Sole caller — submits `PassRequest` | `PassRequest` struct must be designed in Section 3.1 before Decision Tree is written. Decision Tree implements this contract. |
| Statistics Engine (Stage 1) | Consumer of `PassAttemptEvent` and `PassCompletedEvent` | Event struct fields must be extensible. No additional fields required at Stage 0. |
| Formation & Instructions System (Stage 1) | Provides team instruction modifiers | Section 7.2 defines the modifier hook. No implementation in Stage 0. |
| Heading Mechanics Spec #10 | Shares boundary at 0.5 m contact height | Ball contacts above 0.5 m do not enter Pass Mechanics state machine. Boundary enforced by caller. |
| Goalkeeper Mechanics Spec #11 | Shares distribution boundary | Goal kicks and punts use a separate execution path. `IsGoalkeeper` flag on `PassRequest` reserved for Stage 1 routing. |

---

## 1.6 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 20, 2026 | Claude (AI) / Anton | Initial draft. All subsections complete. ERR-007 and ERR-008 flagged as pending. |
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 5 instances); Spec Error Log version updated v1.0→v1.4 (Min-01). |

---

## Section 1 Summary

This section establishes the complete scope boundary for Pass Mechanics Specification #5.
Pass Mechanics translates a `PassRequest` into ball state via `Ball.ApplyKick()`. It owns
velocity calculation, launch angle derivation, spin vector calculation, error modelling,
target resolution, weak foot penalties, state machine execution, and event publishing. It
does not own the decision to pass, ball trajectory physics, reception quality, or tactical
reasoning.

Two dependency flags remain open and block Section 3:

- **ERR-007** — `PlayerAttributes` missing `KickPower`, `WeakFootRating`, `Crossing`
  (Amendment AM-002-001 drafted, pending approval)
- **ERR-008** — `BallState` design decision on `PossessingAgentId` unresolved
  (blocks AM-001-001 approval)

Neither flag blocks Section 2.

**Next:** Section 2 — System Overview and Functional Requirements.

---

*End of Section 1 — Pass Mechanics Specification #5*