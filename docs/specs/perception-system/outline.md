# Perception System Specification #7 — Detailed Outline

**File:** `Perception_System_Spec_Outline_v1_0.md`  
**Purpose:** Planning document for the Perception System specification. Establishes scope,
structure, mathematical framework, and technical approach before full section drafting begins.
This document defines *what* will be written, not yet *how* it will be implemented in detail.

**Created:** February 23, 2026, 12:00 PM PST  
**Version:** 1.1  
**Status:** REVISED — All open questions resolved. Awaiting Lead Developer Approval  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Estimated Effort:** ~28 hours  
**Author:** Claude (AI) with Anton (Lead Developer)

**Upstream Dependencies (all approved):**
- Ball Physics Specification #1 — `BallState` struct (position, velocity, spin)
- Agent Movement Specification #2 — `AgentState` struct (position, velocity, facingDirection); `PlayerAttributes` (Decisions, Anticipation)
- Collision System Specification #3 — spatial hash for occlusion and proximity queries

**Downstream (callers of Perception output):**
- Decision Tree Specification #8 — sole consumer of `PerceptionSnapshot`; no interface written until #8 is specified
- Positioning AI, Pressing AI, Defensive AI, Attacking AI — all downstream; not yet specified

**Key Constraint:** No `IDecisionTreeConsumer` or similar interface is written in this spec.
Decision Tree owns its own subscription. This follows the project interface principle.

---

## EXECUTIVE SUMMARY

Perception governs what an agent *knows* about the match state at any given moment.
It is the bridge between raw simulation truth (omniscient world state) and the
limited, delayed, error-prone awareness that drives realistic agent decision-making.

The central design problem: **agents should not be omniscient**. A defender who
cannot see a runner behind him should not react to that runner. A midfielder under
pressure should have degraded awareness of distant options. Elite players should
perceive more, faster, and more accurately than lesser players — and this must be
measurable and tunable.

The core model:

```
PerceptionSnapshot = f(FacingDirection, FieldOfView, OcclusionCheck, RecognitionLatency, Decisions, Anticipation)
```

Where:
- `FacingDirection` — agent's current facing vector (from AgentState)
- `FieldOfView` — angular cone defining what is spatially visible (attribute-modified)
- `OcclusionCheck` — shadow cone test; agents block line-of-sight to targets behind them
- `RecognitionLatency (L_rec)` — time delay before a newly-visible entity enters awareness
- `Decisions` attribute [1–20] — governs scan frequency and recognition speed
- `Anticipation` attribute [1–20] — governs blind-side awareness and proactive scanning

**Output:** `PerceptionSnapshot` — a filtered, latency-aware view of the world delivered
to the Decision Tree each heartbeat (10Hz).

**Simulation frequency:** Perception runs at the **10Hz tactical heartbeat**, not the
60Hz physics step. This is a deliberate design decision — perception is a cognitive
process, not a physics update.

---

## SECTION 1: PURPOSE & SCOPE (~3 pages)

### 1.1 What This Specification Covers

