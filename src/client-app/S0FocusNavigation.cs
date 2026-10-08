// File:     src/client-app/S0FocusNavigation.cs
// Created:  2026-10-08
// Modified: 2026-10-08 (explicit Match View order)
// Author:   —
// Spec:     S0 journey §14.3, I-Q15, Code Standards #20
// Purpose:  Host-free focus traversal, composite pitch entry and current feedback target decisions.

using System;
using System.Collections.Generic;

namespace TacticalDirector.ClientApp
{
    /// <summary>Pure focus decisions; the binding supplies current visibility/modal eligibility and applies selection.</summary>
    public static class S0FocusNavigation
    {
        /// <summary>Distinguishes actions and composite inspection from explicit focus destinations.</summary>
        public enum Role
        {
            /// <summary>A heading or feedback destination outside Tab traversal.</summary>
            Anchor,
            /// <summary>An action or disclosure in normal traversal.</summary>
            Action,
            /// <summary>One member of the pitch's single Tab-entry group.</summary>
            PitchMarker
        }

        /// <summary>Only actions and the current pitch entry participate in Tab traversal.</summary>
        public static bool IsTabStop(Role role, int markerIndex, int pitchEntryIndex)
            => role == Role.Action || (role == Role.PitchMarker && markerIndex >= 0 && markerIndex == pitchEntryIndex);

        /// <summary>First available selector, or -1 when no selector survives.</summary>
        public static int FirstAvailable<T>(IReadOnlyList<T> items, Predicate<T> isAvailable)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (isAvailable == null)
                throw new ArgumentNullException(nameof(isAvailable));
            for (int i = 0; i < items.Count; i++)
                if (isAvailable(items[i]))
                    return i;
            return -1;
        }

        /// <summary>Move one eligible target forward/back with wrap; -1 means no eligible target.</summary>
        public static int Move<T>(IReadOnlyList<T> items, int currentIndex, int step, Predicate<T> isAvailable)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (isAvailable == null)
                throw new ArgumentNullException(nameof(isAvailable));
            if (step != 1 && step != -1)
                throw new ArgumentOutOfRangeException(nameof(step));
            if (currentIndex < -1 || currentIndex >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(currentIndex));
            int index = currentIndex < 0 ? (step > 0 ? -1 : 0) : currentIndex;
            for (int i = 0; i < items.Count; i++)
            {
                index = (index + step + items.Count) % items.Count;
                if (isAvailable(items[index]))
                    return index;
            }
            return -1;
        }

        /// <summary>
        /// Move through an explicit logical order rather than hierarchy order. A current target outside
        /// that order (a heading or feedback anchor) enters it before its next listed hierarchy successor.
        /// Returns a <paramref name="logicalOrder"/> index, or -1 when no eligible target exists.
        /// </summary>
        public static int MoveInLogicalOrder<T>(IReadOnlyList<T> logicalOrder, IReadOnlyList<T> hierarchy, int hierarchyIndex, int step, Predicate<T> isAvailable)
            where T : class
        {
            if (logicalOrder == null)
                throw new ArgumentNullException(nameof(logicalOrder));
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (isAvailable == null)
                throw new ArgumentNullException(nameof(isAvailable));
            if (step != 1 && step != -1)
                throw new ArgumentOutOfRangeException(nameof(step));
            if (hierarchyIndex < -1 || hierarchyIndex >= hierarchy.Count)
                throw new ArgumentOutOfRangeException(nameof(hierarchyIndex));
            if (hierarchyIndex < 0)
                return Move(logicalOrder, -1, step, isAvailable);
            int listed = IndexOf(logicalOrder, hierarchy[hierarchyIndex]);
            if (listed >= 0)
                return Move(logicalOrder, listed, step, isAvailable);
            int successor = -1;
            for (int h = hierarchyIndex + 1; h < hierarchy.Count && successor < 0; h++)
                successor = IndexOf(logicalOrder, hierarchy[h]);
            if (successor < 0)
                return Move(logicalOrder, step > 0 ? logicalOrder.Count - 1 : -1, step, isAvailable);
            return step > 0 ? Move(logicalOrder, successor - 1, 1, isAvailable) : Move(logicalOrder, successor, -1, isAvailable);
        }

        /// <summary>Latest current feedback anchor, regardless of spare retained rows; -1 means no record.</summary>
        public static int LatestFeedbackIndex(int currentCount, int retainedCount)
        {
            if (currentCount < 0)
                throw new ArgumentOutOfRangeException(nameof(currentCount));
            if (retainedCount < currentCount)
                throw new ArgumentOutOfRangeException(nameof(retainedCount));
            return currentCount - 1;
        }

        private static int IndexOf<T>(IReadOnlyList<T> items, T item) where T : class
        {
            for (int i = 0; i < items.Count; i++)
                if (ReferenceEquals(items[i], item))
                    return i;
            return -1;
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Consumed focus policy for action/anchor/pitch traversal, dialog selectors and retained feedback. |
// | 1.1     | 2026-10-08 | —      | Explicit logical Tab order with anchor entry at the next listed hierarchy successor (journey §9.3). |
#endregion
