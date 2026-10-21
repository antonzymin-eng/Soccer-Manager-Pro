// File:     src/collision-system/tests/SpatialHashGridQueryOrderTests.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     Collision System #3 §3.1.2, Perception System #7 §3.0 Step 1, Code Standards #20;
//           docs/tracking/perception-shared-pass-design.md §3.1 item 1, §5 E1 (P1a)
// Purpose:  Locks Gate-J J-05 / P1a. SpatialHashGrid.Query now clamps its cell square to the grid and,
//           when the square covers more cells than are occupied, walks the occupied-cell list sorted
//           into row-major order. Perception consumes the returned sequence in order (pressure sum,
//           Visible* entry order, ball occlusion), so the COMPLETE sequence — order and duplicates —
//           must equal the pre-J-05 dense scan, which is kept below as a test-only reference.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.CollisionSystem.Tests
{
    [TestFixture]
    internal class SpatialHashGridQueryOrderTests
    {
        private const int Configurations = 3000;

        // Perception's real query radius (PerceptionConstants.MaxPerceptionRange) and the agent
        // hitbox radius collision queries with; spelled out because this test assembly does not
        // reference perception-system.
        private const float PerceptionRangeM = 120f;
        private const float HitboxRadiusM = 0.40f;

        // ── Test-only reference: the pre-J-05 Query, verbatim apart from reading the grid through
        //    the insert log instead of the private cells. ─────────────────────────────────────────
        private sealed class ReferenceGrid
        {
            private readonly List<int>[] _cells;

            public ReferenceGrid()
            {
                _cells = new List<int>[SpatialHashConstants.TotalCells];
                for (int i = 0; i < _cells.Length; i++) { _cells[i] = new List<int>(); }
            }

            public void Clear()
            {
                for (int i = 0; i < _cells.Length; i++) { _cells[i].Clear(); }
            }

            // Mirrors SpatialHashGrid.Insert's straddle rule exactly.
            public void Insert(int entityId, Vector3 position, float radius)
            {
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) { return; }
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
                Add(entityId, cx, cy);
                if (left)            Add(entityId, cx - 1, cy);
                if (right)           Add(entityId, cx + 1, cy);
                if (bottom)          Add(entityId, cx, cy - 1);
                if (top)             Add(entityId, cx, cy + 1);
                if (left && bottom)  Add(entityId, cx - 1, cy - 1);
                if (left && top)     Add(entityId, cx - 1, cy + 1);
                if (right && bottom) Add(entityId, cx + 1, cy - 1);
                if (right && top)    Add(entityId, cx + 1, cy + 1);
            }

            private void Add(int entityId, int cx, int cy) =>
                _cells[cy * SpatialHashConstants.GridWidth + cx].Add(entityId);

            public List<int> Query(Vector3 position, float radius)
            {
                var result = new List<int>();
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
                        result.AddRange(_cells[y * gw + x]);
                    }
                }
                return result;
            }

            private static int CellX(float worldX) =>
                Mathf.Clamp(Mathf.FloorToInt(worldX / SpatialHashConstants.CellSize), 0, SpatialHashConstants.GridWidth - 1);

            private static int CellY(float worldY) =>
                Mathf.Clamp(Mathf.FloorToInt(worldY / SpatialHashConstants.CellSize), 0, SpatialHashConstants.GridHeight - 1);
        }

        // SplitMix64 (#16 §3.4.4) so every configuration is reproducible from its index.
        private static ulong NextSplitMix64(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL; // Spec #16 §3.4.4
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static float Uniform(ref ulong state, float min, float max) =>
            min + (max - min) * ((NextSplitMix64(ref state) >> 40) / (float)(1UL << 24));

        // On-pitch, off-pitch (clamped into the edge cells), exactly-on-a-cell-boundary, and stacked
        // on an earlier entity: the cases where cell assignment and per-cell order are least obvious.
        private static Vector3 RandomPosition(ref ulong state, Vector3[] earlier, int count)
        {
            switch (NextSplitMix64(ref state) % 8)
            {
                case 0:
                    return new Vector3(Uniform(ref state, -6f, 111f), Uniform(ref state, -6f, 74f), 0f);
                case 1:
                    return new Vector3(NextSplitMix64(ref state) % 106, NextSplitMix64(ref state) % 69, 0f);
                case 2:
                    return count > 0 ? earlier[(int)(NextSplitMix64(ref state) % (ulong)count)] : Vector3.zero;
                default:
                    return new Vector3(Uniform(ref state, 0f, 105f), Uniform(ref state, 0f, 68f), Uniform(ref state, 0f, 2f));
            }
        }

        [Test]
        public void Query_SeededConfigurations_ReturnsTheDenseScanSequenceExactly()
        {
            var grid = new SpatialHashGrid();
            var reference = new ReferenceGrid();
            var positions = new Vector3[24];
            float[] radii = { 0f, 0.4f, 1f, 2.5f, 3f, 7.9f, 12f, 40f, PerceptionRangeM, 130f };
            int wideQueries = 0;
            int nonEmptyQueries = 0;

            for (int c = 0; c < Configurations; c++)
            {
                ulong state = 0xD1A6D05EUL + (ulong)c;
                grid.Clear();
                reference.Clear();

                // 22 players + ball (id −1), or 21/20 players when someone is "sent off".
                int players = 22 - (int)(NextSplitMix64(ref state) % 3);
                // Perception inserts points (radius 0); collision inserts hitboxes; mix both.
                float insertRadius = (c & 1) == 0 ? 0f : HitboxRadiusM * (1 + (int)(NextSplitMix64(ref state) % 3));
                for (int i = 0; i <= players; i++)
                {
                    int id = i == players ? -1 : i;
                    positions[i] = RandomPosition(ref state, positions, i);
                    grid.Insert(id, positions[i], insertRadius);
                    reference.Insert(id, positions[i], insertRadius);
                }

                for (int q = 0; q < 6; q++)
                {
                    Vector3 at = q < 3 ? positions[(int)(NextSplitMix64(ref state) % (ulong)(players + 1))]
                                       : RandomPosition(ref state, positions, players + 1);
                    float radius = radii[NextSplitMix64(ref state) % (ulong)radii.Length];

                    List<int> expected = reference.Query(at, radius);
                    List<int> actual = grid.Query(at, radius);

                    if (expected.Count > 0) { nonEmptyQueries++; }
                    if (radius >= 12f) { wideQueries++; }
                    CollectionAssert.AreEqual(expected, actual,
                        $"config {c}, query {q}: at ({at.x}, {at.y}) radius {radius} — sequence diverged from the dense scan.");
                }
            }

            // The comparison must have exercised both paths with real content, not empty lists.
            Assert.Greater(wideQueries, Configurations, "too few wide (occupied-cell path) queries");
            Assert.Greater(nonEmptyQueries, Configurations * 4, "too few non-empty queries");
        }

        [Test]
        public void Query_PerceptionRange_ReturnsEveryEntityInRowMajorCellOrder()
        {
            // A hand-checkable instance of the order contract: cell row first, then column, then
            // insertion order within a cell.
            var grid = new SpatialHashGrid();
            grid.Insert(7, new Vector3(90.5f, 10.5f, 0f), 0f);  // row 10, col 90
            grid.Insert(3, new Vector3(5.5f, 60.5f, 0f), 0f);   // row 60, col 5
            grid.Insert(-1, new Vector3(5.5f, 10.5f, 0f), 0f);  // row 10, col 5
            grid.Insert(9, new Vector3(90.2f, 10.7f, 0f), 0f);  // row 10, col 90, after 7

            CollectionAssert.AreEqual(new[] { -1, 7, 9, 3 }, grid.Query(new Vector3(52.5f, 34f, 0f), PerceptionRangeM));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-21 | —      | Initial creation (Gate-J J-05 / P1a E1): seeded sequence        |
// |         |            |        | differential against the pre-J-05 dense scan (order and          |
// |         |            |        | duplicates), plus a hand-checkable row-major order instance.     |
#endregion
