// File:     src/positioning-ai/RotationPair.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Positional Rotations #25 §2.2.3, Code Standards #20
// Purpose:  One rotation adjacency-table row: an unordered pair of formation-slot indices
//           eligible for an organic exchange (#25 FM-RO-01).

using System;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// One adjacency-table row (#25 §2.2.3): slot indices into the family's formation table.
    /// Stored normalized <see cref="SlotA"/> &lt; <see cref="SlotB"/> (the predicate is symmetric;
    /// normalization keeps rows canonical and distinctness checks trivial). The constructor
    /// enforces the F1 invariants that do not need the family table (distinct, non-GK, ordered);
    /// index validity against SQUAD_SIZE is the catalogue-invariant test's concern (FR-RO-016).
    /// </summary>
    public readonly struct RotationPair
    {
        /// <summary>Lower slot index of the pair. Never 0 (the goalkeeper — FR-RO-003).</summary>
        public readonly int SlotA;

        /// <summary>Higher slot index of the pair.</summary>
        public readonly int SlotB;

        /// <summary>Constructs a normalized pair; refuses GK, duplicate, and negative indices (F1).</summary>
        public RotationPair(int slotA, int slotB)
        {
            if (slotA == slotB)
            {
                throw new ArgumentException($"Rotation pair slots must be distinct (both {slotA}). F1 / FR-RO-016.");
            }
            int lo = Math.Min(slotA, slotB);
            int hi = Math.Max(slotA, slotB);
            if (lo <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotA), lo,
                    "Rotation pair slots must be positive outfield slot indices (slot 0 is the goalkeeper — FR-RO-003).");
            }
            SlotA = lo;
            SlotB = hi;
        }

        /// <summary>True when <paramref name="slotIndex"/> is either member (partner-lock scans, §3.3).</summary>
        public bool Contains(int slotIndex)
        {
            return slotIndex == SlotA || slotIndex == SlotB;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#25 T0 scaffolding). |
#endregion
