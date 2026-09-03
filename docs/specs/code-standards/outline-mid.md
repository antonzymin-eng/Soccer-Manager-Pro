# Code Standards & Style Guide Specification #20 — Mid-Level Outline

**Purpose:** Bridge between the high-level outline (`outline.md`) and the
forthcoming detailed outline (`outline-detailed.md`). For each section,
names the subsections, the FRs they will hold, the rules they will codify,
and the cross-references they will cite. Does *not* yet contain rule text,
code blocks, or rationale prose — those land in the detailed outline.

**Created:** May 6, 2026, 7:15 PM PST
**Updated:** September 2, 2026
**Version:** 1.5
**Status:** DRAFT — A3.1b post-merge corrections synchronized; normative section files control
**Companion documents:** `outline.md` (high-level), `outline-detailed.md`
(to be drafted after this outline is validated).

---

## SECTION 1 — PURPOSE & SCOPE

### 1.1 What This Specification Covers
- Bullet list scope (C# style, naming, constant tagging at code level,
  allocation discipline, determinism in code, dependency direction,
  architecture integration/activation, documentation conventions, conformance verification model).
- Applicability:
  - **Primary:** every `.cs` file under `src/`, with edge-case carve-outs
    enumerated in §3.9.
  - **Secondary (determinism-only subset):** Python or other-language
    tooling that mirrors, generates, or verifies `[FIXED]` / `[DERIVED]`
    C# constants. Such tooling is bound by §3.4.4's masking rule. No
    other Spec #20 rule applies to non-`.cs` files.

### 1.2 What Is Out of Scope
- Build commands, IDE/editor configuration, CI server choice → `src/CLAUDE.md`.
- Test framework selection → Spec #19 (Testing Strategy).
- Fixed64 numeric library design → Spec #9.
- Project invariants (coordinate system, fatigue convention, tick rates) →
  root `CLAUDE.md` + their owning physics specs.
- UX/asset pipeline conventions → Stage 1+ specs.
- **PR/process rules** (review approval count, branch protection,
  required-reviewers list, merge strategy) → repository settings and
  `src/CLAUDE.md`; Spec #20 governs *code content*, not *process*.
- Concrete `BannedSymbols.txt` / `.editorconfig` files → Stage 1
  deliverables (§7.1).

### 1.3 Key Design Decisions
- **KD-1** — Cite-not-redefine. Spec #20 never restates a CLAUDE.md
  invariant; it cites and binds it to code-level enforcement.
- **KD-2** — Authority Matrix (full table here; preview in `outline.md`).
- **KD-3** — Template-slot reconciliation (§3 holds rules in lieu of
  formulas; §5 holds conformance in lieu of numerical tests; §6 holds
  *code* performance rules in lieu of complexity analysis).
- **KD-4** — Verification evolves with repository state: manual review is
  the baseline, live repository tooling is not optional, and the custom
  Spec #20 analyzer set plus its baselines stay Stage 0+1 deliverables.
- **KD-5** — No numeric lint thresholds at Stage 0 (deferral D1).
- **KD-6** — Single-source-of-truth lists. The banned/required API list
  lives only in Appendix D; §3 sections cite Appendix D entries by
  symbol name. (Drift-prevention rule.)

### 1.4 Dependencies and Integration Contracts
- Upstream (substantive): root `CLAUDE.md`, `development-best-practices.md`, and Project Architecture Governance for FR-CS-074–081; Spec #19 owns executable proof/bounded-substitute/gate mechanics consumed by those rules.
- Upstream (consulted at coding-start; placeholder during spec drafting):
  `certification-platform.md` for Unity LTS revision and C# language
  version pins. Drafting Spec #20 does not require these values to be
  pinned (§3.1.3 references the file by path); first real-code
  implementation does require them and is gated on the open issue
  tracking the platform pin.
