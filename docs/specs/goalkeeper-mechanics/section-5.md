# Goalkeeper Mechanics Specification #11 — Section 5: Test Plan

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Specify unit tests, integration tests, validation
scenarios, and cross-spec conformance tests for Goalkeeper
Mechanics #11. Test framework conventions follow Testing Strategy
#19 §3.

---

## 5.1 Unit Tests

One sub-section per §3 algorithm; ~6–10 test cases each.

### 5.1.1 State machine (§3.1)

- T-5.1.1.1 Every transition exercised (24 transitions from §3.1.1).
- T-5.1.1.2 Cycle detection: `Resting → Set → Anticipate → Diving
  → Airborne → Recovering → Set → ...` round-trip.
- T-5.1.1.3 Deterministic ordering under sequential `ShotExecutedEvent`
  + `RushIntent` arrival in the same tick.
- T-5.1.1.4 `HandsOnBall → HandsOnBall` (forced release at
  `GK_HOLD_MAX_TICKS = 60` ticks) verified.
- T-5.1.1.5 Invalid transition rejection (e.g. `Resting → Diving`
  blocked).
- T-5.1.1.6 State invariants: `Airborne` implies `handPathZ > 0`;
  `Diving` implies `gk.kinematics.velocityXY ≠ 0`.

### 5.1.2 Reaction pipeline (§3.2)

- T-5.1.2.1 Sweep `reactionOffsetMs` from −300 ms to +300 ms in
  20 ms steps; verify `reactionWindowAchieved` is monotonic-
  decreasing in `|reactionOffsetMs|` and uses asymmetric
  tolerances per KD-18.
- T-5.1.2.2 Sweep `Reflexes_norm` 0…1 in 0.1 steps; verify
  `requiredReactionMs` monotonic-decreasing.
- T-5.1.2.3 Sweep ball speed 10…40 m/s in 2 m/s steps; verify
  `requiredReactionMs` monotonic-increasing above
  `REACTION_BALL_SPEED_REF_MPS`.
- T-5.1.2.4 `OneOnOne` state on/off: `requiredReactionMs` shifts
  by `−ONE_VS_ONE_REACTION_COEFF · OneVsOne_norm` (KD-20).
- T-5.1.2.5 Label-band boundary cases: `reactionWindowAchieved`
  exactly at `REFLEXIVE_LABEL_THRESHOLD` and
  `SLUGGISH_LABEL_THRESHOLD`.
- T-5.1.2.6 Determinism: same inputs → same `reactionOffsetMs`
  across 10 runs (no `System.Random` / `DateTime.Now` leakage).

### 5.1.3 Dive kinematics (§3.3)

- T-5.1.3.1 Sweep `Strength_norm` ±10% around 0.5; verify
  `diveLaunchImpulse` linear.
- T-5.1.3.2 Sweep `Aerial_norm`; verify `peakHandZ_m` linear.
- T-5.1.3.3 Sweep fatigue 0…1; verify reach reduction at fatigue=1
  matches `−DIVE_FATIGUE_PEAK_Z_COEFF` (KD-8 sign).
- T-5.1.3.4 Apex-Z sensitivity: peak frame at `apexFrame`; ground
  re-entry at `diveLaunchFrame + diveDurationFrm`.
- T-5.1.3.5 Gaussian jitter mean = 0 over 10000 draws (single
  draw site).
- T-5.1.3.6 `GroundedReason.DIVING_HEADER` on ground re-entry per
  KD-12 (FR-GK-036).

### 5.1.4 Handling-quality scalar (§3.5)

- T-5.1.4.1 Sweep point error 0…0.1 m in 0.005 m steps; verify
  `pointQuality` linear and clamped.
- T-5.1.4.2 Sweep ball speed; verify `speedFactor` clamped at 0
  for very fast shots.
- T-5.1.4.3 Sweep `Handling_norm`; verify `attrFactor` linear.
- T-5.1.4.4 `OneOnOne` state on/off: `attrFactor` shifts by
  `+ONE_VS_ONE_HANDLING_COEFF · OneVsOne_norm` (KD-20).
- T-5.1.4.5 Fatigue sweep 0…1; verify `attrFactor` decreases
  (KD-8 sign).
- T-5.1.4.6 Convex-blend invariant: weights of
  `HANDLING_REACTION_BLEND_ALPHA` and `1 − HANDLING_REACTION_BLEND_ALPHA`
  sum to 1.0.
- T-5.1.4.7 Independent draw-site invariant: 1000 draws on
  `DRAW_SITE_HANDLING_NOISE` and `DRAW_SITE_HANDLING_POINT_NOISE`
  produce statistically independent sequences (correlation ≈ 0)
  per KD-7 single-purpose-per-site rule.

