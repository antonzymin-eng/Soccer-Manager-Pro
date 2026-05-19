# Adversarial Review — src/CLAUDE.md v1.1

> **Created:** 2026-05-19
> **Reviewer:** Adversarial pass (automated)
> **Subject:** `src/CLAUDE.md` v1.1 (post–first-adversarial-review state)
> **Cross-referenced:** Spec #20 (`docs/specs/code-standards/`) §3.2.3, §3.4.4, §3.5.2, §4.2
> **Findings:** 2 HIGH · 7 MEDIUM · 8 LOW

---

## Summary

`src/CLAUDE.md` v1.1 is a well-structured coding guide that correctly covers the
mandated scope (C# naming, constant catalogues, Unity project structure,
build/test commands) deferred from the root `CLAUDE.md`. The prior adversarial
pass resolved the layer-taxonomy and dependency-arrow direction errors. However,
cross-validation against Spec #20's authoritative section files reveals two HIGH
defects (a semantically inverted arrow label and a fabricated class name) and
seven MEDIUM defects, most of which are code-example errors that would produce
compilation failures if copied verbatim.

---

## HIGH Findings

### H-1 — Arrow label "`←` means 'references'" is semantically inverted

**Location:** `src/CLAUDE.md` — Reference Direction section

**Current text:**
```
project-constants  ←  referenced read-only by all assemblies

Physics  ←  Mechanics  ←  AI  ←  UI

`←` means 'references.' The AI assembly imports types from Mechanics, which
imports types from Physics.
```

**Problem:** The prose confirms that AI imports from Mechanics and Mechanics
imports from Physics — meaning AI *depends on* Mechanics. For `Mechanics ← AI`
to express "AI depends on Mechanics," `←` must mean "is referenced by" (passive),
not "references" (active). A developer reading "`←` means 'references'" at face
value concludes that `Physics ← Mechanics` = "Physics references Mechanics" —
i.e., Physics depends on Mechanics — which is architecturally backwards and
prohibited by FR-CS-046.

Spec #20 §3.5.2 uses `──►` (rightward) for the same diagram with the semantics
"A ──► B means B depends on A." The src/CLAUDE.md correctly inverted the arrow
direction to `←`, but kept the label in the wrong voice.

**Fix:** Change the label to:

```
`←` means 'is referenced by' (A ← B means B depends on A / B imports from A).
```

Or invert all arrows to `Physics → Mechanics → AI → UI` with "`→` means
'references'" (standard dependency-arrow convention).

---

### H-2 — `ConfigLoader.GetValue(…)` is not defined in any spec

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[GT]` region example

**Current text:**
```csharp
#region GT  // [GT] → public static readonly int MaxSubsteps = ConfigLoader.GetValue(…);
```

**Problem:** `ConfigLoader` does not exist in any approved spec. Spec #20 §3.2.3
says `[GT]` constants are "Loaded from tunable config at boot; not a `const`"
(FR-CS-019) but never names a loader class, signature, or assembly. A developer
implementing a `[GT]` constant has no spec-backed guidance on the actual loading
mechanism and cannot produce correct code.

This is the most-common constant type: many specs carry ≥ 20 `[GT]` constants.
A fabricated class name here will propagate across all 20 spec implementations.

**Fix:** Replace the fabricated call with a placeholder that honestly reflects the
unresolved state, and add a note explaining where the loader will be defined:

```csharp
#region GT
// [GT] — loaded from tunable config at boot (FR-CS-019).
// Exact loading mechanism (class, method, config-key format) is a Stage 1
// deliverable; document it in this file when resolved.
public static readonly int MaxSubsteps = /* ConfigLoader.GetValue("…") */ 8;
```

Alternatively, file a spec-error-log entry that gates any `[GT]` constant
implementation on the loader definition.

---

## MEDIUM Findings

### M-1 — `s_fixedUpdateMarker` used but never declared in game-loop example

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, COMPLIANT example

**Current text:**
```csharp
// COMPLIANT
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_fixedUpdateMarker.Auto();
    state = state with { … };
}
```

**Problem:** `s_fixedUpdateMarker` is not declared anywhere in this example.
The Profiler Markers section uses `s_marker` for the equivalent field. Copying
the COMPLIANT example produces `CS0103: The name 's_fixedUpdateMarker' does not
exist in the current context`.

**Fix:** Add the field declaration inside the example, using the consistent name
from the Profiler Markers section:

```csharp
private static readonly ProfilerMarker s_updateMarker =
    new ProfilerMarker("BallPhysics.UpdateBallPhysics");

// COMPLIANT
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_updateMarker.Auto();
    state = state with { … };
}
```

---

### M-2 — `[CROSS]` mirror cites `ProjectConstants.PhysicsTickHz` but the source constant is `PHYSICS_TICK_HZ`

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[CROSS]` mirrors example

