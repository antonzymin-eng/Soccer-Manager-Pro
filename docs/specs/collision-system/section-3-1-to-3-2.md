# Collision System Specification â€” Section 3: Core Systems

**Purpose:** Technical implementation of spatial partitioning, collision detection, and collision response for Specification #3 (Collision System).

**Created:** February 15, 2026, 4:15 PM PST  
**Revised:** March 05, 2026, audit session  
**Version:** 1.1  
**Status:** Draft — Revised  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Prerequisites:** Section 1 (Purpose & Scope) v1.1, Section 2 (System Overview) v1.2

**Changelog:**
- v1.1 (Mar 5, 2026): Updated prerequisite version references to match revised
  Section 1 (v1.1) and Section 2 (v1.2). No technical content changes. ERR-009
  (SpatialHash Query radius parameter) documented as known limitation pending
  resolution — see Spec Error Log.
- v1.0 (Feb 15, 2026): Initial draft.

---

## 3.1 Spatial Partitioning

The spatial partitioning subsystem divides the pitch into a uniform grid, enabling O(1) average-case queries for nearby entities. This is the **broad phase** of collision detectionâ€”it quickly identifies candidate pairs that might be colliding, eliminating the need to check all 253 possible entity pairs.

### 3.1.1 Grid Configuration

```csharp
/// <summary>
/// Spatial hash configuration constants.
/// Cell size chosen to guarantee that colliding entities occupy the same cell 
/// or adjacent cells, enabling 3Ã—3 neighborhood queries to find all collisions.
/// 
/// Derivation:
///   max_agent_hitbox = 0.50m (Strength 20, from Agent Movement Â§3.5.4.3)
///   max_combined_radius = 0.50m + 0.50m = 1.00m (two max-size agents touching)
///   cell_size >= max_combined_radius â†’ cell_size = 1.0m
///   
///   Pitch dimensions: 105m Ã— 68m (from Ball Physics Â§3.1.1)
///   Grid dimensions: ceil(105/1.0) + 1 = 106, ceil(68/1.0) + 1 = 69
///   Total cells: 106 Ã— 69 = 7,314
/// </summary>
public static class SpatialHashConstants
{
    // ================================================================
    // GRID DIMENSIONS
    // ================================================================
    
    /// <summary>
    /// Cell size in meters.
    /// Chosen as 2Ã— max hitbox radius to ensure colliding entities share
    /// the same cell or adjacent cells.
    /// </summary>
    public const float CELL_SIZE = 1.0f;
    
    /// <summary>
    /// Grid width in cells (X-axis, pitch length direction).
    /// 106 cells cover 0â€“105m with one cell of boundary margin.
    /// </summary>
    public const int GRID_WIDTH = 106;
    
    /// <summary>
    /// Grid height in cells (Y-axis, pitch width direction).
    /// 69 cells cover 0â€“68m with one cell of boundary margin.
    /// </summary>
    public const int GRID_HEIGHT = 69;
    
    /// <summary>
    /// Total number of cells in the grid.
    /// </summary>
    public const int TOTAL_CELLS = GRID_WIDTH * GRID_HEIGHT; // 7,314
    
    // ================================================================
    // BALL CONSTANTS (from Ball Physics Spec #1)
    // ================================================================
    
    /// <summary>
    /// Ball entity ID convention.
    /// Agents use IDs 0â€“21; ball uses -1.
    /// </summary>
    public const int BALL_ENTITY_ID = -1;
    
    /// <summary>
    /// Ball radius in meters (from BallPhysicsConstants.Ball.RADIUS).
    /// </summary>
    public const float BALL_RADIUS = 0.11f;
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    
    /// <summary>
    /// Maximum iterations for any loop in spatial hash operations.
    /// Prevents infinite loops from corrupted data structures.
    /// </summary>
    public const int MAX_ITERATIONS = 1000;
    
    /// <summary>
    /// Maximum collision pairs to process per frame.
    /// Sanity check for clustered scenarios.
    /// </summary>
    public const int MAX_COLLISION_PAIRS_PER_FRAME = 50;
    
    /// <summary>
    /// Maximum entities per cell before logging a warning.
    /// Normal gameplay rarely exceeds 4 entities per cell.
    /// </summary>
    public const int CELL_DENSITY_WARNING_THRESHOLD = 8;
}
```

