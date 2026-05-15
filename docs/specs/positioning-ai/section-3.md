# Positioning AI Specification #12 — Section 3: Core Formulas and Algorithms

**Created:** May 15, 2026
**Last Updated:** May 15, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.2)
**Version:** 0.1
**Status:** DRAFT

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

`V₀ = 4.0 m/s` `[EST]` (`PHASE_LOOSE_VELOCITY_THRESHOLD`). The
3-tick moving average filters tactical ball touches from genuine
transitions.

### 3.0.3 Hysteresis

A candidate transition commits only if it persists for
`PHASE_HYSTERESIS_TICKS = 3` `[EST]` consecutive ticks. While
candidate ≠ lastPhase, `phaseDwellTicks` counts up; on a return to
the prior phase before reaching the threshold, `phaseDwellTicks`
resets to 0.

### 3.0.4 Worked Example

Tick T: own team loses possession, ball loose, `ball.vx_filtered =
−5.2 m/s`. Candidate = `TransToDef`. If `lastPhase = InPoss` and
`phaseDwellTicks` ∈ {1, 2}: output remains `InPoss`. At tick T+3
with the same candidate sustained: output flips to `TransToDef`,
`phaseDwellTicks` reset to 0.

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

`OFFSET_RANGE_X_M = 12.0 m` `[EST]`, `OFFSET_RANGE_Y_M = 8.0 m`
`[EST]`. `pullFactor[role, phase]` is a `[GT]` lookup keyed on
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

AM anchor pulls back ≈4.5 m toward own goal — consistent with the
outline's "AM anchor pulls back 8m" intent at extremes; 4.5 m is
the linear-interpolation value at the 20 m ball position. Full pull
of 8 m would require ball at x = 0.

## 3.3 Line Membership

### 3.3.1 Algorithm

Outfield agents (GK excluded — FR-PA-035) are sorted ascending by
`agent.x` (own-orientation). The stable k=3 partition cuts at
indices 3 and 7 of the 10-agent ordering:

- **Defense:** indices [0..3) → 4 agents (extended back line)
- **Midfield:** indices [3..7) → 4 agents
- **Attack:** indices [7..10) → 3 agents

For other archetype shapes (4-3-3 has 4/3/3; 4-2-3-1 has 4/5/1
after grouping AM with midfield), the cuts are archetype-specific
`[GT]` indices, published per archetype in §3.10.

### 3.3.2 Hysteresis

A transition from `lastLine` to a new line commits only if the
agent's longitudinal distance from the line boundary exceeds
`LINE_HYSTERESIS_M = 3.0 m` `[EST]` and persists for
`LINE_DWELL_TICKS = 5` `[EST]` consecutive ticks.

### 3.3.3 Goalkeeper Slot

The GK is excluded from the line partition. The GK slot is:

```
gkSlot.x = GK_DEPTH_M  + GK_ADVANCE_FACTOR * basisX(ball.x_clamped)
gkSlot.y = PITCH_WIDTH_M / 2 + GK_LATERAL_FACTOR * basisY(ball.y)
```

`GK_DEPTH_M = 5.5 m` `[GT]` (rest depth from own goal-line);
`GK_ADVANCE_FACTOR = 8.0 m` `[GT]`; `GK_LATERAL_FACTOR = 2.0 m`
`[GT]`. Detailed GK behavior is specified in #11 Goalkeeper
Mechanics; #12 produces only the resting baseline.

## 3.4 Lane Occupation

### 3.4.1 Classification

Five lateral bins of width `PITCH_WIDTH_M / 5 = 13.6 m`:

| Lane | Y range (m) |
|---|---|
| LW (Left Wing) | [0.0, 13.6) |
| LH (Left Half) | [13.6, 27.2) |
| C (Center) | [27.2, 40.8) |
| RH (Right Half) | [40.8, 54.4) |
| RW (Right Wing) | [54.4, 68.0] |

### 3.4.2 Hysteresis

Lane transitions commit only when the agent crosses the boundary
plus `LANE_HYSTERESIS_M = 2.0 m` `[EST]`. Dwell time is not
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

### 3.5.1 Composition

Compactness scalars are composed multiplicatively:

```
lateralCompactness = baseLateral[phase]
                     * (1 + SCORE_ATK_GAIN * clamp(scoreDiff, -3, +3))
                     * (1 - FATIGUE_LATERAL_RELAX * teamMeanFatigue)

verticalCompactness = baseVertical[phase]
                      * (1 + INTENSITY_VERTICAL_GAIN * tacticalIntensity)
```

`baseLateral[phase]` and `baseVertical[phase]` are 4-row `[GT]`
lookups in §3.10. Gains:

- `SCORE_ATK_GAIN = 0.05` `[GT]` (each goal up tightens by 5%).
- `FATIGUE_LATERAL_RELAX = 0.15` `[GT]` (fully fatigued team relaxes
  lateral compactness by 15%). `FATIGUE_LATERAL_RELAX_M = 4.0 m`
  `[GT]` is the absolute lateral spread cap added by full fatigue.
