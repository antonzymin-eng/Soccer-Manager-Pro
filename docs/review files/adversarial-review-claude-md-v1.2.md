# Adversarial Review — src/CLAUDE.md v1.2

> **Created:** 2026-05-19
> **Reviewer:** Adversarial pass (automated)
> **Subject:** `src/CLAUDE.md` v1.2 (post–second-adversarial-review state; 2H · 7M · 8L resolved)
> **Cross-referenced:** Spec #16 §3.4.4; Spec #20 §3.2.3, §3.5.2, §4.2; FR-CS-010, FR-CS-019, FR-CS-044, FR-CS-056, FR-CS-057
> **Findings:** 2 HIGH · 5 MEDIUM · 4 LOW

---

## Summary

`src/CLAUDE.md` v1.2 applied 17 findings from the prior adversarial review and is
substantially correct. Two HIGH defects remain: the Reference Direction diagram's
first line is broken by the H-1 arrow-label fix (the right operand of `←` is now
prose rather than an assembly name), and the `// §3.4.4` comment pattern mandated
by FR-CS-044 is spec-number-ambiguous — it cites a different section in every
assembly that writes a 64-bit unchecked block. Five MEDIUM defects cover arrow
inconsistency in the prohibition sentence, an `async`/`await` scope mismatch, a
missing `.asmdef` for test folders, an asymmetric `foreach` parenthetical, and a
`[GT]` region-comment format that does not match the actual code example. Four LOW
defects cover the author field, `.asmdef` deferral scope, duplicate DI guidance,
and a documentation comment embedded inside a code block.

---

## HIGH Findings

### H-1 — Diagram first line breaks the `A ← B` arrow convention after the v1.2 fix

**Location:** `src/CLAUDE.md` — Reference Direction section

**Current text:**
```
project-constants  ←  referenced read-only by all assemblies

Physics  ←  Mechanics  ←  AI  ←  UI

`←` means "is referenced by" — `A ← B` means B depends on A (B imports from A).
```

**Problem:** The v1.2 H-1 fix correctly relabelled `←` as "is referenced by" and
the main dependency chain (`Physics ← Mechanics ← AI ← UI`) now parses correctly.
However, the first line was not updated: `project-constants ← referenced read-only
by all assemblies` places prose text on the right-hand side of `←`. Per the
established convention, `A ← B` requires B to be an assembly (or assembly group)
name. As written, the line parses as "project-constants is-referenced-by
'referenced read-only by all assemblies'" — the right-hand operand is a sentence
fragment, not an assembly.

A developer reading the diagram as a formal notation (which the label instructs
them to do) will be confused about whether the `←` is decorative or carries the
same "is referenced by" meaning as the rest of the diagram.

**Fix:** Remove the `←` from the explanatory line and express it as a label or
prose annotation:

```
project-constants  (read-only by all assemblies)

Physics  ←  Mechanics  ←  AI  ←  UI
```

Or rewrite both lines using the same form:

```
(all assemblies)  →  project-constants   [read-only; no write path]

Physics  ←  Mechanics  ←  AI  ←  UI
```

The key requirement is that both operands of every `←` are assembly names.

---

### H-2 — `// §3.4.4` comment (FR-CS-044) is spec-number-ambiguous across all assemblies

**Location:** `src/CLAUDE.md` — Determinism Rules section

**Current text:**
```
**64-bit multiplication** must use `unchecked { }` with a `// §3.4.4` comment
(FR-CS-044):

    unchecked  // §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
    {
        state += 0x9E3779B97F4A7C15UL;
    }
```

**Problem:** The bare `§3.4.4` carries no spec number. In `deterministic-sim/`
code this is unambiguous — §3.4.4 is Spec #16's SplitMix64 state-update section.
But FR-CS-044 is a general rule applying to any assembly that performs 64-bit
unchecked arithmetic (e.g., hash arithmetic in `event-system/`, deterministic ID
generation in other assemblies). When a developer in `goalkeeper-mechanics/` or
`perception-system/` writes a 64-bit unchecked block, the comment `// §3.4.4`
cites that assembly's own §3.4.4 — an entirely unrelated section of a different
spec. The comment then conveys false provenance and will mislead future readers.

The SplitMix64 constant `0x9E3779B97F4A7C15UL` in the example makes the
`deterministic-sim/` context obvious for this specific snippet, but the rule is
stated as a universal pattern.

**Fix:** Qualify the section reference with a spec number:

