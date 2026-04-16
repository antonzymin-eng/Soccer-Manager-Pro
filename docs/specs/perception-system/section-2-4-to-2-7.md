## 2.4 Functional Requirements

Requirements are stated using SHALL (mandatory), SHOULD (recommended), and MAY
(permitted). All SHALL requirements are testable and define the complete behavioural
contract for Section 3 implementations. Requirements are grouped by pipeline step
for traceability.

### 2.4.1 Core Pipeline Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-1 | Each agent SHALL produce exactly one `FilteredView` and one `PerceptionDiagnostics` per 10Hz heartbeat tick under normal operation | Mandatory | UT-CORE-001 |
| FR-2 | The pipeline SHALL execute Steps 1–6 in order; no step may be skipped or reordered | Mandatory | UT-CORE-002 |
| FR-3 | A forced mid-heartbeat refresh SHALL execute the full pipeline for affected agents only; unaffected agents SHALL NOT be refreshed | Mandatory | UT-CORE-003 |
| FR-4 | The pipeline SHALL produce valid output when all 22 agents are processed in a single heartbeat | Mandatory | IT-CORE-001 |
| FR-5 | The `FilteredView` and `PerceptionDiagnostics` outputs SHALL be value structs; no heap allocation SHALL occur during standard pipeline execution | Mandatory | UT-CORE-004 |
| FR-6 | Agent attributes and state SHALL be read once, at the start of the heartbeat pipeline, and cached for its duration | Mandatory | UT-CORE-005 |

### 2.4.2 Field of View Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-7 | Agents beyond `MAX_PERCEPTION_RANGE` SHALL be excluded from the candidate list before any FoV test is performed | Mandatory | UT-FOV-001 |
| FR-8 | An entity whose bearing from the observer exceeds `EffectiveFoVAngle / 2` SHALL be excluded from the FoV-passing set | Mandatory | UT-FOV-002 |
| FR-9 | The effective FoV angle SHALL be computed from `BASE_FOV_ANGLE` modified by the agent's `Decisions` attribute | Mandatory | UT-FOV-003 |
| FR-10 | The effective FoV angle SHALL be reduced when `PressureScalar` exceeds `PRESSURE_FOV_THRESHOLD` (KD-7) | Mandatory | UT-FOV-004 |
| FR-11 | `EffectiveFoVAngle` SHALL be clamped to a minimum of `MIN_FOV_ANGLE` regardless of pressure | Mandatory | UT-FOV-005 |
| FR-12 | `EffectiveFoVAngle` SHALL be clamped to a maximum of `BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE` regardless of Decisions value | Mandatory | UT-FOV-006 |
| FR-13 | A Decisions=20 agent SHALL have a measurably larger `EffectiveFoVAngle` than a Decisions=1 agent at the same PressureScalar | Mandatory | UT-FOV-007 |
| FR-14 | An agent with `PressureScalar = 0` SHALL have an `EffectiveFoVAngle` equal to `BASE_FOV_ANGLE + Decisions modifier` | Mandatory | UT-FOV-008 |

### 2.4.3 Occlusion Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-15 | An entity falling within the shadow cone projected by any opponent SHALL be excluded from the occlusion-passing set | Mandatory | UT-OCC-001 |
| FR-16 | An entity closer to the observer than the occluding opponent SHALL NOT be occluded by that opponent's shadow cone | Mandatory | UT-OCC-002 |
| FR-17 | Teammates SHALL NOT generate shadow cones at Stage 0 (OQ-1 resolution) | Mandatory | UT-OCC-003 |
| FR-18 | An observer SHALL NOT be occluded by their own position in self-reference edge cases | Mandatory | UT-OCC-004 |
| FR-19 | The ball SHALL be subject to occlusion using the same shadow cone geometry as agents (OQ-2 resolution) | Mandatory | UT-OCC-005 |
| FR-20 | A target entity occluded by multiple overlapping shadow cones SHALL still be treated as a single occlusion, not compound-penalised | Mandatory | UT-OCC-006 |

