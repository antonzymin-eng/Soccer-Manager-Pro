# Positioning AI Specification #12 — Section 5: Test Plan

**Created:** May 15, 2026
**Last Updated:** June 13, 2026 (v0.3 — ERR-012-006: T-T-001 clarified as an absolute deep-line threshold, not an OutOfPoss-vs-InPoss comparison)
**Version:** 0.3
**Status:** DRAFT

---

The test plan binds to Testing Strategy #19 §3 (test taxonomy) and
§4 (FR traceability framework).

## 5.1 Test Counts

| Category | Target | Source |
|---|---|---|
| Unit (anchor, offset, line, lane, hysteresis, spacing, directional invariants) | ≥48 | §3.1–§3.8 |
| Integration (full-team shape under phase transitions) | ≥11 | §3.7 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Tactical-correctness scenarios | ≥6 | Appendix B (one per archetype × 2 phases) |
| **Total** | **≥74** | — |

## 5.2 Unit Test List (representative)

### 5.2.1 Anchor (§3.1)
- **T-U-001** Anchor at neutral ball matches Appendix B table within ±0.01 m.
- **T-U-002** Anchor mirroring under defending-side orientation is exact.
- **T-U-003** Anchor for every (archetype × role) combination is unique within ±0.5 m.

### 5.2.2 Ball-Relative Offset (§3.2)
- **T-U-010** Basis function clamps at ball at corner (0, 0): `basisX = −1`, `basisY = −1`.
- **T-U-011** Basis function is zero at pitch center: `basisX(52.5) = 0`, `basisY(34) = 0`.
- **T-U-012** Offset zero at center for every role/phase pair.
- **T-U-013** Worked example §3.2.2 reproduces exactly to within ±0.01 m.

### 5.2.3 Phase Classification (§3.0)
- **T-U-020** Possession-flip own→opp commits to `OutOfPoss` immediately on next tick.
- **T-U-021** Loose ball with `vx_filtered > +4 m/s` produces candidate `TransToAtk`.
- **T-U-022** Phase hysteresis: oscillating candidate at boundary stays in `lastPhase` for at least `PHASE_HYSTERESIS_TICKS = 3` ticks.

### 5.2.4 Line / Lane Membership (§3.3, §3.4)
- **T-U-030** k=3 partition is stable under EntityId reordering.
- **T-U-031** GK excluded from line partition (FR-PA-035).
- **T-U-032** Line hysteresis: oscillating agent at boundary stays in original line for ≥5 ticks (`LINE_DWELL_TICKS`).
- **T-U-033** Lane hysteresis: agent crossing boundary by < `LANE_HYSTERESIS_M` does not flip lane.
- **T-U-034** Hard lane constraint: a 4th agent in a lane is displaced (§3.6 path).
- **T-U-035** *(AR-S1-02 per-archetype cuts)* Defense/Midfield/Attack cardinalities match the archetype: 4-4-2 → 4/4/2, 4-3-3 → 4/3/3, 4-2-3-1 → 4/5/1 (with AM in Midfield via override).
- **T-U-036** *(AR-S1-12 lane boundaries)* `Y == 27.2f` classifies as lane C (boundary belongs to higher-index bin); `Y == 68.0f` classifies as lane RW (terminal right edge inclusive); `Y == 13.6f − 1 ULP` classifies as LW.
- **T-U-037** *(AR-S1-03 post-spacing lane)* Agent displaced from `Y = 30.0` to `Y = 29.6` by §3.6.3 has `lastLane` committed against the post-displacement Y (still C), not the pre-displacement Y.

### 5.2.5 Spacing (§3.6)
- **T-U-040** Hard spacing violation at `distSq = 1.0 < 2.25` triggers displacement.
- **T-U-041** Cost-based tie-break: agent with smaller `|slot − anchor|²` is displaced.
- **T-U-042** EntityId terminal tie-break activates only when `|cost(i) − cost(j)| < SPACING_EPSILON_M2`.
- **T-U-043** Float epsilon: comparison at boundary `±0.5 cm` is stable across float ULP noise.
- **T-U-044** *(AR-S1-06 convergence)* Three-agent collision (A, B, C all within 1.0 m of a common point) resolves to all pairwise distances `≥ 1.5 m − ε` within `SPACING_MAX_PASSES = 4` passes.
- **T-U-045** *(AR-S1-06 non-convergence path)* Pathological five-agent collision that requires `> 4` passes emits `POSITIONING_SPACING_NONCONVERGENT` dev-log warning and still produces a finite, in-bounds, digested slot set.

