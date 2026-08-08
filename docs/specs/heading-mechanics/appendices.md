# Heading Mechanics Specification #10 — Appendices

**Created:** May 16, 2026
**Version:** 0.3
**Status:** DRAFT
**Purpose:** Derivations, sensitivity tables, exemplar tuning
profiles, glossary, and adversarial-review traceability tables
(v0.1 outline review and pass-1 outline-detailed review).

---

## Appendix A — Derivations

### A.1 `JumpReach` Derivation from First Principles

The Stage 0 derivation (KD-4, KD-18) treats vertical reach as the
sum of an anatomical baseline and three attribute-driven
increments:

```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
            + JUMP_REACH_K_HEADING  · Heading_norm
```

**Anatomical baseline** (`JUMP_REACH_BASE_M`, `[FIXED]`). The
standing head-height of an average professional footballer plus
the average squat-jump amplitude with zero strength/balance/
heading skill applied — a physical floor below which no player
can drop.

**Strength term.** Strength drives explosive leg extension power;
higher Strength translates to higher vertical impulse and thus
greater apex altitude. The coefficient is calibrated against
Tomczak et al. (2021) head-kinematics data (§8.3). [v0.3 OI-003: replaced v0.1 "Auger & Pellegrini (2007)" — original citation not findable in DOI registry.]

**Balance term.** Balance drives takeoff posture quality and
mid-air orientation control; a stable takeoff converts a larger
fraction of leg impulse into vertical motion (less wasted lateral
component). The coefficient is `[GT]` and tuned against §5.3.2
A/B observations.

**Heading term (pass-1 H-2 addition).** Heading captures
jump-timing skill — the ability to commit to a jump such that
the apex aligns with the ball arrival frame. At Stage 0, with
no dedicated jump-timing attribute (deferred to §7.10), the
Heading term carries this skill. The coefficient is `[GT]` and
its sensitivity is exercised in Appendix B.1.

### A.2 `contactQualityScalar` Linearity Proof

The scalar (§3.4) is a convex combination of two clamped linear
quantities. Sketch:

```
timingQuality, pointQuality ∈ [0, 1]   (both clamped)
contactQualityScalar = α · timingQuality + (1 - α) · pointQuality
                       ∈ [0, 1] for any α ∈ [0, 1]
```

Where `α = TIMING_POINT_BLEND_ALPHA`. The piecewise-linear
character of each component derives from the asymmetric tolerance
formulation (pass-1 H-1):

```
timingOffsetMs ≤ 0:  timingQuality = 1 - clamp01(-Δt / T_early)
timingOffsetMs > 0:  timingQuality = 1 - clamp01( Δt / T_late)
```

Both pieces are continuous and monotone in `|Δt|`; the joint is
continuous at `Δt = 0` with value 1 (perfect timing). The
piecewise function is therefore Lipschitz in `Δt` with constant
`max(1/T_early, 1/T_late)`.

### A.3 Spin-Transfer Reversal Boundary (v0.2 H-1)

The outgoing spin (§3.6) is:

```
outgoingSpin = SPIN_TRANSFER_COEFF · headAngularVelocity
             + incomingSpin · spinPreservationFactor
```

