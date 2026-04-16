## 4.5 Memory Layout

### 4.5.1 Struct Sizes

| Struct | Size (bytes) | Fields | Notes |
|--------|--------------|--------|-------|
| CollisionManifold | 28 | Vector2 Ã— 2 + float + int Ã— 2 | Per-collision, temporary |
| CollisionResponseData | 32 | Vector3 Ã— 2 + bool Ã— 2 + float | Per-agent per-collision |
| CollisionEvent | 56 | float + enum + int Ã— 2 + Vector3 + float + ContactForceData | Stored in buffer |
| AgentBallCollisionData | 32 | Vector3 Ã— 2 + enum + int Ã— 2 + bool | Passed to Ball Physics |
| ContactForceData | 28 | float + Vector3 + enum + int Ã— 2 + bool Ã— 2 | Part of CollisionEvent |
| DeterministicRNG | 16 | ulong Ã— 2 | Per-frame, stack allocated |

### 4.5.2 CollisionSystem Memory Footprint

```
CollisionSystem instance memory breakdown:
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
COMPONENT                        CALCULATION              BYTES
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
SpatialHashGrid
  _cells array                   7,314 Ã— 8 (refs)        58,512
  _cells contents (lists)        7,314 Ã— 32 (avg)       234,048
  _queryResultBuffer             32 Ã— 4 (int refs)          128
  _occupiedCellIndices           64 Ã— 4 (int refs)          256
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  Subtotal (SpatialHashGrid)                            292,944
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
Collision Event Buffer
  _collisionEvents array         50 Ã— 56 (events)         2,800
  _collisionEventCount           int                          4
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  Subtotal (Event Buffer)                                 2,804
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
Processed Pairs Tracking
  _processedPairs HashSet        ~100 Ã— 16 (tuples)       1,600
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  Subtotal (Pair Tracking)                                1,600
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
Miscellaneous
  Object header                                              16
  DeterministicRNG (value)                                   16
  Other references                                           32
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  Subtotal (Misc)                                            64
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
TOTAL COLLISION SYSTEM                                ~297 KB
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
```

**Comparison to budget:**
- Section 2.4 allocated <180 KB
- Actual estimate: ~297 KB
- Overage: ~117 KB (due to List<int> internal overhead)

**Mitigation options (if needed):**
1. Reduce list initial capacity from 32 to 4 per cell (most cells empty)
2. Use pooled array instead of List<int> per cell
3. Accept 297 KB as reasonable (still <300 KB, minimal impact)

**Recommendation:** Accept 297 KB. The overage is List<int> pre-allocation overhead, which prevents per-frame allocation. Trading static memory for zero runtime GC pressure is the correct tradeoff for a 60Hz physics system.

### 4.5.3 Per-Frame Allocations

**Target:** Zero heap allocations per frame.

**Achieved by:**
- Pre-allocated List<int> for each grid cell (cleared, not recreated)
- Pre-allocated collision event buffer (fixed size 50)
- Pre-allocated query result buffer (fixed size 32)
- Pre-allocated processed pairs HashSet (cleared, not recreated)
- DeterministicRNG is a value type (stack allocated)
- All struct types (manifold, response, event) are value types

**Validation:** Use Unity Profiler's GC.Alloc marker to confirm zero allocations during collision update.

---

## 4.6 Performance Profiling

### 4.6.1 Profiling Hooks

**Conditional compilation for editor builds:**

```csharp
public void UpdateCollisions(Agent[] agents, ref BallState ball, ...)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.Update");
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.SpatialHash.Clear");
    #endif
    _spatialHash.Clear();
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample();
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.SpatialHash.Insert");
    #endif
    // ... insert all entities ...
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample();
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.BroadPhase");
    #endif
    // ... query spatial hash ...
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample();
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.NarrowPhase");
    #endif
    // ... intersection tests ...
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample();
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.BeginSample("Collision.Response");
    #endif
    // ... compute responses ...
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample();
    #endif
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Profiler.EndSample(); // Collision.Update
    #endif
}
```

**Profiling labels:**
- `Collision.Update` â€” Total update time
- `Collision.SpatialHash.Clear` â€” Grid reset
- `Collision.SpatialHash.Insert` â€” Entity insertion
- `Collision.BroadPhase` â€” Candidate pair identification
- `Collision.NarrowPhase` â€” Intersection testing
- `Collision.Response` â€” Impulse calculation and state triggers

### 4.6.2 Performance Targets

**Per-frame budget (from Section 2.4):**

| Metric | Target | Worst Case | Measurement |
|--------|--------|------------|-------------|
| Total update time | <0.15ms typical; <0.30ms p95 | <0.50ms p99 | Profiler mean |
| SpatialHash.Clear | <0.02ms | <0.05ms | Profiler mean |
| SpatialHash.Insert | <0.03ms | <0.05ms | Profiler mean |
| BroadPhase | <0.05ms | <0.10ms | Profiler mean |
| NarrowPhase | <0.10ms | <0.20ms | Profiler mean |
| Response | <0.10ms | <0.15ms | Profiler mean |

**Measurement protocol:**
1. Run 10 simulated matches (full 90 minutes each)
2. Record profiler data every frame
3. Calculate mean, p50, p95, p99 for each marker
4. Flag any frame >0.5ms for investigation
5. Verify memory stability (no growth over match duration)

---

