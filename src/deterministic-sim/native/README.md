# Native MXCSR shim (`td_mxcsr`)

Backs the §4.8.2 **runtime float-mode validation** gate. .NET / Mono exposes no
managed intrinsic for the `STMXCSR` instruction, so `MxcsrValidator` reads the
live SSE control/status register through this tiny P/Invoke library.

- Source: [`mxcsr_query.c`](./mxcsr_query.c) — one exported function, `td_get_mxcsr()`, returning the raw 32-bit MXCSR of the calling thread.
- Managed interop: [`../MxcsrNative.cs`](../MxcsrNative.cs) (`[DllImport("td_mxcsr")]`).
- Decode + policy: [`../MxcsrValidator.cs`](../MxcsrValidator.cs).

## What it validates

Only the three MXCSR fields the determinism pin (§4.8.3, `FloatFlagTuple`
fields 5/6/7) constrains:

| Bits | Field | Stage-0 pinned value |
|------|-------|----------------------|
| 6 | DAZ (Denormals Are Zero) | off (0) |
| 15 | FTZ (Flush To Zero) | off (0) |
| 13–14 | RC (Rounding Control) | 0 = round-to-nearest-even |

`fp-contract` and `FMA` from the pin are **compile-time codegen** decisions, not
MXCSR bits — the `floatModelHash` tuple covers those; this query cannot see them.
On x64 all scalar `float`/`double` math uses SSE, so the x87 control word is not
consulted.

## Building

Pinned Windows/Unity cert host (Stage-0 tuple, MSVC x64):

```
cl /O2 /LD mxcsr_query.c /Fe:td_mxcsr.dll
```

Place `td_mxcsr.dll` in `Assets/Plugins/x86_64/` so Unity loads it for the x64 player.

GCC/Clang shared object (Linux/dev, non-certifying):

```
cc -O2 -shared -fPIC -o libtd_mxcsr.so mxcsr_query.c
```

## Availability policy (why the gate stays green off-host)

`MxcsrNative.TryQuery` catches `DllNotFoundException` / `EntryPointNotFoundException`
and reports the probe **unavailable**. On any host without the compiled library —
the Linux `dotnet-ci` gate, a dev checkout, a build with no plugin — the validator
is a no-op (`ProbeStatus.Unavailable`) rather than a failure. It only *enforces*
where the library is actually loadable, i.e. the pinned cert host. This is
defense-in-depth over the already-certified pin (the determinism-KAT run is the
proof that the bits are exact); it does not *replace* that proof.

## Certified capture — LANDED July 22, 2026

The certified golden read was captured and signed off on the pinned host
(Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64): raw MXCSR `0x1FBF`; the
DAZ/FTZ/RC mode fields match the Stage-0 pin (the low 6 bits are sticky
exception status flags the §4.8.2 gate correctly masks out); `ValidateStage0FloatMode()`
returned `Validated`. Evidence:
`docs/specs/deterministic-sim/cert-runs/mxcsr-live-mode-cert-2026-07-22.md`.
The `td_mxcsr.dll` plugin committed in this directory is the one built and used
for that capture. No host-block remains on this gate.
