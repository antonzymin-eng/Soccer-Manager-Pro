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

Method signatures consumed by #10. Upstream subsection anchors are
pinned where verified; unpinned anchors are marked TBD per OI-005.

| Signature | Owning spec | Anchor |
|-----------|-------------|--------|
| `BallPhysics.GetBallState(matchTime) → BallState` | Spec #1 | Subsection anchor TBD; pinned during pass-2 review (OI-005). |
| `Agent` instance (XY kinematic state, `facing`, attribute exposure) — passed by reference from simulation scheduler. | Spec #2 | §3.5.1 (`section-3-5-part-1.md` lines 112–610). No `GetKinematicState(agentId, frame)` getter is cited because AM #2 publishes per-agent state via the `Agent` instance, not a registry getter. |
| `PlayerAttributes` struct field reads (`Heading`, `Strength`, `Balance`). | Spec #2 | §3.5.6 (`section-3-5-part-2.md` line 230 onward; struct declared line 259). Field reads are unqualified struct access, not getter calls. |
| `AgentMovementState` enum + `GroundedReason.DIVING_HEADER` enum value (aerial-phase exit and ground re-entry). | Spec #2 | §3.1.2 (`section-3-1-part-2.md` lines 23–105). No `Jumping` state exists; Stage 0 aerial phase is owned by #10 per KD-18 and is invisible to the AM #2 state machine. |
| `CollisionSystem.GetContactEventsAtFrame(frame) → ReadOnlySpan<ContactEvent>` | Spec #3 | Subsection anchor TBD; pinned during pass-2 review (OI-005). |
| `DecisionTree.GetHeaderIntent(agentId, tick) → HeaderIntent?` | Spec #8 | §1.7.x; subsection anchor TBD; pinned during pass-2 review (OI-005). |
| `DeterministicRng.NextFloat(drawSiteId) → float` | Spec #16 | §4.1. |
| `DeterministicRng.NextGaussian(drawSiteId) → float` | Spec #16 | §4.1 / §4.5. |

---

## 4.3 Output Interface Contracts

Method signatures emitted by #10.

| Signature | Owning spec | Anchor |
|-----------|-------------|--------|
| `Ball.ApplyKick(velocity, spin, agentId, matchTime)` | Spec #1 | §3.1.11.2. Invoked only on successful header contact (NOT on failed attempts per FR-HE-006 / KD-12). |
| `EventBus.Publish<HeaderExecutedEvent>(evt)` | Spec #17 | Event publish API; subsection anchor TBD; pinned during pass-2 review (OI-005). |
| `EventBus.Publish<HeaderAttemptFailedEvent>(evt)` | Spec #17 | Same anchor. |

Trace channels (§2.4) are emitted via the Performance Optimization
#18 §3.10 trace pipeline.

---

## 4.4 Determinism Compliance Surface

This subsection enumerates every #10 → #16 touchpoint and is the
authoritative source for §9.1 verification.

### Domain Tag Allocation

| Tag | Value | Status | Back-prop |
|-----|-------|--------|-----------|
| `DOMAIN_TAG_HEADING` | `0x16` | `[CROSS-PENDING]` | ERR-010-001 against #16 §3.4. Pure namespace amendment (no `DETERMINISM_DIGEST_VERSION` bump) — precedent: #17 `DOMAIN_TAG_EVENT_LEDGER = 0x15` patch (May 14, 2026). Promoted to `[CROSS]` atomically with #16 §3.4 patch landing. |

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

### 60 Hz Physics Loop

```
Per 60 Hz physics tick:
  for each agent in #16 §3.2 entity order:
    if agent has a HeaderIntent latched and aerial-phase active:
      (eligible, predictedContactFrame, idealContactFrame)
          = EligibilityPredicate(agent, ball, intent, currentFrame)
      if not eligible:
        continue
      update synthetic Z trajectory (§3.3)
      if currentFrame == predictedContactFrame:
        accumulate into ContestedDuelContext if multi-agent
  for each ContestedDuelContext:
    DuelResolution(context)           // §3.7
    Winner: contactQualityScalar → §3.5 outgoingSpeed, §3.6 outgoingSpin
            → Ball.ApplyKick(...)
            → publish HeaderExecutedEvent
    Losers: emit HeaderAttemptFailedEvent (DisturbedInDuel)
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
