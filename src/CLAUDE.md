# src/CLAUDE.md — Tactical Director Coding Guide

> **Created:** May 19, 2026
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
├── CLAUDE.md                          ← You are here
│
├── project-constants/
│   ├── project-constants.asmdef       ← one assembly per folder (FR-CS-055)
│   └── ProjectConstants.cs            ← [CROSS] source-of-truth for all cross-spec constants
│
├── ball-physics/                      ← Spec #1
│   ├── ball-physics.asmdef
│   ├── BallPhysicsConstants.cs
│   ├── BallState.cs
│   ├── BallStateSystem.cs
│   ├── BallPhysicsCore.cs
│   ├── BallStateMachine.cs
│   ├── BallGroundInteraction.cs
│   ├── BallCollision.cs
│   ├── BallEventLogger.cs
│   ├── SurfaceProperties.cs
│   └── tests/
│       ├── BallPhysicsCoreTests.cs
│       └── BallIntegrationTests.cs
│
├── agent-movement/                    ← Spec #2
│   ├── agent-movement.asmdef
│   ├── AgentMovementConstants.cs
│   ├── AgentState.cs
│   ├── AgentMovementSystem.cs
│   └── tests/
│
├── collision-system/                  ← Spec #3
│   ├── collision-system.asmdef
│   ├── CollisionSystemConstants.cs
│   └── tests/
│
├── first-touch/                       ← Spec #4
├── pass-mechanics/                    ← Spec #5
├── shot-mechanics/                    ← Spec #6
├── perception-system/                 ← Spec #7
├── decision-tree/                     ← Spec #8
├── fixed64-math/                      ← Spec #9  (Stage 5+; no runtime code at Stage 0)
├── heading-mechanics/                 ← Spec #10
├── goalkeeper-mechanics/              ← Spec #11
├── positioning-ai/                    ← Spec #12
├── pressing-ai/                       ← Spec #13
├── defensive-ai/                      ← Spec #14
├── attacking-ai/                      ← Spec #15
│
├── deterministic-sim/                 ← Spec #16  (cross-cutting; referenced by all layers)
│   ├── deterministic-sim.asmdef
│   ├── DeterministicSimConstants.cs
│   ├── TickOrchestrator.cs
│   ├── SnapshotCodec.cs
│   └── tests/
│
├── event-system/                      ← Spec #17  (cross-cutting; referenced by all layers)
│   ├── event-system.asmdef
│   ├── EventSystemConstants.cs
│   ├── EventBus.cs
│   ├── EventLedger.cs
│   ├── CosmeticChannel.cs
│   ├── EventRegistry.cs
│   └── tests/
│
├── performance-optimization/          ← Spec #18  (owns trace pipeline; minimal game-loop code)
├── testing-strategy/                  ← Spec #19  (CI orchestration tooling; no game-loop code)
└── code-standards/                    ← Spec #20  (governance only; no runtime code)
```

**One folder per spec. One `.asmdef` per folder. Folder names match `docs/specs/` exactly.**

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

### Reference Direction

**AI depends on Mechanics. Mechanics depends on Physics. Never the reverse.**

```
project-constants  ←  referenced read-only by all assemblies