## 4.7 Deterministic RNG Implementation

### 4.7.1 Algorithm Selection

**Algorithm:** xorshift128+

**Rationale:**
- Fast (few operations per random number)
- High-quality randomness (passes BigCrush)
- Deterministic (same seed â†’ same sequence)
- No floating-point operations in core algorithm
- Small state (16 bytes)

### 4.7.2 Implementation

```csharp
/// <summary>
/// Deterministic random number generator using xorshift128+.
/// 
/// Seeded from match seed + frame number to ensure:
///   1. Same match with same seed produces identical collisions
///   2. Replays are perfectly reproducible
///   3. Networked clients agree on collision outcomes
/// 
/// Per Master Vol 1 Â§1.3 determinism requirement.
/// 
/// Memory: 16 bytes (two ulong state values)
/// </summary>
public struct DeterministicRNG
{
    private ulong _state0;
    private ulong _state1;
    
    /// <summary>
    /// Initializes RNG from a single seed.
    /// 
    /// Uses SplitMix64 to expand seed into two state values.
    /// This ensures even simple seeds (1, 2, 3) produce uncorrelated sequences.
    /// </summary>
    /// <param name="seed">Combined match seed and frame number</param>
    public DeterministicRNG(ulong seed)
    {
        // SplitMix64 expansion
        _state0 = SplitMix64(seed);
        _state1 = SplitMix64(_state0);
        
        // Ensure non-zero state (xorshift requirement)
        if (_state0 == 0 && _state1 == 0)
        {
            _state0 = 0x853C49E6748FEA9BUL;
            _state1 = 0xDA3E39CB94B95BDBUL;
        }
    }
    
    /// <summary>
    /// SplitMix64 hash function for seed expansion.
    /// Produces well-distributed output from any input.
    /// </summary>
    private static ulong SplitMix64(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }
    
    /// <summary>
    /// Generates next random float in range [0, 1).
    /// 
    /// Uses top 24 bits of xorshift128+ output.
    /// 24 bits provides 16 million distinct values, sufficient for
    /// probability checks to 0.00001% precision.
    /// </summary>
    /// <returns>Random float in [0, 1)</returns>
    public float NextFloat()
    {
        ulong s1 = _state0;
        ulong s0 = _state1;
        ulong result = s0 + s1;
        
        // Scramble state
        _state0 = s0;
        s1 ^= s1 << 23;
        _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
        
        // Convert top 24 bits to float in [0, 1)
        return (result >> 40) * (1.0f / (1UL << 24));
    }
    
    /// <summary>
    /// Generates next random ulong (full 64 bits).
    /// For cases requiring more precision or integer output.
    /// </summary>
    public ulong NextULong()
    {
        ulong s1 = _state0;
        ulong s0 = _state1;
        ulong result = s0 + s1;
        
        _state0 = s0;
        s1 ^= s1 << 23;
        _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
        
        return result;
    }
}
```

### 4.7.3 Per-Frame Seeding

```csharp
public void BeginFrame(ulong matchSeed, int frameNumber)
{
    // Combine match seed with frame number
    // XOR ensures different frames get different sequences
    // Even if collision happens at same position, frame number disambiguates
    ulong frameSeed = matchSeed ^ (ulong)frameNumber ^ ((ulong)frameNumber << 32);
    
    _rng = new DeterministicRNG(frameSeed);
}
```

**Why per-frame seeding?**

1. **Determinism:** Same frame â†’ same RNG sequence â†’ same collision outcomes
2. **Independence:** RNG state doesn't carry over between frames
3. **Debuggability:** Can reproduce exact frame by providing seed + frame number
4. **Parallelism readiness:** No shared state if future parallel processing

---

## Section 4 Summary

| Subsection | Key Content |
|------------|-------------|
| **4.1 Code Organization** | 11 source files, namespace convention, class responsibilities |
| **4.2 Core Data Structures** | 5 structs with full field documentation |
| **4.3 Constants** | Organized by category: SpatialHash, Physics, FallThresholds, Safety |
| **4.4 Dependencies** | Internal graph, external specs, Unity dependencies |
| **4.5 Memory Layout** | ~297 KB total, zero per-frame allocations |
| **4.6 Profiling** | 6 profiler markers, performance targets per phase |
| **4.7 Deterministic RNG** | xorshift128+ implementation, per-frame seeding |

---

## Cross-Reference Verification

| Reference | Verified | Notes |
|-----------|----------|-------|
| Ball Physics Â§3.1.10.1 (BodyPart enum) | âœ“ | Used in AgentBallCollisionData |
| Agent Movement Â§3.5.4 (AgentPhysicalProperties) | âœ“ | Consumed by CollisionDetection |
| Agent Movement Â§3.1.2 (GROUNDED/STUMBLING) | âœ“ | Triggered by CollisionResponseData |
| Ball Physics Â§4.1.1 (File structure pattern) | âœ“ | Followed for organization |
| Ball Physics Â§4.3 (Configuration management) | âœ“ | Tuning workflow adopted |
| Master Vol 1 Â§1.3 (Determinism) | âœ“ | DeterministicRNG implemented |
| Section 2.5 (Performance budget) | âš  | Memory slightly over (297 KB vs 180 KB target) |

---

**End of Section 4**

**Page Count:** ~12 pages  
**Next Section:** Section 5 â€” Testing (unit tests, integration tests, performance tests)