### 3.1.2 SpatialHashGrid Data Structure

```csharp
/// <summary>
/// Spatial hash grid for collision broad phase.
/// 
/// Design decisions:
///   - Uniform grid (not quadtree) because N=23 entities is small
///   - 1D array of cells rather than 2D for cache efficiency
///   - Pooled List<int> per cell to avoid per-frame allocation
///   - Cell index formula: index = y * GRID_WIDTH + x
/// 
/// Memory layout:
///   - _cells: 7,314 List<int> references (58.5 KB for references)
///   - Each List<int>: pooled, cleared but not reallocated per frame
///   - Total: ~175 KB including list internals
/// 
/// Thread safety: NOT thread-safe. Single-threaded execution only.
/// </summary>
public class SpatialHashGrid
{
    // ================================================================
    // PRIVATE FIELDS
    // ================================================================
    
    /// <summary>
    /// Cell storage. Each cell contains a list of entity IDs occupying that cell.
    /// Index formula: _cells[y * GRID_WIDTH + x]
    /// </summary>
    private readonly List<int>[] _cells;
    
    /// <summary>
    /// Temporary buffer for query results.
    /// Reused across queries to avoid allocation.
    /// </summary>
    private readonly List<int> _queryResultBuffer;
    
    /// <summary>
    /// Tracks which cells have entities this frame.
    /// Used for efficient Clear() operation.
    /// </summary>
    private readonly List<int> _occupiedCellIndices;
    
    // ================================================================
    // CONSTRUCTOR
    // ================================================================
    
    /// <summary>
    /// Initializes the spatial hash grid with pre-allocated cell storage.
    /// Called once at simulation initialization, not per-frame.
    /// </summary>
    public SpatialHashGrid()
    {
        _cells = new List<int>[SpatialHashConstants.TOTAL_CELLS];
        
        // Pre-allocate all cell lists with small initial capacity
        // Most cells will remain empty; occupied cells rarely exceed 4 entities
        for (int i = 0; i < SpatialHashConstants.TOTAL_CELLS; i++)
        {
            _cells[i] = new List<int>(4);
        }
        
        _queryResultBuffer = new List<int>(32);
        _occupiedCellIndices = new List<int>(64);
    }
    
    // ================================================================
    // CORE OPERATIONS
    // ================================================================
    
    /// <summary>
    /// Clears all cells for a new frame.
    /// Only clears cells that were occupied last frame (sparse clear).
    /// 
    /// Complexity: O(K) where K = number of occupied cells (typically 20â€“40)
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _occupiedCellIndices.Count; i++)
        {
            _cells[_occupiedCellIndices[i]].Clear();
        }
        _occupiedCellIndices.Clear();
    }
    
    /// <summary>
    /// Inserts an entity into the spatial hash.
    /// 
    /// Most entities occupy a single cell. Entities near cell boundaries
    /// (within radius of edge) are inserted into multiple cells to ensure
    /// queries from either cell can find them.
    /// 
    /// Complexity: O(1) for most entities; O(4) for corner cases
    /// </summary>
    /// <param name="entityId">Entity identifier (0â€“21 for agents, -1 for ball)</param>
    /// <param name="position">Entity position in world coordinates (meters)</param>
    /// <param name="radius">Entity collision radius (meters)</param>
    public void Insert(int entityId, Vector3 position, float radius)
    {
        // Validate position (NaN/Infinity handled by caller, but double-check)
        if (float.IsNaN(position.x) || float.IsNaN(position.y))
        {
            return; // Skip invalid entity
        }
        
        // Calculate cell coordinates for entity center
        int centerCellX = PositionToCellX(position.x);
        int centerCellY = PositionToCellY(position.y);
        
        // Calculate position within cell (0 to CELL_SIZE)
        float cellLocalX = position.x - (centerCellX * SpatialHashConstants.CELL_SIZE);
        float cellLocalY = position.y - (centerCellY * SpatialHashConstants.CELL_SIZE);
        
        // Determine which cells the entity overlaps
        // Entity overlaps adjacent cell if distance to cell edge < radius
        bool overlapsLeft = cellLocalX < radius && centerCellX > 0;
        bool overlapsRight = (SpatialHashConstants.CELL_SIZE - cellLocalX) < radius 
                             && centerCellX < SpatialHashConstants.GRID_WIDTH - 1;
        bool overlapsBottom = cellLocalY < radius && centerCellY > 0;
        bool overlapsTop = (SpatialHashConstants.CELL_SIZE - cellLocalY) < radius 
                           && centerCellY < SpatialHashConstants.GRID_HEIGHT - 1;
        
        // Insert into center cell
        InsertIntoCell(entityId, centerCellX, centerCellY);
        
        // Insert into adjacent cells as needed
        if (overlapsLeft) InsertIntoCell(entityId, centerCellX - 1, centerCellY);
        if (overlapsRight) InsertIntoCell(entityId, centerCellX + 1, centerCellY);
        if (overlapsBottom) InsertIntoCell(entityId, centerCellX, centerCellY - 1);
        if (overlapsTop) InsertIntoCell(entityId, centerCellX, centerCellY + 1);
        
        // Corner overlaps (diagonal cells)
        if (overlapsLeft && overlapsBottom) InsertIntoCell(entityId, centerCellX - 1, centerCellY - 1);
        if (overlapsLeft && overlapsTop) InsertIntoCell(entityId, centerCellX - 1, centerCellY + 1);
        if (overlapsRight && overlapsBottom) InsertIntoCell(entityId, centerCellX + 1, centerCellY - 1);
        if (overlapsRight && overlapsTop) InsertIntoCell(entityId, centerCellX + 1, centerCellY + 1);
    }
    
    /// <summary>
    /// Queries the spatial hash for entities near a given position.
    /// 
    /// Returns all entity IDs in a dynamic neighbourhood of cells around the query position.
    /// The caller is responsible for filtering out self-collisions and duplicate pairs.
    /// 
    /// Complexity: O(E) where E = entities in queried cells (typically 2â€“8)
    /// </summary>
    /// <param name="position">Query position in world coordinates</param>
    /// <param name="radius">Query radius in metres; determines neighbourhood size</param>
    /// <returns>List of entity IDs in nearby cells (reused buffer, valid until next query)</returns>
    public List<int> Query(Vector3 position, float radius)
    {
        _queryResultBuffer.Clear();
        
        int centerCellX = PositionToCellX(position.x);
        int centerCellY = PositionToCellY(position.y);
        
        // Dynamic neighbourhood: ceil(radius / CELL_SIZE) cells in each direction.
        // For collision detection (radius ~0.50m, CELL_SIZE = 1.0m): cellRadius = 1 -> 3x3.
        // For pressure queries (radius = 3.0m): cellRadius = 3 -> 7x7.
        // [ERR-009 fix]: radius parameter was previously ignored; always returned 3x3.
        int cellRadius = Mathf.Max(1, Mathf.CeilToInt(radius / SpatialHashConstants.CELL_SIZE));
        
        for (int dy = -cellRadius; dy <= cellRadius; dy++)
        {
            int cellY = centerCellY + dy;
            if (cellY < 0 || cellY >= SpatialHashConstants.GRID_HEIGHT) continue;
            
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                int cellX = centerCellX + dx;
                if (cellX < 0 || cellX >= SpatialHashConstants.GRID_WIDTH) continue;
                
                int cellIndex = GetCellIndex(cellX, cellY);
                var cellContents = _cells[cellIndex];
                
                for (int i = 0; i < cellContents.Count; i++)
                {
                    _queryResultBuffer.Add(cellContents[i]);
                }
            }
        }
        
        return _queryResultBuffer;
    }
    // ================================================================
    // HELPER METHODS
    // ================================================================
    
    /// <summary>
    /// Converts world X coordinate to cell X index.
    /// Clamps to valid range [0, GRID_WIDTH-1].
    /// </summary>
    private int PositionToCellX(float worldX)
    {
        int cellX = (int)(worldX / SpatialHashConstants.CELL_SIZE);
        return Mathf.Clamp(cellX, 0, SpatialHashConstants.GRID_WIDTH - 1);
    }
    
    /// <summary>
    /// Converts world Y coordinate to cell Y index.
    /// Clamps to valid range [0, GRID_HEIGHT-1].
    /// </summary>
    private int PositionToCellY(float worldY)
    {
        int cellY = (int)(worldY / SpatialHashConstants.CELL_SIZE);
        return Mathf.Clamp(cellY, 0, SpatialHashConstants.GRID_HEIGHT - 1);
    }
    
    /// <summary>
    /// Converts cell coordinates to flat array index.
    /// Formula: index = y * GRID_WIDTH + x
    /// </summary>
    private int GetCellIndex(int cellX, int cellY)
    {
        return cellY * SpatialHashConstants.GRID_WIDTH + cellX;
    }
    
    /// <summary>
    /// Inserts entity into a specific cell and tracks the cell as occupied.
    /// </summary>
    private void InsertIntoCell(int entityId, int cellX, int cellY)
    {
        int cellIndex = GetCellIndex(cellX, cellY);
        var cell = _cells[cellIndex];
        
        // Track cell as occupied if this is its first entity this frame
        if (cell.Count == 0)
        {
            _occupiedCellIndices.Add(cellIndex);
        }
        
        cell.Add(entityId);
        
        // Density warning (development build only)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (cell.Count >= SpatialHashConstants.CELL_DENSITY_WARNING_THRESHOLD)
        {
            Debug.LogWarning($"[Collision] High cell density: cell ({cellX},{cellY}) has {cell.Count} entities");
        }
        #endif
    }
}
```

