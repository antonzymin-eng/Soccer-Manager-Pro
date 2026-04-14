## 3.7 PerceptionSnapshot Struct Definition

This section is the authoritative definition of `PerceptionSnapshot`. Any field change
must be version-controlled through this specification. The Decision Tree consumes this
struct; it does not modify the struct definition.

### 3.7.1 Primary Struct

```csharp
/// <summary>
/// PerceptionSnapshot — authoritative output of the Perception System.
///
/// Produced once per 10Hz heartbeat per agent. Passed by value to the Decision Tree.
/// Represents the agent's complete knowledge of the world state at this tick.
/// Contains only confirmed, filtered, latency-resolved information.
///
/// OWNERSHIP: Defined exclusively by Perception System Spec #7.
/// CONSUMERS: Decision Tree Spec #8 (sole consumer at Stage 0).
/// MODIFICATION: Any field change requires a version bump of this specification.
/// </summary>
struct PerceptionSnapshot
{
    // ── Identity and Timing ──────────────────────────────────────────────────────

    int     ObserverId;
    // AgentId of the agent this snapshot describes.
    // Set to the observing agent's AgentState.AgentId.

    int     FrameNumber;
    // Heartbeat tick (10Hz counter) at which this snapshot was built.
    // Used by Decision Tree to detect stale snapshots (should never be > 1 tick old).

    bool    IsForceRefreshed;
    // True if this snapshot was produced by a mid-heartbeat forced refresh (§3.8).
    // False for all standard 10Hz heartbeat snapshots.

    // ── Ball Data ────────────────────────────────────────────────────────────────

    bool    BallVisible;
    // True if ball passed all three visibility tests (range, FoV, occlusion) this tick.

    Vector2 BallPerceivedPosition;
    // Last confirmed ball position. If BallVisible=true: equals ground truth this tick.
    // If BallVisible=false: retains last confirmed position (may be stale).
    // Never (0,0) after first ball sighting — retains last known value indefinitely.

    int     BallStalenessFrames;
    // Number of consecutive heartbeats during which BallVisible was false.
    // 0 = ball currently visible (ground truth). Higher values = older position estimate.
    // Decision Tree should weight ball confidence inversely with this value.

    // ── Visible Agents ───────────────────────────────────────────────────────────

    PerceivedAgent[] VisibleTeammates;
    // All teammate agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..10 (max 10 outfield teammates + GK at Stage 0).

    PerceivedAgent[] VisibleOpponents;
    // All opponent agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..11 (max 11 opponents).

    // ── Blind-Side Data ──────────────────────────────────────────────────────────

    bool    BlindSideWindowActive;
    // True during an active shoulder check window (§3.4.3).
    // When true: BlindSidePerceivedAgents is populated.

    int     BlindSideWindowExpiry;
    // FrameNumber at which the current shoulder check window expires.
    // Meaningless if BlindSideWindowActive = false.

    PerceivedAgent[] BlindSidePerceivedAgents;
    // Agents confirmed during the active shoulder check window.
    // Empty if BlindSideWindowActive = false.
    // Subject to same L_rec requirements as forward-arc agents (§3.4.3).

    // ── FoV Diagnostic ───────────────────────────────────────────────────────────

    float   EffectiveFoVAngle;
    // Actual FoV angle in degrees after Decisions modifier and pressure narrowing.
    // Stored for Decision Tree diagnostic use and for debug tooling.
    // Range: [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE] = [120°, 170°].

    float   PressureScalar;
    // Pressure scalar at the time of snapshot construction.
    // Range: [0.0, 1.0]. Stored for Decision Tree use in evaluating action quality.

    // ── Animation Stub ───────────────────────────────────────────────────────────

    ShoulderCheckAnimData ShoulderCheckAnim;
    // Stage 1+ stub. Populated at Stage 0; no consumer until Animation System written.
    // See §3.4.4 for struct definition.
}
```

### 3.7.2 PerceivedAgent Sub-Struct

