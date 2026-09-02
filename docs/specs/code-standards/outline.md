# Code Standards & Style Guide Specification #20 — High-Level Outline

**Purpose:** Planning document for the Code Standards & Style Guide
specification. Establishes scope, authority boundaries, and section structure
before mid-level and detailed outlines are drafted. Defines *what* will be
written, not yet *how*.

**Created:** May 6, 2026, 6:30 PM PST
**Updated:** September 2, 2026
**Version:** 1.1
**Status:** DRAFT — A3.1b synchronized; normative section files control
**Specification Number:** 20 of 20 (Stage 0)
**Estimated Effort:** ~14 hours (lighter than physics specs; no formula
derivations or numerical verification)
**Dependencies:** Root `CLAUDE.md` (project invariants), `docs/planning/development-best-practices.md` (performance guidance), Project Architecture Governance (architecture property/review/evidence authority), and Spec #19 for executable proof/gate mechanics used by the A3 amendment.
**Downstream:** existing `src/CLAUDE.md`, architecture-governance registries/tooling, and implementation code.
**Adjacency note:** No physics/AI domain rule is imported from Specs #1–#18. The A3 architecture amendment deliberately depends on Governance and delegates proof/gate mechanics to Spec #19.

---

## EXECUTIVE SUMMARY

Spec #20 is the lone meta-spec in the Stage 0 set. It does not model a
physical or AI subsystem; it codifies the **rules every Stage 1+ source file
must obey**. Its job is to be the single, citable reference that every code
review, every static-analysis rule, and every future `src/CLAUDE.md` entry
points at.

The spec governs:
- C# style (naming, layout, language features in/out)
- Constant declaration and tagging (`[GT] / [EST] / [FIXED] / [DERIVED] / [CROSS] / [CROSS-PENDING]`)
- File and folder naming inside `src/`
- Module dependency direction, integration ownership/lifecycle, activation surfaces, bypasses and interface design
- Determinism rules in code (no `System.Random`, no `DateTime.Now`,
  SplitMix64 RNG, masked intermediate multiplication)
- Allocation discipline in the game loop (zero-alloc, struct-based)
- Documentation conventions (file headers, version-history blocks)
- Conformance verification (manual at Stage 0; tooling at Stage 0+1
  transition)

The spec **cites, never redefines**, project invariants that already live in
root `CLAUDE.md`. This is a hard rule (see Authority Matrix in §1).

---

## AUTHORITY MATRIX (preview — full table in §1)

| Rule class                              | Authoritative source       | Spec #20 role          |
|-----------------------------------------|----------------------------|------------------------|
| Coordinate system, fatigue convention   | Ball Physics #1, CLAUDE.md | Cite, do not restate   |
| Constant tags (`[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`) | Root `CLAUDE.md`           | Cite + give code-level binding rules |
| Interface principle (both sides specified) | Root `CLAUDE.md`           | Cite + give file-level binding rules |
| Architecture property/review/evidence model | Project Architecture Governance | Cite; bind to code/integration surfaces without duplicating it |
| Executable proof + gate mechanics | Spec #19 | Delegate; consume evidence, do not redefine |
| Determinism rules (no `Random`, etc.)   | Root `CLAUDE.md`           | Cite + give lint-equivalent rules |
| Stage 0 numeric type (`float`)          | Root `CLAUDE.md`, Spec #9  | Cite, do not restate   |
| C# style, naming, layout                | **Spec #20**               | Authoritative          |
| File naming inside `src/`               | **Spec #20**               | Authoritative          |
| Folder layout inside `src/`             | **Spec #20** (high-level), `src/CLAUDE.md` (concrete paths) | Authoritative for shape; defers concrete paths to `src/CLAUDE.md` |
| Constant catalogue file locations       | **Spec #20** (convention), `src/CLAUDE.md` (concrete paths) | Authoritative for convention; defers concrete paths to `src/CLAUDE.md` |
| Build/test commands                     | `src/CLAUDE.md` (deferred) | Out of scope           |

---

## SECTION PLAN (mapped to CLAUDE.md 9-section template)

- **Section 1 — Purpose & Scope.** Authority matrix; what this spec owns;
  what it cites; what is out of scope (build commands, IDE setup,
  test-framework choice — those belong elsewhere); dependencies on root
  `CLAUDE.md`.
- **Section 2 — Functional Requirements & Conformance Model.** Numbered
  rules (FR-CS-001 …) every file must obey; conformance levels (MUST /
  SHOULD / MAY); failure-to-comply modes (review block, refactor required,
  exception with sign-off).
