# Cert Run — §4.8.2 Runtime MXCSR Float-Mode Live Read

**Date:** 2026-07-22
**Gate:** Deterministic Simulation #16 §4.8.2 (runtime float-mode validation), §4.8.3 fields 5/6/7 (DAZ/FTZ/RC)
**Result:** ✅ PASS — live MXCSR float-mode fields match the Stage-0 determinism pin.

## Host tuple (pinned cert platform)

| Field | Value |
|-------|-------|
| OS | Windows 11 |
| Unity | 6000.4.9f1 |
| Graphics API | DX11 |
| Scripting backend | Mono |
| Arch / SIMD | x64 / SSE4.2 |
| Native compiler | MSVC (cl.exe) — version: 'Microsoft (R) C/C++ Optimizing Compiler Version 19.44.35213 for x64' |
| Plugin build | `cl /O2 /LD mxcsr_query.c /Fe:td_mxcsr.dll` |
| Plugin location | `src/deterministic-sim/native/td_mxcsr.dll` (loaded in-Editor via the Assets/Scripts junction) |

## Live read

Raw MXCSR: **`0x1FBF`**

| Field | Bits | Value | Stage-0 pin | Match |
|-------|------|-------|-------------|-------|
| DAZ (Denormals Are Zero) | 6 | 0 (False) | off | ✅ |
| FTZ (Flush To Zero) | 15 | 0 (False) | off | ✅ |
| RC (Rounding Control) | 13–14 | 0 (nearest-even) | 0 | ✅ |

The low 6 bits (`0x3F`) are sticky exception **status** flags accumulated by prior FP
ops in the session — status, not mode configuration. The §4.8.2 pin masks them out
(`MxcsrValidator.MatchesStage0Pin`), so the certified assertion is on the DAZ/FTZ/RC
mode fields above, all of which match the Stage-0 pin. `ValidateStage0FloatMode()`
returned `Validated` (native probe available + pin satisfied).

## Evidence

- Test: `TacticalDirector.DeterministicSim.MxcsrValidatorTests` (EditMode) — all pass,
  `ValidateStage0FloatMode_NoNativeShim_ReportsUnavailableNotFailure` resolved to `Validated`.
- Golden raw value captured via a throwaway probe (deleted post-capture).

## Sign-off

Platform Certification owner sign-off: recorded via PR merge (Spec #16 §1.7).