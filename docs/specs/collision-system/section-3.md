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

## 3.3 Collision Response

Collision response computes the physical effects of detected collisions: velocity changes (impulses), position corrections (separation), and state triggers (stumble/fall).

### 3.3.1 Response Constants

```csharp
/// <summary>
/// Constants for collision response calculations.
/// 
/// All physics constants include derivation rationale.
/// Values marked "empirically tuned" require gameplay validation.
/// </summary>
public static class CollisionResponseConstants
{
    // ================================================================
    // IMPULSE PHYSICS
    // ================================================================
    
    /// <summary>
    /// Coefficient of restitution for agent-agent collisions.
    /// 
    /// Value: 0.3 (highly inelastic â€” players are soft bodies)
    /// 
    /// Derivation:
    ///   e = 1.0 â†’ perfectly elastic (billiard balls)
    ///   e = 0.0 â†’ perfectly inelastic (clay)
    ///   Human bodies: e â‰ˆ 0.2â€“0.4 depending on padding/tension
    ///   Football players (muscular, braced): e â‰ˆ 0.3
    /// 
    /// Reference: Empirically tuned, no direct academic source.
    /// </summary>
    public const float COEFFICIENT_OF_RESTITUTION = 0.3f;
    
    /// <summary>
    /// Momentum scale for same-team collisions.
    /// 
    /// Value: 0.3 (teammates don't knock each other hard)
    /// 
    /// Rationale: Real players on the same team exhibit spatial
    /// awareness and naturally soften contact with teammates.
    /// Reduced momentum simulates this implicit coordination.
    /// </summary>
    public const float SAME_TEAM_MOMENTUM_SCALE = 0.3f;
    
    // ================================================================
    // FALL/STUMBLE THRESHOLDS
    // ================================================================
    
    /// <summary>
    /// Base force threshold for falling (Newtons).
    /// Agent with Strength 1 falls at forces above 550 N.
    /// 
    /// Derivation:
    ///   F_threshold = FALL_FORCE_BASE + (Strength Ã— FALL_FORCE_PER_STRENGTH)
    ///   Strength 1:  500 + (1 Ã— 50) = 550 N
    ///   Strength 10: 500 + (10 Ã— 50) = 1000 N
    ///   Strength 20: 500 + (20 Ã— 50) = 1500 N
    /// 
    /// Reference: Empirically tuned based on desired gameplay feel.
    /// Target: Strength 10 agent falls from full-sprint collision (~8 m/s).
    /// </summary>
    public const float FALL_FORCE_BASE = 500f;
    
    /// <summary>
    /// Additional force threshold per Strength point (Newtons per point).
    /// </summary>
    public const float FALL_FORCE_PER_STRENGTH = 50f;
    
    /// <summary>
    /// Stumble threshold as fraction of fall threshold.
    /// Agent stumbles when force is between 50%â€“100% of fall threshold.
    /// </summary>
    public const float STUMBLE_THRESHOLD_FRACTION = 0.5f;
    
    /// <summary>
    /// Force range over which fall probability transitions 0â†’1.
    /// Once force exceeds fall threshold, probability increases
    /// linearly until force exceeds threshold + this range.
    /// </summary>
    public const float FALL_PROBABILITY_RANGE = 500f;
    
    // ================================================================
    // GROUNDED DURATION
    // ================================================================
    
    /// <summary>
    /// Minimum time on ground after collision-induced fall (seconds).
    /// Modified by agent attributes â€” see CalculateGroundedDuration().
    /// </summary>
    public const float GROUNDED_DURATION_MIN = 0.5f;
    
    /// <summary>
    /// Maximum time on ground after collision-induced fall (seconds).
    /// </summary>
    public const float GROUNDED_DURATION_MAX = 2.0f;
    
    /// <summary>
    /// Base duration on ground for collision falls (seconds).
    /// Modified by Agility attribute.
    /// </summary>
    public const float GROUNDED_DURATION_BASE = 1.2f;
    
    /// <summary>
    /// Duration reduction per Agility point (seconds per point).
    /// Agility 20 agent: 1.2 - (20 Ã— 0.03) = 0.6s
    /// Agility 1 agent:  1.2 - (1 Ã— 0.03) = 1.17s
    /// </summary>
    public const float GROUNDED_DURATION_PER_AGILITY = 0.03f;
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    
    /// <summary>
    /// Maximum impulse magnitude (kgÂ·m/s).
    /// Prevents physics explosions from extreme inputs.
    /// 
    /// Derivation:
    ///   max_velocity = 10.2 m/s (sprint speed, Agent Movement Â§3.2)
    ///   max_mass = 100 kg (Strength 20, Agent Movement Â§3.5.4.2)
    ///   max_relative_velocity = 2 Ã— 10.2 = 20.4 m/s (head-on collision)
    ///   max_impulse â‰ˆ m Ã— Î”v = 100 Ã— 20.4 Ã— (1 + e) / 2 â‰ˆ 1300 kgÂ·m/s
    ///   
    ///   Safety ceiling: 2000 kgÂ·m/s (50% margin)
    /// </summary>
    public const float MAX_IMPULSE_MAGNITUDE = 2000f;
    
    /// <summary>
    /// Maximum penetration depth before flagging as tunneling (meters).
    /// If exceeded, use gentle separation to avoid physics explosion.
    /// </summary>
    public const float MAX_PENETRATION_DEPTH = 0.5f;
}
```

