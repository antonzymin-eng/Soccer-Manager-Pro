// File:     src/deterministic-sim/FloatFlagTuple.cs
// Created:  2026-07-19
// Modified: 2026-07-19
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8.3, §3.2.4.1, §3.4, Code Standards #20
// Purpose:  The canonical 11-field float-flag tuple whose SHA-256 is EnvironmentFingerprint.FloatModelHash
//           (§4.8.3). ComputeHash() is the live-host hasher that was missing (ERR-016-006, resolved via
//           the Option-A Mono mapping in env-fingerprint-float-model-hash-mono-mapping.md).

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// The canonical, ordered 11-field compiler/runtime float-mode tuple of #16 §4.8.3.
    /// <see cref="ComputeHash"/> produces <c>floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ tuple))</c>,
    /// the value carried by <see cref="EnvironmentFingerprint.FloatModelHash"/>. Field order and types are
    /// pinned by the §4.8.3 table and MUST NOT be reordered (the hash is a wire contract; a reorder breaks
    /// cross-run fingerprint comparison, §4.8.2).
    /// </summary>
    public readonly struct FloatFlagTuple
    {
        /// <summary>Field 1: compiler toolchain — "MSVC"/"Clang"/"AppleClang"/"GCC" (Stage 5+ IL2CPP) or "Mono" (Stage-0). §4.8.3.</summary>
        public readonly string CompilerToolchain;

        /// <summary>Field 2: compiler / runtime version string (e.g. the Mono runtime version at Stage 0). §4.8.3.</summary>
        public readonly string CompilerVersion;

        /// <summary>Field 3: target triple — an LLVM triple (IL2CPP) or the .NET RID (e.g. "win-x64") under Mono. §4.8.3.</summary>
        public readonly string TargetTriple;

        /// <summary>Field 4: IL2CPP version string, or the "MONO" sentinel for the Stage-0 Mono backend. §4.8.3.</summary>
        public readonly string Il2cppVersion;

        /// <summary>Field 5: runtime CSR/MXCSR denormals-are-zero bit. Stage-0 required value: false. §4.8.3.</summary>
        public readonly bool DenormalsAreZero;

        /// <summary>Field 6: runtime CSR/MXCSR flush-to-zero bit. Stage-0 required value: false. §4.8.3.</summary>
        public readonly bool FlushToZero;

        /// <summary>Field 7: rounding mode (0=NearestEven, 1=ToZero, 2=Upward, 3=Downward). Stage-0 required value: 0. §4.8.3.</summary>
        public readonly byte RoundingMode;

        /// <summary>Field 8: fp-contract mode (0=Off, 1=On, 2=Fast). Stage-0 required value: 0. §4.8.3.</summary>
        public readonly byte FpContractMode;

        /// <summary>Field 9: whether fused multiply-add is permitted in authoritative paths. Stage-0 required value: false. §4.8.3 / FR-CS-040.</summary>
        public readonly bool FmaEnabled;

        /// <summary>Field 10: -ffast-math / /fp:fast. Stage-0 required value: false. §4.8.3.</summary>
        public readonly bool FastMath;

        /// <summary>Field 11: lowest-common-denominator SIMD level; MUST equal the fingerprint's SimdFeatureLevel. §4.8.3.</summary>
        public readonly string SimdLevel;

        /// <summary>Constructs the §4.8.3 tuple. See the field docs for each field's Stage-0 required value.</summary>
        public FloatFlagTuple(
            string compilerToolchain,
            string compilerVersion,
            string targetTriple,
            string il2cppVersion,
            bool   denormalsAreZero,
            bool   flushToZero,
            byte   roundingMode,
            byte   fpContractMode,
            bool   fmaEnabled,
            bool   fastMath,
            string simdLevel)
        {
            CompilerToolchain = compilerToolchain ?? string.Empty;
            CompilerVersion   = compilerVersion   ?? string.Empty;
            TargetTriple      = targetTriple      ?? string.Empty;
            Il2cppVersion     = il2cppVersion     ?? string.Empty;
            DenormalsAreZero  = denormalsAreZero;
            FlushToZero       = flushToZero;
            RoundingMode      = roundingMode;
            FpContractMode    = fpContractMode;
            FmaEnabled        = fmaEnabled;
            FastMath          = fastMath;
            SimdLevel         = simdLevel ?? string.Empty;
        }

        /// <summary>
        /// Computes <c>floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ tuple))</c> per §4.8.3, returned as
        /// a 64-char lowercase hex string. Not on a hot path — a fingerprint is built once at match start.
        /// </summary>
        public string ComputeHash()
        {
            int size = DeterministicSimConstants.FIELD_WIDTH_DOMAIN_TAG
                     + 4 + Encoding.ASCII.GetByteCount(CompilerToolchain)
                     + 4 + Encoding.ASCII.GetByteCount(CompilerVersion)
                     + 4 + Encoding.ASCII.GetByteCount(TargetTriple)
                     + 4 + Encoding.ASCII.GetByteCount(Il2cppVersion)
                     + 1 + 1  // denormalsAreZero, flushToZero
                     + 1 + 1  // roundingMode, fpContractMode
                     + 1 + 1  // fmaEnabled, fastMath
                     + 4 + Encoding.ASCII.GetByteCount(SimdLevel);

            byte[] preimage = new byte[size];
            int o = 0;
            CanonicalSerializer.WriteU8(preimage, ref o, DeterministicSimConstants.DOMAIN_TAG_ENV_FP);
            CanonicalSerializer.WriteString(preimage, ref o, CompilerToolchain);
            CanonicalSerializer.WriteString(preimage, ref o, CompilerVersion);
            CanonicalSerializer.WriteString(preimage, ref o, TargetTriple);
            CanonicalSerializer.WriteString(preimage, ref o, Il2cppVersion);
            CanonicalSerializer.WriteBool(preimage, ref o, DenormalsAreZero);
            CanonicalSerializer.WriteBool(preimage, ref o, FlushToZero);
            CanonicalSerializer.WriteU8(preimage, ref o, RoundingMode);
            CanonicalSerializer.WriteU8(preimage, ref o, FpContractMode);
            CanonicalSerializer.WriteBool(preimage, ref o, FmaEnabled);
            CanonicalSerializer.WriteBool(preimage, ref o, FastMath);
            CanonicalSerializer.WriteString(preimage, ref o, SimdLevel);

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(preimage, 0, o);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-19 | —      | Initial implementation. The §4.8.3 11-field float-flag tuple + |
// |         |            |        | ComputeHash() live-host hasher (ERR-016-006 Option A). Golden  |
// |         |            |        | vector pinned in DeterministicSimTests.                        |
#endregion
