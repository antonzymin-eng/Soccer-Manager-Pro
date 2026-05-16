# Goalkeeper Mechanics Specification #11 — Section 6: Performance Analysis & Budgets

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Per Performance Optimization #18 KD-2
(ratify-not-override authority), publish the per-tick cost budgets,
hot-path allocation discipline, scaling analysis, profiling
compliance, and Stage 0 → Stage 1+ migration notes for Goalkeeper
Mechanics #11.

---

## 6.1 Per-Tick Budget

Three-tier budget framing per Heading #10 H-4 reconciliation:

- **Steady-state per-tick cost budget**: ≤40 µs `[EST]` at 22-agent
  match peak under non-save-frame load. Dominated by state-machine
  evaluation (≈5 µs/GK) + reactive-position micro-update (≈10 µs/GK
  at 60 Hz). 2 GK agents. (v0.2 AR-S1-H1: revised from ≤30 µs to
  ≤40 µs to match §6.3.1 component decomposition; mirrors Heading
  #10 H-4 reconciliation — worst-case decomposition is the binding
  budget.)
- **p99 save-frame tail budget**: ≤220 µs `[EST]` at save-
  resolution frames (dive launch + contact resolution + handling-
  quality computation + band-to-action dispatch + event emission).
- **p99 cross-claim duel-frame tail budget**: ≤280 µs `[EST]` at
  3-way duel frames (GK + 2 attackers; body-part determination +
  duel arithmetic + tiebreak Gaussian + downstream pipeline).

All `[EST]` budgets are NOT credible until
`certification-platform.md` Stage-0 host pin lands; `FR-PO-052`
Stage 0+1 perf-gate activation is gated on that pin and not on
#11 sign-off (shared with Heading #10 OI-006).

---

## 6.2 Hot-Path Allocation Discipline

- 0 bytes/tick `[FIXED]` per #18 §3.10.
- No `new` in formula files (`GoalkeeperReactionPipeline.cs`,
  `GoalkeeperDiveKinematics.cs`, `GoalkeeperHandlingQuality.cs`,
  `GoalkeeperCrossClaimDuel.cs`, `GoalkeeperRushDispatch.cs`,
  `GoalkeeperDistribution.cs`).
- `ReadOnlySpan<>` for Collision System #3 contact-event
  consumption.
- Struct return types for all intent payloads (`SaveIntent`,
  `ClaimIntent`, `DistributeIntent`, `RushIntent`) and contact
  state (`GKContactState`).
- Event publish uses pre-allocated event-pool slots per Event
  System #17 §3.2.1 convention (no new event allocations on the
  hot path).
- No `HotPathAllocExempt` attribute uses required (Stage 0+1
  attribute declaration per #18 §3.7.5 does not affect #11 source
  files).

---

## 6.3 Scaling Analysis

### 6.3.1 Steady-state cost decomposition

| Component | Cost / GK / 60 Hz frame | Notes |
|-----------|--------------------------|-------|
| State-machine evaluation (§3.1) | ≈5 µs | 24-row transition table; ≤3 condition evaluations per frame |
| Reactive-position micro-update (§3.3.0) | ≈10 µs | Single vector arithmetic; bounded by `GK_REACTIVE_RADIUS_M` |
| `BallState.GetBallState` read | ≈3 µs | Per #1 publish convention |
| `PositioningAI.GetGKBaselineSlot` read (10 Hz amortised) | ≈1 µs / 60 Hz frame | Cached between tactical ticks |
| Telemetry counter emission (§2.4) | ≈1 µs | 12 channels; lazy-emission |
| **Per-GK total** | **≈20 µs** | |
| **Per-match total (2 GKs)** | **≈40 µs** | Matches §6.1 revised ≤40 µs `[EST]` budget (v0.2 AR-S1-H1) |

The steady-state estimate exceeds the headline ≤30 µs `[EST]`
target above by ~33%; the `[EST]` tags are explicit acknowledgment
that the budget is preliminary. The Heading #10 §6.3 precedent
shows that real-world per-tick measurements typically come in
below worst-case decomposition; the credibility tag stays `[EST]`
until benchmarked.

