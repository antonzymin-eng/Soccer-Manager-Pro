# Code Standards & Style Guide Specification #20 — Section 4: Architecture & Integration

**File:** `docs/specs/code-standards/section-4.md`
**Purpose:** Defines the `src/` folder-layout shape, constant catalogue file convention,
file and module boundary rules, and the handoff boundary between Spec #20 (conventions)
and `src/CLAUDE.md` (concrete paths). Spec #20 does not publish a runtime interface;
§4.4 records the N/A justification.

**Created:** May 7, 2026
**Last Updated:** September 2, 2026
**Version:** 1.4
**Status:** AMENDMENT DRAFT (A3.1b; approved v1.3 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 4
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.35, §6; A3.1b
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
    └── (this spec produces no source files — no src/code-standards/ folder exists
        in the live tree, and none ever will; the leaf appears here only to
        complete the one-folder-per-spec map)
```

### Dependency graph shape

Assembly references follow the ten-tier order established in §3.5.2, and **§3.5.2's
table is the single rendering of the dependency graph** — this section deliberately
carries no second drawing. (A retired ASCII rendering here mixed `◄──` and `──►`
arrows with no label — the exact ambiguity `ERR-020-003` was filed against, and which
both §3.5.2 and `src/CLAUDE.md` were amended to remove — labelled
`pass-mechanics`/`shot-mechanics`/`first-touch` a "Mechanics layer" where §3.5.2 seats
all three in tier 1 Physics, and asserted three edges the `.asmdef` reference graph
contains in NEITHER direction: `agent-movement`↔`ball-physics` (`agent-movement`
references only `project-constants`), `pass-mechanics`↔`shot-mechanics`, and
`shot-mechanics`↔`first-touch` — re-verified against `src/*/[a-z]*.asmdef` on
August 18, 2026 and deleted at v1.1, round-6 finding H3.) The rule the drawing existed
to illustrate is stated by FR-CS-046 and FR-CS-046a: an assembly **MUST NOT** reference
any assembly seated in a higher tier — the upward-reference ban, across all ten tiers,
Foundation through Client — and intra-tier references are permitted provided the
intra-tier graph stays acyclic. Because the reference graph is encoded in `.asmdef`
files, `tools/assembly-tier-check.py` (wired into `.github/workflows/ci.yml`) verifies
every reference against the §3.5.2 table on every push to `main` and every pull request
targeting `main`. Note what the compiler does and does not do here: Unity rejects a
reference **cycle** outright, but a non-cyclic *upward* reference compiles cleanly, so
the tier order is enforced by that check and by review — not by the build. §3.5.2 says
the same thing from the other side: adopting the order "changed nothing that compiles".

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

**Storage-class note (round-6 finding H7, filed as `ERR-020-007`, August 18, 2026).** The mirrors this
carve-out describes are declared `public const` (e.g.
`public const byte CardKindYellow = EventSystemConstants.CARD_KIND_YELLOW;`), which
the base FR-CS-022 / §3.2.3 rule — `[CROSS]` → `public static readonly`, PascalCase —
did not permit: as first written, this paragraph certified as "compliant" a declaration
its own spec's MUST forbade. Resolved by the **const-mirror carve-out** in §3.2.3
(carried by FR-CS-022 since section-2.md v1.4): a `[CROSS]` mirror whose initializer
references the owning catalogue's own compile-time constant MAY itself be
`public const`, because the compiler enforces value identity on every build and the
divergence risk the `static readonly` default guards against cannot arise;
literal-initialized mirrors never qualify and stay on the default. `CardKindYellow`
above is now a conforming example of that carve-out, not an exception to FR-CS-022.

### Per-Tag Region Ordering

Constants within a catalogue file are grouped in `#region` blocks in the following
order (most-immutable to most-mutable):

```
1. #region Fixed        — [FIXED]         → public const; ALL_CAPS
2. #region Derived      — [DERIVED]       → public static readonly; PascalCase
3. #region Cross        — [CROSS]         → public static readonly; PascalCase
                                            (const-mirror carve-out: §3.2.3)
4. #region CrossPending — [CROSS-PENDING] → public static readonly; PascalCase
                                            (transitional; promotes into #region Cross)
5. #region GT           — [GT]            → public static readonly; PascalCase
6. #region EST          — [EST]           → public static readonly; PascalCase + TODO
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

The `.asmdef` reference graph is the machine-readable fact source for §3.5.2's tier
order. An acyclic upward reference can compile successfully, so compiler success is not
proof of FR-CS-046 conformance. `tools/assembly-tier-check.py` parses the authoritative
§3.5.2 table and the production `.asmdef` graph and blocks upward references,
out-of-band Infrastructure violations, unseated folders and cycles at the CI merge
boundary. Unity/compiler cycle rejection is an additional check for cycles only.

---

## 4.4 Interface Contracts

Spec #20 publishes **no runtime interface** — no `interface` type, `abstract class`,
event-bus entry, or public gameplay struct. The architecture records introduced by
FR-CS-074–081 (`integration-contracts.json`, runtime-surface classifications,
dependency/proof records) are governance/tooling data, not runtime dependencies and are
never referenced by gameplay assemblies.

Their relationship is one-way: compiler/assembly discovery emits facts; the canonical
Governance registries bind durable identity, ownership, lifecycle and activation intent;
Spec #19 proof/gate machinery evaluates applicable obligations. Until A4 closes the
cross-registry resolver and discovery blind spots, declarations that require that
resolution remain report-only as §3.5.6–§3.5.7 require.

For runtime interface design rules that all other specs follow when they publish
interfaces, see §3.5.

---

## 4.5 Pointer to `src/CLAUDE.md`

Spec #20 declares the **shape** of the `src/` layout and the **conventions** for
constant catalogues and namespaces. It does not declare concrete paths, Unity project
configuration, or assembly GUIDs — those are implementation details that depend on the
Unity LTS version pinned in `docs/tracking/certification-platform.md` and on the
directory structure chosen at Stage 1 project setup.

`src/CLAUDE.md` is the live document that holds concrete information:

| What | Owner |
|---|---|
| Exact `src/` subdirectory paths and assembly names | `src/CLAUDE.md` |
| `.asmdef` GUIDs and Unity project folder structure | `src/CLAUDE.md` |
| Build commands (`dotnet build`, `dotnet test`, Unity batch-mode) | `src/CLAUDE.md` |
| IDE/editor configuration (`.editorconfig` path, VS solution setup) | `src/CLAUDE.md` |
| Constant catalogue concrete file paths | `src/CLAUDE.md` (names follow Spec #20 convention; paths depend on project structure) |

The original creation gate for `src/CLAUDE.md` is historical and has been satisfied;
the file now exists. It remains the concrete-path/build-command guide and cites Spec #20
for conventions. Architecture-governance registries stay under
`docs/tracking/architecture-governance/` and do not move into `src/CLAUDE.md`.

---

## 4.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 4. | — |
| 1.0.1 | May 22, 2026 | — | ERR-020-001: §4.2 `[CROSS]` mirror example field name corrected `PHYSICS_TICK_HZ` (ALL_CAPS) → `PhysicsTickHz` (PascalCase) per §3.2.3 authoritative rule; XML doc updated to include spec+section citation and value per FR-CS-022. | — |
| 1.0.2 | August 15, 2026 | Claude Code | ERR-020-004 (reviewed-findings pass, M4/owner decision 2): §4.2's `[CROSS]` routing rule stated only a two-way split (multi-consumer → `ProjectConstants.cs`; single-consumer → local) with no accommodation for a constant that has ≥ 2 consumers but a single owning spec's catalogue to mirror from. New "Owning-catalogue carve-out" paragraph: such a constant mirrors from its owning spec's catalogue directly regardless of consumer count (the `CardIssuedEvent.CardKind` / `EventSystemConstants` example, mirrored by three downstream catalogues with none routed through `ProjectConstants.cs`). Found because `src/discipline/DisciplineConstants.cs`'s compliant mirror had invented a false "single consumer" justification for want of a rule that fit its actual shape. `src/CLAUDE.md`'s `[CROSS]` mirrors section gains the identical carve-out. | — |
| 1.0.3 | August 15, 2026, later | Claude Code | ERR-020-005 (extends ERR-020-004; reviewed-findings pass): the "`ProjectConstants.cs` — Cross-Spec Source of Truth" subsection's opening sentence still stated the pre-carve-out two-way split unqualified, 25 lines below the carve-out paragraph that had already narrowed it — the heading a reader looks under for the routing rule, restating the rule the carve-out exists to correct. Qualified in place: the primary declaration for a singly-owned constant is that spec's own catalogue (the carve-out), and `ProjectConstants.cs` is primary only for a constant with no single owning catalogue — the worked example immediately below (`PHYSICS_TICK_HZ`) is exactly that case, so the example is now internally consistent with the rule text above it. | — |
| 1.0.4 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.1 | August 18, 2026 | Claude Code | **Adversarial-review round-6 findings H3 + H5 + H6 + H7.** H3: §4.1's "Dependency graph shape" ASCII block DELETED — a retired rendering with three edges the `.asmdef` graph contains in neither direction (`agent-movement`↔`ball-physics`, `pass-mechanics`↔`shot-mechanics`, `shot-mechanics`↔`first-touch`; re-verified August 18, 2026 by reading `src/*/[a-z]*.asmdef` references), a "(Mechanics layer)" label on three assemblies §3.5.2 seats in tier 1 Physics, unlabelled mixed `◄──`/`──►` arrows (the `ERR-020-003` ambiguity), and prose that under-scoped the ban to "Mechanics-, AI-, or UI-layer" — six tiers short. §3.5.2 is named the single rendering; the prose restated as the FR-CS-046 upward-reference ban + FR-CS-046a intra-tier permission, with `tools/assembly-tier-check.py` (CI-wired) named as the mechanical check. H5: the tree's `code-standards/` leaf no longer claims "empty at Stage 0" — no `src/code-standards/` folder exists at all (verified: `ls -d src/code-standards` fails), and the leaf now says so. H6: the §4.2 per-tag `#region` ordering gains the `[CROSS-PENDING]` slot (position 4, directly after the `Cross` region it promotes into — a promotion is a one-region move), extending the list 5 → 6. H7: the ERR-020-004 carve-out gains the Storage-class note — it had cited `DisciplineConstants.CardKindYellow` (a `public const byte`) as "a compliant mirror" while FR-CS-022/§3.2.3 required `public static readonly`; the §3.2.3 const-mirror carve-out resolves the contradiction in the spec and the paragraph now cites it. | — |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-7 findings H2 + H3.** H2: §4.1 asserted "an illegal dependency is a build error, not just a review finding" — false, and contradicting §3.5.2's "adopting the order changed nothing that compiles", written the same day. Unity rejects a reference CYCLE, but a non-cyclic UPWARD reference compiles cleanly — which is exactly why `ERR-020-002` drifted for fourteen months and why `tools/assembly-tier-check.py` had to be written and CI-wired. The clause is deleted, the real enforcement stated (the tool, plus Unity's cycle rejection), and §3.5.2 cross-referenced so the two now agree. H3: "on every push" corrected to `ci.yml`'s real triggers — `branches: [main]` on both `push` and `pull_request`, so a push to a topic branch runs nothing. §3.5.2 had corrected the identical phrase about the identical tool the previous day. | — |
| 1.3 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding M1.** `ERR-020-007` was cited nowhere in the spec it patches — the Storage-class note said only "round-6 finding H7". Now cites the id directly. | — |
| 1.4 | September 2, 2026 | Codex | **A3.1b supporting-surface synchronization.** Corrects the live `.asmdef` claim: acyclic upward references can compile and are blocked by `tools/assembly-tier-check.py`, not by ordinary assembly resolution. §4.4 distinguishes Governance records from runtime interfaces and preserves the no-runtime-dependency boundary; §4.5 is synchronized to the existing `src/CLAUDE.md`. | PENDING — A3.4 |

---

*End of Section 4 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
