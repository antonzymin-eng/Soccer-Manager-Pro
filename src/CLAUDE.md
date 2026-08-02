# src/CLAUDE.md — Tactical Director Coding Guide

> **Created:** May 19, 2026
> **Change log:** `docs/tracking/CHANGELOG-src.md` — the `**Last Updated:**` chain and the `VERSION HISTORY` table.
> **File tree:** `docs/tracking/src-tree.md` — the annotated `src/` tree (orientation only; `file-manifest.md` is authoritative).
> **Purpose:** Concrete coding rules for any AI agent or developer writing C# source code in this project. Covers file naming, constant catalogues, Unity project structure, and build/test commands. Cites Spec #20 (Code Standards & Style Guide) as the source for every convention here. Read the root `CLAUDE.md` first — this file supplements it, not replaces it.

---

## BEFORE YOU WRITE ANY CODE

1. Read the root `CLAUDE.md` completely.
2. Read Spec #20 (`docs/specs/code-standards/`) for the full rule set with rationale.
3. Read the `§4` (Architecture) file of the spec you are implementing.
4. Check `docs/specs/SPEC_INDEX.md` to confirm the spec's status is `APPROVED`.

---

## UNITY PROJECT STRUCTURE

```
src/
├── <one folder per assembly>/      ← one .asmdef per folder (FR-CS-055)
│   ├── <Assembly>Constants.cs      ← the folder's constant catalogue
│   ├── *.cs
│   └── tests/                      ← its own .asmdef, testPlatforms: [EditMode]
└── CLAUDE.md                       ← you are here
```

**31 assembly folders.** The full annotated tree lives in
`docs/tracking/src-tree.md`; the authoritative inventory is
`docs/tracking/file-manifest.md`. Do not maintain a third copy here.

**One folder per assembly. One `.asmdef` per folder.** Folder names usually match
`docs/specs/` but not always — see the assembly map in the root `CLAUDE.md`, and do
not infer the spec mapping from a folder name.

**One folder per spec. One `.asmdef` per folder. Folder names match `docs/specs/` exactly.**

