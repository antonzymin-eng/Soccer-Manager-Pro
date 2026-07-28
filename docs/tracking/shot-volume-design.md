# Shot Volume — Design Supplement

> **Created:** July 28, 2026
> **Status:** DESIGN SUPPLEMENT (class (b) — governs a balance pass over an APPROVED spec's surface;
> the contract change back-propagates into #8 as ERR-008-017)
> **Owner surface:** Decision Tree #8 §3.2.3 (SHOOT utility) — `src/decision-tree/UtilityScorer.cs` +
> `UtilityWeights.cs`; measured through the match engine.
> **Version:** 1.0 (converged — AR history §8; measured results §6)

---

## 0. Scope

§5.Z.19 discharged roughly half of the shot-volume excess as a side effect of real shot pace
(59–70 → 31–45 shots/match; football ≈ 25) and named the remainder *"a DT shot-selection /
possession-churn property."* This pass measures which of those two properties carries the mass,
fixes the one that is a Decision Tree defect, and records the other.

Out of scope: possession churn itself (final-third entries ≈ 3× football — a pass-accuracy /
possession-retention property spread across #4/#5/#13, recorded in §7), the keeper contact rate
(§5.Z.20 §7.1), and any #6 change (shot execution is not the problem; shot *selection* is).

## 1. Baseline measurement (July 28, 2026 — post-§5.Z.20 tree)

`ShotOutcomeDiagnosticTests` v1.3 (TD_SHOT_DIAGNOSTIC=1), 3 full 90-minute matches on the
`ConfigureSquads` path, same three seeds as every pass since §5.Z.17:

| seed | shots | goals | mean shot dist | ≤11.5 m | 11.5–16.5 | 16.5–22 | >22 m | third entries | shots/entry |
|---|---|---|---|---|---|---|---|---|---|
| 0x0F1E…6978 | 31 | 6 | **34.1 m** | 7 | 2 | 4 | **18** | 317 | 0.10 |
| 0x0000…D05E | 35 | 9 | **34.1 m** | 3 | 6 | 5 | **21** | 305 | 0.11 |
| 0x5EED…0003 | 38 | 9 | **30.2 m** | 8 | 3 | 3 | **24** | 319 | 0.12 |

Football reference: ~25 shots/match, mean shot distance ~17 m, ~15% of shots beyond 22 m,
~100–120 combined final-third entries.

**The distribution, not the count, is the finding.** ~60% of all shots come from beyond 22 m and
the mean sits at 30–34 m — against a range gate whose maximum is 35 m
(`BASE_SHOOT_RANGE` 20 + `A_LongShots` × `LONGSHOT_RANGE_BONUS` 15). Shots cluster AT the range
boundary: an agent fires the moment the goal comes into range.

## 2. Diagnosis — the utility has no distance term

Verified against source (`UtilityScorer.ScoreShoot`, `OptionGenerator.ComputeGoalOpeningScore`):

- `U_SHOOT = baseU(zone) × am × GoalOpeningScore × tactM × (1 − risk)` — **no distance factor**.
- `GoalOpeningScore` is the unblocked fraction of the subtended goal arc — **scale-free by
  construction**: the goal arc and a near-goal blocker's occlusion arc both shrink ~1/d, so a
  30 m shot with the same blocker geometry scores the same opening as an 8 m one. (This is
  correct for what the score measures — *how much of the goal is visible* — and wrong as the
  sole geometry term in a shot-preference model.)
- The zone modifier is a step function that never bites: every reachable shot is in the
  ATTACKING zone (×1.00), because the MIDFIELD branch needs distance ≥ 40 m (team-relative zone
  boundary at 65 m from own goal line) while the range gate caps at 35 m — the midfield
  long-shot machinery (`LONG_SHOT_THRESHOLD`, `SHOOT_ZONE_MID_*`) is production-unreachable
  through the generator (recorded §7.3, not fixed here).

So within [0, 35] m, distance influences shot preference not at all, while football's
P(goal | shot) falls roughly tenfold from 11 m to 30 m. The formula omits the strongest
single predictor of shot value in the game it models — the ERR-008-016 class ("the spec
inverted/omitted the game it models"), filed as **ERR-008-017**.

Churn is real too (≈3× football's third entries) but it is not what puts the mean at 34 m; at
0.10–0.12 shots per third entry, entries alone would produce a *football-shaped* distance
distribution if selection were right. Selection is the lever; churn is recorded.

## 3. Key decisions

- **KD-V1 — the lever is a multiplicative `DistanceQuality_SHOOT` term in U_SHOOT**, not a
  tighter range gate. A gate is a cliff: it forbids the 30 m screamer outright instead of
  making it lose to a decent pass most of the time — and the composure-noise band (±0.15)
  should still let an adventurous agent occasionally take one, which is football. The range
  gate stays as the hard eligibility cap.
- **KD-V2 — shape: hyperbolic decay above a sweet range.**
  `distQ(d) = 1.0` for `d ≤ SHOOT_SWEET_RANGE_M`, else
  `SHOOT_DIST_FALLOFF_M / (SHOOT_DIST_FALLOFF_M + (d − SHOOT_SWEET_RANGE_M))`.
  Continuous at the knee, bounded (0, 1], monotone, no transcendental on the 10 Hz path.
  Inside the sweet range the term is exactly 1.0, so every existing close-shot utility is
  byte-identical — the calibrated §5.Z.17/§5.Z.19 close-range behaviour is untouched.
- **KD-V3 — applied in the scorer, not the generator.** `ActionOption.DistanceToGoal` is
  already populated by `GenerateShootCandidate`; the scorer reads it. Direct-injection test
  options that never set the field read 0 ⇒ distQ = 1.0 ⇒ every existing unit expectation
  stands unmodified.
- **KD-V4 — `A_LongShots` keeps its two existing roles** (range-gate extension; the
  unreachable midfield zone bar) and does NOT additionally soften the decay. A third coupling
  would add tuning surface with no measured need; if long-shot specialists prove
  under-represented after calibration, a falloff scaled by `A_LongShots` is the recorded
  follow-up shape.
- **KD-V5 — acceptance discriminates on DISTANCE, not count.** Over a 9-minute scenario
  window the shot count is too noisy to band tightly (3–5 shots), but the mean-distance gap
  (30–34 m pre vs < 22 m target post) is an order-one signal even on few shots. The
  `match-engine-shot-speed` scenario (which owns shot quality) gains a mean-shot-distance
  predicate; the volume claim itself is measured over 3 full matches by the diagnostic.

## 4. Changes

| File | Change |
|---|---|
| `src/decision-tree/UtilityWeights.cs` | + `[GT] SHOOT_SWEET_RANGE_M` = 12, `[GT] SHOOT_DIST_FALLOFF_M` = 8 (v1.5) |
| `src/decision-tree/UtilityScorer.cs` | `ScoreShoot` gains the distQ factor (v1.12) |
| `docs/specs/decision-tree/section-3-2-3-to-3-2-9.md` | §3.2.3.1 formula + constants; §3.2.3.3 worked examples gain the term (Case A pinned inside the sweet range so its arithmetic is unchanged); §3.2.3.4 gains boundary case 4 (ERR-008-017 anchors) |
| `docs/tracking/spec-error-log.md` | ERR-008-017 filed, resolved same commit |
| `src/decision-tree/Tests/UtilityScorerTests.cs` | + distQ locks (sweet-range identity; monotone decay; the long-vs-close discriminating comparison) |
| `src/match-engine/tests/MatchEngineShotSpeedScenarios.cs` | + `mean-shot-distance-reaches-football-band` predicate (KD-V5) |
| `src/match-engine/tests/ShotOutcomeDiagnosticTests.cs` | v1.3 measurement extension (landed with the baseline) |
| `src/match-engine/tests/MatchEngineShotOutcomeScenarios.cs` | v1.1 — windows 9 → 18 min/seed: at the calibrated goal rate the 9-min corpus measured ZERO goals, failing its own `goals-still-scored` reachability predicate (found by the full gate); the sanity ceiling rescaled 1.2 → 2.4 per doubled window, still failing the pre-fix ~3.1 (the keeper-conversion corpus-sizing lesson) |

No `SNAPSHOT_SCHEMA_VERSION` change (utility scoring serializes nothing), no new RNG
stream / domain tag / draw site, no draw-order change. Digests move for any match containing
a shot decision, as every balance pass's do.

## 5. Acceptance

1. `match-engine-shot-speed` v1.2: existing speed floors hold AND mean shot distance over the
   corpus ≤ 24 m — **fails on the pre-fix engine** (measured 30–34 m), verified by running the
   scenario before the scorer change landed.
2. `UtilityScorerTests` locks (7): sweet-range identity vs the pre-fix formula, decay
   monotonicity, exact knee-point continuity, the discriminating comparison (an open 30 m shot
   loses to a moderate pass that the pre-fix scorer had it beating), and [GT] shape guards.
3. Diagnostic over 3 full matches: shots/match in a ~20–30 band, mean shot distance ≤ 22 m.
   **Outcome recorded in §6:** the ladder showed the two halves of this target are not
   simultaneously reachable by this lever (close-chance creation, not selection, bounds the
   count once long shots are suppressed); the calibrated landing takes the distribution +
   goal-rate win at ~18 shots/match and §7.1 owns the count gap. The design target is left
   here as written — moving it after measurement would hide that the measurement refused it.

## 6. Measured results (3 full matches, same seeds pre/post)

**The falloff ladder** (SWEET = 12 throughout; shots / >22 m share / goals, each cell the
3-match aggregate):

| FALLOFF | shots (avg) | >22 m share | goals (avg) | mean dist per seed |
|---|---|---|---|---|
| ∞ (pre-fix) | 31 / 35 / 38 (34.7) | 60% | 6 / 9 / 9 (8.0) | 34.1 / 34.1 / 30.2 |
| 10 | 24 / 32 / 34 (30.0) | 38% | 6 / 8 / 13 (9.0) | 22.8 / 28.6 / 19.5 |
| 9 | 20 / 26 / 26 (24.0) | 39% | 6 / 8 / 9 (7.7) | 28.7 / 33.5 / 21.3 |
| **8 (final)** | **17 / 19 / 17 (17.7)** | **30%** | **4 / 5 / 5 (4.7)** | **16.5 / 27.1 / 20.1** |
| 6 | 11 / 13 / 13 (12.3) | ~30% | 3 / 8 / 5 (5.3) | 15.5 / 28.6 / 18.7 |

**The calibration decision, stated plainly.** FALLOFF = 9 hits the §5 count band squarely
(24.0 avg vs the ~25 target) but keeps ~39% of shots beyond 22 m and leaves goals at 7.7 —
essentially the pre-fix rate. FALLOFF = 8 lands the football-shaped *distribution*: long-shot
share halved to 30%, **goals 4.7/match — the closest this engine has ever measured to
football's ~2.7** — and football-shaped scorelines (2-2 / 3-2 / 5-0), at the cost of the count
undershooting to ~18. The ladder shows the two targets are NOT simultaneously reachable by
this lever: reaching 25 shots that are mostly inside 22 m requires ~22 close-range chances a
match, and close-chance *creation* is bounded by box penetration, not by shot selection —
suppressed long shots convert into passes, and most of those possessions die before producing
a close shot (the churn/creation residual, §7.1). **FALLOFF = 8 chosen**: the pass's purpose
in the roadmap chain is a goal rate that makes the A4a corpus worth fitting, and a
football-shaped distribution at 18 shots serves that strictly better than a football-count 24
shots still dominated by range-boundary strikes. The per-seed mean-distance column is noisy
(each falloff step re-cascades the whole match; n = 3), which is why the ladder is read on the
aggregate share and count columns.

Speed floors were unaffected throughout (means 15.4–21.0, maxima 23.8–27.6 across all
iterations — the decay changes which shots are TAKEN, not how they are struck).

## 7. Residuals — recorded, NOT fixed

1. **Possession churn / close-chance creation:** 305–319 final-third entries vs football's
   ~100–120 — a possession-retention property (pass accuracy #5, first-touch #4, press
   turnover rate #13), not a shot property. It inflates every per-match rate that has "per
   possession" in its true denominator, and it now also owns the calibrated shot-count
   undershoot (§6): with long shots correctly losing to passes, total volume is bounded by how
   many possessions penetrate the box, and at 3× football's churn almost none do — each entry
   produces a shot a third as often as football's. Own pass, own measurement.
2. **Goals/shot rises as volume falls** (0.19–0.26 → 0.24–0.29 at the final [GT] — the
   arithmetic consequence of dropping low-conversion long shots). Goals land at 4.7/match; the
   remaining excess over football's ~2.7 is the keeper contact rate + Stage-0 pointQuality
   lottery already recorded in §5.Z.20 §7.
3. **The midfield long-shot branch is production-unreachable** (§2): `SHOOT_ZONE_MID_LONG` /
   `SHOOT_ZONE_MID_SHORT` / `LONG_SHOT_THRESHOLD` gate a zone whose minimum distance (40 m)
   exceeds the range gate's maximum (35 m). Harmless dead surface; a future pass that wants
   40 m specialists must touch both constants together. Not filed as its own ERR — ERR-008-017
   records it as context.

## 8. Adversarial-review history

| Round | Findings | Resolution |
|---|---|---|
| AR-1 (design) | M-1: the first draft proposed patching the GENERATOR gate (`MIN_GOAL_VISIBILITY` distance-scaled) — but §5.Z.18 already measured visibility gates "barely dent" volume, and a gate cannot make a long shot *lose a comparison*, only vanish; moved to the scorer per KD-V1. | Fixed in draft |
| AR-2 (design) | L-1: acceptance predicate first drafted as a shots-per-window band — too noisy at 9 min (KD-V5 rationale); switched to mean distance. L-2: Case A worked example redrafted to a distance inside the sweet range so the approved arithmetic is preserved rather than recomputed. | Fixed in draft |
| AR-3 (code, over the shipped diff + by execution) | 0H+1M+2L — **M-1** (found by running the suite, not by reading): `ShootMidfield_LongShotsRaw12_GetsLongModifier` compared the two zone-modifier branches as a pure ratio at 28 m, where the new decay pushes the SUPPRESSED branch (0.05 zone × 0.33 distQ) under `UTILITY_FLOOR` — the clamp corrupted the ratio (8.67 vs 11.0). The distance was incidental to the lock's intent (the shifted-form gate); re-anchored inside the sweet range with a comment naming the interaction. **L-1**: a NaN `DistanceToGoal` propagates NaN through distQ into the utility — verified fail-closed by the existing AR-3 NaN-gate at the clamp (floor), no new gate needed. **L-2**: the scenario's vx-sign goal attribution misattributes a hypothetical exactly-lateral shot (vx = 0); unreachable from the generator (aim is at the goal centre from an in-range position) and one sample of an aggregate mean — accepted, noted in the predicate comment's contract. | M-1 fixed same commit |
| AR-4 (sweep + full gate) | 0H+1M — **M-1** (found by the full gate, not by reading): the `match-engine-shot-outcomes` scenario's `goals-still-scored` reachability predicate failed — its 4 × 9-min neutral-path corpus produced ZERO goals at the calibrated rate. Corpus resized to 18 min/seed with the sanity ceiling rescaled (pre-fix discriminator preserved). Also verified: no other SHOOT producer than `GenerateShootCandidate` (grep); negative direct-injection distances read distQ = 1.0 (the KD-V3 safe side); the keeper-conversion scenario survives the volume drop (re-run, passes); the [GT] constants match the spec text after calibration. CONVERGENCE. | M-1 fixed same commit |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-28 | — | Baseline measurement + diagnosis + KD-V1..V5; design AR-1/AR-2 folded into the draft. |
| 1.0 | 2026-07-28 | — | Implemented + calibrated over a four-rung falloff ladder (§6 — the ladder refused half the design target and the distribution/goal-rate landing was chosen, FALLOFF = 8); ERR-008-017 filed + spec patched; code AR-3 (1M by execution) + AR-4 sweep — CONVERGENCE. |
#endregion
