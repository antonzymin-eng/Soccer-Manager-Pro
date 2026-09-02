# Code Standards & Style Guide Specification #20 — Appendices

**File:** `docs/specs/code-standards/appendices.md`
**Purpose:** Paste-ready templates (A–B), exemplar source files (C), the single
source-of-truth banned/required API list (D), glossary (E), and architecture
integration record examples (F) for Spec #20.
Appendix D is the KD-6 single source of truth; §3.3, §3.4, §5.2, and §7.1 cite it
by category name and must not reproduce its symbol lists.

**Created:** May 7, 2026
**Modified:** September 2, 2026
**Version:** 1.6.2
**Status:** AMENDMENT DRAFT (A3.1a; approved v1.5 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §APPENDICES
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md`
v0.34, §6; A3.1a
**Appendix target lengths:** A ~50 lines · B ~30 lines · C ~150 lines ·
D ~80 lines · E ~40 lines · F ~160 lines

---

## Table of Contents

- [Appendix A — File Header Template](#appendix-a--file-header-template)
- [Appendix B — Version-History Block Template](#appendix-b--version-history-block-template)
- [Appendix C — Exemplar Pair](#appendix-c--exemplar-pair)
- [Appendix D — Banned & Required APIs (Single Source of Truth)](#appendix-d--banned--required-apis-single-source-of-truth)
- [Appendix E — Glossary](#appendix-e--glossary)
- [Appendix F — Architecture Integration Records](#appendix-f--architecture-integration-records)
- [Appendix Version History](#appendix-version-history)

---

## Appendix A — File Header Template

Every `.cs` file under `src/` **MUST** open with the following block (FR-CS-056,
FR-CS-057). Copy verbatim; replace angle-bracket placeholders. Do not omit fields.

```csharp
// ============================================================================
// File:     <path relative to repo root, e.g. src/ball-physics/BallPhysicsConstants.cs>
// Created:  <YYYY-MM-DD>
// Modified: <YYYY-MM-DD>   (update on every change; matches latest version-history row)
// Author:   <name or "Claude Code / <lead-developer>">
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           <Spec #N §S.S — the spec this file implements, if applicable>
// Purpose:  <One or two sentences. What this file declares and why it exists.
//            Example: "Declares all compile-time and tunable constants for the
//            Ball Physics subsystem (Spec #1). Consumed read-only by BallStateSystem.">
// ============================================================================
```

**Field rules (FR-CS-057):**

| Field | Rule |
|---|---|
| `File:` | Path from repo root. Must match actual filesystem path. |
| `Created:` | ISO 8601 date the file was first committed. Never updated. |
| `Modified:` | ISO 8601 date of the last change. Must match the latest row in the version-history block. |
| `Author:` | Person or agent who authored the file. Add co-authors on the same line, comma-separated. |
| `Specs:` | One line per specification the file is governed by or implements. Always includes `Spec #20 §3.6.2`. |
| `Purpose:` | ≤ 2 sentences. States *what* the file declares and *why* it exists. No implementation detail. |

**Populated example** (for a hypothetical `BallPhysicsConstants.cs`):

```csharp
// ============================================================================
// File:     src/ball-physics/BallPhysicsConstants.cs
// Created:  2026-10-01
// Modified: 2026-10-01
// Author:   Claude Code / Anton
// Specs:    Spec #1 §2.1 (Ball Physics — constant definitions)
//           Spec #20 §3.6.2 (style & docs governance)
// Purpose:  Declares all compile-time and tunable constants for Ball Physics
//           (Spec #1). Consumed read-only by BallStateSystem and related structs.
// ============================================================================
```

---

## Appendix B — Version-History Block Template

Every `.cs` file under `src/` **MUST** end with the following region block
(FR-CS-058, FR-CS-059). The block is always the last item in the file, after all
type declarations. Add one row per modification; never delete rows.

```csharp
#region VersionHistory
// Version | Date       | Author           | Change
// --------|------------|------------------|------------------------------------------
// 1.0     | YYYY-MM-DD | <author>         | Initial file.
// 1.1     | YYYY-MM-DD | <author>         | <One-line description of what changed and why.>
#endregion
```

**Column rules:**

| Column | Rule |
|---|---|
| `Version` | Semantic version starting at `1.0`. Patch (`1.0.1`) for non-behavioural changes; minor (`1.1`) for additive changes; major (`2.0`) for breaking changes to public surface or formula. |
| `Date` | ISO 8601 date the change was committed. |
| `Author` | Person or agent who made the change. |
| `Change` | One line: the *what* and *why*. Terse is fine; "Fixed typo" is not — say what was wrong and what was corrected. |

**Populated example:**

```csharp
#region VersionHistory
// Version | Date       | Author           | Change
// --------|------------|------------------|------------------------------------------
// 1.0     | 2026-10-01 | Claude Code/Anton | Initial constants file; BALL_RADIUS,
//         |            |                  | DRAG_COEFFICIENT, MAX_SUBSTEPS declared.
// 1.1     | 2026-10-15 | Anton            | Added TERMINAL_VELOCITY [EST]; tracking
//         |            |                  | entry added to spec-error-log.md.
#endregion
```

---

## Appendix C — Exemplar Pair

Two hypothetical files demonstrating every §3 rule applied simultaneously. These are
**illustrative only** — they are not production source files and will be superseded by
actual Stage 1 code. Every §3 rule is visible with an inline `// §N.N` pointer.

Rule coverage map:

| Rule area | FR-CS-### | Demonstrated in |
|---|---|---|
| Naming | FR-CS-001–004 | `ExemplarConstants.cs` and `ExemplarStruct.cs` |
| File layout | FR-CS-005–007 | Both files |
| Language features | FR-CS-009–010 | `ExemplarStruct.cs` |
| Whitespace/braces | FR-CS-011–013 | Both files |
| Access modifiers | FR-CS-014–015 | Both files |
| Constant catalogue | FR-CS-016–025 | `ExemplarConstants.cs` |
| Allocation discipline | FR-CS-026–035 | `ExemplarStruct.cs` (Update method) |
| Determinism | FR-CS-036–045 | `ExemplarStruct.cs` (MatchClock, unchecked) |
| Dependency direction | FR-CS-046–055 | `ExemplarStruct.cs` (struct event, no singletons) |
| Documentation | FR-CS-056–065 | Both files (headers, XML docs, XC- comments) |
| Performance rules | FR-CS-066–070 | `ExemplarStruct.cs` (ProfilerMarker, sealed) |
| Numeric type | FR-CS-071–073 | `ExemplarStruct.cs` (float throughout) |

---

### C.1 — `ExemplarConstants.cs`

```csharp
// ============================================================================
// File:     src/ball-physics/BallPhysicsConstants.cs
// Created:  2026-10-01
// Modified: 2026-10-01
// Author:   Claude Code / Anton
// Specs:    Spec #1 §2.1 (Ball Physics — constant definitions)
//           Spec #20 §3.6.2 (style & docs governance)
// Purpose:  Declares all compile-time and tunable constants for Ball Physics
//           (Spec #1). Consumed read-only by BallStateSystem and related structs.
// ============================================================================

// §3.1.2 — using order: System → Unity → project
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics   // §3.1.2, §4.3 — one namespace per assembly
{
    /// <summary>
    /// All constants for Ball Physics Spec #1. Organised by tag per §4.2.
    /// </summary>
    public static class BallPhysicsConstants   // §3.1.1 — PascalCase; §3.1.5 — explicit public
    {
        // ── [FIXED] ── compile-time literals; ALL_CAPS; public const ──────────
        // §3.2.3 (FR-CS-018), §3.1.1 (FR-CS-004)

        /// <summary>[FIXED] Ball radius in metres. Spec #1 §2.1.</summary>
        public const float BALL_RADIUS = 0.11f;       // §3.2.3 — [FIXED] → public const

        /// <summary>[FIXED] Aerodynamic drag coefficient (dimensionless). Spec #1 §3.2 FM-001.</summary>
        public const float DRAG_COEFFICIENT = 0.47f;

        // ── [DERIVED] ── static readonly; formula comment required ────────────
        // §3.2.3 (FR-CS-021)

        /// <summary>
        /// [DERIVED] Ball centre height when resting on ground (m).
        /// Formula: BallGroundHeight = BALL_RADIUS. Spec #1 §2.1.
        /// </summary>
        public static readonly float BallGroundHeight = BALL_RADIUS;    // §3.2.3

        // ── [CROSS] ── read-only mirror; cite authoritative source ────────────
        // §3.2.3 (FR-CS-022)

        /// <summary>
        /// [CROSS] Tick rate of the tactical/AI loop (Hz).
        /// Authoritative source: root CLAUDE.md, "Heartbeat Tick Rate" — which is
        /// where this particular value is genuinely owned, and is what the live
        /// declaration cites. Where a NUMBERED spec owns the value, FR-CS-022 requires
        /// the citation to name that spec and section instead: the 60 Hz half of the
        /// same block cites Ball Physics #1 §1.2.
        /// Never set independently here: the initializer BINDS the source symbol, so
        /// the compiler enforces the mirror and divergence is impossible. A literal
        /// (`= 10.0f`) would make this a SECOND authority — that is the
        /// literal-initialized shape §3.2.3 excludes from the const carve-out, and the
        /// tree's own `// TODO: mirror from ProjectConstants` tick-rate declarations
        /// are the standing example of it. Illustrative symbol: this appendix is a
        /// worked example, not compiled code.
        /// </summary>
        public static readonly float TacticalTickHz = DeterministicSimConstants.TACTICAL_TICK_HZ;

        // ── [GT] ── static readonly; loaded from tunable config at boot ───────
        // §3.2.3 (FR-CS-019)

        /// <summary>
        /// [GT] Maximum drag-integration substeps per frame.
        /// Loaded from GameplayConfig at boot; not a compile-time literal (FR-CS-019),
        /// which is why the initializer is the config read and not `= 4`.
        /// </summary>
        public static readonly int MaxSubsteps = Config.GetInt("ball-physics", "MaxSubsteps", 4);

        // ── [EST] ── static readonly; TODO validate; spec-error-log entry ─────
        // §3.2.3 (FR-CS-020)

        /// <summary>
        /// [EST] Estimated terminal velocity (m/s) for a regulation football.
        /// Must be validated against wind-tunnel data before Stage 1.
        /// </summary>
        public static readonly float TerminalVelocity = 38.0f; // TODO: validate — spec-error-log entry required (FR-CS-020)
    }
}

