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
├── deterministic-sim/                 ← Spec #16
│   ├── deterministic-sim.asmdef
│   ├── DeterministicSimConstants.cs
│   ├── TickOrchestrator.cs
│   ├── SnapshotCodec.cs
│   └── tests/
│
├── event-system/                      ← Spec #17
│   ├── event-system.asmdef
│   ├── EventSystemConstants.cs
│   ├── EventBus.cs
│   ├── EventLedger.cs
│   ├── CosmeticChannel.cs
│   ├── EventRegistry.cs
│   └── tests/
│
├── performance-optimization/          ← Spec #18  (tooling/governance; minimal runtime code)
├── testing-strategy/                  ← Spec #19  (tooling/governance; minimal runtime code)
└── code-standards/                    ← Spec #20  (governance only; no runtime code)
```

**One folder per spec. One `.asmdef` per folder. Folder names match `docs/specs/` exactly.**

### Assembly Dependency Direction

References flow downward only. A lower-layer assembly must never reference a higher-layer assembly. Use struct events on the event bus for upward communication.

```
project-constants     ← referenced read-only by all assemblies
       ↑
  Physics layer:   ball-physics → agent-movement → collision-system → …
       ↑
  Mechanics layer: pass-mechanics, shot-mechanics, first-touch, heading-mechanics, …
       ↑
  AI layer:        perception-system, decision-tree, positioning-ai, pressing-ai, …
       ↑
  Systems layer:   deterministic-sim, event-system   (cross-cutting; referenced by all)
```

Every inter-assembly dependency must be declared explicitly in the `.asmdef` file. Implicit compiler resolution is prohibited (FR-CS-055).

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

No Hungarian notation. No other prefix/suffix schemes.

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
#region EST        // [EST]     → public static readonly float LiftCoefficient = 0.35f; // TODO: validate before impl
```

Omit a region entirely if the spec has no constants with that tag. Empty regions are prohibited.

**[CROSS] mirrors:** A `[CROSS]` entry in a spec catalogue mirrors its value from `ProjectConstants.cs` and must not diverge. Cite the source:

```csharp
/// <summary>
/// [CROSS] Physics tick rate. Source: ProjectConstants.PhysicsTickHz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PhysicsTickHz;
```

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

**Banned on hot paths:**
- `new` class objects or managed arrays
- Boxing (value type → object cast)
- LINQ (`.Where`, `.Select`, `.ToList`, etc.)
- `params` array parameters
- String formatting (`$"…"`, `string.Format`, `+` concatenation)
- Closures capturing local variables
- `foreach` over non-struct enumerators (`List<T>`, `Dictionary<K,V>`)
- Reflection
- `try`/`catch` inside per-frame inner loops (FR-CS-069)
- Virtual method calls inside per-frame inner loops (FR-CS-068)

```csharp
// COMPLIANT
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_fixedUpdateMarker.Auto();
    state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
}

// VIOLATION — copies struct by value; allocates nothing but wastes memory bandwidth
public static void UpdateBallPhysics(BallState state, float dt) { … }
```

---

## DETERMINISM RULES

No `System.Random`, no `DateTime.Now`, no `Guid.NewGuid()`, no `Task.Run` or `Parallel.*` in game logic (FR-CS-036–040).

| Need | Use |
|---|---|
| Random numbers | `SplitMix64` helper |
| Simulation time | `MatchClock` (injected) |
| Deterministic IDs | Pre-allocated deterministic ID ranges (Spec #16 §3.2.5) |

**64-bit multiplication** must use `unchecked { }` with a `// §3.4.4` comment:

```csharp
unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{
    state += 0x9E3779B97F4A7C15UL;
}
```

**Python tooling** that mirrors C# SplitMix64 constants: omit the `UL` suffix and mask intermediates with `& 0xFFFFFFFFFFFFFFFF`.

---

## NUMERIC TYPES

- `float` everywhere at Stage 0 (FR-CS-071).
- `double` is banned by default; override requires lead-developer sign-off and inline comment.
- `decimal` is always banned.
- Fixed64 migration is a Stage 5+ concern (Spec #9).

---

## INTERFACE DESIGN

Write an interface only when both the producer and consumer are specified. No phantom interfaces for unspecified systems (ERR-001, ERR-004, FR-CS-048).

Interface types belong in the consumer's assembly, not the producer's. Access modifier is `public` only if callers cross the assembly boundary; `internal` otherwise (FR-CS-015).

---

## FILE HEADER (REQUIRED ON EVERY FILE)

```csharp
// File: src/ball-physics/BallPhysicsCore.cs
// Created: 2026-05-19
// Modified: 2026-05-19
// Spec: Ball Physics #1, Code Standards #20

namespace TacticalDirector.BallPhysics
{
    // …
}

#region VersionHistory
// | Version | Date       | Author | Notes                          |
// | 1.0     | 2026-05-19 | —      | Initial implementation.        |
#endregion
```

Required fields: file path (relative to repo root), created date (ISO), modified date (must match latest version-history row), governing specs. Version history lives at the end of the file; rows are appended, never deleted.

---

## XML DOC COMMENTS

Every `public` type, method, property, and event requires `/// <summary>`. Every constant (any access level) requires `/// <summary>` that includes its tag.

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

Write a comment only when the **why** is non-obvious. Do not comment what the code already says.

```csharp
// COMPLIANT — hidden constraint
unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
{ … }

// VIOLATION — states the obvious
int count = agentList.Count;  // Get the number of agents
```

---

## `using` DIRECTIVE ORDER

System → Unity → Project, each group separated by a blank line:

```csharp
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;
using TacticalDirector.Shared;
```

---

## PROFILER MARKERS

Every system entry point (`FixedUpdate`, `Update`, tick method) must be wrapped in a `ProfilerMarker.Auto()`. The marker is a `private static readonly` field (allocated once at startup — zero per-frame cost).

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
| `dotnet test` framework args | Spec #19 framework selection |
| Unity batch-mode CI commands | Unity project initialization |
| `.editorconfig` path and contents | Stage 1 setup |
| C# language version pin | `certification-platform.md` pinned |

Update this file when those items are resolved.

---

## VERSION HISTORY

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-19 | — | Initial creation. All 20 Stage 0 specs approved; coding begins. |
