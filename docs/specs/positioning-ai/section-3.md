# Positioning AI Specification #12 — Section 3: Core Formulas and Algorithms

**Created:** May 15, 2026
**Last Updated:** May 18, 2026 (v0.4 — Run 3 adversarial fix pass: 11 body-text `[EST]` constants promoted to `[GT]` matching §6.1 catalogue; GK §3.3.3 stale prose updated to reflect #11 APPROVED; §3.8 table-header corrected; SPACING_MAX_PASSES promoted)
**Version:** 0.4
**Status:** APPROVED

---

This section publishes the per-tick computation pipeline. Every
formula carries units, valid input ranges, and at least one worked
example (CLAUDE.md "When Writing or Editing Specs").

## 3.0 Phase Computation (Local)

### 3.0.1 Inputs and Outputs

- **Inputs:** possession owner from #7 Perception (`EntityId?`,
  `null` for loose ball); ball longitudinal velocity `ball.vx`
  filtered over a 3-tick window.
- **Output:** `Phase ∈ {InPoss, OutOfPoss, TransToAtk, TransToDef}`.

### 3.0.2 Classification Rule

```
isOwn = (possessionOwner != null) && (possessionOwner.team == ownTeam)
isOpp = (possessionOwner != null) && (possessionOwner.team != ownTeam)
isLoose = (possessionOwner == null)

candidate =
    isOwn                             → InPoss
    isOpp                             → OutOfPoss
    isLoose && ball.vx_filtered > +V₀ → TransToAtk    (ball heading toward opp goal)
    isLoose && ball.vx_filtered < -V₀ → TransToDef    (ball heading toward own goal)
    else                              → lastPhase     (no candidate change)
```

`V₀ = 4.0 m/s` `[GT]` (`PHASE_LOOSE_VELOCITY_THRESHOLD` — Appendix A.6). The
3-tick moving average filters tactical ball touches from genuine
transitions.

### 3.0.3 Hysteresis

A candidate transition commits on the Nth consecutive candidate
tick, where `N = PHASE_HYSTERESIS_TICKS = 3` `[GT]` (Appendix A.5). Concretely:
`phaseDwellTicks` counts the number of consecutive ticks the
candidate has differed from `lastPhase`. When `phaseDwellTicks
reaches N`, the commit fires AT THAT TICK (not the tick after).
On a return to `lastPhase` before commit, `phaseDwellTicks` resets
to 0.

### 3.0.4 Worked Example

Tick T (first candidate tick): own team loses possession, ball
loose, `ball.vx_filtered = −5.2 m/s`. Candidate = `TransToDef`.
`lastPhase = InPoss`. After increment, `phaseDwellTicks = 1`;
output remains `InPoss` (1 < 3). Tick T+1: candidate sustained,
`phaseDwellTicks = 2`; output remains `InPoss` (2 < 3). Tick T+2:
candidate sustained, `phaseDwellTicks = 3` — threshold met,
commit fires: output flips to `TransToDef`, `phaseDwellTicks`
reset to 0. (Commit on the third candidate tick, not the fourth —
AR-S1-09.)

## 3.1 Anchor Computation

### 3.1.1 Formula

For each agent assigned `role` under the active `FormationArchetype`:

```
anchor.x = PITCH_LENGTH_M * formationOffset[role].longPct
anchor.y = PITCH_WIDTH_M  * formationOffset[role].lateralPct
```

`PITCH_LENGTH_M = 105.0` `[FIXED]` and `PITCH_WIDTH_M = 68.0`
`[FIXED]` are cited from Ball Physics #1 §1.2 (coordinate system
appendix). Anchors are computed in the own-team attacking
orientation; the orchestrator mirrors `anchor.x → PITCH_LENGTH_M −
anchor.x` for the defending side before forwarding to #8.

### 3.1.2 Worked Example

4-3-3 archetype, role `LW` (left winger), `formationOffset[LW] =
(longPct: 0.743, lateralPct: 0.100)`:

```
anchor.x = 105.0 × 0.743 = 78.015 m
anchor.y = 68.0  × 0.100 =  6.800 m
```

Anchor = `(78.0, 6.8)`. Matches the reference table in Appendix B.

## 3.2 Ball-Relative Offset