### 5.2.6 Failure Modes (§2.4)
- **T-U-050** F1 stale perception: previous-tick output is reused.
- **T-U-051** F2 invalid archetype index → fallback to 4-4-2.
- **T-U-052** F3 NaN in offset → replaced with raw anchor.
- **T-U-053** F4 mid-tick input change is deferred to next tick.
- **T-U-054** F5 out-of-bounds slot is clamped with 0.5 m margin.
- **T-U-055** F6 corrupted phase enum → fallback to `InPoss`.

### 5.2.7 Context Modifiers (§3.5)
- **T-U-060** `scoreDiff = +2`, `fatigue = 0.4`, `InPoss` produces `lateralCompactness = 1.034 ± 0.001`.
- **T-U-061** Fatigue convention is `0 = rested` (regression for the historical inversion bug).
- **T-U-062** Score clamp: `scoreDiff = +5` clamps to `+3`.
- **T-U-063** *(AR-S1-15 directional invariant)* Under `scoreDiff = +2, fatigue = 0` the team-mean `|rel.y|` over own-team active outfield strictly DECREASES vs. baseline `(0, 0)`. Reciprocal: `scoreDiff = 0, fatigue = 1` INCREASES `|rel.y|`. Vertical pair: `tacticalIntensity = 1` DECREASES `|rel.x|` vs. baseline `0`. Catches sign-inverted compactness application (AR-S1-01).
- **T-U-064** §3.5.0 centroid is computed over `isActive` outfield only: with one substituted agent at `(0, 0)`, centroid coincides with the 10-agent active mean — not the 11-agent mean.
- **T-U-065** Compactness rescale uses `(baseSlot − centroid)` not `(anchor − centroid)`: under non-zero ball offset, scaling factor `< 1` reduces displacement from centroid for the post-offset slot, not for the raw anchor (AR-S1-05 alignment of §3.5.2 / §3.11).

### 5.2.8 Hysteresis (§3.8)
- **T-U-070** Anchor dwell counter increments per tick.
- **T-U-071** Anchor change resets `anchorDwellTicks` to 0.
- **T-U-072** `HysteresisState` round-trip through digest is exact.

## 5.3 Integration Test List