### 3.3.2 Impulse Calculation

```csharp
/// <summary>
/// Collision response calculations.
/// Implements impulse-based collision resolution with momentum conservation.
/// </summary>
public static class CollisionResponse
{
    /// <summary>
    /// Calculates collision response for two agents.
    /// 
    /// Physics model:
    ///   Conservation of momentum with coefficient of restitution.
    ///   Impulse applied along collision normal.
    ///   Same-team collisions receive reduced impulse.
    ///   Grounded agents do not receive impulses (obstacle only).
    /// 
    /// Formula derivation (Appendix A):
    ///   v_rel = dot(v1 - v2, normal)      // Relative velocity along normal
    ///   j = -(1 + e) Ã— v_rel / (1/m1 + 1/m2)  // Impulse magnitude
    ///   Î”v1 = (j / m1) Ã— normal            // Velocity change for agent 1
    ///   Î”v2 = -(j / m2) Ã— normal           // Velocity change for agent 2
    /// </summary>
    /// <param name="a1">First agent (modified if not grounded)</param>
    /// <param name="a2">Second agent (modified if not grounded)</param>
    /// <param name="manifold">Collision manifold from detection phase</param>
    /// <param name="isSameTeam">True if agents are on same team</param>
    /// <param name="rng">Deterministic RNG for fall/stumble probability</param>
    /// <returns>Response data including impulses and state triggers</returns>
    public static AgentAgentCollisionResult CalculateAgentAgentResponse(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        in CollisionManifold manifold,
        bool isSameTeam,
        ref DeterministicRNG rng)
    {
        var result = new AgentAgentCollisionResult();
        
        // ============================================================
        // STEP 1: Handle grounded agents
        // ============================================================
        
        // Grounded agents are obstacles but don't generate impulses
        bool a1Active = !a1.IsGrounded;
        bool a2Active = !a2.IsGrounded;
        
        if (!a1Active && !a2Active)
        {
            // Both grounded â€” no response needed
            return result;
        }
        
        // ============================================================
        // STEP 2: Calculate relative velocity along collision normal
        // ============================================================
        
        // 2D velocity projection (ignore Z component)
        Vector2 v1_2d = new Vector2(a1.Velocity.x, a1.Velocity.y);
        Vector2 v2_2d = new Vector2(a2.Velocity.x, a2.Velocity.y);
        
        // Relative velocity of a1 with respect to a2
        Vector2 relativeVelocity = v1_2d - v2_2d;
        
        // Component along collision normal
        float vRel = Vector2.Dot(relativeVelocity, manifold.Normal);
        
        // If agents are separating, no impulse needed
        if (vRel > 0)
        {
            // Still need to resolve penetration
            CalculateSeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);
            return result;
        }
        
        // ============================================================
        // STEP 3: Calculate impulse magnitude
        // ============================================================
        
        float e = CollisionResponseConstants.COEFFICIENT_OF_RESTITUTION;
        
        // Inverse masses (grounded agent has infinite mass â†’ inv_mass = 0)
        float invMass1 = a1Active ? (1.0f / a1.Mass) : 0f;
        float invMass2 = a2Active ? (1.0f / a2.Mass) : 0f;
        float invMassSum = invMass1 + invMass2;
        
        // Guard against division by zero (shouldn't happen with at least one active)
        if (invMassSum < 0.0001f)
        {
            return result;
        }
        
        // Impulse magnitude
        float j = -(1f + e) * vRel / invMassSum;
        
        // Apply same-team reduction
        if (isSameTeam)
        {
            j *= CollisionResponseConstants.SAME_TEAM_MOMENTUM_SCALE;
        }
        
        // Clamp to safety limit
        j = Mathf.Clamp(j, -CollisionResponseConstants.MAX_IMPULSE_MAGNITUDE, 
                           CollisionResponseConstants.MAX_IMPULSE_MAGNITUDE);
        
        // ============================================================
        // STEP 4: Calculate velocity impulses
        // ============================================================
        
        Vector2 impulse = j * manifold.Normal;
        
        if (a1Active)
        {
            result.VelocityImpulse1 = new Vector3(
                impulse.x * invMass1,
                impulse.y * invMass1,
                0f); // No Z component for ground collision
        }
        
        if (a2Active)
        {
            result.VelocityImpulse2 = new Vector3(
                -impulse.x * invMass2,
                -impulse.y * invMass2,
                0f);
        }
        
        // ============================================================
        // STEP 5: Calculate penetration separation
        // ============================================================
        
        CalculateSeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);
        
        // ============================================================
        // STEP 6: Calculate impact force and state triggers
        // ============================================================
        
        // Impact force (used for fall/stumble determination)
        // F = j / dt where dt = 1/60 s
        float impactForce = Mathf.Abs(j) * 60f; // Convert impulse to force
        
        result.ImpactForce = impactForce;
        
        // Determine fall/stumble for each active agent
        // 
        // STAGE 0 SIMPLIFICATION: Agility attribute is not exposed in AgentPhysicalProperties.
        // For grounded duration calculation, we use Strength as a proxy for Agility.
        // Rationale: Stronger players tend to have slower recovery (heavier build),
        // while weaker/lighter players recover faster. This approximation is acceptable
        // for Stage 0. In Stage 1+, AgentPhysicalProperties should be extended to include
        // Agility, or the collision system should access the full Agent class.
        //
        // Impact: Grounded duration will be slightly different than if using true Agility.
        // A Strength 20 player uses duration calculation as if Agility = 20, giving 0.6s.
        // If their true Agility is 10, the correct duration would be 0.9s.
        // This 0.3s difference is acceptable for Stage 0 prototype.
        
        if (a1Active)
        {
            DetermineFallOrStumble(a1.Strength, impactForce, isSameTeam, ref rng,
                out result.TriggerGrounded1, out result.TriggerStumble1,
                out result.GroundedDuration1, a1.Strength); // Using Strength as Agility proxy (Stage 0)
        }
        
        if (a2Active)
        {
            DetermineFallOrStumble(a2.Strength, impactForce, isSameTeam, ref rng,
                out result.TriggerGrounded2, out result.TriggerStumble2,
                out result.GroundedDuration2, a2.Strength); // Using Strength as Agility proxy (Stage 0)
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculates position separation to resolve penetration.
    /// Distributes separation inversely proportional to mass.
    /// </summary>
    private static void CalculateSeparation(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        in CollisionManifold manifold,
        bool a1Active,
        bool a2Active,
        ref AgentAgentCollisionResult result)
    {
        if (manifold.PenetrationDepth <= 0) return;
        
        // Check for tunneling (excessive penetration)
        bool isTunneling = manifold.PenetrationDepth > CollisionResponseConstants.MAX_PENETRATION_DEPTH;
        
        // Separation amount (slightly more than penetration to prevent re-collision)
        float separation = manifold.PenetrationDepth * 1.01f;
        
        if (isTunneling)
        {
            // Gentle separation for tunneling â€” don't over-correct
            separation = CollisionResponseConstants.MAX_PENETRATION_DEPTH;
        }
        
        // Distribute separation based on inverse mass
        float invMass1 = a1Active ? (1.0f / a1.Mass) : 0f;
        float invMass2 = a2Active ? (1.0f / a2.Mass) : 0f;
        float invMassSum = invMass1 + invMass2;
        
        if (invMassSum < 0.0001f) return;
        
        Vector2 mtv = manifold.Normal * separation;
        
        if (a1Active)
        {
            float ratio1 = invMass1 / invMassSum;
            result.PositionCorrection1 = new Vector3(-mtv.x * ratio1, -mtv.y * ratio1, 0f);
        }
        
        if (a2Active)
        {
            float ratio2 = invMass2 / invMassSum;
            result.PositionCorrection2 = new Vector3(mtv.x * ratio2, mtv.y * ratio2, 0f);
        }
    }
    
    /// <summary>
    /// Determines if collision force causes fall or stumble.
    /// Uses deterministic RNG for probability rolls.
    /// </summary>
    private static void DetermineFallOrStumble(
        int strength,
        float impactForce,
        bool isSameTeam,
        ref DeterministicRNG rng,
        out bool triggerGrounded,
        out bool triggerStumble,
        out float groundedDuration,
        int agility)
    {
        triggerGrounded = false;
        triggerStumble = false;
        groundedDuration = 0f;
        
        // Same-team collisions cannot trigger GROUNDED (design decision Â§1.6.4)
        // but can trigger STUMBLE
        
        // Calculate thresholds
        float fallThreshold = CollisionResponseConstants.FALL_FORCE_BASE + 
                              (strength * CollisionResponseConstants.FALL_FORCE_PER_STRENGTH);
        float stumbleThreshold = fallThreshold * CollisionResponseConstants.STUMBLE_THRESHOLD_FRACTION;
        
        // Check fall condition (not for same-team)
        if (!isSameTeam && impactForce > fallThreshold)
        {
            // Calculate fall probability
            float excessForce = impactForce - fallThreshold;
            float fallProbability = Mathf.Clamp01(
                excessForce / CollisionResponseConstants.FALL_PROBABILITY_RANGE);
            
            float roll = rng.NextFloat();
            if (roll < fallProbability)
            {
                triggerGrounded = true;
                groundedDuration = CalculateGroundedDuration(agility);
                return; // Fall takes precedence over stumble
            }
        }
        
        // Check stumble condition
        if (impactForce > stumbleThreshold && impactForce <= fallThreshold)
        {
            float stumbleProbability = (impactForce - stumbleThreshold) / 
                                       (fallThreshold - stumbleThreshold);
            
            float roll = rng.NextFloat();
            if (roll < stumbleProbability)
            {
                triggerStumble = true;
            }
        }
    }
    
    /// <summary>
    /// Calculates how long an agent stays on the ground after falling.
    /// Based on Agility attribute.
    /// 
    /// Formula:
    ///   duration = GROUNDED_DURATION_BASE - (Agility Ã— GROUNDED_DURATION_PER_AGILITY)
    ///   Clamped to [GROUNDED_DURATION_MIN, GROUNDED_DURATION_MAX]
    /// 
    /// Examples:
    ///   Agility 1:  1.2 - 0.03 = 1.17s
    ///   Agility 10: 1.2 - 0.30 = 0.90s
    ///   Agility 20: 1.2 - 0.60 = 0.60s
    /// </summary>
    private static float CalculateGroundedDuration(int agility)
    {
        float duration = CollisionResponseConstants.GROUNDED_DURATION_BASE - 
                         (agility * CollisionResponseConstants.GROUNDED_DURATION_PER_AGILITY);
        
        return Mathf.Clamp(duration,
            CollisionResponseConstants.GROUNDED_DURATION_MIN,
            CollisionResponseConstants.GROUNDED_DURATION_MAX);
    }
}
```