Physics  ←  Mechanics  ←  AI  ←  UI
```

`←` means "references." The AI assembly imports types from Mechanics, which imports
types from Physics. A Physics assembly MUST NOT import from Mechanics or AI.
Upward references (Physics→AI, Mechanics→AI, etc.) are prohibited by FR-CS-046 and
enforced as build errors via `.asmdef` reference declarations.

For upward event notification (e.g., a physics event consumed by AI), use a struct
event on the event bus — no direct assembly reference (FR-CS-047).

For the specific `.asmdef` references each assembly declares, read that spec's `§4`
(Architecture) file. Do not infer the intra-layer dependency chain from this document.

---

## BUILD AND TEST COMMANDS

> **Note:** The Unity LTS revision, backend (Mono/IL2CPP), and compiler flags are not yet pinned in `docs/tracking/certification-platform.md`. Fill those in before running the first certification gate (`FR-DS-009-GATE`). The commands below are the intended Stage 1 setup; update this section when the project is configured.

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

**Unity batch-mode test run (CI):**
```
# To be filled in once Unity project is initialized and certification-platform.md is pinned.
```

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
| `[FIXED]` constants | `ALL_CAPS` | `BALL_RADIUS`, `DRAG_COEFFICIENT` |
| All other constants (`[GT]`, `[EST]`, `[DERIVED]`, `[CROSS]`) | PascalCase | `MaxSubsteps`, `TerminalVelocity` |
| Interfaces | `I` prefix + PascalCase | `IEventBus`, `ICollisionConsumer` |
| Assembly names / namespaces | `TacticalDirector.<SpecName>` | `TacticalDirector.BallPhysics` |

No Hungarian notation. No other prefix/suffix schemes (FR-CS-001/002).

**`var` policy (FR-CS-013):** Use `var` only when the type is immediately obvious from the RHS. `var state = new BallState()` is clear. `var result = Compute()` is not — write the explicit type.

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
#region Derived    // [DERIVED] → public static readonly float TerminalVelocity = …;
#region Cross      // [CROSS]   → public static readonly float PhysicsTickHz = ProjectConstants.PhysicsTickHz;
#region GT         // [GT]      → public static readonly int MaxSubsteps = ConfigLoader.GetValue(…);
#region EST        // [EST]     → public static readonly float LiftCoefficient = 0.35f; // TODO: validate
```

Omit a region entirely if the spec has no constants with that tag. Empty regions are prohibited.

**`[EST]` constants:** Every `[EST]` constant requires a `spec-error-log.md` entry (FR-CS-020). The constant must be promoted to `[GT]` or `[FIXED]` before the system that consumes it is implemented.

**`[CROSS]` mirrors:** A `[CROSS]` entry in a spec catalogue mirrors its value from `ProjectConstants.cs` (or the authoritative source spec) and must not diverge. Naming is PascalCase per §3.2.3. Cite the source:

```csharp
/// <summary>
/// [CROSS] Physics tick rate (Hz). Source: ProjectConstants.PhysicsTickHz.
/// Root CLAUDE.md — "Heartbeat Tick Rate": 60 Hz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PhysicsTickHz;
```

> **Note — naming discrepancy in Spec #20 §4.2:** The §4.2 worked example shows
> `PHYSICS_TICK_HZ` (ALL_CAPS) for a `[CROSS]` mirror. This contradicts §3.2.3, which
> is the rule-definition section and states PascalCase for `[CROSS]`. §3.2.3 is
> authoritative. Use PascalCase for all `[CROSS]` constants.

---

## GAME-LOOP RULES (ZERO ALLOCATION)

The 60 Hz physics/render path must produce **zero managed-memory allocations per frame** (FR-CS-066).

**Required patterns:**
- Game-state data in `readonly struct`, not `class`
- State passed by `ref` parameter
- Pre-allocated fixed-size buffers for temp arrays
- Struct-based events on the event bus (not `event Action<T>`)
- `stackalloc` for transient buffers with statically bounded size
- `ProfilerMarker.Auto()` on every system entry point (static readonly field — one-time alloc at startup)
- **Dependency injection via constructor parameters** — pass dependencies into constructors; do not resolve them at runtime

**Banned constructs on hot paths (FR-CS-027–034):**
- `new` class objects or managed arrays
- Boxing (value type → object cast)
- LINQ (`.Where`, `.Select`, `.ToList`, etc.)
- `params` array parameters
- String formatting (`$"…"`, `string.Format`, `+` concatenation)
- Closures capturing local variables
- `foreach` over non-struct enumerators (`List<T>`, `Dictionary<K,V>`)
- Reflection

**Banned language features in all game-logic code (FR-CS-010):**
- `dynamic` — bypasses compile-time type safety; introduces non-deterministic dispatch paths
- `async`/`await` for game-state work — breaks deterministic tick ordering; continuations run on unpredictable frames
- `unsafe` without lead-developer sign-off recorded in the PR description
- `try`/`catch` inside per-frame inner loops (FR-CS-069)
- Virtual method calls inside per-frame inner loops (FR-CS-068)