### 3.1.3 Edge Cases

| Edge Case | Detection | Handling |
|-----------|-----------|----------|
| **Entity outside pitch bounds** | Position.x < 0 or > 105, Position.y < 0 or > 68 | Clamp to boundary cells (0 or GRID_WIDTH-1 / GRID_HEIGHT-1) |
| **Entity exactly on cell boundary** | Position modulo CELL_SIZE = 0 | Insert into both adjacent cells |
| **Entity radius larger than cell size** | radius > CELL_SIZE (never occurs with 0.50m max) | Would require multi-cell insertion; not implemented for Stage 0 |
| **Ball near pitch corner** | Query spans cells outside grid bounds | Query clamps to valid cell range |
| **Empty grid query** | No entities near query position | Returns empty list; handled gracefully by caller |

### 3.1.4 Performance Characteristics

| Operation | Complexity | Typical Time | Notes |
|-----------|------------|--------------|-------|
| Clear() | O(K) | 0.02ms | K = occupied cells (~30â€“50) |
| Insert() single entity | O(1)â€“O(9) | 0.001ms | Most entities â†’ 1 cell; boundary â†’ up to 9 cells |
| Insert() all 23 entities | O(N) | 0.03ms | N = 23 |
| Query() | O(E) | 0.002ms | E = entities in 3Ã—3 neighborhood (~4â€“8) |