#region VersionHistory
// Version | Date       | Author           | Change
// --------|------------|------------------|------------------------------------------
// 1.0     | 2026-10-01 | Claude Code/Anton | Initial file. All five tag types demonstrated.
#endregion
```

---

### C.2 — `ExemplarStruct.cs`

```csharp
// ============================================================================
// File:     src/ball-physics/BallStateSystem.cs
// Created:  2026-10-01
// Modified: 2026-10-01
// Author:   Claude Code / Anton
// Specs:    Spec #1 §3.x (Ball Physics — state update system)
//           Spec #20 §3.6.2 (style & docs governance)
// Purpose:  Advances BallState by one physics frame. Called at 60 Hz on the
//           physics update path; must satisfy zero-allocation budget (FR-CS-026).
// ============================================================================

// §3.1.2 — using order: System → Unity → project
using UnityEngine;
using UnityEngine.Profiling;
using TacticalDirector.BallPhysics;
using TacticalDirector.Shared;          // MatchClock lives here

namespace TacticalDirector.BallPhysics  // §3.1.2, §4.3 — flat namespace; folder ≠ sub-namespace
{
    /// <summary>
    /// Advances BallState on the 60 Hz physics update path.
    /// XC-001-001: depends on BallState layout defined in Ball Physics Spec #1 §3.1.
    /// </summary>
    public sealed class BallStateSystem  // §3.1.1 — PascalCase; sealed = no virtual dispatch (FR-CS-068)
    {
        // §3.4.3 (FR-CS-042) — time injected; not DateTime.Now
        // §3.5.5 (FR-CS-053) — no static mutable singleton
        private readonly MatchClock _clock;   // §3.1.3 — _camelCase private field (FR-CS-003)

