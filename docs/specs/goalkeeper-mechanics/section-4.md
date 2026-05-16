# Goalkeeper Mechanics Specification #11 — Section 4: Architecture, File Layout, Interface Contracts

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Establish the architectural file layout, the input /
output interface contracts, the determinism compliance surface,
the performance compliance surface, and the tick-scheduling
arrangement for Goalkeeper Mechanics #11.

---

## 4.1 File Layout (under `src/Gameplay/Goalkeeper/`)

| File | Owns | Section refs |
|------|------|--------------|
| `GoalkeeperMechanics.cs` | Orchestrator; scheduler hook at 10 Hz tactical + 60 Hz physics | §3.1 / §4.6 |
| `GoalkeeperConstants.cs` | Every constant from §3.4 with its source-tag comment (KD-9 / FR-GK-015 / FR-GK-042) | §3.4 |
| `GoalkeeperStateMachine.cs` | State enum + transition evaluator | §3.1 |
| `GoalkeeperReactionPipeline.cs` | `requiredReactionMs`, `reactionWindowAchieved`, dive-direction commit | §3.2 |
| `GoalkeeperDiveKinematics.cs` | Stage 0 synthetic XY+Z dive trajectory; KD-12 owner | §3.3 |
| `GoalkeeperPositioningContract.cs` | §3.3.0 consumer contract façade for #12 baseline read | §3.3.0 / KD-13 |
| `GoalkeeperHandlingQuality.cs` | Handling-quality scalar; band-to-action mapping | §3.5 |
| `GoalkeeperCrossClaimDuel.cs` | Body-part determination; routing; duel-score arithmetic | §3.6 / KD-14 |
| `GoalkeeperRushDispatch.cs` | Rush launch + per-frame update + abort | §3.7 / KD-15 |
| `GoalkeeperDistribution.cs` | Release-point geometry; `PassIntent` emission | §3.8 / KD-16 |
| `GoalkeeperTelemetry.cs` | §2.4 channel emission surface | §2.4 |

Test layout mirror under `tests/Gameplay/Goalkeeper/`, one file per
source file per Spec #19 §3 convention.

---

## 4.2 Input Interface Contracts (consumed)

| Surface | Spec | Section | Used in |
|---------|------|---------|---------|
| `BallPhysics.GetBallState(matchTime) → BallState` | #1 | §3.1 | §3.2 / §3.5 |
| `BallState.PossessorId` | #1 | §3.1 | §3.7 / F-08 rush abort |
| `Agent` instance access (XY kinematics + attributes) | #2 | §3.5.1 / §3.5.6 | §3.1 / §3.3 / §3.5 / §3.7 |
| `AgentMovementState`, `GroundedReason` enums | #2 | §3.1.2 | §3.1 / §3.3 |
| `CollisionSystem` event subscription | #3 | §3.4.2 `ICollisionEventConsumer` | §3.5 / §3.6 |
| `ShotExecutedEvent` subscription | #6 | §4.5 (via Event System #17 §3.2.1) | §3.2 |
| `Perception.GetVisibilityLatency(agentId, target) → float ms` | #7 | §3 visibility-cone latency surface (anchor pinned during implementation) | §3.2 |
| `DecisionTree.GetGKIntent(agentId, tick) → SaveIntent \| ClaimIntent \| RushIntent \| DistributeIntent \| None` | #8 | §1.7 GK-branch intent surface (anchor pinned during implementation) | §3.1 / §3.2 / §3.7 / §3.8 |
| `PositioningAI.GetGKBaselineSlot(matchTime) → Vector2` | #12 | §3.3.3 (consumed per KD-3 / §3.3.0) | §3.3.0 |
| `DeterministicRng.NextFloat(drawSiteId, domainTag) → float`, `DeterministicRng.NextGaussian(drawSiteId, domainTag) → float` | #16 | §4.1 / §4.5 | §3.3 / §3.5 / §3.6 |

---

## 4.3 Output Interface Contracts (emitted)