### 2.4.4 Recognition Latency Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-21 | A newly visible agent SHALL NOT enter `VisibleTeammates` or `VisibleOpponents` until their recognition latency counter has reached `L_rec` ticks | Mandatory | UT-LREC-001 |
| FR-22 | A Decisions=1 agent's `L_rec` SHALL equal `L_MAX` heartbeat ticks | Mandatory | UT-LREC-002 |
| FR-23 | A Decisions=20 agent's `L_rec` SHALL equal `L_MIN` heartbeat ticks | Mandatory | UT-LREC-003 |
| FR-24 | When an agent leaves the visible set (any reason), their latency counter SHALL reset to 0; the full `L_rec` cycle SHALL restart on re-appearance | Mandatory | UT-LREC-004 |
| FR-25 | When the half-turn orientation bonus is active (from First Touch §3.3.2), `L_rec` SHALL be reduced by 15% | Mandatory | UT-LREC-005 |
| FR-26 | The ball SHALL have `L_rec = 0`; it SHALL enter the snapshot immediately upon becoming visible (OQ-2 resolution) | Mandatory | UT-LREC-006 |
| FR-27 | Recognition latency SHALL be deterministic: identical (observerId, targetId, frameNumber, Decisions) inputs SHALL produce identical `L_rec` outputs | Mandatory | UT-LREC-007 |
| FR-28 | Deterministic noise applied to `L_rec` SHALL NOT use `System.Random` or any non-deterministic random number generator | Mandatory | UT-LREC-008 |

### 2.4.5 Blind-Side and Shoulder Check Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-29 | Entities in the 200° rear arc SHALL be excluded from the snapshot by default (KD-5) | Mandatory | UT-BLIND-001 |
| FR-30 | When `BlindSideWindowActive` is true, rear-arc entities SHALL be processed through Steps 3 and 4 with their own recognition latency cycle | Mandatory | UT-BLIND-002 |
| FR-31 | Rear-arc entities admitted during a shoulder check window SHALL NOT be immediately confirmed; they SHALL require `L_rec` ticks to confirm | Mandatory | UT-BLIND-003 |
| FR-32 | The shoulder check interval SHALL be attribute-scaled by `Anticipation` [1–20]: Anticipation=1 produces the longest interval; Anticipation=20 produces the shortest | Mandatory | UT-BLIND-004 |
| FR-33 | The shoulder check window SHALL expire after `SHOULDER_CHECK_DURATION` heartbeat ticks (KD-6) | Mandatory | UT-BLIND-005 |
| FR-34 | The Perception System SHALL trigger shoulder checks autonomously; the Decision Tree SHALL NOT be responsible for requesting checks (OQ-3 resolution) | Mandatory | UT-BLIND-006 |
| FR-35 | When an agent is in ball-possession state, the shoulder check interval SHALL double (§3.4.4) | Mandatory | UT-BLIND-007 |
| FR-36 | Shoulder check scheduling jitter SHALL be deterministic: identical (agentId, frameNumber) SHALL produce identical jitter values | Mandatory | UT-BLIND-008 |

### 2.4.6 Ball Perception Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-37 | `BallVisible` SHALL be true if and only if the ball is within `MAX_PERCEPTION_RANGE`, within `EffectiveFoVAngle`, and not occluded | Mandatory | UT-BALL-001 |
| FR-38 | `BallPerceivedPosition` SHALL equal `BallState.Position` when `BallVisible` is true | Mandatory | UT-BALL-002 |
| FR-39 | `BallPerceivedPosition` SHALL retain the last confirmed position when `BallVisible` is false (stale position) | Mandatory | UT-BALL-003 |
| FR-40 | `BallStalenessFrames` SHALL increment by 1 each heartbeat the ball is not directly visible | Mandatory | UT-BALL-004 |
| FR-41 | `BallStalenessFrames` SHALL reset to 0 each heartbeat the ball is directly visible | Mandatory | UT-BALL-005 |
| FR-42 | `BallStalenessFrames` SHALL be 0 if and only if `BallVisible` is true in the same snapshot (INV-8) | Mandatory | UT-BALL-006 |

### 2.4.7 Determinism Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-43 | Given identical world state and identical agent attributes at tick T, all 22 agents' `FilteredView` outputs SHALL be identical across independent runs | Mandatory | IT-DET-001 |
| FR-44 | The `_latencyCounters` dictionary state at tick T SHALL be identical across independent runs given identical inputs from tick 0 | Mandatory | IT-DET-002 |
| FR-45 | No non-deterministic API SHALL be called anywhere in the perception pipeline (no `System.Random`, `DateTime.Now`, `UnityEngine.Random`) | Mandatory | UT-DET-001 |
| FR-46 | The forced mid-heartbeat refresh mechanism SHALL be deterministic given identical trigger event sequences | Mandatory | UT-DET-002 |

