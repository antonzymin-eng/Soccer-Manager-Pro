# Adversarial Review — src/CLAUDE.md v1.3

> **Created:** 2026-05-22
> **Reviewer:** Adversarial pass (automated)
> **Subject:** `src/CLAUDE.md` v1.3 (post–third-adversarial-review state; 2H · 5M · 4L resolved)
> **Cross-referenced:** Spec #20 §3.2.3, §3.3.3, §3.5.2, §4.2; FR-CS-010, FR-CS-021, FR-CS-022, FR-CS-035, FR-CS-070
> **Findings:** 1 HIGH · 4 MEDIUM · 3 LOW

---

## Summary

`src/CLAUDE.md` v1.3 resolved all 11 prior-pass findings (2H · 5M · 4L) and is
substantially correct. However, cross-validation of the COMPLIANT game-loop code
example against Spec #20 §3's canonical `BallStateSystem` class reveals one HIGH
defect: the entry point method is marked `public static`, directly contradicting
the instance-class architecture mandated by Spec #20. Four MEDIUM defects cover
incomplete `[EST]` promotion targets, MonoBehaviour lifecycle naming confusion,
an undocumented spec-error-log entry for a known Spec #20 §4.2 discrepancy, and
a missing `stackalloc` safety distinction. Three LOW defects cover a vague §3.2
section citation, a missing `[DERIVED]` worked example, and a `#region` naming
case inconsistency.

---

## HIGH Findings

### H-1 — Game-Loop Rules COMPLIANT example uses `public static void`; Spec #20 mandates `public void` (instance) for system entry points

**Location:** `src/CLAUDE.md` — Game-Loop Rules section (lines ~360–377)

**Current text in `src/CLAUDE.md`:**
```csharp
private static readonly ProfilerMarker s_updateBallPhysicsMarker =
    new ProfilerMarker("BallPhysics.UpdateBallPhysics");

// COMPLIANT
public static void UpdateBallPhysics(ref BallState state, float dt)
{
    using var _ = s_updateBallPhysicsMarker.Auto();
    state = state with { Velocity = … };
}
```

**Authoritative counterpart in Spec #20 §3 (section-3.md, the access-modifier
COMPLIANT example):**
```csharp
// COMPLIANT — FR-CS-014: explicit on every declaration
public sealed class BallStateSystem
{
    private readonly MatchClock _clock;
    private static readonly ProfilerMarker s_marker = …;

    public BallStateSystem(MatchClock clock) { … }
    public void Update(ref BallState state) { … }         ← instance, not static
    private static void ApplyDrag(ref BallState state, float dt) { … }
}
```

**Problem:** Spec #20 §3 establishes that game systems are **sealed instance classes**
with **`public void` entry points** and constructor-injected dependencies. Static
methods are the pattern for **private internal helpers** (`private static void
ApplyDrag`), not for public API entry points.

`src/CLAUDE.md`'s COMPLIANT example inverts this: `UpdateBallPhysics` is `public
static void`, matching the *helper* pattern, not the entry-point pattern. A
developer reading the Game-Loop Rules section will write systems as static classes —
contradicting:

1. **Spec #20's own example** (instance `Update` vs static `ApplyDrag`).
2. **Constructor injection** — which the document requires (lines 351–356) but is
   only applicable to instance classes.
3. **The Profiler Markers section within `src/CLAUDE.md` itself** (line 528:
   `public void FixedUpdate(ref BallState state, float dt)`) — which correctly
   uses an instance method.

Note: a separate `public static void ApplyDrag` COMPLIANT example *does* appear in
Spec #20 §3.3.3 (FR-CS-033 ref-passed struct section) as a helper illustration —
this is likely the source the `src/CLAUDE.md` author drew from. The distinction
between helper and entry point is not made explicit in Spec #20 §3.3.3 either,
but the canonical `BallStateSystem` example (§3's access-modifier section) is
authoritative.

**Fix:** Rewrite the COMPLIANT example to match the `BallStateSystem` architecture
pattern from Spec #20 §3:

```csharp
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateBallPhysicsMarker =
        new ProfilerMarker("BallPhysics.UpdateBallPhysics");

    // COMPLIANT — instance method; dependencies injected via constructor
    // Note: `state with { … }` requires C# 10+ on readonly structs. Verify the
    // Unity LTS + backend in certification-platform.md before using this pattern.
    public void UpdateBallPhysics(ref BallState state, float dt)
    {
        using var _ = s_updateBallPhysicsMarker.Auto();
        state = state with { Velocity = state.Velocity * (1f - BallPhysicsConstants.DRAG_COEFFICIENT * dt) };
    }
}