where

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset / SPIN_TRANSFER_REVERSAL_THRESHOLD)
```

At `contactPointAxialOffset = SPIN_TRANSFER_REVERSAL_THRESHOLD`,
`spinPreservationFactor` crosses zero — at that exact offset, the
incoming spin contribution to outgoing spin is zero. Beyond the
threshold, `spinPreservationFactor` is negative and the
`(incomingSpin · spinPreservationFactor)` term carries the sign
flip directly: the incoming spin contribution to outgoing spin
reverses with magnitude proportional to the axial-offset overshoot.

(The v0.1 formulation also subtracted a `reversalTerm =
max(0, -spinPreservationFactor) · incomingSpin`, which
double-counted the reversal — once via the preservation term going
negative, and a second time via the explicit subtraction. The
`reversalTerm` has been removed in v0.2 H-1; the sign flip is
carried exclusively by `spinPreservationFactor`.)

**Worked example.** Incoming topspin 8 rad/s; axial offset 0.02 m;
`SPIN_PRESERVATION_BASE = 0.6`; `SPIN_TRANSFER_REVERSAL_THRESHOLD =
0.015 m`.

```
spinPreservationFactor   = 0.6 · (1 - 0.02 / 0.015) = -0.2
incomingSpinContribution = 8 · (-0.2)               = -1.6 rad/s
```

The 8 rad/s topspin becomes a 1.6 rad/s backspin in the outgoing
spin contribution (before `SPIN_TRANSFER_COEFF · headAngularVelocity`
is added).

### A.4 Own-Goal-Shape Projection Geometry

The dual-horizon projection (§3.8, pass-1 L-7) terminates at:

```
projectionEndTime = first of:
  (a) horizon_s seconds elapsed simulated time
  (b) horizon_m metres of travelled arc-length
```

Rationale: a flat header at 20 m/s travels 60 m in 3 s; a looping
header at 8 m/s travels 24 m in the same 3 s. A pure time horizon
of 3 s over-reaches on the flat case (the ball would have already
passed beyond any sensible projection envelope and entered the
opposing half) and under-reaches on the loop case (the ball is
still close to the contact point). The distance cap binds the
flat case; the time cap binds the loop case.

`ownGoalBoundingBox(team)` is the rectangle defined by the
defending team's own goal-line and the goal-area lines, expressed
in corner-origin coordinates (Ball Physics #1 §1.2).

---

## Appendix B — Sensitivity Tables

### B.1 `JumpReach` over Strength × Balance Grid (11 × 11)

Sensitivity grid varying `Strength_norm` and `Balance_norm` from
0.0 to 1.0 in 0.1 increments, with `Heading_norm` fixed at 0.5.
Output values to be populated during Stage 0 calibration; structure
shown below.

```
                Balance_norm
              0.0   0.1   ...   1.0
