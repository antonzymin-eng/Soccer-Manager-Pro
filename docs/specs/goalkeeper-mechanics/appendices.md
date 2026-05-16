# Goalkeeper Mechanics Specification #11 — Appendices

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT

---

## Appendix A — Derivations

### A.1 `requiredReactionMs` derivation

From Perception System #7 base latency `PERCEPTION_BASE_LATENCY_MS`
+ GK reflex modulation. The reaction-time formula models a serial
chain: visual cue detection (Perception #7 base + reflex
modulation) → motor program selection (`REACTION_BASE_MS` minus
reflex bonus) → motor execution start. The ball-speed term
captures the empirically observed shorter reaction allowances at
high incoming speeds (Williams & Burwitz 1993; Savelsbergh et al.
2002).

Sensitivity to ball-speed term: a 1 m/s increase in ball speed
above `REACTION_BALL_SPEED_REF_MPS` extends `requiredReactionMs`
by `REACTION_BALL_SPEED_COEFF = 8 ms`. Over a typical 10 m/s
range (15–25 m/s) this is +80 ms, comparable to the headline
`REACTION_REFLEXES_COEFF = 100 ms` swing — i.e. attribute and
shot speed contribute on the same order to required reaction time.

### A.2 `handlingQualityScalar` linearity proof + monotonicity

`handlingQualityScalar` is by construction the convex blend of two
`[0, 1]`-valued scalars (`rawHandling` clamped + `reactionWindowAchieved`)
with weights `HANDLING_REACTION_BLEND_ALPHA` and `1 − ...`. By the
convexity of `clamp01`, the result is in `[0, 1]`.

Monotonicity of band-to-action helpers (§3.5.3):

- `parryVelocity` retain: `retain = base − k_quality · quality − k_clutch · clutch`,
  strictly decreasing in `quality` and `clutch` over their valid
  ranges. Better handling and firmer clutch both reduce retained
  bounce energy → smaller outgoing speed.
- `deflectVelocity` retain ≥ `parryVelocity` retain at matched
  quality (by the `+0.10` additive constant in §3.5.3): deflections
  retain slightly more bounce energy than parries because the GK
  has elected to redirect, not absorb.
- `spillVelocity` retain ≥ `parryVelocity` retain by `+0.20`:
  spills retain even more (poor handling → near-full incoming
  rebound).

Combined: at matched `quality`, outgoing-speed magnitude is
`spill > deflect > parry > catch (0 by construction)` — the
band-to-action mapping preserves intuitive ordering.

### A.3 Dive launch impulse derivation

Work-energy from `Strength`: the GK's launch impulse scales
linearly with the maximum lateral push from the standing position.
Per Spratford et al. (2009) elite-keeper biomechanics, lateral
launch velocities of 3–5 m/s are observed; the §3.4.4 anchors
(base 3.5 m/s, `+1.2 · Strength_norm` peak modifier, `+0.8 ·
Aerial_norm`) bracket this range with `Strength_norm = 0.8 +
Aerial_norm = 0.7` giving a top-end ≈ 5.02 m/s.

First-principles ablation:

- Strength contribution alone (`Aerial = 0`, `Strength = 1`):
  3.5 + 1.2 = 4.7 m/s.
- Aerial contribution alone: 3.5 + 0.8 = 4.3 m/s.
- Joint contribution: 3.5 + 1.2 + 0.8 = 5.5 m/s (capped via
  reach-envelope physics in §3.3.4, NOT via clamp at the impulse
  step — high-Strength + high-Aerial keepers genuinely launch
  faster).

### A.4 Cross-claim duel score sensitivity

Analog of Heading #10 Appendix A duel derivation. The cross-claim
duel score is a fixed-weight linear combination of three
`[0, 1]`-valued attributes (`Balance`, `Strength`, `Aerial`).
Weights sum to 1.0 (FR-GK-039). Maximum achievable score is 1.0
(all attributes at 1.0); typical mid-skill ≈ 0.5 (all at 0.5).

