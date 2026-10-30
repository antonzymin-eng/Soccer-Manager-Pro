# Goalkeeper Mechanics Specification #11 — Section 5: Test Plan

**Created:** May 16, 2026
**Version:** 0.3
**Last Updated:** October 29, 2026 (v0.3 — ERR-011-021: W8 C hand-control tests; six-second tests retired)
**Status:** DRAFT
**Purpose:** Specify unit tests, integration tests, validation
scenarios, and cross-spec conformance tests for Goalkeeper
Mechanics #11. Test framework conventions follow Testing Strategy
#19 §3.

---

## 5.1 Unit Tests

One sub-section per §3 algorithm; ~6–10 test cases each.

### 5.1.1 State machine (§3.1)

- T-5.1.1.1 Every transition exercised (every row of §3.1.1; 29 rows at section 3 v0.24, including the `HandsOnBall → Recovering` teardown row).
- T-5.1.1.2 Cycle detection: `Resting → Set → Anticipate → Diving
  → Airborne → Recovering → Set → ...` round-trip.
- T-5.1.1.3 Deterministic ordering under sequential `ShotExecutedEvent`
  + `RushIntent` arrival in the same tick.
- T-5.1.1.4 **Retired October 29, 2026 (ERR-011-021).** Former test:
  `HandsOnBall → HandsOnBall` forced release at `GK_HOLD_MAX_TICKS = 60`
  ticks. The six-second row no longer exists; the hand-control timeout is
  covered by §5.1.10.
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
- T-5.1.8.2 `GoalkeeperDistributionRequest` mapping correctness (§3.8.3):
  Throw / Roll / Kick map one-to-one to #5 `GoalkeeperDeliveryVariant`
  Throw / Roll / Kick. *(Corrected October 29, 2026: the former text named the
  phantom `PassIntent` LowDriven/GroundRoll/Lofted mapping removed by ERR-011-015.)*
- T-5.1.8.3 F-05 missing-receiver fallback: `targetReceiverId`
  cleared; `targetPoint` retained; warning telemetry emitted.
- T-5.1.8.4 F-09 out-of-bounds clamp: `targetPoint` clamped to
  `[0, PITCH_LENGTH_M] × [0, PITCH_WIDTH_M]`.
- T-5.1.8.5 **Retired October 29, 2026 (ERR-011-021).** Former test: a
  `HandsOnBall` duration `≥ GK_HOLD_MAX_TICKS` triggers `Distributing` without
  `DistributeIntent` (default ROLL to the nearest defender). FR-GK-043 is
  retired; a hand-control deadline never manufactures a distribution
  (§3.8.3, T-5.1.10.2).

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

### 5.1.10 Hand-control clock and Law-12 offence (§3.8.5; ERR-011-021)

Deterministic contract tests, each mirrored for both defended ends where geometry is involved:

- T-5.1.10.1 The 60 Hz claim frame is recorded at each of the three hand-claim sites (aerial/cross
  claim, dive-save catch, smother); elapsed 0 / 479 / 480 / 481; a claim landing late within its
  six-frame tactical cell is still timed from its exact frame. Oracle: no corner at elapsed 0, 479 or
  480; a corner at 481 when all five predicates hold. The stored start frame is a 64-bit
  `long` set by #11 and read-only to the engine; the frame domain feeding it is the engine's `int`
  frame (about 414 days of play, an accepted Stage-0 limit), and storing the frame never narrows.
- T-5.1.10.2 A CONTACT at elapsed 480 is legal; hand control held into elapsed 481 is sanctioned
  with no kick, no CONTACT, no `Completed` terminal and no `DistributionExecutedEvent`. (a) Production
  case: a sanction with no pending #5 request produces zero terminals and zero feedback. (b) Defensive
  case: if a #5 request is still in flight, the sanction cancels it, `ApplyRestart` leaves exactly one
  `Cancelled` terminal, and the following frame's Resolve drains it with no #11 state change (the keeper
  is already `Recovering`). The §3.8.4 admission predicate makes a request in flight at elapsed ≥ 481
  unreachable in production, so (b) builds that state through a `TestOnly_` seam or a crafted restore.
  A pending distribution reaching CONTACT at elapsed 480 is not cancelled by any pre-executor guard.
