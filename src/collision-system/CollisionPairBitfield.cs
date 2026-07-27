// File:     src/collision-system/CollisionPairBitfield.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.3]
// Author:   —
// Spec:     Collision System #3 §3.2.4, Code Standards #20
// Purpose:  Zero-alloc bitfield for deduplicating collision pairs each frame.

using UnityEngine;

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

        /// <summary>
        /// Reads the four backing words, for snapshot serialization of cross-tick contact state
        /// (<see cref="CollisionSystem.CaptureContactState"/>). Order is w0..w3 = bits 0..255.
        /// </summary>
        public void CaptureWords(out ulong w0, out ulong w1, out ulong w2, out ulong w3)
        {
            w0 = _bits0;
            w1 = _bits1;
            w2 = _bits2;
            w3 = _bits3;
        }

        /// <summary>
        /// Restores the four backing words captured by <see cref="CaptureWords"/>. No validation is
        /// possible or needed: every 256-bit pattern is a structurally valid pair set (bits above the
        /// reachable pair index simply never match a query).
        /// </summary>
        public void RestoreWords(ulong w0, ulong w1, ulong w2, ulong w3)
        {
            _bits0 = w0;
            _bits1 = w1;
            _bits2 = w2;
            _bits3 = w3;
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
            // Self-pairs are nonsensical and produce a triangular-index collision
            // (e.g. pair (22, 22) computes to 252, the same bit as pair (21, BALL)).
            // Caller invariant — outer loop already skips j == i.
            Debug.Assert(lowId != highId,
                "CollisionPairBitfield: self-pair is not addressable; triangular formula collapses.");

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
            // Max reachable index for 22 agents + 1 ball = 252 (pair (21, BALL)). C# shift
            // operators silently mask shift count modulo 64 — an out-of-range index would
            // alias onto the wrong bit in _bits3. Fail fast in development builds.
            Debug.Assert(index >= 0 && index < 256,
                "CollisionPairBitfield index out of range; pair-formula regression suspected.");

            if (index < 64)  return (_bits0 & (1UL << index))        != 0;
            if (index < 128) return (_bits1 & (1UL << (index - 64))) != 0;
            if (index < 192) return (_bits2 & (1UL << (index - 128))) != 0;
            return                  (_bits3 & (1UL << (index - 192))) != 0;
        }

        private void SetBit(int index)
        {
            Debug.Assert(index >= 0 && index < 256,
                "CollisionPairBitfield index out of range; pair-formula regression suspected.");

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
// | 1.2     | 2026-06-05 | —      | AR-3 L-1. GetBit / SetBit gain Debug.Assert(index in [0, 256)) — C# shift   |
// |         |            |        | masks shift count mod 64, so an out-of-range index would silently corrupt   |
// |         |            |        | _bits3 instead of throwing.                                                 |
// | 1.3     | 2026-06-05 | —      | AR-6 L-1. GetPairIndex gains Debug.Assert(lowId != highId) — self-pairs    |
// |         |            |        | produce a triangular-index collision (e.g. (22,22) → 252 collides with     |
// |         |            |        | (21, BALL)). Outer loop already skips j == i; defensive trap for misuse.   |
#endregion
