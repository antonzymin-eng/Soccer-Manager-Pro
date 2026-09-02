# Code Standards & Style Guide Specification #20 — Section 5: Conformance Verification

**File:** `docs/specs/code-standards/section-5.md`
**Purpose:** Defines the Stage 0 manual-review model, Stage 0+1 tool-selection plan,
threshold policy, paste-ready review-time checklist (§5.4), 83-row FR-to-verification
traceability table (§5.5), and the determinism/architecture verification handoff.

**Created:** May 7, 2026
**Modified:** September 2, 2026
**Version:** 1.5
**Status:** AMENDMENT DRAFT (A3.1b; approved v1.4 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 5
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.35, §6; A3.1b
**Subsection target lengths:** §5.1 ~30 lines · §5.2 ~60 lines · §5.3 ~25 lines ·
§5.4 ~80 lines · §5.5 ~45 lines · §5.6 ~10 lines

---

## Table of Contents

- [5.1 Stage 0 Verification Model](#51-stage-0-verification-model)
- [5.2 Stage 0+1 Transition: Tool Selection](#52-stage-01-transition-tool-selection)
- [5.3 Threshold Policy](#53-threshold-policy)
- [5.4 Review-Time Checklist](#54-review-time-checklist)
- [5.5 FR-to-Verification Traceability](#55-fr-to-verification-traceability)
- [5.6 Determinism Verification Note](#56-determinism-verification-note)
- [5.7 Version History](#57-version-history)

---

## 5.1 Stage 0 Verification Model

This section was authored (May 2026) when `src/` held no source code and no static
analysis tool was configured. Both premises have since expired — coding began
May 19, 2026, and as of August 18, 2026 the tree holds 35 production assemblies and
947 `.cs` files (re-derive: `ls -d src/*/ | wc -l`; `find src -name '*.cs' | wc -l`),
with one of §5.2's six tools live in CI (`.github/workflows/ci.yml`): `dotnet format
whitespace --verify-no-changes` runs on every push to `main` and every PR targeting `main`, over a synthetic project (advisory —
a failure emits a warning and exits 0, "non-blocking until repo opts in"). Alongside it,
`tools/dotnet-ci/run-gate.sh` compiles the entire tree and runs every NUnit suite
(blocking; non-certifying Linux shim) — this is the whole-tree compile/test gate, not
one of §5.2's named tools (round-7 finding M4: it appears nowhere in the table above).
The **custom Spec #20 Roslyn analyzer set,
`.editorconfig`, and `BannedSymbols.txt` from §5.2's tool table remain unbuilt** —
none exists anywhere in the repository — so for the FRs those tools would enforce,
conformance verification remains **manual review** against the FRs in §2.2, using the
reviewer checklist in §5.4.

**Process:**

1. Every PR that introduces or modifies `.cs` files under `src/` must include the §5.4
   checklist, either as a PR description template section or as a linked reviewer note.
2. The reviewer works through each applicable checklist category and marks each item
   pass / fail / N/A. Any fail triggers Mode 1 (Review Block) in §2.3.
3. Items marked N/A must carry a brief justification (e.g., "no game-loop methods in
   this file", "editor-only code — §3.9.3 carve-out applies").
4. The completed checklist is preserved in the PR review trail for audit purposes.

**Tooling status.** The Stage 0 absence of tooling was intentional (KD-4 in §1.3):
empirical lint baselines cannot be established against non-existent code, and
committing to configuration files then would have produced arbitrary thresholds. The
Stage 0+1 transition (§5.2) — the designated moment to activate tooling — has since
arrived: the format check and the whole-tree compile/test gate are wired (see above),
while the analyzer-backed remainder of §5.2's tool table is still owed and the D1
numeric thresholds remain deferred (§7.5) pending a profiled baseline.

**Scope of manual review:** All MUST and MUST NOT FRs are subject to review. SHOULD
FRs are reviewed with the understanding that documented deviation is acceptable
(§2.1). MAY and inactive FRs are not subject to review.

**Reviewer qualification:** Any team member familiar with §3 mechanics may serve as
reviewer. For determinism FRs (FR-CS-036–045, FR-CS-071–073), the reviewer must
confirm they have read §3.4 and Appendix D "det-banned" before signing off.

---

## 5.2 Stage 0+1 Transition: Tool Selection

At the Stage 0+1 transition — when the first Stage 1 `.cs` files are committed — the
following tooling is activated. **Tool selection** (naming the tools and their FR
coverage) is the Stage 0+1 deliverable. **Concrete configuration files** (`.editorconfig`
content, `BannedSymbols.txt` entries, analyzer ruleset XML) are Stage 1 deliverables
that land in `src/` and are tracked in §7.1.

| Tool | What it enforces | FR-CS-### covered |
|---|---|---|
| `Microsoft.CodeAnalysis.NetAnalyzers` (built-in) | General C# style and quality rules; CS1591 missing XML docs | FR-CS-001–007, FR-CS-011–014, FR-CS-060 (partial) |
| `.editorconfig` | Naming conventions, indentation, brace style, `var` usage | FR-CS-001–004, FR-CS-006, FR-CS-011–013 |
| `dotnet format` | Whitespace and brace formatting (reads `.editorconfig`) | FR-CS-011–012 |
| **Custom Spec #20 Roslyn analyzer set** | Project-specific rules: catalogue placement, tag presence, access-modifier rules, phantom interfaces, file-header/version-history presence | FR-CS-005, FR-CS-007, FR-CS-016–022, FR-CS-025, FR-CS-047–055, FR-CS-056–065 |
| `BannedSymbols.txt` (sourced from Appendix D) | Explicit symbol-level bans; seeded from Appendix D categories "det-banned" and "alloc-hot-path" | FR-CS-010, FR-CS-027–040 |
| Unity-specific allocation analyzer | Zero-allocation checks on methods with `[GameLoopMethod]` or equivalent marker | FR-CS-026–034, FR-CS-066–067 |

**Appendix D is the seed for `BannedSymbols.txt`.** At Stage 1, each row in Appendix
D categories "det-banned" and "alloc-hot-path" generates one `BannedSymbols.txt` entry.
No other document may add entries to `BannedSymbols.txt` without first adding the
symbol to Appendix D (KD-6).

**Analyzer ID prefix reservation.** The following prefixes are reserved for the custom
Spec #20 analyzer set and appear as placeholders in §5.5 and Appendix D:

| Prefix | Domain |
|---|---|
| `CS20-STYLE-NNN` | §3.1 C# style rules |
| `CS20-CONST-NNN` | §3.2 constant tagging rules |
| `CS-ALLOC-NNN` | §3.3 allocation discipline (Appendix D) |
| `CS-DET-NNN` | §3.4 determinism (Appendix D) |
| `CS20-DEP-NNN` | §3.5 dependency direction |
| `CS20-DOC-NNN` | §3.6 documentation |
| `CS20-PERF-NNN` | §6 performance rules |

Concrete IDs are assigned when the analyzer project is created at the Stage 0+1
transition. Until then, all Stage 1 analyzer ID cells in §5.5 are placeholders.

---

## 5.3 Threshold Policy

**No numeric thresholds are pinned at Stage 0** (KD-5 in §1.3, Deferral D1 in §7.5).
The following metric types are all deferred:

- Cyclomatic complexity per method.
- Lines of code per file.
- Lines of code per method.
- Managed allocation count per frame (beyond the binary "zero vs. non-zero" rule).
- Warning count per build.

**Stage 1 calibration procedure:**

1. Run the full Spec #20 analyzer suite on the first completed Stage 1 module (the
   first spec's implementation — expected to be Ball Physics Spec #1) with all rules
   at Warning severity.
2. Review the warning distribution. Identify the top-three most-violated SHOULD-level
   rules; determine whether the violation pattern reflects a real quality gap or a
   threshold set too aggressively.
3. Set numeric thresholds to the first-module baseline rounded to the nearest
   meaningful boundary (e.g., if the longest method in Ball Physics is 47 lines, set
   the threshold at 60 lines rather than 47 or 50).
4. Record the chosen values as a §5.3 amendment (version bump to this file) before
   any other module's implementation begins. Thresholds must not be per-module; one
   set applies to the entire codebase.

**Until Stage 1 calibration:** Numeric judgement calls are made during review. A
reviewer who flags a method as "too long" should cite their reasoning in the PR
comment, not invent a threshold number. The goal is to flag genuine problems, not to
enforce an arbitrary line count.

---

## 5.4 Review-Time Checklist

Seven categories. Each item is a yes/no question; answer YES (pass), NO (fail), or
N/A (with justification). Any NO triggers Mode 1 (Review Block, §2.3) unless the
reviewer and lead developer agree to Mode 3 (Exception with sign-off).

Attach this checklist to every PR that introduces or modifies `.cs` files in `src/`.
Items that do not apply to the file under review (e.g., Performance items for a
constants-only file) are marked N/A.

---

### 5.4.1 Style (FR-CS-001–015)

```
[ ] 1. Naming — All types and methods PascalCase; all locals and parameters
        camelCase; all private fields _camelCase; ALL_CAPS used only for
        [FIXED] const in a catalogue file? (FR-CS-001–004)

[ ] 2. File — Exactly one public type per file; filename matches type name? (FR-CS-005)

[ ] 3. Using order — System → Unity → project, each group separated by a
        blank line? (FR-CS-006, SHOULD)

[ ] 4. Namespace — Declared namespace matches folder path; flat-namespace
        rule (§4.3) observed (no sub-namespace for sub-folders)? (FR-CS-007)

[ ] 5. Banned features — No dynamic, no async/await for game-state work,
        no unsafe without sign-off in game-logic code? (FR-CS-010)

[ ] 6. Formatting — 4-space indent (no tabs); Allman braces (opening on own line);
        var only when type is obvious from RHS? (FR-CS-011–013)

[ ] 7. Access modifiers — Explicit on every declaration; internal not used
        for cross-assembly surface? (FR-CS-014–015)

Note: FR-CS-008 (language version pin) is INACTIVE until certification-platform.md
resolves. FR-CS-009 is MAY-level; no pass/fail check required.
```

---

### 5.4.2 Constants & Tagging (FR-CS-016–025)

```
[ ] 1. Location — All constants declared in a catalogue file; none inline
        in formula/system/struct code? (FR-CS-016)

[ ] 2. Tag in doc comment — Every constant carries its CLAUDE.md tag
        ([GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]/[CROSS-PENDING]) in the
        immediately preceding XML doc comment? (FR-CS-017)

[ ] 3. [FIXED] storage — [FIXED] constants are public const with ALL_CAPS? (FR-CS-018)

[ ] 4. [GT] storage — [GT] constants are public static readonly, loaded
        from tunable config at boot (not a compile-time literal)? (FR-CS-019)

[ ] 5. [EST] constants — [EST] constants carry // TODO: validate and a
        spec-error-log.md entry? (FR-CS-020)

[ ] 6. [DERIVED]/[CROSS] — [DERIVED] constants carry formula doc comment;
        [CROSS] constants cite authoritative spec & section ([CROSS-PENDING]
        additionally the spec-error-log back-prop ID — §3.2.3)? (FR-CS-021–022)

[ ] 7. No magic numbers — No unqualified numeric literals in formula/system/
        struct code (permitted exceptions per FR-CS-024 checked)? (FR-CS-023)

[ ] 8. Catalogue naming & region order — File named <SpecName>Constants.cs
        (FR-CS-025); regions ordered [FIXED]→[DERIVED]→[CROSS]→[CROSS-PENDING]→
        [GT]→[EST] (§4.2/§3.2.3 — round-7 finding M5: FR-CS-025 governs file
        naming only, not region ordering)?
```

---

### 5.4.3 Allocation (FR-CS-026–035)

```
[ ] 1. Zero-alloc game loop — No managed allocation in any method on the
        60 Hz physics/render update path? (FR-CS-026)

[ ] 2. No boxing — No value-type-to-object or value-type-to-interface cast
        in hot-path code? (FR-CS-027)

[ ] 3. No LINQ — No LINQ-to-objects fluent chains in hot-path code? (FR-CS-028)

[ ] 4. No params — No params array parameters on methods called from
        hot-path code? (FR-CS-029)

[ ] 5. No string formatting — No string.Format, interpolation, or concatenation
        in per-frame paths? (FR-CS-030)

[ ] 6. No closures — No closures capturing locals in hot-path code? (FR-CS-031)

[ ] 7. No non-struct foreach — No foreach over non-struct enumerators
        in hot-path code? (FR-CS-032)

[ ] 8. Required patterns — Where allocation would otherwise occur, ref-passed
        structs / pools / struct events / stackalloc used instead? (FR-CS-033–035)

[ ] 9. No reflection — No System.Reflection APIs in hot-path code? (FR-CS-034)

(Full symbol list for items 2–7 and 9: Appendix D "alloc-hot-path".)
```

---

### 5.4.4 Determinism (FR-CS-036–045, FR-CS-071–073)

```
[ ] 1. No det-banned APIs — No banned RNG, wall-clock, process-unique-ID,
        or multithreaded game-state APIs in game-logic code?
        (FR-CS-036–039; full list: Appendix D "det-banned")

[ ] 2. FMA — Hardware-intrinsic FMA absent, or present only with sign-off
        + platform pin? (FR-CS-040)

[ ] 3. SplitMix64 — All RNG in game-logic code uses the SplitMix64 helper? (FR-CS-041)

[ ] 4. MatchClock — All simulation time sourced from injected MatchClock,
        not from wall-clock APIs? (FR-CS-042)

[ ] 5. Math helper — Trigonometry and math operations use the project math
        helper; System.Math absent or covered by sign-off? (FR-CS-043)

[ ] 6. 64-bit C# multiplication — Where seed/hash-chain multiplications
        appear in C# game-logic code, wrapped in unchecked { }
        with a §3.4.4 comment? (FR-CS-044)

[ ] 7. Python tooling masking — Where Python tooling mirrors [FIXED]/[DERIVED]
        constants and performs 64-bit multiplications, masked with
        & 0xFFFFFFFFFFFFFFFF; UL suffix absent? (FR-CS-045)

[ ] 8. Numeric type — float used throughout game-logic code; double absent
        or covered by sign-off + inline rationale; decimal absent? (FR-CS-071–073)
```

---

### 5.4.5 Dependencies & Interfaces (FR-CS-046–055)

```
[ ] 1. Tier order — Assembly references flow down the §3.5.2 ten-tier order
        only; no upward references? Intra-tier references are permitted, but
        the production reference graph stays acyclic. No ordered-tier assembly
        references an out-of-band Infrastructure assembly
        (performance-optimization, testing-strategy), and each Infrastructure
        assembly references only tier-0 (Foundation) assemblies and its
        Infrastructure peer? (FR-CS-046, FR-CS-046a, FR-CS-046b)

[ ] 2. Struct events — Cross-layer upward notifications dispatched as struct
        events, not class-based delegates? (FR-CS-047)

[ ] 3. Interface placement — Every interface lives in the same assembly as
        at least one specified consumer? (FR-CS-048)

[ ] 4. No phantom interfaces — No interface defined for an unspecified or
        unwritten consumer? (FR-CS-049)

[ ] 5. Decision tree applied — Event-vs-interface decision tree (§3.5.4)
        applied; chosen mechanism documented in file header? (FR-CS-050)

[ ] 6. Anti-patterns absent — No service locator, ambient context, static
        mutable singleton, or generic DI container in game-loop code?
        (FR-CS-051–054)

[ ] 7. .asmdef references — All cross-assembly dependencies declared
        explicitly in .asmdef; no implicit namespace-based coupling? (FR-CS-055)
```

---

### 5.4.6 Documentation (FR-CS-056–065)

```
[ ] 1. File header — Every new .cs file opens with the Appendix A header
        block? (FR-CS-056)

[ ] 2. Header fields — Header contains all required fields: path, created date,
        modified date, author, spec-citation list, purpose ≤ 2 sentences? (FR-CS-057)

[ ] 3. Version history — #region VersionHistory updated with a new row for
        this change; placed at end of file? (FR-CS-058–059)

[ ] 4. XML docs — Every public type and public/protected member has an XML
        doc comment; every constant has an XML doc comment regardless of
        access modifier? (FR-CS-060–061)

[ ] 5. Cross-reference style — Cross-reference comments use XC-/FM-/EC-/ERR-
        format; IDs match those defined in owning spec? (FR-CS-062–063)

[ ] 6. Inline comments — Comments explain WHY only; no WHAT comments,
        no task-reference comments? (FR-CS-064, SHOULD)

[ ] 7. No commented-out code — No commented-out code blocks in this
        commit? (FR-CS-065)
```

---

### 5.4.7 Performance (FR-CS-066–070)

```
[ ] 1. Game-loop budget — Game-loop code produces zero managed-memory
        allocations per frame? (FR-CS-066)

[ ] 2. UI budget — Presentation/Client-tier code (§3.5.2 tiers 8-9) stays under
        1 MB allocations per frame? (FR-CS-067)

[ ] 3. No virtual in inner loops — No virtual method calls inside per-frame
        inner loops; sealed or static dispatch used instead? (FR-CS-068)

[ ] 4. No try/catch in inner loops — No try/catch inside per-frame inner
        loops; exception handling at system/frame boundaries? (FR-CS-069)

[ ] 5. ProfilerMarker — Every system-level Update method wrapped in a
        ProfilerMarker.Auto() scope named <SpecName>.<MethodName>? (FR-CS-070)
```

### 5.4.8 Architecture Integration & Activation (FR-CS-074–081)

```
[ ] 1. Durable identity — Applicable runtime-bearing components have a stable component_id and an unambiguous canonical selector; renames/moves preserve identity and selector history? (FR-CS-074)
[ ] 2. Ownership & lifecycle — Applicable components have integration-contract records for host, assembly, composition root, construction, activation, update/use and teardown ownership; N/A is used only for genuinely absent phases/testhosts? (FR-CS-075–076)
[ ] 3. Alternate hosts — Production alternates, testhosts and tooling activation surfaces are classified and preserve the applicable invariant or carry an approved, surface-specific divergence? (FR-CS-077)
[ ] 4. Bypasses — Within a mechanically closed discovery universe, known bypass paths are absent/prohibited or explicitly supported and proved; a known-path list is not treated as proof that no other bypass exists? (FR-CS-078)
[ ] 5. Public activation surface — Every activation-capable public surface is contract-supported, test-only, mechanically non-activating, or non-public? (FR-CS-079)
[ ] 6. Static initialization — Explicit and compiler-generated type initialization that participates in construction/activation/order is declared and cannot create an undeclared root or bypass lifecycle ownership? (FR-CS-080)
[ ] 7. Evidence boundary — Blocking claims rely only on compiler-supported facts, resolvable typed records, and current Spec #19 proof evidence; unresolved cross-registry bindings, unsupported absence claims and unverified disabled anchors remain report-only until A4 closes the resolver/discovery fixtures and the later activation stage enables them? (FR-CS-081)
```

**Current enforcement boundary (A3.1b):** this checklist is the amendment-draft review surface. A4 still owns compiler-backed cross-registry resolution, closed discovery and blind-spot fixtures; Spec #19 owns proof/gate execution. A3.1b does not convert those pending facts into Machine-blocking evidence.

---

## 5.5 FR-to-Verification Traceability

One row per FR, plus one per sub-numbered clause. Stage 0 verification resolves to the §5.4 category a reviewer uses.
Stage 1 columns are placeholders (intentional — baselines are deferred to D1; see
§5.3). Analyzer IDs use the reserved prefixes from §5.2; concrete IDs assigned at
Stage 0+1 transition.

Legend: **E** = Error (blocks build) · **W** = Warning · **–** = Not analyzer-enforced.

| FR-CS-### | Stage 0 verification (§5.4 category) | Stage 1 analyzer ID (placeholder) | Stage 1 severity |
|---|---|---|---|
| FR-CS-001 | Style — §5.4.1 item 1 | `.editorconfig` naming rule | E |
| FR-CS-002 | Style — §5.4.1 item 1 | `.editorconfig` naming rule | E |
| FR-CS-003 | Style — §5.4.1 item 1 | `.editorconfig` naming rule | E |
| FR-CS-004 | Style — §5.4.1 item 1 | `.editorconfig` naming rule | E |
| FR-CS-005 | Style — §5.4.1 item 2 | `CS20-STYLE-001` | E |
| FR-CS-006 | Style — §5.4.1 item 3 | `.editorconfig` using-order | W |
| FR-CS-007 | Style — §5.4.1 item 4 | `CS20-STYLE-002` | E |
| FR-CS-008 | **INACTIVE** — deferred until `certification-platform.md` resolves | Activated on platform pin | E (when active) |
| FR-CS-009 | MAY — no pass/fail check | N/A | – |
| FR-CS-010 | Style — §5.4.1 item 5 | `BannedSymbols.txt` (`dynamic`); `CS20-STYLE-003` | E |
| FR-CS-011 | Style — §5.4.1 item 6 | `.editorconfig` `indent_size = 4` | E |
| FR-CS-012 | Style — §5.4.1 item 6 | `.editorconfig` `csharp_new_line_before_open_brace` | E |
| FR-CS-013 | Style — §5.4.1 item 6 | `.editorconfig` `csharp_style_var_*` | W |
| FR-CS-014 | Style — §5.4.1 item 7 | `CS20-STYLE-004` | E |
| FR-CS-015 | Style — §5.4.1 item 7 | `CS20-STYLE-005` | E |
| FR-CS-016 | Constants & Tagging — §5.4.2 item 1 | `CS20-CONST-001` | E |
| FR-CS-017 | Constants & Tagging — §5.4.2 item 2 | `CS20-CONST-002` | E |
| FR-CS-018 | Constants & Tagging — §5.4.2 item 3 | `CS20-CONST-003` | E |
| FR-CS-019 | Constants & Tagging — §5.4.2 item 4 | `CS20-CONST-004` | E |
| FR-CS-020 | Constants & Tagging — §5.4.2 item 5 | `CS20-CONST-005` | E |
| FR-CS-021 | Constants & Tagging — §5.4.2 item 6 | `CS20-CONST-006` | E |
| FR-CS-022 | Constants & Tagging — §5.4.2 item 6 | `CS20-CONST-007` | E |
| FR-CS-023 | Constants & Tagging — §5.4.2 item 7 | `CS20-CONST-008` | E |
| FR-CS-024 | MAY — no pass/fail check | N/A | – |
| FR-CS-025 | Constants & Tagging — §5.4.2 item 8 (naming half only; the item's region-order half traces to §4.2/§3.2.3, not this FR — round-7 finding M5) | `CS20-CONST-009` | E |
| FR-CS-026 | Allocation — §5.4.3 item 1 | Unity alloc analyzer (game-loop path) | E |
| FR-CS-027 | Allocation — §5.4.3 item 2 | `CS-ALLOC-001` | E |
| FR-CS-028 | Allocation — §5.4.3 item 3 | `CS-ALLOC-002` | E |
| FR-CS-029 | Allocation — §5.4.3 item 4 | `CS-ALLOC-003` | E |
| FR-CS-030 | Allocation — §5.4.3 item 5 | `CS-ALLOC-004`, `CS-ALLOC-005`, `CS-ALLOC-006` | E |
| FR-CS-031 | Allocation — §5.4.3 item 6 | `CS-ALLOC-007` | E |
| FR-CS-032 | Allocation — §5.4.3 item 7 | `CS-ALLOC-008` | E |
| FR-CS-033 | Allocation — §5.4.3 item 8 | Manual review (pattern guidance) | W |
| FR-CS-034 | Allocation — §5.4.3 item 9 | `CS-ALLOC-009` | E |
| FR-CS-035 | MAY — no pass/fail check | N/A | – |
| FR-CS-036 | Determinism — §5.4.4 item 1 | `CS-DET-001`, `CS-DET-002` | E |
| FR-CS-037 | Determinism — §5.4.4 item 1 | `CS-DET-003`, `CS-DET-004`, `CS-DET-005`, `CS-DET-006` | E |
| FR-CS-038 | Determinism — §5.4.4 item 1 | `CS-DET-007` | E |
| FR-CS-039 | Determinism — §5.4.4 item 1 | `CS-DET-008`, `CS-DET-009`, `CS-DET-010`, `CS-DET-011` | E |
| FR-CS-040 | Determinism — §5.4.4 item 2 | `CS-DET-012` | E |
| FR-CS-041 | Determinism — §5.4.4 item 3 | `CS20-DET-001` | E |
| FR-CS-042 | Determinism — §5.4.4 item 4 | `CS20-DET-002` | E |
| FR-CS-043 | Determinism — §5.4.4 item 5 | `CS20-DET-003` | W |
| FR-CS-044 | Determinism — §5.4.4 item 6 | `CS20-DET-004` | W |
| FR-CS-045 | Determinism — §5.4.4 item 7 | Manual review (Python tooling; not C# analyzer) | W (manual) |
| FR-CS-046 | Dependencies & Interfaces — §5.4.5 item 1 | `.asmdef` reference graph check | E |
| FR-CS-046a | Dependencies & Interfaces — §5.4.5 item 1 | `.asmdef` cycle → build error (Unity + `tools/dotnet-ci`); `tools/assembly-tier-check.py` | E |
| FR-CS-046b | Dependencies & Interfaces — §5.4.5 item 1 | `tools/assembly-tier-check.py` (ordered-tier → Infrastructure reference) | E |
| FR-CS-047 | Dependencies & Interfaces — §5.4.5 item 2 | `CS20-DEP-001` | E |
| FR-CS-048 | Dependencies & Interfaces — §5.4.5 item 3 | `CS20-DEP-002` | E |
| FR-CS-049 | Dependencies & Interfaces — §5.4.5 item 4 | `CS20-DEP-003` | E |
| FR-CS-050 | Dependencies & Interfaces — §5.4.5 item 5 | Manual review (file-header field) | W |
| FR-CS-051 | Dependencies & Interfaces — §5.4.5 item 6 | `CS20-DEP-004` | E |
| FR-CS-052 | Dependencies & Interfaces — §5.4.5 item 6 | `CS20-DEP-005` | E |
| FR-CS-053 | Dependencies & Interfaces — §5.4.5 item 6 | `CS20-DEP-006` | E |
| FR-CS-054 | Dependencies & Interfaces — §5.4.5 item 6 | `BannedSymbols.txt` (DI container types) | E |
| FR-CS-055 | Dependencies & Interfaces — §5.4.5 item 7 | `.asmdef` audit (`CS20-DEP-007`) | E |
| FR-CS-056 | Documentation — §5.4.6 item 1 | `CS20-DOC-001` | E |
| FR-CS-057 | Documentation — §5.4.6 item 2 | `CS20-DOC-002` | E |
| FR-CS-058 | Documentation — §5.4.6 item 3 | `CS20-DOC-003` | E |
| FR-CS-059 | Documentation — §5.4.6 item 4 | `CS20-DOC-004` | E |
| FR-CS-060 | Documentation — §5.4.6 item 4 | `CS1591` (NetAnalyzers) | E |
| FR-CS-061 | Documentation — §5.4.6 item 4 | `CS20-DOC-005` | E |
| FR-CS-062 | Documentation — §5.4.6 item 5 | `CS20-DOC-006` | W |
| FR-CS-063 | Documentation — §5.4.6 item 5 | Manual review (ID must exist in owning spec) | W |
| FR-CS-064 | Documentation — §5.4.6 item 6 | Manual review (SHOULD-level) | – |
| FR-CS-065 | Documentation — §5.4.6 item 7 | `CS20-DOC-007` | E |
| FR-CS-066 | Performance — §5.4.7 item 1 | Unity alloc profiler test | E |
| FR-CS-067 | Performance — §5.4.7 item 2 | Unity alloc profiler test | E |
| FR-CS-068 | Performance — §5.4.7 item 3 | `CS20-PERF-001` | E |
| FR-CS-069 | Performance — §5.4.7 item 4 | `CS20-PERF-002` | E |
| FR-CS-070 | Performance — §5.4.7 item 5 | `CS20-PERF-003` | E |
| FR-CS-071 | Determinism — §5.4.4 item 8 | `CS20-DET-005` | E |
| FR-CS-072 | Determinism — §5.4.4 item 8 | `CS20-DET-006` | E |
| FR-CS-073 | Determinism — §5.4.4 item 8 | `BannedSymbols.txt` (`decimal`) | E |
| FR-CS-074 | Architecture — §5.4.8 item 1 | A4 canonical-selector / identity resolver | – (report-only until A4/A8 activation) |
| FR-CS-075 | Architecture — §5.4.8 items 2–3 | A4 contract + closed-surface resolver | – (report-only until A4/A8 activation) |
| FR-CS-076 | Architecture — §5.4.8 item 2 | A4 integration-contract resolver | – (report-only until A4/A8 activation) |
| FR-CS-077 | Architecture — §5.4.8 item 3 | A4 alternate-host discovery + Spec #19 proof | – (report-only until A4/A8 activation) |
| FR-CS-078 | Architecture — §5.4.8 item 4 | A4 compiler-backed bypass closure + Spec #19 proof | – (report-only until A4/A8 activation) |
| FR-CS-079 | Architecture — §5.4.8 item 5 | A4 public activation-surface discovery | – (report-only until A4/A8 activation) |
| FR-CS-080 | Architecture — §5.4.8 item 6 | A4 static-initialization discovery / lifecycle edges | – (report-only until A4/A8 activation) |
| FR-CS-081 | Architecture — §5.4.8 item 7 | A4 typed-record resolution + Spec #19 proof freshness | – (report-only until A4/A8 activation) |

**Traceability coverage:** All 81 numbered FRs have a review path, as do the two sub-numbered clauses FR-CS-046a and FR-CS-046b — 83 rows in total. The sub-clauses are listed for traceability and are **outside the 81-FR count** (§2.2.10's partition Count column reports numbered FR IDs, not traceability rows). FR-CS-074–081 are deliberately report-only in the Stage 1/mechanical columns until A4 supplies the resolver/discovery evidence and the later activation stage enables verified checks; this table does not manufacture enforcement from declarations. FR-CS-008 is
marked INACTIVE with a defined activation condition. FR-CS-009, FR-CS-024, and
FR-CS-035 are MAY-level; no enforcement row is needed. FR-CS-045 and FR-CS-063 are
verified by manual review because their correctness depends on cross-document matching
(Python tooling, spec ID validity) that cannot be automated without custom tooling
beyond the C# analyzer scope. FR-CS-045 and FR-CS-063 carry severity **W (manual)** —
the rules are MUST-level but enforcement is by reviewer eyes per PR; "W" signals
"required-but-manual" rather than the bare "–" which previously read as "no
enforcement."

---

## 5.6 Determinism Verification Note

Spec #20 owns no determinism harness and publishes no numerical test vectors. Its role
in determinism is enabling, not testing: the rules in §3.4 (FR-CS-036–045) and
FR-CS-071–073 ensure that every game-logic source file is structurally compatible with
the determinism harnesses defined in Spec #16 (Deterministic Simulation) and Spec #19
(Testing Strategy & Framework).

Specifically:

- §3.4's banned-API rules prevent non-deterministic state from entering game-logic
  assemblies, which is a precondition for Spec #16's snapshot-comparison harness.
- §3.4's required-API rules (`SplitMix64`, `MatchClock`) ensure the RNG seed and time
  interfaces that Spec #16's harness controls are the ones actually used.
- FR-CS-071 (`float` throughout at Stage 0) ensures the numeric type is consistent
  with the precision assumptions in approved physics specs (#1–#8).

No Spec #20 test vectors or golden files exist; the numerical verification corpus
belongs to Spec #16 and Spec #19.

---

## 5.7 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 5. All 73 FRs covered in §5.5 traceability table; §5.4 paste-ready checklist in seven categories; §5.2 tool-selection table with analyzer-prefix reservation. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fix (audit finding L-B): §5.5 FR-CS-045 severity changed from `–` ("not analyzer-enforced", which read as "no enforcement") to `W (manual)` to signal "MUST-level rule, manual-review enforcement per PR" — aligned with FR-CS-063's existing `W` treatment. §5.5 footnote prose extended to explain the W-manual semantic. No change to the rule itself. | — |
| 1.0.2 | August 17, 2026 | Claude Code | **`ERR-020-002` adopted.** §5.4.5 checklist item 1 restated against the §3.5.2 ten-tier order (it named the retired three-layer chain) and extended to cover FR-CS-046a's intra-tier acyclicity. The §5.5 traceability row for FR-CS-046 covers FR-CS-046a as a sub-clause of the same rule; the 73-row count is unchanged. Header corrected: `Status DRAFT` against a SPEC_INDEX status of APPROVED. **⚠️ ANNOTATED (v1.0.3, August 18, 2026): this row's description is now FALSE of its own file** — a later, unversioned edit added dedicated §5.5 traceability rows for FR-CS-046a and FR-CS-046b (75 rows total) and rewrote the coverage note to say so, superseding both the "covers FR-CS-046a as a sub-clause of the same rule" mechanism and the "73-row count is unchanged" claim, while the header stayed at 1.0.2 and the file's Purpose line still advertised a "73-row" table. The unversioned edit is versioned by the 1.0.3 row below; this row is left in place per the annotate-don't-rewrite convention. | — |
| 1.0.3 | August 18, 2026 | Claude Code | **Adversarial-review findings, reviewed round (Mediums).** (1) Versions the previously unversioned edit annotated in the 1.0.2 row above: §5.5 gained dedicated traceability rows for FR-CS-046a and FR-CS-046b, and the coverage note was rewritten to "75 rows in total" with the sub-clauses stated outside the 73-FR count. (2) Header Purpose line "73-row" → "75-row" to match the table the file actually holds. (3) §5.4.5 item 1 extended to cover **FR-CS-046b** — §5.5's FR-CS-046b row routed its checklist path to "§5.4.5 item 1", but item 1 cited only FR-CS-046/046a and never mentioned Infrastructure; it now checks both FR-CS-046b clauses (no ordered-tier → Infrastructure reference; Infrastructure references only tier 0 and its peer) — and its title standardised "Layer order" → "Tier order" per the §3.5.2 vocabulary. | — |
| 1.1 | August 18, 2026 | Claude Code | **Adversarial-review round-6 finding H5.** §5.1's opening ("At Stage 0 no source code exists; all static analysis tools are therefore untriggered") and its "No tooling required at Stage 0" paragraph both asserted a state fifteen months stale — and the opening contradicted the §5.1 process list two lines below it, which legislates for PRs "that introduce or modify `.cs` files under `src/`". Restated against the live tree, every figure re-derived August 18, 2026: 35 production assemblies (`ls -d src/*/ | wc -l`), 947 `.cs` files (`find src -name '*.cs' | wc -l`), `dotnet format whitespace --verify-no-changes` advisory on every push and `tools/dotnet-ci/run-gate.sh` blocking on every push (both in `.github/workflows/ci.yml`). What genuinely remains missing is stated without overreach: the custom Spec #20 Roslyn analyzer set, `.editorconfig`, and `BannedSymbols.txt` exist nowhere in the repository, so those FRs remain manually reviewed and the KD-4/D1 threshold deferral stands (no profiled baseline yet). Consequential to round-6 H6 (see section-3.md v1.6): §5.4.2's checklist items 2, 6 and 8 extended to the six-tag vocabulary and the six-slot region order. | — |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding H3.** §5.1's "runs on every push" overstated `ci.yml`'s triggers (`branches: [main]` on both `push` and `pull_request`); a push to a topic branch — including every review branch this series has run on — triggers nothing. Corrected to "every push to `main` and every PR targeting `main`", matching §3.5.2's same-phrase correction. The v1.1 row above is left as written per the do-not-rewrite-history convention and carries the same overstatement as a record of what was written. | — |
| 1.3 | August 18, 2026 | Claude Code | **Adversarial-review round-7 findings M4 + M5.** M4: §5.1 counted `tools/dotnet-ci/run-gate.sh` as one of "two of §5.2's tools live in CI" — `run-gate.sh` is the whole-tree compile/test gate, not a row in §5.2's six-tool table; restated as one §5.2 tool (`dotnet format`) live in CI, alongside the separately-named compile/test gate. M5: three sites cited `FR-CS-025` as the authority for per-tag `#region` ordering; verified against §2.2.2 that FR-CS-025 governs catalogue file naming only. §5.4.2 checklist item 8 re-cited (naming → FR-CS-025, region order → §4.2/§3.2.3); the §5.5 traceability row for FR-CS-025 annotated to scope its `CS20-CONST-009` mapping to the naming half of item 8 only. | — |
| 1.4 | September 2, 2026 | Claude Code | **A3.1a review correction — renumbering sweep completed here.** §5.5's coverage note cited "§2.2.9's partition Count column"; the A3.1a amendment draft gave §2.2.9 to the new Architecture Integration & Activation partition and moved the FR Table Footer, with its Count column, to §2.2.10 — the same defect `section-2.md` v1.6.1 repaired at its own site and did not sweep. Annotated rather than re-pointed: this file is APPROVED and describes the approved v1.5 baseline of `section-2.md`, where the Count column genuinely is §2.2.9, so the citation stands and the note now names the draft's renumbering and the slice that syncs it (A3.1b, which owns this file's FR-CS-074–081 rows and its stale 73/75 counts). Status stays APPROVED; no traceability row, count, severity, or checklist item changed. | PENDING — A3.4 |
| 1.5 | September 2, 2026 | Codex | **A3.1b supporting-surface synchronization.** §5.4 gains the eighth Architecture Integration & Activation checklist category; §5.5 gains FR-CS-074–081, making 81 numbered FRs / 83 traceability rows including 046a/046b. Pending A4 cross-registry/discovery facts are explicitly report-only and Spec #19 retains proof/gate ownership. | PENDING — A3.4 |

---

*End of Section 5 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
