// File:     src/client-app/S0ScreenLayout.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  Host-free measured responsive layout and collision-free pitch labels with marker tether inputs.

using System;
using UnityEngine;

namespace TacticalDirector.ClientApp
{
    /// <summary>Pure presentation geometry; never changes world coordinates or shrinks selected text.</summary>
    public static class S0ScreenLayout
    {
        /// <summary>Use side-by-side pitch/rail only when the measured minimums and gap fit.</summary>
        public static bool CanUseColumns(float available, float pitchMinimum, float railMinimum, float gap)
        {
            RequireFinite(available);
            RequireFinite(pitchMinimum);
            RequireFinite(railMinimum);
            RequireFinite(gap);
            return available >= pitchMinimum + railMinimum + gap;
        }

        /// <summary>
        /// Places full labels near their markers, falling back to a complete grid if greedy placement
        /// exhausts free cells. Returns false for an unready/undersized surface; invalid inputs still fail.
        /// </summary>
        public static bool TryPlacePitchLabels(Vector2 viewport, Vector2[] markers, Vector2[] sizes, bool[] visible, Vector2[] centres, float gap)
        {
            RequireFinite(viewport.x);
            RequireFinite(viewport.y);
            RequireFinite(gap);
            if (markers == null || sizes == null || visible == null || centres == null || markers.Length != sizes.Length || markers.Length != visible.Length || markers.Length != centres.Length)
                throw new ArgumentException("Pitch label buffers differ.");
            float cellWidth = gap, cellHeight = gap;
            for (int i = 0; i < sizes.Length; i++)
            {
                RequireFinite(sizes[i].x);
                RequireFinite(sizes[i].y);
                cellWidth = Math.Max(cellWidth, sizes[i].x + gap);
                cellHeight = Math.Max(cellHeight, sizes[i].y + gap);
            }

            if (cellWidth <= 0 || cellHeight <= 0)
                return true;
            for (int i = 0; i < markers.Length; i++)
            {
                if (!visible[i])
                    continue;
                RequireFinite(markers[i].x);
                RequireFinite(markers[i].y);
                if (sizes[i].x > viewport.x || sizes[i].y > viewport.y)
                    return false;
                float hx = sizes[i].x * 0.5f, hy = sizes[i].y * 0.5f;
                Vector2 desired = new Vector2(Math.Max(hx, Math.Min(viewport.x - hx, markers[i].x)), Math.Max(hy, Math.Min(viewport.y - hy, markers[i].y)));
                centres[i] = desired;
                if (IsFree(i, desired, sizes, visible, centres, gap))
                    continue;
                float nearest = float.PositiveInfinity;
                for (float y = cellHeight * 0.5f; y + hy <= viewport.y; y += cellHeight)
                    for (float x = cellWidth * 0.5f; x + hx <= viewport.x; x += cellWidth)
                    {
                        Vector2 candidate = new Vector2(x, y);
                        float dx = candidate.x - desired.x, dy = candidate.y - desired.y;
                        float distance = dx * dx + dy * dy;
                        if (distance < nearest && IsFree(i, candidate, sizes, visible, centres, gap))
                        {
                            nearest = distance;
                            centres[i] = candidate;
                        }
                    }

                if (float.IsPositiveInfinity(nearest))
                    return PlaceGrid(viewport, sizes, visible, centres, cellWidth, cellHeight, gap);
            }
            return true;
        }

        /// <summary>
        /// Height needed for a complete grid at this width. The binding may grow its scrollable label
        /// surface while retaining the pitch image's aspect. Positive infinity means width is unready.
        /// </summary>
        public static float RequiredPitchLabelHeight(float width, Vector2[] sizes, bool[] visible, float gap)
        {
            RequireFinite(width);
            RequireFinite(gap);
            if (sizes == null || visible == null || sizes.Length != visible.Length)
                throw new ArgumentException("Pitch label buffers differ.");
            float cellWidth = gap, cellHeight = gap;
            int count = 0;
            for (int i = 0; i < sizes.Length; i++)
            {
                RequireFinite(sizes[i].x);
                RequireFinite(sizes[i].y);
                if (!visible[i])
                    continue;
                count++;
                cellWidth = Math.Max(cellWidth, sizes[i].x + gap);
                cellHeight = Math.Max(cellHeight, sizes[i].y + gap);
            }
            if (count == 0)
                return 0;
            int columns = cellWidth > 0 ? (int)Math.Floor((width + gap) / cellWidth) : 0;
            return columns == 0 ? float.PositiveInfinity : ((count + columns - 1) / columns) * cellHeight - gap;
        }

        private static bool PlaceGrid(Vector2 viewport, Vector2[] sizes, bool[] visible,
            Vector2[] centres, float cellWidth, float cellHeight, float gap)
        {
            int columns = (int)Math.Floor((viewport.x + gap) / cellWidth);
            int rows = (int)Math.Floor((viewport.y + gap) / cellHeight);
            int count = 0;
            for (int i = 0; i < visible.Length; i++)
                if (visible[i])
                    count++;
            if (columns == 0 || rows == 0 || (long)columns * rows < count)
                return false;
            int cell = 0;
            for (int i = 0; i < visible.Length; i++)
            {
                if (!visible[i])
                    continue;
                centres[i] = new Vector2((cell % columns) * cellWidth + (cellWidth - gap) * 0.5f,
                    (cell / columns) * cellHeight + (cellHeight - gap) * 0.5f);
                cell++;
            }
            return true;
        }

        private static bool IsFree(int index, Vector2 candidate, Vector2[] sizes, bool[] visible, Vector2[] centres, float gap)
        {
            for (int j = 0; j < index; j++)
                if (visible[j] && Math.Abs(candidate.x - centres[j].x) < (sizes[index].x + sizes[j].x) * 0.5f + gap && Math.Abs(candidate.y - centres[j].y) < (sizes[index].y + sizes[j].y) * 0.5f + gap)
                    return false;
            return true;
        }

        private static void RequireFinite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Measured reflow and marker-preserving label deconfliction. |
// | 1.1     | 2026-10-08 | —      | Nonfatal capacity result, complete-grid fallback and measured scroll-surface height. |
#endregion
