# Adversarial Review — src/CLAUDE.md v1.4

> **Created:** 2026-05-22
> **Reviewer:** Adversarial pass (automated)
> **Subject:** `src/CLAUDE.md` v1.4 (post–fourth-adversarial-review state; 1H · 4M · 3L resolved)
> **Cross-referenced:** Spec #20 §3.2.3, §3.3.3, §3.5.2, §4.2; FR-CS-022, FR-CS-051–054, FR-CS-070
> **Findings:** 1 HIGH · 1 MEDIUM · 5 LOW

---

## Summary

`src/CLAUDE.md` v1.4 resolved all 8 prior-pass findings. One HIGH defect remains: the
COMPLIANT game-loop example introduces `BallPhysicsSystem` with no constructor, directly
contradicting the "constructor injection is required" rule in the same paragraph. One
MEDIUM defect: the same class name (`BallPhysicsSystem`) appears with method
`UpdateBallPhysics` in the Game-Loop section and `Update` in the Profiler Markers section,
giving developers two irreconcilable canonical names for the same class's entry point.
Five LOW defects cover a factual error in the region convention note, the VIOLATION
method floating outside any class, a non-spec citation in the `[CROSS]` XML doc example,
a conflated ProfilerMarker required-patterns bullet, and a missing single-consumer
`[CROSS]` mirror example.

---

## HIGH Findings

### H-1 — COMPLIANT `BallPhysicsSystem` example has no constructor; directly contradicts the immediately-preceding "constructor injection is required" rule

**Location:** `src/CLAUDE.md` — Game-Loop Rules section (lines ~373–396)

**Current text:**
```csharp
// COMPLIANT — sealed instance class; ProfilerMarker field is private static readonly
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateBallPhysicsMarker =
        new ProfilerMarker("BallPhysics.UpdateBallPhysics");

    public void UpdateBallPhysics(ref BallState state, float dt)
    {
        using var _ = s_updateBallPhysicsMarker.Auto();
        state = state with { … };
    }
}
```

**Rule stated three lines earlier (line ~373):**
> The required alternative to all four [banned patterns] is **constructor injection**:
> pass dependencies as constructor parameters.

**Spec #20 §3 authoritative pattern (section-3.md lines ~182–192):**
```csharp
public sealed class BallStateSystem
{
    private readonly MatchClock _clock;
    private static readonly ProfilerMarker s_marker = …;

    public BallStateSystem(MatchClock clock) { … }    ← constructor shown
    public void Update(ref BallState state) { … }
    private static void ApplyDrag(ref BallState state, float dt) { … }
}
```

**Problem:** The COMPLIANT example's `BallPhysicsSystem` has no constructor and no
injected fields. A developer who copies this example verbatim produces a class that
cannot receive any dependencies — so to get hold of, e.g., `MatchClock` they must
use a service locator, ambient context, or static singleton — all explicitly banned
by the four anti-patterns listed immediately above. The COMPLIANT label signals
"sufficient" — creating a direct contradiction.

This is the only full game-system class example in the document. Every developer
who writes a new system will reference it. The missing constructor makes the example
factually incomplete relative to the rule it is supposed to demonstrate.

**Fix:** Add a representative injected dependency to the COMPLIANT example to match
the Spec #20 §3 `BallStateSystem` pattern:

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

