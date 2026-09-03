# Code Standards & Style Guide Specification #20 — Detailed Outline

**Purpose:** Expansion of `outline-mid.md` v1.2 into a section-by-section
draft plan. For every subsection: the rule statement(s) it will publish,
the exemplar/citation it will carry, and the cross-references it will
emit. Detailed enough that section files (`section-1.md` … `section-9-…md`,
`appendices.md`) can be drafted directly from this document, with no
further outlining work required.

This document does **not** publish the FR text in normative form — that
text lands in `section-2.md`. Detailed outline records every FR's
intended rule, conformance level, and source so the FR table can be
authored mechanically.

**Created:** May 6, 2026, 8:50 PM PST
**Updated:** September 2, 2026
**Version:** 1.6
**Status:** DRAFT — A3.1b post-merge corrections synchronized; normative section files control
**Companion documents:** `outline.md` (high-level v1.2),
`outline-mid.md` (mid-level v1.4).

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~40 lines.

**Content:**
- Opening sentence: declarative scope statement.
- Bullet list of governance areas (8 items): C# style, naming, constant
  tagging at code level, allocation discipline, determinism in code,
  dependency direction, documentation conventions, conformance
  verification model.
- **Applicability block (verbatim from `outline-mid.md` §1.1, refined):**
  - Primary scope: every `.cs` file under `src/`.
  - Secondary scope: non-`.cs` tooling that mirrors/generates/verifies
    `[FIXED]`/`[DERIVED]` C# constants — bound only by §3.4.4.
  - Out-of-scope reference: §1.2 (not duplicated here).
- Closing sentence: pointer to §3.9 carve-outs.

### 1.2 What Is Out of Scope

**Subsection target length:** ~30 lines.

**Content (one-line entries with the owning document for each):**
- Build commands, IDE/editor configuration, CI server choice →
  `src/CLAUDE.md`.
- Test framework selection → Spec #19.
- Fixed64 numeric library design → Spec #9.
- Project invariants (coordinates, fatigue, tick rates) → root `CLAUDE.md`
  + owning physics specs.
- UX/asset pipeline conventions → Stage 1+ specs.
- PR-process rules (review approval count, branch protection,
  required-reviewers, merge strategy) → repo settings + `src/CLAUDE.md`.
- Concrete `BannedSymbols.txt` / `.editorconfig` files → Stage 1
  deliverables (§7.1).
- Non-game-state tooling (build scripts, content authoring) — except
  the determinism-only subset called out in §1.1.

### 1.3 Key Design Decisions

**Subsection target length:** ~80 lines.

Six numbered decisions, each with: statement (1 sentence), rationale
(2–3 sentences), consequence-if-violated (1 sentence).

- **KD-1 — Cite-not-redefine.** Spec #20 cites every CLAUDE.md invariant
  it depends on; never restates. Rationale: prevents two-sources-of-truth
  drift documented in `OPEN ISSUES → Stale spec numbers`.
  Consequence-if-violated: silent drift between `CLAUDE.md` and Spec #20
  on (e.g.) constant tag definitions; bug class previously seen in
  Pass Mechanics ERR-class fixes.