### 2.4.8 Performance Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-47 | Total perception processing for all 22 agents SHALL complete within **2ms** per 10Hz heartbeat on target hardware | Mandatory | PT-001 |
| FR-48 | Per-agent perception pipeline SHALL complete within **90μs** on target hardware | Mandatory | PT-002 |
| FR-49 | No per-heartbeat heap allocation SHALL occur in the standard (non-forced-refresh) pipeline path | Mandatory | PT-003 |
| FR-50 | `_latencyCounters`, `_nextCheckTick`, and `_checkWindowExpiry` SHALL have fixed capacity allocated at match initialisation; capacity SHALL NOT grow at runtime | Mandatory | PT-004 |

---

## 2.5 Attribute Influence Summary

This table provides a consolidated reference for how `PlayerAttributes.Decisions` and
`PlayerAttributes.Anticipation` influence the perception pipeline. All formulas are
derived in Section 3. This table is informational — Section 3 is the mathematical authority.

| Attribute | Range | Pipeline Step | Influence | Direction |
|-----------|-------|---------------|-----------|-----------|
| `Decisions` | [1–20] | Step 2: FoV | Expands effective FoV angle | Higher → wider FoV |
| `Decisions` | [1–20] | Step 4: L_rec | Reduces recognition latency | Higher → faster recognition |
| `Anticipation` | [1–20] | Step 5: Shoulder check | Reduces check interval | Higher → more frequent checks |
| `PressureScalar` | [0–1] | Step 2: FoV | Narrows effective FoV angle | Higher → narrower FoV |
| Half-turn orientation (cross-spec: First Touch §3.3.2) | bool | Step 4: L_rec | Reduces L_rec by 15% | Active → faster recognition |
| Ball-possession state | bool | Step 5: Shoulder check | Doubles check interval | Possessed → less frequent checks |

**Notable absences (intentional):**
- `Anticipation` does NOT expand the geometric extent of the blind-side arc (KD-5). The
  200° rear arc is a fixed geometric constant, not an attribute-scaled value. What
  Anticipation scales is the *frequency* of autonomous checks, not the size of the arc.
- `Decisions` does NOT affect shoulder check frequency. Shoulder check behaviour is
  exclusively an Anticipation function.
- No attribute reduces the blind-side arc below 200° at Stage 0. Rear-arc sensitivity is
  a binary outcome of shoulder check timing, not a continuous awareness gradient.

---

## 2.6 Constants Declared in This Section

The following constants are introduced in this section. All values are provisional pending
Section 3 mathematical derivation and citation audit. Constants marked [GT] are
gameplay-tuned within literature-informed ranges; [LIT] are directly literature-derived.

| Constant | Value | Type | Source |
|----------|-------|------|--------|
| `MAX_PERCEPTION_RANGE` | 120f | float, metres | [GT] — full pitch diagonal; OQ-5 resolution |
| `BASE_FOV_ANGLE` | 160f | float, degrees | [LIT-INFORMED] — human peripheral vision ≈ 180°; 160° accounts for focus degradation at periphery |
| `MAX_FOV_BONUS_ANGLE` | 10f | float, degrees | [GT] — small range intentional; see §3.1.2 |
| `MIN_FOV_ANGLE` | 120f | float, degrees | [GT] — floor for pressure-narrowed FoV; prevents total tunnel vision |
| `PRESSURE_FOV_THRESHOLD` | 0.5f | float, [0–1] | [GT] — pressure onset at mid-scale; see §3.1.3 |
| `PRESSURE_FOV_SCALE` | 40f | float, degrees per unit | [GT] — governs rate of FoV narrowing above threshold; at PressureScalar=1.0 and threshold=0.5, reduction = (1.0−0.5)×40 = 20°, yielding EffectiveFoV floor contact at MIN_FOV_ANGLE=120° from a base ~160°. Section 3 will validate or revise. |
| `OCCLUSION_CONE_HALF_ANGLE` | 8f | float, degrees | [GT] — approximates agent body half-width at mid-range; see §3.2.1 |
| `L_MAX` | 5 | int, heartbeat ticks | [GT] — 500ms; within Franks (1985) bracket |
| `L_MIN` | 1 | int, heartbeat ticks | [GT] — 100ms minimum recognition; see §3.3.2 |
| `CHECK_MAX_TICKS` | 30 | int, heartbeat ticks | [GT] — 3.0s interval for Anticipation=1; within Franks (1985) bracket |
| `CHECK_MIN_TICKS` | 6 | int, heartbeat ticks | [GT] — 0.6s interval for Anticipation=20; Master Vol 1 elite rate |
| `SHOULDER_CHECK_DURATION` | 3 | int, heartbeat ticks | [GT] — 300ms window; Master Vol 1 "~0.3 seconds" |