### 3.2.1 Formula

Piecewise-linear in each axis independently, three break-points
per axis:

```
breakPointsX = [ 0.0, PITCH_LENGTH_M/2, PITCH_LENGTH_M ]
breakPointsY = [ 0.0, PITCH_WIDTH_M /2, PITCH_WIDTH_M  ]
basisX(ball.x) ∈ [-1, +1]   (linear interpolation between break-points;
                              center = 0)
basisY(ball.y) ∈ [-1, +1]   (same)

offset.x = pullFactor[role, phase].x * basisX(ball.x) * OFFSET_RANGE_X_M
offset.y = pullFactor[role, phase].y * basisY(ball.y) * OFFSET_RANGE_Y_M
```

`OFFSET_RANGE_X_M = 12.0 m` `[GT]` (Appendix A.7), `OFFSET_RANGE_Y_M = 8.0 m`
`[GT]` (Appendix A.8). `pullFactor[role, phase]` is a `[GT]` lookup keyed on
`(RoleId, Phase)` — published in §3.10 catalogue.

### 3.2.2 Worked Example

Ball at `(20.0, 34.0)` (own defensive third, center channel),
phase `OutOfPoss`, role `AM`, `pullFactor[AM, OutOfPoss] =
(0.60, 0.10)`:

```
basisX(20.0) = (20.0 − 52.5)/52.5 = −0.619
basisY(34.0) = (34.0 − 34.0)/34.0 =  0.000
offset.x = 0.60 × (−0.619) × 12.0 = −4.46 m
offset.y = 0.10 ×   0.000   × 8.0 =  0.00 m
```

AM anchor pulls back ≈4.5 m toward own goal at this ball position.
The maximum AM longitudinal pull is bounded by `pullFactor[AM,
OutOfPoss].x · OFFSET_RANGE_X_M = 0.60 · 12.0 = 7.2 m`, achieved
at `ball.x = 0` where `basisX = −1`. (AR-S1-10: prior "8 m" was a
stale outline value; the formula's true maximum at the catalogued
constants is 7.2 m.)

## 3.3 Line Membership

### 3.3.1 Algorithm

Outfield agents (GK excluded — FR-PA-035) are sorted ascending by
`agent.x` (own-orientation). The k=3 partition cuts are
**archetype-specific** and stored as a per-archetype
`lineCutIndices : (int firstMid, int firstAtk)` pair on
`FormationArchetype` (§2.2.2). Given a sorted index range `[0, 10)`:

- **Defense:** indices `[0, firstMid)` → `firstMid` agents
- **Midfield:** indices `[firstMid, firstAtk)` → `firstAtk − firstMid` agents
- **Attack:** indices `[firstAtk, 10)` → `10 − firstAtk` agents

Per-archetype values (AR-S1-02):

| Archetype | `firstMid` | `firstAtk` | Defense / Midfield / Attack |
|---|---|---|---|
| 4-4-2 | 4 | 8 | 4 / 4 / 2 |
| 4-3-3 | 4 | 7 | 4 / 3 / 3 |
| 4-2-3-1 | 4 | 9 | 4 / 5 / 1 |

For 4-2-3-1, the AM role (Appendix B.3, `longPct = 0.65`) is
**not** assigned to its sorted-x bucket. Instead the archetype
table overrides AM's `defaultLine` to `Midfield` and the line
partition is computed after applying the role→line override
table — Appendix B.3 column `defaultLine`. This makes the "4/5/1
after grouping AM with midfield" partition explicit (rather than
emergent from the sort).

### 3.3.2 Hysteresis

A transition from `lastLine` to a new line commits only if the
agent's longitudinal distance from the line boundary exceeds
`LINE_HYSTERESIS_M = 3.0 m` `[GT]` (Appendix A.2) and persists for
`LINE_DWELL_TICKS = 5` `[GT]` (Appendix A.3) consecutive ticks.

### 3.3.3 Goalkeeper Slot

The GK is excluded from the line partition. The GK slot at Stage 0
is:

```
gkSlot.x = GK_DEPTH_M  + GK_ADVANCE_FACTOR * basisX(ball.x_clamped)
gkSlot.y = PITCH_WIDTH_M / 2 + GK_LATERAL_FACTOR * basisY(ball.y)
```