**Banned architectural patterns in game-state assemblies (FR-CS-051–054):**
- **Service locator** (`ServiceLocator.Get<T>()`) — hides dependencies; breaks deterministic testing
- **Ambient context** (`MatchContext.Current`) — hidden state; breaks replay rewind
- **Static mutable singleton** — cannot be reset between deterministic replay ticks
- **Generic DI container on the hot path** (Zenject, VContainer, `Microsoft.Extensions.DependencyInjection`) — reflection-based; allocates; violates zero-alloc budget

The required alternative to all four is **constructor injection**: pass dependencies as constructor parameters.

```csharp
// COMPLIANT
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_fixedUpdateMarker.Auto();
    state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
}

// VIOLATION — copies struct by value; wastes memory bandwidth
public static void UpdateBallPhysics(BallState state, float dt) { … }
```

---

## DETERMINISM RULES

No `System.Random`, no `DateTime.Now`, no `Guid.NewGuid()`, no `Task.Run` or `Parallel.*`, no hardware-intrinsic FMA in game logic (FR-CS-036–040).

| Need | Use |
|---|---|
| Random numbers | `SplitMix64` helper (FR-CS-041) |
| Simulation time | `MatchClock` (injected) (FR-CS-042) |
| Trigonometry / math | Project math helper (FR-CS-043) |
| Deterministic IDs | Pre-allocated deterministic ID ranges (Spec #16 §3.2.5) |

**Hardware-intrinsic FMA (FR-CS-040):** Fused multiply-add instructions can produce different results from separate multiply + add on different hardware or compiler versions. FMA intrinsics are banned unless the platform is pinned and the lead developer has signed off.

**64-bit multiplication** must use `unchecked { }` with a `// §3.4.4` comment (FR-CS-044):

```csharp
unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
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
unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
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

Every system entry point (`FixedUpdate`, `Update`, tick method) must be wrapped in a `ProfilerMarker.Auto()`. The marker is a `private static readonly` field (allocated once at startup — zero per-frame cost) (FR-CS-070).

```csharp
private static readonly ProfilerMarker s_marker =
    new ProfilerMarker("BallPhysics.FixedUpdate");

public void FixedUpdate(ref BallState state, float dt)
{
    using var _ = s_marker.Auto();
    // …
}
```

Marker name format: `<SpecName>.<MethodName>` (e.g., `"BallPhysics.FixedUpdate"`, `"DeterministicSim.RunTick"`).

---

## STAGE 0 VERIFICATION

No static analysis tooling yet. Verify each file manually against the Spec #20 §5.4 checklist before marking it complete. Roslyn analyzers, `BannedSymbols.txt`, and `.editorconfig` activate at Stage 1 once `certification-platform.md` is fully pinned.

---

## WHAT IS NOT HERE YET

These items are deferred pending Unity project setup and platform pinning:

| Item | Blocked on |
|---|---|
| `.asmdef` GUIDs | Unity project initialization |
| Exact Unity LTS revision | `docs/tracking/certification-platform.md` pinned |
| `dotnet test` framework args | Stage 0+1 setup (Spec #19 §7.5 D2 — framework pin deferred to Stage 0+1) |
| Unity batch-mode CI commands | Unity project initialization |
| `.editorconfig` path and contents | Stage 1 setup |
| C# language version pin | `certification-platform.md` pinned |

Update this file when those items are resolved.

---

## VERSION HISTORY

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-19 | — | Initial creation. All 20 Stage 0 specs approved; coding begins. |
| 1.1 | 2026-05-19 | — | Adversarial review v1.0 fix pass. H-1: layer taxonomy rebuilt from §3.5.2. H-2/H-3: dependency arrows corrected. H-4: Author and Purpose added to file header template. M-1: FMA ban added. M-2: dynamic/async/unsafe bans added. M-3: four architectural anti-patterns added. M-4: phantom TacticalDirector.Shared replaced. M-5: [CROSS] naming contradiction flagged. M-6: Spec #19 blocker resolved to §7.5 D2. L-1: style section added (indentation, Allman braces). L-2: project-constants.asmdef added to tree. L-3: commented-out code ban added. L-4: [EST] spec-error-log requirement added. L-5: var policy added. |