The tiebreak Gaussian's `CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE =
0.015` is small enough that it can only flip a duel outcome when
the baseline gap is < `CROSS_CLAIM_TIEBREAK_EPSILON = 0.03`
(approximately 1.5 standard deviations of the perturbation).
Beyond that gap, the noise cannot overcome the deterministic
ordering, preserving "skill matters" while permitting deterministic
indeterminacy on coin-flip cases.

---

## Appendix B — Sensitivity Tables

### B.1 `requiredReactionMs` over `Reflexes × ball-speed` grid (11 × 11)

Computed at `state ≠ OneOnOne`. Rows: `Reflexes_norm` 0.0 to 1.0
in 0.1 steps. Columns: ball speed 12 m/s to 32 m/s in 2 m/s steps.

| Reflexes \\ Speed | 12 | 14 | 16 | 18 | 20 | 22 | 24 | 26 | 28 | 30 | 32 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0.0 | 350 | 350 | 350 | 350 | 366 | 382 | 398 | 414 | 430 | 446 | 462 |
| 0.1 | 340 | 340 | 340 | 340 | 356 | 372 | 388 | 404 | 420 | 436 | 452 |
| 0.2 | 330 | 330 | 330 | 330 | 346 | 362 | 378 | 394 | 410 | 426 | 442 |
| 0.3 | 320 | 320 | 320 | 320 | 336 | 352 | 368 | 384 | 400 | 416 | 432 |
| 0.4 | 310 | 310 | 310 | 310 | 326 | 342 | 358 | 374 | 390 | 406 | 422 |
| 0.5 | 300 | 300 | 300 | 300 | 316 | 332 | 348 | 364 | 380 | 396 | 412 |
| 0.6 | 290 | 290 | 290 | 290 | 306 | 322 | 338 | 354 | 370 | 386 | 402 |
| 0.7 | 280 | 280 | 280 | 280 | 296 | 312 | 328 | 344 | 360 | 376 | 392 |
| 0.8 | 270 | 270 | 270 | 270 | 286 | 302 | 318 | 334 | 350 | 366 | 382 |
| 0.9 | 260 | 260 | 260 | 260 | 276 | 292 | 308 | 324 | 340 | 356 | 372 |
| 1.0 | 250 | 250 | 250 | 250 | 266 | 282 | 298 | 314 | 330 | 346 | 362 |

(Below 18 m/s ball-speed reference, the `max(0, ...)` clamp
zeros the speed-penalty term. The 100 ms `Reflexes` swing maps
exactly onto the row index.)

### B.2 `peakHandZ_m` over `Aerial × Strength × fatigue` grid

3-D sample: fatigue ∈ {0.0, 0.5, 1.0}; `Aerial_norm`, `Strength_norm`
∈ {0.0, 0.5, 1.0}. Noise contribution zeroed for illustration.

| fatigue=0.0 | Aerial=0.0 | Aerial=0.5 | Aerial=1.0 |
|---:|---:|---:|---:|
| Strength=0.0 | 1.20 | 1.55 | 1.90 |
| Strength=0.5 | 1.35 | 1.70 | 2.05 |
| Strength=1.0 | 1.50 | 1.85 | 2.20 |

| fatigue=0.5 | Aerial=0.0 | Aerial=0.5 | Aerial=1.0 |
|---:|---:|---:|---:|
| Strength=0.0 | 1.10 | 1.45 | 1.80 |
| Strength=0.5 | 1.25 | 1.60 | 1.95 |
| Strength=1.0 | 1.40 | 1.75 | 2.10 |

| fatigue=1.0 | Aerial=0.0 | Aerial=0.5 | Aerial=1.0 |
|---:|---:|---:|---:|
| Strength=0.0 | 1.00 | 1.35 | 1.70 |
| Strength=0.5 | 1.15 | 1.50 | 1.85 |
| Strength=1.0 | 1.30 | 1.65 | 2.00 |

(Approximately 17% reduction from fatigue=0 to fatigue=1, matching
T-5.3.3 validation target.)

### B.3 `handlingQualityScalar` over `Handling × ball-speed × point-error` (selected slices)

Slice at `point-error = 0.02 m`, `reactionWindowAchieved = 0.7`,
`fatigue = 0.2`, no `OneOnOne`. Noise zeroed.

| Handling \\ Speed (m/s) | 14 | 18 | 22 | 26 | 30 |
|---:|---:|---:|---:|---:|---:|
| 0.4 | 0.45 | 0.43 | 0.40 | 0.36 | 0.32 |
| 0.6 | 0.51 | 0.49 | 0.45 | 0.41 | 0.36 |
| 0.8 | 0.58 | 0.55 | 0.51 | 0.46 | 0.40 |
| 1.0 | 0.65 | 0.62 | 0.57 | 0.51 | 0.45 |

Slice at `point-error = 0.06 m` (just beyond the `[GT]` sigma):

| Handling \\ Speed | 14 | 18 | 22 | 26 | 30 |
|---:|---:|---:|---:|---:|---:|
| 0.4 | 0.21 | 0.21 | 0.21 | 0.21 | 0.21 |
| 0.6 | 0.21 | 0.21 | 0.21 | 0.21 | 0.21 |
| 0.8 | 0.21 | 0.21 | 0.21 | 0.21 | 0.21 |
| 1.0 | 0.21 | 0.21 | 0.21 | 0.21 | 0.21 |

(At `point-error ≥ HANDLING_POINT_ERROR_SIGMA_M = 0.05 m`,
`pointQuality = 0` and `rawHandling = 0`; the convex blend with
`reactionWindowAchieved = 0.7` × `(1 − α) = 0.3` yields a floor of
0.21. Attribute differences disappear — bad contact-point error
dominates.)

### B.4 Cross-claim duel score sensitivity

| Configuration | gk score | striker score | Margin |
|---|---:|---:|---:|
| All-equal mid-skill (0.6 / 0.6 / 0.6 both) | 0.600 | 0.600 | 0.000 (tiebreak invoked) |
| GK aerial-advantaged (0.6 / 0.6 / 0.8 vs. 0.6 / 0.6 / 0.6) | 0.690 | 0.600 | 0.090 (no tiebreak) |
| GK strength-disadvantaged (0.6 / 0.4 / 0.6 vs. 0.6 / 0.7 / 0.6) | 0.530 | 0.635 | 0.105 (no tiebreak; striker wins) |
| Near-tie (0.7 / 0.7 / 0.7 vs. 0.7 / 0.72 / 0.69) | 0.700 | 0.7065 | 0.0065 (tiebreak invoked) |

---

## Appendix C — Exemplar GK Tuning Profiles

Three illustrative preset profiles per Heading #10 Appendix C
precedent. Designer-authored values supersede at Stage 1+.

### C.1 Sweeper-keeper (high-aerial specialist)

| Attribute | Value |
|-----------|------:|
| `Reflexes_norm` | 0.75 |
| `Handling_norm` | 0.72 |
| `Aerial_norm` | 0.90 |
| `OneVsOne_norm` | 0.65 |
| `Throwing_norm` | 0.78 |
| `Kicking_norm` | 0.82 |
| `Strength_norm` | 0.78 |
| `Balance_norm` | 0.70 |
| `Composure_norm` | 0.70 |
| `Pace_norm` | 0.72 |

Tuning bias: high `Pace` and `Aerial` for sweep + cross-claim
specialism; `OneVsOne` mid-band (sweepers face fewer 1v1s by
design).

### C.2 Classic reactive shot-stopper

| Attribute | Value |
|-----------|------:|
| `Reflexes_norm` | 0.92 |
| `Handling_norm` | 0.88 |
| `Aerial_norm` | 0.65 |
| `OneVsOne_norm` | 0.85 |
| `Throwing_norm` | 0.62 |
| `Kicking_norm` | 0.55 |
| `Strength_norm` | 0.72 |
| `Balance_norm` | 0.80 |
| `Composure_norm` | 0.85 |
| `Pace_norm` | 0.55 |

Tuning bias: highest `Reflexes` + `Handling` + `OneVsOne` for
shot-stopping; lower `Pace` and `Aerial` (line-keeper).

### C.3 Balanced modern keeper

| Attribute | Value |
|-----------|------:|
| `Reflexes_norm` | 0.78 |
| `Handling_norm` | 0.78 |
| `Aerial_norm` | 0.75 |
| `OneVsOne_norm` | 0.75 |
| `Throwing_norm` | 0.75 |
| `Kicking_norm` | 0.75 |
| `Strength_norm` | 0.75 |
| `Balance_norm` | 0.75 |
| `Composure_norm` | 0.75 |
| `Pace_norm` | 0.65 |

Tuning bias: uniform 0.75 spread.

---

## Appendix D — Glossary

| Term | Definition |
|------|-----------|
| `GK_SAVE_VOLUME` | XYZ envelope around the GK within which the §3.1 eligibility predicate considers ball contacts |
| `SaveIntent` | Decision Tree #8 GK-branch output payload consumed by §3.5 |
| `ClaimIntent` | #8 GK-branch payload for cross / aerial claim |
| `DistributeIntent` | #8 GK-branch payload for throw / roll / kick |
| `RushIntent` | #8 GK-branch payload for sweep / 1v1 close-down |
| `HandlingQualityScalar` | Continuous `[0, 1]` quality emitted by §3.5; physics does not branch on its label |
| `ReactionWindowAchieved` | Continuous `[0, 1]` reaction quality emitted by §3.2; asymmetric tolerances per KD-18 |
| `SaveAttemptedEvent` | Event emitted on every save attempt (successful or failed) |
| `BallClaimedEvent` | Event emitted on cross / aerial / 1v1 claim (catch path) |
| `DistributionExecutedEvent` | Event emitted when distribution kick / throw / roll releases |
| `GoalkeeperRushEvent` | Event emitted on rush state entry / exit / abort |
| `CrossClaimDuelContext` | Per-frame duel structure populated when ≥2 agents within `CROSS_CLAIM_VOLUME_RADIUS_M` |
| `Resting` | State machine state; GK position authority held by Positioning AI #12 |
| `Set` | State; GK reactive to ball position with micro-shuffle bounded by `GK_REACTIVE_RADIUS_M` |
| `Anticipate` | State; pre-shot anticipation engaged (`gkAnticipationScore > ANTICIPATE_THRESHOLD`) |
| `Diving` | State; dive launch impulse applied; pre-airborne single-frame |
| `Airborne` | State; in the air during dive |
| `HandsOnBall` | State; ball caught and held; `releaseTickEarliest` countdown active; `GK_HOLD_MAX_TICKS` enforces 6-second rule |
| `Recovering` | State; post-attempt recovery to set line |
| `Distributing` | State; distribution windup in progress |
| `Rushing` | State; sweep launched per KD-15 |
| `OneOnOne` | State; 1v1 confrontation engaged; KD-20 coefficients apply |
| `Smothered` | State; ball smothered post-rush at attacker's feet |

---

## Appendix E — Mapping Table to v0.1 Adversarial Review Findings

Two-column table: finding number (1–13 from `outline.md`
adversarial-review appendix) → resolution location in this spec
(KD-N or section ID).

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| 1 missing metadata | H | All section files carry Created / Version / Status / Purpose headers (this file and §1–§9) |
| 2 section-plan misalignment | H | Sections re-mapped to CLAUDE.md template (§2 = FR/data/failure; §6 = perf; §7 = future; §8 = refs) |
| 3 save-outcome enum risk | H | KD-1 / KD-21; §2.2.6 enums are TELEMETRY ONLY |
| 4 GK head ownership unclear | H | KD-4; §3.6 routing predicate; §3.10 boundary algorithm |
| 5 reaction-time gating | M | KD-2; §3.2 continuous scalar; labels post-formula |
| 6 #12 boundary | H | KD-3; KD-13; §3.3.0 consumer contract |
| 7 distribution / RNG / outcome-enum cluster | H | KD-6 (distribution); KD-7 (RNG governance); KD-11 (failed-save physics) |
| 8 fatigue / coordinate / tick-rate | M | KD-8; KD-10 |
| 9 constant-tag policy | M | KD-9; §3.4 master table; §9.1 verification gate |
| 10 — (no finding) | — | n/a |
| 11 invariants citation | M | KD-10 |
| 12 invariants citation | M | KD-10 |
| 13 dependencies enumeration | M | §1.4 dependency table; §4.2 input contracts; §4.3 output contracts |

---

## Appendix F — Mapping Table to Outline Pass-1 Review Findings

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| H-1 asymmetric reaction tolerance | H | KD-18; §3.2.3 piecewise formula; §3.4.3 keeps `REACTION_EARLY_TOLERANCE_MS` and `REACTION_LATE_TOLERANCE_MS` as distinct `[GT]` rows |
| H-2 KD-3 boundary with #12 was a hand-wave | H | KD-3 sharpened; KD-13 added; §3.3.0 consumer contract added |
| H-3 distribution kick coupling to Pass Mechanics #5 unclear | H | KD-6 + KD-16; §3.8 `mapToPassMechanicsDelivery` specified |
| H-4 dive Z-kinematics missing AM #2 boundary | H | KD-12; §3.3 owns synthetic dive trajectory; §7.5 deferral |
| M-1 RNG draw sites not enumerated | M | §4.4.2 lists 4 draw sites; each wired to specific §3.X caller |
| M-2 reaction-time citation to #7 absent | M | KD-2 / §3.2.1 cites #7 §3 |
| M-3 #12 ratification mechanism unspecified | M | KD-13; §3.3.0 contract; §9.4 OI-005 |
| M-4 cross-claim head-vs-hand routing ambiguity | M | KD-14; §3.6.1 / §3.6.2 |
| M-5 rush abort policy undefined | M | KD-15; §3.7.3; F-08 |
| M-6 distribution release geometry ownership | M | KD-16; §3.8.1 |
| M-7 set-piece scope unclear | M | KD-19; §3.2 / §5.2.2 |
| M-8 §3 constants not inventoried against §3.4 | M | §3.4 expanded to ~79 rows; inventory closure verified |
| L-1 concussion / discipline absence | L | KD-17; §7.1 / §7.3 |
| L-2 `OneVsOne` attribute use unspecified | L | KD-20; §3.5.1 `attrFactor`; §3.2.2 `requiredReactionMs` |
| L-3 band-to-action mapping ambiguity | L | KD-21; §3.5.2 |
| L-4 6-second-rule constant not classified | L | `GK_HOLD_MAX_TICKS` `[FIXED]` in §3.4.2 |
| L-5 `DRAW_SITE_HANDLING_NOISE` shared between two error sources | L | Split into `DRAW_SITE_HANDLING_NOISE` + `DRAW_SITE_HANDLING_POINT_NOISE`; §3.5.1 / §4.4.2 |
| L-6 §8.3 anchor sparseness | L | Six external references named in §8.3 |

---

## Appendix G — Mapping Table to Outline Pass-2 Review Findings

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| P2-M-1 §3.5 contact-point noise shared with handling-scale | M | Resolved by L-5 split (Appendix F) |
| P2-M-2 `Ball.SetPossessor` surface presumed but not verified | M | §4.3 OI-006 verification posture; §9.4 OI-006 |
| P2-L-1 KD-12 dual reference to `DIVING_HEADER` and new `DIVING_SAVE` | L | KD-12 simplified: Stage 0 re-uses `DIVING_HEADER`; Stage 1+ adds `DIVING_SAVE` via §7.5 deferral |
| P2-L-2 FR-GK-026 atomic-resolution mechanism unclear | L | FR-GK-026 refined: atomic with #16 back-prop AND with #11 status flip |
| P2-L-3 §6.3 cross-claim duel-rate cited "per Opta" without cross-ref | L | §6.3.4 cross-references §8.3 Opta/StatsBomb commercial-data class |

---

## Appendix H — Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; Appendices A–G + glossary | self-pass-1 in `adversarial-review-section-files-v1.md` |