`GK_DEPTH_M = 5.5 m` `[GT]`; `GK_ADVANCE_FACTOR = 8.0 m` `[GT]`;
`GK_LATERAL_FACTOR = 2.0 m` `[GT]`. **All three promoted to `[GT]`
(KD-13, May 18, 2026):** Goalkeeper Mechanics #11 reached `APPROVED`
on May 18, 2026; these constants were promoted from `[EST]` → `[GT]`
atomically with that transition per KD-13. The v0.1 / v0.2 text
describing them as `[EST]` pending #11 `IN REVIEW` is superseded —
#11 is now APPROVED and these values are locked as `[GT]` in §6.1.

## 3.4 Lane Occupation

### 3.4.1 Classification

Five lateral bins. Bin edges are stored as a `static readonly`
literal array (AR-S1-12) to avoid the `13.6f` representation
drift `(i+1)·13.6f` would introduce:

```
static readonly float[] LANE_EDGES_M =
    { 0.0f, 13.6f, 27.2f, 40.8f, 54.4f, 68.0f };
```

Bins are inclusive-left, exclusive-right, with the final bin
closed on the right:

| Lane | Y range (m) | Edge index |
|---|---|---|
| LW (Left Wing) | [0.0, 13.6) | 0..1 |
| LH (Left Half) | [13.6, 27.2) | 1..2 |
| C (Center) | [27.2, 40.8) | 2..3 |
| RH (Right Half) | [40.8, 54.4) | 3..4 |
| RW (Right Wing) | [54.4, 68.0] | 4..5 |

Boundary equality: `Y == 27.2f` → C (the boundary belongs to the
higher-index bin). `Y == 68.0f` → RW (terminal-bin right edge is
inclusive). Out-of-range values (`Y < 0` or `Y > 68.0f`) cannot
occur post-clamp (§3.7 step 6 / FR-PA-046).

### 3.4.2 Hysteresis

Lane transitions commit only when the agent crosses the boundary
plus `LANE_HYSTERESIS_M = 2.0 m` `[GT]` (Appendix A.4). Dwell time is not
required for lane (LANE_HYSTERESIS_M alone is sufficient to
suppress oscillation).

### 3.4.3 Constraints

- **Soft (FR-PA-026):** at most two agents per lane in the
  midfield third (`x ∈ [35, 70] m`). Violation incurs a
  `SOFT_LANE_OVERLOAD_COST = 0.5` `[GT]` penalty in the spacing
  cost function (§3.6).
- **Hard (FR-PA-027):** at most three agents per lane anywhere.
  Violation triggers cost-based displacement (§3.6) to evict the
  third occupant.

## 3.5 Context Modifiers

**Semantics convention (AR-S1-01):** "compactness" is a *tightness*
scalar — **higher → tighter shape, lower → looser shape**. The
§3.5.2 application formula divides by the scalar so that higher
compactness yields smaller displacement from centroid. All gain
signs and worked examples below are aligned to this convention.

### 3.5.0 Centroid Definition (AR-S1-13)

The centroid used by §3.5.2 is the mean of `agent.position` over
own-team outfield agents that are `isActive` (FR-PA-036 filter
applied) at tick start. GK is excluded:

```
N = count(agent in ownTeamOutfield where agent.isActive)
centroid.x = (1/N) * Σ agent.position.x
centroid.y = (1/N) * Σ agent.position.y
```

The centroid is computed once per tick at tick start, before the
per-agent loop. Using `agent.position` (not `anchor[agent]`) makes
the centroid game-state aware — when the whole shape has drifted
upfield in possession, the compactness rescale operates around the
current centroid, not the static anchor centroid.

### 3.5.1 Composition

Compactness scalars are composed multiplicatively. Higher values
denote a tighter shape:

```
lateralCompactness  = baseLateral[phase]
                      * (1 + SCORE_ATK_GAIN * clamp(scoreDiff, -3, +3))
                      * (1 - FATIGUE_LATERAL_RELAX * teamMeanFatigue)

verticalCompactness = baseVertical[phase]
                      * (1 + INTENSITY_VERTICAL_GAIN * tacticalIntensity)
```

`baseLateral[phase]` and `baseVertical[phase]` are 4-row `[GT]`
lookups in §6.1. Gains:

- `SCORE_ATK_GAIN = 0.05` `[GT]` — each goal lead **raises**
  compactness by 5% → shape **tightens** under §3.5.2 division.
- `FATIGUE_LATERAL_RELAX = 0.15` `[GT]` — fully fatigued team
  **lowers** lateral compactness by 15% → shape **loosens** under
  §3.5.2 division.
- `INTENSITY_VERTICAL_GAIN = 0.20` `[GT]` — higher intensity
  raises vertical compactness → shape tightens vertically.

### 3.5.2 Application

Compactness scalars rescale the spread of the slot set around the
centroid. The formula operates on `(baseSlot − centroid)` where
`baseSlot = anchor + offset` from §3.1+§3.2 (AR-S1-05 alignment
with §3.11 pseudocode):

```
foreach (agent in ownTeamOutfield where agent.isActive) {
    rel = baseSlot[agent] - centroid
    rel.y *= baseLateral[phase]  / lateralCompactness     // higher compactness → tighter
    rel.x *= baseVertical[phase] / verticalCompactness    // higher compactness → tighter
    baseSlot[agent] = centroid + rel
}
```

Note the **inverted ratio** relative to v0.1 (AR-S1-01): scaling
by `base/compactness` means a compactness scalar of `1.10` reduces
`rel` by a factor of `1/1.10 ≈ 0.909` → shape tightens by ~9.1%.
This matches the prose intent at all sites.

### 3.5.3 Worked Example

Phase = `InPoss`, `baseLateral[InPoss] = 1.00`, `scoreDiff = +2`,
`teamMeanFatigue = 0.40`:

```
lateralCompactness = 1.00 × (1 + 0.05 × 2) × (1 − 0.15 × 0.40)
                   = 1.00 × 1.10 × 0.94
                   = 1.034
rescale factor     = baseLateral / lateralCompactness
                   = 1.00 / 1.034
                   = 0.9671
```

Team is leading by 2 and moderately fatigued. Net lateral rescale
is `0.9671` — agents move 3.29% closer to centroid (tighter
lateral shape). This matches §3.5.1's stated direction ("each goal
lead tightens"; "fatigue loosens") and the AR-S1-01 fix.

**Directional check (T-U-063 reference, AR-S1-15):**
- `scoreDiff = +2, fatigue = 0` → factor `1.00 / 1.10 = 0.909`
  (tighter than baseline).
- `scoreDiff = 0, fatigue = 0.40` → factor `1.00 / 0.94 = 1.064`
  (looser than baseline).
- Combined (this example): factor `0.9671` — net tighter
  (`SCORE_ATK_GAIN × 2 > FATIGUE_LATERAL_RELAX × 0.40`).

## 3.6 Spacing Constraints

### 3.6.1 Hard Spacing

```
MIN_AGENT_SEPARATION_M    = 1.5    [FIXED]   (from #3 collision radius)
MIN_AGENT_SEPARATION_M_SQ = 2.25   [DERIVED] (= MIN_AGENT_SEPARATION_M^2)
SPACING_EPSILON_M2        = 1e-4   [FIXED]   (KD-16)
SPACING_MAX_PASSES        = 4      [GT]      (AR-S1-06 convergence cap)
```

For every ordered pair `(i, j)` with `i.entityId < j.entityId`:

```
distSq = (slot[i] - slot[j]).sqrMagnitude
if (distSq + SPACING_EPSILON_M2 < MIN_AGENT_SEPARATION_M_SQ) {
    // violation — apply cost-based displacement (§3.6.3)
}
```

**Iteration to fixed point (AR-S1-06):** the pair scan is repeated
in canonical pair order up to `SPACING_MAX_PASSES = 4` passes per
tick. A pass that produces zero displacements terminates early. If
all four passes produce at least one displacement, the tick emits
the post-pass-4 state and a dev-log warning
`POSITIONING_SPACING_NONCONVERGENT` is recorded; the slot set is
still digested (#16 §6.2). Three-agent collisions (e.g. centroid
pull tripling up CBs) typically converge in 2 passes; the cap
exists to bound worst-case work, not as a normal-path target.

### 3.6.2 Soft Spacing