- T-5.1.10.3 Controlled feet possession never becomes a corner, and the feet guard neither inherits
  nor leaks the hand clock. The engine stall guard is feet-only at C, with no hand exception; it is a
  non-law guard. (a) A hand episode ends at CONTACT; more than 481 frames after the original claim the
  same keeper holds the ball at its feet: no corner, and `handControlStartFrame` reads the inactive
  sentinel. (b) With the feet guard suppressed through a `TestOnly_` seam, feet possession run to
  elapsed ≥ 481 (measured from the original hand claim and from the hand-to-feet transition): no
  corner. (c) With the guard enabled, it fires at 360 frames of feet possession and publishes no
  `RestartAwardedEvent`; its elapsed state resets at the hand-to-feet transition (a 300-frame hand
  hold followed by feet possession does not fire 60 frames into the feet possession).
- T-5.1.10.4 The full five-part predicate (one case failing each predicate in turn) in both `HandsOnBall` and controlled `Distributing`,
  including retained-possession reject/cancel and an in-progress WINDUP.
- T-5.1.10.5 Only a successful claim sets `handControlStartFrame`. Oracle: across N tactical ticks of
  holding (`HandsOnBall → HandsOnBall` at 10 Hz), and across a retained #5 reject or a retained cancel
  (`Distributing → HandsOnBall`), `handControlStartFrame` is unchanged. Bounce/toss has no Stage-0
  producer and is not a Stage-0 test; the future punt/drop (stop the clock at its earlier hand release,
  permit a later kick without a phantom corner) is deferred to its own specification and is not a
  Stage-0 test either. Actual release, real loss, restart, goal or foul tears the episode down once;
  stale intent cannot cause a later restart. All current Roll/Throw/Kick variants end hand control at
  their atomic CONTACT.
- T-5.1.10.6 Predicate 4: hand episodes held inside the area, on each boundary face/corner and with
  goal-line overhang (§3.1.4 geometry) all time out to a corner at 481; so does §3.1.4's body-outside
  hold after a legal inside catch, held still or moved back toward the area.
- T-5.1.10.7 Corner recipient, the sanctioned keeper's goal line, the nearest Y side, the exact
  `y = 34` high-Y tie, shared restart placement/taker/event, and exactly one restart.
- T-5.1.10.8 A §3.1.4 outside contact or outward carry gives its DFK, never an eight-second corner,
  including on frames 480/481; own-release re-handling (§3.1.3) stays distinct. The body-outside hold
  is bounded by the corner after the guard split, with teardown and save/restore, and shows no
  repeated free-claim loop (a released or sanctioned keeper cannot re-claim the same ball to start a
  fresh clock without an intervening other-player touch, §3.1.3, or a restart). The landed D-07 regression cases stay green.
- T-5.1.10.9 Save/restore at the claim frame, before 480, at 480, before 481, and mid-WINDUP or
  cancellation gives identical continuation, event ordering and restart outcome. `ResetSlot` on a slot
  with a live episode (substitute keeper) clears the start frame to the inactive sentinel and produces
  no corner or kick; a slot refresh that does not call it preserves the episode and its clock.
  Corrupt restore: a body with `HandsOnBall` or controlled `Distributing` and an inactive or future
  `handControlStartFrame`, or with an active clock and any other state, is rejected with
  `InvalidOperationException`; so is a body whose pending #5 request has
  `FrameNumber + WindupFrames > handControlStartFrame + 480`.
- T-5.1.10.10 No new RNG draw, default-policy switch, duplicate CONTACT or bypass of the existing #5
  completion/cancellation feedback.
- T-5.1.10.11 A first or repeated offence produces no card and no card event at Stage 0; repeated-offence
  bookings are deferred (§7.3).
- T-5.1.10.12 Adversarial same-frame ordering against a prior whistle, §3.1.4 handling (including its
  post-reattachment re-check), a ball boundary crossing, possession loss, a successful #5 CONTACT, and
  replay/restore. Expected results, each mirrored for both ends: (a) a same-frame captured
  tackle/collision foul candidate at elapsed 481 gives the corner only; the foul, its decided flag and
  any card are dropped with no card-severity draw, and Resolve ends that frame after the event and
  possession publication. (b) The held ball wholly out of play at elapsed 481 (for example team 0 at
  `(−0.30, 34.0)` m, team 1 at `(105.30, 34.0)` m) gives the boundary/goal restart and no §3.8.5 corner;
  an overhang inside `r` still gives the corner. (c) A §3.1.4 DFK on the frame gives the DFK and no
  corner. (d) A successful #5 CONTACT at elapsed 480 is legal and gives no corner; a possession loss
  earlier in the frame gives no corner. (e) Replay and restore of each case reproduce the same outcome.
