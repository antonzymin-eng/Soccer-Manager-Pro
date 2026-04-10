# Collision System Specification â€” Appendices A, B, C

**Purpose:** Completes the Collision System Specification with formula derivations from first principles, numerical verification tables for test validation, and sample test data sets. Together with Sections 1â€“8 and Section 9 (Approval Checklist), these appendices satisfy all template requirements for formal approval.

**Created:** February 16, 2026, 5:00 PM PST  
**Revised:** February 16, 2026, 6:45 PM PST  
**Version:** 1.1  
**Status:** Draft  
**Stage:** Stage 0 â€” Physics Foundation  
**Specification:** #3 of 20  
**Dependencies:** Section 3 v1.0 (Core Systems), Section 4 v1.0 (Data Structures), Section 5 v1.0 (Testing), Section 8 v1.1 (References), Ball Physics Spec #1 Appendices v1.2 (pattern reference), Agent Movement Spec #2 Appendices v1.1 (pattern reference)

**Note:** Appendix D (Tolerance Justifications) is incorporated directly into Section 5 per the established pattern. This file contains Appendices Aâ€“C only.

---

## CHANGELOG v1.0 â†’ v1.1

**Internal Consistency Audit Corrections:**

1. **MAX_HITBOX_RADIUS corrected** (A.1.2): Changed from 0.55m â†’ **0.50m** to match Agent Movement Â§3.5.4.3 authoritative formula. Maximum combined radius updated to 1.00m (from 1.10m).

2. **CELL_SIZE decision updated** (A.1.2): Changed recommendation from 2.0m â†’ **1.0m** to match actual Section 3/4 implementation. Rationale updated to reflect that 1.0m provides a more conservative (finer) grid while remaining correct per Ericson-2005 guidelines.

3. **Grid dimensions corrected** (A.1.2): Updated to GRID_WIDTH = 106, GRID_HEIGHT = 69 to match implementation.

---

## Appendix A: Formula Derivations

This appendix provides step-by-step mathematical derivations from first principles for every core formula in Section 3. Where Section 3 presents the final implementation form, this appendix shows *how* each formula was reached and *why* each design choice was made.

**Derivation vs. tuning distinction:** Some constants in the Collision System are physics-derived (derivable from first principles or standard physics references). Others are gameplay-tuned (chosen for feel, balance, or practical reasons with no single "correct" value). Each derivation below states which category applies. Gameplay-tuned values are flagged explicitlyâ€”this honesty is preferable to fabricating post-hoc physics justifications.

---

### A.1 Spatial Hash Cell Size Derivation

#### A.1.1 Problem Statement

Choose a cell size for the spatial hash grid that:
1. Ensures no collisions are missed (correctness)
2. Minimizes query overhead (performance)
3. Balances memory usage against lookup efficiency

#### A.1.2 Derivation from First Principles

**Constraint 1: Collision Detection Correctness**

For circle-circle collision detection, two agents collide if:

```
distance(a1, a2) < radius1 + radius2
```

The maximum combined radius is:

```
max_combined_radius = MAX_HITBOX_RADIUS + MAX_HITBOX_RADIUS
                    = 0.50m + 0.50m
                    = 1.00m
```

**Note (v1.1):** MAX_HITBOX_RADIUS = 0.50m per Agent Movement Â§3.5.4.3 (Strength 20 agent). The v1.0 value of 0.55m was incorrect.

For spatial hash to detect all potential collisions, an agent must be found by querying its cell AND all adjacent cells (3Ã—3 neighborhood). This means:

```
cell_size â‰¥ max_combined_radius
cell_size â‰¥ 1.00m
```

**Constraint 2: Boundary Overlap Handling**

Agents near cell boundaries must be inserted into multiple cells to ensure they're found by queries from adjacent cells. The insertion radius is:

```
insertion_radius = hitbox_radius + epsilon
```

For an agent at position (x, y), insert into all cells where:

```
cell contains any point within insertion_radius of (x, y)
```

This means an agent can be in up to 4 cells if positioned exactly at a corner.

**Constraint 3: Performance Optimization**

Query cost scales with entities per cell. For N entities uniformly distributed across C cells:

```
entities_per_cell â‰ˆ N / C
query_cost âˆ 9 Ã— entities_per_cell  (3Ã—3 neighborhood)
```

Smaller cells = more cells = fewer entities per cell = faster queries.