Two agents sharing `(line, lane)` incur a cost:

```
softCost(i, j) = SOFT_LANE_OVERLOAD_COST if line[i]==line[j] && lane[i]==lane[j]
                 else 0
```

`SOFT_LANE_OVERLOAD_COST = 0.5` `[GT]`. The cost feeds into §3.6.3
displacement selection.

### 3.6.3 Cost-Based Displacement (KD-14)

When pair `(i, j)` violates hard spacing:

```
cost(k) = |slot[k] - anchor[k]|²    for k ∈ {i, j}
displaceTarget = argmin_k cost(k)        (smaller required move displaces)
if (|cost(i) - cost(j)| < SPACING_EPSILON_M2) {
    displaceTarget = max(i.entityId, j.entityId)    // EntityId terminal tie-break
}
```

The displaced agent moves along the unit vector
`(slot[displaceTarget] − slot[other])` normalised, by exactly
`sqrt(MIN_AGENT_SEPARATION_M_SQ) − sqrt(distSq) + SPACING_EPSILON_M`
metres, then is clamped to pitch bounds (§3.7 step 6).

### 3.6.4 Worked Example

Agents A (EntityId 7) and B (EntityId 11) compute slots
`(50.0, 30.0)` and `(50.8, 30.6)`. `distSq = 0.64 + 0.36 = 1.0
m²`. Violation: `1.0 < 2.25`.

```
cost(A) = |(50.0, 30.0) − anchor_A|² = 0.4 m²
cost(B) = |(50.8, 30.6) − anchor_B|² = 0.9 m²
```

`cost(A) < cost(B)` → A is displaced (smaller required move). The
displacement vector is the unit vector from B to A:

```
(slot[A] − slot[B]) = (−0.8, −0.6),  ||..|| = 1.0
unit                = (−0.8, −0.6)
displaceMag         = sqrt(2.25) − sqrt(1.0) + 0.01
                    = 1.5 − 1.0 + 0.01 = 0.51 m
A.newSlot           = (50.0 + 0.51·−0.8, 30.0 + 0.51·−0.6)
                    = (49.59, 29.69)
```

Post-displacement check: distance from B `(50.8, 30.6)` to A's new
slot `(49.59, 29.69)` is `sqrt(1.4641 + 0.8281) = sqrt(2.2922) ≈
1.5140 m` — just above 1.5 m, OK.

**Line/lane state (AR-S1-14):** because §3.7 step 7 now resolves
line/lane AFTER spacing displacement (AR-S1-03 fix), `lastLane[A]`
records A's POST-displacement lane. Pre-displacement A was at
y = 30.0 (lane C, `27.2 ≤ 30.0 < 40.8`); post-displacement A is at
y = 29.69 (still lane C). The hysteresis-state entry committed for
this tick is `lastLane[A] = C` and is consistent with the emitted
`formationSlot[A] = (49.59, 29.69)`.

Under the v1.0 EntityId-based rule, B would always have moved
instead — KD-14 inverts that fairness defect.

## 3.7 Slot Composition (Stage 0)

Per tick, in canonical EntityId-ascending order. Step order revised
in v0.2 to commit line/lane state AFTER spacing displacement
(AR-S1-03):

1. Compute baseline anchor (§3.1).
2. Apply ball-relative offset (§3.2).
3. Apply context modifiers (§3.5) — operates on `baseSlot −
   centroid`, with `isActive`-filtered centroid per §3.5.0.
4. Enforce hard spacing with cost-based displacement (§3.6),
   iterated up to `SPACING_MAX_PASSES`.
5. Clamp to pitch bounds with 0.5 m touchline margin (FR-PA-033).
6. Resolve line/lane membership with hysteresis (§3.3, §3.4) —
   classification reads the final post-displacement, post-clamp
   slot so the digested `HysteresisState` (FR-PA-038) matches the
   emitted slot exactly.
7. Write `formationSlot[entityId]` into the output buffer for the
   orchestrator to forward into #8 (per §4.4.3, by writing the
   `TacticalContext.FormationSlot` field directly).

Inactive agents (substituted, red-carded) are filtered before
step 1 and receive the `SENTINEL_NO_SLOT` value (§2.4 / AR-S1-07).