- **KD-2 — Authority Matrix.** Three-way partition (root `CLAUDE.md`,
  Spec #20, existing `src/CLAUDE.md`) over the rule space. Rationale:
  every rule must have exactly one owner; the matrix names the owner.
  Full table reproduced here (~12 rows from `outline.md`).
- **KD-3 — Template-slot reconciliation.** §3 holds rules in lieu of
  formulas; §5 holds conformance in lieu of numerical tests; §6 holds
  *code* performance rules in lieu of complexity analysis. Rationale:
  preserves cross-spec section-number conventions while accommodating
  meta-spec content.
- **KD-4 — Stage 0 verification = manual review.** Tooling deferred to
  Stage 0+1 transition. Rationale: no source code exists at Stage 0;
  empirical baselines impossible.
- **KD-5 — No numeric lint thresholds at Stage 0.** All thresholds
  (cyclomatic complexity, file length, method length, allocation count)
  deferred to first real code (D1). Rationale: pre-code thresholds are
  guesses.
- **KD-6 — Single-source-of-truth lists.** Banned/required APIs live in
  Appendix D only; §3 sections cite Appendix D entries. Rationale:
  drift-prevention; symbol-level lists are notoriously prone to
  divergence between rule prose and reference table.

### 1.4 Dependencies and Integration Contracts

**Subsection target length:** ~25 lines.

**Content:**
- Upstream (substantive): root `CLAUDE.md`, `development-best-practices.md`, Project Architecture Governance for FR-CS-074–081, and Spec #19 for executable proof/bounded-substitute/gate mechanics consumed by those rules.
- Upstream (consulted at coding-start): `certification-platform.md`
  (placeholder during original spec drafting; current pins are consumed where applicable).
- Downstream: every Stage 1+ source file and existing `src/CLAUDE.md`.
- Pointer-only reference: Spec #9 (Fixed64).
- One-paragraph note: A3 reapproval and later A4/A8 evidence/enforcement activation are separate gates; Spec #20 owns the code/integration rules while Governance and Spec #19 retain their upstream decision/proof authority.

### 1.5 Version History

Standard table (5 columns: Version | Date | Author | Notes | Reviewer).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS & CONFORMANCE MODEL (`section-2.md`)

### 2.1 Conformance Levels

**Subsection target length:** ~20 lines.

- RFC 2119 citation (URL + retrieval date).
- Verbatim definitions of MUST / MUST NOT / SHOULD / SHOULD NOT / MAY.
- "Exception with sign-off" definition: lead-developer recorded override
  in PR description; expires at next refactor of the affected file;
  tracking entry required in `spec-error-log.md` only if the override
  affects determinism (§3.4) or allocation (§3.3) rules.

### 2.2 Functional Requirement Catalogue

**Subsection target length:** ~250 lines (the bulk of §2).

**Format:** master FR table with columns:
`ID | Statement | Level | Source | Mechanics §`

**Partition (numbering synchronized to A3.1a — 81 FRs total):**

#### 2.2.1 C# Style — FR-CS-001 … FR-CS-015 (15 FRs)
- FR-CS-001: PascalCase for types and methods. *MUST*. Source: §3.1.1.
- FR-CS-002: camelCase for locals and parameters. *MUST*. Source: §3.1.1.
- FR-CS-003: `_camelCase` for private fields. *MUST*. Source: §3.1.1.
- FR-CS-004: `ALL_CAPS` reserved for `const` in `[FIXED]` catalogue
  context. *MUST*. Source: §3.1.1.
- FR-CS-005: One public type per file; filename matches type. *MUST*.
  Source: §3.1.2.
- FR-CS-006: `using` ordering: System → Unity → project. *SHOULD*.
  Source: §3.1.2.
- FR-CS-007: Namespace = folder path (modulo §4.3 flat-namespace rule).
  *MUST*. Source: §3.1.2.
- FR-CS-008: Code MUST target the C# language version specified in the
  Unity LTS revision pinned in `certification-platform.md`. *MUST*
  (deferred activation: this FR is INACTIVE until
  `certification-platform.md` resolves from placeholder; tracked under
  the Stage 0 host-platform-pin open issue). Source: §3.1.3.
- FR-CS-009: Allowed language features list. *MAY*. Source: §3.1.3.
- FR-CS-010: Banned language features in game logic (`dynamic`,
  `async`/`await` for game-state, `unsafe` without sign-off). *MUST NOT*.
  Source: §3.1.3.
- FR-CS-011: 4-space indentation. *MUST*. Source: §3.1.4.
- FR-CS-012: Allman braces. *MUST*. Source: §3.1.4.
- FR-CS-013: `var` permitted only when type is obvious from RHS.
  *SHOULD*. Source: §3.1.4.
- FR-CS-014: Explicit access modifiers always. *MUST*. Source: §3.1.5.
- FR-CS-015: No `internal` reliance for cross-assembly API surface.
  *MUST NOT*. Source: §3.1.5.

#### 2.2.2 Constant Declaration & Tagging — FR-CS-016 … FR-CS-025 (10 FRs)
- FR-CS-016: Every constant in code MUST appear in a constants
  catalogue file. *MUST*. Source: §3.2.2.
- FR-CS-017: Every constant declaration carries its CLAUDE.md tag in
  the immediately preceding XML doc comment. *MUST*. Source: §3.2.2.
- FR-CS-018: `[FIXED]` → `public const`. *MUST*. Source: §3.2.3.
- FR-CS-019: `[GT]` → `public static readonly` from tunable config.
  *MUST*. Source: §3.2.3.
- FR-CS-020: `[EST]` → `public static readonly` + `// TODO: validate`
  + `spec-error-log.md` entry. *MUST*. Source: §3.2.3.
- FR-CS-021: `[DERIVED]` → `public static readonly` with formula doc
  comment. *MUST*. Source: §3.2.3.
- FR-CS-022: `[CROSS]` → `public static readonly` mirror with
  authoritative-spec citation in doc comment. *MUST*. Source: §3.2.3.
- FR-CS-023: No magic numbers in formula code. *MUST NOT*. Source: §3.2.4.
- FR-CS-024: Permitted literal exceptions enumerated. *MAY*. Source: §3.2.4.
- FR-CS-025: Catalogue file name = `<SpecName>Constants.cs`. *MUST*.
  Source: §4.2.

#### 2.2.3 Allocation Discipline — FR-CS-026 … FR-CS-035 (10 FRs)
- FR-CS-026: No allocations in game-loop methods. *MUST NOT*. Source: §3.3.1.
- FR-CS-027: No boxing in hot paths. *MUST NOT*. Source: §3.3.2 (alloc-hot-path).
- FR-CS-028: No LINQ-to-objects in hot paths. *MUST NOT*. Source: §3.3.2.
- FR-CS-029: No `params` arrays in hot paths. *MUST NOT*. Source: §3.3.2.
- FR-CS-030: No `string.Format` / string concatenation in per-frame
  paths. *MUST NOT*. Source: §3.3.2.
- FR-CS-031: No closures capturing locals in hot paths. *MUST NOT*.
  Source: §3.3.2.
- FR-CS-032: No `foreach` over non-`struct` enumerators in hot paths.
  *MUST NOT*. Source: §3.3.2.
- FR-CS-033: Required patterns — ref-passed structs, pre-allocated
  buffers, object pools, struct events. *MUST* (when allocation would
  otherwise occur). Source: §3.3.3.
- FR-CS-034: No reflection in hot paths. *MUST NOT*. Source: §3.3.2
  (alloc-hot-path; recategorized from determinism in mid-level v1.2).
- FR-CS-035: `stackalloc` permitted for size-bounded transient buffers.
  *MAY*. Source: §3.3.3.

#### 2.2.4 Determinism — FR-CS-036 … FR-CS-045 (10 FRs)
- FR-CS-036: Banned RNG APIs in game logic (`System.Random`,
  `RandomNumberGenerator`). *MUST NOT*. Source: §3.4.2 (det-banned).
- FR-CS-037: Banned wall-clock APIs in game logic. *MUST NOT*.
  Source: §3.4.2.
- FR-CS-038: Banned process-unique-id APIs in game logic. *MUST NOT*.
  Source: §3.4.2.
- FR-CS-039: Banned multithreaded game-state APIs. *MUST NOT*.
  Source: §3.4.2.
- FR-CS-040: Hardware-intrinsic FMA in game logic is banned by default.
  *MUST NOT* — overrideable only when both (a) lead-developer sign-off
  is recorded in the PR description and (b) the target platform is
  pinned in `certification-platform.md`. Source: §3.4.2.
- FR-CS-041: Required RNG: `SplitMix64` helper. *MUST*.
  Source: §3.4.3 (det-required-apis).
- FR-CS-042: Required time source: `MatchClock` injection. *MUST*.
  Source: §3.4.3.
- FR-CS-043: Required math helper for trigonometry. *MUST*.
  Source: §3.4.3.
- FR-CS-044: Where 64-bit intermediate multiplication occurs in
  seed/hash chains, the multiplication MUST be wrapped in an
  `unchecked { … }` scope with a one-line comment citing §3.4.4.
  *MUST* (where applicable). Source: §3.4.4 (C# rule).
- FR-CS-045: Where Python (or other-language) tooling mirrors C#
  `[FIXED]` or `[DERIVED]` constants, intermediate multiplications
  MUST be masked with `& 0xFFFFFFFFFFFFFFFF` and the `UL` suffix MUST
  be omitted. *MUST* (where applicable). Source: §3.4.4 (Python rule).

#### 2.2.5 Dependency Direction & Interfaces — FR-CS-046 … FR-CS-055 (10 FRs)
- FR-CS-046: Layer order Physics → Mechanics → AI → UI; no upward
  references. *MUST*. Source: §3.5.2.
- FR-CS-047: Cross-spec events flow upward via struct dispatch.
  *MUST*. Source: §3.5.2.
- FR-CS-048: An `interface` file MUST live in the same assembly as at
  least one specified consumer. *MUST*. Source: §3.5.3.
- FR-CS-049: No phantom interface folders (interface declared without
  any specified consumer). *MUST NOT*. Source: §3.5.3.
- FR-CS-050: Event-vs-interface decision tree applied; chosen mechanism
  documented in the file header. *MUST*. Source: §3.5.4.
- FR-CS-051: No service locator anti-pattern. *MUST NOT*. Source: §3.5.5.
- FR-CS-052: No ambient context anti-pattern. *MUST NOT*. Source: §3.5.5.
- FR-CS-053: No static mutable singletons in game logic. *MUST NOT*.
  Source: §3.5.5.
- FR-CS-054: No generic DI container in the game loop. *MUST NOT*.
  Source: §3.5.5.
- FR-CS-055: Cross-assembly references explicit at asmdef level.
  *MUST*. Source: §4.3.

#### 2.2.6 Documentation — FR-CS-056 … FR-CS-065 (10 FRs)
- FR-CS-056: File header present on every new file. *MUST*. Source: §3.6.2.
- FR-CS-057: File header carries created date, last-modified date,
  author, spec-citation list, purpose. *MUST*. Source: §3.6.2.
- FR-CS-058: Version-history block updated on every modification.
  *MUST*. Source: §3.6.3.
- FR-CS-059: Version history lives in trailing `#region`. *MUST*.
  Source: §3.6.3.
- FR-CS-060: XML doc comments on every `public` type and member.
  *MUST*. Source: §3.6.4.
- FR-CS-061: XML doc comments on every constant declaration regardless
  of access modifier (FR-CS-060 covers public surface; FR-CS-061
  extends the rule to non-public constants — overlap on public
  constants is intentional, not duplicative). *MUST*. Source: §3.6.4
  (cross-references §3.2.2).
- FR-CS-062: Cross-reference comment style enforced (XC-/FM-/EC-/ERR-).
  *MUST*. Source: §3.6.5.
- FR-CS-063: Cross-reference IDs match those defined in their owning
  spec (no fabrication). *MUST*. Source: §3.6.5.
- FR-CS-064: Inline comments only when WHY is non-obvious. *SHOULD*.
  Source: §3.6.6.
- FR-CS-065: No commented-out code in merged commits. *MUST NOT*.
  Source: §3.6.6.

#### 2.2.7 Code Performance Rules — FR-CS-066 … FR-CS-070 (5 FRs)
- FR-CS-066: Game-loop allocation budget = 0 bytes/frame. *MUST*.
  Source: §6.1.
- FR-CS-067: UI allocation budget < 1 MB/frame. *MUST*. Source: §6.1.
- FR-CS-068: No virtual calls in per-frame inner loops. *MUST NOT*.
  Source: §6.2.
- FR-CS-069: No `try/catch` inside per-frame inner loops. *MUST NOT*.
  Source: §6.2.
- FR-CS-070: All system-level Update methods MUST be wrapped in a
  `ProfilerMarker.Auto()` scope (or equivalent). *MUST*. Source: §6.3.

#### 2.2.8 Numeric Type Discipline — FR-CS-071 … FR-CS-073 (3 FRs)
- FR-CS-071: Stage 0 game logic uses `float`. *MUST*. Source: §3.7.1.
- FR-CS-072: `double` in game logic is banned by default.
  *MUST NOT* — overrideable only when both (a) lead-developer sign-off
  is recorded in the PR description and (b) an inline citation to the
  rationale is present at the use site. Source: §3.7.2.
- FR-CS-073: `decimal` in game logic. *MUST NOT*. Source: §3.7.3.

#### 2.2.9 Architecture Integration & Activation — FR-CS-074 … FR-CS-081 (8 FRs)

- FR-CS-074: runtime-bearing components whose correctness depends on activation have an explicit integration owner, exact integration point and orthogonal activation state; durable identity/canonical selector binding supports that record. *MUST*. Source: §3.5.6.
- FR-CS-075: every production host/composition root in the approved runtime discovery universe is classified and mechanically accounted for. *MUST*. Source: §3.5.6–§3.5.7.
- FR-CS-076: integration contract records construction/activation/update/teardown ownership. *MUST*. Source: §3.5.6.
- FR-CS-077: alternate hosts/testhosts preserve applicable invariants or approved surface-specific divergence. *MUST*. Source: §3.5.7.
- FR-CS-078: bypass claims may block only inside a mechanically closed discovery universe. *MUST*. Source: §3.5.7.
- FR-CS-079: activation-capable public surfaces are supported, test-only, mechanically non-activating, or non-public. *MUST*. Source: §3.5.7.
- FR-CS-080: explicit/implicit static initialization participating in lifecycle is declared and cannot bypass ownership. *MUST*. Source: §3.5.6–§3.5.7.
- FR-CS-081: blocking architecture claims require resolvable typed facts/current proof; unsupported semantic assertions remain report-only. *MUST*. Source: §3.5.6–§3.5.7.

#### 2.2.10 FR Table Footer
- One-paragraph note: 81 FRs total. Existing IDs were not renumbered; A3.1a appended
  FR-CS-074–081. Future FRs append at FR-CS-082+. Mirrors CLAUDE.md
  "Renumbering Cascades" hazard.

### 2.3 Failure-to-Comply Modes

**Subsection target length:** ~30 lines.

Four modes with definition + invocation criterion + record-keeping
requirement:
- Review block.
- Refactor required.
- Exception with sign-off (record format defined here).
- Tooling violation report (Stage 0+1 onward).

### 2.4 Data Structures

**Subsection target length:** ~10 lines.

One-paragraph "N/A — meta-spec" justification. Pointer to §4 for
structural conventions about other specs' data structures.

### 2.5 Version History

Standard table.

---

## SECTION 3 — TECHNICAL SPECIFICATION (`section-3.md`)

> Each subsection: rule mechanics + 1–3 worked examples (small C# code
> blocks, ~10 lines each) + cross-references. The full FR list is in §2.2;
> §3 is the "how the rule is applied" detail.

### 3.1 C# Style Rules (FR-CS-001 … FR-CS-015)

**Subsection target length:** ~150 lines, ~5 code blocks.

- 3.1.1 Naming. Code block: example file with type / method / local /
  field / const naming. Anti-example: hungarian notation explicitly
  banned.
- 3.1.2 File layout. Code block: `using` ordering + namespace
  declaration. One-paragraph rule on partial classes (Unity-generated
  excepted; rationale).
- 3.1.3 Language version & feature gating. Pin statement (citing
  `certification-platform.md`). Allowed-features bullet list with
  one-line rationale each. Banned-features bullet list.
- 3.1.4 Whitespace and braces. Decision recorded once: spaces (not
  tabs); Allman (not K&R). Permanent-exclusion pointer to §7.4.
- 3.1.5 Access modifiers. Code block: explicit `public` / `private`
  example. Rule on `internal`.

### 3.2 Constant Declaration & Tagging (FR-CS-016 … FR-CS-025)

**Subsection target length:** ~120 lines, ~3 code blocks.

- 3.2.1 Citation block (verbatim CLAUDE.md "Constant Tags" table).
- 3.2.2 Code-level binding rule: catalogue file + XML doc comment.
- 3.2.3 Tag → C# storage class mapping table (5 rows: tag | C# storage |
  doc-comment requirement | example-file pointer).
- 3.2.4 Magic-number ban. Code block: violating example + compliant
  refactor. Permitted-exceptions bullet list with rationale per
  exception.

### 3.3 Allocation Discipline (FR-CS-026 … FR-CS-035)

**Subsection target length:** ~100 lines, ~2 code blocks.

- 3.3.1 Game-loop zero-allocation rule + citation.
- 3.3.2 Banned in hot paths: pointer to Appendix D category
  "alloc-hot-path"; rule statement only (no symbol list).
- 3.3.3 Required patterns: pointer to Appendix D category
  "det-required-apis" + "det-required-patterns"; one code block
  showing ref-passed struct.
- 3.3.4 UI / non-loop budget rule.
- 3.3.5 Verification model: §5.1 (Stage 0 manual) + §5.2 (Stage 0+1
  tooling).

### 3.4 Determinism in Code (FR-CS-036 … FR-CS-045)

**Subsection target length:** ~120 lines, ~2 code blocks.

- 3.4.1 CLAUDE.md citation block.
- 3.4.2 Banned APIs: pointer to Appendix D "det-banned"; rule only.
- 3.4.3 Required APIs and patterns: pointer to Appendix D
  "det-required-apis" + "det-required-patterns".
- 3.4.4 64-bit multiplication semantics. Two distinct rules clearly
  partitioned:
  - C# game logic: `unchecked` block. Code block (~6 lines) showing
    canonical SplitMix64 step wrapped in `unchecked`.
  - Python tooling that mirrors C#: `& 0xFFFFFFFFFFFFFFFF` mask, no
    `UL` suffix. Code block (~6 lines) showing canonical SplitMix64
    step in Python.

### 3.5 Dependency Direction & Interface Design (FR-CS-046 … FR-CS-055)

**Subsection target length:** ~110 lines, ~2 diagrams (ASCII).

- 3.5.1 CLAUDE.md citation: "interfaces only when both sides specified".
- 3.5.2 Layer-order diagram (ASCII): Physics → Mechanics → AI → UI.
  Upward references = compile error rule. Struct-event flow direction.
- 3.5.3 Interface placement rule. Phantom-interface anti-example
  (cite ERR-001, ERR-004).
- 3.5.4 Event-vs-interface decision tree (ASCII flowchart, ~15 lines).
- 3.5.5 Anti-pattern enumeration: service locator, ambient context,
  static mutable singletons, generic DI container in game loop.
  Each: 1-sentence rule + 1-sentence rationale.

### 3.5.6 Integration Identity, Ownership & Activation
- Stable `component_id` plus selector-v1/current selector history.
- Canonical integration-contract owner/lifecycle/activation fields and typed N/A sentinel.
- Cross-registry bindings are declarations only until A4's resolver and fixtures make them mechanically resolvable.

### 3.5.7 Closed Runtime Surfaces, Bypasses & Static Initialization
- Compiler-backed closed discovery of production roots/children/testhosts/tooling/static initialization.
- Alternate/bypass/public-activation rules and static-init lifecycle edges.
- Regex/public-member inventory is not an absence proof; unsupported claims remain report-only until A4 closes blind spots.

### 3.6 Documentation Conventions (FR-CS-056 … FR-CS-065)

**Subsection target length:** ~90 lines, ~3 code blocks (templates
referenced from Appendices A & B; §3.6 shows minimal in-line versions).

- 3.6.1 CLAUDE.md citation: header + version-history rule.
- 3.6.2 File header: required-fields list + pointer to Appendix A.
- 3.6.3 Version-history block: pointer to Appendix B; rule on
  `#region` placement.
- 3.6.4 XML doc comment requirements. Table columns:
  `Target | Required? | FR-CS-### | Example`. Rows:
  - public type → MUST → FR-CS-060.
  - public method/property → MUST → FR-CS-060.
  - constant declaration (any access) → MUST → FR-CS-061.
  - non-public type/method → SHOULD → FR-CS-060 (informational; not
    enforced at Stage 0).
  Note row: public constants are covered by both FR-CS-060 and
  FR-CS-061; the overlap is intentional (single doc-comment satisfies
  both).
- 3.6.5 Cross-reference comment style. Code block: each XC-/FM-/EC-/ERR-
  with one example.
- 3.6.6 Inline-comment policy: cite CLAUDE.md "default to writing no
  comments"; one-line rule. No commented-out code rule (FR-CS-065).

### 3.7 Numeric Type Discipline (FR-CS-071 … FR-CS-073)

**Subsection target length:** ~30 lines.

- 3.7.1 `float` everywhere in game logic at Stage 0 (FR-CS-071).
- 3.7.2 `double` requires sign-off + inline rationale (FR-CS-072).
- 3.7.3 `decimal` banned in game logic (FR-CS-073).
- 3.7.4 Stage 5+ Fixed64 transition: pointer to Spec #9.

### 3.8 Worked Examples Index

**Subsection target length:** ~10 lines.

Pointer to Appendix C with one-line description of each exemplar file
and a mapping table (rule → exemplar line range).

### 3.9 Edge Cases (rule-application carve-outs)

**Subsection target length:** ~60 lines.

Five carve-outs, each: scope statement | rule modification | required
comment marker.
- 3.9.1 Generated code (Unity asset import).
- 3.9.2 Third-party imports (vendored as-is; wrappers conform).
- 3.9.3 Editor-only / tooling code.
- 3.9.4 Test fixtures (split by test type — verbatim from
  `outline-mid.md` v1.2 §3.9.4).
- 3.9.5 Benchmark / micro-perf scaffolds.

### 3.10 Constants Catalogue (governance metadata only)

**Subsection target length:** ~10 lines.

One-paragraph "N/A — meta-spec; tag vocabulary owned by CLAUDE.md"
justification.

### 3.11 Version History

Standard table.

---

## SECTION 4 — ARCHITECTURE & INTEGRATION (`section-4.md`)

### 4.1 `src/` Folder Layout

**Subsection target length:** ~50 lines, 1 ASCII tree diagram.

- ASCII tree showing: `src/<spec-name>/`, `src/<spec-name>/Constants/`,
  `src/<spec-name>/<types>.cs`, `src/<spec-name>/tests/`.
- One-folder-per-Stage-0-spec convention.
- Folder name = spec folder name under `docs/specs/`.

### 4.2 Constant Catalogue File Convention

**Subsection target length:** ~40 lines.

- File-naming rule: `<SpecName>Constants.cs`.
- `ProjectConstants.cs` for cross-spec `[CROSS]` source-of-truth.
- Per-tag region ordering: `[FIXED]` → `[DERIVED]` → `[CROSS]` → `[GT]`
  → `[EST]`. Rationale: most-immutable to most-mutable; aligns with
  storage-class ordering.

### 4.3 File / Module Boundary Rules

**Subsection target length:** ~70 lines.

- `internal` vs `public` access surface rules.
- No partial classes spanning logical concerns.
- Flat-namespace rule (one namespace per assembly; no nested folders
  introducing sub-namespaces). Rationale block (~6 lines) verbatim
  from `outline-mid.md` v1.2 §4.3.
- Cross-assembly references explicit at asmdef level; the labelled ten-tier provider→consumer order lives only in §3.5.2, not in a duplicate outline diagram.

### 4.4 Interface Contracts (no runtime API)

**Subsection target length:** ~10 lines.

Spec #20 publishes no runtime interface. Architecture-governance JSON contracts/registries are tooling records only and create no gameplay dependency; compiler/discovery facts flow into those records, and Spec #19 proof/gate machinery consumes applicable evidence.

### 4.5 Pointer to `src/CLAUDE.md`

**Subsection target length:** ~15 lines.

One-paragraph statement of the Spec #20 ↔ `src/CLAUDE.md` boundary:
shape vs paths.

### 4.6 Version History

Standard table.

---

## SECTION 5 — CONFORMANCE VERIFICATION (`section-5.md`)

### 5.1 Verification Model

**Subsection target length:** ~30 lines.

- Review against §2.2 FRs plus the current repository's mechanical checks.
- Reviewer-checklist pointer to §5.4.
- Architecture facts not closed by A4 remain report-only; existing code/tooling means the historical "no source code" premise is no longer current.

### 5.2 Stage 0+1 Transition: Tool Selection

**Subsection target length:** ~60 lines.

For each tool: name | what it enforces | which FRs it covers.
- `Microsoft.CodeAnalysis.NetAnalyzers` — built-in style/quality rules.
- Custom Spec #20 analyzer set — banned/required APIs (Appendix D).
- `.editorconfig` — naming, layout, var usage.
- `dotnet format` — whitespace/braces.
- `BannedSymbols.txt` — generated from Appendix D categories
  "det-banned" and "alloc-hot-path".
- Unity-specific analyzer for zero-allocation hot-path checks.

Stage 0+1 *deliverable* = tool selection.
Stage 1 *deliverable* = concrete config files (§7.1).

### 5.3 Threshold Policy

**Subsection target length:** ~25 lines.

- No numeric thresholds at Stage 0.
- Stage 1 calibration procedure (3 bullet steps).
- Until then: judgement calls flagged in review.

### 5.4 Review-Time Checklist

**Subsection target length:** ~100 lines.

Eight checklist categories — paste-ready (under each: focused yes/no questions).

- Style.
- Constants & tagging.
- Allocation.
- Determinism.
- Dependencies & interfaces.
- Documentation.
- Performance.
- Architecture Integration & Activation (FR-CS-074–081): durable identity/contracts, closed runtime surfaces, alternate hosts, bypass/public activation, static initialization, and report-only-vs-blocking evidence boundary.

### 5.5 FR-to-Verification Traceability

**Subsection target length:** ~45 lines.

Table with one row per numbered FR-CS-### plus FR-CS-046a/046b (83 rows: 81 numbered + 2 sub-clauses). Columns:
`FR ID | Stage 0 verification | Stage 1 analyzer ID (placeholder) | Stage 1 severity (placeholder)`.

Stage 0 column resolves to a §5.4 category. Stage 1 columns are
intentionally placeholder until Stage 1 (acknowledged degeneracy).

### 5.6 Determinism Verification Note

**Subsection target length:** ~10 lines.

Spec #20 owns no determinism harness; the rules in §3.4 *enable*
Spec #16 / Spec #19 harnesses to function.

### 5.7 Version History

Standard table.

---

## SECTION 6 — CODE PERFORMANCE RULES (`section-6.md`)

> Slot-reconciliation note (referenced from §1.3 KD-3).

### 6.1 Allocation Budget Rules

**Subsection target length:** ~30 lines.

- Discipline-vs-budget split (verbatim from `outline-mid.md` v1.2 §6.1).
- Game-loop budget = 0 bytes/frame.
- UI budget < 1 MB/frame.
- Citation: `development-best-practices.md`.

### 6.2 Hot-Path Rules

**Subsection target length:** ~40 lines.

- No virtual calls in per-frame inner loops (use `sealed` / static).
- No `try/catch` inside per-frame inner loops.
- Avoid `interface`-typed locals in hot paths.
- One code block: anti-example + refactor.

### 6.3 Profiling Hooks

**Subsection target length:** ~25 lines.

- `ProfilerMarker` required around every system-level Update.
- Marker naming: `<SpecName>.<Method>`.
- One code block: declaration + use.

### 6.4 Complexity Targets

**Subsection target length:** ~25 lines.

- Qualitative rules (O(1) / O(N) / O(N²)).
- Quantitative thresholds deferred to Stage 1.

### 6.5 Performance-Related FR Cross-Listing

**Subsection target length:** ~10 lines.

Pointer to FR-CS-066 … FR-CS-070 in §2.2.7. One-paragraph statement
on the §2.2-defines / §6-codifies pattern.

### 6.6 Version History

Standard table.

---

## SECTION 7 — FUTURE EXTENSIONS (`section-7.md`)

### 7.1 Stage 1 Deliverables

**Subsection target length:** ~30 lines.

Five deliverables, each: name | trigger | acceptance criterion.
- Numeric lint thresholds (D1).
- Roslyn analyzer ruleset.
- `BannedSymbols.txt` populated.
- `.editorconfig` finalised.
- First `src/CLAUDE.md`.

### 7.2 Stage 1 CI Gates

**Subsection target length:** ~25 lines.

Three gates: pre-commit, PR, merge. Each: hook command + failure
behaviour.

### 7.3 Stage 5+ Extensions

**Subsection target length:** ~20 lines.

- Fixed64 enforcement (Spec #9 trigger).
- Cross-platform bit-exact parity rules.
- `unsafe` / SIMD intrinsic policy revisit.

### 7.4 Permanent Exclusions

**Subsection target length:** ~20 lines.

- Tabs-vs-spaces (decided once in §3.1.4).
- Brace style (decided once in §3.1.4).
- Specific IoC container choice.
- Specific logging framework.

### 7.5 Deferred Decisions Tracker

**Subsection target length:** ~25 lines.

D1 … D5 with: deferral statement | trigger to revisit | owner.

### 7.6 Version History

Standard table.

---

## SECTION 8 — REFERENCES & CITATION AUDIT (`section-8.md`)

### 8.1 Source Register

**Subsection target length:** ~50 lines.

Ten sources (S-01–S-10; current register in `section-8.md`). Each row:
Source | URL or path | Retrieved date | Used by §.

### 8.2 Verification Notes

**Subsection target length:** ~15 lines.

- Every CLAUDE.md citation re-verified against current text on
  drafting date.
- Every external URL retrieved and date-stamped.
- Re-verification cadence: at every spec amendment.

### 8.3 Cross-Spec Citation Audit

**Subsection target length:** ~25 lines.

- Spec #20 cited *by*: every Stage 1+ source file (downstream).
- Spec #20 cites *to* (substantive): Project Architecture Governance (property/applicability/review/evidence authority) and Spec #19 (proof classes, bounded substitutes and gate evidence).
- Pointer-only citation: Spec #9.
- No `[CROSS]` constants imported.
- `TBD-NORMATIVE` placeholders: none; S-09/S-10 are registered substantive upstream sources, not unresolved placeholders.

### 8.4 Constant Provenance Summary

**Subsection target length:** ~10 lines.

Spec #20 declares no physical constants; tag vocabulary owned by
CLAUDE.md. (Mirrors `outline-mid.md` §8.4.)

### 8.5 Version History

Standard table.

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

### 9.1 Content Checklist

**Subsection target length:** ~30 lines, 8–12 items, each
programmatically verifiable.

- All required sections present.
- Authority Matrix in §1.3.
- 81 FR-CS-### rows in §2.2 (15 style + 10 constants + 10 alloc + 10 det + 10 deps + 10 docs + 5 perf + 3 numeric type + 8 architecture integration/activation).
- Every FR has level + source + mechanics §.
- Template-slot reconciliation note in §1.3.
- Appendices A–F present.
- Exemplar pair in Appendix C compiles (manual review at Stage 0).
- File header on every section file.
- Version history on every section file.

### 9.2 Quality Checklist

**Subsection target length:** ~40 lines, 8–12 items.

- Cite-not-redefine audit: zero CLAUDE.md restatements.
- Every Appendix D entry traced to an FR-CS-### and a CLAUDE.md (or
  `development-best-practices.md`) citation.
- No banned-API symbol appears outside Appendix D (single source of
  truth).
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- All RFC 2119 keywords used correctly.
- All "informational" / "out of scope" pointers resolve to a real
  document.
- Reviewer-checklist categories in §5.4 cover all 81 FRs (style →
  Style category; constants → Constants & Tagging; alloc + perf →
  Allocation + Performance categories; det + numeric-type →
  Determinism category; deps → Dependencies & Interfaces; docs →
  Documentation).

### 9.3 Review Checklist

**Subsection target length:** ~25 lines.

- Open issues logged in `OPEN ISSUES` of root CLAUDE.md.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated if any cross-spec drift discovered.
- `file-manifest.md` updated to reflect new spec status.
- `SPEC_INDEX.md` updated from NOT STARTED → IN REVIEW → APPROVED.

### 9.4 Decision

**Subsection target length:** ~20 lines.

- Status block.
- Approval evidence: file paths to programmatically-verifiable sources.
- Re-approval triggers: any change to root CLAUDE.md "Constant Tags",
  "When Writing Code" determinism rules, or interface principle; any
  Stage 1 calibration of numeric thresholds.

---

## APPENDICES (`appendices.md`)

### Appendix A — File Header Template

**Target length:** ~50 lines.

Paste-ready C# block. Required fields populated for a hypothetical
`BallPhysicsConstants.cs` exemplar:
- File path, created date, last-modified date, author.
- Spec citation list (Spec #1 §1.2, Spec #20 §3.6.2).
- Purpose (≤2 sentences).
- Empty version-history `#region` skeleton.

### Appendix B — Version-History Block Template

**Target length:** ~30 lines.

Paste-ready `#region VersionHistory … #endregion` block. Three columns:
date | author | change. Includes one example row.

### Appendix C — Exemplar Pair

**Target length:** ~150 lines (two C# files).

- `ExemplarStruct.cs`: ~80 lines. Demonstrates §3.1 (style),
  §3.5 (deps), §3.6 (docs), §6.2–§6.3 (perf). Inline comments
  reference §3.x and §6.x rules.
- `ExemplarConstants.cs`: ~70 lines. One constant per tag
  (`[FIXED]`, `[GT]`, `[EST]`, `[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`). Demonstrates
  §3.2, §4.2.

### Appendix D — Banned & Required APIs (single source of truth, KD-6)

**Target length:** ~80 lines.

Four-column table per category:
`Symbol | FR-CS-### | CLAUDE.md citation | Stage 1 analyzer ID (placeholder)`.

Categories (verbatim from `outline-mid.md` v1.2 Appendix D):
- `det-banned`
- `alloc-hot-path`
- `det-required-apis`
- `det-required-patterns`

Footer note: this list is the *seed* for Stage 1 `BannedSymbols.txt`;
no other document may add entries without updating this Appendix first.

### Appendix E — Glossary

**Target length:** ~40 lines.

Spec #20-specific terms only (game-loop method, hot path, per-frame
path, game-state assembly, det-banned/det-required-apis/etc.,
constants catalogue file, exception with sign-off). Physics terms
cited from owning specs (no redefinition).

### Appendix F — Architecture Integration Records

Selectors, durable identity/rename history, integration contracts, N/A representation,
runtime-surface classifications, activation states, and dependency/proof examples. These
examples demonstrate schema/record shape; they do not claim A4 resolver coverage or A8
enforcement activation.

---

## DRAFTING GUIDANCE

- Authoring order: §1 → §2 → §3 → §4 → §5 → §6 → §7 → §8 → §9 →
  Appendices.
- Every section file copies the metadata header from this outline's
  corresponding section block.
- Anti-pattern: do not draft Appendix D last. It is the single
  source of truth (KD-6); §3.3 / §3.4 / §5.2 / §7.1 cite it. Draft
  Appendix D *before* §3.3 / §3.4 to avoid forward-reference churn.
- After §2.2 is drafted, count FR-CS-### identifiers in `section-2.md`
  and confirm 81 unique numbered IDs (one occurrence each as the row identifier;
  references elsewhere don't count as duplicates).
- After §3 is drafted, run a cite-not-redefine audit: search for
  any text that paraphrases a CLAUDE.md rule rather than citing it.
- After §8 is drafted, verify every URL in §8.1 with `curl --head`
  and date-stamp.
- Before §9 sign-off, run the §9.2 quality-checklist verifications
  programmatically and link evidence in §9.4.

---

## VERSION HISTORY

| Version | Date        | Author      | Notes                                                                         |
|---------|-------------|-------------|-------------------------------------------------------------------------------|
| 1.0     | May 6, 2026 | Claude Code | Initial detailed outline derived from `outline-mid.md` v1.2. Awaiting self-critique. |
| 1.1     | May 6, 2026 | Claude Code | Self-critique pass 1: FR-CS-008 reworded with deferred-activation language (gated on `certification-platform.md` resolution); FR-CS-040 (FMA) reworded to make sign-off + platform-pin override explicit; FR-CS-044 / FR-CS-045 scoped with "where applicable" qualifier; §3.6.4 doc-comment table given concrete columns and explicit FR-CS-060 / FR-CS-061 overlap note; added overlap clarification on FR-CS-061. |
| 1.2     | May 6, 2026 | Claude Code | Self-critique pass 2: §3.7 (Numeric Type Discipline) had no FR rows; added FR-CS-071 / FR-CS-072 / FR-CS-073 as new partition §2.2.8; total FR count 70 → 73; FR Table Footer renumbered to §2.2.9; FR-CS-070 (`ProfilerMarker`) FR text expanded to match §6.3 wording; §3.7 subsection annotated with FR coverage; §5.5 row count and §9.1 / §9.2 totals updated to 73; §5.4 coverage mapping made explicit. |
| 1.3     | May 6, 2026 | Claude Code | Self-critique pass 3: FR-CS-072 phrasing aligned with FR-CS-040 RFC 2119 pattern (MUST NOT with overrideable conditions, not invented "MUST (override-with-justification)" level). No outstanding self-critique findings. Detailed outline ready for section-file authoring. |
| 1.4     | September 2, 2026 | Codex | A3.1b synchronization: 81-FR partition with FR-CS-074–081, §3.5.6–§3.5.7 architecture mechanics, single ten-tier arrow authority, eight §5.4 categories / 83 traceability rows, Appendix F, and report-only A4 boundary. Historical 70→73 record preserved. |
| 1.5     | September 2, 2026 | Codex | Post-merge Codex-review correction: synchronizes live authority/dependency text to existing `src/CLAUDE.md`, Governance and substantive Spec #19 ownership; corrects FR-CS-074/075 mapping; updates §8 to ten sources with only Spec #9 pointer-only. |
| 1.6     | September 3, 2026 | Claude Code | Scope correction to v1.5: that revision rewrote **KD-4** here ("Verification evolves with repository state") while the authoritative `section-1.md` §1.3 kept the original decision — a KD change no A3.1b finding asked for, and one that would have made this slice carry a governance-semantic change rather than a synchronization. KD-4 restored verbatim to its pre-A3.1b text, so all three outline tiers and the section file agree again. Modernizing KD-4 against the live tree is tracked separately for A3.4. Companion-version pins refreshed. |
