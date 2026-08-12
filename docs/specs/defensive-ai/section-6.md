# Defensive AI Specification #14 — Section 6: Performance Analysis and Budgets

**Created:** May 17, 2026
**Last Updated:** August 12, 2026 (v0.4 — APPROVED patch: KD-6 revised (`ERR-014-006`, wiring backlog W2) — §6.1 gains the new "Region: Tackle Outcome Resolution" (ten `[GT]` + one `[FIXED]`, exactly as tabulated at `section-3.md` §3.6.5.5))
**Version:** 0.4
**Status:** APPROVED
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## 6.1 Constant Catalogue

All `[GT]` constants are promoted from `[EST]` at outline stage. Derivation
rationale for each value is in `appendices.md` Appendix A (the formal promotion
record). All `[GT]` constants live exclusively in `DefensiveAIConstants.cs`
per FR-DA-007 / KD-14 / #20 §4.2 FR-CS-025. All `[CROSS]` constants are
consumed read-only from their authoritative spec; #14 never redefines them.

`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` — ERR-014-004 resolved May 18, 2026.
Allocated in #16 §3.4 v1.0.5. Phase B/C block final layout: `0x17` = #12, `0x1D` = #11
(shifted from `0x18` — #12 reached `APPROVED` first), `0x19` = #13, `0x1A` = #14, `0x1B` = #15.

---

### Region: Assignment Algorithm

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `MAN_MARK_CANDIDATE_RADIUS_M` | `[GT]` | 15.0 | m | Radius within which an opponent qualifies as a MAN_MARK candidate for a given agent (§3.3.3). |
| `RUNNER_VELOCITY_THRESHOLD_M_S` | `[GT]` | 3.0 | m/s | Minimum opponent speed to qualify as an INTERCEPT_RUNNER target (§3.3.3). |

### Region: Last-Man Detection

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `LAST_MAN_BALL_BUFFER_M` | `[GT]` | 5.0 | m | Ball-ahead buffer: emergency fires when `distToOwnGoal(ball) < distToOwnGoal(lastMan) + buffer` (§3.8.1). |
| `LAST_MAN_OWN_HALF_MIN_X` | `[GT]` | 5.0 | m | Minimum `distToOwnGoal(ball)` for the last-man predicate to fire; prevents constant emergency triggering deep in own area (§3.8.1). |

### Region: Offside Trap

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `OFFSIDE_BALL_SPEED_THRESHOLD_M_S` | `[GT]` | 4.0 | m/s | Ball speed ceiling for offside trap trigger condition 1. Above this speed, a through-ball can outrun the step-up (§3.7.2). |
| `OFFSIDE_STEP_SIZE_M` | `[GT]` | 3.0 | m | Forward advancement (distToOwnGoal increase) per trap execution (§3.7.4). |
| `OFFSIDE_MAX_DEPTH_M` | `[GT]` | 45.0 | m | Safety ceiling: defensive line may not advance beyond this `distToOwnGoal` value via the trap (§3.7.4). |
| `OFFSIDE_TRAP_DWELL_TICKS` | `[GT]` | 3 | ticks | Consecutive qualifying ticks required before the trap fires (§3.7.2). At 10 Hz: 300 ms minimum qualification window. |
| `OFFSIDE_RESET_COOLDOWN_TICKS` | `[GT]` | 10 | ticks | Post-trap cooldown before a new step-up may fire (§3.7.3). At 10 Hz: 1,000 ms cooldown. |
| `LINE_COHERENCE_THRESHOLD_M` | `[GT]` | 8.0 | m | Maximum x-spread of the DEFENSE-line for it to be eligible for a coordinated step-up (§3.7.2 condition 3). |

### Region: Hysteresis and Assignment Timing

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `MARK_DWELL_TICKS` | `[GT]` | 4 | ticks | Consecutive ticks a new mark candidate must be consistently preferred before the transition commits (§3.11.4). At 10 Hz: 400 ms dwell. |
| `REASSIGN_LATENCY_TICKS` | `[GT]` | 2 | ticks | Maximum ticks before an unassigned zone receives cover after a ball switch (§5.6.2 T-DA-EXP-002 criterion). At 10 Hz: 200 ms. |

### Region: Tackle Intent

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `TACKLE_ELIGIBLE_RADIUS_M` | `[GT]` | 3.0 | m | Radius within which an agent's assigned opponent qualifies for tackle intent evaluation (§3.6.2). |
| `TACKLE_COMMIT_COVERAGE_FLOOR` | `[GT]` | 1 | count | Minimum teammates behind the agent (within y-corridor) before COMMIT mode is permitted (§3.6.2). |
| `TACKLE_JOCKEY_ANGLE_RAD` | `[GT]` | 0.35 | rad (~20°) | Approach angle below which JOCKEY is preferred over HOLD when COMMIT is disallowed (§3.6.2). |
| `COVERAGE_DEPTH_CORRIDOR_M` | `[GT]` | 5.0 | m | Half-width of the y-axis corridor used to count "teammates behind" for coverage depth (§3.6.2). |

### Region: Tackle Outcome Resolution (KD-6, revised — `ERR-014-006`)

Added at the wiring backlog W2 landing (August 12, 2026). All ten `[GT]`
values are **un-calibrated**: no player in this engine had ever made a
tackle before this landing, so there was no prior behaviour to preserve
and nothing here was fitted against anything. KD-W1 permits new dials on
a dead surface and forbids tuning them at the wiring pass; they are the
calibration pass's input, not its output (§3.6.5.5).

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `TACKLE_ENGAGE_BASE` | `[GT]` | 0.10 | probability | Base P(a committed challenge connects at all), before commitment and proximity (§3.6.5.3). |
| `TACKLE_ENGAGE_COMMITMENT_K` | `[GT]` | 0.25 | probability | Added to the engage probability per unit of movement commitment, `cos(approachAngle)` clamped at 0 (§3.6.5.3). |
| `TACKLE_ENGAGE_PROXIMITY_K` | `[GT]` | 0.20 | probability | Added to the engage probability per unit of proximity to the ball (§3.6.5.3). |
| `TACKLE_FOUL_SHARE_BASE` | `[GT]` | 0.14 | share | Base share of connecting challenges that are fouls, before the Aggression and Tackling terms (§3.6.5.3). |
| `TACKLE_FOUL_SHARE_AGGRESSION_K` | `[GT]` | 0.12 | share | Added to the foul share per unit of normalized tackler Aggression (§3.6.5.3). |
| `TACKLE_FOUL_SHARE_TACKLING_K` | `[GT]` | 0.10 | share | Subtracted from the foul share per unit of normalized tackler Tackling (§3.6.5.3). |
| `TACKLE_CLEAN_SHARE_BASE` | `[GT]` | 0.30 | share | Base share of non-foul connecting challenges won cleanly rather than knocked loose (§3.6.5.3). |
| `TACKLE_CLEAN_SHARE_EDGE_K` | `[GT]` | 0.60 | share | Added to the clean-win share per unit of the tackler's ability edge over the carrier (§3.6.5.3). |
| `TACKLE_RETAIN_DRIBBLING_WEIGHT` | `[GT]` | 0.65 | weight | Weight of the carrier's Dribbling in his ability to retain the ball through a challenge (§3.6.5.3). |
| `TACKLE_RETAIN_BALANCE_WEIGHT` | `[GT]` | 0.35 | weight | Weight of the carrier's Balance in the same retain term (§3.6.5.3). |
| `TACKLE_FOUL_SHARE_CEILING` | `[FIXED]` | 0.95 | share | Numerical guarantee, not a football judgment: at a foul share of exactly 1 the second inverse transform divides by zero, making `BALL_WON`/`BALL_LOOSE` unreachable rather than unlikely (§3.6.5.5). |

### Region: Anti-Chaos Invariants

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `MIN_BACKLINE_AGENTS` | `[GT]` | 3 | count | Minimum DEFENSE-line agents that must remain in ZONAL mode after assignment. Invariant 1 in §3.10 (FR-DA-025). |
| `MAX_MAN_MARK_ASSIGNMENTS` | `[GT]` | 4 | count | Maximum simultaneous MAN_MARK assignments across the HOLD_SHAPE pool. Invariant 2 in §3.10 (FR-DA-026). |
| `MAX_MARK_DISPLACEMENT_M` | `[GT]` | 20.0 | m | Maximum displacement of a non-ZONAL assignment from the agent's #12 baseline anchor. Invariant 3 in §3.10 (FR-DA-027). |

### Region: GK Zone

| Constant | Tag | Value | Unit | Purpose |
|---|---|---|---|---|
| `GK_EXPECTED_ZONE_MIN_X` | `[GT]` | −2.0 | m (rel. to own goal-line; expressed as signed distToOwnGoal lower bound) | Lower bound of GK expected zone; allows GK to stand slightly behind goal-line (e.g., during goal-kick preparation) without triggering a false COVER_GK_ZONE override (§3.9.1). Not used in Stage 0 trigger logic (trigger uses `GK_EXPECTED_ZONE_MAX_X` upper bound only); reserved for Stage 1+ zone visualisation. |
| `GK_EXPECTED_ZONE_MAX_X` | `[GT]` | 15.0 | m (distToOwnGoal) | Maximum `distToOwnGoal` at which the GK is considered "in position". Exceeding this value triggers the COVER_GK_ZONE check when emergency is active (§3.9.1). |
| `COVER_GK_ZONE_MAX_TICKS` | `[GT]` | 20 | ticks | Safety release duration: if COVER_GK_ZONE override has been active for this many consecutive ticks, the cover agent reverts to ZONAL regardless of GK position (§3.9.2). At 10 Hz: 2,000 ms. |

### Region: Cross-Spec Constants (Consumed Read-Only)

| Constant | Tag | Value | Unit | Authoritative Source | Purpose in #14 |
|---|---|---|---|---|---|
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | 105.0 | m | Ball Physics #1 §1.2 | Denominator in `perceivedGoalProximity` formula (§3.5.2); normalises opponent x-position to [0, 1]. |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 | m | Ball Physics #1 §1.2 | Offside trap trigger condition 2: ball must be in opponent half (§3.7.2). |
| `PITCH_WIDTH_M` | `[CROSS: #1 §1.2]` | 68.0 | m | Ball Physics #1 §1.2 | `abandonedZoneCenter.y = PITCH_WIDTH_M / 2.0 = 34.0` in §3.9.2. |
| `DOMAIN_TAG_DEFENSIVE_AI` | `[CROSS: #16 §3.4]` `0x1A` | 0x1A | — | #16 §3.4 v1.0.5 (ERR-014-004 resolved May 18, 2026) | RNG domain tag for any stochastic tie-breaking in this subsystem (§4.6). Promoted to `[CROSS: #16 §3.4]` atomically with #14 `APPROVED` transition May 18, 2026. |

---

## 6.2 Hot Path Enumeration (Binding to #18 KD-10)

All operations below are on the 10 Hz tactical loop. No 60 Hz work (FR-DA-001 / KD-2).
Worst-case agent counts: `N_HOLD = 10` (all outfield minus GK), `N_OPP = 11`
(all opponents), `N_DEF = 6` (maximum DEFENSE-line in a 5-at-back formation).

| Operation | Algorithm | Complexity | Worst-case count |
|---|---|---|---|
| Phase gate read | Single enum read from #12 accessor | O(1) | 1 access |
| HOLD_SHAPE pool filter (§3.2) | Loop over perception team agents | O(N), N ≤ 22 (both teams) | 22 isActive + role checks |
| Last-man predicate (§3.8) | argmin over pool | O(N_HOLD), N ≤ 10 | 10 comparisons |
| GK zone check (§3.9) | Single float comparison | O(1) | 1 comparison |
| Emergency override (§3.8–§3.9) | Argmax over opponents + argmin cost over pool | O(N_OPP) + O(N_HOLD) | 21 evaluations |
| Hysteresis pre-check (§3.11 in §3.3) | Per-agent dwell counter check | O(N_HOLD) | 10 checks |
| Mark-mode candidate evaluation (§3.3) | Per-agent × per-opponent radius + velocity check | O(N_HOLD × N_OPP) | 100 candidate evaluations |
| Threat score computation (§3.5) | 3 arithmetic ops per candidate | O(N_HOLD × N_OPP) | 300 float ops |
| Displacement cost (§3.4) | 4 arithmetic ops per (agent, candidate) pair | O(N_HOLD × N_OPP) | 400 float ops |
| Hysteresis gate (§3.11 post-eval) | Per-agent state update | O(N_HOLD) | 10 state writes |
| Tackle intent evaluation (§3.6) | Per-agent: 1 distance check + approach angle + coverage depth | O(N_HOLD × N_HOLD) worst | ≤ 40 evaluations (only agents within 3 m; typically 2–4) |
| Offside trap dwell check (§3.7) | 4 conditions + DEFENSE-line spread check | O(N_DEF) | 6 comparisons |
| Offside trap execution (§3.7) | DEFENSE-line assignment update | O(N_DEF) | 6 assignments |
| Anti-chaos enforcement (§3.10) | 3 invariant checks × 3 passes; per-pass demote = 1 argmin O(N_HOLD) | O(3 × 3 × N_HOLD) | 90 comparisons (worst case) |
| Publish (§3.13 Step 9) | memcpy of assignment buffer | O(N_HOLD) | 10 writes |
| **Total per tick dominant term** | **O(N_HOLD × N_OPP)** | — | **≈ 800 float operations** |

**Zero-allocation guarantee:** All arrays (`HoldShapePool`, `assignments[]`,
`hysteresis[]`, `tackleRequests[]`, `offsideState`) are pre-allocated at
subsystem initialisation (Stage 1 startup). No `new` on hot path (FR-DA-006 /
#18 §3.7). All inter-module parameter passing uses `Span<T>`, `ReadOnlySpan<T>`,
and `ref` / `in` struct parameters.

---

## 6.3 Per-Tick Budget

**Target: ≤ 0.12 ms** per 10 Hz tick.

| Reference host | Value |
|---|---|
| CPU | Ryzen 7 5800X @ 4.5 GHz (single thread) |
| Runtime | Mono backend, Unity 2022.3 LTS |
| Measurement method | `System.Diagnostics.Stopwatch`-based micro-benchmark; 100 warm repetitions; discard top 5% outliers (§5.5 T-DA-PERF-001) |

**Rationale:** the 0.12 ms budget is slightly higher than Pressing AI #13's
0.10 ms budget. The additional 0.02 ms headroom reflects:
- The offside-trap dwell tracking and DEFENSE-line coherence check (§3.7):
  an additional O(N_DEF) pass absent from #13.
- The tackle-intent evaluation pass (§3.6): an additional O(N_HOLD × N_HOLD)
  pass (in practice O(K × N_HOLD) where K = 2–4 eligible agents), absent from #13.

**Per-spec §6 ratify-not-override principle (#18 KD-2):** Performance Optimization
Strategy #18 §6 may ratify this budget at Stage 1 against the actual
implementation profile. If a future hot-path audit identifies a faster
decomposition, the budget may be tightened but never silently widened without
an explicit §6 amendment in this file.

**Certification host caveat:** this budget is expressed against the named reference
host. Once `docs/tracking/certification-platform.md` is pinned by the lead
developer, the certification host budget supersedes this reference budget for
the purpose of formal acceptance testing (same carve-out as Goalkeeper Mechanics #11 /
Positioning AI #12 / Pressing AI #13). First `FR-DS-009-GATE` certification run
requires the pinned host.

---

## 6.4 Per-Frame Budget

**N/A.** Defensive AI #14 produces outputs only on the 10 Hz tactical loop
(FR-DA-001 / KD-2). No callbacks, no state reads, and no output writes occur
on the 60 Hz physics / render frame. #14 has zero per-frame budget allocation.

---

## 6.5 Memory Footprint

All sizes are Stage 0 estimates using C# value-type packing conventions per
#20 §4.2. Field sizes: `float` = 4 bytes; `int` = 4 bytes; `bool` = 1 byte
(padded to 4 in struct); `EntityId` = 4 bytes (assumed int32); `TeamId` = 1 byte
(padded to 4); `byte` = 1 byte (padded); `Vector2` = 8 bytes; `MarkMode`
enum = 1 byte (padded to 4); nullable `EntityId?` = 8 bytes (4-byte value + 4-byte
has-value flag).

| Structure | Fields | Est. size | Instances | Total |
|---|---|---|---|---|
| `MarkDirective` | `TeamId`, `float offensiveLineDepth`, `bool offsideTrapActive`, `float stepUpTargetDepth`, `bool emergencyFlag` | ~20 bytes | 2 (one per team) | 40 bytes |
| `MarkAssignment` | `EntityId agent`, `MarkMode mode`, `EntityId? targetEntityId`, `Vector2? targetPosition`, `int validThroughTick`, `bool overriddenThisTick` | ~32 bytes | 22 slots (fixed capacity) | 704 bytes |
| `MarkHysteresisState` | `MarkMode currentMode`, `int dwellCounter`, `MarkMode candidateMode`, `EntityId? candidateTargetId`, `int holdTicks` | ~24 bytes | 22 slots (fixed capacity; one per outfield agent) | 528 bytes |
| `TackleIntentRequest` | `EntityId agent`, `TackleMode mode`, `EntityId targetEntityId`, `float approachAngle`, `byte coverageDepth` | ~20 bytes | 22 slots max (one per pool agent; pre-allocated) | 440 bytes |
| `OffsideLineState` | `float currentLineDepth`, `int stepUpDwellCounter`, `int cooldownTicksRemaining`, `int coverGkZoneActiveTicks` | ~16 bytes | 2 (one per team) | 32 bytes |
| `HoldShapePool` (scratch per tick) | `EntityId[] poolIds` + `int count` | ~48 bytes (pool IDs: 10 × 4) | 2 (one per team per tick) | 96 bytes |
| **Total working set** | | | | **≈ 1,840 bytes (< 2 KB)** |

The working set excludes:
- The upstream perception snapshot (owned by #7; passed by reference).
- The baseline defensive shape view (owned by #12; passed by reference).
- The press assignment array (owned by #13; passed by reference).

**GC pressure:** zero at steady state. All structures are stack-allocated
`readonly struct` values or pre-allocated fixed-size arrays in
`DefensiveAITick.cs` Stage 1 initialisation. The HoldShapePool is a
pre-allocated fixed-size backing array reused each tick.

---

## 6.6 Performance Considerations for Anti-Chaos (§3.10)

The `EnforceAntiChaosInvariants` function is bounded at 3 passes × O(N_HOLD)
per pass. Worst case: 3 × 10 = 30 pool iterations per enforcement call.
Each iteration requires at most one `ThreatScore` evaluation (§3.5: 3 float
operations). Total worst-case: 90 float operations and 90 array reads.

This is dominated by the §3.3 candidate evaluation (800+ float ops) and
is not the performance bottleneck. Even in the degenerate F4 fallback case,
the `EmitAllZonal` function is an O(N_HOLD) memset — the most trivial path.

---

## 6.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial draft. Full 26-entry constant catalogue in §6.1 (22 [GT] + 4 [CROSS] / [CROSS-PENDING]). All [GT] constants promoted from [EST] at outline stage (Appendix A derivation record). Hot-path enumeration table in §6.2. Per-tick budget ≤ 0.12 ms against named reference host (§6.3). Memory footprint ≈ 1,840 bytes (§6.5). `REASSIGN_LATENCY_TICKS` added to catalogue (§5.6.2 exploit criterion). `GK_EXPECTED_ZONE_MIN_X` retained in catalogue for Stage 1+ zone visualisation even though it is not used in Stage 0 trigger logic. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. M1: §6.1 and v0.1 history row corrected "27-entry" → "26-entry" (22 GT + 4 CROSS/CROSS-PENDING = 26, not 27). M7: §6.1 domain tag block layout now reflects ERR-011-001/ERR-012-001 race — #11 occupies `0x18` or `0x1D` depending on which of #11/#12 reaches `APPROVED` first. |
| 0.3 | May 18, 2026 | AI agent (claude/review-phase-0-requirements-yMzh6) | APPROVED patch. ERR-014-004 resolved: `DOMAIN_TAG_DEFENSIVE_AI` promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` (value `0x1A` confirmed, allocated in #16 §3.4 v1.0.5). Block layout updated to final values: `0x17` = #12, `0x1D` = #11 (final value), `0x19` = #13, `0x1A` = #14, `0x1B` = #15. |
| 0.4 | August 12, 2026 | AI agent (wiring backlog W2) | APPROVED patch. KD-6 revised (`ERR-014-006`): §6.1 gains "Region: Tackle Outcome Resolution" — ten `[GT]` + one `[FIXED]` constants, values copied verbatim from `section-3.md` §3.6.5.5. Catalogue grows from 26 entries (22 `[GT]` + 4 `[CROSS]`) to 37 (32 `[GT]` + 4 `[CROSS]` + 1 `[FIXED]`); §9.1 item 4's evidence count is now stale as a result and is flagged, not corrected, in this pass (see the W2 sweep report). |