---

## 3.2 Collision Detection

Collision detection is the **narrow phase** that performs exact geometric intersection tests on candidate pairs identified by the spatial hash query.

### 3.2.1 Agent-Agent Collision Detection

Agent-agent collision uses circle-circle intersection in the XY plane (2D ground projection). Height (Z-axis) is ignored for Stage 0 ground-based collision.

```csharp
/// <summary>
/// Collision detection functions.
/// All methods are static and pure (no side effects, no state).
/// </summary>
public static class CollisionDetection
{
    // ================================================================
    // CONSTANTS
    // ================================================================
    
    /// <summary>
    /// Minimum distance threshold to avoid division by zero.
    /// If distance < this value, use fallback collision normal.
    /// </summary>
    private const float MIN_DISTANCE_EPSILON = 0.0001f;
    
    /// <summary>
    /// Agent reach height for ball collision filtering.
    /// Balls above this height require aerial challenge (Stage 1).
    /// 
    /// Derivation:
    ///   Average professional footballer height: 1.78m
    ///   Standing reach (arms extended): ~2.20m
    ///   Conservative ceiling for Stage 0: 2.0m
    /// </summary>
    public const float AGENT_REACH_HEIGHT = 2.0f;
    
    // ================================================================
    // AGENT-AGENT DETECTION
    // ================================================================
    
    /// <summary>
    /// Checks if two agents are colliding using circle-circle intersection.
    /// 
    /// Uses 2D (XY plane) projection â€” height (Z) ignored for ground-based collision.
    /// Returns collision manifold with contact point, normal, and penetration depth.
    /// 
    /// Performance: ~20 floating-point operations + 1 sqrt (only if collision detected)
    /// </summary>
    /// <param name="a1">First agent's physical properties</param>
    /// <param name="a2">Second agent's physical properties</param>
    /// <param name="manifold">Output collision manifold (valid only if method returns true)</param>
    /// <returns>True if collision detected, false otherwise</returns>
    public static bool CheckAgentAgentCollision(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        out CollisionManifold manifold)
    {
        // 2D distance calculation (XY plane only)
        float dx = a2.Position.x - a1.Position.x;
        float dy = a2.Position.y - a1.Position.y;
        float distanceSq = dx * dx + dy * dy;
        
        // Combined radius check
        float combinedRadius = a1.HitboxRadius + a2.HitboxRadius;
        float combinedRadiusSq = combinedRadius * combinedRadius;
        
        // Early exit if not colliding
        if (distanceSq >= combinedRadiusSq)
        {
            manifold = default;
            return false;
        }
        
        // Collision detected â€” compute manifold
        float distance = Mathf.Sqrt(distanceSq);
        float penetration = combinedRadius - distance;
        
        // Collision normal: points from a1 toward a2
        // Handle degenerate case where agents are at identical position
        Vector2 normal;
        if (distance > MIN_DISTANCE_EPSILON)
        {
            float invDistance = 1.0f / distance;
            normal = new Vector2(dx * invDistance, dy * invDistance);
        }
        else
        {
            // Fallback: arbitrary normal (agents exactly overlapping)
            normal = Vector2.right;
            penetration = combinedRadius;
        }
        
        // Contact point: on the line between centers, weighted by radii
        // Closer to the smaller agent (contact happens at boundary of smaller)
        float t = a1.HitboxRadius / combinedRadius;
        Vector2 contactPoint = new Vector2(
            a1.Position.x + dx * t,
            a1.Position.y + dy * t);
        
        manifold = new CollisionManifold
        {
            Normal = normal,
            ContactPoint = contactPoint,
            PenetrationDepth = penetration,
            Entity1ID = -1, // Set by caller
            Entity2ID = -1  // Set by caller
        };
        
        return true;
    }
    
    // ================================================================
    // AGENT-BALL DETECTION
    // ================================================================
    
    /// <summary>
    /// Checks if an agent is colliding with the ball.
    /// 
    /// Uses circle-sphere intersection in XY plane with height filter.
    /// Ball must be below AGENT_REACH_HEIGHT to be considered reachable.
    /// 
    /// Performance: ~15 floating-point operations + 1 sqrt (only if collision detected)
    /// </summary>
    /// <param name="agent">Agent's physical properties</param>
    /// <param name="ball">Ball state</param>
    /// <param name="contactPoint">Output contact point (valid only if method returns true)</param>
    /// <returns>True if collision detected, false otherwise</returns>
    public static bool CheckAgentBallCollision(
        in AgentPhysicalProperties agent,
        in BallState ball,
        out Vector3 contactPoint)
    {
        // Height filter: ball must be within agent's reach
        // Stage 0 simplification â€” all agents have same reach height
        if (ball.Position.z > AGENT_REACH_HEIGHT)
        {
            contactPoint = default;
            return false;
        }
        
        // 2D distance calculation (XY plane)
        float dx = ball.Position.x - agent.Position.x;
        float dy = ball.Position.y - agent.Position.y;
        float distanceSq = dx * dx + dy * dy;
        
        // Combined radius check
        float combinedRadius = agent.HitboxRadius + SpatialHashConstants.BALL_RADIUS;
        float combinedRadiusSq = combinedRadius * combinedRadius;
        
        if (distanceSq >= combinedRadiusSq)
        {
            contactPoint = default;
            return false;
        }
        
        // Collision detected â€” compute contact point
        float distance = Mathf.Sqrt(distanceSq);
        
        // Contact point: on agent's hitbox boundary toward ball
        if (distance > MIN_DISTANCE_EPSILON)
        {
            float invDistance = 1.0f / distance;
            float nx = dx * invDistance;
            float ny = dy * invDistance;
            
            contactPoint = new Vector3(
                agent.Position.x + nx * agent.HitboxRadius,
                agent.Position.y + ny * agent.HitboxRadius,
                ball.Position.z); // Z from ball position
        }
        else
        {
            // Degenerate case: ball at agent center
            contactPoint = new Vector3(
                agent.Position.x + agent.HitboxRadius,
                agent.Position.y,
                ball.Position.z);
        }
        
        return true;
    }
}
```

