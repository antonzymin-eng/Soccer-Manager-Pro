# Master Volume I: Physics & Simulation Core

**Created:** December 29, 2025  
**Version:** 2.0 (Consolidated)  
**Scope:** The 10Hz Heartbeat, Match Logic, and Physical Environment  
**Philosophy:** Natural Simulation (Deterministic, Zero-Magic)

---

## Table of Contents

1. [Engine Architecture](#1-engine-architecture)
2. [Agent Physicality](#2-agent-physicality)
3. [Match Logic - PDE Pipeline](#3-match-logic-pde-pipeline)
4. [Environmental Physics](#4-environmental-physics)
5. [Officiating & Rules](#5-officiating-rules)
6. [Tactical Micro-Physics](#6-tactical-micro-physics)

---

## 1. Engine Architecture

### 1.1 The Decoupled Timer

**The engine operates on a dual-frequency system to balance simulation accuracy with visual smoothness:**

#### Tactical Heartbeat (10Hz)
- **Frequency:** Every 100ms (10 times per second)
- **Purpose:** Logic processing, PDE cycles, decision-making
- **Components:**
  - Recognition latency calculations
  - Decision tree evaluation
  - Tactical instruction application
  - Social/psychological state updates

#### Physics Step (60Hz+)
- **Frequency:** 60+ times per second
- **Purpose:** Smooth visual interpolation
- **Components:**
  - Position interpolation
  - Ball rotation and trajectory
  - Skeletal animations
  - Camera movements

**Benefits:**
- Separates "thinking" (10Hz) from "moving" (60Hz)
- Allows complex AI without visual stuttering
- Reduces CPU load by limiting expensive calculations

---

### 1.2 Dual-Engine System

To simulate an entire football universe efficiently:

#### Active Engine (Full Physics)
- **Usage:** User's match OR specifically watched matches
- **Characteristics:**
  - Full 3D collision detection
  - Real-time PDE pipeline
  - Complete environmental simulation
  - High CPU cost, high accuracy
- **Performance:** Real-time (1:1 time ratio)

#### Background Engine (Statistical Abstraction)
- **Usage:** All other global matches (500+ concurrent)
- **Logic:**
  - Resolves **Tactical Interactions** instead of collisions
  - Formula: `Team_A_Pressing_Efficiency vs Team_B_Resistance = Turnover_Probability`
  - Generates identical output metrics (xG, Fatigue, Form)
- **Performance:** <30 seconds for full match day simulation
- **Data Parity:** Scout reports are seamless across engines

---

### 1.3 Determinism

**Critical for cross-platform consistency and anti-cheat:**

#### Fixed-Point Mathematics
- **Implementation:** Custom `Fixed64` struct library
- **Structure:**
  - Wraps `Int64` (64-bit integer)
  - 32 bits for integer part
  - 32 bits for fractional part
- **Operations:** Uses bit-shifting instead of floating-point arithmetic
  - Addition, subtraction, multiplication, division
  - All physics calculations (Position, Velocity, Acceleration)
- **Compilation Rule:** Usage of `System.Math`, `float`, or `double` inside GameLoop triggers compilation error

#### RNG Seeding
- **Pre-Match Generation:** `Simulation_Seed` generated at match setup
- **Deterministic Outcomes:** Reloading a save produces identical results
- **Anti-Save-Scum:** Seed is set at `Pre_Match_Setup`, not at kickoff
  - User cannot reload to change match outcome
  - Injuries, referee decisions, skill expression all predetermined

---

## 2. Agent Physicality

### 2.1 Recognition Latency ($L_{rec}$)

**The foundation of realistic agent behavior - how quickly an agent recognizes game situations:**

#### Formula
```
L_rec = Biological_Floor + (Max_Attr_Scale - Effective_Mental_Avg) Ã— Tick_Cost
```

**Components:**
- **Biological_Floor:** ~200ms (human visual processing minimum)
- **Max_Attr_Scale:** Attribute ceiling (typically 20)
- **Effective_Mental_Avg:** Average of:
  - Decisions
  - Anticipation
  - Composure
  - Concentration
- **Tick_Cost:** Conversion to simulation ticks (100ms per tick)

**Modifiers:**
- **Information_Density:** High-chaos areas increase $L_{rec}$
- **Neural_Fatigue:** Mental tiredness slows recognition
- **Language_Latency ($L_{com}$):** If agent doesn't share language with teammates, adds 500ms delay
- **Effective_Concentration:** Reduced by Acoustic_Distortion (crowd pressure)

**Example:**
- Elite midfielder (Mental Avg = 18): ~220ms recognition
- Average player (Mental Avg = 12): ~280ms recognition
- Fatigued player: +60-100ms additional delay

---

### 2.2 Bio-Mechanical Profiling

#### Running Gait Analysis
Each agent is assigned a **Kinetic_Profile** affecting injury susceptibility:

**Profile Types:**
- **Heavy Heel-Striker:** Higher impact forces, prone to knee injuries on hard pitches
- **Forefoot Runner:** More Achilles stress, vulnerable on slippery surfaces
- **Neutral Gait:** Balanced load distribution

**Implementation:**
- Certain profiles have lower `Tissue_Load_Threshold` on specific pitch types
- Identifies "injury prone" players through physics, not random number generation
- Affects optimal boot stud configuration

#### Body Mechanics
- **Center of Gravity:** Lower CoG = better balance, harder to dispossess
- **Stride Length:** Affects acceleration vs. top speed characteristics
- **Hip Mobility:** Influences turning radius and agility
- **Proprioception:** Spatial awareness without visual input

---

### 2.3 Youth Development - Bio-Banding

**Addresses the "late bloomer" problem in youth football:**

#### Concept
- Separates **Chronological_Age** from **Biological_Maturity**
- **Maturity Offset:** Measures development relative to Peak_Height_Velocity (PHV)

**Implementation:**
- Youth players grouped by biological age, not birth year
- Late bloomers (Offset < 0) protected from being cut
- Early developers flagged to prevent over-reliance on temporary physical advantages

#### Growth Spurts
- **Peak_Height_Velocity Event:** Temporary coordination penalty
  - "Bambi on Ice" phase
  - Reduced balance and agility
  - Increased Collision_Hitbox_Uncertainty
- **Duration:** 3-6 months in-game time
- **Recovery:** Attributes normalize post-growth

---

### 2.4 Pathophysiology

**Injury mechanics based on sports science, not dice rolls:**

#### ACWR (Acute:Chronic Workload Ratio)
```
ACWR = Acute_Load (last 7 days) / Chronic_Load (last 28 days)
```

**Zones:**
- **Safe Zone:** 0.8 - 1.3 (optimal performance)
- **Danger Zone:** > 1.5 (exponential injury risk)
- **Undertraining:** < 0.8 (loss of match fitness)

**Medical Veto:**
- If `ACWR > 2.0`, medical staff issues selection veto
- Manager can override, triggering mandatory `Soft_Tissue_Injury_Event`
  - Hamstring strain (most likely)
  - Groin pull
  - Calf tear

#### ACL Injury Mechanism
**Not random - triggered by specific physics conditions:**

```
ACL_Risk = Deceleration_Force Ã— Rotational_Torque Ã— Surface_Friction
```

**High-Risk Scenarios:**
- Sudden direction change on high-friction surface
- Planting leg at awkward angle
- Contact while decelerating
- Landing from aerial duel with rotation

**Prevention:**
- Strength training reduces `Tissue_Stress_Multiplier`
- Functional Movement Screening identifies vulnerable players
- Pitch conditions (waterlogged = lower friction = lower ACL risk, paradoxically)

#### Injury Types (ICD-10 Based)
- **ACL_Rupture:** 6-9 months, requires reconstruction
- **MCL_Sprain_Grade1:** 2-3 weeks, conservative treatment
- **Hamstring_Tear_G2:** 4-6 weeks, high re-injury risk
- **Metatarsal_Fracture:** 6-8 weeks, boot required
- **Concussion_Protocol:** 7-14 days, graduated return
- **Groin_Strain:** 2-4 weeks, bilateral risk
- **Achilles_Tendinopathy:** Chronic management, degrades over seasons

#### Recovery States
1. **Inflammation Phase:** 24-72 hours, pain and swelling
2. **Proliferation Phase:** Tissue repair, progressive loading
3. **Remodeling Phase:** Strength building, sport-specific drills
4. **Match_Fitness_Decay:** Separate from injury healing
   - Endurance decays faster than technique
   - Requires specific Return_to_Play protocol

#### HRV (Heart Rate Variability)
- **Daily Measurement:** Hidden scalar for `Neural_Fatigue`
- **High HRV:** Faster recovery of *Effective_Concentration*
- **Low HRV:** Warning sign of overtraining or illness
- **Implementation:** Not shown to user, affects medical recommendations

---

### 2.5 Nutritional State

#### Carb-Loading Cycles
- **Match Day Minus 1 (MD-1):** Glycogen supercompensation
- **Impact:** Scales the `Anaerobic_Burst_Limit`
- **Missed Cycle:** Agent's `High_Intensity_Sprint_Count` capped at ~75% normal

#### In-Match Nutrition
- **Halftime:** Energy gel option (increases second-half endurance by 5-8%)
- **Hydration:** Dehydration penalty kicks in after 60 minutes in hot weather
  - Increased cramping risk
  - Reduced cognitive function

---

## 3. Match Logic - PDE Pipeline

**The three-layer decision system that transforms perception into action:**

### 3.1 Layer 1: Perception (Tactical Intelligence)

#### Visual Processing

**Occlusion & Shadow Cones:**
- Agents project "Shadow Cones" from their body position
- Receivers inside an opponent's shadow cone:
  - `Connection_Fidelity = 0` (passer cannot "see" them as viable target)
  - Must move into open space to become passing option

**Blind Side Awareness:**
- Default `Perception_Error (P_e) = 1.0` for threats from 200Â° rear arc
- Reduced by "Shoulder Check" action:
  - High Anticipation agents perform checks automatically
  - Each check costs ~0.3 seconds of reaction time
  - Grants temporary awareness of blind-side runners

**Scanning Frequency:**
- Elite players: 6-8 scans per possession phase
- Average players: 2-4 scans per possession phase
- Dictates quality of decision-making under pressure

---

#### Zone 14 (The Golden Square)
**The most dangerous attacking zone:**

- **Location:** Central area just outside penalty box
- **Physics Benefit:** `Reception_Smoothing` scalar applied
  - Reduces `X_e` (Execution Error) on first touch
  - Simulates the "time and space" advantage
- **Defensive Response:** AI prioritizes pressing triggers in Zone 14

---

#### The Corridor of Uncertainty
**A goalkeeper's nightmare:**

- **Definition:** Crosses with Z-axis height between 0.5m - 1.5m
  - Too high to safely leave
  - Too low to confidently claim
- **GK Logic:**
  - `GK_Decision_Latency` spikes by +300ms
  - Agent toggles between "Claim" and "Stay" states
  - Poor Decisions attribute = higher error rate
- **Exploitation:** AI coaches identify GK weakness and target corridor

---

#### Second Ball Density
**The chaos after aerial duels:**

- **Trigger:** Contested header or clearance
- **Effect:** Creates `Chaos_Pulse` in 5m radius
  - `Information_Density` temporarily maximized
  - Forces agents into "Duel State" (high aggression, reduced positioning)
- **Winner:** Agent with highest Aggression + Anticipation combination
- **Coaching:** "Second Ball" instructions bias midfielder positioning

---

### 3.2 Layer 2: Decision (Professional Tactics)

#### Hysteresis (Decision Memory)
**Prevents agents from oscillating between choices:**

- **Memory Buffer:** Agent retains last 3 decisions
- **Threshold Logic:** Single bad event doesn't shift mental state
  - Requires N consecutive events to trigger state change
  - Simulates human "benefit of doubt" thinking
- **Example:** 
  - One misplaced pass doesn't trigger "Safe" mode
  - Three consecutive turnovers shifts to conservative passing

---

#### Tactical Fouls
**Professional decision to stop dangerous attacks:**

**Trigger Conditions:**
```
IF Opponent_Counter_Value > Foul_Threshold 
AND Agent_Aggression > Risk_Acceptance_Level
THEN Initiate Collision_Event (without ball-intent)
```

**Outcome:**
- Resets match state to Set_Piece
- `Booking_Probability` calculated based on:
  - Referee strictness
  - Previous fouls accumulated
  - Match importance
  - Desperation factor (losing late in game)

**AI Coaching:**
- Teams with "Get Stuck In" instruction have lower `Foul_Threshold`
- Cautious teams avoid tactical fouls unless critical

---

#### Shadow Marking
**Defending without visual contact:**

- **Mechanism:** Agents back-pedaling use `Proprioceptive_Coordinate_Caching`
  - Tracks opponent position via spatial memory
  - Updates when opponent enters peripheral vision
- **Decay:** Memory accuracy degrades over time without visual refresh
- **Cover Shadow:** Defender positions body to block passing lanes, not just mark player
- **Coaching:** "Tight Marking" instruction reduces cache duration (forces constant visual contact)

---

#### Game State Scenarios
**Automatic tactical adjustments based on match context:**

**Scenarios:**
- **Protecting_Lead:** Score_Delta > 0, Time_Remaining < 20 minutes
  - Shifts `Instructional_Bias (I_b)` toward defensive shape
  - Reduces risk-taking on forward passes
- **Desperate_Chase:** Score_Delta < -1, Time_Remaining < 15 minutes
  - Increases long-ball frequency
  - Commits more players forward
  - Reduces defensive compaction
- **Clean_Sheet_Hunt:** Score_Delta = 0, Defensive_Record bonus active
  - Maintains defensive discipline
  - Caution on tackles

---

### 3.3 Layer 3: Execution (Motor Control)

#### Dismarking (Creating Separation)
**Physics of offensive movement:**

**Double Movements (Feints):**
- **Mechanism:** Create `Momentum_Inertia` trap for defender
  - Attacker initiates movement in Direction_A
  - Defender commits weight shift
  - Attacker exploits defender's momentum with Direction_B cut
- **Success Rate:** Based on:
  ```
  Feint_Success = (Attacker_Agility - Defender_Anticipation) / Defender_Balance
  ```
- **Cost:** Slight reduction in Attacker's velocity during feint execution

**Third Man Runs:**
- Curved run from blind side into space vacated by second runner
- Defender logic: Must choose between tracking First_Runner or Third_Man
- AI identification: Recognizes `Shadow_Play` patterns

---

#### Phase Skipping
**When the game bypasses midfield:**

**Trigger Conditions:**
- Both teams in high attacking intent
- High turnover frequency
- Tactical instruction: "Direct Passing"

**Effects:**
- Disables `Formation_Morphing` (teams stay in attacking shape)
- Agents remain in chaotic coordinates (no defensive reset)
- "End-to-End" state activated
  - High entertainment value
  - Increased injury risk (constant sprinting)
  - Fatigue accelerates

---

#### Touch Mechanics

**Touch-Radius Variance:**
- **Formula:** 
  ```
  Control_Quality = Agent_Technique / (Ball_Velocity Ã— Agent_Inertia)
  ```
- **Perfect Control:** Ball "sticks" within 0.3m radius
- **Heavy Touch:** Ball escapes up to 2m (allows defender to intercept)

**The "Half-Turn" Anchor:**
- **Body Orientation Bonus:** If receiver's body is at 45Â° to ball vector:
  - `L_rec` reduced by 15%
  - Enables faster next action
- **Coaching:** "Half-Turn" trait emerges naturally from positioning intelligence

---

## 4. Environmental Physics

### 4.1 Pitch Characteristics

#### Desso GrassMaster (Hybrid Pitch)
**Modern pitch technology with physical implications:**

**Properties:**
- High `Root_Tensile_Strength`
- Prevents pitch degradation in goal mouths
- Reduces `Surface_Friction_Variance`

**Impact:**
- Decreases `Pivot_Injury_Risk_Coefficient` (fewer ACL tears)
- More consistent ball roll (reduces randomness)
- Higher maintenance cost (financial impact)

---

#### Dynamic Degradation
**Pitch quality changes during match:**

**Footfall Density Tracking:**
- High-traffic zones (center circle, goal mouth) accumulate wear
- **Threshold:** After 500 footfalls in 1mÂ² area:
  - Tile state changes to "Degraded"
  - Increased `Surface_Friction` (40% higher)
  - Ball bounce becomes erratic

**Recovery:**
- Groundskeeper quality determines overnight recovery rate
- Poor facilities: Visible degradation persists for multiple matches

---

#### Variable Dimensions
**Not all pitches are equal:**

- **League Regulations:** Define Min/Max dimensions
  - Example: 100m Ã— 64m to 105m Ã— 68m
- **Tactical Impact:**
  - Wider pitches favor wing play (lower `Voronoi_Density`)
  - Narrower pitches favor central pressing (higher compaction)
- **Manager Choice:** Pre-season option to alter pitch within regulations
  - Suits tactical style
  - Home field advantage through familiarity

---

### 4.2 Weather Systems

#### Static States
- **Light_Rain:** +10% `Surface_Friction` (ball slows faster)
- **Torrential_Rain:** -30% `Surface_Friction`, +50% `Bounce_Variance`
- **Snow_Flurries:** -20% visibility, lines obscured
- **Blizzard:** Match suspension logic activated
- **High_Wind_Swirling:** +40% cross accuracy penalty
- **Fog_Density:** Reduces `Visual_Range` to 30m
- **Heat_Wave:** Accelerates hydration depletion

#### Dynamic Weather Transitions
**Weather changes mid-match:**

- **Weather_Seed:** Defines state transitions at `Time_Keyframes`
  - Example: Clear â†’ Light Rain at 60'
  - Friction coefficients update dynamically
- **Impact:** Teams with "Heavy Touch" players suffer more in rain

---

### 4.3 Circadian Rhythm & Travel

#### Jet Lag Mechanics
**Time zone travel affects performance:**

```
Jet_Lag_Penalty = abs(Agent_Timezone_Clock - Local_Stadium_Time) Ã— Disruption_Factor
```

**Impact:**
- Mismatch > 3 hours: Increases `L_rec` by 10-15%
- Affects `Homeostatic_Baseline` recovery rate
- Sleep quality degraded (shown in HRV metrics)

**Mitigation:**
- Early arrival (2+ days before match) reduces penalty by 50%
- Bright light exposure protocols
- Scheduled team meals sync circadian rhythm

---

## 5. Officiating & Rules

### 5.1 The Referee Agent

#### Decision Latency
**Referees are agents with their own processing:**

- **Base Latency:** 150-200ms (elite referee)
- **Modifiers:**
  - Speed of play (faster = harder to track)
  - Viewing angle (poor position = longer delay)
  - Acoustic_Bias (crowd pressure influences close calls)

#### Strictness Profiles
- **Strict_Carder:** Low `Foul_Threshold`, books early
- **Lets_Play_Flow:** High tolerance, advantage-focused
- **VAR_Reliant:** Waits for VAR on marginal calls
- **Home_Crowd_Influenced:** `Acoustic_Bias_Coefficient` > 0

---

### 5.2 VAR System

#### Clear and Obvious Standard
```
VAR triggers IF:
  abs(Engine_Truth - Referee_Perception) > Clear_And_Obvious_Constant
```

**Implementation:**
- Engine knows "truth" (collision occurred, offside by 2cm)
- Referee makes initial call based on perception
- Delta calculated
- If delta exceeds threshold, VAR review initiated

**Types:**
- **Offside:** T-Shirt Line Offside (3D bounding box including shoulder)
- **Penalties:** Contact force threshold
- **Red Cards:** Violent conduct vs. reckless play distinction

#### Camera Parallax Error
- **Rare Event:** 1% chance of VAR miscalculation
  - Simulates the "human error in technology"
  - Camera angle distortion
  - Maintains realism (VAR isn't perfect)

---

### 5.3 Advantage Rule

**Referee calculates opportunity cost before whistling:**

```
Advantage_Value = Expected_Outcome(play_on) - Expected_Outcome(free_kick)
```

**Delay Logic:**
- Referee waits 2 ticks (200ms) after foul
- Monitors ball trajectory and player positioning
- If attacking opportunity deteriorates, blows whistle retroactively
- If advantage materializes, waves play on

---

### 5.4 Set Piece Physics

#### Penalty Spot Mind Games
**Psychological warfare:**

- **Intimidation_Radius:** Defenders crowding spot
- **Turf Kicking:** Damaging penalty spot
- **Effect:** Increases Taker's `Composure_Noise`
  - Reduces placement accuracy
  - Higher chance of hitting target zone but not corner
- **Referee Intervention:** If excessive, booking issued

#### Wall Mechanics

**Wall Creep:**
- **Behavior:** Defensive wall inches forward to reduce angle
- **Discipline_Check:** Each 10cm forward = +10% `Creep_Distance`
- **Referee Tolerance:** If `Creep_Distance > Ref_Tolerance`:
  - Booking issued OR
  - Free kick retaken from 1m closer

**Wall Jumping Logic:**
- **Timing:** Coordinated jump at moment of kick
- **GK Impact:** If wall jumps at wrong time:
  - `P_e` (Perception Error) for GK increases
  - GK loses sight of ball during wall ascent
  - Increases goal probability by 12-18%

---

### 5.5 Drop Ball & Restarts

#### Modern Drop Ball Rule
- **Implementation:** `Possession_Return_Logic`
  - If stopped inside box â†’ Drop to GK
  - Otherwise â†’ Drop to team that last touched
- **Old Rule (Legacy Mode):** Contested drop ball

#### Quick Free Kicks
- **Trigger:** `Awareness_Trigger` activated
  - Ball_Velocity is low (stationary)
  - Defenders are `Unsettled` (out of position)
- **Action:** Attacker initiates `Quick_Restart_Vector`
  - No whistle required
  - Bypasses defensive organization
  - High Decisions attribute players exploit more often

---

## 6. Tactical Micro-Physics

**50 granular physics interactions that separate simulation from arcade:**

### 6.1 Ball Physics

1. **Curved Passing Arcs:** Magnus effect from spin
2. **Bounce-Pass Physics:** Grass type affects bounce height consistency
3. **Knuckleball Mechanics:** Low-spin shots with unpredictable movement
4. **Slice Technique:** Defensive clearances with sidespin
5. **Driven vs. Lofted:** Velocity decay rates differ based on launch angle

### 6.2 Defensive Mechanics

6. **Tackle Follow-Through Momentum:** Over-committing creates recovery penalty
7. **Sliding Block Recovery Time:** Prone position requires 1.5 seconds to stand
8. **Standing Leg Stability:** Plant foot positioning affects tackle success
9. **Jockeying Footwork:** Side-stepping maintains defensive orientation
10. **Shepherding to Touchline:** Using pitch boundaries as "extra defender"

### 6.3 Offensive Movements

11. **Checking Away Then Toward:** Creating separation before receiving
12. **Diagonal Runs:** Attacking the space between CB and FB
13. **Delayed Runs:** Timing arrival to avoid offside trap
14. **False 9 Drop:** Creating space for wingers to exploit
15. **Overlapping Run Physics:** Wider player runs beyond ball carrier

### 6.4 Aerial Duels

16. **Box-Out Positioning:** Using body to shield opponent from ball
17. **Flick-On Trajectory:** Redirecting headers with minimal contact
18. **Defensive Headed Clearance:** Distance based on neck strength + timing
19. **Running Jump vs. Standing Jump:** Momentum provides height advantage
20. **Two-Footed Landing Stability:** Landing mechanics affect next action

### 6.5 Goalkeeper Physics

21. **Diving Extension Range:** Based on height + agility combination
22. **Narrowing the Angle:** Positional geometry to reduce shooting target
23. **One-on-One Rush Out:** Timing of charge affects striker composure
24. **Punch vs. Catch Decision:** Risk assessment on high crosses
25. **Parrying Direction Control:** Deflecting shots away from danger

### 6.6 Environmental Interactions

26. **Wind-Assisted Long Balls:** Trajectory calculation with wind vector
27. **Backspin Landing Control:** Ball "bites" and stops on landing
28. **Puddle Zones:** Water accumulation creates dead spots on pitch
29. **Shadow on Pitch:** Late afternoon sun creates visibility issues
30. **Corner Flag Shielding:** Using corner arc to protect ball

### 6.7 Set Piece Specializations

31. **In-Swinging vs. Out-Swinging Corners:** Attack trajectories
32. **Near-Post Flick-On:** Offensive header to redirect to far post
33. **Short Corner Variants:** Creating shooting angles
34. **Defensive Wall Organization Speed:** Time to form effective wall
35. **Penalty Shootout Psychology:** Cumulative pressure increases

### 6.8 Pressing & Counter-Pressing

36. **Pressing Trigger Zones:** Spatial activation of high press
37. **Counter-Press Window:** 5-second window after turnover
38. **Rest-Defense Transition:** Speed of retreat to defensive shape
39. **Ball-Oriented Press:** Chaotic swarming vs. structured pressing
40. **Lane-Blocking:** Cutting passing angles during press

### 6.9 Transition Phases

41. **Vertical Passing Speed:** Rapid forward passes during counter
42. **Breaking Lines:** Passes that bypass defensive layers
43. **Support Positioning:** Second wave runners in counter-attacks
44. **Transition Balance:** Number of players committed vs. covering
45. **Chaos Management:** Maintaining structure during end-to-end play

### 6.10 Physical Contests

46. **Shoulder Charge Legality:** Angle and force thresholds
47. **Holding Detection:** Sustained contact vs. incidental
48. **Shirt-Pulling:** Referee visibility affects call probability
49. **Obstruction:** Blocking opponent without playing ball
50. **Going to Ground:** Simulation detection algorithm

---

## Summary

This volume establishes the **physical reality** of the simulation:

- **Engine runs at 10Hz** for tactical decisions, 60Hz+ for visuals
- **Deterministic math** ensures consistency and prevents exploitation
- **Agent physicality** is deeply modeled (bio-mechanics, injuries, nutrition)
- **Match logic flows** through Perception â†’ Decision â†’ Execution pipeline
- **Environment matters** - pitch, weather, travel all affect performance
- **Officiating is simulated** - referees are agents with their own limitations
- **Micro-physics** create emergent tactical complexity

**Next:** Master Volume II covers the psychological and social layers built on top of this physical foundation.
