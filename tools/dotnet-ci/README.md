# tools/dotnet-ci — Non-Certifying Linux Compile/Test Gate

> **Created:** June 12, 2026
> **Purpose:** Compile the entire `src/` tree and execute every NUnit suite under
> plain `dotnet test` on Linux CI — without Unity.

## Why this exists

By June 2026, **seven consecutive specs** had shipped a structurally dead build
surface that no tool ever checked: `PassMechanicsTests` (stray `}`, CS1022, dead
since v1.1), First Touch ERR-004 (same class), the Decision Tree production
assembly (static calls to instance executors), the deterministic-sim test
assembly (missing `InternalsVisibleTo`), and more. Unit suites "verified" claims
while being uncompilable. The certification platform (Windows / Unity
6000.4.9f1, target pin as of `certification-platform.md` v1.3 — recertification
pending, superseding the 2022.3.62f1 tuple this gate was originally written
against) governs *determinism certification*, not smoke-level "does it
compile and do the tests run" — and the codebase is, by design, pure
deterministic C# whose only engine surface is `Vector2`/`Vector3`, `Mathf`,
`Debug`, `ProfilerMarker`/`Profiler`, and `LogAssert`. A ~6-type shim closes the
gap.

**On its first execution (June 12, 2026) the gate found:** 18 files importing
`ProfilerMarker` from the wrong namespace; the EventBus/EventBusStub
constraint-only overload triple (CS0111 — illegal in any C#; ERR-017-002, spec
patched); `File.Move(…, overwrite:)` absent from Unity's netstandard2.1 surface
(SaveManager); wrong enum-member casing (ShotExecutor); a missing
`using System;` (CoverShadowSelector); an `int?`→`int` mismatch
(GoalkeeperMechanics); the sixth stray-brace dead suite (ShotMechanicsTests);
51 `internal` test methods NUnit cannot run (DefensiveAITests); NUnit API
misuse in two suites; an `EventRegistry` static-init ordering fragility; and
four fabricated SipHash reference vectors. Then 1,165 tests ran for the first
time: 30 genuine model/expectation failures were quarantined into
`known-failures.txt` (tracked in `docs/tracking/dotnet-ci-quarantine.md`).

## What it is NOT

**Not a determinism certification.** Bit-exactness, FR-DS-009-GATE, perf gates,
and golden-digest pins are certified ONLY on the pinned host in
`docs/tracking/certification-platform.md` (target tuple as of v1.3: Windows 11 /
Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker /
DAZ+FTZ+fp-contract+FMA off — status ⏳ Recert required, not yet certified).
This gate proves
the tree *compiles* and the suites *execute and pass*; float results on Linux
x64 under .NET 8 are expected to agree for the operations used, but no digest
produced here is authoritative.

## Layout

| Path | Role |
|---|---|
| `generate_projects.py` | Maps every `src/**/*.asmdef` → `*.gen.csproj` (gitignored; asmdefs stay the single source of truth) + `TacticalDirector.gen.sln`. Production TFM `netstandard2.1` (Unity's BCL surface), tests `net8.0`; `LangVersion` 9.0 (Unity 2022.3 C# level); `AssemblyName` = asmdef name so `InternalsVisibleTo` resolves; `DEVELOPMENT_BUILD` defined so FR-CS-031-gated emits compile and `LogAssert.Expect` stays meaningful. |
| `UnityShim/` | `UnityEngine` shim: `Vector2`/`Vector3` (Unity-exact approximate `==`, exact `Equals`, normalize threshold), `Mathf` (Unity NaN semantics — the project NaN-gate pattern depends on them — and round-half-to-even), `Debug` + `LogType` + `ShimLog` event spine, no-op `Profiler`/`ProfilerMarker`. |
| `UnityShim.TestTools/` | `LogAssert` with Unity Test Framework parity (unmet expectation fails the test; unexpected Error/Assert/Exception log fails the test) + `LogAssertVerifyAttribute` (assembly-level NUnit `ITestAction`). |
| `LogAssertVerifyAssemblyInfo.cs` | Linked into every generated test project — applies the log contract to all tests. |
| `known-failures.txt` | Machine-readable quarantine (shrinking-only). See `docs/tracking/dotnet-ci-quarantine.md`. |
| `run-gate.sh` | The gate: generate → restore → build (errors fail) → `dotnet test` excluding quarantine (any failure fails) → report-only run of the quarantined set. |

## Running locally

```bash
bash tools/dotnet-ci/run-gate.sh
```

Requires the .NET 8 SDK and Python 3 (stdlib only). Generated `*.gen.csproj`,
`*.gen.sln`, `bin/`, `obj/` are gitignored — never commit them.

## Shim fidelity rules

- Shim members replicate **documented Unity semantics exactly** where the
  codebase depends on them (Vector approximate `==`, `Mathf.Max/Min/Clamp01`
  NaN propagation, `RoundToInt` banker's rounding, `Normalize` 1e-5 threshold).
- The shim must stay **strictly Unity-shaped**: never add a member Unity does
  not have, and never add a namespace alias to make broken code compile — a
  compile error here that Unity would also produce is the gate working
  (e.g. `ProfilerMarker` was deliberately NOT added to `UnityEngine.Profiling`).
- When .NET 8 and Unity's netstandard2.1 BCL disagree, the **netstandard2.1
  surface wins** (that is why production targets it).

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-06-12 | — | Initial gate: shim + generator + runner + quarantine; first-ever full suite execution. |
| 1.1 | 2026-07-13 | — | Certification-pin citations updated to the `certification-platform.md` v1.3 target tuple (Unity 6000.4.9f1, DX11) — recert pending, not yet certified. The `generate_projects.py` / `UnityShim` technical claims about Unity's actual `netstandard2.1` BCL surface and `LangVersion 9.0` C# level are UNCHANGED and unverified against Unity 6 — see root `CLAUDE.md` OPEN ISSUES. |
