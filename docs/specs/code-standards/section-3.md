# Code Standards & Style Guide Specification #20 — Section 3: Technical Specification

**File:** `docs/specs/code-standards/section-3.md`
**Purpose:** Rule mechanics for all FR-CS-### groups. Provides the "how the rule is
applied" detail — code shapes, exception lists, worked examples, cross-references.
FR definitions (rule statements and conformance levels) live in §2.2; this section
does not restate them. Appendix D is the single source of truth for banned/required
API symbol lists; §3.3 and §3.4 cite it by category name only.

**Created:** May 7, 2026
**Modified:** August 18, 2026
**Version:** 1.5
**Status:** APPROVED (May 11, 2026)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 3
**Subsection target lengths:** §3.1 ~150 lines · §3.2 ~120 lines · §3.3 ~100 lines ·
§3.4 ~120 lines · §3.5 ~110 lines · §3.6 ~90 lines · §3.7 ~30 lines ·
§3.8 ~10 lines · §3.9 ~60 lines · §3.10 ~10 lines

---

## Table of Contents

- [3.1 C# Style Rules (FR-CS-001 … FR-CS-015)](#31-c-style-rules-fr-cs-001--fr-cs-015)
- [3.2 Constant Declaration & Tagging (FR-CS-016 … FR-CS-025)](#32-constant-declaration--tagging-fr-cs-016--fr-cs-025)
- [3.3 Allocation Discipline (FR-CS-026 … FR-CS-035)](#33-allocation-discipline-fr-cs-026--fr-cs-035)
- [3.4 Determinism in Code (FR-CS-036 … FR-CS-045)](#34-determinism-in-code-fr-cs-036--fr-cs-045)
- [3.5 Dependency Direction & Interface Design (FR-CS-046 … FR-CS-055)](#35-dependency-direction--interface-design-fr-cs-046--fr-cs-055)
- [3.6 Documentation Conventions (FR-CS-056 … FR-CS-065)](#36-documentation-conventions-fr-cs-056--fr-cs-065)
- [3.7 Numeric Type Discipline (FR-CS-071 … FR-CS-073)](#37-numeric-type-discipline-fr-cs-071--fr-cs-073)
- [3.8 Worked Examples Index](#38-worked-examples-index)
- [3.9 Edge Cases (Rule-Application Carve-Outs)](#39-edge-cases-rule-application-carve-outs)
- [3.10 Constants Catalogue](#310-constants-catalogue)
- [3.11 Version History](#311-version-history)

---

## 3.1 C# Style Rules (FR-CS-001 … FR-CS-015)

*Implements:* FR-CS-001–015. See §2.2.1 for rule statements and conformance levels.

---

### 3.1.1 Naming

Four naming contexts cover all identifiers in the codebase. Hungarian notation and
any other prefix/suffix scheme not listed here is prohibited by implication.

| Context | Convention | Example |
|---|---|---|
| Types (class, struct, enum, delegate, interface, record) | PascalCase | `BallState`, `IEventBus`, `ContactZone` |
| Methods, properties, events | PascalCase | `ApplyKick`, `CurrentSpeed`, `OnBounce` |
| Local variables and parameters | camelCase | `deltaTime`, `agentId`, `spinVector` |
| Private instance fields | `_camelCase` | `_clock`, `_updateMarker`, `_agentCount` |
| Constants with tag `[FIXED]` | `ALL_CAPS` | `BALL_RADIUS`, `DRAG_COEFFICIENT` |
| Constants with any other tag | PascalCase | `MaxSubsteps`, `TerminalVelocity` |

**Anti-example — Hungarian notation (prohibited):**

```csharp
// VIOLATION — FR-CS-001/002: Hungarian prefix adds type-noise with no value in a
// strongly-typed language. Rename to remove the type prefix.
float fBallRadius = 0.11f;    // 'f' prefix
int   nAgentCount = 22;        // 'n' prefix
bool  bIsGrounded  = true;     // 'b' prefix
```

**Compliant naming:**

```csharp
// COMPLIANT — FR-CS-001/002
float ballRadius  = 0.11f;
int   agentCount  = 22;
bool  isGrounded  = true;
```

---

### 3.1.2 File Layout

Each `.cs` file contains exactly one public type (FR-CS-005). The filename matches
the type name exactly, including case. Files containing only a `static class` of
constants follow the same rule (`BallPhysicsConstants.cs` for
`public static class BallPhysicsConstants`).

**`using` ordering** (FR-CS-006): System namespaces first, Unity namespaces second,
project namespaces third. Each group separated by one blank line. Alphabetical within
each group is RECOMMENDED but not enforced.

```csharp
// COMPLIANT — FR-CS-006: using order
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

using TacticalDirector.BallPhysics;
using TacticalDirector.Shared;
```

**Namespace rule** (FR-CS-007): The declared namespace matches the folder path from
`src/` root — one namespace per assembly (§4.3 flat-namespace rule). A file in
`src/ball-physics/` declares `namespace TacticalDirector.BallPhysics`. Sub-folders
within the same assembly do *not* introduce sub-namespaces.

**Partial classes:** Unity-generated partial classes (e.g., MonoBehaviour inspector
scaffolding) are permitted. Developer-authored partial classes that span logical
concerns are prohibited. A partial class is only allowed when both parts are
generated artefacts or when the Unity editor requires it.

---

### 3.1.3 Language Version and Feature Gating

**Language version pin** (FR-CS-008): The project targets the C# version shipped by
the Unity LTS revision recorded in `docs/tracking/certification-platform.md`. This FR
is currently inactive (the platform document is a placeholder — see root `CLAUDE.md`
open issue "Stage 0 host platform pin"). Once the platform is pinned, the language
version becomes a hard constraint enforced by the `.csproj` `<LangVersion>` element.

**Allowed features** (FR-CS-009):

| Feature | Condition |
|---|---|
| Records | DTOs only (data-transfer structs with no behaviour). |
| Pattern matching (`is`, `switch` expressions) | No restriction. |
| Expression-bodied members (`=>`) | Simple single-expression members only; not for multi-step logic. |
| `readonly struct` | Strongly preferred for all immutable value types. |
| Default interface methods | Only if the pinned Unity LTS C# version supports them. |

**Banned in game-logic code** (FR-CS-010):

| Feature | Reason |
|---|---|
| `dynamic` | Bypasses compile-time type safety; introduces non-deterministic dispatch paths. See Appendix D `det-banned`. |
| `async`/`await` for game-state work | Breaks deterministic tick ordering; Task continuations run on unpredictable frames. |
| `unsafe` without sign-off | Pointer arithmetic requires explicit lead-developer sign-off recorded in the PR description. |

---

### 3.1.4 Whitespace and Braces

These decisions are recorded once here and listed as permanent exclusions in §7.4 to
prevent relitigation.

- **Indentation:** 4 spaces. Tabs are prohibited (FR-CS-011). This is enforced by
  `.editorconfig` at the Stage 0+1 transition (§5.2).
- **Brace style:** Allman — opening brace on its own line (FR-CS-012).
- **`var` usage** (FR-CS-013): permitted when the type is unambiguously clear from the
  RHS. `var state = new BallState()` is clear; `var result = Compute()` is not.

```csharp
// COMPLIANT — FR-CS-011/012: 4-space indent, Allman braces
public void Update(ref BallState state)
{
    if (state.IsGrounded)
    {
        ApplyFriction(ref state);
    }
}

// VIOLATION — K&R brace style (not Allman)
public void Update(ref BallState state) {   // opening brace on same line
    if (state.IsGrounded) {
        ApplyFriction(ref state);
    }
}
```

---

### 3.1.5 Access Modifiers

Every type, method, property, field, and event declaration **MUST** carry an explicit
access modifier (FR-CS-014). Relying on C#'s implicit `private` for fields or implicit
`internal` for types is prohibited.

`internal` is permitted only for types whose consumers are all within the same assembly.
It **MUST NOT** be used to expose types to other assemblies (FR-CS-015); cross-assembly
surface must be `public` and declared via `.asmdef` references (FR-CS-055).

```csharp
// COMPLIANT — FR-CS-014: explicit on every declaration
public sealed class BallStateSystem
{
    private readonly MatchClock _clock;
    private static readonly ProfilerMarker s_marker = …;

    public BallStateSystem(MatchClock clock) { … }
    public void Update(ref BallState state) { … }
    private static void ApplyDrag(ref BallState state, float dt) { … }
}

// VIOLATION — implicit private (no access modifier on field and method)
sealed class BallStateSystem           // implicit internal on the type
{
    readonly MatchClock _clock;        // implicit private on field
    static void ApplyDrag(…) { … }    // implicit private on method
}
```

---

## 3.2 Constant Declaration & Tagging (FR-CS-016 … FR-CS-025)

*Implements:* FR-CS-016–025. See §2.2.2 for rule statements and conformance levels.

---

### 3.2.1 Citation — Constant Tag Definitions

The following table is **owned by root `CLAUDE.md` — "Constant Tags"**. It is
reproduced here verbatim as a citation for code-author convenience. This subsection
does not redefine or extend the tag vocabulary; it only applies it at code level.
If a discrepancy exists between this table and root `CLAUDE.md`, root `CLAUDE.md`
is authoritative.

| Tag | Meaning | Rule |
|---|---|---|
| `[GT]` | Gameplay-Tuned | Designer sets value; must live in tunable config |
| `[EST]` | Estimated | Placeholder; must be validated before implementation |
| `[FIXED]` | Fixed / physical law | Derived from physics; never tune |
| `[DERIVED]` | Derived from other constants | Formula must be documented; never set independently |
| `[CROSS]` | Cross-spec constant | Defined in another approved spec; consumed read-only here; never set independently in this spec. Citation must name the authoritative spec and section. Use `[CROSS]` only when the value is copied verbatim without modification — if a formula transforms it, tag the result `[DERIVED]`. |

*(Source: root `CLAUDE.md` — "Constant Tags", retrieved May 7, 2026.)*

---

### 3.2.2 Code-Level Binding Rule

Every constant in implementation code **MUST** appear in a constants catalogue file
(FR-CS-016). The rule has two components:

1. **Location:** The constant is declared in `<SpecName>Constants.cs` or
   `ProjectConstants.cs` (§4.2). It is not declared inline in formula, system, or
   struct files.
2. **Tag in XML doc comment:** The constant's CLAUDE.md tag appears in the XML doc
   comment immediately preceding the `const` or `static readonly` declaration
   (FR-CS-017). The tag is part of the doc comment, not a separate inline comment.

```csharp
// COMPLIANT — FR-CS-016, FR-CS-017: constant in catalogue; tag in XML doc comment
/// <summary>[FIXED] Ball radius in metres. Ball Physics Spec #1 §2.1.</summary>
public const float BALL_RADIUS = 0.11f;

// VIOLATION — FR-CS-016: constant declared in formula code, not catalogue
public void ApplyGravity(ref BallState state, float dt)
{
    const float gravity = 9.81f;   // must be in BallPhysicsConstants.cs
    state.Velocity.z -= gravity * dt;
}
```

---

### 3.2.3 Tag → C# Storage Class Mapping

| Tag | C# storage class | Naming | XML doc requirement | Notes |
|---|---|---|---|---|
| `[FIXED]` | `public const` | `ALL_CAPS` | Tag + spec section | Compile-time literal; inlined by compiler (FR-CS-018) |
| `[GT]` | `public static readonly` | PascalCase | Tag + config-key reference | Loaded from tunable config at boot; not a `const` (FR-CS-019) |
| `[EST]` | `public static readonly` | PascalCase | Tag + validation requirement | `// TODO: validate` on declaration line; `spec-error-log.md` entry required (FR-CS-020) |
| `[DERIVED]` | `public static readonly` | PascalCase | Tag + formula + source constants | Formula derivation cited in summary; never set independently (FR-CS-021) |
| `[CROSS]` | `public static readonly` | PascalCase | Tag + authoritative spec & section | Mirror of source-of-truth; never modified here (FR-CS-022) |

Per-tag region ordering within a catalogue file (FR-CS-025, §4.2):
`[FIXED]` → `[DERIVED]` → `[CROSS]` → `[GT]` → `[EST]`

Rationale: most-immutable to most-mutable. `[FIXED]` constants never change; `[EST]`
constants are placeholders. Readers scanning a catalogue file encounter the stable
values first. See Appendix C §C.1 for a complete worked example.

---

### 3.2.4 Magic-Number Prohibition

Any unqualified numeric literal in formula, system, or struct code is a magic number
and a FR-CS-023 violation. The fix is always the same: move the value into the
appropriate constants catalogue with the correct tag.

**Violation and compliant refactor:**

```csharp
// VIOLATION — FR-CS-023: three magic numbers
public void ApplyGravity(ref BallState state, float dt)
{
    state.Velocity.z -= 9.81f * dt;          // magic: gravitational acceleration
    if (state.Velocity.z < -35.0f)            // magic: terminal velocity cap
        state.Velocity.z = -35.0f;
}

// COMPLIANT — FR-CS-016/023: named constants from BallPhysicsConstants
public void ApplyGravity(ref BallState state, float dt)
{
    state.Velocity.z -= BallPhysicsConstants.GRAVITY * dt;
    if (state.Velocity.z < -BallPhysicsConstants.TERMINAL_VELOCITY)
        state.Velocity.z = -BallPhysicsConstants.TERMINAL_VELOCITY;
}
```

**Permitted literal exceptions** (FR-CS-024):

| Literal | Permitted context | Rationale |
|---|---|---|
| `0`, `1` | Loop-control bounds (`for (int i = 0; i < n; i++)`) | Universal loop idiom; extracting to a constant adds no clarity |
| `array.Length` | Array-length-of-self comparisons | Structural, not domain-specific |
| Expected values in unit-test assertions | `Assert.AreEqual(3, result.Count)` | Test fixture context; test values are not formula constants |
| Bit-pattern literals (e.g., `0xFFFFFFFFFFFFFFFF`) | Determinism scaffolding only | Must be annotated with `// §3.4` |

---

## 3.3 Allocation Discipline (FR-CS-026 … FR-CS-035)

*Implements:* FR-CS-026–035. See §2.2.3 for rule statements and conformance levels.
For the authoritative symbol list of prohibited constructs see Appendix D category
**"alloc-hot-path"**. This section provides mechanics only; it does not reproduce the
Appendix D symbol list.

---

### 3.3.1 Game-Loop Zero-Allocation Rule

Any method on the 60 Hz physics/render update path — directly or transitively called
by the physics or render loop entry point — must produce zero managed-memory
allocations per invocation (FR-CS-026, FR-CS-066). The budget source is
`docs/planning/development-best-practices.md`.

"Managed-memory allocation" means any operation that causes the .NET garbage collector
to allocate from the managed heap: `new` class-type expressions, boxing, closures,
LINQ chains, and all constructs listed in Appendix D category "alloc-hot-path".

The zero-allocation budget is verified at Stage 0 by code review and at Stage 1+ by a
profiler-based allocation test (§5.2, §5.6).

---

### 3.3.2 Banned Constructs in Hot-Path Code

The full list of prohibited constructs is in Appendix D category **"alloc-hot-path"**
(FR-CS-027–034). The following categories summarise the types of construct that are
banned; consult Appendix D for the specific symbols and class/method names that apply
at Stage 1 to `BannedSymbols.txt`.

- **Boxing** — value type cast to `object` or to a non-`struct` interface.
- **LINQ-to-objects** — any `System.Linq` fluent chain that returns `IEnumerable<T>`
  or materialises a new collection.
- **`params` arrays** — a declaration-site ban; callers cannot opt out.
- **String formatting** — `string.Format`, `$"…"` interpolation, and `+` concatenation
  with non-constant operands.
- **Closures** — any lambda or anonymous method that captures a local variable.
- **Non-`struct` enumerators** — `foreach` over `List<T>`, `Dictionary<K,V>`, etc.
- **Reflection** — `System.Reflection` APIs.

---

### 3.3.3 Required Allocation-Free Patterns

When a game-loop method needs to pass state across a call boundary, produce a result,
or communicate an event, the following patterns **MUST** replace allocating equivalents
(FR-CS-033). See also Appendix D category **"det-required-patterns"**.

**Ref-passed structs** — pass game state by `ref` instead of copying a class instance:

```csharp
// COMPLIANT — FR-CS-033: ref-passed struct; zero allocation on call
public static void ApplyDrag(ref BallState state, float dt)
{
    state.Velocity *= 1.0f - BallPhysicsConstants.DRAG_COEFFICIENT * dt;
}

// VIOLATION — class-based state forces heap allocation or boxing on cast
// public static void ApplyDrag(IBallState state, float dt) { … }
```

Additional required patterns:

| Pattern | When to use |
|---|---|
| Pre-allocated fixed-size buffer | When a method needs a temporary array of bounded size |
| Object pool | For rare-path allocations (e.g., one-time match-start setup) where zero-alloc patterns are impractical |
| Struct-based events | For cross-assembly event dispatch (FR-CS-047, §3.5.2) |
| `stackalloc` | For transient buffers whose size is statically bounded (FR-CS-035) |

---

### 3.3.4 Presentation / Client (Non-Loop) Allocation Budget

Code in the **Presentation and Client tiers** (§3.5.2 tiers 8 and 9), plus the Unity host code outside the gate, is not on the 60 Hz game-loop path. Its allocation budget is
**< 1 MB per frame** (FR-CS-067), sourced from
`docs/planning/development-best-practices.md`. The "alloc-hot-path" bans (§3.3.2)
are relaxed for code in these tiers, except where a method there directly calls
into a game-loop system.

---

### 3.3.5 Verification

At Stage 0: manual code review — the reviewer checks that no banned construct appears
in any method that is transitively called by the physics loop (§5.1).

At Stage 0+1: Unity Profiler allocation track + a custom Unity analyzer (§5.2) flag
allocations in game-state assemblies.

---

## 3.4 Determinism in Code (FR-CS-036 … FR-CS-045)

*Implements:* FR-CS-036–045. See §2.2.4 for rule statements and conformance levels.

---

### 3.4.1 Citation — Root CLAUDE.md Determinism Rules

The determinism rules for this project are **owned by root `CLAUDE.md` — "When
Writing Code"**. The binding rules reproduced here are derived from that source;
if a discrepancy exists, root `CLAUDE.md` is authoritative.

Relevant excerpt (paraphrased for citation; the binding text is in §2.2.4 FR-CS-036–045):

> Deterministic replay is a hard requirement — no `System.Random`, no `DateTime.Now`
> in game logic. SplitMix64 for deterministic RNG. In Python tooling: omit `UL` suffix
> from C# constants; mask all intermediate multiplications with `& 0xFFFFFFFFFFFFFFFF`.
>
> *(root `CLAUDE.md` — "When Writing Code", retrieved May 7, 2026)*

This section binds those rules to enforceable code shapes. It does not restate the
underlying rationale (single-machine determinism via state snapshots, Stage 5+
Fixed64 migration — both owned by root `CLAUDE.md`).

---

### 3.4.2 Banned APIs in Game-Logic Code

The authoritative symbol list is Appendix D category **"det-banned"**
(FR-CS-036–040). This section provides mechanics only.

Any API in the "det-banned" category that appears in a game-state assembly is a
FR-CS-036–040 violation and triggers Mode 1 (Review Block) in §2.3. The benchmark
carve-out in §3.9.5 applies only to files explicitly marked `// benchmark-only` and
excluded from the game-state assembly graph.

The categories of banned API:

- **Non-deterministic RNG** (FR-CS-036) — Appendix D "det-banned".
- **Wall-clock time** (FR-CS-037) — Appendix D "det-banned".
- **Process-unique identifiers** (FR-CS-038) — Appendix D "det-banned".
- **Multithreaded game-state** (FR-CS-039) — Appendix D "det-banned".
- **Hardware-intrinsic FMA** (FR-CS-040) — Appendix D "det-banned"; override
  requires sign-off + platform pin.

---

### 3.4.3 Required APIs and Patterns in Game-Logic Code

The authoritative lists are Appendix D categories **"det-required-apis"** and
**"det-required-patterns"** (FR-CS-041–045).

**Required APIs** (Appendix D §D.3):

| Need | Required API | FR-CS-### |
|---|---|---|
| Random number generation | `SplitMix64` helper | FR-CS-041 |
| Simulation time | `MatchClock` (injected) | FR-CS-042 |
| Trigonometry / math | Project math helper | FR-CS-043 |
| Performance profiling | `ProfilerMarker` | FR-CS-070 |

**Required patterns** (Appendix D §D.4): see §3.4.4 below for the two distinct
64-bit multiplication rules.

---

### 3.4.4 64-Bit Multiplication Semantics

Two separate rules apply in two separate contexts. They must not be conflated.

**Rule A — C# game-logic code (FR-CS-044):**

Where a 64-bit intermediate multiplication appears in a seed or hash chain (e.g., a
SplitMix64 step), the multiplication **MUST** be wrapped in `unchecked { … }`. This
makes the intended 64-bit truncation explicit to the reader and suppresses the C#
checked-context overflow exception that would otherwise fire in debug builds.

```csharp
// COMPLIANT — FR-CS-044: unchecked scope makes truncation explicit
// SplitMix64 step; §3.4.4 C# rule
private static ulong SplitMix64Step(ref ulong state)
{
    unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
```

**Rule B — Python (or other non-C#) tooling that mirrors C# constants (FR-CS-045):**

Where Python tooling replicates a SplitMix64 step or other hash chain to verify
`[FIXED]` / `[DERIVED]` constants, every intermediate 64-bit multiplication **MUST**
be masked with `& 0xFFFFFFFFFFFFFFFF`. The C# `UL` integer suffix **MUST** be omitted
(Python integers are arbitrary-precision; the suffix has no meaning and causes a
syntax error).

```python
# COMPLIANT — FR-CS-045: mask intermediates; no UL suffix
# SplitMix64 step; §3.4.4 Python rule
def splitmix64_step(state: int) -> tuple[int, int]:
    state = (state + 0x9E3779B97F4A7C15) & 0xFFFFFFFFFFFFFFFF  # no UL suffix
    z = state
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & 0xFFFFFFFFFFFFFFFF
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & 0xFFFFFFFFFFFFFFFF
    return state, z ^ (z >> 31)
```

These two rules are kept separate so neither infects the other's domain: C# game code
uses `unchecked` (a language scope); Python tooling uses bitwise masking (a runtime
operation). Mixing them — adding `& 0xFFFFFFFFFFFFFFFF` to C# code, or using `unchecked`
in Python — is incorrect and confuses the semantics of each rule.

---

## 3.5 Dependency Direction & Interface Design (FR-CS-046 … FR-CS-055)

*Implements:* FR-CS-046–055. See §2.2.5 for rule statements and conformance levels.

---

### 3.5.1 Citation — Root CLAUDE.md Interface Principle

The interface design principle for this project is **owned by root `CLAUDE.md` —
"Interface Design Principle"**:

> **Write interfaces only when both sides are specified.** Do not create interfaces
> against unspecified systems. This avoids phantom interface proliferation (ERR-001,
> ERR-004).
>
> *(root `CLAUDE.md` — "Interface Design Principle", retrieved May 7, 2026)*

This subsection does not redefine the principle; it binds it to file-level placement
rules (§3.5.3) and extends it with the tier-order and event-dispatch rules below.

---

### 3.5.2 Tier Order and Dependency Arrows

Assembly references must flow in one direction only (FR-CS-046). The canonical order
is a **ten-tier** order covering every assembly folder in `src/`. Two terms used by
the table are fixed here so every row means the same thing by them: the **gameplay
tiers** are tiers 1–4 — Physics, Configuration, Mechanics and AI, the tiers that
decide what happens on the pitch — and the table *covers* the two
**Infrastructure** assemblies without *ordering* them: their row is out of band
(FR-CS-046b binds them by name), and it must never be folded into the numbered
order:

| Tier | Assemblies | Why this tier |
|---|---|---|
| 0 **Foundation** | `project-constants`, `deterministic-sim`, `event-system` | Referenceable by everything; reference nothing but each other. |
| 1 **Physics** | `ball-physics`, `agent-movement`, `collision-system`, `first-touch`, `pass-mechanics`, `shot-mechanics`, `heading-mechanics`, `goalkeeper-mechanics` | Ball, body and contact. Parameter-driven — no type enums (see root `CLAUDE.md`). |
| 2 **Configuration** | `tactical-instructions` (#21) | May be referenced by Mechanics, AI, Data, Composition, Management, Presentation and Client — everything above; references only `project-constants`. Today's consumers are Mechanics (all four), AI's `decision-tree` (`perception-system` does not reference it), Composition's `match-engine`, and Client's `match-client-core`, `match-client-web` and `ui-framework` — no Data or Management or Presentation assembly, and neither `client-app` nor `match-client-unity`, references it yet. A separate tier below Mechanics states that one-way relationship outright rather than burying it in an intra-tier edge; seating it *inside* Mechanics would be legal (`decision-tree` → Mechanics is downward and already exists) but would make the four Mechanics assemblies' dependence on it invisible to the order. **No Physics assembly references it**, so seating it above Physics keeps the physics tier parameter-only. |
| 3 **Mechanics** | `positioning-ai`, `pressing-ai`, `defensive-ai`, `attacking-ai` | Off-ball and on-ball behaviour over the physics primitives. |
| 4 **AI** | `decision-tree`, `perception-system` | Choice and what a player can know. |
| 5 **Data** | `player-database` (#27) | May be referenced by Composition, Management, Presentation and Client — everything above; references only `deterministic-sim`. Today's consumers are Composition's `match-engine`, five of the six Management assemblies (`player-progression`, `training-system`, `injuries-medical`, `discipline`, `season-save` — not `living-world`) and Client's `match-client-core` — and by **no gameplay-tier assembly**. Seating it above AI preserves that: the gameplay tiers keep operating on struct parameters, not squad rows. |
| 6 **Composition** | `match-engine` | References all four gameplay tiers plus Data; the only assembly that does. Not a numbered spec — governed by `docs/tracking/match-engine-design.md`. |
| 7 **Management** | `living-world` (#22), `player-progression` (#28), `training-system` (#29), `injuries-medical` (#41), `discipline` (#44), `season-save` (#30) | Long-horizon state above a single match. |
| 8 **Presentation** | `match-viewer`, `match-analytics` (#37) | Derived from a played match. This tier is what keeps the root `CLAUDE.md` rule that **no sim assembly may reference `match-analytics`** true. |
| 9 **Client** | `match-client-core`, `ui-framework` (#38), `client-app`, `match-client-unity`, `match-client-web` | Screens, shells and hosts. |
| — **Infrastructure** | `performance-optimization` (#18), `testing-strategy` (#19) | Out of band: not members of the order, and no tier may reference them at runtime. |

```
  Foundation ──► Physics ──► Configuration ──► Mechanics ──► AI ──► Data
      ──► Composition ──► Management ──► Presentation ──► Client

        ──►  reads "is available to"

        NO upward references permitted (FR-CS-046)
```

Stated the other way round, in the words the root `CLAUDE.md` uses: **AI → Mechanics →
Physics, never the reverse** — an assembly may reference assemblies below it in the
order, never above. The two notations are the same rule; `──►` above points from the
provider to the consumer, the root `CLAUDE.md` arrow points from the consumer to the
provider. Both files label their arrow so the reader never has to reconstruct which
convention is in force.

**A tier is a ceiling, not a licence.** An individual spec may forbid a reference the
tier order permits — #44 Discipline sits in Management but its FR-DC set forbids it to
reference `match-engine` or `season-save`, and the composition root mediates instead.
Where a spec is stricter, the spec wins.

**Intra-tier references are permitted; intra-tier cycles are not.** An assembly MAY
reference another assembly in the same tier (`pressing-ai` → `positioning-ai` is the
standing example), but the assembly reference graph as a whole MUST remain acyclic
(FR-CS-046a). This is already enforced mechanically — Unity rejects circular `.asmdef`
references, and `tools/dotnet-ci/generate_projects.py` emits one `<ProjectReference>`
per `.asmdef` reference, so a cycle also fails the Linux compile gate. It is written
down because a build error reports what broke, not why the constraint exists.

**Placement rule for new assemblies.** A commit that adds a production
`src/<folder>/<name>.asmdef` **MUST** place that folder in the table above in the
same commit — the table enumerates, so an unamended table is stale the moment the
folder lands. The seating is a **bound plus a justified choice**, not a derivation:
an assembly **MUST NOT** be seated at or below the tier of any assembly it
references, except that FR-CS-046a permits seating it *at* the tier of an assembly
it references intra-tier (`pressing-ai` → `positioning-ai` is the standing example).
Within that bound the tier **is a design choice** and **MUST** be justified in the
row's *Why this tier* cell — the justification is what the table's third column
exists to hold, and a brief characterisation of the tier's role ("Long-horizon state
above a single match") satisfies it for an assembly that fits the characterisation.
The bound is exactly FR-CS-046/046a read from the seating side, so it holds of a
table iff the graph has no upward reference. *(An earlier form of this rule stated
the seating as an equality — "the lowest tier strictly above every assembly it
references" — which 27 of the 33 ordered seatings do not satisfy, would have
mandated seating a Management-shaped assembly that reads only `player-database`
into Composition, and was unsatisfiable for the four assemblies whose highest
reference is tier 9; restated as the bound at v1.4.)* The two out-of-band
**Infrastructure** assemblies are outside the seating rule twice over: the
references they source seat them in no tier — FR-CS-046b binds them instead — and a
new assembly cannot acquire a tier through them, since no ordered tier may reference
them at runtime. Because the table is **folder-keyed**, a top-level `src/<folder>/`
**MUST** hold exactly one production `.asmdef`: a second one under the same folder
(at any nesting depth outside `[Tt]ests/`) has no seat of its own and is a hard
failure of the mechanical check below — this constraint was previously enforced by
the tool but written nowhere.

This rule is enforced mechanically by `tools/assembly-tier-check.py`
(`python3 tools/assembly-tier-check.py --repo .`), **run on every push and pull
request by the `Spec hygiene checks` job in `.github/workflows/ci.yml`**. The tool
re-parses the table above rather than carrying its own copy, enumerates every
production `src/<folder>/<name>.asmdef`, and fails on a folder absent from the
table, a table entry naming no existing folder, a top-level folder holding more
than one production `.asmdef`, a tier row whose *Why this tier* cell is empty, any
upward reference, any reference that breaks FR-CS-046b in either direction (an
ordered-tier assembly referencing Infrastructure, or an Infrastructure assembly
referencing anything other than tier 0 or its Infrastructure peer), any cycle, or
an Infrastructure row that no longer names exactly the assemblies FR-CS-046b binds
— membership in the out-of-band set is asserted **by name** against FR-CS-046b's
own list in §2.2.5 (parsed, not duplicated), so reseating that row into the
numbered order cannot silently disable the FR-CS-046b checks — re-running the
adoption verification below on every invocation instead of leaving it a one-off
hand check. Two halves of the placement rule remain review matters, not mechanical
ones: whether the table was amended *in the same commit* as the `.asmdef` (CI sees
only the resulting tree, so a stale table fails the next run, whichever commit
caused it), and whether a *Why this tier* justification is *adequate* — the tool
verifies only that the cell is non-empty.

**Verification at adoption (August 17, 2026).** The order was derived from the
`.asmdef` reference graph, not from folder names, and checked against it: all **35**
production assembly folders in `src/` are placed, none is named that does not exist,
and across every production `.asmdef` reference there are **zero upward references**.
The 148 production→production references partition as **105 downward, 38 intra-tier, and
5 sourced by the two out-of-band Infrastructure assemblies** (`performance-optimization`
→ `deterministic-sim`, `project-constants`; `testing-strategy` → `deterministic-sim`,
`performance-optimization`, `project-constants`), which the order does not rank — all of
them already present and the whole graph acyclic. Quoting 105 + 38 as if it were the
whole is what makes the count unreconcilable for a reader who re-derives it: the total is
148, and `tools/assembly-tier-check.py` prints all four figures for exactly that reason. Adopting the order therefore changed nothing that compiles; it
constrains only what can be written next. Test assemblies (`src/*/[Tt]ests/`) are **not**
members of the order and are excluded from the check: a test assembly legitimately
references upward (`event-system.Tests` → `decision-tree`), which is why FR-CS-046 binds
production assemblies only.

**Struct-event upward flow** (FR-CS-047): when an event must propagate upward through
the tier order (e.g., a physics system notifying an AI system), it is dispatched as
a `struct` via a pre-allocated event bus, not as a class-based `event Action<T>` or
delegate. This keeps the physics assembly free of any reference to the AI assembly
while preserving the event-notification pattern.

---

### 3.5.3 Interface Placement Rule

An `interface` file **MUST** reside in the same assembly as at least one of its
specified consumers (FR-CS-048). This is the file-level binding of root `CLAUDE.md`'s
"both sides specified" principle.

**Phantom interface anti-pattern** (FR-CS-049): placing an `interface` in a folder or
assembly whose consumer side is not yet written is the phantom interface error
documented in `docs/tracking/spec-error-log.md` as ERR-001 and ERR-004. The correct
response is to delay creating the interface until the consumer spec is approved.

```
// VIOLATION — phantom interface: IGoalkeeperResponse defined in shot-mechanics
// assembly but GoalkeeperMechanics (#11) is NOT YET SPECIFIED.
// src/shot-mechanics/IGoalkeeperResponse.cs   ← FR-CS-049 violation
interface IGoalkeeperResponse { … }

// COMPLIANT — no interface declared; ShotExecutedEvent (a struct) is published
// instead. GoalkeeperMechanics subscribes when it is specified and implemented.
// src/shot-mechanics/ShotExecutedEvent.cs     ← struct event; no interface needed
public readonly struct ShotExecutedEvent { … }
```

---

### 3.5.4 Event-vs-Interface Decision Tree

Apply this decision tree when choosing a cross-boundary communication mechanism for a
new system interaction (FR-CS-050). Document the chosen path in the file header's
purpose field.

```
Cross-assembly communication needed?
│
├─ NO ──► Direct method call within assembly.
│
└─ YES ─► Is the consumer assembly specified
           (written or in an approved Stage 0 spec)?
           │
           ├─ NO ──► Do NOT create an interface or event type yet.
           │          Wait. Creating either now risks phantom interfaces
           │          (FR-CS-049) or prematurely locking a contract.
           │
           └─ YES ─► Will there be more than one implementation
                      of the producer-side contract?
                      │
                      ├─ YES ─► Interface (FR-CS-048).
                      │          Place in consumer's assembly.
                      │
                      └─ NO ──► Is this a cross-tier notification
                                 (producer in a lower tier than consumer)?
                                 │
                                 ├─ YES ─► Struct event on event bus
                                 │          (FR-CS-047). Zero-alloc;
                                 │          no upward assembly reference.
                                 │
                                 └─ NO ──► Direct method call.
                                            (Intra-tier or downward call.)
```

---

### 3.5.5 Anti-Patterns Prohibited in Game-Logic Code

Each of the following patterns is banned in game-state assemblies (FR-CS-051–054).
Each entry: the pattern, the rule, and a one-line rationale.

| Anti-pattern | FR-CS-### | Rationale |
|---|---|---|
| **Service locator** — a global registry that resolves service instances by type key (e.g., `ServiceLocator.Get<IPhysicsService>()`) | FR-CS-051 | Hides dependencies; makes deterministic testing impossible without replacing the global registry |
| **Ambient context** — a static property returning the "current" instance (e.g., `MatchContext.Current`) | FR-CS-052 | Creates hidden state; breaks determinism in multi-test runs and replay rewind |
| **Static mutable singleton** — a `static` field holding a mutable instance shared by all callers | FR-CS-053 | Cannot be reset between deterministic replay ticks; violates single-machine replay guarantees |
| **Generic DI container in game loop** — IoC frameworks (Zenject, VContainer, `Microsoft.Extensions.DependencyInjection`) on the hot path | FR-CS-054 | Reflection-based resolution allocates; breaks zero-allocation budget (FR-CS-026) |

Dependency injection is not prohibited. Constructor injection — passing dependencies
as constructor parameters — is the required pattern. The prohibition is on *runtime
resolution* machinery in the game loop, not on the dependency-injection principle.

---

## 3.6 Documentation Conventions (FR-CS-056 … FR-CS-065)

*Implements:* FR-CS-056–065. See §2.2.6 for rule statements and conformance levels.

---

### 3.6.1 Citation — Root CLAUDE.md Documentation Rules

Two documentation rules are **owned by root `CLAUDE.md` — "When Writing or Editing
Specs"** and bound here to C# code:

> - Include creation date and purpose header on every new file.
> - Append a version history entry to every modified file.
>
> *(root `CLAUDE.md` — "When Writing or Editing Specs", retrieved May 7, 2026)*

Additionally, the inline-comment policy is **owned by root `CLAUDE.md` — "When
Writing Code"**:

> Default to writing no comments. Only add one when the WHY is non-obvious.
>
> *(root `CLAUDE.md` — "When Writing Code", retrieved May 7, 2026)*

This subsection binds these rules to C# file and code structures; it does not
restate their rationale.

---

### 3.6.2 File Header

Every new `.cs` file **MUST** open with the file header block defined in Appendix A
(FR-CS-056, FR-CS-057). The required fields are: file path, created date, last-modified
date, author, spec-citation list, and a purpose statement of ≤ 2 sentences.

The `Modified:` field **MUST** be updated on every change and **MUST** match the date
of the latest row in the version-history block (§3.6.3). A mismatch is a FR-CS-057
violation.

See Appendix A for the paste-ready template and a populated example.

---

### 3.6.3 Version-History Block

The version-history block lives in a `#region VersionHistory … #endregion` at the
**end** of every `.cs` file (FR-CS-058, FR-CS-059). It is never placed mid-file or
interspersed with logic. New rows are appended; rows are never deleted or edited
(they are the change log).

See Appendix B for the paste-ready template and column rules.

---

### 3.6.4 XML Doc Comments

| Target | Required? | Governing FR | Notes |
|---|---|---|---|
| `public` type (class, struct, enum, interface, delegate) | MUST | FR-CS-060 | `<summary>` required; `<remarks>` optional |
| `public` or `protected` method, property, event | MUST | FR-CS-060 | Include `<param>` for non-obvious parameters |
| Constant declaration (any access modifier) | MUST | FR-CS-061 | Satisfies both FR-CS-060 (if public) and FR-CS-061 simultaneously |
| `private`/`internal` type or member | SHOULD | FR-CS-060 | Not enforced at Stage 0; enforced at Stage 1 analyzer level |

Public constants are covered by both FR-CS-060 and FR-CS-061. A single XML doc comment
satisfies both requirements simultaneously; no duplication is needed.

```csharp
// COMPLIANT — FR-CS-060 + FR-CS-061: one doc comment satisfies both FRs
/// <summary>[FIXED] Ball radius in metres. Ball Physics Spec #1 §2.1.</summary>
public const float BALL_RADIUS = 0.11f;

// COMPLIANT — FR-CS-061 (non-public constant also requires a doc comment)
/// <summary>[GT] Internal substep count. Loaded from config at boot.</summary>
private static readonly int _maxSubsteps = 4;
```

---

### 3.6.5 Cross-Reference Comment Style

Typed cross-reference IDs must be used whenever a comment references another spec,
formula, edge case, or error log entry (FR-CS-062, FR-CS-063; root `CLAUDE.md` —
"Cross-Reference System"). The four prefixes and their usage:

```csharp
// XC-001-001: depends on BallState layout defined in Ball Physics Spec #1 §3.1.
//             Coordinate convention: X goal-to-goal, Y touchline, Z vertical up.

// FM-001: drag model — v' = v × (1 − DRAG_COEFFICIENT × dt)

// EC-012: ball resting at Z = BALL_RADIUS (0.11 m), not Z = 0.

// ERR-016-002: EntityId no-reuse constraint from Deterministic Simulation #16 §3.2.5.
//              Back-propagated to Agent Movement #2 §2.5 and Decision Tree #8 §1.7.3.
```

IDs **MUST** match identifiers defined in their owning specification (FR-CS-063).
Never fabricate an ID. If the referenced cross-spec binding does not yet have an
assigned ID in its owning spec, leave a `// TODO: assign XC-###` comment rather than
inventing a number.

---

### 3.6.6 Inline Comment Policy

Inline comments follow the policy owned by root `CLAUDE.md` — "When Writing Code"
(FR-CS-064): write a comment only when the **WHY** is non-obvious. Well-named
identifiers, explicit constants, and XML doc comments carry the *what*. An inline
comment is warranted only for:

- A hidden constraint (e.g., an ordering dependency that is not visible from the call
  site).
- A subtle invariant that would surprise a reader.
- A workaround for a specific external bug.
- A rule citation (e.g., `// §3.4.4`, `// FR-CS-026`).

Comments that explain *what* the code does, reference the current task, or
attribute code to a caller are prohibited by the same CLAUDE.md policy.

**Commented-out code** (FR-CS-065) is prohibited in any commit merged to a shared
branch. If code is temporarily disabled during development it must be deleted; version
control preserves the history. Leaving commented-out code signals unfinished work and
creates reviewer confusion about whether the block is meant to be restored.

---

## 3.7 Numeric Type Discipline (FR-CS-071 … FR-CS-073)

*Implements:* FR-CS-071–073. See §2.2.8 for rule statements and conformance levels.
These rules cite root `CLAUDE.md` — "When Writing Code" as their source.

---

### 3.7.1 Stage 0: `float` Throughout Game Logic

All continuous numeric quantities in game-logic code at Stage 0 use `float`
(FR-CS-071). This includes positions, velocities, angles, forces, attribute values,
and time deltas. The rule applies to **every production assembly under `src/`** —
Foundation through Client, *including* the two out-of-band **Infrastructure**
assemblies (`performance-optimization`, `testing-strategy`), which acquire no tier
under FR-CS-046 but are production code all the same — FR-CS-071 itself carries no narrower scoping, and the
retired three-layer wording (*"the Physics, Mechanics, and AI layers"*) was near-vacuous
under the four-layer taxonomy but under the ten-tier order would exclude six tiers,
`deterministic-sim`, `player-database`, `match-engine`, `season-save`, `discipline` and
`match-analytics` among them.

The decision to use `float` at Stage 0 is owned by root `CLAUDE.md` — "When Writing
Code": *"Stage 0 uses float. Fixed64 migration is a Stage 5+ concern."* Single-machine
determinism is achieved via state snapshots, not by fixed-point arithmetic.

---

### 3.7.2 `double` Requires Sign-Off

`double` is banned in game-logic code by default (FR-CS-072). Override requires both:

1. Lead-developer sign-off in the PR description.
2. An inline comment at the use site citing the rationale.

The dual-condition override pattern matches FR-CS-040 (FMA) and FR-CS-010 (`unsafe`).
The rationale for the ban: mixing `float` and `double` in physics computations
introduces implicit widening conversions that obscure the true precision budget and
complicate any future Fixed64 migration at Stage 5+.

---

### 3.7.3 `decimal` Always Banned

`decimal` is prohibited in game-logic code at every stage (FR-CS-073). `decimal` is
a base-10 type designed for financial calculations; it is not appropriate for physics
simulation. Its precision model is incompatible with the Stage 5+ Fixed64 migration
target.

---

### 3.7.4 Stage 5+ Fixed64 Transition

When Spec #9 (Fixed64 Math Library) activates at Stage 5+, the numeric type for
game-logic code changes from `float` to the Fixed64 type defined in that spec. The
transition is out of scope for Spec #20. See Spec #9 for the Fixed64 API surface and
root `CLAUDE.md` for the transition timeline.

---

## 3.8 Worked Examples Index

Full exemplar source files are in Appendix C. The table below maps rule areas to the
specific lines in those files where each rule is demonstrated.

| Rule area | FR-CS-### | Exemplar file | Where |
|---|---|---|---|
| Naming (PascalCase, `_camelCase`, `ALL_CAPS`) | FR-CS-001–004 | Appendix C §C.1 and §C.2 | Class names, field names, const names |
| File layout (one type, `using` order, namespace) | FR-CS-005–007 | Both exemplars | Top of each file |
| Language features allowed / banned | FR-CS-009–010 | Appendix C §C.2 | `sealed` class, no `dynamic` |
| 4-space indent, Allman braces | FR-CS-011–012 | Both exemplars | All method bodies |
| Explicit access modifiers | FR-CS-014–015 | Both exemplars | Every declaration |
| All five tag types, per-tag region ordering | FR-CS-016–025 | Appendix C §C.1 | Constants regions |
| Ref-passed struct, no boxing | FR-CS-026–035 | Appendix C §C.2 | `Update(ref BallState state)` |
| MatchClock injection, no DateTime.Now | FR-CS-041–042 | Appendix C §C.2 | Constructor + `Update` |
| `unchecked` 64-bit multiplication | FR-CS-044 | §3.4.4 code block | Inline in §3.4.4 |
| Python masking rule | FR-CS-045 | §3.4.4 code block | Inline in §3.4.4 |
| Struct event (no phantom interface) | FR-CS-047–049 | Appendix C §C.2 | `PublishBounceEvent` method |
| File header template | FR-CS-056–057 | Both exemplars | Top of each file |
| Version-history `#region` | FR-CS-058–059 | Both exemplars | End of each file |
| XML doc comments | FR-CS-060–061 | Both exemplars | All public members and constants |
| Cross-reference comment style | FR-CS-062–063 | Appendix C §C.2 | `XC-001-001` comment on `Update` |
| ProfilerMarker | FR-CS-070 | Appendix C §C.2 | `s_updateMarker` + `using (…Auto())` |
| `float` throughout | FR-CS-071 | Appendix C §C.2 | `float dt`, `state.Velocity` |

---

## 3.9 Edge Cases (Rule-Application Carve-Outs)

These carve-outs narrow the applicability of Spec #20 rules. Each entry states the
scope, the rule modification, and the required comment marker.

---

### 3.9.1 Generated Code

Unity-generated partial classes, asset-import scripts, and code produced by
`dotnet build` source generators are **excluded** from Spec #20 in full. They do not
require file headers, version-history blocks, or naming compliance.

*Required marker:* None (generated files should not be manually annotated).

*Configuration:* Generator configuration is tracked in `src/CLAUDE.md` when coding
begins. In-project hand-written wrappers around generated types **MUST** conform to
all Spec #20 rules.

---

### 3.9.2 Third-Party Imports

Vendored third-party source files are included as-is and are excluded from Spec #20.
In-project wrapper types that adapt third-party code for use in game-state assemblies
**MUST** fully conform. The wrapper, not the vendored code, is the Spec #20 boundary.

*Required marker:* A comment at the top of the wrapper file:
`// Third-party wrapper — Spec #20 applies to this file, not to the vendored source.`

---

### 3.9.3 Editor-Only and Offline Tooling Code

Level editors, debug UI, and offline content-authoring scripts **SHOULD** conform to
all Spec #20 style and documentation rules. The allocation rules in §3.3 **MAY** be
relaxed for code that runs offline and never touches the game loop.

*Required marker:* `// §3.9.3 offline-tooling — allocation rules relaxed`

---

### 3.9.4 Test Fixtures

Test fixtures are split into three categories with different relaxations:

**Determinism-harness tests** (tests exercising Spec #16 / Spec #19 determinism
contracts) **MUST** follow §3.4 in full. No relaxation. These tests are part of the
deterministic-correctness guarantee and must not introduce non-determinism.

**General unit tests** (logic, formula, edge-case coverage) **MUST** conform to
naming, documentation, and §3.4 banned-API rules. Allocation rules (§3.3) **MAY** be
relaxed in test bodies (setup, assertion, teardown) with a comment citing §3.9.4.

**Property-based / fuzz tests MAY** use a non-deterministic source *for seed
selection only*, provided the executed test body routes through `SplitMix64` with the
recorded seed and the seed is logged on failure.

*Required marker for general unit test allocation relaxation:*
`// §3.9.4 general-unit-test — allocation rules relaxed in test body`

*Required marker for property-based / fuzz tests using a non-deterministic seed source:*
`// §3.9.4 property-based — non-deterministic seed source; SplitMix64 routes test body with recorded seed`

---

### 3.9.5 Benchmark / Micro-Perf Scaffolds

Benchmark files **MAY** use `System.Diagnostics.Stopwatch` and other APIs that appear
in Appendix D category "det-banned", provided:

1. The file is explicitly marked `// benchmark-only` in its header.
2. The file's `.asmdef` is excluded from the game-state assembly reference graph.
3. No production game-logic code imports the benchmark assembly.
4. The benchmark `.csproj` **MUST NOT** reference
   `Microsoft.CodeAnalysis.BannedApiAnalyzers`. The package operates per-project at the
   symbol level: if the benchmark project references the analyzer, the seed entries from
   Appendix D §D.1 fire inside the benchmark assembly and the carve-out is voided.
   Assembly-level isolation alone is insufficient; the analyzer reference must also be
   absent from the benchmark project file.

These files are not subject to the zero-allocation rule; they are measurement
infrastructure, not gameplay code.

*Required marker:* `// benchmark-only — §3.9.5: det-banned APIs permitted in this
file; excluded from game-state assembly graph; BannedApiAnalyzers not referenced.`

---

## 3.10 Constants Catalogue

Spec #20 is a meta-specification. It declares **no physical constants** and introduces
no numeric values that require `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]`
tags.

The tag vocabulary itself is governance metadata owned by root `CLAUDE.md` — "Constant
Tags". This section is retained per the CLAUDE.md 9-section template (KD-3 in §1.3).
For a spec that does carry a substantive constants catalogue (e.g., Deterministic
Simulation #16), the per-tag region ordering defined in §3.2.3 and §4.2 applies.

---

## 3.11 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 3. All eleven subsections present. Appendix D cited by category name in §3.3.2 and §3.4.2; no symbol lists duplicated. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fixes: (a) §3.2.1 [CROSS] tag-table row restored to verbatim CLAUDE.md text — missing phrase "without modification" added (closes audit finding H-01); (b) §3.9.4 added required marker for property-based / fuzz tests with non-deterministic seed source (closes L-04); (c) §3.9.5 added criterion #4 requiring benchmark `.csproj` to omit the `BannedApiAnalyzers` package reference (closes M-B — assembly-level isolation alone is insufficient). | — |
| 1.1 | August 17, 2026 | Claude Code | **`ERR-020-002` + `ERR-020-003` adopted by owner decision.** §3.5.2 replaced: the three-gameplay-layer box (which placed 14 of the 35 assembly folders — leaving 21 undecided; figures re-derived August 17, 2026 by counting the retired box, see the 1.2 row — and left the stale empty `UI (Stage 1+ — not specified yet)` row) becomes the **ten-tier order** covering all 35, derived from the `.asmdef` reference graph and re-verified at adoption — 0 upward references, 105 downward, 38 intra-tier, graph acyclic. Adds **FR-CS-046a** (intra-layer references permitted, intra-layer cycles not), the tier-is-a-ceiling rule (#44 Discipline as the worked case), the explicit test-assembly exclusion, and — closing `ERR-020-003` — an arrow label (`──►` reads "is available to") plus the root `CLAUDE.md` sentence verbatim, so both files state one rule in one vocabulary. Header corrected: it read `Version 1.0 / Status DRAFT` against a §3.11 row at 1.0.1 and a SPEC_INDEX status of APPROVED. | — |
| 1.2 | August 17, 2026 | Claude Code | **Adversarial-review findings H4 + H7.** H4: the 1.1 row above originally said the retired three-gameplay-layer box "placed 19 of the 35 assembly folders"; the true figure is **14** (8 Physics + 4 Mechanics + 2 AI, `UI` row empty), leaving 21 undecided — corrected in place, **re-derived by counting the retired box** (`git show 0e78d381~1`) rather than rescaled from the earlier 31-assembly error-log count, which is how the wrong 19 arose. H7: §3.5.2 gains the **placement rule for new assemblies** — a commit adding a production `.asmdef` MUST amend the table in the same commit, and a tier is derived (lowest tier strictly above every referenced assembly) unless a lower seating is stated with its reason — enforced mechanically by the new `tools/assembly-tier-check.py`, which parses the table rather than duplicating it and re-runs the adoption verification on every invocation. **⚠️ ANNOTATED (v1.4, August 18, 2026): this row is INCOMPLETE of its own commit** — the same commit also rescoped §3.3.4 from "the UI layer" to the Presentation and Client tiers (FR-CS-067's mechanics), rewrote §3.7.1's scoping sentence off the retired three-layer wording, rewrote the §3.5.2 adoption-verification paragraph (the 105 + 38 partition became 105/38/5 of 148, naming the five Infrastructure-sourced references), and changed the tier-2 *Why this tier* cell's rationale; none was recorded until this annotation and the 1.4 row below. Left in place per the annotate-don't-rewrite convention. | — |
| 1.3 | August 17, 2026 | Claude Code | **Adversarial-review findings L1 + L2, both re-verified against the `.asmdef` reference graph.** L1: the tier-2 (`tactical-instructions`) and tier-5 (`player-database`) "Why this tier" cells stated their consumer sets as exhaustive facts that were false — eleven assemblies above tier 2 do not reference `tactical-instructions` (`player-database`, all six Management assemblies, both Presentation assemblies, `client-app`, `match-client-unity`) and `living-world` (Management) does not reference `player-database`. Both cells recast as permission ("may be referenced by … and everything above") plus a separately-stated today's-consumers list, so the two claims cannot drift apart again. L2: §3.5.2's heading ("Layer Order and Dependency Arrows" → **"Tier Order and Dependency Arrows"**), its §3.5.1 forward-reference ("layer-order … rules below" → "tier-order … rules below"), its intra-tier paragraph ("Intra-layer references are permitted; intra-layer cycles are not" → "Intra-tier"), and its FR-CS-047 sentence ("propagate upward through the layer order" → "tier order") standardised on **tier**, matching FR-CS-046a in §2.2.5 and §5.4.5 item 1, both of which already said "tier" *(⚠️ CORRECTED at v1.4, August 18, 2026: the §5.4.5 half of this claim was FALSE — item 1's checkbox title still read "**Layer order** —" until section-5.md v1.0.3 standardised it; §2.2.5's FR-CS-046a did already say "tier")*. `git grep -n '3.5.2 Layer Order'` found two prose citations of the old heading text in `docs/tracking/spec-error-log.md` (§4430, §4622) — neither is a markdown anchor link (no generated `#325-layer-order…` anchor is referenced anywhere in the tree), so nothing breaks; `spec-error-log.md` is outside this pass's owned-file list and is left for its own citation-refresh pass. The retired three-gameplay-layer wording quoted historically in §3.7.1 and in the 1.1/1.2 rows above is left as "layer" deliberately — it names the box this order replaced, not the current vocabulary. | — |
| 1.4 | August 18, 2026 | Claude Code | **Adversarial-review findings H8 + H10 (reviewed round), plus one Medium.** H8: §3.5.2's placement rule **contradicted the table it governs for 27 of the 33 ordered seatings** — "the tier is derived, not chosen: … the lowest tier strictly above every assembly it references, unless a lower seating is stated in the table with its reason" mandated 8 seatings HIGHER than any exception covered (e.g. `player-database` derives to 1, seated 5; a new Management assembly reading only `player-database` would have been mandated into Composition), allowed the 19 seated LOWER only via a "stated … with its reason" clause that only tier 0's cell satisfied, and was **unsatisfiable** for the four assemblies whose highest reference is tier 9 (derived tier 10 does not exist). Restated as a **bound plus a justified choice**: seating at or below a referenced assembly's tier is forbidden (intra-tier permitted per FR-CS-046a), and within that bound the tier is a design choice justified in the *Why this tier* cell — verified against all 35 seatings (0 upward references, 38 intra-tier, every row's third cell non-empty). The rule's tool-enumeration sentence now also names the FR-CS-046b checks `tools/assembly-tier-check.py` gained the same day (it previously skipped every Infrastructure-sourced reference unchecked — reviewed finding H9, whose spec half lands in section-2.md v1.3). H10 (this file's half): the 1.2 row is annotated in place as incomplete of its own commit (four unrecorded changes, enumerated there), and the 1.3 row's false "§5.4.5 … already said 'tier'" claim is corrected in place. Medium: §3.7.1 rescoped from "every production assembly in the §3.5.2 tier order" — which excluded the two Infrastructure assemblies, since FR-CS-046 says they acquire no tier — to "every production assembly under `src/`", Infrastructure included. | — |
| 1.5 | August 18, 2026 | Claude Code | **Reviewed-findings pass H1 + M1 (+ four Lows), spec halves.** H1: §3.5.2's "enforced mechanically" sentence claimed an enforcement that did not exist — nothing ran `tools/assembly-tier-check.py` (no CI step, not in `run-gate.sh`, no hook), so an unamended table stayed green, exactly the `ERR-020-002` drift condition. The tool is now WIRED: the `Spec hygiene checks` job in `.github/workflows/ci.yml` runs it on every push and pull request, and the sentence now states that, enumerates the checks the tool actually performs (including the new ones below), and scopes what stays review — same-commit atomicity (CI sees trees, not commits) and the *adequacy* of a *Why this tier* justification (the tool checks the cell is non-empty only, closing the half-mechanical overclaim). M1 (tool v1.2, spec side): the out-of-band Infrastructure set is now asserted **by name** against FR-CS-046b's own list in §2.2.5 — previously one character of drift in the tier cell ("—" → "0" or "10") emptied the infra set and PASSED with both FR-CS-046b checks silently disabled (mutation-proved); the §3.5.2 preamble now also states the row is covered-not-ordered and must never be folded into the numbered order, retiring the wording that invited exactly that. Lows: the one-production-`.asmdef`-per-top-level-folder constraint the tool enforced but no spec stated is written into the placement rule; **gameplay tiers** defined once in the preamble (tiers 1–4 — the tier-5 cell had parenthesised it as three tiers while the tier-6 cell counted four) and the tier-5 cell recast on the defined term; §3.5.4's decision tree de-"layer"ed ("cross-tier notification", "producer in a lower tier", "Intra-tier or downward call" — "layer order" is undefined since v1.3); §3.3.4's heading renamed "UI / Non-Loop Allocation Budget" → "Presentation / Client (Non-Loop) Allocation Budget", matching the body FR-CS-067 rescoped at v1.2, and its trailing "UI code / UI method" phrasing aligned. | — |

---

*End of Section 3 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