- Downstream: every implementation source file, the existing `src/CLAUDE.md`, and architecture-governance registries/tooling.
- No physics/AI domain dependency on Specs #1–#18. Spec #19 is substantive only for proof/gate mechanics; Project Architecture Governance is the upstream decision model.
- Pointer-only future reference to Spec #9 (Fixed64; Stage 5+).

### 1.5 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS & CONFORMANCE MODEL

### 2.1 Conformance Levels
- MUST / SHOULD / MAY (RFC 2119 cited).
- "Exception with sign-off": lead-developer recorded override; tracked
  in PR description; expires at next refactor of the affected file.

### 2.2 Functional Requirement Catalogue (full enumerated list)
- All FR-CS-### live here with their rule statement, conformance level,
  and a §3-or-§6 pointer for the implementation/rationale block.
- Detailed outline will fill in every numbered FR; here we name the
  partition and confirm location:
  - **FR-CS-001 … 015 — C# Style.** Indexed in §2.2; rule mechanics in §3.1.
  - **FR-CS-016 … 025 — Constant Declaration & Tagging.** Indexed in
    §2.2; rule mechanics in §3.2.
  - **FR-CS-026 … 035 — Allocation Discipline.** Indexed in §2.2; rule
    mechanics in §3.3 with cross-list to §6.1–§6.2.
  - **FR-CS-036 … 045 — Determinism.** Indexed in §2.2; rule mechanics
    in §3.4. Banned-API list in Appendix D (single source of truth).
  - **FR-CS-046 … 055 — Dependency Direction & Interfaces.** Indexed in
    §2.2; rule mechanics in §3.5.
  - **FR-CS-056 … 065 — Documentation.** Indexed in §2.2; rule mechanics
    in §3.6; templates in Appendices A & B.
  - **FR-CS-066 … 070 — Code Performance Rules.** Indexed in §2.2; rule
    mechanics in §6 (allocation budgets, hot-path rules, profiler markers).
  - **FR-CS-071 … 073 — Numeric Type Discipline.** Indexed in §2.2; rule
    mechanics in §3.7. Added in v1.3 after detailed-outline review
    flagged §3.7 had no FR coverage.
  - **FR-CS-074 … 081 — Architecture Integration & Activation.** Indexed in §2.2; rule mechanics in §3.5.6–§3.5.7; verification in §5.4.8/§5.5. Added by A3.1a without renumbering existing FRs. Total FR count 81.
- Each FR row: `ID | Statement | Level | Source citation | Verification (§5.x)`.

### 2.3 Failure-to-Comply Modes
- Review block (PR cannot merge).
- Refactor required (merged with follow-up issue).
- Exception with sign-off (recorded; expires at next refactor).
- Tooling violation reporting (Stage 0+1 onward).

### 2.4 Data Structures (informational)
- Spec #20 defines no runtime data structures. Section retained per
  template; one-line "N/A — meta-spec" justification.

### 2.5 Version History

---

## SECTION 3 — TECHNICAL SPECIFICATION (rule mechanics)

> Each subsection cites the FR-CS-### IDs it implements (defined in §2.2)
> and provides the *mechanics* — code shape, exception list, exemplar
> pointer. It does not redefine the rule statement.

### 3.1 C# Style Rules (FR-CS-001 … 015)
- 3.1.1 Naming: PascalCase for types/methods, camelCase for locals/params,
  `_camelCase` for private fields, ALL_CAPS only for `const` declared in
  a constants catalogue with a `[FIXED]` tag.
- 3.1.2 File layout: one public type per file; filename matches type name;
  `using` ordering (System → Unity → project); namespace = folder path
  (modulo §4.3 flat-namespace rule).
- 3.1.3 Language version & feature gating:
  - **Pinned C# language version:** the version shipped by the Unity LTS
    revision pinned in `certification-platform.md` (currently
    placeholder; resolution tracked under the Stage 0 host-platform pin
    open issue).
  - Allowed: records for DTOs only, pattern matching, expression-bodied
    members, `readonly struct`, default-interface-methods (only if
    Unity LTS supports).
  - Discouraged in non-loop code: LINQ-to-objects fluent chains.
  - Banned in game logic: `dynamic`, reflection in hot paths, `unsafe`
    without sign-off, `await`/`async` for game-state work.
