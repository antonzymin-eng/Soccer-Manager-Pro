// File:     src/collision-system/SpatialHashGrid.cs
// Created:  2026-05-25
// Modified: 2026-06-12
// Author:   —
// Spec:     Collision System #3 §3.1.2, FR-01, Code Standards #20
// Purpose:  Uniform grid spatial hash — O(N) insert, O(1) avg query. Broad phase only.

using System.Collections.Generic;

using UnityEngine;
using Unity.Profiling;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Pre-allocated uniform grid covering 106×69 cells (1m each) over the 105×68m pitch.
    /// Zero heap allocation per frame after construction. Collision System #3 §3.1.2.
    /// Not thread-safe.
    /// </summary>
    public sealed class SpatialHashGrid
    {
        private static readonly ProfilerMarker s_clearMarker =
            new ProfilerMarker("CollisionSystem.SpatialHash.Clear");

        private static readonly ProfilerMarker s_insertMarker =
            new ProfilerMarker("CollisionSystem.SpatialHash.Insert");

        private static readonly ProfilerMarker s_queryMarker =
            new ProfilerMarker("CollisionSystem.SpatialHash.Query");

        private readonly List<int>[] _cells;
        private readonly List<int> _queryBuffer;
        private readonly List<int> _occupiedCells;

        /// <summary>Allocates all cell lists at construction time. Call once per match.</summary>
        public SpatialHashGrid()
        {
            int total = SpatialHashConstants.TotalCells;
            _cells = new List<int>[total];
            for (int i = 0; i < total; i++)
            {
                _cells[i] = new List<int>(4);
            }
            _queryBuffer = new List<int>(32);
            _occupiedCells = new List<int>(64);
        }

        /// <summary>
        /// Clears only occupied cells (sparse clear). O(K) where K = occupied cells (~30–50).
        /// </summary>
        public void Clear()
        {
            using var _ = s_clearMarker.Auto();

            for (int i = 0; i < _occupiedCells.Count; i++)
            {
                _cells[_occupiedCells[i]].Clear();
            }
            _occupiedCells.Clear();
        }

        /// <summary>
        /// Inserts entity into all cells its hitbox overlaps. O(1)–O(9).
        /// </summary>
        /// <param name="entityId">Agent index 0–21 or SpatialHashConstants.BALL_ENTITY_ID.</param>
        /// <param name="position">World position.</param>
        /// <param name="radius">Hitbox radius (m).</param>
        public void Insert(int entityId, Vector3 position, float radius)
        {
            using var _ = s_insertMarker.Auto();

            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
            {
                return;
            }

            // Defensive: negative or non-finite radius silently degrades to center-cell-only
            // insert (left/right/bottom/top predicates all evaluate false).
            Debug.Assert(radius >= 0f && float.IsFinite(radius),
                "SpatialHashGrid.Insert radius must be finite and non-negative.");

            float cs = SpatialHashConstants.CellSize;
            int cx = CellX(position.x);
            int cy = CellY(position.y);
            float lx = position.x - cx * cs;
            float ly = position.y - cy * cs;
            int gw = SpatialHashConstants.GridWidth;
            int gh = SpatialHashConstants.GridHeight;

            bool left   = lx < radius && cx > 0;
            bool right  = (cs - lx) < radius && cx < gw - 1;
            bool bottom = ly < radius && cy > 0;
            bool top    = (cs - ly) < radius && cy < gh - 1;

            InsertCell(entityId, cx,     cy);
            if (left)              InsertCell(entityId, cx - 1, cy);
            if (right)             InsertCell(entityId, cx + 1, cy);
            if (bottom)            InsertCell(entityId, cx,     cy - 1);
            if (top)               InsertCell(entityId, cx,     cy + 1);
            if (left  && bottom)   InsertCell(entityId, cx - 1, cy - 1);
            if (left  && top)      InsertCell(entityId, cx - 1, cy + 1);
            if (right && bottom)   InsertCell(entityId, cx + 1, cy - 1);
            if (right && top)      InsertCell(entityId, cx + 1, cy + 1);
        }

        /// <summary>
        /// Returns all entity IDs in the neighbourhood of cells covering query radius.
        /// Returns the internal reuse buffer — DO NOT MUTATE; copy if needed beyond the
        /// next Query call (the buffer is cleared at the next Query). O(E) where E =
        /// entities in queried cells.
        /// </summary>
        public List<int> Query(Vector3 position, float radius)
        {
            using var _ = s_queryMarker.Auto();

            _queryBuffer.Clear();

            int cx = CellX(position.x);
            int cy = CellY(position.y);
            int cr = Mathf.Max(1, Mathf.CeilToInt(radius / SpatialHashConstants.CellSize));
            int gw = SpatialHashConstants.GridWidth;
            int gh = SpatialHashConstants.GridHeight;

            for (int dy = -cr; dy <= cr; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= gh) continue;

                for (int dx = -cr; dx <= cr; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= gw) continue;

                    List<int> cell = _cells[y * gw + x];
                    for (int i = 0; i < cell.Count; i++)
                    {
                        _queryBuffer.Add(cell[i]);
                    }
                }
            }

            return _queryBuffer;
        }

        private void InsertCell(int entityId, int cx, int cy)
        {
            int idx = cy * SpatialHashConstants.GridWidth + cx;
            List<int> cell = _cells[idx];

            if (cell.Count == 0)
            {
                _occupiedCells.Add(idx);
            }

            cell.Add(entityId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cell.Count >= SpatialHashConstants.CellDensityWarning)
            {
                Debug.LogWarning($"[CollisionSystem] High cell density at ({cx},{cy}): {cell.Count} entities");
            }
#endif
        }

        private static int CellX(float worldX)
        {
            // FloorToInt, not cast-truncation: (int) truncates toward zero, which made cell 0
            // double-width for slightly-negative (off-pitch) coordinates.
            return Mathf.Clamp(Mathf.FloorToInt(worldX / SpatialHashConstants.CellSize),
                0, SpatialHashConstants.GridWidth - 1);
        }

        private static int CellY(float worldY)
        {
            return Mathf.Clamp(Mathf.FloorToInt(worldY / SpatialHashConstants.CellSize),
                0, SpatialHashConstants.GridHeight - 1);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                    |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                           |
// | 1.1     | 2026-06-05 | —      | AR-1 M-3. Insert position validity check widened to !float.IsFinite on x/y/z (was        |
// |         |            |        | IsNaN on x/y only). Closes NaN-z propagation into CollisionEvent.ContactPoint via        |
// |         |            |        | CheckAgentBallCollision (NaN > AgentReachHeight returns false, bypassing the gate).      |
// | 1.2     | 2026-06-05 | —      | AR-3 L-3. Insert gains Debug.Assert(radius >= 0 && IsFinite) — negative or non-finite    |
// |         |            |        | radius silently degraded to center-cell-only insert.                                     |
// | 1.3     | 2026-06-05 | —      | AR-6 L-3. Query XML doc explicitly forbids mutating the returned buffer (was implicit    |
// |         |            |        | from "copy if needed beyond next query").                                                |
// | 1.4     | 2026-06-10 | —      | AR-7 L-3. CellX/CellY cast-truncation → Mathf.FloorToInt — (int) truncates toward zero, |
// |         |            |        | making cell 0 cover [-1, 1) for slightly-negative off-pitch coordinates.                 |
// | 1.5     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling.             |
// |         |            |        | ProfilerMarker's actual namespace is Unity.Profiling; the old using was CS0246 under    |
// |         |            |        | Unity and the Linux compile gate alike, so this assembly could not have compiled        |
// |         |            |        | in-engine. No functional change.                                                        |
#endregion