| Surface | Spec | Section | Used in |
|---------|------|---------|---------|
| `Ball.ApplyKick(velocity, spin, agentId, matchTime)` | #1 | §3.1.11.2 | §3.5 / §3.8 |
| `Ball.SetPossessor(agentId)` | #1 | §3.1 possession surface (anchor pinned during implementation; presumed published per ERR-008 resolution; if absent at implementation time, a back-prop entry filed as `ERR-011-002` for a pure namespace amendment to APPROVED #1) | §3.5 catch path / §3.7 smother |
| `EventBus.Publish<SaveAttemptedEvent>(evt)` | #17 | §3.2.1 | §3.5 / §3.9 |
| `EventBus.Publish<BallClaimedEvent>(evt)` | #17 | §3.2.1 | §3.5 catch / §3.6 / §3.7 |
| `EventBus.Publish<DistributionExecutedEvent>(evt)` | #17 | §3.2.1 | §3.8 |
| `EventBus.Publish<GoalkeeperRushEvent>(evt)` | #17 | §3.2.1 | §3.7 |
| `PassMechanics.ConsumePassIntent(passIntent)` | #5 | §3 (anchor pinned during implementation) | §3.8 |
| `Heading.SubmitGKIntent(headerIntent)` | #10 | (used when a GK head save occurs; payload routed via Decision Tree #8 GK branches per KD-4) | §3.6 head-route |

**`Ball.SetPossessor` verification posture (OI-006).** This surface
is presumed published per the ERR-008 resolution recorded in
`docs/tracking/spec-error-log.md` (Option B: possession external to
`BallState`). If the surface name or signature has drifted at
implementation time, the resolution is a pure namespace amendment
to APPROVED #1, filed as `ERR-011-002`. This is documented in §9.4
as an outstanding item but does NOT block #11's `IN REVIEW`
transition.

---

## 4.4 Determinism Compliance Surface

Listing of all #11 → #16 touchpoints per KD-7:

### 4.4.1 Domain-tag allocation

`DOMAIN_TAG_GOALKEEPER` allocated `[CROSS-PENDING]`. Proposed value
`0x17`; if ERR-012-001 (#12 `DOMAIN_TAG_POSITIONING_AI` block
`0x17…0x1C`) is ratified by lead-developer before ERR-011-001
lands, `DOMAIN_TAG_GOALKEEPER` shifts to `0x1D` per KD-7
collision-management policy. Back-propagation entry filed as
`ERR-011-001` in `docs/tracking/spec-error-log.md`. Allocation is
a pure namespace amendment to APPROVED #16 (no
`DETERMINISM_DIGEST_VERSION` bump), following the precedent of
ERR-010-001 (`0x16`, May 16, 2026) and ERR-017-001 (`0x15`,
May 14, 2026).

### 4.4.2 Registered draw sites (four)

| Draw site ID | Used in | Purpose |
|--------------|---------|---------|
| `DRAW_SITE_HANDLING_NOISE` | §3.5.1 `handlingScaleNoise` | Gaussian perturbation of `rawHandling` |
| `DRAW_SITE_HANDLING_POINT_NOISE` | §3.5.1 `pointErrorNoise` | Gaussian perturbation of `contactPointError` (separate per KD-7 single-purpose-per-site rule) |
| `DRAW_SITE_DIVE_TIMING_JITTER` | §3.3.2 | Gaussian dive launch timing jitter |
| `DRAW_SITE_CROSS_CLAIM_TIEBREAK` | §3.6.3 | Gaussian near-tie perturbation in cross-claim duel |

Each draw site call passes `DOMAIN_TAG_GOALKEEPER` per #16 §4.5
draw-site registry signature.

### 4.4.3 Iteration-order discipline

The cross-claim duel in §3.6.3 iterates participants in #16 §3.2
entity order. Tied scores after Gaussian tiebreak fall back to
entity-order secondary sort. With one GK per side, no further
iteration-order concern exists in #11.

---

## 4.5 Performance Compliance Surface

