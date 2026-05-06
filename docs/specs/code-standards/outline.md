# Code Standards & Style Guide Specification #20 — Outline

## Purpose
Standardize code quality, maintainability, and review consistency across all gameplay systems.

## Scope
Naming, structure, dependencies, documentation, error handling, and static-quality baselines.

## Section Plan
- Section 1 — Naming, foldering, and file-ownership conventions.
- Section 2 — Constants governance and anti-magic-number policy.
- Section 3 — Interface design and dependency-direction principles.
- Section 4 — Documentation standards, version history, review checklist.
- Section 5 — Error-handling/logging conventions and debugging hygiene.
- Section 6 — Lint/format/static-analysis baseline requirements.
- Section 7 — Enforcement workflows (pre-commit, CI, review gates).
- Section 8 — Migration plan for legacy non-compliant code.
- Section 9 — Approval checklist.
- Appendices — Templates and exemplars.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #20 status in `SPEC_INDEX.md`: NOT STARTED. Stage 0 has no source code
  (CLAUDE.md: "No code exists yet").
- CLAUDE.md defers `src/CLAUDE.md` until coding begins. Spec #20 risks
  duplicating or contradicting that file when it is eventually written.
- CLAUDE.md already declares: constant-tag policy
  (`[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`), interface design principle
  ("only when both sides specified"), determinism rules (no `System.Random`,
  no `DateTime.Now`, SplitMix64 for RNG, mask intermediates), Stage 0 uses
  `float`, struct-based zero-allocation game loop.

### Findings

1. **[H] Missing metadata header.** Same gap as siblings. Add per Shot
   Mechanics #6 outline header.

2. **[H] Section plan deviates from CLAUDE.md template.** Lint baseline
   in §6 (performance slot per template), enforcement workflows in §7
   (future-extensions slot), migration plan in §8 (references slot). No
   references slot. Re-map.

3. **[H] Authority overlap with `src/CLAUDE.md` and root `CLAUDE.md`.**
   Root CLAUDE.md already authoritatively declares constant tagging,
   interface principle, determinism rules, and Stage 0 numeric type.
   `src/CLAUDE.md` is reserved for naming conventions, constant catalogue
   locations, Unity project structure, build/test commands. Spec #20 must
   declare which document is authoritative for which rule, or two
   (eventually three) sources of truth will diverge. Recommendation: Spec
   #20 owns C# style, file structure, dependency direction; root CLAUDE.md
   owns project-level invariants; `src/CLAUDE.md` owns codebase-local
   pointers.

4. **[H] Migration plan for legacy code is vestigial at Stage 0.** §8
   "migration plan for legacy non-compliant code" — there is no legacy
   code. Either drop the section, or scope it to "post-Stage-0
   incremental adoption" explicitly.

5. **[H] CI / pre-commit enforcement infeasible at Stage 0.** §7
   "enforcement workflows (pre-commit, CI, review gates)" presumes
   tooling that does not exist in spec phase. Stage 0 enforcement is
   manual review against spec rules. Distinguish.

6. **[H] Lint baseline cannot be empirically validated at Stage 0.**
   §6 "lint/format/static-analysis baseline" must be derived from real
   code. With zero source code, baselines are guesses. Either defer to
   Stage 0+1 transition with a stub at Stage 0, or scope to "tools and
   thresholds chosen, baseline values deferred".

7. **[M] Constant-tag policy must cite, not redefine, root CLAUDE.md.**
   §2 "constants governance" must reference the canonical
   `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]` definitions in CLAUDE.md, not
   reinvent them. Renumbering / redefinition risk per project history
   (e.g., Pass Mechanics ERR-class bugs).

8. **[M] Interface principle must cite, not redefine.** §3 "interface
   design and dependency-direction principles" must explicitly reference
   the CLAUDE.md "Write interfaces only when both sides are specified"
   rule. ERR-001 / ERR-004 came from violating this.

9. **[M] Determinism rules absent from outline.** Code Standards must
   surface CLAUDE.md determinism rules (no `System.Random`, no
   `DateTime.Now`, SplitMix64, mask intermediates, no fixed64 at Stage 0)
   as enforceable lint rules. §6 currently only names "lint baseline"
   abstractly.

10. **[M] No allocation policy.** CLAUDE.md mandates struct-based,
    zero-allocation game loop. §2 / §6 should pre-commit allocation
    rules (no boxing in hot paths, no LINQ in tight loops, ref-passed
    structs over class events).

11. **[M] Documentation standards in §4 must align with version-history
    rule.** CLAUDE.md mandates "append a version history entry to every
    modified file" and "creation date and purpose header on every new
    file". Outline §4 names documentation but does not pre-commit these
    rules.

12. **[L] Constant catalogue file locations not declared.** CLAUDE.md
    flags constant catalogues as deferred to `src/CLAUDE.md`. Spec #20
    should at least name the *expected* convention (one catalogue per
    spec, named matching the spec, plus a project-wide constants file).

13. **[L] No exemplar file provided in appendices yet.** Appendices
    "templates and exemplars" — pre-Stage 1, the only exemplar can be
    a hypothetical struct + constants file. Plan one explicitly so
    Stage 0+1 transition has a starting reference.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Add Authority Matrix declaring which rules live in root CLAUDE.md vs
  Spec #20 vs (future) `src/CLAUDE.md`.
- Cite (do not redefine) constant tags, interface principle, determinism
  rules from root CLAUDE.md.
- Scope §6 lint baseline and §7 CI workflows to Stage 0+1 transition
  with explicit Stage 0 deliverables (tool selection + threshold
  policy, no values).
- Drop or rescope §8 migration plan.