- `INTENSITY_VERTICAL_GAIN = 0.20` `[GT]`.

### 3.5.2 Application

Compactness scalars rescale the spread of the anchor set around
its centroid:

```
foreach (agent in ownTeamOutfield) {
    rel = anchor[agent] - centroid
    rel.y *= lateralCompactness  / baseLateral[phase]
    rel.x *= verticalCompactness / baseVertical[phase]
    anchor[agent] = centroid + rel
}
```

### 3.5.3 Worked Example

Phase = `InPoss`, `baseLateral[InPoss] = 1.00`, `scoreDiff = +2`,
`teamMeanFatigue = 0.40`:

```
lateralCompactness = 1.00 × (1 + 0.05 × 2) × (1 − 0.15 × 0.40)
                   = 1.00 × 1.10 × 0.94
                   = 1.034
```

Team is leading by 2 and moderately fatigued — net 3.4% tighter
lateral shape.

## 3.6 Spacing Constraints

### 3.6.1 Hard Spacing

```
MIN_AGENT_SEPARATION_M    = 1.5    [FIXED]   (from #3 collision radius)
MIN_AGENT_SEPARATION_M_SQ = 2.25   [DERIVED] (= MIN_AGENT_SEPARATION_M^2)
SPACING_EPSILON_M2        = 1e-4   [FIXED]   (KD-16)
```

For every ordered pair `(i, j)` with `i.entityId < j.entityId`:

```
distSq = (slot[i] - slot[j]).sqrMagnitude
if (distSq + SPACING_EPSILON_M2 < MIN_AGENT_SEPARATION_M_SQ) {
    // violation — apply cost-based displacement (§3.6.3)
}
```

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

Since `cost(A) < cost(B)`, A is displaced (smaller required move).
The pre-displacement EntityId-7 agent moves; with the v1.0
EntityId-based rule, B would always have moved instead — KD-14
inverts this fairness defect.

## 3.7 Slot Composition (Stage 0)

Per tick, in canonical EntityId-ascending order:

1. Compute baseline anchor (§3.1).
2. Apply ball-relative offset (§3.2).
3. Apply context modifiers (§3.5).
4. Resolve line/lane membership with hysteresis (§3.3, §3.4).
5. Enforce hard spacing with cost-based displacement (§3.6).
6. Clamp to pitch bounds with 0.5 m touchline margin (FR-PA-033).
7. Write `formationSlot[entityId]` into the output buffer for the
   orchestrator to forward into #8 `TacticalContext.FormationSlot`.

No Stage 0 step performs #13 Press, #14 Mark, or #15 Run override.
KD-13: those compositor slots are declared in §7 only.

## 3.8 Hysteresis (Binding to #2 §3.1)

Anchor, line, lane, and phase hysteresis all parameterise the
dwell-time + dead-zone pattern from Agent Movement #2 §3.1. #12
does not redefine the algorithm.

Outline-stage parameters (all `[EST]`; promote to `[GT]` with
Appendix A derivations before approval):

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
  `DOMAIN_TAG_POSITIONING_AI = 0x16` `[CROSS-PENDING]` until
  `ERR-012-001` is ratified. Stage 0 §3 has no current stochastic
  step — the field is declared so Stage 1+ extensions inherit the
  tag without re-litigation.
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

    // Centroid (for §3.5 rescaling)
    Vector2 centroid = ComputeCentroid(perception, archetype);

    // Per-agent compute, EntityId-sorted
    foreach (var id in perception.OutfieldIdsAscending) {
        Vector2 anchor   = ComputeAnchor(archetype, id);
        Vector2 offset   = ComputeBallRelativeOffset(perception.ball, id, archetype, phase);
        Vector2 baseSlot = anchor + offset;

        baseSlot = ApplyContextModifiers(baseSlot, centroid, modifiers, phase);

        LineMembership line = ResolveLineWithHysteresis(id, baseSlot, ref hyst);
        LaneAssignment  lane = ResolveLaneWithHysteresis(id, baseSlot, ref hyst);

        // F3 — NaN guard
        if (float.IsNaN(baseSlot.x) || float.IsNaN(baseSlot.y))     // FR-PA-044
            baseSlot = anchor;

        outSlots[id.Index] = baseSlot;
    }

    // GK (§3.3.3)
    outSlots[gk.Index] = ComputeGkSlot(perception.ball);

    // §3.6 hard spacing pass
    EnforceHardSpacing(outSlots);

    // F5 — pitch-bound clamp
    for (int i = 0; i < 22; i++) outSlots[i] = ClampToPitch(outSlots[i]);
}
```

The function is pure over its inputs and the prior `HysteresisState`
(FR-PA-037). The `ref hyst` mutation is the only side effect; the
mutated state is itself authoritative and digested (FR-PA-038).

## 3.12 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. §3.0–§3.11 published with worked examples per FR-PA-041. |