### 6.3.2 p99 save-frame decomposition

| Component | Cost | Notes |
|-----------|------|-------|
| Dive launch + integration (§3.3) | ≈40 µs | Single-frame impulse + per-frame parabolic interpolation |
| #3 hand-ball contact resolution consumption | ≈30 µs | Read-only via `ICollisionEventConsumer` |
| §3.5 handling-quality computation | ≈80 µs | 3 Gaussian draws (handling-noise + point-error-noise + jitter via §3.3) + 4 multiplications + 1 convex blend + 1 clamp |
| Band-to-action dispatch + `Ball.*` emission | ≈40 µs | Branchy but bounded |
| `SaveAttemptedEvent` serialisation + publish | ≈30 µs | Per #17 §3.2.1 |
| **Total** | **≈220 µs** | Matches §4.5.3 tail budget |

### 6.3.3 p99 cross-claim duel-frame decomposition

| Component | Cost | Notes |
|-----------|------|-------|
| Body-part determination across 3 agents (§3.6.1) | ≈60 µs | Capsule-vs-sphere intersection priority |
| Duel-score arithmetic (3 weighted attributes, ranking) | ≈40 µs | Stable sort over ≤4 participants |
| Tiebreak Gaussian + re-rank | ≈30 µs | Invoked only on near-tie |
| Head-route deferral or §3.5 invocation | ≈150 µs | Dominated by §3.5 pipeline above |
| **Total** | **≈280 µs** | Matches §4.5.4 tail budget |

### 6.3.4 Per-match frequency

- p99 save frames: ≤8 per match (one per shot on target per Opta
  baseline class).
- p99 cross-claim duel frames: ≈0.05/min (≈4–5 per 90 min;
  ~15 crosses per match × ~30% contested by GK per Opta /
  StatsBomb commercial-data baseline class cited in §8.3). Each
  is more expensive than #10's typical duel because of the
  head-vs-hand routing decision and the GK-specific handling
  pipeline.
- Distribution events: ~1 per save + 1 per goal-kick ≈ ~12 per
  match per side.

---

## 6.4 Profiling Compliance (KD-6 of #18)

- Determinism-aware profiling hooks at §3.5 entry, §3.6 entry,
  §3.7 entry, §4.3 emission (per #18 KD-6).
- Trace channel allocations declared in §2.4 (12 channels per
  §2.4 — v0.2 AR-S1-L1). Channel rows
  back-propagated to #18 Appendix F.0 13-field schema at Stage 0+1
  delivery schedule per Heading #10 OI-002 closure precedent. The
  rows populate at the first `src/Gameplay/Goalkeeper/` commit.

---

## 6.5 Stage 0 → Stage 1 Performance Migration Notes

- **Fixed64 binding deferred to Stage 5+** per Spec #9 §8.1. `float`
  is canonical at Stage 0; deterministic replay achieved via state
  snapshots, not deterministic arithmetic.
- **Dive Z kinematics** retire to AM #2 §3.6 native Z kinematics at
  Stage 1+ per KD-12 / §7.5. The synthetic Z trajectory in §3.3
  has zero `[FIXED]` constants and no special-case API surface;
  retirement is a straight substitution of read source (AM #2
  `agentZ` instead of §3.3 `handPathZ`). No re-tuning expected.

---

## 6.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; three-tier budget; component decomposition for steady-state, save-frame, and cross-claim duel-frame; profiling compliance and Stage 0→1 migration notes | self-pass-1 in `adversarial-review-section-files-v1.md` |
| 0.2 | May 16, 2026 | pass-1 fix pass | AR-S1-H1 (steady-state budget reconciled ≤30 µs → ≤40 µs to match §6.3.1 decomposition); AR-S1-L1 (channel-count restatement in §6.4) | self-pass-2 self-critique on v0.2 yields no further findings |