- 3.1.4 Whitespace and braces: 4-space indent, Allman braces, `var`
  permitted only when the type is obvious from RHS. (Tabs-vs-spaces is
  decided once here; §7.4 lists this in permanent exclusions to prevent
  relitigation.)
- 3.1.5 Access modifiers: explicit always; no `internal` reliance for
  cross-assembly API surface.

### 3.2 Constant Declaration & Tagging (FR-CS-016 … 025)
- 3.2.1 Citation: tag definitions are owned by root `CLAUDE.md` §
  "Constant Tags". This subsection cites verbatim; does not redefine.
- 3.2.2 Code-level binding rule: every constant in code MUST appear in
  a constants catalogue file with its tag in an XML doc comment
  immediately above the declaration.
- 3.2.3 Tag → C# storage class mapping:
  - `[FIXED]` → `public const` (compile-time literal).
  - `[GT]` → `public static readonly` loaded from tunable config at
    boot.
  - `[EST]` → `public static readonly` with `// TODO: validate`
    comment AND a tracking entry in `spec-error-log.md`.
  - `[DERIVED]` → `public static readonly` with formula comment citing
    source constants.
  - `[CROSS]` → `public static readonly` mirror of source-of-truth
    constant; XML doc comment cites authoritative spec & section.
- 3.2.4 Magic-number ban: any literal numeric value in formula code is a
  conformance violation. Permitted exceptions enumerated:
  - Loop bounds `0` and `1`, `i++`, array-length-of-self comparisons.
  - Fixed-size struct field counts when the struct is sealed and
    declared in the same file.
  - Unit-test fixtures (the assertion's expected value).
  - Bit-pattern literals (e.g., `0xFFFFFFFFFFFFFFFF`) used only in
    determinism scaffolding; flagged with `// §3.4` comment.

### 3.3 Allocation Discipline (FR-CS-026 … 035)
- 3.3.1 Game-loop zero-allocation rule (cites `development-best-practices.md`
  allocation budget).
- 3.3.2 Banned in hot paths (categorical only — full symbol list in
  Appendix D, category "alloc-hot-path"):
  - Boxing.
  - LINQ-to-objects fluent chains.
  - `params` arrays.
  - `string.Format` and string concatenation in per-frame paths.
  - Closures capturing locals.
  - `foreach` over non-`struct` enumerators.
- 3.3.3 Required patterns: ref-passed structs; pre-allocated buffers;
  object pools for rare allocations; struct events; `stackalloc` where
  size-bounded.
- 3.3.4 UI / non-loop layer: <1 MB/frame allocation budget (cite source).
- 3.3.5 Verification: profiler-based at Stage 1; manual review at
  Stage 0 (§5.1).

### 3.4 Determinism in Code (FR-CS-036 … 045)
- 3.4.1 Citation: determinism rules owned by root `CLAUDE.md` §
  "When Writing Code". This subsection binds them to enforceable
  code rules; it does not restate them.
- 3.4.2 Banned APIs in game logic (categorical only — full symbol list in
  Appendix D, category "det-banned"):
  - Non-deterministic RNG (`System.Random`, `RandomNumberGenerator`, …).
  - Wall-clock time (`DateTime.Now`, `DateTime.UtcNow`,
    `Stopwatch.GetTimestamp`, `Environment.TickCount`).
  - Process-unique identifiers (`Guid.NewGuid` in game logic).
  - Multithreaded game-state work (`Task.Run`, `Parallel.*`,
    `AsParallel`).
  - Hardware-intrinsic FMA (without sign-off and platform pin).
- 3.4.3 Required APIs and patterns (categorical only — full symbol list
  in Appendix D, category "det-required"):
  - **APIs:** `SplitMix64` for RNG; `MatchClock` injection for time;
    project math helper (`Mathf` or wrapper) for trigonometry.
  - **Patterns (not APIs):** `unchecked { … }` scope for documented
    overflow-safe multiplication (mirrors the Python tooling masking
    rule; see §3.4.4). Categorized as "pattern" rather than "API"
    because `unchecked` is a C# scope keyword.
- 3.4.4 64-bit multiplication semantics (two distinct rules):
  - **C# game logic:** wrap intermediate 64-bit multiplications used in
    seed/hash chains in `unchecked { … }` to make truncation explicit;
    citation to CLAUDE.md.
  - **Python tooling that mirrors C# constants:** mask intermediates
    with `& 0xFFFFFFFFFFFFFFFF` and omit `UL` suffix (citation).
  - Subsections kept separate so neither rule infects the other's domain.

### 3.5 Dependency Direction & Interface Design (FR-CS-046 … 055)
- 3.5.1 Citation: "interfaces only when both sides are specified" rule
  owned by root `CLAUDE.md`. Spec #20 binds it to file placement.
- 3.5.2 Dependency arrow rules: layer order Physics → Mechanics → AI → UI;
  no upward references; events flow upward via struct dispatch.
- 3.5.3 Interface placement rule: an `interface` file MUST live in the
  same assembly as at least one specified consumer. No "phantom"
  interface folders.
- 3.5.4 Event vs. interface decision tree: when to use struct event
  (cross-spec, low-frequency); when to use direct call (within
  assembly); when to use interface (multiple specified consumers).
- 3.5.5 Anti-patterns enumerated: service locator, ambient context,
  static mutable singletons in game logic, generic dependency-injection
  containers in the game loop.

### 3.5.6 Integration Identity, Ownership & Activation (FR-CS-074–076, 080–081)
- Stable component identity + selector history; canonical selector-v1 bindings.
- Integration-contract ownership/lifecycle/activation declarations.
- Cross-registry string bindings remain non-blocking until A4 resolves them.

### 3.5.7 Closed Runtime Surfaces, Bypasses & Static Initialization (FR-CS-075, 077–081)
- Compiler-backed closed discovery universe, alternate hosts/testhosts, public activation surfaces.
- Bypass/static-initializer coverage; unsupported absence claims remain report-only until A4 fixtures close the universe.

### 3.6 Documentation Conventions (FR-CS-056 … 065)
- 3.6.1 Citation: "creation date and purpose header" + "version history
  entry on every modified file" rules owned by root `CLAUDE.md`.
- 3.6.2 File header template (paste-ready; full template in Appendix A).
  Required fields: file path, created date, last-modified date, author,
  spec-citation list, purpose (≤2 sentences).
- 3.6.3 Version-history block template (full template in Appendix B).
  Where it lives in C# files: trailing `#region` block, never inline
  amid logic.
- 3.6.4 XML doc comments: required on every `public` type and member;
  required on every constant declaration (per §3.2.2).
- 3.6.5 Cross-reference comment style: `// XC-008-001: …`,
  `// FM-003: …`, `// EC-012: …`, `// ERR-016-002: …`. IDs match those
  defined in their owning spec.
- 3.6.6 Inline comments: only when the WHY is non-obvious (cited from
  CLAUDE.md "default to writing no comments" rule).

### 3.7 Numeric Type Discipline
- 3.7.1 Stage 0: `float` everywhere in game logic.
- 3.7.2 `double` in game logic requires lead-developer sign-off and an
  inline citation to the rationale.
- 3.7.3 `decimal` is banned in game logic.
- 3.7.4 Stage 5+ Fixed64 transition: pointer to Spec #9; out of scope
  for Spec #20.

### 3.8 Worked Examples Index
- Pointer to Appendix C (exemplar struct + exemplar constants file
  showing every §3 rule applied).

### 3.9 Edge Cases (rule-application carve-outs)
- 3.9.1 Generated code (e.g., from Unity asset import): excluded from
  this spec; generator config tracked in `src/CLAUDE.md`.
- 3.9.2 Third-party imports: vendored as-is; in-project wrappers MUST
  conform.
- 3.9.3 Editor-only / tooling code (level editors, debug UI): SHOULD
  conform; allocation rules in §3.3 MAY relax for offline tooling
  (with comment citing §3.9.3).
- 3.9.4 Test fixtures (split by test type):
  - **Determinism-harness tests** (those exercising Spec #16 / Spec #19
    determinism contracts): MUST follow §3.4 in full. No relaxation.
  - **General unit tests** (logic, formula, edge-case coverage): MUST
    conform to naming, documentation, and §3.4 banned-API rules; MAY
    relax allocation rules (§3.3) with a comment citing §3.9.4.
  - **Property-based / fuzz tests:** MAY use a non-deterministic seed
    *for seed selection only*, provided the executed test body still
    routes through `SplitMix64` with the recorded seed.
- 3.9.5 Benchmark / micro-perf scaffolds: MAY use `Stopwatch` (one of
  the §3.4.2 banned APIs) but only inside files explicitly marked
  `// benchmark-only` and excluded from game-state assembly graph.

### 3.10 Constants Catalogue (governance metadata only)
- This spec declares **no physical constants**. The "tags" listed are
  governance vocabulary owned by root `CLAUDE.md`. Section retained per
  template with one-line justification. (Note: Spec #16 by contrast has
  a substantive constants catalogue; the comparison is informational
  only and does not establish precedent.)

### 3.11 Version History

---

## SECTION 4 — ARCHITECTURE & INTEGRATION

### 4.1 `src/` Folder Layout (shape, not concrete paths)
- Convention: one folder per Stage 0 spec; folder name matches spec
  folder under `docs/specs/`.
- Within each folder: one assembly (`<spec-name>.csproj` or Unity asmdef),
  one constants catalogue file, struct files, system files; tests in a
  sibling `tests/` folder.
- Canonical ten-tier provider→consumer order is defined in §3.5.2; this outline does not carry a second arrow diagram.

### 4.2 Constant Catalogue File Convention
- File name: `<SpecName>Constants.cs` (e.g., `BallPhysicsConstants.cs`).
- One project-wide root: `ProjectConstants.cs` for cross-spec `[CROSS]`
  source-of-truth values; only published from here.
- Per-tag region ordering inside each catalogue: `[FIXED]` → `[DERIVED]`
  → `[CROSS]` → `[GT]` → `[EST]`.

### 4.3 File / Module Boundary Rules
- Internal vs. public surface: `internal` for assembly-local helpers;
  `public` only for types crossing assembly boundaries.
- No partial classes spanning logical concerns (Unity-generated partials
  excepted).
- One namespace per assembly; nested folders do **not** introduce
  sub-namespaces.
- **Rationale (stated, not deferred):** flat namespaces eliminate
  `using` churn during refactors and align with Unity asmdef granularity
  (one asmdef ↔ one assembly ↔ one namespace). Cross-assembly references
  are then explicit at the asmdef level rather than implicit at the
  namespace level. Trade-off acknowledged: deeper folder trees lose
  namespace-driven discoverability; addressed by §4.1's one-folder-per-spec
  convention.

### 4.4 Interface Contracts (no runtime API)
- Spec #20 publishes no runtime interface. Architecture-governance JSON records are tooling contracts, not gameplay dependencies.

### 4.5 Pointer to `src/CLAUDE.md`
- The existing `src/CLAUDE.md` owns concrete paths/build guidance; Spec #20 owns the convention shape.

### 4.6 Version History

---

## SECTION 5 — CONFORMANCE VERIFICATION

### 5.1 Stage 0 Verification Model
- Manual review against this spec's FRs.
- Reviewer checklist (full text in Appendix; categories named in §5.4)
  attached to every PR.
- The live tree has code and CI tooling; architecture assertions not closed by A4 remain report-only.

### 5.2 Stage 0+1 Transition: Tool Selection
- Roslyn analyzers (`Microsoft.CodeAnalysis.NetAnalyzers` + a custom
  analyzer set for Spec #20-specific rules).
- `.editorconfig` for style enforcement (naming, layout, var usage).
- `dotnet format` for whitespace/braces.
- `BannedSymbols.txt` (sourced from Appendix D, category "det-banned"
  + "alloc-hot-path") for §3.3 / §3.4 banned APIs.
- Unity-specific: Project Settings + custom Unity Analyzer for
  zero-allocation hot-path checks.
- Tool selection is the Stage 0+1 *deliverable*; concrete config files
  are the Stage 1 *deliverable* (§7.1).

### 5.3 Threshold Policy
- No numeric thresholds (cyclomatic complexity, file length, method
  length, allocation count) pinned at Stage 0.
- First values chosen at Stage 1 first-real-code milestone, calibrated
  against actual code, recorded as a §5.3 amendment.
- Until then, judgement calls flagged in review.

### 5.4 Review-Time Checklist (categories)
- Style, constants, allocation, determinism, deps & interfaces, docs,
  performance, architecture integration & activation. Full paste-ready text lives in §5.

### 5.5 FR-to-Verification Traceability
- Table mapping each FR-CS-### to its verification mechanism.
  - Stage 0: most rows resolve to "manual-review category in §5.4" —
    acknowledged as degenerate; the table's value comes at Stage 0+1
    when rows transition to specific analyzer/rule IDs.
  - Stage 1: each row gains an analyzer ID (e.g., `CS-DET-001`), a
    diagnostic severity, and a `BannedSymbols.txt` line where applicable.

### 5.6 Determinism Verification Note
- This spec defines no numerical determinism tests of its own; it
  *requires* code to be testable by Spec #16 (Deterministic Simulation)
  and Spec #19 (Testing Strategy) determinism harnesses.

### 5.7 Version History

---

## SECTION 6 — CODE PERFORMANCE RULES

> **Slot reconciliation:** Replaces the template's "Performance Analysis"
> slot. A meta-spec has no algorithm to analyse; it codifies the
> performance *rules* gameplay code must obey. Justification in §1.3 KD-3.

### 6.1 Allocation Budget Rules
- **Discipline-vs-budget split (clarified in v1.2):** §3.3 governs *how
  to write code that does not allocate* (banned constructs, required
  patterns). §6.1 governs *what allocation rate the resulting code must
  achieve* (the budget the code is measured against). The two are
  complementary, not duplicative; FR-CS-026..035 (discipline) and
  FR-CS-066..070 (budget) are distinct FR rows.
- Budgets:
  - Game loop: zero allocations per frame.
  - UI: < 1 MB allocations per frame.

### 6.2 Hot-Path Rules
- No virtual calls in per-frame inner loops (use `sealed` or static
  dispatch).
- No `try/catch` inside per-frame inner loops (catch at boundaries).
- Avoid `interface`-typed locals in hot paths; prefer concrete struct
  types.

### 6.3 Profiling Hooks
- `ProfilerMarker` required around every system-level Update call.
- Marker naming convention: `<SpecName>.<Method>` (matches §4.1 folder
  shape).

### 6.4 Complexity Targets (qualitative at Stage 0)
- O(1) preferred for per-agent per-frame work.
- O(N) acceptable where N ≤ 22 (one match's agents).
- O(N²) requires sign-off.
- Quantitative thresholds (microseconds per call) deferred to Stage 1.

### 6.5 Performance-Related FR Cross-Listing
- FR-CS-066 … FR-CS-070 are *defined* in §2.2 and have their rule
  mechanics here in §6. (Aligns with the §2.2-defines / §3-or-§6-codifies
  pattern used elsewhere in this spec.)

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1 Deliverables
- Numeric lint threshold values (deferral D1).
- Roslyn analyzer ruleset finalised (§5.2).
- `BannedSymbols.txt` populated from Appendix D.
- `.editorconfig` finalised.
- First `src/CLAUDE.md` drafted.

### 7.2 Stage 1 CI Gates
- Pre-commit: `dotnet format --verify-no-changes`.
- PR gate: analyzer pass at Error-level for Spec #20 rules.
- Merge gate: zero-allocation profiler test on game-loop assemblies.

### 7.3 Stage 5+ Extensions
- Fixed64 enforcement rules activate when Spec #9 ships.
- Cross-platform bit-exact parity rules added.
- `unsafe` / SIMD intrinsic policy revisited.

### 7.4 Permanent Exclusions
- Style debates this spec refuses to relitigate (tabs-vs-spaces:
  spaces, decided in §3.1.4; brace style: Allman, decided in §3.1.4).
- Frameworks/libraries this spec refuses to mandate (specific IoC
  container, specific logging framework).

### 7.5 Deferred Decisions Tracker
- D1 — Numeric lint thresholds.
- D2 — Test framework (owned by Spec #19).
- D3 — Build commands & IDE setup (owned by `src/CLAUDE.md`).
- D4 — Fixed64 enforcement rules (owned by Spec #9).
- D5 — Concrete C# language version pin (gated on
  `certification-platform.md` resolution).

### 7.6 Version History

---

## SECTION 8 — REFERENCES & CITATION AUDIT

### 8.1 Source Register
- Root `CLAUDE.md` (project invariants).
- `docs/planning/development-best-practices.md` (allocation budgets).
- `docs/planning/master-development-plan.md` (Stage definitions).
- `docs/tracking/certification-platform.md` (Unity LTS + C# version
  pin; placeholder at draft time).
- Microsoft C# Coding Conventions (cited URL + retrieval date).
- Unity Scripting API & Performance Best Practices (cited URL + date).
- RFC 2119 (MUST/SHOULD/MAY).
- Roslyn Analyzer rule reference (cited URL + date).

### 8.2 Verification Notes
- Every CLAUDE.md citation in §3 verified against current CLAUDE.md
  text on this spec's drafting date.
- Every external URL retrieved and date-stamped.

### 8.3 Cross-Spec Citation Audit
- Spec #20 is **cited by** every Stage 1+ source file (downstream).
- Spec #20 imports no physics/AI domain rules from Specs #1–#18. Project Architecture
  Governance is substantive upstream authority for FR-CS-074–081, and Spec #19 owns
  executable proof/bounded-substitute/gate mechanics; Spec #9 remains a future pointer.
- No `[CROSS]` constants are imported by this spec (it declares none).

### 8.4 Constant Provenance Summary
- Spec #20 declares no physical constants. Tag vocabulary
  (`[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]/[CROSS-PENDING]`) is governance metadata owned
  by root `CLAUDE.md`. Statement repeated here for auditor clarity.

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST (mid-level shape only)

### 9.1 Content Checklist
- All required sections present (incl. template-slot reconciliation).
- All FR-CS-### present in §2.2 with conformance level tagged.
- Authority Matrix present in §1.3.

### 9.2 Quality Checklist
- Cite-not-redefine rule audited (no CLAUDE.md restatements).
- Every banned API in Appendix D traced to its CLAUDE.md citation.
- Every required pattern in §3.3.3 / §3.4.3 traced to a source.
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- Single-source-of-truth audit: no symbol-level duplication between §3
  and Appendix D.

### 9.3 Review Checklist
- Open issues logged.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated if any cross-spec drift discovered during
  drafting.

### 9.4 Decision
- Status block (`IN REVIEW` / `APPROVED` / `SUSPENDED` / `DEFERRED`).
- Approval evidence: file paths to programmatically-verifiable sources.

---

## APPENDICES (mid-level shape)

- **Appendix A** — File header template (paste-ready C# block; example
  with all required fields populated for a hypothetical
  `BallPhysicsConstants.cs`).
- **Appendix B** — Version-history block template (C# `#region` form;
  date / author / change description columns).
- **Appendix C** — Exemplar pair: `ExemplarStruct.cs` and
  `ExemplarConstants.cs`. Every §3 rule visible with inline pointer
  comments (`// §3.1.1`, `// §3.2.3`, etc.).
- **Appendix D** — Banned & required APIs list (the **single source of
  truth** referenced by §3.3 / §3.4 / §5.2 / §7.1):
  - **Category "det-banned"** (game logic): `System.Random`,
    `RandomNumberGenerator`, `DateTime.Now`, `DateTime.UtcNow`,
    `Stopwatch.GetTimestamp` (game-state assemblies only — see §3.9.5
    benchmark carve-out), `Guid.NewGuid`, `Environment.TickCount`,
    `Task.Run`, `Parallel.For` / `Parallel.ForEach` / `AsParallel`,
    `dynamic`.
  - **Category "alloc-hot-path"** (per-frame paths): LINQ-to-objects,
    `params` arrays, `string.Format`, closures over locals, boxing
    operations, non-struct enumerators, reflection (moved from
    det-banned in v1.2 — reflection is fundamentally a perf concern,
    not a determinism one).
  - **Category "det-required-apis"** (game logic): `SplitMix64`,
    `MatchClock`, project math helper, `ProfilerMarker`.
  - **Category "det-required-patterns"** (game logic): `unchecked`
    scopes for documented overflow-safe multiplication. Listed
    separately because patterns are not analyzer-symbol-bound.
  - Each entry maps to its FR-CS-### and CLAUDE.md citation.
- **Appendix E** — Glossary (only Spec #20-specific terms; physics
  terms cited from owning specs).
- **Appendix F** — Architecture-governance examples for selectors, contracts,
  activation states, closed surfaces and dependency/proof records.

---

## VERSION HISTORY

| Version | Date        | Author      | Notes                                                                                                                                  |
|---------|-------------|-------------|----------------------------------------------------------------------------------------------------------------------------------------|
| 1.0     | May 6, 2026 | Claude Code | Initial mid-level outline drafted from `outline.md` v1.0.                                                                              |
| 1.1     | May 6, 2026 | Claude Code | Self-critique pass 1: FR catalogue moved to §2.2 (template alignment); §3.7 re-housed as Numeric Type; §3.4.4 split into C#/Python rules; Appendix D made single source of truth for banned/required APIs (KD-6); §1.2 excludes PR-process rules; §3.1.3 pins C# language version (gated on certification-platform.md); §4.3 rationale stated; §5.5 transition value note added; §3.10 Spec-#16 comparison corrected; §3.9.5 benchmark carve-out added; §7.5 D5 added. |
| 1.2     | May 6, 2026 | Claude Code | Self-critique pass 2: §1.1 applicability extended to determinism-only Python tooling subset; §1.4 reconciled with §3.1.3 on `certification-platform.md` gating; §3.3-vs-§6.1 disambiguated (discipline-vs-budget); §3.4.3 split into APIs vs patterns; §3.4.5 / §3.7.5 placeholder strikethroughs removed; §3.9.4 split by test type (determinism-harness vs general vs property-based); Appendix D reflection re-categorized to alloc-hot-path; Appendix D split det-required into APIs and patterns; Appendix D `Stopwatch.GetTimestamp` scoped to game-state assemblies. No outstanding self-critique findings. |
| 1.3     | May 6, 2026 | Claude Code | Back-alignment with detailed outline v1.2: §2.2 FR partition extended with FR-CS-071..073 (Numeric Type Discipline) — §3.7 had been substantive but FR-less. Total FR count 70 → 73. No other content changes; mid-level outline remains the section-by-section subsection list. |
| 1.4     | September 2, 2026 | Codex | A3.1b synchronization: current 81-FR partition, §3.5.6–§3.5.7 architecture mechanics, eight §5.4 categories, Governance/#19 authority boundary, existing `src/CLAUDE.md`, and Appendix F. Historical 70→73 record preserved. |
| 1.5     | September 3, 2026 | Claude Code | Post-merge review correction: KD-4 synchronized with the restated normative rule in `section-1.md` v1.2 so all three outline tiers and the section file agree that live repository tooling is not optional. No FR, partition or subsection-list change. |