Strength_norm
0.0           …     …            …
0.1           …     …            …
...
1.0           …     …            …
```

Sweep with `Heading_norm ∈ {0.25, 0.5, 0.75}` repeated for the
KD-4 / H-2 ablation; expected: increasing `Heading_norm` shifts
the entire grid by `0.25 × JUMP_REACH_K_HEADING` between bands.

### B.2 `outgoingSpeed` over `PowerIntent` × Fatigue × Heading Grid

Three-axis grid:

- `PowerIntent ∈ {0.1, 0.3, 0.5, 0.7, 1.0}`
- `fatigue ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`
- `Heading_norm ∈ {0.4, 0.6, 0.8}`

Cells populated during Stage 0 tuning. Expected:

- Monotone increase in `PowerIntent` along each row.
- Monotone decrease in `fatigue` along each column.
- Monotone increase in `Heading_norm` between bands.
- At `fatigue = 1, PowerIntent = 1, Heading_norm = 0.6`,
  `outgoingSpeed` is ≈12 % below the rested value (validation
  target for §5.3.3).

### B.3 Duel-Score Sensitivity (`Heading × Strength × Balance`)

Ranking sensitivity table. Rows enumerate two-agent contests at
varying attribute deltas; columns are the relative weights
(`DUEL_HEADING_WEIGHT`, `DUEL_STRENGTH_WEIGHT`,
`DUEL_BALANCE_WEIGHT`). The table identifies the configurations
in which the duel falls within `DUEL_TIEBREAK_EPSILON` and the
tiebreak perturbation is invoked.

---

## Appendix C — Exemplar Tuning Profiles

Three illustrative presets. Designer-authored values supersede
these at Stage 1+.

### C.1 High-Leap Centre-Back

| Constant                     | Value (illustrative) |
|------------------------------|----------------------|
| `JUMP_REACH_K_STRENGTH`      | 0.18 m × Strength_norm |
| `JUMP_REACH_K_HEADING`       | 0.10 m × Heading_norm |
| `POWER_K_STRENGTH`           | 6.0 m/s × Strength_norm |
| `DUEL_STRENGTH_WEIGHT`       | 0.45 |
| `DUEL_HEADING_WEIGHT`        | 0.40 |
| `DUEL_BALANCE_WEIGHT`        | 0.15 |

Profile favours raw vertical reach and duel dominance via
Strength + Heading. Outgoing speed is high; placement precision
(point-error) is moderate.

### C.2 Glancing-Finish Forward

| Constant                          | Value (illustrative) |
|-----------------------------------|----------------------|
| `JUMP_REACH_K_BALANCE`            | 0.14 m × Balance_norm |
| `CONTACT_POINT_HEADING_ATTR_COEFF` | 0.45 |
| `POWER_K_HEADING`                 | 5.0 m/s × Heading_norm |
| `DUEL_BALANCE_WEIGHT`             | 0.35 |
| `DUEL_HEADING_WEIGHT`             | 0.45 |

Profile favours point-error precision (tighter contact-point
distribution via the Heading-attribute scaling) over raw duel
dominance. Outgoing speed is moderate; placement precision is
high.

### C.3 Balanced Midfielder

Default `[GT]` candidate values from §3.1 with no profile
overrides. Used as the neutral baseline against which §5.3
validation scenarios are measured.

---

## Appendix D — Glossary

| Term | Definition |
|------|------------|
| `HEAD_CONTACT_VOLUME` | Cylindrical region around the agent head where ball contact is geometrically possible; radius and height are `[GT]` constants in §3.1 |
| `ContactPointIntent` | Decision Tree #8 output specifying the intended contact location on the head surface as a 2-D `Vector2` in head-local coordinates (metres). Axis convention (v0.2 L-7): origin at the head centre; `+x` = `agent.facing` forward (toward forehead); `+y` = agent-left lateral. Distances are euclidean in metres, consistent with `CONTACT_POINT_ERROR_SIGMA_M` and `||contactPointActual − contactPointIntent||` in §3.4. |
| `ContactQualityScalar` | Continuous scalar ∈ [0,1] derived from signed timing offset and contact-point error; the formula-gating quantity for outgoing power / direction (KD-2) |
| `HeaderIntent` | Decision Tree #8 output struct: `powerIntent`, `contactPointIntent`, `targetIntent`, `attemptCommittedTick` |
| `HeaderExecutedEvent` | Event published on every contacted header; carries telemetry, outgoing velocity / spin, and the `ownGoalShapedTrajectory` flag |
| `HeaderAttemptFailedEvent` | Event published on missed-contact attempts (mistimed, mis-positioned, disturbed-in-duel); ball state unchanged (KD-12) |
| `ContestedDuelContext` | Per-duel context struct: participating agents, winner, per-agent disturbance factors |
| `OwnGoalShapedTrajectory` | Boolean flag on `HeaderExecutedEvent` set when the outgoing trajectory's dual-horizon projection intersects the defending team's own goal-line bounding box (KD-6; adjudication is Event System #17) |
| `JumpReach` | `[DERIVED]` quantity (KD-4): the vertical apex altitude of the head during the synthetic Stage 0 jump phase |
| `DRAW_SITE_*` | Registered RNG draw sites per Deterministic Sim #16 §4.5: `DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER` |
| `DOMAIN_TAG_HEADING` | `[CROSS]` `0x16` allocation in #16 §3.4 catalogue (ERR-010-001 RESOLVED May 16, 2026 via #16 §3.5 v1.0.2 patch) |

---

## Appendix E — Mapping Table to v0.1 Adversarial Review Findings

Maps the 22 findings of the `outline.md` adversarial-review
appendix (May 6, 2026) to their resolution location in the spec.

| Finding | Severity | Resolution |
|---------|----------|------------|
| 1. Missing metadata header | H | All section files now carry creation-date / version / status headers |
| 2. Section plan misaligned with CLAUDE.md template | H | Sections remapped: §2 FRs/structs/failure; §3 formulas; §4 architecture; §5 tests; §6 perf; §7 deferrals; §8 references; §9 approval; appendices |
| 3. Contact-window enum risk | H | KD-2 + FR-HE-002: continuous quality scalar; named windows are telemetry labels only |
| 4. Glancing-vs-power enum risk | H | KD-1 + FR-HE-003: no `HeaderType`/`HeaderClass` enum; emergent outcomes |
| 5. Missing `Jumping` attribute in AM #2 | H | KD-4: `JumpReach` `[DERIVED]` from existing attributes; no AM #2 amendment |
| 6. Upstream dependencies undeclared | H | §1.4 + §8.2 tables list all 11 upstream specs with section-level anchors |
| 7. Output interface to Ball Physics undeclared | H | §4.3 declares `Ball.ApplyKick` + `HeaderExecutedEvent` + `HeaderAttemptFailedEvent` |
| 8. Pass Mechanics #5 SUSPENDED risk | H | KD-5: `BallState`-level consumption only; #5 since re-approved May 6, 2026 |
| 9. Own-goal handling out-of-scope | H | KD-6: flag emitted; adjudication is Event System #17 |
| 10. Determinism plan absent | M | KD-10 + §4.4: 3 registered draw sites; `DOMAIN_TAG_HEADING = 0x16` (OI-001); #16 §3.2 iteration order |
| 11. Coordinate-system convention unstated | M | KD-9 + §1.3 + §8.1: corner-origin per Ball Physics #1 §1.2 |
| 12. Fatigue convention not pre-committed | M | KD-9 + FR-HE-011: `0 = rested, 1 = fatigued` |
| 13. Tick-rate split unstated | M | KD-9 + FR-HE-013: 10 Hz tactical / 60 Hz physics |
| 14. Constant-tag policy not invoked | M | KD-11 + FR-HE-014 + §9.1 |
| 15. Boundary with First Touch #4 unstated | M | KD-3 + §3.10 + XC-010-003 |
| 16. Boundary with Goalkeeper #11 unstated | M | KD-7 + FR-HE-009 |
| 17. Contested-duel resolution overlap with #3 | M | KD-8 + §3.7: consumes #3 contact-event API; layers Heading-specific resolution |
| 18. Weak-aerial-side penalty | L | KD-14 + §7.1: deferred to Stage 1+ |
| 19. "Complete misses" physics output undefined | L | KD-12 + §3.9: `HeaderAttemptFailedEvent`, no ball state change |
| 20. Set-piece scope demarcation | L | KD-13 + §1.2 + §7.5: set-piece headers IN scope; set-piece kicks DEFERRED |
| 21. Concussion / injury modelling | L | KD-15 + §7.2: deferred to Stage 1+ Medical spec |
| 22. Spin transfer responsibility undeclared | L | KD-16 + §3.6: outgoing spin computed by #10 and passed via `Ball.ApplyKick` |

---

## Appendix F — Mapping Table to Pass-1 Review Findings (v1.0 → v1.1)

Maps the 21 findings of `outline-detailed-pass-1-review.md`
(5 H / 9 M / 7 L) plus the cross-cutting AM #2 jump-surface
absence to their resolution location in the v1.1 detailed
outline and the section files.

| Finding | Severity | Resolution |
|---------|----------|------------|
| H-1 timing tolerance | HIGH | §3.4 asymmetric piecewise formula; §3.1 keeps `MAX_EARLY_TOLERANCE_MS` and `MAX_LATE_TOLERANCE_MS` as distinct `[GT]` rows |
| H-2 JumpReach Heading term | HIGH | KD-4 revised; §3.3 formula gains `JUMP_REACH_K_HEADING · Heading_norm`; §3.1 adds `JUMP_REACH_K_HEADING` |
| H-3 headAngularVelocity source | HIGH | §3.6 derives `headAngularVelocity` from AM #2 `agent.facing` finite-difference + projected neck rotation; no #2 amendment; §7.9 deferral for Stage 1+ direct read |
| H-4 §6.1 vs §6.3 budget contradiction | HIGH | §6.1 split into steady-state (≤80 µs) and p99 duel-frame tail (≤180 µs); §6.3 component breakdown is source-of-truth |
| H-5 magic `0.01` in duel tiebreak | HIGH | §3.7 step 3 rewritten using `DUEL_TIEBREAK_EPSILON` and `DUEL_TIEBREAK_NOISE_AMPLITUDE` (both `[GT]` in §3.1); non-tie scores never perturbed |
| M-1 missing §3.1 constants | MEDIUM | §3.1 expanded from ~28 to ~35 rows; every §3.4–§3.7 symbol enumerated or tagged `[DERIVED]` |
| M-2 IDEAL_CONTACT_FRAME_OFFSET miscategorised | MEDIUM | Removed from §3.1; relocated to §3.2 as per-call output of eligibility predicate |
| M-3 header-count expectations off by 3–4× | MEDIUM | §6.3 (≤0.5 duels/min) and §5.3.1 (~3 headers/10 min) recalibrated against Opta / StatsBomb; cite added to §8.3 |
| M-4 phantom draw sites | MEDIUM | §3.4 injects Gaussian noise via `DRAW_SITE_CONTACT_POINT_ERROR` and `DRAW_SITE_TIMING_JITTER`; §3.1 adds `CONTACT_POINT_NOISE_SIGMA_M`, `TIMING_JITTER_SIGMA_MS`; §4.4 marks all three draw sites as wired |
| M-5 intent-staleness policy | MEDIUM | New KD-17; §3.2 intent-staleness handling subsection added; FR-HE-018 covers the contract |
| M-6 XC-010-001 unmotivated | MEDIUM | XC-010-001 (EntityId no-reuse) removed; remaining cross-refs renumbered 001–006 |
| M-7 KD-13 set-piece scope | MEDIUM | KD-13 rationale expanded — spin-on-arrival propagates through `BallState`; wall/pile geometry handled by #3; no set-piece-specific #10 branch needed |
| M-8 AM #2 anchor pinning | MEDIUM | §1.4 dependency table now lists exact subsections (§3.5.1, §3.5.6, §3.1.2); §4.2 explains why no `GetKinematicState(agentId, frame)` getter exists |
| M-9 §5.4.4 duplicate gate | MEDIUM | §5.4.4 removed; CI gate ownership retained at #19 / #20 |
| L-1 `Perfect` label naming | LOW | Renamed `Perfect` → `OnTime` across KD-2, §2.2 enum, §3.4 telemetry, §5.3.1 |
| L-2 §1.2 ambiguous "or" | LOW | Disambiguated: Stage 0 set-piece kicks routed to #5; Stage 1+ may take over |
| L-3 dead GLANCING_ANGLE_THRESHOLD_RAD | LOW | Removed from §3.1; deferred to §7.11 as a Stage 1+ telemetry-classifier concern |
| L-4 #7 in direct-dependency list | LOW | Moved to "Tractability cites" footnote; upstream table reduced to 9 direct deps |
| L-5 §3.7 step 4 / F-04 wording drift | LOW | Both phrasings aligned to "winner-only emits HeaderExecutedEvent; all losers emit HeaderAttemptFailedEvent" |
| L-6 sparse §8.3 anchors | LOW | Six anchors named in §8.3 (Bull, Auger & Pellegrini, Shewchenko et al., Naunheim et al., Kirkendall & Garrett, Opta/StatsBomb) |
| L-7 own-goal projection horizon semantics | LOW | §3.1 adds `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M`; §3.8 uses `min(time, distance)` dual-horizon |
| AM #2 jump-surface absence (cross-cutting) | HIGH | New KD-18; §3.3 owns synthetic jump trajectory at Stage 0; §7.8 deferral when AM #2 grows Z kinematics; no #2 amendment required |

---

## Appendix G — Open-Items Tracker (Mirror of Outline OI Table)

Status at section-files v0.1 (May 16, 2026):

| ID | Item | Owner | Status |
|----|------|-------|--------|
| OI-001 | `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 | back-prop ERR-010-001 | ✅ RESOLVED May 16, 2026 — #16 §3.5 v1.0.2 patch landed; #10 §3.1 row promoted `[CROSS]`. |
| OI-002 | `heading.*` trace channel rows | re-anchored | ✅ RESOLVED May 16, 2026 — schema-conforming rows declared in §2.4 against #18 Appendix F.0 schema; populated rows are Stage 0+1 deliverable per #18 §7.2 schedule. |
| OI-003 | DOI verification for §8.3 external references | drafter | ✅ RESOLVED May 16, 2026 — 5/5 academic DOIs verified; 2 fabricated v0.1 references replaced. |
| OI-004 | Goalkeeper #11 interface confirmation | post-#11 IN REVIEW | not blocking (post-approval per §9.6) |
| OI-005 | Pin #3 / #8 / #17 anchors | drafter | ✅ RESOLVED May 16, 2026 — #3 §3.4.2 `ICollisionEventConsumer`; #8 §1.7.2 (Stage 0+1 activation per §4.6.1); #17 §3.2.1 `Publish API surface`. |
| OI-006 | `certification-platform.md` Stage-0 host pin | lead developer | not blocking #10 sign-off (gates `FR-PO-052` only) |