- T-5.1.10.13 §3.8.4 admission/retry budget (`candidateContactFrame = currentFrame + WindupFrames`,
  CONTACT asserted on exactly that frame): live configured windups and all six #21 policy delays
  satisfy the first-attempt bound (`210 + 84 = 294 ≤ 480`), and the test fails if any configured policy has
  `6·D + W > GK_HAND_CONTROL_MAX_FRAMES` (such a first attempt would be refused and concede a corner); a retry reaching CONTACT at elapsed 480
  is accepted and one at 481 refused (claim 1,000, heartbeat 1,398: `WindupFrames` 82 accepted, 83
  and the 84-frame Kick refused); repeated rejection until the budget closes; and §3.8.4 case (a), a
  retained cancellation at CONTACT about 294 frames after the claim with the heartbeat at most 6 frames
  later and an 84-frame windup, which reaches CONTACT at about elapsed `294 + 6 + 84 = 384 ≤ 480` and
  is accepted.

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
  accepts a valid `GoalkeeperDistributionRequest` and executes one
  CONTACT (§3.8.3). *(Corrected October 29, 2026: the former text named
  the phantom `PassIntent` / `ConsumePassIntent` API removed by ERR-011-015.)*
- T-5.2.10 Deterministic replay: a 1000-tick scenario producing
  identical `SaveAttemptedEvent` and `DistributionExecutedEvent`
  sequences across 10 runs with the same RNG seed.
- T-5.2.11 #12 baseline-slot ratification: with the #12 v1.0.x
  patch applied (post-`IN REVIEW` transition), GK reads baseline
  from #12's `GetFormationSlot(keeperEntityId)` via §3.3.0.1 and
  applies KD-13 reactive radius correctly. The baseline read and the
  `Recovering → Set` cooldown are locked at composition level by
  `GoalkeeperBaselineSlotTests` (ERR-011-018); the reactive-radius
  micro-adjustment still has no production consumer.

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
| 0.2 | September 27, 2026 | ERR-011-018 | T-5.2.11 names the real #12 surface and records what is now locked (baseline read, cooldown) versus still unconsumed (reactive-radius clamp). | spec + code, same commit |
| 0.3 | October 29, 2026 | W8 C spec application / ERR-011-021 | T-5.1.1.4 and T-5.1.8.5 (six-second forced release) retired with their text preserved; new §5.1.10 (T-5.1.10.1–13) locks the §3.8.5 eight-second offence and the §3.8.4 retry budget. T-5.1.8.2 and T-5.2.9 reconciled with the approved W8 B #5 goalkeeper-request seam (stale phantom `PassIntent` text from before ERR-011-015). T-5.1.1.1's stale transition count corrected. Tests specified, not yet implemented. Adversarial review round 1: T-5.1.1.1 recounted to 29 rows (new `HandsOnBall → Recovering` row); T-5.1.10.3 split into stale-clock, suppressed-guard and 360-frame cases; T-5.1.10.4/.6 five-part predicate; T-5.1.10.8/.9/.13 extended; T-5.1.10.11 states the no-card Stage-0 behaviour; T-5.1.10.12 given expected outcomes. Review fix: T-5.1.10.13 uses CONTACT = commit frame + `WindupFrames` (no `+ 1`, #11 §3.8.4): first attempt 294, case (a) about 384, boundary pair `WindupFrames` 82 accepted / 83 refused at heartbeat 1,398. Review round 2: T-5.1.10.1 int frame domain; T-5.1.10.2 one drained `Cancelled` terminal and no pre-executor guard cancel at elapsed 480; T-5.1.10.9 corrupt-restore case for the live-iff invariant. Review round 3: T-5.1.10.1 elapsed oracle; T-5.1.10.2 production case (no pending request, zero terminals) and the `TestOnly_`/crafted-restore defensive case; T-5.1.10.5 tactical-tick and retained reject/cancel oracle, bounce/toss and punt/drop not Stage-0 tests; T-5.1.10.9 pending-request frame corrupt-restore case; T-5.1.10.13 `6·D + W` failure condition and case (a) reworded. | W8 C contract v0.5 |