**Current text:**
```csharp
public static readonly float PhysicsTickHz = ProjectConstants.PhysicsTickHz;
```

**Problem:** Spec #20 §4.2 shows the authoritative constant in `ProjectConstants.cs`
as `[FIXED] public const float PHYSICS_TICK_HZ = 60.0f;` — ALL_CAPS because
`[FIXED]` constants use `ALL_CAPS` per §3.2.3. The mirror's right-hand side must
reference the actual field name in the source, which is `PHYSICS_TICK_HZ`, not
`PhysicsTickHz`. The current example would fail to compile.

Note that `PhysicsTickHz` (PascalCase) is the correct name for the `[CROSS]`
*mirror* field (per the §3.2.3 precedence ruling already documented in the
Naming Discrepancy note). Only the source reference on the right-hand side of `=`
must match the ALL_CAPS name in `ProjectConstants.cs`.

**Fix:**
```csharp
// [CROSS] mirror in BallPhysicsConstants.cs (PascalCase per §3.2.3)
/// <summary>
/// [CROSS] Physics tick rate (Hz). Source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Root CLAUDE.md — "Heartbeat Tick Rate": 60 Hz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;
```

---

### M-3 — Tree comment mischaracterises `ProjectConstants.cs` (wrong tag, wrong scope)

**Location:** `src/CLAUDE.md` — Unity Project Structure tree

**Current text:**
```
│   └── ProjectConstants.cs            ← [CROSS] source-of-truth for all cross-spec constants
```

**Problem (two errors):**

1. **Wrong tag.** The constants declared *inside* `ProjectConstants.cs` are
   `[FIXED]`, `[GT]`, etc. — the tags that describe those values. They are NOT
   tagged `[CROSS]` (that tag belongs to the *mirrors* in per-spec catalogues).
   The tree comment implies that everything in `ProjectConstants.cs` carries
   `[CROSS]`, which contradicts §3.2.3 and Spec #20 §4.2.

2. **Wrong scope.** Spec #20 §4.2 states: "The primary declaration for any
   constant that **multiple specs consume** lives in `ProjectConstants.cs`. A
   constant that appears in only one spec's catalogue is **not** promoted to
   `ProjectConstants.cs`." The phrase "all cross-spec constants" implies 100%
   of `[CROSS]`-tagged values route through this file, which is false for
   single-consumer `[CROSS]` constants.

**Fix:**
```
│   └── ProjectConstants.cs  ← source-of-truth for constants consumed by more than one spec assembly (Spec #20 §4.2)
```

---