```csharp
/// <summary>
/// PerceivedAgent — data for one visible agent in a PerceptionSnapshot.
///
/// All fields reflect agent state at the time of snapshot construction.
/// At Stage 0: position and velocity are ground truth for confirmed entities.
/// At Stage 1+: error modelling will introduce uncertainty into these values.
/// </summary>
struct PerceivedAgent
{
    int     AgentId;
    // AgentState.AgentId of the perceived agent.

    Vector2 PerceivedPosition;
    // Ground-truth position at time of snapshot (Stage 0).
    // Stage 1: will include perception error based on distance and Decisions attribute.

    Vector2 PerceivedVelocity;
    // Ground-truth velocity at time of snapshot (Stage 0).
    // Stage 1: will include direction estimation error for far agents.

    float   ConfidenceScore;
    // [0.0, 1.0]. At Stage 0: binary — 1.0 for confirmed, not present if unconfirmed.
    // Stage 1: continuous, degrading with distance and staleness.

    bool    IsInBlindSide;
    // True if this agent was confirmed via blind-side shoulder check window,
    // rather than via the forward FoV pipeline.

    int     LatencyCounterAtConfirmation;
    // Latency counter value when this agent was first confirmed.
    // Stored for diagnostic/tuning purposes. Not used by Decision Tree.
}
```

### 3.7.3 Debug Occlusion Record (Editor/Debug Only)

```csharp
/// <summary>
/// OcclusionDebugRecord — populated only when perception debug mode is active.
/// Not part of the production PerceptionSnapshot. Excluded from all timing budgets.
/// Used by Stage 1+ visual debug tooling to render shadow cones.
/// </summary>
struct OcclusionDebugRecord
{
    int     ObserverId;
    int     TargetId;
    bool    IsOccluded;
    int     OccludingAgentId;  // -1 if not occluded
    float   ShadowHalfAngle;   // Of the occluding agent's cone, in degrees
    int     FrameNumber;
}
```

---

## 3.8 Mid-Heartbeat Forced Refresh

### 3.8.1 Trigger Events

Certain physics-layer events produce a forced mid-heartbeat perception refresh for the
directly involved agents only. All other agents continue on the standard 10Hz schedule.

The complete list of qualifying trigger events at Stage 0:

| Event | Agents Refreshed | Rationale |
|-------|-----------------|-----------|
| Ball contact (pass/shot/first touch) | Ball-contacting agent + agents within 5m of ball | Contact event resets ball awareness; nearby agents re-evaluate |
| Tackle completion | Tackler + tackled agent | Possession change; both agents need updated snapshot |
| Possession change (non-tackle) | Ball-holder losing possession + new holder | Decision context changed; immediate re-evaluation warranted |

**Rationale for selectivity:** Forcing a full 22-agent refresh on any ball event would
eliminate the 10Hz cognitive cadence and approach the cost of a 60Hz system. Only the
directly involved agents receive a refresh; spectating agents continue on schedule.

### 3.8.2 Forced Refresh Procedure

```
ForcedRefresh(agentId, triggerEvent):
    // Standard pipeline §3.0 Steps 1–7, executed immediately (out of heartbeat schedule)
    // Step 5 override: L_rec = 0 for ALL entities in this refresh
    //   Rationale: the triggering event (ball contact, tackle) is a salient attention cue
    //   that eliminates recognition delay for the brief refresh window
    // Result: snapshot with IsForceRefreshed = true
    // Normal heartbeat schedule is NOT reset — next standard heartbeat fires on schedule
```

### 3.8.3 PerceptionRefreshEvent Stub

```csharp
/// <summary>
/// PerceptionRefreshEvent — published when a forced mid-heartbeat refresh occurs.
/// Stub at Stage 0; consumed by Event System Spec #17 when written.
/// </summary>
struct PerceptionRefreshEvent
{
    int     AgentId;
    int     TriggerFrame;
    string  TriggerEventType;  // "BallContact", "TackleCompletion", "PossessionChange"
    // Event System #17 will define subscription architecture
}
```

---

## 3.9 Worked Examples

### 3.9.1 Example A: Elite Midfielder, No Pressure

**Setup:**
- Observer: Decisions=18, Anticipation=16, FacingAngle=90° (facing "up" pitch)
- PressureScalar=0.05 (one opponent at ~2.5m)
- Three teammates in forward arc, one opponent at 70° offset, one teammate at 110° offset

**Step 1 — EffectiveFoV calculation:**
```
EffectiveFoV_BeforePressure = 160° + (18/20) × 10° = 160° + 9° = 169°
Reduction = 0.05 × 30° = 1.5°
EffectiveFoV = 169° - 1.5° = 167.5°
EffectiveFoV_HalfAngle = 83.75°
BlindSide_HalfAngle = (360° - 167.5°) / 2 = 96.25°
```

**Step 2 — FoV filter:**
```
Teammate at 70° offset: 70° ≤ 83.75° → IN FoV ✓
Opponent at 70° offset: IN FoV ✓  
Teammate at 110° offset: 110° > 83.75° → OUT (in blind side) ✗
Three forward teammates: assumed within PERIPHERAL_ARC_INNER_BOUND (40°) → all in focal zone, IN FoV ✓
```

