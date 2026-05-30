// File:     src/deterministic-sim/CanonicalSerializer.cs
// Created:  2026-05-29
// Modified: 2026-05-29 (AR-1 H-1/H-2: FloatUintUnion replaces BitConverter heap pattern)
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.4.1, §3.4, Code Standards #20
// Purpose:  Static helper for canonical binary serialization of all primitive types per §3.2.4.1.
//           All multi-byte values are little-endian. -0.0 is normalized to +0.0. NaN (Tier B only)
//           is normalized to the canonical bit pattern. Stage-0 strings are ASCII-only.

using System;
using System.Text;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Canonical binary serialization per §3.2.4.1.
    /// Byte layout: all multi-byte integers little-endian; f32/f64 IEEE-754 with -0.0 normalization.
    /// Used by SnapshotCodec and digest chain computation.
    /// Deterministic Simulation #16 §3.2.4.1.
    /// </summary>
    public static class CanonicalSerializer
    {
        // ── Write methods ─────────────────────────────────────────────────────────────

        /// <summary>Writes canonical bool (0x00/0x01). §3.2.4.1.</summary>
        public static void WriteBool(byte[] buf, ref int offset, bool value)
        {
            buf[offset++] = value ? DeterministicSimConstants.BOOL_TRUE : DeterministicSimConstants.BOOL_FALSE;
        }

        /// <summary>Writes u8. §3.2.4.1.</summary>
        public static void WriteU8(byte[] buf, ref int offset, byte value)
        {
            buf[offset++] = value;
        }

        /// <summary>Writes i8. §3.2.4.1.</summary>
        public static void WriteI8(byte[] buf, ref int offset, sbyte value)
        {
            buf[offset++] = (byte)value;
        }

        /// <summary>Writes u16 little-endian. §3.2.4.1.</summary>
        public static void WriteU16(byte[] buf, ref int offset, ushort value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            offset += 2;
        }

        /// <summary>Writes i16 little-endian. §3.2.4.1.</summary>
        public static void WriteI16(byte[] buf, ref int offset, short value)
        {
            WriteU16(buf, ref offset, (ushort)value);
        }

        /// <summary>Writes u32 little-endian. §3.2.4.1.</summary>
        public static void WriteU32(byte[] buf, ref int offset, uint value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
            offset += 4;
        }

        /// <summary>Writes i32 little-endian. §3.2.4.1.</summary>
        public static void WriteI32(byte[] buf, ref int offset, int value)
        {
            WriteU32(buf, ref offset, (uint)value);
        }

        /// <summary>Writes u64 little-endian. §3.2.4.1.</summary>
        public static void WriteU64(byte[] buf, ref int offset, ulong value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
            buf[offset + 4] = (byte)(value >> 32);
            buf[offset + 5] = (byte)(value >> 40);
            buf[offset + 6] = (byte)(value >> 48);
            buf[offset + 7] = (byte)(value >> 56);
            offset += 8;
        }

        /// <summary>Writes i64 little-endian. §3.2.4.1.</summary>
        public static void WriteI64(byte[] buf, ref int offset, long value)
        {
            WriteU64(buf, ref offset, (ulong)value);
        }

        /// <summary>
        /// Writes f32 as 4-byte IEEE-754 little-endian.
        /// -0.0 is normalized to +0.0. §3.2.4.1.
        /// NaN normalization applies only to Tier B fields; Tier A NaN is a hard error (ERR_DS_TIERA_NONFINITE).
        /// </summary>
        public static void WriteF32(byte[] buf, ref int offset, float value)
        {
            uint bits = SingleToUInt32Bits(value);
            // Normalize -0.0 → +0.0
            if (bits == DeterministicSimConstants.F32_SIGN_MASK)
            {
                bits = DeterministicSimConstants.F32_POSITIVE_ZERO_BITS;
            }
            WriteU32(buf, ref offset, bits);
        }

        /// <summary>
        /// Writes f32 Tier B canonical encoding (NaN normalized to 0x7FC00000; -0.0 → +0.0). §3.2.4.1.
        /// </summary>
        public static void WriteF32TierB(byte[] buf, ref int offset, float value)
        {
            uint bits = SingleToUInt32Bits(value);
            if (bits == DeterministicSimConstants.F32_SIGN_MASK)
            {
                bits = DeterministicSimConstants.F32_POSITIVE_ZERO_BITS;
            }
            // Normalize any NaN to canonical quiet NaN
            bool isNaN = (bits & 0x7F800000u) == 0x7F800000u && (bits & 0x007FFFFFu) != 0u;
            if (isNaN)
            {
                bits = DeterministicSimConstants.F32_CANONICAL_NAN_BITS;
            }
            WriteU32(buf, ref offset, bits);
        }

        /// <summary>Writes f64 as 8-byte IEEE-754 little-endian with -0.0 normalization. §3.2.4.1.</summary>
        public static void WriteF64(byte[] buf, ref int offset, double value)
        {
            ulong bits = DoubleToUInt64Bits(value);
            if (bits == 0x8000000000000000UL)
            {
                bits = 0UL; // -0.0 → +0.0
            }
            WriteU64(buf, ref offset, bits);
        }

        /// <summary>
        /// Writes a length-prefixed string (u32 length + ASCII bytes). Stage 0 is ASCII-only. §3.2.4.1.
        /// </summary>
        public static void WriteString(byte[] buf, ref int offset, string value)
        {
            if (value == null)
            {
                WriteU32(buf, ref offset, 0u);
                return;
            }
            byte[] encoded = Encoding.ASCII.GetBytes(value);
            WriteU32(buf, ref offset, (uint)encoded.Length);
            Array.Copy(encoded, 0, buf, offset, encoded.Length);
            offset += encoded.Length;
        }

        /// <summary>
        /// Writes a length-prefixed byte array (u32 count + raw bytes). §3.2.4.1.
        /// </summary>
        public static void WriteBytes(byte[] buf, ref int offset, byte[] value)
        {
            if (value == null)
            {
                WriteU32(buf, ref offset, 0u);
                return;
            }
            WriteU32(buf, ref offset, (uint)value.Length);
            Array.Copy(value, 0, buf, offset, value.Length);
            offset += value.Length;
        }

        /// <summary>Writes optional&lt;T&gt; present tag followed by the value. §3.2.4.1.</summary>
        public static void WriteOptionalPresent(byte[] buf, ref int offset)
        {
            buf[offset++] = DeterministicSimConstants.OPTIONAL_PRESENT_TAG;
        }

        /// <summary>Writes optional&lt;T&gt; absent tag. §3.2.4.1.</summary>
        public static void WriteOptionalAbsent(byte[] buf, ref int offset)
        {
            buf[offset++] = DeterministicSimConstants.OPTIONAL_ABSENT_TAG;
        }

        // ── Read methods ──────────────────────────────────────────────────────────────

        /// <summary>Reads canonical bool. §3.2.4.1.</summary>
        public static bool ReadBool(byte[] buf, ref int offset) =>
            buf[offset++] != 0;

        /// <summary>Reads u8. §3.2.4.1.</summary>
        public static byte ReadU8(byte[] buf, ref int offset) => buf[offset++];

        /// <summary>Reads u16 little-endian. §3.2.4.1.</summary>
        public static ushort ReadU16(byte[] buf, ref int offset)
        {
            ushort v = (ushort)((ushort)buf[offset] | ((ushort)buf[offset + 1] << 8));
            offset += 2;
            return v;
        }

        /// <summary>Reads u32 little-endian. §3.2.4.1.</summary>
        public static uint ReadU32(byte[] buf, ref int offset)
        {
            uint v = (uint)buf[offset]
                   | ((uint)buf[offset + 1] << 8)
                   | ((uint)buf[offset + 2] << 16)
                   | ((uint)buf[offset + 3] << 24);
            offset += 4;
            return v;
        }

        /// <summary>Reads i32 little-endian. §3.2.4.1.</summary>
        public static int ReadI32(byte[] buf, ref int offset) => (int)ReadU32(buf, ref offset);

        /// <summary>Reads u64 little-endian. §3.2.4.1.</summary>
        public static ulong ReadU64(byte[] buf, ref int offset)
        {
            ulong v = (ulong)buf[offset]
                    | ((ulong)buf[offset + 1] << 8)
                    | ((ulong)buf[offset + 2] << 16)
                    | ((ulong)buf[offset + 3] << 24)
                    | ((ulong)buf[offset + 4] << 32)
                    | ((ulong)buf[offset + 5] << 40)
                    | ((ulong)buf[offset + 6] << 48)
                    | ((ulong)buf[offset + 7] << 56);
            offset += 8;
            return v;
        }

        /// <summary>Reads f32 with -0.0 normalization. §3.2.4.1.</summary>
        public static float ReadF32(byte[] buf, ref int offset)
        {
            uint bits = ReadU32(buf, ref offset);
            if (bits == DeterministicSimConstants.F32_SIGN_MASK)
            {
                bits = DeterministicSimConstants.F32_POSITIVE_ZERO_BITS;
            }
            return UInt32BitsToSingle(bits);
        }

        /// <summary>Reads length-prefixed ASCII string. §3.2.4.1.</summary>
        public static string ReadString(byte[] buf, ref int offset)
        {
            uint length = ReadU32(buf, ref offset);
            if (length == 0u)
            {
                return string.Empty;
            }
            string s = Encoding.ASCII.GetString(buf, offset, (int)length);
            offset += (int)length;
            return s;
        }

        /// <summary>Reads optional presence tag. §3.2.4.1.</summary>
        public static bool ReadOptionalTag(byte[] buf, ref int offset) =>
            buf[offset++] == DeterministicSimConstants.OPTIONAL_PRESENT_TAG;

        // ── IEEE-754 bit manipulation helpers (zero allocation) ────────────────────────
        // AR-1 H-1/H-2: BitConverter.GetBytes allocates byte[4] on every call — violates FR-CS-066.
        // Fix: use an explicit-layout union struct for bit reinterpretation with zero allocation and no unsafe.

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct FloatUintUnion
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public float FloatValue;
            [System.Runtime.InteropServices.FieldOffset(0)] public uint  UintValue;
        }

        /// <summary>Reinterprets a float as its IEEE-754 bit pattern. Zero allocation. §3.2.4.1.</summary>
        public static uint SingleToUInt32Bits(float value)
        {
            FloatUintUnion u = default;
            u.FloatValue = value;
            return u.UintValue;
        }

        /// <summary>Reinterprets a uint as a float. Zero allocation. §3.2.4.1.</summary>
        public static float UInt32BitsToSingle(uint bits)
        {
            FloatUintUnion u = default;
            u.UintValue = bits;
            return u.FloatValue;
        }

        /// <summary>Reinterprets a double as its IEEE-754 bit pattern.</summary>
        public static ulong DoubleToUInt64Bits(double value) => (ulong)BitConverter.DoubleToInt64Bits(value);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1/H-2: FloatUintUnion replaces BitConverter.GetBytes alloc.  |
#endregion