### 3.3.3 Agent-Agent Collision Result Structure

```csharp
/// <summary>
/// Result of agent-agent collision response calculation.
/// Contains velocity impulses, position corrections, and state triggers for both agents.
/// </summary>
public struct AgentAgentCollisionResult
{
    // ================================================================
    // AGENT 1 RESPONSE
    // ================================================================
    
    /// <summary>Velocity change for agent 1 (m/s).</summary>
    public Vector3 VelocityImpulse1;
    
    /// <summary>Position correction for agent 1 (meters).</summary>
    public Vector3 PositionCorrection1;
    
    /// <summary>True if agent 1 should enter GROUNDED state.</summary>
    public bool TriggerGrounded1;
    
    /// <summary>True if agent 1 should enter STUMBLING state.</summary>
    public bool TriggerStumble1;
    
    /// <summary>Duration agent 1 stays grounded (seconds). Valid only if TriggerGrounded1.</summary>
    public float GroundedDuration1;
    
    // ================================================================
    // AGENT 2 RESPONSE
    // ================================================================
    
    /// <summary>Velocity change for agent 2 (m/s).</summary>
    public Vector3 VelocityImpulse2;
    
    /// <summary>Position correction for agent 2 (meters).</summary>
    public Vector3 PositionCorrection2;
    
    /// <summary>True if agent 2 should enter GROUNDED state.</summary>
    public bool TriggerGrounded2;
    
    /// <summary>True if agent 2 should enter STUMBLING state.</summary>
    public bool TriggerStumble2;
    
    /// <summary>Duration agent 2 stays grounded (seconds). Valid only if TriggerGrounded2.</summary>
    public float GroundedDuration2;
    
    // ================================================================
    // SHARED DATA
    // ================================================================
    
    /// <summary>Impact force in Newtons. Used for foul detection.</summary>
    public float ImpactForce;
}
```