**Step 3 — L_rec for opponent (first time seen):**
```
L_rec_base(Decisions=18) = 5 - (17/19) × 4 = 5 - 3.579 = 1.421 → floor to 2 ticks after noise
noise = DeterministicHash(observerId, opponentId, frame) % 2 = 1 (assumed)
L_rec_final = Min(1.421 + 1, L_MAX) = Min(2.421, 5) = 2 ticks (L_MIN preserved algebraically)

On tick 1 of seeing opponent: counter = 1, not confirmed
On tick 2: counter = 2 ≥ 2 → CONFIRMED. Opponent enters VisibleOpponents.
```

**Step 4 — Shoulder check:**
```
CheckInterval(Anticipation=16) = 30 - (15/19) × 24 = 30 - 18.947 = 11.05 ticks
With jitter: ±2 ticks → fires between tick 9 and 13 of last check
Observer NOT in possession → no possession doubling
```

If shoulder check fires this heartbeat (BlindSideWindowActive=true):
```
Teammate at 110° offset now queried in blind-side pipeline.
110° offset from facing = 70° offset from rear (180°)
70° ≤ BlindSide_HalfAngle (96.25°) → IN blind-side arc ✓
Subject to L_rec: must accumulate 2 ticks (same Decisions=18 formula)
Not confirmed on first tick of check — needs second consecutive tick to confirm.
```

**Resulting snapshot (tick 2 of scenario):**
```
VisibleTeammates: [3 forward teammates]  (all confirmed from prior ticks)
VisibleOpponents: [opponent at 70°]      (just confirmed tick 2)
BlindSidePerceivedAgents: []             (teammate at 110° not yet confirmed)
BallVisible: true, BallStalenessFrames: 0
EffectiveFoVAngle: 167.5°
PressureScalar: 0.05
```

---

### 3.9.2 Example B: Low-Attribute Defender Under Full Press

**Setup:**
- Observer: Decisions=4, Anticipation=3, FacingAngle=270°
- PressureScalar=0.85 (three opponents within 2m)
- Ball behind occluding opponent

**Step 1 — EffectiveFoV:**
```
EffectiveFoV_BeforePressure = 160° + (4/20) × 10° = 162°
Reduction = 0.85 × 30° = 25.5°
EffectiveFoV = 162° - 25.5° = 136.5° (above MIN_FOV_ANGLE = 120° ✓)
EffectiveFoV_HalfAngle = 68.25°
```

**Step 2 — L_rec for newly visible agent:**
```
L_rec_base(Decisions=4) = 5 - (3/19) × 4 = 5 - 0.632 = 4.368 → ≈4–5 ticks with noise
This agent will not enter snapshot for 400–500ms from first visibility.
```

**Step 3 — Ball occluded:**
```
Opponent O at distance 4m lies between observer and ball.
shadowHalfAngle = arcsin(0.4/4.0) = arcsin(0.1) = 5.74°
Ball bearing from observer differs from occluder bearing by 2° → 2° < 5.74° → occluded ✓
BallVisible = false. BallStalenessFrames increments.
```

**Step 4 — Shoulder check frequency:**
```
CheckInterval(Anticipation=3) = 30 - (2/19) × 24 = 30 - 2.526 = 27.47 ≈ 27 ticks (2.7s)
Observer NOT in possession → no penalty
This defender checks their blind side approximately once every 2.7 seconds.
Compare: elite scanner (Anticipation=20) checks every 0.6–0.8s.
Tactical consequence: defender is frequently blind to runs behind them.
```

---

## 3.10 Constants Master Table

All constants are tagged per project-wide citation methodology:
- **[GT]** — Gameplay-tuned; validated within academic plausibility range
- **[PHYS]** — Derived from physics; not tunable without changing the physical model
- **[CROSS]** — Defined in another specification; consumed read-only here