However, smaller cells also mean:
- More memory (C cells Ã— list overhead)
- More insertions when agents span boundaries

**Optimal Cell Size Selection**

Given:
- Pitch dimensions: 105m Ã— 68m (with buffer: 106m Ã— 69m for boundary agents)
- Entity count: 23 (22 agents + 1 ball)
- Maximum combined radius: 1.00m

Cell size options:

| Cell Size | Grid Dimensions | Total Cells | Avg Entities/Cell | Query Entities (3Ã—3) |
|-----------|-----------------|-------------|-------------------|---------------------|
| 1.0m | 106 Ã— 69 | 7,314 | 0.003 | 0.03 |
| 2.0m | 53 Ã— 35 | 1,855 | 0.012 | 0.11 |
| 5.0m | 22 Ã— 14 | 308 | 0.075 | 0.67 |

**Decision: CELL_SIZE = 1.0m**

Rationale (updated v1.1):
- 1.0m = 1.00m maximum combined radius âœ“ (exactly meets minimum requirement)
- 7,314 cells uses more memory (~58KB with list pointers) but remains acceptable
- 0.03 entities per 3Ã—3 query is excellent performance
- Finer grid is more conservative â€” handles edge cases better
- No "breathing room" needed since MAX_HITBOX_RADIUS is unlikely to increase

**Alternative considered:** 2.0m cell size would also be correct (exceeds 1.00m requirement) with better memory efficiency. However, Section 3 and Section 4 implement 1.0m, so this appendix is updated to match the implementation rather than change the code.

**Formula (Section 3.1.2):**

```csharp
public const float CELL_SIZE = 1.0f;
public const int GRID_WIDTH = 106;   // ceil(105 / 1.0) + margin
public const int GRID_HEIGHT = 69;   // ceil(68 / 1.0) + margin
```

**Source:** Design decision based on Ericson-2005 heuristics (cell size â‰¥ largest entity diameter).

---

### A.2 Circle-Circle Collision Detection

#### A.2.1 Problem Statement

Determine if two circular hitboxes overlap in the XY plane, and if so, compute the collision manifold (normal, contact point, penetration depth).

#### A.2.2 Derivation from First Principles

**Step 1: Distance Calculation**

Two circles with centers p1, p2 and radii r1, r2 overlap if:

```
||p2 - p1|| < r1 + r2
```

Computing distance directly requires a square root, which is expensive. Instead, compare squared values:

```
||p2 - p1||Â² < (r1 + r2)Â²

(p2.x - p1.x)Â² + (p2.y - p1.y)Â² < (r1 + r2)Â²
```

This is the **squared distance test** (Ericson-2005, p. 128).

**Step 2: Collision Normal**

The collision normal points from p1 toward p2:

```
d = p2 - p1
distance = ||d||
normal = d / distance
```

If distance = 0 (exact overlap), use a fallback normal (e.g., (1, 0)).

**Step 3: Penetration Depth**

The penetration depth is how much the circles overlap:

```
penetration = (r1 + r2) - distance
```

This value is always positive when a collision is detected.

**Step 4: Contact Point**

The contact point lies on the line segment between centers, weighted by radii:

```
t = r1 / (r1 + r2)
contact_point = p1 + d Ã— t
```

This places the contact point closer to the smaller circle.

**Implementation (Section 3.2.1):**

```csharp
public static bool CheckCircleCircle(
    Vector2 p1, float r1,
    Vector2 p2, float r2,
    out CollisionManifold manifold)
{
    float dx = p2.x - p1.x;
    float dy = p2.y - p1.y;
    float distSq = dx * dx + dy * dy;
    float combinedRadius = r1 + r2;
    
    if (distSq >= combinedRadius * combinedRadius)
    {
        manifold = default;
        return false;
    }
    
    float distance = Mathf.Sqrt(distSq);
    
    // Handle exact overlap
    Vector2 normal = distance > 0.0001f
        ? new Vector2(dx / distance, dy / distance)
        : Vector2.right;
    
    float t = r1 / combinedRadius;
    Vector2 contactPoint = new Vector2(p1.x + dx * t, p1.y + dy * t);
    
    manifold = new CollisionManifold
    {
        Normal = normal,
        ContactPoint = contactPoint,
        PenetrationDepth = combinedRadius - distance
    };
    
    return true;
}
```