### 3.2.2 Collision Manifold Structure

```csharp
/// <summary>
/// Collision manifold describing the geometry of a collision.
/// 
/// Used for:
///   - Impulse direction (Normal)
///   - Force application point (ContactPoint)
///   - Penetration resolution (PenetrationDepth)
/// 
/// Memory: 28 bytes (2D vectors + float + 2 ints)
/// </summary>
public struct CollisionManifold
{
    /// <summary>
    /// Collision normal (unit vector) pointing from Entity1 toward Entity2.
    /// Used as impulse direction for collision response.
    /// </summary>
    public Vector2 Normal;
    
    /// <summary>
    /// Point of contact in world coordinates (XY plane).
    /// Used for rendering debug visualization and foul location reporting.
    /// </summary>
    public Vector2 ContactPoint;
    
    /// <summary>
    /// Penetration depth in meters.
    /// Distance the two entities overlap; used for position separation.
    /// Always positive for valid collisions.
    /// </summary>
    public float PenetrationDepth;
    
    /// <summary>
    /// ID of first entity in collision (lower ID by convention).
    /// </summary>
    public int Entity1ID;
    
    /// <summary>
    /// ID of second entity in collision (higher ID by convention).
    /// </summary>
    public int Entity2ID;
}
```

### 3.2.3 Collision Filtering