- **T-I-001** Each archetype × each phase (3 × 4 = 12 cells) produces zero hard-spacing violations over a 100-tick window.
- **T-I-002** Phase boundary crossings produce no oscillation over a 50-tick window across each archetype.
- **T-I-003** Full 4-4-2 vs 4-3-3 match opening 30 seconds (300 ticks): every produced slot is finite, in-bounds.
- **T-I-004** Substitution event: substituted agent's slot transitions to `SENTINEL_NO_SLOT = Vector2.NegativeInfinity` (AR-S1-07; NOT `(NaN, NaN)`, which would be rewritten by the F3 NaN guard) on the tick following the substitution. Orchestrator-side: `TacticalContext.FormationSlot` for the substituted agent retains its pre-substitution value.
- **T-I-005** Red-card: same behavior as substitution.
- **T-I-006** F2 fallback: simulation continues without crash when archetype index is corrupted mid-match (Stage 1+ regression — Stage 0 archetype is fixed per FR-PA-039 but the fallback path must still be exercised).
- **T-I-007** 4-3-3 archetype against centroid pull: AM offset matches §3.2.2 worked example within ±0.05 m.
- **T-I-008** Lane overload: forcing 4 agents into one lane resolves to ≤3 within 1 tick. *(AR-S1-03: resolution uses §3.6 spacing cost-based displacement; line/lane is committed AFTER spacing, so `lastLane[]` records the post-resolution lanes.)*
- **T-I-011** *(AR-S1-04 orchestrator contract)* Per-tick orchestrator path does NOT invoke `TacticalContext.Stage0Default()`. After 100 ticks, `PressingInstruction`, `PassingInstruction`, and `DefensiveLineDepth` retain values written by external test fixtures — not reset to Stage 0 defaults.
- **T-I-009** Hysteresis state survives a save/restore round-trip (#16 §3.2 binding).
- **T-I-010** Pure-function property: identical `(perception, modifiers, archetype, prevHysteresisState)` produces bit-identical output across two invocations.

## 5.4 Determinism Regression (Binding to #16 §5)

- **T-D-001** 90-minute match replay on reference host produces bit-identical per-tick digest over two runs (same seed).
- **T-D-002** EntityId-permuted input produces identical post-iteration state (#16 §3.2.5 binding).
- **T-D-003** `HysteresisState` digest contribution is non-empty for every archetype.
- **T-D-004** Cross-run digest stability: 10 consecutive 90-minute runs produce the same final-tick digest.
- **T-D-005** Save / load mid-match: post-load tick 1 digest matches pre-save tick (N+1) digest.
- **T-D-006** RNG domain tag isolation: removing `DOMAIN_TAG_POSITIONING_AI` calls from another spec's RNG stream leaves #12's stream unchanged.

## 5.5 Performance Validation (Binding to §6)

- **T-P-001** Per-tick wall-clock ≤ 0.15 ms on the named reference host (§6.3).
- **T-P-002** Zero heap allocations on the hot path under .NET allocation tracker (FR-PA-006, #18 §3.7).
- **T-P-003** 22² spacing pass measurable cost ≤ 0.05 ms on reference host.

## 5.6 Tactical-Correctness Scenarios (Binding to Appendix B)

- **T-T-001** 4-4-2 `OutOfPoss` against opponent attacking own third: defensive line at `x ≤ 25 m` for all 4 defenders. **(ERR-012-006 clarification, June 13, 2026):** this is an ABSOLUTE deep-line threshold with the ball in the own defensive third — the deep line is a §3.2 ball-relative pull (OutOfPoss defender `pullFactor.x` > InPoss). It is NOT an OutOfPoss-vs-InPoss centroid comparison: at a center ball §3.2 contributes zero offset and the only phase effect is §3.5 vertical compactness, which compresses the shape toward the centroid and RAISES the deepest line's mean X. Assert the `x ≤ 25 m` threshold, not `avgDefXOut < avgDefXIn`.
- **T-T-002** 4-3-3 `InPoss` deep build-up: wingers `lane ∈ {LW, RW}` for all 10 ticks.
- **T-T-003** 4-2-3-1 `TransToAtk`: AM advances ≥ 5 m relative to `OutOfPoss` baseline.
- **T-T-004** 4-4-2 `TransToDef`: full team retreats; centroid moves ≥ 8 m toward own goal within 10 ticks.
- **T-T-005** 4-3-3 `InPoss` ball on right wing: shape compresses toward right; team-centroid `y > 36 m`.
- **T-T-006** 4-2-3-1 `OutOfPoss`: no lane overload (≤2 in midfield third, FR-PA-026) over 100 ticks.

## 5.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. Added T-U-035..T-U-037 (archetype line cuts, lane boundary semantics, post-spacing lane); T-U-044/T-U-045 (spacing convergence + non-convergent fallback); T-U-063..T-U-065 (compactness directional invariants, isActive centroid, baseSlot subject); T-I-004 sentinel correction; T-I-011 orchestrator non-invocation of `Stage0Default()`. Unit target ≥48; integration target ≥11; total ≥74. |
| 0.3 | June 13, 2026 | AI agent (dotnet-CI quarantine adjudication) | ERR-012-006: T-T-001 clarified — the `x ≤ 25 m` deep-line condition is ABSOLUTE (ball in own defensive third; §3.2 ball-relative pull), not an OutOfPoss-vs-InPoss comparison. The implemented `TacticalCorrectness_OutOfPoss_DefensiveLineCompact` test had invented such a comparison with a center ball (unsatisfiable under §3.5 vertical compactness) and was corrected to the absolute threshold. |