### 3.3.4 Agent-Ball Collision Data

```csharp
/// <summary>
/// Data passed to Ball Physics when agent-ball collision is detected.
/// 
/// Ball Physics receives this via OnCollision() callback and handles:
///   - Ball deflection based on body part coefficients
///   - Spin transfer based on contact geometry
///   
/// First Touch Mechanics (Spec #11) receives this via separate callback and handles:
///   - Possession determination
///   - First touch quality based on attributes
/// </summary>
public struct AgentBallCollisionData
{
    /// <summary>
    /// Contact point in world coordinates (meters).
    /// Where the agent's hitbox touched the ball.
    /// </summary>
    public Vector3 ContactPoint;
    
    /// <summary>
    /// Agent velocity at moment of contact (m/s).
    /// Used by Ball Physics for momentum transfer calculation.
    /// </summary>
    public Vector3 AgentVelocity;
    
    /// <summary>
    /// Body part that contacted the ball.
    /// Stage 0: Always TORSO (simplification).
    /// Stage 1+: FOOT, SHIN, THIGH, TORSO, HEAD based on ball height and agent state.
    /// </summary>
    public BodyPart BodyPart;
    
    /// <summary>
    /// ID of agent that contacted the ball (0â€“21).
    /// </summary>
    public int AgentID;
    
    /// <summary>
    /// Team of agent that contacted the ball.
    /// Used for possession tracking.
    /// </summary>
    public int TeamID;
    
    /// <summary>
    /// True if contacting agent is a goalkeeper.
    /// Affects Ball Physics deflection behavior (goalkeeper can catch/parry).
    /// </summary>
    public bool IsGoalkeeper;
}
```

### 3.3.5 Foul Detection Data

```csharp
/// <summary>
/// Contact force data packaged for the Referee System.
/// 
/// Stage 0: Collision System populates this struct and includes it in CollisionEvent.
/// Stage 1+: Referee System consumes this to adjudicate fouls.
/// 
/// Design note: This is a data contract only. The Collision System does NOT
/// determine whether a foul occurred â€” it only provides the raw data.
/// </summary>
public struct ContactForceData
{
    /// <summary>Force magnitude in Newtons.</summary>
    public float ForceMagnitude;
    
    /// <summary>Normalized force direction (from instigator toward victim).</summary>
    public Vector3 ForceDirection;
    
    /// <summary>Type of contact for foul classification.</summary>
    public ContactType Type;
    
    /// <summary>Agent who initiated the contact (moved toward victim).</summary>
    public int InstigatorAgentID;
    
    /// <summary>Agent who received the contact.</summary>
    public int VictimAgentID;
    
    /// <summary>True if victim was in possession of the ball at contact time.</summary>
    public bool VictimHasBall;
    
    /// <summary>True if instigator was attempting to play the ball.</summary>
    public bool InstigatorPlayingBall;
}

/// <summary>
/// Classification of physical contact for foul determination.
/// Referee System uses this to apply different foul thresholds.
/// </summary>
public enum ContactType
{
    /// <summary>
    /// Shoulder-to-shoulder contact.
    /// Generally legal if force is reasonable and ball is within playing distance.
    /// </summary>
    SHOULDER_TO_SHOULDER,
    
    /// <summary>
    /// Contact from behind the victim.
    /// Higher likelihood of being ruled a foul.
    /// </summary>
    FROM_BEHIND,
    
    /// <summary>
    /// Contact from the side.
    /// Context-dependent â€” may be legal charge or foul.
    /// </summary>
    SIDE_IMPACT,
    
    /// <summary>
    /// Slide tackle contact (Stage 1+).
    /// Requires animation state to detect.
    /// </summary>
    SLIDE_TACKLE,
    
    /// <summary>
    /// Aerial challenge contact (Stage 1+).
    /// Requires jump state to detect.
    /// </summary>
    AERIAL_CHALLENGE
}
```

