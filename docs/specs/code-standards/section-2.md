# Code Standards & Style Guide Specification #20 — Section 2: Functional Requirements & Conformance Model

**File:** `docs/specs/code-standards/section-2.md`
**Purpose:** Defines all 73 numbered functional requirements (FR-CS-001 … FR-CS-073),
conformance levels, failure-to-comply modes, and the data-structures note for Spec #20.
This section is the authoritative FR catalogue; §3 and §6 provide rule mechanics.

**Created:** May 7, 2026
**Modified:** August 18, 2026
**Version:** 1.5
**Status:** APPROVED (May 11, 2026)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 2
**Subsection target lengths:** §2.1 ~20 lines · §2.2 ~250 lines · §2.3 ~30 lines ·
§2.4 ~10 lines

---

## Table of Contents

- [2.1 Conformance Levels](#21-conformance-levels)
- [2.2 Functional Requirement Catalogue](#22-functional-requirement-catalogue)
  - [2.2.1 C# Style — FR-CS-001 … FR-CS-015](#221-c-style--fr-cs-001--fr-cs-015)
  - [2.2.2 Constant Declaration & Tagging — FR-CS-016 … FR-CS-025](#222-constant-declaration--tagging--fr-cs-016--fr-cs-025)
  - [2.2.3 Allocation Discipline — FR-CS-026 … FR-CS-035](#223-allocation-discipline--fr-cs-026--fr-cs-035)
  - [2.2.4 Determinism — FR-CS-036 … FR-CS-045](#224-determinism--fr-cs-036--fr-cs-045)
  - [2.2.5 Dependency Direction & Interfaces — FR-CS-046 … FR-CS-055](#225-dependency-direction--interfaces--fr-cs-046--fr-cs-055)
  - [2.2.6 Documentation — FR-CS-056 … FR-CS-065](#226-documentation--fr-cs-056--fr-cs-065)
  - [2.2.7 Code Performance Rules — FR-CS-066 … FR-CS-070](#227-code-performance-rules--fr-cs-066--fr-cs-070)
  - [2.2.8 Numeric Type Discipline — FR-CS-071 … FR-CS-073](#228-numeric-type-discipline--fr-cs-071--fr-cs-073)
  - [2.2.9 FR Table Footer](#229-fr-table-footer)
- [2.3 Failure-to-Comply Modes](#23-failure-to-comply-modes)
- [2.4 Data Structures](#24-data-structures)
- [2.5 Version History](#25-version-history)

---

## 2.1 Conformance Levels

This specification uses the key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY as
defined in **RFC 2119** ("Key words for use in RFCs to Indicate Requirement Levels",
S. Bradner, March 1997, https://www.rfc-editor.org/rfc/rfc2119).

**MUST / REQUIRED / SHALL** — The definition is an absolute requirement. A file that
violates a MUST-level FR is non-conformant and cannot be merged (see §2.3 Review Block).

**MUST NOT / SHALL NOT** — The definition is an absolute prohibition. A file that
contains a MUST NOT construct is non-conformant and cannot be merged.

**SHOULD / RECOMMENDED** — There may exist valid reasons to ignore this item in a
particular circumstance, but the full implications must be understood and carefully
weighed before choosing a different approach. Deviation must be noted in the PR
description.

**SHOULD NOT / NOT RECOMMENDED** — There may exist valid reasons when the behaviour is
acceptable, but the full implications must be understood before implementation. Deviation
must be noted in the PR description.

**MAY / OPTIONAL** — The item is truly optional.

**Exception with sign-off:** A lead-developer-recorded override that permits temporary
deviation from a MUST or MUST NOT requirement. Format and lifecycle are defined in §2.3.

---

## 2.2 Functional Requirement Catalogue

**Table columns:** `ID | Statement | Level | Source | Mechanics §`

- **ID** — unique identifier; renumbering after publication is forbidden (§2.2.9).
- **Statement** — normative rule text. RFC 2119 keyword in **bold**.
- **Level** — RFC 2119 conformance level.
- **Source** — authoritative document or section from which the rule derives.
- **Mechanics §** — subsection of §3 or §6 where application mechanics are detailed.

---

### 2.2.1 C# Style — FR-CS-001 … FR-CS-015

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-001 | Types and methods **MUST** use PascalCase. | MUST | §3.1.1 | §3.1.1 |
| FR-CS-002 | Local variables and parameters **MUST** use camelCase. | MUST | §3.1.1 | §3.1.1 |
| FR-CS-003 | Private instance fields **MUST** use `_camelCase` (leading underscore, camelCase remainder). | MUST | §3.1.1 | §3.1.1 |
| FR-CS-004 | `ALL_CAPS` naming **MUST** be reserved for `const` declarations in a constants catalogue file tagged `[FIXED]` (per root `CLAUDE.md` — "Constant Tags"). No other context **MAY** use `ALL_CAPS`. | MUST | §3.1.1; root `CLAUDE.md` — "Constant Tags" | §3.1.1 |
| FR-CS-005 | Each file **MUST** contain exactly one public type; the filename **MUST** match the type name (e.g., `BallState.cs` for `public struct BallState`). | MUST | §3.1.2 | §3.1.2 |
| FR-CS-006 | `using` directives **SHOULD** appear in the order: System namespaces → Unity namespaces → project namespaces, each group separated by a blank line. | SHOULD | §3.1.2 | §3.1.2 |
| FR-CS-007 | The namespace declared in a file **MUST** match the folder path from `src/` root, modulo the flat-namespace rule in §4.3 (one namespace per assembly; sub-folders do not introduce sub-namespaces). | MUST | §3.1.2; §4.3 | §3.1.2 |
| FR-CS-008 | Code **MUST** target the C# language version specified by the Unity LTS revision pinned in `docs/tracking/certification-platform.md`. *Deferred activation: this FR is INACTIVE until `certification-platform.md` resolves from placeholder status. Tracked under root `CLAUDE.md` open issue "Stage 0 host platform pin".* | MUST (inactive) | §3.1.3; `docs/tracking/certification-platform.md` | §3.1.3 |
| FR-CS-009 | Allowed language features (records for DTOs only, pattern matching, expression-bodied members, `readonly struct`, default interface methods where Unity LTS supports) **MAY** be used. | MAY | §3.1.3 | §3.1.3 |
| FR-CS-010 | The following language features **MUST NOT** be used in game-logic code: `dynamic`; `async`/`await` for game-state work; `unsafe` blocks without lead-developer sign-off recorded in the PR description. | MUST NOT | §3.1.3 | §3.1.3 |
| FR-CS-011 | Indentation **MUST** use 4 spaces. Tabs **MUST NOT** be used. | MUST | §3.1.4 | §3.1.4 |
| FR-CS-012 | Braces **MUST** follow Allman style (opening brace on its own line). | MUST | §3.1.4 | §3.1.4 |
| FR-CS-013 | `var` **SHOULD** be used only when the type is unambiguously inferrable from the right-hand side of the assignment (e.g., `var state = new BallState()` is clear; `var result = Compute()` is not). | SHOULD | §3.1.4 | §3.1.4 |
| FR-CS-014 | Access modifiers **MUST** be explicit on every type, method, property, and field declaration. No implicit `private` or `internal`. | MUST | §3.1.5 | §3.1.5 |
| FR-CS-015 | `internal` **MUST NOT** be used to expose types across assembly boundaries. Cross-assembly API surface **MUST** be `public`. | MUST NOT | §3.1.5 | §3.1.5 |

---

### 2.2.2 Constant Declaration & Tagging — FR-CS-016 … FR-CS-025

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-016 | Every constant used in formula code **MUST** be declared in a constants catalogue file (§4.2). No constant **MAY** be defined in a formula, system, or struct file. | MUST | §3.2.2; §4.2 | §3.2.2 |
| FR-CS-017 | Every constant declaration **MUST** carry its root `CLAUDE.md` tag (`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, or `[CROSS-PENDING]`) in the XML doc comment immediately preceding the declaration. **`[CROSS-PENDING]` added to this enumeration August 18, 2026 (`ERR-020-006`, round-6 finding H6) — see §3.2.1.** | MUST | §3.2.2; root `CLAUDE.md` — "Constant Tags" | §3.2.2 |
| FR-CS-018 | A constant tagged `[FIXED]` **MUST** be declared `public const` (compile-time literal). | MUST | §3.2.3; root `CLAUDE.md` — "Constant Tags" | §3.2.3 |
| FR-CS-019 | A constant tagged `[GT]` **MUST** be declared `public static readonly` and **MUST** be loaded from a tunable configuration source at boot; it **MUST NOT** be a compile-time literal. | MUST | §3.2.3; root `CLAUDE.md` — "Constant Tags" | §3.2.3 |
| FR-CS-020 | A constant tagged `[EST]` **MUST** be declared `public static readonly`, **MUST** carry a `// TODO: validate` comment on the same or immediately following line, and **MUST** have a corresponding tracking entry in `docs/tracking/spec-error-log.md`. | MUST | §3.2.3; root `CLAUDE.md` — "Constant Tags" | §3.2.3 |
| FR-CS-021 | A constant tagged `[DERIVED]` **MUST** be declared `public static readonly` and **MUST** carry an XML doc comment citing the formula and the source constants from which it is derived. | MUST | §3.2.3; root `CLAUDE.md` — "Constant Tags" | §3.2.3 |
| FR-CS-022 | A constant tagged `[CROSS]` **MUST** be declared `public static readonly` and **MUST** carry an XML doc comment citing the authoritative spec and section where the value is defined. A `[CROSS]` constant **MUST NOT** be assigned a different value from the source or modified by a formula in the declaring file. **Const-mirror carve-out (extends ERR-020-004; August 18, 2026, round-6 H7, filed as `ERR-020-007`):** a `[CROSS]` mirror whose initializer is a compile-time constant expression referencing the owning catalogue's own `public const` (or enum-member) declaration **MAY** instead be declared `public const` — the compiler then enforces value identity on every build, so the divergence risk the `public static readonly` default guards against cannot arise. A literal-initialized mirror never qualifies and keeps the default. Mechanics, naming, and bounds in §3.2.3. | MUST | §3.2.3; root `CLAUDE.md` — "Constant Tags" | §3.2.3 |
| FR-CS-023 | Literal numeric values (magic numbers) **MUST NOT** appear in formula, system, or struct code. Every numeric value **MUST** be referenced through a named constant in a catalogue file. | MUST NOT | §3.2.4 | §3.2.4 |
| FR-CS-024 | The following literal exceptions **MAY** appear without a named constant: loop-control literals `0` and `1` in `for`/`while` bounds and `i++` expressions; array-length-of-self comparisons (`array.Length`); expected-value literals in unit-test assertion statements; bit-pattern literals used exclusively in determinism scaffolding (e.g., `0xFFFFFFFFFFFFFFFF`) annotated with `// §3.4`. | MAY | §3.2.4 | §3.2.4 |
| FR-CS-025 | A constants catalogue file **MUST** be named `<SpecName>Constants.cs` (e.g., `BallPhysicsConstants.cs` for Ball Physics Spec #1). The project-wide root catalogue **MUST** be named `ProjectConstants.cs`. | MUST | §4.2 | §4.2 |

---

### 2.2.3 Allocation Discipline — FR-CS-026 … FR-CS-035

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-026 | Game-loop methods (any method on the 60 Hz physics/render update path) **MUST NOT** allocate managed memory. Zero bytes allocated per frame is the required budget. | MUST NOT | §3.3.1; `docs/planning/development-best-practices.md` | §3.3.1; §6.1 |
| FR-CS-027 | Boxing of value types (struct-to-`object`, struct-to-`interface`) **MUST NOT** occur in hot-path code. See Appendix D category "alloc-hot-path" for the authoritative symbol list. | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-028 | LINQ-to-objects fluent chains (e.g., `.Where(…).Select(…).ToList()`) **MUST NOT** appear in hot-path code. See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-029 | `params` array parameters **MUST NOT** be declared on methods called from hot-path code. See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-030 | `string.Format`, string interpolation (`$"…"`), and string concatenation (`+`) **MUST NOT** be used in per-frame code paths. See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-031 | Closures that capture local variables **MUST NOT** be created in hot-path code. See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-032 | `foreach` **MUST NOT** be used over non-`struct` enumerators in hot-path code (e.g., iterating a `List<T>` via `foreach` boxes the enumerator). See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-033 | Where managed allocation would otherwise be required in a game-loop method, the following patterns **MUST** be used instead: ref-passed structs; pre-allocated fixed-size buffers; object pools for rare-allocation paths; struct-based events. See Appendix D category "det-required-patterns". | MUST | §3.3.3; Appendix D | §3.3.3 |
| FR-CS-034 | Reflection APIs (`System.Reflection`, `Type.GetType`, `Activator.CreateInstance`, and equivalents) **MUST NOT** appear in hot-path code. See Appendix D category "alloc-hot-path". | MUST NOT | §3.3.2; Appendix D | §3.3.2 |
| FR-CS-035 | `stackalloc` **MAY** be used for transient buffers in hot-path code provided the allocation size is statically bounded or guarded by a runtime check before allocation. | MAY | §3.3.3 | §3.3.3 |

---

### 2.2.4 Determinism — FR-CS-036 … FR-CS-045

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-036 | Non-deterministic RNG APIs (`System.Random`, `System.Security.Cryptography.RandomNumberGenerator`, and all equivalent wrappers) **MUST NOT** be used in game-logic code. See Appendix D category "det-banned". | MUST NOT | §3.4.2; root `CLAUDE.md` — "When Writing Code"; Appendix D | §3.4.2 |
| FR-CS-037 | Wall-clock time APIs (`System.DateTime.Now`, `System.DateTime.UtcNow`, `System.Diagnostics.Stopwatch.GetTimestamp`, `System.Environment.TickCount`, and equivalents) **MUST NOT** be used in game-logic code. See Appendix D category "det-banned". | MUST NOT | §3.4.2; root `CLAUDE.md` — "When Writing Code"; Appendix D | §3.4.2 |
| FR-CS-038 | Process-unique-identifier APIs (`System.Guid.NewGuid()` and equivalents) **MUST NOT** be used in game-logic code. See Appendix D category "det-banned". | MUST NOT | §3.4.2; root `CLAUDE.md` — "When Writing Code"; Appendix D | §3.4.2 |
| FR-CS-039 | Multithreaded game-state APIs (`Task.Run`, `Parallel.For`, `Parallel.ForEach`, `.AsParallel()`, and equivalents) **MUST NOT** be used in game-logic code. See Appendix D category "det-banned". | MUST NOT | §3.4.2; root `CLAUDE.md` — "When Writing Code"; Appendix D | §3.4.2 |
| FR-CS-040 | Hardware-intrinsic fused-multiply-add (FMA) **MUST NOT** be used in game-logic code by default. Override requires both: (a) lead-developer sign-off recorded in the PR description, and (b) the target platform pinned in `docs/tracking/certification-platform.md`. See Appendix D category "det-banned". | MUST NOT | §3.4.2; Appendix D | §3.4.2 |
| FR-CS-041 | All random-number generation in game-logic code **MUST** use the project's `SplitMix64` helper (root `CLAUDE.md` — "When Writing Code"). See Appendix D category "det-required-apis". | MUST | §3.4.3; root `CLAUDE.md` — "When Writing Code"; Appendix D | §3.4.3 |
| FR-CS-042 | All time values consumed by game-logic code **MUST** be sourced from the injected `MatchClock` service, not from any wall-clock API. See Appendix D category "det-required-apis". | MUST | §3.4.3; Appendix D | §3.4.3 |
| FR-CS-043 | Trigonometric and other math operations in game-logic code **MUST** use the project-designated math helper (wrapper around `UnityEngine.Mathf` or an approved equivalent). Direct `System.Math` use in game-logic code requires lead-developer sign-off. See Appendix D category "det-required-apis". | MUST | §3.4.3; Appendix D | §3.4.3 |
| FR-CS-044 | Every 64-bit intermediate multiplication in a seed or hash chain in C# game-logic code **MUST** be wrapped in `unchecked { … }` with a one-line comment citing §3.4.4. (Vacuously satisfied in files that contain no such multiplication; no Mode 3 exception is required for absence.) | MUST | §3.4.4; root `CLAUDE.md` — "When Writing Code" | §3.4.4 |
| FR-CS-045 | Every 64-bit intermediate multiplication in Python or other non-C# tooling that mirrors, generates, or verifies `[FIXED]` or `[DERIVED]` C# constants **MUST** be masked with `& 0xFFFFFFFFFFFFFFFF`, and the C# `UL` suffix **MUST** be omitted from numeric literals in that tooling. (Vacuously satisfied in tooling that performs no such mirror; no Mode 3 exception is required for absence.) | MUST | §3.4.4; root `CLAUDE.md` — "When Writing Code" | §3.4.4 |

---

### 2.2.5 Dependency Direction & Interfaces — FR-CS-046 … FR-CS-055

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-046 | Assembly references **MUST** flow in one direction only, along the ten-tier order defined in §3.5.2 (Foundation → Physics → Configuration → Mechanics → AI → Data → Composition → Management → Presentation → Client). A production assembly **MUST NOT** reference an assembly in a higher tier (no upward references permitted). Test assemblies are not members of the order and are outside this rule, as are the two out-of-band **Infrastructure** assemblies named in §3.5.2 (`performance-optimization`, `testing-strategy`), which acquire no tier and are instead bound by FR-CS-046b. | MUST | §3.5.2 | §3.5.2 |
| FR-CS-046a | An assembly **MAY** reference another assembly in the same tier, but the production assembly reference graph as a whole **MUST** remain acyclic. | MUST | §3.5.2 | §3.5.2 |
| FR-CS-046b | An assembly seated in the ten-tier order **MUST NOT** reference an out-of-band **Infrastructure** assembly (`performance-optimization`, `testing-strategy`) at runtime; an Infrastructure assembly **MUST NOT** reference any ordered-tier assembly other than the tier-0 (Foundation) assemblies — it **MAY** reference the other Infrastructure assembly (`testing-strategy` → `performance-optimization` is the standing example), subject to FR-CS-046a's acyclicity. | MUST | §3.5.2 | §3.5.2 |
| FR-CS-047 | Cross-spec events that propagate upward through the tier order **MUST** be dispatched as struct-based events (not class-based delegates or `event Action<T>`). | MUST | §3.5.2 | §3.5.2 |
| FR-CS-048 | An `interface` definition **MUST** reside in the same assembly as at least one specified consumer of that interface. | MUST | §3.5.3; root `CLAUDE.md` — "Interface Design Principle" | §3.5.3 |
| FR-CS-049 | Phantom interface folders (directories containing `interface` definitions whose consumer side is unspecified or unwritten) **MUST NOT** be created. Cites ERR-001 and ERR-004 in `docs/tracking/spec-error-log.md`. | MUST NOT | §3.5.3; root `CLAUDE.md` — "Interface Design Principle" | §3.5.3 |
| FR-CS-050 | The event-vs-interface decision tree (§3.5.4) **MUST** be applied when choosing a cross-boundary communication mechanism, and the chosen mechanism **MUST** be documented in the file header's purpose field. | MUST | §3.5.4 | §3.5.4 |
| FR-CS-051 | The service-locator pattern (a global registry returning service instances by type key) **MUST NOT** be used in game-logic code. | MUST NOT | §3.5.5 | §3.5.5 |
| FR-CS-052 | The ambient-context pattern (a static property returning the "current" context, e.g., `SomeContext.Current`) **MUST NOT** be used in game-logic code. | MUST NOT | §3.5.5 | §3.5.5 |
| FR-CS-053 | Static mutable singletons (a `static` field holding a mutable instance shared across callers) **MUST NOT** be used in game-logic code. | MUST NOT | §3.5.5 | §3.5.5 |
| FR-CS-054 | Generic dependency-injection containers (IoC frameworks such as Zenject, VContainer, or `Microsoft.Extensions.DependencyInjection`) **MUST NOT** be used in the game-loop execution path. | MUST NOT | §3.5.5 | §3.5.5 |
| FR-CS-055 | Cross-assembly references **MUST** be declared explicitly at the `.asmdef` (Unity Assembly Definition) level. Implicit namespace-based cross-assembly coupling **MUST NOT** be relied upon. | MUST | §4.3 | §4.3 |

---

### 2.2.6 Documentation — FR-CS-056 … FR-CS-065

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-056 | Every new `.cs` file **MUST** carry a file header block as specified in §3.6.2 and Appendix A. | MUST | §3.6.2; root `CLAUDE.md` — "When Writing or Editing Specs" | §3.6.2 |
| FR-CS-057 | The file header **MUST** contain all required fields: file path relative to repo root, created date, last-modified date, author, spec-citation list, and a purpose statement of no more than two sentences. | MUST | §3.6.2; root `CLAUDE.md` — "When Writing or Editing Specs" | §3.6.2 |
| FR-CS-058 | The version-history block **MUST** be updated with a new row on every modification to a file. | MUST | §3.6.3; root `CLAUDE.md` — "When Writing or Editing Specs" | §3.6.3 |
| FR-CS-059 | The version-history block **MUST** reside in a trailing `#region VersionHistory … #endregion` at the end of the file, never interspersed with logic. | MUST | §3.6.3 | §3.6.3 |
| FR-CS-060 | Every `public` type and every `public` or `protected` method, property, and event **MUST** carry an XML doc comment (`/// <summary>…</summary>`). | MUST | §3.6.4 | §3.6.4 |
| FR-CS-061 | Every constant declaration **MUST** carry an XML doc comment regardless of access modifier. (FR-CS-060 covers public surface; this FR extends the rule to non-public constants. Public constants are covered by both FRs; the overlap is intentional — a single doc comment satisfies both.) | MUST | §3.6.4 (cross-references §3.2.2) | §3.6.4 |
| FR-CS-062 | Cross-reference comments in code **MUST** use the typed ID format defined in root `CLAUDE.md` — "Cross-Reference System": `// XC-NNN-NNN: …`, `// FM-NNN: …`, `// EC-NNN: …`, `// ERR-NNN-NNN: …`. | MUST | §3.6.5; root `CLAUDE.md` — "Cross-Reference System" | §3.6.5 |
| FR-CS-063 | Cross-reference IDs used in code **MUST** match IDs defined in their owning specification. Fabricated or guessed IDs **MUST NOT** be used. | MUST | §3.6.5; root `CLAUDE.md` — "Cross-Reference System" | §3.6.5 |
| FR-CS-064 | Inline comments **SHOULD** be written only when the WHY is non-obvious — a hidden constraint, a subtle invariant, a workaround for a specific bug, or behaviour that would surprise a reader (root `CLAUDE.md` — "When Writing Code"). | SHOULD | §3.6.6; root `CLAUDE.md` — "When Writing Code" | §3.6.6 |
| FR-CS-065 | Commented-out code **MUST NOT** be present in any commit merged to a shared branch. | MUST NOT | §3.6.6 | §3.6.6 |

---

### 2.2.7 Code Performance Rules — FR-CS-066 … FR-CS-070

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-066 | Game-loop code **MUST** produce zero managed-memory allocations per frame. | MUST | §6.1; `docs/planning/development-best-practices.md` | §6.1 |
| FR-CS-067 | Code in the **Presentation and Client tiers** (§3.5.2 tiers 8 and 9), plus the Unity host code outside the gate, **MUST** produce fewer than 1 MB of managed-memory allocations per frame. | MUST | §6.1; `docs/planning/development-best-practices.md` | §6.1 |
| FR-CS-068 | Virtual method calls **MUST NOT** appear inside per-frame inner loops. Use `sealed` classes or static dispatch instead. | MUST NOT | §6.2 | §6.2 |
| FR-CS-069 | `try`/`catch` blocks **MUST NOT** appear inside per-frame inner loops. Exception handling **MUST** be placed at system or frame boundaries. | MUST NOT | §6.2 | §6.2 |
| FR-CS-070 | Every system-level Update method **MUST** be enclosed in a `ProfilerMarker.Auto()` scope (or `ProfilerMarker.Begin()` / `ProfilerMarker.End()` pair) named `<SpecName>.<MethodName>`. | MUST | §6.3 | §6.3 |

---

### 2.2.8 Numeric Type Discipline — FR-CS-071 … FR-CS-073

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-071 | Game-logic code at Stage 0 **MUST** use `float` for all continuous numeric quantities. | MUST | §3.7.1; root `CLAUDE.md` — "When Writing Code" | §3.7.1 |
| FR-CS-072 | `double` **MUST NOT** be used in game-logic code at Stage 0 by default. Override requires both: (a) lead-developer sign-off recorded in the PR description, and (b) an inline comment at the use site citing the rationale. | MUST NOT | §3.7.2; root `CLAUDE.md` — "When Writing Code" | §3.7.2 |
| FR-CS-073 | `decimal` **MUST NOT** be used in game-logic code at any stage. | MUST NOT | §3.7.3 | §3.7.3 |

---

### 2.2.9 FR Table Footer

This catalogue contains **73 functional requirements** (FR-CS-001 … FR-CS-073) in eight
partitions:

| Partition | FR range | Count |
|---|---|---|
| C# Style | FR-CS-001 … FR-CS-015 | 15 |
| Constant Declaration & Tagging | FR-CS-016 … FR-CS-025 | 10 |
| Allocation Discipline | FR-CS-026 … FR-CS-035 | 10 |
| Determinism | FR-CS-036 … FR-CS-045 | 10 |
| Dependency Direction & Interfaces | FR-CS-046 … FR-CS-055 | 10 (+2 sub-clauses: FR-CS-046a, FR-CS-046b) |
| Documentation | FR-CS-056 … FR-CS-065 | 10 |
| Code Performance Rules | FR-CS-066 … FR-CS-070 | 5 |
| Numeric Type Discipline | FR-CS-071 … FR-CS-073 | 3 |
| **Total** | | **73** |

**Renumbering is forbidden after publication.** FR IDs are referenced in code review
comments, `spec-error-log.md` entries, Stage 1 analyzer rule IDs, and future
`src/CLAUDE.md` entries. Renumbering produces the same cascade-failure class as stale
spec numbers (root `CLAUDE.md` — "KNOWN HAZARD — Spec Renumbering Cascades"). New
requirements append as FR-CS-074, FR-CS-075, … and require a version bump to this
section and to §9.1.

---

## 2.3 Failure-to-Comply Modes

Failure to conform to a MUST or MUST NOT FR is addressed through one of four modes,
chosen by the reviewer based on the nature and severity of the violation.

---

**Mode 1 — Review Block.**

*Invocation criterion:* Any violation of a MUST or MUST NOT FR discovered during PR
review.

*What happens:* The PR cannot be approved or merged until the violation is corrected.
The reviewer cites the FR-CS-### ID in the review comment. The author corrects the
code and re-requests review.

*Record-keeping:* No additional tracking required beyond the standard PR review trail.

---

**Mode 2 — Refactor Required.**

*Invocation criterion:* A MUST or MUST NOT violation is discovered in already-merged
code (post-merge audit, or incidental discovery during adjacent work).

*What happens:* A follow-up issue is filed with a title citing the FR-CS-### ID and
the affected file path. The violation may remain in the codebase until the scheduled
refactor; it must not be replicated in new code in the same area.

*Record-keeping:* The follow-up issue is linked from the discovery PR or commit
description. `docs/tracking/spec-error-log.md` is updated if the violation affects
cross-spec contracts (determinism rules or allocation budgets).

---

**Mode 3 — Exception with sign-off.**

*Invocation criterion:* A MUST or MUST NOT rule cannot be satisfied in a specific
context without disproportionate cost, and the lead developer agrees the exception is
warranted.

*What happens:* The lead developer records the override in the PR description:

```
EXCEPTION [FR-CS-###]: <one-sentence statement of the rule being overridden>
REASON: <one-sentence justification>
EXPIRES: at next refactor of <file path>
```

*Scope:* The exception applies to the specific use site only; it does not create a
general exemption for that FR. The exception expires — and the violation must be
corrected — at the next scheduled refactor of the affected file.

*Record-keeping:* If the override affects a determinism rule (§3.4) or an allocation
rule (§3.3 / §6.1), a corresponding entry **MUST** be added to
`docs/tracking/spec-error-log.md` with status "exception-active" and the expiry
condition noted.

---

**Mode 4 — Tooling violation report.**

*Invocation criterion:* Stage 0+1 onward, when static-analysis tooling (§5.2)
detects a violation automatically.

*What happens:* The analyzer produces a diagnostic at the severity level assigned to
the FR in §5.5. Error-severity diagnostics block the build (equivalent to Mode 1).
Warning-severity diagnostics are logged and must be addressed within the same sprint.

*Record-keeping:* The CI run log captures the diagnostic.
`docs/tracking/spec-error-log.md` is updated for analyzer-reported violations that
remain open across more than one sprint.

---

## 2.4 Data Structures

Spec #20 is a meta-specification. It defines no runtime data structures, simulation
state, or game-object representations. This section is retained per the CLAUDE.md
9-section template to preserve cross-spec section-numbering conventions (KD-3 in §1.3).

For conventions governing *how other specs' data structures must be coded* — struct
declarations, constant catalogue layout, namespace assignments — see §4.

---

## 2.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 2. All 73 FRs authored; FR-CS-008 carries deferred-activation language; FR-CS-040 and FR-CS-072 use MUST NOT + override-condition pattern per outline-detailed.md v1.3 self-critique. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fix (audit finding M-A): recast FR-CS-044 and FR-CS-045 footnotes from "Applies where applicable" to "Vacuously satisfied … no Mode 3 exception required for absence." Same normative content; clearer non-triggering semantics. No rule changes. | — |
| 1.1 | August 17, 2026 | Claude Code | **`ERR-020-002` adopted by owner decision.** FR-CS-046 restated against the §3.5.2 ten-tier order (it named the retired three-layer `Physics → Mechanics → AI → UI` chain, which decided nothing about 21 of the 35 assembly folders — figure re-derived August 17, 2026 by counting the retired box, see the 1.2 row) and its double negative repaired — the published text read “No assembly **MUST NOT** reference…”. Test assemblies stated out of scope explicitly. **FR-CS-046a** added as a sub-numbered clause of FR-CS-046 (intra-tier references permitted, intra-tier cycles not), so the FR-CS-046…055 span and the 73-FR count are unchanged. Header corrected: it read `Version 1.0 / Status DRAFT` against a §2.5 row at 1.0.1 and a SPEC_INDEX status of APPROVED. | — |
| 1.2 | August 17, 2026 | Claude Code | **Adversarial-review finding H4.** The 1.1 row above originally said the retired taxonomy "decided nothing about 16 of the 35 assembly folders"; the true figure is **21** (corrected in place). The retired §3.5.2 box places exactly **14** folders — 8 Physics + 4 Mechanics + 2 AI, with the `UI` row empty — so 35 − 14 = 21 were undecided. The 14/21 figures were **re-derived by counting the retired box** (`git show 0e78d381~1`) rather than rescaled from the earlier 31-assembly error-log count, which is how the wrong 16 arose. No rule change. **⚠️ ANNOTATED (v1.3, August 18, 2026): the closing "No rule change" claim is FALSE of the commit this row versions** — the same commit ALSO added **FR-CS-046b** (a new MUST NOT), restated **FR-CS-046** with the Infrastructure exclusion, rescoped **FR-CS-067** from "UI-layer code" to the Presentation/Client tiers, and changed §2.2.9's Count row; none of those appeared in any version-history row until the 1.3 row below enumerated them. Left in place per the annotate-don't-rewrite convention. | — |
| 1.3 | August 18, 2026 | Claude Code | **Adversarial-review findings H9 + H10 (reviewed round).** H10: the 1.2 row's version history is completed — the v1.2 commit made **four normative changes it never recorded**: (1) **FR-CS-046b added** (Infrastructure assemblies bound: ordered tiers may not reference them at runtime, and they may not reference above Foundation); (2) **FR-CS-046 restated** to exclude the two out-of-band Infrastructure assemblies from the tier order and delegate them to FR-CS-046b; (3) **FR-CS-067 rescoped** from "UI-layer code" to the Presentation and Client tiers (§3.5.2 tiers 8 and 9) plus Unity host code; (4) **§2.2.9's Count row changed** ("10" → "10 (+2 sub-clauses: FR-CS-046a, FR-CS-046b)"). The 1.2 row's "No rule change" is annotated in place as false. H9: **FR-CS-046b clause 2 reworded** — "MUST NOT reference any assembly above tier 0" was ambiguous against the live tree (`testing-strategy` → `performance-optimization` exists, and Infrastructure assemblies acquire no tier, so "above tier 0" had two readings); now states exactly what is permitted — only tier-0 (Foundation) assemblies and the other Infrastructure assembly, acyclic per FR-CS-046a — resolving the ambiguity in favour of what the tree does, and `tools/assembly-tier-check.py` now enforces both clauses (it previously skipped every Infrastructure-sourced reference unchecked). Also: FR-CS-047 "layer order" → "tier order", standardising on the §3.5.2 vocabulary (the term "layer order" is no longer defined there). | — |
| 1.4 | August 18, 2026 | Claude Code | **Adversarial-review round-6 findings H6 + H7.** H6: FR-CS-017's tag enumeration was a CLOSED five-tag list against root `CLAUDE.md`'s six — `[CROSS-PENDING]` (root table row verified August 18, 2026: `grep -n 'CROSS-PENDING' CLAUDE.md` → line 128) was missing, so every one of the 218 `[CROSS-PENDING]` occurrences under `docs/specs/` (`grep -rn 'CROSS-PENDING' docs/specs/ | wc -l`, August 18, 2026) was formally a MUST-level violation of this APPROVED spec, while `.github/workflows/ci.yml`'s tag lint accepted the tag throughout; the enumeration now lists all six. FR count deliberately unchanged at 73 — no new FR row, an existing row's enumeration corrected. H7: FR-CS-022 gains the const-mirror carve-out (extends ERR-020-004): the §4.2 carve-out cited `DisciplineConstants.CardKindYellow` — a `public const byte` — as a compliant mirror while this row's MUST required `public static readonly`, i.e. the spec certified as compliant a declaration its own MUST forbade; the compile-time-constant symbol-referencing mirror shape is tree-wide (19 such declarations across `discipline`, `match-engine`, `match-analytics`, `player-progression`) and compiler-enforced, so the rule is extended rather than the tree condemned — with the literal-initialized bound stated so the six literal-initialized `[CROSS]` `const` declarations (five carrying a `TODO: mirror from ProjectConstants` note — `goalkeeper-mechanics` ×2, `heading-mechanics` ×2, `perception-system` ×1 — plus `DisciplineConstants.LeagueCompetitionKey`, whose doc justifies its literal VALUE but not its storage class) remain non-conformant, reported for a code-owning pass. Full statement in §3.2.3. **⚠️ ANNOTATED (v1.5, August 18, 2026, round-7 finding M6): the "218 … `grep -rn 'CROSS-PENDING' docs/specs/ \| wc -l`" clause is a command offered as live proof of a figure it no longer reproduces** — every citation of the tag added since (including this annotation) keeps raising the count; that command returns 245 as of the v1.5 fix, not 218. 218 is the count immediately BEFORE this row's own commit (`git grep -c 'CROSS-PENDING' 9b841d1^ -- docs/specs \| awk -F: '{s+=$NF} END {print s}'` → 218) and is left standing as landing-time history. The H6/H7 finding IDs this row fixed are `ERR-020-006`/`ERR-020-007`; they are now cited at their fix sites (§3.2.1, §3.2.3, §4.2) by the v1.5 pass. Left in place per the annotate-don't-rewrite convention. | — |
| 1.5 | August 18, 2026 | Claude Code | **Adversarial-review round-7 findings M1 + M6.** M1: `ERR-020-006` and `ERR-020-007` were cited nowhere in the spec they patch (only in `spec-error-log.md`) — the v1.4 row's "round-6 H6/H7" text is now also cross-cited by ERR id at its fix sites: FR-CS-017's row (§2.2.2) and FR-CS-022's row (§2.2.2), plus §3.2.1/§3.2.3/§4.2 in the sibling section files. M6: the v1.4 row's "218 … `grep -rn 'CROSS-PENDING' docs/specs/ \| wc -l`" clause offered a command as live proof of a figure it no longer returns (245 today, since every citation of the tag — including the v1.4 row's own text — keeps raising the count); annotated in place per the annotate-don't-rewrite convention rather than rewritten, with 218 re-derived against the pre-fix commit (`git grep -c 'CROSS-PENDING' 9b841d1^ -- docs/specs`) instead. No FR text, count, or conformance level changed by this row. | — |

---

*End of Section 2 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