```csharp
/// <summary>
/// Collision filtering rules.
/// Determines which collision pairs should be processed.
/// </summary>
public static class CollisionFiltering
{
    /// <summary>
    /// Determines if a collision pair should be processed.
    /// 
    /// Filters:
    ///   - Self-collision (same entity ID)
    ///   - Already-processed pairs (using bitfield)
    ///   - Grounded agents initiating collision (they don't push, only obstruct)
    /// </summary>
    /// <param name="id1">First entity ID</param>
    /// <param name="id2">Second entity ID</param>
    /// <param name="a1">First agent properties (null if ball)</param>
    /// <param name="a2">Second agent properties (null if ball)</param>
    /// <param name="processedPairs">Bitfield of already-processed pairs</param>
    /// <returns>True if pair should be processed, false to skip</returns>
    public static bool ShouldProcessPair(
        int id1, 
        int id2,
        in AgentPhysicalProperties? a1,
        in AgentPhysicalProperties? a2,
        CollisionPairBitfield processedPairs)
    {
        // Skip self-collision
        if (id1 == id2) return false;
        
        // Skip already-processed pairs
        // Pair key uses canonical ordering (lower ID first)
        int lowId = Mathf.Min(id1, id2);
        int highId = Mathf.Max(id1, id2);
        if (processedPairs.IsSet(lowId, highId)) return false;
        
        // Note: Grounded agent filtering is handled in response phase,
        // not detection phase â€” we still detect collisions with grounded
        // agents, but they don't generate impulses.
        
        return true;
    }
    
    /// <summary>
    /// Determines if same-team collision rules apply.
    /// Same-team collisions have reduced momentum transfer.
    /// </summary>
    public static bool IsSameTeamCollision(int team1, int team2)
    {
        return team1 == team2 && team1 >= 0; // -1 indicates no team (ball)
    }
}
```

