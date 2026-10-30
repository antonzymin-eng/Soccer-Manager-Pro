// File:     src/deterministic-sim/CngSha256.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.3 (DigestVersion=1 → SHA-256), Code Standards #20
// Purpose:  One-shot SHA-256 through the Windows CNG provider (bcrypt.dll), for the per-tick snapshot
//           digest. Gate-J J-05 / perception-shared-pass-design.md P1b: Unity's Mono maps
//           SHA256.Create() to the pure-managed SHA256Managed, which made the 35 KB per-tick digest
//           the largest single cost of a match tick on the pinned host. SHA-256 is fully specified, so
//           the provider changes cost only, never the 32 digest octets; a known-answer self-test
//           against the managed implementation gates use, and every host without CNG (the Linux gate)
//           keeps the managed path.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// SHA-256 over a byte range via <c>BCryptHash</c> with the <c>BCRYPT_SHA256_ALG_HANDLE</c>
    /// pseudo-handle: no provider to open or close, no per-call object, no managed allocation, and
    /// safe to call from any thread. <see cref="IsAvailable"/> is false wherever the library or the
    /// pseudo-handle is missing (non-Windows, Windows before 10) or the self-test disagrees with the
    /// managed SHA-256; callers then use the managed implementation. Deterministic Simulation #16 §3.2.3.
    /// </summary>
    internal static class CngSha256
    {
        /// <summary>Windows CNG library; resolves to <c>%SystemRoot%\System32\bcrypt.dll</c>.</summary>
        private const string LibraryName = "bcrypt";

        // BCRYPT_SHA256_ALG_HANDLE (bcrypt.h, Windows 10+). A fixed pseudo-handle, not a resource.
        private static readonly IntPtr s_sha256AlgorithmHandle = new IntPtr(0x00000041);

        // NTSTATUS STATUS_SUCCESS.
        private const int StatusSuccess = 0;

        [DllImport(LibraryName, EntryPoint = "BCryptHash", CallingConvention = CallingConvention.Winapi)]
        private static extern int BCryptHash(
            IntPtr hAlgorithm, IntPtr pbSecret, int cbSecret,
            [In] byte[] pbInput, int cbInput, [Out] byte[] pbOutput, int cbOutput);

        /// <summary>
        /// True once, per process, the CNG path has loaded and reproduced the managed SHA-256 on the
        /// self-test inputs. Evaluated at first use (init time, never re-probed on the tick path).
        /// </summary>
        internal static readonly bool IsAvailable = Probe();

        /// <summary>
        /// Hashes <paramref name="input"/>[0, <paramref name="count"/>) into the first
        /// <see cref="DeterministicSimConstants.SHA256_BYTES"/> octets of <paramref name="digest"/>.
        /// Returns false (digest unspecified) when CNG reports failure; the caller then hashes with
        /// the managed implementation. Call only when <see cref="IsAvailable"/> is true.
        /// </summary>
        internal static bool TryHash(byte[] input, int count, byte[] digest)
        {
            return BCryptHash(s_sha256AlgorithmHandle, IntPtr.Zero, 0, input, count,
                digest, DeterministicSimConstants.SHA256_BYTES) == StatusSuccess;
        }

        // Known-answer gate: CNG must reproduce the managed SHA-256 on empty input, the padding
        // boundaries (55/56/63/64/65 bytes) and a snapshot-sized buffer, or it is never used.
        private static bool Probe()
        {
            // Never probe elsewhere: an unrelated library named bcrypt (a password-hashing libbcrypt)
            // could resolve on another OS.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return false; }

            int[] lengths = { 0, 3, 55, 56, 63, 64, 65, 1000, 35261 };
            var input = new byte[35261];
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(i * 31 + 7);
            }
            var native = new byte[DeterministicSimConstants.SHA256_BYTES];

            try
            {
                using (SHA256 managed = SHA256.Create())
                {
                    foreach (int length in lengths)
                    {
                        if (!TryHash(input, length, native)) { return false; }
                        byte[] expected = managed.ComputeHash(input, 0, length);
                        for (int i = 0; i < expected.Length; i++)
                        {
                            if (expected[i] != native[i]) { return false; }
                        }
                    }
                }
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-21 | —      | Initial creation (Gate-J J-05 / P1b): CNG one-shot SHA-256 for  |
// |         |            |        | the per-tick snapshot digest, gated by a known-answer self-test |
// |         |            |        | against the managed SHA-256; managed path everywhere else.      |
#endregion