### 3.3.6 Determining Contact Type

```csharp
/// <summary>
/// Determines the type of contact for foul classification.
/// Uses relative positions and velocities of the two agents.
/// </summary>
public static class ContactTypeClassifier
{
    /// <summary>
    /// Angle threshold for "from behind" classification.
    /// Contact is "from behind" if instigator approaches from >120Â° behind victim's facing direction.
    /// </summary>
    private const float FROM_BEHIND_ANGLE_THRESHOLD = 120f; // degrees
    
    /// <summary>
    /// Angle threshold for "shoulder to shoulder" classification.
    /// Contact is "shoulder to shoulder" if angle between facing directions is <45Â°
    /// and relative position is roughly perpendicular to both.
    /// </summary>
    private const float SHOULDER_ANGLE_THRESHOLD = 45f; // degrees
    
    /// <summary>
    /// Classifies the type of contact between two agents.
    /// 
    /// Stage 0 implementation uses position-based heuristics.
    /// Stage 1+ will incorporate facing direction and animation state.
    /// </summary>
    /// <param name="instigator">Agent who initiated contact (higher approach velocity)</param>
    /// <param name="victim">Agent who received contact</param>
    /// <param name="collisionNormal">Direction from instigator toward victim</param>
    /// <returns>Contact type classification</returns>
    public static ContactType Classify(
        in AgentPhysicalProperties instigator,
        in AgentPhysicalProperties victim,
        Vector2 collisionNormal)
    {
        // Stage 0 simplified classification based on collision geometry
        // Full classification requires facing direction (available in Agent class but
        // not exposed in AgentPhysicalProperties â€” design decision for Stage 0)
        
        // Calculate approach angle
        Vector2 instigatorVel = new Vector2(instigator.Velocity.x, instigator.Velocity.y);
        float instigatorSpeed = instigatorVel.magnitude;
        
        if (instigatorSpeed < 0.1f)
        {
            // Stationary contact â€” classify as side impact
            return ContactType.SIDE_IMPACT;
        }
        
        // Normalize instigator velocity
        Vector2 approachDir = instigatorVel / instigatorSpeed;
        
        // Angle between approach direction and collision normal
        float approachAngle = Vector2.Dot(approachDir, collisionNormal);
        
        // If approaching nearly head-on (dot product close to 1)
        // and similar speeds, likely shoulder-to-shoulder
        Vector2 victimVel = new Vector2(victim.Velocity.x, victim.Velocity.y);
        float victimSpeed = victimVel.magnitude;
        
        if (victimSpeed > 0.1f)
        {
            Vector2 victimDir = victimVel / victimSpeed;
            float facingDot = Vector2.Dot(approachDir, victimDir);
            
            // Both moving in similar direction (parallel), contact from side
            if (facingDot > 0.7f)
            {
                return ContactType.SHOULDER_TO_SHOULDER;
            }
        }
        
        // If approaching from opposite side of victim's movement, likely from behind
        // This is a simplification â€” proper implementation needs facing direction
        if (approachAngle > 0.5f && victimSpeed > 1.0f)
        {
            Vector2 victimDir = victimVel / victimSpeed;
            float behindDot = Vector2.Dot(-collisionNormal, victimDir);
            
            if (behindDot > 0.5f)
            {
                return ContactType.FROM_BEHIND;
            }
        }
        
        // Default to side impact
        return ContactType.SIDE_IMPACT;
    }
    
    /// <summary>
    /// Determines which agent is the "instigator" (initiated contact).
    /// The instigator is the agent with higher approach velocity toward the other.
    /// </summary>
    public static void DetermineInstigatorAndVictim(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        Vector2 collisionNormal,
        out int instigatorIndex,
        out int victimIndex)
    {
        // Calculate approach velocities along collision normal
        Vector2 v1_2d = new Vector2(a1.Velocity.x, a1.Velocity.y);
        Vector2 v2_2d = new Vector2(a2.Velocity.x, a2.Velocity.y);
        
        // Velocity component toward the other agent
        float v1Approach = Vector2.Dot(v1_2d, collisionNormal);
        float v2Approach = Vector2.Dot(v2_2d, -collisionNormal);
        
        if (v1Approach > v2Approach)
        {
            instigatorIndex = 0;
            victimIndex = 1;
        }
        else
        {
            instigatorIndex = 1;
            victimIndex = 0;
        }
    }
}
```

---

## 3.4 Integration with Game Loop

### 3.4.1 CollisionSystem Class

