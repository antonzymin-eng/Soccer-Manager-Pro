# Heading Mechanics Specification #10 — Section 5: Test Plan

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Define unit, integration, validation, and cross-spec
conformance tests for Heading Mechanics #10. Test layout follows
Spec #19 (Testing Strategy & Framework) §3.x conventions: one test
file per `HeadingMechanics.cs` source file under
`tests/Gameplay/Heading/`.

---

## 5.1 Unit Tests

One sub-section per §3 algorithm. Test counts are minimums; designer
may add cases.

### 5.1.1 Eligibility Predicate (`HeadingEligibility.cs`)

Truth-table over the three eligibility inputs:

| Aerial phase | Ball in `HEAD_CONTACT_VOLUME` | Predicted body part = `Head` | Expected |
|--------------|-------------------------------|------------------------------|----------|
| In synthetic jump (#10-owned) | yes | yes | `isEligible = true` |
| Grounded (`STANDING`) but standing-high-reach | yes | yes | `isEligible = true` |
| `GROUNDED` / `STUMBLING` (AM #2) | yes | yes | `isEligible = false` |
| In jump | no | yes | `isEligible = false` |
| In jump | yes | no (foot/torso predicted) | `isEligible = false` |

Verifies KD-3 (body-part discriminator), KD-18 (Stage 0 aerial-phase
ownership), and FR-HE-001.

### 5.1.2 JumpReach Formula (`HeadingJumpKinematics.cs`)

Sensitivity sweep at ±10 % per input attribute:

- `Strength_norm` ∈ {0.0, 0.5, 1.0}
- `Balance_norm` ∈ {0.0, 0.5, 1.0}
- `Heading_norm` ∈ {0.0, 0.5, 1.0}

Expected: `JumpReach_m` monotone non-decreasing in each attribute;
no negative values; finite for all inputs. Verifies FM-010-001,
FR-HE-004, FR-HE-021. ~27 cases.

### 5.1.3 Contact-Quality Scalar (`HeadingContactQuality.cs`)

- Signed timing-offset sweep: `timingOffsetMs ∈ {-200, -100, -50,
  -25, 0, +25, +50, +100, +200}` with `pointError = 0`.
- Point-error sweep: `pointError ∈ {0, 0.01, 0.02, 0.03, 0.05} m`
  with `timingOffsetMs = 0`.
- Asymmetric tolerance check (pass-1 H-1): at `timingOffsetMs = +50`
  vs. `-50`, `timingQuality` differs because `MAX_LATE_TOLERANCE_MS <
  MAX_EARLY_TOLERANCE_MS`.
- RNG-disabled determinism: with `TIMING_JITTER_SIGMA_MS = 0` and
  `CONTACT_POINT_NOISE_SIGMA_M = 0`, identical inputs produce
  identical outputs across runs.

Verifies FM-010-002, FR-HE-002, FR-HE-022. ~20 cases.

### 5.1.4 Power & Launch-Angle Generation (`HeadingPowerAngle.cs`)

- `PowerIntent` sweep: {0.1, 0.3, 0.5, 0.7, 1.0}.
- `fatigue` sweep: {0.0, 0.25, 0.5, 0.75, 1.0}.
- Combined grid produces ~25 cases.

Expected: `outgoingSpeed` monotone increasing in `PowerIntent`,
monotone decreasing in `fatigue`. Fatigue convention check: at
`fatigue = 0` the effective attribute equals `Heading_norm`; at
`fatigue = 1` it is reduced by `POWER_FATIGUE_COEFF`. Verifies
FM-010-003, FR-HE-011.

### 5.1.5 Spin Transfer (`HeadingSpinTransfer.cs`)

- Incoming-spin direction sweep: pure topspin, pure backspin, pure
  sidespin, zero spin.
- Contact-point axial-offset sweep: {-0.03, -0.015, 0, +0.015,
  +0.03} m.
- Reversal-boundary test (v0.2 H-1): at `contactPointAxialOffset =
  SPIN_TRANSFER_REVERSAL_THRESHOLD`, `spinPreservationFactor` is
  exactly zero and the incoming-spin contribution to `outgoingSpin`
  is exactly zero. At `contactPointAxialOffset = 2 ·
  SPIN_TRANSFER_REVERSAL_THRESHOLD`, `spinPreservationFactor =
  -SPIN_PRESERVATION_BASE` and the incoming-spin contribution is
  `incomingSpin · (-SPIN_PRESERVATION_BASE)` (single reversal,
  proportional to overshoot — verify the v0.1 double-reversal bug
  has not regressed). Add a monotonicity assertion: the magnitude
  of the incoming-spin contribution is linear in
  `contactPointAxialOffset`, with no discontinuity at the
  threshold.

- Heading-attribute pointError direction (v0.2 H-2): hold
  `||contactPointActual − contactPointIntent||` constant; sweep
  `Heading_norm ∈ {0.0, 0.5, 1.0}`; assert `pointQuality` is
  monotone non-decreasing in `Heading_norm` (higher-Heading
  agents produce smaller effective `pointError` and thus higher
  `pointQuality` for the same physical contact geometry).

Verifies FM-010-004, FR-HE-015, KD-16. ~20 cases.

### 5.1.6 Duel Resolution (`HeadingDuelResolution.cs`)

- 2-way duel: defender vs. striker, varying `Heading × Strength ×
  Balance` profiles.
- 3-way duel: striker + striker + defender.
- Tiebreak-invocation count (v0.2 M-6): 1000 deterministic-replay
  iterations on near-tie configurations — verify
  `DRAW_SITE_DUEL_TIEBREAK` is called exactly `N` times per duel,
  where `N` is the count of participants whose `baseScore` lies
  within `DUEL_TIEBREAK_EPSILON` of `baseScore[rank0]`. Cases:
  2-way near-tie → `N = 2`; 3-way near-tie (all three within ε) →
  `N = 3`; 3-way with two clustered + one outlier → `N = 2`;
  non-tie (gap > ε) → `N = 0`.
- Iteration-order determinism: shuffle the input order of duel
  participants; verify identical winner per #16 §3.2 entity
  ordering.

Verifies FM-010-005, FR-HE-010, FR-HE-017, FR-HE-023.

### 5.1.7 Failed-Attempt Emission (`HeadingMechanics.cs`) — v0.2 M-7 split

Split into two groups matching the §2.3 failure-mode semantics.

**Group A — F-01..F-04 (failed-event emission).** One test per
mode. Each test:

1. Construct an input scenario that triggers exactly that failure.
2. Run the heading pipeline for one 60 Hz tick.
3. Assert: no `Ball.ApplyKick` invocation; `BallState` unchanged
   after the tick; `HeaderAttemptFailedEvent` published with
   `failureCause` matching the expected enum value (`MistimedEarly`
   / `MistimedLate` / `PositionedPoorly` / `DisturbedInDuel`).

Verifies FR-HE-006, KD-12.

**Group B — F-05..F-07 (continue-with-modification semantics).**
One test per mode, each asserting the documented non-failed
behaviour:

- F-05 (`targetIntent` outside pitch bounding box, FR-HE-029):
  assert `targetIntent` is clamped to the nearest in-bounds
  point; assert a warning-channel telemetry entry is emitted;
  assert NO `HeaderAttemptFailedEvent` is published; assert the
  attempt continues to evaluate eligibility.
- F-06 (stale `BallState`, FR-HE-033): assert `BallState` is
  re-queried via `BallPhysics.GetBallState(currentTime)`; assert
  a diagnostic-channel entry is emitted; assert NO
  `HeaderAttemptFailedEvent`; assert no extrapolation occurred.
- F-07 (`contactPointIntent` outside head-local envelope,
  FR-HE-030): assert the intent is clamped to the envelope edge;
  assert the clamp delta is reflected in the `pointError`
  component of `contactQualityScalar` via §3.4; assert NO
  standalone telemetry channel or failed event is generated.

### 5.1.8 Own-Goal-Shape Flag (`HeadingMechanics.cs`)

- Positive case A: defender heads ball with `outgoingVelocity`
  vector pointing toward own goal-line bounding box; trajectory
  intersects within the dual-horizon window. Expected:
  `ownGoalShapedTrajectory = true`.
- Positive case B: header trajectory exits over own goal-line but
  outside the bounding box. Expected: `false`.
- True-negative: attacker heads ball toward opponent goal.
  Expected: `false`.
- Dual-horizon boundary: flat header just inside `horizon_m` but
  past `horizon_s` → flag triggered by distance cap. Loop header
  just inside `horizon_s` but short of `horizon_m` → flag
  triggered by time cap.

Verifies FR-HE-007, KD-6, pass-1 L-7.

---

## 5.2 Integration Tests

End-to-end multi-tick scenarios. Each scenario uses the deterministic
scheduler with a pinned seed.

### 5.2.1 Open-Play Header from a Pass Mechanics #5 Cross

Pass Mechanics #5 delivers a cross from the right wing; striker is
positioned in the box. Header pipeline consumes the resulting
`BallState` at the predicted contact frame and produces an outgoing
velocity. Verifies KD-5 (no Pass-specific label coupling — the test
asserts that no `CrossDelivery` or similar label is read by #10).

### 5.2.2 Corner-Kick Header (Set-Piece Pathway, KD-13)

In-swinging corner from #5; centre-back attacks the near post.
Verifies that set-piece-derived crosses route through #10
mechanically identically to open-play crosses (no set-piece-specific
branch). FR-HE-016.

### 5.2.3 Free-Kick Header

Wide free kick delivered to the far post; striker meets it on the
volley with a downward header. Same KD-13 pathway as 5.2.2.

### 5.2.4 Goalkeeper Headed Clearance (GK Pipeline, KD-7)

GK leaves the line on a long ball, heads it clear over an
approaching striker. Verifies FR-HE-009: the GK head contact
executes the #10 pipeline; no GK-specific physics branch is taken.

### 5.2.5 Contested 2-Way Duel (Defender vs. Striker)

Both agents commit to a header on the same incoming cross. Verifies
§3.7 algorithm: winner emits `HeaderExecutedEvent`; loser receives
`disturbanceFactor` and either emits a poor-quality executed event
or, if `contactQualityScalar < MIN_CONTACT_QUALITY`, emits
`HeaderAttemptFailedEvent`.

### 5.2.6 Contested 3-Way Duel

Two strikers + one defender at the same contact frame. Verifies
multi-way semantics under v0.2 M-5 alignment (F-04, §3.7 step 5
uniform with step 4): winner emits full-quality
`HeaderExecutedEvent`; each loser emits either a disturbed
`HeaderExecutedEvent` (if `q' ≥ MIN_CONTACT_QUALITY`) or
`HeaderAttemptFailedEvent` with `failureCause = DisturbedInDuel`
(if `q' < MIN_CONTACT_QUALITY`). Construct two sub-scenarios:
(a) tight 3-way (small `baseScore` gap → small disturbance →
losers emit disturbed executed events); (b) lopsided 3-way (large
gap → saturated disturbance → losers fail).

### 5.2.7 Mistimed Jump → Failed Attempt → No Ball State Change

Striker commits early; jump apex reached before ball arrival;
`timingOffsetMs > MAX_LATE_TOLERANCE_MS` at re-evaluated contact
frame. Verifies F-01 + KD-17 intent-staleness re-validation: ball
trajectory is unchanged across the affected ticks.

### 5.2.8 Own-Goal-Shape Flag → Event System #17 Adjudication (Mock)

Defender heads the ball back toward own goal; `HeaderExecutedEvent`
is published with `ownGoalShapedTrajectory = true`. Mock Event
System receives the event and asserts that goal adjudication is
its responsibility, not #10's.

### 5.2.9 Deterministic Replay

1000-tick scenario including 12 headers, 2 contested duels, 1
near-tie tiebreak invocation. Run three times with the same seed;
assert byte-identical `HeaderExecutedEvent` sequences across runs.
Verifies KD-10 / FR-HE-008.

---

## 5.3 Validation Scenarios (Match-Feel)

These scenarios validate match-level feel against published empirical
baselines. They emit telemetry distributions that designers review;
they are not pass/fail unit tests.

### 5.3.1 22-Agent Match Peak: Header Frequency

10-minute simulated match segment with all 22 agents. Expected
~3 headers in the segment (linearly scaled from the ~28-header
full-match baseline established by Kirkendall & Garrett 2001 and
modern Opta / StatsBomb match-level statistics, per pass-1 M-3
recalibration; see §8.3).

Designer-set telemetry-distribution targets (illustrative; v0.2
L-6 framing — these shares model a population that includes
systematic mistiming from Decision Tree #8 commit-tick choice
and from upstream perception-tick variance, not noise alone from
`TIMING_JITTER_SIGMA_MS = 8` which would by itself put >99% of
attempts into `OnTime`. Empirical baseline for the share split
is a designer target pending Stage 0 calibration; no published
academic reference for header timing-label distribution is
currently catalogued in §8.3):

| Telemetry label | Expected share |
|-----------------|----------------|
| `OnTime`        | ≈55 % |
| `Early`         | ≈20 % |
| `Late`          | ≈25 % |

Higher `Late` than `Early` mirrors the asymmetric tolerance per
pass-1 H-1.

### 5.3.2 Corner-Routine A/B: Heading Attribute Sensitivity

Same delivery (in-swinging corner, identical seed, identical
defender geometry). Two striker profiles: Heading 75 vs. Heading 90,
all other attributes held constant. Expected: measurable outcome
divergence — header from the Heading-90 striker has tighter
point-error distribution, higher mean `contactQualityScalar`, and
higher mean `outgoingSpeed`. Verifies that the `Heading` attribute
materially affects outcomes (KD-4, §3.4 `headingAttrScale`, §3.5
`POWER_K_HEADING`).

### 5.3.3 Fatigue Gradient

Same striker, same delivery, same contact-quality target. Run at
`fatigue = 0.0` and `fatigue = 1.0`. Expected: ~12 % reduction in
mean `outgoingSpeed` at full fatigue versus rested (validation
against KD-9 fatigue convention + `POWER_FATIGUE_COEFF`).

---

## 5.4 Cross-Spec Conformance Tests

CI-time gates enforced before merge to main.

### 5.4.1 No `HeaderType` / `HeaderClass` Symbol in `src/`

`grep -r "HeaderType\|HeaderClass\|HeaderStyle" src/` returns zero
matches. Verifies KD-1 / FR-HE-003. Gate is #10-specific (the symbol
grep targets are owned here) and lives in #10's test plan.

### 5.4.2 Constant-Tag Verification

Every constant in `HeadingConstants.cs` has a source-tag comment
matching one of `{[GT], [EST], [FIXED], [DERIVED], [CROSS],
[CROSS-PENDING]}`. Verifies KD-11 / FR-HE-014. Programmatic
verification per Code Standards #20 §3.x constant-tag grep.

### 5.4.3 RNG Routing Discipline

Every RNG call in `src/Gameplay/Heading/` uses
`DeterministicRng.NextFloat(drawSiteId)` or
`DeterministicRng.NextGaussian(drawSiteId)`. No `System.Random`,
`Random.Range`, or `new Random()` symbols. Verifies KD-10 /
FR-HE-008.

(Former §5.4.4 "no `System.Random` / `DateTime.Now`" project-wide
gate **removed per pass-1 M-9** — that gate is owned by Testing
Strategy #19 §3.x / Code Standards #20 §3.x; re-asserting here
would duplicate the authoritative gate and risk drift.)

---

## 5.5 Test-Coverage Targets

| Section | Minimum line-coverage | Minimum branch-coverage |
|---------|----------------------|-------------------------|
| §3.2 Eligibility | 100 % | 95 % |
| §3.3 JumpReach | 100 % | 90 % |
| §3.4 Contact-Quality | 100 % | 95 % |
| §3.5 Power & Launch-Angle | 95 % | 90 % |
| §3.6 Spin Transfer | 95 % | 90 % |
| §3.7 Duel Resolution | 100 % | 95 % |
| §3.8 Own-Goal Flag | 100 % | 95 % |
| §3.9 Failed-Attempt | 100 % | 100 % |

Coverage tooling per Testing Strategy #19 §3.x.

---

## 5.6 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1       | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: §5.1.5 reversal-boundary test rewritten for v0.2 H-1 single-reversal formula + Heading-direction monotonicity assertion added (H-2); §5.1.6 tiebreak test "exactly once" → "exactly N" (M-6); §5.1.7 split into Group A (F-01..F-04, failed-event) and Group B (F-05..F-07, continue-with-modification) (M-7); §5.2.6 3-way duel rewritten for v0.2 M-5 uniform loser semantics; §5.3.1 telemetry-shares framing made explicit re. systematic vs. noise components (L-6). | pending |