**Source:** Eberly-2006 Section 8.3 (pp. 478â€“492), Ericson-2005 Chapter 4 (pp. 128â€“130).

---

### A.3 Impulse-Based Collision Response

#### A.3.1 Problem Statement

Given two agents with masses m1, m2 and velocities v1, v2 that are colliding with manifold (normal n, penetration depth), compute the impulse that conserves momentum while incorporating energy loss (coefficient of restitution).

#### A.3.2 Derivation from First Principles

**Step 1: Conservation of Momentum**

Total momentum before collision equals total momentum after:

```
m1 Ã— v1 + m2 Ã— v2 = m1 Ã— v1' + m2 Ã— v2'
```

Where v1', v2' are post-collision velocities.

**Step 2: Impulse Definition**

An impulse j is an instantaneous change in momentum:

```
m1 Ã— v1' = m1 Ã— v1 + j Ã— n
m2 Ã— v2' = m2 Ã— v2 - j Ã— n
```

Note: j is applied along the collision normal n. Agent 1 receives +j, agent 2 receives -j (Newton's third law).

Rearranging:

```
v1' = v1 + (j / m1) Ã— n
v2' = v2 - (j / m2) Ã— n
```

**Step 3: Coefficient of Restitution**

The coefficient of restitution (COR, denoted e) relates separation speed to approach speed:

```
e = -(v1' - v2') Â· n / (v1 - v2) Â· n
e = -v_rel' / v_rel
```

Where v_rel = (v1 - v2) Â· n is the relative velocity along the normal.

Substituting the velocity formulas:

```
v_rel' = (v1' - v2') Â· n
       = (v1 + j/m1 Ã— n - v2 + j/m2 Ã— n) Â· n
       = v_rel + j Ã— (1/m1 + 1/m2)
```

From the COR equation:

```
-e Ã— v_rel = v_rel + j Ã— (1/m1 + 1/m2)
j Ã— (1/m1 + 1/m2) = -v_rel Ã— (1 + e)
j = -(1 + e) Ã— v_rel / (1/m1 + 1/m2)
```

**Step 4: Final Impulse Formula**

```
j = -(1 + e) Ã— (v1 - v2) Â· n / (1/m1 + 1/m2)
```

This is the standard impulse formula (Catto-2006, Hecker-1997).

**Implementation (Section 3.3.1):**

```csharp
float relativeVelocity = Vector2.Dot(v1 - v2, normal);

// Skip if separating
if (relativeVelocity > 0) return;

float invMass1 = 1f / m1;
float invMass2 = agent2.IsGrounded ? 0f : 1f / m2;

float impulseMagnitude = -(1f + COEFFICIENT_OF_RESTITUTION) * relativeVelocity 
                        / (invMass1 + invMass2);

Vector2 impulse = impulseMagnitude * normal;
v1' = v1 + impulse / m1;
v2' = v2 - impulse / m2;
```

**Source:** Catto-2006 "Fast and Simple Physics using Sequential Impulses", Hecker-1997 "Physics, Part 4".

---

### A.4 Penetration Resolution

#### A.4.1 Problem Statement

After impulse is applied, agents may still overlap (penetration). Compute position corrections to separate them.

#### A.4.2 Derivation

The minimum translation vector (MTV) moves agents apart along the collision normal:

```
total_separation = penetration Ã— SEPARATION_FACTOR
```

Where SEPARATION_FACTOR > 1.0 (e.g., 1.01) ensures slight over-separation to prevent re-collision next frame.

Position corrections are distributed inversely proportional to mass:

```
correction1 = -total_separation Ã— (invMass1 / (invMass1 + invMass2)) Ã— normal
correction2 = +total_separation Ã— (invMass2 / (invMass1 + invMass2)) Ã— normal
```

Heavier agents move less; lighter agents move more.

**Implementation (Section 3.3.4):**

```csharp
float totalInvMass = invMass1 + invMass2;
float separation = penetration * SEPARATION_FACTOR;

Vector2 correction1 = -separation * (invMass1 / totalInvMass) * normal;
Vector2 correction2 = +separation * (invMass2 / totalInvMass) * normal;

position1 += correction1;
position2 += correction2;
```

**Source:** Catto-2006, Ericson-2005 Chapter 4.

---

### A.5 Fall/Stumble Severity System

#### A.5.1 Problem Statement

Determine when a collision causes an agent to stumble (brief recovery) or fall (grounded state).

#### A.5.2 Design Rationale

**This is a gameplay-tuned system, not physics-derived.**

Real football players fall based on complex biomechanical factors (balance, foot placement, anticipation, intent to dive). Simulating this accurately is beyond Stage 0 scope. Instead, we use a severity-based probabilistic system tuned to achieve realistic-looking fall/stumble ratios.

**Severity Calculation:**

```
severity = |impulse| Ã— framerate
         = |j| Ã— 60
```

This produces a collision severity score (not true Newtonian force). The Ã— 60 factor converts from "per frame" to "per second" scale.

**Threshold Comparison:**

```
fall_threshold = FALL_FORCE_BASE + (Strength Ã— FALL_FORCE_PER_STRENGTH)
stumble_threshold = fall_threshold Ã— STUMBLE_THRESHOLD_FRACTION
```

- If severity > fall_threshold: Calculate P(fall)
- If severity > stumble_threshold: Calculate P(stumble)
- Otherwise: No effect

**Important calibration note (v1.1):** The severity values for typical collisions (~16,000â€“33,000) significantly exceed the base thresholds (500â€“1,500). This means most moderate-to-hard collisions will exceed thresholds. The probability ramp (FALL_PROBABILITY_RANGE) is where the actual tuning occurs. Implementation must validate that the resulting fall/stumble ratios match targets (5% fall, 25% stumble, 70% none) and adjust thresholds if needed.

**Source:** Empirically chosen for gameplay. Target ratios (5%/25%/70%) based on observation of professional matches.

---

### A.6 Same-Team Collision Factor

#### A.6.1 Problem Statement

Teammates should not knock each other over as hard as opponents.

#### A.6.2 Design Rationale

**This is a gameplay-tuned value.**

In real football, teammates have spatial awareness and avoid hard contact. Modeling this accurately would require intent simulation. Instead, we apply a momentum scale factor:

```
if (agent1.TeamID == agent2.TeamID)
    impulse *= SAME_TEAM_MOMENTUM_SCALE; // 0.3
```

This produces gentle "brushing past" behavior for teammates.

**Source:** Empirically chosen. Range [0.2, 0.5] is reasonable.

---

### A.7 Coefficient of Restitution Selection

#### A.7.1 Problem Statement

Choose e (COR) for agent-agent collisions.

#### A.7.2 Design Rationale

**This is a gameplay-tuned value within physics constraints.**

Physical bounds:
- e = 0: Perfectly inelastic (agents stick together) â€” undesirable
- e = 1: Perfectly elastic (full energy retained) â€” unrealistic for humans

Real human collisions are highly inelastic due to soft tissue, padding, and energy dissipation.

**Selected: e = 0.3**

Rationale:
- Produces visible but dampened "bounce"
- Agents separate after collision (e > 0)
- Most kinetic energy dissipated (e < 0.5)
- Matches observed football collision aesthetics

**Source:** Design decision. Literature on human body COR suggests values in [0.1, 0.4] range depending on contact area and velocity.

---

## Appendix B: Numerical Verification Tables

This appendix provides hand-calculated expected values for test cases in Section 5.

### B.1 Impulse Calculation Verification

#### B.1.1 Equal Mass Head-On Collision (CR-001)

**Setup:**
- Agent 1: m1 = 85 kg, v1 = (5, 0) m/s
- Agent 2: m2 = 85 kg, v2 = (-5, 0) m/s
- COR: e = 0.3
- Normal: n = (1, 0)

**Calculation:**

```
Step 1: Inverse masses
  invMass1 = 1/85 = 0.01176
  invMass2 = 1/85 = 0.01176

Step 2: Relative velocity along normal
  v_rel = (v1 - v2) Â· n
        = (5 - (-5), 0) Â· (1, 0)
        = 10 m/s

Step 3: Impulse magnitude
  j = -(1 + e) Ã— v_rel / (invMass1 + invMass2)
    = -(1.3) Ã— 10 / (0.01176 + 0.01176)
    = -13 / 0.02353
    = -552.5 kgÂ·m/s

Step 4: Velocity changes
  Î”v1 = j / m1 = -552.5 / 85 = -6.5 m/s
  Î”v2 = -j / m2 = 552.5 / 85 = 6.5 m/s

Step 5: Final velocities
  v1' = 5 + (-6.5) = -1.5 m/s (reversed)
  v2' = -5 + 6.5 = 1.5 m/s (reversed)
```

**Expected Test Values:**
- v1' â‰ˆ -1.5 m/s Â± 0.2 m/s
- v2' â‰ˆ 1.5 m/s Â± 0.2 m/s

**Verification:**
- Momentum conserved: 85Ã—5 + 85Ã—(-5) = 0 = 85Ã—(-1.5) + 85Ã—1.5 âœ“
- Energy lost: KE_before = 2125 J, KE_after = 191 J (91% energy loss) âœ“

---

#### B.1.2 Asymmetric Mass Collision (CR-002)

**Setup:**
- Agent 1: m1 = 100 kg (heavy), v1 = (5, 0) m/s
- Agent 2: m2 = 72.5 kg (light), v2 = (0, 0) m/s (stationary)
- COR: e = 0.3
- Normal: n = (1, 0)

**Calculation:**

```
Step 1: Inverse masses
  invMass1 = 0.01
  invMass2 = 0.0138

Step 2: Relative velocity
  v_rel = 5 m/s

Step 3: Impulse magnitude
  j = -(1.3) Ã— 5 / (0.01 + 0.0138)
    = -6.5 / 0.0238
    = -273.1 kgÂ·m/s

Step 4: Velocity changes
  Î”v1 = -273.1 / 100 = -2.73 m/s
  Î”v2 = 273.1 / 72.5 = 3.77 m/s

Step 5: Final velocities
  v1' = 5 - 2.73 = 2.27 m/s (slowed)
  v2' = 0 + 3.77 = 3.77 m/s (accelerated)
```

**Expected Test Values:**
- v1' â‰ˆ 2.3 m/s Â± 0.3 m/s
- v2' â‰ˆ 3.8 m/s Â± 0.3 m/s

---

#### B.1.3 Grounded Agent Collision (CR-003)

**Setup:**
- Agent 1: m1 = 85 kg, v1 = (7, 0) m/s (running)
- Agent 2: m2 = 85 kg, IsGrounded = true (infinite effective mass)
- COR: e = 0.3
- Normal: n = (1, 0)

**Calculation:**

```
Step 1: Inverse masses
  invMass1 = 1/85 = 0.01176
  invMass2 = 0 (grounded = infinite mass)

Step 2: Relative velocity
  v_rel = 7 m/s

Step 3: Impulse magnitude
  j = -(1.3) Ã— 7 / (0.01176 + 0)
    = -9.1 / 0.01176
    = -773.8 kgÂ·m/s

Step 4: Velocity changes
  Î”v1 = -773.8 / 85 = -9.1 m/s
  Î”v2 = 0 (grounded)

Step 5: Final velocities
  v1' = 7 - 9.1 = -2.1 m/s (reversed direction)
  v2' = 0 m/s (unchanged)
```

**Expected Test Values:**
- v1' â‰ˆ -2.1 m/s Â± 0.3 m/s (reversed)
- v2' = 0 m/s (unchanged)

---

### B.2 Penetration Resolution Verification

#### B.2.1 Equal Mass Separation (CR-004)

**Setup:**
- Agent 1: m1 = 85 kg, position (50, 34), radius = 0.40m
- Agent 2: m2 = 85 kg, position (50.7, 34), radius = 0.40m
- Penetration: 0.80 - 0.70 = 0.10m
- Normal: n = (1, 0)
- SEPARATION_FACTOR = 1.01

**Calculation:**

```
Step 1: Inverse masses
  invMass1 = 1/85 = 0.01176
  invMass2 = 1/85 = 0.01176
  totalInvMass = 0.02353

Step 2: Separation distance
  separation = 0.10 Ã— 1.01 = 0.101m

Step 3: Corrections
  correction1 = -0.101 Ã— (0.01176 / 0.02353) = -0.101 Ã— 0.5 = -0.0505m
  correction2 = +0.101 Ã— (0.01176 / 0.02353) = +0.101 Ã— 0.5 = +0.0505m

Step 4: New positions
  position1' = (50 - 0.0505, 34) = (49.9495, 34)
  position2' = (50.7 + 0.0505, 34) = (50.7505, 34)
```

**Verification:**
- New distance: 50.7505 - 49.9495 = 0.801m > 0.80m (combined radius) âœ“
- Equal movement for equal masses âœ“

---

#### B.2.2 Asymmetric Mass Separation (CR-005)

**Setup:**
- Agent 1: m1 = 100 kg (heavy)
- Agent 2: m2 = 72.5 kg (light)
- Penetration: 0.10m

**Calculation:**

```
Step 1: Inverse masses
  invMass1 = 0.01
  invMass2 = 0.0138
  totalInvMass = 0.0238

Step 2: Corrections
  correction1 = -0.101 Ã— (0.01 / 0.0238) = -0.101 Ã— 0.42 = -0.0424m
  correction2 = +0.101 Ã— (0.0138 / 0.0238) = +0.101 Ã— 0.58 = +0.0586m
```

**Verification:**
- Heavy agent moves 0.0424m (42%)
- Light agent moves 0.0586m (58%)
- Total movement = 0.101m âœ“

---

### B.3 Collision Severity Verification

#### B.3.1 Typical Collision Severity (FL-001)

**Setup:**
- Equal mass head-on collision (from B.1.1)
- |j| = 552.5 kgÂ·m/s

**Calculation:**

```
severity = |j| Ã— 60
         = 552.5 Ã— 60
         = 33,150 severity units
```

**Note:** This exceeds the outline's threshold range (500â€“1,500). Implementation must calibrate thresholds.

---

#### B.3.2 Gentle Brush Collision (FL-002)

**Setup:**
- Agent 1: m1 = 85 kg, v1 = (2, 0) m/s (jogging)
- Agent 2: m2 = 85 kg, v2 = (1, 0) m/s (jogging same direction)
- Relative velocity: 1 m/s

**Calculation:**

```
v_rel = 1 m/s
j = -(1.3) Ã— 1 / (0.02353) = -55.25 kgÂ·m/s
severity = 55.25 Ã— 60 = 3,315 severity units
```

Much lower severity than head-on collision, but still above outline thresholds.

---

## Appendix C: Test Data Sets

This appendix provides sample agent configurations and expected collision outcomes for comprehensive testing.

### C.1 Agent Configuration Templates

#### C.1.1 Standard Test Agents

| Agent ID | Mass (kg) | Hitbox Radius (m) | Strength | Description |
|----------|-----------|-------------------|----------|-------------|
| LIGHT | 72.5 | 0.3525 | 1 | Small, weak player |
| AVERAGE | 85.0 | 0.4250 | 10 | Average player |
| HEAVY | 100.0 | 0.50 | 20 | Large, strong player |
| GROUNDED | 85.0 | 0.4250 | 10 | Average player, IsGrounded=true |

**Note (v1.1):** Hitbox radius values corrected to match Agent Movement Â§3.5.4.3 formula.

#### C.1.2 Test Positions (Pitch Center)

| Position Name | X (m) | Y (m) | Description |
|--------------|-------|-------|-------------|
| CENTER | 52.5 | 34.0 | Pitch center |
| LEFT | 50.0 | 34.0 | 2.5m left of center |
| RIGHT | 55.0 | 34.0 | 2.5m right of center |
| NEAR | 52.9 | 34.0 | 0.4m right of center (collision distance) |

---

### C.2 Collision Scenario Test Cases

#### C.2.1 Head-On Collision Matrix

| Scenario | Agent 1 | Agent 2 | v1 (m/s) | v2 (m/s) | Expected Outcome |
|----------|---------|---------|----------|----------|------------------|
| Equal-Equal | AVERAGE | AVERAGE | (5,0) | (-5,0) | Both reverse, equal speed |
| Heavy-Light | HEAVY | LIGHT | (5,0) | (-5,0) | Heavy slows less, light bounces more |
| Running-Stationary | AVERAGE | AVERAGE | (8,0) | (0,0) | Runner slows, stationary accelerates |
| Running-Grounded | AVERAGE | GROUNDED | (7,0) | (0,0) | Runner bounces back, grounded unmoved |

#### C.2.2 Glancing Collision Matrix

| Scenario | Angle | v1 (m/s) | v2 (m/s) | Expected Outcome |
|----------|-------|----------|----------|------------------|
| 45Â° approach | 45Â° | (5,5) | (0,0) | Partial deflection |
| 90Â° cross | 90Â° | (5,0) | (0,5) | Both deflect ~45Â° |
| Same direction | 0Â° | (8,0) | (5,0) | Faster slows, slower speeds up |

#### C.2.3 Same-Team Collision Cases

| Scenario | Agent 1 | Agent 2 | Same Team | Factor | Expected |
|----------|---------|---------|-----------|--------|----------|
| Opponents | AVERAGE | AVERAGE | No | 1.0 | Full impulse |
| Teammates | AVERAGE | AVERAGE | Yes | 0.3 | 30% impulse |

---

### C.3 Spatial Hash Test Positions

#### C.3.1 Cell Boundary Test Positions

| Position | X (m) | Y (m) | Expected Cells |
|----------|-------|-------|----------------|
| Center of cell (0,0) | 0.5 | 0.5 | 1 cell |
| Edge of cell | 1.0 | 0.5 | 2 cells (straddles boundary) |
| Corner of cell | 1.0 | 1.0 | 4 cells (straddles corner) |
| Pitch corner | 0.1 | 0.1 | 1 cell (clamped to grid) |
| Pitch edge | 104.9 | 34.0 | 1-2 cells (boundary handling) |

**Note (v1.1):** Cell boundary positions adjusted for CELL_SIZE = 1.0m.

#### C.3.2 Clustering Test Positions (Corner Kick)

12 agents clustered in penalty area (18m Ã— 40m zone):

| Agent | X (m) | Y (m) | Notes |
|-------|-------|-------|-------|
| 0 | 95.0 | 24.0 | Penalty area corner |
| 1 | 97.0 | 26.0 | Near goal |
| 2 | 99.0 | 28.0 | Near goal |
| 3 | 101.0 | 30.0 | 6-yard box |
| 4 | 103.0 | 32.0 | Goal line |
| 5 | 95.0 | 36.0 | Central |
| 6 | 97.0 | 38.0 | Central |
| 7 | 99.0 | 40.0 | Far post |
| 8 | 101.0 | 42.0 | Far post |
| 9 | 103.0 | 44.0 | Goal line |
| 10 | 96.0 | 34.0 | Penalty spot |
| 11 | 98.0 | 34.0 | 6-yard box edge |

This configuration tests maximum query load (many overlapping cells).

---

### C.4 Ball Collision Test Configurations

#### C.4.1 Agent-Ball Collision Cases

| Scenario | Agent Pos | Ball Pos | Ball Vel | Expected |
|----------|-----------|----------|----------|----------|
| Ball at feet | (50, 34, 0) | (50.3, 34, 0.11) | (5, 0, 0) | Collision detected |
| Ball in air | (50, 34, 0) | (50.3, 34, 1.5) | (5, 0, 1) | No collision (height filter) |
| Ball behind | (50, 34, 0) | (49.5, 34, 0.11) | (-5, 0, 0) | Collision detected |

---

## Document Status

**Appendices Aâ€“C Completion (v1.1):**
- âœ… Appendix A: 7 formula derivations with step-by-step math
  - A.1: Spatial hash cell size (corrected for CELL_SIZE = 1.0m)
  - A.2: Circle-circle collision detection
  - A.3: Impulse-based collision response
  - A.4: Penetration resolution
  - A.5: Fall/stumble severity system
  - A.6: Same-team collision factor
  - A.7: Coefficient of restitution selection
- âœ… Appendix B: 6 numerical verification tables
  - B.1: Impulse calculations (3 scenarios)
  - B.2: Penetration resolution (2 scenarios)
  - B.3: Collision severity (2 scenarios)
- âœ… Appendix C: Test data sets (corrected for hitbox radius values)
  - C.1: Agent configuration templates
  - C.2: Collision scenario matrices
  - C.3: Spatial hash test positions
  - C.4: Ball collision test cases
- âš ï¸ Appendix D: Tolerance derivations (in Section 5, not duplicated here)

**Page Count:** ~18 pages

**Quality Checks:**
- âœ… All formulas traced to sources (Catto-2006, Ericson-2005, Eberly-2006)
- âœ… Gameplay-tuned values explicitly flagged
- âœ… Numerical examples include intermediate steps
- âœ… Verification checks (momentum conservation, energy loss)
- âœ… Test configurations cover edge cases
- âœ… Internal consistency with Sections 3 and 4 verified (v1.1)
- âš ï¸ Severity threshold calibration noted as implementation task

**Ready for:** Review and approval

---

**END OF APPENDICES A, B, C**