```csharp
/// <summary>
/// Main collision system class.
/// Orchestrates spatial partitioning, detection, and response phases.
/// 
/// Usage:
///   Called once per frame by Match Simulator after Agent Movement and Ball Physics updates.
///   All agent positions must be finalized before calling UpdateCollisions().
/// 
/// Thread safety: NOT thread-safe. Single-threaded execution only.
/// Memory: Zero heap allocations during UpdateCollisions() after initialization.
/// </summary>
public class CollisionSystem
{
    // ================================================================
    // PRIVATE FIELDS
    // ================================================================
    
    private readonly SpatialHashGrid _spatialHash;
    private CollisionPairBitfield _processedPairs;
    private DeterministicRNG _rng;
    
    // Buffers for collision events (reused each frame)
    private readonly List<CollisionEvent> _collisionEvents;
    private int _collisionEventCount;
    
    // Agent reference for team lookup (set each frame)
    private Agent[] _agentRefs;
    
    // ================================================================
    // CONSTRUCTOR
    // ================================================================
    
    /// <summary>
    /// Initializes the collision system with pre-allocated buffers.
    /// Called once at simulation initialization.
    /// </summary>
    public CollisionSystem()
    {
        _spatialHash = new SpatialHashGrid();
        _processedPairs = new CollisionPairBitfield();
        _collisionEvents = new List<CollisionEvent>(SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME);
        
        // Pre-allocate event list capacity
        for (int i = 0; i < SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME; i++)
        {
            _collisionEvents.Add(default);
        }
    }
    
    // ================================================================
    // MAIN UPDATE
    // ================================================================
    
    /// <summary>
    /// Performs collision detection and response for all entities.
    /// 
    /// Called once per frame after Agent Movement and Ball Physics updates.
    /// Modifies agent velocities/positions and may trigger state changes.
    /// 
    /// Performance: <0.5ms worst case (see Section 2.4)
    /// </summary>
    /// <param name="agents">Array of 22 agents with finalized positions</param>
    /// <param name="ball">Ball state (modified if agent-ball collision)</param>
    /// <param name="matchSeed">Match seed for deterministic RNG</param>
    /// <param name="frameNumber">Current frame number (for RNG seeding)</param>
    /// <param name="matchTime">Current match time (for event timestamps)</param>
    /// <param name="eventBuffer">Output buffer for collision events</param>
    public void UpdateCollisions(
        Agent[] agents,
        ref BallState ball,
        ulong matchSeed,
        int frameNumber,
        float matchTime,
        ICollisionEventConsumer eventBuffer)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using var _ = CollisionProfiler.UpdateMarker.Auto();
        #endif
        
        // Store agent references for team lookup
        _agentRefs = agents;
        
        // Initialize deterministic RNG for this frame
        _rng = new DeterministicRNG(matchSeed ^ (ulong)frameNumber);
        
        // Reset frame state
        _processedPairs.Clear();
        _collisionEventCount = 0;
        
        // ============================================================
        // PHASE 1: Populate spatial hash
        // ============================================================
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.ClearMarker.Auto())
        #endif
        {
            _spatialHash.Clear();
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.InsertMarker.Auto())
        #endif
        {
            // Insert all agents
            for (int i = 0; i < agents.Length; i++)
            {
                var props = agents[i].PhysicalProperties;
                
                // Skip invalid positions
                if (HasInvalidValues(props.Position)) continue;
                
                _spatialHash.Insert(i, props.Position, props.HitboxRadius);
            }
            
            // Insert ball
            if (!HasInvalidValues(ball.Position))
            {
                _spatialHash.Insert(
                    SpatialHashConstants.BALL_ENTITY_ID, 
                    ball.Position, 
                    SpatialHashConstants.BALL_RADIUS);
            }
        }
        
        // ============================================================
        // PHASE 2 & 3: Broad phase query + Narrow phase detection
        // ============================================================
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.BroadPhaseMarker.Auto())
        #endif
        {
            ProcessAllCollisionPairs(agents, ref ball, matchTime);
        }
        
        // ============================================================
        // PHASE 4: Publish events
        // ============================================================
        
        for (int i = 0; i < _collisionEventCount; i++)
        {
            eventBuffer?.OnCollisionEvent(_collisionEvents[i]);
        }
    }
    
    // ================================================================
    // COLLISION PAIR PROCESSING
    // ================================================================
    
    private void ProcessAllCollisionPairs(Agent[] agents, ref BallState ball, float matchTime)
    {
        int pairsProcessed = 0;
        
        for (int i = 0; i < agents.Length; i++)
        {
            var props_i = agents[i].PhysicalProperties;
            
            // Skip invalid agents
            if (HasInvalidValues(props_i.Position)) continue;
            
            // Query nearby entities
            var nearby = _spatialHash.Query(props_i.Position, props_i.HitboxRadius);
            
            foreach (int j in nearby)
            {
                // Skip self
                if (j == i) continue;
                
                // Enforce pair ordering (lower ID first) for deduplication
                int lowId = (j < i) ? j : i;
                int highId = (j < i) ? i : j;
                
                // Skip if already processed
                if (_processedPairs.IsSet(lowId, highId)) continue;
                _processedPairs.Set(lowId, highId);
                
                // Check collision pair limit
                if (++pairsProcessed > SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[Collision] Exceeded MAX_COLLISION_PAIRS_PER_FRAME ({pairsProcessed})");
                    #endif
                    return;
                }
                
                if (j == SpatialHashConstants.BALL_ENTITY_ID)
                {
                    // Agent-ball collision
                    ProcessAgentBallCollision(agents[i], i, ref ball, matchTime);
                }
                else
                {
                    // Agent-agent collision
                    ProcessAgentAgentCollision(agents, i, j, matchTime);
                }
            }
        }
    }
    
    // ================================================================
    // AGENT-AGENT COLLISION
    // ================================================================
    
    private void ProcessAgentAgentCollision(Agent[] agents, int id1, int id2, float matchTime)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using var _ = CollisionProfiler.NarrowPhaseMarker.Auto();
        #endif
        
        var props1 = agents[id1].PhysicalProperties;
        var props2 = agents[id2].PhysicalProperties;
        
        // Narrow phase: exact intersection test
        if (!CollisionDetection.CheckAgentAgentCollision(in props1, in props2, out var manifold))
        {
            return; // No collision
        }
        
        manifold.Entity1ID = id1;
        manifold.Entity2ID = id2;
        
        // Determine if same team
        bool isSameTeam = CollisionFiltering.IsSameTeamCollision(
            agents[id1].TeamID, agents[id2].TeamID);
        
        // Calculate response
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.ResponseMarker.Auto())
        #endif
        {
            var response = CollisionResponse.CalculateAgentAgentResponse(
                in props1, in props2, in manifold, isSameTeam, ref _rng);
            
            // Apply responses to agents
            ApplyAgentResponse(agents[id1], response.VelocityImpulse1, response.PositionCorrection1,
                response.TriggerGrounded1, response.TriggerStumble1, response.GroundedDuration1);
            
            ApplyAgentResponse(agents[id2], response.VelocityImpulse2, response.PositionCorrection2,
                response.TriggerGrounded2, response.TriggerStumble2, response.GroundedDuration2);
            
            // Package foul detection data
            ContactTypeClassifier.DetermineInstigatorAndVictim(
                in props1, in props2, manifold.Normal,
                out int instigatorIdx, out int victimIdx);
            
            int instigatorId = (instigatorIdx == 0) ? id1 : id2;
            int victimId = (instigatorIdx == 0) ? id2 : id1;
            
            var foulData = new ContactForceData
            {
                ForceMagnitude = response.ImpactForce,
                ForceDirection = new Vector3(manifold.Normal.x, manifold.Normal.y, 0),
                Type = ContactTypeClassifier.Classify(
                    instigatorIdx == 0 ? in props1 : in props2,
                    instigatorIdx == 0 ? in props2 : in props1,
                    manifold.Normal),
                InstigatorAgentID = instigatorId,
                VictimAgentID = victimId,
                VictimHasBall = false, // TODO: Query possession system
                InstigatorPlayingBall = false // TODO: Query tactical system
            };
            
            // Record collision event
            RecordCollisionEvent(matchTime, CollisionType.AGENT_AGENT, id1, id2,
                new Vector3(manifold.ContactPoint.x, manifold.ContactPoint.y, 0),
                response.ImpactForce, foulData);
        }
    }
    
    // ================================================================
    // AGENT-BALL COLLISION
    // ================================================================
    
    private void ProcessAgentBallCollision(Agent agent, int agentId, ref BallState ball, float matchTime)
    {
        var props = agent.PhysicalProperties;
        
        // Narrow phase: exact intersection test
        if (!CollisionDetection.CheckAgentBallCollision(in props, in ball, out var contactPoint))
        {
            return; // No collision
        }
        
        // Package collision data for Ball Physics
        var collisionData = new AgentBallCollisionData
        {
            ContactPoint = contactPoint,
            AgentVelocity = props.Velocity,
            BodyPart = BodyPart.Torso, // Stage 0 simplification
            AgentID = agentId,
            TeamID = agent.TeamID,
            IsGoalkeeper = agent.IsGoalkeeper
        };
        
        // Callback to Ball Physics
        BallCollisionHandler.OnAgentCollision(ref ball, collisionData);
        
        // Record collision event (no foul data for agent-ball)
        RecordCollisionEvent(matchTime, CollisionType.AGENT_BALL, agentId,
            SpatialHashConstants.BALL_ENTITY_ID, contactPoint, 0f, default);
    }
    
    // ================================================================
    // RESPONSE APPLICATION
    // ================================================================
    
    private void ApplyAgentResponse(
        Agent agent,
        Vector3 velocityImpulse,
        Vector3 positionCorrection,
        bool triggerGrounded,
        bool triggerStumble,
        float groundedDuration)
    {
        // Apply velocity impulse
        if (velocityImpulse.sqrMagnitude > 0.0001f)
        {
            agent.ApplyVelocityImpulse(velocityImpulse);
        }
        
        // Apply position correction
        if (positionCorrection.sqrMagnitude > 0.0001f)
        {
            agent.ApplyPositionCorrection(positionCorrection);
        }
        
        // Trigger state changes
        if (triggerGrounded)
        {
            agent.TriggerGroundedState(GroundedReason.COLLISION, groundedDuration);
        }
        else if (triggerStumble)
        {
            agent.TriggerStumbleState();
        }
    }
    
    // ================================================================
    // EVENT RECORDING
    // ================================================================
    
    private void RecordCollisionEvent(
        float matchTime,
        CollisionType type,
        int entity1Id,
        int entity2Id,
        Vector3 contactPoint,
        float impactForce,
        ContactForceData foulData)
    {
        if (_collisionEventCount >= SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME)
        {
            return; // Event buffer full
        }
        
        _collisionEvents[_collisionEventCount++] = new CollisionEvent
        {
            MatchTime = matchTime,
            Type = type,
            Entity1ID = entity1Id,
            Entity2ID = entity2Id,
            ContactPoint = contactPoint,
            ImpactForce = impactForce,
            FoulData = foulData
        };
    }
    
    // ================================================================
    // VALIDATION
    // ================================================================
    
    private static bool HasInvalidValues(Vector3 position)
    {
        return float.IsNaN(position.x) || float.IsInfinity(position.x) ||
               float.IsNaN(position.y) || float.IsInfinity(position.y) ||
               float.IsNaN(position.z) || float.IsInfinity(position.z);
    }
}
```

