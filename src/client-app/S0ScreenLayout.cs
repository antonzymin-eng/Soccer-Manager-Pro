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

        /// <summary>Places full measured labels inside the viewport, moving collisions to the nearest free grid cell.</summary>
        public static void PlacePitchLabels(Vector2 viewport, Vector2[] markers, Vector2[] sizes, bool[] visible, Vector2[] centres, float gap)
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
                return;
            for (int i = 0; i < markers.Length; i++)
            {
                if (!visible[i])
                    continue;
                RequireFinite(markers[i].x);
                RequireFinite(markers[i].y);
                if (sizes[i].x > viewport.x || sizes[i].y > viewport.y)
                    throw new InvalidOperationException("Pitch viewport cannot hold the measured labels.");
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

                // If the measured viewport cannot hold every label, the binding must grow/scroll it;
                // a hidden/truncated name or autoshrink is never a successful layout result.
                if (float.IsPositiveInfinity(nearest))
                    throw new InvalidOperationException("Pitch viewport cannot hold the measured labels.");
            }
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
#endregion