// VIOLATION — copies struct by value; wastes memory bandwidth
public void UpdateBallPhysics(BallState state, float dt) { … }
```

Also update the Profiler Markers section example for consistency (change `FixedUpdate`
to `Update` to match the canonical Spec #20 naming, and add the enclosing class
declaration to make the `private static readonly` field context unambiguous).

---

## MEDIUM Findings

### M-1 — `[EST]` promotion target list omits `[DERIVED]` and `[CROSS]` as valid outcomes

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[EST]` constants
paragraph (lines ~293–294)

**Current text:**
> The constant must be promoted to `[GT]` or `[FIXED]` before the system that
> consumes it is implemented.

**Problem:** An `[EST]` constant is a validated placeholder. When validation
completes, the correct replacement tag is determined by the constant's nature:

| Validated outcome | Correct tag |
|---|---|
| Designer-tunable value | `[GT]` |
| Physically fixed law | `[FIXED]` |
| Derivable from other constants via formula | `[DERIVED]` |
| Already defined authoritatively in another spec | `[CROSS]` |

By listing only `[GT]` or `[FIXED]`, the document will cause developers to:
- Re-tag a derivable value as `[GT]` instead of `[DERIVED]`, bypassing the FR-CS-021
  formula-documentation requirement.
- Duplicate a constant already defined in another spec rather than cross-referencing
  it (`[CROSS]`), creating divergence risk.

Spec #20 §3.2.3 does not enumerate promotion targets either — making this gap
present in both the spec and the coding guide.

**Fix:** Replace the sentence:

> The constant must be promoted to `[GT]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]`
> before the system that consumes it is implemented. If the validated value is
> derivable via formula, use `[DERIVED]` (document the formula per §3.2.3
> FR-CS-021). If it already exists authoritatively in another spec, use `[CROSS]`
> (cite the authoritative spec and section per §3.2.3 FR-CS-022).

---

### M-2 — Profiler Markers section names `FixedUpdate` and `Update` as system entry points — Unity lifecycle method names that are incompatible with ref-parameter signatures

**Location:** `src/CLAUDE.md` — Profiler Markers section (lines ~515–532)

**Current text:**
> Every system entry point (`FixedUpdate`, `Update`, tick method) must be wrapped
> in a `ProfilerMarker.Auto()`.

```csharp
public void FixedUpdate(ref BallState state, float dt)
{
    using var _ = s_fixedUpdateMarker.Auto();
    // …
}
```

**Problem:** `FixedUpdate` and `Update` are Unity MonoBehaviour lifecycle method
names. Unity calls them with **no parameters** (`void FixedUpdate()`). The method
signature shown — `public void FixedUpdate(ref BallState state, float dt)` — is
not a valid Unity MonoBehaviour lifecycle method. It will never be called by Unity.

A developer reading "wrap `FixedUpdate` and `Update` in a `ProfilerMarker`" faces
two contradictory conclusions:
1. If game systems ARE MonoBehaviours, their `FixedUpdate()` takes no parameters —
   contradicting the struct-based `ref` architecture.
2. If game systems are NOT MonoBehaviours (correct per the zero-alloc design),
   then `FixedUpdate` is just a misleading name for what the H-1 finding establishes
   should be called `Update(ref BallState state)` per Spec #20 §3.

Neither answer is derivable from the current text. There is no explanation of the
integration layer between Unity's actual MonoBehaviour event loop and the
struct-based game systems — and no guidance on whether *that* layer's lifecycle
methods also need profiler markers.

This is distinct from H-1, which covers the static/instance error in the code
example. This finding covers the terminology confusion (`FixedUpdate`/`Update`
as Unity lifecycle names) and the undocumented MonoBehaviour boundary.

**Fix:**
1. Replace `FixedUpdate` with the Spec #20-aligned `Update` (or a generic
   non-Unity name like `Tick`/`RunStep`) in the entry point list and example.
2. Add a one-sentence note: "These are custom methods on game system classes —
   NOT Unity MonoBehaviour lifecycle callbacks. Unity lifecycle integration
   is a Stage 1 concern deferred to the Unity project setup phase."
3. Add a WHAT IS NOT HERE YET row: "MonoBehaviour / PlayerLoop integration
   pattern — blocked on Unity project initialization."

---

### M-3 — Spec #20 §4.2 `[CROSS]` mirror naming error documented in `src/CLAUDE.md` but absent from `spec-error-log.md`; approved spec §4.2 still shows the incorrect example

**Location:** `src/CLAUDE.md` — Constant Catalogues section, naming discrepancy
note (lines ~311–318); `docs/tracking/spec-error-log.md` (no entry)