        // §6.3 (FR-CS-070) — ProfilerMarker declared once; reused each frame (zero alloc)
        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("BallPhysics.BallStateSystem.Update");  // naming: <Spec>.<Method>

        /// <summary>
        /// Initialises BallStateSystem. All dependencies supplied by caller;
        /// no service-locator or ambient-context pattern (FR-CS-051, FR-CS-052).
        /// </summary>
        public BallStateSystem(MatchClock clock)  // §3.1.1 — PascalCase; §3.1.4 — Allman braces
        {
            _clock = clock;
        }

        /// <summary>
        /// Advances ball state by one physics frame (called at 60 Hz).
        /// Ref-passed struct: no managed allocation on call path (FR-CS-033).
        /// FM-001: v' = v * (1 − DRAG_COEFFICIENT × dt).
        /// </summary>
        public void Update(ref BallState state)  // §3.3.3 — ref-passed struct (FR-CS-033)
        {
            using (s_updateMarker.Auto())  // §6.3 (FR-CS-070) — profiler scope wraps full method
            {
                // §3.7.1 (FR-CS-071) — float throughout; no double
                float dt = _clock.DeltaTime;  // §3.4.3 (FR-CS-042) — MatchClock, not DateTime.Now

                // Inner hot-path loop: no boxing, no LINQ, no virtual calls (FR-CS-027–032, FR-CS-068)
                // §3.2.4 (FR-CS-024) — loop bounds 0 / i++ are permitted literals
                for (int i = 0; i < BallPhysicsConstants.MaxSubsteps; i++)  // §3.2.3 — named constant
                {
                    ApplyDrag(ref state, dt);
                }
            }
        }

        // §3.1.5 (FR-CS-014) — explicit private; not implicit
        private static void ApplyDrag(ref BallState state, float dt)
        {
            // FM-001: drag model — magnitude *= (1 − DRAG_COEFFICIENT × dt)
            // §3.2.3 (FR-CS-023) — no magic number; DRAG_COEFFICIENT from catalogue
            state.Velocity *= 1.0f - BallPhysicsConstants.DRAG_COEFFICIENT * dt;
        }

        // §3.5.4 (FR-CS-050) — upward cross-spec event dispatched as struct, not delegate
        // §3.5.2 (FR-CS-047) — struct event flows upward from Physics to Mechanics layer
        private static void PublishBounceEvent(in BallState state, float impactSpeed)
        {
            // BallBounceEvent is a struct; dispatched via pre-allocated event buffer
            // §3.3.3 (FR-CS-033) — struct events avoid delegate allocation
            var evt = new BallBounceEvent(state.Position, impactSpeed);
            BallEventBus.Publish(ref evt);
        }
    }
}