| Constant | Value | Tag | Source / Rationale |
|----------|-------|-----|--------------------|
| `MAX_PERCEPTION_RANGE` | 120m | [GT] | Full pitch diagonal; effectively uncapped. OQ-5 resolution. |
| `BASE_FOV_ANGLE` | 160° | [GT] | Within Franks 1985 plausibility range (140°–180°) |
| `MAX_FOV_BONUS_ANGLE` | 10° | [GT] | Williams & Davids 1998 expert-novice scanning data |
| `MAX_FOV_PRESSURE_REDUCTION` | 30° | [GT] | Beilock 2010: 20–50% performance degradation; 30° ≈ 18% of 170° |
| `MIN_FOV_ANGLE` | 120° | [GT] | Safety floor; prevents degenerate near-zero cone |
| `AGENT_BODY_RADIUS` | 0.4m | [GT] | Adult shoulder half-width; consistent with Agent Movement §3.2 |
| `MIN_SHADOW_HALF_ANGLE` | 5° | [GT] | Prevents zero-width cones; below this threshold occlusion is physically meaningless |
| `L_MAX` | 5 ticks (500ms) | [GT] | Franks 1985 upper bracket for novice recognition latency |
| `L_MIN` | 1 tick (100ms) | [GT] | Helsen 1999 expert lower bracket |
| `CONFIRMATION_EXPIRY_TICKS` | 1 tick (100ms) | [DERIVED] | Minimum window to absorb single-tick shadow cone boundary noise; not GT-tuned |
| `PERIPHERAL_ARC_INNER_BOUND` | 40° (= BASE_FOV_HALF_ANGLE / 2) | [DERIVED] | Derived from BASE_FOV_HALF_ANGLE; no independent value |
| `CHECK_MAX_TICKS` | 30 ticks (3.0s) | [GT] | Franks 1985: average player scans every 3–5s |
| `CHECK_MIN_TICKS` | 6 ticks (0.6s) | [GT] | Master Vol 1 §3.1: 6–8 elite scans per possession |
| `SHOULDER_CHECK_DURATION` | 3 ticks (300ms) | [GT] | Master Vol 1 §3.1: "~0.3 seconds for a shoulder check" |
| `PRESSURE_RADIUS` | 3.0m | [CROSS] | First Touch Spec #4 §3.5.1 — authoritative |
| `MIN_PRESSURE_DISTANCE` | 0.3m | [CROSS] | First Touch Spec #4 §3.5.2 — authoritative |
| `PRESSURE_SATURATION` | 1.5 | [CROSS] | First Touch Spec #4 §3.5.3 — authoritative |
| Half-turn L_rec reduction | 15% (×0.85) | [CROSS] | First Touch Spec #4 §3.3.2 — authoritative |

**[GT] constant count:** 12 of 17 total (71%). Two constants are [DERIVED] — determined
algebraically from other constants with no independent tuning. Three are [CROSS] — defined
in upstream specifications. This is within expected range for a cognitive simulation system.

---

## Section 3 Summary

Section 3 defines six interdependent models that together produce a complete, deterministic
`PerceptionSnapshot` for each of the 22 agents per 10Hz heartbeat:

**§3.1** — FoV geometry: BASE_FOV_ANGLE = 160°, Decisions modifier adds up to 10°,
pressure degrades up to 30°, floor at 120°. Blind-side arc is the mathematical complement.

**§3.2** — Shadow cone occlusion: angular interval computed from AGENT_BODY_RADIUS = 0.4m
and occluder distance. Conservative (over-occludes slightly at close range). O(n×k) cost.
Opponents only at Stage 0.

**§3.3** — Recognition latency: L_rec linear in Decisions from 500ms (D=1) to 100ms (D=20).
Half-turn bonus (-15%) applied to entities in peripheral arc (40°–80° from facing), derived
from BASE_FOV_HALF_ANGLE — no new constant. Additive-only noise (+0 or +1 tick) preserves
L_MIN floor algebraically. Counter tracking with 1-tick expiry window (derived minimum,
not GT-tuned) to absorb single-tick shadow cone boundary crossings.

**§3.4** — Shoulder check: autonomous, 0.6s–3.0s interval by Anticipation. 3-tick window.
Blind-side entities still subject to L_rec during window. Possession doubles interval.
`ShoulderCheckAnimData` stub defined for Stage 1+.

**§3.5** — Ball perception: immediate recognition when visible (no L_rec). Same FoV +
occlusion tests as agents. `BallStalenessFrames` tracks invisibility duration.

**§3.6** — Pressure scalar: formula reused verbatim from First Touch Spec #4 §3.5. Single
use: FoV narrowing only. Not applied to L_rec or shoulder check timing.

**§3.7** — `PerceptionSnapshot` struct: 12 fields, value type, copied to Decision Tree each
heartbeat. `PerceivedAgent` sub-struct defined. Debug record and animation stub defined.

**§3.8** — Forced refresh: three qualifying trigger events. Involved agents only. L_rec = 0
on forced refresh. Standard heartbeat schedule unaffected.

**§3.9** — Worked examples for elite midfielder (Decisions=18) and low-attribute defender
under full press, with numerical verification throughout.

**§3.10** — Constants table: 16 constants, 12 [GT], 3 [CROSS], 1 [GT]. All tagged.

---

**Next:** Section 4 — Integration Contracts

---

*End of Section 3 — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