// VIOLATION — copies struct by value; wastes memory bandwidth
// (method shown inside BallPhysicsSystem above)
// public void Update(BallState state, float dt) { … }
```

Note: the fix also resolves M-1 by aligning `Update` / `s_updateMarker` with the
Profiler Markers section.

---

## MEDIUM Findings

### M-1 — `BallPhysicsSystem` has `UpdateBallPhysics` in the Game-Loop section and `Update` in the Profiler Markers section — same class name, incompatible entry-point method names

**Location:** `src/CLAUDE.md` — Game-Loop Rules (line ~387) and Profiler Markers (line ~554)

**Game-Loop section:**
```csharp
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateBallPhysicsMarker =
        new ProfilerMarker("BallPhysics.UpdateBallPhysics");

    public void UpdateBallPhysics(ref BallState state, float dt) { … }
}
```

**Profiler Markers section:**
```csharp
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_updateMarker =
        new ProfilerMarker("BallPhysics.Update");

    public void Update(ref BallState state, float dt) { … }
}
```

**Problem:** Both examples use the class name `BallPhysicsSystem` but give it different
entry-point method names (`UpdateBallPhysics` vs `Update`), different field names
(`s_updateBallPhysicsMarker` vs `s_updateMarker`), and different profiler strings
(`"BallPhysics.UpdateBallPhysics"` vs `"BallPhysics.Update"`). A developer writing
`BallPhysicsSystem` must choose between the two; neither example acknowledges the
other. Spec #20 §3's canonical class (`BallStateSystem`) uses `Update` — the
Profiler Markers example is correct; the Game-Loop example is not.

**Fix:** Align the Game-Loop example to use `Update` / `s_updateMarker` /
`"BallPhysics.Update"`, consistent with the Profiler Markers section and Spec #20 §3.
This is the same change as the H-1 fix — resolving both in a single edit.

---

## LOW Findings

### L-1 — Region convention note says "two-letter acronyms" — `EST` has three letters

**Location:** `src/CLAUDE.md` — Constant Catalogues section (line ~286)

**Current text:**
> `GT` and `EST` match their tag names exactly since those are already **two-letter
> acronyms**.

**Problem:** `GT` has 2 letters ✓. `EST` has 3 letters — it is not a two-letter
acronym. The intent is to explain why `GT` and `EST` use ALL_CAPS for their region
names (they match the tag name directly) whereas `Fixed`, `Derived`, and `Cross`
use Title Case. The rationale is correct; the phrasing is not.

**Fix:** Replace "two-letter acronyms" with "all-caps abbreviations":
> `GT` and `EST` match their tag names exactly since those are already all-caps
> abbreviations.

---

### L-2 — VIOLATION method appears after the COMPLIANT class closes — orphaned outside any class context

**Location:** `src/CLAUDE.md` — Game-Loop Rules section (lines ~393–395)

**Current text:**
```csharp
}  ← closes BallPhysicsSystem

// VIOLATION — copies struct by value; wastes memory bandwidth
public void UpdateBallPhysics(BallState state, float dt) { … }
```

**Problem:** A method declaration at file scope is invalid C#. The VIOLATION example
is syntactically unreachable as written. A developer copying this snippet verbatim
will get a compile error. More subtly, a developer reading the example has no context
that the VIOLATION is a member of `BallPhysicsSystem` — they might conclude it is
intended as a free function or belongs to a different class.

**Fix:** Comment-out the VIOLATION and annotate it as a member that would go inside
the class (note this also eliminates the name-consistency problem resolved by M-1):

```csharp
    // VIOLATION — copies struct by value; wastes memory bandwidth
    // public void Update(BallState state, float dt) { … }
}
```

---

### L-3 — `[CROSS]` XML doc example includes `Root CLAUDE.md "Heartbeat Tick Rate"` — a non-spec document — alongside the valid `Ball Physics #1 §1.2` spec citation

**Location:** `src/CLAUDE.md` — Constant Catalogues section (lines ~319–323)

**Current text:**
```csharp
/// <summary>
/// [CROSS] Physics/render loop tick rate (Hz).
/// Authoritative source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Ball Physics #1 §1.2 / Root CLAUDE.md "Heartbeat Tick Rate". Value: 60 Hz.
/// </summary>
```