### 3.2.4 Collision Pair Bitfield

```csharp
/// <summary>
/// Bitfield for tracking processed collision pairs.
/// 
/// Avoids HashSet allocation by using a fixed-size bit array.
/// Maximum pairs = 23 * 22 / 2 = 253, fits in 4 ulongs (256 bits).
/// 
/// Pair encoding: pairIndex = lowId * 23 + (highId - lowId - 1)
/// This maps pairs (0,1), (0,2), ..., (21,22) to indices 0â€“252.
/// </summary>
public struct CollisionPairBitfield
{
    // 256 bits = 4 ulongs, covers all 253 possible pairs
    private ulong _bits0;
    private ulong _bits1;
    private ulong _bits2;
    private ulong _bits3;
    
    /// <summary>
    /// Clears all bits for a new frame.
    /// </summary>
    public void Clear()
    {
        _bits0 = 0;
        _bits1 = 0;
        _bits2 = 0;
        _bits3 = 0;
    }
    
    /// <summary>
    /// Checks if a pair has been processed.
    /// </summary>
    public bool IsSet(int lowId, int highId)
    {
        int pairIndex = GetPairIndex(lowId, highId);
        return GetBit(pairIndex);
    }
    
    /// <summary>
    /// Marks a pair as processed.
    /// </summary>
    public void Set(int lowId, int highId)
    {
        int pairIndex = GetPairIndex(lowId, highId);
        SetBit(pairIndex);
    }
    
    /// <summary>
    /// Computes unique index for a pair.
    /// Ball (ID -1) is mapped to index 22 for pair calculations.
    /// </summary>
    private static int GetPairIndex(int lowId, int highId)
    {
        // Map ball ID (-1) to 22 for indexing
        int mappedLow = (lowId == SpatialHashConstants.BALL_ENTITY_ID) ? 22 : lowId;
        int mappedHigh = (highId == SpatialHashConstants.BALL_ENTITY_ID) ? 22 : highId;
        
        // Ensure ordering
        if (mappedLow > mappedHigh)
        {
            int temp = mappedLow;
            mappedLow = mappedHigh;
            mappedHigh = temp;
        }
        
        // Triangular number formula for pair index
        // Row = mappedLow, column offset = mappedHigh - mappedLow - 1
        // Index = (23 * mappedLow - mappedLow * (mappedLow + 1) / 2) + (mappedHigh - mappedLow - 1)
        return mappedLow * 23 - (mappedLow * (mappedLow + 1)) / 2 + (mappedHigh - mappedLow - 1);
    }
    
    private bool GetBit(int index)
    {
        if (index < 64) return (_bits0 & (1UL << index)) != 0;
        if (index < 128) return (_bits1 & (1UL << (index - 64))) != 0;
        if (index < 192) return (_bits2 & (1UL << (index - 128))) != 0;
        return (_bits3 & (1UL << (index - 192))) != 0;
    }
    
    private void SetBit(int index)
    {
        if (index < 64) _bits0 |= (1UL << index);
        else if (index < 128) _bits1 |= (1UL << (index - 64));
        else if (index < 192) _bits2 |= (1UL << (index - 128));
        else _bits3 |= (1UL << (index - 192));
    }
}
```

---