### 3.4.2 Collision Event Interface

```csharp
/// <summary>
/// Interface for systems that consume collision events.
/// Implemented by Event System (Spec #17), statistics tracker, replay system.
/// </summary>
public interface ICollisionEventConsumer
{
    void OnCollisionEvent(CollisionEvent evt);
}

/// <summary>
/// Collision event record for replay, statistics, and foul detection.
/// </summary>
public struct CollisionEvent
{
    /// <summary>Match time when collision occurred (seconds from kickoff).</summary>
    public float MatchTime;
    
    /// <summary>Type of collision.</summary>
    public CollisionType Type;
    
    /// <summary>First entity ID (lower ID by convention).</summary>
    public int Entity1ID;
    
    /// <summary>Second entity ID (higher ID, or BALL_ENTITY_ID).</summary>
    public int Entity2ID;
    
    /// <summary>Contact point in world coordinates.</summary>
    public Vector3 ContactPoint;
    
    /// <summary>Impact force in Newtons (agent-agent only).</summary>
    public float ImpactForce;
    
    /// <summary>Foul detection data (agent-agent only).</summary>
    public ContactForceData FoulData;
}

/// <summary>
/// Collision type enumeration.
/// </summary>
public enum CollisionType
{
    /// <summary>Two agents collided.</summary>
    AGENT_AGENT,
    
    /// <summary>Agent contacted the ball.</summary>
    AGENT_BALL,
    
    /// <summary>Goalkeeper-specific collision (Stage 1+).</summary>
    AGENT_GOALKEEPER,
    
    /// <summary>Aerial duel collision (Stage 1+).</summary>
    AERIAL_DUEL
}
```

