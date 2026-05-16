# Heading Mechanics Specification #10 — Section 4: Architecture, File Layout, Interface Contracts

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Define the file layout under `src/Gameplay/Heading/`, the
input and output interface contracts (consumed and emitted method
signatures), the determinism compliance surface (`DOMAIN_TAG`,
draw-site IDs, iteration order), the performance compliance surface
(per-tick budgets, hot-path allocation discipline), and the
tick-scheduling surface.

---

## 4.1 File Layout

All files live under `src/Gameplay/Heading/`. One source file per
algorithmic concern, mirrored by one test file under
`tests/Gameplay/Heading/` (Spec #19 §3.x convention).

| File | Purpose | Owning § |
|------|---------|----------|
| `HeadingMechanics.cs` | Orchestrator; consumed by simulation scheduler at 60 Hz physics tick. | §3.2–§3.9 dispatch |
| `HeadingConstants.cs` | Every constant from §3.1 with its source tag comment. No magic numbers in formula files (KD-11 / FR-HE-014). | §3.1 |
| `HeadingEligibility.cs` | Eligibility predicate. | §3.2 |
| `HeadingJumpKinematics.cs` | `JumpReach` formula + Stage 0 synthetic Z trajectory. | §3.3 |
| `HeadingContactQuality.cs` | Contact-quality scalar computation. | §3.4 |
| `HeadingPowerAngle.cs` | Power and launch-angle generation. | §3.5 |
| `HeadingSpinTransfer.cs` | `headAngularVelocity` derivation and spin transfer. | §3.6 |
| `HeadingDuelResolution.cs` | Contested-duel resolution. | §3.7 |
| `HeadingTelemetry.cs` | Counter / histogram emission per §2.4. | §2.4 |

Test layout: `tests/Gameplay/Heading/<SourceFile>Tests.cs` for each
source file above, plus integration tests under
`tests/Integration/Heading/` (§5.2).

---

## 4.2 Input Interface Contracts

Method signatures consumed by #10. All upstream subsection anchors
pinned May 16, 2026 (v0.3 OI-005 closure).

| Signature | Owning spec | Anchor |
|-----------|-------------|--------|
| `BallPhysics.GetBallState(matchTime) → BallState` | Spec #1 | §3.1.11.2 (`section-3.md` ball-state query API; consumed read-only by §3.2 / §3.4 / §3.8). v0.3 OI-005 closure: anchored against the same Ball Physics surface §4.3 cites for `Ball.ApplyKick` output. |
| `Agent` instance (XY kinematic state, `facing`, attribute exposure) — passed by reference from simulation scheduler. | Spec #2 | §3.5.1 (`section-3-5-part-1.md` lines 112–610). No `GetKinematicState(agentId, frame)` getter is cited because AM #2 publishes per-agent state via the `Agent` instance, not a registry getter. |
| `PlayerAttributes` struct field reads (`Heading`, `Strength`, `Balance`). | Spec #2 | §3.5.6 (`section-3-5-part-2.md` line 230 onward; struct declared line 259). Field reads are unqualified struct access, not getter calls. |
| `AgentMovementState` enum + `GroundedReason.DIVING_HEADER` enum value (aerial-phase exit and ground re-entry). | Spec #2 | §3.1.2 (`section-3-1-part-2.md` lines 23–105). No `Jumping` state exists; Stage 0 aerial phase is owned by #10 per KD-18 and is invisible to the AM #2 state machine. |
| `ICollisionEventConsumer.OnCollisionEvent(CollisionEvent evt)` — #10 implements the consumer interface and buffers events per-frame keyed by `MatchTime`. | Spec #3 | §3.4.2 (`section-3-4.md` lines 387–445; `ICollisionEventConsumer` interface + `CollisionEvent` struct). v0.3 OI-005 closure: anchored to actual #3 push-API surface. Earlier v0.1 / v0.2 pull-API framing (`GetContactEventsAtFrame`) is replaced — #3 has no such getter; the consumer pattern is the only as-published surface (KD-8 / FR-HE-010, no #3 redefinition). See §4.2.1 below for the per-frame buffer mechanic. |
| `HeaderIntent` (Decision Tree #8) — Stage 0+1 activation. | Spec #8 | §1.7.2 row "Heading Mechanics #10 (Stage 1) — HEADER action type and dispatch interface — Not defined at Stage 0; stub placeholder in §7." v0.3 OI-005 closure: anchored to #8's existing Stage 0 deferral row. #10's `HeaderIntent` struct (§2.2) is declared at Stage 0 as a forward contract; the DT-side dispatch wiring lands at Stage 0+1 atomically with #8's §7 stub promotion. See §4.6.1 below for the Stage 0+1 activation framing (mirrors the OI-002 channel-registry pattern). |
| `DeterministicRng.NextFloat(drawSiteId) → float` | Spec #16 | §4.1. |
| `DeterministicRng.NextGaussian(drawSiteId) → float` | Spec #16 | §4.1 / §4.5. |

### 4.2.1 Collision-Event Consumer Buffer (v0.3 OI-005 closure)

Per #3 §3.4.2, `CollisionEvent` records are pushed via the
`ICollisionEventConsumer.OnCollisionEvent(evt)` interface; there is
no pull-API getter on `CollisionSystem`. #10's
`HeadingDuelResolution.cs` implements the consumer interface and
maintains a per-frame `List<CollisionEvent>` buffer (struct-backed,
pre-sized to the §6.3 worst-case header-frame contact count). On
the 60 Hz physics tick:

```
// #10 implements ICollisionEventConsumer
public void OnCollisionEvent(CollisionEvent evt):
    if evt.Type == CollisionType.AGENT_BALL and
       evt.MatchTime falls within current physics-frame window:
        currentFrameContactBuffer.Add(evt)

// §3.7 contested-duel resolution reads from the per-frame buffer:
contactEvents = currentFrameContactBuffer
                 .Where(evt => evt.MatchTime == contactFrameMatchTime)
                 .ToReadOnlySpan()       // span over backing array;
                                         // no allocation
// then proceeds per §3.7 algorithm
```

The buffer is cleared at the start of each physics frame. Zero
heap allocation per #18 §3.7.3 (the buffer's backing array is
allocated once at `HeadingMechanics.Initialize()`, sized to
`HEADING_CONTACT_BUFFER_CAPACITY [GT]`; `ToReadOnlySpan()` returns
a view over the existing buffer).

---

## 4.3 Output Interface Contracts

Method signatures emitted by #10.

| Signature | Owning spec | Anchor |
|-----------|-------------|--------|
| `Ball.ApplyKick(velocity, spin, agentId, matchTime)` | Spec #1 | §3.1.11.2. Invoked only on successful header contact (NOT on failed attempts per FR-HE-006 / KD-12). |
| `EventBus.Publish<HeaderExecutedEvent>(in evt)` | Spec #17 | §3.2.1 `Publish API surface` (`section-3.md` lines 104–127). v0.3 OI-005 closure: anchored to the typed `Publish<T>` overload set. `HeaderExecutedEvent` implements `IEventB` (Tier B — included in determinism digest per #17 KD-6, since outgoing-velocity / outgoing-spin influence subsequent `Ball.ApplyKick` state). |
| `EventBus.Publish<HeaderAttemptFailedEvent>(in evt)` | Spec #17 | §3.2.1 same anchor. `HeaderAttemptFailedEvent` implements `IEventC` (Tier C — no ball-state modification per FR-HE-006 / KD-12; telemetry-only payload, excluded from determinism digest per #17 KD-3 / FR-EVT-014). |

Trace channels (§2.4) are emitted via the Performance Optimization
#18 §3.10 trace pipeline.

---

## 4.4 Determinism Compliance Surface

This subsection enumerates every #10 → #16 touchpoint and is the
authoritative source for §9.1 verification.

### Domain Tag Allocation

| Tag | Value | Status | Back-prop |
|-----|-------|--------|-----------|
| `DOMAIN_TAG_HEADING` | `0x16` | `[CROSS]` | ERR-010-001 RESOLVED May 16, 2026 via #16 §3.5 v1.0.2 patch (pure namespace amendment, no `DETERMINISM_DIGEST_VERSION` bump; followed #17 `DOMAIN_TAG_EVENT_LEDGER = 0x15` precedent). |

### Registered Draw Sites (pass-1 M-4 closure)

All three are wired to call sites in §3. No phantom draw sites.

| Draw Site ID | Call Site | Purpose |
|--------------|-----------|---------|
| `DRAW_SITE_DUEL_TIEBREAK` | §3.7 step 3 | Near-tie perturbation in contested-duel resolution. RNG type: `NextFloat`. |
| `DRAW_SITE_CONTACT_POINT_ERROR` | §3.4 `pointNoiseM` Gaussian | Per-attempt point-error noise. RNG type: `NextGaussian`. |
| `DRAW_SITE_TIMING_JITTER` | §3.4 `timingJitterMs` Gaussian | Per-attempt timing-noise. RNG type: `NextGaussian`. |

### Entity-Iteration Order

Contested-duel participants in §3.7 are iterated in #16 §3.2
entity-ordering (FR-HE-017). Iteration order is invariant under
contact-event arrival order in the #3 contact-event list.

---

## 4.5 Performance Compliance Surface

Pre-commitments referenced from Performance Optimization #18 §6
under #18's ratify-not-override authority (KD-2 of #18).

### Budget Framing (pass-1 H-4 reconciliation, FR-HE-035)

The 80 µs steady-state ceiling does not bind at duel frames; the
tail budget binds instead.

| Budget | Value | Tag | Scope |
|--------|-------|-----|-------|
| Hot-path heap allocation | 0 bytes/tick | `[FIXED]` | All ticks (#18 §3.10). |
| Steady-state per-tick cost | ≤80 µs | `[EST]` | 22-agent match peak, non-duel-frame load. Carried into #18 §6 as the steady-state row. |
| p99 duel-frame tail cost | ≤180 µs | `[EST]` | Duel-resolution frame (3-way duel with near-tie tiebreak perturbation). Carried into #18 §6 as a separate p99 spike row. Justified by §6.3 component breakdown. |

Both `[EST]` budgets are not credible until `certification-platform.md`
Stage-0 host pin lands (CLAUDE.md OPEN ISSUES). `FR-PO-052` Stage 0+1
perf-gate activation is gated on that pin and not on #10 sign-off
(OI-006).

### Hot-Path Discipline

- No `new` in formula files (`HeadingContactQuality.cs`,
  `HeadingPowerAngle.cs`, `HeadingSpinTransfer.cs`,
  `HeadingDuelResolution.cs`).
- `ReadOnlySpan<ContactEvent>` for contact-event consumption.
- Struct return types for `HeaderIntent`, `HeaderContactState`,
  `HeaderExecutedEvent`, `HeaderAttemptFailedEvent`,
  `ContestedDuelContext`.
- No `HotPathAllocExempt` attribute uses required (struct-based
  data flow throughout).

---

## 4.6 Tick-Scheduling Surface

Two loops, per CLAUDE.md / KD-9 / FR-HE-013.

### 10 Hz Tactical Loop

Decision Tree #8 produces `HeaderIntent` at the 10 Hz tactical tick.
The intent is consumed by #10's eligibility predicate (§3.2) on the
next 60 Hz physics tick. `targetIntent`, `powerIntent`, and
`contactPointIntent` are locked at commit (KD-17 (a), (c)).

### 4.6.1 DT-Side Activation Schedule (v0.3 OI-005 closure)

DT #8 §1.7.2 explicitly defers the HEADER action-type and dispatch
interface to Stage 0+1: *"Heading Mechanics #10 (Stage 1) — HEADER
action type and dispatch interface — Not defined at Stage 0; stub
placeholder in §7."* The Stage 0 / Stage 0+1 split for #10's
upstream DT dependency is therefore:

| Stage | DT-side state | #10-side state |
|-------|---------------|----------------|
| Stage 0 (this spec) | #8 §7 stub placeholder; no live `HeaderIntent` flow | `HeaderIntent` struct declared (§2.2); physics layer (§3.2…§3.9) complete; consumer interface in §4.6 ready to receive intent when DT wiring activates |
| Stage 0+1 | #8 §7 stub promotes to live HEADER action-type producer | #10 receives `HeaderIntent` at 10 Hz; §3.2 eligibility predicate activates the live path |

This split mirrors the pattern used for the OI-002 channel registry
(populated subsystem-channel rows are Stage 0+1 per #18 Appendix
F.0 / §7.2) and for the KD-18 jump-kinematics ownership (Stage 0
synthetic; retires to AM #2 native Z at Stage 1+ per §7.8). #10's
Stage 0 deliverable is the spec text and the forward contracts;
the live `DecisionTree → #10` wiring is the natural Stage 0+1
deliverable atomic with #8 §7 stub promotion. No #8 amendment is
required at Stage 0 (CLAUDE.md "Interface Design Principle":
both sides specified at Stage 0+1, not at Stage 0).

### 60 Hz Physics Loop

```
Per 60 Hz physics tick:
  for each agent in #16 §3.2 entity order:
    // jumpStartFrame initialization (v0.2 M-3): set on the
    // first frame at or after attemptCommittedTick·6 where
    // movementState ∉ {GROUNDED, STUMBLING}.
    if HeaderIntent latched and contactState.jumpStartFrame is unset:
      if currentFrame >= intent.attemptCommittedTick · 6 and
         agent.movementState not in { GROUNDED, STUMBLING }:
        contactState.jumpStartFrame = currentFrame

    if agent has a HeaderIntent latched and aerial-phase active:
      (eligible, predictedContactFrame, idealContactFrame,
       mistimedDirection)
          = EligibilityPredicate(agent, ball, intent, currentFrame)
      if not eligible:
        // v0.2 M-2: predicate is pure; caller emits the failed event.
        if mistimedDirection == Early:
          emitFailedAttempt(agent, MistimedEarly)
        else if mistimedDirection == Late:
          emitFailedAttempt(agent, MistimedLate)
        continue
      update synthetic Z trajectory (§3.3)
      if currentFrame == predictedContactFrame:
        contactState.actualContactFrame = currentFrame   // v0.2 M-4
        accumulate into ContestedDuelContext if multi-agent
  for each ContestedDuelContext:
    DuelResolution(context)           // §3.7
    Winner: contactQualityScalar → §3.5 outgoingSpeed, §3.6 outgoingSpin
            → Ball.ApplyKick(...)
            → publish HeaderExecutedEvent (full quality)
    Losers (v0.2 M-5 alignment, applies to 2-way and 3+ way uniformly):
            if q' >= MIN_CONTACT_QUALITY:
              publish disturbed HeaderExecutedEvent
            else:
              emit HeaderAttemptFailedEvent (DisturbedInDuel)
```

ASCII sequence diagram:

```
   T (10 Hz)           t (60 Hz, physics)
   ──────┐
         │ HeaderIntent committed
         ▼
         ─────┐
              │ EligibilityPredicate (re-evaluates each tick)
              │ ↓
              │ JumpKinematics (advance Z trajectory)
              │ ↓
              │ ContactQualityScalar (on contact frame)
              │ ↓
              │ DuelResolution (if contested)
              │ ↓
              │ Power & launch angle → outgoingSpeed
              │ ↓
              │ Spin transfer → outgoingSpin
              │ ↓
              │ Ball.ApplyKick(...)
              │ ↓
              │ Publish HeaderExecutedEvent
              │   or HeaderAttemptFailedEvent
              ▼
```

---

## 4.7 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | section authoring | Initial draft from `outline-detailed.md` v1.1. File layout, interface contracts, determinism + performance compliance surfaces, tick-scheduling enumerated. Upstream anchors pinned where verified; remaining anchors marked TBD per OI-005. | pending |
| 0.2 | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: §4.6 60 Hz pseudocode now (a) defines `jumpStartFrame` initialization (M-3), (b) emits mistimed-failed events at the caller after the pure predicate returns (M-2), (c) sets `actualContactFrame` on the contact-frame branch (M-4), (d) replaces "Losers: emit failed" with the uniform 2-way/3+ way M-5 loser semantics. | pending |
| 0.3 | May 16, 2026 | drafter | APPROVAL. §4.2 Input Interface Contracts re-anchored: #3 row moved from non-existent `GetContactEventsAtFrame` pull-API to actual `ICollisionEventConsumer` push-API (#3 §3.4.2); #8 row marked as Stage 0+1 activation per #8 §1.7.2; #1 / #16 / #17 anchors pinned. New §4.2.1 documents per-frame `ICollisionEventConsumer` buffer mechanic (zero-alloc via pre-sized `HEADING_CONTACT_BUFFER_CAPACITY`). §4.3 Output Interface Contracts: `HeaderExecutedEvent` tagged `IEventB` (Tier B), `HeaderAttemptFailedEvent` tagged `IEventC` (Tier C). New §4.6.1 DT-Side Activation Schedule formalizes Stage 0 / Stage 0+1 split mirroring KD-18 pattern. OI-005 RESOLVED. | granted |