### 5.1.5 Band-to-action mapping (§3.5.2)

- T-5.1.5.1 Each band boundary exercised by sweeping
  `handlingQualityScalar` from 0 to 1 in 0.01 steps; verify
  band-label assignment matches §3.5.2.
- T-5.1.5.2 `Ball.SetPossessor(gkId)` invoked iff label = `Caught`
  (KD-21 / FR-GK-023).
- T-5.1.5.3 `Ball.ApplyKick(...)` invoked iff label ∈ `{Parried,
  Deflected, Spilled}` (KD-21 / FR-GK-023).
- T-5.1.5.4 No ball API invoked iff label = `Missed`; ball state
  unchanged; `SaveAttemptedEvent.failureCause` populated (KD-11 /
  F-01..F-03).
- T-5.1.5.5 `parryVelocity` retain monotonic-decreasing in quality.
- T-5.1.5.6 `spillVelocity` retain > `parryVelocity` retain at
  matched quality (monotonicity invariant).

### 5.1.6 Cross-claim duel (§3.6)

- T-5.1.6.1 2-way GK vs. striker with `|scoreA − scoreB| >
  CROSS_CLAIM_TIEBREAK_EPSILON`: tiebreak NOT invoked.
- T-5.1.6.2 2-way with `|scoreA − scoreB| < epsilon`: tiebreak
  invoked; winner depends on RNG seed.
- T-5.1.6.3 3-way GK + 2 strikers: iteration-order determinism;
  same seed → same winner across 1000 runs.
- T-5.1.6.4 Head-vs-hand routing (KD-14): contact body part
  determined by §3.6.1 capsule/sphere priority; head route invokes
  Heading #10 §3.7 path; hand route invokes §3.5.
- T-5.1.6.5 Loser emission: `SaveAttemptedEvent.failureCause =
  DisturbedInDuel` for each loser when winner ≠ gk.
- T-5.1.6.6 Weight-sum invariant: `CROSS_CLAIM_DUEL_BALANCE_W +
  CROSS_CLAIM_DUEL_STRENGTH_W + CROSS_CLAIM_DUEL_AERIAL_W = 1.0`.

### 5.1.7 Rush dispatch (§3.7)

- T-5.1.7.1 Commit threshold: `commitmentLevel >
  RUSH_COMMIT_THRESHOLD` enters `Rushing`; ≤ threshold does NOT.
- T-5.1.7.2 Abort on `BallIntercepted` per KD-15;
  `GoalkeeperRushEvent.abortReason = BallIntercepted` emitted.
- T-5.1.7.3 KD-15 non-abort under ball-trajectory change:
  `rushTarget` modified mid-flight → rush continues to original
  `rushTarget`.
- T-5.1.7.4 `Pace_norm` linear effect on `rushLaunchMps`.
- T-5.1.7.5 Fatigue reduces `rushLaunchMps` linearly (KD-8 sign).
- T-5.1.7.6 `Rushing → Smothered` on hand contact; `Rushing →
  OneOnOne` on attacker proximity.

### 5.1.8 Distribution generation (§3.8)

- T-5.1.8.1 Each `deliveryKind` (Throw / Roll / Kick): release
  height + windup match §3.4.7 anchors.
- T-5.1.8.2 `PassIntent` mapping correctness (§3.8.4): one-to-one
  Throw → LowDriven, Roll → GroundRoll, Kick → Lofted.
- T-5.1.8.3 F-05 missing-receiver fallback: `targetReceiverId`
  cleared; `targetPoint` retained; warning telemetry emitted.
- T-5.1.8.4 F-09 out-of-bounds clamp: `targetPoint` clamped to
  `[0, PITCH_LENGTH_M] × [0, PITCH_WIDTH_M]`.
- T-5.1.8.5 6-second-rule forced release: `HandsOnBall`
  duration `≥ GK_HOLD_MAX_TICKS` triggers `Distributing` entry
  even without `DistributeIntent` (default distribution: ROLL
  to nearest defender).

### 5.1.9 Failed-save emission (§3.9)

- T-5.1.9.1 Each `failureCause` enum value emitted for the
  corresponding F-01..F-04 detection condition.
- T-5.1.9.2 No `Ball.ApplyKick` or `Ball.SetPossessor` invoked on
  any failure path (FR-GK-009).
- T-5.1.9.3 Ball state unchanged (`incomingBallState ==
  outgoingBallState`).
- T-5.1.9.4 F-07 (non-eligible state): NO `SaveAttemptedEvent`
  emitted; #3 standard rebound physics resolves contact.
- T-5.1.9.5 F-10 (range-clamp): `clutchFirmness = 1.5` clamped to
  1.0 with warning telemetry.

---

## 5.2 Integration Tests