- Definition of `PerceptionSnapshot` — the output struct delivered to Decision Tree
- Field of view model: base cone angle, attribute modifiers, directional facing
- Occlusion model: shadow cone projection from occluding agents; line-of-sight test
- Recognition latency (`L_rec`): time from first visibility to inclusion in snapshot
- Blind-side awareness: rear-arc perception governed by Anticipation attribute
- Shoulder check mechanic: deliberate scan action that temporarily expands blind-side awareness
- Scanning frequency model: how often agent refreshes full perception (attribute-dependent)
- Pressure scalar effect on perception quality (degraded awareness under pressure)
- Half-turn orientation bonus to recognition latency (cross-spec, established in First Touch #4)
- `PerceptionSnapshot` struct definition — all fields, types, frame stamps
- Performance contract: all 22 agents processed within 10Hz heartbeat budget

### 1.2 What Is Out of Scope

**Owned by other specifications:**
- Decision-making logic — owned by Decision Tree (#8)
- Tactical instruction parsing — owned by future Formation/Instructions specs (Stage 1)
- Ball physics — owned by Ball Physics (#1)
- Agent physical movement — owned by Agent Movement (#2)
- Spatial hash implementation — owned by Collision System (#3); this spec consumes it
- Goalkeeper perception specifics — will be addressed in Goalkeeper Mechanics (#11)

**Deferred to Stage 1+:**
- Weather/fog effects on vision distance
- Crowd noise occlusion of communication
- Referee positioning awareness
- Detailed peripheral vision degradation model
- Communication/calling mechanic between teammates

**Permanently excluded:**
- Named perception "types" or classified awareness states — perception is parametric,
  not enumerated. No `PerceptionType` enum exists.
- Rendering or visual representation of perception cones — that is Stage 1+ debug tooling

### 1.3 Key Design Decisions (to be locked before Section 3)

**KD-1 — Perception runs at 10Hz, not 60Hz.**
Perception is a cognitive process governed by the tactical heartbeat. Running it at
60Hz would be both unrealistic and wasteful. All output is valid for the full 100ms
heartbeat window. Physics-layer events (ball contact, tackle) can trigger a forced
mid-heartbeat perception refresh for the directly involved agents only.

**KD-2 — `PerceptionSnapshot` is a value struct, not a class.**
Consistent with `PassRequest`, `ShotRequest`, `BallState`. Stack allocation. No heap
pressure. Copied into Decision Tree on each heartbeat.

**KD-3 — Occlusion uses shadow cone approximation, not raycast.**
Full raycasting against agent body geometry is unnecessary complexity and cost at Stage 0.
Each agent projects a shadow cone of fixed angular width from their body position.
Targets inside any occluding agent's shadow cone — from the observer's perspective —
are marked as occluded. This is O(n²) in the worst case but O(n × k) in practice
where k is a small constant of nearby agents. Budget is acceptable at n=22.

**KD-4 — Recognition latency is deterministic, not random.**
`L_rec` is computed from the Decisions attribute and a deterministic hash of
(agentId, targetId, frameNumber). No System.Random. Consistent with project-wide
determinism requirement.

**KD-5 — Blind-side arc is fixed at 200° rear, not attribute-scaled.**
The angular extent of the blind-side arc is fixed. What varies by attribute (Anticipation)
is the *probability and frequency* of automatic shoulder checks — not the geometry of
the blind side itself. This prevents edge cases where high-Anticipation agents
effectively gain unrealistic rear vision.

**KD-6 — Shoulder check is a simulated action, not a toggle.**
When an agent performs a shoulder check, they gain a timed awareness window of the
rear arc (duration: ~0.3s, or 3 heartbeat ticks). High-Anticipation agents perform
checks more frequently. The check is triggered by the Perception system autonomously;
Decision Tree does not explicitly request it.

**KD-7 — Pressure degrades perception breadth, not depth.**
Under pressure (PressureScalar > threshold), the agent's effective field of view angle
narrows. They see fewer entities, but entities they do see are perceived at full fidelity.
This models attentional narrowing under stress (Beilock, 2010).

### 1.4 Relationship to Adjacent Systems

| System | Direction | What Crosses the Boundary |
|--------|-----------|--------------------------|
| Agent Movement (#2) | → into Perception | `AgentState.FacingDirection`, `AgentState.Position`, `PlayerAttributes.Decisions`, `PlayerAttributes.Anticipation` |
| Ball Physics (#1) | → into Perception | `BallState.Position` (is ball visible to this agent?) |
| Collision System (#3) | → into Perception | Spatial hash for efficient nearby-agent queries |
| First Touch (#4) | Cross-reference | Half-turn orientation bonus reduces L_rec by 15% (already established) |
| Decision Tree (#8) | ← receives output | `PerceptionSnapshot` struct (no interface until #8 is written) |

### 1.5 Dependencies

**Hard dependencies (all approved):**
- `AgentState` — Position, Velocity, FacingDirection ✅
- `PlayerAttributes.Decisions` [1–20] ✅
- `PlayerAttributes.Anticipation` [1–20] ✅
- `BallState.Position` ✅
- Spatial hash query interface from Collision System ✅

**Soft dependencies (forward references):**
- Decision Tree (#8) — sole consumer; no interface defined here
- Pressure scalar — reuses the calculation pattern from First Touch (#4) §3.3

---

## SECTION 2: SYSTEM OVERVIEW & FUNCTIONAL REQUIREMENTS (~4 pages)

### 2.1 Perception Pipeline

Single-pass pipeline executed once per 10Hz heartbeat per agent:

```
Step 1: QueryNearbyEntities()      — spatial hash; retrieve all agents + ball within MAX_PERCEPTION_RANGE
Step 2: ApplyFieldOfView()         — discard entities outside facing cone
Step 3: ApplyOcclusionFilter()     — discard shadow-cone occluded entities
Step 4: ApplyRecognitionLatency()  — stamp entities with L_rec; only expose confirmed entities
Step 5: ApplyBlindSideAwareness()  — add any rear-arc entities revealed by recent shoulder check
Step 6: BuildPerceptionSnapshot()  — assemble output struct
```

### 2.2 `PerceptionSnapshot` Struct (preliminary)

```csharp
struct PerceptionSnapshot
{
    int         ObserverId;             // Agent performing perception
    int         FrameNumber;            // Heartbeat tick this snapshot is valid for
    bool        BallVisible;            // Is the ball in the observer's current perception?
    Vector2     BallPerceivedPosition;  // Last confirmed ball position (may be stale if not visible)
    int         BallStalenessFrames;    // Frames since ball was last directly observed
    PerceivedAgent[] VisibleTeammates;  // Confirmed visible teammates
    PerceivedAgent[] VisibleOpponents;  // Confirmed visible opponents
    float       EffectiveFoVAngle;      // Actual FoV after pressure degradation
    bool        BlindSideWindowActive;  // True if shoulder check window is open
    float       BlindSideWindowExpiry;  // Match time when window closes
}

struct PerceivedAgent
{
    int     AgentId;
    Vector2 PerceivedPosition;      // May differ from true position (future: perception error)
    Vector2 PerceivedVelocity;      // Stage 0: exact; Stage 1: error-modelled
    float   ConfidenceScore;        // [0–1]; currently binary (Stage 0); continuous in Stage 1
    bool    IsOccluded;             // True if behind shadow cone (included for debugging)
    float   RecognitionLatencyRemaining; // Frames until this agent enters confirmed awareness
}
```

### 2.3 Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-1 | Each agent SHALL produce one `PerceptionSnapshot` per 10Hz heartbeat tick |
| FR-2 | Agents outside `MAX_PERCEPTION_RANGE` SHALL be excluded before FoV test |
| FR-3 | Agents inside occluding shadow cones SHALL be excluded from `VisibleTeammates`/`VisibleOpponents` |
| FR-4 | Newly visible agents SHALL not enter awareness until `L_rec` has elapsed |
| FR-5 | Blind-side arc (200° rear) SHALL be excluded unless shoulder check window is active |
| FR-6 | A Decisions 20 agent SHALL have measurably lower L_rec than a Decisions 1 agent |
| FR-7 | A Decisions 20 agent SHALL scan with measurably higher frequency than a Decisions 1 agent |
| FR-8 | High-Anticipation agents SHALL perform shoulder checks more frequently than low-Anticipation agents |
| FR-9 | Pressure scalar above threshold SHALL reduce effective FoV angle |
| FR-10 | Half-turn orientation (from First Touch §3.3.2) SHALL reduce L_rec by 15% |
| FR-11 | Ball staleness counter SHALL increment each heartbeat ball is not directly visible |
| FR-12 | All perception logic SHALL produce identical output given identical inputs (determinism) |
| FR-13 | Total perception processing for all 22 agents SHALL complete within 2ms per heartbeat |

---

## SECTION 3: CORE MODELS (~18 pages — largest section)

### 3.1 Field of View Model

**3.1.1 Base FoV Cone**
- `BASE_FOV_ANGLE` = 160° (forward-facing arc) — literature: human peripheral vision ≈ 180°; 
  160° accounts for focus degradation at periphery
- Centered on `AgentState.FacingDirection`
- Entity is in FoV if angular separation from FacingDirection < BASE_FOV_ANGLE / 2

**3.1.2 Attribute Modifier (Decisions)**
- Decisions [1–20] linearly expands effective FoV up to `MAX_FOV_BONUS_ANGLE` = 10°
- Formula: `EffectiveFoV = BASE_FOV_ANGLE + (Decisions / 20.0f) × MAX_FOV_BONUS_ANGLE`
- Range: 160° (Decisions=1) to 170° (Decisions=20) [GT values; small range intentional]

**3.1.3 Pressure Narrowing (KD-7)**
- When PressureScalar > `PRESSURE_FOV_THRESHOLD` (e.g., 0.5), FoV narrows
- `PressureFoVReduction = (PressureScalar - PRESSURE_FOV_THRESHOLD) × PRESSURE_FOV_SCALE`
- `EffectiveFoV = EffectiveFoV - PressureFoVReduction`, min clamped to `MIN_FOV_ANGLE` = 120°

**3.1.4 `MAX_PERCEPTION_RANGE`**
- `MAX_PERCEPTION_RANGE = 120f` [GT] — full pitch diagonal; effectively uncapped for standard pitch
- Spatial hash query still uses this constant — preserves optimisation path for Stage 2+
- Stage 2 weather/fog effects will reduce this value; the constant is the correct hook for that
- Do NOT remove this constant even though it is effectively uncapped at Stage 0 (OQ-5 resolution)

### 3.2 Occlusion Model — Shadow Cones (KD-3)

**3.2.1 Shadow Cone Geometry**
- Each agent at position P projects a shadow cone away from the observer O
- Shadow cone apex = P (the occluding agent's position)
- Shadow cone axis = normalize(P - O) (direction away from observer)
- Shadow cone half-angle = `OCCLUSION_CONE_HALF_ANGLE` ≈ 8° (approximates agent body width at distance)
- Target T is occluded if:
  - T is beyond P relative to O (dot product check: `dot(T-O, P-O) > dot(P-O, P-O)`)
  - AND angular deviation of T from shadow cone axis < `OCCLUSION_CONE_HALF_ANGLE`

**3.2.2 Self-Occlusion Exemption**
- Observer never occludes their own perception
- **Teammates do not generate shadow cones at Stage 0 (OQ-1 resolved).** Teammate occlusion is a Stage 1 upgrade. This is a deliberate design decision, not an oversight — document explicitly in §1.2.
- Only opponents generate shadow cones at Stage 0

**3.2.3 Ball Occlusion Note (OQ-2)**
- Ball can be occluded by opponent shadow cones using identical geometry
- When ball re-emerges from occlusion, it enters the snapshot instantly (no L_rec)
- This is intentional — ball occlusion + instant recognition must be documented in §1.2 as a design decision

**3.2.3 Performance**
- O(n²) worst case; O(n × k) typical where k = nearby opponents in range
- At n=22, maximum 22 × 21 = 462 shadow cone tests per heartbeat — trivially fast

### 3.3 Recognition Latency Model

**3.3.1 What L_rec Represents**
`L_rec` is the time (in heartbeat ticks) between an entity first becoming visible to
an agent and that entity being included in the agent's `PerceptionSnapshot`. It models
cognitive recognition delay — the time to register and classify a new stimulus.

**3.3.2 Base L_rec Formula**
```
L_rec_base = L_MAX - ((Decisions - 1) / 19.0f) × (L_MAX - L_MIN)
```
- `L_MAX` = 5 heartbeat ticks (500ms for Decisions=1) [GT — from Master Vol 1 scanning data]
- `L_MIN` = 1 heartbeat tick (100ms for Decisions=20) [GT]
- Linear interpolation between extremes

**3.3.3 Modifiers**
- Half-turn orientation active: `L_rec × 0.85` (−15%, per First Touch §3.3.2)
- Ball contact event (forced refresh): `L_rec = 0` for the ball entity only
- Result clamped to [1, L_MAX] after all modifiers

**3.3.4 Deterministic Noise**
- Small per-entity noise added deterministically:
  `noise = DeterministicHash(agentId, targetId, frameNumber) % 2` (0 or 1 tick)
- Prevents all agents with same Decisions attribute having identical latencies
- No System.Random

**3.3.5 Latency Tracking**
- `PerceptionSystem` maintains a dictionary: `Dictionary<(observerId, targetId), int> _latencyCounters`
- Counter increments each heartbeat the target is visible but not yet confirmed
- Entity enters `VisibleTeammates` / `VisibleOpponents` when counter ≥ L_rec
- Counter resets to 0 if entity leaves visibility (disappears, re-appears → new L_rec cycle)

### 3.4 Blind-Side Awareness & Shoulder Check

**3.4.1 Blind-Side Arc**
- Rear arc: 200° centered on the direction *opposite* to FacingDirection
- This is the complement of the 160° forward FoV — they sum to 360°
- All entities in this arc are invisible by default (KD-5)

**3.4.2 Shoulder Check Trigger (OQ-3 resolved — fully autonomous)**
- Decision Tree does not request shoulder checks. Perception System triggers them autonomously.
- Anticipation [1–20] determines mean interval between autonomous shoulder checks:
  ```
  CheckInterval_ticks = CHECK_MAX_TICKS - ((Anticipation - 1) / 19.0f) × (CHECK_MAX_TICKS - CHECK_MIN_TICKS)
  ```
  - `CHECK_MAX_TICKS` = 30 ticks (3 seconds for Anticipation=1) [GT — within Franks 1985 bracket]
  - `CHECK_MIN_TICKS` = 6 ticks (0.6 seconds for Anticipation=20) [GT — Master Vol 1: elite 6–8 scans/possession]
- Deterministic jitter: ±2 ticks from hash(agentId, frameNumber) — prevents synchronised mass-checks
- **Stage 1 upgrade point:** Context-sensitive check urgency (higher frequency when runner detected
  in peripheral arc). Not implementable until Decision Tree (#8) is specified.

**3.4.3 Shoulder Check Window**
- Duration: `SHOULDER_CHECK_DURATION` = 3 heartbeat ticks (300ms) [GT — per Master Vol 1 "~0.3 seconds"]
- During window: blind-side arc entities are processed with a separate L_rec cycle
  (no free instant knowledge — they still need L_rec to confirm)
- `BlindSideWindowActive` and `BlindSideWindowExpiry` populated in snapshot

**3.4.4 Possession-Phase Penalty**
- If agent is in ball-carrying state (has possession), shoulder check interval doubles
- Rationale: dribbler cannot scan as freely as off-ball agent (Master Vol 1 §3.1)

### 3.5 Ball Perception

**3.5.1 Ball Visibility**
- Ball treated as a special entity — no L_rec applied (ball is immediately recognised)
- Ball is visible if: within MAX_PERCEPTION_RANGE AND within EffectiveFoV AND not occluded
  by any opponent shadow cone
- Ball shadow cone occlusion uses same model as agent occlusion (§3.2)

**3.5.2 Stale Ball Position**
- When ball leaves visibility: last known position stored in snapshot (`BallPerceivedPosition`)
- `BallStalenessFrames` increments each tick ball is not seen
- Decision Tree uses staleness to weight confidence in ball position estimate
- No predictive ball tracking at Stage 0 (Stage 1 upgrade: Anticipation-driven ball prediction)

### 3.6 Pressure Scalar Calculation (cross-spec reuse)

- Reuses pressure model established in First Touch §3.3.1
- PressureScalar = f(distance to nearest opponent, number of nearby opponents)
- Perception uses the scalar for FoV narrowing (§3.1.3) only
- No new formula required — reference First Touch §3.3.1 as authoritative source

---

## SECTION 4: INTEGRATION CONTRACTS (~5 pages)

### 4.1 Integration with Agent Movement (#2)
- Read `AgentState.FacingDirection` and `AgentState.Position` each heartbeat
- Read `PlayerAttributes.Decisions` and `PlayerAttributes.Anticipation` at heartbeat start
  (cached for duration of pipeline; attribute changes mid-heartbeat are ignored)
- No modifications to AgentState — read-only consumer

### 4.2 Integration with Ball Physics (#1)
- Read `BallState.Position` each heartbeat
- No modifications to BallState — read-only consumer

### 4.3 Integration with Collision System (#3)
- Call `spatialHash.QueryRadius(observerPosition, MAX_PERCEPTION_RANGE)` to get candidate agents
- No modifications to spatial hash — read-only consumer
- Collision System spatial hash is the authoritative proximity data source

### 4.4 Output to Decision Tree (#8)
- `PerceptionSnapshot` is passed by value to Decision Tree each heartbeat
- No interface defined here — Decision Tree spec defines its own intake
- `PerceptionSnapshot` struct is owned and defined by this specification
- Decision Tree must not modify the struct definition; amendments go through this spec

### 4.5 Forced Refresh Events
- Ball contact event (ball changes possession or is struck) → forced refresh for involved agents
- Tackle event → forced refresh for tackler and tackled agent
- These are edge-triggered, not polled — Event System stub used (consistent with other specs)

---

## SECTION 5: TESTING (~6 pages)

### 5.1 Unit Tests (target: ~40)

**FoV model (8 tests):**
- Entity at 0° offset from facing → always visible
- Entity at exactly FoV boundary → boundary condition
- Decisions=1 vs Decisions=20 FoV angle difference measurable
- PressureScalar=0 → no FoV reduction
- PressureScalar=1.0 → FoV clamped to MIN_FOV_ANGLE
- Entity beyond MAX_PERCEPTION_RANGE → excluded regardless of angle
- Half-turn bonus applied correctly to L_rec
- FoV formula produces valid angles for all Decisions values [1–20]

**Occlusion model (8 tests):**
- Target directly behind occluder (fully shadowed) → occluded
- Target at shadow cone edge (boundary condition)
- Observer and target same position → no self-test crash
- Occluder between observer and target at various distances
- Multiple occluders in line → target still occluded
- Target closer than occluder → not occluded
- No teammates as occluders at Stage 0
- Large distance between observer and occluder → cone narrow, fewer targets occluded

**Recognition latency (8 tests):**
- Decisions=1 → L_rec = L_MAX
- Decisions=20 → L_rec = L_MIN
- Half-turn active → L_rec reduced by 15%
- Entity reappears after disappearing → L_rec resets
- Ball entity → L_rec = 0 (immediate)
- Deterministic noise: same inputs → same output always
- Counter increments correctly over heartbeat ticks
- Entity confirmed exactly when counter reaches L_rec threshold

**Blind-side (8 tests):**
- Entity at 181° from facing → not visible without shoulder check
- Shoulder check active → rear entity visible (with L_rec)
- Anticipation=1 → long check interval
- Anticipation=20 → short check interval
- Check window expires correctly after SHOULDER_CHECK_DURATION ticks
- Possession state doubles check interval
- Deterministic jitter: same agentId/frameNumber → same jitter value
- Blind-side entity exits rear arc → normal L_rec cycle begins

**Ball perception (4 tests):**
- Ball visible → BallVisible=true, staleness=0
- Ball occluded → BallVisible=false, staleness increments
- Ball staleness resets when ball re-enters visibility
- Ball behind MAX_PERCEPTION_RANGE → not visible

**Snapshot assembly (4 tests):**
- Snapshot contains exactly the confirmed visible entities
- FrameNumber matches current heartbeat tick
- BlindSideWindowActive matches shoulder check state
- Empty snapshot (no visible entities) → valid struct, no null refs

### 5.2 Integration Tests (target: ~12)

- Full 22-agent heartbeat completes within 2ms budget
- All agents start match unaware of each other → awareness builds over first several ticks
- Agent behind wall of opponents is correctly perception-isolated
- High-Decisions midfielder perceives more options than Low-Decisions equivalent
- High-Anticipation defender detects blind-side runner before Low-Anticipation equivalent
- Pressure narrows FoV for agent under press
- Ball possession change triggers forced refresh for correct agents
- Determinism: 100 heartbeats with identical inputs → byte-identical snapshots
- Agent facing 180° away from ball → ball not in snapshot
- Chain test: Perception → Decision Tree receives non-null snapshot every tick
- Shoulder check timing is consistent across long match simulation (no drift)
- L_rec tracking does not leak memory over 90-minute simulation (54,000 physics ticks, 9,000 heartbeats)

### 5.3 Balance Tests

- Decisions=1 vs 20 agent: option count difference measurable per snapshot over 100-heartbeat sample
- Anticipation=1 vs 20 agent: blind-side detection frequency ratio matches expected from formula
- No perception system behaviour creates dominant exploitable patterns

---

## SECTION 6: PERFORMANCE ANALYSIS (~3 pages)

### 6.1 Computational Budget

**Budget:** 10Hz heartbeat = 100ms. Perception allocation: 2ms for all 22 agents.
Per-agent budget: ~90μs.

**Pipeline step costs (estimated):**
| Step | Operation | Cost estimate |
|------|-----------|--------------|
| QueryNearbyEntities | Spatial hash radius query | ~5μs |
| ApplyFieldOfView | Angle test × ~22 candidates | ~2μs |
| ApplyOcclusionFilter | Shadow cone test × ~15 passes | ~8μs |
| ApplyRecognitionLatency | Dictionary lookup × ~15 entities | ~5μs |
| ApplyBlindSideAwareness | Conditional; 3 ticks only | ~1μs |
| BuildPerceptionSnapshot | Struct assembly | ~2μs |
| **Total per agent** | | **~23μs** |
| **Total all 22 agents** | | **~506μs — well within 2ms** |

### 6.2 Memory

- `_latencyCounters` dictionary: 22 observers × 21 targets = 462 (int, int) → int entries
- Fixed size; no per-match allocation growth
- `PerceptionSnapshot` per agent: estimated ~200 bytes (22 PerceivedAgent entries × ~8 bytes + header)
- Total snapshot allocation: ~4.4KB per heartbeat — trivial

### 6.3 Scaling

- O(n²) occlusion is acceptable at n=22 (Stage 0)
- If n grows significantly in future stages, spatial partitioning of shadow cone tests is the upgrade path
- Document in Section 7 as future extension

---

## SECTION 7: FUTURE EXTENSIONS (~3 pages)

### 7.1 Stage 1 Extensions
- **Perception error on position:** PerceivedPosition differs from TruePosition by a small
  error scaled by Decisions attribute — makes passing predictions less perfect
- **Continuous confidence score:** ConfidenceScore moves from binary to [0,1] float,
  decaying with distance and occlusion proximity
- **Teammate occlusion:** Teammates also generate shadow cones (OQ-1 deferral)
- **Ball trajectory prediction:** Anticipation attribute drives predictive ball position estimate
  rather than using last-known position
- **Context-sensitive shoulder check urgency:** Check frequency increases when a runner is
  detected entering the blind-side arc periphery. Requires Decision Tree (#8) to be specified
  first. (OQ-3 Stage 1 flag)

### 7.2 Stage 2 Extensions
- **Weather effects:** Fog density reduces MAX_PERCEPTION_RANGE; rain adds FoV noise
- **Crowd noise:** Disrupts teammate communication model (Stage 2 system)

### 7.3 Stage 3+ Extensions
- **Fixed64 migration:** Float arithmetic → Fixed64 for full cross-platform determinism
- **Peripheral vision degradation curve:** Non-linear FoV quality gradient rather than binary in/out
- **Communication arc:** Verbal calling mechanic extends effective awareness between nearby teammates

### 7.4 Permanent Exclusions
- `PerceptionType` enum — perception is parametric output, not classified states
- Rendering of vision cones in gameplay — debug tooling only, never gameplay feature
- Referee or crowd awareness — out of scope for physics foundation

---

## SECTION 8: REFERENCES (~2 pages)

### 8.1 Academic Sources

**[BEILOCK-2010]** Beilock, S.L. (2010). *Choke: What the Secrets of the Brain Reveal
About Getting It Right When You Have To.* Free Press. Cited for: pressure-induced attentional
narrowing (§3.1.3); 20-50% performance degradation range.

**[WILLIAMS-1998]** Williams, A.M. & Davids, K. (1998). "Visual search strategy, selective
attention, and expertise in soccer." *Research Quarterly for Exercise and Sport*, 69(2).
Cited for: expert vs. novice scanning frequency; 2-4m pressure proximity zone.

**[HELSEN-1999]** Helsen, W.F. & Starkes, J.L. (1999). "A multidimensional approach to
skilled perception and performance in sport." *Applied Cognitive Psychology*, 13(1).
Cited for: half-turn awareness advantage; recognition latency ranges.

**[FRANKS-1985]** Franks, I.M. & Miller, G. (1985). "Eyewitness testimony in sport."
*Journal of Sport Behavior*, 9(1). Cited for: plausibility bracket on recognition latency
range (L_MAX = 500ms baseline).

### 8.2 Design Authority References

**[MASTER-VOL1]** Tactical Director Design Team (2025). "Master Volume I: Physics Core."
- §3.1 Layer 1 Perception: shadow cones, scanning frequency (6–8 elite, 2–4 average)
- §3.1 Blind-side awareness, shoulder check cost (~0.3s), P_e = 1.0 default rear arc

**[MASTER-VOL2]** Tactical Director Design Team (2025). "Master Volume II: Human Systems."
- Decisions and Anticipation attribute definitions
- Recognition latency attribute scaling authority

**[AGENT-MOVEMENT-SPEC]** Agent Movement Specification #2 v1.3 (Approved).
- PlayerAttributes: Decisions [1–20], Anticipation [1–20], confirmed fields
- AgentState: FacingDirection, Position — authoritative struct

**[FIRST-TOUCH-SPEC]** First Touch Mechanics Specification #4 (Approved).
- §3.3.2: Half-turn orientation bonus — L_rec reduced by 15% (cross-spec reference)
- §3.3.1: Pressure scalar calculation — authoritative formula reused in §3.6

**[COLLISION-SYSTEM-SPEC]** Collision System Specification #3 (Approved).
- Spatial hash interface: `QueryRadius(position, radius)` — used in §3 pipeline Step 1

---

## SECTION 9: APPROVAL CHECKLIST

*To be drafted as the final document in the specification.*
Standard template: completeness, dependency validation, test coverage, cross-spec consistency,
performance contracts, constants audit.

---

## OPEN QUESTIONS — RESOLVED

All open questions resolved before Section 1 drafting. Decisions are locked.

| ID | Decision | Rationale |
|----|----------|-----------|
| OQ-1 | **Opponents only generate shadow cones at Stage 0.** Teammate occlusion deferred to Stage 1. | Additive extension; no restructuring required. O(n²) cost not justified at Stage 0 for marginal realism gain. |
| OQ-2 | **No L_rec for ball. Ball is always immediately recognised when visible.** Ball occlusion + instant recognition is intentional and must be explicitly documented in §1.2 as a design decision, not an oversight. Ball staleness handles the "lost sight" case. | L_rec models cognitive agent-recognition delay. Ball is inanimate — applying L_rec misuses the model. |
| OQ-3 | **Shoulder check is fully autonomous, triggered by Perception System.** Decision Tree does not request checks. Context-sensitivity limitation (idle vs. possession state) partially mitigated by possession doubling interval (§3.4.4). Remainder flagged as Stage 1 upgrade in §7. | Separation of concerns — Perception owns awareness, Decision Tree owns decisions. DT managing its own sensory apparatus creates circular dependency risk at spec #8. |
| OQ-4 | **Academic-informed [GT] with documented brackets.** Franks (1985) and Helsen (1999) provide plausibility ranges. Values marked [GT] within those ranges. Identical pattern to Pass Mechanics and Shot Mechanics constants. | Consistent with project-wide constants methodology. No pure [GT] without academic range reference. |
| OQ-5 | **Keep range cap in architecture; set `MAX_PERCEPTION_RANGE = 120f` [GT] (full pitch diagonal).** Effectively uncapped for a standard pitch. Spatial hash preserved for future optimisation. Stage 2 weather/fog effects reduce this value with proper design. Constant marked [GT] and tunable. | Removing cap entirely eliminates spatial hash benefit and sets bad precedent. Hard-coding 60m creates exploitable blind spots. 120m constant preserves architecture, documents intent, and leaves optimisation path open. |

---

## ESTIMATED SECTION EFFORT

| Section | Pages | Est. Hours |
|---------|-------|-----------|
| 1: Purpose & Scope | ~3 | 3h |
| 2: System Overview & Requirements | ~4 | 4h |
| 3: Core Models | ~18 | 10h |
| 4: Integration Contracts | ~5 | 4h |
| 5: Testing | ~6 | 4h |
| 6: Performance | ~3 | 2h |
| 7: Future Extensions | ~3 | 2h |
| 8: References | ~2 | 1h |
| 9: Approval Checklist | ~3 | 2h |
| **Total** | **~47 pages** | **~32h** |

---

## CRITICAL PATH

Before Section 3 can be drafted, the following must be resolved:
1. All Open Questions (OQ-1 through OQ-5) answered by Lead Developer
2. `PerceptionSnapshot` struct confirmed (Section 2.2 is provisional)
3. `PlayerAttributes.Decisions` and `PlayerAttributes.Anticipation` confirmed in Agent Movement
   approval (currently IN REVIEW — non-blocking if approved before Section 3 begins)

---

*End of Outline — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
