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

## Certified capture (host-blocked)

The certified golden read (`0x1F80` on Windows 11 / Unity 6000.4.9f1 / Mono / x64)
can only be captured and signed off on the pinned host — see
`docs/tracking/cert-run-runbook.md`. Everything in this directory and the two
managed files above is buildable and CI-green now; that golden capture folds into
the next pinned-host cert run.