No Stage 0 step performs #13 Press, #14 Mark, or #15 Run override.
KD-13: those compositor slots are declared in §7 only.

## 3.8 Hysteresis (Binding to #2 §3.1)

Anchor, line, lane, and phase hysteresis all parameterise the
dwell-time + dead-zone pattern from Agent Movement #2 §3.1. #12
does not redefine the algorithm.

Parameters (all `[GT]` per §6.1 catalogue and Appendix A.1–A.6; promoted from `[EST]` at APPROVED transition May 18, 2026):

| Constant | Value | Unit | Domain |
|---|---|---|---|
| `ANCHOR_DWELL_TICKS` | 5 | tick (500 ms) | anchor change |
| `LINE_HYSTERESIS_M` | 3.0 | m | line boundary dead zone |
| `LINE_DWELL_TICKS` | 5 | tick | line commit |
| `LANE_HYSTERESIS_M` | 2.0 | m | lane boundary dead zone |
| `PHASE_HYSTERESIS_TICKS` | 3 | tick | phase commit |
| `PHASE_LOOSE_VELOCITY_THRESHOLD` | 4.0 | m/s | loose-ball direction |

## 3.9 Determinism (Binding to #16)

- **Iteration order:** outfield agents iterated in EntityId
  ascending order (#16 §3.2.5). GK handled before outfielders (its
  slot is independent of the partition).
- **RNG domain tag:** any stochastic micro-jitter uses
  `DeterministicRngService` with domain tag
  `DOMAIN_TAG_POSITIONING_AI = 0x17` `[CROSS: #16 §3.4]` —
  ERR-012-001 resolved May 18, 2026; allocated in #16 §3.4 v1.0.5
  (value shifted from `0x16` to `0x17` on May 16, 2026 after #10
  claimed `0x16` via ERR-010-001). Stage 0 §3 has no current
  stochastic step — the field is declared so Stage 1+ extensions
  inherit the tag without re-litigation.
- **Digest scope:** per-agent `formationSlot` and the full
  `HysteresisState` struct contribute to the per-tick digest at
  #16 §6.2 scope (tactical-AI outputs).

## 3.10 Constants Catalogue (Forward Reference to §6.1)

All constants used by §3.0–§3.9 are catalogued in §6.1 with
their tags and source-of-truth references. They live in
`src/PositioningAI/PositioningAIConstants.cs` (KD-17, FR-PA-011).

## 3.11 Pseudocode — Per-Tick Main Loop

```
void PositioningAITick(
    in PerceptionSnapshot perception,
    in ContextModifierInputs modifiers,
    in FormationArchetype archetype,
    ref HysteresisState hyst,
    Span<Vector2> outSlots)              // length 22; written in-place
{
    // F1 — stale perception
    if (perception.tickIndex < currentTick) {
        outSlots = prevTickSlots;        // FR-PA-042
        return;
    }

    // F2 — invalid archetype
    if (!archetype.IsValid) archetype = FAMILY_4_4_2;     // FR-PA-043

    // Phase (§3.0)
    Phase candidate = ClassifyPhase(perception, ref hyst);
    Phase phase = CommitPhaseWithHysteresis(candidate, ref hyst);

    // Centroid (§3.5.0 — own-team outfield, isActive filtered, GK excluded)
    Vector2 centroid = ComputeCentroidActive(perception);

    // Per-agent compute, EntityId-sorted
    foreach (var id in perception.OutfieldIdsAscending) {
        // AR-S1-07: inactive (substituted / red-carded) → SENTINEL, skip
        if (!perception.agents[id].isActive) {
            outSlots[id.Index] = SENTINEL_NO_SLOT;        // FR-PA-036
            continue;
        }

        Vector2 anchor   = ComputeAnchor(archetype, id);
        Vector2 offset   = ComputeBallRelativeOffset(perception.ball, id, archetype, phase);
        Vector2 baseSlot = anchor + offset;

        // §3.5 context modifiers, operating on (baseSlot - centroid)
        baseSlot = ApplyContextModifiers(baseSlot, centroid, modifiers, phase);

        // F3 — NaN guard (FR-PA-044). SENTINEL paths exited above, so
        // any NaN here is a genuine intermediate fault → fall back to
        // raw anchor, not to SENTINEL.
        if (float.IsNaN(baseSlot.x) || float.IsNaN(baseSlot.y))
            baseSlot = anchor;

        outSlots[id.Index] = baseSlot;
    }

    // GK (§3.3.3) — always active at Stage 0; treated separately
    outSlots[gk.Index] = ComputeGkSlot(perception.ball);

    // §3.6 hard spacing — iterate to fixed point up to SPACING_MAX_PASSES
    EnforceHardSpacingIterated(outSlots, archetype, anchors,
                               SPACING_MAX_PASSES);

    // F5 — pitch-bound clamp
    for (int i = 0; i < 22; i++) {
        if (IsSentinel(outSlots[i])) continue;             // AR-S1-07
        outSlots[i] = ClampToPitch(outSlots[i]);
    }

    // §3.7 step 6 — resolve line/lane AFTER spacing+clamp so digested
    // HysteresisState matches the emitted slot (AR-S1-03)
    foreach (var id in perception.OutfieldIdsAscending) {
        if (IsSentinel(outSlots[id.Index])) continue;
        ResolveLineWithHysteresis(id, outSlots[id.Index], archetype, ref hyst);
        ResolveLaneWithHysteresis(id, outSlots[id.Index], ref hyst);
    }
}
```

