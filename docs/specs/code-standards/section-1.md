# Code Standards & Style Guide Specification #20 — Section 1: Purpose & Scope

**File:** `docs/specs/code-standards/section-1.md`
**Purpose:** Defines the scope boundary, authority matrix, key design decisions, and
dependency contracts for Spec #20. Authoritative reference for what this specification
owns, what it cites, and what is out of scope.

**Created:** May 7, 2026
**Modified:** September 2, 2026
**Version:** 1.2
**Status:** AMENDMENT DRAFT (A3.1b post-merge correction; approved v1.0.4 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 1
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.35, §6; A3.1b
**Subsection target lengths:** §1.1 ~40 lines · §1.2 ~30 lines · §1.3 ~80 lines ·
§1.4 ~25 lines

---

## Table of Contents

- [1.1 What This Specification Covers](#11-what-this-specification-covers)
- [1.2 What Is Out of Scope](#12-what-is-out-of-scope)
- [1.3 Key Design Decisions](#13-key-design-decisions)
- [1.4 Dependencies and Integration Contracts](#14-dependencies-and-integration-contracts)
- [1.5 Version History](#15-version-history)

---

## 1.1 What This Specification Covers

Spec #20 (Code Standards & Style Guide) is the governance specification for every
Stage 1+ C# source file in the **System XI** project. It establishes the
enforceable rules that all implementation code must satisfy before merging — covering
style, constant tagging at code level, allocation discipline, determinism in code,
dependency direction, documentation conventions, and conformance verification.

This specification governs the following eight areas:

1. **C# style** — naming conventions, file layout, language-feature gating, whitespace
   and braces, access modifiers.
2. **Constant declaration and tagging** — code-level binding rules for the six tag
   types defined in root `CLAUDE.md`; tag-to-storage-class mapping; magic-number
   prohibition.
3. **Allocation discipline** — zero-allocation game loop; banned allocating constructs
   in hot paths; required allocation-free patterns.
4. **Determinism in code** — banned non-deterministic APIs; required deterministic
   alternatives; 64-bit multiplication semantics for both C# game logic and Python
   tooling that mirrors C# constants.
5. **Dependency direction, interface design, and architecture integration** — tier order;
   interface placement; durable integration ownership, lifecycle/activation declarations,
   closed runtime surfaces, bypass handling, and static-initialization accountability.
6. **Documentation conventions** — file header template; version-history block; XML
   doc comments; cross-reference comment style.
7. **Conformance verification model** — RFC 2119 conformance levels; failure-to-comply
   modes; Stage 0 manual review; Stage 0+1 tooling transition.
8. **Code performance rules** — allocation budgets that game-loop code must achieve;
   hot-path rules; profiling hook requirements; complexity targets.

**Applicability:**

- **Primary scope:** Every `.cs` file under `src/`. All rules in this specification
  apply unless a carve-out in §3.9 explicitly covers the file's role.
- **Secondary scope (determinism-only):** Non-`.cs` tooling — Python scripts or
  other-language helpers — that mirrors, generates, or verifies `[FIXED]` or
  `[DERIVED]` C# constants is bound exclusively by §3.4.4's 64-bit multiplication
  masking rule. No other rule in this specification applies to non-`.cs` files.

Rule-application carve-outs for generated code, third-party imports, editor-only
tooling, test fixtures, and benchmark scaffolds are enumerated in §3.9.

---

## 1.2 What Is Out of Scope

The following areas are explicitly excluded from Spec #20. Each entry names the
authoritative owner.

| Excluded area | Authoritative owner |
|---|---|
| Build commands, CI server choice, IDE/editor configuration | `src/CLAUDE.md` (deferred; created when coding begins) |
| Test framework selection (NUnit, Unity Test Framework, or custom) | Spec #19 (Testing Strategy & Framework) |
| Architecture property admission, review disposition/convergence, proof execution semantics, and merge-gate policy | Project Architecture Governance + Spec #19; Spec #20 binds their decisions to code/integration surfaces but does not redefine them |
| Fixed64 numeric library design and API surface | Spec #9 (Fixed64 Math Library) |
| Project invariants: coordinate system, fatigue convention, heartbeat tick rates | Root `CLAUDE.md` + owning physics specs (Ball Physics #1, Agent Movement #2) |
| UX/asset pipeline conventions | Stage 1+ specs |
| PR-process rules: review-approval count, branch protection, required reviewers, merge strategy | Repository settings + `src/CLAUDE.md` |
| Concrete `BannedSymbols.txt` and `.editorconfig` files | Stage 1 deliverables (§7.1); tool selection is a Stage 0+1 transition deliverable (§5.2) |
| Non-game-state tooling: build scripts, content authoring, asset import pipelines | Out of scope, except for the determinism-only secondary-scope subset named in §1.1 |

Spec #20 governs **code content**. It does not govern process, tooling configuration,
or physical/AI system design.

---

## 1.3 Key Design Decisions

Six decisions were locked during outline development (outline v1.0 adversarial review,
all H findings resolved; mid-level outline v1.1–v1.3 self-critique passes). They are
recorded here with statement, rationale, and consequence-if-violated. Any change to a
KD requires a version bump to this section before downstream sections are revised.

### Authority Matrix

The following table is the authoritative answer to any rule-ownership question across
the System XI project. Consult it before adding a rule to any document.

| Rule class | Authoritative source | Spec #20 role |
|---|---|---|
| Coordinate system (X/Y/Z axes, corner origin) | Ball Physics Spec #1 §1.2 and Appendix C; root `CLAUDE.md` | Cite; do not restate |
| Fatigue convention (0.0 = rested, 1.0 = fatigued) | Root `CLAUDE.md` — "Fatigue Convention" | Cite; do not restate |
| Constant tags (`[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]`) | Root `CLAUDE.md` — "Constant Tags" | Cite tag definitions; add code-level binding rules (§3.2) |
| Interface principle ("write interfaces only when both sides are specified") | Root `CLAUDE.md` — "Interface Design Principle" | Cite principle; add file-level placement rules (§3.5) |
| Architecture property admission, applicability, review disposition and convergence | `docs/planning/project-architecture-governance.md` | Cite upstream authority; do not reproduce its decision/review model |
| Runtime integration ownership, lifecycle/activation declarations, closed surfaces and bypass/static-init code rules | **Spec #20** (§2.2.9, §3.5.6–§3.5.7) | Authoritative code-level binding of Governance requirements |
| Executable proof classes, bounded substitutes and gate evidence | Spec #19 (Testing Strategy & Framework) | Cite/delegate; Spec #20 must not create a second proof or gate model |
| Determinism rules (no `System.Random`, no `DateTime.Now`, SplitMix64, masking) | Root `CLAUDE.md` — "When Writing Code" | Cite rules; provide enforceable code-level formulation (§3.4) |
| Stage 0 numeric type (`float`) | Root `CLAUDE.md` — "When Writing Code"; Spec #9 | Cite; do not restate |
| Heartbeat tick rates (10 Hz AI loop / 60 Hz physics loop) | Root `CLAUDE.md` — "Heartbeat Tick Rate" | Cite; do not restate |
| C# style, naming, layout | **Spec #20** | Authoritative |
| File naming inside `src/` | **Spec #20** | Authoritative |
| `src/` folder layout shape | **Spec #20** (shape); `src/CLAUDE.md` (concrete paths) | Authoritative for shape; defers concrete paths to `src/CLAUDE.md` |
| Constant catalogue file locations and naming | **Spec #20** (convention); `src/CLAUDE.md` (concrete paths) | Authoritative for convention; defers concrete paths to `src/CLAUDE.md` |
| Build/test commands | `src/CLAUDE.md` (deferred) | Out of scope |

---

**KD-1 — Cite-not-redefine.**

*Statement:* Spec #20 cites every root `CLAUDE.md` invariant it depends on; it never
paraphrases or redeclares those rules. **Verbatim reproduction is permitted only with
explicit attribution and an authoritative-source disclaimer**, and only when the cited
table is genuinely needed at the point of use for code-author convenience (current
single instance: the constant-tag table in §3.2.1). A reproduction that qualifies under
this carve-out MUST include both: (a) a `(Source: root CLAUDE.md — "<section name>",
retrieved <date>)` attribution line, and (b) a "if a discrepancy exists, root
`CLAUDE.md` is authoritative" disclaimer. Whenever §3.2.1 is touched, the reviewer MUST
run a literal `diff` against the corresponding `CLAUDE.md` block — a presence check is
insufficient (audit finding H-01 demonstrated that without a diff, a one-phrase drift
slipped through Q-01).

*Rationale:* Project history documents how two-sources-of-truth drift produces silent
inconsistencies (root `CLAUDE.md` — "Things That Have Gone Wrong Before": stale spec
numbers; Pass Mechanics ERR-class audit findings). Constant-tag definitions, the fatigue
convention, and determinism rules are the exclusive property of root `CLAUDE.md`. Any
restatement in Spec #20 creates a maintenance hazard: when `CLAUDE.md` is updated, the
Spec #20 copy silently diverges until a reader notices the mismatch. The verbatim-with-
attribution carve-out exists because the tag table is referenced often enough by
code-authors that an indirect pointer would degrade usability; the diff-on-every-change
discipline is what keeps the carve-out safe.

*Consequence-if-violated:* Silent divergence between `CLAUDE.md` and Spec #20 on (for
example) constant-tag semantics; a future implementer citing "Spec #20 §3.2" could
operate under different rules than one citing "CLAUDE.md", producing exactly the bug
class documented in the Pass Mechanics audit.

---

**KD-2 — Authority Matrix.**

*Statement:* Every rule in the System XI governance space has exactly one owner;
the Authority Matrix in §1.3 names that owner.

*Rationale:* Without an explicit ownership boundary, Spec #20 risks either under-
specifying (leaving rules implicit in prose) or over-specifying (redefining rules already
owned by `CLAUDE.md` or a physics spec). The three-way partition — root `CLAUDE.md` for
project invariants, Spec #20 for code-shape rules, `src/CLAUDE.md` for codebase-local
pointers — ensures every rule has a single discoverable home and eliminates overlap.

*Consequence-if-violated:* Rule conflicts at review time with no clear tie-breaker, or
silent rule gaps that different reviewers fill inconsistently over successive PRs.

---

**KD-3 — Template-slot reconciliation.**

*Statement:* Spec #20 uses the standard CLAUDE.md 9-section template with three slots
re-purposed: §3 holds rules in lieu of formulas; §5 holds conformance verification in
lieu of numerical tests; §6 holds code performance rules in lieu of complexity analysis.

*Rationale:* The CLAUDE.md template was designed for physics and AI specs. Dropping
unused slots would break the cross-spec reader expectation that a given section number
contains a particular class of content. Re-purposing each slot to carry the closest
meta-spec analogue preserves section-number conventions while accommodating the content
of a meta-spec. The re-purposing is stated explicitly here so readers are not surprised.

*Consequence-if-violated:* Cross-spec reviewers expecting §5 to contain numerical test
catalogues find conformance review checklists instead, triggering uncertainty about
whether the spec is complete.

---

**KD-4 — Stage 0 verification is manual review.**

*Statement:* At Stage 0, conformance verification is performed by manual review against
§2.2 FRs. No static-analysis tooling is required.

*Rationale:* No source code exists at Stage 0. Empirical lint baselines (cyclomatic
complexity, file length) cannot be established against non-existent code. Mandating CI
gates before any code exists produces infrastructure with no calibration signal and
arbitrary thresholds that will be violated or ignored on day one. The Stage 0+1
transition is the correct moment to activate tooling (§5.2).

*Consequence-if-violated:* Arbitrary threshold values that immediately block legitimate
code, degrading reviewer trust in the tooling from first use.

---

**KD-5 — No numeric lint thresholds at Stage 0.**

*Statement:* All numeric conformance thresholds — cyclomatic complexity, file length,
method length, allocation count — are deferred to first real code (Deferral D1 in §7.5).

*Rationale:* Pre-code thresholds are guesses. System XI's struct-based,
zero-allocation game loop will produce a different distribution of method lengths and
complexity scores than a typical object-oriented Unity project. The right time to choose
thresholds is after the first meaningful body of real code has been profiled and reviewed.
Choosing them now would be calibrating an instrument before the measurement domain exists.

*Consequence-if-violated:* Thresholds that are either too strict (blocking legitimate
game-loop structs whose Update methods are necessarily long) or too loose (signalling
nothing useful), both outcomes eroding reviewer confidence in the tooling.

---

**KD-6 — Single-source-of-truth lists.**

*Statement:* Banned and required API lists live exclusively in Appendix D. Sections
§3.3, §3.4, §5.2, and §7.1 cite Appendix D by category name or symbol; they do not
reproduce those lists.

*Rationale:* Symbol-level lists duplicated across multiple sections are the most
persistent source of silent divergence in specification documents. When a new banned
API is identified, updating the rule prose in §3.4 without also updating Appendix D
(or vice versa) produces an invisible enforcement gap: the rule text forbids the symbol
but Appendix D's `BannedSymbols.txt` seed does not include it, so the Stage 1 analyzer
silently misses it.

*Consequence-if-violated:* A banned symbol not present in Appendix D cannot generate
a `BannedSymbols.txt` entry at Stage 1, creating a permanent gap between the written
rule and the enforced rule.

---

## 1.4 Dependencies and Integration Contracts

**Upstream — substantive (binding rules cited from these documents):**
- Root `CLAUDE.md` — constant-tag definitions, determinism rules, interface principle,
  fatigue convention, coordinate system, heartbeat tick rates, inline-comment policy
  ("default to writing no comments"), version-history rule, file-header rule.
- `docs/planning/development-best-practices.md` — allocation budget values cited in
  §3.3.4 and §6.1.

**Upstream — consulted at coding-start; placeholder during spec drafting:**
- `docs/tracking/certification-platform.md` — Unity LTS revision and C# language
  version pin. Spec #20 references this file by path in §3.1.3 (FR-CS-008). The
  concrete language-version value is not required for spec approval; it is required
  before the first Stage 1 implementation commit. Activation of FR-CS-008 is gated on
  this document resolving from placeholder status (see root `CLAUDE.md` open issue:
  "Stage 0 host platform pin").

**Downstream (every Stage 1+ source file depends on this spec):**
- Every `.cs` file under `src/` cites Spec #20 in its file header (FR-CS-057).
- The existing `src/CLAUDE.md` carries concrete paths and assembly definitions derived
  from the shape conventions established in §4 of this spec.

**Architecture-governance dependencies:**
- `docs/planning/project-architecture-governance.md` is a substantive upstream authority
  for FR-CS-074–081: it owns property admission, applicability, review disposition,
  convergence and the distinction between declarations and blocking evidence.
- Spec #19 (Testing Strategy & Framework) owns executable proof classes, bounded
  substitutes and merge-gate evidence. Spec #20 supplies the code/integration rules those
  proofs evaluate and does not duplicate #19's proof model.
- Spec #9 (Fixed64 Math Library) remains a pointer-only future trigger for §3.7.4.

Spec #20 still imports no physics/AI domain rule from Specs #1–#18: it governs *how*
their implementations are coded and integrated, not *what* they compute.

**Approval and activation independence:** approval of this amendment does not itself
activate architecture enforcement. A4 must first provide the compiler-backed resolver,
closed discovery inventory and blind-spot fixtures required by §3.5.6–§3.5.7; Spec #19
owns proof/gate mechanics, and the later activation stage wires only verified blocking
checks. Unsupported semantic claims remain report-only.

---

## 1.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 7, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 1. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fix (audit finding M-04): KD-1 statement softened from absolute "never restates" to "never paraphrases or redeclares; verbatim reproduction permitted only with explicit attribution + authoritative-source disclaimer + literal-diff discipline on every change." The carve-out matches the §3.2.1 actual practice (constant-tag table reproduced verbatim) and adds a diff-not-presence-check requirement directly motivated by audit finding H-01, where Q-01's presence check missed a one-phrase drift. Non-behavioural: codifies existing practice and tightens the audit method around it. | — |
| 1.0.2 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.0.3 | August 18, 2026 | Claude Code | **Adversarial-review round-6 finding H6 (consequential).** §1's scope list said "the five tag types defined in root `CLAUDE.md`"; the root table holds six (`[CROSS-PENDING]` — see section-3.md v1.6 for the primary fix). Enumeration corrected to six; no other content change. | — |
| 1.0.4 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding M3.** The 1.0.3 row above fixed §1's prose scope list but missed the §1.3 Authority Matrix row two sections later, which still enumerated five tags (`[GT]`/`[EST]`/`[FIXED]`/`[DERIVED]`/`[CROSS]`) — the same six-vs-five gap the 1.0.3 row exists to close, left standing in a second table. `[CROSS-PENDING]` added to the Authority Matrix row; no other content change. | — |
| 1.1 | September 2, 2026 | Codex | **A3.1b supporting-surface synchronization.** Extends scope/authority/dependency text to FR-CS-074–081, Project Architecture Governance, and Spec #19's proof/gate ownership; states that A3 approval is distinct from A4/A8 mechanical activation. No new runtime dependency or enforcement is introduced. | PENDING — A3.4 |
| 1.2 | September 3, 2026 | Claude Code | **A3.1b stale-claim sweep completed here.** §1.4's downstream bullet still described `src/CLAUDE.md` as future work ("The future `src/CLAUDE.md` will contain concrete paths and assembly definitions"); the file exists, and §1.4's own architecture-governance bullets and both other outline tiers had already been corrected in A3.1b. Restated as existing. **KD-4 deliberately NOT modernized here.** An automated review of PR #351 asked for the normative KD-4 text to be updated to match live tooling; the owner declined that scope for this slice — A3.1b is a synchronization and finding-closure pass, and rewriting a Key Design Decision's statement is a governance-semantic change, not a stale-wording fix. §5.1's "Tooling status" paragraph already frames KD-4 as the historical Stage 0 decision and records that the Stage 0+1 transition has since arrived, which is where reviewers meet the question. Tracked separately for A3.4. No FR, count, KD, authority-matrix row or scope boundary changed. | PENDING — A3.4 |

---

*End of Section 1 — Code Standards & Style Guide Specification #20*
*System XI — Specification #20 of 20 | Stage 0: Physics Foundation*
