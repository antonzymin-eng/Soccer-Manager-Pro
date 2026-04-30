# Master Development Plan: "Tactical Director" (Working Title)
## The Football Manager Killer - Full Simulation Engine

**Created:** December 30, 2025, 10:15 AM PST  
**Version:** 1.0  
**Project Type:** Full-Scale Football Management Simulation  
**Development Approach:** Staged releases over 10+ years  
**Target:** Dethrone Football Manager as the definitive football sim  
**Physics Level:** Sim-Complex (Authentic, not arcade)  
**Lead Developer:** Solo developer with AI assistance  
**Quality Philosophy:** Quality over speed, no deadlines

---

## EXECUTIVE SUMMARY

### The Vision

**What Football Manager Does Well:**
- Comprehensive database (500,000+ players globally)
- Deep tactical systems
- Long-term career progression
- Passionate community

**Where Football Manager Falls Short:**
- Match engine feels disconnected from tactics
- Social/psychological systems are shallow
- "Hidden attributes" frustrate users
- Tactical impact is opaque (did my instruction actually work?)

**Our Differentiation:**
- **Observable Systems:** Every tactic has visible, measurable impact
- **Deep Physics:** Sim-quality ball physics and player movement
- **Psychological Depth:** Master Vol 2's social graph and H-Gate system
- **Transparent Mechanics:** Users understand WHY things happen

---

### The 10-Year Roadmap

```
Year 1   â†’ Stage 0: Physics Foundation
Year 2   â†’ Stage 1: Tactical Demo (Proof of Concept)
Year 3-4 â†’ Stage 2: V1 Release (Single Season Playable)
Year 5-6 â†’ Stage 3: V2 (Management Depth - Master Vol 3)
Year 7-8 â†’ Stage 4: V3 (Human Systems - Master Vol 2)
Year 9-10 â†’ Stage 5: V4 (Global Simulation - Master Vol 1 full scope)
Year 11+ â†’ Stage 6: V5 (Multiplayer & Beyond)
```

**Critical Path Philosophy:**
- Each stage is **commercially releasable**
- Each stage **funds the next**
- Early Access model from Stage 2 onward
- Community feedback drives refinement

---

## TABLE OF CONTENTS