All constants will be subject to citation audit in Section 8. Constants marked [GT]
will be explicitly distinguished from physics-derived values in that audit, consistent
with the project-wide constants methodology established in Pass Mechanics and Shot
Mechanics specifications.

---

## 2.7 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | February 24, 2026, 3:00 PM PST | Claude (AI) / Anton | Initial draft. Pipeline, structs, 50 functional requirements, constants table. |
| 1.1 | February 24, 2026, 4:00 PM PST | Claude (AI) / Anton | Three issue resolutions: (1) Fixed-size embedded arrays replaced with `ReadOnlySpan<PerceivedAgent>` views into pre-allocated `PerceptionSystem` buffers — eliminates `unsafe` context requirement and preserves zero-allocation policy. (2) `PRESSURE_FOV_SCALE` constant added to §2.6 with provisional value and derivation note. (3) `WasOccludedThisTick` removed from shipped `PerceivedAgent` struct; replaced with `#if UNITY_EDITOR` companion `PerceivedAgentDebug` struct. |
| 1.2 | April 11, 2026 | Claude (AI) / Anton | **Architectural rework — separation of filter output from filter metadata.** (1) `PerceptionSnapshot` replaced by two structs: `FilteredView` (9 fields — pure consumer output delivered to Decision Tree) and `PerceptionDiagnostics` (7 fields — filter metadata for debug/telemetry/future systems, NOT delivered to DT). (2) `PerceivedAgent` reduced from 5 fields to 4: `RecognitionLatencyRemaining` removed (always 0 in confirmed arrays; moved to editor-only `PerceivedAgentDebug`). (3) `ReadOnlySpan` + count field pattern replaced with direct `PerceivedAgent[]` arrays — count fields were redundant with `.Length` and existed only to support the Span pattern. (4) `ForcedRefreshThisTick` added to `FilteredView` (resolves XC-4.5-01 from Section 4). (5) `BlindSidePerceivedAgents` added as separate array in `FilteredView` — blind-side agents are a distinct category of knowledge from forward-arc agents. (6) `EffectiveFoVAngle`, `PressureScalar`, `BlindSideWindowActive`, `BlindSideWindowExpiry`, and `ShoulderCheckAnimData` moved to `PerceptionDiagnostics` — these describe how the filter works, not what the agent knows. (7) `BlindSideWindowExpiry` type confirmed as `float` (match time in seconds), resolving the int/float type conflict between former Section 3 and Section 4 definitions. |

---

## Section 2 Summary

Section 2 establishes three things required before Section 3 can be drafted:

1. **Architecture** — The six-step perception pipeline is defined with invariants,
   forced refresh behaviour, and the system's position between world state and Decision Tree.
   The pipeline produces two distinct outputs: `FilteredView` (consumer data) and
   `PerceptionDiagnostics` (filter metadata).

2. **Data structures** — `FilteredView` (9 fields), `PerceptionDiagnostics` (7 fields),
   and `PerceivedAgent` (4 fields) are defined as implementation authority. The core
   architectural principle is **separation of filter output from filter metadata**: the
   Decision Tree receives only `FilteredView` — pure data about what the agent knows.
   Filter internals (FoV angles, pressure scalars, animation stubs) live in
   `PerceptionDiagnostics` and are never delivered to decision-making systems. All fields
   are documented with Stage 0 behaviour and Stage 1 upgrade paths. Ownership and
   amendment rules are established.

3. **Functional requirements** — 50 SHALL-level requirements cover every pipeline step,
   all output structs, all attribute influences, determinism, and performance contracts.
   Each requirement is individually testable and maps to Section 5 test cases.

No mathematical detail appears in this section — that is Section 3's domain. Section 2
specifies *what* the system must achieve; Section 3 specifies *how*.

**Next:** Section 3 — Core Models (FoV, Occlusion, Recognition Latency, Blind-Side,
Ball Perception, Pressure Scalar).

---

*End of Section 2 — Perception System Specification #7*
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