### M-4 — Single-consumer vs multi-consumer `[CROSS]` routing rule is undocumented

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[CROSS]` mirrors sub-section

**Current text:**
> A `[CROSS]` entry in a spec catalogue mirrors its value from `ProjectConstants.cs`
> (or the authoritative source spec) and must not diverge.

**Problem:** The "or the authoritative source spec" caveat is correct per Spec #20
§4.2 but has no explanation. A developer cannot determine *when* to use
`ProjectConstants.cs` vs a direct spec-catalogue reference. Without this rule,
some developers will route all `[CROSS]` constants through `ProjectConstants.cs`
(creating unnecessary coupling) and others will never use it.

The governing rule from Spec #20 §4.2:
- **Multi-consumer** (constant appears in ≥ 2 spec assemblies) → declare in
  `ProjectConstants.cs`; each consuming catalogue mirrors from there.
- **Single-consumer** (constant appears in exactly 1 spec assembly) → declare in
  the source spec's catalogue; the consuming catalogue mirrors directly from that.

**Fix:** Add the routing rule explicitly:

> A `[CROSS]` constant is declared in `ProjectConstants.cs` when **more than one**
> spec assembly consumes it. When only one spec consumes a constant defined by
> another spec (e.g., a domain tag allocated in Spec #16 §3.4 used only by
> Goalkeeper Mechanics #11), the consuming spec's catalogue mirrors from the
> source spec's catalogue directly — not via `ProjectConstants.cs` (Spec #20 §4.2).

---

### M-5 — `with { }` expression on `readonly struct` requires C# 10+; language version unspecified

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, COMPLIANT example

**Current text:**
```csharp
state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
```

**Problem:** `with` expressions on `struct` types (not just `record struct`) were
added in C# 10 (Visual Studio 2022 / .NET 6). The "WHAT IS NOT HERE YET" table
explicitly lists "C# language version pin" as blocked on `certification-platform.md`.
Unity 2022 LTS shipped with C# 9 by default on some backends; IL2CPP on older
Unity 2022.x releases may not support `with` on plain structs.

A developer writing for an older Unity LTS + older backend will see
`CS8858: The receiver type 'BallState' is not a valid record type` or similar.

**Fix:** Add a note alongside the example:

> **C# version note:** `with` expressions on `readonly struct` require C# 10+.
> Verify the Unity LTS revision and backend in `docs/tracking/certification-platform.md`
> before using this pattern. If the platform pins to C# 9, copy-and-modify the
> struct fields manually instead.

---

### M-6 — Layer taxonomy does not classify four spec assemblies

**Location:** `src/CLAUDE.md` — Assembly Layer Taxonomy table

**Problem:** The taxonomy table covers Physics / Mechanics / AI / UI. The following
spec assemblies are absent from the table and from the cross-cutting footnote
(which names only `deterministic-sim` and `event-system`):

| Assembly | Status |
|---|---|
| `project-constants` | Cross-cutting; read-only by all |
| `performance-optimization` | Contains trace-pipeline code (Spec #18 KD-3); no game-loop types |
| `testing-strategy` | CI orchestration tooling only; no game-loop code |
| `code-standards` | Governance only; no runtime code |

A developer referencing the taxonomy to decide whether their assembly can import
from, say, `performance-optimization` has no guidance.

**Fix:** Extend the cross-cutting note:

> The following assemblies are infrastructure-only and are **not** members of any
> gameplay layer. Game-layer code (Physics / Mechanics / AI) **MUST NOT** import
> them at runtime:
> - `performance-optimization` — trace pipeline only (Spec #18 KD-3)
> - `testing-strategy` — CI tooling only (Spec #19)
> - `code-standards` — governance; no runtime types (Spec #20)
> - `project-constants` — read-only constants; referenced by all layers

---

### M-7 — `.asmdef` files shown for only 5 of 20 spec folders, violating the stated rule

**Location:** `src/CLAUDE.md` — Unity Project Structure tree

**Problem:** The document states "One folder per spec. One `.asmdef` per folder."
The directory tree shows `.asmdef` files for `ball-physics`, `agent-movement`,
`collision-system`, `deterministic-sim`, and `event-system` only. The remaining
15 spec folders show no `.asmdef` entry. A developer reading the tree could
infer that only 5 assemblies get `.asmdef` files.

**Fix:** Either (a) add a commented `.asmdef` placeholder line to every spec
folder in the tree, or (b) add a note under the tree:

> **Note:** Every spec folder shown here requires a `.asmdef` file
> (e.g., `pressing-ai/pressing-ai.asmdef`). Only a subset is shown in the tree
> above for brevity. See each spec's `§4` (Architecture) file for the exact
> `.asmdef` reference list. `.asmdef` GUIDs are blocked on Unity project
> initialization (see "WHAT IS NOT HERE YET").

---

## LOW Findings

### L-1 — Missing `Last Updated` field in document header

**Location:** `src/CLAUDE.md` — header block

The root `CLAUDE.md` carries both `Created` and `Last Updated` fields. After the
v1.1 fix pass, the `src/CLAUDE.md` header still shows only `Created: May 19, 2026`.
Any future reader cannot tell whether the document has been updated since creation.

**Fix:** Add `**Last Updated:** 2026-05-19 (v1.1 — adversarial review fix pass)`.

---

### L-2 — Profiler marker field naming inconsistent across sections

**Location:** `src/CLAUDE.md` — Game-Loop Rules (uses `s_fixedUpdateMarker`) and
Profiler Markers (uses `s_marker`)

No naming convention is given for the `private static readonly ProfilerMarker`
field itself. The two examples use different names without explanation. Developers
need a rule: e.g., `s_<methodName>Marker` (descriptive) or `s_marker` (generic).

**Fix:** Add a naming rule: "Name the field `s_<EntryPointName>Marker`, e.g.,
`s_fixedUpdateMarker` for `FixedUpdate`, `s_runTickMarker` for `RunTick`." Then
update the Profiler Markers section example to use `s_fixedUpdateMarker` (or the
convention-defined form) instead of the generic `s_marker`.

---

### L-3 — Profiler Markers example missing `using UnityEngine.Profiling;`

**Location:** `src/CLAUDE.md` — Profiler Markers section code block

`ProfilerMarker` lives in `UnityEngine.Profiling`. The example block shows no
`using` directive, yet the `using` directive order section explicitly includes
`using UnityEngine.Profiling;` as an example import. The omission is inconsistent
and will produce `CS0246` for a developer who copies the snippet into a new file.

**Fix:** Add `using UnityEngine.Profiling;` to the example, or add a sentence:
"Requires `using UnityEngine.Profiling;` (see Using Directive Order section)."

---

### L-4 — Missing semicolon in `var` policy example

**Location:** `src/CLAUDE.md` — Naming Conventions section, `var` policy

**Current text:**
> `var result = Compute()` is not — write the explicit type.

The inline code is missing a terminal semicolon. Should be `var result = Compute();`.
Minor but inconsistent with every other C# example in the document.

---

### L-5 — `SplitMix64`, `MatchClock`, and "Project math helper" have no assembly attribution

**Location:** `src/CLAUDE.md` — Determinism Rules section, requirements table

Three required types are named with FR citations but no spec/assembly reference:

| Item | Missing attribution |
|---|---|
| `SplitMix64 helper` | Which assembly? `deterministic-sim`? `project-constants`? |
| `MatchClock` (injected) | Defined in which spec and assembly? |
| Project math helper | Class name? Assembly? Spec? |

Spec #20 §3.4 references `MatchClock` in a constructor-injection example but
does not specify its owning assembly. Without attribution, a developer searching
for these types has no starting point.

**Fix:** Add a "Defined in:" column or inline parenthetical, e.g.,
"`SplitMix64` helper (FR-CS-041) — defined in `deterministic-sim/`; Spec #16 §3.x."
File spec-error-log entries if the authoritative assembly is genuinely unresolved.

---

### L-6 — `BallCollision.cs` in `ball-physics/` vs the separate `collision-system/` assembly

**Location:** `src/CLAUDE.md` — Unity Project Structure tree

The tree shows `BallCollision.cs` inside `ball-physics/` while a separate
`collision-system/` spec assembly exists. A developer will ask: does
`ball-physics/BallCollision.cs` duplicate responsibility with `collision-system/`,
or does it handle only ball-specific collision response while `collision-system/`
handles detection geometry?

**Fix:** Add a one-line comment in the tree:

```
│   ├── BallCollision.cs   ← ball-specific collision response; detection geometry lives in collision-system/
```

---

### L-7 — `[CROSS]` XML doc comment does not satisfy Spec #20 §3.2.3 citation requirement

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[CROSS]` mirrors example