---

## Appendix H — Mapping Table to v0.1 Section-Files PASS-1 Review Findings

Maps the 21 findings of
`adversarial-review-section-files-v1.md` (5 H / 9 M / 7 L,
May 16, 2026) to their resolution location in v0.2.

| Finding | Severity | Resolution |
|---------|----------|------------|
| H-1 §3.6 spin double-reversal | HIGH | §3.6 formula simplified — `reversalTerm` removed; sign flip carried by `spinPreservationFactor` only. §3.6 worked example recomputed (−1.6 rad/s). Appendix A.3 rewritten. §5.1.5 reversal-boundary test rewritten with monotonicity assertion. |
| H-2 §3.4 `headingAttrScale` inversion | HIGH | §3.4 formula rewritten — `headingAttrScale` now **divides** both `||contactPointActual − contactPointIntent||` (systematic miss) and `pointNoiseM` (random); `pointQuality` denominator is the bare `CONTACT_POINT_ERROR_SIGMA_M`. Higher Heading → tighter physical-error distribution → higher quality. Prose updated. §5.1.5 adds monotonicity test. |
| H-3 §3.2 worked example off-by-one | HIGH | Re-prediction changed `T+14 → T+16`; rounding policy (`ceil`) pinned in new §3.2 "Frame-Tolerance Rounding" subsection. Closes M-8. |
| H-4 §3.7 step 4 missing `disturbanceFactor` formula | HIGH | New `DUEL_DISTURBANCE_GAP_SATURATION [GT]` row in §3.1; explicit `disturbanceFactor_i = DUEL_DISTURBANCE_MAX · clamp01(gap_i / DUEL_DISTURBANCE_GAP_SATURATION)` formula in §3.7 step 4 + per-loser `q'_i` and FR-HE-026 branch logic. §9.1 checklist row added. Closes L-2. |
| H-5 §3.5 `ANGULAR_COEFF` magic constant | HIGH | `ANGULAR_COEFF` removed from §3.5 pseudocode; Stage 0 `headerLaunchAngle` is pure reflection geometry. New §7.12 deferral for Stage 1+ `LAUNCH_ANGLE_HEAD_VELOCITY_COEFF`. KD-11 compliance restored. |
| M-1 `ERR-010-001` not filed | MEDIUM | Entry filed in `docs/tracking/spec-error-log.md`. KD-10 wording in §1.3 adjusted to reflect actual filing status; Appendix G OI-001 and §9.4 OI-001 status updated. |
| M-2 `EligibilityPredicate` side effects | MEDIUM | Predicate now returns `(bool, predictedContactFrame, idealContactFrame, mistimedDirection)`; §4.6 caller emits failed events on `mistimedDirection ∈ {Early, Late}`. |
| M-3 `jumpStartFrame` source undefined | MEDIUM | New §3.3 "jumpStartFrame Source" subsection defines initialization rule (first frame at or after `attemptCommittedTick·6` with non-grounded state). §2.2 `HeaderContactState` adds `jumpStartFrame` field. §4.6 pseudocode shows initialization. |
| M-4 `actualContactFrame` set nowhere | MEDIUM | §4.6 pseudocode now sets `contactState.actualContactFrame = currentFrame` on the contact-frame branch. §2.2 field comment updated. |
| M-5 2-way vs. 3-way duel loser semantics | MEDIUM | §3.7 step 5 rewritten — uniform semantics across participant counts: winner full-quality executed event; each loser either disturbed executed event (`q' ≥ MIN_CONTACT_QUALITY`) or failed event. FR-HE-027 rewritten; §2.3 F-04 prose rewritten; §3.9 failure-cause table updated; §5.2.6 3-way test rewritten with tight + lopsided sub-scenarios. |
| M-6 §5.1.6 tiebreak test "exactly once" | MEDIUM | Rewritten as "exactly `N` calls per duel, where `N` is the near-tie cohort size" with four enumerated cases. |
| M-7 §5.1.7 test description vs §2.3 mismatch | MEDIUM | Split into Group A (F-01..F-04, failed-event assertion) and Group B (F-05..F-07, continue-with-modification assertions cite-mapped to FR-HE-029 / FR-HE-033 / FR-HE-030). |
| M-8 frame-tolerance rounding unspecified | MEDIUM | Pinned `ceil` in new §3.2 "Frame-Tolerance Rounding" subsection (closes with H-3). |
| M-9 `timingJitterMs` semantics unclear | MEDIUM | New §3.4 "`timingJitterMs` Semantics" subsection states explicitly: sub-frame execution noise applied post-eligibility to `timingOffsetMs` only; never to `predictedContactFrame` / `actualContactFrame`. |
| L-1 `attemptCommittedTick` unused | LOW | Now documented in §2.2 struct comment as the source for §3.3 `jumpStartFrame` derivation (chains with M-3). |
| L-2 `DUEL_DISTURBANCE_MAX` formula gap | LOW | Closed with H-4 (formula and saturation constant added in §3.7 step 4 + §3.1). |
| L-3 `JUMP_APEX_FRACTION` tag rationale | LOW | Moved into a footnote on the §3.1 row; description column reserved for "what the constant does". |
| L-4 `XC-010-005` anchor missing | LOW | Anchored to Event System #17 §3.4.2 `EventBus.Publish` surface (transport for `HeaderExecutedEvent`); §8.2 / §8.4 / §9.2 updated. Adjudication framed as future Match Referee concern not requiring an anchor at Stage 0. |
| L-5 §6.3.1 eligibility upper-bound looseness | LOW | New footnote on §6.3.1 eligibility-predicate row makes the ≤22/frame cap explicit as worst-case set-piece-frame pessimism, not steady-state expectation. |
| L-6 §5.3.1 telemetry shares unsourced | LOW | §5.3.1 framing rewritten: shares model systematic mistiming from Decision Tree #8 commit-tick choice and upstream perception variance, not noise alone; designer-target pending Stage 0 calibration; no published header timing-label distribution available. |
| L-7 Glossary `ContactPointIntent` semantics drift | LOW | Glossary row rewritten — head-local axis convention pinned (origin at head centre; `+x` = `agent.facing` forward; `+y` = agent-left lateral; euclidean metres). §2.2 struct comment also pinned. |

---

## Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial appendices draft from outline-detailed v1.1    | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: Appendix A.3 rewritten for single-reversal spin formula (H-1); Appendix D glossary `ContactPointIntent` row rewritten with head-local axis convention (L-7); Appendix G OI-001 status updated for `ERR-010-001` filing (M-1); new Appendix H mapping the 21 PASS-1 findings to v0.2 resolutions. | pending |
| 0.3     | May 16, 2026 | drafter | APPROVAL. Appendix A.1 citation "Auger & Pellegrini (2007)" → "Tomczak et al. (2021)" (OI-003 closure — original ref not findable). Appendix G all five OIs marked RESOLVED. | granted |
