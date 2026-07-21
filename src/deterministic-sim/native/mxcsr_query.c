// File:     src/deterministic-sim/native/mxcsr_query.c
// Created:  2026-07-21
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8.2 (runtime float-mode validation), §4.8.3 (float-flag tuple)
// Purpose:  Native shim exposing the live x64 SSE control/status register (MXCSR) to managed code.
//           .NET / Mono has no managed intrinsic for STMXCSR, so the runtime MXCSR gate (§4.8.2)
//           reads it through this P/Invoke entry point (see MxcsrNative.cs / MxcsrValidator.cs).
//           MXCSR is per-OS-thread; the caller MUST invoke on the thread that ticks the sim.
//
// Build (pinned Windows/Unity cert host — Stage-0 tuple, MSVC x64):
//     cl /O2 /LD mxcsr_query.c /Fe:td_mxcsr.dll
//   then place td_mxcsr.dll in Assets/Plugins/x86_64/ so the x64 player loads it.
// Build (GCC/Clang, for a Linux/dev shared object — non-certifying):
//     cc -O2 -shared -fPIC -o libtd_mxcsr.so mxcsr_query.c
//
// _mm_getcsr() compiles to a single STMXCSR instruction on x86/x64 under MSVC, GCC and Clang.

#include <stdint.h>
#include <xmmintrin.h>   /* _mm_getcsr */

#if defined(_WIN32)
#define TD_EXPORT __declspec(dllexport)
#else
#define TD_EXPORT __attribute__((visibility("default")))
#endif

/* Returns the raw 32-bit MXCSR value of the CALLING thread. Decoded managed-side:
 *   bit  6      = DAZ (Denormals Are Zero)      — Stage-0 pin: 0
 *   bit 15      = FTZ (Flush To Zero)           — Stage-0 pin: 0
 *   bits 13..14 = RC  (Rounding Control)        — Stage-0 pin: 0 (round-to-nearest-even)
 */
TD_EXPORT uint32_t td_get_mxcsr(void)
{
    return _mm_getcsr();
}