#region VersionHistory
// Version | Date       | Author           | Change
// --------|------------|------------------|------------------------------------------
// 1.0     | 2026-10-01 | Claude Code/Anton | Initial exemplar. All §3 and §6 rules demonstrated.
#endregion
```

---

## Appendix D — Banned & Required APIs (Single Source of Truth)

> **KD-6 — Single source of truth.** This table is the sole authoritative list of
> banned and required symbols for Spec #20. Sections §3.3, §3.4, §5.2, and §7.1 cite
> categories from this table by name; they do **not** reproduce symbol lists. Any
> addition or removal of an entry **MUST** update this table first, with a version bump
> to this file.
>
> At Stage 1, the "det-banned" and "alloc-hot-path" categories become the seed for
> `BannedSymbols.txt`. The "det-required-apis" category seeds custom Roslyn analyzer
> rules. The "det-required-patterns" category cannot be symbol-encoded and requires
> custom analyzer logic.

---

### D.1 — Category: `det-banned` (game-logic code)

Symbols and language constructs in this category **MUST NOT** appear in game-state
assemblies (FR-CS-010, FR-CS-036–040). The benchmark carve-out in §3.9.5 permits
`Stopwatch.GetTimestamp` exclusively in files marked `// benchmark-only`, excluded
from the game-state assembly graph, and built in a `.csproj` that does not reference
`Microsoft.CodeAnalysis.BannedApiAnalyzers` (see §3.9.5 criterion 4).

The category covers both *deterministic-replay* hazards (non-deterministic state,
wall-clock time, process-unique IDs, multithreaded game-state) and *single-source
compile-time-safety / tick-ordering* hazards (`dynamic`, `async`/`await` for game-state
work, `unsafe` without sign-off). All such hazards share the same enforcement model
(BannedSymbols / banned-construct analyzer) and therefore live in one source-of-truth
table per KD-6.

| Symbol / construct | FR-CS-### | Root `CLAUDE.md` citation | Stage 1 analyzer ID |
|---|---|---|---|
| `System.Random` (constructor or static `Shared`) | FR-CS-036 | "When Writing Code" — "no `System.Random`" | `CS-DET-001` (placeholder) |
| `System.Security.Cryptography.RandomNumberGenerator` | FR-CS-036 | "When Writing Code" — "no `System.Random`" | `CS-DET-002` (placeholder) |
| `System.DateTime.Now` | FR-CS-037 | "When Writing Code" — "no `DateTime.Now`" | `CS-DET-003` (placeholder) |
| `System.DateTime.UtcNow` | FR-CS-037 | "When Writing Code" — "no `DateTime.Now`" | `CS-DET-004` (placeholder) |
| `System.Diagnostics.Stopwatch.GetTimestamp` *(game-state assemblies only; see §3.9.5)* | FR-CS-037 | "When Writing Code" — "no `DateTime.Now`" | `CS-DET-005` (placeholder) |
| `System.Environment.TickCount` / `TickCount64` | FR-CS-037 | "When Writing Code" — "no `DateTime.Now`" | `CS-DET-006` (placeholder) |
| `System.Guid.NewGuid()` | FR-CS-038 | "When Writing Code" — "no `DateTime.Now` in game logic" (process-unique IDs) | `CS-DET-007` (placeholder) |
| `System.Threading.Tasks.Task.Run` | FR-CS-039 | "When Writing Code" — determinism requirement | `CS-DET-008` (placeholder) |
| `System.Threading.Tasks.Parallel.For` | FR-CS-039 | "When Writing Code" — determinism requirement | `CS-DET-009` (placeholder) |
| `System.Threading.Tasks.Parallel.ForEach` | FR-CS-039 | "When Writing Code" — determinism requirement | `CS-DET-010` (placeholder) |
| `System.Linq.ParallelEnumerable.AsParallel()` | FR-CS-039 | "When Writing Code" — determinism requirement | `CS-DET-011` (placeholder) |
| Hardware-intrinsic FMA (`System.Runtime.Intrinsics.*Fma*`) | FR-CS-040 | "When Writing Code" — float + determinism rules | `CS-DET-012` (placeholder) |
| `dynamic` keyword in game-logic code | FR-CS-010 | "When Writing Code" — banned language feature | `CS-DET-013` (placeholder) |
| `async` / `await` on game-state work (any method whose continuation can run on a non-tick frame) | FR-CS-010 | "When Writing Code" — determinism requirement (deterministic tick ordering) | `CS-DET-014` (placeholder) |
| `unsafe` blocks without recorded lead-developer sign-off in the PR description | FR-CS-010 | "When Writing Code" — banned language feature; pointer arithmetic plus undefined cross-platform behaviour | `CS-DET-015` (placeholder) |

---

### D.2 — Category: `alloc-hot-path` (per-frame / hot-path code)

Constructs in this category **MUST NOT** appear in hot-path code (FR-CS-026–034).
"Hot-path code" means any code called on the 60 Hz physics/render update path.

| Symbol / construct | FR-CS-### | Notes | Stage 1 analyzer ID |
|---|---|---|---|
| Boxing: value type cast to `object` or to a non-`struct` `interface` | FR-CS-027 | Includes passing a struct to an `IComparable` parameter | `CS-ALLOC-001` (placeholder) |
| LINQ-to-objects fluent chain (e.g., `.Where(…).Select(…)`, `.ToList()`) | FR-CS-028 | Any `System.Linq` operator that returns `IEnumerable<T>` or allocates | `CS-ALLOC-002` (placeholder) |
| `params` array parameter on a hot-path method | FR-CS-029 | Declaration-site ban; callers cannot opt out | `CS-ALLOC-003` (placeholder) |
| `string.Format(…)` | FR-CS-030 | Allocates a new string | `CS-ALLOC-004` (placeholder) |
| String interpolation (`$"…"`) containing non-constant expressions | FR-CS-030 | Equivalent to `string.Format` at runtime | `CS-ALLOC-005` (placeholder) |
| String concatenation (`+` with non-constant operands) | FR-CS-030 | Allocates a new string | `CS-ALLOC-006` (placeholder) |
| Closure capturing a local variable (lambda or anonymous method) | FR-CS-031 | Compiler generates a heap-allocated display class | `CS-ALLOC-007` (placeholder) |
| `foreach` over a non-`struct` enumerator (e.g., `List<T>`, `Dictionary<K,V>`) | FR-CS-032 | Boxes the enumerator on each loop entry | `CS-ALLOC-008` (placeholder) |
| `System.Reflection` APIs (`Type.GetType`, `Activator.CreateInstance`, `MethodInfo.Invoke`, etc.) | FR-CS-034 | Reflection is both allocating and non-deterministic in ordering | `CS-ALLOC-009` (placeholder) |

