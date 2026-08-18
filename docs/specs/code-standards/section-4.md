# Code Standards & Style Guide Specification #20 — Section 4: Architecture & Integration

**File:** `docs/specs/code-standards/section-4.md`
**Purpose:** Defines the `src/` folder-layout shape, constant catalogue file convention,
file and module boundary rules, and the handoff boundary between Spec #20 (conventions)
and `src/CLAUDE.md` (concrete paths). Spec #20 does not publish a runtime interface;
§4.4 records the N/A justification.

**Created:** May 7, 2026
**Last Updated:** August 15, 2026, later
**Version:** 1.0.4
**Status:** APPROVED (May 11, 2026)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 4
**Subsection target lengths:** §4.1 ~50 lines · §4.2 ~40 lines · §4.3 ~70 lines ·
§4.4 ~5 lines · §4.5 ~15 lines

---

## Table of Contents

- [4.1 `src/` Folder Layout](#41-src-folder-layout)
- [4.2 Constant Catalogue File Convention](#42-constant-catalogue-file-convention)
- [4.3 File and Module Boundary Rules](#43-file-and-module-boundary-rules)
- [4.4 Interface Contracts](#44-interface-contracts)
- [4.5 Pointer to `src/CLAUDE.md`](#45-pointer-to-srcclaude-md)
- [4.6 Version History](#46-version-history)

---

## 4.1 `src/` Folder Layout

*Implements:* FR-CS-005 (one public type per file), FR-CS-007 (namespace = folder
path), FR-CS-025 (`<SpecName>Constants.cs`), FR-CS-055 (cross-assembly references
explicit at `.asmdef` level).

Spec #20 declares the **shape** of the `src/` tree. Concrete assembly paths, exact
`.asmdef` GUIDs, and Unity project configuration are deferred to `src/CLAUDE.md`
(§4.5), which is created when Stage 1 coding begins.

### Convention: one folder per Stage 0 specification

Each Stage 0 spec gets exactly one folder under `src/`. The folder name matches the
spec's folder name under `docs/specs/` (canonical names in `docs/specs/SPEC_INDEX.md`).
Within each spec folder: one Unity Assembly Definition (`.asmdef`), one constants
catalogue file, one or more struct/system files, and a sibling `tests/` folder for
unit tests.

```
src/
├── project-constants/
│   └── ProjectConstants.cs          ← cross-spec [CROSS] source-of-truth (§4.2)
│
├── ball-physics/                     ← Spec #1; folder name matches docs/specs/ball-physics/
│   ├── ball-physics.asmdef           ← one assembly per spec
│   ├── BallPhysicsConstants.cs       ← constants catalogue (FR-CS-025)
│   ├── BallState.cs                  ← one public type per file (FR-CS-005)
│   ├── BallStateSystem.cs
│   └── tests/
│       └── BallPhysicsTests.cs
│
├── agent-movement/                   ← Spec #2
│   ├── agent-movement.asmdef
│   ├── AgentMovementConstants.cs
│   ├── AgentState.cs
│   ├── AgentMovementSystem.cs
│   └── tests/
│
├── collision-system/                 ← Spec #3
│   └── …
│
│   ⋮  (one folder per approved spec, in dependency order)
│
├── decision-tree/                    ← Spec #8
│   └── …
│
└── code-standards/                   ← Spec #20 (no runtime code; governance only)
    └── (empty at Stage 0 — this spec produces no source files)
```

### Dependency graph shape

Assembly references follow the layer order established in §3.5.2. The `src/` folder
tree reflects this: a Physics-layer folder's `.asmdef` references only other
Physics-layer `.asmdef` files or lower; it never references a Mechanics-, AI-, or
UI-layer `.asmdef`. This makes illegal dependencies a build error, not just a review
finding.

```
project-constants  ◄── (referenced by all assemblies read-only)
       ▲
  ball-physics  ◄──── agent-movement  ◄──── collision-system  ◄──── …
       ▲
  pass-mechanics ──► shot-mechanics ──► first-touch ──► …     (Mechanics layer)
       ▲
  decision-tree ──► perception-system ──► …                   (AI layer)
```

---

## 4.2 Constant Catalogue File Convention

*Implements:* FR-CS-016 (constants in catalogue), FR-CS-025 (naming), FR-CS-017–022
(tag → storage class). Supplements §3.2.3.

### Naming

Each spec's constants catalogue is named `<SpecName>Constants.cs`, where `<SpecName>`
is the PascalCase form of the spec's folder name:

| Spec folder | Catalogue file |
|---|---|
| `ball-physics/` | `BallPhysicsConstants.cs` |
| `agent-movement/` | `AgentMovementConstants.cs` |
| `collision-system/` | `CollisionSystemConstants.cs` |
| `pass-mechanics/` | `PassMechanicsConstants.cs` |
| `decision-tree/` | `DecisionTreeConstants.cs` |
| *(all 20 specs)* | `<SpecName>Constants.cs` |

The project-wide root catalogue, `ProjectConstants.cs`, lives in the
`project-constants/` folder. It is the sole source of truth for `[CROSS]` constants
that are shared across more than one spec assembly **and have no single owning
catalogue** — see the carve-out below. A constant that appears in only one spec's
catalogue is **not** promoted to `ProjectConstants.cs`.

**Owning-catalogue carve-out (ERR-020-004, added August 15, 2026).** A constant whose
owning spec already has its own constant catalogue mirrors from THAT catalogue
directly, regardless of how many other specs consume it — `ProjectConstants.cs`
routing is for constants with no single owning catalogue (a project-wide physical
value like `PHYSICS_TICK_HZ`, which no one spec owns outright). Example: Event
System #17 owns the `CardIssuedEvent.CardKind` domain-ordinal encoding (Appendix A:
"#17 default owner") and declares it once in `EventSystemConstants.cs`; three
downstream catalogues — `MatchEngineConstants`, `DisciplineConstants`, and
`MatchAnalyticsConstants` — each mirror it directly from `EventSystemConstants`, none
routed through `ProjectConstants.cs`, even though the encoding plainly has more than
one consumer. This is not a violation of the multi-consumer rule above; it is the case
that rule's two-way split (declared here vs. shared bucket) never named. Routing a
spec-owned encoding through `ProjectConstants.cs` would add a hop without adding an
authority — the owning spec's catalogue already IS the authority. (ERR-020-004 found
this gap because a compliant mirror — `DisciplineConstants.CardKindYellow` — had to
justify itself with a false "single consumer" claim for want of a rule that fit the
actual shape.)

### Per-Tag Region Ordering

Constants within a catalogue file are grouped in `#region` blocks in the following
order (most-immutable to most-mutable):

```
1. #region Fixed       — [FIXED]   → public const; ALL_CAPS
2. #region Derived     — [DERIVED] → public static readonly; PascalCase
3. #region Cross       — [CROSS]   → public static readonly; PascalCase
4. #region GT          — [GT]      → public static readonly; PascalCase
5. #region EST         — [EST]     → public static readonly; PascalCase + TODO
```

**Rationale:** Physical constants (`[FIXED]`) never change; estimated placeholders
(`[EST]`) are the most likely to be revised. Placing them in this order means a reader
scanning the file encounters stable, high-confidence values first and deferred/uncertain
values last. It also aligns with the storage-class ordering: `const` (lowest runtime
overhead) → `static readonly` (one-time init).

A spec that has no constants in a given tag category simply omits that region. An empty
`#region Fixed #endregion` block with no constants is prohibited.

### `ProjectConstants.cs` — Cross-Spec Source of Truth

`[CROSS]` constants declared in individual spec catalogues are **mirrors** — they copy
the value from the primary declaration and must not diverge. **Subject to the
owning-catalogue carve-out above** (ERR-020-004): for a constant with a single owning
spec, the primary declaration is that spec's own catalogue, mirrored directly regardless
of consumer count. `ProjectConstants.cs` is the primary declaration only for the
remaining case — a constant with no single owning catalogue, shared across specs by
convention rather than ownership (`PHYSICS_TICK_HZ` below is exactly that case: no one
spec owns the tick rate). ERR-020-005 (reviewed-findings pass, 2026-08-15) qualifies this
sentence in place: as written it restated the pre-carve-out two-way split directly under
the heading a reader looks to for the routing rule, 25 lines below the carve-out
paragraph that already narrowed it — under the unqualified sentence, the three
`CardKind*` mirrors the carve-out exists to legitimise all read as non-compliant. The
mirroring catalogue file's `[CROSS]` entry cites the source:

```csharp
// In BallPhysicsConstants.cs (mirror)
/// <summary>
/// [CROSS] Physics tick rate (Hz). PascalCase per §3.2.3 (ERR-020-001).
/// Authoritative source: ProjectConstants.cs — PHYSICS_TICK_HZ.
/// Ball Physics #1 §1.2 / Root CLAUDE.md "Heartbeat Tick Rate". Value: 60 Hz.
/// </summary>
public static readonly float PhysicsTickHz = ProjectConstants.PHYSICS_TICK_HZ;

// In ProjectConstants.cs (source of truth)
/// <summary>
/// [FIXED] Physics/render loop tick rate (Hz).
/// Root CLAUDE.md — "Heartbeat Tick Rate": 60 Hz.
/// </summary>
public const float PHYSICS_TICK_HZ = 60.0f;
```

---

## 4.3 File and Module Boundary Rules

*Implements:* FR-CS-005 (one type per file), FR-CS-007 (namespace = folder), FR-CS-014
(explicit access modifiers), FR-CS-015 (`internal` boundary), FR-CS-055 (cross-assembly
via `.asmdef`).

### `internal` vs `public` Access Surface

`internal` is the correct modifier for types whose callers are all within the same
assembly — helper structs, internal state machines, sub-system utilities that no other
spec assembly needs to reference. `public` is reserved for types that cross assembly
boundaries (FR-CS-015).

```csharp
// COMPLIANT — helper type used only within ball-physics assembly
internal readonly struct DragIntegrator { … }

// COMPLIANT — BallState must be read by pass-mechanics and shot-mechanics assemblies
public readonly struct BallState { … }
```

A type that starts `internal` and later needs to cross an assembly boundary is promoted
to `public` with a version bump to the catalogue file and a new `.asmdef` reference
declaration. The promotion is an explicit, reviewable change, not an implicit one.

### No Partial Classes Spanning Logical Concerns

Partial classes are permitted only when both parts are Unity-generated (e.g.,
MonoBehaviour inspector scaffolding). Hand-authored partial classes that split a type's
logic across two or more files are prohibited. If a type is large enough to feel like it
needs splitting, it is a signal to refactor into smaller composed types, not to use
`partial`.

### Flat-Namespace Rule

One namespace per assembly. Sub-folders within the same assembly do **not** introduce
sub-namespaces (FR-CS-007, §3.1.2).

A file in `src/ball-physics/simulation/` declares
`namespace TacticalDirector.BallPhysics`, not
`namespace TacticalDirector.BallPhysics.Simulation`. The sub-folder exists for file
organisation only.

**Rationale** (from `outline-mid.md` v1.2 §4.3, carried verbatim): Flat namespaces
eliminate `using` churn during refactors and align with Unity `.asmdef` granularity
(one `.asmdef` ↔ one assembly ↔ one namespace). Cross-assembly references are then
explicit at the `.asmdef` level rather than implicit at the namespace level. The
trade-off is that deeper folder trees lose namespace-driven discoverability; this is
addressed by the one-folder-per-spec convention in §4.1, which keeps each assembly's
root folder shallow.

```csharp
// COMPLIANT — file at src/ball-physics/simulation/DragIntegrator.cs
namespace TacticalDirector.BallPhysics   // flat; no sub-namespace
{
    internal readonly struct DragIntegrator { … }
}

// VIOLATION — sub-folder introducing a sub-namespace
namespace TacticalDirector.BallPhysics.Simulation  // prohibited
{
    internal readonly struct DragIntegrator { … }
}
```

### Cross-Assembly References via `.asmdef`

Every inter-assembly dependency **MUST** be declared explicitly in the producer
assembly's `.asmdef` file (FR-CS-055). Relying on the C# compiler to resolve a
reference because both assemblies happen to be in the same Unity project — without an
explicit `.asmdef` reference — is prohibited.

The `.asmdef` reference graph is the machine-readable equivalent of the §3.5.2 layer
diagram. If the `.asmdef` references do not match the intended dependency direction,
the build fails with a CS0246 / Unity assembly resolution error. This turns an invisible
dependency violation into an auditable compile error.

---

## 4.4 Interface Contracts

Spec #20 is a governance meta-specification. It publishes **no runtime interface** —
no `interface` type, no `abstract class`, no event bus entry, no public struct that
crosses an assembly boundary at runtime.

This section is retained per the CLAUDE.md 9-section template (KD-3 in §1.3). For the
runtime interface design rules that all other specs must follow when they do publish
interfaces, see §3.5.

---

## 4.5 Pointer to `src/CLAUDE.md`

Spec #20 declares the **shape** of the `src/` layout and the **conventions** for
constant catalogues and namespaces. It does not declare concrete paths, Unity project
configuration, or assembly GUIDs — those are implementation details that depend on the
Unity LTS version pinned in `docs/tracking/certification-platform.md` and on the
directory structure chosen at Stage 1 project setup.

`src/CLAUDE.md` is the document that will hold concrete information:

| What | Owner |
|---|---|
| Exact `src/` subdirectory paths and assembly names | `src/CLAUDE.md` |
| `.asmdef` GUIDs and Unity project folder structure | `src/CLAUDE.md` |
| Build commands (`dotnet build`, `dotnet test`, Unity batch-mode) | `src/CLAUDE.md` |
| IDE/editor configuration (`.editorconfig` path, VS solution setup) | `src/CLAUDE.md` |
| Constant catalogue concrete file paths | `src/CLAUDE.md` (names follow Spec #20 convention; paths depend on project structure) |

`src/CLAUDE.md` **MUST NOT** be created until all 20 Stage 0 specs are approved and
Stage 1 coding begins (root `CLAUDE.md` — "Deferred: `src/CLAUDE.md`"). At that point,
the author of `src/CLAUDE.md` cites Spec #20 as the source for every convention it
concretises, establishing the Spec #20 ↔ `src/CLAUDE.md` cite-chain.

---

## 4.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 4. | — |
| 1.0.1 | May 22, 2026 | — | ERR-020-001: §4.2 `[CROSS]` mirror example field name corrected `PHYSICS_TICK_HZ` (ALL_CAPS) → `PhysicsTickHz` (PascalCase) per §3.2.3 authoritative rule; XML doc updated to include spec+section citation and value per FR-CS-022. | — |
| 1.0.2 | August 15, 2026 | Claude Code | ERR-020-004 (reviewed-findings pass, M4/owner decision 2): §4.2's `[CROSS]` routing rule stated only a two-way split (multi-consumer → `ProjectConstants.cs`; single-consumer → local) with no accommodation for a constant that has ≥ 2 consumers but a single owning spec's catalogue to mirror from. New "Owning-catalogue carve-out" paragraph: such a constant mirrors from its owning spec's catalogue directly regardless of consumer count (the `CardIssuedEvent.CardKind` / `EventSystemConstants` example, mirrored by three downstream catalogues with none routed through `ProjectConstants.cs`). Found because `src/discipline/DisciplineConstants.cs`'s compliant mirror had invented a false "single consumer" justification for want of a rule that fit its actual shape. `src/CLAUDE.md`'s `[CROSS]` mirrors section gains the identical carve-out. | — |
| 1.0.3 | August 15, 2026, later | Claude Code | ERR-020-005 (extends ERR-020-004; reviewed-findings pass): the "`ProjectConstants.cs` — Cross-Spec Source of Truth" subsection's opening sentence still stated the pre-carve-out two-way split unqualified, 25 lines below the carve-out paragraph that had already narrowed it — the heading a reader looks under for the routing rule, restating the rule the carve-out exists to correct. Qualified in place: the primary declaration for a singly-owned constant is that spec's own catalogue (the carve-out), and `ProjectConstants.cs` is primary only for a constant with no single owning catalogue — the worked example immediately below (`PHYSICS_TICK_HZ`) is exactly that case, so the example is now internally consistent with the rule text above it. | — |
| 1.0.4 | August 17, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. | — |

---

*End of Section 4 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