- **Section 3 — Technical Specification.** The substantive rules:
  3.1 C# style (naming, layout, language features allowed/disallowed)
  3.2 Constant declaration and tagging (binding rules, not redefining tags)
  3.3 Allocation discipline (zero-alloc game loop, ref-passed structs,
      LINQ exclusions, boxing avoidance)
  3.4 Determinism in code (RNG, time sources, math intrinsics)
  3.5 Dependency direction, interface design, integration ownership/lifecycle,
      closed runtime surfaces, bypasses, activation and static initialization
  3.6 Documentation: file header template, version-history block,
      cross-reference comment style
  3.7 Stage 0 numeric type (`float`) — pointer to Spec #9 for Fixed64
- **Section 4 — Architecture & Integration.**
  4.1 `src/` folder layout shape (one assembly per concern, dependency arrows)
  4.2 Constant catalogue convention (one per spec + project-wide root)
  4.3 File/module boundary rules
  4.4 Governance integration records are tooling contracts, not runtime interfaces
  4.5 Pointer to the existing `src/CLAUDE.md` for concrete paths/build guidance
- **Section 5 — Conformance Verification.**
  5.1 Stage 0 verification: manual review against this spec
  5.2 Stage 0+1 transition: tool selection (Roslyn analyzers, .editorconfig,
      `dotnet format`, `BannedSymbols.txt`)
  5.3 Threshold policy (no values pinned at Stage 0; deferred to first real
      code in Stage 1)
  5.4 Review-time checklist (including Architecture Integration & Activation)
  5.5 FR-to-verification traceability; pending A4 facts remain report-only