> **Note on `.asmdef` coverage:** Every spec folder listed above requires a
> `.asmdef` file (e.g., `pressing-ai/pressing-ai.asmdef`). Only a subset is shown
> in the tree for brevity. See each spec's `§4` (Architecture) file for the exact
> `.asmdef` reference list. GUIDs are blocked on Unity project initialization (see
> "WHAT IS NOT HERE YET").
>
> **Test assemblies:** Every `tests/` subfolder requires its own `.asmdef` with
> `testPlatforms: [EditMode]` (or as specified per Spec #19 §7.5 D2) and a reference
> to the parent spec's `.asmdef`. Test assemblies are excluded from production builds
> via platform filtering. Only the expanded spec folders in the tree above show the
> `.asmdef` entry; all `tests/` subfolders follow the same pattern.

### Assembly Layer Taxonomy

The authoritative layer taxonomy is Spec #20 §3.5.2. The three layers and their
members are reproduced here verbatim — do not infer layer membership from folder
order or spec number.

| Layer | Assemblies |
|---|---|
| **Physics** | ball-physics, agent-movement, collision-system, first-touch, pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics |
| **Mechanics** | positioning-ai, pressing-ai, defensive-ai, attacking-ai |
| **AI** | decision-tree, perception-system |
| **UI** | (Stage 1+ — not yet specified) |

The `deterministic-sim` and `event-system` assemblies are cross-cutting foundations
referenced by all layers (not members of any single layer).

The following assemblies are **infrastructure-only** and are NOT members of any
gameplay layer. Game-layer code (Physics / Mechanics / AI) MUST NOT import them
at runtime:

| Assembly | Role |
|---|---|
| `project-constants` | Constants shared across ≥ 2 spec assemblies; read-only by all |
| `performance-optimization` | Trace pipeline only (Spec #18 KD-3); no game-loop types |
| `testing-strategy` | CI orchestration tooling only (Spec #19); no game-loop types |
| `code-standards` | Governance only (Spec #20); no runtime types |


> ⚠️ **This table is out of date and is not the current assembly index.**
> It accounts for 19 of the 31 assembly folders now in `src/` — the 17 named in the
> layer tables plus `deterministic-sim` and `event-system`, covered as cross-cutting
> foundations in the paragraph above.
>
> **Unlisted (12):** `living-world`, `match-analytics`, `match-client-core`,
> `match-client-unity`, `match-client-web`, `match-engine`, `match-viewer`,
> `player-database`, `player-progression`, `season-save`, `tactical-instructions`,
> `ui-framework`.
>
> **Listed but absent from `src/`:** `code-standards` (Spec #20 is a style guide, not a
> coded assembly).
>
> The table is reproduced verbatim from Spec #20 §3.5.2, which is the authority on layer
> membership — so the fix is a back-prop to that spec, not an edit here. Assigning these
> 12 to layers is a design decision requiring owner sign-off, not something to infer from
> folder names. Until it lands, use the **assembly map in the root `CLAUDE.md`** as the
> current index. The Reference Direction rule below is unaffected and still binding.

### Reference Direction

**AI depends on Mechanics. Mechanics depends on Physics. Never the reverse.**

```
project-constants  (read-only by all assemblies)

Physics  ←  Mechanics  ←  AI  ←  UI
```

`←` means "is referenced by" — `A ← B` means B depends on A (B imports from A).
The AI assembly imports types from Mechanics, which imports types from Physics.
A Physics assembly MUST NOT import from Mechanics or AI; a Mechanics assembly MUST NOT
import from AI. These prohibited import directions are enforced as build errors via
`.asmdef` reference declarations (FR-CS-046).

For upward event notification (e.g., a physics event consumed by AI), use a struct
event on the event bus — no direct assembly reference (FR-CS-047).

For the specific `.asmdef` references each assembly declares, read that spec's `§4`
(Architecture) file. Do not infer the intra-layer dependency chain from this document.

---

## BUILD AND TEST COMMANDS

> **Note:** The certification platform is now pinned in `docs/tracking/certification-platform.md` v1.3 (Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker / deterministic compiler flags) but is **⏳ RECERT REQUIRED** — no certification run has executed against the Unity-6 tuple yet (`cert-run-runbook.md` P1/P2). The batch-mode command below is defined but requires a Unity host to execute; the Linux path (`dotnet-ci` gate) is the day-to-day compile/test gate.

**Format check (pre-commit gate):**
```bash
dotnet format --verify-no-changes
```

**Build with warnings-as-errors:**
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**Run tests:**
```bash
dotnet test
```

**Linux compile/test gate (non-certifying; runs in CI on every push):**
```bash
bash tools/dotnet-ci/run-gate.sh
```
Generates plain .NET projects from the asmdefs (production `netstandard2.1` —
Unity 2022.3's BCL surface; tests `net8.0`), compiles the whole tree against the
`tools/dotnet-ci/UnityShim` UnityEngine shim, and runs every NUnit suite minus
the quarantine in `tools/dotnet-ci/known-failures.txt` (tracked in
`docs/tracking/dotnet-ci-quarantine.md`). Any compile error or non-quarantined
test failure fails the gate. NOT a determinism certification — see
`docs/tracking/certification-platform.md`.

**Unity batch-mode test run (pinned host — FR-PO-052 certified perf capture):**
```bash
# Run on the pinned certification host (Windows 11 / Unity 6000.4.9f1 / DX11 / Mono),
# after creating the Assets/Scripts junction into src/ (see Assets/README.md).
# TD_PERF_RUN_COUNT=100 (= BaselineSampleCount) drives the certified capture; unset ⇒ CI-fast 2.
Unity -batchmode -runTests -projectPath . \
      -testPlatform EditMode \
      -testFilter "TacticalDirector.MatchEngine.MatchEngineCapstonePerfHarnessTests" \
      -testResults ./perf-results.xml -logFile -
```
The test (`MatchEngineCapstonePerfHarnessTests`) drives the real `MatchEngine`
capstone through `MatchEngineCapstonePerfHarness.CaptureBaseline` and logs the
per-tick `p50`/`p99` via `TestContext` for the operator to transcribe into the
`.cert.md` corpus entry (`cert-run-runbook.md` Step 3). The same test runs on the
Linux `dotnet-ci` gate (non-certifying) as the harness's compile+execute proof.
This command requires a Unity host — it is **not** runnable from the Linux gate.

**Stage 0 verification:** Manual code review against Spec #20 §5.4 checklist (7 categories, 73 FRs). Static analysis tooling (Roslyn analyzers, BannedSymbols.txt, `.editorconfig`) activates at Stage 1.

---

## FILE NAMING

- One public type per file. Filename must match the type name exactly (case-sensitive).
  - `BallState.cs` contains `public struct BallState`
  - `BallPhysicsConstants.cs` contains `public static class BallPhysicsConstants`
- Tests live in a sibling `tests/` folder under the same spec folder.
- No version suffixes in filenames. Git tracks history.

---

## NAMING CONVENTIONS

| Identifier | Convention | Example |
|---|---|---|
| Types, methods, properties, events | PascalCase | `BallState`, `ApplyKick` |
| Local variables, parameters | camelCase | `deltaTime`, `agentId` |
| Private instance fields | `_camelCase` | `_clock`, `_agentCount` |
| Private static fields | `s_camelCase` | `s_updateMarker`, `s_runTickMarker` |
| `[FIXED]` constants | `ALL_CAPS` | `BALL_RADIUS`, `DRAG_COEFFICIENT` |
| All other constants (`[GT]`, `[EST]`, `[DERIVED]`, `[CROSS]`) | PascalCase | `MaxSubsteps`, `TerminalVelocity` |
| Interfaces | `I` prefix + PascalCase | `IEventBus`, `ICollisionConsumer` |
| Assembly names / namespaces | `TacticalDirector.<SpecName>` | `TacticalDirector.BallPhysics` |

No Hungarian notation. No other prefix/suffix schemes (FR-CS-001/002).

**`var` policy (FR-CS-013):** Use `var` only when the type is immediately obvious from the RHS. `var state = new BallState()` is clear. `var result = Compute();` is not — write the explicit type.

---

## STYLE

**Indentation:** 4 spaces. Tabs are prohibited (FR-CS-011). Enforced by `.editorconfig` at Stage 1.

**Brace style:** Allman — opening brace on its own line (FR-CS-012).

```csharp
// COMPLIANT — FR-CS-012
public void Update(ref BallState state)
{
    if (state.IsGrounded)
    {
        ApplyFriction(ref state);
    }
}

// VIOLATION — K&R brace style
public void Update(ref BallState state) {
    if (state.IsGrounded) {
        ApplyFriction(ref state);
    }
}
```

**Explicit access modifiers:** Every type, method, property, field, and event declaration MUST carry an explicit access modifier. Relying on C#'s implicit `private` or `internal` is prohibited (FR-CS-014).

---

## NAMESPACES

One namespace per assembly. Sub-folders do not introduce sub-namespaces (FR-CS-007).

```csharp
// File: src/ball-physics/simulation/DragIntegrator.cs
namespace TacticalDirector.BallPhysics   // flat — no sub-namespace
{
    internal readonly struct DragIntegrator { … }
}

// VIOLATION:
namespace TacticalDirector.BallPhysics.Simulation { … }
```

---

## CONSTANT CATALOGUES

Every constant lives in `<SpecName>Constants.cs`. No literals in formula or system files (FR-CS-016).

**Naming:** PascalCase folder name + `Constants.cs`

| Spec folder | Catalogue file |
|---|---|
| `ball-physics/` | `BallPhysicsConstants.cs` |
| `agent-movement/` | `AgentMovementConstants.cs` |
| `collision-system/` | `CollisionSystemConstants.cs` |
| *(all specs)* | `<SpecName>Constants.cs` |
| *(cross-spec)* | `project-constants/ProjectConstants.cs` |

**Region order inside every catalogue (most-immutable first):**

```csharp
#region Fixed      // [FIXED]   → public const float BALL_RADIUS = 0.11f;
#region Derived    // [DERIVED] → public static readonly float TerminalVelocity = Mathf.Sqrt(GRAVITY / DRAG_COEFFICIENT);
#region Cross      // [CROSS]   → public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;
#region GT         // [GT]      → public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
#region EST        // [EST]     → public static readonly float LiftCoefficient = 0.35f; // TODO: validate
```

Omit a region entirely if the spec has no constants with that tag. Empty regions are prohibited.

**Region name convention:** The first three region names use Title Case (`Fixed`, `Derived`, `Cross`). `GT` and `EST` match their tag names exactly since those are already **all-caps abbreviations**. Do not use ALL_CAPS (`FIXED`) or lowercase for region names.

**`[DERIVED]` constants:** The XML doc must include the tag, the formula, and the source constants (FR-CS-021). Substitute actual formula references (FM-NNN, §x.y) from the implementing spec:

```csharp
#region Derived
/// <summary>
/// [DERIVED] Terminal velocity (m/s) at which drag force equals gravity.
/// Formula: sqrt(GRAVITY / DRAG_COEFFICIENT). FM-NNN. Ball Physics #1 §3.x.
/// Source constants: BallPhysicsConstants.GRAVITY, BallPhysicsConstants.DRAG_COEFFICIENT.
/// </summary>
public static readonly float TerminalVelocity =
    Mathf.Sqrt(BallPhysicsConstants.GRAVITY / BallPhysicsConstants.DRAG_COEFFICIENT);
```

**`[GT]` loading mechanism (FR-CS-019):** The loader landed June 30, 2026 as
`TacticalDirector.ProjectConstants.GameplayConfig` + `GameplayConfigFileLoader`
(`src/project-constants/`). `GameplayConfigFileLoader.Parse(text)` reads the on-disk
text format — line-oriented case-insensitive `key = value` pairs under `[section]`
headers (the section is the owning catalogue, conventionally its spec folder name),
`#` comments, blank lines ignored — into an immutable `GameplayConfig`. A catalogue
reads each constant at boot via `GetFloat/GetInt/GetBool/GetString(section, key, fallback)`:

```csharp
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;
// …
#region GT
/// <summary>[GT] Maximum physics substeps per frame. Config key [ball-physics] MaxSubsteps. Code Standards #20 §3.2.3.</summary>
public static readonly int MaxSubsteps = Config.GetInt("ball-physics", "MaxSubsteps", 8);
```

Contract: an **absent** key returns the supplied design-time fallback, so an empty/partial
config file leaves every constant at today's baseline (behaviour-neutral); a **present** key
whose value does not parse to the requested type throws `FormatException` (a config typo fails
loud at boot). `GameplayConfig` is immutable and **constructor-injected** (never a static mutable
singleton — stays clear of FR-CS-051..054); getters run once at boot, never on the 60 Hz path. The
text grammar is a human-authoring format, NOT a determinism-pinned wire format — only the loaded
values feed the sim, so a future binary/richer grammar is a pure parser swap leaving the catalogues
untouched (the #19 `ScenarioIndex` / #21 `TeamTacticFileLoader` precedent).

**Boot-sequencing resolution (`GameplayConfigHolder`, landed 2026-06-30):** a `[GT]` field above is
`public static readonly` — it is assigned in the catalogue's own static constructor, a point no
ordinary constructor-injection call site can reach (there is no object yet to inject into).
`src/project-constants/GameplayConfigHolder.cs` is the single explicit binding point: a composition
root calls `GameplayConfigHolder.Bind(config)` once, before referencing any `[GT]` catalogue; every
catalogue's `Config.GetX(...)` call reads `GameplayConfigHolder.Config`. Until `Bind` is called,
`Config` resolves to `GameplayConfig.Empty`, so an unbound process (any test today; Stage 0 before a
composition root exists) is behaviour-neutral — every constant keeps its literal fallback. The FIRST
read of `Config` locks the binding; a `Bind` call after that point throws `InvalidOperationException`
instead of silently leaving whichever catalogue read first stuck on default values forever — the
ordering hazard fails loud at the moment it would otherwise become an undetectable bug. This is a
one-shot boot-time wire-up, not the banned static-mutable-singleton pattern (FR-CS-051..054 targets
hidden, repeatedly-mutated ambient *game* state); it is the same boundary the project already accepts
for `EventBusRegistrar.Initialize()` / `EventRegistry.EnsureInitialized()`. No `MatchEngine` call site
calls `Bind` yet — wiring an actual on-disk config load into match-engine boot is a follow-up.

**Migration status:** 509 of the 520 `[GT]` declarations the root `CLAUDE.md` OPEN ISSUES entry
tracked are migrated (17 catalogues; the `using static GameplayConfigHolder;` import + the
`Config.GetX(...)` call shown above). Two categories are explicitly carved out, left as literals
with their original `// TODO:` marker:
- **11 array/table-valued `[GT]` constants** (all in `TacticalInstructionsConstants.cs` — e.g.
  `MentalityRiskMult`, `TempoActionBias`) — `GameplayConfig` has no array getter, and these tables
  carry their own cross-cell invariant tests (`BalancePassInvariantsTests` — identity-row exactness,
  monotonicity) that only run against compile-time literals; making them config-overridable without
  an equivalent runtime invariant check would let a config file silently violate the pinned §5.6/G2
  balance pass. Needs its own design pass, not a mechanical `GetFloat` call.
- `decision-tree` / `living-world` / `positioning-ai` / `pressing-ai` carry `[GT]` constants that
  never picked up the `// TODO: replace with config loader (Stage 1)` marker at all (an earlier
  authoring inconsistency, confirmed by grep) — out of scope for this mechanical pass; flagged here
  rather than silently swept in.

Files: `src/project-constants/GameplayConfigHolder.cs` (+ tests), and 17 `<Spec>Constants.cs` +
`.asmdef` pairs (agent-movement, attacking-ai, ball-physics, collision-system, defensive-ai,
deterministic-sim, event-system, first-touch, goalkeeper-mechanics, heading-mechanics, match-engine,
pass-mechanics, perception-system, performance-optimization, shot-mechanics, tactical-instructions,
testing-strategy).

**`[EST]` constants:** Every `[EST]` constant requires a `spec-error-log.md` entry (FR-CS-020). The constant must be promoted to `[GT]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]` before the system that consumes it is implemented. If the validated value is derivable via formula, use `[DERIVED]` (document the formula per FR-CS-021). If it already exists authoritatively in another spec, use `[CROSS]` (cite the authoritative spec and section per FR-CS-022).

**`[CROSS]` mirrors — routing rule (Spec #20 §4.2):**
- **Multi-consumer** (constant used by ≥ 2 spec assemblies): declare in `ProjectConstants.cs`; each consuming catalogue mirrors from there.
- **Single-consumer** (constant used by exactly 1 spec assembly, e.g., a domain tag allocated in Spec #16 §3.4 used only by one spec): the consuming catalogue mirrors directly from the source spec's catalogue — not via `ProjectConstants.cs`.

A `[CROSS]` mirror must not diverge from its source. Naming is PascalCase per §3.2.3. Cite the authoritative spec and section:

```csharp
// Multi-consumer mirror: declare in ProjectConstants.cs; each consuming catalogue mirrors from there.
/// <summary>
/// [CROSS] Physics/render loop tick rate (Hz).
/// Authoritative source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Ball Physics #1 §1.2. Value: 60 Hz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;

// Single-consumer mirror: source spec's catalogue directly, NOT via ProjectConstants.cs
/// <summary>
/// [CROSS] Goalkeeper subsystem domain tag.
/// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER.
/// Deterministic Simulation #16 §3.4. Value: 0x1D.
/// </summary>
public static readonly uint DomainTagGoalkeeper =
    DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER;
```

> **Note — naming discrepancy in Spec #20 §4.2 (ERR-020-001, resolved):** The §4.2
> worked example originally showed `PHYSICS_TICK_HZ` (ALL_CAPS) for the `[CROSS]`
> *mirror* field in `BallPhysicsConstants.cs`. This contradicts §3.2.3, which is the
> rule-definition section and states PascalCase for `[CROSS]`. §3.2.3 is authoritative —
> use PascalCase for the mirror field name. Spec #20 §4.2 has been patched to show
> `PhysicsTickHz` (PascalCase). Note that the source constant in `ProjectConstants.cs`
> is tagged `[FIXED]` and correctly uses ALL_CAPS (`PHYSICS_TICK_HZ`); the right-hand
> side of the mirror assignment must reference that ALL_CAPS name.

---

## GAME-LOOP RULES (ZERO ALLOCATION)

The 60 Hz physics/render path must produce **zero managed-memory allocations per frame** (FR-CS-066).

**Required patterns:**
- Game-state data in `readonly struct`, not `class`
- State passed by `ref` parameter
- Pre-allocated fixed-size buffers for temp arrays
- Struct-based events on the event bus (not `event Action<T>`)
- `stackalloc` with `Span<T>` for transient buffers with statically bounded size (C# 7.2+; no `unsafe` block required). The pointer form (`int* p = stackalloc int[n]`) requires `unsafe` and therefore lead-developer sign-off per FR-CS-010 — use the `Span<T>` form by default
- `private static readonly ProfilerMarker` field on every system class for profiling (one-time alloc at startup); call `.Auto()` at each entry point to bracket the measurement scope (FR-CS-070)
- **Dependency injection via constructor parameters** — see "Banned Architectural Patterns" below for the full rule and the four anti-patterns it replaces

**Banned constructs on hot paths (FR-CS-027–034):**
- `new` class objects or managed arrays
- Boxing (value type → object cast)
- LINQ (`.Where`, `.Select`, `.ToList`, etc.)
- `params` array parameters
- String formatting (`$"…"`, `string.Format`, `+` concatenation)
- Closures capturing local variables
- `foreach` over any type that does not expose a concrete struct `GetEnumerator()` at the call site — including `List<T>` or `Dictionary<K,V>` via an interface variable (both `List<T>.Enumerator` and `Dictionary.Enumerator` are structs, but both are boxed when the collection variable is typed as an interface); use arrays or `Span<T>` for hot-path iteration
- Reflection

**Banned language features in game-loop and game-state code (FR-CS-010):**
- `dynamic` — bypasses compile-time type safety; introduces non-deterministic dispatch paths
- `async`/`await` in game-loop / game-state-modifying code — breaks deterministic tick ordering; continuations resume on unpredictable frames. Permitted in initialization code, editor tooling, and loading pipelines that do not touch game state.
- `unsafe` without lead-developer sign-off recorded in the PR description
- `try`/`catch` inside per-frame inner loops (FR-CS-069)
- Virtual method calls inside per-frame inner loops (FR-CS-068)

**Banned architectural patterns in game-state assemblies (FR-CS-051–054):**
- **Service locator** (`ServiceLocator.Get<T>()`) — hides dependencies; breaks deterministic testing
- **Ambient context** (`MatchContext.Current`) — hidden state; breaks replay rewind
- **Static mutable singleton** — cannot be reset between deterministic replay ticks
- **Generic DI container on the hot path** (Zenject, VContainer, `Microsoft.Extensions.DependencyInjection`) — reflection-based; allocates; violates zero-alloc budget

The required alternative to all four is **constructor injection**: pass dependencies as constructor parameters.

The `ProfilerMarker` field is `private static readonly`, named per the
`s_<EntryPointName>Marker` convention (see "Profiler Markers" section).

```csharp
// COMPLIANT — sealed instance class; dependencies injected via constructor per FR-CS-051–054
// Note: `state with { … }` requires C# 10+ on readonly structs. Verify the
// Unity LTS + backend in certification-platform.md before using this pattern.
public sealed class BallPhysicsSystem
{
    private readonly MatchClock _clock;
    private static readonly ProfilerMarker s_updateMarker =
        new ProfilerMarker("BallPhysics.Update");

    public BallPhysicsSystem(MatchClock clock)
    {
        _clock = clock;
    }

    public void Update(ref BallState state, float dt)
    {
        using var _ = s_updateMarker.Auto();
        state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
    }
}

// VIOLATION — copies BallState by value; wastes memory bandwidth
public void Update(BallState state, float dt) { … }
```

---

## DETERMINISM RULES

No `System.Random`, no `DateTime.Now`, no `Guid.NewGuid()`, no `Task.Run` or `Parallel.*`, no hardware-intrinsic FMA in game logic (FR-CS-036–040).

| Need | Use | Owning assembly |
|---|---|---|
| Random numbers | `SplitMix64` helper (FR-CS-041) | `deterministic-sim/` (Spec #16) |
| Simulation time | `MatchClock` (injected) (FR-CS-042) | `deterministic-sim/` (Spec #16) |
| Trigonometry / math | Project math helper (FR-CS-043) | `project-constants/` — exact class TBD at Stage 1 |
| Deterministic IDs | Pre-allocated deterministic ID ranges (Spec #16 §3.2.5) | `deterministic-sim/` (Spec #16) |

**Hardware-intrinsic FMA (FR-CS-040):** Fused multiply-add instructions can produce different results from separate multiply + add on different hardware or compiler versions. FMA intrinsics are banned unless the platform is pinned and the lead developer has signed off.

**64-bit multiplication** must use `unchecked { }` with a `// Spec #16 §3.4.4` comment
(FR-CS-044), regardless of which assembly the code lives in. The citation always refers
to Spec #16 §3.4.4 (SplitMix64 state update) — not the local spec's §3.4.4:

```csharp
unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{
    state += 0x9E3779B97F4A7C15UL;
}
```

**Python tooling** that mirrors C# SplitMix64 constants (FR-CS-045): omit the `UL` suffix and mask intermediates with `& 0xFFFFFFFFFFFFFFFF`. Do not mix `unchecked` into Python (it has no meaning there) or mask operators into C# (that would introduce a different semantic).

---

## NUMERIC TYPES

- `float` everywhere at Stage 0 (FR-CS-071).
- `double` is banned by default; override requires lead-developer sign-off and inline comment.
- `decimal` is always banned.
- Fixed64 migration is a Stage 5+ concern (Spec #9).

---

## INTERFACE DESIGN

Write an interface only when both the producer and consumer are specified. No phantom interfaces for unspecified systems (ERR-001, ERR-004, FR-CS-048/049).

An `interface` file MUST reside in the same assembly as at least one of its specified consumers (FR-CS-048). Access modifier is `public` only if callers cross the assembly boundary; `internal` otherwise (FR-CS-015).

**Event-vs-interface decision tree (FR-CS-050):**
- Same assembly → direct method call
- Cross-assembly, consumer not yet specified → wait; create nothing
- Cross-assembly, consumer specified, multiple implementations → interface (in consumer's assembly)
- Cross-assembly, single implementation, lower→higher layer notification → struct event on event bus
- Cross-assembly, single implementation, same or downward layer → direct method call

---

## FILE HEADER (REQUIRED ON EVERY FILE)

```csharp
// File:     src/ball-physics/BallPhysicsCore.cs
// Created:  2026-05-19
// Modified: 2026-05-19
// Author:   <name or handle>
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Implements core ball physics calculations (gravity, drag, Magnus effect).
//           Does not manage state; all state is passed by ref parameter.

namespace TacticalDirector.BallPhysics
{
    // …
}

#region VersionHistory
// | Version | Date       | Author           | Notes                   |
// | 1.0     | 2026-05-19 | <name or handle> | Initial implementation. |
#endregion
```

**Required fields (FR-CS-056/057):** file path (relative to repo root), created date (ISO), modified date (must match latest version-history row), author, governing specs, purpose (≤ 2 sentences).

Version history lives at the end of the file; rows are appended, never deleted.

When a file is authored or modified by an automated agent with no named individual, use `—` in the Author field.

---

## XML DOC COMMENTS

Every `public` type, method, property, and event requires `/// <summary>`. Every constant (any access level) requires `/// <summary>` that includes its tag (FR-CS-060/061).

```csharp
/// <summary>[FIXED] Ball radius in metres. Ball Physics Spec #1 §2.1.</summary>
public const float BALL_RADIUS = 0.11f;

/// <summary>Applies drag to ball velocity for one physics step.</summary>
/// <param name="velocity">Current velocity vector (m/s).</param>
/// <param name="dt">Time delta in seconds.</param>
public static Vector3 CalculateDrag(Vector3 velocity, float dt) { … }
```

---

## INLINE COMMENTS

Write a comment only when the **why** is non-obvious. Do not comment what the code already says (FR-CS-064).

```csharp
// COMPLIANT — hidden constraint
unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{ … }

// VIOLATION — states the obvious
int count = agentList.Count;  // Get the number of agents
```

**Commented-out code is prohibited** in any commit to a shared branch (FR-CS-065). Delete disabled code; version control preserves the history.

---

## `using` DIRECTIVE ORDER

System → Unity → Project, each group separated by a blank line (FR-CS-006):

```csharp
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;
using TacticalDirector.EventSystem;
```

Alphabetical within each group is recommended but not enforced.

---

## PROFILER MARKERS

Every system entry point (`Update`, `Tick`, `RunStep`, or similarly named method) must be wrapped in a `ProfilerMarker.Auto()`. The marker is a `private static readonly` field (allocated once at startup — zero per-frame cost) (FR-CS-070).

> **Note:** These are custom methods on game system classes — **not** Unity MonoBehaviour
> lifecycle callbacks (`FixedUpdate()` / `Update()` with no parameters). The MonoBehaviour
> / PlayerLoop integration layer is a Stage 1 concern; see "WHAT IS NOT HERE YET" below.

**Field naming convention:** `s_<EntryPointName>Marker` — e.g., `s_updateMarker` for `Update`, `s_runTickMarker` for `RunTick`.

**Marker string format:** `<SpecName>.<MethodName>` (e.g., `"BallPhysics.Update"`, `"DeterministicSim.RunTick"`).

```csharp
using UnityEngine.Profiling;

// Profiler-relevant fields shown; constructor and injected dependencies
// follow the same pattern as the Game-Loop Rules COMPLIANT example above.
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateMarker =
        new ProfilerMarker("BallPhysics.Update");

    public void Update(ref BallState state, float dt)
    {
        using var _ = s_updateMarker.Auto();
        // …
    }
}
```

---

## STAGE 0 VERIFICATION

No static analysis tooling yet. Verify each file manually against the Spec #20 §5.4 checklist before marking it complete. Roslyn analyzers, `BannedSymbols.txt`, and `.editorconfig` activate at Stage 1 once `certification-platform.md` is fully pinned.

---

## WHAT IS NOT HERE YET

These items are deferred pending Unity project setup and platform pinning:

| Item | Blocked on |
|---|---|
| `.asmdef` content (GUIDs, `allowUnsafeCode`, `autoReferenced`, `testPlatforms`, `versionDefines`) | Unity project initialization |
| Exact Unity LTS revision | `docs/tracking/certification-platform.md` pinned |
| `dotnet test` framework args | Stage 0+1 setup (Spec #19 §7.5 D2 — framework pin deferred to Stage 0+1) |
| Unity batch-mode CI commands | **Command DEFINED July 13, 2026** — the FR-PO-052 certified perf capture command is in "BUILD AND TEST COMMANDS" above, driving `MatchEngineCapstonePerfHarnessTests` → the real `MatchEngine` capstone via `MatchEngineCapstonePerfHarness`. The harness itself (`StopwatchPerfHarness` + the capstone runner) runs on the Linux `dotnet-ci` gate as a non-certifying compile+execute proof. Still requires a **Unity host** to execute the certified capture (`cert-run-runbook.md` P2 + Steps 2–4); a full batch-mode *build* pipeline (as opposed to the test-runner invocation) remains for Unity project initialization. |
| `.editorconfig` path and contents | Stage 1 setup |
| C# language version pin | `certification-platform.md` pinned |
| `[GT]` config loader class / method | **Mechanism landed June 30, 2026** — `TacticalDirector.ProjectConstants.GameplayConfig` + `GameplayConfigFileLoader` (`src/project-constants/`). **Migration landed June 30, 2026** — `GameplayConfigHolder` resolves the boot-sequencing design point; 509/520 `[GT]` declarations across 17 catalogues now read `Config.GetX(...)`. See the "`[GT]` loading mechanism" + "Boot-sequencing resolution" + "Migration status" sections above for the mechanism, the 11-array-table + 4-untagged-catalogue carve-outs, and the still-open follow-up (no composition root calls `GameplayConfigHolder.Bind` yet — Stage 0 has no on-disk config load wired into match-engine boot). |
| Project math helper class name / assembly | Stage 1 setup — update determinism table when defined |
| `src/tactical-instructions/` seams into #8/#11–#15 (Spec #21 T2–T3) | **T0 landed June 21, 2026** — the `TacticalDirector.TacticalInstructions` assembly (16 enums + `TeamTactic`/`PlayerTactic`/`PlayerInstructions` + identity factories + catalogue + ordinal/identity tests) is in the tree above, behaviour-neutral. **T2 Decision Tree (#8) seam landed June 28, 2026** (behaviour-neutral): `decision-tree/TacticTranslation.cs` (rank-mapped TacticPressing/TacticPassing → #8 enums + §3.1 F5 clamp + §3.2 Mentality risk/line resolvers), a `Mentality` routing field on `TacticalContext` (`Stage0Default` seeds Balanced = identity), and the §3.2/§3.3 Mentality risk multiplier in `UtilityScorer.ComputeUtility` (×1.0 at Balanced). `decision-tree(.Tests).asmdef` gain the `TacticalInstructions` ref. **Runtime activation (Phase-D single-writer for #8) landed June 28, 2026** (behaviour-neutral): `MatchEngine` holds a per-team `TeamTactic` (default `Balanced`), exposes `public SetTeamTactic(teamId, tactic)` (stages pending; committed pending→active at the AI-stride boundary per FR-TI-027), and `RunMechanicsAI` overlays the active tactic's `Mentality` + translated `Pressing`/`Passing` into each `TacticalContext`. `TacticTranslation` promoted internal→public (the match-engine is its §3.1 caller). `match-engine(.Tests).asmdef` gain the `TacticalInstructions` ref; `MatchEngineTacticTests.cs` added. **T2 Pressing AI (#13) consumer seam landed June 28, 2026** (behaviour-neutral): `pressing-ai/TacticTranslation.cs` maps `LineOfEngagement` → a multiplicative scalar on the #13 press-trigger radius (`PressTriggerDistanceM`) via `LineOfEngagementScalar` (direct ordinal lookup + §3.1 F5 clamp; Standard ⇒ ×1.0), a `LineOfEngagement` routing field on `PressingSnapshot` (ctor-seeded `Standard` = identity; zero-value default is `VeryLow`), and `PrimaryPressSelector.Select` scaling its eligibility radius by that scalar (×1.0 at Standard = byte-identical). `pressing-ai(.Tests).asmdef` gain the `TacticalInstructions` ref; `Tests/TacticTranslationTests.cs` added. **The #13 match-engine Phase-D writer landed June 29, 2026** (MatchEngine.cs v1.18, behaviour-neutral): `FillPressingSnapshot` routes the pressing team's active `TeamTactic.LineOfEngagement` → `PressingSnapshot.LineOfEngagement` (default Balanced ⇒ Standard ⇒ ×1.0; new `TestOnly_PressLineOfEngagement` seam; `MatchEngineTacticTests.cs` v1.1). **The #12/#14/#15 consumer seams landed June 29, 2026** (behaviour-neutral): each gains `<assembly>/TacticTranslation.cs` + a snapshot routing field seeded to identity — #12 `TacticWidth`/`TacticDefWidth` → lateral-compactness scalar on `ContextModifierInputs`, fully wired into `ContextModifier.ApplyToAll` (Standard ⇒ ×1.00 exact); #14 `OffsideTrap` → `DefensiveSnapshot.OffsideTrapRequested` (false identity, arming-gate consumption deferred — KD-9 request-not-guarantee, gating today's autonomous trap behind a default-false toggle is not neutral); #15 `FocusPlay` → `AttackingSnapshot.FocusPlay` (Mixed zero-value identity, `OverloadDetector` flank-preference consumption deferred). **The #14/#15 match-engine Phase-D writers landed June 29, 2026** (MatchEngine.cs v1.19, behaviour-neutral): `FillDefensiveSnapshot` routes `TeamTactic.OffsideTrap` → `DefensiveSnapshot.OffsideTrapRequested` via fully-qualified `DefensiveAI.TacticTranslation` (CS0104 — five `TacticTranslation` types in match-engine scope per the #13 v1.17 lesson); `FillAttackingSnapshot` routes `TeamTactic.FocusPlay` → `AttackingSnapshot.FocusPlay` (enum passthrough). Default Balanced ⇒ false / Mixed = identities, byte-identical to pre-#21. Active consumption still deferred (#14 `OffsideTrapController` per KD-9; #15 `OverloadDetector` per §5.6/G2). New `TestOnly_OffsideTrapRequested`/`TestOnly_FocusPlay` seams; `MatchEngineTacticTests.cs` v1.2. **The #12 match-engine Phase-D writer landed June 29, 2026** (MatchEngine.cs v1.20, behaviour-neutral) — the last of the three Mechanics writers: `RunMechanicsAI` builds `ContextModifierInputs` via the 5-arg ctor, routing the active `TeamTactic.Width`/`DefensiveWidth` (translated by `ContextModifier` to the in-poss / OOP lateral-compactness scalar). Default Balanced ⇒ Standard/Standard ⇒ ×1.00 = byte-identical (5-arg both-Standard ≡ 3-arg identity ctor). Per-team `_posModifiers` captured for the new `TestOnly_PositioningWidth`/`TestOnly_PositioningDefWidth` seams; `MatchEngineTacticTests.cs` v1.3. **All three Mechanics Phase-D writers (#12/#13/#14/#15) are now closed.** **Active #14/#15 consumption landed June 29, 2026** (behaviour-neutral on default): #14 `OffsideTrapController.Update` consumes `OffsideTrapRequested` as the KD-9 additive request (requested ⇒ arms after reduced `[GT] OffsideTrapRequestedDwellTicks` ≤ baseline; the §3.7.2 conditions still adjudicate; false ⇒ baseline); #15 `OverloadDetector` gains a 5-arg `Evaluate(…, Flank? preferredFlank, …)` (4-arg delegates null) using `FocusPlay`→`Flank?` as a bias (preferred ball-side flank lowers the trigger count by `[GT] OverloadFocusCountBias`; null/non-ball-side ⇒ unchanged); `AttackingAITick` threads it. Both `[GT]` magnitudes illustrative pending §5.6/G2. **The TeamTactic config-loader in-code source + boot applier landed June 29, 2026** (the runtime-activation gate that populates `SetTeamTactic`): `match-engine/TeamTacticConfig.cs` (immutable per-team `TeamTactic`, index = teamId; `Default` = Balanced-for-every-team identity, FR-TI-031; `ForTeam` bounds-guarded) + `match-engine/TeamTacticConfigApplier.cs` (static `Apply(engine, config)` stages each team via the public `SetTeamTactic` before kickoff — committed at the first stride per FR-TI-027). Per the #19 `ScenarioIndex` D1 precedent the **on-disk file format is deferred** (the FR-CS-019 `[GT]` loader is Stage 1; encodings D1-pinned at Stage 0+1) — the Stage 0+1 disk loader is a pure parser swap producing a `TeamTacticConfig` and feeding `Apply` unchanged; no format invented, no production `MatchEngine.cs` change. `match-engine/tests/TeamTacticConfigTests.cs` added. **The §3.3 per-agent PlayerTactic utility product seam landed June 29, 2026** (v1.80): `decision-tree/TacticTranslation.PlayerTacticActionMultiplier` composes `RoleWeightModifiers`×dutyBias×instrBias×`TempoActionBias`; `TacticalContext` gains `Tempo` + `PlayerTactic` routing fields (Stage0Default identity-seeded); `UtilityScorer` applies it per option (identity ⇒ ×1.0). `RunMechanicsAI` routes the active team `Tempo`; the per-agent `PlayerTactic` stays the Stage-0 identity (no per-agent config surface). **The on-disk tactic-file loader landed June 29, 2026** (v1.81): `TeamTacticFileLoader.Parse(text) → TeamTacticConfig` (Stage-0 `key=value` text format; empty ⇒ Default ⇒ neutral; fail-loud). **ERR-021-002 resolved June 29, 2026** (v1.82): per-team active+pending `TeamTactic` serialized into the snapshot, `SNAPSHOT_SCHEMA_VERSION` 8 → 9 — a mid-match change is now restore-deterministic. Still pending (Stage-1): the **per-agent tactic config surface** (all agents are the identity `PlayerTactic` at Stage 0) + the **§5.6/G2 balance pass** pinning the illustrative `[GT]` magnitudes; the §3.4 `DefensiveLine` depth recompute (#12/#14 depth-ownership) remains separately deferred. The #21 assembly's own asmdef `references` array is empty until `project-constants` exists (FR-TI-002 — T0 consumes nothing from it). |
| MonoBehaviour / PlayerLoop integration pattern | Unity project initialization — how Unity's lifecycle loop calls into struct-based game systems; until defined, system entry points are pure C# instance methods named `Update`, `Tick`, or similar |
| `AgentState` as `readonly struct` + `with` expressions | C# language version pin in `certification-platform.md` — `with` on `readonly struct` requires C# 10+. Until pinned, `AgentState` (and equivalent game-state structs) are mutable structs mutated by `ref` parameter; migration to readonly + with is a Stage 0+1 cleanup task once the language version is locked. |

Update this file when those items are resolved.

---