### 3.4.3 Ball Collision Handler

```csharp
/// <summary>
/// Static handler for ball collision callbacks.
/// Routes collision data to Ball Physics for deflection calculation.
/// 
/// Note: This is a stub interface. Full implementation is in Ball Physics (Spec #1).
/// The collision system only calls this; it does not modify ball state directly.
/// </summary>
public static class BallCollisionHandler
{
    /// <summary>
    /// Callback when agent contacts ball.
    /// Ball Physics handles deflection based on body part coefficients.
    /// </summary>
    public static void OnAgentCollision(ref BallState ball, AgentBallCollisionData data)
    {
        // Implementation in Ball Physics Spec #1, Section 3.1.10.1
        // This spec defines the interface; Ball Physics defines the behavior
        
        // Deflection calculation:
        //   1. Get body part coefficients (speed retention, spin retention)
        //   2. Calculate deflection normal (from contact geometry)
        //   3. Apply momentum transfer (agent velocity â†’ ball velocity)
        //   4. Update ball spin based on contact angle
        
        // Placeholder for spec documentation â€” actual code in Ball Physics
    }
}
```

---

## Section 3 Summary

| Subsection | Key Content |
|------------|-------------|
| **3.1 Spatial Partitioning** | Grid-based spatial hash (1.0m cells, 106Ã—69 grid); O(N) insert, O(1) query |
| **3.2 Collision Detection** | Circle-circle (agent-agent), circle-sphere (agent-ball); XY plane projection |
| **3.3 Collision Response** | Impulse-based momentum transfer; fall/stumble thresholds; deterministic RNG |
| **3.4 Game Loop Integration** | CollisionSystem class; event publishing; Ball Physics callback |

### Known Limitations (Stage 0)

| Limitation | Impact | Resolution Target |
|------------|--------|-------------------|
| **Agility proxy** | Grounded duration uses Strength as Agility proxy; may differ by ~0.3s | Stage 1: Add Agility to AgentPhysicalProperties |
| **Body part always TORSO** | All agent-ball contacts treated as torso contact | Stage 1: Height-based body part detection |
| **VictimHasBall always false** | Foul detection lacks possession context | Stage 1: Integration with possession system |
| **InstigatorPlayingBall always false** | Cannot determine if contact was ball-directed | Stage 1: Integration with tactical intent system |
| **Contact type classification simplified** | Uses velocity heuristics without facing direction | Stage 1: Add facing direction to classification |

---

## Cross-Reference Verification

| Reference | Verified | Notes |
|-----------|----------|-------|
| Agent Movement Â§3.5.4 (AgentPhysicalProperties) | âœ“ | All fields consumed correctly |
| Agent Movement Â§3.5.4.2 (Mass formula: 72.5â€“100 kg) | âœ“ | Used in impulse calculation |
| Agent Movement Â§3.5.4.3 (HitboxRadius formula: 0.35â€“0.50m) | âœ“ | Used in cell size derivation |
| Agent Movement Â§3.1.2 (GROUNDED/STUMBLING states) | âœ“ | State triggers defined |
| Ball Physics Â§3.1.10.1 (BodyPart enum, OnCollision) | âœ“ | Interface matches |
| Ball Physics Â§3.1.1 (Coordinate system) | âœ“ | Origin, axes consistent |
| Master Vol 1 Â§1.3 (Determinism) | âœ“ | Deterministic RNG implemented |

---

**End of Section 3**

**Page Count:** ~18 pages  
**Next Section:** Section 4 â€” Data Structures (comprehensive struct definitions)
