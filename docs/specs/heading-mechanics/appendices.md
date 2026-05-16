# Heading Mechanics Specification #10 — Appendices

**Created:** May 16, 2026
**Version:** 0.1
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
Auger & Pellegrini (2007) head-kinematics data (§8.3).

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

### A.3 Spin-Transfer Reversal Boundary

The outgoing spin (§3.6) is:

```
outgoingSpin = SPIN_TRANSFER_COEFF · headAngularVelocity
             + incomingSpin · spinPreservationFactor
             - reversalTerm
```

where

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset / SPIN_TRANSFER_REVERSAL_THRESHOLD)
reversalTerm           = max(0, -spinPreservationFactor) · incomingSpin
```

At `contactPointAxialOffset = SPIN_TRANSFER_REVERSAL_THRESHOLD`,
`spinPreservationFactor` crosses zero — at that exact offset, the
incoming spin contribution to outgoing spin is zero. Beyond the
threshold, `spinPreservationFactor` is negative and the
`reversalTerm` activates, producing a partial reversal of the
incoming spin component.

**Worked example.** Incoming topspin 8 rad/s; axial offset 0.02 m;
`SPIN_PRESERVATION_BASE = 0.6`; `SPIN_TRANSFER_REVERSAL_THRESHOLD =
0.015 m`.

```
spinPreservationFactor = 0.6 · (1 - 0.02 / 0.015) = -0.2
reversalTerm           = 0.2 · 8 = 1.6 rad/s
incomingSpinContribution = 8 · (-0.2) - 1.6 = -3.2 rad/s
```

The 8 rad/s topspin becomes a 3.2 rad/s backspin in the outgoing
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
| `ContactPointIntent` | Decision Tree #8 output specifying the intended contact location on the head surface (2-D head-local coordinates: forehead-centre / forehead-edge / temple as a continuous parameter) |
| `ContactQualityScalar` | Continuous scalar ∈ [0,1] derived from signed timing offset and contact-point error; the formula-gating quantity for outgoing power / direction (KD-2) |
| `HeaderIntent` | Decision Tree #8 output struct: `powerIntent`, `contactPointIntent`, `targetIntent`, `attemptCommittedTick` |
| `HeaderExecutedEvent` | Event published on every contacted header; carries telemetry, outgoing velocity / spin, and the `ownGoalShapedTrajectory` flag |
| `HeaderAttemptFailedEvent` | Event published on missed-contact attempts (mistimed, mis-positioned, disturbed-in-duel); ball state unchanged (KD-12) |
| `ContestedDuelContext` | Per-duel context struct: participating agents, winner, per-agent disturbance factors |
| `OwnGoalShapedTrajectory` | Boolean flag on `HeaderExecutedEvent` set when the outgoing trajectory's dual-horizon projection intersects the defending team's own goal-line bounding box (KD-6; adjudication is Event System #17) |
| `JumpReach` | `[DERIVED]` quantity (KD-4): the vertical apex altitude of the head during the synthetic Stage 0 jump phase |
| `DRAW_SITE_*` | Registered RNG draw sites per Deterministic Sim #16 §4.5: `DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER` |
| `DOMAIN_TAG_HEADING` | `[CROSS-PENDING]` `0x16` allocation in #16 §3.4 catalogue; promoted to `[CROSS]` atomic with back-prop ERR-010-001 |

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
| OI-001 | `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 | back-prop ERR-010-001 | pending — to file when section-3 lands |
| OI-002 | #18 §3.10 trace channel rows for `heading.*` channels | back-prop | pending |
| OI-003 | DOI verification for §8.3 external references | drafter | pending |
| OI-004 | Goalkeeper #11 interface confirmation | post-#11 IN REVIEW | not blocking |
| OI-005 | Pin #3 contact-event API subsection and #8 intent-surface §1.7.x exact anchor | drafter | pending — during pass-2 |
| OI-006 | `certification-platform.md` Stage-0 host pin | lead developer | not blocking #10 sign-off |

---

## Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial appendices draft from outline-detailed v1.1    | pending  |