```
**64-bit multiplication** must use `unchecked { }` with a `// Spec #16 §3.4.4`
comment (FR-CS-044), regardless of which assembly the code lives in:

    unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
    {
        state += 0x9E3779B97F4A7C15UL;
    }
```

Add a parenthetical note: "The `Spec #16 §3.4.4` cite is intentionally constant —
it always refers to the SplitMix64 specification, not a section of the local spec."

---

## MEDIUM Findings

### M-1 — `Physics→AI` prohibition uses `→` inconsistent with established `←` convention

**Location:** `src/CLAUDE.md` — Reference Direction section

**Current text:**
```
Upward references (Physics→AI, Mechanics→AI, etc.) are prohibited by FR-CS-046
and enforced as build errors via `.asmdef` reference declarations.
```

**Problem:** After the v1.2 H-1 fix, `←` is the canonical arrow for "is
referenced by." The prohibition sentence introduces `→` without labelling its
meaning. A developer reading both sentences must resolve:

- `Physics ← Mechanics` → "is referenced by" → Mechanics depends on Physics (allowed)
- `Physics→AI` → which convention? If `→` = "depends on" (opposite of `←`), then
  `Physics→AI` = "Physics depends on AI" (prohibited upward dependency). ✓
- But if a developer applies the `←` convention to `→` by inversion, they read
  `Physics→AI` as "AI is referenced by Physics" = "Physics depends on AI" —
  same conclusion, but only by inverting the diagram's own label, which is not
  stated.

The inconsistency does not introduce a wrong rule, but it forces developers to
reason through two competing conventions in adjacent sentences.

**Fix:** Eliminate arrows from the prohibition sentence and use prose:

```
A Physics assembly MUST NOT import from Mechanics or AI. A Mechanics assembly
MUST NOT import from AI. These upward import directions are prohibited by
FR-CS-046 and enforced as build errors via `.asmdef` reference declarations.
```

Or use the established `←` convention consistently:

```
The reverse directions (AI ← Physics, AI ← Mechanics, Mechanics ← Physics in
the wrong role, etc.) are prohibited by FR-CS-046 — these would make a lower
layer depend on a higher layer.
```

---

### M-2 — `async`/`await` ban scope mismatches section heading

**Location:** `src/CLAUDE.md` — Game-Loop Rules section

**Section heading:**
```
**Banned language features in all game-logic code (FR-CS-010):**
```

**`async`/`await` entry:**
```
`async`/`await` for game-state work — breaks deterministic tick ordering;
continuations run on unpredictable frames
```

**Problem:** The heading says the ban covers "all game-logic code." The
`async`/`await` description qualifies the ban to "game-state work" — implying
`async`/`await` is permitted for other code in game-logic assemblies (loading
callbacks, UI notifications, editor tooling). This distinction matters: developers
writing asset-loading or deferred-initialization code inside `decision-tree/` or
`perception-system/` will ask whether they may use `async`/`await` there.

Neither answer is clearly wrong, but the current text gives two incompatible
signals. Spec #20 FR-CS-010 is the authority; `src/CLAUDE.md` should accurately
reproduce the scope.

**Fix:** If the ban applies only to the game-state tick path, add a scoping
qualifier to the section heading:

```
**Banned language features in game-loop / game-state code (FR-CS-010):**
```

And clarify the `async`/`await` entry:

```
`async`/`await` in game-state or per-tick code — breaks deterministic tick
ordering; continuations resume on unpredictable frames. Permitted in
initialization code, editor tooling, and loading pipelines that do not touch
game state.
```

If the ban is truly universal for all game-logic code, remove the "for game-state
work" qualifier.

---

### M-3 — `tests/` folders shown without `.asmdef` files; test code would compile into production assembly

**Location:** `src/CLAUDE.md` — Unity Project Structure tree

**Current text (representative entries):**
```
├── ball-physics/
│   ├── ball-physics.asmdef
│   ├── BallPhysicsCore.cs
│   └── tests/
│       ├── BallPhysicsCoreTests.cs
│       └── BallIntegrationTests.cs
```

**Problem:** Unity requires test code to be in a separate `.asmdef` with
`"testPlatforms": ["EditMode"]` (or PlayMode) and references to the NUnit /
UnityTestTools packages. Without a separate `.asmdef`, test files in `tests/`
are compiled into the parent assembly (`ball-physics.asmdef`), shipping test code
into production builds and potentially creating circular references when test
assemblies reference each other.