Pre-commitments referenced from Performance Optimization #18 §6
ratify-not-override (KD-2 of #18). Budgets are framed by
steady-state vs. p99 tail, mirroring Heading #10 H-4 reconciliation.

### 4.5.1 Hot-path allocation budget

0 bytes/tick `[FIXED]` per #18 §3.10. Struct-based data flow; no
`new` in formula files; `ReadOnlySpan<>` for #3 contact-event
consumption.

### 4.5.2 Steady-state per-tick cost budget

≤40 µs `[EST]` at 22-agent match peak (v0.2 AR-S1-H1 reconciliation
— see §6.1 / §6.3.1). Only 2 GK agents per match
(vs. 22 outfielders), so steady-state cost is dominated by:

- State-machine evaluation: amortised ≈5 µs / GK at 60 Hz.
- Reactive-position micro-update vs. #12 baseline: ≈10 µs / GK at
  60 Hz.

### 4.5.3 p99 save-frame tail budget

≤220 µs `[EST]` at save-resolution frames. Decomposition (§6.3):

- Dive launch + integration: ≈40 µs.
- #3 hand-ball contact resolution consumption: ≈30 µs.
- §3.5 handling-quality computation (3 Gaussian draws + 4
  multiplications + 1 clamp): ≈80 µs.
- Band-to-action dispatch + `Ball.*` emission: ≈40 µs.
- `SaveAttemptedEvent` serialisation: ≈30 µs.

### 4.5.4 p99 cross-claim duel-frame tail budget

≤280 µs `[EST]` at 3-way duel frames. Decomposition:

- Body-part determination across 3 agents (§3.6.1): ≈60 µs.
- Duel-score arithmetic (3 weighted attributes, ranking): ≈40 µs.
- Tiebreak Gaussian + re-rank: ≈30 µs.
- Head-route deferral or §3.5 invocation: ≈150 µs (dominated by §3.5).

### 4.5.5 Budget-credibility caveat

All `[EST]` budgets above are NOT credible until
`certification-platform.md` Stage-0 host pin lands. `FR-PO-052`
Stage 0+1 perf-gate activation is gated on that pin and not on
#11 sign-off (per Heading #10 OI-006 precedent).

### 4.5.6 `HotPathAllocExempt` attribute uses

None required. The struct-based data flow above prevents any
hot-path allocations; the attribute deferral to Stage 0+1 per #18
§3.7.5 does not affect #11 source files.

---

## 4.6 Tick-Scheduling Surface

### 4.6.1 10 Hz tactical loop (every 100 ms)

- Read `BallPhysics.GetBallState(currentTick)` snapshot.
- Read `PositioningAI.GetGKBaselineSlot(currentTick)`.
- Query `DecisionTree.GetGKIntent(gkId, currentTick)` for each GK
  in #16 §3.2 entity order.
- Evaluate state-machine transitions (§3.1).
- Apply reactive-position micro-update bounded by
  `GK_REACTIVE_RADIUS_M` (§3.3.0).
- Update `releaseTickEarliest` countdown for `HandsOnBall` state.

### 4.6.2 60 Hz physics loop (every 16.667 ms)

- Subscribe-side: ingest `ShotExecutedEvent` (#6), `#3` contact
  events.
- Update reaction-pipeline state for `Anticipate` / `Diving` /
  `Airborne` GKs: advance `elapsedSinceShotMs`,
  `reactionWindowAchieved`.
- Integrate dive kinematics (§3.3) for `Diving` / `Airborne`.
- Resolve candidate hand-ball contacts (§3.5) for `Airborne` /
  `OneOnOne` / `Smothered` / `Rushing`.
- Resolve cross-claim duels (§3.6) when ≥2 agents within
  `CROSS_CLAIM_VOLUME_RADIUS_M`.
- Per-frame rush update (§3.7.2) for `Rushing`.
- Emit telemetry channels (§2.4).
- Publish events (§4.3) to Event System #17.

### 4.6.3 ASCII sequence diagram (single save attempt)

```
t-100ms │ 10 Hz tick │ #8 commits SaveIntent → state Anticipate → Diving
        │            │ DecisionTree.GetGKIntent returns SaveIntent
t-83ms  │ 60 Hz frame│ §3.2 reaction-pipeline update; dive launch impulse
        │            │ state Diving → Airborne; §3.3 integration begins
t-67ms  │            │ §3.3 integration; handPathZ rises toward apex
...     │            │
t   0ms │            │ #3 hand-ball contact event fires
        │            │ §3.5 handling-quality computation
        │            │ band-to-action dispatch (Caught | Parried | ...)
        │            │ Ball.SetPossessor OR Ball.ApplyKick
        │            │ EventBus.Publish<SaveAttemptedEvent>
        │            │ state Airborne → HandsOnBall (catch) or Recovering
```

---

## 4.7 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; 11 source files declared, input/output contracts tabulated, four draw sites registered, performance budgets staged, tick-scheduling sequence diagrammed | self-pass-1 in `adversarial-review-section-files-v1.md` |
| 0.2 | May 16, 2026 | pass-1 fix pass | AR-S1-H1 (§4.5.2 steady-state budget revised to ≤40 µs) | self-pass-2 self-critique on v0.2 yields no further findings |
