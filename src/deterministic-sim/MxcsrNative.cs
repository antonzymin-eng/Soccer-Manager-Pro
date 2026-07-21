// File:     src/deterministic-sim/MxcsrNative.cs
// Created:  2026-07-21
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8.2, Code Standards #20
// Purpose:  P/Invoke boundary to the native MXCSR shim (native/mxcsr_query.c). .NET / Mono has no
//           managed intrinsic for STMXCSR, so the §4.8.2 runtime float-mode gate reads the live
//           SSE control/status register through this entry point. Init-time only (queried once at
//           match start / restore step 0) — never on the 60 Hz hot path.

using System;
using System.Runtime.InteropServices;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Managed interop to the native <c>td_mxcsr</c> library (see <c>native/mxcsr_query.c</c>).
    /// Reads the live 32-bit MXCSR of the calling OS thread — MXCSR is per-thread, so the query
    /// MUST run on the thread that ticks the sim (the Stage-0 single-worker config satisfies this
    /// trivially). Deterministic Simulation #16 §4.8.2.
    /// </summary>
    internal static class MxcsrNative
    {
        /// <summary>Native library base name; resolves to <c>td_mxcsr.dll</c> / <c>libtd_mxcsr.so</c>.</summary>
        private const string LibraryName = "td_mxcsr";

        [DllImport(LibraryName, EntryPoint = "td_get_mxcsr", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint TdGetMxcsr();

        /// <summary>
        /// Attempts to read the calling thread's MXCSR. Returns <c>true</c> with the raw register value
        /// when the native library is present and loadable; <c>false</c> when it is absent (no compiled
        /// plugin on this host — the Linux CI gate, a dev checkout, a build without the plugin). A
        /// <c>false</c> result is "probe unavailable", NOT a float-mode failure: the gate stays inert
        /// where it cannot run and enforces only where the library actually loads (the pinned cert host).
        /// </summary>
        internal static bool TryQuery(out uint mxcsr)
        {
            try
            {
                mxcsr = TdGetMxcsr();
                return true;
            }
            catch (DllNotFoundException)
            {
                mxcsr = 0u;
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                mxcsr = 0u;
                return false;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-21 | —      | Initial implementation. P/Invoke boundary to the native       |
// |         |            |        | td_mxcsr shim for the §4.8.2 runtime float-mode gate. Absent   |
// |         |            |        | library ⇒ TryQuery false (probe unavailable, not a failure).   |
#endregion
