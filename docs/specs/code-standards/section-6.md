# Code Standards & Style Guide Specification #20 — Section 6: Code Performance Rules

**File:** `docs/specs/code-standards/section-6.md`
**Purpose:** Defines allocation budgets, hot-path prohibitions, profiling-hook requirements, and
complexity targets that code written under Spec #20 must satisfy. §3.3 defines *how* to
write zero-allocation code; §6 defines *what rate* the resulting code is measured against.
**Created:** May 8, 2026
**Version:** 1.2
**Status:** APPROVED (May 11, 2026)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 6; `outline-mid.md` v1.2, §6.1–§6.5

> **Slot-reconciliation note (KD-3):** The nine-section CLAUDE.md spec template names slot 6
> "Performance Analysis." For Spec #20 — a governance meta-spec that produces no runtime
> interface — this slot is renamed **"Code Performance Rules."** The rules here govern how
> *other specs' code* must perform; they are not an analysis of Spec #20's own runtime cost.
> The name change is recorded in §1.3 KD-3.

---

## Table of Contents

- [6.1 Allocation Budget Rules](#61-allocation-budget-rules)
- [6.2 Hot-Path Rules](#62-hot-path-rules)
- [6.3 Profiling Hooks](#63-profiling-hooks)
- [6.4 Complexity Targets](#64-complexity-targets)
- [6.5 Performance-Related FR Cross-Listing](#65-performance-related-fr-cross-listing)
- [6.6 Version History](#66-version-history)

---

## 6.1 Allocation Budget Rules

*Implements:* FR-CS-066 (game-loop budget), FR-CS-067 (Presentation/Client-tier budget).

### Discipline-vs-Budget Split

> **Verbatim from `outline-mid.md` v1.2 §6.1 (carried with attribution):**
> §3.3 governs *how to write code that does not allocate* (banned constructs, required
> patterns). §6.1 governs *what allocation rate the resulting code must achieve* (the budget
> the code is measured against). The two are complementary, not duplicative; FR-CS-026..035
> (discipline) and FR-CS-066..067 (budget) are distinct FR rows.

§3.3 is the implementation guide. §6.1 is the acceptance criterion. Passing §3.3's rules
(no `new` in hot paths, no LINQ, etc.) is necessary but not sufficient — the resulting
assembly must also pass a profiler measurement against the budgets below before it clears
Stage 1 review.

### Budgets

| Scope | Allocation limit | Enforcement point | FR |
|---|---|---|---|
| Game loop (60 Hz physics path) | **0 bytes / frame** | Unity Profiler / managed-heap snapshot | FR-CS-066 |
| Presentation and Client tiers (§3.5.2 tiers 8–9), plus Unity host code outside the gate | **< 1 MB / frame** | Unity Profiler allocation tracker | FR-CS-067 |

**Game-loop budget rationale:** The 60 Hz physics path is the most time-critical path in the
engine. Any managed allocation on this path risks GC pauses during match simulation.
Zero-allocation is the only target that eliminates GC jitter entirely; any non-zero budget
would require case-by-case negotiation and GC tuning.

**Presentation/Client budget rationale:** Presentation- and Client-tier code — screens,
HUD, overlays, render projections; per §3.5.2 that is tier 8 (`match-viewer`,
`match-analytics`) and tier 9 (`match-client-core`, `ui-framework`, `client-app`,
`match-client-unity`, `match-client-web`), plus the Unity host code the gate cannot
compile — does not run on every physics frame and tolerates GC pauses that are
invisible to the user at inter-frame intervals. The 1 MB/frame ceiling is a
conservative limit drawn from `docs/planning/development-best-practices.md`; it prevents
runaway presentation-side allocations from interfering with the game-loop heap.

**Stage 0 status:** Both budgets are *normative rules* at Stage 0. Profiler measurement is a
Stage 1 enforcement artifact. Stage 0 review verifies the budget numbers are declared and
cited; it does not run a profiler.

---

## 6.2 Hot-Path Rules

*Implements:* FR-CS-068 (no virtual calls in per-frame inner loops), FR-CS-069 (no
`try/catch` in per-frame inner loops).

"Per-frame inner loop" means any code that executes once or more per physics frame (60 Hz)
on every active game entity. The rules in this section apply to all game-loop and
tier-1 Physics assemblies. They do not apply to editor tooling, test fixtures, or
Presentation/Client-tier code (which carries the FR-CS-067 budget instead, §6.1).

### FR-CS-068 — Virtual Dispatch Prohibition

Virtual method calls (`virtual` / `override`) in per-frame inner loops are **prohibited**.
Virtual dispatch requires an indirect function call through the vtable; under tight loops
over 22 agents at 60 Hz, vtable indirections accumulate into measurable overhead and prevent
inlining.

**Compliant patterns:**

- Declare hot-path types `sealed` so the JIT can devirtualise and inline.
- Use `static` methods for stateless operations that do not need polymorphism.
- Replace polymorphic dispatch with data-driven parameter structs supplied by the Decision
  Tree (see CLAUDE.md "Parameter-Based Physics — No Type Enums").

### FR-CS-069 — try/catch Prohibition in Inner Loops

`try/catch` blocks inside per-frame inner loops are **prohibited**. Exception handling in
C# suppresses certain JIT optimisations (register allocation, loop hoisting) even when no
exception is thrown. Exception handling MUST be pushed to call-site boundaries outside the
per-frame loop.

### Interface-Typed Locals in Hot Paths

Storing a value in a variable of interface type (e.g., `IFoo foo = new FooStruct()`) causes
the value-type to be boxed. Boxing allocates on the managed heap — a direct violation of
FR-CS-066. Interface-typed locals in hot paths SHOULD be replaced with concrete struct types
or generic constrained type parameters.

### Code Example — Anti-Pattern and Compliant Refactor

```csharp
// VIOLATION — virtual dispatch + try/catch in inner loop (FR-CS-068, FR-CS-069)
for (int i = 0; i < agents.Length; i++)
{
    try
    {
        agents[i].UpdateMovement(dt);   // virtual call; agents[i] may be a subclass
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);         // exception handling inside the loop
    }
}
```

```csharp
// COMPLIANT — sealed struct, static dispatch, exception boundary outside loop
// FR-CS-068: AgentMovementSystem is sealed; UpdateAll is static → no vtable.
// FR-CS-069: try/catch moved to caller, not inside the per-agent loop.
try
{
    AgentMovementSystem.UpdateAll(agentStates, dt);   // one call; all agents inside
}
catch (Exception ex)
{
    Debug.LogException(ex);   // boundary catch at system level, not inside loop
}

// AgentMovementSystem.cs (src/agent-movement/)
public sealed class AgentMovementSystem
{
    private static readonly ProfilerMarker s_marker =
        new ProfilerMarker("AgentMovement.UpdateAll");

    public static void UpdateAll(NativeArray<AgentState> states, float dt)
    {
        using var _ = s_marker.Auto();
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = Integrate(states[i], dt);   // static call; inlineable
        }
    }

    private static AgentState Integrate(AgentState s, float dt) { /* … */ }
}
```

---

## 6.3 Profiling Hooks

*Implements:* FR-CS-070 (ProfilerMarker required around every system-level Update method).

### Requirement

Every system-level `Update`, `FixedUpdate`, `LateUpdate`, `OnUpdate`, or equivalent
entry-point method in a game-loop assembly **MUST** be wrapped in a `ProfilerMarker.Auto()`
scope. "System-level" means a method called once per frame by the engine or by the
per-frame scheduler — not internal helpers called by that method.

### Naming Convention

Markers are named `<SpecName>.<MethodName>`, where `<SpecName>` is the PascalCase form of
the spec folder name (matching the §4.2 catalogue naming convention):

| Spec folder | Example marker name |
|---|---|
| `ball-physics/` | `"BallPhysics.FixedUpdate"` |
| `agent-movement/` | `"AgentMovement.UpdateAll"` |
| `collision-system/` | `"CollisionSystem.FixedUpdate"` |
| `pass-mechanics/` | `"PassMechanics.Update"` |

This naming scheme makes each spec's contribution identifiable in the Unity Profiler timeline
without ambiguity.

### Code Example

```csharp
// COMPLIANT — ProfilerMarker declared as static readonly field (one allocation at startup)
// and consumed via Auto() scope (zero allocation per frame).
public sealed class BallPhysicsSystem
{
    private static readonly ProfilerMarker s_fixedUpdateMarker =
        new ProfilerMarker("BallPhysics.FixedUpdate");

    public void FixedUpdate(ref BallState state, float dt)
    {
        using var _ = s_fixedUpdateMarker.Auto();
        // … physics integration …
    }
}
```

`ProfilerMarker` is a Unity value type; declaring it `static readonly` confines the
allocation to startup. The `Auto()` call inside `FixedUpdate` is zero-allocation. The
`using var _` pattern ends the marker scope at the method's closing brace without requiring
an explicit `End()` call.

---

## 6.4 Complexity Targets

*Supports:* FR-CS-066 (game-loop budget) contextually; no dedicated FR (complexity is a
design constraint, not a lint-checkable rule).

Stage 0 complexity rules are **qualitative**. Quantitative thresholds (wall-clock
microseconds per agent per call) are deferred to Stage 1 when profiler baselines exist.

### Per-Agent Per-Frame Work

| Complexity class | Status | Condition |
|---|---|---|
| O(1) | Preferred | All per-agent per-frame operations SHOULD be O(1) in agent count. |
| O(N) | Acceptable | Permitted where N ≤ 22 (one match's agent roster). Cite N bound in code comment. |
| O(N²) | Requires sign-off | Prohibited without explicit written sign-off from project lead citing the performance budget justification. |
| O(N log N) | Case-by-case | Treated as O(N) for N ≤ 22; escalate if N may grow past 22. |

**N = 22 bound source:** One association football match has 22 players on the pitch
(11 per side × 2 sides = 22, including the two goalkeepers — there are no separate
"outfield + GK" totals; the keepers are part of the 11). Physics systems that iterate
over all agents operate over at most 22 entities simultaneously. Any algorithm whose
worst-case grows faster than O(N log N) for N = 22 must be justified in a sign-off
comment citing this bound. Substitutes are not on the pitch and are not counted in N.

### Spatial Queries

Spatial operations (broad-phase collision, perception radius checks) that are naively O(N²)
MUST be reduced to O(N) or better using spatial partitioning (e.g., uniform grid, BVH).
The specific data structure is left to the implementing spec; the O(N) target is the
constraint.

### Quantitative Thresholds (Deferred)

Microsecond budgets per system are **not set at Stage 0**. They depend on the host platform
pinned in `docs/tracking/certification-platform.md`, which remains `_TBD_` as of May 2026.
Stage 1 profiler baselines establish the concrete per-system microsecond ceilings; these
ceilings become FR-level requirements in `src/CLAUDE.md` once measured.

---

## 6.5 Performance-Related FR Cross-Listing

FR-CS-066 through FR-CS-070 are **defined** in §2.2.7 (the normative FR table) and have
their rule mechanics **codified** here in §6. This follows the same §2.2-defines /
§N-codifies pattern used throughout Spec #20: §2.2 is the single authoritative FR registry;
the numbered sections (§3, §4, §5, §6) expand on each FR's meaning, provide worked examples,
and supply the implementation detail that the FR row itself cannot fit.

| FR | §2.2.7 statement (summary) | Codified in |
|---|---|---|
| FR-CS-066 | Game-loop allocation budget = 0 bytes/frame | §6.1 |
| FR-CS-067 | Presentation/Client-tier allocation budget < 1 MB/frame | §6.1 |
| FR-CS-068 | No virtual calls in per-frame inner loops | §6.2 |
| FR-CS-069 | No `try/catch` inside per-frame inner loops | §6.2 |
| FR-CS-070 | All system-level Update methods wrapped in `ProfilerMarker.Auto()` | §6.3 |

---

## 6.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 8, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 6 and `outline-mid.md` v1.2 §6.1–§6.5. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fix (audit finding L-A): §6.4 "N = 22 bound source" prose corrected — original read "22 outfield players + 2 goalkeepers" (24 total), but association football is 11 per side × 2 sides = 22 with the keepers included. Wording rewritten to make this explicit; substitutes off-pitch noted as excluded from N. No change to the N = 22 numeric bound. | — |
| 1.0.2 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.1 | August 18, 2026 | Claude Code | **Adversarial-review round-6 finding H4.** §6.1 was the one section still scoping FR-CS-067 to the retired "UI layer (menus, HUD, overlays)" after section-2.md v1.2 rescoped the FR to the Presentation and Client tiers (§3.5.2 tiers 8–9) plus Unity host code outside the gate — the FR's Mechanics-§ column routes readers HERE, so the stale wording read `match-viewer`, `match-analytics`, `match-client-core`, `ui-framework`, `client-app`, `match-client-unity` and `match-client-web` out of the budget. Fixed: the §6.1 budget-table row and its "Layer" column header (→ "Tier", per the v1.3 vocabulary standardisation), the budget rationale paragraph (now enumerating the tier-8/9 assemblies from §3.5.2's table), the §6.5 summary row, the §6.1 *Implements* line, and §6.2's scope sentence ("do not apply to … UI code" → Presentation/Client-tier code, which carries the FR-CS-067 budget instead). No budget value changed — 0 bytes/frame and < 1 MB/frame stand as approved. | — |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-7 findings L1 + L2.** L1: the v1.1 header rename "Layer" → "Tier" was itself wrong — row 1 of the budget table ("Game loop (60 Hz physics path)") is not a §3.5.2 tier, only row 2 is; header restated as "Scope", which covers both a tier and a non-tier row without asserting either is a tier. L2: §6.2's scope sentence still read "game-loop and physics-layer assemblies" — retired three-layer vocabulary the v1.1 pass was supposed to have cleared; restated as "tier-1 Physics assemblies" per §3.5.2. No budget value changed. | — |

---

*End of Section 6 — Code Standards & Style Guide Specification #20*
*System XI — Specification #20 of 20 | Stage 0: Physics Foundation*
