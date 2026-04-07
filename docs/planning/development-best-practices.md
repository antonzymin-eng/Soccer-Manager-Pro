# Development Best Practices: Tactical Director

**Created:** December 30, 2025, 11:45 AM PST  
**Version:** 1.0  
**Purpose:** Technical wisdom and anti-patterns extracted from project analysis  
**Source:** Distilled from CRITICAL_ANALYSIS_Development_Risks.md  
**Scope:** Applies across all development stages

---

## Table of Contents

1. [Performance Profiling Methodology](#1-performance-profiling-methodology)
2. [Testing Strategies](#2-testing-strategies)
3. [Technical Debt Prevention](#3-technical-debt-prevention)
4. [Anti-Patterns to Avoid](#4-anti-patterns-to-avoid)
5. [Quality Gate Framework](#5-quality-gate-framework)
6. [Optimization Guidelines](#6-optimization-guidelines)
7. [Architecture Validation](#7-architecture-validation)
8. [Specification Writing Standards](#8-specification-writing-standards)

---

## 1. PERFORMANCE PROFILING METHODOLOGY

### The "Profile Before Optimize" Rule

**Golden Rule:** Never optimize without measuring first.

**Workflow:**
```
1. Build feature (simple/naive implementation)
2. Profile with realistic data (not toy examples)
3. Identify actual bottlenecks (usually 10% of code = 90% of time)
4. Optimize ONLY measured bottlenecks
5. Re-profile to validate improvement
6. Iterate
```

**Anti-Pattern:**
```
âŒ "This looks slow, let me optimize it"
âœ… "Profiler shows this takes 40% CPU, let me optimize it"
```

---

### Profiling Tools & Techniques

**Unity Profiler Setup:**
- Enable Deep Profiling for initial analysis
- Disable for production (high overhead)
- Focus on CPU, Memory, Rendering tabs
- Use Timeline view to identify spikes

**Key Metrics to Track:**

**Frame Time Budget:**
- Simulation tick (10Hz): <5ms target
- Render frame (60Hz): <16ms target
- Total frame budget: <16.67ms (60 FPS)

**Memory Budget:**
- Stage 0-1: <500MB RAM
- Stage 2: <1GB RAM
- Stage 5: <2GB RAM

**Allocation Budget:**
- Zero allocations in GameLoop (GC pressure kills performance)
- <1MB allocations per frame in UI layer
- Profile with Memory Profiler to catch leaks

---

### Realistic Test Scenarios

**Don't profile with:**
- âŒ Empty scenes
- âŒ Single agent
- âŒ 1-minute test matches

**Do profile with:**
- âœ… Full 22 agents + ball
- âœ… 90-minute simulated match
- âœ… Worst-case scenarios (all agents near ball)
- âœ… Stage-appropriate data (Stage 2: 500 players, Stage 5: 85,000)

---

### Performance Regression Testing

**Automated Performance Tests:**

```csharp
[Test]
public void FullMatch_CompletesWithin_PerformanceBudget() {
    var stopwatch = Stopwatch.StartNew();
    
    // Simulate full 90-minute match
    MatchSimulator.Run(matchDuration: 90 * 60 * 10); // 90 min * 60 sec * 10 ticks
    
    stopwatch.Stop();
    
    // Should complete in <10 real-world seconds (54,000 ticks)
    Assert.Less(stopwatch.ElapsedMilliseconds, 10000);
}
```

**Run these in CI/CD:**
- Every commit to main branch
- Flag performance regressions >5%
- Block merge if >10% regression

---

## 2. TESTING STRATEGIES

### Test Pyramid for Football Sim

```
           /\
          /  \  E2E Tests (Few)
         /____\  - Full season simulation
        /      \  - Save/load integrity
       / Integr-\ Integration Tests (Some)
      /  ation   \ - Match simulation
     /____________\ - Tactical system
    /              \ Unit Tests (Many)
   /  Unit  Tests   \ - Physics formulas
  /__________________\ - Decision logic
```

**Distribution:**
- 70% Unit tests (fast, focused)
- 20% Integration tests (components working together)
- 10% E2E tests (full workflows)

---

### Unit Test Examples

**Physics Formula Validation:**

```csharp
[Test]
public void MagnusForce_ProducesRealisticCurve() {
    // Given: Ball with sidespin
    var ball = new Ball {
        Velocity = new Vector2(20, 0), // 20 m/s forward
        AngularVelocity = 10 // rad/s sidespin
    };
    
    // When: Calculate Magnus force
    var force = BallPhysics.CalculateMagnusForce(ball);
    
    // Then: Force perpendicular to velocity (causes curve)
    Assert.AreEqual(0, force.x, 0.01); // No force in velocity direction
    Assert.Greater(force.y, 0); // Positive lateral force
    
    // And: Magnitude matches expected formula
    // F = (0.5 * rho * v^2 * A * Cl)
    var expected = 0.5f * 1.225f * 400 * 0.0378f * 0.3f;
    Assert.AreEqual(expected, force.magnitude, 0.5);
}
```

**Decision Logic Validation:**

```csharp
[Test]
public void Agent_ChoosesPass_WhenTeammateOpen() {
    // Given: Agent with ball, open teammate
    var agent = CreateAgent(hasBall: true);
    var teammate = CreateAgent(position: new Vector2(10, 0));
    var opponent = CreateAgent(position: new Vector2(10, 5)); // 5m away
    
    // When: Agent makes decision
    var decision = DecisionSystem.Evaluate(agent);
    
    // Then: Should choose to pass
    Assert.AreEqual(DecisionType.Pass, decision.Type);
    Assert.AreEqual(teammate.ID, decision.TargetAgentID);
}
```

---

### Integration Test Examples

**Match Simulation Sanity:**

```csharp
[Test]
public void FullMatch_ProducesRealisticStatistics() {
    // Given: Two balanced teams
    var match = SetupMatch(homeRating: 75, awayRating: 75);
    
    // When: Simulate full match
    var result = MatchSimulator.Run(match);
    
    // Then: Stats within realistic ranges
    Assert.InRange(result.HomePossession, 40, 60); // Balanced possession
    Assert.InRange(result.TotalShots, 15, 35); // Realistic shot count
    Assert.InRange(result.TotalGoals, 0, 6); // Reasonable scoreline
    Assert.Greater(result.PassAccuracy, 65); // Minimum pass completion
}
```

**Tactical Impact Validation:**

```csharp
[Test]
public void HighPressing_IncreasesRecoveriesInOpponentHalf() {
    // Given: Same team with different tactics
    var baseMatch = SetupMatch(pressing: PressingIntensity.Low);
    var pressMatch = SetupMatch(pressing: PressingIntensity.High);
    
    // When: Simulate both
    var baseResult = MatchSimulator.Run(baseMatch);
    var pressResult = MatchSimulator.Run(pressMatch);
    
    // Then: High pressing produces more recoveries in attacking half
    var baseRecoveries = baseResult.RecoveriesInAttackingHalf;
    var pressRecoveries = pressResult.RecoveriesInAttackingHalf;
    
    Assert.Greater(pressRecoveries, baseRecoveries * 1.2); // At least 20% more
}
```

---

### E2E Test Examples

**Season Simulation Integrity:**

```csharp
[Test]
public void FullSeason_CompletesWithoutErrors() {
    // Given: New season setup
    var season = CreateSeason(teams: 20);
    
    // When: Simulate all 380 matches
    var results = SeasonSimulator.Run(season);
    
    // Then: All matches completed
    Assert.AreEqual(380, results.MatchesCompleted);
    
    // And: League table adds up
    Assert.AreEqual(380 * 3, results.TotalPointsAwarded); // 3 points per match
    
    // And: No data corruption
    Assert.IsTrue(results.ValidateIntegrity());
}
```

---

### Balance Testing Framework

**Statistical Validation:**

Run 1000+ simulated matches between equal-rated teams. Distribution should follow real-world patterns.

**Expected Distributions (Equal Teams):**

```
Home Win:  45-48%
Draw:      25-28%
Away Win:  22-27%

Goals per match: 2.5-3.0 average
Clean sheets: 35-40% of matches
```

**Automated Balance Tests:**

```csharp
[Test]
public void EqualTeams_ProduceRealisticDistribution() {
    var results = new Dictionary<string, int> {
        ["HomeWin"] = 0,
        ["Draw"] = 0,
        ["AwayWin"] = 0
    };
    
    // Simulate 1000 matches
    for (int i = 0; i < 1000; i++) {
        var match = SetupMatch(homeRating: 75, awayRating: 75);
        var result = MatchSimulator.Run(match);
        
        if (result.HomeScore > result.AwayScore) results["HomeWin"]++;
        else if (result.HomeScore < result.AwayScore) results["AwayWin"]++;
        else results["Draw"]++;
    }
    
    // Validate distribution
    Assert.InRange(results["HomeWin"] / 1000f, 0.45f, 0.48f);
    Assert.InRange(results["Draw"] / 1000f, 0.25f, 0.28f);
    Assert.InRange(results["AwayWin"] / 1000f, 0.22f, 0.27f);
}
```

---

## 3. TECHNICAL DEBT PREVENTION

### The "Rewrite in Year 3" Problem

**Symptom:** 
- Year 1: Ship prototype with poor architecture
- Year 2: Add features, codebase becomes unmaintainable
- Year 3: "We need to rewrite from scratch"
- Year 4-5: Rewrite while maintaining live game (nightmare)

**Prevention Strategy:**

**1. Architecture Review Gates:**
- After Stage 0: External code review (post on Reddit, forums)
- After Stage 1: Architecture review before Stage 2 commitment
- Every 6 months: Refactoring sprint (clean up debt)

**2. Code Quality Standards (Enforced):**
- No merge without unit tests for new code
- Code coverage >70% for core systems
- Static analysis tools (ReSharper, SonarQube)
- Automated style checking

**3. Refactoring Budget:**
- 20% of each stage allocated to refactoring
- "Clean Code" sprints between stages
- Never ship new features on dirty foundation

---

### Save File Migration Hell

**The Problem:**

```
Save Format V1 â†’ V2: Works fine
V2 â†’ V3: 5% of saves corrupt
V3 â†’ V4: New feature incompatible with old data
V4 â†’ V5: Migration takes 10 minutes
V5 â†’ V6: 20% of players lose progress
```

**Prevention:**

**Design for Forward Compatibility from Day 1:**

```csharp
[Serializable]
public class SaveData {
    public int SchemaVersion = 1; // Always track version
    
    // Use flexible data structures
    public Dictionary<string, object> ExtensionData; // For future additions
    
    // Never remove fields, only add and deprecate
    [Obsolete("Use NewFieldName instead")]
    public int OldField;
    
    public int NewFieldName; // Add new, keep old
}
```

**Migration Pipeline:**

```csharp
public class SaveMigrator {
    public SaveData Migrate(SaveData data) {
        while (data.SchemaVersion < CurrentVersion) {
            data = MigrateToNextVersion(data);
        }
        return data;
    }
    
    private SaveData MigrateToNextVersion(SaveData data) {
        switch (data.SchemaVersion) {
            case 1: return MigrateV1ToV2(data);
            case 2: return MigrateV2ToV3(data);
            // etc.
        }
    }
}
```

**Testing:**
- Keep old save files from every version
- Automated migration tests
- Never break old saves (offer conversion, but preserve original)

---

## 4. ANTI-PATTERNS TO AVOID

### 1. Premature Optimization

**Anti-Pattern:**
```csharp
âŒ "I'll use object pooling for everything from the start"
âŒ "Let me implement DoD before proving OOP is too slow"
âŒ "String hashing will make this faster (without profiling)"
```

**Correct Approach:**
```csharp
âœ… Build it simple first
âœ… Profile with realistic data
âœ… Optimize only proven bottlenecks
âœ… Re-profile to validate
```

**Quote:** "Premature optimization is the root of all evil" - Donald Knuth

---

### 2. Feature Creep

**Anti-Pattern:**
```
âŒ "While building ball physics, let me also add wind simulation"
âŒ "Stage 1 is done, but let me add one more formation..."
âŒ "I'll just quickly implement the youth academy while I'm here"
```

**Correct Approach:**
```
âœ… Finish current stage completely
âœ… Pass quality gate
âœ… THEN move to next stage
âœ… Track "future features" in backlog, don't build now
```

**Rule:** If it's not in the current stage plan, it goes in the parking lot.

---

### 3. Building Without Specification

**Anti-Pattern:**
```
âŒ "I'll figure out the formula while coding"
âŒ "Let's just start building and see what happens"
âŒ "Specification documents are a waste of time"
```

**Correct Approach:**
```
âœ… Write full specification first
âœ… Include formulas, edge cases, test scenarios
âœ… Get feedback on spec before coding
âœ… Code becomes implementation of spec
```

**Master Plan Requirement:** 20 weeks of specification before ANY Stage 0 coding.

---

### 4. Ignoring Cross-Platform Differences

**Anti-Pattern:**
```csharp
âŒ float calculation = Mathf.Sqrt(x); // Different results on ARM vs x86
âŒ DateTime.Now for game logic // Timezone issues
âŒ File paths with hardcoded separators
```

**Correct Approach:**
```csharp
âœ… Fixed64 math for determinism
âœ… In-game time system (not real-world clock)
âœ… Path.Combine() for cross-platform paths
âœ… Test on Windows, Mac, Linux regularly
```

---

### 5. The "Perfect Code" Trap

**Anti-Pattern:**
```
âŒ Spending 2 weeks on beautiful architecture for feature that takes 2 days
âŒ Refactoring working code "just because"
âŒ Over-engineering simple systems
```

**Correct Approach:**
```
âœ… "Good enough" for non-critical paths
âœ… Optimize architecture for hot paths only
âœ… Refactor when pain is felt, not preemptively
```

**Quote:** "Make it work, make it right, make it fast" - Kent Beck

---

### 6. Data Structure Rigidity

**Anti-Pattern:**
```csharp
âŒ Tightly coupled data structures
âŒ No versioning in serialized data
âŒ Hardcoded array sizes
âŒ No extension points
```

**Correct Approach:**
```csharp
âœ… Flexible data structures (Dictionary, List vs arrays)
âœ… Version all serialized data
âœ… Extension data fields for future additions
âœ… Interface-based dependencies
```

---

## 5. QUALITY GATE FRAMEWORK

### Stage Completion Criteria

**Every stage must pass ALL criteria before proceeding.**

**Technical Gates:**
1. âœ… Zero critical bugs
2. âœ… Performance within budget
3. âœ… Test coverage >70%
4. âœ… Code review passed
5. âœ… Documentation complete

**Functional Gates:**
1. âœ… All planned features working
2. âœ… User acceptance testing passed
3. âœ… Balance testing validated
4. âœ… Determinism validated (where applicable)

**Business Gates (Stage 2+):**
1. âœ… Sales/revenue targets met (if applicable)
2. âœ… Community feedback positive (>70%)
3. âœ… Roadmap for next stage funded

---

### Go/No-Go Decision Framework

**After each stage, explicit decision:**

**GO Decision (Proceed to next stage):**
- All quality gates passed
- Team morale high
- Funding secured for next stage
- Community excited

**ITERATE Decision (Stay in current stage):**
- Most gates passed, but critical issues remain
- Fix issues, re-test, try again

**PIVOT Decision (Change direction):**
- Fundamental design flaw discovered
- User feedback demands different approach
- Redesign, then retry

**NO-GO Decision (Stop project):**
- Quality gates repeatedly failed
- Funding exhausted
- Community uninterested
- (Rare, but honest assessment needed)

---

## 6. OPTIMIZATION GUIDELINES

### When to Optimize

**Optimize if:**
- âœ… Profiler shows >5% CPU in specific function
- âœ… Frame time exceeds budget
- âœ… User-visible lag/stuttering
- âœ… Memory usage exceeds budget

**Don't optimize if:**
- âŒ "It looks slow" (without measurement)
- âŒ Function uses <1% CPU
- âŒ No user-visible impact
- âŒ Would delay critical features

---

### Optimization Priority Order

**1. Algorithm Optimization (Biggest wins)**
- O(NÂ²) â†’ O(N log N) â†’ O(N)
- Example: Spatial hashing for collision detection

**2. Data Structure Optimization**
- Arrays instead of Lists for fixed-size data
- Structs instead of classes for small data
- Contiguous memory for cache efficiency

**3. Micro-Optimization (Last resort)**
- Inlining functions
- Bit manipulation
- SIMD instructions

**Rule:** Start at #1, only go to #3 if #1 and #2 exhausted.

---

### Optimization Techniques (Proven)

**Spatial Partitioning:**
```csharp
// Instead of checking all 22 agents against each other (231 checks)
// Use grid-based spatial hash (typically 40-60 checks)

var grid = new SpatialGrid(cellSize: 10f);
foreach (var agent in agents) {
    grid.Insert(agent);
}

foreach (var agent in agents) {
    var nearby = grid.GetNearby(agent); // Only 2-4 agents typically
    foreach (var other in nearby) {
        CheckCollision(agent, other);
    }
}
```

**Object Pooling (For rare allocations):**
```csharp
// Pool for temporary event objects
var eventPool = new ObjectPool<MatchEvent>(
    createFunc: () => new MatchEvent(),
    actionOnGet: (e) => e.Reset(),
    actionOnRelease: (e) => e.Clear(),
    actionOnDestroy: (e) => { },
    defaultCapacity: 100
);

// Get from pool instead of new
var matchEvent = eventPool.Get();
// Use it
// Return to pool
eventPool.Release(matchEvent);
```

**Attribute Caching:**
```csharp
private int cachedEffectivePassing;
private bool passingCacheDirty = true;

public int EffectivePassing {
    get {
        if (passingCacheDirty) {
            cachedEffectivePassing = CalculateEffectivePassing();
            passingCacheDirty = false;
        }
        return cachedEffectivePassing;
    }
}

// Invalidate on state change
public void OnFatigueChanged() {
    passingCacheDirty = true;
}
```

---

## 7. ARCHITECTURE VALIDATION

### Code Review Checklist

**Before merging any major system:**

**Architecture:**
- [ ] Follows SOLID principles
- [ ] Clear separation of concerns
- [ ] Minimal coupling between systems
- [ ] Testable design (dependency injection where needed)

**Performance:**
- [ ] No allocations in hot paths
- [ ] Appropriate data structures
- [ ] No obvious O(NÂ²) algorithms
- [ ] Profiled if touching GameLoop

**Maintainability:**
- [ ] Clear, descriptive names
- [ ] Functions <50 lines
- [ ] Classes <500 lines
- [ ] XML documentation on public APIs

**Testing:**
- [ ] Unit tests for logic
- [ ] Integration tests for workflows
- [ ] Edge cases covered

---

### External Review Strategy

**Stage 0 (After completion):**
- Post architecture overview on Reddit (r/gamedev)
- Ask for code review from experienced Unity developers
- Share physics demos for feedback

**Stage 1 (After completion):**
- Beta test with 20-50 hardcore FM players
- Collect feedback on tactical depth
- Iterate based on community input

**Ongoing:**
- Monthly dev blog with architecture insights
- Open discussion on design decisions
- Build community of technical reviewers

---

## 8. SPECIFICATION WRITING STANDARDS

### Specification Template

**Every specification document must include:**

**1. Header:**
- Document name
- Created date/time
- Version number
- Author
- Last updated

**2. Purpose & Scope:**
- What system does this describe?
- What is explicitly OUT of scope?
- Which stage implements this?

**3. Requirements:**
- Functional requirements (what it does)
- Performance requirements (how fast)
- Quality requirements (how accurate)

**4. Design:**
- Formulas (with derivations)
- Algorithms (pseudocode)
- Data structures
- Edge cases

**5. Testing:**
- Unit test scenarios
- Integration test scenarios
- Acceptance criteria

**6. Dependencies:**
- What other systems does this depend on?
- What systems depend on this?

**7. Future Extensions:**
- What features are deferred to later stages?
- How is this designed for extensibility?

---

### Formula Documentation Standard

**Every formula must include:**

**1. Plain English Description:**
```
Magnus Force: The sideways force on a spinning ball due to pressure differential
```

**2. Mathematical Formula:**
```
F_magnus = 0.5 Ã— Ï Ã— vÂ² Ã— A Ã— C_L
Where:
  Ï = air density (1.225 kg/mÂ³ at sea level)
  v = velocity magnitude (m/s)
  A = cross-sectional area (Ï€rÂ²)
  C_L = lift coefficient (depends on spin rate)
```

**3. Derivation (if non-obvious):**
```
From Bernoulli's principle:
  Higher velocity on one side â†’ Lower pressure
  Pressure differential â†’ Net force
  Direction perpendicular to velocity and spin axis
```

**4. Units:**
```
Input:  velocity (m/s), spin (rad/s)
Output: force (Newtons)
```

**5. Valid Ranges:**
```
Velocity: 0-35 m/s (typical range for football)
Spin: 0-50 rad/s (typical range)
```

**6. Test Cases:**
```
Test 1: Zero spin â†’ Zero Magnus force
Test 2: 20 m/s forward, 10 rad/s sidespin â†’ ~2-3N lateral force
Test 3: Real-world validation (compare to free kick footage)
```

---

### Specification Review Process

**Before implementation begins:**

1. **Self-Review:** Author reviews own spec (checklist)
2. **Peer Review:** Another developer reviews (if available)
3. **Community Review:** Post key specs for feedback
4. **Approval:** Mark as "Ready for Implementation"

**During implementation:**
- Spec is living document
- Update as design evolves
- Track changes (version control)

**After implementation:**
- Update spec to match "as-built"
- Add lessons learned section
- Archive or mark as "Implemented in Stage X"

---

## SUMMARY

### The Three Laws of Quality Development

**Law 1: Measure Before Optimize**
- Profile with realistic data
- Optimize only proven bottlenecks
- Re-profile to validate

**Law 2: Specify Before Build**
- Write full specification first
- Get feedback before coding
- Treat spec as contract

**Law 3: Test Everything**
- Unit tests for formulas
- Integration tests for workflows
- E2E tests for user journeys

---

### Red Flags to Watch For

**Development Red Flags:**
- ðŸš© "I'll just quickly add this feature..."
- ðŸš© "Let me optimize this before profiling..."
- ðŸš© "We don't need tests, it's simple code..."
- ðŸš© "I'll write the spec after I build it..."

**Process Red Flags:**
- ðŸš© Quality gates repeatedly failed
- ðŸš© Scope expanding beyond stage plan
- ðŸš© Performance degrading with each commit
- ðŸš© Test coverage dropping

**Team Red Flags (future):**
- ðŸš© Developers not following code standards
- ðŸš© Merge conflicts every commit
- ðŸš© "Works on my machine" syndrome

---

### Success Patterns

**What Works:**
- âœ… Small, focused commits
- âœ… Comprehensive specifications
- âœ… Test-driven development
- âœ… Regular profiling
- âœ… Code reviews
- âœ… Refactoring sprints
- âœ… Community feedback loops

**What Doesn't Work:**
- âŒ "Move fast and break things"
- âŒ Optimization without measurement
- âŒ Feature creep
- âŒ Skipping specifications
- âŒ Ignoring technical debt
- âŒ Building in isolation

---

**This document should be reviewed:**
- Before starting each stage
- When making major technical decisions
- When tempted to deviate from process
- Monthly as a team reminder

**Remember:** Quality over speed. This is a 10-year project. Build it right.

---

**Version Control:**
- Version: 1.0
- Next Review: After Stage 0 Month 6
- Owner: Lead Developer
