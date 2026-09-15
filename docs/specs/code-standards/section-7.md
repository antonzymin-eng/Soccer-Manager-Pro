# Code Standards & Style Guide Specification #20 — Section 7: Future Extensions

**File:** `docs/specs/code-standards/section-7.md`
**Purpose:** Records Stage 1 tooling deliverables and CI gates that activate when coding
begins, Stage 5+ rule extensions, permanent exclusions (style debates this spec refuses to
relitigate), and the deferred-decisions tracker (D1–D5).
**Created:** May 8, 2026
**Modified:** September 14, 2026
**Version:** 1.4
**Status:** AMENDMENT DRAFT (A3.1b; approved v1.2 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 7; `outline-mid.md` v1.2, §7.1–§7.5
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.35, §6; A3.1b

---

## Table of Contents

- [7.1 Stage 1 Deliverables](#71-stage-1-deliverables)
- [7.2 Stage 1 CI Gates](#72-stage-1-ci-gates)
- [7.3 Stage 5+ Extensions](#73-stage-5-extensions)
- [7.4 Permanent Exclusions](#74-permanent-exclusions)
- [7.5 Deferred Decisions Tracker](#75-deferred-decisions-tracker)
- [7.6 Version History](#76-version-history)

---

## 7.1 Stage 1 Deliverables

Five tooling artifacts MUST be produced at Stage 1 project setup, before the first source
file is committed to `src/`. Each deliverable cites Spec #20 as its normative source.

| # | Deliverable | Trigger to activate | Acceptance criterion |
|---|---|---|---|
| D1-artifact | Numeric lint thresholds (line-length cap, method-length cap, nesting-depth limit; resolves Deferral D1 in §7.5) | **Platform half satisfied:** `docs/tracking/certification-platform.md` is pinned to Unity 6000.4.9f1 / C# 9.0. Remaining trigger: first Stage 1 module profiled. FR-CS-008 is already active; profiling chooses threshold values, not the language version. | Threshold values committed to `.editorconfig` and cited in `src/CLAUDE.md` |
| D2-artifact | Roslyn analyzer ruleset (`.ruleset` or `.editorconfig` `[*.cs]` severity block) | First `src/` file committed | All Spec #20 Error-level FRs produce `error`-severity Roslyn diagnostics; `dotnet build` fails on any violation; Stage 1 analyzer IDs from Appendix D §D.2–§D.5 are populated |
| D3-artifact | `BannedSymbols.txt` populated | First `src/` file committed | File contains every symbol in Appendix D categories `det-banned` (§D.1) and `alloc-hot-path` (§D.2); `Microsoft.CodeAnalysis.BannedApiAnalyzers` package referenced in all game-loop `.csproj` files |
| D4-artifact | `.editorconfig` finalised | First `src/` file committed | Covers: indent style (4 spaces), brace style (Allman), `using` directive placement, `var` policy, **block-scoped namespace style (file-scoped namespaces are C# 10 and outside FR-CS-008's C# 9.0 ceiling)**, `sealed`-by-default suggestion; committed at repo root alongside `BannedSymbols.txt` |
| D5-artifact | First `src/CLAUDE.md` drafted | All 20 Stage 0 specs approved | **Satisfied:** `src/CLAUDE.md` exists and carries concrete source-layout/build guidance. Future corrections follow that file's own landing chain. |

**Relationship to deferred decisions:** D1-artifact still depends on the unresolved
profiling half of D1 (§7.5); its platform/language prerequisite is satisfied. The
historical D5-artifact creation gate has also been satisfied and the artifact exists.
The D2/D3/D4 artifacts remain owed; this erratum does not claim their implementation.

**Appendix D and Roslyn IDs:** The Appendix D tables in `appendices.md` include a
"Stage 1 analyzer ID (placeholder)" column. Populating those placeholders with real
diagnostic IDs is part of D2-artifact and D3-artifact. At Stage 0 the column values are
`TBD-ROSLYN-###`; they are replaced at Stage 1 once the analyzer packages are chosen.

---

## 7.2 Stage 1 CI Gates

Three gates enforce Spec #20 compliance in the CI pipeline. Each gate runs a specific
command; failure at that gate blocks the corresponding action.

| Gate | Trigger | Command | Failure behaviour |
|---|---|---|---|
| Pre-commit | Every `git commit` on a `src/` file | `dotnet format --verify-no-changes` | Commit rejected; developer runs `dotnet format` to auto-fix whitespace/indent violations before retrying |
| PR | Every pull-request open or push-to-PR | `dotnet build /p:TreatWarningsAsErrors=true` with Roslyn analyzer ruleset active | PR blocked; all Error-level Spec #20 diagnostics must be resolved before merge is permitted |
| Merge | Merge to `main` | Zero-allocation profiler test on game-loop assemblies (Unity batch-mode, managed-heap snapshot) | Merge blocked; any non-zero allocation in the 60 Hz physics path (FR-CS-066) must be eliminated before merge |

**Current verification model (`ERR-020-008`; owner decision September 12, 2026):** the
**Unity 6000.4.9f1 editor on the pinned host is the governing compiler for code
conformance. A green Linux `tools/dotnet-ci/run-gate.sh` result is required supplemental
evidence, but it is not sufficient to establish that the code compiles.** The Linux shim
uses MSBuild project references, which can make dependencies visible transitively; Unity
compilation requires every assembly whose types a source assembly names to be listed
directly in that source assembly's `.asmdef`. A missing direct reference can therefore
remain green in the Linux gate and fail the governing Unity compile with errors such as
CS0012/CS0234. FR-CS-055's rule text is unchanged; §5.4.5 item 7 now makes the direct-
reference review test explicit until the planned analyzer exists.

`.github/workflows/ci.yml` currently runs `dotnet format whitespace --verify-no-changes`
on every push to `main` and every PR targeting `main`, over a synthetic project (advisory
— a failure emits a warning and exits 0). Each such push/PR also runs
`tools/dotnet-ci/run-gate.sh`, which compiles the whole synthetic tree and runs the NUnit
suites (blocking, non-certifying, and supplemental to Unity).

A pinned-host Unity workflow **does exist**, but it is not the per-PR governing compile:
`.github/workflows/nightly.yml` defines `nightly-determinism-certified`, a self-hosted
Windows job that checks the Unity 6000.4.9f1 pin and invokes Unity in batch mode. It is
conditional on `DETERMINISM_CERTIFIED_RUNNER_ENABLED == 'true'`, scheduled/manual rather
than PR-triggered, and filters execution to the determinism test surface. The current
repository-variable enablement is not established by this spec correction. This lane is
the existing automation seam for pinned-host execution; it does not currently prove each
code landing compiles in Unity.

Still missing from the original Stage 1 target are the Roslyn analyzer ruleset half of
the PR gate — no analyzer project, `BannedSymbols.txt`, or `.editorconfig` exists — and
the automated zero-allocation profiler merge gate. The command column above remains the
normative target for those missing pieces.

**Pre-commit hook setup note:** The pre-commit gate requires a Git pre-commit hook or
Husky configuration pointing at `dotnet format --verify-no-changes`. The exact hook
installation command is deferred to `src/CLAUDE.md` (D5-artifact, §7.1).

**Merge gate dependency:** the certification host is pinned; host pinning is no longer
the reason the zero-allocation merge gate is inactive. The remaining work is to wire and
verify pinned-host profiler execution as a blocking merge gate. Until that workflow and
evidence path exists, the profiler requirement remains normative but is not an automated
GitHub merge check.

**Erratum force.** The `ERR-020-008` platform/verification corrections in §§7.1, 7.2 and
7.5 are effective immediately against the approved Code Standards baseline. They do not
approve the separate A3.1a/A3.1b architecture amendment, which remains pending A3.4.

### Architecture-governance activation boundary (A3 → A4 → later enforcement)

FR-CS-074–081 are amendment-draft rules until A3.4 reapproves the combined A3.1a/A3.1b package. Reapproval still does **not** make every architecture assertion blocking. A4 must first implement and verify the compiler-backed discovery/identity resolver, cross-registry contract binding, closed runtime-surface inventory, static-initializer coverage, and blind-spot fixtures required by §3.5.6–§3.5.7. Until that evidence exists, those unresolved bindings and semantic absence claims remain report-only. Spec #19 owns executable proof classes, bounded-substitute rules and gate evidence. The later activation stage may wire only checks whose coverage and failure behavior have been verified; A3.1b changes no workflow or CI enforcement.

---

## 7.3 Stage 5+ Extensions

The following rules are intentionally deferred beyond Stage 0 and Stage 1. They are listed
here so future maintainers understand why they are absent from the current rule set.

### Fixed64 Enforcement (Stage 5 trigger: Spec #9 ships)

CLAUDE.md (Open Issues — "Fixed64 stage scope decision") establishes that Fixed64
migration is a **Stage 5+** concern. At Stage 5, when cross-platform multiplayer is added:

- FR-CS-072 (`double` prohibition) will be reviewed; if Fixed64 replaces `float` on the
  physics path, the rule expands to cover `double` and `float` in game-state assemblies.
- Spec #9 (the Fixed64 library spec) will publish the authoritative `Fixed64` type. Spec
  #20 §3.7 will gain a cross-reference to Spec #9 at that point.
- Appendix D `det-banned` category will expand to include `System.Single` and
  `System.Double` for game-state assemblies (with a parallel `det-required` entry for
  the Fixed64 arithmetic type).
- The Roslyn analyzer ruleset (D2-artifact) will add diagnostics for floating-point
  literals in game-state namespaces.

### Cross-Platform Bit-Exact Parity Rules (Stage 5 trigger: multiplayer milestone)

Single-machine determinism (replay, save/load, debug rewind) is achieved at Stage 0 via
state snapshots, not deterministic arithmetic — per CLAUDE.md "When Writing Code." At
Stage 5, cross-platform bit-exact parity becomes a hard requirement. Additions at that
point:

- FMA and denormals-are-zero compiler flags remain locked to the certified platform tuple
  in `certification-platform.md`. FR-CS-040 itself is active at Stage 0 (the default ban
  applies); its override pathway requires both that platform pin and lead-developer
  sign-off.
- Platform-specific known-answer test (KAT) suites added to the merge gate.

### `unsafe` and SIMD Intrinsic Policy Revisit (Stage 2+ trigger: performance profiling)

The current Stage 0 rule prohibits `unsafe` code without lead-developer sign-off
(FR-CS-010). SIMD intrinsics (`System.Runtime.Intrinsics`) are not addressed. At Stage 2,
after profiler baselines exist, the policy SHOULD be revisited:

- If a hot-path system cannot meet its microsecond budget without SIMD, an exception
  process (matching the format in §2.2 Exception block) is added for that system.
- Any `unsafe` expansion requires a dedicated security and memory-safety review.

---

## 7.4 Permanent Exclusions

The following topics are **permanently excluded** from Spec #20 and from any future
amendment. They represent decisions that were made once and are not open for
re-specification.

### Style Debates Already Decided

| Topic | Decision | Where decided | Rationale for closure |
|---|---|---|---|
| Indentation: tabs vs spaces | 4 spaces | §3.1.4 | Matches C# community standard and `.editorconfig` default; reopening would break all existing diffs |
| Brace style | Allman (opening brace on new line) | §3.1.4 | Matches Microsoft C# Coding Conventions and the project's existing spec-file conventions |
| Line-ending style | LF (Unix) | §3.1.4 / `.editorconfig` | Git `autocrlf` handles Windows dev machines; re-debating yields no value |

These rules are frozen. A PR that reopens indentation or brace style will be rejected
without review. The goal is to eliminate bikeshedding permanently, not to invite yearly
revisits.

### Frameworks and Libraries Not Mandated

Spec #20 deliberately avoids mandating:

- **IoC / dependency injection container:** The Stage 0 physics layer uses struct-based
  composition with no service locator or DI framework. If a DI framework is introduced at
  Stage 3+ (UI layer), that choice belongs to the Stage 3 spec, not Spec #20.
- **Logging framework:** Unity's `Debug.Log` family is the Stage 0 baseline. A structured
  logging framework (Serilog, NLog, etc.) is a Stage 3+ concern. Spec #20 governs
  comment and documentation conventions, not runtime logging.
- **Serialization library:** JSON / binary serialization is out of scope for the physics
  layer. Spec #16 (Deterministic Simulation) owns the canonical serialization contract.

These exclusions prevent Spec #20 from accumulating framework-specific rules that would
need to change whenever a library is swapped.

---

## 7.5 Deferred Decisions Tracker

Five decisions were explicitly deferred from Stage 0. This table records both still-open
deferrals and decisions whose trigger has fired.

| ID | Decision deferred | Deferral / resolution statement | Trigger / current state | Owner |
|---|---|---|---|---|
| D1 | Numeric lint thresholds (line-length cap, method-length cap, nesting-depth limit) | Thresholds remain deferred per KD-5 (§1.3). The **platform prerequisite is satisfied**: Unity 6000.4.9f1 / C# 9.0 is pinned and FR-CS-008 is active. The only unresolved prerequisite is an actual profiled Stage 1 module from which to choose empirical thresholds. No placeholder values are inserted. | **Partially unblocked:** platform/language pin satisfied; first Stage 1 module profiling per §5.3 still required. | Lead developer + Stage 1 setup author |
| D2 | Test framework choice | Spec #19 (Testing Strategy) owns test-framework selection. Spec #20 §3.9.4 remains framework-agnostic to avoid a circular dependency. | Spec #19 is the authoritative owner; no #20 choice is required here. | Spec #19 author |
| D3 | Build commands, IDE setup, assembly GUIDs | Concrete implementation details live in the existing `src/CLAUDE.md`; the historical creation deferral has fired. | **Resolved as a location decision:** `src/CLAUDE.md` exists; maintain concrete codebase-local guidance there. | Stage 1 setup author |
| D4 | Fixed64 enforcement rules | Stage 0 uses `float`. Fixed64 migration remains Stage 5+. Spec #9 defines that future surface; see §7.3. | Spec #9 Stage 5+ activation. | Spec #9 author → Spec #20 amendment author |
| D5 | Concrete C# language version pin | **RESOLVED September 14, 2026 (`ERR-020-008`): C# 9.0**, fixed by the certified Unity 6000.4.9f1 host. FR-CS-008 is active; supplemental/synthetic compilers may not widen the allowed language surface. | **Resolved.** `certification-platform.md` now records the C# 9.0 row explicitly; `tools/dotnet-ci/generate_projects.py` already mirrors it with `<LangVersion>9.0</LangVersion>`. | Lead developer |

**Resolution protocol:** When a deferred decision is resolved, the owner MUST:
1. Update this table row (change "deferred" entry to a resolution summary with date).
2. Activate or amend any INACTIVE FRs that depended on the decision.
3. Append a version history entry to every section file changed.
4. Update `docs/tracking/PROGRESS.md` with the milestone.

---

## 7.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 8, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 7 and `outline-mid.md` v1.2 §7.1–§7.5. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fixes (audit finding H-02): corrected three stale FR-CS-### identifiers — §7.3 `double` cite FR-CS-039 → FR-CS-072; §7.3 `unsafe` cite FR-CS-042 → FR-CS-010; §7.3 FMA paragraph clarified that FR-CS-040 is active at Stage 0 and only its override pathway is gated on the platform pin. §7.1 D1-artifact and §7.5 D1 rewordings: D1 deferral is governed by KD-5 (no Stage 0 code to baseline against), with FR-CS-008 activation as a precondition, not the source of the threshold values themselves. | — |
| 1.0.2 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.1 | August 18, 2026 | Claude Code | **Adversarial-review round-6 finding H5.** Two sites asserted `src/` is empty / no source code exists, fifteen months after coding began (May 19, 2026). §7.2's "Stage 0 status" paragraph rewritten against the live tree and CI, every figure re-derived August 18, 2026 (35 assemblies via `ls -d src/*/ | wc -l`, 947 `.cs` files via `find src -name '*.cs' | wc -l`; `.github/workflows/ci.yml` runs the advisory `dotnet format whitespace` check and the blocking `tools/dotnet-ci/run-gate.sh` on every push) — and precise about what remains missing: the Roslyn analyzer ruleset, `BannedSymbols.txt`, `.editorconfig`, and the zero-allocation profiler merge gate. §7.5's D1 row premise ("no source code exists at Stage 0") corrected to the surviving half of its own argument: code exists, a profiled baseline does not, so D1 stays deferred on grounds that are still true. | — |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-7 finding H3.** §7.2's "on every push" corrected to `ci.yml`'s real triggers (`branches: [main]`, `push` and `pull_request`). Same correction as `section-4.md` v1.2 and `section-5.md` v1.2; the v1.1 row above is left as written per the history convention. | — |
| 1.3 | September 2, 2026 | Codex | **A3.1b supporting-surface synchronization.** Adds the explicit A3→A4→activation boundary for FR-CS-074–081: reapproval, compiler-backed resolver/discovery proof, and enforcement activation are separate gates; Spec #19 retains proof/gate ownership. Also fixes the live §7.2 "Every push" residue to the already-stated main-push/PR trigger scope. | PENDING — A3.4 |
| 1.4 | September 14, 2026 | Codex | **`ERR-020-008` — verification/platform drift corrected.** Records Unity 6000.4.9f1 on the pinned host as governing compiler; Linux is required supplemental but insufficient; documents the direct-`.asmdef` blind spot and the existing conditional `nightly-determinism-certified` pinned-host lane without misrepresenting it as a PR gate. Corrects the stale host-pin explanation for the profiler gate, resolves D5 at C# 9.0, marks D1's platform half satisfied, and records that the D5 `src/CLAUDE.md` artifact already exists. Consequential correction: D4-artifact now requires block-scoped namespaces because file-scoped namespaces are C# 10 and impossible under the activated C# 9.0 ceiling. The erratum is immediately effective; A3.1b remains pending A3.4. No FR-CS-055 text, architecture rule, schema, RNG stream/domain/draw site, or draw order changed. | — |

---

*End of Section 7 — Code Standards & Style Guide Specification #20*
*System XI — Specification #20 of 20 | Stage 0: Physics Foundation*