The document states "One `.asmdef` per folder" but `tests/` is a subfolder and
the rule as stated is ambiguous about whether it applies to test subfolders. The
`.asmdef` note under the tree ("Every spec folder listed above requires a
`.asmdef` file") covers spec-level folders but not their `tests/` subdirectories.

**Fix:** Add a `.asmdef` placeholder to each `tests/` entry:

```
│   └── tests/
│       ├── ball-physics-tests.asmdef   ← EditMode; references ball-physics.asmdef
│       ├── BallPhysicsCoreTests.cs
│       └── BallIntegrationTests.cs
```

And add a note: "Every `tests/` subdirectory requires its own `.asmdef` with
`testPlatforms` set to `[EditMode]` (or as specified in Spec #19 §7.5 D2). Test
assemblies are excluded from production builds via platform filtering."

---

### M-4 — `foreach` parenthetical mentions only `Dictionary.Enumerator`; `List<T>.Enumerator` is equally a struct

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, banned constructs list

**Current text:**
```
`foreach` over any type that does not expose a concrete struct `GetEnumerator()`
at the call site — including `List<T>` or `Dictionary<K,V>` via an interface
variable (the enumerator is boxed even though `Dictionary.Enumerator` is itself
a struct); use arrays or `Span<T>` for hot-path iteration
```

**Problem:** The parenthetical cites `Dictionary.Enumerator` as the illustrative
example of a struct enumerator that is boxed when accessed through an interface.
`List<T>.Enumerator` is equally a struct (`System.Collections.Generic.List<T>+Enumerator`)
and is equally boxed when the `List<T>` variable is typed as `IEnumerable<T>` or
`IList<T>`. A developer who looks up `List<T>.Enumerator` in the CLR source, finds
a struct, and reads the parenthetical will conclude the concern only applies to
`Dictionary` — and will keep iterating `List<IEnumerable<T>>` references on hot
paths.

**Fix:**
```
`foreach` over any type that does not expose a concrete struct `GetEnumerator()`
at the call site — including `List<T>` or `Dictionary<K,V>` via an interface
variable (both `List<T>.Enumerator` and `Dictionary.Enumerator` are structs, but
the enumerator is boxed when the collection variable is typed as an interface);
use arrays or `Span<T>` for hot-path iteration
```

---

### M-5 — `[GT]` region comment format (`/* loader TBD */`) differs from the actual code pattern (`= 8; // TODO:`)

**Location:** `src/CLAUDE.md` — Constant Catalogues section, region taxonomy

**Current region comment:**
```csharp
#region GT  // [GT] → public static readonly int MaxSubsteps = /* loader TBD — see note below */;
```

**Actual code example (same section, below):**
```csharp
public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
```

**Problem:** The two forms are inconsistent. The region comment shows a
`/* ... */` block replacing the initializer, which is syntactically valid C# but
omits the design-time default. The code example shows the correct pattern: a
hardcoded default with a trailing `// TODO:` comment. A developer copying the
taxonomy comment verbatim writes `MaxSubsteps = /* loader TBD */;` — missing the
default value — and must infer the correct form from the separate code example.

**Fix:** Align the region comment with the actual pattern:

```csharp
#region GT  // [GT] → public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
```

---

## LOW Findings

### L-1 — Author field required by FR-CS-056/057 but `src/CLAUDE.md`'s own version history uses `—`

**Location:** `src/CLAUDE.md` — File Header section and Version History table

**File header rule:**
```
**Required fields (FR-CS-056/057):** file path, created date (ISO), modified date,
author, governing specs, purpose (≤ 2 sentences).
```

**`src/CLAUDE.md` version history:**
```
| 1.0 | 2026-05-19 | — | Initial creation. … |
| 1.1 | 2026-05-19 | — | Adversarial review v1.0 fix pass. … |
| 1.2 | 2026-05-19 | — | Adversarial review v1.1 fix pass. … |
```

**Problem:** All three version history rows use `—` (em dash) for the Author
column. If `—` is an accepted placeholder for AI-authored or anonymous entries,
FR-CS-056/057 should say so explicitly — otherwise `src/CLAUDE.md` violates by
example the rule it defines.

**Fix:** Either (a) document that `—` is the accepted author value for AI-authored
changes: "When a file is authored or modified by an automated agent with no named
author, use `—` in the Author field." Or (b) use a consistent token such as
`AI-agent` or `Claude` that satisfies the field requirement while being
informative.

---

### L-2 — `.asmdef` WHAT IS NOT HERE YET entry scoped only to GUIDs; other unresolved fields not listed

**Location:** `src/CLAUDE.md` — WHAT IS NOT HERE YET table

**Current entry:**
```
| `.asmdef` GUIDs | Unity project initialization |
```

**Problem:** Unity `.asmdef` files require several fields besides GUIDs:
`allowUnsafeCode` (relevant given the `unsafe` sign-off rule), `autoReferenced`
(should be `false` for spec assemblies to enforce explicit references),
`testPlatforms` (for `tests/` subfolders — see M-3 above), and
`versionDefines` (if conditional compilation is used). None of these appear in the
WHAT IS NOT HERE YET table. If they are already decided (all `false`,
`false`, `[EditMode]`, `[]` respectively), that should be stated. If they are not
yet decided, the deferral scope is understated.

**Fix:** Add a row or expand the existing row to list all undecided `.asmdef`
fields:

```
| `.asmdef` content | Unity project initialization — GUIDs, `allowUnsafeCode`,
`autoReferenced`, `testPlatforms`, `versionDefines` |
```

---

### L-3 — Constructor injection guidance appears in two sections without cross-reference

**Location:** `src/CLAUDE.md` — Game-Loop Rules section and Banned Architectural
Patterns subsection

**Problem:** Constructor injection is introduced under "GAME-LOOP RULES (ZERO
ALLOCATION)" as a required pattern:

> **Dependency injection via constructor parameters** — pass dependencies into
> constructors; do not resolve them at runtime

And it appears again as the required alternative to four banned anti-patterns
(service locator, ambient context, static singleton, generic DI container):

> The required alternative to all four is **constructor injection**: pass
> dependencies as constructor parameters.

The two mentions are substantively identical but neither links to the other. A
developer reading only the anti-patterns section does not see the zero-allocation
framing; a developer reading only the zero-allocation section does not see the
specific anti-patterns being avoided. This creates a maintenance risk: if the
guidance changes, both mentions must be updated.

**Fix:** In the zero-allocation required-patterns list, replace the standalone
bullet with a forward reference:

> **Dependency injection via constructor parameters** — see "Banned Architectural
> Patterns" below for the full rule and the four anti-patterns it replaces.

And keep the authoritative statement in the anti-patterns section where the
rationale is explained in detail.

---

### L-4 — Documentation prose embedded inside the game-loop COMPLIANT code block

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, COMPLIANT example

**Current text:**
```csharp
// ProfilerMarker field: private static readonly; named s_<EntryPointName>Marker
private static readonly ProfilerMarker s_updateBallPhysicsMarker =
    new ProfilerMarker("BallPhysics.UpdateBallPhysics");

// COMPLIANT
// Note: `state with { … }` requires C# 10+ on readonly structs. Verify the
// Unity LTS + backend in certification-platform.md before using this pattern.
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_updateBallPhysicsMarker.Auto();
    state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
}
```

**Problem:** The first line `// ProfilerMarker field: private static readonly;
named s_<EntryPointName>Marker` is documentation prose masquerading as an inline
C# comment. A developer copying this example verbatim into a source file commits
a comment that reads like a spec note rather than a code justification. The
INLINE COMMENTS rule (FR-CS-064) requires comments only when the WHY is
non-obvious — a comment that restates the naming convention (documented separately
in the Profiler Markers section) violates that rule.

**Fix:** Move the naming convention description outside the code block:

> The `ProfilerMarker` field must be `private static readonly`, named
> `s_<EntryPointName>Marker` (e.g., `s_updateBallPhysicsMarker` for
> `UpdateBallPhysics`):
>
> ```csharp
> private static readonly ProfilerMarker s_updateBallPhysicsMarker =
>     new ProfilerMarker("BallPhysics.UpdateBallPhysics");
> ```

---

## Open Items for Tracking

Resolution priority:

1. **H-2** — `// §3.4.4` ambiguity should be corrected before any assembly other
   than `deterministic-sim/` writes unchecked 64-bit arithmetic. File a spec-error-log
   entry if FR-CS-044 needs an amendment in Spec #20.
2. **H-1** — Diagram first line must be fixed before any developer reads the
   dependency architecture section; the broken parse is immediately visible.
3. **M-3** — Test `.asmdef` gap should be closed before any `tests/` folder is
   created; retroactive restructuring once test files exist is disruptive.
4. **M-1, M-2** — Arrow inconsistency and `async`/`await` scope should be resolved
   in the next fix pass; both affect developer decisions on day one of coding.
5. **M-4, M-5** — `foreach` parenthetical and `[GT]` region comment are lower
   urgency but should land in the same fix pass.
6. **L-1 through L-4** — Cosmetic or low-impact; batch with the next fix pass.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-19 | — | Adversarial review of src/CLAUDE.md v1.2. 2H · 5M · 4L findings. |