1. [Development Stages Overview](#1-development-stages-overview)
2. [Stage 0: Physics Foundation (Year 1)](#2-stage-0-physics-foundation-year-1)
3. [Stage 1: Tactical Demo (Year 2)](#3-stage-1-tactical-demo-year-2)
4. [Stage 2: V1 Commercial Release (Year 3-4)](#4-stage-2-v1-commercial-release-year-3-4)
5. [Stage 3-6: Long-term Roadmap](#5-stage-3-6-long-term-roadmap)
6. [Technical Architecture Foundation](#6-technical-architecture-foundation)
7. [Specification Documents Required](#7-specification-documents-required)
8. [Risk Mitigation Strategy](#8-risk-mitigation-strategy)
9. [Success Metrics & Quality Gates](#9-success-metrics-quality-gates)
10. [Resource Requirements & Budget](#10-resource-requirements-budget)

---

## 1. DEVELOPMENT STAGES OVERVIEW

### Stage Dependency Chain

```
Stage 0 (Physics)
    â†“
Stage 1 (Tactics + Visualization)
    â†“
Stage 2 (Season Structure + Squad Management)
    â†“
Stage 3 (Financial Systems + Recruitment)
    â†“
Stage 4 (Social Systems + Psychology)
    â†“
Stage 5 (Global Simulation + Multi-League)
    â†“
Stage 6 (Multiplayer + Competitive)
```

**Critical Rule:** Cannot skip stages. Each builds on previous foundation.

---

### Quality Gates (Go/No-Go Decision Points)

**After Stage 0:**
- âœ… Physics feels authentic (passes curve realistically, players accelerate naturally)
- âœ… Performance acceptable (<5ms per simulation tick for 22 agents)
- âœ… Deterministic (same inputs produce same outputs)
- âŒ If any fail â†’ Redesign physics system

**After Stage 1:**
- âœ… Tactical changes produce observable, measurable differences
- âœ… Users can "read" the match (understand what's happening)
- âœ… Visual representation is professional-quality
- âŒ If any fail â†’ Refine tactical engine or visualization

**After Each Subsequent Stage:**
- âœ… New systems integrate without breaking existing features
- âœ… Performance remains acceptable
- âœ… User feedback is positive (>70% satisfaction)

---

## 2. STAGE 0: PHYSICS FOUNDATION (YEAR 1)

### Goal
Build the **physics and simulation core** that all future systems depend on. Prove that sim-complex physics work in Unity.

---

### Deliverables

#### 2.1 Ball Physics System

**Components:**
1. **Magnus Force (Spin-Induced Curve)**
   - Topspin, backspin, sidespin
   - Realistic curve trajectories
   - Spin decay over time/distance

2. **Aerodynamic Drag**
   - Velocity-dependent drag coefficient
   - Turbulent flow transition (knuckleball effect at certain speeds)
   - Air density (altitude affects long balls)

3. **Bounce Physics**
   - Surface-dependent coefficient of restitution
   - Grass type affects bounce height/spin retention
   - Irregular bounces on degraded pitch areas

4. **Rolling Friction**
   - Grass length affects ball speed
   - Wet pitch reduces friction
   - Ball slows realistically when not airborne

**Testing Criteria:**
- Curved free kick behaves like real footage
- Long ball trajectory matches professional game data
- Bounces feel natural, not arcade-y

---

#### 2.2 Agent Movement System

**Components:**
1. **Acceleration Curves**
   - Exponential acceleration to max speed (realistic: 2.5-3.5 seconds to peak)
   - Pace attribute affects both acceleration rate AND top speed
   - Fatigue reduces acceleration capacity

2. **Momentum & Inertia**
   - Cannot turn instantly at high speed
   - Agility attribute affects turning radius
   - Balance affects ability to change direction under pressure

3. **Deceleration**
   - Gradual slowdown when stopping
   - Controlled stop vs. emergency stop (different rates)
   - Stumble risk if turning too sharply at speed

4. **Collision Physics**
   - Physical body collision with momentum transfer
   - Strength attribute affects who wins physical duels
   - Foul detection based on force magnitude

**Testing Criteria:**
- Player sprint from standstill looks natural
- Agile player can turn sharper than physical player
- Collisions feel impactful but fair

---

#### 2.3 Core Simulation Loop (10Hz + 60Hz)

**From Master Vol 1:**
```
Tactical Heartbeat: 10Hz (every 100ms)
- Decision making
- Tactical instruction application
- AI processing

Physics Step: 60Hz (every 16.7ms)
- Position updates
- Ball physics
- Collision detection
- Visual interpolation
```

**Implementation:**
- Decouple logic from rendering
- Single-machine deterministic simulation via state snapshots; `float` arithmetic acceptable in Stage 0. (Fixed64 migration deferred to Stage 5 when cross-platform multiplayer becomes a requirement.)
- Replay system (save inputs + periodic state snapshots; reconstruct match exactly on the same machine)

---

#### 2.4 Basic Decision System (Minimal PDE)

**Simplified for Stage 0:**

**Perception:**
- Agent scans 360Â° for ball, teammates, opponents
- Occlusion detection (can't see through other players)
- Recognition latency based on Decisions attribute

**Decision (5 Options):**
1. Pass (if teammate available)
2. Dribble (if space ahead)
3. Shoot (if in range)
4. Clear (if under pressure in defensive third)
5. Hold (maintain position)

**Execution:**
- Skill check: Attribute vs. Difficulty
- Example: `Pass_Success = (Passing / 20) - (Distance / 50) - (Pressure Ã— 0.1)`

**Testing Criteria:**
- Agent makes sensible decisions 90%+ of time
- Poor decisions correlate with low attributes
- Pressure affects decision quality

---

### Month-by-Month Breakdown

#### **Month 1-2: Ball Physics**

**Week 1-2:** Specification writing
- Ball Physics Spec Document (20-30 pages)
- Formula derivation from sports physics papers
- Unit testing framework setup

**Week 3-4:** Core implementation
- Magnus force calculation
- Drag coefficient implementation
- Gravity + environmental factors

**Week 5-6:** Bounce system
- Coefficient of restitution calculations
- Surface interaction
- Spin retention on bounce

**Week 7-8:** Testing & refinement
- Build test scenarios (free kicks, long balls, crosses)
- Compare to real-world footage
- Tune parameters until realistic

**Deliverable:** `BallPhysics.cs` system with full unit test coverage

---

#### **Month 3-4: Agent Movement**

**Week 1-2:** Specification writing
- Agent Movement Spec Document
- Biomechanics research
- Attribute mapping

**Week 3-4:** Acceleration system
- Exponential acceleration curves
- Pace attribute integration
- Fatigue modifiers

**Week 5-6:** Momentum & turning
- Turning radius calculations
- Agility/Balance attribute effects
- Emergency stop mechanics

**Week 7-8:** Collision physics
- Agent-agent collision detection
- Momentum transfer
- Strength-based outcomes

**Deliverable:** `AgentMovement.cs` system with test suite

---

#### **Month 5-6: Collision Detection**

**Week 1-2:** Spatial partitioning
- Grid-based spatial hash (from Master Vol 4)
- Optimize for 22 agents + ball
- Benchmark: <1ms per frame

**Week 3-4:** Ball-agent interaction
- First touch mechanics
- Technique attribute affects control
- "Heavy touch" vs. "close control"

**Week 5-6:** Agent-agent collision
- Physical duel resolution
- Foul detection thresholds
- Referee perception system (basic)

**Week 7-8:** Integration testing
- Full match simulation (22 agents, ball physics)
- Performance profiling
- Bug fixing

**Deliverable:** `CollisionSystem.cs` with spatial optimization

---

#### **Month 7-8: Basic AI (PDE Minimal)**

**Week 1-2:** Perception layer
- Visual scanning (360Â° awareness)
- Occlusion calculation
- Recognition latency (L_rec from Master Vol 1)

**Week 3-4:** Decision tree
- 5 core decisions implementation
- Attribute-based skill checks
- Context awareness (position, score, time)

**Week 5-6:** Execution layer
- Action initiation
- Success/failure determination
- Animation triggers (simple)

**Week 7-8:** Testing
- Watch 100 simulated matches (console output)
- Decision quality analysis
- Attribute correlation validation

**Deliverable:** `DecisionSystem.cs` with decision logging

---

#### **Month 9-10: Integration & Testing**

**Week 1-2:** End-to-end integration
- Physics + Movement + Collision + AI
- Full 90-minute match simulation
- Data output (shots, passes, possession)

**Week 3-4:** Performance optimization
- Profiling (Unity Profiler)
- Bottleneck identification
- Optimization passes

**Week 5-6:** Determinism validation
- Replay system implementation
- Seed-based match reproduction
- Cross-platform testing (Windows/Mac/Linux)

**Week 7-8:** Documentation
- Code documentation (XML comments)
- System architecture diagrams
- Formula documentation wiki

**Deliverable:** Working match simulation (console output only, no graphics)

---

#### **Month 11-12: Quality Assurance & Refinement**

**Week 1-4:** Physics tuning
- Compare to real match footage
- Adjust parameters for realism
- Community feedback (show videos of physics)

**Week 5-8:** Edge case handling
- Rare collision scenarios
- Boundary conditions
- Crash prevention

**Final Deliverable:** 
- **Stage 0 Complete:** Physics engine that can simulate realistic matches
- **Performance:** <5ms per 10Hz tick, 60 FPS rendering
- **Deterministic:** Bit-perfect replay from seeds
- **Validated:** Feels authentic to football observers

---

### Success Criteria (Stage 0 Quality Gate)

**Must Achieve:**
1. âœ… Pass trajectory with curve looks realistic
2. âœ… Player acceleration from standstill feels natural
3. âœ… Collisions produce sensible outcomes
4. âœ… AI makes reasonable decisions 90%+ of time
5. âœ… Performance: 22 agents simulated in <5ms per tick
6. âœ… Deterministic (single-machine): Same seed = identical match on the same machine. (Cross-platform parity deferred to Stage 5; achieved via Fixed64.)
7. âœ… Community reaction: "This looks/feels real"

**If ANY criteria fail â†’ Do not proceed to Stage 1**

---

## 3. STAGE 1: TACTICAL DEMO (YEAR 2)

### Goal
Create a **playable demo** with 2D visualization that proves tactics matter. This becomes your crowdfunding/investment pitch.

---

### Deliverables

#### 3.1 2D Match Visualization

**Visual Style:**
- Top-down perspective
- Clean, professional art style (reference: Football Manager 2D mode, but better)
- Grass texture with pitch markings
- Agent representation: Team-colored circles with jersey numbers
- Ball: White sphere with trail effect

**Camera System:**
- Fixed top-down OR
- Follow-ball camera with smooth tracking
- Zoom controls (full pitch, half pitch, zoomed on action)

**Animation System:**
- Smooth interpolation between physics steps (10Hz â†’ 60 FPS visual)
- Agent rotation to face movement direction
- Ball trail for velocity visualization
- Celebration animations (simple)

---

#### 3.2 Tactical System Implementation

**From Master Vol 1 & 3:**

**Formations (10 Options):**
1. 4-4-2 (Flat)
2. 4-4-2 (Diamond)
3. 4-3-3 (Attack)
4. 4-3-3 (Holding)
5. 4-2-3-1 (Wide)
6. 4-2-3-1 (Narrow)
7. 3-5-2
8. 5-3-2
9. 3-4-3
10. 4-1-4-1

**Team Instructions (15 Core):**
- Tempo: High / Medium / Low
- Width: Wide / Narrow
- Pressing: High / Medium / Low
- Defensive Line: High / Medium / Low
- Passing Style: Direct / Mixed / Short
- Creative Freedom: High / Low
- Tackling: Get Stuck In / Stay On Feet
- Time Wasting: Yes / No
- Counter: Yes / No
- Offside Trap: Yes / No

**Individual Instructions (Per Position):**
- Fullback: Overlap / Stay Back / Sit Narrow / Get Forward
- Midfielder: Hold Position / Roam / Get Forward / Stay Back
- Winger: Hug Line / Cut Inside / Track Back / Stay High
- Striker: Run Channels / Hold Up / Get In Behind / Come Deep

**Implementation:**
- Instructions modify agent behavior parameters
- Observable impact on positioning and decision making
- Measurable in statistics (press maps, pass maps, etc.)

---

#### 3.3 Match Statistics & Analytics

**Basic Stats:**
- Possession %
- Shots (Total / On Target)
- Pass Completion %
- Tackles (Won / Total)
- Interceptions
- Fouls
- Corners / Free Kicks

**Advanced Stats (From Master Vol 1):**
- xG (Expected Goals) - location-based model
- PPDA (Passes Per Defensive Action)
- High Press Success Rate
- Territorial %
- Pressing Maps (heatmaps)
- Possession Maps (heatmaps)

**Visual Presentation:**
- Post-match summary screen
- Real-time stats panel during match
- Heatmap overlays
- Event timeline

---

#### 3.4 User Interface (Basic)

**Screens Required:**
1. **Main Menu**
   - New Demo Match
   - Options
   - Exit

2. **Tactics Setup**
   - Formation selection
   - Team instructions
   - Individual instructions per player
   - Save/load tactics

3. **Match View**
   - 2D pitch visualization
   - Stats panel (collapsible)
   - Speed controls (Pause, 1x, 3x, 5x, 10x)
   - Tactical adjustment buttons

4. **Post-Match Report**
   - Final score
   - Statistics comparison
   - Heatmaps
   - Event timeline

**UI Framework:** Unity UGUI (simpler than UI Toolkit for now)

---

### Month-by-Month Breakdown

#### **Month 1-2: 2D Rendering**

**Week 1-2:** Pitch rendering
- Grass texture
- Pitch markings (lines, center circle, penalty boxes)
- Lighting/shading for depth

**Week 3-4:** Agent rendering
- Circle sprites with team colors
- Jersey number display
- Player state indicators (has ball, pressing, etc.)

**Week 5-6:** Ball rendering
- Ball sprite
- Trail effect for movement
- Shadow for height perception

**Week 7-8:** Camera system
- Follow-ball tracking
- Smooth interpolation
- Zoom controls

**Deliverable:** Visual match representation (no UI, just watch simulated match)

---

#### **Month 3-4: Formation System**

**Week 1-2:** Formation data structures
- Define 10 formation templates
- Position coordinate systems (relative to pitch size)
- Formation morphing (attacking vs. defending shapes)

**Week 3-4:** Formation application
- Assign agents to positions
- Out-of-possession shape
- In-possession shape
- Transition logic

**Week 5-6:** Visual feedback
- Formation overlay on pitch (optional toggle)
- Agent target positions shown
- Out-of-position warnings

**Week 7-8:** Testing
- Verify formations look correct
- Compare to real-world equivalents
- Community feedback on accuracy

**Deliverable:** `FormationSystem.cs` with 10 working formations

---

#### **Month 5-6: Team Instructions**

**Week 1-2:** Instruction parsing system
- Define instruction data structures
- Instruction â†’ Behavior modification mapping
- Priority/conflict resolution

**Week 3-4:** Implement 15 core instructions
- Tempo affects pass frequency
- Width affects winger positioning
- Pressing affects defensive trigger points
- Etc. (all 15)

**Week 5-6:** Measurement systems
- Track instruction impact on stats
- A/B testing (same teams, different instructions)
- Verify observable differences

**Week 7-8:** Tuning
- Balance instruction effects
- Prevent dominant strategies
- Ensure all instructions viable

**Deliverable:** `TacticalInstructions.cs` with measurable impact

---

#### **Month 7-8: Individual Instructions**

**Week 1-4:** Per-position instruction implementation
- Fullback instructions (4 types)
- Midfielder instructions (4 types)
- Winger instructions (4 types)
- Striker instructions (4 types)

**Week 5-6:** Conflict detection
- Invalid instruction combinations
- Warning system
- Auto-correction suggestions

**Week 7-8:** Testing
- Verify individual instructions work
- Don't conflict with team instructions
- Visible on pitch

**Deliverable:** `IndividualInstructions.cs` integrated with formations

---

#### **Month 9-10: Statistics & Analytics**

**Week 1-2:** xG calculation
- Shot location model
- Angle calculation
- Defender proximity modifier
- GK position modifier

**Week 3-4:** Advanced stats
- PPDA calculation
- Pressing success rate
- Territorial control %
- Pass map generation

**Week 5-6:** Heatmap system
- Grid-based data collection
- Visual heatmap rendering
- Multiple heatmap types (possession, pressing, shots)

**Week 7-8:** UI integration
- Stats panel design
- Real-time stat updates
- Post-match report screen

**Deliverable:** `StatisticsEngine.cs` with comprehensive analytics

---

#### **Month 11-12: UI & Polish**

**Week 1-2:** Tactics setup UI
- Formation selector
- Instruction toggles
- Save/load tactics

**Week 3-4:** Match UI
- HUD design
- Speed controls
- Substitution interface (basic)

**Week 5-6:** Post-match UI
- Report layout
- Heatmap displays
- Statistics comparison

**Week 7-8:** Polish pass
- Visual effects (goal celebrations, etc.)
- Sound effects (crowd, whistle, ball kick)
- Bug fixing

**Final Deliverable:** **Tactical Demo** - Playable single match with full tactical control

---

### Success Criteria (Stage 1 Quality Gate)

**Must Achieve:**
1. âœ… Tactical changes produce 20%+ difference in key stats
2. âœ… Users can "read" the match (understand what's happening visually)
3. âœ… Professional visual quality (not placeholder art)
4. âœ… UI is intuitive (5-minute learning curve)
5. âœ… Performance: 60 FPS at all speed settings
6. âœ… Community reaction: "This is better than FM's match engine"

**Monetization Opportunity:** 
- Release demo on Steam (free)
- Kickstarter campaign: "Fund the full game"
- Target: $50K-100K to fund Stage 2

---

## 4. STAGE 2: V1 COMMERCIAL RELEASE (YEAR 3-4)

### Goal
First **paid commercial release**. Full single-season playable game. This becomes your revenue engine for future development.

---

### Deliverables

#### 4.1 Season Structure

**League System:**
- Single league (20 teams)
- 38-match season (home & away)
- League table with live updates
- Fixture calendar
- Match scheduling

**Competitions (Simple):**
- League only (no cups in V1)
- Promotions/Relegation tracked but not playable

---

#### 4.2 Squad Management

**Squad Size:** 25 players per team (11 starters, 7 bench, 7 reserves)

**Player Attributes (From Master Vol 1):**

**Physical (6):**
- Pace
- Acceleration  
- Strength
- Stamina
- Agility
- Balance

**Technical (8):**
- Passing
- First Touch
- Dribbling
- Shooting
- Heading
- Tackling
- Marking
- Positioning

**Mental (6):**
- Decisions
- Anticipation
- Composure
- Concentration
- Work Rate
- Teamwork

**Total: 20 attributes** (simplified from Master Vol 1's fuller set for V1)

**Player Management:**
- Squad selection (drag & drop)
- Training focus (simplified: Tactical / Physical / Rest)
- Fitness monitoring
- Injury management (basic)
- Form tracking

---

#### 4.3 Transfer System (Simplified)

**Summer Window Only (No January in V1):**

**Transfer Activities:**
- Search players (position, age, rating filters)
- Make bids
- Simple negotiation (accept/reject, no agent involvement)
- Budget based on league finish

**No in V1:**
- Loan deals
- Complex contract structures
- Release clauses
- Agent negotiations

**Aging/Development:**
- Players >30 decline (-1 overall per year)
- Players <24 improve (+1 overall per year)
- Retirement at 36

---

#### 4.4 Training System (Simplified)

**Weekly Training Choices (Pick One):**
1. **Tactical:** Improve team cohesion, tactical familiarity
2. **Physical:** Reduce injury risk, maintain fitness
3. **Attacking:** Slight boost to offensive attributes
4. **Defending:** Slight boost to defensive attributes

**Effects:**
- No granular attribute growth in V1
- Affects Form and Fitness only
- Medical staff warnings if overtraining

---

#### 4.5 Board Objectives & Progression

**Season Goals:**
- Board sets expectation based on budget (e.g., "Finish Top 6")
- Performance tracked throughout season
- Season-end evaluation

**Consequences:**
- Exceed: Bonus budget, job security
- Meet: Stable
- Fail: Warning, potential sacking (game over)

**Career Progression:**
- Multi-season play
- Reputation grows with success
- Better job offers (V2 feature, but tracked)

---

#### 4.6 Save System

**Save Features:**
- Unlimited save slots
- Autosave after each match
- Cloud save support (Steam Cloud)
- Save file versioning (for future updates)

**Technical:**
- JSON-based for V1 (simple, human-readable)
- Compressed (gzip)
- Save time target: <3 seconds

---

### Development Timeline

**Year 3:**

**Month 1-3:** Season structure
- League table system
- Fixture generation
- Match day flow
- Calendar UI

**Month 4-6:** Squad management
- Player database (20 teams Ã— 25 players = 500 total)
- Squad screen UI
- Player attribute display
- Training system

**Month 7-9:** Transfer system
- Player search/filter
- Transfer negotiation
- Budget management
- Squad building

**Month 10-12:** Integration & polish
- Save/load system
- Board objectives
- Career mode flow
- Bug fixing

---

**Year 4:**

**Month 1-3:** Content creation
- 500 player database (names, attributes, varied)
- 20 team database (names, kits, tactical styles)
- Balance testing (ensure teams competitive)

**Month 4-6:** Quality assurance
- Extensive playtesting (20+ full seasons)
- Balance adjustments
- Bug fixing
- Performance optimization

**Month 7-9:** Polish & UX
- Tutorial system (5-match guided tutorial)
- UI/UX refinement
- Accessibility features
- Controller support

**Month 10-12:** Launch preparation
- Marketing materials (trailer, screenshots)
- Steam page setup
- Press outreach
- Early Access launch

---

### Success Criteria (Stage 2 Quality Gate)

**Must Achieve:**
1. âœ… 20+ hours of gameplay (1-2 full seasons)
2. âœ… Zero game-breaking bugs
3. âœ… Performance: <3 second save/load times
4. âœ… User feedback: 75%+ positive (Steam reviews)
5. âœ… Sales: 5,000 units in first 6 months (minimum)

**Monetization:**
- Launch price: $24.99 (Early Access)
- Target: $125K revenue Year 1
- Funds Stage 3 development

---

## 5. STAGE 3-6: LONG-TERM ROADMAP

### Stage 3: V2 - Management Depth (Year 5-6)

**Goal:** Implement Master Vol 3 systems (Club Operations)

**Major Features:**
- Financial management (FFP, budgets, wages)
- Staff hiring (coaches, scouts, physios)
- Youth academy (bio-banding system from Master Vol 1)
- Contract negotiations (complex clauses)
- Board dynamics (different ownership types)
- Infrastructure upgrades (training ground, stadium)

**Revenue Target:** $250K (10K units at $24.99)

---

### Stage 4: V3 - Human Systems (Year 7-8)

**Goal:** Implement Master Vol 2 systems (Psychology & Sociology)

**Major Features:**
- Social graph (cliques, relationships)
- H-Gate happiness system (Confidence vs. Self-Efficacy)
- Media interactions (press conferences)
- Personality evolution (wealth impact, crisis resilience)
- Team chemistry effects on performance
- Mentor/protege systems

**Revenue Target:** $500K (20K units, higher price $29.99)

---

### Stage 5: V4 - Global Simulation (Year 9-10)

**Goal:** Implement Master Vol 1's full scope (85,000 entities)

**Major Features:**
- 5+ top leagues (England, Spain, Germany, Italy, France)
- European competitions (Champions League)
- International management
- Manager career mode (job offers, reputation)
- Background engine (statistical sim for other matches)
- Dual-engine system (active vs. background)

**Revenue Target:** $1M+ (50K units at $34.99)

**This is the "Football Manager Killer" release**

---

### Stage 6: V5 - Multiplayer (Year 11+)

**Goal:** Online competitive play

**Major Features:**
- Head-to-head matches online
- Competitive seasons/leagues
- Cross-platform play
- Deterministic netcode (built on the Stage 5 Fixed64 migration)
- Leaderboards, rankings
- Esports potential

**Revenue Target:** Subscription model ($5-10/month) or DLC model

---

## 6. TECHNICAL ARCHITECTURE FOUNDATION

### Core Systems (Built in Stage 0-1)

**Engine Core:**
```
Soccer-Manager-Pro/
â”œâ”€â”€ Core/
â”‚   â”œâ”€â”€ GameLoop.cs              # 10Hz tactical, 60Hz render
â”‚   â”œâ”€â”€ Fixed64.cs                # Deterministic math (added Stage 5)
â”‚   â”œâ”€â”€ SnapshotManager.cs        # Single-machine determinism (Stage 0)
â”‚   â”œâ”€â”€ EventBus.cs               # Pub/sub messaging
â”‚   â””â”€â”€ TimeManager.cs            # Tick management
â”‚
â”œâ”€â”€ Physics/
â”‚   â”œâ”€â”€ BallPhysics.cs            # Magnus, drag, bounce
â”‚   â”œâ”€â”€ AgentMovement.cs          # Acceleration, momentum
â”‚   â”œâ”€â”€ CollisionSystem.cs        # Spatial hash + detection
â”‚   â””â”€â”€ PhysicsConstants.cs       # Tuning parameters
â”‚
â”œâ”€â”€ Simulation/
â”‚   â”œâ”€â”€ PerceptionSystem.cs       # Agent sensing
â”‚   â”œâ”€â”€ DecisionSystem.cs         # PDE pipeline
â”‚   â”œâ”€â”€ ExecutionSystem.cs        # Action resolution
â”‚   â””â”€â”€ MatchSimulator.cs         # Orchestration
â”‚
â”œâ”€â”€ Tactics/
â”‚   â”œâ”€â”€ Formation.cs              # Position templates
â”‚   â”œâ”€â”€ TacticalInstructions.cs   # Team/individual instructions
â”‚   â””â”€â”€ TacticalEngine.cs         # Instruction application
â”‚
â””â”€â”€ Data/
    â”œâ”€â”€ Agent.cs                  # Player entity
    â”œâ”€â”€ Team.cs                   # Team entity
    â”œâ”€â”€ Match.cs                  # Match state
    â””â”€â”€ Attributes.cs             # Attribute definitions
```

---

### Data Architecture

**Entity-Component Pattern (Not full ECS, but inspired):**

```csharp
// Core entity
public class Agent {
    // Identity
    public int ID;
    public string Name;
    public int TeamID;
    
    // Physical state (hot data - accessed every frame)
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    
    // Attributes (warm data - accessed per decision)
    public AgentAttributes Attributes;
    
    // Tactical (warm data)
    public AgentRole Role;
    public TacticalInstructions Instructions;
    
    // Psychological (cold data - accessed rarely)
    public AgentPsychology Psychology; // Stage 4
    
    // Career (cold data)
    public AgentCareer Career; // Stage 3
}
```

**Database Schema (SQLite for V1):**

```sql
-- Players table
CREATE TABLE players (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    team_id INTEGER,
    position TEXT,
    age INTEGER,
    -- Attributes (20 columns)
    pace INTEGER,
    acceleration INTEGER,
    -- ... etc
    -- Career stats
    appearances INTEGER DEFAULT 0,
    goals INTEGER DEFAULT 0,
    assists INTEGER DEFAULT 0
);

-- Teams table
CREATE TABLE teams (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    reputation INTEGER,
    budget INTEGER
);

-- Matches table
CREATE TABLE matches (
    id INTEGER PRIMARY KEY,
    home_team_id INTEGER,
    away_team_id INTEGER,
    home_score INTEGER,
    away_score INTEGER,
    match_date TEXT,
    -- Serialized match data (JSON blob)
    match_data_json TEXT
);
```

---

### Performance Budgets

**Stage 0-1 Targets:**
- Simulation tick (10Hz): <5ms
- Render frame (60Hz): <16ms
- Total CPU: <30% on mid-range PC (i5-12400)
- Memory: <2GB RAM

**Stage 2+ Targets:**
- Season processing: <30 seconds
- Save/load: <3 seconds
- Database queries: <100ms

---

## 7. SPECIFICATION DOCUMENTS REQUIRED

### Stage 0 Specifications (20 Documents)

**Physics & Movement (8 docs):**
1. âœ… Ball Physics Specification (Magnus, drag, bounce)
2. âœ… Agent Movement Specification (acceleration, momentum)
3. âœ… Collision System Specification (spatial hash, detection)
4. âœ… First Touch Mechanics (ball-agent interaction)
5. âœ… Pass Mechanics (power, curve, accuracy formulas)
6. âœ… Shot Mechanics (power, placement, goal probability)
7. âœ… Heading Mechanics (aerial duels, flick-ons)
8. âœ… Goalkeeper Mechanics (diving, positioning)

**AI & Decision Making (6 docs):**
9. âœ… Perception System (vision, occlusion, recognition latency)
10. âœ… Decision Tree (5 core decisions, skill checks)
11. âœ… Positioning AI (maintaining formation shape)
12. âœ… Pressing AI (trigger zones, intensity)
13. âœ… Defensive AI (jockeying, marking)
14. âœ… Attacking AI (runs, movement)

**Systems & Architecture (6 docs):**
15. âœ… Fixed64 Math Library (implementation, operators)
16. âœ… Deterministic Simulation (seeding, replay)
17. âœ… Event System (event types, data structures)
18. âœ… Performance Optimization (profiling, bottlenecks)
19. âœ… Testing Strategy (unit, integration, balance tests)
20. âœ… Code Standards & Style Guide

---

### Stage 1 Specifications (10 docs)

21. âœ… Formation System (10 formations, morphing)
22. âœ… Team Instructions (15 instructions, effects)
23. âœ… Individual Instructions (per-position)
24. âœ… Statistics Engine (xG, PPDA, advanced stats)
25. âœ… Heatmap System (data collection, rendering)
26. âœ… 2D Rendering (visual style, camera)
27. âœ… Animation System (interpolation, celebrations)
28. âœ… UI/UX Design (wireframes, flow)
29. âœ… Sound Design (effects, music)
30. âœ… Tutorial System (5-match guided experience)

---

### Writing Schedule

**Weeks 1-4:** Physics specs (8 docs)  
**Weeks 5-8:** AI specs (6 docs)  
**Weeks 9-12:** Systems specs (6 docs)  
**Weeks 13-16:** Stage 1 specs (10 docs)  
**Weeks 17-20:** Review, refinement, approval

**Total: 5 months of specification writing before any coding**

---

## 8. RISK MITIGATION STRATEGY

### Technical Risks

**Risk:** Physics too complex, performance suffers  
**Mitigation:** 
- Profile early and often
- Simplify if necessary (arcade-complex vs. sim-complex)
- Fallback: Level-of-detail physics (full for user match, simplified for others)

**Risk:** Determinism breaks (cross-platform differences)  
**Mitigation:**
- Stage 0–4: single-machine determinism only (state snapshots for replay/save; `float` simulation acceptable since runs are not compared across machines)
- Stage 5: introduce Fixed64 math (Spec #9) and re-verify approved physics specs against the Fixed64 backend
- Stage 6 (multiplayer): cross-platform bit-exact parity becomes a hard requirement; Fixed64 + extensive cross-platform testing + automated determinism tests in CI/CD

**Risk:** AI makes poor decisions, breaks immersion  
**Mitigation:**
- Extensive playtesting
- Decision logging for debugging
- Difficulty settings (AI handicapping for balance)

---

### Scope Risks

**Risk:** Feature creep, never ship  
**Mitigation:**
- Strict stage gates
- "No" to features not in current stage plan
- Parking lot for future stages

**Risk:** Stages take longer than planned  
**Mitigation:**
- Buffer time in each stage (20% contingency)
- Can cut Stage 2 features to hit quality gate
- Early Access allows iterative release

---

### Business Risks

**Risk:** Insufficient funding  
**Mitigation:**
- Stage 1 demo for Kickstarter
- Early Access revenue funds next stage
- Freelance/contract work during development gaps

**Risk:** Market competition (FM releases new version)  
**Mitigation:**
- Differentiation on observable tactics, physics depth
- Community building early (Discord, dev blog)
- Niche focus initially (hardcore tacticians)

**Risk:** Burn-out (10-year project)  
**Mitigation:**
- Sustainable pace (40 hours/week, not crunch)
- Each stage is releasable (sense of accomplishment)
- Community engagement for motivation

---

## 9. SUCCESS METRICS & QUALITY GATES

### Stage 0 Metrics

**Physics Quality:**
- âœ… Pass curve matches real footage (visual comparison)
- âœ… Player acceleration feels natural (playtester feedback)
- âœ… Collision outcomes sensible (no bizarre bounces)

**Performance:**
- âœ… <5ms per simulation tick (Unity Profiler)
- âœ… 60 FPS rendering (no stutters)

**Determinism (Stage 0):**
- âœ… 1000 matches with same seed = identical outcomes **on the same machine** (single-machine replay/save via state snapshots)
- ⏳ Cross-platform: Windows, Mac, Linux produce same results — **deferred to Stage 5** (requires Fixed64 migration; not a Stage 0 quality gate)

---

### Stage 1 Metrics

**Tactical Impact:**
- âœ… A/B test: Different tactics produce 20%+ stat variance
- âœ… Heatmaps show instruction effects (press high â†’ turnovers in opponent half)
- âœ… User feedback: "I can SEE my tactics working"

**Visual Quality:**
- âœ… Professional art (Steam wishlist conversion >10%)
- âœ… No placeholder assets in demo

**Usability:**
- âœ… New user can play tutorial in <30 minutes
- âœ… 80% of testers understand tactical instructions

---

### Stage 2 Metrics

**Engagement:**
- âœ… Average session: 60+ minutes
- âœ… Completion rate: 40% finish 1 season

**Technical:**
- âœ… Zero crash bugs in QA
- âœ… Save/load <3 seconds

**Commercial:**
- âœ… 5,000 units sold in 6 months
- âœ… 75% positive Steam reviews

---

## 10. RESOURCE REQUIREMENTS & BUDGET

### Solo Development (Your Current Situation)

**Stage 0 (Year 1):**
- **Time:** 12 months, 40 hours/week = 1,920 hours
- **Costs:**
  - Unity Pro: $2,400/year
  - Assets (minimal, mostly code): $500
  - **Total: ~$3K**

**Stage 1 (Year 2):**
- **Time:** 12 months, 40 hours/week = 1,920 hours
- **Costs:**
  - Unity Pro: $2,400
  - Art assets (UI kits, pitch textures): $2,000
  - Sound effects/music: $1,000
  - **Total: ~$5.5K**

**Stage 2 (Year 3-4):**
- **Time:** 24 months, 40 hours/week = 3,840 hours
- **Costs:**
  - Unity Pro: $4,800
  - Marketing (Steam page, trailer): $3,000
  - QA/Testing contractors: $5,000
  - **Total: ~$13K**

**Cumulative to V1 Release:** ~$22K over 4 years

**Funding Strategy:**
- Personal savings: $10K
- Kickstarter (Stage 1 demo): $50K target
- Early Access revenue: Fund future stages

---

### Team Expansion (Stage 3+)

**Year 5+:** Hire help with Early Access revenue

**Roles Needed:**
- UI/UX Designer (contract): $15K/year
- QA Tester (part-time): $10K/year
- Community Manager (part-time): $8K/year

**Total team cost:** $33K/year (affordable if Stage 2 hits sales targets)

---

## NEXT STEPS

### Immediate Actions

**Week 1-20: Specification Phase**

The next 5 months are dedicated to writing 30 comprehensive specification documents that will guide all development. No coding begins until these are complete and approved.

**Specification Priority Order:**

**Priority 1 - Physics Foundation (Weeks 1-4):**
1. Ball Physics Specification
2. Agent Movement Specification
3. Collision System Specification
4. Pass Mechanics Specification

**Priority 2 - Core Gameplay (Weeks 5-8):**
5. Shot Mechanics Specification
6. Perception System Specification
7. Decision Tree Specification
8. Fixed64 Math Library Specification

**Priority 3 - Advanced Physics (Weeks 9-12):**
9. Heading Mechanics Specification
10. Goalkeeper Mechanics Specification
11. First Touch Mechanics Specification
12. Positioning AI Specification

**Priority 4 - Tactical AI (Weeks 13-16):**
13. Pressing AI Specification
14. Defensive AI Specification
15. Attacking AI Specification
16. Deterministic Simulation Specification

**Priority 5 - Systems Architecture (Weeks 17-20):**
17. Event System Specification
18. Performance Optimization Strategy
19. Testing Strategy & Framework
20. Code Standards & Style Guide

**Stage 1 Specifications (Written During Stage 0 Implementation):**
21. Formation System Specification
22. Team Instructions Specification
23. Individual Instructions Specification
24. Statistics Engine Specification
25. Heatmap System Specification
26. 2D Rendering Specification
27. Animation System Specification
28. UI/UX Design Document
29. Sound Design Specification
30. Tutorial System Specification

---

### Development Philosophy

**Quality Gates Are Sacred:**
- No stage begins until previous stage passes ALL quality criteria
- No exceptions for timeline pressure
- Better to delay than ship broken foundation

**Community Involvement:**
- Share physics videos early (get feedback on realism)
- Public dev blog (build audience before launch)
- Discord community from Stage 0 (hardcore tacticians give best feedback)

**Sustainable Pace:**
- 40 hours/week maximum
- No crunch (this is 10+ year journey)
- Breaks between stages (prevent burnout)

**Documentation First:**
- Every system fully specified before coding
- Formulas documented in wiki
- Architecture diagrams maintained

---

## CONCLUSION

This Master Development Plan represents a **10-year commitment** to building the definitive football management simulation. 

**Key Success Factors:**
1. **Discipline:** Resist feature creep, follow the stage plan
2. **Quality:** Pass quality gates or don't proceed
3. **Community:** Build audience early, iterate with feedback
4. **Funding:** Each stage must fund the next
5. **Endurance:** Sustainable pace for the long haul

**The Vision:**
By Year 10, "Tactical Director" (working title) will have:
- The most realistic match physics in any football game
- Observable, transparent tactical systems that actually work
- Deep psychological and social modeling
- Global simulation with 85,000+ entities
- Multiplayer competitive play

**This will be the Football Manager Killer.**

But it starts with writing one specification document at a time.

**Ready to begin?**

---

**Document Version Control:**
- Version: 1.0
- Created: December 30, 2025, 10:15 AM PST
- Next Review: After Stage 0 completion
- Approval Required: Lead Developer (You)

**Status: AWAITING APPROVAL**

Once approved, work begins on:
**Specification Document #1: Ball Physics Specification**