---

### D.3 — Category: `det-required-apis` (game-logic code)

The following APIs **MUST** be used in place of their det-banned equivalents
(FR-CS-041–043, FR-CS-070).

| API | FR-CS-### | Purpose | Root `CLAUDE.md` citation |
|---|---|---|---|
| `SplitMix64` (project RNG helper class/struct) | FR-CS-041 | Deterministic random-number generation seeded from `matchSeed + agentId + frameNumber` | "When Writing Code" — "SplitMix64 for deterministic RNG" |
| `MatchClock` (injected time service) | FR-CS-042 | Deterministic simulation time; replaces all wall-clock APIs | "When Writing Code" — "no `DateTime.Now`" |
| Project math helper (`TacticalDirector.MathHelper` or wrapper around `UnityEngine.Mathf`) | FR-CS-043 | Trig and math operations; `System.Math` requires sign-off | "When Writing Code" — determinism requirement |
| `UnityEngine.Profiling.ProfilerMarker` | FR-CS-070 | Performance profiling marker around every system Update | `development-best-practices.md` profiling section |

---

### D.4 — Category: `det-required-patterns` (game-logic code)

The following patterns **MUST** be applied where applicable (FR-CS-044–045). These are
C# language constructs, not named API symbols; they cannot be encoded directly in
`BannedSymbols.txt` and require custom analyzer logic at Stage 1.