- **Section 6 — Code Performance Rules.** *(Re-purposed from "Performance
  Analysis" template slot.)* Allocation budgets code must obey, hot-path
  rules, profiling hooks required in game-loop code. **Not** a performance
  analysis of the standards themselves (which would be vacuous).
- **Section 7 — Future Extensions.** Lint baseline values (deferred to
  Stage 1); CI gates and pre-commit hooks (deferred); `src/CLAUDE.md`
  scope; multiplayer-era additions (Fixed64 enforcement when Spec #9
  activates in Stage 5+); permanent exclusions (style debates the spec
  refuses to relitigate).
- **Section 8 — References.** Source register includes root `CLAUDE.md`, `development-best-practices.md`, Project Architecture Governance, Spec #19, Microsoft C# conventions, Unity performance guidance, RFC 2119 and Roslyn docs. Cross-spec audit distinguishes architecture-governance dependencies from physics/AI domain rules.
  Constant provenance summary (Spec #20 declares no physical constants;
  tags listed are governance metadata, not values).
- **Section 9 — Approval Checklist.** Standard 4-block checklist with
  programmatically-verifiable evidence.
- **Appendices.**
  - Appendix A — File header template (paste-ready C# block).
  - Appendix B — Version-history block template.
  - Appendix C — Exemplar pair: one struct file + one constants file
    showing every rule applied.
  - Appendix D — Banned/required-API list (informational at Stage 0;
    becomes `BannedSymbols.txt` source at Stage 1).
  - Appendix E — Glossary (only terms specific to Spec #20; physics terms
    cited from their owning spec).
  - Appendix F — Architecture-governance record examples (selectors, contracts,
    closed runtime surfaces and proof/dependency examples).

> **Template-slot reconciliation note (§3 / §5 / §6).** The CLAUDE.md
> 9-section template was authored for physics/AI specs. For a meta-spec,
> several slots are re-purposed rather than dropped: §3 holds *rules* in
> place of formulas; §5 holds *conformance verification* in place of
> numerical test catalogues; §6 holds *performance rules code must obey*
> in place of complexity analysis. Every section retains its slot number
> and topic family so cross-spec readers find the expected content.
> Justification is restated in §1.3 (Key Design Decisions) of the
> drafted spec.

---

## ADVERSARIAL REVIEW — May 6, 2026 (resolved in v1.0)

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Original review
> was performed against the prior draft of this outline. All H findings
> are now resolved; M findings carried forward as drafting commitments;
> L findings carried forward as appendix items.

### Resolution status

| # | Severity | Finding (abbrev.) | Resolution in v1.0 |
|---|---------|-------------------|--------------------|
| 1 | H | Missing metadata header | **Resolved.** Header added at top of file. |
| 2 | H | Section plan deviates from template | **Resolved.** §6 now Code Performance Rules; §7 now Future Extensions (lint baselines deferred there); §8 now References. Migration plan dropped (see #4). Template-slot reconciliation note added. |
| 3 | H | Authority overlap with `CLAUDE.md` / `src/CLAUDE.md` | **Resolved.** Authority Matrix added (preview above; full table belongs in §1). Spec #20 owns C# style + file structure + dependency direction; root `CLAUDE.md` owns project invariants; `src/CLAUDE.md` owns codebase-local pointers. |
| 4 | H | Migration plan vestigial at Stage 0 | **Resolved.** §8 Migration Plan dropped. References slot restored to §8. There is no legacy code to migrate. |
| 5 | H | CI / pre-commit infeasible at Stage 0 | **Resolved.** §5 split: Stage 0 verification is *manual review*; tooling is a Stage 0+1 transition deliverable in §5.2; concrete CI/pre-commit configurations deferred to §7 (Future Extensions). |
| 6 | H | Lint baseline cannot be empirically validated | **Resolved.** §5.3 explicitly defers numeric thresholds to first real code. Stage 0 deliverable is *tool selection + threshold policy*, not values. |
| 7 | M | Constant-tag policy must cite, not redefine | Carried into drafting rules for §3.2. Cite root CLAUDE.md verbatim; add only code-level binding rules (e.g., "every constant in a `[FIXED]` catalogue file must be `const`, not `static readonly`"). |
| 8 | M | Interface principle must cite, not redefine | Carried into drafting rules for §3.5. Cite root CLAUDE.md "both sides specified" rule; add file-level rule (e.g., "do not place an `interface` definition in a folder whose consumer side is unwritten"). |
| 9 | M | Determinism rules absent from outline | Carried into §3.4. Surface CLAUDE.md determinism rules as enforceable code rules: banned APIs (`System.Random`, `DateTime.Now`, `Stopwatch.GetTimestamp` in game logic, `Guid.NewGuid` in game logic), required APIs (SplitMix64 helper, `MatchClock` injection), masking rules for 64-bit multiplications. |
| 10 | M | No allocation policy | Carried into §3.3 and §6. Zero-alloc game loop; no boxing in hot paths; no LINQ in tight loops; ref-passed structs over class events; no `params` in hot paths; no `string.Format` in per-frame paths. |
| 11 | M | Documentation standards must align with version-history rule | Carried into §3.6. File header template (Appendix A) and version-history template (Appendix B) restate (with citation) the CLAUDE.md "creation date and purpose header" + "version history entry on every modified file" rules. |
| 12 | L | Constant catalogue file locations not declared | Addressed at convention level in §4.2 (one catalogue per spec, named matching the spec, plus a project-wide constants file). Concrete paths deferred to `src/CLAUDE.md`. |
| 13 | L | No exemplar file in appendices | Addressed in Appendix C. Pre-Stage-1 exemplar is hypothetical struct + constants file demonstrating every Spec #20 rule; serves as a starting reference for the Stage 0+1 transition. |

---

## DRAFTING DEFERRALS (recorded so they are not forgotten)

- **D1 — Numeric lint thresholds.** Cyclomatic complexity, file length,
  method length: chosen at first real-code implementation, not now.
- **D2 — Test framework choice.** NUnit vs. Unity Test Framework vs.
  custom: belongs to Spec #19 (Testing Strategy), not Spec #20.
- **D3 — Build commands & IDE setup.** `src/CLAUDE.md` territory;
  not in this spec.
- **D4 — Fixed64 enforcement rules.** Spec #9 owns the Fixed64 library
  itself; Spec #20 will reference Spec #9 once it reaches IN REVIEW.
  Stage 0 enforcement: `float` only in game logic, no `double` in
  game logic without sign-off.

---

## VERSION HISTORY

| Version | Date           | Author      | Notes                                                                 |
|---------|----------------|-------------|-----------------------------------------------------------------------|
| 0.1     | (pre-May 2026) | Claude Code | Initial 9-line section list (no metadata, no review).                 |
| 0.2     | May 6, 2026    | Claude Code | Adversarial review appended (6H / 5M / 2L findings).                  |
| 1.0     | May 6, 2026    | Claude Code | Metadata header, Authority Matrix, re-mapped sections, all H findings resolved, M/L findings carried forward as drafting commitments. Mid-level outline begins next. |
| 1.1     | September 2, 2026 | Codex | A3.1b synchronization: Governance/#19 authority boundary, FR-CS-074–081 architecture scope, existing `src/CLAUDE.md`, §5 architecture verification, and Appendix F reflected. Historical May planning decisions remain intact. |