**Problem:** FR-CS-022 requires `[CROSS]` doc comments to cite the "authoritative
spec & section." `Ball Physics #1 §1.2` satisfies this requirement. `Root CLAUDE.md
"Heartbeat Tick Rate"` is a project coordination document — not a numbered spec.
The example as written suggests that CLAUDE.md is an acceptable citation source for
`[CROSS]` constants. A developer following this example will add `Root CLAUDE.md`
citations to their own `[CROSS]` constants, violating FR-CS-022's spec-and-section
requirement.

**Fix:** Remove the CLAUDE.md reference; the spec+section citation is sufficient:
```csharp
/// <summary>
/// [CROSS] Physics/render loop tick rate (Hz).
/// Authoritative source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Ball Physics #1 §1.2. Value: 60 Hz.
/// </summary>
```

---

### L-4 — Required-patterns bullet conflates `ProfilerMarker.Auto()` (the call) with the `private static readonly` field (the storage) — parenthetical describes the field but is grammatically attached to the call

**Location:** `src/CLAUDE.md` — Game-Loop Rules section, required patterns list (line ~347)

**Current text:**
> - `ProfilerMarker.Auto()` on every system entry point (static readonly field —
>   one-time alloc at startup)

**Problem:** The bullet describes the call (`Auto()`) but the parenthetical "(static
readonly field — one-time alloc at startup)" describes the *marker field*, not the
call. A developer parsing this reads: "`ProfilerMarker.Auto()` [which is a static
readonly field]" — but `Auto()` is an instance method that returns an `AutoScope`
struct; it is not a static readonly field. The correct model is:

1. Declare a `private static readonly ProfilerMarker` field → one-time heap alloc at startup
2. Call `.Auto()` at each entry point → returns a stack-allocated `AutoScope`; zero per-frame heap alloc

**Fix:**
> - `private static readonly ProfilerMarker` field on every system class for profiling
>   (one-time alloc at startup); call `.Auto()` at each entry point to bracket the
>   measurement scope (FR-CS-070)

---

### L-5 — No worked example for single-consumer `[CROSS]` mirror; only the multi-consumer path (via `ProjectConstants.cs`) is illustrated

**Location:** `src/CLAUDE.md` — Constant Catalogues section, `[CROSS]` mirrors (lines ~311–323)

**Problem:** The routing rule documents two paths but the worked example covers only one:

> - **Multi-consumer** → declare in `ProjectConstants.cs`; each consuming catalogue mirrors from there.
> - **Single-consumer** → consuming catalogue mirrors directly from the source spec's catalogue.

The worked example shows the multi-consumer path (`PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ`). There is no example for the single-consumer path. In practice, the most common `[CROSS]` constants in this project are single-consumer domain tags allocated in Spec #16 §3.4 (e.g., `DOMAIN_TAG_GOALKEEPER`) — used by exactly one spec assembly. A developer writing their first single-consumer mirror has no template to follow.

**Fix:** Add a single-consumer example after the existing multi-consumer example:

```csharp
// Single-consumer mirror: source spec's catalogue directly, NOT via ProjectConstants.cs
/// <summary>
/// [CROSS] Goalkeeper subsystem domain tag.
/// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER.
/// Deterministic Simulation #16 §3.4. Value: 0x1D.
/// </summary>
public static readonly uint DomainTagGoalkeeper =
    DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER;
```

---

## Open Items for Tracking

Resolution priority:

1. **H-1 + M-1** — Both are resolved by the same single edit: rewrite the COMPLIANT
   example to show constructor injection, use `Update` / `s_updateMarker`, and move
   the VIOLATION inside the class as a commented-out method. Fix before any game
   system is authored.
2. **L-3** — Remove the CLAUDE.md citation from the `[CROSS]` XML doc example before
   any `[CROSS]` constant is implemented; the wrong citation precedent propagates at
   first use.
3. **L-5** — Add the single-consumer example before the first domain-tag `[CROSS]`
   mirror is written (Spec #16 domain tags are the most common case).
4. **L-1, L-2, L-4** — Cosmetic / low-impact; resolve in the same fix pass.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-05-22 | — | Adversarial review of `src/CLAUDE.md` v1.4. 1H · 1M · 5L findings. |