**Current XML doc:**
```csharp
/// [CROSS] Physics tick rate (Hz). Source: ProjectConstants.PhysicsTickHz.
/// Root CLAUDE.md — "Heartbeat Tick Rate": 60 Hz.
```

Spec #20 §3.2.3 requires the XML doc for a `[CROSS]` constant to include
"Tag + authoritative **spec & section**." The root `CLAUDE.md` is not a numbered
spec; citing it does not satisfy the "spec & section" requirement. The authoritative
spec for `PhysicsTickHz` is Ball Physics #1 §1.2 (coordinate/physics fundamentals)
or, once `ProjectConstants.cs` is written, that file's own doc comment becomes
the canonical citation.

**Fix:**
```csharp
/// <summary>
/// [CROSS] Physics/render loop tick rate (Hz). Source: ProjectConstants.cs —
/// PHYSICS_TICK_HZ. Authoritative: Ball Physics #1 §1.2 / Root CLAUDE.md
/// "Heartbeat Tick Rate". Value: 60 Hz.
/// </summary>
```

---

### L-8 — `foreach` ban description "non-struct enumerators" is technically imprecise

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, banned constructs list

**Current text:**
> `foreach` over non-struct enumerators (`List<T>`, `Dictionary<K,V>`)

**Problem:** `Dictionary<TKey,TValue>.Enumerator` IS a struct. The concern is
that `foreach` on a `Dictionary<K,V>` variable typed as `IDictionary<K,V>` or
`IEnumerable<KeyValuePair<K,V>>` boxes the enumerator. The description "non-struct
enumerators" will confuse a developer who checks the CLR source, sees a struct
enumerator, and concludes the ban does not apply.

**Fix:** Replace with a precise statement:

> `foreach` over any type that does not expose a concrete struct `GetEnumerator()`
> at the call site — specifically: `List<T>` via interface reference,
> `Dictionary<K,V>` (boxes enumerator when used via interface), or any
> `IEnumerable<T>` variable. Use arrays or `Span<T>` for hot-path iteration.

---

## Open Items for Tracking

These findings should be filed in `docs/tracking/spec-error-log.md` or resolved
inline before any `[GT]` constant implementation begins. Priority order for
resolution:

1. **H-2** — `ConfigLoader` gap blocks every `[GT]` constant implementation.
   File a spec-error-log entry and identify the owning spec/assembly for the
   loader before Stage 1 coding begins.
2. **H-1** — Arrow label must be corrected before any developer reads the
   dependency architecture section.
3. **M-1, M-2** — Broken examples must be corrected before any developer writes
   a constants catalogue or a zero-alloc game-loop method.
4. **M-3, M-4** — `ProjectConstants.cs` scope and `[CROSS]` routing must be
   clarified before any multi-spec constant is implemented.
5. Remaining M/L findings are lower urgency but should land in the v1.2 fix pass.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-19 | — | Initial adversarial review of src/CLAUDE.md v1.1. 2 H · 7 M · 8 L findings. |
