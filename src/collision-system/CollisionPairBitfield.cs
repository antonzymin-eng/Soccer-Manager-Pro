// File:     src/collision-system/CollisionPairBitfield.cs
// Created:  2026-05-25
// Modified: 2026-05-25  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.2.4, Code Standards #20
// Purpose:  Zero-alloc bitfield for deduplicating collision pairs each frame.

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Tracks processed collision pairs via four ulongs (256 bits, covers 253 max pairs).
    /// Pair index formula: lowId × PairFormulaRowSize - lowId×(lowId+1)/2 + (highId - lowId - 1).
    /// Ball ID (-1) maps to BallVirtualIndex. Collision System #3 §3.2.4.
    /// </summary>
    public struct CollisionPairBitfield
    {
        private ulong _bits0;
        private ulong _bits1;
        private ulong _bits2;
        private ulong _bits3;

        /// <summary>Clears all bits for a new frame. Zero-alloc.</summary>
        public void Clear()
        {
            _bits0 = 0;
            _bits1 = 0;
            _bits2 = 0;
            _bits3 = 0;
        }

        /// <summary>Returns true if the pair (lowId, highId) has already been processed.</summary>
        public bool IsSet(int lowId, int highId)
        {
            return GetBit(GetPairIndex(lowId, highId));
        }

        /// <summary>Marks the pair (lowId, highId) as processed.</summary>
        public void Set(int lowId, int highId)
        {
            SetBit(GetPairIndex(lowId, highId));
        }

        private static int GetPairIndex(int lowId, int highId)
        {
            int mappedLow = (lowId == SpatialHashConstants.BALL_ENTITY_ID)
                ? SpatialHashConstants.BallVirtualIndex : lowId;
            int mappedHigh = (highId == SpatialHashConstants.BALL_ENTITY_ID)
                ? SpatialHashConstants.BallVirtualIndex : highId;

            if (mappedLow > mappedHigh)
            {
                int tmp = mappedLow;
                mappedLow = mappedHigh;
                mappedHigh = tmp;
            }

            return mappedLow * SpatialHashConstants.PairFormulaRowSize
                - (mappedLow * (mappedLow + 1)) / 2
                + (mappedHigh - mappedLow - 1);
        }

        private bool GetBit(int index)
        {
            if (index < 64)  return (_bits0 & (1UL << index))        != 0;
            if (index < 128) return (_bits1 & (1UL << (index - 64))) != 0;
            if (index < 192) return (_bits2 & (1UL << (index - 128))) != 0;
            return                  (_bits3 & (1UL << (index - 192))) != 0;
        }

        private void SetBit(int index)
        {
            if      (index < 64)  _bits0 |= (1UL << index);
            else if (index < 128) _bits1 |= (1UL << (index - 64));
            else if (index < 192) _bits2 |= (1UL << (index - 128));
            else                  _bits3 |= (1UL << (index - 192));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                               |
// | 1.1     | 2026-05-25 | —      | Pass-4 fix. P4-1: 22 and 23 literals replaced with                           |
// |         |            |        | SpatialHashConstants.BallVirtualIndex / PairFormulaRowSize (FR-CS-016).      |
#endregion