The function is pure over its inputs and the prior `HysteresisState`
(FR-PA-037). The `ref hyst` mutation is the only side effect; the
mutated state is itself authoritative and digested (FR-PA-038).

**`SENTINEL_NO_SLOT`:** defined as `Vector2.NegativeInfinity`
(both components `−∞`). Distinct from NaN; F3 NaN guard does not
rewrite the sentinel. Pitch-clamp (F5) skips the sentinel. The
orchestrator treats `IsSentinel(slot) == true` as "no slot this
tick" and does not write into `TacticalContext.FormationSlot` for
that agent (§4.4.3).

## 3.12 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. §3.0–§3.11 published with worked examples per FR-PA-041. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-01 §3.5 compactness formula inverted to match prose ("higher = tighter") — §3.5.2 now `rel *= base/compactness`, §3.5.3 worked example replayed; AR-S1-02 §3.3.1 per-archetype `lineCutIndices` + AM override for 4-2-3-1; AR-S1-03 §3.7 step order: line/lane resolved AFTER spacing+clamp; AR-S1-05 §3.5.2 now operates on `(baseSlot − centroid)` aligning with §3.11 pseudocode; AR-S1-06 spacing iterates up to `SPACING_MAX_PASSES = 4`; AR-S1-07 `SENTINEL_NO_SLOT = Vector2.NegativeInfinity` distinct from NaN; isActive filter added in §3.11; AR-S1-09 §3.0.3/§3.0.4 commit on the Nth (not N+1th) candidate tick; AR-S1-10 §3.2.2 "8 m" → 7.2 m corrected to formula; AR-S1-11 GK constants demoted `[GT]` → `[EST]`; AR-S1-12 lane bins declared as `LANE_EDGES_M` literal array with explicit boundary semantics; AR-S1-13 §3.5.0 centroid definition added; AR-S1-14 §3.6.4 worked example records post-displacement lane state. |
| 0.3 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): §3.9 RNG domain tag — corrected value `0x16` → `0x17` and promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]`; ERR-012-001 resolved; value-shift history documented. |
| 0.4 | May 18, 2026 | AI agent (adversarial-specs-review-run3) | Run 3 adversarial fix pass (FAIL-6): 11 body-text `[EST]` constants promoted to `[GT]` to match §6.1 catalogue (PHASE_LOOSE_VELOCITY_THRESHOLD, PHASE_HYSTERESIS_TICKS, OFFSET_RANGE_X_M, OFFSET_RANGE_Y_M, LINE_HYSTERESIS_M, LINE_DWELL_TICKS, LANE_HYSTERESIS_M, GK_DEPTH_M, GK_ADVANCE_FACTOR, GK_LATERAL_FACTOR, SPACING_MAX_PASSES); §3.3.3 GK prose updated to reflect #11 APPROVED (May 18, 2026) and [GT] promotion; §3.8 table-header corrected from "all [EST]" to "all [GT]"; Appendix A.N citations added to formula-context inline tags. |