- T-5.2.1 Open-play save from a Shot Mechanics #6 shot (consumes
  `ShotExecutedEvent`).
- T-5.2.2 Free-kick save (set-piece pathway; KD-19): identical
  pipeline behavior; wall is NOT modelled.
- T-5.2.3 Penalty save: penalty shot from 11 m; GK initially in
  `Set`; expected reaction window is at the early-tolerance edge
  due to short shot flight.
- T-5.2.4 Corner cross-claim — hand-contact path (§3.6 hand route).
- T-5.2.5 Corner cross-claim — head-contact path (routes to Heading
  #10; KD-14 / FR-GK-004 / FR-GK-022).
- T-5.2.6 1v1 confrontation (`OneOnOne` state; `OneVsOne` attribute
  effects per KD-20).
- T-5.2.7 Mistimed dive → failed save → no ball state change
  (FR-GK-009).
- T-5.2.8 GK rush + abort on interception (KD-15).
- T-5.2.9 Save + distribution: full cycle from `ShotExecutedEvent`
  to `DistributionExecutedEvent`; verify Pass Mechanics #5
  receives a valid `PassIntent` via `ConsumePassIntent`.
- T-5.2.10 Deterministic replay: a 1000-tick scenario producing
  identical `SaveAttemptedEvent` and `DistributionExecutedEvent`
  sequences across 10 runs with the same RNG seed.
- T-5.2.11 #12 baseline-slot ratification: with the #12 v1.0.x
  patch applied (post-`IN REVIEW` transition), GK reads baseline
  from `PositioningAI.GetGKBaselineSlot` and applies KD-13
  reactive radius correctly.

---

## 5.3 Validation Scenarios (match-feel)

- T-5.3.1 22-agent match peak: 90-minute simulation with ~4
  shots-on-target per side (~8 total per match per Opta /
  StatsBomb baseline class cited in §8.3); verify
  handling-label distribution (`Caught` / `Parried` / `Deflected`
  / `Spilled` / `Missed`) matches a designer-set target band.
- T-5.3.2 Reflex A/B: same shot trajectory, two GK profiles
  (`Reflexes_norm = 0.60` vs. `0.90`); measurable
  `reactionWindowAchieved` divergence (target ≥0.20).
- T-5.3.3 Fatigue gradient: dive `peakHandZ_m` at fatigue 0.0 vs.
  1.0; reach reduction ≈ `DIVE_FATIGUE_PEAK_Z_COEFF /
  DIVE_PEAK_Z_BASE_M` ≈ 17% (validation against KD-8 plus the
  §3.4 anchor values).
- T-5.3.4 1v1 conversion rate: striker through-ball into 1v1;
  GK closes via `Rush → OneOnOne → Smothered`; verify save rate
  ≈70% (i.e. conversion ≈30%) matches Opta 1v1 baseline class
  cited in §8.3.

---

## 5.4 Cross-Spec Conformance Tests

- T-5.4.1 No `SaveType` / `SaveClass` / `SaveOutcome` symbol
  exists in `src/` (grep gate; KD-1 / FR-GK-003).
- T-5.4.2 Every constant in `GoalkeeperConstants.cs` has a
  source-tag comment in `{[GT], [EST], [FIXED], [DERIVED],
  [CROSS], [CROSS-PENDING]}` (KD-9 / FR-GK-015 / §9.1
  programmatic verification per Code Standards #20 §3).
- T-5.4.3 Every RNG call uses
  `DeterministicRng.NextFloat(drawSiteId, DOMAIN_TAG_GOALKEEPER)`
  or `DeterministicRng.NextGaussian(...)` (KD-7 / FR-GK-010).
  Grep gate against `System.Random` and `Random.Range`.
- T-5.4.4 #12 GK constant promotion atomic with #11 IN REVIEW: a
  scripted gate verifies that at the commit moment of the #11
  status flip, `positioning-ai/section-3.md` GK constants carry
  `[GT]` and `positioning-ai/section-6.md` rows agree (KD-13).
- T-5.4.5 No `System.Random` / `DateTime.Now` paths in any
  `src/Gameplay/Goalkeeper/` file (CLAUDE.md / FR-GK-027).
- T-5.4.6 Every `XC-011-NNN` cross-reference resolves to a
  specific section in the named upstream spec (§9.2).
- T-5.4.7 Iteration order in §3.6.3 cross-claim duel is exactly
  #16 §3.2 entity order — verified by enumerating duel
  participants under fixed seed and asserting stable winner
  ordering on tie-after-tiebreak fallback.

---

## 5.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; 9 unit-test families, 11 integration tests, 4 validation scenarios, 7 cross-spec conformance gates | self-pass-1 in `adversarial-review-section-files-v1.md` |