**Current `src/CLAUDE.md` note:**
> **Note — naming discrepancy in Spec #20 §4.2:** The §4.2 worked example shows
> `PHYSICS_TICK_HZ` (ALL_CAPS) for the `[CROSS]` *mirror* field in
> `BallPhysicsConstants.cs`. This contradicts §3.2.3 … §3.2.3 is authoritative —
> use PascalCase for the mirror field name.

**Spec #20 §4.2 source (section-4.md, lines ~154–160) — current state:**
```csharp
// In BallPhysicsConstants.cs (mirror)
/// [CROSS] Physics tick rate (Hz).
/// Authoritative source: ProjectConstants.cs — ProjectConstants.PHYSICS_TICK_HZ.
public static readonly float PHYSICS_TICK_HZ = ProjectConstants.PHYSICS_TICK_HZ;
//                           ^^^^^^^^^^^^^^^^^^^
//                           ALL_CAPS — contradicts §3.2.3 PascalCase rule
```

**Problem (three-part):**

1. **Spec #20 §4.2 still contains the incorrect example.** The discrepancy was
   identified and the correct rule established, but Spec #20 §4.2 was never
   patched. Any developer who reads only Spec #20 (bypassing `src/CLAUDE.md`) will
   follow the §4.2 example and use ALL_CAPS for `[CROSS]` mirror fields.

2. **No `spec-error-log.md` entry exists.** The project convention (root `CLAUDE.md`
   cross-reference system) tracks spec defects in `spec-error-log.md`. This
   discrepancy in an APPROVED spec qualifies and has no entry. The resolution
   rationale is currently preserved only in `src/CLAUDE.md`, where it is at risk
   of being edited or lost.

3. **The §4.2 example XML doc** (`/// [CROSS]`) also lacks the required `<summary>`
   tag (FR-CS-060) — a secondary defect in the same example.

**Fix:**
1. File a `spec-error-log.md` entry recording the §4.2/§3.2.3 conflict and its
   resolution (§3.2.3 PascalCase is authoritative). Cross-reference the entry
   from `src/CLAUDE.md`'s discrepancy note.
2. Patch Spec #20 §4.2 `section-4.md` to show the correct PascalCase mirror
   field name (`PhysicsTickHz`) and add `<summary>` tag to the XML doc, matching
   the `src/CLAUDE.md` example.

---

### M-4 — `stackalloc` listed as a required pattern without distinguishing the safe `Span<T>` form from the `unsafe` pointer form

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, required patterns list
(line ~327) and banned language features list (lines ~347–348)

**Current required-patterns list:**
> - `stackalloc` for transient buffers with statically bounded size

**Current banned-features list:**
> - `unsafe` without lead-developer sign-off recorded in the PR description

**Problem:** `stackalloc` has two syntactically distinct forms with different
safety profiles:

| Form | Syntax | `unsafe` required? |
|---|---|---|
| C# 7.2+ `Span<T>` form | `Span<int> buf = stackalloc int[n];` | No |
| Traditional pointer form | `int* p = stackalloc int[n];` | Yes — requires `unsafe` block |

The document lists `stackalloc` as unconditionally required and `unsafe` as
unconditionally requiring sign-off. A developer who uses the pointer form of
`stackalloc` (which requires `unsafe`) is told both "MUST use this" and "MUST
get sign-off." A developer who uses the `Span<T>` form may not realize it is the
intended (and only unsupervised-permitted) form.

Spec #20 §3 (section-3.md line 383) lists `stackalloc` with FR-CS-035 but also
does not distinguish the two forms — making this gap present in both the upstream
spec and `src/CLAUDE.md`. It is especially important that `src/CLAUDE.md` clarify
this since it is the developers' primary code-writing reference.

**Fix:** Qualify the `stackalloc` entry:

> - `stackalloc` with `Span<T>` for transient buffers with statically bounded size
>   (C# 7.2+; no `unsafe` block required). The traditional pointer form
>   (`int* p = stackalloc int[n]`) requires `unsafe` and therefore lead-developer
>   sign-off per FR-CS-010. Use the `Span<T>` form by default.

---

## LOW Findings

### L-1 — `[GT]` XML doc example cites `Code Standards #20 §3.2` — should be `§3.2.3`

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[GT]` loading
mechanism example (lines ~289–292)

**Current text:**
```csharp
/// <summary>[GT] Maximum physics substeps per frame. Code Standards #20 §3.2.</summary>
public static readonly int MaxSubsteps = 8; // TODO: replace with config loader (Stage 1)
```

**Problem:** Every other section citation in `src/CLAUDE.md` that refers to
constant-naming rules targets `§3.2.3` specifically (the "Tag → C# Storage Class
Mapping" subsection). `§3.2` is the parent section covering all naming conventions.
Citing `§3.2` points developers to a broader section instead of the rule they need.

**Fix:** Change `Code Standards #20 §3.2` → `Code Standards #20 §3.2.3`.

---

### L-2 — `[DERIVED]` region comment shows `TerminalVelocity = …` with no worked example; Spec #20 §3.2.3 requires "Tag + formula + source constants" in the XML doc

**Location:** `src/CLAUDE.md` — Constant Catalogues section, region taxonomy
(line ~279) and constant examples

**Current region taxonomy:**
```csharp
#region Derived    // [DERIVED] → public static readonly float TerminalVelocity = …;
```

**Problem:** The region taxonomy shows `= …` (ellipsis) with no formula. Unlike all
other tags — `[FIXED]` (`= 0.11f`), `[GT]` (`= 8; // TODO:`), `[CROSS]`
(`= ProjectConstants.PHYSICS_TICK_HZ`), `[EST]` (`= 0.35f; // TODO: validate`) —
`[DERIVED]` has no concrete value or formula pattern. A developer cannot determine:
- What the RHS of a `[DERIVED]` constant looks like in practice.
- What the required XML doc format looks like (Spec #20 §3.2.3: "Tag + formula +
  source constants" in the summary — different from other tags).

**Fix:** Add a worked example for `[DERIVED]`:

```csharp
/// <summary>
/// [DERIVED] Terminal velocity (m/s) where drag force equals gravity.
/// Formula: sqrt(GRAVITY / DRAG_COEFFICIENT). FM-NNN.
/// Source constants: BallPhysicsConstants.GRAVITY, BallPhysicsConstants.DRAG_COEFFICIENT.
/// Ball Physics #1 §3.x.
/// </summary>
public static readonly float TerminalVelocity =
    Mathf.Sqrt(BallPhysicsConstants.GRAVITY / BallPhysicsConstants.DRAG_COEFFICIENT);
```

Note: substitute actual formula references (FM-NNN, §3.x) from the relevant spec
when implementing.

---

### L-3 — `#region` header names (`Fixed`, `Derived`, `Cross`) use Title Case while their constant tags (`[FIXED]`, `[DERIVED]`, `[CROSS]`) use ALL_CAPS; inconsistency undocumented

**Location:** `src/CLAUDE.md` — Constant Catalogues section, region taxonomy
(lines ~276–283)

**Current region headers:**
```csharp
#region Fixed      // [FIXED]   → …
#region Derived    // [DERIVED] → …
#region Cross      // [CROSS]   → …
#region GT         // [GT]      → …
#region EST        // [EST]     → …
```

**Problem:** `Fixed`, `Derived`, and `Cross` are Title Case while `GT` and `EST`
match their tags exactly (already ALL_CAPS). No rule is stated for region naming.
A developer implementing a new catalogue file must guess the convention, and could
reasonably choose either `#region FIXED` (ALL_CAPS, matching the tag) or
`#region Fixed` (Title Case, matching the examples). If different catalogue files
use different region-naming conventions, grep-based tooling that searches for
`#region` headers will give inconsistent results.

**Fix:** Either (a) document the region naming convention explicitly ("Region names
use Title Case; `GT` and `EST` are both tag name and Title Case since they are
already two-letter acronyms"), or (b) adopt a consistent convention and update
the examples. Option (a) requires only adding a prose note. Option (b) requires
choosing ALL_CAPS (`#region FIXED`) or Title Case (`#region Gt`) — Title Case is
cleaner since `#region FIXED` conflicts visually with constant identifiers
(`BALL_RADIUS`) that appear immediately below.

---

## Open Items for Tracking

Resolution priority:

1. **H-1** — The static/instance ambiguity affects every system the first developer
   implements. Patch the COMPLIANT example before any game-loop code is written.
2. **M-3** — File the spec-error-log entry and patch Spec #20 §4.2 before any
   `[CROSS]` mirror constant is implemented; if a developer follows the §4.2
   example they will use ALL_CAPS and the convention will diverge across the
   codebase immediately.
3. **M-1** — Patch `[EST]` promotion targets before any `[EST]` constant is
   validated; incorrect re-tagging to `[GT]` instead of `[DERIVED]` cannot be
   caught by tooling.
4. **M-2** — Patch entry point naming terminology before developers structure their
   first system class; the MonoBehaviour integration boundary has no guidance.
5. **M-4** — Patch `stackalloc` distinction before any buffer-heavy system (e.g.,
   collision detection) is implemented.
6. **L-1, L-2, L-3** — Low urgency; batch with the next fix pass.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-22 | — | Adversarial review of `src/CLAUDE.md` v1.3. 1H · 4M · 3L findings. |