| Pattern | FR-CS-### | Application rule | Root `CLAUDE.md` citation |
|---|---|---|---|
| `unchecked { … }` scope around 64-bit intermediate multiplication | FR-CS-044 | Applied to every 64-bit integer multiplication in seed or hash chains in C# game-logic code; accompanied by a one-line comment citing §3.4.4 | "When Writing Code" — SplitMix64 masking |
| `& 0xFFFFFFFFFFFFFFFF` mask on intermediate 64-bit multiplication; omit `UL` suffix | FR-CS-045 | Applied in Python (or other non-C#) tooling that mirrors `[FIXED]` / `[DERIVED]` constants | "When Writing Code" — "In Python tooling: omit `UL` suffix … mask all intermediate multiplications with `& 0xFFFFFFFFFFFFFFFF`" |

---

### D.5 — Footer

This table is the **seed** for Stage 1 `BannedSymbols.txt` (categories `det-banned`
and `alloc-hot-path`) and for the custom Roslyn analyzer ruleset (category
`det-required-apis`). No other document in this repository may declare a banned or
required API symbol without first adding it to this table and bumping this file's
version. Violations of this rule constitute a KD-6 breach.

The placeholder analyzer IDs (`CS-DET-NNN`, `CS-ALLOC-NNN`) are reserved prefixes.
Concrete IDs will be assigned when the analyzer project is created at the Stage 0+1
transition (§5.2, §7.1).

---

## Appendix E — Glossary

This glossary defines only terms specific to Spec #20. Physics, AI, and simulation
terms are defined in their owning specifications and cited here by reference rather than
redefined.

| Term | Definition |
|---|---|
| **Constants catalogue file** | A `.cs` file whose sole purpose is to declare named constants for one specification (or the project as a whole). Named `<SpecName>Constants.cs` or `ProjectConstants.cs`. No logic, no type declarations other than the enclosing `static class`. See §4.2. |
| **`[CROSS-PENDING]` (constant tag)** | The sixth root `CLAUDE.md` constant tag: a cross-spec constant blocked on an upstream spec, whose numeric value is not yet allocated; promoted to `[CROSS]` atomically with upstream approval. Reproduced verbatim in §3.2.1; storage-class mapping in §3.2.3; `#region` slot in §4.2. Added to this spec August 18, 2026 (round-6 finding H6, honouring §9.4 re-approval trigger 1). |
| **det-banned** | The category name (Appendix D §D.1) for APIs and constructs prohibited in game-logic code because their use would break deterministic replay. References in §3.4.2 and §5.2. |
| **det-required-apis** | The category name (Appendix D §D.3) for APIs that **MUST** be used in place of their det-banned equivalents. References in §3.4.3 and §5.2. |
| **det-required-patterns** | The category name (Appendix D §D.4) for C# language patterns (not named API symbols) that **MUST** be applied to preserve determinism. References in §3.4.3 and §5.2. |
| **alloc-hot-path** | The category name (Appendix D §D.2) for constructs that allocate managed memory and are prohibited in hot-path code. References in §3.3.2 and §5.2. |
| **Exception with sign-off** | A lead-developer-recorded override permitting temporary deviation from a MUST or MUST NOT FR. Defined in §2.1 and §2.3 Mode 3. Applies to the specific use site only; expires at next refactor of the affected file. |
| **Game-loop method** | Any method on the 60 Hz physics/render update path. Subject to the zero-allocation budget (FR-CS-026, FR-CS-066) and hot-path rules (FR-CS-027–034, FR-CS-068–069). The 10 Hz AI/tactical loop is also covered when its methods are called from within a 60 Hz frame. See root `CLAUDE.md` — "Heartbeat Tick Rate" for the authoritative loop definitions. |
| **Game-state assembly** | A Unity Assembly Definition (`.asmdef`) that is subject to the det-banned API ban (§3.4.2) and the determinism patterns (FR-CS-051–054), whether or not it participates in the simulation. Scope: **every production assembly under `src/`** — Foundation through Client in the §3.5.2 tier order, *including* the two out-of-band Infrastructure assemblies — the same scope §3.7.1 states for FR-CS-071; this reaches Presentation/Client-tier assemblies (`ui-framework`, `client-app`, `match-client-web`, `match-viewer`, etc.) and the Infrastructure pair even though none of them participates in the deterministic simulation. Editor-only and benchmark assemblies (§3.9.3, §3.9.5) are not game-state assemblies; test assemblies are not production assemblies. *(Rescoped August 18, 2026, round-6 finding H2: the prior "Physics, Mechanics, and AI layers" wording predated the §3.5.2 ten-tier order and read the deterministic core — `deterministic-sim`, `event-system`, `match-engine`, `season-save` among others — out of the determinism rules entirely. Definitional clause corrected round-7 finding L3: it still read "participates in the deterministic simulation" against a scope clause that is rule-EXTENSION, not participation — `ui-framework`, `client-app`, `match-client-web`, `match-viewer`, and the two Infrastructure assemblies are in scope without participating.)* |
| **Hot path** | Code executed on every physics or AI tick — i.e., code called 10–60 times per second during active gameplay. Boxing, LINQ, `string.Format`, closures, and reflection in hot-path code violate allocation rules. See §3.3 and §6.2. |
| **Magic number** | A literal numeric value in formula, system, or struct code that is not referenced through a named constant in a catalogue file. Prohibited by FR-CS-023. Permitted exceptions enumerated in FR-CS-024. |
| **Per-frame path** | Synonym for **game-loop method** (see that entry for the canonical loop-rate scope and rule citations). Used in the context of allocation rules (§3.3.2, FR-CS-030) when the emphasis is on per-frame repetition rather than method shape. |
| **Phantom interface** | An `interface` definition whose consumer side is unspecified or not yet written. Prohibited by FR-CS-049; cites ERR-001 and ERR-004. See root `CLAUDE.md` — "Interface Design Principle". |
| **Stage 0+1 transition** | The development milestone at which the first real Stage 1 source code is written. This transition activates the tooling deliverables in §5.2 and §7.1 (Roslyn analyzers, `.editorconfig`, `BannedSymbols.txt`, `src/CLAUDE.md`). |
| **System-level Update method** | The top-level entry point of a simulation system, called once per physics or AI tick. Required to be wrapped in a `ProfilerMarker.Auto()` scope per FR-CS-070. |

---

## Appendix F — Architecture Integration Records

These examples illustrate §3.5.6–3.5.7 and FR-CS-074–081. They are not a second
schema. The canonical Draft 2020-12 files under
`docs/tracking/architecture-governance/schemas/` and reference semantics version
`2.1.0` control field names, allowed values, validation, and blocking behavior.

### F.1 — Overload-Safe Selectors

Selectors use assembly identity and C# XML-documentation-ID type-signature spelling.
These two legal overloads remain distinct because `@` identifies the by-reference
parameter:

```json
[
  {
    "assembly": "Example.Runtime",
    "kind": "method",
    "containing_type_id": "Example.MatchHost",
    "member_name": "Activate",
    "parameter_type_ids": ["System.Int32"],
    "generic_arity": 0,
    "is_static": false
  },
  {
    "assembly": "Example.Runtime",
    "kind": "method",
    "containing_type_id": "Example.MatchHost",
    "member_name": "Activate",
    "parameter_type_ids": ["System.Int32@"],
    "generic_arity": 0,
    "is_static": false
  }
]
```

### F.2 — Active Integration Contract with Rename History

`component:match-bootstrap` is the durable concept. Renaming `Start` to `Activate`
changes its current source selector and `symbol_key`, not its `component_id`:

```json
{
  "contract_id": "CONTRACT-MATCH-BOOTSTRAP",
  "component_id": "component:match-bootstrap",
  "current_selector": {
    "assembly": "Example.Runtime",
    "kind": "method",
    "containing_type_id": "Example.MatchHost",
    "member_name": "Activate",
    "parameter_type_ids": [],
    "generic_arity": 0,
    "is_static": false
  },
  "selector_history": [
    {
      "selector": {
        "assembly": "Example.Runtime",
        "kind": "method",
        "containing_type_id": "Example.MatchHost",
        "member_name": "Start",
        "parameter_type_ids": [],
        "generic_arity": 0,
        "is_static": false
      },
      "superseded_reason": "Method renamed; architectural component unchanged"
    }
  ],
  "owning_host": "component:match-host",
  "owning_assembly": "Example.Runtime",
  "composition_root": "SURFACE-MATCH-HOST-COMPOSE",
  "construction_path": "SURFACE-MATCH-HOST-COMPOSE -> component:match-bootstrap",
  "activation_phase": "host-startup",
  "update_use_owner": "SURFACE-MATCH-LOOP-TICK",
  "teardown_owner": "SURFACE-MATCH-HOST-DISPOSE",
  "relevant_testhost_path": "src/example-tests/MatchTestHost.cs",
  "alternate_supported_paths": ["SURFACE-REPLAY-HOST-COMPOSE"],
  "prohibited_bypass_paths": ["SURFACE-MATCH-BOOTSTRAP-CONSTRUCTOR"],
  "static_initialization_involved": false,
  "lifecycle_ordering_requirements": ["Compose before Activate before Tick"],
  "na_fields": [],
  "activation_state": "active",
  "tuning_surface_selectors": []
}
```

### F.3 — Runtime-Surface Classification

The classification record binds the current compiler symbol to the same durable
component and contract:

```json
{
  "surface_id": "SURFACE-MATCH-BOOTSTRAP-ACTIVATE",
  "symbol_key": "M:Example.MatchHost.Activate",
  "kind": "method",
  "source_path": "src/example/MatchHost.cs",
  "signature": "System.Void Example.MatchHost.Activate()",
  "assembly": "Example.Runtime",
  "classification": "production-runtime-root",
  "component_id": "component:match-bootstrap",
  "contract_id": "CONTRACT-MATCH-BOOTSTRAP"
}
```

### F.4 — Typed Lifecycle Edges

Lifecycle/order proof uses canonical dependency nodes and `source` / `target` /
`relation` edges. Every edge endpoint is declared in `nodes`; the relation value,
not surrounding prose, gives the edge its machine meaning. The fingerprints below
are illustrative valid SHA-256 values, not reusable proof evidence:

```json
{
  "nodes": [
    {
      "dependency_id": "symbol:MatchHost.Compose",
      "kind": "runtime-root",
      "fingerprint": "09c509bd92878883b9090ae7f05049821fdce67664f39957fba26fe2a00a238f"
    },
    {
      "dependency_id": "component:match-bootstrap",
      "kind": "contract",
      "fingerprint": "64509290862a16c1de105df37b8576d256ece06f671e107064c36d805eebef62"
    },
    {
      "dependency_id": "symbol:MatchLoop.Tick",
      "kind": "lifecycle",
      "fingerprint": "5ecf264377fcb363b8b01145a2edaff12fd5c6be7d3d1594710d5c9435406a8c"
    },
    {
      "dependency_id": "testhost:match",
      "kind": "testhost",
      "fingerprint": "81cf7c6f69ff15a23a4d35131ec1ab69f61a0b50e1fa0039ee54922321ff4561"
    }
  ],
  "edges": [
    {
      "source": "symbol:MatchHost.Compose",
      "target": "component:match-bootstrap",
      "relation": "lifecycle-member"
    },
    {
      "source": "component:match-bootstrap",
      "target": "symbol:MatchLoop.Tick",
      "relation": "ordering"
    },
    {
      "source": "component:match-bootstrap",
      "target": "testhost:match",
      "relation": "testhost-equivalent"
    }
  ]
}
```

For `intentionally-disabled`, the full contract additionally carries
`activation_owner`, `decision_ref`, `reactivation_condition`, and this typed anchor
shape:

```json
{
  "disable_anchor": {
    "selector": {
      "assembly": "Example.Runtime",
      "kind": "field",
      "containing_type_id": "Example.MatchConfig",
      "member_name": "EnableBootstrap",
      "is_static": true
    },
    "operator": "equals",
    "expected": {
      "value_type": "boolean",
      "value": false
    }
  }
}
```

---

## Appendix Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §APPENDICES. Appendix D authored to KD-6 single-source-of-truth standard. All five appendices present. | — |
| 1.1 | May 11, 2026 | Claude Code | Adversarial review fixes (audit findings H-04 demoted to M, L-03): Appendix D §D.1 expanded to include FR-CS-010's remaining banned constructs — `async`/`await` for game-state work (CS-DET-014 placeholder) and `unsafe` without sign-off (CS-DET-015 placeholder). Closes the KD-6 enforcement gap whereby FR-CS-010's rule text banned these constructs but Appendix D's BannedSymbols seed listed only `dynamic`. §D.1 header text expanded to make the dual rationale (determinism + compile-time safety) explicit. Appendix E "Per-frame path" glossary entry tightened to point to "Game-loop method" rather than restating loop-rate scope, eliminating the prior overlap. Minor version (additive — new rows, new wording; no removals or rule changes). | — |
| 1.1.1 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-6 findings H2 + H6.** H2: the Appendix E "Game-state assembly" entry still scoped the term to "the Physics, Mechanics, and AI layers (§3.5.2)" — a taxonomy §3.5.2 has not held since the ten-tier order was adopted (`ERR-020-002`). Under the stale wording the deterministic simulation's own assemblies (`deterministic-sim`, `event-system`, `match-engine`, `season-save`, `discipline`, `player-progression`, `training-system`, `injuries-medical`, `living-world`, `player-database`, `tactical-instructions`, `project-constants`) were NOT game-state assemblies and so were exempt from §3.4.2's det-banned ban and FR-CS-051–054, while §3.7.1 had already been rescoped to every production assembly under `src/` — the two sections contradicted each other. Rescoped identically to §3.7.1. H6: Appendix E gains a `[CROSS-PENDING]` entry (the tag — one of root `CLAUDE.md`'s six — was unknown to #20 anywhere; see section-3.md v1.6 for the primary fix). Appendix D reviewed per §9.4 re-approval trigger 1 and deliberately unchanged: `[CROSS-PENDING]` implies no banned or required API symbol, so there is no D-row to add. | — |
| 1.3 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding H5.** Appendix C §C.1 — #20's own "compliant exemplar", the file §3.9's coverage map and Appendix C's rule-coverage table point readers at for FR-CS-016–025 — violated §3.2.3 for four of its five tags: `BALL_GROUND_HEIGHT`, `PHYSICS_TICK_HZ`, `MAX_SUBSTEPS` and `TERMINAL_VELOCITY` were `public static readonly` in ALL_CAPS where `[DERIVED]`/`[CROSS]`/`[GT]`/`[EST]` all require PascalCase. This is `ERR-020-001` itself: that entry renamed `PHYSICS_TICK_HZ → PhysicsTickHz` in §4.2 in May 2026 and its file list never included this appendix, so the defect survived in the exemplar. Two further violations in the same block: the `[CROSS]` mirror was LITERAL-initialized (`= 60.0f`) — the shape §3.2.3's own carve-out names as never qualifying — and cited "root CLAUDE.md" where FR-CS-022 requires spec and section; and `[GT] MAX_SUBSTEPS = 4` was a compile-time literal against FR-CS-019's explicit MUST NOT while its own doc comment claimed it was config-loaded. All four renamed; the `[CROSS]` mirror now BINDS `DeterministicSimConstants.TACTICAL_TICK_HZ` (verified to exist) and names the authority that owns the value; `MaxSubsteps` shows the config read its comment promised. | — |
| 1.4 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding L3.** The "Game-state assembly" glossary entry's definitional clause ("participates in the deterministic simulation") contradicted its own scope clause ("every production assembly under `src/`") — `ui-framework`, `client-app`, `match-client-web`, `match-viewer`, and the two Infrastructure assemblies do not participate in the simulation, so the definition was false of the scope it names two sentences later. The scope (a rule EXTENSION) is correct and unchanged; the definition restated as "subject to the det-banned API ban … whether or not it participates in the simulation." | — |
| 1.5 | August 18, 2026 | Claude Code | **Adversarial-review round-8 finding H1, found mechanically by the new `tools/doc-claim-check.py`.** *(Renumbered 1.4 → 1.5 at adversarial-review round 9: this row landed in `f23f480` as a SECOND `1.4`, above the round-7 L3 row that had already taken that number in `20760cf`. `recurring-defect-lint.py` reported it as the tree's only ERROR — while root `CLAUDE.md` still claimed 0 ERROR tree-wide — so the round that added a mechanical checker for dangling identifiers introduced a defect a mechanical checker the repo already had was reporting. Rows reordered ascending with it; no content changed.)* §C.2 still called `BallPhysicsConstants.MAX_SUBSTEPS` after v1.3 renamed the declaration to `MaxSubsteps` in §C.1 one section above — a dangling reference inside the pair #20 offers as its COMPLIANT exemplar, annotated `// §3.2.3 — named constant`, i.e. claiming conformance to the rule that forced the rename. **This is `ERR-020-001` for the third time**: that entry renamed `PHYSICS_TICK_HZ → PhysicsTickHz` in §4.2 in May 2026 and its file list never included this appendix; v1.3's own history row named that failure while repeating it one section away. In `src/` the compiler would have rejected it; in a spec fence nothing binds, which is exactly why the identifier check now exists and why it is wired into CI rather than left to review. | — |
| 1.6 | September 2, 2026 | Codex | **A3.1a governance amendment draft.** Adds Appendix F with illustrative schema-aligned examples for overload-safe selectors, stable component identity across a rename, active integration ownership, runtime-surface classification, typed lifecycle edges, and a verifiable disabled-state anchor. Canonical A2 schemas and reference semantics remain authoritative. This draft is not approved; A3.4 reapproval remains required. | PENDING — A3.4 |
| 1.6.1 | September 2, 2026 | Codex | **A3.1a review correction.** F.4 now supplies the `nodes` list required by reference semantics v2.1.0 and declares every typed edge endpoint with a schema-valid kind and fingerprint. The complete example is accepted by `normalize_dependency_graph`; the fingerprints remain illustrative rather than reusable proof evidence. | PENDING — A3.4 |
| 1.6.2 | September 2, 2026 | Codex | **A3.1a metadata synchronization.** Advances the amendment-plan pointer from v0.33 to v0.34 after FR-CS-078 was aligned with Governance FR-AG-025. Appendix F content is unchanged from v1.6.1. | PENDING — A3.4 |

---

*End of Appendices — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
